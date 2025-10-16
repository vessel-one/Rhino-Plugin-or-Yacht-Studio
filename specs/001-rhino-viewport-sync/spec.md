# Feature Specification: Rhino Viewport Sync Plugin

**Feature Branch**: `001-rhino-viewport-sync`  
**Created**: October 16, 2025  
**Status**: Draft  
**Input**: User description: "Rhino plugin that captures the current viewport and sends it to an open Vessel Studio project canvas. Users authenticate via the plugin, select their project, and screenshots are automatically synced to their web browser in real-time."

## Clarifications

### Session 2025-10-16

- Q: When a user's authentication token expires during active plugin use, what should happen? → A: Plugin attempts silent token refresh, prompts only if refresh fails
- Q: How should the plugin handle very large viewport images (4K+ resolution)? → A: Compress images above threshold while preserving metadata
- Q: When the user loses internet connectivity during viewport capture, what should happen? → A: Queue capture locally, auto-retry upload when connectivity returns
- Q: How should the plugin behave when the selected Vessel Studio project is deleted from the web? → A: Detect deleted project on upload, prompt user to select new project
- Q: What should happen when multiple Rhino instances try to authenticate simultaneously? → A: Allow each instance to authenticate independently with separate sessions

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One-Click Viewport Capture (Priority: P1)

A yacht designer working in Rhino 3D wants to share their current viewport with their team by uploading it directly to their Vessel Studio project without leaving Rhino or switching applications.

**Why this priority**: This is the core value proposition - eliminating the friction of manual screenshot capture, saving, and uploading. It's the minimum viable feature that delivers immediate value.

**Independent Test**: Can be fully tested by opening Rhino, running the capture command, and verifying the screenshot appears in the web project. Delivers immediate value of seamless viewport sharing.

**Acceptance Scenarios**:

1. **Given** a user has an active Rhino viewport with a 3D model, **When** they run the viewport capture command, **Then** the current viewport is captured as an image and uploaded to their selected Vessel Studio project
2. **Given** a user captures a viewport, **When** the upload completes, **Then** they receive confirmation with a link to view the image in their web browser
3. **Given** a user captures a viewport from a specific display mode (wireframe, shaded, rendered), **When** the image is uploaded, **Then** the viewport metadata (display mode, camera position) is preserved with the image

---

### User Story 2 - Secure Plugin Authentication (Priority: P1)

A user needs to securely connect their Rhino plugin to their Vessel Studio account to ensure screenshots are uploaded to their private projects.

**Why this priority**: Security and user account association is fundamental - without authentication, the plugin cannot determine which projects belong to the user or ensure data privacy.

**Independent Test**: Can be fully tested by running the login command, completing web authentication, and verifying the plugin recognizes the authenticated user. Delivers security foundation for all other features.

**Acceptance Scenarios**:

1. **Given** a user has a Vessel Studio account, **When** they run the login command in Rhino, **Then** their web browser opens to the authentication page
2. **Given** a user completes web authentication, **When** they return to Rhino, **Then** the plugin confirms successful login and remembers their credentials for future sessions
3. **Given** an authenticated user closes and reopens Rhino, **When** they use the plugin, **Then** they remain logged in without re-authentication

---

### User Story 3 - Project Selection Interface (Priority: P2)

A user with multiple Vessel Studio projects needs to choose which project should receive their viewport screenshots before uploading.

**Why this priority**: Essential for users with multiple projects, but the plugin could theoretically work with a single default project. Enables organized workflow management.

**Independent Test**: Can be fully tested by logging in, running the capture command, and selecting from a list of available projects. Delivers project organization value.

**Acceptance Scenarios**:

1. **Given** an authenticated user with multiple projects, **When** they initiate a viewport capture, **Then** they see a dialog listing their available Vessel Studio projects
2. **Given** a user selects a project from the list, **When** they confirm the selection, **Then** the viewport is uploaded to that specific project
3. **Given** a user has previously selected a project, **When** they capture another viewport in the same session, **Then** the plugin remembers their project choice for convenience

---

### User Story 4 - Real-time Web Sync (Priority: P3)

A team member viewing the Vessel Studio project in their web browser wants to see new Rhino viewport screenshots appear automatically without manually refreshing the page.

**Why this priority**: Enhances collaboration experience but the core functionality works without real-time updates. Users can still refresh manually to see new content.

**Independent Test**: Can be fully tested by having one user upload from Rhino while another watches the web project page. Delivers enhanced collaboration experience.

**Acceptance Scenarios**:

1. **Given** a user has a Vessel Studio project open in their web browser, **When** a team member uploads a viewport from Rhino to that project, **Then** the new image appears in the browser within 5 seconds without page refresh
2. **Given** multiple users are viewing the same project, **When** any user uploads a viewport, **Then** all viewers see the update simultaneously

---

### Edge Cases

- What happens when the user loses internet connectivity during viewport capture?
- How does the system handle very large viewport images (4K+ resolution)?
- What occurs when the user's authentication token expires during active plugin use?
- How does the plugin behave when the selected Vessel Studio project is deleted from the web?
- What happens if multiple Rhino instances try to authenticate simultaneously?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Plugin MUST integrate with Rhino 3D as a native command accessible via command line
- **FR-002**: Plugin MUST capture the current active viewport as a high-quality image (PNG format, minimum 1920x1080 when available)
- **FR-003**: Plugin MUST authenticate users via secure web-based flow using their existing Vessel Studio credentials
- **FR-004**: Plugin MUST display a project selection interface showing the user's available Vessel Studio projects
- **FR-005**: Plugin MUST upload captured viewport images to the selected Vessel Studio project with metadata
- **FR-006**: Plugin MUST preserve viewport metadata including display mode, camera position, timestamp, and Rhino version
- **FR-007**: Plugin MUST provide visual feedback during capture and upload operations (progress indication, success/error messages)
- **FR-008**: Plugin MUST compress viewport images above 5MB file size threshold using JPEG compression at 85% quality while preserving all metadata
- **FR-009**: Plugin MUST remember user authentication state between Rhino sessions
- **FR-009a**: Plugin MUST attempt silent token refresh when authentication expires, prompting for re-authentication only if refresh fails
- **FR-009b**: Plugin MUST support independent authentication sessions for multiple concurrent Rhino instances
- **FR-010**: System MUST validate user permissions before allowing uploads to specific projects
- **FR-010a**: Plugin MUST detect when a selected project has been deleted and prompt user to select a new project before proceeding with uploads
- **FR-011**: System MUST ensure uploaded images are immediately visible in the corresponding web project
- **FR-012**: Plugin MUST handle network connectivity issues gracefully with appropriate error messages
- **FR-012a**: Plugin MUST queue captured viewport images locally when network connectivity is lost and automatically retry upload when connectivity returns
- **FR-013**: Plugin MUST support Windows version of Rhino 3D (macOS support deferred for future release)

### Key Entities

- **Viewport Screenshot**: Digital image captured from Rhino's current viewport, including metadata such as display mode, camera position, timestamp, resolution, and Rhino version
- **Authentication Session**: Secure connection between Rhino plugin and Vessel Studio account, including user identity, access permissions, and session expiration
- **Project Association**: Link between captured screenshots and specific Vessel Studio projects, ensuring proper organization and access control
- **Upload Transaction**: Complete process of transferring viewport image and metadata from Rhino to Vessel Studio, including status tracking and error handling

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete viewport capture and upload in under 15 seconds from command execution to web visibility
- **SC-002**: Plugin authentication completes in under 60 seconds including web browser interaction
- **SC-003**: 95% of viewport captures result in successful uploads without user intervention
- **SC-004**: Plugin maintains authentication state for at least 24 hours without requiring re-login
- **SC-005**: Captured viewport images maintain full resolution and visual quality with minimal compression artifacts (PSNR > 30dB for compressed images)
- **SC-006**: System supports concurrent uploads from multiple Rhino instances without conflicts
- **SC-007**: Users can successfully capture and upload viewports with file sizes up to 10MB
- **SC-008**: Plugin installation completes in under 5 minutes following standard Rhino plugin procedures

## Assumptions *(mandatory)*

- Users have active Vessel Studio accounts with at least one project
- Rhino workstations have stable internet connectivity during plugin use
- Users have appropriate system permissions to install Rhino plugins
- Vessel Studio web application supports real-time image updates via WebSocket or similar technology
- User authentication follows OAuth 2.0 or similar secure token-based standards
- Default viewport capture resolution matches the current Rhino viewport size
- Plugin stores minimal user data locally (authentication tokens only)
- Image uploads use standard HTTP multipart form data protocols

## Dependencies *(mandatory)*

### External Dependencies
- Vessel Studio web application API for authentication and image upload
- Rhino 3D software (version 7.0 or later) with RhinoCommon SDK
- Internet connectivity for authentication and upload operations
- Web browser availability for authentication flow

### Internal Dependencies
- Vessel Studio user authentication system
- Vessel Studio project management system
- Cloud backend storage for image hosting (implementation-agnostic)
- Real-time sync infrastructure for web application updates

## Constraints *(mandatory)*

### Technical Constraints
- Plugin must be compatible with both Rhino 7 and Rhino 8
- Image file sizes limited by Vessel Studio storage quotas
- Authentication tokens must expire for security (maximum 24-hour sessions)
- Plugin installation requires Rhino restart

### Business Constraints
- Feature limited to authenticated Vessel Studio users
- Upload functionality requires active Vessel Studio subscription
- Plugin distribution through Rhino package manager or direct download only

### User Experience Constraints
- Authentication requires temporary web browser access
- Project selection limited to user's existing Vessel Studio projects
- Viewport capture reflects current Rhino display settings only

