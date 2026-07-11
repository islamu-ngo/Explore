// ABOUTME: Integration tests for antiforgery coverage on preference BFF mutation endpoints.
// ABOUTME: Proves unsafe cookie-authenticated BFF requests require the X-CSRF-TOKEN header.

using System.Net;
using System.Text.Json;
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

    [Test]
    public async Task ThemePreference_AnonymousWithValidAntiforgery_PersistsThemeCookieAndReturnsUpdatedPreference()
    {
        var token = await IssueAntiforgeryCookieAsync();

        using var request = CreateMutationRequest(HttpMethod.Post, "/bff/theme?theme=dark", token);
        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("themeMode").GetString().Should().Be("dark");
        GetSetCookieHeaders(response).Should().Contain(cookie => cookie.StartsWith("theme=dark", StringComparison.Ordinal));
    }

    [Test]
    public async Task LanguagePreference_AnonymousWithValidAntiforgery_PersistsLangAndAspNetCultureCookies()
    {
        var token = await IssueAntiforgeryCookieAsync();

        using var request = CreateMutationRequest(HttpMethod.Post, "/bff/language?lang=fr", token);
        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("language").GetString().Should().Be("fr");
        var cookies = GetSetCookieHeaders(response);
        cookies.Should().Contain(cookie => cookie.StartsWith("lang=fr", StringComparison.Ordinal));
        cookies.Should().Contain(cookie => cookie.StartsWith(".AspNetCore.Culture=", StringComparison.Ordinal));
    }

    [Test]
    public async Task DirectionPreference_AnonymousAuto_DeletesDirectionCookie()
    {
        var token = await IssueAntiforgeryCookieAsync();

        using (var setRtlRequest = CreateMutationRequest(HttpMethod.Post, "/bff/direction?dir=rtl", token))
        using (var setRtlResponse = await _client.SendAsync(setRtlRequest))
        {
            setRtlResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            GetSetCookieHeaders(setRtlResponse).Should().Contain(cookie => cookie.StartsWith("direction=rtl", StringComparison.Ordinal));
        }

        using var autoRequest = CreateMutationRequest(HttpMethod.Post, "/bff/direction?dir=auto", token);
        using var autoResponse = await _client.SendAsync(autoRequest);

        autoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await ReadJsonAsync(autoResponse);
        json.RootElement.GetProperty("direction").GetString().Should().Be("auto");
        GetSetCookieHeaders(autoResponse).Should().Contain(cookie =>
            cookie.StartsWith("direction=", StringComparison.Ordinal) &&
            cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task ClonePreset_AnonymousWithValidAntiforgery_ReturnsUnauthorized()
    {
        var token = await IssueAntiforgeryCookieAsync();
        var presetId = Guid.NewGuid();

        using var request = CreateMutationRequest(HttpMethod.Post, $"/bff/appearance/profiles/from-preset/{presetId}", token);
        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

    private static HttpRequestMessage CreateMutationRequest(HttpMethod method, string requestUri, string token)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-CSRF-TOKEN", token);
        return request;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body);
    }

    private static IReadOnlyCollection<string> GetSetCookieHeaders(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Set-Cookie", out var values).Should().BeTrue();
        return values!.ToArray();
    }
}
