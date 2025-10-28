# Changelog

All notable changes to the Vessel Studio Rhino Plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.4.0] - 2025-10-28

### Added
- **Batch Capture Queue**: Queue multiple captures for batch processing and export
- **Image Format Settings**: Dialog to configure PNG/JPEG format and JPEG quality (1-100)
- **Queue Manager Dialog**: View, manage, and export all queued captures with metadata
- **Settings Access Points**: Image format button in toolbar and queue manager dialog

### Fixed
- **VesselStudioStatus Freeze**: Removed async deadlock that blocked UI thread
- **Image Settings Dialog**: Fixed class name conflict between API key dialog and format settings
- **Image Settings Dialog UI**: Improved padding, sizing (400x280 → 480x380), and control spacing
- **Dialog Layout**: Added proper margins and visual hierarchy to Image Format Settings dialog

### Changed
- **Dialog Organization**: Separated API key settings from image format settings for clarity
- **UI Polish**: Improved button sizes, control spacing, and overall dialog layout

## [1.3.0] - 2025-10-23

### Added
- **Project Selection Toolbar**: Dockable panel with project dropdown and capture button
- **Project Persistence**: Plugin remembers last selected project across sessions
- **Manual Filename Entry**: Users must enter custom name for each capture (no auto-generated names)
- **DEV/RELEASE Build Modes**: Separate DEV build configuration for testing without conflicts
- **Project Refresh**: Manual refresh button to reload projects from API

### Changed
- **Simplified Capture Workflow**: Single capture button (removed Quick Capture from toolbar)
- **Empty Filename Requirement**: Capture button disabled until user enters a name
- **Project Selection**: Projects loaded from API and displayed in dropdown instead of dialog
- **Command Structure**: All commands support DEV prefix for dual-mode installation

### Fixed
- **Project Dropdown Population**: Fixed ComboBox binding to properly display project names
- **Settings Isolation**: DEV and RELEASE modes use separate settings folders

## [1.2.0] - 2025-10-23

### Fixed
- **Capture Freeze**: Screenshot capture and upload now happens in background - Rhino no longer freezes
- **Quick Capture Freeze**: Quick capture uploads asynchronously - immediate return to work
- **Server Busy Dialog**: Eliminated blocking dialogs by removing synchronous upload waits
- **User Experience**: Added feedback messages ("Upload happens in background") for clarity

### Changed
- **Async Uploads**: Both VesselCapture and VesselQuickCapture now upload in background threads
- **Instant Response**: Commands return immediately after screenshot, upload continues independently

## [1.1.1] - 2025-10-21

### Fixed
- **About Dialog**: Adjusted subtitle position to prevent text overlap with main title
- **Icon Loading**: Fixed COM error in VesselStudioDebugIcons command by properly disposing icon handles

## [1.1.0] - 2025-10-21

### Added
- **About Dialog**: GUI showing version info, features, and links
- **Enhanced Capture UX**: Loading feedback visible in capture dialog
- **Async Project Loading**: Projects load in background with status feedback

### Fixed
- **FormData Upload Error**: Changed field name from "image" to "file" for proper multipart upload
- **Capture Dialog Layout**: Increased dialog size (420x230) for better visibility
- **API Endpoint Paths**: Fixed all endpoint paths with correct /api prefix
- **Error Handling**: Removed blocking MessageBox popups, using command bar messages

### Changed
- **Settings Dialog**: GUI with console-style output for API validation
- **Project Loading**: Now loads asynchronously in capture dialog
- **Metadata Upload**: Individual form fields instead of JSON string
- **Build Output**: Post-build cleanup removes RhinoCommon and Eto DLLs

### Commands
- `VesselStudioAbout` - Show about dialog with version and credits

## [1.0.0] - 2025-10-21

### Added
- **Viewport Screenshot Capture**: Capture current viewport with one command
- **API Key Authentication**: Secure API key-based authentication with Vessel Studio
- **Project Selection**: Browse and select Vessel Studio projects for uploads
- **Automatic Upload**: Screenshots automatically upload to selected project
- **Metadata Capture**: Includes viewport name, display mode, camera position, and Rhino version
- **Settings Management**: Configure API key and view connection status
- **Toolbar Panel**: Dockable panel with quick-access buttons
- **Quick Capture**: One-click capture to last used project

### Commands
- `VesselCapture` - Capture current viewport and upload to Vessel Studio
- `VesselQuickCapture` - Quick capture to last used project
- `VesselSetApiKey` - Configure your Vessel Studio API key
- `VesselStudioStatus` - View plugin connection status
- `VesselStudioHelp` - Open documentation

### Technical
- Built for Rhino 8 (.NET Framework 4.8)
- Windows Forms UI for maximum compatibility
- RhinoCommon 8.0+ integration
- Asynchronous upload with error handling
- Environment variable-based credential storage

## [Planned Features]

- Chat UI for AI-assisted yacht design
- Real-time viewport sync across team members
- Batch screenshot capture for multiple viewports
- Screenshot history and management
- Custom metadata tags and annotations
