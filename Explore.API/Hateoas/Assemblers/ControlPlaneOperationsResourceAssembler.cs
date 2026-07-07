// ABOUTME: HAL assembler for the Control Plane operations resource.
// ABOUTME: Reuses the shared link authorization pipeline for operational status affordances.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.ControlPlane;

public sealed class ControlPlaneOperationsResourceAssembler
    : ResourceAssemblerBase<ControlPlaneOperationsDto, ControlPlaneOperationsDto>
{
    public ControlPlaneOperationsResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<ControlPlaneOperationsDto> detailLinkPolicy,
        ICollectionLinkPolicy<ControlPlaneOperationsDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
