<!-- ABOUTME: Required structure for repository-grounded implementation plan files. -->
<!-- ABOUTME: Preserves evidence, architecture decisions, executable phases, risks, and implementation-agent duties. -->

# Plan Template

Use this structure for `dev/active/<task-name>/<task-name>-plan.md`.

## Header

```markdown
# <Human Title> — Implementation Plan

Last Updated: YYYY-MM-DD Europe/Brussels
```

## 0. Planning Metadata

Record:

- original request and task directory;
- planning status: Draft, User-reviewed, Approved, In implementation, or Re-baselined;
- matched intents, relevant skills, and relevant rules;
- primary layers touched;
- S/M/L/XL complexity with evidence-based rationale;
- **I-VSD Document:** `[islamic-value-sensitive-design/i-vsd-<task-name>.md](../../../islamic-value-sensitive-design/i-vsd-<task-name>.md)`;
- **Grill-Me Intake:** a concise resolved-decisions summary, including recommendations accepted or rejected and any explicitly deferred branch.

## 1. Executive Summary

State what will change, why it matters, the intended user/business/platform outcome, and explicit non-goals.

## 2. Source-Grounded Current State Report

### 2.0 Pre-Flight Structural Context (Blast Radius)

Inject bounded Turn 1 knowledge-graph impact slice:

```yaml
# Injected Structural Context (Pre-Flight Blast Radius)
Target: <Namespace.ClassName.MethodName>
Callers (Upstream):
  - <Controller.Action> (Route: <route_template>)
  - <Blazor.Component.Handler>
Callees (Downstream):
  - <Repository.Method>
  - <Outbox.EnqueueAsync> (Event: <DomainEventName>)
Impacted Flows:
  - Flow: <BusinessFlowName> (Criticality: <Tier>)
Test Coverage:
  - <PathToUnitTests>
  - <PathToIntegrationTests>
```

### 2.1 Evidence Log

Use a table with `Claim`, `Evidence`, `Confidence`, and `Notes`.

### 2.2 Existing Implementation

Describe verified behavior by owning layer.

### 2.3 Existing Tests And Verification Coverage

Name verified test projects/files, protected behavior, and explicit gaps.

### 2.4 Existing Documentation And Contracts

List relevant docs, API/OpenAPI contracts, generated clients, configuration, policies, schemas, and runbooks.

### 2.5 Current Pain Points / Improvement Areas

Tie concrete correctness, security, UX, accessibility, performance, maintenance, duplication, and test gaps to evidence.

### 2.6 Unknowns After Investigation

For each unknown, record what was searched and the implementation task that will resolve it.

## 3. Proposed Future State

Describe target ownership, behavior, user/developer/operator experience, and important control/data flows.

## 4. Non-Negotiable Constraints

Reference the matched contract and list only task-relevant repository, security, tenant, architecture, API, UI, compatibility, and documentation constraints.

## 5. Architecture And Design Decisions

For each decision include:

- **Decision**
- **Why**
- **Alternatives considered**
- **Consequences**
- **Files/layers affected**

When the decision is a major technology selection, external library, or competing architectural design, run `robin-neutral` steelmanning first. Record the strongest viable alternatives, their trade-offs, and why each rejected approach lost; do not mix this technical comparison into the I-VSD analysis.

## 6. Implementation Phases

Use reviewable architectural slices. Every phase in `plan.md` defines high-level architectural scope, dependencies, relevant files/layers, phase exit criteria, the single phase-end verification command, and rollback handling.

```markdown
### Phase N: <Name>
- **Goal:**
- **Depends on:**
- **Relevant files:** existing/new status included
- **Related skills/rules:**
- **Acceptance criteria:** observable architectural and contract outcomes (bullet list, not execution checkboxes)
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project <one-relevant-project>.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:**
```

> [!IMPORTANT]
> **Single Responsibility Rule (`plan.md` vs `tasks.md`)**:
> - `plan.md` defines **architectural phases, goals, component files, phase-level acceptance criteria, verification commands, and rollback strategy**.
> - Do **NOT** include granular `#### Task N.M:` execution blocks, task descriptions/effort, or `[ ]` / `[x]` checkboxes in `plan.md`.
> - All granular task breakdowns (`Task N.M`), Red/Green/Refactor task sequences, and actionable execution checklists belong **strictly in `tasks.md`**.
> - Ephemeral session progress, worktree dirty scopes, test pass counts, and handoffs belong **strictly in `context.md`**.

#### Behavioral Slice Rule: Test-First Invariant Task Sequencing (in `tasks.md`)
To prevent **Post-Hoc Test Tautology ("The Ugly Mirror")** where agents write code first and generate self-fulfilling tests that mirror implementation bugs, every phase introducing or modifying behavioral logic MUST break down its actionable tasks in **`tasks.md`** in **Test-First Invariant order**:

1. **Task N.1 (Red Phase): Author Invariant & Contract Specification Tests**
   - Author failing tests against public interfaces, MediatR requests, or API contracts *before* implementing production logic.
   - Assert domain invariants, checked integer arithmetic, state transitions, fail-closed error responses (RFC 7807), tenant boundary isolation, and concurrency/race conditions.
   - Run or verify that the test fails for the expected missing capability (red anchor).
2. **Task N.2 (Green Phase): Implement Handlers, Entities & Domain Logic**
   - Author production C# code strictly to satisfy the test specifications.
3. **Task N.3 (Refactor & Wire Up): Clean Architecture & Registration**
   - Refactor for performance, memory allocation, and zero-PII logging (`StarRedactor`/`HmacRedactor`), and wire DI service registrations.

Do not create standalone manual-QA, documentation-review, reporting, dev-doc maintenance, or redundant verification tasks. Run no build or test command until the phase implementation is complete.

#### Final Phase Closing Rule: Changelog & Commit as the Final Task (in `tasks.md`)
Every implementation workstream MUST sequence its **Changelog Contribution & Commit Composition** as the **FINAL task of the FINAL phase in `tasks.md`** (e.g. `Task N.Last: Changelog Contribution & Final Commit Composition`). This ensures that:
1. All functional implementation across layers is 100% complete.
2. All tests are green and verified.
3. All relevant documentation (docs, schemas, runbooks) has been updated.
4. Only then is the appropriate changelog artifact created (`docs/releases/changes/CHG-YYYY-NNNN.yaml` for Tier 2) and the final Conventional Commit composed.

## 7. Testing Strategy

Keep this section short and high-leverage. Every implementation plan must define:
1. **Test-First Invariant Anchors**: Detail which test project (e.g., `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.API.IntegrationTests`, `Event.Persistence.IntegrationTests`) hosts the Red-phase specification tests.
2. **High-Leverage Adversarial Scenarios**: Prioritize high-value invariant tests (concurrency races, state machine exhaustiveness, real DB transaction boundaries, zero-PII log sinks, tenant isolation) over shallow mock-heavy boilerplate tests.
3. **Phase Verification Lane**: Assign exactly one fastest relevant non-browser test project to each phase, never repeat a project without a concrete reason, and never schedule more than one `dotnet test` command in a phase. Do not plan E2E, Playwright, browser automation, Chrome DevTools MCP, visual QA, live-app smoke, Aspire/Docker startup, or manual runtime verification.

Record additional intent-mandated projects as contract requirements, then distribute them across existing phases where possible; do not create artificial test-only phases.

## 8. Documentation, Configuration, And Operations Impact

Name the exact docs, schemas, generated artifacts, settings, environment variables, Aspire/Compose files, deployment material, and runbooks to update or state why none apply.

### 8.1 Release & Changelog Strategy (Procedural Contribution)

Every implementation plan MUST classify its procedural changelog approach across the 3-tier release model (executed in the plan's final closing task):

1. **Tier 1 — Standard Feature or Fix (Conventional Commits):**
   - Use public capability scopes from `eng/release/policy/scope-registry.yaml` (e.g. `feat(event): ...`, `fix(auth): ...`).
   - The release engine automatically aggregates these into `What's Changed` at release time.
2. **Tier 2 — High-Impact / Breaking / Migration / Security / Operator Impact (Change Fragment):**
   - The plan's final phase MUST include a task creating an append-only change fragment under `docs/releases/changes/CHG-YYYY-NNNN.yaml`.
   - The task acceptance criteria must enforce valid YAML structure, `ReleaseInputPolicy` validation, and terminal commit footer `Change-Id: CHG-YYYY-NNNN` (plus `BREAKING CHANGE:` where applicable).
3. **Tier 3 — Internal Architecture / DevOps / Refactoring (Explicit Skip):**
   - The plan must specify terminal trailers: `Changelog: skip` and `Changelog-Reason: <clear-reason>` to prevent internal plumbing noise from leaking into public release notes.

## 9. Islamic Value-Sensitive Design (I-VSD) & Moral Boundaries

Link the mapped `[I-VSD report](../../../islamic-value-sensitive-design/i-vsd-<task-name>.md)` and trace each applicable provider-controlled decision from principle and affected stakeholder to risk, mitigation, evidence, uncertainty, and implementation task. State scholarly escalation needs and non-applicable domains explicitly. The same report link must appear in the task-owned context and task ledger.

## 10. Security, Authorization, Privacy, And Abuse Considerations

Cover trust boundaries, authentication, server-side authorization, tenant isolation, HAL affordances, rate limiting, idempotency, auditability, privacy, sensitive-data handling, and abuse controls where relevant.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

Mark every concern Applicable, Not Applicable, or Needs Investigation and explain the classification.

## 12. Observability And Operations

Plan bounded logs, metrics, traces, health/readiness, troubleshooting, operator-visible failure modes, and recovery where relevant.

## 13. Migration And Compatibility Plan

Cover database/schema/data migration, seed changes, generated contracts, deployment order, rollback/reset, and breaking-change notes. Do not add compatibility shims unless explicitly approved.

## 14. Risk Register

Use a table with `Risk`, `Likelihood`, `Impact`, `Mitigation`, `Detection Signal`, and `Owner/Task`.

## 15. Success Metrics And Definition Of Done

Define observable functional success. For each phase, the automated gate is only one Release build plus at most one selected project test; do not add separate browser, runtime, manual-QA, migration-command, documentation-check, or operator-smoke gates.

## 16. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Require future implementation agents to:

1. At first implementation start or cold resume, read task-owned context and the current task first, then retrieve only the plan heading needed for the current phase or changed decision; never preload all three artifacts.
2. Keep a `path + heading/symbol + revision` ledger. During an uninterrupted session, do not reread unchanged plan/context/tasks; reopen only an invalidated exact section.
3. Start from the highest-priority unchecked task unless the user overrides it.
4. Treat `tasks.md` as the hot execution ledger: check a substantial task immediately after its implementation acceptance criteria are met, and reconcile smaller completed tasks together no later than phase end.
5. Keep implementation-task and phase-verification checkboxes separate; a task may be checked when its implementation is complete, but the phase is complete only after its build and selected test checkboxes pass.
6. Update the task status summary, completed count, current priority, next recommended slice, discovered tasks, deferred work, and `Last Updated` whenever task state changes.
7. Update context after a completed phase, meaningful decision, blocker, failed validation, material discovery, or before pause/compaction/transfer; do not rewrite it for trivial edits.
8. Update the plan only when scope, architecture, phase order, acceptance criteria, risks, or validation strategy changes; do not churn it for ordinary progress.
9. Record failed validation with the known cause and next recovery action in tasks/context without marking the phase complete.
10. Before pausing, compaction, transfer, or PR creation, reconcile the affected tasks, add a concise dated handoff, and identify unrelated dirty files that the next contributor must avoid.
11. Run phase verification only after all phase tasks, with one Release build and at most one selected project test; do not repeat successful commands or start the application/browser.
12. Never report completion when repository reality and the task ledger disagree.

Require every implementation summary to teach:

- what changed and why;
- architecture/design patterns, libraries, infrastructure, protocols, and project abstractions used;
- important files, classes, handlers, services, and components with their responsibilities;
- data/control flow;
- relevant repository conventions and reliability/security practices;
- verification performed, remaining work, next work, and dev-doc update status.

## 17. Progress Reporting Contract

Require this response shape after each implementation slice:

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason
```

For completed implementation work, `Docs updated` must confirm that `tasks.md` was reconciled. Report context and plan separately as updated or unchanged because no trigger occurred.

## 18. Potential Risks & Unknowns

End with a candid, specific critique of the part most likely to fail, expand, or require a decision.
