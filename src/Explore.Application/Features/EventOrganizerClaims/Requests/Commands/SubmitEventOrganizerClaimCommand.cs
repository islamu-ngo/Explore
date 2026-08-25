// ABOUTME: Authorized CQRS request for an actor to claim organizer authority over an event.
// ABOUTME: Uses the organizer-claim resource while the handler verifies claimant-actor ownership.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventOrganizerClaim;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventOrganizerClaims.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventOrganizerClaim, AuthorizationActions.Events.ClaimOrganizer)]
public sealed record SubmitEventOrganizerClaimCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; init; }
    public required SubmitEventOrganizerClaimDto Claim { get; init; }
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();
    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        new EventScopedAuthorizationFacts(Guid.Empty, EventId);
}
