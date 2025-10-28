using System;
using Rhino;
using Rhino.Commands;
using VesselStudioSimplePlugin.UI;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Command to open image format and quality settings dialog
    /// 
    /// Enhancement: Phase 5 Group 3+ (Post-MVP Polish)
    /// - Allows user to choose image format (PNG/JPEG)
    /// - Allows user to set JPEG quality (1-100)
    /// - Settings persist in JSON file
    /// </summary>
    [System.Runtime.InteropServices.Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
    public class VesselStudioSettingsCommand : Command
    {
#if DEV
        public override string EnglishName => "DevVesselImageSettings";
#else
        public override string EnglishName => "VesselImageSettings";
#endif

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Open image format settings dialog
                using (var dialog = new VesselImageFormatDialog())
                {
                    dialog.ShowDialog();
                }

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Error opening image format settings: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}
