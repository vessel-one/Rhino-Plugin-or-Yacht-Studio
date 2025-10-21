# 🚀 Quick Release Guide

## TL;DR - Complete Release in 3 Steps

```powershell
# 1. Edit CHANGELOG.md [Unreleased] section with your changes
# 2. Run release script
.\release.ps1

# 3. Done! Package is live on Rhino Package Manager
```

---

## Semantic Versioning (Auto-Detected)

| CHANGELOG Section | Version Bump | Example |
|-------------------|--------------|---------|
| `### BREAKING` | MAJOR | 1.0.0 → **2.0.0** |
| `### Added` or `### Changed` | MINOR | 1.0.0 → **1.1.0** |
| `### Fixed` | PATCH | 1.0.0 → **1.0.1** |

---

## Common Commands

```powershell
# Full release (auto version from CHANGELOG)
.\release.ps1

# Test server first (recommended)
.\release.ps1 -TestServer

# Explicit version
.\release.ps1 -Version "1.2.3"

# See what would happen (no changes)
.\release.ps1 -DryRun

# Skip build (faster, use existing binaries)
.\release.ps1 -SkipBuild

# Create package only (no publish)
.\release.ps1 -SkipPublish
```

---

## Individual Scripts

| Script | Purpose | Usage |
|--------|---------|-------|
| `update-version.ps1` | Update version across all files | `.\update-version.ps1` or `.\update-version.ps1 -Version "1.2.3"` |
| `create-package.ps1` | Build and create .yak package | `.\create-package.ps1` |
| `publish-package.ps1` | Push to Package Manager | `.\publish-package.ps1` or `.\publish-package.ps1 -TestServer` |
| **`release.ps1`** | **Run all steps** | **`.\release.ps1`** |

---

## What Gets Automated

✅ **Version Detection** - Analyzes CHANGELOG.md [Unreleased] section  
✅ **File Updates** - AssemblyInfo.cs, .csproj, CHANGELOG.md  
✅ **Build** - Compiles plugin in Release mode  
✅ **Package Creation** - Creates dist/ folder and .yak archive  
✅ **Publishing** - Pushes to yak.rhino3d.com  
✅ **Verification** - Confirms package is searchable  

---

## Example Workflow

### Bug Fix Release

```markdown
## [Unreleased]

### Fixed
- Viewport capture crash on ultra-wide monitors
```

```powershell
.\release.ps1
# → Bumps PATCH: 1.0.1 → 1.0.2
# → Builds, packages, publishes
# → Live in ~3 minutes
```

### Feature Release

```markdown
## [Unreleased]

### Added
- AI chat interface for design assistance
- Batch screenshot capture

### Changed
- Improved error messages
```

```powershell
.\release.ps1
# → Bumps MINOR: 1.0.2 → 1.1.0
# → Builds, packages, publishes
# → Live in ~3 minutes
```

### Breaking Change Release

```markdown
## [Unreleased]

### BREAKING
- Removed deprecated VesselOldCapture command
- Changed API endpoint structure

### Added
- New REST API v2 with better authentication
```

```powershell
.\release.ps1
# → Bumps MAJOR: 1.1.0 → 2.0.0
# → Builds, packages, publishes
# → Live in ~3 minutes
```

---

## Troubleshooting

### "No changes detected in [Unreleased] section"
→ Add one of: `### Added`, `### Changed`, `### Fixed`, `### BREAKING`

### "Yak CLI not found"
→ Download: `Invoke-WebRequest -Uri "https://files.mcneel.com/yak/tools/0.13.0/yak.exe" -OutFile "$env:TEMP\yak-standalone.exe"`

### "Not authenticated with Yak"
→ Login: `& "$env:TEMP\yak-standalone.exe" login`

### "Version X.Y.Z already exists"
→ Cannot re-publish same version. Either yank it or increment version.

---

## Rollback a Bad Release

```powershell
# 1. Yank the bad version
& "$env:TEMP\yak-standalone.exe" yank VesselStudio 1.2.3

# 2. Release hotfix
# Edit CHANGELOG.md with ### Fixed section
.\release.ps1
# → Auto-bumps to 1.2.4
```

---

## Full Documentation

See [AUTOMATED_RELEASE.md](./docs/guides/AUTOMATED_RELEASE.md) for:
- Complete parameter reference
- Advanced usage patterns
- Error handling guide
- CI/CD integration
- Best practices

---

## Before You Start

**One-time setup:**
1. Download Yak CLI: `Invoke-WebRequest -Uri "https://files.mcneel.com/yak/tools/0.13.0/yak.exe" -OutFile "$env:TEMP\yak-standalone.exe"`
2. Authenticate: `& "$env:TEMP\yak-standalone.exe" login`
3. Test: `.\release.ps1 -TestServer` (wiped nightly)
4. Production: `.\release.ps1`

**Token expires ~30 days** - Re-run login when needed.

---

**Time to Release**: Edit code → Update CHANGELOG → `.\release.ps1` → **~3 minutes total**
