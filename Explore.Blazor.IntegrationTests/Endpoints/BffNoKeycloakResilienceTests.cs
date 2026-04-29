// ABOUTME: Tests that the Blazor BFF gracefully handles the complete absence of Keycloak configuration.
// ABOUTME: No crash, no fake login, provider list empty, auth status not authenticated.

using Explore.Blazor.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TUnit.Core;

namespace Explore.Blazor.IntegrationTests.Endpoints;

/// <summary>
/// Verifies the Blazor BFF's behavior when no Keycloak configuration is present.
/// The DynamicAuthSchemeManager should find no env vars, register no OIDC schemes,
/// and the BFF should start and serve pages normally with no authentication provider.
/// </summary>
[Category(BffTestCategories.Security)]
public class BffNoKeycloakResilienceTests : IAsyncDisposable
{
    private readonly NoKeycloakBlazorBffWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BffNoKeycloakResilienceTests()
    {
        _factory = new NoKeycloakBlazorBffWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    #region BFF Starts Without Keycloak

    [Test]
    public async Task Bff_StartsWithoutKeycloak()
    {
        _factory.Should().NotBeNull("the BFF factory must build successfully without Keycloak config");
        _client.Should().NotBeNull("the HTTP client must be creatable without OIDC errors");
    }

    #endregion

    #region Auth Endpoints — Graceful Degradation

    [Test]
    public async Task AuthStatus_ReturnsNotAuthenticated()
    {
        var response = await _client.GetAsync("/auth/status");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<AuthStatusPayload>();
        payload.Should().NotBeNull();
        payload!.IsAuthenticated.Should().BeFalse(
            "without any auth provider, the user is always not authenticated");
    }

    [Test]
    public async Task Challenge_RedirectsToLoginPage_NoProvider()
    {
        var response = await _client.GetAsync("/auth/challenge?returnUrl=/dashboard");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect,
            "challenge should still redirect even without providers");

        var location = response.Headers.Location?.ToString();
        location.Should().NotBeNullOrEmpty();

        location.Should().Contain("/login",
            "without any registered provider, challenge should redirect to the login page " +
            "for provider selection (which will show an empty list)");
    }

    [Test]
    public async Task Providers_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/auth/providers");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("providers",
            "the providers endpoint should return a valid response even when empty");

        var payload = await response.Content.ReadFromJsonAsync<ProvidersPayload>();
        payload.Should().NotBeNull();
        payload!.Providers.Should().BeNullOrEmpty(
            "without Keycloak configuration, no providers should be registered");
    }

    [Test]
    public async Task Signout_StillWorks()
    {
        var response = await _client.GetAsync("/auth/signout?returnUrl=/");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect,
            "signout should work even for anonymous users without any provider configured");
    }

    #endregion

    #region Static Pages Accessible

    [Test]
    public async Task StaticPages_AreAccessible()
    {
        var response = await _client.GetAsync("/");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
            "static pages should be accessible regardless of auth configuration");
    }

    #endregion

    private sealed class AuthStatusPayload
    {
        public bool IsAuthenticated { get; set; }
        public string? Name { get; set; }
    }

    private sealed class ProvidersPayload
    {
        public List<object>? Providers { get; set; }
    }

    /// <summary>
    /// BFF WebApplicationFactory with NO Keycloak configuration.
    /// The DynamicAuthSchemeManager will find no env vars and register no OIDC schemes.
    /// </summary>
    private sealed class NoKeycloakBlazorBffWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                var testConfig = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test_bff_no_keycloak;Username=postgres;Password=postgres",
                    ["Deployment:Mode"] = "SingleTenant",
                    ["Deployment:DefaultTenantId"] = "018e4e5c-7f00-7000-8000-000000000001",
                    ["ExploreApi:BaseUrl"] = "http://localhost:9999/",
                    ["S3Settings:Region"] = "us-east-1",
                    ["S3Settings:BucketName"] = "test-bucket",
                    ["S3Settings:AccessKeyId"] = "test-key",
                    ["S3Settings:SecretAccessKey"] = "test-secret",
                    ["S3Settings:Endpoint"] = "https://s3.example.com",
                };

                config.AddInMemoryCollection(testConfig);
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();

                services.RemoveAll(typeof(IDbContextFactory<Explore.Persistence.ExploreDbContext>));
                services.RemoveAll<Explore.Persistence.ExploreDbContext>();
                services.AddDbContext<Explore.Persistence.ExploreDbContext>(options =>
                    options.UseInMemoryDatabase($"BffNoKeycloakDb_{Guid.NewGuid():N}"));

                services.RemoveAll<IDistributedCache>();
                services.AddDistributedMemoryCache();

                var mockOnboarding = NSubstitute.Substitute.For<Explore.Blazor.Services.IBffOnboardingStatusProvider>();
                mockOnboarding.GetStatusAsync(Arg.Any<CancellationToken>())
                    .Returns(new Explore.Blazor.Services.BffOnboardingStatus(
                        IsCompleted: true, IsSetupModeActive: false, Known: true));
                services.RemoveAll<Explore.Blazor.Services.IBffOnboardingStatusProvider>();
                services.AddSingleton(mockOnboarding);
            });
        }
    }
}
