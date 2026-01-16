using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IIndexedDidRepository : IGenericRepository<IndexedDid, string>
    {
        Task<List<IndexedDid>> GetAllIndexedDids();
        Task<IndexedDid?> GetIndexedDidByDid(string did);
        Task<bool> Exists(string did);
    }
}
