// ABOUTME: Security-focused BFF WebApplicationFactory that uses real OIDC against containerized Keycloak.
// ABOUTME: Does NOT use TestAuthHandler — exercises the actual Cookie + OIDC authentication pipeline.

using Explore.Application.Contracts.Services;
using Explore.Blazor.Services;
using Explore.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Explore.Blazor.IntegrationTests.Fixtures;

/// <summary>
/// BFF WebApplicationFactory configured for security integration tests.
/// Uses real OIDC authentication against a containerized Keycloak instance.
/// The DynamicAuthSchemeManager will register Keycloak from environment variables
/// pointing to the container, enabling the full OIDC challenge/callback flow.
/// </summary>
public class SecurityBlazorBffWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _keycloakAuthority;
    private readonly string _keycloakMetadataAddress;
    private readonly string _keycloakClientId;
    private readonly string _keycloakClientSecret;

    public SecurityBlazorBffWebApplicationFactory(
        string keycloakAuthority,
        string keycloakMetadataAddress,
        string keycloakClientId = "islamu-event-blazor",
        string keycloakClientSecret = "test-blazor-secret")
    {
        _keycloakAuthority = keycloakAuthority;
        _keycloakMetadataAddress = keycloakMetadataAddress;
        _keycloakClientId = keycloakClientId;
        _keycloakClientSecret = keycloakClientSecret;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test_bff_security;Username=postgres;Password=postgres",
                ["Keycloak:Authority"] = _keycloakAuthority,
                ["Keycloak:Realm"] = "ISLAMU",
                ["Keycloak:ClientId"] = _keycloakClientId,
                ["Keycloak:ClientSecret"] = _keycloakClientSecret,
                ["Keycloak:MetadataAddress"] = _keycloakMetadataAddress,
                ["Keycloak:RequireHttpsMetadata"] = "false",
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

            services.RemoveAll(typeof(IDbContextFactory<ExploreDbContext>));
            services.RemoveAll<ExploreDbContext>();
            services.AddDbContext<ExploreDbContext>(options =>
                options.UseInMemoryDatabase($"SecurityBffTestDb_{Guid.NewGuid():N}"));

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            services.RemoveAll<IResolverConfigService>();
            var mockResolverConfig = NSubstitute.Substitute.For<IResolverConfigService>();
            mockResolverConfig.GetConfigurationAsync(Arg.Any<CancellationToken>())
                .Returns(new Explore.Application.DTOs.Onboarding.ResolverConfigurationDto
                {
                    PathEnabled = false
                });
            services.AddSingleton(mockResolverConfig);

            services.RemoveAll<IBffOnboardingStatusProvider>();
            var mockOnboarding = NSubstitute.Substitute.For<IBffOnboardingStatusProvider>();
            mockOnboarding.GetStatusAsync(Arg.Any<CancellationToken>())
                .Returns(new BffOnboardingStatus(
                    IsCompleted: true,
                    IsSetupModeActive: false,
                    Known: true));
            services.AddSingleton(mockOnboarding);
        });
    }
}
