# Tasks: Rhino Package Manager Distribution

**Input**: Design documents from `/specs/002-rhino-package-manager/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: No test tasks included (not requested in specification)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

This feature uses existing project structure with new dist/ staging folder:
- `VesselStudioSimplePlugin/` - Existing plugin source (no changes)
- `dist/` - Package staging folder (new)
- `build.ps1` - Build script (modifications in P3/US4)
- `docs/guides/` - Documentation (new guide)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare distribution folder structure and verify prerequisites

- [X] T001 Create dist/ folder in repository root with .gitignore entry
- [X] T002 Verify Rhino 8 installation and Yak CLI exists at C:\Program Files\Rhino 8\System\Yak.exe
- [X] T003 [P] Verify plugin builds successfully in Release mode (VesselStudioSimplePlugin/bin/Release/net48/)
- [X] T004 [P] Verify icon files exist in VesselStudioSimplePlugin/Resources/ (icon_24.png, icon_32.png, icon_48.png)

**Checkpoint**: Prerequisites verified - ready for package creation

---

## Phase 2: User Story 1 - Initial Package Publication (Priority: P1) 🎯 MVP PART 1

**Goal**: Publish vesselstudio-1.0.0-rh8_0-win.yak to production yak.rhino3d.com and make it discoverable to all Rhino users

**Independent Test**: Run `yak search vesselstudio` and verify package appears with correct metadata. Install in fresh Rhino instance and run `VesselCapture` command successfully.

### File Preparation

- [X] T005 [P] [US1] Copy VesselStudioSimplePlugin.rhp from bin/Release/net48/ to dist/
- [X] T006 [P] [US1] Copy Newtonsoft.Json.dll from bin/Release/net48/ to dist/
- [X] T007 [P] [US1] Copy Resources/icon_48.png to dist/icon.png
- [X] T008 [US1] Verify no RhinoCommon.dll, Rhino.UI.dll, or Eto.dll in dist/ (should be excluded)

### Manifest Creation

- [X] T009 [US1] Run `yak spec` in dist/ to generate initial manifest.yml
- [X] T010 [US1] Edit manifest.yml name to "vesselstudio" in dist/manifest.yml
- [X] T011 [US1] Edit manifest.yml version to "1.0.0" (match AssemblyInfo.cs) in dist/manifest.yml
- [X] T012 [US1] Edit manifest.yml authors to ["Creata Collective Limited"] in dist/manifest.yml
- [X] T013 [US1] Edit manifest.yml description with marketing copy in dist/manifest.yml
- [X] T014 [US1] Edit manifest.yml url to "https://vesselstudio.io" in dist/manifest.yml
- [X] T015 [US1] Edit manifest.yml icon to "icon.png" in dist/manifest.yml
- [X] T016 [US1] Edit manifest.yml keywords to include ["vessel", "studio", "yacht", "capture", "viewport", "guid:A1B2C3D4-E5F6-7A8B-9C0D-1E2F3A4B5C6D"] in dist/manifest.yml

### Package Building

- [X] T017 [US1] Run `yak build` in dist/ to create vesselstudio-1.0.0-rh8_0-win.yak (WORKAROUND: Manual ZIP creation)
- [X] T018 [US1] Verify .yak file created with correct filename format (name-version-distribution_tag.yak)
- [X] T019 [US1] Inspect .yak contents to verify all required files present (rhp, dll, manifest, icon)

### Authentication

- [X] T020 [US1] Run `yak login` and complete OAuth flow via browser
- [X] T021 [US1] Verify OAuth token saved to %APPDATA%\McNeel\yak.yml

### Test Server Validation

- [X] T022 [US1] Push package to test server: `yak push vesselstudio-1.0.0-rh8_0-win.yak --source https://test.yak.rhino3d.com`
- [X] T023 [US1] Verify successful push (no errors in command output)
- [X] T024 [US1] Search test server: `yak search --source https://test.yak.rhino3d.com --all vesselstudio`
- [X] T025 [US1] Verify package appears in search results with correct name and version
- [X] T026 [US1] Verify manifest metadata correct in search results (description, authors, keywords)

### Production Publishing

- [X] T027 [US1] Push package to production: `yak push vesselstudio-1.0.0-rh8_0-win.yak`
- [X] T028 [US1] Verify successful push to production (no errors in command output)
- [X] T029 [US1] Search production server: `yak search vesselstudio`
- [X] T030 [US1] Verify package appears in production search results

**Checkpoint**: Package published to yak.rhino3d.com - ready for user installation testing (User Story 2)

---

## Phase 3: User Story 2 - User Installation from Package Manager (Priority: P1) 🎯 MVP PART 2

**Goal**: Validate that end users can discover and install the plugin from Rhino's Package Manager GUI without manual file downloads

**Independent Test**: Open fresh Rhino 8 instance, search "Vessel Studio" in Package Manager, install plugin, verify `VesselCapture` command works.

**Prerequisites**: User Story 1 complete (package must be published to production)

### GUI Discovery Testing

- [ ] T031 [US2] Open Rhino 8 on fresh/test machine
- [ ] T032 [US2] Open Package Manager via Tools > Options > Package Manager
- [ ] T033 [US2] Search for "Vessel Studio" in Package Manager GUI
- [ ] T034 [US2] Verify vesselstudio package appears in results with icon
- [ ] T035 [US2] Verify package metadata displays correctly (description, version, authors)
- [ ] T036 [P] [US2] Search for "vessel" and verify package appears
- [ ] T037 [P] [US2] Search for "yacht" and verify package appears

### Installation Testing

- [ ] T038 [US2] Click "Install" button for vesselstudio package
- [ ] T039 [US2] Confirm installation when prompted
- [ ] T040 [US2] Verify installation success message appears
- [ ] T041 [US2] Verify no error messages during installation

### Functionality Verification

- [ ] T042 [US2] Type `VesselCapture` in Rhino command line
- [ ] T043 [US2] Verify command recognized and capture dialog opens
- [ ] T044 [US2] Test capture workflow to confirm plugin fully functional
- [ ] T045 [US2] Run `VesselStudioShowToolbar` command
- [ ] T046 [US2] Verify Vessel Studio toolbar panel appears and functions

### Installation Documentation

- [ ] T047 [P] [US2] Create user installation guide in docs/guides/USER_INSTALLATION.md
- [ ] T048 [P] [US2] Add screenshots of Package Manager search and install process to docs/guides/
- [ ] T049 [US2] Update main README.md with Package Manager installation instructions

**Checkpoint**: User installation workflow validated - MVP complete (P1 stories done)

---

## Phase 4: User Story 3 - Publishing Plugin Updates (Priority: P2)

**Goal**: Establish repeatable process for publishing plugin updates so existing users receive update notifications

**Independent Test**: Increment version to 1.0.1, rebuild, repackage, push, verify users see update notification in Rhino Package Manager.

**Prerequisites**: User Story 1 complete (initial package published)

### Version Update Process

- [ ] T050 [US3] Document version increment strategy in docs/guides/PACKAGE_MANAGER_GUIDE.md (MAJOR.MINOR.PATCH rules)
- [ ] T051 [US3] Update version in VesselStudioSimplePlugin/Properties/AssemblyInfo.cs (1.0.0.0 → 1.0.1.0)
- [ ] T052 [US3] Update version in VesselStudioSimplePlugin/VesselStudioSimplePlugin.csproj (1.0.0 → 1.0.1)
- [ ] T053 [US3] Rebuild plugin in Release configuration
- [ ] T054 [US3] Verify new .rhp generated with updated version

### Package Update Creation

- [ ] T055 [US3] Clear dist/ folder of previous version files
- [ ] T056 [P] [US3] Copy updated VesselStudioSimplePlugin.rhp to dist/
- [ ] T057 [P] [US3] Copy Newtonsoft.Json.dll to dist/ (same version)
- [ ] T058 [P] [US3] Copy icon.png to dist/ (no changes needed)
- [ ] T059 [US3] Update dist/manifest.yml version field to "1.0.1"
- [ ] T060 [US3] Run `yak build` in dist/ to create vesselstudio-1.0.1-rh8_0-win.yak
- [ ] T061 [US3] Verify new .yak file created with updated version in filename

### Update Publishing

- [ ] T062 [US3] Push updated package to test server first: `yak push vesselstudio-1.0.1-rh8_0-win.yak --source https://test.yak.rhino3d.com`
- [ ] T063 [US3] Verify test server push successful
- [ ] T064 [US3] Push updated package to production: `yak push vesselstudio-1.0.1-rh8_0-win.yak`
- [ ] T065 [US3] Verify production push successful
- [ ] T066 [US3] Verify cannot overwrite existing version (test immutability by attempting to push 1.0.0 again)

### Update Notification Testing

- [ ] T067 [US3] Open Rhino 8 with vesselstudio 1.0.0 installed
- [ ] T068 [US3] Open Package Manager
- [ ] T069 [US3] Verify update notification badge/indicator appears
- [ ] T070 [US3] Verify "Update" button appears for vesselstudio package
- [ ] T071 [US3] Click "Update" button
- [ ] T072 [US3] Verify update downloads and installs successfully
- [ ] T073 [US3] Restart Rhino if prompted
- [ ] T074 [US3] Verify plugin now shows version 1.0.1
- [ ] T075 [US3] Test plugin functionality to confirm update successful

### Update Documentation

- [ ] T076 [P] [US3] Document update publishing process in docs/guides/PACKAGE_MANAGER_GUIDE.md
- [ ] T077 [P] [US3] Add version consistency checklist to PACKAGE_MANAGER_GUIDE.md (AssemblyInfo, csproj, manifest must match)
- [ ] T078 [US3] Document rollback strategy (yank + new version) in PACKAGE_MANAGER_GUIDE.md

**Checkpoint**: Update publishing workflow established and validated

---

## Phase 5: User Story 4 - Automated Build Integration (Priority: P3)

**Goal**: Integrate package creation into build.ps1 script to generate .yak packages automatically with each release build

**Independent Test**: Run `.\build.ps1 -Configuration Release -CreatePackage` and verify vesselstudio-1.0.0-rh8_0-win.yak appears in dist/ without manual yak commands.

**Prerequisites**: User Story 1 complete (manual process proven) and User Story 3 complete (update workflow established)

### Build Script Analysis

- [ ] T079 [US4] Review current build.ps1 structure (275 lines) to identify integration points
- [ ] T080 [US4] Identify post-build hook location after .rhp creation in build.ps1

### Parameter Addition

- [ ] T081 [P] [US4] Add `-CreatePackage` switch parameter to build.ps1
- [ ] T082 [P] [US4] Add `-PushToTest` switch parameter to build.ps1 (optional push to test server)
- [ ] T083 [P] [US4] Add `-PushToProduction` switch parameter to build.ps1 (optional push to production, requires confirmation)

### Version Validation Function

- [ ] T084 [US4] Create `Test-VersionConsistency` function in build.ps1
- [ ] T085 [US4] Read version from AssemblyInfo.cs in Test-VersionConsistency
- [ ] T086 [US4] Read version from .csproj in Test-VersionConsistency
- [ ] T087 [US4] Read version from dist/manifest.yml (if exists) in Test-VersionConsistency
- [ ] T088 [US4] Compare versions and return validation result in Test-VersionConsistency
- [ ] T089 [US4] Call Test-VersionConsistency before package creation if -CreatePackage specified

### Distribution Folder Population

- [ ] T090 [US4] Create `New-DistributionFolder` function in build.ps1
- [ ] T091 [US4] Create/clear dist/ folder in New-DistributionFolder
- [ ] T092 [US4] Copy .rhp from bin/Release/net48/ to dist/ in New-DistributionFolder
- [ ] T093 [US4] Copy Newtonsoft.Json.dll from bin/Release/net48/ to dist/ in New-DistributionFolder
- [ ] T094 [US4] Copy Resources/icon_48.png to dist/icon.png in New-DistributionFolder
- [ ] T095 [US4] Verify no excluded DLLs (RhinoCommon, Eto, Rhino.UI) in New-DistributionFolder

### Manifest Generation Integration

- [ ] T096 [US4] Create `New-PackageManifest` function in build.ps1
- [ ] T097 [US4] Check if dist/manifest.yml exists in New-PackageManifest
- [ ] T098 [US4] If manifest missing: Run `yak spec` in dist/ in New-PackageManifest
- [ ] T099 [US4] If manifest missing: Auto-populate fields (name, version, authors, description, url, icon, keywords) in New-PackageManifest
- [ ] T100 [US4] If manifest exists: Update only version field to match AssemblyInfo.cs in New-PackageManifest
- [ ] T101 [US4] Validate manifest.yml is valid YAML in New-PackageManifest

### Package Building Integration

- [ ] T102 [US4] Create `Invoke-YakBuild` function in build.ps1
- [ ] T103 [US4] Verify Yak CLI exists at C:\Program Files\Rhino 8\System\Yak.exe in Invoke-YakBuild
- [ ] T104 [US4] Run `yak build` in dist/ folder in Invoke-YakBuild
- [ ] T105 [US4] Capture yak build output and parse for .yak filename in Invoke-YakBuild
- [ ] T106 [US4] Verify .yak file created successfully in Invoke-YakBuild
- [ ] T107 [US4] Return .yak file path from Invoke-YakBuild

### Optional Push Integration

- [ ] T108 [P] [US4] Create `Invoke-YakPush` function in build.ps1
- [ ] T109 [P] [US4] If -PushToTest: Push .yak to test.yak.rhino3d.com in Invoke-YakPush
- [ ] T110 [P] [US4] If -PushToProduction: Prompt for confirmation then push to yak.rhino3d.com in Invoke-YakPush
- [ ] T111 [P] [US4] Capture push output and verify success in Invoke-YakPush

### Main Workflow Integration

- [ ] T112 [US4] Add package creation workflow to build.ps1 main execution after successful plugin build
- [ ] T113 [US4] If -CreatePackage: Call Test-VersionConsistency and abort on mismatch in build.ps1
- [ ] T114 [US4] If -CreatePackage: Call New-DistributionFolder in build.ps1
- [ ] T115 [US4] If -CreatePackage: Call New-PackageManifest in build.ps1
- [ ] T116 [US4] If -CreatePackage: Call Invoke-YakBuild in build.ps1
- [ ] T117 [US4] If -CreatePackage: Log package creation summary (filename, size, location) in build.ps1
- [ ] T118 [US4] If -PushToTest or -PushToProduction: Call Invoke-YakPush in build.ps1

### Testing and Validation

- [ ] T119 [US4] Test build.ps1 without -CreatePackage (ensure no changes to default behavior)
- [ ] T120 [US4] Test build.ps1 -CreatePackage (verify .yak created in dist/)
- [ ] T121 [US4] Test build.ps1 -CreatePackage -PushToTest (verify push to test server)
- [ ] T122 [US4] Test version mismatch detection (intentionally mismatch versions and verify abort)
- [ ] T123 [US4] Test with missing Yak CLI (verify graceful error message)
- [ ] T124 [US4] Test with missing icon (verify validation catches it)

### Documentation Updates

- [ ] T125 [P] [US4] Update BUILD_GUIDE.md with -CreatePackage parameter documentation
- [ ] T126 [P] [US4] Add package creation examples to BUILD_GUIDE.md
- [ ] T127 [P] [US4] Document version consistency validation in BUILD_GUIDE.md
- [ ] T128 [US4] Update PACKAGE_MANAGER_GUIDE.md with automated workflow section

**Checkpoint**: Build automation complete - package creation now streamlined

---

## Phase 6: Polish & Documentation

**Purpose**: Complete comprehensive documentation and final improvements

### Maintainer Documentation

- [ ] T129 [P] Create docs/guides/PACKAGE_MANAGER_GUIDE.md with complete maintainer workflow
- [ ] T130 [P] Add "Manual Package Creation" section to PACKAGE_MANAGER_GUIDE.md (US1 process)
- [ ] T131 [P] Add "Publishing Updates" section to PACKAGE_MANAGER_GUIDE.md (US3 process)
- [ ] T132 [P] Add "Automated Package Creation" section to PACKAGE_MANAGER_GUIDE.md (US4 process)
- [ ] T133 [P] Add "Troubleshooting" section to PACKAGE_MANAGER_GUIDE.md (common errors and solutions)
- [ ] T134 [P] Add "Yak CLI Reference" section to PACKAGE_MANAGER_GUIDE.md (all commands used)

### User-Facing Documentation

- [ ] T135 [P] Update main README.md with "Installation via Package Manager" section
- [ ] T136 [P] Add Package Manager installation as primary method in README.md
- [ ] T137 [P] Add manual .rhp installation as alternative method in README.md
- [ ] T138 [P] Create README.md for dist/ folder explaining purpose and contents

### Optional Enhancements

- [ ] T139 [P] Create LICENSE.txt file for package inclusion (coordinate with legal team)
- [ ] T140 [P] Create README.md for package inclusion (user-facing quick start)
- [ ] T141 [P] Add screenshots to docs/guides/ showing Package Manager workflow
- [ ] T142 [P] Update CHANGELOG.md with package manager distribution feature

### Validation

- [ ] T143 Run quickstart.md validation scenarios from specs/002-rhino-package-manager/quickstart.md
- [ ] T144 Verify all documentation links work and paths are correct
- [ ] T145 Verify all success criteria from spec.md are met (SC-001 through SC-010)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **User Story 1 (Phase 2)**: Depends on Setup completion
- **User Story 2 (Phase 3)**: Depends on User Story 1 completion (package must be published)
- **User Story 3 (Phase 4)**: Depends on User Story 1 completion (can proceed independently of US2)
- **User Story 4 (Phase 5)**: Depends on User Story 1 and 3 completion (manual process must be proven)
- **Polish (Phase 6)**: Can proceed once MVP (US1+US2) is complete

### User Story Dependencies

```
Setup (Phase 1)
     ↓
User Story 1 (Phase 2) - Initial Package Publication ← MVP BLOCKER
     ↓
     ├─→ User Story 2 (Phase 3) - User Installation Testing ← MVP COMPLETION
     └─→ User Story 3 (Phase 4) - Update Publishing (independent of US2)
              ↓
         User Story 4 (Phase 5) - Build Automation
```

### Critical Path for MVP

1. Phase 1: Setup (T001-T004) - 30 minutes
2. Phase 2: User Story 1 (T005-T030) - 2-3 hours
3. Phase 3: User Story 2 (T031-T049) - 1-2 hours
4. **MVP COMPLETE** - Package published and user installation validated

### Parallel Opportunities

#### Within Setup (Phase 1)
- T003 (verify plugin build) parallel with T004 (verify icons)

#### Within User Story 1 (Phase 2)
- T005, T006, T007 (file copying) all parallel
- T036, T037 (keyword searches) parallel with T035

#### Within User Story 2 (Phase 3)
- T036, T037 (search variations) parallel
- T047, T048 (documentation) parallel

#### Within User Story 3 (Phase 4)
- T056, T057, T058 (file copying) all parallel
- T076, T077 (documentation) parallel

#### Within User Story 4 (Phase 5)
- T081, T082, T083 (parameter additions) all parallel
- T108, T109, T110, T111 (push function) parallel with T102-T107 (build function)
- T125, T126, T127 (documentation) parallel

#### Within Polish (Phase 6)
- All documentation tasks (T129-T142) can run in parallel

### Incremental Delivery Strategy

**Sprint 1: MVP (Weeks 1-2)**
- Complete Phase 1 + Phase 2 (User Story 1)
- Complete Phase 3 (User Story 2)
- **Deliverable**: Package published and installable by users

**Sprint 2: Updates (Week 3)**
- Complete Phase 4 (User Story 3)
- **Deliverable**: Update publishing process established

**Sprint 3: Automation (Week 4)**
- Complete Phase 5 (User Story 4)
- **Deliverable**: Build script with automated package creation

**Sprint 4: Polish (Week 5)**
- Complete Phase 6 (Documentation and polish)
- **Deliverable**: Production-ready with comprehensive documentation

---

## Parallel Example: User Story 1 File Preparation

```powershell
# Launch all file preparation tasks together (T005-T007):
# Terminal 1:
Copy-Item "VesselStudioSimplePlugin/bin/Release/net48/VesselStudioSimplePlugin.rhp" "dist/"

# Terminal 2 (parallel):
Copy-Item "VesselStudioSimplePlugin/bin/Release/net48/Newtonsoft.Json.dll" "dist/"

# Terminal 3 (parallel):
Copy-Item "VesselStudioSimplePlugin/Resources/icon_48.png" "dist/icon.png"
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2 Only)

**Timeline**: 2-3 days

1. Complete Phase 1: Setup → Foundation ready
2. Complete Phase 2: User Story 1 → Package published ✅
3. Complete Phase 3: User Story 2 → User installation validated ✅
4. **STOP and VALIDATE**: Test complete workflow end-to-end
5. Deploy to production (already done in US1)
6. Monitor user adoption and support tickets

**Success Metrics**:
- ✅ Package searchable on yak.rhino3d.com
- ✅ Users can install via Package Manager GUI
- ✅ Zero "where do I install?" support tickets

### Incremental Delivery

**Week 1**: MVP (US1 + US2)
- Manual package creation workflow
- Published to production
- Users can install via Package Manager

**Week 2**: Updates (US3)
- Establish version update process
- Test update notifications
- Document update workflow

**Week 3**: Automation (US4)
- Integrate into build.ps1
- Reduce manual steps
- Version validation

**Week 4**: Polish
- Complete documentation
- Add optional README/LICENSE
- Final validation

### Parallel Team Strategy

With multiple developers:

**Developer A (Primary)**:
1. Complete Setup
2. Complete User Story 1 (blocking)
3. Complete User Story 2 (validates US1)

**Developer B (Documentation)**:
1. Start Phase 6 tasks after US1 completes (T129-T138)
2. User-facing documentation (parallel with US2 testing)

**Developer C (Future Work)**:
1. Begin User Story 3 after US1 completes (can proceed in parallel with US2)
2. User Story 4 after US3 completes

---

## Success Criteria Validation

All success criteria from spec.md mapped to tasks:

- **SC-001**: Package creation < 5 minutes → T005-T030 (streamlined in US4)
- **SC-002**: Searchable within 30 seconds → T024, T029, T030 (validated)
- **SC-003**: Discoverable within 1 hour → T030-T035 (validated)
- **SC-004**: Search by keywords → T033-T037 (validated)
- **SC-005**: Installation success → T038-T046 (validated)
- **SC-006**: Update notifications → T067-T075 (validated)
- **SC-007**: Update time < 10 minutes → T050-T066 (streamlined in US4)
- **SC-008**: 90% adoption within 3 months → Monitor post-launch
- **SC-009**: Zero installation support tickets → Monitor post-launch
- **SC-010**: Version validation catches mismatches → T084-T089, T122 (automated in US4)

---

## Notes

- [P] tasks = different files/operations, no dependencies, can run in parallel
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- User Story 1 is MVP blocker - must complete before US2
- User Story 2 validates US1 - should complete before moving to US3/US4
- User Story 3 and 4 can proceed in parallel after US1 complete
- Commit after each logical group of tasks
- Stop at any checkpoint to validate story independently
- Version immutability means we cannot overwrite published packages - always increment version
- OAuth token expires every ~30 days - be prepared to re-authenticate
- Test server (test.yak.rhino3d.com) is wiped nightly - use for validation only
- Production server (yak.rhino3d.com) is permanent - triple-check before pushing

---

**Total Tasks**: 145  
**MVP Tasks**: 49 (Phase 1-3: T001-T049)  
**Parallel Opportunities**: ~40 tasks marked [P]  
**Independent Stories**: 4 user stories, 3 can proceed in parallel after US1  
**Suggested MVP Scope**: User Story 1 + 2 (Initial publication + user installation validation)
