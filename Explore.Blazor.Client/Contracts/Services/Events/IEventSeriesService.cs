// ABOUTME: Contract for EventSeries operations — list, detail, create, and search.
// ABOUTME: Used by EventSeriesSection component for series selection on Create/Edit Event pages.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Events;

public interface IEventSeriesService
{
    Task<PaginatedResultOfEventSeriesListDto?> GetSeriesListAsync(
        int pageNumber = 1, int pageSize = 10, Guid? actorId = null, CancellationToken ct = default);

    Task<EventSeriesDto?> GetSeriesDetailAsync(Guid id, CancellationToken ct = default);

    Task<BaseCommandResponseOfGuid?> CreateSeriesAsync(CreateEventSeriesDto dto, CancellationToken ct = default);

    /// <summary>
    /// Searches series by title substring for autocomplete.
    /// Returns at most <paramref name="maxResults"/> items.
    /// </summary>
    Task<IEnumerable<EventSeriesListDto>> SearchSeriesAsync(string query, int maxResults = 10, CancellationToken ct = default);
}
