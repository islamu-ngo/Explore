using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventSessionLanguageRepository : IGenericRepository<EventSessionLanguage, int>
{
    Task<List<EventSessionLanguage>> GetBySession(Guid eventSessionId);
    Task<EventSessionLanguage?> GetBySessionAndLanguage(Guid eventSessionId, int languageId, int? excludeId = null);
    Task<(List<EventSessionLanguage> Items, int TotalCount)> GetLanguagesWithDetailsPaged(int pageNumber, int pageSize);
}
