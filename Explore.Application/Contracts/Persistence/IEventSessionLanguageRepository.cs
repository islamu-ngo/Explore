using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface IEventSessionLanguageRepository : IGenericRepository<EventSessionLanguage, int>
    {
        Task<List<EventSessionLanguage>> GetBySession(Guid eventSessionId);
    }
}
