// ABOUTME: Finds provider payments and manages payment reconciliation claims, decisions, and health.
// ABOUTME: Applies fenced reconciliation outcomes and schedules paid registration finalization.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed partial class RegistrationPaymentAttemptRepository
{
    public Task<PaymentAttempt?> FindByProviderObjectAsync(
        Guid tenantId,
        string externalAccountId,
        string providerCheckoutSessionId,
        CancellationToken cancellationToken) =>
        dbContext.PaymentAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .SingleOrDefaultAsync(value =>
                value.TenantId == tenantId &&
                value.RecipientSnapshot.ExternalAccountId == externalAccountId &&
                value.ProviderCheckoutSessionId == providerCheckoutSessionId,
                cancellationToken);

    public async Task EnsureReconciliationDueAsync(
        PaymentAttempt attempt,
        Guid? sourceIncomingWebhookMessageId,
        DateTime dueAt,
        CancellationToken cancellationToken,
        Guid? checkoutDispatchEffectId = null,
        DateTime? checkoutDispatchUnknownAt = null,
        long? checkoutDispatchProcessingFence = null,
        int? checkoutDispatchAttemptCount = null)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        PaymentReconciliationEffect? existing = await dbContext.PaymentReconciliationEffects
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .SingleOrDefaultAsync(value => value.TenantId == attempt.TenantId && value.PaymentAttemptId == attempt.Id, cancellationToken);
        if (existing is null)
        {
            await dbContext.PaymentReconciliationEffects.AddAsync(
                PaymentReconciliationEffect.Create(
                    attempt,
                    dueAt,
                    sourceIncomingWebhookMessageId,
                    checkoutDispatchEffectId,
                    checkoutDispatchUnknownAt,
                    checkoutDispatchProcessingFence,
                    checkoutDispatchAttemptCount),
                cancellationToken);
        }
        else
        {
            existing.MakeDue(
                dueAt,
                sourceIncomingWebhookMessageId,
                checkoutDispatchEffectId,
                checkoutDispatchUnknownAt,
                checkoutDispatchProcessingFence,
                checkoutDispatchAttemptCount);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentReconciliationClaim>> ClaimDueReconciliationsAsync(
        string leaseOwner,
        int batchSize,
        DateTime claimedAt,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(leaseOwner) || batchSize is < 1 or > 1000 || leaseDuration <= TimeSpan.Zero)
        {
            return [];
        }

        List<PaymentReconciliationEffect> candidates = await dbContext.PaymentReconciliationEffects
            .IgnoreTenantFilter(TenantFilterBypassReasons.RegistrationFinalizationWorkerCrossTenantQueue)
            .Where(value =>
                ((value.Status == OutboxMessageStatus.Pending || value.Status == OutboxMessageStatus.Failed) && value.NextAttemptAt <= claimedAt) ||
                (value.Status == OutboxMessageStatus.Processing && value.ProcessingLeaseExpiresAt <= claimedAt))
            .OrderBy(value => value.NextAttemptAt ?? value.CreatedAt)
            .Take(1)
            .ToListAsync(cancellationToken);
        var claims = new List<PaymentReconciliationClaim>(candidates.Count);
        foreach (PaymentReconciliationEffect effect in candidates)
        {
            if (effect.Status == OutboxMessageStatus.Processing)
            {
                effect.RecoverExpiredClaim(claimedAt);
            }

            Guid leaseToken = Guid.CreateVersion7();
            effect.Claim(leaseOwner, leaseToken, claimedAt.Add(leaseDuration), claimedAt);
            claims.Add(new(
                effect.TenantId,
                effect.Id,
                effect.PaymentAttemptId,
                leaseToken,
                effect.ProcessingFence,
                effect.AttemptCount,
                effect.CheckoutDispatchEffectId,
                effect.CheckoutDispatchUnknownAt,
                effect.CheckoutDispatchProcessingFence,
                effect.CheckoutDispatchAttemptCount));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return claims;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return [];
        }
    }

    public async Task<PaymentAttempt?> GetReconciliationAttemptAsync(
        PaymentReconciliationClaim claim,
        DateTime observedAt,
        CancellationToken cancellationToken)
    {
        PaymentReconciliationEffect? effect = await dbContext.PaymentReconciliationEffects
            .IgnoreTenantFilter(TenantFilterBypassReasons.RegistrationFinalizationWorkerCrossTenantQueue)
            .SingleOrDefaultAsync(value => value.TenantId == claim.TenantId && value.Id == claim.EffectId, cancellationToken);
        effect?.EnsureClaim(claim.LeaseToken, claim.ProcessingFence, observedAt);
        return effect is null
            ? null
            : await dbContext.PaymentAttempts
                .IgnoreTenantFilter(TenantFilterBypassReasons.RegistrationFinalizationWorkerCrossTenantQueue)
                .AsNoTracking()
                .SingleOrDefaultAsync(value => value.TenantId == claim.TenantId && value.Id == claim.PaymentAttemptId, cancellationToken);
    }

    public Task<bool> SettleReconciliationAsync(
        PaymentReconciliationClaim claim,
        PaymentReconciliationDecision decision,
        CancellationToken cancellationToken) =>
        ExecuteFencedTransactionAsync(async token =>
        {
            PaymentReconciliationEffect? effect = await dbContext.PaymentReconciliationEffects
                .IgnoreTenantFilter(TenantFilterBypassReasons.RegistrationFinalizationWorkerCrossTenantQueue)
                .SingleOrDefaultAsync(value => value.TenantId == claim.TenantId && value.Id == claim.EffectId, token);
            if (effect is null)
            {
                return false;
            }

            try
            {
                effect.EnsureClaim(claim.LeaseToken, claim.ProcessingFence, decision.ObservedAt);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            PaymentAttempt? attempt = await dbContext.PaymentAttempts
                .IgnoreTenantFilter(TenantFilterBypassReasons.RegistrationFinalizationWorkerCrossTenantQueue)
                .SingleOrDefaultAsync(value => value.TenantId == claim.TenantId && value.Id == claim.PaymentAttemptId, token);
            if (attempt is null)
            {
                effect.Park("payment_attempt_missing", decision.ObservedAt);
                await dbContext.SaveChangesAsync(token);
                return true;
            }

            bool applied;
            try
            {
                applied = ApplyPaymentDecision(attempt, decision);
            }
            catch (InvalidOperationException)
            {
                effect.Park("payment_reconciliation_terminal_conflict", decision.ObservedAt);
                await dbContext.SaveChangesAsync(token);
                return true;
            }
            if (decision.Disposition == PaymentReconciliationDisposition.Complete &&
                decision.Status is PaymentAttemptStatusEnum.Failed or PaymentAttemptStatusEnum.Cancelled &&
                attempt.PaymentAttemptStatusId == (int)decision.Status)
            {
                _ = attempt.TryReleaseActiveSlot(decision.ObservedAt);
            }
            bool succeededConsistently =
                attempt.PaymentAttemptStatusId == (int)PaymentAttemptStatusEnum.Succeeded &&
                string.Equals(attempt.ProviderCheckoutSessionId, decision.CheckoutSessionId, StringComparison.Ordinal) &&
                string.Equals(attempt.ProviderPaymentId, decision.PaymentId, StringComparison.Ordinal);
            if (decision.Status == PaymentAttemptStatusEnum.Succeeded && !applied && !succeededConsistently)
            {
                DateTime settledAt = decision.ObservedAt > attempt.LastStatusObservedAt
                    ? decision.ObservedAt
                    : attempt.LastStatusObservedAt.AddTicks(1);
                effect.Retry(
                    "payment_reconciliation_stale_success",
                    settledAt.AddMinutes(2),
                    settledAt,
                    unknown: true);
                await dbContext.SaveChangesAsync(token);
                return true;
            }
            if (decision.Status == PaymentAttemptStatusEnum.Succeeded &&
                decision.CheckoutSessionId is not null && decision.PaymentId is not null &&
                !await dbContext.PaymentSucceededObservations
                    .IgnoreTenantFilter(TenantFilterBypassReasons.RegistrationFinalizationWorkerCrossTenantQueue)
                    .AnyAsync(value => value.TenantId == claim.TenantId && value.PaymentAttemptId == claim.PaymentAttemptId, token))
            {
                await dbContext.PaymentSucceededObservations.AddAsync(PaymentSucceededObservation.Create(
                    attempt,
                    effect.SourceIncomingWebhookMessageId,
                    decision.CheckoutSessionId,
                    decision.PaymentId,
                    decision.ProviderRequestId,
                    decision.ObservedAt), token);
            }

            if (decision.Status == PaymentAttemptStatusEnum.Succeeded && succeededConsistently)
            {
                RegistrationFinalizationEffect? finalizationEffect = await dbContext.RegistrationFinalizationEffects
                    .IgnoreTenantFilter(TenantFilterBypassReasons.RegistrationFinalizationWorkerCrossTenantQueue)
                    .SingleOrDefaultAsync(value => value.TenantId == claim.TenantId &&
                                                   value.RegistrationOrderId == attempt.RegistrationOrderId,
                        token);
                if (finalizationEffect is null)
                {
                    RegistrationOrder? order = await dbContext.RegistrationOrders
                        .IgnoreTenantFilter(TenantFilterBypassReasons.RegistrationFinalizationWorkerCrossTenantQueue)
                        .SingleOrDefaultAsync(value => value.TenantId == claim.TenantId &&
                                                       value.Id == attempt.RegistrationOrderId,
                            token);
                    if (order is not null)
                    {
                        await dbContext.RegistrationFinalizationEffects.AddAsync(
                            RegistrationFinalizationEffect.Create(order, decision.ObservedAt), token);
                    }
                }
                else
                {
                    finalizationEffect.Request(decision.ObservedAt);
                }
            }

            switch (decision.Disposition)
            {
                case PaymentReconciliationDisposition.Complete:
                    effect.Complete(decision.ObservedAt);
                    break;
                case PaymentReconciliationDisposition.Park:
                    effect.Park(decision.FailureCode, decision.ObservedAt);
                    break;
                default:
                    effect.Retry(decision.FailureCode, decision.NextAttemptAt ?? decision.ObservedAt.AddMinutes(5), decision.ObservedAt, decision.Status == PaymentAttemptStatusEnum.Unknown);
                    break;
            }

            await dbContext.SaveChangesAsync(token);
            return true;
        }, cancellationToken);

    public async Task<PaymentReconciliationHealth> GetReconciliationHealthAsync(
        DateTime observedAt,
        CancellationToken cancellationToken)
    {
        IQueryable<PaymentReconciliationEffect> effects = dbContext.PaymentReconciliationEffects
            .IgnoreTenantFilter(TenantFilterBypassReasons.RegistrationFinalizationWorkerCrossTenantQueue)
            .AsNoTracking();
        int due = await effects.CountAsync(value =>
            ((value.Status == OutboxMessageStatus.Pending || value.Status == OutboxMessageStatus.Failed) && value.NextAttemptAt <= observedAt) ||
            (value.Status == OutboxMessageStatus.Processing && value.ProcessingLeaseExpiresAt <= observedAt), cancellationToken);
        int unknown = await effects.CountAsync(value => value.UnknownAt != null && value.Status == OutboxMessageStatus.Failed, cancellationToken);
        int parked = await effects.CountAsync(value => value.Status == OutboxMessageStatus.DeadLettered, cancellationToken);
        DateTime? oldest = await effects
            .Where(value => (value.Status == OutboxMessageStatus.Pending || value.Status == OutboxMessageStatus.Failed) && value.NextAttemptAt <= observedAt)
            .MinAsync(value => (DateTime?)value.NextAttemptAt, cancellationToken);
        int configurationBlocked = await dbContext.CheckoutDispatchEffects
            .IgnoreTenantFilter(TenantFilterBypassReasons.RegistrationFinalizationWorkerCrossTenantQueue)
            .AsNoTracking()
            .CountAsync(value => value.Status == OutboxMessageStatus.Failed &&
                                 value.LastFailureCode != null &&
                                 value.LastFailureCode.StartsWith("checkout_provider_"), cancellationToken);
        configurationBlocked += await effects.CountAsync(value => value.Status == OutboxMessageStatus.Failed &&
            value.LastFailureCode != null && value.LastFailureCode.StartsWith("checkout_provider_"), cancellationToken);
        int duplicateSucceededOrders = await dbContext.PaymentSucceededObservations
            .IgnoreTenantFilter(TenantFilterBypassReasons.RegistrationFinalizationWorkerCrossTenantQueue)
            .AsNoTracking()
            .GroupBy(value => new { value.TenantId, value.RegistrationOrderId })
            .CountAsync(group => group.Count() > 1, cancellationToken);
        return new(due, unknown, parked, oldest, configurationBlocked, duplicateSucceededOrders);
    }

    private static bool ApplyPaymentDecision(PaymentAttempt attempt, PaymentReconciliationDecision decision)
    {
        switch (decision.Status)
        {
            case PaymentAttemptStatusEnum.RequiresAction when decision.CheckoutSessionId is not null:
                return attempt.MarkRequiresAction(decision.CheckoutSessionId, decision.ObservedAt, decision.ProviderRequestId);
            case PaymentAttemptStatusEnum.Processing when decision.PaymentId is not null:
                return attempt.MarkProcessing(decision.CheckoutSessionId, decision.PaymentId, decision.ObservedAt, decision.ProviderRequestId);
            case PaymentAttemptStatusEnum.Succeeded when decision.CheckoutSessionId is not null && decision.PaymentId is not null:
                return attempt.MarkSucceededFromCheckout(decision.CheckoutSessionId, decision.PaymentId, decision.ObservedAt, decision.ProviderRequestId);
            case PaymentAttemptStatusEnum.Failed when decision.CheckoutSessionId is not null && decision.PaymentId is not null:
                return attempt.MarkFailedFromCheckout(decision.CheckoutSessionId, decision.PaymentId, decision.ObservedAt, decision.ProviderRequestId);
            case PaymentAttemptStatusEnum.Cancelled when decision.CheckoutSessionId is not null:
                return attempt.MarkCancelledFromCheckout(decision.CheckoutSessionId, decision.ObservedAt, decision.ProviderRequestId);
            case PaymentAttemptStatusEnum.Unknown:
                return attempt.MarkUnknown(decision.ObservedAt, decision.ProviderRequestId);
            default:
                return false;
        }
    }
}
