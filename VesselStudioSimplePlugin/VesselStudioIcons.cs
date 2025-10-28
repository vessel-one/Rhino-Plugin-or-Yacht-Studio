using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Helper class to load and manage plugin icons from embedded resources
    /// </summary>
    public static class VesselStudioIcons
    {
        private static Icon _panelIcon;
        private static Bitmap _icon24;
        private static Bitmap _icon32;
        private static Bitmap _icon48;
        
        /// <summary>
        /// Get the panel icon (32x32)
        /// </summary>
        public static Icon GetPanelIcon()
        {
            return GetPanelIcon(false);
        }

        /// <summary>
        /// Get the panel icon with optional dark mode support
        /// Currently returns same icon for both modes - update if you provide separate dark mode icons
        /// </summary>
        public static Icon GetPanelIcon(bool darkMode)
        {
            if (_panelIcon != null)
                return _panelIcon;

            // Try to load 24x24 bitmap for panel tab
            var bitmap = LoadEmbeddedIcon("icon_24.png");
            
            // If embedded resource fails, try to load from file system
            if (bitmap == null)
            {
                bitmap = LoadIconFromFileSystem("icon_24.png");
            }
            
            if (bitmap == null)
            {
                // Fallback to generated icon if both methods fail
                Rhino.RhinoApp.WriteLine("⚠ Could not load icon_24.png, using fallback icon");
                bitmap = CreateFallbackIconBitmap(24);
            }
            else
            {
                Rhino.RhinoApp.WriteLine($"✓ Loaded icon: {bitmap.Width}x{bitmap.Height}");
            }

            try
            {
                // Convert bitmap to icon using proper ICO conversion
                // Create an ICO file in memory with the bitmap
                using (var ms = new MemoryStream())
                {
                    // Write ICO file header
                    using (var bw = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                    {
                        // ICO header (6 bytes)
                        bw.Write((short)0);      // Reserved (must be 0)
                        bw.Write((short)1);      // Image type (1 = ICO)
                        bw.Write((short)1);      // Number of images
                        
                        // Image directory (16 bytes)
                        bw.Write((byte)bitmap.Width);   // Width
                        bw.Write((byte)bitmap.Height);  // Height
                        bw.Write((byte)0);       // Color palette
                        bw.Write((byte)0);       // Reserved
                        bw.Write((short)1);      // Color planes
                        bw.Write((short)32);     // Bits per pixel
                        
                        // Get PNG data
                        byte[] pngData;
                        using (var pngStream = new MemoryStream())
                        {
                            bitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
                            pngData = pngStream.ToArray();
                        }
                        
                        bw.Write((int)pngData.Length);  // Image data size
                        bw.Write((int)22);              // Image data offset (6 + 16)
                        bw.Write(pngData);              // PNG data
                    }
                    
                    ms.Seek(0, SeekOrigin.Begin);
                    _panelIcon = new Icon(ms);
                    
                    Rhino.RhinoApp.WriteLine($"✓ Icon converted successfully: {_panelIcon.Width}x{_panelIcon.Height}");
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"❌ Icon conversion failed: {ex.Message}");
                Rhino.RhinoApp.WriteLine($"   Stack: {ex.StackTrace}");
                _panelIcon = null;
            }
            
            return _panelIcon;
        }

        /// <summary>
        /// Get a toolbar button bitmap at specified size
        /// </summary>
        /// <param name="size">Icon size (24, 32, or 48)</param>
        /// <param name="darkMode">Dark mode (currently unused, returns same icon)</param>
        public static Bitmap GetToolbarBitmap(int size = 24, bool darkMode = false)
        {
            // Load from embedded resources based on size
            switch (size)
            {
                case 24:
                    if (_icon24 == null)
                        _icon24 = LoadEmbeddedIcon("icon_24.png");
                    return _icon24 ?? CreateFallbackIconBitmap(24);
                    
                case 32:
                    if (_icon32 == null)
                        _icon32 = LoadEmbeddedIcon("icon_32.png");
                    return _icon32 ?? CreateFallbackIconBitmap(32);
                    
                case 48:
                    if (_icon48 == null)
                        _icon48 = LoadEmbeddedIcon("icon_48.png");
                    return _icon48 ?? CreateFallbackIconBitmap(48);
                    
                default:
                    // For custom sizes, scale from nearest available
                    var baseIcon = LoadEmbeddedIcon("icon_32.png") ?? CreateFallbackIconBitmap(32);
                    return new Bitmap(baseIcon, new Size(size, size));
            }
        }

        /// <summary>
        /// Load an icon from embedded resources
        /// </summary>
        private static Bitmap LoadEmbeddedIcon(string filename)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                
                // The correct pattern based on our build output
                var resourceName = $"VesselStudioSimplePlugin.Resources.{filename}";
                
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        return new Bitmap(stream);
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"❌ Error loading embedded icon {filename}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Load an icon from the file system as fallback
        /// </summary>
        private static Bitmap LoadIconFromFileSystem(string filename)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var pluginFolder = Path.GetDirectoryName(assembly.Location);
                var resourcePath = Path.Combine(pluginFolder, "Resources", filename);
                
                if (File.Exists(resourcePath))
                {
                    Rhino.RhinoApp.WriteLine($"✓ Loaded icon from file system: {resourcePath}");
                    return new Bitmap(resourcePath);
                }
                
                Rhino.RhinoApp.WriteLine($"⚠ Icon file not found: {resourcePath}");
                return null;
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"❌ Error loading icon from file system: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a fallback icon bitmap if embedded resource not found
        /// Creates a visible icon with colored background suitable for both light and dark modes
        /// </summary>
        private static Bitmap CreateFallbackIconBitmap(int size)
        {
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                
                // Background - Vessel Studio brand blue (visible in both light and dark modes)
                using (var brush = new SolidBrush(Color.FromArgb(64, 123, 255)))
                {
                    g.FillEllipse(brush, 2, 2, size - 4, size - 4);
                }
                
                // Draw ship/vessel icon using simple shapes
                var centerX = size / 2f;
                var centerY = size / 2f;
                var scale = size / 32f;
                
                using (var whiteBrush = new SolidBrush(Color.White))
                {
                    // Draw simple boat/vessel shape
                    // Hull (trapezoid)
                    PointF[] hull = new PointF[]
                    {
                        new PointF(centerX - 6 * scale, centerY + 4 * scale),
                        new PointF(centerX + 6 * scale, centerY + 4 * scale),
                        new PointF(centerX + 4 * scale, centerY + 2 * scale),
                        new PointF(centerX - 4 * scale, centerY + 2 * scale)
                    };
                    g.FillPolygon(whiteBrush, hull);
                    
                    // Cabin (rectangle)
                    g.FillRectangle(whiteBrush, centerX - 3 * scale, centerY - 2 * scale, 6 * scale, 4 * scale);
                    
                    // Mast (line)
                    using (var pen = new Pen(Color.White, Math.Max(1, scale)))
                    {
                        g.DrawLine(pen, centerX, centerY - 2 * scale, centerX, centerY - 6 * scale);
                    }
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Create an icon file and save it to disk (for debugging/inspection)
        /// </summary>
        public static void SaveIconToDisk(string path)
        {
            try
            {
                // Get the bitmap instead of the icon to avoid COM issues
                var bitmap = GetToolbarBitmap(32, false);
                
                // Save as PNG if path ends with .ico (ICO format can be problematic)
                if (path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                {
                    path = path.Replace(".ico", ".png");
                }
                
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save icon: {ex.Message}", ex);
            }
        }
    }
}
