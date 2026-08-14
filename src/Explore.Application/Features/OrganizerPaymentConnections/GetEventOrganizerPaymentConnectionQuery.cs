// ABOUTME: Resolves the event organizer payment management envelope for a persisted event organizer.
// ABOUTME: Uses server-owned tenant, organizer, provider, and platform facts for the private read model.

using Explore.Application.Authorization;
using Explore.Application.DTOs.OrganizerPaymentConnections;
using MediatR;

namespace Explore.Application.Features.OrganizerPaymentConnections;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManagePaidEventCommerce)]
public sealed record GetEventOrganizerPaymentConnectionQuery(Guid EventId)
    : IRequest<EventOrganizerPaymentConnectionManagementDto?>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object> { ["eventId"] = EventId.ToString() };
}
