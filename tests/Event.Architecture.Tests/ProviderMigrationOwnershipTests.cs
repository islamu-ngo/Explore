// ABOUTME: Verifies provider migration ownership and portable identifiers through configured EF Core models.
// ABOUTME: Locks application and Data Protection assemblies, history tables, and Jetstream cursor mappings.

using Explore.Domain.Federation;
using Explore.Domain;
using Explore.Persistence;
using Explore.Persistence.Database;
using Explore.Persistence.ValueGenerators;
using Explore.Secrets.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

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
                    .StartsWith("ie_", StringComparison.Ordinal);
                await Assert.That(dataProtection.MigrationsHistoryTableName)
                    .StartsWith("ie_", StringComparison.Ordinal);
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

    [Test]
    public async Task ProviderModelsUsePortableJetstreamCursorColumnNames()
    {
        foreach (var provider in Enum.GetValues<PrimaryDatabaseProvider>())
        {
            var builder = new DbContextOptionsBuilder<ExploreDbContext>();
            PrimaryDatabaseProviderComposition.ConfigureApplication(builder, CreateOptions(provider));
            await using var db = new ExploreDbContext(builder.Options);

            await AssertPortableCursorMapping<AtprotoJetstreamConsumerState>(
                db,
                nameof(AtprotoJetstreamConsumerState.Cursor),
                "ck_atproto_jetstream_cursor");
            await AssertPortableCursorMapping<AtprotoJetstreamQuarantine>(
                db,
                nameof(AtprotoJetstreamQuarantine.Cursor),
                "ck_atproto_jetstream_quarantine_cursor");
        }
    }

    [Test]
    public async Task ProviderModelsUseClientUuidV7AndPortableMySqlDefaults()
    {
        foreach (var provider in Enum.GetValues<PrimaryDatabaseProvider>())
        {
            var builder = new DbContextOptionsBuilder<ExploreDbContext>();
            PrimaryDatabaseProviderComposition.ConfigureApplication(builder, CreateOptions(provider));
            await using var db = new ExploreDbContext(builder.Options);
            IModel model = db.GetService<IDesignTimeModel>().Model;

            string[] defaultSql = model.GetEntityTypes()
                .SelectMany(entityType => entityType.GetProperties())
                .Select(property => property.GetDefaultValueSql())
                .Where(sql => sql is not null)
                .Cast<string>()
                .ToArray();
            await Assert.That(defaultSql.Any(sql =>
                sql.Contains("uuidv7()", StringComparison.OrdinalIgnoreCase))).IsFalse();

            IProperty idProperty = model.FindEntityType(typeof(AtprotoJetstreamConsumerState))!
                .FindProperty(nameof(AtprotoJetstreamConsumerState.Id))!;
            Type? generatorType = idProperty.GetValueGeneratorFactory()?
                .Invoke(idProperty, idProperty.DeclaringType)
                .GetType();
            await Assert.That(generatorType).IsEqualTo(typeof(GuidVersion7ValueGenerator));

            if (provider is PrimaryDatabaseProvider.MariaDb or PrimaryDatabaseProvider.MySql)
            {
                string[] unparenthesizedExpressions = defaultSql
                    .Where(sql => sql.Contains("UTC_TIMESTAMP()", StringComparison.OrdinalIgnoreCase))
                    .Where(sql => !sql.StartsWith('(') || !sql.EndsWith(')'))
                    .ToArray();
                await Assert.That(unparenthesizedExpressions).IsEmpty();

                string[] overlongColumnNames = model.GetEntityTypes()
                    .SelectMany(entityType => entityType.GetProperties())
                    .Select(property => property.GetColumnName())
                    .Where(columnName => columnName is not null && columnName.Length > 64)
                    .Cast<string>()
                    .ToArray();
                await Assert.That(overlongColumnNames).IsEmpty();

                IProperty[] portableCoalesceProperties = model.GetEntityTypes()
                    .SelectMany(entityType => entityType.GetProperties())
                    .Where(property => property.Name is
                        nameof(WebhookConsumer.ConfigurationScopeId) or
                        "RegistrationWorkflowVersionKey" or
                        "RegistrationProviderBindingKey" or
                        nameof(RegistrationRequirement.AppliesToSubjectKey) or
                        nameof(RegistrationAnswer.RequirementSubjectKey) or
                        nameof(RegistrationAnswer.EffectiveSubjectIdentity))
                    .ToArray();
                await Assert.That(portableCoalesceProperties).IsNotEmpty();
                await Assert.That(portableCoalesceProperties)
                    .All(property => property.GetComputedColumnSql() is null);
                await Assert.That(portableCoalesceProperties)
                    .All(property => property.ValueGenerated == ValueGenerated.Never);
                await Assert.That(portableCoalesceProperties)
                    .All(property => property.GetBeforeSaveBehavior() == PropertySaveBehavior.Save);
            }
        }
    }

    private static async Task AssertPortableCursorMapping<TEntity>(
        ExploreDbContext db,
        string propertyName,
        string constraintName)
    {
        IModel model = db.GetService<IDesignTimeModel>().Model;
        IEntityType entityType = model.FindEntityType(typeof(TEntity))!;
        var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        string? columnName = entityType.FindProperty(propertyName)!.GetColumnName(table);
        string? constraintSql = entityType.GetCheckConstraints()
            .Single(constraint => constraint.Name == constraintName)
            .Sql;

        await Assert.That(columnName).IsEqualTo("jetstream_cursor");
        await Assert.That(constraintSql).IsEqualTo("jetstream_cursor >= 0");
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
