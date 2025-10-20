using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VesselStudioPlugin.Models;

namespace VesselStudioPlugin.Services
{
    /// <summary>
    /// Interface for screenshot service that handles viewport capture and upload operations
    /// </summary>
    public interface IScreenshotService : IDisposable
    {
        /// <summary>
        /// Event raised when a screenshot capture is completed
        /// </summary>
        event EventHandler<ScreenshotCapturedEventArgs>? ScreenshotCaptured;
        
        /// <summary>
        /// Event raised when an upload operation progresses
        /// </summary>
        event EventHandler<UploadProgressEventArgs>? UploadProgress;
        
        /// <summary>
        /// Event raised when an upload operation completes
        /// </summary>
        event EventHandler<UploadCompletedEventArgs>? UploadCompleted;
        
        /// <summary>
        /// Captures the current active viewport as a screenshot
        /// </summary>
        /// <param name="compressionQuality">JPEG compression quality (0-100), null for PNG</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The captured screenshot with metadata</returns>
        Task<ViewportScreenshot> CaptureViewportAsync(int? compressionQuality = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Uploads a screenshot to the specified Vessel Studio project
        /// </summary>
        /// <param name="screenshot">The screenshot to upload</param>
        /// <param name="projectId">The target project ID</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The upload transaction result</returns>
        Task<UploadTransaction> UploadScreenshotAsync(ViewportScreenshot screenshot, string projectId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Captures the current viewport and uploads it to the specified project
        /// </summary>
        /// <param name="projectId">The target project ID</param>
        /// <param name="compressionQuality">JPEG compression quality (0-100), null for PNG</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The upload transaction result</returns>
        Task<UploadTransaction> CaptureAndUploadAsync(string projectId, int? compressionQuality = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the list of pending uploads in the queue
        /// </summary>
        /// <returns>List of pending upload transactions</returns>
        List<UploadTransaction> GetPendingUploads();
        
        /// <summary>
        /// Retries failed uploads that are eligible for retry
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Number of uploads that were retried</returns>
        Task<int> RetryFailedUploadsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Cancels a pending or in-progress upload
        /// </summary>
        /// <param name="transactionId">The transaction ID to cancel</param>
        /// <returns>True if the upload was cancelled, false if it couldn't be cancelled</returns>
        bool CancelUpload(Guid transactionId);
        
        /// <summary>
        /// Gets the status of a specific upload transaction
        /// </summary>
        /// <param name="transactionId">The transaction ID to check</param>
        /// <returns>The upload transaction status, or null if not found</returns>
        UploadTransaction? GetUploadStatus(Guid transactionId);
        
        /// <summary>
        /// Clears completed uploads from the queue
        /// </summary>
        /// <param name="olderThan">Only clear uploads older than this timespan (optional)</param>
        /// <returns>Number of uploads that were cleared</returns>
        int ClearCompletedUploads(TimeSpan? olderThan = null);
        
        /// <summary>
        /// Processes the upload queue (for background processing)
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        Task ProcessUploadQueueAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets upload statistics and queue information
        /// </summary>
        /// <returns>Upload queue statistics</returns>
        UploadQueueStats GetQueueStats();
        
        /// <summary>
        /// Captures the active viewport as a screenshot
        /// </summary>
        /// <param name="options">Screenshot capture options</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>The captured screenshot</returns>
        Task<ViewportScreenshot?> CaptureActiveViewportAsync(ScreenshotOptions options, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets metadata about the active viewport
        /// </summary>
        /// <returns>Viewport metadata or null if no active viewport</returns>
        ViewportMetadata? GetActiveViewportMetadata();
    }
    
    /// <summary>
    /// Event arguments for screenshot capture completion
    /// </summary>
    public class ScreenshotCapturedEventArgs : EventArgs
    {
        public ViewportScreenshot Screenshot { get; }
        public TimeSpan CaptureTime { get; }
        
        public ScreenshotCapturedEventArgs(ViewportScreenshot screenshot, TimeSpan captureTime)
        {
            Screenshot = screenshot;
            CaptureTime = captureTime;
        }
    }
    
    /// <summary>
    /// Event arguments for upload progress
    /// </summary>
    public class UploadProgressEventArgs : EventArgs
    {
        public Guid TransactionId { get; }
        public int ProgressPercentage { get; }
        public long BytesUploaded { get; }
        public long TotalBytes { get; }
        
        public UploadProgressEventArgs(Guid transactionId, int progressPercentage, long bytesUploaded, long totalBytes)
        {
            TransactionId = transactionId;
            ProgressPercentage = progressPercentage;
            BytesUploaded = bytesUploaded;
            TotalBytes = totalBytes;
        }
    }
    
    /// <summary>
    /// Event arguments for upload completion
    /// </summary>
    public class UploadCompletedEventArgs : EventArgs
    {
        public UploadTransaction Transaction { get; }
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }
        
        public UploadCompletedEventArgs(UploadTransaction transaction, bool isSuccess, string? errorMessage = null)
        {
            Transaction = transaction;
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }
    }
    
    /// <summary>
    /// Statistics about the upload queue
    /// </summary>
    public class UploadQueueStats
    {
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int FailedCount { get; set; }
        public long TotalBytesQueued { get; set; }
        public DateTime? LastProcessed { get; set; }
        public bool IsProcessorRunning { get; set; }
    }
}