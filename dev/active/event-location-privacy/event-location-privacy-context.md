<!-- ABOUTME: Current Senior CTO handoff for the event-location privacy workstream. -->
<!-- ABOUTME: Separates verified runtime reality from target architecture, release gates, and self-hosting obligations. -->

# Event Location Privacy — Context

Last Updated: 2026-07-22 Europe/Brussels

## Senior CTO Executive Verdict

The first-class `EventLocation` disclosure architecture is the correct direction. It centralizes purpose-limited location projection, keeps physical PII behind the Application boundary, preserves API-authoritative authorization, and gives self-hosters a credible path to operate privacy-sensitive event locations.

The workstream is not release-ready. Historical staged-migration evidence no longer describes the current EventLocation schema baseline, and several production paths exist without their task-level verification gates. Platform User erasure and authority topology are external dependencies, not implementation scope for this workstream.

**Core architecture decision:** Approved.

**Workstream decision:** Re-baselined for review. The plan/tasks now contain only EventLocation-owned work plus one typed platform-erasure adapter boundary.

**Release posture:** Blocked. Do not activate exact-location disclosure, execute destructive contract cleanup, or claim enterprise readiness until every release gate in this context is green.

**Implementation posture:** Continue only in bounded slices that preserve fail-closed disclosure, tenant isolation, irreversible erasure, and transactional correction guarantees.

**Complexity:** XL / high risk because this spans contextual privacy, tenant isolation, authorization, migrations, OpenAPI/NSwag, Blazor, federation, transactional correction delivery, and cache convergence.

## SESSION PROGRESS (2026-07-22 Europe/Brussels)

### ✅ COMPLETED

- Re-reviewed the complete `plan.md`, `context.md`, and `tasks.md` workstream against the current `senior-cto-feedback` and `implementation-plan` quality gates.
- Verified the core architecture in current source: `EventLocation`, the pure disclosure evaluator, batched disclosure service, purpose-specific API controller, HAL policies, correction dispatcher, and remediation paths exist.
- Re-baselined the task ledger to 27 of 59 EventLocation-owned tasks; additional production code remains intentionally unchecked until its risk-oriented tests pass.
- Confirmed the root `ExploreDbContext` `init` migration is the current EventLocation schema baseline; earlier staged ELP migration proofs are historical evidence only.
- Removed global User-erasure, membership-removal, authority topology, receipt, replay, retention, and restore ownership from this workstream.
- Captured a fresh Release build on 2026-07-22: 26 projects, 0 errors, 41 warnings.

### 🟡 IN PROGRESS

- Re-baselining the remaining Event Location Privacy work against the current root migration and the typed platform-erasure adapter boundary.
- Converting code-presence claims for `ELP-315`, `ELP-320`, `ELP-350`, `ELP-360`, `ELP-405`, `ELP-410`, `ELP-440`, `ELP-520`, `ELP-530`, `ELP-720`, and `ELP-730` into executable verification evidence before checking them complete.

### ⏭️ NEXT

1. Review and approve the re-baselined EventLocation-only plan and task ledger.
2. Verify existing Application/API/HAL/correction paths in dependency order, then check only tasks whose full acceptance criteria pass.
3. Implement/prove the typed EventLocation disposition adapter without importing platform saga, topology, receipt, replay, or retention logic.
4. Generate and review additive OpenAPI/NSwag contracts before Blazor adoption; contract obsolete generic location surfaces only after all consumers have moved.
5. Complete EventLocation migration, correction/dead-letter, cache-convergence, review/remediation, and operator guidance before release activation.

### ⚠️ BLOCKERS

- **Blocker — migration evidence is stale.** The plan/tasks retain historical staged ELP evidence, while current source has a generated root `init` migration. EventLocation lookup/constraints/triggers/append-only audits and irreversible Location-state guards must be re-proven from current artifacts before destructive contraction.
- **External dependency — platform erasure.** The authority workstream supplies the User intent/fence and transaction contract. ELP supplies only typed Home/room disposition and affected-EventLocation correction behavior; it must not implement a second saga.
- **Critical — code presence is not acceptance evidence.** Existing disclosure, route, HAL, calendar, correction, remediation, MCP/federation, and discovery code remains unchecked where the task ledger says tests were deferred.
- **Critical — contract and UI adoption are incomplete.** Additive OpenAPI/NSwag generation, generated-client consumption, HAL-gated Blazor affordances, accessibility/localization, and removal of obsolete exact-location contracts remain open.
- **Critical — operations closure is incomplete.** Self-hosters still need reconciled EventLocation migration, disclosure activation, correction backlog/dead-letter, cache-convergence, review/remediation, and rollback/forward-repair guidance.

## Quick Resume

1. Read this file and `event-location-privacy-tasks.md` first.
2. Read only plan Sections 2, 8–16, and the current task’s phase; the plan contains historical evidence that must not be treated as current-head proof.
3. Read `optional-retained-erasure-authority-context.md` only when implementing or testing the typed platform-erasure adapter boundary.
4. Start with the first blocker-removal task, not the first file that already contains unverified production code.
5. Update `tasks.md` immediately when a substantial task passes acceptance; update this context after a decision, blocker, failed validation, phase completion, or handoff.

## Current Repository Reality

| Concern | Verified current state | CTO consequence |
|---|---|---|
| Event-local authority | `src/Explore.Domain/EventLocation.cs` exists and owns event-scoped disclosure state, explicit TBA, policy version, concurrency, review state, and soft deletion. | Keep `EventLocation` as the only event-local disclosure authority; do not restore a side-table policy model. |
| Disclosure evaluation | `EventLocationDisclosureEvaluator` and `EventLocationDisclosureService.ResolveManyAsync` exist. | All public/attendee/management projections must route through the service; task completion still requires query-budget, authorization, and negative-disclosure tests. |
| API contract | `src/Explore.API/Controllers/EventLocationController.cs` exposes public, attendee, management, update, review, and remediation routes. | Treat routes as additive and unverified until API/HAL/ProblemDetails/cache/tenant tests and generated contracts pass. |
| HAL authorization | EventLocation detail/collection policies and assembler registration exist. | UI may render mutation affordances only from `_links`; route guards and local roles remain UX-only. |
| Correction delivery | `LocationPrivacyCorrectionDispatcher` and concrete composite outbox routes exist. | Prove closed payloads, duplicate delivery, retry, unknown outcome, dead-letter visibility, reconciliation, and PII-free telemetry before release. |
| Platform-erasure adapter | Location/Home disposition and correction behavior exists across generalized erasure and correction services but lacks one accepted EventLocation adapter gate. | Define/prove only the typed EventLocation boundary here; platform orchestration stays external. |
| Migration baseline | Root `20260720162943_init` is the current EventLocation schema lane. | Revalidate every EventLocation/Location invariant from this artifact; removed staged-migration evidence is non-authoritative. |
| Build baseline | Release build passes with 0 errors and 41 warnings. | The repository is buildable; warnings include high-severity `System.Security.Cryptography.Xml` 10.0.7 advisories that require separate dependency remediation before an enterprise release. |

## Workstream Ownership Boundaries

| Workstream | Owns | Must not own |
|---|---|---|
| Event Location Privacy | `EventLocation` lifecycle, field policy, registration entitlement, disclosure service, routes/HAL, location projections, policy audit, correction intents, UI adoption, and location-specific migration constraints. | A second platform-wide account-erasure saga, generic provider cleanup framework, or separate authority topology. |
| Platform Privacy Erasure Authority | User fence/receipt/status, complete PII inventory/disposition, platform transaction, provider work, topology, replay, retention, restore, and completion semantics. | Event-specific disclosure decisions or browser affordance logic. |
| Home Discovery | Coarse governed discovery areas and any future separately consented spatial projection. | Reuse of exact `LocationPii`, `ShowCoordinates` as indexing consent, or browser-downloadable exact catalogs. |
| AI/MCP Disclosure | `IAiContextGateway` sensitivity ceiling and tool/resource contracts. | Bypassing EventLocation disclosure or treating sanitized output as authorization authority. |

## Locked Architecture Decisions

- Physical address and coordinates remain in `LocationPii`; `LocationKind` describes a place but never grants disclosure.
- `LocationPrivacyState` distinguishes `NOT_PROVIDED`, `ACTIVE`, and irreversible `ERASED`; an erased `Location` never accepts PII again.
- Public, attendee, and management are separate purposes and contracts. Public output is principal-invariant; attendee and management output is authenticated and `private, no-store`.
- Public contracts expose `EventLocationId`, not unrestricted physical `LocationId`.
- Registration authority comes from exact Event/Day/SessionSelection intent coverage and current lifecycle, never row existence.
- Server UTC controls delayed reveal. Client clocks and local claims do not influence disclosure.
- Tenant filters remain enabled for EventLocation reads/writes. The external platform adapter must supply persisted subject/tenant scope and fail closed on mismatch.
- Policy/audit/correction payloads contain bounded identifiers, versions, codes, and timestamps only; no address, coordinates, room text, provider response, or free-text error.
- Policy mutation and correction intent persist in one transaction; cache invalidation and external processing occur after commit.
- Every correction delivery and reconciliation attempt opens a fresh dependency scope, reloads persisted tenant/EventLocation ownership, and fails closed when ownership is missing or mismatched. Caller-supplied tenant or aggregate identifiers are routing hints, never authority.
- Sensitive responses are `no-store` or keyed by tenant, purpose, principal/entitlement, and policy version. Invalidation failure must not permit stale exact disclosure; it creates retryable convergence work, readiness degradation, and an operator alert.
- External correction calls never execute inside EventLocation policy transactions or migrations.
- Breaking pre-v1 contracts may be deleted once consumer migration and operator upgrade guidance are complete; no compatibility shims are required.

## Control and Data Flow

### Disclosure

1. A server-owned command attaches or resolves an `EventLocation` with fail-closed defaults.
2. A query builds purpose-specific requests keyed by `EventLocationId`.
3. `EventLocationDisclosureService` performs bounded tenant-scoped reads and one purpose-appropriate authority batch.
4. `EventLocationDisclosureEvaluator` applies lifecycle, purpose ceiling, governance, authorization/registration coverage, server-time reveal, field policy, and Home redaction.
5. API/MCP/federation/calendar consumers serialize only the resulting purpose-specific DTO.

### Policy mutation and correction

1. API authorization and concurrency tokens guard the command.
2. Application updates the aggregate, appends a PII-free audit, and writes the correction outbox intent transactionally.
3. Post-commit invalidation removes tenant/event/EventLocation projections; sensitive reads use `no-store` or policy-versioned keys so failed eviction cannot expose a superseded exact location.
4. The outbox processor creates a fresh scope, rebinds tenant and EventLocation ownership from persistence, fails closed on mismatch, performs idempotent correction, retries bounded failures, and retains dead-letter evidence for operator reconciliation.

### Platform-erasure adapter

1. The external platform workflow supplies a typed User intent plus persisted subject/tenant scope.
2. The EventLocation adapter reloads ownership, fails closed on mismatch, tombstones owned Home/room labels through domain invariants, and marks affected associations `NeedsPrivacyReview`.
3. It emits stable, PII-free EventLocation correction intents with no authority, receipt, provider, replay, or topology logic.
4. Event Location owns adapter integration, correction delivery, cache convergence, and remediation tests. The authority workstream owns the surrounding transaction and platform outcome.

## Enterprise Self-Hosting Contract

| Area | Required target behavior |
|---|---|
| Required services | PostgreSQL remains required. No broker, Redis, PostGIS, or external policy service becomes mandatory solely for location privacy. |
| Configuration | Keep EventLocation field/governance settings explicit, tenant-restrictive, auditable, and fail closed. No authority connection or secret is owned here. |
| Startup | Validate EventLocation schema/contract compatibility and keep exact disclosure disabled until the selected migration/consumer gate is satisfied. |
| Health | Expose bounded migration state, correction backlog/dead letters, cache convergence, and review backlog without identifiers or location text. |
| Upgrade | Use the approved pre-v1 EventLocation migration/contract sequence; breaking contracts are allowed after consumers move, but silent data loss or renewed anonymous exact disclosure is not. |
| Rollback | Before destructive contraction, additive rollback may be possible. After irreversible Location-state or contract activation, use forward repair and never resurrect exact PII. |
| Scale | Keep bounded batch sizes, no N+1 policy calls, and no per-row external calls. Introduce a saga or new infrastructure only from measured limits and a separate approved design. |
| Observability | Use closed-vocabulary counters and failure categories. No user IDs, tenant IDs, location IDs, addresses, coordinates, room text, endpoints, secrets, or exception text in metrics. |

## Release Gates

| Gate | Minimum evidence before green |
|---|---|
| G1 — Ownership convergence | ELP plan/context/tasks contain only EventLocation work and the typed external adapter boundary; no platform saga/topology/receipt/replay task remains. |
| G2 — Current migration integrity | Fresh PostgreSQL apply of the current root `init`; no pending EventLocation model changes; lookup/constraints/triggers/append-only/irreversible guards, forward repair, and contraction limits proven. |
| G3 — Disclosure authority | Evaluator matrix, bounded query/auth budget, wrong-tenant/missing-context denial, registration-scope coverage, server-time reveal, and Home/TBA/erased states pass. |
| G4 — API/HAL/contracts | Public-cookie equivalence, `private, no-store`, 401/403/409/RFC 7807 behavior, route-name/operation-ID/HAL parity, additive OpenAPI generation, and NSwag cleanliness pass. |
| G5 — Correction reliability | Transactional outbox creation, duplicate delivery, retry/backoff, unknown outcome, dead-letter visibility, fresh-scope persisted tenant rebinding, tenant-substitution denial, reconciliation, stale-cache prevention, readiness/alert behavior, and PII-free payload/telemetry pass. |
| G6 — Platform adapter and remediation | Persisted ownership rebinding, Home/room tombstoning, unrelated-location preservation, affected-EventLocation review marking, disposition idempotency, correction creation, and remediation pass. |
| G7 — Consumer convergence | Sessions, program, agenda, calendars, JSON-LD, email/notifications, webhook/export/report absence or projection proof, MCP/AI, federation/PDS, API keys, and Home Discovery have purpose-specific evidence. |
| G8 — Blazor and operator UX | Generated client consumed; every mutation gated by HAL; governance denial, concurrency, TBA, unavailable, review/remediation, localization, keyboard/focus, responsive, and RTL states pass. |
| G9 — Operations closure | EventLocation configuration, migration/activation, correction/dead-letter, cache convergence, review queue, troubleshooting, alerting, rollback, and forward-repair docs match shipped behavior. |
| G10 — Final repository gate | Release build plus intent-mandated project tests pass, generated artifacts are clean, no obsolete route/model remains, and plan/context/tasks agree. |

## Current Known Risks / Unknowns

| Severity | Risk | Required disposition |
|---|---|---|
| Blocker | Historical ELP migration evidence may hide missing invariants in the current clean baseline. | Produce an invariant inventory and real PostgreSQL proof from the current `init` migrations before activation. |
| Critical | Existing unverified code may contain tenant, cache, auth, or contract drift. | Keep tasks unchecked and run the exact risk-oriented owning tests; a build alone is insufficient. |
| Critical | A wrong-tenant correction or failed cache eviction could preserve stale exact disclosure. | Rebind ownership from persistence on every delivery, partition cache authority, fail closed, and prove tenant substitution plus invalidation-failure behavior. |
| Critical | The platform adapter boundary could grow into a second erasure orchestrator. | Limit ELP to typed Location/EventLocation disposition and correction behavior; keep fence, receipt, provider, topology, replay, retention, and restore acceptance external. |
| Critical | Outbound copies cannot always be recalled. | Correct owned projections, emit idempotent external correction where supported, and disclose retention limits without promising remote deletion. |
| Major | The workstream is too large for one review unit. | Preserve risk-boundary slices: migration/data, Application/API, correction/remediation, clients/UI, and operations/docs. |
| Major | Dependency security warnings remain in the green build. | Track and remediate the `System.Security.Cryptography.Xml` advisories separately before release; do not hide them as an ELP warning count. |

## Validation Baseline

- `dotnet build --configuration Release --verbosity quiet` — passed on 2026-07-22: 26 projects, 0 errors, 41 warnings.
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — non-green baseline on 2026-07-22: 286 total, 282 passed, 3 failed, 1 skipped. The unrelated failures are repository naming, the organization-centric scope-file guardrail finding multiple matches, and explicit HATEOAS permission metadata in existing EventReport/EmailDispatch policies.
- The 41 warnings are pre-existing package advisories, including high-severity `System.Security.Cryptography.Xml` 10.0.7 advisories. This context change does not resolve them.
- Historical focused ELP receipts remain useful for regression targeting, but any receipt tied to removed staged migrations, obsolete hashes, or older snapshots is not current-head release evidence.
- Required documentation checks for this review: `git diff --check -- dev/active/event-location-privacy` and the Senior CTO architecture-test hook.

## Handoff Notes

### Handoff — 2026-07-22 Europe/Brussels

- **Current state:** Core EventLocation architecture is approved and the workstream is now EventLocation-only; implementation is partial and release activation remains blocked by migration, verification, contracts, adapter/remediation, UI, and operations gates.
- **Next action:** Review the re-baselined plan/tasks, then prove the current root migration and existing EventLocation application/API paths before further product edits.
- **Blockers:** G1 ownership documentation is re-baselined and awaits review; G2 current-migration proof is the first implementation blocker. No platform-erasure implementation may be added here beyond the typed EventLocation adapter.
- **Modified files:** all three Event Location planning artifacts, all three authority planning artifacts, and the superseded `.omo` work-plan header.
- **Validation:** Fresh root Release build passed; `git diff --check` passed. The architecture suite is non-green on three unrelated existing code failures recorded in the validation baseline.
- **Documentation impact:** Plan/context/tasks were re-baselined together; platform-erasure information moved to the authority workstream.
- **Risks:** Preserve unrelated dirty-worktree changes. Do not treat historical hashes, old migration IDs, or code presence as completion evidence.
- **Notes for next contributor/agent:** Teach the implementation and verification result in the final summary; keep `tasks.md` authoritative and update this context only at meaningful state changes.
