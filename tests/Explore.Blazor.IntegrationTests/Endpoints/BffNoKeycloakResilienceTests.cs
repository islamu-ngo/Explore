// ABOUTME: Tests that the Blazor BFF gracefully handles the complete absence of Keycloak configuration.
// ABOUTME: No crash, no fake login, provider list empty, auth status not authenticated.

using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.IntegrationTests.Fixtures;
using Explore.Blazor.Services;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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
        csp.Should().Contain("img-src 'self' data: https: blob:");
        csp.Should().Contain("connect-src 'self' https: http: ws: wss:");
        csp.Should().Contain("font-src 'self' https://fonts.gstatic.com");
        csp.Should().Contain("frame-ancestors 'none'");
        csp.Should().Contain("base-uri 'self'");
        csp.Should().Contain("object-src 'none'");
        csp.Should().Contain("form-action 'self'");

        var body = await response.Content.ReadAsStringAsync();
        var nonceStart = csp.IndexOf("'nonce-", StringComparison.Ordinal);
        nonceStart.Should().BeGreaterThanOrEqualTo(0);
        nonceStart += "'nonce-".Length;
        var nonceEnd = csp.IndexOf('\'', nonceStart);
        nonceEnd.Should().BeGreaterThan(nonceStart);
        var nonce = csp[nonceStart..nonceEnd];

        body.Should().Contain($"<script type=\"importmap\" nonce=\"{nonce}\">");
        body.Should().NotContain("http-equiv=\"Content-Security-Policy\"");
    }

    [Test]
    public async Task LaunchRoutes_CarryBrowserSecurityHeaders()
    {
        string[] paths = ["/", "/errors/404", "/css/layers.css"];

        foreach (var path in paths)
        {
            var response = await _client.GetAsync(path);

            response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.InternalServerError);
            AssertBrowserSecurityHeaders(response, path);
        }
    }

    [Test]
    public async Task AppShell_LinksWhiteLabelManifest()
    {
        var response = await _client.GetAsync("/");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("rel=\"manifest\" href=\"manifest.webmanifest\"");
        body.Should().Contain("name=\"theme-color\" content=\"#2563eb\"");
        body.Should().NotContain("Icon_landingpage.png");
    }

    [Test]
    public async Task ManifestWebManifest_ReturnsDbBackedWhiteLabelInstallMetadata()
    {
        var apiClient = Substitute.For<IEventApiClient>();
        apiClient.GetPublicExperienceShellAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PublicExperienceShellDto
            {
                Home = new PublicExperienceHomeDto
                {
                    BrandDisplayName = "Community Events",
                    BrandLogoUrl = "https://cdn.example.test/logo.png",
                    BrandFaviconUrl = "https://cdn.example.test/favicon.svg"
                }
            }));
        await using var factory = new NoKeycloakBlazorBffWebApplicationFactory(configureServices: services =>
        {
            services.RemoveAll<IEventApiClient>();
            services.AddSingleton(apiClient);
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var response = await client.GetAsync("/manifest.webmanifest");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("ISLAMU");
        body.Should().NotContain("Icon_landingpage.png");
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        root.GetProperty("name").GetString().Should().Be("Community Events");
        root.GetProperty("short_name").GetString().Should().Be("Community");
        root.GetProperty("description").GetString().Should().Be("Discover and register for events.");
        root.GetProperty("start_url").GetString().Should().Be("/");
        root.GetProperty("scope").GetString().Should().Be("/");
        root.GetProperty("display").GetString().Should().Be("standalone");
        root.GetProperty("theme_color").GetString().Should().Be("#2563eb");
        root.GetProperty("background_color").GetString().Should().Be("#ffffff");

        var icons = root.GetProperty("icons").EnumerateArray().ToArray();
        icons.Should().Contain(icon =>
            icon.GetProperty("src").GetString() == "https://cdn.example.test/favicon.svg" &&
            icon.GetProperty("sizes").GetString() == "any" &&
            icon.GetProperty("type").GetString() == "image/svg+xml");
        icons.Should().Contain(icon =>
            icon.GetProperty("src").GetString() == "https://cdn.example.test/logo.png" &&
            icon.GetProperty("sizes").GetString() == "any" &&
            icon.GetProperty("type").GetString() == "image/png");
    }

    [Test]
    public async Task RobotsTxt_WhenNonProduction_DisallowsCrawlers()
    {
        var response = await _client.GetAsync("/robots.txt");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("User-agent: *");
        body.Should().Contain("Disallow: /");
        body.Should().NotContain("Sitemap:");
    }

    [Test]
    public async Task RobotsTxt_WhenProduction_UsesForwardedCanonicalSitemapUrl()
    {
        await using var factory = new NoKeycloakBlazorBffWebApplicationFactory("Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/robots.txt");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "https");
        request.Headers.TryAddWithoutValidation("X-Forwarded-Host", "events.example.test");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("User-agent: *");
        body.Should().Contain("Allow: /");
        body.Should().Contain("Sitemap: https://events.example.test/sitemap.xml");
    }

    #endregion

    private static void AssertBrowserSecurityHeaders(HttpResponseMessage response, string path)
    {
        response.Headers.Contains("Content-Security-Policy").Should().BeTrue(path);
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle().Which.Should().Be("DENY", path);
        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff", path);
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle().Which.Should().Be("strict-origin-when-cross-origin", path);
        response.Headers.GetValues("Permissions-Policy").Should().ContainSingle().Which.Should().Be(
            "camera=(), microphone=(), geolocation=(self), payment=()",
            path);
    }

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
                ["POSTGRESQL_PASSWORD"] = "postgres",
                ["ConnectionStrings__cache"] = "localhost:6379,abortConnect=false,connectTimeout=100"
            };

        private readonly Dictionary<string, string?> _originalEnvironmentValues = new(StringComparer.Ordinal);

        private readonly string _environmentName;
        private readonly Action<IServiceCollection>? _configureServices;

        public NoKeycloakBlazorBffWebApplicationFactory(
            string environmentName = "Development",
            Action<IServiceCollection>? configureServices = null)
        {
            _environmentName = environmentName;
            _configureServices = configureServices;

            foreach (var (key, value) in BootstrapEnvironmentOverrides)
            {
                _originalEnvironmentValues[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environmentName);

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
                    ["ConnectionStrings:cache"] = "localhost:6379,abortConnect=false,connectTimeout=100",
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
                services.RemoveAll<Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.DataProtection.KeyManagement.KeyManagementOptions>>();
                services.AddDataProtection().UseEphemeralDataProtectionProvider();

                services.RemoveAll<IDistributedCache>();
                services.AddDistributedMemoryCache();

                services.RemoveAll<IBffResolverConfigurationProvider>();
                var mockResolverConfiguration = Substitute.For<IBffResolverConfigurationProvider>();
                mockResolverConfiguration.GetConfigurationAsync(Arg.Any<CancellationToken>())
                    .Returns(new ResolverConfigurationDto { PathEnabled = false });
                services.AddSingleton(mockResolverConfiguration);

                var mockOnboarding = NSubstitute.Substitute.For<Explore.Blazor.Services.IBffOnboardingStatusProvider>();
                mockOnboarding.GetStatusAsync(Arg.Any<CancellationToken>())
                    .Returns(new Explore.Blazor.Services.BffOnboardingStatus(
                        IsCompleted: true, IsSetupModeActive: false, Known: true));
                services.RemoveAll<Explore.Blazor.Services.IBffOnboardingStatusProvider>();
                services.AddSingleton(mockOnboarding);

                _configureServices?.Invoke(services);
            });
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
