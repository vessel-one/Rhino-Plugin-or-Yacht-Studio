using System;
using System.Threading.Tasks;
using Rhino;
using Rhino.Commands;
using VesselStudioPlugin.Models;
using VesselStudioPlugin.Services;
using VesselStudioPlugin.UI;

namespace VesselStudioPlugin.Commands
{
    /// <summary>
    /// Main command for capturing and uploading viewport screenshots to Vessel Studio
    /// </summary>
    public class CaptureToVesselStudioCommand : Command
    {
        public override string EnglishName => "CaptureToVesselStudio";
        
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Get plugin instance
                var plugin = VesselStudioPlugin.Instance;
                if (plugin == null)
                {
                    RhinoApp.WriteLine("VesselStudio plugin not loaded properly");
                    return Result.Failure;
                }
                
                // Check if user is authenticated
                if (!plugin.AuthService.IsAuthenticated)
                {
                    RhinoApp.WriteLine("Not authenticated. Starting authentication flow...");
                    
                    // Start authentication process
                    _ = Task.Run(async () =>
                    {
                        var authenticated = await plugin.AuthService.AuthenticateAsync();
                        if (authenticated)
                        {
                            RhinoApp.WriteLine("Authentication successful! You can now use CaptureToVesselStudio command.");
                        }
                        else
                        {
                            RhinoApp.WriteLine("Authentication failed. Please try again.");
                        }
                    });
                    
                    return Result.Success;
                }
                
                // Start the capture and upload process
                _ = Task.Run(async () => await CaptureAndUploadAsync(plugin));
                
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Command error: {ex.Message}");
                return Result.Failure;
            }
        }
        
        /// <summary>
        /// Performs the capture and upload process asynchronously
        /// </summary>
        /// <param name="plugin">Plugin instance</param>
        private async Task CaptureAndUploadAsync(VesselStudioPlugin plugin)
        {
            try
            {
                RhinoApp.WriteLine("Starting capture process...");
                
                // Step 1: Show project selection dialog
                ProjectInfo? selectedProject = null;
                
                // We need to run UI operations on the main thread
                RhinoApp.InvokeOnUiThread(() =>
                {
                    selectedProject = ProjectSelectionDialogFactory.ShowDialog(
                        plugin.ApiClient, 
                        plugin.AuthService);
                });
                
                if (selectedProject == null)
                {
                    RhinoApp.WriteLine("No project selected. Operation cancelled.");
                    return;
                }
                
                RhinoApp.WriteLine($"Selected project: {selectedProject.Name}");
                
                // Step 2: Capture screenshot
                RhinoApp.WriteLine("Capturing viewport screenshot...");
                
                var screenshotOptions = ScreenshotOptions.WebOptimized();
                var screenshot = await plugin.ScreenshotService.CaptureActiveViewportAsync(screenshotOptions);
                
                if (screenshot == null)
                {
                    RhinoApp.WriteLine("Failed to capture screenshot.");
                    return;
                }
                
                RhinoApp.WriteLine($"Screenshot captured: {screenshot.Metadata.ViewTitle} ({screenshot.ImageData.Length:N0} bytes)");
                
                // Step 3: Upload to Vessel Studio
                RhinoApp.WriteLine("Uploading to Vessel Studio...");
                
                var progress = new Progress<UploadProgressInfo>(info =>
                {
                    RhinoApp.WriteLine($"Upload progress: {info.Percentage}% - {info.Message}");
                });
                
                var uploadResult = await plugin.ApiClient.UploadScreenshotAsync(
                    selectedProject.Id, 
                    screenshot, 
                    progress);
                
                if (uploadResult?.State == UploadState.Completed)
                {
                    RhinoApp.WriteLine($"✓ Upload completed successfully!");
                    RhinoApp.WriteLine($"  Server URL: {uploadResult.ServerUrl}");
                    RhinoApp.WriteLine($"  Upload time: {uploadResult.GetDuration().TotalSeconds:F1} seconds");
                }
                else
                {
                    RhinoApp.WriteLine($"✗ Upload failed: {uploadResult?.ErrorMessage ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Capture and upload failed: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Command for authenticating with Vessel Studio
    /// </summary>
    public class VesselStudioAuthCommand : Command
    {
        public override string EnglishName => "VesselStudioAuth";
        
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                var plugin = VesselStudioPlugin.Instance;
                if (plugin == null)
                {
                    RhinoApp.WriteLine("VesselStudio plugin not loaded properly");
                    return Result.Failure;
                }
                
                if (plugin.AuthService.IsAuthenticated)
                {
                    var userInfo = plugin.AuthService.GetUserInfo();
                    RhinoApp.WriteLine($"Already authenticated as: {userInfo?.DisplayName ?? userInfo?.Email ?? "Unknown User"}");
                    
                    // Ask if user wants to sign out
                    var result = System.Windows.Forms.MessageBox.Show(
                        "Already authenticated. Sign out?", 
                        "Vessel Studio", 
                        System.Windows.Forms.MessageBoxButtons.YesNo, 
                        System.Windows.Forms.MessageBoxIcon.Question);
                    
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        _ = Task.Run(async () =>
                        {
                            await plugin.AuthService.SignOutAsync();
                            RhinoApp.WriteLine("Signed out successfully");
                        });
                    }
                }
                else
                {
                    RhinoApp.WriteLine("Starting authentication...");
                    
                    _ = Task.Run(async () =>
                    {
                        var authenticated = await plugin.AuthService.AuthenticateAsync();
                        if (authenticated)
                        {
                            var userInfo = plugin.AuthService.GetUserInfo();
                            RhinoApp.WriteLine($"✓ Authentication successful! Welcome, {userInfo?.DisplayName ?? userInfo?.Email ?? "User"}");
                        }
                        else
                        {
                            RhinoApp.WriteLine("✗ Authentication failed. Please try again.");
                        }
                    });
                }
                
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Authentication command error: {ex.Message}");
                return Result.Failure;
            }
        }
    }
    
    /// <summary>
    /// Command for showing Vessel Studio connection status
    /// </summary>
    public class VesselStudioStatusCommand : Command
    {
        public override string EnglishName => "VesselStudioStatus";
        
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                var plugin = VesselStudioPlugin.Instance;
                if (plugin == null)
                {
                    RhinoApp.WriteLine("VesselStudio plugin not loaded properly");
                    return Result.Failure;
                }
                
                RhinoApp.WriteLine("=== Vessel Studio Plugin Status ===");
                
                // Authentication status
                if (plugin.AuthService.IsAuthenticated)
                {
                    var userInfo = plugin.AuthService.GetUserInfo();
                    RhinoApp.WriteLine($"✓ Authenticated as: {userInfo?.DisplayName ?? userInfo?.Email ?? "Unknown User"}");
                    
                    var session = plugin.AuthService.CurrentSession;
                    if (session != null)
                    {
                        var expiresIn = session.ExpiresAt - DateTime.UtcNow;
                        RhinoApp.WriteLine($"  Token expires in: {expiresIn.TotalHours:F1} hours");
                    }
                }
                else
                {
                    RhinoApp.WriteLine("✗ Not authenticated");
                }
                
                // Connection status
                _ = Task.Run(async () =>
                {
                    var connected = await plugin.ApiClient.TestConnectivityAsync();
                    if (connected)
                    {
                        RhinoApp.WriteLine("✓ Connected to Vessel Studio API");
                        
                        // Get projects count
                        try
                        {
                            var projects = await plugin.ApiClient.GetProjectsAsync();
                            if (projects != null)
                            {
                                var projectList = System.Linq.Enumerable.ToList(projects);
                                RhinoApp.WriteLine($"  Available projects: {projectList.Count}");
                            }
                        }
                        catch (Exception ex)
                        {
                            RhinoApp.WriteLine($"  Could not retrieve projects: {ex.Message}");
                        }
                    }
                    else
                    {
                        RhinoApp.WriteLine("✗ Cannot connect to Vessel Studio API");
                    }
                });
                
                // Viewport info
                var activeViewport = doc?.Views?.ActiveView?.ActiveViewport;
                if (activeViewport != null)
                {
                    RhinoApp.WriteLine($"✓ Active viewport: {activeViewport.Name}");
                    RhinoApp.WriteLine($"  Size: {activeViewport.Size.Width}x{activeViewport.Size.Height}");
                    
                    var metadata = plugin.ScreenshotService.GetActiveViewportMetadata();
                    if (metadata != null)
                    {
                        RhinoApp.WriteLine($"  Projection: {metadata.ProjectionMode}");
                        RhinoApp.WriteLine($"  Objects: {metadata.ObjectCount}");
                    }
                }
                else
                {
                    RhinoApp.WriteLine("✗ No active viewport");
                }
                
                RhinoApp.WriteLine("====================================");
                
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Status command error: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}