// ABOUTME: Redaction regression tests for event-management MCP error paths.
// ABOUTME: Verifies MCP failures do not echo credentials, tenant/user hints, or raw internals.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using FluentAssertions;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class EventManagementMcpRedactionTests
{
    private const string RawApiKey = "redacted-test-api-key-72d3";
    private const string RawBearerSecret = "redacted-test-bearer-token-72d3";
    private const string ProviderEndpoint = "https://provider.example.test/private?api_key=redacted-test-api-key-72d3";
    private const string RawException = "System.InvalidOperationException: stack trace redaction probe";

    [Test]
    public async Task ProjectedProposalTool_WithInvalidArguments_ReturnsGenericRedactedFailure()
    {
        await using var factory = CreateAuthenticatedMcpFactory();
        using var client = factory.CreateClient();
        var userId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        using var request = CreateAuthenticatedToolCallRequest(
            "propose_create_event_draft",
            new JsonObject
            {
                ["conversationId"] = Guid.CreateVersion7().ToString(),
                ["title"] = "MCP redaction probe",
                ["tenantId"] = tenantId.ToString(),
                ["userId"] = userId.ToString(),
                ["apiKey"] = RawApiKey,
                ["authorization"] = $"Bearer {RawBearerSecret}",
                ["providerEndpoint"] = ProviderEndpoint,
                ["rawException"] = RawException
            },
            userId);

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = await ReadJsonRpcDocumentAsync(response);
        document.RootElement.TryGetProperty("error", out _).Should().BeFalse(document.RootElement.GetRawText());
        var descriptorText = GetFirstTextContent(document.RootElement.GetProperty("result"));
        using var descriptor = JsonDocument.Parse(descriptorText);
        descriptor.RootElement.GetProperty("Success").GetBoolean().Should().BeFalse();
        descriptor.RootElement.GetProperty("FailureCode").GetString().Should().Be("invalid_tool_arguments");
        descriptor.RootElement.GetProperty("Message").GetString().Should().Be("Invalid MCP tool arguments.");
        descriptor.RootElement.GetProperty("Errors")[0].GetString().Should().Be("Invalid MCP tool arguments.");

        AssertNoToolArgumentEcho(
            document.RootElement.GetRawText(),
            tenantId.ToString(),
            userId.ToString(),
            RawApiKey,
            RawBearerSecret,
            ProviderEndpoint,
            RawException);
    }

    [Test]
    public async Task McpEndpoint_WithBearerAndApiKeyConflict_DoesNotEchoCredentials()
    {
        var userId = Guid.CreateVersion7();
        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            DefaultTenantId = PlatformDefaults.DefaultTenantId,
            McpEnabled = true
        };
        using var client = factory.CreateClient();
        var bearerToken = factory.CreateJwt(userId);
        using var request = CreateListToolsRequest();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        request.Headers.Add("X-API-Key", RawApiKey);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        AssertNoCredentialOrInternalEcho(body, RawApiKey, bearerToken, userId.ToString());
    }

    private static AuthenticatedWebApplicationFactory CreateAuthenticatedMcpFactory()
    {
        var factory = new AuthenticatedWebApplicationFactory
        {
            AuthorizationProviderOverride = new StubAuthorizationProvider()
        };
        factory.AdditionalConfiguration["Mcp:Enabled"] = "true";
        factory.AdditionalConfiguration["Mcp:EndpointPath"] = "/mcp";
        factory.AdditionalConfiguration["Mcp:Stateless"] = "true";
        factory.AdditionalConfiguration["Mcp:EnableLegacySse"] = "false";
        return factory;
    }

    private static HttpRequestMessage CreateAuthenticatedToolCallRequest(
        string toolName,
        JsonObject arguments,
        Guid userId)
    {
        var requestBody = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "tools/call",
            ["params"] = new JsonObject
            {
                ["name"] = toolName,
                ["arguments"] = arguments
            }
        };

        var request = CreateBaseMcpRequest();
        request.Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json");
        request.Headers.Add(TestAuthHandler.AuthHeaderName, TestAuthHandler.CreateAuthHeaderValue(userId));
        return request;
    }

    private static HttpRequestMessage CreateListToolsRequest()
    {
        var request = CreateBaseMcpRequest();
        request.Content = new StringContent(
            """
            {"jsonrpc":"2.0","id":1,"method":"tools/list"}
            """,
            Encoding.UTF8,
            "application/json");
        return request;
    }

    private static HttpRequestMessage CreateBaseMcpRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        request.Headers.Add("ProtocolVersion", "2025-06-18");
        request.Headers.Add("MCP-Protocol-Version", "2025-06-18");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        return request;
    }

    private static string GetFirstTextContent(JsonElement result)
    {
        var content = result.GetProperty("content");
        content.ValueKind.Should().Be(JsonValueKind.Array);
        content.GetArrayLength().Should().BeGreaterThan(0);
        return content[0].GetProperty("text").GetString() ?? string.Empty;
    }

    private static async Task<JsonDocument> ReadJsonRpcDocumentAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var trimmed = body.TrimStart();
        if (trimmed.StartsWith('{'))
        {
            return JsonDocument.Parse(trimmed);
        }

        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var payload = line[5..].Trim();
            if (payload.StartsWith('{'))
            {
                return JsonDocument.Parse(payload);
            }
        }

        throw new InvalidOperationException("The MCP response did not contain a JSON-RPC message.");
    }

    private static void AssertNoToolArgumentEcho(string value, params string[] forbiddenValues)
    {
        AssertNoCredentialOrInternalEcho(value, forbiddenValues);
        var normalized = value.ToLowerInvariant();
        normalized.Should().NotContain("tenantid");
        normalized.Should().NotContain("userid");
        normalized.Should().NotContain("apikey");
        normalized.Should().NotContain("authorization");
        normalized.Should().NotContain("providerendpoint");
        normalized.Should().NotContain("rawexception");
    }

    private static void AssertNoCredentialOrInternalEcho(string value, params string[] forbiddenValues)
    {
        foreach (var forbiddenValue in forbiddenValues)
        {
            value.Should().NotContain(forbiddenValue);
        }

        var normalized = value.ToLowerInvariant();
        normalized.Should().NotContain("redacted-test-api-key");
        normalized.Should().NotContain("redacted-test-bearer-token");
        normalized.Should().NotContain("system.invalidoperationexception");
        normalized.Should().NotContain("stack trace");
        normalized.Should().NotContain("https://provider.example.test");
    }
}
