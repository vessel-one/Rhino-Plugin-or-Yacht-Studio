# Implementation Plan: Queued Batch Capture

**Branch**: `003-queued-batch-capture` | **Date**: October 28, 2025 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-queued-batch-capture/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add queued batch capture functionality to allow users to queue multiple viewport captures from different angles/viewports and upload them together as a single batch to Vessel Studio. Users can add captures to queue, review thumbnails, remove unwanted captures, and send all queued images as one project entry with descriptive filenames (ProjectName_ViewportName_Sequence.png). Queue is session-only (clears on plugin unload).

## Technical Context

**Language/Version**: C# .NET Framework 4.8 (Rhino 7/8 compatibility)  
**Primary Dependencies**: RhinoCommon SDK 8.x, System.Windows.Forms (UI), System.Drawing (image handling), Newtonsoft.Json (API communication)  
**Storage**: In-memory queue (session-only, cleared on plugin unload)  
**Testing**: Manual testing via Rhino plugin load/reload, acceptance criteria validation  
**Target Platform**: Windows (mandatory), macOS (architecturally planned)  
**Project Type**: Single project (Rhino plugin with Commands + Services + UI layers)  
**Performance Goals**: Queue 10+ captures in <3 minutes total, batch upload 20 captures without timeout, UI updates <2 second lag  
**Constraints**: Session-only persistence (no disk queue), single project per batch, chronological queue order (no manual reorder), integrate into existing toolbar panel without breaking current UI  
**Scale/Scope**: 1-20 captures per batch typical, 3-10 captures expected common case, thumbnail generation for preview

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ Principle I: Security-First Authentication
**Status**: PASS - Uses existing VesselStudioApiClient authentication. No new auth required. API key validation reused from single capture (FR-014).

### ✅ Principle II: User Experience Excellence  
**Status**: PASS - Commands accessible via Rhino command line ("AddToQueue", "SendBatch"), visual feedback via queue UI with thumbnails (FR-002), progress indication for batch upload (FR-006), integrates into existing toolbar panel (FR-010).

### ✅ Principle III: Cross-Platform Foundation
**Status**: PASS - Queue UI integrated into existing VesselStudioToolbarPanel which uses System.Windows.Forms (cross-platform via Eto.Forms architecture already established). In-memory data structures are platform-agnostic.

### ✅ Principle IV: Resilient Operations
**Status**: PASS - Queue preserved on upload failure for retry (FR-008). Session-only storage avoids stale data issues. Existing image compression logic from single capture reused (no new compression needed).

### ✅ Principle V: Measurable Performance
**Status**: PASS - Clear measurable criteria: <3 min for 10+ captures (SC-001), 20 capture batch limit (SC-002), <2s UI lag (SC-007), 90% first-attempt success rate (SC-003).

### 🔍 Security Requirements
**Status**: PASS - Reuses existing HTTPS API client (VesselStudioApiClient), existing OAuth/API key authentication, no new credential storage needed.

### ✅ Development Quality Gates
**Status**: PASS - User stories are independently testable with Given-When-Then acceptance scenarios. Queue/Upload/UI concerns separated. Edge cases explicitly defined in spec.

## Project Structure

### Documentation (this feature)

```
specs/003-queued-batch-capture/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── api-batch-upload.md
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```
VesselStudioSimplePlugin/
├── Commands/
│   ├── VesselCaptureCommand.cs          # Existing - single capture
│   ├── VesselAddToQueueCommand.cs       # NEW - add viewport to queue
│   ├── VesselSendBatchCommand.cs        # NEW - upload queued batch
│   └── VesselClearQueueCommand.cs       # NEW - clear queue (optional)
├── Services/
│   ├── VesselStudioApiClient.cs         # Existing - API communication
│   ├── CaptureQueueService.cs           # NEW - queue management
│   └── BatchUploadService.cs            # NEW - batch upload logic
├── Models/
│   ├── QueuedCaptureItem.cs             # NEW - single queued capture
│   └── CaptureQueue.cs                  # NEW - queue collection
├── UI/
│   ├── VesselStudioToolbarPanel.cs      # Existing - EXTEND with queue UI
│   └── Components/
│       ├── QueueListControl.cs          # NEW - queue list with thumbnails
│       └── QueueItemControl.cs          # NEW - individual queue item display
├── VesselStudioSettings.cs              # Existing - no changes needed
├── VesselStudioIcons.cs                 # Existing - may add queue icons
└── VesselStudioSimplePlugin.cs          # Existing - register new commands

Resources/
└── [queue-related icons if needed]
```

**Structure Decision**: Single project structure maintained. Queue functionality added via new Commands (user actions), Services (business logic), Models (data structures), and UI components integrated into existing toolbar panel. Follows established layered architecture: Commands → Services → Models, with UI consuming Services.

## Complexity Tracking

*No Constitution violations detected. All principles satisfied by design.*

---

## Phase Completion Status

### ✅ Phase 0: Research (COMPLETE)
**Date**: October 28, 2025  
**Output**: `research.md`  
**Summary**: Resolved 6 technical unknowns:
1. Thumbnail generation strategy (System.Drawing.Bitmap scaling, 80x60px)
2. Queue management approach (List<T> with singleton CaptureQueueService)
3. Batch upload strategy (Sequential with multipart/form-data, fallback to individual)
4. UI integration pattern (Collapsible panel below Quick Tips)
5. Filename generation (Template-based with regex sanitization)
6. Memory management (Compressed JPEG bytes, on-demand thumbnails)

### ✅ Phase 1: Design (COMPLETE)
**Date**: October 28, 2025  
**Outputs**: 
- `data-model.md` - Entity definitions, state transitions, relationships
- `contracts/api-batch-upload.md` - Batch upload service contract
- `quickstart.md` - Developer implementation guide

**Summary**: Designed 3 core entities (QueuedCaptureItem, CaptureQueue, BatchUploadRequest) with full validation rules, lifecycle management, state transition diagrams, and relationships. Created comprehensive API contract for BatchUploadService with method signatures, error handling, progress reporting, and testing scenarios. Generated quickstart guide with 6-phase implementation sequence, code examples, and testing approach.

### ✅ Phase 2: Task Breakdown (COMPLETE)
**Date**: October 28, 2025  
**Output**: `tasks.md`  
**Summary**: Generated 90 discrete implementation tasks organized by user story:
- Phase 1: Setup (4 tasks)
- Phase 2: Foundational models and services (13 tasks) - blocking prerequisites
- Phase 3: US1 - Queue Multiple Captures (11 tasks) - P1
- Phase 4: US2 - Manage Queued Captures (14 tasks) - P1
- Phase 5: US3 - Send Batch to Vessel Studio (21 tasks) - P1
- Phase 6: US4 - Visual Queue Management UI (8 tasks) - P2
- Phase 7: US5 - Automatic File Naming (7 tasks) - P2
- Phase 8: Polish & Edge Cases (12 tasks)

**MVP Scope**: Phases 1-5 (63 tasks) deliver complete core workflow  
**Parallel Opportunities**: 15 tasks marked [P] for concurrent execution  
**Estimated Time**: ~10 hours total, ~7.5 hours for MVP

---

## Agent Context Update

✅ **GitHub Copilot context updated** (October 28, 2025)

Added to `.github/copilot-instructions.md`:
- Language: C# .NET Framework 4.8 (Rhino 7/8 compatibility)
- Framework: RhinoCommon SDK 8.x, System.Windows.Forms, System.Drawing, Newtonsoft.Json
- Storage: In-memory queue (session-only)

Copilot will now provide better suggestions for queued batch capture implementation.

