// ABOUTME: HAL assembler for instance onboarding status resources.
// ABOUTME: Applies setup/admin link policies through the shared authorization pipeline.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Onboarding;

public sealed class InstanceOnboardingStatusResourceAssembler
    : ResourceAssemblerBase<InstanceOnboardingStatusDto, InstanceOnboardingStatusDto>
{
    public InstanceOnboardingStatusResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<InstanceOnboardingStatusDto> detailLinkPolicy,
        ICollectionLinkPolicy<InstanceOnboardingStatusDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
