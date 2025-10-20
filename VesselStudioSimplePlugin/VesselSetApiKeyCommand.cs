using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Command to set/update the Vessel One API key
    /// </summary>
    [System.Runtime.InteropServices.Guid("C2D3E4F5-A6B7-8C9D-0E1F-2A3B4C5D6E7F")]
    public class VesselStudioSetApiKeyCommand : Command
    {
        public VesselStudioSetApiKeyCommand()
        {
            Instance = this;
        }

        public static VesselStudioSetApiKeyCommand Instance { get; private set; }

        public override string EnglishName => "VesselSetApiKey";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Prompt user for API key
            string apiKey = string.Empty;
            var rc = RhinoGet.GetString("Enter Vessel One API key (vsk_live_...)", false, ref apiKey);
            
            if (rc != Result.Success || string.IsNullOrWhiteSpace(apiKey))
            {
                RhinoApp.WriteLine("API key entry cancelled");
                return Result.Cancel;
            }

            // Validate key format
            if (!apiKey.StartsWith("vsk_"))
            {
                RhinoApp.WriteLine("❌ Invalid API key format. Key should start with 'vsk_live_' or 'vsk_test_'");
                RhinoApp.WriteLine("Get your API key from: https://vessel.one/profile");
                return Result.Failure;
            }

            // Create API client and validate
            using (var apiClient = new VesselStudioApiClient())
            {
                apiClient.SetApiKey(apiKey);

                RhinoApp.WriteLine("Validating API key with Vessel One...");
                var validateTask = apiClient.ValidateApiKeyAsync();
                validateTask.Wait();
                
                var (success, userName) = validateTask.Result;
                
                if (success)
                {
                    // Save to settings
                    var settings = VesselStudioSettings.Load();
                    settings.ApiKey = apiKey;
                    settings.Save();

                    RhinoApp.WriteLine($"✅ Connected to Vessel One as: {userName}");
                    RhinoApp.WriteLine("");
                    RhinoApp.WriteLine("Next steps:");
                    RhinoApp.WriteLine("  • Run 'VesselCapture' to upload a screenshot with project selection");
                    RhinoApp.WriteLine("  • Run 'VesselQuickCapture' for rapid-fire captures to last project");
                    RhinoApp.WriteLine("  • Run 'VesselStatus' to check connection");
                    
                    return Result.Success;
                }
                else
                {
                    RhinoApp.WriteLine("❌ Failed to validate API key");
                    RhinoApp.WriteLine("");
                    RhinoApp.WriteLine("Troubleshooting:");
                    RhinoApp.WriteLine("  • Check that you copied the entire key");
                    RhinoApp.WriteLine("  • Verify the key hasn't been revoked");
                    RhinoApp.WriteLine("  • Create a new key at: https://vessel.one/profile");
                    
                    return Result.Failure;
                }
            }
        }
    }
}
