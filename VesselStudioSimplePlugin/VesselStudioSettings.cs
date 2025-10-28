using System;
using System.IO;
using Newtonsoft.Json;
using Rhino;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Persistent settings for Vessel Studio plugin
    /// </summary>
    public class VesselStudioSettings
    {
        public string ApiKey { get; set; }
        public string LastProjectId { get; set; }
        public string LastProjectName { get; set; }
        
        // Subscription status caching
        public bool HasValidSubscription { get; set; } = true; // Default to true for backwards compatibility
        public DateTime LastSubscriptionCheck { get; set; }
        public string SubscriptionErrorMessage { get; set; }
        public string UpgradeUrl { get; set; }

        // Image format settings (Phase 5 Group 3 Enhancement)
        /// <summary>
        /// Image format for captures: "jpeg" (default) or "png"
        /// </summary>
        public string ImageFormat { get; set; } = "png"; // Changed default to PNG for better quality
        
        /// <summary>
        /// JPEG quality (1-100). Only used when ImageFormat is "jpeg".
        /// 95 = high quality with good compression
        /// </summary>
        public int JpegQuality { get; set; } = 95;

        /// <summary>
        /// Check if subscription should be revalidated (every hour)
        /// </summary>
        public bool ShouldRecheckSubscription()
        {
            return (DateTime.Now - LastSubscriptionCheck).TotalHours > 1;
        }

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
#if DEV
            "VesselStudioDEV",  // Separate folder for DEV builds
#else
            "VesselStudio",     // Production folder
#endif
            "settings.json"
        );

        /// <summary>
        /// Load settings from disk
        /// </summary>
        public static VesselStudioSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<VesselStudioSettings>(json);
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Warning: Could not load settings: {ex.Message}");
            }
            
            return new VesselStudioSettings();
        }

        /// <summary>
        /// Save settings to disk
        /// </summary>
        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Warning: Could not save settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all settings
        /// </summary>
        public static void Clear()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    File.Delete(SettingsPath);
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Warning: Could not clear settings: {ex.Message}");
            }
        }
    }
}
