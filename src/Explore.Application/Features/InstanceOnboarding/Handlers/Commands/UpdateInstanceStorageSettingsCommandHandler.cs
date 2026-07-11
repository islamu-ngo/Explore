// ABOUTME: Handles updates to provider-neutral instance storage settings by instance administrators.
// ABOUTME: Validates storage policy, persists settings, and invalidates S3 resolver cache when relevant.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding.Validators;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateInstanceStorageSettingsCommandHandler : IRequestHandler<UpdateInstanceStorageSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IAdminContext _adminContext;
    private readonly IInstanceStorageSettingService _storageSettingService;
    private readonly IS3ConfigResolver _s3ConfigResolver;

    public UpdateInstanceStorageSettingsCommandHandler(
        IAdminContext adminContext,
        IInstanceStorageSettingService storageSettingService,
        IS3ConfigResolver s3ConfigResolver)
    {
        _adminContext = adminContext;
        _storageSettingService = storageSettingService;
        _s3ConfigResolver = s3ConfigResolver;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateInstanceStorageSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var isInstanceAdmin = await _adminContext.IsInstanceAdminAsync(request.UserId, cancellationToken);
        if (!isInstanceAdmin)
        {
            response.Success = false;
            response.Message = "Only instance administrators can update storage settings.";
            return response;
        }

        var validator = new InstanceStorageSettingsDtoValidator();
        var validationResult = await validator.ValidateAsync(request.Settings, cancellationToken);
        if (!validationResult.IsValid)
        {
            response.Success = false;
            response.Message = "Storage settings validation failed.";
            response.Errors = validationResult.Errors.Select(error => error.ErrorMessage).ToList();
            return response;
        }

        await _storageSettingService.ApplySettingsAsync(request.Settings);

        // Invalidate S3 config cache so optional S3 provider changes take effect immediately.
        _s3ConfigResolver.InvalidateCache();

        response.Success = true;
        response.Message = "Storage settings updated successfully.";
        return response;
    }
}
