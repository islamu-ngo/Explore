// ABOUTME: HAL assemblers for instance and tenant paid-event policy settings resources.
// ABOUTME: Wraps paid-policy management DTOs without placing authorization logic in controllers.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.PaidEventPolicies;

namespace Explore.API.Hateoas.Assemblers;

public sealed class InstancePaidEventPolicyResourceAssembler(
    IHateoasLinkGenerator linkGenerator,
    ILinkPolicy<PaidEventPolicyDto> detailPolicy,
    ICollectionLinkPolicy<PaidEventPolicyDto> collectionPolicy)
    : ResourceAssemblerBase<PaidEventPolicyDto, PaidEventPolicyDto>(linkGenerator, detailPolicy, collectionPolicy);

public sealed class TenantPaidEventPolicyConfigurationResourceAssembler(
    IHateoasLinkGenerator linkGenerator,
    ILinkPolicy<TenantPaidEventPolicyConfigurationDto> detailPolicy,
    ICollectionLinkPolicy<TenantPaidEventPolicyConfigurationDto> collectionPolicy)
    : ResourceAssemblerBase<TenantPaidEventPolicyConfigurationDto, TenantPaidEventPolicyConfigurationDto>(linkGenerator, detailPolicy, collectionPolicy);
