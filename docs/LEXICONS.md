ABOUTME: Canonical decision document for ATProto-compatible NSID lexicon hierarchy and evolution.
ABOUTME: Covers EAV extensions, sector profiles, and aggregate read models.

# Lexicon Decisions and NSID Hierarchy

Last Updated: 2026-04-24

## Purpose

This document defines the canonical NSID (Namespaced Identifier) hierarchy used to represent the platform's multi-layered event data model in an ATProto-compatible format. These lexicons provide stable semantic keys for data projections, aggregate read models, and interoperability surfaces.

The NSID system serves as the source of truth for:
- Mapping runtime EAV custom properties to stable machine-readable keys.
- Defining the boundaries between universal core fields, typed sector profiles, and local extensions.
- Guiding the evolution of data schemas using add-only discipline.

**Explicitly Out of Scope:**
- ATProto PDS (Personal Data Server) publication.
- bridgy-fed wiring or ActivityPub federation.
- Outbox or publication machinery.
- Cross-tenant aggregate views.
- Materialized views (rejected in favor of live projections and keyless entities).
- `$extensions` ATProto pattern (not yet standardized upstream).
- Lexicon code generation pipelines.

## Canonical NSID Hierarchy

The platform uses a 3-layer architecture represented by the following NSID taxonomy:

| NSID | Layer | Domain Entity | Status |
|---|---|---|---|
| `im.islamu.event.core.v1` | 1 | `Event` | Stable |
| `im.islamu.eventsession.core.v1` | 1 | `EventSession` | Stable |
| `im.islamu.event.islamic.v1` | 2 | `EventIslamicAspect` | Stable |
| `im.islamu.event.tech.v1` | 2 | `EventTechAspect` | Stable |
| `im.islamu.eventsession.islamic.v1` | 2 | `EventSessionIslamicAspect` | Stable |
| `im.islamu.event.extension.v1` | 3 | `EventCustomPropertyProjection` | Stable |
| `im.islamu.eventsession.extension.v1` | 3 | `EventSessionCustomPropertyProjection` | Stable |
| `im.islamu.event.withSessions.v1` | Agg | `EventWithSessionsView` | Stable |
| `im.islamu.event.temp.*` | 0 | (Various) | Experimental |

### Layer Descriptions

1.  **Layer 1 (Core)**: Universal fields shared by all events and sessions (e.g., Title, StartTime, Organizer).
2.  **Layer 2 (Sector Profiles)**: First-class typed schema for domain-specific aspects (e.g., Islamic madhab, Tech skill levels).
3.  **Layer 3 (Extensions)**: Local tenant-specific custom properties projected into a stable facet format.
4.  **Aggregate View**: A composed read model merging core, sector, and extension data into a single projection.

## Evolution Rules (Add-Only Lexicon Evolution)

To maintain backward compatibility and interoperability, the platform enforces "Add-Only" evolution for stable lexicons (Rule 14):

1.  **Immutability of Constraints**: NEVER tighten existing field constraints (e.g., making an optional field required or shrinking a maximum length) within the same major-version NSID.
2.  **Safe Additions**: New optional fields may be added to an existing NSID at any time.
3.  **Breaking Changes**: Any breaking change (field deletion, renaming, or constraint tightening) requires a new major-version NSID (e.g., `im.islamu.event.core.v2`). The previous version must remain available and frozen.
4.  **Experimental Promotion**: Experimental lexicons in the `im.islamu.event.temp.*` namespace must be promoted to a stable versioned NSID before being relied upon by production discovery paths.
5.  **Field Retirement**: Fields are retired by adding an `IsRetired` or `is_deprecated` flag. Physical deletion is forbidden to preserve historical data provenance.

## NSID Versioning Discipline

Decisions to version an NSID follow this classification tree:

- **Is the change additive?** (New optional field) → Update existing NSID version (revision only).
- **Is the change breaking?** (Renaming, tightening, deletion) → Create new major version NSID.
- **Rollout Control**: Use feature flags (e.g., `custom_properties.projection_discovery_enabled`) to control the adoption of new lexicon projections per tenant.
- **Safety Rails**: Quota settings (Rule 16) prevent extension abuse and ensure performance stability.

## Lexicon to Projection / Aggregate-View Mapping

| NSID | Domain Entity | EF Configuration | Projection Table | DTO | CQRS Query |
|---|---|---|---|---|---|
| `im.islamu.event.core.v1` | `Event` | `EventConfiguration` | `events` | `EventDto` | `GetEventDetailQuery` |
| `im.islamu.eventsession.core.v1` | `EventSession` | `EventSessionConfiguration` | `event_sessions` | `EventSessionDto` | `GetEventSessionDetailQuery` |
| `im.islamu.event.islamic.v1` | `EventIslamicAspect` | `EventIslamicAspectConfiguration` | `event_islamic_aspects` | `EventIslamicAspectDto` | (Part of Event query) |
| `im.islamu.event.tech.v1` | `EventTechAspect` | `EventTechAspectConfiguration` | `event_tech_aspects` | `EventTechAspectDto` | (Part of Event query) |
| `im.islamu.event.extension.v1` | `EventCustomPropertyProjection` | `EventCustomPropertyProjectionConfiguration` | `event_custom_property_projections` | `EventCustomPropertyProjectionDto` | `GetEventListQuery` |
| `im.islamu.event.withSessions.v1` | `EventWithSessionsView` | `EventWithSessionsViewConfiguration` | `vw_event_with_sessions` | `EventWithSessionsViewDto` | `GetEventWithSessionsAggregateViewQuery` |

## Promotion Criteria (Atlassian 4 Questions)

Layer 3 custom properties earn promotion to Layer 2 or Layer 1 based on the Atlassian 4-Question Framework (Rule 12). Promotion is considered if 2 or more answers are **Yes**:

1.  **Cross-tenant reporting required?**: Is the attribute aggregated or reported across multiple tenants?
2.  **Automation / AI consumer required?**: Do automated systems (LLMs, recommendation engines) depend on this field?
3.  **Search / filter affordance needed?**: Is the field required for discovery filters or search facets on public surfaces?
4.  **Long-term stability intended?**: Is the attribute expected to remain semantically stable for ≥ 2 years?

**Promotion Procedure**:
- Candidates are identified via the property governance admin surface.
- Promoted fields are implemented as first-class typed columns in Layer 2 aspects or Layer 1 core.
- Data migration is performed from EAV storage to the new typed home.

## Experimental (`im.islamu.event.temp.*`) Namespace Guidelines

The `temp.*` namespace is reserved for rapid iteration and unstable schemas:

- **Registration**: Register a temp lexicon in `dev/active/` documentation before use.
- **Expiration**: Temp lexicons should expire or be promoted to stable within 6 months.
- **Graduation Checklist**:
  - Semantic stability achieved.
  - Test coverage complete (Unit + Integration).
  - Documentation updated in this registry.
  - Versioned rename to stable NSID.
- **Isolation**: NEVER deploy `temp.*` lexicons to public federation channels or external publication targets.

## References

- [AGENTS.md](../AGENTS.md) — agent contract.
- [QUICK_REFERENCE.md](QUICK_REFERENCE.md) — hard invariants.
- [GOVERNANCE.md](GOVERNANCE.md) — governance + decision frameworks.
- [ARCHITECTURE.md](ARCHITECTURE.md) — layering + request flow.
- `dev/active/eav-custom-properties/` — EAV plan and context.
- [ATProto NSID Specification](https://atproto.com/specs/nsid) — upstream format definition.
