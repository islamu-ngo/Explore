---
name: mcp-csharp-create
description: "Load when creating or scaffolding a C#/.NET MCP server, adding MCP tools/prompts/resources, choosing stdio versus HTTP transport, configuring Program.cs, or mapping ASP.NET Core MCP endpoints; not for debugging, tests, publishing, MCP clients, or non-.NET servers."
type: workflow
enforcement: suggest
priority: medium
license: MIT
---
<!-- ABOUTME: Minimal workflow for creating C# MCP servers with the official SDK. -->
<!-- ABOUTME: Routes advanced API and transport decisions to focused references. -->

# C# MCP Server Creation

## Decisions

| Need | Choice |
|---|---|
| Local CLI or IDE subprocess | stdio; default when transport is unspecified |
| Remote, multi-client, or container host | Streamable HTTP |
| Basic stdio project | `dotnet new mcpserver -n <Name>` |
| HTTP project | ASP.NET Core + `ModelContextProtocol.AspNetCore` + `MapMcp()` |
| Native AOT | Explicit `.WithTools<T>()`; avoid reflection discovery |

## Rules

- Require .NET 10+ and verify the installed `mcpserver` template before scaffolding.
- Tool classes use `[McpServerToolType]`; exposed methods and parameters need useful `[Description]` metadata.
- Async tools accept and propagate `CancellationToken`.
- Use DI for external clients and shared services; do not construct infrastructure inside tool methods.
- stdio servers write logs only to stderr because stdout is the JSON-RPC channel.
- Add prompts or resources only when the requested server actually needs them.

## Workflow

1. Inspect the target repository and choose transport from the deployment shape.
2. Scaffold the smallest matching project and install only the official MCP package it needs.
3. Register MCP with `AddMcpServer()`, the selected transport, and tool discovery.
4. Implement one tool end to end before adding optional prompts or resources.
5. Build and start the server, then hand off interactive debugging to `mcp-csharp-debug`.

## Minimal stdio host

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(o =>
    o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
```

## Resources

- [API patterns](references/api-patterns.md) — load for DI, return types, prompts, resources, dynamic tools, or AOT registration.
- [Transport configuration](references/transport-config.md) — load for HTTP auth, stateless mode, custom paths, `HttpContext`, or telemetry.
- [Official C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) — verify SDK behavior that may have changed.

## Verification

- `dotnet build --configuration Release`
- Start the server: stdio must keep stdout protocol-clean; HTTP must expose the configured MCP endpoint.
- Confirm every intended tool is discoverable before adding more surface area.
