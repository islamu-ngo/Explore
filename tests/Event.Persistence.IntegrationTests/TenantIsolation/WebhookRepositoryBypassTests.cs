// ABOUTME: Verifies webhook repository tenant-filter bypasses stay bounded to webhook predicates.
// ABOUTME: Proves tenant operations and worker queues do not leak ambient tenant rows.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
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
        var messageA = CreateMessage(tenantA.Id, consumerA.Id, "evt-a");
        var messageB = CreateMessage(tenantB.Id, consumerB.Id, "evt-b");
        var attemptA = CreateDeliveryAttempt(
            tenantA.Id,
            messageA.Id,
            endpointA.Id,
            WebhookDeliveryAttemptOutcome.Failed,
            DateTime.UtcNow.AddMinutes(-10));
        var attemptB = CreateDeliveryAttempt(
            tenantB.Id,
            messageB.Id,
            endpointB.Id,
            WebhookDeliveryAttemptOutcome.Failed,
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
        var tenantAIncoming = await incomingRepository.GetByProviderMessageIdForUpdateAsync(
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
        var incomingLeaseToken = Guid.CreateVersion7();
        tenantAIncoming!.Claim("bypass-test", incomingLeaseToken, changedAt.AddMinutes(1), changedAt);
        tenantAIncoming.Ignore(
            incomingLeaseToken,
            tenantAIncoming.ProcessingFence,
            tenantAIncoming.ProcessingGeneration,
            "test_settled",
            "Tenant-scoped transition completed.",
            changedAt.AddSeconds(1));
        await incomingRepository.SaveChangesAsync(CancellationToken.None);

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
        await Assert.That(messages[messageA.Id].PayloadHash).IsEqualTo(messageA.PayloadHash);
        await Assert.That(messages[messageB.Id].PayloadHash).IsEqualTo(messageB.PayloadHash);
        await Assert.That(incomingMessages[incomingA.Id].Status).IsEqualTo(IncomingWebhookMessageStatus.Ignored);
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

        var dueMessageA = CreateMessage(tenantA.Id, consumerA.Id, "due-a");
        var dueMessageB = CreateMessage(tenantB.Id, consumerB.Id, "due-b");
        var futureMessageA = CreateMessage(tenantA.Id, consumerA.Id, "future-a");
        var staleMessageA = CreateMessage(tenantA.Id, consumerA.Id, "stale-a");
        var statusMessageA = CreateMessage(tenantA.Id, consumerA.Id, "status-a");
        var expiredMessageA = CreateMessage(tenantA.Id, consumerA.Id, "expired-a", now.AddMinutes(-1));
        var expiredMessageB = CreateMessage(tenantB.Id, consumerB.Id, "expired-b", now.AddMinutes(-1));
        var retainedMessageA = CreateMessage(tenantA.Id, consumerA.Id, "retained-a", now.AddDays(1));

        var dueAttemptA = CreateDeliveryAttempt(
            tenantA.Id,
            dueMessageA.Id,
            endpointA.Id,
            WebhookDeliveryAttemptOutcome.Scheduled,
            now.AddMinutes(-10));
        var dueAttemptB = CreateDeliveryAttempt(
            tenantB.Id,
            dueMessageB.Id,
            endpointB.Id,
            WebhookDeliveryAttemptOutcome.Scheduled,
            now.AddMinutes(-9));
        var futureAttemptA = CreateDeliveryAttempt(
            tenantA.Id,
            futureMessageA.Id,
            endpointA.Id,
            WebhookDeliveryAttemptOutcome.Scheduled,
            now.AddMinutes(10));
        var staleAttemptA = CreateDeliveryAttempt(
            tenantA.Id,
            staleMessageA.Id,
            endpointA.Id,
            WebhookDeliveryAttemptOutcome.Sending,
            now.AddMinutes(-30));
        staleAttemptA.ProcessingStartedAt = now.AddMinutes(-30);
        staleAttemptA.ProcessingLeaseToken = Guid.CreateVersion7();
        var statusSucceededAttemptA = CreateDeliveryAttempt(
            tenantA.Id,
            statusMessageA.Id,
            endpointA.Id,
            WebhookDeliveryAttemptOutcome.Succeeded,
            now.AddMinutes(-20));
        var statusFailedAttemptA = CreateDeliveryAttempt(
            tenantA.Id,
            statusMessageA.Id,
            endpointA.Id,
            WebhookDeliveryAttemptOutcome.Failed,
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

        var dueTenantIds = await attemptRepository.GetDueTenantIdsAsync(10, now, CancellationToken.None);
        var dueAttemptCount = await attemptRepository.CountDueScheduledAsync(now, CancellationToken.None);
        var staleSendingCount = await attemptRepository.CountStaleSendingAsync(now.AddMinutes(-10), CancellationToken.None);
        var wrongTenantClaims = await attemptRepository.ClaimDueAsync(
            new WebhookDeliveryClaimRequest(
                1,
                10,
                10,
                [tenantB.Id],
                now,
                TimeSpan.FromMinutes(5),
                dueAttemptA.Id),
            new Dictionary<Guid, WebhookDeliveryClaimLimits>
            {
                [tenantB.Id] = new(10, 10, 10)
            },
            CancellationToken.None);
        var claims = await attemptRepository.ClaimDueAsync(
            new WebhookDeliveryClaimRequest(
                1,
                10,
                10,
                [tenantA.Id],
                now,
                TimeSpan.FromMinutes(5),
                dueAttemptA.Id),
            new Dictionary<Guid, WebhookDeliveryClaimLimits>
            {
                [tenantA.Id] = new(10, 10, 10)
            },
            CancellationToken.None);
        var leaseToken = claims.Single().LeaseToken;
        var processingFence = claims.Single().ProcessingFence;
        await attemptRepository.MarkSucceededAsync(
            tenantA.Id,
            dueAttemptA.Id,
            leaseToken,
            processingFence,
            now,
            now.AddSeconds(1),
            httpStatusCode: 204,
            durationMs: 25,
            cancellationToken: CancellationToken.None);
        var recovered = await attemptRepository.ResetStaleSendingAsync(
            now.AddMinutes(-10),
            now,
            "worker_recovered",
            batchSize: 10,
            CancellationToken.None);
        var clearedPayloads = await messageRepository.ClearExpiredPayloadsAsync(now, 10, CancellationToken.None);
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

        await Assert.That(dueTenantIds).IsEquivalentTo(new[] { tenantA.Id, tenantB.Id });
        await Assert.That(dueAttemptCount).IsEqualTo(2);
        await Assert.That(staleSendingCount).IsEqualTo(1);
        await Assert.That(wrongTenantClaims).IsEmpty();
        await Assert.That(claims.Select(claim => claim.Attempt.Id)).IsEquivalentTo(new[] { dueAttemptA.Id });
        await Assert.That(recovered).IsEqualTo(1);
        await Assert.That(clearedPayloads).IsEqualTo(2);
        await Assert.That(pendingLinks.Select(row => row.Id)).IsEquivalentTo([pendingLinkA.Id, pendingLinkB.Id]);

        await Assert.That(attempts[dueAttemptA.Id].Outcome).IsEqualTo(WebhookDeliveryAttemptOutcome.Succeeded);
        await Assert.That(attempts[dueAttemptA.Id].HttpStatusCode).IsEqualTo(204);
        await Assert.That(attempts[dueAttemptB.Id].Outcome).IsEqualTo(WebhookDeliveryAttemptOutcome.Scheduled);
        await Assert.That(attempts[futureAttemptA.Id].Outcome).IsEqualTo(WebhookDeliveryAttemptOutcome.Scheduled);
        await Assert.That(attempts[staleAttemptA.Id].Outcome).IsEqualTo(WebhookDeliveryAttemptOutcome.Scheduled);
        await Assert.That(attempts[staleAttemptA.Id].FailureCategory).IsEqualTo("worker_recovered");
        await Assert.That(messages[expiredMessageA.Id].GetPayloadBytes()).IsNull();
        await Assert.That(messages[expiredMessageB.Id].GetPayloadBytes()).IsNull();
        await Assert.That(messages[retainedMessageA.Id].GetPayloadBytes()).IsNotNull();
        await Assert.That(messages[statusMessageA.Id].PayloadHash).IsEqualTo(statusMessageA.PayloadHash);
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
        DateTime? retentionUntil = null)
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return WebhookMessage.Create(
            Guid.CreateVersion7(),
            tenantId,
            "event.published",
            eventId,
            "event",
            Guid.CreateVersion7(),
            consumerId,
            System.Text.Encoding.UTF8.GetBytes($"{{\"id\":\"{eventId}\"}}"),
            "application/json",
            "utf-8",
            createdAt,
            retentionUntil ?? new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            createdAt);
    }

    private static WebhookDeliveryAttempt CreateDeliveryAttempt(
        Guid tenantId,
        Guid messageId,
        Guid endpointId,
        WebhookDeliveryAttemptOutcome status,
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
            Outcome = status,
            ScheduledAt = scheduledAt,
            SentAt = status == WebhookDeliveryAttemptOutcome.Sending ? scheduledAt : null,
            CompletedAt = status is WebhookDeliveryAttemptOutcome.Succeeded or WebhookDeliveryAttemptOutcome.Failed
                ? scheduledAt.AddSeconds(1)
                : null,
            HttpStatusCode = status == WebhookDeliveryAttemptOutcome.Succeeded ? 204 : null,
            FailureCategory = status == WebhookDeliveryAttemptOutcome.Failed ? "server_error" : null,
            DurationMs = status is WebhookDeliveryAttemptOutcome.Succeeded or WebhookDeliveryAttemptOutcome.Failed ? 123 : null,
            NextRetryAt = status == WebhookDeliveryAttemptOutcome.Failed ? scheduledAt.AddMinutes(10) : null,
            CreatedAt = scheduledAt,
        };
    }

    private static IncomingWebhookMessage CreateIncomingMessage(
        Guid tenantId,
        string provider,
        string providerMessageId,
        string idempotencyKey)
    {
        var receivedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var verifiedAt = receivedAt.AddSeconds(1);
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
            $"{{\"svix-id\":\"{providerMessageId}\"}}",
            receivedAt,
            verifiedAt,
            verifiedAt.AddDays(14));
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
