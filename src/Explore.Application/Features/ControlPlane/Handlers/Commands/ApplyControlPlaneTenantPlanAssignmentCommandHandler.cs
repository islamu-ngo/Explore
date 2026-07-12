// ABOUTME: Applies a tenant plan assignment by copying version settings into tenant overrides.
// ABOUTME: Performs lock and quota preflight before transactional tenant-setting upserts.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Settings;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class ApplyControlPlaneTenantPlanAssignmentCommandHandler(
    ITenantPlanRepository tenantPlanRepository,
    ITenantSettingRepository tenantSettingRepository,
    ISystemSettingRepository systemSettingRepository,
    TenantPlanStorageQuotaCeilingPolicy storageQuotaCeilingPolicy,
    IUnitOfWork unitOfWork,
    ISettingMutationLock mutationLock,
    IHierarchicalSettingsResolver settingsResolver,
    IMediator mediator)
    : IRequestHandler<ApplyControlPlaneTenantPlanAssignmentCommand, BaseCommandResponse<Guid>>
{
    public async Task<BaseCommandResponse<Guid>> Handle(
        ApplyControlPlaneTenantPlanAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        TenantPlanAssignment? assignment = await tenantPlanRepository.GetAssignmentAsync(
            request.AssignmentId,
            cancellationToken);

        if (assignment is null)
        {
            return Failure(request.AssignmentId, "tenant_plan_assignment_not_found");
        }

        if (assignment.TenantId != request.TenantId)
        {
            return Failure(request.AssignmentId, "tenant_plan_assignment_tenant_mismatch");
        }

        if (assignment.TenantPlanAssignmentStatusId != (int)TenantPlanAssignmentStatusEnum.Active)
        {
            return Failure(request.AssignmentId, "tenant_plan_assignment_not_active");
        }

        TenantPlanVersion version = assignment.TenantPlanVersion;
        TenantSettingOverrideUpsert[] upserts = version.Settings
            .Select(setting => new TenantSettingOverrideUpsert(setting.SettingKey, setting.JsonValue, setting.IsLocked))
            .ToArray();
        string[] settingKeys = upserts
            .Select(upsert => upsert.SettingKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string[] mutationKeys = version.Quotas.Any(quota => quota.QuotaKey == TenantPlanQuotaKeys.StorageBytes)
            ? [.. settingKeys, GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes]
            : settingKeys;

        if (mutationKeys.Length == 0)
        {
            return new BaseCommandResponse<Guid>
            {
                Id = request.AssignmentId,
                Success = true,
                Message = "Tenant plan applied."
            };
        }

        (BaseCommandResponse<Guid> Response, IReadOnlyList<SettingChangedNotification> Notifications) outcome =
            await unitOfWork.ExecuteInTransactionAsync(
            token => mutationLock.ExecuteManyAsync(
                mutationKeys,
                async innerToken =>
                {
                    string? quotaError = await storageQuotaCeilingPolicy.ValidateAsync(
                        version.Quotas,
                        innerToken);
                    if (quotaError is not null)
                    {
                        return (Response: Failure(request.AssignmentId, quotaError), Notifications: (IReadOnlyList<SettingChangedNotification>)[]);
                    }

                    foreach (string settingKey in settingKeys)
                    {
                        if (await systemSettingRepository.IsLocked(settingKey, innerToken))
                        {
                            return (
                                Response: Failure(request.AssignmentId, "tenant_plan_setting_locked"),
                                Notifications: (IReadOnlyList<SettingChangedNotification>)[]);
                        }
                    }

                    var notifications = new List<SettingChangedNotification>(upserts.Length);
                    foreach (TenantSettingOverrideUpsert upsert in upserts)
                    {
                        TenantSetting? existing = await tenantSettingRepository.GetByTenantAndKey(
                            request.TenantId,
                            upsert.SettingKey,
                            innerToken);
                        notifications.Add(new SettingChangedNotification(
                            upsert.SettingKey,
                            existing?.Value,
                            upsert.Value,
                            upsert.IsLocked ? SettingSource.TenantLocked : SettingSource.TenantOverride,
                            request.TenantId,
                            request.AppliedByUserId,
                            DateTime.UtcNow));
                    }

                    await tenantSettingRepository.UpsertManyForTenantAsync(
                        request.TenantId,
                        upserts,
                        request.AppliedByUserId,
                        innerToken);
                    return (
                        Response: new BaseCommandResponse<Guid>
                        {
                            Id = request.AssignmentId,
                            Success = true,
                            Message = "Tenant plan applied."
                        },
                        Notifications: (IReadOnlyList<SettingChangedNotification>)notifications);
                },
                token),
            cancellationToken);

        if (outcome.Notifications.Count > 0)
        {
            settingsResolver.InvalidateCache(SettingScope.Tenant, request.TenantId);
            foreach (SettingChangedNotification notification in outcome.Notifications)
            {
                await mediator.Publish(notification, cancellationToken);
            }
        }

        return outcome.Response;
    }

    private static BaseCommandResponse<Guid> Failure(Guid assignmentId, string error) => new()
    {
        Id = assignmentId,
        Success = false,
        Message = error,
        Errors = [error]
    };
}
