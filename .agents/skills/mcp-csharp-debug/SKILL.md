---
name: mcp-csharp-debug
description: "Load when running or diagnosing a C# MCP server locally: tool registration, protocol messages, stdio/HTTP transport, mcp.json, logging, IDE launch configuration, MCP Inspector, or Copilot Agent Mode; not for server creation, automated tests, or publishing."
type: workflow
enforcement: suggest
priority: medium
license: MIT
---
<!-- ABOUTME: Focused debugging workflow for C# MCP servers and transports. -->
<!-- ABOUTME: Keeps stdio protocol safety and tool-discovery checks close to execution. -->

# C# MCP Server Debugging

## Rules

- Detect transport from registration/package usage; do not guess from the project name.
- For stdio, any stdout logging corrupts JSON-RPC; route diagnostics through `ILogger` to stderr.
- Rebuild and restart before investigating stale tool discovery.
- Diagnose the protocol with MCP Inspector before blaming the consuming IDE.
- Build Debug when breakpoints must bind.

## Workflow

1. Run `dotnet build`, then `dotnet run --project <server.csproj>`.
2. For stdio, confirm the process waits silently on stdout; for HTTP, confirm the actual listening URL and MCP path.
3. Connect MCP Inspector:
   - stdio: `npx @modelcontextprotocol/inspector dotnet run --project <server.csproj>`
   - HTTP: start the server, run Inspector, and connect to its MCP URL.
4. If tools are missing, verify `[McpServerToolType]`, `[McpServerTool]`, and `.WithTools<T>()` or `.WithToolsFromAssembly()`.
5. If invocation fails, inspect stderr/server logs, parameter binding, JSON-serializable return types, DI registrations, and thrown exceptions.
6. Only after Inspector succeeds, configure and test the IDE client.

## Client configuration

```json
{
  "servers": {
    "server-name": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "path/to/server.csproj"]
    }
  }
}
```

Use `type: http` and `url` for an HTTP server.

## Resources

- [MCP Inspector](references/mcp-inspector.md) — load for connection modes, protocol inspection, or Inspector-specific failures.
- [IDE configuration](references/ide-config.md) — load for VS Code/Visual Studio launch files, environment variables, secrets, or breakpoint setup.

## Verification

- Server starts without protocol errors.
- Inspector lists and invokes every expected tool.
- stdio emits no application output on stdout.
- The target IDE discovers the same tools only after Inspector passes.
