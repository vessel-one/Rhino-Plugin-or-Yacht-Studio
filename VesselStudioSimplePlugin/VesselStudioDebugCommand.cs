using System;
using System.Drawing;
using System.IO;
using Rhino;
using Rhino.Commands;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Debug command to save icon files to disk for inspection
    /// </summary>
    [System.Runtime.InteropServices.Guid("C7D8E9F0-A1B2-3C4D-5E6F-7A8B9C0D1E2F")]
    public class VesselStudioDebugIconsCommand : Command
    {
        public override string EnglishName => "VesselStudioDebugIcons";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                
                // Save light mode icon (32x32)
                var iconPath = Path.Combine(desktopPath, "VesselStudio_Icon_32.ico");
                VesselStudioIcons.SaveIconToDisk(iconPath);
                RhinoApp.WriteLine($"✓ Saved 32x32 icon to: {iconPath}");
                
                // Save light mode PNG (32x32)
                var pngPath32 = Path.Combine(desktopPath, "VesselStudio_Icon_32.png");
                var bitmap32 = VesselStudioIcons.GetToolbarBitmap(32, false);
                bitmap32.Save(pngPath32, System.Drawing.Imaging.ImageFormat.Png);
                RhinoApp.WriteLine($"✓ Saved 32x32 PNG to: {pngPath32}");
                
                // Save high-DPI PNG (48x48)
                var pngPath48 = Path.Combine(desktopPath, "VesselStudio_Icon_48.png");
                var bitmap48 = VesselStudioIcons.GetToolbarBitmap(48, false);
                bitmap48.Save(pngPath48, System.Drawing.Imaging.ImageFormat.Png);
                RhinoApp.WriteLine($"✓ Saved 48x48 PNG to: {pngPath48}");
                
                // Save dark mode PNG (32x32)
                var pngPathDark = Path.Combine(desktopPath, "VesselStudio_Icon_32_Dark.png");
                var bitmapDark = VesselStudioIcons.GetToolbarBitmap(32, true);
                bitmapDark.Save(pngPathDark, System.Drawing.Imaging.ImageFormat.Png);
                RhinoApp.WriteLine($"✓ Saved 32x32 dark mode PNG to: {pngPathDark}");
                
                // Save toolbar size PNG (24x24) - standard toolbar button size
                var pngPath24 = Path.Combine(desktopPath, "VesselStudio_Icon_24.png");
                var bitmap24 = VesselStudioIcons.GetToolbarBitmap(24, false);
                bitmap24.Save(pngPath24, System.Drawing.Imaging.ImageFormat.Png);
                RhinoApp.WriteLine($"✓ Saved 24x24 toolbar PNG to: {pngPath24}");
                
                RhinoApp.WriteLine("\n✅ All icon files saved to Desktop!");
                RhinoApp.WriteLine("You can now inspect the icons to see how they look.");
                
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Error saving icons: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}
