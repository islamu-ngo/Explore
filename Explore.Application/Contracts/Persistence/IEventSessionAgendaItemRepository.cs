using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEventSessionAgendaItemRepository : IGenericRepository<EventSessionAgendaItem, Guid>
    {
        Task<List<EventSessionAgendaItem>> GetBySession(Guid eventSessionId);
        Task<(List<EventSessionAgendaItem> Items, int TotalCount)> GetAgendaItemsWithDetailsPaged(int pageNumber, int pageSize);
    }
}
