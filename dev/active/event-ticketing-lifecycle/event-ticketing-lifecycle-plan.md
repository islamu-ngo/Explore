<!-- ABOUTME: Re-baselined implementation plan for the post-Phase-21 Event Ticketing Lifecycle workstream. -->
<!-- ABOUTME: Defines invariant-first behavior, architecture remediation, PR boundaries, recovery, and release gates. -->

# Event Ticketing Lifecycle — Implementation Plan

Last Updated: 2026-08-28 Europe/Brussels

## 0. Planning Metadata

- **Original request:** move all unimplemented Registration Data Collection Phase 22+ work into a successor workstream.
- **Hardening request:** strengthen architecture, conventions, maintainability, technical-debt remediation, code comments, and durable `docs/` ownership without backward-compatibility constraints.
- **Task directory:** `dev/active/event-ticketing-lifecycle/`
- **Planning status:** Approved implementation scope partially delivered and merged into `develop`. Phases 0–5 and Phase 6 Tasks 6.1–6.7 are implemented; Phase 6 evidence closeout plus Phases 7–9 remain open.
- **Execution-ledger status:** implementation presence is recorded separately from task completion. No Phase 0–6 task checkbox is closed without its exact acceptance evidence; historical RED chronology is currently unproven.
- **Change classification:** Behavioral Delta.
- **Matched intent:** `registration-data-collection`.
- **Criticality:** Tier 0 Sovereign for money/orders; Tier 1 Security for tenancy, capabilities, migrations, and authorization; Tier 2 Privacy for participant/contact/consent data.
- **Complexity rationale:** ten architectural phases cross Domain, Application, Persistence, Infrastructure, API/HAL/OpenAPI, BFF/Blazor, generated contracts, privacy/security, and operator recovery; payment/capacity/credential races require real relational evidence.
- **Primary layers:** Domain, Application, Persistence, Infrastructure, API/HAL, BFF, Blazor Client, generated contracts, operations/docs.
- **Matched skills/rules:** criticality-guardrail, clean-architecture-rules, CQRS/MediatR, EF Core, outbox, auth/BFF/HAL, Blazor/accessibility, error tracking, payments-commerce, privacy/PII, scheduling, tests, IP clean-room.
- **Grill-Me decisions:** hard purchaser ceilings require stable authority; name-only limits remain honest; seller withdrawal fails closed without full commercial equivalence; pre-restore bearer authority rotates; protected delayed payout stays absent.
- **Implementation shape:** the implemented purchase, readiness, transfer, and fair-return slices were integrated through the ticketing branch and follow-up repair commits. Remaining work must preserve the dependency order from Phase 6 closeout through `REL`.
- **Compatibility posture:** development-mode direct replacement. Do not add compatibility shims, dual authority, obsolete routes, stale DTOs, or tests that preserve superseded behavior.
- **I-VSD report:** [`i-vsd-event-ticketing-lifecycle.md`](../../../islamic-value-sensitive-design/i-vsd-event-ticketing-lifecycle.md), whose authoritative metadata binds the exact plan/tasks revisions reviewed.
- **I-VSD status:** `current / plan-aligned`.
- **Clean-room evidence:** [`event-ticketing-lifecycle-clean-room-evidence.md`](event-ticketing-lifecycle-clean-room-evidence.md).
- **Hot execution ledger:** [`event-ticketing-lifecycle-tasks.md`](event-ticketing-lifecycle-tasks.md).
- **Working context:** [`event-ticketing-lifecycle-context.md`](event-ticketing-lifecycle-context.md).

## 1. Executive Summary

This workstream finishes the ticketing lifecycle after the forms-focused predecessor stopped at implemented Phase 21. It adds:

1. access-mode-aware purchase governance;
2. participant-owned completion, consent, approval, and admission readiness;
3. transfer and credential rotation without resale;
4. explainable waitlist and fair-return allocation;
5. optional event-bound add-ons isolated from admission;
6. authoritative recovery, operator controls, and measurable self-hosting evidence; and
7. a machine-readable deployment boundary that keeps protected delayed payout absent.

The first increment does not add features. It remediates lifecycle authority debt before new states are introduced:

- normal state transitions move behind semantic aggregate methods;
- commands, workers, and HAL consume one domain decision surface;
- persistence exposes transaction-bound primitives instead of becoming business-state authority;
- additions to oversized lifecycle/repository seams are frozen in favor of capability-specific coordinators; and
- architecture tests prevent direct state mutation and duplicated eligibility logic from returning.

The implementation uses project-native Clean Architecture, CQRS/MediatR, EF Core, transactional outbox, Quartz one-pass jobs, HAL affordances, BFF token isolation, generated NSwag clients, and deterministic TUnit tests. It does **not** introduce a generic workflow engine, generic repository, event sourcing, distributed transaction, second scheduler, or compatibility layer.

## 2. Source-Grounded Current State Report

### 2.1 Verified Repository Reality

| Concern | Verified current authority |
|---|---|
| Architecture | `Explore.Domain` → `Explore.Application` → Persistence/Infrastructure → API/BFF/Blazor |
| Order/hold/payment | `RegistrationOrder`, `RegistrationInventoryHold`, `PaymentAttempt`, repositories, reconciliation services, and real PostgreSQL tests exist |
| Concurrency | UUIDv7 concurrency stamps, ordered PostgreSQL `FOR UPDATE` locks, retry-aware `IUnitOfWork`, durable lease/fence patterns |
| External effects | transactional outbox and specialized durable effect/reconciliation rows |
| Scheduling | Quartz is the one scheduling authority; jobs are one-pass wrappers over testable services |
| Admission | opaque credential, append-only check-in facts, recovery flows, and fixed-cardinality telemetry exist |
| Authorization | writes authorize server-side; HAL links are UI affordances, not mutation authority |
| Client boundary | browser tokens remain in the BFF; generated clients are generator-owned |
| Multi-tenancy | central tenant resolution and named EF filters; bypasses require an explicit reason |
| Implemented successor capabilities | purchase governance, participant admission readiness, credential-rotating transfer, fair-return waitlist/allocation, durable orchestration, API/HAL/BFF/Blazor surfaces, and generated contracts are merged in `develop` |
| Remaining successor capabilities | event-bound add-ons, lifecycle recovery/operator controls, deployment capability matrix, and final contract/release convergence do not yet exist |

### 2.2 Remaining Technical Debt And Open Scope

- Phase 0 consolidated touched lifecycle authority behind semantic aggregate rules and capability-specific coordinators; architecture ratchets now protect those seams.
- Purchase, readiness, transfer, and fair-return behavior is merged with tenant-scoped persistence, HAL/BFF/generated-client boundaries, and critical evidence for Phases 0, 1, 3, and 5.
- All Phase 0–6 task checkboxes remain evidence-open under the ledger's exact acceptance contract; Task 6.8 additionally lacks the complete Phase 6 mutation/evidence manifest required by this plan.
- The post-merge task audit found no retained pre-GREEN assertion-failure transcript for the Phase 0–6 RED tasks and incomplete exact command/report artifacts for multiple GREEN/closeout tasks. Current passing regression tests must not be relabeled as historical RED evidence.
- Full `Event.Architecture.Tests`, `Event.API.IntegrationTests`, and `Event.Persistence.IntegrationTests` phase-closeout evidence is not recorded as green. Ticketing-focused selectors passed, while the architecture project retained 12 inherited non-ticketing failures.
- Event-bound add-ons, recovery/operator controls, the deployment capability matrix, and final release convergence remain unimplemented.
- A live API curl walkthrough is not recorded because the existing fail-closed privacy-erasure replay gate blocked local startup before the HTTP listener opened. The gate was not weakened; focused HTTP behavior passed through the repository integration host.

### 2.3 Externally Verified Functional Constraints

Official documentation was used only for source-free behavioral constraints:

- Stripe webhook deliveries can retry, duplicate, and arrive out of order; provider I/O therefore cannot be transaction truth.
- EF Core optimistic concurrency requires explicit conflict handling; manually controlled transactions must account for execution strategies and savepoints.
- PostgreSQL serializable transactions can abort and retry, while explicit locks require a consistent acquisition order to reduce deadlocks.
- OWASP transaction authorization requires server-side, operation-specific authorization and protection against request manipulation.
- ASP.NET Core Data Protection key lifecycle makes retained key availability part of recovery.
- WCAG 2.2 dynamic status and error feedback require programmatic announcement without unnecessary focus movement.

The source register is in the clean-room evidence packet. No third-party code, schema, tests, comments, or expressive design was ingested.

### 2.4 Remaining Unknowns That Must Be Resolved Inside Named Tasks

- Phase 6 closeout must determine and retain the missing mutation-summary/evidence artifacts without reopening already merged lifecycle behavior.
- Add-on, recovery, and deployment-boundary tasks still require their bounded pre-edit discovery; their owning layers and behavioral contracts remain fixed here.
- Deployment-specific legal, scholarly, provider, accessibility, privacy, security, and operator evidence may disable or narrow release; it cannot widen scope.
- Protected delayed payout remains outside this workstream regardless of implementation progress.

## 3. Proposed Future State: Behavioral Contract And Scenarios

### Requirement R1 — Honest Access And Purchase Governance

The system MUST support authenticated accounts, verified guests, and configured name-only access. It MUST pin accepted terms, access mode, actor/context, policy lineage, and the exact server-owned enforcement dimension.

Hard cross-order purchaser ceilings apply only to stable authority:

- authenticated account identity;
- verified normalized-contact identity; and
- server-proven organization/group authority plus the acting account.

Name-only access receives per-order, capacity, and abuse/rate controls only. Product copy and operator documentation MUST NOT claim a hard per-person ceiling for name-only access.

#### Scenario S1-A — Valid Group Purchase

- **GIVEN** an authenticated purchaser is authorized to act for a group
- **WHEN** checkout reserves/finalizes quantity within instance 5, tenant 4, and event 6 ceilings
- **THEN** effective ceiling 4 is pinned and consumed once for the server-proven actor dimension

#### Scenario S1-B — Context-Switch Ceiling Race

- **GIVEN** concurrent personal/group requests resolve to the same stable controlling authority
- **WHEN** their combined quantity would exceed the pinned hard ceiling
- **THEN** only the allowed quantity commits and no capacity, order, payment, or outbox effect exceeds it

Unrelated group members MUST NOT be collapsed merely because they share a group.

#### Scenario S1-C — Durable Operation Identity

- **GIVEN** the HTTP idempotency cache expires, a request is retried after restore, or the same key is reused with a different tenant/principal/route/body/capability
- **WHEN** a lifecycle command reaches the application boundary
- **THEN** durable business-operation uniqueness prevents duplicate money/authority, legitimate scope changes conflict, and tenants remain independent

### Requirement R2 — Subject-Correct Completion, Consent, Approval, And Admission

Order-level and participant-level requirements MUST remain distinct. Purchaser-supplied data for another adult MUST NOT become that adult's consent truth. Active admission authority MUST require all current participant facts, consent, and approval.

#### Scenario S2-A — Post-Purchase Participant Completion

- **GIVEN** a paid/free order exists with incomplete participant requirements
- **WHEN** the subject completes valid pinned requirements
- **THEN** typed answers are persisted for that subject and admission eligibility changes through the shared decision surface

#### Scenario S2-B — Payment Outruns Admission Readiness

- **GIVEN** payment succeeds while participant facts, consent, or approval remain incomplete
- **WHEN** issuance/check-in evaluates the ticket
- **THEN** payment remains successful but active credential/check-in remains denied with a bounded, dignified status

#### Scenario S2-C — Independent Adult Consent

- **GIVEN** a purchaser supplied provisional data for another adult
- **WHEN** that adult claims/completes the ticket
- **THEN** fresh subject-owned data and consent replace provisional facts; purchaser consent is never copied

### Requirement R3 — Transfer Without Resale

Transfer MUST change future holder/admission authority without changing purchaser, merchant, currency, billing, payment, refund, or append-only check-in truth. No money moves between attendees.

#### Scenario S3-A — Recipient Claim

- **GIVEN** a future, transferable, unchecked ticket and valid single-use claim capability
- **WHEN** the recipient completes required facts and accepts before expiry
- **THEN** holder authority changes once, the old credential/capability is revoked, a new credential is issued, and notifications are recorded post-commit

#### Scenario S3-B — Transfer Versus Check-In

- **GIVEN** acceptance and old-credential check-in race
- **WHEN** both reach the shared ticket/eligibility fence
- **THEN** exactly one terminal authority wins and the old credential never remains valid after transfer

#### Scenario S3-C — Consent Or Approval Revocation Race

- **GIVEN** consent withdrawal or approval revocation races transfer acceptance, issuance, correction, reissue, and check-in
- **WHEN** contenders reach the shared fence
- **THEN** no future active credential survives a winning revocation, while already-committed append-only check-in truth is preserved

### Requirement R4 — Explainable Waitlist And Fair Return

Waitlist ordering MUST be deterministic, policy-versioned, tenant-qualified, privacy-minimized, and exact-supply compatible. Paid priority is forbidden.

#### Scenario S4-A — Released Ticket Reallocation

- **GIVEN** a compatible entitlement is authoritatively released
- **WHEN** the queue selects by pinned priority, enqueue time, and stable tie-break ID
- **THEN** one offer is created, one existing hold is reused, and only bounded position/reason is exposed

#### Scenario S4-B — No Double Capacity Or Premature Refund

- **GIVEN** allocation, checkout, expiry, and refund race
- **WHEN** they contend
- **THEN** one entitlement exists and no original-holder refund starts before replacement payment is reconciled

#### Scenario S4-C — Seller Withdrawal With Compatible Supply

- **GIVEN** a buyer is already attached to seller-originated supply and a commercially equivalent source exists
- **WHEN** the seller withdraws before provider handoff
- **THEN** source binding changes atomically without repricing, restarting, or altering buyer snapshots

#### Scenario S4-D — Last Compatible Supply Is In Flight

- **GIVEN** no compatible substitute exists or payment is handed off/ambiguous
- **WHEN** the seller attempts withdrawal
- **THEN** the sale remains privately conflicted and supply is not released or resold

#### Scenario S4-E — Payment Handoff Versus Withdrawal

- **GIVEN** dispatch is durably claimed while withdrawal, hold expiry, substitute allocation, and checkout contend
- **WHEN** the canonical lock order is applied
- **THEN** either pre-handoff rebinding commits before dispatch or handoff wins and the original/compatible supply remains bound; late success can never create a second buyer

#### Scenario S4-F — Replacement Success Versus Refund Crash

- **GIVEN** replacement payment settles and the process crashes before or after refund-intent creation
- **WHEN** duplicate, stale, or contradictory provider observations replay
- **THEN** payment settlement and one unique original-holder refund intent converge without duplicate refund or capacity release

Commercial equivalence means same tenant, event, ticket type, catalog/policy lineage, currency, accepted commercial terms, admission entitlement, gross minor-unit amount, and refund-funding compatibility.

### Requirement R5 — Optional Event-Bound Add-Ons

Add-ons MUST be separately cataloged, priced, inventoried, fulfilled, refunded, and reported. They MUST NOT create, revoke, or alter admission.

#### Scenario S5-A — Mixed Ticket And Add-On Order

- **GIVEN** compatible optional add-ons
- **WHEN** checkout is calculated
- **THEN** immutable line snapshots and independently testable totals are disclosed before provider handoff

#### Scenario S5-B — Refund Crosses Admission Boundary

- **GIVEN** an add-on is cancelled/refunded
- **WHEN** fulfillment/refund settles
- **THEN** admission remains unchanged and allocations sum exactly to the refunded total

#### Scenario S5-C — Checked Minor-Unit Arithmetic

- **GIVEN** multiplication/addition/allocation would exceed `long` range or produce a rounding remainder
- **WHEN** the command is evaluated
- **THEN** overflow fails before persistence/provider effects; valid allocations are non-negative and conserve captured/refunded minor units exactly

### Requirement R6 — Authoritative Recovery And Deployment Truth

Recovery MUST restore application state, keys, fences, idempotency identities, inbox/outbox, Quartz state, provider cursors, and authority in a tested order. Runtime MUST remain in recovery mode until validation succeeds.

#### Scenario S6-A — Consistency-Manifest Restore

- **GIVEN** a signed/hash-bound recovery manifest for the supported reference topology
- **WHEN** clean storage is restored
- **THEN** release/schema revisions, database checkpoint, object cutoff, retained key inventory, authority floor, and provider cursors reconcile before writes reopen

#### Scenario S6-B — Missing Key, Fence, Or Cursor

- **GIVEN** required key version, fence, business idempotency identity, or provider cursor is missing/inconsistent
- **WHEN** startup validates recovery
- **THEN** writes/workers remain closed and operators receive a bounded non-PII recovery state

#### Scenario S6-C — Point-In-Time Bearer Resurrection

- **GIVEN** a backup predates credential/capability revocation
- **WHEN** it is restored
- **THEN** every pre-restore transfer/waitlist/recovery capability is invalidated and active admission credentials are rotated/reissued before reopening; stale workers/fences lose

The supported reference deployment target is RPO ≤15 minutes and RTO ≤60 minutes. Self-hosters MUST publish their declared target and timed evidence; they MUST NOT claim production-ready status without meeting it.

### Requirement R7 — Protected Delayed Payout Remains Absent

Protected delayed payout MUST have no route, HAL relation, generated client method, scheduler job, configuration key, claim, secret, UI, or deployment status unless a separate I-VSD/ADR/workstream and all named approvals exist.

#### Scenario S7-A — Capability-Manifest Ratchet

- **GIVEN** the machine-readable deployment capability matrix
- **WHEN** architecture/release validation runs
- **THEN** protected delayed payout is absent/disabled and any accidental surface fails the build

### The Worst Break — WB-1 Paid Entitlement Split-Brain

- **GIVEN** one commercially compatible entitlement, a durably claimed payment dispatch, concurrent seller withdrawal/hold expiry, public reallocation, crash/restore, and late provider success
- **WHEN** two replicas resume against restored queues and stale fences
- **THEN** at most one buyer owns admission authority, at most one capture-linked settlement/refund chain exists, no released supply is resold while ambiguous, and the system remains stop-sale/recovery-only until authoritative reconciliation

WB-1 is the release-blocking invariant. No lower-level green result can compensate for failure here.

## 4. Non-Negotiable Constraints

1. Domain entities own explicit semantic transitions; no new direct status assignment or persistence-level lifecycle authority.
2. Application handlers/services own orchestration and use manually instantiated validators.
3. Repositories return entities, never DTOs or `IQueryable`.
4. `IUnitOfWork.ExecuteSerializableAsync` owns retry-aware outer transactions; persistence lock primitives require that transaction and never open a competing one.
5. Allocate retry-stable IDs/timestamps before transaction delegates.
6. Canonical lock order: tenant/event/ticket type → capacity pools ordered by ID → compatible supply/release rows ordered by priority/ID → buyer binding/order → payment attempt → refund operation.
7. Provider/network I/O never occurs inside a database transaction.
8. Money uses checked integer minor units and repository `MinorUnitMath`; no floating-point or duplicated arithmetic.
9. Durable business idempotency outlives HTTP middleware retention.
10. Every unique constraint, lock, claim, and lookup is tenant-qualified; background bypasses use named reasons and explicit tenant facts.
11. Outbox/effect rows commit with the winning transition. Dispatch is at-least-once and consumers are idempotent.
12. Quartz only wakes one-pass services; durable queue/lease/fence/retry/poison state remains below the scheduler.
13. HAL `_links` gates client affordances only. API handlers/policies remain the authorization and lifecycle authority.
14. Browser clients use the BFF/generated client boundary; bearer tokens never enter browser code.
15. All capability failures are generic, private/no-store, timing-bounded, and zero-PII.
16. EF migrations/snapshots, OpenAPI, and NSwag clients are generated artifacts and are never hand-edited.
17. Secrets originate only from Infisical or `.env`, with schema in `.env.example`.
18. Every new file starts with two `ABOUTME:` lines.
19. Comments explain non-obvious invariants, lock order, authority, retry/recovery, and privacy decisions—not syntax or obvious control flow.
20. No third-party source/code/schema/tests/assets and no new dependency without outbound-license proof.

## 5. Architecture And Design Decisions

### Decision A — Existing Bounded Context, New Workstream

Keep runtime ownership in the existing registration/ticketing context. The split is planning/release ownership, not a new service or project.

### Decision B — Semantic Lifecycle Authority Before Features

Phase 0 introduces a reusable **domain decision surface**, not a generic engine. Commands, workers, and HAL ask the same domain policy/aggregate whether a transition/affordance is valid. Persistence executes only transaction-bound storage primitives.

Do not add new behavior to oversized lifecycle/repository classes. Capability-specific coordinators remain small and are placed in the owning layer. Architecture tests ratchet this boundary.

### Decision C — Mandatory PR Train, Not HAL-Based Activation

Core PRs are inert because they expose no controller, public command registration, scheduler registration, or UI—not because HAL links are absent. Once an API command ships, server authorization/lifecycle rules secure it. HAL omission alone is never rollback.

Generated contracts ship with their API PR. BFF/UI follows in a separate dependent PR.

### Decision D — Explicit State Machines And Versioned Policy

Use project-native entities, semantic value types, closed enums, transition methods, and immutable policy/commercial snapshots. Do not introduce a workflow framework, rules engine, base lifecycle class, or generic repository.

### Decision E — Database-Authoritative One-Winner Transitions

High-risk transitions use semantic aggregate rules, optimistic concurrency, tenant-qualified uniqueness, deterministic row locks, serializable transaction retry, and independently tested loser rollback.

### Decision F — Durable Process Managers, Outbox, And Quartz

Long-running provider/recovery workflows use explicit durable effect/process state with monotonic transitions, stable operation IDs, leases, fences, retry/dead-letter/unknown states, and operator resolution. Quartz is pointer-only scheduling. Notifications/refund intents commit atomically with business truth.

### Decision G — Scoped Capabilities And Durable Idempotency

Capabilities bind purpose, tenant, resource, subject/actor where applicable, generation, expiry, and single-use state. Plaintext and digests never enter logs/metrics/public errors. Domain uniqueness—not a 24-hour HTTP cache—is the final replay defense.

### Decision H — Buyer Continuity Requires Full Commercial Equivalence

Supply-source rebinding is invisible only when every commercial/admission/refund dimension in R4 is equal. Otherwise withdrawal fails closed; the plan does not silently reprice, restart payment, or substitute a weaker entitlement.

### Decision I — Recovery Mode And Mandatory Bearer Rotation

Restore starts under deployment-level maintenance/stop-sale independent of restored rows. Signed webhook intake and reconciliation can run while new sales, transfers, waitlist allocation, add-on fulfillment, generic replay, and BFF mutation exposure remain closed. Pre-restore bearer authority rotates before reopen.

### Decision J — Protected Payout Is Excluded, Not Feature-Flagged

No dormant implementation, disabled route, configuration stub, secret coordinate, or generated client surface is allowed.

### Decision K — Direct Replacement And Debt Ratchets

Delete obsolete paths, DTOs, routes, duplicated decisions, tests, and docs in the same PR that replaces them. Keep immutable historical audit/payment/check-in facts. Never preserve dead behavior “for compatibility.”

## 6. Mandatory PR Dependency Graph And Phase Boundaries

```text
FND ──> PUR-CORE ──> PUR-APP ──> PUR-API ──> PUR-UI
                     ├─> RDY-CORE ──> RDY-APP ──> RDY-API ──> RDY-UI ──> TRN-CORE ──> TRN-API ──> TRN-UI
                     └─> ADD-CORE ──> ADD-API ──> ADD-UI

PUR-APP + TRN-UI ──> WAI-CORE ──> WAI-ORCH ──> WAI-API ──> WAI-UI

PUR-UI + RDY-UI + TRN-UI + WAI-UI + ADD-UI ──> REC-CORE ──> REC-OPS ──> REL
```

Every PR node owns its Red tests, smallest Green implementation, refactor/debt deletion, generated artifacts, affected docs, and task-level targeted evidence. Phase closeout runs exactly one Release build and at most one selected non-browser project test. No QA-only, docs-only, reporting-only, or verification-only phase is allowed.

The table below is the authoritative node-to-node DAG. The drawing is illustrative only.

| PR node | Exact direct dependency |
|---|---|
| `FND` | exact-revision approval and scoped green baseline |
| `PUR-CORE` | `FND` |
| `PUR-APP` | `PUR-CORE` |
| `PUR-API` | `PUR-APP` |
| `PUR-UI` | `PUR-API` |
| `RDY-CORE` | `PUR-APP` |
| `RDY-APP` | `RDY-CORE` |
| `RDY-API` | `RDY-APP` |
| `RDY-UI` | `RDY-API` |
| `TRN-CORE` | `RDY-UI` |
| `TRN-API` | `TRN-CORE` |
| `TRN-UI` | `TRN-API` |
| `WAI-CORE` | `PUR-APP`, `TRN-UI` |
| `WAI-ORCH` | `WAI-CORE` |
| `WAI-API` | `WAI-ORCH` |
| `WAI-UI` | `WAI-API` |
| `ADD-CORE` | `PUR-APP` |
| `ADD-API` | `ADD-CORE` |
| `ADD-UI` | `ADD-API` |
| `REC-CORE` | `PUR-UI`, `RDY-UI`, `TRN-UI`, `WAI-UI`, `ADD-UI` |
| `REC-OPS` | `REC-CORE` |
| `REL` | `REC-OPS` |

### Phase Contract Matrix

| Phase | Depends on | Relevant files/layers | Related skills/rules | Phase-end verification | Rollback / failure handling |
|---|---|---|---|---|---|
| 0 | exact-revision approval and baseline | existing order/admission Domain, Application, Persistence, HAL; new architecture tests | Clean Architecture, CQRS, EF, tests | Release build + `Event.Architecture.Tests` | retain current behavior; do not start successor features until authority ratchets pass |
| 1 | `FND` | purchase-governance Domain/Persistence/Application; generated migrations | payments, CQRS, EF, criticality | Release build + `Event.Domain.UnitTests` | remove unexposed new model/migration through generated tooling; retain old implemented lifecycle |
| 2 | `PUR-APP` | API/HAL/OpenAPI/generated client/BFF/Blazor/docs | auth, HAL, BFF, Blazor, accessibility | Release build + `Explore.Blazor.IntegrationTests` | remove new public registrations/contracts together; do not rely on HAL omission as authorization |
| 3 | `PUR-APP` | participant/consent/readiness Domain/Persistence/Application | privacy, CQRS, EF, auth | Release build + `Event.Application.UnitTests` | keep credentials withheld and remove unexposed new readiness path |
| 4 | `RDY-APP` | private API/HAL/OpenAPI/generated client/BFF/Blazor/docs | auth, HAL, BFF, Blazor, accessibility | Release build + `Event.API.IntegrationTests` | withdraw public contracts atomically; preserve canonical participant facts |
| 5 | `RDY-UI` | transfer/credential Domain through UI; generated migrations/contracts | auth, privacy, EF, outbox, HAL/BFF/Blazor | Release build + `Event.Persistence.IntegrationTests` | stop new offers/claims; reconcile durable state; never rewrite commerce/check-in history |
| 6 | `PUR-APP`, `TRN-UI` | waitlist/supply/payment/refund Domain through UI; Quartz/outbox | payments, EF, outbox, scheduling, auth/HAL | Release build + `Explore.Infrastructure.Tests` | stop new allocation/withdrawal; preserve ambiguous supply/payment and reconcile forward |
| 7 | `PUR-APP` | add-on commerce/inventory Domain through UI | payments, EF, outbox, HAL/BFF/Blazor | Release build + `Explore.Blazor.Client.Tests` | stop new add-on writes/fulfillment; preserve admission and reconcile money forward |
| 8 | `PUR-UI`, `RDY-UI`, `TRN-UI`, `WAI-UI`, `ADD-UI` | recovery state/services/host/health/options/runbooks | EF, outbox, scheduling, secrets, operations | Release build + `Explore.Secrets.UnitTests` | remain recovery-only; do not synthesize missing key/fence/cursor authority |
| 9 | `REC-OPS` | deployment manifest, architecture validators, generated contracts, release docs | architecture, IP clean-room, conventional commit | Release build + `Event.Architecture.Tests` | withhold release/status; keep incomplete capabilities `test-only` or `disabled` |

### Phase 0 — Lifecycle Authority Remediation

- **Goal:** establish semantic aggregate mutation and one domain decision surface before new states.
- **Exit:** direct-state/de duplicated decision inventory is closed for touched seams; architecture tests freeze oversized class growth and forbid new persistence lifecycle authority.
- **PR:** `FND`.

### Phase 1 — Purchase Governance Core

- **Goal:** implement R1 and durable operation identity in Domain/Persistence/Application.
- **Exit:** stable authority matrix, pinned policy/enforcement dimension, real PostgreSQL one-winner races, and no provider I/O.
- **PRs:** `PUR-CORE` then `PUR-APP`.

### Phase 2 — Purchase Public Surfaces

- **Goal:** expose purchase governance through API/HAL/generated contracts, then BFF/UI.
- **Exit:** generic private failures, antiforgery/rate/idempotency, HAL-only affordances, accessible disclosure, and repository-hosted API/BFF/component contract evidence.
- **PRs:** `PUR-API` then `PUR-UI`.

### Phase 3 — Participant Readiness Core

- **Goal:** implement R2 as the sole issuance/check-in readiness authority.
- **Exit:** order/participant scope, subject-owned consent, approval/revocation, and credential withholding pass deterministic races.
- **PRs:** `RDY-CORE` then `RDY-APP`.

### Phase 4 — Participant Readiness Surfaces

- **Goal:** expose private subject/organizer completion and dignified scanner/support states.
- **Exit:** API/HAL/contracts and UI remain PII-minimal, accessible, localized, and HAL-driven.
- **PRs:** `RDY-API` then `RDY-UI`.

### Phase 5 — Transfer And Credential Rotation

- **Goal:** implement R3 without resale or commerce mutation.
- **Exit:** shared ticket/eligibility fence closes transfer/check-in/consent/approval/reissue races.
- **PRs:** `TRN-CORE`, `TRN-API`, then `TRN-UI`.

### Phase 6 — Fair Return And Waitlist

- **Goal:** implement R4, S4-E/F, and WB-1 transaction ownership.
- **Exit:** deterministic ordering, full commercial equivalence, buyer continuity, one-winner capacity, provider ambiguity, and refund crash recovery pass.
- **PRs:** `WAI-CORE`, `WAI-ORCH`, `WAI-API`, then `WAI-UI`.

### Phase 7 — Event-Bound Add-Ons

- **Goal:** implement R5 with separate inventory/fulfillment/refund authority.
- **Exit:** checked arithmetic, conservation, admission separation, exact disclosure, and accessible optionality pass.
- **PRs:** `ADD-CORE`, `ADD-API`, then `ADD-UI`.

### Phase 8 — Recovery And Operator Controls

- **Goal:** implement R6, recovery mode, stop/resume controls, health, and timed drills.
- **Exit:** deterministic recovery/rotation/fence/cursor/poison contracts and operator runbooks pass; production-like timed restore remains an external launch gate.
- **PRs:** `REC-CORE` then `REC-OPS`.

### Phase 9 — Deployment Boundary, Contracts, And Release

- **Goal:** implement R7 and converge machine-consumed contracts/documentation.
- **Exit:** capability matrix is accurate; protected payout is absent; every capability is `production-approved`, `test-only`, or `disabled`; fresh I-VSD/CTO/user gates are closed.
- **PR:** `REL`.

## 7. Test-First And Verification Strategy

### 7.1 Red/Green Contract

- Every behavioral task writes a public-seam Red test before production edits.
- A Red command MUST compile/discover its test and fail by assertion for the absent behavior—not by build, fixture, discovery, timing, or environment failure.
- Targeted TUnit shape:

```bash
dotnet test --project <project.csproj> --configuration Release -- \
  --treenode-filter "/*/*/*<TestClass>/*" \
  --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
```

- `--no-build` is forbidden for newly authored Red/Green evidence.
- Concurrency tests subscribe/install barriers before triggering contenders; sleeps, polling, and timing luck are forbidden.
- Expected ceilings, ordering, money, and allocation values are literal/independently computed; tests MUST NOT call production policy/calculation helpers to derive expected values.

### 7.2 Required Evidence By Risk

- **Domain/Application:** semantic transition and failure-code tests.
- **Persistence:** deterministic real PostgreSQL controlled-interleaving tests for every one-winner/race/restore invariant; provider-portability tests for model/constraints.
- **API/HAL:** auth, tenancy, capability equivalence, ProblemDetails, cache headers, idempotency, rate limiting, OpenAPI.
- **BFF/UI:** token isolation, antiforgery, generated-client use, HAL affordances, state rendering.
- **Privacy/observability:** inject literal email/phone/token/provider/money sentinels and prove zero plaintext in logs, metrics, traces, health, ProblemDetails, Quartz/outbox/operator outputs.
- **Mutation:** Phases 0, 1, 3, 5, 6, 7, and 8 run phase-scoped Stryker campaigns over changed safety-critical Domain/Application files with CLI break threshold 86 (>85%); retain JSON killed/survived/no-coverage counts.
- **MAD:** each Tier 0–2 PR retains anonymized structured YAML with independent domain/payment, PostgreSQL/concurrency, and security/privacy arguments, 60/40 weighted vote, reproducible invariant-breakers, remediation, and rerun evidence.

### 7.3 PR And Phase Gates

- Establish one green scoped baseline after exact-revision approval and before the first product edit.
- Task-level Red/Green uses exact focused selectors. Each phase closeout runs exactly one Release build and at most the one selected non-browser full project in the Phase Contract Matrix.
- Phase closeout does not defer missing evidence to Phase 9.
- No solution-level `dotnet test`.
- Generated migrations/OpenAPI/NSwag outputs are regenerated and diff-reviewed in the PR that changes their source.

### 7.4 Deterministic Non-Browser Evidence

Planning and phase verification are limited to repository non-interactive unit/integration fixtures and generated/static contract checks. External operator launch evidence is not an implementation-plan verification lane.

Minimum deterministic performance/scale assertions for the declared reference fixture:
  - 50 concurrent claims for one entitlement: exactly one winner, p95 ≤500 ms, p99 ≤1 s;
  - check-in/transfer shared-fence race: p95 ≤250 ms, p99 ≤500 ms;
  - 10,000 due lifecycle effects: no starvation and drain within 15 minutes;
  - memory/query/metric cardinality remains bounded by configured batch and closed vocabularies.

## 8. Documentation, Comments, Configuration, And Operations

Each PR updates the owning documentation; no final documentation catch-up:

| Capability | Required documentation |
|---|---|
| Architecture/debt | `ARCHITECTURE.md`, `DOMAIN.md`, `CODEBASE_INSIGHTS.md`, relevant ADR status/implementation notes |
| Purchase/payment | `PAYMENTS.md`, `API.md`, `WEBHOOKS.md`, `AUTHORIZATION.md`, `TESTING.md` |
| Participants/transfer | `DOMAIN.md`, `SECURITY-MODEL.md`, `CONTACT_SHARING.md`, `PRIVACY_ERASURE.md`, `ACCESSIBILITY.md`, `BLAZOR.md` |
| Waitlist/add-ons | `PAYMENTS.md`, `DOMAIN.md`, `API.md`, `OPERATIONS.md`, `TROUBLESHOOTING.md` |
| Recovery/release | `BACKUP_RESTORE_UPGRADE.md`, `SELF_HOSTING.md`, `CONFIGURATION.md`, `SECRETS.md`, `OPERATIONS.md`, `TROUBLESHOOTING.md`, `.env.example`, `docs/index.md` |

Code documentation rules:

- two-line `ABOUTME:` header on every file;
- XML documentation for public ports/options/contracts whose invariants are not obvious from types;
- comments at lock acquisition, provider-handoff, idempotency, recovery, and privacy boundaries explain **why** ordering/fencing exists and which failure it prevents;
- no comments that restate code, duplicate tests, promise future work, or conceal an oversized abstraction;
- state-transition tables and operator decision matrices belong in canonical docs, not giant comments.

Configuration is added only with implementation: typed options, validator, safe default, range, restart/hot-reload semantics, secret owner, `.env.example`, self-hosting docs, and tests. Disabled Quartz jobs are absent, not merely no-op.

### 8.1 Release And Changelog Strategy

- This workstream is Tier 2 release-note eligible because it changes user/operator behavior.
- Final Task 9.4 creates `docs/releases/changes/CHG-YYYY-NNNN.yaml` only after implementation/docs/evidence converge and validates it through `ReleaseInputPolicy`.
- The terminal commit, when explicitly authorized, carries `Change-Id: CHG-YYYY-NNNN` and `BREAKING CHANGE:` because public contracts are intentionally replaced without compatibility.
- Internal plumbing commits use `Changelog: skip` and `Changelog-Reason: <clear reason>` so release notes describe observable behavior rather than refactoring noise.
- Intentional breaking OpenAPI changes update `docs/API_CHANGELOG.md` in the same PR with affected routes/schemas/generated methods and development-mode migration guidance.

## 9. Islamic Value-Sensitive Design And Authority Boundaries

Independent planning-mode revalidation has bound this plan and its task ledger to the current I-VSD report. Any later material change to provider-controlled access, consent, transfer, refund, recovery, payout, scenarios, or mapped task ownership makes that report stale again.

Validated mapping:

| Finding / mitigation | Scenarios | Tasks |
|---|---|---|
| `IVSD-F001` / `IVSD-M001` | S1-A/B/C | 1.1–1.4, 2.1–2.6 |
| `IVSD-F002` / `IVSD-M002` | S2-A/B/C | 3.1–3.4, 4.1–4.6 |
| `IVSD-F003` / `IVSD-M003` | S3-A/B/C | 5.1–5.6 |
| `IVSD-F004` / `IVSD-M004` | S4-A/B/C/D/E/F, WB-1 | 6.1–6.8, 8.1–8.4 |
| `IVSD-F005` / `IVSD-M005` | S5-A/B/C | 7.1–7.6 |
| `IVSD-F006` / `IVSD-M006` | S6-A/B/C, WB-1 | 8.1–8.4, 9.1–9.4 |
| `IVSD-F007` / `IVSD-M007` | S7-A | 9.1–9.4 |

I-VSD does not grant technical, user, scholarly, legal, privacy, security, accessibility, provider, or production approval. Protected payout remains an escalation gate.

## 10. Security, Authorization, Privacy, And Abuse

- Writes fail closed under server authorization and resource lifecycle policy.
- Current tenant/user/actor derives from trusted server context, never request bodies or browser headers.
- Capability plaintext/digests and participant/payment/provider values never enter telemetry or public errors.
- Capability absence, malformed, expired, consumed, wrong-resource, and wrong-tenant cases are externally indistinguishable.
- Public/private endpoints use appropriate antiforgery, partitioned rate limiting, no-store/private caching, and stable failure codes.
- Contact/consent/transfer/waitlist data has explicit owner, purpose, minimization, retention, export, correction, and erasure behavior.
- Provider webhook intake verifies exact signed bytes, deduplicates durable event identity, and treats out-of-order observations monotonically.
- Stop controls disable new writes and worker initiation while preserving signed intake, reconciliation, refunds, support reads, and ambiguity resolution.

## 11. Multi-Tenancy, Federation, Localization, Accessibility, And Performance

- Every persisted row and uniqueness/claim scope includes tenant identity.
- Multi-tenant background work carries explicit tenant facts; no cross-tenant batch mutation without named bypass and per-row tenant fencing.
- SQLite supports exactly one API replica. Server-database multi-replica deployments require shared primary state and Quartz clustering configuration.
- Federation is not a ticketing authority and cannot widen local commerce/admission decisions.
- Public/operator copy is localizable and RTL-safe; no concatenated fragments or physical-direction CSS.
- UI action affordances come only from HAL.
- Dynamic status uses appropriate live-region behavior without focus theft; errors associate to controls; irreversible/payment/transfer/timeout recovery is keyboard and screen-reader tested.
- Query plans/indexes are measured at representative cardinality; no N+1, unbounded materialization, identifier metric labels, or tenant/event-per-metric series.

## 12. Observability, Health, And Recovery Operations

Telemetry uses fixed vocabularies and aggregate counts/age/duration only.

- **Unhealthy:** aggregate authority unavailable, required key/cursor missing, scheduler required but unavailable, or recovery validation failed.
- **Degraded:** any `Unknown`, dead-letter, poison, expired lease, or cursor gap; oldest due age ≥120 seconds; or configured backlog threshold exceeded.
- **Warning/page:** warn at oldest due age 60 seconds; page at 120 seconds and immediately for unknown/dead-letter/cursor-gap count >0.
- No metric/log/trace/health label contains tenant, event, order, ticket, actor, contact, amount, provider object, capability, or digest.

Backup quiescence protocol:

1. enter deployment-level maintenance/stop-sale;
2. reject new purchase/transfer/waitlist/add-on mutations;
3. pause Quartz on all replicas and stop new queue claims;
4. fail readiness and drain active work within the host grace budget;
5. classify unfinished post-handoff effects as `Unknown`;
6. capture queue/fence/cursor/key highs and the consistency manifest;
7. back up primary state, retained authority, object storage, identity, and exact secret versions;
8. restore into recovery mode;
9. validate/rotate/reconcile;
10. reopen reads/support, then workers, then sales last.

Operator resolution is authenticated, HAL-advertised, generation/fence-checked, and append-only audited. Direct SQL repair and blind replay are forbidden.

## 13. Migration, Rollout, Rollback, And Compatibility

- Product behavior changes by direct cutover; obsolete compatibility code/tests/contracts are deleted.
- Generated migrations are produced from entities/configurations/generator for every supported provider and are never hand-edited.
- Unapplied disposable development migrations may be removed/regenerated through EF tooling; applied/merged history receives a generated corrective migration.
- Expand/contract compatibility is used only when required for a rolling multi-replica deployment, not for legacy API behavior.
- API source, OpenAPI, generated client, consumers, and contract tests change atomically in the same PR.
- Core PRs have no public/scheduled activation. Public PRs can be stopped only by authoritative server controls, route/job removal, or forward fix—not HAL omission.
- Durable payment/admission/audit facts are never rolled back to fake success. Ambiguity is reconciled forward.
- Every release note classifies rollback as image-only, forward-fix, or matched data restore and names the stop/reconcile/reopen procedure.

## 14. Risk Register

| Risk | Impact | Required control | Owner tasks |
|---|---|---|---|
| duplicated lifecycle authority | Critical | Phase 0 semantic decision surface and architecture ratchet | 0.1–0.3 |
| anonymous ceiling overclaim | High | access-mode matrix and honest copy | 1.1–2.3 |
| payment/supply split-brain | Critical | S4-E/F, WB-1, canonical lock order, durable operation IDs | 6.1–6.5, 8.1–8.4 |
| transfer/check-in/revocation double authority | Critical | shared fence and credential generation rotation | 5.1–5.4 |
| add-on overflow/admission leakage | High | checked minor units and architecture separation | 7.1–7.4 |
| restore bearer resurrection | Critical | recovery mode and mandatory rotation/reissue | 8.1–8.4 |
| cross-tenant worker leak | Critical | tenant-qualified claims and real negatives | every persistence/worker phase |
| false Red/Green | Critical | no `--no-build`, nonzero discovery, assertion failure, public seams | every behavioral task |
| mutation/PII/MAD evidence deferred | High | phase-owned gates and retained artifacts | 0–8 |
| payout leakage | Critical | machine-readable absence ratchet | 9.1–9.4 |
| external approval confused with code readiness | High | deployment status matrix and named external gates | 9.1–9.4 |

## 15. Success Metrics And Definition Of Done

- Every S1–S7 and WB-1 behavior has Red-before-Green, deterministic, non-tautological evidence.
- Real PostgreSQL tests prove one-winner, rollback, retry, deadlock avoidance, tenant isolation, and restore behavior.
- Safety-critical changed files achieve Stryker break threshold 86 with retained JSON evidence.
- Zero sentinel PII/capability/provider/money values appear in telemetry or public/operator outputs.
- Every Tier 0–2 PR has anonymized MAD evidence and no unresolved critical vote.
- All task-level focused evidence, the selected phase project, Release build, generated artifacts, and docs pass.
- Deterministic performance/recovery contracts pass; production-like timed restore and multi-replica evidence remain external launch gates.
- No compatibility shim, duplicated authority, oversized-seam extension, manual migration/client edit, or protected payout surface remains.
- I-VSD is current/plan-aligned for exact plan/tasks revisions; a fresh CTO review is bound to exact hashes; the user approves that revision.

## 16. Implementation Agent Contract — Keep Dev Docs Current

1. Resume from context plus the current task; open only the referenced plan section.
2. Establish the green baseline once after approval.
3. Write and run the named Red test; prove assertion failure.
4. Implement the smallest Green change in the owning layer.
5. Refactor only touched debt required by Phase 0 ratchets.
6. Update generated artifacts, code comments, canonical docs, config, and tests in the same PR.
7. Run task-level focused verification and the one selected phase closeout project; do not start the app/browser/runtime during this workstream.
8. Reconcile task/context immediately; refresh the plan only when strategy, scenario, phase order, risk, or validation changes.
9. Stop and revalidate I-VSD/CTO after any authority, access, consent, refund, recovery, payout, or mapped-scenario/task change.
10. Do not implement protected payout or broaden external/business-system scope.

## 17. Progress Reporting Contract

```text
Phase/PR: <number and graph node>
Status: RED | GREEN | VERIFIED | BLOCKED
Behavior proved: <scenario and public seam>
Changed authority: <aggregate/application/persistence/API/HAL/BFF/UI>
Evidence: <commands, mutation, PII scan, MAD, generated diff>
Operational/docs impact: <updated canonical files and runbook state>
Risk/rollback: <stop, reconcile, forward-fix/restore rule>
Next: <dependency-approved PR>
```

## 18. Potential Risks And Unknowns

The most likely failure is not a missing endpoint; it is duplicated authority across aggregate methods, persistence conditionals, worker reconciliation, and HAL state reconstruction. Phase 0 exists to prevent that debt from growing before the concurrency surface expands.

WB-1 remains the catastrophic boundary: payment handoff, seller withdrawal, capacity reuse, refund, crash/restore, and stale workers can produce two buyers for one entitlement unless transaction ownership, durable idempotency, lock order, and recovery mode are one coherent design.

The largest external uncertainty remains deployment authority. Legal, qualified scholarly, provider, privacy/security, accessibility, stakeholder, and operator evidence may keep a capability `test-only` or `disabled`. None of those gates can enable protected delayed payout inside this workstream.
