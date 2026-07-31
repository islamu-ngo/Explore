<!-- ABOUTME: Implementation plan for the Registration Data Collection & Participation Platform workstream. -->
<!-- ABOUTME: Converts the two CTO consultation reports into evidence-grounded, phased, executable slices. -->

# Registration Data Collection & Participation Platform — Implementation Plan

Last Updated: 2026-07-29 Europe/Brussels

---

## 0. Planning Metadata

- **Original request:** Create a full implementation plan in `dev/active/registration-data-collection/` from the combined consultation document `registration-data-collection-consultation.md` (Report No. 1: registration data collection + forms provider architecture; Report No. 2: participation modes, community-reported listings, guest registration, ticket types, group bookings). Backward compatibility explicitly waived — the platform is in full development mode and the model should be replaced, not patched.
- **Re-baseline request (2026-07-20):** Integrate the decision/design/data-model findings from `hi-events-report.md` (Hi.Events ticketing research — behavior lessons only, not its tech stack or code), and expand ticket pricing beyond Hi.Events: free, paid, donation, pay-what-you-can with optional minimum (Gumroad-style 0-allowed input), Leanpub-style sliding scale (minimum + suggested price, dual linked "You pay" / "Organizer earns" sliders with exact platform-share transparency), plus a LaunchGood-style platform-contribution ("tip the platform") checkout dropdown — default 0, quick 5/10/15/20% options showing percentage + computed amount, DB-stored messaging, enableable by **instance administrators only** (never tenant admins).
- **Licensing correction (2026-07-21):** ISLAMU Event operates under a CLA that enables dual-licensing (offering the software under a non-AGPLv3 license to recipients who cannot use AGPLv3). Therefore **zero code may be copied from the Hi.Events repository** — copying AGPLv3-licensed third-party code would contaminate the codebase and destroy the dual-licensing capability. Hi.Events remains a *behavior, design, and data-model* reference only; the report's §10 code-reuse permission is explicitly overridden by this workstream (see §4.13, D19).
- **Studio integration re-baseline (2026-07-26):** Treat the implemented workspace shell from `dev/active/dynamic-event-management-ui/` as current architecture. Organizer ticketing, orders, attendees, registration forms, and provider operations extend the existing Studio workspace and its single contextual sidebar; they do not create a parallel `/events/manage` navigation system. Public and guest checkout remains outside Studio.
- **Task directory:** `dev/active/registration-data-collection/`
- **Planning status:** Approved by the user on 2026-07-26; re-baselined after the Phase 4 findings on 2026-07-29. The task ledger has 32/88 implementation-task boxes checked, and Task 5.8 is verified. Corrected source, direct final review, and verification execution are recorded. Owned focused lanes are green, broad projects are not globally green, and the Docker-backed row-lock proof remains unavailable. Commit `ff30795a2` owns the participation/ticketing migration artifact; generated `20260729183118_RemoveLegacyEventPricing` removes legacy pricing schema and EF reports no pending model changes. Phase 5 is still incomplete because the final verifier has flagged the migration for correction; database application/runtime rollout is still not evidenced.
- **Matched intent:** `registration-data-collection`, the dedicated cross-cutting intent created in Phase 0. Its related granular intents remain `add-write-endpoint`, `add-get-endpoint`, `add-hal-link`, `add-cqrs-handler`, `add-ef-migration`, `update-repository-query`, `blazor-component-affordance`, `cerbos-policy-change`, and `openapi-contract-change`.
- **Relevant skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `outbox-pattern`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `error-tracking`.
- **Relevant rules:** `.claude/rules/domain.md`, `application-layer.md`, `efcore-persistence.md`, `efcore-migrations.md`, `api-controllers.md`, `api-hateoas.md`, `blazor-server.md`, `blazor-client.md`, `tests.md`.
- **Primary layers touched:** Domain, Application, Persistence, Infrastructure, API, Blazor (BFF + WASM), Cerbos policies, Docs, DevOps (compose profiles for Formbricks, later phases).
- **Complexity:** **XL** — 15 phases, 88 tasks, ~40 new domain entities/lookups, one new endpoint classification, three external provider integrations, a replacement of the registration aggregate, a new public-transactional security surface, and Studio integration across the organizer-facing phases. Evidence: the consultation spans 3,786 lines across two reports; the current registration model (3 entities + 3 lookups) covers roughly 5% of the required target model.

---

## 1. Executive Summary

ISLAMU Event today models participation with four booleans/strings on `Event` (`IsRegistrationRequired`, `IsUserReported`, `EventUrl`, `ExternalRegistrationUrl`), a strictly user-centric registration aggregate (`EventRegistrationIntent` → `EventRegistration`, both requiring an authenticated `User`), and no way to collect organizer-defined registration data at all.

This workstream replaces that with the platform's strongest differentiator:

> **ISLAMU Event owns the registration workflow and the normalized registration record. A form provider (built-in Native, Formbricks, Google Forms, Microsoft Forms) supplies a versioned collection channel and evidence of completion. Listing an event, managing participation, collecting data, selling tickets, and receiving attendee information are separate authorities — possessing one never grants the others.**

What ships, in dependency order:

1. **Provenance & authority model** — community-reported listings, organizer claims, typed public actions, external-link security.
2. **Typed participation configuration** — information-only / walk-in / external-managed / platform-managed, advance-registration obligation, identity-access mode; HAL-authored participation CTAs (zero actions is valid).
3. **Guest transaction security** — a new `PublicTransactional` endpoint classification with capability tokens, dedicated rate limits, antiforgery, and idempotency.
4. **Ticketing & capacity** — versioned ticket catalogs with **five pricing modes** (fixed, free, donation, pay-what-you-can with optional minimum, sliding scale with minimum + suggested price and transparent "Organizer earns" display), shared capacity pools, entitlements, atomic inventory holds, and **instance-admin-only platform monetization** (fee-transparency policy + optional "tip the platform" contribution with DB-stored messaging — defaults all zero/off for self-hosted freedom).
5. **Order aggregate** — buyer/order/lines/participants/assignments replacing the user-centric intent; free-order confirmation; `AwaitingPayment` boundary for the future payment workstream.
6. **Registration Data Collection bounded context** — immutable form versions, typed one-row-per-answer storage, bounded condition language, consent evidence, requirement fulfillment, idempotent finalization.
7. **Provider framework + Formbricks (deep), Microsoft Forms, Google Forms** — capability-authoritative channels reusing the hardened incoming-webhook intake.
8. **Consent & attendee-data surfaces** — typed consent subjects, verified-recipient rule, audited exports.

**Non-goals (explicit):** payment providers, checkout/capture/refund/tax/payout (only the stable pre-payment order state — chosen prices, donations, and platform contributions are **modeled and snapshotted** up to `AwaitingPayment`, never charged in this workstream), payment-processor fee estimation, `AdmissionTicket`/QR check-in materialization (future entity documented, not built), promo codes/affiliates/invoices/general-merchandise add-ons (Hi.Events breadth deliberately deferred — Task 14.8), multi-currency orders, federation of orders over AT Protocol (needs separate design), automatic account creation from guest data, and any undocumented Microsoft internal APIs.

A deep research pass over Hi.Events (`hi-events-report.md`, pinned commit `9de8863a`) confirmed this plan's architectural choices — immutable catalogs, typed answers, hashed capabilities, durable effects, explicit state machines ("No evidence from Hi.Events justifies reversing D1–D16") — and contributed a production **behavior catalog** (reservation-before-PII checkout, visible expiry + state-specific recovery screens, commercial snapshots, shared-capacity visualization, buyer-vs-participant question separation) plus a hardened list of concurrency/security acceptance criteria from its concrete defects (report §7), now embedded in Phases 4–8.

---

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---|---|
| `Event` models participation via booleans/strings | Verified: `src/Explore.Domain/Event.cs` lines 38–48, 77 (`Price`, `CurrencyCode`, `IsRegistrationRequired`, `IsUserReported`, `EventUrl`, `ExternalRegistrationUrl`) | High | Exactly the fields both reports say must be replaced |
| `Event` already has string provenance fields | Verified: `src/Explore.Domain/Event.cs` lines 95–96 (`ProvenanceSource`, `ProvenanceExternalId`) | High | Import provenance only; no authority model |
| Registration intent is user-centric | Verified: `src/Explore.Domain/EventRegistrationIntent.cs` — required `UserId`/`User`, `RegistrationScopeId`, `SelectedEventDayId`, `RegistrationPolicySnapshotId`, `ApprovalStatusId` | High | Cannot represent guests, quantities, participants |
| Per-session registration requires a User | Verified: `src/Explore.Domain/EventRegistration.cs` — required `UserId`, `EventSessionId`, nullable `EventRegistrationIntentId`, `AtprotoRecordId` | High | Keep as materialized admission row, rewire to participants |
| Registration lookups exist | Verified: `RegistrationScope.cs`, `RegistrationMode.cs` (`Open/ApprovalRequired/InviteOnly/Closed`), `EventRegistrationPolicy.cs`, `Enums/RegistrationScopeEnum.cs`, `Enums/RegistrationModeEnum.cs` | High | `RegistrationMode` maps 1:1 to the consultation's admission-decision mode |
| Pure domain policy rules exist | Verified: `src/Explore.Domain/Services/Registration/RegistrationPolicyRules.cs::IsScopeAllowed`, `ResolveInitialApprovalStatus` | High | Pattern to follow for new state machines |
| Registration CRUD feature exists | Verified: `src/Explore.Application/Features/EventRegistrations/` (Create/Update/Delete handlers, 5 query handlers); `src/Explore.API/Controllers/EventRegistrationController.cs` (7 named routes) | High | Will be replaced by order-centric features |
| HAL policy for registrations exists | Verified: `src/Explore.API/Hateoas/Policies/EventRegistrationLinkPolicy.cs`, `EventLinkPolicy.cs`; `LinkRelations.cs` has `registration`/`registrations` | High | |
| Contact-share consent is user-centric | Verified: `src/Explore.Domain/EventContactShareConsent.cs` — required `UserId`, `RecipientActorId`, snapshots (`EmailSnapshot`, `ConsentTextSnapshot`, `ConsentUiVersion`), nullable `SourceEventRegistrationIntentId` | High | Snapshot pattern is correct; subject typing needed |
| Custom-property system is the Layer 3 reference | Verified: `src/Explore.Domain/CustomPropertyDefinition.cs` (namespaced keys, typed validation metadata, exposure/governance flags), `CustomPropertyValue.cs` (one row per value, typed columns, `Ordinal`); `docs/CUSTOM_PROPERTIES.md` defines Layer 1/2/3 boundary | High | Reuse primitives/vocabulary, **not** tables |
| Hardened incoming-webhook intake exists | Verified: `src/Explore.Domain/IncomingWebhookMessage.cs` (exact bytes, payload hash, retention, fenced processing, redrive), `IncomingWebhookEffectOutbox.cs` (leases, fencing, dead-letter) | High | Provider callbacks extend this; no new mechanism |
| Endpoint classes are Public/Authenticated/Admin/PublicTransactional | Verified: `src/Explore.API/Attributes/EndpointClass.cs`; `tests/Event.Architecture.Tests/EndpointClassificationArchitectureTests.cs`; `tests/Event.Architecture.Tests/PublicTransactionalGovernanceTests.cs` | High | Phase 3 implemented |
| UnitOfWork transaction pattern exists | Verified: `src/Explore.Application/Contracts/Persistence/IUnitOfWork.cs`; `src/Explore.Persistence/EfCoreUnitOfWork.cs`; `docs/GOVERNANCE.md` command-handler review checklist | High | All multi-step writes (holds, finalization) use it |
| Idempotency middleware exists | Verified: `docs/ARCHITECTURE.md` §Idempotency — `Idempotency-Key` header, `IdempotencyRecord`, `(Key, TenantId)` replay 24h | High | Guest create/finalize will require it |
| Secrets bindings support Instance and Tenant scopes only | Verified: `src/Explore.Domain/Secrets/SecretBinding.cs`, `Enums/SecretScope.cs` (`Instance = 0, Tenant = 1`), `SecretDefinitionRegistry.cs` | High | Org-scoped provider connections deferred (D15) |
| Cerbos policies exist per resource incl. registrations | Verified: `cerbos/policies/islamuevent_event_registration.yaml`, `islamuevent_event.yaml`, `islamuevent_event_contact_share_consent.yaml`, `derived_roles.yaml` | High | New resources need new policies + parity tests |
| Three generated migration baselines exist | Verified 2026-07-26: `Migrations/20260720162943_init.cs`, `Migrations/DataProtection/20260720163113_init.cs`, and `Migrations/PrivacyErasureAuthority/20260720163243_init.cs`; `.claude/contract/intents.yaml::platform-privacy-erasure.migration_history_exception` remains the authority for the clean reset | High | Registration owns only later additive generated migrations; it never regenerates history |
| Erasure workstream remains active | Verified: `dev/active/optional-retained-erasure-authority/optional-retained-erasure-authority-context.md` | High | Its generated init lanes are now the registration schema baseline |
| Blazor registration UX exists and is client-generated-contract-only | Verified: `src/Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor`, `Pages/Events/EventListRegistrationWorkflow.cs`, `Clients/EventApiClient.g.cs`; QUICK_REFERENCE rule 23 (Blazor isolation) | High | Client work happens only against regenerated `IEventApiClient` |
| Workspace shell and Studio are implemented | Verified: `src/Explore.Blazor.Client/Services/Shell/WorkspaceRegistry.cs`, `Components/Shell/Workspaces/StudioWorkspaceNavigation.razor`, `StudioEventNavigation.razor`, `Pages/Studio/StudioEventShell.razor`, `Routes.razor` | High | Actor navigation currently exposes Overview/Events; an event route replaces it in the same sidebar with HAL-derived sections |
| Studio event navigation is relation-driven and regression-tested | Verified: `tests/Explore.Blazor.Client.Tests/Pages/Studio/StudioEventNavigationTests.cs`, `Pages/Studio/StudioPagesTests.cs`, `Components/Shell/WorkspaceNavigationHostTests.cs`, `Routing/RoutesConfigurationTests.cs` | High | Existing mapped relations cover Details, Schedule, legacy Registration, Publication, Team, and Danger zone |
| No actor-level operational HAL resource exists for cross-event Orders/Attendees navigation | Not found: inspected `UiShellContextDto` consumers, `StudioWorkspaceNavigation`, Studio routes, and Studio tests | High | Task 5.7 introduces one compact authenticated Studio context resource; no role-derived or per-link probe requests |
| None of the target entities exist | Not found: searched `rg "RegistrationWorkflow|RegistrationOrder|RegistrationAttempt|TicketType|CapacityPool|Formbricks|RegistrationParticipant|EventPublicAction|EventProvenance|GuestAccessToken|InventoryHold"` across `src/` — only unrelated hits (`EventListRegistrationWorkflow.cs` UI helper, `ManagedControlPlaneRegistration*`) | High | Everything in §3 is new |
| Provider capabilities (Formbricks/Google/Microsoft) | Source-derived: consultation §2, §14–16 with dated citations (Formbricks Standard Webhooks + headless APIs; Google Forms Pub/Sub watches with 1-week renewal and post-2026-06-30 unpublished-by-default; Microsoft Forms connector-only, org accounts) | Medium | External research tools (anysearch/context7 MCP) were unavailable in this session; re-verification tasks are embedded in Phases 10–12 |
| Rate limiting, caching, ETag, ProblemDetails conventions | Verified: `docs/QUICK_REFERENCE.md` (policies table, `NoLimiter` in Testing, chained `IExceptionHandler`, weak ETags) | High | New `public_transactional` policy must join this table |
| Metrics live in bounded-dimension registries | Verified: `src/Explore.Application/Telemetry/BusinessMetrics.cs`, `WebhookTelemetryDimensions.cs` | High | New registration metrics follow the same pattern |
| Hi.Events research report exists and was fully read | Verified: `dev/active/registration-data-collection/hi-events-report.md` (1,591 lines; Hi.Events pinned at commit `9de8863a`, `develop` 2026-07-19) | High | Research snapshot — behavior catalog + risk findings, **not** an architecture authority |
| Hi.Events validates D1–D16; its commercial breadth is a deferred-feature inventory | Source-derived: report §8 comparison table + §11.1 ("No evidence from Hi.Events justifies reversing D1–D16") and §11.3/§11.4 scope discipline | High | Adopt behavior/workflow lessons; reject its persistence, authorization, money, idempotency, and side-effect machinery |
| Hi.Events concrete defects catalogued (shared-capacity race, duplicate-completion race, cache-only webhook idempotency, public-ID-as-bearer access, per-price multiset gap, attendee-derived inventory release, plaintext tokens/PII logging) | Source-derived: report §7.1–§7.14 | High | Converted into binding acceptance criteria in Phases 4–8 (§11.2 of the report) |
| Hi.Events supports paid/free/donation pricing with per-order minimums; mutable in place | Source-derived: report §4.3 (`ProductPrice` types `PAID/FREE/DONATION/TIERED/REGISTRATION`; "published catalog state is mutable in place") | High | D17 exceeds this breadth while keeping immutable versioned catalogs |
| Hi.Events branding demand is a removable AGPLv3 further restriction | Source-derived: report §10 (FSF analysis reference) | High | Moot for code: §4.13 forbids **any** code copy from Hi.Events (CLA/dual-licensing protection); the report's §10 code-reuse permission is overridden by this workstream |
| Phase 2 typed participation source and contracts are implemented | Verified 2026-07-28: `EventParticipationConfiguration`, `EventAuthorityRules`, `ConfigureEventParticipationCommandHandler`, `EventParticipationController`, `EventLinkPolicy`, generated OpenAPI/NSwag, Studio participation editor, and focused tests | High | Tasks 2.1 through 2.5 are source/contract complete; phase and migration rollout are not complete |
| Participation management does not reuse generic event-update authority | Verified: `ConfigureEventParticipationCommand` requests `AuthorizationActions.Events.ManageRegistrations`; fallback maps it to `PermissionCodes.EventRegistrationManage`; organizer controllers are derived from `OrganizerActor`, and an explicit event-role assignment may grant `EventRegistrationManage` | High | A community reporter receives no implicit `EventOwner` or participation-management authority |
| Public participation has three distinct HAL relations | Verified: `LinkRelations.StartRegistration`, `SignInToRegister`, and `ExternalRegistration`; `EventLinkPolicy` emits them by participation mode and authentication state | High | Studio uses only `configure-participation`; attendee relations never authorize Studio |
| Public-action filtering is shared by API and federation output | Verified: `EventMappingProfile` filters `EventDto.PublicActions` through `EventAuthorityRules.IsPublicActionAllowed`; `AtprotoEventPublicationSnapshotFactory.BuildUris` applies the same rule before emitting a registration URI | High | Stale or mode-incompatible external registration actions do not leak through either output |
| Guest recovery is a string enum contract | Verified: `GuestRecoveryPolicyEnum`; `ContractInvariantsTests.OpenApiDocument_PublicEnumSchemasUseStringValues`; `InstanceOnboardingOpenApiContractTests.GeneratedClient_MustUse_GuestRecoveryPolicyEnum_Contract` | High | Exact literals are `VerifiedEmailRequired`, `UnverifiedEmailAccepted`, `EmailOptional`, `CapabilityLinkOnly`, and `NoRecovery` |
| Participation/ticketing migration artifact is committed | Verified from commit `ff30795a2 feat(persistence/ticketing): add participation schema` | High | Owns `20260728152646_AddParticipationHandlingModes.cs`, its designer, and the snapshot; contains participation, catalog/type/entitlement/pool, fee/fixed-charge, and contribution setting/option schema. Database application/runtime rollout is not evidenced |

### 2.2 Existing Implementation (by owning layer)

- **Domain.** `Event` aggregate (219 lines) carries schedule projection methods and the four participation booleans/strings. Registration is `EventRegistrationIntent` (why the user registered: Event/Day/SessionSelection scope, policy snapshot, approval status) → `EventRegistration` (per-session access row). Both require `User`. `RegistrationMode` (admission policy), `RegistrationScope`, `EventRegistrationPolicy` are `int`-keyed lookups with enum mirrors. `RegistrationPolicyRules` centralizes scope/approval rules. `EventContactShareConsent` stores per-organizer email-share consent with text/UI-version snapshots. The custom-property subsystem (definition/option/value + per-entity clones for Event, EventSession, templates) demonstrates the repo's typed-metadata-row pattern and its governance vocabulary (`ExposureLevel`, `IsExportable`, min/max/regex/URL-scheme constraints).
- **Application.** `Features/EventRegistrations/` implements user-centric CRUD (19K-line create handler includes policy/approval resolution). `Features/ContactShareConsents/`, `Features/RegistrationModes|RegistrationScopes|EventRegistrationPolicies/` expose lookups. `Features/Webhooks/` owns the durable intake/effect processing. `Authorization/` carries `AuthorizationActions`, resource descriptors, Cerbos wiring. `Telemetry/BusinessMetrics.cs` is the bounded metrics registry. `Contracts/Persistence/IUnitOfWork.cs` + `IIdempotencyRepository` provide transactionality and replay.
- **Persistence.** `ExploreDbContext` partials (`DbSets`, `QueryFilters`, `SaveChanges`) enforce tenant isolation and named `SoftDelete` filters centrally. `Configurations/Entities/*Configuration.cs` per entity. `Seed/LookupTableSeeder.cs` seeds stable lookup IDs. `EfCoreUnitOfWork` implements the transaction pattern. **Migrations: the three privacy-erasure-owned generated init lanes now exist; registration schema changes land only as later additive generated migrations.**
- **Infrastructure.** External service adapters (Listmonk, control-plane provisioning, webhook providers) with `InfrastructureServicesRegistration.cs`. No form-provider adapters exist.
- **API.** `EventRegistrationController` (7 named routes, endpoint classifications, rate-limited writes), HAL link policies per entity (detail + collection separated), `RouteNames.cs`, `EndpointClassificationTransformer` → `x-endpoint-class` OpenAPI extension. Idempotency middleware. Chained exception handlers → RFC 7807.
- **Blazor.** BFF (`Explore.Blazor`) + WASM client (`Explore.Blazor.Client`) strictly isolated from backend layers; all data via generated `EventApiClient.g.cs`; affordances gated by `_links` presence only. `EventRegistration.razor` + `EventListRegistrationWorkflow.cs` implement the current user-centric attendee flow. The implemented Studio workspace owns organizer navigation: `StudioWorkspaceNavigation` shows actor-level Overview/Events, then replaces itself with `StudioEventNavigation` on `/studio/events/{eventId}/...`; `StudioEventContextState` shares one event HAL resource between the sidebar and `StudioEventShell`.
- **Authorization.** Cerbos policy-per-resource (`cerbos/policies/islamuevent_event_registration.yaml` etc.) with derived roles and JSON schemas; parity tests in API integration suite.

### 2.3 Existing Tests And Verification Coverage

- Verified test projects (all under `tests/`): `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.Architecture.Tests` (incl. `EndpointClassificationArchitectureTests`, `ApiContractArchitectureTests`), `Event.Persistence.IntegrationTests`, `Explore.Infrastructure.Tests`, `Event.API.IntegrationTests` (incl. `ContractInvariantsTests`, authorization parity), `Explore.Blazor.IntegrationTests`, `Explore.Blazor.Client.Tests` (incl. `ApiClientNamingTests`, `StudioEventNavigationTests`, `StudioPagesTests`, `WorkspaceNavigationHostTests`, and `RoutesConfigurationTests`), `Explore.Secrets.UnitTests`.
- Protected behavior today: registration CRUD contract routes, endpoint classification completeness, HAL policy emission, tenant filters, lookup seeding parity, typed participation validation, explicit participation-management authorization, HAL CTA synthesis, API/federation public-action filtering, string-enum generation, and bounded outbound engagement metrics.
- Phase 2 focused evidence is green: Domain 53/53, Application 15/15, fallback/Cerbos service 119/119, participation controller 10/10, HAL 20/20, OpenAPI parity 11/11, architecture contract 10/10 with one unrelated skip, and Persistence integration 4/4 from the available Docker-backed lane. `Explore.API` and `Explore.Blazor.Client` Release builds pass.
- The selected full API phase test is not green for two externally owned `EventSessionSpeakerControllerTests`: `CollectionEditLink_UsesOnlyRelationshipIdForCanonicalPatchRoute` expects no `eventSessionId` route value, and `Update_WhenIfMatchIsMissing_ReturnsValidationProblemDetails` expects title `Event session speaker validation failed` but receives `Validation failed`. A 2026-07-28 focused rerun executed six tests, four passed and two failed. This workstream does not fix speaker behavior.
- **Gaps (all new-scope):** zero coverage for guest flows, capability tokens, ticket/capacity concurrency, form versioning, answer normalization, provider callbacks for registration, consent subjects beyond `User`, provenance authority, public-transactional abuse controls, or Studio ticket/order/attendee/form/integration sections. §23/§31 of the consultation define the target matrices; they are folded into phase tasks below.
- Known baseline note: `MEMORY.md` records 15 shared pre-existing test failures attributed to upstream webhook fallout (islamu.ngo import status). Implementation agents must snapshot the failing set at Phase 1 start and never attribute those failures to this workstream.

### 2.4 Existing Documentation And Contracts

- Canonical docs to update as behavior lands: `docs/DOMAIN.md`, `docs/API.md` + `docs/API_CHANGELOG.md`, `docs/AUTHORIZATION.md`, `docs/SECURITY-MODEL.md`, `docs/CONTACT_SHARING.md`, `docs/WEBHOOKS.md`, `docs/OUTBOX_PATTERN.md` (reference only), `docs/CUSTOM_PROPERTIES.md` (boundary note), `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md` (Formbricks profile), `docs/INTEGRATIONS.md`, `docs/ACCESSIBILITY.md` (reference), `schemas/islamu-event.md`, `schemas/openapi_islamu-event.json` (regenerated), ADRs in `docs/adr/` (next free numbers: ADR-016+; verified ADR-001…ADR-015 exist).
- Contracts: OpenAPI document + generated `EventApiClient.g.cs` (regeneration is a governed discrete step per `docs/GOVERNANCE.md` §API Contract Rules); Cerbos policies + `_schemas`; `LookupTableSeeder` stable IDs; `RouteNames`/`LinkRelations` constants.
- Research evidence: `dev/active/registration-data-collection/hi-events-report.md` — Hi.Events ticketing architecture assessment (behavior catalog, defect-derived acceptance criteria, deferred-feature inventory). Cited throughout Phases 4–8 and Task 14.8; do not edit. **Its §10 code-reuse permission is superseded**: this workstream forbids any Hi.Events code copy (§4.13 — CLA/dual-licensing protection); only behavior, design, and data-model lessons may be used.
- Workspace-shell contract: `docs/adr/ADR-019-workspace-shell-composition.md`, `docs/BLAZOR.md`, `docs/DOCK_LAYOUT.md`, and the implemented `dynamic-event-management-ui` workstream. Organizer event navigation must extend the one `shell.workspace-nav` provider; event-level navigation replaces actor-level navigation rather than adding a third sidebar.

### 2.5 Current Pain Points / Improvement Areas (evidence-tied)

1. **Ambiguous booleans.** `IsRegistrationRequired` cannot distinguish nine participation scenarios (consultation §4; `Event.cs:46`). `IsUserReported` grants no authority model (`Event.cs:47`).
2. **Authority conflation.** `Event.ActorId` simultaneously implies submitter/organizer/editor/data-recipient (`Event.cs:34–36`); community contributors would inherit organizer powers.
3. **Account-centric registration.** Required `UserId` on both registration entities blocks guests, families, companies, quantities, unnamed holders (`EventRegistrationIntent.cs:18–20`).
4. **No data collection.** There is no way to ask a registrant anything; organizers must leave the platform, losing typed/filterable/governed data — the core USP gap.
5. **Price is decoration.** `Event.Price`/`EventSession.Price` are display fields with no inventory, limits, snapshots, or currency policy.
6. **Consent subject rigidity.** `EventContactShareConsent.UserId` cannot represent purchasers, participants, or guest contacts (§22 of Report 2).
7. **Public writes are unclassified.** The `Public` endpoint class semantics say "No tenant mutation" (`docs/GOVERNANCE.md`); guest registration cannot be expressed without a new class.
8. **The original plan predates Studio.** Organizer UI tasks still name standalone `Pages/Events/Manage/**` locations and do not define routes or sidebar relations for ticketing, orders, attendees, forms, or integrations. Implementing them unchanged would fragment the shipped workspace model.

### 2.6 Unknowns After Investigation

| Unknown | What was searched/read | Resolving task |
|---|---|---|
| Whether the committed participation/ticketing migration has been applied to a database | Commit `ff30795a2` proves only the source artifact, designer, and snapshot | Require explicit database application/runtime rollout evidence before making an operational claim |
| Whether `Event.ActorId` should be renamed to `PublishedByActorId` or kept with documented semantics | `Event.cs`, usage breadth not fully enumerated | Task 1.1 (bounded investigation inside the slice) |
| Cerbos resource shape for claims/orders/exports (attribute names, derived roles) | `cerbos/policies/*.yaml`, `derived_roles.yaml` read; no target policies exist | Tasks 1.6, 5.7, 13.4 |
| File-upload malware scanning/quarantine capability | `StorageObject` exists; no scanner found (searched `rg -i "malware|clamav|quarantine" src`) | Task 8.8 (File field type gated behind investigation) |
| Form content localization strategy (per-language field labels vs single language per version) | `docs/LOCALIZATION.md` not yet read in depth; CultureRegistry lives in Domain (MEMORY D16) | Task 7.8 |
| Guest antiforgery flow through the BFF (cookie + antiforgery token pairing for anonymous order sessions) | `docs/SECURITY-MODEL.md` headings; `Explore.Blazor` pipeline not traced for anonymous POSTs | Task 3.2 |
| AT Protocol implications of orders/participants (existing `AtprotoRecordId` on `EventRegistration`) | `EventRegistration.cs:46–48`; `docs/FEDERATION.md` not deep-read | Task 5.9 (decision: keep nullable, defer federation) |
| Current Formbricks/Google/Microsoft API surface as of implementation date | External tools unavailable this session; consultation citations dated 2026-07 | Tasks 10.1, 11.1, 12.1 (conformance re-verification) |
| Exact actor-level authority contract for cross-event Orders/Attendees navigation | Inspected `UiShellContextDto`, `StudioWorkspaceNavigation`, current Studio routes/tests; no operational HAL resource exists | Task 5.7 creates `StudioContextDto` + `GET /api/studio/context`; later phases add relations without changing the shell contract |

---

## 3. Proposed Future State

Target ownership (small text trees; authoritative shape from consultation §29 and Final CTO recommendations of both reports):

```text
Event
├── Provenance & organizer authority (EventProvenanceType, SubmittedByUserId, OrganizerActorId, SourcePublisherName, SourceUrl)
├── EventParticipationConfiguration (handling mode, advance-registration obligation, identity-access mode; admission via RegistrationMode)
├── EventPublicAction[] (typed, ordered, health-tracked; zero is valid; one primary CTA)
├── EventOrganizerClaim[] (auditable claim workflow)
├── EventTicketCatalogVersion[] → EventTicketType[] → TicketTypeEntitlement[]; EventCapacityPool[]
└── RegistrationWorkflow → RegistrationRequirement[] (criticality, sync mode, applicability) → RegistrationChannel[]

RegistrationOrder (buyer/guest capability, one currency, catalog+workflow+participation versions pinned)
├── RegistrationOrderPii / RegistrationParticipant[] + RegistrationParticipantPii
├── RegistrationOrderLine[] (price/name snapshots) → RegistrationTicketAssignment[]
├── RegistrationInventoryHold[] (Active/Consumed/Released/Expired/Cancelled)
├── RegistrationRequirementFulfillment[]
├── RegistrationAttempt[] → RegistrationSubmission[] → RegistrationAnswer[] / RegistrationAnswerFile[] / RegistrationSubmissionIssue[]
└── EventRegistration[] (materialized per-session admission rows, participant-linked)

Provider plane: RegistrationProviderConnection → RegistrationProviderBinding (+ capabilities, field/option mappings,
schema revisions, sync mode, trust level) → RegistrationChannel; callbacks ride IncomingWebhookMessage → IncomingWebhookEffectOutbox.
```

Behavioral invariants:

- External completion is **evidence**, never confirmation; only the ISLAMU finalization transaction (capacity + approval + dedup + validation) confirms.
- Attempts pin form version, binding, and mapping revision; "current form" is never resolved after a response arrives.
- Published form versions and ticket catalog versions are immutable; edits create new versions; in-flight work stays pinned.
- Answers are typed rows (exactly one populated value column, DB CHECK), multi-subject (`Order/Purchaser/Participant/TicketAssignment/SessionSelection`), never canonical JSON.
- All UI affordances are HAL-authored; no client role checks; community contributors receive only contributor-safe relations.
- Callback controllers never mutate registrations; workers claim durable effects with fencing and finalize via Application commands inside `IUnitOfWork`.
- Sync mode (`NONE`/`COMPLETION_ONLY`/`SELECTED_FIELDS`/`FULL_CANONICAL`/`MIRROR_ONLY`) is per channel/binding; `NONE` stores nothing and never fulfills required requirements.
- Consent is immutable evidence with typed subjects; purchasing never implies consent; claim approval never grants historical data.

User/operator experience: organizers configure participation, tickets, workflows, and channels inside the implemented Studio workspace; attendees see one accurate primary CTA per event ("Register on ISLAMU" / "Register on organizer website" / "View original event page"), guests complete orders with capability-token management links, and provider health/drift is visible to organizers without exposing attendee data.

Studio route and sidebar ownership:

| Organizer surface | Canonical route | Sidebar authority | Owning phase |
|---|---|---|---|
| Actor Overview / Events | `/studio`, `/studio/events` | Existing Studio workspace availability and managed-actor context | Implemented prerequisite |
| Cross-event Orders | `/studio/orders` | `StudioContextDto._links.view-registration-orders` | Phase 5 |
| Cross-event Attendees | `/studio/attendees` | `StudioContextDto._links.view-participants` | Phases 6/13 |
| Event Registration | `/studio/events/{eventId}/registration` | `EventDto._links.configure-participation` | Phase 2 |
| Event Ticketing | `/studio/events/{eventId}/tickets` | `manage-ticket-types` or `manage-capacity-pools` | Phase 4 |
| Event Orders | `/studio/events/{eventId}/orders` | `view-registration-orders` | Phase 5 |
| Event Attendees | `/studio/events/{eventId}/attendees` | `view-participants` | Phases 6/13 |
| Event Forms | `/studio/events/{eventId}/forms` | `manage-registration-workflow` | Phase 7 |
| Event Integrations | `/studio/events/{eventId}/integrations` | `manage-registration-channels` or `view-registration-provider-health` | Phase 9 |

Public/guest checkout and native form completion remain under `/registration/**`; they are attendee flows, not Studio pages. Check-in, communications, and analytics are not placeholders in this workstream: their sidebar links appear only when future implementations emit their own HAL relations.

---

## 4. Non-Negotiable Constraints

From the repository contract (AGENTS.md §5, QUICK_REFERENCE, GOVERNANCE):

1. Repositories return entities; mapping in handlers. Validators manually instantiated. `Guid` aggregates / `int` lookups / `long` cursors. File-scoped namespaces; two-line `ABOUTME:` headers.
2. GET = `[AllowAnonymous]`+`Public`; writes = `[Authorize]`+`Authenticated` — **except** the new, explicitly governed `PublicTransactional` class introduced by Phase 3 (this is the sanctioned exception route; it must be added to governance docs and architecture tests, never improvised per-endpoint).
3. HAL links are the single source of truth for UI affordances; no DTO capability booleans (`CanViewAttendees` is a named anti-pattern).
4. Tenant isolation via central query filters; every new entity implements `ITenantEntity` (+ audit/soft-delete/concurrency interfaces per `docs/GOVERNANCE.md` entity requirements); `IgnoreQueryFilters` only with the named `SoftDelete` filter.
5. Multi-step writes inside `IUnitOfWork.ExecuteInTransactionAsync`; IDs/timestamps precomputed; **no provider HTTP, email, webhook, or broker call inside a transaction**; side effects post-commit via outbox.
6. Normalized lookups expose `Id`/`Code`/`Name`; stable IDs in `LookupTableSeeder`; no persisted-enum wrappers in API contracts.
7. Controller authoring standard: explicit template, `Name = RouteNames.X`, endpoint classification, `[ProducesResponseType]`, operation-ID naming (`{Controller}_{Action}`); OpenAPI/NSwag regeneration is a discrete governed step.
8. Blazor isolation: client consumes only the generated `IEventApiClient`; no backend project references.
9. Logs/metrics/traces/ProblemDetails must never contain answers, emails, guest tokens, provider payloads, or unbounded labels (consultation NFR-09/10 + repo telemetry pattern).
10. **Migration-baseline coordination:** the privacy-erasure-owned init lanes remain the baseline. Commit `ff30795a2` owns the later additive `20260728152646_AddParticipationHandlingModes` migration, designer, and snapshot. Treat this as a committed artifact only; do not infer database application or runtime rollout.
11. Consultation anti-pattern lists (§24 Report 1, §33 Report 2) are binding forbidden-moves for every phase.
12. Dev-mode waiver: backward compatibility, dual writes, and compatibility shims are **forbidden**, not merely unnecessary — replaced members are deleted.
13. **Hi.Events reuse rule (research-derived, D19 — NO CODE COPY):** Hi.Events is a behavior catalog, never an architecture authority and **never a code source**. Because ISLAMU Event uses a CLA to enable dual-licensing (offering the software under a non-AGPLv3 license to recipients who cannot accept AGPLv3), **copying any code, file, snippet, migration, SQL, or verbatim asset from the Hi.Events repository is forbidden** — third-party AGPLv3 code would contaminate the codebase and break the dual-licensing capability, and its authors are not ISLAMU CLA signatories. All implementation is clean-room: agents may read the *report* (`hi-events-report.md`) for behavior, design, and data-model lessons, but must not open, transcribe, or paraphrase-translate Hi.Events source files into ISLAMU code. This supersedes the report's §10 code-reuse permission. Independently, the reject-list (report §9.3) remains binding: no mutable published prices, no JSON canonical answers, no public/display IDs as authorization, no cache-only idempotency, no float money, no inventory release derived from attendee rows, no external calls inside business transactions, no local status/role authorization in the UI. The "Powered by Hi.Events" branding must obviously never appear in ISLAMU Event.
14. **Money is integer, explicit, and instance-fair:** persisted/API amounts use integer minor units supplied at the contract boundary; the shipped model defines neither decimal-major conversion nor foreign exchange. Platform fee and platform contribution default to zero/off on every self-hosted instance; monetization configuration is instance-admin-only (D18) — tenant-level enablement is a forbidden move.
15. **Studio is the organizer UI boundary:** organizer pages use canonical `/studio/**` routes and the existing `StudioWorkspaceNavigation` / `StudioEventNavigation` replacement model. Event sections come only from the loaded event `_links`; cross-event sections come only from `StudioContextDto._links`. No local role checks, dead sidebar placeholders, third sidebar, or parallel `/events/manage` navigation tree.

---

## 5. Architecture And Design Decisions

### D1 — Dedicated Registration Data Collection bounded context
- **Decision:** New `Registration*` entities own forms/answers; the custom-property subsystem's typed primitives and governance vocabulary (typed value columns, `Ordinal`, namespaced `Namespace+Key` identity, min/max/regex/URL-scheme constraint fields, exposure flags) are mirrored, but **no custom-property table, entity, or projection is reused**.
- **Why:** Event properties describe the event; registration answers describe a participant relationship — different subjects, privacy boundaries, retention, and lifecycle (consultation §1). `docs/CUSTOM_PROPERTIES.md` already forbids workflow-critical state in Layer 3.
- **Alternatives considered:** (a) Reuse `EventCustomPropertyValue` with a "registration" namespace — rejected: wrong parent aggregate, wrong retention/privacy model, anti-pattern §24.2. (b) JSONB response documents — rejected: kills typed filtering/analytics, anti-pattern §24.3.
- **Consequences:** More tables, but each with a single honest owner; shared validation value objects extracted to `Explore.Domain/ValueObjects` for reuse by both systems.
- **Files/layers:** Domain + Persistence (Phases 7–8).

### D2 — Workflow → Requirement → Channel model; five orthogonal provider dimensions
- **Decision:** `RegistrationWorkflow` (per event/purpose) contains `RegistrationRequirement`s (`ALL` at workflow level); each requirement offers `RegistrationChannel`s (`ANY` within a requirement). A channel binds one provider binding with explicit **SchemaAuthority**, **PresentationMode**, **CollectionMode**, **CompletionMode**, **TrustLevel**, and **AnswerSyncMode** — never a single provider enum.
- **Why:** The four providers are not functionally equivalent (consultation §2–4, §10 Report 2); a provider enum conflates ownership, rendering, storage, completion, and trust.
- **Alternatives:** single `RegistrationFormProvider` enum or a `CompositeFormProvider` — both explicitly rejected by the consultation (§3, §4).
- **Consequences:** Slightly more configuration surface; organizers get simultaneous providers, fallbacks, and per-event sync policies without combinatorial provider classes.
- **Files/layers:** Domain + Application (Phases 7, 9).

### D3 — Capability-segregated provider interfaces, fail-closed capability tuples
- **Decision:** Small interfaces (`IRegistrationProviderDescriptor`, `IRegistrationPresentationProvider`, `IRegistrationSchemaReader`, `IRegistrationFormProvisioner`, `IRegistrationSubmissionWriter/Reader`, `IRegistrationCallbackVerifier`, `IRegistrationSubscriptionManager`, `IRegistrationReconciliationProvider`, `IRegistrationSubmissionSink`) registered per provider in Infrastructure. Effective capability = proven profile ∩ connection config ∩ tenant governance ∩ mapping compatibility ∩ authorization. Capability profiles bind to an exact tuple (`ProviderCode`, `DeploymentKind`, `ApiVersion`, `AdapterPolicyVersion`, `ConformanceEvidenceRevision`); unknown tuples fail closed for automatic finalization.
- **Why:** Mirrors the already-shipped webhook capability-authority pattern (verified in `WebhookProviderCapabilityLookup.cs` et al.); avoids `NotSupportedException` interface pollution (anti-pattern §24.10/11).
- **Alternatives:** one fat `IRegistrationFormProvider` — rejected.
- **Consequences:** More interfaces; honest static capability truth; `IRegistrationSubmissionSink` cleanly separates mirror/export destinations from collection.
- **Files/layers:** Application contracts + Infrastructure adapters (Phases 9–12).

### D4 — Buyer–Order–Participant–Ticket aggregate replaces the user-centric intent
- **Decision:** `RegistrationOrder` (nullable `AccountUserId`, `BookingPartyType`, guest capability hash, pinned catalog/workflow/participation versions) replaces `EventRegistrationIntent`, which is **deleted**. `EventRegistration` survives as the materialized per-session admission row but its required `UserId` is replaced by `RegistrationParticipantId` (+ optional `LinkedUserId` denormalization for user-scoped queries). `EventContactShareConsent.SourceEventRegistrationIntentId` is rewired to `SourceRegistrationOrderId`.
- **Why:** Report 2 §11/§16 prove the account-centric aggregate cannot express guests, quantities, families, companies, or deferred assignment; making `UserId` nullable alone is anti-pattern §33.10.
- **Alternatives:** incremental patching of the intent — explicitly rejected by the Final CTO recommendation.
- **Consequences:** Breaking change to registration features, HAL policies, Cerbos policies, DTOs, generated client, and Blazor flow — sanctioned by dev-mode waiver; per-session uniqueness moves from `(User, Session)` to assignment-based uniqueness.
- **Files/layers:** Domain/Persistence/Application/API/Blazor (Phases 5–6).

### D5 — One row per atomic typed answer; sensitive-value split; no canonical JSON
- **Decision:** `RegistrationAnswer` has typed value columns (`Text/Integer/Decimal/Boolean/Date/Time/Instant/OptionId/SensitiveValueId`) with a DB `CHECK (num_nonnulls(...) = 1)` plus type-agreement checks; multivalue via `Ordinal`; subjects via `AnswerSubjectTypeId + AnswerSubjectId`. Sensitive classifications store ciphertext in `RegistrationSensitiveAnswerValue` (key-versioned, optional governed blind index). A short-retention raw provider payload copy lives only on the intake message (existing retention machinery), never as answer truth.
- **Why:** Consultation §6, §18; preserves the platform differentiator (typed, filterable, governed data).
- **Alternatives:** JSONB canonical store — rejected (§24.3).
- **Consequences:** More rows and constraints; straightforward, indexable queries; encryption needs a key-versioning strategy (reuse Data Protection stack — investigation folded into Task 8.6).
- **Files/layers:** Domain + Persistence (Phase 8).

### D6 — JSON Schema 2020-12 as generated, immutable, content-hashed interchange artifact
- **Decision:** Relational rows stay authoritative; each published `RegistrationFormVersion` generates four artifacts (data schema, UI schema, logic schema, provider-mapping schema), content-hashed into `SchemaHash`.
- **Why:** Consultation §8; enables provider adapters, SDK consumers, and stable validation pointers without making JSON the source of truth.
- **Consequences:** A deterministic serializer + hash test (schema hash stability) is mandatory.
- **Files/layers:** Application service + Domain fields (Phase 7).

### D7 — Provider callbacks extend the existing incoming-webhook intake
- **Decision:** Formbricks/Microsoft callbacks and Google Pub/Sub pushes are ingested as `IncomingWebhookMessage` rows (exact bytes, provider proof verification, dedup) that enqueue `IncomingWebhookEffectOutbox` effects; a registration worker claims effects (fenced), re-verifies, fetches where supported, normalizes, validates, fulfills, and finalizes via Application commands. Callback controllers never touch registration aggregates.
- **Why:** The intake pattern is already hardened (verified §2.1); consultation §11/§26 require exactly this shape.
- **Alternatives:** bespoke registration callback tables — rejected (duplicate unsafe mechanism).
- **Consequences:** Registration effect kinds join the webhook effect vocabulary; retention/redrive machinery is inherited for free.
- **Files/layers:** API controllers + Application effects + Infrastructure verifiers (Phase 9).

### D8 — New `EndpointClass.PublicTransactional`
- **Decision:** Add a fourth endpoint classification for narrowly-scoped anonymous mutations (guest order start/continue/finalize, guest capability-token management), with mandatory: dedicated `public_transactional` rate-limit policy, antiforgery for browser-origin same-site requests, required `Idempotency-Key` on create/finalize, capability-token authorization, minimal order-scoped exposure, and PII-free logging. Architecture tests enforce that `PublicTransactional` actions carry all required attributes.
- **Why:** Report 2 §12; the existing `Public` class explicitly promises "no tenant mutation" and must not be weakened.
- **Alternatives:** overloading `Public` — rejected; per-endpoint ad-hoc `[AllowAnonymous]` writes — forbidden.
- **Consequences:** Governance docs, OpenAPI `x-endpoint-class`, client-generation filters, and Cerbos scaffolding all learn the new class once (Phase 3), then Phase 5 consumes it.
- **Files/layers:** API attributes/tests/middleware + docs (Phase 3).

### D9 — Provenance and four-authority model; `ActorId` semantics narrowed, not overloaded
- **Decision:** Add `EventProvenanceTypeId` (lookup: `ORGANIZER_CREATED/COMMUNITY_REPORTED/TENANT_CURATED/IMPORTED/FEDERATED`), `SubmittedByUserId`, `OrganizerActorId` (nullable), `SourcePublisherName`, `SourceUrl` to `Event`; delete `IsUserReported` and `EventUrl`. `Event.ActorId` keeps its column but is redefined and documented as **publishing authority** (`PublishedByActorId` semantics); a Phase 1 in-slice investigation decides whether a physical rename is cheap enough now (dev mode favors renaming if usage count permits). Listing, participation-management, data-collection, and commercial authority are computed from typed state, never from listing ownership.
- **Why:** Report 2 §3/§7; provenance must be historical and non-removable.
- **Consequences:** Cerbos event policy gains `provenance_type`, `organizer_actor_id`, `submitted_by_user_id`, `organizer_verification_status` attributes; UI badge derived server-side.
- **Files/layers:** Domain/Persistence/API/Cerbos/Blazor (Phase 1).

### D10 — Typed participation configuration; prices move to the ticket catalog
- **Decision:** `EventParticipationConfiguration` (1:1 with Event) carries `ParticipationHandlingModeId` (`INFORMATION_ONLY/WALK_IN/EXTERNAL_MANAGED/PLATFORM_MANAGED`), `AdvanceRegistrationObligationId` (`NOT_APPLICABLE/OPTIONAL/REQUIRED`), `IdentityAccessModeId` (`ACCOUNT_REQUIRED/GUEST_ALLOWED/CAPABILITY_TOKEN_ALLOWED`) plus guest-recovery policy; admission decisions keep using the existing `RegistrationMode` lookup. `Event.IsRegistrationRequired` is deleted in Phase 2; `Event.Price`/`CurrencyCode` and `EventSession.Price` are deleted in Phase 4, and display price is derived from the published ticket catalog.
- **Why:** Report 2 §4/§13.4; one registration engine, not two.
- **Consequences:** Every event gets a participation configuration (seeded default `PLATFORM_MANAGED`-equivalent is **not** assumed — creation flows must set it explicitly per "no implicit business defaults" rule; event create/update commands gain required configuration input).
- **Files/layers:** Domain/Persistence/Application/API/Blazor (Phases 2, 4).

### D11 — Capacity pools with atomic holds inside `IUnitOfWork`
- **Decision:** `EventCapacityPool` (+ `HoldDurationSeconds`, oversell policy) shared across ticket types; `RegistrationInventoryHold` with states `Active/Consumed/Released/Expired/Cancelled`; hold creation atomically validates catalog version, quantity limits, and pool capacity using row/counter locking inside one transaction; a background sweeper releases expired holds; hold policies `NO_HOLD_UNTIL_READY/TIMED_HOLD_ON_SELECTION/APPROVAL_NO_HOLD/WAITLIST_WHEN_FULL`.
- **Why:** Report 2 §14/§20; NFR-04 concurrency safety across replicas.
- **Consequences:** Requires a deliberate locking strategy in PostgreSQL (e.g., `SELECT ... FOR UPDATE` on pool counters via repository method) — folded into Task 5.3 with a persistence-level race test.
- **Files/layers:** Domain/Persistence/Application + background service (Phase 5).

### D12 — Consent is immutable evidence with typed subjects
- **Decision:** New `RegistrationConsentRecord` (purpose, exact text snapshot, text/UI versions, language, granted/withdrawn, provider/submission source) with typed subject reference (`User/RegistrationPurchaser/RegistrationParticipant/GuestContact`); `EventContactShareConsent` is evolved to the same subject typing (dev-mode breaking change) and its verified-recipient rule is enforced (`OrganizerActorId` present + verified; never on unclaimed reported events).
- **Why:** Consultation §18 (Report 1) + §22 (Report 2); a Boolean answer is not consent evidence (anti-pattern §24.16).
- **Files/layers:** Domain/Application/API (Phases 8, 13).

### D13 — Clean-baseline schema strategy (no data migrations)
- **Decision:** The privacy-erasure workstream's three generated init lanes are the baseline. Later model/configuration/seeder changes use additive migrations. Commit `ff30795a2` contains the committed participation/ticketing artifact. No legacy-data backfills exist anywhere in this plan; consultation §30.1 migration defaults are recorded as N/A for the clean baseline.
- **Why:** The generated init migrations and snapshots exist; `platform-privacy-erasure` forbids restoring old migration IDs and mandates generated migrations only.
- **Consequences:** Registration never owns baseline regeneration or history rewriting. A committed migration artifact does not prove it was applied to a database.
- **Files/layers:** Persistence (all phases).

### D14 — Trust levels and sync modes govern finalization
- **Decision:** `RegistrationTrustLevel` lookup (`FirstParty/SignedProvider/AuthenticatedProviderFetch/DelegatedAutomation/UserReturnOnly/ManualImport`) and `AnswerSyncMode` lookup (`NONE/COMPLETION_ONLY/SELECTED_FIELDS/FULL_CANONICAL/MIRROR_ONLY`) are typed lookups on bindings/channels; event/tenant policy sets a minimum trust level for automatic finalization; below it → `NeedsReconciliation` for organizer review.
- **Why:** Consultation §3, §10 (Report 2 §10), Microsoft `DelegatedAutomation` classification (§16).
- **Files/layers:** Domain lookups + Application finalization policy (Phase 9).

### D15 — Provider credentials via `SecretBinding`, tenant scope first
- **Decision:** `RegistrationProviderConnection` references credentials exclusively through the existing Explore.Secrets `SecretBinding`/`SecretDefinitionRegistry` (scopes today: Instance, Tenant). Organization/group-scoped connections are deferred until `SecretScope` grows (recorded as deferred work, not silently hacked).
- **Why:** Verified `SecretScope` enum; consultation requires secret references, never inline credentials.
- **Files/layers:** Domain/Secrets + Infrastructure (Phase 9).

### D16 — State machines are explicit lookups, never overloaded `ApprovalStatus`
- **Decision:** Three independent machines as typed lookups with domain rule classes (mirroring `RegistrationPolicyRules`): `RegistrationOrderStatus` (`Draft/AwaitingIdentity/AwaitingParticipantDetails/AwaitingRequirements/ReadyForCheckout/AwaitingPayment/AwaitingApproval/Waitlisted/Confirmed/Expired/Cancelled/NeedsReconciliation`), `RegistrationAttemptStatus` (`Created/Launched/Submitted/Expired/Superseded/Cancelled`), `RegistrationSubmissionStatus` (`Received/ProviderVerified/Normalized/Valid/Invalid/RequirementFulfilled/Finalized/Rejected/NeedsReconciliation`). `ApprovalStatus` remains solely the organizer approval verdict.
- **Why:** Consultation §10; anti-pattern §24.9. Hi.Events independently validates separated order/payment/refund axes (report §5.3) but spreads transitions across handlers without concurrency guards — ISLAMU centralizes transitions in rule classes and persists them conditionally.
- **Files/layers:** Domain enums/lookups + `Services/Registration/` rules (Phases 5, 8).

### D17 — Five ticket pricing modes with server-validated buyer-chosen prices
- **Decision:** `EventTicketType` gains a required `TicketPricingModeId` lookup with exactly five modes, each with mode-specific fields and validation in a pure `TicketPricingRules` class:
  - `FIXED` — organizer-set `PriceAmount`; buyer chooses nothing.
  - `FREE` — no amounts anywhere; always the free confirmation path.
  - `DONATION` — buyer-chosen amount to the **organizer**; organizer may set `MinimumPriceAmount` (0 allowed); checkout input **defaults to 0** when the minimum is 0 (Gumroad-style "0+" field), so free admission with optional giving is one mode, not two tickets.
  - `PAY_WHAT_YOU_CAN` — buyer-priced admission with optional `MinimumPriceAmount` and optional `SuggestedPriceAmount` used as input placeholder/preset; 0 permitted iff minimum is 0.
  - `SLIDING_SCALE` — Leanpub-style: required `MinimumPriceAmount` + required `SuggestedPriceAmount` (≥ minimum). Checkout renders **two linked sliders**: "You pay" (bounded below by minimum, defaulting to suggested) and "Organizer earns" (the exact amount the organizer receives after the instance's platform fee policy, D18). Dragging either slider recomputes the other, so the payer sees precisely what does *not* go to the organizer — transparency by construction.
  The buyer-chosen unit price is validated **server-side** against the pinned catalog version's mode bounds and snapshotted per order line (`ChosenUnitPriceAmountSnapshot`); a zero-total order follows the free confirmation path; any positive total stops at `AwaitingPayment` (charging is the payment workstream's job).
- **Why:** User requirement to exceed Hi.Events' paid/free/donation breadth (report §4.3 proves market demand for `DONATION` with minimum, but its prices are mutable in place and become PHP floats — both rejected). Sliding-scale transparency strengthens both community trust and the ISLAMU SaaS story.
- **Alternatives considered:** Hi.Events-style mutable `ProductPrice` rows — rejected (violates immutable catalogs); one generic "flexible price" mode with UI hints — rejected (the five modes have genuinely different validation and UX semantics); modeling donation as a separate general product — rejected (dilutes admission vocabulary, report §4.1 lesson).
- **Consequences:** Mode-specific widgets in checkout (numeric input, dual sliders); API and persistence accept already-normalized integer minor units and define neither decimal-major conversion nor foreign exchange; `TicketPricingRules` becomes part of publish preflight; tier-style multi-price options can later map onto multiple ticket types without a new mechanism.
- **Files/layers:** Domain/Persistence (Phase 4), Application/API/Blazor (Phase 5).

### D18 — Instance-scoped platform monetization: fee transparency + optional platform contribution
- **Decision:** Two instance-admin-only, DB-stored, default-off/zero concepts, deliberately separated because the money has different destinations:
  1. **`PlatformFeePolicy`** (instance scope; percentage + fixed components; default 0/0): the amount the instance operator retains from organizer-directed payments. Used to compute the "Organizer earns" figure in `SLIDING_SCALE` UX and organizer-facing price previews (with an "excluding payment-processing fees" disclaimer until the payment workstream defines processor costs). Snapshotted (policy version) on order lines whenever non-zero.
  2. **`PlatformContributionSetting`** (instance scope): a LaunchGood-style optional checkout add-on directed to the **instance operator**, never the organizer. Contains: enable flag; DB-stored heading and body text (e.g., "Help us help the Ummah — because this instance doesn't charge a platform fee, we rely on the generosity of donors like you 💓") — fully data-driven, never hardcoded, localization-ready; a configurable quick-percentage option list seeded `0` (default, preselected) plus `5 / 10 / 15 / 20`, rendered as a dropdown whose options show the percentage on the left and the server-computed currency amount (percentage × order total) on the right. The buyer's selection persists on the order as `RegistrationOrderPlatformContribution` (percentage, computed amount, setting-version snapshot).
  Enablement, text, and percentages are managed exclusively through `Admin`-classified endpoints restricted to **instance administrators**; tenant admins can neither see-in-management, enable, nor alter them (fail-closed authorization tests). Rationale: instance operators pay for/host the software; tenant-level enablement is an abuse vector.
- **Why:** User requirement; enables the "ISLAMU nonprofit single-tenant instance with fully free Islamic events funded by voluntary contributions" scenario, and a transparent commercial SaaS offering — while every self-hosted instance stays zero-fee by default (open-source fairness).
- **Alternatives considered:** Hardcoded tip UI — rejected (must be per-instance data); tenant-level enablement — explicitly rejected by requirement; contribution as a ticket order line — rejected (different money destination; would corrupt organizer earnings/capacity semantics); governance-settings key-value storage — rejected for the structured content (typed versioned entities chosen so orders can snapshot deterministically), while the *enablement lock* semantics follow the existing instance-lock convention of the 5-tier settings cascade.
- **Consequences:** Order totals = organizer-directed line totals + instance-directed contribution, kept separate everywhere (DTOs, exports, future payment split); contribution > 0 on an otherwise free order makes the order payable — until payments ship, the contribution UI is composed only where a payment path will exist; `OrganizerEarningsCalculator` is a pure, decimal-exact Application service.
- **Files/layers:** Domain entities + Admin API + Blazor instance settings (Phase 4, Task 4.5); order component + checkout composition (Phase 5, Task 5.10).

### D19 — Hi.Events is a behavior catalog only: concepts yes, code never
- **Decision:** Adopt Hi.Events' behavioral lessons: reserve inventory **before** collecting PII; visible reservation expiry with countdown, navigation-away warning, explicit abandon, and business-state-specific recovery screens; snapshot every mutable commercial fact on order lines; shared-capacity visualization (used vs total, pool-overrides-product warning); buyer questions separated from participant questions with buyer-to-participant copy controls; anti-enumeration ticket/order lookup. Adapt its concepts onto ISLAMU aggregates per the report's §9.2 mapping table. Reject its persistence, authorization, money, idempotency, and side-effect machinery wholesale (§9.3, now constraint §4.13). **Code reuse is categorically forbidden** — ISLAMU Event's CLA-based dual-licensing model (non-AGPLv3 licenses for recipients who cannot use AGPLv3) would be destroyed by importing third-party AGPLv3 code whose authors never signed the CLA; the report's §10 code-reuse permission is overridden. Implementation is clean-room from the report and this plan only. Its concrete defects (§7) become mandatory acceptance criteria in Phases 4–8. Its commercial breadth (Stripe/refunds/invoices/taxes/promo/affiliates/add-ons/waitlist offers/check-in/transfers/lookup) is a deferred-feature inventory recorded once in Task 14.8 — not silent scope growth.
- **Why:** Report §14 final recommendation (behavior catalog) + the ISLAMU CLA/dual-licensing business model; converts someone else's production experience into cheap, early test criteria at zero licensing risk.
- **Alternatives considered:** AGPLv3 code reuse with provenance pinning (the report's §10 suggestion) — rejected: legally compatible for a pure-AGPLv3 project but fatal to CLA-based dual-licensing.
- **Consequences:** Zero new phases from the report; strengthened acceptance criteria; one new docs task (14.8); no-code-copy rule in §4.13; implementation agents never open the Hi.Events repository during coding.
- **Files/layers:** Cross-cutting; ADR-018 records the rationale (Task 0.2).

### D20 — Registration organizer operations extend the implemented Studio workspace
- **Decision:** Keep the shipped `WorkspaceRegistry` and single `shell.workspace-nav` architecture. `StudioWorkspaceNavigation` continues to own actor-level navigation and swaps to `StudioEventNavigation` for event routes. Event sections are added only when the shared `EventDto` contains their management relation. Cross-event Orders and Attendees use one new authenticated, private/no-store HAL resource, `StudioContextDto`, returned by `GET /api/studio/context?actorId={optionalActorHint}` and consumed through a scoped `IStudioContextService`; the server validates the actor hint against current principal authority.
- **Why:** Studio, route classification, actor switching, shared event context, and relation-mapped navigation already exist. Reusing them keeps deep links, mobile projection, RTL, accessibility landmarks, persisted shell state, and revocation behavior consistent while preserving HAL as the action/navigation authority.
- **Alternatives considered:** standalone `Pages/Events/Manage/**` navigation — rejected as a second organizer product; hardcoded role-based Studio links — rejected because they drift from server authority; one API probe per sidebar item — rejected as noisy and race-prone; a third event sidebar — rejected by ADR-019.
- **Consequences:** Organizer pages land under `/studio/**`; attendee checkout remains under `/registration/**`. Phase tasks extend the centralized `Routes.razor`, `StudioEventNavigation`, `StudioEventShell` or focused Studio pages, and their existing test suites. Actor Overview/Events stay as the base navigation; additional cross-event links render only from `StudioContextDto._links`. Check-in, Communications, and Analytics remain absent until an owning workstream ships both route and HAL relation.
- **Relation mapping:** `configure-participation` → Registration; `manage-ticket-types|manage-capacity-pools` → Ticketing; `view-registration-orders` → Orders; `view-participants` → Attendees; `manage-registration-workflow` → Forms; `manage-registration-channels|view-registration-provider-health` → Integrations. `export-consented-contacts` is an action inside Attendees, not another sidebar destination.
- **Files/layers:** API/Application contract in Phase 5; Blazor `Routes.razor`, `Pages/Studio/**`, `StudioWorkspaceNavigation.razor`, `StudioEventNavigation.razor`, and focused client tests across Phases 2/4/5/6/7/9/13.

---

## 6. Implementation Phases

> Phase ordering is a strict dependency chain except where noted. Each phase is a reviewable vertical slice ending in exactly one Release build and at most one test-project run. Tasks fold their own tests and doc updates in; there are no standalone QA/doc tasks. NSwag client regeneration + `schemas/openapi_islamu-event.json` refresh is a discrete final step of any phase that changes the API surface (kept inside the phase's last API/Blazor task per governance).

### Phase 0: Governance, ADRs, And Contract Intent
- **Goal:** Lock the architecture in decision records, create the machine-readable contribution-contract intent, and resolve the migration-baseline ordering with the erasure workstream before any code.
- **Depends on:** Nothing.
- **Relevant files:** `docs/adr/ADR-016-registration-data-collection-context.md` (new), `docs/adr/ADR-017-event-participation-authority-model.md` (new), `docs/adr/ADR-018-registration-order-ticketing-aggregate.md` (new), `.claude/contract/intents.yaml` (existing), `docs/GOVERNANCE.md` (existing, PublicTransactional forward note), `dev/active/registration-data-collection/*` (existing).
- **Related skills/rules:** `skill-authoring` conventions for intent shape; `docs/DOCUMENTATION_STYLE_GUIDE.md`.
- **Acceptance criteria:** Three ADRs accepted-status with decision/consequences sections mirroring D1–D16; new `registration-data-collection` intent lists must-reads, skills, rules, paths, minimum tests, docs, unique acceptance (incl. consultation anti-pattern lists as forbidden moves); migration-ordering decision recorded in context file.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Docs-only phase; revert files. If architecture tests flag intent-schema violations, fix the YAML before proceeding.

#### Task 0.1: Author ADR-016 (Registration Data Collection bounded context & provider channels)
- **Type:** create
- **Layer:** Docs
- **Files:** `docs/adr/ADR-016-registration-data-collection-context.md` (new)
- **Description:** Record D1, D2, D3, D5, D6, D7, D14 with context, decision, alternatives, consequences; cite consultation sections; include the invariant block ("ISLAMU owns the workflow and normalized record...").
- **Acceptance Criteria:**
  - [ ] ADR follows existing ADR file conventions (verified against ADR-015 format)
  - [ ] All anti-patterns from consultation §24 listed as rejected alternatives
- **Dependencies:** none
- **Effort:** M
- **Required Skills/Rules:** documentation style guide

#### Task 0.2: Author ADR-017 (participation authority) and ADR-018 (order/ticket aggregate)
- **Type:** create
- **Layer:** Docs
- **Files:** `docs/adr/ADR-017-event-participation-authority-model.md` (new), `docs/adr/ADR-018-registration-order-ticketing-aggregate.md` (new)
- **Description:** ADR-017 records D8, D9, D10, D12 (four authorities, provenance, participation config, PublicTransactional, consent subjects). ADR-018 records D4, D11, D16, D17, D18 (buyer–order–participant–ticket, capacity pools/holds, state machines, five pricing modes, instance monetization, `AwaitingPayment` boundary, payment explicitly out of scope) **and** the Hi.Events research evidence: why ISLAMU does not use "attendee-as-ticket" or mutable published products (report §8/§11.1), the adopt/adapt/reject boundary (D19, report §9), and the **no-code-copy rule**: ISLAMU's CLA-based dual-licensing forbids copying any Hi.Events (AGPLv3) code — behavior/design/data-model lessons only, clean-room implementation, report §10's code-reuse permission explicitly overridden.
- **Acceptance Criteria:**
  - [ ] Report 2 §33 anti-patterns listed as rejected alternatives
  - [ ] Payment boundary documented as a named future ADR dependency
  - [ ] Hi.Events adopt/adapt/reject boundary + CLA/dual-licensing **no-code-copy** rule recorded, citing `hi-events-report.md` §9–§10 and stating the override of §10
- **Dependencies:** 0.1
- **Effort:** M

#### Task 0.3: Add `registration-data-collection` intent to the contribution contract
- **Type:** modify
- **Layer:** Docs
- **Files:** `.claude/contract/intents.yaml` (existing)
- **Description:** Model the entry on the `webhook-delivery-redesign` intent: triggers, must_read_docs (ADRs 016–018, this plan/context/tasks, DOMAIN/API/AUTHORIZATION/SECURITY-MODEL/WEBHOOKS/CONTACT_SHARING/MULTI_TENANCY/OUTBOX_PATTERN/TESTING), load_skills/rules, paths_in_scope (Domain/Application/Persistence/Infrastructure/API/Blazor registration+ticket+participation+provenance globs, cerbos, tests, docs), minimum_tests (all nine projects), docs_to_update, unique_acceptance (external completion ≠ confirmation; attempts pin versions; typed answers; HAL authority; PublicTransactional controls; no provider IO in transactions), forbidden_without_approval (both consultation anti-pattern lists condensed), verification_commands.
- **Acceptance Criteria:**
  - [ ] YAML parses; architecture tests that validate contract files stay green
  - [ ] Intent cross-references this workstream's three dev docs
- **Dependencies:** 0.1, 0.2
- **Effort:** M

#### Task 0.4: Resolve migration-baseline ordering with the erasure workstream
- **Type:** investigate
- **Layer:** DevOps
- **Files:** `dev/active/registration-data-collection/registration-data-collection-context.md` (existing), `src/Explore.Persistence/Migrations/` (observe only)
- **Description:** Verify the three privacy-erasure-owned init lanes and record that registration's first allowed migration is a generated additive migration after the owning phase's model/configuration/seeder work is complete. Do not create or delete any migration artifact in this task.
- **Acceptance Criteria:**
  - [ ] Decision + date recorded in context Key Decisions
- [ ] Tasks/context record that the baseline exists and registration must not regenerate or rewrite it
- **Dependencies:** none
- **Effort:** S

---

### Phase 1: Event Provenance, Organizer Authority, And Public Actions
- **Goal:** Deliver the discovery-only product mode: typed provenance, contributor vs organizer separation, moderated external actions, claim workflow, non-removable community badge — with authorization fail-closed.
- **Depends on:** Phase 0.
- **Relevant files:** existing — `src/Explore.Domain/Event.cs`, `src/Explore.Persistence/Configurations/Entities/EventConfiguration.cs`, `src/Explore.Persistence/ExploreDbContext.DbSets.cs`, `ExploreDbContext.QueryFilters.cs`, `src/Explore.Persistence/Seed/LookupTableSeeder.cs`, `src/Explore.Application/Features/Events/**`, `src/Explore.API/Controllers/EventController.cs` (name verified at implementation), `src/Explore.API/Hateoas/Policies/EventLinkPolicy.cs`, `src/Explore.API/Hateoas/RouteNames.cs`, `src/Explore.Application/Hateoas/LinkRelations.cs`, `cerbos/policies/islamuevent_event.yaml`, `src/Explore.Blazor.Client/Pages/Events/**`; new — `src/Explore.Domain/EventProvenanceType.cs`, `Enums/EventProvenanceTypeEnum.cs`, `EventPublicAction.cs`, `EventPublicActionKind.cs`, `Enums/EventPublicActionKindEnum.cs`, `EventPublicActionHealthState.cs`, `Enums/EventPublicActionHealthStateEnum.cs`, `EventOrganizerClaim.cs`, `EventOrganizerClaimStatus.cs`, `Enums/EventOrganizerClaimStatusEnum.cs`, `ValueObjects/ExternalActionUrl.cs`, plus configurations/features/policies listed per task.
- **Related skills/rules:** `domain.md`, `efcore-persistence.md`, `application-layer.md`, `api-controllers.md`, `api-hateoas.md`, `blazor-client.md`, `cqrs-mediatr-guidelines`, `auth-patterns`.
- **Acceptance criteria:** FR-PROV-01…08 satisfied; contributor authorization matrix (§23) enforced server-side; `IsUserReported`/`EventUrl` deleted; community badge non-removable and HAL/DTO-derived; external links validated (HTTPS, scheme allowlist, no open redirect).
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Slice is additive except the two deleted Event fields; if downstream compilation reveals unexpectedly broad `EventUrl` usage, complete the removal within the phase (no compatibility shim) and record the discovered surface in context.

#### Task 1.1: Provenance typed state on `Event` + `ActorId` semantics decision
- **Type:** modify + investigate
- **Layer:** Domain
- **Files:** `src/Explore.Domain/Event.cs` (existing), `src/Explore.Domain/EventProvenanceType.cs` (new), `src/Explore.Domain/Enums/EventProvenanceTypeEnum.cs` (new)
- **Description:** Add `EventProvenanceTypeId` (int FK, required), `SubmittedByUserId` (Guid?, FK User), `OrganizerActorId` (Guid?, FK Actor), `SourcePublisherName` (string?), keep existing `SourceUrl` semantics on the new `EventPublicAction` instead of Event; delete `IsUserReported` and `EventUrl`. Enumerate `ActorId` usages (`rg "ActorId" src/`), decide rename vs documented narrowing, execute the decision in this slice, and record it. Add domain rule class `Services/Registration/EventAuthorityRules.cs` (new) computing listing/participation/data/commercial authority from typed state; unit-test it.
- **Acceptance Criteria:**
  - [ ] `IsUserReported`/`EventUrl` no longer exist anywhere in `src/`
  - [ ] `EventAuthorityRules` denies organizer authority when `OrganizerActorId` is null (fail-closed test)
  - [ ] Provenance is required state — no implicit default in the entity
- **Dependencies:** 0.4 (ordering only for persistence artifacts)
- **Effort:** L

#### Task 1.2: `EventPublicAction` + kinds + health states + URL value object
- **Type:** create
- **Layer:** Domain
- **Files:** new domain files listed above; `src/Explore.Domain/ValueObjects/ExternalActionUrl.cs` (new)
- **Description:** Typed, ordered action collection (`ORIGINAL_SOURCE`, `EXTERNAL_EVENT_PAGE`, `EXTERNAL_REGISTRATION`, `OPTIONAL_QUESTIONNAIRE`, `LIVESTREAM`, `ORGANIZER_CONTACT`), health states (`PendingReview/Active/Broken/Unsafe/Disabled/Expired`), `IsPrimary` with a domain rule enforcing at most one primary participation CTA, and `ExternalActionUrl` value object (normalized parse, HTTPS-only default, blocked schemes, no fragments of credential userinfo, stored destination domain for disclosure).
- **Acceptance Criteria:**
  - [ ] Value-object tests reject `javascript:`, `data:`, `file:`, protocol-relative and userinfo URLs
  - [ ] Domain rule test: two primary actions rejected; zero actions valid
- **Dependencies:** 1.1
- **Effort:** M

#### Task 1.3: `EventOrganizerClaim` aggregate
- **Type:** create
- **Layer:** Domain
- **Files:** `src/Explore.Domain/EventOrganizerClaim.cs` (new), `EventOrganizerClaimStatus.cs` (new), `Enums/EventOrganizerClaimStatusEnum.cs` (new)
- **Description:** Fields per consultation §8 (`ClaimantActorId`, status `Pending/EvidenceRequired/Approved/Rejected/Withdrawn/Expired`, evidence type/reference, reviewer, decision reason code, `ConcurrencyStamp`); domain transition methods (no silent status sets); approval sets `Event.OrganizerActorId` but **never** touches historical data (rule + test).
- **Acceptance Criteria:**
  - [ ] Invalid transitions throw; approval effect limited to organizer assignment
- **Dependencies:** 1.1
- **Effort:** M

#### Task 1.4: Persistence for Phase 1 entities
- **Type:** create/modify
- **Layer:** Persistence
- **Files:** new `src/Explore.Persistence/Configurations/Entities/{EventPublicActionConfiguration,EventOrganizerClaimConfiguration,EventProvenanceTypeConfiguration,EventPublicActionKindConfiguration,EventPublicActionHealthStateConfiguration,EventOrganizerClaimStatusConfiguration}.cs`; existing `ExploreDbContext.DbSets.cs`, `ExploreDbContext.QueryFilters.cs`, `Seed/LookupTableSeeder.cs`, `schemas/islamu-event.md`; repository contracts/implementations move to Task 1.5 when real access paths exist.
- **Description:** Tenant + soft-delete filters, stable lookup IDs in the seeder (document ID ranges in `schemas/islamu-event.md`), unique index one-primary-action-per-event (filtered). Do not add speculative repositories before Application consumers exist. Commit `ff30795a2` owns the additive `20260728152646_AddParticipationHandlingModes` migration, designer, and snapshot. Preserve migration history and never rewrite committed migration artifacts; the commit does not prove database application.
- **Acceptance Criteria:**
  - [ ] Lookup seeder parity (enum ↔ seeded rows) covered by existing seeder-parity test pattern
  - [ ] Query filters verified by a persistence test for cross-tenant invisibility
- **Dependencies:** 1.1–1.3; migration generation additionally depends on `atproto-federation-actor-lifecycle` Task 2.1 because both workstreams modify the shared Event/Actor model snapshot
- **Effort:** L

#### Task 1.5: Application features — public actions, claims, provenance exposure
- **Type:** create/modify
- **Layer:** Application
- **Files:** new `src/Explore.Application/Features/EventPublicActions/{Requests,Handlers}/**`, `Features/EventOrganizerClaims/{Requests,Handlers}/**`; existing `Features/Events/**` (event DTOs gain provenance + actions), `src/Explore.Application/DTOs/**`, AutoMapper profiles, validators (manual instantiation)
- **Description:** Commands: manage actions (organizer/curator), submit/withdraw claim, review claim (curator). Correction suggestions and unsafe-link reports reuse the existing hardened `SubmitEventReportCommand` intake with stable `event_correction_suggestion` / `unsafe_external_link` subcategory codes rather than duplicating report persistence. Queries: actions by event, claims by event/claimant. Event DTOs expose normalized lookups (`ProvenanceTypeId/Code/Name`) and the ordered action collection with semantic labels; **no** capability booleans. `BaseCommandResponse<Guid>` conventions; `IUnitOfWork` where multi-write (claim approval writes claim + event).
- **Acceptance Criteria:**
  - [ ] Contributor cannot invoke registration/ticket/attendee features (authorization action constants added; fail-closed handler checks)
  - [ ] Claim approval command is transactional and idempotent under retry
- **Dependencies:** 1.4
- **Effort:** L

#### Task 1.6: API + Cerbos + HAL for provenance/claims/actions
- **Type:** create/modify
- **Layer:** API
- **Files:** new `src/Explore.API/Controllers/EventPublicActionController.cs`, `EventOrganizerClaimController.cs`, new `src/Explore.API/Hateoas/Policies/{EventPublicActionLinkPolicy,EventOrganizerClaimLinkPolicy}.cs`; existing `EventLinkPolicy.cs`, `RouteNames.cs`, `LinkRelations.cs`, `cerbos/policies/islamuevent_event.yaml`, new `cerbos/policies/islamuevent_event_organizer_claim.yaml` + `_schemas` entry
- **Description:** Named routes + classifications (`Public` reads, `Authenticated` writes), ProducesResponseType coverage, HAL relations `view-original-source`, `external-event-page`, `external-registration`, `optional-questionnaire`, `claim-event`, `suggest-correction`, `report-external-link`; Cerbos attributes per D9; redirect endpoint resolves stored action IDs only (no `?url=`); `noopener/noreferrer` guidance in DTO metadata. Regenerate OpenAPI + NSwag client as the discrete contract step; update `docs/API_CHANGELOG.md`.
- **Acceptance Criteria:**
  - [ ] Contract invariants + endpoint-classification architecture tests pass
  - [ ] Cerbos parity test covers claim review deny-by-default
  - [ ] No open-redirect endpoint exists (API test asserts stored-action resolution)
- **Dependencies:** 1.5
- **Effort:** L

#### Task 1.7: Blazor — community badge, provenance panel, claim/correction flows
- **Type:** create/modify
- **Layer:** Blazor
- **Files:** existing `src/Explore.Blazor.Client/Pages/Events/**` (detail + card components), new `Components/Events/EventProvenancePanel.razor` (+ `.razor.css` BEM), new claim submission dialog components
- **Description:** Card badge "Community reported" + detail panel (source domain, last-checked date, suggest-correction, claim, report-unsafe-link) rendered **only** from DTO/HAL data; all mutating affordances gated by `_links`; accessible labels; RTL-safe; external links marked with domain disclosure.
- **Acceptance Criteria:**
  - [ ] Badge cannot be hidden by contributor-controlled data (derives from provenance lookup code)
  - [ ] bUnit tests: affordances absent without links; badge present for `COMMUNITY_REPORTED`
- **Dependencies:** 1.6
- **Effort:** M

---

### Phase 2: Typed Participation Configuration And HAL Participation Actions
- **Status:** Tasks 2.1 through 2.5 have checked implementation boxes. Commit `ff30795a2` supplies the participation/ticketing migration artifact. Corrected verification has executed; broad API remains non-green from environment/shared failures, and database application/runtime rollout remains unproved.
- **Goal:** Replace `IsRegistrationRequired` with the typed participation model, make the API author every participation CTA (zero-action events valid, external-managed first-class), and make Studio Registration the canonical organizer configuration surface.
- **Depends on:** Phase 1.
- **Relevant files:** existing — `src/Explore.Domain/Event.cs`, `Features/Events/**`, `EventLinkPolicy.cs`, seeder, Cerbos event policy, Blazor event detail, `src/Explore.Blazor.Client/Pages/Studio/StudioEventShell.razor`, `Components/Shell/Workspaces/StudioEventNavigation.razor`, `Routes.razor`; new — `src/Explore.Domain/EventParticipationConfiguration.cs`, `ParticipationHandlingMode.cs` + enum, `AdvanceRegistrationObligation.cs` + enum, `IdentityAccessMode.cs` + enum, configuration/feature/policy files per task.
- **Related skills/rules:** as Phase 1.
- **Acceptance criteria:** FR-ACT-01…07 satisfied; §5 scenario table representable; labels semantically accurate; no completion inference from clicks; `IsRegistrationRequired` deleted.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** If event-creation flows in Blazor break on the new required configuration, ship the organizer configuration UI inside this phase (Task 2.4) before deleting the boolean — deletion is the last commit of the phase.

#### Task 2.1: `EventParticipationConfiguration` + three mode lookups
- **Status:** Source complete; focused Domain checks pass 53/53.
- **Type:** create/modify
- **Layer:** Domain
- **Files:** new domain files above; existing `src/Explore.Domain/Event.cs` (delete `IsRegistrationRequired`), extend `Services/Registration/EventAuthorityRules.cs`
- **Description:** 1:1 entity with the three lookup FKs + guest-recovery policy fields (verified-email-required / unverified-accepted / email-optional / capability-link-only / no-recovery) + `ConcurrencyStamp`; domain rules validating legal combinations (e.g., `EXTERNAL_MANAGED` forbids native workflow attachment; `INFORMATION_ONLY` forbids registration actions) with unit tests for the §5 scenario table.
- **Acceptance Criteria:**
  - [x] All ten §5 scenarios constructible; illegal combinations rejected with typed errors
  - [x] `GuestRecoveryPolicyEnum` remains an OpenAPI and generated-client string enum with the exact five public literals
- **Dependencies:** Phase 1 complete
- **Effort:** M

#### Task 2.2: Persistence + seeding for participation lookups
- **Status:** Source and committed migration artifact exist. Corrected verification has executed, EF reports no pending model changes after the additive legacy-pricing migration, and database application/runtime rollout remains unproved.
- **Type:** create/modify
- **Layer:** Persistence
- **Files:** new configurations for the four entities; existing DbSets/QueryFilters/LookupTableSeeder; additive migration per 0.4 gate
- **Acceptance Criteria:**
  - [x] Stable IDs documented; seeder parity green
  - [x] Commit `ff30795a2` owns `20260728152646_AddParticipationHandlingModes.cs`, its designer, and the snapshot
  - [ ] Database application/runtime rollout is evidenced separately
- **Dependencies:** 2.1
- **Effort:** M

#### Task 2.3: Application + API — configure-participation and action synthesis
- **Status:** Source and generated contracts complete; focused Application 3/3, API/HAL 15/15, and API inventory 1/1 checks pass.
- **Type:** create/modify
- **Layer:** Application + API
- **Files:** new `Features/EventParticipation/{Requests,Handlers}/**`; existing event detail queries, `EventLinkPolicy.cs`, `RouteNames.cs`, `LinkRelations.cs`
- **Description:** `configure-participation` requires `ManageRegistrations`. Verified organizer controllers are resolved from `OrganizerActor`; explicitly assigned event roles require `EventRegistrationManage`. Community reporters receive no implicit `EventOwner`. The event public read model emits `start-registration`, `sign-in-to-register`, or `external-registration` according to participation mode and authentication state. `EventDto.PublicActions` and ATProto registration URIs are both filtered through `EventAuthorityRules`. HAL relation `configure-participation` is management-only. Generated contract and changelog are current.
- **Acceptance Criteria:**
  - [x] API integration tests: per-mode link emission matrix (information-only: none; external-managed: external only; platform-managed: native)
  - [x] External labels: "View original event page" pre-verification vs "Register on organizer website" post-verification
  - [x] Authorization tests allow OrganizerActor controllers or explicit `EventRegistrationManage` assignment and deny community reporters, unrelated controllers, tenant admins, instance admins, and machines
- **Dependencies:** 2.2
- **Effort:** L

#### Task 2.4: Blazor — Studio participation configuration + public CTA rendering
- **Status:** Source complete and `Explore.Blazor.Client` Release builds. bUnit execution remains blocked by externally owned compiler errors.
- **Type:** create/modify
- **Layer:** Blazor
- **Files:** existing `Pages/Events/Components/EventRegistration.razor` (public CTA refactor), `Pages/Studio/StudioEventShell.razor`, `Components/Shell/Workspaces/StudioEventNavigation.razor`, `Routes.razor`; new Studio-owned `Components/Studio/ParticipationConfigurationEditor.razor` (+ scoped CSS)
- **Description:** Organizer editor lives at `/studio/events/{eventId}/registration` and both the page and sidebar entry require the event's `configure-participation` link. Public detail renders CTAs strictly from `_links`/action DTOs with accurate labels; zero-action events show only platform actions (save/share/calendar/report); outbound external clicks are recorded via the Phase 2 aggregate engagement endpoint (no identity). Replace the current legacy `registration|registrations` sidebar predicate with the management-specific relation; attendee `start-registration` links never authorize Studio.
- **Acceptance Criteria:**
  - [x] Source/contract: no public registration CTA without `start-registration`, `sign-in-to-register`, or `external-registration`; Studio Registration is absent without `configure-participation`; external CTA never claims ISLAMU registration
  - [ ] bUnit execution after the 17 externally owned `ProgramSectionsDialogTests.cs` compiler errors are fixed outside this workstream
- **Dependencies:** 2.3
- **Effort:** M

#### Task 2.5: Aggregate outbound-engagement counter
- **Status:** Source complete; focused metric checks pass 2/2.
- **Type:** create
- **Layer:** Application + API
- **Files:** new `Features/EventPublicActions/Requests/Commands/RecordActionEngagementCommand.cs` + handler; new metrics in `src/Explore.Application/Telemetry/BusinessMetrics.cs` (existing file)
- **Description:** Anonymous, aggregate-only engagement recording (`EventId`, `ActionId`, `OccurredAt`, surface, outcome) with bounded metric labels (`action_kind`, `outcome`); no user identity, no per-user rows; a click is never called a registration anywhere (metric names reviewed).
- **Acceptance Criteria:**
  - [x] Metric labels bounded; no tenant/event IDs in labels; focused tests pass
- **Dependencies:** 2.3
- **Effort:** S

---

### Phase 3: Guest Transaction Security Foundation (`PublicTransactional`)
- **Status:** Tasks 3.1 through 3.3 are source complete and verification has executed. The canonical build has 0 errors and 5,162 worktree-wide warnings; Architecture is 330/340 passed, 9 unrelated failed, 1 skipped, so the phase gate is not globally green. The Phase 2 migration artifact exists in commit `ff30795a2`, but database application is not evidenced. No Phase 5 product endpoint/persistence work is complete.
- **Goal:** Introduce the governed anonymous-mutation endpoint class and the reusable guest capability-token primitives before any guest-facing endpoint exists.
- **Depends on:** Phase 0 (ADR-017); independent of Phases 1–2 code but sequenced after them to keep one schema stream.
- **Relevant files:** `src/Explore.API/Attributes/EndpointClass.cs`, `EndpointClassificationAttribute.cs`, `src/Explore.API/OpenApi/EndpointClassificationTransformer.cs`, `src/Explore.API/Extensions/RateLimitingExtensions.cs` (policy), `src/Explore.API/Program.cs` (middleware order), `src/Explore.Application/Contracts/Services/IGuestCapabilityTokenService.cs` (contract), `src/Explore.Infrastructure/Services/GuestCapabilityTokenService.cs` (implementation), `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs` (DI), `src/Explore.Domain/ValueObjects/CapabilityTokenHash.cs`, `tests/Event.Architecture.Tests/EndpointClassificationArchitectureTests.cs`, `docs/GOVERNANCE.md`, `docs/QUICK_REFERENCE.md`, `docs/SECURITY-MODEL.md`.
- **Related skills/rules:** `auth-patterns`, `blazor-bff-patterns`, `api-controllers.md`, `tests.md`.
- **Acceptance criteria:** FR-GUEST-04/06 foundations; the class exists with enforced invariants (rate policy, antiforgery, idempotency, classification) and zero endpoints yet; governance docs updated; `Public` semantics untouched.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` *(repeat justified: the classification governance tests extended here live in this project)*
- **Rollback / failure handling:** Foundation-only phase; nothing consumes it until Phase 5. Revert cleanly if the antiforgery investigation (3.2) demands a different token transport.

#### Task 3.1: `EndpointClass.PublicTransactional` + enforcement
- **Status:** Source complete; focused PublicTransactional governance passes 6/6, implementation-pass endpoint classification passes 4/4, and idempotency passes 6/6.
- **Type:** modify/create
- **Layer:** API + tests
- **Files:** `src/Explore.API/Attributes/EndpointClass.cs` (existing), `EndpointClassificationTransformer.cs` (existing), `tests/Event.Architecture.Tests/EndpointClassificationArchitectureTests.cs` (existing), new `tests/Event.Architecture.Tests/PublicTransactionalGovernanceTests.cs`
- **Description:** Add enum value + XML doc; transformer emits `x-endpoint-class: PublicTransactional`; new architecture tests assert every `PublicTransactional` action carries `[AllowAnonymous]`, the `public_transactional` rate-limit policy, and idempotency requirement metadata for create/finalize verbs. Architecture tests forbid API antiforgery metadata on `PublicTransactional` actions because same-site browser writes are protected at the BFF proxy boundary; direct bearer/API-key traffic is not browser-antiforgery validated. Update `docs/GOVERNANCE.md` classification table + `docs/QUICK_REFERENCE.md` rules and rate-limit table.
- **Acceptance Criteria:**
  - [x] Governance enforces anonymous classification, `public_transactional`, required idempotency metadata/middleware, and the OpenAPI idempotency boolean
- **Dependencies:** ADR-017
- **Effort:** M

#### Task 3.2: `public_transactional` rate policy + antiforgery decision
- **Status:** Source complete; the policy is 10 requests per 60 seconds per IP and uses `NoLimiter` in `Testing`. Anonymous browser mutations use BFF proxy antiforgery, while direct API clients do not use the browser antiforgery pairing. BFF proxy checks pass 20/20.
- **Type:** create + investigate
- **Layer:** API
- **Files:** `src/Explore.API/Program.cs` (existing — rate limiter section only; middleware order preserved), `docs/SECURITY-MODEL.md`
- **Description:** Dedicated fixed-window IP+session policy (limits documented; disabled in `Testing` like others); investigate + document the BFF antiforgery path for anonymous browser mutations (cookie+token pairing through `Explore.Blazor` YARP) and record the decision; wire idempotency middleware applicability to the new class.
- **Acceptance Criteria:**
  - [x] Policy registered and documented; `Testing` uses `NoLimiter`
  - [x] Anonymous BFF proxy antiforgery and direct API distinction recorded in context
- **Dependencies:** 3.1
- **Effort:** M

#### Task 3.3: Guest capability-token primitives
- **Status:** Source complete; Domain capability checks pass 3/3 and Infrastructure capability checks pass 5/5.
- **Type:** create
- **Layer:** Application (contract) + Infrastructure (impl)
- **Files:** new `src/Explore.Application/Contracts/Services/IGuestCapabilityTokenService.cs`, new `src/Explore.Infrastructure/Services/GuestCapabilityTokenService.cs`, new `src/Explore.Domain/ValueObjects/CapabilityTokenHash.cs`, registration in `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs` (existing)
- **Description:** High-entropy (≥256-bit) token generation, constant-time hash comparison, storage of hash only, scoping payload (order id + purpose), expiry/rotation policy hooks; explicit "token ≠ identity proof" doc comment; unit tests incl. timing-safe comparison and non-guessability (format).
- **Acceptance Criteria:**
  - [x] 256-bit token primitives reveal plaintext once, retain hashes only, and compare hashes in constant time
- **Dependencies:** 3.1
- **Effort:** M

#### Phase 3 historical verification checkpoint (superseded by the current corrected matrix)
- The focused PublicTransactional governance, endpoint-classification implementation pass, idempotency, BFF proxy, Domain capability, and Infrastructure capability checks pass 6/6, 4/4, 6/6, 20/20, 3/3, and 5/5 respectively.
- Oracle follow-up result: **PASS**, with no Critical or High issues. The governance metadata-bypass and secret-formatting findings were fixed.
- At that checkpoint, the new rate-policy test stopped before discovery on six unrelated `CustomPropertyDefinitionControllerTests` errors caused by missing DTO members.
- At that checkpoint, the canonical Release build reported 12 unrelated errors: six in that API test and six in Blazor client custom-property generated-contract call sites.
- At that checkpoint, full Architecture executed 315 tests: 304 passed, 10 unrelated tests failed, and 1 was skipped. The new `PublicTransactional` checks weren't among the failures.
- **Superseded next slice:** Phase 4 later received review findings and source corrections. The current corrected verification matrix and direct final review supersede this checkpoint. Commit `ff30795a2` supersedes the earlier missing-migration claim.

---

### Phase 4: Ticket Catalog, Capacity Pools, Entitlements, And Instance Monetization
- **Status:** SOURCE COMPLETE / CORRECTED VERIFICATION EXECUTED / DOCKER RUNTIME PROOF BLOCKED. Tasks 4.1 through 4.5 are implemented, the final diff has no unresolved High or Major review finding, and owned focused lanes are green. Broad projects are not globally green, and the two PostgreSQL row-lock tests remain unavailable without Docker.
- **Goal:** Versioned admission products with five pricing modes (D17), shared capacity pools and session/day entitlements; Studio Ticketing authoring; instance-admin-only monetization configuration (D18); delete decorative prices.
- **Depends on:** Phases 1–2 (organizer authority + participation config).
- **Relevant files:** existing — `Event.cs`, `EventSession.cs` (Price removal), seeder, DbContext partials, event manage API, `Routes.razor`, `StudioEventNavigation.razor`, `StudioEventShell.razor`; new — `src/Explore.Domain/EventTicketCatalogVersion.cs`, `EventTicketType.cs`, `TicketTypeEntitlement.cs`, `EventCapacityPool.cs`, `PlatformFeePolicy.cs`, `PlatformContributionSetting.cs`, `PlatformContributionOption.cs`, lookups `TicketCatalogStatus`, `TicketPricingMode`, `ParticipantDataCollectionMode`, `EntitlementScopeType`, `EntitlementSelectionRule`, `CapacityOversellPolicy` (+ enums), `Services/Registration/TicketPricingRules.cs`, configurations, `Features/EventTicketing/**`, `Features/PlatformMonetization/**`, `EventTicketTypeController.cs` etc., HAL policies, Cerbos `islamuevent_event_ticket_type.yaml`, Studio ticketing page/components.
- **Related skills/rules:** as Phase 1 + `efcore-migrations.md`.
- **Acceptance criteria:** FR-TICKET-01/02/08/09/13 (authoring side); published versions immutable (edit → new version, domain-enforced); all five D17 pricing modes authorable with mode-specific validation; new platform-managed Events create no bootstrap ticket catalog; Studio creates an explicit currency-selected draft that supports all five pricing modes; no `XXX` bootstrap catalog; one currency per active catalog enforced; API and persistence accept integer minor-unit amounts without claiming decimal-major or FX conversion; hidden/cross-event ticket identifiers produce generic not-found (report §11.2); monetization settings instance-admin-only and default off/zero; `Event.Price`/`CurrencyCode`/`EventSession.Price` deleted with display price derived from the published catalog.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Price deletion is the phase's final task; all read models (list/detail/SEO/AT Proto lexicon projections if any reference price — enumerate via `rg "\.Price" src/`) must compile against derived display price before deletion commits.

#### Phase 4 verification evidence
- Oracle session `ses_052476c4cffeqCboMjdwpnB2xM` initially returned **FAIL**.
- Corrected mechanisms: legacy Event/EventSession prices removed; price summary sourced from the published catalog; no `XXX` bootstrap catalog; published plus draft/management pool reference guard; non-public ticket child mutators; EF conflict translation bounded to concurrency and named published-catalog/capacity-pool unique constraints; Studio editor request cancellation.
- Release build: 0 errors and 5,162 worktree-wide warnings.
- Current focused evidence: pricing 19 + 10 + 11 + 19 + 13 + 36; pool 4 + 13 + 5 architecture; persistence 11 + 2 architecture; Studio 12 + 14 + 76.
- Corrected project execution: Domain 602/602; Application 3,374/3,377 after fixing two stale removed-price AI assertions, with exactly three unrelated failures; Architecture 330/340 passed, 9 failed, 1 skipped; Secrets 205/205; Infrastructure non-runtime 1,151/1,151; Persistence 96/698 passed and 602 environment/provider-heavy failures, with focused ticketing 11/11; API 1,571/2,114 passed and 543 environment/shared failures, with all seven focused ticketing/monetization classes 23/23; Blazor Integration 398/398; Blazor Client 2,250/2,252 passed, 1 unrelated failure, 1 skipped.
- The three unrelated Application failures are `PublishEventCommandHandlerTests.Handle_WithEnabledAtproto_StagesEventOutboxAfterLocalSaveInsideTransactionWithoutPdsCall`, `UpdateOrganizationCommandHandlerTests.Handle_WhenRequesterIsNotOrgAdmin_ReturnsAuthorizationFailureAndDoesNotSave`, and `EventLocationDisclosureContractTests.Contracts_AreImmutableRecordsAndDoNotReuseGenericLocationDto`. The Blazor Client failure is `LaunchAccessibilitySourceTests.LaunchCriticalPages_ShouldPreserveAccessibilityContracts`.
- Generated additive `src/Explore.Persistence/Migrations/20260729183118_RemoveLegacyEventPricing.cs`, its designer, and the snapshot drop the two nonnegative-price checks and Event/EventSession `price`/`currency_code`. `dotnet ef migrations has-pending-model-changes` reports no changes. Database application remains unproved.
- Verification execution and direct final review are complete, but the broad matrix is not globally green and the Docker-backed row-lock proof remains unavailable.
- Phase 5 remains blocked on accepting or executing the PostgreSQL row-lock proof; Task 5.4 atomic idempotency remains a separate prerequisite.

#### Task 4.1: Catalog domain model with immutable publication and five pricing modes
- **Status:** Source complete; Domain 602/602 passes.
- **Type:** create
- **Layer:** Domain
- **Files:** new entities/lookups listed above; new `Services/Registration/TicketCatalogRules.cs`; new `Services/Registration/TicketPricingRules.cs`; new `src/Explore.Domain/TicketPricingMode.cs` + `Enums/TicketPricingModeEnum.cs`
- **Description:** Catalog version states `Draft/Published/Retired`; publication freezes; mutation of published members throws; new-version cloning; eligibility typed fields (`MinimumAge`, `MaximumAge`, `RequiresGuardian`, `RequiresApproval`); quantity-limit fields (`PerOrder/PerAccount/PerVerifiedContact/PerBookingParty`); entitlements per §15 with selection rules `AllIncluded/FixedSelection/ChooseOne/ChooseUpToN`; one-currency rule across a version. Pricing per D17: `TicketPricingModeId` (`FIXED/FREE/DONATION/PAY_WHAT_YOU_CAN/SLIDING_SCALE`) with `FixedPriceMinor`, `MinimumPriceMinor`, and `SuggestedPriceMinor`; `TicketCatalogRules` enforces per-mode field consistency (FIXED requires price; FREE forbids amounts; DONATION/PWYC allow minimum ≥ 0 with 0-input semantics; SLIDING_SCALE requires minimum + suggested ≥ minimum). The contract accepts already-normalized integer minor units and defines neither decimal-major conversion nor foreign exchange.
- **Acceptance Criteria:**
  - [x] Domain unit tests: publish-freeze, clone-to-draft, currency uniformity, entitlement legality vs Event/Day/Session references
  - [x] Pricing-mode validation matrix unit-tested (each of the five modes × valid/invalid/boundary amounts, incl. 0-allowed cases)
  - [x] Persisted/API money uses `long ...Minor`, percentages use integer basis points (`10_000 = 100%`), and the shipped model accepts already-normalized minor units without decimal-major or FX conversion
- **Dependencies:** Phase 2
- **Effort:** L

#### Task 4.2: Persistence + seeding
- **Status:** Source complete; focused ticketing Persistence passes 11/11. Broad Persistence is 96/698 passed with 602 environment/provider-heavy failures, so the project is not globally green.
- **Type:** create/modify
- **Layer:** Persistence
- **Files:** new configurations + repositories (`IEventTicketCatalogRepository` etc.); existing DbSets/QueryFilters/seeder; additive migration per gate
- **Description:** Filtered unique indexes (one active published catalog per event), FK cascades reviewed (no cascade delete into published versions), stable lookup IDs, integration tests for immutability at DB level (concurrency stamp) and tenant filters.
- **Acceptance Criteria:**
  - [x] Persistence tests: published version rows unmodifiable via optimistic concurrency; pool shared by two ticket types resolves single capacity row
  - [x] Hidden/cross-tenant/cross-event ticket-type lookups return generic not-found (report §7.9/§11.2 lesson)
- **Dependencies:** 4.1
- **Effort:** L

#### Task 4.3: Authoring Application + API + Cerbos + HAL
- **Status:** Source complete; all seven focused ticketing/monetization API classes pass 23/23. Broad Application and API remain non-green only for the current unrelated/environment results recorded above.
- **Type:** create
- **Layer:** Application + API
- **Files:** new `Features/EventTicketing/**` (catalog/ticket/pool/entitlement commands+queries, DTOs, validators), new controllers + link policies, `RouteNames`/`LinkRelations` additions (`manage-ticket-types`, `manage-capacity-pools`), new Cerbos policy + schema
- **Description:** Organizer-only writes (verified organizer authority via Phase 1 rules — community contributor forbidden test); publish command runs one-currency + entitlement + pricing-mode preflight (`TicketPricingRules`); public event read model derives display price ("from X" across active types; "Free / pay what you can" labels for buyer-priced modes). Event HAL emits `manage-ticket-types` and/or `manage-capacity-pools` only when the corresponding Studio Ticketing operations are authorized. Contract regeneration + changelog.
- **Acceptance Criteria:**
  - [x] Contributor-denied and curator-delegation provider parity tests
  - [x] Publish preflight rejects mixed currencies, orphan entitlements, and pricing-mode field inconsistencies
  - [x] Event HAL omits both Ticketing relations for community contributors and external-managed/listing-only events
- **Dependencies:** 4.2
- **Effort:** L

#### Task 4.4: Studio ticket authoring + price display migration + field deletion
- **Status:** Source complete; Studio focused lanes pass 12 + 14 + 76. Broad Blazor Client is 2,250/2,252 passed with one unrelated failure and one skip; browser visual QA is not claimed.
- **Type:** create/modify/delete
- **Layer:** Blazor + Domain
- **Files:** new `Pages/Studio/StudioEventTicketing.razor` (+ scoped CSS and child editors under `Components/Studio/Ticketing/`); modify `Routes.razor`, `StudioEventNavigation.razor`, existing event display components; final commit deletes `Event.Price`, `Event.CurrencyCode`, `EventSession.Price` and updates every reader
- **Description:** Add `/studio/events/{eventId}/tickets`. `StudioEventNavigation` renders Ticketing when either `manage-ticket-types` or `manage-capacity-pools` exists and reuses the shared `StudioEventContextState`; the page renders only controls authorized by their exact relations. The catalog editor covers types, pools, entitlements, windows, limits, pricing modes, and shared-capacity visualization per the Hi.Events organizer lesson. Public price display comes from the derived DTO; delete decorative price fields last.
- **Acceptance Criteria:**
  - [x] Legacy decorative Event/EventSession price members are removed; ticket catalog minor-unit fields own ticket pricing
  - [x] bUnit: Ticketing navigation/route uses `manage-ticket-types OR manage-capacity-pools`; ticket and pool controls are independently gated by their exact relations; catalog item mutations use item HAL links
- **Dependencies:** 4.3
- **Effort:** L

#### Task 4.5: Instance monetization configuration (fee policy + platform contribution)
- **Status:** Corrected source, completed verification evidence, and direct final review are recorded. The UI lives at `/settings/instance`; the API is the separate Admin-class `GET|PUT /api/instance/settings/platform-monetization` resource. The remaining Phase 4 evidence gap is the Docker-backed PostgreSQL row-lock proof.
- **Type:** create
- **Layer:** Domain + Persistence + Application + API + Blazor
- **Files:** new `src/Explore.Domain/PlatformFeePolicy.cs`, `PlatformContributionSetting.cs`, `PlatformContributionOption.cs` (typed option rows: percentage + sort order — no JSON blob); new configurations + DbSets; new `Features/PlatformMonetization/{Requests,Handlers}/**`; new Admin controller actions (`EndpointClass.Admin`, instance-admin authorization) + `RouteNames`; new Blazor instance-admin settings page; `docs/CONFIGURATION.md` + `docs/ADMIN_GUIDE.md` sections
- **Description:** Per D18. Both concepts instance-scoped, versioned (edits create a new version so order snapshots stay deterministic), and **default off/zero**. `PlatformFeePolicy`: percentage + fixed components used only for organizer-earnings computation/display until payments ship. `PlatformContributionSetting`: enable flag; DB-stored heading + body text (localization-ready, example seeded as documentation not data: "Help us help the Ummah…"); option list seeded `0 (default, preselected), 5, 10, 15, 20` percent — fully editable by the instance admin. Authorization: instance administrators only; tenant admins must have no read-write management path (fail-closed tests). No hardcoded strings, percentages, or amounts anywhere in API or client.
- **Acceptance Criteria:**
  - [x] Query and command handlers independently recheck instance-admin authority; tenant-scoped roles have no management path
  - [x] Defaults are off/zero on a fresh instance; enabling is explicit and edits create new active revisions
  - [x] Heading/body/basis-point options round-trip through the separate API resource; platform contribution remains separate from organizer earnings
- **Dependencies:** 4.1
- **Effort:** L

---

### Phase 5: Registration Orders, Inventory Holds, And Guest Checkout Core
- **Goal:** Replace the user-centric aggregate with the order aggregate; atomic capacity holds; free-order confirmation; `AwaitingPayment` boundary; guest capability access; Studio order operations at event and actor scope.
- **Depends on:** Phases 3, 4.
- **Relevant files:** existing (to delete/rewire) — `src/Explore.Domain/EventRegistrationIntent.cs` (delete), `EventRegistration.cs` (rewire), `EventContactShareConsent.cs` (FK rewire), `Features/EventRegistrations/**` (replace), `EventRegistrationController.cs` (replace), `EventRegistrationLinkPolicy.cs` (replace), `cerbos/policies/islamuevent_event_registration.yaml` (evolve), Blazor `EventRegistration.razor`/`EventListRegistrationWorkflow.cs` (replace), Studio routes/navigation; new — `RegistrationOrder.cs`, `RegistrationOrderPii.cs`, `RegistrationOrderLine.cs`, `RegistrationInventoryHold.cs`, `RegistrationOrderPlatformContribution.cs`, `RegistrationOrderStatus` + `BookingPartyType` + `RegistrationInventoryHoldStatus` + `CapacityHoldPolicy` lookups (+ enums), `Services/Registration/RegistrationOrderRules.cs`, `Contracts/Services/IOrganizerEarningsCalculator.cs` + implementation, features `Features/RegistrationOrders/**`, controllers `RegistrationOrderController.cs` (+ guest surface), `StudioContextDto` + query/controller/assembler, background `src/Explore.API/BackgroundServices/InventoryHoldExpiryWorker.cs`, Studio order pages/service.
- **Related skills/rules:** all path rules; `outbox-pattern` for post-commit notifications.
- **Acceptance criteria:** FR-TICKET-03/12/14, FR-GUEST-01…07, NFR-03/04; §31.5 core matrix rows (concurrent last-seat, expired-hold release, snapshot protection, free-order confirm, paid order stops at `AwaitingPayment`); buyer-chosen prices (D17) validated server-side and snapshotted; platform contribution (D18) composed only when instance-enabled; old aggregate deleted; capability-token order access with generic 404 on cross-tenant/guessed IDs; Hi.Events §7-derived criteria (deterministic pool locking, conditional completion transition, per-line allocation) all enforced.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` *(repeat justified: hold/capacity race-safety and new uniqueness constraints are database-level guarantees)*
- **Rollback / failure handling:** This is the highest-risk slice. Sequence inside the phase: (a) new aggregate lands green alongside old, (b) new endpoints/UX land, (c) old aggregate + endpoints deleted in the final commits. If (c) uncovers unexpected dependents (AT Proto records, notifications, email templates), finish rewiring within the phase — no dual-write, no shim; record discoveries in context.

#### Task 5.1: Order aggregate + status machine + PII separation
- **Type:** create
- **Layer:** Domain
- **Files:** new entities/lookups above; `Services/Registration/RegistrationOrderRules.cs`
- **Description:** Per consultation §16/§21: nullable `AccountUserId`, `PurchaserActorId`, `BookingPartyType`, pinned `TicketCatalogVersionId`/`ParticipationConfigurationVersion`/`RegistrationWorkflowVersionId` (nullable until Phase 7), `GuestAccessTokenHash`, timestamps, `ConcurrencyStamp`; `RegistrationOrderPii` separate entity (contact name/email/normalized email/phone/org name, minimal-by-policy); status transitions as pure rules class incl. free path (`Draft→AwaitingRequirements→ReadyForCheckout→Confirmed`), paid boundary (`→AwaitingPayment` terminal for this workstream), approval path, expiry.
- **Acceptance Criteria:**
  - [ ] Transition table unit-tested exhaustively (illegal transitions throw)
  - [ ] No PII fields on `RegistrationOrder` itself
- **Dependencies:** Phases 3, 4
- **Effort:** L

#### Task 5.2: Order lines with snapshots and buyer-chosen prices
- **Type:** create
- **Layer:** Domain + Persistence
- **Files:** new `RegistrationOrderLine.cs` + configuration
- **Description:** `TicketTypeId`, `Quantity`, `UnitPriceAmountSnapshot`, `ChosenUnitPriceAmountSnapshot` (buyer-priced modes, D17), `CurrencyCodeSnapshot`, `LineSubtotalSnapshot`, `TicketTypeNameSnapshot`, `TicketPricingModeSnapshot`, `TicketCatalogVersionId`, `PlatformFeePolicyVersionSnapshot` (nullable; set when a non-zero fee policy is in effect, D18); snapshot invariants (later catalog edits never mutate lines — persistence test); chosen prices validated by `TicketPricingRules` against the **pinned** catalog version's bounds, never against the current catalog (Hi.Events lesson: order lines don't pin a revision — report §4.5); explicit rounding at line-subtotal computation.
- **Acceptance Criteria:**
  - [ ] Price-revision test: new catalog version leaves existing lines byte-identical
  - [ ] Chosen price below the pinned minimum is rejected server-side regardless of client payload; 0-amount donation/PWYC line accepted when the pinned minimum is 0
- **Dependencies:** 5.1
- **Effort:** M

#### Task 5.3: Atomic hold reservation + expiry sweeper
- **Type:** create
- **Layer:** Persistence + Application + API background service
- **Files:** new `RegistrationInventoryHold.cs` + configuration; new repository methods with explicit locking (`IRegistrationInventoryRepository`); new `Features/RegistrationOrders/Handlers/Commands/CreateOrderWithHoldCommandHandler.cs`; new `src/Explore.API/BackgroundServices/InventoryHoldExpiryWorker.cs`
- **Description:** One transaction: validate catalog version → enforce per-order/account/verified-contact limits → check type+pool capacity under row locks → create order+lines → reserve → persist holds (per §20.2, via `IUnitOfWork`; IDs precomputed; zero external IO). **Every affected `EventCapacityPool` row is locked in deterministic order and active holds are recounted inside the transaction** — the Hi.Events shared-capacity race (validation outside its event advisory lock, report §7.1) is the named counter-example; a coarse event-level lock is explicitly rejected. Hold policies per D11; reservation-before-PII sequencing (report §9.1.1): the hold exists before any buyer/participant data is collected. Worker releases expired holds with fenced claims and fresh scopes per item (webhook-worker pattern); expired/released/consumed transitions are conditional and idempotent; an order whose hold expired before finalization gets a defined recovery path (re-reserve or waitlist), never silent oversell.
- **Acceptance Criteria:**
  - [ ] Real-PostgreSQL race test: two concurrent orders for **different ticket types sharing one pool** cannot jointly exceed the pool (report §11.2 holds criteria — not mock-based)
  - [ ] Expired hold returns capacity (worker test); waitlist-when-full path creates `Waitlisted` order; expiry-vs-finalization overlap resolves via the defined recovery path
- **Dependencies:** 5.2
- **Effort:** XL

#### Task 5.4: Guest order flow (PublicTransactional endpoints)
- **Type:** create
- **Layer:** API + Application
- **Files:** new `RegistrationOrderController.cs` guest actions (`start-guest-registration`, get/continue/amend/cancel by capability token), consuming Phase 3 primitives
- **Description:** `PublicTransactional` classification; identity-access-mode enforcement (`ACCOUNT_REQUIRED` rejects anonymous start; `GUEST_ALLOWED`/`CAPABILITY_TOKEN_ALLOWED` accept per config); token issued once on order creation; email management link when email supplied; name-only path shows booking reference + loss warning; idempotency-key required on create/finalize; no account auto-creation. The Hi.Events public-order exposure defect (completed orders loadable by short ID without session verification, report §7.8/§7.9) is the named counter-example: display/public identifiers never authorize anything, and every guest lookup verifies the full tenant/event/order tuple.
- **Prerequisite gate:** Before the first Phase 5 `PublicTransactional` endpoint, replace the generic `IdempotencyMiddleware` `FindAsync` → execute → `SaveAsync` window with an atomic in-progress key claim or business-transaction-owned dedupe. Concurrent identical keys must not execute twice, and required claim-persistence failures must fail closed. This gate does not require a migration design now.
- **Acceptance Criteria:**
  - [ ] §31.3 matrix covered in API tests (anonymous rejected on account-required; token scoped to its order; guessed ID → generic 404; expired token fails safely; no silent account)
  - [ ] Display/public order identifiers grant zero access on every endpoint (explicit test); capability rotation invalidates the prior token; capability values never appear in logs (log-assertion test)
  - [ ] Atomic in-progress key claim or business-transaction-owned dedupe is implemented before endpoint exposure; concurrent identical keys execute once, and required claim-persistence failure fails closed
- **Dependencies:** 5.3, Phase 3, atomic idempotency prerequisite above
- **Effort:** L

#### Task 5.5: Authenticated order flow + finalization + outbox events
- **Type:** create
- **Layer:** Application + API
- **Files:** new commands (submit, finalize-free, cancel), HAL policy `RegistrationOrderLinkPolicy.cs` (new), `RouteNames`/`LinkRelations` additions
- **Description:** Finalization transaction: re-validate requirements placeholder (until Phase 8), consume holds via **conditional state transition** (`WHERE status = ...` / concurrency token — the Hi.Events duplicate-completion race, report §7.2, is the named counter-example), apply admission mode (`RegistrationMode`: open/approval/invite/closed) + waitlist, materialize `EventRegistration` rows from entitlements, write outbox events (`registration.confirmed` etc. via existing webhook ledger) — notifications strictly post-commit. Cancellation/release effects derive from **order lines and holds, never from participant/registration rows** (Hi.Events general-product inventory bug, report §7.6).
- **Acceptance Criteria:**
  - [ ] Duplicate finalize (idempotency) returns the original result; a concurrent second completion cannot create additional registrations, answers, or outbox rows (conditional-transition test)
  - [ ] Rollback leaves no partial rows; outbox row written in-transaction, delivered post-commit; cancellation releases every line's inventory including zero-participant lines
- **Dependencies:** 5.3
- **Effort:** L

#### Task 5.6: Rewire `EventRegistration` + delete `EventRegistrationIntent`
- **Type:** modify/delete
- **Layer:** Domain + Persistence + Application
- **Files:** `EventRegistration.cs` (existing — `RegistrationParticipantId` required FK once Phase 6 lands; interim: `RegistrationOrderId` + nullable participant), `EventRegistrationIntent.cs` (delete), `EventContactShareConsent.cs` (FK → `SourceRegistrationOrderId`), `Features/EventRegistrations/**` (queries rewritten order-centric; commands deleted), seeder/scope/policy lookups retained
- **Description:** Delete the intent aggregate and its handlers/controller routes; organizer registration views become order/participant queries; uniqueness moves to assignment-level; `RegistrationScope`/`EventRegistrationPolicy` remain as workflow vocabulary consumed by entitlements.
- **Acceptance Criteria:**
  - [ ] `rg "EventRegistrationIntent" src/ tests/ --glob '!src/Explore.Persistence/Migrations/**'` → zero runtime/current-model hits; immutable historical migration files remain untouched
  - [ ] Organizer list views function against orders
- **Dependencies:** 5.5
- **Effort:** L

#### Task 5.7: Order Cerbos/HAL + actor-level Studio context
- **Type:** create/modify
- **Layer:** API + Cerbos
- **Files:** evolve `cerbos/policies/islamuevent_event_registration.yaml` → order semantics or new `islamuevent_registration_order.yaml` + schema; HAL relations `start-registration`, `start-guest-registration`, `sign-in-to-register`, `view-registration-orders`; new `StudioContextDto`, Application query/handler, `StudioController.GetContext`, resource assembler/link policy
- **Description:** Server-side authorization on order visibility (buyer, linked account, organizer permission-gated), attendee surfaces hidden for external-managed events; parity tests. Add authenticated `GET /api/studio/context?actorId={optionalActorHint}` with `PrivateNoStore`: validate the optional actor through existing managed-actor authority, support the personal fallback, and return one HAL resource for actor-level operational navigation. Base Events remains existing UI; emit `view-registration-orders` only when the selected actor can operate at least one platform-managed event. Phase 6 extends the same resource with Attendees without changing the shell contract.
- **Acceptance Criteria:**
  - [ ] Organizer of external-managed event receives no order/attendee-management links (FR-PRIV-05)
  - [ ] Unauthorized actor hints fail closed; `StudioContextDto` contains no role booleans or tenant-wide event data
- **Dependencies:** 5.5
- **Effort:** M

#### Task 5.8: Blazor checkout + Studio order management UX
- **Type:** create/modify
- **Layer:** Blazor
- **Files:** attendee flow: replace `Pages/Events/Components/EventRegistration.razor` with `Pages/Registration/{TicketSelection,OrderDetails,OrderConfirmation,GuestOrderAccess}.razor`; organizer flow: new `Pages/Studio/StudioOrders.razor`, `StudioEventOrders.razor`, scoped `IStudioContextService`; modify `Routes.razor`, `StudioWorkspaceNavigation.razor`, `StudioEventNavigation.razor`; update `EventListRegistrationWorkflow.cs`
- **Description:** Checkout is structured around the **order state machine, not a linear form** (Hi.Events UX lesson, report §6.2): distinct surfaces/screens for reservation created / expiring / expired, details incomplete, ready to finalize, awaiting payment (boundary placeholder), completed, abandoned, cancelled, and recovery-required; hold expiry countdown with navigation-away warning and explicit abandon action; guest vs sign-in branch per identity-access mode. Pricing widgets follow D17/D18. Add actor `/studio/orders` and event `/studio/events/{eventId}/orders` views. Actor navigation reads only `StudioContextDto._links`; event navigation reads only the shared event `_links`. Both list/detail actions remain order-resource HAL-gated.
- **Acceptance Criteria:**
  - [ ] bUnit: attendee and Studio affordances by `_links` only; actor/event Orders sidebar links disappear when their source relation is absent; hold expiry visible; free order reaches confirmed state; recovery screens render per status
  - [ ] bUnit: sliding-scale sliders stay linked and honor the minimum; donation/PWYC input accepts 0 only when minimum is 0; contribution dropdown shows "percentage — computed amount" pairs from DTO data with 0 preselected
- **Dependencies:** 5.4, 5.5, 5.7, 5.10 (+ contract regeneration)
- **Effort:** XL

#### Task 5.9: AT Proto + notification dependents sweep
- **Type:** investigate/modify
- **Layer:** Application
- **Files:** `rg "EventRegistration" src/` dependents outside replaced features (AtprotoRecordId usage, email templates, notification handlers)
- **Description:** Bounded sweep rewiring every dependent to order/participant semantics; decision recorded for AT Proto (keep `AtprotoRecordId` nullable on `EventRegistration`, defer order federation).
- **Acceptance Criteria:**
  - [ ] Build green with zero references to deleted members; decisions in context
- **Dependencies:** 5.6
- **Effort:** M

#### Task 5.10: Platform-contribution order component + organizer-earnings transparency
- **Type:** create
- **Layer:** Domain + Application + API
- **Files:** new `src/Explore.Domain/RegistrationOrderPlatformContribution.cs` + configuration; new `src/Explore.Application/Contracts/Services/IOrganizerEarningsCalculator.cs` + `Services/Registration/OrganizerEarningsCalculator.cs` (pure, decimal-exact); order-totals composition in `Features/RegistrationOrders/**`; checkout-composition DTO additions
- **Description:** Per D18. When the instance's `PlatformContributionSetting` is enabled, checkout composition exposes the DB-stored heading/body plus the dropdown options — each option carries the percentage and the **server-computed** currency amount (percentage × organizer-directed order total), with `0` preselected. The buyer's selection persists as `RegistrationOrderPlatformContribution` (percentage, computed amount, setting-version snapshot). Contribution money is instance-operator-directed and is excluded from organizer totals, organizer earnings, capacity semantics, and organizer-facing exports everywhere. `IOrganizerEarningsCalculator` computes "Organizer earns" = organizer-directed line totals − platform fee (D18 policy snapshot), powering the sliding-scale slider and organizer price previews, with an "excluding payment-processing fees" disclaimer until the payment workstream defines processor costs. Totals rule: any positive total (lines + contribution) → `AwaitingPayment`; all-zero → free confirmation path; the contribution control is composed only when a payment path will exist for the order (rule enforced at composition, so free-only instances without payments never dead-end a buyer).
- **Acceptance Criteria:**
  - [ ] Contribution absent by default and entirely hidden when the instance setting is disabled; selection of 0 stores no contribution row
  - [ ] Dropdown amounts computed server-side and delivered via DTO — the client performs no authoritative money math
  - [ ] Organizer-earnings figure equals line totals minus fee policy exactly (decimal + rounding tests); contribution never leaks into organizer earnings, organizer totals, or organizer exports
- **Dependencies:** 4.5, 5.2
- **Effort:** L

---

### Phase 6: Participants, Group Bookings, And Data-Collection Modes
- **Goal:** Buyer ≠ participant: families, companies, deferred assignment, per-ticket data modes, per-booking-party limits, group amendments.
- **Depends on:** Phase 5.
- **Relevant files:** new — `RegistrationParticipant.cs`, `RegistrationParticipantPii.cs`, `RegistrationTicketAssignment.cs`, `ParticipantType` + `AssignmentStatus` lookups (+ enums), features/UI per task; existing — order features, `EventRegistration.cs` (participant FK becomes required).
- **Related skills/rules:** as Phase 5.
- **Acceptance criteria:** FR-TICKET-04/05/06/07/10; §17 family + company configurations; §31.5 participant rows (child-required/adult-optional, deferred company assignment, per-booking-party limit).
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Additive on the Phase 5 aggregate; assignment-uniqueness index changes are the risky part — verify in the persistence suite when it next runs (Phase 9) and record as pending evidence.

#### Task 6.1: Participant + PII + assignment domain model
- **Type:** create
- **Layer:** Domain
- **Files:** new entities/lookups above; extend `RegistrationOrderRules.cs`
- **Description:** Participant types `Adult/Child/Dependent/Employee/Guest/Unnamed`; guardian self-reference; PII split entity; assignment (`OrderLineId`, `ParticipantId?`, `Ordinal`, status incl. deferred); rules: `PER_TICKET_REQUIRED` blocks confirmation until assigned, `DEFERRED_ASSIGNMENT` sets deadline, guardian required by ticket eligibility.
- **Acceptance Criteria:**
  - [ ] Rules unit tests for all five `ParticipantDataCollectionMode` values
- **Dependencies:** Phase 5
- **Effort:** L

#### Task 6.2: Persistence + `EventRegistration` participant linkage
- **Type:** create/modify
- **Layer:** Persistence
- **Files:** new configurations/repositories; `EventRegistration.cs` — `RegistrationParticipantId` required, `LinkedUserId` denormalized nullable; assignment-based uniqueness (participant × session)
- **Description addition (Hi.Events §7.7 lesson):** every `RegistrationTicketAssignment` references a **concrete order line**, and a DB constraint prevents assignments from exceeding that line's quantity — the exact per-line multiset is enforced, not just the total count (Hi.Events validates only the total, allowing tier-mismatch).
- **Acceptance Criteria:**
  - [ ] Unique index prevents double admission of one participant to one session while allowing multiple unnamed tickets pre-assignment
  - [ ] DB-level test: assignments per order line cannot exceed the line quantity; a tier-A/tier-B order cannot carry two tier-A assignments
- **Dependencies:** 6.1
- **Effort:** M

#### Task 6.3: Group booking commands + limits
- **Type:** create
- **Layer:** Application
- **Files:** new participant/assignment commands (add/update/assign/bulk-assign/defer), limit enforcement extension in hold/finalize handlers (`MaximumQuantityPerBookingParty` via purchaser actor)
- **Description:** Company bulk assignment (CSV import deferred to Phase 14 — command accepts collection payload now); anonymous-enforcement honesty: per-account/verified-contact limits only when identity exists; organizer UI copy required by 6.5.
- **Acceptance Criteria:**
  - [ ] Family scenario (2×Adult+3×Child, child PII required, adult optional) passes end-to-end handler test
- **Dependencies:** 6.2
- **Effort:** L

#### Task 6.4: API + HAL for participants/assignments
- **Type:** create
- **Layer:** API
- **Files:** participant/assignment controller actions on the order surface (authenticated + capability-token variants), event and `StudioContextDto` `view-participants` organizer relations; contract regeneration
- **Acceptance Criteria:**
  - [ ] Capability-token guest can manage only own order's participants; organizer sees participants only with permission-gated link
- **Dependencies:** 6.3
- **Effort:** M

#### Task 6.5: Blazor group-booking + Studio attendee UX
- **Type:** create/modify
- **Layer:** Blazor
- **Files:** extend checkout pages with participant editors per data-mode; new `Pages/Studio/StudioAttendees.razor`, `StudioEventAttendees.razor`; modify `Routes.razor`, `StudioWorkspaceNavigation.razor`, `StudioEventNavigation.razor`; deferred-assignment reminder surfaces
- **Description:** Per-ticket participant forms rendered by mode; buyer-to-participant **copy controls** ("copy my details to first / to all participants" — Hi.Events checkout lesson, report §9.1.9) with per-participant override; "Hard per-person limits require an account or a verified contact method" organizer hint (§19.5); accessible repeatable form groups. Add actor `/studio/attendees` and event `/studio/events/{eventId}/attendees`; each sidebar link requires `view-participants`, and rows/actions use participant/order resource links.
- **Acceptance Criteria:**
  - [ ] bUnit: child ticket requires participant fields; unnamed employee tickets confirm with deferred deadline visible; copy-buyer-details remains editable; Attendees actor/event links and operations disappear without `view-participants`
- **Dependencies:** 6.4
- **Effort:** L

---

### Phase 7: Registration Form Authoring Core
- **Goal:** The canonical form system: workflows, requirements, immutable versions, typed fields/options, bounded conditions, JSON Schema artifacts.
- **Depends on:** Phase 5 (orders exist to attach workflows); Phase 6 recommended (subject types).
- **Relevant files:** new Domain — `RegistrationWorkflow.cs`, `RegistrationRequirement.cs` (with `Criticality`, `CanSkip`, `CompletionEffect`, `AnswerSyncMode`, `AppliesToSubjectType` per Report 2 §10.4), `RegistrationForm.cs`, `RegistrationFormVersion.cs` (`Version/Status/SchemaHash/PublishedAt/RetiredAt/SourceTemplate*/ConcurrencyStamp`), `RegistrationFormSection.cs`, `RegistrationFormField.cs`, `RegistrationFormFieldOption.cs`, `RegistrationFormRule.cs`, lookups: `RegistrationFieldType` (17 portable types + `OpaqueExternal`), `RequirementCriticality`, `RequirementCompletionEffect`, `AnswerSyncMode`, `AnswerSubjectType`, `DataClassification`, field governance fields (D12/§18 flags incl. `RetentionPolicyId`, `OrganizerVisibility`, `RequiresExplicitConsent`, `IsProviderTransferAllowed`); shared validation value objects extracted per D1; Application — `Features/RegistrationForms/**` authoring commands/queries + `Services/Registration/FormSchemaArtifactGenerator.cs`; API — form authoring controllers + HAL (`manage-registration-workflow`); Cerbos `islamuevent_registration_form.yaml`; Blazor — form builder.
- **Related skills/rules:** all path rules; documentation style for `docs/DOMAIN.md` new section.
- **Acceptance criteria:** §23 canonical-form matrix (immutable versions, all types, multivalue ordering, option retirement, conditional requiredness, schema-hash stability); bounded condition language only (`equals/notEquals/in/contains/exists/compare/all/any/not`) — no scripts; composite decomposition (name/address/matrix/ranking) documented in field-type registry.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` *(repeat justified: version immutability, condition evaluation, and schema-hash stability are pure domain logic)*
- **Rollback / failure handling:** Self-contained authoring plane; runtime (Phase 8) not yet wired. Schema-hash algorithm is frozen on first publish — treat the hash serializer as contract from day one (test pins a golden hash).

#### Task 7.1: Workflow + requirement + channel skeleton
- **Type:** create
- **Layer:** Domain + Persistence
- **Files:** `RegistrationWorkflow.cs`, `RegistrationRequirement.cs`, `RegistrationChannel.cs` (channel references binding — nullable provider fields until Phase 9; Native channel kind works standalone), lookups + configurations + seeder
- **Description:** Workflow per event/purpose; requirement completion semantics (`ALL` mandatory at workflow, `ANY` across channels, optional/informational/post-registration never block per FR-SYNC-03); applicability (`AllOrders/SpecificTicketType/EveryParticipant/LeadBookerOnly/ChildParticipants/SpecificSessionSelection`) as typed rules.
- **Acceptance Criteria:**
  - [ ] Requirement evaluation rules unit-tested incl. skip recording (`SkippedByRegistrant`)
- **Dependencies:** Phase 5
- **Effort:** L

#### Task 7.2: Form/version/section/field/option model with immutability
- **Type:** create
- **Layer:** Domain + Persistence
- **Files:** listed above + configurations; `Services/Registration/FormVersionRules.cs` (new)
- **Description:** Published versions immutable (domain + concurrency enforcement); edit → new draft; field dual identity (immutable version-field ID + stable `Namespace/Key` machine identity, e.g., `platform.registration/email`); option stability; field governance flags per §18; validation constraint fields mirroring the custom-property vocabulary via shared value objects.
- **Acceptance Criteria:**
  - [ ] Immutability + provenance tests (template fields recorded, never silently synced)
  - [ ] Provider question IDs impossible to store as canonical identity (no such column; mapping-only by construction)
- **Dependencies:** 7.1
- **Effort:** XL

#### Task 7.3: Bounded condition language
- **Type:** create
- **Layer:** Domain
- **Files:** `RegistrationFormRule.cs`, `Services/Registration/FormConditionEvaluator.cs` (new)
- **Description:** Typed condition AST (the ten operators only), referencing earlier answers in the same version; visibility + requiredness effects only; explicit test that the evaluator surface cannot mutate state or perform IO (pure function over answer snapshot).
- **Acceptance Criteria:**
  - [ ] Evaluator property tests; forbidden-construct list asserted (no arbitrary expressions representable)
- **Dependencies:** 7.2
- **Effort:** M

#### Task 7.4: JSON Schema 2020-12 artifact generation
- **Type:** create
- **Layer:** Application
- **Files:** new `Contracts/Services/IFormSchemaArtifactGenerator.cs` + `Services/Registration/FormSchemaArtifactGenerator.cs` (Application service, no IO), version fields `SchemaHash` + stored artifacts
- **Description:** Deterministic generation of data/UI/logic/mapping artifacts; content hash pinned at publish; golden-file tests for hash stability across runs and machines (culture-invariant serialization).
- **Acceptance Criteria:**
  - [ ] Same version → identical hash on repeat generation; hash changes on any field mutation
- **Dependencies:** 7.2, 7.3
- **Effort:** M

#### Task 7.5: Authoring Application + API + Cerbos
- **Type:** create
- **Layer:** Application + API
- **Files:** `Features/RegistrationForms/**` (workflow/requirement/form/version/field/option/rule commands + queries, validators), controllers, event HAL (`manage-registration-workflow`), Cerbos policy + parity
- **Description:** Verified-organizer-only authoring (contributor forbidden test); publish preflight (fields valid, options complete, conditions resolvable, consent fields carry purpose + text version); contract regeneration + changelog.
- **Acceptance Criteria:**
  - [ ] Publish rejects unresolvable conditions and consent fields without purpose codes
- **Dependencies:** 7.4
- **Effort:** L

#### Task 7.6: Studio form builder
- **Type:** create
- **Layer:** Blazor
- **Files:** new `Pages/Studio/RegistrationForms/**` (builder shell, section list, field editor per type, option editor, condition editor, version timeline); modify `Routes.razor`, `StudioEventNavigation.razor`
- **Description:** Add `/studio/events/{eventId}/forms`. The sidebar and builder require `manage-registration-workflow`; version states are visible; publish flow shows preflight results; drag-ordering has a keyboard fallback. Reuse `StudioEventContextState`; do not create a second management shell.
- **Acceptance Criteria:**
  - [ ] bUnit: Forms sidebar link absent without `manage-registration-workflow`; published version read-only; new-version flow works against mocked client
- **Dependencies:** 7.5
- **Effort:** XL

#### Task 7.7: Requirement attachment to participation + walk-in standalone questionnaires
- **Type:** create/modify
- **Layer:** Application + API
- **Files:** workflow attach/detach commands; participation-config validation extension (walk-in/info-only may attach only `NoRegistrationEffect` requirements per FR-SYNC-06)
- **Acceptance Criteria:**
  - [ ] Walk-in event exposes `optional-questionnaire` action without any order/registration creation
- **Dependencies:** 7.5, Phase 2
- **Effort:** M

#### Task 7.8: Form localization strategy
- **Type:** investigate + modify
- **Layer:** Domain/Application
- **Files:** `docs/LOCALIZATION.md` read; field/section label storage decision (single-language per version vs per-language columns/table)
- **Description:** Bounded investigation; implement the chosen minimal model (recommended: per-version `LanguageTag` + optional translation table deferred); record decision.
- **Acceptance Criteria:**
  - [ ] Decision recorded; RTL rendering unaffected; `MULTILINGUAL` capability honestly reported as absent until implemented
- **Dependencies:** 7.2
- **Effort:** M

---

### Phase 8: Native Collection Runtime
- **Goal:** The reference implementation every provider is tested against: attempts, submissions, typed multi-subject answers, normalization/validation pipeline, consent evidence, requirement fulfillment, idempotent finalization, native Blazor renderer.
- **Depends on:** Phases 6, 7.
- **Relevant files:** new Domain — `RegistrationAttempt.cs` (pins intent/requirement/channel/form-version/binding/mapping-revision + `AttemptTokenHash`, expiry, supersession), `RegistrationSubmission.cs` (per §5 fields incl. `PayloadHash`, verification/normalization/validation statuses, `TrustLevel`, `SupersedesSubmissionId`), `RegistrationSubmissionRevision.cs`, `RegistrationAnswer.cs` (D5 shape), `RegistrationSensitiveAnswerValue.cs`, `RegistrationAnswerFile.cs`, `RegistrationSubmissionIssue.cs`, `RegistrationRequirementFulfillment.cs`, `RegistrationFinalizationEffect.cs`, `RegistrationConsentRecord.cs`, status lookups per D16; `Services/Registration/AnswerNormalizationRules.cs`; Application — `Features/RegistrationSubmissions/**` pipeline handlers; API — native submission endpoints (authenticated + PublicTransactional guest variant); Blazor — native form renderer.
- **Related skills/rules:** all path rules; `outbox-pattern`.
- **Acceptance criteria:** §7 pipeline implemented (decode→normalize→parse→field constraints→cross-field rules→persist→safe output); §23 registration-correctness rows for the native path (concurrent attempts, duplicate finalization effect, rollback, outbox-after-commit); consent evidence immutable; optional requirement skip works; answers never appear in logs/metrics/ProblemDetails.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` *(repeat justified: the normalization/validation pipeline handlers are Application-owned and this is the fastest deterministic coverage)*
- **Rollback / failure handling:** Native runtime is additive; finalization extends the Phase 5 transaction. If the answer CHECK constraints fight EF value conversions, resolve at configuration level (owned columns), never by weakening constraints; record in context.

#### Task 8.1: Attempt + submission + status machines
- **Type:** create
- **Layer:** Domain + Persistence
- **Files:** entities above + configurations; uniqueness `(ProviderBindingId, ProviderResponseId, ProviderResponseRevision)` prepared (nullable binding for native); attempt-token hash single-use
- **Acceptance Criteria:**
  - [ ] Duplicate submission insert → acknowledged no-op (unique index test)
  - [ ] Attempt supersession rules unit-tested (late superseded evidence retained, cannot finalize)
  - [ ] Answer identity uniqueness constrained at DB level (one answer row set per submission/field/subject/ordinal — Hi.Events lacks this, report §4.7)
- **Dependencies:** Phase 7
- **Effort:** L

#### Task 8.2: Typed answer storage + CHECK constraints + subjects
- **Type:** create
- **Layer:** Domain + Persistence
- **Files:** `RegistrationAnswer.cs`, `RegistrationSensitiveAnswerValue.cs` + configurations with raw-SQL check constraints (`num_nonnulls(...) = 1` + type agreement)
- **Description:** Subject typing per §18 Report 2 (`RegistrationOrder/Purchaser/Participant/TicketAssignment/SessionSelection`); multivalue `Ordinal`; option FK to version options; subject-shape checks (an order-scoped field cannot carry a participant subject and vice versa — Hi.Events leaves this unenforced, report §4.7).
- **Acceptance Criteria:**
  - [ ] DB-level test: two value columns populated → constraint violation; wrong-type column for declared field type → violation
  - [ ] Subject-shape constraint test: answer subject type must match the field's declared applicability
- **Dependencies:** 8.1
- **Effort:** L

#### Task 8.3: Normalization + validation pipeline
- **Type:** create
- **Layer:** Application (+ Domain value objects)
- **Files:** new `Features/RegistrationSubmissions/Handlers/Commands/{SubmitNativeFormCommandHandler,ValidateSubmissionService}.cs`; Domain normalizers (NFC, E.164 phone, email dual-value, ISO country, BCP-47, URL scheme allowlist, decimal/date/instant parsing)
- **Description:** Per §7: reject don't coerce; no HTML in text; output-context encoding left to renderers; cross-field rules via Phase 7 evaluator; issues recorded as `RegistrationSubmissionIssue` rows; sensitive classifications routed to encrypted store (Data Protection stack, key version recorded — investigation folded here).
- **Acceptance Criteria:**
  - [ ] Type matrix unit tests (all 17 portable types, valid + invalid + boundary)
  - [ ] Sensitive answer round-trips encrypted; plaintext absent from DB row (integration-style test via persistence suite when next run)
- **Dependencies:** 8.2
- **Effort:** XL

#### Task 8.4: Consent evidence records
- **Type:** create
- **Layer:** Domain + Application
- **Files:** `RegistrationConsentRecord.cs` + configuration; consent-field handling in the pipeline (purpose, exact text snapshot, versions, language, subject reference)
- **Acceptance Criteria:**
  - [ ] Consent answer produces immutable evidence row; withdrawal timestamps supported; Boolean-only consent impossible for consent-typed fields
- **Dependencies:** 8.3
- **Effort:** M

#### Task 8.5: Requirement fulfillment + idempotent finalization effect
- **Type:** create
- **Layer:** Application
- **Files:** `RegistrationRequirementFulfillment.cs`, `RegistrationFinalizationEffect.cs` + handlers extending Phase 5 finalization (all-mandatory-fulfilled gate before `ReadyForCheckout`)
- **Description:** Fulfillment recorded per intent/requirement with source submission; finalization effect is durable + fenced (webhook-outbox pattern) so provider and native paths share one finalizer; canonical flow §10 enforced end-to-end.
- **Acceptance Criteria:**
  - [ ] Duplicate finalization effect executes once (fencing test); optional requirements never block (FR-SYNC-03)
- **Dependencies:** 8.3, Phase 5
- **Effort:** L

#### Task 8.6: Native submission API surface
- **Type:** create
- **Layer:** API
- **Files:** attempt-launch + submit endpoints on the order surface (authenticated; guest variant `PublicTransactional` with capability token); HAL `optional-questionnaire`, requirement-progress relations; contract regeneration
- **Acceptance Criteria:**
  - [ ] Answers absent from ProblemDetails on validation failure (issue codes + field keys only)
- **Dependencies:** 8.5
- **Effort:** M

#### Task 8.7: Native Blazor form renderer
- **Type:** create
- **Layer:** Blazor
- **Files:** new `Components/Registration/FormRenderer/**` (renderer shell + one component per field type + condition-driven visibility + skip control + consent blocks + progress)
- **Description:** Renders pinned form version from DTO; client-side hints only (server validation authoritative); optional requirements show "Optional" + "Skip and continue"; keyboard-complete; RTL; announced processing status.
- **Acceptance Criteria:**
  - [ ] bUnit per-type render + condition toggle tests; skip flow recorded without error styling
- **Dependencies:** 8.6
- **Effort:** XL

#### Task 8.8: File answers (gated)
- **Type:** investigate + create
- **Layer:** Domain + Infrastructure
- **Files:** `RegistrationAnswerFile.cs` (metadata, quarantine state, scan status, storage reference to `StorageObject`); upload path investigation (existing storage endpoints, MIME sniffing, size limits; malware scanning availability)
- **Description:** If no scanner exists, ship quarantine-by-default (files never exposed until manually released) and record scanner integration as deferred work — File field type remains publishable only when the deployment enables the file pipeline.
- **Acceptance Criteria:**
  - [ ] Quarantined file inaccessible via any read endpoint; decision + deferral recorded
- **Dependencies:** 8.3
- **Effort:** L

---

### Phase 9: Provider Framework (Connections, Bindings, Capabilities, Callbacks, Reconciliation)
- **Goal:** Everything provider-agnostic: connections with secret bindings, bindings with mapping/drift state, capability tuples, sync modes, callback intake extension, reconciliation, health — no concrete external provider yet.
- **Depends on:** Phase 8.
- **Relevant files:** new Domain — `RegistrationProviderConnection.cs`, `RegistrationProviderBinding.cs` (per §5 binding fields incl. presentation/collection/completion/sync/trust + publication/synchronization/health state), `RegistrationProviderCapability.cs`, `RegistrationProviderFieldMapping.cs`, `RegistrationProviderOptionMapping.cs`, `RegistrationProviderSchemaRevision.cs`, lookups (`RegistrationProviderKind`, `SchemaAuthority`, `PresentationMode`, `CollectionMode`, `CompletionMode`, `RegistrationTrustLevel`, `SchemaDriftClass`); Application — capability-segregated contracts (D3) in `Contracts/Services/Registration/`, `Features/RegistrationProviders/**`, launch-descriptor service, drift classifier, reconciliation commands; API — `POST /api/integrations/registration/{provider}/{bindingId}/callback` family riding the incoming-webhook intake, binding-management endpoints, health read models; Infrastructure — provider registry + Null/Native descriptors; Secrets — new `SecretDefinitionRegistry` entries; Blazor — connection/binding management UI; CSP `frame-src` allowlist plumbing for embeds.
- **Related skills/rules:** `outbox-pattern`, `auth-patterns`, webhook rules by analogy; `docs/WEBHOOKS.md`.
- **Acceptance criteria:** §23 provider-conformance + callback-reliability matrices for the framework level (dedup, out-of-order, stale timestamp, unknown tuple fails closed); sync modes enforced (`NONE` stores nothing, `COMPLETION_ONLY` stores no answers — FR-SYNC-04/05); drift classes per §17 with fail-closed behavior; embeds only from connection-approved domains, server-generated iframes, accessible titles, new-tab fallback.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` *(repeat justified: callback intake endpoints, duplicate acknowledgment, and binding-resolution behavior are API-level contracts)*
- **Rollback / failure handling:** Callback endpoints fail closed to 2xx-acknowledge-and-park (`NeedsReconciliation`) rather than 5xx loops; framework ships behind absent-provider reality (no adapter yet), so nothing is user-visible until Phase 10.

#### Task 9.1: Provider configuration domain model + secret definitions
- **Type:** create
- **Layer:** Domain + Persistence + Secrets
- **Files:** entities/lookups above + configurations + seeder; `src/Explore.Domain/Secrets/SecretDefinitionRegistry.cs` (existing — add registration provider secret definitions, tenant scope)
- **Acceptance Criteria:**
  - [ ] Credentials representable only as secret-binding references (no secret columns); `Explore.Secrets.UnitTests` addition for new definitions
- **Dependencies:** Phase 8
- **Effort:** L

#### Task 9.2: Capability contracts + registry + effective-capability resolution
- **Type:** create
- **Layer:** Application + Infrastructure
- **Files:** the ten D3 interfaces; `RegistrationProviderRegistry` (Infrastructure); effective-capability resolver (proven ∩ configured ∩ governance ∩ mapping ∩ authorization); capability tuple entity wiring
- **Acceptance Criteria:**
  - [ ] Unknown tuple → automatic finalization refused, redirect/manual channels still offered (fail-closed test)
- **Dependencies:** 9.1
- **Effort:** L

#### Task 9.3: Field/option mapping + schema revision + drift classifier
- **Type:** create
- **Layer:** Domain + Application
- **Files:** mapping entities; `Services/Registration/SchemaDriftClassifier.cs` (new, pure); mapping-revision pinning on attempts (Phase 8 fields already present)
- **Description:** Drift classes `NoDrift/AdditiveOptionalChange/LabelOnlyChange/MappingRequired/RequiredFieldRemoved/TypeChanged/OptionSetChanged/UnsupportedChange` with §17 behaviors; mappings never silently rewritten after submissions exist (guard + test).
- **Acceptance Criteria:**
  - [ ] Classifier unit-tested per class; fail-closed classes block binding publication
- **Dependencies:** 9.1
- **Effort:** L

#### Task 9.4: Callback intake extension + registration effect worker
- **Type:** create
- **Layer:** API + Application
- **Files:** new `src/Explore.API/Controllers/RegistrationProviderCallbackController.cs` (bounded-bytes read, binding resolution without tenant disclosure, provider-proof verification hook, insert-or-acknowledge `IncomingWebhookMessage`, one unique registration effect, prompt return); new `Features/RegistrationSubmissions/Handlers/Commands/ProcessProviderSubmissionEffectHandler.cs` worker path (fenced claim → re-verify → fetch where supported → normalize → validate → fulfill → finalize via Phase 8 effect)
- **Acceptance Criteria:**
  - [ ] Controller provably never touches order/registration aggregates (architecture test on namespace references)
  - [ ] Duplicate callback acknowledged; callback-before-user-return and user-return-before-callback orderings both converge (handler tests)
- **Dependencies:** 9.2, 9.3
- **Effort:** XL

#### Task 9.5: Sync-mode enforcement + trust-level finalization policy
- **Type:** create
- **Layer:** Application
- **Files:** pipeline extensions: `NONE` (no storage, no fulfillment), `COMPLETION_ONLY` (evidence only, fulfillment iff verified+correlated+unexpired per §10.3), `SELECTED_FIELDS` (approved mappings only), `FULL_CANONICAL`, `MIRROR_ONLY` (sink path stub for Phase 10); minimum-trust-level policy gate → `NeedsReconciliation` below threshold
- **Acceptance Criteria:**
  - [ ] FR-SYNC-01…07 handler tests; completion-only stores zero `RegistrationAnswer` rows
- **Dependencies:** 9.4
- **Effort:** L

#### Task 9.6: Reconciliation + provider health
- **Type:** create
- **Layer:** Application + API
- **Files:** reconciliation commands (poll checkpoint fetch abstraction, manual import queue, `NeedsReconciliation` organizer queue); health read model per binding (connection validity, callback age, drift, reconciliation lag — bounded fields per §21); event HAL relations `manage-registration-channels` / `view-registration-provider-health`
- **Acceptance Criteria:**
  - [ ] Health surface exposes no attendee data; reconciliation queue lists parked submissions with issue codes only
- **Dependencies:** 9.4
- **Effort:** L

#### Task 9.7: Channels + embed/CSP + Studio provider UI
- **Type:** create
- **Layer:** API + Blazor
- **Files:** channel CRUD on requirements (attach binding, order, fallback); server-generated iframe descriptors from approved connection domains; CSP `frame-src` allowlist wiring (investigate current CSP source in BFF/API — bounded); new `Pages/Studio/StudioEventIntegrations.razor`; modify `Routes.razor`, `StudioEventNavigation.razor`; attendee-facing processing-status pattern with intent-status polling
- **Acceptance Criteria:**
  - [ ] Arbitrary organizer iframe HTML impossible (no such input path); non-allowlisted domain refused server-side
  - [ ] Completion never inferred from iframe navigation (UI polls order/requirement status only)
  - [ ] Integrations sidebar link is absent unless `manage-registration-channels` or `view-registration-provider-health` exists
- **Dependencies:** 9.5, 9.6
- **Effort:** XL

---

### Phase 10: Formbricks Provider (Deep Integration)
- **Goal:** First external provider at full depth, in consultation §22 Phase-3 order: BYO link/embed → signed callback → API fetch → schema import/mapping → managed provisioning → headless renderer → mirror sink → files/multilingual conformance.
- **Depends on:** Phase 9.
- **Relevant files:** new Infrastructure — `src/Explore.Infrastructure/Services/Registration/Providers/Formbricks/{FormbricksDescriptor,FormbricksCallbackVerifier,FormbricksSchemaReader,FormbricksSubmissionReader,FormbricksSubmissionWriter,FormbricksFormProvisioner,FormbricksSubscriptionManager,FormbricksReconciliationProvider,FormbricksSubmissionSink}.cs` + HTTP client wiring; docker/compose optional profile for self-hosted Formbricks (`docker-compose.yml`, `docs/SELF_HOSTING.md`); conformance-evidence fixtures under `tests/Explore.Infrastructure.Tests/Registration/Formbricks/`.
- **Related skills/rules:** `error-tracking`; webhook signature-verification patterns already in repo.
- **Acceptance criteria:** All four modes (A BYO, B managed provisioning with publish preflight §14, C headless via ISLAMU backend proxy, D mirror sink); Standard-Webhooks verification (`webhook-id/timestamp/signature`, `whsec_`, HMAC-SHA256 raw body, replay tolerance) sharing the crypto core with existing webhook code; API v1 pinned with tested deployment tuple; unknown Formbricks version fails closed.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Each mode is independently disableable via capability rows; a failing mode is withdrawn by capability, not by code rollback. All HTTP behind adapters — no live-service tests; fixtures capture recorded payloads.

#### Task 10.1: Conformance re-verification + capability profile pin
- **Type:** investigate
- **Layer:** Infrastructure
- **Files:** capability tuple seed + `tests/.../Formbricks/conformance-notes.md` (new, in-repo evidence)
- **Description:** Re-verify (against current official docs at implementation time) webhook header profile, API v1 endpoints used, headless schema/response contracts, domain-split and S3 file-upload requirements; pin the supported version tuple; record deltas from consultation citations.
- **Acceptance Criteria:**
  - [ ] Evidence file lists exact endpoints/headers with dates; capability rows match it
- **Dependencies:** Phase 9
- **Effort:** M

#### Task 10.2: Signed callback verifier + BYO link/embed (Modes A start)
- **Type:** create
- **Layer:** Infrastructure + API
- **Files:** `FormbricksCallbackVerifier.cs` (Standard Webhooks profile over shared HMAC core), binding setup for existing surveys, link/embed presentation descriptors
- **Acceptance Criteria:**
  - [ ] §23 callback rows: valid/invalid signature, stale timestamp, duplicate, out-of-order — all against recorded fixtures
- **Dependencies:** 10.1
- **Effort:** L

#### Task 10.3: Management-API response fetch + schema import/mapping (Mode A complete)
- **Type:** create
- **Layer:** Infrastructure + Application
- **Files:** `FormbricksSchemaReader.cs`, `FormbricksSubmissionReader.cs`; import → frozen `ExternalImported` ISLAMU version; fingerprint + mapping revision recorded
- **Acceptance Criteria:**
  - [ ] `responseFinished` → fetch → normalize → fulfill end-to-end handler test on fixtures; drift on re-import classified per Phase 9
- **Dependencies:** 10.2
- **Effort:** L

#### Task 10.4: Managed provisioning (Mode B) with publish preflight
- **Type:** create
- **Layer:** Infrastructure + Application
- **Files:** `FormbricksFormProvisioner.cs`, `FormbricksSubscriptionManager.cs` (webhook registration)
- **Description:** Create/update survey from canonical version; §14 preflight (required fields supported, options mapped, no unsupported mandatory condition, webhook registered, survey active, fingerprint matches); ambiguous provider acceptance never auto-retried without reconciliation (webhook-intent lesson).
- **Acceptance Criteria:**
  - [ ] Preflight failure blocks binding publication with typed reasons
- **Dependencies:** 10.3
- **Effort:** L

#### Task 10.5: Headless mode (C) through ISLAMU backend
- **Type:** create
- **Layer:** Application + Infrastructure
- **Files:** `FormbricksSubmissionWriter.cs`; native renderer submits to ISLAMU API (canonical validation/persistence first), optional post-commit Formbricks response write via outbox-driven sink call
- **Acceptance Criteria:**
  - [ ] Browser never talks to Formbricks response endpoints for registration (no such URL in client DTOs); Formbricks write failure never affects finalized order
- **Dependencies:** 10.3
- **Effort:** M

#### Task 10.6: Mirror sink (Mode D) + self-host profile + files/multilingual conformance
- **Type:** create
- **Layer:** Infrastructure + DevOps
- **Files:** `FormbricksSubmissionSink.cs` (approved-fields-only, post-commit, retried via outbox); optional compose profile + `docs/SELF_HOSTING.md` (workspace-per-tenant isolation, domain split, pinned image); capability truth for FILE_UPLOAD/MULTILINGUAL per 10.1 evidence
- **Acceptance Criteria:**
  - [ ] Sink transfers only `IsProviderTransferAllowed` fields; compose profile optional and absent from default profiles
- **Dependencies:** 10.4, 10.5
- **Effort:** L

---

### Phase 11: Microsoft Forms Provider (Connector Channel)
- **Goal:** Link/embed presentation + versioned Power Automate callback solution at `DelegatedAutomation` trust; org-account scope; Excel remains a sink/reconciliation source, never transaction authority.
- **Depends on:** Phase 9 (Phase 10 not required — parallelizable after 9).
- **Relevant files:** new Infrastructure — `Providers/Microsoft/{MicrosoftFormsDescriptor,MicrosoftFormsCallbackVerifier}.cs`; versioned flow template artifact `docs/integrations/microsoft-forms-flow-template.md` + exported solution file under `docs/integrations/` (path confirmed at implementation); manual mapping UI reuse from Phase 9; `docs/INTEGRATIONS.md`.
- **Acceptance criteria:** Callback envelope per §16 (provider code, binding, form/response IDs, attempt token, timestamp, mapped values, contract version, idempotency key) with API-key or signature verification; `TrustLevel = DelegatedAutomation` policy gating (tenant/event decides auto-finalize vs review); personal-account forms offered only as redirect/iframe/manual-reconciliation; no undocumented Microsoft APIs anywhere.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` *(repeat justified: a new provider adapter + envelope verifier lands in this project)*
- **Rollback / failure handling:** Channel disabled by capability; callbacks park to reconciliation on verification failure; setup wizard test-event verification gates activation.

#### Task 11.1: Conformance re-verification + connector contract pin
- **Type:** investigate
- **Layer:** Infrastructure
- **Files:** conformance-notes evidence file; capability tuple seed
- **Description:** Re-verify Forms connector triggers/actions and org-account restriction against current Microsoft Learn docs; version the callback envelope contract (`connector contract version` field).
- **Acceptance Criteria:**
  - [ ] Evidence file dated; personal-account limitation documented in organizer-facing copy
- **Dependencies:** Phase 9
- **Effort:** S

#### Task 11.2: Callback endpoint profile + envelope verifier
- **Type:** create
- **Layer:** Infrastructure + API
- **Files:** `MicrosoftFormsCallbackVerifier.cs`; binding-scoped API-key secret (tenant-scope secret definition); intake rides Phase 9 controller
- **Acceptance Criteria:**
  - [ ] Envelope validation matrix (missing token, stale timestamp, bad key, duplicate idempotency key) on fixtures
- **Dependencies:** 11.1
- **Effort:** M

#### Task 11.3: Versioned Power Automate solution + setup wizard + manual mapping
- **Type:** create
- **Layer:** Docs + Application + Blazor
- **Files:** flow template doc + export; setup wizard flow (create binding → download template → send test event → verify → activate); manual field-mapping UI (Phase 9 mapping surfaces) since schema read is unsupported
- **Acceptance Criteria:**
  - [ ] Test-event verification required before channel activation; mapping completeness enforced for required canonical fields
- **Dependencies:** 11.2
- **Effort:** L

#### Task 11.4: Reconciliation import (CSV/Excel) path
- **Type:** create
- **Layer:** Application
- **Files:** manual-import command accepting organizer-uploaded response export mapped through binding mappings; `ManualImport` trust level; never auto-finalizes above policy
- **Acceptance Criteria:**
  - [ ] Imported rows dedupe against callback-received responses (same response ID → no-op)
- **Dependencies:** 11.2
- **Effort:** M

---

### Phase 12: Google Forms Provider (Pub/Sub Channel)
- **Goal:** OAuth connection, form import/provision + explicit publication, field mapping, `RESPONSES` watch lifecycle with renewal, checkpointed response fetch, drift, Drive-file policy.
- **Depends on:** Phase 9 (parallelizable with 10/11 after 9).
- **Relevant files:** new Infrastructure — `Providers/Google/{GoogleFormsDescriptor,GoogleFormsSchemaReader,GoogleFormsProvisioner,GoogleFormsSubmissionReader,GoogleFormsSubscriptionManager (watch mgmt),GooglePubSubIntakeVerifier}.cs`; watch-renewal background job (extend existing background-service patterns); connection fields per §15 (OAuth secret ref, scopes, watch ID/expiry, checkpoint).
- **Acceptance criteria:** Pub/Sub notification → authenticated intake → dedupe → list-responses-after-checkpoint → dedupe response IDs → map/normalize → finalize-or-reconcile; watch renewed before 7-day expiry with failure alerts; correlation via prefilled attempt token treated as correlation-only (stronger identity or `NeedsReconciliation` per §15); API-created forms explicitly published (post-2026-06-30 behavior); no headless-submission capability advertised.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` *(repeat justified: Google adapter incl. watch lifecycle + Pub/Sub verification is new Infrastructure surface)*
- **Rollback / failure handling:** Watch loss degrades to polling reconciliation automatically (health surface flags it); OAuth failure disables channel by capability with organizer-visible health state.

#### Task 12.1: Conformance re-verification + OAuth connection model
- **Type:** investigate + create
- **Layer:** Infrastructure
- **Files:** conformance evidence; connection entity fields per §15; Google OAuth secret definitions (tenant scope)
- **Acceptance Criteria:**
  - [ ] Evidence dated; scopes minimal and listed; token refresh recorded on connection
- **Dependencies:** Phase 9
- **Effort:** M

#### Task 12.2: Form select/import/provision + explicit publication + mapping
- **Type:** create
- **Layer:** Infrastructure + Application
- **Files:** `GoogleFormsSchemaReader.cs`, `GoogleFormsProvisioner.cs`; import → frozen version; provision → explicit publish step with verification
- **Acceptance Criteria:**
  - [ ] Unpublished API-created form blocks binding activation with typed reason
- **Dependencies:** 12.1
- **Effort:** L

#### Task 12.3: Pub/Sub intake + watch lifecycle + checkpoint fetch
- **Type:** create
- **Layer:** Infrastructure + API + Application
- **Files:** `GooglePubSubIntakeVerifier.cs` (intake auth), `GoogleFormsSubscriptionManager.cs` (watch create/renew), renewal background job, `GoogleFormsSubmissionReader.cs` (list after checkpoint, dedupe)
- **Acceptance Criteria:**
  - [ ] Notification-without-data flow fetches separately (fixture test); missed-notification reconciliation sweep recovers responses; watch expiry alert emitted at bounded metric
- **Dependencies:** 12.2
- **Effort:** XL

#### Task 12.4: Correlation policy + Drive-file handling decision
- **Type:** create + investigate
- **Layer:** Application
- **Files:** prefilled-token correlation (single-use, expiring, not identity proof); below-threshold → `NeedsReconciliation`; Drive file policy (copy into ISLAMU storage + quarantine per 8.8, or capability off) — decide and implement minimal
- **Acceptance Criteria:**
  - [ ] Token-only correlation cannot auto-finalize when event policy requires authenticated respondent; decision recorded
- **Dependencies:** 12.3
- **Effort:** M

---

### Phase 13: Consent, Attendee-Data Surfaces, And Audited Exports
- **Goal:** Verified-recipient consent rule, typed consent subjects, per-participant consent independence, audited/purpose-gated exports, retention execution.
- **Depends on:** Phases 6, 8 (subjects + consent records exist); Phase 1 (verified organizer).
- **Relevant files:** existing — `EventContactShareConsent.cs` + features + Cerbos policy + `docs/CONTACT_SHARING.md`; new — consent-subject typing shared with `RegistrationConsentRecord`, export feature `Features/RegistrationExports/**`, audit events (pattern: ADR-007 durable audit trail), retention sweeps for answers/PII per field `RetentionPolicyId`.
- **Acceptance criteria:** FR-PRIV-01…06; §31.6 matrix (no prompt on unclaimed reported events, contributor never receives attendee links, purchaser consent self-only, withdrawn consent excluded, claim approval never reinterprets old consent, exports audited + tenant-scoped + field-level `IsExportable` gated).
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` *(repeat justified: consent gating and export authorization are API-surface contracts with Cerbos parity)*
- **Rollback / failure handling:** Export endpoints ship disabled-by-policy default; enable per tenant after audit evidence reviewed.

#### Task 13.1: Typed consent subjects on `EventContactShareConsent`
- **Type:** modify
- **Layer:** Domain + Persistence + Application
- **Files:** `EventContactShareConsent.cs` (subject type + subject ID replacing required `UserId`; dev-mode breaking), consent features, `docs/CONTACT_SHARING.md`
- **Acceptance Criteria:**
  - [ ] `User/RegistrationPurchaser/RegistrationParticipant/GuestContact` subjects representable; prompts only with verified `OrganizerActorId` (FR-PRIV-02, §22.1 rule tests)
- **Dependencies:** Phases 6, 8
- **Effort:** L

#### Task 13.2: Per-participant consent independence
- **Type:** create
- **Layer:** Application + Blazor
- **Files:** consent prompts per participant in checkout/requirement flow; guardian-for-child operational info per policy; child marketing disabled by default
- **Acceptance Criteria:**
  - [ ] Purchaser consent never copied to adult participants (handler test); no-retroactive-consent list (§22.4) asserted
- **Dependencies:** 13.1
- **Effort:** M

#### Task 13.3: Audited, purpose-gated exports + retention execution
- **Type:** create
- **Layer:** Application + API + Infrastructure
- **Files:** export commands (field-level `IsExportable` + purpose + active retention + consent state filters), audit event records, retention sweep job clearing expired answers/PII/consent-excluded data (respecting erasure-workstream boundaries — coordinate, don't duplicate)
- **Acceptance Criteria:**
  - [ ] Export excludes withdrawn consent and non-exportable fields; every export writes an audit row; retention sweep test clears expired answers only
- **Dependencies:** 13.1
- **Effort:** L

#### Task 13.4: Attendee-management HAL/Cerbos completion + Studio export action
- **Type:** create/modify
- **Layer:** API + Cerbos + Blazor
- **Files:** `view-participants`, `export-consented-contacts` relations finalized; Cerbos policies for export resources; existing Phase 6 Studio attendee pages/components; contributor/curator/instance-admin matrix (§23) parity and bUnit tests
- **Acceptance Criteria:**
  - [ ] §31.6 authorization rows all covered by parity tests; Studio export action appears only from `export-consented-contacts` and stays inside Attendees rather than adding another sidebar item
- **Dependencies:** 13.3
- **Effort:** M

---

### Phase 14: Advanced Orchestration, Hardening, And Deferred Integrations
- **Goal:** Close the long tail once the core is proven: guest→account linking, form templates/packs, provider migration tooling, analytics projections, bulk company import, generalized sinks, localization completion.
- **Depends on:** Phases 9–13 (any subset usable as prerequisites per task).
- **Relevant files:** per task; all new surfaces follow established patterns.
- **Acceptance criteria:** Each sub-feature lands with its own tests; nothing here blocks the platform being production-meaningful after Phase 13.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Fully additive; tasks independently deferrable.

#### Task 14.1: Guest order → account linking (explicit verification flow)
- **Type:** create — email-verified claim of a guest order into an authenticated account (§31.3); never silent. **Effort:** M
#### Task 14.2: Form templates/packs (tenant + platform blueprints with provenance)
- **Type:** create — `RegistrationFormTemplate` per §5 authoring tables; template changes never rewrite runtime versions. **Effort:** L
#### Task 14.3: Provider switching + attempt supersession tooling
- **Type:** create — organizer-facing channel switch (future attempts only), explicit restart-with-fallback, late-callback retention proof (§17). **Effort:** M
#### Task 14.4: Governed analytics projections over answers
- **Type:** create — `IsAnalyticsRelevant`/`IsOperationallyFilterable` projections (custom-property projection pattern), organizer dashboards without raw PII. **Effort:** L
#### Task 14.5: Company CSV bulk assignment + amendment flows
- **Type:** create — bulk participant assignment import; `RegistrationAmendment` controlled post-finalization changes. **Effort:** L
#### Task 14.6: Generalized submission sinks (Excel/Sheets/webhook consumers)
- **Type:** create — additional `IRegistrationSubmissionSink` implementations; approved-fields, post-commit, audited. **Effort:** M
#### Task 14.7: Blazor affordance-gating + Studio route/sidebar + accessibility audit
- **Type:** modify — client tests asserting every new mutating affordance and Studio section is `_links`-gated; route table covers all canonical `/studio/**` paths; actor navigation is replaced, not stacked, on event routes; keyboard/RTL/announcement audit per `docs/ACCESSIBILITY.md`. **Effort:** M
#### Task 14.8: Deferred commerce/admission design records (Hi.Events-informed)
- **Type:** create
- **Layer:** Docs
- **Files:** new `dev/active/registration-data-collection/deferred-design-records.md` (or `docs/adr/` notes when a decision is ripe)
- **Description:** Concise deferred design records per report §11.4, each citing the relevant `hi-events-report.md` section so future agents do not re-research the same repository: `PaymentAttempt` + provider payment/refund identity + reconciliation (unique provider attempt identity, idempotency keys, provider calls outside transactions — report §7.3/§7.5); `AdmissionTicket` with a rotatable signed/hashed admission credential that is **never** the display/public ID, transfer revoking/rotating the credential (report §5.5/§7.10); check-in lists mapped to ticket entitlements/sessions with append-only admission events, unique-active-admission constraint, authenticated or scoped-expiring scanner capabilities, camera/HID scanner UX with partial batch results (report §5.6/§7.11); ticket lookup/resend/self-service via hashed, single-purpose, expiring recovery capabilities without email enumeration (report §5.5); promo codes whose usage counts include live unexpired reservations (report §4.8); waitlist offers with expiry; optional add-ons/general products kept out of the admission vocabulary; taxes/fees/invoices snapshots.
- **Acceptance Criteria:**
  - [ ] Each record names its trigger, the ISLAMU aggregates it extends, and the report sections it supersedes
- **Effort:** M

---

## 7. Testing Strategy

One fastest relevant non-browser project per phase, exactly one `dotnet test` command at phase end (assignments and repeat-justifications inline in each phase above): P0 Architecture, P1 Domain, P2 API, P3 Architecture, P4 Persistence, P5 Persistence, P6 Application, P7 Domain, P8 Application, P9 API, P10 Infrastructure, P11 Infrastructure, P12 Infrastructure, P13 API, P14 Blazor.Client.

Contract-mandated projects not selected above are recorded as contract requirements, distributed without extra phases: `Explore.Secrets.UnitTests` coverage lands inside Task 9.1's acceptance (new secret definitions carry unit tests; if the dominant P9 risk shifts to secret handling, the implementing agent may substitute it as P9's single selected project); `Explore.Blazor.IntegrationTests` becomes relevant only if Task 3.2 changes the BFF pipeline — in that case it substitutes as P3's selected project. No E2E, Playwright, browser automation, Chrome DevTools MCP, visual QA, live-app smoke, Aspire/Docker startup, or manual runtime verification is planned anywhere. Provider adapters test against recorded fixtures exclusively. The consultation test matrices (§23 Report 1, §31 Report 2) are distributed into task acceptance criteria — they are requirements on tasks, not separate phases.

## 8. Documentation, Configuration, And Operations Impact

- **Docs:** `docs/adr/ADR-016..018` (new, P0); `docs/DOMAIN.md` (aggregate replacement, P5–8); `docs/API.md` + `docs/API_CHANGELOG.md` (every API phase); `docs/AUTHORIZATION.md` (new resources/relations and `StudioContextDto`, P1/5/7/13); `docs/BLAZOR.md` (canonical Studio routes, contextual navigation, and attendee/organizer boundary, P2–9); `docs/SECURITY-MODEL.md` + `docs/QUICK_REFERENCE.md` + `docs/GOVERNANCE.md` (PublicTransactional, P3); `docs/ADMIN_GUIDE.md` (instance monetization settings, P4); `docs/CONTACT_SHARING.md` (P13); `docs/WEBHOOKS.md` (registration effect kinds, P9); `docs/CUSTOM_PROPERTIES.md` (boundary note, P7); `docs/INTEGRATIONS.md` + `docs/SELF_HOSTING.md` + `docs/CONFIGURATION.md` + `docs/SECRETS.md` (P9–12); `schemas/islamu-event.md` (every schema phase); `docs/index.md` if new pages are added; `dev/active/registration-data-collection/deferred-design-records.md` (P14).
- **Configuration:** `public_transactional` rate policy (P3); instance monetization entities — fee policy + platform contribution, defaults off/zero (P4, documented in CONFIGURATION + ADMIN_GUIDE); provider secret definitions (P9+); optional Formbricks compose profile (P10); Google Pub/Sub configuration keys (P12) — all documented in CONFIGURATION/SECRETS at their phase.
- **Operations:** two new background workers (hold expiry P5, Google watch renewal P12) join the documented background-services list in `docs/ARCHITECTURE.md`/`docs/OPERATIONS.md`; reconciliation queues and provider health are organizer-visible, operator-documented in `docs/TROUBLESHOOTING.md`.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- **Trust boundaries:** provider callbacks verified before intake persistence (D7); external completion never confirms (§10); capability tokens hashed, single-purpose, expiring, never identity proof (D8/§12); embeds from approved domains only, server-generated, CSP-allowlisted; no open redirects; SSRF-guarded link checking (no private/metadata networks).
- **Authorization:** Cerbos policy-per-resource with provenance/organizer attributes; four authorities never implied by each other; contributor matrix (§23) enforced server-side and tested; HAL links are the only client affordance/navigation authority; fail-closed on ambiguous organizer authority (NFR-02). The Studio context endpoint revalidates the optional actor hint against the principal, returns `PrivateNoStore`, and exposes relations rather than roles or tenant-wide event data.
- **Privacy:** PII split entities (order/participant PII, sensitive answer ciphertext); field-level classification/purpose/retention/visibility/exportability governance (§18); consent immutable evidence with typed subjects; third-party processing disclosure before external-form launch; completion-only mode stores zero answers; logs/metrics/traces/ProblemDetails free of answers/emails/tokens/payloads (NFR-09) — asserted by tests in P8/P9.
- **Abuse:** `public_transactional` rate policy, idempotency, antiforgery, quotas per verified contact, best-effort-only honesty for anonymous limits (§19.5), link-reporting + moderation path, file quarantine default.
- **Monetization:** platform fee policy and platform-contribution enablement are instance-admin-only `Admin`-class surfaces — tenant admins, organizers, and curators fail closed (D18); API and persistence accept already-normalized integer minor units and define no decimal-major or FX conversion; buyer-chosen prices are validated server-side against pinned catalog bounds (D17); contribution money is segregated from organizer earnings in every DTO, export, and future payment split; display/public identifiers never authorize (Hi.Events §7.8 counter-example); monetization content is DB-stored, so no hardcoded solicitation text ships in the product.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Rationale |
|---|---|---|
| Multi-tenancy | **Applicable** | Every new entity is `ITenantEntity` under central query filters; callback binding resolution never discloses tenant existence; tenant governance intersects provider capabilities (D3) |
| Federation (AT Proto) | **Needs Investigation** | `EventRegistration.AtprotoRecordId` exists today; order/participant federation deliberately deferred (Task 5.9 decision); provenance `FEDERATED` value reserved |
| Localization | **Applicable / partially Needs Investigation** | UI strings follow existing localization; form-content language strategy resolved by Task 7.8; RTL required on all new surfaces |
| Accessibility | **Applicable** | NFR-11: quantity controls, skip controls, badges, status changes, iframe titles, focus, announcements — folded into every Blazor task; audit in 14.7 |
| Product | **Applicable** | §5 scenario table is the product acceptance surface; optional forms never called "registration"; honest CTAs |

## 11. Observability And Operations

Bounded metrics only (`provider`, `operation`, `outcome`, `trust_level`, `completion_mode`, `failure_category`, `action_kind`, `order_status`) added to `BusinessMetrics.cs` per phase — never tenant/event/form/response/attendee IDs, question keys, or values (§21). Provider health read models per §21 (Formbricks callback age/drift; Google watch state/expiry/checkpoint; Microsoft callback test/staleness/mapping completeness). Workers emit fenced-claim traces consistent with webhook workers. Troubleshooting runbook entries per provider land with their phase. Reconciliation queue depth is the primary operator saturation signal.

## 12. Migration And Compatibility Plan

Clean-baseline strategy per D13: **no data migrations, no shims, and no dual writes anywhere**. The privacy-erasure workstream's three generated init lanes are the baseline. Commit `ff30795a2` adds the ordered additive `20260728152646_AddParticipationHandlingModes` migration, designer, and snapshot for participation, ticketing, and instance monetization. Generated additive `20260729183118_RemoveLegacyEventPricing` plus its designer/snapshot drops the two nonnegative-price checks and Event/EventSession `price`/`currency_code`; `dotnet ef migrations has-pending-model-changes` reports no changes. These artifacts do not prove database application or runtime rollout. Breaking changes remain sanctioned: `Event.IsUserReported`/`EventUrl` deleted (P1); `Event.IsRegistrationRequired` deleted in source/contracts (P2); `Event.Price`/`CurrencyCode`/`EventSession.Price` deleted (P4); `EventRegistrationIntent` deleted and `EventRegistration`/`EventContactShareConsent` rewired (P5/P6/P13). Deployment cannot be claimed until migration application and runtime rollout are evidenced.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---|---|---|---|---|
| Registration accidentally rewrites the privacy-erasure-owned baseline | Low | High — corrupts migration history | Additive generated migrations only; no baseline regeneration or hand-edited snapshots | Any registration change to an existing init migration or snapshot | Every persistence phase |
| Committed migration artifacts are mistaken for database rollout | Medium | High | Treat `ff30795a2`, `20260729183118_RemoveLegacyEventPricing`, and the no-pending-model result as artifact/model evidence only; require explicit database-application and runtime-rollout evidence before deployment claims | Ledger or release note claims schema is applied without deployment evidence | Task 2.2 / release owner |
| Phase 5 aggregate replacement ripples wider than mapped (AT Proto, emails, notifications) | High | High | In-phase dependents sweep (5.9); delete-last sequencing; `rg` gates in acceptance | Build breaks on delete commits | 5.6/5.9 |
| Capacity race bugs under multi-replica load | Medium | High — oversell | Explicit locking in one transaction; persistence race tests; hold sweeper fencing | Race test flakes; pool counter drift | 5.3 |
| Scope explosion (15 phases erode) | High | Medium | Phases 10–12 parallelizable + independently shippable; P14 fully deferrable; per-phase DoD | tasks.md drift vs plan | all |
| Provider API drift vs consultation citations (dated 2026-07) | Medium | Medium | Conformance re-verification tasks (10.1/11.1/12.1); capability tuples fail closed | Conformance evidence deltas | 10.1, 11.1, 12.1 |
| Schema-hash instability breaking published-version identity | Low | High | Golden-hash tests, culture-invariant serializer frozen at first publish | Hash test failure | 7.4 |
| Guest abuse of PublicTransactional surface | Medium | Medium | Dedicated rate policy, idempotency, antiforgery, quotas, honest limits | Rate-limit metrics, reconciliation queue spikes | 3.1–3.3, 5.4 |
| PII leakage via logs/exports/metrics | Low | Critical | NFR-09 assertions in tests; bounded label review; export gating + audit | Log-scan test failures | 8.3, 13.3 |
| OpenAPI/NSwag churn destabilizing client | Medium | Medium | Regeneration as discrete per-phase final step; naming tests; changelog discipline | `ApiClientNamingTests` failures | every API phase |
| Answer CHECK constraints vs EF model friction | Medium | Low | Raw-SQL check constraints in configurations; persistence tests | Migration generation errors | 8.2 |
| Consent/subject refactor breaking existing contact-share flows | Medium | Medium | P13 owns the full vertical incl. docs; parity tests | Contact-share API test failures | 13.1 |
| Money-math defects in pricing modes / fee policy / contribution (rounding, currency, float drift) | Medium | High | Decimal-only value objects, explicit per-currency rounding rules, exhaustive decimal tests; Hi.Events float-money defect is the named counter-example | Failing decimal tests; totals mismatch between line/order/earnings figures | 4.1, 5.2, 5.10 |
| Tenant-level actors gaining monetization control (abuse of contribution/fees) | Low | Medium | Instance-admin-only Admin endpoints; fail-closed authorization tests; no tenant-visible management surface | Authorization parity failures; unexpected 200s in monetization tests | 4.5 |
| Hi.Events commercial breadth pulling deferred features into active phases | Medium | Medium | D19 scope discipline; deferred inventory lives only in Task 14.8 records | Discovered tasks referencing promo/refund/check-in outside P14 | 14.8 |
| AGPLv3 code contamination from Hi.Events breaking CLA dual-licensing | Low | Critical | §4.13 no-code-copy rule; clean-room implementation from report + plan only; agents never open the Hi.Events repo while coding; PR review watches for transcribed code | Code resembling Hi.Events sources; references to its repo paths in diffs | All phases; rule recorded in ADR-018 (0.2) |
| Studio actor-context over-disclosure or stale navigation authority | Low | High | Validate actor hints server-side; `PrivateNoStore`; relation-only DTO; event pages re-authorize operations and use event/resource HAL links | Unauthorized actor hint succeeds; role/event inventory appears in context DTO; sidebar action survives missing relation | 5.7, 5.8, 6.4, 14.7 |

## 14. Success Metrics And Definition Of Done

Functional success = the twelve Final-CTO scenarios (consultation Report 2 §34) each demonstrable through API + UI: lead-generation-only organizer; community listing; zero-action event; walk-in + optional form; name-only guest registration; authenticated application; no-sync external form; completion-only integration; family order; company order; child/adult ticket differentiation; payment-ready `AwaitingPayment` boundary. Plus Report 1's invariant: typed, normalized, queryable, governed answers regardless of collection UI. Plus the pricing/monetization scenarios (D17/D18): a donation ticket accepting 0; a pay-what-you-can ticket enforcing its minimum; a sliding-scale ticket whose dual sliders show the exact organizer earnings under the instance fee policy; and an instance-enabled platform-contribution checkout rendering DB-stored messaging with 0 preselected and 5/10/15/20% computed-amount options — while a fresh self-hosted instance shows none of it (defaults off/zero). Organizer operations are reachable only through the canonical Studio routes in §3: event sections derive from the shared event HAL resource, actor Orders/Attendees derive from `StudioContextDto`, and public/guest checkout remains outside Studio.

Per phase, the automated gate is exactly the phase's one Release build + one selected project test (no browser/runtime/manual gates). The workstream is Done when: all P0–P13 phase gates green (P14 optional/deferrable), the `registration-data-collection` intent's unique acceptance holds, both consultation anti-pattern lists have zero instances in `src/`, and docs listed in §8 match shipped behavior.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. At first implementation start, read plan, context, and tasks once; on cold resume, read context + tasks first, then only the plan sections for the current phase or changed decision.
2. During an uninterrupted session, do not reread unchanged plan/context/tasks after every task; keep the current task in working context and reopen only the exact section needed.
3. Start from the highest-priority unchecked task unless the user overrides.
4. `tasks.md` is the hot execution ledger: check a substantial task immediately after its implementation acceptance criteria are met; reconcile smaller completed tasks together no later than phase end.
5. Implementation-task and phase-verification checkboxes stay separate; a phase is complete only after its build and selected test checkboxes pass.
6. Update the status summary, completed count, current priority, next recommended slice, discovered tasks, deferred work, and `Last Updated` whenever task state changes.
7. Update context after a completed phase, meaningful decision, blocker, failed validation, material discovery, or before pause/compaction/transfer; do not rewrite for trivia.
8. Update the plan only when scope, architecture, phase order, acceptance criteria, risks, or validation strategy changes.
9. Record failed validation with known cause and next recovery action in tasks/context without marking the phase complete.
10. Before pausing, compaction, transfer, or PR creation: reconcile affected tasks, add a dated handoff, and identify unrelated dirty files the next contributor must avoid.
11. Run phase verification only after all phase tasks, with one Release build and at most one selected project test; do not repeat green commands or start the application/browser.
12. Never report completion when repository reality and the task ledger disagree.

Every implementation summary must teach: what changed and why; patterns/libraries/protocols used (CQRS/MediatR, UnitOfWork, outbox, HAL affordance gating, capability segregation, idempotency/fencing, tenant isolation); important files/classes/handlers with responsibilities; data/control flow; repo conventions honored; verification performed; remaining work; dev-doc update status.

## 16. Progress Reporting Contract

After each implementation slice:

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason
```

For completed work, `Docs updated` must confirm `tasks.md` reconciliation; report context and plan separately as updated or unchanged-because-no-trigger.

## 17. Potential Risks & Unknowns

The part most likely to fail or expand is **Phase 5**. Replacing `EventRegistrationIntent` while rewiring `EventRegistration`, `EventContactShareConsent`, HAL policies, Cerbos, the generated client, and the Blazor flow — under a migration baseline owned by a *different in-flight workstream* — concentrates the two highest risks (aggregate ripple + baseline deadlock) in one slice; the re-baseline added buyer-chosen pricing and the platform-contribution component to that same slice (5.2/5.8/5.10), raising its density further — if Phase 5 overruns, 5.10 plus the sliding-scale widget are the first candidates to split into a follow-on slice since D17/D18 snapshots keep them additive. The 5.9 dependents sweep is deliberately bounded, but AT Proto records and notification/email templates referencing registrations are only partially enumerated; expect discovered tasks there. Second-most-likely expansion: the answer-storage CHECK constraints (8.2) and the deterministic schema hash (7.4) are contract-like artifacts that are cheap to build but expensive to change after first publish — they must be treated as frozen the moment Phase 7/8 verification passes. Third: all external-provider facts are consultation-cited (July 2026) but were not re-verifiable in this planning session (research MCP tools unavailable); the conformance re-verification tasks (10.1/11.1/12.1) are mandatory gates, not formalities — if Formbricks API v2 leaves beta or Google's watch semantics change, capability tuples must be re-pinned before any binding activates.
