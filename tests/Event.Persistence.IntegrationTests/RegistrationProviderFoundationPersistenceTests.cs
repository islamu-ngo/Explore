// ABOUTME: Verifies Phase 9 provider-neutral EF metadata, lookup seeding, filters, and credential references.
// ABOUTME: Includes the manual-QA persistence driver for qualified SecretBinding credentials and connections.

using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using Explore.Persistence;
using Explore.Persistence.QueryFilters;
using Explore.Persistence.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Event.Persistence.IntegrationTests;

public sealed class RegistrationProviderFoundationPersistenceTests
{
    private static readonly DateTime Now = new(2026, 8, 9, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task EfModelMapsProviderFoundationWithoutCredentialValueColumns()
    {
        await using ExploreDbContext context = CreateModelContext();
        IModel model = context.GetService<IDesignTimeModel>().Model;

        Type[] lookups = [typeof(RegistrationProviderKind), typeof(RegistrationProviderDeploymentKind), typeof(RegistrationProviderSchemaAuthority), typeof(RegistrationProviderPresentationMode), typeof(RegistrationProviderCollectionMode), typeof(RegistrationProviderCompletionMode), typeof(RegistrationProviderTrustLevel), typeof(RegistrationProviderDriftClass), typeof(RegistrationProviderBindingState)];
        foreach (Type lookupType in lookups)
        {
            IEntityType lookup = model.FindEntityType(lookupType)!;
            await Assert.That(lookup.FindPrimaryKey()!.Properties.Single().ClrType).IsEqualTo(typeof(int));
            await Assert.That(lookup.GetSeedData()).IsEmpty();
        }

        IEntityType connection = model.FindEntityType(typeof(RegistrationProviderConnection))!;
        await Assert.That(connection.FindDeclaredQueryFilter(QueryFilterNames.Tenant)).IsNotNull();
        await Assert.That(connection.FindDeclaredQueryFilter(QueryFilterNames.SoftDelete)).IsNotNull();
        await Assert.That(connection.GetProperties().Select(property => property.Name)).DoesNotContain("ApiToken");
        await Assert.That(connection.GetProperties().Select(property => property.Name)).DoesNotContain("WebhookSecret");
        await Assert.That(connection.FindProperty(nameof(RegistrationProviderConnection.ApiTokenSecretBindingId))).IsNotNull();
        await Assert.That(connection.FindProperty(nameof(RegistrationProviderConnection.WebhookSecretBindingId))).IsNotNull();

        IEntityType secret = model.FindEntityType(typeof(SecretBinding))!;
        await Assert.That(secret.FindProperty(nameof(SecretBinding.Qualifier))!.GetMaxLength()).IsEqualTo(128);
        await Assert.That(secret.GetIndexes().Any(index => index.IsUnique && HasProperties(index, nameof(SecretBinding.SettingKey), nameof(SecretBinding.ScopeId), nameof(SecretBinding.Qualifier)))).IsTrue();
    }

    [Test]
    public async Task RuntimeSeederRepairsRegistrationProviderLookups()
    {
        await using ExploreDbContext context = CreateInMemoryContext($"provider-seed-{Guid.NewGuid():N}");
        await LookupTableSeeder.SeedRegistrationProviderLookupsAsync(context, default);
        context.RegistrationProviderKinds.Remove(await context.RegistrationProviderKinds.SingleAsync(row => row.Id == (int)RegistrationProviderKindEnum.ExternalForm));
        await context.SaveChangesAsync();

        await LookupTableSeeder.SeedRegistrationProviderLookupsAsync(context, default);
        await LookupTableSeeder.SeedRegistrationProviderLookupsAsync(context, default);

        await Assert.That(await context.RegistrationProviderKinds.CountAsync()).IsEqualTo(3);
        await Assert.That(await context.RegistrationProviderDriftClasses.CountAsync()).IsEqualTo(8);
        await Assert.That(await context.RegistrationProviderBindingStates.CountAsync()).IsEqualTo(4);
    }

    [Test]
    public async Task NamedFiltersHideDeletedAndCrossTenantProviderRows()
    {
        string databaseName = $"provider-filters-{Guid.NewGuid():N}";
        Guid tenantA = Guid.CreateVersion7();
        Guid tenantB = Guid.CreateVersion7();
        await using (ExploreDbContext seed = CreateInMemoryContext(databaseName))
        {
            seed.RegistrationProviderConnections.AddRange(Connection(tenantA, "a", false), Connection(tenantA, "deleted", true), Connection(tenantB, "b", false));
            await seed.SaveChangesAsync();
        }

        await using ExploreDbContext tenant = CreateInMemoryContext(databaseName);
        tenant.TenantContext = new TestTenantContext(tenantA);
        await Assert.That(await tenant.RegistrationProviderConnections.CountAsync()).IsEqualTo(1);
        await Assert.That(await tenant.RegistrationProviderConnections.IgnoreQueryFilters([QueryFilterNames.SoftDelete]).CountAsync()).IsEqualTo(2);
    }

    [Test]
    public async Task ManualQa_DistinctQualifiedProviderCredentialsPersistWithoutConnectionSecretValues()
    {
        Guid tenantId = Guid.CreateVersion7();
        await using ExploreDbContext context = CreateInMemoryContext($"provider-manual-qa-{Guid.NewGuid():N}");
        SecretBinding first = SecretBinding.CreateEnvironmentVariable(SecretDefinitionRegistry.Keys.RegistrationProviders.ApiToken, SecretScope.Tenant, tenantId, "TOKEN_A", qualifier: "connection-a");
        SecretBinding second = SecretBinding.CreateEnvironmentVariable(SecretDefinitionRegistry.Keys.RegistrationProviders.ApiToken, SecretScope.Tenant, tenantId, "TOKEN_B", qualifier: "connection-b");
        context.SecretBindings.AddRange(first, second);
        await context.SaveChangesAsync();
        context.RegistrationProviderConnections.AddRange(
            RegistrationProviderConnection.Create(tenantId, "Connection A", RegistrationProviderKindEnum.ExternalForm, RegistrationProviderDeploymentKindEnum.HostedSaas, first.Id, null, Now),
            RegistrationProviderConnection.Create(tenantId, "Connection B", RegistrationProviderKindEnum.ExternalForm, RegistrationProviderDeploymentKindEnum.HostedSaas, second.Id, null, Now));
        await context.SaveChangesAsync();
        context.TenantContext = new TestTenantContext(tenantId);

        await Assert.That(await context.SecretBindings.CountAsync(binding => binding.SettingKey == SecretDefinitionRegistry.Keys.RegistrationProviders.ApiToken)).IsEqualTo(2);
        await Assert.That(await context.RegistrationProviderConnections.CountAsync()).IsEqualTo(2);
        await Assert.That(context.Model.FindEntityType(typeof(RegistrationProviderConnection))!.GetProperties().Any(property => property.Name.Contains("Token", StringComparison.Ordinal) && property.ClrType == typeof(string))).IsFalse();
    }

    [Test]
    public async Task SecretPurposeMismatchFailsBeforePersistence()
    {
        Guid tenantId = Guid.CreateVersion7();
        await using ExploreDbContext context = CreateInMemoryContext($"provider-secret-purpose-{Guid.NewGuid():N}");
        SecretBinding smtpBinding = SecretBinding.CreateEnvironmentVariable(
            SecretDefinitionRegistry.Keys.Smtp.Password, SecretScope.Tenant, tenantId, "SMTP_PASSWORD", qualifier: "connection-a");
        context.SecretBindings.Add(smtpBinding);
        await context.SaveChangesAsync();
        context.RegistrationProviderConnections.Add(RegistrationProviderConnection.Create(
            tenantId, "Connection A", RegistrationProviderKindEnum.ExternalForm,
            RegistrationProviderDeploymentKindEnum.HostedSaas, smtpBinding.Id, null, Now));

        await Assert.That(() => context.SaveChangesAsync()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ChildQueriesHideRowsWhenParentBindingOrConnectionIsSoftDeleted()
    {
        Guid tenantId = Guid.CreateVersion7();
        await using ExploreDbContext context = CreateInMemoryContext($"provider-child-filters-{Guid.NewGuid():N}");
        RegistrationProviderConnection connection = Connection(tenantId, "a", false);
        RegistrationProviderBinding binding = Binding(tenantId, connection.Id);
        RegistrationProviderFieldMapping field = RegistrationProviderFieldMapping.Create(binding, "attendee.email", "email", true);
        binding.AddFieldMapping(field);
        binding.AddOptionMapping(RegistrationProviderOptionMapping.Create(binding, field, "yes", "1"));
        binding.AddCapability(RegistrationProviderCapability.Create(binding, "unknown", "hosted", "v1", "policy", "evidence", "callback"));
        RegistrationProviderSchemaRevision revision = RegistrationProviderSchemaRevision.Create(tenantId, connection.Id, RegistrationProviderSchemaAuthorityEnum.ProviderDiscovered, Hash(), Now);
        context.AddRange(connection, binding, revision);
        await context.SaveChangesAsync();
        context.TenantContext = new TestTenantContext(tenantId);
        await Assert.That(await context.RegistrationProviderFieldMappings.CountAsync()).IsEqualTo(1);

        binding.IsDeleted = true;
        connection.IsDeleted = true;
        await context.SaveChangesAsync();

        await Assert.That(await context.RegistrationProviderCapabilities.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.RegistrationProviderFieldMappings.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.RegistrationProviderOptionMappings.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.RegistrationProviderSchemaRevisions.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task CrossTenantBindingConnectionCompositeForeignKeyFails()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"provider-cross-tenant-{Guid.CreateVersion7():N}.db");
        try
        {
            await using ExploreDbContext context = CreateSqliteContext(databasePath);
            await context.Database.EnsureCreatedAsync();
            await LookupTableSeeder.SeedAsync(context, CancellationToken.None);
            Guid tenantA = Guid.CreateVersion7();
            Guid tenantB = Guid.CreateVersion7();
            context.Tenants.AddRange(Tenant(tenantA, "a"), Tenant(tenantB, "b"));
            RegistrationProviderConnection connection = Connection(tenantA, "a", false);
            context.RegistrationProviderConnections.Add(connection);
            await context.SaveChangesAsync();

            context.RegistrationProviderBindings.Add(Binding(tenantB, connection.Id));

            await Assert.That(() => context.SaveChangesAsync()).Throws<DbUpdateException>();
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }
    }

    [Test]
    public async Task AttemptProviderRevisionForeignKeyTargetsPublishedBindingRevision()
    {
        await using ExploreDbContext context = CreateModelContext();
        IForeignKey? foreignKey = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(RegistrationAttempt))!
            .GetForeignKeys().SingleOrDefault(candidate => candidate.PrincipalEntityType.ClrType == typeof(RegistrationProviderBinding) &&
                candidate.Properties.Select(property => property.Name).SequenceEqual([nameof(RegistrationAttempt.TenantId), nameof(RegistrationAttempt.RegistrationProviderBindingId), "ProviderMappingRevisionHashKey"]));

        await Assert.That(foreignKey).IsNotNull();
        await Assert.That(foreignKey!.PrincipalKey.Properties.Select(property => property.Name)).IsEquivalentTo([
            nameof(RegistrationProviderBinding.TenantId), nameof(RegistrationProviderBinding.Id), nameof(RegistrationProviderBinding.PublishedMappingRevisionHashKey)]);
    }

    private static RegistrationProviderConnection Connection(Guid tenantId, string name, bool deleted)
    {
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(tenantId, name, RegistrationProviderKindEnum.ExternalForm, RegistrationProviderDeploymentKindEnum.HostedSaas, null, null, Now);
        connection.IsDeleted = deleted;
        return connection;
    }

    private static RegistrationProviderBinding Binding(Guid tenantId, Guid connectionId) => RegistrationProviderBinding.Create(
        tenantId, connectionId, Guid.CreateVersion7(), Guid.CreateVersion7(), RegistrationProviderPresentationModeEnum.Redirect,
        RegistrationProviderCollectionModeEnum.ProviderHosted, RegistrationProviderCompletionModeEnum.Callback,
        RegistrationProviderTrustLevelEnum.SelectedFields, Now);

    private static Tenant Tenant(Guid id, string slug) => new()
    {
        Id = id,
        FullName = slug,
        Slug = $"provider-{slug}-{Guid.CreateVersion7():N}",
        TenantStatusId = (int)TenantStatusEnum.Active,
        TenantStatus = null!,
    };

    private static RegistrationEvidenceHash Hash() => RegistrationEvidenceHash.Create(Convert.ToBase64String(new byte[32]));

    private static bool HasProperties(IReadOnlyIndex index, params string[] propertyNames) =>
        index.Properties.Select(property => property.Name).SequenceEqual(propertyNames);

    private static ExploreDbContext CreateModelContext() => new(new DbContextOptionsBuilder<ExploreDbContext>()
        .UseNpgsql("Host=localhost;Database=provider_model;Username=unused;Password=unused")
        .UseSnakeCaseNamingConvention().Options);

    private static ExploreDbContext CreateInMemoryContext(string databaseName) => new(new DbContextOptionsBuilder<ExploreDbContext>()
        .UseInMemoryDatabase(databaseName).Options);

    private static ExploreDbContext CreateSqliteContext(string databasePath) => new(new DbContextOptionsBuilder<ExploreDbContext>()
        .UseSqlite($"Data Source={databasePath}")
        .UseSnakeCaseNamingConvention().Options);

    private sealed class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
        public string? TenantSlug => null;
        public bool IsResolved => true;
    }
}
