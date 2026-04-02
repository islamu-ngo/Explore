namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.TenantSettings;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for TenantSettingsDto (detail view).
/// Provides links for tenant settings operations.
/// </summary>
public sealed class TenantSettingsDetailLinkPolicy : ILinkPolicy<TenantSettingsDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(TenantSettingsDto dto, ClaimsPrincipal? user)
    {
        // Self link
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetTenantSettingsById,
            new { id = dto.Id },
            "GET",
            "Tenant settings");

        // Tenant link
        yield return new LinkDefinition(
            "tenant",
            RouteNames.GetTenantById,
            new { id = dto.TenantId },
            "GET",
            "Parent tenant");

        // Edit link - requires authentication
        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateTenantSettings,
            new { id = dto.Id },
            "PUT",
            "Update settings",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.TenantSettings, dto);
    }
}

/// <summary>
/// Link policy for TenantSettingsListDto (collection items).
/// </summary>
public sealed class TenantSettingsCollectionLinkPolicy : ICollectionLinkPolicy<TenantSettingsListDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(TenantSettingsListDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetTenantSettingsById,
            new { id = dto.Id },
            "GET",
            "Tenant settings");

        // Tenant link
        yield return new LinkDefinition(
            "tenant",
            RouteNames.GetTenantById,
            new { id = dto.TenantId },
            "GET",
            "Parent tenant");
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // No create link - settings are created automatically with tenant
        yield break;
    }
}
