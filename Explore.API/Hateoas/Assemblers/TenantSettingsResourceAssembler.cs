namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.TenantSettings;

/// <summary>
/// Resource assembler for TenantSettings entities.
/// Converts TenantSettingsDto and TenantSettingsListDto to HAL resources with appropriate links.
/// </summary>
public sealed class TenantSettingsResourceAssembler : ResourceAssemblerBase<TenantSettingsDto, TenantSettingsListDto>
{
    public TenantSettingsResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<TenantSettingsDto> detailLinkPolicy,
        ICollectionLinkPolicy<TenantSettingsListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded resources for tenant settings details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        TenantSettingsDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Settings link to parent Tenant via _links
        return null;
    }
}
