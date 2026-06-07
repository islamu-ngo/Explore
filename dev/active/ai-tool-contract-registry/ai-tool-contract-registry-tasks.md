<!-- ABOUTME: Tactical checklist for the AI Tool Contract Registry workstream. -->
<!-- ABOUTME: Tracks registry, confirmation, API/HAL, Blazor, MCP, retention, and docs tasks. -->

# AI Tool Contract Registry — Task Checklist

Last Updated: 2026-06-07 Europe/Brussels

## Status Summary

- **Overall status:** 🟡 ATCR implementation is complete through Phase 12; Phase 13 is in progress for MCP API-key-first access, anonymous safe reads, runtime governance, and remaining rate-limit/audit hardening.
- **Completed:** Current-state investigation, old AI workstream migration analysis, old AI integration archive pointer, new plan/context/tasks creation, Phase 1 registry foundation, Phase 2 `CreateEventDraft` registry migration, Phase 3 confirmation engine/audit metadata, Phase 4 API/HAL/OpenAPI confirm/reject flow, Phase 5 reference search/API/HAL/prompt packing, Phase 6 Blazor service/state/rail/reference/proposal/full-panel test foundation, Phase 8 retention/redaction/cancellation/polling/operations/final-docs hardening, Phase 7 MCP adapter delivery, all Phase 9 official .NET AI provider-hardening tasks, all Phase 10 AgentBlazor-inspired hardening tasks, Phase 11 planning/doc integration from the requested `technology-selection` and `mcp-csharp-create` skills, and Phase 11.1-11.6 official SDK/auth/projection/transport/runbook/protocol-evolution hardening, plus Phase 12.1-12.10 redacted MCP debug docs, protocol contract/error tests, bounded telemetry, projected-tool binding/cancellation tests, deterministic replay/evaluation scenarios, compatibility matrix, doctor readiness, stdio deferral ADR, and Phase 13.1/13.2/13.7 startup/runtime MCP governance safe-state work.
- **Current priority:** Finish remaining Phase 13 MCP rate-limit/audit assertions and decide whether Phase 13.5 adds new anonymous-safe MCP read resources beyond registry discovery.
- **Next recommended slice:** Close remaining Phase 13.6 coverage gaps: MCP rate-limit partitioning and invalid-key audit/metrics; decide whether Phase 13.5 adds new anonymous-safe MCP reads, then add private-read parity tests only if new reads are implemented.
- **Verification note:** Phase 9/10 refactor verification passed Application, Diagnostic, Infrastructure, Architecture, Release build, fake replay, and advisory evaluation commands. Phase 11.6 completed a documentation/protocol review and `dotnet build --configuration Release --verbosity quiet --no-restore` remained green (25 projects, 0 errors, existing warnings). Phase 11.5 verification remains green for Diagnostic unit tests (30/30), focused MCP API integration tests (28/28), Diagnostic Release build, and deterministic AI replay report generation (5 PASS, 0 WARN, 0 FAIL). Phase 12.1-12.10 added `docs/MCP_DEBUGGING.md`, bounded `Explore.Mcp` telemetry, `McpProtocolContractTests`, projected-tool binding/cancellation tests, deterministic MCP replay/evaluation scenarios, `McpDebugReadinessDoctorCheck`, ADR-011 stdio deferral, and expanded runbooks; Diagnostic unit tests passed 35/35, focused projected-tool tests passed 9/9, full API integration tests passed 1279/1283 with 4 intentional skips, replay generated 7 PASS, eval generated 6 PASS, doctor generated 8 PASS, full Release build passed, and `git diff --check` passed. Architecture tests passed after Phase 12 (190 total, 189 succeeded, 1 intentional skip). Phase 13 startup/API-key slice passed `Explore.API` Release build, full Application unit tests, full Infrastructure tests (437/437 after adding model-discovery coverage), full API integration tests (1282 succeeded, 4 intentional skips), architecture tests (189 succeeded, 1 intentional skip), full Release build, and `git diff --check`. Phase 13 runtime-governance/scope-hardening slices passed `Explore.API` and `Explore.Blazor.Client` Release builds, full Application unit tests (1310/1310), full API integration tests (1293 succeeded, 4 intentional skips), full Blazor client tests (1326 succeeded, 1 intentional skip), architecture tests (189 succeeded, 1 intentional skip), full Release build, and `git diff --check`.

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

## Phase 9: Official .NET AI Alignment And Provider Hardening ✅ COMPLETE

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

- [x] **9.6 Add structured output for non-action assistant modes**
  - **Files:** chat payload/options, provider adapters, prompt/response parsing tests.
  - **Acceptance:** Structured output is opt-in by assistant mode, does not replace registry-governed tool proposals, and maps provider errors to safe failure codes.
  - **Progress:** Added provider-neutral structured-output request metadata, assistant-message JSON schema, SDK `ChatResponseFormat.ForJsonSchema(...)` integration, raw OpenAI-compatible `response_format` emission, fake-provider support, and `AiStructuredOutputResponseMapper` fail-closed parsing for malformed non-action assistant output.
  - **Validation:** Structured response parsing and provider fallback tests.
  - **Effort:** M
  - **Dependencies:** 9.1.

- [x] **9.7 Add advisory AI evaluation reports**
  - **Files:** evaluation harness/report docs TBD.
  - **Acceptance:** `Microsoft.Extensions.AI.Evaluation` reports cover tool proposal correctness, refusal/safety behavior, prompt-injection resistance, groundedness against selected references, and event-draft regression; reports are advisory/trend artifacts at first, not hard CI blockers.
  - **Progress:** Added deterministic `Explore.Diagnostic/AiEvaluation/*` report generation, JSON/Markdown writers, console reporter, documented `ai-eval-report` operations command, and unit coverage. Reports remain no-live-provider, advisory trend evidence.
  - **Validation:** Evaluation report generation command documented; normal unit test suites require no real provider call.
  - **Effort:** M
  - **Dependencies:** 9.1, 9.4.

- [x] **9.8 Plan tenant-safe vector/RAG foundation**
  - **Files:** future vector ingestion/query docs and optional prototype files.
  - **Acceptance:** Any `Microsoft.Extensions.VectorData`/`IEmbeddingGenerator` design starts with public/local tenant event summaries, metadata/citations, tenant/public visibility filters, and ingestion/update hooks; private content is excluded unless explicitly approved.
  - **Progress:** Added `Explore.Application/Features/AiAssistant/Rag/*` policy contracts for tenant-safe index documents, citations, content scopes, and filters, plus `docs/AI_RAG_FOUNDATION.md` documenting public/local-tenant event-summary-only ingestion and no private-content default.
  - **Validation:** Design review and tenant-isolation tests if prototyped.
  - **Effort:** L
  - **Dependencies:** Phase 5 reference search and product/security approval.

## Phase 10: AgentBlazor-Inspired Agent Experience Hardening ✅ COMPLETE

- [x] **10.1 Enrich registry metadata for agent UX**
  - **Files:** registry tool definition models/tests, prompt/schema emission, docs.
  - **Acceptance:** Tool definitions can describe route/workflow/context scopes, risk class, approval mode, availability reason, follow-up policy, safe action instructions, and result presentation metadata without becoming execution authority.
  - **Progress:** Added risk/approval/follow-up/scope/result/agent metadata contracts and populated `CreateEventDraft` metadata through ATCR definitions.
  - **Validation:** Registry metadata tests and schema snapshot/parity tests.
  - **Effort:** M
  - **Dependencies:** Phase 9.4 preferred.

- [x] **10.2 Add structured safe tool recovery results**
  - **Files:** tool validation/result contracts, parser/provider adapter tests, Blazor result cards.
  - **Acceptance:** Tool validation and execution can return safe `requiresClarification`, `clarificationQuestion`, `warnings`, `nextActions`, stable failure codes, and bounded machine outputs without raw payload echo or private/provider data leakage.
  - **Progress:** Added `AiToolRecoveryResult` and recovery metadata on validation failures without echoing raw payloads, provider details, tenant/user data, or private content.
  - **Validation:** Missing argument, invalid shape, unsupported field, clarification, warning, and model self-correction tests.
  - **Effort:** M
  - **Dependencies:** 9.4.

- [x] **10.3 Harden schema format and argument-shape coverage**
  - **Files:** registry schema emission, payload guards, mapper parity tests.
  - **Acceptance:** Strict schemas cover `DateOnly`, `TimeOnly`, `DateTime`, `DateTimeOffset`, `Guid`, enums/allowed values, arrays/objects, nullability, hidden runtime-context parameters, and `additionalProperties=false`; mapper/schema drift fails tests.
  - **Progress:** Expanded strict schema validation to nullable type arrays, UUID/date/time/date-time formats, enums, nested objects, nested `additionalProperties=false`, and hidden runtime context rejection.
  - **Validation:** Schema/mapper parity tests, invalid shape tests, and provider fallback tests.
  - **Effort:** M
  - **Dependencies:** 9.4.

- [x] **10.4 Add route/workflow-scoped registry catalogs**
  - **Files:** registry query APIs, HAL/API integration points, Blazor assistant state, MCP registry discovery.
  - **Acceptance:** Assistant and MCP catalog views are scoped by current route/resource/workflow, tenant, user/machine principal, and API/HAL affordances; catalog visibility never grants execution authority by itself.
  - **Progress:** Added `AiToolCatalogQuery`, principal kind, catalog item, availability codes, and `AiToolCatalogService` with scoped visibility/availability logic and case-insensitive HAL relation handling.
  - **Validation:** Authorized/unauthorized route catalog tests, HAL absence tests, and MCP discovery tests.
  - **Effort:** L
  - **Dependencies:** 10.1.

- [x] **10.5 Generate an agent contract inventory**
  - **Files:** generated `.agent` or docs inventory path TBD, generator tests, architecture/docs tests.
  - **Acceptance:** A deterministic inventory generated from ATCR/API/HAL/OpenAPI includes route/resource/tool coverage, confirmed vs eligible tools, approval/risk labels, handler/service links, invariant instructions, and preserved manual sections without exposing secrets or content-bearing AI data.
  - **Progress:** Added `Explore.Diagnostic/AgentInventory/AiAgentContractInventoryGenerator.cs`, `docs/AI_AGENT_CONTRACT_INVENTORY.md`, and generator tests for deterministic redacted inventory output.
  - **Validation:** Snapshot/diff tests, docs link/path checks, and architecture/docs drift checks.
  - **Effort:** L
  - **Dependencies:** 10.1, 10.4.

- [x] **10.6 Add dev-only readiness and scaffold analyzer**
  - **Files:** analyzer/scaffold project TBD, docs/runbook updates.
  - **Acceptance:** Analyzer reports pass/warning/missing checks for registry schema, mapper, executor, HAL links, API endpoints, tests, docs, config, and OpenAPI/client regeneration; optional scaffold output is review-first and never runtime authority.
  - **Progress:** Added `AiToolReadinessDoctorCheck` and diagnostic wiring/tests for review-first readiness reporting without runtime authority.
  - **Validation:** Analyzer fixture tests for pass/warning/missing reports.
  - **Effort:** L
  - **Dependencies:** 10.5 useful but not required.

- [x] **10.7 Add safe schema-only data context summaries**
  - **Files:** explicit allow-list metadata files TBD, prompt context tests, docs.
  - **Acceptance:** Prompt/reference grounding may use explicit safe summaries of selected entity/DTO/reference projection fields, but cannot expose arbitrary EF entities, SQL/LINQ, private content, repositories, direct data access, or tenant-filter bypass.
  - **Progress:** Added immutable safe data-context definition/field registry and summary policy with duplicate-field protection, explicit allow-listing, stable failure codes, normalization, and no repository/query authority.
  - **Validation:** Allow-list tests, tenant/public visibility tests, and prompt redaction checks.
  - **Effort:** M
  - **Dependencies:** Phase 5 reference search and product/security approval.

- [x] **10.8 Plan multi-step proposed action preview and validation**
  - **Files:** proposed plan DTO/status/contracts TBD, validator tests, Blazor preview components.
  - **Acceptance:** Multi-step plans carry step status, risk class, approval mode, context freshness, warnings, and next actions, but remain proposal-only; confirmed side effects still dispatch existing MediatR commands with idempotency and HAL/API checks.
  - **Progress:** Added proposed-plan contracts and `AiProposedPlanValidator` with fail-closed validation for unsupported tools, missing HAL affordances, stale/future context, maximum steps, stable failure codes, and `ExecutionAuthorityGranted=false`.
  - **Validation:** Plan validation tests for stale context, missing HAL affordances, unsupported tools, duplicate confirmation, clarification-required steps, and failure states.
  - **Effort:** L
  - **Dependencies:** 10.1, 10.2, 10.4.

- [x] **10.9 Add fake/replay-provider usability and e2e loop**
  - **Files:** Playwright or equivalent e2e harness TBD, runbook/report artifacts.
  - **Acceptance:** Assistant rail and MCP proposal-first flows have deterministic fake/replay-provider scenarios in normal CI, optional manual/nightly live-provider runbooks, redacted artifacts, pass-rate/failure-class reporting, and DB side-effect checks.
  - **Progress:** Added deterministic `Explore.Diagnostic/AiReplay/*`, artifact safety policy, CI-safe report generation/writing/console output, and tests for assistant/MCP proposal-first replay scenarios.
  - **Validation:** Deterministic fake-provider e2e scenarios and documented manual live-provider runbook.
  - **Effort:** L
  - **Dependencies:** Phase 6, Phase 7, 10.2.

## Phase 11: Official .NET MCP SDK Alignment And Enterprise Hardening ✅ COMPLETE

- [x] **11.1 Lock official SDK contract conformance**
  - **Files:** `Explore.API/Program.cs`, `Explore.API/Mcp/*`, `Event.API.IntegrationTests/Features/Mcp*.cs`, active workstream docs.
  - **Acceptance:** MCP startup passes the configured stateless posture into `WithHttpTransport`; all tool/resource/prompt methods have descriptions; all schema-visible non-injected parameters have descriptions; explicit `.WithTools<T>()`/`.WithResources<T>()`/`.WithPrompts<T>()` registration remains in use.
  - **Progress:** Implemented explicit `WithHttpTransport(options => options.Stateless = mcpAdapterSettings.Stateless)`, added `[Description]` attributes to schema-visible MCP tool/resource parameters, and added `McpSdkContractTests` to enforce official SDK type and description hygiene.
  - **Validation:** `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*Mcp*/*" --no-progress --maximum-parallel-tests 1` passed 14/14. `dotnet build --configuration Release --verbosity quiet --no-restore` passed 25 projects, 0 errors, existing warnings.
  - **Effort:** S
  - **Dependencies:** Phase 7 adapter.

- [x] **11.2 Reconcile authorization filters with API boundary**
  - **Files:** `Explore.API/Program.cs`, MCP tool classes, authorization docs/tests.
  - **Acceptance:** Endpoint authentication remains mandatory; `.AddAuthorizationFilters()` and method-level `[Authorize]` are added only if they provide defense-in-depth without replacing tenant resolution, MediatR authorization, or HAL confirmation.
  - **Progress:** Added official SDK `.AddAuthorizationFilters()`, method-level `[Authorize]` on MCP tools/resources/prompts, endpoint mapping from effective options so test/runtime configuration can enable MCP deterministically, reflection tests for no anonymous MCP methods, and anonymous/authenticated endpoint tests. Endpoint auth remains authoritative, and method filters are documented as defense-in-depth only.
  - **Validation:** `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*Mcp*/*" --no-progress --maximum-parallel-tests 1` passed 18/18. `dotnet build --configuration Release --verbosity quiet --no-restore` passed 25 projects, 0 errors, existing warnings.
  - **Effort:** M
  - **Dependencies:** 11.1.

- [x] **11.3 Design first-class registry-to-MCP tool projection**
  - **Files:** `Explore.API/Mcp/*`, `Explore.Application/Features/AiAssistant/Tools/*`, registry catalog tests, docs/ADR updates if required.
  - **Acceptance:** ATCR definitions can project first-class MCP tools with registry JSON schema and SDK hint annotations while mutating calls still persist proposed actions only.
  - **Progress:** Added `AiMcpProjectedToolFactory`, registry-backed `AiMcpProjectedProposalTool`, and `AiMcpProjectedToolOptionsSetup` so `ExposeToMcp` definitions are projected as first-class `propose_*` MCP tools. Projected tool schemas use ATCR payload fields plus only `conversationId`/`summary`, SDK annotations/meta remain non-authoritative hints, and invocation maps back to `ProposeAiToolActionCommand` through MediatR rather than repositories. `list_ai_tool_contracts` now reports each projected `McpToolName`.
  - **Validation:** Registry/MCP schema parity, MCP exposure filtering, proposal command mapping, SDK hint/auth metadata, options setup, and no direct repository dependency tests pass in the focused MCP integration suite.
  - **Effort:** L
  - **Dependencies:** 10.1-10.4, 11.1.

- [x] **11.4 Harden transport, hosting, and AOT posture**
  - **Files:** `docs/adr/ADR-010-mcp-adapter-hosting-strategy.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, Docker/Aspire docs if touched.
  - **Acceptance:** Production remains API-hosted stateless Streamable HTTP; stdio stays local-only/deferred; legacy SSE remains rejected; explicit type registration/AOT tradeoffs are documented.
  - **Progress:** Added MCP startup/source posture tests that prove the API host binds SDK `HttpServerTransportOptions` to stateless Streamable HTTP, avoids product stdio/legacy-SSE registration, and keeps explicit tool/resource/prompt registration plus registry-projected tool options instead of assembly scanning. Updated ADR/configuration/operations/self-hosting docs to keep stdio local/deferred, reject legacy SSE/stateful sessions, and avoid Native AOT promises until a dedicated publish profile is verified.
  - **Validation:** Focused MCP API integration tests passed 28/28. Full Release build passed 25 projects, 0 errors, existing warnings. Architecture tests were attempted and fail on the pre-existing untracked `.claude/skills/*` schema/line-count issues.
  - **Effort:** M
  - **Dependencies:** 11.1.

- [x] **11.5 Add MCP Inspector and redacted contract test runbook**
  - **Files:** `docs/OPERATIONS.md`, `docs/AI_AGENT_EXPERIENCE_HARDENING.md`, `Explore.Diagnostic/AiReplay/*`, optional diagnostic scripts.
  - **Acceptance:** Runbook lists tools/resources/prompts and exercises proposal-only flows with fake/replay providers, auth/tenant headers, and redacted artifacts only.
  - **Progress:** Added a deterministic `ai.replay.mcp.inspector-contract` scenario to the AI replay report so normal CI records the expected Inspector discovery scope without running live clients. Documented the manual Inspector smoke checklist in Operations and AI Agent Experience Hardening docs, including tools/resources/prompts to list, auth/tenant header redaction, proposal-only call limits, and forbidden artifacts.
  - **Validation:** Diagnostic unit tests passed 30/30. The AI replay report generated 5 PASS, 0 WARN, 0 FAIL with no live provider credentials, content-bearing artifacts, or database side effects. Focused MCP API integration tests still pass 28/28.
  - **Effort:** M
  - **Dependencies:** 10.9, 11.1.

- [x] **11.6 Review client compatibility and protocol evolution**
  - **Files:** ADR/docs/workstream context.
  - **Acceptance:** SDK/protocol changes around sessions, auth filters, resources, prompts, elicitation, sampling, progress, and annotations are reviewed before client-visible behavior changes.
  - **Progress:** Added an ADR-level protocol evolution review gate plus architecture/configuration/operations/self-hosting notes. Future stateful sessions, legacy SSE, sampling, elicitation, roots, completions, progress/list-changed notifications, resource subscriptions, dynamic non-registry changes, protocol-version changes, client shims, or annotation-authority changes now require an ADR/task review before implementation.
  - **Validation:** Documentation review completed with Context7 SDK evidence; Release build remains green. Architecture tests were reattempted and still fail on pre-existing untracked `.claude/skills/*` schema/line-count issues. Targeted compatibility smoke tests are required on future `ModelContextProtocol.AspNetCore` upgrades or behavior changes.
  - **Effort:** S-M recurring
  - **Dependencies:** 11.4.

## Phase 12: MCP Debuggability, Contract Test Harness, And Client-Smoke Hardening ✅ COMPLETE

- [x] **12.1 Add local MCP debug profile and redacted client config templates**
  - **Files:** `docs/OPERATIONS.md`, `docs/AI_AGENT_EXPERIENCE_HARDENING.md`, optional redacted template under docs/dev, active workstream docs.
  - **Acceptance:** Copy-pasteable Debug/local MCP instructions cover `Mcp:Enabled=true`, stateless HTTP, fake/disposable data, auth/tenant headers, debugger attach, and `.vscode/mcp.json`/`.mcp.json` examples without real secrets or raw protocol artifacts.
  - **Progress:** Added `docs/MCP_DEBUGGING.md` with Debug launch posture, redacted `.vscode/mcp.json` and `.mcp.json` examples, Inspector/Copilot/curl smoke guidance, and forbidden artifact rules. Linked it from Operations, Configuration, Self-hosting, AI hardening, and docs index.
  - **Validation:** Docs review, focused API MCP protocol tests, full Release build, and `git diff --check` passed.
  - **Effort:** S-M
  - **Dependencies:** 11.5, 11.6.

- [x] **12.2 Build an official-SDK-or-compatible MCP client contract harness**
  - **Files:** `Event.API.IntegrationTests/Features/Mcp*.cs`, `Event.API.IntegrationTests/Fixtures/*`, optional MCP protocol helper.
  - **Acceptance:** Tests exercise `initialize`, `tools/list`, `resources/list`, `prompts/list`, and `tools/call` for registry discovery plus generic/projected proposal tools; authenticated contexts succeed, anonymous/missing-tenant contexts fail closed, and proposal calls do not create events.
  - **Progress:** Added `McpProtocolContractTests` with a minimal stateless JSON-RPC/WebApplicationFactory helper because the official SDK HTTP client transport does not directly target the in-memory test host. Tests cover `initialize`, `tools/list`, `resources/list`, `resources/templates/list`, `prompts/list`, `list_ai_tool_contracts`, generic `propose_ai_tool_action`, and projected `propose_create_event_draft`.
  - **Validation:** `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*McpProtocolContractTests/*" --no-progress --maximum-parallel-tests 1` passed 4/4 with no live provider credentials.
  - **Effort:** L
  - **Dependencies:** 11.2, 11.3.

- [x] **12.3 Add MCP protocol error and redaction regression tests**
  - **Files:** `Event.API.IntegrationTests/Features/Mcp*.cs`, `docs/AI_AGENT_EXPERIENCE_HARDENING.md` if failure categories are documented.
  - **Acceptance:** Malformed JSON-RPC, unsupported methods, unknown tools, invalid arguments, hidden fields, missing `conversationId`, missing auth, missing tenant, and disabled endpoint behavior return bounded safe failures with no raw payload or secret echo.
  - **Progress:** Added malformed JSON-RPC, unknown tool, hidden `tenantId`, and disabled endpoint coverage to `McpProtocolContractTests`; tests assert sensitive marker/API-key/stack-trace data is not echoed.
  - **Validation:** Covered by the focused 4/4 MCP protocol test pass.
  - **Effort:** M
  - **Dependencies:** 12.2.

- [x] **12.4 Harden MCP debug logging, correlation, and metrics**
  - **Files:** `Explore.API/Mcp/*`, `Explore.Application/Telemetry/BusinessMetrics.cs` if metrics are added, `docs/OPERATIONS.md`, focused tests.
  - **Acceptance:** Operators can distinguish auth, protocol, registry validation, and proposal persistence failures through bounded method/tool/outcome/failure-category diagnostics without raw JSON-RPC bodies, tenant/user IDs, prompts, payloads, endpoint URLs, model IDs, API keys, or exception text.
  - **Progress:** Added `Explore.API/Mcp/McpAdapterTelemetry.cs`, wired bounded `Explore.Mcp` ActivitySource/Meter export through ServiceDefaults, and instrumented registry/generic/projected MCP tool calls with allow-listed tool/outcome/failure-code dimensions only.
  - **Validation:** `McpProjectedToolTests` cover telemetry normalization; `McpSdkContractTests` cover ServiceDefaults source/meter export; Operations/MCP debugging docs updated.
  - **Effort:** M
  - **Dependencies:** 12.2, 12.3.

- [x] **12.5 Expand Inspector and GitHub Copilot Agent Mode smoke runbooks**
  - **Files:** `docs/OPERATIONS.md`, `docs/AI_AGENT_EXPERIENCE_HARDENING.md`, optional redacted checklist.
  - **Acceptance:** Manual smoke covers Inspector tools/resources/prompts/protocol views, projected proposal calls, Copilot Agent Mode tool visibility, rebuild/restart troubleshooting, and Debug breakpoints with fake/disposable data and redacted retained evidence only.
  - **Progress:** Expanded `docs/MCP_DEBUGGING.md`, `docs/OPERATIONS.md`, and `docs/AI_AGENT_EXPERIENCE_HARDENING.md` with Inspector tools/resources/templates/prompts checks, Copilot Agent Mode tool visibility, rebuild/restart troubleshooting, Debug breakpoints, and redacted evidence rules.
  - **Validation:** Documentation review plus focused protocol tests; manual disposable smoke remains optional.
  - **Effort:** S-M
  - **Dependencies:** 12.1, 12.2.

- [x] **12.6 Add focused unit tests for projected tool binding and cancellation**
  - **Files:** `Event.API.IntegrationTests/Features/McpProjectedToolTests.cs` or a small API adapter test helper if introduced.
  - **Acceptance:** Direct tests cover required/optional parameters, unknown fields, typed JSON values, DI/service resolution, cancellation propagation where exposed, safe result shaping, no repository dependency, and no direct mutation.
  - **Progress:** Extended `McpProjectedToolTests` to verify request-scope MediatR resolution, command binding, cancellation-token propagation, safe success shaping, no repository dependency, and hidden-field rejection.
  - **Validation:** Full API integration test run passed 1279/1283 with 4 intentional skips.
  - **Effort:** M
  - **Dependencies:** 11.3, 12.2.

- [x] **12.7 Add deterministic MCP evaluation scenarios**
  - **Files:** `Explore.Diagnostic/AiReplay/*`, `Explore.Diagnostic/AiEvaluation/*` if reused, docs for evaluation questions.
  - **Acceptance:** Advisory read-only evaluations prove an agent can discover contracts, distinguish proposal from execution, choose projected proposal tools safely, and state that confirmation is required before side effects; no live provider or mutation is required in normal CI.
  - **Progress:** Added deterministic MCP projected-tool-selection and confirmation-required replay scenarios plus an advisory `McpProposalFlow` AI evaluation dimension.
  - **Validation:** Diagnostic unit tests passed 35/35; `ai-replay-report` generated 7 PASS, 0 WARN, 0 FAIL; `ai-eval-report` generated 6 PASS, 0 WARN, 0 FAIL.
  - **Effort:** M
  - **Dependencies:** 10.9, 11.5, 12.2.

- [x] **12.8 Maintain MCP client compatibility matrix and upgrade gate**
  - **Files:** `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, `docs/adr/ADR-010-mcp-adapter-hosting-strategy.md`, active workstream docs.
  - **Acceptance:** Inspector, official C# SDK client tests, VS Code/GitHub Copilot Agent Mode, and curl/JSON-RPC fallback have documented auth headers, tenant binding, protocol version, stateless/no-session expectations, unsupported capability list, rollback posture, and upgrade smoke requirements.
  - **Progress:** Added the compatibility matrix and SDK/client upgrade gate to `docs/MCP_DEBUGGING.md`, and updated Operations to reference `McpProtocolContractTests`, replay, redacted Inspector smoke, and rollback posture.
  - **Validation:** Documentation review; future SDK/client upgrades still require targeted compatibility tests.
  - **Effort:** S-M recurring
  - **Dependencies:** 11.6, 12.2, 12.5.

- [x] **12.9 Add a review-first MCP debug doctor check**
  - **Files:** `Explore.Diagnostic/Doctor/*`, `Explore.Diagnostic.UnitTests/Doctor/*`, `docs/OPERATIONS.md`.
  - **Acceptance:** Doctor reports MCP debug/test readiness and remediation links without starting servers, calling live endpoints, generating tokens, persisting config, running migrations, or printing secrets.
  - **Progress:** Added `McpDebugReadinessDoctorCheck`, registered it in the Diagnostic CLI, and covered pass/missing/possible-secret warning cases without starting clients or printing secrets.
  - **Validation:** Diagnostic unit tests passed 35/35; read-only doctor run returned 8 PASS, 0 WARN, 0 FAIL.
  - **Effort:** M
  - **Dependencies:** 12.1, 12.2, 12.7.

- [x] **12.10 Decide future local-only stdio diagnostic host separately**
  - **Files:** ADR/workstream docs only unless explicitly approved later.
  - **Acceptance:** Decision records whether a local-only stdio host is worth adding and covers stdout/stderr safety, auth/tenant simulation, AOT/reflection implications, self-hosting non-impact, and why product MCP hosting remains API-hosted Streamable HTTP.
  - **Progress:** Added `docs/adr/ADR-011-local-mcp-stdio-diagnostic-host.md` to defer stdio implementation and keep product MCP API-hosted Streamable HTTP only.
  - **Validation:** ADR/docs review plus `McpDebugReadinessDoctorCheck` presence checks; no stdio product host code was added.
  - **Effort:** S
  - **Dependencies:** 12.1, 12.5.

## Phase 13: MCP Runtime Governance, API-Key Scopes, And Anonymous Safe Reads 🟡 IN PROGRESS

- [x] **13.1 Finalize Infisical MCP startup compatibility and validation**
  - **Files:** `Explore.API/Extensions/ConfigurationExtensions.cs`, `Explore.API/Configuration/McpAdapterSettingsValidator.cs`, configuration tests, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`.
  - **Acceptance:** `/api/MCP_ENABLED`, `/api/MCP_ENDPOINT_PATH`, `/api/MCP_STATELESS`, and `/api/MCP_ENABLE_LEGACY_SSE` map to canonical `Mcp:*`; bare endpoint paths normalize to `/...`; canonical keys win; invalid booleans are ignored; endpoint path/stateless remain startup-only; `MCP_ENABLED=false` blocks runtime exposure.
  - **Progress:** Complete for startup semantics. Compatibility mapping maps the four `/api/MCP_*` secrets, normalizes `MCP_ENDPOINT_PATH=mcp` to `/mcp`, canonical `Mcp:*` keys still win, endpoint/stateless remain startup-only, `MCP_ENABLED=false` still prevents endpoint mapping, and `MCP_ENABLE_LEGACY_SSE=true` is now accepted only as a startup ceiling while runtime SSE remains disabled.
  - **Validation:** `McpAdapterSettingsValidatorTests`, `McpAdapterHealthCheckTests`, full API integration test pass, and `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet --no-restore`.
  - **Effort:** S-M

- [x] **13.2 Add runtime MCP governance settings, locks, and admin UX/API**
  - **Files:** `GovernanceSettingKeys.cs`, settings seed/definition files, settings resolvers, instance administration API/UI, tenant settings API/UI, HAL policies if applicable, docs.
  - **Acceptance:** `mcp.enabled` and `mcp.enable_legacy_sse` resolve through startup ceiling -> instance setting/lock -> tenant setting; instance admins can enable/disable and lock; tenant admins can override only when unlocked; endpoint path/stateless are not runtime-editable.
  - **Progress:** Implemented `McpSettingDefinitions`, `McpSettingGroup`, MCP governance DTOs, instance governance service/commands/endpoints, tenant policy read/apply validation, runtime `McpRuntimeStateService`, `McpRuntimeGateMiddleware`, safe health data, instance admin MCP controls, and tenant override controls. Runtime default is enabled only inside the DB governance layer; startup `Mcp:Enabled=false` still prevents endpoint mapping. Endpoint path/stateless remain startup-only.
  - **Validation:** `McpSettingGroupTests`, `InstanceGovernanceSettingServiceTests`, `McpRuntimeStateServiceTests`, `McpAdapterHealthCheckTests`, MCP authorization/runtime-disabled integration tests, `Explore.Blazor.Client` Release build, Blazor client tests, architecture tests, full Release build, `git diff --check`, and full API integration test pass.
  - **Effort:** L

- [ ] **13.3 Replace endpoint-wide MCP auth with optional API-key context**
  - **Files:** `Explore.API/Program.cs`, auth extensions/handlers, MCP middleware/filters, rate limiting, `Event.API.IntegrationTests/Features/Mcp*.cs`.
  - **Acceptance:** Normal external MCP uses `X-API-Key`/`ISLAMU_EVENT_API_KEY`, not bearer-token-first docs; no-key and invalid-key requests can use only anonymous-safe tools/resources; authorized MCP operations require a valid key; `/api` auth behavior is unchanged.
  - **Progress:** Partially implemented. `MapMcp(...).AllowAnonymous()` removed endpoint-wide authorization; SDK authorization filters and method metadata now decide per-operation posture. `list_ai_tool_contracts` is `[AllowAnonymous]`; scoped MCP tools/resources/prompts use API-key scope-aware authorization policies. `/mcp` now participates in auth-conflict and tenant middleware; invalid or revoked API keys can fall back to anonymous-safe discovery when tenant context is resolved; explicit tenant mismatch returns `404`; valid scoped API keys can discover only the MCP operations their scopes allow; and bearer+API-key requests return bad request. Remaining gap: rate-limit partition coverage.
  - **Validation:** MCP protocol tests for no key, invalid key, revoked key, tenant mismatch, valid scoped key, and bearer/API-key conflict paths.
  - **Effort:** L

- [ ] **13.4 Add MCP API-key scope catalog and authorization mapping**
  - **Files:** `ExternalApiKeyScopes.cs`, API-key DTO/validation code, authorization policies, MCP registry metadata, docs/tests.
  - **Acceptance:** Grantable MCP scopes exist; unknown scopes are rejected; `events:read` remains read-only; proposal/write tools require explicit proposal/write scopes; missing scopes return safe forbidden outcomes.
  - **Progress:** Partially implemented. Added `mcp:read` and `mcp:propose`, included both in user/tenant ceilings, mapped `mcp:read` to AI conversation view plus `mcp:propose` to AI conversation proposal, and added MCP-specific authorization policies so read-only keys cannot discover/call proposal tools. Unit tests prove MCP scopes do not grant event read/write, send-message, confirmation, or generic user-write authority; create/update command tests reject unknown MCP-like scopes; and API integration tests cover read-only call failure shape. Remaining gap: private-data read parity for any future Phase 13.5 anonymous/public read resources.
  - **Validation:** External API Key tests and MCP scope integration tests.
  - **Effort:** M-L

- [ ] **13.5 Add anonymous-safe MCP read tools/resources from the registry**
  - **Files:** ATCR catalog/definitions, explicit MCP read tools/resources, query handlers for public events if needed, tests.
  - **Acceptance:** Anonymous MCP reads match public API visibility, stay rate-limited, preserve tenant/public filters, return bounded DTO/HAL-style data, and never expose arbitrary controllers/repositories/private content.
  - **Validation:** API/MCP tests comparing anonymous API and MCP visibility outcomes.
  - **Effort:** L

- [ ] **13.6 Harden MCP rate limiting, tenant resolution, audit, and health**
  - **Files:** rate-limit configuration, tenant middleware, health checks, telemetry, operations docs.
  - **Acceptance:** `/mcp` partitions anonymous/keyed traffic correctly, multi-tenant requests fail closed without trusted tenant binding, invalid-key attempts are counted without secret leakage, and health reports startup/runtime effective state safely.
  - **Progress:** Partially implemented for tenant/auth-conflict middleware, runtime gate, and safe health keys. `/mcp` now uses the API auth-conflict guard and tenant pre/post-auth middleware, runtime-disabled MCP returns `404`, tenant mismatch returns `404`, and health reports startup/runtime effective-state plus legacy-SSE requested/enabled booleans without tenant IDs or secrets. Rate-limit partitioning and invalid-key audit/metrics are still pending.
  - **Validation:** Rate-limit/tenant integration tests and safe health payload assertions.
  - **Effort:** M

- [x] **13.7 Implement legacy SSE runtime governance or a safe unavailable state**
  - **Files:** MCP settings/transport registration, ADR/docs, API tests.
  - **Acceptance:** `MCP_ENABLE_LEGACY_SSE` acts as a startup ceiling; `mcp.enable_legacy_sse` resolves through instance/tenant settings and locks; startup false blocks runtime enable; no stateful/session-affinity behavior is exposed unless official SDK review and tests approve it.
  - **Progress:** Safe-unavailable state implemented. Startup `MCP_ENABLE_LEGACY_SSE=true` no longer fails validation, `mcp.enable_legacy_sse` resolves through instance/tenant settings and locks, but `Program.cs` still does not enable SDK legacy SSE and health reports `legacySseRuntimeEnabled=false`.
  - **Validation:** Startup/runtime option tests, `McpRuntimeStateServiceTests`, `McpAdapterHealthCheckTests`, focused/full MCP API integration coverage, Context7 official SDK review confirming legacy SSE requires stateful mode, and redacted docs update.
  - **Effort:** M-L

- [ ] **13.8 Refresh MCP client configs, runbooks, and contract tests**
  - **Files:** `docs/MCP_DEBUGGING.md`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, diagnostic replay/evaluation scenarios, MCP integration tests.
  - **Acceptance:** Docs explain local `https://localhost:7039/mcp`, API-key optional behavior, invalid-key anonymous fallback, scope requirements, runtime settings, locks, and rollback with no real keys/endpoints/payloads.
  - **Progress:** Partially implemented. MCP debugging/configuration/operations/self-hosting/API/security/ADR docs now describe API-key-first external clients, `ISLAMU_EVENT_API_KEY`, anonymous/invalid-key registry discovery only, conflict handling, MCP scopes, runtime `mcp.enabled`/`mcp.enable_legacy_sse`, tenant locks, runtime rollback, and legacy-SSE startup ceiling/runtime-disabled state. Remaining doc work is focused on any future Phase 13.5 anonymous read resources and rate-limit/audit behavior once implemented.
  - **Validation:** Docs diff checks, doctor checks if updated, replay/evaluation reports, focused MCP tests.
  - **Effort:** M

## Verification Checklist

- [ ] LSP diagnostics clean for modified files.
- [x] `dotnet build --configuration Release --verbosity quiet` passes before final closure or failures are documented as unrelated/pre-existing.
- [ ] `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` passes when Domain lifecycle changes are included.
- [ ] `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passes for Application slices.
- [x] `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` targeted tests pass for registry/action slices.
- [ ] `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` passes for provider/MCP infrastructure slices.
- [x] `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` targeted/full tests pass for API/HAL/MCP slices.
- [ ] `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` targeted tests pass for persistence/reference/retention slices.
- [x] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` passes for Blazor slices.
- [ ] `dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet` passes when UI flow changes require it.
- [x] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` attempted before PR; unrelated pre-existing failures recorded separately if present.
- [ ] OpenAPI/client/changelog regenerated when API changes land.
- [ ] HAL link tests prove UI affordance gating.
- [ ] AI authorization/resource parity tests pass for local and Cerbos modes when affected.
- [ ] Send-message and confirm idempotency tests pass.
- [ ] Provider endpoint validation, safe health, and no-content logging tests pass when provider/MCP slices are affected.
- [x] Phase 9 `IChatClient` adapter mapping and raw fallback parity tests pass before SDK-backed providers become default.
- [x] Phase 9 `content_filtered`, strict schema, invalid tool input, and safe self-correction tests pass when provider hardening lands.
- [x] Phase 9 tokenizer budget tests pass before tokenizer-backed prompt/reference/tool caps replace character-only budgeting.
- [x] Phase 9 telemetry redaction tests prove no prompt/response/reference/tool payload/endpoint/key/model secret/tenant/user identifier/raw provider error leaks.
- [x] Phase 9 evaluation reports remain advisory unless a later plan promotes stable checks to CI gates.
- [x] Phase 9 vector/RAG prototypes prove tenant/public visibility filters before indexing beyond public event summaries.
- [x] Phase 10 registry metadata, structured recovery, schema format, hidden runtime context, and mapper parity tests pass before exposing new tool metadata to providers/MCP.
- [x] Phase 10 scoped catalog tests prove route/workflow context never grants execution authority without API/HAL authorization.
- [x] Phase 10 generated inventory and readiness analyzer outputs are deterministic, redacted, and covered by drift/path checks.
- [x] Phase 10 schema-only context summaries are explicit allow-lists and pass tenant/public visibility tests.
- [x] Phase 10 plan-preview validation proves unsupported/stale/unauthorized steps cannot execute before user confirmation.
- [x] Phase 10 fake/replay-provider e2e usability scenarios run without live provider credentials in normal CI.
- [x] Phase 11 official MCP SDK conformance tests prove tool/resource/prompt attributes, method descriptions, parameter descriptions, explicit stateless HTTP transport configuration, and explicit type registration.
- [x] Phase 11 MCP auth-filter tests prove endpoint auth, SDK method authorization metadata, and proposal-first MediatR/HAL authorization remain fail-closed.
- [x] Phase 11 registry-to-MCP projection tests prove schema parity, SDK annotations as hints only, and proposal-first mutation behavior.
- [x] Phase 11 transport/AOT posture tests prove product MCP remains API-hosted stateless Streamable HTTP, avoids product stdio/legacy-SSE wiring, and keeps AOT-sensitive SDK registration explicit.
- [x] Phase 11 Inspector/runbook checks prove deterministic replay artifacts cover MCP discovery/proposal posture while manual Inspector runs remain redacted, authenticated, fake/disposable-data-only, and outside normal CI.
- [x] Phase 11 protocol-evolution review gates stateful/server-to-client/client-shim capabilities behind ADR/task review before client-visible behavior changes.
- [x] Phase 12 local debug docs/templates are redacted, fake/disposable-data-only, and do not commit real MCP client secrets.
- [x] Phase 12 official-SDK-or-compatible MCP client contract tests prove discovery and proposal-only calls through `initialize`, list operations, and `tools/call`.
- [x] Phase 12 protocol error/redaction tests prove malformed/unauthorized/invalid MCP requests fail safely without raw payload or secret leakage.
- [x] Phase 12 projected-tool binding/cancellation tests prove schema/DI/cancellation behavior without repository mutation.
- [x] Phase 12 deterministic MCP replay/evaluation scenarios remain read-only/advisory and require no live provider credentials in normal CI.
- [x] Phase 12 compatibility matrix is current before any future `ModelContextProtocol.AspNetCore` upgrade or client-visible behavior change; doctor readiness checks are implemented.
- [x] Phase 13 startup-ceiling/runtime-setting matrix tests prove DB settings cannot expose MCP when `MCP_ENABLED=false` and cannot expose legacy SSE when `MCP_ENABLE_LEGACY_SSE=false`.
- [x] Phase 13 no-key/invalid-key/valid-key MCP tests prove anonymous-safe reads remain available while scoped tools require valid API keys.
- [ ] Phase 13 API-key scope tests prove least-privilege keys cannot propose writes or read private data beyond their grants. Proposal-scope denial is covered; private read parity remains tied to any future Phase 13.5 read resources.
- [x] Phase 13 instance/tenant lock tests prove tenant overrides cannot bypass instance administrator policy.
- [x] Phase 13 docs and client examples are API-key-first, anonymous-aware, redacted, and do not expose endpoint path/stateless as runtime settings.
- [ ] MVP quota/prompt/reference/model/tool limits and retention/redaction gate tests pass.
- [x] No real AI provider calls are required for tests.
- [x] Docs updated where behavior/config/operations/API changed.
- [x] Dev docs refreshed with final state and remaining work before handoff.

## Remaining / Deferred Work

- Old `dev/active/ai-integration` archive is deferred until user approves this new workstream.
- Direct MCP mutation without human confirmation is deferred and requires explicit policy approval.
- Additional tools beyond `CreateEventDraft` remain deferred until product scope is approved; registry schema/mapper/executor parity has been proven for the first tool.
- Phase 9 SDK-backed provider default adoption remains deferred until operator rollout evidence exists; `IChatClient` adapter parity, raw fallback behavior, strict schema handling, `content_filtered` mapping, redacted metric telemetry foundations, and token-budgeted prompt/reference/tool packing are now implemented.
- Advisory AI evaluation reports are implemented but deferred from hard CI gates until model/provider volatility, caching, cost, and false-positive posture are understood.
- Vector/RAG policy foundation is implemented; production ingestion/query rollout is deferred until tenant-safe event-summary ingestion/query design is product-approved. Do not index private event content by default.
- AgentBlazor reflection-based service/action execution is rejected for ISLAMU; future tooling may analyze candidates, but runtime tools must be explicit registry definitions.
- Direct remote MCP tool import/execution is rejected for this workstream; ISLAMU exposes selected registry tools through its API-hosted MCP adapter and keeps mutations proposal-first.
- First-class registry-to-MCP tool projection is implemented for registry-exposed proposal tools; the generic `propose_ai_tool_action` wrapper remains as a safe fallback path.
- Method-level MCP authorization filters remain authoritative for scoped MCP operations. Phase 13 replaced endpoint `RequireAuthorization()` with anonymous endpoint mapping plus per-tool/resource authorization so explicit anonymous-safe discovery can work without weakening MediatR/HAL authorization for proposals and side effects.
- Product MCP stdio hosting, stateful MCP sessions, and Native AOT support are deferred unless a new ADR/task adds a separate host or publish profile with verification. Legacy SSE is now a Phase 13 runtime-governance investigation/implementation item, gated by startup ceiling, locks, and official SDK compatibility evidence.
- Phase 12 may evaluate a local-only stdio diagnostic host, but product stdio remains rejected unless a new ADR/user-approved task implements it with stdout/stderr, auth/tenant simulation, and self-hosting non-impact verification.
- Automated live Inspector/Copilot MCP runs remain deferred from normal CI; use fake/disposable data, deterministic protocol tests, and redacted manual smoke evidence instead.
- MCP sampling, elicitation, roots, completions, progress notifications, list-changed notifications, resource subscriptions, and client-specific compatibility shims are deferred unless a new ADR/task models transport, auth, tenant isolation, redaction, side effects, self-hosting, and rollback.
- API-key-first MCP, anonymous-safe registry discovery, invalid/revoked-key fallback, tenant-mismatch fail-closed behavior, MCP scopes, read/propose policy filtering, runtime MCP enable/disable settings, tenant locks, and safe legacy-SSE unavailable state are now implemented in Phase 13. Additional anonymous-safe read resources, MCP rate-limit partition assertions, invalid-key audit metrics, and private-read parity tests remain pending.
- Arbitrary EF entity exposure, SQL/LINQ generation, repository access, and private-content schema summaries are rejected; only explicit safe schema summaries may be considered.
- Real-provider usability/e2e runs are deferred from normal CI and require explicit manual/nightly posture with redacted artifacts.
- Cross-request provider abort orchestration and streaming transport remain deferred. Persisted cancellation and authenticated polling are implemented; `ai_assistant.streaming_enabled` stays disabled/reserved until a future streaming hardening slice.
- Federation-aware AI references are deferred; first release should use local tenant events only.
