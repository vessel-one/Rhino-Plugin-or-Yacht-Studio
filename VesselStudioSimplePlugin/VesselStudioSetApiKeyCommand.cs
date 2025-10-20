using System;
using Rhino;
using Rhino.Commands;
using Rhino.Input;

namespace VesselStudioSimplePlugin
{
    [System.Runtime.InteropServices.Guid("C2D3E4F5-A6B7-8C9D-0E1F-2A3B4C5D6E7F")]
    public class VesselStudioSetApiKeyCommand : Command
    {
        public VesselStudioSetApiKeyCommand()
        {
            Instance = this;
        }

        /// <summary>
        /// The only instance of this command.
        /// </summary>
        public static VesselStudioSetApiKeyCommand Instance { get; private set; }

        /// <summary>
        /// The command name as it appears on the Rhino command line.
        /// </summary>
        public override string EnglishName => "VesselStudioSetApiKey";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var plugin = VesselStudioSimplePlugin.Instance;
            
            if (plugin?.ApiClient == null)
            {
                RhinoApp.WriteLine("Vessel Studio plugin not properly loaded");
                return Result.Failure;
            }

            try
            {
                // Get API key from user input
                RhinoApp.WriteLine("Please enter your Vessel Studio API key:");
                
                // Simple prompt - user can type it in command line or we'll get it another way
                string apiKey = "";
                
                // For now, use a simple approach - user can set via environment variable
                // or we can enhance this later with better UI
                apiKey = Environment.GetEnvironmentVariable("VESSEL_STUDIO_API_KEY_INPUT");
                
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    RhinoApp.WriteLine("No API key provided. Please set VESSEL_STUDIO_API_KEY_INPUT environment variable and run again.");
                    RhinoApp.WriteLine("Or contact support for proper authentication setup.");
                    return Result.Cancel;
                }

                // Set the API key
                plugin.ApiClient.SetApiKey(apiKey);
                
                RhinoApp.WriteLine("Testing connection to Vessel Studio...");

                // Test the connection
                var testTask = plugin.ApiClient.TestConnectionAsync();
                var connectionOk = testTask.GetAwaiter().GetResult(); // Simple blocking wait

                if (connectionOk)
                {
                    RhinoApp.WriteLine("✅ API key set successfully and connection verified!");
                    RhinoApp.WriteLine("You can now use 'VesselStudioCapture' to upload viewport screenshots");
                }
                else
                {
                    RhinoApp.WriteLine("⚠️  API key saved but connection test failed");
                    RhinoApp.WriteLine("Please check your API key and internet connection");
                }

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error setting API key: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}