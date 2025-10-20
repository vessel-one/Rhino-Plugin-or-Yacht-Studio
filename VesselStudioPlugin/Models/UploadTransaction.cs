using System;
using System.ComponentModel.DataAnnotations;

namespace VesselStudioPlugin.Models
{
    /// <summary>
    /// Tracks image upload process and provides status feedback
    /// </summary>
    public class UploadTransaction
    {
        #region Properties
        
        /// <summary>
        /// Gets or sets the unique transaction identifier
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Gets or sets the ID of the associated screenshot
        /// </summary>
        [Required]
        public Guid ScreenshotId { get; set; }
        
        /// <summary>
        /// Gets or sets the target project ID
        /// </summary>
        [Required]
        public string ProjectId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the current upload status
        /// </summary>
        public UploadStatus Status { get; set; } = UploadStatus.Queued;
        
        /// <summary>
        /// Gets or sets the upload completion percentage (0-100)
        /// </summary>
        [Range(0, 100)]
        public int Progress { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets when the upload was initiated
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets when the upload finished (success or failure)
        /// </summary>
        public DateTime? CompletionTime { get; set; }
        
        /// <summary>
        /// Gets or sets the error message if upload failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Gets or sets the number of retry attempts made
        /// </summary>
        [Range(0, int.MaxValue)]
        public int RetryCount { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets the server-assigned image ID after successful upload
        /// </summary>
        public string? RemoteImageId { get; set; }
        
        /// <summary>
        /// Gets or sets the URL of the uploaded image
        /// </summary>
        public string? RemoteImageUrl { get; set; }
        
        /// <summary>
        /// Gets or sets the number of bytes uploaded so far
        /// </summary>
        [Range(0, long.MaxValue)]
        public long BytesUploaded { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets the total number of bytes to upload
        /// </summary>
        [Range(0, long.MaxValue)]
        public long TotalBytes { get; set; } = 0;
        
        /// <summary>
        /// Gets or sets when the next retry should be attempted (if applicable)
        /// </summary>
        public DateTime? NextRetryTime { get; set; }
        
        /// <summary>
        /// Gets or sets the HTTP status code from the last upload attempt
        /// </summary>
        public int? LastHttpStatusCode { get; set; }
        
        /// <summary>
        /// Gets or sets additional metadata about the upload
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> Metadata { get; set; } = 
            new System.Collections.Generic.Dictionary<string, object>();
            
        /// <summary>
        /// Gets or sets the filename of the uploaded file
        /// </summary>
        public string? FileName { get; set; }
        
        /// <summary>
        /// Gets or sets the size of the file in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }
        
        /// <summary>
        /// Gets or sets when this transaction was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Alias properties for API compatibility
        /// <summary>
        /// Alias for StartTime - when the upload was started
        /// </summary>
        public DateTime StartedAt 
        { 
            get => StartTime; 
            set => StartTime = value; 
        }
        
        /// <summary>
        /// Alias for Status - current upload state
        /// </summary>
        public UploadStatus State 
        { 
            get => Status; 
            set => Status = value; 
        }
        
        /// <summary>
        /// Alias for CompletionTime - when the upload was completed
        /// </summary>
        public DateTime? CompletedAt 
        { 
            get => CompletionTime; 
            set => CompletionTime = value; 
        }
        
        /// <summary>
        /// Alias for BytesUploaded
        /// </summary>
        public long UploadedBytes 
        { 
            get => BytesUploaded; 
            set => BytesUploaded = value; 
        }
        
        /// <summary>
        /// Alias for RemoteImageId - server-assigned image ID
        /// </summary>
        public string? ServerImageId 
        { 
            get => RemoteImageId; 
            set => RemoteImageId = value; 
        }
        
        /// <summary>
        /// Alias for RemoteImageUrl - server URL of uploaded image
        /// </summary>
        public string? ServerUrl 
        { 
            get => RemoteImageUrl; 
            set => RemoteImageUrl = value; 
        }
        
        #endregion
        
        #region Computed Properties
        
        /// <summary>
        /// Gets whether the upload is currently active (in progress or queued)
        /// </summary>
        public bool IsActive => Status == UploadStatus.Queued || Status == UploadStatus.InProgress || Status == UploadStatus.Retrying;
        
        /// <summary>
        /// Gets whether the upload has completed (successfully or failed)
        /// </summary>
        public bool IsCompleted => Status == UploadStatus.Completed || Status == UploadStatus.Failed;
        
        /// <summary>
        /// Gets whether the upload can be retried
        /// </summary>
        public bool CanRetry => Status == UploadStatus.Failed && RetryCount < 3;
        
        /// <summary>
        /// Gets the duration of the upload (or elapsed time if still in progress)
        /// </summary>
        public TimeSpan Duration => (CompletionTime ?? DateTime.UtcNow) - StartTime;
        
        /// <summary>
        /// Gets the upload speed in bytes per second (if in progress or completed)
        /// </summary>
        public double? UploadSpeedBps
        {
            get
            {
                if (BytesUploaded == 0 || Duration.TotalSeconds < 1)
                    return null;
                return BytesUploaded / Duration.TotalSeconds;
            }
        }
        
        /// <summary>
        /// Gets the estimated time remaining for the upload
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining
        {
            get
            {
                if (!IsActive || BytesUploaded == 0 || Progress >= 100)
                    return null;
                    
                var remainingBytes = TotalBytes - BytesUploaded;
                var speed = UploadSpeedBps;
                
                if (speed.HasValue && speed.Value > 0)
                    return TimeSpan.FromSeconds(remainingBytes / speed.Value);
                    
                return null;
            }
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates the upload transaction for required fields and logical consistency
        /// </summary>
        /// <returns>True if transaction is valid, false otherwise</returns>
        public bool IsValid()
        {
            // Check required fields
            if (ScreenshotId == Guid.Empty)
                return false;
                
            if (string.IsNullOrWhiteSpace(ProjectId))
                return false;
            
            // Check progress range
            if (Progress < 0 || Progress > 100)
                return false;
            
            // Check retry count
            if (RetryCount < 0)
                return false;
            
            // Check byte counts
            if (BytesUploaded < 0 || TotalBytes < 0)
                return false;
                
            if (BytesUploaded > TotalBytes)
                return false;
            
            // Check status-specific requirements
            if (Status == UploadStatus.Completed)
            {
                if (Progress != 100)
                    return false;
                if (!CompletionTime.HasValue)
                    return false;
                if (string.IsNullOrWhiteSpace(RemoteImageId))
                    return false;
            }
            
            if (Status == UploadStatus.Failed)
            {
                if (string.IsNullOrWhiteSpace(ErrorMessage))
                    return false;
                if (!CompletionTime.HasValue)
                    return false;
            }
            
            // Check dates
            if (StartTime > DateTime.UtcNow.AddMinutes(1))
                return false;
                
            if (CompletionTime.HasValue && CompletionTime.Value < StartTime)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// Gets validation errors for this upload transaction
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public System.Collections.Generic.List<string> GetValidationErrors()
        {
            var errors = new System.Collections.Generic.List<string>();
            
            if (ScreenshotId == Guid.Empty)
                errors.Add("ScreenshotId cannot be empty");
                
            if (string.IsNullOrWhiteSpace(ProjectId))
                errors.Add("ProjectId cannot be empty");
            
            if (Progress < 0 || Progress > 100)
                errors.Add("Progress must be between 0 and 100");
            
            if (RetryCount < 0)
                errors.Add("RetryCount must be non-negative");
            
            if (BytesUploaded < 0)
                errors.Add("BytesUploaded must be non-negative");
                
            if (TotalBytes < 0)
                errors.Add("TotalBytes must be non-negative");
                
            if (BytesUploaded > TotalBytes)
                errors.Add("BytesUploaded cannot exceed TotalBytes");
            
            if (Status == UploadStatus.Completed)
            {
                if (Progress != 100)
                    errors.Add("Completed uploads must have 100% progress");
                if (!CompletionTime.HasValue)
                    errors.Add("Completed uploads must have completion time");
                if (string.IsNullOrWhiteSpace(RemoteImageId))
                    errors.Add("Completed uploads must have remote image ID");
            }
            
            if (Status == UploadStatus.Failed)
            {
                if (string.IsNullOrWhiteSpace(ErrorMessage))
                    errors.Add("Failed uploads must have error message");
                if (!CompletionTime.HasValue)
                    errors.Add("Failed uploads must have completion time");
            }
            
            if (StartTime > DateTime.UtcNow.AddMinutes(1))
                errors.Add("StartTime cannot be in the future");
                
            if (CompletionTime.HasValue && CompletionTime.Value < StartTime)
                errors.Add("CompletionTime cannot be before StartTime");
                
            return errors;
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Marks the upload as started
        /// </summary>
        public void MarkAsStarted()
        {
            Status = UploadStatus.InProgress;
            Progress = 0;
            StartTime = DateTime.UtcNow;
            CompletionTime = null;
            ErrorMessage = null;
        }
        
        /// <summary>
        /// Updates the upload progress
        /// </summary>
        /// <param name="progress">Progress percentage (0-100)</param>
        /// <param name="bytesUploaded">Number of bytes uploaded</param>
        public void UpdateProgress(int progress, long bytesUploaded)
        {
            Progress = Math.Max(0, Math.Min(100, progress));
            BytesUploaded = Math.Max(0, Math.Min(TotalBytes, bytesUploaded));
        }
        
        /// <summary>
        /// Marks the upload as completed successfully
        /// </summary>
        /// <param name="remoteImageId">The server-assigned image ID</param>
        /// <param name="remoteImageUrl">The URL of the uploaded image</param>
        public void MarkAsCompleted(string remoteImageId, string? remoteImageUrl = null)
        {
            Status = UploadStatus.Completed;
            Progress = 100;
            BytesUploaded = TotalBytes;
            CompletionTime = DateTime.UtcNow;
            RemoteImageId = remoteImageId;
            RemoteImageUrl = remoteImageUrl;
            ErrorMessage = null;
        }
        
        /// <summary>
        /// Marks the upload as failed
        /// </summary>
        /// <param name="errorMessage">The error message describing the failure</param>
        /// <param name="httpStatusCode">The HTTP status code if applicable</param>
        public void MarkAsFailed(string errorMessage, int? httpStatusCode = null)
        {
            Status = UploadStatus.Failed;
            CompletionTime = DateTime.UtcNow;
            ErrorMessage = errorMessage;
            LastHttpStatusCode = httpStatusCode;
        }
        
        /// <summary>
        /// Schedules the upload for retry
        /// </summary>
        /// <param name="delayMinutes">Minutes to wait before retry</param>
        public void ScheduleRetry(int delayMinutes = 5)
        {
            if (CanRetry)
            {
                Status = UploadStatus.Retrying;
                RetryCount++;
                NextRetryTime = DateTime.UtcNow.AddMinutes(delayMinutes);
                Progress = 0;
                BytesUploaded = 0;
            }
        }
        
        /// <summary>
        /// Checks if the upload is ready for retry
        /// </summary>
        /// <returns>True if ready for retry, false otherwise</returns>
        public bool IsReadyForRetry()
        {
            return Status == UploadStatus.Retrying && 
                   NextRetryTime.HasValue && 
                   NextRetryTime.Value <= DateTime.UtcNow;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Gets a human-readable status description
        /// </summary>
        /// <returns>Status description</returns>
        public string GetStatusDescription()
        {
            return Status switch
            {
                UploadStatus.Captured => "Captured, waiting to upload",
                UploadStatus.Queued => "Queued for upload",
                UploadStatus.InProgress => $"Uploading... {Progress}%",
                UploadStatus.Completed => "Upload completed successfully",
                UploadStatus.Failed => $"Upload failed: {ErrorMessage}",
                UploadStatus.Retrying => $"Retry scheduled (attempt {RetryCount}/3)",
                _ => "Unknown status"
            };
        }
        
        /// <summary>
        /// Gets upload speed as a formatted string
        /// </summary>
        /// <returns>Human-readable upload speed</returns>
        public string GetUploadSpeedDisplay()
        {
            var speed = UploadSpeedBps;
            if (!speed.HasValue)
                return "Unknown";
                
            const double KB = 1024;
            const double MB = KB * 1024;
            
            if (speed.Value >= MB)
                return $"{speed.Value / MB:F1} MB/s";
            else if (speed.Value >= KB)
                return $"{speed.Value / KB:F1} KB/s";
            else
                return $"{speed.Value:F0} B/s";
        }
        
        /// <summary>
        /// Gets estimated time remaining as a formatted string
        /// </summary>
        /// <returns>Human-readable time remaining</returns>
        public string GetTimeRemainingDisplay()
        {
            var timeRemaining = EstimatedTimeRemaining;
            if (!timeRemaining.HasValue)
                return "Unknown";
                
            if (timeRemaining.Value.TotalHours >= 1)
                return $"{timeRemaining.Value.Hours}h {timeRemaining.Value.Minutes}m";
            else if (timeRemaining.Value.TotalMinutes >= 1)
                return $"{timeRemaining.Value.Minutes}m {timeRemaining.Value.Seconds}s";
            else
                return $"{timeRemaining.Value.Seconds}s";
        }
        
        /// <summary>
        /// Creates a summary string of the upload transaction
        /// </summary>
        /// <returns>Human-readable transaction summary</returns>
        public string GetSummary()
        {
            return $"Upload {Id:N} - {GetStatusDescription()}, Duration: {Duration.TotalSeconds:F1}s";
        }
        
        #endregion
        
        #region Equality
        
        /// <summary>
        /// Determines whether the specified object is equal to this transaction
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object? obj)
        {
            if (obj is UploadTransaction other)
                return Id.Equals(other.Id);
            return false;
        }
        
        /// <summary>
        /// Returns a hash code for this transaction
        /// </summary>
        /// <returns>Hash code based on transaction ID</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        
        /// <summary>
        /// Returns a string representation of the transaction
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return GetSummary();
        }
        
        #endregion
    }
}