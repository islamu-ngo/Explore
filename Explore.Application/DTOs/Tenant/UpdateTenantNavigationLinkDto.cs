using System;

namespace Explore.Application.DTOs.Tenant;

/// <summary>
/// DTO for updating an existing tenant navigation link.
/// Used in PUT endpoints to accept navigation link update requests.
/// </summary>
public class UpdateTenantNavigationLinkDto
{
    /// <summary>
    /// Unique identifier for the navigation link to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display label for the navigation link.
    /// Required. Maximum 50 characters.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// URL or route that the navigation link points to.
    /// Required. Maximum 500 characters.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Optional icon identifier or CSS class for the navigation link.
    /// Can be null if no icon is desired.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Indicates whether the link should open in a new tab/window.
    /// </summary>
    public bool OpenInNewTab { get; set; }
}
