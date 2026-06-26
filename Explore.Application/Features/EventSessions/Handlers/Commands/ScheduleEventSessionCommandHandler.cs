// ABOUTME: Handler for assigning a concrete schedule to an existing event session.
// ABOUTME: Uses domain schedule projection, lifecycle readiness, and room-overlap guard before saving.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services.Lifecycle;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public sealed class ScheduleEventSessionCommandHandler(
    IEventSessionRepository eventSessionRepository,
    IEventRepository eventRepository,
    IEventDayRepository eventDayRepository,
    IEventScheduleProjectionCalculator scheduleProjectionCalculator,
    IEventLifecyclePolicyProvider policyProvider,
    IEventLifecycleReadinessEvaluator readinessEvaluator,
    HybridCache cache) : IRequestHandler<ScheduleEventSessionCommand, BaseCommandResponse<Guid>>
{
    private const string ConcurrencyConflictCode = "event_session_schedule_concurrency_conflict";
    private const string ReadinessFailedCode = "event_session_schedule_readiness_failed";

    public async Task<BaseCommandResponse<Guid>> Handle(ScheduleEventSessionCommand command, CancellationToken cancellationToken)
    {
        var validator = new ScheduleEventSessionRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(command.Request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Failure(command.Id, "Event session schedule request is invalid.", validationResult.Errors.Select(error => error.ErrorMessage));
        }

        var session = await eventSessionRepository.GetById(command.Id);
        if (session is null)
        {
            return Failure(command.Id, "Event session was not found.", ["Event session was not found."]);
        }

        if (session.ConcurrencyStamp != command.Request.ExpectedConcurrencyStamp)
        {
            return Failure(command.Id, "Event session was modified by another request.", ["Refresh the event session and try scheduling again."], ConcurrencyConflictCode);
        }

        var parentEvent = await eventRepository.GetById(session.EventId);
        if (parentEvent is null || parentEvent.TenantId != session.TenantId)
        {
            return Failure(command.Id, "Parent event was not found in the current tenant.", ["Parent event was not found in the current tenant."]);
        }

        session.Reschedule(
            command.Request.StartTime,
            command.Request.EndTime,
            parentEvent.EventTimeZoneId ?? parentEvent.Timezone ?? string.Empty,
            scheduleProjectionCalculator);

        EventDay? matchingDay = session.LocalStartDate is not null
            ? await eventDayRepository.FindByEventAndLocalDateAsync(parentEvent.Id, session.LocalStartDate.Value, cancellationToken)
            : null;
        session.EventDayId = matchingDay?.Id;

        EventLifecyclePolicy policy = await policyProvider.GetEffectivePolicyAsync(session.TenantId, ValidationProfile.SessionSchedule, cancellationToken);
        LifecycleReadinessResult readiness = readinessEvaluator.Evaluate(session, parentEvent, ValidationProfile.SessionSchedule, policy);
        if (!readiness.IsReady)
        {
            return Failure(command.Id, "Event session is not ready to schedule.", readiness.Errors.Select(error => error.Message), ReadinessFailedCode);
        }

        try
        {
            await eventSessionRepository.UpdateWithRoomOverlapGuardAsync(session, cancellationToken);
        }
        catch (RoomScheduleConflictException ex)
        {
            return Failure(command.Id, "Event session schedule request conflicts with an existing room booking.", [ex.Message], "room_schedule_conflict");
        }

        await RefreshParentScheduleSummaryAsync(parentEvent.Id, cancellationToken);

        await cache.RemoveAsync($"event:detail:{parentEvent.Id}", cancellationToken);
        await cache.RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), cancellationToken);

        return Success(session.Id, "Event session scheduled successfully.");
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
}
