// ABOUTME: Explicit result for one atomic registration parent/child lifecycle transition.
// ABOUTME: Carries stable occurrence identity and actor provenance for later notification materialization.

namespace Explore.Application.Contracts.Persistence;

public enum EventRegistrationTransitionReason
{
    NoChange,
    Created,
    Updated,
    ApprovalStatusChanged,
    CapacityWaitlisted,
    SelfCancelled,
    Revoked
}

public enum EventRegistrationActorProvenance
{
    Attendee,
    Organizer,
    System
}

public sealed record EventRegistrationChildTransition(
    Guid RegistrationId,
    Guid EventSessionId,
    int? PreviousStatus,
    int? FinalStatus);

public sealed record EventRegistrationTransitionResult(
    bool Changed,
    Guid? ParentIntentId,
    int? PreviousStatus,
    int? FinalStatus,
    EventRegistrationTransitionReason TransitionReason,
    Guid OccurrenceId,
    DateTimeOffset OccurredAt,
    EventRegistrationActorProvenance ActorProvenance,
    Guid? ActorUserId,
    IReadOnlyList<EventRegistrationChildTransition> ChildTransitions);
