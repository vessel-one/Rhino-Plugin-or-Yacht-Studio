using System;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Menu integration for Vessel Studio plugin
    /// Creates a top-level "Vessel Studio" menu in Rhino's menu bar
    /// </summary>
    public static class VesselStudioMenu
    {
        private static bool _menuAdded = false;

        /// <summary>
        /// Add the Vessel Studio menu to Rhino's menu bar
        /// </summary>
        public static void AddMenu()
        {
            if (_menuAdded)
                return;

            try
            {
                // Create main menu
                var mainMenu = RhinoApp.MainMenu();
                if (mainMenu == null)
                {
                    RhinoApp.WriteLine("Warning: Could not access main menu");
                    return;
                }

                // Insert "Vessel Studio" menu before "Help" menu
                int helpIndex = -1;
                for (int i = 0; i < mainMenu.TopLevelMenuCount; i++)
                {
                    if (mainMenu.GetTopLevelMenuText(i) == "&Help")
                    {
                        helpIndex = i;
                        break;
                    }
                }

                int insertIndex = helpIndex >= 0 ? helpIndex : mainMenu.TopLevelMenuCount;
                int menuIndex = mainMenu.InsertMenu(insertIndex, "&Vessel Studio", "VesselStudioMenu");

                if (menuIndex < 0)
                {
                    RhinoApp.WriteLine("Warning: Could not create Vessel Studio menu");
                    return;
                }

                // Get the menu we just created
                var vesselMenu = mainMenu.GetMenu(menuIndex);
                if (vesselMenu == null)
                {
                    RhinoApp.WriteLine("Warning: Could not get Vessel Studio menu");
                    return;
                }

                // Add menu items
                vesselMenu.InsertMenuItem(0, "Set &API Key...", "VesselSetApiKey");
                vesselMenu.InsertSeparator(1);
                vesselMenu.InsertMenuItem(2, "&Capture Screenshot...", "VesselCapture");
                vesselMenu.InsertMenuItem(3, "&Quick Capture", "VesselQuickCapture");
                vesselMenu.InsertSeparator(4);
                vesselMenu.InsertMenuItem(5, "&Status", "VesselStudioStatus");
                vesselMenu.InsertSeparator(6);
                vesselMenu.InsertMenuItem(7, "&Help && Documentation", "VesselStudioHelp");

                _menuAdded = true;
                RhinoApp.WriteLine("Vessel Studio menu added successfully");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error adding Vessel Studio menu: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove the Vessel Studio menu from Rhino's menu bar
        /// </summary>
        public static void RemoveMenu()
        {
            if (!_menuAdded)
                return;

            try
            {
                var mainMenu = RhinoApp.MainMenu();
                if (mainMenu == null)
                    return;

                // Find and remove the Vessel Studio menu
                for (int i = 0; i < mainMenu.TopLevelMenuCount; i++)
                {
                    if (mainMenu.GetTopLevelMenuText(i) == "&Vessel Studio")
                    {
                        mainMenu.RemoveMenu(i);
                        break;
                    }
                }

                _menuAdded = false;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error removing Vessel Studio menu: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Help command that opens Vessel Studio documentation
    /// </summary>
    public class VesselStudioHelpCommand : Command
    {
        public override string EnglishName => "VesselStudioHelp";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Open documentation in default browser
            try
            {
                System.Diagnostics.Process.Start("https://vessel.one/docs/rhino-plugin");
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
                RhinoApp.WriteLine("For more information, visit: https://vessel.one/docs");
            }

            return Result.Success;
        }
    }
}
