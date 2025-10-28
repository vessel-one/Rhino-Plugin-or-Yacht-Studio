using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rhino;
using VesselStudioSimplePlugin.Models;
using VesselStudioSimplePlugin.Services;

namespace VesselStudioSimplePlugin.UI
{
    /// <summary>
    /// Queue Manager Dialog for reviewing and managing queued batch captures.
    /// 
    /// Tasks T029-T042: Complete UI for managing queued items including:
    /// - T029: Form creation with 600x500 fixed size
    /// - T030: ListView with thumbnails, viewport names, timestamps
    /// - T031: Action buttons (Remove Selected, Clear All, Export All, Close)
    /// - T032-T040: Populate ListView with queue items and thumbnails
    /// - T034-T036: Event handlers for button clicks
    /// - T041-T042: Performance optimization (dialog opens <500ms)
    /// </summary>
    public class QueueManagerDialog : Form
    {
        private ListView _queueListView;
        private ImageList _thumbnailImageList;
        private Button _removeSelectedButton;
        private Button _clearAllButton;
        private Button _exportAllButton;
        private Button _settingsButton;
        private Button _closeButton;
        private ProgressBar _uploadProgressBar;
        private Label _progressStatusLabel;

        public QueueManagerDialog()
        {
            InitializeComponent();
            LoadQueueItems();
        }

        // T029-T031: Initialize all form components
        private void InitializeComponent()
        {
            // T029: Form properties
            this.Text = "Batch Export Queue Manager";
            this.Size = new Size(600, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // T030: ImageList for thumbnails (120x90 size)
            _thumbnailImageList = new ImageList
            {
                ImageSize = new Size(120, 90),
                ColorDepth = ColorDepth.Depth32Bit
            };

            // T030: ListView with columns
            _queueListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                SmallImageList = _thumbnailImageList
            };
            _queueListView.Columns.Add("Thumbnail", 120);
            _queueListView.Columns.Add("Viewport Name", 200);
            _queueListView.Columns.Add("Timestamp", 150);
            this.Controls.Add(_queueListView);

            // T031: Bottom panel for buttons
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = SystemColors.Control
            };
            this.Controls.Add(buttonPanel);

            // Add progress bar (hidden by default, shown during export)
            _uploadProgressBar = new ProgressBar
            {
                Size = new Size(580, 20),
                Location = new Point(10, 10),
                Visible = false,
                Style = ProgressBarStyle.Continuous
            };
            buttonPanel.Controls.Add(_uploadProgressBar);

            // Add progress status label
            _progressStatusLabel = new Label
            {
                Text = "Uploading...",
                Size = new Size(300, 20),
                Location = new Point(10, 35),
                Visible = false,
                Font = new Font("Arial", 9, FontStyle.Regular),
                ForeColor = SystemColors.ControlText
            };
            buttonPanel.Controls.Add(_progressStatusLabel);

            // T031: Remove Selected button
            _removeSelectedButton = new Button
            {
                Text = "Remove Selected",
                Size = new Size(120, 30),
                Location = new Point(10, 40)
            };
            _removeSelectedButton.Click += OnRemoveSelectedClick;
            buttonPanel.Controls.Add(_removeSelectedButton);

            // T031: Clear All button
            _clearAllButton = new Button
            {
                Text = "Clear All",
                Size = new Size(100, 30),
                Location = new Point(140, 40)
            };
            _clearAllButton.Click += OnClearAllClick;
            buttonPanel.Controls.Add(_clearAllButton);

            // T031: Export All button (T059 placeholder)
            _exportAllButton = new Button
            {
                Text = "Export All",
                Size = new Size(100, 30),
                Location = new Point(250, 40)
            };
            _exportAllButton.Click += OnExportAllClick;
            buttonPanel.Controls.Add(_exportAllButton);

            // Settings button with tooltip for image format options
            _settingsButton = new Button
            {
                Text = "📸 Format",
                Size = new Size(80, 30),
                Location = new Point(360, 40)
            };
            _settingsButton.Click += OnSettingsClick;
            
            var tooltip = new ToolTip();
            tooltip.SetToolTip(_settingsButton,
                "Image Format Settings\n" +
                "• PNG: Lossless quality (recommended)\n" +
                "• JPEG: Configurable quality (1-100)\n" +
                "• High quality = larger file size");
            
            buttonPanel.Controls.Add(_settingsButton);

            // T031: Close button
            _closeButton = new Button
            {
                Text = "Close",
                Size = new Size(80, 30),
                Location = new Point(450, 40),
                DialogResult = DialogResult.Cancel
            };
            this.CancelButton = _closeButton;
            buttonPanel.Controls.Add(_closeButton);
        }

        // T032-T040: Load queue items into ListView
        private void LoadQueueItems()
        {
            _queueListView.Items.Clear();
            _thumbnailImageList.Images.Clear();

            // Get all items from queue service
            var items = CaptureQueueService.Current.GetItems();

            foreach (var item in items)
            {
                // Create ListView item
                var listViewItem = new ListViewItem();
                listViewItem.Tag = item;  // Store reference for button handlers

                // T038: Add thumbnail
                // Note: GetThumbnail() returns 80x60, may need scaling to 120x90
                var thumbnail = item.GetThumbnail();
                string imageKey = item.Id.ToString();
                _thumbnailImageList.Images.Add(imageKey, thumbnail);
                listViewItem.ImageKey = imageKey;

                // T039: Add viewport name
                listViewItem.SubItems.Add(item.ViewportName);

                // T040: Add timestamp
                listViewItem.SubItems.Add(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

                _queueListView.Items.Add(listViewItem);
            }
        }

        // T034: Remove Selected button handler
        private void OnRemoveSelectedClick(object sender, EventArgs e)
        {
            // Collect checked items
            var checkedItems = new List<ListViewItem>();
            foreach (ListViewItem item in _queueListView.CheckedItems)
            {
                checkedItems.Add(item);
            }

            if (checkedItems.Count == 0)
                return;

            // Show confirmation for >5 items
            if (checkedItems.Count > 5)
            {
                var result = MessageBox.Show(
                    $"Remove {checkedItems.Count} items from queue?",
                    "Confirm Remove",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    return;
            }

            // Remove each checked item
            foreach (var listItem in checkedItems)
            {
                var queueItem = (QueuedCaptureItem)listItem.Tag;
                CaptureQueueService.Current.RemoveItem(queueItem);
                _queueListView.Items.Remove(listItem);
            }
        }

        // T035: Clear All button handler
        private void OnClearAllClick(object sender, EventArgs e)
        {
            int count = CaptureQueueService.Current.ItemCount;
            if (count == 0)
                return;

            var result = MessageBox.Show(
                $"Remove all {count} items from queue?",
                "Confirm Clear All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                CaptureQueueService.Current.Clear();
                LoadQueueItems();
            }
        }

        // Image format settings button handler
        private void OnSettingsClick(object sender, EventArgs e)
        {
            // Open image format settings dialog
            using (var dialog = new VesselImageFormatDialog())
            {
                dialog.ShowDialog();
            }
        }

        // T059-T061: Export All button handler - Upload entire queue to Vessel Studio
        // Phase 5 Group 3 Implementation
        private async void OnExportAllClick(object sender, EventArgs e)
        {
            // T059: Validate prerequisites
            var settings = VesselStudioSettings.Load();
            
            if (string.IsNullOrEmpty(settings?.ApiKey))
            {
                MessageBox.Show(
                    "API key not configured. Please set it first using 'Set API Key' button.",
                    "API Key Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var queueService = CaptureQueueService.Current;
            if (queueService.IsEmpty)
            {
                MessageBox.Show(
                    "Queue is empty. Nothing to export.",
                    "Empty Queue",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(settings.LastProjectId))
            {
                MessageBox.Show(
                    "No project selected. Please select a project from the toolbar dropdown.",
                    "Project Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Disable controls during upload
            _exportAllButton.Enabled = false;
            _removeSelectedButton.Enabled = false;
            _clearAllButton.Enabled = false;
            _queueListView.Enabled = false;

            // Show progress bar and status
            _uploadProgressBar.Visible = true;
            _progressStatusLabel.Visible = true;
            _uploadProgressBar.Value = 0;
            _progressStatusLabel.Text = "Uploading... 0%";

            try
            {
                // T060: Create batch upload service and execute upload
                var apiClient = new VesselStudioApiClient();
                apiClient.SetApiKey(settings.ApiKey);
                var uploadService = new BatchUploadService(apiClient);

                var itemCount = queueService.ItemCount;
                RhinoApp.WriteLine($"📤 Uploading {itemCount} item{(itemCount == 1 ? "" : "s")} to project: {settings.LastProjectName}...");

                // Create progress reporter
                var progress = new Progress<BatchUploadProgress>(p =>
                {
                    if (p.TotalItems > 0)
                    {
                        // Update progress bar
                        _uploadProgressBar.Value = (int)p.PercentComplete;
                        _progressStatusLabel.Text = $"Uploading... {p.PercentComplete}% ({p.CompletedItems}/{p.TotalItems})";
                        Application.DoEvents(); // Refresh UI
                        RhinoApp.WriteLine($"[Export] {p.CompletedItems}/{p.TotalItems} - {p.CurrentFilename}");
                    }
                });

                // T060: Execute batch upload asynchronously
                var result = await uploadService.UploadBatchAsync(settings.LastProjectId, progress, CancellationToken.None);

                // T061: Handle results and refresh dialog
                if (result.Success)
                {
                    RhinoApp.WriteLine($"✅ Export complete! {result.UploadedCount} captures uploaded.");
                    MessageBox.Show(
                        $"✅ Batch export successful!\n\n{result.UploadedCount} capture{(result.UploadedCount == 1 ? "" : "s")} uploaded\nDuration: {result.TotalDurationMs}ms",
                        "Export Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    
                    // Close dialog on successful export
                    this.Close();
                }
                else if (result.IsPartialSuccess)
                {
                    RhinoApp.WriteLine($"⚠ Export incomplete: {result.UploadedCount} success, {result.FailedCount} failed");
                    var errorDetails = string.Empty;
                    if (result.Errors.Count > 0)
                    {
                        errorDetails = "\n\nErrors:\n";
                        for (int i = 0; i < Math.Min(3, result.Errors.Count); i++)
                        {
                            errorDetails += $"• {result.Errors[i].filename}: {result.Errors[i].error}\n";
                        }
                        if (result.Errors.Count > 3)
                            errorDetails += $"... and {result.Errors.Count - 3} more";
                    }

                    MessageBox.Show(
                        $"⚠ Partial success\n\nUploaded: {result.UploadedCount}\nFailed: {result.FailedCount}\n\nQueue preserved for retry." + errorDetails,
                        "Export Partial Failure",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    
                    // Refresh to show updated queue
                    LoadQueueItems();
                }
                else
                {
                    RhinoApp.WriteLine($"❌ Export failed: {result.FailedCount} items failed");
                    var errorDetails = string.Empty;
                    if (result.Errors.Count > 0)
                    {
                        errorDetails = "\n\nErrors:\n";
                        for (int i = 0; i < Math.Min(3, result.Errors.Count); i++)
                        {
                            errorDetails += $"• {result.Errors[i].filename}: {result.Errors[i].error}\n";
                        }
                        if (result.Errors.Count > 3)
                            errorDetails += $"... and {result.Errors.Count - 3} more";
                    }

                    MessageBox.Show(
                        $"❌ Export failed\n\nFailed: {result.FailedCount}\n\nQueue preserved for retry." + errorDetails,
                        "Export Failure",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    
                    // Refresh to show queue
                    LoadQueueItems();
                }
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"❌ Unexpected error during export: {ex.Message}");
                MessageBox.Show(
                    $"Unexpected error: {ex.Message}",
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                
                // Refresh to show queue
                LoadQueueItems();
            }
            finally
            {
                // Re-enable controls
                _exportAllButton.Enabled = true;
                _removeSelectedButton.Enabled = true;
                _clearAllButton.Enabled = true;
                _queueListView.Enabled = true;

                // Hide progress bar
                _uploadProgressBar.Visible = false;
                _progressStatusLabel.Visible = false;
            }
        }

        // T039: Proper disposal of resources
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _thumbnailImageList?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
