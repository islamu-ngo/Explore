<!-- ABOUTME: Reading map and progressive disclosure index for the .NET MCP skill library. -->
<!-- ABOUTME: Directs agents and developers to the exact guide or reference needed for any .NET MCP task. -->

# .NET MCP Resource Index

Use this index to identify the minimal, focused resource for your specific Model Context Protocol task. **Never load all resources at once**; load only the document directly relevant to your active development step.

## Core Lifecycle Workflows

Load these when executing a high-level operational phase:

1. [server-scaffolding.md](server-scaffolding.md) — Creating projects, choosing transports (stdio vs. HTTP), configuring `Program.cs`, tool contracts, and DI registration.
2. [debugging-and-inspector.md](debugging-and-inspector.md) — Local server execution, stdout safety rules, MCP Inspector verification, and IDE client integration.
3. [testing-and-evaluations.md](testing-and-evaluations.md) — Tool unit testing, `McpClient` integration fixtures, `WebApplicationFactory` host testing, and LLM evaluation suites.
4. [publishing-and-deployment.md](publishing-and-deployment.md) — Packaging as NuGet tools, chiseled Docker containerization, Azure deployment, and MCP Registry publishing.

## Deep Technical References

Load these when needing specific API signatures, configuration schemas, or deployment manifests:

### Server & API Implementation
- [api-patterns.md](api-patterns.md) — Tool parameter binding, complex return types, returning `ContentBlock`, prompts, resources, dynamic tools, and Native AOT.
- [transport-config.md](transport-config.md) — Detailed transport options: `StdioServerTransportOptions`, ASP.NET Core `MapMcp` streams, auth, `HttpContext`, and session state.

### Diagnostics & Tooling
- [mcp-inspector.md](mcp-inspector.md) — Interactive testing with `@modelcontextprotocol/inspector`: CLI flags, UI features, and protocol tracing.
- [ide-config.md](ide-config.md) — Client configurations for VS Code (`launch.json`), Visual Studio (`launchSettings.json`), Claude Desktop, and GitHub Copilot.

### Testing & Quality
- [test-patterns.md](test-patterns.md) — Advanced test fixtures: in-memory `ClientServerTestBase`, HTTP `WebApplicationFactory`, and `MockHttpMessageHandler`.
- [evaluations.md](evaluations.md) — Authoring, formatting, and scoring LLM tool evaluation suites for quality measurement.

### Packaging & Distribution
- [nuget-packaging.md](nuget-packaging.md) — Project properties (`PackAsTool`), trusted publishing, local installation, and package signing.
- [docker-azure.md](docker-azure.md) — Production multi-stage Dockerfiles, chiseled .NET images, Azure Container Apps, and App Service commands.
- [mcp-registry.md](mcp-registry.md) — Specification and schema for `.mcp/server.json`, namespace claiming, and registry publishing.
