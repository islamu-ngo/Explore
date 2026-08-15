// ABOUTME: Assembles organizer promotion management DTOs through the shared HAL pipeline.
// ABOUTME: Keeps promotion code secrets out of HAL while batching paid-commerce link authorization.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.Features.Promotions;

namespace Explore.API.Hateoas.Assemblers;

public sealed class PromotionManagementResourceAssembler
    : ResourceAssemblerBase<PromotionManagementDto, PromotionManagementDto>
{
    public PromotionManagementResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<PromotionManagementDto> detailLinkPolicy,
        ICollectionLinkPolicy<PromotionManagementDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
