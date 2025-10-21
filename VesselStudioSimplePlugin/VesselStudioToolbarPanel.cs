using System;
using System.Drawing;
using System.Windows.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Dockable panel with toolbar buttons for Vessel Studio
    /// </summary>
    [System.Runtime.InteropServices.Guid("A5B6C7D8-E9F0-4A5B-8C9D-0E1F2A3B4C5D")]
    public class VesselStudioToolbarPanel : Panel
    {
        private Button _captureButton;
        private Button _quickCaptureButton;
        private Button _settingsButton;
        private Label _statusLabel;
        private Panel _statusPanel;

        public VesselStudioToolbarPanel()
        {
            InitializeComponents();
            UpdateStatus();
        }

        private void InitializeComponents()
        {
            // Main layout
            this.Padding = new Padding(10);
            this.BackColor = Color.FromArgb(240, 240, 240);

            // Title
            var titleLabel = new Label
            {
                Text = "Vessel Studio",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 10)
            };
            this.Controls.Add(titleLabel);

            // Status panel
            _statusPanel = new Panel
            {
                Location = new Point(10, 40),
                Size = new Size(260, 50),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            _statusLabel = new Label
            {
                Text = "Not configured",
                Location = new Point(10, 5),
                Size = new Size(240, 40),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9)
            };
            _statusPanel.Controls.Add(_statusLabel);
            this.Controls.Add(_statusPanel);

            // Settings button
            _settingsButton = CreateButton("⚙ Set API Key", 10, 100, OnSettingsClick);
            _settingsButton.BackColor = Color.FromArgb(70, 130, 180);
            _settingsButton.ForeColor = Color.White;
            this.Controls.Add(_settingsButton);

            // Capture button
            _captureButton = CreateButton("📷 Capture Screenshot", 10, 145, OnCaptureClick);
            _captureButton.BackColor = Color.FromArgb(76, 175, 80);
            _captureButton.ForeColor = Color.White;
            this.Controls.Add(_captureButton);

            // Quick capture button
            _quickCaptureButton = CreateButton("⚡ Quick Capture", 10, 190, OnQuickCaptureClick);
            _quickCaptureButton.BackColor = Color.FromArgb(255, 152, 0);
            _quickCaptureButton.ForeColor = Color.White;
            this.Controls.Add(_quickCaptureButton);

            // Help text
            var helpLabel = new Label
            {
                Text = "Quick Capture saves to\nthe last used project",
                Location = new Point(10, 240),
                Size = new Size(260, 35),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };
            this.Controls.Add(helpLabel);

            // Documentation link
            var docLink = new LinkLabel
            {
                Text = "📖 View Documentation",
                Location = new Point(10, 285),
                Size = new Size(260, 20),
                Font = new Font("Segoe UI", 9),
                LinkColor = Color.FromArgb(70, 130, 180)
            };
            docLink.LinkClicked += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("https://vesselstudio.io/docs/rhino-plugin");
                }
                catch { }
            };
            this.Controls.Add(docLink);

            // About link
            var aboutLink = new LinkLabel
            {
                Text = "ℹ About Plugin",
                Location = new Point(10, 310),
                Size = new Size(260, 20),
                Font = new Font("Segoe UI", 9),
                LinkColor = Color.FromArgb(70, 130, 180)
            };
            aboutLink.LinkClicked += (s, e) =>
            {
                RhinoApp.RunScript("VesselStudioAbout", false);
            };
            this.Controls.Add(aboutLink);
        }

        private Button CreateButton(string text, int x, int y, EventHandler onClick)
        {
            var button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(260, 35),
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += onClick;
            return button;
        }

        private void OnSettingsClick(object sender, EventArgs e)
        {
            RhinoApp.RunScript("VesselSetApiKey", false);
            UpdateStatus();
        }

        private void OnCaptureClick(object sender, EventArgs e)
        {
            var settings = VesselStudioSettings.Load();
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                RhinoApp.WriteLine("❌ Please set your API key first. Run VesselSetApiKey command or use the '⚙ Set API Key' button.");
                return;
            }

            RhinoApp.RunScript("VesselCapture", false);
        }

        private void OnQuickCaptureClick(object sender, EventArgs e)
        {
            var settings = VesselStudioSettings.Load();
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                RhinoApp.WriteLine("❌ Please set your API key first. Run VesselSetApiKey command or use the '⚙ Set API Key' button.");
                return;
            }

            if (string.IsNullOrEmpty(settings.LastProjectId))
            {
                RhinoApp.WriteLine("❌ Please use 'Capture Screenshot' at least once to select a project.");
                return;
            }

            RhinoApp.RunScript("VesselQuickCapture", false);
        }

        private void UpdateStatus()
        {
            try
            {
                var settings = VesselStudioSettings.Load();
                
                if (string.IsNullOrEmpty(settings?.ApiKey))
                {
                    _statusLabel.Text = "❌ Not configured\nSet your API key to get started";
                    _statusLabel.ForeColor = Color.FromArgb(200, 50, 50);
                    _captureButton.Enabled = false;
                    _quickCaptureButton.Enabled = false;
                }
                else if (!string.IsNullOrEmpty(settings.LastProjectName))
                {
                    _statusLabel.Text = $"✓ Connected\nLast project: {settings.LastProjectName}";
                    _statusLabel.ForeColor = Color.FromArgb(76, 175, 80);
                    _captureButton.Enabled = true;
                    _quickCaptureButton.Enabled = true;
                }
                else
                {
                    _statusLabel.Text = "✓ API key configured\nReady to capture";
                    _statusLabel.ForeColor = Color.FromArgb(70, 130, 180);
                    _captureButton.Enabled = true;
                    _quickCaptureButton.Enabled = false;
                }
            }
            catch
            {
                _statusLabel.Text = "❌ Error reading status";
                _statusLabel.ForeColor = Color.FromArgb(200, 50, 50);
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                UpdateStatus();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _captureButton?.Dispose();
                _quickCaptureButton?.Dispose();
                _settingsButton?.Dispose();
                _statusLabel?.Dispose();
                _statusPanel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
