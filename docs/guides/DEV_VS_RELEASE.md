# DEV vs RELEASE Build Guide

## Overview
The Vessel Studio Rhino Plugin supports two build modes that can run **side-by-side** without conflicts:

- **DEV** - For active development and testing new features
- **RELEASE** - For production deployment via package manager

## Quick Commands

### Build DEV Version
```powershell
.\dev-build.ps1              # Build only
.\dev-build.ps1 -Install     # Build and install to Rhino
.\dev-build.ps1 -Clean       # Clean and build
```

### Build RELEASE Version
```powershell
.\quick-build.ps1            # Fast build (recommended)
.\build.ps1                  # Full build with verification
```

## Key Differences

| Feature | DEV Build | RELEASE Build |
|---------|-----------|---------------|
| **Configuration** | Debug | Release |
| **Plugin GUID** | `D1E2F3A4-B5C6-7D8E-9F0A-1B2C3D4E5F6A` | `A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D` |
| **Panel GUID** | `D5E6F7A8-B9C0-1D2E-3F4A-5B6C7D8E9F0A` | `A5B6C7D8-E9F0-4A5B-8C9D-0E1F2A3B4C5D` |
| **Command Prefix** | `Dev` | None |
| **Settings Folder** | `%APPDATA%\VesselStudioDEV\` | `%APPDATA%\VesselStudio\` |
| **Can coexist?** | ✅ Yes | ✅ Yes |

## DEV Commands

When running the DEV build, all commands are prefixed with `Dev`:

- `DevVesselStudioShowToolbar` - Show DEV toolbar
- `DevVesselSetApiKey` - Configure API key (DEV settings)
- `DevVesselCapture` - Capture screenshot
- `DevVesselQuickCapture` - Quick capture
- `DevVesselStudioStatus` - Check status
- `DevVesselStudioHelp` - Show help
- `DevVesselStudioAbout` - About dialog

## RELEASE Commands

RELEASE builds use standard command names:

- `VesselStudioShowToolbar`
- `VesselSetApiKey`
- `VesselCapture`
- `VesselQuickCapture`
- `VesselStudioStatus`
- `VesselStudioHelp`
- `VesselStudioAbout`

## Installation Locations

### DEV Plugin
```
%APPDATA%\McNeel\Rhinoceros\8.0\Plug-ins\Vessel Studio DEV\
├── VesselStudioSimplePlugin.rhp
└── Newtonsoft.Json.dll
```

### RELEASE Plugin (via Package Manager)
Installed via Yak package manager to Rhino's standard plugin directory.

## Typical Workflow

### 1. Development Phase
```powershell
# Make changes to code
# Build and test in DEV mode
.\dev-build.ps1 -Install

# Start Rhino - both DEV and RELEASE can run together
# Test using Dev-prefixed commands
# DEV uses separate settings so no conflict
```

### 2. Testing Phase
```powershell
# Build DEV version
.\dev-build.ps1 -Install

# Test thoroughly in Rhino
# DEV settings are in %APPDATA%\VesselStudioDEV\

# Production version continues to work normally
```

### 3. Release Phase
```powershell
# When ready to release
.\build.ps1                  # Full build with checks
.\create-package.ps1         # Create .yak package
.\publish-package.ps1        # Publish to package manager

# Or use quick release
.\release.ps1                # Automated release workflow
```

## How It Works

### Conditional Compilation
The project uses the `DEV` preprocessor directive in Debug builds:

```xml
<!-- VesselStudioSimplePlugin.csproj -->
<PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
  <DefineConstants>DEBUG;TRACE;DEV</DefineConstants>
</PropertyGroup>
```

### Code Implementation
```csharp
// Different GUIDs
#if DEV
[assembly: Guid("D1E2F3A4-B5C6-7D8E-9F0A-1B2C3D4E5F6A")] // DEV
#else
[assembly: Guid("A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D")] // RELEASE
#endif

// Different command names
#if DEV
public override string EnglishName => "DevVesselCapture";
#else
public override string EnglishName => "VesselCapture";
#endif

// Different settings storage
private static string SettingsPath => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
#if DEV
    "VesselStudioDEV",  // DEV folder
#else
    "VesselStudio",     // Production folder
#endif
    "settings.json"
);
```

## Benefits

✅ **No Conflicts** - Different GUIDs mean DEV and RELEASE are completely separate plugins
✅ **Separate Settings** - DEV changes don't affect production settings
✅ **Test Safely** - Experiment without breaking production workflow
✅ **Easy Switching** - Use commands with different prefixes to choose which version
✅ **Side-by-Side** - Both versions can be installed and run simultaneously
✅ **Clear Distinction** - DEV commands are clearly marked with "Dev" prefix

## Troubleshooting

### "Command not found"
- DEV builds: Use `DevVesselCapture` not `VesselCapture`
- RELEASE builds: Use `VesselCapture` not `DevVesselCapture`

### Settings not persisting
- DEV settings: `%APPDATA%\VesselStudioDEV\settings.json`
- RELEASE settings: `%APPDATA%\VesselStudio\settings.json`
- They are completely separate!

### Both versions installed but only one works
- Both can work simultaneously
- Use the correct command prefix for each version
- Check Rhino's PluginManager to see which is loaded

### Uninstalling
```powershell
# DEV version - delete folder
Remove-Item "$env:APPDATA\McNeel\Rhinoceros\8.0\Plug-ins\Vessel Studio DEV" -Recurse -Force

# RELEASE version - use package manager
# In Rhino: _PackageManager, find "Vessel Studio", click Uninstall
```

## Package Manager Deployment

Only RELEASE builds should be published to the package manager:

```powershell
# Build release version
.\build.ps1 -Configuration Release

# Create package
.\create-package.ps1

# Publish (test server)
.\publish-package.ps1 -Test

# Publish (production)
.\publish-package.ps1
```

DEV builds are **never** published to the package manager - they're for local development only.
