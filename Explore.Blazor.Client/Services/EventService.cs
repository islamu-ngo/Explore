// ABOUTME: Service for managing Events via generated API client calls.
// ABOUTME: Keeps Blazor event pages behind HAL-aware service methods and the BFF typed-client boundary.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models;
using Explore.Blazor.Client.Models.EventSessionGroups;
using Explore.Blazor.Client.Models.EventSessions;
using ComposerCreateEventSessionRequest = Explore.Blazor.Client.Models.EventSessions.CreateEventSessionRequest;

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
    Task<BaseCommandResponseOfGuid?> ModerateEventLightAsync(Guid eventId, CancellationToken cancellationToken = default, string? reasonCode = null, string? correlationId = null);
    Task<BaseCommandResponseOfGuid?> ModerateEventHeavyAsync(Guid eventId, CancellationToken cancellationToken = default, string? reasonCode = null, string? correlationId = null);
    Task<BaseCommandResponseOfGuid?> UnmoderateEventAsync(Guid eventId, CancellationToken cancellationToken = default, string? reasonCode = null, string? correlationId = null);
    Task<ICollection<EventTypeListDto>> GetEventTypesAsync();
    Task<ICollection<EventFormatListDto>> GetEventFormatsAsync();
    Task<ICollection<EventSessionListDto>> GetAllSessionsAsync();
    Task<ICollection<EventSessionListDto>> GetSessionsByEventAsync(Guid eventId, bool includeManagedSessions = false);
    Task<BaseCommandResponseOfGuid> CreateSessionAsync(ComposerCreateEventSessionRequest session);
    Task<BaseCommandResponseOfGuid> UpdateSessionAsync(UpdateEventSessionRequest session);
    Task<BaseCommandResponseOfGuid?> PublishEventSessionAsync(Guid sessionId, Guid expectedConcurrencyStamp, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid?> ArchiveEventSessionAsync(Guid sessionId, Guid expectedConcurrencyStamp, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid?> CancelEventSessionAsync(Guid sessionId, Guid expectedConcurrencyStamp, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid?> CompleteEventSessionAsync(Guid sessionId, Guid expectedConcurrencyStamp, CancellationToken cancellationToken = default);
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
    Task<ICollection<EventListDto>> GetRegistrationEventsByUserAsync(Guid userId);
    Task<ICollection<EventListDto>> GetRegistrationEventsByActorAsync(Guid actorId);
    Task<BaseCommandResponseOfGuid> UpdateRegistrationAsync(UpdateEventRegistrationDto registration);
    Task<bool> CancelEventRegistrationAsync(Guid registrationId);
    Task<EventSessionDto?> GetSessionByIdAsync(Guid sessionId);
    Task<EventSessionDto?> GetManagedSessionByIdAsync(Guid eventId, Guid sessionId);
    Task<BaseCommandResponseOfGuid?> ArchiveEventAsync(Guid eventId, Guid expectedConcurrencyStamp, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid?> CancelEventAsync(Guid eventId, Guid expectedConcurrencyStamp, CancellationToken cancellationToken = default);
    Task<ICollection<EventListDto>> GetProfileEventsByActorAsync(Guid actorId);
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
        catch (ApiException<ProblemDetails> ex) when (ex.StatusCode == 404)
        {
            _logger.LogDebug("Public event detail hidden or missing for event {EventId}; trying authorized management detail.", eventId);
            return await GetEventManagementDetailsAsync(eventId);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogDebug("Public event detail hidden or missing for event {EventId}; trying authorized management detail.", eventId);
            return await GetEventManagementDetailsAsync(eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching event {EventId}", eventId);
            return null;
        }
    }

    private async Task<EventDto?> GetEventManagementDetailsAsync(Guid eventId)
    {
        try
        {
            var result = await _apiClient.GetEventManagementDetailsAsync(eventId);
            return result?.ToDto();
        }
        catch (ApiException ex) when (ex.StatusCode is 401 or 403 or 404)
        {
            _logger.LogDebug(
                "Authorized management event detail was not available for event {EventId}; status {StatusCode}.",
                eventId,
                ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching authorized management event detail {EventId}", eventId);
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

    public async Task<BaseCommandResponseOfGuid?> ArchiveEventAsync(Guid eventId, Guid expectedConcurrencyStamp, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.ArchiveEventAsync(
                eventId,
                new ArchiveEventRequestDto { ExpectedConcurrencyStamp = expectedConcurrencyStamp },
                cancellationToken: cancellationToken);
        }
        catch (ApiException<BaseCommandResponseOfGuid> ex)
        {
            _logger.LogWarning(ex, "Event archive rejected for event {EventId}", eventId);
            return ex.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving event {EventId}", eventId);
            return null;
        }
    }

    public async Task<BaseCommandResponseOfGuid?> CancelEventAsync(Guid eventId, Guid expectedConcurrencyStamp, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _apiClient.CancelEventAsync(
                eventId,
                new CancelEventRequestDto { ExpectedConcurrencyStamp = expectedConcurrencyStamp },
                cancellationToken: cancellationToken);
        }
        catch (ApiException<BaseCommandResponseOfGuid> ex)
        {
            _logger.LogWarning(ex, "Event cancel rejected for event {EventId}", eventId);
            return ex.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling event {EventId}", eventId);
            return null;
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

    public async Task<BaseCommandResponseOfGuid?> ModerateEventLightAsync(
        Guid eventId,
        CancellationToken cancellationToken = default,
        string? reasonCode = null,
        string? correlationId = null)
    {
        try
        {
            return await _apiClient.ModerateEventLightAsync(
                eventId,
                CreateModerationRequest(reasonCode, correlationId),
                cancellationToken: cancellationToken);
        }
        catch (ApiException<BaseCommandResponseOfGuid> ex)
        {
            _logger.LogWarning(ex, "Event moderation rejected for event {EventId}", eventId);
            return ex.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moderating event {EventId}", eventId);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Event could not be moderated."
            };
        }
    }

    public async Task<BaseCommandResponseOfGuid?> ModerateEventHeavyAsync(
        Guid eventId,
        CancellationToken cancellationToken = default,
        string? reasonCode = null,
        string? correlationId = null)
    {
        try
        {
            return await _apiClient.ModerateEventHeavyAsync(
                eventId,
                CreateModerationRequest(reasonCode, correlationId),
                cancellationToken: cancellationToken);
        }
        catch (ApiException<BaseCommandResponseOfGuid> ex)
        {
            _logger.LogWarning(ex, "Event heavy moderation rejected for event {EventId}", eventId);
            return ex.Result;
        }
        catch (ApiException<ProblemDetails> ex)
        {
            _logger.LogWarning(ex, "Event heavy moderation returned problem details for event {EventId}", eventId);
            return ProblemToCommandResponse(eventId, ex.Result, "Event could not be heavy moderated.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error heavy moderating event {EventId}", eventId);
            return new BaseCommandResponseOfGuid
            {
                Id = eventId,
                Success = false,
                Message = "Event could not be heavy moderated."
            };
        }
    }

    public async Task<BaseCommandResponseOfGuid?> UnmoderateEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default,
        string? reasonCode = null,
        string? correlationId = null)
    {
        try
        {
            return await _apiClient.UnmoderateEventAsync(
                eventId,
                CreateModerationRequest(reasonCode, correlationId),
                cancellationToken: cancellationToken);
        }
        catch (ApiException<BaseCommandResponseOfGuid> ex)
        {
            _logger.LogWarning(ex, "Event unmoderation rejected for event {EventId}", eventId);
            return ex.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unmoderating event {EventId}", eventId);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Event could not be unmoderated."
            };
        }
    }

    private static EventModerationRequestDto CreateModerationRequest(string? reasonCode, string? correlationId) => new()
    {
        ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? null : reasonCode.Trim(),
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim()
    };

    private static BaseCommandResponseOfGuid ProblemToCommandResponse(Guid eventId, ProblemDetails? problemDetails, string fallbackMessage) =>
        new()
        {
            Id = eventId,
            Success = false,
            Message = problemDetails?.Detail ?? problemDetails?.Title ?? fallbackMessage,
            Errors = string.IsNullOrWhiteSpace(problemDetails?.Detail) ? [] : [problemDetails.Detail]
        };

    public Task<ICollection<EventTypeListDto>> GetEventTypesAsync() => _apiClient.GetEventTypesAsync();

    public Task<ICollection<EventFormatListDto>> GetEventFormatsAsync() => _apiClient.GetEventFormatOptionsAsync();

    public async Task<ICollection<EventSessionListDto>> GetAllSessionsAsync()
    {
        var result = await _apiClient.GetEventSessionsListAsync(1, 100);
        return result?.GetItems() ?? new List<EventSessionListDto>();
    }

    public async Task<ICollection<EventSessionListDto>> GetSessionsByEventAsync(Guid eventId, bool includeManagedSessions = false)
    {
        var publicSessions = await GetPublicSessionsByEventAsync(eventId);

        if (!includeManagedSessions)
            return publicSessions;

        try
        {
            var result = await _apiClient.GetManagedEventSessionsByEventAsync(eventId);
            var managedSessions = result?.GetItems() ?? new List<EventSessionListDto>();
            return MergeEventSessions(publicSessions, managedSessions);
        }
        catch (ApiException ex) when (ex.StatusCode is 401 or 403 or 404)
        {
            _logger.LogDebug(
                "Managed event sessions unavailable for event {EventId}; status {StatusCode}. Using public sessions only.",
                eventId,
                ex.StatusCode);
            return publicSessions;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching managed event sessions for event {EventId}. Using public sessions only.", eventId);
            return publicSessions;
        }
    }

    private async Task<ICollection<EventSessionListDto>> GetPublicSessionsByEventAsync(Guid eventId)
    {
        try
        {
            var result = await _apiClient.GetEventSessionsAsync(eventId);
            return result?.GetItems() ?? new List<EventSessionListDto>();
        }
        catch (ApiException ex) when (ex.StatusCode is 401 or 403 or 404)
        {
            _logger.LogDebug(
                "Public event sessions unavailable for event {EventId}; status {StatusCode}.",
                eventId,
                ex.StatusCode);
            return new List<EventSessionListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching public sessions for event {EventId}", eventId);
            return new List<EventSessionListDto>();
        }
    }

    public Task<BaseCommandResponseOfGuid> CreateSessionAsync(ComposerCreateEventSessionRequest session)
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

    public Task<BaseCommandResponseOfGuid?> PublishEventSessionAsync(
        Guid sessionId,
        Guid expectedConcurrencyStamp,
        CancellationToken cancellationToken = default) =>
        ExecuteSessionLifecycleActionAsync(
            sessionId,
            "publish",
            () => _apiClient.PublishEventSessionAsync(
                sessionId,
                new PublishEventSessionRequestDto { ExpectedConcurrencyStamp = expectedConcurrencyStamp },
                cancellationToken: cancellationToken));

    public Task<BaseCommandResponseOfGuid?> ArchiveEventSessionAsync(
        Guid sessionId,
        Guid expectedConcurrencyStamp,
        CancellationToken cancellationToken = default) =>
        ExecuteSessionLifecycleActionAsync(
            sessionId,
            "archive",
            () => _apiClient.ArchiveEventSessionAsync(
                sessionId,
                new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = expectedConcurrencyStamp },
                cancellationToken: cancellationToken));

    public Task<BaseCommandResponseOfGuid?> CancelEventSessionAsync(
        Guid sessionId,
        Guid expectedConcurrencyStamp,
        CancellationToken cancellationToken = default) =>
        ExecuteSessionLifecycleActionAsync(
            sessionId,
            "cancel",
            () => _apiClient.CancelEventSessionAsync(
                sessionId,
                new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = expectedConcurrencyStamp },
                cancellationToken: cancellationToken));

    public Task<BaseCommandResponseOfGuid?> CompleteEventSessionAsync(
        Guid sessionId,
        Guid expectedConcurrencyStamp,
        CancellationToken cancellationToken = default) =>
        ExecuteSessionLifecycleActionAsync(
            sessionId,
            "complete",
            () => _apiClient.CompleteEventSessionAsync(
                sessionId,
                new EventSessionLifecycleRequestDto { ExpectedConcurrencyStamp = expectedConcurrencyStamp },
                cancellationToken: cancellationToken));

    private async Task<BaseCommandResponseOfGuid?> ExecuteSessionLifecycleActionAsync(
        Guid sessionId,
        string actionName,
        Func<Task<BaseCommandResponseOfGuid>> action)
    {
        try
        {
            return await action();
        }
        catch (ApiException<BaseCommandResponseOfGuid> ex)
        {
            _logger.LogWarning(ex, "Event session {ActionName} rejected for session {SessionId}", actionName, sessionId);
            return ex.Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing event session {ActionName} for session {SessionId}", actionName, sessionId);
            return null;
        }
    }

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

    public async Task<ICollection<EventListDto>> GetRegistrationEventsByUserAsync(Guid userId)
    {
        try
        {
            var registrations = await GetRegistrationsByUserAsync(userId);
            var tasks = registrations
                .GroupBy(GetRegistrationEventGroupKey)
                .Select(BuildRegistrationEventListItemAsync);

            var events = await Task.WhenAll(tasks);

            return events
                .Where(evt => evt is not null)
                .Select(evt => evt!)
                .OrderBy(evt => evt.FirstSessionDate ?? DateTimeOffset.MaxValue)
                .ThenBy(evt => evt.Title)
                .ToList();
        }
        catch (ApiException ex) when (ex.StatusCode is 401 or 403 or 404)
        {
            return new List<EventListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching registration events for user {UserId}", userId);
            return new List<EventListDto>();
        }
    }

    public async Task<ICollection<EventListDto>> GetRegistrationEventsByActorAsync(Guid actorId)
    {
        try
        {
            var actor = (await _apiClient.GetActorByIdAsync(actorId))?.ToDto();
            if (actor?.UserId is not Guid userId)
            {
                return new List<EventListDto>();
            }

            return await GetRegistrationEventsByUserAsync(userId);
        }
        catch (ApiException ex) when (ex.StatusCode is 401 or 403 or 404)
        {
            return new List<EventListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching registration events for actor {ActorId}", actorId);
            return new List<EventListDto>();
        }
    }

    private static Guid GetRegistrationEventGroupKey(EventRegistrationListDto registration)
    {
        return registration.EventId
            ?? registration.EventRegistrationIntentId
            ?? registration.Id
            ?? Guid.NewGuid();
    }

    private async Task<EventListDto?> BuildRegistrationEventListItemAsync(
        IGrouping<Guid, EventRegistrationListDto> registrationGroup)
    {
        var rows = registrationGroup.ToList();
        if (rows.Count == 0)
        {
            return null;
        }

        var first = rows
            .OrderBy(registration => registration.EventStartTime ?? DateTimeOffset.MaxValue)
            .ThenBy(registration => registration.EventSessionTitle)
            .First();

        var eventId = first.EventId ?? Guid.Empty;
        var title = first.EventTitle ?? first.EventSessionTitle ?? "Event";
        var featuredImageUri = first.EventFeaturedImageUri;
        var startTime = first.EventStartTime;

        if (eventId == Guid.Empty && first.EventSessionId.HasValue)
        {
            var session = await GetSessionByIdAsync(first.EventSessionId.Value);
            if (session is not null)
            {
                eventId = session.EventId ?? eventId;
                title = string.IsNullOrWhiteSpace(session.EventTitle) ? title : session.EventTitle;
                startTime ??= session.StartTime;
            }
        }

        EventDto? eventDetails = null;
        if (eventId != Guid.Empty)
        {
            eventDetails = await GetEventByIdAsync(eventId);
        }

        if (eventDetails is not null)
        {
            title = string.IsNullOrWhiteSpace(eventDetails.Title) ? title : eventDetails.Title;
            featuredImageUri = string.IsNullOrWhiteSpace(eventDetails.FeaturedImageUri)
                ? featuredImageUri
                : eventDetails.FeaturedImageUri;
        }

        if (eventId == Guid.Empty)
        {
            return null;
        }

        return MapRegistrationEventListItem(
            eventId,
            title,
            featuredImageUri,
            startTime,
            eventDetails);
    }

    private static EventListDto MapRegistrationEventListItem(
        Guid eventId,
        string title,
        string? featuredImageUri,
        DateTimeOffset? fallbackStartTime,
        EventDto? eventDetails)
    {
        var firstSessionDate = eventDetails?.FirstSessionDate ?? fallbackStartTime;
        var lastSessionDate = eventDetails?.LastSessionDate ?? fallbackStartTime;

        return new EventListDto
        {
            Id = eventId,
            Title = title,
            Subtitle = eventDetails?.Subtitle,
            Description = eventDetails?.Description,
            Slug = eventDetails?.Slug,
            EventTypeId = eventDetails?.EventTypeId ?? 0,
            EventTypeFullName = eventDetails?.EventTypeFullName ?? "Event",
            AudienceGenderId = eventDetails?.AudienceGenderId ?? 0,
            AudienceGenderFullName = eventDetails?.AudienceGenderFullName ?? "All audiences",
            AudienceAgeId = eventDetails?.AudienceAgeId ?? 0,
            AudienceAgeFullName = eventDetails?.AudienceAgeFullName ?? "All ages",
            AudienceAgeMinAge = eventDetails?.AudienceAgeMinAge,
            AudienceAgeMaxAge = eventDetails?.AudienceAgeMaxAge,
            ActorId = eventDetails?.ActorId ?? Guid.Empty,
            ActorDisplayName = eventDetails?.ActorDisplayName ?? string.Empty,
            ActorTypeId = eventDetails?.ActorTypeId ?? 0,
            ActorTypeFullName = eventDetails?.ActorTypeFullName ?? "Actor",
            ActorUserId = eventDetails?.ActorUserId,
            ActorOrganizationId = eventDetails?.ActorOrganizationId,
            ActorGroupId = eventDetails?.ActorGroupId,
            ActorProfilePictureId = eventDetails?.ActorProfilePictureId,
            ActorProfilePictureUri = eventDetails?.ActorProfilePictureUri,
            Price = eventDetails?.Price,
            CurrencyCode = eventDetails?.CurrencyCode,
            FeaturedImageId = eventDetails?.FeaturedImageId,
            FeaturedImageUri = featuredImageUri,
            IsRegistrationRequired = eventDetails?.IsRegistrationRequired,
            ExternalRegistrationUrl = eventDetails?.ExternalRegistrationUrl,
            RegistrationPolicyId = eventDetails?.RegistrationPolicyId,
            RegistrationPolicyFullName = eventDetails?.RegistrationPolicyFullName,
            EventStatusId = eventDetails?.EventStatusId ?? 0,
            EventStatusFullName = eventDetails?.EventStatusFullName ?? string.Empty,
            VisibilityTypeId = eventDetails?.VisibilityTypeId ?? 1,
            VisibilityTypeFullName = eventDetails?.VisibilityTypeFullName ?? "Public",
            EventFormatId = eventDetails?.EventFormatId ?? 0,
            EventFormatFullName = eventDetails?.EventFormatFullName ?? "Event",
            MadhabId = eventDetails?.MadhabId,
            MadhabFullName = eventDetails?.MadhabFullName,
            SessionCount = eventDetails?.SessionCount,
            FirstSessionDate = firstSessionDate,
            LastSessionDate = lastSessionDate,
            Timezone = eventDetails?.Timezone,
            TotalViews = eventDetails?.TotalViews,
            IsUserReported = eventDetails?.IsUserReported,
            EventUrl = eventDetails?.EventUrl,
            TenantId = eventDetails?.TenantId,
            IsPast = IsPastRegistrationEvent(lastSessionDate ?? firstSessionDate),
            AdditionalProperties = eventDetails is null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(eventDetails.AdditionalProperties)
        };
    }

    private static bool IsPastRegistrationEvent(DateTimeOffset? referenceDate)
    {
        return referenceDate.HasValue && referenceDate.Value.Date < DateTimeOffset.UtcNow.Date;
    }

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

    public async Task<EventSessionDto?> GetManagedSessionByIdAsync(Guid eventId, Guid sessionId)
    {
        try
        {
            var result = await _apiClient.GetEventSessionByIdAsync(sessionId);
            var session = result?.ToDto();
            if (session is not null)
                return session;

            return await GetManagedSessionByEventAsync(eventId, sessionId);
        }
        catch (ApiException ex) when (ex.StatusCode is 401 or 403 or 404)
        {
            _logger.LogDebug(
                "Public event session detail unavailable for session {SessionId}; status {StatusCode}. Trying managed event session read.",
                sessionId,
                ex.StatusCode);
            return await GetManagedSessionByEventAsync(eventId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching managed-capable session {SessionId} on event {EventId}", sessionId, eventId);
            return null;
        }
    }

    private async Task<EventSessionDto?> GetManagedSessionByEventAsync(Guid eventId, Guid sessionId)
    {
        try
        {
            var result = await _apiClient.GetManagedEventSessionsByEventAsync(eventId);
            var session = result?
                .GetItems()
                .FirstOrDefault(item => item.Id == sessionId);

            return session is null
                ? null
                : MapManagedSessionListToDetail(session);
        }
        catch (ApiException ex) when (ex.StatusCode is 401 or 403 or 404)
        {
            _logger.LogDebug(
                "Managed event session detail unavailable for session {SessionId} on event {EventId}; status {StatusCode}.",
                sessionId,
                eventId,
                ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching managed session {SessionId} on event {EventId}", sessionId, eventId);
            return null;
        }
    }

    private static EventSessionDto MapManagedSessionListToDetail(EventSessionListDto session)
    {
        var dto = new EventSessionDto
        {
            Id = session.Id,
            ConcurrencyStamp = session.ConcurrencyStamp,
            EventId = session.EventId,
            EventTitle = session.EventTitle ?? string.Empty,
            EventDayId = session.EventDayId,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            IsScheduled = session.IsScheduled,
            LocalStartDate = session.LocalStartDate,
            LocalStartTime = session.LocalStartTime,
            LocalEndTime = session.LocalEndTime,
            SortOrder = session.SortOrder,
            LocationId = session.LocationId,
            LocationFullName = session.LocationFullName,
            LocationCity = session.LocationCity,
            RoomId = session.RoomId,
            RoomName = session.RoomName,
            Title = session.Title,
            EventSessionKindId = session.EventSessionKindId,
            EventSessionKindFullName = session.EventSessionKindFullName,
            EventSessionKindMasterCode = session.EventSessionKindMasterCode,
            EventSessionStatusId = session.EventSessionStatusId,
            EventSessionStatusFullName = session.EventSessionStatusFullName,
            EventSessionStatusMasterCode = session.EventSessionStatusMasterCode,
            Slug = session.Slug,
            FeaturedImageId = session.FeaturedImageId,
            FeaturedImageUri = session.FeaturedImageUri,
            MaxAudienceAttendees = session.MaxAudienceAttendees,
            CurrentAudienceAttendees = session.CurrentAudienceAttendees,
            RegistrationModeId = session.RegistrationModeId,
            RegistrationModeFullName = session.RegistrationModeFullName,
            Price = session.Price,
            CurrencyCode = session.CurrencyCode,
            IslamicAspect = session.IslamicAspect,
            TenantId = session.TenantId
        };

        dto.AdditionalProperties = new Dictionary<string, object>(session.AdditionalProperties);
        return dto;
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

    public async Task<ICollection<EventListDto>> GetProfileEventsByActorAsync(Guid actorId)
    {
        var publicEvents = await GetPublicEventsByActorAsync(actorId);

        try
        {
            var result = await _apiClient.GetManagedEventsByActorAsync(
                actorId: actorId,
                pageNumber: 1,
                pageSize: 100);
            var managedEvents = result?.GetItems() ?? new List<EventListDto>();
            return MergeProfileEvents(publicEvents, managedEvents);
        }
        catch (ApiException ex) when (ex.StatusCode is 401 or 403 or 404)
        {
            _logger.LogDebug(
                "Managed profile events unavailable for actor {ActorId}; status {StatusCode}. Using public events only.",
                actorId,
                ex.StatusCode);
            return publicEvents;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching managed profile events for actor {ActorId}. Using public events only.", actorId);
            return publicEvents;
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

    private static ICollection<EventListDto> MergeProfileEvents(
        ICollection<EventListDto> publicEvents,
        ICollection<EventListDto> managedEvents)
    {
        if (managedEvents.Count == 0)
            return publicEvents;

        var remainingManagedEvents = managedEvents.ToList();
        var merged = new List<EventListDto>(publicEvents.Count + remainingManagedEvents.Count);
        foreach (var publicEvent in publicEvents)
        {
            if (publicEvent.Id is not { } publicEventId || publicEventId == Guid.Empty)
            {
                merged.Add(publicEvent);
                continue;
            }

            var managedIndex = remainingManagedEvents.FindIndex(managedEvent => managedEvent.Id == publicEventId);
            if (managedIndex >= 0)
            {
                var managedEvent = remainingManagedEvents[managedIndex];
                remainingManagedEvents.RemoveAt(managedIndex);
                merged.Add(managedEvent);
                continue;
            }

            merged.Add(publicEvent);
        }

        merged.AddRange(remainingManagedEvents);
        return merged;
    }

    private static ICollection<EventSessionListDto> MergeEventSessions(
        ICollection<EventSessionListDto> publicSessions,
        ICollection<EventSessionListDto> managedSessions)
    {
        if (managedSessions.Count == 0)
            return publicSessions;

        var remainingManagedSessions = managedSessions.ToList();
        var merged = new List<EventSessionListDto>(publicSessions.Count + remainingManagedSessions.Count);
        foreach (var publicSession in publicSessions)
        {
            if (publicSession.Id is not { } publicSessionId || publicSessionId == Guid.Empty)
            {
                merged.Add(publicSession);
                continue;
            }

            var managedIndex = remainingManagedSessions.FindIndex(session => session.Id == publicSessionId);
            if (managedIndex >= 0)
            {
                var managedSession = remainingManagedSessions[managedIndex];
                remainingManagedSessions.RemoveAt(managedIndex);
                merged.Add(managedSession);
                continue;
            }

            merged.Add(publicSession);
        }

        merged.AddRange(remainingManagedSessions);
        return merged;
    }
}
