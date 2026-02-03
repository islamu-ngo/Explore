namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventRegistration;

/// <summary>
/// Resource assembler for EventRegistration entities (relationship with payload).
/// Converts EventRegistrationDto and EventRegistrationListDto to HAL resources with appropriate links.
/// </summary>
public sealed class EventRegistrationResourceAssembler : ResourceAssemblerBase<EventRegistrationDto, EventRegistrationListDto>
{
    public EventRegistrationResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventRegistrationDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventRegistrationListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded resources for event registration details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        EventRegistrationDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Registrations link to User, EventSession via _links
        return null;
    }
}
