using System;
using System.ComponentModel.DataAnnotations;

namespace VesselStudioPlugin.Models
{
    /// <summary>
    /// Manages user authentication state and token lifecycle
    /// </summary>
    public class AuthenticationSession
    {
        #region Properties
        
        /// <summary>
        /// Gets or sets the Vessel Studio user identifier
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the JWT or OAuth access token
        /// </summary>
        public string? AccessToken { get; set; }
        
        /// <summary>
        /// Gets or sets the token used to refresh the access token
        /// </summary>
        public string? RefreshToken { get; set; }
        
        /// <summary>
        /// Gets or sets when the access token expires
        /// </summary>
        public DateTime TokenExpiry { get; set; }
        
        /// <summary>
        /// Gets or sets when the authentication session was initially started
        /// </summary>
        public DateTime SessionStarted { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets when the token was last refreshed
        /// </summary>
        public DateTime? LastRefreshed { get; set; }
        
        /// <summary>
        /// Gets or sets whether the session is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the user's display name
        /// </summary>
        public string? UserName { get; set; }
        
        /// <summary>
        /// Gets or sets the user's email address
        /// </summary>
        public string? UserEmail { get; set; }
        
        /// <summary>
        /// Gets or sets the authentication scope or permissions
        /// </summary>
        public string? Scope { get; set; }
        
        /// <summary>
        /// Gets or sets the Rhino instance ID for multi-instance support
        /// </summary>
        public string? InstanceId { get; set; }
        
        /// <summary>
        /// Gets or sets the current authentication state
        /// </summary>
        public AuthenticationState State { get; set; }
        
        /// <summary>
        /// Gets or sets the device authorization code for OAuth device flow
        /// </summary>
        public string? DeviceCode { get; set; }
        
        /// <summary>
        /// Gets or sets the user verification code for OAuth device flow
        /// </summary>
        public string? UserCode { get; set; }
        
        /// <summary>
        /// Gets or sets the verification URI for OAuth device flow
        /// </summary>
        public string? VerificationUri { get; set; }
        
        /// <summary>
        /// Gets or sets the complete verification URI with user code
        /// </summary>
        public string? VerificationUriComplete { get; set; }
        
        /// <summary>
        /// Gets or sets when the device authorization expires
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
        
        /// <summary>
        /// Gets or sets the user information
        /// </summary>
        public UserInfo? UserInfo { get; set; }
        
        /// <summary>
        /// Gets or sets the user's display name (alias for UserName)
        /// </summary>
        public string? UserDisplayName 
        { 
            get => UserName; 
            set => UserName = value; 
        }
        
        #endregion
        
        #region Computed Properties
        
        /// <summary>
        /// Gets whether the access token is currently valid (not expired)
        /// </summary>
        public bool IsTokenValid => IsActive && 
                                    !string.IsNullOrWhiteSpace(AccessToken) && 
                                    TokenExpiry > DateTime.UtcNow;
        
        /// <summary>
        /// Gets whether the token is expired but the session can be refreshed
        /// </summary>
        public bool CanRefresh => IsActive && 
                                  !string.IsNullOrWhiteSpace(RefreshToken) && 
                                  TokenExpiry <= DateTime.UtcNow;
        
        /// <summary>
        /// Gets whether the session needs authentication (no valid token or refresh capability)
        /// </summary>
        public bool NeedsAuthentication => !IsActive || 
                                           (string.IsNullOrWhiteSpace(AccessToken) && 
                                            string.IsNullOrWhiteSpace(RefreshToken));
        
        /// <summary>
        /// Gets the time remaining until token expiry
        /// </summary>
        public TimeSpan TimeUntilExpiry => TokenExpiry > DateTime.UtcNow ? 
                                           TokenExpiry - DateTime.UtcNow : 
                                           TimeSpan.Zero;
        
        /// <summary>
        /// Gets the duration of the current session
        /// </summary>
        public TimeSpan SessionDuration => DateTime.UtcNow - SessionStarted;
        
        /// <summary>
        /// Gets whether the token is approaching expiry (within 5 minutes)
        /// </summary>
        public bool IsApproachingExpiry => IsTokenValid && TimeUntilExpiry <= TimeSpan.FromMinutes(5);
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates the authentication session for required fields and logical consistency
        /// </summary>
        /// <returns>True if session is valid, false otherwise</returns>
        public bool IsValid()
        {
            // Check required fields
            if (string.IsNullOrWhiteSpace(UserId))
                return false;
            
            // Active sessions must have an access token
            if (IsActive && string.IsNullOrWhiteSpace(AccessToken))
                return false;
            
            // Token expiry must be a future date for active sessions
            if (IsActive && TokenExpiry <= DateTime.UtcNow.AddMinutes(-1))
                return false;
            
            // Session started cannot be in the future
            if (SessionStarted > DateTime.UtcNow.AddMinutes(1))
                return false;
            
            // Last refreshed cannot be before session started
            if (LastRefreshed.HasValue && LastRefreshed.Value < SessionStarted)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Gets validation errors for this authentication session
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public System.Collections.Generic.List<string> GetValidationErrors()
        {
            var errors = new System.Collections.Generic.List<string>();
            
            if (string.IsNullOrWhiteSpace(UserId))
                errors.Add("UserId cannot be empty");
            
            if (IsActive && string.IsNullOrWhiteSpace(AccessToken))
                errors.Add("Active sessions must have an access token");
            
            if (IsActive && TokenExpiry <= DateTime.UtcNow.AddMinutes(-1))
                errors.Add("Active sessions must have future token expiry");
            
            if (SessionStarted > DateTime.UtcNow.AddMinutes(1))
                errors.Add("Session started time cannot be in the future");
            
            if (LastRefreshed.HasValue && LastRefreshed.Value < SessionStarted)
                errors.Add("Last refreshed time cannot be before session started");
                
            return errors;
        }
        
        #endregion
        
        #region Session Management
        
        /// <summary>
        /// Updates the session with new token information
        /// </summary>
        /// <param name="accessToken">The new access token</param>
        /// <param name="expiresIn">Seconds until token expiry</param>
        /// <param name="refreshToken">The new refresh token (optional)</param>
        public void UpdateTokens(string accessToken, int expiresIn, string? refreshToken = null)
        {
            AccessToken = accessToken;
            TokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);
            LastRefreshed = DateTime.UtcNow;
            IsActive = true;
            
            if (!string.IsNullOrWhiteSpace(refreshToken))
                RefreshToken = refreshToken;
        }
        
        /// <summary>
        /// Updates the session with new token information using expiry datetime
        /// </summary>
        /// <param name="accessToken">The new access token</param>
        /// <param name="tokenExpiry">When the token expires</param>
        /// <param name="refreshToken">The new refresh token (optional)</param>
        public void UpdateTokens(string accessToken, DateTime tokenExpiry, string? refreshToken = null)
        {
            AccessToken = accessToken;
            TokenExpiry = tokenExpiry;
            LastRefreshed = DateTime.UtcNow;
            IsActive = true;
            
            if (!string.IsNullOrWhiteSpace(refreshToken))
                RefreshToken = refreshToken;
        }
        
        /// <summary>
        /// Marks the session as expired
        /// </summary>
        public void MarkAsExpired()
        {
            IsActive = false;
            AccessToken = null;
            // Keep refresh token for potential refresh
        }
        
        /// <summary>
        /// Ends the session and clears all tokens
        /// </summary>
        public void EndSession()
        {
            IsActive = false;
            AccessToken = null;
            RefreshToken = null;
        }
        
        /// <summary>
        /// Refreshes the session start time (for session extension)
        /// </summary>
        public void ExtendSession()
        {
            SessionStarted = DateTime.UtcNow;
            IsActive = true;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Gets the current authentication state
        /// </summary>
        /// <returns>Current authentication state</returns>
        public AuthenticationState GetAuthenticationState()
        {
            if (NeedsAuthentication)
                return AuthenticationState.NotAuthenticated;
            else if (IsTokenValid)
                return AuthenticationState.Authenticated;
            else if (CanRefresh)
                return AuthenticationState.Expired;
            else
                return AuthenticationState.Failed;
        }
        
        /// <summary>
        /// Gets a human-readable status description
        /// </summary>
        /// <returns>Status description</returns>
        public string GetStatusDescription()
        {
            return GetAuthenticationState() switch
            {
                AuthenticationState.NotAuthenticated => "Not authenticated",
                AuthenticationState.Authenticated => $"Authenticated as {UserName ?? UserId}",
                AuthenticationState.Expired => "Token expired, refresh needed",
                AuthenticationState.Failed => "Authentication failed",
                AuthenticationState.Pending => "Authentication in progress",
                _ => "Unknown status"
            };
        }
        
        /// <summary>
        /// Gets token expiry information as a formatted string
        /// </summary>
        /// <returns>Human-readable token expiry info</returns>
        public string GetTokenExpiryDisplay()
        {
            if (!IsTokenValid)
                return "Token expired or invalid";
                
            var timeRemaining = TimeUntilExpiry;
            if (timeRemaining.TotalDays >= 1)
                return $"Expires in {(int)timeRemaining.TotalDays} day(s)";
            else if (timeRemaining.TotalHours >= 1)
                return $"Expires in {(int)timeRemaining.TotalHours} hour(s)";
            else if (timeRemaining.TotalMinutes >= 1)
                return $"Expires in {(int)timeRemaining.TotalMinutes} minute(s)";
            else
                return "Expires soon";
        }
        
        /// <summary>
        /// Gets session duration as a formatted string
        /// </summary>
        /// <returns>Human-readable session duration</returns>
        public string GetSessionDurationDisplay()
        {
            var duration = SessionDuration;
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays} day(s)";
            else if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours} hour(s)";
            else if (duration.TotalMinutes >= 1)
                return $"{(int)duration.TotalMinutes} minute(s)";
            else
                return "Just started";
        }
        
        /// <summary>
        /// Creates a summary string of the authentication session
        /// </summary>
        /// <returns>Human-readable session summary</returns>
        public string GetSummary()
        {
            return $"{GetStatusDescription()} - Session: {GetSessionDurationDisplay()}, {GetTokenExpiryDisplay()}";
        }
        
        /// <summary>
        /// Creates a safe copy of the session without sensitive data
        /// </summary>
        /// <returns>Session copy without tokens</returns>
        public AuthenticationSession GetSafeCopy()
        {
            return new AuthenticationSession
            {
                UserId = UserId,
                TokenExpiry = TokenExpiry,
                SessionStarted = SessionStarted,
                LastRefreshed = LastRefreshed,
                IsActive = IsActive,
                UserName = UserName,
                UserEmail = UserEmail,
                Scope = Scope,
                InstanceId = InstanceId,
                // Exclude sensitive tokens
                AccessToken = null,
                RefreshToken = null
            };
        }
        
        #endregion
        
        #region Equality
        
        /// <summary>
        /// Determines whether the specified object is equal to this session
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object? obj)
        {
            if (obj is AuthenticationSession other)
                return string.Equals(UserId, other.UserId, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(InstanceId, other.InstanceId, StringComparison.OrdinalIgnoreCase);
            return false;
        }
        
        /// <summary>
        /// Returns a hash code for this session
        /// </summary>
        /// <returns>Hash code based on user ID and instance ID</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(
                UserId?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0,
                InstanceId?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0);
        }
        
        /// <summary>
        /// Returns a string representation of the session
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return GetSummary();
        }
        
        #endregion
    }
}