namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.TenantUser;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for TenantUserDto (detail view).
/// Provides links for tenant user role assignment operations.
/// </summary>
public sealed class TenantUserDetailLinkPolicy : ILinkPolicy<TenantUserDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(TenantUserDto dto, ClaimsPrincipal? user)
    {
        // Self link
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetTenantUserById,
            new { id = dto.Id },
            "GET",
            $"{dto.UserFullName} - {dto.UserRoleName}");

        // User link
        yield return new LinkDefinition(
            "user",
            RouteNames.GetUserById,
            new { id = dto.UserId },
            "GET",
            dto.UserFullName);

        // Tenant link
        yield return new LinkDefinition(
            "tenant",
            RouteNames.GetTenantById,
            new { id = dto.TenantId },
            "GET",
            dto.TenantFullName);

        // Edit link - requires authentication
        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateTenantUser,
            new { id = dto.Id },
            "PUT",
            "Update role assignment",
            RequiresAuth: true);

        // Delete link - requires authentication
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteTenantUser,
            new { id = dto.Id },
            "DELETE",
            "Remove user from tenant",
            RequiresAuth: true);
    }
}

/// <summary>
/// Link policy for TenantUserListDto (collection items).
/// </summary>
public sealed class TenantUserCollectionLinkPolicy : ICollectionLinkPolicy<TenantUserListDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(TenantUserListDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetTenantUserById,
            new { id = dto.Id },
            "GET",
            $"{dto.UserFullName} - {dto.UserRoleName}");

        // User link
        yield return new LinkDefinition(
            "user",
            RouteNames.GetUserById,
            new { id = dto.UserId },
            "GET",
            dto.UserFullName);
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires authentication
        yield return new LinkDefinition(
            "create",
            RouteNames.CreateTenantUser,
            null,
            "POST",
            "Add user to tenant",
            RequiresAuth: true);
    }
}
