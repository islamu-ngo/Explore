// ABOUTME: Centralized upsert logic for SystemSetting records with audit trail.
// ABOUTME: Replaces copy-pasted UpsertSystemSettingAsync methods across 3+ services.

namespace Explore.Application.Settings;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
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
    private readonly ILocationPrivacyGovernanceMutationService? _locationPrivacyMutations;

    public SettingUpsertService(
        ISystemSettingRepository systemSettingRepository,
        IMediator mediator,
        ILocationPrivacyGovernanceMutationService? locationPrivacyMutations = null)
    {
        _systemSettingRepository = systemSettingRepository;
        _mediator = mediator;
        _locationPrivacyMutations = locationPrivacyMutations;
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
        Guid? actorId = null,
        CancellationToken cancellationToken = default)
    {
        string? oldValue = await PersistAsync(new SystemSetting
        {
            SettingKey = settingKey,
            Value = value,
            ValueType = valueType,
            IsLocked = isLocked,
            Description = description,
            Category = category,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = actorId
        }, actorId, cancellationToken);

        await _mediator.Publish(new SettingChangedNotification(
            settingKey, oldValue, value, SettingSource.SystemDefault, null, actorId, DateTime.UtcNow), CancellationToken.None);
    }

    /// <summary>
    /// Upserts a system setting with minimal parameters (uses existing metadata if updating).
    /// </summary>
    public async Task UpsertValueAsync(
        string settingKey,
        string value,
        Guid? actorId = null,
        CancellationToken cancellationToken = default)
        => await UpsertValueAsync(settingKey, value, isLocked: false, actorId, cancellationToken);

    /// <summary>
    /// Upserts a system setting with lock control. Pulls metadata from SettingRegistry (Guardrail 2).
    /// </summary>
    public async Task UpsertValueAsync(
        string settingKey,
        string value,
        bool isLocked,
        Guid? actorId,
        CancellationToken cancellationToken = default)
    {
        var definition = Domain.Settings.SettingRegistry.Get(settingKey);
        string? oldValue = await PersistAsync(new SystemSetting
        {
            SettingKey = settingKey,
            Value = value,
            ValueType = definition?.ValueType ?? SettingValueType.String,
            IsLocked = isLocked,
            Description = definition?.Description,
            Category = definition?.Category ?? "Unknown",
            DisplayOrder = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = actorId
        }, actorId, cancellationToken);

        await _mediator.Publish(new SettingChangedNotification(
            settingKey, oldValue, value, SettingSource.SystemDefault, null, actorId, DateTime.UtcNow), CancellationToken.None);
    }

    private async Task<string?> PersistAsync(
        SystemSetting setting,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        if (_locationPrivacyMutations?.Handles(setting.SettingKey) != true)
        {
            return await _systemSettingRepository.UpsertAsync(setting, cancellationToken);
        }

        LocationPrivacyGovernanceMutationResult mutation = await _locationPrivacyMutations.ExecuteAsync(
            setting.SettingKey,
            setting.Value,
            Domain.Settings.SettingScope.Instance,
            tenantId: null,
            actorId ?? Guid.Empty,
            token => _systemSettingRepository.UpsertAsync(setting, token),
            cancellationToken);
        if (!mutation.Accepted)
        {
            throw new InvalidOperationException(mutation.Error);
        }

        return mutation.PreviousStoredValue;
    }
}
