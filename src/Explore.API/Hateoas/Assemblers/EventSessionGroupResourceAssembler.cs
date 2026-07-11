// ABOUTME: Resource assembler for event program sections/tracks/devrooms.
// ABOUTME: Converts session group DTOs to HAL resources with HATEOAS links.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventSessionGroup;

/// <summary>
/// Resource assembler for event session group entities.
/// </summary>
public sealed class EventSessionGroupResourceAssembler : ResourceAssemblerBase<EventSessionGroupDto, EventSessionGroupListDto>
{
    public EventSessionGroupResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventSessionGroupDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventSessionGroupListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
