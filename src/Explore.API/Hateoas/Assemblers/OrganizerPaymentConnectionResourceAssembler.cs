// ABOUTME: Assembles organizer payment connection DTOs into bounded HAL resources.
// ABOUTME: Keeps provider state link generation out of controllers.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.OrganizerPaymentConnections;

namespace Explore.API.Hateoas.Assemblers;

public sealed class OrganizerPaymentConnectionResourceAssembler
    : ResourceAssemblerBase<EventOrganizerPaymentConnectionManagementDto, EventOrganizerPaymentConnectionManagementDto>
{
    public OrganizerPaymentConnectionResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventOrganizerPaymentConnectionManagementDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventOrganizerPaymentConnectionManagementDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
