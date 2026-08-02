// ABOUTME: Verifies provider migration ownership through configured EF Core relational options.
// ABOUTME: Locks application and Data Protection assemblies and history tables without parsing source files.

using Explore.Persistence.Database;
using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Event.Architecture.Tests;

public sealed class ProviderMigrationOwnershipTests
{
    [Test]
    public async Task ProviderCompositionAssignsDedicatedMigrationAssemblies()
    {
        var applicationAssemblies = new Dictionary<PrimaryDatabaseProvider, string>();
        var dataProtectionAssemblies = new Dictionary<PrimaryDatabaseProvider, string>();

        foreach (var provider in Enum.GetValues<PrimaryDatabaseProvider>())
        {
            RelationalOptionsExtension application = Configure(provider, PrimaryDatabaseMigrationTarget.Application);
            RelationalOptionsExtension dataProtection = Configure(provider, PrimaryDatabaseMigrationTarget.DataProtection);

            applicationAssemblies.Add(provider, application.MigrationsAssembly!);
            dataProtectionAssemblies.Add(provider, dataProtection.MigrationsAssembly!);
        }

        await Assert.That(applicationAssemblies.Values).IsEquivalentTo([
            "Explore.Persistence",
            "Explore.Persistence.Migrations.Sqlite",
            "Explore.Persistence.Migrations.SqlServer",
            "Explore.Persistence.Migrations.MariaDb",
            "Explore.Persistence.Migrations.MySql",
        ]);
        await Assert.That(dataProtectionAssemblies.Values).IsEquivalentTo([
            "Explore.Persistence",
            "Explore.Persistence.DataProtection.Migrations.Sqlite",
            "Explore.Persistence.DataProtection.Migrations.SqlServer",
            "Explore.Persistence.DataProtection.Migrations.MariaDb",
            "Explore.Persistence.DataProtection.Migrations.MySql",
        ]);

        PrimaryDatabaseProvider[] sharedOwners = applicationAssemblies.Keys
            .Where(provider => applicationAssemblies[provider] == dataProtectionAssemblies[provider])
            .ToArray();
        await Assert.That(sharedOwners).IsEquivalentTo([PrimaryDatabaseProvider.PostgreSql]);
    }

    [Test]
    public async Task ProviderCompositionSeparatesHistoryTablesAndAlignsNamespacePolicy()
    {
        var schemaProviders = new List<PrimaryDatabaseProvider>();
        var prefixedProviders = new List<PrimaryDatabaseProvider>();

        foreach (var provider in Enum.GetValues<PrimaryDatabaseProvider>())
        {
            RelationalOptionsExtension application = Configure(provider, PrimaryDatabaseMigrationTarget.Application);
            RelationalOptionsExtension dataProtection = Configure(provider, PrimaryDatabaseMigrationTarget.DataProtection);

            await Assert.That(application.MigrationsHistoryTableName)
                .IsNotEqualTo(dataProtection.MigrationsHistoryTableName);
            await Assert.That(application.MigrationsHistoryTableSchema)
                .IsEqualTo(dataProtection.MigrationsHistoryTableSchema);

            if (application.MigrationsHistoryTableSchema is { } schema)
            {
                await Assert.That(schema).IsEqualTo("islamu_event");
                await Assert.That(application.MigrationsHistoryTableName).IsEqualTo("__EFMigrationsHistory");
                await Assert.That(dataProtection.MigrationsHistoryTableName)
                    .IsEqualTo("__EFDataProtectionMigrationsHistory");
                schemaProviders.Add(provider);
            }
            else
            {
                await Assert.That(application.MigrationsHistoryTableName)
                    .StartsWith("islamu_event_", StringComparison.Ordinal);
                await Assert.That(dataProtection.MigrationsHistoryTableName)
                    .StartsWith("islamu_event_", StringComparison.Ordinal);
                prefixedProviders.Add(provider);
            }
        }

        await Assert.That(schemaProviders)
            .IsEquivalentTo([PrimaryDatabaseProvider.PostgreSql, PrimaryDatabaseProvider.SqlServer]);
        await Assert.That(prefixedProviders)
            .IsEquivalentTo([
                PrimaryDatabaseProvider.Sqlite,
                PrimaryDatabaseProvider.MariaDb,
                PrimaryDatabaseProvider.MySql,
            ]);
    }

    private static RelationalOptionsExtension Configure(
        PrimaryDatabaseProvider provider,
        PrimaryDatabaseMigrationTarget target)
    {
        var builder = new DbContextOptionsBuilder();
        var options = CreateOptions(provider);

        if (target == PrimaryDatabaseMigrationTarget.Application)
        {
            PrimaryDatabaseProviderComposition.ConfigureApplication(builder, options);
        }
        else
        {
            PrimaryDatabaseProviderComposition.ConfigureDataProtection(builder, options);
        }

        return builder.Options.Extensions.OfType<RelationalOptionsExtension>().Single();
    }

    private static PrimaryDatabaseConnectionOptions CreateOptions(PrimaryDatabaseProvider provider)
    {
        if (provider == PrimaryDatabaseProvider.Sqlite)
        {
            return new PrimaryDatabaseConnectionOptions
            {
                Role = PrimaryDatabaseRole.Migrator,
                Provider = provider,
                Database = "architecture-event.db",
            };
        }

        PrimaryDatabaseServerFlavor? flavor =
            Enum.TryParse(provider.ToString(), out PrimaryDatabaseServerFlavor parsedFlavor)
                ? parsedFlavor
                : null;
        return new PrimaryDatabaseConnectionOptions
        {
            Role = PrimaryDatabaseRole.Migrator,
            Provider = provider,
            Host = "database.example.test",
            Database = "event_db",
            Username = "migration_user",
            Password = "test-only-password",
            TlsMode = PrimaryDatabaseTlsMode.Required,
            ServerFlavor = flavor,
            ServerVersion = flavor is null ? null : new Version(11, 4),
        };
    }
}
