namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.OrganizationMember;

/// <summary>
/// Resource assembler for OrganizationMember entities (relationship with payload).
/// Converts OrganizationMemberDto to HAL resources with appropriate links.
/// Note: OrganizationMember uses same DTO for detail and list views.
/// </summary>
public sealed class OrganizationMemberResourceAssembler : ResourceAssemblerBase<OrganizationMemberDto, OrganizationMemberDto>
{
    public OrganizationMemberResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<OrganizationMemberDto> detailLinkPolicy,
        ICollectionLinkPolicy<OrganizationMemberDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded resources for organization member details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        OrganizationMemberDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Members link to User, Organization, Role via _links
        return null;
    }
}
