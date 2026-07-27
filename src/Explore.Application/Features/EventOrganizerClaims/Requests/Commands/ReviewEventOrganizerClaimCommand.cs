// ABOUTME: Curator-authorized CQRS request for reviewing an event organizer claim.
// ABOUTME: Approval changes claim and event authority together under one retryable transaction.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventOrganizerClaim;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventOrganizerClaims.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventOrganizerClaim, AuthorizationActions.Events.ReviewOrganizerClaim)]
public sealed class ReviewEventOrganizerClaimCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; init; }
    public Guid ClaimId { get; init; }
    public required ReviewEventOrganizerClaimDto Review { get; init; }
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["eventId"] = EventId.ToString(),
        ["claimId"] = ClaimId.ToString()
    };
}
