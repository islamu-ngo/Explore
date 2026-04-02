namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Group;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for GroupDto (detail view).
/// Determines which links should be included based on resource state and user authorization.
/// </summary>
public sealed class GroupDetailLinkPolicy : ILinkPolicy<GroupDto>
{
    public IEnumerable<LinkDefinition> GetLinks(GroupDto dto, ClaimsPrincipal? user)
    {
        yield return LinkDefinition.Self(
            RouteNames.GetGroupById,
            new { id = dto.Id });

        yield return LinkDefinition.Collection(RouteNames.GetGroups);

        yield return LinkDefinition.Related(
            LinkRelations.Members,
            RouteNames.GetGroupMembers,
            new { groupId = dto.Id });

        if (dto.ActorId.HasValue)
        {
            yield return LinkDefinition.Related(
                LinkRelations.Actor,
                RouteNames.GetActorById,
                new { id = dto.ActorId.Value });
        }

        yield return LinkDefinition.Edit(
            RouteNames.UpdateGroup,
            new { id = dto.Id })
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.Group, dto);

        yield return LinkDefinition.Delete(
            RouteNames.DeleteGroup,
            new { id = dto.Id })
            .RequirePermission(AuthorizationActions.Delete, ResourceDescriptors.Group, dto);
    }
}

/// <summary>
/// Link policy for GroupListDto (collection items).
/// </summary>
public sealed class GroupCollectionLinkPolicy : ICollectionLinkPolicy<GroupListDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(GroupListDto dto, ClaimsPrincipal? user)
    {
        yield return LinkDefinition.Self(
            RouteNames.GetGroupById,
            new { id = dto.Id });

        yield return LinkDefinition.Related(
            LinkRelations.Members,
            RouteNames.GetGroupMembers,
            new { groupId = dto.Id });
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield return LinkDefinition.Create(RouteNames.CreateGroup)
            .RequirePermission(AuthorizationActions.Create, typeof(GroupDto), "group");
    }
}
