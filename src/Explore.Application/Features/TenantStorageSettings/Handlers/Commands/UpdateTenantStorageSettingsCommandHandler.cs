// ABOUTME: Handles tenant storage override updates under instance delegation policy.
// ABOUTME: Enforces tenant/instance admin authority, lock state, provider allow-listing, and byte ceilings.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Tenant.Validators;
using Explore.Application.Features.TenantStorageSettings.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Settings;
using MediatR;

namespace Explore.Application.Features.TenantStorageSettings.Handlers.Commands;

public sealed class UpdateTenantStorageSettingsCommandHandler
    : IRequestHandler<UpdateTenantStorageSettingsCommand, BaseCommandResponse<Guid>>
{
    private const string LockedFailureCode = "StorageTenantOverridesLocked";

    private readonly ITenantContext _tenantContext;
    private readonly IAdminContext _adminContext;
    private readonly ITenantStorageSettingService _storageSettingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly IS3ConfigResolver _s3ConfigResolver;

    public UpdateTenantStorageSettingsCommandHandler(
        ITenantContext tenantContext,
        IAdminContext adminContext,
        ITenantStorageSettingService storageSettingService,
        IUnitOfWork unitOfWork,
        IHierarchicalSettingsResolver settingsResolver,
        IS3ConfigResolver s3ConfigResolver)
    {
        _tenantContext = tenantContext;
        _adminContext = adminContext;
        _storageSettingService = storageSettingService;
        _unitOfWork = unitOfWork;
        _settingsResolver = settingsResolver;
        _s3ConfigResolver = s3ConfigResolver;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(
        UpdateTenantStorageSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var response = new BaseCommandResponse<Guid>();

        if (!await IsUserAuthorizedAsync(tenantId, request.UserId, cancellationToken))
        {
            response.Success = false;
            response.Message = "Only tenant administrators or instance administrators can update tenant storage settings.";
            return response;
        }

        var currentSettings = await _storageSettingService.ReadSettingsAsync(tenantId, cancellationToken);
        if (currentSettings.IsReadOnly)
        {
            response.Success = false;
            response.FailureCode = LockedFailureCode;
            response.Message = "Tenant storage settings are locked by instance policy.";
            response.Errors = ["Storage delegation must be unlocked by an instance administrator before tenant overrides can be saved."];
            return response;
        }

        var validator = new TenantStorageSettingsDtoValidator(currentSettings.EffectivePolicy.InstanceMaxUploadBytes);
        var validationResult = await validator.ValidateAsync(request.Settings, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Tenant storage settings validation failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            ct => _storageSettingService.ApplySettingsAsync(tenantId, request.UserId, request.Settings, ct),
            cancellationToken);

        _settingsResolver.InvalidateCache(SettingScope.Tenant, tenantId);
        _s3ConfigResolver.InvalidateCache(tenantId);

        response.Success = true;
        response.Id = tenantId;
        response.Message = "Tenant storage settings updated successfully.";
        return response;
    }

    private async Task<bool> IsUserAuthorizedAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (await _adminContext.IsTenantAdminAsync(tenantId, cancellationToken))
        {
            return true;
        }

        var adminTenantIds = await _adminContext.GetAdminTenantIdsAsync(userId, cancellationToken);
        if (adminTenantIds.Contains(tenantId))
        {
            return true;
        }

        return await _adminContext.IsInstanceAdminAsync(userId, cancellationToken);
    }
}
