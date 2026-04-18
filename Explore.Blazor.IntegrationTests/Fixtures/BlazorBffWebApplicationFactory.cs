// ABOUTME: WebApplicationFactory for Explore.Blazor BFF integration tests with deterministic test-time overrides.
// ABOUTME: Replaces auth, resolver config, database, and cache dependencies so middleware/endpoints can run in isolation.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Blazor.Services;
using Explore.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Explore.Blazor.IntegrationTests.Fixtures;

public class BlazorBffWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly ResolverConfigurationDto _resolverConfiguration;

    public BlazorBffWebApplicationFactory()
        : this(new ResolverConfigurationDto
        {
            PathEnabled = true,
            PathPrefix = "/t"
        })
    {
    }

    private BlazorBffWebApplicationFactory(ResolverConfigurationDto resolverConfiguration)
    {
        _resolverConfiguration = resolverConfiguration;
    }

    public BlazorBffWebApplicationFactory WithResolverConfiguration(ResolverConfigurationDto resolverConfiguration)
    {
        return new BlazorBffWebApplicationFactory(resolverConfiguration);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=test_bff;Username=postgres;Password=postgres");
        builder.UseSetting("Keycloak:Authority", "https://auth.example.com");
        builder.UseSetting("Keycloak:Realm", "explore");
        builder.UseSetting("Deployment:Mode", "SingleTenant");
        builder.UseSetting("Deployment:DefaultTenantId", "018e4e5c-7f00-7000-8000-000000000001");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var inMemoryConfig = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test_bff;Username=postgres;Password=postgres",
                ["Keycloak:Authority"] = "https://auth.example.com",
                ["Keycloak:Realm"] = "explore",
                ["Deployment:Mode"] = "SingleTenant",
                ["Deployment:DefaultTenantId"] = "018e4e5c-7f00-7000-8000-000000000001"
            };

            config.AddInMemoryCollection(inMemoryConfig);
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.RemoveAll<IClaimsTransformation>();
            services.RemoveAll<BffAdminClaimsTransformation>();
            services.AddSingleton<IClaimsTransformation, PassthroughClaimsTransformation>();

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
            });

            services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.ForwardAuthenticate = TestAuthHandler.SchemeName;
                options.ForwardChallenge = TestAuthHandler.SchemeName;
            });

            services.RemoveAll<IDynamicAuthSchemeManager>();
            var mockSchemeManager = Substitute.For<IDynamicAuthSchemeManager>();
            mockSchemeManager.GetRegisteredProviderSchemesAsync().Returns(new List<string>());
            services.AddSingleton(mockSchemeManager);

            services.RemoveAll(typeof(IDbContextFactory<ExploreDbContext>));
            services.RemoveAll(typeof(ExploreDbContext));
            services.AddDbContext<ExploreDbContext>(options => options.UseInMemoryDatabase("BffTestDb"));

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            services.RemoveAll<IResolverConfigService>();
            var mockResolverConfig = Substitute.For<IResolverConfigService>();
            mockResolverConfig.GetConfigurationAsync(Arg.Any<CancellationToken>())
                .Returns(_resolverConfiguration);
            services.AddSingleton(mockResolverConfig);

            var testAssembly = typeof(TenantTestController).Assembly;
            services.AddControllers().AddApplicationPart(testAssembly);
        });
    }

    private sealed class PassthroughClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            return Task.FromResult(principal);
        }
    }
}
