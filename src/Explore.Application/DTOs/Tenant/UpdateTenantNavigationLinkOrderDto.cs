using System;

namespace Explore.Application.DTOs.Tenant;

/// <summary>
/// DTO for updating the display order of a tenant navigation link.
/// Used in PATCH endpoints to reorder navigation links.
/// </summary>
public sealed record UpdateTenantNavigationLinkOrderDto
{
    /// <summary>
    /// Unique identifier for the navigation link.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// New display order for the navigation link.
    /// Lower values appear first in the navbar.
    /// </summary>
    public int Order { get; init; }
}
