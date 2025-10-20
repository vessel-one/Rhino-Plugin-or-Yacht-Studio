using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace VesselStudioPlugin.Models
{
    /// <summary>
    /// Represents a captured viewport image with associated metadata
    /// </summary>
    public class ViewportScreenshot
    {
        #region Properties
        
        /// <summary>
        /// Gets or sets the unique identifier for this screenshot
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Gets or sets the binary image data
        /// </summary>
        public byte[]? ImageData { get; set; }
        
        /// <summary>
        /// Gets or sets the image format (PNG or JPEG)
        /// </summary>
        public ImageFormat Format { get; set; } = ImageFormat.PNG;
        
        /// <summary>
        /// Gets or sets the local storage file path (alternative to ImageData)
        /// </summary>
        public string? FilePath { get; set; }
        
        /// <summary>
        /// Gets or sets the image size in bytes
        /// </summary>
        [Range(0, long.MaxValue)]
        public long FileSize { get; set; }
        
        /// <summary>
        /// Gets or sets when the viewport was captured
        /// </summary>
        public DateTime CaptureTimestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the viewport metadata associated with this screenshot
        /// </summary>
        [Required]
        public ViewportMetadata ViewportMetadata { get; set; } = new ViewportMetadata();
        
        /// <summary>
        /// Gets or sets the current upload status
        /// </summary>
        public UploadStatus UploadStatus { get; set; } = UploadStatus.Captured;
        
        /// <summary>
        /// Gets or sets the target Vessel Studio project ID for upload
        /// </summary>
        public string? ProjectId { get; set; }
        
        /// <summary>
        /// Gets or sets the URL of the image after successful upload
        /// </summary>
        public string? RemoteUrl { get; set; }
        
        /// <summary>
        /// Gets or sets the server-assigned image ID after upload
        /// </summary>
        public string? RemoteImageId { get; set; }
        
        /// <summary>
        /// Gets or sets the compression quality used (for JPEG format)
        /// </summary>
        [Range(1, 100)]
        public int? CompressionQuality { get; set; }
        
        /// <summary>
        /// Gets or sets the original uncompressed size (if compression was applied)
        /// </summary>
        public long? OriginalSize { get; set; }
        
        /// <summary>
        /// Gets or sets additional metadata about the screenshot
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> AdditionalMetadata { get; set; } = 
            new System.Collections.Generic.Dictionary<string, object>();
            
        // Alias properties for API compatibility
        /// <summary>
        /// Alias for ViewportMetadata - metadata about the viewport state
        /// </summary>
        public ViewportMetadata Metadata 
        { 
            get => ViewportMetadata; 
            set => ViewportMetadata = value; 
        }
        
        /// <summary>
        /// Alias for CaptureTimestamp - when the screenshot was captured
        /// </summary>
        public DateTime CapturedAt 
        { 
            get => CaptureTimestamp; 
            set => CaptureTimestamp = value; 
        }
        
        /// <summary>
        /// Gets or sets the compression type used for this screenshot
        /// </summary>
        public CompressionType CompressionType { get; set; } = CompressionType.Png;
        
        /// <summary>
        /// Alias for CompressionQuality - compression quality setting
        /// </summary>
        public int? Quality 
        { 
            get => CompressionQuality; 
            set => CompressionQuality = value; 
        }
        
        /// <summary>
        /// Gets or sets the filename for this screenshot
        /// </summary>
        public string? Filename { get; set; }
        
        /// <summary>
        /// Alias for Filename - alternate property name
        /// </summary>
        public string? FileName 
        { 
            get => Filename; 
            set => Filename = value; 
        }
        
        /// <summary>
        /// Alias for FileSize - file size in bytes
        /// </summary>
        public long FileSizeBytes 
        { 
            get => FileSize; 
            set => FileSize = value; 
        }
        
        #endregion
        
        #region Computed Properties
        
        /// <summary>
        /// Gets whether the screenshot has been uploaded successfully
        /// </summary>
        public bool IsUploaded => UploadStatus == UploadStatus.Completed && !string.IsNullOrWhiteSpace(RemoteImageId);
        
        /// <summary>
        /// Gets whether the screenshot is ready for upload
        /// </summary>
        public bool IsReadyForUpload => (ImageData != null || File.Exists(FilePath)) && 
                                        !string.IsNullOrWhiteSpace(ProjectId) && 
                                        UploadStatus == UploadStatus.Captured;
        
        /// <summary>
        /// Gets the effective file size (from ImageData or file on disk)
        /// </summary>
        public long EffectiveFileSize
        {
            get
            {
                if (ImageData != null)
                    return ImageData.Length;
                    
                if (!string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
                    return new FileInfo(FilePath).Length;
                    
                return FileSize;
            }
        }
        
        /// <summary>
        /// Gets whether compression was applied to this screenshot
        /// </summary>
        public bool IsCompressed => Format == ImageFormat.JPEG || 
                                    (OriginalSize.HasValue && OriginalSize.Value > EffectiveFileSize);
        
        /// <summary>
        /// Gets the compression ratio if compression was applied
        /// </summary>
        public double? CompressionRatio
        {
            get
            {
                if (!OriginalSize.HasValue || OriginalSize.Value == 0)
                    return null;
                    
                return EffectiveFileSize / (double)OriginalSize.Value;
            }
        }
        
        /// <summary>
        /// Gets the file extension based on the image format
        /// </summary>
        public string FileExtension => Format switch
        {
            ImageFormat.PNG => ".png",
            ImageFormat.JPEG => ".jpg",
            _ => ".png"
        };
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates the screenshot for required fields and logical consistency
        /// </summary>
        /// <returns>True if screenshot is valid, false otherwise</returns>
        public bool IsValid()
        {
            // Must have either image data or valid file path
            if (ImageData == null && (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath)))
                return false;
            
            // Both ImageData and FilePath should not be set simultaneously
            if (ImageData != null && !string.IsNullOrWhiteSpace(FilePath))
                return false;
            
            // File size must be positive and within limits (10MB max per spec)
            var effectiveSize = EffectiveFileSize;
            if (effectiveSize <= 0 || effectiveSize > 10 * 1024 * 1024)
                return false;
            
            // Capture timestamp should not be in the future
            if (CaptureTimestamp > DateTime.UtcNow.AddMinutes(1))
                return false;
            
            // Validate viewport metadata
            if (ViewportMetadata == null || !ViewportMetadata.IsValid())
                return false;
            
            // Compression quality validation for JPEG
            if (Format == ImageFormat.JPEG && CompressionQuality.HasValue)
            {
                if (CompressionQuality.Value < 1 || CompressionQuality.Value > 100)
                    return false;
            }
            
            // Original size should be larger than compressed size
            if (OriginalSize.HasValue && OriginalSize.Value < effectiveSize)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Gets validation errors for this screenshot
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public System.Collections.Generic.List<string> GetValidationErrors()
        {
            var errors = new System.Collections.Generic.List<string>();
            
            if (ImageData == null && (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath)))
                errors.Add("Screenshot must have either ImageData or valid FilePath");
            
            if (ImageData != null && !string.IsNullOrWhiteSpace(FilePath))
                errors.Add("Screenshot cannot have both ImageData and FilePath set");
            
            var effectiveSize = EffectiveFileSize;
            if (effectiveSize <= 0)
                errors.Add("File size must be greater than 0");
            else if (effectiveSize > 10 * 1024 * 1024)
                errors.Add("File size cannot exceed 10MB");
            
            if (CaptureTimestamp > DateTime.UtcNow.AddMinutes(1))
                errors.Add("Capture timestamp cannot be in the future");
            
            if (ViewportMetadata == null)
                errors.Add("ViewportMetadata is required");
            else
                errors.AddRange(ViewportMetadata.GetValidationErrors());
            
            if (Format == ImageFormat.JPEG && CompressionQuality.HasValue)
            {
                if (CompressionQuality.Value < 1 || CompressionQuality.Value > 100)
                    errors.Add("JPEG compression quality must be between 1 and 100");
            }
            
            if (OriginalSize.HasValue && OriginalSize.Value < effectiveSize)
                errors.Add("Original size cannot be smaller than compressed size");
                
            return errors;
        }
        
        #endregion
        
        #region File Operations
        
        /// <summary>
        /// Loads image data from the file path into memory
        /// </summary>
        /// <returns>True if loaded successfully, false otherwise</returns>
        public bool LoadImageData()
        {
            if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
                return false;
                
            try
            {
                ImageData = File.ReadAllBytes(FilePath);
                FileSize = ImageData.Length;
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Saves image data to the specified file path
        /// </summary>
        /// <param name="filePath">The file path to save to</param>
        /// <returns>True if saved successfully, false otherwise</returns>
        public bool SaveImageData(string filePath)
        {
            if (ImageData == null || ImageData.Length == 0)
                return false;
                
            try
            {
                File.WriteAllBytes(filePath, ImageData);
                FilePath = filePath;
                FileSize = ImageData.Length;
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Saves image data to a temporary file
        /// </summary>
        /// <returns>The temporary file path, or null if failed</returns>
        public string? SaveToTemporaryFile()
        {
            if (ImageData == null)
                return null;
                
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"VesselStudio_{Id:N}{FileExtension}");
                if (SaveImageData(tempPath))
                    return tempPath;
            }
            catch
            {
                // Fall through to return null
            }
            
            return null;
        }
        
        /// <summary>
        /// Clears image data from memory (keeping file path if available)
        /// </summary>
        public void ClearImageData()
        {
            ImageData = null;
        }
        
        /// <summary>
        /// Deletes the associated file if it exists
        /// </summary>
        /// <returns>True if file was deleted or didn't exist, false if deletion failed</returns>
        public bool DeleteFile()
        {
            if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
                return true;
                
            try
            {
                File.Delete(FilePath);
                FilePath = null;
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Gets a human-readable description of the file size
        /// </summary>
        /// <returns>Formatted file size</returns>
        public string GetFileSizeDisplay()
        {
            var size = EffectiveFileSize;
            const long KB = 1024;
            const long MB = KB * 1024;
            
            if (size >= MB)
                return $"{size / (double)MB:F1} MB";
            else if (size >= KB)
                return $"{size / (double)KB:F1} KB";
            else
                return $"{size} bytes";
        }
        
        /// <summary>
        /// Gets a display name for the screenshot
        /// </summary>
        /// <returns>Human-readable screenshot name</returns>
        public string GetDisplayName()
        {
            var timestamp = CaptureTimestamp.ToString("yyyy-MM-dd HH:mm:ss");
            var viewport = ViewportMetadata?.ViewportName ?? "Unknown";
            return $"{viewport} - {timestamp}";
        }
        
        /// <summary>
        /// Gets compression information as a string
        /// </summary>
        /// <returns>Compression details</returns>
        public string GetCompressionInfo()
        {
            if (!IsCompressed)
                return "No compression";
                
            if (CompressionRatio.HasValue)
                return $"{Format} compression, {CompressionRatio.Value:P1} of original";
            else if (CompressionQuality.HasValue)
                return $"{Format} compression, quality {CompressionQuality.Value}%";
            else
                return $"{Format} compression";
        }
        
        /// <summary>
        /// Creates a summary string of the screenshot
        /// </summary>
        /// <returns>Human-readable screenshot summary</returns>
        public string GetSummary()
        {
            return $"{GetDisplayName()} - {GetFileSizeDisplay()}, {GetCompressionInfo()}, Status: {UploadStatus}";
        }
        
        /// <summary>
        /// Generates a suggested filename for this screenshot
        /// </summary>
        /// <returns>Suggested filename</returns>
        public string GetSuggestedFileName()
        {
            var timestamp = CaptureTimestamp.ToString("yyyyMMdd_HHmmss");
            var viewport = ViewportMetadata?.ViewportName?.Replace(" ", "_") ?? "Viewport";
            return $"VesselStudio_{viewport}_{timestamp}{FileExtension}";
        }
        
        #endregion
        
        #region Equality
        
        /// <summary>
        /// Determines whether the specified object is equal to this screenshot
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object? obj)
        {
            if (obj is ViewportScreenshot other)
                return Id.Equals(other.Id);
            return false;
        }
        
        /// <summary>
        /// Returns a hash code for this screenshot
        /// </summary>
        /// <returns>Hash code based on screenshot ID</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        
        /// <summary>
        /// Returns a string representation of the screenshot
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return GetSummary();
        }
        
        #endregion
    }
}