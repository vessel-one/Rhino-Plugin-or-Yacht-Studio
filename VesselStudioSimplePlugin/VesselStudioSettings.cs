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
