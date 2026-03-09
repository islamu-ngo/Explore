// ABOUTME: Integration tests for the external API Phase 0 authentication and tenant seam.
// ABOUTME: Covers direct JWT, API-key-derived tenant resolution, mismatch rejection, and single-tenant short-circuit behavior.

using System.Net;
using System.Net.Http.Json;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Authentication;
using Explore.Domain.Constants;
using Explore.Infrastructure;

namespace Event.Api.IntegrationTests.Features;

public sealed class ExternalApiPhase0IntegrationTests
{
    private const string ProbeUrl = "/api/_internal/auth-probe/secure";

    [Test]
    public async Task SecureProbe_WithBearerToken_AndTenantSlug_ReturnsResolvedJwtContext()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.MultiTenant,
            TenantSlugMappings = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
            {
                ["alpha"] = tenantId
            }
        };

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ProbeUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", factory.CreateJwt(userId));
        request.Headers.Add("X-Tenant-Slug", "alpha");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<AuthProbeResult>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.AuthMethod).IsEqualTo("jwt");
        await Assert.That(body.UserId).IsEqualTo(userId);
        await Assert.That(body.TenantId).IsEqualTo(tenantId);
        await Assert.That(body.ApiKeyId).IsNull();
    }

    [Test]
    public async Task SecureProbe_WithApiKey_DerivesTenantFromKey()
    {
        var tenantId = Guid.NewGuid();
        const string rawApiKey = "phase0-live-key";

        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.MultiTenant,
            ApiKeyClients =
            [
                new ApiKeyClientDescriptor
                {
                    KeyId = "key-alpha",
                    TenantId = tenantId,
                    OwnerType = "Organization",
                    OwnerId = Guid.NewGuid().ToString(),
                    Scopes = ["events:write"],
                    SecretHash = ApiKeyHashing.ComputeHash(rawApiKey)
                }
            ]
        };

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ProbeUrl);
        request.Headers.Add("X-API-Key", rawApiKey);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<AuthProbeResult>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.AuthMethod).IsEqualTo("api_key");
        await Assert.That(body.ApiKeyId).IsEqualTo("key-alpha");
        await Assert.That(body.TenantId).IsEqualTo(tenantId);
        await Assert.That(body.UserId).IsNull();
    }

    [Test]
    public async Task SecureProbe_WithApiKey_AndConflictingTenantSlug_ReturnsNotFound()
    {
        var apiKeyTenantId = Guid.NewGuid();
        var hintedTenantId = Guid.NewGuid();
        const string rawApiKey = "phase0-conflict-key";

        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.MultiTenant,
            TenantSlugMappings = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
            {
                ["beta"] = hintedTenantId
            },
            ApiKeyClients =
            [
                new ApiKeyClientDescriptor
                {
                    KeyId = "key-conflict",
                    TenantId = apiKeyTenantId,
                    OwnerType = "User",
                    OwnerId = Guid.NewGuid().ToString(),
                    Scopes = ["events:read"],
                    SecretHash = ApiKeyHashing.ComputeHash(rawApiKey)
                }
            ]
        };

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ProbeUrl);
        request.Headers.Add("X-API-Key", rawApiKey);
        request.Headers.Add("X-Tenant-Slug", "beta");

        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task SecureProbe_InSingleTenantMode_DoesNotRequireTenantMaterial()
    {
        var userId = Guid.NewGuid();

        await using var factory = new ExternalApiPhase0WebApplicationFactory
        {
            DeploymentMode = DeploymentMode.SingleTenant,
            DefaultTenantId = PlatformDefaults.DefaultTenantId
        };

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ProbeUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", factory.CreateJwt(userId));

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<AuthProbeResult>();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.TenantId).IsEqualTo(PlatformDefaults.DefaultTenantId);
        await Assert.That(body.AuthMethod).IsEqualTo("jwt");
        await Assert.That(body.UserId).IsEqualTo(userId);
    }

    private sealed class AuthProbeResult
    {
        public string? AuthMethod { get; set; }

        public string? ApiKeyId { get; set; }

        public Guid? TenantId { get; set; }

        public Guid? UserId { get; set; }
    }
}
