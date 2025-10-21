# Rhino Package Manager - Publishing & Updates Guide

**Package Name**: VesselStudio  
**Current Version**: 1.0.1  
**Owner**: rikki@vesselone.io  
**Production URL**: https://yak.rhino3d.com/  
**Search**: https://www.rhino3d.com/packages/search/?q=vesselstudio

---

## Quick Reference

### Common Commands

```powershell
# Authentication (once per 30 days)
& "$env:TEMP\yak-standalone.exe" login

# Search for package
& "$env:TEMP\yak-standalone.exe" search VesselStudio

# Push new version to production
& "$env:TEMP\yak-standalone.exe" push VesselStudio-X.Y.Z-rh8_0-win.yak

# Push to test server first
& "$env:TEMP\yak-standalone.exe" push VesselStudio-X.Y.Z-rh8_0-win.yak --source https://test.yak.rhino3d.com

# Yank (hide) a version
& "$env:TEMP\yak-standalone.exe" yank VesselStudio X.Y.Z

# Unyank (restore) a version
& "$env:TEMP\yak-standalone.exe" unyank VesselStudio X.Y.Z

# List installed packages (on local machine)
& "$env:TEMP\yak-standalone.exe" list

# Add additional package owner
& "$env:TEMP\yak-standalone.exe" owner add VesselStudio email@example.com

# List package owners
& "$env:TEMP\yak-standalone.exe" owner list VesselStudio
```

---

## Version Numbering System

### Semantic Versioning: MAJOR.MINOR.PATCH

**Format**: `X.Y.Z` (e.g., 1.2.3)

- **MAJOR (X)**: Breaking changes, incompatible API changes
  - Example: 1.0.0 → 2.0.0 (major rewrite, breaking changes)
  
- **MINOR (Y)**: New features, backwards-compatible
  - Example: 1.0.0 → 1.1.0 (added new command)
  
- **PATCH (Z)**: Bug fixes, backwards-compatible
  - Example: 1.0.0 → 1.0.1 (fixed upload bug)

### Examples

- `1.0.0` → `1.0.1`: Fixed bug in viewport capture
- `1.0.1` → `1.1.0`: Added new toolbar panel feature
- `1.1.0` → `2.0.0`: Changed API, removed old commands

---

## Update Workflow

### Step 1: Make Changes & Build Locally

```powershell
# Make code changes to VesselStudioSimplePlugin/

# Run build script (checks CHANGELOG, auto-assigns version)
.\build.ps1 -Configuration Release

# Test the plugin in Rhino 8
# - Load the .rhp from bin/Release/net48/
# - Test all commands work
# - Verify no errors in command line
```

**Note**: `build.ps1` should:
- Check `CHANGELOG.md` for new entries since last version
- Auto-increment version number based on changelog category:
  - `### Added` or `### Changed` → Increment MINOR (Y)
  - `### Fixed` → Increment PATCH (Z)
  - `### BREAKING` → Increment MAJOR (X)
- Update version in:
  - `Properties/AssemblyInfo.cs`
  - `VesselStudioSimplePlugin.csproj`
  - `dist/manifest.yml` (when creating package)

---

### Step 2: Create Package for Distribution

#### Option A: Manual Package Creation (Current Method)

```powershell
# Navigate to repository root
cd "C:\Users\rikki.mcguire\Documents\vesselone\Yacht Studio Rhino Plugin"

# Clear and prepare dist/ folder
Remove-Item dist\* -Recurse -Force -ErrorAction SilentlyContinue

# Copy files to dist/
Copy-Item "VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp" "dist\"
Copy-Item "VesselStudioSimplePlugin\bin\Release\net48\Newtonsoft.Json.dll" "dist\"
Copy-Item "VesselStudioSimplePlugin\Resources\icon_48.png" "dist\icon.png"

# Update manifest.yml version (match AssemblyInfo.cs version)
# Edit dist/manifest.yml:
#   name: VesselStudio
#   version: X.Y.Z  # <-- Update this
#   authors: [Creata Collective Limited]
#   description: ... (update if needed)
#   url: https://vesselstudio.io
#   icon: icon.png
#   keywords: [vessel, studio, yacht, ...]

# Create .yak package (ZIP archive)
cd dist
Compress-Archive -Path * -DestinationPath "..\VesselStudio-X.Y.Z-rh8_0-win.yak" -Force
cd ..
```

#### Option B: Automated Package Script (Recommended - To Be Created)

Create `create-package.ps1`:

```powershell
# create-package.ps1
# Automatically creates Yak package from build output

param(
    [Parameter(Mandatory=$false)]
    [string]$Version  # If not provided, read from AssemblyInfo.cs
)

# Read version from AssemblyInfo if not provided
if (-not $Version) {
    $assemblyInfo = Get-Content "VesselStudioSimplePlugin\Properties\AssemblyInfo.cs"
    $versionLine = $assemblyInfo | Select-String 'AssemblyVersion\("(.+?)"\)'
    $Version = $versionLine.Matches.Groups[1].Value
    Write-Host "Auto-detected version: $Version"
}

# Prepare dist/ folder
Write-Host "Preparing dist/ folder..."
Remove-Item dist\* -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path dist -Force | Out-Null

# Copy files
Write-Host "Copying files to dist/..."
Copy-Item "VesselStudioSimplePlugin\bin\Release\net48\VesselStudioSimplePlugin.rhp" "dist\"
Copy-Item "VesselStudioSimplePlugin\bin\Release\net48\Newtonsoft.Json.dll" "dist\"
Copy-Item "VesselStudioSimplePlugin\Resources\icon_48.png" "dist\icon.png"

# Read changelog for description update
$changelog = Get-Content "CHANGELOG.md" -Raw
$latestChanges = $changelog -split "##" | Select-Object -Skip 1 -First 1
Write-Host "Latest changelog entry:`n$latestChanges"

# Create/update manifest.yml
$manifest = @"
---
name: VesselStudio
version: $Version
authors:
  - Creata Collective Limited
description: >
  Send design views straight from your Rhino viewport into your Vessel Studio project.
  Visualise designs in a fraction of a second, make amendments and refine.
  For better client and designer outcomes.
url: https://vesselstudio.io
icon: icon.png
keywords:
  - vessel
  - studio
  - yacht
  - marine
  - design
  - capture
  - viewport
  - screenshot
  - upload
  - visualization
  - guid:A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D
"@

$manifest | Out-File "dist\manifest.yml" -Encoding UTF8

# Create .yak package
Write-Host "Creating package: VesselStudio-$Version-rh8_0-win.yak"
cd dist
Compress-Archive -Path * -DestinationPath "..\VesselStudio-$Version-rh8_0-win.yak" -Force
cd ..

Write-Host "✅ Package created successfully!"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Test: yak push VesselStudio-$Version-rh8_0-win.yak --source https://test.yak.rhino3d.com"
Write-Host "2. Prod: yak push VesselStudio-$Version-rh8_0-win.yak"
```

Usage:
```powershell
# Auto-detect version from AssemblyInfo.cs
.\create-package.ps1

# Or specify version manually
.\create-package.ps1 -Version "1.2.0"
```

---

### Step 3: Test on Test Server (Optional but Recommended)

```powershell
# Push to test server (wiped nightly)
& "$env:TEMP\yak-standalone.exe" push VesselStudio-X.Y.Z-rh8_0-win.yak --source https://test.yak.rhino3d.com

# Verify it appears in search
& "$env:TEMP\yak-standalone.exe" search --source https://test.yak.rhino3d.com --all VesselStudio

# Test installation in Rhino 8
# 1. Open Rhino 8
# 2. Run: PackageManager
# 3. Click gear icon → Package Sources → Add test.yak.rhino3d.com
# 4. Search for "VesselStudio"
# 5. Install and test

# If test fails, yank it (only affects test server)
& "$env:TEMP\yak-standalone.exe" yank VesselStudio X.Y.Z --source https://test.yak.rhino3d.com
```

---

### Step 4: Publish to Production

```powershell
# Ensure you're authenticated (token valid for ~30 days)
& "$env:TEMP\yak-standalone.exe" login

# Push to production
& "$env:TEMP\yak-standalone.exe" push VesselStudio-X.Y.Z-rh8_0-win.yak

# Expected output:
# Successfully published VesselStudio (X.Y.Z) to https://yak.rhino3d.com/
# 
# Details
# =======
# name:          VesselStudio
# version:       X.Y.Z
# rhino version: rh8_0
# platform:      win

# Verify production listing
& "$env:TEMP\yak-standalone.exe" search VesselStudio
```

---

### Step 5: User Updates

**Automatic Updates**: 
- Rhino 8 checks for package updates automatically
- Users see update notification in Package Manager
- One-click update downloads and installs new version
- Old version is automatically uninstalled

**Manual Update**:
```
1. Open Rhino 8
2. Tools → Options → Package Manager
3. Look for "VesselStudio" with update badge
4. Click "Update" button
5. Restart Rhino (if prompted)
```

---

## Package Management Commands

### Yanking Versions (Hiding from Search)

**When to yank**:
- Critical bug discovered after publishing
- Security vulnerability found
- Wrong files included in package
- Need to prevent new installations

**How to yank**:
```powershell
& "$env:TEMP\yak-standalone.exe" yank VesselStudio 1.0.1
```

**What happens**:
- Version no longer appears in Package Manager search
- Existing users keep their installed version (not uninstalled)
- Can still be installed if exact version is known
- Cannot re-push the same version number (must increment)

**Unyank** (restore):
```powershell
& "$env:TEMP\yak-standalone.exe" unyank VesselStudio 1.0.1
```

---

### Managing Package Owners

**Add co-owner** (they can push updates):
```powershell
& "$env:TEMP\yak-standalone.exe" owner add VesselStudio other@vesselone.io
```

**Remove owner**:
```powershell
& "$env:TEMP\yak-standalone.exe" owner remove VesselStudio other@vesselone.io
```

**List owners**:
```powershell
& "$env:TEMP\yak-standalone.exe" owner list VesselStudio
```

**Note**: New owners must have a Rhino Accounts account and must run `yak login` once before being added.

---

### Permanent Package Deletion

**⚠️ Warning**: Cannot reuse package name after deletion!

If you absolutely need to delete the package permanently:
1. Email: support@mcneel.com
2. Subject: "Delete Yak Package: VesselStudio"
3. Provide: Package name, version, reason
4. Wait for McNeel support response

---

## Troubleshooting

### Authentication Issues

**Problem**: `401 Unauthorized` when pushing

**Solution**:
```powershell
# Re-authenticate
& "$env:TEMP\yak-standalone.exe" login
```

**Token Location**: `%APPDATA%\McNeel\yak.yml`  
**Token Lifespan**: ~30 days

---

### Version Conflict

**Problem**: `400: A package by the name of 'VesselStudio' with version number 'X.Y.Z' already exists`

**Solution**: 
- Version numbers are immutable (cannot overwrite)
- Increment version number (e.g., 1.0.1 → 1.0.2)
- Update `dist/manifest.yml` and rebuild package

---

### Wrong Package Name Published

**Problem**: Published as "vesselstudio" instead of "VesselStudio"

**Solution**:
```powershell
# 1. Yank the incorrect version
& "$env:TEMP\yak-standalone.exe" yank vesselstudio 1.0.0

# 2. Fix manifest.yml (name: VesselStudio)
# 3. Increment version (1.0.0 → 1.0.1)
# 4. Rebuild and push new version
```

**Note**: Package name in manifest determines the actual name, not the filename.

---

### Missing Files in Package

**Problem**: Users report missing DLLs or icons

**Solution**:
```powershell
# 1. Extract .yak to verify contents
Expand-Archive VesselStudio-X.Y.Z-rh8_0-win.yak -DestinationPath temp-check

# 2. Verify required files present:
#    - VesselStudioSimplePlugin.rhp ✓
#    - Newtonsoft.Json.dll ✓
#    - icon.png ✓
#    - manifest.yml ✓

# 3. If files missing, rebuild package correctly
# 4. Increment version and push fixed version
```

---

## File Structure

### Repository Files

```
Yacht Studio Rhino Plugin/
├── VesselStudioSimplePlugin/
│   ├── bin/Release/net48/
│   │   ├── VesselStudioSimplePlugin.rhp  ← Built binary
│   │   └── Newtonsoft.Json.dll            ← Dependency
│   ├── Resources/
│   │   └── icon_48.png                    ← Package icon
│   └── Properties/
│       └── AssemblyInfo.cs                ← Version source
│
├── dist/                                   ← Package staging (gitignored)
│   ├── VesselStudioSimplePlugin.rhp
│   ├── Newtonsoft.Json.dll
│   ├── icon.png
│   └── manifest.yml                       ← Package metadata
│
├── VesselStudio-X.Y.Z-rh8_0-win.yak      ← Generated package
├── build.ps1                              ← Build script
├── create-package.ps1                     ← Package creation script (to be created)
└── CHANGELOG.md                           ← Version history
```

### Package Contents (.yak is a ZIP)

```
VesselStudio-1.0.1-rh8_0-win.yak (ZIP archive)
├── VesselStudioSimplePlugin.rhp  (57 KB)
├── Newtonsoft.Json.dll           (712 KB)
├── icon.png                      (1 KB)
└── manifest.yml                  (543 bytes)
```

---

## manifest.yml Reference

```yaml
---
name: VesselStudio                    # Package name (CamelCase)
version: 1.0.1                         # Semantic version (X.Y.Z)
authors:                               # Package creators
  - Creata Collective Limited
description: >                         # Marketing text (shown in Package Manager)
  Send design views straight from your Rhino viewport into your Vessel Studio project.
  Visualise designs in a fraction of a second, make amendments and refine.
  For better client and designer outcomes.
url: https://vesselstudio.io          # Homepage URL
icon: icon.png                         # Icon filename (must be in package)
keywords:                              # Search keywords
  - vessel
  - studio
  - yacht
  - marine
  - design
  - capture
  - viewport
  - screenshot
  - upload
  - visualization
  - guid:A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D  # Plugin GUID (for package restore)
```

**Editable Fields**:
- `version`: Must increment for each release
- `description`: Update when features change
- `url`: Update if website changes
- `keywords`: Add new search terms as needed

**Fixed Fields** (don't change after first publish):
- `name`: Changing requires new package (can't rename)
- `icon`: File must exist in package
- `guid`: Must match plugin GUID in AssemblyInfo.cs

---

## Integration with Build Script

### Recommended build.ps1 Enhancement

```powershell
# build.ps1 additions

param(
    [Parameter(Mandatory=$false)]
    [string]$Configuration = "Release",
    
    [Parameter(Mandatory=$false)]
    [switch]$CreatePackage  # New parameter for package creation
)

# ... existing build logic ...

# After successful build:
if ($CreatePackage) {
    Write-Host "`n=== Creating Yak Package ===" -ForegroundColor Cyan
    
    # Read version from AssemblyInfo
    $assemblyInfo = Get-Content "VesselStudioSimplePlugin\Properties\AssemblyInfo.cs"
    $versionMatch = $assemblyInfo | Select-String 'AssemblyVersion\("(.+?)"\)'
    $version = $versionMatch.Matches.Groups[1].Value
    
    # Verify CHANGELOG.md has entry for this version
    $changelog = Get-Content "CHANGELOG.md" -Raw
    if ($changelog -notmatch "## \[$version\]") {
        Write-Error "CHANGELOG.md missing entry for version $version"
        exit 1
    }
    
    # Call package creation script
    & ".\create-package.ps1" -Version $version
    
    Write-Host "`n✅ Package ready: VesselStudio-$version-rh8_0-win.yak" -ForegroundColor Green
    Write-Host "Next: Run publish-package.ps1 to push to Yak server" -ForegroundColor Yellow
}
```

Usage:
```powershell
# Build only
.\build.ps1 -Configuration Release

# Build + Create package
.\build.ps1 -Configuration Release -CreatePackage
```

---

## Changelog Integration

### CHANGELOG.md Format

```markdown
# Changelog

All notable changes to the Vessel Studio Rhino Plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2025-10-22

### Fixed
- Fixed viewport capture on 4K displays
- Resolved API key validation error message

## [1.0.1] - 2025-10-21

### Changed
- Updated package name to CamelCase: VesselStudio

### Fixed
- Fixed multipart form data upload boundary issue

## [1.0.0] - 2025-10-21

### Added
- Initial release
- VesselCapture command for viewport screenshots
- API key management with VesselSetApiKey
- Toolbar panel integration
- Rhino Package Manager distribution
```

### Version Increment Rules

Based on changelog categories:

| Category | Version Change | Example |
|----------|---------------|---------|
| `### Added` (new features) | MINOR | 1.0.0 → 1.1.0 |
| `### Changed` (modifications) | MINOR | 1.0.0 → 1.1.0 |
| `### Fixed` (bug fixes) | PATCH | 1.0.0 → 1.0.1 |
| `### BREAKING` (incompatible changes) | MAJOR | 1.0.0 → 2.0.0 |
| `### Deprecated` | MINOR | 1.0.0 → 1.1.0 |
| `### Removed` | MAJOR | 1.0.0 → 2.0.0 |
| `### Security` (fixes) | PATCH | 1.0.0 → 1.0.1 |

---

## Complete Release Checklist

- [ ] **Code changes complete**
- [ ] **Update CHANGELOG.md** with new version section
- [ ] **Run build script**: `.\build.ps1 -Configuration Release`
  - [ ] AssemblyInfo.cs version auto-updated
  - [ ] .csproj version auto-updated
  - [ ] Build successful, no errors
- [ ] **Test locally in Rhino 8**
  - [ ] Load .rhp from bin/Release/net48/
  - [ ] Test all commands work
  - [ ] Test upload functionality
  - [ ] Verify toolbar displays correctly
- [ ] **Create package**: `.\create-package.ps1`
  - [ ] dist/ folder populated correctly
  - [ ] manifest.yml version matches AssemblyInfo.cs
  - [ ] .yak file created
- [ ] **Test on test server** (optional)
  - [ ] Push to test.yak.rhino3d.com
  - [ ] Install in fresh Rhino instance
  - [ ] Verify functionality
- [ ] **Publish to production**: `yak push VesselStudio-X.Y.Z-rh8_0-win.yak`
- [ ] **Verify production**: `yak search VesselStudio`
- [ ] **Git commit**: Tag with version number
  ```powershell
  git add -A
  git commit -m "chore: Release version X.Y.Z"
  git tag vX.Y.Z
  git push origin feature-packagemanager --tags
  ```
- [ ] **Notify users** (if major release)

---

## Links & Resources

- **Package Manager**: https://yak.rhino3d.com/
- **Search**: https://www.rhino3d.com/packages/search/?q=vesselstudio
- **Yak CLI Docs**: https://developer.rhino3d.com/guides/yak/yak-cli-reference/
- **Manifest Docs**: https://developer.rhino3d.com/guides/yak/the-package-manifest/
- **Package Anatomy**: https://developer.rhino3d.com/guides/yak/the-anatomy-of-a-package/
- **McNeel Support**: support@mcneel.com
- **Rhino Forum**: https://discourse.mcneel.com/

---

**Last Updated**: October 21, 2025  
**Package Version**: 1.0.1  
**Maintainer**: rikki@vesselone.io
