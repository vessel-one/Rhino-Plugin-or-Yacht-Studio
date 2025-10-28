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
#if DEV
        public override string EnglishName => "DevVesselStudioStatus";
#else
        public override string EnglishName => "VesselStudioStatus";
#endif

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
                    
                    // Note: Connection testing removed - was causing UI freezes with async/await
                    // The API key validation is sufficient to confirm authentication
                    RhinoApp.WriteLine("✅ API key is configured and ready");
                    RhinoApp.WriteLine("");
                    RhinoApp.WriteLine("Ready to capture! Use commands:");
                    RhinoApp.WriteLine("  • VesselCapture - Capture and add to queue");
                    RhinoApp.WriteLine("  • VesselStudioShowToolbar - Show toolbar");
                }
                else
                {
                    RhinoApp.WriteLine("❌ Authentication: No API key set");
                    RhinoApp.WriteLine("Use 'VesselSetApiKey' command to authenticate");
                }

                RhinoApp.WriteLine("");
                RhinoApp.WriteLine("Available commands:");
                RhinoApp.WriteLine("  • VesselSetApiKey - Set your API key");
                RhinoApp.WriteLine("  • VesselCapture - Capture viewport and add to queue");
                RhinoApp.WriteLine("  • VesselImageSettings - Configure image format (PNG/JPEG)");
                RhinoApp.WriteLine("  • VesselQueueManagerCommand - View and manage queue");
                RhinoApp.WriteLine("  • VesselStudioStatus - Show this status");

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