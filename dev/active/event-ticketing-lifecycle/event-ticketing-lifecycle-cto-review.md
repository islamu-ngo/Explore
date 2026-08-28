<!-- ABOUTME: Fresh read-only Senior CTO review of the Event Ticketing Lifecycle workstream. -->
<!-- ABOUTME: Binds technical approval to exact plan, tasks, and current I-VSD revisions. -->

# Senior CTO Feedback

Last Updated: 2026-08-27 Europe/Brussels

## Review Metadata

- **Review mode:** Read-only.
- **Reviewed plan revision:** SHA-256 `84bcd73f5d603fcd24f1a4cf9aaeef5e7f041a36e8459b83b173582ea25e24fa`
- **Reviewed tasks revision:** SHA-256 `0373aa09e4555fda371e073eee17ab7b0bb8ebfeaa4c9c5591268f7813b5397b`
- **Reviewed I-VSD revision:** SHA-256 `8cbacacba7be2268501ed703337534e3659ea0095812ca074524452249b0b128`
- **I-VSD freshness:** Current / plan-aligned.
- **Decision:** Approve.
- **User approval:** Not granted by this review.

## Executive Verdict

The exact reviewed revision is technically ready for implementation. It turns a large cross-cutting capability into a dependency-bound PR train, puts lifecycle authority remediation before feature growth, gives Tier 0 money and entitlement races deterministic public-seam Red tests, and defines fail-closed recovery rather than treating restore as object serialization. The implementation must still honor the named PR graph; this approval is not permission for an omnibus change, protected delayed payout, or production enablement.

**Decision: Approve.**

Implementation remains gated on explicit user approval of the exact hashes above, one scoped green baseline, and migration-history classification before any migration regeneration.

## 3-Dimensional Scorecard

| Dimension | Status | Key finding |
|---|---|---|
| **Completeness** | Pass | S1-S7 and WB-1 map to 51 Red/Green/refactor tasks, phase evidence, docs, recovery, and release ownership. |
| **Correctness** | Pass | High-risk seams use assertion-failing Red tests, real PostgreSQL races, literal expectations, canonical lock order, durable idempotency, and crash/replay outcomes. |
| **Coherence** | Pass | Domain owns transitions; Application orchestrates; Persistence locks/stores; Infrastructure dispatches; API authorizes and emits HAL; BFF isolates tokens; Blazor consumes HAL/generated contracts. |

## Top Risks

### 1. CRITICAL — Paid entitlement split-brain

**Why it matters:** payment handoff, seller withdrawal, capacity reuse, late provider success, and stale restore workers can create two buyers or two money chains for one entitlement.

**Evidence:** plan S4-E, S4-F, S6-C, WB-1, Decisions E/F/I, and Tasks 6.1-6.8 plus 8.1-8.4 define one canonical lock order, durable local intent before provider I/O, monotonic reconciliation, stale-fence rejection, and recovery-only startup.

**Required implementation discipline:** WB-1 remains release-blocking. Real PostgreSQL barriers must prove one winner and loser rollback; component-level green tests cannot waive it.

### 2. CRITICAL — Authority duplication can return

**Why it matters:** adding new states to existing oversized lifecycle and repository seams would let aggregates, handlers, workers, persistence conditionals, and HAL disagree.

**Evidence:** Phase 0 / `FND` forbids feature work until direct mutation and duplicated decisions are inventoried, moved behind semantic aggregate mutation and one domain decision surface, then frozen by architecture tests.

**Required implementation discipline:** do not bypass Tasks 0.1-0.3 or turn the shared decision surface into a generic workflow/rules engine.

### 3. CRITICAL — Restore can resurrect bearer authority

**Why it matters:** a point-in-time backup can predate transfer-capability revocation or credential rotation.

**Evidence:** S6-A/B/C and Tasks 8.1-8.4 require deployment-level stop-sale, manifest validation, restored key/fence/cursor/idempotency integrity, capability cancellation, credential rotation/reissue, and staged reopen.

**Required implementation discipline:** recovery evidence must start from clean storage and include stale worker/credential attempts. In-memory serialization tests are insufficient.

### 4. MAJOR — Migration history is environment-dependent

**Why it matters:** deleting or regenerating an applied migration can break self-hosters and violate repository migration policy.

**Evidence:** plan Sections 2.4 and 13 distinguish disposable unapplied migrations from applied/merged history.

**Required implementation discipline:** classify every affected migration before generation. Use EF tooling only; never hand-edit migrations, designers, or snapshots.

## What I Would Keep

- The exact `FND` through `REL` dependency graph and prohibition on omnibus delivery.
- Honest access-mode ceilings: hard cross-order enforcement only where stable server authority exists.
- Durable business idempotency independent of HTTP cache retention.
- Full commercial equivalence before buyer-transparent supply rebinding.
- Provider I/O outside transactions, with transactional outbox/effect state and monotonic reconciliation.
- HAL-only UI affordances while API/Application remain authorization and lifecycle authorities.
- Phase-owned mutation, zero-PII, MAD, generated-contract, documentation, and operational evidence.
- Structural absence of protected delayed payout.

## What Must Hold During Implementation

1. Each PR node lands its own Red, smallest Green, debt deletion, generated artifacts, docs, and focused evidence.
2. A Red test compiles, discovers at least one test, and fails by assertion before production edits.
3. Concurrency tests install deterministic barriers before contenders; sleeps, polling, and timing luck remain forbidden.
4. Tenant identity qualifies every lookup, uniqueness rule, lock, claim, cache key, and background operation.
5. Provider effects commit durable intent first and reconcile at least once without duplicate authority or money.
6. Any material authority, consent, refund, recovery, payout, scenario, or task-mapping change makes I-VSD stale and stops the train.

## Dev-Docs Quality Assessment

### `event-ticketing-lifecycle-plan.md`

Pass. It separates observable RFC 2119 behavior and S1-S7/WB-1 scenarios from architecture decisions, names the canonical lock/transaction order, defines rollback and recovery behavior, and makes the PR DAG authoritative.

### `event-ticketing-lifecycle-context.md`

Pass once synchronized with this review. It already preserves the verified repository state, exact resume point, baseline policy, blockers, decisions, risks, and handoff.

### `event-ticketing-lifecycle-tasks.md`

Pass. All 51 implementation tasks have bounded ownership, exact planned evidence, effort, dependencies, acceptance, and Red-before-Green ordering. Every phase closes with one Release build and one selected non-browser project.

## Islamic Value-Sensitive Design Assessment

The current report at `islamic-value-sensitive-design/i-vsd-event-ticketing-lifecycle.md` is bound to the exact reviewed plan/tasks hashes. Stable findings `IVSD-F001` through `IVSD-F007` and mitigations `IVSD-M001` through `IVSD-M007` map to S1-S7/WB-1 and Tasks 1.1-9.4. It correctly limits itself to provider responsibility, preserves evidence gaps and qualified scholarly/legal escalation, and does not convert technical completion into certification or production approval.

## Socratic Stress-Testing And Worst-Break Audit

### The Worst Break

WB-1 is the correct catastrophic scenario: a payment dispatch is durably claimed while supply withdrawal/expiry and reallocation contend, a late provider success arrives, and restored stale workers resume. The plan requires at most one admission owner, one capture-linked settlement/refund chain, no ambiguous resold supply, stale-fence rejection, and stop-sale until reconciliation.

### Stress-Test Findings

- **Rollback:** durable payment/admission/audit facts reconcile forward; they are never rewritten to simulate rollback.
- **Tenant boundary:** all claims, locks, uniqueness, filter bypasses, jobs, and negative tests are tenant-qualified.
- **Performance:** 50-way one-entitlement races and 10,000-effect drains have deterministic ownership and thresholds.
- **Operator clarity:** stop, pause, drain, classify Unknown, restore, validate, rotate, reconcile, and staged reopen are explicit.
- **External ambiguity:** duplicate, stale, out-of-order, and contradictory provider observations converge monotonically.
- **Access honesty:** name-only mode does not claim an unenforceable per-person cross-order ceiling.

No unresolved technical fork blocks implementation. External launch evidence can narrow or disable capability status but cannot broaden this workstream.

## Enterprise And Self-Hosting Assessment

The plan covers typed configuration, `.env.example` schema, secret references, startup validation, SQLite single-replica limits, server-database multi-replica/Quartz requirements, fixed-cardinality health, dead-letter/Unknown operator handling, generated migrations, release notes, recovery manifests, declared RPO/RTO, and staged reopen. Production-like timed restore and multi-replica takeover correctly remain external operator gates.

## Security And Multi-Tenancy Assessment

Writes authorize server-side; browser tokens stay in the BFF; unsafe proxy endpoints require antiforgery; trusted tenant/actor context is server-owned; capability failures are generic and no-store; logs, metrics, traces, health, and ProblemDetails exclude PII, amounts, provider IDs, capabilities, and digests. Cerbos/local parity and cross-tenant negatives remain mandatory evidence.

## Architecture And Maintainability Assessment

The design follows Clean Architecture without introducing a speculative framework. Explicit capability-specific state machines and coordinators are preferable to a generic lifecycle engine. CQRS handlers manually validate and orchestrate; repositories return entities and expose transaction-bound primitives; EF owns generated storage artifacts; outbox/process state owns durable effects; Quartz remains a pointer-only scheduler; generated OpenAPI/NSwag contracts and HAL remain the public/client boundary.

## Breaking-Change Position

Direct replacement is correct for this pre-v1 development repository. Superseded routes, DTOs, duplicated authority, tests, and docs should be deleted in the owning PR. Immutable audit, payment, refund, and check-in facts remain preserved. Compatibility aliases, dual writes, dormant payout surfaces, and hand-edited generated artifacts are not acceptable.

## Implementation Sequencing

1. Close user approval and baseline gates.
2. Deliver `FND` lifecycle authority remediation.
3. Follow the exact core-to-API-to-BFF/UI dependency graph.
4. Deliver recovery/operator controls after all public lifecycle slices.
5. Converge capability matrix, generated contracts, I-VSD/CTO/user evidence, and changelog in `REL`.

## Verification Bar

- One initial Release build and `Event.Architecture.Tests` baseline after user approval.
- Exact TUnit selectors for each Red/Green task with nonzero discovery and no `--no-build`.
- Real PostgreSQL race/restore evidence where named.
- Phase-scoped Stryker break threshold 86 JSON, zero-sentinel telemetry evidence, and anonymized MAD YAML for Tier 0-2 phases.
- One Release build plus the selected full project at each phase closeout.
- Generated EF/OpenAPI/NSwag artifacts regenerated from source and diff-reviewed.
- No solution-level tests, browser runtime, fixed sleeps, or manual migration/client edits.

## Approval Boundary

This review grants technical plan readiness only. It does not grant user approval, production approval, legal/payment-services approval, qualified Islamic scholarly approval, accessibility/privacy/security certification, provider capability approval, or permission to implement protected delayed payout.
