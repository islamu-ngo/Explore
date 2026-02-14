namespace Explore.API.Hateoas;

using System.Security.Claims;
using Explore.Application.Contracts.Hateoas;  // For ILinkPolicy, ICollectionLinkPolicy
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Base class for resource assemblers that provides common link generation logic.
/// </summary>
/// <typeparam name="TDto">The detail DTO type.</typeparam>
/// <typeparam name="TListDto">The list DTO type.</typeparam>
public abstract class ResourceAssemblerBase<TDto, TListDto> : IResourceAssembler<TDto, TListDto>
    where TDto : class
    where TListDto : class
{
    private readonly IHateoasLinkGenerator _linkGenerator;
    private readonly ILinkPolicy<TDto> _detailLinkPolicy;
    private readonly ICollectionLinkPolicy<TListDto> _collectionLinkPolicy;

    protected ResourceAssemblerBase(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<TDto> detailLinkPolicy,
        ICollectionLinkPolicy<TListDto> collectionLinkPolicy)
    {
        _linkGenerator = linkGenerator;
        _detailLinkPolicy = detailLinkPolicy;
        _collectionLinkPolicy = collectionLinkPolicy;
    }

    /// <inheritdoc />
    public virtual async Task<HalResource<TDto>> ToResource(TDto dto, HttpContext httpContext)
    {
        if (IsMinimalResponse(httpContext))
        {
            return new HalResource<TDto>(dto);
        }

        var user = httpContext.User;
        var links = await GenerateLinks(_detailLinkPolicy.GetLinks(dto, user), user, httpContext);

        return new HalResource<TDto>
        {
            Data = dto,
            Links = links,
            Embedded = GetEmbeddedResources(dto, httpContext)
        };
    }

    /// <inheritdoc />
    public virtual async Task<HalResource<TListDto>> ToListResource(TListDto dto, HttpContext httpContext)
    {
        if (IsMinimalResponse(httpContext))
        {
            return new HalResource<TListDto>(dto);
        }

        var user = httpContext.User;
        var links = await GenerateLinks(_collectionLinkPolicy.GetItemLinks(dto, user), user, httpContext);

        return new HalResource<TListDto>
        {
            Data = dto,
            Links = links
        };
    }

    /// <inheritdoc />
    public virtual async Task<HalCollectionResource<TListDto>> ToCollectionResource(
        PaginatedResult<TListDto> paginatedResult,
        string routeName,
        object? additionalRouteValues,
        HttpContext httpContext)
    {
        var user = httpContext.User;
        var isMinimal = IsMinimalResponse(httpContext);

        var items = await BuildListResourcesWithBatch(paginatedResult.Items, user, httpContext);

        if (isMinimal)
        {
            return new HalCollectionResource<TListDto>
            {
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize,
                TotalCount = paginatedResult.TotalCount,
                TotalPages = paginatedResult.TotalPages,
                Embedded = new HalCollectionEmbedded<TListDto> { Items = items }
            };
        }

        // Generate pagination links
        var links = _linkGenerator.GeneratePaginationLinks(
            routeName,
            paginatedResult.PageNumber,
            paginatedResult.PageSize,
            paginatedResult.TotalPages,
            additionalRouteValues,
            httpContext);

        // Add collection-level links (create, search, etc.)
        var collectionActionLinks = await GenerateLinks(_collectionLinkPolicy.GetCollectionLinks(user), user, httpContext);
        foreach (var pair in collectionActionLinks)
        {
            links[pair.Key] = pair.Value;
        }

        return HalCollectionResource<TListDto>.FromPagination(
            items,
            paginatedResult.PageNumber,
            paginatedResult.PageSize,
            paginatedResult.TotalCount,
            paginatedResult.TotalPages,
            links);
    }

    /// <inheritdoc />
    public virtual async Task<HalCollectionResource<TListDto>> ToCollectionResource(
        IEnumerable<TListDto> items,
        string routeName,
        HttpContext httpContext)
    {
        var itemsList = items.ToList();
        var user = httpContext.User;
        var isMinimal = IsMinimalResponse(httpContext);

        var halItems = await BuildListResourcesWithBatch(itemsList, user, httpContext);

        var links = new Dictionary<string, HalLink>();

        if (!isMinimal)
        {
            // Self link for the collection
            var selfPath = _linkGenerator.GeneratePath(routeName, null, httpContext);
            if (selfPath is not null)
            {
                links[LinkRelations.Self] = HalLink.Create(selfPath);
            }

            // Collection-level links
            var collectionActionLinks = await GenerateLinks(_collectionLinkPolicy.GetCollectionLinks(user), user, httpContext);
            foreach (var pair in collectionActionLinks)
            {
                links[pair.Key] = pair.Value;
            }
        }

        return new HalCollectionResource<TListDto>
        {
            PageNumber = 1,
            PageSize = itemsList.Count,
            TotalCount = itemsList.Count,
            TotalPages = 1,
            Links = links,
            Embedded = new HalCollectionEmbedded<TListDto> { Items = halItems }
        };
    }

    /// <summary>
    /// Override to provide embedded resources for detail views.
    /// </summary>
    protected virtual Dictionary<string, object>? GetEmbeddedResources(TDto dto, HttpContext httpContext)
    {
        return null;
    }

    /// <summary>
    /// Generates HAL links from link definitions, filtering by authorization.
    /// </summary>
    protected async Task<Dictionary<string, HalLink>> GenerateLinks(
        IEnumerable<LinkDefinition> definitions,
        ClaimsPrincipal? user,
        HttpContext httpContext)
    {
        var definitionList = definitions.ToList();
        if (definitionList.Count == 0)
            return new Dictionary<string, HalLink>();

        var evaluator = httpContext.RequestServices.GetRequiredService<IHateoasAuthorizationEvaluator>();
        var decisions = await evaluator.AreLinksAllowedAsync(definitionList, user, httpContext);

        var links = new Dictionary<string, HalLink>();

        for (var index = 0; index < definitionList.Count; index++)
        {
            if (index >= decisions.Count || !decisions[index])
                continue;

            var definition = definitionList[index];

            var link = _linkGenerator.GenerateLink(definition, httpContext);
            if (link is not null)
            {
                links[definition.Rel] = link;
            }
        }

        return links;
    }

    private async Task<List<HalResource<TListDto>>> BuildListResourcesWithBatch(
        IEnumerable<TListDto> items,
        ClaimsPrincipal? user,
        HttpContext httpContext)
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
            return [];

        var definitionsByItem = itemList
            .Select(item => _collectionLinkPolicy.GetItemLinks(item, user).ToList())
            .ToList();

        var flattenedDefinitions = definitionsByItem.SelectMany(x => x).ToList();
        var evaluator = httpContext.RequestServices.GetRequiredService<IHateoasAuthorizationEvaluator>();
        var decisions = await evaluator.AreLinksAllowedAsync(flattenedDefinitions, user, httpContext);

        var resources = new List<HalResource<TListDto>>(itemList.Count);
        var cursor = 0;

        for (var itemIndex = 0; itemIndex < itemList.Count; itemIndex++)
        {
            var itemDefinitions = definitionsByItem[itemIndex];
            var links = new Dictionary<string, HalLink>();

            for (var definitionIndex = 0; definitionIndex < itemDefinitions.Count; definitionIndex++)
            {
                var globalIndex = cursor + definitionIndex;
                if (globalIndex >= decisions.Count || !decisions[globalIndex])
                    continue;

                var definition = itemDefinitions[definitionIndex];
                var link = _linkGenerator.GenerateLink(definition, httpContext);
                if (link is not null)
                {
                    links[definition.Rel] = link;
                }
            }

            cursor += itemDefinitions.Count;
            resources.Add(new HalResource<TListDto>
            {
                Data = itemList[itemIndex],
                Links = links
            });
        }

        return resources;
    }

    /// <summary>
    /// Checks if the client requested minimal response (Prefer: return=minimal).
    /// </summary>
    protected static bool IsMinimalResponse(HttpContext httpContext)
    {
        return httpContext.Items.TryGetValue(HateoasConstants.MinimalResponseKey, out var value)
            && value is true;
    }
}

/// <summary>
/// Base class for resource assemblers with a single DTO type.
/// </summary>
/// <typeparam name="TDto">The DTO type used for both detail and list views.</typeparam>
public abstract class ResourceAssemblerBase<TDto> : ResourceAssemblerBase<TDto, TDto>, IResourceAssembler<TDto>
    where TDto : class
{
    protected ResourceAssemblerBase(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<TDto> detailLinkPolicy,
        ICollectionLinkPolicy<TDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
