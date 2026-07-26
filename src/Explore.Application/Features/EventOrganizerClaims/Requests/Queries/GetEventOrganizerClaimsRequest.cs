// ABOUTME: Curator-authorized query for organizer claims attached to one event.
// ABOUTME: Returns claim evidence and normalized status only through an event-scoped authorization check.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventOrganizerClaim;
using MediatR;

namespace Explore.Application.Features.EventOrganizerClaims.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewOrganizerClaims)]
public sealed record GetEventOrganizerClaimsRequest(Guid EventId)
    : IRequest<IReadOnlyList<EventOrganizerClaimDto>>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();
}
