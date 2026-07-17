ABOUTME: Documents the current domain model and persistence-enforced rules.
ABOUTME: Prioritizes non-inferable patterns (PII split tables, aspects, filters, and constraints).

# Domain Model

This project stores most entities directly under `Explore.Domain/` (not in an `Entities/` subfolder).

## Core Aggregates

1. Tenant and access scope:
   `Tenant`, `TenantUser`, `TenantUserRoleGrant`, `TenantSetting`, `TenantSettingsDocument`, `TenantNavigationLink`, `TenantInvitation`, `TenantLifecycleLog`
2. Identity and actor model:
   `User`, `Actor`, `ActorSubscription`, `Group`, `Organization`, `Role`, `Permission`, `RolePermission`, `PlatformUserRole`
3. Events:
   `Event`, `EventSession`, `EventRegistration`, `EventSessionSpeaker`, `EventSessionLanguage`, `EventSessionAgendaItem`, `Notification`, `NotificationFanoutRun`
4. Event reporting and moderation review:
   `EventReport`, `EventReportTarget`, `EventReportEvidence`, `EventReportCase`, `EventReportSignal`, `EventReportDecision`, `EventReportExternalLink`
5. Classification/lookups:
   `EventType`, `EventStatus`, `VisibilityType`, `EventFormat`, `RegistrationMode`, `Category`, `Tag`, `Language`, `Madhab`, `AudienceAge`, `AudienceGender`
6. Federation:
   `AtprotoRecord`, `IndexedDid`, `SyncState`, `ActorKeyStore`
7. Settings and governance:
   `SystemSetting`, `AppSetting`, `ConfigurationChangeLog`
8. Module governance:
   `ModuleDefinition`, `TenantCapability`, plus event aspect entities

## Normalized Lookup Families

Several previously enum-shaped persistence fields are now modeled as lookup/reference rows with stable integer IDs, stable `MasterCode` values, human-readable `FullName`, and optional `Description`. The persisted entity stores the `{LookupName}Id` FK plus a navigation; any enum property that remains on the domain entity is a convenience wrapper ignored by EF, not a database column.

| Domain field | Persisted FK | Lookup entity/table |
|---|---|---|
| `Role.Scope` | `RoleScopeId` | `RoleScope` / `role_scopes` |
| `Permission.Scope` | `RoleScopeId` | `RoleScope` / `role_scopes` |
| `SystemSetting.ValueType` | `SettingValueTypeId` | `SettingValueTypeLookup` / `setting_value_types` |
| `ConfigurationChangeLog.Scope` | `SettingScopeId` | `SettingScopeLookup` / `setting_scopes` |
| `SecretBinding.Scope` | `SettingScopeId` | `SettingScopeLookup` / `setting_scopes` |
| `SecretBinding.SourceType` | `SecretSourceTypeId` | `SecretSourceTypeLookup` / `secret_source_types` |
| `SecretBinding.LastValidationResult` | `SecretValidationStatusId` | `SecretValidationStatus` / `secret_validation_statuses` |
| `ExternalApiKey.OwnerType` | `ExternalApiKeyOwnerTypeId` | `ExternalApiKeyOwnerTypeLookup` / `external_api_key_owner_types` |
| `ExternalApiKey.Status` | `ExternalApiKeyStatusId` | `ExternalApiKeyStatus` / `external_api_key_statuses` |
| `ExternalApiKey.CreditPeriod` | `ExternalApiKeyCreditPeriodId` | `ExternalApiKeyCreditPeriod` / `external_api_key_credit_periods` |
| `Notification.Scope` | `NotificationScopeId` | `NotificationScopeType` / `notification_scope_types` |
| `NotificationChannelPreference.Category` | `CategoryId` | `NotificationPreferenceCategory` / `notification_preference_categories` |
| `NotificationChannelPreference.Channel` | `ChannelId` | `NotificationPreferenceChannel` / `notification_preference_channels` |
| `ActorSubscription.Status` | `StatusId` | `ActorSubscriptionStatus` / `actor_subscription_statuses` |
| `ActorSubscription.NotificationLevel` | `NotificationLevelId` | `ActorSubscriptionNotificationLevel` / `actor_subscription_notification_levels` |
| `Event.SessionStatus` | `EventSessionStatusId` | `EventSessionStatus` / `event_session_statuses` |

API DTOs expose lookup primitives (`*Id`, `*Code`, `*Name`) rather than domain enum values. Repositories query on the normalized FK IDs. Handlers may convert IDs to internal enums only for business-rule switches while keeping persistence and public contracts normalized.

## Non-Inferable Modeling Patterns

### 1) PII Split (1:1 extension tables)

Some entities keep sensitive fields in dedicated PII tables and expose convenience properties via `NotMapped` wrappers. This allows hard-deletion of PII while preserving the main entity for auditing/history.

- `User` -> `UserPii` (`Email`, `FirstName`, `LastName`)
- `Organization` -> `OrganizationPii` (`FullName`, `Email`, address fields)
- `Actor` -> `ActorPii` (`DisplayName`, `Did`, `Handle`, `ProfilePictureUri`)
- `Location` -> `LocationPii`

`EnsurePii()` helper methods create PII objects lazily when mapped properties are set.

Exact proximity discovery is **not implemented**. [ADR-013](adr/ADR-013-postgis-proximity-discovery.md) proposes a separate governed `LocationDiscoveryPoint` projection for a future PostGIS phase. It would be tenant-scoped, explicitly approved, revocable, and stored as `geography(Point,4326)` with GiST indexing; it would not replace or automatically publish `LocationPii` coordinates. Generic location DTOs remain coordinate-free.

The proposed distance unit is an eligible future public `EventSession` occurrence: scheduled, published, non-deleted, tenant-matching, under a public published event, and attached to a location with an active governed point. Online-only, past, draft, private, moderated, unscheduled, deleted, or unapproved occurrences do not participate. PostgreSQL would select the minimum qualifying occurrence distance per event; no current entity, migration, or runtime query provides that capability.

### 2) Optional Event Aspects (Layer 2 typed schema)

Base event data stays in `Event`. Optional modules add 1:1 aspect records sharing the same primary key:

- `EventIslamicAspect` (Id = Event.Id)
- `EventTechAspect` (Id = Event.Id)
- `EventSessionIslamicAspect` (session-level extension)

Aspects are optional; an event/session can exist without aspect rows. Sector-standard semantics belong here, not only in Layer 3 custom properties.

`EventSessionIslamicAspect` owns Islamic session scheduling metadata without changing the UTC schedule source of truth.

- **Start Time:** `StartTimeType = Fixed` means the session's UTC `StartTime` is authoritative and `ReferencePrayer`/`OffsetMinutes` must be null. `StartTimeType = RelativeToPrayer` requires `ReferencePrayer` and `OffsetMinutes` (constrained to `-180..180` minutes); application validation also requires `LocationId` so prayer-time resolution has a location anchor.
- **End Time:** Exposes flexible ending logic via `EventSession.EndTimeType` (`Fixed`, `OpenEnded`, `RelativeToPrayer`). When `EndTimeType = RelativeToPrayer`, the ending is relative to `EndReferencePrayer` and `EndOffsetMinutes` (constrained to `-180..180` minutes) on `EventSessionIslamicAspect`. When `EndTimeType = OpenEnded`, the session does not have a set end time and `EndTime` is stored as null.

EF/PostgreSQL check constraints enforce the fixed/relative field shapes, offset ranges, and prayer enum ranges.

### 3) Event And Session Lifecycle

Events remain a single aggregate table. Draft, published, cancelled, completed, archived, and moderated event states are represented by `Event.EventStatusId`; there is no separate event-draft table. Lifecycle writes use explicit commands such as publish, archive, cancel, moderation, and import instead of a generic public status update.

Event sessions also remain normal `EventSession` rows. Draft/internal sessions are represented by `EventSessionStatusId = Draft`, can be unscheduled, and are hidden from anonymous/public program surfaces until they are scheduled and published. This allows a published event to own an internal draft session without leaking it through public session list/detail, program summary, calendar export, agenda projection, or event-list schedule facets. Session publication is subordinate to event publication: an `EventSession` cannot move to `Published` unless its parent `Event` is already `Published`.

Session moderation is event-scoped, not independently session-scoped. Light event moderation moves every session in the event to `Moderated`. Heavy event moderation also redacts event-owned session text/custom-property values to `Redacted`, clears session image references, and moves the sessions to `Moderated`. If one session violates listing rules, the entire event is removed from listing because sessions are tightly bound to the event container.

`EventSessionStatus` is a seeded lookup with stable IDs/codes:

| ID | Code | Meaning |
|---:|---|---|
| 1 | `DRAFT` | Internal editable session draft. |
| 2 | `SUBMITTED` | Submitted for review. |
| 3 | `UNDER_REVIEW` | Under active review. |
| 4 | `APPROVED` | Approved but not public. |
| 5 | `PUBLISHED` | Publicly visible when the parent event is public/published and the session is scheduled. |
| 6 | `REJECTED` | Rejected during review. |
| 7 | `CANCELLED` | Cancelled and not public. |
| 8 | `ARCHIVED` | Archived and not public. |
| 9 | `COMPLETED` | Completed and not actionable for public publishing. |
| 10 | `MODERATED` | Hidden through event-level moderation. |

### 4) Event Reporting And Moderation Review

`EventReport` is the tenant-scoped aggregate for user-facing event reports. It references the reported event, optional reporter user/actor identity, reason code, report status, priority, severity hint, duplicate grouping, reporter contact consent, and hashed reporter fingerprints. Reporter IP/User-Agent fingerprints are hashed at the API boundary before the command leaves the controller.

The aggregate owns:

- `EventReportTarget` rows for event/session/field/storage-object targets.
- `EventReportEvidence` rows for sensitive evidence. Reporter text is encrypted before persistence and exposed only through authorized management detail projections.
- `EventReportCase` rows for local moderation queue state, SLA, assignment, and optimistic concurrency.
- `EventReportSignal` rows for bounded provider verdict metadata such as Osprey signals.
- `EventReportDecision` rows for local moderator or provider decisions before enforcement.
- `EventReportExternalLink` rows for provider sync state, retry metadata, external case/signal IDs, and correlation IDs.

Submit-report writes create the report, primary target, encrypted reporter-text evidence, initial local case, and provider-sync outbox intent in one unit-of-work transaction. The outbox payload is metadata-only: it carries tenant/report/event/case IDs, reason/status/priority codes, idempotency/correlation metadata, and evidence descriptors. It must not contain reporter text, reporter IP hashes, user-agent hashes, event titles, slugs, URLs, raw provider payloads, provider secrets, or raw exception text.

Moderation review is CQRS-driven. Triage, assignment, decision capture, and decision execution all require event-management authorization and validate the report/event/case graph plus `EventReportCase.ConcurrencyStamp`. Executable decisions reuse the existing light-moderation and heavy-redaction command paths rather than writing event moderation state directly. When a report decision enforces moderation, the resulting `EventModerationRecord` links back to `SourceReportId` and `SourceReportDecisionId`.

Provider integrations remain metadata-only. Osprey signals and Coop review-queue/callback state are stored as bounded codes and external IDs with idempotency indexes. Signed, authenticated Coop callbacks are retained with one unique `IncomingWebhookEffectOutbox` pointer. The pointer's fenced worker loads and revalidates the retained callback, invokes canonical decision execution outside intake, and commits the applied-effect receipt with pointer completion only after command success. Retryable failures reschedule; poison callbacks dead-letter for authenticated, generation-checked operator redrive. Osprey remains signal-only.

### 5) Event Schedule Source Of Truth

Event scheduling uses UTC instants as the authoritative write model when a session or agenda item is scheduled. `EventSession.StartTime/EndTime` are nullable for draft-capable sessions; `EventAgendaItem.StartTime/EndTime` remain required because agenda items represent concrete schedule blocks. Local dates, local times, and minute-of-day values are generated by the domain scheduling services and persisted only as query/display projections.

The approved write paths are:

- `EventSession.Reschedule(...)` and `EventAgendaItem.Reschedule(...)` for scheduled child items.
- `Event.ApplyScheduleTimeZone(...)` when an event timezone changes and the full schedule graph is loaded for update.
- `Event.RecalculateScheduleSummaryFromSessions()` for event-level schedule rollups.

`ScheduleTimeZoneResolver` normalizes blank timezone input to UTC and validates non-blank values with `TimeZoneInfo.FindSystemTimeZoneById`. Invalid timezone IDs fail validation instead of silently falling back. `Timezone` and `EventTimeZoneId` are treated as aliases during writes and are kept in sync while the product is still in development.

Database constraints provide defense in depth:

- event schedule rollups cannot store inverted first/last local dates or UTC starts;
- event timezone IDs cannot be blank strings;
- scheduled session and agenda item end times must be strictly after start times;
- session local projection constraints are conditional so unscheduled drafts can keep all schedule projection columns null;
- persisted local minute-of-day values must match persisted local time fields and stay within `0..1439` when present;
- active room-bound scheduled sessions cannot overlap in the same tenant/location/room. `EventSessionConfiguration` declares `EX_EventSession_RoomNoOverlap` as model-owned PostgreSQL metadata, and `PostgresModelConstraintApplier` applies the GiST exclusion constraint over `tstzrange(StartTime, EndTime, '[)')` only when `StartTime` and `EndTime` are non-null; adjacent sessions are allowed and soft-deleted or unscheduled sessions release the room.

PostgreSQL generated columns were not selected for timezone projection ownership because timezone conversion depends on system timezone data and is a poor fit for immutable generated expressions. Keeping projection ownership in the domain/application layer preserves deterministic tests, explicit validation, and Clean Architecture boundaries.

### 6) Layer 3 Governed Custom-Property Extension Model

The platform provides a flexible EAV-based extension system across multiple scopes:

- **Shared Definitions**: `CustomPropertyDefinition` for Organization and Group extensions, plus "Shared Event Definitions".
- **Event Templates**: `EventTemplate` blueprints with `EventTemplateCustomPropertyDefinition`.
- **Event Runtime**: `EventCustomPropertyDefinition` tied to specific events, materialized from templates or created directly.
- **Event Values**: `EventCustomPropertyValue` stores typed runtime data with multi-value ordinal support.
- **Event Session Runtime**: `EventSessionCustomPropertyDefinition` and `EventSessionCustomPropertyValue` mirror the event model for scheduled child content.
- **Projections**: `EventCustomPropertyProjection` and `EventSessionCustomPropertyProjection` provide denormalized read models for discovery/filtering.

**Key Rule**: Layer 3 exists for long-tail extensions. Standard sector fields must use Layer 2 typed schema.

Explicit admin purge is the only hard-delete path for dependency-free custom-property definitions. Normal delete remains retire + soft delete so historical values, projections, and audit evidence stay recoverable.

### 7) Polymorphic Reference Registry

Polymorphic references that cannot use a direct FK are governed by `Explore.Domain.References.ReferenceTypeRegistry`. The registry is the domain source of truth for target kind, ID shape, ownership, tenant-scope rule, cleanup behavior, and validation wording. Current registries cover:

- `ExternalBinding`: allowed external/internal type pairs from `ExternalBindingTypes`, including the tenant/customer provisioning binding, admin user, tenant-local user state, profile, actor, login, organization, and group organizer bindings.
- `Notification`: every `NotificationEntityTypeEnum` value maps to a registered target kind. `Notification.EntityId` is a string column for compatibility with lookup-driven deep links, but registered targets currently require Guid-form entity IDs and retain historical references when the linked entity is deleted or hidden.
- Shared custom properties: every `EntityTypeName` value is represented. `Organization` and `Group` support shared `CustomPropertyDefinition`/`CustomPropertyValue` rows. `Event` is deliberately registered as unsupported for shared definitions because event custom properties use `EventCustomPropertyDefinition`, `EventCustomPropertyValue`, and template materialization instead.

Write-time enforcement happens at the repository/application boundary: external-binding, notification, and shared custom-property definition writes validate against the registry before saving. EF model metadata also declares check constraints for registered external-binding pair/scope combinations, shared custom-property target types, and notification entity reference shape. Migration regeneration is intentionally separate in the development workflow, so the registry and repository guards remain the immediate runtime enforcement until generated migrations are refreshed.

### 8) Tenant and Soft-Delete Interfaces

Isolation and lifecycle are enforced via marker interfaces:

- `ITenantEntity` -> `TenantId` (Global filter in DbContext)
- `IAuditableEntity` -> `CreatedAt/By`, `UpdatedAt/By` (Auto-populated in SaveChanges)
- `ISoftDeletable` -> `IsDeleted`, `DeletedAt/By` (Converted from Delete state in SaveChanges)

### 9) Tenant-Local User Authority

`TenantUser` is the tenant-local user root. It owns tenant participation status, moderation lifecycle, actor/profile links, and soft-delete state for a global `User` inside one tenant.

Tenant role authority is represented by `TenantUserRoleGrant`, not by a direct `User`/`Tenant` membership row. The database enforces this with:

- a composite FK from `TenantUserRoleGrant(TenantId, TenantUserId)` to `TenantUser(TenantId, Id)`;
- a composite FK from `TenantUserRoleGrant(RoleId, RoleScopeId)` to `Role(Id, RoleScopeId)`;
- a check constraint forcing `RoleScopeId = Tenant`;
- a filtered unique index allowing only one active grant per `(TenantId, TenantUserId, RoleId)`.

Revocation is explicit (`RevokedAt`, `RevokedBy`, `RevocationReason`) so historical authority evidence remains auditable while active checks ignore revoked grants.

### 10) Actor Subscriptions And Notification Fanout

`ActorSubscription` is the canonical durable relationship for user subscriptions to subscribable actors. V1 supports organization and group target actors only. The subscription stores the active tenant-local subscriber (`SubscriberTenantUserId`), denormalized global `SubscriberUserId` for notification delivery, target actor, target actor type, subscription status, notification level, audit fields, soft-delete fields, and a concurrency stamp.

Unsubscribe is modeled as a status transition to `UNSUBSCRIBED`, not as deletion. Resubscribe reactivates the same durable row and resets the notification level to the v1 default. Command handlers and fanout scans require an active, non-deleted `TenantUser` so suspended, banned, removed, or deleted tenant-local users do not receive subscription fanout.

`Notification.DeduplicationKey` is required for fanout-created notifications. Event-published fanout uses deterministic keys so outbox retries or duplicate internal dispatches do not create duplicate inbox rows for the same tenant/event/subscriber tuple.

`NotificationFanoutRun` records resumable worker state for a fanout source: tenant, fanout kind, entity type, entity ID, source actor, status, subscriber cursor, aggregate processed/created counts, failure text, and timestamps. It intentionally stores no PII.

Notification preference matrix state is normalized separately from the in-app `Notification` rows. `NotificationPreferenceCategory` and `NotificationPreferenceChannel` are stable lookup rows; `NotificationChannelPreference` stores scoped category/channel choices; `NotificationPreferenceProfile` stores scoped global mute state. Preference rows are tenant-scoped, soft-deletable, audited, concurrency-aware, and constrained so user, organization, and group scopes carry exactly the matching target id.

Delivery services call the effective notification preference resolver before creating non-required in-app fanout rows. Required categories, such as trust-safety, remain enabled through category metadata and resolver output rather than by client-side checks.

## Messaging and Reliability

### OutboxMessage

Transactional outbox entity for reliable asynchronous event dispatch (at-least-once delivery):

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | UUID v7 (time-sortable) |
| `AggregateType` | `string` | e.g., "Event", "Actor" |
| `AggregateId` | `Guid` | Source entity ID |
| `EventType` | `string` | Event classification |
| `Payload` | `string?` | JSONB serialized data |
| `Status` | `Enum` | Pending, Processing, Completed, Failed, DeadLettered |
| `NextRetryAt` | `DateTime?`| Exponential backoff schedule |

Event publication writes an internal `EventPublishedNotificationFanoutRequested` outbox message for actor-subscription fanout. The fanout message is routed by the composite dispatcher to the application fanout service, which writes durable `Notification` rows and advances `NotificationFanoutRun` state idempotently. External broker publication for `EventPublished` is retired from this workstream; future integration-event broker work needs a separate product requirement and failure model.

Specialized variants: `PdsSyncOutbox` (federation), `PolicyChangeOutbox` (governance), `EmailDispatchOutbox` (basic email dispatch state).

## Persistence-Enforced Rules (from EF configuration)

- `Event.Title`: Required, max 200.
- `Event.Price`: Precision (19,4), non-negative constraint.
- `Event.EventTimeZoneId`: Optional, max 100; blank strings rejected.
- `Event`: Schedule rollups reject inverted first/last local date and UTC start ranges.
- `EventSession`: `EventSessionStatusId` is required; schedule and local projection fields are nullable for drafts; if a schedule is present, UTC end must be after UTC start and local minute projections must match local time projections.
- `EventAgendaItem`: UTC end must be after UTC start; local minute projections must match local time projections.
- `AppSetting`: Blocks high-value secret keys (e.g., `Database:`, `ConnectionStrings:`) via DB constraint.
- `Actor`: Unique nullable owner FKs (exactly one of UserId, OrganizationId, or GroupId).
- `ActorSubscription`: Unique non-deleted subscription row per `(TenantId, SubscriberTenantUserId, TargetActorId)`; target actor type is limited to organization/group in v1.
- `Notification`: Fanout rows require deterministic `DeduplicationKey` for duplicate prevention.
- `NotificationChannelPreference`: Unique non-deleted row per tenant/scope/target/category/channel; scope-target check constraints enforce no target for system/instance/tenant scopes and exactly one matching target for organization, group, or user scopes.
- `NotificationPreferenceProfile`: Unique non-deleted row per tenant/scope/target for global mute state with the same scope-target constraints.
- `NotificationFanoutRun`: Unique source tuple per `(TenantId, FanoutKind, NotificationEntityTypeId, EntityId, SourceActorId)`.
- `EventReport`: Composite tenant/event alternate keys enforce same-tenant event ownership; status/priority/reporter/source enum ranges are DB constrained; terminal statuses require `ClosedAt`.
- `EventReportCase`: Composite tenant/report/case keys enforce queue ownership; queue code is required; status/priority ranges are constrained; concurrency stamp is the optimistic write guard.
- `EventReportEvidence`: Reporter-text evidence rows require encrypted text; content hashes are optional but non-blank when present; retention and content-hash indexes support cleanup/deduplication without exposing raw evidence.
- `EventReportDecision`: Local decisions require `ModeratorUserId`; provider decisions may use external decision IDs with a tenant/source uniqueness guard.
- `EventReportExternalLink` and `EventReportSignal`: Provider correlation/external IDs are unique per tenant/provider and store bounded failure categories only.

## Related
- [ARCHITECTURE.md](ARCHITECTURE.md)
- [CUSTOM_PROPERTIES.md](CUSTOM_PROPERTIES.md)
- [MULTI_TENANCY.md](MULTI_TENANCY.md)
- [OUTBOX_PATTERN.md](OUTBOX_PATTERN.md)
