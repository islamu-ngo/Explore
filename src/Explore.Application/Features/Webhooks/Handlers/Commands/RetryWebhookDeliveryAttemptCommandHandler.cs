// ABOUTME: Handles manual webhook delivery retry scheduling commands.
// ABOUTME: Converts drain-service retry outcomes into safe command responses for API callers.

using Explore.Application.Contracts.Services;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Commands;

public sealed class RetryWebhookDeliveryAttemptCommandHandler(IWebhookDeliveryDrainService deliveryDrainService)
    : IRequestHandler<RetryWebhookDeliveryAttemptCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        RetryWebhookDeliveryAttemptCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TenantId == Guid.Empty || request.AttemptId == Guid.Empty)
        {
            return Failure(
                request.AttemptId,
                "webhook_delivery_retry_validation_failed",
                "A tenant id and delivery attempt id are required.");
        }

        var result = await deliveryDrainService.ScheduleManualRetryAsync(
            request.TenantId,
            request.AttemptId,
            cancellationToken);

        return result.Outcome switch
        {
            WebhookDeliveryDrainOutcome.RetryScheduled => new BaseCommandResponse<Guid>
            {
                Id = result.AttemptId ?? request.AttemptId,
                Success = true,
                Message = "Webhook delivery retry scheduled."
            },
            WebhookDeliveryDrainOutcome.Missing => Failure(
                request.AttemptId,
                "webhook_delivery_attempt_not_found",
                "Webhook delivery attempt was not found."),
            WebhookDeliveryDrainOutcome.Skipped => Failure(
                request.AttemptId,
                "webhook_delivery_retry_skipped",
                "Webhook delivery retry could not be scheduled for the current provider or endpoint state."),
            WebhookDeliveryDrainOutcome.Deferred => Failure(
                request.AttemptId,
                "webhook_delivery_retry_deferred",
                "A scheduled or active delivery attempt already exists for this message and endpoint."),
            WebhookDeliveryDrainOutcome.AlreadyClaimed => Failure(
                request.AttemptId,
                "webhook_delivery_attempt_active",
                "Webhook delivery attempt is currently being processed."),
            WebhookDeliveryDrainOutcome.AlreadySettled => Failure(
                request.AttemptId,
                "webhook_delivery_attempt_not_retryable",
                "Webhook delivery attempt is already settled and cannot be manually retried."),
            _ => Failure(
                request.AttemptId,
                "webhook_delivery_retry_failed",
                "Webhook delivery retry could not be scheduled.")
        };
    }

    private static BaseCommandResponse<Guid> Failure(Guid attemptId, string code, string message) =>
        new()
        {
            Id = attemptId,
            Success = false,
            Message = message,
            FailureCode = code,
            Errors = [message]
        };
}
