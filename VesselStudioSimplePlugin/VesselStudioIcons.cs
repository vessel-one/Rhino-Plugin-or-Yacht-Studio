using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Helper class to generate and manage plugin icons
    /// Supports both light and dark modes for Rhino 8
    /// </summary>
    public static class VesselStudioIcons
    {
        private static Icon _panelIcon;
        private static Icon _panelIconDark;
        
        /// <summary>
        /// Get the panel icon for light mode (VS logo)
        /// </summary>
        public static Icon GetPanelIcon()
        {
            return GetPanelIcon(false);
        }

        /// <summary>
        /// Get the panel icon (VS logo) with optional dark mode support
        /// </summary>
        public static Icon GetPanelIcon(bool darkMode)
        {
            if (darkMode && _panelIconDark != null)
                return _panelIconDark;
            
            if (!darkMode && _panelIcon != null)
                return _panelIcon;

            // Create a 32x32 bitmap with VS logo
            var bitmap = CreateIconBitmap(32, darkMode);

            // Convert bitmap to icon
            IntPtr hIcon = bitmap.GetHicon();
            var icon = Icon.FromHandle(hIcon);
            
            if (darkMode)
                _panelIconDark = icon;
            else
                _panelIcon = icon;
            
            return icon;
        }

        /// <summary>
        /// Create a bitmap icon at specified size
        /// </summary>
        private static Bitmap CreateIconBitmap(int size, bool darkMode = false)
        {
            var bitmap = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                
                // Background - Vessel Studio brand blue
                var bgColor = darkMode 
                    ? Color.FromArgb(55, 110, 160)  // Slightly darker for dark mode
                    : Color.FromArgb(37, 99, 235);   // Vessel Studio primary blue
                
                using (var brush = new SolidBrush(bgColor))
                {
                    g.FillRectangle(brush, 0, 0, size, size);
                }
                
                // Draw "VS" text in white
                var fontSize = size * 0.45f; // Scale font with icon size
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
                var borderColor = darkMode 
                    ? Color.FromArgb(35, 80, 120)
                    : Color.FromArgb(25, 70, 180);
                    
                using (var pen = new Pen(borderColor, Math.Max(1, size / 16)))
                {
                    g.DrawRectangle(pen, 1, 1, size - 2, size - 2);
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Get a high-resolution toolbar button bitmap (24x24 for standard, 48x48 for high-DPI)
        /// </summary>
        public static Bitmap GetToolbarBitmap(int size = 24, bool darkMode = false)
        {
            return CreateIconBitmap(size, darkMode);
        }

        /// <summary>
        /// Create an icon file and save it to disk (for debugging/inspection)
        /// </summary>
        public static void SaveIconToDisk(string path)
        {
            var icon = GetPanelIcon();
            using (var stream = new FileStream(path, FileMode.Create))
            {
                icon.Save(stream);
            }
        }
    }
}
