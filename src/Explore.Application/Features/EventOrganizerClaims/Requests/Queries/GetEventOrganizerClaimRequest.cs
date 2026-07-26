// ABOUTME: Curator-authorized CQRS query for one organizer claim under its parent event.
// ABOUTME: Keeps claim evidence behind the same event-scoped review authorization boundary.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventOrganizerClaim;
using MediatR;

namespace Explore.Application.Features.EventOrganizerClaims.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewOrganizerClaims)]
public sealed record GetEventOrganizerClaimRequest(Guid EventId, Guid ClaimId)
    : IRequest<EventOrganizerClaimDto?>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();
}
