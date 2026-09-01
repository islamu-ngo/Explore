// ABOUTME: Verifies post-login user synchronization precedes BFF administrative authority resolution.
// ABOUTME: Covers authoritative internal IDs, sync failures, bearer forwarding, and stale authority cache invalidation.

using System.Text;
using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace Explore.Blazor.IntegrationTests.Services;

public sealed class BffAdminClaimsTransformationTests
{
    private static readonly Guid InternalUserId = Guid.Parse("0190f50d-1690-7000-8000-000000000001");

    [Test]
    public async Task EnrichPrincipalAsync_WhenSynchronizing_SyncsBeforeAuthorityAndUsesSameBearer()
    {
        var handler = new IdentityReadinessHandler();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var client = new HttpClient(handler) { BaseAddress = new("https://api.example/") };
        var service = CreateService(client, cache);
        var principal = CreatePrincipal();

        var result = await service.EnrichPrincipalAsync(
            principal,
            CreateProperties(),
            synchronizeUser: true);

        await Assert.That(result).IsTrue();
        await Assert.That(handler.Requests.Count).IsEqualTo(2);
        await Assert.That(handler.Requests[0].Method).IsEqualTo(HttpMethod.Post.Method);
        await Assert.That(handler.Requests[0].Path).IsEqualTo("/api/user/sync");
        await Assert.That(handler.Requests[1].Method).IsEqualTo(HttpMethod.Get.Method);
        await Assert.That(handler.Requests[1].Path).IsEqualTo("/api/user/admin-authority");
        await Assert.That(handler.Requests.All(request => request.Authorization == "Bearer access-token")).IsTrue();
        await Assert.That(principal.FindFirst("internal_user_id")?.Value).IsEqualTo(InternalUserId.ToString());
        await Assert.That(principal.HasClaim("explore:admin:instance", "true")).IsTrue();
    }

    [Test]
    public async Task EnrichPrincipalAsync_WhenSyncReturnsBadRequest_StillResolvesAuthority()
    {
        var handler = new IdentityReadinessHandler { SyncStatusCode = HttpStatusCode.BadRequest };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var client = new HttpClient(handler) { BaseAddress = new("https://api.example/") };
        var service = CreateService(client, cache);
        var principal = CreatePrincipal();

        var result = await service.EnrichPrincipalAsync(
            principal,
            CreateProperties(),
            synchronizeUser: true);

        await Assert.That(result).IsTrue();
        await Assert.That(handler.Requests.Count).IsEqualTo(2);
        await Assert.That(handler.Requests[0].Path).IsEqualTo("/api/user/sync");
        await Assert.That(handler.Requests[1].Path).IsEqualTo("/api/user/admin-authority");
        await Assert.That(principal.FindFirst("internal_user_id")).IsNull();
        await Assert.That(principal.HasClaim("explore:admin:instance", "true")).IsTrue();
    }

    [Test]
    public async Task EnrichPrincipalAsync_AfterSuccessfulSync_InvalidatesStaleNegativeAuthorityCache()
    {
        var handler = new IdentityReadinessHandler
        {
            Authority = new AdminAuthorityDto { HasAnyAuthority = false }
        };
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var client = new HttpClient(handler) { BaseAddress = new("https://api.example/") };
        var service = CreateService(client, cache);
        var principal = CreatePrincipal();
        var properties = CreateProperties();

        var initialResult = await service.EnrichPrincipalAsync(principal, properties);
        await Assert.That(initialResult).IsFalse();

        handler.Requests.Clear();
        handler.Authority = CreateAdminAuthority();

        var synchronizedResult = await service.EnrichPrincipalAsync(
            principal,
            properties,
            synchronizeUser: true);

        await Assert.That(synchronizedResult).IsTrue();
        await Assert.That(handler.Requests.Count).IsEqualTo(2);
        await Assert.That(handler.Requests[0].Path).IsEqualTo("/api/user/sync");
        await Assert.That(handler.Requests[1].Path).IsEqualTo("/api/user/admin-authority");
    }

    [Test]
    public async Task EnrichPrincipalAsyncUsesProviderSubjectAcrossSessionChanges()
    {
        using var handler = new IdentityReadinessHandler();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var client = new HttpClient(handler) { BaseAddress = new("https://api.example/") };
        var service = CreateService(client, cache);
        var first = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "provider-subject"),
            new Claim("sid", "session-one")
        ], "Cookies"));
        var second = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "provider-subject"),
            new Claim("sid", "session-two")
        ], "Cookies"));

        var firstResult = await service.EnrichPrincipalAsync(first, CreateProperties());
        handler.Authority = new AdminAuthorityDto { HasAnyAuthority = false };
        var secondResult = await service.EnrichPrincipalAsync(second, CreateProperties());

        await Assert.That(firstResult).IsTrue();
        await Assert.That(secondResult).IsTrue();
        await Assert.That(second.HasClaim("explore:admin:instance", "true")).IsTrue();
        await Assert.That(handler.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task EnrichPrincipalAsyncWithOnlyPlatformIdentityClaimFailsClosed()
    {
        using var handler = new IdentityReadinessHandler();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var client = new HttpClient(handler) { BaseAddress = new("https://api.example/") };
        var service = CreateService(client, cache);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("internal_user_id", InternalUserId.ToString("D"))
        ], "Cookies"));

        var result = await service.EnrichPrincipalAsync(principal, CreateProperties());

        await Assert.That(result).IsFalse();
        await Assert.That(principal.HasClaim("explore:admin:instance", "true")).IsFalse();
        await Assert.That(handler.Requests).IsEmpty();
    }


    [Test]
    public async Task EnrichPrincipalAsyncSupportsSidOnlyProviderSessionFallback()
    {
        using var handler = new IdentityReadinessHandler();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        using var client = new HttpClient(handler) { BaseAddress = new("https://api.example/") };
        var service = CreateService(client, cache);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sid", "session-only-subject")
        ], "Cookies"));

        var result = await service.EnrichPrincipalAsync(principal, CreateProperties());

        await Assert.That(result).IsTrue();
        await Assert.That(principal.HasClaim("explore:admin:instance", "true")).IsTrue();
        await Assert.That(handler.Requests.Count).IsEqualTo(1);
    }
    private static BffAdminClaimsTransformation CreateService(HttpClient client, IMemoryCache cache)
    {
        var onboardingStatusProvider = Substitute.For<IBffOnboardingStatusProvider>();
        onboardingStatusProvider.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new BffOnboardingStatus(IsCompleted: true, IsSetupModeActive: false, Known: true));
        return new(
            new FixedHttpClientFactory(client),
            cache,
            onboardingStatusProvider,
            NullLogger<BffAdminClaimsTransformation>.Instance);
    }

    private static ClaimsPrincipal CreatePrincipal() => new(
        new ClaimsIdentity([new Claim("sub", "provider-user")], "Cookies"));

    private static AuthenticationProperties CreateProperties()
    {
        var properties = new AuthenticationProperties();
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "access-token" }
        ]);
        return properties;
    }

    private static AdminAuthorityDto CreateAdminAuthority() => new()
    {
        IsInstanceAdmin = true,
        HasAnyAuthority = true,
        AdminTenantIds = [],
        AdminOrganizationIds = [],
        AdminGroupIds = []
    };

    private sealed class FixedHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class IdentityReadinessHandler : HttpMessageHandler
    {
        public HttpStatusCode SyncStatusCode { get; init; } = HttpStatusCode.OK;
        public AdminAuthorityDto Authority { get; set; } = CreateAdminAuthority();
        public List<CapturedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new(
                request.Method.Method,
                request.RequestUri!.AbsolutePath,
                request.Headers.Authorization?.ToString()));

            if (request.Method == HttpMethod.Post && request.RequestUri.AbsolutePath == "/api/user/sync")
            {
                return Task.FromResult(JsonResponse(
                    SyncStatusCode,
                    new BaseCommandResponseOfGuid { Success = true, Id = InternalUserId }));
            }

            if (request.Method == HttpMethod.Get && request.RequestUri.AbsolutePath == "/api/user/admin-authority")
            {
                return Task.FromResult(JsonResponse(HttpStatusCode.OK, Authority));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage JsonResponse<T>(HttpStatusCode statusCode, T value) => new(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
        };
    }

    private sealed record CapturedRequest(string Method, string Path, string? Authorization);
}
