// ABOUTME: HAL resource assembler for durable webhook bulk replay operations.
// ABOUTME: Delegates collection, detail, and queued-only cancellation affordances to dedicated policies.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Webhooks;

namespace Explore.API.Hateoas.Assemblers;

public sealed class WebhookBulkReplayResourceAssembler
    : ResourceAssemblerBase<WebhookBulkReplayOperationDto, WebhookBulkReplayOperationDto>
{
    public WebhookBulkReplayResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<WebhookBulkReplayOperationDto> detailLinkPolicy,
        ICollectionLinkPolicy<WebhookBulkReplayOperationDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
