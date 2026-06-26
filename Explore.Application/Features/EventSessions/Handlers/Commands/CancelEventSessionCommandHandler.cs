// ABOUTME: Handler that transitions an event session to the Cancelled lifecycle state.
// ABOUTME: Uses the shared session lifecycle path so cache invalidation and schedule recalculation stay consistent.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public sealed class CancelEventSessionCommandHandler(
    IEventSessionRepository eventSessionRepository,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    HybridCache cache)
    : EventSessionLifecycleTransitionCommandHandlerBase<CancelEventSessionCommand>(
        eventSessionRepository,
        eventRepository,
        unitOfWork,
        cache)
{
    protected override EventSessionStatusEnum TargetStatus => EventSessionStatusEnum.Cancelled;
    protected override string ActionName => "cancel";
    protected override string PastTenseActionName => "cancelled";
    protected override string ConcurrencyFailureCode => "event_session_cancel_concurrency_conflict";
    protected override string AlreadyInTargetStatusFailureCode => "event_session_cancel_already_cancelled";
    protected override string InvalidStatusFailureCode => "event_session_cancel_invalid_status";

    protected override bool CanTransition(int currentSessionStatusId, int parentEventStatusId) =>
        IsParentEventMutable(parentEventStatusId)
        && currentSessionStatusId is (int)EventSessionStatusEnum.Draft
            or (int)EventSessionStatusEnum.Submitted
            or (int)EventSessionStatusEnum.UnderReview
            or (int)EventSessionStatusEnum.Approved
            or (int)EventSessionStatusEnum.Published;
}
