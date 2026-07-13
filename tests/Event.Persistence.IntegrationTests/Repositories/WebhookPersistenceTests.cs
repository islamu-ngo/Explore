// ABOUTME: PostgreSQL persistence tests for webhook canonical tables and repository behavior.
// ABOUTME: Verifies tenant isolation, endpoint subscription filtering, idempotency, and payload cleanup.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence;
using Explore.Persistence.QueryFilters;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class WebhookPersistenceTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task WebhookTenantFilters_WhenNoAmbientTenant_FailClosed()
    {
        await fixture.ResetAsync();
        await using var setupContext = fixture.CreateDbContext();
        var tenantA = CreateTenant("webhook-filter-a");
        var tenantB = CreateTenant("webhook-filter-b");
        setupContext.Tenants.AddRange(tenantA, tenantB);
        setupContext.WebhookConsumers.AddRange(
            CreateConsumer(tenantA.Id, "Tenant A Consumer", WebhookProviderMode.Local),
            CreateConsumer(tenantB.Id, "Tenant B Consumer", WebhookProviderMode.Local));
        await setupContext.SaveChangesAsync();

        await using var noTenantContext = fixture.CreateTenantFilteredDbContext();
        await using var tenantAContext = fixture.CreateTenantFilteredDbContext(new StaticTenantContext(tenantA.Id));

        await Assert.That(await noTenantContext.WebhookConsumers.CountAsync()).IsEqualTo(0);
        await Assert.That(await tenantAContext.WebhookConsumers.CountAsync()).IsEqualTo(1);
        await Assert.That(await tenantAContext.WebhookConsumers.Select(e => e.TenantId).SingleAsync()).IsEqualTo(tenantA.Id);
    }

    [Test]
    public async Task EndpointRepository_ReturnsOnlyActiveSubscribedEndpointsForTenantAndProvider()
    {
        await fixture.ResetAsync();
        await using var setupContext = fixture.CreateDbContext();
        var tenant = CreateTenant("webhook-endpoint-resolution");
        setupContext.Tenants.Add(tenant);

        var publishedType = CreateEventType("event.published");
        var updatedType = CreateEventType("event.updated");
        setupContext.WebhookEventTypes.AddRange(publishedType, updatedType);

        var localConsumer = CreateConsumer(tenant.Id, "Local Consumer", WebhookProviderMode.Local);
        var compositeConsumer = CreateConsumer(tenant.Id, "Composite Consumer", WebhookProviderMode.Composite);
        var svixConsumer = CreateConsumer(tenant.Id, "Svix Consumer", WebhookProviderMode.Svix);
        var disabledConsumer = CreateConsumer(tenant.Id, "Disabled Consumer", WebhookProviderMode.Local);
        setupContext.WebhookConsumers.AddRange(localConsumer, compositeConsumer, svixConsumer, disabledConsumer);
        await setupContext.SaveChangesAsync();

        var localEndpoint = CreateEndpoint(tenant.Id, localConsumer.Id, "local", WebhookEndpointStatus.Active);
        var compositeEndpoint = CreateEndpoint(tenant.Id, compositeConsumer.Id, "composite", WebhookEndpointStatus.Active);
        var svixEndpoint = CreateEndpoint(tenant.Id, svixConsumer.Id, "svix", WebhookEndpointStatus.Active);
        var disabledEndpoint = CreateEndpoint(tenant.Id, disabledConsumer.Id, "disabled", WebhookEndpointStatus.Disabled);
        var wrongEventEndpoint = CreateEndpoint(tenant.Id, localConsumer.Id, "wrong-event", WebhookEndpointStatus.Active);
        setupContext.WebhookEndpoints.AddRange(
            localEndpoint,
            compositeEndpoint,
            svixEndpoint,
            disabledEndpoint,
            wrongEventEndpoint);

        setupContext.WebhookEndpointSubscriptions.AddRange(
            CreateSubscription(tenant.Id, localEndpoint.Id, publishedType.Id),
            CreateSubscription(tenant.Id, compositeEndpoint.Id, publishedType.Id),
            CreateSubscription(tenant.Id, svixEndpoint.Id, publishedType.Id),
            CreateSubscription(tenant.Id, disabledEndpoint.Id, publishedType.Id),
            CreateSubscription(tenant.Id, wrongEventEndpoint.Id, updatedType.Id));
        await setupContext.SaveChangesAsync();

        var repository = new WebhookEndpointRepository(setupContext);
        var result = await repository.GetActiveSubscribedEndpointsAsync(
            tenant.Id,
            "event.published",
            WebhookProviderMode.Local,
            CancellationToken.None);

        await Assert.That(result.Select(e => e.Id)).IsEquivalentTo([localEndpoint.Id, compositeEndpoint.Id]);
        await Assert.That(result.SelectMany(e => e.Subscriptions).Select(e => e.EventType!.Name).Distinct())
            .IsEquivalentTo(["event.published"]);
    }

    [Test]
    public async Task IncomingWebhookRepository_TryCreate_IsIdempotentPerTenantProviderMessage()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenantA = CreateTenant("incoming-webhook-a");
        var tenantB = CreateTenant("incoming-webhook-b");
        context.Tenants.AddRange(tenantA, tenantB);
        await context.SaveChangesAsync();

        var repository = new IncomingWebhookMessageRepository(context);
        var first = CreateIncomingMessage(tenantA.Id, "coop", "msg-123", "idem-123");
        var duplicate = CreateIncomingMessage(tenantA.Id, "coop", "msg-123", "idem-duplicate");
        var otherTenant = CreateIncomingMessage(tenantB.Id, "coop", "msg-123", "idem-123");

        await Assert.That(await repository.TryCreateAsync(first, CancellationToken.None)).IsTrue();
        await Assert.That(await repository.TryCreateAsync(duplicate, CancellationToken.None)).IsFalse();
        await Assert.That(await repository.TryCreateAsync(otherTenant, CancellationToken.None)).IsTrue();

        var persistedCount = await context.IncomingWebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .CountAsync(e => e.Provider == "coop" && e.ProviderMessageId == "msg-123");
        await Assert.That(persistedCount).IsEqualTo(2);
    }

    [Test]
    public async Task ProviderLinkRepository_ReturnsTenantScopedExternalMessageAndPendingSyncRows()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenantA = CreateTenant("provider-link-a");
        var tenantB = CreateTenant("provider-link-b");
        context.Tenants.AddRange(tenantA, tenantB);
        await context.SaveChangesAsync();

        var repository = new WebhookProviderLinkRepository(context);
        var tenantALink = CreateProviderLink(tenantA.Id, "msg-shared");
        var tenantBLink = CreateProviderLink(tenantB.Id, "msg-shared");

        await repository.CreateAsync(tenantALink, CancellationToken.None);
        await repository.CreateAsync(tenantBLink, CancellationToken.None);

        var resolved = await repository.GetByExternalMessageIdAsync(
            tenantA.Id,
            WebhookExternalProvider.Svix,
            "msg-shared",
            CancellationToken.None);
        var pendingLinks = await repository.GetPendingByProviderAsync(
            WebhookExternalProvider.Svix,
            limit: 10,
            CancellationToken.None);

        await Assert.That(resolved).IsNotNull();
        await Assert.That(resolved!.TenantId).IsEqualTo(tenantA.Id);
        await Assert.That(pendingLinks.Select(e => e.Id)).IsEquivalentTo([tenantALink.Id, tenantBLink.Id]);
    }

    [Test]
    public async Task MessageRepository_ClearExpiredPayloads_RemovesPayloadOnlyAfterRetention()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenant = CreateTenant("webhook-payload-retention");
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var expired = CreateMessage(tenant.Id, "event.published", "evt-expired", DateTime.UtcNow.AddMinutes(-5));
        var retained = CreateMessage(tenant.Id, "event.updated", "evt-retained", DateTime.UtcNow.AddDays(1));
        context.WebhookMessages.AddRange(expired, retained);
        await context.SaveChangesAsync();

        var repository = new WebhookMessageRepository(context);
        var clearedCount = await repository.ClearExpiredPayloadsAsync(DateTime.UtcNow, batchSize: 10, CancellationToken.None);

        var messages = await context.WebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .ToDictionaryAsync(e => e.Id);

        await Assert.That(clearedCount).IsEqualTo(1);
        await Assert.That(messages[expired.Id].GetPayloadBytes()).IsNull();
        await Assert.That(messages[expired.Id].PayloadClearedAt).IsNotNull();
        await Assert.That(System.Text.Encoding.UTF8.GetString(messages[retained.Id].GetPayloadBytes()!)).Contains("evt-retained");
        await Assert.That(messages[retained.Id].PayloadClearedAt).IsNull();
    }

    [Test]
    public async Task MessageAndDeliveryRepositories_ApplyExplicitTenantPredicates()
    {
        await fixture.ResetAsync();
        await using var context = fixture.CreateDbContext();
        var tenantA = CreateTenant("webhook-audit-a");
        var tenantB = CreateTenant("webhook-audit-b");
        var consumerA = CreateConsumer(tenantA.Id, "Tenant A Consumer", WebhookProviderMode.Local);
        var consumerB = CreateConsumer(tenantB.Id, "Tenant B Consumer", WebhookProviderMode.Local);
        var endpointA = CreateEndpoint(tenantA.Id, consumerA.Id, "tenant-a", WebhookEndpointStatus.Active);
        var endpointB = CreateEndpoint(tenantB.Id, consumerB.Id, "tenant-b", WebhookEndpointStatus.Active);
        var messageA = CreateMessage(tenantA.Id, "event.published", "evt-a", DateTime.UtcNow.AddDays(14), consumerA.Id);
        var messageB = CreateMessage(tenantB.Id, "event.published", "evt-b", DateTime.UtcNow.AddDays(14), consumerB.Id);
        var attemptA = CreateDeliveryAttempt(tenantA.Id, messageA.Id, endpointA.Id);
        var attemptB = CreateDeliveryAttempt(tenantB.Id, messageB.Id, endpointB.Id);
        context.Tenants.AddRange(tenantA, tenantB);
        context.WebhookConsumers.AddRange(consumerA, consumerB);
        context.WebhookEndpoints.AddRange(endpointA, endpointB);
        context.WebhookMessages.AddRange(messageA, messageB);
        context.WebhookDeliveryAttempts.AddRange(attemptA, attemptB);
        await context.SaveChangesAsync();
        var messageRepository = new WebhookMessageRepository(context);
        var attemptRepository = new WebhookDeliveryAttemptRepository(context);

        var tenantAMessages = await messageRepository.ListByTenantAsync(tenantA.Id, 10, CancellationToken.None);
        var crossTenantMessage = await messageRepository.GetByTenantAndIdAsync(
            tenantA.Id,
            messageB.Id,
            CancellationToken.None);
        var tenantAAttempts = await attemptRepository.ListByTenantAsync(
            tenantA.Id,
            messageId: null,
            endpointId: null,
            limit: 10,
            CancellationToken.None);
        var tenantAAttemptsForForeignEndpoint = await attemptRepository.ListByTenantAsync(
            tenantA.Id,
            messageId: null,
            endpointB.Id,
            limit: 10,
            CancellationToken.None);
        var crossTenantAttempt = await attemptRepository.GetByTenantAndIdAsync(
            tenantA.Id,
            attemptB.Id,
            CancellationToken.None);

        await Assert.That(tenantAMessages.Select(e => e.Id)).IsEquivalentTo([messageA.Id]);
        await Assert.That(crossTenantMessage).IsNull();
        await Assert.That(tenantAAttempts.Select(e => e.Id)).IsEquivalentTo([attemptA.Id]);
        await Assert.That(tenantAAttemptsForForeignEndpoint).IsEmpty();
        await Assert.That(crossTenantAttempt).IsNull();
    }

    private static Tenant CreateTenant(string slugPrefix)
    {
        return new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = "Webhook Test Tenant " + slugPrefix,
            Slug = slugPrefix + "-" + Guid.NewGuid().ToString("N")[..8],
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!
        };
    }

    private static WebhookConsumer CreateConsumer(Guid tenantId, string name, WebhookProviderMode providerMode)
    {
        return new WebhookConsumer
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerKind = WebhookConsumerKind.Tenant,
            Name = name,
            Status = WebhookConsumerStatus.Active,
            ProviderMode = providerMode
        };
    }

    private static WebhookEventType CreateEventType(string name)
    {
        return new WebhookEventType
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            GroupName = name.Split('.')[0],
            Description = "Webhook event type for " + name,
            SchemaJson = "{\"type\":\"object\",\"additionalProperties\":true}",
            SchemaVersion = 1,
            IsPublic = true,
            IsEnabled = true,
            PayloadRetentionDays = 14
        };
    }

    private static WebhookEndpoint CreateEndpoint(
        Guid tenantId,
        Guid consumerId,
        string name,
        WebhookEndpointStatus status)
    {
        return new WebhookEndpoint
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerId = consumerId,
            Url = "https://example.com/webhooks/" + name,
            Status = status,
            SecretRef = "webhooks/" + name + "/secret",
            SecretVersion = 1,
            MaxAttempts = 8,
            TimeoutSeconds = 15
        };
    }

    private static WebhookEndpointSubscription CreateSubscription(
        Guid tenantId,
        Guid endpointId,
        Guid eventTypeId)
    {
        return new WebhookEndpointSubscription
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            EndpointId = endpointId,
            EventTypeId = eventTypeId,
            IsEnabled = true
        };
    }

    private static IncomingWebhookMessage CreateIncomingMessage(
        Guid tenantId,
        string provider,
        string providerMessageId,
        string idempotencyKey)
    {
        var now = DateTime.UtcNow;
        var payloadBytes = System.Text.Encoding.UTF8.GetBytes("{\"decision\":\"accepted\"}");
        var payloadHash = $"sha256:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(payloadBytes)).ToLowerInvariant()}";
        return IncomingWebhookMessage.CreateVerified(
            tenantId,
            provider,
            providerMessageId,
            idempotencyKey,
            "decision.created",
            payloadBytes,
            payloadHash,
            "application/json",
            "utf-8",
            "{\"svix-id\":\"" + providerMessageId + "\"}",
            now,
            now,
            now.AddDays(14));
    }

    private static WebhookMessage CreateMessage(
        Guid tenantId,
        string eventType,
        string eventId,
        DateTime retentionUntil,
        Guid? consumerId = null)
    {
        var createdAt = retentionUntil.AddDays(-1);
        return WebhookMessage.Create(
            Guid.CreateVersion7(),
            tenantId,
            eventType,
            eventId,
            "event",
            Guid.CreateVersion7(),
            consumerId,
            System.Text.Encoding.UTF8.GetBytes("{\"id\":\"" + eventId + "\"}"),
            "application/json",
            "utf-8",
            createdAt,
            retentionUntil,
            createdAt);
    }

    private static WebhookDeliveryAttempt CreateDeliveryAttempt(
        Guid tenantId,
        Guid messageId,
        Guid endpointId)
    {
        return new WebhookDeliveryAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            MessageId = messageId,
            EndpointId = endpointId,
            AttemptNumber = 1,
            Outcome = WebhookDeliveryAttemptOutcome.Failed,
            ScheduledAt = DateTime.UtcNow.AddMinutes(-5),
            SentAt = DateTime.UtcNow.AddMinutes(-4),
            CompletedAt = DateTime.UtcNow.AddMinutes(-4),
            HttpStatusCode = 500,
            FailureCategory = "server_error",
            DurationMs = 123,
            NextRetryAt = DateTime.UtcNow.AddMinutes(10)
        };
    }

    private static WebhookProviderLink CreateProviderLink(Guid tenantId, string externalMessageId)
    {
        return new WebhookProviderLink
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Provider = WebhookExternalProvider.Svix,
            ExternalMessageId = externalMessageId,
            SyncState = WebhookProviderLinkSyncState.Pending
        };
    }

    private sealed record StaticTenantContext(Guid TenantId) : ITenantContext;
}
