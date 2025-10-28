# Agent 4 Prompt: UI Components (Phase 3 + Phase 4)

## Your Role
You are **Agent 4**, responsible for implementing all UI components (toolbar badge, popup dialog).

## Context Files to Read FIRST
1. `specs/003-queued-batch-capture/spec.md` - US1, US2, US4 acceptance criteria
2. `specs/003-queued-batch-capture/research.md` - Q4 popup dialog design with UI mockups
3. `specs/003-queued-batch-capture/quickstart.md` - Phase 5 QueueManagerDialog implementation
4. `specs/003-queued-batch-capture/tasks.md` - Task T025-T042 details
5. `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs` - Existing toolbar pattern

## Your Assigned Tasks (Two Phases)

### PHASE GATE: Wait for Phase 2 Completion
**Do NOT start** until orchestrator confirms Phase 2 foundational work is complete.

---

## Phase 3 (US1): Toolbar Badge Button - T025-T028

### T025 [US1] Add badge count label to toolbar
**File**: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`

**Requirements**:
- Add Label control: `badgeLabel`
- Text format: "Batch ({count})"
- Position: Next to existing buttons
- Initially hidden (Visible = false)
- Font: Regular, readable size
- Background: Light gray or transparent

### T026 [US1] Add "Quick Export Batch" button to toolbar
**File**: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`

**Requirements**:
- Add Button control: `quickExportButton`
- Text: "Quick Export Batch"
- Position: Next to badge label
- Initially disabled (Enabled = false) - only enable when queue has items
- Click handler: Opens QueueManagerDialog (placeholder for now)

### T027 [US1] Style badge with rounded corners and background
**File**: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`

**Requirements**:
- Set badge label BackColor to light gray (#E0E0E0)
- Add padding: 4px all sides
- Optional: Use Paint event for rounded corners (low priority)
- Compact size to not consume excessive toolbar space

### T028 [US1] Show/hide badge and button based on queue
**File**: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`

**Requirements**:
- Update badge visibility:
  - Visible when CaptureQueueService.Instance.Count > 0
  - Hidden when count = 0
- Update button state:
  - Enabled when count > 0
  - Disabled when count = 0
- Subscribe to ItemAdded, ItemRemoved, QueueCleared events
- Update handler: `UpdateQueueUI()` method

---

## Phase 4 (US2): Queue Manager Dialog - T029-T042

### T029 [US2] Create QueueManagerDialog form
**File**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Requirements**:
- Inherit from System.Windows.Forms.Form
- Size: 600x500 pixels
- Title: "Batch Export Queue Manager"
- FormBorderStyle: FixedDialog (not resizable)
- StartPosition: CenterParent
- Modal dialog (ShowDialog())

### T030 [US2] Add ListView with columns
**File**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Requirements**:
- ListView control: `queueListView`
- View: Details
- Columns: [Checkbox] | Thumbnail (120px) | Viewport Name (200px) | Timestamp (150px)
- CheckBoxes = true
- FullRowSelect = true
- Dock: Fill (takes most of dialog space)
- ImageList for thumbnails: 120x90 pixels

### T031 [US2] Add action buttons panel
**File**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Requirements**:
- Panel at bottom: Height 50px, Dock = Bottom
- Buttons (left to right):
  - "Remove Selected" - Remove checked items
  - "Clear All" - Remove all items
  - "Export All" - Start batch upload
  - "Close" - Close dialog
- Button size: 100x30, spaced 10px apart

### T032 [US2] Implement LoadQueueItems() to populate ListView
**File**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Requirements**:
```csharp
private void LoadQueueItems()
{
    queueListView.Items.Clear();
    
    foreach (var item in CaptureQueueService.Instance.GetAllItems())
    {
        var listItem = new ListViewItem();
        listItem.Tag = item; // Store reference
        
        // Add thumbnail to ImageList
        var thumbnail = item.GetThumbnail(120, 90);
        string imageKey = item.Id.ToString();
        queueListView.SmallImageList.Images.Add(imageKey, thumbnail);
        listItem.ImageKey = imageKey;
        
        // Add sub-items
        listItem.SubItems.Add(item.ViewportName);
        listItem.SubItems.Add(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        
        queueListView.Items.Add(listItem);
    }
}
```

### T033 [US2] Wire up "Quick Export Batch" button
**File**: `VesselStudioSimplePlugin/VesselStudioToolbarPanel.cs`

**Requirements**:
- Button click handler: `OnQuickExportClick`
- Logic:
  ```csharp
  using (var dialog = new QueueManagerDialog())
  {
      dialog.ShowDialog();
  }
  ```
- Refresh badge after dialog closes (queue may have changed)

### T034 [US2] Implement Remove Selected button
**File**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Requirements**:
- Button click handler: `OnRemoveSelectedClick`
- Logic:
  1. Get all checked items from ListView
  2. For each checked item, get QueuedCaptureItem from Tag
  3. Call CaptureQueueService.Instance.RemoveItem(item.Id)
  4. Remove from ListView.Items
- Show confirmation if many items selected (>5)

### T035 [US2] Implement Clear All button
**File**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Requirements**:
- Button click handler: `OnClearAllClick`
- Logic:
  1. Show confirmation dialog: "Remove all {count} items?"
  2. If Yes: CaptureQueueService.Instance.Clear()
  3. Clear ListView.Items
- MessageBox for confirmation

### T036 [US2] Implement Close button
**File**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Requirements**:
- Button click handler: `OnCloseClick`
- Logic: `this.Close()`
- OR set button DialogResult = Cancel and handle in form

### T037 [US2] Add checkboxes to ListView items
**File**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Requirements**:
- Already covered by T030 (CheckBoxes = true)
- Ensure users can check/uncheck items
- "Select All" / "Deselect All" optional enhancement

### T038 [US2] Display thumbnails in ListView
**File**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Requirements**:
- Already covered by T032 LoadQueueItems()
- Use item.GetThumbnail(120, 90)
- Add to ImageList with unique key (item.Id)
- Assign ImageKey to ListViewItem

### T039 [US2] Display viewport names in ListView
**File**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Requirements**:
- Already covered by T032 LoadQueueItems()
- SubItem[1] = item.ViewportName

### T040 [US2] Display timestamps in ListView
**File**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Requirements**:
- Already covered by T032 LoadQueueItems()
- SubItem[2] = item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")

### T041 [US2] Handle window resize (optional enhancement)
**File**: `VesselStudioSimplePlugin/UI/QueueManagerDialog.cs`

**Requirements**:
- FormBorderStyle = FixedDialog (not resizable)
- If making resizable: Set MinimumSize, handle column width adjustments
- **SKIP for MVP** - keep fixed size

### T042 [US2] Test dialog open time <500ms
**File**: Manual testing task

**Requirements**:
- Measure time from button click to dialog visible
- Test with 10 items in queue (thumbnails cached)
- Must be <500ms per SC-008
- Optimize LoadQueueItems() if needed (async thumbnail loading?)

---

## Code Templates

### VesselStudioToolbarPanel.cs (Phase 3 additions)
```csharp
// Add to class fields:
private Label badgeLabel;
private Button quickExportButton;

// In InitializeControls() or constructor:
private void InitializeQueueUI()
{
    // Badge label
    badgeLabel = new Label
    {
        Text = "Batch (0)",
        AutoSize = true,
        BackColor = ColorTranslator.FromHtml("#E0E0E0"),
        Padding = new Padding(4),
        Visible = false
    };
    this.Controls.Add(badgeLabel);
    
    // Quick export button
    quickExportButton = new Button
    {
        Text = "Quick Export Batch",
        AutoSize = true,
        Enabled = false
    };
    quickExportButton.Click += OnQuickExportClick;
    this.Controls.Add(quickExportButton);
    
    // Subscribe to queue events
    CaptureQueueService.Instance.ItemAdded += (s, e) => UpdateQueueUI();
    CaptureQueueService.Instance.ItemRemoved += (s, e) => UpdateQueueUI();
    CaptureQueueService.Instance.QueueCleared += (s, e) => UpdateQueueUI();
    
    UpdateQueueUI();
}

private void UpdateQueueUI()
{
    int count = CaptureQueueService.Instance.Count;
    
    if (count > 0)
    {
        badgeLabel.Text = $"Batch ({count})";
        badgeLabel.Visible = true;
        quickExportButton.Enabled = true;
    }
    else
    {
        badgeLabel.Visible = false;
        quickExportButton.Enabled = false;
    }
}

private void OnQuickExportClick(object sender, EventArgs e)
{
    using (var dialog = new QueueManagerDialog())
    {
        dialog.ShowDialog();
    }
    UpdateQueueUI(); // Refresh after dialog closes
}
```

### QueueManagerDialog.cs (Phase 4)
```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using VesselStudioSimplePlugin.Services;

namespace VesselStudioSimplePlugin.UI
{
    public class QueueManagerDialog : Form
    {
        private ListView queueListView;
        private ImageList thumbnailImageList;
        private Button removeSelectedButton;
        private Button clearAllButton;
        private Button exportAllButton;
        private Button closeButton;

        public QueueManagerDialog()
        {
            InitializeComponent();
            LoadQueueItems();
        }

        private void InitializeComponent()
        {
            this.Text = "Batch Export Queue Manager";
            this.Size = new Size(600, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // ImageList for thumbnails
            thumbnailImageList = new ImageList
            {
                ImageSize = new Size(120, 90),
                ColorDepth = ColorDepth.Depth32Bit
            };

            // ListView
            queueListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                SmallImageList = thumbnailImageList
            };
            queueListView.Columns.Add("Thumbnail", 120);
            queueListView.Columns.Add("Viewport Name", 200);
            queueListView.Columns.Add("Timestamp", 150);
            this.Controls.Add(queueListView);

            // Button panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };
            this.Controls.Add(buttonPanel);

            // Buttons
            removeSelectedButton = new Button
            {
                Text = "Remove Selected",
                Size = new Size(120, 30),
                Location = new Point(10, 10)
            };
            removeSelectedButton.Click += OnRemoveSelectedClick;
            buttonPanel.Controls.Add(removeSelectedButton);

            clearAllButton = new Button
            {
                Text = "Clear All",
                Size = new Size(100, 30),
                Location = new Point(140, 10)
            };
            clearAllButton.Click += OnClearAllClick;
            buttonPanel.Controls.Add(clearAllButton);

            exportAllButton = new Button
            {
                Text = "Export All",
                Size = new Size(100, 30),
                Location = new Point(250, 10)
            };
            exportAllButton.Click += OnExportAllClick;
            buttonPanel.Controls.Add(exportAllButton);

            closeButton = new Button
            {
                Text = "Close",
                Size = new Size(100, 30),
                Location = new Point(360, 10),
                DialogResult = DialogResult.Cancel
            };
            this.CancelButton = closeButton;
            buttonPanel.Controls.Add(closeButton);
        }

        private void LoadQueueItems()
        {
            queueListView.Items.Clear();
            thumbnailImageList.Images.Clear();

            foreach (var item in CaptureQueueService.Instance.GetAllItems())
            {
                var listItem = new ListViewItem();
                listItem.Tag = item;

                // Add thumbnail
                var thumbnail = item.GetThumbnail(120, 90);
                string imageKey = item.Id.ToString();
                thumbnailImageList.Images.Add(imageKey, thumbnail);
                listItem.ImageKey = imageKey;

                // Add columns
                listItem.SubItems.Add(item.ViewportName);
                listItem.SubItems.Add(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

                queueListView.Items.Add(listItem);
            }
        }

        private void OnRemoveSelectedClick(object sender, EventArgs e)
        {
            var checkedItems = new System.Collections.Generic.List<ListViewItem>();
            foreach (ListViewItem item in queueListView.CheckedItems)
            {
                checkedItems.Add(item);
            }

            if (checkedItems.Count > 5)
            {
                var result = MessageBox.Show(
                    $"Remove {checkedItems.Count} items?",
                    "Confirm Remove",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;
            }

            foreach (var listItem in checkedItems)
            {
                var queueItem = (QueuedCaptureItem)listItem.Tag;
                CaptureQueueService.Instance.RemoveItem(queueItem.Id);
                queueListView.Items.Remove(listItem);
            }
        }

        private void OnClearAllClick(object sender, EventArgs e)
        {
            int count = CaptureQueueService.Instance.Count;
            if (count == 0) return;

            var result = MessageBox.Show(
                $"Remove all {count} items from queue?",
                "Confirm Clear All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                CaptureQueueService.Instance.Clear();
                LoadQueueItems();
            }
        }

        private void OnExportAllClick(object sender, EventArgs e)
        {
            // Placeholder - will be implemented in Phase 5 (US3)
            MessageBox.Show("Batch upload not yet implemented", "Info");
        }
    }
}
```

## Success Criteria

### Phase 3 (T025-T028):
- [ ] Badge label added to toolbar with "Batch (N)" format
- [ ] Quick Export button added to toolbar
- [ ] Badge visibility toggles based on queue count
- [ ] Button enabled/disabled based on queue count
- [ ] Badge styled with light gray background and padding
- [ ] UpdateQueueUI() subscribed to all queue events
- [ ] No compiler errors

### Phase 4 (T029-T042):
- [ ] QueueManagerDialog created with 600x500 fixed size
- [ ] ListView with 3 columns (Thumbnail, Viewport, Timestamp)
- [ ] Checkboxes enabled on ListView
- [ ] 4 action buttons added (Remove Selected, Clear All, Export All, Close)
- [ ] LoadQueueItems() populates ListView with thumbnails
- [ ] Remove Selected removes checked items
- [ ] Clear All removes all items with confirmation
- [ ] Close button closes dialog
- [ ] Thumbnails display correctly (120x90)
- [ ] Dialog opens from Quick Export button
- [ ] Dialog open time <500ms (test with 10 items)
- [ ] No compiler errors
- [ ] No memory leaks (ImageList and thumbnails disposed)

## Coordination

### After Phase 3 (T025-T028):
1. Update `agent-coordination.json`:
   - Set `phases.phase_3_us1.parallel_groups.group_2_toolbar_ui.agent = "agent_4"`
   - Add T025-T028 to `phases.phase_3_us1.completed_tasks`
   - Add T025-T028 to `agents.agent_4.completed_tasks`
2. Notify orchestrator: "Agent 4 completed Phase 3 Group 2 (Toolbar UI)"

### After Phase 4 (T029-T042):
1. Update `agent-coordination.json`:
   - Set `phases.phase_4_us2.parallel_groups.group_1_queue_dialog.agent = "agent_4"`
   - Add T029-T042 to `phases.phase_4_us2.completed_tasks`
   - Add T029-T042 to `agents.agent_4.completed_tasks`
   - Update `agents.agent_4.status = "complete"` if no more tasks assigned
2. Notify orchestrator: "Agent 4 completed Phase 4 (QueueManagerDialog)"

## Next Steps After This
You may be assigned:
- **Phase 5 (US3)**: Progress dialog for batch upload (T057-T061)
- Work in parallel with Agent 3 (commands) and Agent 2 (services)

## Notes
- **Phase Gate**: Wait for Phase 2 completion before starting Phase 3
- **Parallel Work**: Phase 3 runs parallel with Agent 3 (commands), Phase 4 runs parallel with Agent 3 working on other US2 tasks
- **Export All Button**: Leave as placeholder in Phase 4, will be wired up in Phase 5
- **Memory Management**: Ensure thumbnails are disposed when dialog closes
- **Testing**: Test dialog performance with 10, 15, 20 items to hit SC-008 (<500ms open time)
