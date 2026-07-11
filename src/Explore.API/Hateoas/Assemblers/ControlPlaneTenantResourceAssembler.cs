// ABOUTME: HAL assembler for control-plane tenant lifecycle resources.
// ABOUTME: Reuses the shared HATEOAS authorization pipeline for tenant fleet affordances.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.ControlPlane;

public sealed class ControlPlaneTenantResourceAssembler
    : ResourceAssemblerBase<ControlPlaneTenantDetailDto, ControlPlaneTenantListItemDto>
{
    public ControlPlaneTenantResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<ControlPlaneTenantDetailDto> detailLinkPolicy,
        ICollectionLinkPolicy<ControlPlaneTenantListItemDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
