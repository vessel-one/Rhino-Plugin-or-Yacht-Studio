using System;
using Rhino;
using Rhino.Commands;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Menu integration for Vessel Studio plugin
    /// Note: RhinoCommon 8 doesn't support programmatic menu creation via RhinoApp.MainMenu()
    /// Menu customization should be done through Rhino's UI or .rui files
    /// This class is kept for future compatibility
    /// </summary>
    public static class VesselStudioMenu
    {
        /// <summary>
        /// Placeholder for menu initialization
        /// Menu items can be accessed via the toolbar panel or commands
        /// </summary>
        public static void AddMenu()
        {
            // Menu API not available in RhinoCommon 8
            // Users can access features via:
            // 1. Toolbar panel (VesselStudioShowToolbar)
            // 2. Direct commands (VesselSetApiKey, VesselCapture, etc.)
            // 3. Custom .rui toolbar files
        }

        /// <summary>
        /// Placeholder for menu cleanup
        /// </summary>
        public static void RemoveMenu()
        {
            // No menu to remove
        }
    }

    /// <summary>
    /// Help command that opens Vessel Studio documentation
    /// </summary>
    [System.Runtime.InteropServices.Guid("D8E9F0A1-B2C3-4D5E-6F7A-8B9C0D1E2F3A")]
    public class VesselStudioHelpCommand : Command
    {
        public override string EnglishName => "VesselStudioHelp";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Open documentation in default browser
            try
            {
                System.Diagnostics.Process.Start("https://vesselstudio.io/docs/rhino-plugin");
            }
            catch
            {
                RhinoApp.WriteLine("Vessel Studio Rhino Plugin");
                RhinoApp.WriteLine("=========================");
                RhinoApp.WriteLine("");
                RhinoApp.WriteLine("Commands:");
                RhinoApp.WriteLine("  VesselSetApiKey      - Configure your API key");
                RhinoApp.WriteLine("  VesselCapture        - Capture and upload screenshot");
                RhinoApp.WriteLine("  VesselQuickCapture   - Quick capture to last project");
                RhinoApp.WriteLine("  VesselStudioStatus   - Check connection status");
                RhinoApp.WriteLine("");
                RhinoApp.WriteLine("For more information, visit: https://vesselstudio.io/docs/rhino-plugin");
            }

            return Result.Success;
        }
    }
}
