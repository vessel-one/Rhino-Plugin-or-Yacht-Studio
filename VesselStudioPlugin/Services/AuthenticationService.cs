using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VesselStudioPlugin.Models;
using VesselStudioPlugin.Utils;

namespace VesselStudioPlugin.Services
{
    /// <summary>
    /// Handles OAuth 2.0 device authorization flow and session management
    /// </summary>
    public class AuthenticationService : IAuthService
    {
        #region Fields and Properties
        
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly string _clientId;
        private readonly string _instanceId;
        
        private AuthenticationSession? _currentSession;
        private Timer? _tokenRefreshTimer;
        private readonly object _sessionLock = new object();
        
        public event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;
        public event EventHandler<TokenRefreshedEventArgs>? TokenRefreshed;
        public event EventHandler<AuthenticationErrorEventArgs>? AuthenticationError;
        
        public bool IsAuthenticated
        {
            get
            {
                lock (_sessionLock)
                {
                    return _currentSession?.IsValid() == true && 
                           _currentSession.State == AuthenticationState.Authenticated;
                }
            }
        }
        
        public AuthenticationSession? CurrentSession
        {
            get
            {
                lock (_sessionLock)
                {
                    return _currentSession?.GetSafeCopy();
                }
            }
        }
        
        // Interface properties
        public AuthenticationState CurrentState
        {
            get
            {
                lock (_sessionLock)
                {
                    return _currentSession?.State ?? AuthenticationState.NotAuthenticated;
                }
            }
        }
        
        public string? CurrentUserId
        {
            get
            {
                lock (_sessionLock)
                {
                    return _currentSession?.UserInfo?.Id;
                }
            }
        }
        
        public string? CurrentToken
        {
            get
            {
                lock (_sessionLock)
                {
                    return _currentSession?.AccessToken;
                }
            }
        }
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of AuthenticationService
        /// </summary>
        /// <param name="httpClient">HTTP client for API communication</param>
        /// <param name="apiBaseUrl">Base URL for the Vessel Studio API</param>
        /// <param name="clientId">OAuth client ID</param>
        /// <param name="instanceId">Rhino instance ID for multi-instance support</param>
        public AuthenticationService(HttpClient httpClient, string apiBaseUrl, string clientId, string? instanceId = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiBaseUrl = apiBaseUrl ?? throw new ArgumentNullException(nameof(apiBaseUrl));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _instanceId = instanceId ?? Guid.NewGuid().ToString();
            
            // Try to restore previous session
            _ = Task.Run(RestorePreviousSessionAsync);
        }
        
        #endregion
        
        #region Interface Implementation
        
        /// <summary>
        /// Initiates the authentication flow by opening the user's browser
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if authentication succeeded, false otherwise</returns>
        public async Task<bool> LoginAsync(CancellationToken cancellationToken = default)
        {
            return await AuthenticateAsync(cancellationToken);
        }
        
        /// <summary>
        /// Logs out the current user and clears stored credentials
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            lock (_sessionLock)
            {
                var previousState = _currentSession?.State ?? AuthenticationState.NotAuthenticated;
                _currentSession = null;
                OnAuthenticationStateChanged(previousState, AuthenticationState.NotAuthenticated);
            }
            
            // Clear stored credentials
            await SecureStorage.DeleteAsync("vessel_studio_session");
            
            // Stop token refresh timer
            _tokenRefreshTimer?.Dispose();
            _tokenRefreshTimer = null;
        }
        
        /// <summary>
        /// Validates the current authentication token
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if token is valid, false otherwise</returns>
        public async Task<bool> ValidateTokenAsync(CancellationToken cancellationToken = default)
        {
            lock (_sessionLock)
            {
                if (_currentSession == null || !_currentSession.IsValid())
                    return false;
            }
            
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/auth/validate", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Loads the user's accessible projects from Vessel Studio
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>List of projects the user can access</returns>
        public async Task<List<ProjectInfo>> GetUserProjectsAsync(CancellationToken cancellationToken = default)
        {
            if (!IsAuthenticated)
                return new List<ProjectInfo>();
                
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/projects", cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var projects = JsonSerializer.Deserialize<List<ProjectInfo>>(json);
                return projects ?? new List<ProjectInfo>();
            }
            catch (Exception ex)
            {
                OnAuthenticationError($"Failed to get user projects: {ex.Message}");
                return new List<ProjectInfo>();
            }
        }
        
        /// <summary>
        /// Checks if the user has upload permissions for the specified project
        /// </summary>
        /// <param name="projectId">The project ID to check</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if user has upload permissions, false otherwise</returns>
        public async Task<bool> HasProjectPermissionAsync(string projectId, CancellationToken cancellationToken = default)
        {
            if (!IsAuthenticated || string.IsNullOrEmpty(projectId))
                return false;
                
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/api/projects/{projectId}/permissions", cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var permissions = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                return permissions?.GetValueOrDefault("upload", false) == true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Initializes the service by attempting to restore a previous session
        /// </summary>
        /// <returns>True if session was restored, false otherwise</returns>
        public async Task<bool> InitializeAsync()
        {
            return await RestorePreviousSessionAsync();
        }
        
        /// <summary>
        /// Gets the authentication session information
        /// </summary>
        /// <returns>Current session info or null if not authenticated</returns>
        public AuthenticationSession? GetCurrentSession()
        {
            return CurrentSession;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Initiates OAuth 2.0 device authorization flow
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if authentication successful, false otherwise</returns>
        public async Task<bool> AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Step 1: Request device authorization
                var deviceAuth = await RequestDeviceAuthorizationAsync(cancellationToken);
                if (deviceAuth == null)
                {
                    OnAuthenticationError("Failed to request device authorization");
                    return false;
                }
                
                // Step 2: Launch browser for user authorization
                LaunchAuthorizationBrowser(deviceAuth.VerificationUriComplete);
                
                // Step 3: Create pending session
                var session = new AuthenticationSession
                {
                    UserId = string.Empty, // Will be populated after successful auth
                    InstanceId = _instanceId,
                    State = AuthenticationState.PendingAuthorization,
                    DeviceCode = deviceAuth.DeviceCode,
                    UserCode = deviceAuth.UserCode,
                    VerificationUri = deviceAuth.VerificationUri,
                    VerificationUriComplete = deviceAuth.VerificationUriComplete,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(deviceAuth.ExpiresIn)
                };
                
                UpdateCurrentSession(session);
                
                // Step 4: Poll for token
                var success = await PollForTokenAsync(deviceAuth, cancellationToken);
                
                if (success)
                {
                    Rhino.RhinoApp.WriteLine("Authentication successful!");
                }
                else
                {
                    OnAuthenticationError("Authentication failed or timed out");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                OnAuthenticationError($"Authentication error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Signs out the current user and clears stored credentials
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        public async Task SignOutAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                string? userId = null;
                
                lock (_sessionLock)
                {
                    userId = _currentSession?.UserId;
                    
                    if (_currentSession != null)
                    {
                        _currentSession.State = AuthenticationState.SignedOut;
                        OnAuthenticationStateChanged(AuthenticationState.SignedOut);
                    }
                }
                
                // Stop token refresh timer
                _tokenRefreshTimer?.Dispose();
                _tokenRefreshTimer = null;
                
                // Revoke token on server (best effort)
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        await RevokeTokenAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Rhino.RhinoApp.WriteLine($"Failed to revoke token on server: {ex.Message}");
                    }
                    
                    // Clear stored credentials
                    SecureStorage.DeleteCredentials(userId, _instanceId);
                }
                
                // Clear current session
                lock (_sessionLock)
                {
                    _currentSession = null;
                }
                
                Rhino.RhinoApp.WriteLine("Signed out successfully");
            }
            catch (Exception ex)
            {
                OnAuthenticationError($"Sign out error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refreshes the current access token using the refresh token
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if refresh successful, false otherwise</returns>
        public async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                string? refreshToken = null;
                
                lock (_sessionLock)
                {
                    if (_currentSession == null || string.IsNullOrEmpty(_currentSession.RefreshToken))
                    {
                        OnAuthenticationError("No refresh token available");
                        return false;
                    }
                    
                    refreshToken = _currentSession.RefreshToken;
                }
                
                var requestData = new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken,
                    ["client_id"] = _clientId
                };
                
                var response = await SendTokenRequestAsync(requestData, cancellationToken);
                if (response == null)
                    return false;
                
                // Update session with new tokens
                lock (_sessionLock)
                {
                    if (_currentSession != null)
                    {
                        _currentSession.AccessToken = response.AccessToken;
                        _currentSession.RefreshToken = response.RefreshToken ?? _currentSession.RefreshToken;
                        _currentSession.ExpiresAt = DateTime.UtcNow.AddSeconds(response.ExpiresIn);
                        _currentSession.LastRefreshed = DateTime.UtcNow;
                        
                        // Store updated credentials
                        SecureStorage.StoreCredentials(
                            _currentSession.UserId, 
                            _currentSession.AccessToken, 
                            _currentSession.RefreshToken, 
                            _instanceId);
                        
                        SecureStorage.StoreSession(_currentSession);
                    }
                }
                
                OnTokenRefreshed();
                ScheduleTokenRefresh();
                
                return true;
            }
            catch (Exception ex)
            {
                OnAuthenticationError($"Token refresh error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets the current access token, refreshing if necessary
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Valid access token or null if not available</returns>
        public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            lock (_sessionLock)
            {
                if (_currentSession == null)
                    return null;
                
                // Check if token is still valid (with 5 minute buffer)
                if (_currentSession.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
                {
                    return _currentSession.AccessToken;
                }
            }
            
            // Token needs refresh
            var refreshed = await RefreshTokenAsync(cancellationToken);
            if (!refreshed)
                return null;
            
            lock (_sessionLock)
            {
                return _currentSession?.AccessToken;
            }
        }
        
        /// <summary>
        /// Gets user information from the authentication session
        /// </summary>
        /// <returns>User information or null if not authenticated</returns>
        public UserInfo? GetUserInfo()
        {
            lock (_sessionLock)
            {
                if (_currentSession == null || !_currentSession.IsValid())
                    return null;
                
                return new UserInfo
                {
                    UserId = _currentSession.UserId,
                    Email = _currentSession.UserEmail,
                    DisplayName = _currentSession.UserDisplayName,
                    IsAuthenticated = _currentSession.State == AuthenticationState.Authenticated
                };
            }
        }
        
        /// <summary>
        /// Cleans up resources
        /// </summary>
        public void Dispose()
        {
            _tokenRefreshTimer?.Dispose();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Requests device authorization from the OAuth server
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Device authorization response</returns>
        private async Task<DeviceAuthorizationResponse?> RequestDeviceAuthorizationAsync(CancellationToken cancellationToken)
        {
            try
            {
                var requestData = new Dictionary<string, string>
                {
                    ["client_id"] = _clientId,
                    ["scope"] = "read:projects write:projects"
                };
                
                var content = new FormUrlEncodedContent(requestData);
                var requestUrl = $"{_apiBaseUrl}/oauth/device/code";
                
                var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<DeviceAuthorizationResponse>(responseContent);
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Device authorization request failed: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Launches the default browser to the authorization URL
        /// </summary>
        /// <param name="authorizationUrl">The authorization URL</param>
        private void LaunchAuthorizationBrowser(string authorizationUrl)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = authorizationUrl,
                    UseShellExecute = true
                };
                
                Process.Start(processInfo);
                
                Rhino.RhinoApp.WriteLine($"Please complete authorization in your browser: {authorizationUrl}");
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Failed to launch browser: {ex.Message}");
                Rhino.RhinoApp.WriteLine($"Please manually navigate to: {authorizationUrl}");
            }
        }
        
        /// <summary>
        /// Polls the token endpoint until authorization is complete
        /// </summary>
        /// <param name="deviceAuth">Device authorization response</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if token obtained successfully</returns>
        private async Task<bool> PollForTokenAsync(DeviceAuthorizationResponse deviceAuth, CancellationToken cancellationToken)
        {
            var interval = TimeSpan.FromSeconds(deviceAuth.Interval);
            var expiresAt = DateTime.UtcNow.AddSeconds(deviceAuth.ExpiresIn);
            
            while (DateTime.UtcNow < expiresAt && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, cancellationToken);
                    
                    var tokenResponse = await RequestTokenAsync(deviceAuth.DeviceCode, cancellationToken);
                    if (tokenResponse != null)
                    {
                        // Get user info
                        var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);
                        
                        // Update session with tokens and user info
                        lock (_sessionLock)
                        {
                            if (_currentSession != null)
                            {
                                _currentSession.AccessToken = tokenResponse.AccessToken;
                                _currentSession.RefreshToken = tokenResponse.RefreshToken;
                                _currentSession.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                                _currentSession.UserId = userInfo?.UserId ?? "unknown";
                                _currentSession.UserEmail = userInfo?.Email;
                                _currentSession.UserDisplayName = userInfo?.DisplayName;
                                _currentSession.State = AuthenticationState.Authenticated;
                                
                                // Store credentials securely
                                SecureStorage.StoreCredentials(
                                    _currentSession.UserId, 
                                    _currentSession.AccessToken, 
                                    _currentSession.RefreshToken, 
                                    _instanceId);
                                
                                SecureStorage.StoreSession(_currentSession);
                            }
                        }
                        
                        OnAuthenticationStateChanged(AuthenticationState.Authenticated);
                        ScheduleTokenRefresh();
                        
                        return true;
                    }
                }
                catch (AuthorizationPendingException)
                {
                    // Authorization still pending, continue polling
                    continue;
                }
                catch (SlowDownException)
                {
                    // Server requested to slow down, double the interval
                    interval = TimeSpan.FromSeconds(interval.TotalSeconds * 2);
                    continue;
                }
                catch (Exception ex)
                {
                    Rhino.RhinoApp.WriteLine($"Token polling error: {ex.Message}");
                    break;
                }
            }
            
            // Polling failed or timed out
            lock (_sessionLock)
            {
                if (_currentSession != null)
                {
                    _currentSession.State = AuthenticationState.Failed;
                    OnAuthenticationStateChanged(AuthenticationState.Failed);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Requests an access token using the device code
        /// </summary>
        /// <param name="deviceCode">Device code from authorization response</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Token response or null if not ready</returns>
        private async Task<TokenResponse?> RequestTokenAsync(string deviceCode, CancellationToken cancellationToken)
        {
            var requestData = new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                ["device_code"] = deviceCode,
                ["client_id"] = _clientId
            };
            
            try
            {
                return await SendTokenRequestAsync(requestData, cancellationToken);
            }
            catch (AuthorizationPendingException)
            {
                // Re-throw these to continue polling
                throw;
            }
            catch (SlowDownException)
            {
                // Re-throw these to adjust polling interval
                throw;
            }
            catch (Exception)
            {
                // Other errors should stop polling
                return null;
            }
        }
        
        /// <summary>
        /// Sends a token request to the OAuth server
        /// </summary>
        /// <param name="requestData">Request form data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Token response</returns>
        private async Task<TokenResponse?> SendTokenRequestAsync(Dictionary<string, string> requestData, CancellationToken cancellationToken)
        {
            var content = new FormUrlEncodedContent(requestData);
            var requestUrl = $"{_apiBaseUrl}/oauth/token";
            
            var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize<OAuthErrorResponse>(responseContent);
                
                switch (error?.Error)
                {
                    case "authorization_pending":
                        throw new AuthorizationPendingException();
                    case "slow_down":
                        throw new SlowDownException();
                    case "expired_token":
                        throw new ExpiredTokenException();
                    case "access_denied":
                        throw new AccessDeniedException();
                    default:
                        throw new OAuthException($"Token request failed: {error?.ErrorDescription ?? "Unknown error"}");
                }
            }
            
            return JsonSerializer.Deserialize<TokenResponse>(responseContent);
        }
        
        /// <summary>
        /// Gets user information using an access token
        /// </summary>
        /// <param name="accessToken">Valid access token</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>User information</returns>
        private async Task<UserInfo?> GetUserInfoAsync(string accessToken, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/user/me");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                
                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<UserInfo>(responseContent);
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Failed to get user info: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Revokes the current access token
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        private async Task RevokeTokenAsync(CancellationToken cancellationToken)
        {
            string? accessToken = null;
            
            lock (_sessionLock)
            {
                accessToken = _currentSession?.AccessToken;
            }
            
            if (string.IsNullOrEmpty(accessToken))
                return;
            
            var requestData = new Dictionary<string, string>
            {
                ["token"] = accessToken,
                ["client_id"] = _clientId
            };
            
            var content = new FormUrlEncodedContent(requestData);
            var requestUrl = $"{_apiBaseUrl}/oauth/revoke";
            
            await _httpClient.PostAsync(requestUrl, content, cancellationToken);
        }
        
        /// <summary>
        /// Restores a previous authentication session from secure storage
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        private async Task RestorePreviousSessionAsync()
        {
            try
            {
                // Try to find any stored sessions (we don't know the user ID yet)
                // In practice, this would need to iterate through known user IDs
                // or store a "last user" reference
                
                // For now, we'll implement a simple approach where we check for
                // a default session file
                var session = SecureStorage.RetrieveSession("default", _instanceId);
                
                if (session != null && session.IsValid())
                {
                    // Try to refresh the token to ensure it's still valid
                    lock (_sessionLock)
                    {
                        _currentSession = session;
                    }
                    
                    var refreshed = await RefreshTokenAsync();
                    if (refreshed)
                    {
                        OnAuthenticationStateChanged(AuthenticationState.Authenticated);
                        ScheduleTokenRefresh();
                        Rhino.RhinoApp.WriteLine("Previous session restored successfully");
                    }
                    else
                    {
                        // Clear invalid session
                        lock (_sessionLock)
                        {
                            _currentSession = null;
                        }
                        
                        SecureStorage.DeleteCredentials("default", _instanceId);
                    }
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Failed to restore previous session: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Schedules automatic token refresh
        /// </summary>
        private void ScheduleTokenRefresh()
        {
            _tokenRefreshTimer?.Dispose();
            
            TimeSpan refreshInterval;
            
            lock (_sessionLock)
            {
                if (_currentSession == null)
                    return;
                
                // Schedule refresh 5 minutes before expiry, or in 30 minutes, whichever is sooner
                var timeUntilExpiry = _currentSession.ExpiresAt - DateTime.UtcNow;
                var refreshTime = TimeSpan.FromMinutes(Math.Min(30, Math.Max(5, timeUntilExpiry.TotalMinutes - 5)));
                refreshInterval = refreshTime;
            }
            
            _tokenRefreshTimer = new Timer(async _ =>
            {
                await RefreshTokenAsync();
            }, null, refreshInterval, Timeout.InfiniteTimeSpan);
        }
        
        /// <summary>
        /// Updates the current session thread-safely
        /// </summary>
        /// <param name="session">New session</param>
        private void UpdateCurrentSession(AuthenticationSession session)
        {
            lock (_sessionLock)
            {
                _currentSession = session;
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnAuthenticationStateChanged(AuthenticationState newState)
        {
            AuthenticationStateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs(newState));
        }
        
        private void OnTokenRefreshed()
        {
            TokenRefreshed?.Invoke(this, new TokenRefreshedEventArgs());
        }
        
        private void OnAuthenticationError(string message)
        {
            AuthenticationError?.Invoke(this, new AuthenticationErrorEventArgs(message));
        }
        
        #endregion
    }
    
    #region Response Models
    
    /// <summary>
    /// Response from device authorization endpoint
    /// </summary>
    internal class DeviceAuthorizationResponse
    {
        public string DeviceCode { get; set; } = string.Empty;
        public string UserCode { get; set; } = string.Empty;
        public string VerificationUri { get; set; } = string.Empty;
        public string VerificationUriComplete { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public int Interval { get; set; } = 5;
    }
    
    /// <summary>
    /// Response from token endpoint
    /// </summary>
    internal class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = "bearer";
    }
    
    /// <summary>
    /// OAuth error response
    /// </summary>
    internal class OAuthErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public string ErrorDescription { get; set; } = string.Empty;
    }
    
    #endregion
    
    #region OAuth Exceptions
    
    internal class OAuthException : Exception
    {
        public OAuthException(string message) : base(message) { }
    }
    
    internal class AuthorizationPendingException : OAuthException
    {
        public AuthorizationPendingException() : base("Authorization pending") { }
    }
    
    internal class SlowDownException : OAuthException
    {
        public SlowDownException() : base("Slow down") { }
    }
    
    internal class ExpiredTokenException : OAuthException
    {
        public ExpiredTokenException() : base("Token expired") { }
    }
    
    internal class AccessDeniedException : OAuthException
    {
        public AccessDeniedException() : base("Access denied") { }
    }
    
    #endregion
}