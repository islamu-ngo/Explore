<!-- ABOUTME: Testing strategy, protocol integration fixtures, and evaluation suites for C# MCP servers. -->
<!-- ABOUTME: Covers direct tool unit testing, McpClient protocol tests, WebApplicationFactory, and LLM evaluations. -->

# C# MCP Server Testing & Evaluations

A guide for verifying C# Model Context Protocol (MCP) servers across unit, integration, protocol, and evaluation boundaries using the official `ModelContextProtocol` client SDK and repository test conventions.

## Test Boundary Matrix

| Risk Surface | Smallest Useful Check | Implementation Approach |
|---|---|---|
| Tool business logic | Direct unit test calling method | Call tool method directly with mocked external dependencies (`HttpMessageHandler`) |
| Protocol discovery & schemas | Integration test listing tools | `McpClient.ListToolsAsync()` asserting tool name, description, and parameter schemas |
| Tool invocation & serialization | Protocol integration test calling tool | `McpClient.CallToolAsync()` asserting typed return values or `TextContentBlock` |
| HTTP hosting & routing | Host integration test | `WebApplicationFactory<Program>` exercising the mapped `/mcp` endpoint |
| LLM tool selection quality | Evaluation suite | Deterministic, non-destructive eval prompts executed against model endpoints |

## Testing Invariants & Rules

1. **Test Tool Methods Directly**: The fastest and cleanest unit tests invoke the static or instance tool methods directly without spinning up the MCP transport.
2. **Protocol Integration Proof**: Integration tests must prove both discovery (`ListToolsAsync`) and invocation (`CallToolAsync`) over a live transport.
3. **Mock Dependencies, Not Protocol**: In unit tests, mock `HttpMessageHandler` or domain services. Never attempt to mock internal MCP protocol framing.
4. **stdio Client Timeout Prevention**: Stdio integration test fixtures will hang on `McpClient.CreateAsync()` if the server writes non-protocol output to stdout. Verify standard error threshold configuration.
5. **Non-Destructive Evaluations**: LLM evaluation questions must be read-only, deterministic (single verifiable answer), and require multi-step reasoning.

## 1. Tool Unit Tests

Directly test tool execution with mocked dependencies:
```csharp
public class WeatherToolsTests
{
    [Test]
    public async Task GetCurrentWeather_ValidCity_ReturnsFormattedWeather()
    {
        // Act
        var result = await WeatherTools.GetCurrentWeather("Seattle, WA");

        // Assert
        await Assert.That(result).Contains("Seattle, WA");
    }
}
```

## 2. Protocol Integration Tests with `McpClient`

Exercise the full JSON-RPC protocol using a child-process stdio transport:
```csharp
using ModelContextProtocol.Client;

public class McpServerIntegrationTests : IAsyncDisposable
{
    private McpClient? _client;

    public async Task SetupClientAsync()
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "IntegrationTestClient",
            Command = "dotnet",
            Arguments = ["run", "--project", "../MyMcpServer/MyMcpServer.csproj"]
        });

        _client = await McpClient.CreateAsync(transport);
    }

    [Test]
    public async Task Server_DiscoversExpectedTools()
    {
        await SetupClientAsync();
        var tools = await _client!.ListToolsAsync();

        await Assert.That(tools.Any(t => t.Name == "GetCurrentWeather")).IsTrue();
    }

    [Test]
    public async Task Server_InvokesToolSuccessfully()
    {
        await SetupClientAsync();
        var response = await _client!.CallToolAsync("GetCurrentWeather",
            new Dictionary<string, object?> { ["city"] = "Austin, TX" });

        var text = response.Content.OfType<TextContentBlock>().First().Text;
        await Assert.That(text).Contains("Austin, TX");
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
            await _client.DisposeAsync();
    }
}
```

## 3. In-Memory & HTTP Testing

For in-memory testing without child-process overhead, use `ClientServerTestBase`, or `WebApplicationFactory` for HTTP endpoints. See [test-patterns.md](test-patterns.md).

## 4. LLM Tool Evaluations

Evaluations test whether an AI model accurately chooses and uses your server's tools. Design evaluation suites with:
- **Zero side-effects**: Test queries must not alter databases or state.
- **Deterministic answers**: Single unambiguous ground truth string or count.
- **Complex orchestration**: Multi-step questions requiring multiple tool calls.

See [evaluations.md](evaluations.md) for eval schemas, sample suites, and scoring metrics.

## Related Resources

- [test-patterns.md](test-patterns.md) — In-memory client-server fixtures, `WebApplicationFactory` setups, and `MockHttpMessageHandler`.
- [evaluations.md](evaluations.md) — Designing, formatting, and scoring evaluation suites.
- [debugging-and-inspector.md](debugging-and-inspector.md) — Interactive verification before automated testing.
