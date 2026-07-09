// ABOUTME: HAL assembler for one tenant's effective Control Plane configuration.
// ABOUTME: Keeps tenant configuration affordances behind the shared authorization-aware link pipeline.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.ControlPlane;

public sealed class ControlPlaneTenantEffectiveConfigurationResourceAssembler
    : ResourceAssemblerBase<ControlPlaneTenantEffectiveConfigurationDto, ControlPlaneTenantEffectiveConfigurationDto>
{
    public ControlPlaneTenantEffectiveConfigurationResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<ControlPlaneTenantEffectiveConfigurationDto> detailLinkPolicy,
        ICollectionLinkPolicy<ControlPlaneTenantEffectiveConfigurationDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
