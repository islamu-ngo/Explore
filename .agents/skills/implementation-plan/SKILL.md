---
name: implementation-plan
description: Create or re-baseline lean, repository-grounded implementation plans and persistent dev docs when users ask for an implementation plan, technical plan, feature plan, refactor plan, or dev-docs workstream.
type: workflow
enforcement: block
priority: high
---
<!-- ABOUTME: Repository-grounded implementation-planning workflow for persistent dev docs. -->
<!-- ABOUTME: Converts planning requests into synchronized plan, context, and task artifacts without implementing code. -->

## Purpose
Create the operational source of truth that future implementation agents use before editing code. The skill investigates current repository reality, designs executable slices, and writes a resumable three-file workstream under `dev/active/` that implementation agents maintain progressively without repeated full rereads.

## When to Load
- Keywords: implementation plan, technical plan, plan this feature, refactor plan, create dev docs, dev-docs workstream.
- A complex change needs repository investigation and executable sequencing before implementation.
- Work is likely to span layers, multiple sessions, multiple contributors, or more than two hours.
- An existing `dev/active/<task-name>/` workstream needs re-baselining after scope or repository changes.
- A handoff requests persistent planning artifacts for future implementation agents.

## When NOT to Load
- Not for a simple bug fix, single-file edit, quick update, or other atomic task that can be safely implemented now.
- Not for product discovery or requirements elicitation without an implementation focus; use the PRD workflow instead.
- Not for reviewing an existing completed plan; use `senior-cto-feedback` for critique and approval readiness.
- Not for executing an approved plan; load the plan's implementation skills and rules instead.
- Not for claiming implementation has started or runtime behavior has changed.

## Must-Read Docs
- [../../../AGENTS.md](../../../AGENTS.md)
- [../../../docs/QUICK_REFERENCE.md](../../../docs/QUICK_REFERENCE.md)
- [../../../docs/GOVERNANCE.md](../../../docs/GOVERNANCE.md)
- [../../../docs/DOCUMENTATION_STYLE_GUIDE.md](../../../docs/DOCUMENTATION_STYLE_GUIDE.md)
- [../../../docs/OPERATIONS.md](../../../docs/OPERATIONS.md)
- [../../../docs/TESTING.md](../../../docs/TESTING.md)
- [../../../dev/active/README.md](../../../dev/active/README.md)
- [../../../.claude/contract/intents.yaml](../../../.claude/contract/intents.yaml)
- [resources/index.md](resources/index.md)
- [resources/investigation-workflow.md](resources/investigation-workflow.md)
- [resources/plan-template.md](resources/plan-template.md)
- [resources/operational-artifacts.md](resources/operational-artifacts.md)
- [resources/quality-gates.md](resources/quality-gates.md)

## Top 5 Invariants
1. Investigate and plan only; do not edit runtime code or claim implementation has started.
2. Verify every claimed existing path, symbol, test, contract, and configuration key from repository evidence before naming it as current state.
3. Classify the requested implementation work against every relevant intent and carry its docs, skills, rules, scope, tests, acceptance criteria, and forbidden moves into the plan.
4. Treat `tasks.md` as the hot progress ledger by checking substantial tasks immediately and all remaining completed tasks by phase end, while updating context only for meaningful state changes and the plan only for strategy changes.
5. Run verification once at each phase end using exactly one Release `dotnet build` command and at most one `dotnet test --project` command for the fastest relevant non-browser test project.

## Top 5 Anti-Patterns
1. Memory-based planning, which turns assumptions about the repository into false implementation facts.
2. Future-state-first planning, which designs changes before reporting what exists, what is missing, and what evidence supports those conclusions.
3. Verification sprawl, which wastes implementation time on per-task checks, multiple test commands, app startup, browser automation, Playwright, Chrome DevTools MCP, Aspire, Docker, or live-service smoke tests.
4. Stale checkbox debt, which postpones task updates until a separate refresh command and leaves completed implementation appearing unfinished.
5. Repetitive rereading, which reloads unchanged plan/context files after every task instead of using `tasks.md` as the current execution ledger.

## Minimal Examples
```text
Request: "Create an implementation plan for event RSVP."
Output:
dev/active/event-rsvp/event-rsvp-plan.md
dev/active/event-rsvp/event-rsvp-context.md
dev/active/event-rsvp/event-rsvp-tasks.md
```

```text
Planning sequence:
classify intents -> load rules/skills/docs -> inspect related work ->
verify code/tests/contracts -> report current state -> design slices ->
write and cross-check plan/context/tasks
```

```text
Phase-end verification only:
dotnet build --configuration Release --verbosity quiet
dotnet test --project <one-relevant-project>.csproj --configuration Release --verbosity quiet
```

```text
Progress cadence:
start/resume -> read once
substantial task done -> check it immediately
small tasks done -> reconcile no later than phase end
strategy changed -> update plan
decision/blocker/handoff -> update context
```

## Verification Hooks
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- `git diff --check -- .agents/skills/implementation-plan dev/active`

## Related Skills
- [../agentic-research/SKILL.md](../agentic-research/SKILL.md)
- [../senior-cto-feedback/SKILL.md](../senior-cto-feedback/SKILL.md)
- [../clean-architecture-rules/SKILL.md](../clean-architecture-rules/SKILL.md)
- [../skill-authoring/SKILL.md](../skill-authoring/SKILL.md)
