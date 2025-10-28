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
    /// API error response for subscription issues
    /// </summary>
    public class ApiErrorResponse
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string UserMessage { get; set; }
        public bool RequiresUpgrade { get; set; }
        public string CurrentPlan { get; set; }
        public string[] RequiredPlans { get; set; }
        public string UpgradeUrl { get; set; }
    }

    /// <summary>
    /// API client for Vessel One integration
    /// </summary>
    public class VesselStudioApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://vesselstudio.io";
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
                RhinoApp.WriteLine($"Attempting connection to: {BaseUrl}/api/rhino/validate");
                RhinoApp.WriteLine($"Authorization: Bearer {_apiKey.Substring(0, Math.Min(10, _apiKey.Length))}...");
                
                var response = await _httpClient.PostAsync("/api/rhino/validate", null);
                
                RhinoApp.WriteLine($"Response status: {(int)response.StatusCode} {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    RhinoApp.WriteLine($"Response body: {json}");
                    
                    var result = JsonConvert.DeserializeObject<dynamic>(json);
                    
                    // Check subscription tier
                    var subscriptionTier = result.user?.subscriptionTier?.ToString()?.ToLower();
                    var allowedTiers = new[] { "standard", "pro", "educational" };
                    var hasValidSubscription = !string.IsNullOrEmpty(subscriptionTier) && 
                                               Array.Exists(allowedTiers, tier => tier == subscriptionTier);
                    
                    if (!hasValidSubscription)
                    {
                        // API key valid but subscription tier insufficient
                        return new ValidationResult
                        {
                            Success = true, // API key is valid
                            HasValidSubscription = false, // But subscription is insufficient
                            UserName = result.user?.displayName?.ToString(),
                            UserEmail = result.user?.email?.ToString(),
                            ErrorMessage = "Subscription upgrade required",
                            ErrorDetails = $"Your current plan ({subscriptionTier}) does not include Rhino plugin access. Please upgrade to Standard, Pro, or Educational plan.",
                            SubscriptionError = new ApiErrorResponse
                            {
                                Success = false,
                                Error = "SUBSCRIPTION_INSUFFICIENT",
                                UserMessage = $"Your {subscriptionTier?.ToUpper()} plan does not include Rhino plugin access.\n\nUpgrade to Standard, Pro, or Educational to use this plugin.",
                                CurrentPlan = subscriptionTier,
                                RequiredPlans = new[] { "Standard", "Pro", "Educational" },
                                UpgradeUrl = "https://vesselstudio.io/settings?tab=billing"
                            }
                        };
                    }
                    
                    // Valid subscription
                    return new ValidationResult
                    {
                        Success = true,
                        HasValidSubscription = true,
                        UserName = result.user?.displayName?.ToString(),
                        UserEmail = result.user?.email?.ToString(),
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
                var response = await _httpClient.GetAsync("/api/rhino/projects");
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
                RhinoApp.WriteLine($"[Upload] Starting upload to project: {projectId}");
                RhinoApp.WriteLine($"[Upload] Image size: {imageBytes.Length / 1024:F2} KB");
                RhinoApp.WriteLine($"[Upload] Image filename: {imageName ?? "capture.png"}");
                
                // Create multipart form data - HttpClient will auto-set Content-Type with boundary
                using (var formData = new MultipartFormDataContent())
                {
                    // Add file - REQUIRED field (backend expects "file" field name)
                    var fileContent = new ByteArrayContent(imageBytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                    
                    // Use the provided image name as filename, or default to "capture.png"
                    var filename = string.IsNullOrWhiteSpace(imageName) ? "capture.png" : $"{imageName}.png";
                    formData.Add(fileContent, "file", filename);
                    RhinoApp.WriteLine($"[Upload] ✓ Added file field: {filename} ({imageBytes.Length} bytes, image/png)");

                    // Add metadata fields as individual string fields (all optional)
                    if (metadata != null && metadata.Count > 0)
                    {
                        RhinoApp.WriteLine($"[Upload] Adding {metadata.Count} metadata fields:");
                        foreach (var kvp in metadata)
                        {
                            var value = kvp.Value?.ToString() ?? "";
                            formData.Add(new StringContent(value), kvp.Key);
                            RhinoApp.WriteLine($"[Upload]   ✓ {kvp.Key} = {value}");
                        }
                    }

                    // POST to upload endpoint - Content-Type header is automatically set by MultipartFormDataContent
                    var uploadUrl = $"/api/rhino/projects/{projectId}/upload";
                    RhinoApp.WriteLine($"[Upload] POST {BaseUrl}{uploadUrl}");
                    RhinoApp.WriteLine($"[Upload] Authorization: Bearer {_apiKey.Substring(0, 12)}...");
                    
                    var response = await _httpClient.PostAsync(uploadUrl, formData);
                    var responseText = await response.Content.ReadAsStringAsync();
                    
                    RhinoApp.WriteLine($"[Upload] Response: {(int)response.StatusCode} {response.StatusCode}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        RhinoApp.WriteLine($"[Upload] ✅ Success: {responseText}");
                        var result = JsonConvert.DeserializeObject<dynamic>(responseText);
                        return new UploadResult 
                        { 
                            Success = true, 
                            Message = "Upload successful",
                            Url = result.url?.ToString() ?? ""
                        };
                    }
                    else
                    {
                        RhinoApp.WriteLine($"[Upload] ❌ Failed: {responseText}");
                        return new UploadResult 
                        { 
                            Success = false, 
                            Message = $"{response.StatusCode}: {responseText}" 
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
                var response = await _httpClient.GetAsync("/api/plugin/ping");
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
                // Use different environment variable for DEV vs RELEASE
#if DEV
                var savedKey = Environment.GetEnvironmentVariable("VESSEL_STUDIO_API_KEY_DEV", EnvironmentVariableTarget.User);
#else
                var savedKey = Environment.GetEnvironmentVariable("VESSEL_STUDIO_API_KEY", EnvironmentVariableTarget.User);
#endif
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
                // Use different environment variable for DEV vs RELEASE
#if DEV
                Environment.SetEnvironmentVariable("VESSEL_STUDIO_API_KEY_DEV", apiKey, EnvironmentVariableTarget.User);
#else
                Environment.SetEnvironmentVariable("VESSEL_STUDIO_API_KEY", apiKey, EnvironmentVariableTarget.User);
#endif
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
        public string UserEmail { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDetails { get; set; }
        
        // Subscription validation
        public bool HasValidSubscription { get; set; }
        public ApiErrorResponse SubscriptionError { get; set; }
    }
}