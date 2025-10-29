# Orchestrator Instructions: Multi-Agent Coordination

## Your Role
You are the **Orchestrator** managing 4 agents working in parallel to implement the Queued Batch Capture feature (specs/003-queued-batch-capture).

## Coordination File
**Primary artifact**: `specs/003-queued-batch-capture/agent-coordination.json`

This JSON file tracks:
- Phase status (ready/in_progress/blocked/complete)
- Agent assignments and status (idle/working/complete)
- Completed tasks per phase and per agent
- Parallel groups within phases
- Dependency chains
- Execution log

## The 4 Agents

### Agent 1: Foundational Models
- **Prompt**: `prompts/agent-1-foundational-models.md`
- **Phase 2 Assignments**:
  - Group 1: QueuedCaptureItem model (T005-T008) - **START IMMEDIATELY**
  - Group 3: CaptureQueue model (T009-T010) - **WAIT for Group 1**
- **Skills**: C# model classes, IDisposable pattern, validation logic, thumbnail generation

### Agent 2: Batch Upload Models + Services
- **Prompt**: `prompts/agent-2-batch-models.md`
- **Phase 2 Assignments**:
  - Group 2: BatchUploadProgress and BatchUploadResult (T016-T017) - **START IMMEDIATELY** (parallel with Agent 1 Group 1)
  - Group 4: CaptureQueueService singleton (T011-T015) - **WAIT for Agent 1 Group 3**
- **Phase 5 Assignment**: BatchUploadService (T043-T052) - after Phase 2 complete
- **Skills**: Service layer, singleton pattern, event handling, async/await

### Agent 3: Queue Commands
- **Prompt**: `prompts/agent-3-queue-commands.md`
- **Phase 3 Assignment**: VesselAddToQueueCommand + menu/toolbar registration (T018-T024) - **WAIT for Phase 2 complete**
- **Phase 5 Assignment**: Batch upload command integration (T053-T056)
- **Skills**: RhinoCommon commands, menu/toolbar registration, event subscription

### Agent 4: UI Components
- **Prompt**: `prompts/agent-4-ui-components.md`
- **Phase 3 Assignment**: Toolbar badge + Quick Export button (T025-T028) - **WAIT for Phase 2 complete**
- **Phase 4 Assignment**: QueueManagerDialog (T029-T042) - after Phase 3
- **Phase 5 Assignment**: Progress dialog (T057-T061) - during Phase 5
- **Skills**: Windows Forms, ListView, dialog design, event handling

## Phase Dependency Map

```
Phase 1 (Setup) → Phase 2 (Foundational) → [Phase 3 || Phase 4 || Phase 5] → Phase 6-8
                           ↓
                    CRITICAL GATE
                    Must complete before
                    any User Story work
```

### Phase 2 Internal Dependencies (Sequential Parallel Groups)
```
Group 1: QueuedCaptureItem (T005-T008) [Agent 1] ← START IMMEDIATELY
         ║
         ╠═══════════════════════════════════════╗
         ↓                                       ↓
Group 3: CaptureQueue (T009-T010) [Agent 1]     Group 2: Batch models (T016-T017) [Agent 2] ← START IMMEDIATELY (parallel)
         ↓
Group 4: CaptureQueueService (T011-T015) [Agent 2]
```

### Phase 3-5 Parallelization (After Phase 2 Gate)
```
Phase 3 (US1): Agent 3 (commands) || Agent 4 (toolbar UI)
Phase 4 (US2): Agent 4 (dialog) || Agent 3 (command integration)
Phase 5 (US3): Agent 2 (BatchUploadService) || Agent 3 (command wiring) || Agent 4 (progress dialog)
```

## Execution Protocol

### Step 1: Phase 2 Kickoff (NOW)
1. **Assign Agent 1**: "Start Group 1 (T005-T008 QueuedCaptureItem model)"
   - Update JSON: `phases.phase_2_foundational.parallel_groups.group_1_queueitem.agent = "agent_1"`
   - Update JSON: `agents.agent_1.status = "working"`
   - Update JSON: `agents.agent_1.current_task = "T005-T008"`
   - Log: `execution_log.append({ timestamp, agent: "agent_1", action: "started", tasks: ["T005-T008"] })`

2. **Assign Agent 2**: "Start Group 2 (T016-T017 Batch models) - PARALLEL with Agent 1"
   - Update JSON: `phases.phase_2_foundational.parallel_groups.group_2_batch_models.agent = "agent_2"`
   - Update JSON: `agents.agent_2.status = "working"`
   - Update JSON: `agents.agent_2.current_task = "T016-T017"`
   - Log: `execution_log.append({ timestamp, agent: "agent_2", action: "started", tasks: ["T016-T017"] })`

3. **Block Agent 3 & 4**: "Wait for Phase 2 completion"
   - Set `agents.agent_3.status = "idle"`
   - Set `agents.agent_4.status = "idle"`
   - Add to `phases.phase_3_us1.status = "blocked"` (dependency: phase_2_foundational)

### Step 2: Monitor Agent 1 Completion
When Agent 1 reports "T005-T008 complete":
1. Verify completion:
   - Check files exist: `VesselStudioSimplePlugin/Models/QueuedCaptureItem.cs`
   - Verify JSON updated: `phases.phase_2_foundational.completed_tasks` includes T005-T008
2. **Assign Agent 1 next work**: "Start Group 3 (T009-T010 CaptureQueue model)"
   - Update JSON: `phases.phase_2_foundational.parallel_groups.group_3_capturequeue.agent = "agent_1"`
   - Update JSON: `agents.agent_1.current_task = "T009-T010"`
   - Log: `execution_log.append({ timestamp, agent: "agent_1", action: "started", tasks: ["T009-T010"] })`

### Step 3: Monitor Agent 1 Group 3 Completion
When Agent 1 reports "T009-T010 complete":
1. Verify CaptureQueue.cs exists
2. **Assign Agent 2 next work**: "Start Group 4 (T011-T015 CaptureQueueService)"
   - Update JSON: `phases.phase_2_foundational.parallel_groups.group_4_queueservice.agent = "agent_2"`
   - Update JSON: `agents.agent_2.current_task = "T011-T015"`
   - Wait for Agent 2 to also finish Group 2 (T016-T017) if not already done

### Step 4: Phase 2 Gate - Unblock User Stories
When BOTH:
- Agent 1 completes T005-T010 (Groups 1 & 3)
- Agent 2 completes T011-T017 (Groups 2 & 4)

Then:
1. Update JSON: `phases.phase_2_foundational.status = "complete"`
2. **Open Phase 3 Gate**:
   - Update JSON: `phases.phase_3_us1.status = "ready"`
   - Update JSON: `phases.phase_4_us2.status = "blocked"` (depends on phase_3_us1)
   - Update JSON: `phases.phase_5_us3.status = "blocked"` (depends on phase_3_us1 OR phase_4_us2)
3. **Assign Agent 3**: "Start Phase 3 Group 1 (T018-T024 Queue commands)"
4. **Assign Agent 4**: "Start Phase 3 Group 2 (T025-T028 Toolbar UI)" - PARALLEL with Agent 3
5. Log: `execution_log.append({ timestamp, action: "phase_gate_opened", phase: "phase_3_us1" })`

### Step 5: Phase 3 → Phase 4 Transition
When Phase 3 complete (Agent 3 & 4 both finish):
1. Update JSON: `phases.phase_3_us1.status = "complete"`
2. **Open Phase 4 Gate**:
   - Update JSON: `phases.phase_4_us2.status = "ready"`
   - **Assign Agent 4**: "Start Phase 4 (T029-T042 QueueManagerDialog)"
3. **Assign Agent 1**: "Start Phase 6 (T062-T073 Error Handling)" - can run in parallel

### Step 6: Phase 4 → Phase 5 Transition
When Phase 4 complete:
1. Update JSON: `phases.phase_4_us2.status = "complete"`
2. **Open Phase 5 Gate**:
   - Update JSON: `phases.phase_5_us3.status = "ready"`
   - **Assign Agent 2**: "Start Phase 5 Group 1 (T043-T052 BatchUploadService)"
   - **Assign Agent 3**: "Start Phase 5 Group 2 (T053-T056 Command wiring)"
   - **Assign Agent 4**: "Start Phase 5 Group 3 (T057-T061 Progress dialog)"
3. All 3 agents work in parallel on Phase 5

### Step 7: Phases 6-8 (Polish)
After Phase 5 complete:
- Phase 6 (US4 - Polish): Agent 1 + Agent 4
- Phase 7 (US5 - Validation): Agent 2 + Agent 3
- Phase 8 (Final): All agents collaborate

## Agent Coordination Protocol

### Agent Completion Report Format
When agent completes tasks, they should report:
```json
{
  "agent": "agent_1",
  "completed_tasks": ["T005", "T006", "T007", "T008"],
  "files_created": [
    "VesselStudioSimplePlugin/Models/QueuedCaptureItem.cs"
  ],
  "status": "ready_for_next_assignment"
}
```

### Your Response Format
```
Verified: T005-T008 complete
Next assignment: T009-T010 (CaptureQueue model)
Dependencies satisfied: QueuedCaptureItem exists
Estimated time: 30 minutes
```

### JSON Update Pattern
After each agent completion:
1. **Load** current agent-coordination.json
2. **Update** relevant fields:
   - `phases.{phase}.completed_tasks` (append task IDs)
   - `agents.{agent}.completed_tasks` (append task IDs)
   - `agents.{agent}.current_task` (update to next assignment)
   - `execution_log` (append log entry)
3. **Write** updated JSON back
4. **Assign** next work to agent

## Critical Gates & Blockers

### Phase 2 Gate (MOST CRITICAL)
**Blocks**: All User Story work (Phase 3-5)
**Requirements**:
- ✅ QueuedCaptureItem.cs exists with all properties
- ✅ CaptureQueue.cs exists with Add/Remove/Clear methods
- ✅ CaptureQueueService.cs exists with singleton pattern
- ✅ BatchUploadProgress.cs exists
- ✅ BatchUploadResult.cs exists
- ✅ All Phase 2 tasks (T005-T017) marked complete in JSON

**Do NOT allow** Agent 3 or Agent 4 to start Phase 3 until this gate opens.

### Phase 3 Gate
**Blocks**: Phase 4 (QueueManagerDialog)
**Requirements**:
- ✅ VesselAddToQueueCommand works
- ✅ Toolbar badge shows count
- ✅ Quick Export button enabled when queue has items

### Phase 4 Gate
**Blocks**: Phase 5 (Batch Upload)
**Requirements**:
- ✅ QueueManagerDialog opens in <500ms
- ✅ ListView displays thumbnails correctly
- ✅ Remove/Clear buttons work

## Conflict Resolution

### File Locking
If two agents need to edit the same file:
1. Serialize the work (agent 1 first, agent 2 waits)
2. Split file into regions (agent 1 does top half, agent 2 bottom half)
3. Use partial classes (agent 1 creates base, agent 2 extends)

### JSON Conflicts
If multiple agents update JSON simultaneously:
1. Use append-only execution log (no conflicts)
2. Orchestrator is ONLY entity that updates phase/agent status
3. Agents only report completion, don't modify JSON

### Dependency Violations
If agent starts work before dependencies met:
1. **HALT** the agent immediately
2. Verify dependency files exist
3. Re-assign work in correct order

## Monitoring & Debugging

### Progress Dashboard (Conceptual)
```
Phase 2 (Foundational): [▓▓▓▓▓▓▓░░░] 70% (10/13 tasks)
├─ Agent 1: Working on T009-T010 (CaptureQueue)
└─ Agent 2: Complete T016-T017, waiting for T011-T015 unblock

Phase 3 (US1): [░░░░░░░░░░] 0% - BLOCKED (waiting Phase 2)
Phase 4 (US2): [░░░░░░░░░░] 0% - BLOCKED (waiting Phase 3)
Phase 5 (US3): [░░░░░░░░░░] 0% - BLOCKED (waiting Phase 3)
```

### Verification Checklist
After each agent completion:
- [ ] Files exist at specified paths
- [ ] No compiler errors (`dotnet build`)
- [ ] JSON updated with completed tasks
- [ ] Dependencies satisfied for next assignment
- [ ] Agent reports success criteria met

## Success Criteria

### Phase 2 Complete
- [ ] All 4 models exist (QueuedCaptureItem, CaptureQueue, BatchUploadProgress, BatchUploadResult)
- [ ] CaptureQueueService singleton works
- [ ] 13 tasks complete (T005-T017)
- [ ] No compiler errors
- [ ] Agent 1 & 2 idle, ready for next assignment

### Phase 3 Complete
- [ ] VesselAddToQueueCommand registered
- [ ] Toolbar badge + button functional
- [ ] 11 tasks complete (T018-T028)
- [ ] Agent 3 & 4 idle

### Phase 4 Complete
- [ ] QueueManagerDialog displays queue items
- [ ] Remove/Clear buttons work
- [ ] Dialog opens <500ms
- [ ] 14 tasks complete (T029-T042)

### Phase 5 Complete
- [ ] Batch upload works with progress dialog
- [ ] Success/failure messages show
- [ ] 21 tasks complete (T043-T061)

### MVP Complete (End of Phase 5)
- [ ] 63 total tasks complete (T001-T061 minus polish)
- [ ] User can queue captures, view queue, export batch
- [ ] All 3 user stories (US1, US2, US3) working
- [ ] No critical bugs

## Next Actions (START NOW)

1. **Assign Agent 1**: Provide `prompts/agent-1-foundational-models.md` prompt
   - Say: "Agent 1, start T005-T008 (QueuedCaptureItem model). Report when complete."
   
2. **Assign Agent 2**: Provide `prompts/agent-2-batch-models.md` prompt
   - Say: "Agent 2, start T016-T017 (Batch models) in parallel with Agent 1. Report when complete."
   
3. **Block Agent 3 & 4**: 
   - Say: "Agent 3 & 4, wait for Phase 2 completion before starting."
   
4. **Monitor**: Wait for Agent 1 or Agent 2 completion reports
   - When Agent 1 completes T005-T008 → assign T009-T010
   - When Agent 2 completes T016-T017 → wait for Agent 1 to finish T009-T010, then assign T011-T015
   - When both complete → open Phase 3 gate, assign Agent 3 & 4

## Estimated Timeline

- **Phase 2**: 2-3 hours (foundational models + service)
  - Agent 1 Group 1: 1 hour (T005-T008)
  - Agent 2 Group 2: 30 min (T016-T017) - parallel
  - Agent 1 Group 3: 30 min (T009-T010)
  - Agent 2 Group 4: 1 hour (T011-T015)
- **Phase 3**: 1.5 hours (Agent 3 & 4 parallel)
- **Phase 4**: 2 hours (Agent 4 dialog)
- **Phase 5**: 3 hours (Agent 2, 3, 4 parallel)
- **Total MVP**: ~7.5 hours with parallelization

**Current Status**: Ready to start Phase 2 Groups 1 & 2 in parallel
