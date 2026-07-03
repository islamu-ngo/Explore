// ABOUTME: Regression tests for Blazor server secret-management service composition.
// ABOUTME: Ensures the BFF registers repository dependencies required by the shared secret resolver.

using Explore.Application.Contracts.Secrets;
using Explore.Blazor.Extensions;
using Explore.Secrets.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Explore.Blazor.IntegrationTests.Extensions;

public sealed class SecretManagementRegistrationTests
{
    [Test]
    public async Task AddServerOnlyServices_WhenSecretManagementIsRegistered_CanResolveSecretResolver()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=explore_test;Username=postgres;Password=postgres",
                ["SecretProvider:Provider"] = "None",
                ["Setup:Secret"] = "test-setup-secret"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSecretManagement(configuration, enableAuditing: false, enableRefreshService: false);
        services.AddServerOnlyServices(configuration);

        await using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ISecretResolver>().Should().NotBeNull();
    }
}
