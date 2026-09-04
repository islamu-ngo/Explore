---
name: implement-tasks
description: "Load when executing, running, or implementing an approved task plan from `dev/active/<task>/`; orchestrates phase execution, optional worktree isolation (`git worktree` + plan `mv`), Red/Green/Refactor task loops, phase commits, and final knowledge graduation to `dev/backlog/`."
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: Workflow skill for executing approved implementation tasks from dev/active/<task>/. -->
<!-- ABOUTME: Guides in-tree vs. worktree execution, phase-by-phase implementation, commit contracts, and graduation to dev/backlog/. -->

## Must-Read Docs
- [../../../AGENTS.md](../../../AGENTS.md)
- [../../../.agents/CONTEXT_ENGINEERING.md](../../../.agents/CONTEXT_ENGINEERING.md)
- [../conventional-commit/SKILL.md](../conventional-commit/SKILL.md)

## Top Invariants

1. **Execute Only Approved Plans**: Never implement without an approved task plan (`tasks.md`, `context.md`, `plan.md`). Verify approval state before modifying runtime code.
2. **Execution Strategy Decoupling**: The implementation plan does not dictate execution topology. The developer or agent chooses the execution mode based on concurrency needs:
   - **Mode A: In-Tree Execution** (Default) — Work directly in the current repository checkout. Before branching, ensure base freshness: `git checkout develop && git pull --ff-only`, then `git checkout -b feat/<task-name> develop`.
   - **Mode B: Worktree Isolation** — Isolate work in a separate git worktree (e.g. `../Event-<task-name>`). Best when multiple agents or contributors work in parallel to prevent dirty-tree conflicts.
3. **Upstream Freshness & Worktree Transition Protocol**:
   - Always ensure upstream freshness first: `git fetch origin develop`
   - Create worktree from latest upstream: `git worktree add -b feat/<task-name> ../Event-<task-name> origin/develop`
   - Move active plan: `mkdir -p ../Event-<task-name>/dev/active && mv dev/active/<task-name> ../Event-<task-name>/dev/active/`
   - Switch context: Continue all implementation, testing, and task updates inside `../Event-<task-name>`.
   - Untracked by default: The new worktree inherits `.gitignore` (`dev/active/*`).
4. **Dev-Doc Working Memory & Native Tools**: Active plan files (`tasks.md`, `context.md`) are gitignored local working memory. Read and edit them using native harness file tools by deterministic path. Do not use ad-hoc shell scripts (`cat`, `sed`, `awk`) for file manipulation (Critical Rule #9).
5. **Phase-by-Phase Execution Cadence**:
   - **Red**: Author failing invariant/specification tests first for core domain, concurrency, state machines, and security boundaries.
   - **Green**: Implement production code to satisfy invariants.
   - **Sliced Verification**: Run targeted test class via `--treenode-filter` (~1.5s).
   - **Phase Verification**: Run Release build and single selected project test.
   - **Immediate Phase Commit**: Execute the self-sufficient planned Conventional Commit contract(s) in `tasks.md`. If a phase defines multiple atomic commits (for large or multi-concern phases), execute each in sequence. Stage and commit ONLY exact phase-owned paths related to the implementation plan, strictly isolating them from unrelated working-tree modifications.
   - **Reconcile Ledger**: Check completed tasks in `tasks.md`, update current priority and blockers in `context.md`.
6. **Knowledge Graduation Gate (Mandatory Before Workstream Close)**:
   Before declaring work complete, merging the branch, or removing the worktree, promote durable knowledge:
   - **Deferred Work**: Create `dev/backlog/<topic-slug>.md` with problem statement and acceptance criteria.
   - **Architectural Decisions**: Create an ADR in `docs/internal/adr/ADR-XXX-<name>.md`.
   - **Lessons & Quirks**: Append to `dev/_journal/domains/<domain>.md` or `dev/_journal/journal.md`.
   - Stage and commit these persistent files on the branch so they merge into `develop`!
7. **Worktree Disposal (When Worktree Mode Was Used)**:
   - Once the branch is pushed, PR created, or merged: `git worktree remove ../Event-<task-name>`.
   - The ephemeral `dev/active/<task-name>/` directory is cleanly deleted with the worktree. Zero git log noise or cleanup commits.

## Workflow

```text
1. Inspect approved dev/active/<task>/ (tasks.md, context.md, plan.md)
2. Choose topology (with upstream freshness gate):
   - In-Tree: git checkout develop && git pull --ff-only && git checkout -b feat/<task> develop
   - Worktree: git fetch origin develop && git worktree add -b feat/<task> ../Event-<task> origin/develop + mv dev/active/<task> into worktree
3. Loop through Phases:
   a. Red: failing invariant test
   b. Green: implementation code
   c. Verify: sliced test -> phase build & test
   d. Commit: execute planned contract(s) from tasks.md (sequential atomic commits if phase is large)
   e. Update: check task in tasks.md, update context.md
4. Workstream Close & Knowledge Graduation:
   a. Any deferred items? -> write dev/backlog/<slug>.md
   b. Any non-obvious lessons? -> append to dev/_journal/
   c. Any new architectural invariants? -> write ADR in docs/internal/adr/
   d. Stage and commit graduation files
5. Cleanup:
   - If worktree: git worktree remove ../Event-<task>
   - If in-tree: delete local dev/active/<task> folder
```

## Verification Hooks
- `dotnet build --configuration Release --verbosity quiet`
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- `git diff --check -- .agents/skills/implement-tasks`

## Related Skills
- [../implementation-plan/SKILL.md](../implementation-plan/SKILL.md)
- [../senior-cto-feedback/SKILL.md](../senior-cto-feedback/SKILL.md)
- [../conventional-commit/SKILL.md](../conventional-commit/SKILL.md)
- [../finding/SKILL.md](../finding/SKILL.md)
