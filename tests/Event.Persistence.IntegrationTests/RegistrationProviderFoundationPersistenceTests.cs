// ABOUTME: Verifies Phase 9 provider-neutral EF metadata, lookup seeding, filters, and credential references.
// ABOUTME: Includes the manual-QA persistence driver for qualified SecretBinding credentials and connections.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Secrets;
using Explore.Domain.ValueObjects;
using Explore.Persistence;
using Explore.Persistence.Repositories;
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

    [Test]
    public async Task ProviderManagementQueue_IsolatesRetainedEffectsByBindingAndEvent()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventA = Guid.CreateVersion7();
        Guid eventB = Guid.CreateVersion7();
        await using ExploreDbContext context = CreateInMemoryContext($"provider-management-queue-{Guid.NewGuid():N}");
        RegistrationProviderConnection connection = Connection(tenantId, "a", false);
        RegistrationForm formA = RegistrationForm.Create(tenantId, eventA, "registration", "a", "A", Now);
        RegistrationForm formB = RegistrationForm.Create(tenantId, eventB, "registration", "b", "B", Now);
        RegistrationProviderBinding bindingA = Binding(tenantId, connection.Id, formA.Id);
        RegistrationProviderBinding bindingB = Binding(tenantId, connection.Id, formB.Id);
        context.AddRange(connection, formA, formB, bindingA, bindingB);
        AddRetainedEffect(context, tenantId, bindingA.Id, "a", Now.AddMinutes(-30));
        AddRetainedEffect(context, tenantId, bindingB.Id, "b", Now.AddMinutes(-5));
        await context.SaveChangesAsync();
        context.TenantContext = new TestTenantContext(tenantId);
        RegistrationProviderRepository repository = new(context);

        IReadOnlyList<RegistrationProviderParkedItem> rows = await repository.GetParkedItemsForEventAsync(tenantId, eventA, 10, CancellationToken.None);

        await Assert.That(rows).HasSingleItem();
        await Assert.That(rows[0].Effect!.BindingId).IsEqualTo(bindingA.Id);
        await Assert.That(rows[0].Effect!.EventId).IsEqualTo(eventA);
        await Assert.That(await repository.GetLastCallbackAtAsync(tenantId, bindingA.Id, CancellationToken.None)).IsEqualTo(Now.AddMinutes(-30));
        await Assert.That(await repository.GetOldestPendingItemAtAsync(tenantId, bindingA.Id, CancellationToken.None)).IsEqualTo(Now.AddMinutes(-30));
    }

    [Test]
    public async Task ProviderManagementQueue_ExcludesResolvedEffectsAndSubmissions()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        await using ExploreDbContext context = CreateInMemoryContext($"provider-management-resolved-{Guid.NewGuid():N}");
        RegistrationProviderConnection connection = Connection(tenantId, "a", false);
        RegistrationForm form = RegistrationForm.Create(tenantId, eventId, "registration", "a", "A", Now);
        RegistrationProviderBinding binding = Binding(tenantId, connection.Id, form.Id);
        context.AddRange(connection, form, binding);
        IncomingWebhookEffectOutbox effect = AddRetainedEffect(context, tenantId, binding.Id, "resolved", Now.AddMinutes(-30));
        RegistrationSubmission submission = ProviderSubmission(tenantId, eventId, binding.Id);
        context.AddRange(
            submission,
            RegistrationSubmissionIssue.Create(submission, "BLOCKING_DRIFT", Now));
        await context.SaveChangesAsync();
        context.TenantContext = new TestTenantContext(tenantId);
        RegistrationProviderRepository repository = new(context);

        await Assert.That((await repository.GetParkedItemsForEventAsync(tenantId, eventId, 10, CancellationToken.None)).Count).IsEqualTo(2);

        effect.AcknowledgeResolution("organizer_accepted", Now.AddMinutes(1));
        await repository.AddSubmissionIssueAsync(RegistrationSubmissionIssue.Create(submission, "RESOLVED_ACCEPTED", Now.AddMinutes(1)), CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);

        IReadOnlyList<RegistrationProviderParkedItem> rows = await repository.GetParkedItemsForEventAsync(tenantId, eventId, 10, CancellationToken.None);

        await Assert.That(rows).IsEmpty();
        await Assert.That(await repository.CountParkedItemsAsync(tenantId, binding.Id, CancellationToken.None)).IsEqualTo(0);
        await Assert.That(await repository.GetOldestPendingItemAtAsync(tenantId, binding.Id, CancellationToken.None)).IsNull();
        await Assert.That(await context.RegistrationSubmissionIssues.AnyAsync(issue => issue.RegistrationSubmissionId == submission.Id && issue.Code == "BLOCKING_DRIFT")).IsTrue();
    }

    [Test]
    public async Task ApprovedOrigins_ReaddingSameOriginRevivesSoftDeletedRow()
    {
        Guid tenantId = Guid.CreateVersion7();
        RegistrationProviderConnection connection = Connection(tenantId, "origins", false);

        connection.ReplaceApprovedOrigins(["https://forms.example.org"], Now);
        connection.ReplaceApprovedOrigins([], Now.AddMinutes(1));
        connection.ReplaceApprovedOrigins(["https://forms.example.org"], Now.AddMinutes(2));

        await Assert.That(connection.ApprovedOrigins).HasSingleItem();
        await Assert.That(connection.ApprovedOrigins.Single().IsDeleted).IsFalse();
        await Assert.That(connection.IsOriginApproved(new Uri("https://forms.example.org/launch"))).IsTrue();
    }

    [Test]
    public async Task ApprovedOrigins_ReaddingPersistedSoftDeletedOriginRevivesExistingRow()
    {
        string databaseName = $"provider-origin-revive-{Guid.NewGuid():N}";
        Guid tenantId = Guid.CreateVersion7();
        Guid connectionId;

        await using (ExploreDbContext seed = CreateInMemoryContext(databaseName))
        {
            RegistrationProviderConnection connection = Connection(tenantId, "origins", false);
            connectionId = connection.Id;
            connection.ReplaceApprovedOrigins(["https://forms.example.org"], Now);
            seed.RegistrationProviderConnections.Add(connection);
            await seed.SaveChangesAsync();
        }

        await using (ExploreDbContext removeContext = CreateInMemoryContext(databaseName))
        {
            removeContext.TenantContext = new TestTenantContext(tenantId);
            RegistrationProviderRepository repository = new(removeContext);
            RegistrationProviderConnection connection = (await repository.GetConnectionAsync(tenantId, connectionId, CancellationToken.None))!;
            connection.ReplaceApprovedOrigins([], Now.AddMinutes(1));
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using (ExploreDbContext readdContext = CreateInMemoryContext(databaseName))
        {
            readdContext.TenantContext = new TestTenantContext(tenantId);
            RegistrationProviderRepository repository = new(readdContext);
            RegistrationProviderConnection connection = (await repository.GetConnectionAsync(tenantId, connectionId, CancellationToken.None))!;
            await Assert.That(connection.ApprovedOrigins).HasSingleItem();
            await Assert.That(connection.ApprovedOrigins.Single().IsDeleted).IsTrue();

            connection.ReplaceApprovedOrigins(["https://forms.example.org"], Now.AddMinutes(2));
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        await using ExploreDbContext verifyContext = CreateInMemoryContext(databaseName);
        verifyContext.TenantContext = new TestTenantContext(tenantId);
        RegistrationProviderRepository verifyRepository = new(verifyContext);
        RegistrationProviderConnection reloaded = (await verifyRepository.GetConnectionAsync(tenantId, connectionId, CancellationToken.None))!;

        await Assert.That(reloaded.ApprovedOrigins).HasSingleItem();
        await Assert.That(reloaded.ApprovedOrigins.Single().IsDeleted).IsFalse();
        await Assert.That(reloaded.IsOriginApproved(new Uri("https://forms.example.org/launch"))).IsTrue();
    }

    private static RegistrationProviderConnection Connection(Guid tenantId, string name, bool deleted)
    {
        RegistrationProviderConnection connection = RegistrationProviderConnection.Create(tenantId, name, RegistrationProviderKindEnum.ExternalForm, RegistrationProviderDeploymentKindEnum.HostedSaas, null, null, Now);
        connection.IsDeleted = deleted;
        return connection;
    }

    private static RegistrationProviderBinding Binding(Guid tenantId, Guid connectionId, Guid? formId = null) => RegistrationProviderBinding.Create(
        tenantId, connectionId, formId ?? Guid.CreateVersion7(), Guid.CreateVersion7(), RegistrationProviderPresentationModeEnum.Redirect,
        RegistrationProviderCollectionModeEnum.ProviderHosted, RegistrationProviderCompletionModeEnum.Callback,
        RegistrationProviderTrustLevelEnum.SelectedFields, Now);

    private static IncomingWebhookEffectOutbox AddRetainedEffect(ExploreDbContext context, Guid tenantId, Guid bindingId, string suffix, DateTime createdAt)
    {
        byte[] payload = [1];
        string hash = "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(payload));
        string providerDecisionId = $"{bindingId:N}:{suffix}";
        IncomingWebhookMessage message = IncomingWebhookMessage.CreateVerified(
            tenantId,
            "registration-provider",
            providerDecisionId,
            providerDecisionId,
            "registration.provider_submission",
            payload,
            hash,
            "application/json",
            "utf-8",
            "{}",
            createdAt,
            createdAt,
            createdAt.AddDays(1),
            "test",
            createdAt.AddDays(1),
            createdAt.AddDays(1),
            createdAt.AddDays(1),
            createdAt.AddDays(1));
        IncomingWebhookEffectOutbox effect = IncomingWebhookEffectOutbox.CreatePending(
            tenantId,
            message.Id,
            "registration-provider",
            providerDecisionId,
            "registration.provider_submission",
            hash,
            createdAt);
        context.AddRange(message, effect);
        return effect;
    }

    private static RegistrationSubmission ProviderSubmission(Guid tenantId, Guid eventId, Guid bindingId)
    {
        RegistrationAttempt attempt = RegistrationAttempt.Create(
            Guid.CreateVersion7(), tenantId, eventId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), CapabilityTokenHash.Create(Convert.ToBase64String(new byte[32])),
            bindingId, RegistrationEvidenceHash.Create(Convert.ToBase64String(Enumerable.Repeat((byte)1, 32).ToArray())), Now, Now.AddHours(1));
        return RegistrationSubmission.CreateProviderEvidenceOnly(
            attempt,
            RegistrationEvidenceHash.Create(Convert.ToBase64String(Enumerable.Repeat((byte)2, 32).ToArray())),
            Now.AddMinutes(1),
            null,
            "provider-submission-1",
            "revision-1",
            null,
            null);
    }

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
