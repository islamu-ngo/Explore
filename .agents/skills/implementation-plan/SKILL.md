---
name: implementation-plan
description: "Load when asked to create, update, re-baseline, or continue a repository-grounded implementation/technical/refactor plan and its `dev/active/<task>` plan/context/tasks files; not for a product PRD, informal advice, or reviewing an already-written plan."
type: workflow
enforcement: block
priority: high
---
<!-- ABOUTME: Repository-grounded implementation-planning workflow for persistent dev docs. -->
<!-- ABOUTME: Converts planning requests into synchronized plan, context, and task artifacts without implementing code. -->

## Must-Read Docs
- [../../../AGENTS.md](../../../AGENTS.md)
- [../../../.agents/CONTEXT_ENGINEERING.md](../../../.agents/CONTEXT_ENGINEERING.md)
- [resources/index.md](resources/index.md)
- [resources/investigation-workflow.md](resources/investigation-workflow.md)
- [resources/plan-template.md](resources/plan-template.md)
- [resources/quality-gates.md](resources/quality-gates.md)
- [../i-vsd/SKILL.md](../i-vsd/SKILL.md)
- [../grill-me/SKILL.md](../grill-me/SKILL.md)

## Top 5 Invariants
1. Investigate and plan only; do not edit runtime code or claim implementation has started.
2. Execute the mandatory intake gate before writing: load `i-vsd`, evaluate provider responsibilities, follow its action routing, and create or update `islamic-value-sensitive-design/i-vsd-<task-name>.md`; then load `grill-me`, resolve repository facts first, and interrogate the user—with recommended answers—about every remaining material requirement, failure mode, and edge case.
3. Verify every claimed path, symbol, test, contract, and configuration key from repository evidence, then classify the work against every relevant intent and carry its docs, skills, rules, scope, tests, acceptance criteria, forbidden moves, and **Release & Changelog Strategy** into the plan.
4. **Test-First Invariant Sequencing**: Every behavioral implementation phase MUST sequence task authoring failing specification/invariant tests (Red Phase) *before* the task implementing the production code (Green Phase), preventing post-hoc test generation.
5. **Dev-Doc Triad Single Responsibility**: Maintain strict separation of concerns across artifacts:
   - `*-plan.md`: Canonical architectural design, current state, design decisions, and phase-level exit criteria (no granular execution tasks, checkboxes, dynamic status, or session handoffs).
   - `*-tasks.md`: The sole hot execution ledger (granular Red/Green task breakdown, checkboxes, dynamic status, and phase verification gates).
   - `*-context.md`: The sole active working memory (session progress, quick resume, blockers, validation baseline results, and dated session handoffs).

## Top Anti-Patterns
1. Memory-based planning, which turns assumptions about the repository into false implementation facts.
2. **Post-Hoc Test Tautology ("The Ugly Mirror")**, which writes code first and tests afterwards, producing self-fulfilling tests that mirror implementation bugs and mock away real failure modes.
3. Future-state-first planning, which designs changes before reporting what exists, what is missing, and what evidence supports those conclusions.
4. Verification sprawl, which wastes implementation time on per-task checks, multiple test commands, app startup, browser automation, Playwright, Chrome DevTools MCP, Aspire, Docker, or live-service smoke tests.
5. Stale checkbox debt, which postpones task updates until a separate refresh command and leaves completed implementation appearing unfinished.
6. **Dev-Doc Triad Bleed / Duplication**, which pollutes `plan.md` with granular task checklists (`- [ ]`), dynamic execution statuses (`IN PROGRESS`), or session handoffs, duplicating `tasks.md` and `context.md`.

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
classify intents -> run intake gate (load i-vsd and generate
islamic-value-sensitive-design/i-vsd-<task>.md; run grill-me on unresolved branches) ->
(if architectural fork: load robin-neutral) -> load rules/skills/docs ->
inspect related work -> verify code/tests/contracts -> report current state ->
design slices -> write and cross-check plan/context/tasks (linking I-VSD)
```

```text
Fast subtask verification (TUnit sliced):
dotnet run --project <one-relevant-project>.csproj --no-build -- --treenode-filter "/*/*/*<TargetTestClass>/*"

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

## OmO Prometheus Integration (Optional)

When working through Oh-My-OpenAgent (OpenCode plugin or Senpi), OmO's **Prometheus** agent can serve as an interactive intake interviewer for this skill's planning workflow:

1. Switch to Prometheus in OmO (`/agent prometheus` or agent selector → Prometheus).
2. Instruct Prometheus to follow the `implementation-plan` skill when interviewing and planning.
3. Prometheus conducts the interview (clarifying scope, edge cases, architectural decisions), consults Metis (gap analysis) and Momus (plan review), and writes the resulting plan.
4. **Output target**: Prometheus writes directly into `dev/active/<task>/<task>-plan.md`, `dev/active/<task>/<task>-context.md`, and `dev/active/<task>/<task>-tasks.md` — the canonical ISLAMU Event plan artifacts.
5. **Authority**: `dev/active/<task>/` remains the single source of truth. OmO's `.omo/plans/` is a runtime workspace that may hold OmO-internal state, but the authoritative plan lives in `dev/active/<task>/`.

This synergy combines Prometheus's structured interview capability (Metis gap analysis, Momus review) with the ISLAMU Event implementation-plan skill's domain-specific investigation workflow (I-VSD evaluation, `/grill-me` intake, Clean Architecture slicing, Test-First Invariant Sequencing).

## Verification Hooks
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- `git diff --check -- .agents/skills/implementation-plan dev/active`

## Related Skills
- [../i-vsd/SKILL.md](../i-vsd/SKILL.md)
- [../grill-me/SKILL.md](../grill-me/SKILL.md)
- [../robin-neutral/SKILL.md](../robin-neutral/SKILL.md)
- [../agentic-research/SKILL.md](../agentic-research/SKILL.md)
- [../senior-cto-feedback/SKILL.md](../senior-cto-feedback/SKILL.md)
- [../conventional-commit/SKILL.md](../conventional-commit/SKILL.md)
- [../clean-architecture-rules/SKILL.md](../clean-architecture-rules/SKILL.md)
- [../skill-authoring/SKILL.md](../skill-authoring/SKILL.md)

