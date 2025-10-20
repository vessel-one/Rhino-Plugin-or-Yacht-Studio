using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace VesselStudioPlugin.Utils
{
    /// <summary>
    /// Provides image processing utilities for viewport screenshots
    /// </summary>
    public static class ImageProcessor
    {
        #region Constants
        
        private const long DEFAULT_JPEG_QUALITY = 85L;
        private const int MAX_IMAGE_DIMENSION = 4096;
        private const long MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024; // 10MB
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Compresses an image to JPEG format with specified quality
        /// </summary>
        /// <param name="imageData">The original image data</param>
        /// <param name="quality">JPEG quality (0-100, default 85)</param>
        /// <param name="maxDimension">Maximum width or height (default 4096)</param>
        /// <returns>Compressed JPEG image data</returns>
        public static byte[] CompressToJpeg(byte[] imageData, long quality = DEFAULT_JPEG_QUALITY, int maxDimension = MAX_IMAGE_DIMENSION)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
                
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 0 and 100");
            
            using var inputStream = new MemoryStream(imageData);
            using var originalImage = Image.FromStream(inputStream);
            
            // Check if resizing is needed
            var (newWidth, newHeight) = CalculateResizeDimensions(originalImage.Width, originalImage.Height, maxDimension);
            
            using var processedImage = ResizeImage(originalImage, newWidth, newHeight);
            using var outputStream = new MemoryStream();
            
            // Set up JPEG encoder with quality parameter
            var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            
            processedImage.Save(outputStream, jpegEncoder, encoderParams);
            
            var result = outputStream.ToArray();
            
            // Validate result size
            if (result.Length > MAX_FILE_SIZE_BYTES)
            {
                throw new InvalidOperationException($"Compressed image size ({result.Length:N0} bytes) exceeds maximum allowed size ({MAX_FILE_SIZE_BYTES:N0} bytes)");
            }
            
            return result;
        }
        
        /// <summary>
        /// Converts an image to PNG format
        /// </summary>
        /// <param name="imageData">The original image data</param>
        /// <param name="maxDimension">Maximum width or height (default 4096)</param>
        /// <returns>PNG image data</returns>
        public static byte[] ConvertToPng(byte[] imageData, int maxDimension = MAX_IMAGE_DIMENSION)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
            
            using var inputStream = new MemoryStream(imageData);
            using var originalImage = Image.FromStream(inputStream);
            
            // Check if resizing is needed
            var (newWidth, newHeight) = CalculateResizeDimensions(originalImage.Width, originalImage.Height, maxDimension);
            
            using var processedImage = ResizeImage(originalImage, newWidth, newHeight);
            using var outputStream = new MemoryStream();
            
            processedImage.Save(outputStream, ImageFormat.Png);
            
            var result = outputStream.ToArray();
            
            // Validate result size
            if (result.Length > MAX_FILE_SIZE_BYTES)
            {
                throw new InvalidOperationException($"PNG image size ({result.Length:N0} bytes) exceeds maximum allowed size ({MAX_FILE_SIZE_BYTES:N0} bytes)");
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets image metadata without loading the full image into memory
        /// </summary>
        /// <param name="imageData">The image data</param>
        /// <returns>Image metadata information</returns>
        public static ImageMetadata GetImageMetadata(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
            
            using var inputStream = new MemoryStream(imageData);
            using var image = Image.FromStream(inputStream);
            
            var format = GetImageFormatName(image.RawFormat);
            var hasTransparency = HasTransparency(image);
            
            return new ImageMetadata
            {
                Width = image.Width,
                Height = image.Height,
                Format = format,
                FileSizeBytes = imageData.Length,
                HasTransparency = hasTransparency,
                DpiX = image.HorizontalResolution,
                DpiY = image.VerticalResolution,
                PixelFormat = image.PixelFormat.ToString()
            };
        }
        
        /// <summary>
        /// Validates that image data is a supported format and within size limits
        /// </summary>
        /// <param name="imageData">The image data to validate</param>
        /// <param name="maxSizeBytes">Maximum allowed file size in bytes</param>
        /// <returns>Validation result</returns>
        public static ImageValidationResult ValidateImage(byte[] imageData, long maxSizeBytes = MAX_FILE_SIZE_BYTES)
        {
            var result = new ImageValidationResult();
            
            if (imageData == null || imageData.Length == 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Image data is null or empty";
                return result;
            }
            
            if (imageData.Length > maxSizeBytes)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Image size ({imageData.Length:N0} bytes) exceeds maximum allowed size ({maxSizeBytes:N0} bytes)";
                return result;
            }
            
            try
            {
                using var stream = new MemoryStream(imageData);
                using var image = Image.FromStream(stream);
                
                result.Width = image.Width;
                result.Height = image.Height;
                result.Format = GetImageFormatName(image.RawFormat);
                result.IsValid = true;
                
                // Check for reasonable dimensions
                if (image.Width < 1 || image.Height < 1)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Image has invalid dimensions";
                }
                else if (image.Width > MAX_IMAGE_DIMENSION || image.Height > MAX_IMAGE_DIMENSION)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Image dimensions ({image.Width}x{image.Height}) exceed maximum allowed size ({MAX_IMAGE_DIMENSION}x{MAX_IMAGE_DIMENSION})";
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Invalid image format: {ex.Message}";
            }
            
            return result;
        }
        
        /// <summary>
        /// Creates a thumbnail image with specified maximum dimensions
        /// </summary>
        /// <param name="imageData">The original image data</param>
        /// <param name="maxWidth">Maximum thumbnail width</param>
        /// <param name="maxHeight">Maximum thumbnail height</param>
        /// <param name="asJpeg">Whether to return as JPEG (true) or PNG (false)</param>
        /// <returns>Thumbnail image data</returns>
        public static byte[] CreateThumbnail(byte[] imageData, int maxWidth = 256, int maxHeight = 256, bool asJpeg = true)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
            
            using var inputStream = new MemoryStream(imageData);
            using var originalImage = Image.FromStream(inputStream);
            
            // Calculate thumbnail dimensions maintaining aspect ratio
            var (thumbnailWidth, thumbnailHeight) = CalculateThumbnailDimensions(originalImage.Width, originalImage.Height, maxWidth, maxHeight);
            
            using var thumbnail = ResizeImage(originalImage, thumbnailWidth, thumbnailHeight);
            using var outputStream = new MemoryStream();
            
            if (asJpeg)
            {
                var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 80L);
                thumbnail.Save(outputStream, jpegEncoder, encoderParams);
            }
            else
            {
                thumbnail.Save(outputStream, ImageFormat.Png);
            }
            
            return outputStream.ToArray();
        }
        
        /// <summary>
        /// Estimates the file size after JPEG compression
        /// </summary>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <param name="quality">JPEG quality (0-100)</param>
        /// <returns>Estimated file size in bytes</returns>
        public static long EstimateJpegFileSize(int width, int height, long quality)
        {
            // Rough estimation based on typical JPEG compression ratios
            var pixelCount = (long)width * height;
            var baseSize = pixelCount * 3; // 3 bytes per pixel for RGB
            
            // Quality factor affects compression ratio
            var compressionRatio = quality switch
            {
                >= 95 => 0.15, // Low compression
                >= 85 => 0.10,
                >= 75 => 0.08,
                >= 65 => 0.06,
                >= 50 => 0.05,
                >= 25 => 0.04,
                _ => 0.03 // High compression
            };
            
            return (long)(baseSize * compressionRatio);
        }
        
        #endregion
        
        #region Private Helper Methods
        
        /// <summary>
        /// Calculates new dimensions for resizing while maintaining aspect ratio
        /// </summary>
        /// <param name="originalWidth">Original image width</param>
        /// <param name="originalHeight">Original image height</param>
        /// <param name="maxDimension">Maximum allowed width or height</param>
        /// <returns>New width and height</returns>
        private static (int width, int height) CalculateResizeDimensions(int originalWidth, int originalHeight, int maxDimension)
        {
            if (originalWidth <= maxDimension && originalHeight <= maxDimension)
                return (originalWidth, originalHeight);
            
            var aspectRatio = (double)originalWidth / originalHeight;
            
            if (originalWidth > originalHeight)
            {
                var newWidth = maxDimension;
                var newHeight = (int)(newWidth / aspectRatio);
                return (newWidth, newHeight);
            }
            else
            {
                var newHeight = maxDimension;
                var newWidth = (int)(newHeight * aspectRatio);
                return (newWidth, newHeight);
            }
        }
        
        /// <summary>
        /// Calculates thumbnail dimensions maintaining aspect ratio within bounds
        /// </summary>
        /// <param name="originalWidth">Original image width</param>
        /// <param name="originalHeight">Original image height</param>
        /// <param name="maxWidth">Maximum thumbnail width</param>
        /// <param name="maxHeight">Maximum thumbnail height</param>
        /// <returns>Thumbnail width and height</returns>
        private static (int width, int height) CalculateThumbnailDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            var aspectRatio = (double)originalWidth / originalHeight;
            
            var width = maxWidth;
            var height = (int)(width / aspectRatio);
            
            if (height > maxHeight)
            {
                height = maxHeight;
                width = (int)(height * aspectRatio);
            }
            
            return (width, height);
        }
        
        /// <summary>
        /// Resizes an image to the specified dimensions using high quality bicubic interpolation
        /// </summary>
        /// <param name="originalImage">The original image</param>
        /// <param name="newWidth">New width</param>
        /// <param name="newHeight">New height</param>
        /// <returns>Resized image</returns>
        private static Image ResizeImage(Image originalImage, int newWidth, int newHeight)
        {
            var resized = new Bitmap(newWidth, newHeight);
            
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.CompositingHint = System.Drawing.Drawing2D.CompositingHint.AssumeLinear;
                
                graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
            }
            
            return resized;
        }
        
        /// <summary>
        /// Gets the JPEG encoder
        /// </summary>
        /// <param name="format">Image format</param>
        /// <returns>Image codec info</returns>
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid)
                ?? throw new NotSupportedException($"Encoder for format {format} not found");
        }
        
        /// <summary>
        /// Gets a friendly name for an image format
        /// </summary>
        /// <param name="format">Raw image format</param>
        /// <returns>Format name</returns>
        private static string GetImageFormatName(ImageFormat format)
        {
            if (format.Equals(ImageFormat.Jpeg)) return "JPEG";
            if (format.Equals(ImageFormat.Png)) return "PNG";
            if (format.Equals(ImageFormat.Bmp)) return "BMP";
            if (format.Equals(ImageFormat.Gif)) return "GIF";
            if (format.Equals(ImageFormat.Tiff)) return "TIFF";
            return "Unknown";
        }
        
        /// <summary>
        /// Checks if an image has transparency
        /// </summary>
        /// <param name="image">The image to check</param>
        /// <returns>True if the image has transparency</returns>
        private static bool HasTransparency(Image image)
        {
            return (image.Flags & (int)ImageFlags.HasAlpha) != 0 ||
                   (image.Flags & (int)ImageFlags.HasTranslucent) != 0 ||
                   image.PixelFormat == PixelFormat.Format32bppArgb ||
                   image.PixelFormat == PixelFormat.Format16bppArgb1555;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Contains metadata information about an image
    /// </summary>
    public class ImageMetadata
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public bool HasTransparency { get; set; }
        public float DpiX { get; set; }
        public float DpiY { get; set; }
        public string PixelFormat { get; set; } = string.Empty;
        
        public override string ToString()
        {
            return $"{Width}x{Height} {Format} ({FileSizeBytes:N0} bytes)";
        }
    }
    
    /// <summary>
    /// Result of image validation
    /// </summary>
    public class ImageValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public string Format { get; set; } = string.Empty;
        
        public override string ToString()
        {
            return IsValid 
                ? $"Valid {Width}x{Height} {Format} image"
                : $"Invalid image: {ErrorMessage}";
        }
    }
}