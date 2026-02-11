// ABOUTME: Service contract for managing instance S3 storage configuration.
// ABOUTME: Handles S3-compatible object storage settings for the application.

using Explore.Application.DTOs.Onboarding;

namespace Explore.Application.Contracts.Services;

/// <summary>
/// Service for reading and applying instance S3 storage settings.
/// </summary>
public interface IInstanceStorageSettingService
{
    /// <summary>
    /// Reads current S3 storage settings from SystemSetting records.
    /// </summary>
    /// <returns>Current storage settings DTO.</returns>
    Task<InstanceStorageSettingsDto> ReadSettingsAsync();

    /// <summary>
    /// Applies S3 storage settings to SystemSetting records.
    /// </summary>
    /// <param name="settings">The storage settings to apply.</param>
    Task ApplySettingsAsync(InstanceStorageSettingsDto settings);
}
