ABOUTME: Decision record for the AI Tool Contract Registry MCP adapter hosting strategy.
ABOUTME: Defines transport, auth, tenancy, disable posture, and registry boundary for Phase 7.

# ADR-010: MCP Adapter Hosting Strategy

- **Status:** Accepted
- **Date:** 2026-06
- **Deciders:** Core team

## Context

The AI Tool Contract Registry is now the Application-layer source of truth for assistant tool definitions, JSON schema, payload validation, confirmation posture, authorization metadata, and execution routing. Product AI uses that registry to emit provider schemas and to validate untrusted proposed actions before persistence.

Phase 7 adds an MCP adapter for external agents. The adapter must not become a second authority for tools, authorization, tenancy, or mutation semantics. It must also stay optional for self-hosters that do not want to expose an agent-facing protocol endpoint.

Current repository evidence:

- No MCP server package or adapter exists in the codebase today.
- `Explore.API` is already the authenticated composition root for HTTP APIs, HAL link generation, rate limiting, tenant resolution, health checks, OpenTelemetry, and operational middleware.
- The official C# MCP SDK provides ASP.NET Core hosting via `ModelContextProtocol.AspNetCore`, `AddMcpServer()`, `WithHttpTransport(...)`, and `MapMcp()`. Current SDK guidance recommends explicitly choosing stateless Streamable HTTP for most HTTP servers, and treats legacy SSE as obsolete/high-risk unless isolated and trusted.
- Phase 8 completed retention cleanup, run polling, run cancellation, and operational guardrails before enabling another AI surface.

## Decision

Host the initial MCP adapter inside `Explore.API` as an optional, disabled-by-default ASP.NET Core endpoint using the official `ModelContextProtocol.AspNetCore` package and stateless Streamable HTTP transport.

The adapter is an API presentation adapter over the existing Application registry and MediatR flows:

1. MCP tool definitions are generated from `IAiToolContractRegistry` definitions.
2. MCP payload validation reuses the registry guard path.
3. Mutating MCP tools default to the same proposal/confirmation model as product AI. The adapter must not insert event data directly through repositories.
4. Tool execution that mutates state must route through existing Application commands/handlers or proposal records.
5. Tenant and principal context must be explicit and fail closed before any registry-backed tool or resource is exposed.

The endpoint is not enabled by default. Self-hosters must opt in through static configuration, and the platform must remain fully functional when MCP is disabled.

## Transport

- Use Streamable HTTP through ASP.NET Core.
- Configure stateless mode explicitly for horizontal scaling and to avoid in-memory MCP session affinity.
- Do not enable legacy SSE for the first implementation. Legacy SSE has weaker request backpressure and should require a separate trusted-isolated deployment decision if ever needed.
- Map the MCP endpoint under an explicit path such as `/mcp` only when enabled.

## Authentication And Tenancy

- The MCP endpoint must require authentication.
- The first implementation should support the same API authentication boundary as other private API surfaces: bearer/API-key-aware API pipeline plus tenant resolution through trusted request context.
- Anonymous MCP access is forbidden.
- Tenant context must be resolved before tools/resources/prompts are listed or executed.
- Fail closed when tenant or principal identity cannot be resolved.
- Do not expose tenant IDs, provider endpoints, model IDs, prompts, tool payloads, API keys, or raw provider errors in MCP metadata, logs, metrics, health data, or error responses.

## Configuration

Add a static `Mcp:*` configuration section for the adapter posture:

| Key | Default | Purpose |
|---|---:|---|
| `Mcp:Enabled` | `false` | Enables mapping the MCP endpoint. Disabled is the default self-hosting posture. |
| `Mcp:EndpointPath` | `/mcp` | Route prefix for Streamable HTTP transport. |
| `Mcp:Stateless` | `true` | Keeps Streamable HTTP stateless for horizontal scaling and no session affinity. |
| `Mcp:EnableLegacySse` | `false` | Reserved for a future trusted-isolated deployment decision; keep disabled. |

## Health And Operations

- Add a bounded health check for MCP adapter posture when implementation lands.
- Health output may report enabled/disabled, endpoint path, stateless posture, and legacy-SSE disabled/enabled state.
- Health output must not include tenant IDs, tool payloads, prompts, provider data, endpoint credentials, or API keys.
- Metrics must use low-cardinality labels such as `tool`, `outcome`, and `mode` only after reviewing cardinality. Avoid tenant/principal labels.

## Consequences

1. The first MCP implementation can reuse API authentication, rate limiting, telemetry, health, and tenant infrastructure.
2. Self-hosters do not need a separate MCP process for baseline operation.
3. A future separate MCP host remains possible if isolation, capacity, or transport requirements justify it.
4. MCP tool behavior stays aligned with product AI because both surfaces read from the same registry.
5. Mutating tools remain slower than direct repository writes, but they preserve validation, authorization, idempotency, audit, and confirmation semantics.

## Alternatives Considered

### Separate MCP Host In Phase 7

Rejected for the first slice. A separate host may be cleaner for isolation later, but it adds deployment, tenant resolution, auth, health, and self-hosting complexity before there is runtime evidence that a separate process is needed.

### Stdio MCP Server

Rejected for the product platform. Stdio is useful for local developer tools, but it does not match the multi-tenant authenticated HTTP platform boundary or self-hosted API deployment model.

### Legacy SSE Transport

Rejected for the first implementation. Current C# SDK docs warn that legacy SSE has weaker request backpressure and should only be used for trusted isolated clients. Streamable HTTP is the selected transport.

### Direct Repository Mutation From MCP Tools

Rejected. It would bypass existing CQRS validation, authorization behavior, actor resolution, transactions, audit, and confirmation semantics.

## Implementation Gates

Phase 7.2 may start only after this decision is recorded and reviewed in active workstream docs. The implementation must include:

1. Central package version and project reference changes for the selected MCP SDK.
2. Disabled-by-default configuration and startup validation.
3. API-hosted MCP endpoint mapping behind authentication and explicit enablement.
4. Registry-backed tool definitions and validation.
5. Tests proving disabled mode, auth/tenant fail-closed behavior, registry-backed tool metadata, and no direct repository mutation for mutating tools.
6. Self-hosting and operations docs.

## Related

- [ARCHITECTURE.md](../ARCHITECTURE.md) — composition root and Clean Architecture boundary.
- [CONFIGURATION.md](../CONFIGURATION.md) — static configuration surface.
- [OPERATIONS.md](../OPERATIONS.md) — health, metrics, and runbook posture.
- [SELF_HOSTING.md](../SELF_HOSTING.md) — self-hosting behavior and optional services.
- `dev/active/ai-tool-contract-registry/ai-tool-contract-registry-plan.md` — AI Tool Contract Registry implementation plan.
