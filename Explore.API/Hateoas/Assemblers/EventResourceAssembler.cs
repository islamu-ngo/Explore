namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Event;

/// <summary>
/// Resource assembler for Event entities.
/// Converts EventDto and EventListDto to HAL resources with appropriate links.
/// </summary>
public sealed class EventResourceAssembler : ResourceAssemblerBase<EventDto, EventListDto>
{
    public EventResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded actor resource for event details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        EventDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // For now, we don't embed resources to keep responses lean.
        // In the future, we could embed the actor or sessions if requested.
        return null;
    }
}
