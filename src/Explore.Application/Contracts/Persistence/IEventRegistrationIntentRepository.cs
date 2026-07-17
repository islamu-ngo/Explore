// ABOUTME: Repository contract for EventRegistrationIntent - the parent row preserving why a user registered (whole event, whole day, or chosen sessions).
// ABOUTME: Exposes caller-coordinated capacity transitions plus stable attendee fanout reads.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventRegistrationIntentRepository : IGenericRepository<EventRegistrationIntent, Guid>
{
    /// <summary>
    /// Returns the existing parent intent for the supplied (event, user, scope[, selectedDay]) tuple, or null.
    /// Day-scope intents key on <paramref name="selectedEventDayId"/>; all other scopes ignore it.
    /// </summary>
    Task<EventRegistrationIntent?> FindExistingAsync(
        Guid eventId,
        Guid userId,
        int registrationScopeId,
        Guid? selectedEventDayId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the parent intent and child session-access rows while atomically reserving session capacity.
    /// Full sessions create waitlisted child rows instead of incrementing the attendee counter.
    /// The caller must provide the serializable unit-of-work transaction.
    /// </summary>
    Task<EventRegistrationIntentCreationResult> CreateWithChildrenAndCapacityAsync(
        EventRegistrationIntent intent,
        IReadOnlyList<EventRegistration> children,
        int approvedStatusId,
        int waitlistedStatusId,
        Guid occurrenceId,
        DateTimeOffset occurredAt,
        EventRegistrationActorProvenance actorProvenance,
        Guid? actorUserId,
        CancellationToken cancellationToken,
        IntegrationSyncOutbox? integrationSyncOutbox = null);

    Task<IReadOnlyList<Guid>> GetRegisteredUserFanoutBatchAsync(
        Guid tenantId,
        Guid eventId,
        Guid? afterUserId,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationFanoutAudienceMember>> GetNotificationFanoutAudienceBatchAsync(
        Guid tenantId,
        Guid eventId,
        Guid? sessionId,
        DateTime audienceCutoffAt,
        int deliveryPolicyId,
        NotificationFanoutAudienceCursor? after,
        int pageSize,
        CancellationToken cancellationToken);
}
