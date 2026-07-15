<!-- ABOUTME: Executable task checklist for the CTO-amended event-location privacy implementation. -->
<!-- ABOUTME: Sequences characterization, canonical EventLocation migration, disclosure authority, global erasure, outbox correction, UI, and verification. -->

# Event Location Privacy Tasks

**Status:** Planning complete; implementation not started  
**Last Updated:** 2026-07-15 Europe/Brussels  
**Plan:** `dev/active/event-location-privacy/event-location-privacy-plan.md`  
**Context:** `dev/active/event-location-privacy/event-location-privacy-context.md`

## Maintenance Rules

- Keep this file, the plan, and context synchronized after every task.
- Work in task-ID order unless the dependency table explicitly permits parallel work.
- Mark one implementation task `in progress` at a time; add evidence before checking it complete.
- Write the smallest failing test before non-trivial implementation.
- Record exact changed paths and commands/results in context.
- Never touch unrelated dirty-worktree changes.
- Stop for approval before weakening tenant isolation, public minimization, irreversible erasure, transaction atomicity, or governance floors.
- Generated clients and migrations are generated through canonical commands, never hand-edited.

## Phase Dependencies

| Phase | Entry gate | Exit gate |
|---|---|---|
| 0. Contract and characterization | Amended docs aligned | Leakage tests fail for known current behavior; Home Discovery blocker recorded |
| 1. Domain | Phase 0 complete | Kind/state/EventLocation/audience invariants pass Domain tests |
| 2. Registration access | Domain facts available | Event/Day/SessionSelection effective coverage passes exhaustive tests |
| 3. Persistence and migration | Domain stable | Expand/backfill/validate migrations and PostgreSQL tests pass |
| 4. Application disclosure | Persistence batch contracts stable | Pure evaluator and bounded batch service pass unit/integration budgets |
| 5. API/HAL/contracts | Application authority complete | Exact route/cache/auth split and generated contracts pass |
| 6. Global erasure/outbox/operations | EventLocation persistence and correction model complete | Atomic erasure, dispatch, restore replay, remediation pass |
| 7. Blazor | API/NSwag stable | HAL-gated accessible UX passes component/browser checks |
| 8. Outbound/discovery/docs | Disclosure authority stable | Every surface has evidence and operations/docs match behavior |
| 9. Final QA | All prior phases complete | Full build/tests/contracts/migrations/visual review green |

## Phase 0: Contract and Characterization

- [x] **ELP-000 — Incorporate Senior CTO amendments across all three durable docs**
  - Paths: `dev/active/event-location-privacy/*`.
  - Result: canonical EventLocation, contextual field sensitivity, irreversible state, intent coverage, global erasure, transactional outbox, route split, and enterprise operations are decision-complete.
  - Evidence: planning-only diff and architecture-document quality gate.

- [ ] **ELP-005 — Block stale Home Discovery address/coordinate contract before product edits**
  - Paths: `dev/active/home-discovery-experience/home-discovery-experience-plan.md`, `home-discovery-experience-context.md`, `home-discovery-experience-tasks.md`.
  - Change: correct the false claim that `LocationListDto` omits private data; source currently exposes Address. Forbid browser enumeration of exact addresses/coordinates. Block current-location work on coarse `PublicDiscoveryArea` or a later governed PostGIS design.
  - Result: Home Discovery cannot reintroduce location leakage or treat `ShowCoordinates` as indexing consent.
  - Verify: documentation link/schema tests and grep for contradictory exact-coordinate/address guidance.

- [ ] **ELP-010 — Add current-leakage characterization tests before contracts change**
  - Paths: `tests/Event.API.IntegrationTests/Features/LocationControllerTests.cs`, EventSession controller tests, `tests/Event.Application.UnitTests/Features/EventPrograms/`, Events calendar tests, and Blazor EventDetail tests.
  - Cases: anonymous Location detail/list address exposure; session/program/calendar/JSON-LD direct mapping; auth-cookie public response behavior; current private-address hint copy.
  - Result: tests document every current leak and become red/updated as authority is introduced.

- [ ] **ELP-020 — Freeze outbound surface inventory and purpose table**
  - Paths: plan Section 11, context outbound inventory, `docs/API.md`, `docs/SECURITY-MODEL.md`.
  - Inventory: sessions/program/agenda/JSON-LD, public/attendee calendars, email/reminders, tickets/QR, Svix webhooks, CSV/JSON, search, moderation, API keys, print/PDF/reports, MCP/AI, federation/PDS, discovery.
  - Result: each surface has owner, audience, purpose, allowed fields, cache policy, and target test.

- [ ] **ELP-030 — Capture API/OpenAPI/generated-client and migration baselines**
  - Paths: `src/Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`, current EF snapshot, API contract snapshots.
  - Result: known baseline and exact regeneration commands recorded in context.
  - Verify: generation commands produce no unexplained pre-change diff.

- [ ] **ELP-040 — Lock instance/tenant governance source and most-restrictive merge**
  - Paths: `src/Explore.Application/Contracts/Services/ITenantPolicySettingService.cs`, `src/Explore.Application/Services/TenantPolicySettingService.cs`, `TenantPolicySettingService.Read.cs`, `TenantPolicySettingService.Apply.cs`, planned `src/Explore.Application/Contracts/Services/ILocationPrivacyGovernanceService.cs`, and `src/Explore.Application/Services/LocationPrivacyGovernanceService.cs`.
  - Keys: `allow_home_locations`, `allow_public_exact_address`, `allow_public_coordinates`, `minimum_home_audience`, `default_reveal_offset` under `location_privacy`.
  - Result: exact ownership, precedence, validation, and audit paths recorded before schema work.

- [ ] **ELP-060 — Lock LocationKind, LocationPrivacyState, and irreversible erasure contract in tests**
  - Paths: planned `src/Explore.Domain/LocationKind.cs`, `LocationPrivacyState.cs`, `Location.cs`; tests under `tests/Event.Domain.UnitTests/`.
  - Cases: Unclassified codes; Active Home owner requirement; non-Home owner rejection; NotProvided vs Erased; Erased no owner/PII; reattachment rejected; replacement requires new Location.
  - Result: Domain lifecycle cannot be weakened by later persistence/UI work.

- [ ] **ELP-070 — Lock global-account deletion versus tenant-membership removal semantics**
  - Paths: `docs/MULTI_TENANCY.md`, `src/Explore.Application/Features/Users/Handlers/Commands/DeleteUserCommandHandler.cs`, planned `src/Explore.Application/Features/TenantUsers/Requests/Commands/RemoveTenantMembershipCommand.cs`, `src/Explore.Application/Features/TenantUsers/Handlers/Commands/RemoveTenantMembershipCommandHandler.cs`, and matching tests under `tests/Event.Application.UnitTests/Features/TenantUsers/Commands/`.
  - Result: global deletion erases every owned Home across tenants; membership removal changes TenantUser/TenantUserProfile only.
  - Verify: architecture test forbids membership handlers from invoking global privacy erasure.

## Phase 1: Domain Model

- [ ] **ELP-100 — Add normalized LocationKind lookup**
  - Paths: `src/Explore.Domain/LocationKind.cs`, enum/master-code companion following existing lookup convention, Domain tests.
  - Values: `UNCLASSIFIED`, `COMMERCIAL_VENUE`, `PUBLIC_SPACE`, `COMMUNITY_VENUE`, `PRIVATE_HOME`.
  - Result: stable int lookup; no behavior grants disclosure by kind.

- [ ] **ELP-110 — Add normalized LocationPrivacyState and audience lookups**
  - Paths: `src/Explore.Domain/LocationPrivacyState.cs`, `LocationDisclosureAudience.cs`, Domain tests.
  - Values: NotProvided/Active/Erased and Never/AnyCurrentRegistrant/ConfirmedParticipant.
  - Result: backend values are stable and separate from UI labels.

- [ ] **ELP-120 — Implement Location lifecycle and consent-backed Home ownership**
  - Paths: `src/Explore.Domain/Location.cs`, `LocationPii.cs`, planned ownership-transfer domain records/commands, Domain tests.
  - Change: kind/state/owner/erasure fields; `EraseOwnedPii()` tombstone; reject PII recreation; current-user default owner; explicit consent transfer.
  - Result: irreversible aggregate lifecycle with optimistic concurrency.

- [ ] **ELP-125 — Add canonical first-class EventLocation and migrate aggregate references conceptually**
  - Paths: planned `src/Explore.Domain/EventLocation.cs`; existing EventSession, EventSessionGroup, EventAgendaItem, EventSessionAgendaItem, and LocationRoom entities.
  - Change: field selections, audience, reveal time, review flag, policy version, concurrency/audit/soft-delete; nullable EventLocationId references prepared for migration.
  - Lifecycle: server fail-closed creation; final detach soft-delete; reattach fresh association.
  - Result: every event-local physical place is mediated by one EventLocation.

- [ ] **ELP-130 — Encode contextual field matrix including rooms and operational secrets**
  - Paths: `src/Explore.Application/Services/EventLocationDisclosureEvaluator.cs` tests, `docs/DOMAIN.md`, `docs/SECURITY-MODEL.md`.
  - Cases: country/timezone baseline; city/name/room contextual; room description management-only; exact derivatives sensitive; access instructions never public; Private Home generic label.
  - Result: field decisions are explicit and executable, not inferred from table location.

- [ ] **ELP-140 — Add EventLocation policy and exact-read audit models**
  - Paths: planned `src/Explore.Domain/EventLocationDisclosureAudit.cs`, exact-read security audit model in the established audit layer, tests.
  - Result: append-only old/new policy and audience/version audit plus PII-free exact-read audit; no address values.

- [ ] **ELP-150 — Add explicit Location To Be Announced remediation state**
  - Paths: `src/Explore.Domain/EventLocation.cs`, planned `tests/Event.Domain.UnitTests/EventLocationTests.cs`, and publication validation tests.
  - Result: `EventLocation.IsToBeAnnounced=true` is explicit, suppresses every physical-location field, permits publication without a usable physical venue, and is never inferred from erasure or missing PII; unusable required physical venues otherwise block publication.

## Phase 2: Registration-Intent Access

- [ ] **ELP-200 — Characterize registration intent lifecycle and null approval by mode**
  - Paths: `src/Explore.Domain/EventRegistrationIntent.cs`, `EventRegistration.cs`, `Enums/RegistrationScopeEnum.cs`, `Services/Registration/RegistrationPolicyRules.cs`, registration handlers/repositories and tests.
  - Result: table of effective Pending/Waitlisted/Confirmed/Rejected/Cancelled/Revoked states per registration mode; no guessed null semantics.

- [ ] **ELP-210 — Add EventLocationRegistrationAccess immutable result and effective-state resolver**
  - Paths: planned `src/Explore.Application/DTOs/Location/EventLocationRegistrationAccess.cs`, `src/Explore.Application/Contracts/Services/IEventLocationRegistrationAccessService.cs`, `src/Explore.Application/Services/EventLocationRegistrationAccessService.cs`, and `tests/Event.Application.UnitTests/Services/EventLocationRegistrationAccessServiceTests.cs`.
  - Fields: intent ID, scope, effective state, event/day/session coverage, requested EventLocation coverage.
  - Result: one fail-closed entitlement fact for the evaluator.

- [ ] **ELP-225 — Implement Event, Day, and SessionSelection EventLocation coverage**
  - Paths: planned `src/Explore.Application/Services/EventLocationRegistrationAccessService.cs`, `src/Explore.Application/Contracts/Persistence/IEventRegistrationRepository.cs`, `src/Explore.Persistence/Repositories/EventRegistrationRepository.cs`, `tests/Event.Application.UnitTests/Services/EventLocationRegistrationAccessServiceTests.cs`, and Persistence integration tests.
  - Rules: Event covers all eligible; Day covers eligible items on selected day; SessionSelection covers selected sessions only; no active intent denies.
  - States: Pending/Waitlisted broad only; Confirmed both; Rejected/Cancelled/Revoked/deleted deny.
  - Result: cross-day/session/location over-grant is impossible.

## Phase 3: Persistence and Migration

- [ ] **ELP-230A — Generate focused expand migration**
  - Paths: planned configurations `LocationKindConfiguration.cs`, `LocationPrivacyStateConfiguration.cs`, `LocationDisclosureAudienceConfiguration.cs`, `EventLocationConfiguration.cs`, `EventLocationDisclosureAuditConfiguration.cs`; `ExploreDbContext.cs`, `ExploreDbContext.QueryFilters.cs`, generated migration/snapshot.
  - Change: lookup seeds, Location lifecycle columns, optional PII, EventLocation/audit/ledger tables, nullable EventLocationId references, indexes/checks/tenant-safe FKs.
  - Result: additive schema supports fail-closed dual-write with valid Down before irreversible activation.
  - Verify: generated SQL reviewed for locks, defaults, tenant keys, UUIDv7, concurrency, and rollback.

- [ ] **ELP-230B — Implement idempotent Unclassified and EventLocation backfill**
  - Paths: generated `src/Explore.Persistence/Migrations/*_BackfillUnclassifiedEventLocations.cs`, EF snapshot, and planned `tests/Event.Persistence.IntegrationTests/Migrations/EventLocationBackfillTests.cs`.
  - Rules: every legacy Location => Unclassified; unique tenant/event/location EventLocation; country only; city only with recorded continuity exception; all other fields false; audience Never; NeedsPrivacyReview true; never infer Home/owner.
  - Result: repeat-safe backfill plus unresolved review metrics.

- [ ] **ELP-230C — Validate zero-gap data and contract old references**
  - Paths: focused contract migration, verification SQL in `docs/OPERATIONS.md`, integration tests.
  - Gate: zero missing EventLocationId, orphan, duplicate active pair, tenant mismatch, invalid Home state, resurrected Erased PII.
  - Result: required EventLocation references and removal of obsolete physical event-local references only after all consumers migrate.

- [ ] **ELP-240 — Add EventLocation repositories and bounded batch loading**
  - Paths: planned `src/Explore.Application/Contracts/Persistence/IEventLocationRepository.cs`, `src/Explore.Persistence/Repositories/EventLocationRepository.cs`, repository registration, tests.
  - Result: entity-returning, AsNoTracking read batches; tracked mutation; tenant-safe unique active association; no DTO projection.

- [ ] **ELP-250 — Add named global privacy-erasure repository query**
  - Paths: `ILocationRepository.cs`, `LocationRepository.cs`, `ExploreDbContext.QueryFilters.cs`, architecture and PostgreSQL tests.
  - Rule: explicit tenant-filter bypass strictly bounded by OwnerUserId and PrivateHome; no general unrestricted query.
  - Result: all current/former-tenant owned Homes found without cross-user leakage.

- [ ] **ELP-260 — Persist policy audit, erasure ledger, and concurrency**
  - Paths: planned repositories/configurations for EventLocation audit and erasure ledger, tests.
  - Result: xmin/concurrency conflicts produce stable API errors; policy versions monotonic; audit payloads PII-free.

## Phase 4: Application Disclosure Authority

- [ ] **ELP-300 — Add purpose-specific EventLocation DTOs and requests**
  - Paths: `src/Explore.Application/DTOs/Location/` planned public, attendee, management, update, request, and result records.
  - Result: public DTO exposes EventLocationId only; attendee/management shapes are separate; no generic exact LocationDto reuse.

- [ ] **ELP-310 — Implement pure EventLocationDisclosureEvaluator with exhaustive tests**
  - Paths: `src/Explore.Application/Services/EventLocationDisclosureEvaluator.cs`, unit tests.
  - Order: tenant/association, privacy state, purpose ceiling, governance, authorization/entitlement, server time, field policy, contextual redaction.
  - Result: deterministic fail-closed matrix including Private Home and TBA.

- [ ] **ELP-315 — Implement batched EventLocationDisclosureService and enforce query/auth budgets**
  - Paths: `src/Explore.Application/Contracts/Services/IEventLocationDisclosureService.cs`, `Services/EventLocationDisclosureService.cs`, request/result records, unit/integration tests.
  - API: `ResolveManyAsync(IReadOnlyCollection<EventLocationDisclosureRequest>, CancellationToken)`.
  - Result: deduplicated immutable EventLocationId-keyed result; bounded association/location+PII/room/registration/governance queries; one batched manager authorization; no N+1.

- [ ] **ELP-320 — Migrate public session/program/agenda/calendar projections**
  - Paths: public EventSession query handlers, `GetEventProgramSummaryRequestHandler.cs`, `GetEventCalendarExportRequestHandler.cs`, agenda handlers, `EventSessionMappingProfile.cs`.
  - Result: no direct Location/PII/room mapping; batch disclosure used once per response.

- [ ] **ELP-330 — Migrate event/location creation and attachment commands to server-created fail-closed EventLocation**
  - Paths: Event create/update/import/draft handlers and every `*CommandHandler.cs` under EventSessions, EventSessionGroups, EventAgendaItems, EventSessionAgendaItems, and LocationRooms that attaches/detaches LocationId.
  - Result: dual-write during migration; final detach soft-deletes; reattach fresh association; clients cannot omit policy creation.

- [ ] **ELP-340 — Implement governance composition, server-time reveal, and policy-version invalidation**
  - Paths: governance contract/implementation, EventLocation update handler, `src/Explore.Application/Caching/CacheTags.cs`, tests.
  - Result: most restrictive rule wins; reveal uses server UTC plus entitlement; tightening invalidates all affected projections.

- [ ] **ELP-350 — Add management authorization and PII-free exact-read security audit**
  - Paths: authorization descriptors/handlers following `IAuthorizedRequest` / resource policy conventions; exact-read audit service; tests.
  - Result: manager exact reads fail closed and are audited without values; UI gets HAL affordances only.

- [ ] **ELP-360 — Implement EventLocation policy concurrency and append-only audit**
  - Paths: policy update command/handler/validator, audit repository, Application and Persistence tests.
  - Result: expected concurrency token and PolicyVersion required; old/new selection/audience/reveal metadata recorded; addresses absent.

## Phase 5: API, HAL, and Contracts

- [ ] **ELP-400 — Add route-level authorization/cache characterization tests**
  - Paths: Event/Location API integration tests.
  - Cases: anonymous/auth-cookie equivalence, unauthorized attendee/manager, no-store headers, tenant mismatch, stale policy version.

- [ ] **ELP-405 — Implement exact public/attendee/management route split**
  - Paths: planned `src/Explore.API/Controllers/EventLocationController.cs` and `src/Explore.API/Hateoas/RouteNames.cs`.
  - Routes: public `/api/events/{eventId}/locations`; attendee `/my-access`; management `/{eventLocationId}/management`; disclosure PUT.
  - Result: public always public-only/no shared cache v1; attendee/management authorized/private/no-store.

- [ ] **ELP-410 — Add EventLocation HAL policies and assemblers**
  - Paths: planned `Hateoas/Policies/EventLocationLinkPolicy.cs`, `Hateoas/Assemblers/EventLocationResourceAssembler.cs`, `Extensions/HateoasAssemblerRegistration.cs`, tests.
  - Result: server-authorized edit/disclosure/owner-transfer/remediation links; no client role logic.

- [ ] **ELP-420 — Update OpenAPI/HAL schema and regenerate NSwag client**
  - Paths: `OpenApi/HalOpenApiSchemaCatalog.cs`, API changelog, generated `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, serializer context.
  - Result: purpose-specific contracts and EventLocationId; generated artifacts clean and never hand-edited.

- [ ] **ELP-430 — Remove generic anonymous exact Location detail and obsolete contracts**
  - Paths: `LocationController.cs`, old Location DTO endpoints/assemblers/policies after consumer migration.
  - Result: no anonymous physical exact dereference; coarse non-Home discovery remains explicitly governed.

- [ ] **ELP-440 — Split public and attendee calendar routes/contracts**
  - Paths: Event calendar controller/handlers/builders and tests.
  - Result: public ICS uses public-only disclosure; attendee ICS authorized/private/no-store; no Private Home data in public subscription URL; warning about third-party retention.

## Phase 6: Global Erasure, Outbox, and Operations

- [ ] **ELP-500 — Add adversarial transaction and cross-tenant erasure tests first**
  - Paths: `tests/Event.Application.UnitTests/Features/Users/Commands/DeleteUserCommandHandlerTests.cs`, Persistence integration tests.
  - Cases: two tenants/former memberships; room/name tombstone; rollback; crash-after-commit; membership removal; discovery derivative; no PII in outbox.

- [ ] **ELP-505 — Implement global cross-tenant Home erasure and durable tombstones**
  - Paths: `src/Explore.Application/Features/Users/Handlers/Commands/DeleteUserCommandHandler.cs`, planned `src/Explore.Application/Contracts/Persistence/IGlobalLocationPrivacyErasureRepository.cs`, `src/Explore.Persistence/Repositories/GlobalLocationPrivacyErasureRepository.cs`, `src/Explore.Domain/Location.cs`, `src/Explore.Persistence/Repositories/LocationRepository.cs`, `src/Explore.Domain/LocationErasureLedgerEntry.cs`, and its repository/configuration.
  - Result: one v1 transaction finds all OwnerUserId Homes across tenants, erases/tombstones, clears owner, preserves durable references, and completes user erasure.

- [ ] **ELP-510 — Separate tenant membership removal from global deletion**
  - Paths: planned `src/Explore.Application/Features/TenantUsers/Requests/Commands/RemoveTenantMembershipCommand.cs`, `src/Explore.Application/Features/TenantUsers/Handlers/Commands/RemoveTenantMembershipCommandHandler.cs`, authorization descriptor/policy beside that feature, and `tests/Event.Application.UnitTests/Features/TenantUsers/Commands/RemoveTenantMembershipCommandHandlerTests.cs`.
  - Result: removing TenantUser/TenantUserProfile does not invoke global erasure or alter other-tenant Homes.

- [ ] **ELP-515 — Persist privacy correction outbox inside the erasure transaction**
  - Paths: `src/Explore.Application/Services/LocationPrivacyOutboxMessageFactory.cs`, `IOutboxRepository.cs`, `DeleteUserCommandHandler.cs`, tests.
  - Messages: `LocationPiiErased` and required external-correction intents with IDs/versions only.
  - Result: rollback preserves PII and creates no message; committed erasure always has durable outbox rows before cache eviction.

- [ ] **ELP-520 — Verify concrete correction dispatch, idempotency, retry, and dead-letter recovery**
  - Paths: `src/Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs`, planned concrete location-privacy correction dispatcher/service, `InfrastructureServicesRegistration.cs`, `tests/Explore.Infrastructure.Tests/Infrastructure/CompositeOutboxMessageDispatcherTests.cs`, API outbox dead-letter tests.
  - Result: every new event type has a concrete route; duplicate delivery is safe; unknown/no-op routing cannot pass; dead letters are visible/reconcilable.

- [ ] **ELP-525 — Add backup/restore erasure replay runbook and pre-traffic gate**
  - Paths: `docs/OPERATIONS.md`, planned `src/Explore.Application/Contracts/Services/ILocationErasureReplayService.cs`, `src/Explore.Infrastructure/Services/Privacy/LocationErasureReplayService.cs`, `src/Explore.API/BackgroundServices/LocationPrivacyStartupGate.cs`, registration in `src/Explore.API/Program.cs`, and integration/operational tests.
  - Content: retention limits, external erasure ledger, replay before traffic, cache/index purge, correction replay, evidence SQL, failure incident path.
  - Result: older backup cannot serve resurrected Home PII.

- [ ] **ELP-530 — Implement post-erasure EventLocation remediation workflow**
  - Paths: EventLocation review query/commands, notification outbox, publication validator, admin dashboard API, tests.
  - Result: affected associations need review; managers notified; unusable physical location blocks publication; explicit TBA allowed.

- [ ] **ELP-540 — Add privacy metrics and alerts**
  - Metrics: Unclassified locations, NeedsPrivacyReview EventLocations, missing-policy fail-closed hits, erasure duration/count, correction retries/dead letters, restore replay failures, exceptional exact reads.
  - Rule: labels contain IDs/counts/categories only, never address/user-entered location text.
  - Result: self-hosters can operate and diagnose the feature safely.

## Phase 7: Blazor UX

- [ ] **ELP-600 — Migrate Blazor services and JSON serialization to generated purpose-specific contracts**
  - Paths: `Services/LocationService.cs`, `AdminService.cs`, `LookupCacheService.cs`, `Helpers/HalResourceExtensions.cs`, `Serialization/AppJsonSerializerContext.cs`.
  - Result: no component consumes unrestricted generic Location DTO for event display.

- [ ] **ELP-610 — Add EventLocation management editor with governance-aware controls**
  - Paths: existing create/edit Location dialogs, Event create/edit/session composer, new EventLocation disclosure component using existing wrappers/CSS isolation.
  - Controls: kind/state, field selections, audience, reveal time, governance-denied states, concurrency errors.
  - Result: mutations shown only from HAL links; safe defaults visible, especially Private Home.

- [ ] **ELP-620 — Implement Home owner consent and transfer UX**
  - Paths: management component/dialog and API-generated methods, component tests.
  - Result: current user default; selecting/transferring another owner requires explicit consent, confirmation, audit, and authorization.

- [ ] **ELP-630 — Implement public and attendee disclosure states**
  - Paths: EventDetail, EventDetailsSidebar, session/program components, component tests.
  - States: public-only, eligible delayed reveal, attendee exact, Private venue, TBA, erased/unavailable, policy review.
  - Result: no client clock or role/claim decisions; server DTO/HAL authoritative.

- [ ] **ELP-640 — Add manager privacy-review dashboard and remediation actions**
  - Paths: admin/event management pages and tests.
  - Result: Unclassified/NeedsPrivacyReview queue, replacement association, TBA action, publication blocker explanation.

- [ ] **ELP-650 — Remove overpromising private-address copy and sanitize JSON-LD**
  - Paths: `Pages/Events/EventDetail.razor`, `EventDetail.razor.cs`, localization bundles.
  - Result: “Register to see private address” appears only when server exposes a real eligible affordance; JSON-LD uses public disclosure only.

- [ ] **ELP-660 — Complete localization, accessibility, responsive, RTL, and visual QA**
  - Paths: `src/Explore.Infrastructure/Localization/Bundles/en.json`, `fr.json`, `ar.json`, Razor/CSS isolation, Blazor tests/E2E.
  - Result: WCAG 2.2 AA labels/focus/live announcements/no color-only state/24px targets; 375/768/1280 layouts and RTL pass real-browser QA.

## Phase 8: Outbound Surfaces, Discovery, and Documentation

- [ ] **ELP-700 — Migrate session/program/agenda/JSON-LD and both calendar surfaces**
  - Paths: handlers/components identified in context and ELP-020 inventory.
  - Result: batch disclosure authority used; public vs attendee purpose cannot drift.

- [ ] **ELP-715 — Audit email, notification, webhook, export, ticket, search, API-key, print, and report surfaces**
  - Paths: every concrete producer/serializer discovered from ELP-020; add tests beside each owner.
  - Result: each surface consumes purpose-limited EventLocation results or proves no location payload; Svix/email/export cannot bypass policy.

- [ ] **ELP-720 — Migrate MCP/AI/federation/PDS surfaces and correction behavior**
  - Paths: `src/Explore.API/Mcp/EventManagementMcp*.cs`, `src/Explore.API/BackgroundServices/PdsSyncWorker.cs`, `src/Explore.Infrastructure/Services/Federation/PdsService.cs`, AI disclosure matrix/registry docs/tests.
  - Result: sanitized location still passes through `IAiContextGateway`; policy tightening/erasure emits PII-free idempotent correction.

- [ ] **ELP-730 — Enforce PostGIS/discovery separation and erasure behavior**
  - Paths: Home Discovery docs and future discovery entity/service only if already in implementation scope; tests.
  - Result: no PII auto-copy, Private Home no point by default, erasure transactional cleanup, EventLocation/occurrence server-side joins, no exact client catalog.

- [ ] **ELP-740 — Update canonical architecture/security/API/domain/privacy/federation/testing docs**
  - Paths: docs listed in plan Section 3, API changelog, AI matrix/registry, Home Discovery overlap.
  - Result: shipped behavior, threat model, operator limits, route contracts, and recovery semantics are discoverable and non-contradictory.

## Phase 9: Final QA and Cleanup

- [ ] **ELP-800 — Verify migration/backfill/rollback and production-like PostgreSQL data shapes**
  - Evidence: generated SQL review, idempotent rerun, zero-gap queries, pre-activation Down, post-erasure forward repair, large-owner volume measurement.

- [ ] **ELP-810 — Run adversarial privacy, auth, tenant, cache, and outbox matrix**
  - Evidence: every acceptance row below linked to a test and result; no skipped critical case.

- [ ] **ELP-820 — Run per-project automated suites and Release build**
  - Commands: canonical commands from plan Section 18; never solution-level `dotnet test`.
  - Result: no failures and no new attributable warnings.

- [ ] **ELP-830 — Run OpenAPI/NSwag/contract cleanliness and browser visual/accessibility QA**
  - Result: generated artifacts clean; routes/headers/schemas stable; desktop/mobile/RTL/focus/error/loading/review flows pass.

- [ ] **ELP-840 — Final repository and dev-doc review**
  - Checks: `git diff --check`, diagnostics, architecture suite, no obsolete `EventLocationDisclosurePolicy`/Public legacy defaults/session-row entitlement, all three docs synchronized, unrelated work untouched.
  - Result: implementation-ready or implementation-complete status stated truthfully with remaining risks.

## Mandatory Acceptance Matrix

| Acceptance case | Primary task |
|---|---|
| Unknown legacy becomes Unclassified, never Public | ELP-230B |
| Active Home owner valid; non-erased ownerless invalid; Erased ownerless/PII-less valid; resurrection rejected | ELP-060 / ELP-120 |
| Person/household venue and room labels/descriptions tombstoned and never public | ELP-130 / ELP-505 |
| Same physical Location has independent per-event policies | ELP-125 |
| Public contract exposes EventLocationId, not unrestricted LocationId | ELP-300 / ELP-405 |
| Event/Day/SessionSelection coverage is exact | ELP-225 |
| Pending/waitlisted broad only; cancelled/revoked/deleted deny; null resolved by mode | ELP-200 / ELP-225 |
| Homes in two tenants are both globally erased | ELP-500 / ELP-505 |
| Membership removal does not erase global/other-tenant data | ELP-070 / ELP-510 |
| Rollback leaves PII and outbox unchanged; post-commit crash finds outbox | ELP-500 / ELP-515 |
| Public endpoint remains public-only with auth cookie | ELP-400 / ELP-405 |
| Attendee/management are private/no-store | ELP-400 / ELP-405 |
| Tightened policy/governance defeats stale cache | ELP-340 / ELP-810 |
| Public calendar public-only; attendee calendar authorized/no-store | ELP-440 / ELP-700 |
| Email/webhook/export/ticket/search/report cannot bypass authority | ELP-715 |
| Concrete correction dispatcher is idempotent/retryable/dead-letter visible | ELP-520 |
| Backup restore replays erasure before traffic | ELP-525 |
| Discovery point is never auto-created from PII and erases transactionally | ELP-730 |
| Batch projection stays within query/auth count budget | ELP-315 |
| Server time controls reveal and cannot bypass entitlement | ELP-310 / ELP-340 |
| Private Home safe default is generic public label plus ConfirmedParticipant | ELP-110 / ELP-310 |

## Implementation Completion Evidence

For every checked task, append to context:

- exact files changed;
- failing test added first;
- command and result;
- migration/contract/cache/security implications;
- unresolved risk or `none`;
- next task ID.

Do not check a task because code was written. Check it only when its result and verification evidence exist.
