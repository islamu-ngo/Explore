// ABOUTME: Regression tests for antiforgery enforcement on cookie-authenticated BFF API proxy mutations.
// ABOUTME: Proves WebPush generated-client routes reject missing CSRF tokens while safe reads still proxy.

using System.Net;
using Explore.Blazor.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Blazor.IntegrationTests.Endpoints;

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

    private async Task<AntiforgeryCookie> IssueAntiforgeryCookieAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/status");
        request.Headers.Add(TestAuthHandler.AuthHeaderName, _authHeader);
        using var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Headers.TryGetValues("Set-Cookie", out var values).Should().BeTrue();
        var setCookies = values!.ToArray();
        var token = setCookies
            .Select(ReadXsrfToken)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        token.Should().NotBeNullOrWhiteSpace("GET requests should issue the readable XSRF-TOKEN cookie");
        return new AntiforgeryCookie(token!, BuildCookieHeader(setCookies));
    }

    private static string BuildCookieHeader(IEnumerable<string> setCookies)
    {
        var cookies = setCookies
            .Select(setCookie => setCookie.Split(';', 2)[0])
            .Append(".AspNetCore.Cookies=test-session");

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

        public async Task StartAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");

            _app = builder.Build();
            _app.MapGet("/api/notification/web-push/config", () => Results.Json(new { publicKey = "test-public-key" }));
            _app.MapPost("/api/notification/web-push/subscriptions", () => Results.Json(new { status = "subscribed" }, statusCode: StatusCodes.Status201Created));
            _app.MapDelete("/api/notification/web-push/subscriptions/{subscriptionId:guid}", () => Results.Json(new { status = "unsubscribed" }));

            await _app.StartAsync();
            BaseAddress = _app.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()!
                .Addresses
                .Single();
        }

        public async ValueTask DisposeAsync()
        {
            if (_app is not null)
            {
                await _app.DisposeAsync();
            }
        }
    }
}
