using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEventSessionRepository : IGenericRepository<EventSession, Guid>
    {
        Task<EventSession?> GetSessionWithDetails(Guid id);
        Task<List<EventSession>> GetSessionsByEvent(Guid eventId);
        Task<List<EventSession>> GetSessionsByLocation(Guid locationId);
    }
}
