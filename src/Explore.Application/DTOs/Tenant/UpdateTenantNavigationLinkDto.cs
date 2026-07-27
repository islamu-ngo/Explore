using System;

// ABOUTME: Wrapper DTO for partial tenant navigation-link updates using nullable property groups.
// ABOUTME: Route and tenant context own identity; reorder remains a separate action.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.Tenant;

/// <summary>
/// DTO for partially updating an existing tenant navigation link.
/// </summary>
public class UpdateTenantNavigationLinkDto
{
    public UpdateTenantNavigationLinkLabelDto? Label { get; set; }
    public UpdateTenantNavigationLinkUrlDto? Url { get; set; }
    public UpdateTenantNavigationLinkIconDto? Icon { get; set; }
    public UpdateTenantNavigationLinkOpenInNewTabDto? OpenInNewTab { get; set; }
}

public class UpdateTenantNavigationLinkLabelDto
{
    public required string Value { get; set; }
}

public class UpdateTenantNavigationLinkUrlDto
{
    public required string Value { get; set; }
}

public class UpdateTenantNavigationLinkIconDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateTenantNavigationLinkOpenInNewTabDto
{
    public bool? Value { get; set; }
}
