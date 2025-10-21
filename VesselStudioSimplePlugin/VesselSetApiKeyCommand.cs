using System;
using System.Windows.Forms;
using Rhino;
using Rhino.Commands;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Command to set/update the Vessel Studio API key
    /// </summary>
    [System.Runtime.InteropServices.Guid("C2D3E4F5-A6B7-8C9D-0E1F-2A3B4C5D6E7F")]
    public class VesselStudioSetApiKeyCommand : Command
    {
        public VesselStudioSetApiKeyCommand()
        {
            Instance = this;
        }

        public static VesselStudioSetApiKeyCommand Instance { get; private set; }

        public override string EnglishName => "VesselSetApiKey";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Load current settings
            var settings = VesselStudioSettings.Load();
            
            // Show settings dialog
            using (var dialog = new VesselStudioSettingsDialog(settings.ApiKey))
            {
                var result = dialog.ShowDialog();
                
                if (result == DialogResult.OK)
                {
                    RhinoApp.WriteLine("✅ Vessel Studio API key configured successfully");
                    return Result.Success;
                }
                else
                {
                    RhinoApp.WriteLine("API key configuration cancelled");
                    return Result.Cancel;
                }
            }
        }
    }
}
