// ABOUTME: Shared handler base for explicit event-session terminal lifecycle transitions.
// ABOUTME: Centralizes concurrency, parent-event checks, persistence, schedule refresh, and cache invalidation.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public abstract class EventSessionLifecycleTransitionCommandHandlerBase<TCommand>(
    IEventSessionRepository eventSessionRepository,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    HybridCache cache) : IRequestHandler<TCommand, BaseCommandResponse<Guid>>
    where TCommand : IEventSessionLifecycleTransitionCommand
{
    protected abstract EventSessionStatusEnum TargetStatus { get; }
    protected abstract string ActionName { get; }
    protected abstract string PastTenseActionName { get; }
    protected abstract string ConcurrencyFailureCode { get; }
    protected abstract string AlreadyInTargetStatusFailureCode { get; }
    protected abstract string InvalidStatusFailureCode { get; }

    public async Task<BaseCommandResponse<Guid>> Handle(TCommand request, CancellationToken cancellationToken)
    {
        var validator = new EventSessionLifecycleRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Failure(
                request.Id,
                $"Event session {ActionName} request is invalid.",
                validationResult.Errors.Select(error => error.ErrorMessage));
        }

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var session = await eventSessionRepository.GetById(request.Id);
            if (session is null)
            {
                return Failure(request.Id, "Event session was not found.", ["Event session was not found."]);
            }

            if (session.ConcurrencyStamp != request.Request.ExpectedConcurrencyStamp)
            {
                return Failure(
                    request.Id,
                    "Event session was modified by another request.",
                    ["Refresh the event session and try again."],
                    ConcurrencyFailureCode);
            }

            var parentEvent = await eventRepository.GetById(session.EventId);
            if (parentEvent is null || parentEvent.TenantId != session.TenantId)
            {
                return Failure(
                    request.Id,
                    "Parent event was not found in the current tenant.",
                    ["Parent event was not found in the current tenant."]);
            }

            if (session.EventSessionStatusId == (int)TargetStatus)
            {
                return Failure(
                    session.Id,
                    $"Event session is already {PastTenseActionName}.",
                    [$"The event session is already {PastTenseActionName}."],
                    AlreadyInTargetStatusFailureCode);
            }

            if (!CanTransition(session.EventSessionStatusId, parentEvent.EventStatusId))
            {
                return Failure(
                    session.Id,
                    $"Event session cannot be {PastTenseActionName} from its current lifecycle state.",
                    [$"Event session cannot be {PastTenseActionName} from its current lifecycle state."],
                    InvalidStatusFailureCode);
            }

            session.EventSessionStatusId = (int)TargetStatus;
            session.UpdatedAt = DateTime.UtcNow;

            await eventSessionRepository.Update(session);
            await RefreshParentScheduleSummaryAsync(parentEvent.Id, token);
            await cache.RemoveAsync($"event:detail:{parentEvent.Id}", token);
            await cache.RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), token);

            return Success(session.Id, $"Event session {PastTenseActionName} successfully.");
        }, cancellationToken);
    }

    protected abstract bool CanTransition(int currentSessionStatusId, int parentEventStatusId);

    protected static bool IsParentEventMutable(int parentEventStatusId) =>
        parentEventStatusId is not ((int)EventStatusEnum.Moderated or (int)EventStatusEnum.Archived);

    protected static bool IsTerminalSessionStatus(int statusId) =>
        statusId is (int)EventSessionStatusEnum.Rejected
            or (int)EventSessionStatusEnum.Cancelled
            or (int)EventSessionStatusEnum.Archived
            or (int)EventSessionStatusEnum.Completed
            or (int)EventSessionStatusEnum.Moderated;

    private async Task RefreshParentScheduleSummaryAsync(Guid eventId, CancellationToken cancellationToken)
    {
        Event? scheduleGraph = await eventRepository.GetScheduleGraphForUpdateAsync(eventId, cancellationToken);
        if (scheduleGraph is null)
        {
            return;
        }

        scheduleGraph.RecalculateScheduleSummaryFromSessions();
        await eventRepository.Update(scheduleGraph);
    }

    private static BaseCommandResponse<Guid> Success(Guid id, string message) => new()
    {
        Success = true,
        Id = id,
        Message = message
    };

    private static BaseCommandResponse<Guid> Failure(Guid id, string message, IEnumerable<string> errors, string? failureCode = null) => new()
    {
        Success = false,
        Id = id,
        Message = message,
        Errors = errors.ToList(),
        FailureCode = failureCode
    };
}
