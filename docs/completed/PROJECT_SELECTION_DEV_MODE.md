# Project Selection & DEV Mode Implementation Summary

## What Was Fixed

### 1. **Project Selection Persistence Issue** ✅
**Problem:** When changing projects in the capture dialog, the `LastProjectId` wasn't being saved, so quick capture kept using the first project.

**Solution:** 
- Moved project selection to the toolbar panel
- Project selection now saves immediately when changed (via `OnProjectChanged` event)
- Toolbar dropdown shows current selected project at all times

### 2. **Improved Workflow** ✅
**Old Workflow:**
1. Run `VesselCapture`
2. Wait for projects to load
3. Select project
4. Enter image name
5. Click Capture

**New Workflow:**
1. Select project from toolbar dropdown (stays selected)
2. Run `VesselCapture` → Enter name → Capture
3. Run `VesselQuickCapture` → Auto-named → Instant capture

**Benefits:**
- Project selection happens once, not every capture
- Refresh button to reload projects when needed
- Quick capture is truly quick (no dialogs)
- Current project always visible in toolbar

### 3. **DEV vs RELEASE Mode** ✅
**Problem:** Need to test new features without uninstalling production version.

**Solution:** Complete dual-mode system that allows both to run simultaneously:

| Feature | DEV | RELEASE |
|---------|-----|---------|
| Build Command | `.\dev-build.ps1` | `.\quick-build.ps1` |
| Plugin GUID | Different | Different |
| Command Prefix | `Dev` (DevVesselCapture) | None (VesselCapture) |
| Settings Folder | `VesselStudioDEV\` | `VesselStudio\` |
| Can Coexist | ✅ Yes | ✅ Yes |

## File Changes

### Core Plugin Files Modified
1. **VesselStudioSimplePlugin.csproj**
   - Added `DEV` constant to Debug builds
   - Separate DefineConstants for Debug/Release

2. **Properties/AssemblyInfo.cs**
   - Different GUIDs for DEV vs RELEASE
   - Different titles and descriptions

3. **VesselStudioSettings.cs**
   - Separate storage paths for DEV/RELEASE

4. **All Command Files**
   - Conditional command names (Dev prefix in DEBUG)
   - `VesselCaptureCommand.cs`
   - `VesselSetApiKeyCommand.cs`
   - `VesselStudioStatusCommand.cs`
   - `VesselStudioToolbar.cs`
   - `VesselStudioMenu.cs`
   - `VesselStudioDebugCommand.cs`

### UI Improvements
5. **VesselStudioToolbarPanel.cs**
   - Added project dropdown ComboBox
   - Added refresh button for reloading projects
   - Projects load automatically when panel opens
   - OnProjectChanged saves selection immediately
   - Different panel GUID for DEV vs RELEASE
   - Status shows current selected project

6. **VesselCaptureCommand.cs**
   - Removed project selection dialog
   - Now shows simple image name dialog
   - Uses project from toolbar selection
   - Both capture commands check for selected project

### New Files
7. **BuildConfig.cs**
   - Helper class for DEV/RELEASE detection
   - Constants for command prefixes and display names

8. **dev-build.ps1**
   - Script to build DEV version
   - Optional installation to Rhino
   - Clear instructions for usage

9. **docs/guides/DEV_VS_RELEASE.md**
   - Comprehensive guide to dual-mode system
   - Usage examples
   - Troubleshooting tips

## Usage Guide

### For Development
```powershell
# Build and install DEV version
.\dev-build.ps1 -Install

# Start Rhino (both DEV and RELEASE can run together)
# Use DEV commands:
DevVesselStudioShowToolbar
DevVesselSetApiKey
DevVesselCapture
DevVesselQuickCapture
```

### For Testing
1. Keep RELEASE version installed (from package manager)
2. Install DEV version with `.\dev-build.ps1 -Install`
3. Both plugins run independently
4. DEV uses separate settings (no conflicts)
5. Test features in DEV before releasing

### For Release
```powershell
# Build production version
.\quick-build.ps1

# Create and publish package
.\create-package.ps1
.\publish-package.ps1
```

## Benefits Summary

✅ **No More Uninstalling** - Run DEV and RELEASE together
✅ **Fast Testing** - Quick install with `.\dev-build.ps1 -Install`
✅ **Separate Settings** - DEV changes don't affect production
✅ **Clear Commands** - "Dev" prefix makes it obvious which version
✅ **Better UX** - Project selection in toolbar, not dialog
✅ **Persistent Selection** - Project stays selected between captures
✅ **Quick Refresh** - Reload projects with refresh button
✅ **Easy Workflow** - Select once, capture many times

## Next Steps

1. **Test DEV version in Rhino:**
   ```powershell
   .\dev-build.ps1 -Install
   ```

2. **Verify both modes work:**
   - Install DEV version
   - Keep RELEASE version (or install from package)
   - Both should load and work independently

3. **Test new project selection workflow:**
   - Open toolbar with `DevVesselStudioShowToolbar`
   - Click refresh to load projects
   - Select a project from dropdown
   - Run `DevVesselCapture` (should only ask for name)
   - Run `DevVesselQuickCapture` (should upload instantly)
   - Change project in dropdown
   - Verify quick capture uses new project

4. **When features are ready:**
   ```powershell
   # Build release version
   .\build.ps1
   
   # Increment version if needed
   .\update-version.ps1 -NewVersion "1.2.0"
   
   # Create and publish
   .\create-package.ps1
   .\publish-package.ps1
   ```

## Technical Implementation

### Conditional Compilation
```csharp
// In .csproj
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
  <DefineConstants>DEBUG;TRACE;DEV</DefineConstants>
</PropertyGroup>

// In C# code
#if DEV
    public override string EnglishName => "DevVesselCapture";
#else
    public override string EnglishName => "VesselCapture";
#endif
```

### Separate GUIDs
- Plugin: `D1E2F3A4-...` (DEV) vs `A1B2C3D4-...` (RELEASE)
- Panel: `D5E6F7A8-...` (DEV) vs `A5B6C7D8-...` (RELEASE)

This ensures complete isolation between versions.

## Branch Status

Branch: `update-plugin-project-selection`
Status: Ready for testing
Commits: All changes committed

To merge to main:
```powershell
git checkout main
git merge update-plugin-project-selection
git push origin main
```
