namespace Explore.API.Hateoas;

using System.Security.Claims;
using Explore.Application.Contracts.Hateoas;  // For ILinkPolicy, ICollectionLinkPolicy
using Explore.Application.Hateoas;
using Explore.Application.Responses;
using Microsoft.AspNetCore.Http;

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
    public virtual HalResource<TDto> ToResource(TDto dto, HttpContext httpContext)
    {
        if (IsMinimalResponse(httpContext))
        {
            return new HalResource<TDto>(dto);
        }

        var user = httpContext.User;
        var links = GenerateLinks(_detailLinkPolicy.GetLinks(dto, user), user, httpContext);

        return new HalResource<TDto>
        {
            Data = dto,
            Links = links,
            Embedded = GetEmbeddedResources(dto, httpContext)
        };
    }

    /// <inheritdoc />
    public virtual HalResource<TListDto> ToListResource(TListDto dto, HttpContext httpContext)
    {
        if (IsMinimalResponse(httpContext))
        {
            return new HalResource<TListDto>(dto);
        }

        var user = httpContext.User;
        var links = GenerateLinks(_collectionLinkPolicy.GetItemLinks(dto, user), user, httpContext);

        return new HalResource<TListDto>
        {
            Data = dto,
            Links = links
        };
    }

    /// <inheritdoc />
    public virtual HalCollectionResource<TListDto> ToCollectionResource(
        PaginatedResult<TListDto> paginatedResult,
        string routeName,
        object? additionalRouteValues,
        HttpContext httpContext)
    {
        var user = httpContext.User;
        var isMinimal = IsMinimalResponse(httpContext);

        // Generate item resources
        var items = paginatedResult.Items
            .Select(item => ToListResource(item, httpContext))
            .ToList();

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
        var collectionLinks = _collectionLinkPolicy.GetCollectionLinks(user);
        foreach (var linkDef in collectionLinks)
        {
            if (ShouldIncludeLink(linkDef, user))
            {
                var link = _linkGenerator.GenerateLink(linkDef, httpContext);
                if (link is not null)
                {
                    links[linkDef.Rel] = link;
                }
            }
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
    public virtual HalCollectionResource<TListDto> ToCollectionResource(
        IEnumerable<TListDto> items,
        string routeName,
        HttpContext httpContext)
    {
        var itemsList = items.ToList();
        var user = httpContext.User;
        var isMinimal = IsMinimalResponse(httpContext);

        var halItems = itemsList
            .Select(item => ToListResource(item, httpContext))
            .ToList();

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
            var collectionLinks = _collectionLinkPolicy.GetCollectionLinks(user);
            foreach (var linkDef in collectionLinks)
            {
                if (ShouldIncludeLink(linkDef, user))
                {
                    var link = _linkGenerator.GenerateLink(linkDef, httpContext);
                    if (link is not null)
                    {
                        links[linkDef.Rel] = link;
                    }
                }
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
    protected Dictionary<string, HalLink> GenerateLinks(
        IEnumerable<LinkDefinition> definitions,
        ClaimsPrincipal? user,
        HttpContext httpContext)
    {
        var links = new Dictionary<string, HalLink>();

        foreach (var definition in definitions)
        {
            if (!ShouldIncludeLink(definition, user))
                continue;

            var link = _linkGenerator.GenerateLink(definition, httpContext);
            if (link is not null)
            {
                links[definition.Rel] = link;
            }
        }

        return links;
    }

    /// <summary>
    /// Determines if a link should be included based on authorization and conditions.
    /// </summary>
    protected virtual bool ShouldIncludeLink(LinkDefinition definition, ClaimsPrincipal? user)
    {
        // Check custom condition first
        if (definition.Condition is not null && !definition.Condition())
            return false;

        // Check authentication requirement
        if (definition.RequiresAuth)
        {
            if (user?.Identity?.IsAuthenticated != true)
                return false;

            // Check role requirements
            if (definition.RequiredRoles is { Length: > 0 })
            {
                var hasRequiredRole = definition.RequiredRoles.Any(role =>
                    user.IsInRole(role));

                if (!hasRequiredRole)
                    return false;
            }
        }

        return true;
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
