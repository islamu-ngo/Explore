<!-- ABOUTME: Hot execution ledger for the Registration Data Collection & Participation Platform workstream. -->
<!-- ABOUTME: Mirrors the plan's phases/tasks exactly; implementation agents keep this current during work. -->

# Registration Data Collection & Participation Platform — Task Checklist

Last Updated: 2026-07-31 Europe/Brussels

## Status Summary
- **Overall status:** Phase 5 Tasks 5.2–5.10 remain confirmed. Order persistence/holds, API/security/HAL, the order-centric cutover, and server-authoritative contribution checkout composition are complete. The registration-order lifecycle source receipt is now frozen at `RegistrationOrderLifecycleResponseDto` / `GuestRegistrationOrderLifecycleResponseDto`; focused Application/API/Blazor lanes and the canonical Release build are green. Phase 5 is complete under the shared-baseline blocker policy; the full Architecture baseline still has unrelated shared failures, while migration/EF acceptance is green and database rollout remains unproved
- **Checkbox count:** 32/88 implementation-task boxes are checked. This is not Phase 5 or workstream completion
- **Current priority:** Task 6.1, participant + PII + assignment domain model, test-first across all five `ParticipantDataCollectionMode` cases
- **Next recommended slice:** Complete and independently verify Task 6.1 before starting 6.2; preserve buyer != participant, split PII, concrete order-line assignment, per-line multiset enforcement, and HAL-only Studio/attendee affordances
- **Latest focused evidence:** Contract repair selectors: pure interface 1/1; Application 23/23; API 11/11; Blazor service/capability/ticket/recovery 1+3+4+26; API inventory generator 1/1; API and Blazor builds 0 errors; canonical Release build 0 errors; EF reports no pending model changes; diagnostics and `git diff --check` are clean. Full Architecture is 327/337 passed, 9 failed, 1 governed skip; DTO naming leaves only unrelated `EventOrganizerClaimReviewDecision`

## Implementation Maintenance Rules
- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark a substantial task `🟡 IN PROGRESS` when it will span multiple edits or a handoff; skip this churn for tiny tasks completed immediately.
- Check a substantial completed task immediately; reconcile small completed tasks no later than phase end.
- Add discovered work under its owning phase with acceptance criteria and dependencies; keep completed count, priority, next slice, deferred work, and update date accurate.
- Check a phase complete only after all implementation AND phase-verification checkboxes pass.
- Update context after a phase, decision, blocker, validation failure, material discovery, or handoff.
- Update the plan only when scope, architecture, sequencing, acceptance criteria, risk, or validation strategy changes.
- Do not run build/tests after individual tasks; verify once at phase end.
- Do not start the app, browser, Docker, Aspire, Playwright, Chrome DevTools MCP, or live services for verification.
- ✅ **Migration artifact verified:** commit `ff30795a2 feat(persistence/ticketing): add participation schema` owns `src/Explore.Persistence/Migrations/20260728152646_AddParticipationHandlingModes.cs`, its designer, and `ExploreDbContextModelSnapshot.cs`. The migration contains participation configuration, event ticket catalogs/types/entitlements/capacity pools, platform fee policies/fixed charges, and contribution settings/options. This proves a committed migration artifact, not database application or runtime rollout.
- ✅ **Legacy-pricing migration artifact verified:** generated additive migration `src/Explore.Persistence/Migrations/20260729183118_RemoveLegacyEventPricing.cs`, its designer, and the snapshot drop the two nonnegative-price checks plus Event/EventSession `price` and `currency_code`. `dotnet ef migrations has-pending-model-changes` reports no changes. Neither result proves database application.
- ⚠️ **Standing gate — NO Hi.Events code copy (plan §4.13, D19):** ISLAMU's CLA enables dual-licensing; copying any AGPLv3 code/snippet/SQL/asset from the Hi.Events repository is forbidden. Clean-room implementation from `hi-events-report.md` + this plan only; never open the Hi.Events repo while coding. The report's §10 code-reuse permission is overridden.

## Phase 0: Governance, ADRs, And Contract Intent ⚠️ IMPLEMENTATION COMPLETE / EXTERNAL TEST BLOCKER
- [x] **0.1 Author ADR-016 (bounded context & provider channels)**
  - **Files:** `docs/adr/ADR-016-registration-data-collection-context.md` (new)
  - **Acceptance:** D1/D2/D3/D5/D6/D7/D14 recorded; §24 anti-patterns as rejected alternatives; ADR-015 format followed
  - **Effort:** M — **Dependencies:** none
- [x] **0.2 Author ADR-017 (participation authority) + ADR-018 (order/ticket aggregate)**
  - **Files:** `docs/adr/ADR-017-event-participation-authority-model.md` (new), `docs/adr/ADR-018-registration-order-ticketing-aggregate.md` (new)
  - **Acceptance:** D8/D9/D10/D12 and D4/D11/D16/D17/D18 recorded; §33 anti-patterns; payment boundary named; Hi.Events adopt/adapt/reject boundary + CLA/dual-licensing **no-code-copy** rule recorded (report §9–§10, D19; §10 override stated)
  - **Effort:** M — **Dependencies:** 0.1
- [x] **0.3 Add `registration-data-collection` intent to `.claude/contract/intents.yaml`**
  - **Files:** `.claude/contract/intents.yaml` (existing)
  - **Acceptance:** YAML valid; full 8-question contract; cross-references the three dev docs; architecture tests green
  - **Effort:** M — **Dependencies:** 0.1, 0.2
- [x] **0.4 Resolve migration-baseline ordering (erasure workstream)**
  - **Files:** context file (this dir); observe `src/Explore.Persistence/Migrations/`
  - **Acceptance:** Baseline lane order recorded; later registration migrations require explicit owner/order before generation; blocker updated
  - **Effort:** S — **Dependencies:** none

### Phase 0 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors; pre-existing NuGet advisory warnings remain
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — executed: 302 passed, 1 skipped, 1 unrelated pre-existing event-reporting failure; registration intent schema validated separately

## Phase 1: Event Provenance, Organizer Authority, And Public Actions 🟡 IN PROGRESS
- [x] **1.1 Provenance typed state on `Event` + `ActorId` semantics decision**
  - **Files:** `src/Explore.Domain/Event.cs` (existing), `EventProvenanceType.cs` + enum (new), `Services/Registration/EventAuthorityRules.cs` (new)
  - **Acceptance:** `IsUserReported`/`EventUrl` gone from `src/`; fail-closed authority rules tested; provenance required (no implicit default); `ActorId` decision recorded
  - **Effort:** L — **Dependencies:** 0.4 (persistence gate only)
  - **Progress:** backend runtime `IsUserReported`/external `EventUrl` removed; all four creation lanes assign typed provenance explicitly (`CreateEvent`, generic import, ATProto import, development seed); generated API/client contracts now expose typed provenance and public actions. Global `Actor` identity is retained while tenant participation is checked separately.
- [x] **1.2 `EventPublicAction` + kinds + health states + `ExternalActionUrl` value object**
  - **Files:** new domain files per plan; `ValueObjects/ExternalActionUrl.cs` (new)
  - **Acceptance:** dangerous schemes rejected; ≤1 primary action; zero actions valid
  - **Effort:** M — **Dependencies:** 1.1
- [x] **1.3 `EventOrganizerClaim` aggregate**
  - **Files:** `EventOrganizerClaim.cs`, `EventOrganizerClaimStatus.cs` + enum (new)
  - **Acceptance:** transition methods enforced; approval only sets organizer, never grants historical data
  - **Effort:** M — **Dependencies:** 1.1
- [ ] **1.4 Persistence for Phase 1 entities — VERIFICATION EXECUTED / DATABASE APPLICATION UNPROVED**
  - **Files:** 6 new configurations; DbSets/QueryFilters/LookupTableSeeder (existing); repositories (new)
  - **Acceptance:** seeder parity; tenant-filter test; one-primary filtered unique index; committed additive migration artifact verified without claiming database application
  - **Effort:** L — **Dependencies:** 1.1–1.3
  - **Progress:** model/configuration/seeder/tests/DBML implemented. Commit `ff30795a2` owns the additive participation/ticketing migration, designer, and snapshot. The generated `20260729183118_RemoveLegacyEventPricing` migration removes legacy pricing schema, and EF reports no pending model changes. Database application remains unproved.
- [x] **1.5 Application features — actions, claims, provenance exposure**
  - **Files:** `Features/EventPublicActions/**`, `Features/EventOrganizerClaims/**` (new); `Features/Events/**` DTOs (existing)
  - **Acceptance:** contributor forbidden from registration/ticket/attendee ops; claim approval transactional + retry-idempotent; no capability booleans in DTOs
  - **Effort:** L — **Dependencies:** 1.4
  - **Progress:** normalized provenance/active-action Event DTO exposure; concrete action/claim repositories; action CRUD/query; claimant-authorized claim submit/withdraw/query; transactional retry-idempotent review; existing report intake reused for correction/unsafe-link submissions. Focused Application tests pass 26/26.
- [ ] **1.6 API + Cerbos + HAL (claims/actions/provenance)**
  - **Files:** 2 new controllers, 2 new link policies, `RouteNames`/`LinkRelations` (existing), `islamuevent_event.yaml` (existing), `islamuevent_event_organizer_claim.yaml` (new); contract regeneration
  - **Acceptance:** classification/contract tests green; no open-redirect endpoint; Cerbos parity deny-by-default; changelog updated
  - **Effort:** L — **Dependencies:** 1.5
  - **Progress:** controllers, stored-ID redirect, independent HAL policies/assemblers, route/relation constants, fallback authorization, Cerbos event/claim policies and schemas, checked-in OpenAPI/client contracts, and API/AUTHORIZATION/changelog docs are implemented. Final repair routes every event-bound claim check through `EventOrganizerClaim`, preserves server-only event/claim metadata, separates Event/claim action allowlists, denies claim machines before admin scope, enforces current tenant claimant eligibility for submission/listing and approval-time transactional revalidation, state-gates withdraw/review candidates, suppresses public/claim/report affordances for non-public events, and exposes fixed external-link safety guidance. Withdrawal now uses `withdraw-organizer-claim`; authorization loads the persisted claim and claimant actor, keeps personal-user control unchanged, and grants organization/group control through exact `PermissionCodes.EventCreate` repository permission sets carried separately into fallback batches and Cerbos principals. Unrelated permission scopes, curators without control, instance admins, and machines are denied. As a revocation path, the withdrawal handler repeats ownership/controller authorization without current organizer-eligibility gating, so a controller can withdraw after eligibility loss. The full Infrastructure suite previously passed 1,104/1,104; the final focused authorization rerun passes 115/115 and the exact architecture guard passes 1/1. The latest API Release rerun is now blocked by five unrelated concurrent `AtprotoJwtService` errors; focused Application/API HAL execution and native Cerbos execution remain externally blocked.
- [x] **1.7 Blazor — badge, provenance panel, claim/correction flows**
  - **Files:** event card/detail components (existing), `EventProvenancePanel.razor` + claim dialogs (new)
  - **Acceptance:** badge non-removable, provenance-derived; affordances `_links`-gated (bUnit); RTL/accessible
  - **Effort:** M — **Dependencies:** 1.6
  - **Progress:** every event-card layout renders immutable `COMMUNITY_REPORTED` disclosure from typed `EventListDto.provenanceTypeCode`; detail and preview surfaces reuse an RTL-safe provenance panel. Correction, unsafe-link, claim, claimant withdrawal, source, and public-action affordances are HAL-gated. Public actions display destination domains but open only sanitized item redirect links. Full Blazor Client tests pass 2,129/2,130 with one pre-existing skip; focused Blazor architecture guards pass 17/17.

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors; pre-existing warnings remain
- [x] `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` — 473/473 passed during Phase 1 domain verification

## Phase 2: Typed Participation Configuration And HAL Actions ⚠️ MIGRATION ARTIFACT COMMITTED / VERIFICATION EXECUTED, NOT GLOBALLY GREEN
- [x] **2.1 `EventParticipationConfiguration` + three mode lookups** — new domain files; delete `IsRegistrationRequired` — **Acceptance:** §5 scenario table constructible; illegal combos typed-rejected; `GuestRecoveryPolicyEnum` is an exact string enum contract — **Effort:** M — **Dependencies:** Phase 1
  - **Evidence:** normalized Domain configuration, stable enums, typed validation failures, and legacy Event property deletion are implemented; Domain focused checks pass 53/53.
- [x] **2.2 Persistence + seeding for participation lookups, source portion** — configurations/seeder/repository complete; committed migration artifact verified separately below — **Acceptance:** stable IDs documented; parity green — **Effort:** M — **Dependencies:** 2.1
  - **Evidence:** EF mappings, query filters/includes, repository/DI, stable lookup repair seeds, explicit seed/federation writers, and the committed `20260728152646_AddParticipationHandlingModes` migration artifact exist.
  - [x] **Migration artifact:** commit `ff30795a2` owns the migration, designer, and snapshot. It includes participation configuration plus the Phase 4 ticketing and monetization schema. Database application/runtime rollout is not evidenced.
- [x] **2.3 Application + API, configure-participation + action synthesis** — `Features/EventParticipation/**`; `EventLinkPolicy`; generated contracts — **Acceptance:** per-mode link matrix, exact authority boundary, output filtering, and labels tested — **Effort:** L — **Dependencies:** 2.2 source
  - **Evidence:** `ManageRegistrations` authorizes verified `OrganizerActor` controllers or explicit `EventRegistrationManage` assignments. Community reporters receive no implicit `EventOwner`. HAL distinguishes `start-registration`, `sign-in-to-register`, and `external-registration`. `EventDto.PublicActions` and ATProto registration URIs use `EventAuthorityRules` filtering. Focused Application 15/15, fallback/Cerbos service 119/119, participation controller 10/10, HAL 20/20, OpenAPI parity 11/11, and architecture contract 10/10 with one unrelated skip pass.
- [x] **2.4 Blazor, Studio participation configuration + public CTA rendering source/contract** — `/studio/events/{eventId}/registration`, `StudioEventNavigation.razor`, `ParticipationConfigurationEditor.razor`; public CTA refactor — **Acceptance:** Studio absent without `configure-participation`; attendee CTA relations never authorize Studio; public CTA requires its exact HAL relation; external CTA never claims ISLAMU registration — **Effort:** M — **Dependencies:** 2.3
  - **Evidence:** Studio route/navigation fail closed on `configure-participation`; detail/list/preview/sidebar consume `start-registration`, `sign-in-to-register`, and `external-registration`; Blazor Client Release builds with zero errors.
  - [ ] **bUnit execution:** blocked by 17 externally owned `ProgramSectionsDialogTests.cs` compiler errors. Do not fix ProgramSections in this workstream.
- [x] **2.5 Aggregate outbound-engagement counter** — stored-action redirect + bounded metrics in `BusinessMetrics.cs` — **Acceptance:** no identity captured; bounded labels; click never named registration — **Effort:** S — **Dependencies:** 2.3
  - **Evidence:** `explore.event_public_actions.engagements` uses only bounded action-kind/surface/outcome labels; focused checks pass 2/2.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet` — executed: 0 errors and 5,162 worktree-wide warnings
- [x] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` — executed: 1,571/2,114 passed and 543 failed from environment/shared failures; all seven focused ticketing/monetization classes pass 23/23, so this is execution evidence, not a green project gate

## Phase 3: Guest Transaction Security Foundation ⚠️ VERIFICATION EXECUTED / ARCHITECTURE NON-GREEN
- [x] **3.1 `EndpointClass.PublicTransactional` + enforcement** — `EndpointClass.cs`, transformer, arch tests (existing) + `PublicTransactionalGovernanceTests.cs` (new); GOVERNANCE/QUICK_REFERENCE updates — **Acceptance:** failing-first governance test then green — **Effort:** M — **Dependencies:** ADR-017
  - **Evidence:** governance enforces anonymous `PublicTransactional`, required `public_transactional` rate policy, idempotency metadata/middleware, and the OpenAPI idempotency boolean. PublicTransactional governance passes 6/6, endpoint classification passes 4/4 from the implementation pass, and idempotency passes 6/6.
- [x] **3.2 `public_transactional` rate policy + antiforgery decision** — `Program.cs` rate section; SECURITY-MODEL doc — **Acceptance:** policy registered/documented/Testing-disabled; antiforgery decision in context — **Effort:** M — **Dependencies:** 3.1
  - **Evidence:** fixed-window IP policy is 10 requests per 60 seconds and uses `NoLimiter` in `Testing`. Anonymous browser mutations use BFF proxy antiforgery; direct API clients don't use browser cookie/token pairing. BFF proxy checks pass 20/20. The new rate-policy test exists, but a fresh project build stops before discovery on six unrelated `CustomPropertyDefinitionControllerTests` missing DTO-member errors.
- [x] **3.3 Guest capability-token primitives** — `IGuestCapabilityTokenService` (new contract), Infrastructure impl, `CapabilityTokenHash` VO — **Acceptance:** hash-only storage; constant-time compare; token revealed exactly once — **Effort:** M — **Dependencies:** 3.1
  - **Evidence:** 256-bit token primitives reveal plaintext once, retain hashes only, and use constant-time comparison. Domain capability passes 3/3 and Infrastructure capability passes 5/5.
  - **Oracle follow-up:** **PASS**, with no Critical or High issues; governance metadata bypass and secret formatting were fixed. The remaining Medium atomic-idempotency prerequisite belongs to Task 5.4.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet` — executed: 0 errors and 5,162 worktree-wide warnings
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — executed: 330/340 passed, 9 unrelated failed, 1 skipped; execution is complete but the project gate is not green

## Phase 4: Ticket Catalog, Capacity Pools, Entitlements, And Instance Monetization ✅ SOURCE COMPLETE / DOCKER PROOF BLOCKED
- [x] **4.1 Catalog domain model with immutable publication + five pricing modes** — catalog/type/entitlement/pool entities + 6 lookups incl. `TicketPricingMode` (`FIXED/FREE/DONATION/PAY_WHAT_YOU_CAN/SLIDING_SCALE`) + `TicketCatalogRules.cs` + `TicketPricingRules.cs`; persisted/API amounts use `long ...Minor`, percentages use integer basis points, and no scalar Event/EventSession price or foreign-exchange conversion exists — **Acceptance:** publish-freeze, clone-to-draft, one-currency, entitlement legality tests; pricing-mode validation matrix (5 modes × valid/invalid/boundary incl. 0-allowed); deterministic minor-unit arithmetic — **Effort:** L — **Dependencies:** Phase 2
  - **Evidence:** Oracle initially returned **FAIL** in session `ses_052476c4cffeqCboMjdwpnB2xM`. Corrected source removes legacy Event/EventSession prices, derives summaries from the published catalog, creates no `XXX` bootstrap catalog, routes ticket child mutation through non-public mutators, and uses deterministic same-currency rounding. Pricing-focused lanes pass 19 + 10 + 11 + 19 + 13 + 36 tests.
- [x] **4.2 Persistence + seeding** — configurations/repositories; filtered unique active-catalog index — **Acceptance:** immutability via concurrency; shared-pool resolution tests; hidden/cross-event ticket lookups → generic not-found — **Effort:** L — **Dependencies:** 4.1
  - **Evidence:** pool-focused lanes pass 4 + 13 tests plus 5 architecture tests. Persistence-focused lanes pass 11 tests plus 2 architecture tests. The pool reference guard checks both published and draft/management catalogs, and EF conflict translation is bounded to concurrency plus the named published-catalog and capacity-pool unique constraints.
- [x] **4.3 Authoring Application + API + authorization + HAL** — `Features/EventTicketing/**`, controllers, event `manage-ticket-types`/`manage-capacity-pools` relations, fallback/Cerbos parity, generated contracts — **Acceptance:** contributor denied; external-managed/listing-only event omits both Studio relations; publish preflight (currency/entitlements/pricing-mode consistency) — **Effort:** L — **Dependencies:** 4.2
  - **Evidence:** corrected focused lanes above cover published-catalog summaries, publication, pool guards, aggregate-only child mutation, and bounded conflicts. Stale policy-test literals were corrected to `LinkRelations.CreateDraft`, `CreateTicketType`, and `CreateCapacityPool`; production/runtime/client relations were already canonical.
- [x] **4.4 Studio ticket authoring + price display migration + field deletion** — `/studio/events/{eventId}/tickets`, `StudioEventNavigation.razor`, ticket catalog/type/pool editors, and decorative Event/EventSession price deletion — **Acceptance:** old decorative price members absent; navigation uses `manage-ticket-types OR manage-capacity-pools`; ticket and pool controls independently HAL-gated — **Effort:** L — **Dependencies:** 4.3
  - **Evidence:** Studio-focused lanes pass 12 + 14 + 76 tests. The editor cancels stale loads and mutations when the event changes or the component is disposed. The route and navigation use the OR gate, while create/edit/delete controls use their exact catalog, ticket-type, or capacity-pool HAL relations.
- [x] **4.5 Instance monetization configuration (fee policy + platform contribution)** — versioned instance-scoped domain/persistence, `Features/PlatformMonetization/**`, Admin-class `GET|PUT /api/instance/settings/platform-monetization`, and Blazor instance Monetization settings — **Acceptance:** instance-admin-only handler defense; defaults off/0; DB-stored heading/body and editable basis-point options; contribution remains separate from organizer earnings — **Effort:** L — **Dependencies:** 4.1
  - **Evidence:** the runtime UI route is `/settings/instance`; the API resource is `GET|PUT /api/instance/settings/platform-monetization`. HAL `edit` controls UI save availability, both handlers recheck instance-admin authority, and fresh defaults are disabled/zero.

### Phase 4 Verification — EXECUTED AFTER CORRECTIONS
- [x] `dotnet build --configuration Release --verbosity quiet` — latest run 0 errors and 841 worktree-wide warnings; the earlier broad-matrix run recorded 5,162 warnings
- [x] Corrected focused evidence — pricing 19 + 10 + 11 + 19 + 13 + 36; pool 4 + 13 + 5 architecture; persistence 11 + 2 architecture; Studio 12 + 14 + 76
- [x] Broad per-project verification executed — Domain 602/602; Application 3,374/3,377 after fixing two stale removed-price AI assertions, with exactly 3 unrelated failures; Architecture 330/340 passed, 9 failed, 1 skipped; Secrets 205/205; Infrastructure non-runtime 1,151/1,151; Persistence 96/698 passed and 602 environment/provider-heavy failures; API 1,571/2,114 passed and 543 environment/shared failures; Blazor Integration 398/398; Blazor Client 2,250/2,252 passed, 1 unrelated failure, 1 skipped. This marks execution complete, not global green
- [x] Latest focused source-reality checks — Phase43 ticketing Application 59/59; schedule-target deletion guards 3/3; ticketing persistence 13/13; EventSeries repository pricing regression 1/1; Phase43 ticketing API 11/11; platform-monetization API 10/10; ticketing inventory architecture 1/1; semantic registry 2/2; intent YAML parses successfully
- [ ] PostgreSQL ticketing row-lock concurrency proof — deletion-winning and assignment-winning tests both compile, force observable PostgreSQL `55P03` lock contention, and retry the real ticket assignment/deletion guards, but both are blocked before execution because Testcontainers cannot reach Docker at `unix:///var/run/docker.sock`; do not report either as passed
- [x] Exact unrelated failures recorded — Application: `PublishEventCommandHandlerTests.Handle_WithEnabledAtproto_StagesEventOutboxAfterLocalSaveInsideTransactionWithoutPdsCall`, `UpdateOrganizationCommandHandlerTests.Handle_WhenRequesterIsNotOrgAdmin_ReturnsAuthorizationFailureAndDoesNotSave`, and `EventLocationDisclosureContractTests.Contracts_AreImmutableRecordsAndDoNotReuseGenericLocationDto`; Blazor Client: `LaunchAccessibilitySourceTests.LaunchCriticalPages_ShouldPreserveAccessibilityContracts`. Architecture's 9 failures are unrelated to the corrected Phase 4 lanes
- [x] Additive legacy-pricing migration generated — `20260729183118_RemoveLegacyEventPricing` plus designer/snapshot remove both nonnegative-price checks and Event/EventSession `price`/`currency_code`; `dotnet ef migrations has-pending-model-changes` reports no changes. Database application is unproved
- [x] Final direct review — complete; no unresolved High or Major goal, correctness, tenant-isolation, transaction, retry, or API-contract finding remains

## Phase 5: Registration Orders, Inventory Holds, Guest Checkout Core 🟡 PERSISTENCE/HOLDS VERIFIED
- **Coordinator preflight — independently `CONFIRMED`:** contribution-contract allowlist expanded before generated-contract, MCP, AI, outbox-dispatch, lexicon, or API-inventory edits. Validation: `git diff --check -- .claude/contract/intents.yaml`; `python -c "import yaml; data=yaml.safe_load(open('.claude/contract/intents.yaml', encoding='utf-8')); assert any(i['id']=='registration-data-collection' for i in data['intents'])"`; required-path assertions; `dotnet build --configuration Release --verbosity quiet` (0 errors, 82 pre-existing advisory warnings). Verifier attribution is limited to the intent file and found no compatibility or product-code change. Cleanup receipt: no temporary, TRX, `.received`, browser, Docker, Aspire, or live-service artifacts created.
- **Gate:** Wave 1 runs Tasks 5.1/5.2/5.10 domain/application work beside the atomic-idempotency prerequisite. Persistence/hold work waits on independent Wave 1 verification; guest endpoints additionally wait on Task 5.3 and atomic idempotency. Corrected Phase 4 verification is recorded, but the Docker-backed row-lock proof remains unavailable and must not be reported as passing.
- **Task-graph correction:** stalled Tasks 3/15 and immutable-edge downstream Tasks 4–14 were deleted. Task 16 completed atomic idempotency. Replacement Task 17 verifies Wave 1 against completed Tasks 1/2/16; machine-enforced Tasks 18–27 preserve the approved persistence → API/security → cutover → UI → migration/docs → final-review order.
- **Task 18/28/19 closure:** recovery completed every 5.2/5.3 requirement and independent Task 19 verification is `CONFIRMED`; Task 20 is unlocked.
- **Atomic-idempotency contract:** claim-save and result-save failures both fail closed as RFC 7807 `503 ServiceUnavailable` with code `idempotency_unavailable`; do not introduce a parallel `500` / `idempotency_persistence_failed` contract.
- **Wave 1 exact verification receipt — `CONFIRMED SOURCE / RUNTIME BLOCKED`:**
  - `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` → 617/617 passed.
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/OrganizerEarningsCalculatorTests/*" --minimum-expected-tests 1` → 3/3 passed.
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/PlatformMonetizationHandlersTests/*" --minimum-expected-tests 1` → 8/8 passed.
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IdempotencyMiddlewareAtomicClaimTests/*" --minimum-expected-tests 1` → 3/3 passed.
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/IdempotencyMiddlewareTests/*" --minimum-expected-tests 1` → 8/8 passed.
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IdempotencyRepositoryTests/*" --minimum-expected-tests 1` → 0/2 executed; Testcontainers cannot connect to `unix:///var/run/docker.sock` or `unix:///home/amir/.docker/desktop/docker.sock`, so no database atomicity claim is made.
  - Cleanup: `git diff --check` is clean. `git status --short -- src/Explore.Domain/RegistrationOrder* src/Explore.Domain/Services/Registration/RegistrationOrderRules.cs src/Explore.Application/Services/Registration/OrganizerEarningsCalculator.cs src/Explore.API/Middleware/IdempotencyMiddleware.cs src/Explore.Persistence/Repositories/IdempotencyRepository.cs tests/Event.Domain.UnitTests/Entities/RegistrationOrder* tests/Event.Application.UnitTests/Services/Registration/OrganizerEarningsCalculatorTests.cs tests/Event.API.IntegrationTests/Features/IdempotencyMiddlewareAtomicClaimTests.cs` shows only expected shared-worktree paths. No verifier edits or tracked TRX, `.received`, temporary, browser, Docker, Aspire, or live-service artifacts exist; ignored HTML test reports remain under build output only.
  - Late Task 2 follow-up: its final pre-Task-18 Domain run passed 618/618 after adding one checkout-totals regression. Supplemental command `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/RegistrationOrderRulesTests/*" --minimum-expected-tests 2` now fails before discovery only because Task 18's intentional RED hold tests reference the not-yet-created `RegistrationInventoryHold`. Task 19 must prove the current Domain project at no fewer than 618 tests plus new hold coverage after Task 18 returns green.
- **Task 2 executor reproducibility receipt:**
  - Final GREEN: `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet --no-restore` → 618/618; `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/OrganizerEarningsCalculatorTests/*" --minimum-expected-tests 3` → 3/3; `git diff --check && dotnet build --configuration Release --verbosity quiet --no-restore` → exit 0 with 0 errors.
  - RED: `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/RegistrationOrderTests/*" --minimum-expected-tests 6` → three expected failures; `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/OrganizerEarningsCalculatorTests/*" --minimum-expected-tests 3` → one expected failure; follow-up order selector with `--minimum-expected-tests 8` → one expected failure, then 8/8 GREEN after the checkout-totals fix. `RegistrationOrderRulesTests` and `RegistrationOrderLineTests` pre-existed as untracked shared-worktree files, so no independent RED attribution is claimed for them.
  - Attribution: 15 `RegistrationOrder*`/lookup/rules Domain files, the earnings service contract/implementation plus Application DI, three Domain test files, and one Application test file; no Persistence, API, UI, migration, generated-contract, governance, or ledger files belong to Task 2.
  - Cleanup: removed `tests/{Event.Domain.UnitTests,Event.Application.UnitTests,Event.Architecture.Tests}/bin/Release/net10.0/TestResults` and confirmed each absent. No TRX, `.received`, browser, Docker, Aspire, migration, or temporary source artifacts were created.
- [x] **5.1 Order aggregate + status machine + PII separation** — `RegistrationOrder(.Pii)` + 4 lookups + `RegistrationOrderRules.cs` (new) — **Acceptance:** exhaustive transition tests; zero PII on order entity — **Effort:** L — **Dependencies:** Phases 3, 4
  - **Evidence:** aggregate, PII split, pinned participation/catalog snapshots, explicit transitions including rejection, and integer-minor-unit totals are implemented. Replacement Task 17 independently confirms the current Domain suite 617/617 with clean reviewed diagnostics.
- [x] **5.2 Order lines with snapshots + buyer-chosen prices** — `RegistrationOrderLine.cs` + config (new) incl. `ChosenUnitPriceAmountSnapshot`, `TicketPricingModeSnapshot`, `PlatformFeePolicyVersionSnapshot` — **Acceptance:** catalog revision leaves lines byte-identical; chosen price validated against **pinned** version bounds (below-minimum rejected; 0 accepted when minimum 0) — **Effort:** M — **Dependencies:** 5.1
  - **Evidence:** immutable line/config snapshots and pinned-price bounds are independently source-reviewed; the final Domain project passes 630/630.
- [x] **5.3 Atomic hold reservation + expiry sweeper** — `RegistrationInventoryHold.cs`, deterministic-order pool locking repo methods, `CreateOrderWithHoldCommandHandler`, `InventoryHoldExpiryWorker` (all new); reserve-before-PII sequencing — **Acceptance:** real-PostgreSQL race test incl. **two different ticket types sharing one pool's last seat** (Hi.Events §7.1 counter-example); expired hold releases idempotently; waitlist-when-full; expiry-vs-finalization recovery path defined — **Effort:** XL — **Dependencies:** 5.2
  - **Evidence:** final independent gate passed lifecycle 22/22, create-order 19/19, expiry worker 3/3, real-PostgreSQL holds 6/6, and ticketing row locks 2/2. Migration `20260730200905_AddCapacityHoldPolicyLookup` applies with four policy rows and the capacity-pool FK; EF has no pending model changes. Recovery preserves expired audit rows, validates/consumes only replacement active holds, and finalizes exactly once.
- [x] **5.4 Guest order flow (PublicTransactional endpoints)** — guest actions on `RegistrationOrderController.cs` (new) — **Acceptance:** §31.3 matrix (anonymous rejection, token scope, generic 404, expiry, no silent account); display/public IDs never authorize (Hi.Events §7.8); rotation invalidates prior token; capability values never logged; before endpoint exposure, an atomic in-progress key claim or business-transaction-owned dedupe ensures concurrent identical keys execute once and required claim-persistence failures fail closed — **Effort:** L — **Dependencies:** 5.3, Phase 3, atomic idempotency prerequisite
  - **Prerequisite:** Current generic `IdempotencyMiddleware` performs `FindAsync` → execute → `SaveAsync`, so concurrent identical keys may execute twice and save failures are fail-open. Close this Medium issue before the first Phase 5 `PublicTransactional` endpoint; no migration design is required now.
  - **Prerequisite evidence:** team Task 16 replaced the generic window with tenant-bound durable claiming and conditional completion/replay. Atomic middleware RED 0/2 became GREEN 3/3; existing middleware 8/8 and AI-message 21/21 pass; claim/result persistence failures return canonical RFC 7807 `503 idempotency_unavailable`. Release build and EF pending-model check pass. The PostgreSQL two-context claim race is compiled but Docker-blocked; Task 17 independent verification is in progress.
- [x] **5.5 Authenticated flow + finalization + outbox events** — finalize/cancel commands with **conditional state transition** (Hi.Events §7.2 counter-example), `RegistrationOrderLinkPolicy` (new); release effects derive from lines/holds never participants (§7.6) — **Acceptance:** duplicate finalize returns original result; concurrent second completion creates no extra registrations/answers/outbox rows; rollback clean; outbox in-tx, delivery post-commit; cancellation releases every line type — **Effort:** L — **Dependencies:** 5.3
- [x] **5.6 Rewire `EventRegistration` + delete `EventRegistrationIntent`** — delete intent + handlers/routes; consent FK → order; order-centric organizer queries — **Acceptance:** zero runtime/current-model `EventRegistrationIntent` refs outside immutable `src/Explore.Persistence/Migrations/**`; organizer queries use orders — **Effort:** L — **Dependencies:** 5.5
- [x] **5.7 Order Cerbos/HAL + actor-level Studio context** — order policy; attendee registration relations; event `view-registration-orders`; authenticated private/no-store `StudioContextDto` from `GET /api/studio/context?actorId={optionalActorHint}` — **Acceptance:** external-managed events expose no order/attendee links; unauthorized actor hints fail closed; context has no role booleans or tenant-wide event data — **Effort:** M — **Dependencies:** 5.5
- [x] **5.8 Blazor checkout + Studio order management UX** — attendee `Pages/Registration/**`; actor `/studio/orders`; event `/studio/events/{eventId}/orders`; scoped `IStudioContextService`; state-machine recovery screens, countdown/abandon, pricing widgets, contribution dropdown; contract regen — **Acceptance:** attendee and Studio affordances use `_links`; actor/event sidebar links disappear with their source relation; hold countdown; honest statuses; guest recovery; linked sliders/minimum; 0 only when allowed; DTO-driven contribution options; recovery screens per status — **Effort:** XL — **Dependencies:** 5.4, 5.5, 5.7, 5.10
  - **Evidence:** actor/event Studio order routes and HAL-gated navigation, order lists, authenticated/guest recovery screens, scoped services, capability-header capture, idempotent guest writes, public published-catalog checkout composition, attendee ticket selection, and capability-scoped guest HAL lifecycle actions are implemented. Focused actor-navigation, capability transport, recovery, ticket-selection, and registration-order controller tests pass; API and Blazor Client Release builds are green.
- [x] **5.9 AT Proto + notification dependents sweep** — bounded rewire of all `EventRegistration` dependents; federation decision recorded — **Acceptance:** current-model references and deleted-member references removed; release build blocker recorded in context — **Effort:** M — **Dependencies:** 5.6
- [x] **5.10 Platform-contribution order component + organizer-earnings transparency** — `RegistrationOrderPlatformContribution.cs` + config (new); `IOrganizerEarningsCalculator` + pure decimal implementation (new); order-totals composition; checkout DTO additions — **Acceptance:** hidden when instance-disabled; 0 selection stores no row; amounts computed server-side only; organizer earnings = line totals − fee policy exactly (decimal/rounding tests); contribution never leaks into organizer earnings/totals/exports; positive total → `AwaitingPayment`, all-zero → free path — **Effort:** L — **Dependencies:** 4.5, 5.2
  - **Evidence:** start/continue accept only bounded basis points; `GET` supplies DB heading/body and server-computed minor-unit options; guest capability/current-account guards authorize before the transactional snapshot update; wrong-event authenticated writes now fail before mutation; free orders cannot opt into a payable contribution; zero stores no row; generated OpenAPI/client are current. Application 3,292/3,292, focused API 9/9, persistence 13/13, Release build 0 errors, diagnostics and diff hygiene clean; independent security recheck PASS.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors; existing NU1903 System.Security.Cryptography.Xml warnings
- [x] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` — 99 passed, 574 Docker-blocked; exact `dotnet ef ... --no-build` initially exited 1 because stale Debug binaries were present, then after `dotnet ef ... --configuration Debug` and `dotnet build src/Explore.API --configuration Debug` the same no-build command reported `No changes have been made to the model since the last migration.`; Release build-enabled EF also reported no changes; hold-policy tests 4/4; both downgrade scripts generated; only DBML changed (`schemas/islamu-event.md`); no database apply/revert claim

### Phase 5 Gate
- COMPLETE under shared-baseline policy; the recorded source-receipt blocker was the rename `RegistrationOrderLifecycleResponse.cs` → `RegistrationOrderLifecycleResponseDto.cs` and its dependent `RegistrationOrderLifecycleResponse` → `RegistrationOrderLifecycleResponseDto` references. That receipt is now repaired and compiled, with pure-interface architecture 1/1 green and only the unrelated `EventOrganizerClaimReviewDecision` naming failure remaining in that selector. Full Architecture remains 327/337 passed, 9 failed, 1 governed skip. Migration/EF acceptance is green (`No changes have been made to the model since the last migration`), but Docker-backed persistence and database application/reversal evidence remain unavailable/unclaimed

## Phase 6: Participants, Group Bookings, Data-Collection Modes 🚧 IN PROGRESS
- [x] **6.1 Participant + PII + assignment domain model** — 3 entities + 2 lookups (new); rules extension — **Acceptance:** all five `ParticipantDataCollectionMode` rules tested — **Effort:** L — **Dependencies:** Phase 5
- [ ] **6.2 Persistence + `EventRegistration` participant linkage** — required `RegistrationParticipantId`; assignment-based uniqueness; assignments reference a concrete order line — **Acceptance:** no double admission; unnamed tickets allowed pre-assignment; DB constraint blocks assignments exceeding line quantity (per-line multiset, Hi.Events §7.7) — **Effort:** M — **Dependencies:** 6.1
- [ ] **6.3 Group booking commands + limits** — participant/assignment commands; per-booking-party limits — **Acceptance:** family scenario end-to-end handler test; honest anonymous limits — **Effort:** L — **Dependencies:** 6.2
- [ ] **6.4 API + HAL for participants/assignments** — order-surface actions (auth + capability token); event and `StudioContextDto` `view-participants`; contract regen — **Acceptance:** guest scoped to own order; actor/event organizer links permission-gated — **Effort:** M — **Dependencies:** 6.3
- [ ] **6.5 Blazor group-booking + Studio attendee UX** — participant editors/copy controls/deferred assignment; actor `/studio/attendees`; event `/studio/events/{eventId}/attendees`; §19.5 organizer hint — **Acceptance:** child-required/adult-optional bUnit; deferred deadline visible; copy stays editable; actor/event sections and row operations disappear without their HAL relations — **Effort:** L — **Dependencies:** 6.4

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 7: Registration Form Authoring Core ⏳ NOT STARTED
- [ ] **7.1 Workflow + requirement + channel skeleton** — 3 entities + criticality/effect/sync/subject lookups (new) — **Acceptance:** requirement evaluation rules incl. skip recording — **Effort:** L — **Dependencies:** Phase 5
- [ ] **7.2 Form/version/section/field/option model with immutability** — 6 entities + `FormVersionRules.cs`; dual field identity; governance flags — **Acceptance:** immutability/provenance tests; provider IDs unrepresentable as canonical identity — **Effort:** XL — **Dependencies:** 7.1
- [ ] **7.3 Bounded condition language** — `RegistrationFormRule.cs`, `FormConditionEvaluator.cs` (pure) — **Acceptance:** ten operators only; purity asserted — **Effort:** M — **Dependencies:** 7.2
- [ ] **7.4 JSON Schema 2020-12 artifact generation** — generator service + `SchemaHash` — **Acceptance:** golden-hash determinism; hash sensitive to any mutation — **Effort:** M — **Dependencies:** 7.2, 7.3
- [ ] **7.5 Authoring Application + API + Cerbos** — `Features/RegistrationForms/**`, controllers, `manage-registration-workflow`, `islamuevent_registration_form.yaml` (new); contract regen — **Acceptance:** publish preflight (conditions, consent purposes) — **Effort:** L — **Dependencies:** 7.4
- [ ] **7.6 Studio form builder** — `/studio/events/{eventId}/forms`, `Pages/Studio/RegistrationForms/**`, `StudioEventNavigation.razor` — **Acceptance:** section absent without `manage-registration-workflow`; shared `StudioEventContextState`; published read-only; new-version flow; keyboard ordering; no second management shell — **Effort:** XL — **Dependencies:** 7.5
- [ ] **7.7 Requirement attachment + walk-in standalone questionnaires** — attach/detach; participation-mode validation — **Acceptance:** walk-in `optional-questionnaire` without order creation — **Effort:** M — **Dependencies:** 7.5, Phase 2
- [ ] **7.8 Form localization strategy** — investigate + minimal model; decision recorded — **Acceptance:** `MULTILINGUAL` honestly absent until built; RTL unaffected — **Effort:** M — **Dependencies:** 7.2

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 8: Native Collection Runtime ⏳ NOT STARTED
- [ ] **8.1 Attempt + submission + status machines** — attempt/submission/revision entities + lookups; dedup uniqueness; token single-use — **Acceptance:** duplicate → no-op; supersession rules; answer-identity uniqueness constrained at DB level (Hi.Events §4.7 gap) — **Effort:** L — **Dependencies:** Phase 7
- [ ] **8.2 Typed answer storage + CHECK constraints + subjects** — `RegistrationAnswer`, `RegistrationSensitiveAnswerValue` + raw-SQL checks — **Acceptance:** DB rejects two-column and wrong-type rows; subject-shape constraint (answer subject must match field applicability) — **Effort:** L — **Dependencies:** 8.1
- [ ] **8.3 Normalization + validation pipeline** — submit handlers + Domain normalizers (NFC/E.164/ISO/BCP-47/URL/decimal/date) — **Acceptance:** 17-type matrix; encrypted sensitive round-trip; reject-not-coerce — **Effort:** XL — **Dependencies:** 8.2
- [ ] **8.4 Consent evidence records** — `RegistrationConsentRecord` + pipeline handling — **Acceptance:** immutable evidence; Boolean-only consent impossible — **Effort:** M — **Dependencies:** 8.3
- [ ] **8.5 Requirement fulfillment + idempotent finalization effect** — fulfillment + fenced `RegistrationFinalizationEffect` shared by all paths — **Acceptance:** duplicate effect executes once; optional never blocks — **Effort:** L — **Dependencies:** 8.3, Phase 5
- [ ] **8.6 Native submission API surface** — attempt-launch + submit endpoints (auth + guest); contract regen — **Acceptance:** no answers in ProblemDetails — **Effort:** M — **Dependencies:** 8.5
- [ ] **8.7 Native Blazor form renderer** — `Components/Registration/FormRenderer/**` (new) — **Acceptance:** per-type render + condition toggles; skip flow non-error — **Effort:** XL — **Dependencies:** 8.6
- [ ] **8.8 File answers (gated)** — `RegistrationAnswerFile` + quarantine-by-default; scanner investigation — **Acceptance:** quarantined files unreachable; deferral recorded — **Effort:** L — **Dependencies:** 8.3

### Phase 8 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 9: Provider Framework ⏳ NOT STARTED
- [ ] **9.1 Provider configuration domain model + secret definitions** — connection/binding/capability/mapping/revision entities + 8 lookups; `SecretDefinitionRegistry` additions (+ its unit tests) — **Acceptance:** secret-reference-only credentials — **Effort:** L — **Dependencies:** Phase 8
- [ ] **9.2 Capability contracts + registry + effective resolution** — ten D3 interfaces; registry; resolver — **Acceptance:** unknown tuple fails closed; redirect/manual still offered — **Effort:** L — **Dependencies:** 9.1
- [ ] **9.3 Mapping + schema revision + drift classifier** — mapping entities; `SchemaDriftClassifier` — **Acceptance:** eight drift classes; no silent mapping rewrites — **Effort:** L — **Dependencies:** 9.1
- [ ] **9.4 Callback intake extension + registration effect worker** — `RegistrationProviderCallbackController` (new, intake-only) + fenced worker handler — **Acceptance:** controller never touches aggregates (arch test); dedup + ordering convergence — **Effort:** XL — **Dependencies:** 9.2, 9.3
- [ ] **9.5 Sync-mode enforcement + trust-level policy** — NONE/COMPLETION_ONLY/SELECTED_FIELDS/FULL_CANONICAL/MIRROR_ONLY + minimum-trust gate — **Acceptance:** FR-SYNC-01…07; completion-only stores zero answers — **Effort:** L — **Dependencies:** 9.4
- [ ] **9.6 Reconciliation + provider health** — reconciliation commands + queue; bounded health read models; event `manage-registration-channels`/`view-registration-provider-health` relations — **Acceptance:** health exposes no attendee data — **Effort:** L — **Dependencies:** 9.4
- [ ] **9.7 Channels + embed/CSP + Studio provider UI** — channel CRUD; server-generated iframes; CSP allowlist; `/studio/events/{eventId}/integrations`; processing-status UX — **Acceptance:** Integrations section absent without channel/health relation; no arbitrary iframe input path; completion never inferred from navigation — **Effort:** XL — **Dependencies:** 9.5, 9.6

### Phase 9 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 10: Formbricks Provider (Deep) ⏳ NOT STARTED
- [ ] **10.1 Conformance re-verification + capability profile pin** — evidence file + tuple seed — **Acceptance:** dated endpoint/header evidence; capabilities match — **Effort:** M — **Dependencies:** Phase 9
- [ ] **10.2 Signed callback verifier + BYO link/embed** — Standard-Webhooks profile over shared HMAC core — **Acceptance:** signature/timestamp/duplicate/out-of-order fixture matrix — **Effort:** L — **Dependencies:** 10.1
- [ ] **10.3 Management-API fetch + schema import/mapping** — schema/submission readers; frozen `ExternalImported` versions — **Acceptance:** end-to-end fixture flow; drift classified — **Effort:** L — **Dependencies:** 10.2
- [ ] **10.4 Managed provisioning (Mode B) + publish preflight** — provisioner + subscription manager — **Acceptance:** §14 preflight blocks bad publications; no auto-retry on ambiguity — **Effort:** L — **Dependencies:** 10.3
- [ ] **10.5 Headless mode (C) via ISLAMU backend** — submission writer; canonical-first, provider-write post-commit — **Acceptance:** browser never hits Formbricks response endpoints; provider failure never affects finalized order — **Effort:** M — **Dependencies:** 10.3
- [ ] **10.6 Mirror sink (D) + self-host profile + files/multilingual conformance** — sink impl; optional compose profile; capability truth — **Acceptance:** only `IsProviderTransferAllowed` fields transferred; profile optional — **Effort:** L — **Dependencies:** 10.4, 10.5

### Phase 10 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`

## Phase 11: Microsoft Forms Provider ⏳ NOT STARTED
- [ ] **11.1 Conformance re-verification + connector contract pin** — evidence + tuple; org-account limitation copy — **Effort:** S — **Dependencies:** Phase 9
- [ ] **11.2 Callback profile + envelope verifier** — verifier + binding-scoped API-key secret — **Acceptance:** envelope validation fixture matrix — **Effort:** M — **Dependencies:** 11.1
- [ ] **11.3 Versioned Power Automate solution + setup wizard + manual mapping** — template doc/export; test-event-gated activation — **Acceptance:** activation requires verified test event + complete required mappings — **Effort:** L — **Dependencies:** 11.2
- [ ] **11.4 Reconciliation import (CSV/Excel)** — `ManualImport` trust path — **Acceptance:** import dedupes against callback responses — **Effort:** M — **Dependencies:** 11.2

### Phase 11 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`

## Phase 12: Google Forms Provider ⏳ NOT STARTED
- [ ] **12.1 Conformance re-verification + OAuth connection model** — evidence; connection fields; minimal scopes; secret definitions — **Effort:** M — **Dependencies:** Phase 9
- [ ] **12.2 Import/provision + explicit publication + mapping** — schema reader/provisioner — **Acceptance:** unpublished form blocks activation — **Effort:** L — **Dependencies:** 12.1
- [ ] **12.3 Pub/Sub intake + watch lifecycle + checkpoint fetch** — intake verifier, watch manager + renewal job, checkpointed reader — **Acceptance:** fetch-after-notify; missed-notification recovery; expiry alert — **Effort:** XL — **Dependencies:** 12.2
- [ ] **12.4 Correlation policy + Drive-file decision** — token correlation-only; `NeedsReconciliation` below threshold; file policy decided — **Effort:** M — **Dependencies:** 12.3

### Phase 12 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`

## Phase 13: Consent, Attendee-Data Surfaces, Audited Exports ⏳ NOT STARTED
- [ ] **13.1 Typed consent subjects on `EventContactShareConsent`** — subject type/ID refactor; verified-recipient rule; `docs/CONTACT_SHARING.md` — **Acceptance:** four subject kinds; no prompt on unclaimed reported events — **Effort:** L — **Dependencies:** Phases 6, 8
- [ ] **13.2 Per-participant consent independence** — per-participant prompts; guardian policy; child marketing off by default — **Acceptance:** purchaser consent never copied; §22.4 list asserted — **Effort:** M — **Dependencies:** 13.1
- [ ] **13.3 Audited exports + retention execution** — purpose/exportable/consent-filtered exports + audit rows + retention sweep — **Acceptance:** withdrawn consent excluded; every export audited — **Effort:** L — **Dependencies:** 13.1
- [ ] **13.4 Attendee-management HAL/Cerbos completion + Studio export action** — `export-consented-contacts` etc.; Phase 6 Studio attendee pages; §23 matrix parity/bUnit tests — **Acceptance:** §31.6 rows covered; export appears only from its relation and remains inside Attendees, not a sidebar item — **Effort:** M — **Dependencies:** 13.3

### Phase 13 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 14: Advanced Orchestration & Deferred ⏳ NOT STARTED (each task independently deferrable)
- [ ] **14.1 Guest order → account linking (verified, never silent)** — **Effort:** M
- [ ] **14.2 Form templates/packs with provenance** — **Effort:** L
- [ ] **14.3 Provider switching + supersession tooling** — **Effort:** M
- [ ] **14.4 Governed analytics projections over answers** — **Effort:** L
- [ ] **14.5 Company CSV bulk assignment + `RegistrationAmendment` flows** — **Effort:** L
- [ ] **14.6 Generalized submission sinks (Excel/Sheets/webhooks)** — **Effort:** M
- [ ] **14.7 Blazor affordance-gating + Studio route/sidebar + accessibility audit** — assert every mutation/section is link-gated, canonical `/studio/**` routes are registered, event navigation replaces actor navigation rather than stacking, and new surfaces pass keyboard/RTL/announcement checks — **Effort:** M
- [ ] **14.8 Deferred commerce/admission design records (Hi.Events-informed)** — `deferred-design-records.md` (new): PaymentAttempt/provider reconciliation, AdmissionTicket + rotatable signed/hashed credential (never the display ID; transfer rotates), check-in lists with append-only admission events + unique-active constraint + scoped scanner capabilities + camera/HID UX, anti-enumeration ticket lookup/resend/self-service, promo codes counting live reservations, waitlist offers with expiry, add-ons/general products, taxes/fees/invoices — each citing `hi-events-report.md` sections — **Acceptance:** each record names trigger, extended aggregates, and report sections — **Effort:** M

### Phase 14 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work
- **Migration application/runtime rollout** — `ff30795a2` and `20260729183118_RemoveLegacyEventPricing` provide source artifacts; EF reports no pending model changes, but no database application or runtime rollout evidence is recorded.
- **Broad verification outcome** — execution is complete. Owned focused lanes are green; broad Application, Architecture, Persistence, API, and Blazor Client remain non-green for the exact unrelated/environment results recorded in Phase 4.
- **Final review gate** — the corrected diff was reviewed directly after the user disabled delegation; no unresolved High or Major finding remains. The Docker-backed PostgreSQL row-lock proof is still required when the environment becomes available.
- **Payment integration** — deliberately out of scope; unblocked once orders stop at `AwaitingPayment` (Phase 5). Trigger: separate payment consultation (consultation Report 2 §32 Phase 8). Design record via Task 14.8 (Hi.Events §7.3/§7.5 idempotency/reconciliation lessons). Owner: future workstream.
- **`AdmissionTicket` / QR / check-in / transfers / ticket lookup & self-service** — documented future entities (§16.6; design records via Task 14.8 citing `hi-events-report.md` §5.5/§5.6/§7.10/§7.11); trigger: post-payment or free-event check-in demand. Hard rules already fixed: admission credential ≠ display ID; transfer rotates/revokes; scanner access is authenticated or a scoped expiring capability.
- **Promo codes / affiliates / invoices / taxes & fees / general-product add-ons / waitlist offers** — Hi.Events commercial breadth deliberately deferred (D19 scope discipline); inventory recorded in Task 14.8 only.
- **Organization/group-scoped provider connections** — blocked on `SecretScope` extension (D15). Trigger: org-level Formbricks demand.
- **Malware scanning for file answers** — quarantine-by-default ships in 8.8; scanner integration deferred until infrastructure decision.
- **AT Proto federation of orders/participants** — decision recorded in 5.9; trigger: federation roadmap.
- **Form content multilingual translation tables** — minimal model in 7.8; full translation deferred.
- **Explore.Blazor.IntegrationTests / Explore.Secrets.UnitTests** — contract-mandated projects folded into task acceptance (9.1) or conditional substitution (3.2); see plan §7.
