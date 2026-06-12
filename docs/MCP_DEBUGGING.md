ABOUTME: Redacted local debugging and client-smoke runbook for the API-hosted MCP adapter.
ABOUTME: Provides secret-free Inspector, VS Code, Copilot, JSON-RPC, and compatibility guidance.

# MCP Debugging And Client Smoke

> **Audience:** Contributors | Operators | AI agents
> **Status:** Implemented for local/manual smoke and automated contract tests
> **Last Verified:** 2026-06-07
> **Source Anchors:** `Explore.API/Program.cs`, `Explore.API/Mcp/*`, `Event.API.IntegrationTests/Features/McpProtocolContractTests.cs`, `docs/adr/ADR-010-mcp-adapter-hosting-strategy.md`

This runbook is for debugging the optional API-hosted Model Context Protocol (MCP) adapter. It does **not** change the product boundary: MCP is mapped by default at `/mcp`, stateless, API-key-first for external clients, registry-backed, and proposal-first. No credentials, a blank API-key header, or invalid API keys may see only the explicitly anonymous-safe registry discovery surface; scoped tools/resources/prompts still require a valid authenticated context.

## Safe Local Posture

Use only fake or disposable data when connecting interactive MCP clients.

Required configuration for local MCP debugging:

| Setting | Required value | Why |
|---|---|---|
| `Mcp:Enabled` | `true` or unset default | Maps the local `/mcp` endpoint. Set `false` only when you intentionally want the endpoint unmapped. |
| `mcp.enabled` | `true` or unset default | Runtime governance must also allow the adapter. If a mapped endpoint returns `404`, check this instance/tenant setting before debugging tools. |
| `Mcp:Stateless` | `true` | Keeps Streamable HTTP stateless; no session affinity or `Mcp-Session-Id`. |
| `Mcp:EnableLegacySse` | `true` or unset default | Legacy SSE remains unavailable at runtime. The true startup value is only a ceiling for future governance, not an automatic runtime transport change. |
| `mcp.enable_legacy_sse` | `false` or unset default | Records runtime intent only; it does not expose legacy SSE in the current adapter. |
| Auth | prefer one `X-API-Key` | Normal external MCP smoke uses a disposable API key. Bearer tokens are allowed only for user-delegated local smoke. Do not send both. |
| Tenant binding | normal API edge binding | Anonymous or invalid-key reads still need a resolved tenant in multi-tenant mode. A valid tenant-bound API key may provide tenant context. |

Example local API launch with redacted values:

```bash
ASPNETCORE_ENVIRONMENT=Development \
Mcp__Enabled=true \
Mcp__EndpointPath=/mcp \
Mcp__Stateless=true \
Mcp__EnableLegacySse=true \
dotnet run --project Explore.API/Explore.API.csproj --configuration Debug --urls http://127.0.0.1:<redacted-port>
```

Runtime governance checks:

- `Mcp:Enabled=false` means `/mcp` is not mapped and requires an API restart after changing startup configuration.
- `Mcp:Enabled=true` plus `mcp.enabled=false` means the path is mapped but the runtime gate returns `404` without exposing MCP details.
- MCP clients that render an optional API-key field as `"X-API-Key": ""` are treated the same as no API key. Remove the header or leave it blank for anonymous-safe discovery.
- Invalid, revoked, or missing API keys can still reach anonymous-safe discovery only, but they remain rate-limited by remote IP. Repeated bad-key smoke attempts should produce `429`/`Retry-After` without echoing the credential.
- Instance admins configure `mcp.enabled`, `mcp.enable_legacy_sse`, `governance.lock_tenant_mcp`, and `governance.lock_tenant_mcp_legacy_sse` from instance administration. Tenant admins see MCP overrides only when the corresponding lock is open.
- Endpoint path and stateless mode are never runtime settings. Legacy SSE remains unavailable even when both startup and runtime legacy-SSE values are true.
- `/health/ready` exposes only bounded MCP booleans such as `startupEnabled`, `runtimeEnabled`, `legacySseRuntimeRequested`, and `legacySseRuntimeEnabled`; it must not include tenant IDs, endpoint URLs, keys, prompts, or raw protocol bodies.

Before diagnosing missing tools, rebuild and restart both the API and the MCP client:

```bash
dotnet build Explore.API/Explore.API.csproj --configuration Debug --verbosity quiet
```

Set breakpoints in:

- `Explore.API/Mcp/AiToolRegistryMcpTools.cs` for `list_ai_tool_contracts`;
- `Explore.API/Mcp/AiAssistantMcpTools.cs` for generic proposal calls;
- `Explore.API/Mcp/AiMcpProjectedToolFactory.cs` for projected `propose_*` tools;
- relevant MediatR handlers such as `ProposeAiToolActionCommandHandler`.

## Redacted VS Code MCP Template

Do not commit `.vscode/mcp.json` with real tokens, tenant slugs, endpoint URLs, API keys, or copied response bodies. Root `.mcp.json` is git-ignored for local secrets; documentation must keep only placeholders.

```json
{
  "servers": {
    "islamu-event-local-mcp": {
      "type": "http",
      "url": "http://127.0.0.1:<redacted-port>/mcp",
      "headers": {
        "X-API-Key": "${env:ISLAMU_EVENT_API_KEY}",
        "X-Tenant-Slug": "${input:islamu_mcp_tenant_slug}"
      }
    }
  },
  "inputs": [
    {
      "type": "promptString",
      "id": "islamu_mcp_tenant_slug",
      "description": "Disposable tenant slug for local MCP smoke",
      "password": false
    }
  ]
}
```

Set `ISLAMU_EVENT_API_KEY` only in your local shell or secret manager. Leave it unset to smoke the anonymous-safe `list_ai_tool_contracts` path; never configure both `Authorization` and `X-API-Key` in the same client entry.

## Redacted Visual Studio / Solution MCP Template

Use a solution-local `.mcp.json` only with placeholders or prompt-backed values. Do not commit real credentials.

```json
{
  "servers": {
    "islamu-event-local-mcp": {
      "type": "http",
      "url": "http://127.0.0.1:<redacted-port>/mcp",
      "headers": {
        "X-API-Key": "<redacted-disposable-api-key>",
        "X-Tenant-Slug": "<redacted-disposable-tenant-slug>"
      }
    }
  }
}
```

## MCP Inspector Smoke

Run deterministic replay first; it is the CI-safe contract check and does not contact live clients or providers:

```bash
dotnet run --project Explore.Diagnostic/Explore.Diagnostic.csproj --configuration Release --no-restore -- ai-replay-report --output /tmp/explore-ai-replay-mcp-smoke
```

Then start Inspector manually:

```bash
npx -y @modelcontextprotocol/inspector
```

Inspector checklist:

1. Connect to `http://127.0.0.1:<redacted-port>/mcp`.
2. Configure `X-API-Key` for scoped smoke, leave credentials blank for anonymous-safe discovery, or use a bearer token only for user-delegated local smoke. Do not configure both bearer and API key. Valid keys are rate-limited by key ID; missing/invalid/revoked keys are rate-limited by remote IP.
3. Include the same tenant binding used by the API edge when multi-tenant routing is active. A valid tenant-bound API key can provide tenant context; anonymous/no-key smoke still needs trusted tenant binding outside single-tenant mode.
4. If the client receives `404`, check the startup ceiling (`Mcp:Enabled`) and runtime setting (`mcp.enabled`) before investigating client headers.
5. Initialize the connection and list tools, resources, resource templates, and prompts.
6. Anonymous, invalid-key, or revoked-key expected surface: `list_ai_tool_contracts` only.
7. Valid scoped-key expected surface: `mcp:read` can discover read resources/resource templates, while `mcp:propose` is required to discover or call `propose_ai_tool_action`, projected `propose_create_event_draft`, and prompt `create_event_draft_with_confirmation`.
8. Call `list_ai_tool_contracts` and verify proposal/confirmation metadata is present.
9. Optional: call `propose_create_event_draft` only with a valid scoped key against a disposable conversation and stop after the proposed action is returned.
10. Do not call confirm/reject endpoints from Inspector and do not assert that an event was created.

Retain only scenario code, pass/fail status, redacted endpoint path, redacted auth mode, and bounded failure category. Discard protocol exports, browser storage, copied JSON bodies, and screenshots that contain private content.

## GitHub Copilot Agent Mode Smoke

1. Rebuild the API and restart the MCP client before investigating stale tools.
2. Open Copilot Chat in Agent mode and choose **Select Tools**.
3. With a valid scoped API key, verify the expected MCP tools and prompts are visible. Without a key, verify only `list_ai_tool_contracts` is visible.
4. Use a disposable prompt that asks for a proposal only, not direct event creation.
5. Confirm that Copilot asks for tool approval and that the result says confirmation is required before side effects.
6. If tools are missing, restart the API and the IDE MCP server entry, then verify the client config URL and auth headers.

Do not store Copilot transcripts, screenshots, raw tool payloads, tenant/user identifiers, bearer/API-key values, or model/provider output as evidence.

## Command-Line JSON-RPC Smoke

Use this only when Inspector is unavailable. Keep output local and redacted. Omit `X-API-Key` to smoke anonymous-safe discovery only.

```bash
curl --fail-with-body \
  -H 'Accept: application/json, text/event-stream' \
  -H 'Content-Type: application/json' \
  -H 'ProtocolVersion: 2025-06-18' \
  -H 'X-API-Key: <redacted-disposable-api-key-or-env-placeholder>' \
  -H 'X-Tenant-Slug: <redacted-disposable-tenant-slug>' \
  --data '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' \
  'http://127.0.0.1:<redacted-port>/mcp'
```

Do not paste raw responses into tickets. Summarize only the method, pass/fail status, and bounded failure category.

## Automated Contract Harness

`Event.API.IntegrationTests/Features/McpProtocolContractTests.cs` is the automated counterpart to this manual runbook. It uses `WebApplicationFactory`, authenticated test principals, a minimal stateless JSON-RPC helper, and fake/disposable in-memory AI conversation data to prove:

- anonymous and invalid-key `tools/list` expose only the anonymous-safe registry discovery surface;
- valid scoped API-key `initialize`, `tools/list`, `resources/list`, `resources/templates/list`, and `prompts/list` expose the bounded authenticated surface;
- `tools/call` for registry discovery is redacted;
- generic and projected proposal tools persist proposed actions only;
- proposal calls do not create events;
- malformed, unknown, hidden-field, and disabled-endpoint paths fail safely without echoing sensitive markers.

The harness intentionally does not run MCP Inspector, GitHub Copilot, live AI providers, or product confirmation endpoints in normal CI.

## Bounded Diagnostics And Doctor Checks

The adapter emits bounded OpenTelemetry dimensions for MCP tool calls only:

- ActivitySource: `Explore.Mcp`;
- Meter: `Explore.Mcp`;
- metrics: `explore.mcp.tool_calls` and `explore.mcp.tool_call_duration`;
- allowed tags: known MCP tool name, projected-tool flag, bounded outcome, and bounded failure code.

These traces/metrics must never include prompts, selected-reference content, tool payload JSON, provider responses, tenant/user identifiers, bearer/API-key values, endpoint URLs, model IDs, or raw exceptions.

Use the read-only doctor when reviewing MCP debug readiness:

```bash
dotnet run --project Explore.Diagnostic/Explore.Diagnostic.csproj --configuration Release --no-restore -- --timeout-seconds 30
```

`McpDebugReadinessDoctorCheck` verifies that this runbook, protocol tests, deterministic replay/evaluation coverage, `.gitignore` local-secret posture, and the stdio decision ADR are present. It does not start servers, call live MCP clients, create tokens, persist config, run migrations, or print secrets.

Deterministic replay/evaluation evidence now includes MCP proposal-vs-execution checks:

```bash
dotnet run --project Explore.Diagnostic/Explore.Diagnostic.csproj --configuration Release --no-restore -- ai-replay-report --output /tmp/explore-ai-replay-mcp-smoke
dotnet run --project Explore.Diagnostic/Explore.Diagnostic.csproj --configuration Release --no-restore -- ai-eval-report --output /tmp/explore-ai-eval-mcp-smoke
```

## Local Stdio Diagnostic Host

Product MCP stdio hosting remains out of scope. [ADR-011](adr/ADR-011-local-mcp-stdio-diagnostic-host.md) records the current decision: no stdio host is implemented in Phase 12. Any future local-only diagnostic host must be separate from `Explore.API`, keep logs on stderr, simulate auth/tenant context explicitly, remain proposal-first, and ship its own tests/runbook before use.

## Compatibility Matrix And Upgrade Gate

| Client / path | Supported use | Evidence | Unsupported |
|---|---|---|---|
| MCP Inspector | Manual local/staging discovery and proposal-only smoke | Replay report + redacted manual checklist | Saved screenshots, protocol exports, confirmation calls |
| VS Code / GitHub Copilot Agent Mode | Manual tool visibility and approval-flow smoke | Redacted pass/fail notes only | Stored transcripts, direct mutation claims |
| WebApplicationFactory JSON-RPC tests | CI-safe protocol contract coverage | `McpProtocolContractTests` | Live clients, live providers, raw protocol artifacts |
| Doctor / replay / evaluation | Read-only readiness and advisory MCP proposal-flow evidence | `McpDebugReadinessDoctorCheck`, `ai-replay-report`, `ai-eval-report` | Starting servers, creating tokens, live providers |
| Official C# SDK client | Future upgrade target when test-host transport is practical | ADR/update checklist | Product behavior changes without review |
| curl / JSON-RPC fallback | Local troubleshooting only | Method/status/failure category | Pasted credentials or raw response bodies |

Before upgrading `ModelContextProtocol.AspNetCore` or supporting new client-visible behavior, complete this gate:

1. Review SDK/protocol release notes.
2. Rerun focused MCP API integration tests.
3. Rerun `ai-replay-report` and advisory evals when relevant.
4. Repeat redacted Inspector smoke if endpoint behavior changed.
5. Verify docs still say startup default `/mcp`, runtime-governed, stateless, API-key-first for external clients, anonymous-safe only without a valid key, registry-backed, and proposal-first.
6. Keep rollback simple: set runtime `mcp.enabled=false` for immediate shutdown or set `Mcp:Enabled=false` plus restart to unmap the endpoint.

Unsupported without a new ADR/task: stateful sessions, legacy SSE, server-to-client requests, sampling, elicitation, roots, completions, subscriptions, list-changed notifications, remote tool import/execution, direct repository mutation, and product stdio hosting.
