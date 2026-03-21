ABOUTME: Checklist for tracking the enterprise-grade EAV custom properties implementation across Event and EventSession.
ABOUTME: Reflects extension-layer boundaries, namespaced machine keys, projections, event/session template instantiation, and aggregate views.

# EAV Custom Properties - Task Checklist

**Last Updated: 2026-03-19**

---

## Phase 0: Architecture Lock ✅ COMPLETE

- [x] **0.1** Lock EAV as an extension/configuration layer rather than the universal semantic contract
- [x] **0.1A** Lock the 3-layer event model: Layer 1 universal core, Layer 2 typed sector profile, Layer 3 local custom extension
- [x] **0.1B** Lock the rule that sector-standard semantics must use first-class typed schema, not Layer 3 EAV
- [x] **0.1C** Lock the parent/child aggregate rule: `Event` stays the container aggregate and `EventSession` stays the scheduled child aggregate
- [x] **0.2** Replace live event-definition inheritance with blueprint/template instantiation
- [x] **0.3** Define explicit template sync as a version-aware, operator-driven workflow
- [x] **0.4** Lock machine identity on `Namespace + Key` rather than mutable names
- [x] **0.5** Lock explicit multi-value semantics with one row per value plus `Ordinal`
- [x] **0.6** Replace opaque validation strings with typed governed validation metadata
- [x] **0.7** Lock exposure/publication/search/filter/export/moderation semantics into the model
- [x] **0.8** Lock projection/read-model strategy for discovery-critical properties
- [x] **0.9** Lock delete-retention and provenance/versioning rules
- [x] **0.10** Update active dev docs so there is no lingering deferred architecture language

---

## Phase 1: Domain Layer ⏳ NOT STARTED

- [x] **1.1** Create core enums
  - `PropertyType`
  - `EntityTypeName`
  - `ExposureLevel`
- [x] **1.1A** Audit existing Layer 2 aspect/profile families (`EventIslamicAspect`, `EventTechAspect`, `EventSessionIslamicAspect`) and document what already belongs outside Layer 3
- [ ] **1.1B** Add any missing sector-standard fields to typed aspect/profile schema instead of routing them into EAV
- [x] **1.2** Create shared definition entities for Organization / Group
  - namespaced `Namespace + Key`
  - typed validation fields
  - exposure/search/filter/export flags
  - `Ordinal`-aware value rows
- [x] **1.3** Create event template entities
  - `EventTemplate`
  - `EventTemplateCustomPropertyDefinition`
  - `EventTemplateCustomPropertyOption`
  - includes `TemplateKey`, `Version`, publish state, and namespaced keys
- [x] **1.4** Create event runtime entities
  - `EventCustomPropertyDefinition`
  - `EventCustomPropertyOption`
  - `EventCustomPropertyValue`
  - includes provenance/version/sync fields
- [ ] **1.4A** Create event session template entities
  - `EventSessionTemplate`
  - `EventSessionTemplateCustomPropertyDefinition`
  - `EventSessionTemplateCustomPropertyOption`
  - includes session blueprint identity, parent event-template linkage, publish state, and namespaced keys
- [ ] **1.4B** Create event session runtime entities
  - `EventSessionCustomPropertyDefinition`
  - `EventSessionCustomPropertyOption`
  - `EventSessionCustomPropertyValue`
  - includes provenance/version/sync fields
- [x] **1.5** Create projection entities/value objects for searchable/filterable/exportable custom-property reads
- [ ] **1.5A** Create event session projection entities/value objects for searchable/filterable/exportable session custom-property reads
- [ ] **1.5B** Define aggregate event-with-sessions read/view model contracts
- [x] **1.6** Audit existing appearance / branding fields in `Event.cs`, `Organization.cs`, and `Group.cs` against the new governance model
- [x] **1.6A** Lock reserved namespace constants and Layer 2 semantic reservations in domain constants
- [x] **1.6B** Add machine-identity normalization helpers for `Namespace + Key`
- [ ] **1.7** Add only genuinely missing first-class appearance / branding fields; do not recreate metadata-blob storage
- [ ] **1.8** Remove stale metadata assumptions from the domain design baseline

---

## Phase 2: Persistence - EF Configurations ⏳ NOT STARTED

- [x] **2.0** Reconcile existing Layer 2 aspect/profile configurations with the 3-layer architecture
- [x] **2.1** Configure shared custom-property tables
  - namespaced uniqueness
  - ordinal constraints
  - typed validation columns
  - exposure/indexing columns
- [x] **2.2** Configure event template tables
  - `(TenantId, TemplateKey, Version)` uniqueness
  - namespaced uniqueness for definitions/options
- [ ] **2.2A** Configure event session template tables
  - `(EventTemplateId, SessionTemplateKey, Version)` uniqueness
  - namespaced uniqueness for session definitions/options
- [x] **2.3** Configure event runtime tables
  - `(EventId, Namespace, Key)` uniqueness
  - provenance/version mapping
  - value-row ordering constraints
- [ ] **2.3A** Configure event session runtime tables
  - `(EventSessionId, Namespace, Key)` uniqueness
  - provenance/version mapping
  - value-row ordering constraints
- [x] **2.4** Configure projection/read-model tables with discovery-oriented indexes
- [x] **2.4A** Redesign projection row shape to be atomic per projected value row rather than merged per property
- [ ] **2.4B** Configure session projection/read-model tables with discovery-oriented indexes
- [ ] **2.5** Reconcile `EventConfiguration.cs` with the hardened plan and preserve current first-class appearance mapping
- [ ] **2.6** Update `OrganizationConfiguration.cs` only where this initiative introduces real new fields/constraints
- [ ] **2.7** Update `GroupConfiguration.cs` only where this initiative introduces real new fields/constraints
- [x] **2.8** Add all new DbSets + query filters to `ExploreDbContext.cs`
- [x] **2.9** Create EF migration for the new schema

---

## Phase 3: Persistence - Repositories, Sync, And Projection Support ⏳ NOT STARTED

- [x] **3.1** Create shared-definition repository interfaces and implementations
- [ ] **3.2** Create event template repository interfaces and implementations
- [ ] **3.2A** Create event session template repository interfaces and implementations
- [ ] **3.3** Create event runtime repository interfaces and implementations
- [ ] **3.3A** Create event session runtime repository interfaces and implementations
- [ ] **3.4** Add transactional template-instantiation support for event creation
- [ ] **3.4A** Add transactional session-template instantiation support under event creation/editing
- [ ] **3.5** Add template diff / sync repository and service support
- [ ] **3.5A** Add event session template diff / sync repository and service support
- [ ] **3.6** Add projection updater / rebuilder persistence support
- [ ] **3.6A** Add session projection updater / rebuilder persistence support
- [ ] **3.7** Register repositories and services in `PersistenceServicesRegistration.cs`
- [ ] **3.8** Add source-id-first provenance matching with `Namespace + Key` fallback only for repair/backfill

---

## Phase 4: Application - DTOs And Mapping ⏳ NOT STARTED

- [x] **4.1** Create DTOs for shared Organization / Group custom properties
- [ ] **4.2** Create DTOs for event templates, template definitions, and template options
- [ ] **4.2A** Create DTOs for event session templates, template definitions, and template options
- [ ] **4.3** Create DTOs for event runtime definitions, options, and values
- [ ] **4.3A** Create DTOs for event session runtime definitions, options, and values
- [ ] **4.4** Create DTOs for template diff / sync workflows
- [ ] **4.4A** Create DTOs for event session template diff / sync workflows
- [ ] **4.5** Create projection DTOs for discovery/search/export-oriented reads
- [ ] **4.5A** Create aggregate event-with-sessions view DTOs and lexicon-facing contracts
- [ ] **4.6** Re-audit Event DTOs / generated contracts and remove any stale metadata-blob assumptions
- [ ] **4.7** Re-audit Organization DTOs / generated contracts and remove any stale metadata-blob assumptions
- [ ] **4.8** Re-audit Group DTOs / generated contracts and remove any stale metadata-blob assumptions
- [x] **4.9** Update mapping profiles for the first shared-definition CQRS slice

---

## Phase 5: Application - CQRS For Definitions, Templates, Values, Sync, And Projections ⏳ NOT STARTED

- [ ] **5.0** Preserve distinct CQRS paths for Layer 2 typed sector schema vs Layer 3 custom-property flows
- [ ] **5.0A** Add application-layer rejection for reserved namespaces and reserved Layer 2 semantic collisions
- [x] **5.0A** Add reusable application-layer governance policy for reserved namespaces and reserved Layer 2 semantic collisions
- [x] **5.1** Add first create/list/details CQRS slice for shared Organization / Group definitions
- [x] **5.1A** Add update/delete CQRS for shared Organization / Group definitions
- [x] **5.1B** Add option-edit semantics and duplicate-normalized-option-key rejection for shared definitions
- [ ] **5.2** Add CRUD commands / handlers / validators for event templates
- [ ] **5.2A** Add CRUD commands / handlers / validators for event session templates
- [ ] **5.3** Add CRUD commands / handlers / validators for event template options
- [ ] **5.3A** Add CRUD commands / handlers / validators for event session template options
- [ ] **5.4** Add queries for event runtime definitions and values
- [ ] **5.4A** Add queries for event session runtime definitions and values
- [ ] **5.5** Add commands for setting event runtime values with explicit single vs multi rules
- [ ] **5.5A** Add commands for setting event session runtime values with explicit single vs multi rules
- [ ] **5.6** Add commands for editing event-local definitions after instantiation
- [ ] **5.6A** Add commands for editing event session-local definitions after instantiation
- [ ] **5.7** Add commands / queries for template diff and sync
- [ ] **5.7A** Add commands / queries for event session template diff and sync
- [ ] **5.8** Add commands / jobs for projection updates and rebuilds
- [ ] **5.9** Add promotion/projection rules for discovery-critical properties
- [ ] **5.10** Add promotion rules that move sector-standard Layer 3 candidates into Layer 2 typed schema instead of expanding EAV dependence

---

## Phase 6: Event + EventSession Creation, Template Instantiation, And Editing Flow ⏳ NOT STARTED

- [ ] **6.1** Extend event creation contracts to accept optional template selection
- [ ] **6.2** Instantiate event-local definitions/options/default values transactionally
- [ ] **6.2A** Instantiate event session templates and session-local definitions/options/default values transactionally
- [ ] **6.3** Support events created without a template
- [ ] **6.4** Ensure event edit flows read event-local configuration only
- [ ] **6.5** Add explicit event admin workflow for template diff and sync decisions
- [ ] **6.5A** Add explicit event session admin workflow for template diff and sync decisions
- [ ] **6.6** Keep Layer 2 aspect/profile creation and editing distinct from Layer 3 custom-property creation and editing during event workflows
- [ ] **6.7** Add aggregate event page/read flow that embeds linked session summaries without collapsing canonical models

---

## Phase 7: Remove Stale Metadata Assumptions ⏳ NOT STARTED

- [ ] **7.1** Re-audit the source tree for actual remaining runtime `MetadataJson` / JSONB coupling
- [ ] **7.2** Remove stale comments, docs, or contracts that still assume metadata-blob storage
- [ ] **7.3** Align event list/query contracts with the current non-blob architecture if any stale parameters remain
- [ ] **7.4** Align Event write handlers with the first-class appearance model
- [ ] **7.5** Align Organization write handlers with the first-class appearance model
- [ ] **7.6** Align Group write handlers with the first-class branding model

---

## Phase 8: API Layer ⏳ NOT STARTED

- [x] **8.1** Create API endpoints for shared Organization / Group custom-property governance
- [ ] **8.2** Create API endpoints for event template management
- [ ] **8.2A** Create API endpoints for event session template management
- [ ] **8.3** Create API endpoints for event runtime definition/value management
- [ ] **8.3A** Create API endpoints for event session runtime definition/value management
- [ ] **8.4** Create API endpoints for template diff / sync workflows
- [ ] **8.4A** Create API endpoints for event session template diff / sync workflows
- [ ] **8.5** Create projection admin / rebuild endpoints if needed
- [ ] **8.6** Reconcile `EventController.cs` and related API contracts with template-aware event creation and remove any stale metadata query assumptions
- [ ] **8.6A** Reconcile `EventSessionController.cs` and related API contracts with session template/runtime workflows
- [ ] **8.7** Implement authorization categories
  - template admin
  - event editor
  - property governance admin
  - platform/system namespace editor

---

## Phase 9: Blazor Client ⏳ NOT STARTED

- [ ] **9.1** Reconcile `EventAppearanceMetadataHelper.cs` and related utilities with the existing first-class appearance model
- [ ] **9.2** Refactor `OrganizationAppearanceMetadataHelper.cs` to use dedicated columns
- [ ] **9.3** Refactor `GroupBrandingMetadataHelper.cs` to use dedicated columns
- [ ] **9.4** Add shared definition governance UI for Organization / Group
- [ ] **9.5** Add event template management UI
- [ ] **9.5A** Add event session template management UI
- [ ] **9.6** Add template selection to event creation UI
- [ ] **9.6A** Add session blueprint selection/editing UI
- [ ] **9.7** Build event runtime custom-property editor against event-local definitions/values
- [ ] **9.7A** Build event session runtime custom-property editor against session-local definitions/values
- [ ] **9.8** Add template diff / sync UI
- [ ] **9.8A** Add event session template diff / sync UI
- [ ] **9.9** Add governance UI for exposure/search/filter/export flags
- [ ] **9.10** Update Organization and Group pages to remove any stale metadata-blob assumptions
- [ ] **9.11** Regenerate generated API clients after contract changes

---

## Phase 10: Search, Projection, Moderation, Export, And Aggregate View Integration ⏳ NOT STARTED

- [ ] **10.0** Integrate Layer 2 typed sector fields directly into discovery, policy, moderation, and export paths without routing them through Layer 3 first
- [ ] **10.1** Populate custom-property projections on writes and sync operations
- [ ] **10.1A** Populate session custom-property projections on writes and sync operations
- [ ] **10.2** Integrate filterable/searchable projections into event discovery query paths
- [ ] **10.2A** Integrate filterable/searchable session projections into discovery query paths
- [ ] **10.3** Integrate public/exportable projections into export/publication payloads
- [ ] **10.3A** Integrate aggregate event-with-sessions views into publication/discovery payloads
- [ ] **10.4** Integrate moderation-relevant projections into governance workflows
- [ ] **10.5** Integrate analytics-relevant projections into analytics payload composition

---

## Phase 11: Testing And Documentation ⏳ NOT STARTED

- [ ] **11.1** Add architecture tests for the new entity families and boundaries
- [x] **11.1A** Add unit tests for namespace normalization and Layer 2 semantic reservation helpers
- [x] **11.1B** Add application-unit tests for reserved namespace rejection and Layer 2 semantic collision blocking
- [ ] **11.2** Add unit tests proving display-name renames do not break machine-key identity
- [ ] **11.3** Add unit tests for multi-value semantics, ordering, and duplicate rules
- [x] **11.4** Add first unit tests for shared-definition validator enforcement and create/update/delete handler behavior
- [ ] **11.5** Add unit tests for exposure/search/filter/export/moderation flags
- [ ] **11.6** Add unit tests for template instantiation, provenance, versioning, and sync
- [ ] **11.6A** Add unit tests for event session template instantiation, provenance, versioning, and sync
- [ ] **11.7** Add unit tests for retired definitions/options with historical values
- [ ] **11.8** Add integration tests for persistence constraints and tenant isolation
- [ ] **11.8A** Add integration tests for event session persistence constraints and tenant isolation
- [ ] **11.9** Add deeper integration tests for API roundtrips (template -> event -> sync -> projections)
- [ ] **11.9B** Add integration tests for API roundtrips (event template -> session blueprint -> event session -> sync -> projections)
- [x] **11.9A** Add initial API integration coverage for shared custom-property definition controller routes and auth posture
- [x] **11.10** Update docs: `docs/DOMAIN.md`, `docs/ARCHITECTURE.md`, `docs/EXTENSIBILITY.md`, `docs/MODULAR_EVENTS.md`, `docs/CUSTOM_PROPERTIES.md`
- [ ] **11.10A** Update event/session lexicon planning docs for canonical event/session contracts and aggregate read views

---

## Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| 0. Architecture Lock | 12 | ✅ |
| 1. Domain | 10 | ⏳ |
| 2. EF Configs | 10 | ⏳ |
| 3. Repositories / Sync / Projection | 7 | ⏳ |
| 4. DTOs / Mapping | 9 | ⏳ |
| 5. CQRS | 11 | ⏳ |
| 6. Event Creation / Editing | 6 | ⏳ |
| 7. Remove Stale Metadata Assumptions | 6 | ⏳ |
| 8. API | 7 | ⏳ |
| 9. Blazor | 11 | ⏳ |
| 10. Projection Integration | 6 | ⏳ |
| 11. Tests / Docs | 10 | ⏳ |
| **Total** | **104** | |
