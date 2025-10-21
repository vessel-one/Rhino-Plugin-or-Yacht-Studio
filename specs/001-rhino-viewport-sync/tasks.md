# Tasks: Rhino Viewport Sync Plugin

**Input**: Design documents from `/specs/001-rhino-viewport-sync/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests are NOT requested in the feature specification - focusing on implementation tasks only.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions
- **Rhino Plugin**: `VesselStudioPlugin/` at repository root
- Tests in `VesselStudioPlugin.Tests/`

## Phase 1: Setup (Shared Infrastructure) ✅ COMPLETED

**Purpose**: Project initialization and basic Rhino plugin structure

- [x] T001 Create Visual Studio solution structure per implementation plan in VesselStudioPlugin/VesselStudioPlugin.sln
- [x] T002 Initialize C# class library project with RhinoCommon dependencies in VesselStudioPlugin/VesselStudioPlugin.csproj
- [x] T003 [P] Create folder structure for Commands, Services, UI, Models, Utils in VesselStudioPlugin/
- [x] T004 [P] Add NuGet packages: RhinoCommon, Eto.Forms, System.Net.Http, System.Text.Json to VesselStudioPlugin.csproj
- [x] T005 [P] Create test project structure in VesselStudioPlugin.Tests/ with NUnit references
- [x] T006 [P] Configure assembly info and plugin manifest in VesselStudioPlugin/Properties/AssemblyInfo.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core plugin infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T007 Create main plugin entry point in VesselStudioPlugin/VesselStudioPlugin.cs with service initialization
- [ ] T008 [P] Define core service interfaces in VesselStudioPlugin/Services/IApiClient.cs
- [ ] T009 [P] Define authentication service interface in VesselStudioPlugin/Services/IAuthService.cs
- [ ] T010 [P] Define screenshot service interface in VesselStudioPlugin/Services/IScreenshotService.cs
- [ ] T011 [P] Create base model classes and enumerations in VesselStudioPlugin/Models/
- [ ] T012 Create ViewportMetadata model in VesselStudioPlugin/Models/ViewportMetadata.cs
- [ ] T013 Create ProjectInfo model in VesselStudioPlugin/Models/ProjectInfo.cs
- [ ] T014 Create UploadTransaction model in VesselStudioPlugin/Models/UploadTransaction.cs
- [ ] T015 Create ViewportScreenshot model in VesselStudioPlugin/Models/ViewportScreenshot.cs
- [ ] T016 [P] Create UploadStatus and AuthenticationState enums in VesselStudioPlugin/Models/Enums.cs
- [ ] T017 Implement secure credential storage utility in VesselStudioPlugin/Utils/SecureStorage.cs
- [ ] T018 Implement image processing utility in VesselStudioPlugin/Utils/ImageProcessor.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 2 - Secure Plugin Authentication (Priority: P1) 🎯 MVP Foundation

**Goal**: Enable users to securely connect their Rhino plugin to their Vessel Studio account

**Independent Test**: Run VesselStudioLogin command, complete web authentication, verify plugin recognizes authenticated user

### Implementation for User Story 2

- [ ] T019 [P] [US2] Implement ApiClient base HTTP service in VesselStudioPlugin/Services/ApiClient.cs
- [ ] T020 [US2] Implement AuthService with device flow authentication in VesselStudioPlugin/Services/AuthService.cs
- [ ] T021 [US2] Create VesselStudioLoginCommand in VesselStudioPlugin/Commands/VesselStudioLoginCommand.cs
- [ ] T022 [US2] Create VesselStudioLogoutCommand in VesselStudioPlugin/Commands/VesselStudioLogoutCommand.cs
- [ ] T023 [US2] Add authentication state persistence and token refresh logic to AuthService
- [ ] T024 [US2] Add browser launch functionality for authentication flow
- [ ] T025 [US2] Add polling mechanism for authentication completion
- [ ] T026 [US2] Implement multiple instance support with independent sessions

**Checkpoint**: At this point, User Story 2 (Authentication) should be fully functional and testable independently

---

## Phase 4: User Story 3 - Project Selection Interface (Priority: P2)

**Goal**: Enable users to choose which Vessel Studio project receives their viewport screenshots

**Independent Test**: Login, run capture command, verify project selection dialog appears with user's projects

### Implementation for User Story 3

- [ ] T027 [P] [US3] Create project API integration in ApiClient for /plugin/projects endpoint
- [ ] T028 [US3] Implement ProjectSelectorDialog using Eto.Forms in VesselStudioPlugin/UI/ProjectSelectorDialog.cs
- [ ] T029 [US3] Add project caching and refresh functionality to AuthService
- [ ] T030 [US3] Implement project selection persistence for session convenience
- [ ] T031 [US3] Add project validation and deleted project detection
- [ ] T032 [US3] Create status indicator UI component in VesselStudioPlugin/UI/StatusIndicator.cs

**Checkpoint**: At this point, User Stories 2 AND 3 should both work independently

---

## Phase 5: User Story 1 - One-Click Viewport Capture (Priority: P1) 🎯 Core MVP

**Goal**: Enable yacht designers to share current viewport by uploading directly to Vessel Studio project

**Independent Test**: Open Rhino with 3D model, run capture command, verify screenshot appears in web project

### Implementation for User Story 1

- [ ] T033 [US1] Implement ScreenshotService with viewport capture in VesselStudioPlugin/Services/ScreenshotService.cs
- [ ] T034 [US1] Create VesselStudioCaptureCommand in VesselStudioPlugin/Commands/VesselStudioCaptureCommand.cs
- [ ] T035 [US1] Add viewport metadata extraction from Rhino APIs
- [ ] T036 [US1] Implement image compression logic for large files (>5MB threshold)
- [ ] T037 [US1] Add upload API integration for /plugin/project/{projectId}/screenshot endpoint
- [ ] T038 [US1] Implement local viewport image queue management in VesselStudioPlugin/Services/StorageService.cs
- [ ] T039 [US1] Add upload progress feedback and status reporting
- [ ] T040 [US1] Implement network connectivity detection and retry logic
- [ ] T041 [US1] Add upload transaction tracking and error handling
- [ ] T042 [US1] Integrate with project selection dialog from User Story 3

**Checkpoint**: At this point, Core MVP (User Stories 1, 2, 3) should be fully functional

---

## Phase 6: User Story 4 - Real-time Web Sync (Priority: P3)

**Goal**: Enable team members to see new Rhino viewport screenshots appear automatically in web browser

**Independent Test**: One user uploads from Rhino while another watches web project page, verify real-time updates

### Implementation for User Story 4

- [ ] T043 [P] [US4] Add WebSocket client support to ApiClient in VesselStudioPlugin/Services/ApiClient.cs
- [ ] T044 [US4] Implement real-time notification handling in ScreenshotService
- [ ] T045 [US4] Add upload completion callbacks for web sync triggers
- [ ] T046 [US4] Implement connection management for WebSocket client
- [ ] T047 [US4] Add real-time status updates to StatusIndicator UI

**Checkpoint**: All user stories should now be independently functional with real-time capabilities

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and overall plugin quality

- [ ] T048 [P] Add comprehensive error handling and user-friendly messages across all commands
- [ ] T049 [P] Implement plugin settings and configuration management
- [ ] T050 [P] Add logging and diagnostic information for troubleshooting
- [ ] T051 [P] Optimize plugin startup performance and resource usage
- [ ] T052 [P] Add plugin uninstall cleanup and credential removal
- [ ] T053 Windows compatibility verification and testing
- [ ] T054 Performance optimization for large image handling and upload
- [ ] T055 Security review and credential storage hardening
- [ ] T056 Create plugin installation package (.rhp file) and distribution setup
- [ ] T057 Run quickstart.md validation and update documentation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User Story 2 (Authentication) should complete first as it's needed by others
  - User Story 3 (Project Selection) can run parallel with US2 but needs US2 for testing
  - User Story 1 (Viewport Capture) needs both US2 and US3 for full functionality
  - User Story 4 (Real-time Sync) is independent enhancement
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 2 (Authentication)**: Can start after Foundational - No dependencies on other stories (MVP Foundation)
- **User Story 3 (Project Selection)**: Needs US2 for authentication context but can be developed in parallel
- **User Story 1 (Viewport Capture)**: Integrates with US2 (auth) and US3 (project selection) - Core MVP functionality
- **User Story 4 (Real-time Sync)**: Independent enhancement - can start after Foundational

### Within Each User Story

- Models and interfaces before service implementations
- Core services before command implementations
- Authentication before API integrations
- UI components after core functionality
- Integration tasks after individual components

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Within each user story, tasks marked [P] can run in parallel
- User Story 3 and User Story 4 can be developed in parallel after US2 is complete
- Different team members can work on different user stories simultaneously

---

## Parallel Example: User Story 1 Implementation

```bash
# Launch foundation models together:
Task: "Create ViewportMetadata model in VesselStudioPlugin/Models/ViewportMetadata.cs"
Task: "Create ViewportScreenshot model in VesselStudioPlugin/Models/ViewportScreenshot.cs"
Task: "Create UploadTransaction model in VesselStudioPlugin/Models/UploadTransaction.cs"

# Launch service implementations together:
Task: "Implement ScreenshotService with viewport capture"
Task: "Implement offline queue management in StorageService"
```

---

## Implementation Strategy

### MVP First (Authentication + Project Selection + Viewport Capture)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 2 (Authentication) - Foundation for all features
4. Complete Phase 4: User Story 3 (Project Selection) - Essential for user workflow
5. Complete Phase 5: User Story 1 (Viewport Capture) - Core value proposition
6. **STOP and VALIDATE**: Test complete MVP workflow end-to-end
7. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 2 (Authentication) → Test independently → Foundation ready for other features
3. Add User Story 3 (Project Selection) → Test independently → Project workflow complete
4. Add User Story 1 (Viewport Capture) → Test independently → Core MVP complete → Deploy/Demo
5. Add User Story 4 (Real-time Sync) → Test independently → Full feature set → Deploy/Demo
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers (after Foundational phase complete):

1. Developer A: User Story 2 (Authentication) - Must complete first
2. Once US2 is done:
   - Developer A: User Story 1 (Viewport Capture)
   - Developer B: User Story 3 (Project Selection)
   - Developer C: User Story 4 (Real-time Sync)
3. Stories integrate at the end for complete workflow

---

## Notes

- **[P] tasks**: Different files, no dependencies - can run in parallel
- **[Story] labels**: Map tasks to specific user stories for traceability
- **MVP Strategy**: US2 + US3 + US1 provides complete core workflow
- **Testing**: Each user story should be independently testable via Rhino commands
- **Integration**: User stories integrate naturally through shared services and models
- **Cross-platform**: Eto.Forms ensures Windows/Mac compatibility throughout
- **Security**: OS-level credential storage protects authentication tokens
- **Offline Support**: Local queue with retry ensures reliability without internet
- **File Organization**: Clear separation between Commands, Services, UI, and Models
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence