ABOUTME: Short handoff template for pausing or transferring active development work.
ABOUTME: Captures current state, next action, blockers, files, validation, and risks.

# Handoff Template

Use this when pausing a multi-session task, preparing for context reset, or handing work to another contributor or AI agent.

```markdown
## Handoff — YYYY-MM-DD

### Current State
- What is completed:
- What is in progress:
- What changed since the last handoff:

### Next Action
1. 
2. 
3. 

### Blockers
- None known / describe blocker, owner, and decision needed.

### Modified Files
- `path/to/file` — why it changed / current status.

### Validation
- Commands run:
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — result
- Commands still needed:

### Documentation Impact
- Updated / Not needed / Deferred with reason:

### Risks
- Source-grounding risks:
- Test or build risks:
- Operator/release risks:

### Notes For Next Contributor Or Agent
- Required docs/rules to read:
- Assumptions made:
- Do not touch / unrelated dirty files:
```

Keep handoffs short enough to update often. Put detailed plans in the active task plan, durable facts in the active context file, and checklist state in the active tasks file.
