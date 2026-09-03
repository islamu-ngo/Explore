<!-- ABOUTME: Workflow for creating, scaffolding, and configuring C# MCP servers with the official SDK. -->
<!-- ABOUTME: Covers stdio and ASP.NET Core HTTP hosts, tool contracts, DI registration, and Native AOT. -->

# C# MCP Server Scaffolding & Configuration

A guide for scaffolding and configuring Model Context Protocol (MCP) servers in .NET 10+ using Microsoft's official `ModelContextProtocol` SDK.

## Transport Decision Matrix

| Deployment Shape | Transport | Package & Project Type |
|---|---|---|
| Local CLI, desktop agent, or IDE subprocess | **stdio** (default) | Console application (`dotnet new mcpserver`) + `ModelContextProtocol` |
| Remote server, multi-tenant host, or container | **Streamable HTTP** | ASP.NET Core (`web`) + `ModelContextProtocol.AspNetCore` |
| Low-latency / constrained container | **Native AOT** | Explicit `.WithTools<T>()` registration (avoid reflection discovery) |

## Implementation Rules

1. **Framework & Package**: Target .NET 10+ and install only the official SDK packages:
   - Stdio: `dotnet add package ModelContextProtocol`
   - HTTP: `dotnet add package ModelContextProtocol.AspNetCore`
2. **Tool Contract**: Tool classes must be decorated with `[McpServerToolType]`. Tool methods and their parameters must provide descriptive `[Description]` attributes to guide LLM tool selection.
3. **Async & Cancellation**: All asynchronous tool methods must accept and propagate a `CancellationToken`.
4. **Dependency Injection**: Register domain dependencies and `HttpClient` instances in DI; never construct infrastructure inline inside tool methods.
5. **stdio stdout Safety**: In stdio servers, standard output is strictly reserved for JSON-RPC messages. Route all console logging to stderr:
   ```csharp
   builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
   ```

## Minimal stdio Host

A complete stdio MCP server host in `Program.cs`:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;

var builder = Host.CreateApplicationBuilder(args);

// Ensure stdout is never polluted by application logs
builder.Logging.AddConsole(o =>
    o.LogToStandardErrorThreshold = LogLevel.Trace);

// Register server dependencies
builder.Services.AddHttpClient();

// Register MCP server with stdio transport and assembly tool discovery
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

## Minimal ASP.NET Core HTTP Host

For remote multi-client hosting:
```csharp
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpServer()
    .WithHttpServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapMcp("/mcp");

await app.RunAsync();
```

## Tool Implementation Pattern

```csharp
using System.ComponentModel;
using ModelContextProtocol;

[McpServerToolType]
public class WeatherTools
{
    [McpServerTool]
    [Description("Gets current weather conditions for a specified city.")]
    public static async Task<string> GetCurrentWeather(
        [Description("The name of the city, e.g. Seattle, WA")] string city,
        CancellationToken cancellationToken = default)
    {
        // Business logic or port delegation
        return $"Weather for {city}: 72°F, Sunny";
    }
}
```

## Next Steps & Related Resources

- [api-patterns.md](api-patterns.md) — Complex return types, `ContentBlock`, dynamic tools, prompts, resources, and AOT registration.
- [transport-config.md](transport-config.md) — HTTP auth, stateless mode, custom paths, `HttpContext`, and telemetry.
- [debugging-and-inspector.md](debugging-and-inspector.md) — Testing tools locally via MCP Inspector and client configs.
