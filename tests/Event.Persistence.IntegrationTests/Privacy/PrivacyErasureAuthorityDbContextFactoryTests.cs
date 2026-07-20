// ABOUTME: Verifies authority EF tooling ignores ambient durable database targets.
// ABOUTME: Locks the design-time factory to its fixed inert scaffolding connection.

using Explore.Persistence.Privacy.ErasureAuthority;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Event.Persistence.IntegrationTests.Privacy;

[NotInParallel]
public sealed class PrivacyErasureAuthorityDbContextFactoryTests
{
    [Test]
    public async Task CreateDbContext_IgnoresAmbientAuthorityConnectionString()
    {
        const string key = "ConnectionStrings__PrivacyErasureAuthority";
        string? previousValue = Environment.GetEnvironmentVariable(key);

        try
        {
            Environment.SetEnvironmentVariable(
                key,
                "Host=127.0.0.1;Port=2;Database=hostile_ambient;Username=canary;Password=canary;Timeout=1");

            await using PrivacyErasureAuthorityDbContext context =
                new PrivacyErasureAuthorityDbContextFactory().CreateDbContext([]);
            var target = new NpgsqlConnectionStringBuilder(context.Database.GetConnectionString());

            await Assert.That(target.Host).IsEqualTo("127.0.0.1");
            await Assert.That(target.Port).IsEqualTo(1);
            await Assert.That(target.Database).IsEqualTo("privacy_erasure_authority_design_time");
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, previousValue);
        }
    }
}
