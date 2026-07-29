// ABOUTME: Assembles platform monetization settings into a HAL resource through the shared capability pipeline.
// ABOUTME: Applies the dedicated detail and collection policies without adding controller-side authorization logic.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.PlatformMonetization;

namespace Explore.API.Hateoas.Assemblers;

public sealed class PlatformMonetizationSettingsResourceAssembler
    : ResourceAssemblerBase<PlatformMonetizationSettingsDto, PlatformMonetizationSettingsDto>
{
    public PlatformMonetizationSettingsResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<PlatformMonetizationSettingsDto> detailLinkPolicy,
        ICollectionLinkPolicy<PlatformMonetizationSettingsDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
