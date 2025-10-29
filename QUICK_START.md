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

Command: DevVesselSettings
Expected: Tabbed dialog opens with "API Key" and "Image Format" tabs ✓

Command: DevVesselSetApiKey
Expected: Same unified settings dialog opens ✓

Toolbar: Settings button
Expected: Opens unified settings dialog with both tabs ✓
```

---

## 📋 What Commands Should Appear

### After Update (What You'll See)
```
✓ DevVesselCapture              Single viewport upload
✓ DevVesselAddToQueue           Batch queue (primary now)
✓ DevVesselSetApiKey            Configure API key (opens unified settings)
✓ DevVesselSettings             Unified settings dialog (API Key + Image Format)
✓ DevVesselQueueManagerCommand  Manage queue
✓ DevVesselSendBatchCommand     CLI batch upload
✓ DevVesselStudioStatus         Check status (FIXED)
✓ DevVesselStudioShowToolbar    Show toolbar
✓ DevVesselStudioAbout          About dialog
✓ DevVesselStudioHelp           Help/docs
```

### Deprecated Commands (Removed)
```
✗ DevVesselQuickCapture         REMOVED - use DevVesselCapture or DevVesselAddToQueue
✗ DevVesselStudioDebugIcons     REMOVED - no longer needed
✗ DevVesselImageSettings        REMOVED - use DevVesselSettings instead
```

**Note**: If you still see old commands after reloading, close and reopen Rhino completely.

---

## 🔍 Troubleshooting

### Old Commands Still Show
→ Close Rhino and reopen (full restart clears all caches)

### Toolbar Buttons Not Visible
→ Run: `DevVesselStudioShowToolbar` in command line

### Settings Dialog Missing Image Format Tab
→ Close Rhino and reopen (old plugin version cached in memory)

### DevVesselStudioStatus Freezes/Hangs
→ Close Rhino and reopen (command not reloaded)

---

## 🎯 What Changed (Summary)

| Before | After | Status |
|--------|-------|--------|
| DevVesselStudioStatus freezes | Responds instantly | ✅ FIXED |
| Multiple settings dialogs | Unified tabbed settings | ✅ FIXED |
| No image format access | Settings tab + toolbar button | ✅ ADDED |
| No quality control | PNG/JPEG 1-100 slider | ✅ ADDED |
| Deprecated commands | Cleaned up & removed | ✅ FIXED |

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

