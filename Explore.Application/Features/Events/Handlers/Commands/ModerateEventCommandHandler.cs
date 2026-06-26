// ABOUTME: Handles reversible light moderation by hiding published events without editing content.
// ABOUTME: Writes safe moderation history, attendee notification outbox work, and cache invalidations atomically.

using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
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

public sealed class ModerateEventCommandHandler(
    IEventRepository eventRepository,
    IEventSessionRepository eventSessionRepository,
    IEventModerationRecordRepository moderationRecordRepository,
    IOutboxRepository outboxRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    HybridCache cache,
    BusinessMetrics metrics,
    ILogger<ModerateEventCommandHandler> logger) : IRequestHandler<ModerateEventCommand, BaseCommandResponse<Guid>>
{
    private const string InvalidStatusFailureCode = "event_light_moderation_invalid_status";
    private const string UserResolutionFailureCode = "event_light_moderation_user_unresolved";
    private const string ActionKind = "light_moderated";

    public async Task<BaseCommandResponse<Guid>> Handle(ModerateEventCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is not { } moderatorUserId)
        {
            metrics.RecordEventModerationAction(null, ActionKind, "failed", "user_unresolved", irreversible: false);
            logger.LogWarning(
                "Light event moderation rejected because the authenticated moderator user could not be resolved for event {EventId}.",
                request.Id);

            return Failure(
                request.Id,
                "Moderator user could not be resolved.",
                ["Authenticated moderator user id is required."],
                UserResolutionFailureCode);
        }

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var @event = await eventRepository.GetById(request.Id);
            if (@event is null)
            {
                metrics.RecordEventModerationAction(null, ActionKind, "failed", "not_found", irreversible: false);
                logger.LogWarning(
                    "Light event moderation rejected because event {EventId} was not found.",
                    request.Id);

                return Failure(request.Id, "Event not found.", ["Event not found."]);
            }

            if (@event.EventStatusId == (int)EventStatusEnum.Moderated)
            {
                var repairedSessionCount = await CascadeModerationToSessionsAsync(@event.Id, DateTime.UtcNow);
                if (repairedSessionCount > 0)
                {
                    await cache.RemoveAsync($"event:detail:{@event.Id}", token);
                    await cache.RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), token);
                }

                metrics.RecordEventModerationAction(@event.TenantId.ToString(), ActionKind, "idempotent", irreversible: false);
                logger.LogInformation(
                    "Light event moderation skipped because event {EventId} in tenant {TenantId} is already moderated.",
                    @event.Id,
                    @event.TenantId);

                return Success(@event.Id, "Event is already moderated.");
            }

            if (@event.EventStatusId != (int)EventStatusEnum.Published)
            {
                metrics.RecordEventModerationAction(@event.TenantId.ToString(), ActionKind, "failed", "invalid_status", irreversible: false);
                logger.LogWarning(
                    "Light event moderation rejected for event {EventId} in tenant {TenantId} because current status {CurrentStatusId} is not Published.",
                    @event.Id,
                    @event.TenantId,
                    @event.EventStatusId);

                return Failure(
                    @event.Id,
                    "Only published events can be light moderated.",
                    ["Only published events can be light moderated."],
                    InvalidStatusFailureCode);
            }

            var moderatedAt = DateTimeOffset.UtcNow;
            var moderationRecord = EventModerationRecord.CreateLightModeration(
                @event.TenantId,
                @event.Id,
                moderatorUserId,
                request.ReasonCode,
                @event.EventStatusId,
                request.CorrelationId,
                moderatedAt);

            @event.EventStatusId = (int)EventStatusEnum.Moderated;
            @event.UpdatedAt = moderatedAt.UtcDateTime;

            await CascadeModerationToSessionsAsync(@event.Id, moderatedAt.UtcDateTime);
            await moderationRecordRepository.Create(moderationRecord);
            await eventRepository.Update(@event);
            await outboxRepository.Create(EventModerationOutboxMessageFactory.CreateLightModerationNotificationFanoutMessage(@event, moderationRecord));

            await cache.RemoveAsync($"event:detail:{@event.Id}", token);
            await cache.RemoveByTagAsync(CacheTags.EventListByTenant(@event.TenantId), token);

            metrics.RecordEventModerationAction(@event.TenantId.ToString(), ActionKind, "succeeded", irreversible: false);
            logger.LogInformation(
                "Light event moderation succeeded for event {EventId} in tenant {TenantId}; moderation record {ModerationRecordId}, moderator {ModeratorUserId}, reason {ReasonCode}, correlation {CorrelationId}.",
                @event.Id,
                @event.TenantId,
                moderationRecord.Id,
                moderatorUserId,
                moderationRecord.ReasonCode,
                moderationRecord.CorrelationId);

            return Success(@event.Id, "Event moderated successfully.");
        }, cancellationToken);
    }

    private async Task<int> CascadeModerationToSessionsAsync(Guid eventId, DateTime moderatedAtUtc)
    {
        var sessions = await eventSessionRepository.GetSessionsByEvent(eventId);
        var updatedCount = 0;

        foreach (var session in sessions.Where(session =>
                     session.EventSessionStatusId != (int)EventSessionStatusEnum.Moderated))
        {
            session.EventSessionStatusId = (int)EventSessionStatusEnum.Moderated;
            session.UpdatedAt = moderatedAtUtc;
            await eventSessionRepository.Update(session);
            updatedCount++;
        }

        return updatedCount;
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
