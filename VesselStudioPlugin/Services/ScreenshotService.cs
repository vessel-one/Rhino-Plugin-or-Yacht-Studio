using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using VesselStudioPlugin.Models;
using VesselStudioPlugin.Utils;

namespace VesselStudioPlugin.Services
{
    /// <summary>
    /// Service for capturing and processing Rhino viewport screenshots
    /// </summary>
    public class ScreenshotService : IScreenshotService
    {
        #region Fields and Events
        
        private readonly object _captureLock = new object();
        
        public event EventHandler<ScreenshotCapturedEventArgs>? ScreenshotCaptured;
        public event EventHandler<ScreenshotErrorEventArgs>? ScreenshotError;
        public event EventHandler<ScreenshotProgressEventArgs>? ScreenshotProgress;
        public event EventHandler<UploadProgressEventArgs>? UploadProgress;
        public event EventHandler<UploadCompletedEventArgs>? UploadCompleted;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Captures a screenshot of the active Rhino viewport
        /// </summary>
        /// <param name="options">Screenshot capture options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Viewport screenshot with metadata</returns>
        public async Task<ViewportScreenshot?> CaptureActiveViewportAsync(
            ScreenshotOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                lock (_captureLock)
                {
                    return CaptureActiveViewport(options, cancellationToken);
                }
            }, cancellationToken);
        }
        
        /// <summary>
        /// Captures a screenshot of a specific Rhino viewport
        /// </summary>
        /// <param name="viewportId">ID of the viewport to capture</param>
        /// <param name="options">Screenshot capture options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Viewport screenshot with metadata</returns>
        public async Task<ViewportScreenshot?> CaptureViewportAsync(
            Guid viewportId, 
            ScreenshotOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                lock (_captureLock)
                {
                    return CaptureViewport(viewportId, options, cancellationToken);
                }
            }, cancellationToken);
        }
        
        /// <summary>
        /// Captures screenshots of all open viewports
        /// </summary>
        /// <param name="options">Screenshot capture options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of viewport screenshots</returns>
        public async Task<IList<ViewportScreenshot>> CaptureAllViewportsAsync(
            ScreenshotOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                lock (_captureLock)
                {
                    return CaptureAllViewports(options, cancellationToken);
                }
            }, cancellationToken);
        }
        
        /// <summary>
        /// Gets metadata for the active viewport without capturing
        /// </summary>
        /// <returns>Viewport metadata</returns>
        public ViewportMetadata? GetActiveViewportMetadata()
        {
            try
            {
                var activeViewport = RhinoDoc.ActiveDoc?.Views?.ActiveView?.ActiveViewport;
                if (activeViewport == null)
                    return null;
                
                return ExtractViewportMetadata(activeViewport);
            }
            catch (Exception ex)
            {
                OnScreenshotError($"Failed to get active viewport metadata: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Gets metadata for all open viewports
        /// </summary>
        /// <returns>List of viewport metadata</returns>
        public IList<ViewportMetadata> GetAllViewportMetadata()
        {
            var metadataList = new List<ViewportMetadata>();
            
            try
            {
                var doc = RhinoDoc.ActiveDoc;
                if (doc?.Views == null)
                    return metadataList;
                
                foreach (var view in doc.Views)
                {
                    if (view?.ActiveViewport != null)
                    {
                        var metadata = ExtractViewportMetadata(view.ActiveViewport);
                        if (metadata != null)
                        {
                            metadata.ViewId = view.ActiveViewportID;
                            metadata.ViewTitle = view.ActiveViewport.Name ?? "Unnamed View";
                            metadataList.Add(metadata);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnScreenshotError($"Failed to get viewport metadata: {ex.Message}");
            }
            
            return metadataList;
        }
        
        #endregion
        
        #region Private Screenshot Methods
        
        /// <summary>
        /// Captures the active viewport (synchronous)
        /// </summary>
        /// <param name="options">Screenshot options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Viewport screenshot</returns>
        private ViewportScreenshot? CaptureActiveViewport(ScreenshotOptions? options, CancellationToken cancellationToken)
        {
            try
            {
                OnScreenshotProgress("Preparing active viewport capture...", 0);
                
                var doc = RhinoDoc.ActiveDoc;
                var activeView = doc?.Views?.ActiveView;
                
                if (activeView?.ActiveViewport == null)
                {
                    OnScreenshotError("No active viewport found");
                    return null;
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                
                return CaptureViewportInternal(activeView.ActiveViewport, activeView.ActiveViewportID, options, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                OnScreenshotError("Screenshot capture was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                OnScreenshotError($"Failed to capture active viewport: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Captures a specific viewport (synchronous)
        /// </summary>
        /// <param name="viewportId">Viewport ID</param>
        /// <param name="options">Screenshot options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Viewport screenshot</returns>
        private ViewportScreenshot? CaptureViewport(Guid viewportId, ScreenshotOptions? options, CancellationToken cancellationToken)
        {
            try
            {
                OnScreenshotProgress($"Preparing viewport {viewportId} capture...", 0);
                
                var doc = RhinoDoc.ActiveDoc;
                if (doc?.Views == null)
                {
                    OnScreenshotError("No document or views available");
                    return null;
                }
                
                // Find the specified viewport
                RhinoViewport? targetViewport = null;
                foreach (var view in doc.Views)
                {
                    if (view.ActiveViewportID == viewportId && view.ActiveViewport != null)
                    {
                        targetViewport = view.ActiveViewport;
                        break;
                    }
                }
                
                if (targetViewport == null)
                {
                    OnScreenshotError($"Viewport with ID {viewportId} not found");
                    return null;
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                
                return CaptureViewportInternal(targetViewport, viewportId, options, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                OnScreenshotError("Screenshot capture was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                OnScreenshotError($"Failed to capture viewport {viewportId}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Captures all viewports (synchronous)
        /// </summary>
        /// <param name="options">Screenshot options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of viewport screenshots</returns>
        private IList<ViewportScreenshot> CaptureAllViewports(ScreenshotOptions? options, CancellationToken cancellationToken)
        {
            var screenshots = new List<ViewportScreenshot>();
            
            try
            {
                OnScreenshotProgress("Preparing multi-viewport capture...", 0);
                
                var doc = RhinoDoc.ActiveDoc;
                if (doc?.Views == null)
                {
                    OnScreenshotError("No document or views available");
                    return screenshots;
                }
                
                var views = new List<RhinoView>();
                foreach (var view in doc.Views)
                {
                    if (view?.ActiveViewport != null)
                        views.Add(view);
                }
                
                if (views.Count == 0)
                {
                    OnScreenshotError("No valid viewports found");
                    return screenshots;
                }
                
                for (int i = 0; i < views.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var view = views[i];
                    var progress = (int)((i / (double)views.Count) * 100);
                    OnScreenshotProgress($"Capturing viewport {i + 1} of {views.Count}...", progress);
                    
                    var screenshot = CaptureViewportInternal(view.ActiveViewport, view.ActiveViewportID, options, cancellationToken);
                    if (screenshot != null)
                    {
                        screenshots.Add(screenshot);
                    }
                }
                
                OnScreenshotProgress($"Captured {screenshots.Count} viewports", 100);
            }
            catch (OperationCanceledException)
            {
                OnScreenshotError("Multi-viewport capture was cancelled");
            }
            catch (Exception ex)
            {
                OnScreenshotError($"Failed to capture all viewports: {ex.Message}");
            }
            
            return screenshots;
        }
        
        /// <summary>
        /// Internal method for capturing a single viewport
        /// </summary>
        /// <param name="viewport">Rhino viewport</param>
        /// <param name="viewportId">Viewport ID</param>
        /// <param name="options">Screenshot options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Viewport screenshot</returns>
        private ViewportScreenshot? CaptureViewportInternal(
            RhinoViewport viewport, 
            Guid viewportId, 
            ScreenshotOptions? options, 
            CancellationToken cancellationToken)
        {
            try
            {
                options ??= new ScreenshotOptions();
                
                OnScreenshotProgress("Extracting viewport metadata...", 10);
                
                // Extract viewport metadata
                var metadata = ExtractViewportMetadata(viewport);
                if (metadata == null)
                {
                    OnScreenshotError("Failed to extract viewport metadata");
                    return null;
                }
                
                metadata.ViewId = viewportId;
                
                cancellationToken.ThrowIfCancellationRequested();
                
                OnScreenshotProgress("Capturing viewport image...", 30);
                
                // Capture the viewport image
                var imageData = CaptureViewportImage(viewport, options, cancellationToken);
                if (imageData == null || imageData.Length == 0)
                {
                    OnScreenshotError("Failed to capture viewport image");
                    return null;
                }
                
                OnScreenshotProgress("Processing image...", 60);
                
                // Process the image based on options
                var processedData = ProcessCapturedImage(imageData, options, cancellationToken);
                
                OnScreenshotProgress("Creating screenshot object...", 80);
                
                // Create the viewport screenshot
                var screenshot = new ViewportScreenshot
                {
                    Id = Guid.NewGuid().ToString(),
                    ImageData = processedData,
                    Metadata = metadata,
                    CapturedAt = DateTime.UtcNow,
                    CompressionType = options.CompressionType,
                    Quality = options.JpegQuality
                };
                
                // Set filename based on viewport name and timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var viewportName = viewport.Name?.Replace(" ", "_") ?? "Viewport";
                screenshot.SetFilename($"{viewportName}_{timestamp}");
                
                OnScreenshotProgress("Screenshot capture complete", 100);
                OnScreenshotCaptured(screenshot);
                
                return screenshot;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                OnScreenshotError($"Internal capture error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Captures the actual viewport image
        /// </summary>
        /// <param name="viewport">Rhino viewport</param>
        /// <param name="options">Screenshot options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Raw image data</returns>
        private byte[]? CaptureViewportImage(RhinoViewport viewport, ScreenshotOptions options, CancellationToken cancellationToken)
        {
            try
            {
                // Calculate capture dimensions
                var viewportSize = viewport.Size;
                var captureWidth = options.Width > 0 ? options.Width : viewportSize.Width;
                var captureHeight = options.Height > 0 ? options.Height : viewportSize.Height;
                
                // Apply scale factor
                if (options.ScaleFactor > 0 && options.ScaleFactor != 1.0)
                {
                    captureWidth = (int)(captureWidth * options.ScaleFactor);
                    captureHeight = (int)(captureHeight * options.ScaleFactor);
                }
                
                // Ensure minimum dimensions
                captureWidth = Math.Max(captureWidth, 100);
                captureHeight = Math.Max(captureHeight, 100);
                
                cancellationToken.ThrowIfCancellationRequested();
                
                // Create bitmap for capturing
                using var bitmap = new Bitmap(captureWidth, captureHeight, PixelFormat.Format24bppRgb);
                
                // Capture viewport to bitmap
                var captureSuccess = viewport.CaptureToBitmap(bitmap, options.IncludeGrid, options.IncludeAxes);
                
                if (!captureSuccess)
                {
                    OnScreenshotError("Viewport capture to bitmap failed");
                    return null;
                }
                
                cancellationToken.ThrowIfCancellationRequested();
                
                // Convert bitmap to byte array
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png); // Always capture as PNG first
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                OnScreenshotError($"Image capture failed: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Processes captured image data according to options
        /// </summary>
        /// <param name="imageData">Raw image data</param>
        /// <param name="options">Processing options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processed image data</returns>
        private byte[] ProcessCapturedImage(byte[] imageData, ScreenshotOptions options, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // If no compression requested, return as-is
                if (options.CompressionType == CompressionType.None)
                    return imageData;
                
                // Apply compression based on type
                switch (options.CompressionType)
                {
                    case CompressionType.Jpeg:
                        return ImageProcessor.CompressToJpeg(imageData, options.JpegQuality, options.MaxDimension);
                        
                    case CompressionType.Png:
                        return ImageProcessor.ConvertToPng(imageData, options.MaxDimension);
                        
                    default:
                        return imageData;
                }
            }
            catch (Exception ex)
            {
                OnScreenshotError($"Image processing failed: {ex.Message}");
                return imageData; // Return original data if processing fails
            }
        }
        
        #endregion
        
        #region Metadata Extraction
        
        /// <summary>
        /// Extracts metadata from a Rhino viewport
        /// </summary>
        /// <param name="viewport">Rhino viewport</param>
        /// <returns>Viewport metadata</returns>
        private ViewportMetadata? ExtractViewportMetadata(RhinoViewport viewport)
        {
            try
            {
                var metadata = new ViewportMetadata
                {
                    ViewId = viewport.Id,
                    ViewTitle = viewport.Name ?? "Unnamed Viewport",
                    ProjectionMode = GetProjectionMode(viewport),
                    CameraLocation = viewport.CameraLocation,
                    CameraDirection = viewport.CameraDirection,
                    CameraUp = viewport.CameraUp,
                    TargetPoint = viewport.TargetPoint,
                    ViewportSize = viewport.Size,
                    FrustumNear = viewport.FrustumNear,
                    FrustumFar = viewport.FrustumFar,
                    CapturedAt = DateTime.UtcNow
                };
                
                // Set lens length for perspective views
                if (viewport.IsPerspectiveProjection)
                {
                    metadata.LensLength = viewport.Camera35mmLensLength;
                }
                
                // Extract construction plane info
                var cplane = viewport.ConstructionPlane();
                metadata.ConstructionPlaneOrigin = cplane.Origin;
                metadata.ConstructionPlaneNormal = cplane.Normal;
                
                // Get document info if available
                var doc = RhinoDoc.ActiveDoc;
                if (doc != null)
                {
                    metadata.DocumentPath = doc.Path;
                    metadata.DocumentName = doc.Name;
                    metadata.RhinoVersion = RhinoApp.Version.ToString();
                    
                    // Count objects in document
                    metadata.ObjectCount = doc.Objects.Count;
                    
                    // Get units
                    metadata.ModelUnits = doc.ModelUnitSystem.ToString();
                }
                
                return metadata;
            }
            catch (Exception ex)
            {
                OnScreenshotError($"Failed to extract viewport metadata: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Determines the projection mode of a viewport
        /// </summary>
        /// <param name="viewport">Rhino viewport</param>
        /// <returns>Projection mode string</returns>
        private string GetProjectionMode(RhinoViewport viewport)
        {
            if (viewport.IsPerspectiveProjection)
                return "Perspective";
            
            if (viewport.IsParallelProjection)
            {
                // Try to determine the standard view
                var viewName = viewport.Name?.ToLowerInvariant();
                
                if (viewName != null)
                {
                    if (viewName.Contains("top")) return "Top";
                    if (viewName.Contains("bottom")) return "Bottom";
                    if (viewName.Contains("front")) return "Front";
                    if (viewName.Contains("back")) return "Back";
                    if (viewName.Contains("left")) return "Left";
                    if (viewName.Contains("right")) return "Right";
                }
                
                return "Parallel";
            }
            
            if (viewport.IsTwoPointPerspectiveProjection)
                return "Two-Point Perspective";
            
            return "Unknown";
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnScreenshotCaptured(ViewportScreenshot screenshot)
        {
            ScreenshotCaptured?.Invoke(this, new ScreenshotCapturedEventArgs(screenshot));
        }
        
        private void OnScreenshotError(string message)
        {
            ScreenshotError?.Invoke(this, new ScreenshotErrorEventArgs(message));
        }
        
        private void OnScreenshotProgress(string message, int percentage)
        {
            ScreenshotProgress?.Invoke(this, new ScreenshotProgressEventArgs(message, percentage));
        }
        
        #endregion
        
        #region Interface Implementation
        
        /// <summary>
        /// Captures the current active viewport as a screenshot (interface implementation)
        /// </summary>
        /// <param name="compressionQuality">JPEG compression quality (0-100), null for PNG</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The captured screenshot with metadata</returns>
        Task<ViewportScreenshot> IScreenshotService.CaptureViewportAsync(int? compressionQuality = null, CancellationToken cancellationToken = default)
        {
            var options = new ScreenshotOptions();
            if (compressionQuality.HasValue)
            {
                options.CompressionType = CompressionType.Jpeg;
                options.JpegQuality = compressionQuality.Value;
            }
            else
            {
                options.CompressionType = CompressionType.Png;
            }
            
            return Task.FromResult(CaptureActiveViewportAsync(options, cancellationToken).Result ?? new ViewportScreenshot());
        }
        
        /// <summary>
        /// Uploads a screenshot to the specified Vessel Studio project (interface implementation)
        /// </summary>
        /// <param name="screenshot">The screenshot to upload</param>
        /// <param name="projectId">The target project ID</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The upload transaction result</returns>
        public Task<UploadTransaction> UploadScreenshotAsync(ViewportScreenshot screenshot, string projectId, CancellationToken cancellationToken = default)
        {
            // This would integrate with the API client to upload the screenshot
            // For now, return a mock transaction
            var transaction = new UploadTransaction
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Status = UploadStatus.Pending,
                FileName = screenshot.FileName,
                FileSizeBytes = screenshot.FileSizeBytes,
                CreatedAt = DateTime.UtcNow
            };
            
            // Trigger upload progress event
            UploadProgress?.Invoke(this, new UploadProgressEventArgs(transaction.Id, 0, 0, screenshot.FileSizeBytes));
            
            // Simulate upload completion
            Task.Run(async () =>
            {
                await Task.Delay(1000, cancellationToken);
                transaction.Status = UploadStatus.Completed;
                transaction.CompletedAt = DateTime.UtcNow;
                UploadCompleted?.Invoke(this, new UploadCompletedEventArgs(transaction, true));
            }, cancellationToken);
            
            return Task.FromResult(transaction);
        }
        
        /// <summary>
        /// Captures the current viewport and uploads it to the specified project (interface implementation)
        /// </summary>
        /// <param name="projectId">The target project ID</param>
        /// <param name="compressionQuality">JPEG compression quality (0-100), null for PNG</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The upload transaction result</returns>
        public async Task<UploadTransaction> CaptureAndUploadAsync(string projectId, int? compressionQuality = null, CancellationToken cancellationToken = default)
        {
            var screenshot = await ((IScreenshotService)this).CaptureViewportAsync(compressionQuality, cancellationToken);
            return await UploadScreenshotAsync(screenshot, projectId, cancellationToken);
        }
        
        /// <summary>
        /// Gets the list of pending uploads in the queue (interface implementation)
        /// </summary>
        /// <returns>List of pending upload transactions</returns>
        public List<UploadTransaction> GetPendingUploads()
        {
            // Return empty list for now - would be populated by actual queue implementation
            return new List<UploadTransaction>();
        }
        
        /// <summary>
        /// Retries failed uploads that are eligible for retry (interface implementation)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Number of uploads that were retried</returns>
        public Task<int> RetryFailedUploadsAsync(CancellationToken cancellationToken = default)
        {
            // Return 0 for now - would retry actual failed uploads
            return Task.FromResult(0);
        }
        
        /// <summary>
        /// Cancels a pending or in-progress upload (interface implementation)
        /// </summary>
        /// <param name="transactionId">The transaction ID to cancel</param>
        /// <returns>True if the upload was cancelled, false if it couldn't be cancelled</returns>
        public bool CancelUpload(Guid transactionId)
        {
            // Return false for now - would cancel actual upload
            return false;
        }
        
        /// <summary>
        /// Gets the status of a specific upload transaction (interface implementation)
        /// </summary>
        /// <param name="transactionId">The transaction ID to check</param>
        /// <returns>The upload transaction status, or null if not found</returns>
        public UploadTransaction? GetUploadStatus(Guid transactionId)
        {
            // Return null for now - would look up actual transaction
            return null;
        }
        
        /// <summary>
        /// Clears completed uploads from the queue (interface implementation)
        /// </summary>
        /// <param name="olderThan">Only clear uploads older than this timespan (optional)</param>
        /// <returns>Number of uploads that were cleared</returns>
        public int ClearCompletedUploads(TimeSpan? olderThan = null)
        {
            // Return 0 for now - would clear actual completed uploads
            return 0;
        }
        
        /// <summary>
        /// Processes the upload queue (interface implementation)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        public Task ProcessUploadQueueAsync(CancellationToken cancellationToken = default)
        {
            // No-op for now - would process actual upload queue
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Gets upload statistics and queue information (interface implementation)
        /// </summary>
        /// <returns>Upload queue statistics</returns>
        public UploadQueueStats GetQueueStats()
        {
            return new UploadQueueStats
            {
                PendingCount = 0,
                InProgressCount = 0,
                CompletedCount = 0,
                FailedCount = 0,
                TotalBytesQueued = 0,
                LastProcessed = null,
                IsProcessorRunning = false
            };
        }
        
        /// <summary>
        /// Disposes resources used by the service (interface implementation)
        /// </summary>
        public void Dispose()
        {
            // Clean up any resources
        }
        
        #endregion
    }
    
    /// <summary>
    /// Options for viewport screenshot capture
    /// </summary>
    public class ScreenshotOptions
    {
        /// <summary>
        /// Capture width (0 = use viewport size)
        /// </summary>
        public int Width { get; set; } = 0;
        
        /// <summary>
        /// Capture height (0 = use viewport size)
        /// </summary>
        public int Height { get; set; } = 0;
        
        /// <summary>
        /// Scale factor for capture size
        /// </summary>
        public double ScaleFactor { get; set; } = 1.0;
        
        /// <summary>
        /// Maximum dimension for output image
        /// </summary>
        public int MaxDimension { get; set; } = 4096;
        
        /// <summary>
        /// Compression type to apply
        /// </summary>
        public CompressionType CompressionType { get; set; } = CompressionType.Jpeg;
        
        /// <summary>
        /// JPEG quality (0-100)
        /// </summary>
        public long JpegQuality { get; set; } = 85;
        
        /// <summary>
        /// Include construction plane grid in capture
        /// </summary>
        public bool IncludeGrid { get; set; } = true;
        
        /// <summary>
        /// Include viewport axes in capture
        /// </summary>
        public bool IncludeAxes { get; set; } = true;
        
        /// <summary>
        /// Creates default screenshot options
        /// </summary>
        /// <returns>Default options</returns>
        public static ScreenshotOptions Default()
        {
            return new ScreenshotOptions();
        }
        
        /// <summary>
        /// Creates high-quality screenshot options
        /// </summary>
        /// <returns>High-quality options</returns>
        public static ScreenshotOptions HighQuality()
        {
            return new ScreenshotOptions
            {
                ScaleFactor = 2.0,
                CompressionType = CompressionType.Png,
                MaxDimension = 8192
            };
        }
        
        /// <summary>
        /// Creates web-optimized screenshot options
        /// </summary>
        /// <returns>Web-optimized options</returns>
        public static ScreenshotOptions WebOptimized()
        {
            return new ScreenshotOptions
            {
                CompressionType = CompressionType.Jpeg,
                JpegQuality = 75,
                MaxDimension = 2048
            };
        }
    }
}