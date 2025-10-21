# 🔄 Rhino Plugin Update & Uninstall Guide

## Plugin Update Strategy

### ✅ Drag & Drop Installation

**Good News:** When you drag a `.rhp` file into Rhino, it:
- ✅ Automatically overwrites the old version (same GUID)
- ✅ Updates the plugin in place
- ✅ No manual deletion needed

**However:** Rhino caches loaded plugins, so you need to restart Rhino to see changes.

---

## 🔄 Updating Your Plugin

### Method 1: Drag & Drop (Easiest)

1. **Close Rhino** (important!)
2. **Rebuild the plugin:**
   ```powershell
   cd "VesselStudioSimplePlugin"
   dotnet build -c Release
   ```
3. **Open Rhino**
4. **Drag the new .rhp** into Rhino viewport:
   ```
   VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp
   ```
5. Click **"Yes"** to install/update
6. **Restart Rhino** for changes to take effect

### Method 2: Manual Copy (For Development)

If you're actively developing and testing frequently:

```powershell
# 1. Close Rhino first!

# 2. Copy new version to plugins folder
Copy-Item "VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp" "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\" -Force

# 3. Open Rhino
```

**Note:** Always close Rhino before copying, or you'll get a file lock error.

---

## 🗑️ Completely Uninstalling Plugin

### If You Need a Clean Slate

**Step 1:** Open Rhino and type:
```
PlugInManager
```

**Step 2:** Find "Vessel Studio Simple Plugin" in the list

**Step 3:** Select it and click **"Unload"** or **"Disable"**

**Step 4:** Close Rhino

**Step 5:** Delete the plugin files:
```powershell
# Check what's installed
Get-ChildItem "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\*Vessel*"

# Delete the plugin
Remove-Item "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\VesselStudioSimplePlugin.rhp" -Force
```

**Step 6:** Restart Rhino

---

## 🔍 Check Current Installation

### Find Installed Version

**Method 1:** In Rhino, type:
```
PlugInManager
```
Look for "Vessel Studio Simple Plugin" - shows version and status

**Method 2:** Check file system:
```powershell
Get-ChildItem "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\*Vessel*" | Format-List Name, Length, LastWriteTime, VersionInfo
```

**Method 3:** Run the status command (if plugin is loaded):
```
VesselStudioStatus
```

---

## 🔧 Development Workflow

### For Frequent Updates

**Quick Rebuild & Test Loop:**

```powershell
# 1. Make code changes

# 2. Close Rhino (or it won't update)

# 3. Build
cd "VesselStudioSimplePlugin"
dotnet build -c Release

# 4. Copy to plugins folder
Copy-Item "bin\Release\net48\VesselStudioSimplePlugin.rhp" "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\" -Force

# 5. Start Rhino
& "C:\Program Files\Rhino 8\System\Rhino.exe"

# 6. Test changes
```

**Or create a PowerShell script** to automate this:

```powershell
# rebuild-and-test.ps1
param(
    [switch]$SkipBuild
)

Write-Host "🔨 Vessel Studio Plugin - Rebuild & Test" -ForegroundColor Cyan

# Close Rhino if running
$rhino = Get-Process -Name "Rhino" -ErrorAction SilentlyContinue
if ($rhino) {
    Write-Host "⚠️  Closing Rhino..." -ForegroundColor Yellow
    Stop-Process -Name "Rhino" -Force
    Start-Sleep -Seconds 2
}

if (-not $SkipBuild) {
    Write-Host "🔨 Building plugin..." -ForegroundColor Cyan
    Push-Location "VesselStudioSimplePlugin"
    dotnet build -c Release
    $buildResult = $LASTEXITCODE
    Pop-Location
    
    if ($buildResult -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build succeeded" -ForegroundColor Green
}

Write-Host "📦 Copying plugin..." -ForegroundColor Cyan
Copy-Item "VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp" "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\" -Force

Write-Host "🚀 Starting Rhino..." -ForegroundColor Cyan
Start-Process "C:\Program Files\Rhino 8\System\Rhino.exe"

Write-Host "✅ Done! Test the plugin in Rhino" -ForegroundColor Green
Write-Host "   Type: VesselStudioShowToolbar" -ForegroundColor Gray
```

**Usage:**
```powershell
# Rebuild and test
.\rebuild-and-test.ps1

# Just copy and test (skip build)
.\rebuild-and-test.ps1 -SkipBuild
```

---

## 🐛 Troubleshooting

### "Plugin Already Loaded" Error

**Problem:** Rhino has the plugin locked

**Solution:**
```powershell
# Force close Rhino
Stop-Process -Name "Rhino" -Force

# Wait a moment
Start-Sleep -Seconds 2

# Try copying again
Copy-Item "VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp" "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\" -Force
```

### "Access Denied" Error

**Problem:** File is locked or needs permissions

**Solution:**
1. Close Rhino completely
2. Check Task Manager for lingering Rhino processes
3. Kill any Rhino.exe processes
4. Try again

### Plugin Doesn't Update

**Problem:** Old version still loading

**Solution:**
1. **Uninstall completely** (see above)
2. Delete from plugins folder
3. Restart Rhino
4. Install fresh

### Two Versions of Plugin Show Up

**Problem:** Plugin installed in multiple locations

**Solution:**
```powershell
# Find all installations
Get-ChildItem "C:\Program Files\Rhino 8\Plug-ins\*Vessel*" -Recurse
Get-ChildItem "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\*Vessel*"

# Delete all copies
Remove-Item "C:\Program Files\Rhino 8\Plug-ins\VesselStudio*" -Recurse -Force
Remove-Item "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\VesselStudio*" -Force

# Install fresh in one location
```

---

## 📍 Plugin Installation Locations

Rhino looks for plugins in multiple places:

### 1. User Plugins (Recommended)
```
%APPDATA%\McNeel\Rhinoceros\8.0\Plug-ins\
```
**Pro:** Easy to update, user-specific  
**Con:** Separate for each Windows user

### 2. System Plugins
```
C:\Program Files\Rhino 8\Plug-ins\
```
**Pro:** Available to all users  
**Con:** Requires admin rights, harder to update

### 3. Development Location (Debugging)
```
<Your Project>\bin\Release\net48\
```
**Pro:** Test without installing  
**Con:** Need to configure Rhino debugger

**For testing:** Use User Plugins folder (#1)

---

## 🎯 Best Practices

### During Development

1. **Always close Rhino** before rebuilding
2. **Use the User Plugins folder** for testing
3. **Keep only one copy** of the plugin
4. **Check PlugInManager** to verify version loaded
5. **Watch for GUID conflicts** if testing multiple plugins

### For Distribution

1. **Build in Release mode** (not Debug)
2. **Test fresh install** on clean Rhino
3. **Document uninstall steps** for users
4. **Version your .rhp file** (e.g., VesselStudio-v1.0.rhp)
5. **Provide installer** for non-technical users

### Version Management

**Option 1:** Update version in `.csproj`:
```xml
<Version>1.1.0</Version>
```

**Option 2:** Add version to manifest:
```
VesselStudioSimplePlugin.manifest
```

**Option 3:** Add to AssemblyInfo:
```csharp
[assembly: AssemblyVersion("1.1.0")]
```

---

## ✅ Quick Reference

### Check Installation
```powershell
Get-ChildItem "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\*Vessel*"
```

### Update Plugin (Rhino Closed)
```powershell
Copy-Item "VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp" "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\" -Force
```

### Uninstall Plugin
```powershell
Remove-Item "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\VesselStudioSimplePlugin.rhp" -Force
```

### Find All Installations
```powershell
Get-ChildItem "C:\", "$env:APPDATA" -Recurse -Filter "*VesselStudio*.rhp" -ErrorAction SilentlyContinue
```

---

## 🎓 Understanding Plugin GUIDs

Each Rhino plugin has a unique GUID in its code:

```csharp
[System.Runtime.InteropServices.Guid("12345678-1234-1234-1234-123456789012")]
public class VesselStudioSimplePlugin : PlugIn
```

**This GUID:**
- ✅ Identifies the plugin uniquely
- ✅ Allows Rhino to update the same plugin
- ✅ Prevents installing duplicate versions
- ❌ Means two plugins with same GUID = conflict

**When updating:**
- Keep the same GUID = Rhino updates in place
- Change the GUID = Rhino treats as new plugin

**Our plugin GUID:**
```
{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
```

---

## 🚀 Summary

### For First Install
1. Build the plugin
2. Drag .rhp into Rhino
3. Restart Rhino

### For Updates
1. Close Rhino
2. Rebuild plugin
3. Copy to plugins folder (or drag & drop)
4. Open Rhino

### For Clean Uninstall
1. Close Rhino
2. Use PlugInManager to unload
3. Delete from plugins folder
4. Restart Rhino

**Current Status:** No old version detected - you're good to install fresh! 🎉
