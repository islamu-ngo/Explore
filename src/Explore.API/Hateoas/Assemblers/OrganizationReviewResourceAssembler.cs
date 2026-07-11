namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.OrganizationReview;

/// <summary>
/// Resource assembler for OrganizationReview entities.
/// Converts OrganizationReviewDto to HAL resources with appropriate links.
/// Note: OrganizationReview uses same DTO for detail and list views.
/// </summary>
public sealed class OrganizationReviewResourceAssembler : ResourceAssemblerBase<OrganizationReviewDto, OrganizationReviewDto>
{
    public OrganizationReviewResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<OrganizationReviewDto> detailLinkPolicy,
        ICollectionLinkPolicy<OrganizationReviewDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded resources for organization review details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        OrganizationReviewDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Reviews link to Organization, User via _links
        return null;
    }
}
