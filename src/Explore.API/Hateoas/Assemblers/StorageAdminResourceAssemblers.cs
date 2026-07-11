// ABOUTME: HAL resource assemblers for instance and tenant storage administration DTOs.
// ABOUTME: Reuses the shared capability-planning pipeline for admin storage affordance links.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.Tenant;

public sealed class InstanceStorageSettingsResourceAssembler
    : ResourceAssemblerBase<InstanceStorageSettingsDto, InstanceStorageSettingsDto>
{
    public InstanceStorageSettingsResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<InstanceStorageSettingsDto> detailLinkPolicy,
        ICollectionLinkPolicy<InstanceStorageSettingsDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}

public sealed class TenantStorageSettingsResourceAssembler
    : ResourceAssemblerBase<TenantStorageSettingsDto, TenantStorageSettingsDto>
{
    public TenantStorageSettingsResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<TenantStorageSettingsDto> detailLinkPolicy,
        ICollectionLinkPolicy<TenantStorageSettingsDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
