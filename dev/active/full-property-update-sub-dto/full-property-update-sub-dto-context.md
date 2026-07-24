<!-- ABOUTME: Resume context for the re-baselined partial-update and settings-autosave workstream. -->
<!-- ABOUTME: Records current status, key decisions, source anchors, risks, and next action. -->

# Full Property Update Sub-DTO Pattern - Context

Last Updated: 2026-07-24 Europe/Brussels

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
- Completed Task 1.2: replaced tenant storage, branding document, and footer settings PUT operations with presence-aware grouped PATCH contracts and regenerated OpenAPI/NSwag clients without compatibility methods.
- Tenant storage validates a merged effective candidate but transactionally persists only supplied Policy/S3 leaves, preventing inherited values from becoming tenant overrides; cache invalidation follows commit.
- Tenant branding serializes autosaves around the returned concurrency stamp, preserves omitted DisplayName/asset leaves, supports explicit clear, enforces per-field governance atomically, and returns the authoritative reloaded HAL document.
- Tenant footer now exposes an authenticated HAL admin resource, grouped scalar autosave, typed social links, and a permission-bound `manage-link-groups` capability; a concrete server guard enforces the effective link-group governance lock for all seven direct mutation handlers.
- Storage, branding, and footer Blazor sections own accessible local pending/saved/error state; immediate controls save directly, text/numeric/social fields use bounded debounce with blur flush, and parent broad Save no longer owns storage/branding.
- Updated API changelog/reference, storage, and footer documentation; regenerated the canonical API contract inventory.
- Verified Task 1.2 with 2,999/2,999 Application unit tests, all focused API/Blazor tests, 299/299 architecture tests with one governed skip, and a canonical Release build with 0 errors.
- Five-lane post-implementation review found and resolved three Task 1.2 blockers: branding persistence races now translate through `IUnitOfWork` to HTTP 409, incompatible persisted branding JSON fails closed without mutation/invalidation, and S3 credentials use an explicit coupled rotation action instead of ordinary autosave; the stale tenant-storage PUT test fixture now advertises PATCH.
- Goal, executable QA, code-quality, security, and context-mining rechecks all returned PASS with no remaining Task 1.2 blockers.
- Completed Task 1.3: regenerated instance PATCH contracts are mapped by `InstanceOnboardingService`, storage uses complete Policy/S3 groups, and auth/authz onboarding duplicates route through the canonical update operations.
- Ordinary instance module, governance, branding, domain, analytics, and footer controls now send sparse updates with accessible pending/saved/error state; failed callbacks reload only the authoritative sub-resource. Text fields save on blur, while switches/selects save immediately.
- Storage, SMTP, AI provider/credential, authentication, and authorization groups remain explicit Save/test/action flows; the parent broad Save no longer owns ordinary autosaved sections.
- Task 1.3 post-review fixes removed the legacy H-006 monolith, preserve redacted S3 credentials, reload authoritative auth/authz and ordinary section models after failed writes, and prevent SMTP/AI read contracts from returning persisted secret-shaped fields. OpenAPI and the NSwag client were regenerated, and the breaking pre-v1 read-contract change is recorded in `docs/API_CHANGELOG.md`.
- The final Task 1.3 review found and fixed one AI credential regression: provider configuration now uses one explicit grouped write DTO, nonblank transient keys replace the configured key, blank keys preserve it, and read DTOs still expose only `ApiKeyConfigured`.
- Task 1.3 verification passed 38/38 focused client service tests, 32/32 focused instance UI tests, 22/22 focused Application handler tests, 4/4 instance OpenAPI architecture tests, and the canonical Release build with 0 errors. The full Blazor client suite passed 2,105 tests, skipped one governed test, and retained the five unrelated failures recorded below.

### IN PROGRESS

- Phase 1 Blazor baseline reconciliation; Task 1.3 implementation, focused verification, and post-fix review are complete.

### NEXT

1. Reconcile or separately fix the five unrelated full Blazor failures before checking the Phase 1 test gate.
2. Start Task 2.1 after Phase 1 verification is closed.
3. Keep H-019/A-003 until Task 5.3, then remove them after their remaining callers have explicit save boundaries.
4. Keep unrelated dirty worktree changes untouched.

### BLOCKERS

- No focused Task 1.3 technical blocker.
- Runtime visual QA is intentionally skipped by explicit user direction; do not report a browser visual pass for Tasks 1.1 or 1.2.
- The canonical non-runtime Infrastructure lane currently has one unrelated email-dispatch assertion failure because a null HTML body is passed to `DoesNotContain`; 1,053 tests pass.
- Full Persistence integration currently has 69 unrelated failures across privacy, notification-fanout, and email-outbox database/FK isolation scenarios; 591 tests pass.
- Full API integration currently has 16 unrelated failures (privacy/public-session invariants, EventLocation HAL typing, event view filters, RFC 7807/logging checks, and event-location disclosure tests); 1,947 tests pass and two skip.
- Full Blazor client currently has 5 unrelated failures (EventLocation HAL collection typing, home discovery heading, tag-filter trigger, and two event-report dialog assertions); all Task 1.2 Blazor tests pass.

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
| `src/Explore.API/Controllers/InstanceSettingsController.cs` | Updated | Focused instance sub-resources. | Instance settings use generated dedicated PATCH request DTOs; read DTOs are not write contracts. |
| `src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantPoliciesSection.razor` | Updated | Tenant policy UI and ATProto autosave reference. | Policy switches use exact-key autosave with accessible feedback. |
| `src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantAdminSettingsLayout.razor` | Existing | Tenant settings orchestration. | Broad bottom Save is removed only after every section is classified. |
| `src/Explore.Blazor.Client/Pages/Admin/Instance/Components/InstanceAdminSettingsLayout.razor` | Existing | Instance settings orchestration. | Convert ordinary sections to partial autosave; retain explicit sensitive actions. |
| `src/Explore.Blazor.Client/Services/TenantPublicExperienceAdminService.cs` | Existing | Tenant settings client path. | Reuse if it already exposes exact-key write; otherwise use current generated settings client. |
| `src/Explore.Blazor.Client/Services/InstanceOnboardingService.cs` | Updated | Instance settings client path. | Maps sparse read-model leaves to generated PATCH envelopes and preserves explicit sensitive actions. |
| `tests/Explore.Blazor.Client.Tests/Pages/Admin/TenantPoliciesSectionTests.cs` | Existing | Tenant policy interaction coverage. | Add immediate save, lock, pending, and failure recovery cases. |
| `tests/Event.Application.UnitTests/Features/InstanceOnboarding/Commands/UpdateInstanceSubResourceCommandHandlerTests.cs` | Added | Instance sparse-handler and storage-secret coverage. | Covers omitted leaves, one-property updates, complete groups, and redacted S3 credential preservation. |
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
- Task 1.2 intentionally adds one footer `manage-link-groups` HAL capability rather than per-item link policies because every explicit link mutation shares the same tenant-update permission and governance lock.

## Handoff Notes

### Handoff - 2026-07-24 Europe/Brussels

- **Current state:** Tasks 1.1, 1.2, and Task 1.3 implementation/focused verification/review are complete. H-019/A-003 deletion remains assigned to Task 5.3 after remaining broad callers migrate.
- **Next action:** Reconcile the five unrelated Blazor failures before closing the Phase 1 test gate and beginning Task 2.1.
- **Blockers:** Task 1.3 has no focused blocker. Runtime visual QA remains explicitly waived; unrelated canonical-suite failures are listed above.
- **Modified files:** Task 1.2 touched storage/branding/footer Application and API contracts, HAL/authorization, generated OpenAPI/client outputs, tenant admin services/components/tests, API docs, and active workstream artifacts.
- **Validation:** Task 1.3 passed 38/38 client service, 32/32 instance UI, 22/22 Application handler, and 4/4 OpenAPI architecture tests; canonical Release build passed with 0 errors. Full Blazor passed 2,105/2,111 with one governed skip and five unrelated failures. Domain, Application, Architecture, Secrets, and Blazor integration canonical lanes passed; Infrastructure, Persistence, and API retain the unrelated failures listed above. No browser visual pass is claimed.
- **Documentation impact:** API changelog/reference, storage/footer guides, generated contract inventory, and active workstream state now describe the PATCH-only contracts.
- **Risks:** Concurrent unrelated work already touches tenant/instance admin layouts and generated clients; all affected files must be reread immediately before implementation edits.
- **Notes for next contributor/agent:** Never revert unrelated changes. Re-read a file immediately before editing if it is already dirty.
