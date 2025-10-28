# Command Reference Guide
## Vessel Studio Rhino Plugin - All Commands

**Last Updated**: October 28, 2025  
**Build Status**: ✅ 0 errors, 0 warnings

---

## Command Summary

### Release Commands (Production)
```
VesselSetApiKey               Configure API authentication
VesselImageSettings           Image format (PNG/JPEG) & quality
VesselCapture                 Direct capture & upload
VesselAddToQueue              Add viewport to batch queue
VesselQueueManagerCommand     Open queue manager dialog
VesselSendBatchCommand        CLI batch upload
VesselStudioShowToolbar       Show/hide toolbar
VesselStudioStatus            Check plugin status
VesselStudioAbout             About dialog
VesselStudioHelp              Online documentation
```

### DEV Commands (Testing)
```
DevVesselSetApiKey            [DEV] Configure API authentication
DevVesselImageSettings        [DEV] Image format & quality
DevVesselCapture              [DEV] Direct capture & upload
DevVesselAddToQueue           [DEV] Add viewport to batch queue
DevVesselQueueManagerCommand  [DEV] Open queue manager dialog
DevVesselSendBatchCommand     [DEV] CLI batch upload
DevVesselStudioShowToolbar    [DEV] Show/hide toolbar
DevVesselStudioStatus         [DEV] Check plugin status
DevVesselStudioAbout          [DEV] About dialog
DevVesselStudioHelp           [DEV] Online documentation
```

---

## Detailed Command Reference

### Setup Commands

#### **VesselSetApiKey** / **DevVesselSetApiKey**
- **Purpose**: Configure or update API authentication key
- **Dialog**: API Key Configuration
- **Inputs**: 
  - Paste API key from Vessel Studio account
  - Validate key with API
- **Output**: API key saved to settings.json
- **UI Access**: Toolbar → "⚙️ Set API Key" button
- **Tip**: Must run this first before any captures

---

#### **VesselImageSettings** / **DevVesselImageSettings**
- **Purpose**: Configure image format and quality
- **Dialog**: Image Format Settings (400x280)
- **Options**:
  - Format: PNG (Lossless) or JPEG (Compressed)
  - JPEG Quality: 1-100 slider (95 recommended)
- **Output**: Format and quality saved to settings.json
- **UI Access**: 
  - Toolbar → "🖼️ Image Format" button
  - Queue Manager → "📸 Format" button
- **Recommendations**:
  - PNG: Better quality, larger files (~2-3MB)
  - JPEG 95: High quality, smaller files (~500-800KB)
  - JPEG <75: Not recommended, visible compression

---

### Capture Commands

#### **VesselCapture** / **DevVesselCapture**
- **Purpose**: Capture viewport and immediately upload to Vessel Studio
- **Workflow**:
  1. Image Name dialog appears
  2. Enter custom name for image
  3. Captures active viewport (automatically)
  4. Uploads to Vessel Studio in background
  5. Console shows upload progress/result
- **Prerequisites**: 
  - API key configured (`VesselSetApiKey`)
  - Project selected from toolbar dropdown
- **Result**: Image appears in Vessel Studio immediately
- **UI Access**: Toolbar → "📷 Capture Screenshot" button
- **Use Case**: Quick single-image uploads
- **Time**: ~2-5 seconds for upload (background)

---

#### **VesselAddToQueue** / **DevVesselAddToQueue**
- **Purpose**: Capture viewport and add to batch queue for later upload
- **Workflow**:
  1. Captures active viewport (no dialog)
  2. Adds to queue with timestamp
  3. Updates batch badge "📦 Batch (N)"
  4. Returns control to Rhino immediately
- **Prerequisites**: 
  - API key configured (`VesselSetApiKey`)
  - Optional: Project selected (can select later)
- **Result**: Image queued for batch upload
- **UI Access**: Toolbar → "➕ Add to Batch Queue" button
- **Use Case**: Collecting 5-20 viewports to upload as batch
- **Time**: <1 second (instant)
- **Queue Limit**: 20 items maximum
- **Note**: No network activity until you export

---

### Queue Management

#### **VesselQueueManagerCommand** / **DevVesselQueueManagerCommand**
- **Purpose**: View, manage, and export queued captures
- **Dialog**: Batch Export Queue Manager (600x500)
- **Features**:
  - ListView with thumbnails, viewport names, timestamps
  - Select/deselect individual captures (checkboxes)
  - Actions:
    - "Remove Selected" - Remove checked items (>5 confirmation)
    - "Clear All" - Clear entire queue (count confirmation)
    - "Export All" - Upload all queue items with progress bar
    - "📸 Format" - Open image format settings
    - "Close" - Close dialog
- **UI Access**: 
  - Toolbar → "📤 Quick Export Batch" button
  - Or queue manager badge
- **Progress**: Real-time progress bar and percentage during upload
- **Result**: 
  - Success: Dialog closes, queue clears
  - Failure: Error shown, queue preserved for retry
  - Partial: Warning shown, queue shows failed items

---

#### **VesselSendBatchCommand** / **DevVesselSendBatchCommand**
- **Purpose**: Command-line alternative for batch upload
- **Input**: Type command name in Rhino command line
- **Output**: Console progress and result messages
- **Features**:
  - Progress reporting: "N/M uploaded"
  - Error details (top 3 errors)
  - Final summary
- **Use Case**: Automated scripts, batch processing
- **Note**: Simpler than queue dialog, console-only feedback

---

### Status & Information

#### **VesselStudioStatus** / **DevVesselStudioStatus**
- **Purpose**: Check plugin status and API connection
- **Display**: Console output
- **Information Shown**:
  - Authentication status (API key set? ✓/✗)
  - API availability (ready to use? ✓/✗)
  - Available commands (list)
- **Speed**: <1 second (instant, no network test)
- **Prerequisites**: None (informational only)
- **Tip**: Run this if something seems wrong

---

#### **VesselStudioShowToolbar** / **DevVesselStudioShowToolbar**
- **Purpose**: Show or toggle the Vessel Studio toolbar
- **Result**: 
  - If hidden: Shows toolbar panel
  - If shown: Toggles visibility
- **Toolbar Contents**:
  - Status indicator
  - "⚙️ Set API Key" button
  - "🖼️ Image Format" button
  - Project selector dropdown
  - "📷 Capture Screenshot" button
  - "➕ Add to Batch Queue" button
  - "📤 Quick Export Batch" button
  - Information card
  - Links to docs and about
- **Persistence**: Toolbar position saved

---

#### **VesselStudioAbout** / **DevVesselStudioAbout**
- **Purpose**: Display plugin version and information
- **Content**:
  - Plugin name and version
  - Build info
  - Feature summary
  - Credits
- **UI Access**: Toolbar → "ℹ️ About Plugin" link

---

#### **VesselStudioHelp** / **DevVesselStudioHelp**
- **Purpose**: Open online documentation
- **Action**: Opens https://vesselstudio.io/docs/rhino-plugin in browser
- **Fallback**: If browser fails, displays command list in console
- **UI Access**: Toolbar → "📖 Documentation" link

---

## Typical Workflows

### Workflow 1: Quick Single Image
1. Run: `VesselCapture`
2. Enter image name in dialog
3. Wait for upload confirmation
4. Image appears in Studio immediately

**Time**: 5-10 seconds  
**Best for**: Single viewport review

---

### Workflow 2: Batch Multiple Views
1. Run: `VesselAddToQueue` × 5 (click toolbar button 5 times)
2. See badge: "📦 Batch (5)"
3. Review in queue: Click "📤 Quick Export Batch" button
4. Verify thumbnails and names in dialog
5. Click "Export All" 
6. Watch progress bar fill
7. See success message
8. Continue modeling

**Time**: <30 seconds for 5 views  
**Best for**: Comparing multiple viewports, client reviews

---

### Workflow 3: Adjust Image Quality
1. Run: `VesselImageSettings`
2. Select: JPEG format
3. Set quality: 85 (slider)
4. Click: OK
5. Run: `VesselAddToQueue`
6. See console: "📸 Compressed as JPEG (quality: 85%)"
7. Upload and check quality in Studio

**Time**: 2 minutes  
**Best for**: Finding right quality/size balance

---

### Workflow 4: Change Formats Mid-Session
1. Initially: PNG format (lossless, current setting)
2. After 10 captures: "File sizes too large"
3. Run: `VesselImageSettings`
4. Change: JPEG 95 quality
5. Continue: Next captures use JPEG automatically
6. Export: All (PNG older ones + JPEG newer ones mixed)

**Time**: <1 minute to change  
**Best for**: Optimizing on-the-fly

---

## Command Statistics

```
Total Commands: 20 (10 Release + 10 Dev)
UI Access: 80% (via toolbar buttons)
CLI Access: 20% (direct commands)

By Category:
  Setup:              2 commands
  Capture:            2 commands
  Queue Management:   2 commands
  Status/Info:        4 commands
  UI:                 1 command

By Frequency:
  Critical (must use): VesselSetApiKey
  Common (daily):      VesselAddToQueue, VesselQueueManagerCommand
  Occasional (weekly): VesselImageSettings, VesselCapture
  Rare (troubleshoot): VesselStudioStatus
```

---

## Keyboard Shortcuts

Currently no keyboard shortcuts are defined.  
Access commands via:
- Toolbar buttons (fastest)
- Command line typing (most flexible)
- Help → Online documentation

---

## Tips & Tricks

### Speed Tips
- Use toolbar buttons instead of typing commands
- Batch multiple captures then export once
- Don't close Rhino until batch uploads complete

### Quality Tips
- PNG for archival/client work (lossless)
- JPEG 90+ for client reviews (acceptable quality)
- JPEG 75-89 for preview/draft (visible compression)

### Troubleshooting Tips
- If upload hangs: Check internet connection
- If API key fails: Verify key is copied correctly
- If commands don't work: Run `VesselStudioStatus` to check
- If toolbar missing: Run `VesselStudioShowToolbar` to restore

### DEV vs RELEASE Tips
- **DEV**: Test new features, try settings without affecting production
- **RELEASE**: Stable version for client work
- Can run both simultaneously (different settings, different commands)

---

## Settings Files

### Release Settings
```
Location: %APPDATA%\VesselStudio\settings.json
Contains:
  - ApiKey (encrypted recommended)
  - LastProjectId
  - LastProjectName
  - ImageFormat (png/jpeg)
  - JpegQuality (1-100)
```

### DEV Settings
```
Location: %APPDATA%\VesselStudioDEV\settings.json
Same structure as Release
Separate instance (doesn't affect Release)
```

---

## Summary

- **20 commands** for different tasks
- **80% accessible via toolbar** for convenience
- **Dev prefix system** for safe testing
- **Flexible workflows** for different use cases
- **Real-time feedback** on all operations

**Status**: ✅ All commands working and documented

