ABOUTME: Decision record for future local-only MCP stdio diagnostic hosting.
ABOUTME: Keeps product MCP hosting API-based while documenting why stdio remains deferred.

# ADR-011: Local MCP Stdio Diagnostic Host Decision

## Status

Deferred / not implemented.

## Context

Phase 12 improved MCP debuggability for the existing API-hosted adapter. The supported product adapter is still disabled by default, API-key-first for scoped external clients, tenant-resolved for tenant/private capabilities, registry-backed, stateless Streamable HTTP through `Explore.API`, and anonymous-safe only for explicit registry discovery.

`stdio` is useful for local developer tools, but it has different safety constraints than HTTP with explicit auth/tenant context:

- stdout must carry protocol messages only;
- logs must go to stderr and remain redacted;
- auth and tenant context would need an explicit local simulation model;
- self-hosted product deployments must not gain a second MCP authority;
- Native AOT/reflection implications would need separate verification.

## Decision

Do not add a stdio MCP host in Phase 12.

If a local-only diagnostic host is approved later, it must be a separate executable or profile, not product API hosting. It must:

1. keep `Explore.API` as the product MCP authority;
2. use fake or disposable data by default;
3. simulate auth and tenant context explicitly;
4. write logs to stderr only;
5. never print prompts, payloads, provider responses, tenant/user identifiers, bearer/API-key values, endpoint URLs, model IDs, or raw exceptions;
6. keep mutating tools proposal-first and require the normal product/API confirmation path for side effects;
7. include focused protocol tests, deterministic replay/evaluation evidence, and redacted runbook steps before being advertised.

## Consequences

- Current MCP client smoke uses `docs/MCP_DEBUGGING.md`, WebApplicationFactory JSON-RPC tests, deterministic replay/evaluation reports, Inspector, Copilot Agent Mode, and curl fallback.
- Self-hosters continue to enable or disable one product MCP endpoint with `Mcp:Enabled`.
- No product `WithStdioServerTransport()` wiring is allowed without a new approved implementation task.

## Alternatives Considered

### Add stdio to `Explore.API`

Rejected. Product API hosting must remain HTTP with explicit API-key/bearer auth and tenant context for scoped operations; adding stdio to the product process would blur transport, auth, tenant, logging, and deployment responsibilities.

### Add a separate diagnostic host now

Deferred. The current need is better debug/runbook/test confidence, which Phase 12 covers without another process or protocol surface.
