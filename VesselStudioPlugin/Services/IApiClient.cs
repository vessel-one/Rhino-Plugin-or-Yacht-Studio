using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VesselStudioPlugin.Services
{
    /// <summary>
    /// Interface for HTTP client service that handles communication with Vessel Studio API
    /// </summary>
    public interface IApiClient : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the client is authenticated
        /// </summary>
        bool IsAuthenticated { get; }
        
        /// <summary>
        /// Gets or sets the base URL for the Vessel Studio API
        /// </summary>
        string BaseUrl { get; set; }
        
        /// <summary>
        /// Sets the authentication token for API requests
        /// </summary>
        /// <param name="token">The JWT token for authentication</param>
        void SetAuthenticationToken(string token);
        
        /// <summary>
        /// Clears the authentication token
        /// </summary>
        void ClearAuthenticationToken();
        
        /// <summary>
        /// Sends a GET request to the specified endpoint
        /// </summary>
        /// <param name="endpoint">The API endpoint to call</param>
        /// <param name="cancellationToken">Cancellation token for the request</param>
        /// <returns>The response content as a string</returns>
        Task<string> GetAsync(string endpoint, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Sends a POST request to the specified endpoint with JSON content
        /// </summary>
        /// <param name="endpoint">The API endpoint to call</param>
        /// <param name="jsonContent">The JSON content to send</param>
        /// <param name="cancellationToken">Cancellation token for the request</param>
        /// <returns>The response content as a string</returns>
        Task<string> PostJsonAsync(string endpoint, string jsonContent, CancellationToken cancellationToken = default);
        
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
        Task<string> UploadFileAsync(
            string endpoint, 
            byte[] fileContent, 
            string fileName,
            Dictionary<string, string>? additionalFields = null,
            IProgress<int>? progressCallback = null,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if the network connection is available
        /// </summary>
        /// <returns>True if network is available, false otherwise</returns>
        Task<bool> IsNetworkAvailableAsync();
        
        /// <summary>
        /// Gets the current network connectivity status
        /// </summary>
        /// <returns>Network status information</returns>
        Task<NetworkStatus> GetNetworkStatusAsync();
        
        /// <summary>
        /// Uploads a screenshot to the specified project
        /// </summary>
        /// <param name="projectId">The target project ID</param>
        /// <param name="screenshot">The screenshot to upload</param>
        /// <param name="progress">Progress callback for upload tracking</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The upload transaction result</returns>
        Task<Models.UploadTransaction?> UploadScreenshotAsync(
            string projectId,
            Models.ViewportScreenshot screenshot, 
            IProgress<Models.UploadProgressInfo>? progress = null,
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Tests connectivity to the Vessel Studio API
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>True if connectivity is successful, false otherwise</returns>
        Task<bool> TestConnectivityAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the list of projects accessible to the authenticated user
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>List of accessible projects</returns>
        Task<IEnumerable<Models.ProjectInfo>?> GetProjectsAsync(CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// Represents network connectivity status
    /// </summary>
    public class NetworkStatus
    {
        public bool IsConnected { get; set; }
        public bool CanReachServer { get; set; }
        public TimeSpan? Latency { get; set; }
        public string? ErrorMessage { get; set; }
    }
}