// ABOUTME: Query handler returning a paginated, filtered list of events.
// ABOUTME: Applies EventFilter specification and maps to EventListDto.
using AutoMapper;
using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Responses;
using Explore.Application.Specifications.Events;
using Explore.Domain;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Explore.Application.Features.Events.Handlers.Queries;

public class GetEventListRequestHandler : IRequestHandler<GetEventListRequest, PaginatedResult<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetEventListRequestHandler> _logger;
    private readonly HybridCache _cache;
    private readonly IModuleService _moduleService;
    private readonly ITenantContext _tenantContext;
    private readonly ICustomPropertyQuotaResolver _quotaResolver;

    public GetEventListRequestHandler(
        IEventRepository eventRepository,
        IActorRepository actorRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetEventListRequestHandler> logger,
        HybridCache cache,
        IModuleService moduleService,
        ITenantContext tenantContext,
        ICustomPropertyQuotaResolver quotaResolver)
    {
        _eventRepository = eventRepository;
        _actorRepository = actorRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
        _cache = cache;
        _moduleService = moduleService;
        _tenantContext = tenantContext;
        _quotaResolver = quotaResolver;
    }

    public async Task<PaginatedResult<EventListDto>> Handle(GetEventListRequest request, CancellationToken cancellationToken)
    {
        var ownershipActorId = await ResolveOwnershipActorIdAsync(request);
        if (ownershipActorId == MissingOwnershipActorId)
        {
            return PaginatedResult<EventListDto>.Create([], 0, request.PageNumber, request.PageSize);
        }

        var specification = await BuildSpecificationAsync(request, ownershipActorId, cancellationToken);
        var tenantCacheKey = _tenantContext.TenantId.ToString("N");
        var ownershipCacheKey = ownershipActorId?.ToString("N") ?? "none";
        var cacheKeySuffix = specification.ToCacheKeySuffix();
        var cacheKey = $"events:list:tenant:{tenantCacheKey}:{request.PageNumber}:{request.PageSize}:owner:{ownershipCacheKey}:{cacheKeySuffix}";

        var cachedResult = await _cache.GetOrCreateAsync(
            cacheKey,
            async _ =>
            {
                var (events, totalCount) = await _eventRepository.GetEventsWithDetailsPaged(
                    request.PageNumber, request.PageSize, specification);
                var eventDtos = _mapper.Map<List<EventListDto>>(events);
                return PaginatedResult<EventListDto>.Create(eventDtos, totalCount, request.PageNumber, request.PageSize);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            tags: [CacheTags.EventListByTenant(_tenantContext.TenantId)],
            cancellationToken: cancellationToken);

        // Resolve presigned URLs for images
        foreach (var dto in cachedResult.Items)
        {
            dto.FeaturedImageUri = await ResolveImageUrl(dto.FeaturedImageUri);
            dto.ActorProfilePictureUri = await ResolveImageUrl(dto.ActorProfilePictureUri);
        }

        return cachedResult;
    }

    /// <summary>
    /// Builds an <see cref="EventQuerySpecification"/> from the request's filter and sort parameters.
    /// Aspect-specific filters are only applied when the corresponding module is enabled for the current tenant.
    /// </summary>
    private async Task<EventQuerySpecification> BuildSpecificationAsync(
        GetEventListRequest request, Guid? ownershipActorId, CancellationToken cancellationToken)
    {
        var spec = new EventQuerySpecification();

        // ===== Public discoverability filter =====
        // When no explicit status filter is provided, apply public visibility rules:
        // only show Public visibility + non-Draft/Archived events in discovery listings.
        if (request.EventStatusIds is not { Count: > 0 })
        {
            spec = spec.And(EventFilter.PubliclyDiscoverable());
        }

        // ===== Core Event filters (always available) =====

        if (ownershipActorId.HasValue)
            spec = spec.And(EventFilter.Actor(ownershipActorId.Value));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            spec = spec.And(EventFilter.SearchTerm(request.SearchTerm.Trim()));

        if (request.FormatIds is { Count: > 0 })
            spec = spec.And(EventFilter.Formats(request.FormatIds));

        if (request.MadhabIds is { Count: > 0 })
            spec = spec.And(EventFilter.Madhabs(request.MadhabIds));

        if (request.EventTypeIds is { Count: > 0 })
            spec = spec.And(EventFilter.EventTypes(request.EventTypeIds));

        if (request.AudienceGenderIds is { Count: > 0 })
            spec = spec.And(EventFilter.AudienceGenders(request.AudienceGenderIds));

        if (request.AudienceAgeIds is { Count: > 0 })
            spec = spec.And(EventFilter.AudienceAges(request.AudienceAgeIds));

        if (request.EventStatusIds is { Count: > 0 })
            spec = spec.And(EventFilter.Statuses(request.EventStatusIds));

        if (request.DateFrom.HasValue)
            spec = spec.And(EventFilter.DateFrom(request.DateFrom.Value));

        if (request.DateTo.HasValue)
            spec = spec.And(EventFilter.DateTo(request.DateTo.Value));

        // ===== Subquery filters (junction tables — always available) =====

        if (request.CategoryId.HasValue)
            spec = spec.And(EventSubqueryFilter.Category(request.CategoryId.Value));

        if (request.IncludedCategoryIds is { Count: > 0 })
        {
            spec = request.CategoryInclusionMode == TagFilterMode.And
                ? spec.And(EventSubqueryFilter.CategoriesIncludedAll(request.IncludedCategoryIds))
                : spec.And(EventSubqueryFilter.CategoriesIncludedAny(request.IncludedCategoryIds));
        }

        if (request.ExcludedCategoryIds is { Count: > 0 })
        {
            spec = request.CategoryExclusionMode == TagFilterMode.Or
                ? spec.And(EventSubqueryFilter.CategoriesExcludedAny(request.ExcludedCategoryIds))
                : spec.And(EventSubqueryFilter.CategoriesExcludedAll(request.ExcludedCategoryIds));
        }

        if (request.IncludedTagIds is { Count: > 0 })
        {
            spec = request.InclusionMode == TagFilterMode.And
                ? spec.And(EventSubqueryFilter.TagsIncludedAll(request.IncludedTagIds))
                : spec.And(EventSubqueryFilter.TagsIncludedAny(request.IncludedTagIds));
        }

        if (request.ExcludedTagIds is { Count: > 0 })
        {
            spec = request.ExclusionMode == TagFilterMode.Or
                ? spec.And(EventSubqueryFilter.TagsExcludedAny(request.ExcludedTagIds))
                : spec.And(EventSubqueryFilter.TagsExcludedAll(request.ExcludedTagIds));
        }

        if (request.LocationIds is { Count: > 0 })
            spec = spec.And(EventSubqueryFilter.Locations(request.LocationIds));

        if (request.LanguageIds is { Count: > 0 })
            spec = spec.And(EventSubqueryFilter.Languages(request.LanguageIds));

        if (request.RegistrationModeIds is { Count: > 0 })
            spec = spec.And(EventSubqueryFilter.RegistrationModes(request.RegistrationModeIds));

        // ===== Islamic aspect filters (module-conditional) =====

        var tenantId = _tenantContext.TenantId;
        var hasIslamicAspectFilters = request.GenderModeIds is { Count: > 0 }
            || request.IncludesQuranRecitation is true
            || request.ReferencePrayerIds is { Count: > 0 }
            || request.IslamicPrimaryLanguageIds is { Count: > 0 }
            || request.HasIslamicAspect is true;

        if (hasIslamicAspectFilters &&
            await _moduleService.IsModuleEnabledAsync(tenantId, "Mod_Islamic", cancellationToken))
        {
            if (request.HasIslamicAspect is true)
                spec = spec.And(AspectPresenceFilter.HasIslamicAspect());

            if (request.GenderModeIds is { Count: > 0 })
                spec = spec.And(IslamicAspectFilter.GenderModes(request.GenderModeIds));

            if (request.IncludesQuranRecitation is true)
                spec = spec.And(IslamicAspectFilter.IncludesQuranRecitation());

            if (request.ReferencePrayerIds is { Count: > 0 })
                spec = spec.And(IslamicAspectFilter.ReferencePrayers(request.ReferencePrayerIds));

            if (request.IslamicPrimaryLanguageIds is { Count: > 0 })
                spec = spec.And(IslamicAspectFilter.PrimaryLanguages(request.IslamicPrimaryLanguageIds));
        }

        // ===== Tech aspect filters (module-conditional) =====

        var hasTechAspectFilters = request.SkillLevelId.HasValue
            || request.IsCodingCompetition is true
            || request.IsHackathon is true
            || request.RequiresLaptop is true
            || !string.IsNullOrWhiteSpace(request.TechStackTag)
            || request.HasTechAspect is true;

        if (hasTechAspectFilters &&
            await _moduleService.IsModuleEnabledAsync(tenantId, "Mod_Tech", cancellationToken))
        {
            if (request.HasTechAspect is true)
                spec = spec.And(AspectPresenceFilter.HasTechAspect());

            if (request.SkillLevelId.HasValue)
                spec = spec.And(TechAspectFilter.SkillLevel((SkillLevel)request.SkillLevelId.Value));

            if (request.IsCodingCompetition is true)
                spec = spec.And(TechAspectFilter.IsCodingCompetition());

            if (request.IsHackathon is true)
                spec = spec.And(TechAspectFilter.IsHackathon());

            if (request.RequiresLaptop is true)
                spec = spec.And(TechAspectFilter.RequiresLaptop());

            if (!string.IsNullOrWhiteSpace(request.TechStackTag))
                spec = spec.And(TechAspectFilter.TechStack(request.TechStackTag.Trim()));
        }

        // ===== Custom property projection filters (Layer 3 — tenant-gated) =====

        var hasProjectionFilters = request.CustomPropertyFilters is { Count: > 0 }
            || !string.IsNullOrWhiteSpace(request.CustomPropertySearchTerm);

        if (hasProjectionFilters &&
            await _quotaResolver.GetBoolAsync("custom_properties.projection_discovery_enabled", tenantId, cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(request.CustomPropertySearchTerm))
            {
                spec = spec.And(EventCustomPropertyProjectionFilter.GlobalTextSearch(
                    request.CustomPropertySearchTerm.Trim()));
            }

            if (request.CustomPropertyFilters is { Count: > 0 })
            {
                foreach (var criterion in request.CustomPropertyFilters)
                {
                    var filter = MapCriterionToFilter(criterion);
                    if (filter is not null)
                        spec = spec.And(filter);
                }
            }
        }

        // ===== Sorting =====

        var sort = ResolveSortField(request.SortBy);
        if (sort is not null)
        {
            spec = request.SortDescending ? spec.SortByDescending(sort) : spec.SortBy(sort);
        }
        else
        {
            // Default sort: by date descending
            spec = spec.SortByDescending(EventSort.Date);
        }

        return spec;
    }

    private static readonly Guid MissingOwnershipActorId = Guid.Empty;

    private async Task<Guid?> ResolveOwnershipActorIdAsync(GetEventListRequest request)
    {
        var tenantId = _tenantContext.TenantId;

        if (request.ActorId.HasValue)
        {
            var actor = await _actorRepository.GetById(request.ActorId.Value);
            return actor?.TenantId == tenantId ? actor.Id : MissingOwnershipActorId;
        }

        if (request.OrganizationId.HasValue)
        {
            var actor = await _actorRepository.GetActorByOrganizationId(request.OrganizationId.Value);
            return actor?.TenantId == tenantId ? actor.Id : MissingOwnershipActorId;
        }

        if (request.GroupId.HasValue)
        {
            var actor = await _actorRepository.GetActorByGroupId(request.GroupId.Value);
            return actor?.TenantId == tenantId ? actor.Id : MissingOwnershipActorId;
        }

        return null;
    }

    /// <summary>
    /// Resolves a sort field name string to an <see cref="EventSort"/> instance.
    /// </summary>
    private static EventSort? ResolveSortField(string? sortBy) =>
        sortBy?.ToLowerInvariant() switch
        {
            "date" => EventSort.Date,
            "title" => EventSort.Title,
            "views" => EventSort.Views,
            "createdat" => EventSort.CreatedAt,
            _ => null
        };

    private static EventCustomPropertyProjectionFilter? MapCriterionToFilter(CustomPropertyFilterCriterion criterion) =>
        criterion.Operator switch
        {
            CustomPropertyFilterOperator.Equals when criterion.Value is not null =>
                EventCustomPropertyProjectionFilter.ExactMatch(criterion.Namespace, criterion.Key, criterion.Value),

            CustomPropertyFilterOperator.Contains when criterion.Value is not null =>
                EventCustomPropertyProjectionFilter.TextSearch(criterion.Namespace, criterion.Key, criterion.Value),

            CustomPropertyFilterOperator.Exists =>
                EventCustomPropertyProjectionFilter.Exists(criterion.Namespace, criterion.Key),

            CustomPropertyFilterOperator.BooleanTrue =>
                EventCustomPropertyProjectionFilter.BooleanTrue(criterion.Namespace, criterion.Key),

            CustomPropertyFilterOperator.OptionEquals when criterion.OptionId.HasValue =>
                EventCustomPropertyProjectionFilter.OptionMatch(criterion.Namespace, criterion.Key, criterion.OptionId.Value),

            CustomPropertyFilterOperator.OptionIn when criterion.OptionIds is { Count: > 0 } =>
                EventCustomPropertyProjectionFilter.OptionsMatchAny(criterion.Namespace, criterion.Key, criterion.OptionIds),

            CustomPropertyFilterOperator.NumberRange when criterion.MinNumber.HasValue || criterion.MaxNumber.HasValue =>
                EventCustomPropertyProjectionFilter.NumberRange(criterion.Namespace, criterion.Key, criterion.MinNumber, criterion.MaxNumber),

            CustomPropertyFilterOperator.DateRange when criterion.DateFrom.HasValue || criterion.DateTo.HasValue =>
                EventCustomPropertyProjectionFilter.DateRange(criterion.Namespace, criterion.Key, criterion.DateFrom, criterion.DateTo),

            _ => null
        };

    /// <summary>
    /// Resolves an image object key to a presigned URL for viewing.
    /// If the value is already a full URL (legacy data), extracts the key and generates presigned URL.
    /// </summary>
    private async Task<string?> ResolveImageUrl(string? objectKeyOrUri)
    {
        if (string.IsNullOrEmpty(objectKeyOrUri))
            return null;

        try
        {
            // Check if it's already a full URL (legacy data from before this change)
            if (objectKeyOrUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                objectKeyOrUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Extract object key from full URL and generate presigned URL
                if (Uri.TryCreate(objectKeyOrUri, UriKind.Absolute, out var uri))
                {
                    var objectKey = uri.AbsolutePath.TrimStart('/');
                    return await _objectStorageService.GeneratePresignedDownloadUrl(objectKey, 60);
                }
                return objectKeyOrUri;
            }

            // It's an object key - generate presigned URL
            return await _objectStorageService.GeneratePresignedDownloadUrl(objectKeyOrUri, 60);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for object key: {ObjectKey}", objectKeyOrUri);
            return null;
        }
    }
}
