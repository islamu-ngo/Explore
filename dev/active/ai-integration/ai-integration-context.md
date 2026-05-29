<!-- ABOUTME: Operational memory for the AI integration implementation workstream. -->
<!-- ABOUTME: Summarizes current state, decisions, validation, risks, and next actions for future agents. -->

# AI Integration — Context

Last Updated: 2026-05-29 Europe/Brussels

## SESSION PROGRESS (2026-05-29 Europe/Brussels)

### ✅ COMPLETED

- Read required `/dev-docs` command instructions and canonical repo contract.
- Loaded/reviewed relevant docs: `AGENTS.md`, `dev/active/README.md`, `.claude/contract/intents.yaml`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, architecture/API/security/Blazor/configuration/dock/testing docs.
- Loaded relevant skills and path-scoped rules for Clean Architecture, CQRS/MediatR, EF Core, auth, Blazor/BFF, design system, accessibility, outbox, error tracking, Aspire, API controllers, HATEOAS, persistence, migrations, tests.
- Inspected current AI-related implementation and confirmed the current codebase has a placeholder AI rail/dock foundation but no functional AI backend, provider, persistence, conversation history, reference search, or action execution.
- Reused existing workstream directory `dev/active/ai-integration/` instead of creating a duplicate.
- Created/re-baselined `ai-integration-plan.md` and this context file.
- Baseline build passed: `dotnet build --configuration Release --verbosity quiet` returned 0 errors and 29 existing warnings.
- Attempted Context7 current-doc lookup for `Microsoft.Extensions.AI` and Semantic Kernel; Context7 was quota-blocked in the senior CTO review pass, so official Microsoft/OpenAI docs were used as fallback. Findings: `Microsoft.Extensions.AI` offers `IChatClient`, DI registration, streaming, caching/tracing pipeline extensions, and function invocation; Semantic Kernel offers kernel/plugin DI and automatic function calling. Planning decision remains to wrap these behind an ISLAMU Application-owned `IAiChatProvider` so human confirmation, HAL, tenant policy, persistence, and fake-provider tests stay platform-controlled.
- Completed senior CTO review of the AI plan and resequenced it around required safety gates: no automatic mutating tool invocation, authenticated private surfaces, provider egress validation, safe telemetry, quotas, retention/redaction, and authorization parity tests before broad enablement.
- Updated `ai-integration-plan.md`, `ai-integration-tasks.md`, and this context file to reflect the safety-first implementation plan.
- User approved implementation by instructing the agent to “start implementing,” resolving the prior plan-approval blocker.
- Re-confirmed current repo state before first implementation edit. `git status --short` showed substantial unrelated dirty work across storage/idempotency/subscription areas; AI implementation avoided modifying those files.
- Baseline build passed before first implementation edit: `dotnet build --configuration Release --verbosity quiet` returned 0 errors and 15 existing warnings.
- Attempted the requested Tavily and Context7 research refresh. Both MCPs were quota/plan blocked in this session, so official Microsoft Learn pages were used as fallback for `Microsoft.Extensions.AI` and Semantic Kernel. OpenAI Platform docs returned HTTP 403 through the current fetch path.
- Finalized the provider adapter decision for implementation: Application will own `IAiChatProvider`; Domain remains provider-agnostic; Infrastructure will later provide a deterministic fake provider plus an OpenAI-compatible adapter; Microsoft.Extensions.AI/Semantic Kernel can be wrapped later but cannot leak provider SDK types across Clean Architecture boundaries.
- Implemented the Phase 1 domain foundation slice under `Explore.Domain/Ai/`: conversation/message/run/action/reference statuses and entities, explicit message ordering, run lifecycle, reference attachment, proposed action confirmation/rejection/execution/failure, and tool execution audit metadata.
- Added domain unit tests under `Event.Domain.UnitTests/Ai/` covering message sequence ordering, run success/failure lifecycle, typed references, action proposal safety, blocked conversation behavior, and proposed-action transition rules.
- Implemented the Phase 1 EF persistence surface for AI conversations: DbSets, named tenant/soft-delete query filters, explicit EF configurations, indexes, check constraints, JSONB payload storage, and cascade behavior for messages/runs/references/actions/tool executions.
- Added `IAiConversationRepository` in Application and `AiConversationRepository` in Persistence. The repository returns Domain entities only, uses `AsNoTracking()` for read-only history/detail queries, uses tracking queries for update/action workflows, orders included child collections deterministically, and preserves tenant filters.
- Registered `IAiConversationRepository` in `PersistenceServicesRegistration` without changing unrelated repository registrations.
- Implemented Phase 2 provider/configuration foundation without adding provider SDK dependencies: expanded AI governance keys/settings definitions, enriched `AiAssistantSettingGroup`, added provider-neutral Application contracts under `Explore.Application/Contracts/Infrastructure/Ai`, and added setting-group tests for safe defaults, fake provider availability, real provider model/key requirements, limits, and feature flags.
- Updated `docs/CONFIGURATION.md` with the `ai_assistant.*` governance settings, safe defaults, secret handling, provider gating, and the rule that tool proposals remain persisted proposals requiring confirmation.
- Implemented Phase 2 Infrastructure provider foundation: `AiProviderSettings`, `AiProviderSettingsValidator`, and deterministic `FakeAiChatProvider` under `Explore.Infrastructure/Ai`.
- Registered the AI validator and fake provider in `InfrastructureServicesRegistration`. The fake provider is registered concrete-only, not as the global `IAiChatProvider`, so later runtime provider selection cannot accidentally enable fake AI without an explicit selector.
- Added Infrastructure tests for provider settings validation and fake-provider behavior, including supported provider names, OpenAI-compatible endpoint/key/model requirements, endpoint credential/query/fragment rejection, limit validation, deterministic model catalog, deterministic assistant response, empty-message failure, and optional `CreateEventDraft` proposed-action output.
- Implemented the Phase 2 authenticated AI assistant bootstrap surface: provider defaults, bootstrap DTOs, `GetAiAssistantBootstrapQuery`/handler, source-generated JSON metadata, and `GET /api/ai/assistant/bootstrap` on `AiAssistantController`.
- The bootstrap endpoint is authenticated and returns only configuration-derived capability metadata: enabled/available state, disabled reason, tenant-approved model list, default model, feature flags, limits, retention days, and HAL `self`. It intentionally exposes no provider endpoint URL, API key, prompts, conversation history, raw provider errors, provider request IDs, or write/action links.
- Added Application unit tests for bootstrap settings behavior covering disabled state, deterministic fake provider model metadata, OpenAI-compatible missing endpoint disabled reason, and configured OpenAI-compatible limits/feature flags.
- Implemented Phase 2 provider health, egress safety, and safe telemetry. `AiProviderSettings` now has static `AiProvider:*` binding support, an `Enabled` switch, and explicit local/private endpoint opt-in. `AiProviderSettingsValidator` validates absolute HTTP/HTTPS endpoints, rejects embedded credentials/query/fragment values, rejects localhost/private/link-local endpoints unless `AllowLocalProviderEndpoints` is true, and only requires OpenAI-compatible endpoint/key/model values when the provider is enabled.
- Added provider health contracts and Infrastructure readiness plumbing: `AiProviderHealth`, `AiProviderHealthReporter`, and `AiProviderHealthCheck`. The `ai-provider` readiness check reports healthy-disabled for disabled AI, healthy for fake provider, configured-without-network-probe for valid OpenAI-compatible settings, and unhealthy for invalid/missing runnable provider settings.
- Added safe AI provider telemetry counters to `BusinessMetrics`: `explore.ai.provider.health_checks` and `explore.ai.provider.requests`. Tags are bounded to provider/status/reason/outcome/failure category and intentionally exclude tenant IDs, user IDs, prompts, responses, selected reference content, endpoint URLs, API keys, model IDs, provider request IDs, and raw provider errors.
- Updated `docs/CONFIGURATION.md` and `docs/OPERATIONS.md` with static `AiProvider:*` configuration, readiness semantics, egress rules, and safe telemetry rules. Removed the older duplicate minimal AI Assistant governance section from `docs/CONFIGURATION.md`.
- Added Infrastructure and Application unit tests for AI provider health, health-check payload safety, egress validation, and safe BusinessMetrics tags.
- Implemented the OpenAI-compatible Infrastructure adapter behind `IAiChatProvider` and `IAiModelCatalog` using raw HTTP, not a provider SDK. `OpenAiCompatibleChatProvider` uses validated static `AiProvider:*` settings, builds non-streaming chat-completions requests, applies explicit timeout/cancellation handling, maps usage/request IDs/finish reasons, and maps provider tool calls only into typed proposed-action candidates.
- Registered the OpenAI-compatible adapter as a concrete Infrastructure service and added a named `OpenAiCompatibleAiClient` `HttpClient`. It is not yet bound as the global `IAiChatProvider`; runtime provider selection remains a deliberate next step.
- Added Infrastructure tests for the OpenAI-compatible adapter covering model catalog behavior, request URI/header/body shape, safe response mapping, `CreateEventDraft` tool-call mapping, safe HTTP failure redaction, provider-not-configured no-HTTP behavior, and timeout mapping.

### 🟡 IN PROGRESS

- Phase 1 persistence foundation remains in progress: EF mappings/query filters/repository code is present; migration, schema docs, and persistence integration tests are still pending.
- Phase 2 provider/bootstrap foundation is in progress: internal settings, Application contracts, Infrastructure settings validator, deterministic fake provider, OpenAI-compatible adapter, authenticated bootstrap query/API, provider health, egress safety, and safe telemetry exist; runtime provider selection/reconciliation remains pending.

### ⏭️ NEXT

1. Decide how static `AiProvider:*` deployment options and tenant governance `ai_assistant.*` settings should reconcile in runtime provider selection.
2. Add an explicit runtime provider selector that can choose fake or OpenAI-compatible providers without enabling chat/send prematurely.
3. Decide how to handle EF migration generation in the current dirty worktree, because unrelated model changes would likely be captured in the same migration.
4. Generate an AI-scoped migration and update `schemas/islamu-event.md` only when migration churn can be kept isolated.
5. Add `Event.Persistence.IntegrationTests/Repositories/AiConversationRepositoryTests.cs` for create/list/get/update, message ordering, proposed-action lookup, and tenant isolation.
6. Re-read active event creation/roles workstreams before Phase 5 `CreateEventDraft` action work.
7. Do not broadly enable send/chat before Phase 3.8 and 3.9 safety gates.

### ⚠️ BLOCKERS

- No hard blocker for the current domain/persistence foundation.
- EF migration generation is risky in the current shared dirty worktree because unrelated model changes already exist and may be captured as migration churn.
- Tavily and Context7 are quota/plan blocked in this session; use official docs fallback until MCP quota is available again.
- Broad AI send/chat enablement remains blocked on runtime adapter selection, auth parity, idempotency, quota/rate limiting, and retention/redaction gates.

## Quick Resume

1. Read `dev/active/ai-integration/ai-integration-plan.md`.
2. Read `dev/active/ai-integration/ai-integration-tasks.md`.
3. Continue from the first unchecked Phase 1 persistence task unless user instruction overrides it.
4. Keep plan/context/tasks updated after each meaningful implementation slice.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `dev/active/ai-integration/ai-integration-plane-report.md` | Existing | Docs | Deep Plane source analysis and inspiration report. | 2943 lines; use as reference, not implementation status. |
| `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` | Existing | Blazor | Placeholder right rail UI. | Replace body with functional assistant while preserving shell hosting. |
| `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor.css` | Existing | Blazor | Rail styling for fixed/docked modes. | Extend with BEM-like isolated CSS. |
| `Explore.Blazor.Client/Services/AiAssistantState.cs` | Existing | Blazor | Open/close/availability/navbar preference state. | Do not turn it into an authz authority. Add/delegate conversation state carefully. |
| `Explore.Blazor.Client/Components/Shell/ShellDockPanels.cs` | Existing | Blazor Shell | Registers `shell.ai-assistant` dock panel. | Reuse this; do not create competing drawer. |
| `Explore.Blazor.Client/Layout/MainLayout.razor.cs` | Existing | Blazor Shell | Bridges AI state to dock state. | Existing tests protect this behavior. |
| `Explore.Domain/Constants/GovernanceSettingKeys.cs` | Existing | Domain | AI setting keys. | Needs provider/model/retention/limit settings later. |
| `Explore.Domain/Settings/Definitions/AiAssistantSettingDefinitions.cs` | Existing | Domain | AI settings definitions. | Current settings are enabled/endpoint/key/anonymous only. |
| `Explore.Application/Settings/Groups/AiAssistantSettingGroup.cs` | Existing | Application | Resolves current AI availability. | Needs richer typed config. |
| `Explore.API/Controllers/EventController.cs` | Existing | API | Existing event draft create endpoint. | AI confirm action should reuse Event create command, not controller logic. |
| `Explore.Application/DTOs/Event/CreateEventDraftRequestDto.cs` | Existing | Application DTO | Draft request wrapper mapping to create request. | First AI tool should map into this/current equivalent. |
| `Explore.Application/Features/Events/Requests/Commands/CreateEventCommand.cs` | Existing | Application | Authorized event create command. | Underlying authorization must be reused. |
| `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs` | Existing | Application | Event creation transaction/validation/metrics/cache invalidation. | Do not duplicate event persistence logic. |
| `Explore.API/Hateoas/Policies/EventLinkPolicy.cs` | Existing | API/HATEOAS | Event HAL affordances. | AI UI must use HAL links for buttons/result navigation. |
| `Explore.Application/Contracts/Infrastructure/Ai/*` | New | Application | Provider-neutral AI contracts for chat provider, model catalog, request/response/options/errors/usage/action candidates. | Implemented; no provider SDK dependency. |
| `Explore.Infrastructure/Ai/*` | New | Infrastructure | AI provider settings validation, deterministic fake provider, and OpenAI-compatible HTTP adapter. | Implemented; providers are concrete-only registered until runtime provider selection exists. |
| `Explore.Infrastructure/HealthChecks/AiProviderHealthCheck.cs` | New | Infrastructure | Safe AI provider readiness check. | Implemented; reports bounded metadata only and records safe metrics. |
| `Explore.Application/Contracts/Infrastructure/Ai/AiProviderHealth.cs` | New | Application | Provider health snapshot contract. | Implemented; no secrets/endpoints/prompts/content. |
| `Explore.Application/Telemetry/BusinessMetrics.cs` | Existing | Application Telemetry | Business metric instruments. | Extended with safe AI provider health/request counters. |
| `Explore.Domain/Ai/*` | New | Domain | AI conversation/run/action entities and statuses. | Implemented in Phase 1 domain slice. |
| `Explore.Application/Contracts/Persistence/IAiConversationRepository.cs` | New | Application | AI aggregate repository contract returning entities. | Implemented; no DTO projections. |
| `Explore.Persistence/Configurations/Entities/Ai*Configuration.cs` | New | Persistence | EF mappings for AI conversation, message, run, reference, proposed action, and tool execution tables. | Implemented; migration pending. |
| `Explore.Persistence/Repositories/AiConversationRepository.cs` | New | Persistence | AI aggregate repository returning entities. | Implemented; integration tests pending. |
| `Explore.Application/DTOs/Ai/AiAssistantBootstrapDto.cs` | New | Application DTO | Authenticated AI bootstrap response with availability, model metadata, limits, flags, and retention. | Implemented; no secrets/provider endpoints/history. |
| `Explore.Application/Features/AiAssistant/Requests/Queries/GetAiAssistantBootstrapQuery.cs` | New | Application CQRS | Bootstrap query request. | Implemented; authenticated API dispatches this request. |
| `Explore.Application/Features/AiAssistant/Handlers/Queries/GetAiAssistantBootstrapQueryHandler.cs` | New | Application CQRS | Resolves tenant AI settings into safe bootstrap DTO. | Implemented; no provider network calls. |
| `Explore.API/Controllers/AiAssistantController.cs` | New | API | AI bootstrap/history/message/action endpoints. | Implemented for authenticated bootstrap only; history/message/action endpoints still planned. |
| `Explore.API/Hateoas/Policies/AiAssistantLinkPolicy.cs` | New | API/HATEOAS | AI action/result links. | Planned. |
| `Explore.Blazor.Client/Components/AiAssistant/*` | New | Blazor | Functional rail child components. | Planned. |
| `Explore.Blazor.Client/Services/AiAssistantClientService.cs` | New | Blazor | Generated-client wrapper and idempotency/error handling. | Planned. |

## Key Decisions

1. Reuse existing shell dock; do not introduce a competing `MudDrawer`.
2. Treat provider output as untrusted typed proposals, not executable commands.
3. Execute confirmed event draft creation through existing `CreateEventCommand`.
4. Put provider contracts in Application and provider adapters in Infrastructure.
5. Start with an ISLAMU-owned `IAiChatProvider`, deterministic fake provider, and OpenAI-compatible Infrastructure adapter; current official docs support optionally implementing adapters with `Microsoft.Extensions.AI` or Semantic Kernel behind the ISLAMU wrapper.
6. Persist conversations/messages/runs/actions for history and audit.
7. Rehydrate event references server-side and prompt-pack bounded summaries.
8. Gate all UI action buttons by HAL links.
9. Require idempotency for action confirmation and message send.
10. Implement non-streaming first; add streaming/cancellation hardening later.
11. Credit Plane as AGPL-compatible inspiration in docs/comments where useful; do not copy code.
12. Do not enable automatic provider/tool invocation for mutating tools in the first release; tool calls can only become persisted proposals.
13. Keep assistant disabled or fake/provider-admin-only until the MVP safety gate passes.
14. Provider endpoint URLs are deployment/admin-controlled and egress-validated; the browser or request payload must not choose provider hosts.
15. Static `AiProvider:*` options are Infrastructure readiness/egress controls; tenant governance `ai_assistant.*` settings remain the runtime tenant assistant policy until runtime selection reconciles both surfaces.

## Constraints And Rules To Remember

- Every new file must start with two `ABOUTME:` comment lines.
- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- Use `Guid` for aggregates, `int` for lookups, `long` for cursors/sequence IDs.
- Write endpoints must be `[Authorize]`; private AI history GET endpoints should also require auth despite general GET convention.
- HAL links are the only source of truth for Blazor action affordances.
- Browser must not receive provider credentials or own auth tokens.
- No direct event repository insert from AI; use MediatR and existing command path.
- Do not log raw prompts/responses/reference content by default.
- Tests should use fake provider only; no real network/credentials in CI.
- Use per-project test commands and TUnit `--treenode-filter` when targeting.

## Validation Baseline

Planning baseline:

```bash
dotnet build --configuration Release --verbosity quiet
```

Latest implementation baseline: Passed, 0 errors, 15 existing warnings.

Phase 1 domain validation:

```bash
dotnet build Explore.Domain/Explore.Domain.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
```

Result: Domain build passed with 0 errors/0 warnings; Domain tests passed, 285 total, 0 failed, 0 skipped.

Phase 1 persistence code validation:

```bash
dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
```

Result: Passed with 0 errors/0 warnings. Targeted LSP diagnostics were clean for `Explore.Persistence/Configurations/Entities/Ai*Configuration.cs`, `Explore.Persistence/Repositories/AiConversationRepository.cs`, and `Explore.Application/Contracts/Persistence/IAiConversationRepository.cs`.

Phase 2 provider/config code validation:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false --filter FullyQualifiedName~AiAssistantSettingGroupTests
```

Result: Targeted LSP diagnostics were clean for `Explore.Domain/Settings/Definitions/AiAssistantSettingDefinitions.cs`, `Explore.Application/Settings/Groups/AiAssistantSettingGroup.cs`, `Explore.Application/Contracts/Infrastructure/Ai/*`, and `Event.Application.UnitTests/Settings/Groups/AiAssistantSettingGroupTests.cs`. No-analyzer Application build passed with 0 errors/0 warnings. Targeted `AiAssistantSettingGroupTests` passed: 6 total, 0 failed. A first attempt using VSTest `--filter` ran zero tests; the successful run used the repo's TUnit `--treenode-filter` convention.

Phase 2 Infrastructure provider foundation validation:

```bash
dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet -- --no-progress --maximum-parallel-tests 1
```

Result: LSP diagnostics were clean for `Explore.Infrastructure/Ai/*`, `Explore.Infrastructure.Tests/Infrastructure/AiProviderSettingsValidatorTests.cs`, `Explore.Infrastructure.Tests/Infrastructure/FakeAiChatProviderTests.cs`, and `Explore.Infrastructure/InfrastructureServicesRegistration.cs`. The no-analyzer Infrastructure test-project build passed after fixing the new fake-provider test assertions. The full Infrastructure test assembly passed: 353 total, 0 failed, 0 skipped. Two class-specific TUnit `--treenode-filter` attempts ran zero tests, so the successful verification used the full Infrastructure test assembly. Analyzer-enabled Infrastructure builds still surface unrelated existing package/advisory/nullable warnings outside AI.

Phase 2 bootstrap query/API validation:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --treenode-filter "/*/*/*GetAiAssistantBootstrapQueryHandlerTests*/*" --minimum-expected-tests 4 --no-progress --maximum-parallel-tests 1
```

Result: LSP diagnostics were clean for `Explore.Application/DTOs/Ai/AiAssistantBootstrapDto.cs`, `Explore.Application/Features/AiAssistant/Requests/Queries/GetAiAssistantBootstrapQuery.cs`, `Explore.Application/Features/AiAssistant/Handlers/Queries/GetAiAssistantBootstrapQueryHandler.cs`, `Explore.API/Controllers/AiAssistantController.cs`, `Explore.Application/Serialization/ExploreJsonContext.cs`, `Explore.Application/Contracts/Infrastructure/Ai/AiProviderDefaults.cs`, `Explore.API/Hateoas/RouteNames.cs`, and `Event.Application.UnitTests/Features/AiAssistant/Queries/GetAiAssistantBootstrapQueryHandlerTests.cs`. No-analyzer Application build passed with 0 errors/0 warnings. No-analyzer API build passed with existing non-AI package pruning warnings. Targeted bootstrap handler tests passed: 4 total, 0 failed. An initial test run failed because the new test helper used the wrong `ResolvedSetting` constructor; the helper now uses the existing object-initializer pattern.

Phase 2 provider health/egress/safe telemetry validation:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet -- --no-progress --maximum-parallel-tests 1
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --treenode-filter "/*/*/*BusinessMetricsAiProviderTests*/*" --minimum-expected-tests 2 --no-progress --maximum-parallel-tests 1
```

Result: LSP diagnostics were clean for `Explore.Infrastructure/Ai/*`, `Explore.Infrastructure/HealthChecks/AiProviderHealthCheck.cs`, `Explore.Application/Contracts/Infrastructure/Ai/AiProviderHealth.cs`, `Explore.Application/Telemetry/BusinessMetrics.cs`, `Explore.API/Program.cs`, AI provider health tests, AI provider metrics tests, `docs/CONFIGURATION.md`, and `docs/OPERATIONS.md`. No-analyzer Application, Infrastructure test-project, and API builds passed with existing non-AI warnings only. Full Infrastructure test assembly passed: 364 total, 0 failed, 0 skipped. Targeted AI provider metrics tests passed: 2 total, 0 failed.

Full Persistence build with analyzers:

```bash
dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet
```

Result: Blocked by unrelated pre-existing `Explore.Application` analyzer errors surfaced through project references, including CA1305, CA2000, CA1873, CA1000, CA1720, CA1859, and CA1310 in non-AI files.

Post-change full solution build attempt:

```bash
dotnet build --configuration Release --verbosity quiet
```

Result: Blocked by unrelated dirty work in test projects, including existing Application unit test nullability/analyzer errors such as `AppearanceResolutionServiceTests`, `SettingHandlerTests`, `StoragePolicyResolverTests`, `UpdateCurrentUserAppearancePreferencesCommandHandlerTests`, `GetOrganizationListRequestHandlerTests`, `UnsubscribeFromEmailCategoryCommandHandlerTests`, and `BusinessMetricsEmailDispatchTests`.

Architecture validation attempt:

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Result: Blocked by unrelated dirty persistence compile error: `Explore.Persistence/ExploreDbContext.DbSets.cs` already contains a definition for `NotificationFanoutRuns`.

Minimum eventual validation set:

```bash
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## Current Known Risks / Unknowns

- Provider package dependencies are intentionally not added in the Domain slice; Phase 2 should keep the ISLAMU-owned abstraction and choose concrete Infrastructure packages/adapters there.
- Provider package dependencies are intentionally not added in the Application contracts/settings slice; Phase 2 Infrastructure should keep SDK/adapters behind `IAiChatProvider` and `IAiModelCatalog`.
- The deterministic fake provider is registered as a concrete type only; runtime provider selection and `IAiChatProvider` binding still need to be added deliberately.
- The OpenAI-compatible adapter is registered as a concrete type only; runtime provider selection and `IAiChatProvider` binding still need to be added deliberately.
- Tenant policy read/apply and admin UI still expose only the original AI policy fields; the new provider/model/limit settings are defined and resolved internally but not yet broadly surfaced for tenant-admin editing.
- Bootstrap emits only a manual HAL `self` link. Full AI affordance policy for send/history/confirm/result links remains deferred to Task 3.5, after those endpoints and state transitions exist.
- The bootstrap endpoint is intentionally authenticated even though it is a GET. If a public AI capability endpoint is needed later, add a separate anonymous endpoint with a smaller non-user-specific payload.
- AI migration generation is intentionally not done yet because the current worktree contains unrelated EF model changes; generating now risks a mixed migration.
- Private AI history GET endpoints need explicit auth handling and tests because they are exceptions to the broad public GET convention.
- Retention/privacy defaults must be decided before persistent history is broadly enabled.
- Provider endpoint/egress controls must prevent browser- or request-controlled outbound hosts.
- Provider endpoint/egress controls now validate static configuration and readiness. Runtime provider selection must still ensure send/chat code only uses validated deployment/admin-controlled endpoints.
- OpenAI-compatible health currently reports `configured_no_probe` for valid static settings; no dedicated readiness network probe exists yet. The adapter can perform chat-completions requests only when a future runtime selector calls it.
- Abuse/cost controls must be enforced before broad enablement, not deferred to post-MVP operations.
- Automatic provider/tool invocation must not bypass proposal persistence, HAL, confirmation, tenant policy, idempotency, and audit.
- Streaming transport is intentionally deferred.
- Current repo has substantial unrelated dirty state; implementation agents must avoid mixing unrelated changes.
- Active event creation plan may change DTO/flow; re-read before implementing `CreateEventDraft` action.

## Handoff Notes

### Handoff — 2026-05-29 Europe/Brussels

- **Current state:** Implementation has started. Phase 1 domain entities/statuses/tests are implemented. Phase 1 persistence code now includes EF DbSets/configurations/query filters and AI repository contract/implementation. Phase 2 internal provider/config contracts/settings, Infrastructure provider settings validation, deterministic fake provider, OpenAI-compatible HTTP adapter, authenticated bootstrap query/API, provider health, egress safety, and safe telemetry are implemented. Migration/schema docs/persistence tests are not implemented yet.
- **Next action:** Decide and implement runtime provider selection/reconciliation between static `AiProvider:*` deployment settings and tenant governance `ai_assistant.*` policy without enabling broad chat/send. Separately decide migration strategy in the dirty worktree before generating an AI-scoped migration, updating schema docs, and adding persistence integration tests.
- **Blockers:** Tavily/Context7 quota limits for live MCP research; broad enablement still blocked on safety gates.
- **Modified files:** `Explore.Domain/Ai/*`, `Event.Domain.UnitTests/Ai/*`, `Explore.Domain/Constants/GovernanceSettingKeys.cs`, `Explore.Domain/Settings/Definitions/AiAssistantSettingDefinitions.cs`, `Explore.Application/Settings/Groups/AiAssistantSettingGroup.cs`, `Explore.Application/Contracts/Infrastructure/Ai/*`, `Explore.Application/DTOs/Ai/AiAssistantBootstrapDto.cs`, `Explore.Application/Features/AiAssistant/Requests/Queries/GetAiAssistantBootstrapQuery.cs`, `Explore.Application/Features/AiAssistant/Handlers/Queries/GetAiAssistantBootstrapQueryHandler.cs`, `Explore.Application/Serialization/ExploreJsonContext.cs`, `Explore.Application/Telemetry/BusinessMetrics.cs`, `Event.Application.UnitTests/Settings/Groups/AiAssistantSettingGroupTests.cs`, `Event.Application.UnitTests/Features/AiAssistant/Queries/GetAiAssistantBootstrapQueryHandlerTests.cs`, `Event.Application.UnitTests/Telemetry/BusinessMetricsAiProviderTests.cs`, `Explore.API/Controllers/AiAssistantController.cs`, `Explore.API/Hateoas/RouteNames.cs`, `Explore.API/Program.cs`, `Explore.Infrastructure/Ai/*`, `Explore.Infrastructure/HealthChecks/AiProviderHealthCheck.cs`, `Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `Explore.Infrastructure.Tests/Infrastructure/AiProviderSettingsValidatorTests.cs`, `Explore.Infrastructure.Tests/Infrastructure/FakeAiChatProviderTests.cs`, `Explore.Infrastructure.Tests/Infrastructure/AiProviderHealthReporterTests.cs`, `Explore.Infrastructure.Tests/Infrastructure/AiProviderHealthCheckTests.cs`, `Explore.Application/Contracts/Persistence/IAiConversationRepository.cs`, `Explore.Persistence/Configurations/Entities/Ai*Configuration.cs`, `Explore.Persistence/Repositories/AiConversationRepository.cs`, AI additions in `Explore.Persistence/ExploreDbContext.DbSets.cs`, AI additions in `Explore.Persistence/ExploreDbContext.QueryFilters.cs`, AI registration in `Explore.Persistence/PersistenceServicesRegistration.cs`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, and `dev/active/ai-integration/*`.
- **Validation:** Initial full build passed with 0 errors and 15 warnings before editing; Domain project build passed with 0 errors/0 warnings; Domain unit tests passed with 285/285 tests; targeted LSP diagnostics for AI persistence/provider/config/bootstrap/health/telemetry/adapter files passed; no-analyzer Persistence build passed; no-analyzer Application build passed; no-analyzer API build passed with existing non-AI warnings; targeted `AiAssistantSettingGroupTests` passed 6/6; targeted `GetAiAssistantBootstrapQueryHandlerTests` passed 4/4; no-analyzer Infrastructure test-project build passed; full Infrastructure test assembly passed 373/373 after adding OpenAI-compatible adapter tests; targeted `BusinessMetricsAiProviderTests` passed 2/2. Architecture tests were previously blocked by unrelated dirty persistence compile error `NotificationFanoutRuns` duplicate DbSet; full analyzer Persistence build is blocked by unrelated Application analyzer failures; post-change full solution build was blocked by unrelated dirty Application unit test analyzer/nullability errors.
- **Documentation impact:** Active dev docs updated; `docs/CONFIGURATION.md` now documents AI assistant governance keys, static AI provider settings, egress rules, and secret handling; `docs/OPERATIONS.md` now documents AI readiness and safe metrics.
- **Risks:** Migration isolation, private history auth, runtime provider selection, static-vs-governance provider reconciliation, abuse/cost controls, retention/privacy, unrelated dirty repo state.
- **Notes for next contributor/agent:** Do not enable UI chat/send yet. Continue the foundation path: runtime provider selection/reconciliation, AI-scoped migration/schema docs when isolated, persistence tests, API auth/idempotency/HAL, then Blazor.
