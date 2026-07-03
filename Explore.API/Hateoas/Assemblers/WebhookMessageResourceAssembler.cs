// ABOUTME: HAL resource assembler for webhook message audit rows.
// ABOUTME: Keeps delivery-history affordances discoverable through API-owned link policies.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Webhooks;

public sealed class WebhookMessageResourceAssembler : ResourceAssemblerBase<WebhookMessageDto, WebhookMessageDto>
{
    public WebhookMessageResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<WebhookMessageDto> detailLinkPolicy,
        ICollectionLinkPolicy<WebhookMessageDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
