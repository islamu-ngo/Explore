using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface IEventService
{
    Task<ICollection<EventListDto>> GetAllEventsAsync();
    Task<ICollection<EventListDto>> GetMyEventsAsync();
    Task<EventDto?> GetEventByIdAsync(Guid eventId);
    Task<bool> DeleteEventAsync(Guid eventId);
    Task<BaseCommandResponseOfGuid?> UpdateEventAsync(Guid eventId, UpdateEventDto eventDto);
    Task<BaseCommandResponseOfGuid?> CreateEventAsync(CreateEventDto createDto);
}

public class EventService : IEventService
{
    private readonly IEventApiClient _apiClient;

    public EventService(IEventApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<ICollection<EventListDto>> GetMyEventsAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[EVENT SERVICE] ERROR: API client is null");
                return new List<EventListDto>();
            }
            
            Console.WriteLine("[EVENT SERVICE] Fetching my events...");
            var response = await _apiClient.MyAsync();
            Console.WriteLine($"[EVENT SERVICE] Received {response?.Count ?? 0} events");
            return response ?? new List<EventListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[EVENT SERVICE] API error fetching my events: {ex.StatusCode} - {ex.Message}");
            return new List<EventListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EVENT SERVICE] Error fetching my events: {ex.Message}");
            return new List<EventListDto>();
        }
    }

    public async Task<ICollection<EventListDto>> GetAllEventsAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[EVENT SERVICE] ERROR: API client is null");
                return new List<EventListDto>();
            }
            
            Console.WriteLine("[EVENT SERVICE] Fetching all events...");
            var response = await _apiClient.EventAllAsync();
            Console.WriteLine($"[EVENT SERVICE] Received {response?.Count ?? 0} events");
            return response ?? new List<EventListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[EVENT SERVICE] API error fetching events: {ex.StatusCode} - {ex.Message}");
            return new List<EventListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EVENT SERVICE] Error fetching events: {ex.Message}");
            return new List<EventListDto>();
        }
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid eventId)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[EVENT SERVICE] ERROR: API client is null");
                return null;
            }
            
            return await _apiClient.EventGETAsync(eventId);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            Console.WriteLine($"[EVENT SERVICE] Event not found: {eventId}");
            return null;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[EVENT SERVICE] API error fetching event {eventId}: {ex.StatusCode} - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EVENT SERVICE] Error fetching event {eventId}: {ex.Message}");
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateEventAsync(Guid eventId, UpdateEventDto eventDto)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[EVENT SERVICE] ERROR: API client is null");
                return new BaseCommandResponseOfGuid { Success = false, Message = "API client not available" };
            }
            
            return await _apiClient.EventPUTAsync(eventId, eventDto);
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[EVENT SERVICE] API error updating event {eventId}: {ex.StatusCode} - {ex.Message}");
            return new BaseCommandResponseOfGuid 
            { 
                Success = false, 
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EVENT SERVICE] Error updating event {eventId}: {ex.Message}");
            return new BaseCommandResponseOfGuid 
            { 
                Success = false, 
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> DeleteEventAsync(Guid eventId)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[EVENT SERVICE] ERROR: API client is null");
                return false;
            }
            
            await _apiClient.EventDELETEAsync(eventId);
            return true;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[EVENT SERVICE] API error deleting event {eventId}: {ex.StatusCode} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EVENT SERVICE] Error deleting event {eventId}: {ex.Message}");
            return false;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateEventAsync(CreateEventDto createDto)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[EVENT SERVICE] ERROR: API client is null");
                return new BaseCommandResponseOfGuid { Success = false, Message = "API client not available" };
            }
            
            Console.WriteLine($"[EVENT SERVICE] Creating event: {createDto.Title}");
            Console.WriteLine($"[EVENT SERVICE] OrganizationId: {createDto.OrganizationId}");
            Console.WriteLine($"[EVENT SERVICE] EventTypeId: {createDto.EventTypeId}");
            Console.WriteLine($"[EVENT SERVICE] FirstSessionDate: {createDto.FirstSessionDate}");
            Console.WriteLine($"[EVENT SERVICE] LastSessionDate: {createDto.LastSessionDate}");
            Console.WriteLine($"[EVENT SERVICE] FeaturedImageId: {createDto.FeaturedImageId}");
            
            var response = await _apiClient.EventPOSTAsync(createDto);
            
            Console.WriteLine($"[EVENT SERVICE] Create response: Success={response?.Success}, Id={response?.Id}");
            return response;
        }
        catch (ApiException ex) when (ex.StatusCode == 200 || ex.StatusCode == 201)
        {
            // Sometimes NSwag throws when response is successful but body doesn't match expected schema
            Console.WriteLine($"[EVENT SERVICE] Event created but response parsing had issue (status {ex.StatusCode}): {ex.Message}");
            return new BaseCommandResponseOfGuid 
            { 
                Success = true, 
                Message = "Event created successfully"
            };
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[EVENT SERVICE] API error creating event: {ex.StatusCode} - {ex.Message}");
            Console.WriteLine($"[EVENT SERVICE] Response: {ex.Response}");
            return new BaseCommandResponseOfGuid 
            { 
                Success = false, 
                Message = $"API error ({ex.StatusCode}): {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EVENT SERVICE] Error creating event: {ex.Message}");
            Console.WriteLine($"[EVENT SERVICE] Stack trace: {ex.StackTrace}");
            return new BaseCommandResponseOfGuid 
            { 
                Success = false, 
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }
}
