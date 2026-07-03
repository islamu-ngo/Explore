// ABOUTME: HAL resource assembler for webhook endpoint management rows.
// ABOUTME: Keeps endpoint management affordances discoverable through API-owned link policies.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Webhooks;

public sealed class WebhookEndpointResourceAssembler : ResourceAssemblerBase<WebhookEndpointDto, WebhookEndpointDto>
{
    public WebhookEndpointResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<WebhookEndpointDto> detailLinkPolicy,
        ICollectionLinkPolicy<WebhookEndpointDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
