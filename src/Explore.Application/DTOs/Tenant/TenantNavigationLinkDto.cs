using System;

namespace Explore.Application.DTOs.Tenant;

/// <summary>
/// DTO for reading tenant navigation link details.
/// Used in GET endpoints to return full navigation link information.
/// </summary>
public class TenantNavigationLinkDto
{
    /// <summary>
    /// Unique identifier for this navigation link.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display label for the navigation link.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// URL or route that the navigation link points to.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional icon identifier or CSS class for the navigation link.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Display order of the navigation link.
    /// Lower values appear first in the navbar.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Indicates whether the link should open in a new tab/window.
    /// </summary>
    public bool OpenInNewTab { get; set; }
}
