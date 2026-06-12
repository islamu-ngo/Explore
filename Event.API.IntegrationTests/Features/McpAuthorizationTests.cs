// ABOUTME: MCP endpoint authorization tests for the API-hosted Streamable HTTP adapter.
// ABOUTME: Verifies anonymous-safe discovery, optional API-key fallback, and direct-auth conflict handling.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.Application.Constants;
using Explore.Application.Services;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using FluentAssertions;
using TUnit.Core;

namespace ApiIntegrationTests.Features;

public sealed class McpAuthorizationTests
{

    [Test]
    public async Task McpEndpoint_WhenRuntimeGovernanceDisabled_ReturnsNotFound()
    {
        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            DefaultTenantId = PlatformDefaults.DefaultTenantId,
            McpEnabled = true,
            McpRuntimeEnabled = false
        };
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest();

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task McpEndpoint_WhenStartupDisabled_ReturnsNotFoundEvenIfRuntimeEnabled()
    {
        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            DefaultTenantId = PlatformDefaults.DefaultTenantId,
            McpEnabled = false,
            McpRuntimeEnabled = true
        };
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest();

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task McpEndpoint_WhenAnonymous_CanListAnonymousSafeContractsOnly()
    {
        await using var factory = CreateMcpEnabledFactory();
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest();

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toolNames = await ReadToolNamesAsync(response);
        toolNames.Should().Contain("list_ai_tool_contracts");
        toolNames.Should().NotContain("propose_ai_tool_action");
        toolNames.Should().NotContain("propose_create_event_draft");
    }

    [Test]
    public async Task McpEndpoint_WhenStartupDefaultsAreUsed_MapsDefaultPath()
    {
        await using var factory = new AuthenticatedWebApplicationFactory
        {
            AuthorizationProviderOverride = new StubAuthorizationProvider()
        };
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest();

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toolNames = await ReadToolNamesAsync(response);
        toolNames.Should().Contain("list_ai_tool_contracts");
    }

    [Test]
    public async Task McpEndpoint_WithEmptyApiKeyHeader_TreatsHeaderAsAnonymous()
    {
        await using var factory = CreateMcpEnabledFactory();
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest();
        request.Headers.Add("X-API-Key", string.Empty);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toolNames = await ReadToolNamesAsync(response);
        toolNames.Should().Contain("list_ai_tool_contracts");
        toolNames.Should().NotContain("propose_ai_tool_action");
        toolNames.Should().NotContain("propose_create_event_draft");
    }

    [Test]
    public async Task McpEndpoint_WhenAuthenticated_ReachesMcpProtocolBoundary()
    {
        await using var factory = CreateMcpEnabledFactory();
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest();
        request.Headers.Add(
            TestAuthHandler.AuthHeaderName,
            TestAuthHandler.CreateAuthHeaderValue(Guid.CreateVersion7(), "MCP User"));

        var response = await client.SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task McpEndpoint_WithInvalidApiKey_FallsBackToAnonymousSafeContracts()
    {
        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            DefaultTenantId = PlatformDefaults.DefaultTenantId,
            McpEnabled = true
        };
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest();
        request.Headers.Add("X-API-Key", "invalid-test-key");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toolNames = await ReadToolNamesAsync(response);
        toolNames.Should().Contain("list_ai_tool_contracts");
        toolNames.Should().NotContain("propose_ai_tool_action");
        toolNames.Should().NotContain("propose_create_event_draft");
    }

    [Test]
    public async Task McpEndpoint_WithRevokedApiKey_FallsBackToAnonymousSafeContracts()
    {
        const string keyId = "mcp-revoked-key";
        const string secret = "mcp-revoked-secret";
        var rawApiKey = ApiKeyHashing.FormatPersistedApiKey(keyId, secret);

        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            DefaultTenantId = PlatformDefaults.DefaultTenantId,
            McpEnabled = true,
            PersistedApiKeys =
            [
                new ExternalApiPhase0WebApplicationFactory.PersistedApiKeySeed
                {
                    KeyId = keyId,
                    Secret = secret,
                    TenantId = PlatformDefaults.DefaultTenantId,
                    OwnerId = Guid.CreateVersion7(),
                    Status = ExternalApiKeyStatusEnum.Revoked,
                    Scopes = [ExternalApiKeyScopes.McpRead, ExternalApiKeyScopes.McpPropose]
                }
            ]
        };
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest();
        request.Headers.Add("X-API-Key", rawApiKey);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toolNames = await ReadToolNamesAsync(response);
        toolNames.Should().Contain("list_ai_tool_contracts");
        toolNames.Should().NotContain("propose_ai_tool_action");
        toolNames.Should().NotContain("propose_create_event_draft");
    }

    [Test]
    public async Task McpEndpoint_WithValidApiKeyAndConflictingTenantSlug_ReturnsNotFound()
    {
        var apiKeyTenantId = Guid.CreateVersion7();
        var hintedTenantId = Guid.CreateVersion7();
        const string keyId = "mcp-tenant-key";
        const string secret = "mcp-tenant-secret";
        var rawApiKey = ApiKeyHashing.FormatPersistedApiKey(keyId, secret);

        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.MultiTenant,
            McpEnabled = true,
            TenantSlugMappings = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
            {
                ["beta"] = hintedTenantId
            },
            PersistedApiKeys =
            [
                new ExternalApiPhase0WebApplicationFactory.PersistedApiKeySeed
                {
                    KeyId = keyId,
                    Secret = secret,
                    TenantId = apiKeyTenantId,
                    OwnerId = Guid.CreateVersion7(),
                    Scopes = [ExternalApiKeyScopes.McpRead]
                }
            ]
        };
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest();
        request.Headers.Add("X-API-Key", rawApiKey);
        request.Headers.Add(TenantHeaderNames.TenantSlug, "beta");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task McpEndpoint_WithMcpReadOnlyApiKey_CannotDiscoverOrCallProposalTools()
    {
        const string keyId = "mcp-read-only-key";
        const string secret = "mcp-read-only-secret";
        var rawApiKey = ApiKeyHashing.FormatPersistedApiKey(keyId, secret);

        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            DefaultTenantId = PlatformDefaults.DefaultTenantId,
            McpEnabled = true,
            PersistedApiKeys =
            [
                new ExternalApiPhase0WebApplicationFactory.PersistedApiKeySeed
                {
                    KeyId = keyId,
                    Secret = secret,
                    TenantId = PlatformDefaults.DefaultTenantId,
                    OwnerId = Guid.CreateVersion7(),
                    Scopes = [ExternalApiKeyScopes.McpRead]
                }
            ]
        };
        using var client = factory.CreateClient();
        using var listRequest = CreateMcpRequest();
        listRequest.Headers.Add("X-API-Key", rawApiKey);

        var listResponse = await client.SendAsync(listRequest);

        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var toolNames = await ReadToolNamesAsync(listResponse);
        toolNames.Should().Contain("list_ai_tool_contracts");
        toolNames.Should().NotContain("propose_ai_tool_action");
        toolNames.Should().NotContain("propose_create_event_draft");

        using var callRequest = CreateMcpToolCallRequest("propose_create_event_draft", new
        {
            conversationId = Guid.CreateVersion7(),
            title = "Read-only key should not propose"
        });
        callRequest.Headers.Add("X-API-Key", rawApiKey);

        var callResponse = await client.SendAsync(callRequest);

        callResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var errorDocument = await ReadJsonRpcDocumentAsync(callResponse);
        errorDocument.RootElement.TryGetProperty("error", out _).Should().BeTrue(errorDocument.RootElement.GetRawText());
        errorDocument.RootElement.GetRawText().Should().NotContain(rawApiKey);
    }

    [Test]
    public async Task McpEndpoint_RateLimiting_PartitionsAnonymousAndApiKeyTraffic()
    {
        const string keyId = "mcp-rate-limited-key";
        const string secret = "mcp-rate-limited-secret";
        var rawApiKey = ApiKeyHashing.FormatPersistedApiKey(keyId, secret);

        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            DefaultTenantId = PlatformDefaults.DefaultTenantId,
            McpEnabled = true,
            DisableRateLimitingInTesting = false,
            GlobalRateLimitTokenLimit = 1,
            GlobalRateLimitTokensPerPeriod = 1,
            GlobalRateLimitReplenishPeriodSeconds = 60,
            PersistedApiKeys =
            [
                new ExternalApiPhase0WebApplicationFactory.PersistedApiKeySeed
                {
                    KeyId = keyId,
                    Secret = secret,
                    TenantId = PlatformDefaults.DefaultTenantId,
                    OwnerId = Guid.CreateVersion7(),
                    Scopes = [ExternalApiKeyScopes.McpRead]
                }
            ]
        };
        using var client = factory.CreateClient();

        using var firstAnonymousRequest = CreateMcpRequest();
        var firstAnonymousResponse = await client.SendAsync(firstAnonymousRequest);

        using var secondAnonymousRequest = CreateMcpRequest();
        var throttledAnonymousResponse = await client.SendAsync(secondAnonymousRequest);

        using var firstApiKeyRequest = CreateMcpRequest();
        firstApiKeyRequest.Headers.Add("X-API-Key", rawApiKey);
        var firstApiKeyResponse = await client.SendAsync(firstApiKeyRequest);

        using var secondApiKeyRequest = CreateMcpRequest();
        secondApiKeyRequest.Headers.Add("X-API-Key", rawApiKey);
        var throttledApiKeyResponse = await client.SendAsync(secondApiKeyRequest);

        firstAnonymousResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        throttledAnonymousResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        throttledAnonymousResponse.Headers.Contains("Retry-After").Should().BeTrue();
        firstApiKeyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        throttledApiKeyResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        throttledApiKeyResponse.Headers.Contains("Retry-After").Should().BeTrue();
    }

    [Test]
    public async Task McpEndpoint_WithInvalidApiKey_IsRateLimitedWithoutEchoingCredential()
    {
        const string invalidApiKey = "invalid-mcp-rate-limit-secret";

        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            DefaultTenantId = PlatformDefaults.DefaultTenantId,
            McpEnabled = true,
            DisableRateLimitingInTesting = false,
            GlobalRateLimitTokenLimit = 1,
            GlobalRateLimitTokensPerPeriod = 1,
            GlobalRateLimitReplenishPeriodSeconds = 60
        };
        using var client = factory.CreateClient();

        using var firstRequest = CreateMcpRequest();
        firstRequest.Headers.Add("X-API-Key", invalidApiKey);
        var firstResponse = await client.SendAsync(firstRequest);

        using var throttledRequest = CreateMcpRequest();
        throttledRequest.Headers.Add("X-API-Key", invalidApiKey);
        var throttledResponse = await client.SendAsync(throttledRequest);
        var throttledBody = await throttledResponse.Content.ReadAsStringAsync();

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        throttledResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        throttledBody.Should().NotContain(invalidApiKey);
    }

    [Test]
    public async Task McpEndpoint_WithValidScopedApiKey_CanDiscoverAuthorizedProposalTools()
    {
        var ownerUserId = Guid.NewGuid();
        const string keyId = "mcp-user-key";
        const string secret = "mcp-user-secret";
        var rawApiKey = ApiKeyHashing.FormatPersistedApiKey(keyId, secret);

        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            DefaultTenantId = PlatformDefaults.DefaultTenantId,
            McpEnabled = true,
            PersistedApiKeys =
            [
                new ExternalApiPhase0WebApplicationFactory.PersistedApiKeySeed
                {
                    KeyId = keyId,
                    Secret = secret,
                    TenantId = PlatformDefaults.DefaultTenantId,
                    OwnerId = ownerUserId,
                    OwnerType = ExternalApiKeyOwnerType.User,
                    Scopes = [ExternalApiKeyScopes.McpRead, ExternalApiKeyScopes.McpPropose]
                }
            ]
        };
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest();
        request.Headers.Add("X-API-Key", rawApiKey);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var toolNames = await ReadToolNamesAsync(response);
        toolNames.Should().Contain("list_ai_tool_contracts");
        toolNames.Should().Contain("propose_ai_tool_action");
        toolNames.Should().Contain("propose_create_event_draft");
    }

    [Test]
    public async Task McpEndpoint_WithBearerAndApiKey_ReturnsBadRequest()
    {
        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            DefaultTenantId = PlatformDefaults.DefaultTenantId,
            McpEnabled = true
        };
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateJwt(Guid.NewGuid()));
        request.Headers.Add("X-API-Key", "conflicting-test-key");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static AuthenticatedWebApplicationFactory CreateMcpEnabledFactory()
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

    private static HttpRequestMessage CreateMcpRequest()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/mcp")
        {
            Content = new StringContent(
                """
                {
                  "jsonrpc": "2.0",
                  "id": 1,
                  "method": "tools/list"
                }
                """,
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("ProtocolVersion", "2025-06-18");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        return request;
    }

    private static HttpRequestMessage CreateMcpToolCallRequest(string toolName, object arguments)
    {
        var requestBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("ProtocolVersion", "2025-06-18");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        return request;
    }

    private static async Task<IReadOnlyList<string>> ReadToolNamesAsync(HttpResponseMessage response)
    {
        using var document = await ReadJsonRpcDocumentAsync(response);
        var result = document.RootElement.GetProperty("result");

        return result.GetProperty("tools")
            .EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToArray();
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
}
