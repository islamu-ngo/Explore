// ABOUTME: HAL assembler for tenant moderation-reporting dashboard resources.
// ABOUTME: Wraps redacted tenant queue/provider health with server-authorized reporting links.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventReporting;

public sealed class TenantModerationReportingDashboardResourceAssembler(
    IHateoasLinkGenerator linkGenerator,
    ILinkPolicy<TenantModerationReportingDashboardDto> detailPolicy,
    ICollectionLinkPolicy<TenantModerationReportingDashboardDto> collectionPolicy)
    : ResourceAssemblerBase<TenantModerationReportingDashboardDto, TenantModerationReportingDashboardDto>(
        linkGenerator,
        detailPolicy,
        collectionPolicy);
