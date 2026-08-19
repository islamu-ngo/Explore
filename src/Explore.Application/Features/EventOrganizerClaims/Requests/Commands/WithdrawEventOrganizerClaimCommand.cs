// ABOUTME: Authorized CQRS request for a claimant to withdraw an active organizer claim.
// ABOUTME: Carries optimistic concurrency while authorization resolves persisted claimant ownership.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventOrganizerClaims.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventOrganizerClaim, AuthorizationActions.Events.WithdrawOrganizerClaim)]
public sealed class WithdrawEventOrganizerClaimCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; init; }
    public Guid ClaimId { get; init; }
    public Guid ExpectedConcurrencyStamp { get; init; }
    string? ISecureRequest.ResourceId => ClaimId == Guid.Empty ? null : ClaimId.ToString();
}
