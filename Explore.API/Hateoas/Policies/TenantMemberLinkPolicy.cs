// ABOUTME: HATEOAS link policies for tenant member detail and collection views.
// ABOUTME: Provides self, user, tenant, edit, and delete links with permission checks.

namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.TenantMember;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for TenantMemberDto (detail view).
/// Provides links for tenant member role assignment operations.
/// </summary>
public sealed class TenantMemberDetailLinkPolicy : ILinkPolicy<TenantMemberDto>
{
    public IEnumerable<LinkDefinition> GetLinks(TenantMemberDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetTenantMemberById,
            new { id = dto.Id },
            "GET",
            $"{dto.UserFullName} - {dto.RoleName}");

        yield return new LinkDefinition(
            "user",
            RouteNames.GetUserById,
            new { id = dto.UserId },
            "GET",
            dto.UserFullName);

        yield return new LinkDefinition(
            "tenant",
            RouteNames.GetTenantById,
            new { id = dto.TenantId },
            "GET",
            dto.TenantFullName);

        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateTenantMember,
            new { id = dto.Id },
            "PUT",
            "Update role assignment",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.TenantMember, dto);

        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteTenantMember,
            new { id = dto.Id },
            "DELETE",
            "Remove member from tenant",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Delete, ResourceDescriptors.TenantMember, dto);
    }
}

/// <summary>
/// Link policy for TenantMemberListDto (collection items).
/// </summary>
public sealed class TenantMemberCollectionLinkPolicy : ICollectionLinkPolicy<TenantMemberListDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(TenantMemberListDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetTenantMemberById,
            new { id = dto.Id },
            "GET",
            $"{dto.UserFullName} - {dto.RoleName}");

        yield return new LinkDefinition(
            "user",
            RouteNames.GetUserById,
            new { id = dto.UserId },
            "GET",
            dto.UserFullName);
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            "create",
            RouteNames.CreateTenantMember,
            null,
            "POST",
            "Add member to tenant",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Create, typeof(TenantMemberDto), "tenant_member");
    }
}
