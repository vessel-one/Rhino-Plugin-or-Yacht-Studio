using System;
using Rhino;
using Rhino.PlugIns;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// VesselStudioSimplePlugin plug-in
    /// </summary>
    public class VesselStudioSimplePlugin : PlugIn
    {
        private static VesselStudioSimplePlugin _instance;

        /// <summary>
        /// Gets the only instance of the VesselStudioSimplePlugin plug-in.
        /// </summary>
        public static VesselStudioSimplePlugin Instance => _instance;

        /// <summary>
        /// Simple API client for Vessel Studio
        /// </summary>
        public VesselStudioApiClient ApiClient { get; private set; }

        public VesselStudioSimplePlugin()
        {
            _instance = this;
        }

        /// <summary>
        /// Called one time when plug-in is loaded.
        /// </summary>
        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            try
            {
                // Initialize the API client
                ApiClient = new VesselStudioApiClient();
                
                // Add menu and toolbar
                VesselStudioMenu.AddMenu();
                VesselStudioToolbar.AddToolbar();
                
                // Validate subscription on startup (non-blocking)
                ValidateSubscriptionOnStartup();
                
                return LoadReturnCode.Success;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to load Vessel Studio plugin: {ex.Message}";
                return LoadReturnCode.ErrorShowDialog;
            }
        }

        /// <summary>
        /// Validate subscription status on plugin startup (non-blocking)
        /// </summary>
        private async void ValidateSubscriptionOnStartup()
        {
            try
            {
                var settings = VesselStudioSettings.Load();
                
                // Only validate if we have an API key
                if (string.IsNullOrEmpty(settings.ApiKey))
                {
                    RhinoApp.WriteLine("Vessel Studio loaded");
                    return;
                }
                
                // Check if we should revalidate (every hour or on first load)
                if (!settings.ShouldRecheckSubscription() && settings.LastSubscriptionCheck > DateTime.MinValue)
                {
                    // Use cached status
                    var cachedStatus = settings.HasValidSubscription ? "Active" : "LOCKED";
                    RhinoApp.WriteLine($"Vessel Studio loaded - Subscription: {cachedStatus}");
                    return;
                }
                
                ApiClient.SetApiKey(settings.ApiKey);
                var result = await ApiClient.ValidateApiKeyAsync();
                
                // If validation completely failed (network error, timeout, 401 auth failure, etc.)
                // Don't delete key on subscription tier errors (403) - user can upgrade
                if (!result.Success)
                {
                    // Clear cached subscription data but DON'T delete API key unless it's truly invalid
                    settings.HasValidSubscription = false;
                    settings.LastSubscriptionCheck = DateTime.MinValue;
                    settings.LastProjectId = null;
                    settings.LastProjectName = null;
                    settings.SubscriptionErrorMessage = result.ErrorMessage;
                    settings.Save();
                    
                    RhinoApp.WriteLine($"Vessel Studio loaded - Authentication failed: {result.ErrorMessage}");
                    RhinoApp.WriteLine("Please check your connection or API key in settings.");
                    return;
                }
                
                // Update cached subscription status
                settings.HasValidSubscription = result.HasValidSubscription;
                settings.LastSubscriptionCheck = DateTime.Now;
                settings.SubscriptionErrorMessage = result.SubscriptionError?.UserMessage;
                settings.UpgradeUrl = result.SubscriptionError?.UpgradeUrl;
                
                // If subscription is invalid, clear project cache
                if (!result.HasValidSubscription)
                {
                    settings.LastProjectId = null;
                    settings.LastProjectName = null;
                }
                
                settings.Save();
                
                if (!result.HasValidSubscription)
                {
                    RhinoApp.WriteLine("Vessel Studio loaded - Subscription: LOCKED (upgrade required)");
                }
                else
                {
                    RhinoApp.WriteLine("Vessel Studio loaded - Subscription: Active");
                }
            }
            catch (Exception ex)
            {
                // Silently fail - don't block plugin load
                RhinoApp.WriteLine($"Vessel Studio loaded - Could not validate subscription: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the plug-in is being unloaded.
        /// </summary>
        protected override void OnShutdown()
        {
            // Clean up menu and toolbar
            VesselStudioMenu.RemoveMenu();
            VesselStudioToolbar.RemoveToolbar();
            
            ApiClient?.Dispose();
            _instance = null;
        }
    }
}