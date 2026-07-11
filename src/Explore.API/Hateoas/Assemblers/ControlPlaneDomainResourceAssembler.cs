// ABOUTME: HAL assembler for the multi-tenant control-plane domain/DNS resource.
// ABOUTME: Reuses the shared link authorization pipeline for operator domain affordances.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.ControlPlane;

public sealed class ControlPlaneDomainResourceAssembler
    : ResourceAssemblerBase<ControlPlaneDomainOverviewDto, ControlPlaneDomainOverviewDto>
{
    public ControlPlaneDomainResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<ControlPlaneDomainOverviewDto> detailLinkPolicy,
        ICollectionLinkPolicy<ControlPlaneDomainOverviewDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
