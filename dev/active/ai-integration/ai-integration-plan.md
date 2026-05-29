<!-- ABOUTME: Durable implementation plan for the Plane-inspired ISLAMU Event AI assistant integration. -->
<!-- ABOUTME: Captures verified current state, architecture decisions, phased work, validation, and handoff rules. -->

# AI Integration — Implementation Plan

Last Updated: 2026-05-29 Europe/Brussels

## 0. Planning Metadata

- **Request:** Plan a Plane-inspired, enterprise-grade AI right-side panel for ISLAMU Event that can reference events, create event drafts through confirmation, persist conversation history, support model/provider selection, and remain self-hostable, maintainable, and Clean Architecture compliant.
- **Task directory:** `dev/active/ai-integration/`
- **Planning status:** User-approved implementation in progress. The user instructed the agent to “start implementing,” which resolves the prior approval blocker.
- **Existing source analysis:** `dev/active/ai-integration/ai-integration-plane-report.md` is the deep Plane analysis. It verifies Plane is AGPL-3.0-only and useful as inspiration, but Plane public CE does not contain the complete persisted agentic side-panel described by the desired ISLAMU experience.
- **Baseline verification:** `dotnet build --configuration Release --verbosity quiet` passed before first implementation edit with 0 errors and 15 existing warnings.
- **Provider docs refresh:** Context7 and Tavily were both attempted during implementation start but quota/plan limits blocked live MCP research. Official Microsoft Learn fallback confirms `Microsoft.Extensions.AI` supports `IChatClient`, provider-neutral chat abstractions, streaming, middleware-style telemetry/caching, and function invocation. Semantic Kernel is enterprise-oriented middleware for agents/plugins and automatic function calling. OpenAI docs were unavailable through the current fetch path, so the plan keeps the conservative safety rule from prior research: tool-call arguments are untrusted model output and must be validated. Implementation decision: keep an ISLAMU-owned Application `IAiChatProvider`, add no provider SDK dependency in Domain, use a deterministic fake provider for tests, and implement the first concrete OpenAI-compatible adapter behind Infrastructure in Phase 2. Direct automatic mutation execution remains forbidden in the first release.
- **Matched intents:** `add-get-endpoint`, `add-write-endpoint`, `add-hal-link`, `add-cqrs-handler`, `add-ef-migration`, `update-repository-query`, `blazor-component-affordance`, `openapi-contract-change`.
- **Relevant skills loaded:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `accessibility`, `outbox-pattern`, `error-tracking`, `aspire`.
- **Relevant rules loaded:** `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/application-layer.md`, `.claude/rules/domain.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/efcore-migrations.md`, `.claude/rules/blazor-client.md`, `.claude/rules/blazor-server.md`, `.claude/rules/tests.md`.
- **Canonical docs read:** `AGENTS.md`, `dev/active/README.md`, `.claude/contract/intents.yaml`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, `docs/BLAZOR.md`, `docs/DOCK_LAYOUT.md`, `docs/DOMAIN.md`, `docs/CONFIGURATION.md`, `docs/MULTI_TENANCY.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/DESIGN_SYSTEM.md`, `docs/ACCESSIBILITY.md`, `docs/LOCALIZATION.md`, `docs/OUTBOX_PATTERN.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`.
- **Primary layers touched:** Domain, Application, Persistence, Infrastructure, API, Blazor Client, Docs, Operations.
- **Estimated complexity:** XL, because the work crosses persistence, provider integration, secure API design, HAL, idempotency, Blazor UX, generated clients, privacy/retention, observability, and multiple test projects.

### 0.1 Intent Contract Summary

| Intent | Why it applies | Required tests/docs impact |
|---|---|---|
| `add-get-endpoint` | Bootstrap, conversation list/detail, reference search, run reads. | `Event.API.IntegrationTests`, `Event.Architecture.Tests`, `docs/API_CHANGELOG.md`. |
| `add-write-endpoint` | Create conversation, send message, confirm/reject action; cancel run later in Phase 7. | Auth, idempotency, ProblemDetails, API integration tests, changelog. |
| `add-hal-link` | AI proposal/result buttons must be link-driven. | API HATEOAS tests and Blazor HAL-gating tests. |
| `add-cqrs-handler` | AI commands/queries and tool execution through MediatR. | Application unit tests and architecture tests. |
| `add-ef-migration` | Persist conversations/messages/runs/actions/references. | Persistence integration tests and `schemas/islamu-event.md`. |
| `update-repository-query` | Event reference search and AI aggregate repositories. | Repositories return entities; tenant-filter tests. |
| `blazor-component-affordance` | Right panel UI, action cards, confirmation controls. | `Explore.Blazor.Client.Tests`, docs update. |
| `openapi-contract-change` | New endpoint family changes public API contract. | Operation IDs, route names, generated client, changelog. |

### 0.2 Senior CTO Review Verdict

**Decision:** Approve with required changes. The direction is right, but the first draft deferred too much safety work to late hardening. AI provider calls introduce cost, abuse, privacy, and operator-failure modes as soon as `SendMessage` exists, not after the UI is polished. This revision makes the safe MVP gate explicit:

1. no automatic tool/function invocation for mutating operations in the first release;
2. authenticated private bootstrap/history endpoints split from any anonymous capability metadata;
3. provider endpoint/config validation, health, metrics, prompt bounds, quotas, and rate limits before broad enablement;
4. retention/redaction posture planned before persisted conversation history is exposed;
5. AI resource/action authorization and Cerbos/local parity tests added as first-class tasks.

## 1. Executive Summary

ISLAMU Event already has the shell-level foundation for an AI assistant: tenant settings, user preference, navbar/dock bridge, and a placeholder right rail. The implementation gap is the functional assistant itself: persisted conversations, model/provider bootstrap, event reference search, prompt orchestration, typed proposed actions, confirmation, event draft creation, auditability, provider health, and enterprise operational controls.

The target is not to copy Plane code. The target is to adopt the best product lessons from Plane's open-source code and build a native ISLAMU architecture:

1. User opens the existing right-side AI dock.
2. Panel bootstraps assistant availability, tenant-approved models, recent conversations, and HAL affordances.
3. User selects event references through server-side search.
4. User asks AI to create an event draft.
5. API persists the user message, builds bounded prompt context, calls a provider through an Application-owned abstraction, and persists the assistant response.
6. Assistant returns a typed `CreateEventDraft` proposed action, not an immediate side effect.
7. UI renders a confirmation card with the exact draft fields and only shows Confirm/Reject buttons if HAL links are present.
8. Confirm endpoint revalidates permissions and executes the existing `CreateEventCommand` path.
9. Conversation history records prompt, model, references, proposal, confirmation, and result links.

The safe MVP is not "chat first, harden later." The first user-facing send/confirm flow must already include tenant/auth tests, prompt/reference bounds, provider timeout handling, low-cardinality metrics, no-content logging, rate/usage limits, and an operator-visible disabled/misconfigured state. Streaming, advanced dashboards, and additional tools can wait.

### In scope

- Persistent AI conversations/messages/runs/history.
- Tenant-governed provider/model configuration and model selection.
- Event reference search and server-side prompt packing.
- First tool: propose and confirm `CreateEventDraft`.
- Blazor/MudBlazor assistant UI inside existing shell dock.
- HAL-gated action cards and result links.
- Provider abstraction with fake provider tests and OpenAI-compatible adapter first.
- API contract, generated client, tests, docs, metrics, health checks, and retention planning.

### Out of scope for first release

- Autonomous publish/delete/registration/email/bulk side effects.
- Arbitrary tool execution or unrestricted agent loops.
- Browser-side provider calls or browser-visible provider secrets.
- Compatibility shims for nonexistent old AI endpoints.
- Creating child events for talks/workshops. Future session generation must use `EventSession`, not child `Event` aggregates.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| AI rail exists but is placeholder. | Verified: `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor`, `.razor.css`. | High | Body only says chat functionality is coming soon. |
| Shell dock already owns AI panel placement/persistence. | Verified: `Explore.Blazor.Client/Components/Shell/ShellDockPanels.cs`; `docs/DOCK_LAYOUT.md`. | High | Panel ID is `shell.ai-assistant`, end side, resizable, persisted. |
| Main layout bridges AI state to dock. | Verified: `Explore.Blazor.Client/Layout/MainLayout.razor.cs`. | High | Preserve `AiAssistantState` ↔ `DockLayoutState` bridge. |
| AI availability/user preference exists. | Verified: `AiAssistantState.cs`, `SettingsAiAssistant.razor`, `AiAssistantPreferenceSettingDefinitions.cs`. | High | No conversation/model/reference state yet. |
| AI governance config exists but is minimal. | Verified: `GovernanceSettingKeys.cs`, `AiAssistantSettingDefinitions.cs`, `AiAssistantSettingGroup.cs`, `docs/CONFIGURATION.md`. | High | Current keys: enabled, endpoint URL, API key, anonymous access. |
| No functional AI backend exists. | Not found: `AiAssistantController`, `AiConversation`, `IAiChatProvider`, `AiProposedAction`, `AiReference` in production code. | High | New Domain/Application/Persistence/Infrastructure/API work required. |
| Event draft create path exists and must be reused. | Verified: `EventController.cs`, `CreateEventDraftRequestDto.cs`, `CreateEventCommand.cs`, `CreateEventCommandHandler.cs`. | High | Handler validates, resolves actor, uses transaction, metrics, cache invalidation. |
| Event create validator is manually instantiated. | Verified: `CreateEventCommandHandler.cs` and `CreateEventRequestValidator.cs`. | High | AI handlers must follow manual validator convention. |
| Event HAL link policy exists. | Verified: `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`; `RouteNames.cs`. | High | AI UI must gate affordances by links. |
| Generated Blazor client/service patterns exist. | Verified: `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `Explore.Blazor.Client/Services/EventService.cs`. | High | New AI endpoints should flow through OpenAPI/generation. |
| Existing tests protect AI shell state. | Verified: `AiAssistantStateTests.cs`, `MainLayoutTests.cs`. | High | Must be extended, not regressed. |
| Related event creation plan changes semantics. | Verified: `dev/active/event-creation-progressive-disclosure/event-creation-progressive-disclosure-plan.md`. | High | `Event` is container; talks/workshops are `EventSession`. |
| Plane analysis report exists. | Verified: `ai-integration-plane-report.md` has 2943 lines. | High | Use as inspiration and credit source. |

### 2.2 Existing Implementation By Layer

- **Domain:** AI setting keys and definitions exist, but no conversation/message/run/action/reference entities exist.
- **Application:** `AiAssistantSettingGroup` exposes basic availability. Event creation already uses `CreateEventCommand` and `CreateEventRequestValidator`; no AI feature handlers exist.
- **Persistence:** No AI DbSets/configurations/repositories exist. Event repository/query patterns exist and repositories return entities.
- **Infrastructure:** No AI provider exists. `Explore.Infrastructure/InfrastructureServicesRegistration.cs` is the likely registration point for provider clients/settings/health checks.
- **API:** Event create endpoint exists and is authorized. No AI controller/routes/HAL policy exist.
- **Blazor:** The AI rail/dock shell exists. No chat client service, model selector, reference picker, action card, or history component exists.
- **Docs:** Dock/config docs mention the AI assistant, but only as availability/settings, not as a functional integration.

### 2.3 Existing Tests And Missing Coverage

Existing relevant coverage:

- `Explore.Blazor.Client.Tests/Services/AiAssistantStateTests.cs` — state availability/open/preference behavior.
- `Explore.Blazor.Client.Tests/Layout/MainLayoutTests.cs` — dock bridge behavior.
- `Event.API.IntegrationTests/Features/Hateoas/*` — patterns for HAL policy testing.
- `Event.API.IntegrationTests/Features/IdempotencyMiddlewareTests.cs` — idempotency test pattern.
- `Event.Application.UnitTests/*`, `Event.Persistence.IntegrationTests/*`, `Event.Architecture.Tests/*` — required project test surfaces.

Missing required coverage:

- AI domain lifecycle tests.
- AI repository/persistence tests with tenant isolation.
- Provider config/adapter/fake tests.
- Application tests for prompt building, parsing, runs, and action confirmation.
- API tests for auth, idempotency, ProblemDetails, HAL links, and cross-tenant isolation.
- Blazor tests for history, reference picker, model selector, action cards, and HAL-gated buttons.

### 2.4 Current Pain Points / Improvement Areas

1. The existing UI advertises an AI assistant but cannot chat or act.
2. Existing AI settings are too small for self-hosted enterprise use: no provider type, model allow-list, retention, token limits, timeout, streaming, or tool policy.
3. There is no persisted history/audit model.
4. There is no typed proposed-action model, so any naive implementation risks unsafe model-driven side effects.
5. There is no server-side reference search/prompt-packing boundary.
6. There is no AI provider abstraction or fake provider test harness.
7. There is no AI-specific rate-limit/idempotency/concurrency design.
8. The existing dock foundation is good and should be reused, not replaced.
9. The event model is easy to misuse; AI must create an `Event` draft first and use future session tools for talks/workshops.

### 2.5 Unknowns After Investigation

| Unknown | Impact | Resolution |
|---|---|---|
| Best .NET AI abstraction: raw OpenAI-compatible HTTP, Microsoft.Extensions.AI, Semantic Kernel, or provider SDK. | Affects dependencies, streaming, tool calling, and tests. | Phase 0 uses Context7/official docs; default is internal `IAiChatProvider` and OpenAI-compatible adapter. |
| Exact generated-client workflow. | Affects Blazor integration. | Verify in Phase 3 before updating `EventApiClient.g.cs`. |
| Rate-limiting primitives already available in API. | Affects abuse/cost controls. | Investigate in Phase 7 or while adding endpoints. |
| Streaming transport choice. | Affects API contract and Blazor UX. | Non-streaming first; Phase 7 decides SSE/SignalR/polling. |
| Retention/privacy defaults. | Affects DB growth/compliance. | Add settings in Phase 2; cleanup may be a later hardening task. |
| Event creation DTO may change via active workstream. | AI mapper could drift. | Re-read event creation docs before Phase 5. |
| Provider endpoint egress/SSRF posture. | Affects self-hosted provider URLs and server-side outbound requests. | Phase 2 must validate configured endpoints, forbid browser-supplied provider URLs, and document allowed schemes/ownership. |

## 3. Proposed Future State

### 3.1 User Experience Flow

```text
Open AI dock
  -> bootstrap assistant/model/history/HAL state
  -> search and select event references
  -> send prompt with selected model and references
  -> API persists message/run and calls provider
  -> API persists assistant response and typed proposed actions
  -> UI renders CreateEventDraft action preview
  -> user confirms through HAL-gated button
  -> API revalidates authorization and sends CreateEventCommand
  -> event draft is created and returned with result links
  -> conversation history shows full trace
```

The UI should feel like a right-side workspace assistant, not a modal. It should preserve conversation context, show selected references as chips/cards, allow switching model when permitted, display loading/cancellation states, and make AI-generated proposals visually distinct from committed data.

### 3.2 Server Control Flow

```text
Blazor AiAssistantRail
  -> IAiAssistantClientService.GetBootstrapAsync()
  -> SearchReferencesAsync(kind=event, query)
  -> SendMessageAsync(conversationId, prompt, modelId, referenceIds, idempotencyKey)
       AiAssistantController
         -> SendAiMessageCommand
           -> load/create conversation
           -> persist user message + selected references
           -> build bounded prompt context from server-rehydrated events
           -> IAiChatProvider.CompleteAsync(...)
           -> parse assistant text + typed action JSON
           -> persist assistant message + proposed action(s)
           -> return DTO with HAL links
  -> ConfirmActionAsync(actionId, idempotencyKey)
       AiAssistantController
         -> ConfirmAiProposedActionCommand
           -> load action/conversation
           -> validate state + tenant/actor
           -> map payload to CreateEventDraftRequestDto/CreateEventRequest
           -> send CreateEventCommand through MediatR
           -> persist action execution result
           -> return updated action/conversation/result links
```

### 3.3 State Machines

- **Conversation:** `Active` → `Running` → `Active` or `BlockedByUserConfirmation`; optional `Archived` later.
- **Run:** `Queued` → `InProgress` → `Succeeded` / `Failed` / `Cancelled`.
- **Proposed action:** `Proposed` → `Confirmed` → `Executed` / `Failed`; `Proposed` → `Rejected`; optional `Expired` later.

### 3.4 First Tool: `CreateEventDraft`

Allowed payload fields should be intentionally narrow: title, short description, body/content, timezone, tentative start/end dates if known, organization/group candidate when server-authorized, and category/tag/template suggestions only when IDs are rehydrated/validated. The tool must not set tenant, actor, published status, owner, privileged flags, registration side effects, email dispatch, sessions, rooms, or agenda items in the first release.

### 3.5 Future Tools

Future tools should follow the same proposal-confirmation-execution pattern:

- `UpdateEventDraft`
- `CreateEventSessionDrafts` using `EventSession`
- `CreateAgendaItems`
- `SuggestRegistrationQuestions`
- `DraftNotification` through outbox confirmation
- Read-only summarization/rewrite/suggestion tools

## 4. Non-Negotiable Constraints

- Domain → Application → Persistence/Infrastructure → API/Blazor dependency direction only.
- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- `Guid` for aggregates, `int` for lookups, `long` for cursors/sequence IDs.
- GET endpoints are anonymous by default, but private AI history GET endpoints must be authenticated and documented as an explicit security exception.
- Write endpoints are `[Authorize]` and pass through existing authorization behavior.
- HAL links are the only source of truth for UI action affordances.
- Browser never receives provider API keys or raw credentials.
- AI never executes side effects without user confirmation.
- Event draft creation must reuse `CreateEventCommand`, not direct repository insertion.
- Provider prompts must be bounded and tenant-safe.
- Raw prompts/responses are not logged by default.
- New files must start with two `ABOUTME:` comment lines.
- No backward-compatibility shims unless the user explicitly requests them.
- Implementation agents must keep plan/context/tasks current after each meaningful slice.

## 5. Architecture And Design Decisions

### Decision 1: Reuse existing shell dock

- **Decision:** Build inside `AiAssistantRail.razor` and the existing `shell.ai-assistant` dock panel.
- **Why:** Dock layout already provides persistence, resizing, close behavior, mobile/RTL groundwork, and navbar integration.
- **Alternatives:** `MudDrawer`, modal dialog, separate route.
- **Consequence:** Rail must be split into maintainable child components and preserve existing tests.

### Decision 2: Server-side typed proposed actions

- **Decision:** Model output becomes persisted typed proposals; the client only renders and confirms.
- **Why:** Safe, auditable, idempotent, testable, and avoids model-driven direct side effects.
- **Alternatives:** Client-side JSON parsing or direct tool execution.
- **Consequence:** Need action entities, parser, HAL policies, confirm/reject endpoints.

### Decision 3: Reuse existing event creation command

- **Decision:** Confirmed `CreateEventDraft` action sends `CreateEventCommand` through MediatR.
- **Why:** Reuses validation, authorization, transactions, metrics, cache invalidation, and domain behavior.
- **Alternatives:** Direct repository insert or AI-only service.
- **Consequence:** AI mapper stays aligned with `CreateEventDraftRequestDto` and `CreateEventRequest`.

### Decision 4: Internal provider abstraction

- **Decision:** Application defines `IAiChatProvider` and related contracts; Infrastructure implements provider adapters.
- **Why:** Avoid vendor lock-in and preserve Clean Architecture. Current Microsoft docs show `Microsoft.Extensions.AI` has useful `IChatClient`, DI, streaming, tracing, and function-invocation primitives, while Semantic Kernel has richer plugin/kernel orchestration. Those primitives can be used inside Infrastructure adapters, but Application should not depend on a moving provider/agent SDK directly.
- **Alternatives:** Direct SDK calls in handlers, Semantic Kernel everywhere, browser provider calls, or direct `IChatClient` injection into Application handlers.
- **Consequence:** Provider choice can evolve. The safest enterprise default is `IAiChatProvider` in Application with optional Infrastructure adapters backed by `Microsoft.Extensions.AI`, Semantic Kernel, raw OpenAI-compatible HTTP, or a fake provider.

### Decision 5: OpenAI-compatible adapter first

- **Decision:** First production adapter targets OpenAI-compatible chat endpoints with configurable endpoint/model/key, plus deterministic fake provider for tests.
- **Why:** Most practical self-hostable path: OpenAI, enterprise gateways, LiteLLM, local proxies, etc.
- **Alternatives:** Only official OpenAI SDK; only local Ollama; only Semantic Kernel; direct `Microsoft.Extensions.AI` registration without an ISLAMU wrapper.
- **Consequence:** Need strong config validation, safe failure mapping, endpoint egress controls, and docs because compatibility differs by provider. Provider endpoint URLs must be deployment/admin-controlled only; the browser and per-request payloads must never choose outbound provider hosts.

### Decision 5a: No automatic mutation tool invocation in v1

- **Decision:** First release may use model function/tool calling or structured outputs only to produce persisted proposed actions. It must not enable Semantic Kernel automatic invocation or `Microsoft.Extensions.AI` automatic function invocation for mutating tools such as `CreateEventDraft`.
- **Why:** Provider tool-call arguments are untrusted, and ISLAMU requires human confirmation, HAL affordance gating, tenant policy, idempotency, and audit before side effects.
- **Alternatives:** Let Semantic Kernel auto-invoke plugin methods or let the provider directly call mutating tools.
- **Consequence:** Tool contracts are useful as schemas, but execution remains an Application command after user confirmation.

### Decision 6: Persist history and action audit

- **Decision:** Add first-class AI conversation/message/run/reference/action persistence.
- **Why:** Required for conversation history, audit, resumability, and enterprise governance.
- **Alternatives:** In-memory/local storage only.
- **Consequence:** Requires EF migration, tenant filters, retention settings, and repository tests.

### Decision 7: Server-side reference rehydration

- **Decision:** Browser passes reference IDs; Application rehydrates authorized event entities and prompt-packs bounded summaries.
- **Why:** Prevents stale/forged client context and cross-tenant leakage.
- **Alternatives:** Browser sends full event JSON.
- **Consequence:** Need reference search endpoint, event repository query, and prompt packer.

### Decision 8: HAL governs AI buttons

- **Decision:** Responses expose `confirm`, `reject`, `open-result`, event links, and later `cancel` when Phase 7 cancellation exists; UI renders based on link presence.
- **Why:** Aligns with repo invariant and centralizes authorization/state logic.
- **Alternatives:** Local role checks or status-only checks.
- **Consequence:** Need AI link policy tests and bUnit affordance tests.

### Decision 9: Idempotency for costly/side-effecting operations

- **Decision:** Sending messages and confirming actions must carry idempotency keys; confirming draft creation must be idempotent.
- **Why:** Prevent duplicate user messages, provider runs, proposed actions, or events on retry/double-click.
- **Alternatives:** Rely only on action status.
- **Consequence:** Need API tests for duplicate sends/confirms.

### Decision 10: Non-streaming first, streaming later

- **Decision:** Implement persisted request/response run model first; add streaming/cancellation after core flow.
- **Why:** Streaming increases API/UI/test complexity and should not block a safe first release.
- **Alternatives:** SSE/SignalR from day one.
- **Consequence:** First release can show spinner/progress; Phase 7 can add token streaming or polling.

### Decision 11: Credit Plane without copying code

- **Decision:** Reference Plane as inspiration in docs/README/comments where useful; implement native code.
- **Why:** Honors AGPL-compatible inspiration while reducing maintenance/licensing ambiguity.
- **Alternatives:** Port Plane code directly.
- **Consequence:** Add README/docs credit when implementation lands.

### Decision 12: Safe MVP gate before broad enablement

- **Decision:** The assistant remains disabled or limited to fake/provider-admin test mode until the MVP safety gate is complete.
- **Why:** Provider calls create cost, privacy, and operator-failure surfaces immediately.
- **Alternatives:** Ship chat/send first and add quotas/health/retention later.
- **Consequence:** Rate limits, prompt bounds, provider health, metrics, private-history auth tests, and retention policy are Phase 2/3 gates, not optional Phase 7 hardening.

## 6. Implementation Phases

### Phase 0: Plan Review, Baseline, Provider Decision

- **Goal:** Confirm product scope, refresh current AI provider docs, and establish baseline.
- **Depends on:** User review.
- **Relevant files:** `dev/active/ai-integration/*`, active event creation/roles plans.
- **Acceptance criteria:** User confirms scope; provider abstraction decision recorded; build/test baseline recorded; related active plans re-read.
- **Verification:** `dotnet build --configuration Release --verbosity quiet`.
- **Rollback/failure handling:** If provider docs are inconclusive, proceed with internal `IAiChatProvider` plus fake provider and defer production adapter.

#### Task 0.1: User reviews sections 3, 5, 6, 9, and 17

- **Type:** docs/investigate
- **Layer:** Docs
- **Files:** this plan, context, tasks.
- **Description:** Capture user corrections and set plan status to User-reviewed/Approved.
- **Acceptance:** Scope and first vertical slice are confirmed or updated.
- **Dependencies:** none.
- **Effort:** S
- **Validation:** docs updated.

#### Task 0.2: Re-run baseline and inspect dirty state

- **Type:** investigate
- **Layer:** DevOps
- **Files:** context only.
- **Description:** Run build and record `git status --short` so unrelated work is not mixed with AI changes.
- **Acceptance:** Baseline result and unrelated dirty state are documented.
- **Dependencies:** 0.1.
- **Effort:** S
- **Validation:** build command above.

#### Task 0.3: Finalize provider package/adapter decision

- **Type:** investigate
- **Layer:** Infrastructure
- **Files:** plan/context; later package files if approved.
- **Description:** Use the initial Context7 findings plus any additional official docs needed to choose the concrete first adapter path: raw OpenAI-compatible HTTP, Microsoft.Extensions.AI-backed adapter, Semantic Kernel-backed adapter, or provider SDK adapter.
- **Acceptance:** Provider strategy, packages, and fallback are documented.
- **Dependencies:** 0.1.
- **Effort:** M
- **Validation:** docs update.

#### Task 0.4: Re-read related active workstreams before implementation

- **Type:** investigate
- **Layer:** Product/Architecture
- **Files:** `dev/active/event-creation-progressive-disclosure/*`, `dev/active/event-scoped-operational-roles/*`.
- **Description:** Confirm current event draft DTO/flow and authorization model.
- **Acceptance:** AI create-draft payload matches current event model.
- **Dependencies:** 0.1.
- **Effort:** S
- **Validation:** context update.

### Phase 1: Domain And Persistence Foundation

- **Goal:** Persist AI conversations, messages, runs, references, proposed actions, and execution results.
- **Depends on:** Phase 0 approval.
- **Relevant files:** new `Explore.Domain/Ai/*`, new `IAiConversationRepository`, new `AiConversationRepository`, EF configs, DbSets, query filters, migration, `schemas/islamu-event.md`.
- **Acceptance criteria:** Entities enforce lifecycle; repositories return entities; tenant filters/indexes exist; migration and tests pass.
- **Verification:** Domain unit tests, Persistence integration tests, Architecture tests.

#### Task 1.1: Create AI domain status enums/value objects

- **Type:** create
- **Layer:** Domain
- **Files:** `Explore.Domain/Ai/AiConversationStatus.cs`, `AiMessageRole.cs`, `AiRunStatus.cs`, `AiProposedActionStatus.cs`, `AiProposedActionKind.cs`, `AiReferenceKind.cs` (new).
- **Description:** Define conversation/run/action/reference statuses and kinds matching the state machines.
- **Acceptance:** Two ABOUTME lines; no dependencies outside Domain; supports `CreateEventDraft` action and `Event` reference.
- **Dependencies:** 0.1.
- **Effort:** S
- **Validation:** Domain compile.

#### Task 1.2: Create AI aggregate entities

- **Type:** create
- **Layer:** Domain
- **Files:** `AiConversation.cs`, `AiMessage.cs`, `AiRun.cs`, `AiConversationReference.cs`, `AiProposedAction.cs`, `AiToolExecution.cs` under `Explore.Domain/Ai/` (new).
- **Description:** Model tenant/actor ownership, ordered messages (`long` sequence), run lifecycle, selected references, action payload JSON, confirmation/rejection/execution metadata, result resource IDs, timestamps.
- **Acceptance:** Valid transitions enforced; `Guid` aggregate IDs; no Application/Infrastructure dependency; raw content fields are explicit for retention/privacy.
- **Dependencies:** 1.1.
- **Effort:** L
- **Validation:** Domain tests.

#### Task 1.3: Add domain lifecycle tests

- **Type:** test
- **Layer:** Domain Tests
- **Files:** `Event.Domain.UnitTests/Ai/AiConversationTests.cs`, `AiProposedActionTests.cs` (new).
- **Description:** Test message ordering, active run invariant, proposed → confirmed/rejected/executed/failed transitions, invalid transition failures.
- **Acceptance:** Tests require no database/DI and cover failure paths.
- **Dependencies:** 1.2.
- **Effort:** M
- **Validation:** `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`.

#### Task 1.4: Add EF mappings, DbSets, filters, indexes

- **Type:** create/modify
- **Layer:** Persistence
- **Files:** `ExploreDbContext.DbSets.cs`, `ExploreDbContext.QueryFilters.cs`, new `Ai*Configuration.cs` files.
- **Description:** Map AI tables explicitly with indexes on tenant/actor/conversation/status/sequence/created date and JSON payload storage strategy.
- **Acceptance:** Tenant filters preserved; cascade behavior explicit; payload storage documented; indexes support history and pending action queries.
- **Dependencies:** 1.2.
- **Effort:** L
- **Validation:** Persistence tests compile/apply migration.

#### Task 1.5: Add AI conversation repository

- **Type:** create/modify
- **Layer:** Application/Persistence
- **Files:** `Explore.Application/Contracts/Persistence/IAiConversationRepository.cs`, `Explore.Persistence/Repositories/AiConversationRepository.cs`, `PersistenceServicesRegistration.cs`.
- **Description:** Provide create/update/get/list/get-action methods returning entities only with cancellation tokens and cursor pagination.
- **Acceptance:** No DTO projection from repository; tenant-filtered queries; no `IgnoreQueryFilters` without explicit test.
- **Dependencies:** 1.4.
- **Effort:** M
- **Validation:** Persistence repository tests.

#### Task 1.6: Generate migration and update schema docs

- **Type:** create/docs
- **Layer:** Persistence/Docs
- **Files:** new AI migration, `ExploreDbContextModelSnapshot.cs`, `schemas/islamu-event.md`.
- **Description:** Add AI tables in a focused migration after verifying current migration state.
- **Acceptance:** Migration is AI-scoped, applies in integration tests, schema docs explain tables.
- **Dependencies:** 1.4.
- **Effort:** M
- **Validation:** Persistence integration tests.

#### Task 1.7: Add persistence tests

- **Type:** test
- **Layer:** Persistence Tests
- **Files:** `Event.Persistence.IntegrationTests/Repositories/AiConversationRepositoryTests.cs` (new).
- **Description:** Test create/list/get/update, message order, action retrieval, cursor pagination, and tenant isolation.
- **Acceptance:** Cross-tenant records not returned; ordering deterministic.
- **Dependencies:** 1.5, 1.6.
- **Effort:** M
- **Validation:** Persistence integration tests.

### Phase 2: Provider, Configuration, Bootstrap

- **Goal:** Add provider/model configuration, provider validation/health/telemetry, egress safety, and a server bootstrap that exposes availability without secrets.
- **Depends on:** Phase 0 provider decision; can overlap with Phase 1 after contracts are stable.
- **Relevant files:** existing AI settings files, new Application provider contracts, new Infrastructure provider settings/adapters, `InfrastructureServicesRegistration.cs`, config docs.
- **Acceptance criteria:** Secrets stay server-side; bootstrap returns allowed models/features/limits/links; fake provider supports deterministic tests; provider endpoint/config validation and safe health/metrics/logging exist; config docs updated.

#### Task 2.1: Extend AI settings

- **Type:** modify/docs
- **Layer:** Domain/Application/Docs
- **Files:** `GovernanceSettingKeys.cs`, `AiAssistantSettingDefinitions.cs`, `AiAssistantSettingGroup.cs`, `docs/CONFIGURATION.md`.
- **Description:** Add provider type, default/allowed models, timeout, token/context limits, retention days, tools enabled, create-event-draft enabled, streaming flag, rate/concurrency limits.
- **Acceptance:** Sensitive values marked sensitive; safe defaults; tenant governance behavior documented.
- **Dependencies:** 0.3.
- **Effort:** M
- **Validation:** setting registry tests.

#### Task 2.2: Define Application provider contracts

- **Type:** create
- **Layer:** Application
- **Files:** `Explore.Application/Contracts/Infrastructure/Ai/IAiChatProvider.cs`, `IAiModelCatalog.cs`, `AiChatRequest.cs`, `AiChatResponse.cs`, error/result models (new).
- **Description:** Provider-neutral request/response contracts with model, system instructions, messages, structured action schema hint, cancellation, timeout, usage metadata.
- **Acceptance:** No provider SDK types; fake provider can implement; supports assistant text and structured action JSON.
- **Dependencies:** 0.3.
- **Effort:** M
- **Validation:** Application build.

#### Task 2.3: Add provider settings validator and fake provider

- **Type:** create/test
- **Layer:** Infrastructure/Tests
- **Files:** `Explore.Infrastructure/Ai/AiProviderSettings.cs`, `AiProviderSettingsValidator.cs`, fake provider, `Explore.Infrastructure.Tests/...AiProviderSettingsValidatorTests.cs`.
- **Description:** Validate endpoint/model/key requirements and provide deterministic fake responses for tests/dev.
- **Acceptance:** Invalid config detected; fake provider returns known event-draft proposal; no secrets logged.
- **Dependencies:** 2.1, 2.2.
- **Effort:** M
- **Validation:** `dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`.

#### Task 2.4: Implement OpenAI-compatible provider adapter

- **Type:** create
- **Layer:** Infrastructure
- **Files:** `Explore.Infrastructure/Ai/OpenAiCompatibleChatProvider.cs`, related models/registration.
- **Description:** HTTP-client or approved SDK adapter with configurable endpoint/model/key, timeout, cancellation, safe error mapping.
- **Acceptance:** Uses `IHttpClientFactory` or approved SDK pattern; no prompt/content logging; maps rate-limit/auth/unavailable/timeouts to safe errors.
- **Dependencies:** 2.2, 2.3.
- **Effort:** L
- **Validation:** Infrastructure tests with fake HTTP handler.

#### Task 2.5: Add bootstrap query and DTO

- **Type:** create
- **Layer:** Application/API
- **Files:** `GetAiAssistantBootstrapQuery.cs`, handler, `AiAssistantBootstrapDto.cs` (new).
- **Description:** Return enabled state, default/allowed models, feature flags, limits, disabled reason, and links without secrets.
- **Acceptance:** Respects tenant settings/auth policy; disabled assistant returns no write links; model list is tenant-approved.
- **Dependencies:** 2.1, 2.2.
- **Effort:** M
- **Validation:** Application/API tests.

#### Task 2.6: Add provider health, egress safety, and safe telemetry

- **Type:** create/test/docs
- **Layer:** Infrastructure/API/Operations
- **Files:** provider settings validator/health check/telemetry docs, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`.
- **Description:** Validate configured endpoint scheme/ownership, reject blank/unsafe non-local URLs, ensure provider URLs are never browser/per-request-controlled, expose disabled/misconfigured/unavailable states, and emit low-cardinality metrics/logs without prompts/secrets.
- **Acceptance:** Disabled is healthy-disabled; misconfigured/unavailable states are visible; unsafe endpoints fail validation/startup or settings save; no prompt/content/secrets appear in logs; docs updated.
- **Dependencies:** 2.1-2.4.
- **Effort:** M
- **Validation:** Infrastructure/API tests.

### Phase 3: Conversations, Runs, History, API Contract

- **Goal:** Expose secure conversation APIs and MediatR handlers for persisted chat runs.
- **Depends on:** Phases 1 and 2.
- **Relevant files:** new `AiAssistantController`, route names, AI DTOs/commands/queries/handlers, AI HAL policy, OpenAPI/generated client.
- **Acceptance criteria:** Bootstrap/list/detail/create/send/run-status endpoints exist; cancellation is omitted until Phase 7 unless fully cancellable provider execution is implemented; writes authorized; private history authenticated; send/confirm idempotency supported; HAL/ProblemDetails/API tests pass.

#### Task 3.1: Add AI DTOs

- **Type:** create
- **Layer:** Application/API contract
- **Files:** `Explore.Application/DTOs/AiAssistant/*` (new).
- **Description:** Add bootstrap, conversation, message, run, reference, proposed action, create conversation, send message, and confirm/reject request DTOs.
- **Acceptance:** No secrets/internal errors; supports history and action cards; cursor pagination consistent.
- **Dependencies:** Phase 1.
- **Effort:** M
- **Validation:** Application/API build.

#### Task 3.2: Add conversation commands/queries/handlers

- **Type:** create
- **Layer:** Application
- **Files:** `Explore.Application/Features/AiAssistant/Requests/{Commands,Queries}/*`, handlers.
- **Description:** Implement create conversation, list/detail, send message, run status, archive if included. `SendAiMessageCommandHandler` persists user message/run, builds prompt, calls provider, persists assistant response/actions.
- **Acceptance:** Uses manual validators; disabled/invalid model/active run/provider failure handled; one active run per conversation; send-message idempotency prevents duplicate runs; no event created during send.
- **Dependencies:** 2.2, 2.5, 3.1.
- **Effort:** XL
- **Validation:** Application unit tests.

#### Task 3.3: Add prompt builder and structured action parser

- **Type:** create/test
- **Layer:** Application
- **Files:** `AiPromptContextBuilder.cs`, `AiSystemPromptFactory.cs`, `AiStructuredActionParser.cs` under `Features/AiAssistant/Prompting/`.
- **Description:** Compose bounded system/user/reference context and parse provider JSON into typed proposals.
- **Acceptance:** Enforces context limits; separates user/reference content boundaries; rejects unknown actions/fields/invalid JSON; tells model to propose, not execute.
- **Dependencies:** 2.2, 3.1.
- **Effort:** L
- **Validation:** Application parser/prompt tests.

#### Task 3.4: Add AI API controller/routes

- **Type:** create/modify
- **Layer:** API
- **Files:** `Explore.API/Controllers/AiAssistantController.cs`, `Explore.API/Routes/RouteNames.cs`.
- **Description:** Add thin endpoints for bootstrap, conversations, messages/runs, and run status. Use MediatR, endpoint classification, route names, ProblemDetails, response types. Defer cancel endpoint to Phase 7 unless full provider cancellation exists.
- **Acceptance:** Write endpoints `[Authorize]`; private history GET endpoints authenticated and documented as security exception; public bootstrap, if present, exposes safe capability metadata only; no business/provider logic in controller.
- **Dependencies:** 3.1, 3.2.
- **Effort:** L
- **Validation:** API integration tests.

#### Task 3.5: Add AI HAL policy

- **Type:** create/test
- **Layer:** API/HATEOAS
- **Files:** `Explore.API/Hateoas/Policies/AiAssistantLinkPolicy.cs`, registration files.
- **Description:** Emit links for self, history, send, confirm/reject (later Phase 5), conversation navigation, and result navigation. Add cancel link only when Phase 7 cancellation exists.
- **Acceptance:** Links reflect auth, tenant, feature flags, and state; tests cover absence of forbidden links.
- **Dependencies:** 3.4.
- **Effort:** M
- **Validation:** API HATEOAS tests.

#### Task 3.6: Add application/API tests

- **Type:** test
- **Layer:** Application/API Tests
- **Files:** new `Event.Application.UnitTests/Features/AiAssistant/*`, `Event.API.IntegrationTests/Features/AiAssistant*Tests.cs`.
- **Description:** Test fake-provider chat flow, auth, disabled assistant, provider failure, idempotency, private-history auth, cross-tenant access, HAL links, and public/private bootstrap split.
- **Acceptance:** No real provider calls; no event side effect before confirmation; private history/bootstrap anonymous requests fail; safe ProblemDetails.
- **Dependencies:** 3.2-3.5.
- **Effort:** L
- **Validation:** Application/API test commands.

#### Task 3.7: Update OpenAPI/generated client/changelog

- **Type:** modify/docs
- **Layer:** API Contract/Blazor
- **Files:** `schemas/openapi.json`, `Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CHANGELOG.md`, maybe `docs/API.md`.
- **Description:** Regenerate or update API contract and generated client using repo workflow.
- **Acceptance:** Operation IDs stable; generated AI methods available; changelog describes auth/idempotency.
- **Dependencies:** 3.4.
- **Effort:** M
- **Validation:** build + API tests.

#### Task 3.8: Add AI authorization catalog and endpoint safety tests

- **Type:** create/modify/test/docs
- **Layer:** Application/API/Authorization
- **Files:** `AuthorizationActions`, `ResourceKinds`, `ResourceDescriptors`, Cerbos/local policy tests, API endpoint classification tests, docs.
- **Description:** Register AI resource kinds/actions and prove local/Cerbos parity for assistant bootstrap, conversations, runs, references, and proposed actions.
- **Acceptance:** Private GETs are `[Authorize]`/authenticated endpoint class; public bootstrap is safe if split; no anonymous history; resource/action parity tests cover local and Cerbos modes.
- **Dependencies:** 3.4-3.5.
- **Effort:** M
- **Validation:** API/security/architecture tests.

#### Task 3.9: Add MVP abuse, bounds, and retention gate

- **Type:** create/modify/test/docs
- **Layer:** API/Application/Configuration
- **Files:** AI settings, rate limiting policy/API config, handlers/tests, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`.
- **Description:** Enforce per-user/per-tenant/concurrent-run limits, prompt length, selected reference count, model/tool allow-lists, retained-history/redaction posture, and no-content logging before broad enablement.
- **Acceptance:** Excess requests return safe ProblemDetails; broad enablement is blocked until limits and retention policy are configured; settings and operations docs updated.
- **Dependencies:** 2.1, 2.6, 3.2-3.6.
- **Effort:** L
- **Validation:** API/Application/Operations tests.

### Phase 4: Event Reference Search And Prompt Packing

- **Goal:** Let users reference events safely through server-authorized search and bounded prompt context.
- **Depends on:** Phase 3 contract foundation.
- **Relevant files:** `IEventRepository`, `EventRepository`, AI reference DTO/query/handler, API endpoint, prompt packer, Blazor reference picker later.
- **Acceptance criteria:** Search/select event references; cross-tenant isolation; prompt packer rehydrates server-side; HAL links in results.

#### Task 4.1: Add reference DTO/query contracts

- **Type:** create
- **Layer:** Application
- **Files:** `AiReferenceSearchResultDto.cs`, `AiSelectedReferenceDto.cs`, `SearchAiReferencesQuery.cs`.
- **Description:** Define event-first reference search result and selected-reference request shape extensible to pages/sessions later.
- **Acceptance:** Result includes kind, resource ID, title, snippet, metadata, links; no full event content.
- **Dependencies:** 3.1.
- **Effort:** M
- **Validation:** Application build.

#### Task 4.2: Add entity-returning event reference query

- **Type:** modify/test
- **Layer:** Application/Persistence
- **Files:** `IEventRepository.cs`, `EventRepository.cs`, optional specification.
- **Description:** Add bounded event search for AI references while returning entities and preserving tenant filters.
- **Acceptance:** No DTOs from repository; deterministic sorting; indexed/bounded; no untested `IgnoreQueryFilters`.
- **Dependencies:** 4.1.
- **Effort:** M
- **Validation:** Persistence integration tests.

#### Task 4.3: Implement reference search handler/API endpoint

- **Type:** create/modify
- **Layer:** Application/API
- **Files:** handler, `AiAssistantController.cs`, AI HAL policy.
- **Description:** Map authorized event entities to lightweight reference results and links.
- **Acceptance:** Search limits enforced; cross-tenant results absent; event links present when allowed.
- **Dependencies:** 4.2, 3.4.
- **Effort:** M
- **Validation:** API tests.

#### Task 4.4: Add reference prompt packer

- **Type:** create/test
- **Layer:** Application
- **Files:** `AiReferencePromptPacker.cs`, tests.
- **Description:** Convert server-rehydrated events to bounded prompt summaries with clear content boundaries.
- **Acceptance:** Max per-reference and total budgets enforced; sensitive/internal fields excluded; prompt-injection-like content safely quoted.
- **Dependencies:** 4.2.
- **Effort:** M
- **Validation:** Application unit tests.

### Phase 5: Confirmed Create Event Draft Tool

- **Goal:** Implement first real AI action: propose and confirm event draft creation through existing event command.
- **Depends on:** Phases 1-4 and re-reading active event creation plan.
- **Relevant files:** event DTO/command/handler, new AI action payload/mapper/commands/HAL/API/tests.
- **Acceptance criteria:** AI can propose create draft; no draft created before confirmation; confirmation creates draft via `CreateEventCommand`; reject has no side effect; duplicate confirm idempotent; result links are HAL-gated.

#### Task 5.1: Define `CreateEventDraft` action payload/mapper

- **Type:** create/test
- **Layer:** Application
- **Files:** `CreateEventDraftAiActionPayload.cs`, `CreateEventDraftAiActionMapper.cs`, parser tests.
- **Description:** Allow only safe draft fields and map to existing event draft/create request.
- **Acceptance:** Cannot set tenant/actor/published status/privileged fields; unknown fields rejected or ignored by explicit policy; org/group IDs revalidated.
- **Dependencies:** 3.3, 0.4.
- **Effort:** M
- **Validation:** Application tests.

#### Task 5.2: Add confirm/reject commands/handlers

- **Type:** create/test
- **Layer:** Application
- **Files:** `ConfirmAiProposedActionCommand.cs`, `RejectAiProposedActionCommand.cs`, handlers.
- **Description:** Confirm rehydrates action, validates state, authorizes operation, sends `CreateEventCommand`, persists execution result. Reject only updates state.
- **Acceptance:** Duplicate confirmation safe; event authorization enforced; failed execution recorded safely; no direct repository event insert.
- **Dependencies:** 5.1.
- **Effort:** L
- **Validation:** Application tests.

#### Task 5.3: Add confirm/reject API and HAL links

- **Type:** modify/test
- **Layer:** API/HATEOAS
- **Files:** `AiAssistantController.cs`, `AiAssistantLinkPolicy.cs`, `RouteNames.cs`, API tests.
- **Description:** Add authorized action endpoints and state-aware `confirm`/`reject` links; confirm supports idempotency.
- **Acceptance:** ProblemDetails for stale/forbidden/disabled/invalid actions; links absent when not allowed; changelog updated.
- **Dependencies:** 5.2.
- **Effort:** M
- **Validation:** API tests.

#### Task 5.4: Add end-to-end fake-provider API tests

- **Type:** test
- **Layer:** API/Application Tests
- **Files:** `AiAssistantCreateEventDraftFlowTests.cs` (new or extend existing).
- **Description:** Fake provider proposes draft, user confirms, event draft created; reject and duplicate confirm cases tested.
- **Acceptance:** Created event has draft status; send-message creates no event; duplicate confirm creates one event; validation/auth failures safe.
- **Dependencies:** 5.1-5.3.
- **Effort:** L
- **Validation:** Application/API tests.

### Phase 6: Blazor Right-Side Panel UX

- **Goal:** Replace placeholder rail with functional MudBlazor assistant UI inside existing dock.
- **Depends on:** API/generated client through Phase 5.
- **Relevant files:** `AiAssistantRail.razor*`, `AiAssistantState.cs`, service registrations, new `Components/AiAssistant/*`, new client service/tests.
- **Acceptance criteria:** User can bootstrap, list/resume conversations, choose model, select references, send prompts, confirm/reject action cards, and open result links; existing dock tests pass.

#### Task 6.1: Add Blazor AI client service

- **Type:** create/modify/test
- **Layer:** Blazor Client
- **Files:** `IAiAssistantClientService.cs`, `AiAssistantClientService.cs`, `ServiceCollectionExtensions.cs`.
- **Description:** Wrap generated client methods for bootstrap, conversations, reference search, send, confirm, and reject, with error handling and idempotency key generation. Add cancel only if Phase 7 cancellation exists.
- **Acceptance:** Components do not use ad-hoc raw HTTP; service handles API exceptions; provider secrets absent.
- **Dependencies:** 3.7, 5.3.
- **Effort:** M
- **Validation:** Blazor service tests.

#### Task 6.2: Extend AI client state safely

- **Type:** modify/test
- **Layer:** Blazor Client
- **Files:** `AiAssistantState.cs` and/or new `AiAssistantConversationState.cs`.
- **Description:** Track selected conversation/model/references/loading/errors while preserving existing open/availability behavior.
- **Acceptance:** Existing state tests pass; no local authz decisions; state updates trigger UI without leaks.
- **Dependencies:** 6.1.
- **Effort:** M
- **Validation:** `AiAssistantStateTests`/new tests.

#### Task 6.3: Build assistant rail layout/components

- **Type:** modify/create/test
- **Layer:** Blazor UI
- **Files:** `AiAssistantRail.razor*`, new `AiAssistantHeader.razor`, `AiConversationList.razor`, `AiMessageList.razor`, `AiPromptComposer.razor`.
- **Description:** Implement header/model selector/history/message list/composer/loading/error/disabled states using MudBlazor and CSS isolation.
- **Acceptance:** BEM-like isolated CSS; keyboard and ARIA labels; docked/fixed modes preserved.
- **Dependencies:** 6.1, 6.2.
- **Effort:** L
- **Validation:** bUnit tests + manual smoke.

#### Task 6.4: Build event reference picker

- **Type:** create/test
- **Layer:** Blazor UI
- **Files:** `AiReferencePicker.razor`, `AiReferenceChip.razor`, CSS/test files.
- **Description:** Debounced search/select/remove references, render selected events as chips/cards, submit IDs with prompt.
- **Acceptance:** Loading/empty/error states; keyboard-removable chips; no full event data stored beyond DTO.
- **Dependencies:** Phase 4, 6.1.
- **Effort:** M
- **Validation:** bUnit tests.

#### Task 6.5: Build proposed action/result cards

- **Type:** create/test
- **Layer:** Blazor UI
- **Files:** `AiProposedActionCard.razor`, `CreateEventDraftActionPreview.razor`, `AiActionResultCard.razor`.
- **Description:** Render event draft proposal, warnings/rationale, Confirm/Reject buttons from HAL links, and result/open links after execution.
- **Acceptance:** Buttons only render when corresponding HAL link exists; double submit prevented; proposal visually distinct from committed data.
- **Dependencies:** Phase 5, 6.1.
- **Effort:** L
- **Validation:** bUnit HAL-gating tests.

#### Task 6.6: Add full Blazor panel tests

- **Type:** test
- **Layer:** Blazor Tests
- **Files:** `Explore.Blazor.Client.Tests/Components/AiAssistant/*` plus existing layout tests if needed.
- **Description:** Test bootstrap states, history, model selection, reference picker, send flow, action cards, confirm/reject, disabled/error states.
- **Acceptance:** Existing dock bridge tests pass; HAL link absence hides buttons.
- **Dependencies:** 6.1-6.5.
- **Effort:** L
- **Validation:** `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`.

### Phase 7: Streaming, Cancellation, Advanced Operations

- **Goal:** Add optional streaming, true cancellation, and post-MVP operational refinements after the safe MVP gate works.
- **Depends on:** Phases 3-6.
- **Acceptance criteria:** Cancellation and streaming decisions are tested; advanced tuning builds on Phase 2/3 safety gate.

#### Task 7.1: Add cancellation semantics

- **Type:** modify/test
- **Layer:** Application/API/Infrastructure/Blazor
- **Files:** AI run handlers/controller/provider/client components.
- **Description:** Add endpoint/API/UI cancellation if not already implemented, allow cancellable runs, persist `Cancelled`, pass cancellation tokens to provider, hide actions from cancelled runs.
- **Acceptance:** Cancel link only for cancellable runs; UI updates status; tests cover cancellation.
- **Dependencies:** Phase 3.
- **Effort:** M
- **Validation:** Application/API/Blazor tests.

#### Task 7.2: Decide streaming vs polling

- **Type:** investigate/modify
- **Layer:** API/Blazor/Infrastructure
- **Files:** TBD after decision.
- **Description:** Choose SSE, SignalR, or polling based on Blazor InteractiveAuto/BFF/self-hosting constraints.
- **Acceptance:** Decision documented; transport respects auth/tenant isolation; non-streaming fallback remains.
- **Dependencies:** Core flow complete.
- **Effort:** M-XL
- **Validation:** API/Blazor tests and manual smoke.

#### Task 7.3: Tune quotas and retention cleanup after MVP gate

- **Type:** modify/test
- **Layer:** API/Application/Configuration
- **Files:** AI settings, API policy/config, cleanup job/handler/tests.
- **Description:** Tune already-enforced Phase 3 quotas, add cleanup automation if not already present, and refine retention operations based on usage data.
- **Acceptance:** Existing MVP limits remain enforced; cleanup is safe, tenant-scoped, documented, and observable.
- **Dependencies:** 3.9.
- **Effort:** M
- **Validation:** API tests.

#### Task 7.4: Add advanced provider/run dashboards and runbook polish

- **Type:** create/docs/test
- **Layer:** Infrastructure/API/Operations
- **Files:** metrics dashboards/runbook docs, `docs/OPERATIONS.md`, troubleshooting docs if present.
- **Description:** Build on Phase 2 health/metrics/logging with operator dashboards, runbook polish, and provider-specific troubleshooting.
- **Acceptance:** No secrets/content in logs; dashboards use low-cardinality dimensions; runbooks cover disabled/misconfigured/unavailable/rate-limited states.
- **Dependencies:** 2.6, provider adapter complete.
- **Effort:** M
- **Validation:** Infrastructure/API tests.

### Phase 8: Documentation, Credit, Final Validation

- **Goal:** Bring docs, schema, API, README credit, and dev docs to release quality.
- **Depends on:** Implementation phases.
- **Acceptance criteria:** Docs explain setup/contract/privacy/retention/UI/ops; README credits Plane; all required tests pass; dev docs match reality.

#### Task 8.1: Update API docs and changelog

- **Type:** docs
- **Layer:** Docs/API
- **Files:** `docs/API.md`, `docs/API_CHANGELOG.md`, `schemas/openapi.json`.
- **Acceptance:** Endpoints/auth/idempotency/HAL/ProblemDetails documented.
- **Dependencies:** API endpoints complete.
- **Effort:** M
- **Validation:** API tests/build.

#### Task 8.2: Update configuration/self-hosting/ops docs

- **Type:** docs
- **Layer:** Docs/Operations
- **Files:** `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, Aspire/Docker docs if touched.
- **Acceptance:** Self-hosters can configure/disable provider, set models/limits/retention, and diagnose failures.
- **Dependencies:** Provider/config complete.
- **Effort:** M
- **Validation:** docs review/build.

#### Task 8.3: Update Blazor/dock/accessibility docs

- **Type:** docs
- **Layer:** Docs/Blazor
- **Files:** `docs/BLAZOR.md`, `docs/DOCK_LAYOUT.md`, `docs/ACCESSIBILITY.md` if needed.
- **Acceptance:** Docs match component/service names and interaction model.
- **Dependencies:** Blazor implementation.
- **Effort:** S-M
- **Validation:** Blazor tests/manual smoke.

#### Task 8.4: Add Plane inspiration credit

- **Type:** docs
- **Layer:** Docs/README
- **Files:** `README.md`, optional AI docs/comments.
- **Acceptance:** Credit Plane as AGPL-compatible inspiration without claiming copied code.
- **Dependencies:** implementation sufficiently real to describe.
- **Effort:** S
- **Validation:** docs review.

#### Task 8.5: Final validation and dev-doc refresh

- **Type:** test/docs
- **Layer:** All
- **Files:** all changed files plus `dev/active/ai-integration/*`.
- **Acceptance:** Required build/tests pass; plan/context/tasks reflect actual completed/deferred state.
- **Dependencies:** all implementation tasks.
- **Effort:** M-L
- **Validation:** commands in section 14.

## 7. Testing Strategy

### 7.1 Requirement-to-test mapping

| Requirement | Unit tests | Integration tests | UI tests |
|---|---|---|---|
| AI domain lifecycle | `Event.Domain.UnitTests/Ai/*` | Persistence repository tests | N/A |
| Provider config/adapter | Application/provider tests, `Explore.Infrastructure.Tests` | Health/config API tests | Bootstrap disabled/error states |
| Conversation/history | Handler tests | API conversation tests, tenant isolation | Rail history rendering |
| Send message/run | Prompt/parser/handler tests | API fake-provider flow | Composer loading/error states |
| Event references | Prompt packer tests | Repository/API search tests | Reference picker tests |
| Create event draft action | Mapper/confirm handler tests | API confirm/reject/idempotency tests | HAL-gated action card tests |
| Security/HAL | Auth handler tests | HATEOAS/cross-tenant tests | Button absence when links absent |
| Operations | Settings validator tests | Health/rate-limit tests | Disabled/provider-error UI |

### 7.2 Required validation commands

```bash
dotnet build --configuration Release --verbosity quiet

dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet

dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet

dotnet test --project Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet

dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet

dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet

dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet

dotnet test --project Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj --configuration Release --verbosity quiet

dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

If targeted TUnit runs are needed, use `--treenode-filter` per `docs/TESTING.md` and `.claude/rules/tests.md`; do not use VSTest `--filter`.

### 7.3 Fake-provider strategy

- All deterministic tests must use a fake provider.
- CI must not require real AI credentials or network access.
- Provider adapter tests use fake HTTP handlers.
- Fake provider should return known assistant text and known `CreateEventDraft` action JSON.
- Negative tests must cover disabled assistant, invalid model, stale action, rejected/executed action, provider timeout, cross-tenant references, and unauthorized event creation.

## 8. Documentation, Configuration, And Operations Impact

Docs/config to update during implementation:

- `docs/API_CHANGELOG.md` and likely `docs/API.md` for endpoint contract.
- `schemas/openapi.json` and generated `EventApiClient.g.cs`.
- `docs/CONFIGURATION.md` for provider/model/secrets/limits/retention/tool settings.
- `docs/SELF_HOSTING.md` and `docs/OPERATIONS.md` for provider setup, health checks, troubleshooting, and validation.
- `docs/DOCK_LAYOUT.md`, `docs/BLAZOR.md`, and accessibility docs for panel behavior.
- `docs/SECURITY-MODEL.md`, `docs/AUTHORIZATION.md`, and `docs/MULTI_TENANCY.md` if AI-specific policies/resources are added.
- `schemas/islamu-event.md` for AI tables.
- `README.md` for Plane inspiration credit.
- `dev/active/ai-integration/*` throughout implementation.

Potential new/refined settings:

- `ai_assistant.enabled`
- `ai_assistant.allow_anonymous_access` (must not expose private history/tools unless explicitly designed)
- `ai_assistant.provider`
- `ai_assistant.endpoint_url`
- `ai_assistant.api_key` or secret reference
- `ai_assistant.default_model`
- `ai_assistant.allowed_models`
- `ai_assistant.max_prompt_context_chars`
- `ai_assistant.max_output_tokens`
- `ai_assistant.request_timeout_seconds`
- `ai_assistant.retention_days`
- `ai_assistant.enable_tools`
- `ai_assistant.enable_create_event_draft_tool`
- `ai_assistant.enable_streaming`
- `ai_assistant.per_user_rate_limit`
- `ai_assistant.per_tenant_rate_limit`
- `ai_assistant.max_concurrent_runs_per_user`

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Conversation history is private; require auth even for GET endpoints that expose private AI data.
- Bootstrap must be split if needed: public bootstrap can expose safe capability metadata only; authenticated bootstrap/history/actions are `[Authorize]` and classified as authenticated endpoints.
- Public/anonymous AI, if allowed, must be a separate restricted mode with no private history and no write tools unless user explicitly approves.
- Confirming a `CreateEventDraft` action must authorize the underlying Event create operation through existing MediatR authorization behavior.
- Reference search must be tenant-filtered and authorization-aware.
- Prompt packing must rehydrate references server-side and include only bounded, allowed data.
- Provider credentials never reach Blazor, API responses, logs, or generated clients.
- Provider endpoint URLs are deployment/admin-controlled only; validate HTTPS or documented local exceptions, and never accept browser/per-request outbound hosts.
- Logs/metrics/traces must not include raw prompts/responses/reference content by default.
- Provider output is untrusted. Only allow-listed typed actions can be persisted and only confirmed actions can execute side effects.
- Automatic provider/tool invocation for mutating tools is disabled in the first release; model tool calls may only become persisted proposals.
- AI resource kinds/actions must be registered in the authorization catalogs and parity-tested for local and Cerbos modes.
- Retention/redaction policy is required before persistent history is broadly enabled.
- Idempotency is required for event-creating confirmation and send-message.
- Rate limits, prompt/reference limits, and concurrent-run limits are required before broad enablement.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, Product Considerations

| Concern | Applicability | Plan |
|---|---|---|
| Multi-tenancy | Applicable | Tenant-scoped conversations/references/actions, filters, model config, quotas, cross-tenant tests. |
| Federation | Needs investigation | First release should limit references to local tenant events; future federated references must preserve origin/visibility. |
| Localization | Applicable | UI strings follow existing localization; prompt includes user/tenant locale/timezone; generated text may match user language. |
| Accessibility | Applicable | Keyboard navigation, focus, ARIA labels/live status, accessible action cards, screen-reader-friendly loading/errors. |
| Cultural filtering | Applicable/future | Existing validation applies; future prompt constraints can include cultural policies. |
| Prayer-relative scheduling | Future | First tool avoids sessions/schedules beyond basic draft fields; future session tools must use existing prayer-relative model. |
| Spatial discovery | Future | Do not add PostGIS reference scope in first release unless needed. |
| Self-hosting | Core | OpenAI-compatible/local provider path, no browser secrets, disable-by-default safe mode, docs/health checks. |

## 11. Observability And Operations

- **Logs:** structured lifecycle logs with run/action IDs, provider/model, status, duration, failure code; no prompt content or secrets.
- **Metrics:** conversations created, messages sent, runs by status, run duration, token usage if available, action proposals/confirmations/failures, reference searches, provider failures.
- **Tracing:** API request → MediatR handler → provider call → persistence updates; external spans without content.
- **Health:** disabled = healthy disabled; misconfigured = unhealthy or degraded according to ops convention; provider unavailable/rate-limited visible without secrets.
- **Troubleshooting:** document disabled assistant, missing key, invalid model, provider auth/rate-limit/timeout, stale action, validation failure, and missing Event create permission.
- Minimal health/metrics/logging are Phase 2/3 MVP gates, not Phase 7 cleanup.

## 12. Migration And Compatibility Plan

- Add focused EF migration for AI tables after checking current migration state.
- Update `ExploreDbContextModelSnapshot.cs` and `schemas/islamu-event.md`.
- Add indexes for tenant/actor/conversation/status/sequence/created date.
- No data backfill required because no AI tables exist.
- No backward-compatible API shims required in development mode, but route names, operation IDs, API changelog, and generated client still matter.
- Deployment sequence: apply migration, deploy disabled/fake provider safe default, configure provider/secrets/models, verify health/metrics/quotas/retention/auth tests, then enable assistant/tools for approved tenants, monitor metrics, and only then enable streaming/advanced operations.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Wrong provider abstraction/package choice. | Medium | High | Internal `IAiChatProvider`, Context7/official docs refresh. | Build/package/adapter test friction. | 0.3, 2.2, 2.4 |
| AI bypasses auth/confirmation. | Low if plan followed | Critical | Typed proposals, HAL, confirm handler reuses `CreateEventCommand`. | Event exists after send-only test. | 5.2, 5.4 |
| Automatic tool invocation bypasses confirmation. | Medium | Critical | Disable automatic mutating invocation; persist proposals only. | Provider/tool tests mutate during send. | 5a, 3.3, 5.2 |
| Private conversation GET accidentally anonymous. | Medium | Critical | Explicit auth exception and tests. | Anonymous history test succeeds. | 3.4, 3.6 |
| Provider endpoint becomes SSRF/egress bypass. | Medium | Critical | Admin/deployment-controlled endpoint, validation, no request-controlled hosts. | Unsafe endpoint accepted or provider host from payload. | 2.6 |
| Abuse/cost controls arrive too late. | Medium | Critical | Rate/concurrency/prompt/model/tool limits before broad enablement. | High-volume send test succeeds past quota. | 3.9 |
| Prompt injection produces unsafe action. | Medium | High | Prompt boundaries, strict parser, allow-list, confirmation. | Parser tests fail/unknown action accepted. | 3.3, 5.1 |
| Duplicate draft on retry/double-click. | Medium | High | Idempotency + action execution result. | Duplicate confirm test creates two events. | 5.2, 5.4 |
| Cross-tenant data leak. | Low/Medium | Critical | Tenant filters, server rehydration, cross-tenant tests. | Reference search includes other tenant event. | 4.2-4.4 |
| UI shows forbidden buttons. | Medium | High | HAL-only buttons, bUnit link absence tests. | Confirm button visible without link. | 5.3, 6.5 |
| Retention/redaction deferred past launch. | Medium | High | MVP retention/redaction policy before broad enablement; cleanup automation can follow. | Persistent history enabled without retention setting/docs. | 3.9, 8.2 |
| DB grows from conversation history. | High | Medium | Retention settings, cleanup plan, docs. | Storage growth/ops alert. | 2.1, 3.9, 8.2 |
| Streaming delays useful release. | Medium | Medium | Non-streaming first; streaming Phase 7. | Scope creep before Phase 5/6. | 7.2 |

## 14. Success Metrics And Definition Of Done

Functional DoD:

- Existing AI dock opens and renders functional assistant.
- User can create/resume persisted conversation.
- User can select tenant-approved model.
- User can search/select event references.
- User can ask AI for an event draft.
- AI returns persisted `CreateEventDraft` proposed action.
- Confirmation card clearly previews fields and requires user action.
- Confirm creates draft through `CreateEventCommand`; reject creates nothing.
- Conversation history shows message, references, model, proposal, confirmation/rejection, and result.
- Result links are HAL-gated.

Quality DoD:

- All new source files have two ABOUTME comments.
- Clean Architecture boundaries hold.
- Repositories return entities.
- Validators are manually instantiated.
- Controllers are thin and route-named.
- Provider secrets stay server-side.
- No prompt/content in normal logs.
- Fake provider is used in tests.
- MVP safety gate is complete before broad enablement.
- Send-message and confirm idempotency tests pass.
- AI auth/resource parity and private GET auth tests pass.
- Provider endpoint validation, health, metrics, and logs are safe.
- API/OpenAPI/generated client/docs/schema/dev docs are updated.
- Required build/test commands pass or failures are documented with recovery tasks.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Future agents implementing this plan MUST:

1. Read this plan, `ai-integration-context.md`, and `ai-integration-tasks.md` before editing.
2. Re-read `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, matched intents, and path-scoped rules for files being changed.
3. Start from the highest-priority incomplete task unless the user overrides it.
4. Record baseline build/status before first code edit.
5. Update this plan if scope/architecture/risks change.
6. Update `ai-integration-context.md` after each meaningful slice with files changed, decisions, blockers, validation, and next step.
7. Check off/update `ai-integration-tasks.md` immediately after completing tasks.
8. Never report “done” unless docs match actual state.
9. If validation fails, record failure/root cause/recovery in context/tasks.
10. Before handoff or PR, refresh all three dev docs.

## 16. Progress Reporting Contract

Implementation summaries must include:

- **Implemented:** developer teaching summary naming patterns, libraries/infrastructure, files/classes, and data/control flow.
- **Verified:** exact commands/checks run.
- **Remaining:** unchecked tasks/risks.
- **Next:** next task from tasks file.
- **Docs updated:** whether plan/context/tasks were updated.

Do not write only “AI integration implemented.” The user must understand the architecture from the summary.

## 17. Potential Risks & Unknowns

The hardest part is not the chat UI; it is making AI actions safe. Model output must be treated as untrusted proposal data. It should be persisted, rendered through a HAL-gated confirmation card, and executed only through existing MediatR commands such as `CreateEventCommand`. Any shortcut that directly inserts events from AI JSON or shows buttons from local role checks will violate the platform contract.

The second major unknown is provider abstraction. AI packages and provider capabilities change quickly, and self-hosters may use OpenAI, enterprise gateways, local OpenAI-compatible proxies, or future local models. The internal Application contract plus Infrastructure adapters keeps this swappable, but Phase 0 must refresh docs before committing to dependencies.

The third risk is privacy/retention. Persisted conversation history is required by the desired product experience, but it creates storage and privacy obligations. Add retention settings and no-content logging from the beginning, even if automatic cleanup is deferred.
