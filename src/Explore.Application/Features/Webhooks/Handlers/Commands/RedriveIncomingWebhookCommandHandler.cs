// ABOUTME: Atomically redrives a dead-lettered incoming webhook and records operator audit evidence.
// ABOUTME: Rejects stale generations, wrong-tenant identities, active work, and unauthenticated actors.

using System.Text.Json;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Webhooks.Requests.Commands;
using Explore.Application.Features.Webhooks.Validators;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.Webhooks.Handlers.Commands;

public sealed class RedriveIncomingWebhookCommandHandler(
    IIncomingWebhookMessageRepository messageRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IMachinePrincipalAccessor machinePrincipalAccessor,
    TimeProvider timeProvider)
    : IRequestHandler<RedriveIncomingWebhookCommand, BaseCommandResponse<Guid>>
{
    private const string AuditAction = "IncomingWebhookRedriven";

    public async Task<BaseCommandResponse<Guid>> Handle(
        RedriveIncomingWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var validator = new RedriveIncomingWebhookCommandValidator();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Failure(
                request.IncomingWebhookMessageId,
                "incoming_webhook_redrive_validation_failed",
                "Incoming webhook redrive request failed validation.",
                validation.Errors.Select(error => error.ErrorMessage));
        }

        if (!TryResolveActor(out var actorReference, out var actorUserId))
        {
            return Failure(
                request.IncomingWebhookMessageId,
                "incoming_webhook_redrive_actor_required",
                "An authenticated operator identity is required.");
        }

        var requestedAt = timeProvider.GetUtcNow().UtcDateTime;
        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var message = await messageRepository.GetByTenantAndIdForUpdateAsync(
                request.TenantId,
                request.IncomingWebhookMessageId,
                token);
            if (message is null)
            {
                return Failure(
                    request.IncomingWebhookMessageId,
                    "incoming_webhook_not_found",
                    "Incoming webhook was not found.");
            }

            if (message.ProcessingGeneration != request.ExpectedProcessingGeneration)
            {
                return Failure(
                    message.Id,
                    "incoming_webhook_redrive_generation_conflict",
                    "Incoming webhook processing generation changed before redrive.");
            }

            if (message.Status == IncomingWebhookMessageStatus.Processing)
            {
                return Failure(
                    message.Id,
                    "incoming_webhook_redrive_active_lease",
                    "An actively processing incoming webhook cannot be redriven.");
            }

            if (message.Status != IncomingWebhookMessageStatus.DeadLettered)
            {
                return Failure(
                    message.Id,
                    "incoming_webhook_redrive_not_eligible",
                    "Only dead-lettered incoming webhooks can be redriven.");
            }

            var sourceGeneration = message.ProcessingGeneration;
            var record = message.Redrive(
                request.ExpectedProcessingGeneration,
                actorReference,
                request.Reason,
                requestedAt);
            messageRepository.TrackAppendedEvidence(message);
            await messageRepository.SaveChangesAsync(token);

            token.ThrowIfCancellationRequested();
            await auditLogRepository.Create(new AuditLog
            {
                Id = Guid.CreateVersion7(),
                TenantId = message.TenantId,
                Tenant = null!,
                EntityType = nameof(IncomingWebhookMessage),
                EntityId = message.Id.ToString("D"),
                Action = AuditAction,
                OldValues = JsonSerializer.Serialize(new
                {
                    Status = IncomingWebhookMessageStatus.DeadLettered.ToString(),
                    ProcessingGeneration = sourceGeneration
                }),
                NewValues = JsonSerializer.Serialize(new
                {
                    Status = message.Status.ToString(),
                    message.ProcessingGeneration,
                    RedriveRecordId = record.Id,
                    Result = record.Result.ToString()
                }),
                AffectedColumns = JsonSerializer.Serialize(new[]
                {
                    nameof(IncomingWebhookMessage.StatusId),
                    nameof(IncomingWebhookMessage.ProcessingGeneration),
                    nameof(IncomingWebhookMessage.NextAttemptAt)
                }),
                ActorId = actorUserId,
                Timestamp = requestedAt
            });

            return Success(message.Id, "Incoming webhook redrive scheduled.");
        }, cancellationToken);
    }

    private bool TryResolveActor(out string actorReference, out Guid? actorUserId)
    {
        if (currentUserService.UserId is { } userId)
        {
            actorReference = $"user:{userId:D}";
            actorUserId = userId;
            return true;
        }

        if (machinePrincipalAccessor.Current is { } machine)
        {
            actorReference = $"machine:{machine.OwnerType}:{machine.OwnerId:D}";
            actorUserId = null;
            return true;
        }

        actorReference = string.Empty;
        actorUserId = null;
        return false;
    }

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Id = id,
        Success = true,
        Message = message
    };

    private static BaseCommandResponse<Guid> Failure(
        Guid id,
        string code,
        string message,
        IEnumerable<string>? errors = null) => new()
        {
            Id = id,
            Success = false,
            FailureCode = code,
            Message = message,
            Errors = errors?.ToList() ?? [message]
        };
}
