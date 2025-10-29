using Rhino;
using Rhino.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VesselStudioSimplePlugin.Models;
using VesselStudioSimplePlugin.Services;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// T062: Command to send queued batch of captures to Vessel Studio
    /// 
    /// Usage: 
    /// - VesselSendBatch (Release) or DevVesselSendBatch (Dev)
    /// - Uploads all items in CaptureQueueService to selected project
    /// - Alternative to using the "Quick Export Batch" button
    /// 
    /// User Story 3: Send Queued Batch to Vessel Studio (Phase 5)
    /// </summary>
    public class VesselSendBatchCommand : Command
    {
#if DEV
        public override string EnglishName => "DevVesselSendBatch";
#else
        public override string EnglishName => "VesselSendBatch";
#endif

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Load settings
            var settings = VesselStudioSettings.Load();
            
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                RhinoApp.WriteLine("❌ API key not set. Run VesselSetApiKey first.");
                return Result.Failure;
            }

            if (string.IsNullOrEmpty(settings.LastProjectId))
            {
                RhinoApp.WriteLine("❌ No project selected. Please select a project from the toolbar dropdown.");
                return Result.Failure;
            }

            // Check if queue has items
            var queueService = CaptureQueueService.Current;
            if (queueService.IsEmpty)
            {
                RhinoApp.WriteLine("❌ Queue is empty. Add captures before sending batch.");
                RhinoApp.WriteLine("💡 Use 'VesselAddToQueue' command to add captures to the queue.");
                return Result.Failure;
            }

            RhinoApp.WriteLine($"📦 Batch queue has {queueService.ItemCount} item{(queueService.ItemCount == 1 ? "" : "s")}");
            RhinoApp.WriteLine($"📤 Starting batch upload to project: {settings.LastProjectName}...");

            // Run upload asynchronously
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var apiClient = new VesselStudioApiClient();
                    apiClient.SetApiKey(settings.ApiKey);
                    var uploadService = new BatchUploadService(apiClient);

                    // Create progress reporter
                    var progress = new Progress<BatchUploadProgress>(p =>
                    {
                        if (p.TotalItems > 0)
                        {
                            RhinoApp.WriteLine(
                                $"[BatchUpload] {p.CompletedItems}/{p.TotalItems} uploaded ({p.PercentComplete}%) - {p.CurrentFilename}");
                        }
                    });

                    // Upload batch
                    var result = await uploadService.UploadBatchAsync(settings.LastProjectId, progress);

                    // Report results
                    if (result.Success)
                    {
                        RhinoApp.WriteLine($"✅ Batch upload complete!");
                        RhinoApp.WriteLine($"   {result.UploadedCount} capture{(result.UploadedCount == 1 ? "" : "s")} uploaded successfully");
                        RhinoApp.WriteLine($"   Duration: {result.TotalDurationMs}ms");
                    }
                    else if (result.ApiKeyInvalid)
                    {
                        // API key became invalid during upload - clear it
                        settings.ApiKey = null;
                        settings.LastProjectId = null;
                        settings.LastProjectName = null;
                        settings.HasValidSubscription = false;
                        settings.SubscriptionErrorMessage = "API key is no longer valid.";
                        settings.Save();
                        
                        RhinoApp.WriteLine($"❌ API key is no longer valid");
                        RhinoApp.WriteLine($"   {result.UploadedCount} capture{(result.UploadedCount == 1 ? "" : "s")} were uploaded before error");
                        RhinoApp.WriteLine($"   Please run 'VesselSetApiKey' to reconfigure");
                        RhinoApp.WriteLine($"   Queue preserved for retry after reconfiguration");
                    }
                    else if (result.SubscriptionInvalid)
                    {
                        // Subscription/tier error - don't delete key, just show error
                        settings.HasValidSubscription = false;
                        settings.SubscriptionErrorMessage = "Your plan does not include Rhino plugin access. Please upgrade.";
                        settings.Save();
                        
                        RhinoApp.WriteLine($"❌ Subscription upgrade required");
                        RhinoApp.WriteLine($"   {result.UploadedCount} capture{(result.UploadedCount == 1 ? "" : "s")} were uploaded before error");
                        RhinoApp.WriteLine($"   Upgrade your plan at: https://vesselstudio.io/settings?tab=billing");
                        RhinoApp.WriteLine($"   Queue preserved for retry after upgrade");
                    }
                    else if (result.IsPartialSuccess)
                    {
                        RhinoApp.WriteLine($"⚠ Batch upload incomplete");
                        RhinoApp.WriteLine($"   Successful: {result.UploadedCount}");
                        RhinoApp.WriteLine($"   Failed: {result.FailedCount}");
                        RhinoApp.WriteLine($"   Queue preserved for retry");
                        
                        if (result.Errors.Count > 0)
                        {
                            RhinoApp.WriteLine($"   Errors:");
                            foreach (var error in result.Errors.Take(3))
                            {
                                RhinoApp.WriteLine($"     • {error.filename}: {error.error}");
                            }
                            if (result.Errors.Count > 3)
                            {
                                RhinoApp.WriteLine($"     ... and {result.Errors.Count - 3} more errors");
                            }
                        }
                    }
                    else
                    {
                        RhinoApp.WriteLine($"❌ Batch upload failed");
                        RhinoApp.WriteLine($"   Failed: {result.FailedCount}");
                        RhinoApp.WriteLine($"   Queue preserved for retry");
                        
                        if (result.Errors.Count > 0)
                        {
                            RhinoApp.WriteLine($"   Errors:");
                            foreach (var error in result.Errors.Take(3))
                            {
                                RhinoApp.WriteLine($"     • {error.filename}: {error.error}");
                            }
                            if (result.Errors.Count > 3)
                            {
                                RhinoApp.WriteLine($"     ... and {result.Errors.Count - 3} more errors");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"❌ Unexpected error: {ex.Message}");
                }
            });

            return Result.Success;
        }
    }
}
