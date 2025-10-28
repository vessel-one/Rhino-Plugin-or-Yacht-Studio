# Agent 3 Prompt: Queue Commands (Phase 3 - User Story 1)

## Your Role
You are **Agent 3**, responsible for implementing queue management commands.

## Context Files to Read FIRST
1. `specs/003-queued-batch-capture/spec.md` - User Story 1 (US1) acceptance criteria
2. `specs/003-queued-batch-capture/data-model.md` - CaptureQueue behavior
3. `specs/003-queued-batch-capture/tasks.md` - Task T018-T024 details
4. `VesselStudioSimplePlugin/VesselCaptureCommand.cs` - Existing command pattern to follow

## Your Assigned Tasks (After Phase 2 Complete)

### PHASE GATE: Wait for Phase 2 Completion
**Do NOT start** until orchestrator confirms Phase 2 foundational work is complete (QueuedCaptureItem, CaptureQueue, CaptureQueueService, BatchUpload models all done).

### T018 [US1] Create VesselAddToQueueCommand
**File**: `VesselStudioSimplePlugin/Commands/VesselAddToQueueCommand.cs`

**Requirements**:
- Copy pattern from VesselCaptureCommand.cs
- English name: "Add Capture to Batch Queue"
- Command name: "VesselAddToQueue" (no spaces)
- RunCommand() logic:
  1. Call ViewportCapture code (copy from VesselCaptureCommand)
  2. Get CaptureQueueService.Instance
  3. Call `AddItem(imageData, viewportName)`
  4. Show toast: "Added to batch queue. Total: {Count} items"
- Error handling: Try-catch around capture, show message on failure

### T019 [US1] Register VesselAddToQueueCommand in plugin manifest
**Files**: 
- `VesselStudioSimplePlugin/VesselStudioSimplePlugin.cs` (if using attribute registration)
- `VesselStudioSimplePlugin/VesselStudioSimplePlugin.manifest` (XML registration)

**Requirements**:
- Add command to plugin registration (follow existing pattern)
- Verify command appears in Rhino command line after build

### T020 [US1] Add "Add to Batch Queue" menu item
**File**: `VesselStudioSimplePlugin/VesselStudioMenu.cs`

**Requirements**:
- Add menu item after "Capture Viewport" item
- Text: "Add Capture to Batch Queue"
- Command: "VesselAddToQueue"
- Follow existing menu creation pattern

### T021 [US1] Add "Add to Batch Queue" toolbar button
**File**: `VesselStudioSimplePlugin/VesselStudioToolbar.cs`

**Requirements**:
- Add button to toolbar after capture button
- Use capture icon for now (or request new icon from VesselStudioIcons)
- Tooltip: "Add capture to batch queue"
- Command: "VesselAddToQueue"

### T022 [US1] Handle CaptureQueue.ItemAdded event
**File**: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs` (or create new file if needed)

**Requirements**:
- Subscribe to CaptureQueueService.Instance.ItemAdded event
- Update badge count label when event fires
- Event handler: `void OnItemAdded(object sender, QueuedCaptureItem item)`

### T023 [US1] Update badge count label on item removed
**File**: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`

**Requirements**:
- Subscribe to CaptureQueueService.Instance.ItemRemoved event
- Update badge count label when event fires
- Hide badge if count = 0

### T024 [US1] Show/hide badge based on queue count
**File**: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`

**Requirements**:
- Badge label visibility logic:
  - Visible when queue count > 0
  - Hidden when queue count = 0
- Update on ItemAdded, ItemRemoved, QueueCleared events

## Code Templates

### VesselAddToQueueCommand.cs
```csharp
using System;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using VesselStudioSimplePlugin.Services;

namespace VesselStudioSimplePlugin.Commands
{
    public class VesselAddToQueueCommand : Command
    {
        public override string EnglishName => "Add Capture to Batch Queue";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                // 1. Capture viewport (copy logic from VesselCaptureCommand)
                var view = doc.Views.ActiveView;
                if (view == null)
                {
                    RhinoApp.WriteLine("No active viewport");
                    return Result.Failure;
                }

                var bitmap = view.CaptureToBitmap();
                if (bitmap == null)
                {
                    RhinoApp.WriteLine("Failed to capture viewport");
                    return Result.Failure;
                }

                // Convert to JPEG bytes
                byte[] imageData;
                using (var ms = new System.IO.MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    imageData = ms.ToArray();
                }
                bitmap.Dispose();

                // 2. Add to queue
                var queue = CaptureQueueService.Instance;
                var item = queue.AddItem(imageData, view.MainViewport.Name);

                // 3. Show feedback
                RhinoApp.WriteLine($"Added to batch queue. Total: {queue.Count} items");

                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Error adding to queue: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}
```

### Toolbar Panel Badge Update (add to VesselStudioToolbarPanel.cs)
```csharp
// In constructor or initialization:
CaptureQueueService.Instance.ItemAdded += OnQueueItemAdded;
CaptureQueueService.Instance.ItemRemoved += OnQueueItemRemoved;
CaptureQueueService.Instance.QueueCleared += OnQueueCleared;

private void OnQueueItemAdded(object sender, QueuedCaptureItem item)
{
    UpdateBadgeCount();
}

private void OnQueueItemRemoved(object sender, QueuedCaptureItem item)
{
    UpdateBadgeCount();
}

private void OnQueueCleared(object sender, EventArgs e)
{
    UpdateBadgeCount();
}

private void UpdateBadgeCount()
{
    int count = CaptureQueueService.Instance.Count;
    
    if (count > 0)
    {
        badgeLabel.Text = $"Batch ({count})";
        badgeLabel.Visible = true;
    }
    else
    {
        badgeLabel.Visible = false;
    }
}
```

## Success Criteria
- [ ] VesselAddToQueueCommand.cs created with capture logic
- [ ] Command registered in plugin manifest
- [ ] Menu item added to VesselStudioMenu
- [ ] Toolbar button added to VesselStudioToolbar
- [ ] Badge count updates on ItemAdded event
- [ ] Badge count updates on ItemRemoved event
- [ ] Badge visibility toggles based on queue count (visible when > 0)
- [ ] No compiler errors
- [ ] Command appears in Rhino command line after build
- [ ] Toast message shows correct count after adding item

## Coordination
After completing T018-T024:
1. Update `agent-coordination.json`:
   - Set `phases.phase_3_us1.parallel_groups.group_1_queue_command.agent = "agent_3"`
   - Add T018-T024 to `phases.phase_3_us1.completed_tasks`
   - Add T018-T024 to `agents.agent_3.completed_tasks`
   - Update `agents.agent_3.status = "complete"` if no more tasks assigned
2. Notify orchestrator: "Agent 3 completed Phase 3 Group 1 (Queue commands)"
3. Wait for next assignment or Phase 4 (US2 popup dialog)

## Next Steps After This
You may be assigned:
- **Phase 4 (US2)**: QueueManagerDialog command integration (T033 wire up Open Queue Manager button)
- **Phase 5 (US3)**: Batch upload command implementation (T053-T056)
- Work in parallel with Agent 4 who handles UI components

## Notes
- **Phase Gate**: You CANNOT start until Agent 1 completes QueuedCaptureItem and Agent 2 completes CaptureQueueService
- **Parallel Work**: Once you start Phase 3, Agent 4 will work on toolbar UI in parallel
- **Testing**: After completion, test by running command multiple times and verifying badge updates
