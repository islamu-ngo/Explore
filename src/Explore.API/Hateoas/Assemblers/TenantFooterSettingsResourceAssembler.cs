// ABOUTME: Resource assembler for authenticated tenant footer settings HAL responses.
// ABOUTME: Delegates authorization-aware link filtering to the shared HATEOAS pipeline.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Footer;

public sealed class TenantFooterSettingsResourceAssembler
    : ResourceAssemblerBase<TenantFooterSettingsDto, TenantFooterSettingsDto>
{
    public TenantFooterSettingsResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<TenantFooterSettingsDto> detailLinkPolicy,
        ICollectionLinkPolicy<TenantFooterSettingsDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
