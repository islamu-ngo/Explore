using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
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
    private readonly IMapper _mapper;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ILogger<GetEventListRequestHandler> _logger;
    private readonly HybridCache _cache;
    private readonly IModuleService _moduleService;
    private readonly ITenantContext _tenantContext;

    public GetEventListRequestHandler(
        IEventRepository eventRepository,
        IMapper mapper,
        IObjectStorageService objectStorageService,
        ILogger<GetEventListRequestHandler> logger,
        HybridCache cache,
        IModuleService moduleService,
        ITenantContext tenantContext)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
        _objectStorageService = objectStorageService;
        _logger = logger;
        _cache = cache;
        _moduleService = moduleService;
        _tenantContext = tenantContext;
    }

    public async Task<PaginatedResult<EventListDto>> Handle(GetEventListRequest request, CancellationToken cancellationToken)
    {
        var specification = await BuildSpecificationAsync(request, cancellationToken);
        var cacheKeySuffix = specification.ToCacheKeySuffix();
        var cacheKey = $"events:list:{request.PageNumber}:{request.PageSize}:{cacheKeySuffix}";

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
        GetEventListRequest request, CancellationToken cancellationToken)
    {
        var spec = new EventQuerySpecification();

        // ===== Core Event filters (always available) =====

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            spec = spec.And(EventFilter.SearchTerm(request.SearchTerm.Trim()));

        if (request.FormatId.HasValue)
            spec = spec.And(EventFilter.Format(request.FormatId.Value));

        if (request.MadhabId.HasValue)
            spec = spec.And(EventFilter.Madhab(request.MadhabId.Value));

        if (request.EventTypeId.HasValue)
            spec = spec.And(EventFilter.EventType(request.EventTypeId.Value));

        if (request.AudienceGenderId.HasValue)
            spec = spec.And(EventFilter.AudienceGender(request.AudienceGenderId.Value));

        if (request.AudienceAgeId.HasValue)
            spec = spec.And(EventFilter.AudienceAge(request.AudienceAgeId.Value));

        if (request.EventStatusId.HasValue)
            spec = spec.And(EventFilter.Status(request.EventStatusId.Value));

        if (request.DateFrom.HasValue)
            spec = spec.And(EventFilter.DateFrom(request.DateFrom.Value));

        if (request.DateTo.HasValue)
            spec = spec.And(EventFilter.DateTo(request.DateTo.Value));

        // ===== Subquery filters (junction tables — always available) =====

        if (request.CategoryId.HasValue)
            spec = spec.And(EventSubqueryFilter.Category(request.CategoryId.Value));

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

        if (request.LocationId.HasValue)
            spec = spec.And(EventSubqueryFilter.Location(request.LocationId.Value));

        if (request.LanguageId.HasValue)
            spec = spec.And(EventSubqueryFilter.Language(request.LanguageId.Value));

        if (request.RegistrationModeId.HasValue)
            spec = spec.And(EventSubqueryFilter.RegistrationMode(request.RegistrationModeId.Value));

        // ===== JSONB metadata filters (always available) =====

        if (!string.IsNullOrWhiteSpace(request.MetadataJsonContains))
            spec = spec.And(EventSubqueryFilter.JsonContains(request.MetadataJsonContains.Trim()));

        if (!string.IsNullOrWhiteSpace(request.MetadataJsonKeyExists))
            spec = spec.And(EventSubqueryFilter.JsonKeyExists(request.MetadataJsonKeyExists.Trim()));

        // ===== Islamic aspect filters (module-conditional) =====

        var tenantId = _tenantContext.TenantId;
        var hasIslamicAspectFilters = request.GenderModeId.HasValue
            || request.IncludesQuranRecitation is true
            || request.ReferencePrayerId.HasValue
            || request.IslamicPrimaryLanguageId.HasValue
            || request.HasIslamicAspect is true;

        if (hasIslamicAspectFilters &&
            await _moduleService.IsModuleEnabledAsync(tenantId, "Mod_Islamic", cancellationToken))
        {
            if (request.HasIslamicAspect is true)
                spec = spec.And(AspectPresenceFilter.HasIslamicAspect());

            if (request.GenderModeId.HasValue)
                spec = spec.And(IslamicAspectFilter.GenderMode((GenderSegregationMode)request.GenderModeId.Value));

            if (request.IncludesQuranRecitation is true)
                spec = spec.And(IslamicAspectFilter.IncludesQuranRecitation());

            if (request.ReferencePrayerId.HasValue)
                spec = spec.And(IslamicAspectFilter.ReferencePrayer((PrayerTime)request.ReferencePrayerId.Value));

            if (request.IslamicPrimaryLanguageId.HasValue)
                spec = spec.And(IslamicAspectFilter.PrimaryLanguage(request.IslamicPrimaryLanguageId.Value));
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
