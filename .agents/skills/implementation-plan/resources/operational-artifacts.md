<!-- ABOUTME: Templates for synchronized implementation context and task checklist artifacts. -->
<!-- ABOUTME: Keeps workstreams resumable, status-consistent, and maintainable during implementation. -->

# Operational Artifacts

## Dev-Doc Triad Responsibility Matrix

| Artifact | Single Source of Truth For | Forbidden Content (Belongs Elsewhere) |
|---|---|---|
| `dev/active/<task>/<task>-plan.md` | Strategic architectural design, current-state evidence, design decisions (ADRs), phase-level boundaries and exit criteria | Granular task checklists (`- [ ]`), dynamic execution statuses (`IN PROGRESS`), session handoffs, dirty worktree logs |
| `dev/active/<task>/<task>-tasks.md` | Hot execution ledger, granular Red/Green/Refactor task breakdown (`- [ ] **N.M**`), task checkboxes, phase verification gates | Lengthy architectural narrative, design trade-off debates, deep security/I-VSD analysis |
| `dev/active/<task>/<task>-context.md` | Active session working memory (`COMPLETED`, `IN PROGRESS`, `NEXT`, `BLOCKERS`), quick resume, validation baseline, dated session handoffs | Granular task execution checklists, full architectural specifications |

## Review State Contract

Plan, context, and tasks repeat only this compact shared state:

```text
- I-VSD report: <relative path>
- I-VSD reviewed input revision: <Git object or SHA-256 digest>
- I-VSD status / disposition: <current + plan-aligned, or blocking state>
- CTO review: Not reviewed | Changes required | <linked review artifact>
- User approval: Awaiting approval | Approved for <workstream revision>
```

The I-VSD report owns moral analysis; the CTO review owns technical-readiness detail. The triad stores links and current state, not duplicate narratives.

## Context File

Use this structure for `dev/active/<task-name>/<task-name>-context.md`:

```markdown
# <Human Title> — Context

Last Updated: YYYY-MM-DD Europe/Brussels

## Review State
<Use the shared Review State Contract above.>

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
For every implementation phase: one Release build and at most one fastest relevant non-browser project test, both run once after the phase tasks. Record any proven unrelated shared-tree failure separately from the phase-owned verification result; never relabel it green.

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
- **Completed:** 0/N implementation tasks (phase verification and commit closure tracked separately)
- **Current priority:**
- **Next recommended slice:**
- **Review state:** I-VSD path/revision/disposition, CTO review, and user approval from the shared contract.

## Implementation Maintenance Rules
- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task `🟡 IN PROGRESS` when it is likely to span multiple edits or a handoff; skip this churn for tiny tasks completed immediately.
- Check a substantial completed task immediately; reconcile small completed tasks no later than phase end.
- Add discovered work where it belongs and keep completed count, priority, next slice, deferred work, and update date accurate.
- Check a phase complete only after all implementation, phase-verification disposition, and phase-commit checkboxes pass.
- Close every verified phase immediately with phase-owned Conventional Commit(s); for large phases (touching dozens or hundreds of files) or multi-concern scopes, sequence multiple atomic commits following `conventional-commit` rules 1 & 13. Commits must strictly target only phase-owned files related to this implementation plan. The approved checklist authorizes the implementing agent to commit without another prompt.
- Use the planning-authored default title, description, changelog treatment, and trailers unchanged when they remain truthful; do not spend implementation context recomposing them.
- Do not load `conventional-commit` merely to reuse the approved contract; load it only when a permitted material divergence requires replacement contracts.
- Override a planned message only for a recorded material divergence, never stylistic preference.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance criteria, risk, or validation strategy changes.
- Do not run build/tests after individual tasks; verify once at phase end.
- Do not start the app, browser, Docker, Aspire, Playwright, Chrome DevTools MCP, or live services for verification.
- Use a task branch/worktree for parallel work, and never modify, unstage, stage, or commit another contributor's work in any checkout.

## Phase 1: <Name> ⏳ NOT STARTED
**Phase-owned paths:** exact files this phase may stage; update before verification when legitimate phase work discovers or generates another file.

- [ ] **1.1 <Task name>**
  - **Files:** exact paths marked existing/new
  - **Acceptance:** observable outcome
  - **Effort:** S/M/L/XL
  - **Dependencies:** task ids

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] Run `dotnet build --configuration Release --verbosity quiet` once and record pass evidence or the exact proven unrelated shared-tree failure.
- [ ] Run `dotnet test --project <one-relevant-project>.csproj --configuration Release --verbosity quiet` once and record pass evidence or the exact proven unrelated shared-tree failure.
- [ ] Confirm the phase-owned verification lane is green and no phase-attributable failure remains.

### Phase 1 Commit(s) — RUN IMMEDIATELY AFTER VERIFICATION
*(Note: If Phase 1 is large or touches dozens/hundreds of files across multiple concerns, sequence multiple atomic commit contracts below instead of one monolithic umbrella commit. Every commit MUST strictly stage and commit ONLY its own phase-owned files related to this implementation plan, leaving unrelated working-tree modifications untouched).*

#### Planned Commit Contract [Contract 1 of N for multi-commit phases]
- **Default title:** `type(scope): benefit-led phase outcome`
- **Default description:** Exact motivation and data/control-flow description for this phase.
- **Changelog treatment:** Public feature/fix | Change fragment `CHG-YYYY-NNNN` | `Changelog: skip`
- **Required trailers:** Exact terminal trailer lines, or `None`
- **Commit paths:** Exact ordered list of wholly phase-owned files for this commit.
- **Pre-commit inspection commands:**
  - `git status --short`
  - `git diff --name-only`
  - `git diff --cached --name-only`
- **Staging command:** `git add -- <every exact commit path>`
- **Commit command:** `git commit --only -m "<exact title>" -m "<exact description>" <exact trailer arguments> -- <every exact commit path>`
- **Post-commit verification command:** `git show --name-only --format=fuller HEAD`
- **Message override:** Not overridden

<!-- Repeat Planned Commit Contract block for Contract 2, 3, etc. if phase is large -->

The completed workstream MUST replace every placeholder above with concrete paths and commands. The staging/commit pathspecs must exactly equal `Commit paths`, and the commit command must encode the declared message and trailers.

- [ ] Without loading `conventional-commit`, run the exact inspection commands, confirm every path/hunk is wholly phase-owned and related to this plan, and execute the exact staging and commit commands when the contract remains truthful. If multiple atomic commits are defined, execute each in sequence.
- [ ] Only when the default will not be used, load `conventional-commit` and record `Message override: Yes`, `Reason`, and complete `Actual commit contracts` containing every metadata, path, and command field above.
- [ ] Run the exact post-commit verification command, confirm the resulting file list equals `Commit paths`, and record the hash before completing Phase 1.

## Remaining / Deferred Work & Knowledge Graduation
- **Deferred Work:** Explicit deferral, reason, trigger, and target backlog item (`dev/backlog/<slug>.md`).
- **Durable Findings:** Non-obvious quirks or bug root causes promoted to `dev/_journal/domains/<domain>.md`.
- **Architectural Decisions:** Permanent system invariants promoted to `docs/internal/adr/ADR-XXX-<name>.md`.
*(Note: These persistent files must be committed on the feature branch before workstream close so they merge into develop).*
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
- phase-owned paths, verification disposition, planned/actual commit metadata and command packet, override reason, and phase commit hash;
- deferred work and risks;
- I-VSD path/revision/status, CTO review, and user approval.

Any disagreement is a planning defect; fix it before handoff.

## Progressive Maintenance Cadence

Use the lightest update that keeps repository state truthful:

| Trigger | Required update | Do not do |
|---|---|---|
| Substantial task started | Mark it `🟡 IN PROGRESS` and set current priority when it will span meaningful work | Update docs for a tiny task completed immediately |
| Substantial task completed | Check its box, update completed count/current priority/next slice/date | Rewrite context or plan without another trigger |
| Several small related tasks completed | Batch their checkbox updates before starting another phase | Defer updates to a later cleanup command |
| Phase implementation completed | Reconcile every task, run the two phase-end checks once, and classify each failure by ownership | Mark a phase-attributable failure external or repair another contributor's files |
| Phase verification resolved | Use the self-sufficient planned contract(s) without loading `conventional-commit`, commit only exact phase-owned paths related to the plan (sequencing multiple atomic commits if phase is large), and record the verified hash(es) | Reload/recompose a truthful contract, defer to a new session, use blind staging, create an oversized umbrella commit, or include unrelated work |
| Planned commit message became false | Load `conventional-commit`, then record the permitted material-divergence reason and an `Actual commit contracts` list containing a complete contract for every resulting commit; update plan/context when their owned state changed | Silently override for style, preference, or convenience |
| New task discovered | Add it under the owning phase with acceptance criteria and dependencies | Hide it in chat or context only |
| Blocker or validation failure | Mark affected work in progress/blocked and record cause plus recovery action in tasks/context; proven unrelated failures name their external path/evidence | Claim the repository is green or block/fix unrelated work without an ownership decision |
| Scope, architecture, sequence, risk, or acceptance changed | Update plan, then mirror task/context impact | Re-baseline unchanged sections |
| Pause, compaction, transfer, or PR | Reconcile affected tasks and add a concise context handoff | Perform a broad unrelated workstream sweep |

Implementation completion, verification disposition, and phase completion are distinct. Check an implementation task when its acceptance criteria are satisfied. The phase completes only after its own verification is green, any unrelated shared-tree failure is explicitly recorded, and all phase-owned Conventional Commits succeed.

## Read Cadence

- Initial implementation start: read all three artifacts once.
- Same uninterrupted session: do not reread unchanged artifacts after each task.
- Cold resume or new agent: read context and tasks, then the current phase plus referenced decisions/constraints from the plan.
- Scope conflict or stale evidence: reread only the affected plan/current-state sections before changing direction.
- After writing a checkbox or status update: do not reopen the file merely to restate what was just written; continue implementation.

## Stale-State Recovery

If an agent inherits stale docs, perform one bounded reconciliation for the current workstream using the conversation, changed files, validation, and phase commits already produced in the session. Update the plan first only when strategy drifted, then reconcile context and task checkboxes. Do not sweep unrelated `dev/active/` workstreams, absorb unrelated dirty files into a recovery commit, rerun already-green commands, or turn the journal into a session log; record a journal entry only for a non-obvious durable finding likely to recur.
