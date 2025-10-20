using System;
using System.ComponentModel.DataAnnotations;

namespace VesselStudioPlugin.Models
{
    /// <summary>
    /// Represents Vessel Studio project metadata for selection interface
    /// </summary>
    public class ProjectInfo
    {
        #region Properties
        
        /// <summary>
        /// Gets or sets the Vessel Studio project identifier
        /// </summary>
        [Required]
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the user-friendly project name
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the project description
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Gets or sets when the project was last modified
        /// </summary>
        public DateTime LastModified { get; set; }
        
        /// <summary>
        /// Gets or sets the number of images currently in the project
        /// </summary>
        public int ImageCount { get; set; }
        
        /// <summary>
        /// Gets or sets whether the user has upload permissions for this project
        /// </summary>
        public bool IsAccessible { get; set; } = true;
        
        /// <summary>
        /// Gets or sets the URL for the project's thumbnail image
        /// </summary>
        public string? ThumbnailUrl { get; set; }
        
        /// <summary>
        /// Gets or sets the project owner's user ID
        /// </summary>
        public string? OwnerId { get; set; }
        
        /// <summary>
        /// Gets or sets the project owner's display name
        /// </summary>
        public string? OwnerName { get; set; }
        
        /// <summary>
        /// Gets or sets when the project was created
        /// </summary>
        public DateTime CreatedDate { get; set; }
        
        /// <summary>
        /// Gets or sets the project's privacy setting
        /// </summary>
        public bool IsPublic { get; set; }
        
        /// <summary>
        /// Gets or sets the project's status (active, archived, etc.)
        /// </summary>
        public string Status { get; set; } = "active";
        
        /// <summary>
        /// Gets or sets the total storage used by the project in bytes
        /// </summary>
        public long StorageUsed { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum storage allowed for the project in bytes
        /// </summary>
        public long? StorageLimit { get; set; }
        
        // Alias properties for API compatibility
        /// <summary>
        /// Alias for CreatedDate - when the project was created
        /// </summary>
        public DateTime CreatedAt 
        { 
            get => CreatedDate; 
            set => CreatedDate = value; 
        }
        
        /// <summary>
        /// Alias for LastModified - when the project was last updated
        /// </summary>
        public DateTime UpdatedAt 
        { 
            get => LastModified; 
            set => LastModified = value; 
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Validates the project info for required fields and logical consistency
        /// </summary>
        /// <returns>True if project info is valid, false otherwise</returns>
        public bool IsValid()
        {
            // Check required fields
            if (string.IsNullOrWhiteSpace(Id))
                return false;
                
            if (string.IsNullOrWhiteSpace(Name))
                return false;
            
            // Check counts and sizes
            if (ImageCount < 0)
                return false;
                
            if (StorageUsed < 0)
                return false;
                
            if (StorageLimit.HasValue && StorageLimit.Value < 0)
                return false;
            
            // Check dates
            if (LastModified > DateTime.UtcNow.AddMinutes(1))
                return false;
                
            if (CreatedDate > DateTime.UtcNow.AddMinutes(1))
                return false;
                
            if (CreatedDate > LastModified)
                return false;
                
            return true;
        }
        
        /// <summary>
        /// Gets validation errors for this project info
        /// </summary>
        /// <returns>List of validation error messages</returns>
        public System.Collections.Generic.List<string> GetValidationErrors()
        {
            var errors = new System.Collections.Generic.List<string>();
            
            if (string.IsNullOrWhiteSpace(Id))
                errors.Add("Project Id cannot be empty");
                
            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Project Name cannot be empty");
            
            if (ImageCount < 0)
                errors.Add("ImageCount must be non-negative");
                
            if (StorageUsed < 0)
                errors.Add("StorageUsed must be non-negative");
                
            if (StorageLimit.HasValue && StorageLimit.Value < 0)
                errors.Add("StorageLimit must be non-negative");
            
            if (LastModified > DateTime.UtcNow.AddMinutes(1))
                errors.Add("LastModified cannot be in the future");
                
            if (CreatedDate > DateTime.UtcNow.AddMinutes(1))
                errors.Add("CreatedDate cannot be in the future");
                
            if (CreatedDate > LastModified)
                errors.Add("CreatedDate cannot be after LastModified");
                
            return errors;
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Gets a display-friendly version of the project name
        /// </summary>
        /// <returns>Project name with owner info if available</returns>
        public string GetDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(OwnerName))
                return $"{Name} (by {OwnerName})";
            return Name;
        }
        
        /// <summary>
        /// Gets the storage usage as a formatted string
        /// </summary>
        /// <returns>Human-readable storage usage</returns>
        public string GetStorageUsageDisplay()
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;
            
            if (StorageUsed >= GB)
                return $"{StorageUsed / (double)GB:F1} GB";
            else if (StorageUsed >= MB)
                return $"{StorageUsed / (double)MB:F1} MB";
            else if (StorageUsed >= KB)
                return $"{StorageUsed / (double)KB:F1} KB";
            else
                return $"{StorageUsed} bytes";
        }
        
        /// <summary>
        /// Gets the storage usage percentage if limit is set
        /// </summary>
        /// <returns>Storage usage percentage (0-100) or null if no limit</returns>
        public double? GetStorageUsagePercentage()
        {
            if (!StorageLimit.HasValue || StorageLimit.Value == 0)
                return null;
                
            return Math.Min(100.0, (StorageUsed / (double)StorageLimit.Value) * 100.0);
        }
        
        /// <summary>
        /// Checks if the project is approaching its storage limit
        /// </summary>
        /// <param name="warningThreshold">Percentage threshold for warning (default 80%)</param>
        /// <returns>True if approaching limit, false otherwise</returns>
        public bool IsApproachingStorageLimit(double warningThreshold = 80.0)
        {
            var usage = GetStorageUsagePercentage();
            return usage.HasValue && usage.Value >= warningThreshold;
        }
        
        /// <summary>
        /// Gets how long ago the project was last modified
        /// </summary>
        /// <returns>Timespan since last modification</returns>
        public TimeSpan GetTimeSinceLastModified()
        {
            return DateTime.UtcNow - LastModified;
        }
        
        /// <summary>
        /// Gets a human-readable description of when the project was last modified
        /// </summary>
        /// <returns>Relative time description</returns>
        public string GetLastModifiedDisplay()
        {
            var timeSpan = GetTimeSinceLastModified();
            
            if (timeSpan.TotalDays >= 365)
                return $"{(int)(timeSpan.TotalDays / 365)} year(s) ago";
            else if (timeSpan.TotalDays >= 30)
                return $"{(int)(timeSpan.TotalDays / 30)} month(s) ago";
            else if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} day(s) ago";
            else if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} hour(s) ago";
            else if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} minute(s) ago";
            else
                return "Just now";
        }
        
        /// <summary>
        /// Creates a summary string of the project
        /// </summary>
        /// <returns>Human-readable project summary</returns>
        public string GetSummary()
        {
            return $"{GetDisplayName()} - {ImageCount} images, " +
                   $"{GetStorageUsageDisplay()}, modified {GetLastModifiedDisplay()}";
        }
        
        #endregion
        
        #region Equality
        
        /// <summary>
        /// Determines whether the specified object is equal to this project
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if equal, false otherwise</returns>
        public override bool Equals(object? obj)
        {
            if (obj is ProjectInfo other)
                return string.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase);
            return false;
        }
        
        /// <summary>
        /// Returns a hash code for this project
        /// </summary>
        /// <returns>Hash code based on project ID</returns>
        public override int GetHashCode()
        {
            return Id?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
        }
        
        /// <summary>
        /// Returns a string representation of the project
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return GetSummary();
        }
        
        #endregion
    }
}