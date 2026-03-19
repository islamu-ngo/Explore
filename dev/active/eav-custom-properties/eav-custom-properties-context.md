ABOUTME: Context file for the enterprise-grade EAV custom properties redesign.
ABOUTME: Read this first when resuming so implementation follows the hardened extension-layer architecture.

# EAV Custom Properties - Context

**Last Updated: 2026-03-19**

---

## SESSION PROGRESS (2026-03-19)

### ✅ COMPLETED
- Re-reviewed the active EAV dev docs after the first blueprint/template revision.
- Incorporated senior-architect feedback that hardens EAV beyond storage normalization.
- Repositioned EAV as an **internal extension/configuration layer**, not the canonical home of discovery/policy-critical semantics.
- Strengthened the plan with namespaced machine keys, explicit multi-value semantics, typed validation fields, exposure/publication flags, projection strategy, provenance/versioning, delete-retention rules, and governance categories.
- Revised the plan to explicitly use a 3-layer event architecture: Layer 1 universal event core, Layer 2 typed sector profiles/aspects, Layer 3 local custom extensions.
- Locked the rule that sector-standard semantics must use first-class typed relational schema, not Layer 3 EAV.
- Removed deferral language that conflicted with the requirement to plan the stronger model for direct implementation now.
- Gathered supporting signals from repo architecture plus AT Protocol guidance on separating canonical contracts, metadata overlays, and app-view aggregation.
- Verified that parts of the old baseline were stale: the current domain already has some first-class appearance fields and no longer exposes `MetadataJson` on `Event`, `Organization`, or `Group`.
- Started implementation in `Explore.Domain` and `Explore.Persistence` rather than only revising docs.
- Refactored the existing shared Layer 3 custom-property model to use `Namespace + Key`, typed validation columns, explicit exposure flags, default typed values, and `Ordinal` on value rows.
- Added new Layer 3 event template entities: `EventTemplate`, `EventTemplateCustomPropertyDefinition`, and `EventTemplateCustomPropertyOption`.
- Added new Layer 3 event-runtime entities: `EventCustomPropertyDefinition`, `EventCustomPropertyOption`, and `EventCustomPropertyValue` with template provenance fields.
- Added `EventCustomPropertyProjection` as the first projection/read-model entity for searchable/filterable/exportable/moderation-aware custom-property reads.
- Added EF Core configurations for the new template/runtime/projection entities and updated existing custom-property configurations to the new namespaced machine-key model.
- Updated `Explore.Persistence/ExploreDbContext.cs` with DbSets and named tenant/soft-delete query filters for the new Layer 3 entities.
- Verified the first implementation slice with `dotnet build --configuration Release --verbosity quiet`, `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`, and `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`.
- Added a dedicated docs file at `docs/CUSTOM_PROPERTIES.md` and updated `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/EXTENSIBILITY.md`, and `docs/MODULAR_EVENTS.md` so Layer 2/Layer 3 rules live outside `dev/active/`.
- Added `Explore.Domain/Constants/CustomPropertyNamespaces.cs`, `Explore.Domain/Constants/CustomPropertySemanticReservations.cs`, and `Explore.Domain/Constants/CustomPropertyIdentity.cs` to lock reserved roots, Layer 2 semantic reservations, and lowercase machine-identity normalization.
- Tightened the projection contract so `EventCustomPropertyProjection` is now one atomic row per projected value row instead of one merged row per property.
- Added `Event.Domain.UnitTests/CustomProperties/CustomPropertyGovernanceTests.cs` to verify normalization and governance-reservation behavior.
- Added `Explore.Application/Contracts/Services/ICustomPropertyGovernancePolicy.cs`, `Explore.Application/Contracts/Services/CustomPropertyGovernanceEvaluation.cs`, and `Explore.Application/Services/CustomPropertyGovernancePolicy.cs` as the reusable application-layer enforcement point for reserved roots and Layer 2 collision rejection.
- Registered the governance policy in `Explore.Application/ApplicationServicesRegistration.cs` so future CQRS handlers can consume one central boundary service.
- Added `Event.Application.UnitTests/Features/CustomProperties/CustomPropertyGovernancePolicyTests.cs` to verify reserved-namespace rejection, normalization, and Layer 2 semantic collision blocking.
- Added the first real shared-definition vertical slice:
  - `Explore.Application/Contracts/Persistence/ICustomPropertyDefinitionRepository.cs`
  - `Explore.Persistence/Repositories/CustomPropertyDefinitionRepository.cs`
  - `Explore.Application/DTOs/CustomPropertyDefinition/*`
  - `Explore.Application/Features/CustomPropertyDefinitions/*`
- Implemented create/list/details CQRS for shared `Organization` / `Group` custom-property definitions.
- Wired create flow through `ICustomPropertyGovernancePolicy`, scoped machine-key duplicate checks, and transactional option persistence.
- Updated `Explore.Application/Profiles/MappingProfile.cs` and `Explore.Persistence/PersistenceServicesRegistration.cs` for the new slice.
- Added `Event.Application.UnitTests/Features/CustomPropertyDefinitions/Commands/CreateCustomPropertyDefinitionCommandHandlerTests.cs` and `Event.Application.UnitTests/Features/CustomPropertyDefinitions/Validators/CreateCustomPropertyDefinitionDtoValidatorTests.cs`.
- Extended the shared-definition slice to full admin CRUD foundation with:
  - `UpdateCustomPropertyDefinitionDto` and `UpdateCustomPropertyDefinitionDtoValidator`
  - `UpdateCustomPropertyDefinitionCommand` / handler
  - `DeleteCustomPropertyDefinitionCommand` / handler
- Added repository support for tracked reads, duplicate checks excluding the current row, transactional option replacement, and feature-specific hard delete semantics.
- Updated the details query to throw `NotFoundException` when the definition is missing instead of silently mapping null.
- Added `Event.Application.UnitTests/Features/CustomPropertyDefinitions/Commands/UpdateCustomPropertyDefinitionCommandHandlerTests.cs` and `Event.Application.UnitTests/Features/CustomPropertyDefinitions/Commands/DeleteCustomPropertyDefinitionCommandHandlerTests.cs`.
- Fixed the EF Core design-time foreign-key naming collision between template/runtime option self-references and definition references by assigning explicit constraint names in `EventTemplateCustomPropertyOptionConfiguration.cs` and `EventCustomPropertyOptionConfiguration.cs`.
- Generated the clean development migration set in `Explore.Persistence/Migrations/`:
  - `20260319164102_InitDevelopmentSchema.cs`
  - `20260319164102_InitDevelopmentSchema.Designer.cs`
  - `ExploreDbContextModelSnapshot.cs`

### 🟡 IN PROGRESS
- Application/API/Blazor integration has not started yet.
- EF migration has not been generated yet for the new Layer 3 schema.
- Application-layer validator/handler enforcement for reserved namespaces and collision rejection has not been wired yet.
- CQRS handlers do not yet call the governance policy; the service is ready but not yet attached to create/update custom-property flows.
- Shared-definition create CQRS now calls the governance policy; update/delete flows and event-template/runtime flows still do not.
- Shared-definition create and update CQRS now call the governance policy; template/runtime flows still do not.
- Shared-definition delete uses feature-specific delete semantics so machine-key recreation is not blocked by stale soft-deleted rows.
- Design-time `ExploreDbContext` creation via `dotnet ef` now succeeds again.

### ⚠️ BLOCKERS
- Workspace is still dirty with many unrelated user changes, so follow-up implementation must keep edits isolated.

---

## Quick Resume

1. Read this file first.
2. Read `dev/active/eav-custom-properties/eav-custom-properties-plan.md` for the hardened architecture.
3. Read `dev/active/eav-custom-properties/eav-custom-properties-tasks.md` for the updated phase breakdown.
4. Start implementation from **Phase 1** only after preserving the Phase 0 architecture lock decisions already documented.
5. Do **not** treat raw EAV tables as the final discovery/search/publication query model.
6. Do **not** reintroduce live runtime inheritance for events.
7. Do **not** use Layer 3 custom properties as the default home for sector-standard semantics.

---

## Key Architectural Position

### What EAV Is

EAV is the platform's:

- tenant customization layer
- long-tail extension mechanism
- UI-flexible configuration model
- local metadata / extension surface

### What EAV Is Not

EAV is **not** the sole persistence model for:

- discovery-critical event semantics
- ranking logic
- moderation/trust contracts
- publication/export contracts
- cross-instance stable semantics
- analytics-critical canonical fields

If a property becomes central to those areas, it must be:

- promoted to a first-class field, or
- projected into a governed read model with explicit semantics

---

## Approved Runtime Model

### Layer 1 - Universal Event Core

- universal event semantics stay on `Event` and related core relational entities
- this remains the shared model across all sectors/domains

### Layer 2 - Typed Sector Profiles / Aspects

- sector-standard semantics belong in first-class typed schema
- current repo precedent already exists via `EventIslamicAspect`, `EventTechAspect`, and `EventSessionIslamicAspect`
- Layer 2 stays directly queryable, indexable, and policy-friendly

### Organization + Group

- tenant-scoped shared custom-property definitions remain in scope
- still typed, normalized, namespaced, and governed

### Event

- event templates define reusable versioned blueprints
- event creation instantiates event-local definitions/options/initial values
- event runtime reads use only event-local state
- template sync is explicit, version-aware, and operator-driven
- this is Layer 3 behavior, not a replacement for Layer 2 typed schema

### Projection Layer

- searchable/filterable/exportable/moderation-relevant custom properties are projected into dedicated read models
- discovery and hot query paths must not depend on raw EAV joins alone

---

## Important Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | **EAV is an extension layer** | Prevents custom properties from quietly becoming the canonical semantic contract |
| 2 | **Layer 2 typed schema is first-class** | Sector-standard semantics need relational fields, FKs, indexes, and direct moderation/policy support |
| 3 | **Events use template instantiation + explicit sync** | Runtime behavior stays deterministic while still allowing controlled evolution |
| 4 | **Machine identity uses `Namespace + Key`** | Display labels can change without breaking integrations, sync, or governance |
| 5 | **Display text is mutable** | Supports localization and admin renaming without semantic drift |
| 6 | **Multi-value semantics are explicit** | One row per value plus `Ordinal` avoids ambiguity for ordering and uniqueness |
| 7 | **Validation is typed and governed** | Avoids opaque validation strings and accidental rule DSLs |
| 8 | **Exposure/publication semantics are first-class** | Visibility, searchability, exportability, moderation relevance, and analytics relevance are explicit |
| 9 | **Projection strategy is in scope now** | Raw EAV is not sufficient for hot discovery/filter/search paths |
| 10 | **Template provenance is versioned** | Support/debugging must explain which template version created or synced an event |
| 11 | **Historical values survive retirement** | Enterprise supportability requires explainable historical state |
| 12 | **Dedicated appearance/branding columns stay outside EAV** | These are first-class product concepts, not arbitrary metadata |
| 13 | **Platform and tenant semantics can coexist via namespaces** | Enables system-owned property packs without breaking tenant flexibility |

---

## Repo And Research Signals

### Repo Signals

- `Explore.Domain/Settings/SettingRegistry.cs`
  - stable machine keys and explicit configuration governance already exist in the repo
- `Explore.Domain/Settings/SettingDefinition.cs`
  - typed settings metadata and constrained configuration are already familiar concepts here
- `docs/ARCHITECTURE.md`
  - supports separation between canonical persistence, application orchestration, and read-model serving
- `docs/EXTENSIBILITY.md`
  - existing extensibility model already prefers explicit composition, not magical inheritance
- `docs/MODULAR_EVENTS.md`
  - current repo already models Layer 2 through typed 1:1 aspect tables with direct filters and module guards
- `Explore.Domain/Event.cs`
  - current code already uses first-class event appearance fields and a `StorageObject` reference instead of a metadata blob

### AT Protocol Signals

- canonical records are the interoperable contract, not arbitrary metadata bags
- labels/metadata overlays are separate from canonical records
- app-view aggregation sits above the canonical layer
- namespaced ownership is explicit

### Translation To This Plan

- raw custom-property rows are internal extension/configuration data
- publication/discovery/search/export behavior must flow through governed projections
- machine keys and namespaces are mandatory so local extensions do not masquerade as global semantics
- sector-standard semantics should follow the repo's existing typed aspect/profile pattern instead of deepening Layer 3 EAV

---

## New Entity Families Planned

| Family | Purpose |
|--------|---------|
| Shared custom-property entities | Tenant-scoped Organization / Group extensions with namespaced identity and typed governance |
| Typed sector profile/aspect entities | First-class Layer 2 schema for domain-standard event semantics |
| Event template entities | Versioned reusable event blueprints |
| Event runtime entities | Event-owned definitions/options/values after instantiation or sync |
| Projection entities | Read-optimized searchable/filterable/exportable event custom-property views |

## Implemented In Current Slice

### Updated Existing Files

- `Explore.Domain/CustomPropertyDefinition.cs`
- `Explore.Domain/CustomPropertyOption.cs`
- `Explore.Domain/CustomPropertyValue.cs`
- `Explore.Persistence/Configurations/Entities/CustomPropertyDefinitionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/CustomPropertyOptionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/CustomPropertyValueConfiguration.cs`
- `Explore.Persistence/ExploreDbContext.cs`

### New Files Added

- `Explore.Domain/Enums/ExposureLevel.cs`
- `Explore.Domain/EventTemplate.cs`
- `Explore.Domain/EventTemplateCustomPropertyDefinition.cs`
- `Explore.Domain/EventTemplateCustomPropertyOption.cs`
- `Explore.Domain/EventCustomPropertyDefinition.cs`
- `Explore.Domain/EventCustomPropertyOption.cs`
- `Explore.Domain/EventCustomPropertyValue.cs`
- `Explore.Domain/EventCustomPropertyProjection.cs`
- `Explore.Persistence/Configurations/Entities/EventTemplateConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventTemplateCustomPropertyDefinitionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventTemplateCustomPropertyOptionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventCustomPropertyDefinitionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventCustomPropertyOptionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventCustomPropertyValueConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventCustomPropertyProjectionConfiguration.cs`

---

## Required Model Features

### Identity And Namespace

- `Namespace`
- `Key`
- normalized lowercase machine identity via `CustomPropertyIdentity`
- mutable `DisplayName`
- optional system-ownership marker

### Value And Validation

- typed value columns
- `Ordinal` on value rows
- explicit typed validation metadata (`MinLength`, `MaxLength`, `RegexPattern`, `MinNumber`, `MaxNumber`, `MinDateTime`, `MaxDateTime`, `AllowedUrlSchemes`)

### Exposure / Governance

- `ExposureLevel`
- `IsSearchable`
- `IsFilterable`
- `IsExportable`
- `IsModerationRelevant`
- `IsAnalyticsRelevant`
- `IsSystemOwned`

### Provenance / Lifecycle

- `TemplateKey`
- `Version`
- `SourceTemplateVersion`
- `InstantiatedAt`
- `LastSyncedFromTemplateAt`
- source-id-first sync matching with `Namespace + Key` fallback only for repair/backfill

---

## Technical Constraints

- Repositories return entities, never DTOs.
- Validators are manually instantiated, not injected.
- Commands return `BaseCommandResponse<Guid>` where that convention applies.
- File-scoped namespaces for new C# files.
- ABOUTME header on all new files.
- New entities follow audit + soft-delete interfaces where appropriate.
- Use named soft-delete query filters.
- Avoid generic abstractions that blur the boundary between template rows, event runtime rows, and projection rows.
- Do not use raw EAV-only queries for discovery-critical reads.
- Do not route sector-standard fields into Layer 3 when the repo already has a typed Layer 2 pattern.

---

## Implementation Guardrails

### Must Do

- Preserve the rule that EAV is extension/configuration, not the universal semantic contract.
- Preserve the 3-layer split: universal core, typed sector profile, and local custom extension.
- Make event creation + template instantiation transactional.
- Make template diff/sync explicit and version-aware.
- Use namespaced machine keys in all uniqueness rules and APIs.
- Route all future Layer 3 create/update handlers through `ICustomPropertyGovernancePolicy` before persistence.
- Keep the first shared-definition slice limited to `Organization` / `Group` until update/delete and option-edit semantics are stable.
- Reuse the shared-definition repository transaction pattern for future event-template and event-runtime option replacement flows.
- Keep projection updates part of the implementation, not an afterthought.
- Preserve historical values when definitions/options are retired.

### Must Not Do

- No runtime merge engine for event definitions.
- No opaque `ValidationRules` string/DSL.
- No reliance on mutable display labels for machine identity.
- No hidden exposure semantics inferred from type.
- No fallback to `MetadataJson` compatibility paths.
- No raw EAV-heavy search/discovery path as the long-term production design.
- No sector-standard fields in Layer 3 EAV as the first destination.

---

## Related Docs

- `CLAUDE.md`
- `docs/ARCHITECTURE.md`
- `docs/EXTENSIBILITY.md`
- `docs/DOMAIN.md`
- `docs/QUICK_REFERENCE.md`
- `.claude/skills/clean-architecture-rules/SKILL.md`
- `.claude/skills/cqrs-mediatr-guidelines/SKILL.md`
- `.claude/skills/dotnet-efcore-guidelines/SKILL.md`
