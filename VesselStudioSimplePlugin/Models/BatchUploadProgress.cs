namespace VesselStudioSimplePlugin.Models
{
    /// <summary>
    /// Progress update sent to UI during batch upload operation.
    /// </summary>
    public class BatchUploadProgress
    {
        public int TotalItems { get; set; }
        public int CompletedItems { get; set; }
        public int FailedItems { get; set; }
        public string CurrentFilename { get; set; }
        
        public int PercentComplete => TotalItems > 0 
            ? (CompletedItems + FailedItems) * 100 / TotalItems 
            : 0;
    }
}
