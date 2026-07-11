// ABOUTME: Regression tests for persisted ASP.NET Core Data Protection key rings.
// ABOUTME: Verifies BFF session payloads survive a fresh provider using the shared key store.

using Explore.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Event.Persistence.IntegrationTests.DataProtection;

public sealed class DataProtectionKeyPersistenceTests
{
    private const string ApplicationName = "islamu-event";
    private const string Purpose = "bff-session-regression";

    [Test]
    public async Task PersistedKeyRingAllowsFreshProviderToUnprotectExistingPayload()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        const string databaseName = "data-protection-key-ring-regression";
        const string payload = "authenticated-session-ticket";

        string protectedPayload;

        await using (var firstProvider = BuildServiceProvider(databaseName, databaseRoot))
        {
            var protector = firstProvider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(Purpose);

            protectedPayload = protector.Protect(payload);

            await using var scope = firstProvider.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DataProtectionKeyContext>();
            await Assert.That(await dbContext.DataProtectionKeys.CountAsync()).IsGreaterThanOrEqualTo(1);
        }

        await using (var secondProvider = BuildServiceProvider(databaseName, databaseRoot))
        {
            var protector = secondProvider
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(Purpose);

            var unprotectedPayload = protector.Unprotect(protectedPayload);

            await Assert.That(unprotectedPayload).IsEqualTo(payload);
        }
    }

    private static ServiceProvider BuildServiceProvider(string databaseName, InMemoryDatabaseRoot databaseRoot)
    {
        var services = new ServiceCollection();

        services.AddDbContext<DataProtectionKeyContext>(options =>
            options.UseInMemoryDatabase(databaseName, databaseRoot));

        services
            .AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToDbContext<DataProtectionKeyContext>();

        var serviceProvider = services.BuildServiceProvider(validateScopes: true);

        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<DataProtectionKeyContext>().Database.EnsureCreated();

        return serviceProvider;
    }
}
