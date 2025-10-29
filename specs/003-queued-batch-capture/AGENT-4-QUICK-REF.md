# Agent 4 Quick Reference - UI Components (T025-T042)

## Phase Gate Status
✅ **PHASE 2 COMPLETE** - Ready to start Phase 3 UI work

---

## PHASE 3: Toolbar Badge (T025-T028) ~1 hour

### T025: Add badge label
```
File: VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs
Field: Label badgeLabel
Text format: "Batch ({count})"
Initial state: Visible = false
```

### T026: Add Quick Export button
```
File: VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs
Field: Button quickExportButton
Text: "Quick Export Batch"
Initial state: Enabled = false
Click handler: Opens QueueManagerDialog
```

### T027: Style badge
```
BackColor: #E0E0E0 (light gray)
Padding: 4px all sides
```

### T028: Wire queue events
```
Subscribe to:
  - CaptureQueueService.Current.ItemAdded
  - CaptureQueueService.Current.ItemRemoved
  - CaptureQueueService.Current.QueueCleared

Handler: UpdateQueueUI()
  - If count > 0: show badge, enable button
  - If count = 0: hide badge, disable button
```

**Files to Modify**: 1 file
- `VesselStudioToolbarPanel.cs`

---

## PHASE 4: Queue Manager Dialog (T029-T042) ~1.5 hours

### T029: Create dialog form
```
File: VesselStudioSimplePlugin/UI/QueueManagerDialog.cs (NEW)
Inherits: System.Windows.Forms.Form
Size: 600x500 (fixed)
Title: "Batch Export Queue Manager"
Modal: Yes (ShowDialog)
Resizable: No (FixedDialog)
```

### T030: Add ListView
```
Name: queueListView
View: Details
CheckBoxes: true
FullRowSelect: true
Columns:
  - [0] Thumbnail (120px)
  - [1] Viewport Name (200px)
  - [2] Timestamp (150px)
ImageList: 120x90 pixel thumbnails
```

### T031: Add buttons panel
```
Panel at bottom, Height: 50px
Buttons (left to right):
  - "Remove Selected" (120x30) - Click handler: OnRemoveSelectedClick
  - "Clear All" (100x30) - Click handler: OnClearAllClick
  - "Export All" (100x30) - Click handler: OnExportAllClick (placeholder)
  - "Close" (100x30) - DialogResult: Cancel
Spacing: 10px between buttons
```

### T032-T040: LoadQueueItems()
```csharp
private void LoadQueueItems()
{
    queueListView.Items.Clear();
    var imageList = queueListView.SmallImageList;
    imageList.Images.Clear();
    
    foreach (var item in CaptureQueueService.Current.GetItems())
    {
        var listViewItem = new ListViewItem();
        listViewItem.Tag = item;
        
        // Thumbnail (Note: GetThumbnail() returns 80x60, may need scaling to 120x90)
        var thumb = item.GetThumbnail();
        string key = item.Id.ToString();
        imageList.Images.Add(key, thumb);
        listViewItem.ImageKey = key;
        
        // Columns
        listViewItem.SubItems.Add(item.ViewportName);
        listViewItem.SubItems.Add(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        
        queueListView.Items.Add(listViewItem);
    }
}
```

### T034: Remove Selected Handler
```csharp
private void OnRemoveSelectedClick(object sender, EventArgs e)
{
    var checkedItems = new List<ListViewItem>();
    foreach (ListViewItem item in queueListView.CheckedItems)
        checkedItems.Add(item);
    
    if (checkedItems.Count == 0) return;
    
    if (checkedItems.Count > 5)
    {
        var result = MessageBox.Show(
            $"Remove {checkedItems.Count} items?",
            "Confirm",
            MessageBoxButtons.YesNo);
        if (result != DialogResult.Yes) return;
    }
    
    foreach (var item in checkedItems)
    {
        var queueItem = (QueuedCaptureItem)item.Tag;
        CaptureQueueService.Current.RemoveItem(queueItem);
        queueListView.Items.Remove(item);
    }
}
```

### T035: Clear All Handler
```csharp
private void OnClearAllClick(object sender, EventArgs e)
{
    int count = CaptureQueueService.Current.ItemCount;
    if (count == 0) return;
    
    var result = MessageBox.Show(
        $"Remove all {count} items?",
        "Confirm Clear All",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning);
    
    if (result == DialogResult.Yes)
    {
        CaptureQueueService.Current.Clear();
        LoadQueueItems();
    }
}
```

### T036: Close Button
```
Already handled by DialogResult = Cancel
Or implement: this.Close()
```

### T037: CheckBoxes
```
Already configured in T030 (CheckBoxes = true)
```

### T038-T040: Display
```
Already handled by LoadQueueItems() in T032
Thumbnails: item.GetThumbnail()
Viewport: item.ViewportName
Timestamp: item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
```

### T041: Window resize
```
SKIP FOR MVP - Keep FixedDialog (not resizable)
```

### T042: Performance test
```
MANUAL TEST: Dialog opens within 500ms with 10 items
Test with cached thumbnails
Optimize if needed (async loading if >500ms)
```

**Files to Create**: 1 file (NEW)
- `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Files to Modify**: 1 file
- `VesselStudioToolbarPanel.cs` (T033 - wire button to open dialog)

---

## Integration Points

### Phase 3 → Phase 4 Bridge (T033):
```csharp
// In VesselStudioToolbarPanel
private void OnQuickExportClick(object sender, EventArgs e)
{
    using (var dialog = new QueueManagerDialog())
    {
        dialog.ShowDialog();
    }
    UpdateQueueUI(); // Refresh badge after dialog
}
```

### Phase 4 → Phase 5 Bridge (T059):
```csharp
// In QueueManagerDialog - OnExportAllClick
// Currently: MessageBox.Show("Not implemented");
// Phase 5: Create BatchUploadService, call UploadBatchAsync
```

---

## Dependencies Summary

### Must Already Exist:
✅ `QueuedCaptureItem` (T005-T008)
  - Properties: Id, ImageData, ViewportName, Timestamp, SequenceNumber
  - Method: GetThumbnail() → Returns 80x60 Bitmap
  - IDisposable for cleanup

✅ `CaptureQueue` (T009-T010)
  - Properties: Items, CreatedAt, ProjectName
  - Computed: Count, IsEmpty, TotalSizeBytes

✅ `CaptureQueueService` (T011-T015)
  - Singleton: CaptureQueueService.Current
  - Methods: AddItem(), RemoveItem(), Clear(), GetItems()
  - Events: ItemAdded, ItemRemoved, QueueCleared
  - Properties: ItemCount, IsEmpty, IsFull, TotalSizeBytes

✅ `VesselStudioToolbarPanel` (existing)
  - Existing toolbar panel with controls

### New Files to Create:
- `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

---

## Build & Test

### After Phase 3 (T025-T028):
```powershell
cd "c:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin"
.\quick-build.ps1
# Expected: 0 errors, 0 warnings
```

### After Phase 4 (T029-T042):
```powershell
# Same build command
# Expected: 0 errors, 0 warnings
```

### Manual Testing:
1. Start Rhino, load plugin
2. VesselStudioShowToolbar command to show toolbar
3. Badge should be hidden (queue empty)
4. Add some test items manually (or via command once Phase 3 cmd exists)
5. Badge should show "Batch (N)" and button enable
6. Click "Quick Export Batch" button
7. Dialog should open with thumbnails, checkboxes, buttons
8. Test Remove Selected, Clear All, Close buttons
9. Dialog should dispose properly (no memory leaks)

---

## Checklist

### Phase 3 (T025-T028):
- [ ] Badge label field created
- [ ] Quick Export button field created
- [ ] UpdateQueueUI() method implemented
- [ ] Event subscription in constructor/initializer
- [ ] Badge styled with gray background + padding
- [ ] Build succeeds (0 errors)
- [ ] Update agent-coordination.json for Phase 3

### Phase 4 (T029-T042):
- [ ] QueueManagerDialog.cs created
- [ ] Form initialized with correct size/style
- [ ] ListView with columns created
- [ ] ImageList for thumbnails created
- [ ] Button panel with 4 buttons created
- [ ] LoadQueueItems() populated correctly
- [ ] OnRemoveSelectedClick() handler works
- [ ] OnClearAllClick() handler works
- [ ] OnExportAllClick() shows placeholder
- [ ] Thumbnails display correctly
- [ ] Dialog opens from toolbar button
- [ ] Build succeeds (0 errors)
- [ ] ImageList properly disposed
- [ ] Update agent-coordination.json for Phase 4

---

## Time Estimate
- Phase 3: ~1 hour
- Phase 4: ~1.5 hours
- **Total**: ~2.5 hours (focused work)

