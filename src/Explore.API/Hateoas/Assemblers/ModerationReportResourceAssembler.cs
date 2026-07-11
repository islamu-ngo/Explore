// ABOUTME: HAL resource assembler for moderation report detail and queue rows.
// ABOUTME: Uses shared ResourceAssemblerBase authorization batching for report action affordances.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventReporting;

public sealed class ModerationReportResourceAssembler
    : ResourceAssemblerBase<ModerationReportDetailDto, ModerationReportQueueItemDto>
{
    public ModerationReportResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<ModerationReportDetailDto> detailLinkPolicy,
        ICollectionLinkPolicy<ModerationReportQueueItemDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
