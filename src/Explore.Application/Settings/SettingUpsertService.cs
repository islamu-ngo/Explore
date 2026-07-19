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
        SettingPersistenceResult persistence = await PersistAsync(new SystemSetting
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

        await InvalidateCommittedMutationAsync(persistence.Mutation);
        await _mediator.Publish(new SettingChangedNotification(
            settingKey, persistence.PreviousStoredValue, value, SettingSource.SystemDefault, null, actorId, DateTime.UtcNow), CancellationToken.None);
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
        => _ = await UpsertValueCoreAsync(
            settingKey,
            value,
            isLocked,
            actorId,
            invalidateAfterCommit: true,
            cancellationToken);

    internal Task<DeferredSettingUpsertResult> UpsertValueWithDeferredInvalidationAsync(
        string settingKey,
        string value,
        Guid? actorId,
        CancellationToken cancellationToken = default) =>
        UpsertValueCoreAsync(
            settingKey,
            value,
            isLocked: false,
            actorId,
            invalidateAfterCommit: false,
            cancellationToken);

    private async Task<DeferredSettingUpsertResult> UpsertValueCoreAsync(
        string settingKey,
        string value,
        bool isLocked,
        Guid? actorId,
        bool invalidateAfterCommit,
        CancellationToken cancellationToken)
    {
        var definition = Domain.Settings.SettingRegistry.Get(settingKey);
        SettingPersistenceResult persistence = await PersistAsync(new SystemSetting
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

        var notification = new SettingChangedNotification(
            settingKey,
            persistence.PreviousStoredValue,
            value,
            SettingSource.SystemDefault,
            null,
            actorId,
            DateTime.UtcNow);

        if (invalidateAfterCommit)
        {
            await InvalidateCommittedMutationAsync(persistence.Mutation);
            await _mediator.Publish(notification, CancellationToken.None);
        }

        return new(notification, persistence.Mutation);
    }

    private async Task<SettingPersistenceResult> PersistAsync(
        SystemSetting setting,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        if (_locationPrivacyMutations?.Handles(setting.SettingKey) != true)
        {
            string? previousStoredValue = await _systemSettingRepository.UpsertAsync(setting, cancellationToken);
            return new(previousStoredValue, null);
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

        return new(mutation.PreviousStoredValue, mutation);
    }

    private async Task InvalidateCommittedMutationAsync(
        LocationPrivacyGovernanceMutationResult? mutation)
    {
        if (mutation is not { Accepted: true } || _locationPrivacyMutations is null)
        {
            return;
        }

        await _locationPrivacyMutations.InvalidateMutationAsync(
            Domain.Settings.SettingScope.Instance,
            tenantId: null,
            mutation.CorrectedProjections,
            CancellationToken.None);
    }

    private sealed record SettingPersistenceResult(
        string? PreviousStoredValue,
        LocationPrivacyGovernanceMutationResult? Mutation);
}

internal sealed record DeferredSettingUpsertResult(
    SettingChangedNotification Notification,
    LocationPrivacyGovernanceMutationResult? Mutation);
