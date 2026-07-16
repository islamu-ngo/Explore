<!-- ABOUTME: Decision-complete implementation plan for contextual event-location disclosure, irreversible Home-location erasure, and audience-scoped access. -->
<!-- ABOUTME: Defines the canonical EventLocation model, safe rollout, transactional correction outbox, API boundaries, and verification contract. -->

# Event Location Privacy Implementation Plan

**Status:** Approved architecture; W1-W5 complete through bounded persistence and retained erasure authority; W6 next
**Last Updated:** 2026-07-16 Europe/Brussels
**Intent:** Cross-cutting fallback contract composed from `add-cqrs-handler`, `update-repository-query`, `add-ef-migration`, `add-get-endpoint`, `add-write-endpoint`, `openapi-contract-change`, `add-hal-link`, `cerbos-policy-change`, and `blazor-component-affordance`  
**Review:** Senior CTO amendments and repository re-audit incorporated; product changes follow the execution waves in Section 16

## 1. Outcome

Deliver event-location disclosure that is independent of event visibility and safe for public, attendee, organizer, API-key, MCP, federation, notification, export, and backup/restore flows.

The system must:

- keep physical addresses in the existing `Location` / `LocationPii` boundary;
- classify locations with a non-authorizing `LocationKind`;
- distinguish never-provided PII from irreversibly erased PII with `LocationPrivacyState`;
- use first-class `EventLocation` associations as the only event-local disclosure authority;
- disclose fields according to event policy, governance restrictions, registration-intent coverage, manager authorization, and server time;
- erase all owned Private Home PII across tenants during global account deletion;
- persist erasure and external-correction outbox messages in the same database transaction;
- expose public, attendee, and management representations through separate routes and cache policies;
- preserve auditability without copying address values into policy audit, logs, metrics, or outbox payloads.

Completed foundation on 2026-07-16: ELP-010/015/400 make known public Location/session/group/agenda/program/calendar/JSON-LD/filter/HAL/MCP paths fail closed, eligibility-gated, cache-safe, and principal-invariant. Separate authorized `private, no-store` management routes preserve draft editing but expose only locations/rooms already associated with the event; first/new venue selection stays fail-closed for non-admins until ELP-405/610. ELP-200 adds stable `Cancelled=5`/`CANCELLED` and `Revoked=6`/`REVOKED`, null-mode resolution, capacity-aware transitions, synchronized parent lifecycle, immutable registration identity, and own-cancellation authorization with transaction-time ownership revalidation. ELP-060/100/110 add the three normalized privacy lookup families; global repair-seeder activation waits for ELP-230A to create their tables.

Completed W3-W5 on 2026-07-16: ELP-120/125 add optional PII, consent-backed Private Home ownership, irreversible label/room tombstones, and the UUIDv7 physical-XOR-TBA EventLocation aggregate with fail-closed policy/version/concurrency state and carrier references. ELP-130/140/150/210/300 provide the executable 16-field matrix, typed PII-free audits/authority/checkpoint facts, explicit TBA publication behavior, immutable registration access, and purpose-specific constrained contracts. ELP-240/260 persist tenant-filtered bounded entity reads, tracked mutations, initial/contiguous policy audits, exact-read audits, replay checkpoints, stable concurrency conflicts, and a separately retained PostgreSQL authority with transactional monotonic UUIDv7-idempotent append. ELP-230A/250/500 and independent ELP-310 remain open.

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
| `src/Explore.Application/Features/Users/Handlers/Commands/DeleteUserCommandHandler.cs` | User PII and actor identifiers are erased, but owned Home locations are not. | Append the separate authority intent first, then add global cross-tenant Home erasure, local checkpoint, and correction outbox in one application-database transaction. |
| `src/Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs` and `src/Explore.API/BackgroundServices/OutboxProcessor.cs` | Unknown/non-managed reconciliation returns without failure, and the processor can mark that no-op as reconciled. | Make unknown/no-op reconciliation fail closed; add explicit, idempotent location-privacy correction routes and tests. |
| `docs/MULTI_TENANCY.md` | `TenantUser` / `TenantUserProfile` represent tenant membership. | Tenant membership removal must remain separate from global identity erasure. |

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

ELP-200 added and verified stable persisted `Cancelled=5`/`CANCELLED` and `Revoked=6`/`REVOKED` values. Null approval is now resolved by registration mode; Pending/Approved consume capacity, terminal transitions release it, and child transitions synchronize parent lifecycle without cancelling remaining live children. Registration identity fields cannot be reassigned by PATCH. Attendee own-cancellation authorization is enriched from a persisted tenant-safe ownership snapshot and revalidated in the serializable cancellation transaction. ELP-210 now implements the immutable pure EventLocation effective-state resolver; attendee location authority cannot activate before ELP-225 loads placement coverage and the remaining policy/governance/route gates land.

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

## 12. Global Erasure and Transactional Correction

Global account deletion and tenant membership removal are different operations.

### Global account deletion

Add a named privacy-erasure repository/query that explicitly bypasses the Tenant filter and is strictly bounded by `OwnerUserId`. It enumerates owned Private Home locations across all current and former tenants. Runtime code must not use unrestricted `IgnoreQueryFilters()`.

The v1 protocol intentionally does not claim cross-database atomicity:

1. require the separately retained PostgreSQL erasure-authority database to be available;
2. append an immutable PII-free erasure intent first, using a UUIDv7 intent ID as the idempotency key and receiving an authority-assigned monotonic sequence; payload fields are opaque owner/location IDs, reason code, and server UTC metadata only;
3. retry ambiguous authority acknowledgements with the same intent ID so the same sequence is returned;
4. in one application-database transaction, load/lock every OwnerUserId Home across tenants, erase/tombstone Location/PII/rooms/derived discovery data, mark EventLocations for review/versioning, complete User/Actor erasure, persist the local `(authority sequence, intent ID)` replay checkpoint, and insert PII-free correction outbox rows;
5. report success only after that application transaction commits; cache eviction is best effort afterward.

Rollback of the application transaction leaves PII, labels, state, user erasure, checkpoint, and outbox unchanged, while the already-appended authority intent remains pending. A crash after authority append is therefore safe: retry or startup replay applies the same intent idempotently. A crash after application commit finds both checkpoint and correction outbox durable. Authority unavailability fails deletion closed before application mutation.

Extend `src/Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs` with explicit location-privacy event routes and an idempotent concrete dispatcher. Unknown routes remain fatal/retryable and eventually dead-letter. Tests prove dispatch, duplicate delivery safety, retry, dead-letter visibility, and operator reconciliation.

If measured transaction volume makes the single transaction unsafe, stop and obtain approval for a durable saga before changing semantics; do not silently introduce partial erasure.

### Tenant membership removal

Tenant-admin removal changes `TenantUser` / `TenantUserProfile` participation only. It must not delete the global User, global UserPii, or Homes in this or other tenants unless a separate ownership-transfer workflow is completed.

| Operation | Application owner | Persistence/transaction owner | Required verification owner |
|---|---|---|---|
| Global account deletion | `DeleteUserCommandHandler` extended with the erasure-authority client and `IGlobalLocationPrivacyErasureRepository` | authority append first; then one `IUnitOfWork.ExecuteInTransactionAsync` app-DB transaction with the named owner-bounded query, local checkpoint, and outbox | `DeleteUserCommandHandlerTests` plus `GlobalLocationPrivacyErasureTests` |
| Tenant membership removal | planned `RemoveTenantMembershipCommandHandler` under `Features/TenantUsers` | tenant-filtered `TenantUser`/`TenantUserProfile` repositories only; role grants are revoked/soft-deleted in the same tenant | `RemoveTenantMembershipCommandHandlerTests` |
| Boundary enforcement | neither operation may call the other handler; only global deletion may depend on `IGlobalLocationPrivacyErasureRepository` | the repository bypass reason is unavailable to membership code | `EventLocationPrivacyArchitectureTests` rejects global-erasure dependencies from `Features/TenantUsers` |

## 13. Backup, Restore, and Remediation

Add `Event Location Privacy Erasure and Restore` to `docs/OPERATIONS.md`:

- backup retention and the limit that historical backups may still contain erased PII;
- immutable UUIDv7-idempotent PII-free intents with a monotonic sequence in a separately retained and backed-up PostgreSQL authority database outside the application restore set;
- mandatory erasure replay after restoring an older backup and before serving application traffic;
- cache purge and search/index rebuild after replay;
- external projection correction replay and dead-letter inspection;
- evidence queries proving no resurrected PII or discovery point remains;
- incident process when replay fails.

There is no cross-database transaction. The authority append happens first; application tombstones, local replay checkpoint, user erasure, and correction outbox then share one application-database transaction. A fresh application database starts its checkpoint at sequence zero and replays every authority intent. Application logical or physical-cluster restore never overwrites the authority: restore/verify the independently retained authority database, then replay it over the application database. Startup blocks API, BFF proxying, MCP, outbox/workers, and readiness until authority availability, sequence continuity, replay, and evidence queries succeed.

After Home erasure:

- affected EventLocations remain `NeedsPrivacyReview`;
- managers receive a PII-free notification and remediation dashboard item;
- publication is blocked when a required physical venue is unusable;
- organizers may explicitly choose `Location to be announced` rather than receiving a misleading neutral address;
- re-publication requires a reviewed replacement EventLocation or explicit TBA state.

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
- add `EventLocation`, policy audit, local erasure replay checkpoint, and required indexes/constraints;
- add nullable `EventLocationId` references alongside existing physical references;
- make `LocationPii` optional without weakening tenant ownership;
- keep authority-database provisioning outside application EF migrations; configure its append/read-only client and independently retained backup/restore contract, while the app migration adds only the local replay checkpoint;
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

Each migration stage is a separate operator-selected deployment target. `Database:Migrations:EventLocationPrivacyStage` accepts only `Expand`, `Backfill`, or `Contract`; the environment form is `Database__Migrations__EventLocationPrivacyStage`. When ELP migrations are pending, a missing/invalid selector fails startup instead of applying all pending stages. Each value applies only its named stage after predecessor/evidence checks. ELP-230C runs only after ELP-420A generated-contract adoption and every API, Blazor, calendar, outbound, AI, and federation consumer is proven migrated; ELP-420B then regenerates the final contracted client.

## 16. Execution Waves

The thematic phases in the task checklist describe ownership; this dependency-correct wave order controls execution. Tasks in one wave may run in parallel only when they do not share files.

W1 through W5 are complete. W6 is the next implementation wave. This does not complete the evaluator, migrations/backfill, global erasure, EventLocation route/editor, outbound correction, or final QA waves.

| Wave | Tasks | Exit evidence |
|---|---|---|
| W1 | `ELP-000`, `ELP-005`, `ELP-010`, `ELP-020`, `ELP-030`, `ELP-040`, `ELP-060`, `ELP-070`, `ELP-200`, `ELP-400` | Aligned docs, protected Home Discovery contract, leakage/auth/cache characterization, and locked domain/lifecycle facts. |
| W1A | `ELP-015` immediately after `010` and `400` | Known anonymous Location/session/program/calendar/JSON-LD/MCP/filter leaks are fail-closed or coarse without waiting for schema. |
| W2 | `ELP-100`, `ELP-110` | Normalized lookups and stable codes pass Domain tests. |
| W3 | `ELP-120`, `ELP-125` | Nullable-TBA XOR, lifecycle, retained scheduling integrity, and association invariants pass. |
| W4 | `ELP-130`, `ELP-140`, `ELP-150`, `ELP-210`, `ELP-300` | Field test vectors, erasure/audit domain records, TBA, registration facts, and purpose-specific contracts are stable. |
| W5 | `ELP-240`, `ELP-260` | Bounded repositories, concurrency, audit, separate-authority adapter, and local checkpoint pass. |
| W6 | `ELP-230A` → `ELP-250` → `ELP-500`; `ELP-310` independently | Expand schema, bounded cross-tenant query, adversarial erasure tests, and pure evaluator pass. |
| W7 | `ELP-330` | Dual-write is deployed with fail-closed associations. |
| W8 | `ELP-230B`, `ELP-225`, `ELP-340` | Conservative backfill, intent coverage, and restrictive governance pass. |
| W9 | `ELP-350` → `ELP-315`; `ELP-360`; `ELP-510`; atomic `ELP-505` + `ELP-515` | Authorization precedes batched management reads; membership removal remains separate; erasure and outbox commit once. |
| W10 | `ELP-320`, `ELP-405`, `ELP-520` | Backend projections, route split, and fail-closed correction dispatch pass. |
| W11 | `ELP-410`, `ELP-440`, `ELP-525`, `ELP-720`, `ELP-730` | HAL, calendar split, startup restore gate, AI/federation, and discovery boundaries pass. |
| W12 | `ELP-530`, `ELP-715` | Remediation and concrete outbound producers are covered. |
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

`ELP-130` owns the field matrix and evaluator test vectors; `ELP-310` owns evaluator code. `ELP-320` owns session/program/agenda backend projections, `ELP-440` owns both calendars, `ELP-650` owns JSON-LD/copy, and `ELP-700` is the final cross-surface proof. `ELP-420A` generates additive contracts for adoption; `ELP-420B` proves the final removal contract. `ELP-505` and `ELP-515` are one indivisible implementation lane and are never checked separately.

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
- two owned Homes in two tenants are both erased by global deletion;
- tenant membership removal erases neither global PII nor other-tenant Homes;
- application transaction rollback leaves PII, durable labels, state, user, local checkpoint, and outbox unchanged while the authority intent remains pending for replay;
- authority append retry returns the same sequence, and a fresh application database replays from sequence zero before traffic;
- crash after commit finds correction outbox rows already present;
- correction dispatcher is concrete, idempotent, retryable, dead-letter visible, and never carries address values;
- public endpoint with an auth cookie remains byte-for-byte public-only;
- attendee and management endpoints are private/no-store;
- tightened policy/governance defeats stale caches and external projections;
- public calendar is public-only; attendee calendar is authorized/no-store;
- email, webhook, ticket, export, search, MCP, federation, and reports cannot bypass disclosure authority;
- backup restore replays erasure before traffic;
- discovery point is never auto-created from PII and is removed during erasure;
- batch projection stays within query and authorization count budgets;
- server time, not client time, controls delayed reveal;
- Private Home default is generic/no identifying public fields plus ConfirmedParticipant.

Each acceptance case has one primary automated owner; cross-layer cases may add narrower tests but cannot move the primary proof:

| Acceptance owner | Primary test project / file | Task |
|---|---|---|
| Legacy kind/state backfill | `tests/Event.Persistence.IntegrationTests/Migrations/EventLocationBackfillTests.cs` | `ELP-230B` |
| Location lifecycle and resurrection rejection | `tests/Event.Domain.UnitTests/LocationPrivacyLifecycleTests.cs` | `ELP-120` |
| Home label/room tombstoning | `tests/Event.Persistence.IntegrationTests/Privacy/GlobalLocationPrivacyErasureTests.cs` | `ELP-130`, `505` |
| Independent event policies and TBA XOR | `tests/Event.Domain.UnitTests/EventLocationTests.cs` | `ELP-125`, `150` |
| Public contract identifier boundary | `tests/Event.API.IntegrationTests/Features/EventLocationControllerTests.cs` | `ELP-300`, `405` |
| Registration intent scope/lifecycle coverage | `tests/Event.Application.UnitTests/Services/EventLocationRegistrationAccessServiceTests.cs` | `ELP-200`, `225` |
| Global two-tenant erasure and membership separation | `tests/Event.Persistence.IntegrationTests/Privacy/GlobalLocationPrivacyErasureTests.cs` | `ELP-500`, `505`, `510` |
| Erasure/outbox atomicity and crash recovery | `tests/Event.Persistence.IntegrationTests/Privacy/GlobalLocationPrivacyErasureTests.cs` | `ELP-500`, `505`, `515` |
| Public-cookie equivalence and private/no-store routes | `tests/Event.API.IntegrationTests/Features/EventLocationControllerTests.cs` | `ELP-400`, `405` |
| Governance/cache invalidation | `tests/Event.API.IntegrationTests/Features/EventLocationGovernanceTests.cs` | `ELP-340`, `810` |
| Public/attendee calendar separation | `tests/Event.API.IntegrationTests/Features/EventCalendarPrivacyTests.cs` | `ELP-440` |
| Outbound producer boundary | tests beside each producer named by the ELP-020 inventory | `ELP-715` |
| Correction routing/reconciliation | `tests/Explore.Infrastructure.Tests/Infrastructure/CompositeOutboxMessageDispatcherTests.cs` plus API dead-letter tests | `ELP-520` |
| Restore replay startup gate | `tests/Event.API.IntegrationTests/Privacy/LocationPrivacyStartupGateTests.cs` | `ELP-525` |
| Discovery separation/erasure | `tests/Event.Persistence.IntegrationTests/Privacy/LocationDiscoveryPrivacyTests.cs`, or architecture absence proof while no discovery store exists | `ELP-730` |
| Batch query/authorization budget | `tests/Event.Persistence.IntegrationTests/Privacy/EventLocationDisclosureBatchTests.cs` | `ELP-315` |
| Server-time reveal and field matrix | `tests/Event.Application.UnitTests/Services/EventLocationDisclosureEvaluatorTests.cs` | `ELP-130`, `310`, `340` |
| Blazor HAL/disclosure/JSON-LD states | `tests/Explore.Blazor.Client.Tests/EventLocationPrivacyTests.cs` | `ELP-600`, `610`, `630`, `650` |
| Browser accessibility/responsive/RTL | `tests/Explore.Blazor.Client.E2ETests/EventLocationPrivacyE2ETests.cs` | `ELP-660`, `830` |
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
dotnet test --project tests/Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*/*[Category=EventLocationPrivacy]" --minimum-expected-tests 1 --no-progress
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1
```

Mark new tests with `[Category("EventLocationPrivacy")]` where supported so the focused E2E/security lane is stable. Also run migration SQL inspection, contract snapshot/OpenAPI generation, NSwag regeneration cleanliness, targeted output-cache tests, and browser-based responsive/accessibility/RTL journeys.

Completed-wave evidence on 2026-07-16: managed/API ELP 19/19; API public ELP 11/11; Application ELP 18/18; native Cerbos 461/461; full Application 2,317/2,317; full Blazor 1,702/1,702 executed with one not executed; MCP ELP at least 10, authenticated management at least 6, SDK at least 10; ELP-200 Domain ELP at least 30, Persistence ELP 20, own-cancel Application 22/22, PostgreSQL cancellation/race 8/8, architecture/parity 24/24; lookup Domain 2, Persistence 2, PostgreSQL startup/idempotency 1; lifecycle/aggregate Domain 50/50; final Domain ELP 63/63; registration access 42/42; disclosure contracts 17/17; EventLocation persistence 12/12 plus relational model 1/1; retained authority PostgreSQL 16/16; Clean Architecture 15/15. The full Release build passed 26 projects with 0 errors; focused Domain/Application/Persistence/Infrastructure builds passed with 0 errors and the completed W3-W5 lanes passed independent high/medium re-review. Current generated hashes are recorded in context. ELP-230A migration enforcement and browser visual/accessibility QA remain open.

## 19. Risks and Controls

| Risk | Control |
|---|---|
| Durable Home fields remain identifying after PII deletion | Contextual matrix, generic label, room tombstoning, adversarial erasure tests |
| Registration scope over-grants another day/session/location | Intent-coverage value object and exhaustive scope tests |
| Cross-tenant erasure bypass leaks or misses records | Named OwnerUserId-bounded query, architecture guardrail, two-tenant integration test |
| Authority append succeeds but app transaction does not | UUIDv7 idempotency, immutable pending intent, local replay checkpoint, startup replay |
| Partial commit loses correction event | Insert outbox in same transaction; rollback/crash tests |
| Default/no-op dispatcher silently drops correction | Explicit Composite route, concrete handler, startup/architecture tests, dead-letter alert |
| Legacy data becomes public accidentally | Unclassified backfill, conservative fail-closed EventLocation, review queue |
| Auth cookie changes public response and cache safety | Dedicated public-only route and equivalence test |
| Policy tightening leaves stale external data | PolicyVersion, correction outbox, cache purge, surface inventory |
| Backup restores erased PII | Independently retained authority database, sequence-zero replay for fresh app DB, pre-traffic evidence gate |
| Future proximity feature reuses exact PII | Separate discovery provenance/consent and no auto-copy rule |
| Large account erasure exceeds safe transaction limits | Instrument volume; require explicit durable-saga approval before changing v1 semantics |

## 20. Definition of Done

- All tasks in `event-location-privacy-tasks.md` are checked with evidence.
- Plan, context, and tasks remain synchronized after every implementation slice.
- All event-location outputs pass through the batch disclosure authority or document why they contain no location data.
- Irreversible Home erasure, global tenant coverage, outbox atomicity, restore replay, and remediation are proven by tests.
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
- stop for approval before weakening privacy, tenant boundaries, transactional atomicity, or irreversible-erasure rules;
- never claim a surface is migrated without repository evidence and a runnable check;
- complete with a technical teaching summary and explicit remaining risks.
