// ABOUTME: Handles administrative restoration for reversibly light-moderated events.
// ABOUTME: Preserves moderation audit history while returning eligible events to Published.

using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Events.Handlers.Commands;

public sealed class UnmoderateEventCommandHandler(
    IEventRepository eventRepository,
    IEventModerationRecordRepository moderationRecordRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    HybridCache cache,
    BusinessMetrics metrics,
    ILogger<UnmoderateEventCommandHandler> logger,
    AtprotoEventPublicationPlanner atprotoPublicationPlanner) : IRequestHandler<UnmoderateEventCommand, BaseCommandResponse<Guid>>
{
    private const string InvalidStatusFailureCode = "event_unmoderation_invalid_status";
    private const string NotReversibleFailureCode = "event_unmoderation_not_reversible";
    private const string UserResolutionFailureCode = "event_unmoderation_user_unresolved";
    private const string ActionKind = "unmoderated";

    public async Task<BaseCommandResponse<Guid>> Handle(UnmoderateEventCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } moderatorUserId)
        {
            metrics.RecordEventModerationAction(null, ActionKind, "failed", "user_unresolved", irreversible: false);
            logger.LogWarning(
                "Event unmoderation rejected because the authenticated moderator user could not be resolved for event {EventId}.",
                request.Id);

            return Failure(
                request.Id,
                "Moderator user could not be resolved.",
                ["Authenticated moderator user id is required."],
                UserResolutionFailureCode);
        }

        Guid federationOutboxId = Guid.CreateVersion7();
        DateTime federationCreatedAt = DateTime.UtcNow;
        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var @event = await eventRepository.GetById(request.Id);
            if (@event is null)
            {
                metrics.RecordEventModerationAction(null, ActionKind, "failed", "not_found", irreversible: false);
                logger.LogWarning(
                    "Event unmoderation rejected because event {EventId} was not found.",
                    request.Id);

                return Failure(request.Id, "Event not found.", ["Event not found."]);
            }

            if (@event.EventStatusId != (int)EventStatusEnum.Moderated)
            {
                metrics.RecordEventModerationAction(@event.TenantId.ToString(), ActionKind, "failed", "invalid_status", irreversible: false);
                logger.LogWarning(
                    "Event unmoderation rejected for event {EventId} in tenant {TenantId} because current status {CurrentStatusId} is not Moderated.",
                    @event.Id,
                    @event.TenantId,
                    @event.EventStatusId);

                return Failure(
                    @event.Id,
                    "Only moderated events can be unmoderated.",
                    ["Only moderated events can be unmoderated."],
                    InvalidStatusFailureCode);
            }

            var latestModerationRecord = await moderationRecordRepository.GetLatestByEventAsync(
                @event.TenantId,
                @event.Id,
                token);

            if (latestModerationRecord?.AllowsUnmoderation != true)
            {
                metrics.RecordEventModerationAction(@event.TenantId.ToString(), ActionKind, "failed", "not_reversible", irreversible: false);
                logger.LogWarning(
                    "Event unmoderation rejected for event {EventId} in tenant {TenantId} because the latest moderation record is not reversible.",
                    @event.Id,
                    @event.TenantId);

                return Failure(
                    @event.Id,
                    "Only reversibly light-moderated events can be unmoderated.",
                    ["Only reversibly light-moderated events can be unmoderated."],
                    NotReversibleFailureCode);
            }

            var unmoderatedAt = DateTimeOffset.UtcNow;
            var unmoderationRecord = EventModerationRecord.CreateUnmoderation(
                latestModerationRecord,
                moderatorUserId,
                request.ReasonCode,
                request.CorrelationId,
                unmoderatedAt);

            @event.EventStatusId = (int)EventStatusEnum.Published;
            @event.UpdatedAt = unmoderatedAt.UtcDateTime;

            await moderationRecordRepository.Create(unmoderationRecord);
            await eventRepository.Update(@event);
            await atprotoPublicationPlanner.PlanEventAsync(
                new AtprotoEventPublicationInput(
                    @event.TenantId,
                    moderatorUserId,
                    @event.Id,
                    @event.ConcurrencyStamp,
                    PdsSyncOperation.Create,
                    federationOutboxId,
                    federationCreatedAt,
                    RestoreOnly: true),
                token);

            await cache.RemoveAsync($"event:detail:{@event.Id}", token);
            await cache.RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), token);

            metrics.RecordEventModerationAction(@event.TenantId.ToString(), ActionKind, "succeeded", irreversible: false);
            logger.LogInformation(
                "Event unmoderation succeeded for event {EventId} in tenant {TenantId}; moderation record {ModerationRecordId}, source record {SourceModerationRecordId}, moderator {ModeratorUserId}, reason {ReasonCode}, correlation {CorrelationId}.",
                @event.Id,
                @event.TenantId,
                unmoderationRecord.Id,
                unmoderationRecord.SourceModerationRecordId,
                moderatorUserId,
                unmoderationRecord.ReasonCode,
                unmoderationRecord.CorrelationId);

            return Success(@event.Id, "Event unmoderated successfully.");
        }, cancellationToken);
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
