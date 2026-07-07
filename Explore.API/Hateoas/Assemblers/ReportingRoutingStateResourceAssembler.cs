// ABOUTME: HAL assembler for moderation reporting provider routing-state resources.
// ABOUTME: Wraps redacted routing-state DTOs with server-authorized tenant settings links.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventReporting;

public sealed class ReportingRoutingStateResourceAssembler(
    IHateoasLinkGenerator linkGenerator,
    ILinkPolicy<ReportingRoutingStateDto> detailPolicy,
    ICollectionLinkPolicy<ReportingRoutingStateDto> collectionPolicy)
    : ResourceAssemblerBase<ReportingRoutingStateDto, ReportingRoutingStateDto>(linkGenerator, detailPolicy, collectionPolicy);
