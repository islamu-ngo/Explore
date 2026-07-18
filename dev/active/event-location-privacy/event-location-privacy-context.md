<!-- ABOUTME: Durable handoff context for the CTO-amended event-location privacy implementation workstream. -->
<!-- ABOUTME: Records verified source reality, locked decisions, rollout gates, blockers, and the exact next implementation step. -->

# Event Location Privacy Context

**Status:** Stage A and W1-W5 complete; W6 ELP-230A implementation present with disposable PostgreSQL verification pending
**Last Updated:** 2026-07-18 Europe/Brussels
**Plan:** `dev/active/event-location-privacy/event-location-privacy-plan.md`  
**Tasks:** `dev/active/event-location-privacy/event-location-privacy-tasks.md`

## SESSION PROGRESS

### Completed

- Classified the work as a cross-cutting fallback contract spanning Domain, Application/CQRS, Persistence/migrations, API/HAL/OpenAPI, authorization, Blazor, outbox, federation, operations, and privacy erasure.
- Ran the pre-planning Release build: 0 errors and 2,776 pre-existing warnings.
- Traced current Location/LocationPii, room, event/session/program/calendar/JSON-LD, registration-intent, account deletion, tenant-filter, cache, outbox, generated-client, MCP/federation, and Blazor flows.
- Confirmed the current anonymous Location API and generic DTOs expose exact or identifying location data.
- Confirmed `LocationRoom.Name` and `Description` are durable fields outside `LocationPii`.
- Confirmed registration intents support Event, Day, and SessionSelection scopes.
- Confirmed global User identity and tenant membership (`TenantUser` / `TenantUserProfile`) are different concepts.
- Incorporated Senior CTO feedback into the canonical first-class `EventLocation` architecture.
- Replaced the prior `LocationClassification`, side-table disclosure policy, session-row entitlement, tenant-local erasure, Public legacy backfill, and post-commit correction-message decisions.
- Re-audited the live source and corrected the rollout: Stage A minimization is immediate, `EventLocation.LocationId` is nullable only for explicit TBA, internal physical scheduling IDs remain for database integrity, legacy privacy state is backfilled from PII presence, migration stages are operator-selected, and restore replay uses a separately retained PostgreSQL erasure-authority database outside the application restore set.
- Corrected task ownership and execution order: ELP-420 is split into additive/final generation, ELP-505 and ELP-515 are one transaction lane, ELP-230C runs after every consumer migration, and infrastructure plus manual browser verification are mandatory.
- Completed ELP-000/005 on 2026-07-16 and re-synchronized ELP-000 after authority-protocol review: the three Event Location Privacy documents now consistently require authority-first append in a separate retained database followed by the application transaction; the three pre-existing Home Discovery diffs were hunk-preserved and stale generic-location safety claims were removed. The authority contradiction/completeness grep passed, `DocumentationQualityTests` passed 4/4, and the owned three-file `git diff --check` passed.
- Completed ELP-020/030/040/070 on 2026-07-16: every outbound surface has an owner/purpose/field/cache/correction/test contract; current OpenAPI/HAL/NSwag/EF artifacts and deterministic commands are hash-baselined without rewriting protected generated output; governance merge/failure/audit/cache semantics are field-complete; and global deletion is explicitly isolated from tenant membership removal. Path/table/key/checkbox assertions passed, `DocumentationQualityTests` passed 4/4, recorded hashes were rechecked unchanged, and the owned three-file `git diff --check` passed.
- Completed ELP-010/015/400 on 2026-07-16. Generic Location/room reads are authenticated, resource-authorized, `private, no-store`, and removed from output caching. Anonymous session, group, agenda, program, calendar, JSON-LD, filter, HAL, and MCP surfaces now omit physical IDs, identifying venue/room values, address/postcode/coordinates, and location-bearing warnings; an authentication cookie cannot enrich public output. Public child routes require a Published+Public parent and enforce child/day publication and scheduling eligibility. Separate resource-authorized management routes preserve draft editing while returning only locations/rooms already associated with that event. The management picker is intentionally bounded to those associations: first/new venue selection remains fail-closed for non-admins until ELP-405/610 introduce EventLocation management.
- Completed ELP-200 on 2026-07-16. `ApprovalStatus` now has stable `Cancelled=5`/`CANCELLED` and `Revoked=6`/`REVOKED` values. Null approval is resolved from registration mode; Pending and Approved consume capacity, terminal transitions release it, moving a registration reserves/releases atomically, and a full destination waitlists. Child cancellation persists `Cancelled`; remaining live children keep the parent live, while the last live child synchronizes the parent terminal state. PATCH cannot reassign event, intent, or user identity. Authorization enrichment loads a persisted tenant-safe ownership snapshot so attendees can cancel only their own registration, never PATCH; the serializable cancellation transaction revalidates the snapshot to close the authorization/use race while organizer/admin paths remain available.
- Completed the ELP-060/100/110 lookup slice on 2026-07-16. Stable integer/master-code entities, enum companions, EF configurations, and DbSets now exist for LocationKind (`UNCLASSIFIED`, `COMMERCIAL_VENUE`, `PUBLIC_SPACE`, `COMMUNITY_VENUE`, `PRIVATE_HOME`), LocationPrivacyState (`NOT_PROVIDED`, `ACTIVE`, `ERASED`), and LocationDisclosureAudience (`NEVER`, `ANY_CURRENT_REGISTRANT`, `CONFIRMED_PARTICIPANT`). The repair seeder is idempotent, but global startup invocation is intentionally deferred until ELP-230A creates the tables; that migration must activate it immediately. The ELP-120 aggregate lifecycle now consumes these stable lookup identities.
- Completed ELP-120/125 on 2026-07-16. `Location` now owns optional PII, consent-backed Private Home ownership, irreversible erasure metadata, identifying venue/room tombstones, and resurrection guards. The UUIDv7 `EventLocation` aggregate owns immutable tenant/event identity, physical-location XOR explicit TBA, fail-closed policy defaults, publication readiness, policy version/audit metadata, concurrency, and terminal soft deletion. All four scheduling carriers now carry authoritative `EventLocationId` while retaining server-derived physical keys for later database consistency constraints. Domain EventLocationPrivacy passed 50/50; the Domain Release build and architecture/hygiene/naming gates were green, and independent re-review found no residual high/medium issue.
- Completed ELP-130/140/150/210/300 on 2026-07-16. A single executable 16-field contract classifies baseline, contextual, management-only, exact, derivative, and operational-secret fields for Public/Attendee/Management purposes; derived values require source authority and timezone remains unavailable pending explicit policy. Purpose-specific requests, results, and DTO factories reject contradictory disclosure states; public output exposes `EventLocationId`, never physical `LocationId`, and Private Homes permit only `Private venue`. Immutable typed policy/exact-read audits, authority intents, and replay checkpoints are structurally PII-free. The pure registration-access resolver maps scope/lifecycle/mode facts to a sealed fail-closed entitlement, including Approved-as-Confirmed, broad-only Pending/Waitlisted, and terminal/deleted/expired denial. Registration access passed 42/42, disclosure contracts 17/17, Domain EventLocationPrivacy 62/62, and final security/quality reviews passed with no high/medium finding.
- Completed ELP-240/260 on 2026-07-16. Entity-returning repositories provide tracked mutation, bounded ordered no-tracking reads, tenant/soft-delete filters, initial `0→1` and contiguous aggregate-bound policy audit persistence, exact-read audit persistence, local replay checkpoints, and stable `concurrent_update` handling. SaveChanges validation fails closed on tenant/event/location/room/parent-session carrier mismatches; ELP-230A still owns the corresponding database constraints/triggers. The separate PostgreSQL erasure authority uses fixed-search-path `SECURITY DEFINER` append/read functions, a dedicated NOLOGIN owner, runtime execute-only grants, a transactional globally serialized counter, RFC-variant UUIDv7 validation, normalized idempotency, mismatch rejection, server UTC metadata, and bounded sequence reads. Root verification passed Domain EventLocationPrivacy 63/63, persistence repository 12/12, relational model 1/1, authority PostgreSQL 16/16, and Clean Architecture 15/15; final independent persistence and authority reviews passed with no high/medium findings.
- Canonical Stage-A OpenAPI/inventory/client generation was retained as the narrow compatibility exception needed for managed editors: current SHA-256 values are `71313ca44c33e137d84117e0c7fde200a0cbf877774f87f3f23de254e2bea33c` (OpenAPI), `dba2bbbe1792f512ac6472fafbe037a6e262edc5f28cbac0d1263306679a4785` (inventory), and `d263f0ce4271898f89f2f6a7adb54f58b966175349f97b87ad4931a20c2e9687` (generated client). The prior ELP-030 hashes remain below for byte comparison; ELP-420A/B remain open.
- Resumed ELP-230A verification on 2026-07-18 without touching the concurrent email-responsibility workstream. Committed source already contains `20260716132239_AddEventLocationPrivacyExpand`, `EventLocationPrivacyMigrationStage`, both migration hosts, global `SeedLocationPrivacyLookupsAsync` activation, and four PostgreSQL migration-stage tests. `dotnet ef migrations script 20260715172404_AddTypedWebhookOwnership 20260716132239_AddEventLocationPrivacyExpand ... --idempotent --no-build` generated 783 lines successfully; review found migration-local lookup inserts before dependent tables, additive nullable carrier columns, constant fail-closed lookup defaults, filtered uniqueness, tenant-safe foreign keys, UUIDv7/check/tombstone/append-only/carrier triggers, and a pre-activation `Down`. `dotnet ef migrations has-pending-model-changes ... --no-build` reported no changes. `schemas/islamu-event.md` now documents the ELP expand schema while preserving concurrent ATProto and email hunks.
- ELP-230A PostgreSQL execution is not yet green. The focused four-test command compiled the Persistence test project, then all four tests failed before test logic because Testcontainers could not connect to Docker. Starting the local `docker-desktop` user service was attempted; the backend exited successfully without leaving `~/.docker/desktop/docker.sock`. The architecture suite ran from the existing Release binary with 252 passed, one skipped, and two unrelated failures in the concurrent email/controller and authentication-token validator workstreams; a fresh architecture build is independently blocked by the unrelated missing `ApiNotFoundProblemDescriptor` symbol.

### In Progress

- W6: finish ELP-230A disposable PostgreSQL verification, then ELP-250 and ELP-500; ELP-310 may proceed independently. Stage-A browser evidence remains blocked until the expand schema is proven against PostgreSQL.

### Next

1. Re-run the four `EventLocationMigrationStageTests` after Docker is available; do not check ELP-230A complete until fresh expand, legacy upgrade/Down, selector rejection, and adversarial triggers all execute.
2. Implement only the OwnerUserId + Private Home tenant-filter bypass in ELP-250, then land ELP-500 adversarial erasure tests; implement the pure ELP-310 evaluator independently.
3. Resume Stage-A visual capture after the expand schema is installed, and keep first/new venue selection fail-closed for non-admins until ELP-405/610 provide EventLocation-scoped management and HAL affordances.

### Blockers

- ELP-230A disposable PostgreSQL execution is blocked by the unavailable Docker/Testcontainers endpoint; static SQL generation and model parity are green but do not replace the required database evidence.
- Home Discovery may continue only through its dedicated coarse area DTO; it must never consume generic LocationListDto or treat ShowCoordinates as discovery-index consent.
- Public or attendee disclosure activation is blocked until migration ELP-230B proves zero missing/orphan/duplicate/tenant-mismatch EventLocation data; ELP-230C remains the final contraction after every consumer has migrated.
- External correction is blocked from release until every new outbox event type has a concrete route in `CompositeOutboxMessageDispatcher`, idempotency tests, and dead-letter operations.

## Quick Resume

The target is not a field mask on `Location`. Physical place data remains in `Location` / optional `LocationPii`; a canonical first-class `EventLocation` owns per-event disclosure. `LocationKind` describes the place but never authorizes disclosure. `LocationPrivacyState` distinguishes `NotProvided`, `Active`, and irreversible `Erased`. Public, attendee, and management routes are separate. Attendee entitlement is resolved from registration intent scope/lifecycle. Global account deletion first appends an immutable intent to the separate erasure authority, then erases owned Private Homes and inserts the local checkpoint/correction outbox in one application-database transaction.

Stage A is active: public routes are redacted, eligibility-gated, and principal-invariant; generic physical reads and event management reads are authorized and `private, no-store`; management choices are bounded to locations already associated with the event. Domain/Application contracts and tenant-safe repositories are implemented, while public/attendee disclosure activation remains blocked on the migration/backfill and evaluator waves. Continue with ELP-230A, ELP-250, ELP-500, and independent ELP-310. Do not revive the obsolete `EventLocationDisclosurePolicy` design or restore a tenant-wide non-admin venue picker.

## Verified Source Anchors

| Concern | Verified path | Current fact |
|---|---|---|
| Physical location | `src/Explore.Domain/Location.cs` | Owns kind/state/owner, optional PII, explicit consent transfer, irreversible Private Home erasure metadata, and identifying-label/room tombstones. |
| Exact PII | `src/Explore.Domain/LocationPii.cs` | Optional shared-PK street, postcode, latitude, and longitude; attaching it to an Erased Location is rejected. |
| Room data | `src/Explore.Domain/LocationRoom.cs` | Name/description remain durable but privacy tombstoning is irreversible and keyed by the persisted room ID. |
| Event-local authority | `src/Explore.Domain/EventLocation.cs` | UUIDv7 aggregate enforces physical XOR TBA, fail-closed policy v1, publication readiness, versioned audit transitions, and terminal detach. |
| EF mapping | `src/Explore.Persistence/Configurations/Entities/EventLocationConfiguration.cs` and related privacy configurations | Tenant/soft-delete filters, bounded repository access, audit/checkpoint mappings, and migration-ready carrier indexes exist; ELP-230A owns the physical database constraints/triggers. |
| Registration authority fact | `src/Explore.Application/Services/EventLocationRegistrationAccessService.cs` | Pure resolver emits a sealed immutable access fact from validated intent/scope/lifecycle/placement facts; ELP-225 still owns persistence-backed coverage loading. |
| Disclosure contracts | `src/Explore.Application/DTOs/Location/EventLocationDisclosureContract.cs` and purpose-specific request/result/DTO files | One 16-field vector authority and constrained factories prevent public physical-ID or operational-secret disclosure and contradictory states. |
| Retained erasure authority | `src/Explore.Infrastructure/Privacy/ErasureAuthority/` | Separate PostgreSQL schema/client implements transactional monotonic append, UUIDv7 idempotency, bounded reads, and execute-only runtime access; startup replay remains ELP-525. |
| Location repository | `src/Explore.Persistence/Repositories/LocationRepository.cs` | Generic reads include PII; `ForgetPiiAsync` hard-deletes but has no account-erasure caller. |
| Public DTOs | `src/Explore.Application/DTOs/Location/LocationDto.cs` and `LocationListDto.cs` | Exact/identifying data is conflated with discovery/management contracts; list includes Address. |
| Generic Location API | `src/Explore.API/Controllers/LocationController.cs` and `LocationRoomController.cs` | Reads are authenticated, resource-authorized, `private, no-store`, and not output-cached; public event projections do not dereference these management contracts. |
| Public sessions/agenda | EventSession, EventSessionGroup, EventAgendaItem, and EventSessionAgendaItem public handlers | Physical fields are nulled/omitted and child results require public parent and child/day eligibility; management uses separate authorized event-scoped requests. |
| Public program | `src/Explore.Application/Features/EventPrograms/Handlers/Queries/GetEventProgramSummaryRequestHandler.cs` | Uses public-only groups/items and emits no location names, rooms, physical IDs, or location-readiness warnings. |
| Public calendar | `src/Explore.Application/Features/Events/Handlers/Queries/GetEventCalendarExportRequestHandler.cs` | Public free-text location is suppressed pending EventLocation disclosure policy. |
| Browser/JSON-LD | `src/Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` | Public structured data is redacted and the unconditional private-address promise is removed. |
| Registration intent | `src/Explore.Domain/EventRegistrationIntent.cs` | Carries RegistrationScopeId and day-specific selection data. |
| Registration scope | `src/Explore.Domain/Enums/RegistrationScopeEnum.cs` | Event, Day, SessionSelection. |
| Policy rules | `src/Explore.Domain/Services/Registration/RegistrationPolicyRules.cs` | Maps allowed scope combinations for event registration modes. |
| Existing deletion | `src/Explore.Application/Features/Users/Handlers/Commands/DeleteUserCommandHandler.cs` | Erases UserPii/ActorPii but not owned Home locations. |
| Tenant membership | `docs/MULTI_TENANCY.md` | Tenant participation is separate from global User identity. |
| Outbox processing | `src/Explore.API/BackgroundServices/OutboxProcessor.cs` | At-least-once polling, retry, dead-letter lifecycle. |
| Concrete routing | `src/Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs` and `src/Explore.API/BackgroundServices/OutboxProcessor.cs` | Unknown/non-managed reconciliation currently returns successfully, so the processor can mark a no-op recovery complete; location correction routes do not exist yet and must fail closed. |
| Dispatcher registration | `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs` | Composite dispatcher is the registered `IOutboxMessageDispatcher`. |
| Home Discovery overlap | `dev/active/home-discovery-experience/` | ELP-005 now records that generic LocationListDto exposes Address/identifying fields and is unsafe; Home Discovery remains restricted to its dedicated coarse area DTO. |
| AI disclosure overlap | `dev/active/ai-context-disclosure-policy/` | AI/MCP fields must still pass through `IAiContextGateway` and its fail-closed registry/matrix. |

## Superseded Decisions

Do not implement any of these former choices:

- `LocationClassification` with legacy backfill to Public.
- Commerce/Public/Community/Home codes without Unclassified.
- `EventLocationDisclosurePolicy` as a side table while raw event references keep physical LocationId.
- Registered/Approved entitlement based on EventRegistration row existence or nullable status.
- Current-tenant-only Home erasure.
- Clearing owner/PII without an irreversible durable privacy-state tombstone.
- Creating correction outbox messages after commit.
- Generic public Location detail with minimized variants under the same route/cache identity.
- Public default venue name/city for unreviewed legacy locations.
- A single resolver that mixes I/O, authorization, and field policy.

## Locked Durable Decisions

### Contextual sensitivity

- Country/timezone are normally non-sensitive but still purpose/policy controlled.
- City, venue name, and room name are contextual and default hidden for Private Home.
- Room description is management-only in v1.
- Street, postcode, coordinates, formatted address, map URL, and geohash are exact-sensitive.
- Access instructions/door codes are restricted operational secrets and never general public.
- Private Home public label is `Private venue`; identifying durable labels and room content are tombstoned during erasure.

### Location lifecycle

- `LocationKind` codes: `UNCLASSIFIED`, `COMMERCIAL_VENUE`, `PUBLIC_SPACE`, `COMMUNITY_VENUE`, `PRIVATE_HOME`.
- `LocationKind` never grants disclosure.
- `LocationPrivacyState` codes: `NOT_PROVIDED`, `ACTIVE`, `ERASED`.
- Active Private Home with PII requires an explicit owner; non-Home forbids owner.
- Default Home owner is current user; another owner/transfer requires explicit consent workflow.
- Erased means no PII, no owner, durable erasure timestamp/reason, identifying labels/rooms tombstoned, and PII resurrection rejected forever on that Location.
- Replacement address creates a new Location and consent decision.

### Canonical EventLocation

- `EventLocation` is the event-to-place aggregate, not a policy side table. `LocationId` is nullable only for explicit TBA; a database XOR constraint requires exactly one of TBA or a physical location.
- It owns field selection, full-details audience, optional server-time reveal, review status, policy version, concurrency, soft-delete, and audit.
- Public contracts expose EventLocationId and not unrestricted physical LocationId.
- EventSession, EventSessionGroup, EventAgendaItem, and EventSessionAgendaItem retain internal physical `LocationId` columns where required for room-containment composite keys and the existing session GiST overlap exclusion. Their `EventLocationId` and physical `LocationId` must match through tenant/event/location consistency constraints; the physical IDs never become public authority.
- Server auto-creates fail-closed associations.
- Final detach soft-deletes; reattach creates a fresh fail-closed association.
- The same physical Location can have independent policies on different events.
- Room disclosure is evaluated through EventLocation even when `LocationRoomId` remains physical.
- `EventLocation.IsToBeAnnounced` is an explicit organizer choice that suppresses every physical-location field; erasure or missing PII never sets it implicitly.

### Legacy lifecycle backfill

- Every legacy Location remains `UNCLASSIFIED`.
- A legacy row with `LocationPii` backfills to `ACTIVE`; a row without `LocationPii` backfills to `NOT_PROVIDED`.
- Legacy data is never inferred as `ERASED`, and owner is never inferred from `CreatedBy`.

### Legacy defaults

- Existing locations backfill to Unclassified, never Public.
- Legacy EventLocations default to country only; venue/city/room/street/postcode/coordinates hidden, audience Never, `NeedsPrivacyReview=true`.
- City continuity requires a separately recorded decision; it is not the default.
- Metrics and an admin review queue track unresolved Unclassified/NeedsPrivacyReview records.

### Registration intent access

- Backend audience values are Never, AnyCurrentRegistrant, ConfirmedParticipant.
- Event scope covers all eligible event locations.
- Day scope covers only eligible session/item locations on the selected day.
- SessionSelection covers only selected sessions’ EventLocations.
- Private Home safe default is ConfirmedParticipant.

The following ELP-200/210 tables are authoritative and synchronized with plan Section 7. `Approved` is the current persisted approval term; the disclosure resolver treats it as the `Confirmed`-equivalent effective state.

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
| Soft-deleted or expired intent/registration | Non-live | Deny. |

ELP-200 added stable persisted `Cancelled=5`/`CANCELLED` and `Revoked=6`/`REVOKED` values. Null status is resolved by mode: Open becomes Approved, ApprovalRequired becomes Pending, and InviteOnly/Closed deny without separate authority. Pending/Approved are capacity-bearing; Cancelled/Revoked/Rejected are terminal and release capacity. Cancelling one child removes only that child coverage while remaining live children keep the parent live; cancelling the last live child synchronizes the parent to a terminal state. ELP-210 now supplies the immutable pure effective-state result; attendee location authority remains blocked on ELP-225 persistence-backed placement coverage plus the remaining disclosure/governance/route gates.

### Batch disclosure

- `EventLocationDisclosureService.ResolveManyAsync(...)` owns bounded I/O and batched manager authorization.
- `EventLocationDisclosureEvaluator` is pure and deterministic.
- Results are immutable and keyed by EventLocationId.
- Initial budget is one bounded query per association/location+PII/room/registration+coverage/governance set and one batched manager authorization call, with no per-row calls.

### Route and cache split

- Public: `GET /api/events/{eventId}/locations`, anonymous, always public-only, no shared cache in v1.
- Attendee: `GET /api/events/{eventId}/locations/my-access`, authorized, registration-aware, private/no-store.
- Management: `GET /api/events/{eventId}/locations/{eventLocationId}/management`, authorized/resource-protected, private/no-store.
- Management mutation: `PUT /api/events/{eventId}/locations/{eventLocationId}/disclosure`, authorized/resource-protected, private/no-store.
- Remove generic anonymous exact Location detail.
- Public output must be identical with or without an auth cookie.

### Governance

- Scope ownership is fixed: `SystemSetting` is the instance ceiling, `TenantSetting` is a tenant-only restriction, and user settings never participate. Instance writes extend `UpdateInstanceGovernanceSettingsCommandHandler`; tenant writes use the authorized `SettingsController`/setting command path and `ISettingMutationLock`.
- Keys are registered under `GovernanceSettingKeys.LocationPrivacy` and `LocationPrivacySettingDefinitions`: `location_privacy.allow_home_locations`, `allow_public_exact_address`, `allow_public_coordinates`, `minimum_home_audience`, and `default_reveal_offset`.
- The three booleans merge with AND (`false` wins). `minimum_home_audience` uses `NEVER` > `CONFIRMED_PARTICIPANT` > `ANY_CURRENT_REGISTRANT`. `default_reveal_offset` uses the later non-negative ISO-8601 duration, bounded `PT0S` through `P30D`.
- Missing/invalid instance values fail to `false`, `NEVER`, and `P30D`; missing tenant values inherit. Malformed JSON, unknown audience, invalid duration, duplicate conflict, or repository failure denies disclosure with a bounded reason code. A tenant widening attempt is validation failure, not normalization.
- `ILocationPrivacyGovernanceService` is the only location resolver. Existing `TenantPolicySettingService.Resolve*` behavior is not reused because it selects an override and falls back permissively instead of composing two ceilings.
- Accepted writes await `SettingChangedNotification`; `SettingAuditLogHandler` records key/scope/tenant/actor/old/new metadata and `SettingCacheInvalidationHandler` evicts hierarchical settings. Tightening also versions/marks affected EventLocations, writes correction outbox rows transactionally, then evicts planned EventLocation tenant/event/entity cache tags. Public v1 location responses are not shared-cached.
- Reveal uses server UTC and remains subordinate to registration entitlement.

## Global Erasure Control Flow

1. Require the independently retained PostgreSQL erasure-authority database to be available; otherwise fail before any application mutation.
2. Append an immutable PII-free intent first. The UUIDv7 intent ID is the idempotency key; the authority assigns a monotonic sequence. The record contains only opaque owner/location identifiers, reason code, and server UTC metadata—never address or user-entered text.
3. Retry after an ambiguous acknowledgement with the same intent ID; the authority returns the same sequence and never creates a duplicate intent.
4. In one application-database transaction, execute the named tenant-filter bypass bounded by OwnerUserId, lock all current/former-tenant owned Private Homes, erase/tombstone PII and derived data, update EventLocations, complete User/Actor erasure, insert the local `(authority sequence, intent ID)` replay checkpoint, and insert PII-free correction outbox rows.
5. Report deletion success only after the application transaction commits. Cache eviction remains best effort after commit; background workers dispatch the durable outbox.

A crash after the authority append but before application commit is safe: the immutable intent remains pending, and retry/startup replay idempotently applies it. Application rollback leaves PII/local checkpoint/outbox unchanged but never removes the authority intent. A crash after application commit finds both the checkpoint and outbox durable. If volume proves one application transaction unsafe, stop for approval before introducing a durable saga.

Tenant membership removal changes TenantUser/TenantUserProfile only and never invokes this global flow.

### Deletion ownership and negative boundary

| Operation | Exact owner | Must change | Must never change | Test/architecture owner |
|---|---|---|---|---|
| Global account deletion | `DeleteUserCommandHandler` + authority client + `IGlobalLocationPrivacyErasureRepository` | append authority intent first; then update all OwnerUserId Private Homes across current/former tenants, UserPii/tokens/ActorPii, review state, local checkpoint, and correction outbox in one app-DB transaction | unrelated owners' locations; unbounded tenant-filter bypass; claiming atomicity across databases | `DeleteUserCommandHandlerTests`, `GlobalLocationPrivacyErasureTests` |
| Tenant membership removal | planned `RemoveTenantMembershipCommandHandler` + tenant-filtered TenantUser/Profile repositories | the selected tenant's `TenantUser`, `TenantUserProfile`, and active role grants only | global User/UserPii, Location/LocationPii/EventLocation, other tenants, privacy erasure/outbox | `RemoveTenantMembershipCommandHandlerTests` |
| Dependency boundary | `EventLocationPrivacyArchitectureTests` | permit the global-erasure repository only in the global User deletion lane | any `Features/TenantUsers` handler referencing `DeleteUserCommand`, `DeleteUserCommandHandler`, or `IGlobalLocationPrivacyErasureRepository` | architecture suite |

## Outbound Authority Inventory

The complete normative table, including field rules, is frozen in plan Section 11. This context index records every owner and release gate; absence means an architecture proof is required, not that a future producer may emit location.

| Surface | Concrete owner | Audience/purpose and allowed fields | Cache/retention | Correction | Target evidence |
|---|---|---|---|---|---|
| sessions/groups/program/agenda | EventSession query handlers, program/agenda handlers, `EventSessionMappingProfile` | public/attendee/management selected fields; public EventLocationId only | public no shared cache v1; private routes no-store | policy-version eviction | handler tests + `EventLocationOutboundProjectionTests` |
| JSON-LD | `EventDetail.razor.cs` | anonymous public-selected label/city/country only | policy-version page cache | purge and rerender | `EventLocationJsonLdPrivacyTests` |
| public calendar | calendar export handler + `IcalNetEventCalendarFileBuilder` | public-selected only | no cache until version invalidation proven | invalidate; external-import warning | `EventCalendarPrivacyTests` |
| attendee calendar | planned attendee handler/controller + same builder | requester-entitled selected fields | private/no-store | correct next fetch/provider outbox | `EventCalendarPrivacyTests` |
| email/reminders | `EventLifecycleEmailOutboxFactory`, `EmailDispatchProcessor` | no location today; future recipient-specific snapshot, no access instructions | durable outbox/external copy | PII-free follow-up or reissue; no recall claim | factory tests + `EventLifecycleEmailLocationPrivacyTests` |
| notifications/web push | fanout/orchestrator/web-push sender | no location today; future selected label only | recipient private | update/delete projection and refresh | `NotificationLocationPrivacyTests` |
| tickets/QR | no producer exists | opaque admission ID; rendered confirmed-participant selection only | private artifact | revoke/reissue | `EventLocationOutboundSurfaceAbsenceTests` |
| Svix/local webhooks | payload builder, registry, delivery pipeline | allow-listed EventLocationId/purpose-selected fields only | durable retention policy | versioned correction/erasure event, retry/dead letter | webhook builder/delivery + ELP-520 tests |
| CSV/JSON export | no event-location export; contact-share export unrelated | future authorized management selection only | private/no-store; retained copy | reissue/warn | absence architecture test/future owner test |
| search/index | Event/EventSession list/custom-property projections; no geo index | public coarse selection only | tenant/policy-version partition | delete/rebuild via correction intent | list/projection privacy tests |
| moderation/support | moderation queries/controllers; no dedicated location projection | authorized management selection; audited exceptional exact read | private/no-store | immediate view correction/provider outbox if used | moderation tests + absence proof |
| API-key consumers | `ApiKeyAuthenticationHandler` + purpose-specific controllers | key never upgrades public; explicit resource authority for management | route policy | invalidation/subscriber correction | API-key + EventLocation controller tests |
| print/PDF/admin reports | `EventReportsController`/providers are moderation-only; no location producer | future authorized management selection only | private/no-store; disclosed retention | regenerate/reissue | absence architecture test/future owner test |
| MCP/AI | EventManagement MCP tools/resources + `IAiContextGateway` | purpose-selected DTO then AI field ceiling | no shared cache; bounded telemetry only | next call/purge retained context | MCP + AI matrix tests |
| federation/PDS | `PdsSyncWorker`, `PdsService`, PDS outbox | federation-purpose selected fields | remote retained record | idempotent update/delete with retry/dead letter | PDS correction tests |
| Home Discovery/PostGIS | home query + `PublicDiscoveryAreaDto`; no point store | configured coarse area only | `PublicHomeDiscovery` coarse cache | future transactional delete/rebuild | Home tests + ELP-730 absence proof |

Public ICS is public-only. Attendee ICS is authenticated/private/no-store. Private Home data never enters stable public subscription URLs. Operators/users are warned that imported/exported third-party copies may survive correction.

## Backup and Restore Boundary

- Historical backups may contain erased PII until retention expiry; document the limit honestly.
- The PostgreSQL erasure authority is a separate independently retained and backed-up database outside the application database restore set. It stores immutable UUIDv7-idempotent PII-free intents with a monotonic sequence; the application database stores only its local replay checkpoint.
- On a fresh application database the checkpoint starts at sequence zero. Startup reads and idempotently applies every authority intent before traffic, then persists the checkpoint, purges caches/rebuilds indexes, and replays external corrections.
- Application logical or physical-cluster restore never overwrites or pretends to transactionally restore the authority. Restore the retained authority independently, verify continuity, then replay it over the restored application database.
- Authority unavailability fails closed for both deletion and startup. API, BFF proxying, MCP, outbox/workers, and readiness remain blocked until authority availability, sequence continuity, replay, and evidence queries succeed.

## Post-Erasure Remediation

- Affected EventLocations become NeedsPrivacyReview.
- Managers receive PII-free notifications and dashboard work.
- Publication is blocked when a required physical venue is unusable.
- Organizers can explicitly select `Location to be announced`.
- A neutral unavailable address is not treated as a complete remediation.

## Discovery Boundary

- `ShowCoordinates` never grants indexing.
- Discovery data is not auto-copied from LocationPii.
- Discovery point has separate precision, provenance, visibility, consent, and activation.
- Private Home has no point by default.
- Erasure deletes/deactivates derived points transactionally.
- Future proximity joins eligible occurrences through EventLocation.
- Public clients never download an exact catalog.
- Distance/nearby remains blocked until server-side occurrence calculation exists.

## Rollout Gates

- Stage A: immediate anonymous minimization and missing-policy fail-closed compatibility output, including Location/session/program/calendar/JSON-LD/MCP/filter surfaces; do not wait for EventLocation persistence.
- Stage B: additive schema and dual-write.
- Stage C: Unclassified/EventLocation idempotent backfill plus zero-gap verification.
- Stage D: policy-selected disclosure activation.

ELP-230A, ELP-230B, and ELP-230C are separate operator-selected deployment targets. While ELP migrations are pending, `Database:Migrations:EventLocationPrivacyStage=Expand|Backfill|Contract` is required and missing/invalid configuration fails startup. ELP-230C executes only after ELP-420A adoption and every API, Blazor, calendar, JSON-LD, outbound, AI, and federation consumer has moved to purpose-specific contracts; ELP-420B regenerates the final client afterward.

Stage A is deployed in source. Its temporary management routes are event-scoped and `private, no-store`; their picker returns only locations/rooms already referenced by that event, never the tenant-wide physical catalog. MCP public and management builders preserve non-physical content but structurally produce zero physical disclosures, and gateway validation requires exact ordered response cardinality/entity identity, successful decisions, and zero non-null governed fields. ELP-405 and ELP-610 remain open for the final EventLocation route/editor and safe first/new venue selection.

On failure, retain Stage A and additive schema, fix forward, rerun backfill, and never restore exact anonymous exposure. `Down` is valid only before contract activation and irreversible erasure.

## Generated Contract and Migration Baseline (ELP-030)

Captured at repository commit `76116048086d340f5129bcf5c376b0e01a66f4e5` on 2026-07-16. SHA-256 is over the current worktree bytes, not a regenerated copy.

| Artifact | SHA-256 | Capture state |
|---|---|---|
| `src/Explore.API/OpenApi/HalOpenApiSchemaCatalog.cs` | `3b0134472671829506f69479bc62428d7b9aeea8fa2113315a5af4dcc97737f6` | clean |
| `schemas/openapi_islamu-event.json` | `74bf41254556f14cb039316283eb8fb32c211dd06a353ffdd8177c3f6b9bc667` | protected pre-existing Home Discovery generated diff |
| `docs/API_CONTRACT_INVENTORY.md` | `16eb7747be2dc065d1085765630fe975beda0f94a01cbd5d95febcca66e35020` | protected pre-existing Home Discovery generated diff; 446 paths/601 operations |
| `src/Explore.Blazor.Client/nswag.json` | `b1fc2ee7fc07c32e2948ea793a8634351ce1a244a99375590837336668af65ef` | clean generation configuration |
| `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` | `9d5fb27115ebbcf01b8aa6c6ea0a0af68c0f4624b4edb8275524ecacb677ffe8` | protected pre-existing Home Discovery generated diff |
| `src/Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs` | `5e8b298c39941259c255de1b877140fd87f374ba05fc79693c8a31f7e591339d` | clean EF snapshot |
| migration head `20260715172404_AddTypedWebhookOwnership.cs` | `64ea06e74315158d01565979acb910de74b0fb004a9594d8fc036dfef0d78008` | clean; designer hash `553411c10bba1dde28b3791cdcd6e230ba4394447f90526fb876a2e33b95c774` |

Canonical deterministic regeneration, taken from `.github/workflows/openapi-contract.yml`, is:

```bash
dotnet restore --locked-mode
dotnet tool restore
dotnet build src/Explore.API/Explore.API.csproj --configuration Release --no-restore --verbosity minimal
dotnet build tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity minimal
dotnet run --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build -- --treenode-filter "/*/*/*/OpenApiDocument_*" --minimum-expected-tests 5 --no-progress --report-trx --report-trx-filename openapi-invariants.trx
dotnet run --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build -- --treenode-filter "/*/*/*/ApiContractInventory_Generate_WritesMarkdownToDocs" --minimum-expected-tests 1 --no-progress --report-trx --report-trx-filename api-contract-inventory.trx
dotnet build src/Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --no-restore --verbosity quiet
```

Repeat the API build, inventory generator, and Blazor build once, then compare hashes/diff for `schemas/openapi_islamu-event.json`, `docs/API_CONTRACT_INVENTORY.md`, and `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`; legacy `schemas/openapi.json` and `src/schemas/openapi_event-api.json` must remain absent. This capture intentionally did not run generators because those three artifacts already contain protected concurrent Home Discovery output; ELP-420A/B own reviewed regeneration after first recording these hashes.

Stage-A canonical generation was later required so the new managed routes could be consumed without hand-written client drift. The same workflow generated the following current byte identities while preserving the concurrent Home Discovery output:

| Stage-A artifact | SHA-256 |
|---|---|
| `schemas/openapi_islamu-event.json` | `71313ca44c33e137d84117e0c7fde200a0cbf877774f87f3f23de254e2bea33c` |
| `docs/API_CONTRACT_INVENTORY.md` | `dba2bbbe1792f512ac6472fafbe037a6e262edc5f28cbac0d1263306679a4785` |
| `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` | `d263f0ce4271898f89f2f6a7adb54f58b966175349f97b87ad4931a20c2e9687` |

## Validation Baseline

- Release build before planning: passed, 0 errors, 2,776 pre-existing warnings.
- Previous architecture-suite verification: 276 total, 275 passed, 1 documented skip, 0 failed.
- Stage-A managed/API ELP category: 19/19; native Cerbos: 461/461; language security: 3/3, handler 5/5, controller 2/2; full Application unit: 2,317/2,317; full Blazor: 1,702/1,702 executed with one not executed; API Release build: 7 projects, 0 errors/0 warnings; Blazor Release build: 1 project, 0 errors/0 warnings.
- Public sibling-route evidence: API ELP 11/11; Application ELP 18/18; session-agenda 6/6; public eligibility 2, program 6, agenda 6, session detail 3, other handlers 10, Persistence 5, and Clean Architecture 15/15. Public HAL principal invariance and output-cache removal are included in those focused suites.
- MCP evidence: ELP MCP at least 10, authenticated management reads at least 6, SDK at least 10, and API build 7 projects with 0 errors/0 warnings.
- ELP-200 evidence: `RegistrationApprovalStatusRules` at least 24, `RegistrationPolicyRules` exactly 30, create handler at least 18, validator at least 9, update at least 11, PostgreSQL EventRegistrationRepository at least 16, EventRegistrationIntentRepository at least 11, approval seeder at least 1, Persistence ELP 20, Domain ELP at least 30, and Clean Architecture at least 15.
- Own-cancellation evidence: Application 22/22, fallback authorization 7/7, Cerbos registration matrix 14 within 461, real Cerbos 2/2, PostgreSQL cancellation/race 8/8, architecture/parity 24/24, and full Release build 26 projects with 0 errors.
- Lookup evidence: Domain 2, Persistence 2, PostgreSQL startup/idempotency 1, Domain/Persistence builds with 0 errors, and Clean Architecture/Code Hygiene/Naming green. EF pending-model detection is expected until ELP-230A adds the migration.
- Lifecycle/aggregate evidence: Domain EventLocationPrivacy 50/50, Domain Release build 0 errors/0 warnings, Clean Architecture 15/15, Code Hygiene 4/4, Naming 10/10, and independent adversarial re-review PASS.
- Contract/access/audit evidence: Domain EventLocationPrivacy 62/62, registration access 42/42, disclosure contracts 17/17, Application Release build 0 errors/0 warnings, and final security/code-quality re-reviews PASS.
- Persistence/authority evidence: Domain EventLocationPrivacy 63/63, repository 12/12, relational model 1/1, retained-authority PostgreSQL 16/16, Clean Architecture 15/15, and final independent persistence/authority re-reviews PASS. ELP-230A database migration enforcement and ELP-525 startup replay remain open.
- Browser visual/accessibility evidence remains part of ELP-660/830 and is not claimed complete here.

## High-Risk Failure Modes

- Treating durable Home labels/rooms as non-PII.
- Backfilling legacy locations to a permissive kind or policy.
- Resolving attendee access from one registration row instead of intent coverage.
- Bypassing tenant filters without an OwnerUserId-bound privacy query.
- Persisting correction after commit or routing it to no-op/unknown dispatcher.
- Returning richer public output when a cookie is present.
- Restoring erased PII from backups before erasure replay.
- Letting coordinates imply discovery indexing.
- Leaving email/webhook/export/calendar paths outside the disclosure authority.
- Reattaching PII to an Erased Location.

## Handoff Contract

The implementation agent must keep this file current after every task. Record exact code paths changed, commands/results, migration state, new risks, policy decisions, and the next single task. Never mark a phase complete from code presence alone; attach runnable evidence. Preserve all unrelated dirty-worktree changes and stop for approval before changing irreversible erasure, global tenant scope, API exposure, or transactional outbox guarantees.
