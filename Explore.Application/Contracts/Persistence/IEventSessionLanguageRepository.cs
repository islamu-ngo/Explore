// ABOUTME: Persistence contract for event-session language assignment queries.
// ABOUTME: Keeps session-language reads entity-first and cancellation-aware for CQRS handlers.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventSessionLanguageRepository : IGenericRepository<EventSessionLanguage, int>
{
    Task<List<EventSessionLanguage>> GetBySession(Guid eventSessionId, CancellationToken cancellationToken = default);
    Task<EventSessionLanguage?> GetBySessionAndLanguage(Guid eventSessionId, int languageId, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<(List<EventSessionLanguage> Items, int TotalCount)> GetLanguagesWithDetailsPaged(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
