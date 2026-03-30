ABOUTME: Context file for the enterprise-grade EAV custom properties redesign across Event and EventSession.
ABOUTME: Read this first when resuming so implementation follows the hardened extension-layer and parent/child aggregate architecture.

# EAV Custom Properties - Context

**Last Updated: 2026-03-29 (Milestone C complete)**

---

## SESSION PROGRESS (2026-03-29)

### ✅ MILESTONE A - COMPLETE (2026-03-19)
- All shared Organization/Group custom-property definitions implemented
- Governance policy, CRUD, migration baseline, API endpoints, HATEOAS, tests
- See detailed list below in "Milestone A Details" section

### ✅ MILESTONE B - COMPLETE (2026-03-29)
- ~60 files created/modified across all Clean Architecture layers
- Build: 0 errors, 790 pre-existing warnings
- Tests: 657/657 unit tests pass, 52/52 architecture tests pass

#### Milestone B Files Created/Modified

**Track 1 — Repository Interfaces + Implementations (6 files):**
- `Explore.Application/Contracts/Persistence/IEventTemplateRepository.cs` — template CRUD + paged list + uniqueness check + GetLatestPublished
- `Explore.Application/Contracts/Persistence/IEventCustomPropertyRepository.cs` — runtime def CRUD + values (get/set/multi) + paged list
- `Explore.Application/Contracts/Services/IEventTemplateInstantiationService.cs` — InstantiateFromTemplate + MatchByProvenance + result records
- `Explore.Persistence/Repositories/EventTemplateRepository.cs` — 3-level hierarchy (Template→Definitions→Options)
- `Explore.Persistence/Repositories/EventCustomPropertyRepository.cs` — def CRUD + value upsert/multi-replace
- `Explore.Application/Services/EventTemplateInstantiationService.cs` — in-memory instantiation with provenance, option ID remapping, default values

**Track 2 — DTOs + Validators (27 files):**
- `Explore.Application/DTOs/EventTemplate/` — 10 files: EventTemplateDto, ListDto, CreateDto, UpdateDto, DefinitionDto, DefinitionListDto, CreateDefinitionDto, UpdateDefinitionDto, OptionDto, CreateOptionDto
- `Explore.Application/DTOs/EventTemplate/Validators/` — 5 files: CreateTemplate, UpdateTemplate, CreateDefinition, UpdateDefinition, CreateOption validators
- `Explore.Application/DTOs/EventCustomProperty/` — 9 files: DefinitionDto, DefinitionListDto, CreateDefinitionDto, UpdateDefinitionDto, OptionDto, CreateOptionDto, ValueDto, SetValueDto, SetMultiValuesDto
- `Explore.Application/DTOs/EventCustomProperty/Validators/` — 3 files: CreateDefinition, UpdateDefinition, SetValue validators

**Track 3 — AutoMapper Mappings (1 modified):**
- `Explore.Application/Profiles/MappingProfile.cs` — added ~50 lines of mappings for template, template def/option, runtime def/option, and value entities

**Track 4 — Template CQRS (10 files):**
- `Explore.Application/Features/EventTemplates/Requests/Commands/` — CreateEventTemplateCommand, UpdateEventTemplateCommand, DeleteEventTemplateCommand
- `Explore.Application/Features/EventTemplates/Requests/Queries/` — GetEventTemplateListRequest, GetEventTemplateDetailsRequest
- `Explore.Application/Features/EventTemplates/Handlers/Commands/` — Create, Update, Delete handlers (with 3-level hierarchy support, governance, uniqueness checks)
- `Explore.Application/Features/EventTemplates/Handlers/Queries/` — List (HybridCache 5min/1min), Details handlers

**Track 5 — Runtime CQRS (16 files):**
- `Explore.Application/Features/EventCustomProperties/Requests/Commands/` — Create/Update/DeleteDefinition, SetValue, SetMultiValues commands
- `Explore.Application/Features/EventCustomProperties/Requests/Queries/` — GetDefinitionList, GetDefinitionDetails, GetValues queries
- `Explore.Application/Features/EventCustomProperties/Handlers/Commands/` — Create/Update/Delete def, SetValue (upsert), SetMultiValues (atomic replace) handlers
- `Explore.Application/Features/EventCustomProperties/Handlers/Queries/` — List (cached), Details, Values handlers

**Track 6 — Event Creation Integration (4 modified):**
- `Explore.Application/DTOs/Event/CreateEventDto.cs` — added `Guid? TemplateId` property
- `Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs` — added IEventTemplateRepository param + template existence validation
- `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs` — added template instantiation inside transaction (fetch template → guard published+active → instantiate → persist defs/options/values)
- `Event.Application.UnitTests/Features/Events/Commands/CreateEventCommandHandlerTests.cs` — added 3 mock fields for new dependencies

**Track 7 — API Endpoints + HATEOAS (8 created, 2 modified):**
- `Explore.API/Controllers/EventTemplateController.cs` — CRUD endpoints with HATEOAS
- `Explore.API/Controllers/EventCustomPropertyController.cs` — def CRUD + values GET/PUT endpoints
- `Explore.API/Hateoas/Policies/EventTemplateLinkPolicy.cs` — detail + collection link policies
- `Explore.API/Hateoas/Policies/EventCustomPropertyLinkPolicy.cs` — detail + collection link policies (includes Values link)
- `Explore.API/Hateoas/Assemblers/EventTemplateResourceAssembler.cs`
- `Explore.API/Hateoas/Assemblers/EventCustomPropertyResourceAssembler.cs`
- `Explore.API/Hateoas/RouteNames.cs` — added EventTemplate + EventCustomProperty route constants
- `Explore.API/Extensions/HateoasAssemblerRegistration.cs` — registered 6 new HATEOAS services

**Track 8 — Unit Tests (1 file, 16 tests):**
- `Event.Application.UnitTests/Services/EventTemplateInstantiationServiceTests.cs` — instantiation (13 tests) + provenance matching (5 tests)

**Track 9 — DI Registration (2 modified):**
- `Explore.Persistence/PersistenceServicesRegistration.cs` — added IEventTemplateRepository + IEventCustomPropertyRepository
- `Explore.Application/ApplicationServicesRegistration.cs` — added IEventTemplateInstantiationService

**Track 10 — Build Fix (1 modified):**
- `Event.Application.UnitTests/Features/Events/Commands/CreateEventCommandHandlerTests.cs` — added 3 constructor params for new dependencies

#### Key Capabilities Delivered in Milestone B
- Event Template CRUD with 3-level hierarchy (Template → Definitions → Options)
- Event Runtime Custom Properties CRUD + single/multi value setting
- Template Instantiation — transactional copy with full provenance tracking
- Provenance Matching — two-pass algorithm (SourceId first, Namespace+Key fallback)
- Event Creation with optional TemplateId auto-instantiation
- REST API endpoints with HATEOAS, output caching, authorization
- All patterns match existing Milestone A conventions exactly

#### Key Design Decisions Made in Milestone B
- `TemplateId` is `Guid?` on `CreateEventDto` — null = existing flow untouched (no-template path)
- Template instantiation happens INSIDE the existing event creation transaction
- `DefaultOptionId` set to null before `CreateWithOptions` to avoid FK violation on initial save (3-step: save def → save options → set default)
- Provenance matching: SourceTemplateDefinitionId first, then normalized Namespace+Key fallback for repair/backfill
- Runtime edit flows already event-local: queries use `GetDefinitionsForEventPaged(eventId)` which queries runtime entities, not templates
- Ad-hoc runtime definitions (created without template) get `InstantiatedAt = DateTimeOffset.UtcNow` but no provenance fields

### ✅ MILESTONE C - COMPLETE (2026-03-29)
- EventSession Layer 3 parity — 11 tracks completed
- Build: 0 errors, 790 pre-existing warnings
- Tests: 676/676 unit tests pass, 52/52 architecture tests pass

#### Milestone C Files Created/Modified

**Track 1 — Domain: 7 EventSession EAV entities:**
- `Explore.Domain/EventSessionTemplate.cs` — session blueprint, child of EventTemplate
- `Explore.Domain/EventSessionTemplateCustomPropertyDefinition.cs` — session template definition
- `Explore.Domain/EventSessionTemplateCustomPropertyOption.cs` — session template option
- `Explore.Domain/EventSessionCustomPropertyDefinition.cs` — session runtime definition
- `Explore.Domain/EventSessionCustomPropertyOption.cs` — session runtime option
- `Explore.Domain/EventSessionCustomPropertyValue.cs` — session runtime value
- `Explore.Domain/EventSessionCustomPropertyProjection.cs` — session projection entity

**Track 2 — EF + DbContext: 7 configs + DbSets + named query filters:**
- `Explore.Persistence/Configurations/Entities/EventSessionTemplateConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventSessionTemplateCustomPropertyDefinitionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventSessionTemplateCustomPropertyOptionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventSessionCustomPropertyDefinitionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventSessionCustomPropertyOptionConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventSessionCustomPropertyValueConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventSessionCustomPropertyProjectionConfiguration.cs`
- `Explore.Persistence/ExploreDbContext.cs` — added 7 DbSets + 7 named query filters

**Track 3 — Repos + Service:**
- `Explore.Application/Contracts/Persistence/IEventSessionTemplateRepository.cs`
- `Explore.Application/Contracts/Persistence/IEventSessionCustomPropertyRepository.cs`
- `Explore.Application/Contracts/Services/IEventSessionTemplateInstantiationService.cs`
- `Explore.Persistence/Repositories/EventSessionTemplateRepository.cs`
- `Explore.Persistence/Repositories/EventSessionCustomPropertyRepository.cs`
- `Explore.Application/Services/EventSessionTemplateInstantiationService.cs`

**Track 4 — DTOs + Validators:**
- `Explore.Application/DTOs/EventSessionTemplate/` — 10 files (Dto, ListDto, Create/Update DTOs for template/definition/option)
- `Explore.Application/DTOs/EventSessionTemplate/Validators/` — 5 validators
- `Explore.Application/DTOs/EventSessionCustomProperty/` — 9 files (Definition/Option/Value DTOs)
- `Explore.Application/DTOs/EventSessionCustomProperty/Validators/` — 3 validators

**Track 5 — AutoMapper Mappings:**
- `Explore.Application/Profiles/MappingProfile.cs` — added session template + runtime mappings

**Track 6 — CQRS Template (10 files):**
- `Explore.Application/Features/EventSessionTemplates/Requests/Commands/` — Create, Update, Delete
- `Explore.Application/Features/EventSessionTemplates/Requests/Queries/` — List, Details
- `Explore.Application/Features/EventSessionTemplates/Handlers/Commands/` — Create, Update, Delete handlers
- `Explore.Application/Features/EventSessionTemplates/Handlers/Queries/` — List (cached), Details

**Track 7 — CQRS Runtime (16 files):**
- `Explore.Application/Features/EventSessionCustomProperties/Requests/Commands/` — Create/Update/Delete def, SetValue, SetMultiValues
- `Explore.Application/Features/EventSessionCustomProperties/Requests/Queries/` — List, Details, Values
- `Explore.Application/Features/EventSessionCustomProperties/Handlers/Commands/` — 5 handlers
- `Explore.Application/Features/EventSessionCustomProperties/Handlers/Queries/` — 3 handlers

**Track 8 — Session Creation Integration:**
- `Explore.Application/DTOs/EventSession/CreateEventSessionDto.cs` — added `Guid? SessionTemplateId`
- `Explore.Application/DTOs/EventSession/Validators/CreateEventSessionDtoValidator.cs` — template validation
- `Explore.Application/Features/EventSessions/Handlers/Commands/CreateEventSessionCommandHandler.cs` — template instantiation

**Track 9 — API/HATEOAS:**
- `Explore.API/Controllers/EventSessionTemplateController.cs`
- `Explore.API/Controllers/EventSessionCustomPropertyController.cs`
- `Explore.API/Hateoas/Policies/EventSessionTemplateLinkPolicy.cs`
- `Explore.API/Hateoas/Policies/EventSessionCustomPropertyLinkPolicy.cs`
- `Explore.API/Hateoas/Assemblers/EventSessionTemplateResourceAssembler.cs`
- `Explore.API/Hateoas/Assemblers/EventSessionCustomPropertyResourceAssembler.cs`
- `Explore.API/Hateoas/RouteNames.cs` — added session template + runtime route constants
- `Explore.API/Extensions/HateoasAssemblerRegistration.cs` — registered 6 session HATEOAS services

**Track 10 — Unit Tests (1 file, 19 tests):**
- `Event.Application.UnitTests/Services/EventSessionTemplateInstantiationServiceTests.cs`

**Track 11 — DI Registration:**
- `Explore.Persistence/PersistenceServicesRegistration.cs` — added session repos
- `Explore.Application/ApplicationServicesRegistration.cs` — added session instantiation service

#### Key Capabilities Delivered in Milestone C
- EventSession Template CRUD with 3-level hierarchy (SessionTemplate → Definitions → Options)
- EventSession Runtime Custom Properties CRUD + single/multi value setting
- Session Template Instantiation — transactional copy with full provenance tracking
- Session creation with optional SessionTemplateId auto-instantiation
- REST API endpoints with HATEOAS, output caching, authorization
- Full architectural parity with Event (Milestone B) patterns

#### Key Design Decisions Made in Milestone C
- Session templates are owned children of event templates (`EventTemplateId` FK)
- `SessionTemplateId` is `Guid?` on `CreateEventSessionDto` — null = no template
- Session instantiation follows exact same in-memory service + handler persistence pattern as events
- Session provenance matching: same two-pass algorithm (SourceId first, Namespace+Key fallback)
- Session projection entity mirrors event projection shape for future discovery integration

### 🟡 IN PROGRESS
- All Milestone A/B/C changes are UNCOMMITTED — need `git add` + commit
- Milestones D-F remain planned (projections, sync, aggregate views)

### ⚠️ BLOCKERS
- Workspace is still dirty with many unrelated user changes, so follow-up implementation must keep edits isolated
- Full `Event.API.IntegrationTests` still has unrelated pre-existing failures

### Milestone A Details (2026-03-19)
- Locked architecture decisions (3-layer model, EAV as extension layer, template instantiation)
- Domain entities, EF configurations, DbContext updates, migration
- Shared definition CRUD (repos, DTOs, validators, CQRS, controller, HATEOAS)
- Governance policy (reserved namespaces, Layer 2 collision blocking)
- Unit tests + API integration tests
- Docs updates (ARCHITECTURE, DOMAIN, EXTENSIBILITY, MODULAR_EVENTS, CUSTOM_PROPERTIES)

---

## Quick Resume

1. Read this file first.
2. Read `dev/active/eav-custom-properties/eav-custom-properties-plan.md` for the hardened architecture.
3. Read `dev/active/eav-custom-properties/eav-custom-properties-tasks.md` for the updated phase breakdown.
4. Start implementation from **Phase 1** only after preserving the Phase 0 architecture lock decisions already documented.
5. Do **not** treat raw EAV tables as the final discovery/search/publication query model.
6. Do **not** reintroduce live runtime inheritance for events.
7. Do **not** use Layer 3 custom properties as the default home for sector-standard semantics.
8. Do **not** collapse `EventSession` into peer `Event` rows; use parent/child aggregates plus aggregate read views.
9. Do **not** implement full sync UX/API before the Event and EventSession runtime baselines are stable.

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

### Layer 1 - Universal Event And EventSession Core

- universal event semantics stay on `Event` and related core relational entities
- universal session semantics stay on `EventSession` and related core relational entities
- both remain the shared Layer 1 model across all sectors/domains

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

### EventSession

- event sessions remain child aggregates, not standalone peer events
- session templates/blueprints define reusable session defaults under an event template
- session creation instantiates session-local definitions/options/initial values
- session runtime reads use only session-local state
- session sync is explicit, version-aware, and operator-driven
- this is Layer 3 behavior at session scope, not a replacement for Layer 2 typed session schema

### Aggregate Views And Lexicons

- aggregate read models may merge parent event data and child session summaries for UX/discovery/export
- canonical contracts remain separate for event and session
- lexicon direction is: separate canonical records, separate typed/extension records, plus merged event-with-sessions view contracts

### Delivery Position

- Milestone A is the done baseline and should not keep expanding.
- Milestone B is the done Event runtime/template baseline.
- Milestone C is the done EventSession Layer 3 parity baseline.
- Milestone D (projections), E (sync), and F (aggregate views/publication) are later milestones.

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
- `Explore.Domain/EventSession.cs`
  - session already exists as its own aggregate, which supports extending the 3-layer model instead of collapsing sessions into peer events
- `Explore.Domain/EventSessionIslamicAspect.cs`
  - session-specific typed Layer 2 precedent already exists in the repo

### AT Protocol Signals

- canonical records are the interoperable contract, not arbitrary metadata bags
- labels/metadata overlays are separate from canonical records
- app-view aggregation sits above the canonical layer
- namespaced ownership is explicit

### Translation To This Plan

- raw custom-property rows are internal extension/configuration data
- publication/discovery/search/export behavior must flow through governed projections
- machine keys and namespaces are mandatory so local extensions do not masquerade as global semantics
- sector-standard event and session semantics should follow the repo's existing typed aspect/profile pattern instead of deepening Layer 3 EAV
- aggregate views can merge parent event and child session data without collapsing canonical contracts

---

## New Entity Families Planned

| Family | Purpose |
|--------|---------|
| Shared custom-property entities | Tenant-scoped Organization / Group extensions with namespaced identity and typed governance |
| Typed sector profile/aspect entities | First-class Layer 2 schema for domain-standard event and session semantics |
| Event template entities | Versioned reusable event blueprints |
| EventSession template entities | Versioned reusable session blueprints under an event template |
| Event runtime entities | Event-owned definitions/options/values after instantiation or sync |
| EventSession runtime entities | Session-owned definitions/options/values after instantiation or sync |
| Projection entities | Read-optimized searchable/filterable/exportable event and session custom-property views |

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
