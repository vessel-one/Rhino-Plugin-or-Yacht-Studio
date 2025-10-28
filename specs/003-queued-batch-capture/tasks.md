# Tasks: Queued Batch Capture

**Feature**: 003-queued-batch-capture  
**Input**: Design documents from `/specs/003-queued-batch-capture/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-batch-upload.md, quickstart.md

**Tests**: Not explicitly requested - manual acceptance testing via Rhino plugin load/reload per plan.md

**Organization**: Tasks grouped by user story (US1-US5) to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions
- Single project: `VesselStudioSimplePlugin/` at repository root
- Models: `VesselStudioSimplePlugin/Models/`
- Services: `VesselStudioSimplePlugin/Services/`
- Commands: `VesselStudioSimplePlugin/Commands/`
- UI: `VesselStudioSimplePlugin/UI/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify project structure and prepare for batch capture implementation

- [ ] T001 Verify .NET Framework 4.8 SDK and RhinoCommon SDK 8.x references in VesselStudioSimplePlugin.csproj
- [ ] T002 Create Models/ directory in VesselStudioSimplePlugin/ (if not exists)
- [ ] T003 Create Services/ directory in VesselStudioSimplePlugin/ (if not exists)
- [ ] T004 Create UI/ directory with Components/ subdirectory in VesselStudioSimplePlugin/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core data models and queue service that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T005 [P] Create QueuedCaptureItem model in VesselStudioSimplePlugin/Models/QueuedCaptureItem.cs with Id, ImageData (byte[]), ViewportName, Timestamp, SequenceNumber, ThumbnailCache (see data-model.md)
- [ ] T006 [P] Implement IDisposable pattern in QueuedCaptureItem for thumbnail and image data cleanup
- [ ] T007 [P] Add validation in QueuedCaptureItem constructor (non-null ImageData, non-empty ViewportName, 5MB size limit per FR-003)
- [ ] T008 [P] Implement GetThumbnail() method in QueuedCaptureItem using System.Drawing.Bitmap scaling to 80x60px (research.md Q1)
- [ ] T009 Create CaptureQueue model in VesselStudioSimplePlugin/Models/CaptureQueue.cs with Items (List<QueuedCaptureItem>), CreatedAt, ProjectName
- [ ] T010 Add computed properties to CaptureQueue: Count, TotalSizeBytes, IsEmpty (data-model.md)
- [ ] T011 Create CaptureQueueService singleton in VesselStudioSimplePlugin/Services/CaptureQueueService.cs (research.md Q2)
- [ ] T012 Implement thread-safe Add/Remove/Clear/GetItems methods in CaptureQueueService with lock for thread safety
- [ ] T013 Add event handlers to CaptureQueueService: ItemAdded, ItemRemoved, QueueCleared for UI updates
- [ ] T014 Enforce 20-item queue limit in CaptureQueueService.AddItem with InvalidOperationException (FR-003, SC-002)
- [ ] T015 Implement auto-assignment of sequence numbers in CaptureQueueService.AddItem (FR-013 chronological order)
- [ ] T016 [P] Create BatchUploadProgress model in VesselStudioSimplePlugin/Models/BatchUploadProgress.cs with TotalItems, CompletedItems, FailedItems, CurrentFilename, PercentComplete (contracts/api-batch-upload.md)
- [ ] T017 [P] Create BatchUploadResult model in VesselStudioSimplePlugin/Models/BatchUploadResult.cs with Success, UploadedCount, FailedCount, Errors list, TotalDurationMs (contracts/api-batch-upload.md)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Queue Multiple Viewport Captures (Priority: P1) 🎯 MVP

**Goal**: Users can capture multiple viewport angles and queue them without immediate upload, seeing queued items in a list

**Independent Test**: Add 3+ captures from different viewports → toolbar badge shows "Batch (N)" → queue persists until deliberately cleared or sent

### Implementation for User Story 1

- [ ] T018 [US1] Create VesselAddToQueueCommand in VesselStudioSimplePlugin/Commands/VesselAddToQueueCommand.cs with EnglishName "VesselAddToQueue"
- [ ] T019 [US1] Implement viewport capture logic in VesselAddToQueueCommand.RunCommand: get active viewport, call CaptureToBitmap() (follow existing VesselCaptureCommand.cs pattern)
- [ ] T020 [US1] Implement JPEG compression in VesselAddToQueueCommand using System.Drawing.Imaging.ImageCodecInfo with 85% quality (research.md Q6)
- [ ] T021 [US1] Create QueuedCaptureItem from compressed bytes and add to CaptureQueueService.Current in VesselAddToQueueCommand
- [ ] T022 [US1] Add error handling in VesselAddToQueueCommand for no active viewport, capture failure, queue limit exceeded (FR-003)
- [ ] T023 [US1] Write success message to RhinoApp.WriteLine showing current queue count after add (FR-002 feedback)
- [ ] T024 [US1] Register VesselAddToQueueCommand in VesselStudioSimplePlugin.cs plugin class
- [ ] T025 [US1] Add batch badge button in VesselStudioToolbarPanel.cs showing "Batch (0)" initially, width 80px (research.md Q4)
- [ ] T026 [US1] Subscribe to CaptureQueueService events (ItemAdded, ItemRemoved, QueueCleared) in VesselStudioToolbarPanel
- [ ] T027 [US1] Implement UpdateBatchCount event handler in VesselStudioToolbarPanel to update badge text to "Batch (N)" on queue changes (FR-011)
- [ ] T028 [US1] Marshal UI updates to main thread using Control.Invoke in UpdateBatchCount handler (thread safety)

**Acceptance Tests** (manual via Rhino):
- Add capture from Perspective viewport → badge shows "Batch (1)"
- Change viewport angle → add again → badge shows "Batch (2)"
- Switch to Top viewport → add capture → badge shows "Batch (3)"
- All 3 captures preserved in queue until deliberately removed

**Checkpoint**: User Story 1 complete - users can queue multiple captures and see count badge

---

## Phase 4: User Story 2 - Manage Queued Captures (Priority: P1)

**Goal**: Users can review queued captures with large thumbnails, remove unwanted items, and clear entire queue

**Independent Test**: Add multiple captures → click badge → popup shows items with large thumbnails → remove specific items → queue updates correctly

### Implementation for User Story 2

- [ ] T029 [US2] Create QueueManagerDialog in VesselStudioSimplePlugin/UI/QueueManagerDialog.cs as modal Form with Size 600x500 (research.md Q4)
- [ ] T030 [US2] Add ListView to QueueManagerDialog with View.LargeIcon, CheckBoxes enabled, ImageList with 120x90px size (FR-019)
- [ ] T031 [US2] Implement LoadQueueItems in QueueManagerDialog to populate ListView from CaptureQueueService.Current.GetItems()
- [ ] T032 [US2] Generate 120x90px thumbnails in LoadQueueItems using ScaleThumbnail helper with HighQualityBicubic interpolation (FR-019)
- [ ] T033 [US2] Add action buttons to QueueManagerDialog: SelectAll, SelectNone, RemoveSelected (disabled initially), ClearAll, ExportAll
- [ ] T034 [US2] Implement SelectAllItems and SelectNoneItems handlers to check/uncheck all ListView items (FR-018 checkbox support)
- [ ] T035 [US2] Implement OnItemCheckedChanged handler to enable RemoveSelected button only when items checked (FR-018)
- [ ] T036 [US2] Implement RemoveSelectedItems handler with confirmation dialog and CaptureQueueService.RemoveItem calls (FR-003)
- [ ] T037 [US2] Implement ClearAllItems handler with "Clear entire queue?" confirmation and CaptureQueueService.Clear call (FR-004)
- [ ] T038 [US2] Call LoadQueueItems after remove operations to refresh ListView display (FR-002 immediate update)
- [ ] T039 [US2] Implement IDisposable in QueueManagerDialog to dispose ImageList (memory management per research.md Q6)
- [ ] T040 [US2] Wire badge button click in VesselStudioToolbarPanel to OpenQueueManagerDialog showing modal dialog (research.md Q4)
- [ ] T041 [US2] Create VesselClearQueueCommand in VesselStudioSimplePlugin/Commands/VesselClearQueueCommand.cs with confirmation prompt (optional command for Rhino command line)
- [ ] T042 [US2] Register VesselClearQueueCommand in VesselStudioSimplePlugin.cs

**Acceptance Tests** (manual via Rhino):
- Add 5 captures → click badge → dialog opens within 500ms showing 5 large thumbnails (SC-008)
- Check 2 items → RemoveSelected enabled → click → confirmation shown → 2 items removed
- Click ClearAll → confirmation shown → all items cleared → dialog closes
- Queue empty message shown when Count = 0 (FR-004 scenario 4)

**Checkpoint**: User Stories 1 AND 2 complete - users can queue captures and manage them via popup dialog

---

## Phase 5: User Story 3 - Send Queued Batch to Vessel Studio (Priority: P1) 🎯 MVP Core

**Goal**: Users can upload all queued captures as a single batch with progress indication and error handling

**Independent Test**: Queue 3+ captures → click Quick Export Batch → all images upload to Vessel Studio together → queue clears on success

### Implementation for User Story 3

- [ ] T043 [US3] Create BatchUploadService in VesselStudioSimplePlugin/Services/BatchUploadService.cs accepting VesselStudioApiClient in constructor
- [ ] T044 [US3] Implement GenerateFilename private method in BatchUploadService using pattern ProjectName_ViewportName_Sequence.png with regex sanitization (FR-017, research.md Q5)
- [ ] T045 [US3] Implement UploadBatchAsync in BatchUploadService with validation (queue not empty, projectId not null, API key configured per FR-014)
- [ ] T046 [US3] Implement sequential upload loop in UploadBatchAsync calling existing VesselStudioApiClient.UploadImageAsync for each item (research.md Q3)
- [ ] T047 [US3] Add IProgress<BatchUploadProgress> reporting in upload loop after each item completion (SC-007 <2s lag)
- [ ] T048 [US3] Collect errors for failed items in upload loop continuing on individual failures (FR-008 partial success)
- [ ] T049 [US3] Check CancellationToken between items in upload loop for user abort support (contracts/api-batch-upload.md)
- [ ] T050 [US3] Return BatchUploadResult with Success, UploadedCount, FailedCount, Errors, TotalDurationMs (contracts/api-batch-upload.md)
- [ ] T051 [US3] Call CaptureQueueService.Clear only on complete success (all items uploaded, FR-007)
- [ ] T052 [US3] Preserve queue on any failure or partial success for retry (FR-008)
- [ ] T053 [US3] Add Quick Export Batch button in VesselStudioToolbarPanel below badge, width 140px, initially disabled (research.md Q4)
- [ ] T054 [US3] Enable/disable Quick Export button in UpdateBatchCount handler based on count > 0 (FR-012 prevent empty batch)
- [ ] T055 [US3] Implement QuickExportBatch async click handler in VesselStudioToolbarPanel creating BatchUploadService and calling UploadBatchAsync
- [ ] T056 [US3] Get projectId from existing project dropdown in QuickExportBatch handler (FR-016 single project per batch)
- [ ] T057 [US3] Show MessageBox with success count on complete upload in QuickExportBatch handler (FR-006 progress indication)
- [ ] T058 [US3] Show MessageBox with error count on failed/partial upload in QuickExportBatch handler (FR-006, FR-008)
- [ ] T059 [US3] Add ExportAllItems async handler in QueueManagerDialog calling BatchUploadService.UploadBatchAsync (same logic as QuickExportBatch)
- [ ] T060 [US3] Close QueueManagerDialog on successful complete upload in ExportAllItems handler (FR-007)
- [ ] T061 [US3] Keep QueueManagerDialog open on partial/failed upload showing error details (FR-008 allow retry)
- [ ] T062 [US3] Create VesselSendBatchCommand in VesselStudioSimplePlugin/Commands/VesselSendBatchCommand.cs as alternative to button click
- [ ] T063 [US3] Register VesselSendBatchCommand in VesselStudioSimplePlugin.cs

**Acceptance Tests** (manual via Rhino):
- Queue 5 captures → click Quick Export Batch → all 5 upload successfully → queue clears → badge shows "Batch (0)" (SC-001 <3 min for 10+ captures)
- Disconnect network → click Quick Export → upload fails → queue preserved → can retry (FR-008)
- Mock API to fail on item 3 → send batch → items 1,2 succeed, 3 fails → error dialog shows details → queue preserved with all 5 items
- No API key configured → click Quick Export → prompted to configure API key (FR-014)
- Queue empty → Quick Export button disabled, cannot send (FR-012)

**Checkpoint**: MVP COMPLETE - User Stories 1, 2, 3 deliver core batch workflow (queue, manage, upload)

---

## Phase 6: User Story 4 - Visual Queue Management UI (Priority: P2)

**Goal**: Enhanced UI feedback with real-time updates and polish (already implemented in US1, US2 - this phase is validation/polish)

**Independent Test**: Add captures → badge updates immediately → popup opens quickly → checkboxes work smoothly

### Implementation for User Story 4

- [ ] T064 [US4] Verify badge updates within 1 second of queue changes (performance validation for SC-007)
- [ ] T065 [US4] Verify popup dialog opens within 500ms of badge click (performance validation for SC-008)
- [ ] T066 [US4] Verify thumbnails in popup are 120x90px minimum size (validation for FR-019)
- [ ] T067 [US4] Verify checkboxes work for multi-select in popup dialog (validation for FR-018)
- [ ] T068 [US4] Add visual feedback (greyed out) to badge when queue empty (FR-011 disabled state)
- [ ] T069 [US4] Ensure Quick Export button visually indicates enabled/disabled state (FR-012 UX)
- [ ] T070 [US4] Test with 20 items in queue to verify scrolling works in popup ListView (SC-002 edge case)
- [ ] T071 [US4] Verify toolbar layout not broken by batch controls - no overlap with existing Settings/Capture buttons (SC-004)

**Acceptance Tests** (manual via Rhino):
- Add item → badge updates within 1 second (SC-007)
- Click badge → popup opens within 500ms (SC-008)
- Thumbnails are large and clear (120x90px min, FR-019)
- Checkboxes allow selecting multiple items for batch remove (FR-018)
- Badge greyed out when empty, bright when has items
- Quick Export button clearly disabled when empty, enabled when has items

**Checkpoint**: User Stories 1-4 complete - full UI/UX polish applied

---

## Phase 7: User Story 5 - Automatic File Naming for Batches (Priority: P2)

**Goal**: Validate filename generation produces unique, descriptive names following ProjectName_ViewportName_Sequence.png pattern

**Independent Test**: Send batch → verify filenames in Vessel Studio follow pattern with correct project name, viewport names, sequential numbers

### Implementation for User Story 5

- [ ] T072 [US5] Validate GenerateFilename produces correct pattern for single viewport (e.g., "YachtA_Perspective_001.png") in BatchUploadService
- [ ] T073 [US5] Validate GenerateFilename sanitizes illegal characters (spaces → underscores, remove <>:"/\|?*) per research.md Q5
- [ ] T074 [US5] Validate sequence numbers are zero-padded to 3 digits (001, 002... 010... 099) for proper sorting (FR-017)
- [ ] T075 [US5] Test batch with captures from different viewports (Perspective, Top, Front) → each gets correct viewport name in filename (FR-009)
- [ ] T076 [US5] Test batch with project name containing spaces/special chars → filename sanitized correctly (FR-017 sanitization)
- [ ] T077 [US5] Verify filenames are unique across entire batch (no collisions) with SC-005 success criteria
- [ ] T078 [US5] Verify project name from dropdown is included in all filenames (FR-016 single project per batch)

**Acceptance Tests** (manual via Rhino):
- Select project "Yacht Design A" → queue 3 captures (Perspective, Top, Front) → send batch → filenames are:
  - Yacht_Design_A_Perspective_001.png
  - Yacht_Design_A_Top_002.png
  - Yacht_Design_A_Front_003.png
- All filenames unique and descriptive (SC-005)
- Project name correctly extracted from dropdown and sanitized

**Checkpoint**: All user stories (1-5) complete - full feature delivered

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, edge case handling, and documentation

- [ ] T079 [P] Test adding 21st capture → verify error message shown and queue remains at 20 (FR-003 limit enforcement)
- [ ] T080 [P] Test queue persistence across viewport changes → verify queue not cleared when switching viewports (FR-015 session-only but persistent within session)
- [ ] T081 [P] Test plugin unload → verify queue is cleared and all QueuedCaptureItems disposed (FR-015)
- [ ] T082 [P] Test batch upload with 20 items → verify completes within timeout with SC-002 success criteria
- [ ] T083 [P] Test adding captures quickly → verify 10+ captures queued in under 3 minutes (SC-001 performance)
- [ ] T084 [P] Verify chronological ordering maintained in queue → first added = first in list (FR-013)
- [ ] T085 [P] Test with no active viewport → verify VesselAddToQueueCommand shows appropriate error message
- [ ] T086 [P] Test with invalid/expired API key → verify batch upload shows clear error before attempting upload (FR-014)
- [ ] T087 Update VesselStudioSimplePlugin.manifest to list new commands (VesselAddToQueue, VesselSendBatch, VesselClearQueue)
- [ ] T088 Validate all acceptance scenarios from spec.md user stories manually in Rhino 8
- [ ] T089 Document any deviations from spec in CHANGELOG.md or implementation notes
- [ ] T090 Run quickstart.md validation steps to ensure guide accuracy

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational phase - can start after T017
- **User Story 2 (Phase 4)**: Depends on Foundational phase - can start after T017 (independent of US1 but integrates with it)
- **User Story 3 (Phase 5)**: Depends on Foundational phase - can start after T017, integrates with US1 and US2
- **User Story 4 (Phase 6)**: Validation/polish phase - depends on US1, US2, US3 completion
- **User Story 5 (Phase 7)**: Validation phase - depends on US3 completion (tests BatchUploadService.GenerateFilename)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Queue captures and show count badge
  - Depends on: Foundational (Phase 2) complete
  - Blocks: None (US2 and US3 can start independently)
  
- **User Story 2 (P1)**: Manage queue via popup dialog
  - Depends on: Foundational (Phase 2) complete
  - Integrates with: US1 (uses badge button), but can be implemented independently
  - Blocks: None
  
- **User Story 3 (P1)**: Upload batch to Vessel Studio
  - Depends on: Foundational (Phase 2) complete
  - Integrates with: US1 (Quick Export button in toolbar), US2 (Export All in popup)
  - Blocks: US5 (filename validation requires upload working)
  
- **User Story 4 (P2)**: UI polish and validation
  - Depends on: US1, US2, US3 complete
  - Blocks: None (optional polish)
  
- **User Story 5 (P2)**: Filename generation validation
  - Depends on: US3 complete (requires BatchUploadService)
  - Blocks: None

### Within Each User Story

- **US1**: T018 (command) → T019-T023 (command logic) → T024 (register) → T025-T028 (toolbar UI)
- **US2**: T029-T033 (dialog structure) → T034-T039 (dialog handlers) → T040-T042 (integration)
- **US3**: T043-T044 (service setup) → T045-T052 (upload logic) → T053-T058 (toolbar integration) → T059-T063 (dialog/command integration)
- **US4**: All tasks are validation/testing, can run in parallel
- **US5**: All tasks are validation/testing, can run in parallel

### Parallel Opportunities

**Within Foundational Phase**:
- T005-T008 (QueuedCaptureItem) can run parallel with T016-T017 (BatchUpload models)
- T009-T010 (CaptureQueue model) must wait for T005-T008
- T011-T015 (CaptureQueueService) must wait for T009-T010

**Within User Story 3**:
- T043-T044 (service methods) before T045-T052 (async logic)
- T053-T058 (toolbar) can run parallel with T059-T061 (dialog) if different developers

**Within Polish Phase**:
- All T079-T086 testing tasks can run in parallel (different test scenarios)

---

## Parallel Example: Foundational Phase

```bash
# Start together (parallel - different files):
Task T005: Create QueuedCaptureItem model
Task T016: Create BatchUploadProgress model
Task T017: Create BatchUploadResult model

# After T005 completes:
Task T006: Implement IDisposable in QueuedCaptureItem
Task T007: Add validation in QueuedCaptureItem
Task T008: Implement GetThumbnail() in QueuedCaptureItem

# After T008 completes:
Task T009: Create CaptureQueue model (depends on QueuedCaptureItem)
Task T010: Add computed properties to CaptureQueue

# After T010 completes:
Task T011: Create CaptureQueueService singleton
Task T012: Implement Add/Remove/Clear methods
Task T013: Add event handlers
Task T014: Enforce 20-item limit
Task T015: Auto-assign sequence numbers
```

---

## Implementation Strategy

### MVP First (User Stories 1, 2, 3 Only) 🎯

**Recommended MVP Scope**:

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational (T005-T017) - CRITICAL foundation
3. Complete Phase 3: User Story 1 (T018-T028) - Queue captures, show badge
4. Complete Phase 4: User Story 2 (T029-T042) - Manage queue via popup
5. Complete Phase 5: User Story 3 (T043-T063) - Upload batch
6. **STOP and VALIDATE**: Test end-to-end workflow independently
7. Deploy/demo if ready

**Rationale**: US1+US2+US3 deliver complete core value:
- Users can queue captures (US1)
- Users can review and manage queue (US2)
- Users can upload batch (US3)

US4 and US5 are polish/validation that can be added later.

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Users can queue captures
3. Add User Story 2 → Test independently → Users can manage queue
4. Add User Story 3 → Test independently → **MVP COMPLETE** - Deploy/Demo!
5. Add User Story 4 → Polish UI/UX → Enhanced experience
6. Add User Story 5 → Validate filenames → Full feature complete

Each increment adds value without breaking previous functionality.

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together (CRITICAL - don't parallelize this)
2. Once Foundational is done (T017 complete):
   - Developer A: User Story 1 (T018-T028) - Queue and badge
   - Developer B: User Story 2 (T029-T042) - Popup dialog
   - Developer C: User Story 3 (T043-T063) - Batch upload service
3. Integration: Connect US3 to US1 toolbar button and US2 dialog button
4. Stories integrate naturally at UI connection points

---

## Summary

**Total Tasks**: 90 tasks
- Phase 1 (Setup): 4 tasks
- Phase 2 (Foundational): 13 tasks
- Phase 3 (US1 - Queue Captures): 11 tasks
- Phase 4 (US2 - Manage Queue): 14 tasks
- Phase 5 (US3 - Upload Batch): 21 tasks
- Phase 6 (US4 - UI Polish): 8 tasks
- Phase 7 (US5 - Filename Validation): 7 tasks
- Phase 8 (Polish): 12 tasks

**Parallel Opportunities**: 15 tasks marked [P] for parallel execution

**MVP Scope**: Phases 1-5 (US1+US2+US3) = 63 tasks for complete core workflow

**User Story Breakdown**:
- US1: 11 tasks (queue captures, show badge)
- US2: 14 tasks (manage queue via popup dialog)
- US3: 21 tasks (upload batch with progress)
- US4: 8 tasks (UI polish/validation)
- US5: 7 tasks (filename validation)

**Estimated Time** (per quickstart.md):
- Phase 1: 15 min (setup)
- Phase 2: 2 hours (foundational models and services)
- Phase 3: 1.5 hours (US1 - command and toolbar badge)
- Phase 4: 2 hours (US2 - popup dialog)
- Phase 5: 2 hours (US3 - batch upload service and integration)
- Phase 6: 1 hour (US4 - validation/polish)
- Phase 7: 30 min (US5 - filename testing)
- Phase 8: 1 hour (final polish and edge cases)
- **Total**: ~10 hours for complete feature (single developer, focused work)

**Suggested MVP**: Complete through Phase 5 (US1+US2+US3) = ~7.5 hours = 1 day focused work
