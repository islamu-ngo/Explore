using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface ILandingPageService
{
    Task<ICollection<EventListDto>> GetFeaturedEventsAsync(int count = 6);
    Task<int> GetTotalMembersCountAsync();
    Task<int> GetUpcomingEventsCountAsync();
}

public class LandingPageService : ILandingPageService
{
    private readonly IEventApiClient _apiClient;

    public LandingPageService(IEventApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ICollection<EventListDto>> GetFeaturedEventsAsync(int count = 6)
    {
        try
        {
            var response = await _apiClient.EventAllAsync();
            var events = response?.ToList() ?? new List<EventListDto>();

            // Filter and sort for landing page display
            return events
                .Where(p => !string.IsNullOrEmpty(p.Title) && !string.IsNullOrEmpty(p.Description))
                .OrderByDescending(p => p.TotalViews)
                .Take(count)
                .ToList();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"API error fetching featured events: {ex.StatusCode} - {ex.Message}");
            return new List<EventListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching featured events: {ex.Message}");
            return new List<EventListDto>();
        }
    }

    public async Task<int> GetTotalMembersCountAsync()
    {
        try
        {
            // TODO: Implement actual member count from API
            return await Task.FromResult(1247);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching members count: {ex.Message}");
            return 1200; // Fallback value
        }
    }

    public async Task<int> GetUpcomingEventsCountAsync()
    {
        try
        {
            var response = await _apiClient.EventAllAsync();
            return response?.Count ?? 0;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"API error fetching events count: {ex.StatusCode} - {ex.Message}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching events count: {ex.Message}");
            return 0;
        }
    }
}