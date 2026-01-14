using Explore.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
                {"Keycloak:Realm", "explore"},
                {"Keycloak:Audience", "explore-api"},
                {"Keycloak:RequireHttpsMetadata", "false"},
                {"Keycloak:MetadataAddress", "https://auth.example.com/.well-known/openid-configuration"},
                {"ISLAMU_EVENT_REGION", "us-east-1"},
                {"ISLAMU_EVENT_PRIVATE_BUCKET_NAME", "test-bucket"},
                {"ISLAMU_EVENT_PRIVATE_ACCESS_KEY_ID", "test-key"},
                {"ISLAMU_EVENT_PRIVATE_SECRET_ACCESS_KEY_ID", "test-secret"},
                {"ISLAMU_EVENT_S3_ENDPOINT", "https://s3.example.com"}
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
        });
    }
}
