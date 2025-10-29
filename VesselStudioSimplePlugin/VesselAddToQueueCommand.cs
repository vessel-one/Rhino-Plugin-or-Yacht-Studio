using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Rhino;
using Rhino.Commands;
using VesselStudioSimplePlugin.Models;
using VesselStudioSimplePlugin.Services;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Command to add the current viewport capture to the batch queue for deferred upload.
    /// 
    /// Task T018: Create VesselAddToQueueCommand
    /// - Captures active viewport to JPEG
    /// - Creates QueuedCaptureItem from image data
    /// - Adds to CaptureQueueService singleton
    /// - Shows feedback message with queue count
    /// - Handles errors gracefully
    /// </summary>
    [System.Runtime.InteropServices.Guid("F7A8B9C0-1D2E-3F4A-5B6C-7D8E9F0A1B2C")]
    public class VesselAddToQueueCommand : Command
    {
#if DEV
        public override string EnglishName => "DevVesselAddToQueue";
#else
        public override string EnglishName => "VesselAddToQueue";
#endif

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // 1. Get active viewport
                var view = doc.Views.ActiveView;
                if (view == null)
                {
                    RhinoApp.WriteLine("❌ No active viewport. Please click in a viewport before capturing.");
                    return Result.Failure;
                }

                // 2. Capture viewport as bitmap
                RhinoApp.WriteLine("📸 Capturing viewport...");
                var bitmap = view.CaptureToBitmap();
                
                if (bitmap == null)
                {
                    RhinoApp.WriteLine("❌ Failed to capture viewport. Please try again.");
                    return Result.Failure;
                }

                // 3. Convert to image bytes with selected format
                byte[] imageData;
                try
                {
                    using (var ms = new MemoryStream())
                    {
                        var settings = VesselStudioSettings.Load();
                        
                        if (settings.ImageFormat?.ToLower() == "jpeg")
                        {
                            // JPEG format with user-selected quality
                            var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                            var encoderParams = new EncoderParameters(1);
                            encoderParams.Param[0] = new EncoderParameter(
                                System.Drawing.Imaging.Encoder.Quality, 
                                (long)settings.JpegQuality);
                            
                            bitmap.Save(ms, jpegEncoder, encoderParams);
                            RhinoApp.WriteLine($"📸 Compressed as JPEG (quality: {settings.JpegQuality}%)");
                        }
                        else
                        {
                            // PNG format (lossless, default)
                            bitmap.Save(ms, ImageFormat.Png);
                            RhinoApp.WriteLine("📸 Saved as PNG (lossless)");
                        }
                        
                        imageData = ms.ToArray();
                    }
                }
                finally
                {
                    bitmap?.Dispose();
                }

                // 4. Validate image size
                const long maxSizeBytes = 5 * 1024 * 1024; // 5MB
                if (imageData.Length > maxSizeBytes)
                {
                    RhinoApp.WriteLine($"❌ Captured image too large ({imageData.Length / (1024 * 1024)}MB). Maximum is 5MB.");
                    return Result.Failure;
                }

                // 5. Get queue service and add item
                var queueService = CaptureQueueService.Current;
                
                // Check if queue is at capacity
                if (!queueService.Queue.CanAddItems)
                {
                    RhinoApp.WriteLine($"❌ Queue is full (maximum 20 items). Please export the current batch before adding more.");
                    return Result.Failure;
                }

                // 6. Create QueuedCaptureItem and add to queue
                var viewportName = view.MainViewport.Name;
                var item = new QueuedCaptureItem(imageData, viewportName);
                queueService.AddItem(item);

                if (item == null)
                {
                    RhinoApp.WriteLine("❌ Failed to add capture to queue. Please try again.");
                    return Result.Failure;
                }

                // 7. Show success message with queue count
                var count = queueService.ItemCount;
                RhinoApp.WriteLine($"✅ Added to batch queue (Viewport: {viewportName})");
                RhinoApp.WriteLine($"📦 Queue: {count} item{(count == 1 ? "" : "s")} queued");

                if (count >= 10)
                {
                    RhinoApp.WriteLine($"💡 You have {count} captures queued. Ready to export anytime!");
                }

                return Result.Success;
            }
            catch (ArgumentException ex)
            {
                // Validation error from QueuedCaptureItem constructor
                RhinoApp.WriteLine($"❌ Validation error: {ex.Message}");
                return Result.Failure;
            }
            catch (InvalidOperationException ex)
            {
                // Queue limit or other state error
                RhinoApp.WriteLine($"❌ Queue error: {ex.Message}");
                return Result.Failure;
            }
            catch (Exception ex)
            {
                // Unexpected error
                RhinoApp.WriteLine($"❌ Unexpected error: {ex.Message}");
                return Result.Failure;
            }
        }

        /// <summary>
        /// Helper method to get the JPEG image codec encoder.
        /// Used for JPEG compression with quality setting.
        /// </summary>
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
