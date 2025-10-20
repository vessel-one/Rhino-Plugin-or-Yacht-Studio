using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rhino;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Simple API client for Vessel Studio
    /// </summary>
    public class VesselStudioApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://vesselstudio.ai/api";
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
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                
            // Save to Rhino settings
            SaveApiKey(apiKey);
        }

        /// <summary>
        /// Check if authenticated
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(_apiKey);

        /// <summary>
        /// Upload a screenshot bitmap to Vessel Studio
        /// </summary>
        public async Task<UploadResult> UploadScreenshotAsync(Bitmap screenshot, string projectId = null)
        {
            if (!IsAuthenticated)
            {
                return new UploadResult { Success = false, Message = "Not authenticated. Please set API key first." };
            }

            try
            {
                // Convert bitmap to PNG bytes
                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    screenshot.Save(ms, ImageFormat.Png);
                    imageBytes = ms.ToArray();
                }

                // Create metadata
                var metadata = new
                {
                    source = "rhino-plugin-simple",
                    rhinoVersion = RhinoApp.Version.ToString(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    width = screenshot.Width,
                    height = screenshot.Height,
                    projectId = projectId ?? "default"
                };

                // Create multipart form data
                using (var content = new MultipartFormDataContent())
                {
                    // Add image
                    var imageContent = new ByteArrayContent(imageBytes);
                    imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                    content.Add(imageContent, "image", $"rhino_screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                    // Add metadata
                    var metadataJson = JsonConvert.SerializeObject(metadata);
                    content.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"), "metadata");

                    // Upload
                    var response = await _httpClient.PostAsync("/plugin/upload", content);
                    var responseText = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(responseText);
                        return new UploadResult 
                        { 
                            Success = true, 
                            Message = "Screenshot uploaded successfully!",
                            Url = result?.url?.ToString() ?? ""
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
}