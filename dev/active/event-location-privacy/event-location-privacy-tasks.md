<!-- ABOUTME: Executable task checklist for the CTO-amended EventLocation privacy implementation. -->
<!-- ABOUTME: Sequences EventLocation migration, disclosure authority, correction/remediation, UI, and verification. -->

# Event Location Privacy Tasks

**Status:** 40 of 59 EventLocation tasks are verified complete. Backend disclosure authority, batched resolution, management authorization/audit, policy concurrency, API route split (PATCH disclosure / POST remediation / review queue / public / attendee), HAL affordance gating, additive OpenAPI/NSwag client generation (ELP-420A), calendar split, typed platform erasure adapter, outbox correction dispatch, and remediation workflow are fully implemented and verified. Current focus is Phase 7 Blazor UX adoption (`ELP-600`..`ELP-660`) and observability metrics (`ELP-540`).
**Last Updated:** 2026-08-18 Europe/Brussels
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

## Execution Dependencies

| Phase | Entry gate | Exit gate |
|---|---|---|
| 0. Contract and characterization | Amended docs aligned | Leakage tests fail for known current behavior; Home Discovery blocker recorded |
| 1. Domain | Phase 0 complete | Kind/state/EventLocation/audience invariants pass Domain tests |
| 2. Registration access | Domain facts available | Event/Day/SessionSelection effective coverage passes exhaustive tests |
| 3. Persistence and migration | Domain stable | Expand/backfill/validate migrations and PostgreSQL tests pass |
| 4. Application disclosure | Persistence batch contracts stable | Pure evaluator and bounded batch service pass unit/integration budgets |
| 5. API/HAL/contracts | Application authority complete | Exact route/cache/auth split and generated contracts pass |
| 6. Correction and remediation | EventLocation persistence and correction model complete | Typed platform adapter, dispatch, cache convergence, and remediation pass |
| 7. Blazor | API/NSwag stable | HAL-gated accessible UX passes component/browser checks |
| 8. Outbound/discovery/docs | Disclosure authority stable | Every surface has evidence and operations/docs match behavior |
| 9. Final QA | All prior phases complete | Full build/tests/contracts/migrations/visual review green |

The exact execution order is the wave table in plan Section 16. Critical corrections are: `ELP-015` runs immediately after leakage/route characterization; `ELP-350` precedes `ELP-315`; platform erasure is consumed only through the typed EventLocation adapter; `ELP-420A` precedes Blazor adoption; and `ELP-230C`, `ELP-430`, then `ELP-420B` form the final contraction lane.

## Phase 0: Contract and Characterization

- [x] **ELP-000 — Re-baseline the approved architecture across all three durable docs**
- [x] **ELP-005 — Block stale Home Discovery address/coordinate contract before product edits**
- [x] **ELP-010 — Add current-leakage characterization tests before contracts change**
- [x] **ELP-015 — Ship immediate Stage A fail-closed public minimization**
- [x] **ELP-020 — Freeze outbound surface inventory and purpose table**
- [x] **ELP-030 — Capture API/OpenAPI/generated-client and migration baselines**
- [x] **ELP-040 — Lock instance/tenant governance source and most-restrictive merge**
- [x] **ELP-060 — Lock LocationKind, LocationPrivacyState, and audience code contracts in tests**

## Phase 1: Domain Model

- [x] **ELP-100 — Add normalized LocationKind lookup**
- [x] **ELP-110 — Add normalized LocationPrivacyState and audience lookups**
- [x] **ELP-120 — Implement Location lifecycle and consent-backed Home ownership**
- [x] **ELP-125 — Add canonical first-class EventLocation and migrate aggregate references conceptually**
- [x] **ELP-130 — Encode contextual field matrix including rooms and operational secrets**
- [x] **ELP-140 — Add EventLocation policy and exact-read audit models**
- [x] **ELP-150 — Add explicit Location To Be Announced remediation state**

## Phase 2: Registration-Intent Access

| Registration mode when approval is null | Effective result | Disclosure consequence |
|---|---|---|
| `Open` | `Approved` (`Confirmed`-equivalent) | May qualify for attendee disclosure only after requested-placement scope coverage and every policy/governance/reveal gate pass. |
| `ApprovalRequired` | `Pending` | Qualifies only for organizer-selected `ANY_CURRENT_REGISTRANT`; never for `CONFIRMED_PARTICIPANT`. |
| `InviteOnly` | Deny unless a separate authority proves a valid invitation | No invitation authority exists today, so null approval denies registration and location access. |
| `Closed` | Deny | Do not create an access-bearing intent. |

| Persisted/lifecycle fact | Effective disclosure state | Location disclosure ceiling |
|---|---|---|
| `Approved` | `Confirmed` | Exact disclosure may proceed only when the intent scope covers the requested EventLocation placement and all remaining gates pass. |
| `Pending` or `Waitlisted` | Same state | `ANY_CURRENT_REGISTRANT` only, with requested-placement coverage; never confirmed-participant authority. |
| `Rejected`, `Cancelled`, or `Revoked` | Same terminal state | Deny. |
| `Soft-deleted or expired intent/registration` | Non-live | Deny. |

`ApprovalStatus` now persists stable `Cancelled=5`/`CANCELLED` and `Revoked=6`/`REVOKED` values. Partial cancellation removes only the cancelled child coverage while remaining live children keep the parent intent live; last-child cancellation synchronizes the parent terminal state. Pending/Approved consume capacity and terminal transitions release it. ELP-210 provides the immutable pure EventLocation entitlement result. Verified ELP-225 loads exact tenant/event/user registration entities and derives current Event/Day/SessionSelection placement coverage.

- [x] **ELP-200 — Characterize registration intent lifecycle and null approval by mode**
  - Paths: `src/Explore.Domain/EventRegistrationIntent.cs`, `EventRegistration.cs`, `ApprovalStatus.cs`, `Enums/ApprovalStatusEnum.cs`, `Enums/RegistrationModeEnum.cs`, `Enums/RegistrationScopeEnum.cs`, `Services/Registration/RegistrationPolicyRules.cs`, registration handlers/repositories and tests.
- [x] **ELP-210 — Add EventLocationRegistrationAccess immutable result and effective-state resolver**
- [x] **ELP-225 — Implement Event, Day, and SessionSelection EventLocation coverage**
  - Paths: planned `src/Explore.Application/Services/EventLocationRegistrationAccessService.cs`, `src/Explore.Application/Contracts/Persistence/IEventRegistrationRepository.cs`, `src/Explore.Persistence/Repositories/EventRegistrationRepository.cs`, `tests/Event.Application.UnitTests/Services/EventLocationRegistrationAccessServiceTests.cs`, and Persistence integration tests.
  - Rules: Event covers all eligible; Day covers eligible items on selected day; SessionSelection covers selected sessions only; no active intent denies.
  - States: Pending/Waitlisted broad only; Confirmed both; Rejected/Cancelled/Revoked/deleted deny.
  - Result: cross-day/session/location over-grant is impossible.
  - Evidence: one tenant-filter-preserving `AsSingleQuery`/`AsNoTrackingWithIdentityResolution` read returns `EventRegistration` entities and bypasses only the named soft-delete filter. Application revalidates every tenant/event/user/navigation identity, excludes deleted or expired facts, derives Event/Day coverage from the current placement graph, keeps SessionSelection child-bound, and chooses the strongest live intent with a stable tie-break. The 62-case Application matrix passed 62/62; disposable PostgreSQL coverage passed 3/3 on current owned binaries; Clean Architecture passed 15/15, tenant-filter architecture 4/4, and Code Hygiene 4/4. The provider probe observed one reader for 300 requested IDs, three deduplicated results, zero tracked entries, and end-to-end cancellation-token forwarding. Independent verdict: `confirmed` at 0.96 confidence in `.omo/evidence/task-9-registration-coverage-adversarial-verify.md`.

## Phase 3: Persistence and Migration

- [x] **ELP-230A — Generate focused expand migration**
  - Paths: planned configurations `LocationKindConfiguration.cs`, `LocationPrivacyStateConfiguration.cs`, `LocationDisclosureAudienceConfiguration.cs`, `EventLocationConfiguration.cs`, `EventLocationDisclosureAuditConfiguration.cs`; `ExploreDbContext.cs`, `ExploreDbContext.QueryFilters.cs`, generated migration/snapshot.
  - Change: lookup seeds, Location lifecycle columns, optional PII, EventLocation/audit tables, nullable EventLocationId references, TBA/location XOR, filtered uniqueness, indexes/checks/tenant-safe consistency FKs.
  - Result: additive schema supports fail-closed dual-write with valid Down before irreversible activation.
  - Verify: generated SQL reviewed for locks, defaults, tenant keys, UUIDv7, concurrency, local checkpoint, and rollback; `Database:Migrations:EventLocationPrivacyStage=Expand|Backfill|Contract` is required while ELP migrations are pending, and a missing/invalid selector cannot auto-apply backfill/contract.
  - Evidence: `20260716132239_AddEventLocationPrivacyExpand`, its generated Designer/snapshot state, `EventLocationPrivacyMigrationStage`, `ExploreDatabaseMigrator`, API startup, MigrationService startup, lookup-seeder activation, and `EventLocationMigrationStageTests` are present. Independent PostgreSQL 18 verification passed the original five migration-stage tests and the Persistence `EventLocationPrivacy` category 40/40; exact 783-line idempotent Expand SQL applied twice, fresh/legacy upgrade and pre-activation Down preserved legacy rows, and raw SQL rejected append-only audit deletion, erased-PII reattachment/resurrection, tombstone restoration, carrier mismatches, and referenced-association detach.

- [x] **ELP-230B — Implement idempotent Unclassified and EventLocation backfill**
  - Paths: generated `src/Explore.Persistence/Migrations/*_BackfillUnclassifiedEventLocations.cs`, EF snapshot, and planned `tests/Event.Persistence.IntegrationTests/Migrations/EventLocationBackfillTests.cs`.
  - Rules: every legacy Location => Unclassified; PII present => Active, PII absent => NotProvided, never legacy Erased; unique tenant/event/location EventLocation; country only; city only with recorded continuity exception; all other fields false; audience Never; NeedsPrivacyReview true; never infer Home/owner.
  - Result: repeat-safe backfill plus unresolved review metrics.
  - Gate: operator-selected `Backfill` target; zero-gap verification succeeds before policy activation.
  - Evidence: `20260718215537_BackfillUnclassifiedEventLocations` conservatively covers all four real carrier tables, preserves event-local pair identity, derives lifecycle only from `LocationPii`, writes country-only/audience-Never/review-required UUIDv7 associations plus PII-free typed audits, and fills only missing authority. Fresh PostgreSQL acceptance passed 3/3 twice with stable replay hashes/IDs, later-row repair, atomic malformed failure, zero carrier gaps, guarded/safe `Down`, forward convergence, and existing dual-writes preserved; migration stages passed 7/7, Persistence privacy 54/54, and Clean Architecture 15/15.

- [ ] **ELP-230C — Validate zero-gap data and contract old references**
  - Paths: focused contract migration, verification SQL in `docs/OPERATIONS.md`, integration tests.
  - Gate: zero missing EventLocationId, orphan, duplicate active pair, tenant mismatch, invalid Home state, resurrected Erased PII.
  - Result: required EventLocation references and obsolete public/contract references removed only after all consumers migrate; internal physical scheduling IDs required for composite room/GiST integrity remain consistency-constrained.
  - Gate: operator-selected `Contract` target runs only in W18 after ELP-420A client adoption and all consumers; automatic startup migration cannot collapse A/B/C. ELP-420B regenerates the final client afterward.

- [x] **ELP-240 — Add EventLocation repositories and bounded batch loading**
  - Paths: `src/Explore.Application/Contracts/Persistence/IEventLocationRepository.cs`, `src/Explore.Persistence/Repositories/EventLocationRepository.cs`, related audit/checkpoint contracts and repositories, registration, and tests.
  - Result: entity-returning, AsNoTracking read batches; tracked mutation; tenant-safe unique active association; no DTO projection.
  - Evidence: Application persistence contracts and repositories cover EventLocation, disclosure audit, and exact-read audit without DTO, `IQueryable`, or DbContext leakage. Reads are bounded/ordered/no-tracking, mutations tracked, and missing tenant context fails closed. Repository integration passed 12/12 and relational model verification passed 1/1; SaveChanges rejects tenant/event/location/room/parent-session mismatches across all four carriers, backed by database triggers.

- [x] **ELP-260 — Persist EventLocation policy/exact-read audit and concurrency**
  - Paths: EventLocation audit/exact-read repositories, configurations, Application contracts, and integration tests.
  - Result: concurrency-token conflicts produce stable API errors; policy and exact-read evidence is append-oriented and PII-free.
  - Persistence evidence: EventLocation creation atomically writes truthful aggregate-derived `0→1` `AssociationCreated` audit; later writes require contiguous versions matching the aggregate and competing writers yield one winner plus stable `concurrent_update`. Disclosure/exact-read audits are tenant-filtered and append-oriented. Persistence repository passed 12/12, model 1/1, Domain EventLocationPrivacy 63/63.

## Phase 4: Application Disclosure Authority

- [x] **ELP-300 — Add purpose-specific EventLocation DTOs and requests**
  - Paths: `src/Explore.Application/DTOs/Location/EventLocationDisclosureContract.cs`, `EventLocationDisclosureRequest.cs`, `EventLocationDisclosureResult.cs`, and `EventLocationDtos.cs`.
  - Result: public DTO exposes EventLocationId only; attendee/management shapes are separate; no generic exact LocationDto reuse.
  - Evidence: separate public, attendee, management, policy-update, internal request, and constrained result contracts use closed purpose-specific factories. Public exposes EventLocationId but no physical LocationId; suppressed Hidden/TBA/unavailable/review states cannot carry values. Disclosure contract tests passed 17/17 and final security/code-quality reviews found no high/medium issue.

- [x] **ELP-310 — Implement pure EventLocationDisclosureEvaluator with exhaustive tests**
  - Paths: `src/Explore.Application/Services/EventLocationDisclosureEvaluator.cs`, unit tests.
  - Order: tenant/association, privacy state, purpose ceiling, governance, authorization/entitlement, server time, field policy, contextual redaction.
  - Result: deterministic fail-closed matrix including Private Home and TBA.
  - Evidence: `EventLocationDisclosureEvaluator`, `EventLocationDisclosureResult`, and their focused tests distinguish malformed facts as `Hidden` from valid `NotProvided`/`Erased` as `Unavailable`, always with null values, empty fields, and no physical `LocationId`. Management disclosure does not accept or return physical `LocationId`. Evaluator tests passed 72/72 twice, contract 17/17, registration access 42/42, governance 15/15, Clean Architecture 15/15, and Code Hygiene 4/4.

- [x] **ELP-315 — Implement batched EventLocationDisclosureService and enforce query/auth budgets**
  - Paths: `src/Explore.Application/Contracts/Services/IEventLocationDisclosureService.cs`, `Services/EventLocationDisclosureService.cs`, request/result records, unit/integration tests.
  - API: `ResolveManyAsync(IReadOnlyCollection<EventLocationDisclosureRequest>, CancellationToken)`.
  - Result: deduplicated immutable EventLocationId-keyed result; bounded association/location+PII/room/registration/governance queries; one batched manager authorization; no N+1.
  - Evidence: `EventLocationDisclosureService` accepts at most 256 requests, normalizes public identity away, derives private identity from `ICurrentUserService`, permits a public batch to span events within one tenant/purpose, and requires private batches to remain within one event/requester. Duplicate EventLocation requests with conflicting room contexts are conservatively normalized to no room disclosure. The service returns an immutable EventLocationId-keyed dictionary after one bounded EventLocation read with Location/PII, one bounded room read, one governance resolution, and then either one tenant/event/user registration-coverage read or one ELP-350 manager authorization/audit batch. Verified by `EventLocationDisclosureServiceTests` (Application Unit, 319 lines) and `EventLocationDisclosureBatchTests` (Persistence Integration, 529 lines on PostgreSQL 18).

- [x] **ELP-320 — Migrate public session/program/agenda backend projections**
  - Paths: public EventSession query handlers, `GetEventProgramSummaryRequestHandler.cs`, agenda handlers, `EventSessionMappingProfile.cs`.
  - Result: no direct Location/PII/room mapping; batch disclosure used once per response.
  - Evidence: public session, session-group, event-agenda, session-agenda, merged-agenda, and program handlers build event-scoped placements and resolve one public `IEventLocationDisclosureService` batch per response. Shared projection seam maps only `EventLocationDisclosureResult` into nested `EventLocationPublicDto`; legacy physical Location/room fields remain explicit null compatibility seams. AutoMapper ignores those fields. Verified by `PublicEventLocationProjectionTests`, `EventAgendaItemLocationPrivacyHandlerTests`, and `EventSessionGroupLocationPrivacyHandlerTests`.

- [x] **ELP-330 — Migrate event/location creation and attachment commands to server-created fail-closed EventLocation**
  - Paths: Event create/update/import/draft handlers and every `*CommandHandler.cs` under EventSessions, EventSessionGroups, EventAgendaItems, EventSessionAgendaItems, and LocationRooms that attaches/detaches LocationId.
  - Result: dual-write during migration; final detach soft-deletes; reattach fresh association; clients cannot omit policy creation.
  - Evidence: `EventLocationAttachmentService` resolves the unique active physical/TBA association or creates a fail-closed audited one; all four carrier families assign it on create/draft/update and detach it after the final live delete or reassignment inside the same application transaction. Event moves create event-scoped associations, clear operations become explicit TBA, reattachment never resurrects a soft-deleted association, and referenced rooms cannot be moved across locations. Development seeding is an in-scope writer (36 carriers map to 8 distinct active authorities). Independent verification passed real-PostgreSQL seeder 6/6, dual-write 8/8 twice, attachment service 9/9, session-agenda handlers 6/6, strict fixture-dependent handlers 42/42, Clean Architecture 15/15, Code Hygiene 4/4, and root Release build with 0 errors.

- [x] **ELP-340 — Implement governance composition, server-time reveal, and policy-version invalidation**
  - Paths: governance contract/implementation, EventLocation update handler, `src/Explore.Application/Caching/CacheTags.cs`, tests.
  - Result: most restrictive rule wins; reveal uses server UTC plus entitlement; tightening invalidates all affected projections.
  - Evidence: five typed location-privacy keys merge through the most restrictive lattice, malformed values fail closed, tenant writes cannot widen instance ceilings, and reveal decisions use controlled server UTC. Setting rows, EventLocation policy-version/review corrections, disclosure audits, and PII-free `location.privacy.corrected` outbox rows stay inside the shared transaction; post-commit invalidation runs only after outer commit. Governance services passed 15/15, API governance 2/2, Domain privacy audit 8/8, selected cache/architecture 5/5, PostgreSQL EventLocation repository 12/12, handler commit/rollback ordering 10/10.

- [x] **ELP-350 — Add management authorization and PII-free exact-read security audit**
  - Paths: authorization descriptors/handlers following `IAuthorizedRequest` / resource policy conventions; exact-read audit service; tests.
  - Result: manager exact reads fail closed and are audited without values; UI gets HAL affordances only.
  - Evidence: `EventLocationManagementAuthorizationService` normalizes a tenant-bounded batch, loads parent `Event` authorization targets once, evaluates `event:view-management` in one `IAuthorizationProvider.IsAllowedBatchAsync` call, and maps missing targets/decisions to deny. Every decision is persisted first through `EventLocationExactReadAuditService` (identities, typed purpose, decision, server UTC, trace metadata only). Verified by `EventLocationManagementAuthorizationServiceTests`, `EventLocationExactReadAuditServiceTests`, and `EventLocationDisclosureBatchTests`.

- [x] **ELP-360 — Implement EventLocation policy concurrency and append-only audit**
  - Paths: policy update command/handler/validator, audit repository, Application and Persistence tests.
  - Result: expected concurrency token and PolicyVersion required; old/new selection/audience/reveal metadata recorded; addresses absent.
  - Evidence: `UpdateEventLocationPolicyCommandHandler` enforces `ExpectedConcurrencyStamp` and `ExpectedPolicyVersion`, updates `EventLocation`, appends aggregate-derived `EventLocationDisclosureAudit`, writes `location.privacy.corrected` outbox message transactionally, and evicts hybrid cache tags (`CacheTags.EventLocations`, `CacheTags.EventLocationsByEvent(eventId)`). Verified by `UpdateEventLocationPolicyCommandHandlerTests`, `EventLocationPolicyExactCacheInvalidationTests`, and `EventLocationPolicyVersionBoundaryTests`.

## Phase 5: API, HAL, and Contracts

- [x] **ELP-400 — Add route-level authorization/cache characterization tests**
  - Paths: Event/Location API integration tests.
  - Cases: anonymous/auth-cookie equivalence, unauthorized attendee/manager, no-store headers, tenant mismatch, stale policy version.
  - Result: Stage-A public responses are identical across anonymous/authenticated principals and physical values are absent; generic Location/room and temporary managed event routes require resource authorization, reject cross-tenant/cross-event enumeration, and send `private, no-store`.
  - Evidence: managed/API ELP 19/19, API public ELP 11/11, public eligibility 2, native Cerbos 461/461, language security 3/3 plus handler 5/5/controller 2/2, and API Release build 7 projects with 0 errors/0 warnings.

- [x] **ELP-405 — Implement exact public/attendee/management route split**
  - Paths: `src/Explore.API/Controllers/EventLocationController.cs`, `src/Explore.API/Hateoas/RouteNames.cs`.
  - Routes: public `GET /api/events/{eventId}/locations`; attendee `GET /api/events/{eventId}/locations/my-access`; management `GET /api/events/{eventId}/locations/{eventLocationId}/management`; review `GET /api/events/{eventId}/locations/review`; disclosure `PATCH /api/events/{eventId}/locations/{eventLocationId}/disclosure`; remediation `POST /api/events/{eventId}/locations/{eventLocationId}/remediation/confirm`.
  - Result: public always public-only/no shared cache v1; attendee/management authorized/private/no-store. Error responses use RFC 7807 `ProblemDetails` with typed problem descriptors (`EventLocationNotFoundProblem`, `DisclosureValidationProblem`, `RemediationValidationProblem`).
  - Evidence: `EventLocationController` exposes all 6 endpoints with explicit `[EndpointClassification]`, `[PrivateNoStore]`, and `[ProducesResponseType]` metadata. Verified by `EventLocationControllerTests`, `EventLocationPrivacyApiContractTests`, and `EventLocationPrivacyPublicEligibilityTests`.

- [x] **ELP-410 — Add EventLocation HAL policies and assemblers**
  - Paths: `src/Explore.API/Hateoas/Policies/EventLocationLinkPolicy.cs`, `src/Explore.API/Hateoas/Assemblers/EventLocationResourceAssembler.cs`, `src/Explore.API/Extensions/HateoasAssemblerRegistration.cs`.
  - Result: server-authorized edit/disclosure and remediation links; no client role logic.
  - Evidence: `EventLocationManagementLinkPolicy` emits `edit` (targeting `PATCH .../disclosure`) when event-update authorization passes and `remediate-location` (targeting `POST .../remediation/confirm`) when `NeedsPrivacyReview` is set. Registered in `HateoasAssemblerRegistration.cs`. Verified by `EventLocationHateoasTests`.

- [x] **ELP-420A — Generate additive OpenAPI/HAL schema and NSwag client**
  - Paths: `src/Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs`, `schemas/openapi_islamu-event.json`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
  - Result: purpose-specific additive contracts and EventLocationId are available for ELP-600 adoption; generated artifacts clean and never hand-edited.
  - Evidence: `HalOpenApiSchemaCatalog.cs` registers `EventLocationManagementDto`, `HalResourceOfEventLocationManagementDto`, and `HalCollectionEmbeddedOfEventLocationManagementDto`. Generated client `EventApiClient.g.cs` exposes `GetPublicEventLocationsAsync`, `GetAttendeeEventLocationsAsync`, `GetManagementEventLocationAsync`, `GetEventLocationReviewQueueAsync`, `UpdateEventLocationDisclosureAsync`, and `ConfirmEventLocationRemediationAsync`.

- [ ] **ELP-420B — Regenerate and prove final OpenAPI/HAL contract**
  - Dependencies: every backend and Blazor consumer uses purpose-specific contracts, ELP-230C contracted persistence, and ELP-430 removed obsolete anonymous exact routes/contracts.
  - Paths: the ELP-420A artifacts, API inventory/snapshots, and generated-client cleanliness checks.
  - Result: obsolete generic event-location schemas have zero consumers and final generation is clean; generated artifacts match the contracted runtime.

- [ ] **ELP-430 — Remove generic anonymous exact Location detail and obsolete contracts**
  - Paths: `LocationController.cs`, old Location DTO endpoints/assemblers/policies after consumer migration.
  - Result: no anonymous physical exact dereference; coarse non-Home discovery remains explicitly governed.

- [x] **ELP-440 — Split public and attendee calendar routes/contracts**
  - Paths: Event calendar controller/handlers/builders and tests.
  - Result: public ICS uses public-only disclosure; attendee ICS authorized/private/no-store; no Private Home data in public subscription URL; warning about third-party retention.
  - Evidence: `GetEventCalendarExportRequestHandler` (public disclosure, public event required) and `GetAttendeeCalendarExportRequestHandler` (attendee disclosure, authenticated, `[PrivateNoStore]`, `X-Calendar-Retention-Warning`) feed `IcalNetEventCalendarFileBuilder`. Verified by `EventLocationPrivacyApiContractTests`.

## Phase 6: Platform Adapter, Correction, and Remediation

- [x] **ELP-515 — Prove the typed EventLocation platform-erasure adapter**
  - Paths: `src/Explore.Application/Contracts/Persistence/IUserLocationPrivacyErasureRepository.cs`, `src/Explore.Persistence/Repositories/UserLocationPrivacyErasureRepository.cs`, `LocationRepository.GetOwnedPrivateHomesForGlobalErasureAsync`, and focused Application/Persistence integration tests.
  - Result: exact persisted subject/tenant predicates select only owned Home data; Home labels/rooms are tombstoned; affected EventLocations become `NeedsPrivacyReview`; stable PII-free correction intents are idempotent; wrong-tenant substitution and unrelated locations fail closed.
  - Boundary: the authority workstream owns User fencing, cross-family orchestration, transaction/receipt outcome, provider settlement, replay, retention, and restore acceptance (see `docs/PRIVACY_ERASURE.md`).
  - Evidence: `GlobalLocationPrivacyErasureTests` (1,658 lines, real PostgreSQL 18) and `UserLocationPrivacyErasureRepositoryContractTests` prove owner-bounded cross-tenant queries, Home tombstoning, and non-Home exclusion.

- [x] **ELP-520 — Verify concrete correction dispatch, idempotency, retry, and dead-letter recovery**
  - Paths: `src/Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs`, `src/Explore.Infrastructure/Messaging/LocationPrivacyCorrectionDispatcher.cs`, `tests/Explore.Infrastructure.Tests/Infrastructure/LocationPrivacyCorrectionDispatcherTests.cs`, `tests/Event.Persistence.IntegrationTests/Repositories/LocationPrivacyCorrectionOutboxPostgreSqlTests.cs`.
  - Result: every new event type has a concrete route; duplicate delivery is safe; unknown/no-op routing cannot pass; dead letters are visible/reconcilable.
  - Evidence: `LocationPrivacyCorrectionDispatcher` routes `LocationPiiErased`, `LocationPrivacyCorrectionRequested`, and `location.privacy.corrected` events, validates PII-free payloads, invalidates HybridCache tags (`CacheTags.EventLocations`, `CacheTags.EventLocationsByEvent(eventId)`), and replans ATProto correction. Verified by `LocationPrivacyCorrectionDispatcherTests` (433 lines) and `LocationPrivacyCorrectionOutboxPostgreSqlTests` (245 lines).

- [x] **ELP-530 — Implement post-erasure EventLocation remediation workflow**
  - Paths: `src/Explore.Application/Features/EventLocations/Requests/Commands/ConfirmEventLocationRemediationCommand.cs`, `Handlers/Commands/ConfirmEventLocationRemediationCommandHandler.cs`, `Requests/Queries/GetEventLocationReviewQueueRequest.cs`, `Handlers/Queries/GetEventLocationReviewQueueRequestHandler.cs`, `Controllers/EventLocationController.cs`.
  - Result: affected associations need review; managers notified; unusable physical location blocks publication; explicit TBA allowed.
  - Evidence: `GetEventLocationReviewQueueRequest` returns only `NeedsPrivacyReview` associations; `ConfirmEventLocationRemediationCommand` clears privacy reviews only on verified active physical venues or explicit TBA, updates policy audit, and dispatches outbox correction in one transaction. Exposes `GET /api/events/{eventId}/locations/review` and `POST /api/events/{eventId}/locations/{eventLocationId}/remediation/confirm`. Verified by Application unit and API integration tests.

- [ ] **ELP-540 — Add privacy metrics and alerts**

## Phase 7: Blazor UX

- [ ] **ELP-600 — Migrate Blazor services and JSON serialization to generated purpose-specific contracts**
- [ ] **ELP-610 — Add EventLocation management editor with governance-aware controls**
- [ ] **ELP-620 — Implement Home owner consent and transfer UX**
- [ ] **ELP-630 — Implement public and attendee disclosure states**
- [ ] **ELP-640 — Add manager privacy-review dashboard and remediation actions**
- [ ] **ELP-650 — Remove overpromising private-address copy and sanitize JSON-LD**
- [ ] **ELP-660 — Complete localization, accessibility, responsive, RTL, and visual QA**

## Phase 8: Outbound Surfaces, Discovery, and Documentation

- [ ] **ELP-700 — Prove shared projection convergence and remove remaining bypasses**
- [ ] **ELP-715 — Audit email, notification, webhook, export, ticket, search, API-key, print, and report surfaces**
- [x] **ELP-720 — Migrate MCP/AI/federation/PDS surfaces and correction behavior**
  - Paths: `src/Explore.API/Mcp/EventManagementMcp*.cs`, `src/Explore.API/BackgroundServices/PdsSyncWorker.cs`, `src/Explore.Infrastructure/Services/Federation/PdsService.cs`, AI disclosure matrix/registry docs/tests.
  - Result: sanitized location still passes through `IAiContextGateway`; policy tightening/erasure emits PII-free idempotent correction.
  - Evidence: public MCP program/session adapters consume ELP-320 `EventLocationPublicDto` projections, batch flattened fields through `IAiContextGateway`, require exact allow-only identity/cardinality/value parity, and serialize bounded nested EventLocation descriptors; raw `LocationPii` classifications remain Restricted. ATProto snapshot factory builds locations from batched public disclosure results; correction dispatcher validates PII-free envelopes and uses correction message UUIDv7 as replay-safe planning key. Verified by `EventLocationPrivacyMcpContractTests`, `EventLocationPrivacyApiContractTests`, and `LocationPrivacyCorrectionDispatcherTests`.

- [x] **ELP-730 — Enforce PostGIS/discovery separation and erasure behavior**
  - Paths: Home Discovery docs and future discovery entity/service only if already in implementation scope; tests.
  - Result: current implementation records architecture/source absence proof because no `LocationDiscoveryPoint` store exists; if one enters scope later, prove no PII auto-copy, Private Home no point by default, transactional erasure cleanup, EventLocation/occurrence server-side joins, and no exact client catalog.
  - Evidence: production source contains no `LocationDiscoveryPoint`, spatial table/index, `ST_DWithin`/`ST_Distance`, geography point mapping, NetTopologySuite, or PostGIS dependency. Home Discovery uses explicit tenant-governed coarse areas (`PublicDiscoveryAreaDto`) whose optional centroids are limited to two decimal places; internal Location IDs are used only as server-side event filters and omitted from public area DTOs with distance fields null. Architecture absence verified.

- [ ] **ELP-740 — Update canonical architecture/security/API/domain/privacy/federation/testing docs**

## Phase 9: Final QA and Cleanup

- [ ] **ELP-800 — Verify migration/backfill/rollback and production-like PostgreSQL data shapes**
- [ ] **ELP-810 — Run adversarial privacy, auth, tenant, cache, and outbox matrix**
- [ ] **ELP-820 — Run per-project automated suites and Release build**
- [ ] **ELP-830 — Run OpenAPI/NSwag/contract cleanliness and browser visual/accessibility QA**
- [ ] **ELP-840 — Final repository and dev-doc review**

## Mandatory Acceptance Matrix

| Acceptance case | Primary task | Primary automated owner |
|---|---|---|
| Unknown legacy becomes Unclassified, never Public; PII presence maps only to Active/NotProvided | ELP-230B | `EventLocationBackfillTests` in Persistence Integration |
| Active Home owner valid; non-erased ownerless invalid; Erased ownerless/PII-less valid; resurrection rejected | ELP-120 | `LocationPrivacyLifecycleTests` in Domain Unit |
| Person/household venue and room labels/descriptions are never publicly disclosed | ELP-130 / ELP-310 | `EventLocationDisclosureEvaluatorTests` and `EventLocationDisclosureContractTests` |
| Same physical Location has independent per-event policies; TBA/location XOR holds | ELP-125 / ELP-150 | `EventLocationTests` in Domain Unit |
| Public contract exposes EventLocationId, not unrestricted LocationId | ELP-300 / ELP-405 | `EventLocationControllerTests` in API Integration |
| Event/Day/SessionSelection coverage is exact | ELP-225 | `EventLocationRegistrationAccessServiceTests` in Application Unit |
| Pending/waitlisted broad only; cancelled/revoked/deleted deny; null resolved by mode | ELP-200 / ELP-225 | `EventLocationRegistrationAccessServiceTests` in Application Unit |
| Typed platform adapter tombstones owned Home labels/rooms and marks only affected EventLocations | ELP-515 | `GlobalLocationPrivacyErasureTests` in Persistence Integration |
| Public endpoint remains public-only with auth cookie | ELP-400 / ELP-405 | `EventLocationControllerTests` in API Integration |
| Attendee/management are private/no-store | ELP-400 / ELP-405 | `EventLocationControllerTests` in API Integration |
| Tightened policy/governance defeats stale cache | ELP-340 / ELP-810 | `EventLocationGovernanceTests` in API Integration |
| Public calendar public-only; attendee calendar authorized/no-store | ELP-440 | `EventLocationPrivacyApiContractTests` in API Integration |
| Email/webhook/export/ticket/search/report cannot bypass authority | ELP-715 | Focused test beside every ELP-020 inventory owner |
| Concrete correction dispatcher is idempotent/retryable/dead-letter visible | ELP-520 | `LocationPrivacyCorrectionDispatcherTests` in Infrastructure plus PostgreSQL outbox tests |
| Discovery data is absent today or never auto-created from PII and erases transactionally | ELP-730 | Architecture absence proof or `LocationDiscoveryPrivacyTests` in Persistence Integration |
| Batch projection stays within query/auth count budget | ELP-315 | `EventLocationDisclosureBatchTests` in Persistence Integration |
| Server time controls reveal and cannot bypass entitlement | ELP-130 / ELP-310 / ELP-340 | `EventLocationDisclosureEvaluatorTests` in Application Unit |
| Private Home safe default is generic public label plus ConfirmedParticipant | ELP-110 / ELP-310 | `EventLocationDisclosureEvaluatorTests` in Application Unit |

## Implementation Completion Evidence

For every checked task, append to context:

- exact files changed;
- failing test added first;
- command and result;
- migration/contract/cache/security implications;
- unresolved risk or `none`;
- next task ID.

Do not check a task because code was written. Check it only when its result and verification evidence exist.
