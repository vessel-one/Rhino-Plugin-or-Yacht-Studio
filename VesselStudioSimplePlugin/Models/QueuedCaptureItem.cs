using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace VesselStudioSimplePlugin.Models
{
    /// <summary>
    /// Represents a single viewport capture in the queue awaiting batch upload.
    /// 
    /// Task T005: Create QueuedCaptureItem model with core fields
    /// Task T006: Implement IDisposable pattern for resource cleanup
    /// Task T007: Add validation in constructor
    /// Task T008: Implement GetThumbnail() method with caching
    /// </summary>
    public class QueuedCaptureItem : IDisposable
    {
        /// <summary>
        /// Unique identifier for this queue item.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Compressed JPEG image data (2-5MB typical).
        /// </summary>
        public byte[] ImageData { get; }

        /// <summary>
        /// Source viewport name (e.g., "Perspective", "Top", "Front").
        /// </summary>
        public string ViewportName { get; }

        /// <summary>
        /// When capture was added to queue (for chronological ordering).
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Position in batch (1-based, assigned at upload time).
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// Cached 80x60 thumbnail for UI display.
        /// </summary>
        private Bitmap _thumbnailCache;

        /// <summary>
        /// Indicates if this item has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Creates a new QueuedCaptureItem with the provided image data and viewport name.
        /// 
        /// T007: Validates all inputs according to specification:
        /// - ImageData must not be null or empty
        /// - ViewportName must not be null or whitespace
        /// - ImageData.Length must not exceed 5MB limit
        /// 
        /// Auto-assigns:
        /// - Id = Guid.NewGuid()
        /// - Timestamp = DateTime.Now
        /// - SequenceNumber = 0 (assigned by queue service later)
        /// </summary>
        /// <param name="imageData">The compressed image data bytes</param>
        /// <param name="viewportName">The name of the source viewport</param>
        /// <exception cref="ArgumentException">Thrown if validation fails</exception>
        public QueuedCaptureItem(byte[] imageData, string viewportName)
        {
            // T007: Validate imageData not null or empty
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));

            // T007: Validate imageData size (5MB limit = 5 * 1024 * 1024 bytes)
            const long maxSizeBytes = 5 * 1024 * 1024;
            if (imageData.Length > maxSizeBytes)
                throw new ArgumentException(
                    $"Image data exceeds maximum size of 5MB (provided: {imageData.Length} bytes)",
                    nameof(imageData));

            // T007: Validate viewportName not null or whitespace
            if (string.IsNullOrWhiteSpace(viewportName))
                throw new ArgumentException("Viewport name cannot be null or whitespace", nameof(viewportName));

            // Auto-assign fields as per T005 requirements
            Id = Guid.NewGuid();
            ImageData = imageData;
            ViewportName = viewportName;
            Timestamp = DateTime.Now;
            SequenceNumber = 0;  // Will be assigned by CaptureQueue at upload time
            _thumbnailCache = null;
            _disposed = false;
        }

        /// <summary>
        /// T008: Generates and caches an 80x60 pixel thumbnail from the full image data.
        /// 
        /// If a thumbnail has not been generated yet:
        /// - Loads image from ImageData byte array using MemoryStream
        /// - Scales to 80x60 using high-quality bicubic interpolation
        /// - Caches result in _thumbnailCache for subsequent calls
        /// 
        /// Returns the cached thumbnail on subsequent calls without regenerating.
        /// </summary>
        /// <returns>A 80x60 Bitmap thumbnail of the captured image</returns>
        /// <exception cref="ObjectDisposedException">Thrown if called after Dispose()</exception>
        /// <exception cref="Exception">Thrown if image data is invalid or cannot be decoded</exception>
        public Bitmap GetThumbnail()
        {
            if (_disposed)
                throw new ObjectDisposedException("QueuedCaptureItem", "Cannot get thumbnail from disposed item");

            // Return cached thumbnail if already generated
            if (_thumbnailCache != null)
                return _thumbnailCache;

            try
            {
                // T008: Load full image from ImageData byte array
                using (var memoryStream = new MemoryStream(ImageData))
                using (var fullImage = Image.FromStream(memoryStream))
                {
                    // T008: Scale to 80x60 with high-quality bicubic interpolation
                    _thumbnailCache = new Bitmap(fullImage, new Size(80, 60));

                    // Apply high-quality scaling settings
                    using (var graphics = Graphics.FromImage(_thumbnailCache))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to generate thumbnail from image data: {ex.Message}",
                    ex);
            }

            return _thumbnailCache;
        }

        /// <summary>
        /// T006: Implements IDisposable pattern to properly release thumbnail resources.
        /// 
        /// Disposes the cached thumbnail bitmap and clears references to allow
        /// garbage collection. This method should be called when the item is removed
        /// from the queue or the plugin unloads.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            // T006: Release thumbnail cache
            if (_thumbnailCache != null)
            {
                _thumbnailCache.Dispose();
                _thumbnailCache = null;
            }

            _disposed = true;
        }
    }
}
