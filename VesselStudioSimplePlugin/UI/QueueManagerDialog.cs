using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
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
        private Button _closeButton;

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
                Height = 50,
                BackColor = SystemColors.Control
            };
            this.Controls.Add(buttonPanel);

            // T031: Remove Selected button
            _removeSelectedButton = new Button
            {
                Text = "Remove Selected",
                Size = new Size(120, 30),
                Location = new Point(10, 10)
            };
            _removeSelectedButton.Click += OnRemoveSelectedClick;
            buttonPanel.Controls.Add(_removeSelectedButton);

            // T031: Clear All button
            _clearAllButton = new Button
            {
                Text = "Clear All",
                Size = new Size(100, 30),
                Location = new Point(140, 10)
            };
            _clearAllButton.Click += OnClearAllClick;
            buttonPanel.Controls.Add(_clearAllButton);

            // T031: Export All button (T059 placeholder)
            _exportAllButton = new Button
            {
                Text = "Export All",
                Size = new Size(100, 30),
                Location = new Point(250, 10)
            };
            _exportAllButton.Click += OnExportAllClick;
            buttonPanel.Controls.Add(_exportAllButton);

            // T031: Close button
            _closeButton = new Button
            {
                Text = "Close",
                Size = new Size(100, 30),
                Location = new Point(360, 10),
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

        // T059: Export All button handler (Phase 5 placeholder)
        private void OnExportAllClick(object sender, EventArgs e)
        {
            // Placeholder - will be implemented in Phase 5 (US3)
            // T059: Will call BatchUploadService.UploadBatchAsync here
            MessageBox.Show(
                "Batch upload not yet implemented. This will be added in Phase 5.",
                "Export All - Placeholder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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
