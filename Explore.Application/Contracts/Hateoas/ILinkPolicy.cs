namespace Explore.Application.Contracts.Hateoas;

using System.Security.Claims;
using Explore.Application.Hateoas;

/// <summary>
/// Defines link generation policy for a specific DTO type.
/// Link policies determine which links should be included for a resource
/// based on the resource state and user authorization.
/// </summary>
/// <typeparam name="TDto">The DTO type this policy applies to.</typeparam>
public interface ILinkPolicy<in TDto> where TDto : class
{
    /// <summary>
    /// Gets the link definitions for a resource.
    /// </summary>
    /// <param name="dto">The resource data.</param>
    /// <param name="user">The current user's claims principal (null if anonymous).</param>
    /// <returns>Collection of link definitions to generate.</returns>
    IEnumerable<LinkDefinition> GetLinks(TDto dto, ClaimsPrincipal? user);
}

/// <summary>
/// Defines link generation policy for collection/list DTOs.
/// Separate from item policy as collections often have different link requirements.
/// </summary>
/// <typeparam name="TDto">The list DTO type this policy applies to.</typeparam>
public interface ICollectionLinkPolicy<in TDto> where TDto : class
{
    /// <summary>
    /// Gets the link definitions for a collection item.
    /// </summary>
    /// <param name="dto">The item data.</param>
    /// <param name="user">The current user's claims principal (null if anonymous).</param>
    /// <returns>Collection of link definitions for this item.</returns>
    IEnumerable<LinkDefinition> GetItemLinks(TDto dto, ClaimsPrincipal? user);

    /// <summary>
    /// Gets the link definitions for the collection itself (create, search, etc.).
    /// </summary>
    /// <param name="user">The current user's claims principal (null if anonymous).</param>
    /// <returns>Collection of link definitions for the collection.</returns>
    IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user);
}
