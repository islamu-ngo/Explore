using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEventSessionSpeakerRepository : IGenericRepository<EventSessionSpeaker, int>
    {
        Task<List<EventSessionSpeaker>> GetBySession(Guid eventSessionId);
        Task<List<EventSessionSpeaker>> GetByActor(Guid actorId);
    }
}
