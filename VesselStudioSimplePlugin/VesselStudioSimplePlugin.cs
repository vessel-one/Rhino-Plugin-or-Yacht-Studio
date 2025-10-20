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
                
                RhinoApp.WriteLine("Vessel Studio Plugin loaded successfully!");
                RhinoApp.WriteLine("• Use 'Vessel Studio' menu for all commands");
                RhinoApp.WriteLine("• Use 'VesselStudioShowToolbar' to show the toolbar panel");
                RhinoApp.WriteLine("• Or use commands: VesselSetApiKey, VesselCapture, VesselQuickCapture");
                
                return LoadReturnCode.Success;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to load Vessel Studio plugin: {ex.Message}";
                return LoadReturnCode.ErrorShowDialog;
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