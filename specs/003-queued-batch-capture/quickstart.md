# Quickstart Guide: Queued Batch Capture

**Feature**: 003-queued-batch-capture  
**Date**: October 28, 2025  
**Developer Audience**: Implementation team

## Overview

This guide provides a step-by-step implementation sequence for the queued batch capture feature. Follow the order below to ensure smooth integration with minimal rework.

---

## Prerequisites

### Required Reading
- ✅ Read `spec.md` - Feature specification with user stories and requirements
- ✅ Read `research.md` - Technical decisions and rationale
- ✅ Read `data-model.md` - Entity definitions and state transitions
- ✅ Read `contracts/api-batch-upload.md` - Service interface contract

### Development Environment
- Visual Studio 2019+ or VS Code with C# extension
- Rhino 8 installed (for testing)
- .NET Framework 4.8 SDK
- RhinoCommon SDK 8.x (installed with Rhino)
- Git for version control

### Existing Knowledge
- Familiarity with RhinoCommon API (viewport capture, command structure)
- Basic Windows Forms UI development
- C# async/await patterns
- Singleton pattern for services

---

## Implementation Order

Follow this sequence to minimize dependency issues and enable incremental testing.

### Phase 1: Core Data Models (30 min)

**Goal**: Implement entities that other components depend on.

**Files to Create**:
- `VesselStudioSimplePlugin/Models/QueuedCaptureItem.cs`
- `VesselStudioSimplePlugin/Models/CaptureQueue.cs`

**Key Points**:
1. Start with `QueuedCaptureItem` first (no dependencies)
2. Implement IDisposable pattern for memory cleanup
3. Add validation in constructors (non-null checks, size limits)
4. Then implement `CaptureQueue` (depends on QueuedCaptureItem)
5. Add computed properties: Count, TotalSizeBytes, IsEmpty

**Example Start**:
```csharp
// Models/QueuedCaptureItem.cs
using System;
using System.Drawing;

namespace VesselStudioSimplePlugin.Models {
    public class QueuedCaptureItem : IDisposable {
        public Guid Id { get; }
        public byte[] ImageData { get; }
        public string ViewportName { get; }
        public DateTime Timestamp { get; }
        public int SequenceNumber { get; set; }
        private Bitmap _thumbnailCache;

        public QueuedCaptureItem(byte[] imageData, string viewportName) {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data cannot be null or empty");
            if (string.IsNullOrWhiteSpace(viewportName))
                throw new ArgumentException("Viewport name cannot be empty");
            if (imageData.Length > 5 * 1024 * 1024)
                throw new ArgumentException("Image exceeds 5MB limit");

            Id = Guid.NewGuid();
            ImageData = imageData;
            ViewportName = viewportName;
            Timestamp = DateTime.Now;
            SequenceNumber = 0; // Set by queue when added
        }

        public Bitmap GetThumbnail() {
            // Generate 80x60 thumbnail on demand (cached)
            // See research.md for implementation details
        }

        public void Dispose() {
            _thumbnailCache?.Dispose();
            _thumbnailCache = null;
        }
    }
}
```

**Testing**:
- Create item with valid data → succeeds
- Create item with null image → throws ArgumentException
- Create item with >5MB image → throws ArgumentException
- GetThumbnail() generates 80x60 bitmap
- Dispose() releases thumbnail memory

---

### Phase 2: Queue Service (45 min)

**Goal**: Implement singleton service managing the capture queue.

**Files to Create**:
- `VesselStudioSimplePlugin/Services/CaptureQueueService.cs`

**Key Points**:
1. Implement thread-safe singleton pattern (see research.md)
2. Expose Current property for global access
3. Add methods: AddItem, RemoveItem, Clear, GetItems
4. Enforce 20-item limit (throw on exceed)
5. Auto-assign sequence numbers on add
6. Fire events for UI updates: ItemAdded, ItemRemoved, QueueCleared

**Example Start**:
```csharp
// Services/CaptureQueueService.cs
using System;
using VesselStudioSimplePlugin.Models;

namespace VesselStudioSimplePlugin.Services {
    public class CaptureQueueService {
        private static readonly Lazy<CaptureQueueService> _instance = 
            new Lazy<CaptureQueueService>(() => new CaptureQueueService());
        
        public static CaptureQueueService Current => _instance.Value;
        
        private CaptureQueue _queue;
        private readonly object _lock = new object();
        
        public event EventHandler<QueuedCaptureItem> ItemAdded;
        public event EventHandler<Guid> ItemRemoved;
        public event EventHandler QueueCleared;
        
        private CaptureQueueService() {
            _queue = new CaptureQueue();
        }
        
        public void AddItem(QueuedCaptureItem item) {
            lock (_lock) {
                if (_queue.Count >= 20)
                    throw new InvalidOperationException("Queue limit reached (20 items)");
                
                item.SequenceNumber = _queue.Count + 1;
                _queue.Items.Add(item);
                ItemAdded?.Invoke(this, item);
            }
        }
        
        // RemoveItem, Clear, GetItems methods...
    }
}
```

**Testing**:
- AddItem adds to queue with correct sequence
- AddItem beyond 20 items throws InvalidOperationException
- RemoveItem removes by ID
- Clear empties queue and disposes items
- Events fire correctly

---

### Phase 3: Batch Upload Service (1 hour)

**Goal**: Implement batch upload logic using existing API client.

**Files to Create**:
- `VesselStudioSimplePlugin/Services/BatchUploadService.cs`
- `VesselStudioSimplePlugin/Models/BatchUploadProgress.cs`
- `VesselStudioSimplePlugin/Models/BatchUploadResult.cs`

**Key Points**:
1. Accept VesselStudioApiClient in constructor (dependency injection)
2. Implement UploadBatchAsync per contract (see contracts/api-batch-upload.md)
3. Generate filenames: `{ProjectName}_{ViewportName}_{Sequence:D3}.png`
4. Upload sequentially (not parallel)
5. Report progress after each item
6. Collect errors for failed items
7. Clear queue only on complete success

**Example Start**:
```csharp
// Services/BatchUploadService.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using VesselStudioSimplePlugin.Models;

namespace VesselStudioSimplePlugin.Services {
    public class BatchUploadService {
        private readonly VesselStudioApiClient _apiClient;
        
        public BatchUploadService(VesselStudioApiClient apiClient) {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }
        
        public async Task<BatchUploadResult> UploadBatchAsync(
            CaptureQueue queue,
            string projectId,
            IProgress<BatchUploadProgress> progress,
            CancellationToken cancellationToken
        ) {
            // Validate inputs
            if (queue == null || queue.IsEmpty)
                throw new ArgumentException("Queue is empty");
            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("Project ID required");
            if (!_apiClient.HasValidApiKey())
                throw new InvalidOperationException("API key not configured");
            
            // Upload loop
            // See contracts/api-batch-upload.md for full implementation
        }
        
        private string GenerateFilename(string projectName, string viewportName, int sequence) {
            // Sanitize and format per contract
        }
    }
}
```

**Testing**:
- UploadBatchAsync with empty queue throws
- UploadBatchAsync with null projectId throws
- UploadBatchAsync reports progress correctly
- Queue cleared on complete success
- Queue preserved on failure
- Partial success returns correct counts

---

### Phase 4: Commands (1 hour)

**Goal**: Implement Rhino commands for queue operations.

**Files to Create**:
- `VesselStudioSimplePlugin/Commands/VesselAddToQueueCommand.cs`
- `VesselStudioSimplePlugin/Commands/VesselSendBatchCommand.cs`
- `VesselStudioSimplePlugin/Commands/VesselClearQueueCommand.cs`

**Key Points**:
1. Follow existing command pattern (see VesselCaptureCommand.cs)
2. VesselAddToQueueCommand: Capture viewport → compress JPEG → add to queue
3. VesselSendBatchCommand: Validate API key → show progress dialog → call BatchUploadService
4. VesselClearQueueCommand: Confirm with user → clear queue
5. All commands update UI via service events

**Example Start**:
```csharp
// Commands/VesselAddToQueueCommand.cs
using Rhino;
using Rhino.Commands;
using VesselStudioSimplePlugin.Services;
using VesselStudioSimplePlugin.Models;

namespace VesselStudioSimplePlugin.Commands {
    public class VesselAddToQueueCommand : Command {
        public override string EnglishName => "VesselAddToQueue";
        
        protected override Result RunCommand(RhinoDoc doc, RunMode mode) {
            try {
                // Get active viewport
                var view = doc.Views.ActiveView;
                if (view == null) {
                    RhinoApp.WriteLine("No active viewport");
                    return Result.Failure;
                }
                
                // Capture image
                var bitmap = view.CaptureToBitmap();
                if (bitmap == null) {
                    RhinoApp.WriteLine("Failed to capture viewport");
                    return Result.Failure;
                }
                
                // Compress to JPEG bytes
                byte[] imageData = CompressToJpeg(bitmap, 85);
                bitmap.Dispose();
                
                // Add to queue
                var item = new QueuedCaptureItem(imageData, view.ActiveViewport.Name);
                CaptureQueueService.Current.AddItem(item);
                
                RhinoApp.WriteLine($"Added to queue ({CaptureQueueService.Current.Count} items)");
                return Result.Success;
            }
            catch (Exception ex) {
                RhinoApp.WriteLine($"Error: {ex.Message}");
                return Result.Failure;
            }
        }
        
        private byte[] CompressToJpeg(Bitmap bitmap, int quality) {
            // See research.md for JPEG compression implementation
        }
    }
}
```

**Testing**:
- VesselAddToQueueCommand captures and adds to queue
- VesselSendBatchCommand uploads all items
- VesselClearQueueCommand empties queue with confirmation
- Commands update UI correctly

---

### Phase 5: UI Components (2 hours)

**Goal**: Build compact toolbar indicator and popup queue manager dialog.

**Files to Create**:
- `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs` - Popup dialog for queue management
- `VesselStudioSimplePlugin/UI/Components/QueueItemControl.cs` - Individual item in dialog list

**Files to Modify**:
- `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs` - Add batch badge and quick export button

**Key Points**:
1. **Toolbar**: Add "Batch (N)" badge button + "Quick Export Batch" button (compact, minimal space)
2. **Popup Dialog**: QueueManagerDialog opens when badge clicked, shows large thumbnails (120x90px), checkboxes, action buttons
3. **QueueItemControl**: List item in dialog with checkbox + large thumbnail + viewport name + remove button
4. Subscribe to CaptureQueueService events for real-time updates
5. Modal dialog prevents queue modifications during review (thread-safe)

**Example Start - Toolbar Integration**:
```csharp
// VesselStudioToolbarPanel.cs (modify existing)
private Button _batchBadgeButton;
private Button _quickExportButton;

private void InitializeBatchControls() {
    // Compact badge button showing count
    _batchBadgeButton = new Button {
        Text = "Batch (0)",
        Width = 80,
        Location = new Point(200, 10)  // Adjust based on existing layout
    };
    _batchBadgeButton.Click += OpenQueueManagerDialog;
    Controls.Add(_batchBadgeButton);
    
    // Quick export button (enabled when count > 0)
    _quickExportButton = new Button {
        Text = "Quick Export Batch",
        Width = 140,
        Location = new Point(10, 50),  // Below other controls
        Enabled = false
    };
    _quickExportButton.Click += QuickExportBatch;
    Controls.Add(_quickExportButton);
    
    // Subscribe to queue updates for real-time badge updates
    CaptureQueueService.Current.ItemAdded += OnQueueChanged;
    CaptureQueueService.Current.ItemRemoved += OnQueueChanged;
    CaptureQueueService.Current.QueueCleared += OnQueueChanged;
}

private void OnQueueChanged(object sender, EventArgs e) {
    if (InvokeRequired) {
        Invoke(new Action(() => OnQueueChanged(sender, e)));
        return;
    }
    
    var count = CaptureQueueService.Current.Count;
    _batchBadgeButton.Text = $"Batch ({count})";
    _quickExportButton.Enabled = count > 0;
}

private void OpenQueueManagerDialog(object sender, EventArgs e) {
    using (var dialog = new QueueManagerDialog()) {
        dialog.ShowDialog(this);  // Modal popup
    }
}

private async void QuickExportBatch(object sender, EventArgs e) {
    // Export without opening dialog (quick workflow)
    try {
        var service = new BatchUploadService(VesselStudioApiClient.Current);
        var queue = CaptureQueueService.Current.GetQueue();
        var projectId = GetSelectedProjectId();  // From existing dropdown
        
        var result = await service.UploadBatchAsync(queue, projectId, null, CancellationToken.None);
        
        if (result.Success) {
            MessageBox.Show($"Successfully uploaded {result.UploadedCount} captures!", "Success");
        } else {
            MessageBox.Show($"Upload failed. {result.FailedCount} errors.", "Error");
        }
    } catch (Exception ex) {
        MessageBox.Show($"Error: {ex.Message}", "Error");
    }
}
```

**Example Start - Popup Dialog**:
```csharp
// UI/QueueManagerDialog.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using VesselStudioSimplePlugin.Services;
using VesselStudioSimplePlugin.Models;

namespace VesselStudioSimplePlugin.UI {
    public class QueueManagerDialog : Form {
        private ListView _queueListView;
        private ImageList _thumbnailImageList;
        private Button _selectAllButton;
        private Button _selectNoneButton;
        private Button _removeSelectedButton;
        private Button _clearAllButton;
        private Button _exportAllButton;
        
        public QueueManagerDialog() {
            Text = "Queue Manager";
            Size = new Size(600, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            
            InitializeComponents();
            LoadQueueItems();
        }
        
        private void InitializeComponents() {
            // Thumbnail image list (120x90px per FR-019)
            _thumbnailImageList = new ImageList {
                ImageSize = new Size(120, 90),
                ColorDepth = ColorDepth.Depth24Bit
            };
            
            // ListView with checkboxes and large icons
            _queueListView = new ListView {
                Location = new Point(10, 10),
                Size = new Size(570, 380),
                View = View.LargeIcon,  // Shows thumbnails nicely
                CheckBoxes = true,  // FR-018 multi-select support
                LargeImageList = _thumbnailImageList,
                MultiSelect = true
            };
            _queueListView.ItemChecked += OnItemCheckedChanged;
            Controls.Add(_queueListView);
            
            // Action buttons at bottom
            _selectAllButton = new Button {
                Text = "Select All",
                Location = new Point(10, 400),
                Width = 90
            };
            _selectAllButton.Click += SelectAllItems;
            Controls.Add(_selectAllButton);
            
            _selectNoneButton = new Button {
                Text = "Select None",
                Location = new Point(105, 400),
                Width = 90
            };
            _selectNoneButton.Click += SelectNoneItems;
            Controls.Add(_selectNoneButton);
            
            _removeSelectedButton = new Button {
                Text = "Remove Selected",
                Location = new Point(210, 400),
                Width = 120,
                Enabled = false
            };
            _removeSelectedButton.Click += RemoveSelectedItems;
            Controls.Add(_removeSelectedButton);
            
            _clearAllButton = new Button {
                Text = "Clear All",
                Location = new Point(335, 400),
                Width = 90
            };
            _clearAllButton.Click += ClearAllItems;
            Controls.Add(_clearAllButton);
            
            _exportAllButton = new Button {
                Text = "Export All",
                Location = new Point(430, 400),
                Width = 90,
                Font = new Font(Font, FontStyle.Bold)  // Primary action
            };
            _exportAllButton.Click += ExportAllItems;
            Controls.Add(_exportAllButton);
        }
        
        private void LoadQueueItems() {
            _queueListView.Items.Clear();
            _thumbnailImageList.Images.Clear();
            
            var items = CaptureQueueService.Current.GetItems();
            foreach (var item in items) {
                // Generate 120x90 thumbnail (FR-019)
                var thumbnail = item.GetThumbnail();
                var scaledThumb = ScaleThumbnail(thumbnail, 120, 90);
                _thumbnailImageList.Images.Add(item.Id.ToString(), scaledThumb);
                
                // Create list item
                var listItem = new ListViewItem {
                    Text = $"{item.ViewportName} ({item.SequenceNumber:D3})",
                    ImageKey = item.Id.ToString(),
                    Tag = item
                };
                _queueListView.Items.Add(listItem);
            }
        }
        
        private Bitmap ScaleThumbnail(Bitmap source, int width, int height) {
            var scaled = new Bitmap(width, height);
            using (var g = Graphics.FromImage(scaled)) {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(source, 0, 0, width, height);
            }
            return scaled;
        }
        
        private void OnItemCheckedChanged(object sender, ItemCheckedEventArgs e) {
            // Enable "Remove Selected" only when items are checked
            var hasChecked = _queueListView.CheckedItems.Count > 0;
            _removeSelectedButton.Enabled = hasChecked;
        }
        
        private void SelectAllItems(object sender, EventArgs e) {
            foreach (ListViewItem item in _queueListView.Items) {
                item.Checked = true;
            }
        }
        
        private void SelectNoneItems(object sender, EventArgs e) {
            foreach (ListViewItem item in _queueListView.Items) {
                item.Checked = false;
            }
        }
        
        private void RemoveSelectedItems(object sender, EventArgs e) {
            var checkedItems = new List<ListViewItem>();
            foreach (ListViewItem item in _queueListView.CheckedItems) {
                checkedItems.Add(item);
            }
            
            if (checkedItems.Count == 0) return;
            
            var confirm = MessageBox.Show(
                $"Remove {checkedItems.Count} item(s) from queue?",
                "Confirm Remove",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            
            if (confirm == DialogResult.Yes) {
                foreach (var item in checkedItems) {
                    var queueItem = item.Tag as QueuedCaptureItem;
                    CaptureQueueService.Current.RemoveItem(queueItem.Id);
                }
                LoadQueueItems();  // Refresh
            }
        }
        
        private void ClearAllItems(object sender, EventArgs e) {
            var confirm = MessageBox.Show(
                "Clear entire queue? This cannot be undone.",
                "Confirm Clear",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );
            
            if (confirm == DialogResult.Yes) {
                CaptureQueueService.Current.Clear();
                Close();  // Close dialog after clear
            }
        }
        
        private async void ExportAllItems(object sender, EventArgs e) {
            // Export all items to Vessel Studio
            try {
                var service = new BatchUploadService(VesselStudioApiClient.Current);
                var queue = CaptureQueueService.Current.GetQueue();
                var projectId = GetSelectedProjectId();  // Need to pass from toolbar
                
                var result = await service.UploadBatchAsync(queue, projectId, null, CancellationToken.None);
                
                if (result.Success) {
                    MessageBox.Show($"Successfully uploaded {result.UploadedCount} captures!", "Success");
                    Close();
                } else {
                    MessageBox.Show($"Upload completed with {result.FailedCount} errors.", "Partial Success");
                }
            } catch (Exception ex) {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }
        
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _thumbnailImageList?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
```

**Testing**:
- Batch badge updates in real-time when items added/removed
- Quick Export button enabled only when queue has items
- Popup dialog opens within 500ms (SC-008)
- Large thumbnails (120x90px) display clearly (FR-019)
- Checkboxes allow multi-select for batch operations (FR-018)
- Remove Selected removes only checked items
- Clear All clears entire queue with confirmation
- Export All uploads all items and closes dialog on success

---

### Phase 6: Integration Testing (1 hour)

**Goal**: End-to-end testing of complete workflow.

**Test Scenarios**:
- Remove button deletes item from queue
- UI updates in real-time when items added/removed
- Panel collapses when queue empty
- Scrollable when >20 items (edge case testing)

---

### Phase 6: Integration Testing (1 hour)

**Goal**: End-to-end testing of complete workflow.

**Test Scenarios**:

1. **Basic Queue Workflow** (US-001, US-002, US-003)
   - Add 5 captures from different viewports → toolbar badge shows "Batch (5)"
   - Click badge → popup dialog opens showing 5 items with large thumbnails
   - Select 2 items via checkboxes, click Remove Selected → dialog shows 3 items
   - Click Export All → uploads successfully, dialog closes, toolbar badge resets to "Batch (0)"

2. **Edge Case: Queue Limit** (FR-003)
   - Add 20 captures → succeeds, badge shows "Batch (20)"
   - Add 21st capture → error message shown, queue remains at 20

3. **Edge Case: API Failure** (FR-008)
   - Disconnect network
   - Click Quick Export Batch → upload fails
   - Queue preserved, badge still shows count, user can retry

4. **Edge Case: Partial Success** (FR-008)
   - Mock API to fail on 3rd item
   - Send batch → 2 succeed, 1 fails
   - Queue preserved, error dialog shows details, user can remove failed item and retry

5. **Performance Test** (SC-001, SC-008)
   - Add 10 captures → total time <3 min
   - Click badge → popup opens within 500ms
   - Click Export All → batch upload completes <3 min

6. **UI Responsiveness** (SC-007, SC-008)
   - Add item → badge updates within 1 second
   - Open popup → thumbnails visible immediately (<500ms)
   - Send batch with progress reporting → UI updates within 2 seconds

---

## Quick Reference

### Key Files Created
```
VesselStudioSimplePlugin/
├── Models/
│   ├── QueuedCaptureItem.cs          # Capture item entity
│   ├── CaptureQueue.cs               # Queue collection
│   ├── BatchUploadProgress.cs        # Progress reporting
│   └── BatchUploadResult.cs          # Upload result
├── Services/
│   ├── CaptureQueueService.cs        # Queue manager (singleton)
│   └── BatchUploadService.cs         # Batch upload logic
├── Commands/
│   ├── VesselAddToQueueCommand.cs    # Add capture to queue
│   ├── VesselSendBatchCommand.cs     # Upload batch (called by Quick Export button)
│   └── VesselClearQueueCommand.cs    # Clear queue (optional command)
└── UI/
    ├── QueueManagerDialog.cs         # Popup dialog for queue management
    └── Components/
        └── QueueItemControl.cs       # Individual queue item in dialog (optional)

### Key Files Modified
```
VesselStudioSimplePlugin/
└── VesselStudioToolbarPanel.cs       # Add batch badge button + Quick Export button
```

### Key Patterns
- **Singleton**: CaptureQueueService.Current
- **IDisposable**: QueuedCaptureItem, QueueManagerDialog
- **Events**: ItemAdded, ItemRemoved, QueueCleared
- **IProgress**: BatchUploadProgress reporting
- **CancellationToken**: User abort support
- **Modal Dialog**: QueueManagerDialog.ShowDialog() for thread-safe queue management

### Common Pitfalls
- ❌ Don't clear queue on partial success (preserve for retry)
- ❌ Don't upload in parallel (sequential per research.md)
- ❌ Don't store raw bitmaps (compress to JPEG first)
- ❌ Don't forget thread safety in service (use lock)
- ❌ Don't hardcode project name (get from dropdown)
- ❌ Don't show tiny thumbnails in toolbar (use popup dialog with large thumbnails instead)
- ❌ Don't forget to dispose ImageList in dialog (memory leak)

### Testing Shortcuts
```csharp
// Quick queue population for testing
for (int i = 0; i < 5; i++) {
    var bitmap = CreateTestBitmap(800, 600);
    var bytes = CompressToJpeg(bitmap, 85);
    var item = new QueuedCaptureItem(bytes, $"Test_{i}");
    CaptureQueueService.Current.AddItem(item);
}

// Mock API client for testing
public class MockApiClient : VesselStudioApiClient {
    public override async Task<bool> UploadImageAsync(...) {
        await Task.Delay(100); // Simulate network
        return true; // or false for failure testing
    }
}
```

---

## Next Steps

After implementation:
1. ✅ Complete all unit tests (see contracts/api-batch-upload.md)
2. ✅ Run integration tests (scenarios above)
3. ✅ Update plugin manifest with new commands
4. ✅ Test in Rhino 8 on Windows
5. ✅ Document any deviations from spec in CHANGELOG
6. ✅ Create PR for review

---

## Support Resources

- **Spec Questions**: Review `spec.md` functional requirements
- **Design Questions**: Review `research.md` and `data-model.md`
- **API Questions**: Review `contracts/api-batch-upload.md`
- **RhinoCommon API**: https://developer.rhino3d.com/api/RhinoCommon/
- **Existing Code**: See VesselCaptureCommand.cs for capture pattern

---

**Estimated Total Time**: 5.5 hours (individual developer, focused work)

**Complexity**: Medium (extends existing patterns, no new external dependencies)