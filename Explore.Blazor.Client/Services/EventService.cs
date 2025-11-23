using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.DTOs;
using Explore.Blazor.Client.Models.Responses;
using System.Net.Http.Json;

namespace Explore.Blazor.Client.Services;

public interface IEventService
{
    List<Event> GetUserEvents(string? searchText = null, string? country = null, string? category = null, DateTime? date = null);
    Task<List<EventListDto>> GetAllEventsAsync(string? searchText = null, string? country = null, string? category = null, DateTime? date = null);
    Task<List<EventListDto>> GetMyEventsAsync();
    Task<EventDetailsDto> GetEventByIdAsync(Guid eventId);
    Task<bool> DeleteEventAsync(Guid eventId);
    Task<bool> UpdateEventAsync(Guid eventId, UpdateEventDto eventDto);
    Task<Guid?> CreateEventAsync(CreateEventDto createDto);
    Event? GetEventById(int eventId);
    void DeleteEvent(int eventId);
    void UpdateEvent(Event evt);
}

public class EventService : IEventService
{
    private readonly HttpClient _httpClient;

    public EventService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<EventListDto>> GetMyEventsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<EventListDto>>("/bff/api/Event/my");
            return response ?? new List<EventListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching my events: {ex.Message}");
            return new List<EventListDto>();
        }
    }

    public async Task<EventDetailsDto> GetEventByIdAsync(Guid eventId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<EventDetailsDto>($"/bff/api/Event/{eventId}");
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching event {eventId}: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateEventAsync(Guid eventId, UpdateEventDto eventDto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/bff/api/Event/{eventId}", eventDto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating event {eventId}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteEventAsync(Guid eventId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/bff/api/Event/{eventId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting event {eventId}: {ex.Message}");
            return false;
        }
    }

    public async Task<Guid?> CreateEventAsync(CreateEventDto createDto)
    {
        try
        {
            Console.WriteLine($"Creating event with title: {createDto.Title}");
            var response = await _httpClient.PostAsJsonAsync("/bff/api/Event", createDto);
            
            Console.WriteLine($"Response status: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response content: {content}");
            
            if (response.IsSuccessStatusCode)
            {
                // API returns BaseCommandResponse<Guid>
                var result = await response.Content.ReadFromJsonAsync<BaseCommandResponse<Guid>>();
                
                if (result?.Success == true && result.Id != Guid.Empty)
                {
                    Console.WriteLine($"Event created successfully with ID: {result.Id}");
                    return result.Id;
                }
                
                Console.WriteLine($"Event creation failed: {result?.Message}");
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating event: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<List<EventListDto>> GetAllEventsAsync(string? searchText = null, string? country = null, string? category = null, DateTime? date = null)
    {
        try
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(searchText))
                queryParams.Add($"search={Uri.EscapeDataString(searchText)}");
            if (!string.IsNullOrEmpty(country))
                queryParams.Add($"country={Uri.EscapeDataString(country)}");
            if (!string.IsNullOrEmpty(category))
                queryParams.Add($"category={Uri.EscapeDataString(category)}");
            if (date.HasValue)
                queryParams.Add($"date={date.Value:yyyy-MM-dd}");

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetFromJsonAsync<List<EventListDto>>($"/bff/api/Event{query}");
            return response ?? new List<EventListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching events: {ex.Message}");
            return new List<EventListDto>();
        }
    }

    public List<Event> GetUserEvents(string? searchText = null, string? country = null, string? category = null, DateTime? date = null)
    {
        // Legacy method - consider migrating callers to use GetAllEventsAsync
        // Return empty list as mock data has been removed
        return new List<Event>();
    }

    public Event? GetEventById(int eventId)
    {
        // Legacy method - mock data has been removed
        // Consider using GetEventByIdAsync(Guid) instead
        return null;
    }

    public void DeleteEvent(int eventId)
    {
        // Legacy method - mock data has been removed
        // Consider using DeleteEventAsync(Guid) instead
    }

    public void UpdateEvent(Event evt)
    {
        // Legacy method - mock data has been removed
        // Consider using UpdateEventAsync(Guid, UpdateEventDto) instead
    }
}
