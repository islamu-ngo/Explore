namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Organization;

/// <summary>
/// Resource assembler for Organization entities.
/// Converts OrganizationDto and OrganizationListDto to HAL resources with appropriate links.
/// </summary>
public sealed class OrganizationResourceAssembler : ResourceAssemblerBase<OrganizationDto, OrganizationListDto>
{
    public OrganizationResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<OrganizationDto> detailLinkPolicy,
        ICollectionLinkPolicy<OrganizationListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded actor resource for organization details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        OrganizationDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // For now, we don't embed resources to keep responses lean.
        // In the future, we could embed the actor if requested.
        return null;
    }
}
