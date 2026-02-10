namespace Explore.Application.DTOs.Tenant;

/// <summary>
/// DTO for creating a new tenant navigation link.
/// Used in POST endpoints to accept navigation link creation requests.
/// </summary>
public class CreateTenantNavigationLinkDto
{
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
    /// Defaults to false.
    /// </summary>
    public bool OpenInNewTab { get; set; }
}
