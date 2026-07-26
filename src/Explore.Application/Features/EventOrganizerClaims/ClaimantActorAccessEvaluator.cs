// ABOUTME: Verifies that the current user controls the actor used for an organizer claim.
// ABOUTME: Supports personal actors and organization/group actors through existing event-create authority.

using Explore.Application.Contracts.Persistence;
using Explore.Domain.Constants;

namespace Explore.Application.Features.EventOrganizerClaims;

internal static class ClaimantActorAccessEvaluator
{
    internal static async Task<bool> CanControlAsync(
        Guid actorId,
        Guid userId,
        IActorRepository actorRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IGroupMemberRepository groupMemberRepository,
        CancellationToken cancellationToken)
    {
        var actor = await actorRepository.GetActorWithDetails(actorId, cancellationToken);
        if (actor is null || actor.IsDeleted || actor.IsSuspended)
        {
            return false;
        }

        if (actor.UserId == userId)
        {
            return true;
        }

        if (actor.OrganizationId is { } organizationId)
        {
            return await organizationMemberRepository.HasPermissionInOrganization(
                organizationId,
                userId,
                PermissionCodes.EventCreate);
        }

        return actor.GroupId is { } groupId
            && await groupMemberRepository.HasPermissionInGroup(groupId, userId, PermissionCodes.EventCreate);
    }
}
