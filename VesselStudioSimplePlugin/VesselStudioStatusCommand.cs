using System;
using Rhino;
using Rhino.Commands;

namespace VesselStudioSimplePlugin
{
    [System.Runtime.InteropServices.Guid("D3E4F5A6-B7C8-9D0E-1F2A-3B4C5D6E7F8A")]
    public class VesselStudioStatusCommand : Command
    {
        public VesselStudioStatusCommand()
        {
            Instance = this;
        }

        /// <summary>
        /// The only instance of this command.
        /// </summary>
        public static VesselStudioStatusCommand Instance { get; private set; }

        /// <summary>
        /// The command name as it appears on the Rhino command line.
        /// </summary>
        public override string EnglishName => "VesselStudioStatus";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var plugin = VesselStudioSimplePlugin.Instance;
            
            if (plugin?.ApiClient == null)
            {
                RhinoApp.WriteLine("❌ Vessel Studio plugin not properly loaded");
                return Result.Failure;
            }

            try
            {
                RhinoApp.WriteLine("=== Vessel Studio Plugin Status ===");
                
                if (plugin.ApiClient.IsAuthenticated)
                {
                    RhinoApp.WriteLine("✅ Authentication: Logged in with API key");
                    
                    // Test connection
                    RhinoApp.WriteLine("Testing connection...");
                    var testTask = plugin.ApiClient.TestConnectionAsync();
                    var connectionOk = testTask.GetAwaiter().GetResult();
                    
                    if (connectionOk)
                    {
                        RhinoApp.WriteLine("✅ Connection: API is reachable");
                        RhinoApp.WriteLine("");
                        RhinoApp.WriteLine("Ready to capture! Use 'VesselStudioCapture' command");
                    }
                    else
                    {
                        RhinoApp.WriteLine("❌ Connection: Cannot reach Vessel Studio API");
                        RhinoApp.WriteLine("Check your internet connection");
                    }
                }
                else
                {
                    RhinoApp.WriteLine("❌ Authentication: No API key set");
                    RhinoApp.WriteLine("Use 'VesselStudioSetApiKey' command to authenticate");
                }

                RhinoApp.WriteLine("");
                RhinoApp.WriteLine("Available commands:");
                RhinoApp.WriteLine("- VesselStudioSetApiKey: Set your API key");
                RhinoApp.WriteLine("- VesselStudioCapture: Capture and upload viewport");
                RhinoApp.WriteLine("- VesselStudioStatus: Show this status");

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error checking status: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}