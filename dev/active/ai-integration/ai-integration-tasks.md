<!-- ABOUTME: Tactical checklist for the AI integration implementation workstream. -->
<!-- ABOUTME: Tracks phase-by-phase tasks, acceptance criteria, validation, and remaining deferred work. -->

# AI Integration — Task Checklist

Last Updated: 2026-05-29 Europe/Brussels

## Status Summary

- **Overall status:** User-approved implementation in progress.
- **Completed:** Phase 0 approval/baseline/provider decision; Phase 1 domain statuses/entities/tests; Phase 1 EF mappings/query filters/repository surface; Phase 2 provider-neutral Application contracts, expanded AI settings definitions/group, provider settings validator, deterministic fake provider, authenticated bootstrap query/API surface, provider health, egress safety, safe telemetry, and OpenAI-compatible HTTP adapter.
- **Current priority:** Add runtime provider selection/reconciliation while EF migration generation remains deferred by unrelated dirty model changes.
- **Next recommended slice:** Decide and implement runtime selection between static `AiProvider:*` deployment settings and tenant governance `ai_assistant.*`; generate an AI-scoped EF migration only after deciding how to handle unrelated dirty model changes.
- **Implementation started:** Yes.

## Implementation Maintenance Rules

- [x] Before starting implementation, read `ai-integration-plan.md`, `ai-integration-context.md`, and this checklist.
- [x] Re-read `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, matching intents, and path-scoped rules for files being edited.
- [x] After each completed task, update this checklist immediately.
- [ ] If implementation changes scope or architecture, update `ai-integration-plan.md` before continuing.
- [ ] If discoveries affect future work, update `ai-integration-context.md`.
- [ ] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.
- [ ] Do not claim “done” unless all three dev docs reflect actual state.

## Phase 0: Plan Review And Baseline ✅ COMPLETE

- [x] **0.0 Create/re-baseline planning docs**
  - **Files:** `ai-integration-plan.md`, `ai-integration-context.md`, `ai-integration-tasks.md`.
  - **Acceptance:** Three files exist under `dev/active/ai-integration/` with `Last Updated: 2026-05-29 Europe/Brussels` and ABOUTME comments.
  - **Validation:** File creation verified by write operations.
  - **Effort:** S
  - **Dependencies:** none.

- [x] **0.0b Complete current-state investigation**
  - **Files:** source files/docs listed in plan/context.
  - **Acceptance:** Plan section 2 distinguishes verified existing code from planned new code.
  - **Validation:** Repo searches/reads performed; build baseline run.
  - **Effort:** M
  - **Dependencies:** none.

- [x] **0.0c Baseline build during planning**
  - **Files:** none.
  - **Acceptance:** Build result recorded in plan/context.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and existing warnings.
  - **Effort:** S
  - **Dependencies:** none.

- [x] **0.0d Initial provider docs research**
  - **Files:** `ai-integration-plan.md`, `ai-integration-context.md`.
  - **Acceptance:** Current docs findings for `Microsoft.Extensions.AI` and Semantic Kernel are summarized, and the plan records why ISLAMU should still own an internal `IAiChatProvider` abstraction.
  - **Validation:** Context7 attempted; official Microsoft/OpenAI docs used when Context7 was quota-blocked; findings incorporated in plan/context.
  - **Effort:** S
  - **Dependencies:** none.

- [x] **0.0e Senior CTO review and safety resequencing**
  - **Files:** `ai-integration-plan.md`, `ai-integration-context.md`, `ai-integration-tasks.md`.
  - **Acceptance:** Plan explicitly blocks automatic mutating tool invocation, moves provider health/egress/telemetry and abuse/retention gates before broad enablement, and adds auth/resource parity tests.
  - **Validation:** Senior CTO feedback applied to dev docs.
  - **Effort:** S
  - **Dependencies:** 0.0-0.0d.

- [x] **0.1 User reviews plan and approves/corrects scope**
  - **Files:** `ai-integration-plan.md`, `ai-integration-context.md`, `ai-integration-tasks.md`.
  - **Acceptance:** Planning status changes from Draft to User-reviewed/Approved, or corrections are incorporated.
  - **Validation:** User instructed agent to “start implementing”; docs updated to reflect approval.
  - **Effort:** S
  - **Dependencies:** 0.0.

- [x] **0.2 Confirm current repo state before first edit**
  - **Files:** context only unless blockers found.
  - **Acceptance:** Baseline build and `git status --short` recorded; unrelated dirty work identified.
  - **Validation:** `dotnet build --configuration Release --verbosity quiet` passed with 0 errors and 15 existing warnings; `git status --short` showed substantial unrelated dirty work across storage/idempotency/subscription areas.
  - **Effort:** S
  - **Dependencies:** 0.1.

- [x] **0.3 Finalize provider package/adapter decision**
  - **Files:** plan/context updates; no code unless user approves dependency addition.
  - **Acceptance:** Implementation decision recorded for raw OpenAI-compatible HTTP vs Microsoft.Extensions.AI vs Semantic Kernel vs SDK adapter, using initial Context7 findings if available and official docs fallback when Context7 is quota-blocked; automatic mutation tool invocation remains disallowed.
  - **Validation:** Context records an Application-owned `IAiChatProvider`, no Domain provider SDK dependency, deterministic fake provider for tests, and OpenAI-compatible Infrastructure adapter first; Context7 and Tavily were quota/plan blocked.
  - **Effort:** M
  - **Dependencies:** 0.1.

- [ ] **0.4 Re-read related active workstreams**
  - **Files:** `dev/active/event-creation-progressive-disclosure/*`, `dev/active/event-scoped-operational-roles/*`.
  - **Acceptance:** Current event draft DTO/model and role expectations reflected in context before Phase 5.
  - **Validation:** Context update.
  - **Effort:** S
  - **Dependencies:** 0.1.

## Phase 1: Domain And Persistence Foundation ⏳ IN PROGRESS

- [x] **1.1 Create AI domain enums/statuses**
  - **Files:** `Explore.Domain/Ai/AiConversationStatus.cs`, `AiMessageRole.cs`, `AiRunStatus.cs`, `AiProposedActionStatus.cs`, `AiProposedActionKind.cs`, `AiReferenceKind.cs`.
  - **Acceptance:** Statuses support Active/Running/Blocked, Queued/InProgress/Succeeded/Failed/Cancelled, Proposed/Confirmed/Rejected/Executed/Failed; files have ABOUTME comments.
  - **Validation:** `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passed.
  - **Effort:** S
  - **Dependencies:** 0.1.

- [x] **1.2 Create AI aggregate entities**
  - **Files:** `Explore.Domain/Ai/AiConversation.cs`, `AiMessage.cs`, `AiRun.cs`, `AiConversationReference.cs`, `AiProposedAction.cs`, `AiToolExecution.cs`.
  - **Acceptance:** `Guid` aggregate IDs, `long` message sequence/cursors, tenant/actor ownership, lifecycle methods, no Application/Infrastructure dependency.
  - **Validation:** Domain project build and Domain unit tests passed; entities are pure Domain types under `Explore.Domain.Ai`.
  - **Effort:** L
  - **Dependencies:** 1.1.

- [x] **1.3 Add domain lifecycle tests**
  - **Files:** `Event.Domain.UnitTests/Ai/AiConversationTests.cs`, `AiProposedActionTests.cs`.
  - **Acceptance:** Tests cover message ordering, run lifecycle, action transitions, invalid transition errors.
  - **Validation:** `dotnet build Explore.Domain/Explore.Domain.csproj --configuration Release --verbosity quiet` passed with 0 errors/0 warnings. `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passed: 285 total, 0 failed. Architecture tests were attempted but blocked by unrelated dirty persistence compile error `NotificationFanoutRuns` duplicate DbSet. Post-change full solution build was blocked by unrelated dirty Application unit test analyzer/nullability errors.
  - **Effort:** M
  - **Dependencies:** 1.2.

- [x] **1.4 Add EF DbSets/configurations/query filters**
  - **Files:** `Explore.Persistence/ExploreDbContext.DbSets.cs`, `Explore.Persistence/ExploreDbContext.QueryFilters.cs`, `Explore.Persistence/Configurations/Entities/Ai*Configuration.cs`.
  - **Acceptance:** Tenant filters, explicit cascade behavior, indexes for tenant/actor/conversation/status/sequence/created date, explicit JSON payload storage.
  - **Validation:** LSP diagnostics for AI persistence files were clean. `dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passed with 0 errors/0 warnings. Full analyzer build is blocked by unrelated pre-existing `Explore.Application` analyzer errors; migration-backed persistence tests remain pending under 1.6/1.7.
  - **Effort:** L
  - **Dependencies:** 1.2.

- [x] **1.5 Add AI repository contract and implementation**
  - **Files:** `Explore.Application/Contracts/Persistence/IAiConversationRepository.cs`, `Explore.Persistence/Repositories/AiConversationRepository.cs`, `Explore.Persistence/PersistenceServicesRegistration.cs`.
  - **Acceptance:** Repository returns entities only; supports create/update/get/list/get-action; preserves tenant filters; cancellation tokens used.
  - **Validation:** LSP diagnostics were clean for `IAiConversationRepository.cs` and `AiConversationRepository.cs`. No-analyzer Persistence build passed. Repository integration tests are pending until the AI migration is generated and applied.
  - **Effort:** M
  - **Dependencies:** 1.4.

- [ ] **1.6 Add EF migration and schema docs**
  - **Files:** new `Explore.Persistence/Migrations/*AddAiAssistantFoundation*.cs`, `ExploreDbContextModelSnapshot.cs`, `schemas/islamu-event.md`.
  - **Acceptance:** Migration scoped to AI tables; schema docs list table purpose; no unrelated migration churn. Current shared worktree includes unrelated model changes, so migration generation should be deferred or explicitly approved to avoid mixing workstreams.
  - **Validation:** `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` after migration is generated.
  - **Effort:** M
  - **Dependencies:** 1.4.

- [ ] **1.7 Add AI persistence tests**
  - **Files:** `Event.Persistence.IntegrationTests/Repositories/AiConversationRepositoryTests.cs`.
  - **Acceptance:** Create/list/get/update, ordering, cursor pagination, action retrieval, and cross-tenant isolation covered.
  - **Validation:** Persistence integration tests.
  - **Effort:** M
  - **Dependencies:** 1.5, 1.6.

## Phase 2: Provider, Configuration, Bootstrap ⏳ IN PROGRESS

- [x] **2.1 Extend AI settings**
  - **Files:** `GovernanceSettingKeys.cs`, `AiAssistantSettingDefinitions.cs`, `AiAssistantSettingGroup.cs`, `docs/CONFIGURATION.md`.
  - **Acceptance:** Provider/model/limits/retention/tool/streaming/rate settings exist with safe defaults and sensitive metadata.
  - **Validation:** LSP diagnostics were clean for `AiAssistantSettingDefinitions.cs`, `AiAssistantSettingGroup.cs`, and new setting-group tests. `docs/CONFIGURATION.md` documents the new governance keys. `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passed with 0 errors/0 warnings. Targeted `AiAssistantSettingGroupTests` passed: 6 total, 0 failed. Full analyzer builds remain blocked by unrelated pre-existing Application analyzer issues.
  - **Effort:** M
  - **Dependencies:** 0.3.

- [x] **2.2 Add Application provider contracts**
  - **Files:** `Explore.Application/Contracts/Infrastructure/Ai/IAiChatProvider.cs`, `IAiModelCatalog.cs`, `AiChatRequest.cs`, `AiChatResponse.cs`, error/result models.
  - **Acceptance:** No provider SDK types; supports model ID, messages, system prompt, structured action schema, usage metadata, cancellation.
  - **Validation:** LSP diagnostics were clean for `Explore.Application/Contracts/Infrastructure/Ai`. No provider SDK package dependency added; contracts reference only Application primitives and `Explore.Domain.Ai` enums. No-analyzer Application build passed with 0 errors/0 warnings.
  - **Effort:** M
  - **Dependencies:** 0.3.

- [x] **2.3 Add provider validator and fake provider**
  - **Files:** `Explore.Infrastructure/Ai/AiProviderSettings.cs`, `AiProviderSettingsValidator.cs`, `FakeAiChatProvider.cs`, `Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `Explore.Infrastructure.Tests/Infrastructure/AiProviderSettingsValidatorTests.cs`, `FakeAiChatProviderTests.cs`.
  - **Acceptance:** Invalid config detected; fake provider returns deterministic assistant text/action; no secrets logged.
  - **Validation:** LSP diagnostics were clean for `Explore.Infrastructure/Ai`, the new Infrastructure AI tests, and `InfrastructureServicesRegistration.cs`. `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passed after fixing test assertion syntax. `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet -- --no-progress --maximum-parallel-tests 1` passed: 353 total, 0 failed. Two class-specific `--treenode-filter` attempts ran zero tests, so the successful verification used the full Infrastructure test assembly. Full analyzer-enabled Infrastructure builds still surface unrelated existing package/advisory/nullable warnings outside AI.
  - **Effort:** M
  - **Dependencies:** 2.1, 2.2.

- [x] **2.4 Implement OpenAI-compatible adapter**
  - **Files:** `Explore.Infrastructure/Ai/OpenAiCompatibleChatProvider.cs`, `Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `Explore.Infrastructure.Tests/Infrastructure/OpenAiCompatibleChatProviderTests.cs`.
  - **Acceptance:** Configurable endpoint/model/key; timeout/cancellation; safe error mapping; fake HTTP tests; no provider SDK dependency; no raw prompt/content logging; OpenAI tool calls are mapped only into typed proposed-action candidates that still require later persistence and user confirmation.
  - **Validation:** LSP diagnostics were clean for `OpenAiCompatibleChatProvider.cs`, `OpenAiCompatibleChatProviderTests.cs`, and `InfrastructureServicesRegistration.cs`. `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passed with existing non-AI warnings only. `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet -- --no-progress --maximum-parallel-tests 1` passed: 373 total, 0 failed. Tests cover model catalog behavior, request URI/header/body shape, safe response mapping, tool-call-to-`CreateEventDraft` proposal mapping, HTTP failure redaction, provider-not-configured no-HTTP behavior, and timeout mapping.
  - **Effort:** L
  - **Dependencies:** 2.2, 2.3.

- [x] **2.5 Add bootstrap query/DTO**
  - **Files:** `Explore.Application/Contracts/Infrastructure/Ai/AiProviderDefaults.cs`, `Explore.Application/DTOs/Ai/AiAssistantBootstrapDto.cs`, `Explore.Application/Features/AiAssistant/Requests/Queries/GetAiAssistantBootstrapQuery.cs`, `Explore.Application/Features/AiAssistant/Handlers/Queries/GetAiAssistantBootstrapQueryHandler.cs`, `Explore.API/Controllers/AiAssistantController.cs`, `Explore.API/Hateoas/RouteNames.cs`, `Explore.Application/Serialization/ExploreJsonContext.cs`, `Event.Application.UnitTests/Features/AiAssistant/Queries/GetAiAssistantBootstrapQueryHandlerTests.cs`.
  - **Acceptance:** Returns enabled state, disabled reason, tenant-approved model list, default model, feature flags, limits, and HAL `self`; exposes no provider endpoint, API key, prompts, raw provider errors, history, or write/action links.
  - **Validation:** LSP diagnostics were clean for the new bootstrap DTO/query/handler/controller/tests, `ExploreJsonContext`, `RouteNames`, and `AiProviderDefaults`. `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passed with 0 errors/0 warnings. `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passed with existing non-AI package pruning warnings. Targeted TUnit run passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --treenode-filter "/*/*/*GetAiAssistantBootstrapQueryHandlerTests*/*" --minimum-expected-tests 4 --no-progress --maximum-parallel-tests 1` returned 4 total, 0 failed. A first targeted test run failed due an AI test helper using the wrong `ResolvedSetting` constructor; fixed in the test helper.
  - **Effort:** M
  - **Dependencies:** 2.1, 2.2.

- [x] **2.6 Add provider health, egress safety, and safe telemetry**
  - **Files:** `Explore.Infrastructure/Ai/AiProviderSettings.cs`, `Explore.Infrastructure/Ai/AiProviderSettingsValidator.cs`, `Explore.Application/Contracts/Infrastructure/Ai/AiProviderHealth.cs`, `Explore.Infrastructure/Ai/AiProviderHealthReporter.cs`, `Explore.Infrastructure/HealthChecks/AiProviderHealthCheck.cs`, `Explore.Application/Telemetry/BusinessMetrics.cs`, `Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `Explore.API/Program.cs`, `Explore.Infrastructure.Tests/Infrastructure/AiProviderSettingsValidatorTests.cs`, `Explore.Infrastructure.Tests/Infrastructure/AiProviderHealthReporterTests.cs`, `Explore.Infrastructure.Tests/Infrastructure/AiProviderHealthCheckTests.cs`, `Event.Application.UnitTests/Telemetry/BusinessMetricsAiProviderTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`.
  - **Acceptance:** Disabled mode reports healthy-disabled; enabled fake provider reports healthy; enabled OpenAI-compatible settings validate endpoint/model/key and egress constraints; local/private endpoints require explicit opt-in; readiness payload and metrics use bounded metadata only; provider endpoint URLs are deployment/admin-controlled and never browser/per-request controlled; no prompts, secrets, response content, endpoint URLs, model IDs, provider request IDs, or raw provider errors are emitted in health data or metrics.
  - **Validation:** LSP diagnostics were clean for AI provider health files/tests, `BusinessMetrics.cs`, `Explore.API/Program.cs`, `docs/CONFIGURATION.md`, and `docs/OPERATIONS.md`. `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false`, `dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false`, and `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passed with existing non-AI warnings only. `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet -- --no-progress --maximum-parallel-tests 1` passed: 364 total, 0 failed. Targeted AI metrics tests passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --treenode-filter "/*/*/*BusinessMetricsAiProviderTests*/*" --minimum-expected-tests 2 --no-progress --maximum-parallel-tests 1` returned 2 total, 0 failed.
  - **Effort:** M
  - **Dependencies:** 2.1-2.4.

## Phase 3: Conversations, Runs, History, API ⏳ NOT STARTED

- [ ] **3.1 Add AI DTOs**
  - **Files:** `Explore.Application/DTOs/AiAssistant/*`.
  - **Acceptance:** Bootstrap/conversation/message/run/reference/action/request DTOs exist; no secrets/internal errors; cursor pagination consistent.
  - **Validation:** Application/API build.
  - **Effort:** M
  - **Dependencies:** Phase 1.

- [ ] **3.2 Add conversation commands/queries/handlers**
  - **Files:** `Explore.Application/Features/AiAssistant/Requests/{Commands,Queries}/*`, `Handlers/*`.
  - **Acceptance:** Create/list/detail/send/run-status implemented; manual validators; disabled/invalid model/provider failure handled; send idempotency prevents duplicate runs; no event side effect during send.
  - **Validation:** Application unit tests.
  - **Effort:** XL
  - **Dependencies:** 2.2, 2.5, 3.1.

- [ ] **3.3 Add prompt builder and parser**
  - **Files:** `Explore.Application/Features/AiAssistant/Prompting/AiPromptContextBuilder.cs`, `AiSystemPromptFactory.cs`, `AiStructuredActionParser.cs`.
  - **Acceptance:** Bounded context, content boundary markers, strict action allow-list, invalid JSON/unknown action rejected.
  - **Validation:** Application prompt/parser tests.
  - **Effort:** L
  - **Dependencies:** 2.2, 3.1.

- [ ] **3.4 Add AI API controller/routes**
  - **Files:** `Explore.API/Controllers/AiAssistantController.cs`, `Explore.API/Routes/RouteNames.cs`.
  - **Acceptance:** Thin MediatR controller; write endpoints `[Authorize]`; private history GET endpoints authenticated; run-status exists; cancel is deferred unless Phase 7 cancellation is fully implemented; response types/ProblemDetails/route names.
  - **Validation:** API integration tests.
  - **Effort:** L
  - **Dependencies:** 3.1, 3.2.

- [ ] **3.5 Add AI HAL policy**
  - **Files:** `Explore.API/Hateoas/Policies/AiAssistantLinkPolicy.cs`, registration files.
  - **Acceptance:** Links for self/history/send/conversation/result reflect auth, tenant, feature flags, and state; cancel link is omitted until Phase 7 cancellation exists.
  - **Validation:** API HATEOAS tests.
  - **Effort:** M
  - **Dependencies:** 3.4.

- [ ] **3.6 Add conversation/API tests**
  - **Files:** `Event.Application.UnitTests/Features/AiAssistant/*`, `Event.API.IntegrationTests/Features/AiAssistant*Tests.cs`.
  - **Acceptance:** Fake provider flow, auth, disabled assistant, provider failure, private bootstrap/history authenticated, no anonymous history, idempotency, cross-tenant, HAL links.
  - **Validation:** Application and API test commands.
  - **Effort:** L
  - **Dependencies:** 3.2-3.5.

- [ ] **3.7 Update OpenAPI/generated client/changelog**
  - **Files:** `schemas/openapi.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CHANGELOG.md`, maybe `docs/API.md`.
  - **Acceptance:** Generated AI methods available; operation IDs stable; changelog documents auth/idempotency.
  - **Validation:** build + API tests.
  - **Effort:** M
  - **Dependencies:** 3.4.

- [ ] **3.8 Add AI authorization catalog and endpoint safety tests**
  - **Files:** `AuthorizationActions`, `ResourceKinds`, `ResourceDescriptors`, Cerbos/local policy tests, API endpoint classification tests, docs.
  - **Acceptance:** Private GETs are `[Authorize]`/authenticated endpoint class; public bootstrap is safe if split; no anonymous history; resource/action parity tests cover local and Cerbos modes.
  - **Validation:** API/security/architecture tests.
  - **Effort:** M
  - **Dependencies:** 3.4-3.5.

- [ ] **3.9 Add MVP abuse, bounds, and retention gate**
  - **Files:** AI settings, rate limiting policy/API config, handlers/tests, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`.
  - **Acceptance:** Per-user/per-tenant/concurrent-run limits, prompt length, selected reference count, model/tool allow-lists, retention/redaction posture, and no-content logging are enforced before broad enablement; excess returns safe ProblemDetails.
  - **Validation:** API/Application/Operations tests.
  - **Effort:** L
  - **Dependencies:** 2.1, 2.6, 3.2-3.6.

## Phase 4: Event Reference Search ⏳ NOT STARTED

- [ ] **4.1 Add reference DTO/query contracts**
  - **Files:** `AiReferenceSearchResultDto.cs`, `AiSelectedReferenceDto.cs`, `SearchAiReferencesQuery.cs`.
  - **Acceptance:** Event-first reference shape with kind/resource ID/title/snippet/metadata/links; no full event content.
  - **Validation:** Application build.
  - **Effort:** M
  - **Dependencies:** 3.1.

- [ ] **4.2 Add entity-returning event reference query**
  - **Files:** `IEventRepository.cs`, `EventRepository.cs`, optional specification.
  - **Acceptance:** Repository returns entities; bounded/deterministic search; tenant filters preserved; no untested `IgnoreQueryFilters`.
  - **Validation:** Persistence integration tests.
  - **Effort:** M
  - **Dependencies:** 4.1.

- [ ] **4.3 Implement reference handler/API endpoint**
  - **Files:** AI reference handler, `AiAssistantController.cs`, `AiAssistantLinkPolicy.cs`.
  - **Acceptance:** Server maps authorized events to lightweight results; search limits; cross-tenant absence; event links when allowed.
  - **Validation:** API tests.
  - **Effort:** M
  - **Dependencies:** 4.2, 3.4.

- [ ] **4.4 Add reference prompt packer**
  - **Files:** `AiReferencePromptPacker.cs`, application tests.
  - **Acceptance:** Per-reference/total budgets, safe quoting/boundaries, excludes sensitive/internal fields.
  - **Validation:** Application unit tests.
  - **Effort:** M
  - **Dependencies:** 4.2.

- [ ] **4.5 Add reference tests**
  - **Files:** `AiEventReferenceSearchTests.cs`, API reference tests, prompt packer tests.
  - **Acceptance:** Bounded search, cross-tenant isolation, deterministic sorting, prompt stability.
  - **Validation:** Persistence/API/Application tests.
  - **Effort:** M
  - **Dependencies:** 4.2-4.4.

## Phase 5: Confirmed Create Event Draft Tool ⏳ NOT STARTED

- [ ] **5.1 Define CreateEventDraft action payload/mapper**
  - **Files:** `CreateEventDraftAiActionPayload.cs`, `CreateEventDraftAiActionMapper.cs`, parser tests.
  - **Acceptance:** Only safe draft fields; cannot set tenant/actor/published status/privileged fields; org/group IDs revalidated.
  - **Validation:** Application tests.
  - **Effort:** M
  - **Dependencies:** 0.4, 3.3.

- [ ] **5.2 Add confirm/reject commands/handlers**
  - **Files:** `ConfirmAiProposedActionCommand.cs`, `RejectAiProposedActionCommand.cs`, handlers.
  - **Acceptance:** Confirm sends `CreateEventCommand` through MediatR; reject has no side effect; duplicate confirm safe; failures persisted safely.
  - **Validation:** Application tests.
  - **Effort:** L
  - **Dependencies:** 5.1.

- [ ] **5.3 Add confirm/reject API/HAL**
  - **Files:** `AiAssistantController.cs`, `AiAssistantLinkPolicy.cs`, `RouteNames.cs`, `docs/API_CHANGELOG.md`.
  - **Acceptance:** Authorized endpoints; confirm idempotent; links absent when not allowed; safe ProblemDetails.
  - **Validation:** API/HATEOAS tests.
  - **Effort:** M
  - **Dependencies:** 5.2.

- [ ] **5.4 Add full create-draft flow tests**
  - **Files:** `AiAssistantCreateEventDraftFlowTests.cs` or equivalent application/API tests.
  - **Acceptance:** Fake provider proposes draft; send creates no event; confirm creates exactly one draft; reject creates none; validation/auth failures safe.
  - **Validation:** Application/API tests.
  - **Effort:** L
  - **Dependencies:** 5.1-5.3.

## Phase 6: Blazor Right-Side Panel UX ⏳ NOT STARTED

- [ ] **6.1 Add Blazor AI client service**
  - **Files:** `IAiAssistantClientService.cs`, `AiAssistantClientService.cs`, `ServiceCollectionExtensions.cs`.
  - **Acceptance:** Wrap generated client methods for bootstrap/conversations/reference search/send/confirm/reject; central error handling; idempotency key generation; no ad-hoc raw HTTP in components; cancel added only if Phase 7 cancellation exists.
  - **Validation:** Blazor service tests.
  - **Effort:** M
  - **Dependencies:** 3.7, 5.3.

- [ ] **6.2 Extend AI state for conversation UI**
  - **Files:** `AiAssistantState.cs` and/or `AiAssistantConversationState.cs`.
  - **Acceptance:** Tracks selected conversation/model/references/loading/errors; existing open/availability tests pass; no local authz decisions.
  - **Validation:** Blazor state tests.
  - **Effort:** M
  - **Dependencies:** 6.1.

- [ ] **6.3 Replace rail placeholder with assistant layout**
  - **Files:** `AiAssistantRail.razor*`, `AiAssistantHeader.razor`, `AiConversationList.razor`, `AiMessageList.razor`, `AiPromptComposer.razor`.
  - **Acceptance:** MudBlazor UI, BEM-like CSS isolation, keyboard/ARIA support, docked/fixed behavior preserved.
  - **Validation:** bUnit tests + manual smoke.
  - **Effort:** L
  - **Dependencies:** 6.1, 6.2.

- [ ] **6.4 Add event reference picker UI**
  - **Files:** `AiReferencePicker.razor`, `AiReferenceChip.razor`, CSS/tests.
  - **Acceptance:** Debounced search, select/remove chips, loading/empty/error states, keyboard-removable chips.
  - **Validation:** bUnit tests.
  - **Effort:** M
  - **Dependencies:** Phase 4, 6.1.

- [ ] **6.5 Add proposed action/result cards**
  - **Files:** `AiProposedActionCard.razor`, `CreateEventDraftActionPreview.razor`, `AiActionResultCard.razor`.
  - **Acceptance:** Confirm/Reject only render when HAL links exist; double-submit prevented; result links use API links.
  - **Validation:** bUnit HAL-gating tests.
  - **Effort:** L
  - **Dependencies:** Phase 5, 6.1.

- [ ] **6.6 Add full panel bUnit tests**
  - **Files:** `Explore.Blazor.Client.Tests/Components/AiAssistant/*`, existing layout tests as needed.
  - **Acceptance:** Bootstrap, history, model selection, references, send, action cards, confirm/reject, disabled/error states; existing dock bridge tests pass.
  - **Validation:** `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** L
  - **Dependencies:** 6.1-6.5.

## Phase 7: Streaming, Cancellation, Advanced Operations ⏳ NOT STARTED

- [ ] **7.1 Add cancellation semantics**
  - **Files:** AI run handlers/controller/provider/client components.
  - **Acceptance:** Endpoint/API/UI cancellation is added if not already implemented; cancel link appears for cancellable runs only; provider cancellation token honored; cancelled runs produce no actions.
  - **Validation:** Application/API/Blazor tests.
  - **Effort:** M
  - **Dependencies:** Phase 3.

- [ ] **7.2 Decide and implement streaming or polling**
  - **Files:** TBD after decision.
  - **Acceptance:** Decision documented; auth/tenant isolation preserved; non-streaming fallback remains.
  - **Validation:** API/Blazor tests + manual smoke.
  - **Effort:** M-XL
  - **Dependencies:** Core flow complete.

- [ ] **7.3 Tune quotas and retention cleanup after MVP gate**
  - **Files:** AI settings, API policy/config, cleanup job/handler/tests.
  - **Acceptance:** Existing MVP limits remain enforced; cleanup is safe, tenant-scoped, documented, and observable.
  - **Validation:** API tests.
  - **Effort:** M
  - **Dependencies:** 3.9.

- [ ] **7.4 Add advanced provider/run dashboards and runbook polish**
  - **Files:** metrics dashboards/runbook docs, `docs/OPERATIONS.md`, troubleshooting docs if present.
  - **Acceptance:** Builds on Phase 2 health/metrics/logging; no secrets/content in logs; dashboards use low-cardinality dimensions; runbooks cover disabled/misconfigured/unavailable/rate-limited states.
  - **Validation:** Infrastructure/API tests.
  - **Effort:** M
  - **Dependencies:** 2.6.

## Phase 8: Documentation, Credit, Final Validation ⏳ NOT STARTED

- [ ] **8.1 Update API docs/changelog/OpenAPI**
  - **Files:** `docs/API.md`, `docs/API_CHANGELOG.md`, `schemas/openapi.json`.
  - **Acceptance:** AI endpoints, auth, idempotency, HAL, ProblemDetails documented.
  - **Validation:** API tests/build.
  - **Effort:** M
  - **Dependencies:** API endpoints complete.

- [ ] **8.2 Update configuration/self-hosting/operations docs**
  - **Files:** `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, Aspire/Docker docs if touched.
  - **Acceptance:** Provider setup, secrets, model allow-list, limits, retention, health, disable behavior, troubleshooting documented.
  - **Validation:** Docs review/build.
  - **Effort:** M
  - **Dependencies:** Provider/config complete.

- [ ] **8.3 Update Blazor/dock/accessibility docs**
  - **Files:** `docs/BLAZOR.md`, `docs/DOCK_LAYOUT.md`, `docs/ACCESSIBILITY.md` if needed.
  - **Acceptance:** Docs match component names, dock behavior, keyboard/focus/HAL-gated action workflow.
  - **Validation:** Blazor tests/manual smoke.
  - **Effort:** S-M
  - **Dependencies:** Blazor implementation.

- [ ] **8.4 Add Plane inspiration credit**
  - **Files:** `README.md`, optional AI docs/comments.
  - **Acceptance:** Plane credited as AGPL-compatible inspiration; no claim of copied code unless code is actually ported.
  - **Validation:** Docs review.
  - **Effort:** S
  - **Dependencies:** Implementation sufficiently real to describe.

- [ ] **8.5 Run final validation and refresh dev docs**
  - **Files:** all changed files plus `dev/active/ai-integration/*`.
  - **Acceptance:** Required build/tests pass or failures documented; plan/context/tasks reflect actual completed/deferred state.
  - **Validation:** Full command list below.
  - **Effort:** M-L
  - **Dependencies:** all implementation tasks.

## Verification Checklist

- [ ] `dotnet build --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passes.
- [ ] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` passes when UI flow changes require it.
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passes.
- [ ] LSP/compiler diagnostics are clean for modified files or documented as pre-existing.
- [ ] API/OpenAPI/generated client are synchronized.
- [ ] HAL link tests prove UI affordance gating.
- [ ] AI authorization/resource parity tests pass for local and Cerbos modes.
- [ ] Send-message and confirm idempotency tests pass.
- [ ] Provider endpoint validation, safe health, and no-content logging tests pass.
- [ ] MVP quota/prompt/reference/model/tool limits and retention/redaction gate tests pass.
- [ ] No real AI provider calls are required for tests.
- [ ] Docs updated where behavior/config/API/ops changed.
- [ ] Dev docs refreshed with final state and remaining work.

## Remaining / Deferred Work

- Streaming/SSE/SignalR is deferred until after non-streaming core flow succeeds.
- Advanced conversation retention cleanup automation may be deferred, but MVP must ship retention/redaction policy and a broad-enable gate.
- Future tools beyond `CreateEventDraft` are deferred: update event draft, create sessions, agenda items, registration questions, notification drafts.
- Federation-aware AI references are deferred; first release should use local tenant events only.
- Prayer-relative session scheduling is deferred to future `EventSession` tool work.
