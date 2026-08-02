// ABOUTME: Verifies the shared Explore EF Core model builds for every supported primary provider.
// ABOUTME: Asserts fixed schema or prefix naming and PostgreSQL-only model defenses.

using Explore.Persistence;
using Explore.Persistence.Schema;
using Explore.Persistence.ValueGenerators;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Database;

public sealed class ExploreDbContextModelProviderTests
{
    [Test]
    [Arguments("PostgreSql", true)]
    [Arguments("Sqlite", false)]
    public void UuidV7DefaultsUseServerGenerationOnlyOnPostgreSql(string provider, bool usesServerDefault)
    {
        using var context = CreateContext(provider);
        var property = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Explore.Domain.EventRegistration))!
            .FindProperty(nameof(Explore.Domain.EventRegistration.Id))!;

        (property.GetDefaultValueSql() == "uuidv7()").Should().Be(usesServerDefault);
        var factory = property.GetValueGeneratorFactory();

        if (usesServerDefault)
        {
            factory.Should().BeNull();
            return;
        }

        var generator = factory!(property, property.DeclaringType)
            .Should().BeOfType<GuidVersion7ValueGenerator>().Subject;
        generator.Next(null!).Version.Should().Be(7);
    }

    [Test]
    public async Task PostgreSqlConstraintApplierSkipsOtherProviders()
    {
        await using var context = CreateContext("Sqlite");

        await PostgresModelConstraintApplier.ApplyAsync(context);
    }

    [Test]
    [Arguments("PostgreSql")]
    [Arguments("Sqlite")]
    [Arguments("SqlServer")]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public void ModelBuildsWithFixedNamespaceAndProviderCapabilities(string provider)
    {
        using var context = CreateContext(provider);

        var model = context.Model;
        model.GetRelationalModel().Tables.Should().NotBeEmpty();

        if (provider is "PostgreSql" or "SqlServer")
        {
            model.GetDefaultSchema().Should().Be("islamu_event");
        }
        else
        {
            model.GetEntityTypes()
                .Where(entityType => entityType.GetTableName() is not null)
                .Should().OnlyContain(entityType => entityType.GetTableName()!.StartsWith("ie_", StringComparison.Ordinal));
        }

        var annotations = model.GetEntityTypes().SelectMany(entityType => entityType.GetAnnotations()).ToArray();
        annotations.Any(annotation => annotation.Name.StartsWith("Explore:PostgresExclusionConstraint:", StringComparison.Ordinal))
            .Should().Be(provider == "PostgreSql");
    }

    [Test]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public void MySqlModelsHaveDistinctBoundedCustomPropertyOptionForeignKeys(string provider)
    {
        using var context = CreateContext(provider);

        var relationalModel = context.Model.GetRelationalModel();
        var tables = relationalModel.Tables.ToArray();
        tables.SelectMany(table => table.UniqueConstraints).Select(constraint => constraint.Name)
            .Should().OnlyContain(name => name.Length <= 64);
        tables.SelectMany(table => table.Indexes).Select(index => index.Name)
            .Should().OnlyContain(name => name.Length <= 64);
        tables.SelectMany(table => table.ForeignKeyConstraints).Select(constraint => constraint.Name)
            .Should().OnlyContain(name => name.Length <= 64);

        var optionTable = tables.Single(table => table.Name == "ie_custom_property_options");
        var optionForeignKeys = optionTable.ForeignKeyConstraints
            .Where(constraint => constraint.PrincipalTable.Name is
                "ie_custom_property_options" or "ie_custom_property_definitions")
            .Select(constraint => constraint.Name)
            .ToArray();

        optionForeignKeys.Should().HaveCount(2).And.OnlyHaveUniqueItems();
    }

    [Test]
    [Arguments("PostgreSql")]
    [Arguments("Sqlite")]
    [Arguments("SqlServer")]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public void ProviderModelNormalizesPostgreSqlOnlyRelationalAnnotations(string provider)
    {
        using var context = CreateContext(provider);

        var model = context.GetService<IDesignTimeModel>().Model;
        var checkConstraints = model.GetEntityTypes().SelectMany(entityType => entityType.GetCheckConstraints()).ToArray();
        var leaseShape = checkConstraints.Single(constraint =>
            constraint.Name == "ck_atproto_jetstream_lease_shape");

        if (provider == "PostgreSql")
        {
            leaseShape.Sql.Should().Contain("btrim(lease_owner) <> ''");
            return;
        }

        leaseShape.Sql.Should().Contain("trim(lease_owner) <> ''");
        checkConstraints.Any(constraint =>
            constraint.Name == "ck_notifications_entity_reference_shape").Should().BeFalse();
        checkConstraints.Any(constraint =>
            new[] { "btrim(", "::", "jsonb_", "num_nonnulls(", "octet_length(", "extract(", "~" }
                .Any(token => constraint.Sql.Contains(token, StringComparison.OrdinalIgnoreCase))).Should().BeFalse();

        var properties = model.GetEntityTypes().SelectMany(entityType => entityType.GetProperties()).ToArray();
        properties.Should().OnlyContain(property => property.GetCollation() == null);
        properties.Any(property =>
        {
            var columnType = property.GetColumnType();
            return
            columnType is not null && new[] { "bytea", "jsonb", "time without time zone", "timestamp with time zone", "uuid" }
                .Contains(columnType, StringComparer.OrdinalIgnoreCase);
        }).Should().BeFalse();
        properties.Any(property =>
        {
            var defaultSql = property.GetDefaultValueSql();
            return
            defaultSql is not null && new[] { "uuidv7()", "NOW()", "statement_timestamp()" }
                .Contains(defaultSql, StringComparer.OrdinalIgnoreCase);
        }).Should().BeFalse();
        properties.Any(property => property.GetComputedColumnSql()?.Contains(
            "::uuid", StringComparison.OrdinalIgnoreCase) == true).Should().BeFalse();

        var sqlAnnotations = properties.SelectMany(property => new[]
            {
                property.GetColumnType(),
                property.GetDefaultValueSql(),
                property.GetComputedColumnSql()
            })
            .Concat(checkConstraints.Select(constraint => constraint.Sql))
            .Concat(model.GetEntityTypes().SelectMany(entityType => entityType.GetIndexes())
                .Select(index => index.GetFilter()))
            .Where(sql => sql is not null)
            .Cast<string>()
            .ToArray();
        var postgreSqlTokens = new[]
        {
            "uuidv7", "jsonb", "btrim", "::", "statement_timestamp", "INTERVAL", "infinity",
            "~", "num_nonnulls", "octet_length", "extract("
        };
        sqlAnnotations.Any(sql => postgreSqlTokens.Any(token =>
            sql.Contains(token, StringComparison.OrdinalIgnoreCase))).Should().BeFalse();

        if (provider == "SqlServer")
        {
            model.GetEntityTypes().SelectMany(entityType => entityType.GetIndexes()).Any(index =>
                index.GetFilter() is { } filter &&
                    (filter.Contains("true", StringComparison.OrdinalIgnoreCase) ||
                     filter.Contains("false", StringComparison.OrdinalIgnoreCase))).Should().BeFalse();
        }
    }

    [Test]
    public async Task SqliteCanCreateTheNormalizedApplicationSchema()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"islamu-event-model-{Guid.NewGuid():N}.db");
        try
        {
            var builder = new DbContextOptionsBuilder<ExploreDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .UseSnakeCaseNamingConvention();
            await using var context = new ExploreDbContext(builder.Options);

            (await context.Database.EnsureCreatedAsync()).Should().BeTrue();
        }
        finally
        {
            File.Delete(databasePath);
            File.Delete(databasePath + "-shm");
            File.Delete(databasePath + "-wal");
        }
    }

    private static ExploreDbContext CreateContext(string provider)
    {
        var builder = new DbContextOptionsBuilder<ExploreDbContext>();
        switch (provider)
        {
            case "PostgreSql":
                builder.UseNpgsql("Host=localhost;Database=model;Username=model;Password=model");
                break;
            case "Sqlite":
                builder.UseSqlite("Data Source=:memory:");
                break;
            case "SqlServer":
                builder.UseSqlServer("Server=localhost;Database=model;User Id=model;Password=model;TrustServerCertificate=True");
                break;
            case "MariaDb":
                builder.UseMySql("Server=localhost;Database=model;User=model;Password=model", new MariaDbServerVersion(new Version(10, 11)));
                break;
            case "MySql":
                builder.UseMySql("Server=localhost;Database=model;User=model;Password=model", new MySqlServerVersion(new Version(8, 0)));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }

        builder.UseSnakeCaseNamingConvention();
        return new ExploreDbContext(builder.Options);
    }
}
