// ABOUTME: Assembles paid publication preflight DTOs into HAL resources.
// ABOUTME: Reuses the shared authorization-aware resource assembler pipeline.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventTicketing;

namespace Explore.API.Hateoas.Assemblers;

public sealed class PaidEventPublicationPreflightResourceAssembler
    : ResourceAssemblerBase<PaidEventPublicationPreflightDto, PaidEventPublicationPreflightDto>
{
    public PaidEventPublicationPreflightResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<PaidEventPublicationPreflightDto> detailLinkPolicy,
        ICollectionLinkPolicy<PaidEventPublicationPreflightDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
