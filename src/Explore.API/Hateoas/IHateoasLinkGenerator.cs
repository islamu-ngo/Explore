namespace Explore.API.Hateoas;

using Explore.Application.Hateoas;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Generates HAL links using ASP.NET Core's LinkGenerator.
/// This interface lives in the API layer as it depends on HttpContext.
/// </summary>
public interface IHateoasLinkGenerator
{
    /// <summary>
    /// Generates a HalLink from a link definition.
    /// </summary>
    /// <param name="definition">The link definition containing route name and values.</param>
    /// <param name="httpContext">The current HTTP context for URL generation.</param>
    /// <returns>A HalLink with the generated URL, or null if URL generation failed.</returns>
    HalLink? GenerateLink(LinkDefinition definition, HttpContext httpContext);

    /// <summary>
    /// Generates a URL path for a named route.
    /// </summary>
    /// <param name="routeName">The name of the route.</param>
    /// <param name="routeValues">The route values.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>The generated URL path, or null if generation failed.</returns>
    string? GeneratePath(string routeName, object? routeValues, HttpContext httpContext);

    /// <summary>
    /// Generates pagination links for a collection.
    /// </summary>
    /// <param name="routeName">The route name for the collection endpoint.</param>
    /// <param name="pageNumber">Current page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="totalPages">Total number of pages.</param>
    /// <param name="additionalRouteValues">Additional route values to preserve (filters, etc.).</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>Dictionary of pagination links (self, first, prev, next, last).</returns>
    Dictionary<string, HalLink> GeneratePaginationLinks(
        string routeName,
        int pageNumber,
        int pageSize,
        int totalPages,
        object? additionalRouteValues,
        HttpContext httpContext);
}
