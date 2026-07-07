// ABOUTME: HAL assembler for the Control Plane deployment-mode runbook resource.
// ABOUTME: Applies link policies so operators discover only authorized migration actions.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.ControlPlane;

namespace Explore.API.Hateoas.Assemblers;

public sealed class ControlPlaneDeploymentModeRunbookResourceAssembler
    : ResourceAssemblerBase<ControlPlaneDeploymentModeRunbookDto, ControlPlaneDeploymentModeRunbookDto>
{
    public ControlPlaneDeploymentModeRunbookResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<ControlPlaneDeploymentModeRunbookDto> detailLinkPolicy,
        ICollectionLinkPolicy<ControlPlaneDeploymentModeRunbookDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
