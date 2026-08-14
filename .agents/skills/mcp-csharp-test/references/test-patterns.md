<!-- ABOUTME: Provides TUnit integration-test patterns for C# MCP servers. -->
<!-- ABOUTME: Uses real protocol and HTTP boundaries without third-party assertion libraries. -->

# Test Patterns

Complete code patterns for testing C# MCP servers at every level.

## MockHttpMessageHandler Helper

Reusable mock for tools that use `HttpClient`:

```csharp
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _response;
    private readonly HttpStatusCode _statusCode;

    public MockHttpMessageHandler(
        string response = "",
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _response = response;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_response)
        });
}
```

## HTTP Testing with WebApplicationFactory

Test HTTP MCP servers using ASP.NET Core's test infrastructure.

**Important:** `WebApplicationFactory<Program>` requires access to the `Program` class. Either:
- Add `<InternalsVisibleTo Include="YourServer.Tests" />` to the server's `.csproj`, or
- Make the `Program` class public: `public partial class Program { }`

```csharp
using Microsoft.AspNetCore.Mvc.Testing;

public class HttpServerTests
{
    [Test]
    public async Task McpEndpoint_AcceptsInitialize()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var request = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "test", version = "1.0" }
            }
        };

        var response = await client.PostAsJsonAsync("/mcp", request);
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task McpEndpoint_InvokesTool()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        // First initialize the session
        var init = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "test", version = "1.0" }
            }
        };
        await client.PostAsJsonAsync("/mcp", init);

        // Then call a tool
        var toolCall = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "echo",
                arguments = new { message = "hello" }
            }
        };

        var response = await client.PostAsJsonAsync("/mcp", toolCall);
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("hello");
    }

    [Test]
    public async Task HealthEndpoint_ReturnsOk()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }
}
```
