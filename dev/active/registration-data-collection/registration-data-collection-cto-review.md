<!-- ABOUTME: Senior CTO review of the Phase 22+ registration-data-collection implementation-plan rebaseline. -->
<!-- ABOUTME: Records evidence, sizing, risks, and approval gates without changing completed Phase 0–21 work. -->

# Senior CTO Review — Registration Data Collection Phase 22+

Review date: 2026-08-27 Europe/Brussels

Verdict: **CHANGES REQUIRED — split before approval**

## Revision Binding

| Artifact | Reviewed SHA-256 |
|---|---|
| `registration-data-collection-plan.md` | `42ef4342117d07097a06dd2d22c4892a5f18d67f73a0082950dc3811d355494a` |
| `registration-data-collection-tasks.md` | `12517d9c29af7e6bff232d18190971579ca4229b364698ca907b8bea8c64d640` |
| Primary registration I-VSD report | `d7723403e6d8b1a70854599a3c4812091290cf505bea7ea0a4558a5e6532d237` |
| Secondary paid-event consultation | `44e90e5ccb88ba7e98503f0f1b98c00b7bdfaf85d623aff8f7ff882a2a90cb36` |

## Resolution — 2026-08-27

The user accepted this review's split recommendation. The reviewed Phase 22+ scope moved to [`dev/active/event-ticketing-lifecycle/`](../event-ticketing-lifecycle/), and Registration Data Collection closed at Phase 21. This artifact remains the revision-bound historical rationale for the split; it does not approve the successor revision, which requires its own fresh CTO review.

This review covers only the Phase 22+ rewrite. Completed Phase 0–21 bodies and implementation claims were not reviewed for reopening and were intentionally left unchanged.

## Executive Decision

The prior Phase 22–25 draft was not ready to execute. It put transfer before the newer report's purchase-governance and participant-completion prerequisites, combined two independently shippable capabilities in one phase, and retained conditional payout runtime tasks despite absent approvals and preview-only provider evidence.

The plan is now materially stronger:

- Phase 22 closes access modes, explicit purchaser context, accepted terms, and cascading purchase ceilings.
- Phase 23 closes order/participant data scope, consent/approval, and admission gating before transfer.
- Phase 24 implements transfer as atomic future-holder/credential rotation with immutable commercial history.
- Phases 25 and 26 separate fair-return waitlist and event-bound add-ons.
- Phase 27 proves recovery, provider/capability status, generated contracts, and closeout.
- `ProtectedDelayedPayout` runtime work is removed, not hidden behind a feature flag.

Approval is still withheld because this material rewrite occurred after the latest I-VSD report. The report remains authoritative input, but it cannot approve a revision it did not review. I-VSD planning revalidation must come first, followed by a fresh CTO review and explicit user approval.

## Readiness Rubric

| Area | Score | CTO assessment |
|---|---:|---|
| Product/authority correctness | 4/5 | Purchaser, participant, holder, guardian, organization/group, organizer, and provider authorities are separated. Stakeholder validation remains external. |
| Architecture and maintainability | 5/5 | Clean Architecture, CQRS/MediatR, repository entities, typed state, EF constraints, outbox, Quartz, HAL, BFF, generated clients, and existing capability primitives are reused without speculative abstractions. |
| Security/privacy | 4/5 | PublicTransactional, tenant isolation, hashed/protected capabilities, generic failures, zero-PII telemetry, minimization, purpose/retention, and restore-key requirements are explicit. Independent validation remains required. |
| Financial/state integrity | 4/5 | Integer minor units, immutable commercial truth, provider I/O post-commit, fair-return reconciliation, one-winner races, and payout exclusion are explicit. Legal/scholarly/provider/operator approvals remain external. |
| Testing/recovery | 5/5 | Every runtime phase begins RED, names a worst-break scenario, requires real PostgreSQL concurrency, >85% mutation score, zero-PII scans, restore/replay proof, and MAD review. |
| Delivery sizing | 4/5 | Six separate phase boundaries replace four over-broad phases. Backend contracts and HAL/UI activation have explicit PR seams. Enforce these splits during implementation. |
| Traceability/review state | 3/5 | F1–F13 map to phases/tasks and exact report revision, but the rewritten plan still needs I-VSD revalidation and stable-reference acceptance. |

## Blocking Findings

### CTO-22-001 — I-VSD review is stale after material rewrite

- **Evidence:** the primary report is dated 2026-08-26; the authoritative Phase 22+ plan/tasks revision is dated 2026-08-27 and changes ordering, behavior, payout scope, and traceability.
- **Risk:** treating the earlier report as approval would collapse “authoritative input” into “reviewed plan” and overstate Islamic value-sensitive validation.
- **Required action:** an I-VSD planning reviewer must review the exact current plan/tasks hashes, accept or replace the stable F6–F13 references, and record unresolved stakeholder/scholarly/legal/privacy/accessibility evidence.

### CTO-22-002 — Fresh CTO and user approval must bind the same revision

- **Evidence:** the current review necessarily returns `CHANGES REQUIRED`; repository workflow prohibits the rewrite pass from approving its own materially changed revision.
- **Risk:** implementation could begin from an unreviewed moving target.
- **Required action:** after I-VSD revalidation, compute new plan/tasks hashes, run a fresh Senior CTO review without another material rewrite, then obtain explicit user approval before Task 22.0.

### CTO-22-003 — Delivery boundaries must be enforced, not merely documented

- **Evidence:** each of Phases 22–26 spans Domain/Persistence/Application/API/HAL/BFF/Blazor and exceeds one small PR if implemented monolithically.
- **Risk:** one review unit would obscure concurrency, auth, privacy, generated-contract, and UI regressions.
- **Required action:** one phase per merge boundary; use backend-contract PR A and HAL/BFF/UI activation PR B where the diff is not independently reviewable. Backend A must expose no new HAL affordance until B.

## Highest-Risk Invariant Breakers

1. Concurrent individual and organization/group purchases bypass the same hard ceiling.
2. Reconciled payment creates an active credential while participant requirements or approval are incomplete.
3. Transfer acceptance and old-credential check-in both win, or an adult inherits another subject's consent.
4. One released entitlement is allocated to both public checkout and a waitlist offer, or refund precedes replacement-payment truth.
5. Add-on refund mutates admission or fulfilled add-on truth.
6. Restore/replay revives a revoked capability or duplicates refund/notification/fulfillment.

These scenarios are mandatory RED tests through public seams with deterministic database coordination. Any phase that cannot make its named worst break fail first is not ready for Green implementation.

## Architecture And Scope Guardrails

- Reuse current aggregates and infrastructure. Do not add a generic workflow engine, feature-flag framework, second scheduler, second capability abstraction, or payment-provider factory.
- No backward compatibility: remove/replace development contracts at the source and regenerate unapplied development migrations/OpenAPI/NSwag; never hand-edit generated artifacts.
- Repository methods return entities; Application handlers map DTOs and own business validation; controllers dispatch/map; HAL policies own UI affordances; BFF keeps tokens server-side.
- External effects are durable and post-commit. Outbox/inbox/reconciliation and Quartz one-pass jobs own retries/fencing; no provider/email/webhook I/O in business transactions.
- Central tenant filters plus tenant-qualified keys/FKs fail closed. Display IDs, emails, names, purchaser context, and guest contact never authorize.
- Data purpose, subject scope, visibility, retention, export, and erasure linkage are explicit. This workstream integrates with the existing erasure authority rather than creating another.
- Event owns registration, capacity, admission, waitlist, and event add-ons only. Marketing, bookkeeping, accounting, tax determination, and legal invoice/credit-note issuance stay external.

## Protected Delayed Payout Decision

Reject runtime implementation in this workstream. Official Stripe documentation currently describes connected-account reserves as Private preview, while connected-account payout control also depends on exact account/dashboard configuration. The repository forbids preview/raw/undocumented APIs and requires current Stripe, legal, qualified Islamic scholarly, consumer/payment-services, reserve/liability, complaint/dispute, and accountable-operator approvals. None is established here.

Phase 27 therefore verifies `ProtectedDelayedPayout` is absent/unavailable in runtime, configuration, HAL, generated contracts, and scheduler registration. A future proposal is a separate workstream; it cannot be silently enabled or relabeled as escrow.

## Verification Decision

The plan correctly scopes verification by changed layer during active development and defers the full intent matrix to Phase 27 closeout. Tier 0/1/2 slices require:

- failing-first public-seam invariant tests;
- real PostgreSQL concurrency/restart/restore proof without sleeps;
- one Release build per phase and nonzero affected test projects;
- EF pending-model checks for changed model owners;
- zero-PII/capability telemetry scans;
- safety-critical Stryker mutation score above 85%;
- anonymized MAD review with security/privacy and quality reviewers;
- deterministic generator-owned OpenAPI/NSwag/migration evidence.

This planning-only change must not run .NET build/tests. Its correct checks are Markdown/link/traceability/parity/whitespace validation.

## Approval Sequence

1. I-VSD planning revalidation of the exact rewritten plan/tasks revision.
2. Fresh Senior CTO review of the same revision; no self-approval in the rewrite pass.
3. Explicit user approval.
4. Task 22.0 RED.

Until those steps complete, Phases 22–27 remain `NOT STARTED / REVIEW-GATED`.
