# Agent 1 Prompt: Foundational Models (Phase 2 - Group 1)

## Your Role
You are **Agent 1**, responsible for implementing core data models for the queued batch capture feature.

## Context Files to Read FIRST
1. `specs/003-queued-batch-capture/spec.md` - Feature requirements
2. `specs/003-queued-batch-capture/data-model.md` - Entity definitions with validation rules
3. `specs/003-queued-batch-capture/research.md` - Technical decisions (especially Q1 thumbnail generation, Q6 memory management)
4. `specs/003-queued-batch-capture/tasks.md` - Task T005-T008 details

## Your Assigned Tasks (Execute in Order)

### T005 [P] Create QueuedCaptureItem model
**File**: `VesselStudioSimplePlugin/Models/QueuedCaptureItem.cs`

**Requirements**:
- Create class with fields: `Id` (Guid), `ImageData` (byte[]), `ViewportName` (string), `Timestamp` (DateTime), `SequenceNumber` (int), `_thumbnailCache` (Bitmap, private)
- See data-model.md for exact field specifications
- Constructor accepts `byte[] imageData, string viewportName`

### T006 [P] Implement IDisposable pattern
**File**: Same as T005

**Requirements**:
- Implement IDisposable interface
- Dispose() method releases `_thumbnailCache?.Dispose()`
- Set `_thumbnailCache = null` after disposal
- Follow pattern in research.md Q6

### T007 [P] Add validation in constructor
**File**: Same as T005

**Requirements**:
- Validate `imageData` not null or empty → throw ArgumentException
- Validate `viewportName` not null/whitespace → throw ArgumentException
- Validate `imageData.Length <= 5 * 1024 * 1024` (5MB limit) → throw ArgumentException
- Auto-assign `Id = Guid.NewGuid()` and `Timestamp = DateTime.Now`
- Set `SequenceNumber = 0` (assigned by queue service later)

### T008 [P] Implement GetThumbnail() method
**File**: Same as T005

**Requirements**:
- Method signature: `public Bitmap GetThumbnail()`
- If `_thumbnailCache == null`, generate 80x60 thumbnail from `ImageData`
- Use `MemoryStream` to load `ImageData` → `Image.FromStream()`
- Scale to 80x60 using `new Bitmap(fullImage, 80, 60)` with HighQualityBicubic interpolation
- Cache result in `_thumbnailCache`
- Return cached thumbnail
- See research.md Q1 for implementation pattern

## Code Template to Start

```csharp
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace VesselStudioSimplePlugin.Models
{
    /// <summary>
    /// Represents a single viewport capture in the queue awaiting batch upload.
    /// </summary>
    public class QueuedCaptureItem : IDisposable
    {
        public Guid Id { get; }
        public byte[] ImageData { get; }
        public string ViewportName { get; }
        public DateTime Timestamp { get; }
        public int SequenceNumber { get; set; }
        
        private Bitmap _thumbnailCache;

        public QueuedCaptureItem(byte[] imageData, string viewportName)
        {
            // T007: Add validation here
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
            
            // TODO: Add remaining validation from T007
            
            Id = Guid.NewGuid();
            ImageData = imageData;
            ViewportName = viewportName;
            Timestamp = DateTime.Now;
            SequenceNumber = 0;
        }

        // T008: Implement GetThumbnail()
        public Bitmap GetThumbnail()
        {
            // TODO: Implement thumbnail generation with caching
            throw new NotImplementedException();
        }

        // T006: Implement IDisposable
        public void Dispose()
        {
            // TODO: Dispose thumbnail cache
            throw new NotImplementedException();
        }
    }
}
```

## Success Criteria
- [ ] File created at correct path
- [ ] All 5 fields defined with correct types
- [ ] Constructor validates all inputs (T007)
- [ ] Constructor auto-assigns Id, Timestamp, SequenceNumber
- [ ] GetThumbnail() generates and caches 80x60 thumbnail
- [ ] Dispose() releases thumbnail memory
- [ ] No compiler errors
- [ ] Follows data-model.md specifications exactly

## Coordination
After completing T005-T008:
1. Update `agent-coordination.json`: 
   - Set `phases.phase_2_foundational.parallel_groups.group_1_models.agent = "agent_1"`
   - Add T005-T008 to `phases.phase_2_foundational.completed_tasks`
   - Add T005-T008 to `agents.agent_1.completed_tasks`
2. Notify orchestrator: "Agent 1 completed Group 1 (QueuedCaptureItem model)"
3. Wait for next assignment (likely Group 3: CaptureQueue model after dependencies complete)

## Next Steps After This
You may be assigned:
- **T009-T010**: CaptureQueue model (depends on QueuedCaptureItem completion)
- Or reassigned to a User Story phase once foundational is complete
