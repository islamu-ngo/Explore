// ABOUTME: Service contract for managing instance-level governance settings.
// ABOUTME: Handles deployment mode, module enablement, branding, and domain configuration.

using Explore.Application.DTOs.Onboarding;

namespace Explore.Application.Contracts.Services;

/// <summary>
/// Service for reading and applying instance-level governance settings.
/// </summary>
public interface IInstanceGovernanceSettingService
{
    /// <summary>
    /// Reads current instance governance settings from SystemSetting records.
    /// </summary>
    /// <returns>Current instance governance settings DTO.</returns>
    Task<InstanceGovernanceSettingsDto> ReadSettingsAsync();

    /// <summary>
    /// Applies instance governance settings and synchronizes tenant capabilities for default tenant.
    /// </summary>
    /// <param name="defaultTenantId">The default tenant ID for capability synchronization.</param>
    /// <param name="settings">The governance settings to apply.</param>
    /// <param name="actorUserId">The user ID performing the update (for audit trail).</param>
    Task ApplySettingsAsync(Guid defaultTenantId, InstanceGovernanceSettingsDto settings, Guid? actorUserId);
}
