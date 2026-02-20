namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Group;

/// <summary>
/// Resource assembler for Group entities.
/// Converts GroupDto and GroupListDto to HAL resources with appropriate links.
/// </summary>
public sealed class GroupResourceAssembler : ResourceAssemblerBase<GroupDto, GroupListDto>
{
    public GroupResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<GroupDto> detailLinkPolicy,
        ICollectionLinkPolicy<GroupListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    protected override Dictionary<string, object>? GetEmbeddedResources(
        GroupDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        return null;
    }
}
