<!-- ABOUTME: Active context for the re-baselined Event Ticketing Lifecycle workstream. -->
<!-- ABOUTME: Records exact review state, verified decisions, blockers, resume point, and implementation handoff. -->

# Event Ticketing Lifecycle — Context

Last Updated: 2026-08-28 Europe/Brussels

## Review State

- **Reviewed plan revision:** SHA-256 `84bcd73f5d603fcd24f1a4cf9aaeef5e7f041a36e8459b83b173582ea25e24fa`
- **Reviewed tasks revision:** SHA-256 `0373aa09e4555fda371e073eee17ab7b0bb8ebfeaa4c9c5591268f7813b5397b`
- **I-VSD report:** [`i-vsd-event-ticketing-lifecycle.md`](../../../islamic-value-sensitive-design/i-vsd-event-ticketing-lifecycle.md)
- **I-VSD revision:** SHA-256 `8cbacacba7be2268501ed703337534e3659ea0095812ca074524452249b0b128`
- **I-VSD status / disposition:** current / plan-aligned.
- **I-VSD reviewed inputs:** plan `84bcd73f...`; tasks `0373aa09...`.
- **Clean-room evidence revision:** SHA-256 `7c02d45448df2ba332e5684b5bf0de4d60cc7d002937535179ee2b2fec29168c`
- **CTO review:** [`event-ticketing-lifecycle-cto-review.md`](event-ticketing-lifecycle-cto-review.md), SHA-256 `135cdc439e63727a0299dea81cea61245e66d9aa41afbdc7437ec89013d9d470`, records fresh read-only **Approve**.
- **User approval:** implementation continuation was approved on 2026-08-27; the delivered ticketing scope was merged into `develop` on 2026-08-28.
- **Status-update effect on I-VSD:** this reconciliation changes execution status and evidence locations only. It does not change provider-controlled behavior, scenarios, mitigations, or authority boundaries, so the existing I-VSD review remains current under its refresh contract.

## SESSION PROGRESS (2026-08-28 Europe/Brussels)

### COMPLETED

The ticketing implementation branch was integrated into `develop` and then removed. Ticketing integration merge `1b754c51d` (`merge(ticketing): finalize generated endpoint contracts`) contains ticketing tip `79c36305d`.

Implemented and merged:

- Phase 0 lifecycle-authority remediation and architecture ratchets;
- access-mode-aware purchase governance with durable authority/idempotency;
- participant admission readiness and subject-correct consent/approval;
- credential-rotating transfer without resale;
- deterministic fair-return waitlist/allocation and durable orchestration;
- API/HAL/OpenAPI, BFF, generated-client, and Blazor surfaces for those capabilities;
- tenant-scoped persistence and regenerated multi-provider migrations;
- Phase 0, 1, 3, and 5 mutation/PII/MAD evidence manifests; and
- generated contract inventory corrections plus convention-owned table naming.

Verification recorded during integration:

- Release solution build passed with 0 errors;
- Domain 1,067 passed; Application 4,819 passed; Infrastructure 1,653 passed;
- generated contracts 16 passed; Blazor integration 495 passed;
- focused ticketing persistence 24 passed;
- ticketing application mutation lane 105 passed and domain mutation lane 31 passed;
- focused purchase, readiness, admission, transfer, fair-return, HAL, BFF, and component contracts passed; and
- ticketing-specific architecture failures were cleared, while the full architecture project retained 12 inherited non-ticketing failures.

Post-merge task audit:

- implementation and regression-test surfaces are present through Phase 6;
- 0/51 task checkboxes are claimed complete because the ledger requires exact task-level evidence, not implementation presence alone;
- historical RED-before-GREEN assertion-failure transcripts are not retained and must not be fabricated retrospectively;
- Phase 0, 1, 3, and 5 retain partial critical evidence, but some exact mutation-report paths and focused command transcripts are absent;
- Phase 2 and 4 retain implemented/tested surfaces without phase evidence manifests; and
- Phase 6 additionally lacks its evidence manifest, three mutation reports, and deterministic scale transcripts.

### IN PROGRESS

- No runtime edit is active.
- Every Phase 0–6 task checkbox remains open under the exact evidence contract even where implementation is merged.
- Phases 7–9 remain unimplemented.

### NEXT

1. Decide the governance disposition for unrecoverable historical RED transcripts; do not recreate fake RED evidence against merged code.
2. Retain the missing exact task command/report evidence, beginning with the Phase 6 manifest, mutation reports, and scale results.
3. Resolve the inherited architecture baseline separately; do not misclassify its 12 failures as ticketing regressions.
4. Begin Phase 7 / Task 7.1 RED only after the merged Phase 0–6 ledger is honestly reconciled.

### BLOCKERS

- Live API curl QA is blocked by the existing fail-closed privacy-erasure replay gate before the listener opens. Do not bypass or weaken the gate; use the repository integration host until the authority replay environment is healthy.
- Full API and persistence phase-closeout runs have no retained successful transcript; focused ticketing selectors passed.
- Historical RED transcripts are not recoverable from repository state alone. Closing those task clauses requires explicit acceptance of substitute evidence or recovery of the original execution record.
- Production launch still requires provider, legal/tax, qualified scholarly, accessibility, privacy, security, stakeholder, and staffed operator evidence.

## Quick Resume

1. Resume from `develop` at or after `1b754c51d`; all ticketing branches/worktrees were deleted after merge.
2. Preserve unrelated dirty persistence/projection and migration-test files already present in the main worktree.
3. Resolve the RED-evidence governance gap and retain exact Phase 0–6 closeout artifacts.
4. Begin Phase 7 / Task 7.1 RED only after that reconciliation.

## Key Files And Responsibilities

| File | Responsibility |
|---|---|
| [`event-ticketing-lifecycle-plan.md`](event-ticketing-lifecycle-plan.md) | Decision-complete requirements, scenarios, architecture, authoritative PR DAG, phase contracts, rollback, and approval policy. |
| [`event-ticketing-lifecycle-tasks.md`](event-ticketing-lifecycle-tasks.md) | Hot execution ledger: 51 atomic tasks, exact ownership/effort, Red/Green commands, and selected phase gates. |
| [`event-ticketing-lifecycle-context.md`](event-ticketing-lifecycle-context.md) | Review state, exact revisions, resume point, validated repository reality, risks, and handoff. |
| [`event-ticketing-lifecycle-cto-review.md`](event-ticketing-lifecycle-cto-review.md) | Revision-bound read-only CTO review and approval evidence; it does not grant user approval. |
| [`event-ticketing-lifecycle-clean-room-evidence.md`](event-ticketing-lifecycle-clean-room-evidence.md) | Source-free official-documentation constraints and provenance boundary. |
| [`i-vsd-event-ticketing-lifecycle.md`](../../../islamic-value-sensitive-design/i-vsd-event-ticketing-lifecycle.md) | Independent Islamic value-sensitive findings, mitigations, scenario mappings, and reviewed-input state. |
| `docs/PAYMENTS.md`, `SECURITY.md`, `docs/SECURITY-MODEL.md`, `docs/PRIVACY_ERASURE.md`, `docs/OPERATIONS.md` | Canonical money, authority/privacy, recovery, observability, and operator behavior. |
| `docs/API_CONTRACT_INVENTORY.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/TESTING.md`, `docs/BLAZOR_DEV_WORKFLOW.md` | Generated-contract, public API, component, accessibility, and BFF/UI workflow behavior. |
| `.env.example`, `docs/releases/changes/`, `docs/API_CHANGELOG.md` | Secret schema, structured release input, and intentional breaking-change disclosure. |

## Constraints And Rules To Remember

- Review gates block implementation or production claims as named; they do not authorize compatibility shims or protected payout.
- Generated EF Core migrations and model snapshots are never hand-edited.
- Repositories return entities; handlers map contracts and manually instantiate validators.
- All writes are authorized; UI action affordances come only from HAL links.
- Secrets originate only from Infisical or `.env`; `.env.example` owns schema, never values.
- Provider I/O is outside database transactions; durable intent is committed first and reconciled monotonically.
- Superseded development-mode behavior/contracts/tests/docs are deleted in the owning PR.

## Verified Repository Reality

- Clean Architecture, CQRS/MediatR, EF Core, tenant filters, `IUnitOfWork`, transactional outbox, Quartz, HAL, BFF, generated clients, payment reconciliation, inventory holds, admission credentials, and real PostgreSQL tests exist.
- Existing `RegistrationOrderLifecycleService*`, `RegistrationInventoryRepository`, and order-creation seams are already large.
- Touched order/admission lifecycle decisions now flow through semantic aggregate rules and capability-specific coordinators; persistence exposes transaction-bound primitives rather than owning lifecycle policy.
- Purchase governance, participant readiness, transfer, fair-return waitlist/offer, source rebinding, and durable fair-return orchestration types exist across Domain, Application, Persistence, API/HAL, BFF, generated contracts, and Blazor.
- Event add-on, successor recovery/operator, deployment capability-matrix, and final convergence types/tests do not exist.
- Existing PostgreSQL repositories establish ordered `FOR UPDATE` patterns and retry-aware transaction ownership that successor work must reuse.
- Existing admission operations establish fixed-cardinality telemetry and p95 250 ms / p99 500 ms at 50-concurrent-request targets.
- Existing Data Protection, payment reconciliation, outbox, Quartz, and authority-recovery behavior makes keys/fences/cursors part of restore correctness.

## Key Decisions

1. **Semantic authority first:** aggregate methods and one domain decision surface own normal state decisions; persistence is not business authority.
2. **No generic framework:** capability-specific state/coordinator types only; no workflow engine, rules engine, generic repository, or new service boundary.
3. **Mandatory PR graph:** named dependency order in plan Section 6; every PR owns Red/Green/refactor, docs, generated output, and focused evidence; each phase runs one build plus one selected project.
4. **Honest access guarantees:** hard cross-order ceilings require stable account, verified-contact, or server-proven actor authority; name-only uses per-order/capacity/abuse controls.
5. **Canonical lock order:** tenant/event/type → capacity pools → supply/release → buyer binding/order → payment attempt → refund operation.
6. **Provider I/O outside transactions:** local durable intent/effect state is committed before dispatch and reconciled monotonically.
7. **Durable business idempotency:** HTTP cache expiry/restore cannot duplicate money or authority.
8. **Commercial equivalence:** buyer-transparent rebinding requires equality across tenant/event/type/policy/currency/terms/admission/gross/refund-funding dimensions.
9. **Recovery policy:** recovery mode, deployment stop-sale, clean manifest validation, mandatory capability cancellation and credential rotation/reissue before reopen.
10. **No compatibility:** delete superseded code/contracts/tests/docs in the owning PR; preserve immutable audit/payment/check-in facts only.
11. **No protected payout:** absence is structural and machine-validated, not a dormant feature flag.

## Evidence And Limitations

### Pre-Rewrite Revisions Audited

- Plan `ea960c322abf325e59277458a13c8fe889e0be45970ff344366e00f3dd9c3cc3`
- Tasks `5ff89901991d8146e3a3d9c6d2c2a5ebceb5d1e41b51d56805a5648d028f8e2f`
- Context `b0eff98300fe43b90b9c8f8fcaee537d5ab9cd923fabff42d8ca72f98f94f682`
- I-VSD `4884a9d3e283f55928039d2f4c8d1bfad3e0df7b1b698e00bb4445cef15288b4`
- Clean-room packet `8aa2a26be819b507c7fb8927c0ed15586641b4886156687ad06e7bb07adc8d55`

### Review Findings Incorporated

- 4-point right-sizing failure and exact PR split.
- lifecycle authority/oversized-seam remediation.
- name-only ceiling impossibility.
- payment-handoff/seller-withdrawal/refund crash split-brain.
- transfer/check-in/consent/approval/reissue shared-fence gap.
- add-on overflow/conservation gap.
- point-in-time bearer resurrection after restore.
- invalid stale `--no-build` Red/Green commands and seam-mismatched tests.
- missing mutation/PII/MAD/performance/recovery ownership.
- stale/unbound I-VSD metadata.

### Tool/Evidence Limits

- The merge/repair session retained exact focused test totals and final build output, but the long-running full API and persistence sessions expired without retained transcripts.
- Live development-host startup reached migrations and Quartz initialization, then failed closed at `PrivacyErasureStartupGate`; no curl response could be captured.
- The integration host remains the verified HTTP surface for ticketing routes until the local privacy-erasure authority replay environment is healthy.
- This status-only dev-doc update does not rerun product tests; it records already captured implementation evidence.

## Validation Baseline

- **Current integration head:** `develop` at `1b754c51de1c01e6999b5df996bcc554f0e113d1`.
- **Merged ticketing tip:** `79c36305dfaa77e6f2bf8f7f8097f4822964ff6c`, proven an ancestor of `develop`.
- **Ticketing cleanup:** no local `*ticketing*` branch or ticketing worktree remains.
- **Implementation approval:** the user's 2026-08-27 continuation instruction approved proceeding after the exact revision gate was presented.
- **Merged Release build:** passed with 0 errors.
- **Project evidence:** Domain 1,067; Application 4,819; Infrastructure 1,653; generated contracts 16; Blazor integration 495.
- **Focused evidence:** ticketing persistence 24; application mutation lane 105; domain mutation lane 31; focused API/HAL/BFF/component selectors passed.
- **Known inherited baseline:** full `Event.Architecture.Tests` retains 12 non-ticketing failures; all ticketing-specific architecture failures were cleared.
- **Task-ledger audit:** implementation surfaces are merged, but no Phase 0–6 task checkbox is treated as evidence-closed because exact RED chronology and/or task command/report artifacts are missing.
- **Migration-history classification:** generated ticketing migrations are merged for PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL and are immutable. Future changes require forward generated migrations; never hand-edit migrations or snapshots.
- **Planning validation for this update:** markdown whitespace, local links, stale status/worktree references, and triad coherence only. Do not rerun unchanged product evidence.

## Scope Boundary

In scope:

- lifecycle authority remediation;
- purchase governance;
- participant completion/approval/admission readiness;
- transfer/credential rotation;
- fair return/waitlist;
- event-bound add-ons;
- recovery/operator controls;
- deployment capability matrix and release evidence.

Out of scope:

- protected delayed payout;
- attendee resale or peer-to-peer money movement;
- tax, invoice, accounting, CRM, donor, volunteer, inventory, or kiosk systems;
- new dependency/framework selection;
- protected payout implementation without its separate I-VSD/ADR/workstream.

## Current Known Risks / Unknowns

- Phase 6 evidence may be overclaimed if Task 6.8 is checked without the missing mutation-summary/evidence manifest.
- A provider/payment path may acquire rows outside the canonical lock order and recreate WB-1.
- Recovery may be tested as object-level serialization rather than a real clean-storage restore.
- UI/HAL may reconstruct lifecycle decisions rather than consume domain-authoritative facts.
- Phases 7–9 may accidentally be treated as delivered because the ticketing branch was merged; their planned tests and implementation files are absent.
- External evidence may be mistaken for technical or production approval.

## Handoff Notes

### Handoff — 2026-08-28 Europe/Brussels

**Current workstream:** purchase governance, readiness, transfer, and fair-return lifecycle merged into `develop`; Phase 6 closeout and Phases 7–9 remain.

**Next owner:** implementation agent continuing from `develop`.

**Do not do next:** recreate deleted ticketing branches/worktrees, weaken the privacy-erasure startup gate, mark Phase 6 complete without retained evidence, touch unrelated dirty persistence files, or implement protected payout.

**Required first evidence:** retain the complete Task 6.8 mutation/evidence artifacts and pass the focused evidence contract before starting Task 7.1 RED.
