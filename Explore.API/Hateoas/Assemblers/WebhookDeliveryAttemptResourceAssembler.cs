// ABOUTME: HAL resource assembler for webhook delivery attempt audit rows.
// ABOUTME: Emits retry affordances from server-authorized delivery attempt link policies.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Webhooks;

public sealed class WebhookDeliveryAttemptResourceAssembler
    : ResourceAssemblerBase<WebhookDeliveryAttemptDto, WebhookDeliveryAttemptDto>
{
    public WebhookDeliveryAttemptResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<WebhookDeliveryAttemptDto> detailLinkPolicy,
        ICollectionLinkPolicy<WebhookDeliveryAttemptDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
