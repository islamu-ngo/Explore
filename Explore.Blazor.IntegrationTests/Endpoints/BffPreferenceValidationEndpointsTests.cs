// ABOUTME: Endpoint-level tests for browser-facing BFF preference validation.
// ABOUTME: Proves invalid preference values are rejected before cookies or API forwarding.

using Explore.Application.DTOs.Appearance;
using Explore.Blazor.Services.Preferences;
using FluentAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class BffPreferenceValidationEndpointsTests : IAsyncDisposable
{
    private readonly BlazorBffWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BffPreferenceValidationEndpointsTests()
    {
        _factory = new BlazorBffWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Test]
    public async Task ThemePreference_InvalidTheme_ReturnsBadRequestAndDoesNotPersistCookie()
    {
        var token = await IssueAntiforgeryCookieAsync(_client);
        using var request = CreateMutationRequest(HttpMethod.Post, "/bff/theme?theme=sepia", token);

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Theme mode must be one of");
        GetSetCookieHeaders(response).Should().NotContain(cookie => cookie.StartsWith("theme=", StringComparison.Ordinal));
    }

    [Test]
    public async Task LanguagePreference_InvalidLanguage_ReturnsBadRequestAndDoesNotPersistCookie()
    {
        var token = await IssueAntiforgeryCookieAsync(_client);
        using var request = CreateMutationRequest(HttpMethod.Post, "/bff/language?lang=zz", token);

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Language must be a supported culture code");
        GetSetCookieHeaders(response).Should().NotContain(cookie => cookie.StartsWith("lang=", StringComparison.Ordinal));
        GetSetCookieHeaders(response).Should().NotContain(cookie => cookie.StartsWith(".AspNetCore.Culture=", StringComparison.Ordinal));
    }

    [Test]
    public async Task DirectionPreference_InvalidDirection_ReturnsBadRequestAndDoesNotPersistCookie()
    {
        var token = await IssueAntiforgeryCookieAsync(_client);
        using var request = CreateMutationRequest(HttpMethod.Post, "/bff/direction?dir=sideways", token);

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Direction must be");
        GetSetCookieHeaders(response).Should().NotContain(cookie => cookie.StartsWith("direction=", StringComparison.Ordinal));
    }

    [Test]
    public async Task AppearanceMode_AuthenticatedInvalidThemeMode_ReturnsBadRequestWithoutForwarding()
    {
        var forwarding = Substitute.For<IBffPreferenceForwardingService>();
        var antiforgery = Substitute.For<IAntiforgery>();
        antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);
        await using var factory = new BlazorBffWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAntiforgery>();
                services.AddSingleton(antiforgery);
                services.RemoveAll<IBffPreferenceForwardingService>();
                services.AddSingleton(forwarding);
            });
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var authHeader = TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid());
        using var request = new HttpRequestMessage(HttpMethod.Put, "/bff/appearance/mode");
        request.Headers.Add(TestAuthHandler.AuthHeaderName, authHeader);
        request.Content = JsonContent.Create(new SetThemeModeRequestDto { ThemeMode = "sepia" });

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Theme mode must be one of");
        _ = forwarding.DidNotReceive().SetThemeModeAsync(
            Arg.Any<SetThemeModeRequestDto>(),
            Arg.Any<CancellationToken>());
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private static async Task<string> IssueAntiforgeryCookieAsync(HttpClient client, string? authHeader = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/status");
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            request.Headers.Add(TestAuthHandler.AuthHeaderName, authHeader);
        }

        using var response = await client.SendAsync(request);
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

    private static IReadOnlyCollection<string> GetSetCookieHeaders(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values!.ToArray()
            : Array.Empty<string>();
    }
}
