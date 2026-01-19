using Explore.Blazor.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public interface IEventService
{
    Task<ICollection<EventListDto>> GetAllEventsAsync();
    Task<ICollection<EventListDto>> GetMyEventsAsync();
    Task<EventDto?> GetEventByIdAsync(Guid eventId);
    Task<bool> DeleteEventAsync(Guid eventId);
    Task<bool> CanDeleteEventAsync(Guid eventId);
    Task<BaseCommandResponseOfGuid?> UpdateEventAsync(Guid eventId, UpdateEventDto eventDto);
    Task<BaseCommandResponseOfGuid?> CreateEventAsync(CreateEventDto createDto);
    Task<ICollection<EventTypeListDto>> GetEventTypesAsync();
    Task<ICollection<EventFormatListDto>> GetEventFormatsAsync();
    Task<ICollection<EventSessionListDto>> GetAllSessionsAsync();
    Task<ICollection<EventSessionListDto>> GetSessionsByEventAsync(Guid eventId);
    Task<ICollection<EventSessionLanguageListDto>> GetAllSessionLanguagesAsync();
    Task<BaseCommandResponseOfGuid?> CreateSessionAsync(CreateEventSessionDto session);
    Task<BaseCommandResponseOfGuid?> UpdateSessionAsync(UpdateEventSessionDto session);
    Task<bool> DeleteSessionAsync(Guid sessionId);
    Task<BaseCommandResponseOfint?> AssignLanguageToSessionAsync(CreateEventSessionLanguageDto sessionLanguage);
    Task<bool> DeleteSessionLanguageAsync(int sessionLanguageId);
    Task<BaseCommandResponseOfGuid?> RegisterForEventSessionAsync(CreateEventRegistrationDto registration);
    Task<ICollection<EventRegistrationListDto>> GetRegistrationsForSessionAsync(Guid sessionId);
    Task<ICollection<EventRegistrationListDto>> GetRegistrationsByUserAsync(Guid userId);
    Task<BaseCommandResponseOfGuid?> UpdateRegistrationAsync(UpdateEventRegistrationDto registration);
    Task<bool> CancelEventRegistrationAsync(Guid registrationId);
    Task<EventSessionDto?> GetSessionByIdAsync(Guid sessionId);
}

public partial class EventService : IEventService
{
    private readonly IEventApiClient _apiClient;
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IEventApiClient apiClient,
        IOrganizationService organizationService,
        ILogger<EventService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ICollection<EventListDto>> GetMyEventsAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                _logger.LogWarning("[EVENT SERVICE] API client is null");
                return new List<EventListDto>();
            }

            _logger.LogInformation("[EVENT SERVICE] Fetching my events...");
            var response = await _apiClient.MyAsync(pageNumber: 1, pageSize: 100);
            _logger.LogInformation("[EVENT SERVICE] Received {Count} events from {Total} total", response?.Items?.Count ?? 0, response?.TotalCount ?? 0);
            return response?.Items ?? new List<EventListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error fetching my events: {StatusCode}", ex.StatusCode);
            return new List<EventListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching my events");
            return new List<EventListDto>();
        }
    }

    public async Task<ICollection<EventListDto>> GetAllEventsAsync()
    {
        try
        {
            if (_apiClient == null)
            {
                _logger.LogWarning("[EVENT SERVICE] API client is null");
                return new List<EventListDto>();
            }

            _logger.LogInformation("[EVENT SERVICE] Fetching all events...");
            var response = await _apiClient.EventGETAsync(pageNumber: 1, pageSize: 100);
            _logger.LogInformation("[EVENT SERVICE] Received {Count} events from {Total} total", response?.Items?.Count ?? 0, response?.TotalCount ?? 0);
            return response?.Items ?? new List<EventListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error fetching events: {StatusCode}", ex.StatusCode);
            return new List<EventListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching events");
            return new List<EventListDto>();
        }
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid eventId)
    {
        try
        {
            if (_apiClient == null)
            {
                _logger.LogWarning("[EVENT SERVICE] API client is null");
                return null;
            }

            return await _apiClient.EventGET2Async(eventId);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("[EVENT SERVICE] Event not found: {EventId}", eventId);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error fetching event {EventId}: {StatusCode}", eventId, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching event {EventId}", eventId);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateEventAsync(Guid eventId, UpdateEventDto eventDto)
    {
        try
        {
            if (_apiClient == null)
            {
                _logger.LogWarning("[EVENT SERVICE] API client is null");
                return new BaseCommandResponseOfGuid { Success = false, Message = "API client not available" };
            }

            return await _apiClient.EventPUTAsync(eventId, eventDto);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error updating event {EventId}: {StatusCode}", eventId, ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error: {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error updating event {EventId}", eventId);
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
                _logger.LogWarning("[EVENT SERVICE] API client is null");
                return false;
            }

            await _apiClient.EventDELETEAsync(eventId);
            return true;
        }
        catch (ApiException ex) when (ex.StatusCode == 204)
        {
            // 204 No Content is success for DELETE operations
            // NSwag may throw if OpenAPI spec doesn't explicitly document 204
            _logger.LogDebug("[EVENT SERVICE] Event {EventId} deleted successfully (204 No Content)", eventId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error deleting event {EventId}: {StatusCode}", eventId, ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error deleting event {EventId}", eventId);
            return false;
        }
    }

    public async Task<bool> CanDeleteEventAsync(Guid eventId)
    {
        try
        {
            if (_apiClient == null)
            {
                _logger.LogWarning("[EVENT SERVICE] API client is null");
                return false;
            }

            // Get event details
            var eventDto = await _apiClient.EventGET2Async(eventId);
            if (eventDto == null)
            {
                _logger.LogWarning("[EVENT SERVICE] Event {EventId} not found", eventId);
                return false;
            }

            // Get actor details to determine ownership
            var actor = await _apiClient.ActorGET2Async(eventDto.ActorId);
            if (actor == null)
            {
                _logger.LogWarning("[EVENT SERVICE] Actor {ActorId} not found for event {EventId}", eventDto.ActorId, eventId);
                return false;
            }

            // If this is an organization event, check if user is a member with appropriate role
            if (actor.OrganizationId.HasValue)
            {
                var myOrganizations = await _organizationService.GetMyOrganizationsAsync();
                var membershipInOrg = myOrganizations.FirstOrDefault(o => o.Id == actor.OrganizationId.Value);

                if (membershipInOrg == null)
                {
                    _logger.LogDebug(
                        "[EVENT SERVICE] User is not a member of organization {OrgId} for event {EventId}",
                        actor.OrganizationId.Value,
                        eventId);
                    return false;
                }

                // Check if user has Creator (1), CoOwner (2), or Admin (3) role
                // CurrentUserRole contains the user's role in this organization
                var canDelete = membershipInOrg.CurrentUserRole.HasValue &&
                    (membershipInOrg.CurrentUserRole.Value == 1 ||  // Creator
                     membershipInOrg.CurrentUserRole.Value == 2 ||  // CoOwner
                     membershipInOrg.CurrentUserRole.Value == 3);   // Admin

                _logger.LogDebug(
                    "[EVENT SERVICE] Authorization check for event {EventId}: OrgId={OrgId}, UserRole={Role}, CanDelete={CanDelete}",
                    eventId,
                    actor.OrganizationId.Value,
                    membershipInOrg.CurrentUserRole,
                    canDelete);

                return canDelete;
            }

            // If this is a personal event (actor.UserId is set), assume user can delete their own events
            // The backend will perform the actual authorization check
            if (actor.UserId.HasValue)
            {
                _logger.LogDebug("[EVENT SERVICE] Event {EventId} is a personal event, allowing delete", eventId);
                return true;
            }

            _logger.LogWarning(
                "[EVENT SERVICE] Actor {ActorId} has neither OrganizationId nor UserId for event {EventId}",
                eventDto.ActorId,
                eventId);
            return false;
        }
        catch (ApiException ex)
        {
            _logger.LogError(
                ex,
                "[EVENT SERVICE] API error checking delete authorization for event {EventId}: {StatusCode}",
                eventId,
                ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error checking delete authorization for event {EventId}", eventId);
            return false;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateEventAsync(CreateEventDto createDto)
    {
        try
        {
            if (_apiClient == null)
            {
                _logger.LogWarning("[EVENT SERVICE] API client is null");
                return new BaseCommandResponseOfGuid { Success = false, Message = "API client not available" };
            }

            _logger.LogInformation("[EVENT SERVICE] Creating event: {Title}", createDto.Title);
            _logger.LogInformation("[EVENT SERVICE] OrganizationId: {OrganizationId}", createDto.OrganizationId);
            _logger.LogInformation("[EVENT SERVICE] EventTypeId: {EventTypeId}", createDto.EventTypeId);
            _logger.LogInformation("[EVENT SERVICE] FirstSessionDate: {FirstSessionDate}", createDto.FirstSessionDate);
            _logger.LogInformation("[EVENT SERVICE] LastSessionDate: {LastSessionDate}", createDto.LastSessionDate);
            _logger.LogInformation("[EVENT SERVICE] FeaturedImageId: {FeaturedImageId}", createDto.FeaturedImageId);

            var response = await _apiClient.EventPOSTAsync(createDto);

            _logger.LogInformation("[EVENT SERVICE] Create response: Success={Success}, Id={Id}", response?.Success, response?.Id);
            return response;
        }
        catch (ApiException ex) when (ex.StatusCode == 200 || ex.StatusCode == 201)
        {
            // Sometimes NSwag throws when response is successful but body doesn't match expected schema
            _logger.LogWarning(ex, "[EVENT SERVICE] Event created but response parsing had issue (status {StatusCode})", ex.StatusCode);
            return new BaseCommandResponseOfGuid
            {
                Success = true,
                Message = "Event created successfully"
            };
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error creating event: {StatusCode}", ex.StatusCode);
            _logger.LogError("[EVENT SERVICE] Response: {Response}", ex.Response);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = $"API error ({ex.StatusCode}): {ex.Message}",
                Errors = new List<string> { ex.Response ?? ex.Message }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error creating event");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<EventSessionDto?> GetSessionByIdAsync(Guid sessionId)
    {
        try
        {
            if (_apiClient == null)
            {
                _logger.LogWarning("[EVENT SERVICE] API client is null");
                return null;
            }

            return await _apiClient.EventSessionGET2Async(sessionId);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("[EVENT SERVICE] Session not found: {SessionId}", sessionId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] Error fetching session {SessionId}", sessionId);
            return null;
        }
    }
}
