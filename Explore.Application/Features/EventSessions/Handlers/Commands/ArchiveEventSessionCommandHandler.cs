// ABOUTME: Handler that transitions an event session to the Archived lifecycle state.
// ABOUTME: Uses the shared session lifecycle path so cache invalidation and schedule recalculation stay consistent.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessions.Requests.Commands;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;

namespace Explore.Application.Features.EventSessions.Handlers.Commands;

public sealed class ArchiveEventSessionCommandHandler(
    IEventSessionRepository eventSessionRepository,
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    HybridCache cache)
    : EventSessionLifecycleTransitionCommandHandlerBase<ArchiveEventSessionCommand>(
        eventSessionRepository,
        eventRepository,
        unitOfWork,
        cache)
{
    protected override EventSessionStatusEnum TargetStatus => EventSessionStatusEnum.Archived;
    protected override string ActionName => "archive";
    protected override string PastTenseActionName => "archived";
    protected override string ConcurrencyFailureCode => "event_session_archive_concurrency_conflict";
    protected override string AlreadyInTargetStatusFailureCode => "event_session_archive_already_archived";
    protected override string InvalidStatusFailureCode => "event_session_archive_invalid_status";

    protected override bool CanTransition(int currentSessionStatusId, int parentEventStatusId) =>
        IsParentEventMutable(parentEventStatusId)
        && currentSessionStatusId is (int)EventSessionStatusEnum.Draft
            or (int)EventSessionStatusEnum.Cancelled
            or (int)EventSessionStatusEnum.Completed;
}
