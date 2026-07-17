// ABOUTME: HAL assembler for operator-safe incoming Coop effect status rows.
// ABOUTME: Applies server-authored redrive affordances to collection items.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Webhooks;

namespace Explore.API.Hateoas.Assemblers;

public sealed class IncomingWebhookEffectStatusResourceAssembler(
    IHateoasLinkGenerator linkGenerator,
    ILinkPolicy<IncomingWebhookEffectStatusDto> detailLinkPolicy,
    ICollectionLinkPolicy<IncomingWebhookEffectStatusDto> collectionLinkPolicy)
    : ResourceAssemblerBase<IncomingWebhookEffectStatusDto, IncomingWebhookEffectStatusDto>(
        linkGenerator,
        detailLinkPolicy,
        collectionLinkPolicy);
