// ABOUTME: Handles updates to instance-level S3 storage settings by authorized instance administrators.
// ABOUTME: Persists S3 configuration to SystemSetting records and invalidates the resolver cache.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.InstanceOnboarding.Common;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Commands;

public class UpdateInstanceStorageSettingsCommandHandler : IRequestHandler<UpdateInstanceStorageSettingsCommand, BaseCommandResponse<Guid>>
{
    private readonly IInstanceAdministratorRepository _instanceAdministratorRepository;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly IS3ConfigResolver _s3ConfigResolver;

    public UpdateInstanceStorageSettingsCommandHandler(
        IInstanceAdministratorRepository instanceAdministratorRepository,
        ISystemSettingRepository systemSettingRepository,
        IS3ConfigResolver s3ConfigResolver)
    {
        _instanceAdministratorRepository = instanceAdministratorRepository;
        _systemSettingRepository = systemSettingRepository;
        _s3ConfigResolver = s3ConfigResolver;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateInstanceStorageSettingsCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var isInstanceAdmin = await _instanceAdministratorRepository.IsUserInstanceAdmin(request.UserId);
        if (!isInstanceAdmin)
        {
            response.Success = false;
            response.Message = "Only instance administrators can update storage settings.";
            return response;
        }

        await InstanceStorageSettingHelpers.ApplySettingsAsync(
            _systemSettingRepository,
            request.Settings);

        // Invalidate S3 config cache so changes take effect immediately
        _s3ConfigResolver.InvalidateCache();

        response.Success = true;
        response.Message = "Storage settings updated successfully.";
        return response;
    }
}
