// ABOUTME: Performs bounded, restartable classification of legacy webhook delivery rows.
// ABOUTME: Materializes only provable plans and reports ambiguous provider or in-flight evidence without guessing success.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Services;

public sealed class WebhookLegacyBackfillService(ExploreDbContext dbContext)
{
    public async Task<WebhookLegacyBackfillResult> RunBatchAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (batchSize is < 1 or > 1_000)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        var messages = dbContext.WebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookLegacyBackfill);
        var consumers = dbContext.WebhookConsumers
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookLegacyBackfill);
        var plans = dbContext.WebhookDeliveryPlanSnapshots
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookLegacyBackfill);
        var plannedMessageIds = plans.Select(plan => plan.WebhookMessageId);

        var candidates = await (
                from message in messages
                where message.ConsumerId != null
                join consumer in consumers
                    on new { message.TenantId, ConsumerId = message.ConsumerId!.Value }
                    equals new { consumer.TenantId, ConsumerId = consumer.Id }
                where !plannedMessageIds.Contains(message.Id)
                orderby message.CreatedAt, message.Id
                select new { Message = message, Consumer = consumer })
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            var materializedAt = AsUtcOffset(candidate.Message.MaterializedAt);
            dbContext.WebhookDeliveryPlanSnapshots.Add(WebhookDeliveryPlanSnapshot.Create(
                candidate.Message.TenantId,
                candidate.Message.Id,
                candidate.Consumer.Id,
                candidate.Consumer.ProviderMode,
                "legacy-current-consumer-v1",
                "legacy-json-v1",
                "legacy-message-retention",
                "1",
                AsUtcOffset(candidate.Message.PayloadRetentionUntil),
                materializedAt));
        }

        if (candidates.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var remainingEligibleMessages = await (
                from message in messages
                where message.ConsumerId != null
                join consumer in consumers
                    on new { message.TenantId, ConsumerId = message.ConsumerId!.Value }
                    equals new { consumer.TenantId, ConsumerId = consumer.Id }
                where !plannedMessageIds.Contains(message.Id)
                select message.Id)
            .CountAsync(cancellationToken);
        var orphanMessages = await messages
            .CountAsync(message =>
                message.ConsumerId == null ||
                !consumers.Any(consumer =>
                    consumer.TenantId == message.TenantId &&
                    consumer.Id == message.ConsumerId),
                cancellationToken);
        var providerLinksRequiringManualReconciliation = await dbContext.WebhookProviderLinks
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookLegacyBackfill)
            .CountAsync(cancellationToken);
        var ambiguousInFlightAttempts = await dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookLegacyBackfill)
            .CountAsync(attempt =>
                attempt.OutcomeId == (int)WebhookDeliveryAttemptOutcome.Sending &&
                attempt.CompletedAt == null,
                cancellationToken);
        var legacyUnverifiedBindings = await dbContext.WebhookConsumerProviderBindings
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookLegacyBackfill)
            .CountAsync(binding =>
                binding.VerificationStateId == (int)WebhookProviderBindingVerificationState.LegacyUnverified,
                cancellationToken);
        var persistedPlans = await plans
            .AsNoTracking()
            .OrderBy(plan => plan.TenantId)
            .ThenBy(plan => plan.WebhookMessageId)
            .Select(plan => new
            {
                plan.TenantId,
                plan.WebhookMessageId,
                plan.WebhookConsumerId,
                plan.ProviderModeId,
                plan.ConfigurationVersion,
                plan.EventContractVersion,
                plan.RetentionPolicy,
                plan.RetentionPolicyVersion,
                plan.PayloadRetentionUntilUtc,
                plan.MaterializedAtUtc
            })
            .ToListAsync(cancellationToken);

        var result = new WebhookLegacyBackfillResult(
            candidates.Count,
            persistedPlans.Count,
            remainingEligibleMessages,
            orphanMessages,
            providerLinksRequiringManualReconciliation,
            ambiguousInFlightAttempts,
            legacyUnverifiedBindings,
            ComputeChecksum(persistedPlans.Select(plan => string.Join('|',
                plan.TenantId.ToString("D", CultureInfo.InvariantCulture),
                plan.WebhookMessageId.ToString("D", CultureInfo.InvariantCulture),
                plan.WebhookConsumerId.ToString("D", CultureInfo.InvariantCulture),
                plan.ProviderModeId.ToString(CultureInfo.InvariantCulture),
                plan.ConfigurationVersion,
                plan.EventContractVersion,
                plan.RetentionPolicy,
                plan.RetentionPolicyVersion,
                plan.PayloadRetentionUntilUtc.ToString("O", CultureInfo.InvariantCulture),
                plan.MaterializedAtUtc.ToString("O", CultureInfo.InvariantCulture)))));
        dbContext.ChangeTracker.Clear();
        return result;
    }

    private static DateTimeOffset AsUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static string ComputeChecksum(IEnumerable<string> rows)
    {
        var bytes = Encoding.UTF8.GetBytes(string.Join('\n', rows));
        return $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";
    }
}

public sealed record WebhookLegacyBackfillResult(
    int MaterializedPlans,
    int TotalPlans,
    int RemainingEligibleMessages,
    int OrphanMessages,
    int ProviderLinksRequiringManualReconciliation,
    int AmbiguousInFlightAttempts,
    int LegacyUnverifiedBindings,
    string PlanChecksum);
