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

API DTOs expose lookup primitives (`*Id`, `*Code`, `*Name`) rather than domain enum values. Repositories query on the normalized FK IDs. Handlers may convert IDs to internal enums only for business-rule switches while keeping persistence and public contracts normalized.

## Non-Inferable Modeling Patterns

### 1) PII Split (1:1 extension tables)

Some entities keep sensitive fields in dedicated PII tables and expose convenience properties via `NotMapped` wrappers. This allows hard-deletion of PII while preserving the main entity for auditing/history.

- `User` -> `UserPii` (`Email`, `FirstName`, `LastName`)
- `Organization` -> `OrganizationPii` (`FullName`, `Email`, address fields)
- `Actor` -> `ActorPii` (`DisplayName`, `Did`, `Handle`, `ProfilePictureUri`)
- `Location` -> `LocationPii`

`EnsurePii()` helper methods create PII objects lazily when mapped properties are set.

### 2) Optional Event Aspects (Layer 2 typed schema)

Base event data stays in `Event`. Optional modules add 1:1 aspect records sharing the same primary key:

- `EventIslamicAspect` (Id = Event.Id)
- `EventTechAspect` (Id = Event.Id)
- `EventSessionIslamicAspect` (session-level extension)

Aspects are optional; an event/session can exist without aspect rows. Sector-standard semantics belong here, not only in Layer 3 custom properties.

### 3) Layer 3 Governed Custom-Property Extension Model

The platform provides a flexible EAV-based extension system across multiple scopes:

- **Shared Definitions**: `CustomPropertyDefinition` for Organization and Group extensions, plus "Shared Event Definitions".
- **Event Templates**: `EventTemplate` blueprints with `EventTemplateCustomPropertyDefinition`.
- **Event Runtime**: `EventCustomPropertyDefinition` tied to specific events, materialized from templates or created directly.
- **Event Values**: `EventCustomPropertyValue` stores typed runtime data with multi-value ordinal support.
- **Projections**: `EventCustomPropertyProjection` provides a denormalized read-model for discovery/filtering.

**Key Rule**: Layer 3 exists for long-tail extensions. Standard sector fields must use Layer 2 typed schema.

### 4) Tenant and Soft-Delete Interfaces

Isolation and lifecycle are enforced via marker interfaces:

- `ITenantEntity` -> `TenantId` (Global filter in DbContext)
- `IAuditableEntity` -> `CreatedAt/By`, `UpdatedAt/By` (Auto-populated in SaveChanges)
- `ISoftDeletable` -> `IsDeleted`, `DeletedAt/By` (Converted from Delete state in SaveChanges)

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

Specialized variants: `PdsSyncOutbox` (federation), `PolicyChangeOutbox` (governance).

## Persistence-Enforced Rules (from EF configuration)

- `Event.Title`: Required, max 200.
- `Event.Price`: Precision (19,4), non-negative constraint.
- `AppSetting`: Blocks high-value secret keys (e.g., `Database:`, `ConnectionStrings:`) via DB constraint.
- `Actor`: Unique nullable owner FKs (exactly one of UserId, OrganizationId, or GroupId).

## Related
- [ARCHITECTURE.md](ARCHITECTURE.md)
- [CUSTOM_PROPERTIES.md](CUSTOM_PROPERTIES.md)
- [MULTI_TENANCY.md](MULTI_TENANCY.md)
- [OUTBOX_PATTERN.md](OUTBOX_PATTERN.md)
