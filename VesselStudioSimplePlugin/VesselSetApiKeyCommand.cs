using System;
using System.Windows.Forms;
using Rhino;
using Rhino.Commands;
using VesselStudioSimplePlugin.UI;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// DEPRECATED: Command to set/update the Vessel Studio API key
    /// Now redirects to VesselStudioSettingsDialog (combined settings)
    /// Use DevVesselSettings / VesselSettings instead
    /// </summary>
    [System.Runtime.InteropServices.Guid("C2D3E4F5-A6B7-8C9D-0E1F-2A3B4C5D6E7F")]
    public class VesselStudioSetApiKeyCommand : Command
    {
        public VesselStudioSetApiKeyCommand()
        {
            Instance = this;
        }

        public static VesselStudioSetApiKeyCommand Instance { get; private set; }

#if DEV
        public override string EnglishName => "DevVesselSetApiKey";
#else
        public override string EnglishName => "VesselSetApiKey";
#endif

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Redirect to combined settings dialog
            using (var dialog = new VesselStudioSettingsDialog())
            {
                var result = dialog.ShowDialog();
                
                if (result == DialogResult.OK)
                {
                    RhinoApp.WriteLine("✅ Settings saved successfully");
                    return Result.Success;
                }
                else
                {
                    RhinoApp.WriteLine("Settings cancelled");
                    return Result.Cancel;
                }
            }
        }
    }
}
