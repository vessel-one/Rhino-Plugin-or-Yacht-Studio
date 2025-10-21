# Quickstart: Rhino Package Manager Distribution

**Feature**: Rhino Package Manager Distribution  
**Branch**: feature-packagemanager  
**Status**: Specification Complete

## What This Feature Does

Enables distribution of the Vessel Studio Rhino Plugin through Rhino's official Package Manager, allowing users to discover and install the plugin directly from within Rhino without manual file downloads.

## Why This Matters

**Current State**: Users must:
1. Download .rhp file manually
2. Find the correct Rhino plugins folder
3. Copy files to the right location
4. Restart Rhino
5. Hope it worked

**Future State**: Users will:
1. Open Rhino's Package Manager
2. Search for "Vessel Studio"
3. Click "Install"
4. Done ✅

**Benefits**:
- 🔍 **Discoverability**: Plugin appears in Rhino's searchable package catalog
- ⚡ **One-click install**: No file management or folder navigation
- 🔄 **Automatic updates**: Users get notified when new versions are available
- 📦 **Professional distribution**: Matches user expectations from modern software
- 🛡️ **Reduced support**: Zero "where do I install this?" tickets

## Key Concepts

### Yak Package Manager
Rhino's built-in package distribution system (introduced in Rhino 7). Similar to npm (Node.js), pip (Python), or NuGet (.NET) but for Rhino plugins.

### .yak Package File
A compressed archive containing:
- Plugin binary (.rhp)
- Dependencies (Newtonsoft.Json.dll)
- Metadata (manifest.yml)
- Icon and documentation

### Distribution Tag
Indicates compatibility: `rh8_0-win` means "Rhino 8.0+ on Windows"

### Manifest
YAML file with package metadata (name, version, description, authors, etc.)

## Quick Commands

```powershell
# Authenticate (one-time setup)
& "C:\Program Files\Rhino 8\System\Yak.exe" login

# Generate manifest template
& "C:\Program Files\Rhino 8\System\Yak.exe" spec

# Build package
& "C:\Program Files\Rhino 8\System\Yak.exe" build

# Test on test server (wiped nightly)
& "C:\Program Files\Rhino 8\System\Yak.exe" push vesselstudio-1.0.0-rh8_0-win.yak --source https://test.yak.rhino3d.com

# Publish to production
& "C:\Program Files\Rhino 8\System\Yak.exe" push vesselstudio-1.0.0-rh8_0-win.yak

# Search for package
& "C:\Program Files\Rhino 8\System\Yak.exe" search vesselstudio
```

## Workflow Overview

1. **Build Plugin** → Release build creates VesselStudioSimplePlugin.rhp + dependencies
2. **Prepare Distribution** → Create dist/ folder with .rhp, manifest.yml, icon.png
3. **Generate Manifest** → Run `yak spec` and customize the YAML
4. **Build Package** → Run `yak build` to create .yak file
5. **Test Push** → Upload to test server, verify searchability
6. **Production Push** → Upload to production server, goes live within 1 hour
7. **Verify** → Search in Rhino's Package Manager GUI, test installation

## File Requirements

### Required Files
- ✅ `VesselStudioSimplePlugin.rhp` (plugin binary)
- ✅ `Newtonsoft.Json.dll` (dependency, not provided by Rhino)
- ✅ `manifest.yml` (package metadata)
- ✅ `icon.png` (32x32 or 48x48 for Package Manager UI)

### Optional Files
- 📄 `README.md` (installation/usage instructions)
- 📄 `LICENSE.txt` (software license)

### Excluded Files
- ❌ `RhinoCommon.dll` (provided by Rhino)
- ❌ `Rhino.UI.dll` (provided by Rhino)
- ❌ `Eto.dll` (provided by Rhino)
- ❌ `.pdb` files (debug symbols)

## Version Strategy

### Semantic Versioning
- **1.0.0** → Initial release
- **1.0.1** → Bug fixes
- **1.1.0** → New features (backwards compatible)
- **2.0.0** → Breaking changes

### Version Immutability
⚠️ **CRITICAL**: Once a version is pushed, it **cannot be overwritten or deleted**. Always increment version numbers for new releases.

If you push a bad version:
- Use `yak yank` to unlist it (prevents new installs)
- Push a fixed version with incremented number
- Cannot remove from users who already installed

## Distribution Tags

Our plugin uses: **`rh8_0-win`**

- `rh8` = Rhino 8 (major version)
- `_0` = Rhino 8.0+ (minor version, 0 means all 8.x)
- `win` = Windows only (.NET Framework 4.8, System.Windows.Forms)

### Platform Options
- `win` = Windows-only
- `mac` = Mac-only
- `any` = Cross-platform

### Future Considerations
If adding Rhino 7 support: Create separate package with `rh7_0-win` tag and push both versions.

## Success Metrics

- ⏱️ Package creation in under 5 minutes
- 🔍 Searchable within 30 seconds of push
- 📦 Discoverable in Rhino GUI within 1 hour
- 🎯 90% of users install via Package Manager (vs manual download)
- 🛟 Zero "how to install" support tickets

## Next Steps

1. **Read full spec**: [spec.md](spec.md)
2. **Review requirements checklist**: [checklists/requirements.md](checklists/requirements.md)
3. **Proceed to planning**: Run `/speckit.plan` to create implementation tasks

## Helpful Resources

- [Rhino Developer Docs: Creating a Plugin Package](https://developer.rhino3d.com/guides/yak/creating-a-rhino-plugin-package/)
- [Rhino Developer Docs: Pushing to Server](https://developer.rhino3d.com/guides/yak/pushing-a-package-to-the-server/)
- [Yak Manifest Reference](https://developer.rhino3d.com/guides/yak/the-package-manifest/)
- [Yak CLI Reference](https://developer.rhino3d.com/guides/yak/yak-cli-reference/)
