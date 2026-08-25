// ABOUTME: DTO for dedicated Control Plane Admin Portal instance-level settings.
// ABOUTME: Carries enablement, public URL, and tenant-admin access flags through the governance API.

namespace Explore.Application.DTOs.Instance;

public sealed record AdminPortalSettingsDto
{
    public bool Enabled { get; set; } = true;
    public string PublicUrl { get; set; } = string.Empty;
    public bool AllowTenantAdminAccess { get; set; }
}
