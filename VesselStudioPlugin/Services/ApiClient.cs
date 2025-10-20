using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VesselStudioPlugin.Models;

namespace VesselStudioPlugin.Services
{
    /// <summary>
    /// HTTP API client for Vessel Studio integration
    /// </summary>
    public class ApiClient : IApiClient, IDisposable
    {
        #region Fields and Properties
        
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;
        
        private volatile bool _disposed = false;
        
        public event EventHandler<NetworkStatusChangedEventArgs>? NetworkStatusChanged;
        public event EventHandler<ApiErrorEventArgs>? ApiError;
        public event EventHandler<RateLimitEventArgs>? RateLimitReached;
        
        public bool IsConnected { get; private set; } = true;
        
        // Interface properties
        public bool IsAuthenticated => _authService.CurrentToken != null;
        public string BaseUrl { get; set; }
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of ApiClient
        /// </summary>
        /// <param name="httpClient">HTTP client instance</param>
        /// <param name="authService">Authentication service</param>
        /// <param name="baseUrl">Base URL for the API</param>
        public ApiClient(HttpClient httpClient, IAuthService authService, string baseUrl)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
            BaseUrl = _baseUrl;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
            
            // Configure HTTP client
            ConfigureHttpClient();
        }
        
        #endregion
        
        #region Interface Implementation
        
        /// <summary>
        /// Sets the authentication token for API requests
        /// </summary>
        /// <param name="token">The JWT token for authentication</param>
        public void SetAuthenticationToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));
                
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        
        /// <summary>
        /// Clears the authentication token
        /// </summary>
        public void ClearAuthenticationToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        
        /// <summary>
        /// Sends a GET request to the specified endpoint
        /// </summary>
        /// <param name="endpoint">The API endpoint to call</param>
        /// <param name="cancellationToken">Cancellation token for the request</param>
        /// <returns>The response content as a string</returns>
        public async Task<string> GetAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        
        /// <summary>
        /// Sends a POST request to the specified endpoint with JSON content
        /// </summary>
        /// <param name="endpoint">The API endpoint to call</param>
        /// <param name="jsonContent">The JSON content to send</param>
        /// <param name="cancellationToken">Cancellation token for the request</param>
        /// <returns>The response content as a string</returns>
        public async Task<string> PostJsonAsync(string endpoint, string jsonContent, CancellationToken cancellationToken = default)
        {
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        
        /// <summary>
        /// Uploads a file using multipart form data
        /// </summary>
        /// <param name="endpoint">The API endpoint to call</param>
        /// <param name="fileContent">The file content to upload</param>
        /// <param name="fileName">The name of the file</param>
        /// <param name="additionalFields">Additional form fields to include</param>
        /// <param name="progressCallback">Callback to report upload progress (0-100)</param>
        /// <param name="cancellationToken">Cancellation token for the request</param>
        /// <returns>The response content as a string</returns>
        public async Task<string> UploadFileAsync(
            string endpoint, 
            byte[] fileContent, 
            string fileName,
            Dictionary<string, string>? additionalFields = null,
            IProgress<int>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            using var form = new MultipartFormDataContent();
            using var fileStream = new MemoryStream(fileContent);
            using var streamContent = new StreamContent(fileStream);
            
            form.Add(streamContent, "file", fileName);
            
            if (additionalFields != null)
            {
                foreach (var field in additionalFields)
                {
                    form.Add(new StringContent(field.Value), field.Key);
                }
            }
            
            var response = await _httpClient.PostAsync($"{BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}", form, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        
        /// <summary>
        /// Checks if the network connection is available
        /// </summary>
        /// <returns>True if network is available, false otherwise</returns>
        public async Task<bool> IsNetworkAvailableAsync()
        {
            try
            {
                var status = await GetNetworkStatusAsync();
                return status.IsConnected && status.CanReachServer;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets the current network connectivity status
        /// </summary>
        /// <returns>Network status information</returns>
        public async Task<NetworkStatus> GetNetworkStatusAsync()
        {
            var status = new NetworkStatus
            {
                IsConnected = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()
            };
            
            if (!status.IsConnected)
            {
                status.ErrorMessage = "No network connection available";
                return status;
            }
            
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                using var response = await _httpClient.GetAsync($"{BaseUrl}/health", HttpCompletionOption.ResponseHeadersRead);
                stopwatch.Stop();
                
                status.CanReachServer = response.IsSuccessStatusCode;
                status.Latency = stopwatch.Elapsed;
            }
            catch (Exception ex)
            {
                status.CanReachServer = false;
                status.ErrorMessage = ex.Message;
            }
            
            return status;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Gets a list of projects for the authenticated user
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of projects</returns>
        public async Task<IEnumerable<ProjectInfo>?> GetProjectsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await SendAuthenticatedRequestAsync<List<ProjectInfo>>(
                    HttpMethod.Get, 
                    "/api/projects", 
                    cancellationToken: cancellationToken);
                
                return response;
            }
            catch (Exception ex)
            {
                OnApiError($"Failed to get projects: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets details for a specific project
        /// </summary>
        /// <param name="projectId">Project ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Project information</returns>
        public async Task<ProjectInfo?> GetProjectAsync(string projectId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("Project ID cannot be null or empty", nameof(projectId));
            
            try
            {
                var response = await SendAuthenticatedRequestAsync<ProjectInfo>(
                    HttpMethod.Get, 
                    $"/api/projects/{projectId}", 
                    cancellationToken: cancellationToken);
                
                return response;
            }
            catch (Exception ex)
            {
                OnApiError($"Failed to get project {projectId}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Uploads a screenshot to a project
        /// </summary>
        /// <param name="projectId">Target project ID</param>
        /// <param name="screenshot">Screenshot to upload</param>
        /// <param name="progress">Progress callback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Upload transaction information</returns>
        public async Task<UploadTransaction?> UploadScreenshotAsync(
            string projectId, 
            ViewportScreenshot screenshot, 
            IProgress<UploadProgressInfo>? progress = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("Project ID cannot be null or empty", nameof(projectId));
            
            if (screenshot?.ImageData == null)
                throw new ArgumentException("Screenshot or image data cannot be null", nameof(screenshot));
            
            try
            {
                var transaction = new UploadTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    ProjectId = projectId,
                    ScreenshotId = screenshot.Id,
                    StartedAt = DateTime.UtcNow,
                    State = UploadState.Uploading,
                    TotalBytes = screenshot.ImageData.Length
                };
                
                progress?.Report(new UploadProgressInfo
                {
                    TransactionId = transaction.Id,
                    BytesUploaded = 0,
                    TotalBytes = transaction.TotalBytes,
                    Percentage = 0,
                    Message = "Starting upload..."
                });
                
                // Create multipart form data
                using var form = new MultipartFormDataContent();
                
                // Add metadata as JSON
                var metadataJson = JsonSerializer.Serialize(screenshot.Metadata, _jsonOptions);
                form.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"), "metadata");
                
                // Add screenshot properties
                form.Add(new StringContent(screenshot.Id), "screenshotId");
                form.Add(new StringContent(screenshot.CompressionType.ToString()), "compressionType");
                form.Add(new StringContent(screenshot.Quality.ToString()), "quality");
                
                // Add image file
                var imageContent = new ByteArrayContent(screenshot.ImageData);
                imageContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(GetContentType(screenshot));
                form.Add(imageContent, "image", screenshot.Filename);
                
                // Create progress-tracking content
                var progressContent = new ProgressableStreamContent(form, progress, transaction.Id, transaction.TotalBytes);
                
                var response = await SendAuthenticatedRequestAsync<UploadResponse>(
                    HttpMethod.Post,
                    $"/api/projects/{projectId}/screenshots",
                    content: progressContent,
                    cancellationToken: cancellationToken);
                
                if (response != null)
                {
                    transaction.State = UploadState.Completed;
                    transaction.CompletedAt = DateTime.UtcNow;
                    transaction.UploadedBytes = transaction.TotalBytes;
                    transaction.ServerImageId = response.ImageId;
                    transaction.ServerUrl = response.ImageUrl;
                    
                    progress?.Report(new UploadProgressInfo
                    {
                        TransactionId = transaction.Id,
                        BytesUploaded = transaction.TotalBytes,
                        TotalBytes = transaction.TotalBytes,
                        Percentage = 100,
                        Message = "Upload completed successfully"
                    });
                }
                else
                {
                    transaction.State = UploadState.Failed;
                    transaction.ErrorMessage = "Upload failed - no response from server";
                }
                
                return transaction;
            }
            catch (Exception ex)
            {
                OnApiError($"Failed to upload screenshot: {ex.Message}");
                return new UploadTransaction
                {
                    Id = Guid.NewGuid().ToString(),
                    ProjectId = projectId,
                    ScreenshotId = screenshot.Id,
                    StartedAt = DateTime.UtcNow,
                    State = UploadState.Failed,
                    ErrorMessage = ex.Message,
                    TotalBytes = screenshot.ImageData?.Length ?? 0
                };
            }
        }
        
        /// <summary>
        /// Gets upload status for a transaction
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Upload status information</returns>
        public async Task<UploadStatusInfo?> GetUploadStatusAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentException("Transaction ID cannot be null or empty", nameof(transactionId));
            
            try
            {
                var response = await SendAuthenticatedRequestAsync<UploadStatusInfo>(
                    HttpMethod.Get,
                    $"/api/uploads/{transactionId}/status",
                    cancellationToken: cancellationToken);
                
                return response;
            }
            catch (Exception ex)
            {
                OnApiError($"Failed to get upload status: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Tests connectivity to the API server
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if connected, false otherwise</returns>
        public async Task<bool> TestConnectivityAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/health");
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                
                var isConnected = response.IsSuccessStatusCode;
                UpdateNetworkStatus(isConnected);
                
                return isConnected;
            }
            catch (Exception ex)
            {
                UpdateNetworkStatus(false);
                OnApiError($"Connectivity test failed: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Private HTTP Methods
        
        /// <summary>
        /// Sends an authenticated HTTP request
        /// </summary>
        /// <typeparam name="T">Response type</typeparam>
        /// <param name="method">HTTP method</param>
        /// <param name="endpoint">API endpoint</param>
        /// <param name="content">Request content</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Deserialized response</returns>
        private async Task<T?> SendAuthenticatedRequestAsync<T>(
            HttpMethod method, 
            string endpoint, 
            HttpContent? content = null, 
            CancellationToken cancellationToken = default) where T : class
        {
            // Get access token
            var accessToken = await _authService.GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new UnauthorizedAccessException("No valid access token available");
            }
            
            // Create request
            using var request = new HttpRequestMessage(method, $"{_baseUrl}{endpoint}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            
            if (content != null)
            {
                request.Content = content;
            }
            
            // Send request with retry logic
            return await SendRequestWithRetryAsync<T>(request, cancellationToken);
        }
        
        /// <summary>
        /// Sends HTTP request with automatic retry logic
        /// </summary>
        /// <typeparam name="T">Response type</typeparam>
        /// <param name="request">HTTP request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="maxRetries">Maximum retry attempts</param>
        /// <returns>Deserialized response</returns>
        private async Task<T?> SendRequestWithRetryAsync<T>(
            HttpRequestMessage request, 
            CancellationToken cancellationToken, 
            int maxRetries = 3) where T : class
        {
            Exception? lastException = null;
            
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Clone request for retry attempts
                    using var requestClone = await CloneRequestAsync(request);
                    using var response = await _httpClient.SendAsync(requestClone, cancellationToken);
                    
                    // Handle rate limiting
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var retryAfter = GetRetryAfterDelay(response);
                        OnRateLimitReached(retryAfter);
                        
                        if (attempt < maxRetries)
                        {
                            await Task.Delay(retryAfter, cancellationToken);
                            continue;
                        }
                    }
                    
                    // Handle authentication errors
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        // Try to refresh token once
                        if (attempt == 0)
                        {
                            var refreshed = await _authService.RefreshTokenAsync(cancellationToken);
                            if (refreshed)
                            {
                                // Update authorization header and retry
                                var newToken = await _authService.GetAccessTokenAsync(cancellationToken);
                                if (!string.IsNullOrEmpty(newToken))
                                {
                                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);
                                    continue;
                                }
                            }
                        }
                        
                        throw new UnauthorizedAccessException("Authentication failed");
                    }
                    
                    // Handle other HTTP errors
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.StatusCode}: {errorContent}");
                    }
                    
                    // Parse successful response
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    
                    if (string.IsNullOrWhiteSpace(responseContent))
                    {
                        return default(T);
                    }
                    
                    return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                }
                catch (HttpRequestException ex) when (IsNetworkError(ex))
                {
                    lastException = ex;
                    UpdateNetworkStatus(false);
                    
                    if (attempt < maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                }
                catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
                {
                    lastException = ex;
                    
                    if (attempt < maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    break; // Don't retry for other exceptions
                }
            }
            
            // All retries failed
            if (lastException != null)
            {
                throw lastException;
            }
            
            return default(T);
        }
        
        /// <summary>
        /// Clones an HTTP request for retry attempts
        /// </summary>
        /// <param name="original">Original request</param>
        /// <returns>Cloned request</returns>
        private async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
        {
            var clone = new HttpRequestMessage(original.Method, original.RequestUri);
            
            // Copy headers
            foreach (var header in original.Headers)
            {
                clone.Headers.Add(header.Key, header.Value);
            }
            
            // Copy content if present
            if (original.Content != null)
            {
                var contentBytes = await original.Content.ReadAsByteArrayAsync();
                clone.Content = new ByteArrayContent(contentBytes);
                
                foreach (var header in original.Content.Headers)
                {
                    clone.Content.Headers.Add(header.Key, header.Value);
                }
            }
            
            return clone;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Configures the HTTP client with default settings
        /// </summary>
        private void ConfigureHttpClient()
        {
            _httpClient.Timeout = TimeSpan.FromMinutes(5); // 5 minute timeout for uploads
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "VesselStudioPlugin/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }
        
        /// <summary>
        /// Gets the content type for a screenshot
        /// </summary>
        /// <param name="screenshot">Screenshot</param>
        /// <returns>MIME content type</returns>
        private string GetContentType(ViewportScreenshot screenshot)
        {
            return screenshot.CompressionType switch
            {
                CompressionType.Jpeg => "image/jpeg",
                CompressionType.Png => "image/png",
                _ => "application/octet-stream"
            };
        }
        
        /// <summary>
        /// Gets retry delay from HTTP response headers
        /// </summary>
        /// <param name="response">HTTP response</param>
        /// <returns>Retry delay</returns>
        private TimeSpan GetRetryAfterDelay(HttpResponseMessage response)
        {
            if (response.Headers.RetryAfter?.Delta.HasValue == true)
            {
                return response.Headers.RetryAfter.Delta.Value;
            }
            
            if (response.Headers.RetryAfter?.Date.HasValue == true)
            {
                return response.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
            }
            
            return TimeSpan.FromSeconds(60); // Default 1 minute
        }
        
        /// <summary>
        /// Determines if an exception is a network error
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <returns>True if network error</returns>
        private bool IsNetworkError(Exception ex)
        {
            return ex is HttpRequestException ||
                   ex is SocketException ||
                   ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Updates network connectivity status
        /// </summary>
        /// <param name="isConnected">Connection status</param>
        private void UpdateNetworkStatus(bool isConnected)
        {
            if (IsConnected != isConnected)
            {
                IsConnected = isConnected;
                NetworkStatusChanged?.Invoke(this, new NetworkStatusChangedEventArgs(isConnected));
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnApiError(string message)
        {
            ApiError?.Invoke(this, new ApiErrorEventArgs(message));
        }
        
        private void OnRateLimitReached(TimeSpan retryAfter)
        {
            RateLimitReached?.Invoke(this, new RateLimitEventArgs(retryAfter));
        }
        
        #endregion
        
        #region Disposal
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Note: Don't dispose _httpClient as it may be shared
                _disposed = true;
            }
        }
        
        #endregion
    }
    
    #region Response Models
    
    /// <summary>
    /// Response from screenshot upload
    /// </summary>
    internal class UploadResponse
    {
        public string ImageId { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }
    
    #endregion
    
    #region Progress Tracking
    
    /// <summary>
    /// HTTP content wrapper that reports upload progress
    /// </summary>
    internal class ProgressableStreamContent : HttpContent
    {
        private readonly HttpContent _innerContent;
        private readonly IProgress<UploadProgressInfo>? _progress;
        private readonly string _transactionId;
        private readonly long _totalBytes;
        
        public ProgressableStreamContent(
            HttpContent innerContent, 
            IProgress<UploadProgressInfo>? progress, 
            string transactionId, 
            long totalBytes)
        {
            _innerContent = innerContent;
            _progress = progress;
            _transactionId = transactionId;
            _totalBytes = totalBytes;
            
            // Copy headers
            foreach (var header in innerContent.Headers)
            {
                Headers.Add(header.Key, header.Value);
            }
        }
        
        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            var buffer = await _innerContent.ReadAsByteArrayAsync();
            
            if (_progress != null)
            {
                using var progressStream = new ProgressReportingStream(stream, _progress, _transactionId, _totalBytes);
                await progressStream.WriteAsync(buffer, 0, buffer.Length);
            }
            else
            {
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
        }
        
        protected override bool TryComputeLength(out long length)
        {
            length = _innerContent.Headers.ContentLength ?? 0;
            return _innerContent.Headers.ContentLength.HasValue;
        }
    }
    
    /// <summary>
    /// Stream wrapper that reports progress
    /// </summary>
    internal class ProgressReportingStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly IProgress<UploadProgressInfo> _progress;
        private readonly string _transactionId;
        private readonly long _totalBytes;
        private long _bytesWritten = 0;
        
        public ProgressReportingStream(
            Stream innerStream, 
            IProgress<UploadProgressInfo> progress, 
            string transactionId, 
            long totalBytes)
        {
            _innerStream = innerStream;
            _progress = progress;
            _transactionId = transactionId;
            _totalBytes = totalBytes;
        }
        
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
            
            _bytesWritten += count;
            var percentage = _totalBytes > 0 ? (int)((_bytesWritten * 100) / _totalBytes) : 0;
            
            _progress.Report(new UploadProgressInfo
            {
                TransactionId = _transactionId,
                BytesUploaded = _bytesWritten,
                TotalBytes = _totalBytes,
                Percentage = percentage,
                Message = $"Uploading... {percentage}%"
            });
        }
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
            
            _bytesWritten += count;
            var percentage = _totalBytes > 0 ? (int)((_bytesWritten * 100) / _totalBytes) : 0;
            
            _progress.Report(new UploadProgressInfo
            {
                TransactionId = _transactionId,
                BytesUploaded = _bytesWritten,
                TotalBytes = _totalBytes,
                Percentage = percentage,
                Message = $"Uploading... {percentage}%"
            });
        }
        
        // Required Stream overrides
        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        
        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }
        
        public override void Flush() => _innerStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
    
    #endregion
}