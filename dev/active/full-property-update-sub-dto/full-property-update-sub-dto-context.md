<!-- ABOUTME: Resume context for the re-baselined partial-update and settings-autosave workstream. -->
<!-- ABOUTME: Records current status, key decisions, source anchors, risks, and next action. -->

# Full Property Update Sub-DTO Pattern - Context

Last Updated: 2026-07-26 Europe/Brussels

## SESSION PROGRESS (2026-07-26 Europe/Brussels)

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
- Implemented the Task 1.3 migration slice: regenerated instance PATCH contracts are mapped by `InstanceOnboardingService`, storage uses complete Policy/S3 groups, and auth/authz onboarding duplicates route through the canonical update operations. Final gates remain open.
- Ordinary instance module, governance, branding, domain, analytics, and footer controls now send sparse updates with accessible pending/saved/error state; failed callbacks reload only the authoritative sub-resource. Text fields save on blur, while switches/selects save immediately.
- Storage, SMTP, AI provider/credential, authentication, and authorization groups remain explicit Save/test/action flows; the parent broad Save no longer owns ordinary autosaved sections.
- Task 1.3 post-review fixes removed the legacy H-006 monolith, preserve redacted S3 credentials, reload authoritative auth/authz and ordinary section models after failed writes, and prevent SMTP/AI read contracts from returning persisted secret-shaped fields. OpenAPI and the NSwag client were regenerated, and the breaking pre-v1 read-contract change is recorded in `docs/API_CHANGELOG.md`.
- A later Task 1.3 review found and fixed one AI credential regression: provider configuration now uses one explicit grouped write DTO, nonblank transient keys replace the configured key, blank keys preserve it, and read DTOs still expose only `ApiKeyConfigured`.
- Earlier Task 1.3 evidence remains valid historical evidence: 38/38 focused client service tests, 32/32 focused instance UI tests, 22/22 focused Application handler tests, 4/4 instance OpenAPI architecture tests, and a canonical Release build passed with 0 errors.
- Completed the final Oracle correction slice. Only the exact canonical `GET` and `PATCH` auth-provider/authz-provider routes accept active setup-secret or authenticated instance-admin authority; unrelated routes remain excluded. One global `setup:{ip}` fixed window owns setup quota for existing setup endpoints and canonical provider GET/PATCH attempts; named `SetupSecret` and setup-secret `Write` branches bypass duplicate state, while bearer writes remain per-user.
- Typed 429 parity now covers all four exact provider GET/PATCH operations. The shared `setup:{ip}` 5-per-60-second window applies only to existing setup endpoints and provider GET/PATCH requests carrying `X-Setup-Secret`; bearer GETs remain outside the setup bucket, while bearer PATCH requests remain per-user `Write` traffic.
- Module PATCH capability synchronization runs only for `SingleTenant`, targets `PlatformDefaults.DefaultTenantId`, passes only present leaves, and propagates cancellation. `ResolverConfigService` returns copies. All value, mixed value-lock, and lock-only notifications produced by the six transaction-owned PATCH handlers are deferred and published only after successful commit; lock transitions use the correct `SystemLocked` or `SystemDefault` source.
- Final Oracle review passed after GET 429 parity. No Task 1.3 scoped remediation remains.
- Current scoped evidence: exact setup authority/limiter combined tests passed 13/13; focused metadata passed 16/16; API and generated-client builds passed; the Application source build passed; resolver tests passed 2/2; the inventory writer passed 1/1; scoped architecture passed 4/4; the Persistence integration test project builds; and the scoped diff is clean.
- Deferred-notification focused runs passed 33 service tests and 26 handler tests. The canonical Release build now passes with 0 errors and the Phase 1 Blazor suite passes 2,115/2,116 with one governed skip. The full Architecture suite executes 304 tests but has two unrelated concurrent failures: `ConsentAffordanceShouldExistOnlyOnMyReportsPolicies` expects an obsolete source string, and `InventoryCoversCurrentEfAndDesignatedProviderSurfaces` lacks four newly added registration/privacy inventory fields. Docker remains unavailable because the configured Docker Desktop socket does not exist, so the stress/race lanes remain pending.
- The user explicitly deferred the two unavailable Docker-backed Task 1.3 runtime lanes on 2026-07-26 and directed implementation to continue. They remain required before final workstream completion; no runtime pass is inferred or claimed.
- Completed Task 2.1 across all 14 canonical grouped entity PATCH surfaces. EventRegistration required no correction; the other surfaces received only confirmed contract-drift fixes.
- Actor PATCH now excludes provider-owned federation/media fields, retains tenant-owned profile-image updates, and distinguishes actor absence from invalid profile-image references. EventSeries authorization derives Actor/Tenant context from persisted state and rechecks it in the handler. EventSessionLanguage authorization resolves its persisted parent session in `AuthorizationBehavior` before policy evaluation, eliminating the controller-side existence probe.
- User, EventSeries, and EventSessionLanguage reads now use registered authorization-aware HAL assemblers; OpenAPI/NSwag contracts were regenerated and Blazor services unwrap the new HAL resources without local authorization logic.
- Canonical update controllers now declare and return consistent 403/404 responses. Group and Organization permission denials throw `AuthorizationException` before mutation; not-found command responses map to ProblemDetails rather than validation HTTP 400 or success HTTP 200.
- Final GPT Oracle review returned PASS after three corrections: exact Actor-not-found matching, Group/Organization authorization exceptions, and stale EventSessionLanguage controller-test expectations.
- Task 2.1 compile evidence: Application, API, Blazor Client, Application unit-test, and Blazor Client test projects build; `git diff --check` and conflict checks are clean. API integration-test compilation is externally blocked by 29 concurrent Actor/User/Organization fixture errors; the canonical solution build reports 84 errors across affected test projects. No Task 2.1 execution test is claimed because the user deferred tests.

### IN PROGRESS

- Task 2.2 is next: verify the Application-only EventCategories and EventTags canonical relationship updates without inventing public controllers.

### NEXT

1. Verify Task 2.2 EventCategories and EventTags relationship updates.
2. Continue through the approved implementation tasks without treating deferred Task 2.1 or Docker test execution as a pass.
3. Run the Docker-backed stress/race verification and deferred phase tests before final workstream completion.
4. Keep H-019/A-003 until Task 5.3, then remove them after their remaining callers have explicit save boundaries.
5. Keep unrelated dirty worktree changes untouched.

### BLOCKERS

- Deferred verification: Docker-backed stress/race execution is unavailable because `unix:///home/amir/.docker/desktop/docker.sock` does not exist; the user explicitly allowed implementation to continue.
- Non-blocking external status: the full Architecture suite has a stale event-report consent source assertion and four missing newly added registration/privacy inventory fields.
- Runtime visual QA is intentionally skipped by explicit user direction; do not report a browser visual pass for Tasks 1.1 or 1.2.

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
| `src/Explore.API/Extensions/RateLimitingExtensions.cs` | Updated | API write-rate partition selection. | The global limiter owns the shared IP-keyed setup window; named setup/write branches bypass duplicate state, and bearer admins use per-user `Write`. |
| `src/Explore.Infrastructure/Services/ResolverConfigService.cs` | Updated | Cached resolver configuration. | Returns isolated copies so callers cannot mutate cache-owned state. |
| `src/Explore.Persistence/Repositories/SystemSettingRepository.cs` | Updated | Atomic system-setting persistence. | Supports metadata-only governance lock upserts without value rewrites. |
| `src/Explore.Application/Settings/SettingUpsertService.cs` | Updated | Setting upsert orchestration. | Uses the atomic metadata-only path for lock-only governance patches. |
| `src/Explore.Domain/Constants/PlatformDefaults.cs` | Existing | Canonical platform tenant constants. | Module PATCH capability synchronization uses `DefaultTenantId`. |
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

Latest Task 1.3 evidence: exact setup authority/limiter combined tests passed 13/13; focused metadata passed 16/16; API and generated-client builds passed; the Application source build passed; deferred-notification focused runs passed 33 service tests and 26 handler tests; resolver tests passed 2/2; the inventory writer passed 1/1; scoped architecture passed 4/4; the Persistence integration test project builds; and the scoped diff is clean. The canonical Release build passes with 0 errors, and the Phase 1 Blazor suite passes 2,115/2,116 with one governed skip. The full Architecture suite executes 304 tests with 301 passing, one governed skip, and two unrelated non-blocking concurrent failures. Final scoped Oracle review passed after GET 429 parity with no remediation remaining. A fresh GPT Oracle gate review returned WAIT: Task 1.3 stays open solely for unavailable Docker stress/race lanes absent an explicit user waiver; no Docker runtime result is claimed.

## Current Known Risks / Unknowns

- The current 59/71/104 scans have no unclassified row. Task 6.2 must enforce that exact coverage and catch new rows.
- Active unrelated changes touch admin layouts and generated contracts. Implementation agents must reread affected files and merge with current work rather than overwrite it.
- H-019/A-003 remain until Task 5.3 because announcement-bar writes still depend on them; removing them earlier would break a live non-policy caller.
- Instance sub-resource concurrency may rely on existing policy-set/version behavior; Task 1.3 must preserve it rather than introduce a new token blindly.
- Task 1.2 intentionally adds one footer `manage-link-groups` HAL capability rather than per-item link policies because every explicit link mutation shares the same tenant-update permission and governance lock.

## Handoff Notes

### Handoff - 2026-07-26 Europe/Brussels

- **Current state:** Tasks 1.1-2.1 implementation is complete. Task 2.1 has final GPT Oracle PASS: all 14 canonical grouped entity PATCH surfaces were verified, confirmed drift was corrected, HAL/OpenAPI/NSwag contracts agree, and execution tests remain explicitly deferred rather than reported as passed. H-019/A-003 deletion remains assigned to Task 5.3 after remaining broad callers migrate.
- **Next action:** Start Task 2.2 by verifying the Application-only EventCategories and EventTags relationship updates, then continue the approved sequence while retaining deferred verification debt.
- **Blockers:** No Task 2.1 implementation blocker. Docker runtime tests and Task 2.1 test execution are deferred, not passed. API integration-test compilation currently has 29 unrelated concurrent Actor/User/Organization fixture errors; the canonical solution build reports 84 errors across affected test projects. Runtime visual QA remains explicitly waived.
- **Modified files:** Task 2.1 touched the scoped Actor, EventSeries, EventSessionLanguage, User, controller response-mapping, HAL registration/policy/assembler, OpenAPI/generated-client, Blazor adapter, compile-fixture, and active workstream files. Do not alter or revert unrelated worktree changes.
- **Validation:** Application, API, Blazor Client, Application unit-test, and Blazor Client test projects compile; `git diff --check` and conflict checks are clean. Final GPT Oracle review passed with no scoped remediation. No Task 2.1 execution test or canonical solution-build pass is claimed.
- **Documentation impact:** Tasks and context now record Task 2.1 implementation completion, Oracle PASS, exact compile evidence, deferred execution tests, and Task 2.2 as the next slice.
- **Risks:** Concurrent domain-model work has left test fixtures stale and prevents a clean canonical build; distinguish those external errors from Task 2.1 while keeping the full build gate open.
- **Notes for next contributor/agent:** Never revert unrelated changes. Re-read a file immediately before editing if it is already dirty.
