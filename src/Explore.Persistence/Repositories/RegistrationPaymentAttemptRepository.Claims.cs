// ABOUTME: Handles payment-attempt composition queries, claims, active-slot release, and pre-handoff cancellation.
// ABOUTME: Preserves idempotent claim recovery and routes handed-off attempts into reconciliation.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed partial class RegistrationPaymentAttemptRepository
{
    public async Task<(PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)?> GetLatestByOrderAsync(
        Guid tenantId,
        Guid registrationOrderId,
        CancellationToken cancellationToken)
    {
        var row = await (from attempt in dbContext.PaymentAttempts
                         join effect in dbContext.CheckoutDispatchEffects on new { attempt.TenantId, PaymentAttemptId = attempt.Id }
                             equals new { effect.TenantId, effect.PaymentAttemptId }
                         where attempt.TenantId == tenantId && attempt.RegistrationOrderId == registrationOrderId
                         orderby attempt.CreatedAt descending
                         select new { attempt, effect })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : (row.attempt, row.effect);
    }

    public async Task<(PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)?> GetActiveByOrderAsync(
        Guid tenantId,
        Guid registrationOrderId,
        CancellationToken cancellationToken)
    {
        var row = await (from attempt in dbContext.PaymentAttempts
                         join effect in dbContext.CheckoutDispatchEffects on new { attempt.TenantId, PaymentAttemptId = attempt.Id }
                             equals new { effect.TenantId, effect.PaymentAttemptId }
                         where attempt.TenantId == tenantId &&
                               attempt.RegistrationOrderId == registrationOrderId &&
                               attempt.ActiveUniquenessSlot == PaymentAttempt.ActiveUniquenessSlotValue
                         select new { attempt, effect })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : (row.attempt, row.effect);
    }

    public async Task<(PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)?> GetByOrderCompositionAsync(
        Guid tenantId,
        Guid registrationOrderId,
        string compositionRevision,
        CancellationToken cancellationToken)
    {
        var row = await (from attempt in dbContext.PaymentAttempts
                         join effect in dbContext.CheckoutDispatchEffects on new { attempt.TenantId, PaymentAttemptId = attempt.Id }
                             equals new { effect.TenantId, effect.PaymentAttemptId }
                         where attempt.TenantId == tenantId &&
                               attempt.RegistrationOrderId == registrationOrderId &&
                               attempt.CompositionRevision == compositionRevision
                         orderby attempt.CreatedAt descending, attempt.Id descending
                         select new { attempt, effect })
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : (row.attempt, row.effect);
    }

    public async Task<RegistrationPaymentAttemptClaimOutcome> ClaimAsync(
        RegistrationPaymentAttemptClaim claim,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(claim);
        (PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)? active = await GetActiveByOrderAsync(
            claim.Attempt.TenantId, claim.Attempt.RegistrationOrderId, cancellationToken);
        if (active is not null)
        {
            return new(active.Value.Attempt, active.Value.DispatchEffect, Created: false);
        }

        (PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)? historical = await GetByOrderCompositionAsync(
            claim.Attempt.TenantId, claim.Attempt.RegistrationOrderId, claim.Attempt.CompositionRevision, cancellationToken);
        if (historical is not null &&
            (historical.Value.Attempt.ActiveUniquenessSlot == PaymentAttempt.ActiveUniquenessSlotValue ||
             string.Equals(historical.Value.Attempt.ProviderIdempotencyKey, claim.Attempt.ProviderIdempotencyKey, StringComparison.Ordinal)))
        {
            return new(historical.Value.Attempt, historical.Value.DispatchEffect, Created: false);
        }

        await dbContext.PaymentAttempts.AddAsync(claim.Attempt, cancellationToken);
        await dbContext.CheckoutDispatchEffects.AddAsync(claim.DispatchEffect, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new(claim.Attempt, claim.DispatchEffect, Created: true);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            active = await GetActiveByOrderAsync(
                claim.Attempt.TenantId, claim.Attempt.RegistrationOrderId, cancellationToken);
            if (active is not null)
            {
                return new(active.Value.Attempt, active.Value.DispatchEffect, Created: false);
            }

            historical = await GetByOrderCompositionAsync(
                claim.Attempt.TenantId, claim.Attempt.RegistrationOrderId, claim.Attempt.CompositionRevision, cancellationToken);
            if (historical is not null &&
                (historical.Value.Attempt.ActiveUniquenessSlot == PaymentAttempt.ActiveUniquenessSlotValue ||
                 string.Equals(historical.Value.Attempt.ProviderIdempotencyKey, claim.Attempt.ProviderIdempotencyKey, StringComparison.Ordinal)))
            {
                return new(historical.Value.Attempt, historical.Value.DispatchEffect, Created: false);
            }

            throw;
        }
    }

    public async Task<bool> ReleaseActiveSlotAsync(PaymentAttempt attempt, DateTime releasedAt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        PaymentAttempt? tracked = await dbContext.PaymentAttempts.FirstOrDefaultAsync(
            value => value.TenantId == attempt.TenantId && value.Id == attempt.Id, cancellationToken);
        if (tracked is null || !tracked.TryReleaseActiveSlot(releasedAt))
        {
            return false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PaymentCancellationDisposition> TryCancelBeforeProviderHandoffAsync(
        Guid tenantId,
        Guid registrationOrderId,
        DateTime cancelledAt,
        CancellationToken cancellationToken)
    {
        (PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)? row = await GetLatestByOrderAsync(
            tenantId, registrationOrderId, cancellationToken);
        if (row is null)
        {
            return PaymentCancellationDisposition.NoAttempt;
        }

        PaymentAttempt attempt = row.Value.Attempt;
        CheckoutDispatchEffect effect = row.Value.DispatchEffect;
        bool unclaimedCreated = attempt.PaymentAttemptStatusId == (int)PaymentAttemptStatusEnum.Created &&
            attempt.ProviderCheckoutSessionId is null &&
            effect.Status is OutboxMessageStatus.Pending or OutboxMessageStatus.Failed &&
            effect.ProcessingLeaseToken is null;
        if (unclaimedCreated)
        {
            _ = attempt.MarkCancelled(cancelledAt, null);
            _ = attempt.TryReleaseActiveSlot(cancelledAt.AddTicks(1));
            await dbContext.CheckoutDispatchEffects
                .Where(value => value.TenantId == tenantId && value.Id == effect.Id &&
                                value.ProcessingLeaseToken == null &&
                                (value.Status == OutboxMessageStatus.Pending || value.Status == OutboxMessageStatus.Failed))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(value => value.Status, OutboxMessageStatus.DeadLettered)
                    .SetProperty(value => value.ParkedAt, cancelledAt)
                    .SetProperty(value => value.LastFailureCode, "checkout_cancelled_before_handoff")
                    .SetProperty(value => value.UpdatedAt, cancelledAt), cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return PaymentCancellationDisposition.CancelledBeforeHandoff;
        }

        await EnsureReconciliationDueAsync(attempt, null, cancelledAt, cancellationToken);
        return PaymentCancellationDisposition.RequiresReconciliation;
    }

    public async Task<bool> RetryParkedPreHandoffAsync(
        Guid tenantId,
        Guid paymentAttemptId,
        DateTime requestedAt,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || paymentAttemptId == Guid.Empty || requestedAt.Kind != DateTimeKind.Utc)
        {
            return false;
        }

        int rows = await (from effect in dbContext.CheckoutDispatchEffects
                          join attempt in dbContext.PaymentAttempts on new { effect.TenantId, effect.PaymentAttemptId }
                              equals new { attempt.TenantId, PaymentAttemptId = attempt.Id }
                          where effect.TenantId == tenantId && effect.PaymentAttemptId == paymentAttemptId &&
                                effect.Status == OutboxMessageStatus.DeadLettered && attempt.ProviderCheckoutSessionId == null &&
                                attempt.PaymentAttemptStatusId == (int)PaymentAttemptStatusEnum.DispatchPending
                          select effect)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(value => value.Status, OutboxMessageStatus.Failed)
                .SetProperty(value => value.NextAttemptAt, requestedAt)
                .SetProperty(value => value.ParkedAt, (DateTime?)null)
                .SetProperty(value => value.LastFailureCode, (string?)null)
                .SetProperty(value => value.UpdatedAt, requestedAt), cancellationToken);
        return ClearTrackerWhenUpdated(rows);
    }
}
