<!-- ABOUTME: Completion and self-review gates for implementation-plan workstreams. -->
<!-- ABOUTME: Prevents unverified claims, vague tasks, artifact drift, and accidental implementation claims. -->

# Quality Gates

## Intake Gate (I-VSD & Grill-Me)

- `islamic-value-sensitive-design/i-vsd-<task-name>.md` exists, contains `Last Updated: YYYY-MM-DD`, and is linked from the task-owned plan, context, and tasks files.
- The I-VSD report follows `planning` mode, names its reviewed-input revision, is `current`, and has a `plan-aligned` disposition after the completed triad was revalidated.
- The I-VSD report traces applicable principles, stakeholders, provider-controlled decisions, evidence, stable finding/mitigation IDs, uncertainty, refresh triggers, and escalation boundaries.
- Every material `IVSD-*` ID maps in plan Section 9 to a named scenario/task, explicit non-applicability, or named escalation gate; the same report path/revision/status appears in plan, context, and tasks.
- Architectural, product, failure-mode, and edge-case ambiguities were first resolved from repository evidence, then decided through `grill-me` with recommendations rather than filled with assumptions.
- Any conditional `robin-neutral` technology or architecture comparison is recorded in plan Section 5 and remains separate from the I-VSD assessment.

## Evidence Gate

- Every existing path, project, symbol, route, contract, setting, and test named in current state was verified by search and relevant file reads.
- Missing items are labeled not found or new; they are never described as implemented.
- Evidence entries separate repository facts, source-derived constraints, decisions, assumptions, and unknowns.
- Current-state strengths and pain points both cite concrete evidence.
- Related active or paused work was checked for overlap and conflicts.

## Contract Gate

- Every relevant implementation intent was captured.
- **Change Classification** is explicitly declared (`Behavioral Delta` vs `Non-Behavioral Delta`).
- Intent docs, skills, rules, scope, tests, docs impact, acceptance criteria, and forbidden moves are reflected in the plan.
- The **Release & Changelog Strategy** is classified (Conventional Commit scopes, `docs/releases/changes/CHG-*.yaml` fragment requirement for high-impact/breaking changes, or explicit `Changelog: skip` trailers).
- Security, authorization, privacy, abuse, multi-tenancy, federation, localization, accessibility, product, observability, operations, and migration are each classified with rationale.
- Clean Architecture ownership and API/HAL/BFF trust boundaries are explicit where applicable.
- **Greenfield Breaking Change Freedom**: Backward-compatibility shims, deprecated route aliases, and legacy adapters are forbidden in pre-v1. Cleanly break and replace obsolete structures.

## Executability & Invariant-First Gate

- The plan reports current state before proposed future state.
- **Behavior vs. Code Separation**: Section 3 defines observable system behavior using RFC 2119 keywords (`SHALL`/`MUST`) and concrete `WHEN`/`THEN` scenarios without polluting behavioral contracts with internal class names or library choices.
- **Invariant-First Slicing**: High-criticality slices (money, concurrency races, aggregate state transitions, security boundaries) sequence authoring failing invariant tests (Task N.1: Red Phase) directly bound to named Section 3 Scenarios *before* implementing production logic (Task N.2: Green Phase). Standard feature/CQRS orchestration and UI slices implement directly and verify against public contracts.
- **Strict Quality Over Quantity in Tests**: Tests MUST NOT use mock-mirroring (`NSubstitute.Received(1)` on internal repositories/caches), framework-testing boilerplate (testing EF Core cancellation), or raw C#/CSS text scraping. Tests assert observable outcomes, state transitions, and RFC 7807 ProblemDetails.
- **Atomic Verification Criteria**: Every single task checkbox in `tasks.md` states its concrete verification assertion (`- [ ] N.M ... and verify ...`).
- **Strict Deferrable Open Questions**: Unknowns in Section 2.6 contain only genuinely deferrable details; non-deferrable branches were resolved during intake.
- Every task names exact files or a bounded investigation that will discover them.
- Every task includes observable acceptance criteria, dependencies, effort, and required guidance.
- Phases are reviewable slices with rollback or failure-diagnosis guidance.
- **Clean Architecture Slicing**: Verification strictly targets the touched layer (e.g., Blazor UI tests for UI changes; Application unit tests for CQRS changes). Never include irrelevant or cross-layer integration suites in unit-level slices.
- **Subtask Verification**: Subtasks specify targeted TUnit tree-node filtering (`--treenode-filter "/*/*/*<TestClass>/*"`) for active iteration rather than full-project or solution-wide test commands.
- Tests are specified against public contracts (MediatR requests, HTTP routes, ProblemDetails RFC 7807, database states) rather than private implementation details.
- Every phase ends with exactly one Release build and at most one fastest relevant non-browser project test.
- The plan contains no app startup, Playwright, browser automation, Chrome DevTools MCP, visual QA, E2E, Docker/Aspire startup, live-service smoke, or manual runtime walkthrough.

## Continuity Gate

- The stable task name and `Last Updated: YYYY-MM-DD Europe/Brussels` appear in all three files.
- Plan, context, and tasks agree on status, current priority, next action, blockers, task ids, decisions, risks, and verification.
- Plan, context, and tasks agree on I-VSD path, reviewed-input revision, status/disposition, CTO-review status, and user-approval status.
- **Dev-Doc Triad Single Responsibility**: `plan.md` defines architectural phase exit criteria without embedding granular task execution checklists, checkboxes (`- [ ]`), or session handoffs; `tasks.md` is the sole hot execution ledger; `context.md` is the sole session memory and handoff log.
- Context puts resume state and blockers near the top.
- Tasks are checkable and mirror the plan's phases.
- The implementation-agent contract makes `tasks.md` the hot ledger, requires substantial-task updates immediately and full reconciliation by phase end, and separates implementation completion from phase verification.
- Context and plan update triggers are narrow enough to prevent documentation churn, while a dated handoff remains mandatory before pause or transfer.
- Resume guidance reads context/tasks first and only relevant plan sections, avoiding repeated full-workstream rereads.

## Scope Gate

- Planning artifacts are the only product of this workflow.
- Runtime/application code was not changed.
- The response does not claim the feature or refactor was implemented.
- Unknowns that materially affect design are visible and assigned to investigation or user review.
- The workstream is ready for user correction/approval before implementation begins.
- No artificial test-only, documentation-only, QA-only, reporting-only, or verification-only phase was added.

## Verification

Run proportional repository checks for the planning artifact change:

```bash
git diff --check -- dev/active/<task-name>
```

If this workflow skill or other agent-context infrastructure changed, also run:

```bash
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
git diff --check -- .agents/skills .agents/contract tests/Event.Architecture.Tests
```

Run the planned implementation test suite only during implementation, not while producing the plan. Record known baseline failures honestly in context and tasks.

## Final Response

Use this shape:

```text
Created/updated implementation planning docs for `<task-name>`:
- dev/active/<task-name>/<task-name>-plan.md
- dev/active/<task-name>/<task-name>-context.md
- dev/active/<task-name>/<task-name>-tasks.md

Potential Risks & Unknowns:
<Short evidence-grounded paragraph naming the hardest unresolved area.>

Recommended next step:
<Specific section for user review, or the first approved implementation slice.>
```

Do not say implementation started. If re-baselining, summarize what materially changed in the planning artifacts.
