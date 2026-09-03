---
name: dotnet-mcp
description: "Load when creating, scaffolding, configuring, debugging, testing, containerizing, or publishing a C#/.NET Model Context Protocol (MCP) server using the official Microsoft ModelContextProtocol SDK; not for non-.NET MCP servers, general ASP.NET Core APIs without MCP, or consuming external MCP tools as a client."
type: workflow
enforcement: suggest
priority: high
---
<!-- ABOUTME: Master router and invariant guardian for C#/.NET Model Context Protocol (MCP) server engineering. -->
<!-- ABOUTME: Enforces transport selection, stdio protocol safety, tool contracts, and progressive resource disclosure. -->

# .NET Model Context Protocol (MCP) Engineering

A unified skill for building, debugging, testing, and publishing C# Model Context Protocol (MCP) servers using Microsoft's official `ModelContextProtocol` SDK.

## Resources

- [Resource Index](resources/index.md) — Load to locate the exact lifecycle guide, API specification, or deployment recipe needed.
- [Server Scaffolding & Configuration](resources/server-scaffolding.md) — Load when creating projects, selecting transports, configuring `Program.cs`, or implementing tools.
- [Debugging & Inspector](resources/debugging-and-inspector.md) — Load when running servers locally, troubleshooting stdout pollution, using MCP Inspector, or setting up IDE clients.
- [Testing & Evaluations](resources/testing-and-evaluations.md) — Load when authoring tool unit tests, `McpClient` integration fixtures, or LLM evaluation suites.
- [Publishing & Deployment](resources/publishing-and-deployment.md) — Load when packaging as a NuGet tool, building chiseled Docker images, or publishing to the MCP Registry.

## Rules

1. **Official SDK & Target**: Require .NET 10+ and use the official Microsoft `ModelContextProtocol` or `ModelContextProtocol.AspNetCore` package.
2. **Transport Hierarchy**: Default to **stdio** for local command-line tools, desktop assistants, and IDE subprocesses. Use **Streamable HTTP** (`MapMcp`) for remote, multi-client, or containerized deployments.
3. **stdio stdout Safety Invariant**: In stdio servers, standard output is strictly reserved for JSON-RPC messages. Any application logging to stdout corrupts protocol framing and causes client connections to hang. All console logging and diagnostics must be directed to stderr (`LogToStandardErrorThreshold = LogLevel.Trace`).
4. **Tool Contract**: Tool classes must carry `[McpServerToolType]`; exposed methods must carry `[McpServerTool]`. Both methods and parameters require descriptive `[Description]` attributes to guide AI model tool selection. Asynchronous tools must accept and propagate a `CancellationToken`.
5. **Dependency Injection**: Inject external clients, database contexts, and services via DI into constructors or tool methods. Never instantiate network clients or infrastructure directly inside tool methods.
6. **Inspector-First Verification**: Always verify protocol correctness and tool discovery using the MCP Inspector (`npx @modelcontextprotocol/inspector`) before debugging consuming IDE clients.
7. **Testing Discipline**: Tool logic should be verified with direct unit tests (mocking `HttpMessageHandler` or domain services). Protocol behavior must be verified via integration tests using `McpClient` asserting both `ListToolsAsync()` and `CallToolAsync()`.
8. **Secrets & Packaging Safety**: Never bake API keys, connection strings, or `.env` files into NuGet packages, Docker images, or `server.json`.

## Task Routing

| User Intent / Task | Primary Lifecycle Guide | Deep Reference |
|---|---|---|
| Scaffold new MCP server, choose transport, configure DI | [server-scaffolding.md](resources/server-scaffolding.md) | [transport-config.md](resources/transport-config.md), [api-patterns.md](resources/api-patterns.md) |
| Author complex tools, prompts, resources, or Native AOT | [server-scaffolding.md](resources/server-scaffolding.md) | [api-patterns.md](resources/api-patterns.md) |
| Run locally, debug protocol, use MCP Inspector | [debugging-and-inspector.md](resources/debugging-and-inspector.md) | [mcp-inspector.md](resources/mcp-inspector.md) |
| Configure client IDEs (VS Code, Visual Studio, Copilot) | [debugging-and-inspector.md](resources/debugging-and-inspector.md) | [ide-config.md](resources/ide-config.md) |
| Write unit or protocol integration tests | [testing-and-evaluations.md](resources/testing-and-evaluations.md) | [test-patterns.md](resources/test-patterns.md) |
| Create LLM tool quality evaluations | [testing-and-evaluations.md](resources/testing-and-evaluations.md) | [evaluations.md](resources/evaluations.md) |
| Package as global .NET tool (`dotnet pack`) | [publishing-and-deployment.md](resources/publishing-and-deployment.md) | [nuget-packaging.md](resources/nuget-packaging.md) |
| Build Docker container or deploy to Azure Container Apps | [publishing-and-deployment.md](resources/publishing-and-deployment.md) | [docker-azure.md](resources/docker-azure.md) |
| Publish listing to the official MCP Registry | [publishing-and-deployment.md](resources/publishing-and-deployment.md) | [mcp-registry.md](resources/mcp-registry.md) |

## Verification

- **Compilation**: `dotnet build --configuration Release`
- **Protocol Inspection**: `npx @modelcontextprotocol/inspector dotnet run --project <Server.csproj>`
- **Automated Tests**: `dotnet test --project <Server.Tests.csproj> --configuration Release`
- **Clean stdout**: Confirm that stdio servers print zero non-JSON-RPC lines to stdout during execution.
