<!--
Sync Impact Report:
- Version change: template → 1.0.0
- Modified principles: New constitution created from template
- Added sections: All core principles and security requirements
- Removed sections: None (first version)
- Templates requiring updates: ✅ No updates needed - constitution aligns with existing templates
- Follow-up TODOs: None - all placeholders filled
-->

# Yacht Studio Rhino Plugin Constitution

## Core Principles

### I. Security-First Authentication
Authentication and credential management MUST prioritize security over convenience. All user credentials MUST be stored using OS-level secure storage APIs. Token refresh MUST be automatic and transparent. Multiple concurrent Rhino instances MUST maintain independent authentication sessions without conflicts. No credentials may be stored in plain text or logged.

### II. User Experience Excellence
Plugin integration MUST feel native to Rhino workflow. Commands MUST be accessible via Rhino command line. Visual feedback MUST be provided for all operations (progress, success, errors). Viewport capture MUST complete in under 15 seconds from command to web visibility. Users MUST NOT be required to switch applications or manually manage files.

### III. Cross-Platform Foundation
All UI components MUST use Eto.Forms for cross-platform compatibility. Windows support is mandatory for MVP; macOS support MUST be architecturally planned but may be deferred. Platform-specific code MUST be isolated and abstracted behind interfaces. Plugin MUST work with both Rhino 7 and Rhino 8.

### IV. Resilient Operations
Network connectivity loss MUST NOT cause data loss - implement local queuing with automatic retry. Large viewport images (>5MB) MUST be compressed using JPEG at 85% quality while preserving metadata. Authentication token expiration MUST trigger silent refresh with user prompt only on refresh failure. Plugin MUST gracefully handle project deletion and prompt for reselection.

### V. Measurable Performance
All performance requirements MUST include measurable criteria. Compression quality MUST maintain PSNR > 30dB. Authentication MUST complete in under 60 seconds including web interaction. 95% of viewport captures MUST succeed without user intervention. Authentication sessions MUST persist for at least 24 hours.

## Security Requirements

Plugin MUST implement OAuth 2.0 or equivalent secure authentication flow. User data transmission MUST use HTTPS exclusively. Authentication tokens MUST have expiration and refresh mechanisms. Local storage MUST use OS-provided secure credential APIs (Windows Credential Manager, macOS Keychain). Plugin MUST validate user permissions before allowing uploads to projects.

## Development Quality Gates

Each feature MUST have independent test scenarios that deliver standalone value. User stories MUST include acceptance criteria with Given-When-Then format. All API integrations MUST be abstracted behind interfaces for testability. Plugin architecture MUST separate Commands, Services, UI, and Models into distinct layers. Code MUST handle edge cases explicitly defined in specifications.

## Governance

This constitution supersedes all other development practices. All features and implementations MUST demonstrate compliance with these principles during code review. Principle violations MUST be explicitly justified and documented in complexity tracking sections. Constitution amendments require semantic versioning: MAJOR for principle changes, MINOR for additions, PATCH for clarifications.

**Version**: 1.0.0 | **Ratified**: 2025-10-16 | **Last Amended**: 2025-10-16
