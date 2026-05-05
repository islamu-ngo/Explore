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
[NotInParallel("BlazorBootstrapEnvironment")]
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

    [Test]
    public async Task StaticPages_CarryContentSecurityPolicyHeader()
    {
        var response = await _client.GetAsync("/");

        response.Headers.TryGetValues("Content-Security-Policy", out var values).Should().BeTrue(
            "BFF HTML responses must carry the launch CSP header");

        var csp = values.Should().ContainSingle().Subject;
        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("script-src 'self' 'wasm-unsafe-eval'");
        csp.Should().Contain("frame-ancestors 'none'");
        csp.Should().Contain("base-uri 'self'");
        csp.Should().Contain("object-src 'none'");
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
        private static readonly IReadOnlyDictionary<string, string?> BootstrapEnvironmentOverrides =
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Infisical__ProjectId"] = string.Empty,
                ["Infisical__ClientId"] = string.Empty,
                ["Infisical__ClientSecret"] = string.Empty,
                ["Keycloak__Authority"] = string.Empty,
                ["Keycloak__MetadataAddress"] = string.Empty,
                ["Keycloak__Realm"] = string.Empty,
                ["Keycloak__ClientId"] = string.Empty,
                ["Keycloak__ClientSecret"] = string.Empty,
                ["POSTGRESQL_HOST"] = "localhost",
                ["POSTGRESQL_PORT"] = "5432",
                ["POSTGRESQL_DATABASE"] = "test_bff_no_keycloak",
                ["POSTGRESQL_USERNAME"] = "postgres",
                ["POSTGRESQL_PASSWORD"] = "postgres"
            };

        private readonly Dictionary<string, string?> _originalEnvironmentValues = new(StringComparer.Ordinal);

        public NoKeycloakBlazorBffWebApplicationFactory()
        {
            foreach (var (key, value) in BootstrapEnvironmentOverrides)
            {
                _originalEnvironmentValues[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                var testConfig = new Dictionary<string, string?>
                {
                    ["Infisical:ProjectId"] = string.Empty,
                    ["Infisical:ClientId"] = string.Empty,
                    ["Infisical:ClientSecret"] = string.Empty,
                    ["Keycloak:Authority"] = string.Empty,
                    ["Keycloak:MetadataAddress"] = string.Empty,
                    ["Keycloak:Realm"] = string.Empty,
                    ["Keycloak:ClientId"] = string.Empty,
                    ["Keycloak:ClientSecret"] = string.Empty,
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

                RemoveExploreDbContextRegistrations(services);
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

        private static void RemoveExploreDbContextRegistrations(IServiceCollection services)
        {
            var descriptors = services
                .Where(descriptor => IsExploreDbContextRegistration(descriptor.ServiceType) ||
                                     IsExploreDbContextRegistration(descriptor.ImplementationType))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }
        }

        private static bool IsExploreDbContextRegistration(Type? type)
        {
            if (type is null)
            {
                return false;
            }

            if (type == typeof(Explore.Persistence.ExploreDbContext) ||
                type == typeof(DbContextOptions) ||
                type == typeof(DbContextOptions<Explore.Persistence.ExploreDbContext>) ||
                type == typeof(IDbContextFactory<Explore.Persistence.ExploreDbContext>))
            {
                return true;
            }

            return type.IsGenericType &&
                   type.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true &&
                   type.GenericTypeArguments.Contains(typeof(Explore.Persistence.ExploreDbContext));
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var (key, value) in _originalEnvironmentValues)
            {
                Environment.SetEnvironmentVariable(key, value);
            }

            base.Dispose(disposing);
        }
    }
}
