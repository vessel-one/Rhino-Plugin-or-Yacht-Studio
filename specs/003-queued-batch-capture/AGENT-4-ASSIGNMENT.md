# Agent 4 Assignment: UI Components Implementation

## Overview
Agent 4 is assigned to implement all UI components for the batch capture feature across two phases:
- **Phase 3 (US1)**: Toolbar badge counter (T025-T028)
- **Phase 4 (US2)**: Queue Manager dialog (T029-T042)

## Current Status
✅ **Phase 2 Foundational COMPLETE** - All models and services ready
- ✅ T005-T008: QueuedCaptureItem (with IDisposable, validation, thumbnails)
- ✅ T009-T010: CaptureQueue (collection model)
- ✅ T016-T017: BatchUploadProgress & BatchUploadResult
- ✅ T011-T015: CaptureQueueService (thread-safe singleton with events)

**Ready to begin**: Phase 3 & Phase 4 UI implementation

---

## Phase 3: Toolbar UI (T025-T028) - ~1 hour
**Location**: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`

### T025: Add badge label
- Create Label control showing "Batch (N)"
- Position next to existing buttons
- Initially hidden

### T026: Add Quick Export button
- Create Button: "Quick Export Batch"
- Initially disabled
- Click opens QueueManagerDialog

### T027: Style badge
- Light gray background (#E0E0E0)
- 4px padding

### T028: Subscribe to queue events
- Subscribe to: ItemAdded, ItemRemoved, QueueCleared
- UpdateQueueUI() handler updates badge visibility & button enabled state

**Key Integration Points**:
```
VesselStudioToolbarPanel → CaptureQueueService.Current
                         → Subscribe to events
                         → Update badge/button based on queue count
                         → Launch QueueManagerDialog on button click
```

---

## Phase 4: Queue Manager Dialog (T029-T042) - ~1.5 hours
**Location**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

### T029: Create dialog form
- 600x500 fixed size
- Modal (ShowDialog())
- Title: "Batch Export Queue Manager"

### T030: ListView with columns
- 3 columns: Thumbnail | Viewport Name | Timestamp
- ImageList for 120x90 thumbnails
- CheckBoxes enabled

### T031: Action buttons panel
- "Remove Selected" → removes checked items
- "Clear All" → clears entire queue with confirmation
- "Export All" → placeholder (Phase 5)
- "Close" → closes dialog

### T032-T040: Populate ListView
- LoadQueueItems() method loads from CaptureQueueService
- Generates 120x90 thumbnails for each item
- Shows viewport name and timestamp

### T041-T042: Performance & polish
- Dialog opens within 500ms (with cached thumbnails)
- Test with 10, 15, 20 items

**Key Integration Points**:
```
QueueManagerDialog → CaptureQueueService.Current
                  → GetItems() for list population
                  → RemoveItem() for remove operations
                  → Clear() for clear operation
                  → GetThumbnail(120, 90) for display
```

---

## Dependencies & Prerequisites

### Must Already Exist (✅ Confirmed):
1. `VesselStudioSimplePlugin/Models/QueuedCaptureItem.cs`
   - Has `GetThumbnail()` method (returns 80x60 Bitmap)
   - Has `Dispose()` for cleanup
   
2. `VesselStudioSimplePlugin/Models/CaptureQueue.cs`
   - Collection model with Items list
   - Computed properties: Count, IsEmpty, TotalSizeBytes

3. `VesselStudioSimplePlugin/Services/CaptureQueueService.cs`
   - Singleton: `CaptureQueueService.Current`
   - Methods: `AddItem()`, `RemoveItem()`, `Clear()`, `GetItems()`
   - Events: `ItemAdded`, `ItemRemoved`, `QueueCleared`
   - Thread-safe (uses lock)

4. `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`
   - Existing toolbar panel class
   - Already has controls and layout

### Must Be Created:
1. `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs` (NEW)

---

## Important Implementation Notes

### T032: LoadQueueItems() Thumbnail Generation
The provided template calls `item.GetThumbnail(120, 90)` but QueuedCaptureItem only has `GetThumbnail()` that returns 80x60. You may need to:

**Option 1**: Modify to use 80x60
```csharp
var thumbnail = item.GetThumbnail(); // Returns 80x60 bitmap
```

**Option 2**: Add new method to QueuedCaptureItem
```csharp
public Bitmap GetThumbnail(int width, int height) { /* ... */ }
```

**Option 3**: Scale the 80x60 thumbnail to 120x90 in LoadQueueItems()
```csharp
var thumbnail80x60 = item.GetThumbnail();
var thumbnail120x90 = new Bitmap(thumbnail80x60, new Size(120, 90));
```

### RemoveItem() vs RemoveItemAt()
CaptureQueueService has:
- `RemoveItem(QueuedCaptureItem item)` - takes the item itself
- `RemoveItemAt(int index)` - takes index

The dialog should use `RemoveItem(item)` since it has the item reference in `listItem.Tag`.

### ImageList Disposal
Remember to dispose the ImageList when dialog closes:
```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        thumbnailImageList?.Dispose();
    }
    base.Dispose(disposing);
}
```

---

## Testing Checklist

### Phase 3 Testing (Toolbar):
- [ ] Badge label appears when item added (shows "Batch (1)")
- [ ] Badge label hides when queue empty
- [ ] Quick Export button enabled when items in queue
- [ ] Quick Export button disabled when queue empty
- [ ] Button click opens QueueManagerDialog
- [ ] Build has 0 errors, 0 warnings

### Phase 4 Testing (Dialog):
- [ ] Dialog opens and shows all queued items
- [ ] Thumbnails display correctly (120x90)
- [ ] Viewport names show in second column
- [ ] Timestamps show in third column
- [ ] Checkboxes work (can check/uncheck items)
- [ ] Remove Selected removes checked items + shows confirmation for >5 items
- [ ] Clear All removes all items + shows confirmation
- [ ] Close button closes dialog
- [ ] Dialog opens within 500ms with 10 items (SC-008)
- [ ] No memory leaks (ImageList disposed properly)
- [ ] Build has 0 errors, 0 warnings

---

## Coordination Updates Needed

### After Phase 3 (T025-T028):
```json
{
  "phase_3_user_story_1": {
    "status": "in-progress",
    "parallel_groups": {
      "group_2_toolbar": {
        "agent": "agent_4",
        "status": "completed"
      }
    },
    "assigned_agents": ["agent_4"],
    "completed_tasks": ["T025", "T026", "T027", "T028"]
  },
  "agents": {
    "agent_4": {
      "status": "in-progress",
      "current_task": "Phase 4",
      "completed_tasks": ["T025", "T026", "T027", "T028"],
      "assigned_phase": "phase_3_user_story_1"
    }
  }
}
```

### After Phase 4 (T029-T042):
```json
{
  "phase_4_user_story_2": {
    "status": "completed",
    "parallel_groups": {
      "group_1_dialog": {
        "agent": "agent_4",
        "status": "completed"
      }
    },
    "assigned_agents": ["agent_4"],
    "completed_tasks": ["T029", "T030", "T031", "T032", "T033", "T034", "T035", "T036", "T037", "T038", "T039"]
  },
  "agents": {
    "agent_4": {
      "status": "completed",
      "current_task": null,
      "completed_tasks": ["T025", "T026", "T027", "T028", "T029", "T030", "T031", "T032", "T033", "T034", "T035", "T036", "T037", "T038", "T039"],
      "assigned_phase": "phase_4_user_story_2"
    }
  }
}
```

---

## Parallel Work Opportunities

**Agent 3** is simultaneously working on:
- T018-T024: VesselAddToQueueCommand (Phase 3)
- T040-T042: Integration tasks (Phase 4)

**Agent 2** will later work on:
- Phase 5 (US3): BatchUploadService and upload progress

The work can proceed independently since:
- Phase 3 (toolbar): Only needs CaptureQueueService events
- Phase 4 (dialog): Only needs CaptureQueueService methods
- No cross-dependencies between Agent 3 and Agent 4 Phase 3/4 work

---

## Success Criteria Summary

**Phase 3 Complete When**:
- Toolbar has badge showing "Batch (N)" format
- Badge toggles visibility with queue changes
- Quick Export button enables/disables with queue
- Button click launches QueueManagerDialog
- 0 compiler errors

**Phase 4 Complete When**:
- Dialog displays queue items in ListView
- Thumbnails show correctly (120x90)
- Checkboxes work for multi-select
- Remove/Clear/Close buttons functional
- Dialog opens within 500ms
- 0 compiler errors
- Export All button placeholder shows message

**MVP Ready When**:
- Phase 3 + Phase 4 complete (UI foundation)
- Agent 3 Phase 3 complete (add-to-queue command)
- Agent 2 Phase 5 complete (batch upload service)
- All three can then be wired together for full workflow

