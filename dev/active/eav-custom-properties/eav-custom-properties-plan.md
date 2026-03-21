ABOUTME: Strategic plan for a 3-layer event and event-session architecture: universal core, typed sector profiles, and custom extensions.
ABOUTME: Treats EAV as the Layer 3 extension system only, with governed semantics, template instantiation, projection support, and aggregate read views.

# EAV Custom Properties - Implementation Plan

**Last Updated: 2026-03-19**

---

## Executive Summary

Replace the remaining ad hoc metadata coupling and stale `MetadataJson` assumptions across docs/contracts with an enterprise-grade 3-layer event and event-session architecture where the custom-properties system serves only the extension layer.

- normalized and typed
- deterministic and debuggable for self-hosted deployments
- explicit about machine identity, validation, exposure, and provenance
- safe for multi-tenant extensibility without becoming the canonical home of core product semantics

The prior revision correctly moved events away from live shared-definition inheritance and toward template-based instantiation. This revision hardens the plan further by making the missing middle layer explicit:

- **Layer 1 is the universal event and event-session core**
- **Layer 2 is first-class typed sector schema for both event and session scopes**
- **Layer 3 is the EAV/custom-properties extension layer for both event and session scopes**
- **EAV is the extension/configuration layer, not the product's universal or sector-standard semantic contract**
- **event custom properties use blueprint/template instantiation plus explicit sync workflows**
- **definitions and options have stable namespaced machine keys distinct from display labels**
- **multi-value storage semantics are explicit and testable**
- **validation is typed and governed, not an opaque rule string**
- **custom properties carry exposure/publication/search flags**
- **filterable/searchable properties are projected into dedicated read models**
- **template provenance is versioned and reproducible**

This aligns the custom-properties architecture with the platform's long-term need to separate:

- interoperability contracts
- curation / policy logic
- internal persistence / configuration

---

## Core Architecture Decision

### Product-Level Positioning

The event platform model is intentionally split into three layers across both `Event` and `EventSession`:

1. **Layer 1 - Universal event and session core**
   - fields shared by all events or all sessions regardless of sector/domain
   - modeled directly on `Event`, `EventSession`, and related first-class relational entities

2. **Layer 2 - Sector-standard typed schema**
   - structured fields shared across all events or all sessions in a domain/vertical
   - modeled as first-class typed relational schema, usually 1:1 aspect/profile tables plus lookups and indexes

3. **Layer 3 - Local custom extensions**
   - tenant/organizer-specific long-tail extension fields at event scope or session scope
   - modeled by the EAV/custom-properties system in this plan

Custom properties exist to support Layer 3 only:

- tenant customization
- long-tail optional fields
- UI-driven flexibility
- local extension packs

Custom properties do **not** become the sole persistence model for fields that are central to:

- discovery
- ranking
- moderation
- trust / policy
- export
- cross-instance semantic consistency
- analytics

Those fields must be either:

- promoted to first-class domain fields, or
- implemented as sector-standard typed schema, or
- projected into dedicated read / publication models with governed semantics

### Layer Rule

If a field is required across all events or all sessions in a sector, or is used in filtering, moderation, ranking, policy, export, federation, trust workflows, or stable sector semantics, it must not live only in EAV.

### Final Direction

Use three complementary layers:

1. **Layer 1 - Universal event/session core** on `Event`, `EventSession`, and core relational entities
2. **Layer 2 - Typed sector profiles/aspects** for domain-standard structured semantics at event scope and session scope
3. **Layer 3 - Shared, event-local, and session-local custom-property catalogs** for local extensions

### Aggregate Boundary Rule

`Event` remains the parent program/container aggregate.

`EventSession` remains the scheduled child aggregate.

This plan does **not** collapse sessions into standalone peer `Event` rows. Sessions may be rendered like first-class cards in UI and discovery, but canonical write modeling remains parent/child.

### Non-Negotiable Lifecycle Rules

1. Universal event and session semantics stay on Layer 1.
2. Sector-standard event and session semantics stay on Layer 2 typed schema.
3. Event runtime reads use event-local Layer 3 definitions and values directly.
4. Session runtime reads use session-local Layer 3 definitions and values directly.
5. Template changes never silently change existing events or sessions.
6. Template-to-event and template-to-session sync exists, but it is explicit, version-aware, auditable, and operator-driven.
7. Event-level Layer 3 properties do not automatically become session-level Layer 3 properties.
8. Historical values survive retirement of definitions/options.
9. Search/filter/publication-critical custom properties are projected into dedicated read models.
10. EAV remains an internal extension layer, not the canonical interoperability contract.
11. Sessions may appear as first-class discovery/view items, but aggregate truth remains split between parent event and child sessions.

---

## Current State Analysis

### Current Runtime Reality

- `Explore.Domain/Event.cs` already has dedicated appearance fields (`BackgroundColor`, `BackgroundEffect`, `BackgroundImageId`).
- `Explore.Persistence/Configurations/Entities/EventConfiguration.cs` already maps those event appearance fields.
- `Explore.Domain/Organization.cs` and `Explore.Domain/Group.cs` no longer expose `MetadataJson` in the current codebase.
- `MetadataJson` now survives mainly as stale documentation/planning language and as a conceptual placeholder for "flexible metadata," not as an active domain design worth preserving.

### Current Technical Problems

1. The historical `MetadataJson` pattern was untyped and weakly governed, and stale references still distort current planning.
2. Existing plan revisions fixed event inheritance, but still left the Layer 2 sector-standard schema under-modeled for both events and sessions.
3. Current plan still relies on mutable `Name` / `DisplayName` rather than stable namespaced machine identity.
4. `ValidationRules string?` invites an opaque mini-rules engine.
5. `IsMulti` exists without fully specified storage semantics.
6. Exposure and discovery semantics are not modeled strongly enough.
7. Raw EAV storage is still too close to becoming the hot query model for search/discovery.

### Existing Repo Signals That Support The Harder Model

- `SettingRegistry` and governance keys already favor stable machine keys and explicit config shape.
- module enablement already uses explicit visibility/availability semantics.
- the architecture favors deterministic reads and supportable persistence over magical runtime resolution.
- ATProto-related architecture in this repo already separates interoperable resources from internal application concerns.

---

## Design Principles

1. **The event platform architecture is 3-layer for both `Event` and `EventSession`: universal core, typed sector profile, and local extensions.**
2. **EAV is an extension model, not the primary home of core or sector-standard discovery/policy semantics.**
3. **Every custom property has a stable machine key distinct from display text.**
4. **Custom-property identity is namespaced so platform-owned and tenant-owned semantics can coexist.**
5. **Multi-value storage semantics are explicit, ordered, and testable.**
6. **Validation is governed and typed, not hidden in opaque strings.**
7. **Every custom property has explicit exposure/publication semantics.**
8. **Search/filter-critical Layer 3 properties are projected into dedicated read models rather than queried directly from raw EAV tables.**
9. **Historical values and provenance remain explainable after retirement, rename, or template evolution.**
10. **Template instantiation and template sync are both explicit workflows, never implicit runtime inheritance.**
11. **Layer 2 sector-standard fields are first-class typed relational schema with direct query and policy support.**
12. **Aggregate read views may merge event and session data for UX, but canonical persistence remains separate.**

---

## AT Protocol Alignment

The target platform should mirror the same separation of concerns that makes AT Protocol scalable:

- **interoperable contracts** are not the same thing as local persistence tables
- **app-specific aggregation and curation** are not the same thing as raw schema definitions
- **labels / metadata / moderation state** are separate from base content contracts
- **hosting / redistribution decisions** are controlled by downstream services, not by stuffing everything into one generic data bucket

Applied here:

- custom-property definitions are **internal extension/configuration structures**
- publication/discovery views are **projections / app views**, not raw EAV scans
- trust/moderation/search behavior must not depend solely on arbitrary custom-property rows

---

## Proposed Future State

## Model Layers

### Layer 1 - Universal Event Core

Layer 1 remains the universally shared event model.

Examples already modeled in the repo include:

- title / description / slug
- start and end time
- organizer / actor relationships
- visibility and publishing state
- event format and registration basics
- core pricing and capacity basics

These remain first-class fields on `Event` and related normal entities.

### Layer 1B - Universal EventSession Core

Layer 1 also applies to `EventSession` as its own child aggregate.

Examples already modeled in the repo include:

- title / summary
- start and end time
- room/location
- speaker and language links
- pricing basics and session attendance context

These remain first-class fields on `EventSession` and related normal entities.

### Layer 2 - Typed Sector Profiles / Aspects

Layer 2 is the currently missing explicit piece in the planning docs.

Sector-standard structured semantics must be modeled as typed relational schema, not EAV. In this repo, the strongest current precedent is the existing aspect pattern:

- `EventIslamicAspect`
- `EventTechAspect`
- `EventSessionIslamicAspect`

These already demonstrate the preferred Layer 2 approach:

- 1:1 profile/aspect tables
- shared key relationship to the base aggregate
- typed columns
- foreign keys to lookup tables where needed
- indexes and direct SQL filtering support
- direct moderation/policy/query support

#### Layer 2 Rule

If a field is standard across all events in a sector, or across all sessions in a sector, it belongs in a typed profile/aspect family, not in Layer 3 custom properties.

#### Layer 2 Direction For This Plan

This plan must explicitly preserve and extend the typed aspect/profile approach for sector-standard event semantics.

- current Islamic and Tech event aspect families remain Layer 2
- current `EventSessionIslamicAspect` remains Layer 2 for session scope
- future session-standard families should follow the same typed 1:1 pattern instead of becoming generic session metadata
- future sector-standard families should follow the same typed 1:1 pattern
- Layer 3 must not be used as the first destination for domain-standard sector fields

### Layer 3A - Shared Definition Catalog For Organization And Group

These remain tenant-scoped shared catalogs, but with stronger machine identity, exposure control, and governed validation.

```
PropertyType enum: Text, Number, Option, Boolean, DateTime, Url

ExposureLevel enum: Internal, OrganizerOnly, TenantAdminOnly, Public

CustomPropertyDefinition (Guid PK) -- ITenantEntity, IAuditableEntity, ISoftDeletable
|- EntityTypeName              enum      -- Organization or Group
|- TenantId                    Guid
|- Namespace                   string    -- e.g. "tenant", "platform", "pack.tech"
|- Key                         string    -- stable machine key
|- DisplayName                 string    -- mutable UI label
|- Description                 string?
|- PropertyType                enum
|- IsRequired                  bool
|- IsMulti                     bool
|- IsActive                    bool
|- SortOrder                   int
|- ExposureLevel               enum
|- IsSearchable                bool
|- IsFilterable                bool
|- IsExportable                bool
|- IsModerationRelevant        bool
|- IsAnalyticsRelevant         bool
|- DefaultTextValue            string?
|- DefaultNumberValue          decimal?
|- DefaultBooleanValue         bool?
|- DefaultDateTimeValue        DateTimeOffset?
|- DefaultOptionId             Guid?
|- MinLength                   int?
|- MaxLength                   int?
|- RegexPattern                string?
|- MinNumber                   decimal?
|- MaxNumber                   decimal?
|- MinDateTime                 DateTimeOffset?
|- MaxDateTime                 DateTimeOffset?
|- AllowedUrlSchemes           string?   -- serialized constrained list or child rows
|- IsSystemOwned               bool
|- Audit + SoftDelete fields
`- Options: IReadOnlyCollection<CustomPropertyOption>

CustomPropertyOption (Guid PK) -- IAuditableEntity, ISoftDeletable
|- CustomPropertyDefinitionId  Guid
|- Namespace                   string
|- Key                         string
|- DisplayName                 string
|- Description                 string?
|- Value                       string
|- IsDefault                   bool
|- IsActive                    bool
|- SortOrder                   int
|- ParentOptionId              Guid?
|- Audit + SoftDelete fields

CustomPropertyValue (Guid PK) -- ITenantEntity, IAuditableEntity, ISoftDeletable
|- CustomPropertyDefinitionId  Guid
|- EntityId                    Guid
|- Ordinal                     int       -- explicit order for multi values
|- TextValue                   string?
|- NumberValue                 decimal?
|- BooleanValue                bool?
|- DateTimeValue               DateTimeOffset?
|- OptionId                    Guid?
|- Audit + SoftDelete fields
```

### Layer 3B - Event Blueprint / Template Layer

Templates are reusable event blueprints and carry versioned, namespaced custom-property definitions.

```
EventTemplate (Guid PK) -- ITenantEntity, IAuditableEntity, ISoftDeletable
|- TenantId                    Guid
|- TemplateKey                 string
|- DisplayName                 string
|- Description                 string?
|- EventTypeId                 int?
|- Version                     int
|- IsPublished                 bool
|- IsActive                    bool
|- SortOrder                   int
|- Audit + SoftDelete fields
`- Definitions: IReadOnlyCollection<EventTemplateCustomPropertyDefinition>

EventTemplateCustomPropertyDefinition (Guid PK) -- ITenantEntity, IAuditableEntity, ISoftDeletable
|- EventTemplateId             Guid
|- Namespace                   string
|- Key                         string
|- DisplayName                 string
|- Description                 string?
|- PropertyType                enum
|- IsRequired                  bool
|- IsMulti                     bool
|- IsActive                    bool
|- SortOrder                   int
|- ExposureLevel               enum
|- IsSearchable                bool
|- IsFilterable                bool
|- IsExportable                bool
|- IsModerationRelevant        bool
|- IsAnalyticsRelevant         bool
|- DefaultTextValue            string?
|- DefaultNumberValue          decimal?
|- DefaultBooleanValue         bool?
|- DefaultDateTimeValue        DateTimeOffset?
|- DefaultOptionId             Guid?
|- MinLength                   int?
|- MaxLength                   int?
|- RegexPattern                string?
|- MinNumber                   decimal?
|- MaxNumber                   decimal?
|- MinDateTime                 DateTimeOffset?
|- MaxDateTime                 DateTimeOffset?
|- AllowedUrlSchemes           string?
|- IsSystemOwned               bool
`- Options: IReadOnlyCollection<EventTemplateCustomPropertyOption>

EventTemplateCustomPropertyOption (Guid PK) -- IAuditableEntity, ISoftDeletable
|- EventTemplateCustomPropertyDefinitionId Guid
|- Namespace                               string
|- Key                                     string
|- DisplayName                             string
|- Description                             string?
|- Value                                   string
|- IsDefault                               bool
|- IsActive                                bool
|- SortOrder                               int
|- ParentOptionId                          Guid?
|- Audit + SoftDelete fields

EventSessionTemplate (Guid PK) -- ITenantEntity, IAuditableEntity, ISoftDeletable
|- EventTemplateId               Guid      -- parent event template
|- SessionTemplateKey            string
|- DisplayName                   string
|- Description                   string?
|- SortOrder                     int
|- Version                       int
|- IsPublished                   bool
|- IsActive                      bool
|- Audit + SoftDelete fields
`- Definitions: IReadOnlyCollection<EventSessionTemplateCustomPropertyDefinition>

EventSessionTemplateCustomPropertyDefinition (Guid PK) -- ITenantEntity, IAuditableEntity, ISoftDeletable
|- EventSessionTemplateId       Guid
|- Namespace                    string
|- Key                          string
|- DisplayName                  string
|- Description                  string?
|- PropertyType                 enum
|- IsRequired                   bool
|- IsMulti                      bool
|- IsActive                     bool
|- SortOrder                    int
|- ExposureLevel                enum
|- IsSearchable                 bool
|- IsFilterable                 bool
|- IsExportable                 bool
|- IsModerationRelevant         bool
|- IsAnalyticsRelevant          bool
|- DefaultTextValue             string?
|- DefaultNumberValue           decimal?
|- DefaultBooleanValue          bool?
|- DefaultDateTimeValue         DateTimeOffset?
|- DefaultOptionId              Guid?
|- MinLength                    int?
|- MaxLength                    int?
|- RegexPattern                 string?
|- MinNumber                    decimal?
|- MaxNumber                    decimal?
|- MinDateTime                  DateTimeOffset?
|- MaxDateTime                  DateTimeOffset?
|- AllowedUrlSchemes            string?
|- IsSystemOwned                bool
`- Options: IReadOnlyCollection<EventSessionTemplateCustomPropertyOption>

EventSessionTemplateCustomPropertyOption (Guid PK) -- IAuditableEntity, ISoftDeletable
|- EventSessionTemplateCustomPropertyDefinitionId Guid
|- Namespace                                      string
|- Key                                            string
|- DisplayName                                    string
|- Description                                    string?
|- Value                                          string
|- IsDefault                                      bool
|- IsActive                                       bool
|- SortOrder                                      int
|- ParentOptionId                                 Guid?
|- Audit + SoftDelete fields
```

### Layer 3C - Event-Local Runtime Layer

Events own their runtime configuration after instantiation or sync.

```
EventCustomPropertyDefinition (Guid PK) -- ITenantEntity, IAuditableEntity, ISoftDeletable
|- EventId                     Guid
|- Namespace                   string
|- Key                         string
|- DisplayName                 string
|- Description                 string?
|- PropertyType                enum
|- IsRequired                  bool
|- IsMulti                     bool
|- IsActive                    bool
|- SortOrder                   int
|- ExposureLevel               enum
|- IsSearchable                bool
|- IsFilterable                bool
|- IsExportable                bool
|- IsModerationRelevant        bool
|- IsAnalyticsRelevant         bool
|- DefaultTextValue            string?
|- DefaultNumberValue          decimal?
|- DefaultBooleanValue         bool?
|- DefaultDateTimeValue        DateTimeOffset?
|- DefaultOptionId             Guid?
|- MinLength                   int?
|- MaxLength                   int?
|- RegexPattern                string?
|- MinNumber                   decimal?
|- MaxNumber                   decimal?
|- MinDateTime                 DateTimeOffset?
|- MaxDateTime                 DateTimeOffset?
|- AllowedUrlSchemes           string?
|- IsSystemOwned               bool
|- SourceTemplateId            Guid?
|- SourceTemplateKey           string?
|- SourceTemplateVersion       int?
|- SourceTemplateDefinitionId  Guid?
|- InstantiatedAt              DateTimeOffset
|- LastSyncedFromTemplateAt    DateTimeOffset?
|- Audit + SoftDelete fields
`- Options: IReadOnlyCollection<EventCustomPropertyOption>

EventCustomPropertyOption (Guid PK) -- IAuditableEntity, ISoftDeletable
|- EventCustomPropertyDefinitionId Guid
|- Namespace                       string
|- Key                             string
|- DisplayName                     string
|- Description                     string?
|- Value                           string
|- IsDefault                       bool
|- IsActive                        bool
|- SortOrder                       int
|- ParentOptionId                  Guid?
|- SourceTemplateOptionId          Guid?
|- SourceTemplateVersion           int?
|- Audit + SoftDelete fields

EventCustomPropertyValue (Guid PK) -- ITenantEntity, IAuditableEntity, ISoftDeletable
|- EventCustomPropertyDefinitionId Guid
|- EventId                         Guid
|- Ordinal                         int
|- TextValue                       string?
|- NumberValue                     decimal?
|- BooleanValue                    bool?
|- DateTimeValue                   DateTimeOffset?
|- OptionId                        Guid?
|- Audit + SoftDelete fields

EventSessionCustomPropertyDefinition (Guid PK) -- ITenantEntity, IAuditableEntity, ISoftDeletable
|- EventSessionId              Guid
|- Namespace                   string
|- Key                         string
|- DisplayName                 string
|- Description                 string?
|- PropertyType                enum
|- IsRequired                  bool
|- IsMulti                     bool
|- IsActive                    bool
|- SortOrder                   int
|- ExposureLevel               enum
|- IsSearchable                bool
|- IsFilterable                bool
|- IsExportable                bool
|- IsModerationRelevant        bool
|- IsAnalyticsRelevant         bool
|- DefaultTextValue            string?
|- DefaultNumberValue          decimal?
|- DefaultBooleanValue         bool?
|- DefaultDateTimeValue        DateTimeOffset?
|- DefaultOptionId             Guid?
|- MinLength                   int?
|- MaxLength                   int?
|- RegexPattern                string?
|- MinNumber                   decimal?
|- MaxNumber                   decimal?
|- MinDateTime                 DateTimeOffset?
|- MaxDateTime                 DateTimeOffset?
|- AllowedUrlSchemes           string?
|- IsSystemOwned               bool
|- SourceTemplateId            Guid?
|- SourceTemplateKey           string?
|- SourceTemplateVersion       int?
|- SourceTemplateDefinitionId  Guid?
|- InstantiatedAt              DateTimeOffset
|- LastSyncedFromTemplateAt    DateTimeOffset?
|- Audit + SoftDelete fields
`- Options: IReadOnlyCollection<EventSessionCustomPropertyOption>

EventSessionCustomPropertyOption (Guid PK) -- IAuditableEntity, ISoftDeletable
|- EventSessionCustomPropertyDefinitionId Guid
|- Namespace                              string
|- Key                                    string
|- DisplayName                            string
|- Description                            string?
|- Value                                  string
|- IsDefault                              bool
|- IsActive                               bool
|- SortOrder                              int
|- ParentOptionId                         Guid?
|- SourceTemplateOptionId                 Guid?
|- SourceTemplateVersion                  int?
|- Audit + SoftDelete fields

EventSessionCustomPropertyValue (Guid PK) -- ITenantEntity, IAuditableEntity, ISoftDeletable
|- EventSessionCustomPropertyDefinitionId Guid
|- EventSessionId                         Guid
|- Ordinal                                int
|- TextValue                              string?
|- NumberValue                            decimal?
|- BooleanValue                           bool?
|- DateTimeValue                          DateTimeOffset?
|- OptionId                               Guid?
|- Audit + SoftDelete fields
```

### Layer 3D - Projection / Read-Model Layer

Raw EAV tables are not the long-term discovery/read model for hot query paths.

```
EventCustomPropertyProjection
|- EventId
|- Namespace
|- Key
|- PropertyType
|- ExposureLevel
|- SearchToken / SearchValue columns
|- FilterFacetValue columns
|- ExportValue columns
|- ModerationValue columns
|- UpdatedAt

EventSearchDocument / equivalent projection payload
|- canonical event fields
|- promoted custom-property facets
|- public/exportable custom-property payload

EventSessionCustomPropertyProjection
|- EventSessionId
|- Namespace
|- Key
|- PropertyType
|- ExposureLevel
|- SearchToken / SearchValue columns
|- FilterFacetValue columns
|- ExportValue columns
|- ModerationValue columns
|- UpdatedAt

EventWithSessionsView / equivalent aggregate payload
|- canonical event fields
|- event projections
|- session summaries
|- selected session projection facets
```

This projection layer is implemented as part of this plan, not deferred.

### Lexicon Strategy

Lexicons should follow the same separation as the persistence model:

- canonical core lexicons for `Event` and `EventSession`
- typed Layer 2 lexicons for event and session profiles
- Layer 3 extension lexicons for event and session extension payloads
- aggregate read/view lexicons that embed session summaries inside an event-oriented view

Recommended structure:

- `...event.core`
- `...eventsession.core`
- `...event.islamic`, `...event.tech`
- `...eventsession.islamic` and future session typed profile lexicons
- `...event.extension`
- `...eventsession.extension`
- `...event.view` or `...event.withSessions.view`

Canonical lexicons remain separate records. The merged event-with-sessions lexicon is a read/view contract, not the canonical write contract.

### Appearance Columns (Still Not EAV)

**Event**
- `BackgroundColor`
- `BackgroundMediaUrl`
- `BackgroundEffect`

**Organization**
- `ProfileImageUrl`
- `BackgroundColor`
- `BackgroundMediaUrl`
- `BackgroundEffect`

**Group**
- `PictureUrl`
- `BannerColor`
- `BannerMediaUrl`
- `BannerEffect`

---

## Machine Identity, Namespacing, And Uniqueness

### Identity Rules

- `DisplayName` is mutable and localizable.
- `Namespace + Key` is stable and machine-oriented.
- `Name` is removed from the design; use `Key` plus `DisplayName`.

### Namespace Rules

Supported namespace categories:

- `platform` for platform-owned semantic properties
- `sector.{name}` reserved for sector/domain semantic bridges and collision prevention
- `tenant` for tenant-local custom semantics
- `pack.{name}` for curated property packs/templates shipped with the platform
- reserved future namespaces can be introduced without schema redesign

Namespaces and keys are normalized to lowercase machine identifiers before persistence so case-style differences never create distinct semantic identities.

### Uniqueness Rules

- shared definitions: `(TenantId, EntityTypeName, Namespace, Key)`
- shared options: `(CustomPropertyDefinitionId, Namespace, Key)`
- event templates: `(TenantId, TemplateKey, Version)` and `(EventTemplateId, Namespace, Key)`
- session templates: `(EventTemplateId, SessionTemplateKey, Version)` and `(EventSessionTemplateId, Namespace, Key)`
- event-local definitions: `(EventId, Namespace, Key)`
- event-local options: `(EventCustomPropertyDefinitionId, Namespace, Key)`
- session-local definitions: `(EventSessionId, Namespace, Key)`
- session-local options: `(EventSessionCustomPropertyDefinitionId, Namespace, Key)`
- single-value properties: max one value row with `Ordinal = 0`
- multi-value properties: one row per selected/entered value with unique ordinal per definition/entity scope

---

## Layer 2 Sector Profile Rules

Layer 2 is first-class typed relational schema.

### What Belongs In Layer 2

- sector-standard event semantics
- direct filter fields for sector views/list pages
- moderation/policy-critical sector fields
- fields that require foreign keys to lookups or constrained enums
- fields expected across most or all events in a sector

### What Must Not Go Into Layer 3 EAV

- standard Islamic event semantics already modeled by `EventIslamicAspect`
- standard Tech event semantics already modeled by `EventTechAspect`
- future sector-profile fields that need direct filter/query/index support

### Current Repo-Aligned Implementation Choice

For this codebase, the preferred Layer 2 implementation is the existing typed aspect/profile pattern rather than pushing more columns directly onto `Event`:

- universal data stays on `Event`
- sector-standard data lives in 1:1 aspect/profile tables
- local extensions live in Layer 3 custom properties

This keeps `Event` universal while preserving strong relational modeling for sector-standard data.

### Collision Rule

Layer 3 definitions must be rejected when they attempt to reuse a reserved Layer 2 semantic identity.

---

## Multi-Value Semantics

`IsMulti` must be explicit in storage and behavior.

### Baseline Rules

1. One value row per selected or entered value.
2. `Ordinal` defines order whenever `IsMulti = true`.
3. `IsMulti = false` enforces max one value row.
4. For option properties:
   - single-select -> max one row
   - multi-select -> one row per selected option
5. For primitive properties:
   - single -> one row
   - multi -> one row per primitive value
6. Duplicates are disallowed by default for the same `(DefinitionId, normalized value)` within one entity scope unless a future rule explicitly allows them.

### Required Tests

- multi-select option values
- multi-text values
- ordinal ordering behavior
- single-value uniqueness enforcement
- duplicate rejection behavior

---

## Validation Model

`ValidationRules string?` is removed from the design.

### Supported Validation In This Plan

- string: `MinLength`, `MaxLength`, `RegexPattern`
- number: `MinNumber`, `MaxNumber`
- datetime: `MinDateTime`, `MaxDateTime`
- url: `AllowedUrlSchemes`

### Rule

Validation is limited to explicitly modeled, typed constraints in this implementation. No opaque rule DSL and no free-form JSON validation blob.

---

## Exposure, Publication, And Governance Semantics

Every custom property must declare how it can be surfaced and governed.

### Definition-Level Exposure Fields

- `ExposureLevel`
- `IsSearchable`
- `IsFilterable`
- `IsExportable`
- `IsModerationRelevant`
- `IsAnalyticsRelevant`
- `IsSystemOwned`

### Rules

1. Visibility is never inferred.
2. Search/filter/export behavior is never inferred from type alone.
3. Platform-owned / system-owned properties are governed more strictly than tenant-owned local fields.
4. Properties marked searchable/filterable must participate in projection updates.
5. `IsSystemOwned` controls ownership/editability, not visibility.
6. `ExposureLevel` is the maximum audience ceiling; search/filter/export flags only enable pipelines inside that ceiling.

---

## Template Lifecycle And Sync

### Creation Flow

1. Admin creates `EventTemplate` with explicit `TemplateKey` and `Version`.
2. Admin defines versioned template custom properties and options.
3. Organizer creates an event with an optional template selection.
4. In one transaction, the system persists the event and materializes event-local definitions/options/initial values.
5. Event runtime behavior uses only event-local rows.

### Edit Flow

- editing a template creates/updates a specific versioned template state
- editing an event changes only event-local state

### Sync Flow (Implemented In This Plan)

Sync is explicit and version-aware.

1. System compares an event's current provenance version with the selected template version.
2. System builds a diff:
   - added definitions/options
   - changed display labels
   - changed validation/exposure flags
   - retired options/definitions
3. Authorized operator chooses what to apply.
4. Sync updates event-local rows and stamps `LastSyncedFromTemplateAt` plus `SourceTemplateVersion`.
5. No automatic sync on template save.

### Provenance Rules

Support must be able to answer:

- which template created this event?
- which version?
- when was it instantiated?
- was it synced later?

Sync matching must prefer stored source identifiers first and fall back to `Namespace + Key` only for repair or backfill scenarios.

---

## Delete, Retirement, And Historical Behavior

### Rules

1. Definitions/options with historical values are retired/deactivated, not hard-deleted in normal workflows.
2. Historical values remain readable after definition or option retirement.
3. Option retirement does not invalidate existing historical value rows.
4. Template changes do not rewrite historical provenance references.
5. Hard delete is reserved for safe admin cleanup cases with no dependent historical state.
6. Sync operations must preserve auditability of source version lineage.

---

## Query And Projection Strategy

### Runtime Read Rules

- event form reads -> event-local definitions + event-local values
- organization/group runtime reads -> shared definitions + scoped values
- discovery/filtering/search -> read projections, not ad hoc EAV-heavy query composition

### Projection Rules

1. Searchable or filterable custom properties are projected at write/sync time.
2. Public/exportable projections only include properties whose exposure rules allow it.
3. Moderation-relevant properties are projected into moderation-aware read models.
4. Projection rebuild tooling is part of the implementation plan.
5. Projection rows are atomic per projected value row, not one merged row per property.
6. Raw event-local definitions and values remain the source of truth; projections are rebuildable read models only.

---

## Required Phase 0 ADR Before Implementation

Implementation should not begin until these decisions are locked in the plan and supporting docs.

### Phase 0: Architecture And Governance Lock

#### Task 0.1: Lock EAV As Extension Layer, Not Core Semantic Contract
- **Acceptance Criteria:**
  - plan explicitly forbids using custom properties as the sole persistence model for discovery/policy/publication-critical semantics
  - promotion/projection rule is documented

#### Task 0.1A: Lock The 3-Layer Event And EventSession Model
- **Acceptance Criteria:**
  - plan explicitly distinguishes Layer 1 universal core, Layer 2 typed sector profile, and Layer 3 custom extensions for both event and session scopes
  - Layer 2 is described as first-class typed relational schema, not EAV

#### Task 0.1C: Lock Parent/Child Aggregate Boundary
- **Acceptance Criteria:**
  - `Event` remains the parent program/container aggregate
  - `EventSession` remains the scheduled child aggregate
  - merged event-with-sessions views are read models, not canonical write models

#### Task 0.1B: Lock Layer 2 Boundaries
- **Acceptance Criteria:**
  - plan explicitly forbids placing sector-standard semantics into Layer 3 custom properties
  - current aspect/profile families are identified as Layer 2 precedents

#### Task 0.2: Lock Stable Machine-Key Strategy
- **Acceptance Criteria:**
  - `Namespace + Key` replaces mutable `Name` as machine identity
  - uniqueness constraints use namespaced keys

#### Task 0.3: Lock Multi-Value Storage Semantics
- **Acceptance Criteria:**
  - `Ordinal` is required on value rows
  - one-row-per-value semantics are documented for primitive and option types

#### Task 0.4: Lock Validation Model
- **Acceptance Criteria:**
  - `ValidationRules string?` is removed
  - only typed governed validation metadata remains

#### Task 0.5: Lock Exposure / Publication Semantics
- **Acceptance Criteria:**
  - exposure/search/filter/export/moderation fields are required in the model
  - visibility rules are explicit

#### Task 0.6: Lock Projection Strategy
- **Acceptance Criteria:**
  - plan states that raw EAV is not the long-term discovery query model
  - projection entities and rebuild/update flows are in scope now

#### Task 0.7: Lock Template Provenance And Sync Model
- **Acceptance Criteria:**
  - template versioning, provenance stamping, and explicit sync workflow are documented

#### Task 0.8: Lock Delete / Retirement Semantics
- **Acceptance Criteria:**
  - plan documents retirement vs hard delete behavior and historical retention rules

#### Task 0.9: Lock Governance / Authorization Categories
- **Acceptance Criteria:**
  - platform/system namespace editing, property governance, template admin, and event editing are distinguished

---

## Implementation Phases

### Phase 1: Domain Layer
**Effort: XXL** | **Related Skills:** `clean-architecture-rules`

#### Task 1.1: Create Core Enums
- `PropertyType`
- `EntityTypeName`
- `ExposureLevel`

#### Task 1.1A: Audit Existing Layer 2 Sector Aspect Families
- **Acceptance Criteria:**
  - identify which existing event and session aspect/profile entities are already Layer 2
  - document which sector-standard semantics remain outside Layer 3 scope

#### Task 1.1B: Add Missing Typed Sector Profile Fields/Entities Only Where The Current Layer 2 Model Is Incomplete
- **Acceptance Criteria:**
  - missing sector-standard fields are added to typed aspect/profile schema, not EAV
  - no new sector-standard requirement is routed into custom-property definitions by default

#### Task 1.2: Create Shared Definition Entities
- `CustomPropertyDefinition`
- `CustomPropertyOption`
- `CustomPropertyValue`
- **Acceptance Criteria:** namespaced keys, typed validation fields, exposure flags, ordinal support

#### Task 1.3: Create Event Template Entities
- `EventTemplate`
- `EventTemplateCustomPropertyDefinition`
- `EventTemplateCustomPropertyOption`
- **Acceptance Criteria:** versioned template identity and namespaced keys

#### Task 1.4: Create Event-Local Runtime Entities
- `EventCustomPropertyDefinition`
- `EventCustomPropertyOption`
- `EventCustomPropertyValue`
- **Acceptance Criteria:** event-local ownership, provenance/version fields, ordinal support

#### Task 1.4A: Create EventSession Template Entities
- `EventSessionTemplate`
- `EventSessionTemplateCustomPropertyDefinition`
- `EventSessionTemplateCustomPropertyOption`
- **Acceptance Criteria:** child-blueprint identity, parent event template linkage, namespaced keys, versioning

#### Task 1.4B: Create EventSession-Local Runtime Entities
- `EventSessionCustomPropertyDefinition`
- `EventSessionCustomPropertyOption`
- `EventSessionCustomPropertyValue`
- **Acceptance Criteria:** session-local ownership, provenance/version fields, ordinal support

#### Task 1.5: Create Projection Entities / Value Objects
- `EventCustomPropertyProjection` or equivalent domain-side representation
- **Acceptance Criteria:** enough shape to support searchable/filterable/exportable projections

#### Task 1.5A: Create Session Projection Entities / Value Objects
- `EventSessionCustomPropertyProjection` or equivalent domain-side representation
- **Acceptance Criteria:** enough shape to support session search/filter/export projections and event-with-sessions aggregate read views

#### Task 1.6: Audit Existing Appearance / Branding Fields And Align Them To The New Governance Model
- **Acceptance Criteria:** preserve current first-class appearance approach; reconcile existing `StorageObject`-style media references with the custom-properties plan rather than reintroducing URL-in-EAV patterns

#### Task 1.7: Add Missing First-Class Appearance / Branding Fields Only Where The Current Domain Is Still Incomplete

#### Task 1.8: Remove Stale Metadata Assumptions From The Domain Design Baseline

---

### Phase 2: Persistence Layer - EF Configurations
**Effort: XXL** | **Related Skills:** `dotnet-efcore-guidelines`

#### Task 2.0: Reconcile Existing Layer 2 Aspect Configurations With The 3-Layer Plan
- **Acceptance Criteria:**
  - Layer 2 aspect/profile configurations remain first-class and queryable
  - filtering/indexing expectations for sector-standard fields are preserved or improved

#### Task 2.1: Configure Shared Definition Tables
- **Acceptance Criteria:** namespaced uniqueness, indexes for exposure/search flags, typed validation columns, ordinal constraints

#### Task 2.2: Configure Event Template Tables
- **Acceptance Criteria:** `(TenantId, TemplateKey, Version)` uniqueness and definition/option namespaced uniqueness

#### Task 2.2A: Configure EventSession Template Tables
- **Acceptance Criteria:** parent event template linkage, `(EventTemplateId, SessionTemplateKey, Version)` uniqueness, and session definition/option namespaced uniqueness

#### Task 2.3: Configure Event Runtime Tables
- **Acceptance Criteria:** `(EventId, Namespace, Key)` uniqueness and provenance/version columns mapped

#### Task 2.3A: Configure EventSession Runtime Tables
- **Acceptance Criteria:** `(EventSessionId, Namespace, Key)` uniqueness and provenance/version columns mapped

#### Task 2.4: Configure Projection Tables
- **Acceptance Criteria:** indexes optimized for event discovery/search/filter reads

#### Task 2.4B: Configure Session Projection Tables
- **Acceptance Criteria:** indexes optimized for session discovery/search/filter reads and aggregate event-with-sessions views

#### Task 2.5: Reconcile `EventConfiguration.cs` With The Hardened Plan

#### Task 2.6: Add Or Adjust `OrganizationConfiguration.cs` And `GroupConfiguration.cs` Only For Real New Fields Introduced By This Initiative

#### Task 2.7: Keep Existing First-Class Appearance Mappings And Avoid Regressing To Metadata-Blob Storage

#### Task 2.8: Update `ExploreDbContext.cs`
- add DbSets and query filters for all new entities

#### Task 2.9: Create EF Migration

---

### Phase 3: Persistence Layer - Repositories, Sync, And Projection Support
**Effort: XXL** | **Related Skills:** `dotnet-efcore-guidelines`, `clean-architecture-rules`

#### Task 3.1: Create Shared Definition Repositories

#### Task 3.2: Create Event Template Repositories

#### Task 3.2A: Create EventSession Template Repositories

#### Task 3.3: Create Event Runtime Repositories

#### Task 3.3A: Create EventSession Runtime Repositories

#### Task 3.4: Create Template Instantiation Service

#### Task 3.4A: Create Session Template Instantiation Service

#### Task 3.5: Create Template Diff / Sync Service

#### Task 3.5A: Create Session Template Diff / Sync Service

#### Task 3.6: Create Projection Updater / Rebuilder Service

#### Task 3.7: Register Repositories And Services In DI

---

### Phase 4: Application Layer - DTOs And Contracts
**Effort: XXL** | **Related Skills:** `cqrs-mediatr-guidelines`

#### Task 4.1: Create Shared Definition DTOs

#### Task 4.2: Create Event Template DTOs

#### Task 4.2A: Create EventSession Template DTOs

#### Task 4.3: Create Event Runtime Definition / Value DTOs

#### Task 4.3A: Create EventSession Runtime Definition / Value DTOs

#### Task 4.4: Create Template Diff / Sync DTOs

#### Task 4.5: Create Projection/Admin DTOs As Needed

#### Task 4.5A: Create Aggregate Event-With-Sessions View DTOs / Lexicon Contracts

#### Task 4.6: Re-audit Event DTOs / Generated Contracts And Remove Any Stale Metadata-Blob Assumptions

#### Task 4.7: Re-audit Organization DTOs / Generated Contracts And Remove Any Stale Metadata-Blob Assumptions

#### Task 4.8: Re-audit Group DTOs / Generated Contracts And Remove Any Stale Metadata-Blob Assumptions

#### Task 4.9: Update Mapping Profiles

---

### Phase 5: Application Layer - CQRS For Definitions, Templates, Runtime Values, Sync, And Projections
**Effort: XXXL** | **Related Skills:** `cqrs-mediatr-guidelines`

#### Task 5.0: Preserve Layer 2 CQRS Paths For Sector-Standard Schema
- **Acceptance Criteria:**
  - sector-standard typed aspect/profile commands and queries remain distinct from Layer 3 custom-property flows
  - Layer 2 filtering/moderation/policy logic does not depend on EAV handlers

#### Task 5.1: CRUD Commands / Queries For Shared Organization / Group Definitions

#### Task 5.2: CRUD Commands / Queries For Event Templates

#### Task 5.2A: CRUD Commands / Queries For EventSession Templates

#### Task 5.3: CRUD Commands / Queries For Template Options

#### Task 5.4: Queries For Event-Local Definitions And Values

#### Task 5.4A: Queries For EventSession-Local Definitions And Values

#### Task 5.5: Commands For Setting Event-Local Values With Explicit Multi-Value Rules

#### Task 5.5A: Commands For Setting EventSession-Local Values With Explicit Multi-Value Rules

#### Task 5.6: Commands For Editing Event-Local Definitions

#### Task 5.6A: Commands For Editing EventSession-Local Definitions

#### Task 5.7: Commands / Queries For Template Diff And Sync

#### Task 5.7A: Commands / Queries For EventSession Template Diff And Sync

#### Task 5.8: Commands / Jobs For Projection Updates And Rebuilds

#### Task 5.9: Promotion Rules For Discovery-Critical Properties
- **Acceptance Criteria:** plan and handlers support moving properties toward first-class/projection-backed semantics when they become central

#### Task 5.10: Promotion Rules For Sector-Standard Properties
- **Acceptance Criteria:** if a Layer 3 field proves to be sector-standard, the implementation path promotes it into a Layer 2 typed profile/aspect instead of deepening EAV dependence

---

### Phase 6: Event + EventSession Creation, Template Instantiation, And Editing Flow
**Effort: XXL** | **Related Skills:** `cqrs-mediatr-guidelines`

#### Task 6.1: Extend Event Creation Contract With Optional Template Selection

#### Task 6.2: Instantiate Event-Local Definitions/Options/Defaults Transactionally

#### Task 6.2A: Instantiate Session Templates And Session-Local Definitions/Options/Defaults Transactionally

#### Task 6.3: Support Event Creation Without Template

#### Task 6.4: Ensure Event Edit Reads Event-Local Configuration Only

#### Task 6.5: Add Template Sync Decision Flow To Event Administration

#### Task 6.5A: Add Template Sync Decision Flow To EventSession Administration

#### Task 6.6: Keep Layer 2 Editing Separate From Layer 3 Editing
- **Acceptance Criteria:** typed sector-profile editing remains distinct from custom-property editing in contracts and workflows

#### Task 6.7: Add Parent Event Aggregate Read/View Flow
- **Acceptance Criteria:** event page can embed linked session summaries without turning sessions into canonical peer events

---

### Phase 7: Remove Stale Metadata Assumptions And Legacy Planning Drift
**Effort: L**

#### Task 7.1: Re-audit Remaining Source References To `MetadataJson` And Remove Any Actual Runtime Coupling If Found

#### Task 7.2: Clean Up Stale Comments, Docs, And Contracts That Still Assume JSONB Metadata Storage

#### Task 7.3: Align Event / Organization / Group Write Paths With The Current First-Class Appearance/Branding Model

#### Task 7.4: Ensure No New Runtime Query Path Depends On Generic Metadata Blobs

---

### Phase 8: API Layer
**Effort: XXL** | **Related Skills:** `auth-patterns`

#### Task 8.1: Shared Definition Controllers For Organization / Group Custom Properties

#### Task 8.2: Event Template Controllers

#### Task 8.2A: EventSession Template Controllers

#### Task 8.3: Event Runtime Definition / Value Controllers

#### Task 8.3A: EventSession Runtime Definition / Value Controllers

#### Task 8.4: Template Diff / Sync Controllers

#### Task 8.4A: EventSession Template Diff / Sync Controllers

#### Task 8.5: Projection Admin / Rebuild Endpoints If Required

#### Task 8.6: Reconcile `EventController.cs` And Related API Contracts With Template-Aware Event Creation And Remove Any Stale Metadata Query Assumptions

#### Task 8.6A: Reconcile `EventSessionController.cs` And Related API Contracts With Session Template/Layer 3 Workflows

#### Task 8.7: Add Governance-Oriented Authorization Policies
- template admin
- event editor
- property governance admin
- platform/system namespace editor

---

### Phase 9: Blazor Client Updates
**Effort: XXXL** | **Related Skills:** `blazor-ui-conventions`, `blazor-css-isolation`

#### Task 9.1: Reconcile Appearance Helpers And UI Utilities With The Existing First-Class Appearance Model

#### Task 9.2: Add Shared Definition Governance UI For Organization / Group

#### Task 9.3: Add Event Template Management UI

#### Task 9.4: Add Template Selection To Event Creation UI

#### Task 9.4A: Add Session Blueprint Selection / Editing To Event Session UI

#### Task 9.5: Add Event Runtime Custom-Property Editor Against Event-Local Definitions

#### Task 9.5A: Add EventSession Runtime Custom-Property Editor Against Session-Local Definitions

#### Task 9.6: Add Template Diff / Sync UX

#### Task 9.6A: Add EventSession Template Diff / Sync UX

#### Task 9.7: Add Exposure / Searchability / Exportability Governance UX

#### Task 9.8: Update Organization And Group Pages To Remove Any Stale Metadata-Blob Assumptions

#### Task 9.9: Regenerate Generated API Client

---

### Phase 10: Search, Projection, Moderation, Export, And Aggregate View Integration
**Effort: XXL**

#### Task 10.0: Integrate Layer 2 Sector Fields Directly Into Discovery And Governance Paths
- **Acceptance Criteria:** sector-standard typed fields participate in filtering/policy/export without passing through Layer 3 projections first

#### Task 10.1: Populate Event Custom-Property Projections On Writes And Sync

#### Task 10.1A: Populate EventSession Custom-Property Projections On Writes And Sync

#### Task 10.2: Integrate Filterable/Searchable Projections Into Discovery Query Paths

#### Task 10.2A: Integrate Session Filterable/Searchable Projections Into Discovery Query Paths

#### Task 10.3: Integrate Exportable/Public Projections Into Publication / Export Paths

#### Task 10.3A: Integrate Event-With-Sessions Aggregate Read/View Contracts Into Publication And Discovery Surfaces

#### Task 10.4: Integrate Moderation-Relevant Projections Into Governance Workflows

#### Task 10.5: Integrate Analytics-Relevant Projections Into Analytics Payload Composition

---

### Phase 11: Testing And Documentation
**Effort: XXL**

#### Task 11.1: Architecture Tests

#### Task 11.2: Unit Tests For Namespaced Key Uniqueness And DisplayName Renames

#### Task 11.3: Unit Tests For Multi-Value Semantics And Ordering

#### Task 11.4: Unit Tests For Typed Validation Rules

#### Task 11.5: Unit Tests For Exposure / Search / Filter / Export Flags

#### Task 11.6: Unit Tests For Template Instantiation, Versioning, And Sync Provenance

#### Task 11.6A: Unit Tests For EventSession Template Instantiation, Versioning, And Sync Provenance

#### Task 11.7: Unit Tests For Retired Definitions / Options With Historical Values

#### Task 11.8: Integration Tests For Persistence Constraints And Tenant Isolation

#### Task 11.8A: Integration Tests For EventSession Persistence Constraints And Tenant Isolation

#### Task 11.9: Integration Tests For API Roundtrips (template -> event -> sync -> projections)

#### Task 11.9B: Integration Tests For API Roundtrips (event template -> session blueprint -> event session -> sync -> projections)

#### Task 11.10: Update Documentation
- `docs/DOMAIN.md`
- `docs/ARCHITECTURE.md`
- `docs/EXTENSIBILITY.md`
- any relevant publication / search / governance docs
- lexicon planning docs for event/session canonical contracts and aggregate views

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| EAV grows into a semantic dumping ground | Medium | High | explicit extension-layer rule plus promotion/projection rules |
| Machine identity breaks due to mutable labels | High | High | namespaced `Key` replaces mutable `Name` |
| Multi-value behavior becomes inconsistent across API/UI | Medium | High | explicit ordinal semantics and dedicated tests |
| Validation devolves into a hidden DSL | Medium | High | typed governed validation model only |
| Search/discovery queries become EAV-heavy and brittle | High | High | implement projection layer in the same initiative |
| Template provenance becomes insufficient for support | Medium | High | versioned provenance fields and sync audit trail |
| Event/session scope drifts and sessions get collapsed into peer events | Medium | High | preserve parent/child aggregate rule and use aggregate read views instead of canonical collapse |
| Soft deletion causes historical data loss or confusion | Medium | High | explicit retirement rules and historical retention behavior |
| Governance rules are too weak for public/searchable fields | Medium | High | add exposure flags and authorization categories now |

---

## Success Metrics

1. All stale `MetadataJson` assumptions are removed from active runtime, API, UI, and planning surfaces.
2. Event runtime behavior uses only event-local instantiated/synced definitions and values.
3. EventSession runtime behavior uses only session-local instantiated/synced definitions and values.
3. Custom-property identity survives display-name changes and localization changes.
4. Multi-value semantics are consistent across storage, API, and UI.
5. Validation is enforced from typed metadata with no opaque rule blobs.
6. Searchable/filterable/exportable properties flow through projections, not raw EAV-only discovery queries.
7. Template version provenance and sync history are explainable in support scenarios.
8. Historical values remain readable after definition or option retirement.
9. Platform-owned and tenant-owned namespaced properties can coexist without collisions.
10. Sector-standard semantics are modeled through Layer 2 typed schema, not Layer 3 EAV rows.
11. Event and EventSession remain separate canonical resources while aggregate views can merge them for UX and federation-facing reads.

---

## Final Recommendation

Keep the normalized typed custom-property system and keep the event template-instantiation model.

But do **not** let raw EAV become:

- the only semantic home of important product concepts
- a stringly typed rules engine
- the hot query path for discovery
- or the only way support understands event state

The right implementation for this platform is:

- Layer 1 universal core on `Event` and `EventSession`
- Layer 2 typed sector profiles/aspects for domain-standard event and session semantics
- EAV as a governed extension/configuration layer at both event and session scope
- namespaced machine keys and typed validation
- explicit exposure and governance semantics
- event template instantiation plus session blueprint instantiation and versioned sync
- projection-backed discovery/search/export/moderation reads
- aggregate event-with-sessions view contracts for UX and federation-facing reads

That is the enterprise-grade, self-hostable, multi-tenant direction this plan should implement now.
