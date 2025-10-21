# Data Model: Rhino Package Manager Distribution

**Feature**: 002-rhino-package-manager  
**Date**: 2025-10-21  
**Status**: Complete

## Overview

This feature does not introduce new domain entities in the plugin code. Instead, it works with existing entities defined by McNeel's Yak package manager system. This document describes those entities and how they relate to our plugin distribution workflow.

## Entities

### 1. Yak Package

**Description**: A compressed archive file containing a Rhino plugin and its metadata for distribution through the Package Manager.

**Attributes**:
- `name` (string): Package identifier (lowercase, alphanumeric + hyphens)
  - Example: `"vesselstudio"`
  - Constraints: Must be unique on package server, no spaces or special chars
- `version` (string): Semantic version number
  - Format: `MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]`
  - Example: `"1.0.0"`, `"1.1.0-beta.1"`
  - Constraints: Must follow SemVer spec, immutable once published
- `distribution_tag` (string): Rhino version and platform compatibility
  - Format: `rh{major}_{minor}-{platform}`
  - Example: `"rh8_0-win"` (Rhino 8.0+ on Windows)
  - Platforms: `win`, `mac`, `any`
- `filename` (string): Package file name
  - Format: `{name}-{version}-{distribution_tag}.yak`
  - Example: `"vesselstudio-1.0.0-rh8_0-win.yak"`
- `contents` (array): Files included in package
  - Plugin binary (.rhp)
  - Dependencies (.dll files)
  - Manifest (manifest.yml)
  - Icon (icon.png)
  - Optional documentation (README.md, LICENSE.txt)

**Relationships**:
- Contains exactly one Manifest
- May contain multiple Plugin Dependencies
- Published to Package Server
- Installed by Package Manager (Rhino GUI)

**Lifecycle States**:
1. **Staged**: Files in dist/ folder, not yet built
2. **Built**: .yak file created locally by `yak build`
3. **Pushed to Test**: Uploaded to test.yak.rhino3d.com for validation
4. **Pushed to Production**: Uploaded to yak.rhino3d.com (public)
5. **Listed**: Searchable in Package Manager
6. **Yanked**: Unlisted from search but still installable if URL known

**Validation Rules**:
- Name must match manifest.yml name field
- Version must match manifest.yml version field
- Distribution tag must match Rhino version compatibility
- Must contain valid manifest.yml at root
- Icon reference in manifest must resolve to included file
- Cannot publish duplicate version (server enforces immutability)

### 2. Manifest

**Description**: YAML metadata file describing package contents and compatibility.

**Attributes** (Required):
- `name` (string): Package name (must match .yak filename)
- `version` (string): Package version (must match .yak filename)
- `authors` (array of strings): Package creators
  - Example: `["Creata Collective Limited"]`
- `description` (string): Plain text or multiline package description
  - May use YAML `>` for multiline strings

**Attributes** (Optional):
- `url` (string): Project website or documentation URL
  - Example: `"https://vesselstudio.io"`
- `icon` (string): Relative path to icon file
  - Example: `"icon.png"`
  - Recommended: PNG 32x32 or 48x48
- `keywords` (array of strings): Search terms
  - Example: `["vessel", "studio", "yacht", "capture"]`
  - Auto-included: Plugin GUID (for package restore)

**Relationships**:
- Owned by exactly one Yak Package
- References Icon file (if icon field specified)
- Metadata extracted by Yak CLI from .rhp assembly on `yak spec`

**Validation Rules**:
- Must be valid YAML syntax
- Name, version, authors, description are required
- Icon reference must point to included file
- Keywords should aid discoverability (not required)
- URL should be valid HTTP/HTTPS (if provided)

**Example**:
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
  - guid:A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D
```

### 3. Distribution Tag

**Description**: Version identifier indicating Rhino compatibility and platform support.

**Attributes**:
- `rhino_major` (integer): Rhino major version (6, 7, 8, etc.)
- `rhino_minor` (integer): Rhino minor version (0 means all minor versions)
- `platform` (enum): Target operating system
  - Values: `win`, `mac`, `any`

**Format**: `rh{major}_{minor}-{platform}`

**Examples**:
- `rh8_0-win`: Rhino 8.0+ on Windows
- `rh7_0-any`: Rhino 7.0+ on any platform
- `rh8_10-mac`: Rhino 8.10+ on macOS only

**Relationships**:
- Part of Yak Package filename
- Auto-detected by `yak build` from RhinoCommon.dll version reference
- Can be overridden with `--platform` flag

**Validation Rules**:
- Major version must be >= 6 (Rhino 6 introduced modern plugin API)
- Minor version typically 0 (all minor versions) or specific minor
- Platform must match actual plugin compatibility (don't claim cross-platform if Windows-only)

**Our Distribution Tag**: `rh8_0-win`
- Rhino 8.0+ required (RhinoCommon 8.1.23325.13001 dependency)
- Windows-only (.NET Framework 4.8 + System.Windows.Forms)

### 4. OAuth Token

**Description**: Authentication credential for publishing packages to McNeel servers.

**Attributes**:
- `access_token` (string): OAuth 2.0 access token
- `refresh_token` (string): Token for obtaining new access token
- `expires_at` (datetime): Token expiration time (~30 days from issuance)
- `scope` (array): Permissions granted
  - View basic info (name, locale, profile picture)
  - Verify identity (package ownership)
  - View email address (owner management)

**Storage**:
- **Windows**: `%APPDATA%\McNeel\yak.yml`
- **macOS**: `~/.mcneel/yak.yml`
- Format: YAML file managed by Yak CLI

**Relationships**:
- Issued by Rhino Accounts OAuth service
- Used by Yak CLI for `yak push` authentication
- Associated with Rhino Account user (maintainer)

**Lifecycle**:
1. **Not Authenticated**: No token exists
2. **Authenticating**: User runs `yak login`, browser opens
3. **Authenticated**: Token saved to yak.yml
4. **Active**: Token valid, can push packages
5. **Expired**: Token >30 days old, re-authentication required
6. **Refreshed**: New token obtained automatically or via re-login

**Validation Rules**:
- Token must be valid (not expired)
- Token must have push permissions
- User must own package or be added as owner

**Security Notes**:
- Token managed by Yak CLI, not our code
- Do not commit yak.yml to version control
- Do not log token value
- Re-authentication required approximately every 30 days

### 5. Distribution Folder

**Description**: Temporary staging directory containing all files to be packaged.

**Location**: `dist/` (relative to repository root, gitignored)

**Contents**:
- `VesselStudioSimplePlugin.rhp` (plugin binary)
- `Newtonsoft.Json.dll` (dependency not provided by Rhino)
- `icon.png` (package icon, copied from Resources/icon_48.png)
- `manifest.yml` (package metadata)
- `README.md` (optional: installation/usage instructions)
- `LICENSE.txt` (optional: software license)

**Excluded**:
- `RhinoCommon.dll` (provided by Rhino)
- `Rhino.UI.dll` (provided by Rhino)
- `Eto.dll` (provided by Rhino)
- `.pdb` files (debug symbols)
- Source code files

**Relationships**:
- Input for `yak build` command
- Populated by build script (manual or automated)
- Temporary (can be deleted after .yak creation)

**Lifecycle**:
1. **Empty**: dist/ folder doesn't exist or is empty
2. **Staged**: Files copied from bin/Release and Resources
3. **Manifest Generated**: `yak spec` creates manifest.yml
4. **Manifest Customized**: Manual edits to description, URL, keywords
5. **Built**: `yak build` creates .yak file
6. **Cleanup**: dist/ contents can be deleted (keep manifest.yml for next version)

**Validation Rules** (Pre-Build Checklist):
- ✅ VesselStudioSimplePlugin.rhp exists and is not empty
- ✅ Newtonsoft.Json.dll exists (correct version 13.0.3)
- ✅ icon.png exists and is 48x48 PNG
- ✅ manifest.yml exists and is valid YAML
- ✅ manifest.yml name, version, authors, description are filled
- ✅ No RhinoCommon/Eto/Rhino.UI DLLs present
- ✅ Version in manifest.yml matches AssemblyInfo.cs

### 6. Package Server

**Description**: McNeel's hosted service for distributing Rhino packages.

**Attributes**:
- `url` (string): Server base URL
- `environment` (enum): Server type
  - `test`: https://test.yak.rhino3d.com (wiped nightly)
  - `production`: https://yak.rhino3d.com (permanent)

**Relationships**:
- Hosts Yak Packages
- Authenticates via OAuth Token
- Queried by Rhino Package Manager GUI
- Updated via Yak CLI `yak push`

**Operations**:
- `push`: Upload package (requires authentication)
- `search`: Query packages (no authentication)
- `yank`: Unlist version (requires ownership)
- `owner add/remove`: Manage package owners (requires ownership)

**Test Server Characteristics**:
- Wiped clean every night at midnight UTC
- Perfect for testing package creation
- No long-term consequences for mistakes
- Same API as production

**Production Server Characteristics**:
- Permanent storage
- Public-facing, searchable by all Rhino users
- Version immutability enforced
- Package name squatting prevention

**Validation Rules**:
- Package name must be unique (first publisher becomes owner)
- Package version must be unique within name (no overwrites)
- Manifest must be valid
- Authentication required for push operations
- Ownership required for yank/owner operations

## Data Flow

### Package Creation Flow

```
1. Build Plugin (Release mode)
   └─> bin/Release/net48/VesselStudioSimplePlugin.rhp

2. Populate dist/ folder
   ├─> Copy .rhp from bin/Release
   ├─> Copy Newtonsoft.Json.dll from bin/Release
   ├─> Copy icon.png from Resources/icon_48.png
   └─> Generate manifest.yml with `yak spec`

3. Customize Manifest
   └─> Edit manifest.yml (description, URL, keywords)

4. Build Package
   └─> `yak build` creates vesselstudio-1.0.0-rh8_0-win.yak

5. Authenticate
   └─> `yak login` obtains OAuth Token

6. Test Push
   └─> `yak push --source test.yak.rhino3d.com` uploads to test server

7. Validate
   └─> `yak search --source test.yak.rhino3d.com vesselstudio` confirms presence

8. Production Push
   └─> `yak push` uploads to yak.rhino3d.com

9. Verify
   └─> Search in Rhino Package Manager GUI
```

### User Installation Flow

```
1. User opens Rhino 8
2. User opens Package Manager (Tools > Options > Package Manager)
3. User searches "Vessel Studio"
4. Package Manager queries yak.rhino3d.com
5. vesselstudio package appears in results
6. User clicks "Install"
7. Package Manager downloads vesselstudio-1.0.0-rh8_0-win.yak
8. Package Manager extracts to Rhino plugins folder
9. Rhino loads VesselStudioSimplePlugin.rhp
10. User runs `VesselCapture` command successfully
```

### Update Publishing Flow

```
1. Increment version in AssemblyInfo.cs (1.0.0 → 1.1.0)
2. Rebuild plugin
3. Update manifest.yml version field
4. Rebuild package with `yak build`
5. Push updated package `yak push vesselstudio-1.1.0-rh8_0-win.yak`
6. Users with 1.0.0 see update notification in Package Manager
7. Users click "Update"
8. Package Manager downloads 1.1.0, uninstalls 1.0.0, installs 1.1.0
9. Rhino may prompt for restart
```

## Version Management

### Semantic Versioning Strategy

**Format**: `MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]`

**Version Increments**:
- **MAJOR** (1.x.x → 2.x.x): Breaking changes
  - Removed commands or features
  - Changed command behavior breaking workflows
  - API incompatibilities
- **MINOR** (x.1.x → x.2.x): New features (backwards compatible)
  - New commands added
  - New functionality in existing commands
  - Performance improvements
- **PATCH** (x.x.1 → x.x.2): Bug fixes
  - Fix crashes or errors
  - Correct incorrect behavior
  - Security patches
- **PRERELEASE** (x.x.x-beta.1): Testing versions
  - `-alpha.1`, `-alpha.2` for early testing
  - `-beta.1`, `-beta.2` for wider testing
  - `-rc.1`, `-rc.2` for release candidates

**Version Consistency Requirements**:
- `AssemblyInfo.cs`: AssemblyVersion = "1.0.0.0" (4-part .NET version)
- `VesselStudioSimplePlugin.csproj`: Version = "1.0.0" (3-part SemVer)
- `manifest.yml`: version: 1.0.0 (3-part SemVer)
- `.yak filename`: vesselstudio-1.0.0-rh8_0-win.yak

**Recommended**: Build script validation to ensure all versions match before package creation.

## File Size Constraints

**Yak Package Size Limits**:
- No documented hard limit
- Typical plugin packages: 100KB - 5MB
- Our expected size: ~250-300KB
  - VesselStudioSimplePlugin.rhp: ~53KB
  - Newtonsoft.Json.dll: ~700KB (but may be compressed in .yak)
  - icon.png: ~1KB
  - manifest.yml: <1KB
  - Total uncompressed: ~755KB
  - Total compressed (estimated): ~250KB

**Icon Size Requirements**:
- Recommended: 32x32 or 48x48 pixels
- Format: PNG (transparency supported)
- File size: <10KB (ours is ~1KB at 48x48)

## Error Conditions

**Package Build Errors**:
- Missing manifest.yml → "manifest.yml not found"
- Invalid YAML syntax → "Failed to parse manifest.yml"
- Missing required fields → "Required field 'name' not found"
- Icon reference broken → Warning (build succeeds, no icon displayed)
- No .rhp file in dist/ → "No plugin binary found"

**Package Push Errors**:
- Not authenticated → "Not logged in. Run 'yak login' first"
- Token expired → "Authentication expired. Run 'yak login' again"
- Package name taken → "Package 'X' already exists and you are not an owner"
- Version exists → "Version 'X' already exists. Cannot overwrite."
- Invalid manifest → "Manifest validation failed: [details]"
- Network error → "Failed to connect to server: [details]"

**User Installation Errors**:
- Incompatible Rhino version → "Requires Rhino 8.0 or later"
- Incompatible platform → "This package is Windows-only"
- Dependency conflict → Plugin loads but may malfunction (rare)
- Corrupted download → "Package download failed. Retry?"

## Relationships Diagram

```
                    ┌─────────────────┐
                    │  Rhino Account  │
                    │  (OAuth Provider)│
                    └────────┬────────┘
                             │ issues
                             ↓
                    ┌─────────────────┐
                    │  OAuth Token    │
                    │  (yak.yml)      │
                    └────────┬────────┘
                             │ authenticates
                             ↓
    ┌──────────────┐   ┌─────────────────┐   ┌──────────────┐
    │  Maintainer  │─→ │   Yak CLI       │─→ │ Package      │
    │  (Human)     │   │  (yak.exe)      │   │ Server       │
    └──────────────┘   └─────────────────┘   └──────────────┘
           │                   │                      │
           │ creates           │ builds               │ hosts
           ↓                   ↓                      ↓
    ┌──────────────┐   ┌─────────────────┐   ┌──────────────┐
    │ Distribution │─→ │  Yak Package    │─→ │ Published    │
    │ Folder       │   │  (.yak file)    │   │ Package      │
    │ (dist/)      │   └─────────────────┘   └──────────────┘
    └──────────────┘           │                      │
           │                   │                      │
           │ contains          │ contains             │
           ↓                   ↓                      ↓
    ┌──────────────┐   ┌─────────────────┐          │
    │  Manifest    │   │ Plugin Binary   │          │
    │  (YAML)      │   │ (.rhp)          │          │
    └──────────────┘   └─────────────────┘          │
           │                   │                      │
           │ references        │                      │ searches
           ↓                   │                      ↓
    ┌──────────────┐          │              ┌──────────────┐
    │  Icon        │          │              │ Rhino User   │
    │  (.png)      │          │              │ (Human)      │
    └──────────────┘          │              └──────────────┘
                              │                      │
                              │ plus                 │ installs via
                              ↓                      ↓
                     ┌─────────────────┐   ┌──────────────┐
                     │  Dependencies   │   │ Package      │
                     │  (.dll files)   │   │ Manager GUI  │
                     └─────────────────┘   │ (in Rhino)   │
                                           └──────────────┘
```

## Summary

This data model describes the entities and relationships involved in packaging and distributing the Vessel Studio Rhino Plugin via Yak package manager. Key takeaways:

1. **No New Plugin Entities**: This feature doesn't add data structures to the plugin code
2. **External System Integration**: All entities managed by McNeel's Yak CLI and servers
3. **Metadata-Centric**: Success depends on accurate manifest.yml creation
4. **Version Immutability**: Once published, versions cannot be changed (only yanked)
5. **OAuth Security**: Authentication handled securely by external OAuth provider
6. **Distribution Folder**: Temporary staging area for package contents

**Next Steps**: See [plan.md](plan.md) for implementation strategy and [tasks.md](tasks.md) for detailed task breakdown.
