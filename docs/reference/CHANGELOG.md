# Changelog

All notable changes to the Vessel Studio Rhino Plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
