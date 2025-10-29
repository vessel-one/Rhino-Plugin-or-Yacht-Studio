using System.Collections.Generic;

namespace VesselStudioSimplePlugin.Models
{
    /// <summary>
    /// Result of a batch upload operation including success/failure counts and error details.
    /// </summary>
    public class BatchUploadResult
    {
        public bool Success { get; set; }
        public int UploadedCount { get; set; }
        public int FailedCount { get; set; }
        public List<(string filename, string error)> Errors { get; set; }
        public long TotalDurationMs { get; set; }
        public bool ApiKeyInvalid { get; set; }
        public bool SubscriptionInvalid { get; set; }
        
        public bool IsPartialSuccess => UploadedCount > 0 && FailedCount > 0;
        public bool IsCompleteFailure => UploadedCount == 0 && FailedCount > 0;

        public BatchUploadResult()
        {
            Errors = new List<(string filename, string error)>();
        }
    }
}
