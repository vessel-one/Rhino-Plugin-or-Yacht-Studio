# 🚀 Vessel Studio Rhino Plugin - Installation & Testing Guide

## ✅ Build Complete!

The plugin has been successfully built with the new UI features:
- **Dockable Toolbar Panel** with large buttons and status display
- **Commands** accessible via command line
- Smart validation and error handling

---

## 📦 Installation Steps

### Option 1: Drag & Drop (Recommended)

1. **Open Rhino 8**

2. **Drag the .rhp file** into the Rhino viewport:
   ```
   C:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin\VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp
   ```

3. **Click "Yes"** when prompted to install the plugin

4. **Restart Rhino** for full initialization

### Option 2: Manual Installation

1. **Copy the plugin file** to Rhino's plugin folder:
   ```powershell
   Copy-Item "C:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin\VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp" "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\"
   ```

2. **Open Rhino** and type:
   ```
   PlugInManager
   ```

3. **Find "Vessel Studio Simple Plugin"** and verify it's loaded

---

## 🎯 Testing the UI

### 1. Open the Toolbar Panel

**Method 1:** Command line
```
VesselStudioShowToolbar
```

**Method 2:** Rhino menu
```
Panels → Vessel Studio
```

You should see:
```
┌─────────────────────────────────┐
│  Vessel Studio                  │
├─────────────────────────────────┤
│  ┌─────────────────────────┐   │
│  │ ❌ Not configured        │   │
│  │ Set your API key to     │   │
│  │ get started             │   │
│  └─────────────────────────┘   │
│                                 │
│  ┌─────────────────────────┐   │
│  │   ⚙ Set API Key         │   │ ← Blue
│  └─────────────────────────┘   │
│                                 │
│  ┌─────────────────────────┐   │
│  │   📷 Capture Screenshot  │   │ ← Green (disabled)
│  └─────────────────────────┘   │
│                                 │
│  ┌─────────────────────────┐   │
│  │   ⚡ Quick Capture       │   │ ← Orange (disabled)
│  └─────────────────────────┘   │
└─────────────────────────────────┘
```

### 2. Dock the Panel (Optional)

- **Drag the panel** to the left or right edge of Rhino
- It will snap into a docked position
- You can resize it by dragging the edge

### 3. Test Status Display

The panel shows different statuses:

**Initially:**
- ❌ Not configured
- Capture buttons are disabled

**After setting API key:**
- ✓ API key configured
- Capture button enabled
- Quick Capture still disabled (needs first project)

**After first capture:**
- ✓ Connected
- Shows last project name
- All buttons enabled

---

## 🧪 Testing Workflow

### Step 1: Verify Plugin Loaded

```
VesselStudioStatus
```

Expected output:
```
Vessel Studio Status
====================
API Key: Not set
Connected: False
```

### Step 2: Test Toolbar Panel

1. Run: `VesselStudioShowToolbar`
2. Verify panel appears
3. Check status shows "Not configured"
4. Verify capture buttons are disabled (grayed out)

### Step 3: Test Set API Key Button

**Note:** Backend API isn't implemented yet, so validation will fail. That's expected!

1. Click **⚙ Set API Key** button
2. Enter a test key: `vsk_live_test123456789012345678901234`
3. You'll get an error (normal - backend not ready)

**OR test via command:**
```
VesselSetApiKey
```

### Step 4: Test Capture Button States

After attempting to set API key:
- Click **📷 Capture Screenshot**
- Should show error: "Please set your API key first"
- Click **⚡ Quick Capture**
- Should show error: "Please set your API key first"

### Step 5: Test Commands Still Work

All commands work without the panel:
```
VesselSetApiKey
VesselCapture
VesselQuickCapture
VesselStudioStatus
VesselStudioHelp
```

### Step 6: Test Panel Refresh

The panel should automatically update when you:
- Set an API key via command
- Complete a capture via command
- The status should refresh when panel becomes visible

---

## 🎨 UI Features to Verify

### Button Colors
- **Set API Key:** Blue (#4682B4)
- **Capture Screenshot:** Green (#4CAF50)
- **Quick Capture:** Orange (#FF9800)

### Status Indicators
- ❌ Red text = Not configured or error
- ✓ Blue text = API key configured, ready
- ✓ Green text = Connected with project

### Smart Enabling
- Buttons disable/enable based on state
- Helpful error messages guide users
- No crashes from clicking disabled features

### Documentation Link
- Click **📖 View Documentation** link
- Should open: https://vessel.one/docs/rhino-plugin
- (Currently a placeholder URL)

---

## 🐛 Known Limitations (Expected)

### Backend Not Implemented Yet
❌ API key validation will fail (backend /api/rhino/validate doesn't exist yet)
❌ Project list will be empty (backend /api/rhino/projects doesn't exist yet)
❌ Upload will fail (backend /api/rhino/projects/[id]/upload doesn't exist yet)

✅ UI should still work correctly (buttons, panel, status)
✅ Dialogs should appear
✅ Error messages should be helpful
✅ No crashes

### Menu Bar Not Implemented
- RhinoCommon 8 doesn't support programmatic menu creation
- Use toolbar panel instead (better UX anyway!)
- Or use commands directly

---

## 📋 Test Checklist

Copy this checklist for testing:

```
Plugin Installation:
[ ] Plugin loads in Rhino 8 without errors
[ ] VesselStudioStatus command works
[ ] Plugin info shows in PlugInManager

Toolbar Panel:
[ ] VesselStudioShowToolbar opens the panel
[ ] Panel can be docked left/right
[ ] Panel can float as window
[ ] Panel icon appears in Panels menu

Status Display:
[ ] Shows "Not configured" initially
[ ] Red ❌ icon appears
[ ] Capture buttons are disabled (grayed)
[ ] Status text wraps properly

Set API Key Button:
[ ] Blue button appears
[ ] Button is always enabled
[ ] Clicking opens input dialog
[ ] Can enter text
[ ] Dialog validates format (vsk_ prefix)

Capture Screenshot Button:
[ ] Green button appears
[ ] Initially disabled
[ ] Shows error if no API key
[ ] Stays disabled until configured

Quick Capture Button:
[ ] Orange button appears
[ ] Initially disabled
[ ] Shows error if no API key
[ ] Shows error if no project selected

Documentation Link:
[ ] Blue clickable link
[ ] Hover shows hand cursor
[ ] Click attempts to open URL

Commands Work:
[ ] VesselSetApiKey - opens input dialog
[ ] VesselCapture - shows appropriate error
[ ] VesselQuickCapture - shows appropriate error
[ ] VesselStudioStatus - shows current status
[ ] VesselStudioHelp - shows help info

No Crashes:
[ ] Clicking disabled buttons doesn't crash
[ ] Closing panel doesn't crash
[ ] Opening/closing panel multiple times works
[ ] Unloading plugin doesn't crash
```

---

## 🔧 Troubleshooting

### Plugin Won't Load

**Error:** "Unable to load"
```powershell
# Check dependencies are in same folder
ls "C:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin\VesselStudioSimplePlugin\bin\Release\net48\"
```

Should see:
- VesselStudioSimplePlugin.rhp
- VesselStudioSimplePlugin.dll
- Newtonsoft.Json.dll

**Solution:** Copy all .dll files with the .rhp file

### Panel Doesn't Show

**Try:**
1. Type: `VesselStudioShowToolbar`
2. Check: `Panels` menu → `Vessel Studio`
3. Restart Rhino

### Buttons Don't Work

**Check:**
- Is this a "Not configured" error? (Normal!)
- Backend isn't implemented yet
- UI should still function without crashes

### Status Not Updating

**Try:**
- Close and reopen the panel
- Run a command then check panel
- Status updates when panel becomes visible

---

## 🚀 Next Steps

After UI testing is complete:

1. **Backend Implementation** (Task 5)
   - Implement API endpoints in Vessel One
   - Follow `docs/BACKEND_IMPLEMENTATION.md`

2. **End-to-End Testing** (Task 8)
   - Test with real API keys
   - Verify uploads work
   - Check real-time browser updates

3. **UI Polish** (Future)
   - Add tooltips to buttons
   - Add thumbnail preview
   - Add upload progress bar

---

## 💡 Tips for Testing

- **Keep the panel open** while working - it's designed for this!
- **Test error cases** - click buttons without API key
- **Test button states** - verify enable/disable logic
- **Test docking** - try different panel positions
- **Check console** - look for error messages in Rhino console
- **Test commands** - verify everything works without panel too

---

## 📸 What to Look For

### Good Signs ✅
- Panel appears with clean layout
- Buttons have proper colors
- Status updates show correct icons
- Error messages are helpful
- No crashes when clicking around
- Panel can dock smoothly

### Bad Signs ❌
- Panel has rendering issues
- Buttons overlap or cut off
- Status text is unreadable
- Crashes when clicking buttons
- Panel won't open/close
- Memory leaks (Rhino slows down)

---

## 📝 Report Issues

If you find bugs, note:
1. What you clicked
2. What happened
3. What you expected
4. Any error messages in Rhino command line

---

**Happy Testing! 🎉**

The UI is designed to be intuitive for non-technical users while providing power-user features (Quick Capture) for advanced workflows.
