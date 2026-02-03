namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Actor;

/// <summary>
/// Resource assembler for Actor entities.
/// Converts ActorDto and ActorListDto to HAL resources.
/// </summary>
public sealed class ActorResourceAssembler : ResourceAssemblerBase<ActorDto, ActorListDto>
{
    public ActorResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<ActorDto> detailLinkPolicy,
        ICollectionLinkPolicy<ActorListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
