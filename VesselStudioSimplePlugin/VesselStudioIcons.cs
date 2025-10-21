using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Helper class to generate and manage plugin icons
    /// </summary>
    public static class VesselStudioIcons
    {
        private static Icon _panelIcon;
        
        /// <summary>
        /// Get the panel icon (VS logo)
        /// </summary>
        public static Icon GetPanelIcon()
        {
            if (_panelIcon != null)
                return _panelIcon;

            // Create a 32x32 bitmap with VS logo
            var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                
                // Background - Vessel One blue
                using (var brush = new SolidBrush(Color.FromArgb(70, 130, 180)))
                {
                    g.FillRectangle(brush, 0, 0, 32, 32);
                }
                
                // Draw "VS" text in white
                using (var font = new Font("Arial", 14, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("VS", font, textBrush, new RectangleF(0, 0, 32, 32), format);
                }
                
                // Add subtle border
                using (var pen = new Pen(Color.FromArgb(50, 90, 130), 2))
                {
                    g.DrawRectangle(pen, 1, 1, 30, 30);
                }
            }

            // Convert bitmap to icon
            IntPtr hIcon = bitmap.GetHicon();
            _panelIcon = Icon.FromHandle(hIcon);
            
            return _panelIcon;
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
