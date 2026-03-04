// ABOUTME: HAL resource assembler for GroupMember entities.
// ABOUTME: Uses same DTO for detail and list views, matching OrganizationMember pattern.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.GroupMember;

public sealed class GroupMemberResourceAssembler : ResourceAssemblerBase<GroupMemberDto, GroupMemberDto>
{
    public GroupMemberResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<GroupMemberDto> detailLinkPolicy,
        ICollectionLinkPolicy<GroupMemberDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    protected override Dictionary<string, object>? GetEmbeddedResources(
        GroupMemberDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        return null;
    }
}
