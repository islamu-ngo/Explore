<!-- ABOUTME: Hot execution ledger for the Registration Data Collection & Participation Platform workstream. -->
<!-- ABOUTME: Mirrors the plan's phases/tasks exactly; implementation agents keep this current during work. -->

# Registration Data Collection & Participation Platform — Task Checklist

Last Updated: 2026-07-20 Europe/Brussels

## Status Summary
- **Overall status:** Draft — re-baselined 2026-07-20 (Hi.Events research + D17 pricing modes + D18 instance monetization); awaiting user review; no implementation started
- **Completed:** 0/88 implementation tasks (phase verification tracked separately)
- **Current priority:** User review of the plan (esp. D4, D8, D13, and the new D17/D18 pricing/monetization decisions)
- **Next recommended slice:** Task 0.1 → 0.2 → 0.3 (ADRs + contract intent), with 0.4 in parallel

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
- ⚠️ **Standing gate:** no `dotnet ef migrations add` until Task 0.4 resolves ordering with the erasure workstream's init lanes.

## Phase 0: Governance, ADRs, And Contract Intent ⏳ NOT STARTED
- [ ] **0.1 Author ADR-016 (bounded context & provider channels)**
  - **Files:** `docs/adr/ADR-016-registration-data-collection-context.md` (new)
  - **Acceptance:** D1/D2/D3/D5/D6/D7/D14 recorded; §24 anti-patterns as rejected alternatives; ADR-015 format followed
  - **Effort:** M — **Dependencies:** none
- [ ] **0.2 Author ADR-017 (participation authority) + ADR-018 (order/ticket aggregate)**
  - **Files:** `docs/adr/ADR-017-event-participation-authority-model.md` (new), `docs/adr/ADR-018-registration-order-ticketing-aggregate.md` (new)
  - **Acceptance:** D8/D9/D10/D12 and D4/D11/D16/D17/D18 recorded; §33 anti-patterns; payment boundary named; Hi.Events adopt/adapt/reject boundary + AGPLv3/branding/provenance rule recorded (report §9–§10, D19)
  - **Effort:** M — **Dependencies:** 0.1
- [ ] **0.3 Add `registration-data-collection` intent to `.claude/contract/intents.yaml`**
  - **Files:** `.claude/contract/intents.yaml` (existing)
  - **Acceptance:** YAML valid; full 8-question contract; cross-references the three dev docs; architecture tests green
  - **Effort:** M — **Dependencies:** 0.1, 0.2
- [ ] **0.4 Resolve migration-baseline ordering (erasure workstream)**
  - **Files:** context file (this dir); observe `src/Explore.Persistence/Migrations/`
  - **Acceptance:** Ordering decision + first-allowed-migration point recorded in context Key Decisions; blocker updated
  - **Effort:** S — **Dependencies:** none

### Phase 0 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 1: Event Provenance, Organizer Authority, And Public Actions ⏳ NOT STARTED
- [ ] **1.1 Provenance typed state on `Event` + `ActorId` semantics decision**
  - **Files:** `src/Explore.Domain/Event.cs` (existing), `EventProvenanceType.cs` + enum (new), `Services/Registration/EventAuthorityRules.cs` (new)
  - **Acceptance:** `IsUserReported`/`EventUrl` gone from `src/`; fail-closed authority rules tested; provenance required (no implicit default); `ActorId` decision recorded
  - **Effort:** L — **Dependencies:** 0.4 (persistence gate only)
- [ ] **1.2 `EventPublicAction` + kinds + health states + `ExternalActionUrl` value object**
  - **Files:** new domain files per plan; `ValueObjects/ExternalActionUrl.cs` (new)
  - **Acceptance:** dangerous schemes rejected; ≤1 primary action; zero actions valid
  - **Effort:** M — **Dependencies:** 1.1
- [ ] **1.3 `EventOrganizerClaim` aggregate**
  - **Files:** `EventOrganizerClaim.cs`, `EventOrganizerClaimStatus.cs` + enum (new)
  - **Acceptance:** transition methods enforced; approval only sets organizer, never grants historical data
  - **Effort:** M — **Dependencies:** 1.1
- [ ] **1.4 Persistence for Phase 1 entities**
  - **Files:** 6 new configurations; DbSets/QueryFilters/LookupTableSeeder (existing); repositories (new)
  - **Acceptance:** seeder parity; tenant-filter test; one-primary filtered unique index; migration only if 0.4 gate open
  - **Effort:** L — **Dependencies:** 1.1–1.3
- [ ] **1.5 Application features — actions, claims, provenance exposure**
  - **Files:** `Features/EventPublicActions/**`, `Features/EventOrganizerClaims/**` (new); `Features/Events/**` DTOs (existing)
  - **Acceptance:** contributor forbidden from registration/ticket/attendee ops; claim approval transactional + retry-idempotent; no capability booleans in DTOs
  - **Effort:** L — **Dependencies:** 1.4
- [ ] **1.6 API + Cerbos + HAL (claims/actions/provenance)**
  - **Files:** 2 new controllers, 2 new link policies, `RouteNames`/`LinkRelations` (existing), `islamuevent_event.yaml` (existing), `islamuevent_event_organizer_claim.yaml` (new); contract regeneration
  - **Acceptance:** classification/contract tests green; no open-redirect endpoint; Cerbos parity deny-by-default; changelog updated
  - **Effort:** L — **Dependencies:** 1.5
- [ ] **1.7 Blazor — badge, provenance panel, claim/correction flows**
  - **Files:** event card/detail components (existing), `EventProvenancePanel.razor` + claim dialogs (new)
  - **Acceptance:** badge non-removable, provenance-derived; affordances `_links`-gated (bUnit); RTL/accessible
  - **Effort:** M — **Dependencies:** 1.6

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 2: Typed Participation Configuration And HAL Actions ⏳ NOT STARTED
- [ ] **2.1 `EventParticipationConfiguration` + three mode lookups** — new domain files; delete `IsRegistrationRequired` — **Acceptance:** §5 scenario table constructible; illegal combos typed-rejected — **Effort:** M — **Dependencies:** Phase 1
- [ ] **2.2 Persistence + seeding for participation lookups** — configurations/seeder; migration per gate — **Acceptance:** stable IDs documented; parity green — **Effort:** M — **Dependencies:** 2.1
- [ ] **2.3 Application + API — configure-participation + action synthesis** — `Features/EventParticipation/**` (new); `EventLinkPolicy` (existing); contract regen — **Acceptance:** per-mode link emission matrix tested; accurate external labels — **Effort:** L — **Dependencies:** 2.2
- [ ] **2.4 Blazor — configuration UI + CTA rendering** — `ParticipationConfigurationEditor.razor` (new); detail CTA refactor — **Acceptance:** no CTA without link; external CTA never claims ISLAMU registration — **Effort:** M — **Dependencies:** 2.3
- [ ] **2.5 Aggregate outbound-engagement counter** — engagement command + bounded metrics in `BusinessMetrics.cs` — **Acceptance:** no identity captured; bounded labels; click never named registration — **Effort:** S — **Dependencies:** 2.3

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Guest Transaction Security Foundation ⏳ NOT STARTED
- [ ] **3.1 `EndpointClass.PublicTransactional` + enforcement** — `EndpointClass.cs`, transformer, arch tests (existing) + `PublicTransactionalGovernanceTests.cs` (new); GOVERNANCE/QUICK_REFERENCE updates — **Acceptance:** failing-first governance test then green — **Effort:** M — **Dependencies:** ADR-017
- [ ] **3.2 `public_transactional` rate policy + antiforgery decision** — `Program.cs` rate section; SECURITY-MODEL doc — **Acceptance:** policy registered/documented/Testing-disabled; antiforgery decision in context — **Effort:** M — **Dependencies:** 3.1
- [ ] **3.3 Guest capability-token primitives** — `IGuestCapabilityTokenService` (new contract), Infrastructure impl, `CapabilityTokenHash` VO — **Acceptance:** hash-only storage; constant-time compare; token revealed exactly once — **Effort:** M — **Dependencies:** 3.1

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 4: Ticket Catalog, Capacity Pools, Entitlements, And Instance Monetization ⏳ NOT STARTED
- [ ] **4.1 Catalog domain model with immutable publication + five pricing modes** — catalog/type/entitlement/pool entities + 6 lookups incl. `TicketPricingMode` (`FIXED/FREE/DONATION/PAY_WHAT_YOU_CAN/SLIDING_SCALE`) + `TicketCatalogRules.cs` + `TicketPricingRules.cs` (all new); `MinimumPriceAmount`/`SuggestedPriceAmount` fields — **Acceptance:** publish-freeze, clone-to-draft, one-currency, entitlement legality tests; pricing-mode validation matrix (5 modes × valid/invalid/boundary incl. 0-allowed); deterministic rounding — **Effort:** L — **Dependencies:** Phase 2
- [ ] **4.2 Persistence + seeding** — configurations/repositories (new); filtered unique active-catalog index — **Acceptance:** immutability via concurrency; shared-pool resolution tests; hidden/cross-event ticket lookups → generic not-found — **Effort:** L — **Dependencies:** 4.1
- [ ] **4.3 Authoring Application + API + Cerbos + HAL** — `Features/EventTicketing/**`, controllers, `manage-ticket-types`/`manage-capacity-pools` relations, `islamuevent_event_ticket_type.yaml` (new); contract regen — **Acceptance:** contributor denied; publish preflight (currency/entitlements/pricing-mode consistency) — **Effort:** L — **Dependencies:** 4.2
- [ ] **4.4 Blazor ticket authoring + price display migration + field deletion** — `TicketCatalogEditor.razor` (new) incl. pricing-mode editor + shared-capacity used-vs-total visualization with pool-overrides warning; delete `Event.Price`/`CurrencyCode`/`EventSession.Price` last — **Acceptance:** `rg "\.Price" src/` only catalog members; editor HAL-gated; pool visualization renders — **Effort:** L — **Dependencies:** 4.3
- [ ] **4.5 Instance monetization configuration (fee policy + platform contribution)** — `PlatformFeePolicy.cs`, `PlatformContributionSetting.cs`, `PlatformContributionOption.cs` (new, versioned, instance-scoped); `Features/PlatformMonetization/**`; Admin-class endpoints (instance-admin only); Blazor instance settings page; CONFIGURATION + ADMIN_GUIDE docs — **Acceptance:** tenant admin/organizer/curator fail closed on every monetization endpoint; defaults off/0; DB-stored heading/body + percentage options seeded `0 (default), 5, 10, 15, 20`; zero hardcoded monetization content in `src/` — **Effort:** L — **Dependencies:** 4.1

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 5: Registration Orders, Inventory Holds, Guest Checkout Core ⏳ NOT STARTED
- [ ] **5.1 Order aggregate + status machine + PII separation** — `RegistrationOrder(.Pii)` + 4 lookups + `RegistrationOrderRules.cs` (new) — **Acceptance:** exhaustive transition tests; zero PII on order entity — **Effort:** L — **Dependencies:** Phases 3, 4
- [ ] **5.2 Order lines with snapshots + buyer-chosen prices** — `RegistrationOrderLine.cs` + config (new) incl. `ChosenUnitPriceAmountSnapshot`, `TicketPricingModeSnapshot`, `PlatformFeePolicyVersionSnapshot` — **Acceptance:** catalog revision leaves lines byte-identical; chosen price validated against **pinned** version bounds (below-minimum rejected; 0 accepted when minimum 0) — **Effort:** M — **Dependencies:** 5.1
- [ ] **5.3 Atomic hold reservation + expiry sweeper** — `RegistrationInventoryHold.cs`, deterministic-order pool locking repo methods, `CreateOrderWithHoldCommandHandler`, `InventoryHoldExpiryWorker` (all new); reserve-before-PII sequencing — **Acceptance:** real-PostgreSQL race test incl. **two different ticket types sharing one pool's last seat** (Hi.Events §7.1 counter-example); expired hold releases idempotently; waitlist-when-full; expiry-vs-finalization recovery path defined — **Effort:** XL — **Dependencies:** 5.2
- [ ] **5.4 Guest order flow (PublicTransactional endpoints)** — guest actions on `RegistrationOrderController.cs` (new) — **Acceptance:** §31.3 matrix (anonymous rejection, token scope, generic 404, expiry, no silent account); display/public IDs never authorize (Hi.Events §7.8); rotation invalidates prior token; capability values never logged — **Effort:** L — **Dependencies:** 5.3, Phase 3
- [ ] **5.5 Authenticated flow + finalization + outbox events** — finalize/cancel commands with **conditional state transition** (Hi.Events §7.2 counter-example), `RegistrationOrderLinkPolicy` (new); release effects derive from lines/holds never participants (§7.6) — **Acceptance:** duplicate finalize returns original result; concurrent second completion creates no extra registrations/answers/outbox rows; rollback clean; outbox in-tx, delivery post-commit; cancellation releases every line type — **Effort:** L — **Dependencies:** 5.3
- [ ] **5.6 Rewire `EventRegistration` + delete `EventRegistrationIntent`** — delete intent + handlers/routes; consent FK → order; order-centric organizer queries — **Acceptance:** zero `EventRegistrationIntent` refs in src/tests — **Effort:** L — **Dependencies:** 5.5
- [ ] **5.7 Cerbos + HAL surface for orders** — order policy evolution/new file; `start-registration`/`start-guest-registration`/`sign-in-to-register`/`view-registration-orders` — **Acceptance:** external-managed events expose no attendee links (FR-PRIV-05) — **Effort:** M — **Dependencies:** 5.5
- [ ] **5.8 Blazor checkout + order management UX** — `Pages/Registration/**` (new) replacing `EventRegistration.razor` flow; state-machine-structured checkout with business-state recovery screens, countdown + navigation-away warning + explicit abandon (Hi.Events §6.2); pricing-mode widgets (donation/PWYC 0-default input; sliding-scale dual linked "You pay"/"Organizer earns" sliders); platform-contribution dropdown from DTO data; contract regen — **Acceptance:** `_links` gating; hold countdown; honest status states; guest recovery page; sliders linked + minimum honored; 0 accepted only when minimum 0; contribution options show "percentage — computed amount" with 0 preselected; recovery screens per status — **Effort:** XL — **Dependencies:** 5.4, 5.5, 5.7, 5.10
- [ ] **5.9 AT Proto + notification dependents sweep** — bounded rewire of all `EventRegistration` dependents; federation decision recorded — **Acceptance:** build green; zero deleted-member refs; decisions in context — **Effort:** M — **Dependencies:** 5.6
- [ ] **5.10 Platform-contribution order component + organizer-earnings transparency** — `RegistrationOrderPlatformContribution.cs` + config (new); `IOrganizerEarningsCalculator` + pure decimal implementation (new); order-totals composition; checkout DTO additions — **Acceptance:** hidden when instance-disabled; 0 selection stores no row; amounts computed server-side only; organizer earnings = line totals − fee policy exactly (decimal/rounding tests); contribution never leaks into organizer earnings/totals/exports; positive total → `AwaitingPayment`, all-zero → free path — **Effort:** L — **Dependencies:** 4.5, 5.2

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 6: Participants, Group Bookings, Data-Collection Modes ⏳ NOT STARTED
- [ ] **6.1 Participant + PII + assignment domain model** — 3 entities + 2 lookups (new); rules extension — **Acceptance:** all five `ParticipantDataCollectionMode` rules tested — **Effort:** L — **Dependencies:** Phase 5
- [ ] **6.2 Persistence + `EventRegistration` participant linkage** — required `RegistrationParticipantId`; assignment-based uniqueness; assignments reference a concrete order line — **Acceptance:** no double admission; unnamed tickets allowed pre-assignment; DB constraint blocks assignments exceeding line quantity (per-line multiset, Hi.Events §7.7) — **Effort:** M — **Dependencies:** 6.1
- [ ] **6.3 Group booking commands + limits** — participant/assignment commands; per-booking-party limits — **Acceptance:** family scenario end-to-end handler test; honest anonymous limits — **Effort:** L — **Dependencies:** 6.2
- [ ] **6.4 API + HAL for participants/assignments** — order-surface actions (auth + capability token); `view-participants`; contract regen — **Acceptance:** guest scoped to own order; organizer link permission-gated — **Effort:** M — **Dependencies:** 6.3
- [ ] **6.5 Blazor group-booking UX** — participant editors per mode; buyer-to-participant copy controls (copy to first/all, per-participant override — Hi.Events §9.1.9); deferred-assignment surfaces; §19.5 organizer hint — **Acceptance:** child-required/adult-optional bUnit; deferred deadline visible; copy control populates then stays editable — **Effort:** L — **Dependencies:** 6.4

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 7: Registration Form Authoring Core ⏳ NOT STARTED
- [ ] **7.1 Workflow + requirement + channel skeleton** — 3 entities + criticality/effect/sync/subject lookups (new) — **Acceptance:** requirement evaluation rules incl. skip recording — **Effort:** L — **Dependencies:** Phase 5
- [ ] **7.2 Form/version/section/field/option model with immutability** — 6 entities + `FormVersionRules.cs`; dual field identity; governance flags — **Acceptance:** immutability/provenance tests; provider IDs unrepresentable as canonical identity — **Effort:** XL — **Dependencies:** 7.1
- [ ] **7.3 Bounded condition language** — `RegistrationFormRule.cs`, `FormConditionEvaluator.cs` (pure) — **Acceptance:** ten operators only; purity asserted — **Effort:** M — **Dependencies:** 7.2
- [ ] **7.4 JSON Schema 2020-12 artifact generation** — generator service + `SchemaHash` — **Acceptance:** golden-hash determinism; hash sensitive to any mutation — **Effort:** M — **Dependencies:** 7.2, 7.3
- [ ] **7.5 Authoring Application + API + Cerbos** — `Features/RegistrationForms/**`, controllers, `manage-registration-workflow`, `islamuevent_registration_form.yaml` (new); contract regen — **Acceptance:** publish preflight (conditions, consent purposes) — **Effort:** L — **Dependencies:** 7.4
- [ ] **7.6 Blazor form builder** — `Pages/Events/Manage/FormBuilder/**` (new) — **Acceptance:** published read-only; new-version flow; keyboard ordering — **Effort:** XL — **Dependencies:** 7.5
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
- [ ] **9.6 Reconciliation + provider health** — reconciliation commands + queue; bounded health read models; organizer HAL — **Acceptance:** health exposes no attendee data — **Effort:** L — **Dependencies:** 9.4
- [ ] **9.7 Channels + embed/CSP + Blazor provider UI** — channel CRUD; server-generated iframes; CSP allowlist; management pages; processing-status UX — **Acceptance:** no arbitrary iframe input path; completion never inferred from navigation — **Effort:** XL — **Dependencies:** 9.5, 9.6

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
- [ ] **13.4 Attendee-management HAL + Cerbos completion** — `export-consented-contacts` etc.; §23 matrix parity tests — **Acceptance:** §31.6 rows covered — **Effort:** M — **Dependencies:** 13.3

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
- [ ] **14.7 Blazor affordance-gating sweep + accessibility audit** — **Effort:** M
- [ ] **14.8 Deferred commerce/admission design records (Hi.Events-informed)** — `deferred-design-records.md` (new): PaymentAttempt/provider reconciliation, AdmissionTicket + rotatable signed/hashed credential (never the display ID; transfer rotates), check-in lists with append-only admission events + unique-active constraint + scoped scanner capabilities + camera/HID UX, anti-enumeration ticket lookup/resend/self-service, promo codes counting live reservations, waitlist offers with expiry, add-ons/general products, taxes/fees/invoices — each citing `hi-events-report.md` sections — **Acceptance:** each record names trigger, extended aggregates, and report sections — **Effort:** M

### Phase 14 Verification — RUN ONCE AFTER ALL PHASE TASKS
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work
- **Payment integration** — deliberately out of scope; unblocked once orders stop at `AwaitingPayment` (Phase 5). Trigger: separate payment consultation (consultation Report 2 §32 Phase 8). Design record via Task 14.8 (Hi.Events §7.3/§7.5 idempotency/reconciliation lessons). Owner: future workstream.
- **`AdmissionTicket` / QR / check-in / transfers / ticket lookup & self-service** — documented future entities (§16.6; design records via Task 14.8 citing `hi-events-report.md` §5.5/§5.6/§7.10/§7.11); trigger: post-payment or free-event check-in demand. Hard rules already fixed: admission credential ≠ display ID; transfer rotates/revokes; scanner access is authenticated or a scoped expiring capability.
- **Promo codes / affiliates / invoices / taxes & fees / general-product add-ons / waitlist offers** — Hi.Events commercial breadth deliberately deferred (D19 scope discipline); inventory recorded in Task 14.8 only.
- **Organization/group-scoped provider connections** — blocked on `SecretScope` extension (D15). Trigger: org-level Formbricks demand.
- **Malware scanning for file answers** — quarantine-by-default ships in 8.8; scanner integration deferred until infrastructure decision.
- **AT Proto federation of orders/participants** — decision recorded in 5.9; trigger: federation roadmap.
- **Form content multilingual translation tables** — minimal model in 7.8; full translation deferred.
- **Explore.Blazor.IntegrationTests / Explore.Secrets.UnitTests** — contract-mandated projects folded into task acceptance (9.1) or conditional substitution (3.2); see plan §7.
