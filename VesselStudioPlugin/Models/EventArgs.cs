using System;

namespace VesselStudioPlugin.Models
{
    /// <summary>
    /// Event arguments for network status change events
    /// </summary>
    public class NetworkStatusChangedEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string? ConnectionType { get; }
        public DateTime Timestamp { get; }

        public NetworkStatusChangedEventArgs(bool isConnected, string? connectionType = null)
        {
            IsConnected = isConnected;
            ConnectionType = connectionType;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for API error events
    /// </summary>
    public class ApiErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string Endpoint { get; }
        public int? StatusCode { get; }
        public string? ErrorMessage { get; }
        public DateTime Timestamp { get; }

        public ApiErrorEventArgs(Exception exception, string endpoint, int? statusCode = null, string? errorMessage = null)
        {
            Exception = exception;
            Endpoint = endpoint;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for rate limit events
    /// </summary>
    public class RateLimitEventArgs : EventArgs
    {
        public int RequestsRemaining { get; }
        public TimeSpan ResetTime { get; }
        public string? LimitType { get; }
        public DateTime Timestamp { get; }

        public RateLimitEventArgs(int requestsRemaining, TimeSpan resetTime, string? limitType = null)
        {
            RequestsRemaining = requestsRemaining;
            ResetTime = resetTime;
            LimitType = limitType;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for token refresh events
    /// </summary>
    public class TokenRefreshedEventArgs : EventArgs
    {
        public string NewToken { get; }
        public DateTime ExpiryTime { get; }
        public DateTime Timestamp { get; }

        public TokenRefreshedEventArgs(string newToken, DateTime expiryTime)
        {
            NewToken = newToken;
            ExpiryTime = expiryTime;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for authentication error events
    /// </summary>
    public class AuthenticationErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string? ErrorCode { get; }
        public string? ErrorMessage { get; }
        public DateTime Timestamp { get; }

        public AuthenticationErrorEventArgs(Exception exception, string? errorCode = null, string? errorMessage = null)
        {
            Exception = exception;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for screenshot error events
    /// </summary>
    public class ScreenshotErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string? ViewportName { get; }
        public string? ErrorMessage { get; }
        public DateTime Timestamp { get; }

        public ScreenshotErrorEventArgs(Exception exception, string? viewportName = null, string? errorMessage = null)
        {
            Exception = exception;
            ViewportName = viewportName;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for screenshot progress events
    /// </summary>
    public class ScreenshotProgressEventArgs : EventArgs
    {
        public int ProgressPercentage { get; }
        public string StatusMessage { get; }
        public string? ViewportName { get; }
        public DateTime Timestamp { get; }

        public ScreenshotProgressEventArgs(int progressPercentage, string statusMessage, string? viewportName = null)
        {
            ProgressPercentage = progressPercentage;
            StatusMessage = statusMessage;
            ViewportName = viewportName;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Upload progress information
    /// </summary>
    public class UploadProgressInfo
    {
        public Guid UploadId { get; set; }
        public Guid? TransactionId { get; set; }
        public long BytesUploaded { get; set; }
        public long TotalBytes { get; set; }
        public int Percentage { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }

        public UploadProgressInfo(Guid uploadId, long bytesUploaded, long totalBytes, string message)
        {
            UploadId = uploadId;
            BytesUploaded = bytesUploaded;
            TotalBytes = totalBytes;
            Percentage = totalBytes > 0 ? (int)((bytesUploaded * 100) / totalBytes) : 0;
            Message = message;
            Timestamp = DateTime.UtcNow;
        }

        public UploadProgressInfo()
        {
            Message = "";
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Upload status information
    /// </summary>
    public class UploadStatusInfo
    {
        public Guid UploadId { get; }
        public UploadStatus Status { get; }
        public int ProgressPercentage { get; }
        public string? ErrorMessage { get; }
        public DateTime LastUpdate { get; }

        public UploadStatusInfo(Guid uploadId, UploadStatus status, int progressPercentage = 0, string? errorMessage = null)
        {
            UploadId = uploadId;
            Status = status;
            ProgressPercentage = progressPercentage;
            ErrorMessage = errorMessage;
            LastUpdate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// User information
    /// </summary>
    public class UserInfo
    {
        public string Id { get; }
        public string Username { get; }
        public string Email { get; }
        public string? DisplayName { get; }
        public DateTime? LastLoginTime { get; }

        public UserInfo(string id, string username, string email, string? displayName = null, DateTime? lastLoginTime = null)
        {
            Id = id;
            Username = username;
            Email = email;
            DisplayName = displayName;
            LastLoginTime = lastLoginTime;
        }
    }
}