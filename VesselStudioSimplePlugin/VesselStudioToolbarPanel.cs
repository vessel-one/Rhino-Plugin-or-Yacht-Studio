using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.UI;
using VesselStudioSimplePlugin.Models;
using VesselStudioSimplePlugin.Services;
using VesselStudioSimplePlugin.UI;

namespace VesselStudioSimplePlugin
{
    /// <summary>
    /// Dockable panel with toolbar buttons for Vessel Studio - Modern WinForms Design
    /// </summary>
#if DEV
    [System.Runtime.InteropServices.Guid("D5E6F7A8-B9C0-1D2E-3F4A-5B6C7D8E9F0A")] // DEV GUID
#else
    [System.Runtime.InteropServices.Guid("A5B6C7D8-E9F0-4A5B-8C9D-0E1F2A3B4C5D")] // RELEASE GUID
#endif
    public class VesselStudioToolbarPanel : Panel
    {
        private Button _captureButton;
        private Button _addToQueueButton;
        private Button _quickExportBatchButton;
        private Button _settingsButton;
        private Button _refreshProjectsButton;
        private ComboBox _projectComboBox;
        private Label _statusLabel;
        private Label _projectLabel;
        private Label _batchBadgeLabel;
        private Panel _statusPanel;
        private List<VesselProject> _projects;

        public VesselStudioToolbarPanel()
        {
            InitializeComponents();
            UpdateStatus();
            LoadProjectsAsync();

            // T022-T024: Subscribe to queue events for badge updates
            CaptureQueueService.Current.ItemAdded += OnQueueItemAdded;
            CaptureQueueService.Current.ItemRemoved += OnQueueItemRemoved;
            CaptureQueueService.Current.QueueCleared += OnQueueCleared;

            // Update badge on initialization
            UpdateBatchBadge();
        }

        private void InitializeComponents()
        {
            // Main panel configuration with auto-scroll
            this.AutoScroll = true;
            this.Padding = new Padding(15);
            this.BackColor = Color.FromArgb(248, 249, 250);

#if DEV
            var titleText = "Vessel Studio DEV";
            var primaryColor = Color.FromArgb(255, 140, 0); // Orange for DEV
#else
            var titleText = "Vessel Studio";
            var primaryColor = Color.FromArgb(64, 123, 255); // Modern blue
#endif

            // Use TableLayoutPanel for responsive layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 13,
                Padding = new Padding(0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            this.Controls.Add(mainLayout);

            // Configure rows
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // Title + Settings
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));  // Status card
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));  // Project label
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Project dropdown
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));  // Capture button
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // Add to queue
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // Batch badge
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // Export batch
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 85F));  // Info card
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));  // Doc link
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));  // About link
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Spacer

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Row 0: Title + Settings button
            var headerPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0, 0, 0, 5)
            };

            var titleLabel = new Label
            {
                Text = titleText,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = primaryColor,
                AutoSize = true,
                Margin = new Padding(0, 8, 10, 0)
            };
            headerPanel.Controls.Add(titleLabel);

            // Settings button
            _settingsButton = new Button
            {
                Text = "Settings",
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Standard,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(0, 5, 0, 0),
                Cursor = Cursors.Hand
            };
            _settingsButton.Click += OnSettingsClick;
            var settingsTooltip = new ToolTip();
            settingsTooltip.SetToolTip(_settingsButton, "API Key & Image Format Settings");
            headerPanel.Controls.Add(_settingsButton);

            mainLayout.Controls.Add(headerPanel, 0, 0);

            // Row 1: Status card
            _statusPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10),
                Margin = new Padding(0, 5, 0, 10)
            };

            _statusLabel = new Label
            {
                Text = "Not configured",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = false
            };
            _statusPanel.Controls.Add(_statusLabel);
            mainLayout.Controls.Add(_statusPanel, 0, 1);

            // Row 2: Project section header
            _projectLabel = new Label
            {
                Text = "SELECT PROJECT",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 120, 120),
                Margin = new Padding(0, 5, 0, 5),
                AutoSize = false
            };
            mainLayout.Controls.Add(_projectLabel, 0, 2);

            // Row 3: Project dropdown + refresh button
            var projectPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0, 0, 0, 10)
            };

            _projectComboBox = new ComboBox
            {
                Width = 190,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Font = new Font("Segoe UI", 9.5f),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0)
            };
            _projectComboBox.DisplayMember = "Name";
            _projectComboBox.ValueMember = "Id";
            _projectComboBox.SelectedIndexChanged += OnProjectChanged;
            projectPanel.Controls.Add(_projectComboBox);

            _refreshProjectsButton = new Button
            {
                Text = "Reload",
                Size = new Size(80, 23),
                FlatStyle = FlatStyle.Standard,
                Margin = new Padding(5, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            _refreshProjectsButton.Click += OnRefreshProjectsClick;
            projectPanel.Controls.Add(_refreshProjectsButton);

            mainLayout.Controls.Add(projectPanel, 0, 3);

            // Row 4: Capture button
            _captureButton = new Button
            {
                Text = "📷 Capture Screenshot",
                Dock = DockStyle.Fill,
                Height = 45,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Standard,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };
            _captureButton.Click += OnCaptureClick;
            mainLayout.Controls.Add(_captureButton, 0, 4);

            // Row 5: Add to Queue button
            _addToQueueButton = new Button
            {
                Text = "➕ Add to Batch Queue",
                Dock = DockStyle.Fill,
                Height = 40,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Standard,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };
            _addToQueueButton.Click += OnAddToQueueClick;
            mainLayout.Controls.Add(_addToQueueButton, 0, 5);

            // Row 6: Batch badge
            _batchBadgeLabel = new Label
            {
                Text = "📦 Batch (0)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 107, 53),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false,
                Margin = new Padding(0, 0, 0, 5),
                Cursor = Cursors.Hand,
                AutoSize = false
            };
            _batchBadgeLabel.Click += OnBatchBadgeClick;
            mainLayout.Controls.Add(_batchBadgeLabel, 0, 6);

            // Row 7: Quick Export Batch button
            _quickExportBatchButton = new Button
            {
                Text = "📤 Quick Export Batch",
                Dock = DockStyle.Fill,
                Height = 40,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Standard,
                Enabled = false,
                Margin = new Padding(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };
            _quickExportBatchButton.Click += OnQuickExportBatchClick;
            mainLayout.Controls.Add(_quickExportBatchButton, 0, 7);

            // Row 8: Info card
            var infoCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 248, 255),
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(10),
                Margin = new Padding(0, 0, 0, 10)
            };

            var helpLabel = new Label
            {
                Text = "💡 Quick Tip\nQueue captures then export\nthe batch when ready!",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = false
            };
            infoCard.Controls.Add(helpLabel);
            mainLayout.Controls.Add(infoCard, 0, 8);

            // Row 9: Documentation link
            var docLink = new LinkLabel
            {
                Text = "📖 Documentation",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                LinkColor = primaryColor,
                Margin = new Padding(0, 5, 0, 0),
                AutoSize = false
            };
            docLink.LinkClicked += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start("https://vesselstudio.io/docs/rhino-plugin");
                }
                catch { }
            };
            mainLayout.Controls.Add(docLink, 0, 9);

            // Row 10: About link
            var aboutLink = new LinkLabel
            {
                Text = "ℹ️ About Plugin",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                LinkColor = primaryColor,
                Margin = new Padding(0, 5, 0, 0),
                AutoSize = false
            };
            aboutLink.LinkClicked += (s, e) =>
            {
#if DEV
                RhinoApp.RunScript("DevVesselStudioAbout", false);
#else
                RhinoApp.RunScript("VesselStudioAbout", false);
#endif
            };
            mainLayout.Controls.Add(aboutLink, 0, 10);
        }

        private void OnSettingsClick(object sender, EventArgs e)
        {
            // Open combined settings dialog directly (same as Batch Manager)
            using (var dialog = new VesselStudioSettingsDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    RhinoApp.WriteLine("✅ Settings saved successfully");
                    UpdateStatus();
                    // Reload projects after settings change
                    LoadProjectsAsync();
                }
            }
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

        /// <summary>
        /// T021: Handle "Add to Batch Queue" button click
        /// Executes VesselAddToQueueCommand to capture and queue the viewport
        /// </summary>
        private void OnAddToQueueClick(object sender, EventArgs e)
        {
#if DEV
            RhinoApp.RunScript("DevVesselAddToQueue", false);
#else
            RhinoApp.RunScript("VesselAddToQueue", false);
#endif
        }

        /// <summary>
        /// T024: Handle batch badge click (placeholder for queue manager dialog in Phase 4)
        /// Currently just shows queue count and guidance
        /// </summary>
        private void OnBatchBadgeClick(object sender, EventArgs e)
        {
            var count = CaptureQueueService.Current.ItemCount;
            RhinoApp.WriteLine($"📦 Batch queue: {count} item{(count == 1 ? "" : "s")}");
            RhinoApp.WriteLine("💡 Phase 4: Queue Manager dialog coming soon to view and manage captures");
        }

        /// <summary>
        /// T040: Handle "Quick Export Batch" button click
        /// Opens the QueueManagerDialog for reviewing and managing queued captures
        /// (Phase 4 implementation: Dialog management, Phase 5: Upload functionality)
        /// </summary>
        private void OnQuickExportBatchClick(object sender, EventArgs e)
        {
            // T040: Open Queue Manager Dialog
            using (var dialog = new QueueManagerDialog())
            {
                dialog.ShowDialog();
            }
            
            // Refresh UI after dialog closes
            UpdateBatchBadge();
        }

        /// <summary>
        /// T022: Handle ItemAdded event from CaptureQueueService
        /// Updates batch badge to show new queue count
        /// </summary>
        private void OnQueueItemAdded(object sender, ItemAddedEventArgs e)
        {
            UpdateBatchBadge();
        }

        /// <summary>
        /// T023: Handle ItemRemoved event from CaptureQueueService
        /// Updates batch badge to show new queue count
        /// </summary>
        private void OnQueueItemRemoved(object sender, ItemRemovedEventArgs e)
        {
            UpdateBatchBadge();
        }

        /// <summary>
        /// T023: Handle QueueCleared event from CaptureQueueService
        /// Updates batch badge to reflect empty queue
        /// </summary>
        private void OnQueueCleared(object sender, EventArgs e)
        {
            UpdateBatchBadge();
        }

        /// <summary>
        /// T022-T024: Update batch badge label visibility and text based on queue count
        /// Visible when count > 0, hidden when count = 0
        /// T054: Also enable/disable Quick Export button based on queue count
        /// </summary>
        private void UpdateBatchBadge()
        {
            // Thread-safe UI update
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateBatchBadge));
                return;
            }

            var count = CaptureQueueService.Current.ItemCount;

            if (count > 0)
            {
                _batchBadgeLabel.Text = $"📦 Batch ({count})";
                _batchBadgeLabel.Visible = true;
                // T054: Enable Quick Export button when queue has items
                _quickExportBatchButton.Enabled = true;
            }
            else
            {
                _batchBadgeLabel.Visible = false;
                // T054: Disable Quick Export button when queue is empty (FR-012)
                _quickExportBatchButton.Enabled = false;
            }
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

                // If we got 0 projects, could mean authentication failed or user deleted
                if (_projects == null || _projects.Count == 0)
                {
                    // Validate API key to check if it's still valid
                    var validation = await apiClient.ValidateApiKeyAsync();
                    
                    if (!validation.Success)
                    {
                        // Authentication failed - clear settings
                        settings.ApiKey = null;
                        settings.LastProjectId = null;
                        settings.LastProjectName = null;
                        settings.HasValidSubscription = false;
                        settings.Save();
                        
                        RhinoApp.WriteLine($"❌ Authentication failed: {validation.ErrorMessage}");
                        RhinoApp.WriteLine("Please reconfigure your API key in the settings.");
                        
                        // Update UI to disconnected state
                        if (InvokeRequired)
                        {
                            Invoke(new Action(() => {
                                _projectComboBox.DataSource = null;
                                _projectComboBox.Enabled = false;
                                UpdateStatus();
                            }));
                        }
                        else
                        {
                            _projectComboBox.DataSource = null;
                            _projectComboBox.Enabled = false;
                            UpdateStatus();
                        }
                        return;
                    }
                }

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
            
            // Clear everything
            _projectComboBox.DataSource = null;
            _projectComboBox.DisplayMember = "";
            _projectComboBox.ValueMember = "";
            _projectComboBox.Items.Clear();
            RhinoApp.WriteLine("DEBUG: ComboBox cleared completely");
            
            // Add items manually instead of using data binding
            foreach (var project in _projects)
            {
                _projectComboBox.Items.Add(project);
                RhinoApp.WriteLine($"DEBUG: Added project: {project.Name} (ID: {project.Id})");
            }
            
            // NOW set display/value members AFTER items are added
            _projectComboBox.DisplayMember = "Name";
            _projectComboBox.ValueMember = "Id";
            _projectComboBox.Enabled = true;
            
            RhinoApp.WriteLine($"DEBUG: After manual population - Items.Count={_projectComboBox.Items.Count}");
            
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
                else if (!settings.HasValidSubscription)
                {
                    // API key exists but subscription is invalid or auth failed
                    _statusLabel.Text = "❌ Authentication failed\nReconfigure your API key";
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
                _addToQueueButton?.Dispose();
                _quickExportBatchButton?.Dispose();
                _settingsButton?.Dispose();
                _refreshProjectsButton?.Dispose();
                _projectComboBox?.Dispose();
                _projectLabel?.Dispose();
                _statusLabel?.Dispose();
                _batchBadgeLabel?.Dispose();
                _statusPanel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Modern button with rounded corners, hover effects, and smooth animations
    /// </summary>
    public class ModernButton : Button
    {
        private Color _baseColor;
        private Color _hoverColor;
        private Color _pressColor;
        private bool _isHovering;
        private bool _isPressing;
        private System.Windows.Forms.Timer _animationTimer;
        private float _animationProgress;
        public bool IsIconButton { get; set; }

        public ModernButton(string text, Color baseColor) : base()
        {
            this.Text = text;
            _baseColor = baseColor;
            _hoverColor = LightenColor(baseColor, 20);
            _pressColor = DarkenColor(baseColor, 20);
            
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.BackColor = baseColor;
            this.ForeColor = Color.White;
            this.Cursor = Cursors.Hand;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            
            // Enable double buffering for smooth animations
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.OptimizedDoubleBuffer, true);

            // Animation timer
            _animationTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_isHovering && _animationProgress < 1.0f)
            {
                _animationProgress += 0.15f;
                if (_animationProgress >= 1.0f) _animationProgress = 1.0f;
                this.Invalidate();
            }
            else if (!_isHovering && _animationProgress > 0.0f)
            {
                _animationProgress -= 0.15f;
                if (_animationProgress <= 0.0f) _animationProgress = 0.0f;
                this.Invalidate();
            }
            else
            {
                _animationTimer.Stop();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovering = true;
            _animationTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovering = false;
            _animationTimer.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _isPressing = true;
            this.Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressing = false;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None; // Disable anti-aliasing to avoid transparency artifacts
            g.PixelOffsetMode = PixelOffsetMode.Half;

            // Calculate current color based on animation progress
            Color currentColor;
            if (_isPressing)
            {
                currentColor = _pressColor;
            }
            else if (_animationProgress > 0)
            {
                currentColor = BlendColors(_baseColor, _hoverColor, _animationProgress);
            }
            else
            {
                currentColor = _baseColor;
            }

            // Adjust for disabled state
            if (!this.Enabled)
            {
                currentColor = Color.FromArgb(180, 180, 180);
            }

            // Clear background first with solid color
            using (var bgBrush = new SolidBrush(this.Parent.BackColor))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            // Use flat style without rounded corners - cleaner rendering
            using (var brush = new SolidBrush(currentColor))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            // Draw subtle border for depth
            if (this.Enabled)
            {
                var borderColor = _isPressing ? DarkenColor(currentColor, 30) : DarkenColor(currentColor, 15);
                using (var borderPen = new Pen(borderColor, 1))
                {
                    g.DrawRectangle(borderPen, 0, 0, ClientRectangle.Width - 1, ClientRectangle.Height - 1);
                }
            }

            // Draw text
            var textColor = this.Enabled ? Color.White : Color.FromArgb(150, 150, 150);
            TextRenderer.DrawText(g, this.Text, this.Font, ClientRectangle, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private Color LightenColor(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Min(255, color.R + amount),
                Math.Min(255, color.G + amount),
                Math.Min(255, color.B + amount));
        }

        private Color DarkenColor(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Max(0, color.R - amount),
                Math.Max(0, color.G - amount),
                Math.Max(0, color.B - amount));
        }

        private Color BlendColors(Color c1, Color c2, float ratio)
        {
            ratio = Math.Max(0, Math.Min(1, ratio));
            return Color.FromArgb(
                (int)(c1.R + (c2.R - c1.R) * ratio),
                (int)(c1.G + (c2.G - c1.G) * ratio),
                (int)(c1.B + (c2.B - c1.B) * ratio));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Card-style panel with subtle border
    /// </summary>
    public class CardPanel : Panel
    {
        public CardPanel()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.OptimizedDoubleBuffer, true);
            this.Padding = new Padding(10);
            this.BorderStyle = BorderStyle.None;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            
            // Draw solid background
            using (var brush = new SolidBrush(this.BackColor))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            // Draw border
            using (var borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
            {
                g.DrawRectangle(borderPen, 0, 0, ClientRectangle.Width - 1, ClientRectangle.Height - 1);
            }
        }
    }

    /// <summary>
    /// Modern link label with hover effect
    /// </summary>
    public class ModernLinkLabel : LinkLabel
    {
        public ModernLinkLabel(string text, Color linkColor)
        {
            this.Text = text;
            this.Font = new Font("Segoe UI", 9);
            this.LinkColor = linkColor;
            this.ActiveLinkColor = DarkenColor(linkColor, 30);
            this.VisitedLinkColor = linkColor;
            this.LinkBehavior = LinkBehavior.HoverUnderline;
            this.Cursor = Cursors.Hand;
        }

        private Color DarkenColor(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Max(0, color.R - amount),
                Math.Max(0, color.G - amount),
                Math.Max(0, color.B - amount));
        }
    }
}

