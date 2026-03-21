ABOUTME: Documents the current domain model and persistence-enforced rules.
ABOUTME: Prioritizes non-inferable patterns (PII split tables, aspects, filters, and constraints).

# Domain Model

This project stores most entities directly under `Explore.Domain/` (not in an `Entities/` subfolder).

## Core Aggregates

1. Tenant and access scope:
   `Tenant`, `TenantMember`, `TenantUser`, `TenantSetting`, `TenantSettings`, `TenantNavigationLink`, `TenantInvitation`, `TenantLifecycleLog`
2. Identity and actor model:
   `User`, `Actor`, `Group`, `Organization`, `Role`, `Permission`, `RolePermission`, `PlatformUserRole`
3. Events:
   `Event`, `EventSession`, `EventRegistration`, `EventSessionSpeaker`, `EventSessionLanguage`, `EventSessionAgendaItem`, `Notification`
4. Classification/lookups:
   `EventType`, `EventStatus`, `VisibilityType`, `EventFormat`, `RegistrationMode`, `Category`, `Tag`, `Language`, `Madhab`, `AudienceAge`, `AudienceGender`
5. Federation:
   `AtprotoRecord`, `IndexedDid`, `SyncState`, `ActorKeyStore`
6. Settings and governance:
   `SystemSetting`, `AppSetting`, `ConfigurationChangeLog`
7. Module governance:
   `ModuleDefinition`, `TenantCapability`, plus event aspect entities

## Non-Inferable Modeling Patterns

### 1) PII Split (1:1 extension tables)

Some entities keep sensitive fields in dedicated PII tables and expose convenience properties via `NotMapped` wrappers:

- `User` -> `UserPii` (`Email`, `FirstName`, `LastName`)
- `Organization` -> `OrganizationPii` (`FullName`, `Email`, address fields)
- `Actor` -> `ActorPii` (`DisplayName`, `Did`, `Handle`, `ProfilePictureUri`)
- `Location` -> `LocationPii`

`EnsurePii()` helper methods create PII objects lazily when mapped properties are set.

### 2) Optional Event Aspects (vertical partitioning / Layer 2 typed schema)

Base event data stays in `Event`. Optional modules add 1:1 aspect records:

- `EventIslamicAspect` (shared primary key `Id = Event.Id`)
- `EventTechAspect` (shared primary key `Id = Event.Id`)
- `EventSessionIslamicAspect` (session-level extension with key `EventSessionId`)

Aspects are optional; an event/session can exist without aspect rows.

These aspect families are the current Layer 2 precedent. Sector-standard semantics belong here, not only in Layer 3 custom properties.

### 3) Layer 3 custom-property extension model

The repo now contains a strengthened Layer 3 custom-property family:

- shared definitions for `Organization` and `Group` via `CustomPropertyDefinition`, `CustomPropertyOption`, and `CustomPropertyValue`
- event template entities via `EventTemplate`, `EventTemplateCustomPropertyDefinition`, and `EventTemplateCustomPropertyOption`
- event-local runtime entities via `EventCustomPropertyDefinition`, `EventCustomPropertyOption`, and `EventCustomPropertyValue`
- derived read-side projection rows via `EventCustomPropertyProjection`

Planned next extension of the same architecture:

- session template entities under the parent event template
- session-local runtime entities for `EventSession`
- derived session projection rows and aggregate event-with-sessions read views

Important boundary rule:

- Layer 3 exists for tenant-specific and organizer-specific long-tail extensions.
- Layer 3 must not become the only home of filtering, moderation, policy, or sector-standard semantics.
- `Namespace + Key` is the machine identity; `DisplayName` is mutable UI text.
- `Event` and `EventSession` remain separate canonical resources even when read models merge them for UX.

### 4) Tenant and soft-delete interfaces

Key interfaces:

- `ITenantEntity` -> `TenantId`
- `IAuditableEntity` -> `CreatedAt/By`, `UpdatedAt/By`
- `ISoftDeletable` -> `IsDeleted`, `DeletedAt/By`

In `ExploreDbContext.SaveChangesAsync`:

- delete operations on `ISoftDeletable` are converted to soft delete;
- audit fields are auto-populated for added/modified/deleted entities.

## Persistence-Enforced Rules (from EF configuration)

These are enforced at database/model level today.

## Event rules

- `Event.Title` required, max length 200.
- `Event` uses dedicated first-class appearance fields such as `BackgroundColor`, `BackgroundEffect`, and `BackgroundImageId`; current architecture should not reintroduce `MetadataJson` as the event extension model.
- `Event.Price` uses precision `(19,4)` and check constraint `price >= 0` when not null.
- Indexes include:
  - tenant + soft-delete + status (`ix_events_tenant_active_status`)
  - tenant + actor + created-at
  - tenant + date range
  - tenant + event type
  - tenant + slug

## Event session rules

- `EventSession.Price` precision `(19,4)` with non-negative check.
- `EventSession.Location` uses `SetNull` on delete.
- `EventSession -> Event` uses cascade delete.
- `EventSessionIslamicAspect` has constraint requiring prayer fields when `StartTimeType` is relative-to-prayer.
- `EventSession` is the scheduled child aggregate, not a peer `Event` row in canonical persistence.

## Actor ownership rules

`ActorConfiguration` enforces:

- unique nullable indexes for `UserId`, `OrganizationId`, and `GroupId` (one actor per owner),
- check constraint allowing exactly one owner FK or none (bot/service actor case).

## Organization rules

- `Organization.ApprovalStatusId` default is `Pending`.
- `Organization.Pii` is `AutoInclude` and cascades on delete.
- tenant-oriented indexes target active listing queries.

## App settings guardrail

`AppSettingConfiguration` blocks high-value secret keys via DB constraint:

- disallows keys starting with `Database:`, `Security:MasterKey`, `ConnectionStrings:`.

## Query Filter Behavior

`ExploreDbContext` applies named global filters:

- `Tenant` filter on tenant-scoped entities,
- `SoftDelete` filter on soft-deletable entities.

Notable exception:

- `User` is soft-delete filtered but not tenant-scoped.
- `EventType` allows global values (`TenantId = null`) plus tenant-specific values.

## What Is Not Implemented as a Domain Primitive

- No dedicated domain-event dispatch model is defined in `Explore.Domain` (no `IDomainEvent` pattern in current code).
- Most business invariants are currently enforced in handlers/services and EF configuration, not rich domain methods.

## Related

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [CUSTOM_PROPERTIES.md](CUSTOM_PROPERTIES.md)
- [CODEBASE_INSIGHTS.md](CODEBASE_INSIGHTS.md)
- [MULTI_TENANCY.md](MULTI_TENANCY.md)
