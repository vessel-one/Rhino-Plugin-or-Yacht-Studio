# Feature Specification: Rhino Package Manager Distribution

**Feature Branch**: `feature-packagemanager`  
**Created**: October 21, 2025  
**Status**: Draft  
**Input**: "get this rhino plugin into the package manager and be able to easily push updates to it or our users"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Initial Package Publication (Priority: P1)

As a plugin maintainer, I want to publish the Vessel Studio Rhino Plugin to the official Rhino Package Manager so users can discover and install it directly from within Rhino without manual file downloads.

**Why this priority**: This is the foundation of package distribution. Without initial publication, no other package manager functionality is possible. This provides immediate value by making the plugin discoverable and installable through Rhino's built-in package manager.

**Independent Test**: Can be fully tested by running `yak build` to create a package file, then `yak push` to publish it to the test server. Success is verified by searching for the package using `yak search` and seeing it appear in results.

**Acceptance Scenarios**:

1. **Given** the plugin is built in Release mode with all required files, **When** I run `yak spec` in the distribution folder, **Then** a valid `manifest.yml` file is generated with correct plugin metadata (name, version, description, authors, URL, keywords)

2. **Given** a valid `manifest.yml` exists, **When** I run `yak build` in the distribution folder, **Then** a `.yak` package file is created with the correct distribution tag (e.g., `vesselstudio-1.0.0-rh8_0-win.yak`) containing the .rhp file, dependencies, manifest, and optional assets

3. **Given** I am authenticated with `yak login`, **When** I run `yak push vesselstudio-1.0.0-rh8_0-win.yak --source https://test.yak.rhino3d.com`, **Then** the package is successfully pushed to the test server with no errors

4. **Given** the package is on the test server, **When** I run `yak search --source https://test.yak.rhino3d.com --all vesselstudio`, **Then** the package appears in search results with correct name and version

5. **Given** the package is validated on the test server, **When** I run `yak push vesselstudio-1.0.0-rh8_0-win.yak` (production server), **Then** the package is published to the production Rhino Package Manager and becomes discoverable to all Rhino users

---

### User Story 2 - User Installation from Package Manager (Priority: P1)

As a Rhino user, I want to discover and install the Vessel Studio plugin directly from Rhino's Package Manager GUI so I can start using it without downloading files manually or knowing file paths.

**Why this priority**: This is the primary user-facing value of package manager integration. Users should be able to install the plugin through Rhino's UI as easily as installing a browser extension. This is co-priority P1 because publication is meaningless without installation capability.

**Independent Test**: Can be tested by opening Rhino, navigating to Package Manager, searching for "Vessel Studio", and clicking Install. Success is verified when the plugin commands become available in Rhino's command line.

**Acceptance Scenarios**:

1. **Given** I have Rhino 8 open, **When** I open the Package Manager (Tools > Options > Package Manager or `_PackageManager` command), **Then** I see a searchable interface listing available packages

2. **Given** the Package Manager is open, **When** I search for "Vessel Studio" or "vessel" or "yacht", **Then** the Vessel Studio Rhino Plugin appears in results with its icon, description, version, and authors

3. **Given** I find the Vessel Studio plugin in search results, **When** I click "Install" and confirm, **Then** the package downloads, installs automatically, and I see a success message

4. **Given** the plugin is installed, **When** I type `VesselCapture` in Rhino's command line, **Then** the command is recognized and the capture dialog opens (confirming plugin loaded successfully)

5. **Given** the plugin is installed, **When** I check Tools > Toolbars or run `VesselStudioShowToolbar`, **Then** the Vessel Studio toolbar panel is available

---

### User Story 3 - Publishing Plugin Updates (Priority: P2)

As a plugin maintainer, I want to publish new versions of the plugin through the package manager so existing users automatically receive update notifications and can upgrade seamlessly.

**Why this priority**: After initial publication, the ability to push updates is critical for bug fixes, feature additions, and user retention. This is P2 because it requires initial publication (P1) to be complete first, but is essential for long-term maintenance.

**Independent Test**: Can be tested by incrementing the version number in AssemblyInfo.cs, rebuilding, creating a new .yak package with the updated version, and pushing it. Success is verified when users see an update notification in Rhino.

**Acceptance Scenarios**:

1. **Given** I have made changes to the plugin code, **When** I update the version number in `AssemblyInfo.cs` (e.g., from 1.0.0.0 to 1.1.0.0), **Then** the assembly is rebuilt with the new version

2. **Given** the new version is built, **When** I update `manifest.yml` with the new version number and run `yak build`, **Then** a new package file is created with the updated version in its filename (e.g., `vesselstudio-1.1.0-rh8_0-win.yak`)

3. **Given** I have a new package version, **When** I run `yak push vesselstudio-1.1.0-rh8_0-win.yak`, **Then** the new version is published to the package server (note: cannot overwrite existing versions)

4. **Given** a new version is published, **When** users open Rhino with the plugin installed, **Then** they see an update notification in the Package Manager indicating a new version is available

5. **Given** a user sees an update notification, **When** they click "Update" in the Package Manager, **Then** the new version downloads, the old version is uninstalled, the new version is installed, and Rhino may prompt for a restart

---

### User Story 4 - Automated Build Integration (Priority: P3)

As a plugin maintainer, I want package creation integrated into the build process so I can generate `.yak` packages automatically with each release build without manual terminal commands.

**Why this priority**: This is a quality-of-life improvement that reduces human error and streamlines the release process. It's P3 because manual package creation works fine, but automation improves reliability and efficiency for frequent releases.

**Independent Test**: Can be tested by running the build script with a new `--CreatePackage` parameter. Success is verified when a `.yak` file appears in the output directory without running separate yak commands.

**Acceptance Scenarios**:

1. **Given** the build script has a new `-CreatePackage` parameter, **When** I run `.\build.ps1 -Configuration Release -CreatePackage`, **Then** the build completes and a `.yak` package file is created in the `dist/` folder

2. **Given** the build creates a package, **When** I inspect the package creation logs, **Then** I see output showing yak spec generation, yak build execution, and the package filename

3. **Given** I want to build and test locally, **When** I run `.\build.ps1` without the `-CreatePackage` flag, **Then** the build completes normally without creating a package (opt-in behavior)

4. **Given** I want to build and publish in one step, **When** I add a `-PushPackage` flag (future enhancement), **Then** the script builds, creates package, and optionally pushes it after confirmation

---

### Edge Cases

- **What happens when manifest.yml is missing or invalid?** The `yak build` command should fail with a clear error message indicating which fields are missing or incorrectly formatted. The build script should detect this failure and stop execution.

- **What happens when the version number in manifest.yml doesn't match AssemblyInfo.cs?** The yak tool will use the manifest.yml version, potentially creating confusion. We should add validation to ensure version consistency across all files before building the package.

- **What happens when pushing a version that already exists?** The yak server will reject the push with an error "package version already exists". The maintainer must increment the version number and rebuild. Cannot overwrite or delete published versions.

- **What happens when a user has both manual install and package manager install?** Rhino may load the plugin twice causing conflicts. The package should include installation notes warning against mixed installation methods and recommending uninstallation of manual installs before using package manager.

- **What happens when Newtonsoft.Json.dll version conflicts with other plugins?** The package should include Newtonsoft.Json.dll (13.0.3) in the distribution. Rhino's assembly loading will use the first loaded version. We should document this potential conflict in README.

- **What happens when distributing to both Rhino 7 and Rhino 8 users?** We need to create two separate .yak packages with different distribution tags (rh7_0 and rh8_0) if we want to support both versions. Currently the plugin targets Rhino 8 (RhinoCommon 8.1.23325.13001), so we should start with rh8_0-win only and add rh7_0 if there's demand.

- **What happens when users have the plugin installed and we yank a version?** The `yak yank` command unlists a version from search results but doesn't uninstall it from users who already have it. Yanking is for preventing new installations, not removing existing ones.

- **What happens when building on Mac vs Windows?** The current plugin uses .NET Framework 4.8 and System.Windows.Forms (Windows-only). Mac builds are not currently supported. The manifest should specify platform: win to prevent installation attempts on Mac.

- **What happens when the OAuth token expires during push?** The yak tool will prompt for re-authentication. The token is valid for approximately 30 days. Maintainers should be prepared to run `yak login` periodically.

- **What happens if the icon.png file is missing during build?** The yak build will succeed but the package will have no icon in the Package Manager UI. We should include validation to ensure icon.png exists and is the correct size before building.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Build process MUST generate a valid `manifest.yml` file containing name (vesselstudio), version (from AssemblyInfo.cs), authors (Creata Collective Limited), description, URL (vesselstudio.io), icon reference (icon.png), and keywords

- **FR-002**: Build process MUST create a distribution folder containing VesselStudioSimplePlugin.rhp, Newtonsoft.Json.dll (13.0.3), icon.png (32x32 or 48x48), and optional README/LICENSE files

- **FR-003**: Package MUST use correct distribution tag `rh8_0-win` indicating Rhino 8 compatibility and Windows-only platform

- **FR-004**: Package filename MUST follow format `vesselstudio-{version}-rh8_0-win.yak` (e.g., vesselstudio-1.0.0-rh8_0-win.yak)

- **FR-005**: Maintainer MUST be able to authenticate with `yak login` using Rhino Accounts and receive an OAuth token stored in `%APPDATA%\McNeel\yak.yml`

- **FR-006**: Maintainer MUST be able to push packages to test server (https://test.yak.rhino3d.com) for validation before production release

- **FR-007**: Maintainer MUST be able to push packages to production server (https://yak.rhino3d.com) for public distribution

- **FR-008**: System MUST prevent overwriting existing package versions - each push requires a new version number

- **FR-009**: Package MUST include plugin GUID as a keyword in manifest for package restore functionality (GUID: A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D from AssemblyInfo.cs)

- **FR-010**: Build script SHOULD validate version number consistency across AssemblyInfo.cs, .csproj, and manifest.yml before building package

- **FR-011**: Package MUST exclude RhinoCommon.dll, Rhino.UI.dll, Eto.dll as these are provided by Rhino (already handled by post-build cleanup)

- **FR-012**: Package MUST include icon.png (extracted from Resources/ or created separately) for display in Package Manager UI

- **FR-013**: Documentation MUST include step-by-step instructions for maintainers to build, test, and publish packages

- **FR-014**: Documentation MUST include installation instructions for end users discovering the plugin in Package Manager

- **FR-015**: System MUST support yanking (unlisting) specific versions using `yak yank` command if a version needs to be removed from search results

### Key Entities

- **Yak Package (.yak file)**: A compressed archive containing the plugin binary (.rhp), dependencies (Newtonsoft.Json.dll), manifest.yml metadata, icon, and optional documentation. Follows naming convention `{name}-{version}-{distribution_tag}.yak`

- **Manifest (manifest.yml)**: YAML file containing package metadata including name, version, authors, description, URL, keywords, icon reference, and Rhino version compatibility. Generated by `yak spec` and customized manually.

- **Distribution Tag**: A version identifier indicating Rhino compatibility and platform (e.g., rh8_0-win means Rhino 8.0+ on Windows). Format: `rh{major}_{minor}-{platform}` where platform is win, mac, or any.

- **Package Server**: McNeel's hosted service for distributing Rhino packages. Two environments: test server (https://test.yak.rhino3d.com, wiped nightly) and production server (https://yak.rhino3d.com, permanent).

- **OAuth Token**: Authentication credential stored in `%APPDATA%\McNeel\yak.yml` (Windows) or `~/.mcneel/yak.yml` (Mac), valid for approximately 30 days, obtained via `yak login`.

- **Distribution Folder (dist/)**: Directory containing all files to be packaged: .rhp, dependencies, manifest.yml, icon.png, README, LICENSE. Used as input for `yak build`.

- **Plugin GUID**: Unique identifier (A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D) embedded in manifest keywords for package restore functionality and plugin identification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Maintainer can generate a valid .yak package file from the Release build output in under 5 minutes of manual work

- **SC-002**: Package successfully uploads to test server with `yak push` command and appears in search results within 30 seconds

- **SC-003**: Package successfully uploads to production server and becomes discoverable to all Rhino 8 users within 1 hour (allowing for server sync/indexing)

- **SC-004**: End users can discover the plugin by searching for "Vessel Studio", "vessel", or "yacht" in Rhino's Package Manager GUI

- **SC-005**: End users can install the plugin from Package Manager and successfully run `VesselCapture` command without manual file downloads or installation instructions

- **SC-006**: Plugin updates (new versions) are detected by Rhino's Package Manager and users see update notifications when available

- **SC-007**: Maintainer can publish a new version (increment, rebuild, repackage, push) in under 10 minutes of manual work

- **SC-008**: 90% of new users install via Package Manager rather than manual .rhp file download within 3 months of publication

- **SC-009**: Zero installation support tickets related to "where do I put the .rhp file" after package manager publication

- **SC-010**: Build script validation catches version number mismatches before package creation, preventing invalid packages from being built

## Assumptions

- **A-001**: Plugin is already built and working in Release configuration with all files in `VesselStudioSimplePlugin/bin/Release/net48/`

- **A-002**: Maintainer has Rhino 8 installed with the Yak CLI tool available at `C:\Program Files\Rhino 8\System\Yak.exe`

- **A-003**: Maintainer has a valid Rhino Account for authentication (free to create at rhino3d.com)

- **A-004**: Plugin targets Rhino 8 only initially (rh8_0 distribution tag) - Rhino 7 support can be added later if needed

- **A-005**: Plugin is Windows-only due to .NET Framework 4.8 and System.Windows.Forms dependencies - Mac support would require complete UI rewrite

- **A-006**: Newtonsoft.Json.dll (13.0.3) should be included in package distribution as it's not provided by Rhino

- **A-007**: Icon for package manager can be created from existing Resources/icon_32.png or icon_48.png

- **A-008**: README.md and LICENSE files are optional for initial publication but recommended for future versions

- **A-009**: Package name "vesselstudio" is available on yak.rhino3d.com (should verify during initial push)

- **A-010**: Internet connection is available during build/push process for authentication and package upload

## Dependencies

- **D-001**: Yak CLI tool (included with Rhino 8 installation)

- **D-002**: PowerShell 5.1+ for build script enhancements

- **D-003**: Rhino Accounts system for authentication

- **D-004**: McNeel package server infrastructure (test.yak.rhino3d.com and yak.rhino3d.com)

- **D-005**: Existing build.ps1 script as foundation for package creation integration

## Out of Scope

- **OS-001**: Mac version of the plugin (requires complete rewrite to .NET 6+ and Eto.Forms)

- **OS-002**: Rhino 7 support (requires separate testing and distribution tag creation)

- **OS-003**: Automatic update checking within the plugin itself (Rhino's Package Manager handles this)

- **OS-004**: Telemetry or analytics on package downloads/installations (McNeel may provide this in their dashboard)

- **OS-005**: Private package distribution (all packages on yak.rhino3d.com are public)

- **OS-006**: Beta/preview channel distribution (would require separate package naming convention)

- **OS-007**: Automated CI/CD pipeline for package publishing (this is manual initial setup; automation can come later)

## Related Documentation

- [Creating a Rhino Plugin Package](https://developer.rhino3d.com/guides/yak/creating-a-rhino-plugin-package/)
- [Pushing a Package to the Server](https://developer.rhino3d.com/guides/yak/pushing-a-package-to-the-server/)
- [The Package Manifest](https://developer.rhino3d.com/guides/yak/the-package-manifest/)
- [Yak CLI Reference](https://developer.rhino3d.com/guides/yak/yak-cli-reference/)
