// ABOUTME: Stores the current tenant slug for Blazor routing and trusted forwarding only.
// ABOUTME: This is not tenant authority; it preserves route context across the request and circuit lifetime.

namespace Explore.Blazor.Services;

public interface ITenantRouteContextAccessor
{
    string? TenantSlug { get; }

    void SetTenantSlug(string slug);

    void Clear();

    IDisposable BeginActivityScope();
}
