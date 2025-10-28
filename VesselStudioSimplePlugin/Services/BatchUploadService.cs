using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Rhino;
using VesselStudioSimplePlugin.Models;

namespace VesselStudioSimplePlugin.Services
{
    /// <summary>
    /// Service for uploading queued batch captures to Vessel Studio.
    /// 
    /// Implements User Story 3: Send Queued Batch to Vessel Studio
    /// - T043: Create BatchUploadService with VesselStudioApiClient dependency
    /// - T044: Implement GenerateFilename with ProjectName_ViewportName_Sequence.png pattern
    /// - T045-T052: Implement UploadBatchAsync with progress, error handling, and queue management
    /// 
    /// Features:
    /// - Sequential upload of all queued captures (one at a time)
    /// - IProgress<BatchUploadProgress> reporting for UI updates (<2s lag per SC-007)
    /// - Error collection with partial success support (FR-008)
    /// - Cancellation token support for user abort
    /// - Queue preservation on failure (FR-008)
    /// - Queue clear only on complete success (FR-007)
    /// - Descriptive filenames with regex sanitization (FR-017)
    /// </summary>
    public class BatchUploadService
    {
        private readonly VesselStudioApiClient _apiClient;

        /// <summary>
        /// T043: Initializes a new instance of the BatchUploadService.
        /// </summary>
        /// <param name="apiClient">The API client for communication with Vessel Studio</param>
        /// <exception cref="ArgumentNullException">Thrown if apiClient is null</exception>
        public BatchUploadService(VesselStudioApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// T044: Generates a descriptive filename for a queued capture.
        /// 
        /// Pattern: ProjectName_ViewportName_Sequence.png
        /// Example: "Yacht_Design_A_Perspective_001.png"
        /// 
        /// Sanitization applied to project and viewport names:
        /// - Replace spaces with underscores
        /// - Remove illegal filesystem characters: <>:"/\|?*
        /// - Preserve alphanumerics, underscores, hyphens
        /// </summary>
        /// <param name="projectName">The project name from the dropdown/selection</param>
        /// <param name="viewportName">The viewport name (e.g., "Perspective", "Top")</param>
        /// <param name="sequenceNumber">The item's sequence number (auto-assigned, 1-based)</param>
        /// <returns>A sanitized filename (e.g., "Project_Viewport_001.png")</returns>
        private string GenerateFilename(string projectName, string viewportName, int sequenceNumber)
        {
            // T044: Sanitize project name
            // Replace spaces with underscores, remove illegal characters
            var sanitizedProject = SanitizeForFilename(projectName);
            
            // T044: Sanitize viewport name
            var sanitizedViewport = SanitizeForFilename(viewportName);
            
            // T044: Format with zero-padded sequence number (FR-017)
            // Zero-pad to 3 digits (001, 002... 099) for proper sorting
            var filename = $"{sanitizedProject}_{sanitizedViewport}_{sequenceNumber:D3}.png";
            
            return filename;
        }

        /// <summary>
        /// Sanitizes a string for use as a filename.
        /// 
        /// Rules (FR-017):
        /// - Replace spaces with underscores
        /// - Remove illegal filesystem characters: <>:"/\|?*
        /// - Preserve alphanumerics, underscores, hyphens
        /// </summary>
        private string SanitizeForFilename(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "unknown";

            // Replace spaces with underscores
            var result = input.Replace(" ", "_");

            // Remove illegal filesystem characters: <>:"/\|?*
            // Using regex to remove any character not in the allowed set
            result = Regex.Replace(result, @"[<>:""/\\|?*]", "");

            // Ensure result is not empty after sanitization
            if (string.IsNullOrEmpty(result))
                result = "unnamed";

            return result;
        }

        /// <summary>
        /// T045-T052: Uploads all queued captures to Vessel Studio as a batch.
        /// 
        /// Workflow:
        /// 1. Validate preconditions (queue not empty, projectId valid, API key configured)
        /// 2. Start stopwatch for duration tracking
        /// 3. Get items from CaptureQueueService
        /// 4. For each item in sequence:
        ///    a. Generate descriptive filename
        ///    b. Create metadata with viewport name and sequence info
        ///    c. Call _apiClient.UploadScreenshotAsync (existing method)
        ///    d. Track success/failure and errors
        ///    e. Report progress via IProgress<BatchUploadProgress>
        ///    f. Check CancellationToken for abort support
        /// 5. On complete success: Clear queue (FR-007)
        /// 6. On any failure/partial: Preserve queue (FR-008)
        /// 7. Return BatchUploadResult with complete statistics
        /// 
        /// Error Handling (FR-008):
        /// - Collect errors for failed items with (filename, error message) tuples
        /// - Continue uploading remaining items on individual failures
        /// - Success = all items uploaded successfully
        /// - PartialSuccess = some items uploaded, some failed
        /// - CompleteFailure = no items uploaded
        /// </summary>
        /// <param name="projectId">The target project ID from Vessel Studio</param>
        /// <param name="progress">Progress reporter for UI updates (called after each item)</param>
        /// <param name="cancellationToken">Token to support user abort</param>
        /// <returns>BatchUploadResult with success status, counts, and error details</returns>
        public async Task<BatchUploadResult> UploadBatchAsync(
            string projectId,
            IProgress<BatchUploadProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            // T045: Validate preconditions
            var result = new BatchUploadResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // T045: Validate API key is configured (FR-014)
                if (!_apiClient.IsAuthenticated)
                {
                    result.Success = false;
                    result.FailedCount = 0;
                    result.Errors.Add(("batch", "API key not configured. Please set API key before uploading."));
                    RhinoApp.WriteLine("[BatchUpload] ❌ Error: API key not configured");
                    return result;
                }

                // T045: Validate project ID is not null (FR-016)
                if (string.IsNullOrWhiteSpace(projectId))
                {
                    result.Success = false;
                    result.FailedCount = 0;
                    result.Errors.Add(("batch", "Project ID not specified. Please select a project before uploading."));
                    RhinoApp.WriteLine("[BatchUpload] ❌ Error: Project ID not specified");
                    return result;
                }

                // T045: Validate queue is not empty (FR-012)
                var items = CaptureQueueService.Current.GetItems();
                if (items.Count == 0)
                {
                    result.Success = false;
                    result.FailedCount = 0;
                    result.Errors.Add(("batch", "Queue is empty. Add captures before uploading."));
                    RhinoApp.WriteLine("[BatchUpload] ❌ Error: Queue is empty");
                    return result;
                }

                RhinoApp.WriteLine($"[BatchUpload] Starting batch upload: {items.Count} items to project {projectId}");

                // T046: Sequential upload loop - upload each item one at a time
                for (int i = 0; i < items.Count; i++)
                {
                    // T049: Check cancellation token for user abort support
                    cancellationToken.ThrowIfCancellationRequested();

                    var item = items[i];
                    
                    try
                    {
                        // T044: Generate descriptive filename
                        var filename = GenerateFilename(
                            CaptureQueueService.Current.ProjectName ?? "Project",
                            item.ViewportName,
                            item.SequenceNumber);

                        RhinoApp.WriteLine($"[BatchUpload] Uploading ({i + 1}/{items.Count}): {filename}");

                        // Create metadata for the upload
                        var metadata = new Dictionary<string, object>
                        {
                            { "viewport", item.ViewportName },
                            { "sequence", item.SequenceNumber },
                            { "source", "rhino-batch-capture" },
                            { "timestamp", item.Timestamp.ToString("o") }
                        };

                        // T046: Call existing VesselStudioApiClient.UploadScreenshotAsync
                        var uploadResult = await _apiClient.UploadScreenshotAsync(
                            projectId,
                            item.ImageData,
                            filename.Replace(".png", ""), // UploadScreenshotAsync adds .png extension
                            metadata);

                        if (uploadResult.Success)
                        {
                            result.UploadedCount++;
                            RhinoApp.WriteLine($"[BatchUpload] ✓ Success: {filename}");
                        }
                        else
                        {
                            // T048: Collect errors for failed items
                            result.FailedCount++;
                            result.Errors.Add((filename, uploadResult.Message ?? "Unknown error"));
                            RhinoApp.WriteLine($"[BatchUpload] ✗ Failed: {filename} - {uploadResult.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // T048: Collect errors and continue on individual failures
                        result.FailedCount++;
                        var itemFilename = GenerateFilename(
                            CaptureQueueService.Current.ProjectName ?? "Project",
                            item.ViewportName,
                            item.SequenceNumber);
                        result.Errors.Add((itemFilename, ex.Message));
                        RhinoApp.WriteLine($"[BatchUpload] ✗ Exception uploading {itemFilename}: {ex.Message}");
                    }

                    // T047: Report progress after each item (SC-007 <2s lag)
                    var totalItems = items.Count;
                    var currentFilename = GenerateFilename(
                        CaptureQueueService.Current.ProjectName ?? "Project",
                        item.ViewportName,
                        item.SequenceNumber);

                    progress?.Report(new BatchUploadProgress
                    {
                        TotalItems = totalItems,
                        CompletedItems = result.UploadedCount,
                        FailedItems = result.FailedCount,
                        CurrentFilename = currentFilename
                        // Note: PercentComplete is a calculated property
                    });
                }

                // T050: Set success status
                result.Success = result.FailedCount == 0;

                // T051-T052: Clear or preserve queue based on success
                if (result.Success)
                {
                    // T051: Complete success - clear queue (FR-007)
                    RhinoApp.WriteLine("[BatchUpload] ✅ Batch upload complete: All items uploaded successfully");
                    CaptureQueueService.Current.Clear();
                }
                else
                {
                    // T052: Partial or complete failure - preserve queue (FR-008)
                    RhinoApp.WriteLine(
                        $"[BatchUpload] ⚠ Batch upload incomplete: {result.UploadedCount} successful, {result.FailedCount} failed");
                    RhinoApp.WriteLine("[BatchUpload] Queue preserved for retry");
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                RhinoApp.WriteLine("[BatchUpload] ⚠ Upload cancelled by user");
                result.Success = false;
                result.Errors.Add(("batch", "Upload cancelled by user"));
                return result;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"[BatchUpload] ❌ Unexpected error: {ex.Message}");
                result.Success = false;
                result.FailedCount = CaptureQueueService.Current.ItemCount;
                result.Errors.Add(("batch", $"Unexpected error: {ex.Message}"));
                return result;
            }
            finally
            {
                stopwatch.Stop();
                // T050: Record total duration
                result.TotalDurationMs = stopwatch.ElapsedMilliseconds;
                RhinoApp.WriteLine($"[BatchUpload] Duration: {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}
