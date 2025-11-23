using System.Net.Http.Json;
using Explore.Blazor.Client.Models.DTOs;

namespace Explore.Blazor.Client.Services;

public interface ILandingPageService
{
    Task<List<ProgramListDto>> GetFeaturedEventsAsync(int count = 6);
    Task<int> GetTotalMembersCountAsync();
    Task<int> GetUpcomingEventsCountAsync();
}

public class LandingPageService : ILandingPageService
{
    private readonly HttpClient _httpClient;
    
    public LandingPageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProgramListDto>> GetFeaturedEventsAsync(int count = 6)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ProgramListDto>>("/bff/api/Program");
            var events = response ?? new List<ProgramListDto>();
            
            // Filter en sorteer voor landing page display
            return events
                .Where(p => !string.IsNullOrEmpty(p.Title) && !string.IsNullOrEmpty(p.Description))
                .OrderByDescending(p => p.TotalViews) 
                .Take(count)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching featured events: {ex.Message}");
            return new List<ProgramListDto>();
        }
    }

    public async Task<int> GetTotalMembersCountAsync()
    {
        try
        {
            
            return await Task.FromResult(1247); 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching members count: {ex.Message}");
            return 1200; // Fallback waarde
        }
    }

    public async Task<int> GetUpcomingEventsCountAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ProgramListDto>>("/bff/api/Program");
            var events = response ?? new List<ProgramListDto>();
            
            return events.Count;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching events count: {ex.Message}");
            return 0;
        }
    }
}