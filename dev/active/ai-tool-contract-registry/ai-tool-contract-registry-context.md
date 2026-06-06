<!-- ABOUTME: Operational context for the AI Tool Contract Registry workstream. -->
<!-- ABOUTME: Captures current evidence, decisions, risks, and next actions for registry and MCP adapter planning. -->

# AI Tool Contract Registry — Context

Last Updated: 2026-06-06 Europe/Brussels

## SESSION PROGRESS (2026-06-06 Europe/Brussels)

### ✅ COMPLETED

- Integrated the completed official .NET AI documentation report into the ATCR dev docs.
- Re-baselined the plan to preserve completed Phases 0-8 and add Phase 9 as the post-ATCR official .NET AI alignment roadmap.
- Captured report-backed recommendations for `Microsoft.Extensions.AI.IChatClient`, SDK-backed providers with raw fallback, redacted OpenTelemetry/GenAI telemetry, strict schema/self-correction, stable `content_filtered` mapping, tokenizer budgets, structured output, advisory `Microsoft.Extensions.AI.Evaluation` reports, future tenant-safe `Microsoft.Extensions.VectorData`/`IEmbeddingGenerator` RAG, and Azure managed identity through `DefaultAzureCredential`.
- Fully analyzed `/home/amir/dev/Github/AgentBlazor` as a comparative .NET/Blazor agent implementation and integrated beneficial patterns as Phase 10 of the ATCR roadmap.
- Captured AgentBlazor-inspired future work for richer registry metadata, structured safe tool recovery results, schema/argument-shape hardening, route/workflow-scoped catalogs, generated agent contract inventories, dev-only readiness/scaffold analyzers, safe schema-only context summaries, multi-step proposal preview/validation, and fake/replay-provider usability loops.
- Recorded explicit non-transferable patterns: no reflection-based arbitrary service/action execution, no Blazor-local authorization, no direct remote MCP tool import/execution, no arbitrary EF entity/query/SQL/LINQ exposure, no content-bearing prompt traces, and no real-provider dependency in normal CI.
- Implemented Phase 9.1 provider alignment: added `Microsoft.Extensions.AI.Abstractions`, kept `IChatClient` inside `Explore.Infrastructure`, added `MicrosoftExtensionsAiChatProvider` behind the existing Application-owned `IAiChatProvider` contract, and covered provider-neutral message/options/response/token/tool-call mapping with Infrastructure tests.
- Implemented Phase 9.2 provider-mode wiring: added explicit `openai-sdk` and `azure-openai` modes, SDK-backed `IChatClient` factory registration only when those modes are configured, Azure OpenAI `api-key`/`default-azure-credential` posture, runtime selector delegation to the SDK adapter, and operator/self-hosting docs.
- Implemented Phase 9.3 redacted provider telemetry: added platform-owned `Explore.Ai.Provider` spans, bounded `Explore.Business` metrics for request duration/token usage/proposed actions, SDK/raw provider adapter telemetry, and BusinessMetrics redaction tests proving metric tags do not include prompts, responses, tool payloads, provider endpoints, model IDs, provider request IDs, tenant/user IDs, API keys, or raw errors.
- Implemented Phase 9.4 strict schema and self-correction hardening: registry-backed JSON schema tool declarations for the SDK adapter, raw OpenAI-compatible `strict: true` function-tool metadata, strict Application schema-subset validation, safe parser correction messages, one-shot bounded self-correction retry in `SendAiMessageCommandHandler`, fail-closed `invalid_tool_arguments` for malformed SDK tool calls, SDK `ChatFinishReason.ContentFilter` mapping to stable `content_filtered`, and raw OpenAI-compatible `finish_reason: "content_filter"`/`"content_filtered"` mapping to the same safe failure code.
- Refactored the Phase 9.4 implementation for maintainability without behavior drift: schema-subset checks now live in `AiToolJsonSchemaPayloadValidator`, shared safe retry wording lives in `AiToolCorrectionMessages`, and provider send/parse/retry resolution now lives in `AiProviderResponseResolver` so `SendAiMessageCommandHandler` remains focused on CQRS orchestration, persistence, quota/idempotency, and domain state transitions.
- Implemented Phase 9.5 token-budgeted prompt/reference/tool packing: added an Application-owned `IAiTokenEstimator` seam, deterministic fallback estimator, shared `AiPromptTokenBudget`, token-aware provider message selection/truncation, over-budget tool-schema omission, and optional selected-reference token caps while preserving existing count/character fallback limits.

### 🟡 IN PROGRESS

- Phase 9 is in progress. Phase 9.1, 9.2, 9.3, 9.4, and 9.5 are implemented and verified. The next provider-hardening slice is Phase 9.6 structured output or Phase 9.7 advisory evaluation reports.

### ⏭️ NEXT

1. Start Phase 9.6 structured output for non-action assistant modes if provider hardening continues.
2. Keep SDK-backed providers opt-in until operator rollout evidence is settled, and preserve the raw OpenAI-compatible fallback for generic/self-hosted providers.
3. Keep Phase 9 evaluation reports advisory until cost, volatility, caching, and report stability are understood.
4. Treat Phase 10 as a future guarded roadmap over the existing registry/HAL/MediatR foundation, not as permission to expose Blazor services, EF entities, or remote MCP tools directly.

### ⚠️ BLOCKERS

- No blocker for the Phase 9.1/9.2/9.3/9.4/9.5 provider-hardening slices.
- Full Release build now passes for 25 projects with 0 errors. Broader integration/Blazor test suites were not rerun in the Phase 9.5 slice.

## SESSION PROGRESS (2026-06-01 Europe/Brussels)

### ✅ COMPLETED

- Created this new workstream to supersede `dev/active/ai-integration` after user review.
- Read `/dev-docs` command requirements and Senior CTO planning guardrails.
- Inspected old AI integration plan/context/tasks and carried forward unfinished Phase 4-8 work.
- Verified current implementation evidence: hardcoded `CreateEventDraft` schema/parser path, existing strict mapper, existing AI persistence/API/provider foundation, no MCP code, and roadmap-only MCP docs.
- Wrote registry-centered plan, context, and task checklist.
- Audited old Phase 4-8 tasks against this workstream and patched missing fidelity items: Plane inspiration credit, dock/accessibility docs, separate Blazor state/reference/proposal/full-panel tasks, explicit cancellation semantics, advanced dashboards/runbooks, and the full final validation matrix.
- Implemented Phase 1.1 registry contracts in `Explore.Application/Features/AiAssistant/Tools/*` with no API/Blazor/Persistence dependency.
- Implemented Phase 1.2 `AiToolPayloadGuard` plus unit coverage for malformed JSON, non-object payloads, unknown fields, forbidden fields, and registry-backed validation.
- Implemented Phase 1.3 registry-backed prompt/parser wiring: `AiSystemPromptFactory` reads tool schema/kinds from the registry, `AiStructuredActionParser` validates provider payloads through the registry, and `SendAiMessageCommandHandler` shares one default registry instance between prompt construction and parser validation.
- Implemented Phase 2.1/2.2 `CreateEventDraft` registry migration hardening: the tool definition now carries mapper metadata, authorization metadata, provider/MCP exposure flags, and schema/mapper drift tests.
- Implemented Phase 3.1/3.2 proposed-action confirmation and first executor path: confirm/reject Application commands, fail-closed tenant/user checks, duplicate-safe state handling, and `CreateEventDraft` execution through `CreateEventCommand`/MediatR.
- Implemented Phase 3.3 safe execution-result metadata: confirmed tool attempts now persist bounded `AiToolExecution` audit rows for success/failure and tenant-filtered query support without storing raw provider/tool payloads.
- Implemented Phase 4.1/4.2/4.3/4.4 API/HAL/OpenAPI confirmation surface: confirm/reject endpoints, nested proposed-action HAL affordances, DB-backed duplicate-confirm flow coverage, and regenerated OpenAPI/client/inventory artifacts.
- Implemented Phase 5.1/5.2 bounded event reference search: lightweight AI reference DTO/query contracts, tenant-filtered event repository search, and Application/Persistence test coverage without exposing full event content.
- Implemented Phase 5.3 reference API/HAL and prompt packing: authenticated reference search endpoint, HAL event links, generated OpenAPI/client/inventory artifacts, and bounded `AiReferencePromptPacker` with safe quoted boundaries.
- Implemented Phase 6.1/6.2 Blazor client foundation: generated-client service wrapper, assistant conversation state model, HAL-gated proposal/reference helpers, service registration, and Blazor service/state tests.
- Implemented Phase 6.3 assistant rail layout: the shell rail now uses the generated-client service/state foundation to load conversations, show messages, search/select references, send prompts, and render proposed action controls only from HAL links.
- Implemented Phase 6.4/6.5 dedicated Blazor reference picker/chip and proposed-action/result card components with HAL-gated actions and focused bUnit coverage.
- Implemented Phase 6.6 focused full-panel bUnit coverage for unavailable state, history/message loading, create, send, references, HAL-gated proposed actions, and safe command errors.
- Closed the Phase 0 archive pointer by adding `dev/pause/ai-integration/README.md`, because the old active AI integration directory was already absent.
- Implemented Phase 8.1 tenant-scoped AI retention cleanup/redaction primitive using `ai_assistant.retention_days`, dry-run support, content redaction, and soft-delete.
- Implemented Phase 8.2 persisted AI run cancellation semantics with authenticated API endpoint, cancellable-run HAL affordance, and safe terminal-run conflict handling.
- Completed Phase 8.3 streaming/polling decision: authenticated polling remains the supported AI run progress transport, and streaming stays disabled/reserved for a future hardening slice.
- Implemented Phase 8.4 scheduled AI retention operations: tenant-iterating cleanup service, hosted processor, health check, low-cardinality metrics, static scheduler settings, and runbook/config documentation.
- Completed Phase 8.5 final docs/validation refresh across API, configuration, operations, self-hosting, Blazor, dock, accessibility, changelog, and active workstream docs.

### ✅ COMPLETE

- Phase 0-6, Phase 8.1/8.2/8.3/8.4/8.5, and Phase 7.1/7.2/7.3 are complete. The AI Tool Contract Registry implementation plan is complete from archive pointer through MCP adapter documentation.

### ⏭️ NEXT

1. Resolve unrelated dirty-worktree verification blockers if a fully green repository gate is required.
2. Review the completed workstream for PR composition and release notes.
3. Keep AI/MCP logs, health data, metrics, and browser payloads free of prompt content, tool payloads, provider responses, tenant IDs, endpoint URLs, and secrets.
4. Keep reference/proposal UI actions HAL-gated; do not add local role/claim checks for assistant affordances.

### ⚠️ BLOCKERS

- No implementation blocker remains for this workstream.
- MCP hosting/protocol package selection is resolved in `docs/adr/ADR-010-mcp-adapter-hosting-strategy.md`: API-hosted, disabled-by-default, stateless Streamable HTTP through the official C# MCP SDK.
- Old AI migration history is mixed; avoid claiming a clean AI-scoped migration.
- Broad Blazor AI enablement now has retention cleanup, scheduling, health, metrics, run-status polling, cancellation, and HAL-gated UI foundations. MCP remains the next major adapter boundary.
- Lossless handoff audit is now complete; no known old Phase 4-8 task remains unmapped after the latest patch.
- Worktree was already heavily dirty before this slice, including deleted `dev/active/ai-integration/*` files and unrelated CI/API/Blazor/Infrastructure changes. This slice did not revert or modify those unrelated changes.

## Quick Resume

1. Read `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-plan.md`.
2. Read `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-tasks.md`.
3. Do not continue old `dev/active/ai-integration` tasks directly; use `dev/pause/ai-integration/README.md` as the archive pointer.
4. If continuing this workstream for the official .NET AI report, start Phase 9 rather than reopening Phases 0-8.
5. If continuing the AgentBlazor-inspired roadmap, start Phase 10 tasks from registry metadata/recovery/schema hardening before generated inventories or multi-step plan previews.
6. Keep all future MCP tools registry-backed and proposal/confirmation-first for mutations.
7. Keep all three dev docs updated after every meaningful implementation slice.

## Official .NET AI Report Integration — 2026-06-05 Europe/Brussels

- **Current implementation state:** ISLAMU Event already has an Application-owned `IAiChatProvider` boundary, raw OpenAI-compatible Infrastructure adapter, safe provider settings validation, tenant/user/quota/idempotency-guarded send orchestration, registry-backed proposal-only tool calls, bounded reference prompt packing, disabled-by-default MCP adapter over the official MCP C# SDK, and metadata-only health/readiness posture.
- **Report impact:** The official docs do not require replacing the ATCR architecture. They recommend strengthening the provider side with official `Microsoft.Extensions.AI` abstractions, telemetry/evaluation/token budgeting patterns, and future vector/RAG primitives while keeping product authority in existing CQRS/MediatR handlers and HAL-gated UI affordances.
- **Boundary decision:** Keep `Explore.Application/Contracts/Infrastructure/Ai/IAiChatProvider.cs` as the Clean Architecture boundary. `Microsoft.Extensions.AI.IChatClient` belongs inside Infrastructure adapters only; Application should not consume SDK/provider types.
- **Provider decision:** Prefer SDK-backed clients for supported providers, but keep the existing raw OpenAI-compatible fallback for generic/self-hosted providers until parity is proven.
- **Safety decision:** Any telemetry, evaluation, health, support-data, or future RAG output must exclude prompts, assistant responses, selected-reference content, raw tool payloads, provider endpoints, API keys, model secrets, tenant/user identifiers, and raw provider errors.
- **Testing decision:** `Microsoft.Extensions.AI.Evaluation` should initially produce cached/advisory trend reports for tool proposal correctness, refusal/safety behavior, prompt-injection resistance, groundedness, and event-draft regression. Do not make volatile model-scored checks hard CI blockers at first.
- **Future RAG decision:** If vector search is pursued, start with public/local tenant event-summary chunks, metadata/citations, tenant/public visibility filters, and ingestion/update hooks. Do not index private content without explicit product/security approval.

## AgentBlazor Comparative Analysis — 2026-06-05 Europe/Brussels

- **Scope analyzed:** README, generated `.agentblazor/AGENT.md`, core capability/action metadata, reflection registry, `CapabilityResult`, execution/trust contracts, plan validator, CLI/readiness/scaffold analyzer, generated inventory writer, route/action/service linking heuristics, EF schema-only package, hosting/AG-UI runtime, MCP HTTP tool consumer, telemetry/tracing contracts, e2e/usability harnesses, and package license metadata.
- **License posture:** No top-level `LICENSE*` file was found by glob, but `Directory.Build.props` declares `<PackageLicenseExpression>MIT</PackageLicenseExpression>`. This workstream still treats AgentBlazor as conceptual inspiration only; no implementation code should be copied.
- **Transferable concept 1 — capability metadata:** AgentBlazor actions carry descriptions, approval requirements, availability gates, planner instructions, follow-up policy, parameters, and route/workflow registration. ISLAMU should express this through ATCR metadata and tests, not attributes that execute arbitrary methods.
- **Transferable concept 2 — structured recovery:** AgentBlazor `CapabilityResult` supports safe summaries, warnings, next actions, clarification prompts, machine outputs, and recoverable validation failures. ISLAMU should add an equivalent redacted result/recovery contract for registry validation, model self-correction, and Blazor display.
- **Transferable concept 3 — schemas and hidden context:** AgentBlazor schema generation covers strict object schemas, scalar formats, enum allowed values, nullable/default handling, and context-injected parameters. ISLAMU should harden schema/mapper parity and hidden runtime context rules inside the registry.
- **Transferable concept 4 — scoped catalogs and generated inventory:** AgentBlazor route/workflow registrations and `.agentblazor/AGENT.md` inventory provide useful coverage and instruction artifacts. ISLAMU should generate a registry/API/HAL/OpenAPI-derived inventory with preserved manual sections and drift tests.
- **Transferable concept 5 — dev readiness analyzer:** AgentBlazor's install analyzer reports pass/warning/missing checks for host shape, package setup, services, workflows, endpoints, shell assets, providers, and chat surface. ISLAMU can build a dev-only analyzer that reports missing AI tool schema/mapper/executor/HAL/API/tests/docs/config pieces.
- **Transferable concept 6 — schema-only data context:** AgentBlazor's EF package is explicit opt-in and schema-only. ISLAMU can use explicit safe DTO/reference-projection summaries for prompt grounding, but must not expose repositories, arbitrary EF entities, SQL/LINQ, private content, or direct data access.
- **Transferable concept 7 — plan preview and usability:** AgentBlazor has execution-plan/risk/approval/freshness contracts and Playwright-style real usability runners. ISLAMU should adapt this as proposal-only plan preview/validation plus fake/replay-provider e2e scenarios and optional manual live-provider runbooks.
- **Non-transferable:** Reflection-based invocation, arbitrary service-method discovery as runtime authority, direct UI/component execution, Blazor-local auth decisions, direct imported MCP tool execution, content-bearing prompt traces, and live provider calls in normal CI conflict with ISLAMU's ATCR/CQRS/HAL/privacy rules.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `dev/active/ai-integration/*` | Existing | Docs | Old AI implementation workstream. | Superseded after user approval; unfinished tasks migrated here. |
| `dev/pause/ai-integration/README.md` | New | Docs | Archive pointer for the superseded old AI implementation workstream. | Old active directory was already absent; this file maps old unfinished work to this registry workstream. |
| `dev/active/ai-integration/ai-integration-plane-report.md` | Existing | Docs | Plane inspiration analysis/reference artifact. | Not implementation status; final docs should credit inspiration if UI materially uses those ideas. |
| `Explore.Application/Features/AiAssistant/Prompting/AiStructuredActionParser.cs` | Existing | Application | Validates provider proposed actions before persistence. | Registry-backed; rejects unknown kinds and invalid/forbidden payload fields before persistence. |
| `Explore.Application/Features/AiAssistant/Prompting/AiSystemPromptFactory.cs` | Existing | Application | Produces system prompt and action schema. | Reads provider-visible tool kinds/schema from the registry. |
| `Explore.Application/Features/AiAssistant/Actions/CreateEventDraftAiActionPayload.cs` | Existing | Application | Safe event draft payload shape. | Keep as first registered tool payload. |
| `Explore.Application/Features/AiAssistant/Tools/CreateEventDraftAiToolDefinition.cs` | New | Application | First registry-backed tool definition for provider schema, allowed fields, forbidden fields, confirmation posture, mapper metadata, authorization metadata, and exposure flags. | Added in Phase 1.3 and hardened in Phase 2. |
| `Explore.Application/Features/AiAssistant/Actions/CreateEventDraftAiActionMapper.cs` | Existing | Application | Strict mapper from untrusted JSON to `CreateEventDraftRequestDto`. | Uses the registry definition's allowed field set so schema/mapper fields do not drift. |
| `Explore.Application/Features/AiAssistant/Actions/CreateEventDraftAiToolExecutor.cs` | New | Application | Executes confirmed `CreateEventDraft` proposals. | Maps untrusted payload, dispatches `CreateEventCommand` through MediatR, and never writes event repositories directly. |
| `Explore.Application/Contracts/Infrastructure/Ai/AiChatModels.cs` | Existing | Application | Provider-neutral chat contracts and `AiStructuredActionSchema`. | Registry should feed `AiStructuredActionSchema`. |
| `Explore.Application/Features/AiAssistant/Handlers/Commands/SendAiMessageCommandHandler.cs` | Existing | Application | Orchestrates send-message and persists proposed actions. | Delegates provider send/parse/retry resolution to `AiProviderResponseResolver`; still does not execute tools during send. |
| `Explore.Application/Features/AiAssistant/Requests/Commands/ConfirmAiProposedActionCommand.cs` | New | Application | Authenticated command for confirming a proposed action. | Uses AI-conversation authorization metadata and action ID as the secure resource identifier; handler enforces tenant/user ownership. |
| `Explore.Application/Features/AiAssistant/Requests/Commands/RejectAiProposedActionCommand.cs` | New | Application | Authenticated command for rejecting a proposed action. | Reject path has no tool side effects and is duplicate-safe. |
| `Explore.Application/Features/AiAssistant/Handlers/Commands/ConfirmAiProposedActionCommandHandler.cs` | New | Application | Confirms, executes, and persists proposed-action state. | Fail-closed tenant/user checks; duplicate executed actions do not re-run tools. |
| `Explore.Application/Features/AiAssistant/Handlers/Commands/RejectAiProposedActionCommandHandler.cs` | New | Application | Rejects proposed actions without execution. | Fails closed for wrong tenant/user and invalid states. |
| `Explore.Domain/Ai/AiToolExecution.cs` | Existing | Domain | Execution audit metadata for confirmed tools. | Reused in Phase 3.3 for bounded success/failure metadata without raw provider/tool payload storage. |
| `Explore.Application/Contracts/Persistence/IAiConversationRepository.cs` | Existing | Application | AI aggregate repository contract. | Now exposes proposed-action transition methods and tenant-filtered tool execution create/query methods. |
| `Explore.Application/Features/Events/Requests/Commands/CreateEventCommand.cs` | Existing | Application | Authorized event create command. | `CreateEventDraft` executor must dispatch this, not write Event repository directly. |
| `Explore.API/Controllers/AiAssistantController.cs` | Existing | API | Authenticated AI API routes. | Now exposes thin confirm/reject proposed-action endpoints with AI rate limiting, safe ProblemDetails, and confirm `Idempotency-Key` propagation. |
| `Explore.API/Hateoas/Policies/AiAssistantLinkPolicy.cs` | Existing | API/HAL | AI conversation and proposed-action HAL affordances. | Emits `confirm-action`/`reject-action` only for active conversations with proposed actions; authorization fails closed through the existing link pipeline. |
| `Explore.API/Hateoas/Assemblers/AiConversationResourceAssembler.cs` | Existing | API/HAL | AI conversation HAL assembly. | Attaches nested proposed-action links using the async authorization evaluator before serialization. |
| `Explore.Application/DTOs/Ai/AiConversationDtos.cs` | Existing | Application | AI conversation/proposed-action DTOs. | `AiProposedActionDto` carries nullable `_links` for HAL-gated action cards. |
| `Explore.Application/DTOs/Ai/AiReferenceSearchResultDto.cs` | New | Application | Lightweight AI reference-search result. | Event-first metadata only; does not carry full event content. |
| `Explore.Application/DTOs/Ai/AiSelectedReferenceDto.cs` | New | Application | Future selected-reference prompt/input shape. | Reserved for Phase 5.3 prompt packing and API/HAL selection flow. |
| `Explore.Application/Features/AiAssistant/Requests/Queries/SearchAiReferencesQuery.cs` | New | Application | CQRS query contract for AI reference search. | Accepts search term and bounded limit. |
| `Explore.Application/Features/AiAssistant/Handlers/Queries/SearchAiReferencesQueryHandler.cs` | New | Application | Maps tenant-filtered event entities to safe AI reference DTOs. | Trims terms, clamps limits, returns empty for too-short terms, and excludes `Event.Content`. |
| `Explore.Application/Contracts/Persistence/IEventRepository.cs` | Existing | Application | Event repository contract. | Now exposes `SearchAiReferenceEventsAsync` returning Event entities for Application mapping. |
| `Explore.Persistence/Repositories/EventRepository.cs` | Existing | Persistence | Event EF repository implementation. | AI reference search uses `AsNoTracking()`, existing Event specifications, deterministic limit/order, and EF tenant filters. |
| `Explore.Application/Features/AiAssistant/Prompting/IAiTokenEstimator.cs` | New | Application | Defines provider-neutral token counting for prompt budgeting. | Enables tokenizer-backed implementations without leaking provider SDKs into Application. |
| `Explore.Application/Features/AiAssistant/Prompting/ApproximateAiTokenEstimator.cs` | New | Application | Deterministic fallback token estimator. | Uses conservative character-based counting when no provider tokenizer is configured. |
| `Explore.Application/Features/AiAssistant/Prompting/AiPromptTokenBudget.cs` | New | Application | Tracks remaining input-token budget across prompt sections. | Centralizes token consumption for system prompt, messages, references, and tool schemas. |
| `Explore.Application/Features/AiAssistant/Prompting/AiReferencePromptPacker.cs` | New | Application | Packs selected references for provider prompts. | Uses per-reference/total character budgets plus optional per-reference/total token budgets, XML-like safe boundaries, and escaping for text/attributes. |
| `Explore.API/Controllers/AiAssistantController.cs` | Existing | API | Authenticated AI API routes. | Also exposes `GET /api/ai/assistant/references` as a HAL collection with `event` links and no full event content. |
| `Explore.Blazor.Client/Contracts/Services/Ai/IAiAssistantClientService.cs` | New | Blazor Client | Service contract over generated AI assistant API client methods. | Keeps Razor components behind a typed service and exposes safe command results. |
| `Explore.Blazor.Client/Services/Ai/AiAssistantClientService.cs` | New | Blazor Client | Generated-client wrapper for AI assistant operations. | Handles bootstrap/history/detail/create/send/reference/confirm/reject, propagates idempotency keys, logs API failures, and preserves HAL resources. |
| `Explore.Blazor.Client/Services/Ai/AiAssistantConversationState.cs` | New | Blazor Client | Assistant conversation/reference/proposal state model. | Tracks selected conversation/references/loading/errors and gates confirm/reject/event affordances only by HAL link presence. |
| `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` | Existing | Blazor | Shell assistant rail UI. | Now uses `IAiAssistantClientService` and `AiAssistantConversationState` for conversation loading, messages, reference search, send, and HAL-gated proposal actions while preserving docked/fixed shell behavior. |
| `Explore.Blazor.Client/Components/Shell/AiAssistant/AiReferencePicker.razor` | New | Blazor | Dedicated event reference picker. | Debounces search, renders HAL event-link availability, selects/removes references, and delegates selected chips to `AiReferenceChip`. |
| `Explore.Blazor.Client/Components/Shell/AiAssistant/AiReferenceChip.razor` | New | Blazor | Selected reference chip. | Supports click, Delete, and Backspace removal for keyboard-accessible reference management. |
| `Explore.Blazor.Client/Components/Shell/AiAssistant/AiProposedActionCard.razor` | New | Blazor | Proposed-action card shell. | Renders Confirm/Reject only from HAL `confirm-action`/`reject-action` links and honors busy state to avoid duplicate local submits. |
| `Explore.Blazor.Client/Components/Shell/AiAssistant/CreateEventDraftActionPreview.razor` | New | Blazor | Safe CreateEventDraft proposal preview. | Shows only title/description from payload JSON and does not render raw privileged fields. |
| `Explore.Blazor.Client/Components/Shell/AiAssistant/AiActionResultCard.razor` | New | Blazor | Proposed-action result/failure display. | Keeps committed result/failure metadata separate from proposal preview. |
| `Explore.Application/Features/AiAssistant/Requests/Commands/RunAiRetentionCleanupCommand.cs` | New | Application | Tenant-scoped AI retention cleanup command. | Supports dry-run and optional `UtcNow` for deterministic operator/test runs. |
| `Explore.Application/Features/AiAssistant/Handlers/Commands/RunAiRetentionCleanupCommandHandler.cs` | New | Application | Resolves tenant AI retention settings and delegates cleanup. | Uses `AiAssistantSettingGroup.RetentionDays` and clamps invalid values to one day. |
| `Explore.Application/Models/AiRetentionCleanupResult.cs` | New | Application | Observable retention cleanup result. | Reports cutoff, retention days, eligible conversation count, redacted row counts, and dry-run mode without prompt content. |
| `Explore.Persistence/Repositories/AiConversationRepository.cs` | Existing | Persistence | AI conversation EF repository. | Now includes tenant-filtered retention redaction for expired conversation content and soft-delete. |
| `Explore.Application/Features/AiAssistant/Requests/Commands/CancelAiRunCommand.cs` | New | Application | Authenticated AI run cancellation command. | Uses AI conversation authorization metadata and conversation ID as the secure resource identifier. |
| `Explore.Application/Features/AiAssistant/Handlers/Commands/CancelAiRunCommandHandler.cs` | New | Application | Cancels queued/in-progress runs and persists conversation state. | Fails closed for wrong user/conversation/run, is idempotent for already-cancelled runs, and rejects terminal succeeded/failed runs. |
| `Explore.API/Controllers/AiAssistantController.cs` | Existing | API | Authenticated AI API routes. | Run status now emits `cancel-run` only for queued/in-progress runs; `POST /runs/{runId}/cancel` dispatches the cancellation command and returns safe ProblemDetails on conflicts. |
| `Explore.Infrastructure/AiRetentionCleanupService.cs` | New | Infrastructure | Scheduled all-tenant AI retention cleanup coordinator. | Iterates active tenants, sets tenant context per tenant, resolves `ai_assistant.retention_days`, invokes tenant-filtered redaction cleanup, and emits bounded metrics/logs only. |
| `Explore.API/BackgroundServices/AiRetentionCleanupProcessor.cs` | New | API | Hosted retention cleanup loop. | Uses `AiRetentionCleanup:*` static settings for enabled/dry-run/interval/pass bounds and never logs content-bearing AI data. |
| `Explore.API/HealthChecks/AiRetentionCleanupHealthCheck.cs` | New | API | AI retention cleanup readiness check. | Reports bounded scheduler posture only; no tenant IDs, prompts, payloads, provider data, or secrets. |
| `docs/adr/ADR-010-mcp-adapter-hosting-strategy.md` | New | Docs | Phase 7.1 MCP hosting, transport, auth, tenancy, and disable-path decision. | Selects API-hosted, disabled-by-default, stateless Streamable HTTP via official `ModelContextProtocol.AspNetCore`; no legacy SSE for first implementation. |
| `Explore.API/Mcp/AiToolRegistryMcpTools.cs` | New | API/MCP | Read-only registry discovery tool for the API-hosted MCP adapter. | Exposes safe tool contract metadata from `IAiToolContractRegistry`; does not execute mutations or expose prompt/provider/tool payload content. |
| `Explore.API/Mcp/AiAssistantMcpTools.cs` | New | API/MCP | Proposal-first mutating MCP tool surface. | Delegates to `ProposeAiToolActionCommand` through MediatR so MCP persists proposed actions only and never writes repositories directly. |
| `Explore.API/Mcp/AiAssistantMcpResources.cs` | New | API/MCP | Safe AI conversation MCP resources. | Uses MediatR queries and omits raw proposed-action payloads and message content from MCP resource output. |
| `Explore.API/Mcp/AiAssistantMcpPrompts.cs` | New | API/MCP | MCP prompt guidance for confirmation-first event drafts. | Guides external agents to use registry contracts and `propose_ai_tool_action`, then wait for product/API confirmation. |
| `Explore.API/Configuration/McpAdapterSettings.cs` | New | API | Static optional MCP adapter configuration. | Disabled by default; endpoint path and stateless posture validated; legacy SSE rejected for the initial adapter. |
| `Explore.API/HealthChecks/McpAdapterHealthCheck.cs` | New | API | MCP adapter readiness check. | Reports bounded enabled/path/stateless/SSE posture only; no tenant IDs, prompts, payloads, provider data, or secrets. |
| `docs/API.md` | Existing | Docs | API behavior reference. | Phase 8.3 records authenticated polling as the supported AI run progress transport and keeps streaming deferred. |
| `docs/semantic_versioning/v1.0.0.md` | Existing | Docs | Roadmap mentions MCP server support. | Superseded by the implemented Phase 7 MCP adapter docs for current status. |
| `dev/active/ai-tool-contract-registry/*` | New | Docs | New source of truth for registry/MCP-adapter implementation. | Created in this planning slice. |

## Key Decisions

1. Build an internal AI Tool Contract Registry before adding more tools.
2. Keep `CreateEventDraft` as the first registered tool and preserve Phase 5.1 safety behavior.
3. MCP is an optional adapter over the registry/API/Application boundary, not the core authority.
4. Mutating tools require human confirmation by default.
5. Existing CQRS commands remain authoritative for mutations.
6. Registry emits the same schemas for provider tool calls and future MCP tools to avoid drift.
7. Blazor proposal actions must be HAL-gated; no local role/claim checks for Confirm/Reject.
8. Retention/redaction must be addressed before broad AI history/UI enablement.
9. Phase 9 may use `Microsoft.Extensions.AI.IChatClient` only behind the existing `IAiChatProvider` Infrastructure adapter boundary.
10. SDK-backed providers should not remove the raw OpenAI-compatible fallback until self-hosted parity is proven.
11. GenAI telemetry/evaluation/RAG outputs must stay metadata-only and redacted according to the existing provider/MCP support-data policy.
12. AI evaluation reports are advisory/trend evidence until determinism, caching, provider cost, and false-positive behavior are understood.
13. Prompt budgeting remains Application-owned: use `IAiTokenEstimator` and `AiPromptTokenBudget` for preflight limits, keep provider SDK tokenizers optional behind this seam, and never log prompts, references, tool schemas, provider responses, tenant/user identifiers, or raw provider errors during budgeting.
14. AgentBlazor ideas are inspiration only; no AgentBlazor implementation code should be copied into ISLAMU Event.
15. AgentBlazor-inspired metadata, inventories, analyzers, recovery results, schema summaries, and plan previews must be generated from or validated against ATCR/API/HAL/OpenAPI authority.
16. Reflection-based execution, direct Blazor component/service invocation, direct remote MCP tool import/execution, and arbitrary EF/query exposure are rejected for this workstream.
17. Real usability/e2e loops should use fake or replay providers in normal CI; live-provider runs are manual/nightly only and must redact artifacts.

## Constraints And Rules To Remember

- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- Domain remains pure; Application cannot depend on API/Blazor/Persistence implementations.
- Tool output from providers and MCP clients is untrusted.
- Confirmed mutations dispatch existing MediatR commands.
- Tenant isolation must fail closed.
- Private AI/MCP surfaces are authenticated even if general GET convention allows anonymous reads elsewhere.
- HAL links are the UI affordance authority.
- New C# files need two `ABOUTME:` comments.
- Use per-project test commands, not solution-level `dotnet test`.

## Validation Baseline

Planning-only docs were created in this slice. Implementation slices should use targeted commands:

```bash
dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --treenode-filter "/*/*/*AiTool*/*|/*/*/*CreateEventDraftAiAction*/*|/*/*/*AiPromptContextBuilderTests*/*" --no-progress --maximum-parallel-tests 1
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

API/Blazor/Persistence/MCP phases add their own project-level verification from the plan and tasks file.

## Current Known Risks / Unknowns

- Registry can become too abstract. Keep Phase 1 minimal and prove it with `CreateEventDraft` only.
- MCP hosting and authentication strategy require current .NET/MCP research in Phase 7, after Phase 8 is complete.
- Confirm/reject idempotency, event creation dispatch, execution audit row persistence, HAL proposed-action links, duplicate-confirm HTTP flow, and Phase 5 event reference tenant filtering are covered through tests.
- Phase 6 Blazor service/state/component behavior is covered by focused service/state/component/full-panel tests, plus build and architecture checks.
- Phase 8.1 cleanup is a callable Application/Persistence primitive, not a scheduled operator job yet. Scheduling, runbook, and metrics polish remain Phase 8 follow-up.
- Phase 8.2 cancellation persists run cancellation state and exposes HAL/API affordances. It does not yet implement cross-request provider HTTP abort orchestration for provider calls already in progress; send-message still honors its request `CancellationToken`.
- Architecture verification for Phase 8.2 currently fails on unrelated `Explore.Blazor.Client/Pages/User/Components/Dialogs/DeleteAccountDialog.razor.cs` direct `DialogOptions` construction, outside the AI cancellation slice.
- Existing unrelated architecture parity failures may obscure registry validation; record separately.
- Old mixed AI migration history may need self-hoster notes or later migration cleanup.
- Old AI task mapping after audit: old Phase 4 reference search maps to new Phase 5; old Phase 5 confirm/create-draft maps to new Phases 2-4; old Phase 6 Blazor panel maps to new Phase 6; old Phase 7 cancellation/streaming/retention/dashboards maps to new Phase 8; old Phase 8 docs/credit/final validation maps to new Phase 8.5 and verification checklist.
- Phase 9 SDK-backed provider work can accidentally leak provider abstractions into Application; detect by searching for `IChatClient` or provider packages outside Infrastructure/tests.
- Phase 9 telemetry/evaluation work can accidentally emit content-bearing AI data; require explicit redaction tests before enabling production telemetry.
- Phase 9 vector/RAG work has cross-tenant/private-content risk; require tenant-filtered ingestion/query tests and start with public event summaries only.
- Phase 10 AgentBlazor-inspired work can accidentally bypass ATCR by exposing services/components directly; reject reflection execution and require registry/HAL/MediatR tests.
- Generated agent inventories can drift from registry/API/HAL reality; generation must be deterministic and covered by docs/architecture checks.
- Schema-only context can leak private fields if the allow-list is too broad; start with selected DTO/reference projection summaries and tenant/public visibility tests.
- Multi-step plan previews can become direct execution if not constrained; keep them proposal-only until user confirmation dispatches existing commands.
- Usability/e2e artifacts can leak prompt/response/reference/tool content; use fake/replay providers in CI and redact artifacts.

## Handoff Notes

### Handoff — 2026-06-01 Europe/Brussels

- **Current state:** New planning workstream created for AI Tool Contract Registry and future MCP adapter. No production code changed in this planning slice.
- **Next action:** User review. If approved, mark/archive old `dev/active/ai-integration` and begin Phase 1 registry contracts.
- **Blockers:** None for Phase 1; MCP implementation details intentionally deferred to Phase 7 research.
- **Modified files:** `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-plan.md`, `ai-tool-contract-registry-context.md`, `ai-tool-contract-registry-tasks.md`.
- **Validation:** Docs consistency checks should confirm required headers and no placeholder tokens remain.
- **Documentation impact:** Old AI integration docs must later point to this workstream.
- **Risks:** Do not start MCP first; that would duplicate the tool contract boundary this plan is designed to centralize.
- **Notes for next contributor/agent:** Read current `CreateEventDraftAiActionMapper` and parser/prompt factory before editing; preserve Phase 5.1 tests as regression coverage.

### Handoff Audit — 2026-06-01 Europe/Brussels

- **Question answered:** The old AI integration workstream was not archived until verifying that its remaining work was represented here.
- **Result:** Initial registry docs carried the major phases but compressed some old tasks too much. The plan/tasks now explicitly include Plane inspiration credit, Blazor dock/accessibility docs, AI conversation state tests, separate reference picker/proposal card tasks, full panel bUnit tests, cancellation, streaming/polling, advanced provider/run dashboards, and the old final validation matrix.
- **Remaining archive action:** After user approval, update old `dev/active/ai-integration/*` to point here or move it per the project’s active-doc archival convention.

### Implementation Slice — 2026-06-01 Europe/Brussels

- **Current state:** Phase 1.1/1.2 code exists. The registry can expose definitions, find a definition by `AiProposedActionKind`, and validate payload JSON through a shared guard. The guard accepts only JSON objects and uses case-insensitive allow/deny field policies without echoing raw payload content in failure messages.
- **Files added:** `AiToolDefinition.cs`, `AiToolConfirmationMode.cs`, `IAiToolContractRegistry.cs`, `AiToolContractRegistry.cs`, `AiToolValidationResult.cs`, `AiToolExecutionContext.cs`, `AiToolPayloadGuard.cs`, and `Event.Application.UnitTests/Features/AiAssistant/Tools/AiToolPayloadGuardTests.cs`.
- **Behavior preserved:** Existing prompt factory, parser, send-message handler, and `CreateEventDraftAiActionMapper` are not wired to the registry yet; current provider-visible behavior remains unchanged until Phase 1.3.
- **Validation:** AFT diagnostics reported no errors for the new files, but C# LSP is not installed. `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passed with existing warnings. `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --no-progress --maximum-parallel-tests 1` passed: 1197 succeeded. `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` passed: 180 succeeded, 1 skipped.
- **Risks:** The old AI integration files were already deleted in the dirty worktree before this slice. If archival pointers are required, add them explicitly in a separate docs-only slice.

### Implementation Slice — 2026-06-01 Europe/Brussels — Phase 1.3

- **Current state:** Prompt schema generation and parser validation now consume the Application-layer AI tool registry. `CreateEventDraft` remains the only default tool definition and the only provider-visible action kind.
- **Files changed:** `AiSystemPromptFactory.cs`, `AiStructuredActionParser.cs`, `SendAiMessageCommandHandler.cs`, `AiToolContractRegistry.cs`, new `CreateEventDraftAiToolDefinition.cs`, plus `AiPromptContextBuilderTests.cs` and `AiStructuredActionParserTests.cs`.
- **Behavior preserved:** Tool proposals still produce the same `CreateEventDraft` schema shape when enabled, no action schema is emitted when proposals are disabled, and send-message still persists proposed actions only without executing tools or creating events.
- **Security/control-flow change:** Untrusted provider payloads now go through `IAiToolContractRegistry.ValidatePayload(...)`, which uses the shared JSON guard to reject unknown/forbidden fields before persistence. The send handler constructs one registry and shares it between prompt construction and parsing to avoid prompt/parser drift in the default path.
- **Tests added/updated:** Prompt tests now cover an empty registry producing no action schema. Parser tests now cover registry-driven validation, forbidden field rejection without raw field echo, and unknown-kind behavior through an empty registry.
- **Validation:** Targeted Application AI tests passed with existing warnings: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity normal -p:RunAnalyzers=false -- --treenode-filter "/*/*/*AiStructuredActionParserTests*/*|/*/*/*AiPromptContextBuilderTests*/*|/*/*/*AiToolPayloadGuardTests*/*|/*/*/*SendAiMessageCommandHandlerTests*/*" --no-progress --maximum-parallel-tests 1`. Full serial Application unit tests passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false -- --no-progress --maximum-parallel-tests 1` (1200/1200). `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false` passed. A later architecture verification rerun during Phase 2 passed: 180 succeeded, 1 skipped.
- **Remaining:** Phase 2.1 remains unchecked because the new definition still needs explicit mapper/authorization metadata decisions and schema/mapper drift tests before it is considered fully migrated.

### Implementation Slice — 2026-06-01 Europe/Brussels — Phase 2.1/2.2

- **Current state:** `CreateEventDraft` is now fully registered as the first governed registry tool. The definition carries provider schema, allowed fields, forbidden fields, confirmation mode, payload mapper type, required authorization resource/action, and provider/MCP exposure flags.
- **Files changed:** `AiToolDefinition.cs`, new `AiToolAuthorizationRequirement.cs`, `CreateEventDraftAiToolDefinition.cs`, `CreateEventDraftAiActionMapper.cs`, `AiSystemPromptFactory.cs`, and new `CreateEventDraftAiToolDefinitionTests.cs`.
- **Behavior preserved:** Existing `CreateEventDraftAiActionMapper` validation behavior remains strict. The send-message flow still only persists proposed actions and does not execute mutations.
- **Safety/control-flow change:** `CreateEventDraftAiActionMapper` now uses `CreateEventDraftAiToolDefinition.AllowedPayloadFields` instead of a private duplicate allow-list, making the registry the single source for accepted provider JSON fields. The tool definition records `ResourceKinds.Event` plus `AuthorizationActions.Create` for the future confirmation/executor boundary.
- **Tests added/updated:** New drift tests parse the JSON schema properties, compare them to the registry allowed fields, verify every allowed field is accepted by the mapper, verify every forbidden field is rejected by the registry guard, and assert mapper/auth/exposure metadata.
- **Validation:** Targeted AI tests passed: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity normal -p:RunAnalyzers=false -- --treenode-filter "/*/*/*CreateEventDraftAiToolDefinitionTests*/*|/*/*/*CreateEventDraftAiActionMapperTests*/*|/*/*/*AiStructuredActionParserTests*/*|/*/*/*AiPromptContextBuilderTests*/*|/*/*/*AiToolPayloadGuardTests*/*|/*/*/*SendAiMessageCommandHandlerTests*/*" --no-progress --maximum-parallel-tests 1`. Application build passed: `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false`. Full serial Application tests passed: 1208/1208. Architecture tests passed: 180 succeeded, 1 skipped.
- **Remaining:** Phase 3 must add confirm/reject Application commands and the first executor path. The executor must dispatch existing `CreateEventCommand` through MediatR and must not write event repositories directly.

### Implementation Slice — 2026-06-01 Europe/Brussels — Phase 3.1/3.2

- **Current state:** Application-level proposed-action confirmation is implemented for the first registry tool. Authenticated users can confirm or reject proposed actions belonging to their own tenant/conversation; not-found, wrong-tenant, and wrong-user paths fail closed as `proposed_action_not_found`.
- **Files changed:** `AuthorizationActions.cs`, `IAiConversationRepository.cs`, `AiConversationRepository.cs`, new confirm/reject commands, new confirm/reject handlers, new `CreateEventDraftAiToolExecutor.cs`, `AiAssistantAuthorizationMetadataTests.cs`, and new `AiProposedActionCommandHandlerTests.cs`.
- **Control flow:** `ConfirmAiProposedActionCommandHandler` loads the proposed action for update, checks tenant and conversation user ownership, treats already executed actions as duplicate-safe success, confirms proposed actions, maps `CreateEventDraft` payloads, and dispatches existing `CreateEventCommand` through MediatR. `RejectAiProposedActionCommandHandler` marks proposed actions rejected without invoking any executor.
- **Safety notes:** The executor does not write event repositories directly. It reuses `CreateEventDraftAiActionMapper` and `CreateEventCommand`, so existing validation, authorization metadata, transaction behavior, cache invalidation, metrics, and future outbox hooks stay on the canonical command path. Organization/group-scoped AI payloads currently fail closed because confirmation uses an empty mapping allow-list until scoped context is wired.
- **Tests added/updated:** Application tests cover unauthenticated confirm, wrong-user fail-closed behavior, confirm dispatching `CreateEventCommand`, duplicate confirm without re-execution, mapping failure without dispatch, reject success, duplicate reject, invalid reject states, and authorization metadata for new commands.
- **Validation:** AFT diagnostics reported no scoped diagnostics/TODOs, with C# LSP unavailable. Application build passed: `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false`. Focused AI/Application tests passed. Full serial Application unit tests passed: 1218/1218. Architecture tests passed: 180 succeeded, 1 skipped.
- **Remaining at that point:** Phase 3.3 safe execution-result metadata/queryability was still pending and has since been completed. Phase 4 must expose confirm/reject through API/HAL/OpenAPI and add DB-backed duplicate-confirm flow tests.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 3.3

- **Current state:** Confirmed AI tool attempts now persist execution audit metadata. The existing `AiToolExecution` schema was sufficient, so no migration was added.
- **Files changed:** `IAiConversationRepository.cs`, `AiConversationRepository.cs`, `ConfirmAiProposedActionCommandHandler.cs`, `AiProposedActionCommandHandlerTests.cs`, and `AiConversationRepositoryTests.cs`.
- **Control flow:** `ConfirmAiProposedActionCommandHandler` creates an `AiToolExecution` row when a supported proposed action is executed. Successful executions store tenant ID, proposed action ID, tool name, start/completion timestamps, and success state. Failed executions store the same bounded metadata plus safe failure code/message.
- **Safety notes:** Execution audit rows do not store provider prompt text, raw tool payload JSON, model responses, API keys, provider IDs, or endpoint URLs. The original proposed payload remains only on `AiProposedAction`, while execution rows are queryable by proposed action under tenant filters.
- **Tests added/updated:** Application command tests now assert success/failure execution rows are created for confirm attempts and reject/duplicate paths do not create rows. A PostgreSQL-backed repository test persists success and failure execution rows, verifies tenant filtering, and checks that safe audit fields do not contain the original payload content.
- **Validation:** AFT diagnostics reported no scoped diagnostics/TODOs, with C# LSP unavailable. Application build passed: `dotnet build Explore.Application/Explore.Application.csproj --configuration Release --verbosity quiet -p:RunAnalyzers=false`. Focused AI command tests passed: 1218/1218. Targeted persistence integration test passed: 1/1. Full serial Application unit tests passed: 1218/1218. Architecture tests passed.
- **Remaining:** Phase 4 must expose confirm/reject through API/HAL/OpenAPI and add DB-backed duplicate-confirm HTTP/HAL flow tests before Blazor action cards can be enabled.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 4.1/4.2/4.3/4.4

- **Current state:** Proposed-action confirmation is now exposed through the authenticated API and HAL contract. The generated OpenAPI document, generated API client, API contract inventory, and changelog have been refreshed for the new route surface.
- **Files changed:** `AiAssistantController.cs`, `AiAssistantProblemDetails.cs`, `RouteNames.cs`, `AiAssistantLinkPolicy.cs`, `AiConversationResourceAssembler.cs`, `LinkRelations.cs`, `AiConversationDtos.cs`, `ConfirmAiProposedActionCommand.cs`, `ConfirmAiProposedActionCommandHandler.cs`, `MachineScopeMapping.cs`, `cerbos/policies/islamuevent_ai_conversation.yaml`, API/Application tests, `schemas/openapi.json`, `docs/API_CONTRACT_INVENTORY.md`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, and `docs/API_CHANGELOG.md`.
- **Control flow:** API confirm/reject endpoints remain thin and authorized; they dispatch MediatR commands. Confirm passes `Idempotency-Key` through the command contract while the HTTP idempotency middleware owns replay protection. The confirmation handler now builds an organization/group publisher allow-list from membership repositories before executing `CreateEventDraft`, then dispatches the existing `CreateEventCommand` through MediatR.
- **HAL behavior:** `AiProposedActionDto` now serializes nested `_links`. The assembler adds `confirm-action` and `reject-action` only after the existing HAL authorization evaluator allows them, and the policy emits no proposal links for inactive conversations or non-proposed actions.
- **Safety notes:** The DB-backed confirmation test uses an organization publisher because the tenant’s personal event publishing setting intentionally rejects personal event creation. This preserves the real product rule instead of weakening `EventActorResolver`. Safe ProblemDetails mapping avoids prompt/payload leakage for proposed-action failures.
- **Tests/validation:** API build passed. Focused Application command/machine-scope tests passed. API controller contract tests passed. AI HATEOAS tests passed. PostgreSQL-backed confirm flow passed, proving send creates no event, confirm creates one draft event, and duplicate confirm returns the same event without a second insert. API contract inventory generation passed. Blazor Client build regenerated the NSwag client successfully. OpenAPI invariant tests passed. Architecture tests passed.
- **Remaining:** Phase 5 should add bounded event reference search and prompt packing before enabling richer Blazor assistant UX.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 5.1/5.2

- **Current state:** Bounded event reference search now exists at the Application/Persistence boundary. The API/HAL prompt-packing surface remains intentionally deferred to Phase 5.3.
- **Files changed:** Added `AiReferenceSearchResultDto.cs`, `AiSelectedReferenceDto.cs`, `SearchAiReferencesQuery.cs`, `SearchAiReferencesQueryHandler.cs`, `SearchAiReferencesQueryHandlerTests.cs`, and `EventAiReferenceRepositoryTests.cs`; updated `IEventRepository.cs` and `EventRepository.cs`.
- **Control flow:** `SearchAiReferencesQueryHandler` trims the search term, rejects too-short terms by returning an empty list, clamps limits to a maximum of 20, calls `IEventRepository.SearchAiReferenceEventsAsync`, and maps Event entities to lightweight AI reference DTOs in Application.
- **Repository behavior:** `EventRepository.SearchAiReferenceEventsAsync` returns Event entities, uses `AsNoTracking()`, applies `EventFilter.PubliclyDiscoverable()` plus search term filtering through `EventQuerySpecification`, preserves EF tenant filters, and orders deterministically before applying the limit.
- **Safety notes:** Reference results include event ID, display name, bounded summary, date/status/visibility/format metadata, and never expose full `Event.Content`. Persistence tests use tenant-filtered DbContexts and do not call `IgnoreQueryFilters()` in the runtime query path.
- **Tests/validation:** Scoped AFT diagnostics reported no diagnostics/TODOs with C# LSP unavailable. Application build passed. Targeted reference Application tests passed. Full serial Application unit tests passed: 1222/1222. Targeted PostgreSQL-backed Persistence reference tests passed: 2/2. Architecture tests passed: 180 succeeded, 1 known response-metadata test skipped.
- **Remaining:** Phase 5.3 should add the reference API/HAL endpoint and `AiReferencePromptPacker` with per-reference and total prompt budgets plus safe quoted boundaries before Blazor reference-picker work starts.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 6.1/6.2

- **Current state:** Blazor now has a non-UI AI assistant client foundation over the regenerated API client. Razor components can consume a typed service and state container instead of calling `IEventApiClient` directly.
- **Files changed:** Added `IAiAssistantClientService.cs`, `AiAssistantCommandResult.cs`, `AiAssistantClientService.cs`, `AiAssistantConversationState.cs`, `AiAssistantClientServiceTests.cs`, and `AiAssistantConversationStateTests.cs`; updated `ServiceCollectionExtensions.cs`.
- **Control flow:** `AiAssistantClientService` wraps generated bootstrap, conversation history/detail, create, send, reference search, confirm, and reject methods. Send and confirm propagate idempotency keys to generated header parameters, and command responses are normalized into `AiAssistantCommandResult`.
- **HAL behavior:** The service preserves HAL resource wrappers instead of flattening them. `AiAssistantConversationState` exposes helpers for `confirm-action`, `reject-action`, and `event` affordances that check only `_links` presence and never inspect roles or claims.
- **Safety notes:** `ApiException` paths return safe defaults or `api_error` command results and log through `ILogger`. No raw HTTP calls, token handling, local authorization checks, or component-level generated-client usage were added.
- **Tests/validation:** Scoped AFT diagnostics reported no diagnostics/TODOs with C# LSP unavailable. Focused Blazor AI service/state tests passed. Full serial Blazor Client tests passed: 1300 succeeded, 1 skipped. Blazor Client build passed with the existing `Microsoft.Extensions.ApiDescription.Client` deprecation warning. Architecture tests passed after extracting `AiAssistantCommandResult` out of the pure `I*` interface file.
- **Remaining:** Phase 6.3/6.4/6.5 should build the visible assistant rail, reference picker, and proposed-action/result cards over this service/state layer, with bUnit coverage proving HAL-gated affordances.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 6.3

- **Current state:** The shell AI assistant rail is no longer a placeholder. It now renders a service-backed assistant layout while preserving the existing `AiAssistantState` availability/open-close contract and docked/fixed rail behavior.
- **Files changed:** `AiAssistantRail.razor`, `AiAssistantRail.razor.css`, and `AiAssistantRailTests.cs`; workstream context/tasks updated.
- **Control flow:** `AiAssistantRail` injects `IAiAssistantClientService` and `AiAssistantConversationState`, loads conversations when the available rail opens or is hosted in the dock, selects conversation detail through the service, sends prompts with a generated idempotency key, searches event references through the generated-client wrapper, and calls confirm/reject service methods for proposed actions.
- **HAL behavior:** Inline proposal buttons render only when `AiAssistantConversationState.CanConfirm/CanReject` detects `confirm-action`/`reject-action` links. Reference results preserve HAL `event` links and selected references are stored in `AiAssistantConversationState`; no local role/claim checks were added.
- **UI behavior:** The existing `<aside data-testid="shell-ai-rail">`, backdrop, close button, `HostedInDock` mode, mobile overlay behavior, and CSS isolation remain intact. The body now covers loading, empty conversation, conversation list, messages, composer, reference search/selection, error, and inline proposed-action states.
- **Tests/validation:** Scoped AFT diagnostics reported no diagnostics/TODOs with C#/Razor LSP unavailable. Focused `AiAssistantRailTests` passed. Blazor Client build passed with the existing `Microsoft.Extensions.ApiDescription.Client` deprecation warning. Architecture tests passed. A broader combined AI test rerun hit an unrelated `DockSideHost` async teardown abort, so the focused rail test is the authoritative new-component check for this slice; the service/state tests were already green in Phase 6.1/6.2.
- **Remaining:** Phase 6.4/6.5 should extract the inline reference/proposal UI into dedicated picker/chip/action/result components, add debounce and keyboard-removal behavior, preview `CreateEventDraft` safely, render result links from API/HAL data, and expand bUnit coverage.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 6.4/6.5

- **Current state:** The rail no longer owns large inline reference-picker or proposed-action card markup. Dedicated Blazor components now own reference search/chips, safe `CreateEventDraft` preview, action result state, and HAL-gated proposal actions.
- **Files changed:** Added `AiReferencePicker.razor`, `AiReferenceChip.razor`, `AiProposedActionCard.razor`, `CreateEventDraftActionPreview.razor`, `AiActionResultCard.razor`, `AiReferencePickerTests.cs`, and `AiProposedActionCardTests.cs`; updated `AiAssistantRail.razor`, `AiAssistantRailTests.cs`, `MainLayoutTests.cs`, and workstream context/tasks.
- **Control flow:** `AiAssistantRail` passes reference search state and callbacks into `AiReferencePicker`, then passes each proposed action plus busy state and confirm/reject callbacks into `AiProposedActionCard`. The service/state layer remains the only API-facing layer.
- **HAL behavior:** Reference event availability is shown only when the reference HAL resource has an `event` link. Proposed-action Confirm/Reject buttons render only when `AiAssistantConversationState.CanConfirm/CanReject` sees `confirm-action`/`reject-action` links; no local role or claim checks were added.
- **UI behavior:** `AiReferencePicker` debounces search input, supports loading/empty states, selects/removes references, and delegates removable chips to `AiReferenceChip`. `AiReferenceChip` supports click, Delete, and Backspace removal. `CreateEventDraftActionPreview` shows only safe title/description fields from the payload. `AiActionResultCard` separates created-result/failure metadata from the proposal preview. `AiProposedActionCard` honors busy state to prevent duplicate local submits.
- **Tests/validation:** Scoped AFT diagnostics reported no diagnostics/TODOs with C#/Razor LSP unavailable. Focused `AiReferencePickerTests`, `AiProposedActionCardTests`, and `AiAssistantRailTests` passed. Blazor Client build passed with the existing `Microsoft.Extensions.ApiDescription.Client` deprecation warning. Architecture tests passed. A narrow `MainLayoutTests` command hit the same unrelated `DockSideHost` async teardown abort seen earlier, so focused component tests plus build/architecture are the authoritative checks for this slice.
- **Remaining:** Phase 6.6 should add full assistant panel bUnit coverage across bootstrap/history, conversation selection, references, send, proposed actions, disabled/error states, and dock integration. Phase 8 retention/redaction posture is still required before broad AI-history enablement.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 6.6

- **Current state:** Phase 6 Blazor product assistant UX foundation is covered by focused full-panel bUnit tests over the composed rail and dedicated picker/action components.
- **Files changed:** `AiAssistantRailTests.cs` and active workstream context/tasks. No production component behavior changed in this slice.
- **Coverage added:** Rail tests now cover unavailable/disabled rendering without service calls, conversation history/message loading, new conversation creation and detail selection, send-message idempotency key generation plus conversation reload, reference search/select/remove, HAL-gated proposal Confirm/Reject behavior, and safe command error display.
- **HAL behavior:** Confirm/Reject coverage still proves buttons are rendered only for actions with HAL `confirm-action`/`reject-action` links. Reference coverage still preserves the HAL `event` link path through picker/chip selection.
- **Tests/validation:** Scoped AFT diagnostics reported no diagnostics/TODOs with C#/Razor LSP unavailable. Focused `AiAssistantRailTests` passed with 7/7 tests. Blazor Client build passed with the existing `Microsoft.Extensions.ApiDescription.Client` deprecation warning. Architecture tests passed. A broader layout/dock command still hits the unrelated `DockSideHost` async teardown abort seen in earlier slices, so focused assistant panel tests plus build/architecture are the authoritative checks for this slice.
- **Remaining:** Phase 8 retention/redaction remains required before broad AI history enablement. Phase 7 MCP adapter research remains optional and intentionally unresolved.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 0.2 Archive Pointer

- **Current state:** The old AI integration workstream now has an explicit archive pointer. `dev/active/ai-integration` was already absent, so the slice created a pointer instead of moving files.
- **Files changed:** Added `dev/pause/ai-integration/README.md`; updated active workstream context/tasks.
- **Mapping preserved:** Old reference search, confirm/create-draft, Blazor panel, cancellation/streaming/retention/dashboards, final docs, and validation work are mapped to Phases 5, 2-4, 6, 8, and 8.5 in this registry workstream.
- **Validation:** Docs-only archive pointer; scoped diagnostics/docs checks are sufficient. Future agents should not resume `dev/active/ai-integration` directly.
- **Remaining:** Phase 7 MCP adapter research is optional. Phase 8 retention/redaction remains the recommended next implementation before broad AI history enablement.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 8.1

- **Current state:** Tenant-scoped AI retention cleanup/redaction now exists as an Application command and Persistence repository operation. It is callable and testable, but not yet scheduled as an operator job.
- **Files changed:** Added `AiRetentionCleanupResult.cs`, `RunAiRetentionCleanupCommand.cs`, `RunAiRetentionCleanupCommandHandler.cs`, and `RunAiRetentionCleanupCommandHandlerTests.cs`; updated `IAiConversationRepository.cs`, `AiConversationRepository.cs`, `AiConversationRepositoryTests.cs`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, and active workstream context/tasks.
- **Control flow:** `RunAiRetentionCleanupCommandHandler` resolves `AiAssistantSettingGroup.RetentionDays` for the current tenant, clamps invalid values to one day, computes a cutoff from `UtcNow`, and delegates to `IAiConversationRepository.RedactExpiredConversationsAsync(...)`. The command supports dry-run mode for operator preview.
- **Repository behavior:** `AiConversationRepository.RedactExpiredConversationsAsync(...)` selects expired conversations through normal EF tenant and soft-delete filters, counts affected child rows for observability, and in destructive mode redacts message content, proposed-action payload JSON, reference display/summary data, run failure messages, and tool execution failure messages before soft-deleting the conversation shell. It does not call `IgnoreQueryFilters()` in the runtime cleanup path.
- **Safety notes:** Cleanup results report counts and cutoff metadata only; they do not include prompt text, provider output, raw proposed-action payloads, reference summaries, model IDs, API keys, or endpoint URLs. Dry-run returns eligible conversation counts without mutating rows. Scheduling, runbook, and metrics integration remain Phase 8 follow-up.
- **Tests/validation:** Scoped AFT diagnostics reported no diagnostics/TODOs with C# LSP unavailable. Application build passed. Targeted Application retention handler tests passed 2/2. Targeted PostgreSQL-backed retention repository tests passed 2/2, covering dry-run non-mutation, current-tenant-only cleanup, redaction of sensitive AI content fields, and preservation of another tenant's expired rows.
- **Remaining:** Phase 8.2 cancellation semantics, Phase 8.3 streaming/polling decision, Phase 8.4 scheduler/runbook/metrics polish, and Phase 8.5 final docs/validation refresh remain open. Phase 7 MCP adapter research remains optional.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 8.2

- **Current state:** Persisted cancellation semantics are implemented for AI runs. Queued and in-progress runs can be cancelled through an authenticated API endpoint; terminal succeeded/failed runs return safe conflict ProblemDetails; already-cancelled runs are idempotent.
- **Files changed:** Added `CancelAiRunCommand`, `CancelAiRunCommandHandler`, `AiConversation.CancelRun`, cancel authorization constants, machine-scope write classification, Cerbos `cancel_run` policy parity, API route name, controller cancel endpoint, run-status `cancel-run` HAL link, API ProblemDetails mapping, API/controller/Application tests, generated OpenAPI/client/inventory artifacts, and API/operations changelog docs.
- **Control flow:** `GET /api/ai/assistant/conversations/{conversationId}/runs/{runId}` emits `cancel-run` only for `Queued` or `InProgress` runs. `POST /api/ai/assistant/conversations/{conversationId}/runs/{runId}/cancel` dispatches `CancelAiRunCommand`; the handler loads the conversation for update, enforces authenticated owner access fail-closed, cancels the domain run, activates the conversation shell, and persists the aggregate.
- **Safety notes:** Cancelled runs do not create proposed actions. Existing send-message provider calls already receive the request `CancellationToken`; this slice does not add cross-request provider abort orchestration for provider HTTP calls already in progress.
- **Validation:** API build passed; focused Application cancellation/authorization/machine-scope tests passed; API controller tests passed; OpenAPI invariant tests passed; API contract inventory generation passed; Blazor Client build passed. Architecture verification was attempted and failed only on unrelated `DeleteAccountDialog.razor.cs` direct `DialogOptions` construction outside this workstream slice.
- **Remaining:** Phase 8.3 streaming/polling decision, Phase 8.4 scheduler/runbook/metrics polish for retention and AI operations, and Phase 8.5 final docs/validation refresh remain open. Phase 7 MCP adapter research remains optional.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 8.3

- **Current state:** The streaming/polling transport decision is complete. AI run progress remains on authenticated polling through `GET /api/ai/assistant/conversations/{conversationId}/runs/{runId}`. Streaming is intentionally deferred and `ai_assistant.streaming_enabled` remains disabled/reserved.
- **Files changed:** `docs/API.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/API_CHANGELOG.md`, plus active workstream context/tasks.
- **Decision:** Keep polling as the supported AI run progress contract. `SendAiMessage` already returns `202 Accepted` with the run-status route, `GetRunStatus` returns safe HAL metadata, and Phase 8.2 added `cancel-run` only while runs are queued or in progress.
- **Safety notes:** No SSE, SignalR, or streaming provider transport was added. A future streaming implementation must separately cover proxy buffering, BFF/auth behavior, request timeout policy, provider cancellation, safe logging, and a non-streaming fallback before `ai_assistant.streaming_enabled` can become operational.
- **Validation:** Docs-only contract decision. Existing Phase 8.2 API/OpenAPI/controller tests already cover the polling route, run-status HAL links, and cancellation affordance. Scoped docs diagnostics and diff checks are sufficient for this slice.
- **Remaining:** Phase 8.4 scheduler/runbook/metrics polish and Phase 8.5 final validation refresh remain before starting Phase 7 MCP work.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 8.4

- **Current state:** AI retention cleanup is now scheduled and observable. The Phase 8.1 tenant-scoped redaction primitive is wrapped by a hosted API worker, readiness check, static scheduler settings, and low-cardinality metrics.
- **Files changed:** Added `AiRetentionCleanupSettings`, `AiRetentionCleanupSettingsValidator`, `IAiRetentionCleanupService`, `AiRetentionCleanupRunResult`, `AiRetentionCleanupService`, `AiRetentionCleanupProcessor`, `AiRetentionCleanupHealthCheck`, infrastructure/API focused tests, and docs updates; updated `BusinessMetrics`, `InfrastructureServicesRegistration`, `Program.cs`, `docs/CONFIGURATION.md`, and `docs/OPERATIONS.md`.
- **Control flow:** `AiRetentionCleanupProcessor` runs only when enabled, waits its configured initial delay, and periodically invokes `IAiRetentionCleanupService.CleanupAllTenantsAsync`. The service reads active tenant lookups, creates a fresh scope per tenant, sets `ITenantContextAccessor`, resolves tenant `AiAssistantSettingGroup.RetentionDays`, and invokes the tenant-filtered repository cleanup path. It clears tenant context after each tenant and continues after bounded per-tenant failures.
- **Observability:** The `ai-retention-cleanup` readiness check reports only enabled/dry-run/interval/pass-bound configuration. `BusinessMetrics` emits `explore.ai.retention.cleanup_runs` and `explore.ai.retention.cleanup_rows` with bounded `mode`, `outcome`, and `category` tags.
- **Safety notes:** The worker does not disable tenant filters and does not log tenant IDs, prompt content, provider responses, selected reference content, proposed-action payloads, API keys, model IDs, endpoint URLs, or raw provider exception bodies. Dry-run remains available through `AiRetentionCleanup:DryRun=true`.
- **Tests/validation:** API build passed. Focused Infrastructure tests passed for settings validation and tenant-iterating cleanup behavior, including partial-failure aggregation. Focused API health-check tests passed for enabled, dry-run, and disabled states with safe health data. Scoped docs/diagnostics and diff checks are expected for final Phase 8.4 closure.
- **Remaining:** Phase 8.5 final docs/validation refresh remains before Phase 7 MCP adapter implementation can begin.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 8.5

- **Current state:** Phase 8 is complete. Final user-facing and operator documentation now reflects the implemented AI assistant API, retention cleanup, polling/cancellation, Blazor rail, accessibility, and self-hosting posture.
- **Files changed:** `docs/SELF_HOSTING.md`, `docs/BLAZOR.md`, `docs/DOCK_LAYOUT.md`, `docs/ACCESSIBILITY.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, and active workstream context/tasks.
- **Docs covered:** Provider setup and secrets, model/provider safety, tenant retention setting plus scheduled cleanup, `ai-retention-cleanup` health/metrics, authenticated run polling, queued/in-progress run cancellation, HAL-gated proposed actions/references, dock behavior, keyboard/focus expectations, and Plane inspiration credit via the existing README credit.
- **Validation:** Scoped docs diagnostics reported no TODOs or diagnostics. Diff checks passed for the updated docs. Runtime Phase 8.4 checks already passed for API build, Infrastructure cleanup tests, and API health-check tests.
- **Remaining:** Phase 7 MCP adapter work is now unblocked. Start with Phase 7.1 architecture decision before implementing any MCP host/transport.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 7.2

- **Current state:** The API-hosted MCP adapter is implemented over the registry/Application boundary and remains disabled by default. It exposes read-only registry discovery, safe conversation resources, a confirmation-first prompt, and a proposal-first mutating tool path.
- **Files changed:** `Directory.Packages.props`, `Explore.API/Explore.API.csproj`, `ApplicationServicesRegistration.cs`, `Program.cs`, new MCP configuration/validator, `McpAdapterHealthCheck.cs`, `AiToolRegistryMcpTools.cs`, `AiAssistantMcpTools.cs`, `AiAssistantMcpResources.cs`, `AiAssistantMcpPrompts.cs`, `AiToolRegistryMcpJsonContext.cs`, `ProposeAiToolActionCommand*`, authorization/machine-scope/Cerbos parity files, and focused Application/API tests.
- **Control flow:** When `Mcp:Enabled=true`, `Explore.API` registers the official `ModelContextProtocol.AspNetCore` server with HTTP transport and maps the configured endpoint behind authorization. When disabled, no MCP endpoint is mapped and the health check reports a degraded intentional-disable posture.
- **Registry behavior:** `list_ai_tool_contracts` reads `IAiToolContractRegistry`, filters definitions exposed to MCP, and returns safe contract metadata: kind, name, display name, confirmation mode, required authorization, allowed/forbidden fields, and JSON schema. `propose_ai_tool_action` delegates to `ProposeAiToolActionCommand`, which validates payloads through the registry and persists only a proposed action for normal product/API confirmation.
- **Resources/prompts:** MCP conversation resources delegate through MediatR queries and omit raw message content plus proposed-action payload JSON. The `create_event_draft_with_confirmation` prompt instructs external agents to use registry contracts, propose actions, and wait for confirmation rather than claiming committed mutations.
- **Safety notes:** `Mcp:Stateless` must remain true and `Mcp:EnableLegacySse` is rejected by startup validation. MCP remains disabled by default, mapped behind API authorization when enabled, and must not emit prompts, tool payloads, tenant IDs, provider endpoints, API keys, model secrets, or raw provider exceptions.
- **Validation:** API build passed. Focused Application proposal/auth/machine-scope tests passed. Focused MCP API tests for health/config/registry/proposal/resources/prompts are implemented but currently blocked by an unrelated API integration test project compile error in `EmailDispatchAdminControllerTests`.
- **Remaining:** Complete Phase 7.3 self-hosting/operations documentation for the implemented adapter.

### Implementation Slice — 2026-06-03 Europe/Brussels — Phase 7.3

- **Current state:** The Phase 7 MCP adapter work is documented for self-hosters and operators. The overall AI Tool Contract Registry implementation plan is complete.
- **Files changed:** `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, and active workstream context/tasks.
- **Docs covered:** `Mcp:*` settings, disabled-by-default posture, authenticated API-hosted stateless Streamable HTTP endpoint, `mcp-adapter` readiness check, disable/recovery path, proposal-first mutation behavior, and support-data restrictions.
- **Safety notes:** MCP remains optional. Operators can disable it with `Mcp:Enabled=false` and an API restart. Support tickets/logs must not include prompts, tool payloads, provider responses, tenant IDs, endpoint URLs, API keys, model secrets, or raw MCP request/response bodies.
- **Validation:** Scoped diagnostics and diff checks passed for the Phase 7.3 docs. Runtime Phase 7.2 API build and Application proposal tests passed; focused MCP API tests remain blocked by an unrelated API integration test project compile error.
- **Remaining:** No planned AI Tool Contract Registry implementation phase remains. Resolve unrelated repository verification blockers before merge/release if a fully green gate is required.

### Handoff — 2026-06-06 Europe/Brussels

#### Current State

- What is completed: ATCR Phases 0-8 remain complete. Phase 9.1 is implemented with an Infrastructure-only `Microsoft.Extensions.AI.IChatClient` adapter behind the existing `IAiChatProvider` boundary. Phase 9.2 is implemented with explicit `openai-sdk` and `azure-openai` provider modes, conditional SDK client registration, and Azure OpenAI `api-key`/`default-azure-credential` configuration posture. Phase 9.3 is implemented with redacted provider metrics/spans and BusinessMetrics redaction tests. Phase 9.4 is implemented and refactored with strict Application schema validation, safe self-correction retry, SDK/raw schema parity, and stable content-filter mapping. Phase 9.5 is implemented with token-budgeted provider messages, selected references, and registry-backed tool schemas.
- What is in progress: Phase 9 remains active only for future slices. Phase 9.6 structured output or Phase 9.7 advisory evaluation reports are the next recommended provider-hardening tasks.
- What changed since the last handoff: Prompt budgeting now uses an Application-owned token estimator seam and shared prompt-token budget. `AiPromptContextBuilder` spends `MaxInputTokens` across the system prompt, optional action schema, and newest-first provider messages; `AiSystemPromptFactory` omits over-budget action schemas; `AiReferencePromptPacker` adds optional selected-reference token caps while preserving existing count/character caps.

#### Next Action

1. Continue to Phase 9.6 structured output or Phase 9.7 advisory evaluation reports if the user wants provider hardening to continue.
2. Keep SDK-backed provider defaulting deferred until operator rollout evidence is settled.
3. Keep Phase 9.7 evaluation reports advisory until volatility, cost, caching, and false-positive behavior are understood.

#### Blockers

- None for the Phase 9.1/9.2/9.3/9.4/9.5 provider-hardening slices.
- Full Release build passes with 0 errors. Broader API integration and Blazor test suites were not rerun for Phase 9.5.

#### Modified Files

- `Directory.Packages.props` — added centrally managed `Microsoft.Extensions.AI.Abstractions`, `Microsoft.Extensions.AI.OpenAI`, `Azure.AI.OpenAI`, and `Azure.Identity` package versions.
- `Explore.Infrastructure/Explore.Infrastructure.csproj` — added Infrastructure-only AI SDK package references.
- `Explore.Infrastructure/Ai/AiProviderSettings.cs` and `Explore.Infrastructure/Ai/AiProviderSettingsValidator.cs` — added `openai-sdk`, `azure-openai`, Azure credential mode, and fail-closed provider validation.
- `Explore.Infrastructure/Ai/RuntimeAiChatProvider.cs` — added SDK-backed provider delegation while failing closed if the SDK adapter is not registered.
- `Explore.Infrastructure/Ai/AiProviderHealthReporter.cs` — reports valid SDK-backed provider configuration without leaking endpoints, keys, or model IDs.
- `Explore.Infrastructure/InfrastructureServicesRegistration.cs` — conditionally registers concrete SDK-backed `IChatClient` instances only for explicit SDK provider modes.
- `Explore.Infrastructure/Ai/MicrosoftExtensionsAiChatProvider.cs` — added the SDK adapter behind the provider-neutral Application contract.
- `Explore.Infrastructure/Ai/OpenAiCompatibleChatProvider.cs` — added stable content-filter finish-reason mapping for the raw fallback provider.
- `Explore.Infrastructure/Ai/AiProviderTelemetry.cs` — added redacted provider activity spans with bounded tags only.
- `Explore.Application/Telemetry/BusinessMetrics.cs` — added bounded AI provider duration, token-usage, and proposed-action metrics.
- `Explore.ServiceDefaults/Extensions.cs` — added the `Explore.Ai.Provider` activity source to exported traces.
- `Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` — added the test package reference for SDK adapter fakes.
- `Explore.Infrastructure.Tests/Infrastructure/AiProviderSettingsValidatorTests.cs`, `AiProviderHealthReporterTests.cs`, and `RuntimeAiChatProviderTests.cs` — added SDK provider validation, health, and runtime selection coverage.
- `Explore.Infrastructure.Tests/Infrastructure/MicrosoftExtensionsAiChatProviderTests.cs` — added adapter mapping, tool-call, content-filter, malformed-argument, and registry-backed SDK schema parity tests.
- `Explore.Infrastructure.Tests/Infrastructure/OpenAiCompatibleChatProviderTests.cs` — added raw provider content-filter mapping and strict function-tool schema coverage.
- `Explore.Application/Features/AiAssistant/Tools/AiToolPayloadGuard.cs`, `AiToolJsonSchemaPayloadValidator.cs`, `AiToolCorrectionMessages.cs`, and `AiToolValidationResult.cs` — keep field-policy validation, schema-subset validation, and bounded correction wording separated.
- `Explore.Application/Features/AiAssistant/Prompting/IAiTokenEstimator.cs`, `ApproximateAiTokenEstimator.cs`, and `AiPromptTokenBudget.cs` — add the provider-neutral tokenizer/estimator seam, deterministic fallback counting, and shared prompt-token budget tracking.
- `Explore.Application/Features/AiAssistant/Prompting/AiPromptContextBuilder.cs` — spends the configured input-token budget across system prompt, action schema, and newest-first provider messages, with bounded newest-message truncation when needed.
- `Explore.Application/Features/AiAssistant/Prompting/AiSystemPromptFactory.cs` — omits registry-backed tool schema when the remaining token budget cannot fit it.
- `Explore.Application/Features/AiAssistant/Prompting/AiReferencePromptPacker.cs` — keeps existing count/character caps and adds optional per-reference/total token caps for selected references.
- `Explore.Application/Features/AiAssistant/Prompting/AiStructuredActionParser.cs` — forwards safe correction messages from registry validation failures.
- `Explore.Application/Features/AiAssistant/Prompting/AiProviderResponseResolver.cs` and `Explore.Application/Features/AiAssistant/Handlers/Commands/SendAiMessageCommandHandler.cs` — isolate provider send/parse/retry behavior from command persistence while preserving the one bounded self-correction retry before fail-closed behavior.
- `Event.Application.UnitTests/Features/AiAssistant/Tools/AiToolPayloadGuardTests.cs`, `AiStructuredActionParserTests.cs`, `CreateEventDraftAiToolDefinitionTests.cs`, and `SendAiMessageCommandHandlerTests.cs` — added strict-schema and self-correction coverage.
- `Event.Application.UnitTests/Telemetry/BusinessMetricsAiProviderTests.cs` — added bounded/redacted metric tests for request counters, durations, token usage, and proposed-action metrics.
- `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, and `docs/SELF_HOSTING.md` — documented SDK-backed provider modes, Azure OpenAI credential posture, and redacted provider telemetry behavior.
- `Event.Application.UnitTests/Features/AiAssistant/Prompting/AiPromptContextBuilderTests.cs` and `AiReferencePromptPackerTests.cs` — add tokenizer-backed estimator fakes and budget coverage for message selection, action-schema omission, and selected-reference token caps.
- `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-context.md`, `ai-tool-contract-registry-tasks.md`, and `ai-tool-contract-registry-plan.md` — recorded Phase 9.1-9.5 completion and next Phase 9.6/9.7 direction.

#### Validation

- Commands run: `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` — passed, 1255 tests.
- Commands run: `dotnet test Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` — passed, 427 tests.
- Commands run: `dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed, 190 total / 189 succeeded / 1 skipped.
- Commands run for Phase 9.5: `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` — passed, 1259 tests.
- Commands run for Phase 9.5: `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — passed, 190 total / 189 succeeded / 1 skipped.
- Commands run for Phase 9.5: `dotnet build --configuration Release --verbosity quiet` — passed, 25 projects, 0 errors, existing warnings.
- Commands still needed: broader full-repo gates if the unrelated existing API/Blazor blockers are fixed.

#### Documentation Impact

- Updated active dev docs through `/dev-docs-update` after Phase 9.5. Product/operator configuration docs were updated earlier because SDK-backed provider modes are selectable through static `AiProvider:*` configuration.

#### Risks

- Source-grounding risks: Phase 9 must keep `Microsoft.Extensions.AI` SDK types out of Application contracts.
- Test or build risks: advisory evaluation reports can be flaky/costly if promoted to hard gates too early.
- Operator/release risks: telemetry and future RAG must preserve no-content/no-tenant-identifying support-data boundaries.

#### Notes For Next Contributor Or Agent

- Required docs/rules to read: `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/OPERATIONS.md`, this plan/context/tasks set, and relevant Infrastructure/Application rules for Phase 9.
- Assumptions made: the official .NET AI report recommendations are planning inputs, not permission to remove self-hosted raw OpenAI-compatible support.
- Do not touch unrelated dirty files while implementing Phase 9 unless the user explicitly asks.
