namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.TenantUser;

/// <summary>
/// Resource assembler for TenantUser entities (relationship with payload).
/// Converts TenantUserDto and TenantUserListDto to HAL resources with appropriate links.
/// </summary>
public sealed class TenantUserResourceAssembler : ResourceAssemblerBase<TenantUserDto, TenantUserListDto>
{
    public TenantUserResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<TenantUserDto> detailLinkPolicy,
        ICollectionLinkPolicy<TenantUserListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded resources for tenant user details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        TenantUserDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // TenantUser links to User and Tenant via _links, not _embedded
        return null;
    }
}
