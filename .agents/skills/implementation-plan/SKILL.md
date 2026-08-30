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
- [../i-vsd/resources/integration-contract.md](../i-vsd/resources/integration-contract.md)
- [../grill-me/SKILL.md](../grill-me/SKILL.md)

## Top Invariants
1. Investigate and plan only; do not edit runtime code or claim implementation has started.
2. Follow I-VSD `planning` mode: reuse one shared repository evidence packet, create the draft `islamic-value-sensitive-design/i-vsd-<task-name>.md`, resolve material branches through `grill-me`, draft the triad, then revalidate its `IVSD-*` mappings before declaring it plan-aligned. The plan request satisfies the normal I-VSD agreement prompt but never suppresses necessary user questions.
3. Verify every claimed path, symbol, test, contract, and configuration key from repository evidence, then classify the work against every relevant intent and carry its docs, skills, rules, scope, tests, acceptance criteria, forbidden moves, and **Release & Changelog Strategy** into the plan.
4. **Behavior vs. Code Separation & Scenario Contract**: In `plan.md`, define externally observable behavior contracts using RFC 2119 keywords (`SHALL`/`MUST`) and concrete `WHEN`/`THEN` scenarios before designing code. Implementation details (classes, handlers, migrations) belong strictly in Section 5 Architecture. Classify changes as `Behavioral Delta` (requiring scenarios) vs `Non-Behavioral Delta` (pure refactor/tooling).
5. **Invariant-First Slicing & Quality Over Quantity**: Sequence failing invariant/specification tests (Red Phase) *before* production code (Green Phase) specifically for **Core Domain Invariants, Concurrency Races, State Machines, and Security Boundaries**. Standard CQRS commands/queries, API endpoints, and UI components do NOT require dogmatic Red/Green micro-task decomposition; implement them directly and verify via targeted contract/integration tests without boilerplate mock-mirroring (`NSubstitute.Received(1)`).
6. **Greenfield Breaking Change Freedom**: This platform is pre-release (0 users, 0 external adopters). Never plan backward-compatibility shims, deprecated route aliases, or legacy compatibility layers. Break and replace cleanly to achieve optimal architecture.
7. **Strict Deferrable Open Questions Gate**: Unknowns in `plan.md` Section 2.6 are strictly for genuinely deferrable details that will not alter scope, architectural patterns, or task breakdown. If an unknown would shift the task sequence, resolve it via `grill-me` before finalizing the plan.
8. **Dev-Doc Triad Single Responsibility**: Maintain strict separation of concerns across artifacts:
   - `*-plan.md`: Canonical architectural design, current state, design decisions, and phase-level exit criteria (no granular execution tasks, checkboxes, dynamic status, or session handoffs).
   - `*-tasks.md`: The sole hot execution ledger (granular Red/Green task breakdown, checkboxes with atomic verification criteria, dynamic status, and phase verification gates).
   - `*-context.md`: The sole active working memory (session progress, quick resume, blockers, validation baseline results, and dated session handoffs).

## Top Anti-Patterns
1. Memory-based planning, which turns assumptions about the repository into false implementation facts.
2. **Behavior/Implementation Conflation**, which describes code modifications instead of observable system behavior contracts and leaves requirements without testable `WHEN`/`THEN` scenarios.
3. **Non-Deferrable Open Questions Debt**, which postpones foundational scope or architectural decisions into open questions instead of resolving them during intake.
4. **Mock-Mirroring & Tautological Test Debt ("The Ugly Mirror")**, which writes unit tests that mock internal dependencies and assert method call counts (`Received(1)`), framework behavior (EF Core cancellation), or raw source/CSS strings instead of enforcing real domain invariants.
5. **Backward-Compatibility Hesitation**, which introduces deprecated endpoint aliases, adapter shims, or migration baggage in a greenfield project with zero external users.
6. Future-state-first planning, which designs changes before reporting what exists, what is missing, and what evidence supports those conclusions.
7. Verification sprawl, which wastes implementation time on per-task checks, multiple test commands, app startup, browser automation, Playwright, Chrome DevTools MCP, Aspire, Docker, or live-service smoke tests.
8. Stale checkbox debt, which postpones task updates until a separate refresh command and leaves completed implementation appearing unfinished.
9. **Dev-Doc Triad Bleed / Duplication**, which pollutes `plan.md` with granular task checklists (`- [ ]`), dynamic execution statuses (`IN PROGRESS`), or session handoffs, duplicating `tasks.md` and `context.md`.

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
classify intents -> load rules/skills/docs -> inspect related work and verify
current code/tests/contracts -> run I-VSD planning intake from the shared evidence
packet -> grill-me unresolved branches -> (if architectural fork: robin-neutral) ->
design and write plan/context/tasks -> revalidate I-VSD mappings -> cross-check
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

