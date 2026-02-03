namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Tenant;

/// <summary>
/// Resource assembler for Tenant entities.
/// Converts TenantDto and TenantListDto to HAL resources with appropriate links.
/// </summary>
public sealed class TenantResourceAssembler : ResourceAssemblerBase<TenantDto, TenantListDto>
{
    public TenantResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<TenantDto> detailLinkPolicy,
        ICollectionLinkPolicy<TenantListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded resources for tenant details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        TenantDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Tenants don't embed other resources by default
        return null;
    }
}
