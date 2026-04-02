namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.OrganizationMember;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for OrganizationMemberDto (detail view).
/// Provides links for organization membership operations.
/// </summary>
public sealed class OrganizationMemberDetailLinkPolicy : ILinkPolicy<OrganizationMemberDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(OrganizationMemberDto dto, ClaimsPrincipal? user)
    {
        // Self link
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetOrganizationMemberById,
            new { id = dto.Id },
            "GET",
            $"{dto.UserFullName} - {dto.RoleName}");

        // Organization link
        yield return new LinkDefinition(
            "organization",
            RouteNames.GetOrganizationById,
            new { id = dto.OrganizationId },
            "GET",
            dto.OrganizationFullName);

        // User link
        yield return new LinkDefinition(
            "user",
            RouteNames.GetUserById,
            new { id = dto.UserId },
            "GET",
            dto.UserFullName);

        // Edit link - requires authentication (organization admin)
        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateOrganizationMember,
            new { id = dto.Id },
            "PUT",
            "Update membership",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.OrganizationMember, dto);

        // Delete link - requires authentication (organization admin)
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteOrganizationMember,
            new { id = dto.Id },
            "DELETE",
            "Remove member",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Delete, ResourceDescriptors.OrganizationMember, dto);
    }
}

/// <summary>
/// Link policy for OrganizationMemberDto in collection context.
/// </summary>
public sealed class OrganizationMemberCollectionLinkPolicy : ICollectionLinkPolicy<OrganizationMemberDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(OrganizationMemberDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetOrganizationMemberById,
            new { id = dto.Id },
            "GET",
            $"{dto.UserFullName} - {dto.RoleName}");

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
            RouteNames.CreateOrganizationMember,
            null,
            "POST",
            "Add organization member",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Create, typeof(OrganizationMemberDto), "organization_member");
    }
}
