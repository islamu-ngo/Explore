// ABOUTME: Current-baseline and persisted provider-authority tests for the webhook schema.
// ABOUTME: Verifies deterministic schema SQL and that legacy-unverified bindings cannot grant portal capability.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class WebhookMigrationAndPortalPersistenceTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task CurrentBaselineScript_ContainsWebhookFinalStateWithoutRuntimeIo()
    {
        await using var context = CreateRelationalContext();
        string script = context.GetService<IMigrator>().GenerateScript(
            fromMigration: null,
            toMigration: null,
            MigrationsSqlGenerationOptions.Idempotent);

        foreach (string marker in new[]
                 {
                     "webhook_delivery_plan_snapshots",
                     "webhook_local_target_snapshots",
                     "webhook_provider_publications",
                     "webhook_provider_capabilities",
                     "ck_webhook_consumer_provider_bindings_capabilities_known"
                 })
        {
            await Assert.That(script).Contains(marker);
        }

        await Assert.That(script).DoesNotContain("HttpClient");
        await Assert.That(script).DoesNotContain("SendAsync");
        await Assert.That(script).DoesNotContain("api.svix.com");
    }

    [Test]
    public async Task PersistedBinding_GrantsPortalCapabilityOnlyAfterExactOwnershipVerification()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseInMemoryDatabase($"webhook-portal-binding-{Guid.NewGuid():N}")
            .Options;
        await using var context = new ExploreDbContext(options);
        var tenantId = Guid.CreateVersion7();
        var verifiedConsumer = CreateConsumer(tenantId, "Verified");
        var legacyConsumer = CreateConsumer(tenantId, "Legacy");
        var instanceId = Guid.CreateVersion7();
        var profile = CreateCapabilityProfile();
        var verified = WebhookConsumerProviderBinding.CreatePending(
            tenantId,
            verifiedConsumer.Id,
            instanceId,
            "production",
            profile,
            WebhookProviderCapability.AppPortal);
        verified.VerifyOwnership(tenantId, verifiedConsumer.Id, "verified-app", DateTimeOffset.UtcNow);
        var legacy = WebhookConsumerProviderBinding.CreateLegacyUnverified(
            tenantId,
            legacyConsumer.Id,
            instanceId,
            "production",
            "legacy-app",
            profile);

        context.WebhookConsumers.AddRange(verifiedConsumer, legacyConsumer);
        context.WebhookConsumerProviderBindings.AddRange(verified, legacy);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new WebhookConsumerProviderBindingRepository(context);
        var persistedVerified = await repository.GetVerifiedByConsumerAsync(
            tenantId,
            verifiedConsumer.Id,
            WebhookProviderKind.Svix,
            "production",
            CancellationToken.None);
        var persistedLegacy = await repository.GetVerifiedByConsumerAsync(
            tenantId,
            legacyConsumer.Id,
            WebhookProviderKind.Svix,
            "production",
            CancellationToken.None);

        await Assert.That(persistedVerified).IsNotNull();
        await Assert.That(persistedVerified!.CanIssueAppPortalFor(tenantId, verifiedConsumer.Id)).IsTrue();
        await Assert.That(persistedLegacy).IsNull();
        await Assert.That(legacy.CanIssueAppPortalFor(tenantId, legacyConsumer.Id)).IsFalse();
    }

    [Test]
    public async Task CurrentBaseline_UsesNormalizedBindingIdentityAndVerificationColumns()
    {
        await using var context = CreateRelationalContext();
        var binding = context.Model.FindEntityType(typeof(WebhookConsumerProviderBinding))!;

        await Assert.That(binding.FindProperty(nameof(WebhookConsumerProviderBinding.InstanceId))).IsNotNull();
        await Assert.That(binding.FindProperty(nameof(WebhookConsumerProviderBinding.ApplicationUid))).IsNotNull();
        await Assert.That(binding.FindProperty(nameof(WebhookConsumerProviderBinding.VerificationStateId))).IsNotNull();
        var identity = binding.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(
            [
                nameof(WebhookConsumerProviderBinding.ProviderKindId),
                nameof(WebhookConsumerProviderBinding.NormalizedEnvironment),
                nameof(WebhookConsumerProviderBinding.NormalizedApplicationUid)
            ]));
        await Assert.That(identity.IsUnique).IsTrue();
        await Assert.That(identity.GetFilter()).IsNull();
    }

    [Test]
    public async Task CurrentBaseline_RemovesLegacyLinksAndKeepsFinalEvidenceTables()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        string[] tables = await context.Database.SqlQuery<string>(
                $"""
                SELECT table_name AS "Value"
                FROM information_schema.tables
                WHERE table_schema = {context.Model.GetDefaultSchema()}
                  AND table_name IN (
                      'webhook_provider_links',
                      'webhook_provider_publications',
                      'webhook_provider_publication_attempts',
                      'webhook_delivery_plan_snapshots',
                      'webhook_local_target_snapshots')
                """)
            .ToArrayAsync();

        await Assert.That(tables).DoesNotContain("webhook_provider_links");
        await Assert.That(tables).Contains("webhook_provider_publications");
        await Assert.That(tables).Contains("webhook_provider_publication_attempts");
        await Assert.That(tables).Contains("webhook_delivery_plan_snapshots");
        await Assert.That(tables).Contains("webhook_local_target_snapshots");
    }

    private static ExploreDbContext CreateRelationalContext()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql("Host=localhost;Database=webhook_model;Username=unused;Password=unused")
            .UseSnakeCaseNamingConvention()
            .Options;
        return new ExploreDbContext(options);
    }

    private static WebhookConsumer CreateConsumer(Guid tenantId, string name) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = tenantId,
        ConsumerKind = WebhookConsumerKind.Tenant,
        Name = name,
        Status = WebhookConsumerStatus.Active,
        ProviderMode = WebhookProviderMode.Svix
    };

    private static WebhookProviderCapabilityProfile CreateCapabilityProfile() =>
        WebhookProviderCapabilityProfile.Create(
            WebhookProviderKind.Svix,
            "1.84.0",
            WebhookProviderCapability.AppPortal,
            "svix-1.84.0-v1",
            DateTimeOffset.UtcNow);
}
