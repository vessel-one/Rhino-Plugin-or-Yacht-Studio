using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
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
        private List<VesselProject> _projects;

        public VesselStudioToolbarPanel()
        {
            // Enable double buffering to prevent flicker and overlapping artifacts
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                         ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint, true);
            this.UpdateStyles();
            
            InitializeComponents();
            UpdateStatus();
            // Fire-and-forget: Load projects in background without blocking constructor
            _ = LoadProjectsAsync();

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
#else
            var titleText = "Vessel Studio";
#endif
            var primaryColor = Color.FromArgb(64, 123, 255); // Blue matching About dialog

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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // Title
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Settings button
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Refresh button
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));  // Project label
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // Project dropdown
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));  // Status message (projects loaded/errors)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));  // Capture button
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // Add to queue
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));  // Batch badge
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));  // Export batch
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 85F));  // Info card
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));  // Doc link
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));  // About link
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Spacer

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Row 0: Title with icon
            var titlePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 5),
                BackColor = Color.FromArgb(248, 249, 250)
            };

            var titleIcon = new PictureBox
            {
                Image = VesselStudioIcons.GetToolbarBitmap(32),
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 32,
                Height = 32,
                Location = new Point(0, 9) // Center vertically in 50px row
            };
            titlePanel.Controls.Add(titleIcon);

            var titleLabel = new Label
            {
                Text = titleText,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = primaryColor,
                AutoSize = true,
                Location = new Point(40, 12), // 32px icon + 8px spacing, better vertical centering
                BackColor = Color.Transparent
            };
            titlePanel.Controls.Add(titleLabel);
            titleLabel.BringToFront();

            mainLayout.Controls.Add(titlePanel, 0, 0);

            // Row 1: Settings button (aligned right)
            var settingsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 5)
            };

            _settingsButton = new Button
            {
                Text = "⚙ Settings",
                Width = 100,
                Height = 32,
                FlatStyle = FlatStyle.Standard,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _settingsButton.Location = new Point(settingsPanel.Width - _settingsButton.Width, 0);
            _settingsButton.Click += OnSettingsClick;
            settingsPanel.Resize += (s, e) => {
                _settingsButton.Location = new Point(settingsPanel.Width - _settingsButton.Width, 0);
            };
            var settingsTooltip = new ToolTip();
            settingsTooltip.SetToolTip(_settingsButton, "API Key & Image Format Settings");
            settingsPanel.Controls.Add(_settingsButton);
            mainLayout.Controls.Add(settingsPanel, 0, 1);

            // Row 2: Refresh button (aligned right)
            var refreshPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 10)
            };

            _refreshProjectsButton = new Button
            {
                Text = "🔄 Refresh Projects",
                Width = 150,
                Height = 32,
                FlatStyle = FlatStyle.Standard,
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _refreshProjectsButton.Location = new Point(refreshPanel.Width - _refreshProjectsButton.Width, 0);
            _refreshProjectsButton.Click += OnRefreshProjectsClick;
            refreshPanel.Resize += (s, e) => {
                _refreshProjectsButton.Location = new Point(refreshPanel.Width - _refreshProjectsButton.Width, 0);
            };
            var refreshTooltip = new ToolTip();
            refreshTooltip.SetToolTip(_refreshProjectsButton, "Reload project list from server");
            refreshPanel.Controls.Add(_refreshProjectsButton);
            mainLayout.Controls.Add(refreshPanel, 0, 2);

            // Row 3: Project section header
            _projectLabel = new Label
            {
                Text = "Select Project:",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Margin = new Padding(0, 10, 0, 5),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.FromArgb(248, 249, 250) // Solid background matching parent
            };
            mainLayout.Controls.Add(_projectLabel, 0, 3);
            _projectLabel.BringToFront();

            // Row 4: Project dropdown (full width)
            _projectComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,
                Font = new Font("Segoe UI", 9.5f),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 0, 5),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _projectComboBox.DisplayMember = "Name";
            _projectComboBox.ValueMember = "Id";
            _projectComboBox.SelectedIndexChanged += OnProjectChanged;
            mainLayout.Controls.Add(_projectComboBox, 0, 4);

            // Row 5: Status label (projects loaded, errors, etc.)
            // AutoSize = true allows label to grow vertically when text wraps
            _statusLabel = new Label
            {
                Text = "Not configured",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 100, 100),
                Margin = new Padding(0, 0, 0, 10),
                AutoSize = true, // Allow vertical growth for text wrapping
                MaximumSize = new Size(0, 0), // Will be set dynamically on resize
                TextAlign = ContentAlignment.TopLeft,
                BackColor = Color.FromArgb(248, 249, 250), // Solid background matching parent
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            mainLayout.Controls.Add(_statusLabel, 0, 5);
            _statusLabel.BringToFront();

            // Row 6: Capture button
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
            mainLayout.Controls.Add(_captureButton, 0, 6);

            // Row 7: Add to Queue button
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
            mainLayout.Controls.Add(_addToQueueButton, 0, 7);

            // Row 8: Batch badge
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
            mainLayout.Controls.Add(_batchBadgeLabel, 0, 8);

            // Row 9: Quick Export Batch button
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
            mainLayout.Controls.Add(_quickExportBatchButton, 0, 9);

            // Row 10: Info card
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
            mainLayout.Controls.Add(infoCard, 0, 10);

            // Row 11: Documentation link
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
            mainLayout.Controls.Add(docLink, 0, 11);

            // Row 12: About link
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
            mainLayout.Controls.Add(aboutLink, 0, 12);
            
            // Handle resize to update status label MaximumSize for text wrapping
            this.Resize += (s, e) => UpdateStatusLabelWidth();
        }
        
        private void UpdateStatusLabelWidth()
        {
            if (_statusLabel != null && this.Width > 0)
            {
                // Set MaximumSize width to panel width minus padding (30px total)
                // Height = 0 means unlimited vertical growth (allows text wrapping)
                _statusLabel.MaximumSize = new Size(this.Width - 30, 0);
                
                // Force label to recalculate its size
                _statusLabel.AutoSize = false;
                _statusLabel.AutoSize = true;
                
                // Force parent layout refresh
                this.PerformLayout();
            }
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
                    // Reload projects after settings change (fire-and-forget)
                    _ = LoadProjectsAsync();
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

        private async void OnRefreshProjectsClick(object sender, EventArgs e)
        {
            // Show loading state immediately
            _projectLabel.Text = "⏳ Loading...";
            _projectLabel.ForeColor = Color.Gray;
            _refreshProjectsButton.Enabled = false;
            
            // Run async to prevent UI freeze
            await LoadProjectsAsync();
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

        private async Task LoadProjectsAsync()
        {
            var settings = VesselStudioSettings.Load();
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => {
                        _projectComboBox.Enabled = false;
                        _projectComboBox.DataSource = null;
                        _refreshProjectsButton.Enabled = false;
                    }));
                }
                else
                {
                    _projectComboBox.Enabled = false;
                    _projectComboBox.DataSource = null;
                    _refreshProjectsButton.Enabled = false;
                }
                return;
            }

            try
            {
                // UI state already set by caller (OnRefreshProjectsClick)
                // Don't set it again here to avoid flicker
                
                var apiClient = new VesselStudioApiClient();
                apiClient.SetApiKey(settings.ApiKey);
                
                // First validate the API key and subscription
                var validation = await apiClient.ValidateApiKeyAsync();
                
                if (!validation.Success)
                {
                    // API key is invalid or authentication failed - delete it
                    // This includes: authentication errors, network failures, timeouts, etc.
                    settings.ApiKey = null;
                    settings.LastProjectId = null;
                    settings.LastProjectName = null;
                    settings.HasValidSubscription = false;
                    settings.SubscriptionErrorMessage = null;
                    settings.Save();
                    
                    RhinoApp.WriteLine($"❌ {validation.ErrorMessage}");
                    
                    // Use UpdateStatus to show appropriate error
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
                
                // Check subscription validity
                if (!validation.HasValidSubscription)
                {
                    // API key is valid, but subscription tier is insufficient
                    // Keep the API key in memory - only store that subscription is invalid
                    settings.HasValidSubscription = false;
                    settings.LastProjectId = null;
                    settings.LastProjectName = null;
                    
                    // Store the error message and upgrade URL for display
                    settings.SubscriptionErrorMessage = validation.ErrorDetails 
                        ?? "Your plan does not include Rhino plugin access.";
                    settings.UpgradeUrl = validation.SubscriptionError?.UpgradeUrl ?? "https://vesselstudio.io/settings?tab=billing";
                    
                    // Store trial info if available
                    if (validation.HasTrialActive)
                    {
                        settings.HasTrialActive = true;
                        settings.TrialTier = validation.TrialTier;
                        settings.TrialExpiresAt = validation.TrialExpiresAt;
                    }
                    
                    settings.Save();
                    
                    RhinoApp.WriteLine($"❌ {validation.ErrorMessage}");
                    RhinoApp.WriteLine($"   {validation.ErrorDetails}");
                    
                    // Use UpdateStatus to show subscription error
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
                
                // Update subscription status
                settings.HasValidSubscription = true;
                
                // Store trial info if available (API v1.1)
                if (validation.HasTrialActive)
                {
                    settings.HasTrialActive = true;
                    settings.TrialTier = validation.TrialTier;
                    settings.TrialExpiresAt = validation.TrialExpiresAt;
                    settings.LastSubscriptionCheck = DateTime.Now;
                    settings.Save();
                    
                    // Show warning dialog if trial expiring within 3 days (only once per day)
                    var daysRemaining = GetDaysUntilExpiration(validation.TrialExpiresAt);
                    var timeSinceLastWarning = DateTime.Now - settings.LastTrialWarningShown;
                    
                    // Show if: expiring soon AND (never shown before OR > 24h since last shown)
                    if (daysRemaining <= 3 && (settings.LastTrialWarningShown == DateTime.MinValue || timeSinceLastWarning.TotalHours >= 24))
                    {
                        if (InvokeRequired)
                        {
                            Invoke(new Action(() => {
                                ShowTrialExpiringWarning(daysRemaining, settings.UpgradeUrl);
                                settings.LastTrialWarningShown = DateTime.Now;
                                settings.Save();
                            }));
                        }
                        else
                        {
                            ShowTrialExpiringWarning(daysRemaining, settings.UpgradeUrl);
                            settings.LastTrialWarningShown = DateTime.Now;
                            settings.Save();
                        }
                    }
                }
                else
                {
                    // Not on trial, clear trial fields
                    settings.HasTrialActive = false;
                    settings.TrialTier = null;
                    settings.TrialExpiresAt = null;
                }
                
                settings.Save();
                
                _projects = await apiClient.GetProjectsAsync();

                // If we got 0 projects but API key is valid
                if (_projects == null || _projects.Count == 0)
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => {
                            _projectComboBox.DataSource = null;
                            _projectComboBox.Enabled = false;
                            _statusLabel.Text = "⚠ No projects found";
                            _statusLabel.ForeColor = Color.FromArgb(255, 140, 0);
                        }));
                    }
                    else
                    {
                        _projectComboBox.DataSource = null;
                        _projectComboBox.Enabled = false;
                        _statusLabel.Text = "⚠ No projects found";
                        _statusLabel.ForeColor = Color.FromArgb(255, 140, 0);
                    }
                    return;
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
                // Check if it's a true authentication error (401 Unauthorized)
                // Don't delete key for 403 Forbidden (subscription tier) errors
                if (ex.Message.Contains("Invalid or expired API key") || ex.Message.Contains("401 Unauthorized"))
                {
                    settings.ApiKey = null;
                    settings.LastProjectId = null;
                    settings.LastProjectName = null;
                    settings.HasValidSubscription = false;
                    settings.Save();
                    
                    _projectComboBox.DataSource = null;
                    _projectComboBox.Enabled = false;
                    UpdateStatus();
                }
                else if (ex.Message.Contains("403 Forbidden"))
                {
                    // 403 means subscription tier is insufficient, NOT that key is invalid
                    settings.HasValidSubscription = false;
                    settings.SubscriptionErrorMessage = "Your plan does not include Rhino plugin access.";
                    settings.Save();
                    
                    _projectComboBox.DataSource = null;
                    _projectComboBox.Enabled = false;
                    UpdateStatus();
                }
                else
                {
                    _statusLabel.Text = $"⚠ Error loading projects: {ex.Message}";
                    _statusLabel.ForeColor = Color.Red;
                }
                
                RhinoApp.WriteLine($"Error loading projects: {ex.Message}");
            }
            finally
            {
                _refreshProjectsButton.Enabled = true;
                _projectLabel.Text = "Select Project:";
                _projectLabel.ForeColor = Color.FromArgb(60, 60, 60);
            }
        }

        private void PopulateProjects()
        {
            if (_projects == null || _projects.Count == 0)
            {
                _statusLabel.Text = "⚠ No projects found";
                _statusLabel.ForeColor = Color.FromArgb(255, 140, 0);
                _projectComboBox.Enabled = false;
                _projectComboBox.DataSource = null;
                return;
            }
            
            // Temporarily disable event handler to avoid triggering during population
            _projectComboBox.SelectedIndexChanged -= OnProjectChanged;
            
            // Clear everything
            _projectComboBox.DataSource = null;
            _projectComboBox.DisplayMember = "";
            _projectComboBox.ValueMember = "";
            _projectComboBox.Items.Clear();
            
            // Add items manually instead of using data binding
            foreach (var project in _projects)
            {
                _projectComboBox.Items.Add(project);
            }
            
            // NOW set display/value members AFTER items are added
            _projectComboBox.DisplayMember = "Name";
            _projectComboBox.ValueMember = "Id";
            _projectComboBox.Enabled = true;
            
            // Pre-select last used project
            var settings = VesselStudioSettings.Load();
            if (!string.IsNullOrEmpty(settings.LastProjectId))
            {
                var lastProject = _projects.FirstOrDefault(p => p.Id == settings.LastProjectId);
                if (lastProject != null)
                {
                    _projectComboBox.SelectedItem = lastProject;
                }
            }

            _statusLabel.Text = $"✓ {_projects.Count} project(s) loaded";
            _statusLabel.ForeColor = Color.FromArgb(76, 175, 80);
            
            // Re-enable event handler
            _projectComboBox.SelectedIndexChanged += OnProjectChanged;
        }

        private void UpdateStatus()
        {
            try
            {
                var settings = VesselStudioSettings.Load();
                
                if (string.IsNullOrEmpty(settings?.ApiKey))
                {
                    _statusLabel.Text = "⚠ API key not configured";
                    _statusLabel.ForeColor = Color.FromArgb(200, 50, 50);
                    _captureButton.Enabled = false;
                    _projectComboBox.Enabled = false;
                    _refreshProjectsButton.Enabled = false;
                }
                else if (!settings.HasValidSubscription)
                {
                    // Simple, short error message
                    _statusLabel.Text = "❌ Upgrade your subscription";
                    _statusLabel.ForeColor = Color.FromArgb(200, 50, 50);
                    _captureButton.Enabled = false;
                    _projectComboBox.Enabled = false;
                    // Keep refresh button enabled so user can retry after upgrading
                    _refreshProjectsButton.Enabled = true;
                }
                else if (!string.IsNullOrEmpty(settings.LastProjectName))
                {
                    // Check if user is on trial (could enhance this by calling ValidateApiKeyAsync)
                    _statusLabel.Text = $"✓ Ready - {settings.LastProjectName}";
                    _statusLabel.ForeColor = Color.FromArgb(76, 175, 80);
                    _captureButton.Enabled = true;
                    _refreshProjectsButton.Enabled = true;
                }
                else
                {
                    _statusLabel.Text = "⚠ Select a project to continue";
                    _statusLabel.ForeColor = Color.FromArgb(255, 140, 0);
                    _captureButton.Enabled = false;
                    _refreshProjectsButton.Enabled = true;
                }
                
                // Update label width for text wrapping
                UpdateStatusLabelWidth();
            }
            catch
            {
                _statusLabel.Text = "⚠ Error reading status";
                _statusLabel.ForeColor = Color.FromArgb(200, 50, 50);
            }
        }

        /// <summary>
        /// Calculate days until trial expires
        /// </summary>
        private int GetDaysUntilExpiration(string trialExpiresAt)
        {
            if (string.IsNullOrEmpty(trialExpiresAt)) return 0;
            
            try
            {
                var expiresAt = DateTime.Parse(trialExpiresAt);
                var daysRemaining = (expiresAt - DateTime.UtcNow).Days;
                return Math.Max(0, daysRemaining);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Show warning dialog when trial is expiring soon
        /// </summary>
        private void ShowTrialExpiringWarning(int daysRemaining, string upgradeUrl)
        {
            try
            {
                using (var dialog = new Form())
                {
                    dialog.Text = "Trial Expiring Soon";
                    dialog.Size = new Size(500, 320);
                    dialog.StartPosition = FormStartPosition.CenterScreen;
                    dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dialog.MaximizeBox = false;
                    dialog.MinimizeBox = false;
                    dialog.BackColor = Color.White;
                    dialog.Padding = new Padding(0);
                    
                    // Header panel with warning icon and title
                    var headerPanel = new Panel
                    {
                        BackColor = Color.FromArgb(255, 245, 230), // Light orange background
                        Dock = DockStyle.Top,
                        Height = 100,
                        Padding = new Padding(20, 15, 20, 15)
                    };
                    dialog.Controls.Add(headerPanel);
                    
                    var warningIcon = new Label
                    {
                        Text = "⚠️",
                        Font = new Font("Segoe UI", 48),
                        Location = new Point(20, 12),
                        Size = new Size(70, 70),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = false
                    };
                    headerPanel.Controls.Add(warningIcon);
                    
                    var titleLabel = new Label
                    {
                        Text = "Your Trial is Expiring Soon",
                        Font = new Font("Segoe UI", 13, FontStyle.Bold),
                        Location = new Point(100, 15),
                        Size = new Size(380, 30),
                        TextAlign = ContentAlignment.MiddleLeft,
                        AutoSize = false
                    };
                    headerPanel.Controls.Add(titleLabel);
                    
                    // Content panel with message
                    var contentPanel = new Panel
                    {
                        BackColor = Color.White,
                        Dock = DockStyle.Fill,
                        Padding = new Padding(25, 20, 25, 80)
                    };
                    dialog.Controls.Add(contentPanel);
                    
                    var messageLabel = new Label
                    {
                        Text = $"You have {daysRemaining} day{(daysRemaining != 1 ? "s" : "")} remaining on your Rhino plugin trial.\n\n" +
                               "Upgrade now to continue using this plugin and avoid losing your work.",
                        Font = new Font("Segoe UI", 10),
                        Location = new Point(25, 20),
                        Size = new Size(450, 100),
                        TextAlign = ContentAlignment.TopLeft,
                        AutoSize = false
                    };
                    messageLabel.AutoSize = true;
                    messageLabel.MaximumSize = new Size(450, 0);
                    contentPanel.Controls.Add(messageLabel);
                    
                    // Button panel at bottom
                    var buttonPanel = new Panel
                    {
                        BackColor = Color.FromArgb(248, 249, 250),
                        Dock = DockStyle.Bottom,
                        Height = 70,
                        Padding = new Padding(20, 15, 20, 15)
                    };
                    dialog.Controls.Add(buttonPanel);
                    
                    var upgradeButton = new Button
                    {
                        Text = "Upgrade Now",
                        Font = new Font("Segoe UI", 10, FontStyle.Bold),
                        Location = new Point(270, 15),
                        Size = new Size(145, 40),
                        BackColor = Color.FromArgb(64, 123, 255),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Cursor = Cursors.Hand,
                        DialogResult = DialogResult.OK
                    };
                    upgradeButton.Click += (s, e) => {
                        try
                        {
                            System.Diagnostics.Process.Start(upgradeUrl ?? "https://vesselstudio.io/settings?tab=billing");
                        }
                        catch { }
                    };
                    upgradeButton.FlatAppearance.BorderSize = 0;
                    buttonPanel.Controls.Add(upgradeButton);
                    
                    var dismissButton = new Button
                    {
                        Text = "Dismiss",
                        Font = new Font("Segoe UI", 10),
                        Location = new Point(115, 15),
                        Size = new Size(145, 40),
                        FlatStyle = FlatStyle.Standard,
                        DialogResult = DialogResult.Cancel
                    };
                    buttonPanel.Controls.Add(dismissButton);
                    
                    dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error showing trial warning: {ex.Message}");
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

