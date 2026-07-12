// ABOUTME: HAL assembler for tenant onboarding status resources.
// ABOUTME: Applies tenant/platform link policies through the shared authorization pipeline.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Onboarding;

public sealed class TenantOnboardingStatusResourceAssembler
    : ResourceAssemblerBase<TenantOnboardingStatusDto, TenantOnboardingStatusDto>
{
    public TenantOnboardingStatusResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<TenantOnboardingStatusDto> detailLinkPolicy,
        ICollectionLinkPolicy<TenantOnboardingStatusDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
