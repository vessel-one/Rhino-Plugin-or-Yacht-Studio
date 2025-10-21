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

            // Load 32x32 icon from embedded resources
            var bitmap = LoadEmbeddedIcon("icon_32.png");
            if (bitmap == null)
            {
                // Fallback to generated icon if resource not found
                bitmap = CreateFallbackIconBitmap(32);
            }

            try
            {
                // Convert bitmap to icon - must use Bitmap.MakeTransparent() first for proper alpha
                bitmap.MakeTransparent(bitmap.GetPixel(0, 0));
                IntPtr hIcon = bitmap.GetHicon();
                try
                {
                    _panelIcon = Icon.FromHandle(hIcon);
                }
                finally
                {
                    // Clean up the handle to avoid COM errors
                    DestroyIcon(hIcon);
                }
            }
            catch
            {
                // If icon conversion fails, create a simple icon from bitmap
                _panelIcon = Icon.FromHandle(bitmap.GetHicon());
            }
            
            return _panelIcon;
        }
        
        // Import DestroyIcon from user32.dll to properly clean up icon handles
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

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
        /// </summary>
        private static Bitmap CreateFallbackIconBitmap(int size)
        {
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                
                // Background - Vessel Studio brand blue
                using (var brush = new SolidBrush(Color.FromArgb(37, 99, 235)))
                {
                    g.FillRectangle(brush, 0, 0, size, size);
                }
                
                // Draw "VS" text in white
                var fontSize = size * 0.45f;
                using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("VS", font, textBrush, new RectangleF(0, 0, size, size), format);
                }
                
                // Add subtle border
                using (var pen = new Pen(Color.FromArgb(25, 70, 180), Math.Max(1, size / 16)))
                {
                    g.DrawRectangle(pen, 1, 1, size - 2, size - 2);
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
