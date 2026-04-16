ABOUTME: Strategic plan for a 3-layer event and event-session architecture: universal core, typed sector profiles, and custom extensions.
ABOUTME: Treats EAV as the Layer 3 extension system only, with governed semantics, template instantiation, projection support, and aggregate read views.

# EAV Custom Properties - Implementation Plan

**Last Updated: 2026-04-11 (Milestones A+B+C complete; Milestones D/E/F hardened against 2025-2026 industry best practices; CTO architecture review incorporated with delivery-discipline tightening)**

---

## Executive Summary

Replace the remaining ad hoc metadata coupling and stale `MetadataJson` assumptions across docs/contracts with an enterprise-grade 3-layer event and event-session architecture where the custom-properties system serves only the extension layer.

- normalized and typed
- deterministic and debuggable for self-hosted deployments
- explicit about machine identity, validation, exposure, and provenance
- safe for multi-tenant extensibility without becoming the canonical home of core product semantics

The prior revision correctly moved events away from live shared-definition inheritance and toward template-based instantiation. This revision hardens the plan further by making the missing middle layer explicit and by locking concrete delivery patterns for the remaining projection, sync, and publication milestones:

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
- **projection updates follow a live/transactional baseline before any eventual-consistency escalation**
- **template sync follows the Jira two-rule pattern (propagate on confirmation, copy on new instantiation) rather than implicit inheritance**
- **EAV promotion follows the Atlassian 4-question framework so sector-standard or discovery-critical attributes migrate to Layer 2 instead of calcifying in Layer 3**
- **aggregate read views use EF Core keyless entities and explicit query composition, not runtime joins against raw EAV tables**
- **canonical lexicon contracts apply ATProto evolution discipline (add-only optional fields; breaking changes require a new NSID version)**

This aligns the custom-properties architecture with the platform's long-term need to separate:

- interoperability contracts
- curation / policy logic
- internal persistence / configuration

### Research Alignment (2025-2026)

The remaining milestones are planned against these external evidence anchors:

- **Kurrent.io - Live projections for CQRS read models (2025)**: start with transactional (live) projection updates; only add async/eventual-consistency flows when baseline proves insufficient. This locks our Milestone D baseline.
- **Atlassian Jira custom fields (Apr 2025, Mar 2026)**: two-rule template sync (propagate on operator confirmation + copy on instantiation) and the 4-question promotion framework (cross-tenant reporting? automation/AI? search/filter? stability?). Locked as Milestone E sync semantics and Rule 12 promotion criteria.
- **ATProto Lexicon specification + style guide (2026)**: add-only evolution, NSID versioning, experimental `.temp.` namespaces. Locked as Milestone F lexicon discipline.
- **Microsoft Learn + Chris Woodruff - EF Core Keyless Entity Types (Feb 2025)**: `[Keyless]` + `HasNoKey()` + `ToView()`/`ToSqlQuery()` for read-only aggregate views. Locked as Milestone F aggregate view implementation strategy.
- **architecture-weekly.com - Rebuilding event-driven read models (2026)**: projection status tracking table (name, version, status) + PostgreSQL advisory locks for rebuild coordination. Locked as Milestone D rebuild tooling pattern.
- **Clean Architecture skill + dotnet-efcore-guidelines skill (project)**: interface placement, specification pattern, named query filters, pooled DbContext, manual validator instantiation. Non-negotiable.
- **Orchard Core ContentFields pattern**: validates field/index separation (our projection tables = their index tables) but NOT a pattern to adopt wholesale because Orchard uses YesSql, not EF Core.
- **Anti-pattern: nopCommerce `GenericAttribute` (string-only values)**: cautionary reference. Our normalized + typed model is deliberately stricter.
- **Anti-pattern: single-JSONB blob (ABP ExtensibleObject)**: rejected for Layer 3 runtime to preserve typed validation, audit, soft delete, multi-value semantics, and governed exposure flags.

### CTO Architecture Review - Incorporated (2026-04-11)

A senior-CTO architecture review graded the plan **Approve the direction. Tighten the execution.** The review explicitly flagged three must-haves before greenlighting Milestone D implementation, plus a set of delivery-discipline tightenings. All of them are now locked into this plan:

**Three greenlight gates (required before any Milestone D code lands):**

1. **Dirty-scope recovery mechanism** for inline projection writes that skip during rebuild contention (see `Projection Rebuild Coordination (Milestone D)` below). The plan previously said "skip and trust rebuild" - the review correctly pointed out this has an edge case where rows written after the rebuild's scan window are missed until a later edit. The plan now adds an explicit `custom_property_projection_dirty_scope` table that inline writers upsert into on skip, and the rebuild worker drains on completion.
2. **Internal Milestone D sub-gate split** (D1 Correctness → D2 Operability → D3 Consumption). The review correctly flagged that Milestone D carries three hidden initiatives: projection correctness, projection operability, and discovery consumption. The plan now surfaces three internal exit gates so D2 cannot begin until D1 is proven and D3 cannot begin until D2 is proven.
3. **Explicit technical concurrency strategy** locked across templates, runtime definitions, and sync workflows. The plan previously said "optimistic concurrency" vaguely. It now locks: **EF Core `IsConcurrencyToken` on `ConcurrencyStamp` (`Guid`) for technical persistence conflict detection** (applied consistently across all mutable aggregates) + **`SourceTemplateVersion` / `TemplateVersion` for business-level sync provenance** (explicitly not confused with technical concurrency). These are two different concerns and remain distinct.

**Additional tightenings adopted:**

- **Keep sync implementation boring**: Milestone E will not build a generic schema-merge engine. Explicit `EventTemplateDiffService` + `EventSessionTemplateDiffService` with hand-written field comparisons, explicit DTOs, explicit audit output. A little duplication is healthier than a clever abstraction.
- **Operational governance surface for Rule 12**: governance review is not just a rule - it is an admin inspection/reporting surface. A new admin query and endpoint list all active Layer 3 properties by tenant with their exposure/searchable/filterable/moderation/analytics flags and flags candidates for promotion. See new section `Operational Governance Surface`.
- **Hard limits / quotas enforced in configuration**: enterprise self-hostable software needs guardrails because admins can misconfigure into pain. New section `Hard Limits And Quotas` locks explicit ceilings on custom properties per tenant, options per definition, multi-value rows per value, rebuild batch size, and diff apply payload size.
- **Simplified authorization taxonomy**: the initial policy list collapses from seven to four core policies (`template_admin`, `event_editor`, `property_governance_admin`, `platform_namespace_editor`). The additional policies from the previous revision are rolled into these four until workflows demand further subdivision.
- **Milestone F narrowed**: only an aggregate read model for UI/API + a lexicon decision document. Actual federation publication machinery is out. The aggregate view exists to prevent EAV becoming a query surface; it is not a publication platform.
- **Ruthless milestone sequencing**: D1 → D2 → D3 → E → F. No cross-milestone coupling, no "start E in parallel while D3 stabilizes." Locked as Rule 17.
- **Repairability is first-class**: for every advanced mechanism an operator must be able to answer "what is broken / stale / rebuildable / source of truth / how do I recover." Every new entity, service, and endpoint in D/E/F must answer these questions in documentation before it exits its gate.

The CTO review also explicitly validated several existing decisions: 3-layer separation, transactional live projection baseline, parent/child aggregate for Event/Session, Jira two-rule sync, state-based over event-sourced sync, normalized Layer 3 over JSONB, keyless-entity aggregate view over materialized view. Those stay exactly as they were.

## Execution Reset

The architecture is approved.

The delivery plan must now be milestone-based rather than treated as one uninterrupted platform initiative.

This plan is intentionally split into hard, test-gated delivery milestones so the platform reaches stable baselines before adding the next lifecycle layer.

### Delivery Milestones

#### Milestone A - Shared Definitions Foundation ✅ COMPLETE (2026-03-19)

Scope:

- Organization / Group shared definitions
- namespaced machine identity
- governance policy
- CRUD
- foundational tests

Gate exit evidence:

- shared-definition CRUD stable
- governance policy stable (`ICustomPropertyGovernancePolicy` blocks reserved namespaces and Layer 2 semantic collisions)
- duplicate machine-key rules stable via normalized `Namespace + Key` uniqueness
- migration baseline created
- tests green (validator + handler + controller integration)

#### Milestone B - Event Layer 3 Runtime Baseline ✅ COMPLETE (2026-03-29)

Scope:

- `EventTemplate`
- `EventCustomProperty*`
- event template repositories and CQRS
- event create/edit reads and writes
- baseline event projection entities

Gate exit evidence:

- event instantiation stable (transactional inside `CreateEventCommandHandler`)
- event-local runtime reads/writes stable (`GetDefinitionsForEventPaged`, `SetEventCustomPropertyValue`, `SetEventCustomPropertyMultiValues`)
- provenance tests green (13 instantiation + 5 provenance tests in `EventTemplateInstantiationServiceTests`)
- ~60 files touched, 657/657 unit tests + 52/52 architecture tests pass

Explicitly deferred (moved to later milestones):

- Projection updater service and write-path integration → Milestone D
- Operator-driven template diff/sync UX → Milestone E
- Aggregate event-with-sessions read view → Milestone F

#### Milestone C - EventSession Layer 3 Parity ✅ COMPLETE (2026-03-29)

Scope:

- `EventSessionTemplate` (owned child of `EventTemplate`)
- `EventSessionCustomProperty*`
- session template/runtime repositories and CQRS
- session create/edit reads and writes
- baseline session projection entities

Gate exit evidence:

- session template/runtime baseline stable
- session-local reads/writes stable
- session provenance tests green (19 tests in `EventSessionTemplateInstantiationServiceTests`)
- 11 delivery tracks shipped, 676/676 unit tests + 52/52 architecture tests pass

Design lock that carries forward:

- session template ownership stays nested under event template (no standalone session catalog in this phase)
- session-level Islamic aspect is still inline during session creation (Layer 2)
- session-level Layer 3 instantiation mirrors event-level in-memory instantiation service

#### Milestone D - Projection Integration ⏳ NEXT

Primary objective: deliver a **live transactional projection baseline** that powers discovery/search/filter/export for Layer 3 without leaking into Layer 1 or Layer 2 code paths.

**Internal sub-gate sequencing (locked, CTO review 2026-04-11):**

Milestone D contains three hidden initiatives. To prevent delivery sprawl they are internally split into three sequenced sub-gates, each with its own exit criteria. You cannot begin the next sub-gate until the previous one is proven green in integration tests.

| Sub-gate | Theme | Scope | Exit criteria |
|---|---|---|---|
| **D1 - Projection correctness** | Single-tenant, single-transaction correctness of the projection layer | `IEventCustomPropertyProjectionUpdater` + session equivalent + `EventCustomPropertyProjectionUpdater` + session impl + write-path integration into all Layer 3 value/definition command handlers + transactional unit tests + Testcontainers integration tests proving transactional consistency + rebuild command that is idempotent + `CustomPropertyProjectionStatus` tracking table + `CustomPropertyProjectionDirtyScope` table | Every runtime write atomically updates projection; rebuild produces byte-identical state to incremental; Testcontainers tests for transactional consistency pass; projection tables carry `tenant_id` + named `Tenant` filter; no LINQ against raw EAV values appears in any projection code path |
| **D2 - Projection operability** | Admin/operator control surface for projection health | Advisory-lock-coordinated rebuild worker + dirty-scope drain on rebuild completion + `GET /admin/custom-property-projections/status` + `POST rebuild` + `POST rebuild-single-event` + Prometheus metrics + structured logs via `error-tracking` skill + operator runbook covering "what is broken / stale / rebuildable / how do I recover" + tenant-scoped rebuild safety | Operator can observe projection status per tenant; operator can trigger rebuild safely during live writes; advisory-lock coordination prevents double-rebuild; dirty-scope drain guarantees no missed rows on rebuild completion; runbook published in `docs/OPERATIONS.md` |
| **D3 - Projection consumption** | Discovery/search/filter integration gated behind a tenant feature flag | `EventCustomPropertyProjectionFilter` + `EventProjectionSearchSpecification` + `EventProjectionFilterFacetSpecification` + integration into `EventQuerySpecification` + session equivalent in `EventSessionQuerySpecification` + tenant setting `custom_properties.projection_discovery_enabled` gating the new spec branches + query performance baseline on a seeded Testcontainers corpus + Blazor UI filter surface (Phase 9) | Discovery query paths composable onto projection without touching Layer 1 or Layer 2 filter composition; feature flag toggle cleanly switches tenants between projection-backed and direct-only discovery; query performance measured and reported; no regression on Layer 1/Layer 2 existing filters |

**Do not start D2 before D1 is proven green. Do not start D3 before D2 is proven green.** This sequencing is non-negotiable per Rule 17.

Scope (baseline, IN):

- `IEventCustomPropertyProjectionUpdater` + `IEventSessionCustomPropertyProjectionUpdater` interfaces in `Explore.Application/Contracts/Services`
- EF Core-backed projection updater implementations in `Explore.Persistence/Projections`
- Write-path integration: `SetEventCustomPropertyValueCommandHandler`, `SetEventCustomPropertyMultiValuesCommandHandler`, `DeleteEventCustomPropertyDefinitionCommandHandler`, and the template instantiation service call the projection updater **inside the same transaction** as the runtime write
- Session equivalents across `EventSessionCustomProperty*` handlers and `EventSessionTemplateInstantiationService`
- Specification pattern filters in `Explore.Application/Features/EventCustomPropertyProjections/` (e.g. `EventProjectionSearchSpecification`, `EventProjectionFilterFacetSpecification`)
- Projection-backed discovery query integration in `GetEventListRequestHandler` and `GetEventSessionListRequestHandler`, composed via `IQuerySpecification<T>` and gated behind a tenant setting so discovery only switches to projection reads when the baseline is proven
- Named query filters (`Tenant`, `SoftDelete`) on projection tables + PostgreSQL snake_case mapping
- Rebuild tooling:
  - `CustomPropertyProjectionStatus` tracking table (name, version, status, started_at, completed_at, tenant_id)
  - Background rebuild command + handler (`RebuildEventCustomPropertyProjectionCommand`, `RebuildEventSessionCustomPropertyProjectionCommand`)
  - PostgreSQL advisory lock coordination so inline projection writes can detect an in-progress rebuild and either wait or defer safely
- Projection admin/rebuild API endpoints + HATEOAS link policies + authorization category (`property_governance_admin`)
- Unit tests + Testcontainers-based integration tests (Npgsql + `Explore.Persistence.IntegrationTests`)

Scope (baseline, OUT - deferred to Milestone D advanced or later):

- Async outbox/CDC projection consumers (Wolverine/MassTransit) - escalate only if live baseline proves insufficient
- Moderation-specific projection consumers beyond the `IsModerationRelevant` flag (Milestone D advanced)
- Analytics-specific projection consumers beyond the `IsAnalyticsRelevant` flag (Milestone D advanced)
- Vector/embedding search - out of scope unless later justified
- Materialized view approach for projections - rejected per `goldlapel.com` "11 MV pitfalls" analysis; normalized projection tables + B-tree indexes is the baseline

Gate to exit Milestone D (baseline):

- All Layer 3 runtime writes transactionally update projections
- Rebuild tooling runs idempotently and replay-safe on a clean tenant
- Discovery reads can be switched onto projection-backed queries for any single tenant without schema changes
- Baseline projection tests pass in Testcontainers integration tests
- Layer 2 discovery paths remain unchanged (`IslamicAspectFilter`, `TechAspectFilter`, `AspectPresenceFilter` still compose inside `EventQuerySpecification` and are not routed through Layer 3 projection)
- Projection tables carry explicit `tenant_id` column + named `Tenant` query filter (defense in depth for multi-tenancy)
- No new LINQ query against raw `EventCustomPropertyValue` from a discovery code path

Research anchors:

- **Kurrent.io** "Live projections for read models with Event Sourcing and CQRS" - transactional live projection baseline, async only on justification
- **architecture-weekly.com** "Rebuilding event-driven read models" - status tracking + advisory lock rebuild coordination
- **Microsoft Learn - EF Core efficient querying** - projection via `Select`, `AsNoTracking`, pagination, avoid N+1
- **dotnet-efcore-guidelines skill** - named query filters, pooled DbContext, specification pattern, Npgsql resilience

#### Milestone E - Explicit Sync Workflows ⏳ PLANNED

Primary objective: deliver **operator-driven** template-to-runtime sync for Event and EventSession scopes using the Jira two-rule pattern. Rule B (copy on new instantiation) already ships with Milestones B/C. Milestone E implements **Rule A (propagate template changes to existing instances on operator confirmation)**.

Scope (IN):

- `ITemplateDiffService<TTemplate, TRuntime>` + concrete `EventTemplateDiffService` and `EventSessionTemplateDiffService` in `Explore.Application/Services` (pure diff logic, no EF)
- `ITemplateSyncService<TTemplate, TRuntime>` + concrete `EventTemplateSyncService` and `EventSessionTemplateSyncService` (orchestration, calls diff service, applies operator choices transactionally)
- Diff DTO family:
  - `TemplateDiffDto` with `AddedDefinitions`, `ModifiedDefinitions`, `RetiredDefinitions`, `AddedOptions`, `ModifiedOptions`, `RetiredOptions` collections
  - `DefinitionChangeDto` per field (old value, new value, field name, applies_to)
  - `TemplateSyncPlanDto` - operator-chosen subset of the diff
  - `TemplateSyncOutcomeDto` - applied changes + skipped + conflicts
- CQRS:
  - `GetEventTemplateDiffQuery(eventId, targetTemplateVersion)` → `TemplateDiffDto`
  - `GetEventSessionTemplateDiffQuery(eventSessionId, targetSessionTemplateVersion)` → `TemplateDiffDto`
  - `ApplyEventTemplateSyncCommand(eventId, syncPlan, baseProvenanceVersion)` → `BaseCommandResponse<TemplateSyncOutcomeDto>`
  - `ApplyEventSessionTemplateSyncCommand(eventSessionId, syncPlan, baseProvenanceVersion)` → `BaseCommandResponse<TemplateSyncOutcomeDto>`
- Stale-version conflict handling:
  - optimistic concurrency: the apply command rejects when `baseProvenanceVersion` doesn't match the runtime's current `SourceTemplateVersion`
  - conflict response distinguishes "template changed underneath" vs "runtime modified locally since last sync" so the operator can re-diff
- Source-id-first matching already implemented in Milestones B/C is reused; the diff service compares by `SourceTemplateDefinitionId` with `Namespace + Key` fallback
- Historical preservation rules from existing plan remain non-negotiable: retired definitions/options keep their historical values
- API endpoints + HATEOAS:
  - `GET /events/{eventId}/template-sync/diff?templateVersion={version}`
  - `POST /events/{eventId}/template-sync/apply`
  - `GET /event-sessions/{sessionId}/template-sync/diff?templateVersion={version}`
  - `POST /event-sessions/{sessionId}/template-sync/apply`
- Blazor admin UX (Phase 9.8/9.8A) with side-by-side diff view, operator selection, and confirmation
- Testcontainers-based integration tests proving diff correctness, sync atomicity, concurrent conflict detection, and historical preservation

Scope (OUT):

- Automatic sync on template save (explicitly forbidden by Lifecycle Rule 5)
- Cross-tenant sync propagation
- Retroactive sync to deleted/soft-deleted events

Gate to exit Milestone E:

- Diff service returns deterministic structured output
- Apply command is idempotent against a given `(runtimeId, syncPlanHash, baseProvenanceVersion)` triple
- Stale-version attempts return RFC 7807 problem detail with `409 Conflict`
- Historical values of retired definitions/options remain readable after sync
- Audit trail records operator identity, applied plan hash, source and target template versions, and timestamps
- Integration tests cover: add-only diff, modify diff, retire diff, mixed diff, conflict under concurrent sync, concurrent runtime edit

Research anchors:

- **Atlassian Jira custom field propagation (Apr 2025)** - Rule A (template → existing children) + Rule B (template → new children)
- **Sanity CMS - Versioning strategies for Enterprise content models (Sept 2025)** - distinguish schema evolution from content change; version explicitly; gate risky changes
- **ATProto lexicon evolution discipline** - inspired the add-only-plus-break-via-version philosophy we apply at the lexicon boundary; informs the sync story by reinforcing that existing runtime state must remain valid even when templates evolve

#### Milestone F - Aggregate Read Views And Publication Contracts ⏳ PLANNED

**Narrowed scope (CTO review 2026-04-11):** Milestone F delivers exactly **two outputs** and nothing else:

1. **One aggregate read model** (`EventWithSessionsView` keyless entity) for internal UI/API consumption
2. **One lexicon decision document** (`docs/LEXICONS.md` or equivalent) capturing NSID hierarchy, versioning, and add-only evolution discipline

Milestone F is explicitly **not** a publication platform. It does not implement ATProto record publication, PDS integration, bridgy-fed wiring, ActivityPub federation, or any other network-facing publication machinery. Those are separate initiatives that can read from the aggregate view when their own planning happens.

Primary objective: deliver an **event-with-sessions aggregate read model** that composes Layer 1 canonical fields + Layer 2 aspect fields + Layer 3 projection facets into a single query contract for UX and federation/publication surfaces **without collapsing canonical write models**.

Scope (IN):

- `EventWithSessionsView` keyless read entity in `Explore.Domain/Views` (or `Explore.Domain/ReadModels`)
- EF Core configuration using `[Keyless]` + `HasNoKey()` + `ToView("vw_event_with_sessions")` or `ToSqlQuery(...)`
- PostgreSQL backing view (or parameterized keyless query) composed from:
  - Event (Layer 1 core)
  - `EventIslamicAspect` + `EventTechAspect` (Layer 2, only when module enabled)
  - `EventCustomPropertyProjection` (Layer 3D, filtered by `IsExportable` or `IsFilterable`)
  - `EventSession` summary list with `EventSessionIslamicAspect` and `EventSessionCustomPropertyProjection` facets
- `EventWithSessionsViewDto` + `EventSessionSummaryDto` + `EventSessionCustomPropertyFacetDto` in `Explore.Application/DTOs/EventAggregateView`
- CQRS queries:
  - `GetEventWithSessionsAggregateViewQuery(eventId, exposureCeiling)` → `EventWithSessionsViewDto`
  - `GetEventListAggregateViewQuery(filter)` → `Paged<EventWithSessionsViewDto>`
- Exposure ceiling enforcement: the query filters projection facets by the caller's maximum allowed `ExposureLevel` before emitting DTOs
- Publication path reads from the aggregate view, applies public/exportable exposure filtering, and emits canonical + extension records per the lexicon contract
- Lexicon planning deliverables (documentation only - not code):
  - canonical `im.islamu.event.v1` NSID shape (Layer 1 + Layer 2 only)
  - experimental `im.islamu.event.temp.*` namespace for in-development schemas
  - `im.islamu.event.withSessions.v1` aggregate read view lexicon shape
  - `im.islamu.event.extension.v1` optional extension payload (from projection, never raw EAV)
  - version bump rules (new NSID for breaking changes)
- Documentation update to `docs/FEDERATION.md` + new `docs/LEXICONS.md` if helpful

Scope (OUT - explicitly rejected for this milestone):

- **Actual ATProto record publication / PDS integration** - separate initiative, not this plan
- **Bridgy-fed wiring / ActivityPub federation** - separate initiative
- **Publication pipeline / outbox machinery for federation** - separate initiative
- **Cross-tenant aggregate views** - out of scope (tenant isolation non-negotiable)
- **Materialized view with `pg_cron` refresh** - rejected per MV pitfalls; keyless entity + live join preferred
- **Generic "publication platform" abstractions** - Milestone F is deliberately two outputs (one view + one doc). Do not generalize.
- **`$extensions` ATProto field adoption** - rejected until ATProto Discussion #1889 standardizes
- **Lexicon code generation** - planning docs only, not runtime code

Gate to exit Milestone F:

- Keyless entity compiles, builds, migrates, and returns correct rows in Testcontainers integration tests
- Aggregate view respects Layer 1/Layer 2/Layer 3 boundaries (no Layer 2 field routed through Layer 3 projection; no raw EAV scan in the hot path)
- Exposure ceiling enforcement is unit-tested across `Internal`, `OrganizerOnly`, `TenantAdminOnly`, `Public`
- Lexicon planning docs are accepted
- Aggregate view contract is stable enough to be regenerated into an API client without breaking existing Event/EventSession endpoints

Research anchors:

- **Chris Woodruff - EF Core Keyless Entity Types (Feb 2025)** - keyless pattern + `ToView`/`ToSqlQuery`
- **goldlapel.com - 11 materialized view pitfalls (Mar 2026)** - rationale for rejecting materialized views here
- **ATProto Lexicon spec + style guide** - NSID versioning + add-only evolution + `.temp.` experimentation
- **bridgy-fed issue #2324 (Jan 2026)** - sidecar pattern for related records (event + session) traveling together in federation bridging

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

### Session Template Ownership Rule

In this implementation phase, `EventSessionTemplate` is an owned child of `EventTemplate`.

It is not a standalone reusable catalog item yet.

That lock is intentional because it keeps:

- lifecycle simpler
- authorization simpler
- instantiation simpler
- generic abstraction pressure lower

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
12. **EAV Promotion Criteria (Atlassian 4-question framework)**: a Layer 3 custom-property candidate must be **promoted out of EAV** if it answers **yes** to any of:
    - **Cross-tenant reporting**: is the attribute aggregated or reported across more than one tenant?
    - **Automation/AI dependencies**: do automated systems, LLM pipelines, recommendation engines, or moderation models rely on it?
    - **Search/filter required**: is it required in discovery filters or search facets for any public-facing surface?
    - **Long-term stability**: is the attribute expected to remain semantically stable for more than one product cycle?
    Promotion destinations in order of preference: (1) first-class field on `Event` or `EventSession` (Layer 1), (2) typed Layer 2 aspect/profile field, (3) governed projection column with explicit documentation. Layer 3 is not the right home for any field satisfying these criteria. This rule is enforced in Phase 5.10 (promotion commands) and in governance review.
13. **Live projection first**: Projection updates run inside the same transaction as the runtime write for the Milestone D baseline. Async/outbox escalation requires explicit justification and a separate plan amendment.
14. **Add-only lexicon evolution**: Once a canonical lexicon NSID is published, its existing field constraints are immutable. New optional fields may be added. Breaking changes require a new NSID version (`v2`, `v3`). Experimental in-development schemas use `.temp.` in the NSID hierarchy.
15. **Locked concurrency strategy** (CTO review 2026-04-11): Two separate, non-overlapping concurrency concerns across all mutable aggregates touched by this plan:
    - **Technical persistence conflict detection**: EF Core `IsConcurrencyToken` on a `ConcurrencyStamp` (`Guid`) column on every mutable aggregate (`EventTemplate`, `EventTemplateCustomPropertyDefinition`, `EventCustomPropertyDefinition`, `EventCustomPropertyValue`, `EventSessionTemplate`, `EventSessionTemplateCustomPropertyDefinition`, `EventSessionCustomPropertyDefinition`, `EventSessionCustomPropertyValue`, `CustomPropertyDefinition`, `CustomPropertyValue`). On conflict: EF raises `DbUpdateConcurrencyException`, which the handler translates to an RFC 7807 `409 Conflict` response. This is the **only** allowed persistence conflict mechanism.
    - **Business-level sync provenance**: `SourceTemplateVersion` on runtime definitions + `Version` on templates. These are used by `ITemplateSyncService` to determine whether the operator's `baseProvenanceVersion` argument still reflects the current runtime state. They are **not** a persistence conflict mechanism and are **not** the same thing as `ConcurrencyStamp`.
    No flow is allowed to invent a third concurrency mechanism (timestamps, audit comparisons, etag headers alone). Mixing business version and technical token in one "conflict check" is forbidden because it produces support pain.
16. **Hard limits and quotas** (enterprise self-hostable guardrails): All Layer 3 surfaces carry explicit configuration-driven ceilings. Default values are tenant-configurable via the setting registry up to a platform-defined maximum. See `Hard Limits And Quotas` section for the concrete numbers. Handlers enforce the ceiling before writing; projection rebuilds honor a batch-size ceiling; sync apply payloads honor a change-count ceiling.
17. **Ruthless milestone sequencing** (CTO review 2026-04-11): The remaining milestones execute in this order with no parallelization between them:
    1. D1 (projection correctness)
    2. D2 (projection operability)
    3. D3 (projection consumption)
    4. E (explicit sync workflows)
    5. F (aggregate read view + lexicon docs)
    No sub-gate may begin until the previous one exits its gate with all tests green in Testcontainers integration tests. Specifically: D3 does not start before D2. D2 does not start before D1. E does not start before D3. F does not start before E. Cross-milestone coupling is blocked at plan review.

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

Lexicons follow the same separation as the persistence model and apply **ATProto evolution discipline** (atproto.com/specs/lexicon + atproto.com/guides/lexicon-style-guide, 2026):

- canonical core lexicons for `Event` and `EventSession`
- typed Layer 2 lexicons for event and session profiles
- Layer 3 extension lexicons for event and session extension payloads (sourced from projection, never from raw EAV)
- aggregate read/view lexicons that embed session summaries inside an event-oriented view

Recommended NSID structure under the `im.islamu` reverse-DNS prefix:

- `im.islamu.event.core.v1` - Layer 1 canonical event record
- `im.islamu.eventsession.core.v1` - Layer 1 canonical session record
- `im.islamu.event.islamic.v1` - Layer 2 Islamic profile (sector-standard)
- `im.islamu.event.tech.v1` - Layer 2 Tech profile (sector-standard)
- `im.islamu.eventsession.islamic.v1` - Layer 2 session Islamic profile
- `im.islamu.event.extension.v1` - Layer 3 extension payload (projection-sourced, exposure-filtered)
- `im.islamu.eventsession.extension.v1` - Layer 3 session extension payload
- `im.islamu.event.withSessions.v1` - aggregate read/view contract
- `im.islamu.event.temp.*` - experimental namespace for in-development schemas

### Lexicon Evolution Rules (locked)

Once a canonical lexicon NSID is published:

1. **No existing constraint may be tightened or removed**: field type changes, required→optional transitions (adding required), removing a field, narrowing an enum, or tightening a format validator are all forbidden.
2. **New optional fields may be added**: this is the only allowed shape change.
3. **Breaking changes require a new NSID version**: `...v1` → `...v2` with a clean new record type, not an overloaded existing one.
4. **Experimental schemas use `.temp.` in the NSID hierarchy**: e.g. `im.islamu.event.temp.withHijriDate` for live network experimentation before committing to a stable version.
5. **Canonical lexicons remain separate records** from aggregate view contracts. The merged `im.islamu.event.withSessions.v1` lexicon is a read/view contract, not the canonical write contract. Federation / PDS writes use the canonical records; app-view aggregation reads the merged contract.
6. **Layer 3 is never surfaced into a canonical lexicon directly**. Custom-property values reach the lexicon surface only via the `...extension.v1` lexicon, which is itself sourced from the exposure-filtered projection layer - never from the raw EAV runtime rows. A definition must be promoted to Layer 2 or a first-class field before it can appear in a canonical core lexicon.
7. **The `$extensions` open-extension pattern (ATProto Discussion #1889) is NOT adopted**. It is still a community proposal as of 2025. If/when it standardizes, the plan may be amended to expose Layer 3 facets via `$extensions` blocks on canonical records. Until then, the `...extension.v1` separate-record strategy is the committed path.

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

This plan adopts the **Jira two-rule template propagation pattern** (Atlassian, 2025) for template lifecycle:

- **Rule B - Copy on new instantiation**: when a new event or session is created from a template, the current template contents are copied into event-local / session-local rows in the same transaction. This rule is **implemented** in Milestones B and C.
- **Rule A - Propagate on operator confirmation**: when a template is modified, **no automatic propagation happens**. An authorized operator reviews a diff, selects which changes to apply to each existing event/session, and confirms the apply. This rule is scheduled for **Milestone E**.

### Creation Flow (Rule B - Implemented)

1. Admin creates `EventTemplate` with explicit `TemplateKey` and `Version`.
2. Admin defines versioned template custom properties and options.
3. Organizer creates an event with an optional template selection (`CreateEventDto.TemplateId`).
4. In one transaction, `CreateEventCommandHandler` calls `IEventTemplateInstantiationService.InstantiateFromTemplateAsync(template, event)`:
   - materializes event-local definitions with full provenance (`SourceTemplateId`, `SourceTemplateKey`, `SourceTemplateVersion`, `SourceTemplateDefinitionId`, `InstantiatedAt`)
   - materializes event-local options (with `SourceTemplateOptionId`)
   - applies default values as initial `EventCustomPropertyValue` rows when `DefaultOptionId` or `Default*Value` is set
   - stamps `LastSyncedFromTemplateAt` to match `InstantiatedAt`
5. Event runtime behavior thereafter uses only event-local rows.
6. Event sessions follow the same contract via `CreateEventSessionDto.SessionTemplateId` + `IEventSessionTemplateInstantiationService` + the session variant of the in-memory instantiation service.

### Edit Flow

- editing a template creates/updates a specific versioned template state (no back-propagation)
- editing an event changes only event-local state
- editing a session changes only session-local state

### Sync Flow (Rule A - Milestone E)

Sync is explicit, version-aware, auditable, and operator-driven. No automatic sync on template save. Sync operates in two phases: **Diff** and **Apply**.

#### Phase 1 - Diff

1. Operator requests `GET /events/{eventId}/template-sync/diff?templateVersion={version}`.
2. `GetEventTemplateDiffQueryHandler` resolves:
   - the event's current provenance (`SourceTemplateId`, `SourceTemplateVersion`)
   - the target template version (defaulting to the latest published version for the same `TemplateKey`)
3. `EventTemplateDiffService.BuildDiff(runtimeDefs, templateDefs)` performs **source-id-first matching with normalized `Namespace + Key` fallback**:
   - **Added definitions**: in template, not in runtime (by source identity or key)
   - **Retired definitions**: in runtime with a non-null `SourceTemplateDefinitionId` that no longer exists in the target template
   - **Modified definitions**: matched by source identity with at least one of: `DisplayName`, `Description`, `IsRequired`, `IsMulti`, `ExposureLevel`, any typed validation field (`MinLength`/`MaxLength`/`RegexPattern`/`MinNumber`/`MaxNumber`/`MinDateTime`/`MaxDateTime`/`AllowedUrlSchemes`), any exposure flag (`IsSearchable`/`IsFilterable`/`IsExportable`/`IsModerationRelevant`/`IsAnalyticsRelevant`), or any `Default*Value` differing
   - **Added options**: in template definition, not in runtime definition
   - **Retired options**: in runtime definition, no longer in template definition
   - **Modified options**: matched by source identity with differing `DisplayName`/`Description`/`Value`/`IsDefault`/`IsActive`/`SortOrder`/`ParentOptionId`
4. The diff returns a `TemplateDiffDto` with counts per bucket + per-definition change details. Runtime definitions that were created ad-hoc (no `SourceTemplateDefinitionId`) are returned as an `UntouchedLocalDefinitions` summary and are **not** part of the diff surface; the operator cannot sync them.

#### Phase 2 - Apply

1. Operator selects a subset of changes from the diff and POSTs a `TemplateSyncPlanDto` to `/events/{eventId}/template-sync/apply` with `baseProvenanceVersion` matching the diff's source version.
2. `ApplyEventTemplateSyncCommandHandler` opens a transaction and:
   - re-validates `baseProvenanceVersion == runtime.CurrentSourceTemplateVersion` (409 Conflict if mismatched)
   - re-validates `targetTemplateVersion` is still the latest published (409 Conflict otherwise, operator must re-diff)
   - applies added definitions as new event-local rows with full provenance stamping
   - applies modified definitions field-by-field, only for fields the operator chose in the plan
   - applies retirement as `IsActive = false` on the runtime definition (never hard delete)
   - applies added/modified/retired options via the same pattern on the definition's option collection
   - stamps `LastSyncedFromTemplateAt = DateTimeOffset.UtcNow` and `SourceTemplateVersion = targetTemplateVersion`
   - **transactionally updates projections** for every touched definition (Milestone D dependency)
   - emits an audit record with operator identity, applied plan hash, source version, target version, counts
3. Returns `TemplateSyncOutcomeDto` with applied, skipped, conflict sections.

#### Three-Way Merge Rules

When the runtime has been edited locally since the last sync (`LastSyncedFromTemplateAt != null` and local edits exist post-sync), the diff service flags affected definitions with a `HasLocalChanges` marker. The operator still sees the template change but the UI must warn them that applying will overwrite local changes. There is no automatic merge across the three sources (template / last-sync / current runtime) - the operator is the authoritative merge decision.

#### Stale-Version Conflict Handling

- Concurrent sync attempts: first apply wins via `baseProvenanceVersion` check
- Template updated underneath the operator: diff returns 409 Conflict with the new version surfaced in the problem detail
- Runtime modified locally during the operator's review: detected via audit timestamps, surfaced in apply response

### Provenance Rules

Support must be able to answer:

- which template created this event or session?
- which version?
- when was it instantiated?
- was it synced later? how many times?
- which fields were touched by which operator?

Provenance columns on runtime definitions:

- `SourceTemplateId` (Guid?) - template aggregate id
- `SourceTemplateKey` (string?) - stable template key
- `SourceTemplateVersion` (int?) - version at last sync or instantiation
- `SourceTemplateDefinitionId` (Guid?) - exact template definition id for source-id matching
- `InstantiatedAt` (DateTimeOffset) - set once on creation, never mutated
- `LastSyncedFromTemplateAt` (DateTimeOffset?) - updated on each successful sync

Sync matching must prefer stored source identifiers first and fall back to `Namespace + Key` only for repair or backfill scenarios.

### Retirement And Historical Preservation (Cross-Reference)

Sync must never:

- hard-delete definitions or options with historical values (see Delete And Retirement section)
- rewrite provenance on existing historical values
- remove historical value rows when a definition or option is retired

Sync may:

- set `IsActive = false` on retired definitions and options
- add new definitions and options
- change display labels, descriptions, exposure flags, validation rules, and sort order on existing definitions
- update option labels and values on existing options

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

- event form reads -> event-local definitions + event-local values (direct entity queries, `AsNoTracking` where applicable)
- session form reads -> session-local definitions + session-local values (direct entity queries)
- organization/group runtime reads -> shared definitions + scoped values
- discovery/filtering/search -> **projection-backed** via `IQuerySpecification<EventCustomPropertyProjection>` composed into discovery queries; not ad hoc EAV-heavy query composition
- Layer 1 and Layer 2 discovery paths remain **unchanged**. `EventQuerySpecification` keeps composing `EventFilter` + `IslamicAspectFilter` + `TechAspectFilter` + `AspectPresenceFilter` + `EventSubqueryFilter` exactly as it does today. Layer 3 projection filters are added as **additional** spec branches gated behind a tenant setting (`custom_properties.projection_discovery_enabled`) for progressive rollout.

### Projection Rules

1. Searchable or filterable custom properties are projected **at write/sync time, inside the same transaction** as the runtime write (Rule 13 - Live projection first).
2. Public/exportable projections only include properties whose exposure rules allow it (`ExposureLevel` >= caller's allowed ceiling, and the matching `IsExportable` flag).
3. Moderation-relevant properties are projected into moderation-aware read models (flag-gated via `IsModerationRelevant`).
4. Projection rebuild tooling is part of the implementation plan and is **required** before Milestone D can exit its gate.
5. Projection rows are **atomic per projected value row**, not one merged row per property. Each `EventCustomPropertyValue` maps to zero or one `EventCustomPropertyProjection` row (single-value) or to one projection row per value row (multi-value, Ordinal-aligned).
6. Raw event-local definitions and values remain the source of truth; projections are rebuildable read models only. A projection rebuild from source of truth must yield byte-identical projection state modulo `UpdatedAt`.
7. Projection tables carry explicit `tenant_id` column + named `Tenant` query filter (defense in depth for multi-tenancy).
8. Projection tables carry a `SoftDelete` named query filter that cascades from the source runtime value row.
9. Projection DTOs, not projection entities, leave the Application layer (Clean Architecture rule).

### Projection Consistency Baseline (locked)

The baseline consistency model, validated against Kurrent.io "Live projections for CQRS" (2025):

1. **Runtime + projection in one transaction**: `SetEventCustomPropertyValueCommandHandler`, `SetEventCustomPropertyMultiValuesCommandHandler`, `DeleteEventCustomPropertyDefinitionCommandHandler`, and the instantiation service each open a single transaction, write runtime rows, call `IEventCustomPropertyProjectionUpdater.UpdateFor(entityId, definitionId, cancellationToken)`, and commit atomically.
2. **Rebuild tooling exists for recovery and reindexing**: `RebuildEventCustomPropertyProjectionCommand(eventId)` and `RebuildAllEventCustomPropertyProjectionsCommand(tenantId, batchSize)` iterate the source-of-truth rows in stable order, delete the stale projection rows for the target scope inside the same transaction, and write the fresh projection rows. Rebuild commands are idempotent and replay-safe.
3. **Advanced eventual-consistency / outbox / async consumer flows are deferred** until the baseline proves insufficient. Escalation to async requires a separate plan amendment with measured evidence.

This keeps the first implementation operationally simple, debuggable, and strongly consistent for read-after-write scenarios (the most common UX expectation).

### Projection Rebuild Coordination (Milestone D)

Rebuilds may run concurrently with inline projection writes. To avoid races the plan adopts a tracking-table + advisory-lock pattern (architecture-weekly.com "Rebuilding event-driven read models"):

```sql
CREATE TABLE custom_property_projection_status (
  projection_name text NOT NULL,
  projection_version int NOT NULL DEFAULT 1,
  tenant_id uuid NOT NULL,
  status text NOT NULL, -- 'active', 'rebuilding', 'draining'
  started_at timestamptz NULL,
  completed_at timestamptz NULL,
  operator_id uuid NULL,
  last_error text NULL,
  PRIMARY KEY (projection_name, projection_version, tenant_id)
);
```

Rebuild worker:

1. Acquires PostgreSQL advisory lock keyed on `(projection_name, tenant_id)` via `pg_try_advisory_xact_lock(...)`
2. Inserts/updates status row to `rebuilding`
3. Rebuilds in batches, committing each batch
4. Marks status `active` on completion or records `last_error` on failure
5. Releases the advisory lock automatically at transaction end

Inline projection writers:

1. Attempt `pg_try_advisory_xact_lock(...)` with the same key (shared mode or trylock)
2. If lock unavailable and status is `rebuilding`, the inline projection update **is skipped for that transaction** - the runtime write still commits
3. **On skip, the inline writer upserts a row into `custom_property_projection_dirty_scope`** (see Dirty-Scope Recovery Mechanism below) so the rebuild worker can catch up on completion
4. This trades temporary projection staleness for write-path availability during rebuild

### Dirty-Scope Recovery Mechanism (Milestone D, CTO review 2026-04-11)

The original "skip inline projection, trust rebuild to catch up" approach has an edge case: rows written after the rebuild's scan window but before rebuild completion may be missed until a later edit triggers another projection write, which may be a long time or may never happen. For enterprise correctness this is unacceptable. The plan therefore adds an explicit dirty-scope backlog table that is drained on rebuild completion.

**Backlog table schema:**

```sql
CREATE TABLE custom_property_projection_dirty_scope (
  id bigserial PRIMARY KEY,
  projection_name text NOT NULL,     -- 'event_custom_property_projection' | 'event_session_custom_property_projection'
  projection_version int NOT NULL DEFAULT 1,
  tenant_id uuid NOT NULL,
  scope_type text NOT NULL,          -- 'event' | 'event_session'
  scope_id uuid NOT NULL,            -- Event.Id or EventSession.Id
  definition_id uuid NULL,           -- optional: the specific definition that triggered the skip (for narrower refresh)
  reason text NOT NULL,              -- 'rebuild_in_progress' | 'advisory_lock_contention' | 'other'
  created_at timestamptz NOT NULL DEFAULT now(),
  drained_at timestamptz NULL,
  UNIQUE (projection_name, projection_version, tenant_id, scope_type, scope_id, definition_id)
);

CREATE INDEX ix_dirty_scope_pending ON custom_property_projection_dirty_scope (projection_name, projection_version, tenant_id) WHERE drained_at IS NULL;
```

**Inline writer behavior on skip (pseudocode):**

```csharp
// inside IEventCustomPropertyProjectionUpdater.UpdateForValueAsync
if (!await _projectionLock.TryAcquireAsync(projectionName: "event_custom_property_projection", tenantId, ct))
{
    // advisory lock held by rebuild worker; record dirty scope and skip
    await _dirtyScopeRepository.UpsertAsync(
        projectionName: "event_custom_property_projection",
        projectionVersion: 1,
        tenantId: tenantId,
        scopeType: "event",
        scopeId: eventId,
        definitionId: definitionId,
        reason: "rebuild_in_progress",
        ct);
    // runtime write still commits through the enclosing handler transaction
    return;
}
```

The upsert happens inside the same transaction as the runtime write, so dirty-scope registration is atomic with the runtime write and does not create lost-write edge cases.

**Rebuild worker drain-on-completion (pseudocode):**

```csharp
// inside IEventCustomPropertyProjectionUpdater.RebuildForTenantAsync
// Step 1: acquire advisory lock, set status to 'rebuilding'
// Step 2: iterate source of truth in stable order, rebuild projection in batches
// Step 3: before releasing lock, drain dirty scopes
var pending = await _dirtyScopeRepository.GetPendingAsync(
    projectionName: "event_custom_property_projection",
    projectionVersion: 1,
    tenantId,
    ct);
foreach (var scope in pending)
{
    // targeted refresh of the event's projection rows from source of truth
    await RefreshForEventInternalAsync(scope.ScopeId, ct);
    await _dirtyScopeRepository.MarkDrainedAsync(scope.Id, ct);
}
// Step 4: set status to 'active', release lock
```

**Observability contract:**

- `GET /admin/custom-property-projections/dirty-scopes?tenantId={tenantId}` returns the current pending dirty-scope rows for operator inspection (paged, `property_governance_admin` authorized)
- Prometheus gauge `eav_projection_dirty_scope_pending_total{projection_name,tenant_id}` exposed
- Prometheus counter `eav_projection_dirty_scope_drained_total{projection_name,tenant_id}` exposed
- Structured log on every upsert and every drain via the `error-tracking` skill
- Alarm threshold: `pending > 0` lasting more than one rebuild cycle is a deployment-specific alert condition

**Operator self-service recovery:**

- If rebuild is interrupted (crash/restart) before drain completes, a later rebuild will naturally drain remaining scopes because the dirty-scope rows remain in the table
- `POST /admin/custom-property-projections/drain-dirty-scopes` can force a drain without a full rebuild if operator confirms projection is otherwise consistent

**Acceptance criteria (D1 exit gate):**

- Integration test proves: rebuild in progress → concurrent inline write → dirty scope registered → rebuild completion → dirty scope drained → projection reflects the concurrent write
- Integration test proves: rebuild crash mid-run → manual rebuild restart → remaining dirty scopes are drained
- Integration test proves: dirty-scope row is atomic with the runtime write transaction (rollback rolls back both)
- Dirty-scope table is soft-delete-aware: draining marks `drained_at` and does not hard delete (for audit/forensics)

### Advanced Projection Phase

After baseline projection integration is stable, later milestones can add:

- async projection consumers
- moderation-specific projection consumers with richer payloads
- analytics-specific projection consumers feeding an analytics pipeline
- outbox/CDC mechanics (e.g. Wolverine, MassTransit outbox, or Debezium) if justified by scale

No advanced projection phase is committed to in this plan. Each addition requires a plan amendment.

### Concurrency And Versioning Rules (LOCKED, CTO review 2026-04-11)

Per Rule 15, the plan distinguishes two different concerns and forbids mixing them:

**Concern 1 - Technical persistence conflict detection (EF Core concurrency token):**

Every mutable aggregate listed below carries a `ConcurrencyStamp` (`Guid`) column configured as `IsConcurrencyToken()` with `ValueGeneratedOnAddOrUpdate()`:

| Aggregate | Purpose |
|---|---|
| `CustomPropertyDefinition` | shared Org/Group definition |
| `CustomPropertyOption` | shared option |
| `CustomPropertyValue` | shared value |
| `EventTemplate` | event template root |
| `EventTemplateCustomPropertyDefinition` | template definition |
| `EventTemplateCustomPropertyOption` | template option |
| `EventCustomPropertyDefinition` | event-local runtime definition |
| `EventCustomPropertyOption` | event-local runtime option |
| `EventCustomPropertyValue` | event-local runtime value |
| `EventSessionTemplate` | session template root |
| `EventSessionTemplateCustomPropertyDefinition` | session template definition |
| `EventSessionTemplateCustomPropertyOption` | session template option |
| `EventSessionCustomPropertyDefinition` | session-local runtime definition |
| `EventSessionCustomPropertyOption` | session-local runtime option |
| `EventSessionCustomPropertyValue` | session-local runtime value |

Handlers call `SaveChangesAsync` inside transactions and translate `DbUpdateConcurrencyException` to an RFC 7807 `409 Conflict` with a structured problem detail (`type` = `concurrent_update`, `detail` includes aggregate id + last-known stamp).

**Concern 2 - Business-level sync provenance (not persistence tokens):**

`EventCustomPropertyDefinition.SourceTemplateVersion` + `EventSessionCustomPropertyDefinition.SourceTemplateVersion` record which template version was used to instantiate or last sync the runtime row. `EventTemplate.Version` + `EventSessionTemplate.Version` record the current template version. The `ITemplateSyncService` uses these to answer three distinct questions:

1. **Is the operator's `baseProvenanceVersion` still current?** (sync apply rejects if runtime's stored `SourceTemplateVersion` no longer matches the operator's claim) → returns business-level `409 stale_sync_base` problem detail, distinct from technical `409 concurrent_update`
2. **Which template version are we applying changes from?** (recorded in the audit trail)
3. **Has the runtime been modified locally since last sync?** (three-way merge `HasLocalChanges` flag on diff DTO, not a concurrency check)

**Explicit forbidden patterns:**

- Timestamps (`UpdatedAt`) used as a concurrency mechanism → **forbidden** (not monotonic under clock skew)
- ETag headers as the only conflict mechanism → **forbidden** (must be backed by `ConcurrencyStamp` on the server)
- Mixing `ConcurrencyStamp` and `SourceTemplateVersion` into one "is this stale" check → **forbidden**
- Comparing audit fields (`UpdatedAt`, `UpdatedBy`) to detect concurrency → **forbidden**
- Inventing per-feature concurrency tokens → **forbidden**

**Implementation requirements:**

1. All mutable aggregates above must have `ConcurrencyStamp` by Milestone D exit (additive migration; no existing data needs to be rewritten because EF Core initializes on first save)
2. Every command handler must have a unit test covering the `DbUpdateConcurrencyException` branch
3. Every sync flow must have a unit test covering the `stale_sync_base` branch distinct from the technical concurrency branch
4. Projection rebuilds are immune to both concerns because they scan source of truth in stable order and overwrite projection rows; they do not check concurrency tokens on source rows
5. Advisory-lock-coordinated rebuild (see Projection Rebuild Coordination) is a **third** concern (coordination between rebuild worker and inline writers) and has nothing to do with concurrency tokens

### Hard Limits And Quotas (CTO review 2026-04-11)

Enterprise self-hostable software needs guardrails because admins can misconfigure themselves into pain (thousand-option picklists, 10k-property tenants, half-million-row multi-value fields). The plan locks the following platform-default ceilings, each configurable per-tenant via the setting registry up to a platform-maximum that prevents runaway tenant configurations.

| Setting key | Platform default | Platform maximum | Enforced in | Rationale |
|---|---|---|---|---|
| `custom_properties.max_definitions_per_tenant_per_entity_scope` | 500 | 5000 | Handler (shared def create) | Jira's 700-field-per-space incident (Mar 2026) |
| `custom_properties.max_definitions_per_event` | 100 | 1000 | `CreateEventCustomPropertyDefinitionCommandHandler`, template instantiation | Prevent runtime event blow-up |
| `custom_properties.max_definitions_per_event_session` | 50 | 500 | Session handler, session template instantiation | Sessions are leaner than events |
| `custom_properties.max_options_per_definition` | 200 | 2000 | Option create + bulk edit handlers | Protect MudSelect rendering + seed payload size |
| `custom_properties.max_multi_value_rows_per_value` | 20 | 200 | `SetEventCustomPropertyMultiValuesCommandHandler` + session equivalent | Prevent abuse of multi-value as a list storage |
| `custom_properties.max_definitions_per_template` | 100 | 1000 | Template create/update handlers | Mirror event ceiling |
| `custom_properties.projection_rebuild_batch_size` | 500 | 5000 | `RebuildForTenantAsync` | Avoid long PostgreSQL transactions |
| `custom_properties.sync_apply_max_change_count` | 200 | 2000 | `ApplyEventTemplateSyncCommandHandler` | Reject giant diff-apply payloads |
| `custom_properties.sync_apply_max_payload_bytes` | 262144 | 4194304 | API request model binding | 256 KB default, 4 MB hard ceiling |
| `custom_properties.max_dirty_scope_pending_per_tenant` | 10000 | 100000 | Dirty-scope upsert check | If exceeded, reject new inline writes until drain completes and alarm fires |

**Enforcement pattern:**

```csharp
// inside handler (pseudocode)
var quota = await _settingService.GetEffectiveIntAsync(
    SettingKeys.CustomPropertiesMaxDefinitionsPerEvent,
    scope: SettingScope.Tenant,
    tenantId: tenantId,
    ct);

var existingCount = await _eventCustomPropertyRepository.CountDefinitionsForEvent(eventId, ct);
if (existingCount >= quota)
{
    return BaseCommandResponse<Guid>.Failure(
        "quota_exceeded",
        $"Event custom property definition count {existingCount} would exceed quota {quota}");
}
```

**Setting definitions task:** Phase 1.1C (new, see below) adds a `CustomPropertyQuotaSettingDefinitions.cs` file in `Explore.Domain/Settings/Definitions` with the above keys and their types. Validation rejects tenant attempts to exceed the platform maximum.

**Tests:** every quota must have a unit test that seeds to `quota-1`, verifies the next write succeeds, and verifies the `quota+1` write is rejected with a structured error code.

### Operational Governance Surface (Rule 12 implementation, CTO review 2026-04-11)

Rule 12 (EAV Promotion Criteria / Atlassian 4-question framework) is only enforceable if there is a concrete admin inspection surface for it. "The rule exists" is not enough; the rule must be **reviewable**. The plan adds the following surface as part of Milestone D2 (operability).

**Admin query + DTO (Phase 5.8, new):**

```csharp
public sealed record CustomPropertyGovernanceRowDto(
    Guid TenantId,
    string Namespace,
    string Key,
    string DisplayName,
    string EntityScope,              // 'Organization' | 'Group' | 'Event' | 'EventSession' | 'EventTemplate' | 'EventSessionTemplate'
    string PropertyType,
    ExposureLevel ExposureLevel,
    bool IsSearchable,
    bool IsFilterable,
    bool IsExportable,
    bool IsModerationRelevant,
    bool IsAnalyticsRelevant,
    bool IsSystemOwned,
    int ActiveInstanceCount,          // how many runtime rows use this definition across the tenant
    DateTime LastUsedAt,
    PromotionRecommendation Recommendation);

public enum PromotionRecommendation
{
    None,                              // none of the 4 questions say yes
    ConsiderProjectionFirst,           // search/filter criterion triggered
    ConsiderLayer2Promotion,           // automation-AI or cross-tenant reporting criterion triggered
    ConsiderLayer1Promotion             // long-term stability + automation-AI + cross-tenant all triggered
}
```

**Query:** `GetCustomPropertyGovernanceReportQuery(Guid TenantId, GovernanceReportFilter filter) : IRequest<BaseCommandResponse<Paged<CustomPropertyGovernanceRowDto>>>`

The recommendation is computed from:

- **ConsiderProjectionFirst** if `IsSearchable = true` OR `IsFilterable = true` but definition is still Layer 3
- **ConsiderLayer2Promotion** if `IsModerationRelevant = true` OR `IsAnalyticsRelevant = true` (indicates cross-tenant reporting / automation dependencies)
- **ConsiderLayer1Promotion** if a definition is marked `IsModerationRelevant = true` AND `IsSearchable = true` AND used by more than N% of the tenant's events (platform-configurable, default 30%)

**Admin endpoint:** `GET /admin/custom-property-definitions/governance-report?tenantId={tenantId}&scope={EntityScope}&recommendation={PromotionRecommendation}` → paged response with HATEOAS links, `property_governance_admin` authorized, RFC 7807 errors, `Lookup` request timeout, `authenticated` rate limit.

**Blazor admin page (Phase 9.7 expansion):** "Layer 3 Governance Report" page per tenant listing all active Layer 3 definitions with their flags + instance counts + promotion recommendations. Operators can click a recommendation to launch the relevant promotion workflow (Phase 5.9 projection-first promotion or Phase 5.10 Layer 2 promotion playbook).

**Acceptance criteria (D2 exit gate contribution):**

- Query returns deterministic recommendations for seeded fixtures
- Admin endpoint respects tenant isolation (a tenant admin cannot see another tenant's definitions)
- Blazor page renders within 2s for a tenant with up to 500 definitions
- `PromotionRecommendation` logic is unit-tested against the Atlassian 4-question matrix

### PostgreSQL-Specific Projection Tuning

- Normalized projection tables use B-tree indexes on `(tenant_id, namespace, key)`, `(tenant_id, is_searchable)`, `(tenant_id, is_filterable)`, `(tenant_id, exposure_level)` for discovery queries.
- Partial indexes on `exposure_level = 'Public'` reduce size by 20-50% for public-facing discovery queries.
- No JSONB / GIN indexes for Layer 3 runtime or projection tables - our normalized shape is deliberately strict for governance, typed validation, and audit. (See Critical Analysis in session notes: the Sept 2025 JSONB-100x-faster article does not apply to our model because it assumes a greenfield single-column decision; we already pay the normalized cost for type safety + multi-value + audit.)
- Snake case column naming via Npgsql naming conventions (already configured in `Explore.Persistence`).
- Split query behavior already enabled in Npgsql configuration; relevant for aggregate view reads that join multiple projection tables.

### Aggregate View Read Strategy (Milestone F)

The event-with-sessions aggregate view is **not** a materialized view. The plan rejects materialized views here per the "11 MV pitfalls" analysis (goldlapel.com, Mar 2026): cascade staleness, disk bloat, `REFRESH CONCURRENTLY` unique-index requirement, no incremental refresh without `pg_ivm`, no last-refresh timestamp. Instead:

- `EventWithSessionsView` is an **EF Core keyless entity** (`[Keyless]` + `HasNoKey()` + `ToView("vw_event_with_sessions")` or `ToSqlQuery(...)`)
- Backing: a PostgreSQL read-only view or a parameterized SQL query composing Event + EventIslamicAspect (if module enabled) + EventTechAspect (if module enabled) + EventCustomPropertyProjection + a session summary subquery
- Read path: `IQuerySpecification<EventWithSessionsView>` + `AsNoTracking()` + pagination + projection DTO mapping in query handler
- No insert/update/delete support (keyless entities are strictly read-only in EF Core 10, which matches our requirement)
- If the view shape ever needs EF 10 complex type / JSON column mapping for session summary arrays, `ComplexProperty(...).ToJson()` is available (with the known EF 10.0 `Contains()` bug noted for EF 10.0.1+)

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

The phases below remain the architecture/work breakdown map.

Actual delivery is milestone-driven. Milestones control implementation order and test gates; phases describe the full surface area of work.

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

#### Task 3.1: Create Shared Definition Repositories ✅ (Milestone A)

#### Task 3.2: Create Event Template Repositories ✅ (Milestone B)

#### Task 3.2A: Create EventSession Template Repositories ✅ (Milestone C)

#### Task 3.3: Create Event Runtime Repositories ✅ (Milestone B)

#### Task 3.3A: Create EventSession Runtime Repositories ✅ (Milestone C)

#### Task 3.4: Create Template Instantiation Service ✅ (Milestone B)

#### Task 3.4A: Create Session Template Instantiation Service ✅ (Milestone C)

#### Task 3.5: Create Event Template Diff / Sync Service (**Milestone E**)

> **Keep the implementation boring** (CTO review 2026-04-11): Do NOT build a generic `ITemplateDiffService<TTemplate, TRuntime>` or a "schema merge engine" or a reflection-driven field comparer. Write explicit, hand-coded field comparisons. Write explicit DTOs. A little duplication between event and session flavors is healthier than a clever abstraction. The goal is "operationally explainable" not "theoretically elegant."

- **Placement:**
  - `IEventTemplateDiffService` and `IEventTemplateSyncService` interfaces → `Explore.Application/Contracts/Services/`
  - `EventTemplateDiffService` (pure diff logic, no EF) → `Explore.Application/Services/`
  - `EventTemplateSyncService` (orchestration, calls diff, opens transaction, calls projection updater) → `Explore.Application/Services/`
- **Diff service contract (explicit, not generic):**
  - `EventTemplateDiffResult BuildDiff(IReadOnlyList<EventCustomPropertyDefinition> runtimeDefs, IReadOnlyList<EventTemplateCustomPropertyDefinition> templateDefs, int targetTemplateVersion)`
  - source-id-first matching with `Namespace + Key` fallback
  - returns `AddedDefinitions`, `ModifiedDefinitions`, `RetiredDefinitions`, `AddedOptions`, `ModifiedOptions`, `RetiredOptions`, `UntouchedLocalDefinitions`
  - Each `ModifiedEventDefinitionChange` carries **hand-coded per-field old/new value pairs** for the fields the diff actually compares (DisplayName, Description, IsRequired, IsMulti, IsActive, SortOrder, ExposureLevel, IsSearchable, IsFilterable, IsExportable, IsModerationRelevant, IsAnalyticsRelevant, Min/MaxLength, RegexPattern, Min/MaxNumber, Min/MaxDateTime, AllowedUrlSchemes)
  - The diff service does NOT reflect over properties. It is 100 explicit `if (local.Foo != remote.Foo) result.FieldChanges.Add(new FieldChange("Foo", local.Foo, remote.Foo))` lines. Boring is correct.
- **Sync service contract:**
  - `Task<EventTemplateSyncOutcome> ApplySync(Guid eventId, EventTemplateSyncPlan plan, int baseProvenanceVersion, CancellationToken ct)`
  - **Concurrency contract (Rule 15):** This check is **business-level** (`SourceTemplateVersion` check), not the technical concurrency token check. On stale `baseProvenanceVersion`, return `stale_sync_base` problem detail. A separate `DbUpdateConcurrencyException` translation handles `concurrent_update`.
  - Inside a transaction:
    1. Re-fetch runtime definitions with `AsTracking()` so `ConcurrencyStamp` is loaded
    2. Re-validate `baseProvenanceVersion` - return `stale_sync_base` if runtime's `SourceTemplateVersion` no longer matches
    3. Apply the operator's selected subset of changes from the plan (not the whole diff - the operator picks which changes to apply)
    4. Stamp `LastSyncedFromTemplateAt` + `SourceTemplateVersion` on each touched definition
    5. Call `IEventCustomPropertyProjectionUpdater.RefreshForEventAsync(eventId, ct)` INSIDE the same transaction (Rule 13 live projection)
    6. Write audit rows (operator identity, plan hash, source version, target version, applied counts, skipped counts, conflict counts)
    7. Commit; on `DbUpdateConcurrencyException`, translate to `concurrent_update` problem detail (Rule 15 technical concurrency)
- **Hard limits (Rule 16):** Reject plans exceeding `custom_properties.sync_apply_max_change_count` with `quota_exceeded` problem detail before opening the transaction
- **Acceptance Criteria:**
  - Diff is deterministic given same inputs
  - Sync is idempotent against `(eventId, planHash, baseProvenanceVersion)`
  - Sync rejects stale `baseProvenanceVersion` with a structured `stale_sync_base` result (not an exception, not a silent overwrite)
  - Sync translates `DbUpdateConcurrencyException` to `concurrent_update` distinctly from `stale_sync_base`
  - Sync calls projection updater inside the same transaction
  - Sync preserves historical values (retirement sets `IsActive = false`, never hard delete)
  - Sync writes audit rows capturing operator identity, applied plan hash, source + target template version, counts
  - Sync enforces `sync_apply_max_change_count` quota
  - Unit tests cover: empty diff, added-only, modified-only, retired-only, mixed, local-changes warning, stale sync base rejection, concurrent update rejection, quota rejection

#### Task 3.5A: Create Session Template Diff / Sync Service (**Milestone E**)
- **Placement:** mirrors Task 3.5 in `Explore.Application/Contracts/Services/` + `Explore.Application/Services/` for `IEventSessionTemplateDiffService` / `IEventSessionTemplateSyncService`
- **Contract:** mirrors event sync but operates on `EventSessionCustomPropertyDefinition` + `EventSessionTemplateCustomPropertyDefinition`. Same "keep it boring" rule applies - no generic abstraction shared with event.
- **Acceptance Criteria:** same as Task 3.5 with session scope

#### Task 3.6: Create Event Projection Updater / Rebuilder Service (**Milestone D1 Correctness**)
- **Placement:**
  - `IEventCustomPropertyProjectionUpdater` interface → `Explore.Application/Contracts/Services/`
  - `EventCustomPropertyProjectionUpdater` implementation → `Explore.Persistence/Projections/` (EF Core coupling justified)
- **Interface contract:**
  - `Task UpdateForValueAsync(Guid eventId, Guid definitionId, CancellationToken ct)` - called from runtime value commands inside the same transaction
  - `Task UpdateForDefinitionAsync(Guid eventId, Guid definitionId, CancellationToken ct)` - called from runtime definition commands inside the same transaction (handles definition-level changes that affect projection)
  - `Task RemoveForDefinitionAsync(Guid eventId, Guid definitionId, CancellationToken ct)` - called on definition delete (cascades to projection rows)
  - `Task RefreshForEventAsync(Guid eventId, CancellationToken ct)` - called from sync apply + rebuild
  - `Task RebuildForTenantAsync(Guid tenantId, int batchSize, CancellationToken ct)` - called from admin rebuild command (D2 operability)
  - `Task DrainDirtyScopesForTenantAsync(Guid tenantId, CancellationToken ct)` - operator self-service drain without full rebuild
- **Implementation rules:**
  - respects named query filters (`Tenant`, `SoftDelete`)
  - reads from `EventCustomPropertyDefinition` + `EventCustomPropertyValue` + `EventCustomPropertyOption` within the active DbContext transaction
  - writes to `EventCustomPropertyProjection` atomically per projected value row
  - computes `SearchToken`, `FilterFacetValue`, `ExportValue`, `ModerationValue` from definition + value shape
  - preserves exposure flags in the projection (the flags are the gate that lets a projection row be visible to a given discovery query)
  - does not cross tenant boundaries (defense-in-depth via `Tenant` query filter)
  - enforces `projection_rebuild_batch_size` quota on rebuild (Rule 16)
- **Rebuild coordination (D2 operability):**
  - acquires PostgreSQL advisory lock keyed on `(projection_name='event_custom_property_projection', tenant_id)` via `pg_try_advisory_xact_lock`
  - writes status to `custom_property_projection_status` tracking table (`active` → `rebuilding` → `active`)
  - rebuilds in batches of up to `projection_rebuild_batch_size`, committing each batch
  - **Drain-on-completion (D1 correctness requirement):** before releasing the advisory lock, iterates `custom_property_projection_dirty_scope` for the tenant and calls `RefreshForEventAsync` for each pending scope, marking `drained_at`
  - releases advisory lock on transaction end
  - On rebuild crash: a subsequent rebuild will re-run the drain naturally because dirty-scope rows remain pending
- **Inline writer behavior on skip (D1 correctness requirement):**
  - Attempts `pg_try_advisory_xact_lock` with the same key (shared mode)
  - If lock unavailable (rebuild in progress), **upserts a `CustomPropertyProjectionDirtyScope` row** inside the enclosing transaction with `reason = 'rebuild_in_progress'`
  - Runtime write still commits (both the runtime row and the dirty-scope row commit atomically)
  - Never silently drops the projection update - dirty-scope row is the receipt
  - If `max_dirty_scope_pending_per_tenant` quota is exceeded, **rejects** the runtime write with `quota_exceeded` and fires an operator alarm
- **Acceptance Criteria (D1 exit gate contribution):**
  - Unit tests for `UpdateForValueAsync`, `UpdateForDefinitionAsync`, `RemoveForDefinitionAsync`, `RefreshForEventAsync`
  - Testcontainers integration test: `RebuildForTenantAsync` with concurrent inline writes, verifies dirty-scope drain produces byte-identical projection state
  - Testcontainers integration test: inline write during rebuild produces a dirty-scope row atomically with the runtime write, and the row is drained on rebuild completion
  - Testcontainers integration test: rebuild crash mid-run, subsequent rebuild drains the remaining dirty scopes
  - Projection rebuild is byte-identical (modulo `UpdatedAt`) to a fresh build from source of truth
  - Projection rows carry `tenant_id` and respect `Tenant` + `SoftDelete` query filters
  - Rebuild never exceeds `projection_rebuild_batch_size` quota
  - Rebuild leaves `custom_property_projection_status` in `active` on success
  - Dirty-scope quota rejection test

#### Task 3.6A: Create Session Projection Updater / Rebuilder Service (**Milestone D1 Correctness**)
- **Placement:** mirrors Task 3.6 for `IEventSessionCustomPropertyProjectionUpdater` / `EventSessionCustomPropertyProjectionUpdater`
- **Same dirty-scope contract** as Task 3.6 with session scope (`projection_name='event_session_custom_property_projection'`)
- **Acceptance Criteria:** same as Task 3.6 with session scope

#### Task 3.6B: Create Projection Status Tracking Persistence (**Milestone D1 Correctness**)
- Add `CustomPropertyProjectionStatus` entity to `Explore.Domain/` (or as an infrastructure-only entity in `Explore.Persistence` since it carries no domain semantics)
- Add EF configuration with composite PK `(ProjectionName, ProjectionVersion, TenantId)` + snake_case columns + `Tenant` named query filter
- Repository interface for reading + updating status
- Migration adds `custom_property_projection_status` table

#### Task 3.6C: Create Projection Dirty-Scope Backlog Persistence (**Milestone D1 Correctness, CTO review 2026-04-11**)
- **Placement:**
  - `CustomPropertyProjectionDirtyScope` entity → `Explore.Domain/` (or infrastructure-only)
  - EF configuration → `Explore.Persistence/Configurations/Entities/CustomPropertyProjectionDirtyScopeConfiguration.cs`
  - `ICustomPropertyProjectionDirtyScopeRepository` interface → `Explore.Application/Contracts/Persistence/`
  - `CustomPropertyProjectionDirtyScopeRepository` → `Explore.Persistence/Repositories/`
- **Entity fields:** `Id` (long PK `bigserial`), `ProjectionName` (text), `ProjectionVersion` (int, default 1), `TenantId` (Guid), `ScopeType` (text), `ScopeId` (Guid), `DefinitionId` (Guid?), `Reason` (text), `CreatedAt` (timestamptz), `DrainedAt` (timestamptz?)
- **Uniqueness:** `UNIQUE (projection_name, projection_version, tenant_id, scope_type, scope_id, definition_id)` so repeated skips for the same scope don't blow up the table
- **Index:** partial index `ix_dirty_scope_pending` on `(projection_name, projection_version, tenant_id) WHERE drained_at IS NULL` for fast drain queries
- **Repository interface methods:**
  - `Task UpsertAsync(string projectionName, int projectionVersion, Guid tenantId, string scopeType, Guid scopeId, Guid? definitionId, string reason, CancellationToken ct)`
  - `Task<IReadOnlyList<CustomPropertyProjectionDirtyScope>> GetPendingAsync(string projectionName, int projectionVersion, Guid tenantId, CancellationToken ct)`
  - `Task MarkDrainedAsync(long id, CancellationToken ct)`
  - `Task<int> CountPendingAsync(string projectionName, int projectionVersion, Guid tenantId, CancellationToken ct)` - used for quota enforcement
- **Named query filter:** `Tenant` (defense-in-depth)
- **Migration:** adds `custom_property_projection_dirty_scope` table + index
- **Acceptance Criteria:**
  - Testcontainers integration test: upsert twice for same scope updates existing row (doesn't create duplicates)
  - Testcontainers integration test: `GetPendingAsync` returns only `drained_at IS NULL` rows for the specified tenant
  - Testcontainers integration test: `CountPendingAsync` matches actual pending rows
  - Unit test: repository respects `Tenant` named query filter (cross-tenant queries return empty)

#### Task 3.7: Register Repositories And Services In DI ✅ (existing) + Extensions
- Milestone D additions: register `IEventCustomPropertyProjectionUpdater`, `IEventSessionCustomPropertyProjectionUpdater`, `ICustomPropertyProjectionStatusRepository` in `PersistenceServicesRegistration.cs`
- Milestone E additions: register `IEventTemplateDiffService`, `IEventTemplateSyncService`, `IEventSessionTemplateDiffService`, `IEventSessionTemplateSyncService` in `ApplicationServicesRegistration.cs`

#### Task 3.8: Source-Id-First Provenance Matching ✅ (Milestones B, C)

---

### Phase 4: Application Layer - DTOs And Contracts
**Effort: XXL** | **Related Skills:** `cqrs-mediatr-guidelines`

#### Task 4.1: Create Shared Definition DTOs ✅ (Milestone A)

#### Task 4.2: Create Event Template DTOs ✅ (Milestone B)

#### Task 4.2A: Create EventSession Template DTOs ✅ (Milestone C)

#### Task 4.3: Create Event Runtime Definition / Value DTOs ✅ (Milestone B)

#### Task 4.3A: Create EventSession Runtime Definition / Value DTOs ✅ (Milestone C)

#### Task 4.4: Create Event Template Diff / Sync DTOs (**Milestone E**)
- Location: `Explore.Application/DTOs/EventTemplateSync/`
- Files:
  - `TemplateDiffDto` - `TargetTemplateVersion`, `BaseProvenanceVersion`, `AddedDefinitions`, `ModifiedDefinitions`, `RetiredDefinitions`, `AddedOptions`, `ModifiedOptions`, `RetiredOptions`, `UntouchedLocalDefinitions`
  - `AddedDefinitionDto` - full definition shape (namespace, key, type, validation, exposure)
  - `ModifiedDefinitionDto` - `RuntimeDefinitionId`, `SourceTemplateDefinitionId`, `FieldChanges` (list of `FieldChangeDto`), `HasLocalChanges` flag
  - `FieldChangeDto` - `FieldName`, `OldValue`, `NewValue`, `ValueType`
  - `RetiredDefinitionDto` - `RuntimeDefinitionId`, `Namespace`, `Key`
  - `AddedOptionDto`, `ModifiedOptionDto`, `RetiredOptionDto` - same pattern for options
  - `UntouchedLocalDefinitionDto` - ad-hoc runtime definitions that have no template source
  - `TemplateSyncPlanDto` - operator's selected changes (checkbox-style selection of IDs from the diff)
  - `TemplateSyncOutcomeDto` - `AppliedChanges`, `SkippedChanges`, `Conflicts`, `NewProvenanceVersion`, `SyncedAt`
- Validators: `TemplateSyncPlanDtoValidator` manually instantiated in the command handler

#### Task 4.4A: Create EventSession Template Diff / Sync DTOs (**Milestone E**)
- Location: `Explore.Application/DTOs/EventSessionTemplateSync/`
- Mirrors Task 4.4 for session-scope

#### Task 4.5: Create Event Custom-Property Projection DTOs (**Milestone D**)
- Location: `Explore.Application/DTOs/EventCustomPropertyProjection/`
- Files:
  - `EventCustomPropertyProjectionDto` - `EventId`, `Namespace`, `Key`, `PropertyType`, `ExposureLevel`, `SearchToken`, `FilterFacetValue`, `ExportValue`, `ModerationValue`, `UpdatedAt`
  - `EventCustomPropertyProjectionFacetDto` - public-facing subset used in discovery responses (no moderation column)
  - `EventCustomPropertyProjectionListDto` - paged list item shape
  - `RebuildEventCustomPropertyProjectionRequestDto` - admin rebuild request body (`TenantId`, `BatchSize`, `Scope` with options `SingleEvent | AllEvents | RecentlyUpdated`)
  - `RebuildEventCustomPropertyProjectionResponseDto` - `Status`, `EventsProcessed`, `ProjectionRowsWritten`, `StartedAt`, `CompletedAt`, `LastError`
- Exposure filter: DTO mappers in handlers apply the caller's `ExposureLevel` ceiling before emitting facets to any non-admin caller
- Validators manually instantiated in handlers

#### Task 4.5A: Create Aggregate Event-With-Sessions View DTOs And Lexicon Contracts (**Milestone F**)
- Location: `Explore.Application/DTOs/EventAggregateView/`
- Files:
  - `EventWithSessionsViewDto` - canonical event fields + Layer 2 Islamic/Tech aspect DTOs (nullable, module-gated) + exposure-filtered Layer 3 projection facets + list of `EventSessionSummaryDto`
  - `EventSessionSummaryDto` - session Layer 1 fields + Layer 2 session Islamic aspect (nullable) + exposure-filtered Layer 3 session projection facets
  - `EventCustomPropertyFacetDto` (reuse from 4.5) and `EventSessionCustomPropertyFacetDto`
  - `EventListAggregateViewDto` - paged list item shape
- Lexicon planning (docs only, not code):
  - `docs/LEXICONS.md` or `docs/FEDERATION.md` addition with NSID hierarchy: `im.islamu.event.core.v1`, `im.islamu.eventsession.core.v1`, `im.islamu.event.islamic.v1`, `im.islamu.event.tech.v1`, `im.islamu.eventsession.islamic.v1`, `im.islamu.event.extension.v1`, `im.islamu.eventsession.extension.v1`, `im.islamu.event.withSessions.v1`, `im.islamu.event.temp.*`
  - add-only evolution rules + NSID versioning discipline
  - no adoption of ATProto Discussion #1889 `$extensions` pattern until it standardizes

#### Task 4.6: Re-audit Event DTOs / Generated Contracts And Remove Any Stale Metadata-Blob Assumptions (**Milestone D or before**)
- Walk every DTO in `Explore.Application/DTOs/Event/` looking for any `MetadataJson`, `Metadata`, or generic `Dictionary<string, object>` properties
- Remove or replace with first-class fields / typed custom-property references
- Ensure `CreateEventDto`, `UpdateEventDto`, `EventDto`, `EventListDto`, and any generated API client DTOs are free of stale metadata-blob assumptions
- Flag any NSwag-generated client drift that would need client regeneration

#### Task 4.7: Re-audit Organization DTOs / Generated Contracts And Remove Any Stale Metadata-Blob Assumptions
- Same process applied to `Explore.Application/DTOs/Organization/`

#### Task 4.8: Re-audit Group DTOs / Generated Contracts And Remove Any Stale Metadata-Blob Assumptions
- Same process applied to `Explore.Application/DTOs/Group/`

#### Task 4.9: Update Mapping Profiles ✅ (existing, Milestones A/B/C)
- Milestone D additions: `EventCustomPropertyProjection` → `EventCustomPropertyProjectionDto` + facet DTO
- Milestone D additions: session projection mappings
- Milestone E additions: template diff/sync result → DTO mappings
- Milestone F additions: `EventWithSessionsView` keyless entity → `EventWithSessionsViewDto`

---

### Phase 5: Application Layer - CQRS For Definitions, Templates, Runtime Values, Sync, And Projections
**Effort: XXXL** | **Related Skills:** `cqrs-mediatr-guidelines`

#### Task 5.0: Preserve Layer 2 CQRS Paths For Sector-Standard Schema
- **Acceptance Criteria:**
  - Sector-standard typed aspect/profile commands and queries remain distinct from Layer 3 custom-property flows. Specifically: `UpsertEventIslamicAspectCommand`, `UpsertEventTechAspectCommand`, `DeleteEventIslamicAspectCommand`, `DeleteEventTechAspectCommand`, `GetEventIslamicAspectRequest`, and `GetEventTechAspectRequest` continue to live under `Explore.Application/Features/EventAspects/` (or equivalent) and do not depend on any custom-property handler.
  - Layer 2 filtering/moderation/policy logic continues to compose `IslamicAspectFilter`, `TechAspectFilter`, `AspectPresenceFilter` inside `EventQuerySpecification`; this composition path is not migrated onto projection-backed queries.
  - Module gating (`IModuleService.IsModuleEnabledAsync(tenantId, "Mod_Islamic")`, `"Mod_Tech"`) at query time remains the canonical gating mechanism for Layer 2 aspects. Layer 3 projection queries follow the same pattern with their own module/feature flag.

#### Task 5.1: CRUD Commands / Queries For Shared Organization / Group Definitions ✅ (Milestone A)

#### Task 5.2: CRUD Commands / Queries For Event Templates ✅ (Milestone B)

#### Task 5.2A: CRUD Commands / Queries For EventSession Templates ✅ (Milestone C)

#### Task 5.3: CRUD Commands / Queries For Template Options ✅ (Milestones B, C)

#### Task 5.4: Queries For Event-Local Definitions And Values ✅ (Milestone B)

#### Task 5.4A: Queries For EventSession-Local Definitions And Values ✅ (Milestone C)

#### Task 5.5: Commands For Setting Event-Local Values With Explicit Multi-Value Rules ✅ (Milestone B)

#### Task 5.5A: Commands For Setting EventSession-Local Values With Explicit Multi-Value Rules ✅ (Milestone C)

#### Task 5.6: Commands For Editing Event-Local Definitions ✅ (Milestone B)

#### Task 5.6A: Commands For Editing EventSession-Local Definitions ✅ (Milestone C)

#### Task 5.7: Commands / Queries For Event Template Diff And Sync (**Milestone E**)
- **Location:** `Explore.Application/Features/EventTemplateSync/`
- **Queries:**
  - `GetEventTemplateDiffQuery(Guid EventId, int TargetTemplateVersion) : IRequest<BaseCommandResponse<TemplateDiffDto>>` - handler resolves runtime, resolves target template version, calls `IEventTemplateDiffService.BuildDiff(...)`, returns structured diff
- **Commands:**
  - `ApplyEventTemplateSyncCommand(Guid EventId, TemplateSyncPlanDto Plan, int BaseProvenanceVersion) : IRequest<BaseCommandResponse<TemplateSyncOutcomeDto>>` - handler manually instantiates validator, calls `IEventTemplateSyncService.ApplySync(...)`, handles conflict response as RFC 7807 problem detail
- **Pipeline:**
  - Validation (manual validator instantiation per project rule, no DI validators)
  - Authorization via `IAuthorizedRequest` - new authorization category `event_template_sync` (templates require tenant_admin or event_template_admin role)
  - Logging (existing `PerformanceBehavior`)
  - Transaction wraps the sync service call
  - Projection updater is called **inside** the same transaction by the sync service
- **Acceptance Criteria:**
  - diff handler is read-only (`AsNoTracking`)
  - apply handler is transactional and atomic
  - apply handler rejects stale `baseProvenanceVersion` with structured conflict (not exception)
  - apply handler writes audit entries before commit
  - unit tests for happy path, stale version, mixed change types, local-changes warning

#### Task 5.7A: Commands / Queries For EventSession Template Diff And Sync (**Milestone E**)
- **Location:** `Explore.Application/Features/EventSessionTemplateSync/`
- Mirrors Task 5.7 for session scope

#### Task 5.8: Commands / Jobs For Projection Updates, Rebuilds, Dirty-Scope Drain, And Governance Reporting (**Milestone D**)
- **Location:** `Explore.Application/Features/EventCustomPropertyProjections/` and `Explore.Application/Features/EventSessionCustomPropertyProjections/` + `Explore.Application/Features/CustomPropertyGovernance/`
- **Commands (D2 operability):**
  - `RebuildEventCustomPropertyProjectionCommand(Guid TenantId, RebuildScope Scope, int BatchSize) : IRequest<BaseCommandResponse<RebuildEventCustomPropertyProjectionResponseDto>>` - handler calls `IEventCustomPropertyProjectionUpdater.RebuildForTenantAsync(...)`
  - `RebuildSingleEventCustomPropertyProjectionCommand(Guid EventId) : IRequest<BaseCommandResponse<Guid>>` - handler calls `RefreshForEventAsync(...)`
  - `DrainCustomPropertyProjectionDirtyScopesCommand(Guid TenantId, string ProjectionName) : IRequest<BaseCommandResponse<DrainDirtyScopesResponseDto>>` - handler calls `DrainDirtyScopesForTenantAsync(...)` - operator self-service drain without full rebuild (CTO review)
  - Matching session-scope commands for `EventSessionCustomPropertyProjection`
- **Queries (D2 operability + Rule 12 enforcement):**
  - `GetEventCustomPropertyProjectionStatusQuery(Guid TenantId) : IRequest<BaseCommandResponse<IReadOnlyList<ProjectionStatusDto>>>` - returns current `custom_property_projection_status` rows for observability
  - `GetCustomPropertyProjectionDirtyScopesQuery(Guid TenantId, string ProjectionName, int Skip, int Take) : IRequest<BaseCommandResponse<Paged<ProjectionDirtyScopeDto>>>` - returns pending dirty-scope rows for operator inspection
  - `GetEventCustomPropertyProjectionsForEventQuery(Guid EventId, ExposureLevel? ExposureCeiling) : IRequest<BaseCommandResponse<IReadOnlyList<EventCustomPropertyProjectionDto>>>` - for admin inspection + aggregate view composition (Milestone F dependency)
  - `GetCustomPropertyGovernanceReportQuery(Guid TenantId, GovernanceReportFilter Filter) : IRequest<BaseCommandResponse<Paged<CustomPropertyGovernanceRowDto>>>` - **CTO review**: operational governance surface for Rule 12. Returns all active Layer 3 definitions with flags + instance counts + `PromotionRecommendation`. See `Operational Governance Surface` section for DTO shape.
- **Pipeline:**
  - Validation (manual)
  - Authorization via `property_governance_admin` category for rebuild + drain + governance-report commands
  - Long-running: rebuild command uses the `Complex` request timeout policy (60 seconds) or triggers a background worker if work exceeds the timeout
  - Governance-report query uses the `Lookup` timeout policy (10 seconds) with pagination
- **Acceptance Criteria:**
  - Rebuild is replay-safe (running twice produces the same projection state)
  - Rebuild honors `projection_rebuild_batch_size` quota
  - Rebuild updates `custom_property_projection_status` (`active` → `rebuilding` → `active`)
  - Rebuild drains dirty scopes on completion (D1 correctness requirement)
  - Drain command is independently callable and idempotent
  - Governance report returns deterministic `PromotionRecommendation` for seeded fixtures (unit-tested against the Atlassian 4-question matrix)
  - Governance report respects tenant isolation
  - Testcontainers integration tests cover: rebuild + concurrent inline writes + dirty-scope drain

#### Task 5.9: Promotion Rules For Discovery-Critical Properties (**Milestone D**)
- **Purpose:** operational workflow for promoting a Layer 3 custom property to a **projection-first discovery** shape without yet promoting to Layer 2. This is the escape hatch for Rule 12 criteria (search/filter/automation) when the semantic isn't sector-standard enough for Layer 2 yet.
- **Scope:**
  - Add a new `PromoteCustomPropertyToProjectionFirstCommand(Guid TenantId, string Namespace, string Key)` that forces `IsSearchable = true`, `IsFilterable = true`, and ensures projection is populated for that namespace+key across all tenants' runtime rows
  - Document the promotion decision in an audit trail
- **Acceptance Criteria:**
  - command is idempotent
  - promotion is reflected in `custom_property_projection_status` as a version bump
  - unit + integration tests prove promoted property lights up in discovery filters after promotion

#### Task 5.10: Promotion Rules For Sector-Standard Properties (**Milestone E/F or opportunistic**)
- **Purpose:** when the Rule 12 4-question framework flags a Layer 3 property as sector-standard enough to warrant Layer 2 modeling, this task is the operational playbook for lifting it out of EAV and into a typed aspect/profile.
- **Steps (per promoted property):**
  1. Add the field to an existing or new typed aspect entity in `Explore.Domain/`
  2. Add EF configuration + migration
  3. Add backfill command that reads from runtime / projection rows and writes to the new typed column transactionally
  4. Retire the Layer 3 definition (`IsActive = false`, not hard delete) so historical values remain
  5. Update `EventQuerySpecification` composition to read from the new typed column
  6. Update documentation (DOMAIN.md, MODULAR_EVENTS.md, ADR if substantial)
- **Acceptance Criteria:** implementation path exists; a concrete promotion (e.g. `platform.event.language` → `Event.Language` Layer 1 or Layer 2) is documented as the template for future promotions

---

### Phase 6: Event + EventSession Creation, Template Instantiation, And Editing Flow
**Effort: XXL** | **Related Skills:** `cqrs-mediatr-guidelines`

#### Task 6.1: Extend Event Creation Contract With Optional Template Selection ✅ (Milestone B)

#### Task 6.2: Instantiate Event-Local Definitions/Options/Defaults Transactionally ✅ (Milestone B)

#### Task 6.2A: Instantiate Session Templates And Session-Local Definitions/Options/Defaults Transactionally ✅ (Milestone C)

#### Task 6.3: Support Event Creation Without Template ✅ (Milestone B)

#### Task 6.4: Ensure Event Edit Reads Event-Local Configuration Only ✅ (Milestone B)

#### Task 6.5: Add Template Sync Decision Flow To Event Administration (**Milestone E**)
- **Scope:**
  - Admin UI (Phase 9.8) offers a "Sync from template" action for any event with `SourceTemplateId != null`
  - Action calls `GET /events/{eventId}/template-sync/diff?templateVersion={version}` to populate a diff view
  - Operator selects subset, confirms, and action posts `POST /events/{eventId}/template-sync/apply`
  - UI shows structured conflict details on 409 and allows re-diff
  - UI warns when modified definitions have `HasLocalChanges = true`
- **Acceptance Criteria:**
  - flow from diff → plan selection → apply → outcome is deterministic and idempotent
  - operator cannot apply a sync against a stale `baseProvenanceVersion` (server rejects)
  - operator cannot silently lose local edits (UI warns)

#### Task 6.5A: Add Template Sync Decision Flow To EventSession Administration (**Milestone E**)
- Mirrors Task 6.5 for session scope

#### Task 6.6: Keep Layer 2 Editing Separate From Layer 3 Editing
- **Status:** Architecturally already enforced by Milestones B/C construction (Layer 2 upsert commands are separate from Layer 3 value set commands; shared-key 1:1 pattern on aspects vs `EventId`-FK'd EAV pattern on custom properties), but this task explicitly verifies and documents the separation.
- **Acceptance Criteria:**
  - typed aspect/profile commands remain in `Explore.Application/Features/EventAspects/`
  - Layer 3 custom-property commands remain in `Explore.Application/Features/EventCustomProperties/`
  - no handler calls across the two families
  - architecture test asserts the separation
  - Blazor admin UI renders two distinct sections (Layer 2 "Sector profile" section with typed inputs vs Layer 3 "Custom properties" section with dynamic inputs)

#### Task 6.7: Add Parent Event Aggregate Read/View Flow (**Milestone F**)
- **Scope:**
  - `GetEventWithSessionsAggregateViewQuery(eventId, exposureCeiling)` query handler uses the `EventWithSessionsView` keyless entity via `IQuerySpecification<EventWithSessionsView>`
  - `EventController.GetAggregateView(eventId)` endpoint exposes the aggregate read
  - Admin event page + public event page both read from this aggregate query (Phase 10.3A dependency)
- **Acceptance Criteria:**
  - event page embeds linked session summaries without turning sessions into canonical peer events (Rule 11)
  - aggregate view respects exposure ceiling
  - aggregate view includes Layer 2 aspects only when module enabled for tenant

---

### Phase 7: Remove Stale Metadata Assumptions And Legacy Planning Drift
**Effort: L**

> Note: The codebase already uses first-class appearance columns and `StorageObject` references. This phase is a final cleanup pass across source, docs, and contracts to remove lingering `MetadataJson` language and eliminate any generic-metadata-blob assumptions. Scheduled for execution **before or alongside Milestone D** (since stale docs will confuse Milestone D contributors).

#### Task 7.1: Re-audit Remaining Source References To `MetadataJson` And Remove Any Actual Runtime Coupling If Found
- Grep for: `MetadataJson`, `Metadata\s*=`, `Dictionary<string, object>`, `jsonb` string literal in domain/application layers
- For each hit: classify as (a) legitimate (e.g. `EventSessionIslamicAspect.RitualRequirementsJson`), (b) stale comment/doc, (c) actual runtime coupling to remove
- For (c): replace with first-class fields or Layer 3 custom properties via governance policy
- **Acceptance Criteria:** no active runtime code path depends on a generic metadata blob; the only legitimate JSONB columns are those explicitly documented in the domain (e.g. session ritual requirements)

#### Task 7.2: Clean Up Stale Comments, Docs, And Contracts That Still Assume JSONB Metadata Storage
- Walk `docs/`, `dev/active/`, inline comments, XML doc comments
- Remove or update language that refers to `MetadataJson` as the canonical extension mechanism
- **Acceptance Criteria:** docs are aligned with the 3-layer architecture; no lingering "use MetadataJson for X" guidance

#### Task 7.3: Align Event / Organization / Group Write Paths With The Current First-Class Appearance/Branding Model
- Verify `CreateEventCommandHandler`, `UpdateEventCommandHandler`, Organization/Group equivalents do not write through a generic metadata bag
- Ensure Blazor UI writes appearance fields via typed DTOs, not a generic metadata form

#### Task 7.4: Ensure No New Runtime Query Path Depends On Generic Metadata Blobs
- Audit any new Milestone D/E/F query specifications to confirm they read from typed columns + projection rows, never a generic metadata bag

#### Task 7.5: Remove Any Stale Event.MetadataJson References In Event Write Handlers
- Specific to `Event` write handlers if any lingering reference exists

#### Task 7.6: Remove Any Stale Group/Organization MetadataJson References In Group/Organization Write Handlers
- Specific to Group/Organization write handlers if any lingering reference exists

---

### Phase 8: API Layer
**Effort: XXL** | **Related Skills:** `auth-patterns`

#### Task 8.1: Shared Definition Controllers For Organization / Group Custom Properties ✅ (Milestone A)

#### Task 8.2: Event Template Controllers ✅ (Milestone B)

#### Task 8.2A: EventSession Template Controllers ✅ (Milestone C)

#### Task 8.3: Event Runtime Definition / Value Controllers ✅ (Milestone B)

#### Task 8.3A: EventSession Runtime Definition / Value Controllers ✅ (Milestone C)

#### Task 8.4: Event Template Diff / Sync Controllers (**Milestone E**)
- **Location:** `Explore.API/Controllers/EventTemplateSyncController.cs`
- **Endpoints:**
  - `GET /events/{eventId}/template-sync/diff?templateVersion={version}` - returns `TemplateDiffDto` with HAL links to `apply`, `event`, `template`
  - `POST /events/{eventId}/template-sync/apply` - accepts `TemplateSyncPlanDto` + `baseProvenanceVersion`, returns `TemplateSyncOutcomeDto`
  - `GET /events/{eventId}/template-sync/history` - returns audit history of prior syncs
- **HATEOAS:**
  - `EventTemplateSyncLinkPolicy` under `Explore.API/Hateoas/Policies/`
  - `EventTemplateSyncResourceAssembler` under `Explore.API/Hateoas/Assemblers/`
  - Route constants in `Explore.API/Hateoas/RouteNames.cs`
- **Authorization:** `[Authorize(Policy = "event_template_sync")]` - new policy for tenant_admin or event_template_admin roles
- **Error Handling:** RFC 7807 problem detail for 409 Conflict with `current_provenance_version`, `target_template_version`, `reason` fields
- **Rate Limiting:** uses `authenticated` policy
- **Request Timeout:** `Complex` (60s) for apply; `Default` for diff/history

#### Task 8.4A: EventSession Template Diff / Sync Controllers (**Milestone E**)
- Mirrors Task 8.4 at `Explore.API/Controllers/EventSessionTemplateSyncController.cs` with `/event-sessions/{sessionId}/template-sync/*` routes

#### Task 8.5: Projection Admin, Rebuild, Dirty-Scope, And Governance Reporting Endpoints (**Milestone D2 Operability**)
- **Location:** `Explore.API/Controllers/CustomPropertyProjectionAdminController.cs` + `Explore.API/Controllers/CustomPropertyGovernanceController.cs`
- **Endpoints (projection operability):**
  - `GET /admin/custom-property-projections/status?tenantId={tenantId}` - returns `ProjectionStatusDto[]`
  - `POST /admin/custom-property-projections/rebuild` - accepts `RebuildEventCustomPropertyProjectionRequestDto`, returns 202 Accepted with progress URI
  - `POST /admin/custom-property-projections/rebuild-single-event` - accepts `{ eventId }` for targeted rebuild
  - `GET /admin/custom-property-projections/dirty-scopes?tenantId={tenantId}&projectionName={name}&skip&take` - returns paged `ProjectionDirtyScopeDto` (CTO review: dirty-scope observability)
  - `POST /admin/custom-property-projections/drain-dirty-scopes` - accepts `{ tenantId, projectionName }`, returns `DrainDirtyScopesResponseDto` with drained count (operator self-service)
  - Session equivalents under `/admin/custom-property-projections/sessions/*`
- **Endpoints (governance reporting - CTO review Rule 12 enforcement):**
  - `GET /admin/custom-property-definitions/governance-report?tenantId={tenantId}&scope={EntityScope}&recommendation={PromotionRecommendation}&skip&take` - returns paged `CustomPropertyGovernanceRowDto` with promotion recommendations
- **HATEOAS:** `CustomPropertyProjectionAdminLinkPolicy` + `CustomPropertyGovernanceLinkPolicy` + matching assemblers
- **Authorization:** `[Authorize(Policy = "property_governance_admin")]` on all endpoints
- **Rate Limiting:** `write` policy for POST endpoints; `authenticated` for GET endpoints
- **Request Timeout:** `Complex` (60s) for rebuild + drain; `Lookup` (10s) for status/dirty-scopes/governance-report queries
- **Observability:**
  - Log rebuild start/complete via `error-tracking` skill
  - Emit Prometheus metrics: `eav_projection_rebuild_duration_seconds{tenant_id}`, `eav_projection_rebuild_rows_processed_total{tenant_id}`, `eav_projection_dirty_scope_pending_total{projection_name,tenant_id}` (gauge), `eav_projection_dirty_scope_drained_total{projection_name,tenant_id}` (counter)
  - Operator runbook covering `what is broken / stale / rebuildable / how do I recover` in `docs/OPERATIONS.md` (D2 exit gate requirement)

#### Task 8.6: Reconcile `EventController.cs` And Related API Contracts With Template-Aware Event Creation (**Milestone B follow-up**)
- **Scope:**
  - Verify `POST /events` accepts `TemplateId` in request body and the generated NSwag client exposes it
  - Verify `GET /events/{id}` response includes HATEOAS link to `template-sync/diff` when event has `SourceTemplateId != null`
  - Verify `GET /events/{id}` response includes HATEOAS link to custom-properties sub-resource
  - Remove any stale `MetadataJson` or generic-metadata field from request/response contracts
- **Acceptance Criteria:**
  - integration test confirms template-aware create → read roundtrip
  - NSwag client regenerated and verified

#### Task 8.6A: Reconcile `EventSessionController.cs` And Related API Contracts With Session Template/Layer 3 Workflows (**Milestone C follow-up**)
- Mirrors Task 8.6 for session scope

#### Task 8.7: Add Governance-Oriented Authorization Policies (**Milestone D/E**)

> **Simplified taxonomy (CTO review 2026-04-11):** The previous revision listed seven policies; this collapsed the permission model before any workflow demanded it. Start with **four** policies that cover every endpoint introduced by Milestones D, E, F. Subdivide later if and only if actual workflows demand it.

- **Policies to add in `Explore.API/Extensions/AuthorizationExtensions.cs`:**
  - **`template_admin`** - covers everything related to templates (create/update/delete templates + create/update/delete template definitions and options + operate template-sync diff/apply flow). This is "a person who owns template design for a tenant." Rolls in the previous `event_template_sync` policy because the same people run sync that design templates.
  - **`event_editor`** - covers everything related to events and event sessions that an organizer touches (create/update events, edit event-local definitions, set event-local values, create/update event sessions, edit session-local definitions, set session-local values). Rolls in the previous `event_session_editor` because these are the same people in practice.
  - **`property_governance_admin`** - covers everything related to projection operability and governance reporting (rebuild projections, drain dirty scopes, inspect projection status, run governance report, promote properties to projection-first, manage exposure flags across tenant). Rolls in the previous `custom_property_projection_admin` as an alias that no longer exists.
  - **`platform_namespace_editor`** - covers the **only** write path that touches `platform.*` namespaced definitions. Default **deny** for everyone; explicitly granted only to platform operators. This stays a separate policy because "touching platform namespace" is qualitatively different from "touching tenant namespace."
- **Policy-to-endpoint mapping:**
  - `POST /events`, `PUT /events/{id}`, `POST /events/{id}/custom-properties/*`, `POST /event-sessions`, `PUT /event-sessions/{id}`, `POST /event-sessions/{id}/custom-properties/*` → `event_editor`
  - `POST /event-templates`, `PUT /event-templates/{id}`, `DELETE /event-templates/{id}`, `POST /event-session-templates`, `PUT /event-session-templates/{id}`, `DELETE /event-session-templates/{id}`, `GET /events/{id}/template-sync/diff`, `POST /events/{id}/template-sync/apply`, `GET /event-sessions/{id}/template-sync/diff`, `POST /event-sessions/{id}/template-sync/apply` → `template_admin`
  - `GET /admin/custom-property-projections/*`, `POST /admin/custom-property-projections/*`, `GET /admin/custom-property-definitions/governance-report`, `POST /admin/custom-property-definitions/*/promote-to-projection-first` → `property_governance_admin`
  - Any write path that targets `namespace = 'platform'` → `platform_namespace_editor` (most commonly a deny-by-default rule checked at the governance policy layer, not the endpoint)
- **Acceptance Criteria:**
  - Cerbos-backed policies (per project auth stack) express each of the 4 policies
  - Each new endpoint explicitly uses the correct policy
  - Integration tests verify authorized vs unauthorized responses
  - **Test: subdivision is deferred, not impossible** - write one test proving that if Milestone E/F later introduces a new workflow requiring a subdivision, the policy can be split without renaming or breaking existing endpoints
  - Document policy taxonomy in `docs/SECURITY.md`

---

### Phase 9: Blazor Client Updates
**Effort: XXXL** | **Related Skills:** `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `accessibility`

> **Global UI constraints (locked):** MudBlazor v9 only; default render mode `InteractiveAuto`; BEM class naming with CSS isolation; `12px` rounded corners; `Elevation 0-1` subtle shadows; muted neutral palette; accent color reserved for primary CTAs; WCAG 2.2 AA minimum accessibility. No `HttpContext` usage in `InteractiveAuto` or WASM components. Never use raw HTML where a MudBlazor component exists.

#### Task 9.1: Reconcile Appearance Helpers And UI Utilities With The Existing First-Class Appearance Model
- Walk `Explore.Blazor.Client/` for any `EventAppearanceMetadataHelper`, `OrganizationAppearanceMetadataHelper`, `GroupBrandingMetadataHelper` or similar helper that still assumes a metadata blob
- Replace with direct typed property access on the already-first-class `BackgroundColor`, `BackgroundMediaUrl`, `BackgroundEffect` fields
- **Acceptance Criteria:** zero uses of a generic metadata dictionary in appearance code

#### Task 9.2: Add Shared Definition Governance UI For Organization / Group (**Milestone A follow-up**)
- **Location:** `Explore.Blazor.Client/Pages/Admin/CustomProperties/`
- **Components:**
  - `CustomPropertyDefinitionListPage.razor` - MudDataGrid with filter by Namespace, Key, PropertyType, IsActive
  - `CustomPropertyDefinitionDetailsPage.razor` - full definition shape with MudTextField, MudNumericField, MudSwitch, MudSelect for enum values
  - `CustomPropertyDefinitionEditor.razor` - reusable editor component; rendered inside both Organization and Group contexts
  - `CustomPropertyOptionEditor.razor` - nested child editor for managing options; supports reordering via drag-drop (MudBlazor v9 MudItemList)
- **Accessibility:**
  - All inputs have MudField labels + `aria-label` or `aria-describedby`
  - Keyboard navigation for list filters + detail form
  - Error messages announced via `role="alert"`
- **Acceptance Criteria:** admin can CRUD shared definitions without touching a raw JSON editor

#### Task 9.3: Add Event Template Management UI (**Milestone B follow-up**)
- **Location:** `Explore.Blazor.Client/Pages/Admin/EventTemplates/`
- **Components:**
  - `EventTemplateListPage.razor` - MudDataGrid showing templates with TemplateKey, Version, IsPublished
  - `EventTemplateDetailsPage.razor` - shows template + nested definition list + option list
  - `EventTemplateEditor.razor` - template-level editing (key, display name, event type, version, publish state)
  - `EventTemplateDefinitionEditor.razor` - reused by runtime editor via parameter for `IsTemplateMode`; renders full definition shape including validation + exposure flags
- **Versioning UX:** creating a new version from an existing template triggers a MudDialog confirmation; the new version starts as unpublished
- **Acceptance Criteria:** admin can build an event template end-to-end and publish it

#### Task 9.4: Add Template Selection To Event Creation UI (**Milestone B follow-up**)
- **Location:** `Explore.Blazor.Client/Pages/Events/EventCreate.razor`
- **Behavior:**
  - An "Use template" MudSelect dropdown lists published templates for the current tenant + event type (if `EventTypeId` on template matches or is null)
  - Selecting a template previews (non-interactive) the definitions that will be instantiated
  - On submit, the create request includes `TemplateId`
- **Acceptance Criteria:**
  - template preview is read-only
  - template selection is optional (empty selection creates a vanilla event)
  - accessible focus order: event basics → template selection → preview → submit

#### Task 9.4A: Add Session Blueprint Selection / Editing To Event Session UI (**Milestone C follow-up**)
- Mirrors Task 9.4 in `Explore.Blazor.Client/Pages/EventSessions/EventSessionCreate.razor`
- Session template dropdown is scoped to the parent event's template tree (only session templates owned by the parent's `EventTemplateId`)

#### Task 9.5: Add Event Runtime Custom-Property Editor Against Event-Local Definitions (**Milestone B follow-up**)
- **Location:** `Explore.Blazor.Client/Components/CustomProperties/EventCustomPropertyRuntimeEditor.razor`
- **Dynamic form rendering strategy:**
  - Not reflection-based; drives off `PropertyType` enum (Text, Number, Option, Boolean, DateTime, Url)
  - Switch-based component selection:
    - `Text` → `MudTextField` (with `MaxLength` and `RegexPattern` applied as input attributes)
    - `Number` → `MudNumericField<decimal?>` (with `Min`/`Max`)
    - `Option` → single-select `MudSelect<Guid>` or multi-select `MudSelect<Guid>` with `MultiSelection=true` based on `IsMulti`
    - `Boolean` → `MudSwitch<bool?>` or `MudCheckBox<bool?>`
    - `DateTime` → `MudDatePicker` + `MudTimePicker` composition
    - `Url` → `MudTextField` with URL validation + `AllowedUrlSchemes` check via a manual validator
  - Multi-value rendering: repeated field with "Add" + "Remove" buttons; `Ordinal` computed from position
  - Required markers + validation errors rendered inline per definition
- **State management:** component accepts `EventCustomPropertyDefinitionDto[]` + existing value rows; emits `SetEventCustomPropertyValueDto` / `SetEventCustomPropertyMultiValuesDto` via `EventCallback`
- **Accessibility:**
  - each dynamic input has a proper label (DisplayName) and description (Description)
  - required fields carry `aria-required="true"`
  - errors carry `aria-invalid="true"` and are linked to the input via `aria-describedby`
  - keyboard navigation is tested for add/remove multi-value rows
- **BEM classes:** `event-cpr-editor__field`, `event-cpr-editor__field--required`, `event-cpr-editor__field--multi`
- **Acceptance Criteria:**
  - editor renders every `PropertyType` correctly
  - multi-value add/remove is reordering-aware via Ordinal
  - validation errors match server-side validators
  - keyboard-only users can complete the form

#### Task 9.5A: Add EventSession Runtime Custom-Property Editor Against Session-Local Definitions (**Milestone C follow-up**)
- `EventSessionCustomPropertyRuntimeEditor.razor` mirrors Task 9.5

#### Task 9.6: Add Template Selection Preview For Admin Overview (**Milestone B follow-up**)
- Secondary admin page listing which events were created from a given template and when
- Useful for Milestone E: operator can jump straight into the diff/sync flow from this list

#### Task 9.6A: Add Session Blueprint Preview For Admin Overview (**Milestone C follow-up**)
- Mirrors Task 9.6 for session scope

#### Task 9.7: Add Exposure / Searchability / Exportability Governance UX (**Milestone D**)
- **Location:** `Explore.Blazor.Client/Pages/Admin/CustomProperties/ExposureGovernance.razor`
- **Behavior:**
  - Tenant admin lists all definitions (shared + template + runtime) and their `ExposureLevel` + `IsSearchable` + `IsFilterable` + `IsExportable` + `IsModerationRelevant` + `IsAnalyticsRelevant` flags
  - Editing a flag on a runtime definition triggers projection update inline (Milestone D dependency)
  - Bulk operations (select many, update exposure) via MudDataGrid selection
- **Accessibility:** MudDataGrid bulk actions must be reachable via keyboard; flag toggles must be labeled

#### Task 9.8: Add Event Template Diff / Sync UX (**Milestone E**)
- **Location:** `Explore.Blazor.Client/Pages/Admin/EventTemplateSync/EventTemplateSyncPage.razor`
- **Components:**
  - Header with event identity + current `SourceTemplateVersion` + target template version selector
  - Diff view rendered as three tabs or sections: Added, Modified, Retired (+ Options sub-sections)
  - For each modified definition, a side-by-side diff view using `BlazorTextDiff` (small community lib built on DiffPlex) or a custom MudBlazor diff component backed directly by DiffPlex; whichever is chosen, the component MUST support character-level highlighting and collapsible unchanged regions
  - Per-change checkbox: "Apply this change"
  - Local-changes warning banner when `HasLocalChanges == true` for a modified definition
  - Confirm button opens a MudDialog summarizing the plan and asking the operator to type the event slug to confirm (destructive-action pattern)
- **Error handling:**
  - 409 Conflict shows an in-page alert with "re-diff" button
  - Server errors render via the global toast provider
- **Accessibility:**
  - diff view has proper semantic markup (`<table>` or `<dl>` with appropriate roles)
  - side-by-side diff has `aria-label` explaining old vs new
  - confirm dialog traps focus and returns focus to the trigger button on close
- **Acceptance Criteria:**
  - operator can diff → select → apply in under 60 seconds for a typical template update
  - stale version is clearly surfaced and recoverable
  - keyboard-only users can complete the flow

#### Task 9.8A: Add EventSession Template Diff / Sync UX (**Milestone E**)
- Mirrors Task 9.8 for session scope at `Explore.Blazor.Client/Pages/Admin/EventSessionTemplateSync/`

#### Task 9.9: Add Exposure Governance UI Polish And Documentation Tooltips (**Milestone D**)
- Extends Task 9.7 with tooltip explanations for each exposure flag
- Reference doc link opens `docs/CUSTOM_PROPERTIES.md` in a new tab

#### Task 9.10: Update Organization And Group Pages To Remove Any Stale Metadata-Blob Assumptions
- Remove any generic "Metadata" section from Organization / Group edit pages
- Replace with the Custom Properties governance editor from Task 9.2

#### Task 9.11: Regenerate Generated API Client
- After Milestone D/E/F API contract changes, regenerate the NSwag client
- Verify generated client exposes new projection admin, template sync, and aggregate view endpoints
- Run `Explore.Blazor.Client` build + smoke tests to catch any breaking DTO shape changes

---

### Phase 10: Search, Projection, Moderation, Export, And Aggregate View Integration
**Effort: XXL**

#### Task 10.0: Integrate Layer 2 Sector Fields Directly Into Discovery And Governance Paths
- **Status:** architecturally already done (Milestones B/C confirm zero coupling between Layer 2 and Layer 3; `EventQuerySpecification` composes `IslamicAspectFilter`, `TechAspectFilter`, `AspectPresenceFilter` directly on Layer 2 aspect fields); this task is a **verification + documentation** pass
- **Acceptance Criteria:**
  - no new Layer 3 projection code reaches into Layer 2 columns
  - no Layer 2 field flows through `EventCustomPropertyProjection`
  - architecture test enforces the boundary
  - `docs/ARCHITECTURE.md` reflects the locked separation

#### Task 10.1: Populate Event Custom-Property Projections On Writes And Sync (**Milestone D baseline**)
- **Integration points (all inside the same transaction as the runtime write):**
  - `CreateEventCommandHandler` (on template instantiation - calls `RefreshForEventAsync` after materializing definitions/values)
  - `SetEventCustomPropertyValueCommandHandler`
  - `SetEventCustomPropertyMultiValuesCommandHandler`
  - `CreateEventCustomPropertyDefinitionCommandHandler`
  - `UpdateEventCustomPropertyDefinitionCommandHandler`
  - `DeleteEventCustomPropertyDefinitionCommandHandler` (calls `RemoveForDefinitionAsync`)
  - `ApplyEventTemplateSyncCommandHandler` (Milestone E) - calls `RefreshForEventAsync` post-sync
- **Acceptance Criteria:**
  - projection is always consistent with runtime after any commit
  - Testcontainers test proves: every runtime write yields matching projection state
  - rebuild from scratch yields byte-identical projection (modulo `UpdatedAt`)
  - projection updater respects advisory lock coordination during concurrent rebuild

#### Task 10.1A: Populate EventSession Custom-Property Projections On Writes And Sync (**Milestone D baseline**)
- Mirrors Task 10.1 for session scope across `CreateEventSessionCommandHandler`, session value commands, session def commands, session sync apply

#### Task 10.2: Integrate Filterable/Searchable Projections Into Discovery Query Paths (**Milestone D baseline**)
- **Integration:**
  - New `EventCustomPropertyProjectionFilter` specification composed into `EventQuerySpecification.And(...)` when the incoming request has custom-property filter parameters
  - New `EventCustomPropertySearchSpecification` composed when the incoming request has a text search query
  - Gated behind `custom_properties.projection_discovery_enabled` tenant setting (default `false` during rollout; flip to `true` per-tenant once projection baseline is proven)
- **Query cache:**
  - Projection-backed filters contribute to the discovery query cache key suffix so cached results stay coherent
- **Acceptance Criteria:**
  - custom-property filters return correct results against a seeded Testcontainers corpus
  - text search across `SearchToken` columns is correct and case-insensitive
  - existing Layer 1 + Layer 2 filters continue to work unchanged
  - flipping the tenant setting on toggles projection-backed filters without redeploy

#### Task 10.2A: Integrate Session Filterable/Searchable Projections Into Discovery Query Paths (**Milestone D baseline**)
- Mirrors Task 10.2 for `EventSessionQuerySpecification` (or equivalent)

#### Task 10.3: Integrate Exportable/Public Projections Into Publication / Export Paths (**Milestone D advanced**)
- **Integration:**
  - Export/publication payload composer reads `EventCustomPropertyProjection` rows where `ExposureLevel == Public` and `IsExportable == true` for a given event
  - Emits as `im.islamu.event.extension.v1` payload per Milestone F lexicon
- **Acceptance Criteria:**
  - no non-exportable, non-public custom property leaks into export payloads
  - moderation-relevant flags do not appear in export payloads unless the operator has an admin-level export context

#### Task 10.3A: Integrate Event-With-Sessions Aggregate Read/View Contracts Into Publication And Discovery Surfaces (**Milestone F**)
- **Integration:**
  - `GetEventWithSessionsAggregateViewQuery` composes Layer 1 + Layer 2 (module-gated) + Layer 3 projections (exposure-filtered) + session summaries + session projections
  - Public event pages read from this aggregate query
  - Admin event pages read from this aggregate query with a higher exposure ceiling
  - Federation/publication path reads from this aggregate query filtered to public-exportable exposure
- **Acceptance Criteria:**
  - aggregate view respects Layer 1/2/3 boundaries
  - exposure ceiling enforcement is tested across roles
  - no raw EAV scan in the hot path

#### Task 10.4: Integrate Moderation-Relevant Projections Into Governance Workflows (**Milestone D advanced**)
- Moderation queue reads `EventCustomPropertyProjection` rows where `IsModerationRelevant == true`
- Moderation actions update projection rows and optionally the underlying runtime

#### Task 10.5: Integrate Analytics-Relevant Projections Into Analytics Payload Composition (**Milestone D advanced**)
- Analytics pipeline reads `EventCustomPropertyProjection` rows where `IsAnalyticsRelevant == true`
- Per-tenant aggregation, no cross-tenant leakage

---

### Phase 11: Testing And Documentation
**Effort: XXL**

> **Testing stack:** xUnit (unit) + TUnit (new tests where appropriate) + Moq for mocks + Testcontainers-PostgreSQL for integration + Aspire AppHost for E2E. All integration tests that touch projections, sync, aggregate views, or tenant isolation MUST use Testcontainers for real PostgreSQL.

#### Task 11.1: Architecture Tests (**Milestone D onward**)
- Assert Layer 1/Layer 2/Layer 3 boundaries:
  - no Layer 3 type references any Layer 2 aspect type
  - no Layer 2 type references any Layer 3 custom-property type
  - `Explore.Application/Contracts/Services/IEventCustomPropertyProjectionUpdater` implementation lives in `Explore.Persistence`
  - `Explore.Application/Services/EventTemplateSyncService` contains no `DbContext` reference
  - projection entity only references `Explore.Domain` types
- Assert `IEventTemplateInstantiationService`, `IEventSessionTemplateInstantiationService`, `IEventTemplateSyncService`, `IEventSessionTemplateSyncService`, `IEventCustomPropertyProjectionUpdater`, `IEventSessionCustomPropertyProjectionUpdater` all live in `Explore.Application/Contracts/Services`
- Assert no Specification or Repository returns a DTO

#### Task 11.2: Unit Tests For Namespaced Key Uniqueness And DisplayName Renames
- Prove `CustomPropertyIdentity.Normalize("Platform", "Foo")` and `CustomPropertyIdentity.Normalize("platform", "FOO")` yield the same machine identity
- Prove a `DisplayName` rename does not break lookups by namespaced key
- Prove uniqueness is enforced by EF configuration (via Testcontainers integration test)

#### Task 11.3: Unit Tests For Multi-Value Semantics And Ordering
- Setting a multi-value property with 3 values yields 3 rows with Ordinals 0/1/2
- Replacing values preserves ordering semantics
- Single-value property rejects a second value (via service-level validation, since DB allows 1 row with Ordinal=0)
- Duplicate normalized values for the same definition and entity scope are rejected

#### Task 11.4: Unit Tests For Typed Validation Rules
- Text: MinLength/MaxLength/RegexPattern enforcement
- Number: MinNumber/MaxNumber enforcement
- DateTime: MinDateTime/MaxDateTime enforcement
- Url: AllowedUrlSchemes enforcement
- Manually instantiated validators (per project rule) - no DI in validator assertions

#### Task 11.5: Unit Tests For Exposure / Search / Filter / Export Flags
- Projection row populated with correct flags
- Discovery filter honors `IsFilterable = true` only
- Export payload composer honors `IsExportable = true` + `ExposureLevel = Public` only
- Moderation queue honors `IsModerationRelevant = true` only

#### Task 11.6: Unit Tests For Template Instantiation, Versioning, And Sync Provenance ✅ (Milestone B)

#### Task 11.6A: Unit Tests For EventSession Template Instantiation, Versioning, And Sync Provenance ✅ (Milestone C)

#### Task 11.7: Unit Tests For Retired Definitions / Options With Historical Values
- Retired definitions (`IsActive = false`) still allow reading historical values
- Retired options still allow reading historical values that reference them
- Sync does not hard-delete retired rows
- Rebuild does not hard-delete retired rows

#### Task 11.8: Integration Tests For Persistence Constraints And Tenant Isolation (**Milestone D**)
- Testcontainers PostgreSQL fixture
- Prove `(TenantId, EntityTypeName, Namespace, Key)` uniqueness constraints at DB level
- Prove `Tenant` named query filter isolates cross-tenant reads
- Prove `SoftDelete` named query filter hides soft-deleted rows
- Prove cascade delete from runtime value to projection row is correct

#### Task 11.8A: Integration Tests For EventSession Persistence Constraints And Tenant Isolation (**Milestone D**)
- Mirrors Task 11.8 for session scope

#### Task 11.8B: Integration Tests For Projection Updater Transactional Consistency (**Milestone D**)
- Every runtime write command is followed by a query that confirms projection state matches
- Rebuild from scratch yields byte-identical projection state
- Concurrent inline write + background rebuild do not corrupt either state
- Advisory lock coordination works as designed

#### Task 11.8C: Integration Tests For Template Sync Workflow (**Milestone E**)
- Diff empty → no changes
- Diff added-only → sync adds definitions
- Diff modified-only → sync modifies selected fields
- Diff retired-only → sync retires (IsActive=false) without data loss
- Diff mixed → sync applies selected subset
- Stale `baseProvenanceVersion` → 409 Conflict
- Concurrent sync → first wins
- Historical values preserved across sync
- Audit row written

#### Task 11.8D: Integration Tests For Aggregate Event-With-Sessions View (**Milestone F**)
- Keyless entity returns correct rows for a seeded corpus
- Exposure ceiling is enforced
- Layer 2 modules gated correctly (Islamic module disabled → aspect fields null)
- Session summaries reflect current session state
- Query performance acceptable for paged list shape

#### Task 11.9: Integration Tests For API Roundtrips (template → event → sync → projections) (**Milestone D/E**)
- Full HTTP integration test: create template, create event with template, edit runtime values, trigger projection rebuild, diff + apply sync, verify final state

#### Task 11.9A: API Integration Coverage For Shared Custom Property Definition Controller ✅ (Milestone A)

#### Task 11.9B: Integration Tests For API Roundtrips (event template → session blueprint → event session → sync → projections) (**Milestone D/E**)
- Mirrors Task 11.9 for session scope

#### Task 11.10: Update Documentation (**Milestone D/E/F**)
- `docs/DOMAIN.md` - Layer 1/2/3 model, entity shapes
- `docs/ARCHITECTURE.md` - projection layer, sync workflow, aggregate view
- `docs/EXTENSIBILITY.md` - custom property promotion rules (Rule 12), governance workflow
- `docs/MODULAR_EVENTS.md` - module gating interaction with Layer 2/3
- `docs/CUSTOM_PROPERTIES.md` - full operator-facing documentation
- `docs/API.md` - new projection admin + template sync endpoints
- `docs/SECURITY.md` - new authorization policies
- `docs/TROUBLESHOOTING.md` - projection rebuild playbook, sync conflict resolution

#### Task 11.10A: Update Lexicon Planning Docs (**Milestone F**)
- New `docs/LEXICONS.md` or extension to `docs/FEDERATION.md`
- Canonical NSID hierarchy
- Add-only evolution rules + NSID versioning discipline
- Lexicon-to-projection/aggregate-view mapping
- Experimental `.temp.` namespace usage guidelines

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| EAV grows into a semantic dumping ground | Medium | High | Rule 12 (Atlassian 4-question promotion framework) enforced in governance review + Phase 5.10 promotion playbook |
| Machine identity breaks due to mutable labels | High | High | namespaced `Key` replaces mutable `Name`; `CustomPropertyIdentity.Normalize` normalizes to lowercase machine identity |
| Multi-value behavior becomes inconsistent across API/UI | Medium | High | explicit Ordinal semantics + dedicated tests in Phase 11.3 |
| Validation devolves into a hidden DSL | Medium | High | typed governed validation model only; no `ValidationRules` string property anywhere in domain |
| Search/discovery queries become EAV-heavy and brittle | High | High | Milestone D projection baseline + tenant feature flag rollout + Layer 2 filters remain independent of Layer 3 projection |
| Template provenance becomes insufficient for support | Medium | High | versioned provenance fields + sync audit trail + source-id-first matching + historical preservation rules |
| Event/session scope drifts and sessions get collapsed into peer events | Medium | High | Rule 11 (locked) preserves parent/child aggregate; Milestone F aggregate view is explicitly a read-only keyless entity |
| Soft deletion causes historical data loss or confusion | Medium | High | explicit retirement rules + named `SoftDelete` query filter + historical retention tests (Phase 11.7) |
| Governance rules are too weak for public/searchable fields | Medium | High | exposure flags + authorization categories in Phase 8.7 + exposure ceiling enforcement in Phase 10.3A |
| Projection becomes stale under heavy write load | Medium | High | transactional live projection baseline (Rule 13) + advisory-lock-coordinated rebuild tooling + no eventual consistency unless justified |
| Template sync conflicts silently overwrite operator-local edits | Medium | High | Three-way merge rules (template / last-sync / current runtime) + `HasLocalChanges` warning in diff + operator-confirmed apply only |
| Sync workflow concurrency races produce inconsistent runtime state | Medium | High | `baseProvenanceVersion` optimistic concurrency check on apply + structured 409 Conflict response + Phase 11.8C integration tests |
| Projection rebuild corrupts state under concurrent inline writes | Medium | High | PostgreSQL advisory lock coordination + status tracking table + replay-safe rebuild contract + Phase 11.8B integration test |
| Aggregate view query becomes a performance hotspot | Medium | Medium | Keyless entity + parameterized query + B-tree projection indexes + pagination + query plan review in Phase 11.8D |
| Lexicon contracts drift from canonical entity shape | Medium | High | ATProto add-only discipline (Rule 14) + explicit NSID versioning + Phase 11.10A lexicon planning docs |
| Blazor dynamic form editor misrenders new PropertyType | Low | Medium | Switch on `PropertyType` enum + unit tests per type + explicit "unsupported type" fallback render with clear error |
| Accessibility regressions in dynamic form components | Medium | High | WCAG 2.2 AA requirements in Phase 9 + keyboard navigation tests + `aria-` attribute enforcement |
| NSwag client drift after API contract changes | High | Medium | Phase 9.11 regeneration + API integration tests after every milestone |
| Cross-tenant data leakage through projection | Low | Critical | explicit `tenant_id` column + `Tenant` named query filter on every projection table + defense-in-depth tests |
| Milestone D delivery sprawl (CTO review 2026-04-11) | High | High | Rule 17 ruthless sequencing + D1/D2/D3 internal sub-gates + no cross-sub-gate parallelization + each sub-gate has explicit exit criteria tested in Testcontainers |
| Dirty-scope backlog grows unbounded during sustained rebuild contention | Medium | High | `max_dirty_scope_pending_per_tenant` quota (Rule 16) + `eav_projection_dirty_scope_pending_total` Prometheus gauge + operator alarm threshold + `POST drain-dirty-scopes` self-service endpoint |
| Concurrency model drift (mixing business version and technical token) | Medium | High | Rule 15 locked taxonomy + forbidden-patterns list + dedicated unit tests separating `stale_sync_base` from `concurrent_update` branches |
| Over-generalized sync implementation ("schema merge engine") | Medium | High | CTO "keep it boring" guardrail on Task 3.5/3.5A + explicit hand-coded field comparisons + no `ITemplateDiffService<T,U>` generic + code review rejects generic abstractions |
| Tenant admins misconfigure into runaway EAV | High | Medium | Rule 16 hard limits + platform-maximum ceilings + setting registry validation + governance report surfacing high-cardinality tenants |
| Rule 12 (EAV promotion) not enforced in practice | High | Medium | Operational Governance Surface (admin endpoint + Blazor page) + `PromotionRecommendation` automated calculation + quarterly review runbook |
| Milestone F grows into a publication platform | Medium | High | Narrowed OUT scope explicitly rejects publication machinery + Milestone F deliverables locked to "one view + one doc" |
| Authorization taxonomy fractures into unmanageable policy set | Medium | Medium | Simplified to 4 policies at Phase 8.7 start + subdivision requires plan amendment + future-proof test |

---

## Success Metrics

### Architectural (locked)

1. All stale `MetadataJson` assumptions are removed from active runtime, API, UI, and planning surfaces.
2. Event runtime behavior uses only event-local instantiated/synced definitions and values.
3. EventSession runtime behavior uses only session-local instantiated/synced definitions and values.
4. Custom-property identity survives display-name changes and localization changes.
5. Multi-value semantics are consistent across storage, API, and UI.
6. Validation is enforced from typed metadata with no opaque rule blobs.
7. Searchable/filterable/exportable properties flow through projections, not raw EAV-only discovery queries.
8. Template version provenance and sync history are explainable in support scenarios.
9. Historical values remain readable after definition or option retirement.
10. Platform-owned and tenant-owned namespaced properties can coexist without collisions.
11. Sector-standard semantics are modeled through Layer 2 typed schema, not Layer 3 EAV rows.
12. Event and EventSession remain separate canonical resources while aggregate views can merge them for UX and federation-facing reads.

### Milestone D1 Exit (Projection Correctness)

13. Every Layer 3 runtime write transactionally updates the matching projection row.
14. Projection rebuild from scratch is byte-identical (modulo `UpdatedAt`) to live-maintained projection state.
15. Projection rebuild handles concurrent inline writes without corruption (advisory-lock coordination proven in Testcontainers).
16. **Dirty-scope drain proven**: rebuild in progress + concurrent inline write → dirty-scope row registered atomically with runtime write → rebuild completion drains the scope → projection reflects the concurrent write (Testcontainers integration test).
17. **Rebuild crash recovery proven**: rebuild interrupted mid-run + subsequent rebuild drains the remaining dirty scopes (Testcontainers integration test).
18. Projection tables carry `tenant_id` + `Tenant` named query filter; cross-tenant queries are proven impossible at the data-filter layer.
19. Read-after-write for custom-property edits is strongly consistent (no eventual consistency baseline).
20. Every mutable aggregate has `ConcurrencyStamp` + `IsConcurrencyToken()` + handler translates `DbUpdateConcurrencyException` to `concurrent_update` problem detail.
21. All Rule 16 hard limits are enforced in handlers with a unit test per quota (seed to `quota-1`, accept `quota`, reject `quota+1`).

### Milestone D2 Exit (Projection Operability)

22. Advisory-lock-coordinated rebuild worker runs successfully against a tenant with >10k events and concurrent inline writes.
23. `GET /admin/custom-property-projections/status` returns projection status for a tenant.
24. `GET /admin/custom-property-projections/dirty-scopes` returns pending dirty scopes.
25. `POST /admin/custom-property-projections/drain-dirty-scopes` drains independently of a full rebuild.
26. `GET /admin/custom-property-definitions/governance-report` returns deterministic `PromotionRecommendation` for seeded fixtures.
27. Prometheus metrics exposed: `eav_projection_rebuild_duration_seconds`, `eav_projection_rebuild_rows_processed_total`, `eav_projection_dirty_scope_pending_total`, `eav_projection_dirty_scope_drained_total`.
28. `docs/OPERATIONS.md` operator runbook published covering "what is broken / stale / rebuildable / how do I recover."

### Milestone D3 Exit (Projection Consumption)

29. Discovery query paths backed by projection return correct results under a seeded Testcontainers corpus.
30. Projection discovery rollout is tenant-gated via `custom_properties.projection_discovery_enabled` setting.
31. Query performance on seeded corpus: p95 <= existing Layer 1/Layer 2 filter query latency baseline (no regression).
32. `EventQuerySpecification` composition unchanged for Layer 1/Layer 2 paths (architecture test).
33. Blazor discovery UI exposes projection-backed filter surface behind the feature flag.

### Milestone E Exit (Explicit Sync)

34. Operator can diff a running event against any published template version in under 2 seconds for typical event sizes.
35. Apply sync is atomic, idempotent, and rejects stale provenance versions with RFC 7807 problem details.
36. Apply sync distinguishes `stale_sync_base` (business-level, Rule 15) from `concurrent_update` (technical-level, Rule 15) in separate problem detail types.
37. Historical values are always preserved across any sync operation.
38. Concurrent sync attempts are detected and only the first one wins.
39. Audit trail captures operator identity, plan hash, source version, target version, and applied counts.
40. Three-way conflict (template change + local runtime edit) is clearly surfaced in the diff UI.
41. Sync apply rejects plans exceeding `sync_apply_max_change_count` quota (Rule 16) before opening the transaction.
42. No generic `ITemplateDiffService<T,U>` abstraction exists. Explicit `EventTemplateDiffService` and `EventSessionTemplateDiffService` with hand-coded field comparisons (CTO "keep it boring" rule).

### Milestone F Exit (Aggregate Views + Lexicon)

43. `EventWithSessionsView` keyless entity returns correct aggregate rows in Testcontainers integration tests.
44. Aggregate view respects Layer 1/2/3 boundaries - no Layer 2 field routed through Layer 3 projection.
45. Exposure ceiling enforcement works across `Internal`, `OrganizerOnly`, `TenantAdminOnly`, `Public` in unit tests.
46. Lexicon planning docs are accepted and document NSID hierarchy + versioning + add-only evolution rules.
47. No raw EAV scan appears in the event discovery or event detail hot paths.
48. No publication machinery (ATProto PDS, bridgy-fed, ActivityPub) is introduced (Milestone F narrowed scope).

### Operational

49. Build is green: `dotnet build --configuration Release --verbosity quiet` produces 0 errors.
50. All test projects run individually per `CLAUDE.md` instructions and pass.
51. Architecture tests (52+) remain green.
52. No new warnings beyond the pre-existing 790-warning baseline.
53. Dev/active plan, context, and tasks files are kept in sync with actual repository state.
54. Operator runbook in `docs/OPERATIONS.md` covers "what is broken / stale / rebuildable / how do I recover" for every D/E/F surface.
55. Authorization policies collapsed to 4 core policies (`template_admin`, `event_editor`, `property_governance_admin`, `platform_namespace_editor`) at Milestone D/E exit - no policy explosion.

---

## Final Recommendation

Keep the normalized typed custom-property system and keep the event template-instantiation model.

But do **not** let raw EAV become:

- the only semantic home of important product concepts
- a stringly typed rules engine
- the hot query path for discovery
- or the only way support understands event state

The right implementation for this platform is:

- **Layer 1** universal core on `Event` and `EventSession`
- **Layer 2** typed sector profiles/aspects for domain-standard event and session semantics, with module gating at query time
- **Layer 3** EAV as a governed extension/configuration layer at both event and session scope, backed by the Milestone A/B/C delivery
- **namespaced machine keys** (`Namespace + Key`) normalized to lowercase, distinct from mutable `DisplayName`
- **typed validation** with no opaque DSL
- **explicit exposure and governance semantics** (ExposureLevel + IsSearchable + IsFilterable + IsExportable + IsModerationRelevant + IsAnalyticsRelevant + IsSystemOwned)
- **event template instantiation + session blueprint instantiation** (Rule B, done) plus **operator-confirmed versioned sync** (Rule A, Milestone E)
- **transactional (live) projection-backed discovery/search/export/moderation reads** (Milestone D baseline), with advisory-lock-coordinated rebuild tooling, normalized projection tables, and tenant-gated rollout
- **keyless-entity aggregate event-with-sessions view contracts** for UX and federation/publication (Milestone F), composed from Layer 1 + module-gated Layer 2 + exposure-filtered Layer 3 projection
- **ATProto-disciplined lexicon planning** (add-only evolution, NSID versioning, `.temp.` namespace for experimentation, canonical vs aggregate-view lexicons kept separate)
- **Rule 12 EAV promotion framework** (Atlassian 4-question) applied quarterly to keep Layer 3 lean and push sector-standard or discovery-critical attributes toward Layer 1 or Layer 2

### CTO Greenlight Conditions (must all be true before Milestone D implementation starts)

Per the CTO architecture review 2026-04-11, implementation cannot begin until all three of the following are locked in this plan:

1. **Dirty-scope recovery mechanism** for skipped inline projection updates during rebuild → **LOCKED** (new section `Dirty-Scope Recovery Mechanism` + new Task 3.6C + integration tests in Phase 11.8B)
2. **Internal Milestone D sub-gate split** into D1 (correctness), D2 (operability), D3 (consumption) with explicit sequencing → **LOCKED** (Milestone D section table + Rule 17 ruthless sequencing)
3. **Explicit technical concurrency strategy** locked across templates, runtime definitions, and sync workflows → **LOCKED** (Rule 15 + `Concurrency And Versioning Rules` section)

Additional tightenings incorporated (all locked in this plan):

- **Keep sync implementation boring** → Task 3.5/3.5A explicit hand-coded field comparisons, no generic abstractions
- **Operational governance surface for Rule 12** → `GetCustomPropertyGovernanceReportQuery` + `GET /admin/custom-property-definitions/governance-report` + Blazor admin page
- **Hard limits and quotas** → Rule 16 + new section `Hard Limits And Quotas` with 10 concrete ceilings
- **Simplified authorization taxonomy** → Phase 8.7 collapsed from 7 to 4 core policies
- **Narrowed Milestone F** → "one view + one doc" only, publication machinery explicitly OUT
- **Ruthless milestone sequencing** → Rule 17 locked: D1 → D2 → D3 → E → F
- **Repairability first-class** → every new entity/service/endpoint must answer "what is broken / stale / rebuildable / how do I recover" in `docs/OPERATIONS.md` before exiting its gate

**Verdict**: With all three required items and all additional tightenings locked above, the plan is approved to proceed to Milestone D1 (projection correctness) implementation.

That is the enterprise-grade, self-hostable, multi-tenant direction this plan implements now. Milestones A, B, C are done. Milestones D, E, F are planned with concrete research-backed acceptance criteria, clean-architecture-compliant placement, PostgreSQL-specific tuning, CQRS + MediatR + EF Core 10 pattern alignment, MudBlazor v9 UI + WCAG 2.2 AA accessibility, and Testcontainers-backed integration tests for every non-trivial surface.
