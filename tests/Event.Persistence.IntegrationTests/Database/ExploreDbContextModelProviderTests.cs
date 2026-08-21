// ABOUTME: Verifies the shared Explore EF Core model builds for every supported primary provider.
// ABOUTME: Asserts configurable schema or fixed-prefix naming and PostgreSQL-only model defenses.

using Explore.Persistence;
using Explore.Persistence.Schema;
using Explore.Persistence.ValueGenerators;
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
    public async Task UuidV7DefaultsUseClientGenerationOnEveryProvider(string provider)
    {
        using var context = CreateContext(provider);
        var property = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Explore.Domain.EventRegistration))!
            .FindProperty(nameof(Explore.Domain.EventRegistration.Id))!;

        await Assert.That(property.GetDefaultValueSql()).IsNull();
        var generator = property.GetValueGeneratorFactory()!(property, property.DeclaringType);
        await Assert.That(generator).IsTypeOf<GuidVersion7ValueGenerator>();
        await Assert.That(((GuidVersion7ValueGenerator)generator).Next(null!).Version).IsEqualTo(7);
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
    public async Task ModelBuildsWithFixedNamespaceAndProviderCapabilities(string provider)
    {
        using var context = CreateContext(provider);

        var model = context.Model;
        await Assert.That(model.GetRelationalModel().Tables).IsNotEmpty();

        if (provider is "PostgreSql" or "SqlServer")
        {
            await Assert.That(model.GetDefaultSchema()).IsEqualTo("islamu_event");
        }
        else
        {
            await Assert.That(model.GetEntityTypes()
                .Where(entityType => entityType.GetTableName() is not null)
                .All(entityType => entityType.GetTableName()!.StartsWith("ie_", StringComparison.Ordinal))).IsTrue();
        }

        var annotations = model.GetEntityTypes().SelectMany(entityType => entityType.GetAnnotations()).ToArray();
        await Assert.That(annotations.Any(annotation => annotation.Name.StartsWith("Explore:PostgresExclusionConstraint:", StringComparison.Ordinal)))
            .IsEqualTo(provider == "PostgreSql");
    }

    [Test]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public async Task MySqlModelsHaveDistinctBoundedCustomPropertyOptionForeignKeys(string provider)
    {
        using var context = CreateContext(provider);

        var relationalModel = context.Model.GetRelationalModel();
        var tables = relationalModel.Tables.ToArray();
        await Assert.That(tables.SelectMany(table => table.UniqueConstraints).Select(constraint => constraint.Name).All(name => name.Length <= 64)).IsTrue();
        await Assert.That(tables.SelectMany(table => table.Indexes).Select(index => index.Name).All(name => name.Length <= 64)).IsTrue();
        await Assert.That(tables.SelectMany(table => table.ForeignKeyConstraints).Select(constraint => constraint.Name).All(name => name.Length <= 64)).IsTrue();

        var optionTable = tables.Single(table => table.Name == "ie_custom_property_options");
        var optionForeignKeys = optionTable.ForeignKeyConstraints
            .Where(constraint => constraint.PrincipalTable.Name is
                "ie_custom_property_options" or "ie_custom_property_definitions")
            .Select(constraint => constraint.Name)
            .ToArray();

        await Assert.That(optionForeignKeys).Count().IsEqualTo(2);
        await Assert.That(optionForeignKeys.Distinct()).Count().IsEqualTo(2);
    }

    [Test]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public async Task MySqlExternalBindingHashKeysPreserveUnicodeValueCapacity(string provider)
    {
        using var context = CreateContext(provider);
        var entityType = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Explore.Domain.ExternalBinding))!;

        var externalId = entityType.FindProperty(nameof(Explore.Domain.ExternalBinding.ExternalId))!;
        await Assert.That(externalId.GetMaxLength()).IsEqualTo(512);
        await Assert.That(externalId.GetCollation()).IsNull();
        await Assert.That(externalId.GetCharSet()).IsNotEqualTo("ascii");
        await Assert.That(entityType.FindProperty("ExternalGlobalUniquenessHash")!.GetColumnType()).IsEqualTo("binary(32)");
    }

    [Test]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public async Task MySqlExternalBindingHashKeysSeparateGlobalAndTenantScopes(string provider)
    {
        using var context = CreateContext(provider);
        var entityType = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Explore.Domain.ExternalBinding))!;

        await Assert.That(entityType.FindProperty("ExternalGlobalUniquenessHash")!.IsNullable).IsTrue();
        await Assert.That(entityType.FindProperty("ExternalTenantUniquenessHash")!.IsNullable).IsTrue();
        await Assert.That(entityType.FindProperty("InternalGlobalUniquenessHash")!.IsNullable).IsTrue();
        await Assert.That(entityType.FindProperty("InternalTenantUniquenessHash")!.IsNullable).IsTrue();

        await Assert.That(ExploreDbContext.ComputeMySqlUniquenessHash("provider", "system", "type", "identity"))
            .IsNotEqualTo(ExploreDbContext.ComputeMySqlUniquenessHash(
                "provider", "system", "type", "identity", Guid.Empty.ToString("D")));
    }

    [Test]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public async Task MySqlExternalBindingDuplicateProtectionUsesFourUniqueHashIndexes(string provider)
    {
        using var context = CreateContext(provider);
        var indexes = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Explore.Domain.ExternalBinding))!
            .GetIndexes()
            .ToArray();

        var hashIndexes = indexes.Where(index => index.GetDatabaseName()?.EndsWith("_hash", StringComparison.Ordinal) == true).ToArray();
        await Assert.That(hashIndexes).Count().IsEqualTo(4);
        await Assert.That(hashIndexes.All(index => index.IsUnique)).IsTrue();
        await Assert.That(indexes.Select(index => index.GetDatabaseName()).Intersect([
                "ix_external_bindings_external_global_unique",
                "ix_external_bindings_external_tenant_unique",
                "ix_external_bindings_internal_global_unique",
                "ix_external_bindings_internal_tenant_unique"
            ])).IsEmpty();
    }

    [Test]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public async Task MySqlExternalBindingHashInputUsesLengthPrefixedUtf8Components(string provider)
    {
        using var context = CreateContext(provider);
        _ = context.Model;
        await Assert.That(ExploreDbContext.ComputeMySqlUniquenessHash("ab", "c"))
            .IsNotEqualTo(ExploreDbContext.ComputeMySqlUniquenessHash("a", "bc"));
        await Assert.That(ExploreDbContext.ComputeMySqlUniquenessHash("مزوّد", "外部識別子")).Count().IsEqualTo(32);
    }

    [Test]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public async Task MySqlIndexesFitInnoDbKeyLimit(string provider)
    {
        using var context = CreateContext(provider);
        var oversized = context.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetIndexes())
            .Select(index => new { Index = index, Width = EstimateMySqlIndexWidth(index) })
            .Where(candidate => candidate.Width > 3072)
            .Select(candidate => $"{candidate.Index.DeclaringEntityType.GetTableName()}.{candidate.Index.GetDatabaseName()}={candidate.Width}")
            .ToArray();

        await Assert.That(oversized).IsEmpty();
    }

    [Test]
    [Arguments("PostgreSql")]
    [Arguments("Sqlite")]
    [Arguments("SqlServer")]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public async Task ProviderModelNormalizesPostgreSqlOnlyRelationalAnnotations(string provider)
    {
        using var context = CreateContext(provider);

        var model = context.GetService<IDesignTimeModel>().Model;
        var checkConstraints = model.GetEntityTypes().SelectMany(entityType => entityType.GetCheckConstraints()).ToArray();
        var leaseShape = checkConstraints.Single(constraint =>
            constraint.Name == "ck_atproto_jetstream_lease_shape");

        if (provider == "PostgreSql")
        {
            await Assert.That(leaseShape.Sql).Contains("btrim(lease_owner) <> ''");
            return;
        }

        await Assert.That(leaseShape.Sql).Contains("trim(lease_owner) <> ''");
        await Assert.That(checkConstraints.Any(constraint =>
            constraint.Name == "ck_notifications_entity_reference_shape")).IsFalse();
        await Assert.That(checkConstraints.Any(constraint =>
            new[] { "btrim(", "::", "jsonb_", "num_nonnulls(", "octet_length(", "extract(", "~" }
                .Any(token => constraint.Sql.Contains(token, StringComparison.OrdinalIgnoreCase)))).IsFalse();

        var properties = model.GetEntityTypes().SelectMany(entityType => entityType.GetProperties()).ToArray();
        if (provider is "MariaDb" or "MySql")
        {
            await Assert.That(properties.Where(property => !IsMySqlAsciiIdentityProperty(property))
                .All(property => property.GetCollation() == null)).IsTrue();
            await Assert.That(model.FindEntityType(typeof(Explore.Domain.AtprotoIdentity))!
                .FindProperty(nameof(Explore.Domain.AtprotoIdentity.Did))!
                .GetCollation()).IsEqualTo("ascii_bin");
        }
        else
        {
            await Assert.That(properties.All(property => property.GetCollation() == null)).IsTrue();
        }
        await Assert.That(properties.Any(property =>
        {
            var columnType = property.GetColumnType();
            return
            columnType is not null && new[] { "bytea", "jsonb", "time without time zone", "timestamp with time zone", "uuid" }
                .Contains(columnType, StringComparer.OrdinalIgnoreCase);
        })).IsFalse();
        await Assert.That(properties.Any(property =>
        {
            var defaultSql = property.GetDefaultValueSql();
            return
            defaultSql is not null && new[] { "uuidv7()", "NOW()", "statement_timestamp()" }
                .Contains(defaultSql, StringComparer.OrdinalIgnoreCase);
        })).IsFalse();
        await Assert.That(properties.Any(property => property.GetComputedColumnSql()?.Contains(
            "::uuid", StringComparison.OrdinalIgnoreCase) == true)).IsFalse();

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
        await Assert.That(sqlAnnotations.Any(sql => postgreSqlTokens.Any(token =>
            sql.Contains(token, StringComparison.OrdinalIgnoreCase)))).IsFalse();

        if (provider == "SqlServer")
        {
            await Assert.That(model.GetEntityTypes().SelectMany(entityType => entityType.GetIndexes()).Any(index =>
                index.GetFilter() is { } filter &&
                    (filter.Contains("true", StringComparison.OrdinalIgnoreCase) ||
                     filter.Contains("false", StringComparison.OrdinalIgnoreCase)))).IsFalse();
        }
    }

    [Test]
    [Arguments("PostgreSql")]
    [Arguments("Sqlite")]
    [Arguments("SqlServer")]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public async Task PromotionModelUsesPortableOneActiveReservationShape(string provider)
    {
        using var context = CreateContext(provider);
        IModel model = context.GetService<IDesignTimeModel>().Model;

        IEntityType reservation = model.FindEntityType(typeof(Explore.Domain.PromotionReservation))!;
        await Assert.That(reservation).IsNotNull();
        await Assert.That(reservation.FindDeclaredQueryFilter(Explore.Persistence.QueryFilters.QueryFilterNames.Tenant)).IsNotNull();
        await Assert.That(reservation.FindIndex([
            reservation.FindProperty(nameof(Explore.Domain.PromotionReservation.RegistrationOrderId))!,
            reservation.FindProperty(nameof(Explore.Domain.PromotionReservation.OrderReservationSlot))!
        ])?.IsUnique).IsTrue();
        await Assert.That(reservation.GetCheckConstraints().Any(constraint => constraint.Name == "ck_promotion_reservation_active_slot")).IsTrue();
        await Assert.That(reservation.GetCheckConstraints().Any(constraint => constraint.Name == "ck_promotion_reservation_status_timestamps")).IsTrue();

        IEntityType code = model.FindEntityType(typeof(Explore.Domain.PromotionCode))!;
        await Assert.That(code.FindProperty("LookupDigest")!.GetMaxLength()).IsEqualTo(128);
        await Assert.That(code.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([
            "TenantId", "ScopeEventId", "ScopeTicketCatalogVersionId", "LookupKeyVersion", "LookupDigest"
        ]))).IsTrue();
        await Assert.That(code.GetIndexes().Where(index => index.Properties.Any(property => property.Name == "IsActive")).All(index => index.GetFilter() is null)).IsTrue();
    }

    [Test]
    [Arguments("PostgreSql")]
    [Arguments("Sqlite")]
    [Arguments("SqlServer")]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public async Task PaymentAttemptModelUsesPortableActiveSlotAndDispatchFenceShape(string provider)
    {
        using var context = CreateContext(provider);
        IModel model = context.GetService<IDesignTimeModel>().Model;

        IEntityType attempt = model.FindEntityType(typeof(Explore.Domain.PaymentAttempt))!;
        await Assert.That(attempt).IsNotNull();
        await Assert.That(attempt.FindDeclaredQueryFilter(Explore.Persistence.QueryFilters.QueryFilterNames.Tenant)).IsNotNull();
        await Assert.That(attempt.FindIndex([
            attempt.FindProperty(nameof(Explore.Domain.PaymentAttempt.ActiveScopeKey))!,
            attempt.FindProperty(nameof(Explore.Domain.PaymentAttempt.ActiveUniquenessSlot))!
        ])?.IsUnique).IsTrue();
        await Assert.That(attempt.FindIndex([attempt.FindProperty(nameof(Explore.Domain.PaymentAttempt.ProviderIdempotencyKey))!])?.IsUnique).IsTrue();
        string shadowStatusPropertyName = nameof(Explore.Domain.PaymentAttempt.PaymentAttemptStatusId) + "1";
        string shadowStatusColumnName = "payment_attempt_status_id" + "1";
        await Assert.That(attempt.FindProperty(shadowStatusPropertyName)).IsNull();
        await Assert.That(attempt.GetProperties().Any(property => property.GetColumnName().Equals(shadowStatusColumnName, StringComparison.OrdinalIgnoreCase))).IsFalse();
        await Assert.That(attempt.GetCheckConstraints().Any(constraint => constraint.Name == "ck_payment_attempts_active_slot")).IsTrue();
        await Assert.That(attempt.GetCheckConstraints().Any(constraint => constraint.Name == "ck_payment_attempts_amounts")).IsTrue();

        IEntityType effect = model.FindEntityType(typeof(Explore.Domain.CheckoutDispatchEffect))!;
        await Assert.That(effect).IsNotNull();
        await Assert.That(effect.FindDeclaredQueryFilter(Explore.Persistence.QueryFilters.QueryFilterNames.Tenant)).IsNotNull();
        await Assert.That(effect.FindIndex([
            effect.FindProperty(nameof(Explore.Domain.CheckoutDispatchEffect.TenantId))!,
            effect.FindProperty(nameof(Explore.Domain.CheckoutDispatchEffect.PaymentAttemptId))!
        ])?.IsUnique).IsTrue();
        await Assert.That(effect.GetCheckConstraints().Any(constraint => constraint.Name == "ck_checkout_dispatch_effects_state")).IsTrue();
        await Assert.That(effect.GetCheckConstraints().Any(constraint => constraint.Name == "ck_checkout_dispatch_effects_processing_fence")).IsTrue();
        await Assert.That(effect.GetIndexes().Any(index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(Explore.Domain.CheckoutDispatchEffect.Status),
                nameof(Explore.Domain.CheckoutDispatchEffect.NextAttemptAt),
                nameof(Explore.Domain.CheckoutDispatchEffect.CreatedAt)
            ]))).IsTrue();
    }

    [Test]
    [Arguments("PostgreSql")]
    [Arguments("Sqlite")]
    [Arguments("SqlServer")]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public async Task PromotionModelMapsHistoricalOrderSnapshotsAndVerifiedEmail(string provider)
    {
        using var context = CreateContext(provider);
        IModel model = context.GetService<IDesignTimeModel>().Model;

        IEntityType order = model.FindEntityType(typeof(Explore.Domain.RegistrationOrder))!;
        foreach (string property in new[]
                 {
                     nameof(Explore.Domain.RegistrationOrder.PreDiscountOrganizerDirectedTotalMinorSnapshot),
                     nameof(Explore.Domain.RegistrationOrder.PromotionDiscountTotalMinorSnapshot),
                     nameof(Explore.Domain.RegistrationOrder.PostDiscountOrganizerDirectedTotalMinorSnapshot)
                 })
        {
            await Assert.That(order.FindProperty(property)!.GetColumnType()).IsEqualTo("bigint");
        }

        IEntityType line = model.FindEntityType(typeof(Explore.Domain.RegistrationOrderLine))!;
        foreach (string property in new[]
                 {
                     nameof(Explore.Domain.RegistrationOrderLine.PreDiscountLineSubtotalMinorSnapshot),
                     nameof(Explore.Domain.RegistrationOrderLine.PromotionDiscountAmountMinorSnapshot),
                     nameof(Explore.Domain.RegistrationOrderLine.PostDiscountLineSubtotalMinorSnapshot)
                 })
        {
            await Assert.That(line.FindProperty(property)!.GetColumnType()).IsEqualTo("bigint");
        }

        IEntityType pii = model.FindEntityType(typeof(Explore.Domain.RegistrationOrderPii))!;
        await Assert.That(pii.FindProperty(nameof(Explore.Domain.RegistrationOrderPii.IsEmailVerified))!.GetDefaultValue()).IsEqualTo(false);
    }

    [Test]
    [Arguments("src/Explore.Persistence/Migrations/20260815062551_AddEventPromotionCodes.cs")]
    [Arguments("src/Explore.Persistence.Migrations.Sqlite/Migrations/20260815062627_AddEventPromotionCodes.cs")]
    [Arguments("src/Explore.Persistence.Migrations.SqlServer/Migrations/20260815062801_AddEventPromotionCodes.cs")]
    [Arguments("src/Explore.Persistence.Migrations.MariaDb/Migrations/20260815062813_AddEventPromotionCodes.cs")]
    [Arguments("src/Explore.Persistence.Migrations.MySql/Migrations/20260815063136_AddEventPromotionCodes.cs")]
    public async Task GeneratedPromotionMigrationBackfillsHistoricalSnapshotColumns(string migrationPath)
    {
        string source = await File.ReadAllTextAsync(Path.Combine(GetRepositoryRoot(), migrationPath));

        await Assert.That(source).Contains("pre_discount_organizer_directed_total_minor_snapshot");
        await Assert.That(source).Contains("post_discount_organizer_directed_total_minor_snapshot");
        await Assert.That(source).Contains("organizer_directed_total_minor_snapshot");
        await Assert.That(source).Contains("pre_discount_line_subtotal_minor_snapshot");
        await Assert.That(source).Contains("post_discount_line_subtotal_minor_snapshot");
        await Assert.That(source).Contains("line_subtotal_snapshot");
        await Assert.That(source).Contains("migrationBuilder.Sql");
    }

    [Test]
    [Arguments("PostgreSql")]
    [Arguments("Sqlite")]
    [Arguments("SqlServer")]
    [Arguments("MariaDb")]
    [Arguments("MySql")]
    public async Task PromotionLookupsAreGlobalStableIntegerTables(string provider)
    {
        using var context = CreateContext(provider);
        IModel model = context.GetService<IDesignTimeModel>().Model;

        foreach (Type lookupType in new[] { typeof(Explore.Domain.PromotionDefinitionStatus), typeof(Explore.Domain.PromotionReservationStatus) })
        {
            IEntityType lookup = model.FindEntityType(lookupType)!;
            await Assert.That(lookup.FindPrimaryKey()!.Properties.Single().ClrType).IsEqualTo(typeof(int));
            await Assert.That(lookup.FindProperty("Id")!.ValueGenerated).IsEqualTo(ValueGenerated.Never);
            await Assert.That(lookup.FindProperty("MasterCode")!.GetMaxLength()).IsEqualTo(100);
            await Assert.That(lookup.GetIndexes().Any(index => index.IsUnique && index.Properties.Single().Name == "MasterCode")).IsTrue();
            await Assert.That(lookup.FindDeclaredQueryFilter(Explore.Persistence.QueryFilters.QueryFilterNames.Tenant)).IsNull();
            await Assert.That(lookup.GetSeedData().Count).IsEqualTo(0);
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

            await Assert.That(await context.Database.EnsureCreatedAsync()).IsTrue();
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

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
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
