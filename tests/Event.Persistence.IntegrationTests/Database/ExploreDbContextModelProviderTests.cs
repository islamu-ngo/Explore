// ABOUTME: Verifies the shared Explore EF Core model builds for every supported primary provider.
// ABOUTME: Asserts configurable schema or fixed-prefix naming and PostgreSQL-only model defenses.

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
    [Arguments("PostgreSql")]
    [Arguments("Sqlite")]
    [Arguments("SqlServer")]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public void UuidV7DefaultsUseClientGenerationOnEveryProvider(string provider)
    {
        using var context = CreateContext(provider);
        var property = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Explore.Domain.EventRegistration))!
            .FindProperty(nameof(Explore.Domain.EventRegistration.Id))!;

        property.GetDefaultValueSql().Should().BeNull();
        var generator = property.GetValueGeneratorFactory()!(property, property.DeclaringType)
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
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public void MySqlExternalBindingHashKeysPreserveUnicodeValueCapacity(string provider)
    {
        using var context = CreateContext(provider);
        var entityType = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Explore.Domain.ExternalBinding))!;

        var externalId = entityType.FindProperty(nameof(Explore.Domain.ExternalBinding.ExternalId))!;
        externalId.GetMaxLength().Should().Be(512);
        externalId.GetCollation().Should().BeNull();
        externalId.GetCharSet().Should().NotBe("ascii");
        entityType.FindProperty("ExternalGlobalUniquenessHash")!.GetColumnType().Should().Be("binary(32)");
    }

    [Test]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public void MySqlExternalBindingHashKeysSeparateGlobalAndTenantScopes(string provider)
    {
        using var context = CreateContext(provider);
        var entityType = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Explore.Domain.ExternalBinding))!;

        entityType.FindProperty("ExternalGlobalUniquenessHash")!.IsNullable.Should().BeTrue();
        entityType.FindProperty("ExternalTenantUniquenessHash")!.IsNullable.Should().BeTrue();
        entityType.FindProperty("InternalGlobalUniquenessHash")!.IsNullable.Should().BeTrue();
        entityType.FindProperty("InternalTenantUniquenessHash")!.IsNullable.Should().BeTrue();

        ExploreDbContext.ComputeMySqlUniquenessHash("provider", "system", "type", "identity")
            .Should().NotEqual(ExploreDbContext.ComputeMySqlUniquenessHash(
                "provider", "system", "type", "identity", Guid.Empty.ToString("D")));
    }

    [Test]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public void MySqlExternalBindingDuplicateProtectionUsesFourUniqueHashIndexes(string provider)
    {
        using var context = CreateContext(provider);
        var indexes = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Explore.Domain.ExternalBinding))!
            .GetIndexes()
            .ToArray();

        indexes.Where(index => index.GetDatabaseName()?.EndsWith("_hash", StringComparison.Ordinal) == true)
            .Should().HaveCount(4).And.OnlyContain(index => index.IsUnique);
        indexes.Select(index => index.GetDatabaseName()).Intersect([
                "ix_external_bindings_external_global_unique",
                "ix_external_bindings_external_tenant_unique",
                "ix_external_bindings_internal_global_unique",
                "ix_external_bindings_internal_tenant_unique"
            ])
            .Should().BeEmpty();
    }

    [Test]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public void MySqlExternalBindingHashInputUsesLengthPrefixedUtf8Components(string provider)
    {
        using var context = CreateContext(provider);
        _ = context.Model;
        ExploreDbContext.ComputeMySqlUniquenessHash("ab", "c")
            .Should().NotEqual(ExploreDbContext.ComputeMySqlUniquenessHash("a", "bc"));
        ExploreDbContext.ComputeMySqlUniquenessHash("مزوّد", "外部識別子")
            .Should().HaveCount(32);
    }

    [Test]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public void MySqlIndexesFitInnoDbKeyLimit(string provider)
    {
        using var context = CreateContext(provider);
        var oversized = context.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetIndexes())
            .Select(index => new { Index = index, Width = EstimateMySqlIndexWidth(index) })
            .Where(candidate => candidate.Width > 3072)
            .Select(candidate => $"{candidate.Index.DeclaringEntityType.GetTableName()}.{candidate.Index.GetDatabaseName()}={candidate.Width}")
            .ToArray();

        oversized.Should().BeEmpty(
            "every MySQL-family index must fit 3072 bytes; oversized: {0}",
            string.Join(", ", oversized));
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
        if (provider is "MariaDb" or "MySql")
        {
            properties.Where(property => !IsMySqlAsciiIdentityProperty(property))
                .Should().OnlyContain(property => property.GetCollation() == null);
            model.FindEntityType(typeof(Explore.Domain.AtprotoIdentity))!
                .FindProperty(nameof(Explore.Domain.AtprotoIdentity.Did))!
                .GetCollation().Should().Be("ascii_bin");
        }
        else
        {
            properties.Should().OnlyContain(property => property.GetCollation() == null);
        }
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

    private static bool IsMySqlAsciiIdentityProperty(IReadOnlyProperty property) =>
        (property.DeclaringType.ClrType == typeof(Explore.Domain.AtprotoIdentity) &&
         property.Name == nameof(Explore.Domain.AtprotoIdentity.Did)) ||
        (property.DeclaringType.ClrType == typeof(Explore.Domain.UserAuthenticationToken) &&
         property.Name is nameof(Explore.Domain.UserAuthenticationToken.Provider) or
             nameof(Explore.Domain.UserAuthenticationToken.SubjectDid));

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

    private static int EstimateMySqlIndexWidth(IReadOnlyIndex index)
    {
        var prefixLengths = index.FindAnnotation("MySql:IndexPrefixLength")?.Value as int[];
        return index.Properties.Select((property, position) =>
        {
            if (property.ClrType == typeof(string))
            {
                var configuredLength = property.GetMaxLength() ?? 0;
                var prefixLength = prefixLengths is not null && prefixLengths[position] > 0
                    ? prefixLengths[position]
                    : configuredLength;
                var bytesPerCharacter = property.GetCharSet() == "ascii" ? 1 : 4;
                return prefixLength * bytesPerCharacter;
            }

            if (property.ClrType == typeof(Guid) || property.ClrType == typeof(Guid?))
            {
                return 36;
            }

            if (property.ClrType == typeof(byte[]))
            {
                var columnType = property.GetColumnType();
                return columnType == "binary(32)" ? 32 : 3073;
            }

            return 8;
        }).Sum();
    }
}
