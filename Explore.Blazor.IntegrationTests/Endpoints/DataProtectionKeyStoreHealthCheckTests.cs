// ABOUTME: Covers the Blazor BFF readiness check for persisted Data Protection keys.
// ABOUTME: Proves key-store failures become safe unhealthy health results.

using Explore.Blazor.HealthChecks;
using Explore.Persistence;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class DataProtectionKeyStoreHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WhenKeyStoreIsReachable_ReturnsHealthyWithSafeData()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddDbContext<DataProtectionKeyContext>(options =>
            options.UseInMemoryDatabase("data-protection-health-ok", databaseRoot));

        await using var provider = services.BuildServiceProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataProtectionKeyContext>();
            await db.Database.EnsureCreatedAsync();
            db.DataProtectionKeys.Add(new DataProtectionKey
            {
                FriendlyName = "test-key",
                Xml = "<key />"
            });
            await db.SaveChangesAsync();
        }

        var healthCheck = new DataProtectionKeyStoreHealthCheck(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DataProtectionKeyStoreHealthCheck>.Instance);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Healthy);
        await Assert.That(result.Data["keyCount"]).IsEqualTo(1);
        await Assert.That(result.Data.Keys).DoesNotContain("xml");
        await Assert.That(result.Data.Keys).DoesNotContain("connectionString");
    }

    [Test]
    public async Task CheckHealthAsync_WhenKeyStoreIsMissing_ReturnsUnhealthyWithSafeFailureType()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var healthCheck = new DataProtectionKeyStoreHealthCheck(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DataProtectionKeyStoreHealthCheck>.Instance);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        await Assert.That(result.Status).IsEqualTo(HealthStatus.Unhealthy);
        await Assert.That(result.Description).IsEqualTo("Data Protection key store is unreachable.");
        await Assert.That(result.Data["failureType"]).IsEqualTo(nameof(InvalidOperationException));
        await Assert.That(result.Data.Keys).DoesNotContain("connectionString");
    }
}
