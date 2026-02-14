namespace Explore.API.Hateoas;

using Explore.Application.Hateoas;
using Explore.Application.Responses;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Assembles DTOs into HAL resources with appropriate links.
/// Each entity type should have its own assembler implementation.
/// This interface lives in the API layer as it depends on HttpContext.
/// </summary>
/// <typeparam name="TDto">The detail DTO type.</typeparam>
/// <typeparam name="TListDto">The list DTO type.</typeparam>
public interface IResourceAssembler<TDto, TListDto>
    where TDto : class
    where TListDto : class
{
    /// <summary>
    /// Converts a detail DTO to a HAL resource with links.
    /// </summary>
    /// <param name="dto">The detail DTO.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A HAL resource with appropriate links.</returns>
    Task<HalResource<TDto>> ToResource(TDto dto, HttpContext httpContext);

    /// <summary>
    /// Converts a list DTO to a HAL resource with links (for collection items).
    /// </summary>
    /// <param name="dto">The list DTO.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A HAL resource with appropriate links.</returns>
    Task<HalResource<TListDto>> ToListResource(TListDto dto, HttpContext httpContext);

    /// <summary>
    /// Converts a paginated result to a HAL collection resource.
    /// </summary>
    /// <param name="paginatedResult">The paginated result containing items and metadata.</param>
    /// <param name="routeName">The route name for generating pagination links.</param>
    /// <param name="additionalRouteValues">Additional route values to preserve in pagination links.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A HAL collection resource with pagination links and embedded items.</returns>
    Task<HalCollectionResource<TListDto>> ToCollectionResource(
        PaginatedResult<TListDto> paginatedResult,
        string routeName,
        object? additionalRouteValues,
        HttpContext httpContext);

    /// <summary>
    /// Converts a list of DTOs to a HAL collection resource without pagination.
    /// </summary>
    /// <param name="items">The list of DTOs.</param>
    /// <param name="routeName">The route name for the collection.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A HAL collection resource with embedded items.</returns>
    Task<HalCollectionResource<TListDto>> ToCollectionResource(
        IEnumerable<TListDto> items,
        string routeName,
        HttpContext httpContext);
}

/// <summary>
/// Simplified assembler interface for entities with a single DTO type.
/// </summary>
/// <typeparam name="TDto">The DTO type used for both detail and list views.</typeparam>
public interface IResourceAssembler<TDto> : IResourceAssembler<TDto, TDto>
    where TDto : class
{
}
