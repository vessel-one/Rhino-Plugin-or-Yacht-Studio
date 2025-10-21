# Implementation Plan: Rhino Package Manager Distribution

**Branch**: `feature-packagemanager` | **Date**: 2025-10-21 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/002-rhino-package-manager/spec.md`

## Summary

This feature enables distribution of the Vessel Studio Rhino Plugin through Rhino's official Package Manager (Yak). Primary requirement: Create a `.yak` package file containing the plugin binary, dependencies, and metadata, then publish it to McNeel's package server for discoverable one-click installation by Rhino users. Technical approach: Use Yak CLI tooling to generate manifest.yml, build .yak archive from Release output, test on test.yak.rhino3d.com, then push to production yak.rhino3d.com. Optional enhancement: Integrate package creation into build.ps1 script for automated releases.

## Technical Context

**Language/Version**: C# .NET Framework 4.8 (existing), PowerShell 5.1+ (build scripts)  
**Primary Dependencies**: 
- Yak CLI (bundled with Rhino 8 at `C:\Program Files\Rhino 8\System\Yak.exe`)
- Existing plugin binary: VesselStudioSimplePlugin.rhp
- Newtonsoft.Json.dll 13.0.3 (must be included in package)
- PNG icon resources (icon_24.png, icon_32.png, icon_48.png)

**Storage**: 
- Local: dist/ folder for package staging
- Remote: McNeel package servers (test.yak.rhino3d.com, yak.rhino3d.com)
- OAuth token: `%APPDATA%\McNeel\yak.yml` (managed by Yak CLI)

**Testing**: 
- Manual verification on test server
- Installation testing in fresh Rhino 8 instance
- Version update flow testing

**Target Platform**: Windows (Rhino 8.0+)  
**Project Type**: Build tooling enhancement (PowerShell scripts) + documentation  
**Performance Goals**: 
- Package creation < 5 minutes manual work
- Package searchable within 30 seconds of push
- Production discoverability within 1 hour

**Constraints**: 
- Version immutability (cannot overwrite published versions)
- Windows-only distribution (rh8_0-win tag)
- No breaking changes to existing plugin functionality
- OAuth token expires every ~30 days

**Scale/Scope**: 
- Single package (vesselstudio)
- 4-5 files per package (.rhp, .dll, manifest.yml, icon.png, optional README)
- Manual publication workflow initially, optional automation in P3

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Security-First Authentication ✅ PASS
- **Requirement**: Authentication MUST prioritize security, use OAuth, no plain text credentials
- **Status**: ✅ Yak CLI handles OAuth 2.0 via Rhino Accounts (external system, secure by design)
- **Compliance**: OAuth token stored in `%APPDATA%\McNeel\yak.yml` (managed by Yak, not our code)
- **Notes**: No custom authentication implementation required; delegates to McNeel's secure system

### II. User Experience Excellence ✅ PASS
- **Requirement**: Native Rhino integration, command-line accessible, visual feedback, <15s operations
- **Status**: ✅ Package Manager is native Rhino feature; installation is GUI-based (Tools > Options)
- **Compliance**: End users experience one-click installation; maintainers use CLI (yak commands)
- **Notes**: This feature enhances UX by enabling discovery and installation without manual file management

### III. Cross-Platform Foundation ⚠️ EXCEPTION JUSTIFIED
- **Requirement**: Eto.Forms for cross-platform, Windows mandatory, macOS architecturally planned
- **Status**: ⚠️ Package distribution is Windows-only (rh8_0-win tag)
- **Exception Justification**: 
  - Existing plugin uses .NET Framework 4.8 + System.Windows.Forms (Windows-only)
  - Package manager distribution does not change plugin architecture
  - macOS support would require complete plugin rewrite (out of scope per OS-001)
  - Distribution tag `rh8_0-win` accurately reflects Windows-only support
- **Future Path**: If plugin is rewritten for cross-platform (Eto.Forms + .NET 6+), can publish separate `rh8_0-mac` package
- **Documented in**: spec.md Assumptions A-005, Out of Scope OS-001

### IV. Resilient Operations ✅ PASS
- **Requirement**: Network loss handling, retry logic, graceful degradation
- **Status**: ✅ Yak CLI handles network resilience internally; test server available for validation
- **Compliance**: 
  - Maintainers can test on test.yak.rhino3d.com before production (prevents bad releases)
  - OAuth token refresh handled by Yak CLI (prompts for re-login on expiration)
  - Version immutability prevents accidental overwrites (server-side enforcement)
- **Notes**: Build script can validate pre-conditions (version consistency, file existence) before push

### V. Measurable Performance ✅ PASS
- **Requirement**: Measurable criteria for all performance requirements
- **Status**: ✅ All success criteria have explicit metrics
- **Compliance**:
  - SC-001: Package creation < 5 minutes
  - SC-002: Searchable within 30 seconds
  - SC-003: Discoverable within 1 hour
  - SC-007: Version update < 10 minutes
  - SC-008: 90% adoption within 3 months
  - SC-009: Zero installation support tickets

**Overall Gate Status**: ✅ **PASS** (1 justified exception documented)

## Project Structure

### Documentation (this feature)

```
specs/002-rhino-package-manager/
├── spec.md              # ✅ Complete (feature specification)
├── plan.md              # ✅ This file (implementation plan)
├── research.md          # ✅ Complete (Yak CLI findings, manifest format)
├── quickstart.md        # ✅ Complete (quick reference)
├── data-model.md        # Phase 1 output (minimal - package structure entities)
├── contracts/
│   └── manifest.yml     # ✅ Complete (sample manifest contract)
└── checklists/
    └── requirements.md  # ✅ Complete (spec validation checklist)
```

### Source Code (repository root)

```
# Option 1: Single project (SELECTED - build tooling enhancement)
VesselStudioSimplePlugin/
├── bin/Release/net48/           # Existing build output
│   ├── VesselStudioSimplePlugin.rhp
│   ├── VesselStudioSimplePlugin.dll
│   ├── Newtonsoft.Json.dll
│   └── VesselStudioSimplePlugin.pdb
├── Resources/                    # Existing icon resources
│   ├── icon_24.png
│   ├── icon_32.png
│   └── icon_48.png
└── [existing plugin source]

dist/                             # NEW: Package staging folder
├── VesselStudioSimplePlugin.rhp  # Copied from bin/Release
├── Newtonsoft.Json.dll           # Copied from bin/Release
├── icon.png                      # Copied from Resources/icon_48.png
├── manifest.yml                  # Generated by yak spec, customized
├── README.md                     # Optional: Installation/usage guide
└── LICENSE.txt                   # Optional: Software license

build.ps1                         # MODIFIED: Add package creation logic
quick-build.ps1                   # No changes needed
update-changelog.ps1              # No changes needed

docs/
└── guides/
    └── PACKAGE_MANAGER_GUIDE.md  # NEW: Maintainer documentation
```

**Structure Decision**: This feature primarily enhances build tooling (PowerShell scripts) and adds documentation. No new C# code required. The `dist/` folder is a temporary staging area for package creation, populated by build script. Final output is a `.yak` file (e.g., `vesselstudio-1.0.0-rh8_0-win.yak`) created by Yak CLI, which is then pushed to package servers.

## Complexity Tracking

*No constitution violations requiring justification.*

The cross-platform exception (Windows-only distribution) is documented above and aligns with the existing plugin architecture. No additional complexity introduced beyond what's already accepted for the plugin itself.

## Phase 0: Research & Decision Validation

**Status**: ✅ **COMPLETE** (research.md already exists with comprehensive findings)

### Research Summary

All technical unknowns have been resolved through official Rhino Developer documentation:

1. **Yak CLI Location**: `C:\Program Files\Rhino 8\System\Yak.exe` (bundled with Rhino)
2. **Manifest Format**: YAML with required fields (name, version, authors, description) and optional fields (url, icon, keywords)
3. **Distribution Tag**: `rh8_0-win` (auto-detected from RhinoCommon reference, platform specified with --platform flag)
4. **Authentication**: OAuth 2.0 via `yak login` command (token stored in `%APPDATA%\McNeel\yak.yml`)
5. **Test Server**: https://test.yak.rhino3d.com (wiped nightly, safe for testing)
6. **Production Server**: https://yak.rhino3d.com (permanent, public)
7. **Version Immutability**: Cannot overwrite/delete published versions (use `yak yank` to unlist)
8. **Package Structure**: ZIP archive with .rhp, dependencies, manifest.yml, icon, optional docs
9. **Icon Format**: PNG 32x32 or 48x48 (we have icon_48.png available)
10. **Dependency Handling**: Include Newtonsoft.Json.dll, exclude RhinoCommon/Eto (Rhino-provided)

**Key Decisions Made**:
- **Package Name**: `vesselstudio` (lowercase, verify availability on first push)
- **Icon**: Use existing `Resources/icon_48.png` as `icon.png` in package
- **Distribution Tag**: `rh8_0-win` (Rhino 8.0+ on Windows)
- **Version Strategy**: Semantic versioning (1.0.0 initial, increment for updates)
- **Test-First Approach**: Always push to test server before production

**Alternatives Considered**:
- Package name "vessel-studio" or "vesselstudiorhino" → Rejected: "vesselstudio" is concise and matches brand
- Include README/LICENSE in initial package → Deferred: Optional for v1.0.0, add in future versions
- Automate publishing in build script (P3) → Deferred: Manual process for initial releases, automation later

**Reference**: See [research.md](research.md) for complete findings with citations to Rhino Developer docs.

## Phase 1: Design & Contracts

### Data Model

**Status**: ✅ **COMPLETE**

See [data-model.md](data-model.md) for detailed entity definitions. Key entities:

1. **Yak Package** (.yak file)
   - Archive containing .rhp + dependencies + metadata
   - Naming: `{name}-{version}-{distribution_tag}.yak`
   - Example: `vesselstudio-1.0.0-rh8_0-win.yak`

2. **Manifest** (manifest.yml)
   - YAML metadata file
   - Required: name, version, authors, description
   - Optional: url, icon, keywords
   - See [contracts/manifest.yml](contracts/manifest.yml) for template

3. **Distribution Tag**
   - Format: `rh{major}_{minor}-{platform}`
   - Ours: `rh8_0-win` (Rhino 8.0+ on Windows)

4. **OAuth Token**
   - Storage: `%APPDATA%\McNeel\yak.yml`
   - Lifespan: ~30 days
   - Managed by Yak CLI

5. **Distribution Folder** (dist/)
   - Staging area for package contents
   - Populated by build script
   - Input for `yak build` command

### API Contracts

**Status**: ✅ **COMPLETE**

This feature interacts with McNeel's package servers via Yak CLI commands (not direct HTTP API). Contract is defined by Yak CLI command interface:

**Commands Used**:
```powershell
# Authentication
yak login
# Output: Opens browser for OAuth flow
# Side effect: Token saved to %APPDATA%\McNeel\yak.yml

# Manifest generation
yak spec
# Input: Distribution folder with .rhp file
# Output: manifest.yml with auto-detected metadata
# Requires: Manual customization of description, URL, keywords

# Package building
yak build [--platform win|mac|any]
# Input: Distribution folder with manifest.yml
# Output: {name}-{version}-{distribution_tag}.yak file
# Auto-detects: Distribution tag from RhinoCommon version

# Package publishing
yak push <package.yak> [--source <server_url>]
# Input: .yak package file
# Output: Success/failure message
# Side effects: Package uploaded to server, becomes searchable
# Default server: https://yak.rhino3d.com
# Test server: https://test.yak.rhino3d.com (use --source flag)

# Package searching
yak search <query> [--all] [--prerelease] [--source <server_url>]
# Input: Search query string
# Output: List of matching packages with name, version
# Flags: --all (show all versions), --prerelease (include pre-releases)

# Package yanking (unlisting)
yak yank <package> <version>
# Input: Package name and version
# Output: Success/failure message
# Side effect: Version unlisted from search (still installable if URL known)
```

**Contract File**: See [contracts/manifest.yml](contracts/manifest.yml) for YAML schema.

### Quickstart Guide

**Status**: ✅ **COMPLETE**

See [quickstart.md](quickstart.md) for rapid onboarding guide covering:
- What this feature does (package manager integration)
- Why it matters (one-click installation vs manual file management)
- Key concepts (Yak, .yak files, distribution tags, manifest)
- Quick commands (login, spec, build, push, search)
- Workflow overview (7-step process)
- File requirements and exclusions
- Version strategy (semantic versioning, immutability)
- Success metrics

### Agent Context Update

**Status**: Ready for execution after plan completion

The `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot` script will:
1. Detect we're using GitHub Copilot (based on .github/copilot-instructions.md)
2. Update .github/copilot-instructions.md with new technologies from this plan
3. Preserve manual additions between `<!-- MANUAL ADDITIONS START/END -->` markers
4. Add: PowerShell 5.1+ scripting, Yak CLI tooling, McNeel package server integration

**Technologies to Add**:
- Yak CLI (Rhino Package Manager tooling)
- YAML manifest format
- McNeel package server infrastructure
- PowerShell 5.1+ (build script enhancements)

## Phase 2: Task Breakdown

**Status**: ⏳ **DEFERRED TO `/speckit.tasks` COMMAND**

Task breakdown and implementation details will be generated by running `/speckit.tasks` after this plan is complete. Expected task categories:

1. **P1: Initial Package Publication**
   - Create dist/ folder structure
   - Generate and customize manifest.yml
   - Copy files to dist/ (rhp, dll, icon)
   - Authenticate with yak login
   - Build package with yak build
   - Test push to test server
   - Validate searchability
   - Production push

2. **P1: User Installation Validation**
   - Test installation from Package Manager GUI
   - Verify plugin commands available
   - Test toolbar panel display
   - Document user installation process

3. **P2: Update Publishing Workflow**
   - Document version increment process
   - Test update publishing flow
   - Verify update notifications
   - Document maintainer update process

4. **P3: Build Script Integration (Optional)**
   - Add -CreatePackage parameter to build.ps1
   - Implement dist/ folder population
   - Add version consistency validation
   - Add yak build integration
   - Add optional -PushPackage parameter
   - Update build documentation

5. **Documentation**
   - Create PACKAGE_MANAGER_GUIDE.md for maintainers
   - Update README.md with installation instructions
   - Add LICENSE.txt (if required)
   - Update CHANGELOG.md

## Implementation Strategy

### MVP Scope (P1 - Initial Publication)

**Goal**: Get vesselstudio package published to yak.rhino3d.com and installable by users

**Tasks**:
1. Create dist/ folder and populate with files
2. Generate manifest.yml using `yak spec`
3. Customize manifest with accurate metadata
4. Authenticate with `yak login`
5. Build package with `yak build`
6. Test on test.yak.rhino3d.com
7. Push to production yak.rhino3d.com
8. Verify installation in fresh Rhino instance

**Deliverable**: vesselstudio-1.0.0-rh8_0-win.yak published and searchable

**Timeline**: Single session (2-3 hours manual work)

### P2 Scope (Update Publishing)

**Goal**: Establish process for publishing plugin updates

**Tasks**:
1. Increment version in AssemblyInfo.cs
2. Rebuild plugin
3. Update manifest.yml version
4. Build new package
5. Push to production
6. Verify update notification in Rhino

**Deliverable**: Documented update workflow + tested update (1.0.1 or 1.1.0)

**Timeline**: Single session (1-2 hours after version change)

### P3 Scope (Build Automation - Optional)

**Goal**: Automate package creation in build.ps1

**Tasks**:
1. Add -CreatePackage switch parameter
2. Implement dist/ folder creation logic
3. Add file copying (rhp, dll, icon)
4. Add version validation
5. Integrate yak build call
6. Add -PushPackage option (future)
7. Update build documentation

**Deliverable**: Enhanced build.ps1 with package creation

**Timeline**: Single session (3-4 hours development + testing)

## Success Criteria Validation

All success criteria from spec.md are testable and measurable:

- ✅ **SC-001**: Package creation time < 5 minutes → Measure with stopwatch during MVP
- ✅ **SC-002**: Searchable within 30 seconds → Test with `yak search` after push
- ✅ **SC-003**: Discoverable within 1 hour → Check Package Manager GUI after production push
- ✅ **SC-004**: Searchable by keywords → Test "Vessel Studio", "vessel", "yacht" in GUI
- ✅ **SC-005**: Installation success → Test `VesselCapture` command after install
- ✅ **SC-006**: Update notifications → Test after pushing version 1.0.1
- ✅ **SC-007**: Update time < 10 minutes → Measure during P2 testing
- ✅ **SC-008**: 90% adoption within 3 months → Track via support tickets + download analytics
- ✅ **SC-009**: Zero installation support tickets → Monitor support channel
- ✅ **SC-010**: Version validation catches mismatches → Test in P3 build script

## Risk Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Package name "vesselstudio" already taken | Low | High | Check availability on first push; have backup names ready ("vessel-studio", "vesselstudiorhino") |
| Version mismatch between files | Medium | Medium | Add validation in build script (P3) or manual checklist (P1) |
| Manifest.yml syntax errors | Medium | Low | Validate YAML with linter before build; test on test server first |
| OAuth token expiration during work | Low | Low | Re-authenticate with `yak login` (takes 30-60 seconds) |
| Test server unavailable | Low | Low | Skip test push, go direct to production (not recommended but possible) |
| Icon missing or wrong format | Low | Low | Pre-validate icon.png exists and is 48x48 PNG |
| Newtonsoft.Json.dll version conflict | Low | Medium | Document in README; users report if issues arise |
| Bad package published | Low | High | Use `yak yank` to unlist; push fixed version with incremented number |

## Next Steps

1. ✅ **Review this plan** with stakeholders
2. ⏭️ **Run `/speckit.tasks`** to generate detailed task breakdown
3. ⏭️ **Execute P1 tasks** (Initial Package Publication)
4. ⏭️ **Test installation** in fresh Rhino instance
5. ⏭️ **Document process** in PACKAGE_MANAGER_GUIDE.md
6. ⏭️ **Execute P2 tasks** (Update Publishing) after first release
7. 🔮 **Consider P3** (Build Automation) after manual process proven

---

**Plan Status**: ✅ **COMPLETE AND READY FOR TASK GENERATION**

Run `/speckit.tasks` to proceed to detailed implementation tasks.
