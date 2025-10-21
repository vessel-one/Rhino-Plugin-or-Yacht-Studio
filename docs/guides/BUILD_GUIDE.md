# Build System Guide

## Overview

The Vessel Studio Rhino Plugin has an integrated build system that handles changelog analysis, version checking, and compilation in one streamlined process.

## Build Scripts

### `build.ps1` - Full Build with Analysis

The main build script that integrates all quality checks:

```powershell
.\build.ps1
```

**Features:**
- Analyzes git commits for changelog updates
- Suggests version bumps based on changes
- Checks version consistency with CHANGELOG.md
- Builds the solution
- Reports generated .rhp files
- Offers quick plugin reload

**Parameters:**

```powershell
# Specify configuration (default: Release)
.\build.ps1 -Configuration Debug

# Clean before build
.\build.ps1 -Clean

# Skip changelog analysis (faster)
.\build.ps1 -SkipChangelog

# Skip version checks
.\build.ps1 -SkipVersionCheck

# Analyze more commits
.\build.ps1 -Since HEAD~20

# Combine options
.\build.ps1 -Clean -Configuration Release -Since HEAD~15
```

### `quick-build.ps1` - Rapid Development Build

Fast build without interactive analysis:

```powershell
.\quick-build.ps1
```

Equivalent to: `.\build.ps1 -SkipChangelog -SkipVersionCheck`

### `update-changelog.ps1` - Changelog Management

Analyze changes without building:

```powershell
# Suggest changelog updates
.\update-changelog.ps1 -Suggest

# Analyze impact only
.\update-changelog.ps1 -Analyze

# Look at more history
.\update-changelog.ps1 -Suggest -Since HEAD~20
```

## Typical Workflows

### 1. Development Builds (Fast Iteration)

```powershell
# Quick build, no analysis
.\quick-build.ps1
```

### 2. Pre-Commit Build

```powershell
# Full build with changelog analysis
.\build.ps1
```

Review suggested changelog entries and update `CHANGELOG.md` if needed.

### 3. Release Build

```powershell
# 1. Update version in PluginVersion.cs
# 2. Update CHANGELOG.md with release notes
# 3. Clean build with analysis

.\build.ps1 -Clean -Configuration Release
```

### 4. Debugging Build Errors

```powershell
# Clean build in Debug mode
.\build.ps1 -Clean -Configuration Debug -SkipChangelog
```

## Build Output

### Success

```
=== Vessel Studio Plugin Build ===

Step 1: Analyzing changes for changelog...
✓ Changelog analysis complete

Step 2: Checking version information...
  Current Version: 1.0.0
  ✓ Version documented in CHANGELOG.md

Step 3: Skipping clean

Step 4: Building solution...
  Configuration: Release
  Solution: VesselStudioPlugin.sln

Build succeeded.
    0 Error(s)
    101 Warning(s)

✓ Build completed successfully!

=== Build Summary ===

Plugin files generated:
  • net48\VesselStudioPlugin.rhp (187 KB)
  • net6.0-windows\VesselStudioPlugin.rhp (187 KB)

Installation:
  1. Close Rhino
  2. Copy .rhp file to Rhino plugin directory
  3. Start Rhino and test commands:
     - VesselAbout
     - VesselSetApiKey
     - VesselStatus
     - VesselCapture
```

### Build Files Location

After successful build:

```
VesselStudioPlugin/
  bin/
    Release/
      net48/
        VesselStudioPlugin.dll
        VesselStudioPlugin.rhp  ← Install this for Rhino 8
        Eto.Forms.dll
        System.Text.Json.dll
        ...other dependencies
      net6.0-windows/
        VesselStudioPlugin.dll
        VesselStudioPlugin.rhp
        ...
```

## Version Management

### Updating Version

Edit `VesselStudioPlugin/Models/PluginVersion.cs`:

```csharp
public const int Major = 1;
public const int Minor = 1;  // Increment for features
public const int Patch = 0;  // Increment for fixes
```

### Updating Changelog

Edit `CHANGELOG.md` following [Keep a Changelog](https://keepachangelog.com) format:

```markdown
## [1.1.0] - 2025-10-21

### Added
- New viewport synchronization feature
- Batch upload capability

### Fixed
- Screenshot quality settings persisting
- API key validation timeout

### Changed
- Updated UI layout for settings dialog
```

## Build Troubleshooting

### "Solution file not found"

Ensure you're in the repository root:
```powershell
cd "c:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin"
.\build.ps1
```

### "dotnet command not found"

Install .NET SDK 8.0 or later from https://dotnet.microsoft.com/download

### Build errors after updating code

Try a clean build:
```powershell
.\build.ps1 -Clean
```

### Warnings about RhinoCommon version

Normal - uses closest available version. Won't affect functionality.

### "Access denied" when cleaning

Close Visual Studio and Rhino before cleaning:
```powershell
.\build.ps1 -Clean
```

## Continuous Integration

For CI/CD pipelines, use:

```powershell
# Non-interactive build
.\build.ps1 -SkipChangelog -SkipVersionCheck -Configuration Release
```

Or directly:

```powershell
dotnet build VesselStudioPlugin.sln --configuration Release
```

## Advanced Usage

### Custom Build Targets

Directly use MSBuild targets:

```powershell
# Build only plugin project
dotnet build VesselStudioPlugin\VesselStudioPlugin.csproj

# Build specific framework
dotnet build --framework net48

# Verbose output
dotnet build --verbosity detailed
```

### Manual .rhp Generation

The build automatically generates .rhp files via post-build event. To manually create:

```powershell
Copy-Item "VesselStudioPlugin\bin\Release\net48\VesselStudioPlugin.dll" `
          "VesselStudioPlugin\bin\Release\net48\VesselStudioPlugin.rhp"
```

## Quick Reference

| Command | Use Case | Speed |
|---------|----------|-------|
| `.\quick-build.ps1` | Development iteration | ⚡ Fastest |
| `.\build.ps1 -SkipChangelog` | Testing changes | ⚡ Fast |
| `.\build.ps1` | Pre-commit check | ⏱️ Normal |
| `.\build.ps1 -Clean` | After major changes | 🐌 Slower |

## Related Scripts

- `reload-plugin.ps1` - Hot-reload in Rhino (experimental)
- `cleanup-plugin.ps1` - Clean all build artifacts
- `update-plugin.ps1` - Full update and rebuild

## Environment Requirements

- **OS:** Windows 10/11
- **.NET SDK:** 8.0 or later
- **PowerShell:** 5.1 or later (7+ recommended)
- **Git:** For changelog analysis
- **Rhino 8:** For testing

## See Also

- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - User documentation
- [CHANGELOG.md](CHANGELOG.md) - Version history
- [TESTING_GUIDE.md](TESTING_GUIDE.md) - Testing procedures
