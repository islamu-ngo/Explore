<!-- ABOUTME: Resume context for the event lifecycle validation policy planning workstream. -->
<!-- ABOUTME: Tracks current investigation, decisions, constraints, validation baseline, and handoff notes. -->

# Event Lifecycle Validation Policy — Context

Last Updated: 2026-06-25 Europe/Brussels

## SESSION PROGRESS (2026-06-25 Europe/Brussels)

### ✅ COMPLETED

- **Event-session terminal lifecycle, publish-selection UX, and moderation cascade implemented** (2026-06-25):
  - `EventSessionStatusEnum`, lookup seeding, and migration coverage now include `Completed` and `Moderated` alongside the earlier draft/review/published/cancelled/archived statuses.
  - New explicit CQRS commands and API routes support session `archive`, `cancel`, and `complete` transitions. Complete requires both the parent event and session to be `Published`; publish remains blocked by readiness when the parent event is not `Published`.
  - Event-level light moderation now cascades all sessions to `Moderated`. Heavy moderation redacts event-owned session text/custom-property data to `Redacted`, clears session image references, and marks sessions `Moderated`.
  - HAL session affordances now expose `schedule`, `publish`, `cancel`, `complete`, and `archive` only from server-owned state and authorization checks. No session-level `moderate` affordance is emitted because moderation is event-scoped.
  - Blazor publishing now uses progressive disclosure: multi-session events open a publish-selection dialog; single-session events avoid the dialog; users may publish all/select sessions or keep draft sessions. Parent event publish occurs first, then selected sessions publish with their concurrency stamps.
  - Blazor event details link agenda sessions to event-session details only for multi-session events. Single-session events stay abstracted and do not show session-detail links.
  - A new event-session detail route/page (`/events/{eventId}/sessions/{sessionId}`) provides a topbar with parent-event navigation and HAL-gated edit/publish/cancel/complete/archive buttons, intentionally without moderation controls.
  - Verification: focused Application lifecycle/moderation/redaction tests passed 10/10; focused Blazor route tests passed 9/9; focused API/HAL lifecycle tests passed 4/4; focused EventSessionManager filter run passed 1/1; API contract inventory generation passed 1/1 and refreshed `docs/API_CONTRACT_INVENTORY.md`. The broad canonical Release build still exits before diagnostics with `Build FAILED` and 0 warnings/0 errors.
- **Blazor authorized internal draft-session reads implemented and verified** (2026-06-25):
  - `EventService.GetSessionsByEventAsync` now defaults to public-only reads and accepts `includeManagedSessions: true` for management contexts. When enabled, it reads `GetManagedEventSessionsByEventAsync`, replaces public duplicates with managed rows, appends internal-only draft sessions, and falls back to public data on `401`, `403`, or `404`.
  - Existing Blazor management surfaces now opt into the authorized read path without local role checks: `EventDetail` derives the flag from API-provided HAL management links, `EventEdit` uses event HAL edit/session-management affordances, and program/delete dialogs use the management service path because they are opened from management workflows.
  - `EventSessionManager` now accepts `IncludeManagedSessions`, sorts unscheduled drafts last, and renders draft status plus explicit `Schedule TBD` / `Location TBD` fallbacks for nullable draft session data.
  - `docs/BLAZOR.md` now documents the public/default vs authorized/management session-read boundary.
  - Verification: `EventServiceTests` focused Blazor test run passed 56/56; `EventSessionManagerTests` no-build focused run passed 1/1. A build-enabled component-test attempt hit unrelated current-worktree compile error `Explore.Infrastructure/Ai/AiProviderSettings.cs` -> `AiProviderDefaults.ProviderIdOpenAiSdk` missing.
  - External research/docs this session: Context7 MudBlazor docs for component async/loading-state patterns; Tavily search for HAL/hypermedia affordance context. Project HAL and Blazor docs remained the authoritative source for implementation.
- **Blazor WASM full-solution build blocker triaged to local SDK/workload state** (2026-06-25):
  - Current local SDK state is `10.0.300` with no installed workloads (`dotnet workload list` is empty), while official ASP.NET Core Blazor WebAssembly docs require `dotnet workload install wasm-tools` for Release WebAssembly builds.
  - Reproduced the failing path directly with `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity minimal`: `ComputeWasmBuildAssets` cannot create/connect to the `NET`/`x64` task host and then reports disposed `MetadataLoadContext` output.
  - Ruled out a repo-level package patch mismatch: `/p:RuntimeFrameworkVersion=10.0.7` selected `microsoft.net.sdk.webassembly.pack/10.0.7` but failed in the same task-host path; `/p:DisableOutOfProcTaskHost=true` and `-maxcpucount:1` also did not fix it.
  - `dotnet workload install wasm-tools` was attempted and failed with `Inadequate permissions` because the SDK is installed under `/usr/share/dotnet`; resolving broad solution build requires installing the workload with OS-level permission or using a user-local SDK that has `wasm-tools`.
  - Documentation updated in `docs/TESTING.md` and this workstream so scoped lifecycle verification remains distinct from the local Blazor SDK/toolchain prerequisite.
- **Phase 4 task 4.5 generated contract refresh and Phase 5 documentation COMPLETE and verified** (2026-06-25):
  - Generated contract artifacts now include the lifecycle operations and draft-session DTO shape: `ImportEvent`, `CreateDraftEventSession`, `ScheduleEventSession`, `PublishEventSession`, nullable session schedule/local projection fields, `IsScheduled`, `ConcurrencyStamp`, and `EventSessionStatus*`.
  - Contract refresh used the scoped build/generation path: `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity minimal --no-restore -maxcpucount:1` and `dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal`.
  - Updated contract/documentation surfaces: `schemas/openapi.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CONTRACT_INVENTORY.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/DOMAIN.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, and `schemas/islamu-event.md`.
  - Documentation now records the Clean Architecture boundary: public session reads remain scheduled/published-only, management reads are authorized and can see draft/internal sessions, lifecycle writes use explicit command routes, and HAL `_links` remain the UI affordance source of truth.
  - Added event-session-specific tenant isolation coverage: an authenticated management read from the default tenant does not return sessions seeded under another tenant.
  - Verification: scoped API build 7 projects/0 errors; NSwag client target passed; publish-readiness Application tests 3/3; lifecycle API/HAL tests 9/9; session visibility/tenant isolation tests 5/5; Architecture 197/198 with the known skipped API metadata test.
  - Broad solution build currently reaches an unrelated Blazor WASM `ComputeWasmBuildAssets` task-host failure; use the scoped API build plus NSwag target for lifecycle contract generation until that environment issue is resolved.
  - External research/docs this session: Context7 NSwag docs for generated OpenAPI clients, Context7 ASP.NET Core OpenAPI docs for build-time document generation concepts, and Tavily search for current OpenAPI/NSwag generation context.
- **Phase 4 tasks 4.1-4.4 lifecycle API/HAL contracts COMPLETE and verified** (2026-06-25):
  - Event lifecycle API contracts are now explicit and named: `POST /api/event/import`, `GET /api/event/{id}/publish-readiness`, `POST /api/event/{id}/publish`, `POST /api/event/{id}/archive`, and `POST /api/event/{id}/cancel`.
  - Publish-readiness is no longer only controller-authenticated; `GetEventPublishReadinessRequest` now implements `ISecureRequest` and is guarded by `ResourceKinds.Event` + `AuthorizationActions.Update` in the MediatR authorization pipeline.
  - Session lifecycle API contracts expose the approved command-backed subset: `POST /api/eventsession/drafts`, `POST /api/eventsession/{id}/schedule`, and `POST /api/eventsession/{id}/publish`. Submit/approve/archive session routes were not invented because no Application commands exist for them yet.
  - `EventSessionDto` and `EventSessionListDto` now align with draft-capable storage by making schedule/local projection fields nullable and adding `ConcurrencyStamp`, `EventSessionStatus*`, `IsScheduled`, and list `TenantId`.
  - HAL event links now emit explicit `create-session-draft` and keep `add-session` pointed at the draft lifecycle route. HAL session links emit `schedule` and `publish` from server-owned session state plus permission metadata.
  - Verification: `dotnet build --configuration Release --verbosity quiet` ✓; `GetEventPublishReadinessRequestHandlerTests` 3/3 ✓; focused API/HAL contract tests 16/16 ✓; `EventSessionVisibilityContractTests` 4/4 ✓; Architecture 197/198 ✓ with the known skipped API metadata test.
  - External research/docs this session: Context7 ASP.NET Core docs for controller route/auth/response metadata conventions; Tavily search for REST lifecycle transition endpoint and hypermedia affordance context; Context7 Microsoft.Testing.Platform docs for `--treenode-filter` syntax.
- **Phase 1 persistence foundation FULLY IMPLEMENTED AND VERIFIED GREEN** (2026-06-23):
  - Tasks 1.1-1.8 complete: `EventSessionStatus` lookup (entity/enum/repo/config/DbSet/DI/seed/API), nullable `EventSession.StartTime`/`EndTime` + 6 local projections, rollup fixes, partial GiST exclusion, EF migration `20260623101543_AddEventSessionStatusAndNullableSchedule`, 8 new persistence constraint tests.
  - Verification: Build 0 errors; Domain 287/287 ✓; Architecture 194/195 ✓ (1 skipped); Persistence 166/166 ✓.
  - External research: Tavily MCP (lifecycle patterns, nullable schedules, PostgreSQL partial exclusion) + Context7 MCP (EF Core nullability/migrations, Npgsql ranges/constraints) consulted during implementation.
- **Phase 2 application lifecycle policy partially implemented and verified** (2026-06-23):
  - Tasks 2.1-2.5 complete: controlled `ValidationProfile` values, stable `EventFieldKey`/`EventSessionFieldKey` concepts, central `EventLifecyclePolicyProvider`, policy-aware `EventLifecycleReadinessEvaluator`, machine-readable publish readiness DTO mapping, publish-command readiness gate, and direct published-create readiness gate.
  - This session added executable session readiness evaluation for `SessionDraftCreate`, `SessionSchedule`, and `SessionPublish` profiles. Session readiness now reports stable codes/paths for missing title/parent/tenant/status/schedule fields, invalid schedule ranges, parent event compatibility, and rejected/cancelled/archived hard invariants.
  - Verification: `dotnet build --configuration Release --verbosity quiet` ✓; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` 1490/1490 ✓; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` 194/195 ✓ (1 known skipped API metadata test).
  - External research/docs this session: Context7 `/fluentvalidation/fluentvalidation` confirmed current RuleSet invocation syntax; Tavily research endpoint was quota-limited, so Tavily search/extract was used for CQRS command validation and state-transition operation context.
- **Phase 2 application lifecycle policy COMPLETE and verified** (2026-06-23):
  - Tasks 2.6-2.9 complete: explicit archive/cancel command tests, import event command tests, generic status-update bypass regression tests, and session draft/schedule/publish Application commands.
  - Import command now stores structurally valid draft defaults when publication-quality fields are omitted: `VisibilityTypeId = Private`, `EventFormatId = Local`, and `EventStatusId = Draft`, while provenance remains required.
  - New session lifecycle commands:
    - `CreateDraftEventSessionCommand` creates unscheduled Draft sessions under existing events without fake `StartTime`/`EndTime`.
    - `ScheduleEventSessionCommand` applies `EventSession.Reschedule(...)`, links matching `EventDay`, validates `SessionSchedule` readiness, and persists through `UpdateWithRoomOverlapGuardAsync`.
    - `PublishEventSessionCommand` validates `SessionPublish` readiness, including parent event published compatibility, before setting `EventSessionStatusId = Published`.
  - Generic event update no longer exposes status mutation via `UpdateEventDto` or `UpdateEventCommand`; lifecycle status changes must flow through explicit commands.
  - Verification: `dotnet build --configuration Release --verbosity quiet` ✓; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` 1507/1507 ✓; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` 194/195 ✓ (1 known skipped API metadata test).
  - External research/docs this session: Context7 current MediatR docs confirmed `IRequestHandler<TRequest,TResponse>.Handle(TRequest, CancellationToken)` shape; Tavily search confirmed explicit state-transition operations should combine current state and client intent rather than generic status writes.
- **Phase 3 task 3.1 public session visibility filtering COMPLETE and verified** (2026-06-23):
  - Anonymous session list/detail/by-event queries now call explicit public repository methods instead of broad internal session reads.
  - `EventSessionRepository` centralizes the public visibility gate in `BuildPublicSessionQuery()`: session status must be `Published`, schedule must be non-null, parent event visibility must be `Public`, and parent event status must not be Draft/Moderated/Archived.
  - Internal Application flows still use the existing broad reads (`GetSessionWithDetails`, `GetSessionsByEvent`, etc.) so lifecycle commands, registration, agenda, and management-style flows can still work with draft/internal sessions.
  - Verification: `dotnet build --configuration Release --verbosity quiet` ✓; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` 1507/1507 ✓; `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --treenode-filter "/*/*/EventSessionVisibilityContractTests/*"` 3/3 ✓; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` 194/195 ✓ (1 known skipped API metadata test).
  - Full `Event.API.IntegrationTests` was attempted and stopped after several minutes because unrelated pre-existing failures appeared across ProblemDetails, HATEOAS, authorization, and storage settings tests. The focused session visibility contract class passed.
  - External research/docs this session: Context7 EF Core docs confirmed `AsNoTracking()` and filtered/eager query guidance; Tavily search reinforced filtering public resource endpoints for security and consistency; Context7 TUnit docs confirmed `--treenode-filter "/*/*/ClassName/*"` syntax.
- **Phase 3 task 3.3 event schedule rollups COMPLETE and verified** (2026-06-25):
  - `EventSession.ContributesToPublicScheduleSummary()` now centralizes the public rollup predicate: non-deleted, `Published`, scheduled with non-null `StartTime` and `EndTime`.
  - `Event.RecalculateScheduleSummaryFromSessions()` now uses the same public-session definition as anonymous session queries, so Draft/Rejected/Cancelled/Archived/unscheduled sessions do not affect `SessionCount`, `FirstSessionDate`, `LastSessionDate`, `FirstSessionStartUtc`, or `LastSessionStartUtc`.
  - `ScheduleEventSessionCommandHandler` and `PublishEventSessionCommandHandler` refresh the parent event schedule graph after successful session mutation, keeping event list/detail summaries aligned with session lifecycle transitions.
  - `CreateEventCommandHandler` no longer seeds public schedule rollups from draft-created sessions; direct published creation marks initial child sessions as `Published` so publish readiness and outbox dates still come from public sessions.
  - Verification: `dotnet build --configuration Release --verbosity quiet` ✓; `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` 293/293 ✓; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` 1511/1511 ✓; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` 194/195 ✓ (1 known skipped API metadata test).
  - External research/docs this session: Context7 EF Core docs reaffirmed explicit null filtering for optional schedule fields; Tavily search confirmed public calendars/feeds should hide unpublished/internal sessions.
- **Phase 3 task 3.4 public boundary verification COMPLETE and verified** (2026-06-25):
  - `GetEventCalendarExportRequestHandler`, `GetEventProgramSummaryRequestHandler`, and `GetEventAgendaProjectionRequestHandler` now use `GetPublicSessionsByEventAsync(...)` instead of broad `GetSessionsByEvent(...)` reads for anonymous/public outputs.
  - Program summary and agenda projection now fail closed unless the parent event is `Published` and `Public`, preventing event title, agenda, and session metadata exposure for draft/private parents.
  - `EventFilter` now carries an `EventFilterType`, allowing `EventRepository` to detect `PubliclyDiscoverable()` specifications without expression-string parsing. Public event-list location/language/registration-mode subquery filters now require matching sessions to be `Published` and scheduled.
  - AI reference search already composes `EventFilter.PubliclyDiscoverable()` and does not enumerate sessions. Publish outbox/fanout source data comes from the Phase 3.3 public schedule rollups, so hidden sessions do not drive published payload dates.
  - Verification: `dotnet build --configuration Release --verbosity quiet` ✓; `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` 1514/1514 ✓; `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` 178/178 ✓; `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` 194/195 ✓ (1 known skipped API metadata test).
  - External research/docs this session: Context7 EF Core docs confirmed `AsNoTracking()`/explicit null filtering guidance for read-only public queries; Tavily search confirmed public calendars/feeds should hide draft/unpublished content.
- Planning workstream created under `dev/active/event-lifecycle-validation-policy/`.
- Senior CTO re-baseline completed after user confirmed nullable draft session scheduling and development-mode breaking changes.
- Current-state report completed with repository evidence.
- Baseline build verified: `dotnet build --configuration Release --verbosity quiet` returned 0 errors.
- **Phase 1 persistence foundation FULLY IMPLEMENTED AND VERIFIED** (2026-06-23):
  - Tasks 1.1-1.8 complete: `EventSessionStatus` lookup entity/enum/repo/config/DbSet/DI/seed/API; nullable `EventSession.StartTime`/`EndTime` + 6 local projections; rollup fixes; partial GiST exclusion; EF migration `20260623101543_AddEventSessionStatusAndNullableSchedule`; 8 new persistence constraint tests.
  - Verification: Build 0 errors; Domain 287/287 ✓; Architecture 194/195 ✓ (1 skipped); Persistence 166/166 ✓.
  - External research: Tavily MCP (lifecycle patterns, nullable schedules, PostgreSQL partial exclusion) + Context7 MCP (EF Core nullability/migrations, Npgsql ranges/constraints) consulted during implementation.
- Relevant contracts, docs, rules, and skills loaded:
  - `AGENTS.md`
  - `.claude/commands/dev-docs.md`
  - `.claude/contract/intents.yaml`
  - `docs/QUICK_REFERENCE.md`
  - `docs/GOVERNANCE.md`
  - `docs/ARCHITECTURE.md`
  - `docs/DOMAIN.md`
  - `docs/API.md`
  - `docs/SECURITY-MODEL.md`
  - `docs/AUTHORIZATION.md`
  - `docs/MULTI_TENANCY.md`
  - `docs/OPERATIONS.md`
  - `docs/TESTING.md`
  - `.claude/rules/domain.md`
  - `.claude/rules/application-layer.md`
  - `.claude/rules/efcore-persistence.md`
  - `.claude/rules/efcore-migrations.md`
  - `.claude/rules/api-controllers.md`
  - `.claude/rules/api-hateoas.md`
- Relevant skills loaded:
  - `clean-architecture-rules`
  - `dotnet-efcore-guidelines`
  - `cqrs-mediatr-guidelines`
  - `auth-patterns`
  - `senior-cto-feedback`
- External framework docs checked through Context7:
  - EF Core nullable reference type and required/optional property guidance.
  - FluentValidation RuleSets/Include/conditional validation guidance.
- Tavily MCP research completed for state-based draft lifecycle patterns, public visibility filtering, migration sequencing, partial indexes, and operator-risk considerations.

### 🟡 IN PROGRESS

- No source implementation remains for the requested event-session lifecycle/moderation/publish-selection slice.
- Broad-build verification cleanup remains blocked by the current local MSBuild graph and Blazor WebAssembly task-host issues recorded below.

### ⏭️ NEXT

1. Repair the local verification graph, then rerun scoped Application/API/Blazor lifecycle tests and the canonical build.
2. Regenerate OpenAPI/client artifacts through the canonical API/NSwag workflow once the API build graph is stable, if no later generated artifacts already include the session terminal routes.
3. Keep public lifecycle affordances HAL-driven: clients must gate actions by `_links`, not local role/status inference.

### ⚠️ BLOCKERS

- `RTK.md` is referenced by `AGENTS.md` but was not found under `/home/amir/ISLAMU/Github/Event`.
- Event publication with zero public sessions remains intentionally undecided and out of scope for the nullable-session foundation.
- Broad solution build is blocked in this local environment by missing Blazor WebAssembly build tooling. `dotnet workload install wasm-tools` failed with `Inadequate permissions` against `/usr/share/dotnet`, so this cannot be repaired through repo source changes alone.
- Current broad `dotnet build --configuration Release --verbosity quiet` exits before diagnostics with 0 warnings and 0 errors after 2 projects. Focused Application/API/Blazor lifecycle slices are green, but this is not a final full-repo green state.

## Quick Resume

1. Read `event-lifecycle-validation-policy-plan.md`.
2. Read `event-lifecycle-validation-policy-tasks.md`.
3. Phase 1, Phase 2, Phase 3, Phase 4, Phase 5 docs, generated contracts, Blazor draft-session reads, and the event-session terminal lifecycle/moderation/publish-selection source slice are complete.
4. Treat nullable `StartTime`/`EndTime` for draft sessions as approved and implemented.
5. Keep all three dev docs updated after each meaningful implementation slice.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `Explore.Domain/Event.cs` | Existing | Domain | Event aggregate, status, nullable event business fields, schedule rollups | Already soft-validation friendly at event level. |
| `Explore.Domain/EventSession.cs` | Existing | Domain | Scheduled event child content | No status today; schedule fields required. |
| `Explore.Domain/Enums/EventStatusEnum.cs` | Existing | Domain | Event status lookup enum | Draft/Published/Cancelled/Completed/Archived already exist. |
| `Explore.Domain/EventStatus.cs` | Existing | Domain | Event status lookup row | Existing lookup pattern to mirror for sessions. |
| `Explore.Domain/EventSessionStatus.cs` | New | Domain | Session status lookup row | Required for session lifecycle. |
| `Explore.Domain/Enums/EventSessionStatusEnum.cs` | New | Domain | Stable int IDs for session statuses | Baseline IDs/codes recorded in the plan. |
| `Explore.Application/Contracts/Persistence/IEventSessionStatusRepository.cs` | New | Application | Lookup repository contract | Mirror `IEventStatusRepository` if lookup validation/API needs status existence checks. |
| `Explore.Persistence/Repositories/EventSessionStatusRepository.cs` | New | Persistence | Lookup repository implementation | Mirror `EventStatusRepository`. |
| `Explore.API/Controllers/EventSessionStatusController.cs` | New | API | Public lookup controller | Mirror `EventStatusController` unless implementation proves a newer lookup convention supersedes it. |
| `Explore.Persistence/Configurations/Entities/EventConfiguration.cs` | Existing | Persistence | Event EF mapping and constraints | Keep structural required fields required. |
| `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs` | Existing | Persistence | Session EF mapping, constraints, indexes, room exclusion | Highest-risk change if schedule becomes nullable. |
| `Explore.Persistence/Seed/LookupTableSeeder.cs` | Existing | Persistence | Lookup seeding | Add session statuses with stable `MasterCode`. |
| `Explore.Persistence/Repositories/EventRepository.cs` | Existing | Persistence | Event entity queries/spec application | Preserve entity returns and tenant filters. |
| `Explore.Persistence/Repositories/EventSessionRepository.cs` | Existing | Persistence | Session queries and room overlap guards | Must handle nullable schedule and public visibility. |
| `Explore.Application/DTOs/Event/Validators/CreateEventRequestValidator.cs` | Existing | Application | Draft-friendly event create validation | Already allows minimal title-only drafts. |
| `Explore.Application/Services/EventPublishReadinessEvaluator.cs` | Existing | Application | Minimal publish readiness | Replace/evolve into policy-aware evaluator. |
| `Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs` | Existing | Application | Publish transition and outbox creation | Outbox must remain after readiness. |
| `Explore.Application/Features/Events/Handlers/Commands/UpdateEventCommandHandler.cs` | Existing | Application | Generic status update and DTO updates | Should be constrained or replaced for lifecycle transitions. |
| `Explore.Application/Features/EventSessions/**` | Existing | Application | Session CQRS handlers/DTOs/validators | Needs draft/schedule/publish profiles. |
| `Explore.API/Controllers/EventController.cs` | Existing | API | Event routes | Existing draft create/update/publish/readiness routes. |
| `Explore.API/Controllers/EventSessionController.cs` | Existing | API | Session routes | No lifecycle/status routes yet. |
| `Explore.API/Hateoas/Policies/EventLinkPolicy.cs` | Existing | API/HAL | Event affordance links | Must expose/hide lifecycle actions by auth and state. |
| `Explore.API/Hateoas/Policies/EventSessionLinkPolicy.cs` | Existing | API/HAL | Session affordance links | Must expose/hide session transition actions. |
| `docs/API_CHANGELOG.md` | Existing | Docs | Public API behavior changes | Required for breaking/additive API changes. |
| `schemas/islamu-event.md` | Existing | Docs | Schema reference | Required by `add-ef-migration` intent. |

## Key Decisions

- Keep one `Event` aggregate. Do not add `EventDraft`.
- Add `EventSessionStatus` instead of `EventSessionDraft`.
- `EventSession.StartTime` and `EventSession.EndTime` must become nullable for draft-capable sessions.
- A published `Event` may own draft/internal `EventSession` rows; public queries must hide them unless an explicit authorized internal query asks for them.
- `EventSessionStatus` should mirror the existing `EventStatus` lookup implementation pattern: domain lookup entity, enum, EF config, DbSet, seed, repository/interface, DI registration, Application lookup DTO/query flow, and lookup API.
- Keep structural DB requirements required: tenant, owner Actor/event parent, status, title where already required, lookup FKs that are always required.
- Put lifecycle completeness in Application/Domain transition logic, not broad DB `NOT NULL` constraints.
- Use controlled validation profiles and field keys. Do not introduce a generic rules engine in the first slice.
- Public session queries must become status-aware before draft sessions are exposed through API.
- Public session list/detail/by-event queries are now status-aware; keep new anonymous/public session surfaces on the same repository-level gate.
- Authorized management session reads are exposed separately at `GET /api/eventsession/management/by-event/{eventId}` and authorize the parent event with `ResourceKinds.Event` + `AuthorizationActions.Events.ViewManagement`; do not broaden anonymous `/by-event`.
- Blazor session reads stay public-only by default. Existing management surfaces may opt into the authorized internal read through `includeManagedSessions: true` only from HAL-confirmed management context or management-only dialogs; the API remains the hard authorization boundary.
- HAL links remain the UI source of truth for edit/publish/archive/session transition actions.
- Event/session lifecycle APIs are exposed through explicit transition endpoints; session lifecycle currently supports create-draft/schedule/publish/cancel/complete/archive. Submit/approve remain absent until corresponding Application commands and policy decisions exist.
- Event-session moderation is not an independent lifecycle path. Event moderation cascades to all sessions, and heavy moderation redacts session-owned data and clears session images together with the parent event redaction.
- Event-session read DTO schedule fields are nullable to match draft-capable storage; clients must use `IsScheduled` and HAL `_links` rather than treating missing dates as sentinel values.
- Breaking changes are accepted for this pre-v1 development-mode workstream; do not keep compatibility shims for weak lifecycle contracts.

## Constraints And Rules To Remember

- Repositories return entities, not DTOs.
- Validators are manually instantiated.
- Domain stays pure and cannot reference Application/Persistence/API/Blazor.
- Application cannot use `ExploreDbContext` directly.
- Writes are `[Authorize]`; public GETs are only anonymous when safe.
- Tenant filters must not be bypassed casually.
- `Guid` for aggregates, `int` for lookups.
- New C# files require two `ABOUTME:` lines.
- No destructive migration rollback that silently loses data.
- No solution-level `dotnet test`.

## Validation Baseline

Baseline already run:

```bash
dotnet build --configuration Release --verbosity quiet
```

Required for completed implementation:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## Current Known Risks / Unknowns

- Nullable session schedule implementation is the highest-risk persistence change.
- Existing public session endpoints now hide draft/internal sessions through explicit public repository reads.
- OpenAPI/client artifacts have been regenerated for the Phase 4 lifecycle route and DTO contract changes through the scoped API build plus NSwag client target.
- The management session read relies on the existing broad `GetSessionsByEvent(Guid)` repository method after MediatR authorization. Blazor now consumes it through `EventService.GetSessionsByEventAsync(..., includeManagedSessions: true)` only in management contexts; keep future UI/HAL affordances gated by `_links`, not client-side role checks.
- Migration rollback cannot safely restore non-null session times after unscheduled drafts exist.
- Broad solution build currently trips a Blazor WASM `ComputeWasmBuildAssets` task-host failure in this local SDK state. The evidence points to missing `wasm-tools` workload permissions rather than lifecycle source code; keep the focused lifecycle verification evidence separate until the workload is installed.

## Handoff Notes

### Handoff — 2026-06-23 Europe/Brussels (Phase 1 Complete)

- **Current state:** Phase 1 persistence foundation FULLY IMPLEMENTED AND VERIFIED. All 8 tasks (1.1-1.8) complete.
- **Next action:** Start Phase 2 application lifecycle policy (task 2.1: validation profile and field-key model).
- **Blockers:** `RTK.md` missing (non-blocking); import/archive provenance still unresolved (blocks task 2.7 only).
- **Modified files (Phase 1):**
  - Domain: `Explore.Domain/EventSession.cs` (nullable schedule + status FK), `Explore.Domain/Event.cs` (rollup guards), new `Explore.Domain/EventSessionStatus.cs`, new `Explore.Domain/Enums/EventSessionStatusEnum.cs`.
  - Application: new `Explore.Application/Contracts/Persistence/IEventSessionStatusRepository.cs`, new `Explore.Application/DTOs/EventSessionStatus/*`, new `Explore.Application/Features/EventSessionStatuses/**`, existing `Explore.Application/Profiles/LookupMappingProfile.cs`, existing `Explore.Application/Serialization/ExploreJsonContext.cs`.
  - Persistence: new `Explore.Persistence/Configurations/Entities/EventSessionStatusConfiguration.cs`, existing `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`, existing `Explore.Persistence/ExploreDbContext.DbSets.cs`, existing `Explore.Persistence/PersistenceServicesRegistration.cs`, existing `Explore.Persistence/Seed/LookupTableSeeder.cs`, existing `Explore.Persistence/Repositories/EventSessionRepository.cs`, new `Explore.Persistence/Repositories/EventSessionStatusRepository.cs`, new `Explore.Persistence/Migrations/20260623101543_AddEventSessionStatusAndNullableSchedule.cs` + `.Designer.cs` + snapshot.
  - API: new `Explore.API/Controllers/EventSessionStatusController.cs`, existing `Explore.API/Hateoas/RouteNames.cs`.
  - Tests: new `Event.Persistence.IntegrationTests/Repositories/EventSessionLifecycleConstraintTests.cs`, existing `Event.Persistence.IntegrationTests/Fixtures/PostgreSqlContainerFixture.cs`, existing `Event.Persistence.IntegrationTests/Fixtures/ProjectionTestContainerFixture.cs`, existing `Event.Domain.UnitTests/Entities/EventSessionTests.cs`, existing `Event.Application.UnitTests/Common/DataBuilder.cs`, existing `Event.Persistence.IntegrationTests/Repositories/SchedulingConstraintTests.cs`.
  - Dev docs: `dev/active/event-lifecycle-validation-policy/*` (this update).
- **Validation:** Build 0 errors; Domain 287/287 ✓; Architecture 194/195 ✓ (1 skipped); Persistence 166/166 ✓.
- **Documentation impact:** `docs/DOMAIN.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `schemas/islamu-event.md` still need updates (Phase 5 tasks 5.1-5.3).
- **Risks:** Phase 2 must add public session visibility filtering (task 3.1) before draft sessions are exposed through API. Current generic status update endpoint can bypass lifecycle policy until task 2.8.
- **Notes for next contributor/agent:** Treat `dev/active/Event Draft Lifecycle Architecture Consultation.md` as supporting research only. The operational source of truth is this three-file workstream.

### Handoff — 2026-06-23 Europe/Brussels (Phase 3.1 Complete)

- **Current state:** Phase 3.1 public session visibility filtering implemented and verified. Anonymous `GET /api/eventsession`, `GET /api/eventsession/{id}`, and `GET /api/eventsession/by-event/{eventId}` now return only scheduled, published sessions under publicly discoverable parent events.
- **Next action:** Start task 3.3: update event date/schedule rollups so draft/rejected/internal sessions do not affect public event schedule summaries.
- **Modified files (Phase 3.1):**
  - Application contract/handlers: `IEventSessionRepository`, `GetEventSessionListRequestHandler`, `GetSessionsByEventRequestHandler`, `GetEventSessionDetailsRequestHandler`.
  - Persistence: `EventSessionRepository` public-read methods and centralized `BuildPublicSessionQuery()` gate.
  - Tests: `EventSessionVisibilityContractTests`, `GetEventSessionDetailsRequestHandlerTests`.
- **Validation:** Build 0 errors; Application 1507/1507 ✓; focused API session visibility 3/3 ✓; Architecture 194/195 ✓ (1 skipped).
- **Known verification caveat:** Full `Event.API.IntegrationTests` is not green independently of this slice; observed failures include ProblemDetails title/code contract drift, HATEOAS expectation drift, Cerbos authorization policy expectations, and S3 test-provider network calls to `s3.example.com`.

### Handoff — 2026-06-25 Europe/Brussels (Phase 3.3 Complete)

- **Current state:** Phase 3.3 public schedule rollups implemented and verified. Event date summary fields now represent published scheduled sessions only.
- **Next action:** Start task 3.4: inspect calendar export, AI reference search, program summary, notification/federation/outbox consumers, and any public schedule projections for internal-session leakage.
- **Modified files (Phase 3.3):**
  - Domain: `EventSession.ContributesToPublicScheduleSummary()`, `Event.RecalculateScheduleSummaryFromSessions()`.
  - Application handlers: `CreateEventCommandHandler`, `ScheduleEventSessionCommandHandler`, `PublishEventSessionCommandHandler`.
  - Tests: `EventScheduleProjectionTests`, `EventSessionLifecycleCommandHandlerTests`, `CreateEventCommandHandlerTests`, `UpdateEventDraftCommandHandlerScheduleTests`.
- **Validation:** Build 0 errors; Domain 293/293 ✓; Application 1511/1511 ✓; Architecture 194/195 ✓ (1 skipped).
- **Known verification caveat:** Full API integration suite was not rerun for this domain/application-only slice; previous Phase 3.1 caveat about unrelated API integration failures still applies.

### Handoff — 2026-06-25 Europe/Brussels (Phase 3.4 Complete)

- **Current state:** Phase 3.4 public boundary filtering implemented and verified. Calendar export, program summary, public agenda projection, and public event-list session facets now exclude draft/internal sessions.
- **Next action:** Decide whether task 3.2 is needed for authorized draft-session reads, otherwise start Phase 4 API/HAL lifecycle endpoints.
- **Modified files (Phase 3.4):**
  - Application handlers: `GetEventCalendarExportRequestHandler`, `GetEventProgramSummaryRequestHandler`, `GetEventAgendaProjectionRequestHandler`.
  - Specifications/persistence: `EventFilter`, `EventRepository`.
  - Tests: `GetEventCalendarExportRequestHandlerTests`, `GetEventProgramSummaryRequestHandlerTests`, `GetEventAgendaProjectionRequestHandlerTests`, `EventQuerySpecificationTests`.
- **Validation:** Build 0 errors; Application 1514/1514 ✓; Persistence 178/178 ✓; Architecture 194/195 ✓ (1 skipped).
- **Known verification caveat:** Full API integration suite was not rerun for this application/persistence slice; previous Phase 3.1 caveat about unrelated API integration failures still applies.

### Handoff — 2026-06-25 Europe/Brussels (Phase 3.2 Complete)

- **Current state:** Authorized internal draft-session reads are implemented and verified. Anonymous public event-session routes remain filtered; `GET /api/eventsession/management/by-event/{eventId}` is authenticated and dispatches an authorized MediatR query that can return draft/internal sessions for management surfaces.
- **Next action:** Start Phase 4 API/HAL lifecycle endpoints, especially event/session lifecycle route contracts and HAL affordance policies.
- **Modified files (Phase 3.2):**
  - Application request/handler: `GetManagedSessionsByEventRequest` now has `[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]` and `ISecureRequest.ResourceId = EventId`; `GetManagedSessionsByEventRequestHandler` maps entity results from the broad management repository read.
  - API: `EventSessionController.GetManagedByEvent` and `RouteNames.GetManagedEventSessionsByEvent`.
  - Tests: `EventSessionControllerTests`, `EventSessionVisibilityContractTests`, `GetManagedSessionsByEventRequestHandlerTests`.
- **Validation:** Build 0 errors; focused Application handler 1/1 ✓; focused EventSession controller route/auth 12/12 ✓; focused session visibility contract 4/4 ✓; Architecture 197/198 ✓ (1 skipped).
- **Known verification caveat:** Full `Event.API.IntegrationTests` was not rerun; focused API tests covered the new route plus the existing public visibility contract.

### Handoff — 2026-06-25 Europe/Brussels (Phase 4.5 + Phase 5 Complete)

- **Current state:** Generated lifecycle contract artifacts and documentation are aligned with the event/session lifecycle implementation. `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, and `Explore.Blazor.Client/Clients/EventApiClient.g.cs` expose the import, draft-session, schedule-session, and publish-session operations plus nullable draft-capable session fields.
- **Next action:** No backend lifecycle task remains. Resolve the unrelated Blazor WASM full-solution build issue separately, or start deferred lifecycle UI/profile-configuration work after product approval.
- **Modified files (Phase 4.5/5):**
  - Contracts: `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
  - Docs/schema: `docs/DOMAIN.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `schemas/islamu-event.md`.
  - Workstream docs: `dev/active/event-lifecycle-validation-policy/event-lifecycle-validation-policy-context.md`, `dev/active/event-lifecycle-validation-policy/event-lifecycle-validation-policy-tasks.md`.
- **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity minimal --no-restore -maxcpucount:1` passed; `dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal` passed; publish-readiness Application tests 3/3; lifecycle API/HAL tests 9/9; session visibility and wrong-tenant management-read isolation tests 5/5; Architecture 197/198 with the known skipped API metadata test.
- **Known verification caveat:** Broad `dotnet build --configuration Release --verbosity quiet` currently reaches a Blazor WASM `ComputeWasmBuildAssets` task-host failure unrelated to the lifecycle contract generation path. Local evidence: SDK `10.0.300`, no installed workloads, official `wasm-tools` prerequisite missing, and `dotnet workload install wasm-tools` requires OS-level permission on `/usr/share/dotnet`.

### Handoff — 2026-06-25 Europe/Brussels (Event-Session Terminal Lifecycle + Moderation Cascade)

- **Current state:** The requested session lifecycle/moderation UX slice is implemented. Sessions now have `Completed` and `Moderated` lookup states, explicit cancel/complete/archive commands and API/HAL links, event-level moderation cascade, heavy-redaction session cleanup, a multi-session publish-selection dialog, multi-session-only session detail links, and a session detail page without moderation controls.
- **Modified files:**
  - Backend lifecycle: `Explore.Domain/Enums/EventSessionStatusEnum.cs`, `Explore.Persistence/Seed/LookupTableSeeder.cs`, `Explore.Persistence/Migrations/20260625170000_AddEventSessionTerminalStatuses.cs`, `Explore.Application/Features/EventSessions/Requests/Commands/*EventSessionCommand.cs`, `Explore.Application/Features/EventSessions/Handlers/Commands/*EventSessionCommandHandler.cs`, `EventSessionLifecycleTransitionCommandHandlerBase.cs`.
  - Moderation cascade: `ModerateEventCommandHandler.cs`, `EventHeavyRedactionApplicator.cs`.
  - API/HAL/contracts: `EventSessionController.cs`, `RouteNames.cs`, `LinkRelations.cs`, `EventSessionLinkPolicy.cs`, `ExploreJsonContext.cs`, `EventApiClient.g.cs`, `EventService.cs`, serializer contexts.
  - Blazor UX: `EventDetail.razor.cs`, `EventSessionPublishSelectionDialog.razor(.css)`, `EventSessionManager.razor`, `EventSessionDetail.razor(.css)`, `Routes.razor`.
  - Tests/docs: lifecycle command tests, moderation/redaction tests, HAL tests, Blazor route/component tests, `docs/DOMAIN.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `schemas/islamu-event.md`, and this workstream.
- **Verification:** Focused Application lifecycle/moderation/redaction test slice passed 10/10. Focused Blazor route tests passed 9/9, and the combined EventSessionManager/routes filter selected and passed 1/1. Focused API/HAL lifecycle tests passed 4/4. API contract inventory generation passed 1/1 and refreshed `docs/API_CONTRACT_INVENTORY.md`. `git diff --check` passed. Broad `dotnet build --configuration Release --verbosity quiet` still fails before diagnostics with 0 warnings and 0 errors after 2 projects; earlier direct Blazor client verification also hit the known local WebAssembly task-host path after the nullable dialog issue was fixed.
- **Next action:** Repair local build graph/toolchain first, then rerun the focused Application/API/Blazor lifecycle tests and regenerate OpenAPI/client artifacts if the canonical generator is available.
