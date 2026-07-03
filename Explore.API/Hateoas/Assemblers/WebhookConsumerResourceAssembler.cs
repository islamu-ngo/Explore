// ABOUTME: HAL resource assembler for webhook consumer management rows.
// ABOUTME: Lets clients discover webhook management affordances through API-owned link policies.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Webhooks;

public sealed class WebhookConsumerResourceAssembler : ResourceAssemblerBase<WebhookConsumerDto, WebhookConsumerDto>
{
    public WebhookConsumerResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<WebhookConsumerDto> detailLinkPolicy,
        ICollectionLinkPolicy<WebhookConsumerDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
