<!-- ABOUTME: Tactical checklist for the AI Tool Contract Registry workstream. -->
<!-- ABOUTME: Tracks registry, confirmation, API/HAL, Blazor, MCP, retention, and docs tasks. -->

# AI Tool Contract Registry — Task Checklist

Last Updated: 2026-06-06 Europe/Brussels

## Status Summary

- **Overall status:** ✅ ATCR implementation is complete through Phases 0-8, including Phase 7.3 MCP adapter documentation. Phase 9 provider hardening is complete through Phase 9.5: Infrastructure-only `Microsoft.Extensions.AI` adapter, SDK-backed provider modes, redacted provider telemetry, stable content-filter mapping, strict schema validation, bounded self-correction, and token-budgeted prompt/reference/tool packing are implemented; Phase 10 remains the AgentBlazor-inspired agent-experience hardening roadmap.
- **Completed:** Current-state investigation, old AI workstream migration analysis, old AI integration archive pointer, new plan/context/tasks creation, Phase 1 registry foundation, Phase 2 `CreateEventDraft` registry migration, Phase 3 confirmation engine/audit metadata, Phase 4 API/HAL/OpenAPI confirm/reject flow, Phase 5 reference search/API/HAL/prompt packing, Phase 6 Blazor service/state/rail/reference/proposal/full-panel test foundation, Phase 8.1-8.5 retention/redaction/cancellation/polling/operations/final-docs hardening, Phase 7.1/7.2/7.3 MCP adapter delivery, the 2026-06-05 official .NET AI report integration, and the 2026-06-05 AgentBlazor comparative analysis into these dev docs.
- **Current priority:** Continue Phase 9 with Phase 9.6 structured output or Phase 9.7 advisory evaluation reports if provider hardening should continue. If the user prioritizes AgentBlazor-inspired tooling first, start Phase 10.1-10.3 before generated inventories or plan previews.
- **Next recommended slice:** Start Phase 9.6 structured output for non-action assistant modes, unless the user first wants either the unrelated full-repo verification blockers fixed or the Phase 10 registry metadata/recovery/schema hardening slice.
- **Verification note:** Phase 9.5 passed `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` with 1259/1259 tests, `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` with 190 total / 189 succeeded / 1 skipped, and `dotnet build --configuration Release --verbosity quiet` with 25 projects, 0 errors, and existing warnings. Broader integration/Blazor test suites were not rerun in this slice.

## Implementation Maintenance Rules

- [x] Before starting work, read plan/context/tasks.
- [x] Re-read relevant intent docs/rules/skills for the slice being implemented.
- [x] After each completed task, update this checklist immediately.
- [x] If implementation changes architecture/scope, update the plan before continuing.
- [x] If discoveries affect future work, update the context file.
- [x] Final implementation summary must include Implemented / Verified / Remaining / Next / Docs updated.
- [x] Do not claim “done” unless all three dev docs reflect actual state.

## Phase 0: Plan Review And Baseline ✅ COMPLETE

- [x] **0.1 User reviews and approves/corrects scope**
  - **Files:** `dev/active/ai-tool-contract-registry/*`.
  - **Acceptance:** Plan status becomes User-reviewed/Approved or corrections are incorporated.
  - **Validation:** Docs updated with review outcome.
  - **Effort:** S
  - **Dependencies:** none.

- [x] **0.2 Archive or supersede old AI integration workstream**
  - **Files:** `dev/active/ai-integration/*`, `dev/pause/ai-integration/*` or equivalent pointer.
  - **Acceptance:** Old unfinished work points here; no old Phase 4-8 task is lost.
  - **Progress:** Added `dev/pause/ai-integration/README.md` as the supersession pointer. The old active directory was already absent, so no files were moved; the pointer maps old unfinished work to the registry phases.
  - **Validation:** Grep old docs for next-step pointers.
  - **Effort:** S
  - **Dependencies:** 0.1.

- [x] **0.3 Confirm current repo state before first edit**
  - **Files:** context update only unless blockers found.
  - **Acceptance:** `git status --short` and baseline build/test blockers recorded; unrelated dirty work identified and not reverted.
  - **Validation:** `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` at minimum for Phase 1.
  - **Effort:** S
  - **Dependencies:** 0.1.

## Phase 1: Application Tool Contract Registry Foundation ✅ COMPLETE

- [x] **1.1 Define registry contracts**
  - **Files:** new `Explore.Application/Features/AiAssistant/Tools/AiToolDefinition.cs`, `AiToolContractRegistry.cs`, `IAiToolContractRegistry.cs`, `AiToolValidationResult.cs`, `AiToolExecutionContext.cs`.
  - **Acceptance:** Registry can expose tool definitions without API/Blazor/Persistence dependency; failure codes/messages are safe.
  - **Validation:** Application build and targeted unit tests.
  - **Effort:** M
  - **Dependencies:** 0.3.

- [x] **1.2 Add common JSON payload guard**
  - **Files:** new `AiToolPayloadGuard.cs`, `Event.Application.UnitTests/Features/AiAssistant/Tools/AiToolPayloadGuardTests.cs`.
  - **Acceptance:** Invalid JSON, arrays, unknown fields, and forbidden fields fail closed without raw content echo.
  - **Validation:** Targeted Application tests.
  - **Effort:** M
  - **Dependencies:** 1.1.

- [x] **1.3 Registry-back prompt schema and parser**
  - **Files:** `AiSystemPromptFactory.cs`, `AiStructuredActionParser.cs`, `AiPromptContextBuilder.cs`, `SendAiMessageCommandHandler.cs`, prompt/parser tests.
  - **Acceptance:** Current provider-visible behavior remains `CreateEventDraft` only; no schema when tool proposals disabled; parser uses registry allow-list.
  - **Validation:** `AiPromptContextBuilderTests`, `AiStructuredActionParserTests`, Application build.
  - **Effort:** L
  - **Dependencies:** 1.1, 1.2.

## Phase 2: Migrate CreateEventDraft Into Registry ✅ COMPLETE

- [x] **2.1 Register CreateEventDraft tool definition**
  - **Files:** new `CreateEventDraftAiToolDefinition.cs`; existing payload/mapper/schema files.
  - **Acceptance:** Tool definition provides schema, allowed fields, mapper, confirmation metadata, and required authorization metadata; existing mapper safety remains.
  - **Progress:** Phase 2 completed mapper metadata, `ResourceKinds.Event` + `AuthorizationActions.Create` metadata, provider/MCP exposure flags, and single-source allowed fields.
  - **Validation:** `CreateEventDraftAiActionMapperTests` pass.
  - **Effort:** M
  - **Dependencies:** 1.3.

- [x] **2.2 Add schema/mapper drift tests**
  - **Files:** new or extended `CreateEventDraftAiToolDefinitionTests.cs`.
  - **Acceptance:** Schema field set and mapper allowed field set cannot silently drift.
  - **Validation:** Targeted Application tests.
  - **Effort:** S
  - **Dependencies:** 2.1.

## Phase 3: Proposed Action Confirmation Engine ✅ COMPLETE

- [x] **3.1 Add confirm/reject commands and handlers**
  - **Files:** new `ConfirmAiProposedActionCommand.cs`, `RejectAiProposedActionCommand.cs`, handlers/tests.
  - **Acceptance:** Authenticated current user only; wrong tenant/user fails closed; duplicate confirm safe; reject has no side effects.
  - **Progress:** Added authenticated commands, authorization metadata, fail-closed tenant/user checks, duplicate-safe confirmed/rejected handling, and proposed-action transition persistence through `UpdateProposedActionAsync`.
  - **Validation:** Application command tests passed.
  - **Effort:** L
  - **Dependencies:** 2.1.

- [x] **3.2 Add CreateEventDraft executor**
  - **Files:** new executor; existing `CreateEventDraftAiActionMapper.cs`, `CreateEventCommand.cs`.
  - **Acceptance:** Executor dispatches `CreateEventCommand` through MediatR; no direct event repository insert; created event is draft with empty program graph.
  - **Progress:** Added `CreateEventDraftAiToolExecutor`; confirm handler dispatches existing `CreateEventCommand` through MediatR, marks executed on success, marks failed on mapper/create failure, and never writes event repositories directly.
  - **Validation:** Application tests passed; DB-backed API flow remains Phase 4.
  - **Effort:** L
  - **Dependencies:** 3.1.

- [x] **3.3 Persist safe execution result metadata**
  - **Files:** `AiToolExecution` usage, repository update methods, tests; migration only if current schema is insufficient.
  - **Acceptance:** Success/failure execution metadata is queryable without raw provider/tool payload leakage.
  - **Progress:** Reused the existing `AiToolExecution` table/schema; confirm handler writes one safe execution row per attempted tool execution; repository exposes create/query methods under tenant filters; no raw provider/tool payload is stored in execution audit rows.
  - **Validation:** Application command tests and targeted PostgreSQL-backed repository test passed.
  - **Effort:** M
  - **Dependencies:** 3.2.

## Phase 4: API, HAL, OpenAPI, Contract Tests ✅ COMPLETE

- [x] **4.1 Add confirm/reject API endpoints**
  - **Files:** `AiAssistantController.cs`, `AiAssistantProblemDetails.cs`, `RouteNames.cs`, API tests.
  - **Acceptance:** Authorized thin endpoints; confirm propagates `Idempotency-Key`; safe ProblemDetails.
  - **Progress:** Added authenticated POST confirm/reject routes under AI conversations, stable route names, AI assistant rate-limit metadata, safe proposed-action ProblemDetails mappings, and confirm `Idempotency-Key` propagation into the command contract.
  - **Validation:** `Event.API.IntegrationTests` targeted controller tests.
  - **Effort:** M
  - **Dependencies:** 3.1.

- [x] **4.2 Add proposed-action HAL links**
  - **Files:** `AiAssistantLinkPolicy.cs`, `AiConversationResourceAssembler.cs`, `LinkRelations.cs`, HATEOAS tests.
  - **Acceptance:** Confirm/reject links appear only for allowed proposed actions; absent for unauthorized/stale/executed/rejected actions.
  - **Progress:** Added nested proposed-action `_links` on `AiProposedActionDto`; the assembler uses the existing async HAL authorization pipeline and emits `confirm-action`/`reject-action` only for active conversations with proposed actions.
  - **Validation:** AI HATEOAS tests.
  - **Effort:** M
  - **Dependencies:** 4.1.

- [x] **4.3 Add DB-backed create-draft confirmation flow tests**
  - **Files:** new `AiAssistantCreateEventDraftFlowTests.cs` or equivalent.
  - **Acceptance:** Fake provider proposes draft; send creates no event; confirm creates exactly one draft; reject creates none; duplicate confirm creates one event.
  - **Progress:** Extended the PostgreSQL-backed AI assistant flow test to seed an organization publisher, propose an organization-scoped `CreateEventDraft`, assert send creates no event, confirm creates one draft, and duplicate confirm returns the same event without a second insert.
  - **Validation:** API/Application tests.
  - **Effort:** L
  - **Dependencies:** 4.1, 4.2.

- [x] **4.4 Regenerate OpenAPI/client/changelog**
  - **Files:** `schemas/openapi.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CHANGELOG.md`, `docs/API.md` if needed.
  - **Acceptance:** Generated client includes confirm/reject methods; changelog documents auth/idempotency/HAL/ProblemDetails.
  - **Progress:** Regenerated OpenAPI, API contract inventory, and NSwag client; generated client now includes `ConfirmAiProposedActionAsync` and `RejectAiProposedActionAsync`; changelog documents the new explicit-confirmation mutation surface.
  - **Validation:** API build and Blazor Client build.
  - **Effort:** M
  - **Dependencies:** 4.1, 4.2.

## Phase 5: Event Reference Search And Prompt Packing ✅ COMPLETE

- [x] **5.1 Add reference DTO/query contracts**
  - **Files:** new `AiReferenceSearchResultDto.cs`, `AiSelectedReferenceDto.cs`, `SearchAiReferencesQuery.cs`.
  - **Acceptance:** Lightweight event-first reference shape; no full event content.
  - **Progress:** Added lightweight AI event reference result/selection DTOs plus `SearchAiReferencesQuery` and handler. The handler trims search terms, clamps limits, returns empty for too-short terms, and maps Event entities to safe metadata without exposing `Event.Content`.
  - **Validation:** Application build/tests.
  - **Effort:** M
  - **Dependencies:** 1.3.

- [x] **5.2 Add event reference repository/query path**
  - **Files:** event repository/specification files; persistence tests.
  - **Acceptance:** Repository returns entities; bounded deterministic search; tenant filters preserved; no unsafe `IgnoreQueryFilters()`.
  - **Progress:** Added `IEventRepository.SearchAiReferenceEventsAsync(...)` and a PostgreSQL-backed `EventRepository` implementation using `AsNoTracking()`, `EventQuerySpecification`, `EventFilter.PubliclyDiscoverable()`, bounded deterministic ordering, and EF tenant filters.
  - **Validation:** Persistence integration tests.
  - **Effort:** M
  - **Dependencies:** 5.1.

- [x] **5.3 Add reference API/HAL and prompt packer**
  - **Files:** AI reference handler/API endpoint/HAL policy, `AiReferencePromptPacker.cs`, tests.
  - **Acceptance:** Authorized results with links when allowed; per-reference/total prompt budgets; safe quoted boundaries.
  - **Progress:** Added authenticated `GET /api/ai/assistant/references`, HAL collection/item `event` links, source-generated HAL schemas, generated client/inventory refresh, and `AiReferencePromptPacker` with per-reference/total budgets plus XML-safe quoted boundaries.
  - **Validation:** API and Application tests.
  - **Effort:** L
  - **Dependencies:** 5.2.

## Phase 6: Blazor Product Assistant UX ✅ COMPLETE

- [x] **6.1 Add Blazor AI client service**
  - **Files:** new `IAiAssistantClientService.cs`, `AiAssistantClientService.cs`, service registration/tests.
  - **Acceptance:** Components use service, not ad-hoc raw HTTP; service handles bootstrap/history/send/reference/confirm/reject and idempotency keys.
  - **Progress:** Added a scoped generated-client wrapper under `Contracts/Services/Ai` and `Services/Ai`. The service wraps bootstrap, history/detail, create, send, reference search, confirm, and reject generated client methods, preserves HAL resources for UI affordance checks, propagates send/confirm idempotency keys, and returns safe defaults on `ApiException`.
  - **Validation:** Blazor service tests.
  - **Effort:** M
  - **Dependencies:** 4.4.

- [x] **6.2 Extend AI state for conversation UI**
  - **Files:** `AiAssistantState.cs` and/or `AiAssistantConversationState.cs`.
  - **Acceptance:** Tracks selected conversation/model/references/loading/errors; existing open/availability tests pass; no local authz decisions.
  - **Progress:** Added `AiAssistantConversationState` for selected conversation, conversation list, reference results, selected references, loading, and errors. Static helpers gate confirm/reject/event reference affordances exclusively by HAL link presence.
  - **Validation:** Blazor state tests.
  - **Effort:** M
  - **Dependencies:** 6.1.

- [x] **6.3 Replace rail placeholder with assistant layout**
  - **Files:** `AiAssistantRail.razor*`, `AiAssistantHeader.razor`, `AiConversationList.razor`, `AiMessageList.razor`, `AiPromptComposer.razor`.
  - **Acceptance:** MudBlazor UI, BEM-like CSS isolation, keyboard/ARIA support, docked/fixed behavior preserved, loading/error/empty/disabled states covered.
  - **Progress:** Replaced the shell placeholder with a service-backed rail body that loads conversations, displays messages, supports new conversation/send, exposes reference search/selection, and renders proposal Confirm/Reject controls only when HAL links exist. The existing docked/fixed rail shell, backdrop, close behavior, ARIA complementary landmark, and CSS isolation are preserved.
  - **Validation:** bUnit tests and manual smoke.
  - **Effort:** L
  - **Dependencies:** 6.1, 6.2.

- [x] **6.4 Add event reference picker UI**
  - **Files:** `AiReferencePicker.razor`, `AiReferenceChip.razor`, CSS/tests.
  - **Acceptance:** Debounced search, select/remove chips, loading/empty/error states, keyboard-removable chips.
  - **Progress:** Extracted dedicated `AiReferencePicker` and `AiReferenceChip` components from the rail. The picker debounces search input, preserves loading/empty states, selects/removes HAL reference resources, shows event-link availability only from HAL `_links`, and chips are removable by click, Delete, or Backspace.
  - **Validation:** bUnit tests.
  - **Effort:** M
  - **Dependencies:** Phase 5, 6.1.

- [x] **6.5 Add proposed action/result cards**
  - **Files:** `AiProposedActionCard.razor`, `CreateEventDraftActionPreview.razor`, `AiActionResultCard.razor`.
  - **Acceptance:** Confirm/Reject only render when HAL links exist; double-submit prevented; result links use API links; proposal distinct from committed data.
  - **Progress:** Extracted `AiProposedActionCard`, `CreateEventDraftActionPreview`, and `AiActionResultCard`. Confirm/Reject render exclusively from HAL `_links`, buttons respect busy state to prevent duplicate local submits, `CreateEventDraft` preview shows only safe title/description fields, and result/failure metadata is rendered separately from the proposal preview.
  - **Validation:** bUnit HAL-gating tests.
  - **Effort:** L
  - **Dependencies:** Phase 4, 6.1, 6.2.

- [x] **6.6 Add full panel bUnit tests**
  - **Files:** `Explore.Blazor.Client.Tests/Components/AiAssistant/*`, existing layout tests as needed.
  - **Acceptance:** Bootstrap, history, model selection, references, send, action cards, confirm/reject, disabled/error states; existing dock bridge tests pass.
  - **Progress:** Expanded `AiAssistantRailTests` to cover unavailable/disabled rail behavior, history/message loading, new conversation creation, send-message idempotency and detail reload, reference search/select/remove, HAL-gated proposal actions, and safe command error display. Existing focused picker/card tests cover dedicated reference and proposed-action components.
  - **Validation:** `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.
  - **Effort:** L
  - **Dependencies:** 6.1-6.5.

## Phase 7: ISLAMU Event MCP Adapter ✅ COMPLETE

- [x] **7.1 Research and select MCP hosting strategy**
  - **Files:** plan/context update, optional ADR/docs.
  - **Acceptance:** Decision covers .NET MCP library/transport, API vs separate host, auth, tenancy, config, health, self-hosting impact, and disable path.
  - **Progress:** Added `docs/adr/ADR-010-mcp-adapter-hosting-strategy.md` and updated architecture/configuration/operations/self-hosting docs. Decision: host the initial adapter in `Explore.API` using official `ModelContextProtocol.AspNetCore`, disabled by default, stateless Streamable HTTP, no legacy SSE, authenticated/tenant-resolved, registry-backed, and proposal/confirmation-first for mutations.
  - **Validation:** Source-backed decision recorded.
  - **Effort:** M
  - **Dependencies:** Phase 1.

- [x] **7.2 Implement MCP adapter over registry**
  - **Files:** `Directory.Packages.props`, `Explore.API/Explore.API.csproj`, `Explore.API/Program.cs`, `Explore.API/Configuration/McpAdapterSettings*.cs`, `Explore.API/HealthChecks/McpAdapterHealthCheck.cs`, `Explore.API/Mcp/*`, registry wiring/tests.
  - **Acceptance:** MCP tools/resources/prompts are registry-backed; mutating tools default to proposal/confirmation path; no direct repository mutation.
  - **Progress:** Added central `ModelContextProtocol.AspNetCore` package reference, disabled-by-default `Mcp:*` settings and validator, `mcp-adapter` readiness health check, API-hosted MCP server registration guarded by `Mcp:Enabled`, authenticated endpoint mapping, DI registration for `IAiToolContractRegistry`, read-only `list_ai_tool_contracts`, safe AI conversation resources, confirmation prompt, and `propose_ai_tool_action`. Mutating MCP tools delegate to `ProposeAiToolActionCommand` through MediatR, validate payloads through the registry, persist only proposed actions, and never mutate repositories directly.
  - **Validation:** Application proposal/auth/machine-scope tests passed. MCP API tests for health/config/registry/proposal/resources/prompts are implemented but blocked by unrelated API integration test project compile error in `EmailDispatchAdminControllerTests`.
  - **Effort:** XL
  - **Dependencies:** 7.1, Phase 3.

- [x] **7.3 Document MCP self-hosting and operations**
  - **Files:** `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, Aspire/Docker docs if touched.
  - **Acceptance:** Self-hosters know required/optional services, env vars, secrets, health, recovery, and disable behavior.
  - **Progress:** Updated configuration, operations, and self-hosting docs for the implemented MCP adapter: disabled-by-default posture, `Mcp:*` settings, authenticated API-hosted stateless Streamable HTTP endpoint, `mcp-adapter` readiness check, recovery/disable behavior, proposal-first mutation path, and data that must never appear in logs/support payloads.
  - **Validation:** Scoped docs diagnostics and diff checks.
  - **Effort:** M
  - **Dependencies:** 7.2.

## Phase 8: Retention, Redaction, Streaming/Cancellation, Advanced Ops, Final Docs ✅ COMPLETE

- [x] **8.1 Add retention cleanup/redaction jobs**
  - **Files:** cleanup handler/job files TBD, docs/tests.
  - **Acceptance:** Enforces `ai_assistant.retention_days`; tenant-scoped; observable; no prompt/content logs.
  - **Progress:** Added `RunAiRetentionCleanupCommand`/handler, `AiRetentionCleanupResult`, and tenant-filtered repository cleanup. The handler resolves tenant `ai_assistant.retention_days`, supports dry-run, and the repository redacts expired message content, proposed-action payloads, reference summaries, run/tool failure messages, then soft-deletes expired conversation shells without bypassing tenant filters. Scheduling/runbook/metrics integration remains Phase 8 follow-up.
  - **Validation:** Application/Persistence/API tests.
  - **Effort:** L
  - **Dependencies:** Phase 3.

- [x] **8.2 Add cancellation semantics**
  - **Files:** API/Application/Blazor provider files TBD.
  - **Acceptance:** Endpoint/API/UI cancellation is added if implemented; cancel link appears for cancellable runs only; provider cancellation token honored; cancelled runs produce no actions.
  - **Progress:** Added `CancelAiRunCommand`/handler, `AiConversation.CancelRun`, authenticated API cancel endpoint, run-status HAL `cancel-run` affordance for queued/in-progress runs only, safe ProblemDetails for not-found/non-cancellable states, authorization/machine-scope/Cerbos parity, OpenAPI/client/inventory refresh, and controller/Application coverage. Existing send-message flow already passes request `CancellationToken` to the provider; cross-request provider abort orchestration remains a future hardening concern.
  - **Validation:** Application/API/Blazor tests.
  - **Effort:** M
  - **Dependencies:** Phase 3.

- [x] **8.3 Decide and implement streaming or polling**
  - **Files:** TBD after decision.
  - **Acceptance:** Decision documented; auth/tenant isolation preserved; non-streaming fallback remains.
  - **Progress:** Decision recorded: AI run progress uses authenticated polling through `GET /api/ai/assistant/conversations/{conversationId}/runs/{runId}`. `SendAiMessage` returns `202 Accepted` with the run-status route, run-status HAL remains the source of truth for state and `cancel-run`, and `ai_assistant.streaming_enabled` stays reserved/disabled until a separate hardening slice covers streaming transport, proxy buffering, cancellation, timeout behavior, auth, logging, and polling fallback.
  - **Validation:** API/Blazor tests + manual smoke if implemented.
  - **Effort:** M-XL
  - **Dependencies:** Core flow complete.

- [x] **8.4 Add advanced provider/run dashboards and runbook polish**
  - **Files:** metrics dashboards/runbook docs, `docs/OPERATIONS.md`, troubleshooting docs if present.
  - **Acceptance:** Builds on provider health/metrics/logging; no secrets/content in logs; dashboards use low-cardinality dimensions; runbooks cover disabled/misconfigured/unavailable/rate-limited/failed-confirmation/stuck-action/MCP states.
  - **Progress:** Added `AiRetentionCleanupSettings`, validator, `IAiRetentionCleanupService`, tenant-iterating `AiRetentionCleanupService`, API `AiRetentionCleanupProcessor`, `ai-retention-cleanup` health check, and low-cardinality `explore.ai.retention.cleanup_runs` / `explore.ai.retention.cleanup_rows` metrics. The worker sets tenant context per active tenant, resolves each tenant's `ai_assistant.retention_days`, supports dry-run, logs bounded counts only, and never bypasses tenant filters or emits prompt/tool/provider content.
  - **Validation:** Infrastructure/API tests or docs checks as applicable.
  - **Effort:** M
  - **Dependencies:** Provider health/telemetry and confirmation engine.

- [x] **8.5 Final docs, credit, runbooks, and validation refresh**
  - **Files:** `docs/API.md`, `docs/API_CHANGELOG.md`, `schemas/openapi.json`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, `docs/BLAZOR.md`, `docs/DOCK_LAYOUT.md`, `docs/ACCESSIBILITY.md` if needed, `README.md` if Plane inspiration credit applies, dev docs.
  - **Acceptance:** AI endpoints/auth/idempotency/HAL/ProblemDetails, provider setup, secrets, model allow-list, limits, retention, health, disable behavior, troubleshooting, dock behavior, keyboard/focus/accessibility, HAL-gated action workflow, and Plane inspiration credit are documented where applicable. Old AI workstream archived and final validation recorded.
  - **Progress:** Refreshed self-hosting, Blazor, dock-layout, accessibility, configuration, operations, API, changelog, and workstream docs for the implemented AI assistant surface. README already includes Plane inspiration credit. Final docs checks passed and Phase 7 is now unblocked.
  - **Validation:** Project-level builds/tests plus docs/context checks.
  - **Effort:** L
  - **Dependencies:** Implemented phases.

## Phase 9: Official .NET AI Alignment And Provider Hardening 🟡 IN PROGRESS

- [x] **9.1 Add `Microsoft.Extensions.AI` adapter behind `IAiChatProvider`**
  - **Files:** `Directory.Packages.props`, `Explore.Infrastructure/Ai/*`, `Explore.Infrastructure/InfrastructureServicesRegistration.cs`, provider tests.
  - **Acceptance:** `Explore.Application` continues to expose only existing provider-neutral contracts; `IChatClient` and provider SDK types remain inside Infrastructure adapters/tests.
  - **Progress:** Added `Microsoft.Extensions.AI.Abstractions`, an Infrastructure-only `MicrosoftExtensionsAiChatProvider` adapter, provider-neutral request/response/token/tool-call mapping, registry schema-backed tool declarations, safe failure mapping, and adapter unit tests. The adapter is not eagerly selected or registered as the runtime provider until Phase 9.2 configures a concrete SDK-backed `IChatClient` mode.
  - **Validation:** `dotnet test Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed, 419 tests.
  - **Effort:** L
  - **Dependencies:** Phase 8.5.

- [x] **9.2 Add SDK-backed provider modes and Azure identity posture**
  - **Files:** AI provider settings/validator/runtime provider/DI/docs.
  - **Acceptance:** Supported providers can use SDK-backed clients; raw OpenAI-compatible fallback remains for generic/self-hosted endpoints; Azure OpenAI docs prefer Entra ID/managed identity/`DefaultAzureCredential` where appropriate.
  - **Progress:** Added explicit `openai-sdk` and `azure-openai` provider modes. `InfrastructureServicesRegistration` now conditionally registers concrete SDK-backed `IChatClient` instances only when those modes are configured, preserving disabled/fake/raw OpenAI-compatible startup behavior. Azure OpenAI supports `api-key` and `default-azure-credential`, with optional tenant ID for `DefaultAzureCredential`; docs prefer managed identity where appropriate.
  - **Validation:** Provider settings, endpoint validation, runtime selection, health reporter, and configuration docs checks were updated. `dotnet test Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed, 426 tests.
  - **Effort:** L
  - **Dependencies:** 9.1.

- [x] **9.3 Add redacted GenAI telemetry pipeline**
  - **Files:** provider adapter telemetry, logging/metrics configuration, `docs/OPERATIONS.md`.
  - **Acceptance:** OpenTelemetry/GenAI logs, traces, metrics, health, and support data never include prompts, responses, selected-reference content, raw tool payloads, provider endpoints, API keys, model secrets, tenant/user identifiers, or raw provider exceptions.
  - **Progress:** Added platform-owned redacted provider spans through `Explore.Ai.Provider`, request-duration/token/proposed-action metrics through `Explore.Business`, and SDK/raw provider adapter instrumentation that records bounded provider/outcome/failure/action metadata only. The implementation deliberately avoids SDK GenAI middleware because the official middleware can emit provider/model/server metadata that conflicts with ATCR support-data redaction rules.
  - **Validation:** `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed, 1246 tests. `dotnet test Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed, 426 tests. Operations docs were updated with metric and trace-source behavior.
  - **Effort:** M
  - **Dependencies:** 9.1.

- [x] **9.4 Harden strict schema, self-correction, and content-filter mapping**
  - **Files:** registry schema emission, provider adapters, safe failure-code mappings, tests.
  - **Acceptance:** Strict JSON schema metadata is emitted where supported; registry validation remains source of truth; safe self-correction errors do not expose internals; provider content-filter failures map to stable `content_filtered`.
- **Progress:** Completed the remaining provider-hardening slice. Registry payload validation now enforces a safe JSON-schema subset for required fields, primitive types, UUID formats, numeric minimums, string lengths, array items, forbidden fields, and unknown fields without echoing rejected payload content. Parser failures carry bounded correction instructions, and `SendAiMessageCommandHandler` retries exactly once with a safe model-visible correction prompt before failing closed. The SDK adapter preserves registry-backed JSON schema declarations, and the raw OpenAI-compatible adapter emits `strict: true` for function tools while still treating registry validation as the authority. SDK/raw content-filter mapping remains stable as `content_filtered`.
- **Refactor note:** Follow-up quality pass split schema-subset validation into `AiToolJsonSchemaPayloadValidator`, centralized safe retry wording in `AiToolCorrectionMessages`, and moved provider send/parse/retry behavior into `AiProviderResponseResolver`. `SendAiMessageCommandHandler` now owns command orchestration and domain persistence only, while behavior and tests remain unchanged.
- **Validation:** Application schema validation, parser correction, send-handler self-correction, retry-failure, SDK schema parity, raw strict-tool-schema, invalid-tool, and content-filter tests passed. `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` passed 1255 tests; `dotnet test Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passed 427 tests; `dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed 190 total / 189 succeeded / 1 skipped.
  - **Effort:** M
  - **Dependencies:** 9.1.

- [x] **9.5 Add tokenizer-backed prompt and tool budgeting**
  - **Files:** prompt context builder, reference prompt packer, provider settings/docs/tests.
  - **Acceptance:** Messages, selected references, and tool schemas are budgeted by tokenizer-backed limits where available; current message/character caps remain as fallback.
  - **Progress:** Added an Application-owned `IAiTokenEstimator` seam, deterministic `ApproximateAiTokenEstimator` fallback, and shared `AiPromptTokenBudget`. `AiPromptContextBuilder` now spends the configured `MaxInputTokens` budget across system prompt, registry-backed tool schema, and newest-first provider messages, truncating only the newest message when needed while preserving message boundaries. `AiSystemPromptFactory` omits an over-budget tool schema instead of sending schema text that cannot fit. `AiReferencePromptPacker` keeps existing count/character caps and adds optional per-reference and total token caps for selected references.
  - **Validation:** Prompt/reference budget tests plus existing prompt/reference packer tests passed in `Event.Application.UnitTests` 1259/1259. `Event.Architecture.Tests` passed 190 total / 189 succeeded / 1 skipped.
  - **Effort:** M
  - **Dependencies:** 9.1 or tokenizer selection decision.

- [ ] **9.6 Add structured output for non-action assistant modes**
  - **Files:** chat payload/options, provider adapters, prompt/response parsing tests.
  - **Acceptance:** Structured output is opt-in by assistant mode, does not replace registry-governed tool proposals, and maps provider errors to safe failure codes.
  - **Validation:** Structured response parsing and provider fallback tests.
  - **Effort:** M
  - **Dependencies:** 9.1.

- [ ] **9.7 Add advisory AI evaluation reports**
  - **Files:** evaluation harness/report docs TBD.
  - **Acceptance:** `Microsoft.Extensions.AI.Evaluation` reports cover tool proposal correctness, refusal/safety behavior, prompt-injection resistance, groundedness against selected references, and event-draft regression; reports are advisory/trend artifacts at first, not hard CI blockers.
  - **Validation:** Evaluation report generation command documented; normal unit test suites require no real provider call.
  - **Effort:** M
  - **Dependencies:** 9.1, 9.4.

- [ ] **9.8 Plan tenant-safe vector/RAG foundation**
  - **Files:** future vector ingestion/query docs and optional prototype files.
  - **Acceptance:** Any `Microsoft.Extensions.VectorData`/`IEmbeddingGenerator` design starts with public/local tenant event summaries, metadata/citations, tenant/public visibility filters, and ingestion/update hooks; private content is excluded unless explicitly approved.
  - **Validation:** Design review and tenant-isolation tests if prototyped.
  - **Effort:** L
  - **Dependencies:** Phase 5 reference search and product/security approval.

## Phase 10: AgentBlazor-Inspired Agent Experience Hardening 🟡 READY

- [ ] **10.1 Enrich registry metadata for agent UX**
  - **Files:** registry tool definition models/tests, prompt/schema emission, docs.
  - **Acceptance:** Tool definitions can describe route/workflow/context scopes, risk class, approval mode, availability reason, follow-up policy, safe action instructions, and result presentation metadata without becoming execution authority.
  - **Validation:** Registry metadata tests and schema snapshot/parity tests.
  - **Effort:** M
  - **Dependencies:** Phase 9.4 preferred.

- [ ] **10.2 Add structured safe tool recovery results**
  - **Files:** tool validation/result contracts, parser/provider adapter tests, Blazor result cards.
  - **Acceptance:** Tool validation and execution can return safe `requiresClarification`, `clarificationQuestion`, `warnings`, `nextActions`, stable failure codes, and bounded machine outputs without raw payload echo or private/provider data leakage.
  - **Validation:** Missing argument, invalid shape, unsupported field, clarification, warning, and model self-correction tests.
  - **Effort:** M
  - **Dependencies:** 9.4.

- [ ] **10.3 Harden schema format and argument-shape coverage**
  - **Files:** registry schema emission, payload guards, mapper parity tests.
  - **Acceptance:** Strict schemas cover `DateOnly`, `TimeOnly`, `DateTime`, `DateTimeOffset`, `Guid`, enums/allowed values, arrays/objects, nullability, hidden runtime-context parameters, and `additionalProperties=false`; mapper/schema drift fails tests.
  - **Validation:** Schema/mapper parity tests, invalid shape tests, and provider fallback tests.
  - **Effort:** M
  - **Dependencies:** 9.4.

- [ ] **10.4 Add route/workflow-scoped registry catalogs**
  - **Files:** registry query APIs, HAL/API integration points, Blazor assistant state, MCP registry discovery.
  - **Acceptance:** Assistant and MCP catalog views are scoped by current route/resource/workflow, tenant, user/machine principal, and API/HAL affordances; catalog visibility never grants execution authority by itself.
  - **Validation:** Authorized/unauthorized route catalog tests, HAL absence tests, and MCP discovery tests.
  - **Effort:** L
  - **Dependencies:** 10.1.

- [ ] **10.5 Generate an agent contract inventory**
  - **Files:** generated `.agent` or docs inventory path TBD, generator tests, architecture/docs tests.
  - **Acceptance:** A deterministic inventory generated from ATCR/API/HAL/OpenAPI includes route/resource/tool coverage, confirmed vs eligible tools, approval/risk labels, handler/service links, invariant instructions, and preserved manual sections without exposing secrets or content-bearing AI data.
  - **Validation:** Snapshot/diff tests, docs link/path checks, and architecture/docs drift checks.
  - **Effort:** L
  - **Dependencies:** 10.1, 10.4.

- [ ] **10.6 Add dev-only readiness and scaffold analyzer**
  - **Files:** analyzer/scaffold project TBD, docs/runbook updates.
  - **Acceptance:** Analyzer reports pass/warning/missing checks for registry schema, mapper, executor, HAL links, API endpoints, tests, docs, config, and OpenAPI/client regeneration; optional scaffold output is review-first and never runtime authority.
  - **Validation:** Analyzer fixture tests for pass/warning/missing reports.
  - **Effort:** L
  - **Dependencies:** 10.5 useful but not required.

- [ ] **10.7 Add safe schema-only data context summaries**
  - **Files:** explicit allow-list metadata files TBD, prompt context tests, docs.
  - **Acceptance:** Prompt/reference grounding may use explicit safe summaries of selected entity/DTO/reference projection fields, but cannot expose arbitrary EF entities, SQL/LINQ, private content, repositories, direct data access, or tenant-filter bypass.
  - **Validation:** Allow-list tests, tenant/public visibility tests, and prompt redaction checks.
  - **Effort:** M
  - **Dependencies:** Phase 5 reference search and product/security approval.

- [ ] **10.8 Plan multi-step proposed action preview and validation**
  - **Files:** proposed plan DTO/status/contracts TBD, validator tests, Blazor preview components.
  - **Acceptance:** Multi-step plans carry step status, risk class, approval mode, context freshness, warnings, and next actions, but remain proposal-only; confirmed side effects still dispatch existing MediatR commands with idempotency and HAL/API checks.
  - **Validation:** Plan validation tests for stale context, missing HAL affordances, unsupported tools, duplicate confirmation, clarification-required steps, and failure states.
  - **Effort:** L
  - **Dependencies:** 10.1, 10.2, 10.4.

- [ ] **10.9 Add fake/replay-provider usability and e2e loop**
  - **Files:** Playwright or equivalent e2e harness TBD, runbook/report artifacts.
  - **Acceptance:** Assistant rail and MCP proposal-first flows have deterministic fake/replay-provider scenarios in normal CI, optional manual/nightly live-provider runbooks, redacted artifacts, pass-rate/failure-class reporting, and DB side-effect checks.
  - **Validation:** Deterministic fake-provider e2e scenarios and documented manual live-provider runbook.
  - **Effort:** L
  - **Dependencies:** Phase 6, Phase 7, 10.2.

## Verification Checklist

- [ ] LSP diagnostics clean for modified files.
- [x] `dotnet build --configuration Release --verbosity quiet` passes before final closure or failures are documented as unrelated/pre-existing.
- [ ] `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passes when Domain lifecycle changes are included.
- [ ] `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passes for Application slices.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` targeted tests pass for registry/action slices.
- [ ] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passes for provider/MCP infrastructure slices.
- [ ] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` targeted tests pass for API/HAL slices.
- [ ] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` targeted tests pass for persistence/reference/retention slices.
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passes for Blazor slices.
- [ ] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` passes when UI flow changes require it.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` attempted before PR; unrelated pre-existing failures recorded separately if present.
- [ ] OpenAPI/client/changelog regenerated when API changes land.
- [ ] HAL link tests prove UI affordance gating.
- [ ] AI authorization/resource parity tests pass for local and Cerbos modes when affected.
- [ ] Send-message and confirm idempotency tests pass.
- [ ] Provider endpoint validation, safe health, and no-content logging tests pass when provider/MCP slices are affected.
- [ ] Phase 9 `IChatClient` adapter mapping and raw fallback parity tests pass before SDK-backed providers become default.
- [x] Phase 9 `content_filtered`, strict schema, invalid tool input, and safe self-correction tests pass when provider hardening lands.
- [x] Phase 9 tokenizer budget tests pass before tokenizer-backed prompt/reference/tool caps replace character-only budgeting.
- [ ] Phase 9 telemetry redaction tests prove no prompt/response/reference/tool payload/endpoint/key/model secret/tenant/user identifier/raw provider error leaks.
- [ ] Phase 9 evaluation reports remain advisory unless a later plan promotes stable checks to CI gates.
- [ ] Phase 9 vector/RAG prototypes prove tenant/public visibility filters before indexing beyond public event summaries.
- [ ] Phase 10 registry metadata, structured recovery, schema format, hidden runtime context, and mapper parity tests pass before exposing new tool metadata to providers/MCP.
- [ ] Phase 10 scoped catalog tests prove route/workflow context never grants execution authority without API/HAL authorization.
- [ ] Phase 10 generated inventory and readiness analyzer outputs are deterministic, redacted, and covered by drift/path checks.
- [ ] Phase 10 schema-only context summaries are explicit allow-lists and pass tenant/public visibility tests.
- [ ] Phase 10 plan-preview validation proves unsupported/stale/unauthorized steps cannot execute before user confirmation.
- [ ] Phase 10 fake/replay-provider e2e usability scenarios run without live provider credentials in normal CI.
- [ ] MVP quota/prompt/reference/model/tool limits and retention/redaction gate tests pass.
- [ ] No real AI provider calls are required for tests.
- [x] Docs updated where behavior/config/operations/API changed.
- [x] Dev docs refreshed with final state and remaining work before handoff.

## Remaining / Deferred Work

- Old `dev/active/ai-integration` archive is deferred until user approves this new workstream.
- Direct MCP mutation without human confirmation is deferred and requires explicit policy approval.
- Additional tools beyond `CreateEventDraft` remain deferred until product scope is approved; registry schema/mapper/executor parity has been proven for the first tool.
- Phase 9 SDK-backed provider default adoption remains deferred until operator rollout evidence exists; `IChatClient` adapter parity, raw fallback behavior, strict schema handling, `content_filtered` mapping, redacted metric telemetry foundations, and token-budgeted prompt/reference/tool packing are now implemented.
- Advisory AI evaluation reports are deferred from hard CI gates until model/provider volatility, caching, cost, and false-positive posture are understood.
- Vector/RAG work is deferred until tenant-safe event-summary ingestion/query design is approved; do not index private event content by default.
- AgentBlazor reflection-based service/action execution is rejected for ISLAMU; future tooling may analyze candidates, but runtime tools must be explicit registry definitions.
- Direct remote MCP tool import/execution is rejected for this workstream; ISLAMU exposes selected registry tools through its API-hosted MCP adapter and keeps mutations proposal-first.
- Arbitrary EF entity exposure, SQL/LINQ generation, repository access, and private-content schema summaries are rejected; only explicit safe schema summaries may be considered.
- Real-provider usability/e2e runs are deferred from normal CI and require explicit manual/nightly posture with redacted artifacts.
- Cross-request provider abort orchestration and streaming transport remain deferred. Persisted cancellation and authenticated polling are implemented; `ai_assistant.streaming_enabled` stays disabled/reserved until a future streaming hardening slice.
- Federation-aware AI references are deferred; first release should use local tenant events only.
