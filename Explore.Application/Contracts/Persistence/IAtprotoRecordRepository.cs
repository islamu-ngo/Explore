using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IAtprotoRecordRepository : IGenericRepository<AtprotoRecord, Guid>
    {
        Task<List<AtprotoRecord>> GetAllAtprotoRecords();
        Task<AtprotoRecord?> GetAtprotoRecordByUri(string uri);
        Task<List<AtprotoRecord>> GetAtprotoRecordsByDid(string did);
        Task<List<AtprotoRecord>> GetAtprotoRecordsByCollection(string collection);
        Task<bool> Exists(Guid id);
    }
}
