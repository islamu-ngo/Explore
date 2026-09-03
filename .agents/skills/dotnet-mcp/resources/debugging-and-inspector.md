<!-- ABOUTME: Focused debugging and diagnostic workflow for C# MCP servers and transports. -->
<!-- ABOUTME: Covers stdio protocol safety, MCP Inspector execution, log inspection, and client configurations. -->

# C# MCP Server Debugging & Inspector

A guide for executing, inspecting, diagnosing, and connecting C# MCP servers locally using the official Model Context Protocol Inspector and IDE clients.

## Diagnostic Rules & Invariants

1. **Protocol Cleanliness (stdio)**: Any output written to stdout corrupts JSON-RPC protocol framing. If stdout is polluted by startup banners, EF Core SQL queries, or console logs, MCP clients will hang or fail to initialize. Route all diagnostics strictly to stderr.
2. **Inspector First**: Always verify tool discovery and invocation using the official MCP Inspector before troubleshooting IDE integrations (VS Code, Visual Studio, or Claude Desktop). If Inspector succeeds, the server is healthy and the issue is client configuration.
3. **Stale Tool Discovery**: If added tools do not appear in discovery lists, perform a clean rebuild (`dotnet build --no-incremental`) and verify that classes carry `[McpServerToolType]` and methods carry `[McpServerTool]`.

## The Debugging Workflow

### Step 1: Protocol Verification with MCP Inspector
The Inspector provides a browser UI to list tools, execute invocations with arbitrary parameters, and inspect raw JSON-RPC messages.

- **For stdio Servers**:
  Run the server as a child process directly inside the Inspector:
  ```bash
  npx @modelcontextprotocol/inspector dotnet run --project path/to/server.csproj
  ```
- **For HTTP Servers**:
  Start the server in one terminal:
  ```bash
  dotnet run --project path/to/server.csproj
  ```
  In another terminal, launch the Inspector and connect to the listening endpoint (e.g. `http://localhost:5000/mcp`):
  ```bash
  npx @modelcontextprotocol/inspector
  ```

### Step 2: Verify Tool Discovery & Invocation
1. Open the Inspector UI in your browser (`http://localhost:5173`).
2. Click **List Tools** and confirm all expected tools appear with their names and parameter schemas.
3. Call each tool with representative arguments and confirm the response contains the expected `TextContentBlock` or typed payload.

### Step 3: IDE & Client Integration
Only after the server passes Inspector verification, wire it into client configurations:

#### Stdio Client Configuration (`mcp.json` / Claude Desktop / Codex)
```json
{
  "mcpServers": {
    "my-server": {
      "command": "dotnet",
      "args": ["run", "--project", "/absolute/path/to/server.csproj"]
    }
  }
}
```

#### HTTP Client Configuration
```json
{
  "mcpServers": {
    "my-server": {
      "url": "http://localhost:5000/mcp"
    }
  }
}
```

## Troubleshooting Common Defects

| Symptom | Probable Cause | Corrective Action |
|---|---|---|
| Client initialization hangs indefinitely | Application logs written to stdout | Set `LogToStandardErrorThreshold = LogLevel.Trace` in console logging |
| Tool missing from `ListTools` | Missing attributes or assembly scan | Verify `[McpServerToolType]` on class, `[McpServerTool]` on method, and `WithToolsFromAssembly()` in Program.cs |
| Parameter validation error on call | Parameter name or type mismatch | Match parameter names in JSON with method parameters; check `[Description]` annotations |
| DI service injection failure | Missing service registration | Ensure all dependencies injected into tool constructors or static methods are registered in `builder.Services` |
| Breakpoints not binding in IDE | Server launched in Release or external process | Configure IDE launch profiles with `launchSettings.json` or VS Code `launch.json` |

## Related Resources

- [mcp-inspector.md](mcp-inspector.md) — Detailed Inspector capabilities, CLI flags, and transport modes.
- [ide-config.md](ide-config.md) — VS Code `launch.json`, Visual Studio launch profiles, and environment configurations.
- [server-scaffolding.md](server-scaffolding.md) — Host setup and logging configuration.
