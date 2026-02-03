using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Defines the contract for landing page data operations.
/// </summary>
public interface ILandingPageService
{
    /// <summary>
    /// Gets featured events for the landing page display.
    /// </summary>
    /// <param name="count">The number of events to retrieve.</param>
    /// <returns>A collection of featured events.</returns>
    Task<ICollection<EventListDto>> GetFeaturedEventsAsync(int count = 6);

    /// <summary>
    /// Gets the total count of registered members.
    /// </summary>
    /// <returns>The total member count.</returns>
    Task<int> GetTotalMembersCountAsync();

    /// <summary>
    /// Gets the count of upcoming events.
    /// </summary>
    /// <returns>The upcoming events count.</returns>
    Task<int> GetUpcomingEventsCountAsync();
}

/// <summary>
/// Service for retrieving landing page data including featured events and statistics.
/// </summary>
public class LandingPageService : ILandingPageService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<LandingPageService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LandingPageService"/> class.
    /// </summary>
    /// <param name="apiClient">The API client for event operations.</param>
    /// <param name="logger">The logger instance.</param>
    public LandingPageService(IEventApiClient apiClient, ILogger<LandingPageService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ICollection<EventListDto>> GetFeaturedEventsAsync(int count = 6)
    {
        try
        {
            _logger.LogDebug("Fetching featured events with count {Count}", count);
            var response = await _apiClient.GetEventsAsync(pageNumber: 1, pageSize: 100);
            var events = response?.GetItems() ?? new List<EventListDto>();

            // Filter and sort for landing page display
            var featuredEvents = events
                .Where(p => !string.IsNullOrEmpty(p.Title) && !string.IsNullOrEmpty(p.Description))
                .OrderByDescending(p => p.TotalViews)
                .Take(count)
                .ToList();

            _logger.LogDebug("Retrieved {FeaturedCount} featured events from {TotalCount} total", featuredEvents.Count, events.Count);
            return featuredEvents;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error fetching featured events. StatusCode: {StatusCode}", ex.StatusCode);
            return new List<EventListDto>();
        }
    }

    /// <inheritdoc />
    public async Task<int> GetTotalMembersCountAsync()
    {
        try
        {
            // TODO: Implement actual member count from API
            _logger.LogDebug("Returning placeholder member count");
            return await Task.FromResult(1247);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching members count");
            return 1200; // Fallback value
        }
    }

    /// <inheritdoc />
    public async Task<int> GetUpcomingEventsCountAsync()
    {
        try
        {
            _logger.LogDebug("Fetching upcoming events count");
            var response = await _apiClient.GetEventsAsync(pageNumber: 1, pageSize: 100);
            var count = response?.TotalCount ?? 0;
            _logger.LogDebug("Retrieved {Count} upcoming events", count);
            return count;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error fetching events count. StatusCode: {StatusCode}", ex.StatusCode);
            return 0;
        }
    }
}
