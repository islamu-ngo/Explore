// ABOUTME: Resource assembler for tenant branding typed settings document HAL responses.
// ABOUTME: Keeps typed settings affordances server-driven for Blazor and API clients.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.TenantSettingsDocuments;

public sealed class TenantBrandingSettingsDocumentResourceAssembler
    : ResourceAssemblerBase<TenantBrandingSettingsDocumentDto, TenantBrandingSettingsDocumentDto>
{
    public TenantBrandingSettingsDocumentResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<TenantBrandingSettingsDocumentDto> detailLinkPolicy,
        ICollectionLinkPolicy<TenantBrandingSettingsDocumentDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
