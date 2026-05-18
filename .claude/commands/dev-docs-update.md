---
description: Force-refresh active dev documentation before handoff, context compaction, or agent switch
argument-hint: Optional - active task name, focus area, or reason for refresh/handoff
---

# `/dev-docs-update` — Forced Dev Docs Synchronization

You are refreshing implementation memory for the ISLAMU Event platform. This command is used when context is getting tight, work is being handed to another agent, or the user needs to force an implementation agent to update stale dev docs after working for too long without maintaining them.

Your job is not to write a pleasant session summary. Your job is to make the active dev docs truthful enough that a cold agent can continue the work without re-discovering the session.

Additional user context:

> `$ARGUMENTS`

---

## Non-Negotiable Outcome

For every relevant active workstream, reconcile and update the three-file dev-docs set:

```text
dev/active/[task-name]/
├── [task-name]-plan.md      # Strategy, scope, phases, risks, acceptance criteria
├── [task-name]-context.md   # Current state, decisions, files, blockers, handoff
└── [task-name]-tasks.md     # Tactical checklist and next slice
```

If a workstream still uses older names such as `plan.md`, `context.md`, or `tasks.md`, update those files in place unless a rename is already part of the workstream. Do not create duplicate docs just to satisfy naming conventions.

Every touched dev-doc file must include or refresh:

```markdown
Last Updated: YYYY-MM-DD Europe/Brussels
```

---

## Command Modes

Infer the mode from `$ARGUMENTS` and the repo state. If unsure, prefer the more comprehensive mode.

### 1. Handoff / Context Compaction Mode

Use when the user says context limit, handoff, switch agent, pause, compact, reset, continue later, or similar.

Required result:

- plan/context/tasks are synchronized;
- a dated handoff is added or refreshed;
- next actions are explicit;
- validation state is clear;
- unrelated dirty worktree changes are called out so the next agent does not touch them accidentally.

### 2. Forced Refresh Mode

Use when the user says the agent forgot to update docs, implementation has moved ahead, the plan is stale, or they want docs updated before another agent continues.

Required result:

- compare current implementation state against the plan;
- re-baseline stale plan sections before updating context/tasks;
- mark completed work accurately;
- add discovered work and remaining risks;
- make the next implementation slice obvious.

### 3. Comprehensive Active Docs Sweep

Use when `$ARGUMENTS` is empty or asks for a general update.

Required result:

- inspect `dev/active/`;
- update only workstreams affected by the current session or explicitly requested by the user;
- do not rewrite unrelated active docs without evidence they are stale.

---

## Required References

Read these before editing:

- `dev/active/README.md` — canonical active-doc pattern.
- `dev/HANDOFF_TEMPLATE.md` — canonical handoff sections.
- `.claude/commands/dev-docs.md` — planning contract that this command enforces.
- `dev/_journal/README.md` and `dev/_journal/FINDING_TEMPLATE.md` — durable finding format, only if a non-obvious reusable finding should be recorded.

When updating implementation docs, also read the relevant active workstream files:

- `dev/active/[task-name]/[task-name]-plan.md` or `plan.md`;
- `dev/active/[task-name]/[task-name]-context.md` or `context.md`;
- `dev/active/[task-name]/[task-name]-tasks.md` or `tasks.md`.

---

## Workstream Resolution

1. Determine the relevant active workstream from `$ARGUMENTS`, current conversation, modified files, and `dev/active/` contents.
2. If exactly one workstream is relevant, update it.
3. If multiple workstreams are relevant, update each one only for changes that clearly belong to it.
4. If multiple workstreams could match and there is no safe basis to choose, document the ambiguity in the final response and update no files beyond any clearly relevant one.
5. Preserve unrelated dirty files. Do not fold unrelated worktree changes into an active doc unless they directly affect that workstream.

Useful evidence sources:

- `git status --short` for modified/untracked files;
- `git diff --name-only` for changed paths;
- active task plans/context/tasks;
- recent validation output from this session;
- current conversation state.

Do not claim a file changed, a test passed, or a blocker exists unless you verified it from session evidence, repo state, or command output.

---

## Forced Synchronization Workflow

Follow this order. Do not update only the context file and stop.

### 1. Read Existing Active Docs

For each selected workstream, read the plan, context, and tasks files. Extract:

- planned phases and acceptance criteria;
- current session progress;
- completed/in-progress/blocked tasks;
- validation baseline;
- known risks and unknowns;
- last recorded handoff.

### 2. Reconcile Against Reality

Compare docs against the current implementation/session state:

- What was implemented since the last update?
- What was verified?
- What failed validation?
- What remains incomplete?
- What changed in scope, architecture, implementation order, or risk?
- Which files changed and why?
- Which changes are unrelated dirty work that the next agent must avoid?

If the implementation has drifted from the plan, update the plan first. A stale plan poisons future implementation.

### 3. Update The Plan When Needed

Update `[task-name]-plan.md` when any of these changed:

- planning status;
- scope or out-of-scope boundaries;
- implementation phases or task ordering;
- architecture/design decisions;
- acceptance criteria;
- risks, blockers, or unknowns;
- validation strategy;
- docs/config/deployment impact;
- remaining/deferred work.

When re-baselining, add a short dated note such as:

```markdown
## Re-baseline — YYYY-MM-DD Europe/Brussels
- **Reason:** ...
- **What changed:** ...
- **Plan impact:** ...
- **Remaining work:** ...
```

If the plan was already accurate, do not churn it. Note in the final response that the plan did not require changes.

### 4. Update The Context File

Ensure `[task-name]-context.md` contains a fresh top-level operational snapshot:

```markdown
## SESSION PROGRESS (YYYY-MM-DD Europe/Brussels)

### ✅ COMPLETED
- ...

### 🟡 IN PROGRESS
- ...

### ⏭️ NEXT
1. ...
2. ...
3. ...

### ⚠️ BLOCKERS
- None known / ...
```

Also refresh or add these sections as appropriate:

- **Current Implementation State:** what exists now, not what was planned.
- **Key Decisions This Session:** decision, why, affected files.
- **Files Modified And Why:** exact paths and concise reasons.
- **Validation State:** commands run, result, warnings, commands still needed.
- **Risks / Unknowns:** remaining risk, detection signal, next action.
- **Quick Resume:** precise restart steps for a cold agent.

The context file should answer: “What happened, where are we now, and what should the next agent do first?”

### 5. Update The Tasks File

Ensure `[task-name]-tasks.md` reflects reality:

- mark completed tasks with checked boxes;
- mark current work as in progress where the file uses status labels;
- add tasks discovered during implementation;
- split vague remaining work into actionable tasks;
- preserve acceptance criteria and validation notes;
- update “Status Summary,” completed counts, current priority, and next recommended slice if present;
- add or update “Remaining / Deferred Work” with reasons.

Do not mark work complete unless implementation and required validation are actually complete or the task explicitly allows documentation-only completion.

### 6. Add Or Refresh Handoff Notes

Handoff notes are mandatory when pausing, nearing context reset, switching agents, validation is incomplete, blockers remain, or dirty unrelated files exist.

Use this structure inside the active context file unless the workstream already has a dedicated handoff location:

```markdown
## Handoff — YYYY-MM-DD Europe/Brussels

### Current State
- What is completed:
- What is in progress:
- What changed since the last handoff:

### Next Action
1. ...
2. ...
3. ...

### Blockers
- None known / describe blocker, owner, and decision needed.

### Modified Files
- `path/to/file` — why it changed / current status.

### Validation
- Commands run:
  - `command` — result
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

Keep the handoff short but exact. Put strategic changes in the plan, execution state in context, and checkboxes in tasks.

### 7. Journal Durable Findings Only When Appropriate

Do not use the journal as a session log. Use `dev/_journal/journal.md` only for non-obvious, durable, reusable findings such as:

- bug root causes not obvious from the patch;
- system quirks likely to recur;
- important design decisions not yet promoted to canonical docs;
- confirmed/refuted assumptions from skills, rules, or previous plans.

If a finding qualifies, append using `dev/_journal/FINDING_TEMPLATE.md` with the required prefix:

```markdown
[YYYY-MM-DD Europe/Brussels] — <Short descriptive title>
```

If the decision is system-wide, update `dev/_journal/MAJOR_DECISIONS.md` or recommend that follow-up in the tasks/context file.

---

## Validation Expectations

This command primarily edits markdown, but validation still matters.

Run, at minimum:

```bash
rtk git diff --check -- <updated-dev-doc-files>
```

If agent-context docs, slash commands, rules, skills, or journal format changed, also run the architecture/context test project:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

If implementation files changed in the same session and validation has not yet run, record the exact commands still needed in the handoff and tasks file. Do not pretend validation passed.

---

## Quality Gates Before Final Response

Before responding, verify:

- [ ] Relevant active workstream(s) were identified.
- [ ] Plan was checked and updated if stale.
- [ ] Context contains a fresh session progress snapshot.
- [ ] Tasks reflect completed, in-progress, remaining, and discovered work.
- [ ] Handoff notes exist when pausing/switching/compacting or when blockers/dirty unrelated files exist.
- [ ] Validation run/results or still-needed commands are documented.
- [ ] Durable findings were journaled only if they met journal criteria.
- [ ] Unrelated dirty worktree changes are explicitly called out if relevant.
- [ ] A cold agent can read the docs and know the next action.

If any gate fails, keep updating the docs until it passes or clearly explain why it cannot be safely completed.

---

## Final Response Contract

Respond concisely with this structure:

```markdown
Updated dev docs for `[task-name]`:
- **Plan:** updated / no change needed / not found — reason
- **Context:** updated with current state, next steps, and handoff
- **Tasks:** updated completed/in-progress/remaining work
- **Handoff:** added/refreshed/not needed — reason
- **Journal:** entry added/not needed — reason

What changed technically:
- Medium-sized developer teaching summary of the implementation or doc synchronization work. Name the patterns, libraries/infrastructure, important files/classes, data/control flow, and project/industry conventions involved. Do not reduce this to an abstract sentence like “dev docs updated.”

Verified:
- `command` — result

Remaining:
- ...

Next:
- ...
```

If multiple workstreams were updated, repeat the bullets per workstream.

Never end with only “documentation updated.” Always teach what changed technically, state what remains, and state what should happen next. The user is a developer; write enough technical detail that they can understand the implementation state without reading the diff first.
