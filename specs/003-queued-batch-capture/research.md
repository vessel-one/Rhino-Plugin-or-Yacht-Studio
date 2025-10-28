# Research: Queued Batch Capture

**Feature**: 003-queued-batch-capture  
**Date**: October 28, 2025  
**Phase**: 0 - Research

## Research Questions

### 1. Thumbnail Generation from Bitmap

**Question**: How to generate thumbnail previews from full-resolution viewport captures for queue display?

**Decision**: Use System.Drawing.Bitmap scaling with high-quality interpolation

**Rationale**:
- System.Drawing already in use for viewport captures
- Built-in thumbnail generation via `Bitmap.GetThumbnailImage()` or manual scaling
- Target size: 80x60 pixels for list display, maintains aspect ratio
- High-quality bicubic interpolation for smooth scaling

**Alternatives Considered**:
- Store full images and scale in UI paint events → Rejected: Poor performance with 20 images
- Generate thumbnails server-side → Rejected: Adds latency, requires upload before display
- Use Rhino viewport thumbnail API → Selected if available, falls back to Bitmap scaling

**Implementation Notes**:
```csharp
// Preferred approach: Rhino viewport thumbnail
var thumbnail = viewport.GetThumbnailImage(80, 60);

// Fallback: Manual bitmap scaling
var thumbnail = new Bitmap(fullImage, new Size(80, 60));
```

---

### 2. In-Memory Queue Management

**Question**: What data structure and access pattern for managing queue with add/remove/clear operations?

**Decision**: Use `List<QueuedCaptureItem>` with FIFO ordering, singleton service for session scope

**Rationale**:
- List provides indexed access for display and removal by position
- FIFO ordering matches chronological requirement (FR-013)
- Singleton CaptureQueueService ensures single queue per plugin instance
- No persistence needed (session-only per FR-015)

**Alternatives Considered**:
- Queue<T> data structure → Rejected: No random access for removal by index
- ObservableCollection<T> → Rejected: Unnecessary WPF dependency, using WinForms
- Static list in command class → Rejected: Violates separation of concerns

**Implementation Notes**:
```csharp
public class CaptureQueueService
{
    private static CaptureQueueService _instance;
    private List<QueuedCaptureItem> _queue = new List<QueuedCaptureItem>();
    
    public void Add(QueuedCaptureItem item) => _queue.Add(item);
    public void RemoveAt(int index) => _queue.RemoveAt(index);
    public void Clear() => _queue.Clear();
    public IReadOnlyList<QueuedCaptureItem> GetAll() => _queue.AsReadOnly();
}
```

---

### 3. Batch API Upload Strategy

**Question**: Should batch upload send images sequentially or in parallel? Single API call or multiple?

**Decision**: Sequential upload with single multipart/form-data request if API supports, fallback to sequential individual uploads

**Rationale**:
- Sequential uploads simplify progress tracking and error handling
- Single multipart request reduces API round trips (if supported)
- Fallback to individual uploads maintains compatibility with current API
- Progress callback after each image upload for UI updates (meets SC-007 <2s lag)

**Alternatives Considered**:
- Parallel uploads with Task.WhenAll → Rejected: Complicates progress tracking, risks overwhelming API
- Zip file upload → Rejected: Server must unzip, loses individual file metadata
- Base64 encoded JSON array → Rejected: Inefficient for binary image data

**Implementation Notes**:
```csharp
// Preferred: Single multipart request
var content = new MultipartFormDataContent();
foreach (var item in queue) {
    content.Add(new ByteArrayContent(item.ImageData), "files", item.Filename);
}
await apiClient.PostAsync("/batch-upload", content);

// Fallback: Sequential individual uploads
foreach (var item in queue) {
    await apiClient.UploadImageAsync(item.ImageData, item.Filename);
    UpdateProgress((++completed / total) * 100);
}
```

---

### 4. Queue UI Integration

**Question**: How to integrate queue management UI into existing VesselStudioToolbarPanel without cramming thumbnails into limited toolbar space?

**Decision**: Compact toolbar indicator ("Batch (N)" badge + "Quick Export Batch" button) with popup dialog for full queue management

**Rationale**:
- Toolbar has limited vertical space - full thumbnail list would push other controls off screen
- Popup dialog allows larger thumbnails (120x90px min) for better visual review (FR-019)
- Checkboxes for multi-select operations fit naturally in popup, not toolbar
- Toolbar shows queue count at-a-glance, popup opened only when needed for detailed management
- Popup can be resized/scrolled without affecting toolbar layout (SC-004)
- Similar pattern to Windows File Explorer status bar ("N items selected") + context menu

**Alternatives Considered**:
- Inline collapsible list in toolbar → Rejected: Cramped thumbnails, hard to see images, scrolling awkward
- Separate docked panel → Rejected: More complex, requires panel management, less integrated
- Tooltips on hover → Rejected: Can't show multiple images simultaneously for comparison

**UI Layout**:

**Toolbar (compact)**:
```
┌─────────────────────────────────────┐
│ [Settings] [Capture] [Batch (3)] ↗  │  ← Badge shows count
│ [Project Dropdown ▼]                │
│ [Quick Export Batch] ← Enabled when count > 0
│ [Quick Tips Card...]                │
└─────────────────────────────────────┘
```

**Popup Dialog (opened on badge click)**:
```
┌──────────────── Queue Manager ────────────────┐
│ Project: YachtDesignA                    [×]  │
│ ┌────────────────────────────────────────┐   │
│ │ ☑ [Thumb] Perspective    001  [Remove] │   │
│ │ ☑ [Thumb] Top            002  [Remove] │   │
│ │ ☐ [Thumb] Front          003  [Remove] │   │ ← Checkboxes
│ │ ☑ [Thumb] Starboard      004  [Remove] │   │   for batch ops
│ │                                  [...]  │   │
│ └────────────────────────────────────────┘   │
│                                               │
│ [Select All] [Select None]                   │
│ [Remove Selected] [Clear All] [Export All]   │
└───────────────────────────────────────────────┘
```

**Implementation Notes**:
```csharp
// Toolbar compact controls
public class VesselStudioToolbarPanel : UserControl {
    private Button _batchBadgeButton;  // Shows "Batch (N)", opens popup
    private Button _quickExportButton;  // Quick export without opening popup
    
    private void InitializeBatchControls() {
        _batchBadgeButton = new Button { 
            Text = "Batch (0)", 
            Width = 80 
        };
        _batchBadgeButton.Click += OpenQueueManagerPopup;
        
        _quickExportButton = new Button { 
            Text = "Quick Export Batch", 
            Enabled = false 
        };
        _quickExportButton.Click += QuickExportBatch;
        
        // Subscribe to queue updates
        CaptureQueueService.Current.ItemAdded += UpdateBatchCount;
        CaptureQueueService.Current.ItemRemoved += UpdateBatchCount;
    }
    
    private void UpdateBatchCount(object sender, EventArgs e) {
        var count = CaptureQueueService.Current.Count;
        _batchBadgeButton.Text = $"Batch ({count})";
        _quickExportButton.Enabled = count > 0;
    }
    
    private void OpenQueueManagerPopup(object sender, EventArgs e) {
        using (var dialog = new QueueManagerDialog()) {
            dialog.ShowDialog(this);  // Modal popup
        }
    }
}

// Popup dialog for queue management
public class QueueManagerDialog : Form {
    private ListView _queueListView;  // Shows items with large thumbnails
    
    public QueueManagerDialog() {
        Text = "Queue Manager";
        Size = new Size(600, 500);
        
        _queueListView = new ListView {
            View = View.Details,
            CheckBoxes = true,  // FR-018 checkbox support
            LargeImageList = new ImageList { ImageSize = new Size(120, 90) }  // FR-019
        };
        
        LoadQueueItems();
    }
    
    private void LoadQueueItems() {
        var items = CaptureQueueService.Current.GetItems();
        foreach (var item in items) {
            var thumb = item.GetThumbnail();  // 120x90 from research Q1
            _queueListView.LargeImageList.Images.Add(item.Id.ToString(), thumb);
            
            var listItem = new ListViewItem {
                Text = item.ViewportName,
                ImageKey = item.Id.ToString(),
                Tag = item
            };
            _queueListView.Items.Add(listItem);
        }
    }
}
```

**Performance Considerations**:
- Dialog opens within 500ms (SC-008) - thumbnails pre-generated on queue add
- Modal dialog prevents queue modifications during review (thread-safe)
- ListView with virtual mode for >50 items (future-proofing beyond 20-item limit)

---

### 5. Filename Generation Strategy

**Question**: How to implement ProjectName_ViewportName_Sequence.png pattern with proper sanitization?

**Decision**: Template-based filename builder with regex sanitization for filesystem-safe names

**Rationale**:
- Spec defines exact pattern: ProjectName_ViewportName_Sequence.png (FR-017)
- Viewport name from RhinoViewport.Name property
- Sequence zero-padded to 3 digits (001-999) for proper sorting
- Sanitize project/viewport names to remove illegal characters

**Alternatives Considered**:
- GUID-based filenames → Rejected: Not human-readable, spec requires descriptive names
- Hash-based deduplication → Rejected: Not needed, sequence handles uniqueness
- Custom user-defined patterns → Rejected: Out of scope, fixed pattern specified

**Implementation Notes**:
```csharp
public string GenerateFilename(string projectName, string viewportName, int sequence) {
    var safeProject = SanitizeFilename(projectName);
    var safeViewport = SanitizeFilename(viewportName);
    return $"{safeProject}_{safeViewport}_{sequence:D3}.png";
}

private string SanitizeFilename(string input) {
    // Remove invalid filesystem characters
    return Regex.Replace(input, @"[^\w\-]", "_");
}
```

---

### 6. Memory Management for Large Queues

**Question**: How to handle memory pressure with 20 full-resolution captures (potential 100MB+ total)?

**Decision**: Store compressed JPEG bytes (not Bitmap objects), generate thumbnails on-demand, implement disposal

**Rationale**:
- Full Bitmap objects hold uncompressed data (~10MB per 4K capture)
- Store compressed JPEG byte arrays (~2-5MB per capture after compression)
- Generate thumbnails from compressed data only when displaying
- Implement IDisposable on QueuedCaptureItem for proper cleanup

**Alternatives Considered**:
- Store full Bitmap objects → Rejected: Excessive memory (200MB for 20 captures)
- Disk-based temporary storage → Rejected: Violates session-only requirement, adds I/O overhead
- Lazy-load images on upload → Rejected: Must preserve queue on upload failure (FR-008)

**Implementation Notes**:
```csharp
public class QueuedCaptureItem : IDisposable {
    private byte[] _compressedImageData;  // JPEG compressed
    private Bitmap _thumbnail;            // Cached thumbnail
    
    public Bitmap GetThumbnail() {
        if (_thumbnail == null) {
            using (var ms = new MemoryStream(_compressedImageData))
            using (var fullImage = Image.FromStream(ms)) {
                _thumbnail = new Bitmap(fullImage, 80, 60);
            }
        }
        return _thumbnail;
    }
    
    public void Dispose() {
        _thumbnail?.Dispose();
        _compressedImageData = null;
    }
}
```

---

## Best Practices

### Windows Forms UI Patterns

**Pattern**: Model-View-Controller for custom controls
- QueueListControl acts as View, displays items from CaptureQueueService (Model)
- Command classes (Controller) update service and trigger UI refresh
- Event-driven updates: Service raises CollectionChanged event, UI subscribes

**Pattern**: Double-buffering for flicker-free lists
- Enable DoubleBuffered property on QueueListControl
- Use BeginUpdate/EndUpdate when batch-adding items
- Paint thumbnails in OnPaint override with cached Bitmap

### RhinoCommon Command Patterns

**Pattern**: Non-blocking commands for UI operations
- AddToQueue command returns immediately after queueing
- SendBatch command shows progress dialog, allows cancellation
- Use Rhino.UI.Dialogs.ShowMessage for confirmations (Clear Queue)

**Pattern**: Command availability based on context
- Disable "Send Batch" command when queue empty (FR-012)
- Check API key before batch send (FR-014)
- Validate active viewport before Add to Queue

---

## Open Questions

None - all technical decisions resolved through research.