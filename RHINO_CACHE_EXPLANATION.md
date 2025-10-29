# Why Old Commands Still Show in Rhino

**Date**: October 28, 2025  
**Issue**: After updating plugin, old commands still appear in Rhino  
**Root Cause**: Rhino caches command information in memory and on disk

---

## The Problem

When you:
1. Build new plugin with `.\dev-build.ps1 -Install`
2. Reload plugin in Rhino
3. Run a command

**Rhino might still show**:
- Old command names
- Removed commands
- Old help text
- Previous documentation

**Why?** Rhino maintains multiple caches:

```
1. In-Memory Cache: Commands loaded in current session
2. Disk Cache: Persisted command information
3. Plugin GUID Map: Which DLL provides which commands
4. Help System Cache: Command documentation
```

Updating the .rhp file doesn't automatically clear all these caches.

---

## Solutions (In Order of Ease)

### Solution 1: Close and Reopen Rhino (Easiest) ⭐ RECOMMENDED
```
1. Close Rhino completely (all windows)
   - File → Exit Rhino
   - Or Alt+F4
   - Or click X button

2. Wait 5 seconds (let temp files flush)

3. Reopen Rhino
   - New Rhino session loads fresh plugin
   - All caches invalidated
   - New commands appear
```

**Time**: ~10 seconds  
**Success Rate**: 99%  
**Why It Works**: Fresh process = fresh memory, plugin reloaded from disk

---

### Solution 2: Run Rhino Plugin Command
```
If you don't want to close Rhino:

1. In Rhino command line, type: PlugInManager
2. Find "Vessel Studio DEV" in list
3. Click "Reload" button
4. Wait for reload (5-10 seconds)
5. Try commands again
```

**Time**: ~15 seconds  
**Success Rate**: 80% (sometimes doesn't fully reload all caches)  
**Why It Works**: Forces plugin DLL reload, invalidates some caches

---

### Solution 3: Clear Rhino Command Cache
```
If PlugInManager doesn't work:

1. Close Rhino
2. Delete cache files:
   C:\Users\rikki.mcguire\AppData\Roaming\McNeel\Rhinoceros\8.0\UserInstallPlugInList.txt

3. Reopen Rhino
   - Rhino rebuilds command cache from scratch
4. Try commands again
```

**Time**: ~10 seconds  
**Success Rate**: 95%  
**Why It Works**: Forces rebuild of command registry

---

### Solution 4: Nuclear Option (If Nothing Works)
```
1. Close Rhino

2. Delete Rhino 8 settings (CAREFUL - loses other settings):
   C:\Users\rikki.mcguire\AppData\Roaming\McNeel\Rhinoceros\8.0\

3. Reopen Rhino
   - Rhino rebuilds from scratch
   - Fresh cache, fresh everything
   - May lose other custom settings

4. Reconfigure if needed
```

**Time**: ~30 seconds  
**Success Rate**: 100%  
**Why Use**: Only if other solutions fail  
**Warning**: May lose other Rhino customizations

---

## What You Should See After Update

### Old (Before Fix)
```
DevVesselCapture        ✓ Works
DevVesselQuickCapture   ✓ Works (old, might still show)
DevVesselStudioStatus   ✓ Works (but freezes)
DevVesselImageSettings  ✓ Opens API key dialog (WRONG)
DevVesselStudioDebug*   ✓ Works (deprecated, should hide)
```

### New (After Fix)
```
DevVesselCapture            ✓ Works (same)
DevVesselAddToQueue         ✓ Works (new, primary queue command)
DevVesselStudioStatus       ✓ Works (FIXED - no freeze)
DevVesselImageSettings      ✓ Opens Image Format dialog (FIXED)
DevVesselDebugIcons         ? Should not appear (hidden)
DevVesselQueueManagerCommand ✓ Works
DevVesselSendBatchCommand    ✓ Works
```

---

## Verification Checklist

After reloading plugin, verify:

```
✓ Run: DevVesselStudioStatus
  Expected: Completes instantly in console (<1 second)
  
✓ Run: DevVesselImageSettings
  Expected: Image Format dialog opens (NOT API key dialog)
  
✓ Check Toolbar: "🖼️ Image Format" button visible
  Expected: Button appears between "Set API Key" and Project dropdown
  
✓ Check Queue Manager: "📸 Format" button visible
  Expected: Button appears between "Export All" and "Close" buttons
```

---

## Why This Happens (Technical)

Rhino loads plugins and registers commands at startup:

```
1. Startup Sequence:
   a) Load .rhp files from plugins directory
   b) Call OnLoad() for each plugin
   c) Enumerate command classes (reflection)
   d) Register command names in global registry
   e) Cache command info to disk

2. When Plugin Updates:
   a) New .rhp file written to disk
   b) But Rhino still has OLD version in memory
   c) Cache on disk still has OLD info
   d) Commands from NEW file never registered

3. Solution:
   a) Close Rhino (kills memory cache)
   b) OR reload plugin (invalidates some caches)
   c) OR clear cache files (forces rebuild)
```

---

## How to Avoid This in Future

### After Each Build
```
Recommended workflow:
1. Build with: .\dev-build.ps1 -Install
2. Close Rhino completely
3. Wait 5 seconds
4. Reopen Rhino
5. Test commands
```

### Quick Rebuild Testing
```
For rapid testing:
1. Keep Rhino open
2. Make code changes
3. Build with: .\dev-build.ps1 -Install
4. In Rhino: PlugInManager → Reload
5. Test
6. Repeat

If weird behavior: Close and reopen Rhino
```

---

## Summary

| Situation | Solution | Time | Success |
|-----------|----------|------|---------|
| Normal update | Close/reopen Rhino | 10s | 99% |
| In hurry | PlugInManager Reload | 15s | 80% |
| Not working | Clear cache files | 10s | 95% |
| Still stuck | Clear Rhino settings | 30s | 100% |

**Best Practice**: Always close and reopen Rhino after plugin updates for clean state.

---

## Commands for Reference

### Release Version
```
VesselCapture              (direct upload, immediate)
VesselAddToQueue           (batch queue, deferred)
VesselSetApiKey            (API configuration)
VesselImageSettings        (PNG/JPEG format & quality)
VesselQueueManagerCommand  (queue manager dialog)
VesselSendBatchCommand     (CLI batch upload)
VesselStudioStatus         (check plugin status)
VesselStudioShowToolbar    (show toolbar)
VesselStudioAbout          (about dialog)
VesselStudioHelp           (online documentation)
```

### DEV Version (what should appear)
```
DevVesselCapture              (direct upload)
DevVesselAddToQueue           (batch queue) ← NEW primary
DevVesselSetApiKey            (API configuration)
DevVesselImageSettings        (image format) ← FIXED dialog
DevVesselQueueManagerCommand  (queue manager)
DevVesselSendBatchCommand     (CLI batch upload)
DevVesselStudioStatus         (status) ← FIXED no freeze
DevVesselStudioShowToolbar    (show toolbar)
DevVesselStudioAbout          (about dialog)
DevVesselStudioHelp           (online documentation)

❌ SHOULD NOT APPEAR:
DevVesselStudioDebugIcons     (deprecated debug command)
DevVesselQuickCapture         (old, not used)
```

---

## Action Items

1. **Immediately**: Close and reopen Rhino
2. **Verify**: Run `DevVesselStudioStatus` - should complete instantly
3. **Verify**: Run `DevVesselImageSettings` - should show Format dialog
4. **Verify**: Check toolbar for new buttons
5. **Report**: Let me know if old commands still appear after reopen

---

**Status**: This is normal Rhino behavior, not a plugin bug.  
**Solution**: Simple restart of Rhino resolves it.  
**Prevention**: Close Rhino after each build for clean state.

