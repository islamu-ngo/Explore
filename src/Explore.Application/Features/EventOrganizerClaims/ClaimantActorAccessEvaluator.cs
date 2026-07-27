// ABOUTME: Verifies that the current user controls the actor used for an organizer claim.
// ABOUTME: Supports personal actors and organization/group actors through existing event-create authority.

using Explore.Application.Contracts.Persistence;
using Explore.Domain.Constants;
using Explore.Domain.Enums;

namespace Explore.Application.Features.EventOrganizerClaims;

internal static class ClaimantActorAccessEvaluator
{
    internal static async Task<bool> CanControlAsync(
        Guid actorId,
        Guid userId,
        Guid tenantId,
        IActorRepository actorRepository,
        ITenantUserRepository tenantUserRepository,
        IOrganizationTenantRepository organizationTenantRepository,
        IGroupTenantRepository groupTenantRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IGroupMemberRepository groupMemberRepository,
        CancellationToken cancellationToken)
    {
        var actor = await actorRepository.GetActorWithDetails(actorId, cancellationToken);
        if (actor is null || !await IsEligibleAsync(
                actor,
                tenantId,
                tenantUserRepository,
                organizationTenantRepository,
                groupTenantRepository,
                cancellationToken))
        {
            return false;
        }

        return await CanControlOwnershipAsync(
            actor,
            userId,
            organizationMemberRepository,
            groupMemberRepository);
    }

    internal static async Task<bool> CanControlOwnershipAsync(
        Guid actorId,
        Guid userId,
        IActorRepository actorRepository,
        IOrganizationMemberRepository organizationMemberRepository,
        IGroupMemberRepository groupMemberRepository,
        CancellationToken cancellationToken)
    {
        var actor = await actorRepository.GetActorWithDetails(actorId, cancellationToken);
        return actor is not null && await CanControlOwnershipAsync(
            actor,
            userId,
            organizationMemberRepository,
            groupMemberRepository);
    }

    private static async Task<bool> CanControlOwnershipAsync(
        Explore.Domain.Actor actor,
        Guid userId,
        IOrganizationMemberRepository organizationMemberRepository,
        IGroupMemberRepository groupMemberRepository)
    {
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

    internal static async Task<bool> IsEligibleAsync(
        Guid actorId,
        Guid tenantId,
        IActorRepository actorRepository,
        ITenantUserRepository tenantUserRepository,
        IOrganizationTenantRepository organizationTenantRepository,
        IGroupTenantRepository groupTenantRepository,
        CancellationToken cancellationToken)
    {
        var actor = await actorRepository.GetActorWithDetails(actorId, cancellationToken);
        return actor is not null && await IsEligibleAsync(
            actor,
            tenantId,
            tenantUserRepository,
            organizationTenantRepository,
            groupTenantRepository,
            cancellationToken);
    }

    private static async Task<bool> IsEligibleAsync(
        Explore.Domain.Actor actor,
        Guid tenantId,
        ITenantUserRepository tenantUserRepository,
        IOrganizationTenantRepository organizationTenantRepository,
        IGroupTenantRepository groupTenantRepository,
        CancellationToken cancellationToken)
    {
        if (actor.IsDeleted || actor.IsSuspended)
        {
            return false;
        }

        if (actor.UserId is { } userId)
        {
            return await tenantUserRepository.IsActiveTenantUserAsync(tenantId, userId, cancellationToken);
        }

        if (actor.OrganizationId is { } organizationId)
        {
            var participation = await organizationTenantRepository.GetByOrganizationAndTenant(
                organizationId,
                tenantId,
                cancellationToken);
            return participation is
            {
                ApprovalStatusId: (int)ApprovalStatusEnum.Approved,
                IsOrganizerEligible: true,
                IsSuspended: false,
                IsDeleted: false
            };
        }

        if (actor.GroupId is not { } groupId)
        {
            return false;
        }

        var groupParticipation = await groupTenantRepository.GetByGroupAndTenant(
            groupId,
            tenantId,
            cancellationToken);
        return groupParticipation is
        {
            ApprovalStatusId: (int)ApprovalStatusEnum.Approved,
            IsOrganizerEligible: true,
            IsSuspended: false,
            IsDeleted: false
        };
    }
}
