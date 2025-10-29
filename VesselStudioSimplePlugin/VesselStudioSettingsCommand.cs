using System;
using Rhino;
using Rhino.Commands;
using VesselStudioSimplePlugin.UI;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Command to open combined settings dialog (API Key + Image Format)
    /// 
    /// Features:
    /// - API Key configuration with validation
    /// - Image format selection (PNG/JPEG)
    /// - JPEG quality control (1-100)
    /// - Settings persist in JSON file
    /// </summary>
    [System.Runtime.InteropServices.Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")]
    public class VesselStudioSettingsCommand : Command
    {
#if DEV
        public override string EnglishName => "DevVesselSettings";
#else
        public override string EnglishName => "VesselSettings";
#endif

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // Open combined settings dialog (API Key + Image Format)
                using (var dialog = new VesselStudioSettingsDialog())
                {
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        RhinoApp.WriteLine("✅ Settings saved successfully");
                    }
                }

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Error opening settings: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}
