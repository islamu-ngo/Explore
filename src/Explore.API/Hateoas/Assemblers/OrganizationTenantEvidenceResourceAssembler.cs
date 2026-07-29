// ABOUTME: HAL resource assembler for OrganizationTenant legitimacy-evidence detail and collection payloads.
// ABOUTME: Applies authorization-aware evidence, review, and protected document link policies.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.OrganizationTenantEvidence;

namespace Explore.API.Hateoas.Assemblers;

public sealed class OrganizationTenantEvidenceResourceAssembler
    : ResourceAssemblerBase<OrganizationTenantEvidenceDto, OrganizationTenantEvidenceDto>
{
    public OrganizationTenantEvidenceResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<OrganizationTenantEvidenceDto> detailLinkPolicy,
        ICollectionLinkPolicy<OrganizationTenantEvidenceDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
