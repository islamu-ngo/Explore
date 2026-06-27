<!-- ABOUTME: Tactical checklist for implementing event lifecycle validation and nullable session draft storage. -->
<!-- ABOUTME: Tracks phase status, acceptance criteria, validation commands, and remaining work for future agents. -->

# Event Lifecycle Validation Policy — Task Checklist

Last Updated: 2026-06-25 Europe/Brussels

## Status Summary

- **Overall status:** Phase 4 lifecycle API/HAL/OpenAPI contracts, Phase 5 documentation updates, Blazor authorized internal draft-session reads, and the event-session publication/moderation follow-up are implemented.
- **Completed:** Phase 0 planning, Phase 1 tasks 1.1-1.8, Phase 2 tasks 2.1-2.9, Phase 3 tasks 3.1-3.4, Phase 4 tasks 4.1-4.5, Phase 5 tasks 5.1-5.4, Blazor session-read consumption, and the 2026-06-25 event-session terminal lifecycle/moderation/publish-selection slice.
- **Current priority:** Source implementation is complete for the requested slice; verification is partially blocked by current local MSBuild/package-graph and Blazor WebAssembly task-host issues recorded below.
- **Next recommended slice:** Resolve local verification blockers, then refresh generated OpenAPI/client artifacts through the canonical workflow if the API contract generator becomes available.

## Implementation Maintenance Rules

- [x] Before starting work, read plan/context/tasks.
- [x] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope or architecture, update the plan before continuing.
- [x] If discoveries affect future work, update the context file.
- [x] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.

## Phase 0: Plan Review And Baseline ✅ RE-BASELINED

- [x] **0.1 User reviews plan and approves or corrects scope**
  - **Files:** `dev/active/event-lifecycle-validation-policy/event-lifecycle-validation-policy-plan.md`
  - **Acceptance:** planning status changes from Draft to User-reviewed/Approved or the plan is re-baselined.
  - **Validation:** context handoff records user decision.
  - **Effort:** S
  - **Dependencies:** none

- [x] **0.2 Confirm session schedule nullability**
  - **Files:** plan Section 3 and Section 6
  - **Acceptance:** decision recorded: `EventSession.StartTime`/`EndTime` are nullable for draft-capable sessions; no provisional/fake schedule values.
  - **Validation:** context `Key Decisions` updated.
  - **Effort:** S
  - **Dependencies:** 0.1

- [x] **0.3 Confirm `EventSessionStatus` taxonomy**
  - **Files:** plan Section 3, future `Explore.Domain/Enums/EventSessionStatusEnum.cs`
  - **Acceptance:** stable initial status codes and default public visibility mapping recorded.
  - **Validation:** tasks updated with final status list.
  - **Effort:** S
  - **Dependencies:** 0.1

- [x] **0.4 Define import/archive provenance requirements before Phase 2** ✅
  - **Files:** future import/archive DTOs
  - **Acceptance:** minimum provenance fields are defined before import/archive command/API work: `ProvenanceSource` and `ProvenanceExternalId`.
  - **Validation:** `ImportEventRequestDto`, `Event.ProvenanceSource`, and `Event.ProvenanceExternalId` exist.
  - **Effort:** S
  - **Dependencies:** before task 2.7

## Phase 1: Persistence Foundation ✅ COMPLETE (2026-06-23)

- [x] **1.1 Add `EventSessionStatus` domain lookup** ✅
  - **Files:** new `Explore.Domain/EventSessionStatus.cs`, new `Explore.Domain/Enums/EventSessionStatusEnum.cs`, existing `Explore.Domain/EventSession.cs`, new `Explore.Application/Contracts/Persistence/IEventSessionStatusRepository.cs`, new `Explore.Persistence/Repositories/EventSessionStatusRepository.cs`
  - **Acceptance:** session entity has required `EventSessionStatusId` and navigation; lookup IDs are `int`; repository pattern mirrors `IEventStatusRepository`/`EventStatusRepository`.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet`
  - **Effort:** M
  - **Dependencies:** none

- [x] **1.2 Add EF configuration and DbSet for session statuses** ✅
  - **Files:** new `Explore.Persistence/Configurations/Entities/EventSessionStatusConfiguration.cs`, existing `Explore.Persistence/ExploreDbContext.DbSets.cs`, existing `Explore.Persistence/PersistenceServicesRegistration.cs`
  - **Acceptance:** lookup uses `ValueGeneratedNever`, required `MasterCode`/`FullName`, same max lengths as `EventStatusConfiguration`, and DI registration is present if repository is added.
  - **Validation:** `Event.Persistence.IntegrationTests` lookup/seed test added or updated.
  - **Effort:** M
  - **Dependencies:** 1.1

- [x] **1.3 Seed `EventSessionStatus` lookup rows** ✅
  - **Files:** existing `Explore.Persistence/Seed/LookupTableSeeder.cs`
  - **Acceptance:** stable rows inserted idempotently with codes `DRAFT`, `SUBMITTED`, `UNDER_REVIEW`, `APPROVED`, `PUBLISHED`, `REJECTED`, `CANCELLED`, `ARCHIVED`, `COMPLETED`, `MODERATED`.
  - **Validation:** persistence seed test verifies lookup rows.
  - **Effort:** S
  - **Dependencies:** 1.2

- [x] **1.3A Add `EventSessionStatus` lookup DTO/query/API parity** ✅
  - **Files:** new `Explore.Application/DTOs/EventSessionStatus/*`, new `Explore.Application/Features/EventSessionStatuses/**`, existing `Explore.Application/Profiles/*`, existing `Explore.Application/Serialization/ExploreJsonContext.cs`, existing `Explore.API/Hateoas/RouteNames.cs`, new `Explore.API/Controllers/EventSessionStatusController.cs`
  - **Acceptance:** client-facing lookup behavior mirrors `EventStatusController`: anonymous GET list/detail, output cache policy, route names, explicit response types, DTO mapping, JSON source-generation entries if required.
  - **Validation:** API lookup authorization/contract tests mirror existing event-status lookup tests.
  - **Effort:** M
  - **Dependencies:** 1.1-1.3

- [x] **1.4 Make session schedule fields nullable for drafts** ✅
  - **Files:** existing `Explore.Domain/EventSession.cs`, existing `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`
  - **Acceptance:** unscheduled draft sessions can represent null start/end/local projections without fake values; scheduled/published sessions still require real start/end through Application validation.
  - **Validation:** domain and persistence tests for unscheduled draft sessions.
  - **Effort:** L
  - **Dependencies:** 1.1

- [x] **1.5 Update session schedule methods and event rollups** ✅
  - **Files:** existing `Explore.Domain/EventSession.cs`, existing `Explore.Domain/Event.cs`
  - **Acceptance:** `Reschedule` remains the only scheduled write path; rollups ignore unscheduled sessions.
  - **Validation:** `Event.Domain.UnitTests` for rollups and session scheduling.
  - **Effort:** M
  - **Dependencies:** 1.4

- [x] **1.6 Update room overlap query and exclusion metadata** ✅
  - **Files:** existing `Explore.Persistence/Repositories/EventSessionRepository.cs`, existing `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`
  - **Acceptance:** unscheduled sessions do not participate in overlap; scheduled conflicts remain blocked.
  - **Validation:** `SchedulingConstraintTests` covers unscheduled, adjacent, and overlapping scheduled cases.
  - **Effort:** L
  - **Dependencies:** 1.4

- [x] **1.7 Generate EF migration** ✅
  - **Files:** new `Explore.Persistence/Migrations/*`, existing `Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs`
  - **Acceptance:** migration adds lookup/status FK, backfills existing sessions, updates nullable columns/constraints/indexes.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet`
  - **Effort:** L
  - **Dependencies:** 1.1-1.6

- [x] **1.8 Add persistence migration/constraint tests** ✅
  - **Files:** existing `Event.Persistence.IntegrationTests/Repositories/SchedulingConstraintTests.cs`, possible new status test file
  - **Acceptance:** tests prove nullable schedule shape, status FK, room overlap, existing row compatibility, and adding a draft session under a published event.
  - **Validation:** `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - **Effort:** L
  - **Dependencies:** 1.7

## Phase 2: Application Lifecycle Policy ✅ COMPLETE

- [x] **2.1 Add validation profile and field-key model** ✅
  - **Files:** `Explore.Application/Services/Lifecycle/ValidationProfile.cs`, `EventFieldKey.cs`, `EventSessionFieldKey.cs`
  - **Acceptance:** controlled profiles exist for draft, native, import, archive, publish, session draft, session schedule, session publish.
  - **Validation:** `EventLifecycleReadinessEvaluatorTests`.
  - **Effort:** M
  - **Dependencies:** Phase 1

- [x] **2.2 Add effective lifecycle policy provider** ✅
  - **Files:** `IEventLifecyclePolicyProvider.cs`, `EventLifecyclePolicyProvider.cs`, `EventLifecyclePolicy.cs`, `ApplicationServicesRegistration.cs`
  - **Acceptance:** hard invariants are composed centrally and tenant-scoped lookup preserves hard requirements for future tightening.
  - **Validation:** `EventLifecyclePolicyProviderTests`.
  - **Effort:** L
  - **Dependencies:** 2.1

- [x] **2.3 Evolve publish readiness evaluator** ✅
  - **Files:** `Explore.Application/Services/Lifecycle/EventLifecycleReadinessEvaluator.cs`, `LifecycleReadinessResult.cs`, `LifecycleReadinessError.cs`, `ReadinessErrorSeverity.cs`, `ReadinessErrorSource.cs`, existing `EventPublishReadinessDto.cs`, existing `EventPublishReadinessErrorDto.cs`
  - **Acceptance:** readiness result includes machine-readable missing fields by source/profile and preserves existing baseline checks; session profile readiness is executable for draft/schedule/publish.
  - **Validation:** `GetEventPublishReadinessRequestHandlerTests`, `EventLifecycleReadinessEvaluatorTests`.
  - **Effort:** L
  - **Dependencies:** 2.1, 2.2

- [x] **2.4 Update publish command handler** ✅
  - **Files:** existing `Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs`
  - **Acceptance:** uses policy-aware readiness, preserves concurrency and outbox boundaries.
  - **Validation:** `PublishEventCommandHandlerTests`.
  - **Effort:** M
  - **Dependencies:** 2.3

- [x] **2.5 Update published-create path** ✅
  - **Files:** existing `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs`
  - **Acceptance:** creating directly as published uses same readiness policy as publish transition.
  - **Validation:** `CreateEventCommandHandlerTests`.
  - **Effort:** M
  - **Dependencies:** 2.3

- [x] **2.6 Add explicit archive/cancel lifecycle commands** ✅
  - **Files:** new `ArchiveEventCommand`/handler/validator, new `CancelEventCommand`/handler/validator or approved subset
  - **Acceptance:** lifecycle transitions are explicit, authorized, validated, cache-invalidating, and do not emit publish outbox.
  - **Validation:** `EventLifecycleTransitionCommandHandlerTests`.
  - **Effort:** L
  - **Dependencies:** 2.1

- [x] **2.7 Add import event command** ✅
  - **Files:** new import DTO/command/handler/validator under `Explore.Application/Features/Events`
  - **Acceptance:** import requires provenance and owner/tenant, accepts missing publication-quality fields, applies structural lookup defaults, and stores Draft status unless policy permits otherwise.
  - **Validation:** `ImportEventCommandHandlerTests`.
  - **Effort:** L
  - **Dependencies:** 0.4, 2.1

- [x] **2.8 Constrain or replace generic status update** ✅
  - **Files:** existing `UpdateEventCommandHandler.cs`, `UpdateEventCommand.cs`, `UpdateEventStatusDtoValidator.cs`
  - **Acceptance:** arbitrary status ID mutation cannot bypass transition policy; generic update DTO/command no longer expose status mutation.
  - **Validation:** `EventStatusMutationBypassTests`.
  - **Effort:** M
  - **Dependencies:** 2.6

- [x] **2.9 Add session draft/schedule/publish commands** ✅
  - **Files:** `CreateDraftEventSessionCommand`/handler/validator/DTO, `ScheduleEventSessionCommand`/handler/validator, `PublishEventSessionCommand`/handler/validator/DTO
  - **Acceptance:** draft session can be created under a published event; scheduling and publishing are stricter profile-specific paths.
  - **Validation:** `EventSessionLifecycleCommandHandlerTests`.
  - **Effort:** XL
  - **Dependencies:** Phase 1, 2.1

## Phase 3: Query Visibility And Rollups ✅ COMPLETE

- [x] **3.1 Add public session visibility filtering** ✅
  - **Files:** `Explore.Application/Features/EventSessions/Handlers/Queries/*`, `Explore.Persistence/Repositories/EventSessionRepository.cs`, possible `EventSessionFilter`
  - **Acceptance:** anonymous list/detail/by-event queries hide draft/internal sessions and draft/private parent events.
  - **Validation:** `EventSessionVisibilityContractTests` covers anonymous list, detail, and by-event responses.
  - **Effort:** L
  - **Dependencies:** 1.1, 2.9

- [x] **3.2 Add authorized internal session query path if needed** ✅
  - **Files:** `GetManagedSessionsByEventRequest`, `GetManagedSessionsByEventRequestHandler`, `EventSessionController.GetManagedByEvent`, `RouteNames.GetManagedEventSessionsByEvent`
  - **Acceptance:** internal query is authorized through parent-event `view-management` policy and remains separate from anonymous public session routes.
  - **Validation:** API integration tests cover anonymous denial, route authorization metadata, and authenticated draft-session visibility; application unit test proves the handler uses the broad management repository read.
  - **Effort:** M
  - **Dependencies:** 3.1

- [x] **3.3 Update event schedule rollups to respect session status** ✅
  - **Files:** `Explore.Domain/Event.cs`, related Application handlers/services
  - **Acceptance:** rejected/draft/unscheduled sessions do not affect public event date summary.
  - **Validation:** domain/application unit tests pass; build and architecture tests pass.
  - **Effort:** M
  - **Dependencies:** 1.5

- [x] **3.4 Verify export/search/notification/federation public boundaries** ✅
  - **Files:** calendar export handler, AI reference search, publish outbox factory/use sites, program summary handlers
  - **Acceptance:** internal sessions do not leak into public export/search/fanout payloads.
  - **Validation:** application unit tests, persistence integration tests, build, and architecture tests pass.
  - **Notes:** Calendar export, program summary, and public agenda projection now use public-session reads and fail closed for non-published/non-public parent events. Event discovery location/language/registration facet filters apply the published/scheduled session predicate when the specification includes `PubliclyDiscoverable()`. AI reference search already uses public-discoverable event filtering, and publish outbox/fanout payloads derive from the Phase 3.3 public schedule rollup rather than raw hidden sessions.
  - **Effort:** L
  - **Dependencies:** 3.1, 3.3

## Phase 4: API And HAL Contracts ✅ COMPLETE

- [x] **4.1 Add event lifecycle API endpoints** ✅
  - **Files:** `Explore.API/Controllers/EventController.cs`, `Explore.API/Hateoas/RouteNames.cs`
  - **Acceptance:** archive/import/cancel/publish/readiness routes are explicit, authorized, named, classified, and response-typed.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet`; `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --treenode-filter "/*/*/GetEventPublishReadinessRequestHandlerTests/*" --minimum-expected-tests 1` 3/3; `dotnet test Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --treenode-filter "/*/*/EventsControllerTests/*|/*/*/EventSessionControllerTests/*|/*/*/EventLifecycleHateoasPolicyTests/*" --minimum-expected-tests 1` 16/16.
  - **Notes:** Added `POST /api/event/import`; publish-readiness is now MediatR resource-authorized with `ResourceKinds.Event` + `AuthorizationActions.Update`; publish/archive/cancel metadata now declares 403/404.
  - **Effort:** L
  - **Dependencies:** Phase 2

- [x] **4.2 Add session lifecycle API endpoints** ✅
  - **Files:** `Explore.API/Controllers/EventSessionController.cs`, `RouteNames.cs`
  - **Acceptance:** draft/schedule/publish/cancel/complete/archive routes match approved lifecycle subset.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet`; focused API route/anonymous tests 16/16.
  - **Notes:** Approved implemented subset is `POST /api/eventsession/drafts`, `POST /api/eventsession/{id}/schedule`, `POST /api/eventsession/{id}/publish`, `POST /api/eventsession/{id}/cancel`, `POST /api/eventsession/{id}/complete`, and `POST /api/eventsession/{id}/archive`. No submit/approve routes were invented because no Application commands exist for them yet.
  - **Effort:** L
  - **Dependencies:** 2.9, 3.1

- [x] **4.3 Update HAL event affordances** ✅
  - **Files:** `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
  - **Acceptance:** event links expose publish/archive/cancel/create-session-draft only when state and auth allow.
  - **Validation:** `EventLifecycleHateoasPolicyTests` 4/4 as part of the focused 16-test API run.
  - **Notes:** Existing `add-session` now points at the draft lifecycle route and a new explicit `create-session-draft` relation is emitted with create permission metadata.
  - **Effort:** M
  - **Dependencies:** 4.1, 4.2

- [x] **4.4 Update HAL session affordances** ✅
  - **Files:** `Explore.API/Hateoas/Policies/EventSessionLinkPolicy.cs`
  - **Acceptance:** session links expose schedule/publish/cancel/complete/archive only when state and auth allow.
  - **Validation:** `EventLifecycleHateoasPolicyTests` 4/4 as part of the focused 16-test API run.
  - **Notes:** Implemented current command-backed subset: `schedule` for non-terminal sessions, `publish` for scheduled non-terminal non-published sessions, `cancel` for active states, `complete` for published sessions, and `archive` for draft/cancelled/completed sessions. Session read DTOs expose status, concurrency, nullable schedule fields, and `IsScheduled` for HAL state decisions. No event-session moderation affordance is emitted; moderation remains event-scoped.
  - **Effort:** M
  - **Dependencies:** 4.2

- [x] **4.5 Update OpenAPI/client contract artifacts if required** ✅
  - **Files:** generated OpenAPI/client files as determined by repo workflow
  - **Acceptance:** generated client and API contract tests reflect new canonical routes and DTOs.
  - **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity minimal --no-restore -maxcpucount:1` passed; `dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal` passed; focused Application/API/Architecture verification passed.
  - **Notes:** Generated artifacts include `ImportEvent`, `CreateDraftEventSession`, `ScheduleEventSession`, `PublishEventSession`, nullable session schedule/local fields, `isScheduled`, `concurrencyStamp`, and session status fields.
  - **Effort:** M
  - **Dependencies:** 4.1-4.4

## Phase 5: Documentation And Operations ✅ COMPLETE

- [x] **5.1 Update domain documentation** ✅
  - **Files:** `docs/DOMAIN.md`, `schemas/islamu-event.md`
  - **Acceptance:** docs describe nullable DB fields, session status, conditional schedule constraints, and structural vs lifecycle validation.
  - **Validation:** docs review.
  - **Notes:** Documented `EventSessionStatus`, draft/internal unscheduled sessions, nullable schedule/local projections, conditional overlap constraints, and DBML `event_session_statuses`/FK shape.
  - **Effort:** M
  - **Dependencies:** Phase 1

- [x] **5.2 Update API documentation and changelog** ✅
  - **Files:** `docs/API.md`, `docs/API_CHANGELOG.md`
  - **Acceptance:** lifecycle routes, DTO changes, ProblemDetails behavior, and breaking changes are documented.
  - **Validation:** API docs review and API tests.
  - **Notes:** Documented event/session lifecycle routes, public vs management session reads, nullable generated-client fields, generated lifecycle methods, and breaking client migration guidance.
  - **Effort:** M
  - **Dependencies:** Phase 4

- [x] **5.3 Update operations/testing notes** ✅
  - **Files:** `docs/OPERATIONS.md`, `docs/TESTING.md` if needed
  - **Acceptance:** migration/backfill/rollback caveats and test commands are discoverable.
  - **Validation:** docs review.
  - **Notes:** Added lifecycle migration notes for the `event_session_statuses`/nullable schedule migration, partial room-overlap predicate, rollback caveat, and focused lifecycle verification commands.
  - **Effort:** S
  - **Dependencies:** Phase 1, Phase 4

- [x] **5.4 Refresh this dev-doc workstream** ✅
  - **Files:** all files under `dev/active/event-lifecycle-validation-policy/`
  - **Acceptance:** plan/context/tasks reflect actual implementation and remaining work.
  - **Validation:** manual review complete; final focused verification results appended below.
  - **Effort:** S
  - **Dependencies:** all completed implementation slices

## Follow-Up: Blazor Authorized Internal Draft-Session Reads ✅ COMPLETE

- [x] **B1 Consume management session reads from Blazor service** ✅
  - **Files:** `Explore.Blazor.Client/Services/EventService.cs`
  - **Acceptance:** `GetSessionsByEventAsync` keeps public-only behavior by default, optionally reads `GetManagedEventSessionsByEventAsync` for management contexts, merges managed duplicates over public rows, and fails closed to public data on unauthorized/not-found management responses.
  - **Validation:** `EventServiceTests` focused run 56/56.

- [x] **B2 Pass management read intent only from HAL-confirmed management surfaces** ✅
  - **Files:** `EventDetail.razor(.cs)`, `EventEdit.razor.cs`, `ProgramSectionsDialog.razor`, `DeleteEventDialog.razor`, `EventSessionManager.razor`
  - **Acceptance:** public preview/list flows use the default public route; event detail/edit/program-management/delete flows request internal sessions only from server-provided HAL management context or explicit management dialog context.
  - **Validation:** bUnit `EventSessionManagerTests` focused run 1/1.

- [x] **B3 Render draft/internal sessions without blank schedule/location text** ✅
  - **Files:** `EventSessionManager.razor`
  - **Acceptance:** internal draft sessions display status, `Schedule TBD`, and `Location TBD` instead of empty fields when nullable draft schedule/location data is absent.
  - **Validation:** bUnit `EventSessionManagerTests` focused run 1/1.

## Follow-Up: Event-Session Publication, Terminal States, And Moderation Cascade ✅ IMPLEMENTED

- [x] **C1 Add session terminal lifecycle states and transition commands** ✅
  - **Files:** `Explore.Domain/Enums/EventSessionStatusEnum.cs`, `LookupTableSeeder.cs`, `20260625170000_AddEventSessionTerminalStatuses.cs`, `ArchiveEventSessionCommand*`, `CancelEventSessionCommand*`, `CompleteEventSessionCommand*`, `EventSessionLifecycleTransitionCommandHandlerBase.cs`
  - **Acceptance:** sessions support `Cancelled`, `Completed`, `Archived`, and `Moderated`; complete requires a published parent event and a published session; publish remains blocked unless the parent event is published.
  - **Validation:** focused `EventSessionLifecycleCommandHandlerTests` slice passed before later MSBuild/package-graph verification blockage.

- [x] **C2 Cascade event moderation to sessions** ✅
  - **Files:** `ModerateEventCommandHandler.cs`, `EventHeavyRedactionApplicator.cs`, moderation unit tests
  - **Acceptance:** light event moderation moves all event sessions to `Moderated`; heavy event moderation also redacts session text/custom-property data to `Redacted`, clears session image references, and moves sessions to `Moderated`; sessions have no independent moderation command or HAL affordance.
  - **Validation:** focused moderation unit tests passed before later MSBuild/package-graph verification blockage.

- [x] **C3 Add HAL/API surface for terminal session actions** ✅
  - **Files:** `EventSessionController.cs`, `RouteNames.cs`, `LinkRelations.cs`, `EventSessionLinkPolicy.cs`, `ExploreJsonContext.cs`, `EventLifecycleHateoasPolicyTests.cs`
  - **Acceptance:** authenticated POST endpoints exist for session cancel/complete/archive; HAL emits only state-valid lifecycle links and emits no session moderation relation.
  - **Validation:** focused HAL tests added; current API integration build is blocked by the local dependency graph issue recorded below.

- [x] **C4 Add progressive-disclosure publish UX and session detail navigation** ✅
  - **Files:** `EventDetail.razor.cs`, `EventSessionPublishSelectionDialog.razor(.css)`, `EventSessionManager.razor`, `EventSessionDetail.razor(.css)`, `Routes.razor`, `EventService.cs`, `EventApiClient.g.cs`
  - **Acceptance:** publishing a multi-session event opens a dialog to publish all/select sessions/keep drafts; single-session events avoid the dialog. Multi-session agenda rows link to event-session details; single-session events do not. The session detail page has a topbar with parent-event navigation and HAL-gated edit/publish/cancel/complete/archive actions, with no moderation button.
  - **Validation:** direct Blazor client build reached Razor compilation and the nullable `Guid?` issue in the dialog was fixed; subsequent local verification is blocked by the WebAssembly task-host issue recorded below.

## Verification Checklist

- [ ] Broad `dotnet build --configuration Release --verbosity quiet` is currently blocked. Latest run exits `Build FAILED` with 0 warnings and 0 errors after 2 projects; earlier direct Blazor client builds reached the local WebAssembly `ComputeWasmBuildAssets` task-host failure. Evidence from earlier SDK pass: SDK `10.0.300`, no installed workloads, and `dotnet workload install wasm-tools` requires OS-level permission for `/usr/share/dotnet`.
- [x] `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity minimal --no-restore -maxcpucount:1` passes — 7 projects, 0 errors, 4 existing package warnings.
- [x] `dotnet msbuild Explore.Blazor.Client/Explore.Blazor.Client.csproj /t:GenerateApiClient /p:Configuration=Release /p:Restore=false /m:1 /v:minimal` passes — generated client target exits cleanly.
- [x] `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passes — 293/293.
- [x] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passes — 166/166.
- [x] `dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --no-build --treenode-filter "/*/*/*/*"` passes — 197/198 (1 known skipped API metadata test).
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passes — 1511/1511.
- [x] `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --treenode-filter "/*/*/GetEventPublishReadinessRequestHandlerTests/*" --minimum-expected-tests 1` passes — 3/3.
- [x] `dotnet test Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --treenode-filter "/*/*/EventsControllerTests/*|/*/*/EventSessionControllerTests/*|/*/*/EventLifecycleHateoasPolicyTests/*" --minimum-expected-tests 1` passes — 9/9.
- [x] `dotnet test Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --treenode-filter "/*/*/EventSessionVisibilityContractTests/*" --minimum-expected-tests 1` passes — 5/5, including wrong-tenant management-read isolation.
- [x] `dotnet test Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore --treenode-filter "/*/*/EventServiceTests/*" --minimum-expected-tests 1` passes — 56/56.
- [x] `dotnet test Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-build --treenode-filter "/*/*/EventSessionManagerTests/*" --minimum-expected-tests 1` passes — 1/1.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/EventSessionLifecycleCommandHandlerTests/*|/*/*/ModerateEventCommandHandlerTests/*|/*/*/EventHeavyRedactionApplicatorTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passes — 10/10.
- [x] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/EventSessionManagerTests/*|/*/*/RoutesConfigurationTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passes — 1/1 selected by the combined filter.
- [x] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/RoutesConfigurationTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passes — 9/9.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/EventLifecycleHateoasPolicyTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passes — 4/4.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-restore --verbosity quiet -- --treenode-filter "/*/*/*/ApiContractInventory_Generate_WritesMarkdownToDocs" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1 --output Normal --log-level Warning` passes — 1/1 and regenerated `docs/API_CONTRACT_INVENTORY.md`.
- [ ] Build-enabled `EventSessionManagerTests` run hit an unrelated current-worktree compile error in `Explore.Infrastructure/Ai/AiProviderSettings.cs` (`AiProviderDefaults.ProviderIdOpenAiSdk` missing); the no-build run from the rebuilt Blazor test assembly passed.
- [ ] Full `Event.API.IntegrationTests` project has unrelated pre-existing failures in ProblemDetails/HATEOAS/auth/storage tests; not green as a whole.
- [ ] Current follow-up verification caveat: focused Application, API/HAL, and Blazor route/component slices are green, but the broad canonical build still fails before diagnostics (`Build FAILED`, 0 warnings, 0 errors). Do not mark the full repo green until the build graph/toolchain is repaired.
- [x] API/HAL integration tests cover new lifecycle links and missing-link behavior — Phase 4.
- [x] Tenant isolation tests cover wrong-tenant event/session access — Phase 3.
- [x] Dev docs refreshed with final state and remaining work — this update.
- [x] `docs/DOMAIN.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `schemas/islamu-event.md` — Phase 5.

## Remaining / Deferred Work

- Blazor consumption of authorized internal draft-session reads is implemented for existing event/session management surfaces.
- Blazor UI for lifecycle profile configuration remains deferred until product scope is approved.
- Generic tenant-admin policy UI for required field profiles is deferred unless user explicitly includes it in this workstream.
- Federation protocol-specific publication changes are deferred beyond preventing draft/internal data from entering existing public outbox flows.
- Event publication with zero public sessions is deferred unless the user explicitly changes the current readiness rule and outbox payload assumptions.
- Broad full-solution build needs the Blazor WebAssembly workload installed locally (`dotnet workload install wasm-tools`) before rerunning; this is an SDK/toolchain prerequisite, not lifecycle application code.
