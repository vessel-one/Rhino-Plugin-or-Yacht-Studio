# ⚠️ CRITICAL: RhinoCommon.dll Conflict Issue

## 🚨 Important Discovery

**RhinoCommon.dll MUST NOT be in the same folder as the plugin .rhp file!**

If RhinoCommon.dll (or Eto.dll, Rhino.UI.dll) is present alongside VesselStudioSimplePlugin.rhp, **Rhino will NOT update the plugin** even when you drag & drop a new version.

---

## Why This Happens

1. Rhino has its own built-in RhinoCommon.dll
2. If it finds another copy in the plugins folder, it causes conflicts
3. The conflict prevents proper plugin loading/updating
4. Plugin appears to not update even though file is replaced

---

## ✅ Solution

### Method 1: Drag & Drop (Recommended)

When you **drag the .rhp file into Rhino**, it:
- ✅ Automatically copies ONLY the .rhp file
- ✅ Leaves out RhinoCommon.dll and other Rhino DLLs
- ✅ Updates work correctly

**Do NOT drag the entire bin\Release\net48 folder!**

### Method 2: Manual Copy (Use With Caution)

If copying manually:

```powershell
# ✅ CORRECT: Copy only the .rhp file
Copy-Item "VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp" "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\"

# ❌ WRONG: Don't copy the whole folder or RhinoCommon.dll
Copy-Item "VesselStudioSimplePlugin\bin\Release\net48\*" "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\"
```

### Method 3: Use Updated cleanup-plugin.ps1

The cleanup script now automatically removes conflicting DLLs:

```powershell
.\cleanup-plugin.ps1
```

It will:
1. Install the .rhp file
2. Check for RhinoCommon.dll in the plugins folder
3. Remove it if found
4. Remove Eto.dll if found

---

## 🔍 How to Check for the Issue

### Symptoms
- Plugin appears installed but doesn't update
- Old commands still work, new commands don't appear
- "Unknown command" for new features
- File timestamp shows old date even after "updating"

### Verification

```powershell
# Check what's in your plugins folder
Get-ChildItem "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\" | Select-Object Name, Length

# Look for these files (they should NOT be there):
# - RhinoCommon.dll
# - Eto.dll  
# - Rhino.UI.dll
# - Ed.Eto.dll
```

### Fix It

```powershell
# Remove conflicting DLLs
Remove-Item "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\RhinoCommon.dll" -ErrorAction SilentlyContinue
Remove-Item "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\Eto.dll" -ErrorAction SilentlyContinue
Remove-Item "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\Rhino.UI.dll" -ErrorAction SilentlyContinue
Remove-Item "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\Ed.Eto.dll" -ErrorAction SilentlyContinue

# Then restart Rhino
```

---

## 📋 What Files Should Be in Plugins Folder

### ✅ CORRECT

```
C:\Users\<you>\AppData\Roaming\McNeel\Rhinoceros\8.0\Plug-ins\
├── VesselStudioSimplePlugin.rhp        ← Only this!
└── (other plugins...)
```

### ❌ WRONG

```
C:\Users\<you>\AppData\Roaming\McNeel\Rhinoceros\8.0\Plug-ins\
├── VesselStudioSimplePlugin.rhp        
├── RhinoCommon.dll                     ← Delete this!
├── Eto.dll                             ← Delete this!
├── Rhino.UI.dll                        ← Delete this!
├── Ed.Eto.dll                          ← Delete this!
└── Newtonsoft.Json.dll                 ← This is OK (third-party library)
```

---

## 🔧 Development Workflow

### Safe Update Process

1. **Make code changes**

2. **Build the plugin:**
   ```powershell
   cd VesselStudioSimplePlugin
   dotnet build -c Release
   ```

3. **Close Rhino completely**

4. **Install using ONE of these methods:**

   **Option A: Drag & Drop (Easiest)**
   - Open Rhino
   - Drag `VesselStudioSimplePlugin.rhp` into viewport
   - Click "Yes" to install
   - Restart Rhino

   **Option B: Use Script (Automated)**
   ```powershell
   .\cleanup-plugin.ps1
   ```

   **Option C: Manual Copy (Advanced)**
   ```powershell
   # Copy ONLY the .rhp file
   Copy-Item "VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp" "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\" -Force
   
   # Remove any conflicting DLLs
   Remove-Item "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\RhinoCommon.dll" -Force -ErrorAction SilentlyContinue
   
   # Start Rhino
   Start-Process "C:\Program Files\Rhino 8\System\Rhino.exe"
   ```

---

## 🎓 Technical Explanation

### Why RhinoCommon is Special

**RhinoCommon.dll** is Rhino's SDK - it provides:
- Geometry classes (Point3d, Line, Curve, etc.)
- RhinoDoc, RhinoApp, etc.
- Command base classes
- UI components

**Rhino loads its OWN version** from:
```
C:\Program Files\Rhino 8\System\RhinoCommon.dll
```

### The Conflict

When you have ANOTHER copy in the plugins folder:
1. Rhino's assembly loader gets confused
2. Type conflicts occur (same class in two DLLs)
3. Plugin fails to load properly
4. Updates are ignored

### NuGet Package Reference

In the .csproj file:
```xml
<PackageReference Include="RhinoCommon" Version="8.1.23325.13001" PrivateAssets="all" />
```

The `PrivateAssets="all"` means:
- ✅ RhinoCommon is used for compilation
- ✅ NOT copied to output folder
- ✅ Rhino uses its own version at runtime

**However**, if you manually copy build output, you might accidentally include it!

---

## 📊 File Size Reference

| File | Size | Should Copy? |
|------|------|--------------|
| VesselStudioSimplePlugin.rhp | ~39 KB | ✅ YES |
| Newtonsoft.Json.dll | ~700 KB | ✅ YES (third-party) |
| RhinoCommon.dll | ~3.8 MB | ❌ NO (Rhino provides) |
| Eto.dll | ~753 KB | ❌ NO (Rhino provides) |
| Rhino.UI.dll | ~75 MB | ❌ NO (Rhino provides) |
| Ed.Eto.dll | ~62 KB | ❌ NO (Rhino provides) |

---

## 🐛 Troubleshooting

### "Plugin won't update"

1. Check for RhinoCommon.dll in plugins folder
2. Delete it
3. Close ALL Rhino processes (check Task Manager)
4. Re-install plugin
5. Start Rhino fresh

### "New commands not found"

1. Check plugin file size - should be ~39 KB with all features
2. If it's ~20 KB, you have old version
3. Follow update process above

### "Type load exception" errors

This is the classic symptom of RhinoCommon.dll conflict!

Fix:
```powershell
Remove-Item "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\RhinoCommon.dll" -Force
```

---

## ✅ Quick Checklist

Before testing plugin updates:

- [ ] Close Rhino completely
- [ ] Build produces .rhp file (check timestamp)
- [ ] Use drag & drop OR cleanup script
- [ ] Do NOT copy entire bin folder
- [ ] Check plugins folder has ONLY .rhp file
- [ ] Start Rhino fresh
- [ ] Test new commands

---

## 💡 Pro Tips

1. **Always use drag & drop for updates** - safest method
2. **Check Task Manager** for lingering Rhino processes
3. **Watch file timestamps** to confirm updates
4. **Keep cleanup-plugin.ps1 handy** for rapid testing
5. **Never manually copy the entire bin folder**

---

## 📚 Related Files

- `cleanup-plugin.ps1` - Automated update script (now removes conflicting DLLs)
- `update-plugin.ps1` - Alternative update script
- `PLUGIN_UPDATE_GUIDE.md` - General update instructions
- `TESTING_GUIDE.md` - Testing procedures

---

**Remember: Drag & drop the .rhp file = it just works! ✨**
