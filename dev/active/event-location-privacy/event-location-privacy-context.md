<!-- ABOUTME: Durable handoff context for the CTO-amended event-location privacy implementation workstream. -->
<!-- ABOUTME: Records verified source reality, locked decisions, rollout gates, blockers, and the exact next implementation step. -->

# Event Location Privacy Context

**Status:** CTO-amended plan authored; implementation not started  
**Last Updated:** 2026-07-15 Europe/Brussels  
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

### In Progress

- Planning-document consistency and final architecture quality gate only.

### Next

1. Complete `ELP-000`, `ELP-005`, and characterization tasks in order.
2. Do not implement Domain, Persistence, or API changes until the three amended documents have passed the final quality gate and a separate implementation session begins.
3. Update this context after every implementation task with changed paths, decisions, test evidence, failures, and the next exact task.

### Blockers

- Product implementation is blocked in this planning session.
- Domain/Persistence/API work beyond characterization is blocked until the amended plan, context, and tasks remain mutually consistent and the implementation agent records the gate as satisfied.
- Home Discovery current-location work is blocked until `ELP-005` corrects its stale `LocationListDto` privacy assumption and preserves coarse/server-side discovery boundaries.
- Public or attendee disclosure activation is blocked until migration ELP-230C proves zero missing/orphan/duplicate/tenant-mismatch EventLocation data.
- External correction is blocked from release until every new outbox event type has a concrete route in `CompositeOutboxMessageDispatcher`, idempotency tests, and dead-letter operations.

## Quick Resume

The target is not a field mask on `Location`. Physical place data remains in `Location` / optional `LocationPii`; a canonical first-class `EventLocation` owns per-event disclosure. `LocationKind` describes the place but never authorizes disclosure. `LocationPrivacyState` distinguishes `NotProvided`, `Active`, and irreversible `Erased`. Public, attendee, and management routes are separate. Attendee entitlement is resolved from registration intent scope/lifecycle. Global account deletion erases owned Private Homes across tenants and inserts correction outbox rows in the same transaction.

Start with `ELP-005`, then characterization and locked Domain tests. Do not revive the obsolete `EventLocationDisclosurePolicy` design.

## Verified Source Anchors

| Concern | Verified path | Current fact |
|---|---|---|
| Physical location | `src/Explore.Domain/Location.cs` | Durable `FullName`, country, city, timezone plus tenant/audit/concurrency; no privacy state/kind/owner yet. |
| Exact PII | `src/Explore.Domain/LocationPii.cs` | Street, postcode, latitude, longitude in shared-PK one-to-one. |
| Room data | `src/Explore.Domain/LocationRoom.cs` | Required Name and optional Description are outside PII. |
| EF mapping | `src/Explore.Persistence/Configurations/Entities/LocationConfiguration.cs` and `LocationPiiConfiguration.cs` | PII is required/auto-included; tenant filter applies through parent. |
| Location repository | `src/Explore.Persistence/Repositories/LocationRepository.cs` | Generic reads include PII; `ForgetPiiAsync` hard-deletes but has no account-erasure caller. |
| Public DTOs | `src/Explore.Application/DTOs/Location/LocationDto.cs` and `LocationListDto.cs` | Exact/identifying data is conflated with discovery/management contracts; list includes Address. |
| Anonymous API | `src/Explore.API/Controllers/LocationController.cs` | Public cached exact/list reads exist and must be replaced. |
| Public sessions | `src/Explore.Application/Features/EventSessions/Handlers/Queries/GetEventSessionDetailsRequestHandler.cs`, `GetSessionsByEventRequestHandler.cs`, `GetEventSessionListRequestHandler.cs` | AutoMapper projections can expose location fields without audience policy. |
| Public program | `src/Explore.Application/Features/EventPrograms/Handlers/Queries/GetEventProgramSummaryRequestHandler.cs` | Reads location names directly. |
| Public calendar | `src/Explore.Application/Features/Events/Handlers/Queries/GetEventCalendarExportRequestHandler.cs` | Builds public free-text location directly. |
| Browser/JSON-LD | `src/Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` | Public structured data and “register to see private address” copy rely on unrestricted upstream data. |
| Registration intent | `src/Explore.Domain/EventRegistrationIntent.cs` | Carries RegistrationScopeId and day-specific selection data. |
| Registration scope | `src/Explore.Domain/Enums/RegistrationScopeEnum.cs` | Event, Day, SessionSelection. |
| Policy rules | `src/Explore.Domain/Services/Registration/RegistrationPolicyRules.cs` | Maps allowed scope combinations for event registration modes. |
| Existing deletion | `src/Explore.Application/Features/Users/Handlers/Commands/DeleteUserCommandHandler.cs` | Erases UserPii/ActorPii but not owned Home locations. |
| Tenant membership | `docs/MULTI_TENANCY.md` | Tenant participation is separate from global User identity. |
| Outbox processing | `src/Explore.API/BackgroundServices/OutboxProcessor.cs` | At-least-once polling, retry, dead-letter lifecycle. |
| Concrete routing | `src/Explore.Infrastructure/Messaging/CompositeOutboxMessageDispatcher.cs` | Registered dispatcher throws on unknown event types; location correction route does not exist yet. |
| Dispatcher registration | `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs` | Composite dispatcher is the registered `IOutboxMessageDispatcher`. |
| Home Discovery overlap | `dev/active/home-discovery-experience/` | Correctly avoids generic exact coordinates but incorrectly claims generic LocationListDto omits private data. |
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

- `EventLocation` is the event-to-physical-location aggregate, not a policy side table.
- It owns field selection, full-details audience, optional server-time reveal, review status, policy version, concurrency, soft-delete, and audit.
- Public contracts expose EventLocationId and not unrestricted physical LocationId.
- Server auto-creates fail-closed associations.
- Final detach soft-deletes; reattach creates a fresh fail-closed association.
- The same physical Location can have independent policies on different events.
- Room disclosure is evaluated through EventLocation even when `LocationRoomId` remains physical.
- `EventLocation.IsToBeAnnounced` is an explicit organizer choice that suppresses every physical-location field; erasure or missing PII never sets it implicitly.

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
- Pending/Waitlisted qualify only for organizer-selected AnyCurrentRegistrant.
- Approved/Confirmed qualify for both attendee audiences.
- Rejected/Cancelled/Revoked/soft-deleted/expired deny.
- Null approval state is resolved using registration mode and intent lifecycle, never guessed.
- Private Home safe default is ConfirmedParticipant.

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

- Effective rules are the most restrictive instance/tenant combination.
- Keys: `location_privacy.allow_home_locations`, `allow_public_exact_address`, `allow_public_coordinates`, `minimum_home_audience`, `default_reveal_offset`.
- Reveal times use server UTC and do not bypass entitlement.

## Global Erasure Control Flow

One v1 database transaction:

1. Execute a named privacy-erasure query that explicitly bypasses Tenant filtering but is strictly bounded by OwnerUserId.
2. Lock owned Private Homes across all current/former tenant memberships.
3. Hard-delete LocationPii and derived discovery data.
4. Tombstone identifying Location and room fields.
5. Set privacy state Erased, timestamp/reason, and clear owner.
6. Mark affected EventLocations NeedsPrivacyReview and increment PolicyVersion.
7. Insert minimal erasure-ledger/tombstone rows.
8. Insert PII-free `LocationPiiErased` and external-correction outbox rows.
9. Complete existing User/Actor erasure.
10. Commit once.

After commit: best-effort cache eviction only. Background workers dispatch durable outbox work. Rollback changes nothing; crash after commit still leaves messages available. If volume proves one transaction unsafe, stop for approval before introducing a durable saga.

Tenant membership removal changes TenantUser/TenantUserProfile only and never invokes this global flow.

## Outbound Authority Inventory

Every implementation slice must audit these surfaces and record either the purpose-limited disclosure DTO used or proof that no location is present:

- event sessions, session groups, program, agenda, JSON-LD;
- public and attendee calendars;
- email confirmations/reminders and notifications;
- tickets and QR payloads;
- Svix webhooks;
- CSV/JSON exports;
- search indexes/projections;
- moderation and admin support views;
- API-key consumers;
- print/PDF/admin reports;
- MCP tools/resources and AI prompt context;
- federation/PDS records and correction flows;
- Home Discovery and future PostGIS projections.

Public ICS is public-only. Attendee ICS is authenticated/private/no-store. Private Home data never enters stable public subscription URLs. Operators/users must be warned that third-party imports may retain previously imported data even after correction.

## Backup and Restore Boundary

- Historical backups may contain erased PII until retention expiry; document the limit honestly.
- Retain a minimal erasure ledger/tombstone sufficient to reapply erasure after older backup restoration.
- Restore workflow replays erasures before application traffic, then purges caches/rebuilds indexes and replays external corrections.
- Traffic remains blocked if replay evidence fails.

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

- Stage A: anonymous minimization and missing-policy fail-closed.
- Stage B: additive schema and dual-write.
- Stage C: Unclassified/EventLocation idempotent backfill plus zero-gap verification.
- Stage D: policy-selected disclosure activation.

On failure, retain Stage A and additive schema, fix forward, rerun backfill, and never restore exact anonymous exposure. `Down` is valid only before contract activation and irreversible erasure.

## Validation Baseline

- Release build before planning: passed, 0 errors, 2,776 pre-existing warnings.
- Previous architecture-suite verification: 276 total, 275 passed, 1 documented skip, 0 failed.
- Planning-only changes require rerunning the architecture suite and `git diff --check` before handoff.
- Implementation will use TDD across Domain, Application, Persistence, API, Architecture, Blazor component/integration, and browser accessibility/visual journeys.

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
