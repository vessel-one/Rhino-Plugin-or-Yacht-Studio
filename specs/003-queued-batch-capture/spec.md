# Feature Specification: Queued Batch Capture

**Feature Branch**: `003-queued-batch-capture`  
**Created**: October 28, 2025  
**Status**: Draft  
**Input**: User description: "We want to add qued capture. user can press add to que, change the view, add to que slect antoher view port add to que. then send to vessel studio as a batch. unsuer how we handle file naming in this instance open to suggestion. in toolbar we could ahve a small tab maybe or below the documetation text etc a window with the added shoots. ability to remove any from the shot and then send as a batch"

## Clarifications

### Session 2025-10-28

- Q: Should queue persist between Rhino sessions, or clear when plugin unloads? → A: Clear queue when plugin unloads (session-only persistence)
- Q: Should batch uploads create one project entry with multiple images, or multiple project entries that are linked? → A: One project entry with multiple images attached
- Q: What naming pattern should be used for batch filenames? → A: ProjectName_ViewportName_Sequence.png (without timestamp)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Queue Multiple Viewport Captures (Priority: P1)

A user wants to capture multiple views of their yacht design from different angles and perspectives before uploading them all together to Vessel Studio. Instead of capturing and uploading each view separately (which would create multiple separate entries), they can queue up all their desired captures and send them as a single batch.

**Why this priority**: This is the core functionality that enables the batch workflow. Without it, users must interrupt their creative flow to upload each capture individually.

**Independent Test**: Can be fully tested by adding 3+ viewport captures to the queue, verifying they appear in the queue list, and confirming the queue persists until deliberately cleared or sent.

**Acceptance Scenarios**:

1. **Given** user is viewing their yacht model in any viewport, **When** user clicks "Add to Queue" button, **Then** current viewport capture is added to the queue and appears in the queue list
2. **Given** user has added a capture to the queue, **When** user changes the viewport angle/perspective, **Then** user can add another capture from the new viewpoint without affecting previous queued captures
3. **Given** user has added captures from a perspective viewport, **When** user switches to a different viewport (Top, Front, Right, etc.), **Then** user can add captures from the new viewport to the same queue
4. **Given** queue contains multiple captures, **When** user reviews the queue list, **Then** each capture shows a preview thumbnail and identifying information (viewport name, timestamp, or sequence number)

---

### User Story 2 - Manage Queued Captures (Priority: P1)

A user realizes they queued a capture from the wrong angle or want to reorder their shots before sending. They need to review, remove, or reorganize queued captures before uploading.

**Why this priority**: Users need control over their queue to correct mistakes without starting over. This is essential for the queue to be useful rather than frustrating.

**Independent Test**: Can be fully tested by adding multiple captures to queue, removing specific items, and verifying the queue updates correctly without affecting remaining items.

**Acceptance Scenarios**:

1. **Given** queue contains multiple captures, **When** user selects a queued capture, **Then** user can see a larger preview of that specific capture
2. **Given** user is reviewing a queued capture, **When** user clicks "Remove" or delete action, **Then** that capture is removed from the queue and the list updates immediately
3. **Given** queue contains captures, **When** user wants to clear all queued items, **Then** user can clear the entire queue with a single action (with confirmation prompt to prevent accidents)
4. **Given** queue is empty, **When** user views the queue area, **Then** user sees a helpful message indicating the queue is empty and instructions to add captures

---

### User Story 3 - Send Queued Batch to Vessel Studio (Priority: P1)

After queuing multiple viewport captures, the user is ready to upload them all at once to Vessel Studio as a single batch submission.

**Why this priority**: This completes the core workflow - without batch sending, the queue has no purpose. This must work for the feature to deliver any value.

**Independent Test**: Can be fully tested by queuing 3+ captures and clicking "Send Batch", then verifying all images are uploaded together and associated as a single submission in Vessel Studio.

**Acceptance Scenarios**:

1. **Given** queue contains at least one capture, **When** user clicks "Send Batch" button, **Then** all queued captures are uploaded to Vessel Studio together
2. **Given** batch upload is in progress, **When** user observes the upload, **Then** user sees progress indication showing overall batch progress and individual image status
3. **Given** batch upload completes successfully, **When** upload finishes, **Then** queue is automatically cleared and user receives success confirmation with details about the batch submission
4. **Given** batch upload encounters an error, **When** upload fails, **Then** user receives clear error message, queued captures remain in queue, and user can retry the upload
5. **Given** user has not set API key, **When** user attempts to send batch, **Then** user is prompted to configure API key before proceeding (consistent with existing single capture behavior)

---

### User Story 4 - Visual Queue Management UI (Priority: P2)

The user needs an intuitive, space-efficient way to view and manage their capture queue without it overwhelming the main toolbar panel interface.

**Why this priority**: While essential for usability, the specific UI presentation can be iterated after core queue functionality works. Users can tolerate a basic list view initially.

**Independent Test**: Can be fully tested by adding captures and verifying the queue display updates in real-time, shows accurate information, and doesn't interfere with existing toolbar functionality.

**Acceptance Scenarios**:

1. **Given** user has toolbar panel open, **When** queue is empty, **Then** toolbar shows "Batch (0)" or similar compact indicator (greyed out/disabled)
2. **Given** user adds captures to queue, **When** queue is not empty, **Then** toolbar shows "Batch (N)" count badge and "Quick Export Batch" button becomes enabled
3. **Given** user clicks on "Batch (N)" indicator, **When** popup dialog opens, **Then** user sees full queue management interface with large thumbnails, checkboxes, viewport names, and action buttons
4. **Given** user has queue management popup open, **When** user selects items via checkboxes, **Then** user can perform batch actions like "Remove Selected" or "Clear All"
3. **Given** queue contains multiple captures, **When** user views queue area, **Then** captures are displayed in chronological order (oldest to newest) with visual indicators
4. **Given** queue display is visible, **When** user interacts with other toolbar elements (Settings, Capture button, etc.), **Then** queue display does not interfere with existing functionality
5. **Given** queue section is expanded, **When** user wants to focus on other tasks, **Then** user can collapse/minimize the queue section while preserving queued captures

---

### User Story 5 - Automatic File Naming for Batches (Priority: P2)

When users send queued captures as a batch, the system automatically generates meaningful, organized filenames without requiring manual input for each image.

**Why this priority**: Automated naming prevents user frustration and maintains workflow efficiency. However, basic sequential naming can work initially while more sophisticated naming is refined.

**Independent Test**: Can be fully tested by sending a batch and verifying filenames are unique, meaningful, and follow consistent naming conventions.

**Acceptance Scenarios**:

1. **Given** user sends a batch of queued captures, **When** files are uploaded, **Then** each file has a unique, descriptive filename that includes project context
2. **Given** batch contains captures from different viewports, **When** files are named, **Then** filenames indicate which viewport each capture originated from (e.g., "Perspective", "Top", "Front")
3. **Given** user sends multiple batches in the same session, **When** files are named, **Then** batch sequence or timestamp distinguishes files from different batches
4. **Given** user has selected a project in the dropdown, **When** batch is sent, **Then** filenames include project identifier to maintain organization in Vessel Studio

---

### Edge Cases

- What happens when user adds the same viewport capture to queue multiple times? (Allow duplicates with sequence numbers, or prevent duplicates with user notification?)
- How does system handle queue persistence if user closes Rhino before sending batch? (Save queue to disk, or clear queue on close?)
- What happens when batch upload is interrupted (connection loss, Rhino crash)? (Keep queue intact for retry, or partial upload handling?)
- How does system handle very large queues (20+ captures)? (UI scrolling, memory management, upload chunking?)
- What happens when user tries to add a capture to queue but no valid viewport is active?
- How does system handle captures from viewports with identical views/angles? (Allow with sequential naming, or warn user?)
- What happens if API key becomes invalid while queue has pending captures?
- How does batch upload interact with the existing single-capture workflow? (Can they coexist, or does one disable the other?)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an "Add to Queue" action that captures the current active viewport and adds it to a queue without immediately uploading
- **FR-002**: System MUST display queued captures in a visual list showing preview thumbnails, viewport source, and capture sequence
- **FR-003**: System MUST allow users to remove individual captures from the queue before sending
- **FR-004**: System MUST provide a "Clear Queue" action that removes all queued captures with confirmation prompt
- **FR-005**: System MUST provide a "Send Batch" action that uploads all queued captures to Vessel Studio as a single batch submission
- **FR-006**: System MUST show batch upload progress indicating overall completion percentage and status of individual captures
- **FR-007**: System MUST automatically clear the queue after successful batch upload
- **FR-008**: System MUST preserve the queue if batch upload fails, allowing users to retry
- **FR-009**: System MUST generate unique filenames for each capture in a batch that include viewport identifier and sequence information
- **FR-010**: System MUST provide compact queue UI in toolbar panel showing batch count badge (e.g., "Batch (3)") and "Quick Export Batch" button, with full queue management accessible via popup dialog
- **FR-011**: System MUST open queue management popup dialog when user clicks batch count badge, showing large thumbnails, checkboxes, viewport names, and action buttons (Remove Selected, Clear All, Export)
- **FR-012**: System MUST prevent sending empty batches (disable "Quick Export Batch" button when queue is empty)
- **FR-013**: System MUST maintain queue order chronologically (first added = first in list)
- **FR-014**: System MUST validate API key before allowing batch upload, consistent with existing single capture behavior
- **FR-015**: System MUST clear queue when plugin unloads or Rhino session ends (session-only persistence)
- **FR-016**: System MUST create one project entry with multiple images attached when uploading a batch (all captures grouped under single project)
- **FR-017**: Filenames MUST follow the pattern ProjectName_ViewportName_Sequence.png where ProjectName is from the selected project dropdown, ViewportName identifies the source viewport (e.g., "Perspective", "Top", "Front"), and Sequence is a zero-padded number (001, 002, etc.)
- **FR-018**: Popup dialog MUST allow users to select/deselect captures via checkboxes for batch operations (remove selected, export selected subset)
- **FR-019**: Popup dialog MUST show larger thumbnails (min 120x90px) than toolbar would allow for easier visual review

### Key Entities

- **Capture Queue**: Collection of viewport captures awaiting batch upload
  - Contains: ordered list of capture items, creation timestamp, total count
  - Relationships: belongs to current Rhino session, associated with selected project

- **Queued Capture Item**: Individual viewport capture in the queue
  - Contains: image data, viewport identifier, timestamp, sequence number, thumbnail preview
  - Relationships: part of Capture Queue, references source viewport

- **Batch Upload**: Single upload operation containing multiple captures
  - Contains: list of queued captures, upload progress, status (pending/in-progress/complete/failed)
  - Relationships: associated with project, contains multiple captures

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can add 10+ viewport captures to queue and send as batch in under 3 minutes (compared to 10+ minutes for individual uploads)
- **SC-002**: Batch upload completes successfully for queues containing up to 20 captures without errors or timeouts
- **SC-003**: 90% of users successfully queue and send multiple captures on their first attempt without needing support
- **SC-004**: Toolbar batch controls (count badge + Quick Export button) occupy minimal space and don't break existing toolbar layout
- **SC-005**: All queued captures in a batch receive unique, identifiable filenames that prevent naming conflicts
- **SC-006**: Users can identify and remove incorrect captures from queue within 10 seconds using popup dialog
- **SC-007**: Batch upload progress updates in real-time with no more than 2-second lag between actual progress and displayed progress
- **SC-008**: Popup dialog opens within 500ms of clicking batch count badge, with all thumbnails visible immediately

## Assumptions

- Users will typically queue 3-10 captures per batch (based on common yacht documentation needs: port/starboard/bow/stern views plus detail shots)
- Viewport captures use the same image quality and resolution settings as existing single capture feature
- Vessel Studio API can accept batch/multiple image uploads in a single request (or system will send sequential uploads as a logical batch)
- Toolbar will show compact batch count badge (e.g., "Batch (3)") and "Quick Export Batch" button without cramming thumbnails into limited toolbar space
- Full queue management UI (thumbnails, checkboxes, remove buttons) will be in a popup dialog for easier viewing and interaction
- File size limits per capture remain the same as single capture (no special handling needed for batch vs single)
- Users will want to see larger thumbnails in popup dialog to verify correct captures are queued (not tiny toolbar thumbnails)
- "Batch" means images are logically grouped/associated in Vessel Studio, even if uploaded as separate API calls
- Existing project selection dropdown applies to all captures in queue (single project per batch, not mixed projects)

## Dependencies

- Existing viewport capture functionality (VesselCaptureCommand)
- Existing API client and authentication (VesselStudioApiClient, VesselSetApiKeyCommand)
- Existing toolbar panel UI framework (VesselStudioToolbarPanel)
- Project selection functionality (assuming this exists based on "project dropdown" mention in input)

## Out of Scope

- Editing or annotating queued captures before upload (future enhancement)
- Reordering queued captures manually (initial version uses chronological order only)
- Queuing captures from multiple Rhino documents simultaneously (single document per queue)
- Automatic capture queuing based on viewport changes or rules (manual "Add to Queue" only)
- Batch download or retrieval from Vessel Studio (upload only)
- Queue templates or presets for common capture sequences
- Sharing queues between users or sessions

