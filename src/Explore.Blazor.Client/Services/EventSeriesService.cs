// ABOUTME: Service for managing Event Series via the NSwag-generated API client.
// ABOUTME: Provides list, detail, create, update, and search operations for series selection.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public class EventSeriesService : IEventSeriesService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<EventSeriesService> _logger;

    public EventSeriesService(
        IEventApiClient apiClient,
        ILogger<EventSeriesService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResultOfEventSeriesListDto?> GetSeriesListAsync(
        int pageNumber = 1, int pageSize = 10, Guid? actorId = null, CancellationToken ct = default)
    {
        try
        {
            return await _apiClient.GetEventSeriesAsync(
                actorId: actorId,
                pageNumber: pageNumber,
                pageSize: pageSize,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event series list (page {PageNumber}, size {PageSize})",
                pageNumber, pageSize);
            return null;
        }
    }

    public async Task<EventSeriesDto?> GetSeriesDetailAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            return await _apiClient.GetEventSeriesByIdAsync(id, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event series detail {SeriesId}", id);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateSeriesAsync(CreateEventSeriesDto dto, CancellationToken ct = default)
    {
        try
        {
            return await _apiClient.CreateEventSeriesAsync(dto, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event series");
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateSeriesAsync(
        Guid id,
        Guid expectedConcurrencyStamp,
        UpdateEventSeriesDto dto,
        CancellationToken ct = default)
    {
        try
        {
            return await _apiClient.UpdateEventSeriesAsync(
                id,
                dto,
                $"\"{expectedConcurrencyStamp:D}\"",
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event series {SeriesId}", id);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Failed to update event series."
            };
        }
    }

    public async Task<IEnumerable<EventSeriesListDto>> SearchSeriesAsync(string query, int maxResults = 10, CancellationToken ct = default)
    {
        try
        {
            var result = await _apiClient.GetEventSeriesAsync(
                pageNumber: 1,
                pageSize: maxResults,
                cancellationToken: ct);
            if (result?.Items is null)
                return [];

            if (string.IsNullOrWhiteSpace(query))
                return result.Items;

            return result.Items
                .Where(s => s.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(maxResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching event series with query '{Query}'", query);
            return [];
        }
    }
}
