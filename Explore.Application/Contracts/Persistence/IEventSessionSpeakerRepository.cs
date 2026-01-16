using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEventSessionSpeakerRepository : IGenericRepository<EventSessionSpeaker, Guid>
    {
        Task<List<EventSessionSpeaker>> GetBySession(Guid eventSessionId);
        Task<List<EventSessionSpeaker>> GetByActor(Guid actorId);
        Task<(List<EventSessionSpeaker> Items, int TotalCount)> GetSpeakersWithDetailsPaged(int pageNumber, int pageSize);
    }
}
