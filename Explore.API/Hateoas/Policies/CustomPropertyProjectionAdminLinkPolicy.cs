// ABOUTME: HATEOAS link policies for custom-property projection admin endpoints.
// ABOUTME: Provides discovery links between status, rebuild, drain, and dirty-scope inspection.

using System.Security.Claims;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

/// <summary>
/// Link policy for ProjectionStatusDto (detail view).
/// Provides operator-oriented links to rebuild and drain actions.
/// </summary>
public sealed class ProjectionStatusDetailLinkPolicy : ILinkPolicy<ProjectionStatusDto>
{
    public IEnumerable<LinkDefinition> GetLinks(ProjectionStatusDto dto, ClaimsPrincipal? user)
    {
        yield return LinkDefinition.Self(
            RouteNames.GetCustomPropertyProjectionStatus,
            new { tenantId = dto.TenantId });

        yield return new LinkDefinition(
            "rebuild",
            RouteNames.RebuildCustomPropertyProjection);

        yield return new LinkDefinition(
            "drain-dirty-scopes",
            RouteNames.DrainCustomPropertyProjectionDirtyScopes);

        yield return new LinkDefinition(
            "dirty-scopes",
            RouteNames.GetCustomPropertyProjectionDirtyScopes,
            new { tenantId = dto.TenantId, projectionName = dto.ProjectionName });
    }
}

/// <summary>
/// Link policy for RebuildProjectionResponseDto.
/// Provides navigation back to status after a rebuild.
/// </summary>
public sealed class RebuildProjectionResponseLinkPolicy : ILinkPolicy<RebuildProjectionResponseDto>
{
    public IEnumerable<LinkDefinition> GetLinks(RebuildProjectionResponseDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            "status",
            RouteNames.GetCustomPropertyProjectionStatus);
    }
}

/// <summary>
/// Link policy for DrainDirtyScopesResponseDto.
/// Provides navigation back to status and dirty-scope inspection.
/// </summary>
public sealed class DrainDirtyScopesResponseLinkPolicy : ILinkPolicy<DrainDirtyScopesResponseDto>
{
    public IEnumerable<LinkDefinition> GetLinks(DrainDirtyScopesResponseDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            "status",
            RouteNames.GetCustomPropertyProjectionStatus);
    }
}

/// <summary>
/// Link policy for ProjectionDirtyScopeDto collection items.
/// </summary>
public sealed class ProjectionDirtyScopeCollectionLinkPolicy : ICollectionLinkPolicy<ProjectionDirtyScopeDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(ProjectionDirtyScopeDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            "drain",
            RouteNames.DrainCustomPropertyProjectionDirtyScopes);
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            "drain-all",
            RouteNames.DrainCustomPropertyProjectionDirtyScopes);
    }
}
