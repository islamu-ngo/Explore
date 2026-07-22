<!-- ABOUTME: Decision-complete implementation plan for contextual, audience-scoped EventLocation disclosure. -->
<!-- ABOUTME: Defines the EventLocation model, safe rollout, correction integration, API/UI boundaries, and verification contract. -->

# Event Location Privacy Implementation Plan

**Status:** Approved EventLocation architecture; verified and remaining work is tracked in `event-location-privacy-tasks.md`; platform erasure ownership has moved to the authority workstream
**Last Updated:** 2026-07-22 Europe/Brussels
**Intent:** Cross-cutting fallback contract composed from `add-cqrs-handler`, `update-repository-query`, `add-ef-migration`, `add-get-endpoint`, `add-write-endpoint`, `openapi-contract-change`, `add-hal-link`, `cerbos-policy-change`, and `blazor-component-affordance`  
**Review:** Senior CTO amendments and repository re-audit incorporated; product changes follow the execution waves in Section 16
**External privacy-erasure boundary:** [`optional-retained-erasure-authority-plan.md`](../optional-retained-erasure-authority/optional-retained-erasure-authority-plan.md) is the sole owner of User erasure, authority topology, receipt/status, provider settlement, replay, retention, and restore. This plan owns only EventLocation disclosure and its typed correction/remediation integration.

## 1. Outcome

Deliver event-location disclosure that is independent of event visibility and safe for public, attendee, organizer, API-key, MCP, federation, notification, and export flows.

The system must:

- keep physical addresses in the existing `Location` / `LocationPii` boundary;
- classify locations with a non-authorizing `LocationKind`;
- distinguish never-provided PII from irreversibly erased PII with `LocationPrivacyState`;
- use first-class `EventLocation` associations as the only event-local disclosure authority;
- disclose fields according to event policy, governance restrictions, registration-intent coverage, manager authorization, and server time;
- contribute a typed, idempotent EventLocation disposition/correction adapter to the platform erasure workflow;
- persist EventLocation policy changes and correction intents in the same database transaction;
- expose public, attendee, and management representations through separate routes and cache policies;
- preserve auditability without copying address values into policy audit, logs, metrics, or outbox payloads.

Completed foundation on 2026-07-16: ELP-010/015/400 make known public Location/session/group/agenda/program/calendar/JSON-LD/filter/HAL/MCP paths fail closed, eligibility-gated, cache-safe, and principal-invariant. Separate authorized `private, no-store` management routes preserve draft editing but expose only locations/rooms already associated with the event; first/new venue selection stays fail-closed for non-admins until ELP-405/610. ELP-200 adds stable `Cancelled=5`/`CANCELLED` and `Revoked=6`/`REVOKED`, null-mode resolution, capacity-aware transitions, synchronized parent lifecycle, immutable registration identity, and own-cancellation authorization with transaction-time ownership revalidation. ELP-060/100/110 add the three normalized privacy lookup families; ELP-230A now creates them with migration-local inserts and activates their idempotent global repair seeder.

Completed W3-W5 on 2026-07-16: ELP-120/125 add optional PII, consent-backed Private Home ownership, irreversible label/room tombstones, and the UUIDv7 physical-XOR-TBA EventLocation aggregate with fail-closed policy/version/concurrency state and carrier references. ELP-130/140/150/210/300 provide the executable 16-field matrix, typed PII-free audits, explicit TBA publication behavior, immutable registration access, and purpose-specific constrained contracts. ELP-240 persists tenant-filtered bounded entity reads, tracked mutations, initial/contiguous policy audits, exact-read audits, and stable concurrency conflicts. Todo 7 independently confirmed ELP-230A and ELP-310 on 2026-07-19. Platform erasure evidence formerly recorded here is now inherited by the authority workstream.

ELP-230A is verified complete. The committed Expand migration, operator-selected migration ceiling, application and migration-service entrypoints, lookup seeder activation, and database guards passed five focused PostgreSQL migration-stage tests and the 40-test Persistence privacy category. Exact 783-line idempotent Expand SQL applied twice; fresh/legacy upgrade and pre-activation Down preserved legacy rows; unavailable `Contract` failed before traffic in both hosts; raw SQL guards rejected audit deletion, PII resurrection, tombstone restoration, carrier mismatches, and referenced detach. Current repository-wide model parity is explicitly not claimed because concurrent shared model/snapshot work makes `has-pending-model-changes` exit 1. Backfill is separately verified under ELP-230B; Contract and disclosure activation remain open.

ELP-310 is verified complete. The pure synchronous evaluator passed its 72-row matrix twice after executable RED evidence. Malformed association/identity/enum/PII/lifecycle facts return `Hidden`; valid NotProvided/Erased facts return `Unavailable`; both carry null values, empty fields, and no physical identity. Management disclosure no longer accepts or returns physical `LocationId`; field/purpose/governance/audience/server-time behavior remains deterministic and fail closed. Batched loading, cache invalidation, route activation, and public/attendee adoption remain owned by open ELP-315/340/405 work.

ELP-330 is verified complete. The Application attachment service reuses active event/location or event/TBA associations, creates new fail-closed policy version 1 associations with their initial audit when absent, and never revives a detached row. EventSession, EventSessionGroup, EventAgendaItem, EventSessionAgendaItem, and development seeding dual-write through the same server-owned authority boundary; event moves and explicit location clears resolve new event-scoped or TBA associations, final detaches are lock-safe, and referenced rooms cannot be reparented across physical locations. All 36 seeded carriers converge on 8 distinct active authorities/audits with stable carrier/authority identities after the second seed. Real-PostgreSQL seeder 6/6, dual-write 8/8 twice, service 9/9, session-agenda handlers 6/6, strict fixtures 42/42, architecture 15/15 plus 4/4, and the root build are green; independent confidence is 0.99.

W8 is independently verified complete. ELP-230B passed 3/3 twice on fresh PostgreSQL, migration stages 7/7, Persistence privacy 54/54, and Clean Architecture 15/15; all four carrier families converge with stable event-local authority, replay, atomic-failure, zero-gap, guarded/safe-Down, and forward-repair evidence. ELP-225 passed its 62-case Application matrix, current-binary PostgreSQL 3/3, one-reader/zero-tracking budget, tenant-filter architecture 4/4, and Clean Architecture 15/15. ELP-340 passed governance services 15/15, API governance 2/2, Domain audit 8/8, selected cache/architecture 5/5, PostgreSQL repository 12/12, and post-commit handler ordering 10/10. The three independent verdicts are `confirmed` at 0.98, 0.96, and 0.99 confidence. Disclosure stays inactive until the remaining batch, route, contraction, and consumer-adoption gates pass.

Repository-wide green and current-head/model parity are not claimed. Concurrent notification-test compile errors block a fresh Persistence/root build, the shared migration fixture has an externally introduced duplicate `smtp_available_tokens` operation, and a concurrent Reconcile migration plus emptied shared snapshot invalidates current-head/model-parity evidence. The earlier pre-concurrency 18,594-line SQL artifact applied twice at its then-current 67-migration head. Live API health also remains unverified because the Aspire run stopped on persisted PostgreSQL/RabbitMQ credential mismatch.

## 2. Non-Goals and Forbidden Shortcuts

- Do not create a second address subsystem or move disclosure policy into `LocationPii`.
- Do not infer ownership from `CreatedBy`; use explicit consent-backed `OwnerUserId`.
- Do not let `LocationKind` grant disclosure.
- Do not expose physical `LocationId` in public event-location contracts without a separately approved public use case.
- Do not interpret nullable registration approval status as entitlement without registration-mode and intent-lifecycle evaluation.
- Do not create correction messages after commit.
- Do not rely on `LoggingOutboxMessageDispatcher`; every new event type must have a concrete route and idempotent handler.
- Do not vary the anonymous public route by authentication cookie.
- Do not cache attendee or management representations; use `private, no-store`.
- Do not restore the anonymous exact-address behavior during rollback.
- Do not auto-copy `LocationPii` coordinates into discovery indexes.
- Do not hand-edit generated NSwag clients or applied migrations.

## 3. Authoritative Rules

Implementation must follow:

- `AGENTS.md`
- `docs/QUICK_REFERENCE.md`
- `docs/GOVERNANCE.md`
- `docs/ARCHITECTURE.md`
- `docs/DOMAIN.md`
- `docs/API.md`
- `docs/SECURITY-MODEL.md`
- `docs/AUTHORIZATION.md`
- `docs/MULTI_TENANCY.md`
- `docs/FEDERATION.md`
- `docs/OPERATIONS.md`
- `docs/OUTBOX_PATTERN.md`
- `docs/TESTING.md`
- `docs/BLAZOR.md`
- `docs/ACCESSIBILITY.md`
- `docs/DESIGN_SYSTEM.md`
- `docs/LOCALIZATION.md`
- `.claude/rules/domain.md`
- `.claude/rules/application-layer.md`
- `.claude/rules/efcore-persistence.md`
- `.claude/rules/efcore-migrations.md`
- `.claude/rules/api-controllers.md`
- `.claude/rules/api-hateoas.md`
- `.claude/rules/blazor-client.md`
- `.claude/rules/tests.md`

## 4. Verified Baseline and Current Stage

| Evidence | Current behavior | Planning consequence |
|---|---|---|
| `src/Explore.Domain/Location.cs` and `LocationPii.cs` | Durable name/city/country/timezone are on `Location`; street/postcode/coordinates are in required one-to-one `LocationPii`. | Extend this boundary, make PII lifecycle explicit, and recognize durable fields can identify a Home. |
| `src/Explore.Domain/LocationRoom.cs` | Room name and description live outside PII. | Classify and redact/tombstone room data contextually. |
| `src/Explore.Persistence/Repositories/LocationRepository.cs` | Reads auto-include PII; `ForgetPiiAsync` exists but has no account-erasure caller. | Split public/management reads and wire irreversible erasure. |
| `src/Explore.Application/DTOs/Location/LocationDto.cs` and `LocationListDto.cs` | Generic contracts expose exact or identifying data; `LocationListDto` includes Address. | Remove anonymous exact contracts and correct Home Discovery assumptions. |
| `src/Explore.API/Controllers/LocationController.cs` and `LocationRoomController.cs` | Stage A makes generic reads authenticated/resource-authorized, `private, no-store`, and uncached. | ELP-405/430 later replace temporary compatibility boundaries with final EventLocation routes/contracts. |
| Public session/group/agenda handlers | Stage A redacts physical fields and requires Published+Public parent plus child/day eligibility. | ELP-320 later routes selected disclosure through the EventLocation authority. |
| `src/Explore.Application/Features/EventPrograms/Handlers/Queries/GetEventProgramSummaryRequestHandler.cs` | Stage A uses public-only content and emits no physical values or location-readiness warnings. | ELP-320 later batch-resolves policy-selected public disclosure. |
| `src/Explore.Application/Features/Events/Handlers/Queries/GetEventCalendarExportRequestHandler.cs` | Stage A suppresses public location text. | ELP-440 later splits public and attendee calendar authority. |
| `src/Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` | Stage A sanitizes JSON-LD and removes the unconditional private-address promise. | ELP-630/650 later render server-selected disclosure states/affordances. |
| `src/Explore.Domain/EventRegistrationIntent.cs`, `RegistrationScopeEnum.cs`, and `Services/Registration/RegistrationPolicyRules.cs` | Registration intent supports Event, Day, and SessionSelection scopes. | Entitlement must use effective intent coverage, not row existence. |
| `src/Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs` and `src/Explore.API/BackgroundServices/OutboxProcessor.cs` | Unknown/non-managed reconciliation returns without failure, and the processor can mark that no-op as reconciled. | Make unknown/no-op reconciliation fail closed; add explicit, idempotent location-privacy correction routes and tests. |

Baseline before planning: `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and 2,776 pre-existing warnings.

## 5. Contextual Field Classification

Sensitivity is determined by field, location kind, event context, audience, purpose, and governance. Table placement alone does not make a field safe.

| Field | Baseline classification | Public rule | Erasure rule |
|---|---|---|---|
| Country code | Normally non-sensitive | Policy-controlled; may be public | Retain unless combined context requires stricter tenant policy |
| Timezone | Normally non-sensitive | Policy-controlled; may be public | Retain |
| City/locality | Context-sensitive | Explicit `ShowCity`; Private Home defaults false | Remove from public output after erasure if context can identify residence |
| Venue display name / `Location.FullName` | Context-sensitive | Explicit `ShowVenueName`; Private Home public label is always generic `Private venue` | Tombstone owner/household-specific value irreversibly |
| Room/sub-venue name | Context-sensitive | Explicit `ShowRoomName`, evaluated through `EventLocation` | Tombstone identifying Home room names |
| Room description | Restricted management data | Never general public in v1 | Tombstone Home descriptions during erasure |
| Street address | Exact-sensitive PII | Explicit policy plus governance plus entitlement | Hard-delete with `LocationPii` |
| Postcode | Exact-sensitive in Home context | Explicit policy plus governance plus entitlement | Hard-delete with `LocationPii` |
| Exact latitude/longitude | Exact-sensitive PII | Explicit policy plus governance plus entitlement; never implies discovery indexing | Hard-delete with `LocationPii`; remove derived discovery point |
| Formatted address, map URL, geohash | Exact-sensitive derivative | Never independently persisted or exposed without same authority as source coordinates/address | Delete or regenerate only from authorized live PII |
| Access instructions, entry details, door codes | Restricted operational secret | Never general public; separate operational access contract | Hard-delete or cryptographically destroy according to owning store |

Private Home owner/household labels must move into a PII-governed field or be overwritten by `Location.EraseOwnedPii()`. No redacted historical value may remain in audit payloads, room records, search indexes, caches, or outbox payloads.

## 6. Target Domain Model

### 6.1 LocationKind

Create `src/Explore.Domain/LocationKind.cs` as a normalized int lookup with stable `MasterCode` values:

| MasterCode | Meaning |
|---|---|
| `UNCLASSIFIED` | Legacy or not reviewed; grants no disclosure |
| `COMMERCIAL_VENUE` | Commercially operated venue |
| `PUBLIC_SPACE` | Public outdoor or civic space |
| `COMMUNITY_VENUE` | Community, faith, educational, or nonprofit venue |
| `PRIVATE_HOME` | Personal residence requiring strict lifecycle rules |

`LocationKind` is descriptive only. Effective disclosure always comes from `EventLocation`, entitlement, server time, and the most restrictive governance rule.

### 6.2 LocationPrivacyState

Create `src/Explore.Domain/LocationPrivacyState.cs` with:

- `NOT_PROVIDED`: no PII has been supplied; later PII attachment is permitted after validation/consent.
- `ACTIVE`: PII is present and usable under policy.
- `ERASED`: PII was irreversibly erased; PII can never be reattached to this `Location`.

Extend `src/Explore.Domain/Location.cs` with `LocationKindId`, `LocationPrivacyStateId`, nullable `OwnerUserId`, nullable `PiiErasedAtUtc`, nullable `PiiErasureReasonCode`, and concurrency/audit fields.

Invariants:

- `PRIVATE_HOME` with Active PII requires `OwnerUserId`.
- `PRIVATE_HOME` defaults owner to the current user creating it.
- Choosing or transferring ownership to another user requires an explicit consent/transfer command and audit record.
- Non-Home locations forbid `OwnerUserId`.
- `NOT_PROVIDED` and `ERASED` are distinct durable states.
- `ERASED` requires `OwnerUserId == null`, `LocationPii == null`, an erasure timestamp/reason, and tombstoned identifying Home labels/rooms.
- `ERASED` rejects every PII attach/update path. A replacement address requires a new `Location` and consent decision.
- `Location.EraseOwnedPii()` performs the aggregate transition; persistence hard-deletes `LocationPii` and redacts dependent Home room data in the same transaction.

### 6.3 FullDetailsAudience

Create `src/Explore.Domain/LocationDisclosureAudience.cs` with stable values:

- `NEVER`
- `ANY_CURRENT_REGISTRANT`
- `CONFIRMED_PARTICIPANT`

UI labels may say “Registered attendee” and “Approved attendee,” but API/domain values remain precise. `PRIVATE_HOME` defaults to `CONFIRMED_PARTICIPANT` and may be made stricter by governance.

### 6.4 Canonical EventLocation

Create `src/Explore.Domain/EventLocation.cs` as the first-class event-to-place association. It owns:

- `Id`, `TenantId`, `EventId`, nullable `LocationId`;
- `ShowVenueName`, `ShowCity`, `ShowCountry`, `ShowRoomName`;
- `ShowStreetAddress`, `ShowPostcode`, `ShowCoordinates`;
- `FullDetailsAudienceId`;
- nullable `RevealFullDetailsFromUtc`;
- `NeedsPrivacyReview`;
- `IsToBeAnnounced`;
- `PolicyVersion`;
- concurrency stamp/xmin;
- created/updated/soft-delete audit fields;
- last policy actor and timestamp.

Rules:

- A database XOR constraint requires either a physical `LocationId` or `IsToBeAnnounced=true`, never both and never neither. Physical association uniqueness is filtered to active non-TBA rows; TBA uniqueness is filtered per active event.
- The server creates a fail-closed `EventLocation` whenever a physical location is first attached to an event.
- Clients never need to construct a correct policy to establish the association.
- One physical `Location` may have independent policies on multiple events.
- Event/session/agenda public contracts expose `EventLocationId`, not physical `LocationId`.
- `LocationRoomId` may remain linked to the physical location, but room disclosure is evaluated through `EventLocation`.
- Event-local references gain authoritative `EventLocationId`, including sessions, session groups, agenda items, and session agenda items. Their internal physical `LocationId` columns remain where the current room-containment composite keys and session GiST overlap exclusion require them; database consistency constraints require those IDs to match the selected EventLocation. Internal physical IDs never enter public contracts.
- Detaching the final event-local reference soft-deletes the `EventLocation` for audit.
- Reattaching the same physical location creates a fresh fail-closed `EventLocation`; a soft-deleted policy is never resurrected.
- `RevealFullDetailsFromUtc` is evaluated against server UTC and only after audience entitlement succeeds.
- `PolicyVersion` increments on every disclosure mutation and participates in cache/invalidation tokens.
- `IsToBeAnnounced=true` is an explicit organizer decision, suppresses every physical-location field, and permits publication without a usable physical venue; it is never inferred from erasure or missing PII.

### 6.5 Policy Audit

Add append-only `EventLocationDisclosureAudit` records containing event-location ID, tenant, actor, timestamp, old/new field selections, old/new audience, old/new reveal time, policy version, and reason. Never include physical address, coordinates, access instructions, or erased values.

Exceptional/admin exact reads emit a separate PII-free security audit containing requester, purpose, event-location ID, authorization decision, timestamp, and trace/correlation ID.

## 7. Registration-Intent Entitlement

Create immutable `EventLocationRegistrationAccess` and resolve it from `EventRegistrationIntent`, `RegistrationScope`, selected days/sessions, registration policy, lifecycle, and effective state.

The following ELP-200/210 tables are authoritative. `Approved` is the current persisted approval term; the disclosure resolver treats it as the `Confirmed`-equivalent effective state.

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

ELP-200 added and verified stable persisted `Cancelled=5`/`CANCELLED` and `Revoked=6`/`REVOKED` values. Null approval is now resolved by registration mode; Pending/Approved consume capacity, terminal transitions release it, and child transitions synchronize parent lifecycle without cancelling remaining live children. Registration identity fields cannot be reassigned by PATCH. Attendee own-cancellation authorization is enriched from a persisted tenant-safe ownership snapshot and revalidated in the serializable cancellation transaction. ELP-210 implements the immutable pure EventLocation effective-state resolver. Verified ELP-225 uses one tenant-filter-preserving entity read, maps parent and current session-placement facts into exact Event/Day/SessionSelection coverage, and resolves the strongest valid intent for each requested EventLocation. No active matching intent fails closed. Attendee location authority still cannot activate before the remaining batched-service and route gates land.

Coverage rules:

- Event scope covers every eligible `EventLocation` used by the event.
- Day scope covers only `EventLocation` values used by eligible sessions/items on the selected event day.
- SessionSelection covers only the selected sessions’ `EventLocation` values.
- No active intent means no attendee entitlement.
- Null approval status and lifecycle use the authoritative tables above; they are never guessed from row existence.
- Coverage of another event, day, session, or event location never grants access.

The access result contains intent ID, scope, effective state, event/day/session coverage, and `CoversRequestedEventLocation`.

## 8. Disclosure Architecture

Split I/O orchestration from deterministic field evaluation:

- `src/Explore.Application/Contracts/Services/IEventLocationDisclosureService.cs`
- `src/Explore.Application/Services/EventLocationDisclosureService.cs`
- `src/Explore.Application/Services/EventLocationDisclosureEvaluator.cs`
- `src/Explore.Application/DTOs/Location/EventLocationDisclosureRequest.cs`
- `src/Explore.Application/DTOs/Location/EventLocationDisclosureResult.cs`
- `src/Explore.Application/DTOs/Location/EventLocationRegistrationAccess.cs`

The service exposes:

```csharp
Task<IReadOnlyDictionary<Guid, EventLocationDisclosureResult>> ResolveManyAsync(
    IReadOnlyCollection<EventLocationDisclosureRequest> requests,
    CancellationToken cancellationToken);
```

`EventLocationDisclosureService` deduplicates by event-location/requester/purpose, performs bounded batch queries for associations, locations, PII, room data, registration intents, and governance, batches manager authorization through the existing Candidate/Normalize/Batch/Materialize pipeline, then passes immutable facts to the pure evaluator.

`EventLocationDisclosureEvaluator` has no I/O. It applies, in order:

1. tenant and association validity;
2. privacy state and irreversible tombstone;
3. purpose-specific contract ceiling;
4. most restrictive instance/tenant governance;
5. manager authorization or registration-intent entitlement;
6. server-time reveal gate;
7. EventLocation field selections;
8. contextual Home label/room redaction;
9. fail-closed output materialization.

Initial performance budgets for one endpoint request:

- at most one query each for EventLocations, Locations/PII, rooms, registration intents/coverage, and governance;
- at most one batched authorization call for all manager candidates;
- no per-row database or policy calls;
- immutable results keyed by `EventLocationId`;
- query and authorization counts asserted in integration tests and revisited only with measured evidence.

## 9. Governance

Location privacy is an instance-and-tenant policy, never a user preference. Instance defaults are `SystemSetting` rows controlled by the instance-governance command path; tenant restrictions are `TenantSetting` rows controlled by the authorized tenant settings path. Add the five constants under `GovernanceSettingKeys.LocationPrivacy`, register them in a dedicated `LocationPrivacySettingDefinitions`, and read them only through `ILocationPrivacyGovernanceService`. The generic `TenantPolicySettingService.Resolve*` helpers are not the disclosure authority because they select an allowed tenant override and use permissive parse fallbacks instead of composing two ceilings.

| Key | JSON value and validation | Instance fallback when missing/invalid | Most-restrictive merge |
|---|---|---|---|
| `location_privacy.allow_home_locations` | Boolean only | `false` | instance AND tenant; `false` wins |
| `location_privacy.allow_public_exact_address` | Boolean only | `false` | instance AND tenant; `false` wins |
| `location_privacy.allow_public_coordinates` | Boolean only | `false` | instance AND tenant; `false` wins |
| `location_privacy.minimum_home_audience` | String master code: `NEVER`, `CONFIRMED_PARTICIPANT`, or `ANY_CURRENT_REGISTRANT` | `NEVER` | highest restriction in the ordered lattice `NEVER` > `CONFIRMED_PARTICIPANT` > `ANY_CURRENT_REGISTRANT` |
| `location_privacy.default_reveal_offset` | ISO-8601 non-negative duration from `PT0S` through `P30D` | `P30D` | later reveal wins (`max(instance, tenant)`); it never bypasses entitlement |

Tenant values may only preserve or tighten the instance ceiling. A tenant attempt to widen it is rejected by the validator and apply handler rather than silently normalized. Missing tenant values inherit the instance value; an unknown key, malformed JSON, unknown audience code, negative/out-of-range duration, repository failure, or duplicate conflicting row makes location disclosure fail closed and emits a bounded reason code without values. The resolver uses server UTC and returns the effective value plus source/version metadata for audit and cache keys.

Instance writes are owned by `UpdateInstanceGovernanceSettingsCommandHandler`; tenant writes use `SettingsController`, `UpdateSettingCommandHandler`, and `UpdateSettingBatchCommandHandler`, all under existing settings authorization and `ISettingMutationLock`. Every accepted change publishes an awaited `SettingChangedNotification`, which is audited by `SettingAuditLogHandler` and evicts hierarchical setting caches through `SettingCacheInvalidationHandler`. Location-specific tightening additionally increments affected EventLocation policy versions, marks incompatible associations `NeedsPrivacyReview`, inserts correction outbox rows in the same transaction, and invalidates planned `CacheTags.EventLocations`, tenant, event, and EventLocation tags after commit. The v1 public route has no shared cache.

Disabling Home locations blocks new Home creation but does not silently delete existing data. Tests are owned by `LocationPrivacyGovernanceServiceTests` (Application unit), `EventLocationGovernanceTests` (API integration), settings authorization tests, and `SettingCacheInvalidationHandlerTests`.

## 10. API and Cache Boundaries

### Public

`GET /api/events/{eventId}/locations`

- `[AllowAnonymous]` and `EndpointClass.Public`;
- always returns public-only disclosure, even when an authentication cookie/token is present;
- first release has no shared output cache until policy-version invalidation is proven;
- never exposes unrestricted physical `LocationId`;
- may expose `EventLocationId` and explicitly selected public fields only.

### Attendee

`GET /api/events/{eventId}/locations/my-access`

- `[Authorize]`;
- registration-intent aware;
- `Cache-Control: private, no-store`;
- returns only requester-entitled EventLocation details.

### Management

`GET /api/events/{eventId}/locations/{eventLocationId}/management`  
`PUT /api/events/{eventId}/locations/{eventLocationId}/disclosure`

- `[Authorize]` plus resource authorization;
- `Cache-Control: private, no-store`;
- exact operational details and disclosure controls only after server authorization;
- mutation affordances are emitted as HAL links and consumed by Blazor; no client role/claim gating.

Remove generic anonymous exact `GET /Location/{id}`. Anonymous physical-location discovery is coarse and limited to explicitly discoverable non-Home records. Public richness never varies by cookie.

## 11. Calendar and Outbound Surfaces

The frozen outbound contract is below. “Selected fields” means only fields returned by `EventLocationDisclosureService` for the stated purpose; producers never dereference `Location`, `LocationPii`, or `LocationRoom` directly. An absent producer remains an enforced absence proof, not permission for a future producer to emit location.

| Surface | Concrete implementation owner | Audience and purpose | Allowed location fields | Cache/retention | Tightening/erasure correction | Target evidence |
|---|---|---|---|---|---|---|
| Session, session-group, program, and agenda API projections | EventSession query handlers, `GetEventProgramSummaryRequestHandler`, agenda query handlers, `EventSessionMappingProfile` | Public event display; attendee variants use registration intent; management uses separately authorized DTO | Public/attendee/management selected fields respectively; public carries `EventLocationId`, never physical `LocationId` | Public response has no shared cache in v1; attendee/management `private, no-store` | Policy-version invalidation; current responses change immediately | focused tests beside those handlers plus `EventLocationOutboundProjectionTests` |
| Browser JSON-LD | `src/Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` | Anonymous search-engine structured data | Public selected label/city/country only; exact/Home fields absent unless explicitly public and governance permits | Page/output cache includes policy version; no private browser persistence | purge affected page/cache; next render is corrected | `tests/Explore.Blazor.Client.Tests/Pages/Event/EventLocationJsonLdPrivacyTests.cs` |
| Public calendar/ICS | `GetEventCalendarExportRequestHandler`, `IcalNetEventCalendarFileBuilder`, Event calendar controller | Anonymous public subscription | Public selected fields only; no Private Home exact fields or physical ID | public cache only after policy-version invalidation is proven; stable URL never embeds private data | invalidate generated ICS; warn that third-party imports cannot be remotely retracted | `EventCalendarPrivacyTests` in API integration and calendar builder unit tests |
| Attendee calendar/ICS | planned attendee calendar request/controller using the same calendar builder | Authenticated registrant convenience | Requester-entitled selected fields only | `private, no-store`; authenticated non-public URL | next fetch corrected; supported provider correction outbox when introduced; retention warning remains | `EventCalendarPrivacyTests` in API integration |
| Registration confirmations, approvals, reminders, cancellations, and organizer email | `EventLifecycleEmailOutboxFactory`, `EmailDispatchProcessor` | One addressed registrant/organizer; lifecycle communication | Current factory carries no location; future location requires recipient-specific disclosure snapshot and never access instructions | durable email outbox; transport copies cannot be recalled | enqueue a PII-free correction message where follow-up is supported; otherwise warn/reissue, never claim remote deletion | `EventLifecycleEmailOutboxFactoryTests` plus planned `EventLifecycleEmailLocationPrivacyTests` |
| In-app/web-push notifications | `EventPublishedNotificationFanoutService`, `DefaultNotificationOrchestrator`, web-push sender | Recipient-specific event lifecycle notice | No location today; future payload contains EventLocationId plus selected label only, never exact fields in push text | recipient-private notification storage; no shared cache | update/delete owned notification projection and emit refresh; external push cannot be recalled | notification fanout/orchestrator tests plus planned `NotificationLocationPrivacyTests` |
| Tickets and QR payloads | no event ticket/QR producer exists in current source; ELP-715 owns the absence gate | Confirmed participant admission | QR/token contains opaque admission identifier only; rendered ticket may use confirmed-participant selected fields | private/no-store generation; exported artifact is user-retained | revoke/reissue token or ticket; PII-free correction notice; no remote-delete promise | `EventLocationOutboundSurfaceAbsenceTests` architecture scan; future producer test beside owner |
| Svix/local webhooks | `DefaultWebhookPayloadBuilder`, webhook event registry, delivery materializer/dispatcher | Explicit subscribed integration purpose | Schema allow-list may carry EventLocationId and purpose-selected fields only; no generic Location DTO/PII | durable exact payload bytes under webhook retention policy; never shared cache | versioned PII-free `location.privacy.corrected`/`location.pii.erased` event, idempotent retry/dead letter | `DefaultWebhookPayloadBuilderTests`, webhook delivery tests, and ELP-520 dispatcher tests |
| CSV/JSON exports | no event-location export exists; contact-share export is unrelated and carries no location | Authorized one-time management export | If added, management-selected fields only; exact reads require resource authorization and audit | `private, no-store`; artifact retention disclosed | regenerate/reissue and correction notice; exported copies cannot be recalled | `EventLocationOutboundSurfaceAbsenceTests`; future export handler tests beside producer |
| Search indexes/projections | Event/EventSession list and custom-property projection handlers; no geo/location index exists | Anonymous event discovery/search | Public selected coarse fields only; no address/postcode/coordinates/physical ID | index/cache partitioned by tenant and policy version | transactional correction intent, delete stale document, rebuild before serving | list/projection tests plus `EventLocationSearchProjectionPrivacyTests` |
| Moderation/admin support views | event moderation query handlers/controllers; no dedicated location projection exists | Authorized case review/support purpose | Management-selected fields only when necessary; exceptional exact read is resource-authorized and PII-free audited | `private, no-store`; no durable evidence copy of address | policy tightening changes view immediately; audit/correction if an external provider received data | moderation API tests plus `EventLocationOutboundSurfaceAbsenceTests` |
| API-key consumers | `ApiKeyAuthenticationHandler` plus the same purpose-specific controllers | Machine principal constrained by endpoint purpose and resource authorization | API key never upgrades public output; attendee fields require a user registration principal; management fields require explicit resource authority | same cache policy as selected route; management `private, no-store` | policy-version invalidation and webhook-style correction for subscribed integrations | API-key integration tests plus `EventLocationControllerTests` |
| Print/PDF/admin reports | `EventReportsController` and report providers currently carry moderation reports, not event-location output; no event-location PDF/print producer exists | Authorized operational report | Management-selected fields only if later added; no access instructions unless the explicit operational purpose requires them | `private, no-store`; generated artifact retention disclosed | regenerate/reissue and notify; no remote-delete promise | `EventLocationOutboundSurfaceAbsenceTests`; future report-provider test beside producer |
| MCP tools/resources and AI prompt context | `EventManagementMcpTools`, `EventManagementMcpResources`, `IAiContextGateway` registry/matrix | Public MCP discovery or separately authorized management tool | Purpose-selected DTO, then AI gateway field ceiling; never raw Location/PII/room | no shared response cache; prompt/log telemetry stores bounded IDs/categories only | next call reflects policy; purge governed AI cache/context and emit external correction when retained | MCP architecture/integration tests and AI disclosure matrix tests |
| Federation/PDS | `PdsSyncWorker`, `PdsService`, PDS outbox repository | Explicit federation record purpose | EventLocationId plus federation-purpose selected fields only | remote retained record; local outbox idempotency | idempotent update/delete correction through PDS outbox with retry/dead letter | PDS service/worker tests plus planned `PdsLocationPrivacyCorrectionTests` |
| Home Discovery/PostGIS | `GetHomeDiscoveryQueryHandler`, `PublicDiscoveryAreaDto`; no `LocationDiscoveryPoint` store exists | Anonymous coarse occurrence discovery | Configured coarse area id/name only; never generic Location DTO, exact coordinate/address, or physical ID | `PublicHomeDiscovery` cache keyed by coarse query; invalidated on eligible occurrence/policy change | current absence proof; future derived point deleted/deactivated in erasure transaction and index rebuilt | Home Discovery handler/service tests and ELP-730 architecture absence proof |

Public ICS is public-only. Attendee ICS is authenticated and `private, no-store`. Private Home data never enters stable public subscription URLs. AI/MCP output still passes through `IAiContextGateway`; the location authority never bypasses AI disclosure policy.

## 12. Platform Erasure Integration Boundary

The platform privacy-erasure authority workstream owns User fencing, cross-tenant enumeration, authority persistence/topology, receipt/status, provider work, startup replay, retention, and restore behavior. This Event Location workstream must not duplicate that orchestration.

Event Location owns one compiled, typed adapter consumed by the platform workflow. It must:

- accept the platform-owned User intent and persisted tenant/subject scope;
- tombstone owned Home labels/rooms through the Location domain invariants;
- identify affected `EventLocation` associations and persist PII-free correction intents with a stable idempotency key;
- mark affected associations `NeedsPrivacyReview` without embedding addresses, coordinates, room text, provider data, or free-text errors;
- fail closed on missing/mismatched tenant or ownership state; and
- provide focused integration tests for adapter behavior, tenant substitution, unrelated locations, idempotency, and correction creation.

The authority workstream owns the surrounding transaction, replay, receipt/fence outcome, provider settlement, and cross-family acceptance tests. Event Location owns correction delivery after the platform transaction commits: explicit routes, idempotency, retry/backoff, dead-letter visibility, reconciliation, persisted tenant/EventLocation rebinding, and stale-cache prevention.

## 13. EventLocation Correction and Remediation

After the platform erasure adapter reports an affected physical location:

- affected EventLocations remain `NeedsPrivacyReview`;
- managers receive a PII-free notification and remediation dashboard item;
- publication is blocked when a required physical venue is unusable;
- organizers may explicitly choose `Location to be announced` rather than receiving a misleading neutral address;
- re-publication requires a reviewed replacement EventLocation or explicit TBA state.

The correction dispatcher uses a fresh dependency scope, reloads persisted tenant/EventLocation ownership, and treats queued identifiers only as routing hints. Sensitive reads remain `no-store` or policy-version partitioned; cache invalidation failure creates retryable convergence work and must not permit stale exact-location disclosure.

## 14. PostGIS and Discovery Readiness

The future `LocationDiscoveryPoint` remains separate from `LocationPii` and `EventLocation`:

1. `ShowCoordinates` never grants discovery indexing.
2. A discovery point is never auto-copied from `LocationPii`.
3. Discovery points have separate visibility, precision, provenance, consent, and activation.
4. Private Home has no discovery point by default.
5. Home erasure deletes/deactivates derived discovery data in the same transaction.
6. Future proximity joins eligible occurrences through `EventLocation` and session/agenda occurrence data.
7. Public APIs never download a full exact-coordinate catalog.
8. “Distance” and “near you” are blocked until server-side eligible-occurrence calculation exists.

## 15. Migration and Rollout

Use three focused additive migrations; never edit merged history:

### ELP-230A: Expand schema

- add `LocationKind`, `LocationPrivacyState`, and `LocationDisclosureAudience` lookups and stable seed values;
- extend `Location` lifecycle/owner/erasure fields;
- add `EventLocation`, policy/exact-read audit, and required indexes/constraints;
- add nullable `EventLocationId` references alongside existing physical references;
- make `LocationPii` optional without weakening tenant ownership;
- deploy fail-closed reads and dual-write for new/changed event-local references.

### ELP-230B: Backfill

- classify every existing location as `UNCLASSIFIED`, never Public;
- backfill lifecycle deterministically: `LocationPii` present means `ACTIVE`, absent means `NOT_PROVIDED`; legacy data is never inferred as `ERASED` and ownership is never inferred from audit fields;
- derive unique tenant/event/physical-location pairs from every session/group/agenda/room reference;
- create EventLocations with `ShowCountry=true`; all other fields false unless a separately recorded continuity decision allows city; audience `NEVER`; `NeedsPrivacyReview=true`;
- backfill `EventLocationId` references idempotently;
- never infer Private Home or owner from `CreatedBy`;
- emit review-queue metrics for Unclassified locations and unresolved EventLocations.

### ELP-230C: Validate and contract

- prove zero missing EventLocation references, orphan associations, duplicate active event/location pairs, tenant mismatches, invalid Home states, and resurrected Erased PII;
- switch event-local reads/writes to EventLocationId;
- make constraints required where validated;
- remove obsolete anonymous exact contract/routes only after all consumers migrate; retain internal physical scheduling IDs required by composite room-containment and GiST overlap constraints, protected by EventLocation consistency constraints;
- preserve reversible `Down` only before contract activation and irreversible erasure; afterward use forward repair.

Safe activation stages:

- Stage A: ship public minimization and missing-policy fail-closed behavior.
- Stage B: expand schema and dual-write while reads remain Stage A-safe.
- Stage C: run idempotent backfill and zero-gap verification.
- Stage D: enable selected public/attendee disclosure only after the gate passes.

Failure keeps Stage A and additive schema active, fixes data/code, and reruns backfill. Never roll back to anonymous exact exposure.

Each migration stage is a separate operator-selected deployment target. `Database:Migrations:EventLocationPrivacyStage` accepts only `Expand`, `Backfill`, or `Contract`; the environment form is `Database__Migrations__EventLocationPrivacyStage`. When ELP migrations are pending, a missing/invalid selector fails startup instead of applying all pending stages. Each value applies only its named stage after predecessor/evidence checks; requesting an already-applied target is an idempotent no-op before later-pending-stage checks. AppHost reads the stage once and conditionally forwards a nonblank value to both MigrationService and API, with no default or hard-coded fallback. The retry repair passed focused PostgreSQL 2/2, the full stage suite 7/7, and AppHost architecture 7/7. Manual Aspire QA remains blocked by persisted PostgreSQL/RabbitMQ credential mismatch: forwarding is proven, but migration-service exit 0 and healthy API are not. ELP-230C runs only after ELP-420A generated-contract adoption and every API, Blazor, calendar, outbound, AI, and federation consumer is proven migrated; ELP-420B then regenerates the final contracted client.

## 16. Execution Waves

The thematic phases in the task checklist describe ownership; this dependency-correct wave order controls execution. Tasks in one wave may run in parallel only when they do not share files.

W1 through W5, W7, and W8 are complete. W6 contains the verified EventLocation Expand work and pure evaluator; global User-erasure evidence formerly grouped into this wave now belongs to the authority workstream. ELP-330 is independently confirmed: development seeding joins the in-scope dual-write boundary, all 36 seeded carriers converge on 8 distinct active authorities and initial audits stably after a second seed, and the real-PostgreSQL seeder 6/6, dual-write 8/8 twice, service 9/9, session-agenda handler 6/6, strict fixture 42/42, architecture 15/15 plus 4/4, and root build gates were green at verifier confidence 0.99. W8 ELP-230B/225/340 has independent confirmed evidence. W9 retains ELP-350/315 and ELP-360. W10 EventLocation projection/route/correction code is present with deferred verification gates; W11 HAL/calendar/AI/federation/discovery code is present with deferred verification. Owner/remediation actions remain open in W12/W16. This does not claim generated-client/editor adoption, model parity, API 29/29, or final QA waves.

| Wave | Tasks | Exit evidence |
|---|---|---|
| W1 | `ELP-000`, `ELP-005`, `ELP-010`, `ELP-020`, `ELP-030`, `ELP-040`, `ELP-060`, `ELP-200`, `ELP-400` | Aligned docs, protected Home Discovery contract, leakage/auth/cache characterization, and locked domain/lifecycle facts. |
| W1A | `ELP-015` immediately after `010` and `400` | Known anonymous Location/session/program/calendar/JSON-LD/MCP/filter leaks are fail-closed or coarse without waiting for schema. |
| W2 | `ELP-100`, `ELP-110` | Normalized lookups and stable codes pass Domain tests. |
| W3 | `ELP-120`, `ELP-125` | Nullable-TBA XOR, lifecycle, retained scheduling integrity, and association invariants pass. |
| W4 | `ELP-130`, `ELP-140`, `ELP-150`, `ELP-210`, `ELP-300` | Field test vectors, erasure/audit domain records, TBA, registration facts, and purpose-specific contracts are stable. |
| W5 | `ELP-240`, EventLocation-owned portion of `ELP-260` | Bounded repositories, concurrency, disclosure/exact-read audits, and EventLocation checkpoint behavior pass. |
| W6 | `ELP-230A`; `ELP-310` independently | EventLocation Expand schema and pure evaluator pass. |
| W7 | `ELP-330` | Dual-write is verified with fail-closed associations across production and development seeding writers. |
| W8 | `ELP-230B`, `ELP-225`, `ELP-340` | Conservative backfill, intent coverage, and restrictive governance pass. |
| W9 | `ELP-350` → `ELP-315`; `ELP-360` | Authorization precedes batched management reads and policy mutation remains transactional. |
| W10 | `ELP-320`, `ELP-405` | Backend projections and route split pass. |
| W11 | `ELP-410`, `ELP-440`, `ELP-720`, `ELP-730` | HAL, calendar split, AI/federation, and discovery boundaries pass. |
| W12 | `ELP-515`, `ELP-520`, `ELP-530`, `ELP-715` | Typed platform adapter, correction delivery, remediation, and concrete outbound producers are covered. |
| W13 | `ELP-420A`, `ELP-540` | Additive generated contracts are available and bounded metrics/alerts exist. |
| W14 | `ELP-600` | Blazor adopts generated purpose-specific contracts; no hand-edited client. |
| W15 | `ELP-610`, `ELP-630`, `ELP-640`, `ELP-650` | Management/public/attendee/review/JSON-LD UI flows pass; serialize `ELP-630` and `ELP-650` where EventDetail overlaps. |
| W16 | `ELP-620` | Owner consent/transfer UX passes after management editor stabilizes. |
| W17 | `ELP-660`, `ELP-700` | Browser QA and final negative source/behavior audit for shared projections pass. |
| W18 | `ELP-230C` → `ELP-430` → `ELP-420B` | Zero-gap contraction passes, obsolete anonymous exact contracts are removed, then final generated-contract cleanliness is proven. |
| W19 | `ELP-740` | Canonical shipped-behavior docs match code and operations. |
| W20 | `ELP-800`, `ELP-810`, `ELP-830` | Migration, adversarial, contract, and browser evidence is complete. |
| W21 | `ELP-820` | Every required project suite and Release build pass. |
| W22 | `ELP-840` | Repository/dev-doc review proves no obsolete authority or unrelated edits. |

`ELP-130` owns the field matrix and evaluator test vectors; `ELP-310` owns evaluator code. `ELP-320` owns session/program/agenda backend projections, `ELP-440` owns both calendars, `ELP-650` owns JSON-LD/copy, and `ELP-700` is the final cross-surface proof. `ELP-420A` generates additive contracts for adoption; `ELP-420B` proves the final removal contract. Platform User-erasure acceptance is defined only in the authority workstream.

## 17. Required Test Matrix

Tests must prove:

- unknown legacy locations become Unclassified, never Public;
- Active Private Home is valid with owner; non-erased Active Home without owner is invalid;
- Erased Home with no owner/PII is valid; PII reattachment is rejected;
- person names in Home `FullName` and Home room names/descriptions are tombstoned and never public;
- same physical Location on two events has independent EventLocation policies;
- final detach soft-deletes policy; reattach creates fresh fail-closed EventLocation;
- public contracts expose EventLocationId and not unrestricted LocationId;
- Event, Day, and SessionSelection intent coverage is exact;
- pending/waitlisted qualify only for broad audience; cancelled/revoked/rejected/deleted deny; null state is resolved by mode;
- the typed platform-erasure adapter tombstones owned Home labels/rooms, marks affected EventLocations for review, and emits stable PII-free correction intents without touching unrelated locations;
- adapter tenant/subject substitution fails closed and duplicate invocation is idempotent;
- EventLocation policy mutation and correction intent roll back or commit together;
- correction dispatcher is concrete, idempotent, retryable, dead-letter visible, and never carries address values;
- public endpoint with an auth cookie remains byte-for-byte public-only;
- attendee and management endpoints are private/no-store;
- tightened policy/governance defeats stale caches and external projections;
- public calendar is public-only; attendee calendar is authorized/no-store;
- email, webhook, ticket, export, search, MCP, federation, and reports cannot bypass disclosure authority;
- discovery point is never auto-created from exact PII and EventLocation disclosure never treats discovery consent as authority;
- batch projection stays within query and authorization count budgets;
- server time, not client time, controls delayed reveal;
- Private Home default is generic/no identifying public fields plus ConfirmedParticipant.

Each acceptance case has one primary automated owner; cross-layer cases may add narrower tests but cannot move the primary proof:

| Acceptance owner | Primary test project / file | Task |
|---|---|---|
| Legacy kind/state backfill | `tests/Event.Persistence.IntegrationTests/Migrations/EventLocationBackfillTests.cs` | `ELP-230B` |
| Location lifecycle and resurrection rejection | `tests/Event.Domain.UnitTests/LocationPrivacyLifecycleTests.cs` | `ELP-120` |
| EventLocation erasure-adapter correction/remediation | focused integration tests beside the typed EventLocation disposition adapter | `ELP-515`, `520`, `530` |
| Independent event policies and TBA XOR | `tests/Event.Domain.UnitTests/EventLocationTests.cs` | `ELP-125`, `150` |
| Public contract identifier boundary | `tests/Event.API.IntegrationTests/Features/EventLocationControllerTests.cs` | `ELP-300`, `405` |
| Registration intent scope/lifecycle coverage | `tests/Event.Application.UnitTests/Services/EventLocationRegistrationAccessServiceTests.cs` | `ELP-200`, `225` |
| Public-cookie equivalence and private/no-store routes | `tests/Event.API.IntegrationTests/Features/EventLocationControllerTests.cs` | `ELP-400`, `405` |
| Governance/cache invalidation | `tests/Event.API.IntegrationTests/Features/EventLocationGovernanceTests.cs` | `ELP-340`, `810` |
| Public/attendee calendar separation | `tests/Event.API.IntegrationTests/Features/EventCalendarPrivacyTests.cs` | `ELP-440` |
| Outbound producer boundary | tests beside each producer named by the ELP-020 inventory | `ELP-715` |
| Correction routing/reconciliation | `tests/Explore.Infrastructure.Tests/Infrastructure/CompositeOutboxMessageDispatcherTests.cs` plus API dead-letter tests | `ELP-520` |
| Discovery separation | `tests/Event.Persistence.IntegrationTests/Privacy/LocationDiscoveryPrivacyTests.cs`, or architecture absence proof while no discovery store exists | `ELP-730` |
| Batch query/authorization budget | `tests/Event.Persistence.IntegrationTests/Privacy/EventLocationDisclosureBatchTests.cs` | `ELP-315` |
| Server-time reveal and field matrix | `tests/Event.Application.UnitTests/Services/EventLocationDisclosureEvaluatorTests.cs` | `ELP-130`, `310`, `340` |
| Blazor HAL/disclosure/JSON-LD states | `tests/Explore.Blazor.Client.Tests/EventLocationPrivacyTests.cs` | `ELP-600`, `610`, `630`, `650` |
| Browser accessibility/responsive/RTL | manual QA matrix recorded in context | `ELP-660`, `830` |
| Cross-surface negative proof | focused tests above plus repository source scan recorded in context | `ELP-700`, `810` |
| Migration selector/contract gate | `tests/Event.Persistence.IntegrationTests/Migrations/EventLocationMigrationStageTests.cs` | `ELP-230A`, `230B`, `230C`, `800` |

## 18. Verification Commands

Run at minimum:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category!=Runtime]" --minimum-expected-tests 1 --no-progress
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1
```

Mark new tests with `[Category("EventLocationPrivacy")]` where supported so the focused security lane is stable. Also run migration SQL inspection, contract snapshot/OpenAPI generation, NSwag regeneration cleanliness, targeted output-cache tests, and manual browser-based responsive/accessibility/RTL journeys.

Completed-wave EventLocation evidence through Todo 9 includes managed/API ELP 19/19, API public ELP 11/11, Application ELP 18/18, native Cerbos 461/461, full Application 2,317/2,317, full Blazor 1,702/1,702 executed with one not executed, MCP and SDK focused coverage, lifecycle/aggregate Domain coverage, registration access 42/42, disclosure contracts 17/17, EventLocation persistence 12/12 plus relational model 1/1, ELP-230A migration stages/Persistence privacy, ELP-310 evaluator 72/72 twice, ELP-330 seeder/dual-write coverage, and ELP-230B/225/340 evidence. Authority-specific PostgreSQL, global User-erasure, and restore receipts now belong only to the authority workstream. Current repository-wide build/model parity is not claimed; current generated hashes and remaining EventLocation gates are recorded in context/tasks.

## 19. Risks and Controls

| Risk | Control |
|---|---|
| Durable Home fields remain identifying after PII deletion | Contextual matrix, generic label, room tombstoning, adversarial erasure tests |
| Registration scope over-grants another day/session/location | Intent-coverage value object and exhaustive scope tests |
| Platform erasure adapter receives wrong tenant/subject | Reload persisted ownership, fail closed, and prove hostile substitution plus unrelated-location preservation |
| Partial policy mutation loses correction event | Insert correction intent in the same transaction and prove rollback/commit behavior |
| Default/no-op dispatcher silently drops correction | Explicit Composite route, concrete handler, startup/architecture tests, dead-letter alert |
| Legacy data becomes public accidentally | Unclassified backfill, conservative fail-closed EventLocation, review queue |
| Auth cookie changes public response and cache safety | Dedicated public-only route and equivalence test |
| Policy tightening leaves stale external data | PolicyVersion, correction outbox, cache purge, surface inventory |
| Future proximity feature reuses exact PII | Separate discovery provenance/consent and no auto-copy rule |

## 20. Definition of Done

- All tasks in `event-location-privacy-tasks.md` are checked with evidence.
- Plan, context, and tasks remain synchronized after every implementation slice.
- All event-location outputs pass through the batch disclosure authority or document why they contain no location data.
- The typed platform-erasure adapter, EventLocation correction dispatch, cache convergence, review queue, and remediation are proven without duplicating platform orchestration.
- API/HAL/OpenAPI/NSwag/Blazor contracts use EventLocationId and purpose-specific DTOs.
- No anonymous exact physical-location endpoint remains.
- Operations, security, privacy, domain, API, federation, localization, accessibility, testing, AI disclosure, and Home Discovery docs reflect shipped behavior.
- Release build and required project tests pass with no new warnings attributable to this work.

## 21. Implementation-Agent Contract

Before editing product code, the implementation agent must read this plan, context, and task checklist plus the current source paths for that slice. It must:

- mark exactly one atomic task in progress;
- update `event-location-privacy-context.md` with discoveries, decisions, commands, failures, and next step;
- update all three documents whenever architecture or scope changes;
- add tests before non-trivial implementation;
- preserve unrelated dirty-worktree changes;
- stop for approval before weakening privacy, tenant boundaries, EventLocation transaction atomicity, or irreversible location-state rules;
- never claim a surface is migrated without repository evidence and a runnable check;
- complete with a technical teaching summary and explicit remaining risks.
