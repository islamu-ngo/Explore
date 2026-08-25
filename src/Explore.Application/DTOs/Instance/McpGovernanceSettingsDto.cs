// ABOUTME: Sub-resource DTO for API-hosted MCP runtime governance.
// ABOUTME: Exposes only runtime enablement and tenant lock controls, never startup endpoint path or stateless mode.

namespace Explore.Application.DTOs.Instance;

public sealed record McpGovernanceSettingsDto
{
    public bool Enabled { get; set; }
    public bool EnableLegacySse { get; set; }
    public bool LockTenantMcp { get; set; } = true;
    public bool LockTenantMcpLegacySse { get; set; } = true;
}
