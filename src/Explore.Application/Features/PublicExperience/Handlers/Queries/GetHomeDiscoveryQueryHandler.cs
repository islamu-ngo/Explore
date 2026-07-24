// ABOUTME: Composes bounded tenant-local public home discovery sections through existing public event queries.
// ABOUTME: Resolves coarse areas, keeps semantic sections independent, omits unsupported curation, and isolates failures.

using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.PublicExperience;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Features.Federation.Atproto.Requests.Queries;
using Explore.Application.Features.PublicExperience.Requests.Queries;
using Explore.Application.Models.PublicExperience;
using Explore.Application.Responses;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.PublicExperience.Handlers.Queries;

public sealed partial class GetHomeDiscoveryQueryHandler(
    IRequestHandler<GetPublicEventDiscoveryRequest, PaginatedResult<EventDiscoveryItemDto>> eventDiscoveryHandler,
    IRequestHandler<GetPublicExperienceShellQuery, PublicExperienceShellDto> shellHandler,
    ITenantContext tenantContext,
    IHierarchicalSettingsResolver settingsResolver,
    ILocationRepository locationRepository,
    TimeProvider timeProvider,
    ILogger<GetHomeDiscoveryQueryHandler> logger)
    : IRequestHandler<GetHomeDiscoveryQuery, HomeDiscoveryDto>
{
    private const int HeroLimit = 10;
    private const int UpcomingLimit = 18;
    private const int StandardLimit = 10;
    private const int SpotlightLimit = 3;
    private const int MaximumCuratedSections = 2;
    private static readonly TimeSpan SectionTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan CompositeTimeout = TimeSpan.FromSeconds(3);
    private static readonly int[] OnlineFormatIds =
        [(int)EventFormatEnum.Digital, (int)EventFormatEnum.Hybrid];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<HomeDiscoveryDto> Handle(
        GetHomeDiscoveryQuery request,
        CancellationToken cancellationToken)
    {
        var generatedAt = timeProvider.GetUtcNow();
        var result = new HomeDiscoveryDto
        {
            GeneratedAtUtc = generatedAt
        };
        using var compositeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        compositeCancellation.CancelAfter(CompositeTimeout);
        var operationToken = compositeCancellation.Token;

        try
        {
            var today = DateOnly.FromDateTime(generatedAt.UtcDateTime);
            var areaState = await ResolveAreaStateAsync(request, operationToken);
            result.Context = MapContext(areaState);
            var heroRequest = CreateUpcomingRequest(today, "views", sortDescending: true, HeroLimit);
            if (ApplyContext(heroRequest, areaState))
            {
                result.Hero = await QuerySectionAsync(
                    "hero", heroRequest, HeroLimit, result.SectionStatuses, operationToken);
            }
            else
            {
                result.SectionStatuses["hero"] = HomeDiscoverySectionStatus.Empty;
            }

            var upcomingRequest = CreateUpcomingRequest(today, "date", sortDescending: false, UpcomingLimit);
            if (ApplyContext(upcomingRequest, areaState))
            {
                result.UpcomingInArea = await QuerySectionAsync(
                    "upcoming", upcomingRequest, UpcomingLimit, result.SectionStatuses, operationToken);
            }
            else
            {
                result.SectionStatuses["upcoming"] = HomeDiscoverySectionStatus.Empty;
            }

            result.Spotlight = await BuildSpotlightAsync(
                areaState, today, result.SectionStatuses, operationToken);

            if (areaState.Mode == HomeDiscoveryMode.Area &&
                areaState.SelectedArea?.LocationIds is { Count: > 0 } areaLocationIds)
            {
                var mostViewedAreaRequest = CreateUpcomingRequest(today, "views", sortDescending: true, StandardLimit);
                mostViewedAreaRequest.LocationIds = areaLocationIds.Distinct().ToList();
                mostViewedAreaRequest.FormatIds = [(int)EventFormatEnum.Local, (int)EventFormatEnum.Hybrid];
                result.MostViewedInArea = await QuerySectionAsync(
                    "most-viewed-area",
                    mostViewedAreaRequest,
                    StandardLimit,
                    result.SectionStatuses,
                    operationToken);
            }
            else
            {
                result.SectionStatuses["most-viewed-area"] = HomeDiscoverySectionStatus.Omitted;
            }

            var mostViewedOnlineRequest = CreateUpcomingRequest(today, "views", sortDescending: true, StandardLimit);
            mostViewedOnlineRequest.FormatIds = [.. OnlineFormatIds];
            result.MostViewedOnline = await QuerySectionAsync(
                "most-viewed-online",
                mostViewedOnlineRequest,
                StandardLimit,
                result.SectionStatuses,
                operationToken);

            result.CuratedSections = await BuildCuratedSectionsAsync(
                areaState, today, result.SectionStatuses, operationToken);

            var recentlyAddedRequest = CreateUpcomingRequest(today, "createdat", sortDescending: true, StandardLimit);
            if (ApplyContext(recentlyAddedRequest, areaState))
            {
                result.RecentlyAdded = await QuerySectionAsync(
                    "recently-added",
                    recentlyAddedRequest,
                    StandardLimit,
                    result.SectionStatuses,
                    operationToken);
            }
            else
            {
                result.SectionStatuses["recently-added"] = HomeDiscoverySectionStatus.Empty;
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            result.SectionStatuses["composite"] = HomeDiscoverySectionStatus.Failed;
            LogCompositeTimeout(logger);
            return result;
        }
    }

    private async Task<AreaState> ResolveAreaStateAsync(
        GetHomeDiscoveryQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var settingContext = new SettingContext(TenantId: tenantContext.TenantId);
            var rawConfig = await settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.DiscoveryAreas,
                settingContext,
                cancellationToken);
            var config = Deserialize<PublicDiscoveryAreasConfig>(rawConfig) ?? new PublicDiscoveryAreasConfig();
            var configuredAreas = config.Areas ?? [];
            var referencedLocationIds = configuredAreas
                .SelectMany(area => area.LocationIds ?? [])
                .ToHashSet();
            var tenantLocationIds = referencedLocationIds.Count == 0
                ? new HashSet<Guid>()
                : (await locationRepository.GetExistingTenantLocationIdsAsync(
                        tenantContext.TenantId,
                        referencedLocationIds,
                        cancellationToken))
                    .ToHashSet();
            var validationErrors = PublicDiscoveryAreasConfigValidator.Validate(config, tenantLocationIds);
            if (validationErrors.Count > 0)
                return AreaState.Empty(ParseMode(request.Mode));

            var activeAreas = configuredAreas
                .Where(area => area.IsActive)
                .OrderBy(area => area.SortOrder)
                .ThenBy(area => area.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(area => area.Id)
                .ToList();
            var selectedArea = activeAreas.FirstOrDefault(area => area.Id == request.AreaId) ??
                               activeAreas.FirstOrDefault(area => area.IsDefault) ??
                               activeAreas.FirstOrDefault();
            var mode = ParseMode(request.Mode);
            if (mode == HomeDiscoveryMode.Area && selectedArea is null)
                mode = HomeDiscoveryMode.All;

            return new AreaState(mode, activeAreas, selectedArea);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogSectionFailure(logger, exception, "context");
            return AreaState.Empty(ParseMode(request.Mode));
        }
    }

    private async Task<HomeDiscoverySectionDto?> BuildSpotlightAsync(
        AreaState areaState,
        DateOnly today,
        Dictionary<string, HomeDiscoverySectionStatus> statuses,
        CancellationToken cancellationToken)
    {
        var presets = await ResolveCuratedPresetsAsync(cancellationToken);
        var spotlightPreset = presets.FirstOrDefault(IsSpotlightPreset);
        GetEventListRequest? spotlightRequest = null;
        var label = "Community spotlight";

        if (spotlightPreset is not null && TryBuildCuratedRequest(spotlightPreset, today, out var curatedRequest))
        {
            spotlightRequest = curatedRequest;
            label = spotlightPreset.Label.Trim();
        }
        else
        {
            try
            {
                var shell = await shellHandler.Handle(new GetPublicExperienceShellQuery(), cancellationToken);
                if (shell.PrimaryOrganization.ActorId is { } actorId)
                {
                    spotlightRequest = CreateUpcomingRequest(today, "date", sortDescending: false, SpotlightLimit);
                    spotlightRequest.ActorId = actorId;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogSectionFailure(logger, exception, "spotlight");
                statuses["spotlight"] = HomeDiscoverySectionStatus.Failed;
                return null;
            }
        }

        if (spotlightRequest is null)
        {
            statuses["spotlight"] = HomeDiscoverySectionStatus.Omitted;
            return null;
        }

        if (!ApplyContext(spotlightRequest, areaState))
        {
            statuses["spotlight"] = HomeDiscoverySectionStatus.Empty;
            return new HomeDiscoverySectionDto { Key = "spotlight", Label = label };
        }

        var items = await QuerySectionAsync(
            "spotlight", spotlightRequest, SpotlightLimit, statuses, cancellationToken);
        return new HomeDiscoverySectionDto { Key = "spotlight", Label = label, Items = items };
    }

    private async Task<List<HomeDiscoverySectionDto>> BuildCuratedSectionsAsync(
        AreaState areaState,
        DateOnly today,
        Dictionary<string, HomeDiscoverySectionStatus> statuses,
        CancellationToken cancellationToken)
    {
        var sections = new List<HomeDiscoverySectionDto>();
        var presets = await ResolveCuratedPresetsAsync(cancellationToken);

        foreach (var preset in presets
                     .Where(preset => !IsSpotlightPreset(preset))
                     .Take(MaximumCuratedSections))
        {
            if (!TryBuildCuratedRequest(preset, today, out var request))
                continue;

            var limit = Math.Clamp(preset.Limit ?? StandardLimit, 1, StandardLimit);
            var key = $"curated:{preset.Id.Trim()}";
            var items = ApplyContext(request, areaState)
                ? await QuerySectionAsync(key, request, limit, statuses, cancellationToken)
                : [];
            if (!statuses.ContainsKey(key))
                statuses[key] = HomeDiscoverySectionStatus.Empty;
            sections.Add(new HomeDiscoverySectionDto
            {
                Key = preset.Id.Trim(),
                Label = preset.Label.Trim(),
                Items = items
            });
        }

        return sections;
    }

    private async Task<IReadOnlyList<PublicEventSectionPresetConfig>> ResolveCuratedPresetsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var rawConfig = await settingsResolver.ResolveAsync<string>(
                GovernanceSettingKeys.PublicExperience.EventSectionPresets,
                new SettingContext(TenantId: tenantContext.TenantId),
                cancellationToken);
            var config = Deserialize<PublicEventSectionPresetsConfig>(rawConfig);
            return config?.Presets?
                .Where(preset => preset.IsEnabled &&
                                 !string.IsNullOrWhiteSpace(preset.Id) &&
                                 !string.IsNullOrWhiteSpace(preset.Label))
                .OrderBy(preset => preset.SortOrder)
                .ThenBy(preset => preset.Id, StringComparer.OrdinalIgnoreCase)
                .GroupBy(preset => preset.Id.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList() ?? [];
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogSectionFailure(logger, exception, "curated-config");
            return [];
        }
    }

    private async Task<List<EventDiscoveryItemDto>> QuerySectionAsync(
        string key,
        GetEventListRequest request,
        int limit,
        Dictionary<string, HomeDiscoverySectionStatus> statuses,
        CancellationToken cancellationToken)
    {
        request.PageNumber = 1;
        request.PageSize = limit;

        try
        {
            using var sectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            sectionCancellation.CancelAfter(SectionTimeout);
            var page = await eventDiscoveryHandler.Handle(
                new GetPublicEventDiscoveryRequest(request),
                sectionCancellation.Token);
            var items = page.Items
                .Where(item =>
                    (item.Event is { Id: var localId } && localId != Guid.Empty)
                    || (item.FederatedEvent is { Id: var federatedId } && federatedId != Guid.Empty))
                .Take(limit)
                .Select(MapDiscoveryItem)
                .ToList();
            statuses[key] = items.Count > 0
                ? HomeDiscoverySectionStatus.Available
                : HomeDiscoverySectionStatus.Empty;
            return items;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            LogSectionTimeout(logger, key);
            statuses[key] = HomeDiscoverySectionStatus.Failed;
            return [];
        }
        catch (Exception exception)
        {
            LogSectionFailure(logger, exception, key);
            statuses[key] = HomeDiscoverySectionStatus.Failed;
            return [];
        }
    }

    private static HomeDiscoveryContextDto MapContext(AreaState state)
    {
        var defaultArea = state.ActiveAreas.FirstOrDefault(area => area.IsDefault);
        return new HomeDiscoveryContextDto
        {
            Mode = state.Mode,
            SelectedAreaId = state.SelectedArea?.Id,
            DefaultAreaId = defaultArea?.Id,
            SelectedAreaDisplayName = state.SelectedArea?.DisplayName.Trim() ?? string.Empty,
            AvailableAreas = state.ActiveAreas.Select(MapArea).ToList()
        };
    }

    private static EventListDto MapEvent(EventListDto source) => new()
    {
        Id = source.Id,
        Title = Bound(source.Title, 240),
        Subtitle = BoundNullable(source.Subtitle, 160),
        Description = BoundNullable(source.Description, 150),
        Slug = BoundNullable(source.Slug, 180),
        PublicCode = BoundNullable(source.PublicCode, 12),
        EventTypeId = source.EventTypeId,
        EventTypeFullName = Bound(source.EventTypeFullName, 80),
        AudienceGenderId = source.AudienceGenderId,
        AudienceGenderFullName = Bound(source.AudienceGenderFullName, 80),
        AudienceAgeId = source.AudienceAgeId,
        AudienceAgeFullName = Bound(source.AudienceAgeFullName, 80),
        ActorId = source.ActorId,
        ActorDisplayName = Bound(source.ActorDisplayName, 160),
        ActorTypeId = source.ActorTypeId,
        ActorTypeFullName = Bound(source.ActorTypeFullName, 80),
        Price = source.Price,
        CurrencyCode = BoundNullable(source.CurrencyCode, 3),
        FeaturedImageId = source.FeaturedImageId,
        FeaturedImageUri = BoundNullable(source.FeaturedImageUri, 500),
        EventStatusId = source.EventStatusId,
        EventStatusFullName = Bound(source.EventStatusFullName, 80),
        VisibilityTypeId = source.VisibilityTypeId,
        VisibilityTypeFullName = Bound(source.VisibilityTypeFullName, 80),
        EventFormatId = source.EventFormatId,
        EventFormatFullName = Bound(source.EventFormatFullName, 80),
        FirstSessionDate = source.FirstSessionDate,
        FirstSessionStartUtc = source.FirstSessionStartUtc,
        IsPast = source.IsPast,
        IsUserReported = source.IsUserReported,
        CreatedAtUtc = source.CreatedAtUtc,
        AtprotoRecordId = source.AtprotoRecordId,
        TenantId = source.TenantId
    };

    private static EventDiscoveryItemDto MapDiscoveryItem(EventDiscoveryItemDto source) => new()
    {
        Source = source.Source,
        Event = source.Event is null ? null : MapEvent(source.Event),
        FederatedEvent = source.FederatedEvent,
        Federation = source.Federation
    };

    private static string Bound(string value, int maximumLength) =>
        BoundNullable(value, maximumLength) ?? string.Empty;

    private static string? BoundNullable(string? value, int maximumLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maximumLength)
            return value;

        var length = maximumLength;
        if (char.IsHighSurrogate(value[length - 1]) && char.IsLowSurrogate(value[length]))
            length--;

        return value[..length].TrimEnd();
    }

    private static PublicDiscoveryAreaDto MapArea(PublicDiscoveryAreaConfig area) => new()
    {
        Id = area.Id,
        DisplayName = area.DisplayName.Trim(),
        City = area.City.Trim(),
        CountryCode = area.CountryCode.Trim().ToUpperInvariant(),
        CentroidLatitude = area.CentroidLatitude,
        CentroidLongitude = area.CentroidLongitude,
        IsDefault = area.IsDefault,
        SortOrder = area.SortOrder
    };

    private static bool ApplyContext(GetEventListRequest request, AreaState state)
    {
        if (state.Mode == HomeDiscoveryMode.Online)
        {
            request.FormatIds = request.FormatIds is { Count: > 0 }
                ? request.FormatIds.Intersect(OnlineFormatIds).ToList()
                : [.. OnlineFormatIds];
            request.LocationIds = null;
            return request.FormatIds.Count > 0;
        }

        if (state.Mode == HomeDiscoveryMode.Area)
        {
            if (state.SelectedArea?.LocationIds is not { Count: > 0 } locationIds)
                return false;

            request.LocationIds = locationIds.Distinct().ToList();
        }

        return true;
    }

    private static GetEventListRequest CreateUpcomingRequest(
        DateOnly today,
        string sortBy,
        bool sortDescending,
        int limit) =>
        new()
        {
            PageNumber = 1,
            PageSize = limit,
            DateFrom = today,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

    private static bool TryBuildCuratedRequest(
        PublicEventSectionPresetConfig preset,
        DateOnly today,
        out GetEventListRequest request)
    {
        request = CreateUpcomingRequest(today, "date", sortDescending: false, preset.Limit ?? StandardLimit);
        var ownerIds = new[]
        {
            preset.Owners?.ActorIds?.Count ?? 0,
            preset.Owners?.OrganizationIds?.Count ?? 0,
            preset.Owners?.GroupIds?.Count ?? 0
        }.Sum();
        if (ownerIds > 1 || preset.Filters?.CustomProperties is { Count: > 0 })
            return false;

        request.ActorId = preset.Owners?.ActorIds?.SingleOrDefault();
        request.OrganizationId = preset.Owners?.OrganizationIds?.SingleOrDefault();
        request.GroupId = preset.Owners?.GroupIds?.SingleOrDefault();
        request.IncludedCategoryIds = preset.Filters?.CategoryIds?.Distinct().ToList();
        request.IncludedTagIds = preset.Filters?.TagIds?.Distinct().ToList();
        request.AudienceGenderIds = preset.Filters?.AudienceGenderIds?.Distinct().ToList();
        request.AudienceAgeIds = preset.Filters?.AudienceAgeIds?.Distinct().ToList();
        request.EventTypeIds = preset.Filters?.EventTypeIds?.Distinct().ToList();
        request.FormatIds = preset.Filters?.EventFormatIds?.Distinct().ToList();

        if (preset.Filters?.Date is { } dateFilter)
        {
            if (dateFilter.Window == PublicEventSectionDateWindow.Past)
                return false;

            if (dateFilter.Window == PublicEventSectionDateWindow.Custom)
            {
                if (!dateFilter.StartsOnOrAfter.HasValue && !dateFilter.StartsOnOrBefore.HasValue)
                    return false;

                request.DateFrom = dateFilter.StartsOnOrAfter;
                request.DateTo = dateFilter.StartsOnOrBefore;
            }
        }

        return true;
    }

    private static bool IsSpotlightPreset(PublicEventSectionPresetConfig preset) =>
        string.Equals(preset.Id.Trim(), "spotlight", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(preset.Id.Trim(), "community-spotlight", StringComparison.OrdinalIgnoreCase);

    private static HomeDiscoveryMode ParseMode(string? mode) =>
        mode?.Trim().ToLowerInvariant() switch
        {
            "area" => HomeDiscoveryMode.Area,
            "online" => HomeDiscoveryMode.Online,
            _ => HomeDiscoveryMode.All
        };

    private static T? Deserialize<T>(string? rawConfig)
    {
        if (string.IsNullOrWhiteSpace(rawConfig))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(rawConfig, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    [LoggerMessage(LogLevel.Warning, "Home discovery section {SectionKey} failed.")]
    private static partial void LogSectionFailure(
        ILogger logger,
        Exception exception,
        string sectionKey);

    [LoggerMessage(LogLevel.Warning, "Home discovery section {SectionKey} exceeded its time limit.")]
    private static partial void LogSectionTimeout(ILogger logger, string sectionKey);

    [LoggerMessage(LogLevel.Warning, "Home discovery composition exceeded its time limit.")]
    private static partial void LogCompositeTimeout(ILogger logger);

    private sealed record AreaState(
        HomeDiscoveryMode Mode,
        IReadOnlyList<PublicDiscoveryAreaConfig> ActiveAreas,
        PublicDiscoveryAreaConfig? SelectedArea)
    {
        public static AreaState Empty(HomeDiscoveryMode mode) =>
            new(mode == HomeDiscoveryMode.Area ? HomeDiscoveryMode.All : mode, [], null);
    }
}
