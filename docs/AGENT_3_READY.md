# Agent 3 Next Assignment: CaptureQueue Model (Group 3)

**Date**: October 28, 2025  
**Blocked By**: Group 1 (QueuedCaptureItem) ✅ COMPLETE  
**Status**: 🟢 READY TO BEGIN

---

## Quick Reference

**File to Create**:
- `VesselStudioSimplePlugin/Models/CaptureQueue.cs`

**Tasks to Implement**:
- **T009**: Create CaptureQueue model with Items list, CreatedAt, ProjectName fields
- **T010**: Add computed properties (Count, TotalSizeBytes, IsEmpty) and validation methods

**Dependencies Satisfied**:
- ✅ QueuedCaptureItem model exists at: `VesselStudioSimplePlugin/Models/QueuedCaptureItem.cs`
- ✅ Can import: `using VesselStudioSimplePlugin.Models;`
- ✅ Build passes with zero errors

---

## Key Model Interfaces

### QueuedCaptureItem (Now Available)

```csharp
public class QueuedCaptureItem : IDisposable
{
    public Guid Id { get; }
    public byte[] ImageData { get; }
    public string ViewportName { get; }
    public DateTime Timestamp { get; }
    public int SequenceNumber { get; set; }
    
    public QueuedCaptureItem(byte[] imageData, string viewportName)
    public Bitmap GetThumbnail()
    public void Dispose()
}
```

**Constructor Behavior**:
- Throws `ArgumentException` if imageData is null/empty
- Throws `ArgumentException` if imageData exceeds 5MB
- Throws `ArgumentException` if viewportName is null/whitespace
- Auto-assigns `Id` and `Timestamp`
- Sets `SequenceNumber = 0` initially

---

## Next Phase: Group 4

After Group 3 (CaptureQueue) completes:
- **T011-T015**: CaptureQueueService implementation
- Handles: Add, Remove, Clear operations, thread safety, event notifications
- Depends on: CaptureQueue model ✓ (in progress)

---

## Coordination Status

**Phase 2 Foundational**:
- ✅ Group 1: Complete (Agent 1)
- ✅ Group 2: Complete (Agent 2)
- 🔄 Group 3: Ready (T009-T010) ← Next assignment
- ⏳ Group 4: Blocked (depends on Group 3)

**Ready to Assign**: Group 3 (CaptureQueue) - Agent 3 recommended

---

## Build Information

**Last Successful Build**:
```
Time Elapsed 00:00:02.13
✓ Build completed successfully!
0 Warning(s)
0 Error(s)
```

**Project**: VesselStudioSimplePlugin.csproj  
**Framework**: .NET Framework 4.8  
**Output**: net48\VesselStudioSimplePlugin.rhp (76 KB)

---

## Resources

**See agent-3-coremodels.md for complete T009-T010 specifications**

- Full requirements with validation rules
- Code template to start from
- Success criteria checklist
- Coordination update instructions
