// ABOUTME: Integration tests for antiforgery coverage on preference BFF mutation endpoints.
// ABOUTME: Proves unsafe cookie-authenticated BFF requests require the X-CSRF-TOKEN header.

using System.Net;
using Explore.Blazor.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class BffPreferenceAntiforgeryTests : IAsyncDisposable
{
    private readonly BlazorBffWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public BffPreferenceAntiforgeryTests()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Test]
    public async Task ThemePreference_PostWithoutAntiforgeryHeader_ReturnsBadRequest()
    {
        await IssueAntiforgeryCookieAsync();

        using var response = await _client.PostAsync("/bff/theme?theme=dark", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Antiforgery validation failed");
    }

    [Test]
    public async Task ThemePreference_PostWithInvalidAntiforgeryHeader_ReturnsBadRequest()
    {
        await IssueAntiforgeryCookieAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/bff/theme?theme=dark");
        request.Headers.Add("X-CSRF-TOKEN", "invalid-token");

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Antiforgery validation failed");
    }

    [Test]
    public async Task ThemePreference_PostWithValidAntiforgeryHeader_ReturnsOk()
    {
        var token = await IssueAntiforgeryCookieAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/bff/theme?theme=dark");
        request.Headers.Add("X-CSRF-TOKEN", token);

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<string> IssueAntiforgeryCookieAsync()
    {
        using var response = await _client.GetAsync("/auth/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Headers.TryGetValues("Set-Cookie", out var values).Should().BeTrue();

        var token = values!
            .Select(ReadXsrfToken)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        token.Should().NotBeNullOrWhiteSpace("GET requests should issue the readable XSRF-TOKEN cookie");
        return token!;
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
}
