// ABOUTME: Claims or reuses one active payment attempt for a payable registration order.
// ABOUTME: Builds immutable recipient and idempotency facts locally, then persists only a post-commit dispatch effect.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Payments;
using Explore.Application.Contracts.Services;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Services.Registration;

public sealed record RegistrationPaymentAttemptClaimRequest(
    Guid TenantId,
    Guid RegistrationOrderId,
    DateTime RequestedAt,
    Guid? TerminalAttemptId = null);

public sealed record RegistrationPaymentAttemptClaimResult(
    bool Success,
    PaymentAttempt? Attempt,
    CheckoutDispatchEffect? DispatchEffect,
    bool Created,
    string Message,
    string? FailureCode = null)
{
    public static RegistrationPaymentAttemptClaimResult Failure(string failureCode, string message) => new(false, null, null, false, message, failureCode);
}

public sealed class RegistrationPaymentAttemptClaimService(
    IRegistrationPaymentAttemptRepository attempts,
    IRegistrationInventoryRepository orders,
    IEventRepository events,
    IOrganizerPaymentProviderConnectionRepository connections,
    IPaidEventPolicyRepository policies,
    IOrganizerPaymentCommerceConfiguration commerceConfiguration,
    IPaymentProviderDescriptor paymentProviderDescriptor,
    IUnitOfWork unitOfWork)
{
    private static readonly TimeSpan MinimumProviderCutoff = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MaximumProviderCutoff = TimeSpan.FromHours(24);
    private static readonly TimeSpan MaximumReadinessAge = TimeSpan.FromMinutes(5);

    public async Task<RegistrationPaymentAttemptClaimResult> ClaimAsync(
        RegistrationPaymentAttemptClaimRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new RegistrationPaymentAttemptClaimRequestValidator();
        string? validationFailure = validator.Validate(request);
        if (validationFailure is not null)
        {
            return RegistrationPaymentAttemptClaimResult.Failure("validation_failed", validationFailure);
        }

        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            RegistrationOrder? order = await orders.GetOrderForUpdateWithLinesAsync(request.RegistrationOrderId, request.TenantId, token);
            if (order is null)
            {
                return RegistrationPaymentAttemptClaimResult.Failure("not_found", "Registration order was not found.");
            }

            if ((RegistrationOrderStatusEnum)order.RegistrationOrderStatusId != RegistrationOrderStatusEnum.AwaitingPayment ||
                order.TotalDueMinorSnapshot <= 0 ||
                order.ExpiresAt is not { } expiresAt ||
                expiresAt <= request.RequestedAt)
            {
                return RegistrationPaymentAttemptClaimResult.Failure("not_payable", "Registration order is not payable.");
            }

            if (request.TerminalAttemptId is { } terminalAttemptId)
            {
                (PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)? latest = await attempts.GetLatestByOrderAsync(
                    request.TenantId, request.RegistrationOrderId, token);
                if (latest is null)
                {
                    return RegistrationPaymentAttemptClaimResult.Failure("payment_retry_not_available", "Payment retry is not available.");
                }

                if (latest.Value.Attempt.Id != terminalAttemptId)
                {
                    PaymentAttemptStatusEnum replacementStatus = (PaymentAttemptStatusEnum)latest.Value.Attempt.PaymentAttemptStatusId;
                    bool safelyQueuedReplacement = latest.Value.Attempt.ActiveUniquenessSlot == PaymentAttempt.ActiveUniquenessSlotValue &&
                        ((replacementStatus == PaymentAttemptStatusEnum.Created && latest.Value.DispatchEffect.Status == OutboxMessageStatus.Pending) ||
                         (replacementStatus == PaymentAttemptStatusEnum.DispatchPending && latest.Value.DispatchEffect.Status == OutboxMessageStatus.Failed)) &&
                        latest.Value.Attempt.ProviderCheckoutSessionId is null &&
                        latest.Value.DispatchEffect.ParkedAt is null &&
                        latest.Value.DispatchEffect.LastFailureCode is null;
                    return safelyQueuedReplacement
                        ? new(true, latest.Value.Attempt, latest.Value.DispatchEffect, false, "Registration order already has an active replacement payment attempt.")
                        : RegistrationPaymentAttemptClaimResult.Failure("payment_retry_not_available", "Payment retry is not available.");
                }

                PaymentAttemptStatusEnum terminalStatus = (PaymentAttemptStatusEnum)latest.Value.Attempt.PaymentAttemptStatusId;
                if (terminalStatus is not (PaymentAttemptStatusEnum.Failed or PaymentAttemptStatusEnum.Cancelled))
                {
                    return RegistrationPaymentAttemptClaimResult.Failure("payment_retry_not_available", "Payment retry is not available.");
                }

                if (!await attempts.ReleaseActiveSlotAsync(latest.Value.Attempt, request.RequestedAt, token))
                {
                    return RegistrationPaymentAttemptClaimResult.Failure("payment_retry_not_available", "Payment retry is not available.");
                }
            }

            DateTime maximumCutoff = request.RequestedAt.Add(MaximumProviderCutoff);
            if (expiresAt > maximumCutoff)
            {
                return RegistrationPaymentAttemptClaimResult.Failure("payment_cutoff_unsupported", "Payment cutoff exceeds the provider limit.");
            }

            DateTime paymentCutoff = expiresAt < request.RequestedAt.Add(MinimumProviderCutoff)
                ? request.RequestedAt.Add(MinimumProviderCutoff)
                : expiresAt;
            if (paymentCutoff > expiresAt)
            {
                _ = order.ExtendPaymentCutoff(paymentCutoff, request.RequestedAt);
                IReadOnlyList<RegistrationInventoryHold> activeHolds = await orders.GetActiveHoldsForUpdateAsync(order.Id, order.TenantId, token);
                foreach (RegistrationInventoryHold hold in activeHolds)
                {
                    _ = hold.ExtendPaymentCutoff(paymentCutoff, request.RequestedAt);
                }
            }

            string lockedCompositionRevision = ResolveCompositionRevision(order);
            (PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)? active = await attempts.GetActiveByOrderAsync(
                request.TenantId, request.RegistrationOrderId, token);
            if (active is not null)
            {
                PaymentAttemptStatusEnum activeStatus = (PaymentAttemptStatusEnum)active.Value.Attempt.PaymentAttemptStatusId;
                bool retryingAuthoritativeTerminal = request.TerminalAttemptId == active.Value.Attempt.Id &&
                    activeStatus is PaymentAttemptStatusEnum.Failed or PaymentAttemptStatusEnum.Cancelled;
                if (!retryingAuthoritativeTerminal)
                {
                    return new(true, active.Value.Attempt, active.Value.DispatchEffect, false, "Registration order already has an active payment attempt.");
                }

            }

            (PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)? historical = await attempts.GetByOrderCompositionAsync(
                request.TenantId, request.RegistrationOrderId, lockedCompositionRevision, token);
            if (historical is not null)
            {
                PaymentAttemptStatusEnum historicalStatus = (PaymentAttemptStatusEnum)historical.Value.Attempt.PaymentAttemptStatusId;
                bool retryingAuthoritativeTerminal = request.TerminalAttemptId == historical.Value.Attempt.Id &&
                    historicalStatus is PaymentAttemptStatusEnum.Failed or PaymentAttemptStatusEnum.Cancelled;
                if (!retryingAuthoritativeTerminal)
                {
                    return new(true, historical.Value.Attempt, historical.Value.DispatchEffect, false, "Registration order already has a payment attempt for this composition.");
                }

            }

            (OrganizerPaymentRecipientSnapshot? Snapshot, string? FailureCode, string? Message) readiness =
                await CreateRecipientSnapshotAsync(order, request.RequestedAt, token);
            if (readiness.Snapshot is not { } recipient)
            {
                return RegistrationPaymentAttemptClaimResult.Failure(
                    readiness.FailureCode ?? "payment_readiness_unavailable",
                    readiness.Message ?? "Payment readiness is temporarily unavailable.");
            }
            PaymentProviderDescriptor descriptor = paymentProviderDescriptor.Describe();
            if (!string.Equals(descriptor.ProviderCode, recipient.ProviderCode, StringComparison.Ordinal) ||
                !string.Equals(descriptor.ProfileCode, recipient.ProfileCode, StringComparison.Ordinal))
            {
                return RegistrationPaymentAttemptClaimResult.Failure(
                    "payment_configuration_unavailable",
                    "Payment provider configuration does not match the organizer connection.");
            }

            Guid attemptId = Guid.CreateVersion7();
            PaymentAttempt attempt = PaymentAttempt.Create(
                attemptId,
                order.TenantId,
                order.Id,
                recipient,
                descriptor.ProfileCode,
                descriptor.ApiRevision,
                lockedCompositionRevision,
                order.OrganizerDirectedTotalMinorSnapshot,
                order.PlatformFeeTotalMinorSnapshot,
                order.PlatformContributionTotalMinorSnapshot,
                request.TerminalAttemptId.HasValue
                    ? $"checkout:{attemptId:N}"
                    : CreateIdempotencyKey(order, lockedCompositionRevision),
                request.RequestedAt,
                paymentCutoff);
            CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(attempt, request.RequestedAt);
            RegistrationPaymentAttemptClaimOutcome outcome = await attempts.ClaimAsync(new(attempt, effect), token);
            return new(true, outcome.Attempt, outcome.DispatchEffect, outcome.Created, outcome.Created ? "Payment attempt claimed." : "Registration order already has an active payment attempt.");
        }, cancellationToken);
    }

    private async Task<(OrganizerPaymentRecipientSnapshot? Snapshot, string? FailureCode, string? Message)> CreateRecipientSnapshotAsync(
        RegistrationOrder order,
        DateTime requestedAt,
        CancellationToken cancellationToken)
    {
        Event? eventTarget = await events.GetEventWithDetails(order.EventId);
        if (eventTarget?.TenantId != order.TenantId || eventTarget.OrganizerActorId is not Guid organizerActorId)
        {
            return (null, "payment_organizer_unavailable", "The event organizer is not ready to accept payment.");
        }

        PaidEventPolicyVersion? instancePolicy = await policies.GetActiveInstanceAsync(cancellationToken);
        PaidEventPolicyVersion? tenantPolicy = await policies.GetActiveTenantAsync(order.TenantId, cancellationToken);
        if (instancePolicy is null || string.IsNullOrWhiteSpace(commerceConfiguration.ProviderCode) || string.IsNullOrWhiteSpace(commerceConfiguration.ConnectPlatformId))
        {
            return (null, "payment_configuration_unavailable", "Payment policy or platform configuration is unavailable.");
        }

        OrganizerPaymentProviderConnection? connection = await connections.GetActiveByScopeAsync(
            order.TenantId,
            organizerActorId,
            commerceConfiguration.ProviderCode,
            commerceConfiguration.ConnectPlatformId,
            cancellationToken);
        if (connection is null)
        {
            return (null, "payment_connection_unavailable", "The organizer payment connection is not ready.");
        }

        OrganizerPaymentRecipientSnapshotResult result = connection.TryCreateRecipientSnapshot(
            order.CurrencyCode,
            instancePolicy.Id,
            tenantPolicy?.Id,
            requestedAt,
            MaximumReadinessAge);
        return result.Success
            ? (result.Snapshot, null, null)
            : (null, result.FailureCode ?? "payment_readiness_unavailable", "The organizer payment connection is not ready.");
    }

    private static string ResolveCompositionRevision(RegistrationOrder order) => order.ConcurrencyStamp.ToString("N");

    private static string CreateIdempotencyKey(RegistrationOrder order, string compositionRevision) =>
        $"checkout:{order.TenantId:N}:{order.Id:N}:{compositionRevision}";
}

file sealed class RegistrationPaymentAttemptClaimRequestValidator
{
    public string? Validate(RegistrationPaymentAttemptClaimRequest request)
    {
        if (request.TenantId == Guid.Empty || request.RegistrationOrderId == Guid.Empty)
        {
            return "Tenant and registration order identities are required.";
        }

        return request.RequestedAt == default || request.RequestedAt.Kind != DateTimeKind.Utc
            ? "Request timestamp must be UTC."
            : null;
    }
}
