# Agent 4: Project Structure & What You'll Add

## Current Project Structure

```
VesselStudioSimplePlugin/
├── Properties/
│   └── AssemblyInfo.cs
├── Models/
│   ├── QueuedCaptureItem.cs        ✅ EXISTS (T005-T008)
│   ├── CaptureQueue.cs             ✅ EXISTS (T009-T010)
│   ├── BatchUploadProgress.cs       ✅ EXISTS (T016)
│   └── BatchUploadResult.cs         ✅ EXISTS (T017)
├── Services/
│   ├── CaptureQueueService.cs       ✅ EXISTS (T011-T015)
│   └── BatchUploadService.cs        ✅ EXISTS (Phase 5)
├── UI/
│   └── ❌ DOESN'T EXIST YET - YOU CREATE THIS
│       └── QueueManagerDialog.cs    📝 YOU CREATE (T029-T042)
├── Commands/
│   ├── VesselAddToQueueCommand.cs   ✅ EXISTS (Phase 3)
│   ├── VesselCaptureCommand.cs
│   ├── VesselSetApiKeyCommand.cs
│   ├── VesselStudioDebugCommand.cs
│   ├── VesselStatusCommand.cs
│   └── ... (other commands)
├── VesselStudioToolbarPanel.cs      ✏️ YOU MODIFY (T025-T028, T033)
├── VesselStudioToolbar.cs
├── VesselStudioMenu.cs
├── VesselStudioSimplePlugin.cs      (main plugin class)
├── VesselStudioApiClient.cs
├── VesselStudioSettings.cs
├── VesselStudioSettingsDialog.cs
├── VesselStudioAboutDialog.cs
├── VesselStudioIcons.cs
├── BuildConfig.cs
└── VesselStudioSimplePlugin.csproj

bin/
└── Release/
    └── net48/
        └── VesselStudioSimplePlugin.rhp (compiled plugin)
```

---

## What Agent 4 Will Create

### NEW: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Purpose**: Modal dialog for reviewing and managing queued batch captures

**Size**: ~250 lines of code

**Contains**:
- Form with 600x500 size (fixed)
- ListView with 3 columns (Thumbnail, Viewport, Timestamp)
- ImageList for 120x90 thumbnails
- 4 action buttons (Remove Selected, Clear All, Export All, Close)
- Event handlers for button clicks
- LoadQueueItems() method to populate from queue service

**Tasks Covered**: T029-T042

---

## What Agent 4 Will Modify

### MODIFY: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`

**Changes**: Add toolbar badge and quick export button

**Size**: ~80 lines added

**Contains**:
- Label for "Batch (N)" badge
- Button for "Quick Export Batch"
- InitializeQueueUI() setup method
- UpdateQueueUI() event handler
- OnQuickExportClick() button handler
- Event subscriptions to queue service

**Tasks Covered**: T025-T028, T033

---

## Directory Setup

### Step 1: Verify UI directory exists
```powershell
# Check if exists
Test-Path "VesselStudioSimplePlugin\UI"

# If not, create it
New-Item -ItemType Directory "VesselStudioSimplePlugin\UI" -Force
```

### Step 2: Create QueueManagerDialog.cs
- Location: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`
- Copy from: AGENT-4-CODE-TEMPLATES.md - TEMPLATE 2
- ~250 lines, includes all necessary code

### Step 3: Modify VesselStudioToolbarPanel.cs
- Location: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`
- Copy from: AGENT-4-CODE-TEMPLATES.md - TEMPLATE 1
- Add ~80 lines of new methods and fields

---

## What Already Exists (Use These!)

### Models (Read-Only)
```csharp
// ✅ VesselStudioSimplePlugin/Models/QueuedCaptureItem.cs
public class QueuedCaptureItem : IDisposable
{
    public Guid Id { get; }
    public byte[] ImageData { get; }
    public string ViewportName { get; }
    public DateTime Timestamp { get; }
    public int SequenceNumber { get; set; }
    
    public Bitmap GetThumbnail() { /* ... */ }
    public void Dispose() { /* ... */ }
}

// ✅ VesselStudioSimplePlugin/Models/CaptureQueue.cs
public class CaptureQueue
{
    public List<QueuedCaptureItem> Items { get; }
    public DateTime CreatedAt { get; }
    public string ProjectName { get; set; }
    
    public int Count { get { return Items.Count; } }
    public long TotalSizeBytes { get { /* ... */ } }
    public bool IsEmpty { get { return Count == 0; } }
}
```

### Service (Main Integration Point)
```csharp
// ✅ VesselStudioSimplePlugin/Services/CaptureQueueService.cs
public class CaptureQueueService
{
    public static CaptureQueueService Current { get; } // SINGLETON
    
    // Methods
    public void AddItem(QueuedCaptureItem item)
    public bool RemoveItem(QueuedCaptureItem item)
    public void RemoveItemAt(int index)
    public void Clear()
    public List<QueuedCaptureItem> GetItems()
    
    // Properties
    public int ItemCount { get; }
    public bool IsEmpty { get; }
    public bool IsFull { get; }
    public int RemainingCapacity { get; }
    public long TotalSizeBytes { get; }
    public string ProjectName { get; set; }
    
    // Events
    public event EventHandler<ItemAddedEventArgs> ItemAdded;
    public event EventHandler<ItemRemovedEventArgs> ItemRemoved;
    public event EventHandler QueueCleared;
}
```

---

## Files to Reference (Don't Modify)

### Existing UI Patterns
- `VesselStudioToolbar.cs` - Example toolbar implementation
- `VesselStudioAboutDialog.cs` - Example dialog implementation
- `VesselStudioSettingsDialog.cs` - Another dialog example

**Use these to understand code style and patterns!**

---

## Build Command

After making changes:
```powershell
cd "c:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin"
.\quick-build.ps1
```

Expected output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Key Classes You'll Use

### In QueueManagerDialog.cs
```csharp
// Import
using VesselStudioSimplePlugin.Services;
using VesselStudioSimplePlugin.Models;

// Use
CaptureQueueService.Current.GetItems()           // Get list
CaptureQueueService.Current.RemoveItem(item)     // Remove
CaptureQueueService.Current.Clear()              // Clear all
CaptureQueueService.Current.ItemCount            // Get count

// Access item properties
item.Id
item.ViewportName
item.Timestamp
item.GetThumbnail()  // Returns 80x60 Bitmap
```

### In VesselStudioToolbarPanel.cs
```csharp
// Import
using VesselStudioSimplePlugin.Services;
using VesselStudioSimplePlugin.UI;

// Use
CaptureQueueService.Current.ItemCount
CaptureQueueService.Current.ItemAdded += ...
CaptureQueueService.Current.ItemRemoved += ...
CaptureQueueService.Current.QueueCleared += ...

// Launch dialog
using (var dialog = new QueueManagerDialog())
{
    dialog.ShowDialog();
}
```

---

## Visual Layout

### Phase 3: Toolbar (What you'll add)
```
╔════════════════════════════════════════════════╗
║ [Settings] [Capture] [Batch (3)] [Quick Export] │  ← NEW: badge + button
╚════════════════════════════════════════════════╝
```

### Phase 4: Queue Manager Dialog (What you'll create)
```
╔═══════════════════════════════════════════════════════╗
║ Batch Export Queue Manager                        [×] │
╠═══════════════════════════════════════════════════════╣
║  [IMG] │ Viewport Name    │ Timestamp              │   │
║ ────────────────────────────────────────────────────  │
║ ☐ [IMG] │ Perspective     │ 2025-10-28 19:45:00   │ ↕│
║ ☐ [IMG] │ Top             │ 2025-10-28 19:46:15   │  │
║ ☐ [IMG] │ Front           │ 2025-10-28 19:47:30   │  │
║ ☐ [IMG] │ Side            │ 2025-10-28 19:48:45   │ ↕│
╠═══════════════════════════════════════════════════════╣
║ [Remove Selected] [Clear All] [Export All] [Close]   │
╚═══════════════════════════════════════════════════════╝
```

---

## Testing Steps (Manual in Rhino)

### Phase 3 Testing
1. Start Rhino
2. Type: `VesselStudioShowToolbar`
3. Verify toolbar appears
4. Verify badge is hidden (queue empty)
5. Verify "Quick Export Batch" button is disabled
6. ✅ Phase 3 works when:
   - Badge shows when queue has items
   - Badge hides when queue empty
   - Button enables/disables correctly
   - Button click opens dialog

### Phase 4 Testing
1. Follow Phase 3 setup
2. (Manually add an item to queue via code/command)
3. Click "Quick Export Batch" button
4. ✅ Phase 4 works when:
   - Dialog opens with queued items
   - Thumbnails display correctly
   - Checkboxes work
   - Remove/Clear/Close buttons work
   - Dialog closes properly

---

## Checklist: What Must Exist Before You Start

- ✅ QueuedCaptureItem.cs (with GetThumbnail method)
- ✅ CaptureQueue.cs (with Items collection)
- ✅ CaptureQueueService.cs (singleton with events)
- ✅ VesselStudioToolbarPanel.cs (existing toolbar)
- ✅ System.Drawing namespace (for Bitmap, Color, Size)
- ✅ System.Windows.Forms namespace (for controls)

**All present!** ← You can start immediately

---

## Success Indicators

### Build succeeds
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### UI appears correctly
- Badge label shows in toolbar
- "Quick Export Batch" button shows in toolbar
- Dialog has correct size (600x500)
- ListView shows columns
- Buttons are positioned correctly

### Functionality works
- Badge text updates when queue changes
- Button enables/disables correctly
- Dialog opens and closes properly
- Remove/Clear operations work
- No crashes or exceptions

---

## File Locations (Absolute Paths)

```
Create:  C:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin\VesselStudioSimplePlugin\UI\QueueManagerDialog.cs
Modify:  C:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin\VesselStudioSimplePlugin\VesselStudioToolbarPanel.cs

Reference:
  Models:  C:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin\VesselStudioSimplePlugin\Models\
  Services: C:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin\VesselStudioSimplePlugin\Services\
  Project:  C:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin\VesselStudioSimplePlugin.csproj
```

---

## Next Agent

After Agent 4 completes Phase 3 & 4:
- **Agent 3** will complete Phase 3 commands (T018-T024)
- **Agent 2** will implement Phase 5 services (T043-T052)
- Then all can be wired together for complete MVP

No blocking dependencies! Agent 4 can work in parallel.

