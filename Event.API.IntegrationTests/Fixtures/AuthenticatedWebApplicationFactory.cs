// ABOUTME: WebApplicationFactory that wires TestAuthHandler as the default authentication scheme.
// Also replaces IAuthorizationProvider with an allow-all mock for endpoint-level auth tests.

using System.Threading.Channels;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Constants;
using Explore.Persistence;
using Explore.Persistence.Seed;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Event.Api.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory that replaces real authentication with TestAuthHandler,
/// and optionally mocks IAuthorizationProvider for authorization integration tests.
/// </summary>
public class AuthenticatedWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"InMemoryDbForAuthTesting_{Guid.NewGuid():N}";

    /// <summary>
    /// When non-null, replaces the real IAuthorizationProvider with this instance.
    /// Set to an allow-all mock for endpoint auth tests, or a selective mock for HATEOAS link tests.
    /// </summary>
    public IAuthorizationProvider? AuthorizationProviderOverride { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var inMemoryConfig = new Dictionary<string, string?>
            {
                {"ConnectionStrings:DefaultConnection", "Host=localhost;Database=explore_db_test;Username=postgres;Password=postgres"},
                {"Keycloak:Authority", "https://auth.example.com"},
                {"Keycloak:Realm", "ISLAMU"},
                {"Keycloak:Audience", "islamu-event-api"},
                {"Keycloak:RequireHttpsMetadata", "false"},
                {"Keycloak:MetadataAddress", "https://auth.example.com/.well-known/openid-configuration"},
                {"Testing:SkipJwtAuthorityWarmup", "true"},
                {"S3Settings:Region", "us-east-1"},
                {"S3Settings:BucketName", "test-bucket"},
                {"S3Settings:AccessKeyId", "test-key"},
                {"S3Settings:SecretAccessKey", "test-secret"},
                {"S3Settings:Endpoint", "https://s3.example.com"},
                {"Deployment:Mode", "SingleTenant"},
                {"Deployment:DefaultTenantId", PlatformDefaults.DefaultTenantId.ToString()},
                {"PublicBaseUrl", "https://integration.test"}
            };
            config.AddInMemoryCollection(inMemoryConfig);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveExploreDbContextRegistrations();

            services.AddInMemoryExploreDbContext(_databaseName);

            // Override Redis with in-memory distributed cache for tests
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            // Register background seeder to ensure lookup data (roles, etc.) is available in tests
            services.AddHostedService<SeedingHostedService>();
        });

        // ConfigureTestServices runs AFTER the app's ConfigureServices,
        // ensuring our auth scheme overrides the real Keycloak JWT registration.
        builder.ConfigureTestServices(services =>
        {
            // Override ALL default schemes to use TestAuthHandler.
            // Must set DefaultScheme (not just Authenticate/Challenge) because
            // Program.cs sets DefaultScheme = "Bearer" via AddAuthentication("Bearer"),
            // and specific defaults fall back to DefaultScheme if not explicitly set.
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            // Ensure TestAuthHandler is the final default scheme even if
            // Program.cs registers post-configuration for AuthenticationOptions.
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthHandler.SchemeName;
            });

            // Replace IAuthorizationProvider if override provided
            if (AuthorizationProviderOverride is not null)
            {
                services.RemoveAll<IAuthorizationProvider>();
                services.AddScoped(_ => AuthorizationProviderOverride);
            }
        });
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        catch (ChannelClosedException)
        {
            // OpenFeature can race its background event executor shutdown in test hosts.
        }
        catch (ObjectDisposedException)
        {
            // Some hosted services can observe disposal while WebApplicationFactory is tearing down.
        }
        catch (NullReferenceException)
        {
            // Test host disposal can race service-provider cleanup after failed startup paths.
        }
    }

    private sealed class SeedingHostedService(IServiceProvider serviceProvider, IHostEnvironment environment) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
            await DatabaseSeeder.SeedAsync(db, environment, cancellationToken);

            // Refresh the lookup cache to ensure it picks up the seeded data (roles, etc.)
            var cache = scope.ServiceProvider.GetService<ILookupDataCache>();
            if (cache != null)
            {
                await cache.RefreshAsync(cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
