// ABOUTME: Selects the EF Core provider and migrations assembly for the configured primary database.
// ABOUTME: Keeps runtime, design-time, migration, and Data Protection composition on one closed switch.

using Explore.Persistence.Schema;
using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Explore.Persistence.Database;

public enum PrimaryDatabaseMigrationTarget
{
    Application,
    DataProtection,
}

public static class PrimaryDatabaseProviderComposition
{
    public const string PostgreSqlApplicationMigrationsAssembly = "Explore.Persistence";
    public const string PostgreSqlDataProtectionMigrationsAssembly = "Explore.Persistence";
    internal const string ApplicationMigrationsHistoryTable = "__EFMigrationsHistory";
    internal const string DataProtectionMigrationsHistoryTable = "__EFDataProtectionMigrationsHistory";

    public static PrimaryDatabaseConnectionResult ConfigureApplication(
        DbContextOptionsBuilder optionsBuilder,
        PrimaryDatabaseConnectionOptions options)
        => Configure(optionsBuilder, options, PrimaryDatabaseMigrationTarget.Application);

    public static PrimaryDatabaseConnectionResult ConfigureDataProtection(
        DbContextOptionsBuilder optionsBuilder,
        PrimaryDatabaseConnectionOptions options)
        => Configure(optionsBuilder, options, PrimaryDatabaseMigrationTarget.DataProtection);

    public static string GetMigrationsAssemblyName(
        PrimaryDatabaseProvider provider,
        PrimaryDatabaseMigrationTarget target)
    {
        if (provider == PrimaryDatabaseProvider.PostgreSql)
        {
            return target == PrimaryDatabaseMigrationTarget.Application
                ? PostgreSqlApplicationMigrationsAssembly
                : PostgreSqlDataProtectionMigrationsAssembly;
        }

        var providerName = provider.ToString();
        return target == PrimaryDatabaseMigrationTarget.Application
            ? $"Explore.Persistence.Migrations.{providerName}"
            : $"Explore.Persistence.DataProtection.Migrations.{providerName}";
    }

    private static PrimaryDatabaseConnectionResult Configure(
        DbContextOptionsBuilder optionsBuilder,
        PrimaryDatabaseConnectionOptions options,
        PrimaryDatabaseMigrationTarget target)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(options);

        var database = PrimaryDatabaseConfiguration.BuildConnectionString(options);
        var migrationsAssembly = GetMigrationsAssemblyName(options.Provider, target);
        var migrationsHistory = GetMigrationsHistoryTable(options.Provider, target, options.Schema);
        var modelSchema = options.Role == PrimaryDatabaseRole.Migrator
            ? PrimaryDatabaseConnectionOptions.DefaultSchema
            : options.Schema;
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder)
            .AddOrUpdateExtension(new RelationalNamespaceOptionsExtension(modelSchema, options.Schema));

        switch (options.Provider)
        {
            case PrimaryDatabaseProvider.PostgreSql:
                optionsBuilder.UseNpgsql(database.ConnectionString, providerOptions =>
                {
                    providerOptions.MigrationsAssembly(migrationsAssembly);
                    providerOptions.MigrationsHistoryTable(migrationsHistory.Table, migrationsHistory.Schema);
                    providerOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                    providerOptions.CommandTimeout(30);
                    if (target == PrimaryDatabaseMigrationTarget.Application)
                    {
                        providerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    }
                });
                optionsBuilder.ReplaceService<IMigrationsSqlGenerator, ConfigurableNpgsqlMigrationsSqlGenerator>();
                break;

            case PrimaryDatabaseProvider.Sqlite:
                optionsBuilder.UseSqlite(
                    database.ConnectionString,
                    providerOptions =>
                    {
                        providerOptions.MigrationsAssembly(migrationsAssembly);
                        providerOptions.MigrationsHistoryTable(migrationsHistory.Table);
                    });
                break;

            case PrimaryDatabaseProvider.SqlServer:
                optionsBuilder.UseSqlServer(
                    database.ConnectionString,
                    providerOptions =>
                    {
                        providerOptions.MigrationsAssembly(migrationsAssembly);
                        providerOptions.MigrationsHistoryTable(migrationsHistory.Table, migrationsHistory.Schema);
                    });
                optionsBuilder.ReplaceService<IMigrationsSqlGenerator, ConfigurableSqlServerMigrationsSqlGenerator>();
                break;

            case PrimaryDatabaseProvider.MariaDb:
                optionsBuilder.UseMySql(
                    database.ConnectionString,
                    new MariaDbServerVersion(options.ServerVersion!),
                    providerOptions =>
                    {
                        providerOptions.MigrationsAssembly(migrationsAssembly);
                        providerOptions.MigrationsHistoryTable(migrationsHistory.Table);
                    });
                break;

            case PrimaryDatabaseProvider.MySql:
                optionsBuilder.UseMySql(
                    database.ConnectionString,
                    new MySqlServerVersion(options.ServerVersion!),
                    providerOptions =>
                    {
                        providerOptions.MigrationsAssembly(migrationsAssembly);
                        providerOptions.MigrationsHistoryTable(migrationsHistory.Table);
                    });
                break;

            default:
                throw new InvalidOperationException($"Unsupported primary database provider '{options.Provider}'.");
        }

        optionsBuilder.UseSnakeCaseNamingConvention();
        return database;
    }

    internal static (string Table, string? Schema) GetMigrationsHistoryTable(
        PrimaryDatabaseProvider provider,
        PrimaryDatabaseMigrationTarget target,
        string schema = PrimaryDatabaseConnectionOptions.DefaultSchema)
    {
        var table = target == PrimaryDatabaseMigrationTarget.Application
            ? ApplicationMigrationsHistoryTable
            : DataProtectionMigrationsHistoryTable;

        return provider is PrimaryDatabaseProvider.PostgreSql or PrimaryDatabaseProvider.SqlServer
            ? (table, schema)
            : (RelationalModelNamespace.Prefix + table, null);
    }
}
