// ABOUTME: Handles public system onboarding status reads for API/BFF startup decisions.
// ABOUTME: Uses configured onboarding mode before setup and persisted runtime mode after setup.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Onboarding;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.InstanceOnboarding.Handlers.Queries;

public sealed class GetSystemOnboardingStatusQueryHandler
    : IRequestHandler<GetSystemOnboardingStatusQuery, SystemOnboardingStatusDto>
{
    private readonly IInstanceBootstrapStateRepository _instanceBootstrapStateRepository;
    private readonly IDeploymentModeProvider _deploymentModeProvider;

    public GetSystemOnboardingStatusQueryHandler(
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        IDeploymentModeProvider deploymentModeProvider)
    {
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _deploymentModeProvider = deploymentModeProvider;
    }

    public async Task<SystemOnboardingStatusDto> Handle(GetSystemOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var bootstrap = await _instanceBootstrapStateRepository.GetCurrent(cancellationToken);
        var requiresOnboarding = bootstrap?.Status != InstanceBootstrapStatus.Completed;
        var deploymentMode = requiresOnboarding
            ? await _deploymentModeProvider.GetConfiguredOnboardingModeAsync(cancellationToken)
            : bootstrap!.DeploymentMode;

        return new SystemOnboardingStatusDto
        {
            RequiresOnboarding = requiresOnboarding,
            DeploymentMode = deploymentMode.ToString()
        };
    }
}
