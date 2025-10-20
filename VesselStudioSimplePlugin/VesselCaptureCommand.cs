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
    /// Enhanced capture command with project selection dialog
    /// </summary>
    public class VesselCaptureCommand : Command
    {
        public override string EnglishName => "VesselCapture";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Load settings
            var settings = VesselStudioSettings.Load();
            
            if (string.IsNullOrEmpty(settings.ApiKey))
            {
                RhinoApp.WriteLine("❌ API key not set. Run VesselSetApiKey first.");
                return Result.Failure;
            }

            var apiClient = new VesselStudioApiClient();
            apiClient.SetApiKey(settings.ApiKey);

            // Get projects
            RhinoApp.WriteLine("Loading projects...");
            var projectsTask = apiClient.GetProjectsAsync();
            projectsTask.Wait();
            var projects = projectsTask.Result;

            if (projects == null || projects.Count == 0)
            {
                RhinoApp.WriteLine("❌ No projects found. Create a project on vessel.one first.");
                return Result.Failure;
            }

            // Show capture dialog
            var dialog = new CaptureDialog(projects, settings);
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return Result.Cancel;
            }

            var selectedProject = dialog.SelectedProject;
            var imageName = dialog.ImageName;

            // Perform capture
            RhinoApp.WriteLine($"📸 Capturing viewport...");
            var captureResult = PerformCapture(doc, apiClient, selectedProject.Id, imageName);

            if (captureResult.success)
            {
                RhinoApp.WriteLine($"✅ {captureResult.message}");
                RhinoApp.WriteLine($"📷 View at: {captureResult.imageUrl}");
                
                // Save last used project
                settings.LastProjectId = selectedProject.Id;
                settings.LastProjectName = selectedProject.Name;
                settings.Save();
                
                return Result.Success;
            }
            else
            {
                RhinoApp.WriteLine($"❌ {captureResult.message}");
                return Result.Failure;
            }
        }

        private (bool success, string message, string imageUrl) PerformCapture(
            RhinoDoc doc,
            VesselStudioApiClient apiClient,
            string projectId,
            string imageName)
        {
            try
            {
                var view = doc.Views.ActiveView;
                if (view == null)
                {
                    return (false, "No active viewport", null);
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

                // Upload
                var uploadTask = apiClient.UploadScreenshotAsync(
                    projectId,
                    imageBytes,
                    imageName,
                    metadata
                );

                uploadTask.Wait();
                var result = uploadTask.Result;

                return (result.Success, result.Message, result.Url);
            }
            catch (Exception ex)
            {
                return (false, $"Capture error: {ex.Message}", null);
            }
        }
    }

    /// <summary>
    /// Quick capture command for rapid-fire captures
    /// </summary>
    public class VesselQuickCaptureCommand : Command
    {
        public override string EnglishName => "VesselQuickCapture";

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

            var view = doc.Views.ActiveView;
            if (view == null)
            {
                RhinoApp.WriteLine("❌ No active viewport");
                return Result.Failure;
            }

            try
            {
                // Capture viewport
                var bitmap = view.CaptureToBitmap(new Size(1920, 1080));
                
                byte[] imageBytes;
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    imageBytes = ms.ToArray();
                }

                // Auto-generate name with timestamp
                var imageName = $"Quick Capture {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

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

                // Upload
                var uploadTask = apiClient.UploadScreenshotAsync(
                    settings.LastProjectId,
                    imageBytes,
                    imageName,
                    metadata
                );

                uploadTask.Wait();
                var result = uploadTask.Result;

                if (result.Success)
                {
                    RhinoApp.WriteLine($"✅ {result.Message}");
                    return Result.Success;
                }
                else
                {
                    RhinoApp.WriteLine($"❌ {result.Message}");
                    return Result.Failure;
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Error: {ex.Message}");
                return Result.Failure;
            }
        }
    }

    /// <summary>
    /// Simple WinForms dialog for project selection
    /// </summary>
    internal class CaptureDialog : Form
    {
        private ComboBox projectComboBox;
        private TextBox nameTextBox;
        private Button okButton;
        private Button cancelButton;

        public VesselProject SelectedProject { get; private set; }
        public string ImageName { get; private set; }

        public CaptureDialog(List<VesselProject> projects, VesselStudioSettings settings)
        {
            InitializeComponent(projects, settings);
        }

        private void InitializeComponent(List<VesselProject> projects, VesselStudioSettings settings)
        {
            Text = "Capture to Vessel One";
            Width = 400;
            Height = 200;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;

            // Project label
            var projectLabel = new Label
            {
                Text = "Send to project:",
                Location = new Point(20, 20),
                AutoSize = true
            };
            Controls.Add(projectLabel);

            // Project dropdown
            projectComboBox = new ComboBox
            {
                Location = new Point(20, 45),
                Width = 340,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            projectComboBox.DisplayMember = "Name";
            projectComboBox.ValueMember = "Id";
            projectComboBox.DataSource = projects;

            // Pre-select last used project
            if (!string.IsNullOrEmpty(settings.LastProjectId))
            {
                var lastProject = projects.FirstOrDefault(p => p.Id == settings.LastProjectId);
                if (lastProject != null)
                {
                    projectComboBox.SelectedItem = lastProject;
                }
            }

            Controls.Add(projectComboBox);

            // Name label
            var nameLabel = new Label
            {
                Text = "Image name (optional):",
                Location = new Point(20, 80),
                AutoSize = true
            };
            Controls.Add(nameLabel);

            // Name textbox
            nameTextBox = new TextBox
            {
                Location = new Point(20, 105),
                Width = 340,
                Text = $"Rhino Capture {DateTime.Now:HH:mm:ss}"
            };
            Controls.Add(nameTextBox);

            // OK button
            okButton = new Button
            {
                Text = "Capture && Upload",
                Location = new Point(180, 140),
                Width = 120,
                DialogResult = DialogResult.OK
            };
            okButton.Click += (s, e) =>
            {
                SelectedProject = projectComboBox.SelectedItem as VesselProject;
                ImageName = string.IsNullOrWhiteSpace(nameTextBox.Text)
                    ? $"Rhino Capture {DateTime.Now:HH:mm:ss}"
                    : nameTextBox.Text;
            };
            Controls.Add(okButton);

            // Cancel button
            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(310, 140),
                Width = 70,
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}
