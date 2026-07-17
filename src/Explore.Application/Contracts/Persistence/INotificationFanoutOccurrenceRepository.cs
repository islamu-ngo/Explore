// ABOUTME: Persistence contract for immutable notification fanout occurrences.
// ABOUTME: Loads a worker occurrence only through its tenant-scoped PII-free pointer.

using Explore.Application.Models.InternalEvents;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface INotificationFanoutOccurrenceRepository : IGenericRepository<NotificationFanoutOccurrence, Guid>
{
    Task<NotificationFanoutOccurrence?> GetByPointerAsync(
        NotificationFanoutOccurrenceRequested pointer,
        bool trackChanges = false,
        CancellationToken cancellationToken = default);
}
