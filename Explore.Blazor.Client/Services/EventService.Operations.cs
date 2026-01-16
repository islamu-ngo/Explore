using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public partial class EventService
{
    public async Task<ICollection<EventTypeListDto>> GetEventTypesAsync()
    {
        try
        {
            _logger.LogInformation("[EVENT SERVICE] Fetching event types...");
            var response = await _apiClient.EventTypeAllAsync();
            _logger.LogInformation("[EVENT SERVICE] Received {Count} event types", response?.Count ?? 0);
            return response ?? new List<EventTypeListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error fetching event types: {StatusCode}", ex.StatusCode);
            return new List<EventTypeListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching event types");
            return new List<EventTypeListDto>();
        }
    }

    public async Task<ICollection<EventFormatListDto>> GetEventFormatsAsync()
    {
        try
        {
            _logger.LogInformation("[EVENT SERVICE] Fetching event formats...");
            var response = await _apiClient.EventFormatAllAsync();
            _logger.LogInformation("[EVENT SERVICE] Received {Count} event formats", response?.Count ?? 0);
            return response ?? new List<EventFormatListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error fetching event formats: {StatusCode}", ex.StatusCode);
            return new List<EventFormatListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching event formats");
            return new List<EventFormatListDto>();
        }
    }

    public async Task<ICollection<EventSessionListDto>> GetAllSessionsAsync()
    {
        try
        {
            _logger.LogInformation("[EVENT SERVICE] Fetching all sessions...");
            var response = await _apiClient.EventSessionGETAsync(pageNumber: 1, pageSize: 100);
            _logger.LogInformation("[EVENT SERVICE] Received {Count} sessions from {Total} total", response?.Items?.Count ?? 0, response?.TotalCount ?? 0);
            return response?.Items ?? new List<EventSessionListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error fetching sessions: {StatusCode}", ex.StatusCode);
            return new List<EventSessionListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching sessions");
            return new List<EventSessionListDto>();
        }
    }

    public async Task<ICollection<EventSessionListDto>> GetSessionsByEventAsync(Guid eventId)
    {
        try
        {
            _logger.LogInformation("[EVENT SERVICE] Fetching sessions for event {EventId}...", eventId);
            var response = await _apiClient.ByEvent2Async(eventId);
            _logger.LogInformation("[EVENT SERVICE] Received {Count} sessions", response?.Count ?? 0);
            return response ?? new List<EventSessionListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error fetching event sessions: {StatusCode}", ex.StatusCode);
            return new List<EventSessionListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching event sessions");
            return new List<EventSessionListDto>();
        }
    }

    public async Task<ICollection<EventSessionLanguageListDto>> GetAllSessionLanguagesAsync()
    {
        try
        {
            _logger.LogInformation("[EVENT SERVICE] Fetching all session languages...");
            var response = await _apiClient.EventSessionLanguageGETAsync(pageNumber: 1, pageSize: 100);
            _logger.LogInformation("[EVENT SERVICE] Received {Count} session languages from {Total} total", response?.Items?.Count ?? 0, response?.TotalCount ?? 0);
            return response?.Items ?? new List<EventSessionLanguageListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error fetching session languages: {StatusCode}", ex.StatusCode);
            return new List<EventSessionLanguageListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching session languages");
            return new List<EventSessionLanguageListDto>();
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateSessionAsync(CreateEventSessionDto session)
    {
        try
        {
            return await _apiClient.EventSessionPOSTAsync(session);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error creating session: {StatusCode}", ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error creating session");
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateSessionAsync(UpdateEventSessionDto session)
    {
        try
        {
            return await _apiClient.EventSessionPUTAsync(session.Id, session);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error updating session: {StatusCode}", ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error updating session");
            return null;
        }
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        try
        {
            await _apiClient.EventSessionDELETEAsync(sessionId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error deleting session: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error deleting session");
            return false;
        }
    }

    public async Task<BaseCommandResponseOfint?> AssignLanguageToSessionAsync(CreateEventSessionLanguageDto sessionLanguage)
    {
        try
        {
            return await _apiClient.EventSessionLanguagePOSTAsync(sessionLanguage);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error assigning session language: {StatusCode}", ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error assigning session language");
            return null;
        }
    }

    public async Task<bool> DeleteSessionLanguageAsync(int sessionLanguageId)
    {
        try
        {
            await _apiClient.EventSessionLanguageDELETEAsync(sessionLanguageId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error deleting session language: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error deleting session language");
            return false;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> RegisterForEventSessionAsync(CreateEventRegistrationDto registration)
    {
        try
        {
            return await _apiClient.EventRegistrationPOSTAsync(registration);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error registering for session: {StatusCode}", ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error registering for session");
            return null;
        }
    }

    public async Task<ICollection<EventRegistrationListDto>> GetRegistrationsForSessionAsync(Guid sessionId)
    {
        try
        {
            var response = await _apiClient.BySessionAsync(sessionId);
            return response ?? new List<EventRegistrationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error fetching registrations: {StatusCode}", ex.StatusCode);
            return new List<EventRegistrationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching registrations");
            return new List<EventRegistrationListDto>();
        }
    }

    public async Task<ICollection<EventRegistrationListDto>> GetRegistrationsByUserAsync(Guid userId)
    {
        try
        {
            var response = await _apiClient.ByUserAsync(userId);
            return response ?? new List<EventRegistrationListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error fetching user registrations: {StatusCode}", ex.StatusCode);
            return new List<EventRegistrationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching user registrations");
            return new List<EventRegistrationListDto>();
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateRegistrationAsync(UpdateEventRegistrationDto registration)
    {
        try
        {
            return await _apiClient.EventRegistrationPUTAsync(registration.Id, registration);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error updating registration: {StatusCode}", ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error updating registration");
            return null;
        }
    }

    public async Task<bool> CancelEventRegistrationAsync(Guid registrationId)
    {
        try
        {
            await _apiClient.EventRegistrationDELETEAsync(registrationId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error canceling registration: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error canceling registration");
            return false;
        }
    }
}
