// ABOUTME: HAL assembler for control-plane tenant plan SaaS tier resources.
// ABOUTME: Routes tenant plan DTOs through the shared HATEOAS authorization pipeline.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.ControlPlane;

public sealed class ControlPlaneTenantPlanResourceAssembler
    : ResourceAssemblerBase<ControlPlaneTenantPlanDetailDto, ControlPlaneTenantPlanListItemDto>
{
    public ControlPlaneTenantPlanResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<ControlPlaneTenantPlanDetailDto> detailLinkPolicy,
        ICollectionLinkPolicy<ControlPlaneTenantPlanListItemDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
