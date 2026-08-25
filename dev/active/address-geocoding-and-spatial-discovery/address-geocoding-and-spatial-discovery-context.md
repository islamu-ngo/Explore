<!-- ABOUTME: Active resume context for governed address acquisition and its spatial-discovery handoff. -->
<!-- ABOUTME: Records current status, decisions, blockers, evidence changes, validation, and the next task. -->

# Address Geocoding And Spatial Discovery - Context

Last Updated: 2026-08-25 Europe/Brussels

**I-VSD:** [I-VSD Address Geocoding And Spatial Discovery](../../../islamic-value-sensitive-design/i-vsd-address-geocoding-and-spatial-discovery.md)

## SESSION PROGRESS (2026-08-25 Europe/Brussels)

### COMPLETED

- Reclassified the workstream as Tier 2 Privacy with Tier 1 security/migration boundaries.
- Re-read the implementation-plan and Senior CTO quality gates, I-VSD/grill-me guardrails, clean-room rules, relevant path rules, and the composite intent contracts.
- Audited the current plan/context/tasks against repository reality and official documentation.
- Used `web_search` as requested. The configured search provider failed for two framework queries and returned the official PostGIS result for `ST_DWithin`; direct official-document fetches completed the evidence set.
- Requested Context7 through a documentation librarian. Context7 was not available, so no Context7 result is claimed.
- Re-baselined the current code facts:
  - Location write DTOs do not contain tenant identity; tenant is already controller/context-owned.
  - Raw coordinates remain in Location CRUD, nested Event creation, and AI event-draft creation.
  - Location create still uses uncharacterized AutoMapper construction.
  - Current location dialogs exist and expose raw coordinates; Edit also owns private-home consent.
  - Location admin actions remain unconditional rather than HAL-gated.
  - All five primary migration heads are newer than the August 22 location contraction and ended at `AddAdmissionIssuancePersistence` when inspected.
  - Home Discovery remains active, area-only, and the only valid owner of future PostGIS execution.
- Applied an independent Senior CTO audit. Its critical corrections were incorporated:
  - reduce address work to four phases;
  - move PostGIS execution to Home Discovery Phase 6;
  - contract every coordinate-bearing write path;
  - quarantine legacy rows rather than guessing provenance;
  - make protected selections tenant/actor/target/concurrency bound;
  - prohibit Private Home tenant-wide autocomplete;
  - ship local-only API/UI before Photon;
  - keep concrete provider policy outside Application.
- Rewrote the architectural plan with strict plan/tasks/context separation.
- Created the linked I-VSD assessment requirement and synchronized its provider-responsibility decisions into the workstream.

### IN PROGRESS

- Planning artifacts are being finalized and validated. Runtime implementation has not started.

### NEXT

1. User reviews the re-baselined plan and split decision.
2. After approval, start Task 1.1 Red Phase: lock every coordinate-bearing write contract and aggregate invariant before production edits.
3. Complete PR A before local-only API/UI or Photon work.
4. Do not implement exact spatial discovery from this ledger.

### BLOCKERS

- **Implementation approval:** The rewritten plan is ready for user correction/approval; no runtime work is authorized by this planning request.
- **Photon activation:** Phase 4 requires approved endpoint ownership, regional/planet data footprint, capacity, update/swap, support, recovery, terms, attribution, and production endpoint evidence.
- **Spatial activation:** ADR-013 remains `Proposed`. Exact discovery stays with Home Discovery Phase 6 and requires a named authorized decider/date plus the revised per-EventLocation disclosure and installed-disabled lifecycle design.
- **Context7:** No Context7 MCP tool was available. Revalidate package/API details there if it becomes available; otherwise retain the official-doc substitution record.

## Quick Resume

1. Read this context and `address-geocoding-and-spatial-discovery-tasks.md`.
2. Read only the current phase and referenced decisions from the plan.
3. Start from the first unchecked Red task unless the user overrides it.
4. Keep `tasks.md` as the sole hot execution ledger.
5. Preserve unrelated worktree changes and never hand-edit generated artifacts.

## Current Outcome

This workstream now owns address integrity and acquisition only:

- PR A: all write-contract contraction, aggregate invariants, conservative source/visibility persistence, local-only query and moderation.
- PR B: private local-only API/HAL/BFF/Blazor vertical slice.
- PR C: optional Photon adapter and protected selections.

Exact PostGIS discovery remains a documented dependency/handoff to `dev/active/home-discovery-experience/` Phase 6. Maps and alternative providers are separate future work.

## Key Files And Responsibilities

| Path | Status | Responsibility |
|---|---|---|
| `src/Explore.Domain/Location.cs` | Existing | Privacy/ownership aggregate; future explicit manual/provider address transitions |
| `src/Explore.Domain/LocationPii.cs` | Existing | Sole exact address and coordinate store |
| `src/Explore.Application/DTOs/Location/*` | Existing/modify | Remove raw coordinate write members |
| `src/Explore.Application/DTOs/Event/CreateEventLocationDto.cs` | Existing/modify | Remove nested event raw coordinate authority |
| `src/Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs` | Existing/modify | Route nested locations through governed address transition |
| `src/Explore.Application/Features/AiAssistant/Actions/CreateEventDraftAiAction*` | Existing/modify | Remove AI/model coordinate authority |
| `src/Explore.Application/Features/Locations/*` | Existing/modify | Trusted tenant checks, explicit construction, policy/protected selection |
| `src/Explore.Application/Contracts/Geocoding/` | New | Semantic provider-neutral contracts only |
| `src/Explore.Application/Contracts/Persistence/ILocalAddressSuggestionQuery.cs` | New | Bounded visibility-filtered local query port |
| `src/Explore.Persistence/**` | Existing/new | Source/visibility schema, generated migrations, local query implementation |
| `src/Explore.Infrastructure/Geocoding/**` | New in Phase 4 | Photon transport, resilience, token protection, configuration |
| `src/Explore.API/Controllers/**Address**Controller.cs` | New | Capability-partitioned private address routes |
| `src/Event.Web.BffHosting/Proxy/EventApiProxyExtensions.cs` | Existing | Reused `/api/*` trust/antiforgery boundary; no product edit expected |
| `src/Explore.Blazor.Client/Pages/Admin/Dialogs/*LocationDialog.razor` | Existing/modify | Remove coordinates, preserve private-home consent, integrate local/provider selection |
| `src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor` | Existing/modify | Gate location actions by HAL |
| `docs/adr/ADR-013-postgis-proximity-discovery.md` | Existing/Proposed | Spatial decision; not activated here |
| `dev/active/home-discovery-experience/` | Existing/active | Sole future PostGIS execution ledger |
| `islamic-value-sensitive-design/i-vsd-address-geocoding-and-spatial-discovery.md` | New | Provider responsibility, stakeholders, risks, mitigations and evidence gaps |

## Key Decisions

1. Contract raw coordinates from all write paths, not authorized read/disclosure contracts.
2. Preserve tenant context authority and fail closed on internal request/context mismatch.
3. Use explicit aggregate manual/provider transitions; manual changes clear coordinates.
4. Persist source separately from visibility.
5. Migrate existing rows to `UnknownLegacy` plus `Quarantined`; never guess or widen.
6. Private Homes can never become tenant-wide autocomplete suggestions.
7. Ship local-only address acquisition before Photon.
8. Keep concrete provider configuration/registration in Infrastructure/host composition.
9. Keep exact provider coordinates inside least-privilege time-limited tokens.
10. Bind update tokens to tenant, actor, target Location and concurrency.
11. Reuse existing YARP BFF and prove the boundary; add no geocoding BFF endpoint.
12. Implement PII-free `provider=none` metrics in the local-only Phase 3; Photon extends the same bounded contract later.
13. Home Discovery Phase 6 exclusively owns PostGIS, per-EventLocation disclosure eligibility, and `Absent`/`InstalledDisabled`/`Serving` lifecycle.
14. Maps and alternative providers are separate workstreams.

## Constraints To Remember

- Repositories return entities; query ports return bounded Application-owned results.
- Validators are manually instantiated.
- Domain/Application remain free of provider/HTTP/EF/spatial types.
- HAL links are the only UI resource-affordance source.
- No address/query/coordinate/token/provider payload in telemetry, URL, cache, ProblemDetails, or health data.
- `Geocoding:Provider=None` is healthy and registers no outbound client.
- External provider calls occur before database transactions.
- Migrations/snapshots/OpenAPI/client are regenerated, never edited.
- No backward-compatibility readers, aliases, dual contracts, or approximate exact-distance fallback.
- Every new file starts with two `ABOUTME:` lines.

## Validation Baseline

- This turn changes planning/I-VSD Markdown only. Repository rules prohibit .NET build/test execution for documentation-only changes.
- The historical 2026-08-12 Release build is not current evidence and is no longer presented as the baseline.
- Required planning verification: scoped `git diff --check`, link/path checks, task/phase parity, Red-before-Green ordering, no plan checkboxes/granular task blocks, and no unintended product-code changes.
- Implementation phase gates are defined in the plan/tasks and have not run.

## Current Risks / Unknowns

- Phase 1 must prove the full write-contract inventory remains complete as source changes.
- Phase 2 must add truthful migration state without making quarantined/private rows reusable.
- Real query execution is proven for PostgreSQL/SQLite; other providers require a real lane before claiming runtime translation parity.
- Phase 3 must prove existing BFF transforms/antiforgery with targeted integration evidence.
- Phase 4 still needs operator topology and clean-room/license approval.
- Spatial handoff must be reconciled into Home Discovery before ADR-013 acceptance; this ledger must not duplicate it.

## Unrelated Worktree Guidance

- `islamic-value-sensitive-design/i-vsd-records-adoption.md` was already modified by another contributor/session. Do not revert, reformat, or include it.
- Planning changes are limited to this workstream's three files plus the new mapped I-VSD report.

## Handoff Notes

### Handoff - 2026-08-25 Europe/Brussels

- **Current state:** Re-baselined planning only; 0 implementation tasks complete.
- **Next action:** User reviews the split and Phase 1 contract/invariant scope.
- **Blockers:** Implementation approval; Photon operations gate; ADR-013/Home Discovery spatial gate; Context7 unavailable.
- **Modified paths:** Three workstream planning files and the mapped I-VSD report only.
- **Validation:** Markdown/link/triad checks are required before handoff; no .NET build/test is permitted for this docs-only change.
- **Risks:** Coordinate-write inventory drift, tenant/private-address leakage, legacy visibility widening, token replay, PII telemetry, and duplicate spatial ownership.
- **Notes:** Start with Red tests. Do not start provider or spatial work early.
