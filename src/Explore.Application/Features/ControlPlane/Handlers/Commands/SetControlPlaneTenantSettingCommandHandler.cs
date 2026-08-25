// ABOUTME: Validates and writes a tenant-scoped setting override via the Control Plane write surface.
// ABOUTME: Enforces registry, sensitivity, system-lock, and typed-value constraints before persistence.
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Features.Settings.Handlers;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Settings;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class SetControlPlaneTenantSettingCommandHandler(
    ITenantSettingRepository repository,
    ISystemSettingRepository systemSettingRepository,
    ISettingMutationLock mutationLock,
    ICurrentUserService currentUserService,
    IHierarchicalSettingsResolver settingsResolver,
    IMediator mediator)
    : IRequestHandler<SetControlPlaneTenantSettingCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        SetControlPlaneTenantSettingCommand request,
        CancellationToken cancellationToken)
    {
        Guid? actorUserId = currentUserService.UserId;
        if (actorUserId is null)
        {
            return ControlPlaneTenantSettingSecurity.Failure(
                request.TenantId,
                "authenticated_operator_required",
                "Authenticated operator context is required.");
        }

        BaseCommandResponse<Guid>? invalidTarget = ControlPlaneTenantSettingSecurity.ValidateTarget(
            request.TenantId,
            request.Key,
            out SettingDefinition definition);
        if (invalidTarget is not null)
        {
            return invalidTarget;
        }

        (bool isValid, string? serializedValue, _) =
            SettingCommandHelper.ValidateAndSerialize(request.Value, definition);
        if (!isValid)
        {
            return ControlPlaneTenantSettingSecurity.Failure(
                request.TenantId,
                "setting_validation_failed",
                "The tenant setting value is invalid.");
        }

        (BaseCommandResponse<Guid> Response, SettingChangedNotification? Notification) outcome =
            await mutationLock.ExecuteAsync<(BaseCommandResponse<Guid>, SettingChangedNotification?)>(
            request.Key,
            async token =>
            {
                if (await systemSettingRepository.IsLocked(request.Key, token))
                {
                    return (ControlPlaneTenantSettingSecurity.Failure(
                        request.TenantId, "setting_system_locked", "The setting is locked at system scope."), null);
                }

                TenantSetting? existing = await repository.GetByTenantAndKey(
                    request.TenantId, request.Key, token);
                await repository.SetValueAsync(
                    request.TenantId,
                    request.Key,
                    serializedValue!,
                    token,
                    actorUserId);
                BaseCommandResponse<Guid> response = BaseCommandResponse.Success(
                    request.TenantId,
                    "Tenant setting updated.");
                var notification = new SettingChangedNotification(
                    request.Key,
                    existing?.Value,
                    serializedValue,
                    SettingSource.TenantOverride,
                    request.TenantId,
                    actorUserId,
                    DateTime.UtcNow);
                return (response, notification);
            },
            cancellationToken);

        if (outcome.Notification is not null)
        {
            settingsResolver.InvalidateCache(SettingScope.Tenant, request.TenantId);
            await mediator.Publish(outcome.Notification, cancellationToken);
        }

        return outcome.Response;
    }
}
