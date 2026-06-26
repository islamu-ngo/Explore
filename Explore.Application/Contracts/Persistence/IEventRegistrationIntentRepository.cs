// ABOUTME: Repository contract for EventRegistrationIntent - the parent row preserving why a user registered (whole event, whole day, or chosen sessions).
// ABOUTME: Provides a transactional CreateWithChildrenAsync so parent intent + child session access rows land in a single unit of work.

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
    /// Inserts the parent intent and its child session-access rows in a single serializable transaction.
    /// Child rows must already carry <see cref="EventRegistration.EventRegistrationIntentId"/> set to the parent id.
    /// </summary>
    Task<EventRegistrationIntent> CreateWithChildrenAsync(
        EventRegistrationIntent intent,
        IReadOnlyList<EventRegistration> children,
        CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the parent intent and child session-access rows while atomically reserving session capacity.
    /// Full sessions create waitlisted child rows instead of incrementing the attendee counter.
    /// </summary>
    Task<EventRegistrationIntentCreationResult> CreateWithChildrenAndCapacityAsync(
        EventRegistrationIntent intent,
        IReadOnlyList<EventRegistration> children,
        int approvedStatusId,
        int waitlistedStatusId,
        CancellationToken cancellationToken,
        EmailDispatchOutbox? emailDispatchOutbox = null);

    Task<IReadOnlyList<Guid>> GetRegisteredUserFanoutBatchAsync(
        Guid tenantId,
        Guid eventId,
        Guid? afterUserId,
        int pageSize,
        CancellationToken cancellationToken);
}
