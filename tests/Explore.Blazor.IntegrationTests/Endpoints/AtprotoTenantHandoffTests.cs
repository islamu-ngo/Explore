// ABOUTME: Exercises the origin-bound one-time AT Protocol tenant handoff through the real BFF endpoint.
// ABOUTME: Proves protected cookie claims/tokens, opaque redirects, host binding, replay rejection, and cookie flags.

using System.Net;
using System.Security.Claims;
using Explore.Blazor.Authentication;
using Explore.Blazor.IntegrationTests.Fixtures;
using Explore.Blazor.Services;
using Explore.Blazor.Services.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class AtprotoTenantHandoffTests
{
    private const string TenantOrigin = "https://tenant.example.com";
    private const string PlatformAccessToken = "opaque-platform-jwt-secret";
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid UserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000002");

    [Test]
    public async Task HandoffSignsVerifiedProtectedCookieWithoutExposingSessionMaterial()
    {
        await using var factory = CreateFactory();
        var code = await CreateHandoffAsync(factory);
        code.Should().HaveLength(43);
        code.Should().NotContain(PlatformAccessToken);
        var response = await InvokeHandoffEndpointAsync(factory, TenantOrigin, code);

        response.StatusCode.Should().Be(StatusCodes.Status302Found);
        response.Location.Should().Be("/events?source=atproto");
        response.CacheControl.Should().Contain("no-store");
        response.Body.Should().BeEmpty();
        var cookieHeader = response.SetCookies.Single(value =>
            value.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal));
        var normalizedCookieHeader = cookieHeader.ToLowerInvariant();
        normalizedCookieHeader.Should().Contain("; path=/");
        normalizedCookieHeader.Should().Contain("; secure");
        normalizedCookieHeader.Should().Contain("; httponly");
        normalizedCookieHeader.Should().Contain("; samesite=lax");

        var browserVisible = string.Join('\n', response.SetCookies.Append(response.Location ?? string.Empty)) + response.Body;
        foreach (var secret in new[]
        {
            PlatformAccessToken,
            "pds-access-token",
            "pds-refresh-token",
            "dpop-private-key",
            "oauth-session-json",
            "did:plc:alice",
            UserId.ToString("D")
        })
        {
            browserVisible.Should().NotContain(secret);
        }

        var ticket = UnprotectCookieTicket(factory, cookieHeader);
        ticket.Should().NotBeNull();
        ticket!.Principal.FindFirstValue("sub").Should().Be(UserId.ToString("D"));
        ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(UserId.ToString("D"));
        ticket.Principal.FindFirstValue("did").Should().Be("did:plc:alice");
        ticket.Principal.FindFirstValue("tenant_id").Should().Be(TenantId.ToString("D"));
        ticket.Principal.FindFirstValue("auth_provider").Should().Be("atproto");
        ticket.Properties.GetTokenValue("access_token").Should().Be(PlatformAccessToken);
        ticket.Properties.GetTokenValue("token_type").Should().Be("Bearer");
        ticket.Properties.GetTokenValue("expires_at").Should().NotBeNullOrWhiteSpace();
        ticket.Properties.AllowRefresh.Should().BeTrue();
        ticket.Properties.IsPersistent.Should().BeTrue();
    }

    [Test]
    public async Task HostSubstitutionConsumesHandoffAndReplayFailsClosed()
    {
        await using var factory = CreateFactory();
        var code = await CreateHandoffAsync(factory);
        using var attackerClient = CreateClient(factory, "https://attacker.example.com");
        using var tenantClient = CreateClient(factory, TenantOrigin);

        using var substituted = await attackerClient.GetAsync($"/auth/atproto/handoff?code={code}");
        using var replay = await tenantClient.GetAsync($"/auth/atproto/handoff?code={code}");

        AssertSafeHandoffFailure(substituted);
        AssertSafeHandoffFailure(replay);
    }

    [Test]
    public async Task MalformedHandoffNeverReflectsCodeOrCredentialMaterial()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory, TenantOrigin);
        const string supplied = "access_token=browser-secret";

        using var response = await client.GetAsync($"/auth/atproto/handoff?code={Uri.EscapeDataString(supplied)}");

        AssertSafeHandoffFailure(response);
        response.Headers.Location!.OriginalString.Should().NotContain(supplied);
        response.Headers.Location.OriginalString.Should().NotContain("browser-secret");
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new BlazorBffWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Atproto:PublicUrl", "https://events.example.com");
            builder.UseSetting("Atproto:CallbackPath", "/signin-atproto");
            builder.UseSetting("Atproto:UseSingleNodeMemoryStore", "true");
            builder.UseSetting("Atproto:HandoffLifetimeSeconds", "60");
            builder.UseSetting("Atproto:TenantOrigins:0:Origin", TenantOrigin);
            builder.UseSetting("Atproto:TenantOrigins:0:TenantId", TenantId.ToString("D"));
            builder.UseSetting("Atproto:TenantOrigins:0:TenantSlug", "default");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IConnectionMultiplexer>();
                services.AddScoped<BffAdminClaimsTransformation>();
            });
        });

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, string origin) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri(origin),
            HandleCookies = false
        });

    private static async Task<EndpointResponse> InvokeHandoffEndpointAsync(
        WebApplicationFactory<Program> factory,
        string origin,
        string code)
    {
        var endpoint = factory.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == "/auth/atproto/handoff");
        await using var scope = factory.Services.CreateAsyncScope();
        var originUri = new Uri(origin);
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        context.SetEndpoint(endpoint);
        context.Request.Method = HttpMethods.Get;
        context.Request.Scheme = originUri.Scheme;
        context.Request.Host = new HostString(originUri.Authority);
        context.Request.Path = "/auth/atproto/handoff";
        context.Request.QueryString = new QueryString($"?code={Uri.EscapeDataString(code)}");
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return new EndpointResponse(
            context.Response.StatusCode,
            context.Response.Headers.Location.ToString(),
            context.Response.Headers.CacheControl.ToString(),
            context.Response.Headers.SetCookie.Select(value => value!).ToArray(),
            await reader.ReadToEndAsync());
    }

    private static async Task<string> CreateHandoffAsync(WebApplicationFactory<Program> factory)
    {
        var store = factory.Services.GetRequiredService<AtprotoTenantSessionHandoffStore>();
        return await store.CreateAsync(
            new AtprotoOAuthFlowSeed(
                "did:plc:alice",
                new Uri("https://pds.example.com/"),
                TenantId,
                "default",
                new Uri($"{TenantOrigin}/"),
                "/events?source=atproto",
                "oauth-active"),
            new AtprotoBffSessionResult(
                UserId,
                "did:plc:alice",
                PlatformAccessToken,
                DateTimeOffset.UtcNow.AddMinutes(10)),
            CancellationToken.None);
    }

    private static AuthenticationTicket? UnprotectCookieTicket(
        WebApplicationFactory<Program> factory,
        string setCookie)
    {
        const string prefix = ".AspNetCore.Cookies=";
        var end = setCookie.IndexOf(';', prefix.Length);
        var encoded = end < 0 ? setCookie[prefix.Length..] : setCookie[prefix.Length..end];
        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        return options.TicketDataFormat.Unprotect(Uri.UnescapeDataString(encoded));
    }

    private static void AssertSafeHandoffFailure(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.CacheControl?.NoStore.Should().BeTrue();
        response.Headers.Location?.OriginalString.Should().Be("/login?provider=atproto&challengeError=1");
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            cookies.Should().NotContain(value =>
                value.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal));
        }
    }

    private sealed record EndpointResponse(
        int StatusCode,
        string? Location,
        string CacheControl,
        IReadOnlyList<string> SetCookies,
        string Body);
}
