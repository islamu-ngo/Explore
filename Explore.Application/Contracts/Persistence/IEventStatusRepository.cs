using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEventStatusRepository : IGenericRepository<EventStatus, int>
    {
    }
}
