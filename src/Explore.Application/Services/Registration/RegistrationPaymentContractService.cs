// ABOUTME: Builds the authoritative registration-payment contract from durable order, attempt, and dispatch state.
// ABOUTME: Starts and retries only local durable work; provider retrieval is isolated to checkout navigation resolution.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Payments;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Services.Registration;

public sealed class RegistrationPaymentContractService(
    IRegistrationPaymentAttemptRepository attempts,
    IRegistrationFinalizationRepository finalization,
    RegistrationPaymentAttemptClaimService claims,
    IHostedCheckoutSessionRetriever checkoutRetriever,
    TimeProvider timeProvider)
{
    public async Task<RegistrationPaymentCommandResultDto> StartAsync(RegistrationOrder order, CancellationToken cancellationToken)
    {
        if (!IsCurrentlyPayable(order, timeProvider.GetUtcNow().UtcDateTime))
        {
            return Failure("not_payable", "Registration order is not payable.");
        }

        RegistrationPaymentAttemptClaimResult result = await claims.ClaimAsync(
            new(order.TenantId, order.Id, timeProvider.GetUtcNow().UtcDateTime), cancellationToken);
        if (!result.Success || result.Attempt is null || result.DispatchEffect is null)
        {
            return Failure(result.FailureCode ?? "payment_start_failed", result.Message);
        }

        return Success(Map(order, result.Attempt, result.DispatchEffect));
    }

    public async Task<RegistrationPaymentDto?> GetAsync(RegistrationOrder order, CancellationToken cancellationToken)
    {
        (PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)? row = await attempts.GetLatestByOrderAsync(
            order.TenantId, order.Id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        SucceededPaymentLookupResult succeeded = await finalization.GetSucceededPaymentAsync(
            order.TenantId, order.Id, cancellationToken);
        bool conflict = succeeded.Status == SucceededPaymentLookupStatus.Conflict;
        return Map(
            order,
            row.Value.Attempt,
            row.Value.DispatchEffect,
            failureCode: conflict ? SucceededPaymentLookupResult.DuplicateCode : null,
            needsReconciliation: conflict);
    }

    public async Task<RegistrationPaymentCommandResultDto> RetryAsync(RegistrationOrder order, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (!IsCurrentlyPayable(order, now))
        {
            return Failure("payment_retry_not_available", "Payment retry is not available.");
        }

        (PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)? row = await attempts.GetLatestByOrderAsync(
            order.TenantId, order.Id, cancellationToken);
        if (row is null)
        {
            return Failure("payment_retry_not_available", "Payment retry is not available.");
        }

        if (IsAlreadyQueuedAfterUserRetry(row.Value.Attempt, row.Value.DispatchEffect))
        {
            return Success(Map(order, row.Value.Attempt, row.Value.DispatchEffect));
        }

        PaymentAttemptStatusEnum status = (PaymentAttemptStatusEnum)row.Value.Attempt.PaymentAttemptStatusId;
        if (status is PaymentAttemptStatusEnum.Failed or PaymentAttemptStatusEnum.Cancelled)
        {
            RegistrationPaymentAttemptClaimResult terminalRetry = await claims.ClaimAsync(
                new(order.TenantId, order.Id, now, row.Value.Attempt.Id), cancellationToken);
            return terminalRetry.Success && terminalRetry.Attempt is not null && terminalRetry.DispatchEffect is not null
                ? Success(Map(order, terminalRetry.Attempt, terminalRetry.DispatchEffect))
                : Failure(terminalRetry.FailureCode ?? "payment_retry_not_available", terminalRetry.Message);
        }

        if (!CanRetry(order, row.Value.Attempt, row.Value.DispatchEffect, now))
        {
            return Failure("payment_retry_not_available", "Payment retry is not available.");
        }

        DateTime requestedAt = now;
        if (!await attempts.RetryParkedPreHandoffAsync(order.TenantId, row.Value.Attempt.Id, requestedAt, cancellationToken))
        {
            return Failure("payment_retry_not_available", "Payment retry is not available.");
        }

        (PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)? refreshed = await attempts.GetLatestByOrderAsync(
            order.TenantId, order.Id, cancellationToken);
        return refreshed is null
            ? Failure("payment_retry_not_available", "Payment retry is not available.")
            : Success(Map(order, refreshed.Value.Attempt, refreshed.Value.DispatchEffect));
    }

    public async Task<RegistrationPaymentCheckoutTargetDto?> ResolveCheckoutTargetAsync(
        RegistrationOrder order,
        CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        if (!IsCurrentlyPayable(order, now))
        {
            return null;
        }

        (PaymentAttempt Attempt, CheckoutDispatchEffect DispatchEffect)? row = await attempts.GetLatestByOrderAsync(
            order.TenantId, order.Id, cancellationToken);
        PaymentAttempt attempt = row?.Attempt!;
        if (attempt is null || (PaymentAttemptStatusEnum)attempt.PaymentAttemptStatusId != PaymentAttemptStatusEnum.RequiresAction ||
            attempt.ProviderCheckoutSessionId is not { } sessionId)
        {
            return null;
        }

        HostedCheckoutRetrieveResult result = await checkoutRetriever.RetrieveAsync(
            HostedCheckoutRetrieveRequest.Create(attempt.ProviderCode, attempt.RecipientSnapshot.ExternalAccountId, sessionId),
            cancellationToken);
        if (result is not { Outcome: HostedCheckoutOperationOutcome.Succeeded, Session: { } session } ||
            !string.Equals(session.SessionId, sessionId, StringComparison.Ordinal) ||
            session.Status != HostedCheckoutSessionStatus.Open ||
            session.PaymentStatus != HostedCheckoutPaymentStatus.Unpaid ||
            session.ExpiresAt is not { } expiresAt || expiresAt <= now ||
            session.AmountTotalMinor != attempt.TotalMinor ||
            !string.Equals(session.CurrencyCode, attempt.CurrencyCode, StringComparison.OrdinalIgnoreCase) ||
            session.HostedUrl is not { IsAbsoluteUri: true } url)
        {
            return null;
        }

        return new RegistrationPaymentCheckoutTargetDto { Url = url.AbsoluteUri };
    }

    private RegistrationPaymentDto Map(
        RegistrationOrder order,
        PaymentAttempt attempt,
        CheckoutDispatchEffect dispatch,
        bool? retryAvailable = null,
        string? failureCode = null,
        bool needsReconciliation = false)
    {
        string statusCode = needsReconciliation ? "NeedsReconciliation" : StatusCode(order, attempt, dispatch);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        bool currentlyPayable = IsCurrentlyPayable(order, now);
        return new RegistrationPaymentDto
        {
            Id = attempt.Id,
            RegistrationOrderId = order.Id,
            StatusCode = statusCode,
            StatusName = statusCode switch
            {
                "RequiresAction" => "Requires action",
                "NeedsReconciliation" => "Needs reconciliation",
                _ => statusCode
            },
            HostedRedirectAvailable = currentlyPayable && statusCode == "RequiresAction" && attempt.ProviderCheckoutSessionId is not null,
            RetryAvailable = currentlyPayable && (retryAvailable ?? CanRetry(order, attempt, dispatch, now)),
            FailureCode = failureCode ?? (statusCode == "Failed" ? dispatch.LastFailureCode : null),
            CreatedAt = attempt.CreatedAt,
            LastUpdatedAt = attempt.UpdatedAt ?? attempt.LastStatusObservedAt,
            ExpiresAt = attempt.ExpiresAt
        };
    }

    private static string StatusCode(RegistrationOrder order, PaymentAttempt attempt, CheckoutDispatchEffect dispatch)
    {
        if ((RegistrationOrderStatusEnum)order.RegistrationOrderStatusId == RegistrationOrderStatusEnum.NeedsReconciliation)
        {
            return "NeedsReconciliation";
        }

        return (PaymentAttemptStatusEnum)attempt.PaymentAttemptStatusId switch
        {
            PaymentAttemptStatusEnum.Created => "Created",
            PaymentAttemptStatusEnum.DispatchPending when dispatch.Status == OutboxMessageStatus.DeadLettered => "Failed",
            PaymentAttemptStatusEnum.DispatchPending => "Processing",
            PaymentAttemptStatusEnum.RequiresAction => "RequiresAction",
            PaymentAttemptStatusEnum.Processing => "Processing",
            PaymentAttemptStatusEnum.Unknown => "Unknown",
            PaymentAttemptStatusEnum.Failed => "Failed",
            PaymentAttemptStatusEnum.Cancelled => "Cancelled",
            PaymentAttemptStatusEnum.Succeeded => "Succeeded",
            _ => "Unknown"
        };
    }

    private static bool CanRetry(RegistrationOrder order, PaymentAttempt attempt, CheckoutDispatchEffect dispatch, DateTime now) =>
        IsCurrentlyPayable(order, now) &&
        ((PaymentAttemptStatusEnum)attempt.PaymentAttemptStatusId is PaymentAttemptStatusEnum.Failed or PaymentAttemptStatusEnum.Cancelled ||
         ((PaymentAttemptStatusEnum)attempt.PaymentAttemptStatusId == PaymentAttemptStatusEnum.DispatchPending &&
          attempt.ProviderCheckoutSessionId is null && dispatch.Status == OutboxMessageStatus.DeadLettered));

    private static bool IsAlreadyQueuedAfterUserRetry(PaymentAttempt attempt, CheckoutDispatchEffect dispatch) =>
        (((PaymentAttemptStatusEnum)attempt.PaymentAttemptStatusId == PaymentAttemptStatusEnum.Created && dispatch.Status == OutboxMessageStatus.Pending) ||
         ((PaymentAttemptStatusEnum)attempt.PaymentAttemptStatusId == PaymentAttemptStatusEnum.DispatchPending && dispatch.Status == OutboxMessageStatus.Failed)) &&
        attempt.ProviderCheckoutSessionId is null &&
        dispatch.ParkedAt is null &&
        dispatch.LastFailureCode is null;

    private static bool IsCurrentlyPayable(RegistrationOrder order, DateTime now) =>
        RegistrationPaymentPayability.IsCurrentlyPayable(
            order.RegistrationOrderStatusId,
            order.TotalDueMinorSnapshot,
            order.ExpiresAt,
            now);

    private static RegistrationPaymentCommandResultDto Success(RegistrationPaymentDto payment) =>
        new() { Success = true, Payment = payment };

    private static RegistrationPaymentCommandResultDto Failure(string code, string? message) =>
        new() { FailureCode = code, Message = message };
}
