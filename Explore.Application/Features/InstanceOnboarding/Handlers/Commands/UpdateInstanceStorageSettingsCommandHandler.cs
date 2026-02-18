// ABOUTME: Handles updates to instance-level S3 storage settings by authorized instance administrators.
// ABOUTME: Persists S3 configuration to SystemSetting records and invalidates the resolver cache.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateInstanceStorageSettingsCommandHandler : IRequestHandler<UpdateInstanceStorageSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IInstanceStorageSettingService _storageSettingService;
    private readonly IS3ConfigResolver _s3ConfigResolver;

    public UpdateInstanceStorageSettingsCommandHandler(
        IUserRoleRepository userRoleRepository,
        IInstanceStorageSettingService storageSettingService,
        IS3ConfigResolver s3ConfigResolver)
    {
        _userRoleRepository = userRoleRepository;
        _storageSettingService = storageSettingService;
        _s3ConfigResolver = s3ConfigResolver;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateInstanceStorageSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var isInstanceAdmin = await _userRoleRepository.IsUserPlatformAdmin(request.UserId);
        if (!isInstanceAdmin)
        {
            response.Success = false;
            response.Message = "Only instance administrators can update storage settings.";
            return response;
        }

        await _storageSettingService.ApplySettingsAsync(request.Settings);

        // Invalidate S3 config cache so changes take effect immediately
        _s3ConfigResolver.InvalidateCache();

        response.Success = true;
        response.Message = "Storage settings updated successfully.";
        return response;
    }
}
