# Agent 2 Prompt: Batch Upload Models (Phase 2 - Group 2)

## Your Role
You are **Agent 2**, responsible for implementing batch upload progress and result models.

## Context Files to Read FIRST
1. `specs/003-queued-batch-capture/contracts/api-batch-upload.md` - Service contract and data structures
2. `specs/003-queued-batch-capture/data-model.md` - BatchUploadRequest entity definition
3. `specs/003-queued-batch-capture/tasks.md` - Task T016-T017 details

## Your Assigned Tasks (Execute in Parallel)

### T016 [P] Create BatchUploadProgress model
**File**: `VesselStudioSimplePlugin/Models/BatchUploadProgress.cs`

**Requirements** (from contracts/api-batch-upload.md):
- Class with properties: `TotalItems` (int), `CompletedItems` (int), `FailedItems` (int), `CurrentFilename` (string)
- Add computed property: `PercentComplete` → `(CompletedItems + FailedItems) * 100 / TotalItems`
- Used for `IProgress<BatchUploadProgress>` reporting during batch upload
- Simple data transfer object - no validation needed

### T017 [P] Create BatchUploadResult model
**File**: `VesselStudioSimplePlugin/Models/BatchUploadResult.cs`

**Requirements** (from contracts/api-batch-upload.md):
- Class with properties:
  - `Success` (bool)
  - `UploadedCount` (int)
  - `FailedCount` (int)
  - `Errors` (List<(string filename, string error)>) - tuple list
  - `TotalDurationMs` (long)
- Add computed properties:
  - `IsPartialSuccess` → `UploadedCount > 0 && FailedCount > 0`
  - `IsCompleteFailure` → `UploadedCount == 0 && FailedCount > 0`
- Simple data transfer object - no validation needed

## Code Templates

### BatchUploadProgress.cs
```csharp
namespace VesselStudioSimplePlugin.Models
{
    /// <summary>
    /// Progress update sent to UI during batch upload operation.
    /// </summary>
    public class BatchUploadProgress
    {
        public int TotalItems { get; set; }
        public int CompletedItems { get; set; }
        public int FailedItems { get; set; }
        public string CurrentFilename { get; set; }
        
        public int PercentComplete => TotalItems > 0 
            ? (CompletedItems + FailedItems) * 100 / TotalItems 
            : 0;
    }
}
```

### BatchUploadResult.cs
```csharp
using System.Collections.Generic;

namespace VesselStudioSimplePlugin.Models
{
    /// <summary>
    /// Result of a batch upload operation including success/failure counts and error details.
    /// </summary>
    public class BatchUploadResult
    {
        public bool Success { get; set; }
        public int UploadedCount { get; set; }
        public int FailedCount { get; set; }
        public List<(string filename, string error)> Errors { get; set; }
        public long TotalDurationMs { get; set; }
        
        public bool IsPartialSuccess => UploadedCount > 0 && FailedCount > 0;
        public bool IsCompleteFailure => UploadedCount == 0 && FailedCount > 0;

        public BatchUploadResult()
        {
            Errors = new List<(string filename, string error)>();
        }
    }
}
```

## Success Criteria
- [ ] BatchUploadProgress.cs created with 4 properties + PercentComplete computed property
- [ ] BatchUploadResult.cs created with 5 properties + 2 computed properties
- [ ] PercentComplete handles divide-by-zero (returns 0 when TotalItems = 0)
- [ ] Errors list initialized in BatchUploadResult constructor
- [ ] No compiler errors
- [ ] Matches contracts/api-batch-upload.md exactly

## Coordination
After completing T016-T017:
1. Update `agent-coordination.json`:
   - Set `phases.phase_2_foundational.parallel_groups.group_2_batch_models.agent = "agent_2"`
   - Add T016-T017 to `phases.phase_2_foundational.completed_tasks`
   - Add T016-T017 to `agents.agent_2.completed_tasks`
2. Notify orchestrator: "Agent 2 completed Group 2 (BatchUpload models)"
3. Wait for next assignment (likely BatchUploadService in Phase 5 User Story 3)

## Next Steps After This
You may be assigned:
- **Phase 5 (US3) Group 1**: BatchUploadService implementation (T043-T052)
- This is the upload logic that uses the models you just created
