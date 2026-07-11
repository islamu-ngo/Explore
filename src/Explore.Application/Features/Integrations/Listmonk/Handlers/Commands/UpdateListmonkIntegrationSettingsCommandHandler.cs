// ABOUTME: Applies validated tenant Listmonk non-secret setting overrides.
// ABOUTME: Reuses the settings authorization and serialization helpers without exposing credentials.

using System.Globalization;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.Integrations;
using Explore.Application.DTOs.Integrations.Validators;
using Explore.Application.Features.Integrations.Listmonk.Requests.Commands;
using Explore.Application.Features.Settings.Handlers;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using MediatR;

namespace Explore.Application.Features.Integrations.Listmonk.Handlers.Commands;

public sealed class UpdateListmonkIntegrationSettingsCommandHandler(
    IHierarchicalSettingsResolver settingsResolver,
    IAdminContext adminContext,
    ITenantContext tenantContext,
    ICurrentUserService currentUserService,
    IPublisher publisher)
    : IRequestHandler<UpdateListmonkIntegrationSettingsCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateListmonkIntegrationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var validator = new UpdateListmonkIntegrationSettingsDtoValidator();
        var validation = await validator.ValidateAsync(request.Dto, cancellationToken);
        if (!validation.IsValid)
        {
            response.Success = false;
            response.Message = "Listmonk integration settings update failed.";
            response.Errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return response;
        }

        var authorization = await SettingCommandHelper.CheckAuthorizationAsync(
            SettingScope.Tenant,
            adminContext,
            tenantContext,
            currentUserService,
            cancellationToken);
        if (!authorization.Authorized)
        {
            response.Success = false;
            response.Message = authorization.Error;
            return response;
        }

        var actorId = await SettingCommandHelper.ResolveCurrentUserIdAsync(
            adminContext,
            currentUserService,
            cancellationToken);
        if (!actorId.HasValue)
        {
            response.Success = false;
            response.Message = "Authentication is required to update Listmonk integration settings.";
            return response;
        }

        var keys = Values(request.Dto).Keys.ToArray();
        var context = SettingCommandHelper.BuildSettingContext(SettingScope.Tenant, tenantContext, actorId.Value);
        var currentSettings = (await settingsResolver.ResolveBatchAsync(keys, context, cancellationToken))
            .ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, plainValue) in Values(request.Dto))
        {
            var definition = SettingRegistry.Get(key);
            if (definition is null)
            {
                response.Success = false;
                response.Message = "Listmonk integration settings update failed.";
                response.Errors = [$"Unknown setting '{key}'."];
                return response;
            }

            currentSettings.TryGetValue(key, out var current);
            var lockState = current is null
                ? (IsBlockedByLock: false, LockReason: (string?)null)
                : SettingCommandHelper.CheckLockState(current, SettingScope.Tenant);
            if (lockState.IsBlockedByLock)
            {
                response.Success = false;
                response.Message = "Listmonk integration settings update failed.";
                response.Errors = [lockState.LockReason ?? "Listmonk integration settings are locked."];
                return response;
            }

            var serialized = SettingCommandHelper.ValidateAndSerialize(plainValue, definition);
            if (!serialized.IsValid)
            {
                response.Success = false;
                response.Message = "Listmonk integration settings update failed.";
                response.Errors = [serialized.Error ?? $"Invalid value for '{key}'."];
                return response;
            }

            await settingsResolver.SetValueAsync(
                key,
                serialized.SerializedValue!,
                SettingScope.Tenant,
                tenantContext.TenantId,
                actorId.Value,
                cancellationToken);
            await publisher.Publish(
                new SettingChangedNotification(
                    key,
                    current?.Value,
                    serialized.SerializedValue,
                    SettingCommandHelper.MapScopeToSource(SettingScope.Tenant),
                    tenantContext.TenantId,
                    actorId.Value,
                    DateTime.UtcNow),
                cancellationToken);
        }

        response.Success = true;
        response.Id = tenantContext.TenantId;
        response.Message = "Listmonk integration settings updated successfully.";
        return response;
    }

    private static Dictionary<string, string> Values(UpdateListmonkIntegrationSettingsDto dto)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [GovernanceSettingKeys.Integrations.Listmonk.Enabled] = dto.Enabled.ToString(CultureInfo.InvariantCulture),
            [GovernanceSettingKeys.Integrations.Listmonk.InstanceUrl] = dto.InstanceUrl?.Trim() ?? string.Empty,
            [GovernanceSettingKeys.Integrations.Listmonk.DefaultListId] = dto.DefaultListId.ToString(CultureInfo.InvariantCulture),
            [GovernanceSettingKeys.Integrations.Listmonk.PreconfirmSubscriptions] = dto.PreconfirmSubscriptions.ToString(CultureInfo.InvariantCulture),
            [GovernanceSettingKeys.Integrations.Listmonk.SyncOnRegistration] = dto.SyncOnRegistration.ToString(CultureInfo.InvariantCulture)
        };
    }
}
