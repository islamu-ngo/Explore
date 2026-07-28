<!-- ABOUTME: Resume context for the re-baselined partial-update and settings-autosave workstream. -->
<!-- ABOUTME: Records current status, key decisions, source anchors, risks, and next action. -->

# Full Property Update Sub-DTO Pattern - Context

Last Updated: 2026-07-28 Europe/Brussels

## SESSION PROGRESS (2026-07-28 Europe/Brussels)

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
- Completed Task 2.2 for the Application-only EventCategories and EventTags relationship updates. Both flows already had grouped intent validation, concurrency, same-tenant target checks, duplicate checks, repository-mediated one-save mutation, cache invalidation, and tenant-scoped composite FK/unique-index protection.
- Corrected the shared authorization gap: `AuthorizationBehavior` now loads each persisted link by route/link ID, binds its authoritative parent Event/Tenant into the command, and authorizes that parent event. Both handlers recheck the bound context before mutation, preventing caller-selected authorization context from targeting another assignment.
- Added focused behavior and handler coverage for persisted-parent binding and fail-closed context mismatch. `Explore.Application` and `Event.Application.UnitTests` compile with zero errors; indexed diagnostics and `git diff --check` are clean. GPT Oracle returned PASS. Tests were compiled but not executed per user direction.
- Completed Task 2.3. `UpdateEventDraftRequestDto`, its validator, command, and handler are a local/internal draft workflow with no API route, OpenAPI operation, generated client, or production command construction. The canonical public Event update remains the grouped route-ID PATCH verified in Task 2.1.
- The local workflow preserves event authorization, full-draft validation, optimistic concurrency, schedule timezone reprojection, one repository update, and event cache invalidation. Only misleading mandatory header wording was corrected. `Explore.Application` and `Event.Application.UnitTests` compile with zero errors; indexed diagnostics and `git diff --check` are clean. GPT Oracle returned PASS; execution tests remain deferred.
- Task 3.1 implementation is complete across Tag, Tenant, TenantNavigationLink, FooterLinkGroup, FooterLink, and ControlPlaneTenantPlanVersionDraft. All six public operations now use grouped route-ID PATCH bodies; body-owned resource/tenant/user IDs and Tenant lifecycle state are excluded, while lifecycle, navigation/footer reorder, and tenant-plan publish/archive/clone remain dedicated actions.
- The shared external-link transport policy accepts relative paths and HTTPS by default. Registered instance-only setting `security.require_https_external_urls` defaults to `true`; explicit `false` permits HTTP for trusted HTTP-only private networks. Create/update navigation and footer handlers resolve the setting before validation.
- Tenant-plan draft updates validate the merged effective draft and mutate only supplied groups through a tracked repository read. Footer link updates use tenant-bound repository lookup. Tenant metadata now has a concrete update handler, and HAL advertises PATCH where applicable.
- OpenAPI and the generated NSwag client were regenerated. Application, API, Blazor Client, Application unit-test, API integration-test, and Blazor Client test projects compile with zero errors. Focused fixtures cover HTTPS enforcement/opt-out and omitted tenant-plan group preservation; execution tests remain deferred.
- Final GPT Oracle review returned PASS after six remediations: Tenant slug changes refresh the authoritative resolver cache after persistence; footer-link `IsActive` now flows through DTO/OpenAPI/generated client and the edit dialog; Tenant and navigation-link absence return typed 404 responses; plan name is read-only during version editing; stale footer PUT test/docs were corrected; and the obsolete control-plane HAL PUT branch was removed.
- Post-review compilation confirms Application source, Blazor Client, and Blazor Client tests with zero errors. API and Application-test recompilation are currently blocked only by unrelated concurrent ATProto constructor drift; neither command reports a Task 3.1 error. Standard OpenAPI and NSwag generation completed, scoped diagnostics are clean, and diff/conflict checks pass.
- Task 3.2 implementation is complete across current-user, Organization, Group, and ActorSubscription notification preferences. Matrix writes now use PATCH with a presence-aware optional `cells` collection; handlers reject absent/empty updates and validate every supplied required/locked cell before one transaction writes any cell. The three mute endpoints remain focused PUT actions.
- ActorSubscription notification-level PATCH now takes actor identity only from the route and accepts a nullable `notificationLevel` group plus `expectedConcurrencyStamp`. Missing subscriptions map to typed 404 ProblemDetails. HAL `save` advertises PATCH, OpenAPI/NSwag and the contract inventory were regenerated, and the Application, API, Blazor Client, plus all three affected test projects compile with zero errors. One inventory-generator test passed; broader execution tests remain deferred.
- Final GPT Oracle review returned PASS after two remediations: Organization/Group `save` and `set-mute` HAL links now carry exact resource update permission metadata, and actor PATCH OpenAPI marks `expectedConcurrencyStamp` plus nested notification-level `id` required while retaining the nullable group. Focused regression execution passed 2/2 HAL permission cases and 1/1 actor schema case.
- Task 3.3 implementation is complete across current-user appearance localization, AppearanceProfile, and UiTheme. The three broad writes are now PATCH operations. Current-user PATCH accepts only a nullable localization group so it cannot replace omitted direction/language or duplicate focused active-profile/current-mode ownership. AppearanceProfile splits metadata and palettes; UiTheme uses route-owned identity, required row version, and nullable metadata/state/palette groups.
- Appearance handlers reject empty wrappers/groups before mutation. Profile and UI-theme updates apply only supplied leaves; UI-theme default/active invariants are checked against merged persisted state and one transaction performs any default clearing plus one entity update. The BFF sends one localization leaf at a time and uses the focused mode operation for theme changes.
- OpenAPI and NSwag were regenerated. Application, API, BFF, Blazor Client, and affected test projects compile. Focused evidence passes 5 current-user handler tests, 17 profile service tests, 3 UiTheme handler tests, 5 BFF forwarding/validation tests, 8 BFF route/antiforgery tests, 8 appearance architecture tests, 15 client appearance tests, and 1 contract-inventory generator test. API, Blazor Client, and BFF Release builds pass with zero errors; scoped diagnostics, whitespace, and conflict checks are clean.
- Final GPT Oracle review returned PASS after confirming the remediated BFF profile PATCH route, obsolete PUT rejection, and typed HTTP 400 profile-update contract. Task 3.3 has no remaining scoped blocker.
- Task 3.4 implementation and verification are complete across EventLocation disclosure, Event Islamic/Tech aspects, EventSessionAgendaItem, EventSessionGroup, EventSessionSpeaker, TagTypeTags, and CategoryTypeCategories. Acceptance remains open solely for the fresh GPT Oracle gate.
- EventLocation disclosure now independently applies nullable `Fields` and `Audience` groups through PATCH, so omitted privacy state is preserved. Islamic/Tech aspects use explicit POST create operations and grouped PATCH updates instead of upsert semantics.
- EventSessionAgendaItem and EventSessionGroup use route-authoritative grouped PATCH. EventSessionSpeaker now patches `management/{id}` without a redundant parent session ID; its optional `If-Match` header still rejects malformed or unquoted GUID validators.
- TagTypeTags and CategoryTypeCategories remain Application-only relationship commands. Their tenant-scoped composite unique indexes are enforced by `20260727174857_EnforceLookupRelationshipUniqueness`, which performs duplicate preflight checks before replacing only the two prior indexes and restores them in `Down`.
- Focused verification passed 33 Application tests, 30 API/HAL tests, 2 migration tests, and 28 Blazor client tests. The canonical Release build passed with zero errors. Forward/reverse migration scripts contain only the guarded index transition; OpenAPI, NSwag, and inventory generation completed without generated-artifact drift.
- Fresh Task 3.4 review passed after correcting the review's invalid actor-tenant assumption, adding the missing second ABOUTME lines on touched aspect/HAL files, and documenting typed 403 responses for all four authorized Event aspect create/update routes. OpenAPI and NSwag were regenerated, affected API/client builds pass, and the focused metadata suite passes 5/5.

### IN PROGRESS

- Task 3.5 custom-property definition entities are the active slice; Task 3.4 is accepted and the workstream is at 10/20.

### NEXT

1. Implement Task 3.5 across the shared, Event, and EventSession custom-property definition contracts.
2. Preserve exact custom-property value replacement PUT rows A-091/A-092/A-102/A-103.
3. Run the Docker-backed stress/race verification and deferred phase tests before final workstream completion.
4. Keep H-019/A-003 until Task 5.3, then remove them after their remaining callers have explicit save boundaries.
5. Keep unrelated dirty worktree changes untouched.

### BLOCKERS

- Deferred verification: Docker-backed stress/race execution is unavailable because `unix:///home/amir/.docker/desktop/docker.sock` does not exist; the user explicitly allowed implementation to continue.
- Non-blocking external status: the full Architecture suite has 11 unrelated concurrent failures and one governed skip; do not attribute them to Task 3.4 or claim a green full suite.
- Runtime visual QA is intentionally skipped by explicit user direction; do not report a browser visual pass for Tasks 1.1 or 1.2.

## Quick Resume

1. Read this file and `full-property-update-sub-dto-tasks.md`.
2. Read only the current phase, decisions, and affected rows in the plan/inventory.
3. Continue with Task 3.5 rows D-052/D-054/D-055, H-065-H-067, and A-090/A-101/A-104.
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
| `src/Explore.API/Controllers/EventSessionSpeakerController.cs` | Updated | Canonical speaker-assignment PATCH. | Uses only assignment `id`; optional `If-Match` remains strict. |
| `src/Explore.Persistence/Migrations/20260727174857_EnforceLookupRelationshipUniqueness.cs` | Updated | Relationship race-safety migration. | Duplicate preflight plus only two tenant-scoped unique-index replacements. |
| `tests/Event.Persistence.IntegrationTests/Migrations/LookupRelationshipUniquenessMigrationTests.cs` | Added | Migration operation guard. | Proves focused Up/Down index operations and duplicate preflight SQL. |
| `tests/Event.API.IntegrationTests/Features/EventSessionAgendaItemControllerTests.cs` | Added | Agenda-item contract coverage. | Proves canonical PATCH and obsolete PUT absence. |
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

Latest Task 3.4 evidence: focused Application tests passed 33/33, API/HAL tests 30/30, persistence migration tests 2/2, and Blazor client tests 28/28. `dotnet build --configuration Release --verbosity quiet -maxcpucount:1` passed with zero errors. Forward/reverse `dotnet ef migrations script` output contains only the guarded two-index transition. Canonical OpenAPI, NSwag, and inventory generators completed without artifact drift; generated operation IDs/routes, JSON validity, zero-NUL checks, scoped diagnostics, `git diff --check`, and conflict scanning pass. The post-review Event aspect metadata suite passed 5/5, and affected API/client builds pass after typed 403 and ABOUTME remediation. The full Architecture suite has 11 unrelated concurrent failures and one governed skip. Task 3.4 is accepted.

## Current Known Risks / Unknowns

- The current 59/71/104 scans have no unclassified row. Task 6.2 must enforce that exact coverage and catch new rows.
- Active unrelated changes include the registration-data-collection workstream and federation/participation files. Implementation agents must reread affected files and merge with current work rather than overwrite it.
- H-019/A-003 remain until Task 5.3 because announcement-bar writes still depend on them; removing them earlier would break a live non-policy caller.
- Instance sub-resource concurrency may rely on existing policy-set/version behavior; Task 1.3 must preserve it rather than introduce a new token blindly.
- Task 1.2 intentionally adds one footer `manage-link-groups` HAL capability rather than per-item link policies because every explicit link mutation shares the same tenant-update permission and governance lock.

## Handoff Notes

### Handoff - 2026-07-28 Europe/Brussels

- **Current state:** Tasks 1.1-3.4 are accepted, representing 10/20 tasks. Task 3.4 implementation, focused execution, migration-script inspection, generated-contract synchronization, Release build, and fresh review pass.
- **Next action:** Implement Task 3.5 custom-property definition entities.
- **Blockers:** No Task 3.4 blocker remains. Docker runtime remains unavailable, and the full Architecture suite has 11 unrelated concurrent failures plus one governed skip.
- **Modified files:** Task 3.4 spans program/aspect/relationship DTOs and handlers already present at HEAD, current EventSessionSpeaker controller/HAL corrections, focused API/Blazor tests, the relationship uniqueness migration/designer/snapshot, API docs, and these active records. Registration-data-collection and federation/participation changes are unrelated concurrent work.
- **Validation:** Application 33/33, API/HAL 30/30, persistence migration 2/2, Blazor client 28/28, and post-review metadata 5/5 focused tests pass. Release and affected API/client builds pass with zero errors. Migration scripts, generated artifacts, diagnostics, JSON/NUL checks, whitespace, and conflict checks pass.
- **Documentation impact:** API reference/changelog describe the canonical Task 3.4 routes; this refresh synchronizes plan, context, and tasks with accepted implementation reality.
- **Risks:** The graph snapshot predates current HEAD and the worktree contains broad concurrent changes. Review must remain scoped and must not infer that unrelated architecture failures or Docker lanes passed.
- **Notes for next contributor/agent:** Read this context and the Task 3.4 ledger first. Never revert unrelated changes; reread any dirty file immediately before editing.
