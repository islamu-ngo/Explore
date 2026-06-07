<!-- ABOUTME: Operational context for the AI Tool Contract Registry workstream. -->
<!-- ABOUTME: Captures current evidence, decisions, risks, and next actions for registry and MCP adapter planning. -->

# AI Tool Contract Registry — Context

Last Updated: 2026-06-07 Europe/Brussels

## SESSION PROGRESS (2026-06-07 Europe/Brussels)

### ✅ COMPLETED

- Restored the active ATCR dev docs from the tracked baseline after the current worktree showed them deleted, then re-baselined them to the actual Phases 9/10 implementation state.
- Read and applied the requested `.claude/skills/technology-selection/SKILL.md` and `.claude/skills/mcp-csharp-create/SKILL.md` instructions.
- Used Context7 for official `ModelContextProtocol` C# SDK docs. Confirmed current official patterns: `ModelContextProtocol.AspNetCore`, `AddMcpServer()`, `WithHttpTransport(options => options.Stateless = true)`, `MapMcp()`, explicit tool/resource/prompt type registration, method/parameter `[Description]`, cancellation tokens, optional `.AddAuthorizationFilters()` for method-level authorization, and SDK annotation hints.
- Added Phase 11 to the plan/tasks as "Official .NET MCP SDK Alignment And Enterprise Hardening" with tasks for SDK contract conformance, auth-filter reconciliation, first-class registry-to-MCP projection, transport/AOT posture, MCP Inspector/redacted runbooks, and protocol evolution review.
- Implemented the first Phase 11.1 code slice: `Explore.API/Program.cs` now passes the configured stateless posture into `WithHttpTransport(...)`; `AiAssistantMcpTools` and `AiAssistantMcpResources` now describe schema-visible MCP parameters; `McpSdkContractTests` enforces official SDK type/description hygiene.
- Implemented Phase 11.2 authorization-filter reconciliation: `AddAuthorizationFilters()` is registered, all MCP tool/resource/prompt methods carry `[Authorize]`, endpoint mapping remains `RequireAuthorization()`, and focused tests prove anonymous MCP requests fail closed while authenticated requests reach the MCP protocol boundary.
- Implemented Phase 11.3 first-class registry-to-MCP projection: `ExposeToMcp` ATCR definitions now project `propose_*` MCP tools with registry payload fields plus a minimal proposal envelope, SDK annotations/meta hints, and MediatR `ProposeAiToolActionCommand` dispatch.
- Implemented Phase 11.4 transport/AOT posture hardening: startup/source tests now lock the product MCP host to API-hosted stateless Streamable HTTP, reject product stdio/legacy-SSE wiring, and keep explicit SDK registration/AOT tradeoffs documented without promising Native AOT support.
- Implemented Phase 11.5 MCP Inspector/redacted runbook hardening: deterministic replay now includes an Inspector discovery checklist scenario, and docs define authenticated/redacted manual Inspector smoke steps for tools/resources/prompts plus proposal-only calls.
- Implemented Phase 11.6 client compatibility/protocol evolution review: ADR/architecture/configuration/operations/self-hosting docs now require a new ADR/task before stateful sessions, server-to-client features, resource subscriptions, progress/list-changed notifications, client shims, or protocol-version changes alter the current stateless proposal-first surface.
- Read the requested `mcp-csharp-debug` and `mcp-csharp-test` skills plus their reference files, used Context7 MCP SDK/Inspector evidence, and added Phase 12 for MCP debug/test hardening without changing runtime behavior.
- Preserved the existing Clean Architecture decision: MCP remains an API presentation adapter over ATCR/MediatR, disabled by default, authenticated, stateless Streamable HTTP, proposal-first for mutations, and free of `ModelContextProtocol` references in Domain/Application.
- Implemented the Phase 13 runtime-governance slice: `mcp.enabled`/`mcp.enable_legacy_sse` are hierarchical settings with instance/tenant lock controls, `/mcp` has a runtime gate after tenant/auth resolution, health reports safe startup/runtime booleans, and instance/tenant admin UI can govern runtime MCP posture without changing endpoint path/stateless startup settings.
- Implemented the Phase 13 scope-hardening slice: MCP now uses API-key scope-aware read/propose policies, revoked keys fall back only to anonymous-safe discovery, tenant mismatches fail closed, unknown MCP-like scopes are rejected on create/update, and read-only MCP keys cannot discover or call proposal tools.

### 🟢 COMPLETE

- Phase 12 is implemented. It added redacted MCP debugging docs/templates, JSON-RPC/WebApplicationFactory contract tests, protocol redaction tests, bounded `Explore.Mcp` telemetry, projected-tool binding/cancellation tests, deterministic replay/evaluation scenarios, compatibility evidence, a review-first doctor check, and ADR-011 stdio deferral.
- Phase 13.1, 13.2, the safe-unavailable 13.7 legacy-SSE posture, and the Phase 13 API-key scope/read-propose hardening slice are implemented. Future SDK upgrades or compatibility requests must still repeat the Phase 11.6/12.8 review before behavior changes.

### ⏭️ NEXT

1. Finish remaining Phase 13 MCP rate-limit/audit assertions and decide whether Phase 13.5 adds anonymous-safe MCP resources beyond registry discovery.
2. Keep architecture/context validation green if future skill or agent files change.
3. Do not enable legacy SSE, stateful MCP sessions, stdio product hosting, Native AOT support claims, sampling, elicitation, roots, progress/list-changed notifications, resource subscriptions, direct MCP mutation, raw protocol artifact retention, live-client CI, or remote MCP tool import without a new ADR/user approval and targeted verification.

### ⚠️ BLOCKERS

- No blocker for the Phase 13 runtime-governance slice. Tavily MCP is unavailable in this tool context; use Context7 plus repository/docs until the connector is installed.

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
5. Phases 9 and 10 are implemented in the current worktree; do not reopen them unless verification evidence contradicts completion.
6. Phase 11 MCP hardening is complete for the current SDK posture; future MCP compatibility work starts with a new ADR/task review.
7. Keep all future MCP tools registry-backed and proposal/confirmation-first for mutations.
8. Keep all three dev docs updated after every meaningful implementation slice.

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

## Official C# MCP SDK Alignment — 2026-06-07 Europe/Brussels

- **Skills used:** `technology-selection` classifies MCP as an agent/tooling workflow requiring guarded tool dispatch, DI, observability, explicit cost/safety controls, and no ad-hoc provider/business logic in tool methods. `mcp-csharp-create` requires the official C# SDK, .NET 10+, explicit transport selection, SDK attributes, descriptions, cancellation tokens, DI services, `MapMcp()` for HTTP, and AOT/reflection awareness.
- **Context7 evidence:** Official SDK docs show ASP.NET Core Streamable HTTP setup with `AddMcpServer()`, `WithHttpTransport(options => options.Stateless = true)`, explicit `.WithTools<T>()`, `.WithResources<T>()`, `.WithPrompts<T>()`, and `app.MapMcp()`. SDK docs also confirm `.AddAuthorizationFilters()` supports MCP method-level `[Authorize]` filtering on tools, prompts, and resources, and that read-only/destructive/idempotent/open-world annotations are hints, not trusted authorization.
- **Technology decision:** Keep product MCP hosted inside `Explore.API` through `ModelContextProtocol.AspNetCore` and Streamable HTTP. Keep stdio as a possible future local diagnostic/developer host only, not the product platform transport. Keep custom MCP protocol code rejected.
- **Implementation impact in this slice:** `Program.cs` now passes `Mcp:Stateless` into `WithHttpTransport`, registers SDK authorization filters, maps the endpoint from effective options only when `Mcp:Enabled=true`, and keeps endpoint `RequireAuthorization()` as the first boundary. MCP tool/resource schema-visible parameters have `[Description]`, all MCP callable methods carry `[Authorize]`, and registry definitions exposed to MCP project first-class `propose_*` tools that still persist proposed actions only. Phase 11.4 locks transport/source posture with startup tests and docs: product MCP is API-hosted stateless Streamable HTTP, stdio is local/deferred, legacy SSE/stateful sessions are rejected, and Native AOT is unpromised until publish verification exists. Phase 11.5 adds deterministic replay coverage for the MCP Inspector discovery checklist and redacted manual smoke runbooks. Phase 11.6 gates future stateful/server-to-client/client-shim protocol changes behind ADR/task review.
- **Future Phase 11 impact:** Treat Phase 11 as complete for the current SDK posture. Reopen protocol review only for SDK upgrades, client compatibility pressure, or new MCP capabilities.

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
| `Explore.API/Mcp/AiMcpProjectedToolFactory.cs` | New | API/MCP | First-class MCP proposal tool projection from ATCR definitions. | Projects `ExposeToMcp` definitions into `propose_*` SDK tools with registry payload schema fields, non-authoritative SDK hints, `[Authorize]` metadata, and MediatR proposal dispatch. |
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
18. Phase 11 uses official `ModelContextProtocol.AspNetCore` SDK guidance but does not let SDK annotations become authorization authority.
19. Product MCP transport stays API-hosted stateless Streamable HTTP; stdio is local/deferred and legacy SSE remains rejected unless a new ADR is approved.
20. First-class registry-to-MCP projection may improve client ergonomics, but mutating projected tools must still create proposed actions and wait for product/API confirmation.

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

Phase 11 MCP SDK slices should also run:

```bash
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --filter Mcp
dotnet build --configuration Release --verbosity quiet --no-restore
```

## Current Known Risks / Unknowns

- Registry can become too abstract. Keep Phase 1 minimal and prove it with `CreateEventDraft` only.
- MCP hosting and authentication strategy are decided and implemented through Phase 7; Phase 11 now focuses on official SDK conformance, auth-filter defense-in-depth, registry-to-MCP projection, and protocol evolution.
- Confirm/reject idempotency, event creation dispatch, execution audit row persistence, HAL proposed-action links, duplicate-confirm HTTP flow, and Phase 5 event reference tenant filtering are covered through tests.
- Phase 6 Blazor service/state/component behavior is covered by focused service/state/component/full-panel tests, plus build and architecture checks.
- Phase 8 retention cleanup is scheduled and observable; cross-request provider abort orchestration and streaming remain deferred.
- Phase 8.2 cancellation persists run cancellation state and exposes HAL/API affordances. It does not yet implement cross-request provider HTTP abort orchestration for provider calls already in progress; send-message still honors its request `CancellationToken`.
- Architecture verification for Phase 8.2 currently fails on unrelated `Explore.Blazor.Client/Pages/User/Components/Dialogs/DeleteAccountDialog.razor.cs` direct `DialogOptions` construction, outside the AI cancellation slice.
- Existing unrelated architecture parity failures may obscure registry validation; record separately.
- Old mixed AI migration history may need self-hoster notes or later migration cleanup.
- Old AI task mapping after audit: old Phase 4 reference search maps to new Phase 5; old Phase 5 confirm/create-draft maps to new Phases 2-4; old Phase 6 Blazor panel maps to new Phase 6; old Phase 7 cancellation/streaming/retention/dashboards maps to new Phase 8; old Phase 8 docs/credit/final validation maps to new Phase 8.5 and verification checklist.
- Phase 9 SDK-backed provider work is implemented but can still accidentally leak provider abstractions in future edits; detect by searching for `IChatClient` or provider packages outside Infrastructure/tests.
- Phase 9 telemetry/evaluation/RAG work is implemented as metadata-only/advisory/foundation, but future production expansion can accidentally emit content-bearing AI data; require explicit redaction tests before enabling new telemetry or ingestion.
- Phase 10 AgentBlazor-inspired work is implemented through ATCR contracts, but future edits can accidentally bypass ATCR by exposing services/components directly; reject reflection execution and require registry/HAL/MediatR tests.
- Generated agent inventories can drift from registry/API/HAL reality; generation must be deterministic and covered by docs/architecture checks.
- Schema-only context can leak private fields if the allow-list is too broad; start with selected DTO/reference projection summaries and tenant/public visibility tests.
- Multi-step plan previews can become direct execution if not constrained; keep them proposal-only until user confirmation dispatches existing commands.
- Usability/e2e artifacts can leak prompt/response/reference/tool content; use fake/replay providers in CI and redact artifacts.
- Phase 11 registry-to-MCP projection can accidentally make SDK-visible tools appear executable as committed mutations; projected mutating tools must still create proposed actions and require product confirmation.
- Phase 11 method-level MCP authorization filters can create a false sense of authority; endpoint auth, tenant middleware, MediatR authorization, and HAL confirmation remain authoritative.

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
- **Remaining at that time:** No planned AI Tool Contract Registry implementation phase remained before later Phase 9-11 re-baselines. Resolve unrelated repository verification blockers before merge/release if a fully green gate is required.

### Handoff — 2026-06-06 Europe/Brussels

> Superseded by the 2026-06-07 Phase 11 handoff below; retained for historical Phase 9.1-9.5 evidence.

#### Current State

- What is completed: ATCR Phases 0-8 remain complete. Phase 9.1 is implemented with an Infrastructure-only `Microsoft.Extensions.AI.IChatClient` adapter behind the existing `IAiChatProvider` boundary. Phase 9.2 is implemented with explicit `openai-sdk` and `azure-openai` provider modes, conditional SDK client registration, and Azure OpenAI `api-key`/`default-azure-credential` configuration posture. Phase 9.3 is implemented with redacted provider metrics/spans and BusinessMetrics redaction tests. Phase 9.4 is implemented and refactored with strict Application schema validation, safe self-correction retry, SDK/raw schema parity, and stable content-filter mapping. Phase 9.5 is implemented with token-budgeted provider messages, selected references, and registry-backed tool schemas.
- What was in progress at that handoff: Phase 9 remained active only for future slices. Phase 9.6 structured output or Phase 9.7 advisory evaluation reports were the next recommended provider-hardening tasks.
- What changed since the last handoff: Prompt budgeting now uses an Application-owned token estimator seam and shared prompt-token budget. `AiPromptContextBuilder` spends `MaxInputTokens` across the system prompt, optional action schema, and newest-first provider messages; `AiSystemPromptFactory` omits over-budget action schemas; `AiReferencePromptPacker` adds optional selected-reference token caps while preserving existing count/character caps.

#### Next Action

1. At that time, continue to Phase 9.6 structured output or Phase 9.7 advisory evaluation reports if the user wants provider hardening to continue.
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

### Implementation Slice — 2026-06-07 Europe/Brussels — Phase 11.1 Planning And SDK Contract Hygiene

- **Current state:** Phase 11 has been added as the official .NET/C# MCP SDK alignment roadmap. The existing Phase 7 MCP adapter remains API-hosted, disabled by default, authenticated, registry-backed, and proposal-first; Phase 11 tightens SDK conformance rather than changing execution authority.
- **Skills/docs used:** Read `technology-selection` and `mcp-csharp-create`; used Context7 for official `ModelContextProtocol` C# SDK docs covering `AddMcpServer`, `WithHttpTransport`, stateless Streamable HTTP, `MapMcp`, explicit type registration, descriptions, cancellation tokens, authorization filters, and SDK tool annotations.
- **Files changed:** `Explore.API/Program.cs`, `Explore.API/Mcp/AiAssistantMcpTools.cs`, `Explore.API/Mcp/AiAssistantMcpResources.cs`, `Event.API.IntegrationTests/Features/McpSdkContractTests.cs`, and the three active ATCR dev docs.
- **Control flow:** MCP server registration now passes `Mcp:Stateless` into `WithHttpTransport(...)` instead of relying on SDK defaults. Existing `MapMcp(mcpAdapterSettings.EndpointPath).RequireAuthorization()` remains the HTTP endpoint boundary.
- **Contract hygiene:** Schema-visible MCP parameters now carry `[Description]` attributes so LLM clients receive explicit parameter semantics. `McpSdkContractTests` reflect over MCP surface types to prove tool/resource/prompt type attributes and method/parameter descriptions remain in place.
- **Safety notes:** SDK annotations and descriptions are discovery hints only. Authorization still flows through endpoint auth, tenant middleware, MediatR authorization, ATCR validation, HAL confirmation, and CQRS command execution; MCP must not write repositories directly.
- **Validation:** Focused MCP/API integration tests passed: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*Mcp*/*" --no-progress --maximum-parallel-tests 1` completed 14/14. Release build passed: `dotnet build --configuration Release --verbosity quiet --no-restore` completed 25 projects, 0 errors, existing warnings.
- **Remaining:** Follow-up slices completed Phase 11.2 authorization-filter reconciliation and Phase 11.3 first-class projection; continue Phase 11.4 transport/AOT posture.

### Implementation Slice — 2026-06-07 Europe/Brussels — Phase 11.2 MCP Authorization Filters

- **Current state:** Phase 11.2 is implemented. MCP remains disabled by default and proposal-first for mutations, but the official SDK authorization filter layer is now active when the adapter is enabled.
- **Files changed:** `Explore.API/Program.cs`, `Explore.API/Mcp/AiToolRegistryMcpTools.cs`, `Explore.API/Mcp/AiAssistantMcpTools.cs`, `Explore.API/Mcp/AiAssistantMcpResources.cs`, `Explore.API/Mcp/AiAssistantMcpPrompts.cs`, `Event.API.IntegrationTests/Fixtures/AuthenticatedWebApplicationFactory.cs`, `Event.API.IntegrationTests/Features/McpSdkContractTests.cs`, `Event.API.IntegrationTests/Features/McpAuthorizationTests.cs`, MCP docs, and active workstream docs.
- **Control flow:** `Explore.API` now registers the MCP SDK server services outside the `Mcp:Enabled` endpoint gate so test/runtime options can deterministically enable endpoint mapping, while `app.MapMcp(...).RequireAuthorization()` still occurs only when effective `Mcp:Enabled=true`. This preserves the disabled-by-default product posture and makes endpoint auth testable.
- **Authorization posture:** `AddAuthorizationFilters()` is chained into `AddMcpServer()`, and every MCP callable tool/resource/prompt method now has `[Authorize]`. This is defense-in-depth for SDK list/call/read/get operations; it does not replace API authentication, tenant resolution, ATCR validation, MediatR authorization, HAL confirmation, or CQRS command execution.
- **Tests:** `McpSdkContractTests` now proves MCP callable methods require `[Authorize]` and do not opt into `[AllowAnonymous]`. `McpAuthorizationTests` enables the MCP endpoint in the test host, proves anonymous POSTs to `/mcp` return `401`, and proves authenticated principals reach the MCP protocol boundary rather than failing at endpoint authorization.
- **Validation:** Focused MCP/API integration tests passed: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*Mcp*/*" --no-progress --maximum-parallel-tests 1` completed 18/18. Release build passed: `dotnet build --configuration Release --verbosity quiet --no-restore` completed 25 projects, 0 errors, existing warnings.
- **Remaining:** Phase 11.3 projection follows in the next slice. Do not add direct MCP mutations or rely on SDK annotations as authorization.

### Implementation Slice — 2026-06-07 Europe/Brussels — Phase 11.3 Registry-To-MCP Projection

- **Current state:** Phase 11.3 is implemented. The generic `propose_ai_tool_action` tool remains available as a fallback, and each `ExposeToMcp` ATCR definition now projects a first-class `propose_*` MCP proposal tool.
- **Files changed:** `Explore.API/Mcp/AiMcpProjectedToolFactory.cs`, `Explore.API/Mcp/AiToolRegistryMcpTools.cs`, `Explore.API/Mcp/AiAssistantMcpPrompts.cs`, `Explore.API/Program.cs`, `Event.API.IntegrationTests/Features/McpProjectedToolTests.cs`, `Event.API.IntegrationTests/Features/McpAiToolRegistryTests.cs`, MCP docs, and active workstream docs.
- **Projection model:** `AiMcpProjectedToolFactory` creates SDK `McpServerTool` instances from `IAiToolContractRegistry`. The projected input schema preserves registry payload fields and adds only `conversationId` plus optional `summary`; hidden/forbidden fields such as `tenantId` remain absent and are rejected before MediatR dispatch.
- **Execution model:** `AiMcpProjectedProposalTool` maps MCP arguments to `ProposeAiToolActionCommand`, resolves `IMediator` from the MCP request scope, and returns the same safe command-result descriptor as the generic tool. It does not depend on repositories and does not execute domain mutations directly.
- **SDK metadata:** Projected tools carry `[Authorize]` metadata for SDK authorization filters, SDK tool annotations for read-only/destructive/idempotent/open-world hints, and bounded `Tool.Meta` values for registry name, risk, approval mode, and safe action instructions. These values are descriptive only and do not replace endpoint auth, tenant middleware, MediatR authorization, ATCR validation, or HAL confirmation.
- **Validation:** Focused MCP/API integration tests passed: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*Mcp*/*" --no-progress --maximum-parallel-tests 1` completed 25/25. Full Release build passed: `dotnet build --configuration Release --verbosity quiet --no-restore` completed 25 projects, 0 errors, existing warnings. API project Release build passed: `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet --no-restore` completed 7 projects, 0 errors, existing warnings. Architecture tests were attempted and failed on pre-existing untracked `.claude/skills/*` schema/line-count issues unrelated to MCP adapter code.
- **Remaining:** Continue Phase 11.4 transport/AOT posture. Do not promise AOT compatibility beyond explicit registration tests, and do not enable legacy SSE, stateful sessions, stdio product hosting, direct MCP mutation, or remote MCP tool import without a new ADR/user approval.

### Implementation Slice — 2026-06-07 Europe/Brussels — Phase 11.4 Transport/AOT Posture

- **Current state:** Phase 11.4 is implemented. The product MCP host remains `Explore.API` using the official `ModelContextProtocol.AspNetCore` Streamable HTTP transport, disabled by default and authenticated when enabled.
- **Context7/SDK evidence:** Official SDK docs recommend `WithHttpTransport(options => options.Stateless = true)` plus `MapMcp()` for ASP.NET Core Streamable HTTP. They also document legacy SSE as requiring stateful mode and warn about weaker request backpressure. Package XML documents `WithToolsFromAssembly()` as runtime reflection that may not work in Native AOT, recommending generic registration for Native AOT scenarios.
- **Files changed:** `Event.API.IntegrationTests/Features/McpSdkContractTests.cs`, `docs/adr/ADR-010-mcp-adapter-hosting-strategy.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, and the active workstream docs.
- **Control flow:** A new startup test enables MCP in the integration-test host and verifies the SDK `HttpServerTransportOptions` resolve as `Stateless=true` and `EnableLegacySse=false`. Source posture tests assert `Program.cs` uses `WithHttpTransport(...)`, maps `MapMcp(effectiveMcpAdapterSettings.EndpointPath).RequireAuthorization()`, avoids `WithStdioServerTransport()`/legacy-SSE wiring, and keeps explicit `WithTools<T>()`/`WithResources<T>()`/`WithPrompts<T>()` registration plus registry-projected tool options instead of assembly scanning.
- **Docs posture:** ADR/configuration/operations/self-hosting docs now state that product MCP is API-hosted stateless Streamable HTTP only; stdio is local/developer diagnostic and deferred; legacy SSE/stateful sessions are rejected unless a new ADR approves an isolated deployment; and Native AOT is not promised until a dedicated publish profile verifies the SDK registrations, projected tools, schema metadata, and auth metadata survive trimming/AOT.
- **Validation:** Focused MCP/API integration tests passed: `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*Mcp*/*" --no-progress --maximum-parallel-tests 1` completed 28/28. Full Release build passed: `dotnet build --configuration Release --verbosity quiet --no-restore` completed 25 projects, 0 errors, existing warnings. Architecture tests were attempted and still fail on pre-existing untracked `.claude/skills/*` schema/line-count issues unrelated to MCP adapter code.
- **Remaining:** Continue Phase 11.5 MCP Inspector/redacted runbook work. Do not enable legacy SSE, stateful sessions, stdio product hosting, Native AOT support, direct MCP mutation, or remote MCP tool import without a new ADR/user approval and targeted verification.

### Implementation Slice — 2026-06-07 Europe/Brussels — Phase 11.5 MCP Inspector/Redacted Runbook

- **Current state:** Phase 11.5 is implemented. The deterministic fake/replay report now includes an MCP Inspector discovery checklist scenario, and manual Inspector smoke guidance is documented as authenticated, proposal-only, and redacted.
- **Context7/skill evidence:** Local `mcp-csharp-test` guidance recommends unit/integration MCP checks and client-visible tool listing/invocation tests. Context7 MCP docs show `ListToolsAsync`/`CallToolAsync`, JSON-RPC `tools/list`, and Inspector startup through `npx -y @modelcontextprotocol/inspector`; MCP remote-server docs show custom HTTP headers and bearer tokens as client auth inputs.
- **Files changed:** `Explore.Diagnostic/AiReplay/AiReplayScenarioCodes.cs`, `Explore.Diagnostic/AiReplay/AiReplayReportGenerator.cs`, `Explore.Diagnostic.UnitTests/AiReplay/AiReplayReportGeneratorTests.cs`, `docs/OPERATIONS.md`, `docs/AI_AGENT_EXPERIENCE_HARDENING.md`, and active workstream docs.
- **Replay behavior:** Added `ai.replay.mcp.inspector-contract`, which records the expected manual discovery scope (`tools/list`, `resources/list`, `prompts/list`, `list_ai_tool_contracts`, projected `propose_create_event_draft`, safe conversation resources, and `create_event_draft_with_confirmation`) without running live MCP clients, provider calls, database writes, or content-bearing artifacts.
- **Runbook posture:** Operations and AI agent hardening docs now require the deterministic replay report before manual Inspector work, one authenticated context per smoke run, tenant binding through the same trusted API edge posture, listing tools/resources/prompts before calls, optional `propose_create_event_draft` only against disposable test conversations, and no confirm/reject or repository mutation from Inspector. Retained artifacts are limited to scenario codes, pass/fail status, redacted endpoint path/auth mode, and bounded failure categories.
- **Validation:** `dotnet test --project Explore.Diagnostic.UnitTests/Explore.Diagnostic.UnitTests.csproj --configuration Release --verbosity quiet --no-restore` passed 30/30. `dotnet run --project Explore.Diagnostic/Explore.Diagnostic.csproj --configuration Release --no-restore -- ai-replay-report --output /tmp/explore-ai-replay-mcp-inspector` generated 5 PASS, 0 WARN, 0 FAIL with no live provider credentials, content-bearing artifacts, or database side effects. Focused MCP/API integration tests still pass 28/28, and `dotnet build Explore.Diagnostic/Explore.Diagnostic.csproj --configuration Release --verbosity quiet --no-restore` passed.
- **Remaining at that point:** Phase 11.6 client compatibility/protocol-evolution review followed and is now complete. Do not automate live Inspector/client runs in normal CI until artifact redaction, fake data, auth, tenant binding, and side-effect guarantees are explicitly modeled and tested.

### Implementation Slice — 2026-06-07 Europe/Brussels — Phase 11.6 Client Compatibility/Protocol Evolution Review

- **Current state:** Phase 11.6 is implemented as the initial protocol-evolution review gate. No runtime MCP behavior changed in this slice.
- **Context7 evidence:** Official SDK docs say stateless Streamable HTTP is recommended for servers that do not need server-to-client requests. In stateless mode, `Mcp-Session-Id` is not used, GET/DELETE MCP endpoints are unavailable, legacy SSE is disabled, and server-to-client requests such as sampling, elicitation, roots, plus unsolicited notifications are disabled. SDK docs also show list-changed/progress notifications and resource subscriptions as client-visible capabilities that require explicit handling, and `ToolAnnotations` as non-binding hints only.
- **Files changed:** `docs/adr/ADR-010-mcp-adapter-hosting-strategy.md`, `docs/ARCHITECTURE.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/SELF_HOSTING.md`, and active workstream docs.
- **Review gate:** Future stateful sessions, `Mcp-Session-Id`, session migration/resumability, legacy SSE, sampling, elicitation, roots, completions, progress notifications, tool/resource/prompt list-changed notifications, resource subscriptions, dynamic non-registry tools, client-specific compatibility shims, protocol-version/header changes, or annotation-authority changes now require a new ADR/task review before implementation.
- **Operational posture:** The default compatibility answer remains `Mcp:Enabled=false` or the current minimal stateless/proposal-first surface until review evidence, targeted MCP tests, replay report, redacted Inspector smoke, docs, self-hosting impact, and rollback are complete.
- **Validation:** Documentation/protocol review completed with Context7 evidence. `dotnet build --configuration Release --verbosity quiet --no-restore` passed 25 projects, 0 errors, existing warnings. `git diff --check` passed. Architecture tests were reattempted and still fail on pre-existing untracked `.claude/skills/*` frontmatter/section/line-count schema violations unrelated to MCP adapter code.
- **Remaining:** No Phase 11 implementation task remains. Future SDK upgrades or compatibility requests should start from this review gate rather than changing transport or client-visible behavior directly.

### Implementation Slice — 2026-06-07 Europe/Brussels — Phase 12 Planning From MCP Debug/Test Skills

- **Current state:** Phase 12 is now added as the MCP debuggability, automated contract-test, and client-smoke hardening roadmap. No runtime MCP behavior changed in this slice.
- **Skills used:** Read `mcp-csharp-debug` plus `references/ide-config.md` and `references/mcp-inspector.md`; read `mcp-csharp-test` plus `references/test-patterns.md` and `references/evaluations.md`.
- **Context7 evidence:** Official C# SDK docs confirm ASP.NET Core Streamable HTTP hosting through `AddMcpServer().WithHttpTransport(options => options.Stateless = true)` and `MapMcp()`, `.AddAuthorizationFilters()` for `[Authorize]` MCP methods, `McpClient.CreateAsync(...)` with `ListToolsAsync()`/`CallToolAsync()` for client-visible tests, in-memory pipe transports for tests, stderr-only logging for stdio, and MCP Inspector startup through `npx -y @modelcontextprotocol/inspector` with custom HTTP auth headers.
- **Files changed:** `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-plan.md`, `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-tasks.md`, and this context file.
- **Plan changes:** Added a new Phase 12 with tasks for local debug profiles, redacted client config templates, an official-SDK-or-compatible MCP client contract harness, protocol error/redaction tests, debug logging/metrics, Inspector/Copilot smoke runbooks, projected-tool binding/cancellation tests, deterministic MCP evaluations, a compatibility matrix/upgrade gate, a review-first doctor check, and a separate ADR-gated stdio diagnostic-host decision.
- **Safety notes:** Phase 12 is explicitly no-new-authority. It must not enable direct mutation, live-provider CI, stateful sessions, legacy SSE, product stdio hosting, raw protocol artifact retention, or committed MCP client secrets.
- **Validation:** Docs-only planning update. `git diff --check -- dev/active/ai-tool-contract-registry/ai-tool-contract-registry-plan.md dev/active/ai-tool-contract-registry/ai-tool-contract-registry-tasks.md dev/active/ai-tool-contract-registry/ai-tool-contract-registry-context.md` passed.
- **Remaining:** Superseded by the Phase 12 implementation slice below; no Phase 12 task remains.


### Implementation Slice — 2026-06-07 Europe/Brussels — Phase 12 MCP Debug/Test Hardening

- **Current state:** Phase 12.1-12.10 are implemented. MCP remains an optional API-hosted, authenticated, tenant-resolved, stateless Streamable HTTP adapter over ATCR/MediatR; the slice added debug/test/diagnostic confidence only and did not add direct mutation, product stdio, legacy SSE, stateful sessions, live-provider CI, or server-to-client MCP features.
- **Research evidence:** Context7 OpenTelemetry docs confirmed custom ActivitySource/Meter registration through `.AddSource("...")` and `.AddMeter("...")`; Context7 MCP SDK evidence from the planning slice remains the basis for stateless `MapMcp()`/authorization-filter/test posture. Tavily MCP research was used only for official MCP/C# SDK context and retained no secrets or raw outputs in docs.
- **Files changed:** Added `docs/MCP_DEBUGGING.md`, `docs/adr/ADR-011-local-mcp-stdio-diagnostic-host.md`, `Event.API.IntegrationTests/Features/McpProtocolContractTests.cs`, `Explore.API/Mcp/McpAdapterTelemetry.cs`, and `Explore.Diagnostic/Doctor/Checks/McpDebugReadinessDoctorCheck.cs`; updated MCP/API integration tests, ServiceDefaults OpenTelemetry registration, Diagnostic replay/evaluation/doctor tests, Operations/Configuration/Self-hosting/AI hardening/index docs, and active workstream docs.
- **Protocol contract model:** `McpProtocolContractTests` drives the API test host through stateless JSON-RPC (`initialize`, `tools/list`, `resources/list`, `resources/templates/list`, `prompts/list`, and `tools/call`) with authenticated test principals. It verifies registry discovery, generic/projected proposal tools, disabled endpoint behavior, malformed/unknown/hidden-field redaction, and proposal-only behavior by asserting events are not created.
- **Diagnostics model:** `McpAdapterTelemetry` uses `Explore.Mcp` ActivitySource/Meter and records only allow-listed tool names, projected flag, bounded outcome, bounded failure code, and duration. Tool calls are instrumented without logging prompts, payload JSON, tenant/user identifiers, endpoint URLs, bearer/API-key values, provider/model data, or raw exceptions.
- **Evaluation/doctor model:** Diagnostic replay now has seven scenarios, including projected-tool selection and confirmation-required MCP guidance. Advisory AI evaluation now has six dimensions, including `McpProposalFlow`. `McpDebugReadinessDoctorCheck` verifies docs/tests/replay/evaluation/ignore-rule/ADR presence without starting servers, clients, migrations, token generation, endpoint calls, or secret printing. ADR-011 defers local stdio diagnostic hosting and keeps product MCP API-hosted Streamable HTTP only.
- **Validation:** `dotnet test --project Explore.Diagnostic.UnitTests/Explore.Diagnostic.UnitTests.csproj --configuration Release --verbosity quiet --no-restore` passed 35/35. `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*McpProjectedToolTests/*|/*/*/*McpSdkContractTests/*|/*/*/*McpProtocolContractTests/*" --no-progress --maximum-parallel-tests 1` ran the API suite and passed 1279/1283 with 4 intentional skips. `ai-replay-report` generated 7 PASS, 0 WARN, 0 FAIL; `ai-eval-report` generated 6 PASS, 0 WARN, 0 FAIL; doctor generated 8 PASS, 0 WARN, 0 FAIL. `dotnet build --configuration Release --verbosity quiet --no-restore` passed 25 projects, 0 errors, existing warnings. `git diff --check` passed. Focused `McpProjectedToolTests` passed 9/9 after the final null-safety patch.
- **Remaining:** No Phase 12 task remains. Architecture tests now pass for this workspace (190 total, 189 succeeded, 1 intentional skip).

### Handoff — 2026-06-07 Europe/Brussels

#### Current State

- What is completed: Phases 0-12 are implemented in the current worktree. Phase 12 adds redacted MCP debugging docs/templates, JSON-RPC protocol contract tests, bounded `Explore.Mcp` telemetry, projected-tool binding/cancellation tests, deterministic MCP replay/evaluation scenarios, a review-first doctor check, and ADR-011 stdio deferral.
- What is in progress: No Phase 12 implementation task remains. Any next slice should be user-approved follow-up work or cleanup of unrelated context-system blockers.
- What changed since the last handoff: The active plan/tasks/context now reflect Phase 12 completion and verification. MCP remains API-hosted, stateless, authenticated, registry-backed, and proposal-first.

#### Next Action

1. Review and commit the Phase 12 implementation or start a separately approved follow-up slice.
2. Keep architecture/context validation green if future skill or agent files change.
3. Keep legacy SSE, stateful sessions, stdio product hosting, Native AOT support claims, server-to-client MCP capabilities, direct mutation, raw protocol artifact retention, live-client CI, and remote MCP tool import rejected unless a new ADR, user approval, and targeted verification allow them.

#### Blockers

- No blocker for Phase 12 implementation work.
- Full architecture/context validation now passes for this workspace; one existing API-contract metadata test remains intentionally skipped.

#### Modified Files

- `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-plan.md` — re-baselined with MCP debug/test skill evidence and new Phase 12 roadmap.
- `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-tasks.md` — marked Phase 11 complete, added Phase 12 checklist, and refreshed verification/remaining-work notes.
- `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-context.md` — refreshed session progress and handoff state for `/dev-docs-update`.

#### Validation

- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*Mcp*/*" --no-progress --maximum-parallel-tests 1` — passed, 28/28.
- `dotnet test --project Explore.Diagnostic.UnitTests/Explore.Diagnostic.UnitTests.csproj --configuration Release --verbosity quiet --no-restore` — passed, 30/30.
- `dotnet run --project Explore.Diagnostic/Explore.Diagnostic.csproj --configuration Release --no-restore -- ai-replay-report --output /tmp/explore-ai-replay-mcp-inspector` — generated 5 PASS, 0 WARN, 0 FAIL with no live credentials/content artifacts/database side effects.
- `dotnet build Explore.Diagnostic/Explore.Diagnostic.csproj --configuration Release --verbosity quiet --no-restore` — passed, 3 projects, 0 errors, existing warnings.
- `dotnet build --configuration Release --verbosity quiet --no-restore` — passed, 25 projects, 0 errors, existing warnings.
- `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet --no-restore` — passed, 7 projects, 0 errors, existing warnings.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --no-restore -- --no-progress --maximum-parallel-tests 1` — passed, 190 total, 189 succeeded, 1 intentional skip.
- `git diff --check` — passed.
- `git diff --check -- dev/active/ai-tool-contract-registry/ai-tool-contract-registry-plan.md dev/active/ai-tool-contract-registry/ai-tool-contract-registry-tasks.md dev/active/ai-tool-contract-registry/ai-tool-contract-registry-context.md` — passed after `/dev-docs-update`.
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --no-restore -- --no-progress --maximum-parallel-tests 1` — passed, 190 total, 189 succeeded, 1 intentional skip.
- Commands completed for Phase 12: Diagnostic unit tests, API integration tests, replay/eval reports, doctor, Release build, architecture tests, and `git diff --check`.

#### Documentation Impact

- `ai-tool-contract-registry-plan.md` now includes Phase 12 and the MCP debug/test skill re-baseline.
- `ai-tool-contract-registry-tasks.md` marks Phase 11 complete and adds the Phase 12 planned task checklist.
- `ai-tool-contract-registry-context.md` records the debug/test skill evidence, Context7 evidence, planned tasks, safety boundaries, and next action.
- Journal entry: not added; this was a workstream planning/handoff refresh, not a durable reusable finding beyond the active docs.

#### Risks

- Do not let SDK annotations replace product authorization; they are hints only.
- Do not leak `ModelContextProtocol` types into Domain/Application.
- First-class MCP projection must not execute domain mutations directly; it must persist proposed actions and wait for HAL/API confirmation.
- Do not let debug tooling commit raw MCP request/response bodies, Inspector screenshots, Copilot transcripts, bearer/API-key values, tenant/user identifiers, prompts, provider responses, endpoint URLs, or raw exceptions.

#### Notes For Next Contributor Or Agent

- Required docs/rules to read: `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `.claude/skills/mcp-csharp-debug/SKILL.md`, `.claude/skills/mcp-csharp-test/SKILL.md`, and the three active ATCR docs.
- Assumptions made: Phase 12 improves debug/test confidence only; it does not approve transport, authority, mutation, or self-hosting behavior changes.
- Do not touch unrelated dirty files: current `git status --short` includes many pre-existing modified/untracked files outside this `/dev-docs-update` refresh, including `.github/**`, package lock files, Blazor files, provider files, docs, untracked MCP skill folders, and other active workstream docs such as `dev/active/full-input-validation-sanitization/full-input-validation-sanitization-tasks.md`. Preserve them unless the user explicitly asks to work on that scope.

### Implementation Slice — 2026-06-07 Europe/Brussels — MCP Infisical Mapping And Phase 13 Planning

- **Current state:** Added startup compatibility mapping for the new Infisical `/api` MCP secrets and recorded Phase 13 as planned work. Runtime DB governance, API-key scopes, anonymous-safe MCP reads, invalid-key fallback, and legacy-SSE effective-state handling are not implemented yet.
- **Context7 evidence:** ASP.NET Core documentation confirms double-underscore environment variables map to colon-separated configuration keys and options bind from configuration sections. The compatibility mapping follows that convention by mapping raw Infisical `MCP_*` keys into canonical `Mcp:*` settings.
- **Files changed:** `Explore.API/Extensions/ConfigurationExtensions.cs`, `docs/CONFIGURATION.md`, `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-plan.md`, `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-tasks.md`, and this context file.
- **Configuration behavior:** `/api/MCP_ENABLED` maps to `Mcp:Enabled`; `/api/MCP_ENDPOINT_PATH` maps to `Mcp:EndpointPath` and normalizes a bare value like `mcp` to `/mcp`; `/api/MCP_STATELESS` maps to `Mcp:Stateless`; `/api/MCP_ENABLE_LEGACY_SSE` maps to `Mcp:EnableLegacySse`. Canonical `Mcp:*` keys still win because mapping uses the existing `TrySet` guard.
- **Phase 13 scope:** The next slice must treat `MCP_ENABLED` as a startup ceiling before runtime DB settings can enable MCP. `mcp.enabled` and `mcp.enable_legacy_sse` should be runtime-governed at instance and tenant levels with instance locks. Endpoint path and stateless mode remain startup-only and must not be editable at runtime.
- **Auth posture:** Phase 13 should make external MCP API-key-first, not bearer-token-first. MCP must also support anonymous-safe operations when no key or an invalid key is supplied, while valid API keys unlock only scoped authorized MCP capabilities.
- **Compatibility note:** Superseded by the following Phase 13 implementation slice: endpoint-wide MCP authorization has now been replaced by per-operation SDK authorization, and `MCP_ENABLE_LEGACY_SSE=true` is treated as a startup ceiling while runtime legacy SSE remains disabled.
- **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet --no-restore` passed for 7 projects with existing warnings; `git diff --check` passed for the modified files.

### Implementation Slice — 2026-06-07 Europe/Brussels — Phase 13 Startup, API-Key, And Anonymous-Safe MCP Posture

- **Current state:** Phase 13.1 is implemented. Phase 13.3, 13.4, 13.6, 13.7, and 13.8 are partially implemented. Runtime DB governance/locks/admin UX (13.2), public-event anonymous MCP read tools/resources (13.5), rate-limit partitions, revoked-key/tenant-mismatch coverage, unknown-scope API-key validation assertions, and legacy-SSE runtime governance remain pending.
- **Context7 evidence:** Official ModelContextProtocol C# SDK docs confirm ASP.NET Core servers use `AddMcpServer().WithHttpTransport(options => options.Stateless = true).MapMcp()`, stateless mode is recommended when server-to-client features are unnecessary, and `.AddAuthorizationFilters()` enables standard `[Authorize]`/`[AllowAnonymous]` metadata on MCP tools. ASP.NET Core docs confirm normal configuration binding and environment-variable hierarchy mapping; the Infisical `/api/MCP_*` mapping keeps canonical `Mcp:*` as the effective options surface.
- **Tavily note:** Tavily MCP was requested but is not available in the current tool/plugin context after tool discovery and install-candidate checks. This slice used Context7 and repository source/docs for research instead.
- **Files changed:** API startup/auth/tenant middleware, MCP registry tool, API-key handler, MCP settings validator/health check, External API Key scopes/ceilings/machine-scope mapping, MCP/API/infrastructure/application tests, MCP/configuration/operations/self-hosting/security/API/ADR docs, and active workstream plan/tasks/context.
- **Auth/control flow:** `app.MapMcp(...).AllowAnonymous()` removes endpoint-wide auth so SDK authorization filters can make `list_ai_tool_contracts` explicitly `[AllowAnonymous]`. Private MCP tools/resources/prompts keep `[Authorize]` and still delegate to MediatR/CQRS proposal flows; mutating MCP calls continue to create proposed actions only. `/mcp` now participates in the API auth-conflict middleware and tenant pre/post-auth middleware, so `Authorization` plus `X-API-Key` fails with a redacted bad request and valid API keys can bind tenant context while invalid/no-key traffic can reach only anonymous-safe discovery when tenant context is otherwise resolved.
- **Scope model:** Added `mcp:read` and `mcp:propose`, included them in user/tenant ceilings, and mapped them narrowly to AI conversation view/proposal actions. Unit tests prove these scopes do not grant event writes, event reads, send-message, confirmation, or generic user-write authority.
- **Legacy SSE model:** `MCP_ENABLE_LEGACY_SSE=true` no longer fails startup validation. It is only a startup ceiling; `Program.cs` still does not set SDK legacy SSE options, and the readiness payload now reports `legacySseStartupCeiling` plus `legacySseRuntimeEnabled=false`.
- **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet --no-restore` passed after repairing a pre-existing OpenAI-compatible model-discovery compile gap. `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*ExternalApiKeyScopeCeilingTests/*|/*/*/*MachineScopeMappingTests/*" --no-progress --maximum-parallel-tests 1` passed the full Application unit suite (1300/1300). `dotnet test Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*OpenAiCompatibleChatProviderTests/*|/*/*/*AiProviderSettingsValidatorTests/*" --no-progress --maximum-parallel-tests 1` passed the full Infrastructure suite (437/437) after adding model-discovery endpoint coverage. `dotnet test Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*McpAuthorizationTests/*|/*/*/*McpSdkContractTests/*|/*/*/*McpAdapterSettingsValidatorTests/*|/*/*/*McpAdapterHealthCheckTests/*|/*/*/*McpProtocolContractTests/*" --no-progress --maximum-parallel-tests 1` passed the full API integration suite (1282 succeeded, 4 intentional skips). `dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --no-restore -- --no-progress --maximum-parallel-tests 1` passed (189 succeeded, 1 intentional skip). `dotnet build --configuration Release --verbosity quiet --no-restore` passed 25 projects with existing warnings. `git diff --check` passed.
- **Compatibility note:** Superseded by the following runtime-governance slice for `mcp.enabled`/`mcp.enable_legacy_sse` settings and locks. Remaining Phase 13 work is now public anonymous MCP read-resource decisions, revoked-key/tenant-mismatch coverage, missing-scope forbidden-shape tests, unknown-scope validation assertions, and rate-limit/audit partitioning.

### Implementation Slice — 2026-06-07 Europe/Brussels — Phase 13 Runtime MCP Governance And Safe Legacy-SSE State

- **Current state:** Phase 13.2 is implemented and Phase 13.7 now resolves to the safe-unavailable outcome. Runtime DB governance/locks/admin UX are no longer pending. Remaining Phase 13 work is focused on rate-limit/audit assertions, any additional anonymous-safe read resources, revoked-key/tenant-mismatch coverage, and call-level scope failure-shape tests.
- **Context7 evidence:** Official ModelContextProtocol C# SDK docs confirm ASP.NET Core MCP servers use `AddMcpServer().WithHttpTransport(...).MapMcp()`, recommend stateless mode when sessions/server-to-client requests are unnecessary, and document legacy SSE as requiring stateful mode plus explicit `EnableLegacySse`. ASP.NET Core docs confirm middleware must run after routing/authentication and before endpoint mapping/authorization where context-dependent gates are needed.
- **Tavily note:** Tavily MCP remains unavailable in the current tool/plugin context after tool discovery and install-candidate checks; no Tavily connector/plugin is available to install in this session.
- **Files changed:** `GovernanceSettingKeys`, MCP setting definitions/registry, `McpSettingGroup`, tenant-delegation setting group, governance DTOs, instance governance service and commands, tenant policy read/apply/validation paths, `McpRuntimeStateService`, `McpRuntimeGateMiddleware`, MCP health check, instance settings controller routes, Blazor instance/tenant admin settings UI/services, MCP runtime/health/application tests, and MCP configuration/operations/debugging/self-hosting/API docs.
- **Architecture/control flow:** Startup `Mcp:Enabled` is the operator ceiling and maps the endpoint only at application startup. `McpRuntimeGateMiddleware` then runs after tenant/auth resolution and computes effective runtime state from instance `mcp.enabled`, tenant override values, and `governance.lock_tenant_mcp`. If startup is true but runtime resolves false, `/mcp` returns `404` without leaking tenant IDs, endpoint URLs, credentials, prompts, or raw protocol data.
- **Legacy SSE:** `mcp.enable_legacy_sse` and `governance.lock_tenant_mcp_legacy_sse` now exist and resolve through the same instance/tenant policy path, but `LegacySseRuntimeEnabled` remains `false`. This records governance intent without exposing stateful/session-affinity transport behavior.
- **Admin UX/API:** Instance admins can read/update MCP governance via `GET/PUT /api/instance/settings/mcp`; the Blazor instance advanced settings surface exposes runtime enablement, legacy-SSE intent, and tenant locks. Tenant admins see MCP override controls only when the instance locks allow it. Endpoint path and stateless mode are intentionally absent from runtime-editable models.
- **Validation:** `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet --no-restore` passed. `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet --no-restore` passed. `dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*McpSettingGroupTests/*|/*/*/*InstanceGovernanceSettingServiceTests/*|/*/*/*TenantPolicySettingServiceTests/*|/*/*/*UpdateTenantPolicySettingsCommandHandlerTests/*" --no-progress --maximum-parallel-tests 1` passed the full Application unit suite (1308/1308). `dotnet test Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*McpAuthorizationTests/*|/*/*/*McpRuntimeStateServiceTests/*|/*/*/*McpAdapterHealthCheckTests/*|/*/*/*McpProtocolContractTests/*|/*/*/*McpSdkContractTests/*" --no-progress --maximum-parallel-tests 1` passed the full API integration suite (1289 succeeded, 4 intentional skips). `dotnet test Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet --no-restore -- --no-progress --maximum-parallel-tests 1` passed (1326 succeeded, 1 intentional skip). `dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --no-restore -- --no-progress --maximum-parallel-tests 1` passed (189 succeeded, 1 intentional skip). `dotnet build --configuration Release --verbosity quiet --no-restore` passed 25 projects with existing warnings. `git diff --check` passed after stripping generated-client trailing whitespace.
