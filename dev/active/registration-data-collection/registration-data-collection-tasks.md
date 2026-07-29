<!-- ABOUTME: Hot execution ledger for the Registration Data Collection & Participation Platform workstream. -->
<!-- ABOUTME: Mirrors the plan's phases/tasks exactly; implementation agents keep this current during work. -->

# Registration Data Collection & Participation Platform — Task Checklist

Last Updated: 2026-07-29 Europe/Brussels

## Status Summary
- **Overall status:** Phase 4 SOURCE COMPLETE / FULL GATES BLOCKED. Phase 2 migration rollout remains blocked; no Phase 5 endpoint or persistence work exists
- **Completed:** 22/88 implementation tasks (phase verification tracked separately)
- **Current priority:** Oracle review of the Phase 4 source and focused evidence, without greenwashing unrelated or environment-blocked broad gates
- **Next recommended slice:** Run Oracle review for Phase 4, then address only findings owned by Tasks 4.1-4.5; keep external broad-gate and Phase 2 migration blockers open

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
- ✅ **Migration baseline verified:** the three privacy-erasure-owned generated init lanes exist and must not be regenerated or rewritten. Dedicated participation migration ownership/order remains blocked by the standing gate below.
- ⚠️ **Participation migration gate:** no ordered dedicated participation migration exists. `20260727174857_EnforceLookupRelationshipUniqueness` is externally owned and contaminated by participation schema plus legacy `ExternalRegistrationUrl`/`IsRegistrationRequired` removal state. Do not edit or stage its migration, designer, or snapshot in this workstream.
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
- [ ] **1.4 Persistence for Phase 1 entities — IN PROGRESS**
  - **Files:** 6 new configurations; DbSets/QueryFilters/LookupTableSeeder (existing); repositories (new)
  - **Acceptance:** seeder parity; tenant-filter test; one-primary filtered unique index; migration only after Task 0.4, Task 1.1 required provenance writers, and the actor-lifecycle Task 2.1 shared-model migration/snapshot
  - **Effort:** L — **Dependencies:** 1.1–1.3; migration substep also depends on `atproto-federation-actor-lifecycle` Task 2.1
  - **Progress:** model/configuration/seeder/tests/DBML implemented; scoped diagnostics and `git diff --check` are clean; dependency-inclusive Persistence Release build now passes. Migration remains ordered after actor-lifecycle Task 2.1, and executable tests remain blocked by actor-owned test-fixture compilation.
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

## Phase 2: Typed Participation Configuration And HAL Actions ⚠️ SOURCE COMPLETE / MIGRATION AND PHASE GATES BLOCKED
- [x] **2.1 `EventParticipationConfiguration` + three mode lookups** — new domain files; delete `IsRegistrationRequired` — **Acceptance:** §5 scenario table constructible; illegal combos typed-rejected; `GuestRecoveryPolicyEnum` is an exact string enum contract — **Effort:** M — **Dependencies:** Phase 1
  - **Evidence:** normalized Domain configuration, stable enums, typed validation failures, and legacy Event property deletion are implemented; Domain focused checks pass 53/53.
- [x] **2.2 Persistence + seeding for participation lookups, source portion** — configurations/seeder/repository complete; migration rollout tracked separately below — **Acceptance:** stable IDs documented; parity green — **Effort:** M — **Dependencies:** 2.1
  - **Evidence:** EF mappings, query filters/includes, repository/DI, stable lookup repair seeds, explicit seed/federation writers, and Persistence integration checks pass 4/4.
  - [ ] **Migration rollout:** external owner generates the ordered dedicated participation migration. Do not edit or stage `20260727174857_EnforceLookupRelationshipUniqueness`, its designer, or the shared snapshot here.
- [x] **2.3 Application + API, configure-participation + action synthesis** — `Features/EventParticipation/**`; `EventLinkPolicy`; generated contracts — **Acceptance:** per-mode link matrix, exact authority boundary, output filtering, and labels tested — **Effort:** L — **Dependencies:** 2.2 source
  - **Evidence:** `ManageRegistrations` authorizes verified `OrganizerActor` controllers or explicit `EventRegistrationManage` assignments. Community reporters receive no implicit `EventOwner`. HAL distinguishes `start-registration`, `sign-in-to-register`, and `external-registration`. `EventDto.PublicActions` and ATProto registration URIs use `EventAuthorityRules` filtering. Focused Application 15/15, fallback/Cerbos service 119/119, participation controller 10/10, HAL 20/20, OpenAPI parity 11/11, and architecture contract 10/10 with one unrelated skip pass.
- [x] **2.4 Blazor, Studio participation configuration + public CTA rendering source/contract** — `/studio/events/{eventId}/registration`, `StudioEventNavigation.razor`, `ParticipationConfigurationEditor.razor`; public CTA refactor — **Acceptance:** Studio absent without `configure-participation`; attendee CTA relations never authorize Studio; public CTA requires its exact HAL relation; external CTA never claims ISLAMU registration — **Effort:** M — **Dependencies:** 2.3
  - **Evidence:** Studio route/navigation fail closed on `configure-participation`; detail/list/preview/sidebar consume `start-registration`, `sign-in-to-register`, and `external-registration`; Blazor Client Release builds with zero errors.
  - [ ] **bUnit execution:** blocked by 17 externally owned `ProgramSectionsDialogTests.cs` compiler errors. Do not fix ProgramSections in this workstream.
- [x] **2.5 Aggregate outbound-engagement counter** — stored-action redirect + bounded metrics in `BusinessMetrics.cs` — **Acceptance:** no identity captured; bounded labels; click never named registration — **Effort:** S — **Dependencies:** 2.3
  - **Evidence:** `explore.event_public_actions.engagements` uses only bounded action-kind/surface/outcome labels; focused checks pass 2/2.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet` — blocked by 17 externally owned `ProgramSectionsDialogTests.cs` compiler errors; `Explore.API` and `Explore.Blazor.Client` project builds pass
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` — focused `EventSessionSpeakerControllerTests` rerun: 4/6 pass; external failures are `CollectionEditLink_UsesOnlyRelationshipIdForCanonicalPatchRoute` and `Update_WhenIfMatchIsMissing_ReturnsValidationProblemDetails`

## Phase 3: Guest Transaction Security Foundation ⚠️ SOURCE COMPLETE / FULL GATES EXTERNALLY BLOCKED
- [x] **3.1 `EndpointClass.PublicTransactional` + enforcement** — `EndpointClass.cs`, transformer, arch tests (existing) + `PublicTransactionalGovernanceTests.cs` (new); GOVERNANCE/QUICK_REFERENCE updates — **Acceptance:** failing-first governance test then green — **Effort:** M — **Dependencies:** ADR-017
  - **Evidence:** governance enforces anonymous `PublicTransactional`, required `public_transactional` rate policy, idempotency metadata/middleware, and the OpenAPI idempotency boolean. PublicTransactional governance passes 6/6, endpoint classification passes 4/4 from the implementation pass, and idempotency passes 6/6.
- [x] **3.2 `public_transactional` rate policy + antiforgery decision** — `Program.cs` rate section; SECURITY-MODEL doc — **Acceptance:** policy registered/documented/Testing-disabled; antiforgery decision in context — **Effort:** M — **Dependencies:** 3.1
  - **Evidence:** fixed-window IP policy is 10 requests per 60 seconds and uses `NoLimiter` in `Testing`. Anonymous browser mutations use BFF proxy antiforgery; direct API clients don't use browser cookie/token pairing. BFF proxy checks pass 20/20. The new rate-policy test exists, but a fresh project build stops before discovery on six unrelated `CustomPropertyDefinitionControllerTests` missing DTO-member errors.
- [x] **3.3 Guest capability-token primitives** — `IGuestCapabilityTokenService` (new contract), Infrastructure impl, `CapabilityTokenHash` VO — **Acceptance:** hash-only storage; constant-time compare; token revealed exactly once — **Effort:** M — **Dependencies:** 3.1
  - **Evidence:** 256-bit token primitives reveal plaintext once, retain hashes only, and use constant-time comparison. Domain capability passes 3/3 and Infrastructure capability passes 5/5.
  - **Oracle follow-up:** **PASS**, with no Critical or High issues; governance metadata bypass and secret formatting were fixed. The remaining Medium atomic-idempotency prerequisite belongs to Task 5.4.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet` — blocked by 12 unrelated errors: six `CustomPropertyDefinitionControllerTests` missing DTO-member errors and six Blazor client custom-property generated-contract call-site errors; canonical build is not green
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — 304 passed, 10 unrelated failed, 1 skipped; new PublicTransactional checks aren't among the failures

## Phase 4: Ticket Catalog, Capacity Pools, Entitlements, And Instance Monetization ⚠️ SOURCE COMPLETE / FULL GATES BLOCKED
- [x] **4.1 Catalog domain model with immutable publication + five pricing modes** — catalog/type/entitlement/pool entities + 6 lookups incl. `TicketPricingMode` (`FIXED/FREE/DONATION/PAY_WHAT_YOU_CAN/SLIDING_SCALE`) + `TicketCatalogRules.cs` + `TicketPricingRules.cs`; persisted/API amounts use `long ...Minor`, percentages use integer basis points, and currency-aware conversion/rounding is explicit — **Acceptance:** publish-freeze, clone-to-draft, one-currency, entitlement legality tests; pricing-mode validation matrix (5 modes × valid/invalid/boundary incl. 0-allowed); deterministic rounding — **Effort:** L — **Dependencies:** Phase 2
  - **Evidence:** Domain 600/600 passes; immutable catalog revisions, pricing modes, entitlements, capacity pools, minor-unit money, and basis-point calculations are source complete.
- [x] **4.2 Persistence + seeding** — configurations/repositories; filtered unique active-catalog index — **Acceptance:** immutability via concurrency; shared-pool resolution tests; hidden/cross-event ticket lookups → generic not-found — **Effort:** L — **Dependencies:** 4.1
  - **Evidence:** focused ticketing/monetization Persistence 7/7 passes. Full Persistence is 91/688 because 597 Docker/Testcontainers cases cannot start; this is not a green full gate. Phase 2 still owns the separate migration-order blocker.
- [x] **4.3 Authoring Application + API + authorization + HAL** — `Features/EventTicketing/**`, controllers, event `manage-ticket-types`/`manage-capacity-pools` relations, fallback/Cerbos parity, generated contracts — **Acceptance:** contributor denied; external-managed/listing-only event omits both Studio relations; publish preflight (currency/entitlements/pricing-mode consistency) — **Effort:** L — **Dependencies:** 4.2
  - **Evidence:** Phase 4 Application classes 53/53, EventTicketing layout Architecture 5/5, and Phase 4 API cluster 17/17 pass. Stale policy-test literals were corrected to `LinkRelations.CreateDraft`, `CreateTicketType`, and `CreateCapacityPool`; RED 1/3, then GREEN 3/3. Production/runtime/client relations were already canonical.
- [x] **4.4 Studio ticket authoring + price display migration + field deletion** — `/studio/events/{eventId}/tickets`, `StudioEventNavigation.razor`, ticket catalog/type/pool editors, and decorative Event/EventSession price deletion — **Acceptance:** old decorative price members absent; navigation uses `manage-ticket-types OR manage-capacity-pools`; ticket and pool controls independently HAL-gated — **Effort:** L — **Dependencies:** 4.3
  - **Evidence:** focused ticketing Blazor 57/57 passes. The route and navigation use the OR gate, while create/edit/delete controls use their exact catalog, ticket-type, or capacity-pool HAL relations. Browser visual QA is unavailable, so bUnit and architecture evidence are authoritative with that limitation.
- [x] **4.5 Instance monetization configuration (fee policy + platform contribution)** — versioned instance-scoped domain/persistence, `Features/PlatformMonetization/**`, Admin-class `GET|PUT /api/instance/settings/platform-monetization`, and Blazor instance Monetization settings — **Acceptance:** instance-admin-only handler defense; defaults off/0; DB-stored heading/body and editable basis-point options; contribution remains separate from organizer earnings — **Effort:** L — **Dependencies:** 4.1
  - **Evidence:** monetization Blazor 9/9 passes. HAL `edit` controls UI save availability, both query and command handlers recheck instance-admin authority, and fresh defaults are disabled/zero.

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [x] `dotnet build --configuration Release --verbosity quiet` — 0 errors; 551 existing warnings
- [ ] Full gates — focused Phase 4 evidence is green, but broad projects are not globally green: Application 3312/3315 with 3 unrelated failures; Architecture 320/330 with 9 unrelated failures and 1 skip; Persistence 91/688 with 597 Docker/Testcontainers failures; API 1557/2099 with 542 environment/shared failures; Blazor Client 2206/2209 with 2 unrelated failures and 1 skip. Explore.Infrastructure 1149/1149, Secrets 205/205, and Blazor Integration 376/376 pass.

## Phase 5: Registration Orders, Inventory Holds, Guest Checkout Core ⏳ NOT STARTED
- [ ] **5.1 Order aggregate + status machine + PII separation** — `RegistrationOrder(.Pii)` + 4 lookups + `RegistrationOrderRules.cs` (new) — **Acceptance:** exhaustive transition tests; zero PII on order entity — **Effort:** L — **Dependencies:** Phases 3, 4
- [ ] **5.2 Order lines with snapshots + buyer-chosen prices** — `RegistrationOrderLine.cs` + config (new) incl. `ChosenUnitPriceAmountSnapshot`, `TicketPricingModeSnapshot`, `PlatformFeePolicyVersionSnapshot` — **Acceptance:** catalog revision leaves lines byte-identical; chosen price validated against **pinned** version bounds (below-minimum rejected; 0 accepted when minimum 0) — **Effort:** M — **Dependencies:** 5.1
- [ ] **5.3 Atomic hold reservation + expiry sweeper** — `RegistrationInventoryHold.cs`, deterministic-order pool locking repo methods, `CreateOrderWithHoldCommandHandler`, `InventoryHoldExpiryWorker` (all new); reserve-before-PII sequencing — **Acceptance:** real-PostgreSQL race test incl. **two different ticket types sharing one pool's last seat** (Hi.Events §7.1 counter-example); expired hold releases idempotently; waitlist-when-full; expiry-vs-finalization recovery path defined — **Effort:** XL — **Dependencies:** 5.2
- [ ] **5.4 Guest order flow (PublicTransactional endpoints)** — guest actions on `RegistrationOrderController.cs` (new) — **Acceptance:** §31.3 matrix (anonymous rejection, token scope, generic 404, expiry, no silent account); display/public IDs never authorize (Hi.Events §7.8); rotation invalidates prior token; capability values never logged; before endpoint exposure, an atomic in-progress key claim or business-transaction-owned dedupe ensures concurrent identical keys execute once and required claim-persistence failures fail closed — **Effort:** L — **Dependencies:** 5.3, Phase 3, atomic idempotency prerequisite
  - **Prerequisite:** Current generic `IdempotencyMiddleware` performs `FindAsync` → execute → `SaveAsync`, so concurrent identical keys may execute twice and save failures are fail-open. Close this Medium issue before the first Phase 5 `PublicTransactional` endpoint; no migration design is required now.
- [ ] **5.5 Authenticated flow + finalization + outbox events** — finalize/cancel commands with **conditional state transition** (Hi.Events §7.2 counter-example), `RegistrationOrderLinkPolicy` (new); release effects derive from lines/holds never participants (§7.6) — **Acceptance:** duplicate finalize returns original result; concurrent second completion creates no extra registrations/answers/outbox rows; rollback clean; outbox in-tx, delivery post-commit; cancellation releases every line type — **Effort:** L — **Dependencies:** 5.3
- [ ] **5.6 Rewire `EventRegistration` + delete `EventRegistrationIntent`** — delete intent + handlers/routes; consent FK → order; order-centric organizer queries — **Acceptance:** zero `EventRegistrationIntent` refs in src/tests — **Effort:** L — **Dependencies:** 5.5
- [ ] **5.7 Order Cerbos/HAL + actor-level Studio context** — order policy; attendee registration relations; event `view-registration-orders`; authenticated private/no-store `StudioContextDto` from `GET /api/studio/context?actorId={optionalActorHint}` — **Acceptance:** external-managed events expose no order/attendee links; unauthorized actor hints fail closed; context has no role booleans or tenant-wide event data — **Effort:** M — **Dependencies:** 5.5
- [ ] **5.8 Blazor checkout + Studio order management UX** — attendee `Pages/Registration/**`; actor `/studio/orders`; event `/studio/events/{eventId}/orders`; scoped `IStudioContextService`; state-machine recovery screens, countdown/abandon, pricing widgets, contribution dropdown; contract regen — **Acceptance:** attendee and Studio affordances use `_links`; actor/event sidebar links disappear with their source relation; hold countdown; honest statuses; guest recovery; linked sliders/minimum; 0 only when allowed; DTO-driven contribution options; recovery screens per status — **Effort:** XL — **Dependencies:** 5.4, 5.5, 5.7, 5.10
- [ ] **5.9 AT Proto + notification dependents sweep** — bounded rewire of all `EventRegistration` dependents; federation decision recorded — **Acceptance:** build green; zero deleted-member refs; decisions in context — **Effort:** M — **Dependencies:** 5.6
- [ ] **5.10 Platform-contribution order component + organizer-earnings transparency** — `RegistrationOrderPlatformContribution.cs` + config (new); `IOrganizerEarningsCalculator` + pure decimal implementation (new); order-totals composition; checkout DTO additions — **Acceptance:** hidden when instance-disabled; 0 selection stores no row; amounts computed server-side only; organizer earnings = line totals − fee policy exactly (decimal/rounding tests); contribution never leaks into organizer earnings/totals/exports; positive total → `AwaitingPayment`, all-zero → free path — **Effort:** L — **Dependencies:** 4.5, 5.2

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 6: Participants, Group Bookings, Data-Collection Modes ⏳ NOT STARTED
- [ ] **6.1 Participant + PII + assignment domain model** — 3 entities + 2 lookups (new); rules extension — **Acceptance:** all five `ParticipantDataCollectionMode` rules tested — **Effort:** L — **Dependencies:** Phase 5
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
- **Phase 2 participation migration rollout** — no ordered dedicated migration exists. External migration owner must resolve order and generate it. `20260727174857_EnforceLookupRelationshipUniqueness` and its generated artifacts are forbidden to edit or stage in this workstream.
- **External Phase 2 verification failures** — ProgramSections owner fixes the 17 compiler errors; EventSessionSpeaker owner fixes the two exact API tests named above; pinned native Cerbos CLI remains unavailable; Docker/Testcontainers remains unavailable for broader persistence execution.
- **Phase 4 broad verification blockers** — Application retains unrelated failures in `WithdrawEventOrganizerClaimCommandHandlerTests`, `UpdateOrganizationCommandHandlerTests.Handle_WhenRequesterIsNotOrgAdmin_ReturnsAuthorizationFailureAndDoesNotSave`, and `EventLocationDisclosureContractTests.Contracts_AreImmutableRecordsAndDoNotReuseGenericLocationDto`. Blazor Client retains `LaunchAccessibilitySourceTests.LaunchCriticalPages_ShouldPreserveAccessibilityContracts` and `SetupTests.Setup_AfterValidation_ResumesSafeReturnUrl`, plus one governed skip. Architecture, Persistence, and API retain the unrelated/environment failures counted in the Phase 4 gate above. Browser visual QA, Aspire, Docker, and native Cerbos execution remain unavailable.
- **Payment integration** — deliberately out of scope; unblocked once orders stop at `AwaitingPayment` (Phase 5). Trigger: separate payment consultation (consultation Report 2 §32 Phase 8). Design record via Task 14.8 (Hi.Events §7.3/§7.5 idempotency/reconciliation lessons). Owner: future workstream.
- **`AdmissionTicket` / QR / check-in / transfers / ticket lookup & self-service** — documented future entities (§16.6; design records via Task 14.8 citing `hi-events-report.md` §5.5/§5.6/§7.10/§7.11); trigger: post-payment or free-event check-in demand. Hard rules already fixed: admission credential ≠ display ID; transfer rotates/revokes; scanner access is authenticated or a scoped expiring capability.
- **Promo codes / affiliates / invoices / taxes & fees / general-product add-ons / waitlist offers** — Hi.Events commercial breadth deliberately deferred (D19 scope discipline); inventory recorded in Task 14.8 only.
- **Organization/group-scoped provider connections** — blocked on `SecretScope` extension (D15). Trigger: org-level Formbricks demand.
- **Malware scanning for file answers** — quarantine-by-default ships in 8.8; scanner integration deferred until infrastructure decision.
- **AT Proto federation of orders/participants** — decision recorded in 5.9; trigger: federation roadmap.
- **Form content multilingual translation tables** — minimal model in 7.8; full translation deferred.
- **Explore.Blazor.IntegrationTests / Explore.Secrets.UnitTests** — contract-mandated projects folded into task acceptance (9.1) or conditional substitution (3.2); see plan §7.
