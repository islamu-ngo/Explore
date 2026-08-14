// ABOUTME: Cerbos configuration POCO resolved from the cascading settings engine.
// Supports instance PDP (default) and Bring Your Own Cerbos PDP per tenant.

namespace Explore.Application.Models;

/// <summary>
/// Cerbos PDP connection parameters resolved from SystemSetting/TenantSetting.
/// Instance admin can lock settings (IsLocked) to enforce a single Cerbos PDP,
/// or leave unlocked so tenants can bring their own Cerbos endpoint.
/// </summary>
public class CerbosConfiguration
{
    /// <summary>Cerbos PDP gRPC endpoint URL (e.g., "https://cerbosgrpc.example.com:443" or "http://localhost:3593").</summary>
    public required string Endpoint { get; set; }

    /// <summary>Whether this tenant uses the instance PDP or a custom BYO endpoint.</summary>
    public CerbosMode Mode { get; set; } = CerbosMode.Instance;

    /// <summary>Parsed legacy outage behavior. Runtime BYO PDP failures always activate safe mode.</summary>
    public CerbosFailureMode FailureMode { get; set; } = CerbosFailureMode.Closed;

    /// <summary>Optional Admin API endpoint for policy management (BYO tenants).</summary>
    public string? AdminEndpoint { get; set; }

    /// <summary>Admin API username for basic auth (BYO tenants).</summary>
    public string? AdminUsername { get; set; }

    /// <summary>Admin API password for basic auth (BYO tenants).</summary>
    public string? AdminPassword { get; set; }

    /// <summary>Whether this config points to the instance's default PDP (not a BYO override).</summary>
    public bool IsInstanceDefault { get; set; }
}

/// <summary>
/// Determines which Cerbos PDP a tenant uses.
/// Stored as string in TenantSetting (e.g., "instance", "custom_endpoint").
/// </summary>
public enum CerbosMode
{
    /// <summary>Use the instance's Cerbos PDP with scope-based tenant isolation (default).</summary>
    Instance = 0,

    /// <summary>Tenant brings their own Cerbos PDP endpoint.</summary>
    CustomEndpoint = 1
}

/// <summary>
/// Legacy behavior value when a tenant's Cerbos PDP is unreachable.
/// Stored as string in TenantSetting (e.g., "closed", "open"); runtime failures always close.
/// </summary>
public enum CerbosFailureMode
{
    /// <summary>
    /// Safe-Mode: only instance admin emergency access allowed, deny everything else.
    /// Use when tenant policies might be stricter than the fallback — prevents security bypass.
    /// </summary>
    Closed = 0,

    /// <summary>
    /// Deprecated. Parsed for existing configuration values but ignored at runtime.
    /// </summary>
    Open = 1
}
