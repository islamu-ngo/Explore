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
- Implemented runtime AI provider selection. `RuntimeAiChatProvider` is now the Application-facing `IAiChatProvider` and `IAiModelCatalog` binding, while `FakeAiChatProvider` and `OpenAiCompatibleChatProvider` remain concrete Infrastructure adapters. The selector fails closed when static `AiProvider:*` is disabled or invalid, routes fake provider requests without network calls, and routes OpenAI-compatible requests through the validated raw HTTP adapter.
- Added Infrastructure tests for runtime provider selection covering disabled fail-closed behavior, fake-provider routing/model catalog, OpenAI-compatible routing/model catalog, and unsafe endpoint rejection before HTTP.
- Implemented Phase 3 conversation DTOs and Application handler foundation. The new DTO layer exposes safe private conversation, message, run, reference, and proposed-action metadata with `long` message sequences and no provider secrets, raw provider errors, prompts, or internal exception bodies.
- Added create/list/detail/run-status CQRS requests and handlers. Conversation creation manually validates the request, requires an authenticated user, resolves tenant `ai_assistant.*` governance settings, fails closed when AI is unavailable, persists only conversation metadata, and does not call a provider. List/detail/run-status enforce current-user ownership on top of repository tenant filters before returning DTOs.
- Added Application unit tests for conversation creation, disabled/unauthenticated/validation failures, owned list/detail/run-status mapping, deterministic message ordering, and unauthenticated no-repository behavior. The full no-analyzer Application unit suite passed after updating an older public-experience AI availability test to use the provider-aware fake configuration.
- Implemented guarded send-message/run orchestration in Application without adding API/UI chat entry points. `SendAiMessageCommandHandler` manually validates bounded content and idempotency key, requires an authenticated user, resolves tenant `ai_assistant.*` governance, checks runtime provider model readiness through `IAiModelCatalog`, enforces current-user ownership and active conversation state, applies the daily message limit, replays/conflicts idempotency keys, queues/runs provider work, and persists assistant messages or safe provider failures.
- Send orchestration calls `IAiChatProvider` only after tenant governance, static provider readiness, ownership, idempotency, and quota gates pass. Provider proposed actions are validated as JSON objects and persisted as `AiProposedAction` records only; no event creation, automatic tool execution, API route, HAL affordance, or Blazor chat entry point was enabled in this slice.
- Added `IAiConversationRepository.CountUserMessagesSinceAsync` and the EF repository implementation for daily quota checks without adding a migration. Added Application tests covering unauthenticated and disabled fail-closed behavior, idempotency replay, quota failure, provider failure persistence, successful send persistence, and tool-call proposal persistence.
- Implemented Phase 3 prompt builder/parser hardening. `AiPromptContextBuilder` now owns provider request packing, keeps only the most recent provider-safe messages, wraps message content in explicit boundary markers, forces non-streaming MVP options, and applies the typed system prompt/action schema from `AiSystemPromptFactory`. `AiStructuredActionParser` validates untrusted provider action candidates against the `CreateEventDraft` allow-list and rejects invalid JSON, non-object JSON, and unknown action kinds before persistence.
- Updated `SendAiMessageCommandHandler` to use the prompt builder and structured-action parser so the handler focuses on governance/idempotency/quota/provider-call orchestration rather than prompt construction or raw model-output parsing. Added Application tests for bounded prompt packing, boundary markers, tool-message exclusion, action schema generation, and action parser failures.
- Implemented authenticated Phase 3 AI API route foundation. `AiAssistantController` now exposes authenticated bootstrap, conversation list/detail/create, send-message, and run-status endpoints over existing MediatR handlers. Send accepts the `Idempotency-Key` header and propagates it into the Application send DTO. `AiAssistantProblemDetails` maps command failures to safe RFC 7807 responses without leaking prompts, provider payloads, or internal exceptions. The controller emits only conservative manual HAL links (`self`, `collection`, `create`, and active-state `send-message`) until the full AI HAL policy is implemented.
- Implemented Phase 3 AI HAL affordance policy foundation. `AiAssistantLinkPolicy` and `AiConversationResourceAssembler` now route conversation `self`, `collection`, active-state `send-message`, and collection `create` links through the standard HAL policy/assembler/evaluator pipeline instead of controller-local manual links. Authenticated affordances use `RequiresAuth` and fail closed for anonymous users; `send-message` is omitted for non-active conversations. Cancel, confirm/reject, result, and automatic tool-execution links remain omitted until later phases.
- Added host-backed Phase 3 AI API flow tests. `AiAssistantApiFlowTests` exercise the real ASP.NET Core host, auth middleware, routing, `AiAssistantController`, MediatR handlers, fake/failing AI providers, fixed tenant AI settings, and HAL response serialization for no-anonymous history/list access, disabled-assistant 403 ProblemDetails, fake-provider create/send/detail flow, idempotency replay over HTTP, idempotency conflict through the existing API idempotency middleware, provider-failure safe ProblemDetails, cross-user detail absence, and true tenant-header isolation.
- Fixed AI aggregate update semantics in `AiConversationRepository.Update`. The repository now supports tracked and detached AI aggregates by explicitly updating the conversation root row, sanitizing child navigation references, inserting new child messages/runs/references/proposed actions, and updating mutable run/action rows. This avoids EF graph-update false concurrency failures and detached-root reinsertion when Domain methods add children outside EF tracking.
- The host-backed API flow tests intentionally replace AI persistence and idempotency with focused in-memory test doubles inside the test host. This keeps the real controller/MediatR/auth/provider flow covered while avoiding unrelated dirty EF/model churn in those focused tests.
- Added full PostgreSQL-backed AI API flow tests over the real EF AI conversation repository and real idempotency repository. `AiAssistantDbBackedApiFlowTests` uses the real API host, migrated PostgreSQL AI tables, fixed fake AI settings, fake provider, and authenticated requests to validate create/send/detail, run persistence, idempotency replay/conflict, and owner privacy.
- Updated PostgreSQL API test fixtures with a targeted service-override hook, allowing DB-backed API tests to replace only AI settings/provider services while retaining real EF persistence.
- Updated `IdempotencyMiddleware` so AI message-send routes are skipped by response-level idempotency caching. AI send idempotency is Application-owned in `SendAiMessageCommandHandler`, where replay/conflict semantics are tied to the persisted run ID and AI request fingerprint.

### 🟡 IN PROGRESS

- Phase 1 persistence foundation is now implemented and PostgreSQL-validated at repository level: EF mappings/query filters/repository code, migrated AI tables, schema docs, and AI repository integration tests are present. Caveat: AI tables are in the earlier mixed `20260529173418_domainupdate.cs` migration, while `20260531165911_domainupdate4.cs` is an empty snapshot refresh rather than a clean AI-only migration.
- Phase 2 provider/bootstrap foundation is complete enough for Phase 3 work: internal settings, Application contracts, Infrastructure settings validator, deterministic fake provider, OpenAI-compatible adapter, runtime selector, authenticated bootstrap query/API, provider health, egress safety, and safe telemetry exist.
- Phase 3 conversation foundation is in progress: safe DTOs, create/list/detail/run-status/send-message handlers, prompt builder/parser hardening, authenticated API routes, AI conversation HAL affordance policy, focused API controller/HAL contract tests, host-backed AI API flow tests, regenerated OpenAPI/client artifacts, DB-backed AI repository tests, DB-backed EF API create/send/detail/idempotency flow tests, AI authorization catalog/action parity foundation, and MVP abuse/rate-limit/bounds gates exist. Broad Blazor UI enablement remains pending.

### ⏭️ NEXT

1. Re-read active event creation/roles workstreams before Phase 5 `CreateEventDraft` action work.
2. If broad history exposure is planned before Blazor, add retention cleanup/redaction jobs around `ai_assistant.retention_days`.
3. Do not broadly enable send/chat in Blazor until the product surface gates on HAL affordances and the remaining retention posture is accepted.

### ⚠️ BLOCKERS

- No hard blocker for repository-level AI persistence validation.
- The regenerated migration state is mixed: AI schema exists in `20260529173418_domainupdate.cs`, while the latest `20260531165911_domainupdate4.cs` is empty. Avoid claiming a clean AI-scoped migration unless a later migration isolates it.
- Tavily and Context7 are quota/plan blocked in this session; use official docs fallback until MCP quota is available again.
- Broad AI send/chat enablement remains blocked on product-surface HAL gating, retention/redaction posture, and final UI review.

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
| `Explore.Infrastructure/Ai/*` | New | Infrastructure | AI provider settings validation, deterministic fake provider, OpenAI-compatible HTTP adapter, and runtime selector. | Implemented; `RuntimeAiChatProvider` is the Application-facing `IAiChatProvider`/`IAiModelCatalog` binding. |
| `Explore.Infrastructure/HealthChecks/AiProviderHealthCheck.cs` | New | Infrastructure | Safe AI provider readiness check. | Implemented; reports bounded metadata only and records safe metrics. |
| `Explore.Application/Contracts/Infrastructure/Ai/AiProviderHealth.cs` | New | Application | Provider health snapshot contract. | Implemented; no secrets/endpoints/prompts/content. |
| `Explore.Application/Telemetry/BusinessMetrics.cs` | Existing | Application Telemetry | Business metric instruments. | Extended with safe AI provider health/request counters. |
| `Explore.Domain/Ai/*` | New | Domain | AI conversation/run/action entities and statuses. | Implemented in Phase 1 domain slice. |
| `Explore.Application/Contracts/Persistence/IAiConversationRepository.cs` | New | Application | AI aggregate repository contract returning entities. | Implemented; no DTO projections. |
| `Explore.Persistence/Configurations/Entities/Ai*Configuration.cs` | New | Persistence | EF mappings for AI conversation, message, run, reference, proposed action, and tool execution tables. | Implemented; migration pending. |
| `Explore.Persistence/Repositories/AiConversationRepository.cs` | New | Persistence | AI aggregate repository returning entities. | Implemented; PostgreSQL-backed repository tests passed. Tracked aggregate updates explicitly persist newly added child rows and sanitize detached navigation references. |
| `Event.Persistence.IntegrationTests/Repositories/AiConversationRepositoryTests.cs` | New | Persistence Tests | PostgreSQL-backed AI repository validation. | Implemented; covers migrated schema, child persistence, tenant filters, quota counts, and proposed-action lookup. |
| `Event.API.IntegrationTests/Features/AiAssistantApiFlowTests.cs` | New | API Tests | Host-backed AI assistant API flow validation. | Implemented with fixed AI settings, fake/failing providers, and tenant-aware in-memory AI/idempotency repositories for disabled/failure/tenant variants. |
| `Event.API.IntegrationTests/Features/AiAssistantDbBackedApiFlowTests.cs` | New | API Tests | PostgreSQL-backed AI assistant API flow validation. | Implemented with real EF AI repository/idempotency repository, fixed AI settings, and fake provider; covers create/send/detail, run persistence, idempotency replay/conflict, and owner privacy. |
| `Explore.Application/DTOs/Ai/AiAssistantBootstrapDto.cs` | New | Application DTO | Authenticated AI bootstrap response with availability, model metadata, limits, flags, and retention. | Implemented; no secrets/provider endpoints/history. |
| `Explore.Application/DTOs/Ai/AiConversationDtos.cs` | New | Application DTO | Safe private AI conversation/message/run/reference/action DTOs. | Implemented; no secrets/raw provider errors/prompts. |
| `Explore.Application/Features/AiAssistant/Requests/Queries/GetAiAssistantBootstrapQuery.cs` | New | Application CQRS | Bootstrap query request. | Implemented; authenticated API dispatches this request. |
| `Explore.Application/Features/AiAssistant/Handlers/Queries/GetAiAssistantBootstrapQueryHandler.cs` | New | Application CQRS | Resolves tenant AI settings into safe bootstrap DTO. | Implemented; no provider network calls. |
| `Explore.Application/Features/AiAssistant/Requests/{Commands,Queries}/*AiConversation*` | New | Application CQRS | Conversation create/list/detail/run-status request objects plus guarded send command. | Implemented for Application foundation; API routes pending. |
| `Explore.Application/Features/AiAssistant/Handlers/{Commands,Queries}/*AiConversation*` | New | Application CQRS | Conversation create/list/detail/run-status/send handlers. | Implemented; send calls provider only after governance, readiness, ownership, idempotency, and quota gates; no event side effects. |
| `Explore.Application/Features/AiAssistant/Prompting/*` | New | Application Prompting | Prompt packing, system prompt/action schema, and structured action validation. | Implemented; send handler no longer embeds prompt/schema/parser logic. |
| `Explore.API/Controllers/AiAssistantController.cs` | New | API | AI bootstrap/history/message endpoints. | Implemented for authenticated bootstrap, conversation list/detail/create, send-message, and run-status. Conversation HAL links now flow through `AiConversationResourceAssembler`; confirm/reject action endpoints, cancel endpoint, and UI consumption remain planned. |
| `Explore.API/Controllers/AiAssistantProblemDetails.cs` | New | API | Safe ProblemDetails mapping for AI command failures. | Implemented; keeps HTTP status mapping in API and avoids exposing prompts/provider payloads/internal errors. |
| `Explore.API/Hateoas/Policies/AiAssistantLinkPolicy.cs` | New | API/HATEOAS | AI conversation navigation and send affordance links. | Implemented for conversation `self`, `collection`, active-state `send-message`, and collection `create`; cancel/confirm/reject/result links remain planned. |
| `Explore.API/Hateoas/Assemblers/AiConversationResourceAssembler.cs` | New | API/HATEOAS | Standard HAL assembler for AI conversation detail/list resources. | Implemented; uses the normal link authorization/evaluation pipeline. |
| `Explore.Application/Authorization/*` | Existing | Application Auth | Authorization action/resource/catalog metadata. | Extended with `islamuevent_ai_conversation`, `AuthorizationActions.AiConversations`, descriptors, registry mappings, and machine-scope mapping. |
| `cerbos/policies/islamuevent_ai_conversation.yaml` | New | AuthZ Policy | Cerbos policy for AI conversation resources. | Allows authenticated self-service entry to handlers; handlers enforce owner and tenant isolation. |
| `cerbos/policies/_schemas/islamuevent_ai_conversation.json` | New | AuthZ Schema | Cerbos resource schema for AI conversation attributes. | Captures tenant/user/status attributes used by policy metadata. |
| `Event.Application.UnitTests/Features/AiAssistant/AiAssistantAuthorizationMetadataTests.cs` | New | Application Tests | AI request authorization metadata validation. | Verifies `[AuthorizeResource]` and `ISecureRequest.ResourceId` behavior for AI CQRS requests. |
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
16. The current migrated AI schema is real and tested, but it is not in a clean AI-scoped migration. AI tables are in `20260529173418_domainupdate.cs`; `20260531165911_domainupdate4.cs` is an empty snapshot refresh.
17. AI conversation authorization follows the self-service resource pattern: authorization providers gate authenticated `view`/`create`/`send_message` entry, while handlers remain responsible for current-user ownership, tenant isolation, governance, idempotency, and quota checks.
18. AI send abuse controls are layered: API `AiAssistant` rate limiting, Application user daily quota, tenant daily quota, per-user concurrent-run quota, prompt bounds, runtime model readiness, and structured-action allow-lists all execute before provider/tool side effects.

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

Phase 2 runtime provider selector validation:

```bash
dotnet build Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --no-build --verbosity quiet -- --no-progress --maximum-parallel-tests 1
```

Result: LSP diagnostics were clean for `Explore.Infrastructure/Ai/RuntimeAiChatProvider.cs`, `Explore.Infrastructure.Tests/Infrastructure/RuntimeAiChatProviderTests.cs`, and `Explore.Infrastructure/InfrastructureServicesRegistration.cs`. The initial build caught missing `IAiChatProvider`/`IAiModelCatalog` imports in `InfrastructureServicesRegistration.cs` and an incorrect model-catalog call through `IAiChatProvider`; both were fixed. No-analyzer Infrastructure test-project build passed with existing non-AI warnings only. Full Infrastructure test assembly passed: 379 total, 0 failed, 0 skipped.

Phase 3 conversation DTO/handler foundation validation:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --no-progress --maximum-parallel-tests 1
```

Result: LSP diagnostics were clean for `Explore.Application/DTOs/Ai/AiConversationDtos.cs`, `Explore.Application/DTOs/Ai/Validators/CreateAiConversationRequestDtoValidator.cs`, `Explore.Application/Features/AiAssistant/*`, `Explore.Application/Serialization/ExploreJsonContext.cs`, and the new AI Application unit tests. The first Application build caught `AiConversationDto` deriving from a sealed summary DTO; `AiConversationSummaryDto` is now non-sealed. A full Application test run then caught an older `GetPublicExperienceSettingsQueryHandlerTests` assumption that `enabled + api key` made AI available; that test now uses the provider-aware fake configuration. No-analyzer Application build passed, and the full Application unit suite passed: 1141 total, 0 failed, 0 skipped.

Phase 3 guarded send-message/run orchestration validation:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --no-progress --maximum-parallel-tests 1
dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
```

Result: LSP diagnostics were clean for `SendAiMessageCommandHandler.cs`, `SendAiMessageCommand.cs`, `SendAiMessageRequestDtoValidator.cs`, `AiConversationDtos.cs`, `IAiConversationRepository.cs`, `AiConversationRepository.cs`, `ExploreJsonContext.cs`, and `SendAiMessageCommandHandlerTests.cs`. The first Application unit test build caught a new test assertion using pattern matching inside an expression tree; the assertion now uses `!= null`. No-analyzer Application build passed with 0 errors/0 warnings. Full no-analyzer Application unit suite passed: 1148 total, 0 failed, 0 skipped. No-analyzer Persistence build passed with existing non-AI warnings only.

Phase 3 prompt builder/parser hardening validation:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --no-progress --maximum-parallel-tests 1
```

Result: LSP diagnostics were clean for `Explore.Application/Features/AiAssistant/Prompting/*`, `SendAiMessageCommandHandler.cs`, and the new prompt/parser tests. A first prompt test build caught that `AiAssistantSettingGroup` has private setters; tests now populate settings through `Populate`. A full Application test run then caught the existing send-handler assertion expecting unwrapped provider message text; it now asserts bounded wrapped content. No-analyzer Application build passed with 0 errors/0 warnings. Full no-analyzer Application unit suite passed: 1156 total, 0 failed, 0 skipped.

Phase 3 authenticated AI API route validation:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
```

Result: LSP diagnostics were clean for `Explore.API/Controllers/AiAssistantController.cs`, `Explore.API/Controllers/AiAssistantProblemDetails.cs`, `Explore.API/Hateoas/RouteNames.cs`, and `Explore.Application/Serialization/ExploreJsonContext.cs`. Initial build attempts with a one-millisecond timeout were operator error and ignored. The no-analyzer Application build passed. The no-analyzer API build passed with 0 errors and existing non-AI warnings only. API integration and Architecture tests were not run for this slice; Architecture validation was already blocked by unrelated dirty persistence compile error `NotificationFanoutRuns` duplicate DbSet.

Phase 3 AI HAL policy validation:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --treenode-filter "/*/*/*AiAssistantHateoasTests*/*" --minimum-expected-tests 4 --no-progress --maximum-parallel-tests 1
```

Result: LSP diagnostics were clean for `Explore.API/Hateoas/Policies/AiAssistantLinkPolicy.cs`, `Explore.API/Hateoas/Assemblers/AiConversationResourceAssembler.cs`, `Explore.API/Controllers/AiAssistantController.cs`, `Explore.API/Extensions/HateoasAssemblerRegistration.cs`, `Explore.Application/Hateoas/LinkRelations.cs`, and `Event.API.IntegrationTests/Features/Hateoas/AiAssistantHateoasTests.cs`. No-analyzer Application and API builds passed with existing non-AI warnings only. No-analyzer API integration test project build passed with existing non-AI warnings only. Targeted AI HATEOAS tests passed: 4 total, 0 failed, 0 skipped. Full DB-backed API integration and Architecture suites were not run in this slice because unrelated dirty EF/model state still makes broad validation risky.

Phase 3 host-backed AI API flow validation:

```bash
dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false --no-dependencies
dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -p:BuildProjectReferences=false
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/*AiAssistantApiFlowTests*/*" --minimum-expected-tests 8 --no-progress --maximum-parallel-tests 1
```

Result: LSP diagnostics were clean for `Event.API.IntegrationTests/Features/AiAssistantApiFlowTests.cs`, `Explore.Persistence/Repositories/AiConversationRepository.cs`, and the active AI dev docs. No-dependencies Persistence build passed after adding the tracked-aggregate `AiConversationRepository.Update` override. API integration test-project build passed with `BuildProjectReferences=false` and existing non-AI warnings. Targeted host-backed AI API flow tests passed: 8 total, 0 failed, 0 skipped. Normal project-reference API integration build is still blocked by unrelated dirty Application storage-service compile errors, so the focused flow test build intentionally reused current referenced binaries and in-host AI/idempotency test doubles.

Regenerated OpenAPI/client and DB-backed AI repository validation:

```bash
dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet build Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/*AiConversationRepositoryTests*/*" --minimum-expected-tests 5 --no-progress --maximum-parallel-tests 1
```

Result: Regenerated `schemas/openapi.json` includes AI assistant bootstrap, conversation list/create/detail, send-message, and run-status operation IDs. Regenerated `Explore.Blazor.Client/Clients/EventApiClient.g.cs` includes matching generated AI client methods and DTO/HAL shapes. `docs/API_CHANGELOG.md` documents the authenticated AI assistant API foundation. `schemas/islamu-event.md` documents AI conversation/message/run/reference/proposed-action/tool-execution tables. `Explore.Blazor.Client` build passed with one existing `ASPDEPR001` generated-client warning. Persistence build passed. LSP diagnostics were clean for the AI repository, PostgreSQL-backed AI repository tests, `schemas/islamu-event.md`, `docs/API_CHANGELOG.md`, and active AI docs. Targeted PostgreSQL-backed `AiConversationRepositoryTests` passed: 5 total, 0 failed, 0 skipped.

DB-backed AI API flow validation:

```bash
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/*AiAssistantDbBackedApiFlowTests*/*" --minimum-expected-tests 4 --no-progress --maximum-parallel-tests 1
```

Result: LSP diagnostics were clean for `Event.API.IntegrationTests/Features/AiAssistantDbBackedApiFlowTests.cs`, PostgreSQL API fixture override hooks, `Explore.API/Middleware/IdempotencyMiddleware.cs`, and `Explore.Persistence/Repositories/AiConversationRepository.cs`. API and API integration test-project builds passed with existing non-AI warnings. Targeted PostgreSQL-backed `AiAssistantDbBackedApiFlowTests` passed: 4 total, 0 failed, 0 skipped. These tests use migrated PostgreSQL AI tables and real EF repositories for conversation create/send/detail, run persistence, Application-owned idempotency replay/conflict, and owner privacy.

AI authorization catalog/action parity foundation:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet build Explore.Infrastructure/Explore.Infrastructure.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --no-build --verbosity quiet -- --treenode-filter "/*/*/*AiAssistantAuthorizationMetadataTests*/*|/*/*/*MachineScopeMappingTests*/*" --minimum-expected-tests 2 --no-progress --maximum-parallel-tests 1
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --treenode-filter "/*/*/*AuthorizationParityTests*/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
```

Result: LSP diagnostics were clean for AI authorization catalogs, AI request metadata, AI HAL policy, fallback authorization, and AI authorization tests. Application/API/Infrastructure no-analyzer builds passed with existing non-AI warnings only. The Application unit command broadened to the full suite and passed: 1172 total, 0 failed. Architecture authorization parity was attempted but still fails on pre-existing non-AI HATEOAS policies whose permission metadata does not use `AuthorizationActions`; AI conversation links now use `AuthorizationActions.AiConversations.*`. `Event.API.IntegrationTests` build is currently blocked by unrelated dirty `KeycloakTokenClient.cs` compile errors, so the updated AI HATEOAS metadata assertions could not be re-run from a clean rebuilt API integration test assembly in this slice.

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
- `RuntimeAiChatProvider` now binds `IAiChatProvider`/`IAiModelCatalog` to static `AiProvider:*` selection. Future send/chat handlers still must check tenant governance `ai_assistant.*` policy before calling it.
- Tenant policy read/apply and admin UI still expose only the original AI policy fields; the new provider/model/limit settings are defined and resolved internally but not yet broadly surfaced for tenant-admin editing.
- Bootstrap still emits only a manual HAL `self` link, but conversation detail/list affordances now flow through the standard AI HAL policy and resource assembler. Confirm/reject/result links remain deferred until those endpoints and state transitions exist.
- The bootstrap endpoint is intentionally authenticated even though it is a GET. If a public AI capability endpoint is needed later, add a separate anonymous endpoint with a smaller non-user-specific payload.
- AI schema is present and repository-tested, but it landed in a mixed migration (`20260529173418_domainupdate.cs`) rather than a clean AI-only migration. The latest `20260531165911_domainupdate4.cs` is an empty snapshot refresh.
- Private AI history GET endpoints need explicit auth handling and tests because they are exceptions to the broad public GET convention.
- Retention/privacy defaults must be decided before persistent history is broadly enabled.
- Provider endpoint/egress controls must prevent browser- or request-controlled outbound hosts.
- Provider endpoint/egress controls now validate static configuration and readiness. Runtime provider selection must still ensure send/chat code only uses validated deployment/admin-controlled endpoints.
- OpenAI-compatible health currently reports `configured_no_probe` for valid static settings; no dedicated readiness network probe exists yet.
- Authenticated API send is now exposed, but it goes through the already guarded Application send handler. No Blazor UI entry point, automatic tool execution, confirm/reject action endpoint, cancel endpoint, or event side effect is enabled yet.
- AI conversation authorization catalog/action parity foundation is implemented, including permission-bound HAL metadata. Full Architecture authorization parity still fails on unrelated non-AI HATEOAS policy metadata debt.
- MVP abuse/rate-limit/bounds gates are implemented for AI send: the API route uses the `AiAssistant` rate-limit policy, the Application handler enforces per-user daily, per-tenant daily, and per-user concurrent-run quotas before provider calls, and selected-reference limits are defined for Phase 4 reference packing.
- Focused controller/HAL API tests now cover route metadata, MediatR dispatch, idempotency header propagation, safe ProblemDetails mapping, run-status HAL links, and source-generated HAL serialization without DB fixtures.
- Host-backed AI API flow tests cover no-anonymous list/history access, disabled assistant, fake-provider create/send/detail, provider-failure safe ProblemDetails, idempotency replay, API middleware idempotency conflict, cross-user absence, true tenant-header isolation, and HAL links through a real test host with focused test doubles.
- PostgreSQL-backed AI repository tests validate migrated AI tables, tracked aggregate child persistence, tenant-filtered reads, quota counts, and proposed-action lookup. PostgreSQL-backed AI API flow tests now validate real EF create/send/detail, run persistence, Application-owned idempotency replay/conflict, and owner privacy over HTTP.
- Prompt construction and structured-action parsing are now extracted and tested, but future reference packing/search still needs bounded server-side context selection before reference-aware prompts are exposed.
- Retention cleanup/redaction jobs are not implemented yet; current controls define `ai_assistant.retention_days` and keep broad history/UI exposure gated until retention posture is accepted.
- Automatic provider/tool invocation must not bypass proposal persistence, HAL, confirmation, tenant policy, idempotency, and audit.
- Streaming transport is intentionally deferred.
- Current repo has substantial unrelated dirty state; implementation agents must avoid mixing unrelated changes.
- Active event creation plan may change DTO/flow; re-read before implementing `CreateEventDraft` action.

## Handoff Notes

### Handoff — 2026-05-29 Europe/Brussels

- **Current state:** Implementation has started. Phase 1 domain entities/statuses/tests are implemented. Phase 1 persistence now includes EF DbSets/configurations/query filters, AI repository contract/implementation, migrated AI tables, DBML schema docs, and PostgreSQL-backed repository tests. Phase 2 internal provider/config contracts/settings, Infrastructure provider settings validation, deterministic fake provider, OpenAI-compatible HTTP adapter, runtime provider selector, authenticated bootstrap query/API, provider health, egress safety, and safe telemetry are implemented. Phase 3 conversation DTOs, create/list/detail/run-status/send-message Application handlers, prompt builder/parser hardening, authenticated API routes, AI conversation HAL policy, focused API controller/HAL contract tests, host-backed AI API flow tests, regenerated OpenAPI/client artifacts, API changelog updates, PostgreSQL-backed AI API flow tests, AI authorization catalog/action parity foundation, and MVP abuse/rate-limit/bounds gates are implemented.
- **Next action:** Re-read the active event creation and event-scoped roles workstreams before Phase 5 `CreateEventDraft`; if broad history exposure is planned first, add retention cleanup/redaction jobs around `ai_assistant.retention_days`.
- **Blockers:** Tavily/Context7 quota limits for live MCP research; broad enablement still blocked on safety gates.
- **Modified files:** `Explore.Domain/Ai/*`, `Event.Domain.UnitTests/Ai/*`, `Explore.Domain/Constants/GovernanceSettingKeys.cs`, `Explore.Domain/Settings/Definitions/AiAssistantSettingDefinitions.cs`, `Explore.Application/Settings/Groups/AiAssistantSettingGroup.cs`, `Explore.Application/Contracts/Infrastructure/Ai/*`, `Explore.Application/DTOs/Ai/AiAssistantBootstrapDto.cs`, `Explore.Application/DTOs/Ai/AiConversationDtos.cs`, `Explore.Application/DTOs/Ai/Validators/CreateAiConversationRequestDtoValidator.cs`, `Explore.Application/DTOs/Ai/Validators/SendAiMessageRequestDtoValidator.cs`, `Explore.Application/Features/AiAssistant/*`, `Explore.Application/Serialization/ExploreJsonContext.cs`, `Explore.Application/Telemetry/BusinessMetrics.cs`, `Explore.Application/Hateoas/LinkRelations.cs`, `Event.Application.UnitTests/Settings/Groups/AiAssistantSettingGroupTests.cs`, `Event.Application.UnitTests/Features/AiAssistant/*`, `Event.Application.UnitTests/Features/PublicExperience/Queries/GetPublicExperienceSettingsQueryHandlerTests.cs`, `Event.Application.UnitTests/Telemetry/BusinessMetricsAiProviderTests.cs`, `Explore.API/Controllers/AiAssistantController.cs`, `Explore.API/Controllers/AiAssistantProblemDetails.cs`, `Explore.API/Hateoas/Policies/AiAssistantLinkPolicy.cs`, `Explore.API/Hateoas/Assemblers/AiConversationResourceAssembler.cs`, `Explore.API/Extensions/HateoasAssemblerRegistration.cs`, `Explore.API/Hateoas/RouteNames.cs`, `Explore.API/Program.cs`, `Event.API.IntegrationTests/Features/Hateoas/AiAssistantHateoasTests.cs`, `Explore.Infrastructure/Ai/*`, `Explore.Infrastructure/HealthChecks/AiProviderHealthCheck.cs`, `Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `Explore.Infrastructure.Tests/Infrastructure/*Ai*Tests.cs`, `Explore.Application/Contracts/Persistence/IAiConversationRepository.cs`, `Explore.Persistence/Configurations/Entities/Ai*Configuration.cs`, `Explore.Persistence/Repositories/AiConversationRepository.cs`, AI additions in `Explore.Persistence/ExploreDbContext.DbSets.cs`, AI additions in `Explore.Persistence/ExploreDbContext.QueryFilters.cs`, AI registration in `Explore.Persistence/PersistenceServicesRegistration.cs`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, and `dev/active/ai-integration/*`.
- **Validation:** Initial full build passed with 0 errors and 15 warnings before editing; Domain project build passed with 0 errors/0 warnings; Domain unit tests passed with 285/285 tests; targeted LSP diagnostics for AI persistence/provider/config/bootstrap/health/telemetry/adapter/selector/conversation/send/prompt/API/HAL/auth/rate-limit files passed; no-analyzer Persistence build passed; no-analyzer Application build passed; no-analyzer API build passed; full no-analyzer Application unit suite passed 1156/1156 after the Phase 3 prompt/parser tests; full no-build Application unit suite passed 1172/1172 after AI authorization metadata and machine-scope tests; Phase 3.9 Application unit command broadened to the full suite and passed 1174/1174 after quota/rate-limit settings and handler tests; no-analyzer API build passed with existing non-AI warnings after authenticated API route/HAL/DB-backed API/auth/rate-limit slices; no-analyzer API integration test-project build previously passed with existing non-AI warnings before unrelated `KeycloakTokenClient.cs` fixture errors appeared; targeted `AiAssistantSettingGroupTests` passed 6/6 before Phase 3.9 and are covered by the later 1174-test suite; targeted `GetAiAssistantBootstrapQueryHandlerTests` passed 4/4; targeted `AiAssistantHateoasTests` passed 4/4 before the auth metadata update; targeted `AiAssistantControllerTests` passed 6/6 before the rate-limit attribute and API integration rebuild became blocked by unrelated fixture errors; targeted host-backed `AiAssistantApiFlowTests` passed 8/8; targeted PostgreSQL-backed `AiAssistantDbBackedApiFlowTests` passed 4/4; no-analyzer Infrastructure test-project build passed; full Infrastructure test assembly passed 379/379 after adding runtime selector tests; targeted `BusinessMetricsAiProviderTests` passed 2/2; regenerated Blazor client build passed with one `ASPDEPR001` warning; targeted PostgreSQL-backed `AiConversationRepositoryTests` passed 5/5. Architecture authorization parity was attempted and now fails on pre-existing non-AI HATEOAS policy metadata, not on AI links. Full analyzer Persistence build is blocked by unrelated Application analyzer failures; post-change full solution build was blocked by unrelated dirty Application unit test analyzer/nullability errors.
- **Latest API flow validation addendum:** `Event.API.IntegrationTests/Features/AiAssistantApiFlowTests.cs` and the `AiConversationRepository.Update` tracked-aggregate persistence path were added. LSP diagnostics are clean. `dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false --no-dependencies` passed. `dotnet build Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -p:BuildProjectReferences=false` passed with existing non-AI warnings. Targeted host-backed `AiAssistantApiFlowTests` passed 8/8. Normal API integration project-reference build is blocked by unrelated dirty Application storage-service compile errors.
- **Latest DB-backed persistence/OpenAPI addendum:** `schemas/openapi.json` and `Explore.Blazor.Client/Clients/EventApiClient.g.cs` contain the authenticated AI assistant API operations/client methods. `docs/API_CHANGELOG.md` and `schemas/islamu-event.md` were updated. AI tables are in mixed migration `20260529173418_domainupdate.cs`; `20260531165911_domainupdate4.cs` is empty. `AiConversationRepository.Update` now explicitly updates root state, sanitizes child navigations, and inserts/upserts aggregate children. Targeted PostgreSQL-backed `AiConversationRepositoryTests` passed 5/5.
- **Documentation impact:** Active dev docs updated; `docs/CONFIGURATION.md` now documents AI assistant governance keys, static AI provider settings, egress rules, and secret handling; `docs/OPERATIONS.md` now documents AI readiness and safe metrics; `docs/API_CHANGELOG.md` documents authenticated AI API endpoints; `schemas/islamu-event.md` documents AI assistant tables.
- **Risks:** Mixed migration history, private history auth, static-vs-governance send-path enforcement, retention/privacy cleanup jobs, Architecture parity blocked by unrelated non-AI HATEOAS metadata debt, API integration build blocked by unrelated dirty `KeycloakTokenClient.cs` fixture errors, unrelated dirty repo state.
- **Notes for next contributor/agent:** Do not enable Blazor UI chat/send yet unless the product surface gates on HAL affordances and the retention posture is accepted. Continue with Phase 5 event-draft action planning only after re-reading the event creation and event-scoped roles workstreams.
