// ABOUTME: Handles queries for effective instance governance settings used in onboarding/admin UI.
// ABOUTME: Reads settings from SystemSetting records through service layer.

using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Queries;

public class GetInstanceGovernanceSettingsQueryHandler : IRequestHandler<GetInstanceGovernanceSettingsQuery, InstanceGovernanceSettingsDto>
{
    private readonly IInstanceGovernanceSettingService _governanceSettingService;

    public GetInstanceGovernanceSettingsQueryHandler(IInstanceGovernanceSettingService governanceSettingService)
    {
        _governanceSettingService = governanceSettingService;
    }

    public async Task<InstanceGovernanceSettingsDto> Handle(GetInstanceGovernanceSettingsQuery request, CancellationToken cancellationToken)
    {
        return await _governanceSettingService.ReadSettingsAsync();
    }
}
