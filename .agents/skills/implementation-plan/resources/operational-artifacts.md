<!-- ABOUTME: Templates for synchronized implementation context and task checklist artifacts. -->
<!-- ABOUTME: Keeps workstreams resumable, status-consistent, and maintainable during implementation. -->

# Operational Artifacts

## Dev-Doc Triad Responsibility Matrix

| Artifact | Single Source of Truth For | Forbidden Content (Belongs Elsewhere) |
|---|---|---|
| `dev/active/<task>/<task>-plan.md` | Strategic architectural design, current-state evidence, design decisions (ADRs), phase-level boundaries and exit criteria | Granular task checklists (`- [ ]`), dynamic execution statuses (`IN PROGRESS`), session handoffs, dirty worktree logs |
| `dev/active/<task>/<task>-tasks.md` | Hot execution ledger, granular Red/Green/Refactor task breakdown (`- [ ] **N.M**`), task checkboxes, phase verification gates | Lengthy architectural narrative, design trade-off debates, deep security/I-VSD analysis |
| `dev/active/<task>/<task>-context.md` | Active session working memory (`COMPLETED`, `IN PROGRESS`, `NEXT`, `BLOCKERS`), quick resume, validation baseline, dated session handoffs | Granular task execution checklists, full architectural specifications |

## Context File

Use this structure for `dev/active/<task-name>/<task-name>-context.md`:

```markdown
# <Human Title> — Context

Last Updated: YYYY-MM-DD Europe/Brussels

## SESSION PROGRESS (YYYY-MM-DD Europe/Brussels)

### ✅ COMPLETED
- Planning created or re-baselined.
- Current-state report completed with evidence.

### 🟡 IN PROGRESS
- Awaiting user review of the implementation plan.

### ⏭️ NEXT
1. User reviews and corrects or approves the plan.
2. First implementation agent starts with task <id/name>.
3. Refresh context after the first implementation slice.

### ⚠️ BLOCKERS
- None known, or the exact decision/evidence blocker.

## Quick Resume
1. Read this context and `<task-name>-tasks.md`.
2. Read only the current phase, constraints, or changed decisions from `<task-name>-plan.md`; do not reread the full unchanged plan on every resume.
3. Start from the first unchecked high-priority task unless the user overrides it.
4. Keep `tasks.md` current during implementation and update context/plan only at their defined triggers.

## Key Files And Responsibilities
Table: Path, Existing/New, Layer, Purpose, Notes.

## Key Decisions
Concise decision log synchronized with the plan.

## Constraints And Rules To Remember
Matched intents, relevant rules/skills, and task-specific invariants.

## Validation Baseline
For every implementation phase: one Release build and at most one fastest relevant non-browser project test, both run once after the phase tasks.

## Current Known Risks / Unknowns
Short list with owning task ids.

## Handoff Notes

### Handoff — YYYY-MM-DD Europe/Brussels
- **Current state:**
- **Next action:**
- **Blockers:**
- **Modified files:**
- **Validation:**
- **Documentation impact:**
- **Risks:**
- **Notes for next contributor/agent:**
```

Put immediately actionable state at the top. Replace template status with investigated reality; never leave “None” when a blocker or failed baseline is known.

## Tasks File

Use this structure for `dev/active/<task-name>/<task-name>-tasks.md`:

```markdown
# <Human Title> — Task Checklist

Last Updated: YYYY-MM-DD Europe/Brussels

## Status Summary
- **Overall status:** Draft / User-reviewed / In implementation / Blocked / Complete
- **Completed:** 0/N implementation tasks (phase verification tracked separately)
- **Current priority:**
- **Next recommended slice:**

## Implementation Maintenance Rules
- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task `🟡 IN PROGRESS` when it is likely to span multiple edits or a handoff; skip this churn for tiny tasks completed immediately.
- Check a substantial completed task immediately; reconcile small completed tasks no later than phase end.
- Add discovered work where it belongs and keep completed count, priority, next slice, deferred work, and update date accurate.
- Check a phase complete only after all implementation and phase-verification checkboxes pass.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance criteria, risk, or validation strategy changes.
- Do not run build/tests after individual tasks; verify once at phase end.
- Do not start the app, browser, Docker, Aspire, Playwright, Chrome DevTools MCP, or live services for verification.

## Phase 1: <Name> ⏳ NOT STARTED
- [ ] **1.1 <Task name>**
  - **Files:** exact paths marked existing/new
  - **Acceptance:** observable outcome
  - **Effort:** S/M/L/XL
  - **Dependencies:** task ids

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project <one-relevant-project>.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work
- Explicit deferral, reason, trigger, and owner.
```

Repeat phase sections for every phase in the plan. Task ids, names, status, dependencies, acceptance criteria, and the single selected test project must match the plan exactly.

## Synchronization Rules

After writing all three artifacts, compare:

- planning and overall status;
- completed count and checked tasks;
- current priority and next recommended slice;
- phase/task ids and names;
- blockers and unknowns;
- decisions and constraints;
- validation baseline;
- deferred work and risks.

Any disagreement is a planning defect; fix it before handoff.

## Progressive Maintenance Cadence

Use the lightest update that keeps repository state truthful:

| Trigger | Required update | Do not do |
|---|---|---|
| Substantial task started | Mark it `🟡 IN PROGRESS` and set current priority when it will span meaningful work | Update docs for a tiny task completed immediately |
| Substantial task completed | Check its box, update completed count/current priority/next slice/date | Rewrite context or plan without another trigger |
| Several small related tasks completed | Batch their checkbox updates before starting another phase | Defer updates to a later cleanup command |
| Phase implementation completed | Reconcile every task, run the two phase-end checks once, then mark phase status | Mark phase complete before verification passes |
| New task discovered | Add it under the owning phase with acceptance criteria and dependencies | Hide it in chat or context only |
| Blocker or validation failure | Mark affected work in progress/blocked and record cause plus recovery action in tasks/context | Check the task or phase complete |
| Scope, architecture, sequence, risk, or acceptance changed | Update plan, then mirror task/context impact | Re-baseline unchanged sections |
| Pause, compaction, transfer, or PR | Reconcile affected tasks and add a concise context handoff | Perform a broad unrelated workstream sweep |

Implementation completion and phase completion are distinct. Check an implementation task when its acceptance criteria are satisfied; leave the phase verification boxes open until the single build and selected test pass.

## Read Cadence

- Initial implementation start: read all three artifacts once.
- Same uninterrupted session: do not reread unchanged artifacts after each task.
- Cold resume or new agent: read context and tasks, then the current phase plus referenced decisions/constraints from the plan.
- Scope conflict or stale evidence: reread only the affected plan/current-state sections before changing direction.
- After writing a checkbox or status update: do not reopen the file merely to restate what was just written; continue implementation.

## Stale-State Recovery

If an agent inherits stale docs, perform one bounded reconciliation for the current workstream using the conversation, changed files, and validation already produced in the session. Update the plan first only when strategy drifted, then reconcile context and task checkboxes. Do not sweep unrelated `dev/active/` workstreams, rerun already-green commands, or turn the journal into a session log; record a journal entry only for a non-obvious durable finding likely to recur.
