namespace VesselStudioPlugin.Models
{
    /// <summary>
    /// Enumeration of possible upload statuses for viewport screenshots
    /// </summary>
    public enum UploadStatus
    {
        /// <summary>
        /// Upload is pending (not yet started)
        /// </summary>
        Pending = 0,
        
        /// <summary>
        /// Image has been captured but not yet queued for upload
        /// </summary>
        Captured = 1,
        
        /// <summary>
        /// Upload is queued and waiting to be processed
        /// </summary>
        Queued = 2,
        
        /// <summary>
        /// Upload is currently in progress
        /// </summary>
        InProgress = 3,
        
        /// <summary>
        /// Upload has completed successfully
        /// </summary>
        Completed = 4,
        
        /// <summary>
        /// Upload has failed and cannot be retried
        /// </summary>
        Failed = 5,
        
        /// <summary>
        /// Upload failed but is scheduled for retry
        /// </summary>
        Retrying = 6
    }
    
    /// <summary>
    /// Enumeration of authentication states for user sessions
    /// </summary>
    public enum AuthenticationState
    {
        /// <summary>
        /// User is not authenticated
        /// </summary>
        NotAuthenticated = 0,
        
        /// <summary>
        /// Authentication process is in progress
        /// </summary>
        Pending = 1,
        
        /// <summary>
        /// User is successfully authenticated with valid session
        /// </summary>
        Authenticated = 2,
        
        /// <summary>
        /// Session has expired and needs refresh
        /// </summary>
        Expired = 3,
        
        /// <summary>
        /// Authentication process has failed
        /// </summary>
        Failed = 4
    }
    
    /// <summary>
    /// Enumeration of image formats supported for viewport screenshots
    /// </summary>
    public enum ImageFormat
    {
        /// <summary>
        /// Portable Network Graphics - lossless compression
        /// </summary>
        PNG = 0,
        
        /// <summary>
        /// Joint Photographic Experts Group - lossy compression
        /// </summary>
        JPEG = 1
    }
    
    /// <summary>
    /// Enumeration of Rhino display modes
    /// </summary>
    public enum DisplayMode
    {
        /// <summary>
        /// Wireframe display mode
        /// </summary>
        Wireframe = 0,
        
        /// <summary>
        /// Shaded display mode
        /// </summary>
        Shaded = 1,
        
        /// <summary>
        /// Rendered display mode
        /// </summary>
        Rendered = 2,
        
        /// <summary>
        /// Ghosted display mode
        /// </summary>
        Ghosted = 3,
        
        /// <summary>
        /// X-Ray display mode
        /// </summary>
        XRay = 4,
        
        /// <summary>
        /// Technical display mode
        /// </summary>
        Technical = 5,
        
        /// <summary>
        /// Artistic display mode
        /// </summary>
        Artistic = 6,
        
        /// <summary>
        /// Custom or unknown display mode
        /// </summary>
        Custom = 99
    }
    
    /// <summary>
    /// Enumeration of compression types for image processing
    /// </summary>
    public enum CompressionType
    {
        /// <summary>
        /// PNG format - lossless compression
        /// </summary>
        Png = 0,
        
        /// <summary>
        /// JPEG format - lossy compression with adjustable quality
        /// </summary>
        Jpeg = 1
    }
    
    /// <summary>
    /// Upload state enumeration (alias for UploadStatus for API compatibility)
    /// </summary>
    public enum UploadState
    {
        /// <summary>
        /// Upload is pending
        /// </summary>
        Pending = 0,
        
        /// <summary>
        /// Upload is in progress
        /// </summary>
        Uploading = 3,
        
        /// <summary>
        /// Upload completed successfully
        /// </summary>
        Completed = 4,
        
        /// <summary>
        /// Upload failed
        /// </summary>
        Failed = 5
    }
}