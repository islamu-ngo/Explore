using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface IProgramService
{
    Task<ICollection<EventListDto>> GetAllProgramsAsync();
    Task<EventDto?> GetProgramByIdAsync(Guid id);
    Task<ICollection<EventTypeListDto>> GetEventTypesAsync();
    Task<ICollection<EventFormatListDto>> GetProgramTypesAsync();
    Task<ICollection<EventSessionListDto>> GetSessionsByEventAsync(Guid eventId);
    Task<BaseCommandResponseOfGuid?> RegisterForProgramAsync(CreateEventRegistrationDto registration);
    Task<ICollection<EventRegistrationListDto>> GetRegistrationsForSessionAsync(Guid sessionId);
    Task<ICollection<EventRegistrationListDto>> GetRegistrationsByUserAsync(Guid userId);
    Task<bool> UnregisterFromProgramAsync(Guid registrationId);
}

public class ProgramService : IProgramService
{
    private readonly IEventApiClient _apiClient;

    public ProgramService(IEventApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<ICollection<EventListDto>> GetAllProgramsAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[PROGRAM SERVICE] ERROR: API client is null");
                return new List<EventListDto>();
            }
            
            Console.WriteLine("[PROGRAM SERVICE] Fetching all programs...");
            var response = await _apiClient.EventAllAsync();
            Console.WriteLine($"[PROGRAM SERVICE] Received {response?.Count ?? 0} programs");
            return response ?? new List<EventListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] API error fetching programs: {ex.StatusCode} - {ex.Message}");
            return new List<EventListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] Error fetching programs: {ex.Message}");
            Console.WriteLine($"[PROGRAM SERVICE] Stack trace: {ex.StackTrace}");
            return new List<EventListDto>();
        }
    }

    public async Task<EventDto?> GetProgramByIdAsync(Guid id)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[PROGRAM SERVICE] ERROR: API client is null");
                return null;
            }
            
            return await _apiClient.EventGETAsync(id);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            Console.WriteLine($"[PROGRAM SERVICE] Program not found: {id}");
            return null;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] API error fetching program: {ex.StatusCode} - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] Error fetching program: {ex.Message}");
            return null;
        }
    }

    public async Task<ICollection<EventTypeListDto>> GetEventTypesAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[PROGRAM SERVICE] ERROR: API client is null");
                return new List<EventTypeListDto>();
            }
            
            Console.WriteLine("[PROGRAM SERVICE] Fetching event types...");
            var response = await _apiClient.EventTypeAllAsync();
            Console.WriteLine($"[PROGRAM SERVICE] Received {response?.Count ?? 0} event types");
            return response ?? new List<EventTypeListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] API error fetching event types: {ex.StatusCode} - {ex.Message}");
            return new List<EventTypeListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] Error fetching event types: {ex.Message}");
            Console.WriteLine($"[PROGRAM SERVICE] Stack trace: {ex.StackTrace}");
            return new List<EventTypeListDto>();
        }
    }

    public async Task<ICollection<EventFormatListDto>> GetProgramTypesAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[PROGRAM SERVICE] ERROR: API client is null");
                return new List<EventFormatListDto>();
            }
            
            Console.WriteLine("[PROGRAM SERVICE] Fetching program types (event formats)...");
            var response = await _apiClient.EventFormatAllAsync();
            Console.WriteLine($"[PROGRAM SERVICE] Received {response?.Count ?? 0} program types");
            return response ?? new List<EventFormatListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] API error fetching program types: {ex.StatusCode} - {ex.Message}");
            return new List<EventFormatListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] Error fetching program types: {ex.Message}");
            Console.WriteLine($"[PROGRAM SERVICE] Stack trace: {ex.StackTrace}");
            return new List<EventFormatListDto>();
        }
    }

    public async Task<ICollection<EventSessionListDto>> GetSessionsByEventAsync(Guid eventId)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[PROGRAM SERVICE] ERROR: API client is null");
                return new List<EventSessionListDto>();
            }
            
            Console.WriteLine($"[PROGRAM SERVICE] Fetching sessions for event {eventId}...");
            var response = await _apiClient.ByEvent2Async(eventId);
            Console.WriteLine($"[PROGRAM SERVICE] Received {response?.Count ?? 0} sessions");
            return response ?? new List<EventSessionListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] API error fetching event sessions: {ex.StatusCode} - {ex.Message}");
            return new List<EventSessionListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] Error fetching event sessions: {ex.Message}");
            Console.WriteLine($"[PROGRAM SERVICE] Stack trace: {ex.StackTrace}");
            return new List<EventSessionListDto>();
        }
    }

    public async Task<BaseCommandResponseOfGuid?> RegisterForProgramAsync(CreateEventRegistrationDto registration)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[PROGRAM SERVICE] ERROR: API client is null");
                return null;
            }
            
            return await _apiClient.EventRegistrationPOSTAsync(registration);
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] API error registering for program: {ex.StatusCode} - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] Error registering for program: {ex.Message}");
            return null;
        }
    }

    public async Task<ICollection<EventRegistrationListDto>> GetRegistrationsForSessionAsync(Guid sessionId)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[PROGRAM SERVICE] ERROR: API client is null");
                return new List<EventRegistrationListDto>();
            }
            
            var response = await _apiClient.BySessionAsync(sessionId);
            return response ?? new List<EventRegistrationListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] API error fetching registrations: {ex.StatusCode} - {ex.Message}");
            return new List<EventRegistrationListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] Error fetching registrations: {ex.Message}");
            return new List<EventRegistrationListDto>();
        }
    }

    public async Task<ICollection<EventRegistrationListDto>> GetRegistrationsByUserAsync(Guid userId)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[PROGRAM SERVICE] ERROR: API client is null");
                return new List<EventRegistrationListDto>();
            }
            
            var response = await _apiClient.ByUserAsync(userId);
            return response ?? new List<EventRegistrationListDto>();
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] API error fetching user registrations: {ex.StatusCode} - {ex.Message}");
            return new List<EventRegistrationListDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] Error fetching user registrations: {ex.Message}");
            return new List<EventRegistrationListDto>();
        }
    }

    public async Task<bool> UnregisterFromProgramAsync(Guid registrationId)
    {
        try
        {
            if (_apiClient == null)
            {
                Console.WriteLine("[PROGRAM SERVICE] ERROR: API client is null");
                return false;
            }
            
            await _apiClient.EventRegistrationDELETEAsync(registrationId);
            return true;
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] API error unregistering: {ex.StatusCode} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PROGRAM SERVICE] Error unregistering from program: {ex.Message}");
            return false;
        }
    }
}
