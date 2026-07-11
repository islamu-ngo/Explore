// ABOUTME: Enforces registry-owned eligibility for direct Control Plane tenant setting mutations.
// ABOUTME: Rejects unknown, out-of-scope, and sensitive settings before any repository access.

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

using Explore.Application.Responses;
using Explore.Domain.Settings;

internal static class ControlPlaneTenantSettingSecurity
{
    internal static BaseCommandResponse<Guid>? ValidateTarget(
        Guid tenantId,
        string key,
        out SettingDefinition definition)
    {
        SettingDefinition? registered = SettingRegistry.Get(key);
        if (registered is null)
        {
            definition = null!;
            return Failure(tenantId, "setting_not_found", "The tenant setting was not found.");
        }

        if (SettingScope.Tenant < registered.MinScope || SettingScope.Tenant > registered.MaxScope)
        {
            definition = null!;
            return Failure(
                tenantId,
                "setting_scope_not_supported",
                "The setting cannot be configured at tenant scope.");
        }

        if (registered.IsSensitive)
        {
            definition = null!;
            return Failure(
                tenantId,
                "sensitive_setting_not_supported",
                "Sensitive settings cannot be changed through this endpoint.");
        }

        definition = registered;
        return null;
    }

    internal static BaseCommandResponse<Guid> Failure(Guid tenantId, string code, string message) => new()
    {
        Id = tenantId,
        Success = false,
        FailureCode = code,
        Message = message
    };
}
