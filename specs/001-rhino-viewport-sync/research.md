# Research: Rhino Viewport Sync Plugin

**Feature**: 001-rhino-viewport-sync  
**Date**: October 16, 2025  
**Purpose**: Technology decisions and best practices research for Rhino plugin implementation

## Technology Stack Decisions

### Decision: C# with .NET Framework 4.8/.NET 6+
**Rationale**: 
- RhinoCommon SDK is C#-based, making C# the natural choice
- .NET Framework 4.8 provides compatibility with Rhino 7, while .NET 6+ supports Rhino 8
- Mature ecosystem for HTTP clients and UI frameworks

**Alternatives considered**:
- Python: Limited by IronPython constraints in Rhino, lacks native UI capabilities
- C++: Complex development, unnecessary for this use case

### Decision: RhinoCommon SDK
**Rationale**:
- Official Rhino plugin development framework
- Provides viewport capture APIs (`RhinoView.CaptureToBitmap`)
- Cross-platform support for Windows and macOS
- Well-documented command registration system

**Alternatives considered**:
- Rhino.Inside: Overkill for plugin use case, designed for embedding Rhino in other apps
- Direct Rhino SDK: Lower level, unnecessary complexity

### Decision: Eto.Forms for Cross-Platform UI
**Rationale**:
- Recommended by McNeel for cross-platform Rhino plugins
- Native look-and-feel on both Windows (WinForms) and macOS (Cocoa)
- Integrated with RhinoCommon for dialogs and UI components

**Alternatives considered**:
- WinForms: Windows-only, breaks macOS compatibility requirement
- WPF: Windows-only, not supported in Rhino plugin context
- Console-only: Poor UX for project selection dialog

### Decision: System.Net.Http.HttpClient
**Rationale**:
- Built into .NET Framework/Core, no additional dependencies
- Async/await support for non-blocking network operations
- Robust timeout and retry mechanisms
- Multipart form data support for image uploads

**Alternatives considered**:
- RestSharp: Additional dependency, HttpClient sufficient for our needs
- WebClient: Deprecated, limited async support

### Decision: Firebase Authentication Integration
**Rationale**:
- Aligns with existing Vessel Studio authentication system
- Secure token-based authentication with refresh capability
- Web-based OAuth flow works well with browser launch pattern

**Alternatives considered**:
- Custom authentication: Reinventing wheel, security risks
- API key authentication: Less secure, no user identity

## Authentication Flow Pattern

### Decision: Device Authorization Flow (OAuth 2.0)
**Rationale**:
- Secure for desktop applications without embedded browsers
- User authenticates in their default browser with full security context
- Plugin polls for completion, receives secure token

**Implementation Pattern**:
1. Plugin requests device code from Vessel Studio API
2. Plugin opens browser to authentication URL with device code
3. User completes authentication in browser
4. Plugin polls API endpoint until authentication completes
5. Plugin receives and stores authentication token

**Alternatives considered**:
- Embedded browser: Security concerns, complex cross-platform implementation
- Username/password: Poor security, stores credentials locally

## Image Processing Strategy

### Decision: PNG for Lossless Capture, JPEG for Large Image Compression
**Rationale**:
- PNG preserves exact pixel data for technical drawings
- JPEG compression above 5MB threshold maintains quality while reducing upload time
- Metadata preservation maintained in both formats

**Compression Logic**:
- Capture viewport as PNG
- If file size > 5MB, convert to high-quality JPEG (95% quality)
- Preserve all metadata in EXIF/custom fields

**Alternatives considered**:
- Always PNG: Large file sizes impact upload performance
- Always JPEG: Quality loss unacceptable for technical content
- WebP: Limited support in existing Vessel Studio infrastructure

## Offline Queue Implementation

### Decision: Local File System Queue with JSON Metadata
**Rationale**:
- Simple, reliable storage mechanism
- Survives Rhino restarts and system reboots
- Easy to implement retry logic and progress tracking

**Queue Structure**:
```
%APPDATA%/VesselStudio/Queue/
├── queue.json          # Queue metadata and status
├── pending/
│   ├── {guid}.png     # Captured images
│   └── {guid}.json    # Upload metadata
└── completed/          # Successful uploads (cleanup)
```

**Alternatives considered**:
- In-memory queue: Lost on restart
- Database storage: Overkill for simple queue functionality
- Registry storage: Platform-specific, size limitations

## Cross-Platform Considerations

### Decision: Conditional Compilation for Platform Differences
**Rationale**:
- Minimal platform differences expected
- Conditional compilation handles OS-specific file paths and browser launching

**Platform-Specific Areas**:
- File system paths (`Environment.SpecialFolder`)
- Browser launching (`Process.Start` with platform-specific handling)
- UI behavior (handled automatically by Eto.Forms)

### Decision: .rhp Plugin Distribution
**Rationale**:
- Standard Rhino plugin distribution format
- Single file installation via drag-and-drop
- Automatic platform detection and compatibility checking

**Alternatives considered**:
- Package Manager: More complex, unnecessary for single plugin
- Manual installation: Poor user experience

## Testing Strategy

### Decision: Unit Tests + Integration Tests + Manual Plugin Testing
**Rationale**:
- Unit tests for business logic (auth, image processing, API clients)
- Integration tests for API communication
- Manual testing required for Rhino integration (viewport capture, UI)

**Testing Framework**: NUnit for familiarity and tooling support

**Mock Strategy**:
- Mock HTTP responses for API testing
- Mock file system for queue testing
- Rhino SDK mocking limited, focus on service layer testing

## Performance Optimization

### Decision: Async/Await for All Network Operations
**Rationale**:
- Prevents UI freezing during uploads
- Maintains Rhino responsiveness during long operations
- Natural fit with HttpClient async APIs

### Decision: Background Thread for Queue Processing
**Rationale**:
- Automatic retry of failed uploads without user intervention
- Periodic connectivity checking
- Minimal impact on Rhino performance

## Security Considerations

### Decision: Secure Token Storage in OS Credential Store
**Rationale**:
- Leverages OS-level encryption and security
- Automatic cleanup on uninstall
- Follows security best practices for desktop applications

**Implementation**: `CredentialManager` (Windows) / Keychain (macOS) via platform-specific APIs

**Alternatives considered**:
- Plain text storage: Security risk
- Encrypted file storage: Reinventing OS-level security features

## Error Handling Strategy

### Decision: Graceful Degradation with User Feedback
**Rationale**:
- Plugin should never crash Rhino
- Clear error messages help users understand and resolve issues
- Automatic retry for transient failures

**Error Categories**:
- Network errors: Automatic retry with exponential backoff
- Authentication errors: Clear re-login prompts
- File system errors: Graceful fallback and user notification
- API errors: Display server error messages to user