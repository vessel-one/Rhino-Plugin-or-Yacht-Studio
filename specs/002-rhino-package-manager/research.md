# Research: Rhino Package Manager (Yak)

**Feature**: Rhino Package Manager Distribution  
**Research Date**: October 21, 2025  
**Sources**: Rhino Developer Documentation

## Overview

The Rhino Package Manager (nicknamed "Yak") is McNeel's official plugin distribution system introduced in Rhino 7. It provides a centralized, searchable repository for Rhino plugins with built-in installation, update management, and dependency handling.

## Key Findings

### 1. Yak CLI Tool Location

**Windows**: `C:\Program Files\Rhino 8\System\Yak.exe`  
**Mac**: `/Applications/Rhino 8.app/Contents/Resources/bin/yak`

The CLI tool is included with Rhino installation (no separate download required).

### 2. Package Structure

A `.yak` file is essentially a ZIP archive containing:

```
vesselstudio-1.0.0-rh8_0-win.yak
├── VesselStudioSimplePlugin.rhp     # Plugin binary
├── Newtonsoft.Json.dll              # Dependencies (if not provided by Rhino)
├── manifest.yml                     # Package metadata
├── icon.png                         # Icon for Package Manager UI (optional but recommended)
├── README.md                        # Documentation (optional)
└── LICENSE.txt                      # License (optional)
```

### 3. Manifest File (manifest.yml)

Required fields:
- `name`: Package identifier (lowercase, alphanumeric + hyphens)
- `version`: Semantic version (e.g., 1.0.0)
- `authors`: List of author names
- `description`: Plain text or multiline description

Recommended fields:
- `url`: Project website or documentation URL
- `icon`: Relative path to icon file (PNG recommended)
- `keywords`: Search terms (plugin GUID automatically added)

Example:
```yaml
---
name: vesselstudio
version: 1.0.0
authors:
- Creata Collective Limited
description: >
  Send design views straight from your Rhino viewport into your Vessel Studio project.
  Visualise designs in a fraction of a second, make amendments and refine.
url: https://vesselstudio.io
icon: icon.png
keywords:
- vessel
- studio
- yacht
- capture
- viewport
- guid:A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D
```

### 4. Distribution Tags

Format: `rh{MAJOR}_{MINOR}-{PLATFORM}`

Components:
- **MAJOR**: Rhino major version (6, 7, 8, etc.)
- **MINOR**: Rhino minor version (0 means all minor versions)
- **PLATFORM**: `win`, `mac`, or `any`

Examples:
- `rh8_0-win` = Rhino 8.0+ on Windows
- `rh7_0-any` = Rhino 7.0+ on any platform
- `rh8_10-mac` = Rhino 8.10+ on Mac only

**Auto-detection**: The `yak build` command automatically determines the distribution tag by inspecting the RhinoCommon.dll reference in the plugin. For `RhinoCommon 8.1.23325.13001`, it generates `rh8_1` (Rhino 8.1+).

**Platform Override**: Use `--platform win|mac|any` flag with `yak build` to specify platform explicitly.

### 5. Version Immutability

🚨 **CRITICAL CONSTRAINT**: Once a package version is published, it **cannot be deleted or overwritten**.

**Rationale**: Prevents disruption to users who depend on specific versions. Ensures reproducible builds and dependency resolution.

**Workaround**: Use `yak yank <package> <version>` to unlist a version from search results, but it remains installable if users know the exact version number.

**Best Practice**: 
- Always test on test server first
- Increment version numbers for fixes (don't try to republish same version)
- Use pre-release versions (e.g., 1.0.0-beta.1) for testing

### 6. Authentication System

Uses OAuth 2.0 via Rhino Accounts (accounts.rhino3d.com).

**Process**:
1. Run `yak login`
2. Browser opens to Rhino Accounts login page
3. Grant "Yak" access to account (one-time approval)
4. OAuth token saved to:
   - Windows: `%APPDATA%\McNeel\yak.yml`
   - Mac: `~/.mcneel/yak.yml`

**Token Lifespan**: Approximately 30 days. After expiration, rerun `yak login`.

**Required Permissions**:
- View basic info (name, locale, profile picture)
- Verify identity (for package ownership)
- View email address (for owner management)

### 7. Test vs Production Servers

**Test Server**: https://test.yak.rhino3d.com
- Wiped clean every night
- Perfect for testing package creation and push process
- No long-term consequences for mistakes

**Production Server**: https://yak.rhino3d.com
- Permanent, public-facing package repository
- All Rhino users worldwide search this server
- Version immutability strictly enforced

**Usage**:
```powershell
# Push to test
yak push package.yak --source https://test.yak.rhino3d.com

# Push to production (default)
yak push package.yak
```

### 8. Package Ownership

- **First Publisher = Owner**: The first user to push a package name becomes its owner
- **Multiple Owners**: Use `yak owner add <package> <email>` to add additional owners
- **Owner Permissions**: Only owners can push new versions
- **Name Squatting Prevention**: McNeel may revoke ownership of unused/squatted names

**Verification**:
```powershell
yak owner --help
```

### 9. Icon Requirements

**Format**: PNG (recommended) or SVG  
**Size**: 32x32 or 48x48 pixels (will be scaled as needed)  
**Location**: Place in dist/ folder, reference in manifest.yml as `icon: icon.png`

**Current Asset**: We have `Resources/icon_32.png` (768 bytes) which can be copied to dist/ folder.

### 10. Dependency Handling

**Rhino-Provided Assemblies** (exclude from package):
- RhinoCommon.dll
- Rhino.UI.dll
- Eto.dll
- Ed.Eto.dll

**Third-Party Dependencies** (include in package):
- Newtonsoft.Json.dll (13.0.3) ✅ Include
- Any other NuGet packages not provided by Rhino

**Current Status**: Our post-build script already removes Rhino-provided DLLs. Newtonsoft.Json.dll remains in bin/Release/net48/ and should be included in package.

### 11. Package Discovery & Installation

**User Workflow**:
1. Open Rhino 8
2. Type `_PackageManager` command OR go to Tools > Options > Package Manager
3. Search for package name or keywords
4. Click "Install"
5. Rhino downloads, installs, and loads plugin
6. May prompt for restart if plugin requires it

**Search Keywords**: Users can search by:
- Package name (`vesselstudio`)
- Display name (`Vessel Studio`)
- Keywords from manifest (`vessel`, `studio`, `yacht`, `capture`)
- Author name (`Creata Collective Limited`)

### 12. Update Notifications

**Detection**: Rhino periodically checks for package updates (exact frequency not documented, likely on startup)

**User Experience**:
1. User opens Rhino with outdated plugin
2. Package Manager badge shows update available
3. User clicks into Package Manager
4. "Update" button appears next to package
5. Click to update, plugin reinstalls, may require restart

**Maintainer Action**: Just push new version with incremented version number. No special "notify users" step required.

### 13. Common Errors & Troubleshooting

#### Error: "Failed to authenticate"
- **Cause**: OAuth token expired or missing
- **Fix**: Run `yak login` again

#### Error: "Package name already exists"
- **Cause**: Someone else owns that package name
- **Fix**: Choose different name or contact McNeel to claim ownership

#### Error: "Package version already exists"
- **Cause**: Trying to overwrite published version
- **Fix**: Increment version number (cannot overwrite)

#### Error: "Invalid manifest.yml"
- **Cause**: YAML syntax error or missing required fields
- **Fix**: Validate YAML syntax, check required fields (name, version, authors, description)

#### Error: "Incompatible Rhino version"
- **Cause**: Distribution tag too restrictive
- **Fix**: Adjust distribution tag or create multiple versions for different Rhino versions

### 14. Cross-Platform Considerations

**Current Plugin Status**: Windows-only
- Uses .NET Framework 4.8 (Windows-only runtime)
- Uses System.Windows.Forms (Windows-only UI)

**Distribution Tag**: Must use `rh8_0-win` (not `any`)

**Mac Support**: Would require:
- Migrate to .NET 6+ (cross-platform runtime)
- Replace System.Windows.Forms with Eto.Forms (cross-platform UI)
- Significant code rewrite (~70% of UI code)

**Recommendation**: Start with Windows-only. Add Mac support in future version if demand exists.

### 15. Semantic Versioning Best Practices

Format: `MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]`

**MAJOR** (1.x.x): Breaking changes
- API changes incompatible with previous version
- Removed commands or features
- Changed behavior that breaks user workflows

**MINOR** (x.1.x): New features (backwards compatible)
- New commands added
- New functionality in existing commands
- Performance improvements

**PATCH** (x.x.1): Bug fixes
- Fix crashes or errors
- Correct incorrect behavior
- Security patches

**PRERELEASE** (x.x.x-beta.1): Testing versions
- `-alpha.1`, `-alpha.2` for early testing
- `-beta.1`, `-beta.2` for wider testing
- `-rc.1`, `-rc.2` for release candidates

Examples:
- `1.0.0` - Initial release
- `1.0.1` - Bug fix (upload error corrected)
- `1.1.0` - Add new feature (e.g., batch capture)
- `2.0.0` - Breaking change (e.g., remove deprecated commands)
- `1.1.0-beta.1` - Testing version before 1.1.0 release

### 16. Build Automation Considerations

**Manual Process** (current):
1. Build plugin in Release mode
2. Create dist/ folder manually
3. Copy .rhp, DLLs, icon, manifest to dist/
4. Run `yak build` in dist/ folder
5. Run `yak push` with resulting .yak file

**Automated Process** (proposed):
1. Run `.\build.ps1 -Configuration Release -CreatePackage`
2. Script does everything above automatically
3. Optionally adds `-PushToTest` or `-PushToProduction` flags

**Implementation Approach**:
- Add PowerShell functions to build.ps1
- Create dist/ folder structure
- Copy files with post-build logic
- Shell out to yak.exe for build/push
- Validate version consistency before building

**Benefits**:
- Reduces human error (forgot to copy a file)
- Ensures consistency (same process every time)
- Speeds up releases (one command vs multiple steps)
- Easier onboarding (new maintainers just run script)

## Technical Constraints

1. **Version Immutability**: Cannot delete or overwrite published versions
2. **Platform Specificity**: Windows-only due to .NET Framework 4.8
3. **Rhino Version**: Targets Rhino 8+ only (RhinoCommon 8.1.23325.13001)
4. **Name Uniqueness**: Package name must be globally unique on yak.rhino3d.com
5. **OAuth Token Expiry**: Maintainers must re-authenticate every ~30 days
6. **Public Distribution Only**: No private package hosting (all packages are public)

## Open Questions

1. **Package Name Availability**: Is "vesselstudio" available on yak.rhino3d.com?
   - **Resolution**: Check during first push attempt. Have backup names ready ("vessel-studio", "vesselstudiorhino")

2. **Icon Size Preference**: Does 32x32 or 48x48 render better in Package Manager UI?
   - **Resolution**: Use 48x48 (icon_48.png) for better quality on high-DPI displays

3. **README Content**: What should be in README.md for package distribution?
   - **Resolution**: Include installation instructions, API key setup, usage guide, troubleshooting

4. **License File**: What license should be used?
   - **Resolution**: Determine with Creata Collective Limited legal team. Common options: MIT, Apache 2.0, or proprietary license

5. **Update Frequency**: How often should we check for updates in plugin code?
   - **Resolution**: Out of scope - Rhino's Package Manager handles this automatically

## Next Steps

1. Create manifest.yml with correct metadata
2. Copy icon_48.png to icon.png for package
3. Build package with `yak build`
4. Test push to test.yak.rhino3d.com
5. Verify searchability and installation
6. Push to production after validation
7. Document process in BUILD_GUIDE.md

## References

- [Creating a Rhino Plugin Package](https://developer.rhino3d.com/guides/yak/creating-a-rhino-plugin-package/) - Official guide
- [Pushing a Package to the Server](https://developer.rhino3d.com/guides/yak/pushing-a-package-to-the-server/) - Publishing guide
- [The Package Manifest](https://developer.rhino3d.com/guides/yak/the-package-manifest/) - manifest.yml reference
- [Yak CLI Reference](https://developer.rhino3d.com/guides/yak/yak-cli-reference/) - Complete CLI documentation
- [The Anatomy of a Package](https://developer.rhino3d.com/guides/yak/the-anatomy-of-a-package/) - Package structure details
