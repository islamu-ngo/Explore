namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Tenant;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for TenantDto (detail view).
/// Provides links for tenant-related operations.
/// </summary>
public sealed class TenantDetailLinkPolicy : ILinkPolicy<TenantDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(TenantDto dto, ClaimsPrincipal? user)
    {
        // Self link
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetTenantById,
            new { id = dto.Id },
            "GET",
            dto.FullName);

        // Collection link
        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetTenants,
            null,
            "GET",
            "All tenants");

        // Users link
        yield return new LinkDefinition(
            "users",
            RouteNames.GetTenantMembers,
            new { tenantId = dto.Id },
            "GET",
            "Tenant users",
            RequiresAuth: true);
        // Edit link - requires authentication
        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateTenant,
            new { id = dto.Id },
            "PUT",
            "Update tenant",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.Tenant, dto);

        // Delete link - requires authentication
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteTenant,
            new { id = dto.Id },
            "DELETE",
            "Delete tenant",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Delete, ResourceDescriptors.Tenant, dto);
    }
}

/// <summary>
/// Link policy for TenantListDto (collection items).
/// </summary>
public sealed class TenantCollectionLinkPolicy : ICollectionLinkPolicy<TenantListDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(TenantListDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetTenantById,
            new { id = dto.Id },
            "GET",
            dto.FullName);

        // By slug link
        yield return new LinkDefinition(
            "by-slug",
            RouteNames.GetTenantBySlug,
            new { slug = dto.Slug },
            "GET",
            $"Tenant by slug: {dto.Slug}");
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires authentication
        yield return new LinkDefinition(
            "create",
            RouteNames.CreateTenant,
            null,
            "POST",
            "Create new tenant",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Create, typeof(TenantDto), "tenant");
    }
}
