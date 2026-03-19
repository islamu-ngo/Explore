ABOUTME: Governance and lifecycle rules for Layer 3 custom properties and event templates.
ABOUTME: Defines the boundary between Layer 1 core fields, Layer 2 typed sector schema, and Layer 3 custom extensions.

# Custom Properties

## Purpose

This document defines the production rules for Layer 3 custom properties.

The platform uses a 3-layer model:

1. Layer 1 core fields on `Event` and other first-class aggregates.
2. Layer 2 typed sector schema such as `EventIslamicAspect`, `EventTechAspect`, and `EventSessionIslamicAspect`.
3. Layer 3 custom properties for tenant-specific or organizer-specific long-tail extensions.

Layer 3 exists to provide governed extensibility without becoming a parallel domain model.

## Hard Boundary

Layer 3 must not redefine or replace Layer 1 or Layer 2 semantics.

Fields that are standard across a sector, required for filtering, moderation, policy, ranking, publication, export, or stable interoperability must not exist only as Layer 3 custom properties.

Examples in this repo:

- `MadhabId` belongs to Layer 2 via `EventIslamicAspect`.
- `GenderMode` belongs to Layer 2 via `EventIslamicAspect`.
- `SkillLevel` belongs to Layer 2 via `EventTechAspect`.

If a Layer 3 field becomes sector-standard, it must be promoted into typed Layer 2 schema instead of deepening EAV dependence.

## Reserved Namespaces

`Namespace + Key` is the machine identity for all custom-property definitions and options.

Reserved namespace categories:

- `platform.*` — platform-owned semantics and packs managed by the platform.
- `sector.*` — reserved bridge space for sector/domain meanings that must not be tenant-defined casually.
- `tenant.*` — tenant-owned local semantics.
- `pack.*` — curated extension packs and templates shipped by the platform.

Rules:

1. Tenant-owned properties must not use reserved platform or sector namespaces.
2. Platform/system-owned definitions may force specific governance flags.
3. Layer 3 keys must not collide with reserved Layer 2 meanings even if the namespace differs.

## Collision Policy

Layer 3 cannot redefine Layer 2 meaning.

The application layer must reject creation or update of a Layer 3 definition when:

1. the namespace is reserved for platform or sector ownership and the caller lacks the required role,
2. the `Namespace + Key` collides with an existing Layer 3 definition in the same owning scope,
3. the `Namespace + Key` is mapped to a known Layer 2 semantic concept,
4. the definition attempts to encode policy-critical semantics that already exist as first-class typed fields.

This collision rule protects typed sector schema from being bypassed by later custom-property additions.

## Validation Scope

Layer 3 validation is intentionally limited.

Allowed validation categories:

- input correctness,
- requiredness,
- simple length/range constraints,
- regex/pattern checks,
- allowed URL schemes,
- option membership,
- multi-value shape rules.

Layer 3 validation must not become:

- policy evaluation,
- moderation logic,
- ranking rules,
- dynamic authorization,
- a general-purpose rules engine.

Those concerns belong in typed application logic, governance services, or Layer 2 schema.

## Governance Flags

Layer 3 definitions carry explicit exposure and lifecycle flags:

- `ExposureLevel`
- `IsSearchable`
- `IsFilterable`
- `IsExportable`
- `IsModerationRelevant`
- `IsAnalyticsRelevant`
- `IsSystemOwned`

Flag meanings are fixed.

- `ExposureLevel` controls who may see the property in non-admin read surfaces.
- `IsSearchable` means the property may contribute to search-oriented projection payloads.
- `IsFilterable` means the property may contribute to faceting and filter query surfaces.
- `IsExportable` means the property may appear in export/publication payloads when exposure rules also allow it.
- `IsModerationRelevant` means the property may be copied into moderation-oriented derived read models.
- `IsAnalyticsRelevant` means the property may be included in analytics payload composition.
- `IsSystemOwned` means tenant editors cannot treat the definition as a normal tenant-local field.

Handlers, jobs, and UI must not reinterpret these flags locally.

## Projection Lifecycle

`EventCustomPropertyProjection` exists for query and read optimization.

It is not the source of truth.

Source of truth:

- event-local Layer 3 definitions,
- event-local Layer 3 values,
- typed Layer 1 and Layer 2 schema.

Projection rules:

1. Only properties with projection-relevant flags are copied into projection rows.
2. Projection rows are derived and rebuildable.
3. Projection rows may serve discovery, filtering, moderation support, and export shaping.
4. Raw Layer 3 rows remain authoritative for custom-property state.
5. Layer 2 and Layer 1 fields remain authoritative for policy-critical semantics.

Projection row shape:

- one projection row per projected value row,
- copied machine identity (`Namespace + Key`),
- copied governance flags,
- atomic typed values and normalized value for query surfaces,
- explicit `Ordinal` for multi-value ordering.

Lifecycle:

- projection rows are generated on writes and template sync operations,
- projection rows are invalidated when relevant definitions, flags, options, or values change,
- projection rebuild tooling must be able to recompute them from source-of-truth rows,
- projection tables are a read-side optimization, not a business-rule engine.

## Template Provenance And Versioning

Supportability requires stronger provenance than just template identity.

Event-local Layer 3 definitions and options must record:

- source template identifier,
- source template key,
- source template version,
- source template definition/option identifiers where relevant,
- instantiation timestamp,
- last sync timestamp when sync occurs.

The system must be able to answer:

1. which template produced this event-local definition,
2. which version was used,
3. when the event was instantiated,
4. whether the event diverged and whether a later sync happened.

Template changes do not silently rewrite existing events.

## Projection And Moderation Boundary

Layer 3 projections may support moderation workflows, but they are not the canonical truth for sector-standard moderation semantics.

Canonical moderation and policy truth must remain on Layer 1 core fields or Layer 2 typed schema whenever the concept is standard or policy-critical.

Layer 3 enriches curation and local extension. It must not become the hidden replacement for typed domain schema.

## Uniqueness Rules

Expected uniqueness scopes:

- shared definitions: owning scope + `Namespace + Key`
- template definitions: template + `Namespace + Key`
- event-local definitions: event + `Namespace + Key`
- options: local parent definition + `Namespace + Key`

Soft deletion and retirement behavior must be explicit. Re-creation semantics must not be left accidental.

## Related

- `docs/ARCHITECTURE.md`
- `docs/DOMAIN.md`
- `docs/EXTENSIBILITY.md`
- `docs/MODULAR_EVENTS.md`
- `dev/active/eav-custom-properties/eav-custom-properties-plan.md`
