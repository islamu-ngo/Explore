using System.Threading.Channels;
using Explore.Domain.Constants;
using Explore.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Event.Api.IntegrationTests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to "Testing"
        builder.UseEnvironment("Testing");

        // Force configuration to be available immediately
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
                {"S3Settings:Region", "us-east-1"},
                {"S3Settings:BucketName", "test-bucket"},
                {"S3Settings:AccessKeyId", "test-key"},
                {"S3Settings:SecretAccessKey", "test-secret"},
                {"S3Settings:Endpoint", "https://s3.example.com"},
                {"Deployment:Mode", "SingleTenant"},
                {"Deployment:DefaultTenantId", PlatformDefaults.DefaultTenantId.ToString()}
            };
            config.AddInMemoryCollection(inMemoryConfig);
        });

        builder.ConfigureServices(services =>
        {
            // Since skipDbContextRegistration=true in "Testing" environment,
            // no Npgsql provider is registered. We simply add InMemory.
            services.AddDbContext<ExploreDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
                options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            // Override Redis with in-memory distributed cache for tests
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
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
            // The test host can surface duplicate OpenFeature shutdown on disposal under WebApplicationFactory.
        }
        catch (NullReferenceException)
        {
            // Preserve the existing legacy teardown tolerance used by other integration-test fixtures.
        }
    }
}
