<!-- ABOUTME: Governance and lifecycle rules for Layer 3 custom properties across Event and EventSession. -->
<!-- ABOUTME: Defines the boundary between Layer 1 core fields, Layer 2 typed sector schema, Layer 3 custom extensions, and aggregate read views. -->

# Custom Properties

## Purpose

This document defines the production rules for Layer 3 custom properties.

The platform uses a 3-layer model for both `Event` and `EventSession`:

1. Layer 1 core fields on `Event`, `EventSession`, and other first-class aggregates.
2. Layer 2 typed sector schema such as `EventIslamicAspect`, `EventTechAspect`, and `EventSessionIslamicAspect`.
3. Layer 3 custom properties for tenant-specific or organizer-specific long-tail extensions at event scope or session scope.

Layer 3 exists to provide governed extensibility without becoming a parallel domain model.

The product boundary is locked by [ADR-006: Custom Properties Runtime Boundary](adr/ADR-006-custom-properties-runtime-boundary.md): custom properties are custom fields on existing first-class resources, not a runtime schema engine for user-defined entities or relationships.

`Event` remains the parent program/container aggregate.

`EventSession` remains the scheduled child aggregate.

Sessions may appear as first-class cards in UI/search, but canonical write modeling remains parent/child.

## Hard Boundary

Layer 3 must not redefine or replace Layer 1 or Layer 2 semantics.

Fields that are standard across a sector, required for filtering, moderation, policy, ranking, publication, export, or stable interoperability must not exist only as Layer 3 custom properties.

Examples in this repo:

- `MadhabId` belongs to Layer 2 via `EventIslamicAspect`.
- `GenderMode` belongs to Layer 2 via `EventIslamicAspect`.
- `SkillLevel` belongs to Layer 2 via `EventTechAspect`.
- prayer-relative scheduling belongs to Layer 2 session schema via `EventSessionIslamicAspect`.

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

Reserved Layer 2 semantic keys are centralized in
`CustomPropertySemanticReservations` and enforced through
`CustomPropertyGovernancePolicy` before duplicate reads or writes. This means a
tenant namespace such as `tenant.community/madhab_id` or
`tenant.sessions/offset_minutes` is rejected just like the corresponding
`sector.*` definition, because the collision is semantic rather than only
namespace-based.

## Collision Policy

Layer 3 cannot redefine Layer 2 meaning at event scope or session scope.

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

## Quota Enforcement

Custom-property quotas are resolved through `ICustomPropertyQuotaResolver`, backed by
tenant settings, system settings, then `CustomPropertyQuotaSettingDefinitions`
defaults. Quotas are application governance rules: handlers and application services
enforce them before creating or replacing definitions, options, value rows, template
payloads, or projection rebuild work.

Current covered paths:

- shared Organization/Group definitions enforce per-scope definition count and
  per-definition option count,
- event-local and session-local runtime definitions enforce per-resource definition
  count and per-definition option count,
- event and event-session templates enforce per-template definition count and
  per-definition option count before publishing or replacement,
- event and event-session template sync apply preflights the resulting runtime
  definition count and per-definition option count before mutating runtime
  definitions or options,
- event and session multi-value writes enforce the maximum ordinal row count per
  replacement payload,
- projection rebuild and drain paths enforce tenant batch-size limits,
- projection dirty-scope writes enforce tenant backlog limits before adding another
  pending row,
- governance reporting reads the same quota definitions for operator visibility.

The 2026-05-28 Phase 4.1 audit found template and template-sync quota gaps. Those
quota gaps are closed in the Phase 4.2 quota slice: template create/update paths
now enforce option caps, session-template create/update paths enforce definition
and option caps, and sync apply paths enforce the resulting runtime cardinality
before writes.

The Phase 4.2 semantic-reservation slice now prevents known event and session
Layer 2 concepts from being recreated as Layer 3 fields under tenant namespaces.
The purge/retire lifecycle slice makes normal delete and hard purge behavior
explicit across shared, event, and session custom-property definitions.

The Phase 4.3 operator-signal slice adds bounded projection status fields and
metrics for projection quota rejections plus hard-purge decisions. These signals
intentionally use bounded dimensions such as projection type, quota key, scope,
outcome, and blocker category. Raw custom-property namespace/key pairs, display
names, resource IDs, and purge reasons remain inspection data, not metric tags.

## Export And Moderation Boundaries

Custom-property export and moderation payloads must be explicit consumers of
projection or aggregate contracts. A flag alone is not a publication decision:
`ExposureLevel` is the ceiling, while `IsExportable` and `IsModerationRelevant`
are purpose-specific grants inside that ceiling.

Current repository state:

- calendar export remains core-field only and does not compose Layer 3 custom-property payloads,
- no dedicated custom-property export composer exists,
- no dedicated custom-property moderation composer or queue exists,
- admin projection row inspection may expose full row metadata only to authenticated governance/admin surfaces,
- public/export/moderation consumers must request or receive an exposure ceiling and must not read raw EAV rows directly.

If a future export or moderation composer is added, it must source from
projection/aggregate contracts, apply an explicit exposure ceiling, and include
tests proving internal custom properties do not leak through generated payloads.

## Projection Lifecycle

`EventCustomPropertyProjection` and `EventSessionCustomPropertyProjection` exist for query and read optimization.

It is not the source of truth.

Source of truth:

- event-local Layer 3 definitions,
- event-local Layer 3 values,
- session-local Layer 3 definitions,
- session-local Layer 3 values,
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

Aggregate read/view rule:

- parent event views may embed session summaries and selected session projections,
- aggregate event-with-sessions views are read models only,
- canonical event/session contracts stay separate.

Lifecycle:

- projection rows are generated on writes and template sync operations,
- projection rows are invalidated when relevant definitions, flags, options, or values change,
- projection rebuild tooling must be able to recompute them from source-of-truth rows,
- projection tables are a read-side optimization, not a business-rule engine.

## Automation Condition Guardrails

Custom properties may participate in Event lifecycle automation conditions only
when an application-layer policy explicitly approves the definition. The current
policy is `ICustomPropertyAutomationConditionPolicy` / `CustomPropertyAutomationConditionPolicy`.

Eligibility rules:

1. the definition must be tenant-owned (`tenant.*`) and not system-owned,
2. the definition must be active,
3. the definition must be filterable so automation evaluates projection-backed,
   indexed values rather than raw EAV rows,
4. the property type must be bounded and type-validated (`Text`, `Number`,
   `Option`, `Boolean`, or `DateTime`),
5. the `Namespace + Key` must not collide with reserved Layer 2 semantics, and
6. workflow-critical state must remain explicit domain state, never generic
   Layer 3 EAV.

The following concepts are explicitly not valid custom-property automation
conditions: dispatch status, email dispatch replay/parking state, automation
execution status, registration lifecycle/status, delivery attempt state, tenant
pause state, idempotency keys, and deduplication keys. If one of these concepts
is required by automation, model it as Layer 1/2 state or a dedicated durable
runtime table instead.

## Definition Lifecycle

Layer 3 definitions have two deletion paths:

1. Normal delete is retirement. It deactivates and soft-deletes the definition,
   retires options, soft-deletes values, and clears the default option. Event and
   session deletes also remove derived projection rows in the same application
   transaction. The `Namespace + Key` remains reserved while the retired
   definition or historical rows remain, so re-creation is not accidental.
   Repository delete paths must implement this as state mutation, not
   `DbSet.Remove`, because value and projection relationships are intentionally
   restrictive. Normal delete must therefore preserve historical rows instead of
   severing required relationships.
2. Hard purge is an explicit audited operator action. It requires a non-blank
   reason and is allowed only when the definition has no historical values, no
   projection rows, no audit log references, and no template-sync provenance.
   Dependency-free options may be physically deleted as part of the purge.

Purge eligibility is enforced twice: handlers return structured blocked
responses from a dependency summary, and repositories re-check dependencies
before executing physical deletes. The EF model also treats definition-to-value
and definition-to-projection relationships as restrictive rather than cascading,
so future regenerated migrations should preserve value/projection history from
accidental hard deletes.

Template-sync retirement is not a hard purge. Retiring a template-derived
definition or option deactivates the runtime row, clears defaults when needed,
and preserves historical value rows and provenance for supportability.

## Template Provenance And Versioning

Supportability requires stronger provenance than just template identity.

Event-local and session-local Layer 3 definitions and options must record:

- source template identifier,
- source template key,
- source template version,
- source template definition/option identifiers where relevant,
- instantiation timestamp,
- last sync timestamp when sync occurs.

The system must be able to answer:

1. which template produced this event-local or session-local definition,
2. which version was used,
3. when the event or session was instantiated,
4. whether the event diverged and whether a later sync happened.

Template changes do not silently rewrite existing events or sessions.

## Projection And Moderation Boundary

Layer 3 projections may support moderation workflows, but they are not the canonical truth for sector-standard moderation semantics.

Canonical moderation and policy truth must remain on Layer 1 core fields or Layer 2 typed schema whenever the concept is standard or policy-critical.

Layer 3 enriches curation and local extension. It must not become the hidden replacement for typed domain schema.

## Uniqueness Rules

Expected uniqueness scopes:

- shared definitions: owning scope + `Namespace + Key`
- event template definitions: template + `Namespace + Key`
- session template definitions: session template + `Namespace + Key`
- event-local definitions: event + `Namespace + Key`
- session-local definitions: session + `Namespace + Key`
- options: local parent definition + `Namespace + Key`

## Event And Session Layering

### Event

- Layer 1: parent-program core semantics
- Layer 2: event-wide typed sector schema
- Layer 3: event-local custom extensions, templates, and projections

### EventSession

- Layer 1: scheduled child-unit core semantics
- Layer 2: session-wide typed sector schema
- Layer 3: session-local custom extensions, session templates/blueprints, and projections

### Scope Rule

Choose scope first, then layer.

- if a concept is true for the whole program, it belongs on `Event`
- if a concept is true for a specific scheduled unit, it belongs on `EventSession`
- if it is sector-standard, it belongs in Layer 2
- if it is local long-tail semantics, it belongs in Layer 3

## Lexicon / Contract Direction

Canonical contracts should stay separate:

- `...event.core`
- `...eventsession.core`

Typed Layer 2 contracts should stay separate:

- `...event.islamic`
- `...event.tech`
- `...eventsession.islamic`
- future session typed profile contracts as needed

Layer 3 contracts should stay separate:

- `...event.extension`
- `...eventsession.extension`

Aggregate read contracts may merge them:

- `...event.view`
- `...event.withSessions.view`

Do not make the merged event-with-sessions view the canonical write contract.

Soft deletion and retirement behavior must be explicit. Re-creation semantics must not be left accidental.

## Recommended Local Custom Extensions

When an event session is configured with a flexible ending time (such as `SessionEndTimeType.OpenEnded`), organizers may still want to display an approximate duration on the public page for planning purposes. In these cases, it is recommended to define a Layer 3 custom property:

- **Namespace + Key:** `pack.sessions/estimated_duration_minutes` or `tenant.sessions/estimated_duration_minutes`
- **Label:** Estimated Duration (Minutes)
- **Description:** Approximate duration of the session in minutes for scheduling guidance.
- **Value Type:** `Integer`

Using a Layer 3 custom property for estimated duration ensures the core scheduling database schema remains clean and strictly focused on authoritative UTC instants, while allowing long-tail planning hints to be configured dynamically via templates.

## Related

- `docs/ARCHITECTURE.md`
- `docs/DOMAIN.md`
- `docs/EXTENSIBILITY.md`
- `docs/MODULAR_EVENTS.md`
- `dev/active/eav-custom-properties/eav-custom-properties-plan.md`
