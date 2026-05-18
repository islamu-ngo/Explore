// ABOUTME: HAL resource assembler for projection status admin resources.
// ABOUTME: Lets projection status list responses expose server-authored rebuild/drain affordances.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.CustomPropertyProjection;

public sealed class ProjectionStatusResourceAssembler : ResourceAssemblerBase<ProjectionStatusDto, ProjectionStatusDto>
{
    public ProjectionStatusResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<ProjectionStatusDto> detailLinkPolicy,
        ICollectionLinkPolicy<ProjectionStatusDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
