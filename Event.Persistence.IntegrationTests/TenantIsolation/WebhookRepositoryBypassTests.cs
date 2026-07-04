// ABOUTME: Verifies webhook repository tenant-filter bypasses stay bounded to webhook predicates.
// ABOUTME: Proves tenant operations and worker queues do not leak ambient tenant rows.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.TenantIsolation;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public class WebhookRepositoryBypassTests(PostgreSqlContainerFixture fixture)
{
    [Test]
    public async Task TenantOperationBypasses_WithAmbientTenant_ReturnAndUpdateOnlyExplicitWebhookRows()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();

        var tenantA = CreateTenant("webhook-tenant-a");
        var tenantB = CreateTenant("webhook-tenant-b");
        var publishedType = CreateEventType("event.published");
        var updatedType = CreateEventType("event.updated");
        var consumerA = CreateConsumer(tenantA.Id, "Tenant A Consumer", WebhookProviderMode.Local, "shared-app");
        var consumerB = CreateConsumer(tenantB.Id, "Tenant B Consumer", WebhookProviderMode.Local, "shared-app");
        var endpointA = CreateEndpoint(tenantA.Id, consumerA.Id, "tenant-a", WebhookEndpointStatus.Active);
        var disabledEndpointA = CreateEndpoint(tenantA.Id, consumerA.Id, "tenant-a-disabled", WebhookEndpointStatus.Disabled);
        var wrongEventEndpointA = CreateEndpoint(tenantA.Id, consumerA.Id, "tenant-a-wrong-event", WebhookEndpointStatus.Active);
        var endpointB = CreateEndpoint(tenantB.Id, consumerB.Id, "tenant-b", WebhookEndpointStatus.Active);
        var messageA = CreateMessage(tenantA.Id, consumerA.Id, "evt-a", WebhookMessageStatus.Pending);
        var messageB = CreateMessage(tenantB.Id, consumerB.Id, "evt-b", WebhookMessageStatus.Pending);
        var attemptA = CreateDeliveryAttempt(
            tenantA.Id,
            messageA.Id,
            endpointA.Id,
            WebhookDeliveryAttemptStatus.Failed,
            DateTime.UtcNow.AddMinutes(-10));
        var attemptB = CreateDeliveryAttempt(
            tenantB.Id,
            messageB.Id,
            endpointB.Id,
            WebhookDeliveryAttemptStatus.Failed,
            DateTime.UtcNow.AddMinutes(-10));
        var incomingA = CreateIncomingMessage(tenantA.Id, "coop", "provider-shared", "idem-a");
        var incomingB = CreateIncomingMessage(tenantB.Id, "coop", "provider-shared", "idem-b");
        var linkA = CreateProviderLink(tenantA.Id, "external-shared", messageA.Id);
        var linkB = CreateProviderLink(tenantB.Id, "external-shared", messageB.Id);

        seedContext.Tenants.AddRange(tenantA, tenantB);
        seedContext.WebhookEventTypes.AddRange(publishedType, updatedType);
        seedContext.WebhookConsumers.AddRange(consumerA, consumerB);
        seedContext.WebhookEndpoints.AddRange(endpointA, disabledEndpointA, wrongEventEndpointA, endpointB);
        seedContext.WebhookEndpointSubscriptions.AddRange(
            CreateSubscription(tenantA.Id, endpointA.Id, publishedType.Id),
            CreateSubscription(tenantA.Id, disabledEndpointA.Id, publishedType.Id),
            CreateSubscription(tenantA.Id, wrongEventEndpointA.Id, updatedType.Id),
            CreateSubscription(tenantB.Id, endpointB.Id, publishedType.Id));
        seedContext.WebhookMessages.AddRange(messageA, messageB);
        seedContext.WebhookDeliveryAttempts.AddRange(attemptA, attemptB);
        seedContext.IncomingWebhookMessages.AddRange(incomingA, incomingB);
        seedContext.WebhookProviderLinks.AddRange(linkA, linkB);
        await seedContext.SaveChangesAsync();

        await using var tenantBContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantB.Id));
        var visibleConsumers = await tenantBContext.WebhookConsumers.AsNoTracking().Select(row => row.Id).ToListAsync();
        var visibleEndpoints = await tenantBContext.WebhookEndpoints.AsNoTracking().Select(row => row.Id).ToListAsync();
        var visibleMessages = await tenantBContext.WebhookMessages.AsNoTracking().Select(row => row.Id).ToListAsync();
        var visibleAttempts = await tenantBContext.WebhookDeliveryAttempts.AsNoTracking().Select(row => row.Id).ToListAsync();
        var visibleIncoming = await tenantBContext.IncomingWebhookMessages.AsNoTracking().Select(row => row.Id).ToListAsync();
        var visibleLinks = await tenantBContext.WebhookProviderLinks.AsNoTracking().Select(row => row.Id).ToListAsync();

        var consumerRepository = new WebhookConsumerRepository(tenantBContext);
        var endpointRepository = new WebhookEndpointRepository(tenantBContext);
        var messageRepository = new WebhookMessageRepository(tenantBContext);
        var attemptRepository = new WebhookDeliveryAttemptRepository(tenantBContext);
        var incomingRepository = new IncomingWebhookMessageRepository(tenantBContext);
        var providerLinkRepository = new WebhookProviderLinkRepository(tenantBContext);

        var tenantAConsumers = await consumerRepository.ListByTenantAsync(tenantA.Id, 10, CancellationToken.None);
        var tenantAConsumerByApp = await consumerRepository.GetByExternalProviderAppIdAsync(
            tenantA.Id,
            "shared-app",
            CancellationToken.None);
        var tenantAActiveEndpoints = await endpointRepository.GetActiveSubscribedEndpointsAsync(
            tenantA.Id,
            "event.published",
            WebhookProviderMode.Local,
            CancellationToken.None);
        var wrongTenantEndpoint = await endpointRepository.GetByTenantAndIdAsync(
            tenantB.Id,
            endpointA.Id,
            CancellationToken.None);
        var tenantAMessages = await messageRepository.ListByTenantAsync(tenantA.Id, 10, CancellationToken.None);
        var wrongTenantMessage = await messageRepository.GetByTenantAndIdAsync(
            tenantB.Id,
            messageA.Id,
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
        var nextAttemptNumber = await attemptRepository.GetNextAttemptNumberAsync(
            tenantA.Id,
            messageA.Id,
            endpointA.Id,
            CancellationToken.None);
        var activeAttemptExists = await attemptRepository.HasActiveAttemptForEndpointAsync(
            tenantA.Id,
            messageA.Id,
            endpointA.Id,
            CancellationToken.None);
        var tenantAIncoming = await incomingRepository.GetByProviderMessageIdAsync(
            tenantA.Id,
            "coop",
            "provider-shared",
            CancellationToken.None);
        var tenantALink = await providerLinkRepository.GetByExternalMessageIdAsync(
            tenantA.Id,
            WebhookExternalProvider.Svix,
            "external-shared",
            CancellationToken.None);
        var tenantALinkByMessage = await providerLinkRepository.GetByTenantMessageAndProviderAsync(
            tenantA.Id,
            WebhookExternalProvider.Svix,
            messageA.Id,
            CancellationToken.None);

        var changedAt = new DateTime(2026, 1, 7, 12, 0, 0, DateTimeKind.Utc);
        await endpointRepository.ArchiveAsync(tenantA.Id, disabledEndpointA.Id, changedAt, CancellationToken.None);
        await messageRepository.MarkProviderQueuedAsync(
            tenantA.Id,
            messageA.Id,
            "provider-message-a",
            changedAt,
            CancellationToken.None);
        await incomingRepository.MarkProcessedAsync(tenantA.Id, incomingA.Id, changedAt, CancellationToken.None);

        await using var verifyContext = fixture.CreateDbContext();
        var endpoints = await verifyContext.WebhookEndpoints
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(row => row.Id == disabledEndpointA.Id || row.Id == endpointB.Id)
            .ToDictionaryAsync(row => row.Id);
        var messages = await verifyContext.WebhookMessages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(row => row.Id == messageA.Id || row.Id == messageB.Id)
            .ToDictionaryAsync(row => row.Id);
        var incomingMessages = await verifyContext.IncomingWebhookMessages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(row => row.Id == incomingA.Id || row.Id == incomingB.Id)
            .ToDictionaryAsync(row => row.Id);

        await Assert.That(visibleConsumers).IsEquivalentTo([consumerB.Id]);
        await Assert.That(visibleEndpoints).IsEquivalentTo([endpointB.Id]);
        await Assert.That(visibleMessages).IsEquivalentTo([messageB.Id]);
        await Assert.That(visibleAttempts).IsEquivalentTo([attemptB.Id]);
        await Assert.That(visibleIncoming).IsEquivalentTo([incomingB.Id]);
        await Assert.That(visibleLinks).IsEquivalentTo([linkB.Id]);

        await Assert.That(tenantAConsumers.Select(row => row.Id)).IsEquivalentTo([consumerA.Id]);
        await Assert.That(tenantAConsumerByApp).IsNotNull();
        await Assert.That(tenantAConsumerByApp!.Id).IsEqualTo(consumerA.Id);
        await Assert.That(tenantAActiveEndpoints.Select(row => row.Id)).IsEquivalentTo([endpointA.Id]);
        await Assert.That(wrongTenantEndpoint).IsNull();
        await Assert.That(tenantAMessages.Select(row => row.Id)).IsEquivalentTo([messageA.Id]);
        await Assert.That(wrongTenantMessage).IsNull();
        await Assert.That(tenantAAttempts.Select(row => row.Id)).IsEquivalentTo([attemptA.Id]);
        await Assert.That(tenantAAttemptsForForeignEndpoint).IsEmpty();
        await Assert.That(nextAttemptNumber).IsEqualTo(2);
        await Assert.That(activeAttemptExists).IsFalse();
        await Assert.That(tenantAIncoming).IsNotNull();
        await Assert.That(tenantAIncoming!.Id).IsEqualTo(incomingA.Id);
        await Assert.That(tenantALink).IsNotNull();
        await Assert.That(tenantALink!.Id).IsEqualTo(linkA.Id);
        await Assert.That(tenantALinkByMessage).IsNotNull();
        await Assert.That(tenantALinkByMessage!.Id).IsEqualTo(linkA.Id);

        await Assert.That(endpoints[disabledEndpointA.Id].Status).IsEqualTo(WebhookEndpointStatus.Archived);
        await Assert.That(endpoints[endpointB.Id].Status).IsEqualTo(WebhookEndpointStatus.Active);
        await Assert.That(messages[messageA.Id].Status).IsEqualTo(WebhookMessageStatus.Queued);
        await Assert.That(messages[messageA.Id].ProviderMessageId).IsEqualTo("provider-message-a");
        await Assert.That(messages[messageB.Id].Status).IsEqualTo(WebhookMessageStatus.Pending);
        await Assert.That(incomingMessages[incomingA.Id].Status).IsEqualTo(IncomingWebhookMessageStatus.Processed);
        await Assert.That(incomingMessages[incomingB.Id].Status).IsEqualTo(IncomingWebhookMessageStatus.Verified);
    }

    [Test]
    public async Task WorkerQueueBypasses_WithAmbientTenant_ReturnOnlyEligibleRowsAndMutateExactWebhookRows()
    {
        await fixture.ResetAsync();
        await using var seedContext = fixture.CreateDbContext();

        var tenantA = CreateTenant("webhook-worker-a");
        var tenantB = CreateTenant("webhook-worker-b");
        var consumerA = CreateConsumer(tenantA.Id, "Tenant A Worker", WebhookProviderMode.Local, "worker-a");
        var consumerB = CreateConsumer(tenantB.Id, "Tenant B Worker", WebhookProviderMode.Local, "worker-b");
        var endpointA = CreateEndpoint(tenantA.Id, consumerA.Id, "worker-a", WebhookEndpointStatus.Active);
        var endpointB = CreateEndpoint(tenantB.Id, consumerB.Id, "worker-b", WebhookEndpointStatus.Active);
        var now = new DateTime(2026, 1, 8, 12, 0, 0, DateTimeKind.Utc);

        var dueMessageA = CreateMessage(tenantA.Id, consumerA.Id, "due-a", WebhookMessageStatus.Queued);
        var dueMessageB = CreateMessage(tenantB.Id, consumerB.Id, "due-b", WebhookMessageStatus.Queued);
        var futureMessageA = CreateMessage(tenantA.Id, consumerA.Id, "future-a", WebhookMessageStatus.Queued);
        var staleMessageA = CreateMessage(tenantA.Id, consumerA.Id, "stale-a", WebhookMessageStatus.Queued);
        var statusMessageA = CreateMessage(tenantA.Id, consumerA.Id, "status-a", WebhookMessageStatus.Queued);
        var expiredMessageA = CreateMessage(tenantA.Id, consumerA.Id, "expired-a", WebhookMessageStatus.Pending, now.AddMinutes(-1));
        var expiredMessageB = CreateMessage(tenantB.Id, consumerB.Id, "expired-b", WebhookMessageStatus.Pending, now.AddMinutes(-1));
        var retainedMessageA = CreateMessage(tenantA.Id, consumerA.Id, "retained-a", WebhookMessageStatus.Pending, now.AddDays(1));

        var dueAttemptA = CreateDeliveryAttempt(
            tenantA.Id,
            dueMessageA.Id,
            endpointA.Id,
            WebhookDeliveryAttemptStatus.Scheduled,
            now.AddMinutes(-10));
        var dueAttemptB = CreateDeliveryAttempt(
            tenantB.Id,
            dueMessageB.Id,
            endpointB.Id,
            WebhookDeliveryAttemptStatus.Scheduled,
            now.AddMinutes(-9));
        var futureAttemptA = CreateDeliveryAttempt(
            tenantA.Id,
            futureMessageA.Id,
            endpointA.Id,
            WebhookDeliveryAttemptStatus.Scheduled,
            now.AddMinutes(10));
        var staleAttemptA = CreateDeliveryAttempt(
            tenantA.Id,
            staleMessageA.Id,
            endpointA.Id,
            WebhookDeliveryAttemptStatus.Sending,
            now.AddMinutes(-30));
        staleAttemptA.ProcessingStartedAt = now.AddMinutes(-30);
        staleAttemptA.ProcessingLeaseToken = Guid.CreateVersion7();
        var statusSucceededAttemptA = CreateDeliveryAttempt(
            tenantA.Id,
            statusMessageA.Id,
            endpointA.Id,
            WebhookDeliveryAttemptStatus.Succeeded,
            now.AddMinutes(-20));
        var statusFailedAttemptA = CreateDeliveryAttempt(
            tenantA.Id,
            statusMessageA.Id,
            endpointA.Id,
            WebhookDeliveryAttemptStatus.Failed,
            now.AddMinutes(-19),
            attemptNumber: 2);
        var pendingLinkA = CreateProviderLink(tenantA.Id, "pending-a", dueMessageA.Id);
        var pendingLinkB = CreateProviderLink(tenantB.Id, "pending-b", dueMessageB.Id);
        var syncedLinkA = CreateProviderLink(
            tenantA.Id,
            "synced-a",
            dueMessageA.Id,
            WebhookProviderLinkSyncState.Synced);

        seedContext.Tenants.AddRange(tenantA, tenantB);
        seedContext.WebhookConsumers.AddRange(consumerA, consumerB);
        seedContext.WebhookEndpoints.AddRange(endpointA, endpointB);
        seedContext.WebhookMessages.AddRange(
            dueMessageA,
            dueMessageB,
            futureMessageA,
            staleMessageA,
            statusMessageA,
            expiredMessageA,
            expiredMessageB,
            retainedMessageA);
        seedContext.WebhookDeliveryAttempts.AddRange(
            dueAttemptA,
            dueAttemptB,
            futureAttemptA,
            staleAttemptA,
            statusSucceededAttemptA,
            statusFailedAttemptA);
        seedContext.WebhookProviderLinks.AddRange(pendingLinkA, pendingLinkB, syncedLinkA);
        await seedContext.SaveChangesAsync();

        await using var tenantBContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantB.Id));
        var visibleAttempts = await tenantBContext.WebhookDeliveryAttempts
            .AsNoTracking()
            .Select(row => row.Id)
            .ToListAsync();
        var visibleMessages = await tenantBContext.WebhookMessages
            .AsNoTracking()
            .Select(row => row.Id)
            .ToListAsync();
        var visibleLinks = await tenantBContext.WebhookProviderLinks
            .AsNoTracking()
            .Select(row => row.Id)
            .ToListAsync();

        var attemptRepository = new WebhookDeliveryAttemptRepository(tenantBContext);
        var messageRepository = new WebhookMessageRepository(tenantBContext);
        var providerLinkRepository = new WebhookProviderLinkRepository(tenantBContext);

        var dueAttempts = await attemptRepository.GetDueScheduledAsync(10, now, CancellationToken.None);
        var dueAttemptCount = await attemptRepository.CountDueScheduledAsync(now, CancellationToken.None);
        var staleSendingCount = await attemptRepository.CountStaleSendingAsync(now.AddMinutes(-10), CancellationToken.None);
        var wrongTenantClaimed = await attemptRepository.TryMarkAsSendingAsync(
            tenantB.Id,
            dueAttemptA.Id,
            Guid.CreateVersion7(),
            now,
            CancellationToken.None);
        var leaseToken = Guid.CreateVersion7();
        var claimed = await attemptRepository.TryMarkAsSendingAsync(
            tenantA.Id,
            dueAttemptA.Id,
            leaseToken,
            now,
            CancellationToken.None);
        await attemptRepository.MarkSucceededAsync(
            tenantA.Id,
            dueAttemptA.Id,
            leaseToken,
            now,
            now.AddSeconds(1),
            httpStatusCode: 204,
            durationMs: 25,
            responseBodyPreview: "accepted",
            CancellationToken.None);
        var recovered = await attemptRepository.ResetStaleSendingAsync(
            now.AddMinutes(-10),
            now,
            "worker_recovered",
            batchSize: 10,
            CancellationToken.None);
        var clearedPayloads = await messageRepository.ClearExpiredPayloadsAsync(now, 10, CancellationToken.None);
        await messageRepository.RefreshLocalDeliveryStatusAsync(
            tenantA.Id,
            statusMessageA.Id,
            now,
            CancellationToken.None);
        var pendingLinks = await providerLinkRepository.GetPendingByProviderAsync(
            WebhookExternalProvider.Svix,
            limit: 10,
            CancellationToken.None);

        await using var verifyContext = fixture.CreateDbContext();
        var attempts = await verifyContext.WebhookDeliveryAttempts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(row => row.Id == dueAttemptA.Id
                || row.Id == dueAttemptB.Id
                || row.Id == futureAttemptA.Id
                || row.Id == staleAttemptA.Id)
            .ToDictionaryAsync(row => row.Id);
        var messages = await verifyContext.WebhookMessages
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(row => row.Id == expiredMessageA.Id
                || row.Id == expiredMessageB.Id
                || row.Id == retainedMessageA.Id
                || row.Id == statusMessageA.Id)
            .ToDictionaryAsync(row => row.Id);

        await Assert.That(visibleAttempts).IsEquivalentTo([dueAttemptB.Id]);
        await Assert.That(visibleMessages).Contains(dueMessageB.Id);
        await Assert.That(visibleMessages).DoesNotContain(dueMessageA.Id);
        await Assert.That(visibleLinks).IsEquivalentTo([pendingLinkB.Id]);

        await Assert.That(dueAttempts.Select(row => row.Id)).IsEquivalentTo([dueAttemptA.Id, dueAttemptB.Id]);
        await Assert.That(dueAttemptCount).IsEqualTo(2);
        await Assert.That(staleSendingCount).IsEqualTo(1);
        await Assert.That(wrongTenantClaimed).IsFalse();
        await Assert.That(claimed).IsTrue();
        await Assert.That(recovered).IsEqualTo(1);
        await Assert.That(clearedPayloads).IsEqualTo(2);
        await Assert.That(pendingLinks.Select(row => row.Id)).IsEquivalentTo([pendingLinkA.Id, pendingLinkB.Id]);

        await Assert.That(attempts[dueAttemptA.Id].Status).IsEqualTo(WebhookDeliveryAttemptStatus.Succeeded);
        await Assert.That(attempts[dueAttemptA.Id].HttpStatusCode).IsEqualTo(204);
        await Assert.That(attempts[dueAttemptB.Id].Status).IsEqualTo(WebhookDeliveryAttemptStatus.Scheduled);
        await Assert.That(attempts[futureAttemptA.Id].Status).IsEqualTo(WebhookDeliveryAttemptStatus.Scheduled);
        await Assert.That(attempts[staleAttemptA.Id].Status).IsEqualTo(WebhookDeliveryAttemptStatus.Scheduled);
        await Assert.That(attempts[staleAttemptA.Id].FailureCategory).IsEqualTo("worker_recovered");
        await Assert.That(messages[expiredMessageA.Id].PayloadJson).IsNull();
        await Assert.That(messages[expiredMessageB.Id].PayloadJson).IsNull();
        await Assert.That(messages[retainedMessageA.Id].PayloadJson).IsNotNull();
        await Assert.That(messages[statusMessageA.Id].Status).IsEqualTo(WebhookMessageStatus.PartiallyFailed);
    }

    private static Tenant CreateTenant(string slugPrefix)
    {
        return new Tenant
        {
            Id = Guid.CreateVersion7(),
            FullName = $"Webhook Bypass {slugPrefix}",
            Slug = $"{slugPrefix}-{Guid.NewGuid().ToString("N")[..8]}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
    }

    private static WebhookConsumer CreateConsumer(
        Guid tenantId,
        string name,
        WebhookProviderMode providerMode,
        string externalProviderAppId)
    {
        return new WebhookConsumer
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerKind = WebhookConsumerKind.Tenant,
            Name = name,
            Status = WebhookConsumerStatus.Active,
            ProviderMode = providerMode,
            ExternalProviderAppId = externalProviderAppId,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    private static WebhookEventType CreateEventType(string name)
    {
        return new WebhookEventType
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            GroupName = name.Split('.')[0],
            Description = $"Webhook event type for {name}",
            SchemaJson = "{\"type\":\"object\",\"additionalProperties\":true}",
            SchemaVersion = 1,
            IsPublic = true,
            IsEnabled = true,
            PayloadRetentionDays = 14,
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
            Url = $"https://example.com/webhooks/{name}",
            Status = status,
            SecretRef = $"webhooks/{name}/secret",
            SecretVersion = 1,
            MaxAttempts = 8,
            TimeoutSeconds = 15,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
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
            IsEnabled = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    private static WebhookMessage CreateMessage(
        Guid tenantId,
        Guid consumerId,
        string eventId,
        WebhookMessageStatus status,
        DateTime? retentionUntil = null)
    {
        return new WebhookMessage
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            EventType = "event.published",
            EventId = eventId,
            AggregateKind = "event",
            AggregateId = Guid.CreateVersion7(),
            ConsumerId = consumerId,
            PayloadJson = $"{{\"id\":\"{eventId}\"}}",
            PayloadHash = $"sha256:{eventId}",
            PayloadRetentionUntil = retentionUntil ?? new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            ProviderMode = WebhookProviderMode.Local,
            Status = status,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    private static WebhookDeliveryAttempt CreateDeliveryAttempt(
        Guid tenantId,
        Guid messageId,
        Guid endpointId,
        WebhookDeliveryAttemptStatus status,
        DateTime scheduledAt,
        int attemptNumber = 1)
    {
        return new WebhookDeliveryAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            MessageId = messageId,
            EndpointId = endpointId,
            AttemptNumber = attemptNumber,
            Status = status,
            ScheduledAt = scheduledAt,
            SentAt = status == WebhookDeliveryAttemptStatus.Sending ? scheduledAt : null,
            CompletedAt = status is WebhookDeliveryAttemptStatus.Succeeded or WebhookDeliveryAttemptStatus.Failed
                ? scheduledAt.AddSeconds(1)
                : null,
            HttpStatusCode = status == WebhookDeliveryAttemptStatus.Succeeded ? 204 : null,
            FailureCategory = status == WebhookDeliveryAttemptStatus.Failed ? "server_error" : null,
            ResponseBodyPreview = status == WebhookDeliveryAttemptStatus.Failed ? "upstream returned 500" : null,
            DurationMs = status is WebhookDeliveryAttemptStatus.Succeeded or WebhookDeliveryAttemptStatus.Failed ? 123 : null,
            NextRetryAt = status == WebhookDeliveryAttemptStatus.Failed ? scheduledAt.AddMinutes(10) : null,
            CreatedAt = scheduledAt,
        };
    }

    private static IncomingWebhookMessage CreateIncomingMessage(
        Guid tenantId,
        string provider,
        string providerMessageId,
        string idempotencyKey)
    {
        return new IncomingWebhookMessage
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Provider = provider,
            ProviderMessageId = providerMessageId,
            IdempotencyKey = idempotencyKey,
            EventType = "decision.created",
            HeadersJson = $"{{\"svix-id\":\"{providerMessageId}\"}}",
            PayloadJson = "{\"decision\":\"accepted\"}",
            PayloadHash = $"sha256:{providerMessageId}",
            Status = IncomingWebhookMessageStatus.Verified,
            ReceivedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            VerifiedAt = new DateTime(2026, 1, 1, 0, 0, 1, DateTimeKind.Utc),
        };
    }

    private static WebhookProviderLink CreateProviderLink(
        Guid tenantId,
        string externalMessageId,
        Guid messageId,
        WebhookProviderLinkSyncState syncState = WebhookProviderLinkSyncState.Pending)
    {
        return new WebhookProviderLink
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Provider = WebhookExternalProvider.Svix,
            ExternalMessageId = externalMessageId,
            MessageId = messageId,
            SyncState = syncState,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
    }

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;
}
