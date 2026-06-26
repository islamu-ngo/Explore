// ABOUTME: Handles irreversible heavy moderation by redacting event-owned content and triggering image deletion.
// ABOUTME: Writes safe moderation history and cache invalidation inside the UnitOfWork transaction.

using Explore.Application.Authorization;
using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.Events.Moderation;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Events.Handlers.Commands;

public sealed class HeavyRedactEventCommandHandler(
    IEventHeavyRedactionRepository redactionRepository,
    IEventModerationRecordRepository moderationRecordRepository,
    IOutboxRepository outboxRepository,
    IStorageObjectDeletionService storageObjectDeletionService,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    HybridCache cache,
    BusinessMetrics metrics,
    ILogger<HeavyRedactEventCommandHandler> logger) : IRequestHandler<HeavyRedactEventCommand, BaseCommandResponse<Guid>>
{
    private const int ImmediateDeletionBatchSize = 100;
    private const string ActionKind = "heavy_redacted";

    public async Task<BaseCommandResponse<Guid>> Handle(HeavyRedactEventCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } moderatorUserId)
        {
            metrics.RecordEventModerationAction(null, ActionKind, "failed", "user_unresolved", irreversible: true);
            logger.LogWarning(
                "Heavy event moderation rejected because the authenticated moderator user could not be resolved for event {EventId}.",
                request.Id);

            return Failure(
                request.Id,
                "Moderator user could not be resolved.",
                ["Authenticated moderator user id is required."],
                HeavyRedactEventCommand.UserResolutionFailureCode);
        }

        var tenantId = Guid.Empty;
        var eventId = Guid.Empty;
        var wasIdempotent = false;
        EventModerationRecord? moderationRecordForLog = null;
        var transactionResponse = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var graph = await redactionRepository.GetForUpdateAsync(request.Id, token);
            if (graph is null)
            {
                metrics.RecordEventModerationAction(null, ActionKind, "failed", "not_found", irreversible: true);
                logger.LogWarning(
                    "Heavy event moderation rejected because event {EventId} was not found.",
                    request.Id);

                return Failure(request.Id, "Event not found.", ["Event not found."]);
            }

            var @event = graph.Event;
            tenantId = @event.TenantId;
            eventId = @event.Id;
            var latestRecord = await moderationRecordRepository.GetLatestByEventAsync(
                @event.TenantId,
                @event.Id,
                token);

            if (latestRecord?.ActionKind == EventModerationActionKind.HeavyRedacted)
            {
                wasIdempotent = true;
                logger.LogInformation(
                    "Heavy event moderation skipped because event {EventId} in tenant {TenantId} is already heavy-redacted; delete-requested image cleanup will still be checked.",
                    @event.Id,
                    @event.TenantId);

                return Success(@event.Id, "Event is already heavy-redacted.");
            }

            var redactedAt = DateTimeOffset.UtcNow;
            var moderationRecord = EventModerationRecord.CreateHeavyRedaction(
                @event.TenantId,
                @event.Id,
                moderatorUserId,
                request.ReasonCode,
                @event.EventStatusId,
                request.CorrelationId,
                redactedAt);

            EventHeavyRedactionApplicator.Apply(graph, moderatorUserId, redactedAt);

            await redactionRepository.SaveChangesAsync(token);
            await moderationRecordRepository.Create(moderationRecord);
            await outboxRepository.Create(EventModerationOutboxMessageFactory.CreateHeavyRedactionNotificationFanoutMessage(@event, moderationRecord));
            moderationRecordForLog = moderationRecord;

            await cache.RemoveAsync($"event:detail:{@event.Id}", token);
            await cache.RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), token);

            return Success(@event.Id, "Event heavy-redacted successfully.");
        }, cancellationToken);

        if (!transactionResponse.Success)
        {
            return transactionResponse;
        }

        var deletionResult = await storageObjectDeletionService.DeleteRequestedForResourceAsync(
            tenantId,
            ResourceKinds.Event,
            eventId,
            moderatorUserId,
            ImmediateDeletionBatchSize,
            cancellationToken);

        if (!deletionResult.CompletedWithoutFailures)
        {
            metrics.RecordEventModerationAction(tenantId.ToString(), ActionKind, "pending_storage_deletion", "storage_deletion_pending", irreversible: true);
            logger.LogWarning(
                "Heavy event moderation completed with pending image deletion for event {EventId} in tenant {TenantId}; scanned {ScannedCount}, deleted {DeletedCount}, missing-key deleted {MissingKeyDeletedCount}, failed {FailedCount}.",
                eventId,
                tenantId,
                deletionResult.ScannedCount,
                deletionResult.DeletedCount,
                deletionResult.MissingKeyDeletedCount,
                deletionResult.FailedCount);

            return Failure(
                eventId,
                "Event heavy-redacted; image deletion is pending retry.",
                ["One or more image objects could not be deleted immediately and remain queued for retry."],
                HeavyRedactEventCommand.StorageDeletionPendingFailureCode);
        }

        metrics.RecordEventModerationAction(
            tenantId.ToString(),
            ActionKind,
            wasIdempotent ? "idempotent" : "succeeded",
            irreversible: true);
        logger.LogInformation(
            "Heavy event moderation {Outcome} for event {EventId} in tenant {TenantId}; moderation record {ModerationRecordId}, moderator {ModeratorUserId}, reason {ReasonCode}, correlation {CorrelationId}, scanned {ScannedCount}, deleted {DeletedCount}, missing-key deleted {MissingKeyDeletedCount}.",
            wasIdempotent ? "idempotent" : "succeeded",
            eventId,
            tenantId,
            moderationRecordForLog?.Id,
            moderatorUserId,
            moderationRecordForLog?.ReasonCode ?? request.ReasonCode,
            moderationRecordForLog?.CorrelationId ?? request.CorrelationId,
            deletionResult.ScannedCount,
            deletionResult.DeletedCount,
            deletionResult.MissingKeyDeletedCount);

        return transactionResponse;
    }

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    private static BaseCommandResponse<Guid> Failure(
        Guid id,
        string message,
        IEnumerable<string> errors,
        string? failureCode = null) => new()
        {
            Success = false,
            Id = id,
            Message = message,
            Errors = errors.ToList(),
            FailureCode = failureCode
        };

}
