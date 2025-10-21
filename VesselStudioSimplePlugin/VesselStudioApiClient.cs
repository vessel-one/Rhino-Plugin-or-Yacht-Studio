using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rhino;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Represents a Vessel One project
    /// </summary>
    public class VesselProject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string ThumbnailUrl { get; set; }
    }

    /// <summary>
    /// API client for Vessel One integration
    /// </summary>
    public class VesselStudioApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://vesselstudio.io/api";
        private string _apiKey;

        public VesselStudioApiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // Load saved API key
            LoadApiKey();
        }

        /// <summary>
        /// Set the API key for authentication
        /// </summary>
        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", apiKey);
                
            // Save to Rhino settings
            SaveApiKey(apiKey);
        }

        /// <summary>
        /// Check if authenticated
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(_apiKey);

        /// <summary>
        /// Validate API key with Vessel One backend
        /// </summary>
        public async Task<ValidationResult> ValidateApiKeyAsync()
        {
            if (!IsAuthenticated)
            {
                return new ValidationResult
                {
                    Success = false,
                    ErrorMessage = "No API key set",
                    ErrorDetails = "Authentication required"
                };
            }

            try
            {
                RhinoApp.WriteLine($"Attempting connection to: {BaseUrl}/rhino/validate");
                RhinoApp.WriteLine($"Authorization: Bearer {_apiKey.Substring(0, Math.Min(10, _apiKey.Length))}...");
                
                var response = await _httpClient.PostAsync("/rhino/validate", null);
                
                RhinoApp.WriteLine($"Response status: {(int)response.StatusCode} {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    RhinoApp.WriteLine($"Response body: {json}");
                    
                    var result = JsonConvert.DeserializeObject<dynamic>(json);
                    return new ValidationResult
                    {
                        Success = true,
                        UserName = result.userName?.ToString(),
                        ErrorMessage = null,
                        ErrorDetails = null
                    };
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    RhinoApp.WriteLine($"Error response body: {errorBody}");
                    
                    return new ValidationResult
                    {
                        Success = false,
                        ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                        ErrorDetails = errorBody
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                RhinoApp.WriteLine($"HTTP Request Exception: {ex.Message}");
                if (ex.InnerException != null)
                {
                    RhinoApp.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                return new ValidationResult
                {
                    Success = false,
                    ErrorMessage = "Connection failed",
                    ErrorDetails = $"Could not reach {BaseUrl}\n{ex.Message}"
                };
            }
            catch (TaskCanceledException ex)
            {
                RhinoApp.WriteLine($"Request timeout: {ex.Message}");
                
                return new ValidationResult
                {
                    Success = false,
                    ErrorMessage = "Request timeout",
                    ErrorDetails = $"Server did not respond within 30 seconds\n{ex.Message}"
                };
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Unexpected error: {ex.GetType().Name}");
                RhinoApp.WriteLine($"Message: {ex.Message}");
                RhinoApp.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return new ValidationResult
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error: {ex.GetType().Name}",
                    ErrorDetails = ex.Message
                };
            }
        }

        /// <summary>
        /// Get list of user's projects from Vessel One
        /// </summary>
        public async Task<List<VesselProject>> GetProjectsAsync()
        {
            if (!IsAuthenticated)
            {
                return new List<VesselProject>();
            }

            try
            {
                var response = await _httpClient.GetAsync("/rhino/projects");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(json);
                    var projects = new List<VesselProject>();
                    
                    foreach (var proj in result.projects)
                    {
                        projects.Add(new VesselProject
                        {
                            Id = proj.id,
                            Name = proj.name,
                            Type = proj.type ?? "yacht",
                            ThumbnailUrl = proj.thumbnailUrl
                        });
                    }
                    
                    return projects;
                }
                return new List<VesselProject>();
            }
            catch
            {
                return new List<VesselProject>();
            }
        }

        /// <summary>
        /// Upload a screenshot to a specific Vessel One project gallery
        /// </summary>
        public async Task<UploadResult> UploadScreenshotAsync(
            string projectId,
            byte[] imageBytes,
            string imageName,
            Dictionary<string, object> metadata)
        {
            if (!IsAuthenticated)
            {
                return new UploadResult 
                { 
                    Success = false, 
                    Message = "Not authenticated. Please set API key first." 
                };
            }

            try
            {
                // Create multipart form data
                using (var content = new MultipartFormDataContent())
                {
                    // Add image
                    var imageContent = new ByteArrayContent(imageBytes);
                    imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                    content.Add(imageContent, "image", "capture.png");

                    // Add image name
                    content.Add(new StringContent(imageName), "name");

                    // Add metadata
                    var metadataJson = JsonConvert.SerializeObject(metadata);
                    content.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"), "metadata");

                    // Upload to specific project
                    var response = await _httpClient.PostAsync($"/rhino/projects/{projectId}/upload", content);
                    var responseText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(responseText);
                        return new UploadResult 
                        { 
                            Success = true, 
                            Message = result.message?.ToString() ?? "Uploaded successfully",
                            Url = result.imageUrl?.ToString() ?? ""
                        };
                    }
                    else
                    {
                        return new UploadResult 
                        { 
                            Success = false, 
                            Message = $"Upload failed: {response.StatusCode} - {responseText}" 
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new UploadResult 
                { 
                    Success = false, 
                    Message = $"Upload error: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// Legacy upload method for backward compatibility
        /// </summary>
        [Obsolete("Use UploadScreenshotAsync(projectId, imageBytes, imageName, metadata) instead")]
        public async Task<UploadResult> UploadScreenshotAsync(Bitmap screenshot, string projectId = null)
        {
            // Convert bitmap to bytes
            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                screenshot.Save(ms, ImageFormat.Png);
                imageBytes = ms.ToArray();
            }

            // Create basic metadata
            var metadata = new Dictionary<string, object>
            {
                { "source", "rhino-plugin-simple" },
                { "rhinoVersion", RhinoApp.Version.ToString() },
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "width", screenshot.Width },
                { "height", screenshot.Height }
            };

            var imageName = $"Rhino Capture {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            
            return await UploadScreenshotAsync(
                projectId ?? "default",
                imageBytes,
                imageName,
                metadata
            );
        }

        /// <summary>
        /// Test connection to Vessel Studio API
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/plugin/ping");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private void LoadApiKey()
        {
            try
            {
                // Use Windows Registry or simple file-based storage for simplicity
                var savedKey = Environment.GetEnvironmentVariable("VESSEL_STUDIO_API_KEY", EnvironmentVariableTarget.User);
                if (!string.IsNullOrEmpty(savedKey))
                {
                    SetApiKey(savedKey);
                }
            }
            catch
            {
                // Ignore settings load errors
            }
        }

        private void SaveApiKey(string apiKey)
        {
            try
            {
                // Use Windows Registry or simple environment variable for simplicity
                Environment.SetEnvironmentVariable("VESSEL_STUDIO_API_KEY", apiKey, EnvironmentVariableTarget.User);
            }
            catch
            {
                // Ignore settings save errors
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Result of an upload operation
    /// </summary>
    public class UploadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Url { get; set; }
    }

    /// <summary>
    /// Result of API key validation with detailed error information
    /// </summary>
    public class ValidationResult
    {
        public bool Success { get; set; }
        public string UserName { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDetails { get; set; }
    }
}