namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.OrganizationReview;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for OrganizationReviewDto (detail view).
/// Provides links for organization review operations.
/// </summary>
public sealed class OrganizationReviewDetailLinkPolicy : ILinkPolicy<OrganizationReviewDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(OrganizationReviewDto dto, ClaimsPrincipal? user)
    {
        // Self link
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetOrganizationReviewById,
            new { id = dto.Id },
            "GET",
            "Organization review");

        // Collection link
        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetOrganizationReviews,
            null,
            "GET",
            "All reviews");

        // Organization link
        yield return new LinkDefinition(
            "organization",
            RouteNames.GetOrganizationById,
            new { id = dto.OrganizationId },
            "GET",
            dto.OrganizationFullName);

        // Reviewer link
        yield return new LinkDefinition(
            "reviewer",
            RouteNames.GetUserById,
            new { id = dto.UserId },
            "GET",
            dto.UserFullName);

        // Edit link - requires authentication (review owner)
        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateOrganizationReview,
            new { id = dto.Id },
            "PUT",
            "Update review",
            RequiresAuth: true)
            .RequirePermission(
                PermissionAction.Update,
                dto,
                dto.Id.ToString(),
                new Dictionary<string, object>
                {
                    ["organizationId"] = dto.OrganizationId.ToString(),
                    ["userId"] = dto.UserId.ToString()
                });

        // Delete link - requires authentication (review owner or admin)
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteOrganizationReview,
            new { id = dto.Id },
            "DELETE",
            "Delete review",
            RequiresAuth: true)
            .RequirePermission(
                PermissionAction.Delete,
                dto,
                dto.Id.ToString(),
                new Dictionary<string, object>
                {
                    ["organizationId"] = dto.OrganizationId.ToString(),
                    ["userId"] = dto.UserId.ToString()
                });
    }
}

/// <summary>
/// Link policy for OrganizationReviewDto in collection context.
/// </summary>
public sealed class OrganizationReviewCollectionLinkPolicy : ICollectionLinkPolicy<OrganizationReviewDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(OrganizationReviewDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetOrganizationReviewById,
            new { id = dto.Id },
            "GET",
            "Organization review");

        // Organization link
        yield return new LinkDefinition(
            "organization",
            RouteNames.GetOrganizationById,
            new { id = dto.OrganizationId },
            "GET",
            dto.OrganizationFullName);
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires authentication
        yield return new LinkDefinition(
            "create",
            RouteNames.CreateOrganizationReview,
            null,
            "POST",
            "Create review",
            RequiresAuth: true)
            .RequirePermission(PermissionAction.Create, typeof(OrganizationReviewDto), "organization_review");
    }
}
