// ABOUTME: Applies a tenant plan assignment by copying version settings into tenant overrides.
// ABOUTME: Performs lock and quota preflight before transactional tenant-setting upserts.

using System.Globalization;
using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Plans;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.ControlPlane.Handlers.Commands;

public sealed class ApplyControlPlaneTenantPlanAssignmentCommandHandler(
    ITenantPlanRepository tenantPlanRepository,
    ITenantSettingRepository tenantSettingRepository,
    ISystemSettingRepository systemSettingRepository,
    IUnitOfWork unitOfWork)
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
        foreach (TenantPlanVersionSetting setting in version.Settings)
        {
            if (await systemSettingRepository.IsLocked(setting.SettingKey))
            {
                return Failure(request.AssignmentId, "tenant_plan_setting_locked");
            }
        }

        string? quotaError = await ValidateQuotaCeilingsAsync(version);
        if (quotaError is not null)
        {
            return Failure(request.AssignmentId, quotaError);
        }

        TenantSettingOverrideUpsert[] upserts = version.Settings
            .Select(setting => new TenantSettingOverrideUpsert(setting.SettingKey, setting.JsonValue, setting.IsLocked))
            .ToArray();

        return await unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                await tenantSettingRepository.UpsertManyForTenantAsync(request.TenantId, upserts, token);
                return new BaseCommandResponse<Guid>
                {
                    Id = request.AssignmentId,
                    Success = true,
                    Message = "Tenant plan applied."
                };
            },
            cancellationToken);
    }

    private async Task<string?> ValidateQuotaCeilingsAsync(TenantPlanVersion version)
    {
        TenantPlanVersionQuota? storageQuota = version.Quotas
            .FirstOrDefault(quota => quota.QuotaKey == TenantPlanQuotaKeys.StorageBytes);
        if (storageQuota is null)
        {
            return null;
        }

        SystemSetting? ceilingSetting = await systemSettingRepository.GetByKey(
            GovernanceSettingKeys.Storage.DefaultTenantQuotaBytes);
        if (ceilingSetting is null || !TryParseLong(ceilingSetting.Value, out long ceiling))
        {
            return null;
        }

        return storageQuota.Limit > ceiling ? "tenant_plan_quota_ceiling_exceeded" : null;
    }

    private static bool TryParseLong(string value, out long parsed)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
        {
            return true;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(value);
            if (document.RootElement.ValueKind == JsonValueKind.Number)
            {
                return document.RootElement.TryGetInt64(out parsed);
            }

            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                return long.TryParse(
                    document.RootElement.GetString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out parsed);
            }
        }
        catch (JsonException)
        {
            parsed = 0;
            return false;
        }

        parsed = 0;
        return false;
    }

    private static BaseCommandResponse<Guid> Failure(Guid assignmentId, string error) => new()
    {
        Id = assignmentId,
        Success = false,
        Message = error,
        Errors = [error]
    };
}
