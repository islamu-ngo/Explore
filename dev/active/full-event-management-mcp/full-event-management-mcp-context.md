<!-- ABOUTME: Resume context for the full event-management MCP workstream. -->
<!-- ABOUTME: Records verified source facts, decisions, unknowns, and commands for future agents. -->

# Full Event-Management MCP — Context

Last Updated: 2026-06-08 Europe/Brussels

## Quick Resume

The repository already has a working API-hosted MCP adapter at `/mcp` using the official C# SDK with stateless Streamable HTTP. The current MCP surface is intentionally small: anonymous-safe `list_ai_tool_contracts`, scoped AI conversation resources, prompt guidance, generic `propose_ai_tool_action`, and projected `propose_create_event_draft`. It does **not** currently expose event list/detail/session/program/publish/update/delete tools/resources.

This workstream plans a full event-management MCP adapter while preserving existing constraints: Clean Architecture, CQRS/MediatR, HAL affordance authority, tenant isolation, API-key scopes, redaction, rate limiting, and proposal-first writes.

## Current Source Facts

### MCP adapter files

- `Explore.API/Program.cs`
  - Registers `AddMcpServer()` with `WithHttpTransport(options => options.Stateless = mcpAdapterSettings.Stateless)`.
  - Calls `.AddAuthorizationFilters()`.
  - Explicitly registers `AiToolRegistryMcpTools`, `AiAssistantMcpTools`, `AiAssistantMcpResources`, `AiAssistantMcpPrompts`.
  - Maps `app.MapMcp(effectiveMcpAdapterSettings.EndpointPath).AllowAnonymous()` when startup enabled.
- `Explore.API/Mcp/AiToolRegistryMcpTools.cs`
  - `[AllowAnonymous]` `list_ai_tool_contracts` only.
- `Explore.API/Mcp/AiAssistantMcpTools.cs`
  - `[Authorize(Policy = McpAuthorizationPolicies.Propose)]` `propose_ai_tool_action`.
- `Explore.API/Mcp/AiMcpProjectedToolFactory.cs`
  - Builds projected `propose_*` tools from `IAiToolContractRegistry` definitions with `ExposeToMcp`.
- `Explore.API/Mcp/AiAssistantMcpResources.cs`
  - Scoped safe AI conversation list/detail metadata.
- `Explore.API/Mcp/AiAssistantMcpPrompts.cs`
  - Scoped create-event-draft proposal workflow prompt.
- `Explore.API/Mcp/McpAuthorizationPolicies.cs`
  - `mcp_read` and `mcp_propose` policies.
- `Explore.API/Mcp/McpAdapterTelemetry.cs`
  - Bounded telemetry for known MCP tools.

### Event API files

- `Explore.API/Controllers/EventController.cs`
  - Main event management REST controller.
  - Public reads: list, detail, calendar, program summary, event aspects.
  - Authenticated reads: my events, creation context, session create context, publish readiness.
  - Authenticated writes: create, update draft, update status, publish, delete, aspect upsert/delete.
- `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
  - Emits event management HAL links such as edit, delete, publish, publish-readiness, add-session, session-create-context, team, subscriptions.
  - This is the action availability authority for clients.
- `Explore.API/Hateoas/Assemblers/EventResourceAssembler.cs`
  - Converts event DTOs to HAL resources.
- `Explore.API/Hateoas/RouteNames.cs`
  - Stable route names for event and event sub-resource links.

### Application/CQRS files

- `Explore.Application/Features/Events/Requests/Queries/*`
  - `GetEventListRequest`, `GetEventDetailsRequest`, `GetMyEventsRequest`, `GetEventCreationContextRequest`, `GetEventPublishReadinessRequest`, `GetEventCalendarExportRequest`, etc.
- `Explore.Application/Features/Events/Handlers/Queries/*`
  - Event list applies public discoverability when no status filter is present.
  - Event details hide archived events and drafts from non-creators.
- `Explore.Application/Features/Events/Requests/Commands/*`
  - `CreateEventCommand`, `UpdateEventDraftCommand`, `UpdateEventCommand`, `PublishEventCommand`, `DeleteEventCommand`.
- `Explore.Application/Features/Events/Handlers/Commands/*`
  - Existing command handlers validate, authorize through pipeline attributes, update caches, and publish outbox messages where needed.
- `Explore.Application/Features/AiAssistant/Tools/*`
  - Current ATCR/MCP proposal support for create-event-draft.

### Persistence files

- `Explore.Application/Contracts/Persistence/IEventRepository.cs`
  - Repositories return entities, not DTOs.
  - Supports `GetEventsWithDetailsPaged`, `GetEventWithDetails`, `SearchAiReferenceEventsAsync`, etc.
- `Explore.Persistence/Repositories/EventRepository.cs`
  - Uses explicit EF includes/specification filters and tenant query filters.

### Test files

- MCP: `Event.API.IntegrationTests/Features/McpProtocolContractTests.cs`, `McpAuthorizationTests.cs`, `McpSdkContractTests.cs`, `McpProjectedToolTests.cs`, `McpAiToolRegistryTests.cs`, `McpAiAssistantAdapterTests.cs`, `McpAdapterHealthCheckTests.cs`, `McpRuntimeStateServiceTests.cs`.
- Event API: `Event.API.IntegrationTests/Features/EventsControllerTests.cs`, `EventControllerRealRuntimeTests.cs`, `EventVisibilityContractTests.cs`, `EventVisibilityContractTests.cs`, `Features/Hateoas/EventHateoas*.cs`.
- Event application: `Event.Application.UnitTests/Features/Events/**`.
- Event persistence: `Event.Persistence.IntegrationTests/Repositories/EventRepositoryTests.cs`, `EventQuerySpecificationTests.cs`, `EventAiReferenceRepositoryTests.cs`.

### Docs

- `docs/MCP_DEBUGGING.md` — current local MCP smoke runbook.
- `docs/OPERATIONS.md` — MCP operational contract and rollback.
- `docs/CONFIGURATION.md` — `Mcp:*` and Infisical `MCP_*` defaults.
- `docs/API.md` — middleware order, MCP scope summary, API architecture.
- `docs/AUTHORIZATION.md` — endpoint/MediatR/HAL authorization model.
- `docs/adr/ADR-010-mcp-adapter-hosting-strategy.md` — API-hosted stateless HTTP, proposal-first.
- `docs/adr/ADR-011-local-mcp-stdio-diagnostic-host.md` — product stdio not implemented.
- `dev/active/ai-tool-contract-registry/*` — related MCP/ATCR workstream. Phase 13.5 is still the closest existing task for optional public MCP read resources.

## External Documentation Evidence

- Context7 official C# MCP SDK docs confirmed:
  - ASP.NET Core Streamable HTTP setup uses `AddMcpServer()`, `WithHttpTransport(...)`, and `MapMcp()`.
  - Stateless mode is recommended when no server-to-client/session behavior is needed.
  - `.AddAuthorizationFilters()` enables standard ASP.NET Core authorization attributes on MCP tools/resources/prompts.
  - Explicit `WithTools<T>()`, `WithResources<T>()`, and `WithPrompts<T>()` are valid and avoid assembly scanning.
  - Tools/resources/prompts use `[McpServerTool]`, `[McpServerResource]`, `[McpServerPrompt]`, `[Description]`, and cancellation tokens.
- Context7 ASP.NET Core docs confirmed the normal middleware/auth/rate-limiting order and WebApplicationFactory-style HTTP endpoint testing.
- Tavily MCP/tool was not available in this session after discovery; do not claim Tavily research for this plan.

## Key Decisions To Preserve

1. MCP remains an adapter over API/Application behavior, not a second domain authority.
2. Reads call MediatR queries; MCP does not call repositories directly.
3. Writes stay proposal-first by default; direct write execution needs explicit ADR/user approval.
4. HAL links determine management actions; MCP must not infer action availability from roles/claims.
5. Product MCP remains API-hosted stateless Streamable HTTP; no stdio/stateful/legacy-SSE runtime change.
6. New MCP classes are registered explicitly in `Program.cs`.
7. New files must start with two `ABOUTME:` lines.
8. Public reads require REST-vs-MCP visibility parity tests.
9. Private reads should require MCP scope plus domain event authority/scope.
10. Tool/resource outputs and telemetry must stay bounded and redacted.

## Current User-Observed State

The user confirmed with curl that:

- `POST https://localhost:7039/mcp` with `tools/list` returns HTTP 200 Streamable HTTP/SSE-style response.
- Anonymous/no-key tool list shows only `list_ai_tool_contracts`.
- `initialize` succeeds with protocol version `2025-06-18`.
- `/health` reports `mcp-adapter` healthy, enabled, endpoint `/mcp`, stateless true, legacy SSE runtime false.

This means local product MCP is working; the missing behavior is event-management capability, not endpoint availability.

## Implementation Constraints And Gotchas

- Do not add direct repository dependencies to MCP tools/resources.
- Do not add `WithToolsFromAssembly()` in the product API host.
- Do not expose all OpenAPI operations automatically.
- Do not treat SDK annotations (`ReadOnly`, `Destructive`, etc.) as authorization.
- Do not log/copy raw MCP payloads, prompts, secrets, tenant/user IDs, endpoint URLs, or raw exceptions.
- Do not commit local `.mcp.json` secrets; prior local MCP configs may contain credentials and must be handled carefully.
- Existing worktree is dirty from prior Phase 13/AI work. Do not revert unrelated changes.
- Full event scope includes many sub-resources; keep slices vertical and update docs/tasks after each slice.

## Recommended First Implementation Slice

Implement Phase 2 only:

1. Add `EventManagementMcpTools` with `search_public_events` and `get_public_event`.
2. Register it explicitly in `Explore.API/Program.cs`.
3. Reuse `GetEventListRequest` and `GetEventDetailsRequest` via MediatR.
4. Return bounded event summary/detail descriptors.
5. Add API-vs-MCP parity tests for public, draft, archived, private, tenant mismatch, no-key/blank-key/invalid-key.
6. Update `docs/MCP_DEBUGGING.md`, `docs/OPERATIONS.md`, and this workstream.

## Validation Command Set

Docs-only plan validation used for this planning task:

```bash
git diff --check -- dev/active/full-event-management-mcp/full-event-management-mcp-plan.md dev/active/full-event-management-mcp/full-event-management-mcp-context.md dev/active/full-event-management-mcp/full-event-management-mcp-tasks.md
```

Future implementation validation should include focused MCP/event tests and architecture tests before a full Release build.
