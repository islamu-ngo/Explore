// ABOUTME: PostgreSQL integration tests for atomic outgoing webhook delivery-plan materialization.
// ABOUTME: Verifies complete commit, rollback, idempotent replay, and changed-payload conflict behavior.

using System.Text;
using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Exceptions;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class WebhookMaterializationAtomicityTests(PostgreSqlContainerFixture fixture)
{
    private static readonly DateTime MaterializedAt =
        new(2026, 7, 14, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task MaterializeAsync_NewPlan_CommitsMessagePlanLocalTargetAndPublicationTogether()
    {
        var scenario = await SeedAuthorityAsync();
        await using var context = fixture.CreateDbContext();
        var materializer = new WebhookDeliveryPlanMaterializer(context, new EfCoreUnitOfWork(context));

        var result = await materializer.MaterializeAsync(
            CreateMaterialization(scenario),
            CancellationToken.None);

        await Assert.That(result.Created).IsTrue();
        await Assert.That(result.Message.Id).IsEqualTo(scenario.MessageId);
        await Assert.That(result.DeliveryPlan.WebhookMessageId).IsEqualTo(scenario.MessageId);
        await Assert.That(await context.WebhookMessages.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.WebhookDeliveryPlanSnapshots.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.WebhookLocalTargetSnapshots.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.WebhookProviderPublications.CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task MaterializeAsync_WhenAnyTargetViolatesUniqueness_RollsBackEveryRow()
    {
        var scenario = await SeedAuthorityAsync();
        await using var context = fixture.CreateDbContext();
        var materializer = new WebhookDeliveryPlanMaterializer(context, new EfCoreUnitOfWork(context));

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
            await materializer.MaterializeAsync(
                CreateMaterialization(scenario, duplicateProviderPublication: true),
                CancellationToken.None));

        await Assert.That(await context.WebhookMessages.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.WebhookDeliveryPlanSnapshots.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.WebhookLocalTargetSnapshots.CountAsync()).IsEqualTo(0);
        await Assert.That(await context.WebhookProviderPublications.CountAsync()).IsEqualTo(0);
    }

    [Test]
    public async Task MaterializeAsync_ReplayedSameIdentityAndHash_ReturnsFrozenExistingPlan()
    {
        var scenario = await SeedAuthorityAsync();
        await using var context = fixture.CreateDbContext();
        var materializer = new WebhookDeliveryPlanMaterializer(context, new EfCoreUnitOfWork(context));

        var first = await materializer.MaterializeAsync(
            CreateMaterialization(scenario),
            CancellationToken.None);
        var replay = await materializer.MaterializeAsync(
            CreateMaterialization(scenario),
            CancellationToken.None);

        await Assert.That(first.Created).IsTrue();
        await Assert.That(replay.Created).IsFalse();
        await Assert.That(replay.Message.Id).IsEqualTo(first.Message.Id);
        await Assert.That(replay.DeliveryPlan.Id).IsEqualTo(first.DeliveryPlan.Id);
        await Assert.That(await context.WebhookMessages.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.WebhookDeliveryPlanSnapshots.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.WebhookLocalTargetSnapshots.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.WebhookProviderPublications.CountAsync()).IsEqualTo(1);
    }

    [Test]
    public async Task MaterializeAsync_ReplayedIdentityWithChangedBytes_ConflictsWithoutMutation()
    {
        var scenario = await SeedAuthorityAsync();
        await using var context = fixture.CreateDbContext();
        var materializer = new WebhookDeliveryPlanMaterializer(context, new EfCoreUnitOfWork(context));
        await materializer.MaterializeAsync(CreateMaterialization(scenario), CancellationToken.None);

        await Assert.ThrowsAsync<WebhookMaterializationConflictException>(async () =>
            await materializer.MaterializeAsync(
                CreateMaterialization(scenario, payloadSuffix: "-changed"),
                CancellationToken.None));

        var storedMessage = await context.WebhookMessages.AsNoTracking().SingleAsync();
        await Assert.That(Encoding.UTF8.GetString(storedMessage.GetPayloadBytes()!))
            .IsEqualTo("{\"event\":\"materialized\"}");
        await Assert.That(await context.WebhookMessages.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.WebhookDeliveryPlanSnapshots.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.WebhookLocalTargetSnapshots.CountAsync()).IsEqualTo(1);
        await Assert.That(await context.WebhookProviderPublications.CountAsync()).IsEqualTo(1);
    }

    private async Task<SeededAuthority> SeedAuthorityAsync()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = CreateTenant();
        var consumer = CreateConsumer(tenant.Id);
        var endpoint = CreateEndpoint(tenant.Id, consumer.Id);
        var binding = CreateBinding(tenant.Id, consumer.Id);
        context.Tenants.Add(tenant);
        context.WebhookConsumers.Add(consumer);
        context.WebhookEndpoints.Add(endpoint);
        context.WebhookConsumerProviderBindings.Add(binding);
        await context.SaveChangesAsync();

        return new SeededAuthority(
            tenant.Id,
            consumer.Id,
            endpoint,
            binding.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            $"domain-event-{Guid.NewGuid():N}");
    }

    private static WebhookDeliveryMaterialization CreateMaterialization(
        SeededAuthority authority,
        string payloadSuffix = "",
        bool duplicateProviderPublication = false)
    {
        var message = WebhookMessage.Create(
            authority.MessageId,
            authority.TenantId,
            "event.materialized",
            authority.EventId,
            "event",
            authority.AggregateId,
            authority.ConsumerId,
            Encoding.UTF8.GetBytes($"{{\"event\":\"materialized{payloadSuffix}\"}}"),
            "application/json",
            "utf-8",
            MaterializedAt.AddMinutes(-1),
            MaterializedAt.AddDays(7),
            MaterializedAt);
        var plan = WebhookDeliveryPlanSnapshot.Create(
            authority.TenantId,
            message.Id,
            authority.ConsumerId,
            WebhookProviderMode.Composite,
            "configuration-v1",
            "contract-v1",
            "default",
            "retention-v1",
            new DateTimeOffset(message.PayloadRetentionUntil),
            new DateTimeOffset(MaterializedAt));
        var localTarget = WebhookLocalTargetSnapshot.Create(
            plan,
            authority.Endpoint,
            endpointConfigurationVersion: 4,
            new DateTimeOffset(MaterializedAt.AddDays(-1)),
            new DateTimeOffset(MaterializedAt.AddDays(30)),
            new DateTimeOffset(MaterializedAt));
        var publication = CreatePublication(authority, message, plan, "primary");
        var publications = duplicateProviderPublication
            ? new[] { publication, CreatePublication(authority, message, plan, "duplicate") }
            : [publication];

        return new WebhookDeliveryMaterialization(
            message,
            plan,
            [localTarget],
            publications);
    }

    private static WebhookProviderPublication CreatePublication(
        SeededAuthority authority,
        WebhookMessage message,
        WebhookDeliveryPlanSnapshot plan,
        string identitySuffix) =>
        WebhookProviderPublication.Create(
            authority.TenantId,
            message.Id,
            plan.Id,
            WebhookProviderKind.Svix,
            authority.BindingId,
            "1.84.0",
            $"{message.Id:D}-{identitySuffix}",
            $"{message.Id:N}:{authority.BindingId:N}:{identitySuffix}",
            message.PayloadHash,
            "consumer-application-uid",
            "provider-application-id",
            "managed-eu",
            "secret:webhook-provider",
            "credential-v1",
            WebhookProviderMode.Composite,
            "provider-config-v1",
            eventContractVersion: 1,
            "retention-v1",
            message.PayloadRetentionUntil,
            MaterializedAt.AddDays(30),
            MaterializedAt.AddHours(12),
            MaterializedAt);

    private static WebhookEndpoint CreateEndpoint(Guid tenantId, Guid consumerId) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = tenantId,
        ConsumerId = consumerId,
        Url = "https://webhook.example.test/events",
        Status = WebhookEndpointStatus.Active,
        SecretRef = "secret:webhook-endpoint",
        SecretVersion = 3,
        MaxAttempts = 8,
        TimeoutSeconds = 15,
        RateLimitPerMinute = 120,
        CreatedAt = MaterializedAt.AddDays(-1)
    };

    private static WebhookConsumerProviderBinding CreateBinding(Guid tenantId, Guid consumerId)
    {
        var profile = WebhookProviderCapabilityProfile.Create(
            WebhookProviderKind.Svix,
            "1.84.0",
            WebhookProviderCapability.EndpointManagement,
            "svix-1.84.0-v1",
            new DateTimeOffset(MaterializedAt));
        return WebhookConsumerProviderBinding.CreatePending(
            tenantId,
            consumerId,
            Guid.CreateVersion7(),
            "managed-eu",
            profile,
            WebhookProviderCapability.EndpointManagement);
    }

    private static WebhookConsumer CreateConsumer(Guid tenantId) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = tenantId,
        ConsumerKind = WebhookConsumerKind.Tenant,
        Name = $"Materialization Consumer {Guid.NewGuid():N}",
        Status = WebhookConsumerStatus.Active,
        ProviderMode = WebhookProviderMode.Composite,
        CreatedAt = MaterializedAt.AddDays(-1)
    };

    private static Tenant CreateTenant() => new()
    {
        Id = Guid.CreateVersion7(),
        FullName = "Webhook Materialization Test Tenant",
        Slug = $"webhook-materialization-{Guid.NewGuid():N}",
        TenantStatusId = (int)TenantStatusEnum.Active,
        TenantStatus = null!,
        CreatedAt = MaterializedAt.AddDays(-1)
    };

    private sealed record SeededAuthority(
        Guid TenantId,
        Guid ConsumerId,
        WebhookEndpoint Endpoint,
        Guid BindingId,
        Guid MessageId,
        Guid AggregateId,
        string EventId);
}
