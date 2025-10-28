# Phase 5 Group 1 - Quick Testing Guide

## What Was Implemented

**Phase 5, Group 1 (T043-T052)**: Batch Upload Service for User Story 3  
**Status**: ✅ Complete and Ready for Testing  
**Build**: VesselStudioSimplePlugin.rhp (99.5 KB)

## Quick Feature Overview

### Core Features
1. **BatchUploadService** - Orchestrates upload of queued captures
2. **Quick Export Button** - One-click batch upload from toolbar
3. **VesselSendBatch Command** - CLI alternative to button

### What Each Feature Does

**BatchUploadService**:
- Uploads all queued captures in sequence
- Generates descriptive filenames: `ProjectName_ViewportName_001.png`
- Reports progress for each item
- Handles failures gracefully (continues on individual errors)
- Clears queue only on complete success
- Preserves queue on any failure for retry

**Quick Export Button**:
- Blue button "📤 Quick Export Batch" in toolbar
- Automatically enabled when queue has items
- Shows MessageBox with results (success count or error details)
- Progress logged to console

**VesselSendBatch Command**:
- Type in Rhino command line: `VesselSendBatch`
- Same behavior as button
- Results logged to console
- Dev version: `DevVesselSendBatch`

## Testing Workflow

### Setup
1. Load Vessel Studio Rhino Plugin
2. Set API key via "⚙️ Set API Key" button or `VesselSetApiKey` command
3. Select a project from the dropdown

### Test Case 1: Happy Path (All Upload Successfully)

**Steps:**
1. Take 3-5 viewport captures using "Add to Batch Queue" button
   - Change viewport between captures (e.g., Perspective, Top, Front)
2. Watch badge update: "📦 Batch (3)" after each add
3. Click "📤 Quick Export Batch" button
4. Watch console output (progress updates)
5. Get success dialog showing "3 captures uploaded"

**Expected Results:**
- ✅ All items upload successfully
- ✅ Console shows: `[BatchUpload] ✅ Batch upload complete: 3 items uploaded`
- ✅ Success dialog appears with count and duration
- ✅ Badge updates to "📦 Batch (0)" after upload
- ✅ Quick Export button becomes disabled (queue empty)

### Test Case 2: Network Failure (Complete Failure)

**Steps:**
1. Queue 3-5 captures
2. Disconnect network (or use VPN disconnect)
3. Click "Quick Export Batch" button
4. Wait for timeout
5. Get error dialog
6. Reconnect network and retry

**Expected Results:**
- ❌ Upload fails with network error
- ❌ Error dialog shows: "❌ Batch upload failed" with error details
- ❌ Message says "Queue preserved for retry"
- ✅ Badge still shows "📦 Batch (3)" (items preserved)
- ✅ Quick Export button is enabled
- ✅ Click "Quick Export Batch" again and it succeeds

### Test Case 3: Empty Queue

**Steps:**
1. Don't queue any captures
2. Look at Quick Export button

**Expected Results:**
- ✅ Button is disabled (grayed out)
- ✅ Cannot click or click has no effect
- ❌ Badge is not visible

### Test Case 4: No Project Selected

**Steps:**
1. Queue 3 captures
2. Clear project selection from dropdown
3. Click "Quick Export Batch" button

**Expected Results:**
- ❌ Dialog shows: "Please select a project first"
- ❌ No upload attempted
- ✅ Queue is preserved

### Test Case 5: Command-Line Alternative

**Steps:**
1. Queue 3 captures
2. In Rhino command prompt, type: `VesselSendBatch`
3. Press Enter
4. Watch console output

**Expected Results:**
- ✅ Same upload behavior as button
- ✅ Progress printed to console
- ✅ Queue clears on success

### Test Case 6: Filename Verification

**Steps:**
1. Queue captures from different viewports (e.g., Perspective, Top, Front)
2. Use "Quick Export Batch"
3. Check Vessel Studio web app to see uploaded filenames

**Expected Results:**
- ✅ Filenames follow pattern: `Project_ViewportName_###.png`
- ✅ Examples:
  - `YachtA_Perspective_001.png`
  - `YachtA_Top_002.png`
  - `YachtA_Front_003.png`
- ✅ Sequence numbers are zero-padded (001, 002, not 1, 2)
- ✅ Project name matches selected project
- ✅ Viewport names match viewport (Perspective, Top, Front, etc.)

## Console Output Examples

### Successful Upload
```
[BatchUpload] Starting batch upload: 3 items to project 550e8400-e29b-41d4-a716-446655440000
[BatchUpload] Uploading (1/3): YachtA_Perspective_001.png
[BatchUpload] ✓ Success: YachtA_Perspective_001.png
[QuickExportBatch] Progress: 1/3 completed - 33%
[BatchUpload] Uploading (2/3): YachtA_Top_002.png
[BatchUpload] ✓ Success: YachtA_Top_002.png
[QuickExportBatch] Progress: 2/3 completed - 66%
[BatchUpload] Uploading (3/3): YachtA_Front_003.png
[BatchUpload] ✓ Success: YachtA_Front_003.png
[QuickExportBatch] Progress: 3/3 completed - 100%
[BatchUpload] ✅ Batch upload complete: All items uploaded successfully
[BatchUpload] Duration: 2847ms
```

### Partial Failure
```
[BatchUpload] Starting batch upload: 3 items to project 550e8400-e29b-41d4-a716-446655440000
[BatchUpload] Uploading (1/3): YachtA_Perspective_001.png
[BatchUpload] ✓ Success: YachtA_Perspective_001.png
[QuickExportBatch] Progress: 1/3 completed - 33%
[BatchUpload] Uploading (2/3): YachtA_Top_002.png
[BatchUpload] ✗ Failed: YachtA_Top_002.png - Connection timeout
[QuickExportBatch] Progress: 2/3 completed - 66%
[BatchUpload] Uploading (3/3): YachtA_Front_003.png
[BatchUpload] ✓ Success: YachtA_Front_003.png
[QuickExportBatch] Progress: 3/3 completed - 100%
[BatchUpload] ⚠ Batch upload incomplete: 2 successful, 1 failed
[BatchUpload] Queue preserved for retry
[BatchUpload] Duration: 1523ms
```

## Button States

### Button Disabled (Grayed Out)
- Queue is empty
- API key not configured
- No project selected

### Button Enabled (Blue, Clickable)
- Queue has 1+ items
- API key configured
- Project selected

## Common Issues & Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| "API key not set" | API key not configured | Click "⚙️ Set API Key" button |
| "No project selected" | Project dropdown empty/not selected | Select project from dropdown |
| "Queue is empty" | No captures in queue | Add captures using "Add to Batch Queue" |
| Button is disabled | One of above OR queue was cleared | Check prerequisites |
| Upload hangs | Network issue or very large files | Wait or cancel and retry |
| Files show generic names | Viewport name not set properly | Check that captures have viewport info |

## Files Modified

1. **BatchUploadService.cs** (NEW)
   - Core batch upload logic
   - Filename generation and sanitization
   - Error handling and progress reporting

2. **VesselSendBatchCommand.cs** (NEW)
   - Rhino command for batch upload
   - CLI alternative to button

3. **VesselStudioToolbarPanel.cs** (MODIFIED)
   - Added "Quick Export Batch" button
   - Added enable/disable logic
   - Added upload handler

## Integration Status

✅ **Ready to Test**
- Build successful
- All dependencies satisfied
- No compilation errors
- Plugin loadable in Rhino

⏳ **Future Enhancement**
- QueueManagerDialog (Phase 4) will add popup with visual queue management
- Task 13 (T059-T061) will add "Export All" button in that dialog

## Questions?

Refer to:
- `PHASE_5_GROUP_1_COMPLETE.md` - Full technical details
- `PHASE_5_GROUP_1_CHECKLIST.md` - Implementation checklist
- Source code XML comments - Detailed code documentation

---

**Ready to load in Rhino and test!**
