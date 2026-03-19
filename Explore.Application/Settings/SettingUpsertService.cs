// ABOUTME: Centralized upsert logic for SystemSetting records with audit trail.
// ABOUTME: Replaces copy-pasted UpsertSystemSettingAsync methods across 3+ services.

namespace Explore.Application.Settings;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Notifications;
using Explore.Domain;
using MediatR;

/// <summary>
/// Centralized service for upserting SystemSetting records.
/// Handles create-or-update logic with proper audit fields.
/// </summary>
public class SettingUpsertService
{
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IMediator _mediator;

    public SettingUpsertService(ISystemSettingRepository systemSettingRepository, IMediator mediator)
    {
        _systemSettingRepository = systemSettingRepository;
        _mediator = mediator;
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

        var oldValue = existing?.Value;

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
        }
        else
        {
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

        // Fire-and-forget: audit notification should not block the write path
        _ = _mediator.Publish(new SettingChangedNotification(
            settingKey, oldValue, value, SettingSource.SystemDefault, null, actorId, DateTime.UtcNow));
    }

    /// <summary>
    /// Upserts a system setting with minimal parameters (uses existing metadata if updating).
    /// </summary>
    public async Task UpsertValueAsync(string settingKey, string value, Guid? actorId = null)
        => await UpsertValueAsync(settingKey, value, isLocked: false, actorId);

    /// <summary>
    /// Upserts a system setting with lock control. Pulls metadata from SettingRegistry (Guardrail 2).
    /// </summary>
    public async Task UpsertValueAsync(string settingKey, string value, bool isLocked, Guid? actorId)
    {
        var existing = await _systemSettingRepository.GetByKey(settingKey);
        var oldValue = existing?.Value;
        var definition = Domain.Settings.SettingRegistry.Get(settingKey);

        if (existing is null)
        {
            await _systemSettingRepository.Create(new SystemSetting
            {
                SettingKey = settingKey,
                Value = value,
                ValueType = definition?.ValueType ?? SettingValueType.String,
                IsLocked = isLocked,
                Description = definition?.Description,
                Category = definition?.Category ?? "Unknown",
                DisplayOrder = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actorId
            });
        }
        else
        {
            existing.Value = value;
            existing.IsLocked = isLocked;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = actorId;

            await _systemSettingRepository.Update(existing);
        }

        _ = _mediator.Publish(new SettingChangedNotification(
            settingKey, oldValue, value, SettingSource.SystemDefault, null, actorId, DateTime.UtcNow));
    }
}
