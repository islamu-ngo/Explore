// ABOUTME: Verifies every supported primary provider uses the shared EF Core composition switch.
// ABOUTME: Covers provider identity, migration ownership, server flavor, Data Protection, and design-time projection.

using Explore.Application.Contracts.Persistence;
using Explore.Persistence;
using Explore.Persistence.Database;
using Explore.Persistence.Extensions;
using Explore.Persistence.Security;
using Explore.Secrets.Database;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Database;

public sealed class PrimaryDatabaseProviderCompositionTests
{
    [Test]
    [Arguments(PrimaryDatabaseProvider.PostgreSql, "NpgsqlOptionsExtension")]
    [Arguments(PrimaryDatabaseProvider.Sqlite, "SqliteOptionsExtension")]
    [Arguments(PrimaryDatabaseProvider.SqlServer, "SqlServerOptionsExtension")]
    [Arguments(PrimaryDatabaseProvider.MariaDb, "MySqlOptionsExtension")]
    [Arguments(PrimaryDatabaseProvider.MySql, "MySqlOptionsExtension")]
    public void ConfigureApplication_SelectsRequestedProvider(
        PrimaryDatabaseProvider provider,
        string expectedOptionsExtension)
    {
        var builder = new DbContextOptionsBuilder<ExploreDbContext>();

        PrimaryDatabaseProviderComposition.ConfigureApplication(builder, CreateOptions(provider));

        builder.Options.Extensions.Select(extension => extension.GetType().Name)
            .Should().Contain(expectedOptionsExtension);
    }

    [Test]
    [Arguments(PrimaryDatabaseProvider.PostgreSql, "NpgsqlOptionsExtension")]
    [Arguments(PrimaryDatabaseProvider.Sqlite, "SqliteOptionsExtension")]
    [Arguments(PrimaryDatabaseProvider.SqlServer, "SqlServerOptionsExtension")]
    [Arguments(PrimaryDatabaseProvider.MariaDb, "MySqlOptionsExtension")]
    [Arguments(PrimaryDatabaseProvider.MySql, "MySqlOptionsExtension")]
    public void ConfigurePersistenceServices_SelectsRequestedProvider(
        PrimaryDatabaseProvider provider,
        string expectedOptionsExtension)
    {
        var services = new ServiceCollection();
        services.ConfigurePersistenceServices(
            BuildConfiguration(provider),
            skipLookupCacheInitializer: true,
            environmentName: "Production");
        using var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<DbContextOptions<ExploreDbContext>>();

        options.Extensions.Select(extension => extension.GetType().Name)
            .Should().Contain(expectedOptionsExtension);
    }

    [Test]
    public void ConfigurePersistenceServices_DoesNotEnablePostgresRlsForOtherProviders()
    {
        var values = BuildConfigurationValues(PrimaryDatabaseProvider.Sqlite);
        values["Persistence:EnableRlsTenantSession"] = "true";
        var services = new ServiceCollection();
        services.ConfigurePersistenceServices(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build(),
            skipLookupCacheInitializer: true,
            environmentName: "Production");
        using var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<DbContextOptions<ExploreDbContext>>();
        var interceptors = options.FindExtension<CoreOptionsExtension>()?.Interceptors ?? [];

        interceptors.Should().NotContain(interceptor => interceptor is PostgresTenantSessionInterceptor);
    }

    [Test]
    [Arguments(PrimaryDatabaseProvider.PostgreSql, false)]
    [Arguments(PrimaryDatabaseProvider.Sqlite, false)]
    [Arguments(PrimaryDatabaseProvider.SqlServer, false)]
    [Arguments(PrimaryDatabaseProvider.MariaDb, true)]
    [Arguments(PrimaryDatabaseProvider.MySql, true)]
    public void ConfigurePersistenceServices_SelectsProviderNeutralLocksAndMySqlInterceptor(
        PrimaryDatabaseProvider provider,
        bool expectsMySqlInterceptor)
    {
        var services = new ServiceCollection();
        services.ConfigurePersistenceServices(
            BuildConfiguration(provider),
            skipLookupCacheInitializer: true,
            environmentName: "Production");
        using var serviceProvider = services.BuildServiceProvider();

        services.Single(service => service.ServiceType == typeof(ISettingMutationLock))
            .ImplementationType.Should().Be(typeof(RelationalSettingMutationLock));
        services.Single(service => service.ServiceType == typeof(IAtprotoSessionRefreshLock))
            .ImplementationType.Should().Be(typeof(RelationalAtprotoSessionRefreshLock));
        var options = serviceProvider.GetRequiredService<DbContextOptions<ExploreDbContext>>();
        var interceptors = options.FindExtension<CoreOptionsExtension>()?.Interceptors ?? [];

        interceptors.Any(interceptor => interceptor is MySqlNamedLockTransactionInterceptor)
            .Should().Be(expectsMySqlInterceptor);
    }

    [Test]
    [Arguments(PrimaryDatabaseProvider.MariaDb, "MariaDbServerVersion")]
    [Arguments(PrimaryDatabaseProvider.MySql, "MySqlServerVersion")]
    public void ConfigureApplication_SelectsMicrotingServerFlavor(
        PrimaryDatabaseProvider provider,
        string expectedServerVersionType)
    {
        var builder = new DbContextOptionsBuilder<ExploreDbContext>();

        PrimaryDatabaseProviderComposition.ConfigureApplication(builder, CreateOptions(provider));

        var extension = builder.Options.Extensions.Single(candidate =>
            candidate.GetType().Name == "MySqlOptionsExtension");
        var serverVersion = extension.GetType().GetProperty("ServerVersion")!.GetValue(extension);
        serverVersion!.GetType().Name.Should().Be(expectedServerVersionType);
    }

    [Test]
    public void MigrationAssemblyContract_IsStableForApplicationAndDataProtection()
    {
        PrimaryDatabaseProviderComposition.GetMigrationsAssemblyName(
                PrimaryDatabaseProvider.PostgreSql,
                PrimaryDatabaseMigrationTarget.Application)
            .Should().Be("Explore.Persistence");
        PrimaryDatabaseProviderComposition.GetMigrationsAssemblyName(
                PrimaryDatabaseProvider.PostgreSql,
                PrimaryDatabaseMigrationTarget.DataProtection)
            .Should().Be("Explore.Persistence");

        foreach (var provider in Enum.GetValues<PrimaryDatabaseProvider>()
                     .Where(candidate => candidate != PrimaryDatabaseProvider.PostgreSql))
        {
            PrimaryDatabaseProviderComposition.GetMigrationsAssemblyName(
                    provider,
                    PrimaryDatabaseMigrationTarget.Application)
                .Should().Be($"Explore.Persistence.Migrations.{provider}");
            PrimaryDatabaseProviderComposition.GetMigrationsAssemblyName(
                    provider,
                    PrimaryDatabaseMigrationTarget.DataProtection)
                .Should().Be($"Explore.Persistence.DataProtection.Migrations.{provider}");
        }
    }

    [Test]
    public void MigrationHistoryContract_SeparatesApplicationAndDataProtection()
    {
        foreach (var provider in Enum.GetValues<PrimaryDatabaseProvider>())
        {
            var application = PrimaryDatabaseProviderComposition.GetMigrationsHistoryTable(
                provider,
                PrimaryDatabaseMigrationTarget.Application);
            var dataProtection = PrimaryDatabaseProviderComposition.GetMigrationsHistoryTable(
                provider,
                PrimaryDatabaseMigrationTarget.DataProtection);

            application.Table.Should().NotBe(dataProtection.Table);
            application.Schema.Should().Be(dataProtection.Schema);

            if (provider is PrimaryDatabaseProvider.PostgreSql or PrimaryDatabaseProvider.SqlServer)
            {
                application.Should().Be(("__EFMigrationsHistory", "islamu_event"));
                dataProtection.Should().Be(("__EFDataProtectionMigrationsHistory", "islamu_event"));
            }
            else
            {
                application.Should().Be(("ie___EFMigrationsHistory", null));
                dataProtection.Should().Be(("ie___EFDataProtectionMigrationsHistory", null));
            }
        }
    }

    [Test]
    [Arguments(PrimaryDatabaseProvider.PostgreSql, "islamu_event", null)]
    [Arguments(PrimaryDatabaseProvider.SqlServer, "islamu_event", null)]
    [Arguments(PrimaryDatabaseProvider.Sqlite, null, "ie_")]
    [Arguments(PrimaryDatabaseProvider.MariaDb, null, "ie_")]
    [Arguments(PrimaryDatabaseProvider.MySql, null, "ie_")]
    public void DataProtectionModel_UsesFixedNamespacePolicy(
        PrimaryDatabaseProvider provider,
        string? expectedSchema,
        string? expectedPrefix)
    {
        var builder = new DbContextOptionsBuilder<DataProtectionKeyContext>();
        PrimaryDatabaseProviderComposition.ConfigureDataProtection(builder, CreateOptions(provider));
        using var context = new DataProtectionKeyContext(builder.Options);

        var entityType = context.Model.FindEntityType(typeof(DataProtectionKey))!;

        entityType.GetSchema().Should().Be(expectedSchema);
        if (expectedPrefix is not null)
        {
            entityType.GetTableName().Should().StartWith(expectedPrefix);
        }
    }

    [Test]
    [Arguments(PrimaryDatabaseProvider.PostgreSql, "Npgsql.EntityFrameworkCore.PostgreSQL")]
    [Arguments(PrimaryDatabaseProvider.Sqlite, "Microsoft.EntityFrameworkCore.Sqlite")]
    [Arguments(PrimaryDatabaseProvider.SqlServer, "Microsoft.EntityFrameworkCore.SqlServer")]
    [Arguments(PrimaryDatabaseProvider.MariaDb, "Microting.EntityFrameworkCore.MySql")]
    [Arguments(PrimaryDatabaseProvider.MySql, "Microting.EntityFrameworkCore.MySql")]
    public void AddExploreDataProtection_SelectsRequestedProvider(
        PrimaryDatabaseProvider provider,
        string expectedProviderName)
    {
        var services = new ServiceCollection();
        services.AddExploreDataProtection(BuildConfiguration(provider));
        using var serviceProvider = services.BuildServiceProvider();

        using var context = serviceProvider.GetRequiredService<DataProtectionKeyContext>();

        context.Database.ProviderName.Should().Be(expectedProviderName);
    }

    [Test]
    public void DesignTimeFactories_ProjectDiscretePostgresMigratorSettings()
    {
        var values = new Dictionary<string, string?>
        {
            ["Postgresql:Host"] = "pg.example.test",
            ["Postgresql:Database"] = "event_db",
            ["Postgresql:Username"] = "migrator",
            ["Postgresql:Password"] = "factory-secret",
        };

        using var application = new ExploreDbContextFactory().CreateDbContext(
            new ConfigurationBuilder().AddInMemoryCollection(values));
        using var dataProtection = new DataProtectionKeyContextFactory().CreateDbContext(
            new ConfigurationBuilder().AddInMemoryCollection(values));

        application.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
        dataProtection.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Test]
    public void DesignTimeFactories_PreserveExplicitStructuredProviderPriority()
    {
        var values = new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:Database"] = "factory-event.db",
            ["Postgresql:Host"] = "ignored.example.test",
            ["Postgresql:Database"] = "ignored",
            ["Postgresql:Username"] = "ignored",
            ["Postgresql:Password"] = "ignored-secret",
        };

        using var application = new ExploreDbContextFactory().CreateDbContext(
            new ConfigurationBuilder().AddInMemoryCollection(values));
        using var dataProtection = new DataProtectionKeyContextFactory().CreateDbContext(
            new ConfigurationBuilder().AddInMemoryCollection(values));

        application.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");
        dataProtection.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.Sqlite");
    }

    private static IConfiguration BuildConfiguration(PrimaryDatabaseProvider provider)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(BuildConfigurationValues(provider))
            .Build();
    }

    private static Dictionary<string, string?> BuildConfigurationValues(PrimaryDatabaseProvider provider)
    {
        var options = CreateOptions(provider);
        return new Dictionary<string, string?>
        {
            ["Database:Provider"] = provider.ToString(),
            ["Database:Host"] = options.Host,
            ["Database:Port"] = options.Port?.ToString(),
            ["Database:Database"] = options.Database,
            ["Database:Runtime:Username"] = options.Username,
            ["Database:Runtime:Password"] = options.Password,
            ["Database:Runtime:TlsMode"] = options.TlsMode.ToString(),
            ["Database:Runtime:ServerFlavor"] = options.ServerFlavor?.ToString(),
            ["Database:Runtime:ServerVersion"] = options.ServerVersion?.ToString(),
        };
    }

    private static PrimaryDatabaseConnectionOptions CreateOptions(PrimaryDatabaseProvider provider) =>
        provider == PrimaryDatabaseProvider.Sqlite
            ? new PrimaryDatabaseConnectionOptions
            {
                Role = PrimaryDatabaseRole.Runtime,
                Provider = provider,
                Database = "event.db",
            }
            : new PrimaryDatabaseConnectionOptions
            {
                Role = PrimaryDatabaseRole.Runtime,
                Provider = provider,
                Host = "database.example.test",
                Database = "event_db",
                Username = "event_user",
                Password = "composition-secret",
                TlsMode = PrimaryDatabaseTlsMode.Required,
                ServerFlavor = provider switch
                {
                    PrimaryDatabaseProvider.MariaDb => PrimaryDatabaseServerFlavor.MariaDb,
                    PrimaryDatabaseProvider.MySql => PrimaryDatabaseServerFlavor.MySql,
                    _ => null,
                },
                ServerVersion = provider switch
                {
                    PrimaryDatabaseProvider.MariaDb => new Version(11, 4),
                    PrimaryDatabaseProvider.MySql => new Version(8, 4),
                    _ => null,
                },
            };
}
