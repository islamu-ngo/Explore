<!-- ABOUTME: Decision-complete implementation plan for contextual, audience-scoped EventLocation disclosure. -->
<!-- ABOUTME: Defines the EventLocation model, safe rollout, correction integration, API/UI boundaries, and verification contract. -->

# Event Location Privacy Implementation Plan

**Status:** Approved EventLocation architecture; 40 of 59 tasks verified complete; backend authority, API controller split, HAL policies, additive OpenAPI client generation (ELP-420A), correction dispatch, and typed erasure adapter verified; Phase 7 Blazor UX adoption (`ELP-600`..`ELP-660`) in progress; platform erasure authority canonicalized in `docs/PRIVACY_ERASURE.md`.
**Last Updated:** 2026-08-18 Europe/Brussels
**Intent:** Cross-cutting fallback contract composed from `add-cqrs-handler`, `update-repository-query`, `add-ef-migration`, `add-get-endpoint`, `add-write-endpoint`, `openapi-contract-change`, `add-hal-link`, `cerbos-policy-change`, and `blazor-component-affordance`  
**Review:** Senior CTO amendments and repository re-audit incorporated; product changes follow the execution waves in Section 16
**External privacy-erasure boundary:** [`docs/PRIVACY_ERASURE.md`](file:///home/amir/ISLAMU/Github/Event/docs/PRIVACY_ERASURE.md) is the sole owner of User erasure, authority topology (`EmbeddedSqlite`, `CoLocated`, `ExternalDatabase`), receipt/status polling, provider settlement, replay, retention, and restore. This plan owns only EventLocation disclosure and its typed correction/remediation integration (`IUserLocationPrivacyErasureRepository`).

## 1. Outcome

Deliver event-location disclosure that is independent of event visibility and safe for public, attendee, organizer, API-key, MCP, federation, notification, and export flows.

The system must:

- keep physical addresses in the existing `Location` / `LocationPii` boundary;
- classify locations with a non-authorizing `LocationKind`;
- distinguish never-provided PII from irreversibly erased PII with `LocationPrivacyState`;
- use first-class `EventLocation` associations as the only event-local disclosure authority;
- disclose fields according to event policy, governance restrictions, registration-intent coverage, manager authorization, and server time;
- contribute a typed, idempotent EventLocation disposition/correction adapter (`IUserLocationPrivacyErasureRepository`) to the platform erasure workflow;
- persist EventLocation policy changes and correction intents in the same database transaction;
- expose public, attendee, and management representations through separate routes, controller endpoints, and cache policies with RFC 7807 `ProblemDetails`;
- preserve auditability without copying address values into policy audit, logs, metrics, or outbox payloads.

### Verified Implementation Milestones (as of 2026-08-18)

1. **Domain & Lookup Foundations (W1-W5, ELP-000..ELP-260)**:
   - Normalized `LocationKind`, `LocationPrivacyState`, `LocationDisclosureAudience` lookups.
   - `Location` aggregate lifecycle with consent-backed Private Home ownership and irreversible `EraseOwnedPii()` tombstoning.
   - First-class `EventLocation` entity with UUIDv7, optimistic concurrency, and nullable-TBA database XOR.
   - Contextual 16-field disclosure matrix (`EventLocationDisclosureContract`) and append-only PII-free audits (`EventLocationDisclosureAudit`, `EventLocationExactReadAudit`).
   - Pure, synchronous `EventLocationDisclosureEvaluator` tested across exhaustive 72/72 matrix.
   - Strict registration coverage resolver (`EventLocationRegistrationAccessService`) supporting Event, Day, and SessionSelection scopes (62 cases).
   - Entity-returning repositories (`EventLocationRepository`) with bounded batch queries.

2. **Persistence Migrations & Dual-Write (W6-W8, ELP-230A, ELP-230B, ELP-330, ELP-340)**:
   - Expand migration `20260716132239_AddEventLocationPrivacyExpand` with lookup tables, XOR constraints, and audit tables.
   - Backfill migration `20260718215537_BackfillUnclassifiedEventLocations` idempotently backfilling all carrier tables with Unclassified/NeedsPrivacyReview state.
   - `EventLocationAttachmentService` managing fail-closed associations across all 4 carrier families and development seeding.
   - Governance composition (`LocationPrivacyGovernanceService`) merging 5 typed setting keys through most-restrictive lattice with post-commit cache invalidation (`CacheTags.EventLocations`).

3. **Application Authority & Projections (W9-W10, ELP-315, ELP-320, ELP-350, ELP-360)**:
   - `EventLocationDisclosureService` providing batched resolution within strict query/auth budgets (1 query per surface, 1 batched manager authorization).
   - Public session, session group, program summary, and agenda query handlers projecting through `IEventLocationDisclosureService` batch resolution with AutoMapper physical field exclusions.
   - `EventLocationManagementAuthorizationService` evaluating `event:view-management` in batch and persisting PII-free audit records via `EventLocationExactReadAuditService`.
   - `UpdateEventLocationPolicyCommandHandler` enforcing optimistic concurrency tokens (`ExpectedConcurrencyStamp`, `ExpectedPolicyVersion`), appending audits, and dispatching outbox correction intents.

4. **API, HAL, OpenAPI & Calendar Contracts (W10-W13, ELP-405, ELP-410, ELP-420A, ELP-440)**:
   - Dedicated `EventLocationController` exposing 6 purpose-specific endpoints with `[ApiVersion("0.1")]`, `[EndpointClassification]`, `[PrivateNoStore]`, and RFC 7807 `ProblemDetails` with typed problem descriptors (`EventLocationNotFoundProblem`, `DisclosureValidationProblem`, `RemediationValidationProblem`).
   - Grouped disclosure updates via `PATCH /api/events/{eventId}/locations/{eventLocationId}/disclosure` taking `UpdateEventLocationDisclosureDto`.
   - Explicit remediation confirmation via `POST /api/events/{eventId}/locations/{eventLocationId}/remediation/confirm`.
   - HAL link policies (`EventLocationLinkPolicy`) emitting server-authorized `edit` and `remediate-location` affordances without client-side role inspection.
   - Additive OpenAPI / NSwag generation (`ELP-420A`) registered in `HalOpenApiSchemaCatalog.cs` and generated into `EventApiClient.g.cs`.
   - Calendar export split (`ELP-440`) providing separate public and attendee ICS feeds with `X-Calendar-Retention-Warning`.

5. **Correction Dispatch, Typed Adapter & Remediation (W12, ELP-515, ELP-520, ELP-530, ELP-720, ELP-730)**:
   - `LocationPrivacyCorrectionDispatcher` handling `LocationPiiErased`, `LocationPrivacyCorrectionRequested`, `location.privacy.corrected` events with outbox retry, dead-letter visibility, and ATProto correction planning.
   - Typed platform erasure adapter (`IUserLocationPrivacyErasureRepository`) reloading persisted ownership, tombstoning Home PII/rooms, and marking affected associations `NeedsPrivacyReview`.
   - Remediation CQRS workflow (`ConfirmEventLocationRemediationCommand`, `GetEventLocationReviewQueueRequest`) clearing review flags only on verified active physical venues or explicit TBA.
   - AI/MCP disclosure gated by `IAiContextGateway` and Home Discovery bounded to coarse areas without raw PII coordinates.

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
- `docs/PRIVACY_ERASURE.md`
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
- `.agents/rules/domain.md`
- `.agents/rules/application-layer.md`
- `.agents/rules/efcore-persistence.md`
- `.agents/rules/efcore-migrations.md`
- `.agents/rules/api-controllers.md`
- `.agents/rules/api-hateoas.md`
- `.agents/rules/blazor-client.md`
- `.agents/rules/tests.md`

## 4. Verified Baseline and Current Stage

| Evidence | Current behavior | Planning consequence |
|---|---|---|
| `src/Explore.Domain/Location.cs` and `LocationPii.cs` | Durable name/city/country/timezone are on `Location`; street/postcode/coordinates are in optional one-to-one `LocationPii`. | PII lifecycle is explicit (`LocationPrivacyState`); irreversible tombstoning on erasure. |
| `src/Explore.Domain/LocationRoom.cs` | Room name and description live outside PII. | Tombstone identifying Home room names and descriptions during erasure. |
| `src/Explore.Domain/EventLocation.cs` | First-class event-to-place association owning field disclosure, audience, reveal time, review flag, policy version, and concurrency. | `EventLocation` is the sole event-local disclosure authority. |
| `src/Explore.API/Controllers/EventLocationController.cs` | Capability-partitioned controller exposing public, attendee, management, review queue, PATCH disclosure, and POST remediation confirmation. | All endpoints use typed ProblemDetails and `[PrivateNoStore]`. |
| `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` | Strongly-typed NSwag generated client exposing all 6 EventLocation methods. | Blazor UI consumes generated client directly; gate mutations via `_links`. |
| `src/Explore.Infrastructure/Messaging/LocationPrivacyCorrectionDispatcher.cs` | Dispatches correction events, invalidates cache tags, and replans ATProto federated records. | Composite outbox routes provide idempotent retry and dead-letter visibility. |
| `src/Explore.Persistence/Repositories/UserLocationPrivacyErasureRepository.cs` | Implements typed platform erasure adapter for Location/Home disposition. | Platform authority orchestration remains external (`docs/PRIVACY_ERASURE.md`). |

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

`src/Explore.Domain/LocationKind.cs` is a normalized int lookup with stable `MasterCode` values:

| MasterCode | Meaning |
|---|---|
| `UNCLASSIFIED` | Legacy or not reviewed; grants no disclosure |
| `COMMERCIAL_VENUE` | Commercially operated venue |
| `PUBLIC_SPACE` | Public outdoor or civic space |
| `COMMUNITY_VENUE` | Community, faith, educational, or nonprofit venue |
| `PRIVATE_HOME` | Personal residence requiring strict lifecycle rules |

`LocationKind` is descriptive only. Effective disclosure always comes from `EventLocation`, entitlement, server time, and the most restrictive governance rule.

### 6.2 LocationPrivacyState

`src/Explore.Domain/LocationPrivacyState.cs`:

- `NOT_PROVIDED`: no PII has been supplied; later PII attachment is permitted after validation/consent.
- `ACTIVE`: PII is present and usable under policy.
- `ERASED`: PII was irreversibly erased; PII can never be reattached to this `Location`.

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

`src/Explore.Domain/LocationDisclosureAudience.cs` with stable values:

- `NEVER`
- `ANY_CURRENT_REGISTRANT`
- `CONFIRMED_PARTICIPANT`

UI labels may say “Registered attendee” and “Approved attendee,” but API/domain values remain precise. `PRIVATE_HOME` defaults to `CONFIRMED_PARTICIPANT` and may be made stricter by governance.

### 6.4 Canonical EventLocation

`src/Explore.Domain/EventLocation.cs` is the first-class event-to-place association. It owns:

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
- Event-local references carry authoritative `EventLocationId`. Internal physical `LocationId` columns remain where composite room-containment and GiST overlap exclusion require them, protected by database consistency constraints.
- Detaching the final event-local reference soft-deletes the `EventLocation` for audit.
- Reattaching the same physical location creates a fresh fail-closed `EventLocation`; a soft-deleted policy is never resurrected.
- `RevealFullDetailsFromUtc` is evaluated against server UTC and only after audience entitlement succeeds.
- `PolicyVersion` increments on every disclosure mutation and participates in cache/invalidation tokens.
- `IsToBeAnnounced=true` is an explicit organizer decision, suppresses every physical-location field, and permits publication without a usable physical venue.

### 6.5 Policy Audit

Append-only `EventLocationDisclosureAudit` records contain event-location ID, tenant, actor, timestamp, old/new field selections, old/new audience, old/new reveal time, policy version, and reason. Never include physical address, coordinates, access instructions, or erased values.

Exceptional/admin exact reads emit a separate PII-free `EventLocationExactReadAudit` containing requester, purpose, event-location ID, authorization decision, timestamp, and trace/correlation ID.

## 7. Registration-Intent Entitlement

Immutable `EventLocationRegistrationAccess` is resolved from `EventRegistrationIntent`, `RegistrationScope`, selected days/sessions, registration policy, lifecycle, and effective state.

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

Coverage rules:

- Event scope covers every eligible `EventLocation` used by the event.
- Day scope covers only `EventLocation` values used by eligible sessions/items on the selected event day.
- SessionSelection covers only the selected sessions’ `EventLocation` values.
- No active intent means no attendee entitlement.
- Null approval status and lifecycle use the authoritative tables above; they are never guessed from row existence.
- Coverage of another event, day, session, or event location never grants access.

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

`EventLocationDisclosureService` deduplicates by event-location/requester/purpose, performs bounded batch queries for associations, locations, PII, room data, registration intents, and governance, batches manager authorization, then passes immutable facts to the pure evaluator.

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

Performance budgets for one endpoint request:

- at most one query each for EventLocations, Locations/PII, rooms, registration intents/coverage, and governance;
- at most one batched authorization call for all manager candidates;
- no per-row database or policy calls;
- immutable results keyed by `EventLocationId`.

## 9. Governance

Location privacy is an instance-and-tenant policy, never a user preference. Instance defaults are `SystemSetting` rows; tenant restrictions are `TenantSetting` rows. Read only through `ILocationPrivacyGovernanceService`.

| Key | JSON value and validation | Instance fallback when missing/invalid | Most-restrictive merge |
|---|---|---|---|
| `location_privacy.allow_home_locations` | Boolean only | `false` | instance AND tenant; `false` wins |
| `location_privacy.allow_public_exact_address` | Boolean only | `false` | instance AND tenant; `false` wins |
| `location_privacy.allow_public_coordinates` | Boolean only | `false` | instance AND tenant; `false` wins |
| `location_privacy.minimum_home_audience` | String master code: `NEVER`, `CONFIRMED_PARTICIPANT`, or `ANY_CURRENT_REGISTRANT` | `NEVER` | highest restriction in the ordered lattice `NEVER` > `CONFIRMED_PARTICIPANT` > `ANY_CURRENT_REGISTRANT` |
| `location_privacy.default_reveal_offset` | ISO-8601 non-negative duration from `PT0S` through `P30D` | `P30D` | later reveal wins (`max(instance, tenant)`); it never bypasses entitlement |

Tenant values may only preserve or tighten the instance ceiling. Widening attempts are rejected with validation errors. Setting updates increment affected EventLocation policy versions, mark incompatible associations `NeedsPrivacyReview`, insert correction outbox rows transactionally, and invalidate `CacheTags.EventLocations` post-commit.

## 10. API and Cache Boundaries

### Public

`GET /api/events/{eventId}/locations`

- `[AllowAnonymous]` and `EndpointClass.Public`;
- always returns public-only disclosure (`IReadOnlyList<EventLocationPublicDto>`), even when an authentication cookie/token is present;
- no shared output cache until policy-version invalidation is proven;
- never exposes unrestricted physical `LocationId`;
- exposes `EventLocationId` and explicitly selected public fields only.

### Attendee

`GET /api/events/{eventId}/locations/my-access`

- `[Authorize]` and `EndpointClass.Authenticated`;
- registration-intent aware, returning `IReadOnlyList<EventLocationAttendeeDto>`;
- `[PrivateNoStore]` (`Cache-Control: private, no-store`);
- returns only requester-entitled EventLocation details.

### Management

`GET /api/events/{eventId}/locations/{eventLocationId}/management`

- `[Authorize]`, `[PrivateNoStore]`, returns `HalResource<EventLocationManagementDto>`;
- exact operational details and disclosure controls only after server authorization;
- mutation affordances are emitted as HAL links (`edit`, `remediate-location`).

`GET /api/events/{eventId}/locations/review`

- `[Authorize]`, `[PrivateNoStore]`, returns `HalCollectionResource<EventLocationManagementDto>`;
- returns all event locations requiring privacy review (`NeedsPrivacyReview == true`).

`PATCH /api/events/{eventId}/locations/{eventLocationId}/disclosure`

- `[Authorize]`, `[PrivateNoStore]`, takes `UpdateEventLocationDisclosureDto` (`ExpectedConcurrencyStamp`, `ExpectedPolicyVersion`, `Fields`, `Audience`);
- updates disclosure policy, increments `PolicyVersion`, appends audit, writes outbox correction intent;
- returns `BaseCommandResponse<Guid>` or RFC 7807 `ValidationProblemDetails`.

`POST /api/events/{eventId}/locations/{eventLocationId}/remediation/confirm`

- `[Authorize]`, `[PrivateNoStore]`, takes `ConfirmEventLocationRemediationDto` (`ExpectedConcurrencyStamp`, `ExpectedPolicyVersion`);
- clears `NeedsPrivacyReview` flag on verified active venue or explicit TBA, writes outbox correction intent;
- returns `BaseCommandResponse<Guid>` or RFC 7807 `ValidationProblemDetails`.

## 11. Calendar and Outbound Surfaces

| Surface | Concrete implementation owner | Audience and purpose | Allowed location fields | Cache/retention | Tightening/erasure correction | Target evidence |
|---|---|---|---|---|---|---|
| Session, session-group, program, and agenda API projections | EventSession query handlers, `GetEventProgramSummaryRequestHandler`, agenda query handlers, `EventSessionMappingProfile` | Public event display; attendee variants use registration intent; management uses separately authorized DTO | Public/attendee/management selected fields respectively; public carries `EventLocationId`, never physical `LocationId` | Public response has no shared cache in v1; attendee/management `private, no-store` | Policy-version invalidation; current responses change immediately | focused tests beside those handlers plus `EventLocationOutboundProjectionTests` |
| Browser JSON-LD | `src/Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` | Anonymous search-engine structured data | Public selected label/city/country only; exact/Home fields absent unless explicitly public and governance permits | Page/output cache includes policy version; no private browser persistence | purge affected page/cache; next render is corrected | `tests/Explore.Blazor.Client.Tests/Pages/Event/EventLocationJsonLdPrivacyTests.cs` |
| Public calendar/ICS | `GetEventCalendarExportRequestHandler`, `IcalNetEventCalendarFileBuilder`, Event calendar controller | Anonymous public subscription | Public selected fields only; no Private Home exact fields or physical ID | public cache only after policy-version invalidation is proven; stable URL never embeds private data | invalidate generated ICS; warn that third-party imports cannot be remotely retracted | `EventCalendarPrivacyTests` in API integration and calendar builder unit tests |
| Attendee calendar/ICS | `GetAttendeeCalendarExportRequestHandler` using the same calendar builder | Authenticated registrant convenience | Requester-entitled selected fields only | `private, no-store`; authenticated non-public URL | next fetch corrected; retention warning (`X-Calendar-Retention-Warning`) | `EventCalendarPrivacyTests` in API integration |
| In-app/web-push notifications | `EventPublishedNotificationFanoutService`, `DefaultNotificationOrchestrator` | Recipient-specific event lifecycle notice | No raw PII in push text; EventLocationId plus selected label only | recipient-private notification storage; no shared cache | update/delete owned notification projection and emit refresh | notification fanout/orchestrator tests |
| Svix/local webhooks | `DefaultWebhookPayloadBuilder`, webhook event registry, dispatcher | Explicit subscribed integration purpose | Schema allow-list carries EventLocationId and purpose-selected fields only | durable exact payload bytes under webhook retention policy | versioned PII-free `location.privacy.corrected` event, idempotent retry/dead letter | `DefaultWebhookPayloadBuilderTests`, `LocationPrivacyCorrectionDispatcherTests` |
| Search indexes/projections | Event/EventSession list projection handlers; no geo/location index exists | Anonymous event discovery/search | Public selected coarse fields only; no address/postcode/coordinates/physical ID | index/cache partitioned by tenant and policy version | transactional correction intent, delete stale document, rebuild before serving | list/projection tests |
| API-key consumers | `ApiKeyAuthenticationHandler` plus purpose-specific controllers | Machine principal constrained by endpoint purpose | API key never upgrades public output; attendee fields require a user registration principal | same cache policy as selected route; management `private, no-store` | policy-version invalidation | API-key integration tests |
| MCP tools/resources and AI prompt context | `EventManagementMcpTools`, `EventManagementMcpResources`, `IAiContextGateway` | Public MCP discovery or authorized management tool | Purpose-selected DTO, then AI gateway field ceiling; never raw Location/PII/room | no shared response cache; prompt/log telemetry stores bounded IDs/categories only | next call reflects policy; purge governed AI cache/context | MCP architecture/integration tests |
| Federation/PDS | `PdsSyncWorker`, `PdsService`, PDS outbox repository | Explicit federation record purpose | EventLocationId plus federation-purpose selected fields only | remote retained record; local outbox idempotency | idempotent update/delete correction through PDS outbox | PDS service/worker tests |
| Home Discovery/PostGIS | `GetHomeDiscoveryQueryHandler`, `PublicDiscoveryAreaDto` | Anonymous coarse occurrence discovery | Configured coarse area id/name only; never generic Location DTO, exact coordinate/address | `PublicHomeDiscovery` cache keyed by coarse query | current absence proof; future derived point deleted/deactivated in erasure transaction | Home Discovery handler/service tests and ELP-730 architecture absence proof |

## 12. Platform Erasure Integration Boundary

The platform privacy-erasure authority is canonicalized in [`docs/PRIVACY_ERASURE.md`](file:///home/amir/ISLAMU/Github/Event/docs/PRIVACY_ERASURE.md). It owns:
- User fencing, cross-tenant enumeration, authority persistence across 3 topologies (`EmbeddedSqlite`, `CoLocated`, `ExternalDatabase`);
- `202 Accepted` response with `ErasureReceipt` containing cryptographic authentication token for status polling;
- Provider settlement, startup replay, retention, and restore behavior.

Event Location Privacy owns only the compiled, typed `IUserLocationPrivacyErasureRepository` adapter. It:
- accepts the platform-owned User intent and persisted tenant/subject scope;
- tombstones owned Home labels/rooms through the Location domain invariants (`Location.EraseOwnedPii()`);
- identifies affected `EventLocation` associations and marks them `NeedsPrivacyReview`;
- inserts PII-free `location.privacy.corrected` outbox intents with stable UUIDv7 idempotency keys;
- fails closed on missing/mismatched tenant or ownership state.

## 13. EventLocation Correction and Remediation

After the platform erasure adapter reports an affected physical location:

- affected EventLocations remain `NeedsPrivacyReview`;
- managers receive a PII-free notification and remediation dashboard item;
- publication is blocked when a required physical venue is unusable;
- organizers may explicitly choose `Location to be announced` (`IsToBeAnnounced=true`);
- re-publication requires a reviewed replacement EventLocation or explicit TBA state.

`LocationPrivacyCorrectionDispatcher` uses a fresh dependency scope, reloads persisted tenant/EventLocation ownership, and treats queued identifiers only as routing hints. Sensitive reads remain `no-store` or policy-version partitioned (`CacheTags.EventLocations`, `CacheTags.EventLocationsByEvent(eventId)`).

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

### ELP-230A: Expand schema (✅ Complete)

- add `LocationKind`, `LocationPrivacyState`, and `LocationDisclosureAudience` lookups and stable seed values;
- extend `Location` lifecycle/owner/erasure fields;
- add `EventLocation`, policy/exact-read audit, and required indexes/constraints;
- add nullable `EventLocationId` references alongside existing physical references;
- make `LocationPii` optional without weakening tenant ownership.

### ELP-230B: Backfill (✅ Complete)

- classify every existing location as `UNCLASSIFIED`, never Public;
- backfill lifecycle deterministically: `LocationPii` present means `ACTIVE`, absent means `NOT_PROVIDED`;
- derive unique tenant/event/physical-location pairs from every session/group/agenda/room reference;
- create EventLocations with `ShowCountry=true`; all other fields false; audience `NEVER`; `NeedsPrivacyReview=true`;
- backfill `EventLocationId` references idempotently.

### ELP-230C: Validate and contract (⏸️ Deferred to Wave 18)

- prove zero missing EventLocation references, orphan associations, duplicate active pairs, tenant mismatches;
- switch event-local reads/writes to EventLocationId;
- remove obsolete anonymous exact contract/routes only after all consumers migrate; retain internal physical scheduling IDs required by composite room-containment and GiST overlap constraints, protected by EventLocation consistency constraints.

## 16. Execution Waves

| Wave | Tasks | Status | Exit evidence |
|---|---|---|---|
| W1 | `ELP-000`, `ELP-005`, `ELP-010`, `ELP-020`, `ELP-030`, `ELP-040`, `ELP-060`, `ELP-200`, `ELP-400` | ✅ Complete | Aligned docs, protected Home Discovery contract, leakage/auth/cache characterization, and locked domain/lifecycle facts. |
| W1A | `ELP-015` immediately after `010` and `400` | ✅ Complete | Known anonymous Location/session/program/calendar/JSON-LD/MCP/filter leaks are fail-closed or coarse without waiting for schema. |
| W2 | `ELP-100`, `ELP-110` | ✅ Complete | Normalized lookups and stable codes pass Domain tests. |
| W3 | `ELP-120`, `ELP-125` | ✅ Complete | Nullable-TBA XOR, lifecycle, retained scheduling integrity, and association invariants pass. |
| W4 | `ELP-130`, `ELP-140`, `ELP-150`, `ELP-210`, `ELP-300` | ✅ Complete | Field test vectors, erasure/audit domain records, TBA, registration facts, and purpose-specific contracts are stable. |
| W5 | `ELP-240`, `ELP-260` | ✅ Complete | Bounded repositories, concurrency, disclosure/exact-read audits, and EventLocation checkpoint behavior pass. |
| W6 | `ELP-230A`, `ELP-310` | ✅ Complete | EventLocation Expand schema and pure evaluator pass (72/72 matrix). |
| W7 | `ELP-330` | ✅ Complete | Dual-write is verified with fail-closed associations across production and development seeding writers. |
| W8 | `ELP-230B`, `ELP-225`, `ELP-340` | ✅ Complete | Conservative backfill, intent coverage (62 cases), and restrictive governance pass. |
| W9 | `ELP-350` → `ELP-315`; `ELP-360` | ✅ Complete | Management authorization/audit, batched resolution budget, and policy concurrency/audit pass. |
| W10 | `ELP-320`, `ELP-405` | ✅ Complete | Public backend projections and dedicated `EventLocationController` (PATCH/POST/GET) pass. |
| W11 | `ELP-410`, `ELP-440`, `ELP-720`, `ELP-730` | ✅ Complete | HAL link policies, calendar split, AI/federation, and discovery boundaries pass. |
| W12 | `ELP-515`, `ELP-520`, `ELP-530` | ✅ Complete | Typed platform adapter (`IUserLocationPrivacyErasureRepository`), correction dispatcher, and remediation workflow pass. |
| W13 | `ELP-420A`, `ELP-540` | 🟡 In Progress | Additive generated contracts available (`EventApiClient.g.cs` ✅); metrics/alerts (`ELP-540`) in progress. |
| W14 | `ELP-600` | 🟡 In Progress | Blazor adopts generated purpose-specific contracts; no hand-edited client. |
| W15 | `ELP-610`, `ELP-630`, `ELP-640`, `ELP-650` | ⏸️ Next | Management/public/attendee/review/JSON-LD UI flows. |
| W16 | `ELP-620` | ⏸️ Next | Owner consent/transfer UX passes after management editor stabilizes. |
| W17 | `ELP-660`, `ELP-700`, `ELP-715` | ⏸️ Pending | Browser QA, outbound producer audit, and final negative source scan pass. |
| W18 | `ELP-230C` → `ELP-430` → `ELP-420B` | ⏸️ Gated | Zero-gap contraction passes, obsolete generic Location endpoints removed, final client regenerated. |
| W19 | `ELP-740` | ⏸️ Pending | Canonical shipped-behavior docs match code and operations. |
| W20 | `ELP-800`, `ELP-810`, `ELP-830` | ⏸️ Pending | Migration, adversarial, contract, and browser evidence is complete. |
| W21 | `ELP-820` | ⏸️ Pending | Every required project suite and Release build pass. |
| W22 | `ELP-840` | ⏸️ Pending | Final repository/dev-doc review proves zero obsolete authority. |

## 17. Required Test Matrix

| Acceptance owner | Primary test project / file | Task |
|---|---|---|
| Legacy kind/state backfill | `tests/Event.Persistence.IntegrationTests/Migrations/EventLocationBackfillTests.cs` | `ELP-230B` |
| Location lifecycle and resurrection rejection | `tests/Event.Domain.UnitTests/LocationPrivacyLifecycleTests.cs` | `ELP-120` |
| EventLocation erasure-adapter correction/remediation | `tests/Event.Persistence.IntegrationTests/Privacy/GlobalLocationPrivacyErasureTests.cs` | `ELP-515`, `520`, `530` |
| Independent event policies and TBA XOR | `tests/Event.Domain.UnitTests/EventLocationTests.cs` | `ELP-125`, `150` |
| Public contract identifier boundary | `tests/Event.API.IntegrationTests/Features/EventLocationControllerTests.cs` | `ELP-300`, `405` |
| Registration intent scope/lifecycle coverage | `tests/Event.Application.UnitTests/Services/EventLocationRegistrationAccessServiceTests.cs` | `ELP-200`, `225` |
| Public-cookie equivalence and private/no-store routes | `tests/Event.API.IntegrationTests/Features/EventLocationControllerTests.cs` | `ELP-400`, `405` |
| Governance/cache invalidation | `tests/Event.API.IntegrationTests/Features/EventLocationGovernanceTests.cs` | `ELP-340`, `810` |
| Public/attendee calendar separation | `tests/Event.API.IntegrationTests/Features/EventLocationPrivacyApiContractTests.cs` | `ELP-440` |
| Outbound producer boundary | tests beside each producer named by the ELP-020 inventory | `ELP-715` |
| Correction routing/reconciliation | `tests/Explore.Infrastructure.Tests/Infrastructure/LocationPrivacyCorrectionDispatcherTests.cs` | `ELP-520` |
| Discovery separation | `tests/Event.Persistence.IntegrationTests/Privacy/LocationDiscoveryPrivacyTests.cs`, or architecture absence proof | `ELP-730` |
| Batch query/authorization budget | `tests/Event.Persistence.IntegrationTests/Privacy/EventLocationDisclosureBatchTests.cs` | `ELP-315` |
| Server-time reveal and field matrix | `tests/Event.Application.UnitTests/Services/EventLocationDisclosureEvaluatorTests.cs` | `ELP-130`, `310`, `340` |
| Blazor HAL/disclosure/JSON-LD states | `tests/Explore.Blazor.Client.Tests/Security/EventLocationPrivacyStageAContractTests.cs` | `ELP-600`, `610`, `630`, `650` |
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
dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

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
nd explicit remaining risks.
