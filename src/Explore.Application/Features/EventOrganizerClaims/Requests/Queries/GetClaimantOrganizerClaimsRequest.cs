// ABOUTME: Authenticated query for organizer claims submitted through one claimant actor.
// ABOUTME: Handler verification prevents users from reading claims for actors they do not control.

using Explore.Application.DTOs.EventOrganizerClaim;
using MediatR;

namespace Explore.Application.Features.EventOrganizerClaims.Requests.Queries;

public sealed record GetClaimantOrganizerClaimsRequest(Guid ClaimantActorId)
    : IRequest<IReadOnlyList<EventOrganizerClaimDto>>;
