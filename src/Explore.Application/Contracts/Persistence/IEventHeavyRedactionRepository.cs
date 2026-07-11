// ABOUTME: Repository contract for loading and saving the tracked event graph used by heavy moderation redaction.
// ABOUTME: Returns domain entities only so Application owns redaction rules while Persistence owns EF graph loading.

using Explore.Application.Features.Events.Moderation;

namespace Explore.Application.Contracts.Persistence;

public interface IEventHeavyRedactionRepository
{
    Task<EventHeavyRedactionGraph?> GetForUpdateAsync(Guid eventId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
