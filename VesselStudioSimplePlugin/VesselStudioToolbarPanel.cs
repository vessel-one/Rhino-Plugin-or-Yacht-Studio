using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Dockable panel with toolbar buttons for Vessel Studio
    /// </summary>
#if DEV
    [System.Runtime.InteropServices.Guid("D5E6F7A8-B9C0-1D2E-3F4A-5B6C7D8E9F0A")] // DEV GUID
#else
    [System.Runtime.InteropServices.Guid("A5B6C7D8-E9F0-4A5B-8C9D-0E1F2A3B4C5D")] // RELEASE GUID
#endif
    public class VesselStudioToolbarPanel : Panel
    {
        private Button _captureButton;
        private Button _settingsButton;
        private Button _refreshProjectsButton;
        private ComboBox _projectComboBox;
        private Label _statusLabel;
        private Label _projectLabel;
        private Panel _statusPanel;
        private List<VesselProject> _projects;

        public VesselStudioToolbarPanel()
        {
            InitializeComponents();
            UpdateStatus();
            LoadProjectsAsync();
        }

        private void InitializeComponents()
        {
            // Main layout
            this.Padding = new Padding(10);
            this.BackColor = Color.FromArgb(240, 240, 240);

            int yPos = 10;

            // Title
#if DEV
            var titleText = "Vessel Studio DEV";
            var titleColor = Color.FromArgb(255, 140, 0); // Orange for DEV
#else
            var titleText = "Vessel Studio";
            var titleColor = Color.FromArgb(70, 130, 180); // Blue for production
#endif
            
            var titleLabel = new Label
            {
                Text = titleText,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = titleColor,
                AutoSize = true,
                Location = new Point(10, yPos)
            };
            this.Controls.Add(titleLabel);
            yPos += 35; // Space for title + padding

            // Status panel
            _statusPanel = new Panel
            {
                Location = new Point(10, yPos),
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
            yPos += 60; // Status panel height + padding

            // Settings button
            _settingsButton = CreateButton("⚙ Set API Key", 10, yPos, OnSettingsClick);
            _settingsButton.BackColor = Color.FromArgb(70, 130, 180);
            _settingsButton.ForeColor = Color.White;
            this.Controls.Add(_settingsButton);
            yPos += 45; // Button height + padding

            // Project selection section
            _projectLabel = new Label
            {
                Text = "Select Project:",
                Location = new Point(10, yPos),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            this.Controls.Add(_projectLabel);
            yPos += 25;

            // Project dropdown
            _projectComboBox = new ComboBox
            {
                Location = new Point(10, yPos),
                Width = 190,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Font = new Font("Segoe UI", 9)
            };
            _projectComboBox.DisplayMember = "Name";
            _projectComboBox.ValueMember = "Id";
            _projectComboBox.SelectedIndexChanged += OnProjectChanged;
            this.Controls.Add(_projectComboBox);

            // Refresh button next to dropdown
            _refreshProjectsButton = new Button
            {
                Text = "🔄",
                Location = new Point(205, yPos),
                Size = new Size(65, _projectComboBox.Height),
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White
            };
            _refreshProjectsButton.FlatAppearance.BorderSize = 0;
            _refreshProjectsButton.Click += OnRefreshProjectsClick;
            this.Controls.Add(_refreshProjectsButton);
            yPos += 45;

            // Capture button
            _captureButton = CreateButton("📷 Capture", 10, yPos, OnCaptureClick);
            _captureButton.BackColor = Color.FromArgb(76, 175, 80);
            _captureButton.ForeColor = Color.White;
            this.Controls.Add(_captureButton);
            yPos += 45; // Button height + padding

            // Help text
            var helpLabel = new Label
            {
                Text = "Select project, then capture.\nUploads happen in background.",
                Location = new Point(10, yPos),
                Size = new Size(260, 35),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };
            this.Controls.Add(helpLabel);
            yPos += 45; // Help text height + padding

            // Documentation link
            var docLink = new LinkLabel
            {
                Text = "📖 View Documentation",
                Location = new Point(10, yPos),
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
            yPos += 25; // Link height + padding

            // About link
            var aboutLink = new LinkLabel
            {
                Text = "ℹ About Plugin",
                Location = new Point(10, yPos),
                Size = new Size(260, 20),
                Font = new Font("Segoe UI", 9),
                LinkColor = Color.FromArgb(70, 130, 180)
            };
            aboutLink.LinkClicked += (s, e) =>
            {
#if DEV
                RhinoApp.RunScript("DevVesselStudioAbout", false);
#else
                RhinoApp.RunScript("VesselStudioAbout", false);
#endif
            };
            this.Controls.Add(aboutLink);
            yPos += 30; // Link height + bottom padding to ensure visibility
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
#if DEV
            RhinoApp.RunScript("DevVesselSetApiKey", false);
#else
            RhinoApp.RunScript("VesselSetApiKey", false);
#endif
            UpdateStatus();
            // Reload projects after API key change
            LoadProjectsAsync();
        }

        private void OnProjectChanged(object sender, EventArgs e)
        {
            // Save selected project to settings
            if (_projectComboBox.SelectedItem is VesselProject selectedProject)
            {
                var settings = VesselStudioSettings.Load();
                settings.LastProjectId = selectedProject.Id;
                settings.LastProjectName = selectedProject.Name;
                settings.Save();
                
                RhinoApp.WriteLine($"✓ Project changed to: {selectedProject.Name}");
                UpdateStatus();
            }
        }

        private void OnRefreshProjectsClick(object sender, EventArgs e)
        {
            LoadProjectsAsync();
        }

        private void OnCaptureClick(object sender, EventArgs e)
        {
            var settings = VesselStudioSettings.Load();
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
#if DEV
                RhinoApp.WriteLine("❌ Please set your API key first. Run DevVesselSetApiKey command or use the '⚙ Set API Key' button.");
#else
                RhinoApp.WriteLine("❌ Please set your API key first. Run VesselSetApiKey command or use the '⚙ Set API Key' button.");
#endif
                return;
            }

            if (string.IsNullOrEmpty(settings.LastProjectId))
            {
                RhinoApp.WriteLine("❌ Please select a project from the dropdown first.");
                return;
            }

#if DEV
            RhinoApp.RunScript("DevVesselCapture", false);
#else
            RhinoApp.RunScript("VesselCapture", false);
#endif
        }

        private async void LoadProjectsAsync()
        {
            var settings = VesselStudioSettings.Load();
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                _projectComboBox.Enabled = false;
                _projectComboBox.DataSource = null;
                _refreshProjectsButton.Enabled = false;
                return;
            }

            try
            {
                _projectLabel.Text = "⏳ Loading projects...";
                _projectLabel.ForeColor = Color.Gray;
                _refreshProjectsButton.Enabled = false;
                
                var apiClient = new VesselStudioApiClient();
                apiClient.SetApiKey(settings.ApiKey);
                
                _projects = await apiClient.GetProjectsAsync();

                if (InvokeRequired)
                {
                    Invoke(new Action(() => PopulateProjects()));
                }
                else
                {
                    PopulateProjects();
                }
            }
            catch (Exception ex)
            {
                _projectLabel.Text = "❌ Error loading projects";
                _projectLabel.ForeColor = Color.Red;
                RhinoApp.WriteLine($"Error loading projects: {ex.Message}");
            }
            finally
            {
                _refreshProjectsButton.Enabled = true;
            }
        }

        private void PopulateProjects()
        {
            RhinoApp.WriteLine($"DEBUG: PopulateProjects called. _projects is null: {_projects == null}, Count: {_projects?.Count ?? 0}");
            
            if (_projects == null || _projects.Count == 0)
            {
                _projectLabel.Text = "❌ No projects found";
                _projectLabel.ForeColor = Color.Red;
                _projectComboBox.Enabled = false;
                _projectComboBox.DataSource = null;
                RhinoApp.WriteLine("DEBUG: No projects to populate");
                return;
            }

            RhinoApp.WriteLine($"DEBUG: About to populate {_projects.Count} projects");
            
            // Temporarily disable event handler to avoid triggering during population
            _projectComboBox.SelectedIndexChanged -= OnProjectChanged;
            
            // Clear completely
            _projectComboBox.DataSource = null;
            _projectComboBox.Items.Clear();
            RhinoApp.WriteLine("DEBUG: ComboBox cleared completely");
            
            // IMPORTANT: Set DisplayMember and ValueMember BEFORE DataSource (like working version)
            _projectComboBox.DisplayMember = "Name";
            _projectComboBox.ValueMember = "Id";
            _projectComboBox.DataSource = _projects;
            _projectComboBox.Enabled = true;
            
            RhinoApp.WriteLine($"DEBUG: After binding - DisplayMember={_projectComboBox.DisplayMember}, ValueMember={_projectComboBox.ValueMember}, Items.Count={_projectComboBox.Items.Count}");
            
            // Pre-select last used project
            var settings = VesselStudioSettings.Load();
            if (!string.IsNullOrEmpty(settings.LastProjectId))
            {
                var lastProject = _projects.FirstOrDefault(p => p.Id == settings.LastProjectId);
                if (lastProject != null)
                {
                    _projectComboBox.SelectedItem = lastProject;
                    RhinoApp.WriteLine($"DEBUG: Pre-selected project: {lastProject.Name}");
                }
            }

            _projectLabel.Text = $"✓ {_projects.Count} project(s) loaded";
            _projectLabel.ForeColor = Color.FromArgb(76, 175, 80);
            
            // Re-enable event handler
            _projectComboBox.SelectedIndexChanged += OnProjectChanged;
            
            RhinoApp.WriteLine("DEBUG: PopulateProjects completed successfully");
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
                    _projectComboBox.Enabled = false;
                    _refreshProjectsButton.Enabled = false;
                }
                else if (!string.IsNullOrEmpty(settings.LastProjectName))
                {
                    _statusLabel.Text = $"✓ Connected\nProject: {settings.LastProjectName}";
                    _statusLabel.ForeColor = Color.FromArgb(76, 175, 80);
                    _captureButton.Enabled = true;
                    _refreshProjectsButton.Enabled = true;
                }
                else
                {
                    _statusLabel.Text = "✓ API key configured\nSelect a project to continue";
                    _statusLabel.ForeColor = Color.FromArgb(70, 130, 180);
                    _captureButton.Enabled = false;
                    _refreshProjectsButton.Enabled = true;
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
                _settingsButton?.Dispose();
                _refreshProjectsButton?.Dispose();
                _projectComboBox?.Dispose();
                _projectLabel?.Dispose();
                _statusLabel?.Dispose();
                _statusPanel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
