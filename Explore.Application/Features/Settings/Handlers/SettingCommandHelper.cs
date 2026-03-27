// ABOUTME: Shared static helpers for setting command handlers — validation, serialization, scope mapping.
// ABOUTME: Extracted to avoid duplication across Update, BatchUpdate, Reset, Lock, and Unlock handlers.

namespace Explore.Application.Features.Settings.Handlers;

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Settings;

internal static class SettingCommandHelper
{
    /// <summary>
    /// Validates a plain-text value against its definition's ValueType and AllowedValues,
    /// then serializes to JSON storage format.
    /// </summary>
    internal static (bool IsValid, string? SerializedValue, string? Error) ValidateAndSerialize(
        string plainValue, SettingDefinition definition)
    {
        switch (definition.ValueType)
        {
            case SettingValueType.String:
                if (definition.AllowedValues is { Length: > 0 } &&
                    !definition.AllowedValues.Contains(plainValue, StringComparer.OrdinalIgnoreCase))
                {
                    return (false, null,
                        $"Value '{plainValue}' is not allowed. Allowed values: {string.Join(", ", definition.AllowedValues)}");
                }
                return (true, SettingValueSerializer.Serialize(plainValue), null);

            case SettingValueType.Integer:
                if (!int.TryParse(plainValue, out var intValue))
                    return (false, null, $"Value '{plainValue}' is not a valid integer.");
                return (true, SettingValueSerializer.Serialize(intValue), null);

            case SettingValueType.Boolean:
                if (!bool.TryParse(plainValue, out var boolValue))
                    return (false, null, $"Value '{plainValue}' is not a valid boolean.");
                return (true, SettingValueSerializer.Serialize(boolValue), null);

            case SettingValueType.Decimal:
                if (!decimal.TryParse(plainValue, System.Globalization.CultureInfo.InvariantCulture,
                    out var decValue))
                    return (false, null, $"Value '{plainValue}' is not a valid decimal.");
                return (true, SettingValueSerializer.Serialize(decValue), null);

            case SettingValueType.Json:
                return (true, plainValue, null);

            case SettingValueType.DateTime:
                if (!DateTime.TryParse(plainValue, System.Globalization.CultureInfo.InvariantCulture,
                    out var dtValue))
                    return (false, null, $"Value '{plainValue}' is not a valid datetime.");
                return (true, SettingValueSerializer.Serialize(dtValue), null);

            default:
                return (true, plainValue, null);
        }
    }

    /// <summary>
    /// Maps a SettingScope to the corresponding SettingSource for notification publishing.
    /// </summary>
    internal static SettingSource MapScopeToSource(SettingScope scope)
    {
        return scope switch
        {
            SettingScope.Instance => SettingSource.SystemDefault,
            SettingScope.Tenant => SettingSource.TenantOverride,
            SettingScope.Organization => SettingSource.OrganizationOverride,
            SettingScope.Group => SettingSource.GroupOverride,
            SettingScope.User => SettingSource.UserPreference,
            _ => SettingSource.SystemDefault
        };
    }

    /// <summary>
    /// Builds a SettingContext appropriate for the requested scope depth.
    /// </summary>
    internal static SettingContext BuildSettingContext(
        SettingScope scope, ITenantContext tenantContext, ICurrentUserService currentUserService)
    {
        return scope switch
        {
            SettingScope.Instance => new SettingContext(null, null, null, null),
            SettingScope.Tenant => new SettingContext(tenantContext.TenantId, null, null, null),
            SettingScope.User => new SettingContext(tenantContext.TenantId, null, null, currentUserService.UserId),
            _ => new SettingContext(tenantContext.TenantId, null, null, null)
        };
    }

    /// <summary>
    /// Determines scope entity ID and actor ID for the current operation.
    /// </summary>
    internal static (Guid ScopeId, Guid ActorId) GetScopeAndActorIds(
        SettingScope scope, ITenantContext tenantContext, ICurrentUserService currentUserService)
    {
        var userId = currentUserService.UserId ?? Guid.Empty;
        return scope switch
        {
            SettingScope.User => (userId, userId),
            SettingScope.Tenant => (tenantContext.TenantId, userId),
            SettingScope.Instance => (Guid.Empty, userId),
            _ => (Guid.Empty, userId)
        };
    }

    /// <summary>
    /// Checks if the current user has authorization to write settings at the given scope.
    /// </summary>
    internal static async Task<(bool Authorized, string? Error)> CheckAuthorizationAsync(
        SettingScope scope,
        IAdminContext adminContext,
        ITenantContext tenantContext,
        ICurrentUserService currentUserService,
        CancellationToken ct)
    {
        switch (scope)
        {
            case SettingScope.User:
                if (!currentUserService.IsAuthenticated)
                    return (false, "Authentication required to update user preferences.");
                return (true, null);

            case SettingScope.Tenant:
                var isTenantAdmin = await adminContext.IsTenantAdminAsync(tenantContext.TenantId, ct);
                if (!isTenantAdmin && !await adminContext.IsInstanceAdminAsync(ct))
                    return (false, "Only tenant or instance administrators can update tenant settings.");
                return (true, null);

            case SettingScope.Instance:
                if (!await adminContext.IsInstanceAdminAsync(ct))
                    return (false, "Only instance administrators can update instance settings.");
                return (true, null);

            default:
                return (false, $"Scope '{scope}' is not supported for setting operations.");
        }
    }

    /// <summary>
    /// Checks if a resolved setting is locked from a scope above the requested scope.
    /// Returns true if the setting can be edited (not locked from above).
    /// </summary>
    internal static (bool IsBlockedByLock, string? LockReason) CheckLockState(
        ResolvedSetting resolved, SettingScope requestedScope)
    {
        if (!resolved.IsLocked)
            return (false, null);

        var lockScope = resolved.Source switch
        {
            SettingSource.SystemLocked => SettingScope.Instance,
            SettingSource.TenantLocked => SettingScope.Tenant,
            _ => requestedScope
        };

        if (lockScope < requestedScope)
        {
            var lockSource = lockScope == SettingScope.Instance ? "instance" : "tenant";
            return (true, $"Locked by {lockSource} administrator");
        }

        return (false, null);
    }
}
