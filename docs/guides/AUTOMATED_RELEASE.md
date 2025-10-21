# Automated Release Workflow

Complete automation for releasing Vessel Studio Rhino Plugin updates, from changelog analysis to package publishing.

## Overview

The release workflow consists of 4 automation scripts that work together:

1. **`update-version.ps1`** - Analyzes CHANGELOG.md and updates version across all files
2. **`create-package.ps1`** - Builds dist/ folder and creates .yak package
3. **`publish-package.ps1`** - Pushes package to Rhino Package Manager
4. **`release.ps1`** - Master script that orchestrates all steps

## Quick Start

### Simple Release (Full Automation)

```powershell
# 1. Make your code changes
# 2. Update CHANGELOG.md [Unreleased] section with your changes
# 3. Run the release script

.\release.ps1
```

This will:
- Auto-detect version bump from CHANGELOG.md
- Update version in all files
- Build the plugin
- Create the .yak package
- Publish to production (with confirmation)

### Test Server Release

```powershell
.\release.ps1 -TestServer
```

Publishes to `test.yak.rhino3d.com` (wiped nightly) for testing.

### Explicit Version

```powershell
.\release.ps1 -Version "1.2.3"
```

Override auto-detection and set specific version.

---

## Semantic Versioning Rules

The `update-version.ps1` script analyzes your CHANGELOG.md [Unreleased] section to determine the version bump:

| CHANGELOG Section | Version Bump | Example |
|-------------------|--------------|---------|
| `### BREAKING` or `### Breaking Changes` | **MAJOR** | 1.0.0 → 2.0.0 |
| `### Added` or `### Changed` or `### Deprecated` | **MINOR** | 1.0.0 → 1.1.0 |
| `### Fixed` or `### Security` or `### Removed` | **PATCH** | 1.0.0 → 1.0.1 |

### Example CHANGELOG.md

```markdown
## [Unreleased]

### Added
- New AI chat interface for design assistance
- Batch screenshot capture for multiple viewports

### Fixed
- Viewport capture crash on ultra-wide monitors
- API timeout with slow network connections

### Changed
- Improved error messages with actionable suggestions
```

This would bump **MINOR** version (Added takes precedence): `1.0.1` → `1.1.0`

---

## Individual Scripts

### 1. update-version.ps1

**Purpose**: Analyzes CHANGELOG.md and updates version across all files.

```powershell
# Auto-detect from CHANGELOG.md
.\update-version.ps1

# Set explicit version
.\update-version.ps1 -NewVersion "1.2.3"

# Dry run (show what would change)
.\update-version.ps1 -DryRun

# Skip confirmation
.\update-version.ps1 -Force
```

**What it updates**:
- `VesselStudioSimplePlugin/Properties/AssemblyInfo.cs` → `1.2.3.0`
- `VesselStudioSimplePlugin/VesselStudioSimplePlugin.csproj` → `1.2.3`
- `docs/reference/CHANGELOG.md` → Moves `[Unreleased]` to `[1.2.3] - 2025-10-21`

**Output**:
```
=== Vessel Studio Version Update ===

Step 1: Reading current version...
✓ Current version: 1.0.1

Step 2: Determining next version...
✓ Detected changes:
  - Added
  - Fixed
  → Bump type: MINOR
  → Next version: 1.1.0

Step 3: Version update plan
  Current: 1.0.1
  Next:    1.1.0

Continue with version update? (Y/n)

Step 4: Updating AssemblyInfo.cs...
✓ Updated AssemblyInfo.cs → 1.1.0.0

Step 5: Updating .csproj...
✓ Updated .csproj → 1.1.0

Step 6: Updating CHANGELOG.md...
✓ Updated CHANGELOG.md → [1.1.0] - 2025-10-21

=== Version Update Complete ===

Updated version: 1.0.1 → 1.1.0

Next steps:
  1. Run: .\build.ps1 -Configuration Release
  2. Run: .\create-package.ps1
  3. Run: .\publish-package.ps1

Or use master script: .\release.ps1
```

---

### 2. create-package.ps1

**Purpose**: Builds plugin, copies files to dist/, creates .yak package.

```powershell
# Auto-detect version from AssemblyInfo.cs
.\create-package.ps1

# Use existing build (skip build step)
.\create-package.ps1 -SkipBuild

# Explicit version
.\create-package.ps1 -Version "1.2.3"
```

**What it does**:
1. Determines version from AssemblyInfo.cs (or -Version parameter)
2. Builds plugin with `build.ps1` (unless -SkipBuild)
3. Creates/cleans `dist/` folder
4. Copies files to dist/:
   - `VesselStudioSimplePlugin.rhp` (57 KB)
   - `Newtonsoft.Json.dll` (712 KB)
   - `icon.png` (1 KB)
5. Creates `dist/manifest.yml` with correct version
6. Creates `VesselStudio-1.2.3-rh8_0-win.yak` (ZIP archive)
7. Verifies package contents

**Output**:
```
=== Vessel Studio Package Creation ===

Step 1: Determining version...
✓ Read version from AssemblyInfo.cs: 1.1.0

Step 2: Building plugin...
[build.ps1 output...]
✓ Build completed successfully

Step 3: Preparing dist/ folder...
✓ dist/ folder ready

Step 4: Copying files to dist/...
  ✓ VesselStudioSimplePlugin.rhp (56.5 KB)
  ✓ Newtonsoft.Json.dll (695.27 KB)
  ✓ icon.png (1078 bytes)

Step 5: Creating manifest.yml...
✓ manifest.yml created with version 1.1.0

Step 6: Creating .yak package...
✓ Created VesselStudio-1.1.0-rh8_0-win.yak (294.13 KB)

Step 7: Verifying package contents...
  ✓ VesselStudioSimplePlugin.rhp
  ✓ Newtonsoft.Json.dll
  ✓ manifest.yml
  ✓ icon.png (optional)

=== Package Creation Complete ===

Package: VesselStudio-1.1.0-rh8_0-win.yak
Location: C:\...\Yacht Studio Rhino Plugin\VesselStudio-1.1.0-rh8_0-win.yak
Version: 1.1.0

Next steps:
  Test server: .\publish-package.ps1 -TestServer
  Production:  .\publish-package.ps1
```

---

### 3. publish-package.ps1

**Purpose**: Pushes .yak package to Rhino Package Manager.

```powershell
# Publish to production (auto-finds latest .yak)
.\publish-package.ps1

# Publish to test server
.\publish-package.ps1 -TestServer

# Explicit package file
.\publish-package.ps1 -PackageFile "VesselStudio-1.2.3-rh8_0-win.yak"

# Skip confirmation (dangerous for production!)
.\publish-package.ps1 -Force
```

**What it does**:
1. Locates Yak CLI (standalone or bundled)
2. Finds .yak package (auto-detects latest or use -PackageFile)
3. Checks authentication token (age warning if > 25 days)
4. Confirms publishing (extra confirmation for production)
5. Pushes package to server
6. Verifies package is searchable

**Output (Test Server)**:
```
=== Vessel Studio Package Publishing ===

Step 1: Locating Yak CLI...
✓ Found Yak CLI: C:\Users\...\AppData\Local\Temp\yak-standalone.exe

Step 2: Locating package file...
✓ Found package: VesselStudio-1.1.0-rh8_0-win.yak
  Size: 294.13 KB
  Version: 1.1.0

Step 3: Checking authentication...
✓ Authenticated (5 days ago)

Step 4: Publishing confirmation
  Target: TEST SERVER (wiped nightly)
  Package: VesselStudio-1.1.0-rh8_0-win.yak

Push to TEST server? (Y/n)

Step 5: Pushing package to test.yak.rhino3d.com...
  Command: & "...\yak-standalone.exe" push "...\VesselStudio-1.1.0-rh8_0-win.yak" --source https://test.yak.rhino3d.com
  
  Pushing VesselStudio 1.1.0 to https://test.yak.rhino3d.com...
  Package uploaded successfully

✓ Package pushed successfully

Step 6: Verifying package is searchable...
✓ Package found in search results

  VesselStudio 1.1.0

=== Publishing Complete ===

Published to TEST server: https://test.yak.rhino3d.com
⚠ This server is wiped nightly - for testing only

To publish to production:
  .\publish-package.ps1
```

**Output (Production)**:
```
Step 4: Publishing confirmation
  Target: PRODUCTION SERVER
  Package: VesselStudio-1.1.0-rh8_0-win.yak

⚠ THIS WILL PUBLISH TO PRODUCTION!
  All Rhino 8 users worldwide will see this package

Are you sure you want to publish to PRODUCTION? (yes/N) yes

[... publishing ...]

=== Publishing Complete ===

Published to PRODUCTION: https://yak.rhino3d.com

Package details:
  Name: VesselStudio
  Version: 1.1.0
  Search: https://www.rhino3d.com/packages/search/?q=vesselstudio

Users can now install via:
  - Rhino 8 → Tools → Options → Package Manager → Search: VesselStudio
  - Command line: _PackageManager

To yank this version (if needed):
  & "...\yak-standalone.exe" yank VesselStudio 1.1.0
```

---

### 4. release.ps1 (Master Script)

**Purpose**: Orchestrates entire release workflow.

```powershell
# Full automation (all steps)
.\release.ps1

# Publish to test server
.\release.ps1 -TestServer

# Explicit version
.\release.ps1 -Version "1.2.3"

# Dry run (show what would happen)
.\release.ps1 -DryRun

# Skip individual phases
.\release.ps1 -SkipVersionUpdate  # Use current version
.\release.ps1 -SkipBuild          # Use existing binaries
.\release.ps1 -SkipPackage        # Use existing .yak
.\release.ps1 -SkipPublish        # Create package only

# Combine flags
.\release.ps1 -Version "1.2.3" -SkipVersionUpdate -TestServer
```

**What it does**:
1. **Phase 1: Version Management** - Runs `update-version.ps1`
2. **Phase 2: Build** - Runs `build.ps1 -SkipChangelog`
3. **Phase 3: Package Creation** - Runs `create-package.ps1`
4. **Phase 4: Publishing** - Runs `publish-package.ps1`

**Output**:
```
╔════════════════════════════════════════════════════════════════╗
║                                                                ║
║        VESSEL STUDIO RHINO PLUGIN - AUTOMATED RELEASE         ║
║                                                                ║
╚════════════════════════════════════════════════════════════════╝

═══════════════════════════════════════════════════════════════
PHASE 1: VERSION MANAGEMENT
═══════════════════════════════════════════════════════════════

→ Running update-version.ps1...

[update-version.ps1 output...]

✓ Phase 1 Complete - Version: 1.1.0

═══════════════════════════════════════════════════════════════
PHASE 2: BUILD
═══════════════════════════════════════════════════════════════

→ Running build.ps1...

[build.ps1 output...]

✓ Phase 2 Complete - Build successful

═══════════════════════════════════════════════════════════════
PHASE 3: PACKAGE CREATION
═══════════════════════════════════════════════════════════════

→ Running create-package.ps1...

[create-package.ps1 output...]

✓ Phase 3 Complete - Package created

═══════════════════════════════════════════════════════════════
PHASE 4: PUBLISHING
═══════════════════════════════════════════════════════════════

→ Running publish-package.ps1...

[publish-package.ps1 output...]

✓ Phase 4 Complete - Publishing successful

╔════════════════════════════════════════════════════════════════╗
║                                                                ║
║                    RELEASE COMPLETE ✓                          ║
║                                                                ║
╚════════════════════════════════════════════════════════════════╝

Release Details:
  Version:  1.1.0
  Package:  VesselStudio-1.1.0-rh8_0-win.yak
  Target:   PRODUCTION
  Duration: 02:34

Production Server:
  URL: https://yak.rhino3d.com
  Search: https://www.rhino3d.com/packages/search/?q=vesselstudio

Users can install via:
  Rhino 8 → Tools → Options → Package Manager → Search: VesselStudio

To yank this version if needed:
  & "...\yak-standalone.exe" yank VesselStudio 1.1.0

Next Steps:
  1. Git commit: git add -A && git commit -m "chore: Release v1.1.0"
  2. Git tag:    git tag -a v1.1.0 -m "Release v1.1.0"
  3. Git push:   git push && git push --tags
```

---

## Complete Workflow Example

### Scenario: You've fixed a bug and want to release an update

```powershell
# 1. Make your code changes in Visual Studio
#    (e.g., fix viewport capture crash)

# 2. Update CHANGELOG.md
```

Edit `docs/reference/CHANGELOG.md`:
```markdown
## [Unreleased]

### Fixed
- Viewport capture crash on ultra-wide monitors with aspect ratios > 3:1
- API timeout with slow network connections (increased timeout to 60s)
```

```powershell
# 3. Run automated release
.\release.ps1
```

**What happens:**
1. Script reads CHANGELOG.md
2. Detects `### Fixed` → PATCH bump
3. Updates version: `1.0.1` → `1.0.2`
4. Updates AssemblyInfo.cs, .csproj, CHANGELOG.md
5. Builds plugin in Release mode
6. Creates dist/ folder with all files
7. Creates VesselStudio-1.0.2-rh8_0-win.yak
8. Asks: "Are you sure you want to publish to PRODUCTION? (yes/N)"
9. You type: `yes`
10. Pushes to yak.rhino3d.com
11. Verifies package is searchable
12. Shows completion summary

**Result**: Version 1.0.2 is live on Rhino Package Manager within ~3 minutes!

```powershell
# 4. Commit and tag
git add -A
git commit -m "chore: Release v1.0.2"
git tag -a v1.0.2 -m "Release v1.0.2 - Bug fixes"
git push && git push --tags
```

---

## Error Handling

### If version update fails:

```
✗ No changes detected in [Unreleased] section
  CHANGELOG.md should have at least one of: Added, Changed, Fixed, BREAKING
  Use -NewVersion parameter to set version manually
```

**Solution**: Either update CHANGELOG.md or use explicit version:
```powershell
.\release.ps1 -Version "1.0.2"
```

### If build fails:

```
✗ Build failed
```

**Solution**: Fix build errors in Visual Studio, then retry:
```powershell
.\release.ps1 -SkipVersionUpdate
```

### If package creation fails:

```
✗ VesselStudioSimplePlugin.rhp not found at: ...\bin\Release\net48\...
  Run build.ps1 first or use -Configuration to specify build type
```

**Solution**: Ensure build succeeded:
```powershell
.\build.ps1 -Configuration Release
.\release.ps1 -SkipVersionUpdate -SkipBuild
```

### If publishing fails:

```
✗ Not authenticated with Yak
```

**Solution**: Authenticate with Rhino Accounts:
```powershell
& "$env:TEMP\yak-standalone.exe" login
```

---

## Advanced Usage

### Test Server → Production Workflow

```powershell
# 1. Test on test server first
.\release.ps1 -TestServer

# 2. Verify in Rhino 8 test installation

# 3. Publish same version to production (skip rebuilding)
.\release.ps1 -SkipVersionUpdate -SkipBuild -SkipPackage
```

### Rollback a Release

If you publish a bad version:

```powershell
# Yank the bad version
& "$env:TEMP\yak-standalone.exe" yank VesselStudio 1.2.3

# Release a hotfix
# Edit CHANGELOG.md with fix
.\release.ps1
```

### CI/CD Integration

For GitHub Actions or similar:

```powershell
# Non-interactive release
.\release.ps1 -Force -Version $env:RELEASE_VERSION
```

---

## File Structure

After running `create-package.ps1`, you'll have:

```
Yacht Studio Rhino Plugin/
├── dist/                              # Package staging (gitignored)
│   ├── VesselStudioSimplePlugin.rhp   # 57 KB - Plugin binary
│   ├── Newtonsoft.Json.dll            # 712 KB - Dependency
│   ├── icon.png                       # 1 KB - Package icon
│   └── manifest.yml                   # 543 bytes - Package metadata
├── VesselStudio-1.0.1-rh8_0-win.yak  # Final package (ZIP)
├── update-version.ps1                 # Version management
├── create-package.ps1                 # Package creation
├── publish-package.ps1                # Publishing
├── release.ps1                        # Master automation
└── build.ps1                          # Build script
```

---

## Troubleshooting

### "Yak CLI not found"

Download standalone Yak CLI:
```powershell
Invoke-WebRequest -Uri "https://files.mcneel.com/yak/tools/0.13.0/yak.exe" -OutFile "$env:TEMP\yak-standalone.exe"
```

### "Token is 29 days old (expires ~30 days)"

Re-authenticate:
```powershell
& "$env:TEMP\yak-standalone.exe" login
```

### "Package not found in search yet"

Wait 10-30 seconds and search again. The package server may be indexing.

### "Version 1.2.3 already exists"

You cannot re-publish the same version. Either:
1. Yank the existing version: `yak yank VesselStudio 1.2.3`
2. Increment to next version: `.\release.ps1 -Version "1.2.4"`

---

## Parameters Reference

### update-version.ps1

| Parameter | Type | Description |
|-----------|------|-------------|
| `-NewVersion` | String | Explicit version (e.g., "1.2.3") - overrides auto-detection |
| `-DryRun` | Switch | Show what would be updated without making changes |
| `-Force` | Switch | Skip confirmation prompts |
| `-ChangelogPath` | String | Path to CHANGELOG.md (default: docs/reference/CHANGELOG.md) |
| `-AssemblyInfoPath` | String | Path to AssemblyInfo.cs |
| `-CsprojPath` | String | Path to .csproj file |

### create-package.ps1

| Parameter | Type | Description |
|-----------|------|-------------|
| `-Version` | String | Version to package (defaults to AssemblyInfo.cs) |
| `-SkipBuild` | Switch | Skip build step (use existing binaries) |
| `-Configuration` | String | Build configuration (default: "Release") |
| `-OutputDir` | String | Output directory (default: dist/) |
| `-BinDir` | String | Binary directory (default: bin/Release/net48/) |
| `-ResourcesDir` | String | Resources directory (default: Resources/) |

### publish-package.ps1

| Parameter | Type | Description |
|-----------|------|-------------|
| `-PackageFile` | String | Path to .yak file (auto-detect if not provided) |
| `-TestServer` | Switch | Push to test.yak.rhino3d.com (wiped nightly) |
| `-Force` | Switch | Skip confirmation prompts |
| `-YakPath` | String | Path to Yak CLI (default: $env:TEMP\yak-standalone.exe) |

### release.ps1

| Parameter | Type | Description |
|-----------|------|-------------|
| `-Version` | String | Explicit version (e.g., "1.2.0") - overrides auto-detection |
| `-TestServer` | Switch | Publish to test server instead of production |
| `-SkipVersionUpdate` | Switch | Skip version update (use current version) |
| `-SkipBuild` | Switch | Skip build step (use existing binaries) |
| `-SkipPackage` | Switch | Skip package creation (use existing .yak) |
| `-SkipPublish` | Switch | Skip publishing (create package only) |
| `-DryRun` | Switch | Show what would happen without making changes |
| `-Force` | Switch | Skip all confirmation prompts |
| `-Configuration` | String | Build configuration (default: "Release") |

---

## Best Practices

1. **Always update CHANGELOG.md first** - This drives the version bump logic
2. **Test on test server** - Use `-TestServer` flag before production
3. **Review changes before confirming** - Scripts pause for confirmation
4. **Use semantic versioning** - Follow the MAJOR.MINOR.PATCH rules
5. **Commit and tag releases** - Use git tags for version history
6. **Keep token fresh** - Re-authenticate every 25 days
7. **Use dry run for planning** - `-DryRun` shows what will change

---

## Summary

| Task | Command | Time |
|------|---------|------|
| Update code + CHANGELOG → Release | `.\release.ps1` | ~3 min |
| Test release first | `.\release.ps1 -TestServer` | ~3 min |
| Emergency hotfix | Edit CHANGELOG → `.\release.ps1` | ~3 min |
| Rollback bad version | `yak yank VesselStudio X.Y.Z` | ~30 sec |

**Total automation**: Edit code → Update CHANGELOG → Run one command → Live on Package Manager!
