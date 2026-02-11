// ABOUTME: Service contract for managing tenant policy settings with instance-level delegation constraints.
// ABOUTME: Resolves tenant overrides against instance defaults for onboarding and runtime configuration.

using Explore.Application.DTOs.Onboarding;

namespace Explore.Application.Contracts.Services;

/// <summary>
/// Service for reading and applying tenant policy settings with instance-level governance constraints.
/// </summary>
public interface ITenantPolicySettingService
{
    /// <summary>
    /// Reads effective tenant policy settings by merging tenant overrides with instance defaults.
    /// </summary>
    /// <param name="tenantId">The tenant ID to read settings for.</param>
    /// <returns>Effective tenant policy settings DTO with delegation capabilities.</returns>
    Task<TenantPolicySettingsDto> ReadEffectiveTenantSettingsAsync(Guid tenantId);

    /// <summary>
    /// Applies tenant policy setting overrides while enforcing instance-level delegation constraints.
    /// </summary>
    /// <param name="tenantId">The tenant ID to apply settings for.</param>
    /// <param name="actorUserId">The user ID performing the update (for audit trail).</param>
    /// <param name="settings">The tenant policy settings to apply.</param>
    Task ApplyTenantSettingsAsync(Guid tenantId, Guid? actorUserId, TenantPolicySettingsDto settings);
}
