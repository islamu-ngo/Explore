using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface ISyncStateRepository : IGenericRepository<SyncState, int>
    {
        Task<SyncState?> GetByService(string service);
        Task<long> GetCursor(string service);
        Task UpdateCursor(string service, long cursor);
    }
}
