using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IIndexedDidRepository
    {
        Task<IndexedDid?> GetByDid(string did);
        Task<IndexedDid?> GetByHandle(string handle);
        Task<List<IndexedDid>> GetActiveDids();
        Task<IndexedDid> Upsert(IndexedDid indexedDid);
        Task Delete(string did);
        Task<bool> Exists(string did);
    }
}
