// ABOUTME: Service for managing Events via API calls.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.EventSessionGroups;
using Explore.Blazor.Client.Models.EventSessions;

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
        List<Guid>? includedCategoryIds = null,
        List<Guid>? excludedCategoryIds = null,
        string? categoryInclusionMode = null,
        string? categoryExclusionMode = null,
        List<Guid>? includedTagIds = null,
        List<Guid>? excludedTagIds = null,
        string? inclusionMode = null,
        string? exclusionMode = null,
        List<int>? formatIds = null,
        List<int>? madhabIds = null,
        List<Guid>? locationIds = null,
        List<int>? registrationModeIds = null,
        List<int>? languageIds = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        string? sortBy = null,
        bool? sortDescending = null,
        List<int>? eventTypeIds = null,
        List<int>? audienceGenderIds = null,
        List<int>? audienceAgeIds = null,
        List<int>? eventStatusIds = null,
        // Islamic filters
        List<int>? genderModeIds = null,
        bool? includesQuranRecitation = null,
        List<int>? referencePrayerIds = null,
        List<int>? islamicPrimaryLanguageIds = null,
        bool? hasIslamicAspect = null,
        // Tech filters
        int? skillLevelId = null,
        bool? isCodingCompetition = null,
        bool? isHackathon = null,
        bool? requiresLaptop = null,
        string? techStackTag = null,
        bool? hasTechAspect = null,
        Guid? actorId = null,
        Guid? organizationId = null,
        Guid? groupId = null,
        CancellationToken cancellationToken = default);
    Task<PaginatedResult<EventListDto>> GetMyEventsPagedAsync(int pageNumber, int pageSize);
    Task<PaginatedResult<EventSessionListDto>> GetSessionsPagedAsync(int pageNumber, int pageSize);
    Task<EventDto?> GetEventByIdAsync(Guid eventId);
    Task<EventCreationContextDto?> GetEventCreationContextAsync(CancellationToken cancellationToken = default);
    Task<EventSessionCreateContextDto?> GetEventSessionCreateContextAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventProgramSummaryDto?> GetEventProgramSummaryAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventPublishReadinessDto?> GetEventPublishReadinessAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventAsync(Guid eventId);
    Task<BaseCommandResponseOfGuid?> UpdateEventAsync(Guid eventId, UpdateEventDraftRequestDto request);
    Task<BaseCommandResponseOfGuid?> CreateEventAsync(CreateEventDraftRequestDto request, string? idempotencyKey = null);
    Task<BaseCommandResponseOfGuid?> PublishEventAsync(Guid eventId, Guid expectedConcurrencyStamp, CancellationToken cancellationToken = default);
    Task<ICollection<EventTypeListDto>> GetEventTypesAsync();
    Task<ICollection<EventFormatListDto>> GetEventFormatsAsync();
    Task<ICollection<EventSessionListDto>> GetAllSessionsAsync();
    Task<ICollection<EventSessionListDto>> GetSessionsByEventAsync(Guid eventId);
    Task<BaseCommandResponseOfGuid> CreateSessionAsync(CreateEventSessionRequest session);
    Task<BaseCommandResponseOfGuid> UpdateSessionAsync(UpdateEventSessionRequest session);
    Task<bool> DeleteSessionAsync(Guid sessionId);
    Task<ICollection<EventSessionGroupListModel>> GetSessionGroupsByEventAsync(Guid eventId);
    Task<BaseCommandResponseOfGuid> CreateSessionGroupAsync(CreateEventSessionGroupRequestDto group);
    Task<BaseCommandResponseOfGuid> UpdateSessionGroupAsync(UpdateEventSessionGroupRequestDto group);
    Task<bool> DeleteSessionGroupAsync(Guid eventId, Guid sessionGroupId);
    Task<BaseCommandResponseOfGuid> AssignSessionToGroupAsync(Guid eventId, Guid eventSessionGroupId, Guid eventSessionId, bool isPrimary = true, int sortOrder = 0);
    Task<BaseCommandResponseOfGuid> UnassignSessionFromGroupAsync(Guid eventId, Guid eventSessionGroupId, Guid eventSessionId);
    Task<BaseCommandResponseOfGuid> RegisterForEventSessionAsync(CreateEventRegistrationDto registration);
    Task<ICollection<EventRegistrationListDto>> GetRegistrationsForSessionAsync(Guid sessionId);
    Task<ICollection<EventRegistrationListDto>> GetRegistrationsByUserAsync(Guid userId);
    Task<BaseCommandResponseOfGuid> UpdateRegistrationAsync(UpdateEventRegistrationDto registration);
    Task<bool> CancelEventRegistrationAsync(Guid registrationId);
    Task<EventSessionDto?> GetSessionByIdAsync(Guid sessionId);
    Task<bool> UpdateEventStatusAsync(Guid eventId, int eventStatusId);
    Task<ICollection<EventListDto>> GetPublicEventsByActorAsync(Guid actorId);
    Task<ICollection<EventListDto>> GetPublicEventsByOrganizationAsync(Guid organizationId);
    Task<ICollection<EventListDto>> GetPublicEventsByGroupAsync(Guid groupId);
}

public partial class EventService : IEventService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IEventApiClient apiClient,
        ILogger<EventService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
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
        List<Guid>? includedCategoryIds = null,
        List<Guid>? excludedCategoryIds = null,
        string? categoryInclusionMode = null,
        string? categoryExclusionMode = null,
        List<Guid>? includedTagIds = null,
        List<Guid>? excludedTagIds = null,
        string? inclusionMode = null,
        string? exclusionMode = null,
        List<int>? formatIds = null,
        List<int>? madhabIds = null,
        List<Guid>? locationIds = null,
        List<int>? registrationModeIds = null,
        List<int>? languageIds = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        string? sortBy = null,
        bool? sortDescending = null,
        List<int>? eventTypeIds = null,
        List<int>? audienceGenderIds = null,
        List<int>? audienceAgeIds = null,
        List<int>? eventStatusIds = null,
        // Islamic filters
        List<int>? genderModeIds = null,
        bool? includesQuranRecitation = null,
        List<int>? referencePrayerIds = null,
        List<int>? islamicPrimaryLanguageIds = null,
        bool? hasIslamicAspect = null,
        // Tech filters
        int? skillLevelId = null,
        bool? isCodingCompetition = null,
        bool? isHackathon = null,
        bool? requiresLaptop = null,
        string? techStackTag = null,
        bool? hasTechAspect = null,
        Guid? actorId = null,
        Guid? organizationId = null,
        Guid? groupId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Sanitize empty lists to null to prevent NSwag URL builder corruption.
            var safeIncludedCatIds = includedCategoryIds is { Count: > 0 } ? includedCategoryIds : null;
            var safeExcludedCatIds = excludedCategoryIds is { Count: > 0 } ? excludedCategoryIds : null;
            var safeIncludedTagIds = includedTagIds is { Count: > 0 } ? includedTagIds : null;
            var safeExcludedTagIds = excludedTagIds is { Count: > 0 } ? excludedTagIds : null;
            var safeFormatIds = formatIds is { Count: > 0 } ? formatIds : null;
            var safeMadhabIds = madhabIds is { Count: > 0 } ? madhabIds : null;
            var safeLocationIds = locationIds is { Count: > 0 } ? locationIds : null;
            var safeRegistrationModeIds = registrationModeIds is { Count: > 0 } ? registrationModeIds : null;
            var safeLanguageIds = languageIds is { Count: > 0 } ? languageIds : null;
            var safeEventTypeIds = eventTypeIds is { Count: > 0 } ? eventTypeIds : null;
            var safeAudienceGenderIds = audienceGenderIds is { Count: > 0 } ? audienceGenderIds : null;
            var safeAudienceAgeIds = audienceAgeIds is { Count: > 0 } ? audienceAgeIds : null;
            var safeEventStatusIds = eventStatusIds is { Count: > 0 } ? eventStatusIds : null;
            var safeGenderModeIds = genderModeIds is { Count: > 0 } ? genderModeIds : null;
            var safeReferencePrayerIds = referencePrayerIds is { Count: > 0 } ? referencePrayerIds : null;
            var safeIslamicPrimaryLanguageIds = islamicPrimaryLanguageIds is { Count: > 0 } ? islamicPrimaryLanguageIds : null;

            // Only send mode strings when the corresponding ID list is non-empty
            var safeCatIncMode = safeIncludedCatIds != null ? categoryInclusionMode : null;
            var safeCatExcMode = safeExcludedCatIds != null ? categoryExclusionMode : null;
            var safeTagIncMode = safeIncludedTagIds != null ? inclusionMode : null;
            var safeTagExcMode = safeExcludedTagIds != null ? exclusionMode : null;

            var result = await _apiClient.GetEventsAsync(
                pageNumber: pageNumber,
                pageSize: pageSize,
                searchTerm: searchTerm,
                actorId: actorId,
                organizationId: organizationId,
                groupId: groupId,
                categoryId: categoryId,
                includedCategoryIds: safeIncludedCatIds,
                excludedCategoryIds: safeExcludedCatIds,
                categoryInclusionMode: safeCatIncMode,
                categoryExclusionMode: safeCatExcMode,
                includedTagIds: safeIncludedTagIds,
                excludedTagIds: safeExcludedTagIds,
                inclusionMode: safeTagIncMode,
                exclusionMode: safeTagExcMode,
                formatIds: safeFormatIds,
                madhabIds: safeMadhabIds,
                locationIds: safeLocationIds,
                registrationModeIds: safeRegistrationModeIds,
                languageIds: safeLanguageIds,
                dateFrom: dateFrom,
                dateTo: dateTo,
                sortBy: sortBy,
                sortDescending: sortDescending,
                eventTypeIds: safeEventTypeIds,
                audienceGenderIds: safeAudienceGenderIds,
                audienceAgeIds: safeAudienceAgeIds,
                eventStatusIds: safeEventStatusIds,
                genderModeIds: safeGenderModeIds,
                includesQuranRecitation: includesQuranRecitation,
                referencePrayerIds: safeReferencePrayerIds,
                islamicPrimaryLanguageIds: safeIslamicPrimaryLanguageIds,
                hasIslamicAspect: hasIslamicAspect,
                skillLevelId: skillLevelId,
                isCodingCompetition: isCodingCompetition,
                isHackathon: isHackathon,
                requiresLaptop: requiresLaptop,
                techStackTag: techStackTag,
                hasTechAspect: hasTechAspect,
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

    public async Task<EventCreationContextDto?> GetEventCreationContextAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.GetEventCreationContextAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event creation context");
            return null;
        }
    }

    public async Task<EventSessionCreateContextDto?> GetEventSessionCreateContextAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.GetEventSessionCreateContextAsync(eventId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event session create context {EventId}", eventId);
            return null;
        }
    }

    public async Task<EventProgramSummaryDto?> GetEventProgramSummaryAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.GetEventProgramSummaryAsync(eventId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event program summary {EventId}", eventId);
            return null;
        }
    }

    public async Task<EventPublishReadinessDto?> GetEventPublishReadinessAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.GetEventPublishReadinessAsync(eventId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event publish readiness {EventId}", eventId);
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

    public async Task<BaseCommandResponseOfGuid?> UpdateEventAsync(Guid eventId, UpdateEventDraftRequestDto request)
    {
        try
        {
            return await _apiClient.UpdateEventAsync(eventId, request);
        }
        catch (ApiException<ProblemDetails> ex) when (ex.StatusCode == 409)
        {
            _logger.LogWarning(ex, "Event draft update rejected as stale for event {EventId}", eventId);
            return new BaseCommandResponseOfGuid
            {
                Id = eventId,
                Success = false,
                Message = ex.Result?.Detail ?? ex.Result?.Title ?? "Event draft changed since it was loaded.",
                Errors = ["Refresh the event and try again."],
                FailureCode = "event_draft_concurrency_conflict"
            };
        }
        catch (ApiException<BaseCommandResponseOfGuid> ex)
        {
            _logger.LogWarning(ex, "Event draft update rejected for event {EventId}", eventId);
            return ex.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", eventId);
            return null;
        }
    }

    public async Task<bool> UpdateEventStatusAsync(Guid eventId, int eventStatusId)
    {
        try
        {
            var command = new UpdateEventStatusDto { EventStatusId = eventStatusId };
            var result = await _apiClient.UpdateEventStatusAsync(eventId, command);
            return result?.Success ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event status {EventId}", eventId);
            return false;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CreateEventAsync(CreateEventDraftRequestDto request, string? idempotencyKey = null)
    {
        try
        {
            if (_apiClient is EventApiClient generatedClient)
            {
                return await generatedClient.CreateEventWithIdempotencyKeyAsync(
                    request,
                    idempotencyKey ?? Guid.NewGuid().ToString("N"));
            }

            return await _apiClient.CreateEventAsync(request);
        }
        catch (ApiException<BaseCommandResponseOfGuid> ex)
        {
            _logger.LogWarning(ex, "Event creation rejected by API validation");
            return ex.Result;
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _logger.LogWarning(ex, "Event creation rejected with problem details");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = ex.Result?.Detail ?? ex.Result?.Title ?? "Event could not be created."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> PublishEventAsync(Guid eventId, Guid expectedConcurrencyStamp, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.PublishEventAsync(
                eventId,
                new PublishEventRequestDto { ExpectedConcurrencyStamp = expectedConcurrencyStamp },
                cancellationToken: cancellationToken);
        }
        catch (ApiException<BaseCommandResponseOfGuid> ex)
        {
            _logger.LogWarning(ex, "Event publish rejected for event {EventId}", eventId);
            return ex.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventId}", eventId);
            return null;
        }
    }

    public Task<ICollection<EventTypeListDto>> GetEventTypesAsync() => _apiClient.GetEventTypesAsync();

    public Task<ICollection<EventFormatListDto>> GetEventFormatsAsync() => _apiClient.GetEventFormatOptionsAsync();

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

    public Task<BaseCommandResponseOfGuid> CreateSessionAsync(CreateEventSessionRequest session)
        => _apiClient.CreateEventSessionAsync(new CreateEventSessionDto
        {
            EventId = session.EventId,
            TenantId = session.TenantId,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            LocationId = session.LocationId,
            FeaturedImageId = session.FeaturedImageId,
            RoomId = session.RoomId,
            SortOrder = session.SortOrder,
            Title = session.Title,
            EventSessionKindId = session.EventSessionKindId,
            Description = session.Description,
            Slug = session.Slug,
            MaxAudienceAttendees = session.MaxAudienceAttendees,
            RegistrationModeId = session.RegistrationModeId,
            Price = session.Price,
            CurrencyCode = session.CurrencyCode,
            SessionTemplateId = session.SessionTemplateId,
            IslamicAspect = session.IslamicAspect
        });

    public Task<BaseCommandResponseOfGuid> UpdateSessionAsync(UpdateEventSessionRequest session)
        => _apiClient.UpdateEventSessionAsync(session.Id ?? Guid.Empty, new UpdateEventSessionDto
        {
            Id = session.Id,
            EventId = session.EventId,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            LocationId = session.LocationId,
            FeaturedImageId = session.FeaturedImageId,
            RoomId = session.RoomId,
            SortOrder = session.SortOrder,
            Title = session.Title,
            EventSessionKindId = session.EventSessionKindId,
            Description = session.Description,
            Slug = session.Slug,
            MaxAudienceAttendees = session.MaxAudienceAttendees,
            RegistrationModeId = session.RegistrationModeId,
            Price = session.Price,
            CurrencyCode = session.CurrencyCode,
            IslamicAspect = session.IslamicAspect
        });

    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        try { await _apiClient.DeleteEventSessionAsync(sessionId); return true; } catch { return false; }
    }

    public async Task<ICollection<EventSessionGroupListModel>> GetSessionGroupsByEventAsync(Guid eventId)
    {
        try
        {
            var result = await _apiClient.GetEventSessionGroupsByEventAsync(eventId);
            return result?.GetItems() ?? new List<EventSessionGroupListModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching session groups for event {EventId}", eventId);
            return new List<EventSessionGroupListModel>();
        }
    }

    public async Task<BaseCommandResponseOfGuid> CreateSessionGroupAsync(CreateEventSessionGroupRequestDto group)
    {
        try
        {
            return await _apiClient.CreateEventSessionGroupAsync(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating program section for event {EventId}", group.EventId);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Program section could not be created."
            };
        }
    }

    public async Task<BaseCommandResponseOfGuid> UpdateSessionGroupAsync(UpdateEventSessionGroupRequestDto group)
    {
        try
        {
            return await _apiClient.UpdateEventSessionGroupAsync(group.Id ?? Guid.Empty, group);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating program section {SessionGroupId} for event {EventId}",
                group.Id,
                group.EventId);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Program section could not be updated."
            };
        }
    }

    public async Task<bool> DeleteSessionGroupAsync(Guid eventId, Guid sessionGroupId)
    {
        try
        {
            await _apiClient.DeleteEventSessionGroupAsync(sessionGroupId, eventId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting program section {SessionGroupId} for event {EventId}",
                sessionGroupId,
                eventId);
            return false;
        }
    }

    public async Task<BaseCommandResponseOfGuid> AssignSessionToGroupAsync(
        Guid eventId,
        Guid eventSessionGroupId,
        Guid eventSessionId,
        bool isPrimary = true,
        int sortOrder = 0)
    {
        try
        {
            return await _apiClient.AssignEventSessionToGroupAsync(
                eventSessionGroupId,
                new AssignSessionToGroupRequestDto
                {
                    EventId = eventId,
                    EventSessionGroupId = eventSessionGroupId,
                    EventSessionId = eventSessionId,
                    IsPrimary = isPrimary,
                    SortOrder = sortOrder
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error assigning session {SessionId} to program section {SessionGroupId} for event {EventId}",
                eventSessionId,
                eventSessionGroupId,
                eventId);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Session could not be assigned to the selected program section."
            };
        }
    }

    public async Task<BaseCommandResponseOfGuid> UnassignSessionFromGroupAsync(
        Guid eventId,
        Guid eventSessionGroupId,
        Guid eventSessionId)
    {
        try
        {
            await _apiClient.UnassignEventSessionFromGroupAsync(
                eventSessionGroupId,
                eventSessionId,
                eventId);
            return new BaseCommandResponseOfGuid
            {
                Success = true,
                Id = eventSessionId,
                Message = "Session was removed from the selected program section."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error removing session {SessionId} from program section {SessionGroupId} for event {EventId}",
                eventSessionId,
                eventSessionGroupId,
                eventId);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Session could not be removed from the selected program section."
            };
        }
    }

    public Task<BaseCommandResponseOfGuid> RegisterForEventSessionAsync(CreateEventRegistrationDto registration) => _apiClient.CreateEventRegistrationAsync(body: registration);

    public Task<ICollection<EventRegistrationListDto>> GetRegistrationsForSessionAsync(Guid sessionId) => _apiClient.GetRegistrationsBySessionAsync(sessionId);

    public Task<ICollection<EventRegistrationListDto>> GetRegistrationsByUserAsync(Guid userId) => _apiClient.GetRegistrationsByUserAsync(userId);

    public Task<BaseCommandResponseOfGuid> UpdateRegistrationAsync(UpdateEventRegistrationDto registration) => _apiClient.UpdateEventRegistrationAsync(registration.Id ?? Guid.Empty, registration);

    public async Task<bool> CancelEventRegistrationAsync(Guid registrationId)
    {
        try { await _apiClient.DeleteEventRegistrationAsync(registrationId); return true; } catch { return false; }
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

    public async Task<ICollection<EventListDto>> GetPublicEventsByActorAsync(Guid actorId)
    {
        try
        {
            var result = await _apiClient.GetEventsAsync(
                pageNumber: 1,
                pageSize: 100,
                actorId: actorId);
            return result?.GetItems() ?? new List<EventListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching public events for actor {ActorId}", actorId);
            return new List<EventListDto>();
        }
    }

    public async Task<ICollection<EventListDto>> GetPublicEventsByOrganizationAsync(Guid organizationId)
    {
        try
        {
            var result = await _apiClient.GetEventsAsync(
                pageNumber: 1,
                pageSize: 100,
                organizationId: organizationId);
            return result?.GetItems() ?? new List<EventListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching public events for organization {OrganizationId}", organizationId);
            return new List<EventListDto>();
        }
    }

    public async Task<ICollection<EventListDto>> GetPublicEventsByGroupAsync(Guid groupId)
    {
        try
        {
            var result = await _apiClient.GetEventsAsync(
                pageNumber: 1,
                pageSize: 100,
                groupId: groupId);
            return result?.GetItems() ?? new List<EventListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching public events for group {GroupId}", groupId);
            return new List<EventListDto>();
        }
    }
}
