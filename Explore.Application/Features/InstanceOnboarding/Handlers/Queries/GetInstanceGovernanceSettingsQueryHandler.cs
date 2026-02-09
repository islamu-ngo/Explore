// ABOUTME: Handles queries for effective instance governance settings used in onboarding/admin UI.
// ABOUTME: Reads settings from SystemSetting records through shared parsing helpers.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Common;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Queries;

public class GetInstanceGovernanceSettingsQueryHandler : IRequestHandler<GetInstanceGovernanceSettingsQuery, InstanceGovernanceSettingsDto>
{
    private readonly ISystemSettingRepository _systemSettingRepository;

    public GetInstanceGovernanceSettingsQueryHandler(ISystemSettingRepository systemSettingRepository)
    {
        _systemSettingRepository = systemSettingRepository;
    }

    public async Task<InstanceGovernanceSettingsDto> Handle(GetInstanceGovernanceSettingsQuery request, CancellationToken cancellationToken)
    {
        return await InstanceGovernanceSettingHelpers.ReadSettingsAsync(_systemSettingRepository);
    }
}
