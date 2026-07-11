// ABOUTME: Preserves the current tenant slug across the Blazor Server circuit lifetime.
// ABOUTME: Keeps route context available for trusted API forwarding without making Blazor tenant-authoritative.

using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Explore.Blazor.Services;

public class TenantCircuitHandler : CircuitHandler
{
    private readonly ITenantRouteContextAccessor _tenantRouteContextAccessor;

    public TenantCircuitHandler(ITenantRouteContextAccessor tenantRouteContextAccessor)
    {
        _tenantRouteContextAccessor = tenantRouteContextAccessor;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_tenantRouteContextAccessor.TenantSlug))
        {
            _tenantRouteContextAccessor.SetTenantSlug(_tenantRouteContextAccessor.TenantSlug!);
        }

        return Task.CompletedTask;
    }
}
