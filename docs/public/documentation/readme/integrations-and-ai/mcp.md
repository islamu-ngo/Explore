---
description: >-
  Expose the optional stateless proposal-first MCP endpoint with API-key, scope,
  and tenant controls.
---

# MCP

ISLAMU Event can expose an optional API-hosted MCP endpoint at `/mcp` using stateless Streamable HTTP.

## Enablement

Startup setting `Mcp:Enabled` controls whether the route is mapped. Runtime setting `mcp.enabled` can fail closed with `404`. Production exposure uses the normal TLS, rate-limit, tenant, health, and monitoring boundaries.

External clients use API keys. Do not send an API key and bearer token together. Multi-tenant calls still require valid tenant binding.

## Capability model

Anonymous or invalid-key callers can access only a curated set of anonymous-safe registry and public-event reads. Scoped reads require `mcp:read` plus the underlying event authority. Mutation-capable tools are proposal-first and require `mcp:propose`; they do not directly mutate repositories.

Use the returned tool/resource contract and HAL-aware API behavior rather than assuming a broad administrative surface.

## Unsupported features

The current server does not provide:

* legacy SSE transport;
* stateful MCP sessions;
* server-to-client requests;
* sampling, elicitation, roots, completions, or subscriptions;
* list-changed notifications;
* remote tool import/execution;
* direct repository mutation;
* product stdio hosting.

Configuration ceilings or intent fields do not make an unsupported transport active.

## Acceptance

Test route absence when disabled, curated anonymous behavior, valid and invalid API keys, `mcp:read`, `mcp:propose`, tenant mismatch, proposal review, rate limiting, and safe logs. Keep API keys, proposal payload secrets, tenant-private records, and model/tool output containing PII out of shared diagnostics.
