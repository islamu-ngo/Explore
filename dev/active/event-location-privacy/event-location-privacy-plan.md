<!-- ABOUTME: Decision-complete implementation plan for contextual event-location disclosure, irreversible Home-location erasure, and audience-scoped access. -->
<!-- ABOUTME: Defines the canonical EventLocation model, safe rollout, transactional correction outbox, API boundaries, and verification contract. -->

# Event Location Privacy Implementation Plan

**Status:** Planning complete; implementation not started  
**Last Updated:** 2026-07-15 Europe/Brussels  
**Intent:** Cross-cutting fallback contract composed from `add-cqrs-handler`, `update-repository-query`, `add-ef-migration`, `add-get-endpoint`, `add-write-endpoint`, `openapi-contract-change`, `add-hal-link`, `cerbos-policy-change`, and `blazor-component-affordance`  
**Review:** Senior CTO amendments incorporated; implementation remains blocked until this plan, context, and task checklist stay aligned

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

## 4. Verified Current State

| Evidence | Current behavior | Planning consequence |
|---|---|---|
| `src/Explore.Domain/Location.cs` and `LocationPii.cs` | Durable name/city/country/timezone are on `Location`; street/postcode/coordinates are in required one-to-one `LocationPii`. | Extend this boundary, make PII lifecycle explicit, and recognize durable fields can identify a Home. |
| `src/Explore.Domain/LocationRoom.cs` | Room name and description live outside PII. | Classify and redact/tombstone room data contextually. |
| `src/Explore.Persistence/Repositories/LocationRepository.cs` | Reads auto-include PII; `ForgetPiiAsync` exists but has no account-erasure caller. | Split public/management reads and wire irreversible erasure. |
| `src/Explore.Application/DTOs/Location/LocationDto.cs` and `LocationListDto.cs` | Generic contracts expose exact or identifying data; `LocationListDto` includes Address. | Remove anonymous exact contracts and correct Home Discovery assumptions. |
| `src/Explore.API/Controllers/LocationController.cs` | Anonymous exact/list endpoints are output-cached. | Remove generic public exact `GET /Location/{id}`; introduce event-scoped routes. |
| `src/Explore.Application/DTOs/EventSession/EventSessionDto.cs` and public session handlers | Session projections expose location address/city/country without audience evaluation. | Route all event-location projection through one authority. |
| `src/Explore.Application/Features/EventPrograms/Handlers/Queries/GetEventProgramSummaryRequestHandler.cs` | Public program output reads location names directly. | Batch-resolve public disclosure. |
| `src/Explore.Application/Features/Events/Handlers/Queries/GetEventCalendarExportRequestHandler.cs` | Public calendar composes location text directly. | Split public and attendee calendar authority. |
| `src/Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` | JSON-LD and copy derive location hints from public session data. | Consume server-disclosed contracts only; remove overpromising private-address copy. |
| `src/Explore.Domain/EventRegistrationIntent.cs`, `RegistrationScopeEnum.cs`, and `Services/Registration/RegistrationPolicyRules.cs` | Registration intent supports Event, Day, and SessionSelection scopes. | Entitlement must use effective intent coverage, not row existence. |
| `src/Explore.Application/Features/Users/Handlers/Commands/DeleteUserCommandHandler.cs` | User PII and actor identifiers are erased, but owned Home locations are not. | Add global cross-tenant Home erasure in the same transaction. |
| `src/Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs` | The registered dispatcher throws for unknown event types and routes only known handlers. | Add explicit location-privacy correction routes and tests. |
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

Create `src/Explore.Domain/EventLocation.cs` as the first-class event-to-physical-location association. It owns:

- `Id`, `TenantId`, `EventId`, `LocationId`;
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

- The server creates a fail-closed `EventLocation` whenever a physical location is first attached to an event.
- Clients never need to construct a correct policy to establish the association.
- One physical `Location` may have independent policies on multiple events.
- Event/session/agenda public contracts expose `EventLocationId`, not physical `LocationId`.
- `LocationRoomId` may remain linked to the physical location, but room disclosure is evaluated through `EventLocation`.
- Event-local references migrate toward `EventLocationId`, including sessions, session groups, agenda items, and session agenda items.
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

Coverage rules:

- Event scope covers every eligible `EventLocation` used by the event.
- Day scope covers only `EventLocation` values used by eligible sessions/items on the selected event day.
- SessionSelection covers only the selected sessions’ `EventLocation` values.
- No active intent means no attendee entitlement.
- Pending/Waitlisted qualify only for `ANY_CURRENT_REGISTRANT` when the organizer explicitly chose that broad audience.
- Approved/Confirmed qualify for `ANY_CURRENT_REGISTRANT` and `CONFIRMED_PARTICIPANT`.
- Rejected, Cancelled, Revoked, soft-deleted, or expired intents/registrations deny.
- Null approval status is resolved from registration mode and intent lifecycle; it is never treated as implicitly approved or pending.
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

Add instance defaults with tenant overrides, where effective behavior is always the most restrictive:

- `location_privacy.allow_home_locations`
- `location_privacy.allow_public_exact_address`
- `location_privacy.allow_public_coordinates`
- `location_privacy.minimum_home_audience`
- `location_privacy.default_reveal_offset`

Disabling Home locations blocks new Home creation but does not silently delete existing data. Tightening governance increments/effectively invalidates policy versions, purges affected caches, marks incompatible associations for review, and enqueues correction work transactionally when external projections may have received broader data.

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

- Public ICS uses public disclosure only and may be cached only after policy-version invalidation is proven.
- Attendee ICS is a separate authenticated `private, no-store` route.
- Private Home data never appears in a stable public subscription URL.
- UI/operator copy warns that third-party calendar imports cannot be remotely retracted; policy tightening still emits correction where supported.
- Every email confirmation/reminder, ticket/QR payload, Svix webhook, CSV/JSON export, search projection, moderation view, API-key response, print/PDF/admin report, MCP tool/resource, federation/PDS record, discovery result, JSON-LD block, session/program/agenda projection, and notification must consume a purpose-limited disclosure result or prove it carries no location fields.
- AI/MCP output still passes through `IAiContextGateway`; the location service does not bypass AI disclosure policy.

## 12. Global Erasure and Transactional Correction

Global account deletion and tenant membership removal are different operations.

### Global account deletion

Add a named privacy-erasure repository/query that explicitly bypasses the Tenant filter and is strictly bounded by `OwnerUserId`. It enumerates owned Private Home locations across all current and former tenants. Runtime code must not use unrestricted `IgnoreQueryFilters()`.

For v1, one database transaction performs:

1. load and lock all owned Home locations across tenants;
2. hard-delete each `LocationPii`;
3. tombstone identifying `Location.FullName` and Home room names/descriptions;
4. set `LocationPrivacyState=ERASED`, erasure timestamp/reason, and clear owner;
5. delete/deactivate any derived discovery point;
6. mark affected active `EventLocation` rows `NeedsPrivacyReview=true` and increment policy version;
7. insert append-only erasure-ledger/tombstone records;
8. insert `LocationPiiErased` and required external-correction `OutboxMessage` rows with only IDs, versions, and correction intent;
9. complete existing User/Actor erasure;
10. commit once.

Rollback before commit leaves PII, durable labels, privacy state, user erasure, and outbox unchanged. A crash after commit finds correction outbox rows already durable. After commit, only best-effort cache eviction occurs; `OutboxProcessor` performs at-least-once delivery.

Extend `src/Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs` with explicit location-privacy event routes and an idempotent concrete dispatcher. Unknown routes remain fatal/retryable and eventually dead-letter. Tests prove dispatch, duplicate delivery safety, retry, dead-letter visibility, and operator reconciliation.

If measured transaction volume makes the single transaction unsafe, stop and obtain approval for a durable saga before changing semantics; do not silently introduce partial erasure.

### Tenant membership removal

Tenant-admin removal changes `TenantUser` / `TenantUserProfile` participation only. It must not delete the global User, global UserPii, or Homes in this or other tenants unless a separate ownership-transfer workflow is completed.

## 13. Backup, Restore, and Remediation

Add `Event Location Privacy Erasure and Restore` to `docs/OPERATIONS.md`:

- backup retention and the limit that historical backups may still contain erased PII;
- minimal durable erasure ledger/tombstones retained outside ordinary logical restore scope;
- mandatory erasure replay after restoring an older backup and before serving application traffic;
- cache purge and search/index rebuild after replay;
- external projection correction replay and dead-letter inspection;
- evidence queries proving no resurrected PII or discovery point remains;
- incident process when replay fails.

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
- add `EventLocation`, policy audit, erasure ledger, and required indexes/constraints;
- add nullable `EventLocationId` references alongside existing physical references;
- make `LocationPii` optional without weakening tenant ownership;
- deploy fail-closed reads and dual-write for new/changed event-local references.

### ELP-230B: Backfill

- classify every existing location as `UNCLASSIFIED`, never Public;
- derive unique tenant/event/physical-location pairs from every session/group/agenda/room reference;
- create EventLocations with `ShowCountry=true`; all other fields false unless a separately recorded continuity decision allows city; audience `NEVER`; `NeedsPrivacyReview=true`;
- backfill `EventLocationId` references idempotently;
- never infer Private Home or owner from `CreatedBy`;
- emit review-queue metrics for Unclassified locations and unresolved EventLocations.

### ELP-230C: Validate and contract

- prove zero missing EventLocation references, orphan associations, duplicate active event/location pairs, tenant mismatches, invalid Home states, and resurrected Erased PII;
- switch event-local reads/writes to EventLocationId;
- make constraints required where validated;
- remove obsolete anonymous exact contract/routes and old physical event-local references only after all consumers migrate;
- preserve reversible `Down` only before contract activation and irreversible erasure; afterward use forward repair.

Safe activation stages:

- Stage A: ship public minimization and missing-policy fail-closed behavior.
- Stage B: expand schema and dual-write while reads remain Stage A-safe.
- Stage C: run idempotent backfill and zero-gap verification.
- Stage D: enable selected public/attendee disclosure only after the gate passes.

Failure keeps Stage A and additive schema active, fixes data/code, and reruns backfill. Never roll back to anonymous exact exposure.

## 16. Implementation Phases

### Phase 0: Characterization and dependency blockers

- Add leakage characterization tests before changing contracts.
- Execute `ELP-005` to correct all three `dev/active/home-discovery-experience` documents: `LocationListDto` currently exposes Address; generic exact address/coordinates must never be browser-enumerated; current-location work is blocked on coarse `PublicDiscoveryArea` or later governed PostGIS.
- Lock kind/state/erasure, global-vs-membership semantics, field matrix, route contracts, governance defaults, and surface inventory in all three Event Location Privacy docs.
- Record exact generated-client regeneration command and baseline API snapshots.

### Phase 1: Domain and tests

- Implement lookup entities, `Location` lifecycle, `EventLocation`, audit records, registration-access facts, TBA state, and domain invariants.
- Add adversarial Domain tests before implementation.

### Phase 2: Persistence and migration

- Implement configurations/repositories, named global erasure query, EventLocation batch reads, audit/ledger persistence, and migrations ELP-230A/B/C.
- Add PostgreSQL integration tests for filters, cross-tenant bounded erasure, concurrency, idempotent backfill, and rollback.

### Phase 3: Application disclosure authority

- Implement registration-intent coverage resolver, batch disclosure service, pure evaluator, DTOs, CQRS requests, governance composition, cache invalidation, and audit commands.
- Migrate every location-bearing public/attendee/management projection to the service.

### Phase 4: API, HAL, and generated contracts

- Implement the exact route split, resource authorization, no-store headers, HAL affordances, OpenAPI schemas, API changelog, and NSwag regeneration.
- Delete the anonymous exact location route after consumers migrate.

### Phase 5: Erasure, outbox, and operations

- Extend global user deletion and preserve tenant-membership semantics.
- Persist correction outbox rows inside the erasure transaction.
- Add concrete dispatch/reconciliation, restore replay, remediation workflow, metrics, alerts, and runbooks.

### Phase 6: Blazor management and attendee UX

- Replace raw location DTO use with purpose-specific generated contracts.
- Add kind/state/policy/reveal/governance controls, consent-backed owner transfer, review queue, TBA remediation, and attendee disclosure states.
- Gate mutations by HAL links and meet WCAG 2.2 AA, responsive, RTL, and localization requirements.

### Phase 7: Outbound and discovery audit

- Audit and migrate every outbound surface listed in Section 11.
- Update AI disclosure matrix/registry, federation correction, Home Discovery contract, calendar warnings, and PostGIS readiness documentation.

### Phase 8: Final verification and contract cleanup

- Run all project-specific suites, full architecture suite, Release build, generated-client cleanliness, migration SQL review, and real-browser visual/accessibility QA.
- Remove obsolete contracts only after all references and contract snapshots prove migration complete.

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
- transaction rollback leaves PII, durable labels, state, user, and outbox unchanged;
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

## 18. Verification Commands

Run at minimum:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --minimum-expected-tests 1
```

Also run migration SQL inspection, contract snapshot/OpenAPI generation, NSwag regeneration cleanliness, targeted output-cache tests, and browser-based responsive/accessibility/RTL journeys.

## 19. Risks and Controls

| Risk | Control |
|---|---|
| Durable Home fields remain identifying after PII deletion | Contextual matrix, generic label, room tombstoning, adversarial erasure tests |
| Registration scope over-grants another day/session/location | Intent-coverage value object and exhaustive scope tests |
| Cross-tenant erasure bypass leaks or misses records | Named OwnerUserId-bounded query, architecture guardrail, two-tenant integration test |
| Partial commit loses correction event | Insert outbox in same transaction; rollback/crash tests |
| Default/no-op dispatcher silently drops correction | Explicit Composite route, concrete handler, startup/architecture tests, dead-letter alert |
| Legacy data becomes public accidentally | Unclassified backfill, conservative fail-closed EventLocation, review queue |
| Auth cookie changes public response and cache safety | Dedicated public-only route and equivalence test |
| Policy tightening leaves stale external data | PolicyVersion, correction outbox, cache purge, surface inventory |
| Backup restores erased PII | Erasure ledger replay before traffic and restore evidence gate |
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
