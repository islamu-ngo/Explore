// ABOUTME: Handler for publishing an event session through the lifecycle policy path.
// ABOUTME: Validates parent-event compatibility and session readiness before changing session status.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession.Validators;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services.Lifecycle;
using Explore.Domain;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public sealed class PublishEventSessionCommandHandler(
    IEventSessionRepository eventSessionRepository,
    IEventRepository eventRepository,
    IEventLifecyclePolicyProvider policyProvider,
    IEventLifecycleReadinessEvaluator readinessEvaluator,
    HybridCache cache) : IRequestHandler<PublishEventSessionCommand, BaseCommandResponse<Guid>>
{
    private const string ConcurrencyConflictCode = "event_session_publish_concurrency_conflict";
    private const string ReadinessFailedCode = "event_session_publish_readiness_failed";

    public async Task<BaseCommandResponse<Guid>> Handle(PublishEventSessionCommand command, CancellationToken cancellationToken)
    {
        var validator = new PublishEventSessionRequestDtoValidator();
        var validationResult = await validator.ValidateAsync(command.Request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Failure(command.Id, "Event session publish request is invalid.", validationResult.Errors.Select(error => error.ErrorMessage));
        }

        var session = await eventSessionRepository.GetById(command.Id);
        if (session is null)
        {
            return Failure(command.Id, "Event session was not found.", ["Event session was not found."]);
        }

        if (session.ConcurrencyStamp != command.Request.ExpectedConcurrencyStamp)
        {
            return Failure(command.Id, "Event session was modified by another request.", ["Refresh the event session and try publishing again."], ConcurrencyConflictCode);
        }

        var parentEvent = await eventRepository.GetById(session.EventId);
        if (parentEvent is null || parentEvent.TenantId != session.TenantId)
        {
            return Failure(command.Id, "Parent event was not found in the current tenant.", ["Parent event was not found in the current tenant."]);
        }

        EventLifecyclePolicy policy = await policyProvider.GetEffectivePolicyAsync(session.TenantId, ValidationProfile.SessionPublish, cancellationToken);
        LifecycleReadinessResult readiness = readinessEvaluator.Evaluate(session, parentEvent, ValidationProfile.SessionPublish, policy);
        if (!readiness.IsReady)
        {
            return Failure(command.Id, "Event session is not ready to publish.", readiness.Errors.Select(error => error.Message), ReadinessFailedCode);
        }

        if (session.EventSessionStatusId == (int)EventSessionStatusEnum.Published)
        {
            return Success(session.Id, "Event session is already published.");
        }

        session.EventSessionStatusId = (int)EventSessionStatusEnum.Published;
        session.UpdatedAt = DateTime.UtcNow;

        await eventSessionRepository.Update(session);
        await RefreshParentScheduleSummaryAsync(parentEvent.Id, cancellationToken);
        await cache.RemoveAsync($"event:detail:{parentEvent.Id}", cancellationToken);
        await cache.RemoveByTagAsync(CacheTags.EventListByTenant(parentEvent.TenantId), cancellationToken);

        return Success(session.Id, "Event session published successfully.");
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
