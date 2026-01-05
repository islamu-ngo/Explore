using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IAtprotoRecordRepository : IGenericRepository<AtprotoRecord, Guid>
    {
        Task<AtprotoRecord?> GetByUri(string uri);
        Task<AtprotoRecord?> GetByDidAndCollection(string did, string collection, string recordKey);
        Task<List<AtprotoRecord>> GetByDid(string did);
    }
}
