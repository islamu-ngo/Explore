// ABOUTME: Service for managing Events via API calls.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public interface IEventService
{
    Task<ICollection<EventListDto>> GetAllEventsAsync();
    Task<ICollection<EventListDto>> GetMyEventsAsync();
    Task<PaginatedResult<EventListDto>> GetEventsPagedAsync(int pageNumber, int pageSize);
    Task<PaginatedResult<EventListDto>> GetEventsPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        Guid? categoryId = null,
        Guid? tagId = null,
        int? formatId = null,
        int? madhabId = null,
        Guid? locationId = null,
        int? registrationModeId = null,
        int? languageId = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        string? sortBy = null,
        bool? sortDescending = null,
        CancellationToken cancellationToken = default);
    Task<PaginatedResult<EventListDto>> GetMyEventsPagedAsync(int pageNumber, int pageSize);
    Task<PaginatedResult<EventSessionListDto>> GetSessionsPagedAsync(int pageNumber, int pageSize);
    Task<EventDto?> GetEventByIdAsync(Guid eventId);
    Task<bool> DeleteEventAsync(Guid eventId);
    Task<bool> CanDeleteEventAsync(Guid eventId);
    Task<BaseCommandResponseOfGuid?> UpdateEventAsync(Guid eventId, UpdateEventDto eventDto);
    Task<BaseCommandResponseOfGuid?> CreateEventAsync(CreateEventDto createDto);
    Task<ICollection<EventTypeListDto>> GetEventTypesAsync();
    Task<ICollection<EventFormatListDto>> GetEventFormatsAsync();
    Task<ICollection<EventSessionListDto>> GetAllSessionsAsync();
    Task<ICollection<EventSessionListDto>> GetSessionsByEventAsync(Guid eventId);
    Task<ICollection<object>> GetAllSessionLanguagesAsync(); // Neutralized
    Task<BaseCommandResponseOfGuid?> CreateSessionAsync(CreateEventSessionDto session);
    Task<BaseCommandResponseOfGuid?> UpdateSessionAsync(UpdateEventSessionDto session);
    Task<bool> DeleteSessionAsync(Guid sessionId);
    Task<BaseCommandResponseOfGuid?> AssignLanguageToSessionAsync(object sessionLanguage); // Neutralized
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
            var result = await _apiClient.GetMyEventsAsync(1, 100);
            return result?.GetItems() ?? new List<EventListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching my events");
            return new List<EventListDto>();
        }
    }

    public async Task<ICollection<EventListDto>> GetAllEventsAsync()
    {
        try
        {
            var result = await _apiClient.GetEventsAsync(1, 100);
            return result?.GetItems() ?? new List<EventListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all events");
            return new List<EventListDto>();
        }
    }

    public async Task<PaginatedResult<EventListDto>> GetEventsPagedAsync(int pageNumber, int pageSize)
    {
        try
        {
            var result = await _apiClient.GetEventsAsync(pageNumber, pageSize);
            return result.ToPaginatedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paged events (page {PageNumber}, size {PageSize})", pageNumber, pageSize);
            return PaginatedResult<EventListDto>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<PaginatedResult<EventListDto>> GetEventsPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        Guid? categoryId = null,
        Guid? tagId = null,
        int? formatId = null,
        int? madhabId = null,
        Guid? locationId = null,
        int? registrationModeId = null,
        int? languageId = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        string? sortBy = null,
        bool? sortDescending = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _apiClient.GetEventsAsync(
                pageNumber: pageNumber,
                pageSize: pageSize,
                searchTerm: searchTerm,
                categoryId: categoryId,
                tagId: tagId,
                formatId: formatId,
                madhabId: madhabId,
                locationId: locationId,
                registrationModeId: registrationModeId,
                languageId: languageId,
                dateFrom: dateFrom,
                dateTo: dateTo,
                sortBy: sortBy,
                sortDescending: sortDescending,
                cancellationToken: cancellationToken);
            return result.ToPaginatedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching filtered paged events (page {PageNumber}, size {PageSize})", pageNumber, pageSize);
            return PaginatedResult<EventListDto>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<PaginatedResult<EventListDto>> GetMyEventsPagedAsync(int pageNumber, int pageSize)
    {
        try
        {
            var result = await _apiClient.GetMyEventsAsync(pageNumber, pageSize);
            return result.ToPaginatedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching my paged events (page {PageNumber}, size {PageSize})", pageNumber, pageSize);
            return PaginatedResult<EventListDto>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<PaginatedResult<EventSessionListDto>> GetSessionsPagedAsync(int pageNumber, int pageSize)
    {
        try
        {
            var result = await _apiClient.GetEventSessionsListAsync(pageNumber, pageSize);
            return result.ToPaginatedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paged sessions (page {PageNumber}, size {PageSize})", pageNumber, pageSize);
            return PaginatedResult<EventSessionListDto>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid eventId)
    {
        try
        {
            var result = await _apiClient.GetEventByIdAsync(eventId);
            return result?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event {EventId}", eventId);
            return null;
        }
    }

    public async Task<bool> DeleteEventAsync(Guid eventId)
    {
        try
        {
            await _apiClient.DeleteEventAsync(eventId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", eventId);
            return false;
        }
    }

    public async Task<bool> CanDeleteEventAsync(Guid eventId)
    {
        try
        {
            var eventResource = await _apiClient.GetEventByIdAsync(eventId);
            if (eventResource?.ActorId is null) return false;

            var actorResource = await _apiClient.GetActorByIdAsync(eventResource.ActorId.Value);
            if (actorResource?.OrganizationId is null) return true; // It's a personal event

            var myOrgs = await _organizationService.GetMyOrganizationsAsync();
            return myOrgs?.Any(o => o.Id == actorResource.OrganizationId.Value) ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking CanDeleteEvent for {EventId}", eventId);
            return false;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UpdateEventAsync(Guid eventId, UpdateEventDto eventDto)
    {
        try
        {
            return await _apiClient.UpdateEventAsync(eventId, eventDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", eventId);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateEventAsync(CreateEventDto createDto)
    {
        try
        {
            return await _apiClient.CreateEventAsync(createDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            return null;
        }
    }

    public Task<ICollection<EventTypeListDto>> GetEventTypesAsync() => _apiClient.EventtypeAllAsync();

    public Task<ICollection<EventFormatListDto>> GetEventFormatsAsync() => _apiClient.EventformatAllAsync();

    public async Task<ICollection<EventSessionListDto>> GetAllSessionsAsync()
    {
        var result = await _apiClient.GetEventSessionsListAsync(1, 100);
        return result?.GetItems() ?? new List<EventSessionListDto>();
    }

    public async Task<ICollection<EventSessionListDto>> GetSessionsByEventAsync(Guid eventId)
    {
        var result = await _apiClient.GetEventSessionsAsync(eventId);
        return result?.GetItems() ?? new List<EventSessionListDto>();
    }

    public Task<ICollection<object>> GetAllSessionLanguagesAsync()
    {
        // TODO: Fix this when API client is regenerated.
        return Task.FromResult<ICollection<object>>(new List<object>());
    }

    public Task<BaseCommandResponseOfGuid?> CreateSessionAsync(CreateEventSessionDto session) => _apiClient.CreateEventSessionAsync(session);

    public Task<BaseCommandResponseOfGuid?> UpdateSessionAsync(UpdateEventSessionDto session) => _apiClient.UpdateEventSessionAsync(session.Id ?? Guid.Empty, session);

    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        try { await _apiClient.DeleteEventSessionAsync(sessionId); return true; } catch { return false; }
    }

    public Task<BaseCommandResponseOfGuid?> AssignLanguageToSessionAsync(object sessionLanguage)
    {
        // TODO: Fix this when API client is regenerated.
        return Task.FromResult<BaseCommandResponseOfGuid?>(null);
    }

    public Task<bool> DeleteSessionLanguageAsync(int sessionLanguageId) => throw new NotImplementedException();

    public Task<BaseCommandResponseOfGuid?> RegisterForEventSessionAsync(CreateEventRegistrationDto registration) => _apiClient.EventregistrationPOSTAsync(registration);

    public Task<ICollection<EventRegistrationListDto>> GetRegistrationsForSessionAsync(Guid sessionId) => _apiClient.BySessionAsync(sessionId);

    public Task<ICollection<EventRegistrationListDto>> GetRegistrationsByUserAsync(Guid userId) => _apiClient.ByUserAsync(userId);

    public Task<BaseCommandResponseOfGuid?> UpdateRegistrationAsync(UpdateEventRegistrationDto registration) => _apiClient.EventregistrationPUTAsync(registration.Id ?? Guid.Empty, registration);

    public async Task<bool> CancelEventRegistrationAsync(Guid registrationId)
    {
        try { await _apiClient.EventregistrationDELETEAsync(registrationId); return true; } catch { return false; }
    }

    public async Task<EventSessionDto?> GetSessionByIdAsync(Guid sessionId)
    {
        try
        {
            var result = await _apiClient.GetEventSessionByIdAsync(sessionId);
            return result?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching session {SessionId}", sessionId);
            return null;
        }
    }
}
