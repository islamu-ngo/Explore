// ABOUTME: Handles tenant storage override patches under instance delegation policy.
// ABOUTME: Merges supplied leaves for validation, then persists only those leaves transactionally.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Tenant;
using Explore.Application.DTOs.Tenant.Validators;
using Explore.Application.Features.TenantStorageSettings.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Settings;
using MediatR;

namespace Explore.Application.Features.TenantStorageSettings.Handlers.Commands;

public sealed class PatchTenantStorageSettingsCommandHandler
    : IRequestHandler<PatchTenantStorageSettingsCommand, BaseCommandResponse<Guid>>
{
    private const string LockedFailureCode = "StorageTenantOverridesLocked";

    private readonly ITenantContext _tenantContext;
    private readonly IAdminContext _adminContext;
    private readonly ITenantStorageSettingService _storageSettingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly IS3ConfigResolver _s3ConfigResolver;

    public PatchTenantStorageSettingsCommandHandler(
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
        PatchTenantStorageSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var response = new BaseCommandResponse<Guid>();

        if (!await IsUserAuthorizedAsync(tenantId, request.UserId, cancellationToken))
        {
            response.Success = false;
            response.Message = "Only tenant administrators or instance administrators can patch tenant storage settings.";
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

        var patchValidator = new PatchTenantStorageSettingsDtoValidator();
        var validationResult = await patchValidator.ValidateAsync(request.Settings, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Tenant storage settings validation failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        MergePatch(currentSettings, request.Settings);
        var settingsValidator = new TenantStorageSettingsDtoValidator(currentSettings.EffectivePolicy.InstanceMaxUploadBytes);
        validationResult = await settingsValidator.ValidateAsync(currentSettings, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Tenant storage settings validation failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            ct => _storageSettingService.ApplyPatchAsync(tenantId, request.UserId, request.Settings, ct),
            cancellationToken);

        _settingsResolver.InvalidateCache(SettingScope.Tenant, tenantId);
        _s3ConfigResolver.InvalidateCache(tenantId);

        response.Success = true;
        response.Id = tenantId;
        response.Message = "Tenant storage settings patched successfully.";
        return response;
    }

    private static void MergePatch(
        TenantStorageSettingsDto current,
        PatchTenantStorageSettingsDto patch)
    {
        if (patch.Policy?.Provider is { HasValue: true } provider)
        {
            current.Provider = provider.Value ?? string.Empty;
        }

        if (patch.Policy?.MaxUploadBytes is { HasValue: true } maxUploadBytes)
        {
            current.MaxUploadBytes = maxUploadBytes.Value;
        }

        if (patch.Policy?.TenantQuotaBytes is { HasValue: true } tenantQuotaBytes)
        {
            current.TenantQuotaBytes = tenantQuotaBytes.Value;
        }

        if (patch.Policy?.Routes is { HasValue: true } routes)
        {
            current.Routes = routes.Value ?? [];
        }

        if (patch.S3?.Endpoint is { HasValue: true } endpoint)
        {
            current.S3Endpoint = endpoint.Value ?? string.Empty;
        }

        if (patch.S3?.PublicEndpoint is { HasValue: true } publicEndpoint)
        {
            current.S3PublicEndpoint = publicEndpoint.Value ?? string.Empty;
        }

        if (patch.S3?.BucketName is { HasValue: true } bucketName)
        {
            current.S3BucketName = bucketName.Value ?? string.Empty;
        }

        if (patch.S3?.AccessKeyId is { HasValue: true } accessKeyId)
        {
            current.S3AccessKeyId = accessKeyId.Value ?? string.Empty;
        }

        if (patch.S3?.SecretAccessKey is { HasValue: true } secretAccessKey)
        {
            current.S3SecretAccessKey = secretAccessKey.Value ?? string.Empty;
        }

        if (patch.S3?.Region is { HasValue: true } region)
        {
            current.S3Region = region.Value ?? string.Empty;
        }

        if (patch.S3?.ForcePathStyle is { HasValue: true } forcePathStyle)
        {
            current.S3ForcePathStyle = forcePathStyle.Value;
        }

        if (patch.S3?.UploadUrlExpirationMinutes is { HasValue: true } expirationMinutes)
        {
            current.S3UploadUrlExpirationMinutes = expirationMinutes.Value;
        }
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
