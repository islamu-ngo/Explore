// ABOUTME: Authenticated test factory pinned to single-tenant mode for tenant-scoped endpoint integration tests.
// ABOUTME: Avoids tenant-resolution 404s so tests can focus on authenticated controller behavior.

using Explore.Domain.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Event.Api.IntegrationTests.Fixtures;

public class SingleTenantAuthenticatedWebApplicationFactory : AuthenticatedWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var inMemoryConfig = new Dictionary<string, string?>
            {
                {"Deployment:Mode", "SingleTenant"},
                {"Deployment:DefaultTenantId", PlatformDefaults.DefaultTenantId.ToString()}
            };

            config.AddInMemoryCollection(inMemoryConfig);
        });

        base.ConfigureWebHost(builder);
    }
}
