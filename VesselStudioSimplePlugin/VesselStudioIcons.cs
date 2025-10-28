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

            // Load 24x24 bitmap for panel tab (Rhino uses smaller icons in tabs)
            var bitmap = LoadEmbeddedIcon("icon_24.png");
            if (bitmap == null)
            {
                // Fallback to generated icon if resource not found
                bitmap = CreateFallbackIconBitmap(24);
            }

            try
            {
                // Convert bitmap to icon - simpler approach without MakeTransparent
                IntPtr hIcon = bitmap.GetHicon();
                _panelIcon = Icon.FromHandle(hIcon);
                // Note: Don't call DestroyIcon here as Icon.FromHandle creates a clone
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Icon conversion failed: {ex.Message}");
                // Last resort: try to create icon from bitmap directly
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);
                    _panelIcon = new Icon(ms, 24, 24);
                }
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
                var resourceName = $"VesselStudioSimplePlugin.Resources.{filename}";
                
                var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    // Try without namespace prefix
                    resourceName = $"Resources.{filename}";
                    stream = assembly.GetManifestResourceStream(resourceName);
                }
                
                if (stream == null)
                    return null;
                
                using (stream)
                {
                    return new Bitmap(stream);
                }
            }
            catch
            {
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
