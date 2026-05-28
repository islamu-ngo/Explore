<!-- ABOUTME: Tenant-scoped entity and FK inventory for enterprise data-model hardening. -->
<!-- ABOUTME: Classifies Explore.Domain tenant boundaries and selects the first composite-FK migration scope. -->

# Tenant-Scoped Entity And FK Inventory

Last Updated: 2026-05-28 Europe/Brussels

> **2026-05-28 re-baseline:** This inventory still records the event graph as the first completed schema-hardening scope, but generated migration filenames are historical only in the current development branch. The user intentionally deletes/regenerates migrations; future work should treat EF configuration/domain code, tests, and `schemas/islamu-event.md` as the source of truth unless migration regeneration is explicitly requested.

## Scope

This inventory implements Phase 1.1 of the enterprise data-model hardening workstream. The source of truth is `Explore.Domain`; EF configuration and the model snapshot are used only to classify how each domain relationship is currently enforced.

Commands used for this slice:

```bash
rg -n "public (sealed )?class .*ITenantEntity" Explore.Domain --glob '*.cs'
rg -n "ITenantEntity|TenantId" Explore.Domain --glob '*.cs'
rg -n "HasOne|WithMany|WithOne|HasForeignKey|HasPrincipalKey|HasAlternateKey|HasIndex|ToTable" Explore.Persistence/Configurations/Entities --glob '*.cs'
rg -n "HasPrincipalKey|HasForeignKey\(e => new" Explore.Persistence/Configurations/Entities --glob '*.cs'
```

Result: 69 strict `ITenantEntity` domain classes were found. After Phase 1.1, all 69 were registered in `ExploreDbContext.QueryFilters.cs` with named tenant filters. Phase 1.4 then replaced the former null-tenant broad-read behavior with fail-closed runtime filters and explicit bypass reasons for approved system paths.

## Classification Legend

| Class | Meaning | Enterprise target |
|---|---|---|
| Strict tenant entity | Implements `ITenantEntity` and has non-null `TenantId`. | Must have named tenant filter, tenant FK where persisted, and tenant-safe parent FKs unless explicitly global/lookup. |
| Tenant-keyed non-marker row | Has `TenantId`, `ScopeTenantId`, or nullable tenant scoping without `ITenantEntity`. | Must be documented as platform/global/tenant hybrid, or promoted to strict tenant entity. |
| Global lookup/reference | No tenant boundary; stable integer IDs or platform-wide refs. | No tenant FK; referenced by strict entities through normal single-column lookup FK. |
| Parent-derived tenant row | No own tenant key; tenant boundary is inherited through a required parent. | Either keep parent-derived with tested include/query behavior, or add strict `TenantId` if queried independently. |

## Strict Tenant Entity Inventory

| Family | Entities | Current state | Hardening classification |
|---|---|---|---|
| Tenant identity, settings, and user-local auth | `TenantUser`, `TenantUserProfile`, `TenantUserRoleGrant`, `TenantInvitation`, `TenantOnboardingState`, `TenantSetting`, `TenantSettingsDocument`, `TenantNavigationLink`, `TenantCapability`, `OrganizationSetting`, `GroupSetting`, `UserPreference`, `UserNotificationPreference`, `UserAuthenticationToken`, `UserExternalLogin` | All are now tenant-filtered. `TenantUser` owns lifecycle state; `TenantUserRoleGrant` owns tenant role authority as an auditable child of `TenantUser`. | Keep filters. Phase 2.2 added composite tenant/user and tenant-role-scope guardrails for grants. Add composite FKs from settings/preferences to their parent tenant-local owner where parent is tenant-scoped in a later bounded slice. |
| Actors, organizations, groups, locations, and storage | `Actor`, `ActorKeyStore`, `Organization`, `OrganizationMember`, `OrganizationReview`, `Group`, `GroupMember`, `Location`, `LocationRoom`, `StorageObject` | Tenant filters exist. `Group` already uses composite tenant-safe FKs for `ParentOrganization` and `ParentGroup`; EF snapshot has `ak_groups_tenant_id_id` and `ak_organizations_tenant_id_id`. Most other parent FKs are still single-column. | Use the `Group` pattern as the local precedent: parent aggregate gets `(TenantId, Id)` principal key; child FK uses `{ TenantId, ParentId }`. |
| Event graph, registration, roles, and contact sharing | `Event`, `EventSeries`, `EventSession`, `EventDay`, `EventAgendaItem`, `EventSessionGroup`, `EventSessionGroupSession`, `EventRegistrationIntent`, `EventRegistration`, `EventRoleAssignment`, `EventContactShareConsent`, `EventContactShareExport`, `EventCategories`, `EventTags`, `EventSessionCategory`, `EventSessionTag`, `EventSessionLanguage`, `EventSessionSpeaker`, `EventSessionAgendaItem` | Tenant filters cover all strict entities. Phase 1.2 made the high-risk parent relationships explicit and tenant/event-safe with composite FKs. `EventRegistration` now carries `EventId` so the database can constrain registration, intent, and session to the same tenant/event boundary. | Keep the implemented guardrails. Next integrity work should target RLS/runtime bypasses and then remaining non-event composite-FK families. |
| Layer 3 custom properties and templates | `CustomPropertyDefinition`, `CustomPropertyValue`, `CustomPropertyProjectionStatus`, `CustomPropertyProjectionDirtyScope`, `EventTemplate`, `EventTemplateCustomPropertyDefinition`, `EventCustomPropertyDefinition`, `EventCustomPropertyValue`, `EventCustomPropertyProjection`, `EventSessionTemplate`, `EventSessionTemplateCustomPropertyDefinition`, `EventSessionCustomPropertyDefinition`, `EventSessionCustomPropertyValue`, `EventSessionCustomPropertyProjection` | Tenant filters exist. Runtime definitions/values/projections reference event/session/template parents mostly through single-column FKs. Options inherit tenant through definitions and are soft-delete filtered but not strict tenant entities. | Second composite-FK scope after event graph. Preserve EAV boundary: definitions and values stay tenant/event/session-local, projections remain rebuildable read models. |
| Tenant-local taxonomy | `Category`, `Tag`, `CategoryTypeCategories`, `TagTypeTags` | Tenant filters exist. Event/session category and tag junctions now use tenant-safe FKs to taxonomy rows. Category/tag type junctions still need separate review if they are tenant-owned rather than lookup-owned. | Keep event/session taxonomy guardrails; classify category/tag type junction ownership before adding more constraints. |
| Operational data | `AuditLog`, `Notification`, `EmailDispatchOutbox`, `EmailDispatchAttempt`, `EmailDispatchReceipt`, `EmailDispatchTenantControl` | Tenant filters exist. Email dispatch outbox/attempt/receipt rows reference tenant and dispatch parents; retention/partitioning is not implemented. | Keep tenant filters; add composite dispatch parent FKs after event graph; classify retention and partition thresholds in Phase 6. |
| Derived read model | `EventWithSessionsView` | Tenant-filtered view model. | Read-only projection. Keep tenant filter; do not make it a principal for writes. |

## Tenant-Keyed Non-Marker Rows

| Entity | Tenant shape | Current query-filter state | Decision needed |
|---|---|---|---|
| `EventType` | `TenantId?` for global event types plus tenant-specific custom event types. | Named tenant filter exists and allows global rows. | Keep hybrid lookup semantics. |
| `ExternalApiKey` | `TenantId?` because instance-admin keys can be platform-scoped. | Named tenant filter exists. | Keep hybrid semantics; review platform-scoped key handling in security work. |
| `TenantFooterLinkGroup` | `TenantId?` because instance defaults can be visible to tenants. | Named tenant filter exists and allows defaults. | Keep hybrid semantics. |
| `ExternalBinding` | `ScopeTenantId?` rather than `TenantId`. | No named tenant filter. | Phase 5 registry must define which bindings are global, tenant scoped, and delete-sensitive before adding a filter. |
| `IdempotencyRecord` | Non-null `TenantId`, but does not implement `ITenantEntity`. | No named tenant filter. | Candidate for strict tenant entity or explicit repository-only isolation test. |
| `TenantLifecycleLog` | Non-null `TenantId`, but does not implement `ITenantEntity`. | No named tenant filter. | Candidate for strict tenant entity unless it is only operator/system read. |
| `OrganizationPolicySet`, `TenantPolicySet` | Non-null `TenantId`, but no `ITenantEntity`. | No named tenant filter. | Coordinate with Cerbos/policy package work before changing. |
| `ConfigurationChangeLog` | Scope id can represent tenant/org/instance. | No tenant filter. | Keep as polymorphic governance log until scope registry is formalized. |
| `UiTheme`, `UiThemePreset`, `UserAppearanceProfile`, `UserAppearancePreference` | Nullable tenant scope. | No tenant filter. | Needs appearance-specific global/default/tenant policy decision. |

## FK Enforcement Current State

| Classification | Existing examples | Risk | Target |
|---|---|---|---|
| Already tenant-safe composite FK | `Group.ParentOrganization` uses `{ TenantId, ParentOrganizationId } -> Organization { TenantId, Id }`; `Group.ParentGroup` uses `{ TenantId, ParentGroupId } -> Group { TenantId, Id }`. | Low. This proves the repo already accepts composite tenant FK patterns. | Reuse this pattern for event graph and EAV. |
| Tenant FK plus single-column parent FK | Remaining non-event families, especially custom-property/template/storage/operational rows that reference tenant-scoped parents. Phase 1.2 removed the high-risk event graph examples from this class. | Medium/high. A row can carry tenant A and point to a parent in tenant B unless application code prevents it. | Add parent alternate keys and composite child FKs in bounded follow-up migrations. |
| Tenant index but not tenant FK guardrail | Many event tables index `{ TenantId, ParentId, ... }`, but still use single-column parent FK. | Medium. Index shape helps query plans but not integrity. | Keep indexes for performance; add composite FK for integrity. |
| Convention-mapped FK | `EventRegistration` no longer relies on convention-only mapping for the critical event/session/intent relationships. Remaining convention relationships should be reviewed as each future family is hardened. | Medium. Convention relationships are harder to review before tenant-safe conversion. | Make relationships explicit before composite conversion. |
| Parent-derived tenant row | `EventIslamicAspect`, `EventTechAspect`, option rows, PII rows, and export items inherit tenant boundary through parent. | Medium if queried independently; low if always reached through parent with filters. | Keep only when parent-derived access is tested; otherwise promote to strict tenant entity. |
| Polymorphic/string reference | `ExternalBinding`, `Notification` entity type/id, configuration logs, custom-property target kinds. | Medium/high. Delete behavior and tenant scope are not encoded by FK. | Define registry before adding constraints. |

## Implemented Migration Scope: Event Graph Foundation

The first schema migration targeted the event graph, not tenant membership or RLS. It had the highest business impact and the clearest parent-child aggregate shape. The implemented migration is `20260527092407_AddEventGraphTenantForeignKeys`.

### Principal keys to add

- `Event`: `{ TenantId, Id }`
- `EventSession`: `{ TenantId, Id }` and `{ TenantId, EventId, Id }`
- `EventDay`: `{ TenantId, Id }` and `{ TenantId, EventId, Id }`
- `EventSessionGroup`: `{ TenantId, Id }` and `{ TenantId, EventId, Id }`
- `Actor`: `{ TenantId, Id }`
- `Category`: `{ TenantId, Id }`
- `Tag`: `{ TenantId, Id }`
- `Location`: `{ TenantId, Id }`
- `LocationRoom`: `{ TenantId, Id }` and ideally `{ TenantId, LocationId, Id }`
- `EventRegistrationIntent`: `{ TenantId, Id }` and `{ TenantId, EventId, Id }`

### Composite FK conversions

- `EventSession`: `{ TenantId, EventId } -> Event`; `{ TenantId, EventId, EventDayId } -> EventDay`; `{ TenantId, LocationId } -> Location`; `{ TenantId, LocationId, RoomId } -> LocationRoom`.
- `EventDay`: `{ TenantId, EventId } -> Event`; consider `{ TenantId, BannerImageId } -> StorageObject`.
- `EventAgendaItem`: `{ TenantId, EventId } -> Event`; `{ TenantId, EventId, EventDayId } -> EventDay`; `{ TenantId, LocationId } -> Location`; `{ TenantId, LocationId, RoomId } -> LocationRoom`.
- `EventSessionGroup`: `{ TenantId, EventId } -> Event`; `{ TenantId, LocationId } -> Location`; `{ TenantId, LocationId, RoomId } -> LocationRoom`.
- `EventSessionGroupSession`: `{ TenantId, EventId } -> Event`; `{ TenantId, EventId, EventSessionGroupId } -> EventSessionGroup`; `{ TenantId, EventId, EventSessionId } -> EventSession`.
- `EventCategories` / `EventTags`: `{ TenantId, EventId } -> Event`; `{ TenantId, CategoryId } -> Category`; `{ TenantId, TagId } -> Tag`.
- `EventSessionCategory` / `EventSessionTag`: `{ TenantId, EventSessionId } -> EventSession`; `{ TenantId, CategoryId } -> Category`; `{ TenantId, TagId } -> Tag`.
- `EventSessionSpeaker`: `{ TenantId, EventSessionId } -> EventSession`; `{ TenantId, ActorId } -> Actor`.
- `EventSessionLanguage`: `{ TenantId, EventSessionId } -> EventSession`; `LanguageId` remains global lookup.
- `EventSessionAgendaItem`: `{ TenantId, EventSessionId } -> EventSession`; `{ TenantId, LocationId } -> Location`.
- `EventRegistrationIntent`: `{ TenantId, EventId } -> Event`; `{ TenantId, EventId, SelectedEventDayId } -> EventDay`; `RegistrationScopeId`, `RegistrationPolicySnapshotId`, and `ApprovalStatusId` remain global lookups.
- `EventRegistration`: snapshot-discovered FKs are now explicit; `{ TenantId, EventId } -> Event`; `{ TenantId, EventId, EventSessionId } -> EventSession`; `{ TenantId, EventId, EventRegistrationIntentId } -> EventRegistrationIntent`; `UserId`, `ApprovalStatusId`, and `AtprotoRecordId` remain global/non-tenant references.
- `EventRoleAssignment`: `{ TenantId, EventId } -> Event`; `UserId` remains global user; `RoleId` remains global role. The tenant-local active-user invariant belongs to Phase 2 role-grant consolidation.
- `EventContactShareConsent`: `{ TenantId, SourceEventId } -> Event`; `{ TenantId, RecipientActorId } -> Actor`; `{ TenantId, SourceEventRegistrationIntentId } -> EventRegistrationIntent`.
- `EventContactShareExport`: `{ TenantId, EventId } -> Event`; `{ TenantId, RecipientActorId } -> Actor`; `ExportedByUserId` remains global user.

Optional composite relationships use `Restrict` where `SetNull` would have required PostgreSQL to null the non-null `tenant_id` component of the composite FK. Room-aware rows also enforce `room_id IS NULL OR location_id IS NOT NULL`, then bind room references through `{ TenantId, LocationId, RoomId }` so a room cannot come from another location in the same tenant.

## Implementation Completed

### Phase 1.1 Inventory And Query Filters

- Added missing tenant query filters for strict tenant entities:
  - `EventSeries`
  - `EventContactShareConsent`
  - `EventContactShareExport`
  - `OrganizationSetting`
  - `GroupSetting`
  - `UserPreference`
  - `UserNotificationPreference`
- Re-ran the strict-tenant filter inventory command; it now reports no `ITenantEntity` class missing from `ExploreDbContext.QueryFilters.cs`.
- Selected the first schema-hardening migration scope: event graph composite tenant FKs.

### Phase 1.2 Event Graph Composite FKs

- Added tenant-scoped alternate keys for event graph principals: `Event`, `Actor`, `Category`, `Tag`, `Location`, `LocationRoom`, `EventDay`, `EventSession`, `EventSessionGroup`, and `EventRegistrationIntent`.
- Converted event graph child relationships to composite tenant-safe FKs, including event sessions, days, agenda items, session groups, room/location pairs, group-session joins, event/session taxonomy joins, speakers, languages, role assignments, registrations, and contact-share records.
- Added `EventRegistration.EventId` and migration backfill/guard SQL so registration rows can be constrained to both their parent intent and selected session under the same event.
- Added PostgreSQL integration tests that insert representative invalid rows and assert the database rejects cross-tenant, cross-event, and cross-location room mismatches.
- Updated `schemas/islamu-event.md` to document the new event graph keys, indexes, refs, and `event_registrations.event_id`.

## Verification Notes

- Baseline `dotnet build --configuration Release --verbosity quiet` passed before edits with existing warnings.
- Post-edit `dotnet build --configuration Release --verbosity quiet` passed with existing warnings.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 176 succeeded, 1 skipped.
- `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passed: 116 succeeded.
- `git diff --check` passed.
- Context7 confirmed current EF Core composite relationship/alternate key modeling and PostgreSQL composite FK/RLS primitives.
- Tavily MCP remains unavailable from tool discovery; this is still recorded as a research-tooling gap, not a code blocker.
