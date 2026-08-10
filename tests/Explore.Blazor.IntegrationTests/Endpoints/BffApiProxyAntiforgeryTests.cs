// ABOUTME: Regression tests for antiforgery enforcement on authenticated and anonymous unsafe BFF API proxy mutations.
// ABOUTME: Proves missing tokens fail, valid tokens proxy, and safe reads remain unblocked.

using System.Net;
using Explore.Blazor.IntegrationTests.Fixtures;
using Explore.Blazor.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Explore.Blazor.IntegrationTests.Endpoints;

[NotInParallel("BffApiProxySetupSecret")]
public sealed class BffApiProxyAntiforgeryTests : IAsyncDisposable
{
    private readonly ProxyUpstream _upstream = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _authHeader = TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid(), "WebPush Tester");

    public BffApiProxyAntiforgeryTests()
    {
        _upstream.StartAsync().GetAwaiter().GetResult();
        _factory = new BlazorBffWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ExploreApi:BaseUrl", _upstream.BaseAddress);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISetupSecretResolver>();
                services.AddSingleton<ISetupSecretResolver>(new FixedSetupSecretResolver(
                    "trusted-yarp-instance-settings-secret"));
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Test]
    public async Task WebPushConfig_GetWithoutAntiforgeryHeader_ProxiesToApi()
    {
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/notification/web-push/config");

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("publicKey");
    }

    [Test]
    public async Task WebPushSubscribe_PostWithoutAntiforgeryHeader_ReturnsBadRequest()
    {
        await IssueAntiforgeryCookieAsync();
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/notification/web-push/subscriptions");
        request.Content = JsonContent.Create(new { endpoint = "https://push.example.test/device" });

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Antiforgery validation failed");
    }

    [Test]
    public async Task WebPushUnsubscribe_DeleteWithoutAntiforgeryHeader_ReturnsBadRequest()
    {
        await IssueAntiforgeryCookieAsync();
        using var request = CreateAuthenticatedRequest(HttpMethod.Delete, $"/api/notification/web-push/subscriptions/{Guid.NewGuid()}");

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Antiforgery validation failed");
    }

    [Test]
    public async Task WebPushSubscribe_PostWithValidAntiforgeryHeader_ProxiesToApi()
    {
        var antiforgery = await IssueAntiforgeryCookieAsync();
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/notification/web-push/subscriptions");
        request.Headers.Remove("Cookie");
        request.Headers.Add("Cookie", antiforgery.CookieHeader);
        request.Headers.Add("X-CSRF-TOKEN", antiforgery.Token);
        request.Content = JsonContent.Create(new { endpoint = "https://push.example.test/device" });

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadAsStringAsync()).Should().Contain("subscribed");
    }

    [Test]
    public async Task AnonymousProxyPost_WithoutAntiforgeryHeader_ReturnsBadRequest()
    {
        var antiforgery = await IssueAntiforgeryCookieAsync(authenticated: false);
        using var request = CreateAnonymousProxyRequest(HttpMethod.Post, "/api/public-transactional-test");
        request.Headers.Add("Cookie", antiforgery.CookieHeader);

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Antiforgery validation failed");
    }

    [Test]
    public async Task AnonymousProxyPost_WithValidAntiforgeryHeader_ProxiesToApi()
    {
        var antiforgery = await IssueAntiforgeryCookieAsync(authenticated: false);
        using var request = CreateAnonymousProxyRequest(HttpMethod.Post, "/api/public-transactional-test");
        request.Headers.Add("Cookie", antiforgery.CookieHeader);
        request.Headers.Add("X-CSRF-TOKEN", antiforgery.Token);

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    [Arguments("/api/instance/settings/auth-provider", null)]
    [Arguments("/api/instance/settings/auth-provider", "invalid-token")]
    [Arguments("/api/InstanceOnboarding/auth-provider-configuration", null)]
    [Arguments("/api/InstanceOnboarding/auth-provider-configuration", "invalid-token")]
    public async Task CookieSetupAndOnboardingPatch_WithoutValidAntiforgery_DoesNotReachApi(
        string path,
        string? token)
    {
        _upstream.ResetCapture();
        var antiforgery = await IssueAntiforgeryCookieAsync();
        using var request = CreateAuthenticatedRequest(HttpMethod.Patch, path);
        request.Headers.Remove("Cookie");
        request.Headers.Add("Cookie", antiforgery.CookieHeader);
        if (token is not null)
        {
            request.Headers.Add("X-CSRF-TOKEN", token);
        }

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _upstream.LastPathAndQuery.Should().BeNull();
    }

    [Test]
    public async Task CookieSetupPatch_WithValidAntiforgery_ReachesApiWithTrustedEnrichment()
    {
        _upstream.ResetCapture();
        var antiforgery = await IssueAntiforgeryCookieAsync();
        using var request = CreateAuthenticatedRequest(
            HttpMethod.Patch,
            "/api/instance/settings/auth-provider");
        request.Headers.Remove("Cookie");
        request.Headers.Add("Cookie", antiforgery.CookieHeader);
        request.Headers.Add("X-CSRF-TOKEN", antiforgery.Token);

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _upstream.LastPathAndQuery.Should().Be("/api/instance/settings/auth-provider");
        _upstream.LastSetupSecret.Should().Be("trusted-yarp-instance-settings-secret");
    }

    [Test]
    [Arguments("/api/instance/settings/auth-provider")]
    [Arguments("/api/instance/settings/authz-provider")]
    public async Task InstanceProviderPatch_CanonicalPath_ForwardsOnlyResolverSecret(string path)
    {
        _upstream.ResetCapture();
        using var request = CreateProxyRequest(HttpMethod.Patch, $"{path}?source=test");
        request.Headers.Add("X-Setup-Secret", "browser-controlled-secret");

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _upstream.LastPathAndQuery.Should().Be($"{path}?source=test");
        _upstream.LastSetupSecret.Should().Be("trusted-yarp-instance-settings-secret");
    }

    [Test]
    [Arguments("GET", "/api/instance/settings/auth-provider")]
    [Arguments("PUT", "/api/instance/settings/auth-provider")]
    [Arguments("DELETE", "/api/instance/settings/auth-provider")]
    [Arguments("PATCH", "/api/instance/settings/auth-provider/")]
    [Arguments("PATCH", "/api/instance/settings/auth-provider/child")]
    [Arguments("PATCH", "/api/instance/settings/auth-provider-extra")]
    [Arguments("GET", "/api/instance/settings/authz-provider")]
    [Arguments("PUT", "/api/instance/settings/authz-provider")]
    [Arguments("DELETE", "/api/instance/settings/authz-provider")]
    [Arguments("PATCH", "/api/instance/settings/authz-provider/")]
    [Arguments("PATCH", "/api/instance/settings/authz-provider/child")]
    [Arguments("PATCH", "/api/instance/settings/authz-provider-extra")]
    public async Task InstanceProviderRequest_NonCanonicalMethodOrPath_StripsSetupSecret(
        string method,
        string path)
    {
        _upstream.ResetCapture();
        using var request = CreateProxyRequest(new HttpMethod(method), $"{path}?source=test");
        if (!string.Equals(method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase))
        {
            var antiforgery = await IssueAntiforgeryCookieAsync();
            request.Headers.Add("Cookie", antiforgery.CookieHeader);
            request.Headers.Add("X-CSRF-TOKEN", antiforgery.Token);
        }

        request.Headers.Add("X-Setup-Secret", "browser-controlled-secret");

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _upstream.LastPathAndQuery.Should().Be($"{path}?source=test");
        _upstream.LastSetupSecret.Should().BeNull();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _upstream.DisposeAsync();
    }

    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(TestAuthHandler.AuthHeaderName, _authHeader);
        request.Headers.Add("Cookie", ".AspNetCore.Cookies=test-session");
        return request;
    }

    private HttpRequestMessage CreateProxyRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(TestAuthHandler.AuthHeaderName, _authHeader);
        return request;
    }

    private static HttpRequestMessage CreateAnonymousProxyRequest(HttpMethod method, string path)
        => new(method, path);

    private async Task<AntiforgeryCookie> IssueAntiforgeryCookieAsync(bool authenticated = true)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/status");
        if (authenticated)
        {
            request.Headers.Add(TestAuthHandler.AuthHeaderName, _authHeader);
        }

        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Headers.TryGetValues("Set-Cookie", out var values).Should().BeTrue();
        var setCookies = values!.ToArray();
        var token = setCookies
            .Select(ReadXsrfToken)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        token.Should().NotBeNullOrWhiteSpace("GET requests should issue the readable XSRF-TOKEN cookie");
        return new AntiforgeryCookie(token!, BuildCookieHeader(setCookies, authenticated));
    }

    private static string BuildCookieHeader(IEnumerable<string> setCookies, bool includeBffAuthCookie = true)
    {
        var cookies = setCookies.Select(setCookie => setCookie.Split(';', 2)[0]);
        if (includeBffAuthCookie)
        {
            cookies = cookies.Append(".AspNetCore.Cookies=test-session");
        }

        return string.Join("; ", cookies);
    }

    private static string? ReadXsrfToken(string setCookie)
    {
        const string prefix = "XSRF-TOKEN=";
        if (!setCookie.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var end = setCookie.IndexOf(';', prefix.Length);
        var rawValue = end < 0 ? setCookie[prefix.Length..] : setCookie[prefix.Length..end];
        return Uri.UnescapeDataString(rawValue);
    }

    private sealed record AntiforgeryCookie(string Token, string CookieHeader);

    private sealed class ProxyUpstream : IAsyncDisposable
    {
        private WebApplication? _app;

        public string BaseAddress { get; private set; } = string.Empty;

        public string? LastPathAndQuery { get; private set; }

        public string? LastSetupSecret { get; private set; }

        public async Task StartAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");

            _app = builder.Build();
            _app.MapGet("/api/notification/web-push/config", () => Results.Json(new { publicKey = "test-public-key" }));
            _app.MapPost("/api/notification/web-push/subscriptions", () => Results.Json(new { status = "subscribed" }, statusCode: StatusCodes.Status201Created));
            _app.MapDelete("/api/notification/web-push/subscriptions/{subscriptionId:guid}", () => Results.Json(new { status = "unsubscribed" }));
            _app.Map("/{**path}", (HttpContext context) =>
            {
                LastPathAndQuery = $"{context.Request.Path}{context.Request.QueryString}";
                LastSetupSecret = context.Request.Headers["X-Setup-Secret"].FirstOrDefault();
                return Results.Ok();
            });

            await _app.StartAsync();
            BaseAddress = _app.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()!
                .Addresses
                .Single();
        }

        public void ResetCapture()
        {
            LastPathAndQuery = null;
            LastSetupSecret = null;
        }

        public async ValueTask DisposeAsync()
        {
            if (_app is not null)
            {
                await _app.DisposeAsync();
            }
        }
    }

    private sealed class FixedSetupSecretResolver(string? secret) : ISetupSecretResolver
    {
        public SetupSecretResolutionResult Resolve(
            HttpContext? httpContext = null,
            HttpRequestMessage? outboundRequest = null)
        {
            return string.IsNullOrWhiteSpace(secret)
                ? SetupSecretResolutionResult.NotFound("test_secret_missing")
                : SetupSecretResolutionResult.FoundFrom(SetupSecretSource.ServerSideSetupSession, secret);
        }
    }
}
