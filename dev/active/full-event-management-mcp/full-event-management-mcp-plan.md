<!-- ABOUTME: Implementation plan for extending the API-hosted MCP adapter into a full event-management surface. -->
<!-- ABOUTME: Captures current evidence, design decisions, phases, risks, and verification for future implementation agents. -->

# Full Event-Management MCP — Implementation Plan

Last Updated: 2026-06-08 Europe/Brussels

## 0. Planning Metadata

- **Request:** Write a repository-grounded implementation plan for bringing full API capabilities to MCP so external agents can manage events through the ISLAMU Event MCP adapter.
- **Task directory:** `dev/active/full-event-management-mcp/`
- **Planning status:** Draft — user review required before implementation.
- **Matched intents:** Composite match because MCP is an API adapter, not an OpenAPI controller group.
  - `add-get-endpoint` — read/list/detail MCP capabilities over existing event queries.
  - `add-write-endpoint` — write-like MCP tools, but proposal-first unless a new ADR approves direct mutation.
  - `add-cqrs-handler` — only if missing query/command slices are needed.
  - `update-repository-query` — only if current event queries cannot support MCP parity safely.
  - `add-hal-link` — MCP must preserve HAL affordance authority for management actions.
  - `openapi-contract-change` — only if implementation changes the public REST/OpenAPI surface.
  - `cerbos-policy-change` — only if new resource permissions or widened policies are required.
- **Relevant skills loaded:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`, `dotnet-efcore-guidelines`, `mcp-csharp-create`, `mcp-csharp-test`, `mcp-csharp-debug`.
- **Relevant rules loaded:** `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`, `.claude/rules/application-layer.md`, `.claude/rules/domain.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/tests.md`.
- **Primary layers touched:** API/MCP adapter, Application/CQRS, tests, docs. Persistence only if existing queries are insufficient. Domain only if new domain behavior is discovered; not expected for the first slices.
- **Estimated complexity:** XL. The OpenAPI contract contains a large event-management surface (event, sessions, agenda, groups, templates, registrations, custom properties, team, aspects, publish lifecycle). MCP must preserve tenant isolation, HAL authorization, API-key scopes, rate limiting, proposal/confirmation safety, redaction, and tests.
- **Context7 documentation used:** Official Model Context Protocol C# SDK docs via Context7 confirmed `AddMcpServer()`, `WithHttpTransport(options => options.Stateless = true)`, `MapMcp()`, explicit `WithTools<T>()`/`WithResources<T>()`/`WithPrompts<T>()`, `[McpServerTool]`, `[McpServerResource]`, `[McpServerPrompt]`, `[Description]`, `AddAuthorizationFilters()`, and stateless-per-request options. ASP.NET Core docs via Context7 confirmed middleware/auth/rate-limiting ordering and WebApplicationFactory-style HTTP integration testing.
- **Tavily note:** Tavily MCP was requested in the broader workstream, but tool discovery in this session did not expose a Tavily MCP/tool. This plan therefore uses repository evidence plus official Context7 documentation.

## 1. Executive Summary

The current MCP adapter is healthy and production-shaped, but it is intentionally small. Anonymous clients can discover the AI tool registry (`list_ai_tool_contracts`), and scoped clients can use AI conversation resources plus proposal-first tools such as `propose_create_event_draft`. It does **not** yet expose event list, event detail, event session management, publish readiness, publish, update, delete, or registration workflows as first-class MCP capabilities.

This plan proposes turning MCP into a curated **event-management adapter** over existing REST/CQRS/HAL authority. It must not become an OpenAPI auto-import, a repository mutation layer, or an authorization bypass. The safe default is:

1. add bounded public event read tools/resources that match anonymous REST visibility;
2. add authenticated event management resources that expose HAL-derived affordances and concurrency data;
3. add proposal-first event management tools for create/update/publish/delete/sub-resource changes;
4. keep direct write execution out of scope unless the user explicitly approves a new ADR that changes the current proposal-first MCP invariant.

Out of scope for this default plan: product stdio hosting, stateful sessions, legacy SSE runtime transport, server-to-client MCP features, arbitrary controller reflection, direct repository writes from MCP, and committing local MCP client secrets.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Product MCP is hosted inside `Explore.API` with stateless Streamable HTTP. | `Explore.API/Program.cs` registers `AddMcpServer().WithHttpTransport(options => options.Stateless = mcpAdapterSettings.Stateless)` and maps `app.MapMcp(effectiveMcpAdapterSettings.EndpointPath).AllowAnonymous()`. | High | Matches Context7 official C# SDK guidance. |
| MCP registration is explicit, not assembly-scanned. | `Explore.API/Program.cs` uses `WithTools<AiToolRegistryMcpTools>()`, `WithTools<AiAssistantMcpTools>()`, `WithResources<AiAssistantMcpResources>()`, `WithPrompts<AiAssistantMcpPrompts>()`. | High | Keep this pattern for event MCP types. |
| SDK authorization filters are enabled. | `Explore.API/Program.cs` calls `.AddAuthorizationFilters()`. | High | Enables `[AllowAnonymous]`/`[Authorize]` metadata on MCP primitives. |
| Anonymous MCP surface is currently only safe registry discovery. | `Explore.API/Mcp/AiToolRegistryMcpTools.cs` exposes `[AllowAnonymous] list_ai_tool_contracts`; `Event.API.IntegrationTests/Features/McpAuthorizationTests.cs` asserts anonymous/blank/invalid keys do not see proposal tools. | High | User curl also showed only `list_ai_tool_contracts`. |
| Scoped MCP surface currently includes AI conversations and proposal tools. | `Explore.API/Mcp/AiAssistantMcpTools.cs`, `AiAssistantMcpResources.cs`, `AiAssistantMcpPrompts.cs`, `AiMcpProjectedToolFactory.cs`; `McpProtocolContractTests.cs` expects `propose_ai_tool_action`, `propose_create_event_draft`, `ai_conversations`, `ai_conversation_detail`, prompt. | High | All scoped mutations are proposal-only. |
| Mutating MCP tools must remain proposal-first today. | `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/MCP_DEBUGGING.md`, `docs/adr/ADR-010-mcp-adapter-hosting-strategy.md`; `McpProtocolContractTests.cs` asserts proposal calls do not create events. | High | Direct write MCP requires an explicit design decision/ADR. |
| Event REST API already has core event management endpoints. | `Explore.API/Controllers/EventController.cs`; OpenAPI paths `/api/event`, `/api/event/{id}`, `/api/event/{id}/publish`, `/api/event/{id}/status`, `/api/event/{id}/aspects/*`, `/api/event/{id}/program-summary`, etc. | High | These are the primary behavior sources to mirror/adapt. |
| Event API uses CQRS handlers and repositories that return entities. | `Explore.Application/Features/Events/**`, `IEventRepository.cs`, `EventRepository.cs`. | High | MCP should call MediatR requests, not repositories. |
| Event list supports filtering, public visibility, module-conditional aspect filters, custom property projection filters, caching, and tenant tags. | `GetEventListRequest.cs`, `GetEventListRequestHandler.cs`, `EventQuerySpecification`, `EventRepository.GetEventsWithDetailsPaged(...)`. | High | MCP public search should reuse this instead of inventing queries. |
| Event details enforce visibility for archived/draft events. | `GetEventDetailsRequestHandler.cs` returns null for archived; draft is only visible to creator. | High | MCP detail must use this handler to preserve visibility. |
| Event writes already have command handlers. | `CreateEventCommandHandler.cs`, `UpdateEventDraftCommandHandler.cs`, `PublishEventCommandHandler.cs`, `DeleteEventCommandHandler.cs`, aspect command handlers. | High | Proposal tools should map to these contracts through existing AI/proposal flow or MediatR confirmation. |
| HAL event affordances exist and are authorization-aware. | `Explore.API/Hateoas/Policies/EventLinkPolicy.cs` emits edit, delete, publish, readiness, add-session, team, subscription links with permissions. | High | MCP management context must derive action availability from HAL links or equivalent link policy evaluation. |
| Existing API-key scopes include `mcp:read` and `mcp:propose`; event scopes are separate. | `docs/API.md` scope model; `Explore.API/Extensions/AuthenticationExtensions.cs` policies. | High | Full event MCP likely needs combined MCP + event scopes for private event reads/writes. |
| Event-related REST/OpenAPI surface is much larger than `EventController`. | `schemas/openapi.json` lists event sessions, agenda items, groups, custom properties, templates, registrations, team, sync endpoints. | High | “Full API capabilities” must be phased by domain area. |
| Related active work exists under ATCR. | `dev/active/ai-tool-contract-registry/*`, especially Phase 13.5 pending anonymous-safe MCP read tools/resources. | High | This plan should extend, not contradict, ATCR Phase 13.5. |

### 2.2 Existing Implementation By Layer

#### API / MCP adapter

- `Explore.API/Program.cs` owns MCP service registration and endpoint mapping.
- `Explore.API/Configuration/McpAdapterSettings.cs` and `Explore.API/Extensions/ConfigurationExtensions.cs` own default startup configuration and Infisical compatibility mapping.
- `Explore.API/Middleware/McpRuntimeGateMiddleware.cs` gates the mapped path at runtime through governance settings.
- `Explore.API/Mcp/AiToolRegistryMcpTools.cs` exposes anonymous-safe registry metadata.
- `Explore.API/Mcp/AiAssistantMcpTools.cs` exposes generic proposal creation through MediatR.
- `Explore.API/Mcp/AiMcpProjectedToolFactory.cs` creates projected `propose_*` tools from ATCR definitions.
- `Explore.API/Mcp/AiAssistantMcpResources.cs` exposes safe AI conversation metadata for scoped principals.
- `Explore.API/Mcp/AiAssistantMcpPrompts.cs` exposes proposal workflow guidance.
- `Explore.API/Mcp/McpAuthorizationPolicies.cs` names `mcp_read` and `mcp_propose` policies.
- `Explore.API/Mcp/McpAdapterTelemetry.cs` bounds known MCP telemetry dimensions.

#### API / Event REST and HAL

- `Explore.API/Controllers/EventController.cs` owns the main event REST endpoints:
  - anonymous event list/detail/calendar/program/aspect reads;
  - authenticated `my`, creation context, session-create context, publish readiness;
  - authenticated create, update draft, update status, publish, delete, and aspect upserts/deletes.
- `Explore.API/Hateoas/Assemblers/EventResourceAssembler.cs` assembles HAL resources.
- `Explore.API/Hateoas/Policies/EventLinkPolicy.cs` is the action-affordance authority for event detail/collection resources.
- `Explore.API/Hateoas/RouteNames.cs` contains stable route names used by controllers and HAL links.

#### Application / CQRS

- `Explore.Application/Features/Events/Requests/Queries/*` and handlers support event list, details, my events, creation context, session-create context, program summary, publish readiness, calendar export.
- `Explore.Application/Features/Events/Requests/Commands/*` and handlers support create, update draft/status, publish, delete.
- `Explore.Application/Features/EventSessions`, `EventSessionGroups`, `EventAgendaItems`, `EventDays`, `EventCustomProperties`, `EventRegistrations`, `EventTemplates`, and sync features already exist for sub-resource management.
- `Explore.Application/Features/AiAssistant/Tools/*` currently defines `CreateEventDraft` as the registry-backed MCP-projected proposal tool.

#### Persistence

- `Explore.Application/Contracts/Persistence/IEventRepository.cs` returns `Explore.Domain.Event` entities, not DTOs.
- `Explore.Persistence/Repositories/EventRepository.cs` implements public list/detail/search queries with explicit includes/specifications.
- Event query/specification and projection tests already cover much of the query behavior.

#### Docs / operations

- `docs/API.md`, `docs/CONFIGURATION.md`, `docs/OPERATIONS.md`, `docs/MCP_DEBUGGING.md`, `docs/AI_AGENT_EXPERIENCE_HARDENING.md`, `docs/adr/ADR-010-mcp-adapter-hosting-strategy.md`, and `docs/adr/ADR-011-local-mcp-stdio-diagnostic-host.md` describe the current MCP posture.

### 2.3 Existing Tests And Verification Coverage

Verified MCP tests:

- `Event.API.IntegrationTests/Features/McpProtocolContractTests.cs` — protocol initialize/discovery/call coverage, proposal-only assertions, redaction, disabled endpoint.
- `Event.API.IntegrationTests/Features/McpAuthorizationTests.cs` — anonymous, blank key, invalid/revoked key, scoped key, tenant mismatch, rate-limit partitioning, runtime gate.
- `Event.API.IntegrationTests/Features/McpSdkContractTests.cs` — SDK attribute/registration/source posture.
- `Event.API.IntegrationTests/Features/McpProjectedToolTests.cs` — registry-to-MCP projection.
- `Event.API.IntegrationTests/Features/McpAdapterHealthCheckTests.cs`, `McpRuntimeStateServiceTests.cs`, `McpAdapterSettingsValidatorTests.cs`, `McpAiToolRegistryTests.cs`, `McpAiAssistantAdapterTests.cs`.

Verified event/API tests:

- `Event.API.IntegrationTests/Features/EventsControllerTests.cs`, `EventControllerRealRuntimeTests.cs`, `EventVisibilityContractTests.cs`, `AuthFamilyEventControllerTests.cs`.
- `Event.API.IntegrationTests/Features/Hateoas/EventHateoasTests.cs`, `EventHateoasScenarioTests.cs`.
- `Event.Application.UnitTests/Features/Events/**` command/query/validator tests.
- `Event.Persistence.IntegrationTests/Repositories/EventRepositoryTests.cs`, `EventQuerySpecificationTests.cs`, `EventAiReferenceRepositoryTests.cs`, projection tests.

Missing for full event MCP:

- No `EventManagementMcpTools`/`EventManagementMcpResources` tests exist yet.
- No API-vs-MCP event visibility parity tests exist yet.
- No MCP tests for event list/detail/my-events/creation-context/publish-readiness/sessions exist yet.
- No MCP event-management proposal tests beyond `CreateEventDraft` exist yet.

### 2.4 Existing Documentation And Contracts

- `docs/API.md` describes middleware order, rate limiting, HATEOAS, MCP, API-key scopes, and operational endpoints.
- `docs/API_CONTRACT_INVENTORY.md` lists event API operations.
- `schemas/openapi.json` contains the generated OpenAPI surface; event-related operations include event, event sessions, agenda items, groups, templates, registrations, team, sync, custom properties, and lookups.
- `docs/API_CHANGELOG.md` records intentional API contract changes.
- `docs/AUTHORIZATION.md` describes endpoint, MediatR, runtime provider, and HAL authorization.
- `docs/MCP_DEBUGGING.md` is the local/manual MCP smoke runbook.
- `docs/CONFIGURATION.md` documents `Mcp:*` and `MCP_*` defaults.
- `docs/OPERATIONS.md` documents MCP rollback, smoke, redaction, and protocol evolution gates.
- ADR-010 and ADR-011 establish API-hosted stateless HTTP and defer stdio.

### 2.5 Current Pain Points / Improvement Areas

1. **MCP is not an event-management MCP yet.** Current tool discovery only exposes registry discovery anonymously and proposal/conversation primitives for scoped clients. There is no first-class MCP event search/detail/my-events/management context.
2. **“Full API capabilities” is too large for one safe change.** OpenAPI shows dozens of event-adjacent endpoints. A direct mirror risks oversized schemas, unstable client UX, and poor authorization reviewability.
3. **No API-vs-MCP parity harness exists.** Future public reads must prove the same visibility as REST, especially for drafts, archived events, tenant scope, custom property projection filters, module-gated aspect filters, and private resources.
4. **Write semantics are intentionally proposal-first.** Existing docs/tests forbid direct MCP mutation. Full event management must either keep proposal-first writes or obtain explicit ADR/user approval for direct execution.
5. **HAL action authority must be preserved.** UI clients gate action affordances by `_links`. MCP tools should not infer edit/delete/publish permissions from roles or local claims.
6. **Scope semantics need refinement.** Existing `mcp:read` and `mcp:propose` are AI-conversation oriented. Private event reads through MCP should require an MCP scope plus domain event authority, not silently widen `mcp:read` into event management.
7. **Telemetry/redaction needs a broader allow-list.** `McpAdapterTelemetry` currently normalizes only known MCP tool names. New event tools/resources need bounded names and failure codes before exposure.
8. **Tool schema size can become unmanageable.** Directly exposing full `CreateEventRequest` plus sessions/custom properties/templates as one MCP schema may exceed practical client UX. Use focused tools and staged payloads.
9. **Local TLS client pain is not product failure.** Curl `-k` proves the endpoint works locally; production should use publicly trusted TLS. The implementation plan should not add insecure production workarounds.

### 2.6 Unknowns After Investigation

| Unknown | What was searched/read | Resolution task |
|---|---|---|
| Whether the user wants direct write execution via MCP or proposal-first “full management”. | Current docs/ADR/tests all require proposal-first. User asked “full API capabilities” but did not explicitly approve direct mutation. | Phase 0 requires user decision/ADR. Default plan keeps proposal-first writes. |
| Exact first “full” scope boundary. | OpenAPI event paths and EventController were inspected. The event surface is broad. | Phase 1 creates a capability matrix and implementation order. |
| Whether current AI proposed-action infrastructure can represent update/publish/delete/session changes cleanly. | Existing `CreateEventDraft` registry path verified; update/publish/delete registry definitions not found in current evidence. | Phase 4 either adds ATCR definitions/action mappers or records gaps. |
| Whether MCP should return HAL JSON directly or smaller MCP descriptors with HAL affordances embedded. | HAL assembler/policy verified. | Phase 2/3 prototype one resource and standardize response shape. |
| Whether API keys should require `events:read/write` in addition to `mcp:*`. | Current scope model verified; `mcp:*` does not grant event authority. | Phase 3/6 designs and tests combined scope policy. |
| How much of event sessions/agenda/templates/registrations counts as “full event management”. | OpenAPI inventory verified. | Phase 5 slices sub-resources by product priority after review. |

## 3. Proposed Future State

### 3.1 Target Capability Model

The event MCP surface should be explicit, small enough to reason about, and layered by risk:

#### Anonymous/public read capabilities

- `search_public_events` tool — paginated, bounded search/filter wrapper over `GetEventListRequest`; only returns public discoverable event summary data.
- `get_public_event` tool or resource template `islamu-event://events/{eventId}` — wrapper over `GetEventDetailsRequest`; respects draft/archived/private visibility.
- `get_event_program_summary` tool/resource — wrapper over `GetEventProgramSummaryRequest` for public program summary.
- `list_event_sessions` / `get_event_session` public read tools after parity tests for session visibility.

#### Authenticated event-management read capabilities

- `list_my_events` tool/resource — wrapper over `GetMyEventsRequest`, requires a scoped principal.
- `get_event_creation_context` tool/resource — wrapper over `GetEventCreationContextRequest`.
- `get_event_management_context` resource — returns a bounded management descriptor for a specific event: event id/title/status, concurrency stamp, HAL-derived available actions, publish readiness link state, and important related resource URIs.
- `get_event_publish_readiness` tool/resource — wrapper over `GetEventPublishReadinessRequest`.

#### Proposal-first mutation capabilities

- `propose_create_event_draft` already exists through ATCR and should be kept.
- Add registry-backed proposal tools, phased by risk:
  - `propose_update_event_draft`
  - `propose_publish_event`
  - `propose_delete_event`
  - `propose_upsert_event_islamic_aspect`
  - `propose_upsert_event_tech_aspect`
  - `propose_create_event_session`
  - `propose_update_event_session`
  - `propose_delete_event_session`
  - later: groups, days, agenda items, custom property values, registration workflows, template sync.
- Each proposal tool validates against an ATCR schema, persists an `AiProposedAction`, and waits for normal product/API confirmation. It must not call repositories directly.

#### Optional direct-write capability

Direct MCP writes are **not** part of the default plan. If the user wants “full API capabilities” to mean “MCP can execute writes immediately,” create a new ADR first. That ADR must define tool approval semantics, idempotency keys, API-key `mcp:write` plus domain scopes, confirmation bypass rules, audit records, replay behavior, rate limits, and rollback.

### 3.2 Architecture Flow

```text
MCP client
  -> POST /mcp Streamable HTTP
  -> API auth conflict guard + tenant middleware + runtime MCP gate
  -> MCP SDK authorization filters
  -> EventManagementMcpTools/Resources/Prompts
  -> MediatR query/command or ATCR proposal command
  -> Existing Application authorization behavior + handlers
  -> Existing repositories/domain/outbox/cache
  -> MCP descriptor JSON with bounded fields, HAL-derived actions, redacted errors
```

Rules:

- Reads call existing MediatR queries; they do not query EF repositories from MCP.
- Writes call proposal commands by default; they do not call mutation commands unless a future ADR approves direct writes.
- HAL link policies remain the action availability source for management descriptors.
- MCP-specific DTO/descriptor classes live in `Explore.API/Mcp` unless Application needs a reusable non-MCP query DTO.
- Application and Domain must not reference `ModelContextProtocol`.

### 3.3 Suggested File Shape

- `Explore.API/Mcp/EventManagementMcpTools.cs`
- `Explore.API/Mcp/EventManagementMcpResources.cs`
- `Explore.API/Mcp/EventManagementMcpPrompts.cs`
- `Explore.API/Mcp/EventManagementMcpDescriptors.cs`
- `Explore.API/Mcp/EventManagementMcpJsonContext.cs`
- `Explore.API/Mcp/EventManagementMcpAuthorization.cs` or additions to existing policies if needed.
- `Explore.Application/Features/AiAssistant/Tools/*` additions for new proposal definitions/action mappers.
- `Event.API.IntegrationTests/Features/EventManagementMcp*.cs` for protocol, auth, parity, redaction.
- `Event.Application.UnitTests/Features/AiAssistant/Tools/*` for registry definitions/mappers.

## 4. Non-Negotiable Constraints

- Repositories return entities, never DTOs.
- Validators are manually instantiated.
- Use `int` for lookups, `Guid` for aggregates, `long` for cursors.
- GET REST endpoints are `[AllowAnonymous]`; write REST endpoints are `[Authorize]`.
- Every new file starts with two `ABOUTME:` lines.
- HAL links are the single source of truth for UI and management affordances; MCP must not recreate action gates from roles/claims.
- Domain/Application cannot depend on MCP SDK types.
- MCP SDK registration remains explicit; do not add `WithToolsFromAssembly()` to the product host.
- Product MCP remains API-hosted stateless Streamable HTTP at the startup path; no stdio, stateful session, or legacy SSE runtime behavior in this work.
- Anonymous-safe MCP reads must match existing public API visibility and rate limits.
- Mutating MCP tools remain registry-backed and proposal-first unless an explicit ADR/user approval changes that invariant.
- No tool/resource response may include API keys, bearer tokens, provider endpoint URLs, prompts, raw provider responses, raw exceptions, tenant IDs in diagnostics, or unbounded payloads.

## 5. Architecture And Design Decisions

### Decision 1: Curated MCP adapter, not OpenAPI auto-generation

- **Why:** The event API has a large and security-sensitive surface. Auto-importing controllers would bypass careful HAL/scopes/proposal safety and produce poor tool schemas.
- **Alternatives considered:** Generate tools from OpenAPI; expose a generic `call_api` tool; reflect controllers. Rejected because they weaken reviewability and invite auth/tenant mistakes.
- **Consequences:** More manual work, but safer slices and better client UX.
- **Files/layers affected:** API/MCP, tests, docs.

### Decision 2: Reuse MediatR and HAL; never call repositories from MCP

- **Why:** Existing handlers enforce validation, caching, authorization behavior, tenant context, outbox, and domain invariants. HAL policies encode affordances.
- **Alternatives considered:** Repository reads from MCP for speed; duplicated permission checks in MCP. Rejected by repo conventions and HAL invariant.
- **Consequences:** MCP behavior stays aligned with REST/API behavior; tests can compare REST and MCP outputs.
- **Files/layers affected:** API/MCP calls Application; Application/Persistence remain unchanged unless missing behavior is discovered.

### Decision 3: Proposal-first writes by default

- **Why:** ADR-010, docs, and tests require mutating MCP tools to create proposed actions only. This is the safe enterprise pattern for external agents.
- **Alternatives considered:** Directly call `CreateEventCommand`, `UpdateEventDraftCommand`, `PublishEventCommand`, etc. from MCP. Deferred pending ADR/user approval.
- **Consequences:** “Full management” means agents can draft and propose all event changes, while humans/product confirmation executes side effects.
- **Files/layers affected:** ATCR definitions, proposal mappers, API/MCP projected tools, AI assistant confirmation flow tests.

### Decision 4: Public reads first, private management second, writes third

- **Why:** Public read parity is lower risk and proves response shape/testing before adding state-changing proposals.
- **Alternatives considered:** Start with direct create/update tools. Rejected because tool schemas and auth semantics are not yet proven.
- **Consequences:** Incremental value: event search/detail appears quickly; full management follows safely.
- **Files/layers affected:** Event MCP tools/resources and tests.

### Decision 5: Combined MCP + domain scopes for authenticated event MCP

- **Why:** `mcp:read`/`mcp:propose` currently protects MCP protocol surfaces; it should not silently grant private event read/write authority.
- **Alternatives considered:** Let `mcp:read` access all private event reads. Rejected as too broad.
- **Consequences:** API keys need explicit MCP scope plus relevant domain authority/scopes; bearer users rely on normal user authority.
- **Files/layers affected:** auth policies, external API-key scope tests, MCP auth tests.

### Decision 6: Return bounded descriptors with HAL affordance summaries

- **Why:** Raw HAL resources can be large and unstable for agents, but action availability must derive from HAL. A bounded descriptor with a normalized `actions` list preserves authority and usability.
- **Alternatives considered:** Return entire REST HAL payloads; return custom action booleans. Full HAL may be heavy; booleans can drift from HAL. Prototype should compare both.
- **Consequences:** Implementation must map HAL `_links` to MCP action descriptors and test parity.
- **Files/layers affected:** `EventManagementMcpDescriptors`, HAL assembler use, parity tests.

## 6. Implementation Phases

### Phase 0: User Review And Boundary Decision

- **Goal:** Confirm what “full event-management MCP” means before writing code.
- **Deliverables:** Approved scope matrix, direct-write decision, first milestone selection.
- **Acceptance criteria:** User explicitly accepts proposal-first writes as the default or requests an ADR for direct writes.
- **Validation:** Docs-only review.

### Phase 1: Event API-to-MCP Capability Matrix

- **Goal:** Inventory event REST operations and classify MCP exposure.
- **Work:** Create a matrix from `schemas/openapi.json`, `EventController.cs`, event session/group/agenda/custom-property/template/registration/team controllers.
- **Classifications:** public read, authenticated read, proposal mutation, direct mutation deferred, not suitable for MCP, needs ADR.
- **Acceptance criteria:** Every event-adjacent endpoint has a planned MCP status and required auth/scope/tenant behavior.
- **Validation:** Review against `docs/API_CONTRACT_INVENTORY.md` and OpenAPI.

### Phase 2: Public Event Read MCP Surface

- **Goal:** Add anonymous-safe event discovery and detail capabilities.
- **Work:** Implement `search_public_events`, `get_public_event`, and optional public program/session reads via MediatR.
- **Acceptance criteria:** Anonymous/no-key/blank-key/invalid-key clients can read only data that REST public endpoints would expose; drafts/archived/private content remain hidden; page size is bounded.
- **Validation:** New API integration parity tests compare REST `/api/event` and `/api/event/{id}` outcomes with MCP tool/resource outputs.

### Phase 3: Authenticated Event Management Read Context

- **Goal:** Add scoped management reads without mutations.
- **Work:** Implement `list_my_events`, `get_event_creation_context`, `get_event_publish_readiness`, `get_event_management_context`.
- **Acceptance criteria:** Scoped users/API keys see only authorized management context; actions are derived from HAL links; event concurrency stamp/readiness state is available for later proposals.
- **Validation:** MCP auth tests for no key, `mcp:read` only, domain event-scope combinations, bearer user, tenant mismatch, HAL parity.

### Phase 4: Proposal-First Event Mutation Tools

- **Goal:** Expand ATCR/MCP proposal tools from create-draft to full core event lifecycle proposals.
- **Work:** Add registry definitions/action mappers and projected tools for update draft, publish, delete, and event aspects.
- **Acceptance criteria:** Tools validate schema, reject forbidden fields such as tenant/actor/server-owned state, persist `AiProposedAction`, do not create/update/delete/publish events until product confirmation, and include safe failure codes.
- **Validation:** Unit tests for definitions/mappers; MCP protocol tests proving proposed-action persistence and zero direct event side effects.

### Phase 5: Event Sub-Resource Management Slices

- **Goal:** Extend management to the rest of the event domain in prioritized vertical slices.
- **Slices:** sessions, session groups, event days, agenda items, custom property definitions/values, registrations, team assignments, templates/template-sync.
- **Acceptance criteria:** Each slice has read parity, HAL-derived management context, proposal-first mutation tools, and tests before exposure.
- **Validation:** Slice-specific API/MCP parity and proposal tests.

### Phase 6: Authorization, Scope, Tenant, Rate-Limit, And Redaction Hardening

- **Goal:** Make event MCP safe for production/self-hosting.
- **Work:** Extend scope policies if needed, add combined MCP/domain authority tests, update telemetry allow-list, add bounded failure codes, ensure rate-limit partitions and tenant fail-closed behavior for new tools.
- **Acceptance criteria:** No private data exposure through anonymous or invalid-key paths; valid keys are scoped; logs/metrics/health omit secrets and high-cardinality payloads.
- **Validation:** Focused MCP authorization/rate-limit/redaction integration tests, External API Key tests, Architecture tests.

### Phase 7: Docs, Runbooks, Evals, And Production Readiness

- **Goal:** Update developer/operator guidance and CI-safe verification.
- **Work:** Update `docs/MCP_DEBUGGING.md`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/API.md`, `docs/AI_AGENT_EXPERIENCE_HARDENING.md`, and active docs.
- **Acceptance criteria:** Manual smoke instructions list event MCP tools/resources, forbid direct mutation claims, document production TLS expectations, and include rollback steps.
- **Validation:** `git diff --check`, focused docs/readiness tests if available, Release build when code changes exist.

## 7. Test Strategy

Minimum tests by slice:

- **Public read MCP:** `Event.API.IntegrationTests` with JSON-RPC/WebApplicationFactory; compare REST and MCP visibility. Include anonymous/no-key/blank-key/invalid-key/tenant mismatch. Include draft/private/archived cases.
- **Authenticated read MCP:** API-key scope matrix tests (`mcp:read`, `events:read`, both, neither), bearer user tests, HAL action parity tests.
- **Proposal tools:** Application unit tests for tool definitions/action mappers; API integration tests for `tools/list`, `tools/call`, hidden-field rejection, proposed-action persistence, and no direct event side effects.
- **Sub-resources:** per-slice API/MCP parity tests, especially sessions/agenda/custom-property projections.
- **Redaction:** Assert MCP errors do not echo prompts, payload secrets, raw exceptions, API keys, bearer tokens, endpoint URLs, or tenant/user identifiers.
- **Architecture:** `Event.Architecture.Tests` to ensure Application/Domain do not reference MCP SDK and new files follow conventions.

Suggested focused commands during implementation:

```bash
dotnet test Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*Mcp*/*|/*/*/*Event*/*" --no-progress --maximum-parallel-tests 1
dotnet test Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet --no-restore -- --treenode-filter "/*/*/*AiAssistant*/*|/*/*/*Events*/*" --no-progress --maximum-parallel-tests 1
dotnet test Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --no-restore -- --no-progress --maximum-parallel-tests 1
dotnet build --configuration Release --verbosity quiet --no-restore
git diff --check
```

## 8. Documentation Impact

Update these as implementation lands:

- `docs/MCP_DEBUGGING.md` — event-management tool/resource smoke flows.
- `docs/OPERATIONS.md` — production MCP event-management posture, rollback, redaction.
- `docs/CONFIGURATION.md` — scopes and governance notes if extended.
- `docs/API.md` — MCP adapter capability summary, not OpenAPI controller docs.
- `docs/API_CHANGELOG.md` — only if REST/OpenAPI contracts change.
- `docs/API_CONTRACT_INVENTORY.md` / `schemas/openapi.json` — only if REST contracts change.
- `docs/AI_AGENT_EXPERIENCE_HARDENING.md` — external agent event-management workflow and forbidden artifact rules.
- `dev/active/full-event-management-mcp/*` — update every implementation slice.
- `dev/active/ai-tool-contract-registry/*` — update only if ATCR Phase 13.5 or registry work is directly modified.

## 9. Risks And Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---:|---:|---|
| Direct MCP writes bypass confirmation or HAL. | Medium | High | Default proposal-first. Require ADR/user approval for direct writes. |
| Public MCP reads expose drafts/private/archived events. | Medium | High | Reuse MediatR queries; add REST-vs-MCP parity tests. |
| Tool schemas become too large for clients. | High | Medium | Use focused tools and bounded descriptors; avoid one mega-tool. |
| API-key `mcp:read` accidentally becomes event read authority. | Medium | High | Require combined MCP + domain scopes/permissions for private reads. |
| HAL affordances drift from MCP action descriptors. | Medium | High | Derive MCP actions from HAL `_links`; test parity. |
| Event sub-resource scope explodes. | High | Medium | Phase by vertical slice; keep matrix and tasks updated. |
| New telemetry leaks payloads or tenant data. | Low-Med | High | Add allow-listed tool names/failure codes only; redaction tests. |
| Local MCP client TLS issues distract from implementation. | Medium | Low | Document curl/Inspector smoke and production trusted TLS; do not add insecure production bypass. |
| Existing dirty worktree affects tests. | High | Medium | Record baseline before each slice; do not revert unrelated changes. |

## 10. Recommended Next Step

Review and approve these planning decisions before implementation:

1. Default full event management means **read capabilities + proposal-first mutations**, not direct writes.
2. First implementation milestone should be **Phase 2 public event search/detail MCP** plus parity tests.
3. Private event management should require **MCP scope plus domain event authority**.
4. Direct write MCP should require a separate ADR if desired.
