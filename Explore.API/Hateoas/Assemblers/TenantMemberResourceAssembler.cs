// ABOUTME: HAL resource assembler for TenantMember entities.
// ABOUTME: Converts TenantMemberDto and TenantMemberListDto to HAL resources with links.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.TenantMember;

/// <summary>
/// Resource assembler for TenantMember entities (relationship with payload).
/// Converts TenantMemberDto and TenantMemberListDto to HAL resources with appropriate links.
/// </summary>
public sealed class TenantMemberResourceAssembler : ResourceAssemblerBase<TenantMemberDto, TenantMemberListDto>
{
    public TenantMemberResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<TenantMemberDto> detailLinkPolicy,
        ICollectionLinkPolicy<TenantMemberListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    protected override Dictionary<string, object>? GetEmbeddedResources(
        TenantMemberDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        return null;
    }
}
