# API Contract: Batch Upload

**Feature**: 003-queued-batch-capture  
**Date**: October 28, 2025  
**Type**: Internal Service Contract

## Overview

This contract defines the internal interface between the batch upload service and the existing Vessel Studio API client. The batch upload functionality reuses the existing authentication and HTTP client infrastructure, adding batch-specific logic on top.

---

## Service Interface: BatchUploadService

### Method: UploadBatchAsync

Uploads a batch of queued captures to Vessel Studio as a single project entry with multiple images.

**Signature**:
```csharp
public async Task<BatchUploadResult> UploadBatchAsync(
    CaptureQueue queue,
    string projectId,
    IProgress<BatchUploadProgress> progress,
    CancellationToken cancellationToken
)
```

**Parameters**:
- `queue: CaptureQueue` - Queue containing items to upload (non-empty, validated)
- `projectId: string` - Target project ID from selected project dropdown (non-null, non-empty)
- `progress: IProgress<BatchUploadProgress>` - Progress callback for UI updates (nullable)
- `cancellationToken: CancellationToken` - Cancellation token for abort (default = none)

**Returns**: `Task<BatchUploadResult>`
- `Success: bool` - Overall batch success status
- `UploadedCount: int` - Number of successfully uploaded items
- `FailedCount: int` - Number of failed items
- `Errors: List<(string filename, string error)>` - Failed uploads with error messages
- `TotalDurationMs: long` - Total upload time in milliseconds

**Exceptions**:
- `ArgumentNullException` - queue or projectId is null
- `ArgumentException` - queue is empty or projectId is invalid
- `InvalidOperationException` - API key not configured
- `HttpRequestException` - Network or API errors (after retries)
- `OperationCanceledException` - User cancelled upload

**Behavior**:
1. Validate API key is configured (throw if not)
2. Validate queue is not empty (throw if empty)
3. Generate filenames for all items using pattern: `{ProjectName}_{ViewportName}_{Sequence:D3}.png`
4. For each item in queue (sequential):
   a. Call existing API client upload method
   b. Report progress via IProgress callback
   c. Continue on individual item failure (collect errors)
   d. Check cancellation token between items
5. Return aggregate result with success/failure counts
6. Clear queue on complete success (per FR-007)
7. Preserve queue on any failure (per FR-008)

---

## Data Structures

### BatchUploadProgress

Progress update sent to UI during batch upload.

```csharp
public class BatchUploadProgress {
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int FailedItems { get; set; }
    public string CurrentFilename { get; set; }
    public int PercentComplete => (CompletedItems + FailedItems) * 100 / TotalItems;
}
```

**Usage**:
```csharp
progress.Report(new BatchUploadProgress {
    TotalItems = 10,
    CompletedItems = 7,
    FailedItems = 1,
    CurrentFilename = "YachtName_Perspective_008.png"
});
```

---

### BatchUploadResult

Result of batch upload operation.

```csharp
public class BatchUploadResult {
    public bool Success { get; set; }
    public int UploadedCount { get; set; }
    public int FailedCount { get; set; }
    public List<(string filename, string error)> Errors { get; set; }
    public long TotalDurationMs { get; set; }
    
    public bool IsPartialSuccess => UploadedCount > 0 && FailedCount > 0;
    public bool IsCompleteFailure => UploadedCount == 0 && FailedCount > 0;
}
```

---

## Integration with Existing API Client

### Vessel Studio API Client Usage

The batch upload service uses the existing `VesselStudioApiClient` class:

```csharp
public class BatchUploadService {
    private readonly VesselStudioApiClient _apiClient;
    
    public BatchUploadService(VesselStudioApiClient apiClient) {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }
    
    public async Task<BatchUploadResult> UploadBatchAsync(...) {
        // Validate API key using existing method
        if (!_apiClient.HasValidApiKey()) {
            throw new InvalidOperationException("API key not configured");
        }
        
        // Upload each item using existing single-upload method
        foreach (var item in queue.Items) {
            try {
                var filename = GenerateFilename(projectId, item.ViewportName, sequence++);
                await _apiClient.UploadImageAsync(projectId, filename, item.ImageData);
                completedCount++;
            }
            catch (Exception ex) {
                errors.Add((filename, ex.Message));
                failedCount++;
            }
        }
        
        return new BatchUploadResult { ... };
    }
}
```

**Assumptions**:
- `VesselStudioApiClient.UploadImageAsync()` accepts projectId, filename, byte[] imageData
- API client handles HTTPS, authentication headers, retry logic
- Individual upload failures throw exceptions (caught and collected by batch service)
- API supports multiple images per project (FR-016 clarification)

---

## Filename Generation Contract

### Method: GenerateFilename

Generates filename using pattern: `{ProjectName}_{ViewportName}_{Sequence:D3}.png`

**Signature**:
```csharp
private string GenerateFilename(string projectName, string viewportName, int sequence)
```

**Parameters**:
- `projectName: string` - Project name from dropdown (sanitized)
- `viewportName: string` - Viewport name from capture (sanitized)
- `sequence: int` - 1-based sequence number (zero-padded to 3 digits)

**Returns**: `string` - Formatted filename

**Example**:
```csharp
GenerateFilename("Yacht Model", "Perspective", 1)
  → "Yacht_Model_Perspective_001.png"

GenerateFilename("Boat #123", "Top View", 42)
  → "Boat_123_Top_View_042.png"
```

**Sanitization Rules**:
- Replace spaces with underscores
- Remove or replace illegal filesystem characters: `\ / : * ? " < > |`
- Preserve alphanumeric, dash, underscore
- Truncate to max 255 characters total

---

## Error Handling

### Retry Strategy

Batch upload does NOT retry individual item failures. Existing API client handles retries internally per upload.

**Rationale**: Spec requires queue preservation on failure (FR-008). User can retry entire batch.

### Error Collection

All individual item failures are collected and returned in `BatchUploadResult.Errors`. UI displays summary of failures to user.

### Partial Success Handling

If some items succeed and others fail:
1. Do NOT clear queue (preserve for retry per FR-008)
2. Return `IsPartialSuccess = true`
3. Display error count and details to user
4. User can review failures and retry batch

---

## Performance Requirements

### Upload Speed

**Target**: Upload 10 items in <3 minutes total (SC-001)
**Assumption**: ~18 seconds per item (including network latency)

### Progress Updates

**Target**: UI updates within 2 seconds of actual progress (SC-007)
**Implementation**: Report progress after EACH item completion (not batched)

### Memory Usage

**Target**: <100 MB for 20-item batch (SC-002)
**Implementation**: Release item memory after successful upload (dispose pattern)

---

## Sequence Diagram

```
User                 Command              Service              API Client         API
│                      │                    │                      │             │
│ Click "Send Batch"   │                    │                      │             │
├─────────────────────>│                    │                      │             │
│                      │ ValidateApiKey     │                      │             │
│                      ├───────────────────>│                      │             │
│                      │<───────────────────┤                      │             │
│                      │ UploadBatchAsync   │                      │             │
│                      ├───────────────────>│                      │             │
│                      │                    │ For each item:       │             │
│                      │                    │   GenerateFilename   │             │
│                      │                    │   UploadImageAsync   │             │
│                      │                    ├─────────────────────>│             │
│                      │                    │                      │ POST /upload│
│                      │                    │                      ├────────────>│
│                      │                    │                      │<────────────┤
│                      │                    │<─────────────────────┤             │
│                      │                    │   ReportProgress     │             │
│                      │<───────────────────┤                      │             │
│ Update Progress UI   │                    │                      │             │
│<─────────────────────┤                    │                      │             │
│                      │                    │ [Repeat for all]     │             │
│                      │ BatchUploadResult  │                      │             │
│                      │<───────────────────┤                      │             │
│                      │ ClearQueue (if OK) │                      │             │
│                      ├───────────────────>│                      │             │
│ Show Success/Errors  │                    │                      │             │
│<─────────────────────┤                    │                      │             │
```

---

## Testing Contract

### Unit Test Scenarios

1. **Empty queue throws ArgumentException**
   ```csharp
   await Assert.ThrowsAsync<ArgumentException>(
       () => service.UploadBatchAsync(emptyQueue, "proj-123", null, CancellationToken.None)
   );
   ```

2. **Null projectId throws ArgumentNullException**
   ```csharp
   await Assert.ThrowsAsync<ArgumentNullException>(
       () => service.UploadBatchAsync(queue, null, null, CancellationToken.None)
   );
   ```

3. **Progress reported after each item**
   ```csharp
   var progressUpdates = new List<BatchUploadProgress>();
   await service.UploadBatchAsync(queue, "proj-123", 
       new Progress<BatchUploadProgress>(p => progressUpdates.Add(p)), 
       CancellationToken.None);
   Assert.Equal(queue.Count, progressUpdates.Count);
   ```

4. **Queue cleared on complete success**
   ```csharp
   var result = await service.UploadBatchAsync(queue, "proj-123", null, CancellationToken.None);
   Assert.True(result.Success);
   Assert.Equal(0, queue.Count);
   ```

5. **Queue preserved on failure**
   ```csharp
   // Mock API client to throw exception
   var result = await service.UploadBatchAsync(queue, "proj-123", null, CancellationToken.None);
   Assert.False(result.Success);
   Assert.Equal(originalCount, queue.Count);
   ```

6. **Cancellation token respected**
   ```csharp
   var cts = new CancellationTokenSource();
   cts.CancelAfter(100); // Cancel quickly
   await Assert.ThrowsAsync<OperationCanceledException>(
       () => service.UploadBatchAsync(largeQueue, "proj-123", null, cts.Token)
   );
   ```

---

## Notes

- This is an internal service contract, not an external API
- Vessel Studio API contract assumed stable (no changes needed)
- Batch functionality implemented entirely in plugin layer
- API sees N individual uploads, not aware of "batch" concept
- Future: Could optimize with true batch API endpoint if Vessel Studio adds support