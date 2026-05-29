// ABOUTME: Integration tests for browser-facing BFF auth endpoint sanitization.
// ABOUTME: Verifies auth provider failures and browser-supplied auth headers stay safe.

using Explore.Blazor.IntegrationTests.Fixtures;
using Explore.Blazor.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class BffAuthEndpointValidationTests
{
    [Test]
    public async Task AuthProviders_WhenSchemeManagerThrows_ReturnsSafeProblemWithoutRawProviderError()
    {
        using var factory = new BlazorBffWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDynamicAuthSchemeManager>();
                services.AddSingleton<IDynamicAuthSchemeManager>(new ThrowingAuthSchemeManager());
            });
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        using var response = await client.GetAsync("/auth/providers");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Authentication providers could not be resolved.");
        body.Should().Contain("auth_provider_resolution_failed");
        body.Should().Contain("correlationId");
        body.Should().NotContain("raw provider failure");
        body.Should().NotContain("refresh_token");
        body.Should().NotContain("secretLen");
        body.Should().NotContain("islamu-event-blazor");
    }

    [Test]
    public async Task AuthStatus_WithBrowserAuthorizationHeader_DoesNotAuthenticateOrEchoBearerToken()
    {
        using var factory = new BlazorBffWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        const string browserToken = "browser-supplied-access-token";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/status");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", browserToken);

        using var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"isAuthenticated\":false");
        body.Should().NotContain(browserToken);
        body.Should().NotContain("Bearer");
    }

    [Test]
    public async Task RefreshSchemes_WithoutAntiforgeryHeader_ReturnsBadRequest()
    {
        using var factory = new BlazorBffWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        using var response = await client.PostAsync("/bff/auth/refresh-schemes", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Antiforgery validation failed");
    }

    [Test]
    public async Task RefreshSession_WithoutAntiforgeryHeader_ReturnsBadRequest()
    {
        using var factory = new BlazorBffWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Add(
            TestAuthHandler.AuthHeaderName,
            TestAuthHandler.CreateAuthHeaderValue(Guid.NewGuid(), "Refresh Tester"));

        using var response = await client.PostAsync("/bff/auth/refresh-session", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Antiforgery validation failed");
    }

    private sealed class ThrowingAuthSchemeManager : IDynamicAuthSchemeManager
    {
        public Task InitializeAsync() => Task.CompletedTask;

        public Task RefreshSchemesAsync(string? setupSecret = null) => Task.CompletedTask;

        public Task<IReadOnlyList<string>> GetRegisteredProviderSchemesAsync() =>
            throw new InvalidOperationException(
                "raw provider failure refresh_token=secret-token secretLen=24 clientId=islamu-event-blazor");
    }
}
