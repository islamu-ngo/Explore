// ABOUTME: Deterministic classification tests for resumable legacy webhook delivery backfill.
// ABOUTME: Proves bounded convergence, stable checksums, and manual treatment of ambiguous provider evidence.

using System.Text;
using Explore.Domain;
using Explore.Persistence;
using Explore.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using TUnit.Core;

namespace Event.Persistence.IntegrationTests.Repositories;

public sealed class WebhookLegacyBackfillClassificationTests
{
    [Test]
    public async Task RepeatedBackfill_ConvergesWithStableCountsAndNeverGuessesProviderSuccess()
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseInMemoryDatabase($"webhook-legacy-{Guid.NewGuid():N}")
            .Options;
        await using var context = new ExploreDbContext(options);
        var now = new DateTime(2026, 7, 13, 19, 0, 0, DateTimeKind.Utc);
        var tenantId = Guid.CreateVersion7();
        var firstConsumer = CreateConsumer(tenantId, "First", WebhookProviderMode.Local, now);
        var secondConsumer = CreateConsumer(tenantId, "Second", WebhookProviderMode.Svix, now);
        var firstMessage = CreateMessage(tenantId, firstConsumer.Id, "evt-first", now);
        var secondMessage = CreateMessage(tenantId, secondConsumer.Id, "evt-second", now.AddSeconds(1));
        var orphanMessage = CreateMessage(tenantId, null, "evt-orphan", now.AddSeconds(2));
        var endpointId = Guid.CreateVersion7();

        context.WebhookConsumers.AddRange(firstConsumer, secondConsumer);
        context.WebhookMessages.AddRange(firstMessage, secondMessage, orphanMessage);
        context.WebhookProviderLinks.Add(new WebhookProviderLink
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerId = secondConsumer.Id,
            MessageId = secondMessage.Id,
            Provider = WebhookExternalProvider.Svix,
            ExternalAppId = "legacy-app",
            ExternalMessageId = "legacy-provider-message",
            SyncState = WebhookProviderLinkSyncState.Synced,
            CreatedAt = now
        });
        context.WebhookDeliveryAttempts.Add(new WebhookDeliveryAttempt
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            MessageId = firstMessage.Id,
            EndpointId = endpointId,
            AttemptNumber = 1,
            Outcome = WebhookDeliveryAttemptOutcome.Sending,
            ScheduledAt = now,
            SentAt = now,
            CreatedAt = now
        });
        await context.SaveChangesAsync();

        var service = new WebhookLegacyBackfillService(context);
        var firstBatch = await service.RunBatchAsync(1, CancellationToken.None);
        var secondBatch = await service.RunBatchAsync(1, CancellationToken.None);
        var converged = await service.RunBatchAsync(1, CancellationToken.None);
        var repeated = await service.RunBatchAsync(1, CancellationToken.None);

        await Assert.That(firstBatch.MaterializedPlans).IsEqualTo(1);
        await Assert.That(secondBatch.MaterializedPlans).IsEqualTo(1);
        await Assert.That(converged.MaterializedPlans).IsEqualTo(0);
        await Assert.That(repeated.MaterializedPlans).IsEqualTo(0);
        await Assert.That(converged.TotalPlans).IsEqualTo(2);
        await Assert.That(repeated.TotalPlans).IsEqualTo(converged.TotalPlans);
        await Assert.That(repeated.PlanChecksum).IsEqualTo(converged.PlanChecksum);
        await Assert.That(repeated.RemainingEligibleMessages).IsEqualTo(0);
        await Assert.That(repeated.OrphanMessages).IsEqualTo(1);
        await Assert.That(repeated.ProviderLinksRequiringManualReconciliation).IsEqualTo(1);
        await Assert.That(repeated.AmbiguousInFlightAttempts).IsEqualTo(1);
        await Assert.That(await context.WebhookProviderPublications.CountAsync()).IsEqualTo(0);
    }

    private static WebhookConsumer CreateConsumer(
        Guid tenantId,
        string name,
        WebhookProviderMode providerMode,
        DateTime now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            ConsumerKind = WebhookConsumerKind.Tenant,
            Name = name,
            Status = WebhookConsumerStatus.Active,
            ProviderMode = providerMode,
            CreatedAt = now
        };

    private static WebhookMessage CreateMessage(
        Guid tenantId,
        Guid? consumerId,
        string eventId,
        DateTime materializedAt) =>
        WebhookMessage.Create(
            Guid.CreateVersion7(),
            tenantId,
            "event.updated",
            eventId,
            "event",
            Guid.CreateVersion7(),
            consumerId,
            Encoding.UTF8.GetBytes($"{{\"id\":\"{eventId}\"}}"),
            "application/json",
            "utf-8",
            materializedAt.AddMinutes(-1),
            materializedAt.AddDays(14),
            materializedAt);
}
