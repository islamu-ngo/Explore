// ABOUTME: HAL resource assembler for provider publication operations rows and detail evidence.
// ABOUTME: Delegates every affordance decision to the state-aware publication link policies.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Webhooks;

namespace Explore.API.Hateoas.Assemblers;

public sealed class WebhookProviderPublicationResourceAssembler
    : ResourceAssemblerBase<WebhookProviderPublicationDto, WebhookProviderPublicationDto>
{
    public WebhookProviderPublicationResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<WebhookProviderPublicationDto> detailLinkPolicy,
        ICollectionLinkPolicy<WebhookProviderPublicationDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
