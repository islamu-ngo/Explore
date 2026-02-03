namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.User;

/// <summary>
/// Resource assembler for User entities.
/// Converts UserDto to HAL resources with appropriate links.
/// Note: User uses same DTO for detail and list views.
/// </summary>
public sealed class UserResourceAssembler : ResourceAssemblerBase<UserDto, UserDto>
{
    public UserResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<UserDto> detailLinkPolicy,
        ICollectionLinkPolicy<UserDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded resources for user details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        UserDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Users don't embed other resources by default
        return null;
    }
}
