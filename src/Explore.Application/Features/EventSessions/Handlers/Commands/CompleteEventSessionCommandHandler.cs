// ABOUTME: Handler that transitions an event session to the Completed lifecycle state.
// ABOUTME: Uses the shared session lifecycle path so cache invalidation and schedule recalculation stay consistent.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public sealed class CompleteEventSessionCommandHandler(
    IEventSessionRepository eventSessionRepository,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    HybridCache cache)
    : EventSessionLifecycleTransitionCommandHandlerBase<CompleteEventSessionCommand>(
        eventSessionRepository,
        eventRepository,
        unitOfWork,
        cache)
{
    protected override EventSessionStatusEnum TargetStatus => EventSessionStatusEnum.Completed;
    protected override string ActionName => "complete";
    protected override string PastTenseActionName => "completed";
    protected override string ConcurrencyFailureCode => "event_session_complete_concurrency_conflict";
    protected override string AlreadyInTargetStatusFailureCode => "event_session_complete_already_completed";
    protected override string InvalidStatusFailureCode => "event_session_complete_invalid_status";

    protected override bool CanTransition(int currentSessionStatusId, int parentEventStatusId) =>
        IsParentEventMutable(parentEventStatusId)
        && parentEventStatusId == (int)EventStatusEnum.Published
        && currentSessionStatusId == (int)EventSessionStatusEnum.Published;
}
