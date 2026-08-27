<!-- ABOUTME: Active context for the re-baselined Event Ticketing Lifecycle workstream. -->
<!-- ABOUTME: Records exact review state, verified decisions, blockers, resume point, and implementation handoff. -->

# Event Ticketing Lifecycle — Context

Last Updated: 2026-08-27 Europe/Brussels

## Review State

- **Plan revision:** SHA-256 `84bcd73f5d603fcd24f1a4cf9aaeef5e7f041a36e8459b83b173582ea25e24fa`
- **Tasks revision:** SHA-256 `0373aa09e4555fda371e073eee17ab7b0bb8ebfeaa4c9c5591268f7813b5397b`
- **I-VSD report:** [`i-vsd-event-ticketing-lifecycle.md`](../../../islamic-value-sensitive-design/i-vsd-event-ticketing-lifecycle.md)
- **I-VSD revision:** SHA-256 `8cbacacba7be2268501ed703337534e3659ea0095812ca074524452249b0b128`
- **I-VSD status / disposition:** current / plan-aligned.
- **I-VSD reviewed inputs:** plan `84bcd73f...`; tasks `0373aa09...`.
- **Clean-room evidence revision:** SHA-256 `7c02d45448df2ba332e5684b5bf0de4d60cc7d002937535179ee2b2fec29168c`
- **CTO review:** [`event-ticketing-lifecycle-cto-review.md`](event-ticketing-lifecycle-cto-review.md), SHA-256 `135cdc439e63727a0299dea81cea61245e66d9aa41afbdc7437ec89013d9d470`, records fresh read-only **Approve**.
- **User approval:** exact rewritten revision not yet approved.

## SESSION PROGRESS (2026-08-27 Europe/Brussels)

### COMPLETED

The forms-focused predecessor remains closed at implemented Phase 21. No successor product code has been implemented.

The successor planning artifacts have been materially re-baselined:

- implementation is a mandatory dependency-bound named PR train from `FND` through `REL`, not an omnibus PR;
- Phase 0 remediates lifecycle authority debt before adding states;
- purchase ceilings are honest per access mode;
- durable business idempotency outlives HTTP middleware retention;
- transfer, waitlist, add-on, and recovery behaviors now include explicit negative/race/crash scenarios;
- one canonical transaction/lock order owns high-risk transitions;
- recovery starts fail-closed and rotates pre-restore bearer authority;
- tests compile before Red/Green evidence and run at the correct seam;
- mutation, zero-PII, MAD, deterministic performance/recovery contracts, and selected phase evidence have named ownership;
- code-comment and canonical `docs/` responsibilities are phase-owned; and
- protected delayed payout remains absent through a machine-readable release ratchet.

The exact-revision governance gates now have independent evidence:

- I-VSD is `current / plan-aligned` for plan `84bcd73f...` and tasks `0373aa09...`;
- every `IVSD-F001` through `IVSD-F007` mitigation maps to S1-S7/WB-1 and Tasks 1.1-9.4; and
- a fresh read-only CTO review approved those exact plan/tasks and I-VSD `8cbacacb...` revisions.

### IN PROGRESS

- No implementation task is active.
- The next gate is explicit user approval of plan `84bcd73f...`, tasks `0373aa09...`, I-VSD `8cbacacb...`, and CTO review `135cdc43...`.

### NEXT

1. Complete the scoped build and architecture baseline in the isolated worktree.
2. Begin `FND` / Task 0.1 RED.

### BLOCKERS

- No implementation blocker remains after isolation; production launch gates remain external.
- Production launch still requires provider, legal/tax, qualified scholarly, accessibility, privacy, security, stakeholder, and staffed operator evidence.

## Quick Resume

Do **not** start Task 0.1 yet.

1. Resume product work in `/home/amir/ISLAMU/Github/Event-ticketing-lifecycle` on branch `work/event-ticketing-lifecycle`, based on approved `develop` HEAD `558a23210...`.
2. Finish the one-time Release build and architecture baseline.
3. Begin Phase 0 / `FND` / Task 0.1 RED.

## Key Files And Responsibilities

| File | Responsibility |
|---|---|
| [`event-ticketing-lifecycle-plan.md`](event-ticketing-lifecycle-plan.md) | Decision-complete requirements, scenarios, architecture, authoritative PR DAG, phase contracts, rollback, and approval policy. |
| [`event-ticketing-lifecycle-tasks.md`](event-ticketing-lifecycle-tasks.md) | Hot execution ledger: 51 atomic tasks, exact ownership/effort, Red/Green commands, and selected phase gates. |
| [`event-ticketing-lifecycle-context.md`](event-ticketing-lifecycle-context.md) | Review state, exact revisions, resume point, validated repository reality, risks, and handoff. |
| [`event-ticketing-lifecycle-cto-review.md`](event-ticketing-lifecycle-cto-review.md) | Rewrite-mode CTO findings and honest `Defer`; never self-approves the rewritten revision. |
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
- Normal lifecycle state authority is not fully consolidated; some persistence conditionals and HAL checks reconstruct decisions.
- Transfer, waitlist/offer, event add-on, source-rebinding, and successor recovery types do not exist.
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

- Project knowledge-graph and Context7 MCP tools were not registered in this session.
- Configured web-search providers returned no results.
- Repository evidence was therefore verified through available LSP attempts, bounded source/docs reads, and shell text/location search.
- External constraints were retrieved directly from official Stripe, Microsoft, PostgreSQL, and OWASP documentation and sanitized in the clean-room packet.
- No product build/test/runtime was appropriate for this markdown-only planning rewrite.

## Validation Baseline

- **Planning rewrite baseline:** no product build or .NET test was run, as required for markdown-only work.
- **Planning validation:** task inventory/uniqueness, Red-before-Green order, phase gates, scenario mappings, test-project existence, local links, ABOUTME headers, whitespace, forbidden verification lanes, and hash/status coherence must all be clean after final hash binding.
- **Implementation base:** isolated conflict-free worktree `/home/amir/ISLAMU/Github/Event-ticketing-lifecycle`, branch `work/event-ticketing-lifecycle`, HEAD `558a23210522cab125c0d379499dec51a2b0413b`.
- **Implementation approval:** the user's 2026-08-27 continuation instruction approved proceeding after the exact revision gate was presented.
- **Implementation baseline after approval:** the Release build at `558a23210...` exited 0 with 15,217 pre-existing analyzer warnings and regenerated 666 blank lines in `EventApiClient.g.cs`. `Event.Architecture.Tests` then failed 12 pre-existing contracts, including agent-context routing, repository entity boundaries, controller-size ratchets, authorization inventories, DTO naming/OpenAPI enum registration, and generated-client boundaries. No ticketing product edit preceded these failures.
- **Baseline recovery:** clean detached baselines at `origin/develop` and the descendant API-hardening commit `a2a4e3026...` are being evaluated to identify the newest conflict-free green implementation base without altering unrelated unmerged work.
- **Migration-history classification:** every application/provider migration present at isolated HEAD is tracked in committed history. Treat all as applied/merged and immutable; ticketing schema changes must generate new forward corrective migrations for every affected provider. No existing migration or snapshot may be removed, renamed, rewritten, or hand-edited.
- Do not rerun an unchanged baseline; use focused TUnit selectors during active work and the selected project only at phase closeout.

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
- implementation before exact-revision gates close.

## Current Known Risks / Unknowns

- The first implementation agent may bypass Phase 0 and extend existing oversized seams.
- A provider/payment path may acquire rows outside the canonical lock order and recreate WB-1.
- Recovery may be tested as object-level serialization rather than a real clean-storage restore.
- UI/HAL may reconstruct lifecycle decisions rather than consume domain-authoritative facts.
- Mutation, PII, MAD, docs, or focused verification may be deferred to release instead of owned per PR.
- External evidence may be mistaken for technical or production approval.

## Handoff Notes

### Handoff — 2026-08-27 Europe/Brussels

**Current workstream:** re-baselined planning, no product implementation.

**Next owner:** implementation agent in the isolated worktree.

**Do not do next:** baseline/build, Task 0.1, migration generation, endpoint/UI work, or protected payout.

**Required first evidence:** finish the one-time baseline, then create the Task 0.1 architecture RED contract before production edits.
