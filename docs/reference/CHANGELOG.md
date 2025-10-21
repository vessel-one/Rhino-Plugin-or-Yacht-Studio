# Changelog

All notable changes to the Vessel Studio Rhino Plugin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-21

### Added
- **Viewport Screenshot Capture**: Capture current viewport with one command
- **API Key Authentication**: Secure API key-based authentication with Vessel Studio
- **Project Selection**: Browse and select Vessel Studio projects for uploads
- **Automatic Upload**: Screenshots automatically upload to selected project
- **Metadata Capture**: Includes viewport name, display mode, camera position, and Rhino version
- **Multi-format Support**: PNG and JPEG compression options
- **Settings Management**: Configure API key and view connection status
- **Cross-platform UI**: Built with Eto.Forms for Windows and Mac compatibility

### Commands
- `VesselCapture` - Capture current viewport and upload to Vessel Studio
- `VesselSetApiKey` - Configure your Vessel Studio API key
- `VesselStatus` - View plugin connection status and configuration

### Technical
- Built for Rhino 8 (.NET Framework 4.8)
- RhinoCommon 8.0+ integration
- Asynchronous upload with progress tracking
- Secure credential storage using OS-level encryption

## [Unreleased]

### Planned Features
- Chat UI for AI-assisted yacht design
- Real-time viewport sync across team members
- Batch screenshot capture for multiple viewports
- Screenshot history and management
- Custom metadata tags and annotations
