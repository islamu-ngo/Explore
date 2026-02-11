// ABOUTME: Handles queries for instance-level S3 storage settings from SystemSetting records.
// ABOUTME: Reads storage configuration through shared parsing helpers for the admin settings UI.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Common;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Queries;

public class GetInstanceStorageSettingsQueryHandler : IRequestHandler<GetInstanceStorageSettingsQuery, InstanceStorageSettingsDto>
{
    private readonly ISystemSettingRepository _systemSettingRepository;

    public GetInstanceStorageSettingsQueryHandler(ISystemSettingRepository systemSettingRepository)
    {
        _systemSettingRepository = systemSettingRepository;
    }

    public async Task<InstanceStorageSettingsDto> Handle(GetInstanceStorageSettingsQuery request, CancellationToken cancellationToken)
    {
        return await InstanceStorageSettingHelpers.ReadSettingsAsync(_systemSettingRepository);
    }
}
