---
name: mcp-csharp-test
description: "Load when writing unit, integration, protocol, WebApplicationFactory, evaluation, or tool-quality tests for a C# MCP server with the MCP client SDK; not for MCP clients, load/performance tests, non-.NET servers, or debugging runtime failures."
type: workflow
enforcement: suggest
priority: medium
license: MIT
---
<!-- ABOUTME: Minimal testing strategy for C# MCP server tools and protocol behavior. -->
<!-- ABOUTME: Routes detailed fixtures and evaluation design to focused references. -->

# C# MCP Server Testing

## Test boundary

| Risk | Smallest useful check |
|---|---|
| Tool business behavior | Call the tool method directly with faked boundary dependencies |
| Tool discovery/schema | MCP client lists the registered tool and expected metadata |
| Invocation/serialization | MCP client calls the tool and asserts typed/content output |
| HTTP hosting | `WebApplicationFactory` exercises the mapped MCP endpoint |
| LLM tool selection quality | Deterministic, read-only evaluation set |

## Rules

- Use the repository's TUnit conventions and project-specific `dotnet test --project` command.
- Keep tool logic independently callable; mock `HttpMessageHandler` or domain ports, not the MCP protocol itself.
- Integration tests must prove both `ListToolsAsync()` and `CallToolAsync()`.
- A stdio test server must keep stdout protocol-clean or client creation can hang.
- Evaluation questions are non-destructive, deterministic, and have a verifiable answer.

## Workflow

1. Add direct tests for tool validation, success, cancellation, and boundary failures.
2. Add one protocol integration fixture for discovery and invocation.
3. Add HTTP-host coverage only for HTTP transport.
4. Add evaluations only when the task explicitly measures model/tool behavior.
5. Run the focused tests, then the entire MCP test project.

## Resources

- [Test patterns](references/test-patterns.md) — load for in-memory client/server fixtures, `WebApplicationFactory`, HTTP fakes, or coverage setup.
- [Evaluations](references/evaluations.md) — load for eval schema, question design, scoring, or quality measurement.

## Verification

- `dotnet build --configuration Release`
- `dotnet test --project <ServerName>.Tests/<ServerName>.Tests.csproj --configuration Release`
- Confirm CI needs no manual server startup, credentials, or interactive approval.
