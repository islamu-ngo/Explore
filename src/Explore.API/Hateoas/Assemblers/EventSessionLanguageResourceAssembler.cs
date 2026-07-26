// ABOUTME: Converts event-session language assignments into HAL collection items.
// ABOUTME: Uses the shared batched authorization pipeline for edit affordances.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventSessionLanguage;

public sealed class EventSessionLanguageResourceAssembler
    : ResourceAssemblerBase<EventSessionLanguageDto, EventSessionLanguageListDto>
{
    public EventSessionLanguageResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventSessionLanguageDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventSessionLanguageListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
