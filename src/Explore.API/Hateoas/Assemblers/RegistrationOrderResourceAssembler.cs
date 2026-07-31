// ABOUTME: Assembles registration orders through the shared batched authorization pipeline.
// ABOUTME: Keeps opaque guest capabilities outside HAL resources and generated routes.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.RegistrationOrders;

namespace Explore.API.Hateoas.Assemblers;

public sealed class RegistrationOrderResourceAssembler
    : ResourceAssemblerBase<RegistrationOrderDto, RegistrationOrderDto>
{
    public RegistrationOrderResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<RegistrationOrderDto> detailLinkPolicy,
        ICollectionLinkPolicy<RegistrationOrderDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
