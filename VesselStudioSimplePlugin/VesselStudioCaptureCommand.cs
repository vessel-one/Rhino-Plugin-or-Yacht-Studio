using System;
using System.Drawing;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Input;

namespace VesselStudioSimplePlugin
{
    [System.Runtime.InteropServices.Guid("B1C2D3E4-F5A6-7B8C-9D0E-1F2A3B4C5D6E")]
    public class VesselStudioCaptureCommand : Command
    {
        public VesselStudioCaptureCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        /// <summary>
        /// The only instance of this command.
        /// </summary>
        public static VesselStudioCaptureCommand Instance { get; private set; }

        /// <summary>
        /// The command name as it appears on the Rhino command line.
        /// </summary>
        public override string EnglishName => "VesselStudioCapture";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var plugin = VesselStudioSimplePlugin.Instance;
            
            if (plugin?.ApiClient == null)
            {
                RhinoApp.WriteLine("Vessel Studio plugin not properly loaded");
                return Result.Failure;
            }

            // Check if authenticated
            if (!plugin.ApiClient.IsAuthenticated)
            {
                RhinoApp.WriteLine("Not authenticated with Vessel Studio");
                RhinoApp.WriteLine("Use 'VesselStudioSetApiKey' command first to set your API key");
                return Result.Failure;
            }

            // Get active view
            var activeView = doc.Views.ActiveView;
            if (activeView == null)
            {
                RhinoApp.WriteLine("No active viewport found");
                return Result.Failure;
            }

            try
            {
                RhinoApp.WriteLine("Capturing viewport screenshot...");

                // IMPORTANT: Capture must happen on main thread
                // Following RhinoMCP pattern for thread-safe Rhino API access
                Result finalResult = Result.Failure;
                
                RhinoApp.InvokeOnUiThread(new System.Action(() =>
                {
                    try
                    {
                        // Get viewport size
                        var viewport = activeView.ActiveViewport;
                        var size = viewport.Size;

                        // Create bitmap by capturing viewport
                        using (var bitmap = activeView.CaptureToBitmap(size))
                        {
                            if (bitmap == null)
                            {
                                RhinoApp.WriteLine("❌ Failed to capture viewport");
                                finalResult = Result.Failure;
                                return;
                            }

                            RhinoApp.WriteLine("Screenshot captured, uploading to Vessel Studio...");

                            // Upload asynchronously but wait for result
                            var uploadTask = plugin.ApiClient.UploadScreenshotAsync(bitmap);
                            var result = uploadTask.GetAwaiter().GetResult(); // Simple blocking wait

                            if (result.Success)
                            {
                                RhinoApp.WriteLine($"✅ {result.Message}");
                                if (!string.IsNullOrEmpty(result.Url))
                                {
                                    RhinoApp.WriteLine($"View online: {result.Url}");
                                }
                                finalResult = Result.Success;
                            }
                            else
                            {
                                RhinoApp.WriteLine($"❌ {result.Message}");
                                finalResult = Result.Failure;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        RhinoApp.WriteLine($"Error during capture: {ex.Message}");
                        finalResult = Result.Failure;
                    }
                }));

                return finalResult;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}