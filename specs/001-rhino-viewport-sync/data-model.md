# Data Model: Rhino Viewport Sync Plugin

**Feature**: 001-rhino-viewport-sync  
**Date**: October 16, 2025  
**Purpose**: Core entities and their relationships for plugin functionality

## Core Entities

### ViewportScreenshot
**Purpose**: Represents a captured viewport image with associated metadata

**Fields**:
- `Id`: Unique identifier (Guid)
- `ImageData`: Binary image data (byte[])
- `Format`: Image format (PNG, JPEG)
- `FilePath`: Local storage path (string, optional)
- `FileSize`: Image size in bytes (long)
- `CaptureTimestamp`: When viewport was captured (DateTime)
- `ViewportMetadata`: Viewport-specific data (ViewportMetadata)
- `UploadStatus`: Current upload state (UploadStatus enum)
- `ProjectId`: Target Vessel Studio project (string)
- `RemoteUrl`: URL after successful upload (string, optional)

**Validation Rules**:
- Id must be unique and not empty
- ImageData or FilePath must be present (not both null)
- FileSize must be > 0 and <= 10MB
- CaptureTimestamp cannot be future date
- ProjectId required for upload operations

**State Transitions**:
```
Captured → Queued → Uploading → Completed
    ↓         ↓         ↓         ↓
   Failed    Failed    Failed   [terminal]
```

### ViewportMetadata
**Purpose**: Captures Rhino viewport state and camera information

**Fields**:
- `ViewportName`: Rhino viewport identifier (string)
- `DisplayMode`: Rendering mode (string) - "Wireframe", "Shaded", "Rendered", etc.
- `CameraPosition`: 3D camera location (Point3d)
- `CameraTarget`: 3D camera look-at point (Point3d)
- `CameraUp`: Camera up vector (Vector3d)
- `ViewportSize`: Pixel dimensions (Size)
- `RhinoVersion`: Rhino build information (string)
- `DisplaySettings`: Additional render settings (Dictionary<string, object>)

**Validation Rules**:
- ViewportName cannot be empty
- ViewportSize dimensions must be > 0
- CameraPosition and CameraTarget cannot be identical

### AuthenticationSession
**Purpose**: Manages user authentication state and token lifecycle

**Fields**:
- `UserId`: Vessel Studio user identifier (string)
- `AccessToken`: JWT or OAuth token (string)
- `RefreshToken`: Token refresh credential (string, optional)
- `TokenExpiry`: When token expires (DateTime)
- `SessionStarted`: Initial authentication time (DateTime)
- `LastRefreshed`: Most recent token refresh (DateTime, optional)
- `IsActive`: Current session validity (bool)

**Validation Rules**:
- UserId cannot be empty
- AccessToken required when IsActive = true
- TokenExpiry must be future date for active sessions
- SessionStarted cannot be future date

**State Transitions**:
```
Pending → Authenticated → Expired → Refreshed
    ↓           ↓           ↓         ↓
  Failed      Logout     Failed   Authenticated
```

### ProjectInfo
**Purpose**: Vessel Studio project metadata for selection interface

**Fields**:
- `Id`: Vessel Studio project identifier (string)
- `Name`: User-friendly project name (string)
- `Description`: Project description (string, optional)
- `LastModified`: Most recent project update (DateTime)
- `ImageCount`: Number of images in project (int)
- `IsAccessible`: User has upload permissions (bool)
- `ThumbnailUrl`: Project preview image (string, optional)

**Validation Rules**:
- Id cannot be empty
- Name cannot be empty
- ImageCount >= 0
- LastModified cannot be future date

### UploadTransaction
**Purpose**: Tracks image upload process and provides status feedback

**Fields**:
- `Id`: Transaction identifier (Guid)
- `ScreenshotId`: Associated screenshot (Guid)
- `ProjectId`: Target project (string)
- `Status`: Current upload state (UploadStatus enum)
- `Progress`: Upload completion percentage (0-100)
- `StartTime`: Upload initiation (DateTime)
- `CompletionTime`: Upload finish time (DateTime, optional)
- `ErrorMessage`: Failure description (string, optional)
- `RetryCount`: Number of retry attempts (int)
- `RemoteImageId`: Server-assigned image ID (string, optional)

**Validation Rules**:
- ScreenshotId must reference valid ViewportScreenshot
- Progress must be 0-100
- StartTime cannot be future date
- RetryCount >= 0 and <= 3
- RemoteImageId required when Status = Completed

**State Transitions**:
```
Pending → InProgress → Completed
   ↓          ↓           ↓
Retrying → Failed    [terminal]
   ↓        ↓
Pending  [terminal]
```

### UploadQueue
**Purpose**: Manages offline screenshot queue and retry logic

**Fields**:
- `QueuedItems`: List of pending uploads (List<UploadTransaction>)
- `IsProcessing`: Queue processor active state (bool)
- `LastProcessed`: Most recent queue processing (DateTime)
- `ProcessingInterval`: Queue check frequency (TimeSpan)
- `MaxRetries`: Maximum retry attempts per item (int)
- `RetryDelay`: Base delay between retries (TimeSpan)

**Validation Rules**:
- MaxRetries must be 1-5
- ProcessingInterval must be 30 seconds to 10 minutes
- RetryDelay must be 1 second to 5 minutes

## Enumerations

### UploadStatus
**Values**:
- `Captured`: Image captured, not yet queued
- `Queued`: Waiting for upload
- `InProgress`: Currently uploading
- `Completed`: Successfully uploaded
- `Failed`: Upload failed (terminal)
- `Retrying`: Scheduled for retry

### AuthenticationState
**Values**:
- `NotAuthenticated`: No valid session
- `Pending`: Authentication in progress
- `Authenticated`: Valid session active
- `Expired`: Session expired, refresh needed
- `Failed`: Authentication failed

## Relationships

### Primary Relationships
- `ViewportScreenshot` → `ViewportMetadata` (1:1, composition)
- `ViewportScreenshot` → `UploadTransaction` (1:0..1, association)
- `UploadTransaction` → `ProjectInfo` (N:1, reference)
- `AuthenticationSession` → `ProjectInfo` (1:N, access control)

### Storage Relationships
- `UploadQueue` → `UploadTransaction` (1:N, composition)
- `ViewportScreenshot` files stored in local queue directory
- `AuthenticationSession` tokens stored in OS credential store

## Data Flow

### Viewport Capture Flow
```
1. User triggers capture command
2. Create ViewportScreenshot with current viewport state
3. Generate ViewportMetadata from Rhino APIs
4. Save image data to local storage
5. Create UploadTransaction if authenticated
6. Add to UploadQueue for processing
```

### Authentication Flow
```
1. Create AuthenticationSession (Pending state)
2. Launch browser with auth URL
3. Poll API for completion
4. Update session with tokens (Authenticated state)
5. Store credentials securely
6. Load user's ProjectInfo list
```

### Upload Processing Flow
```
1. UploadQueue processes pending transactions
2. Check network connectivity
3. Validate authentication (refresh if needed)
4. Upload image data and metadata
5. Update transaction status
6. Handle retry logic on failure
7. Clean up completed uploads
```

## Persistence Strategy

### Local Storage
- **Screenshots**: File system with JSON metadata
- **Queue State**: JSON file with transaction status
- **Settings**: Rhino plugin settings store
- **Credentials**: OS credential manager

### Remote Storage
- **Images**: Vessel Studio cloud storage (Firebase/S3)
- **Metadata**: Vessel Studio project database
- **User Data**: Vessel Studio authentication system

### Data Retention
- **Local Images**: Cleaned up after successful upload
- **Failed Uploads**: Retained for 7 days for retry
- **Authentication**: Expires per token lifetime (24 hours)
- **Queue Metadata**: Persistent until manual cleanup