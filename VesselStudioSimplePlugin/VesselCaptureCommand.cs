using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Enhanced capture command with image name dialog only (uses project from toolbar)
    /// </summary>
    public class VesselCaptureCommand : Command
    {
#if DEV
        public override string EnglishName => "DevVesselCapture";
#else
        public override string EnglishName => "VesselCapture";
#endif

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Load settings
            var settings = VesselStudioSettings.Load();
            
            if (string.IsNullOrEmpty(settings.ApiKey))
            {
                RhinoApp.WriteLine("❌ API key not set. Run VesselSetApiKey first.");
                return Result.Failure;
            }

            if (string.IsNullOrEmpty(settings.LastProjectId))
            {
                RhinoApp.WriteLine("❌ No project selected. Please select a project from the toolbar dropdown.");
                return Result.Failure;
            }

            // Simple dialog for image name only
            var dialog = new ImageNameDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return Result.Cancel;
            }

            var imageName = dialog.ImageName;

            // Perform capture (fast - just takes screenshot)
            RhinoApp.WriteLine($"📸 Capturing viewport...");
            var captureData = CaptureViewport(doc);
            
            if (captureData.imageBytes == null)
            {
                RhinoApp.WriteLine("❌ Failed to capture viewport");
                return Result.Failure;
            }
            
            var apiClient = new VesselStudioApiClient();
            apiClient.SetApiKey(settings.ApiKey);
            
            // Start upload in background - don't block Rhino!
            RhinoApp.WriteLine($"📤 Uploading to {settings.LastProjectName}...");
            RhinoApp.WriteLine("💡 You can continue working - upload happens in background");
            
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var result = await apiClient.UploadScreenshotAsync(
                        settings.LastProjectId,
                        captureData.imageBytes,
                        imageName,
                        captureData.metadata
                    );
                    
                    if (result.Success)
                    {
                        RhinoApp.WriteLine($"✅ Upload complete!");
                        RhinoApp.WriteLine($"📷 View at: {result.Url}");
                    }
                    else
                    {
                        RhinoApp.WriteLine($"❌ Upload failed: {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"❌ Upload error: {ex.Message}");
                }
            });
            
            return Result.Success;
        }

        private (byte[] imageBytes, Dictionary<string, object> metadata) CaptureViewport(RhinoDoc doc)
        {
            try
            {
                var view = doc.Views.ActiveView;
                if (view == null)
                {
                    return (null, null);
                }

                // Capture viewport as bitmap (fast operation)
                var bitmap = view.CaptureToBitmap(new Size(1920, 1080));
                
                // Convert to byte array
                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    imageBytes = ms.ToArray();
                }

                // Gather metadata
                var metadata = new Dictionary<string, object>
                {
                    { "width", 1920 },
                    { "height", 1080 },
                    { "viewportName", view.MainViewport.Name },
                    { "displayMode", view.MainViewport.DisplayMode.EnglishName },
                    { "rhinoVersion", RhinoApp.Version.ToString() },
                    { "captureTime", DateTime.UtcNow.ToString("o") }
                };

                return (imageBytes, metadata);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Capture error: {ex.Message}");
                return (null, null);
            }
        }
    }

    /// <summary>
    /// Quick capture command for rapid-fire captures
    /// </summary>
    public class VesselQuickCaptureCommand : Command
    {
#if DEV
        public override string EnglishName => "DevVesselQuickCapture";
#else
        public override string EnglishName => "VesselQuickCapture";
#endif

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var settings = VesselStudioSettings.Load();
            
            if (string.IsNullOrEmpty(settings.ApiKey))
            {
                RhinoApp.WriteLine("❌ API key not set. Run VesselSetApiKey first.");
                return Result.Failure;
            }

            if (string.IsNullOrEmpty(settings.LastProjectId))
            {
                RhinoApp.WriteLine("❌ No project selected. Run VesselCapture first to choose a project.");
                return Result.Failure;
            }

            var apiClient = new VesselStudioApiClient();
            apiClient.SetApiKey(settings.ApiKey);
            
            RhinoApp.WriteLine($"📸 Quick capturing to {settings.LastProjectName}...");

            // Capture viewport (fast operation)
            var captureData = CaptureViewportForQuickCapture(doc);
            
            if (captureData.imageBytes == null)
            {
                RhinoApp.WriteLine("❌ No active viewport or capture failed");
                return Result.Failure;
            }

            // Auto-generate name with timestamp
            var imageName = $"Quick Capture {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            
            // Start background upload immediately - don't block!
            RhinoApp.WriteLine($"📤 Uploading in background...");
            RhinoApp.WriteLine("💡 You can continue working - upload happens in background");
            
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var result = await apiClient.UploadScreenshotAsync(
                        settings.LastProjectId,
                        captureData.imageBytes,
                        imageName,
                        captureData.metadata
                    );

                    if (result.Success)
                    {
                        RhinoApp.WriteLine($"✅ Quick capture upload complete!");
                        RhinoApp.WriteLine($"📷 View at: {result.Url}");
                    }
                    else
                    {
                        RhinoApp.WriteLine($"❌ Upload failed: {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine($"❌ Upload error: {ex.Message}");
                }
            });
            
            return Result.Success;
        }
        
        private (byte[] imageBytes, Dictionary<string, object> metadata) CaptureViewportForQuickCapture(RhinoDoc doc)
        {
            try
            {
                var view = doc.Views.ActiveView;
                if (view == null)
                {
                    return (null, null);
                }

                // Capture viewport as bitmap
                var bitmap = view.CaptureToBitmap(new Size(1920, 1080));
                
                // Convert to byte array
                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    imageBytes = ms.ToArray();
                }

                // Gather metadata
                var metadata = new Dictionary<string, object>
                {
                    { "width", 1920 },
                    { "height", 1080 },
                    { "viewportName", view.MainViewport.Name },
                    { "displayMode", view.MainViewport.DisplayMode.EnglishName },
                    { "rhinoVersion", RhinoApp.Version.ToString() },
                    { "captureTime", DateTime.UtcNow.ToString("o") }
                };

                return (imageBytes, metadata);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Capture error: {ex.Message}");
                return (null, null);
            }
        }
    }

    /// <summary>
    /// Simple dialog for entering image name
    /// </summary>
    internal class ImageNameDialog : Form
    {
        private TextBox nameTextBox;
        private Button okButton;
        private Button cancelButton;

        public string ImageName { get; private set; }

        public ImageNameDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Set up form properties
            Text = "Capture Screenshot";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            AutoSize = false;
            Padding = new Padding(20);

            // Define layout constants
            const int controlWidth = 400;
            const int verticalSpacing = 30;
            const int labelControlSpacing = 8;
            int yPos = 20;

            // Name label
            var nameLabel = new Label
            {
                Text = "Image name (required):",
                Location = new Point(20, yPos),
                AutoSize = true
            };
            Controls.Add(nameLabel);
            yPos += nameLabel.Height + labelControlSpacing;

            // Name textbox (empty by default - user must enter name)
            nameTextBox = new TextBox
            {
                Location = new Point(20, yPos),
                Width = controlWidth,
                Text = "" // No default value - user must type their own
            };
            nameTextBox.TextChanged += (s, e) =>
            {
                okButton.Enabled = !string.IsNullOrWhiteSpace(nameTextBox.Text);
            };
            Controls.Add(nameTextBox);
            yPos += nameTextBox.Height + verticalSpacing + 10;

            // Cancel button (place first to calculate OK button position)
            cancelButton = new Button
            {
                Text = "Cancel",
                Width = 80,
                Height = 32,
                DialogResult = DialogResult.Cancel
            };
            cancelButton.Location = new Point(20 + controlWidth - cancelButton.Width, yPos);
            Controls.Add(cancelButton);

            // OK button (place to left of cancel) - disabled until text is entered
            okButton = new Button
            {
                Text = "Capture",
                Width = 100,
                Height = 32,
                DialogResult = DialogResult.OK,
                Enabled = false // Disabled until user enters text
            };
            okButton.Location = new Point(cancelButton.Left - okButton.Width - 10, yPos);
            okButton.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    System.Windows.Forms.MessageBox.Show(
                        "Please enter an image name.", 
                        "Name Required", 
                        System.Windows.Forms.MessageBoxButtons.OK, 
                        System.Windows.Forms.MessageBoxIcon.Warning);
                    return;
                }
                
                ImageName = nameTextBox.Text.Trim();
            };
            Controls.Add(okButton);
            yPos += okButton.Height + 20;

            // Set final form size with padding
            ClientSize = new Size(controlWidth + 40, yPos);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}

