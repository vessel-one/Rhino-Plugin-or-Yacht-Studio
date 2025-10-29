# Data Model: Queued Batch Capture

**Feature**: 003-queued-batch-capture  
**Date**: October 28, 2025  
**Phase**: 1 - Design

## Entity Definitions

### QueuedCaptureItem

Represents a single viewport capture in the queue awaiting batch upload.

**Fields**:
- `Id: Guid` - Unique identifier for this queue item
- `ImageData: byte[]` - Compressed JPEG image data (2-5MB typical)
- `ViewportName: string` - Source viewport name (e.g., "Perspective", "Top", "Front")
- `Timestamp: DateTime` - When capture was added to queue (for chronological ordering)
- `SequenceNumber: int` - Position in batch (1-based, assigned at upload time)
- `ThumbnailCache: Bitmap` (nullable) - Cached 80x60 thumbnail for UI display

**Validation Rules**:
- ImageData must not be null or empty
- ViewportName must match Rhino viewport naming (sanitized for filesystem)
- Timestamp must be valid DateTime
- SequenceNumber assigned sequentially starting at 1 when batch uploads

**Lifecycle**:
1. Created when user executes AddToQueue command
2. Stored in CaptureQueue until upload or removal
3. Disposed after successful batch upload or manual removal
4. All items disposed when plugin unloads (session end)

**Relationships**:
- Belongs to: CaptureQueue (1:many)
- References: No direct Rhino object references (snapshot data only)

---

### CaptureQueue

Collection managing all queued captures for the current session.

**Fields**:
- `Items: List<QueuedCaptureItem>` - Ordered list of captures (chronological, FIFO)
- `CreatedAt: DateTime` - When queue was initialized (session start)
- `ProjectName: string` (nullable) - Associated project from dropdown (applies to all items)

**Computed Properties**:
- `Count: int` → Items.Count (for UI display)
- `TotalSizeBytes: long` → Sum of ImageData lengths
- `IsEmpty: bool` → Count == 0

**Validation Rules**:
- Items must maintain chronological order (earliest first)
- Maximum 20 items enforced (soft limit for performance)
- All items must have unique Ids

**Lifecycle**:
1. Singleton instance created when plugin loads
2. Cleared after successful batch upload (FR-007)
3. Preserved on upload failure for retry (FR-008)
4. Cleared when plugin unloads (FR-015)

**Relationships**:
- Contains: QueuedCaptureItem (1:many)
- Associated with: Currently selected project (1:1, optional)

---

### BatchUploadRequest

Represents a batch upload operation in progress.

**Fields**:
- `Items: List<QueuedCaptureItem>` - Snapshot of queue at upload initiation
- `ProjectId: string` - Target project ID from selected project dropdown
- `Status: BatchUploadStatus` (enum) - Current upload state
- `TotalItems: int` - Count of items in batch
- `CompletedItems: int` - Count of successfully uploaded items
- `FailedItems: List<(QueuedCaptureItem, string)>` - Failed items with error messages
- `StartedAt: DateTime` - Upload start timestamp
- `CompletedAt: DateTime` (nullable) - Upload completion timestamp

**BatchUploadStatus Enum**:
```csharp
public enum BatchUploadStatus {
    Pending,      // Created but not started
    InProgress,   // Upload in progress
    Completed,    // All items uploaded successfully
    PartiallyFailed,  // Some items failed
    Failed        // All items failed or critical error
}
```

**Validation Rules**:
- ProjectId must not be null or empty
- Items count must be > 0 (FR-012 prevents empty batches)
- CompletedItems <= TotalItems at all times
- Status transitions: Pending → InProgress → (Completed | PartiallyFailed | Failed)

**Lifecycle**:
1. Created when user executes SendBatch command
2. Updated during upload as each item completes
3. Disposed after upload completion (success or failure)
4. Not persisted (transient operation state only)

**Relationships**:
- References: QueuedCaptureItem (many, snapshot)
- Associated with: Project in Vessel Studio API (1:1)

---

## State Transitions

### QueuedCaptureItem States

```
[Created] ─────────────────────────────────────────────────┐
   │                                                        │
   ├─> [Queued] ──> [Uploading] ──> [Uploaded] ──> [Disposed]
   │       │                             │
   │       └──> [Removed] ──> [Disposed] │
   │                                     │
   └─────────────────────────────────────┘
```

**State Descriptions**:
- **Created**: Item instantiated, image captured
- **Queued**: Added to CaptureQueue, visible in UI
- **Uploading**: Part of active BatchUploadRequest
- **Uploaded**: Successfully sent to Vessel Studio API
- **Removed**: User deleted from queue before upload
- **Disposed**: Resources released (GC eligible)

---

### CaptureQueue States

```
[Empty] <─────────────────────────────────────┐
   │                                          │
   ├─> [HasItems] ──> [Uploading] ──> [Empty]│
   │       │              │                   │
   │       │              └──> [HasItems] ────┘ (on upload failure)
   │       │                       │
   │       └───────────────────────┘ (on manual clear)
```

**State Descriptions**:
- **Empty**: No items in queue (Count == 0)
- **HasItems**: One or more items queued (Count > 0)
- **Uploading**: BatchUploadRequest in progress
- **Failed Upload → HasItems**: Queue preserved for retry

---

### BatchUploadRequest States

```
[Pending] ──> [InProgress] ──┬──> [Completed]
                              ├──> [PartiallyFailed]
                              └──> [Failed]
```

**State Descriptions**:
- **Pending**: Request created, not yet started
- **InProgress**: Actively uploading items
- **Completed**: All items uploaded successfully (queue cleared)
- **PartiallyFailed**: Some items uploaded, some failed (queue preserved)
- **Failed**: All items failed or critical error (queue preserved)

---

## Relationships Diagram

```
┌─────────────────────────────────────────────────────────┐
│ CaptureQueueService (Singleton)                         │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │ CaptureQueue                                   │    │
│  │  - Items: List<QueuedCaptureItem>              │    │
│  │  - ProjectName: string                         │    │
│  │  - Count: int                                  │    │
│  └────────────────────────────────────────────────┘    │
│         │                                               │
│         │ contains (1:many)                             │
│         ▼                                               │
│  ┌────────────────────────────────────────────────┐    │
│  │ QueuedCaptureItem                              │    │
│  │  - Id: Guid                                    │    │
│  │  - ImageData: byte[]                           │    │
│  │  - ViewportName: string                        │    │
│  │  - Timestamp: DateTime                         │    │
│  │  - SequenceNumber: int                         │    │
│  └────────────────────────────────────────────────┘    │
│                                                          │
└─────────────────────────────────────────────────────────┘
                       │
                       │ snapshot for upload
                       ▼
            ┌──────────────────────────────────────┐
            │ BatchUploadRequest                  │
            │  - Items: List<QueuedCaptureItem>   │
            │  - ProjectId: string                │
            │  - Status: BatchUploadStatus        │
            │  - Progress: int (CompletedItems)   │
            └──────────────────────────────────────┘
                       │
                       │ uploads to
                       ▼
            ┌──────────────────────────────────────┐
            │ Vessel Studio API                    │
            │  Project Entry                       │
            │    └─ Multiple Images                │
            └──────────────────────────────────────┘
```

---

## Data Constraints

### Size Limits
- **QueuedCaptureItem.ImageData**: 2-10 MB per item (JPEG compressed)
- **CaptureQueue.Items**: Maximum 20 items (soft limit, ~100 MB total)
- **Thumbnail size**: 80x60 pixels (cached in memory)

### Performance Constraints
- **Queue operations**: O(1) add, O(n) remove by index
- **Thumbnail generation**: On-demand, cached per item
- **Memory footprint**: ~5 MB per queued item (compressed + thumbnail)

### Validation Constraints
- **Filename pattern**: `{ProjectName}_{ViewportName}_{Sequence:D3}.png`
- **Sequence numbers**: 001-999 (3-digit zero-padded)
- **Viewport names**: Sanitized to remove illegal filesystem characters
- **Project selection**: Required before batch upload (FR-014)

---

## Implementation Notes

### Memory Management
```csharp
public class QueuedCaptureItem : IDisposable {
    private byte[] _imageData;
    private Bitmap _thumbnailCache;
    
    public void Dispose() {
        _thumbnailCache?.Dispose();
        _thumbnailCache = null;
        _imageData = null;  // Allow GC
    }
}
```

### Thread Safety
- CaptureQueueService uses lock for add/remove operations
- UI updates marshaled to main thread via Control.Invoke
- BatchUploadRequest state changes are atomic

### Validation Patterns
```csharp
public class QueuedCaptureItem {
    public void Validate() {
        if (ImageData == null || ImageData.Length == 0)
            throw new InvalidOperationException("Image data required");
        if (string.IsNullOrWhiteSpace(ViewportName))
            throw new InvalidOperationException("Viewport name required");
        if (Timestamp == DateTime.MinValue)
            Timestamp = DateTime.Now;
    }
}
```