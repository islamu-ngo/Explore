// ABOUTME: Repository contract for event-session speaker link entities.
// ABOUTME: Returns domain entities for handler-owned mapping and relationship validation.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventSessionSpeakerRepository : IGenericRepository<EventSessionSpeaker, Guid>
{
    Task<List<EventSessionSpeaker>> GetBySession(Guid eventSessionId);
    Task<List<EventSessionSpeaker>> GetByActor(Guid actorId);
    Task<(List<EventSessionSpeaker> Items, int TotalCount)> GetSpeakersWithDetailsPaged(int pageNumber, int pageSize);
    Task<EventSessionSpeaker?> GetBySessionAndActor(Guid eventSessionId, Guid actorId, Guid? excludeId = null);
}
