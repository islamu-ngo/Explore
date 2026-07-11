// ABOUTME: HAL resource assembler for projection dirty-scope admin resources.
// ABOUTME: Exposes drain affordances on dirty-scope collection items and collection responses.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.CustomPropertyProjection;

public sealed class ProjectionDirtyScopeResourceAssembler : ResourceAssemblerBase<ProjectionDirtyScopeDto, ProjectionDirtyScopeDto>
{
    public ProjectionDirtyScopeResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<ProjectionDirtyScopeDto> detailLinkPolicy,
        ICollectionLinkPolicy<ProjectionDirtyScopeDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
