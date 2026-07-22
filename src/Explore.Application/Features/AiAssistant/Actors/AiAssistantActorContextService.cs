// ABOUTME: Resolves authorized acting actors for AI assistant use across rail, API, and MCP adapters.
// ABOUTME: Centralizes user, organization-member, and group-member actor eligibility before AI writes persist.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Ai;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;

namespace Explore.Application.Features.AiAssistant.Actors;

public sealed class AiAssistantActorContextService(
    IActorRepository actorRepository,
    ITenantUserRepository tenantUserRepository,
    IOrganizationMemberRepository organizationMemberRepository,
    IGroupMemberRepository groupMemberRepository)
    : IAiAssistantActorContextService
{
    public async Task<IReadOnlyList<AiAssistantActorContextDto>> ListAuthorizedActorContextsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || userId == Guid.Empty)
        {
            return [];
        }

        var contexts = new List<AiAssistantActorContextDto>();
        var seenActorIds = new HashSet<Guid>();

        var tenantUser = await tenantUserRepository.GetByTenantAndUserAsync(tenantId, userId, cancellationToken);
        var userActor = tenantUser?.ActorId is Guid tenantUserActorId
            ? await actorRepository.GetActorWithDetails(tenantUserActorId, cancellationToken)
            : await actorRepository.GetActorByUserIdAndTenantId(userId, tenantId, cancellationToken);

        AddActorContext(contexts, seenActorIds, userActor, nameof(ActorTypeEnum.User));

        var allowedOrganizationIds = (await organizationMemberRepository.GetOrganizationIdsWhereUserHasPermission(
            userId,
            PermissionCodes.EventCreate,
            cancellationToken)).ToHashSet();
        var organizationMemberships = await organizationMemberRepository.GetMembershipsByUser(userId, cancellationToken);
        foreach (var membership in organizationMemberships
                     .Where(membership => membership.TenantId == tenantId
                         && membership.Organization.ActorId is Guid
                         && allowedOrganizationIds.Contains(membership.OrganizationId))
                     .OrderBy(membership => membership.Organization.FullName, StringComparer.OrdinalIgnoreCase))
        {
            AddActorContext(
                contexts,
                seenActorIds,
                membership.Organization.ActorId!.Value,
                nameof(ActorTypeEnum.Organization),
                membership.Organization.FullName,
                membership.OrganizationId);
        }

        var allowedGroupIds = (await groupMemberRepository.GetGroupIdsWhereUserHasPermission(
            userId,
            PermissionCodes.EventCreate,
            cancellationToken)).ToHashSet();
        var groupMemberships = await groupMemberRepository.GetMembershipsByUser(userId, cancellationToken);
        foreach (var membership in groupMemberships
                     .Where(membership => membership.TenantId == tenantId
                         && membership.Group.ActorId is Guid
                         && allowedGroupIds.Contains(membership.GroupId))
                     .OrderBy(membership => membership.Group.FullName, StringComparer.OrdinalIgnoreCase))
        {
            AddActorContext(
                contexts,
                seenActorIds,
                membership.Group.ActorId!.Value,
                nameof(ActorTypeEnum.Group),
                membership.Group.FullName,
                membership.GroupId);
        }

        return contexts;
    }

    public async Task<AiAssistantActorContextResolution> ResolveAuthorizedActorAsync(
        Guid tenantId,
        Guid userId,
        Guid? requestedActorId,
        CancellationToken cancellationToken)
    {
        var contexts = await ListAuthorizedActorContextsAsync(tenantId, userId, cancellationToken);
        if (requestedActorId is null)
        {
            return AiAssistantActorContextResolution.Success(contexts.FirstOrDefault()?.ActorId, contexts);
        }

        if (requestedActorId == Guid.Empty)
        {
            return AiAssistantActorContextResolution.Failure(
                "invalid_actor_context",
                "AI acting actor id must be a non-empty actor identifier.",
                contexts);
        }

        return contexts.Any(context => context.ActorId == requestedActorId.Value)
            ? AiAssistantActorContextResolution.Success(requestedActorId, contexts)
            : AiAssistantActorContextResolution.Failure(
                "actor_context_not_authorized",
                "AI acting actor is not available to the authenticated user.",
                contexts);
    }

    private static void AddActorContext(
        ICollection<AiAssistantActorContextDto> contexts,
        ISet<Guid> seenActorIds,
        Actor? actor,
        string fallbackActorType)
    {
        if (actor is null)
        {
            return;
        }

        AddActorContext(contexts, seenActorIds, actor.Id, fallbackActorType, actor.DisplayName);
    }

    private static void AddActorContext(
        ICollection<AiAssistantActorContextDto> contexts,
        ISet<Guid> seenActorIds,
        Guid actorId,
        string actorType,
        string? actorDisplayName,
        Guid? scopeId = null)
    {
        if (actorId == Guid.Empty || !seenActorIds.Add(actorId))
        {
            return;
        }

        contexts.Add(new AiAssistantActorContextDto
        {
            ActorId = actorId,
            ScopeId = scopeId,
            ActorType = actorType,
            ActorDisplayName = string.IsNullOrWhiteSpace(actorDisplayName) ? actorType : actorDisplayName.Trim()
        });
    }
}
