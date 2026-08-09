// ABOUTME: Resolves persisted organizer-claim ownership attributes for claim withdrawal authorization.
// ABOUTME: Keeps claimant context lookup read-only so withdrawal mutation stays inside the handler.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventOrganizerClaims.Requests.Commands;

namespace Explore.Application.Features.EventOrganizerClaims.Authorization;

public sealed class WithdrawEventOrganizerClaimAuthorizationContextEnricher(
    IEventOrganizerClaimRepository claimRepository,
    IActorRepository actorRepository,
    ITenantContext? tenantContext = null)
    : IAuthorizationContextEnricher<WithdrawEventOrganizerClaimCommand>
{
    public async Task<AuthorizationContext> ResolveAsync(
        WithdrawEventOrganizerClaimCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ClaimId == Guid.Empty || tenantContext is null)
        {
            throw new AuthorizationException(ResourceKinds.EventOrganizerClaim, AuthorizationActions.Events.WithdrawOrganizerClaim);
        }

        var claim = await claimRepository.GetDetailsAsync(request.ClaimId, trackChanges: false, cancellationToken);
        if (claim is null || claim.TenantId != tenantContext.TenantId)
        {
            throw new AuthorizationException(ResourceKinds.EventOrganizerClaim, AuthorizationActions.Events.WithdrawOrganizerClaim);
        }

        var claimantActor = await actorRepository.GetActorWithDetails(claim.ClaimantActorId, cancellationToken);
        if (claimantActor is null || claimantActor.Id != claim.ClaimantActorId)
        {
            throw new AuthorizationException(ResourceKinds.EventOrganizerClaim, AuthorizationActions.Events.WithdrawOrganizerClaim);
        }

        var attributes = new Dictionary<string, object>
        {
            ["tenantId"] = claim.TenantId.ToString("D"),
            ["eventId"] = claim.EventId.ToString("D"),
            ["claimId"] = claim.Id.ToString("D"),
            ["claimantActorId"] = claim.ClaimantActorId.ToString("D"),
            ["status"] = claim.Status?.MasterCode ?? claim.StatusId.ToString()
        };

        AddIfPresent(attributes, "claimantUserId", claimantActor.UserId);
        AddIfPresent(attributes, "claimantOrganizationId", claimantActor.OrganizationId);
        AddIfPresent(attributes, "claimantGroupId", claimantActor.GroupId);

        return new AuthorizationContext(claim.Id.ToString(), attributes);
    }

    private static void AddIfPresent(IDictionary<string, object> attributes, string key, Guid? value)
    {
        if (value.HasValue && value.Value != Guid.Empty)
        {
            attributes[key] = value.Value.ToString("D");
        }
    }
}
