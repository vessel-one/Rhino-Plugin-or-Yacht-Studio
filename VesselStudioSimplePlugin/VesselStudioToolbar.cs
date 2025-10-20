using System;
using System.Drawing;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Toolbar integration for Vessel Studio plugin
    /// Creates a dockable toolbar with quick-access buttons
    /// </summary>
    public static class VesselStudioToolbar
    {
        private static Guid _toolbarGroupId = new Guid("{A5B6C7D8-E9F0-4A5B-8C9D-0E1F2A3B4C5D}");
        private static bool _toolbarAdded = false;

        /// <summary>
        /// Add the Vessel Studio toolbar to Rhino
        /// </summary>
        public static void AddToolbar()
        {
            if (_toolbarAdded)
                return;

            try
            {
                // Create toolbar using Rhino's toolbar system
                // Note: In Rhino 8, toolbars are typically defined in .rui files
                // For programmatic creation, we'll use a panel instead
                CreateToolbarPanel();
                
                _toolbarAdded = true;
                RhinoApp.WriteLine("Vessel Studio toolbar panel created");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error creating Vessel Studio toolbar: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a dockable panel with toolbar buttons
        /// </summary>
        private static void CreateToolbarPanel()
        {
            try
            {
                // Register the panel - convert bitmap to icon
                var icon = System.Drawing.Icon.FromHandle(VesselStudioToolbarPanel.PanelIcon.GetHicon());
                
                Panels.RegisterPanel(
                    VesselStudioSimplePlugin.Instance,
                    typeof(VesselStudioToolbarPanel),
                    "Vessel Studio",
                    icon,
                    PanelType.System
                );

                RhinoApp.WriteLine("Vessel Studio panel registered. Use 'VesselStudioShowToolbar' command to show it.");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error registering panel: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove the Vessel Studio toolbar
        /// </summary>
        public static void RemoveToolbar()
        {
            if (!_toolbarAdded)
                return;

            _toolbarAdded = false;
        }

        /// <summary>
        /// Show the Vessel Studio toolbar panel
        /// </summary>
        public static void ShowPanel()
        {
            var panelId = typeof(VesselStudioToolbarPanel).GUID;
            Panels.OpenPanel(panelId);
        }
    }

    /// <summary>
    /// Command to open the Vessel Studio toolbar panel
    /// </summary>
    [System.Runtime.InteropServices.Guid("B6C7D8E9-F0A1-2B3C-4D5E-6F7A8B9C0D1E")]
    public class VesselStudioShowToolbarCommand : Command
    {
        public override string EnglishName => "VesselStudioShowToolbar";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            VesselStudioToolbar.ShowPanel();
            return Result.Success;
        }
    }
}
