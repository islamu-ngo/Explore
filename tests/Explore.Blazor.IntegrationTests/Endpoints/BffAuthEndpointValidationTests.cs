// ABOUTME: Integration tests for browser-facing BFF auth endpoint sanitization.
// ABOUTME: Verifies auth provider failures and browser-supplied auth headers stay safe.

using Explore.Blazor.IntegrationTests.Fixtures;
using Explore.Blazor.Services;
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("Authentication providers could not be resolved.");
        await Assert.That(body).Contains("auth_provider_resolution_failed");
        await Assert.That(body).Contains("correlationId");
        await Assert.That(body).DoesNotContain("raw provider failure");
        await Assert.That(body).DoesNotContain("refresh_token");
        await Assert.That(body).DoesNotContain("secretLen");
        await Assert.That(body).DoesNotContain("islamu-event-blazor");
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("\"isAuthenticated\":false");
        await Assert.That(body).DoesNotContain(browserToken);
        await Assert.That(body).DoesNotContain("Bearer");
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("Antiforgery validation failed");
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("Antiforgery validation failed");
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
