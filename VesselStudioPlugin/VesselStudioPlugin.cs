using System;
using Rhino;
using Rhino.PlugIns;
using VesselStudioPlugin.Services;

namespace VesselStudioPlugin
{
    /// <summary>
    /// Main plugin class for Vessel Studio integration
    /// Handles plugin lifecycle and service initialization
    /// </summary>
    public class VesselStudioPlugin : PlugIn
    {
        #region Fields
        
        private IApiClient? _apiClient;
        private IAuthService? _authService;
        private IScreenshotService? _screenshotService;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets the singleton instance of the plugin
        /// </summary>
        public static VesselStudioPlugin? Instance { get; private set; }
        
        /// <summary>
        /// Gets the API client service for communicating with Vessel Studio
        /// </summary>
        public IApiClient ApiClient => _apiClient ?? throw new InvalidOperationException("Plugin not initialized");
        
        /// <summary>
        /// Gets the authentication service for managing user sessions
        /// </summary>
        public IAuthService AuthService => _authService ?? throw new InvalidOperationException("Plugin not initialized");
        
        /// <summary>
        /// Gets the screenshot service for capturing viewport images
        /// </summary>
        public IScreenshotService ScreenshotService => _screenshotService ?? throw new InvalidOperationException("Plugin not initialized");
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the VesselStudioPlugin class
        /// </summary>
        public VesselStudioPlugin()
        {
            Instance = this;
        }
        
        #endregion
        
        #region Plugin Lifecycle
        
        /// <summary>
        /// Called when the plugin is loaded into Rhino
        /// </summary>
        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            try
            {
                InitializeServices();
                RhinoApp.WriteLine("Vessel Studio Plugin loaded successfully");
                return LoadReturnCode.Success;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to initialize Vessel Studio Plugin: {ex.Message}";
                RhinoApp.WriteLine($"Error loading Vessel Studio Plugin: {ex}");
                return LoadReturnCode.ErrorShowDialog;
            }
        }
        
        /// <summary>
        /// Called when Rhino is shutting down
        /// </summary>
        protected override void OnShutdown()
        {
            try
            {
                CleanupServices();
                RhinoApp.WriteLine("Vessel Studio Plugin shutdown complete");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error during Vessel Studio Plugin shutdown: {ex}");
            }
            finally
            {
                Instance = null;
                base.OnShutdown();
            }
        }
        
        #endregion
        
        #region Service Management
        
        /// <summary>
        /// Initializes all plugin services
        /// </summary>
        private void InitializeServices()
        {
            // Initialize services in dependency order
            var httpClient = new System.Net.Http.HttpClient();
            
            // Configure base settings (these should eventually come from settings)
            var apiBaseUrl = "https://api.vesselstudio.com";
            var clientId = "vessel-studio-rhino-plugin";
            
            _authService = new AuthenticationService(httpClient, apiBaseUrl, clientId);
            _apiClient = new ApiClient(httpClient, _authService, apiBaseUrl);
            _screenshotService = new ScreenshotService();
            
            RhinoApp.WriteLine("Vessel Studio Plugin services initialized");
        }
        
        /// <summary>
        /// Cleans up plugin services and resources
        /// </summary>
        private void CleanupServices()
        {
            // Dispose services in reverse order
            _screenshotService?.Dispose();
            _screenshotService = null;
            
            _authService?.Dispose();
            _authService = null;
            
            _apiClient?.Dispose();
            _apiClient = null;
        }
        
        #endregion
        
        #region Plugin Information
        
        /// <summary>
        /// Gets the plugin name
        /// </summary>
        public string PlugInName => "Vessel Studio Plugin";
        
        /// <summary>
        /// Gets the plugin version
        /// </summary>
        public string PlugInVersion => "1.0.0";
        
        /// <summary>
        /// Gets the plugin description
        /// </summary>
        public string PlugInDescription => "Capture viewport screenshots and sync with Vessel Studio projects";
        
        #endregion
    }
}