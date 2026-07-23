<!-- ABOUTME: Resume context for the re-baselined partial-update and settings-autosave workstream. -->
<!-- ABOUTME: Records current status, key decisions, source anchors, risks, and next action. -->

# Full Property Update Sub-DTO Pattern - Context

Last Updated: 2026-07-23 Europe/Brussels

## SESSION PROGRESS (2026-07-23 Europe/Brussels)

### COMPLETED

- Re-baselined the stale `dev/next/full-property-update-sub-dto/` workstream around remaining repository reality.
- Confirmed Event and EventSession as the canonical grouped partial-update implementations.
- Confirmed current scale: 59 `Update*Dto.cs` files, 71 matching update command-handler files, 104 public PUT/PATCH endpoints across 53 controllers, and 46 update-named tests.
- Confirmed tenant one-key/category settings writes already exist and should be reused for autosave.
- Confirmed instance modules/event/organization policies still use broad full-DTO `PUT` writes.
- Confirmed eight legacy tenant policy switches use local binding while ATProto controls already use callback-based autosave.
- Recorded the explicit no-backward-compatibility decision and action/provider/secret exclusions.
- Corrected the family-level re-baseline after user feedback: the inventory now has one row for every DTO file, handler file, and public PUT/PATCH endpoint.
- Assigned every row to one of 20 implementation tasks with no unclassified or deferred current surface.
- Explicitly assigned unsafe generic writes for `IndexedDid`, `SyncState`, `ActorKeyStore`, and `UserExternalLogin` to removal rather than leaving provider-owned state outside the plan.
- Independent comparison against repository globs and controller attributes confirmed exact 59/59 DTO, 71/71 handler-file, and 104/104 endpoint membership with no missing, duplicate, or stale row.
- Reviewed every action, exact-replacement, and removal exception against source semantics; reclassified ActorSubscription notification level as a grouped PATCH migration, then obtained a clean semantic pass.
- The completed route audit identified EventSessionSpeaker's nested two-ID PATCH as noncanonical; it is now an explicit Task 3.4 migration to one authoritative route ID.
- Completed Task 1.1: hardened exact tenant setting writes, added directional tenant-delegation authorization, autosaved all eight tenant policy switches by registered key, gated writes through HAL/metadata, and fenced broad writes while an exact write is pending.
- Verified Task 1.1 with 115 focused backend tests, 41 focused Blazor tests, and a Release build with 0 errors.
- Recorded the user's explicit waiver of runtime visual QA after Chrome DevTools MCP was unavailable; component interaction coverage remains green, but no browser visual pass is claimed.
- Moved H-019/A-003 deletion to Task 5.3 because the broad onboarding write still serves non-policy announcement-bar callers; this is removal sequencing, not backward compatibility.

### IN PROGRESS

- Task 1.2 tenant storage, branding document, and footer settings grouped PATCH/autosave migration.

### NEXT

1. Implement Task 1.2 rows H-044/H-061 and A-054/A-068/A-075 without overwriting sibling settings.
2. Complete Task 1.3 for every instance settings sub-resource and onboarding duplicate.
3. Keep H-019/A-003 until Task 5.3, then remove them after their remaining callers have explicit save boundaries.
4. Keep unrelated dirty worktree changes untouched.

### BLOCKERS

- No technical blocker.
- Runtime visual QA is intentionally skipped by explicit user direction; do not report a browser visual pass for Task 1.1.

## Quick Resume

1. Read this file and `full-property-update-sub-dto-tasks.md`.
2. Read only the current phase, decisions, and affected rows in the plan/inventory.
3. Start from the first unchecked task after user approval.
4. Update tasks immediately after substantial completion; update context only at a meaningful boundary.

## Key Files And Responsibilities

| Path | State | Responsibility | Notes |
|---|---|---|---|
| `src/Explore.Application/DTOs/Event/UpdateEventDto.cs` | Existing | Canonical grouped entity PATCH DTO. | Route owns ID; `OptionalUpdate<T>` owns explicit clear. |
| `src/Explore.Application/Features/Events/Handlers/Commands/UpdateEventCommandHandler.cs` | Existing | Canonical explicit apply/save/cache flow. | Reference for ordinary aggregate updates. |
| `src/Explore.Application/DTOs/EventSession/UpdateEventSessionDto.cs` | Existing | Canonical relationship/transactional grouped DTO. | Reference for related state and projections. |
| `src/Explore.Application/Features/EventSessions/Handlers/Commands/UpdateEventSessionCommandHandler.cs` | Existing | Relationship validation, concurrency, UoW, cache invalidation. | Reference for coupled groups. |
| `src/Explore.API/Controllers/SettingsController.cs` | Existing | Hierarchical user/tenant/instance key and category writes. | Reuse exact-key tenant write for independent controls. |
| `src/Explore.API/Controllers/InstanceSettingsController.cs` | Existing | Focused instance sub-resources. | Modules/events/organizations still accept full read DTOs through PUT. |
| `src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantPoliciesSection.razor` | Existing | Tenant policy UI and ATProto autosave reference. | Eight legacy switches still rely on later broad Save. |
| `src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantAdminSettingsLayout.razor` | Existing | Tenant settings orchestration. | Broad bottom Save is removed only after every section is classified. |
| `src/Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor` | Existing | Instance settings orchestration. | Convert ordinary sections to partial autosave; retain explicit sensitive actions. |
| `src/Explore.Blazor.Client/Services/TenantPublicExperienceAdminService.cs` | Existing | Tenant settings client path. | Reuse if it already exposes exact-key write; otherwise use current generated settings client. |
| `src/Explore.Blazor.Client/Services/InstanceOnboardingService.cs` | Existing | Instance settings client path. | Change methods to partial write DTOs with generated PATCH operations. |
| `tests/Explore.Blazor.Client.Tests/Pages/Admin/TenantPoliciesSectionTests.cs` | Existing | Tenant policy interaction coverage. | Add immediate save, lock, pending, and failure recovery cases. |
| `tests/Event.Application.UnitTests/Features/InstanceOnboarding/Commands/UpdateInstanceGovernanceSettingsCommandHandlerTests.cs` | Existing | Instance governance handler coverage. | Add omitted-group and one-group update cases. |
| `dev/active/full-property-update-sub-dto/full-property-update-sub-dto-inventory.md` | Updated | Normative exhaustive scope register. | D-001-D-059, H-001-H-071, and A-001-A-104 must all reach final state. |

## Key Decisions

- Entity partial updates use route-ID `PATCH`, nullable logical groups, and `OptionalUpdate<T>` for clearable fields.
- Exact-key setting `PUT` remains because the key value is completely replaced; this is not a compatibility exception.
- Tenant policy autosave reuses existing setting hierarchy/lock/audit behavior.
- Instance policy writes receive dedicated partial request DTOs; read DTOs stop serving as write contracts.
- Switches/selects save immediately; text saves on blur/debounce; sensitive/coupled/destructive operations remain explicit.
- No generic autosave framework or generic patch engine.
- No backward-compatible routes, DTOs, overloads, or tests.
- HAL and server lock state remain the UI authority.
- The exhaustive registers are normative: an implementation task cannot skip an owned row, and a newly discovered update surface blocks completion.
- All ordinary entity/settings properties migrate to grouped PATCH; only individually listed exact replacements and actions retain PUT/PATCH action semantics.
- Generic public writes for provider-owned key/index/cursor/identity state are removed rather than standardized as unsafe PATCH.

## Constraints And Rules To Remember

- Repositories return entities; handlers own mapping.
- Validators are manually instantiated.
- Tenant/resource identity comes from trusted context and route values.
- Authorization and validation complete before mutation.
- Multi-repository writes use existing `IUnitOfWork`.
- Cache invalidation follows successful save only.
- Do not log setting values, secrets, PII, or provider data.
- New/touched source files require two `ABOUTME` lines.
- Do not touch unrelated dirty files.

## Validation Baseline

Each phase runs once after all phase tasks:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project <phase-selected-project>.csproj --configuration Release --verbosity quiet
```

Planning-only verification uses `git diff --check` for this active directory. The baseline Release build was previously reported green with 0 errors and existing warnings; do not rerun it for this docs-only re-baseline.

## Current Known Risks / Unknowns

- The current 59/71/104 scans have no unclassified row. Task 6.2 must enforce that exact coverage and catch new rows.
- Active unrelated changes touch admin layouts and generated contracts. Implementation agents must reread affected files and merge with current work rather than overwrite it.
- H-019/A-003 remain until Task 5.3 because announcement-bar writes still depend on them; removing them earlier would break a live non-policy caller.
- Instance sub-resource concurrency may rely on existing policy-set/version behavior; Task 1.3 must preserve it rather than introduce a new token blindly.

## Handoff Notes

### Handoff - 2026-07-23 Europe/Brussels

- **Current state:** Task 1.1 implementation is complete; Task 1.2 is active. H-019/A-003 deletion is reassigned to Task 5.3 after remaining broad callers migrate.
- **Next action:** Implement Task 1.2 tenant storage, branding document, and footer settings grouped PATCH/autosave contracts.
- **Blockers:** None. Runtime visual QA was explicitly waived by the user after Chrome DevTools MCP was unavailable.
- **Modified files:** Task 1.1 touched settings authorization/handlers/controllers, tenant policy Blazor/service/tests, generated client output, and these active workstream artifacts.
- **Validation:** Focused backend tests passed 115/115; focused Blazor tests passed 41/41; Release build passed with 0 errors. No browser visual pass is claimed.
- **Documentation impact:** Public contract removals and generated-client changes continue per task; H-019/A-003 remain documented for Task 5.3 removal.
- **Risks:** Concurrent unrelated work already touches tenant/instance admin layouts and generated clients; all affected files must be reread immediately before implementation edits.
- **Notes for next contributor/agent:** Never revert unrelated changes. Re-read a file immediately before editing if it is already dirty.
