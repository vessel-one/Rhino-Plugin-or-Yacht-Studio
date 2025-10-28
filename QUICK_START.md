# Quick Start Guide - After Plugin Update

**Issue**: "Why do I still see old commands in Rhino?"

**Answer**: Rhino caches commands. You need to reload them.

---

## ⚡ Quick Fix (90 seconds)

### Option 1: Restart Rhino (Easiest)
```
1. Close Rhino (click X or Alt+F4)
2. Wait 5 seconds
3. Open Rhino again
4. Commands updated! ✓
```

### Option 2: Reload Plugin (Don't Close Rhino)
```
In Rhino command line:
1. Type: PlugInManager
2. Find: "Vessel Studio DEV"
3. Click: Reload button
4. Wait: 5-10 seconds
5. Commands updated! ✓
```

---

## ✓ Verify It Worked

After reloading, test:

```
Command: DevVesselStudioStatus
Expected: Responds instantly (no freeze) ✓

Command: DevVesselImageSettings
Expected: Image Format dialog opens (NOT API key dialog) ✓

Toolbar: Look for "🖼️ Image Format" button
Expected: Between "Set API Key" and Project dropdown ✓

Queue Manager: Open and look for "📸 Format" button
Expected: Between "Export All" and "Close" buttons ✓
```

---

## 📋 What Commands Should Appear

### After Update (What You'll See)
```
✓ DevVesselCapture              Single viewport upload
✓ DevVesselAddToQueue           Batch queue (primary now)
✓ DevVesselSetApiKey            Configure API key
✓ DevVesselImageSettings        Image format & quality (FIXED)
✓ DevVesselQueueManagerCommand  Manage queue
✓ DevVesselSendBatchCommand     CLI batch upload
✓ DevVesselStudioStatus         Check status (FIXED)
✓ DevVesselStudioShowToolbar    Show toolbar
✓ DevVesselStudioAbout          About dialog
✓ DevVesselStudioHelp           Help/docs
```

### If You Still See These (Refresh Didn't Work)
```
✗ DevVesselQuickCapture         OLD - use DevVesselCapture
✗ DevVesselStudioDebugIcons     OLD - ignore or close Rhino
```

**Fix**: Close and reopen Rhino to fully refresh.

---

## 🔍 Troubleshooting

### Old Commands Still Show
→ Close Rhino and reopen (full restart clears all caches)

### Toolbar Buttons Not Visible
→ Run: `DevVesselStudioShowToolbar` in command line

### DevVesselImageSettings Opens API Key Dialog
→ Close Rhino and reopen (old plugin version cached in memory)

### DevVesselStudioStatus Freezes/Hangs
→ Close Rhino and reopen (command not reloaded)

---

## 🎯 What Changed (Summary)

| Before | After | Status |
|--------|-------|--------|
| DevVesselStudioStatus freezes | Responds instantly | ✅ FIXED |
| DevVesselImageSettings → API dialog | → Format dialog | ✅ FIXED |
| No image format access | Toolbar button + queue button | ✅ ADDED |
| No quality control | PNG/JPEG 1-100 slider | ✅ ADDED |
| No tooltips | Helpful tooltips on buttons | ✅ ADDED |

---

## 📚 For More Information

- **BUG_FIXES_SUMMARY.md** - Technical details of what was fixed
- **COMMAND_REFERENCE_GUIDE.md** - All 20 commands documented
- **SESSION_COMPLETE.md** - Complete session summary

---

## ✨ One-Minute Summary

```
Plugin Updated? YES → Close Rhino → Reopen Rhino → Commands Fixed!

That's it!
```

