// ABOUTME: Centralized upsert logic for SystemSetting records with audit trail.
// ABOUTME: Replaces copy-pasted UpsertSystemSettingAsync methods across 3+ services.

namespace Explore.Application.Settings;

using Explore.Application.Contracts.Persistence;
using Explore.Domain;

/// <summary>
/// Centralized service for upserting SystemSetting records.
/// Handles create-or-update logic with proper audit fields.
/// </summary>
public class SettingUpsertService
{
    private readonly ISystemSettingRepository _systemSettingRepository;

    public SettingUpsertService(ISystemSettingRepository systemSettingRepository)
    {
        _systemSettingRepository = systemSettingRepository;
    }

    /// <summary>
    /// Creates or updates a system setting by key. Sets audit timestamps automatically.
    /// </summary>
    public async Task UpsertSystemSettingAsync(
        string settingKey,
        string value,
        SettingValueType valueType,
        bool isLocked,
        string category,
        int displayOrder,
        string description,
        Guid? actorId = null)
    {
        var existing = await _systemSettingRepository.GetByKey(settingKey);

        if (existing is null)
        {
            await _systemSettingRepository.Create(new SystemSetting
            {
                SettingKey = settingKey,
                Value = value,
                ValueType = valueType,
                IsLocked = isLocked,
                Description = description,
                Category = category,
                DisplayOrder = displayOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorId
            });
            return;
        }

        existing.Value = value;
        existing.ValueType = valueType;
        existing.IsLocked = isLocked;
        existing.Description = description;
        existing.Category = category;
        existing.DisplayOrder = displayOrder;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = actorId;

        await _systemSettingRepository.Update(existing);
    }

    /// <summary>
    /// Upserts a system setting with minimal parameters (uses existing metadata if updating).
    /// </summary>
    public async Task UpsertValueAsync(string settingKey, string value, Guid? actorId = null)
    {
        var existing = await _systemSettingRepository.GetByKey(settingKey);

        if (existing is null)
        {
            var definition = Domain.Settings.SettingRegistry.Get(settingKey);
            await _systemSettingRepository.Create(new SystemSetting
            {
                SettingKey = settingKey,
                Value = value,
                ValueType = definition?.ValueType ?? SettingValueType.String,
                IsLocked = false,
                Description = definition?.Description,
                Category = definition?.Category ?? "Unknown",
                DisplayOrder = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorId
            });
            return;
        }

        existing.Value = value;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = actorId;

        await _systemSettingRepository.Update(existing);
    }
}
