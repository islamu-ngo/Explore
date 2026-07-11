// ABOUTME: Unit tests for system onboarding status query behavior.
// ABOUTME: Verifies the handler forwards caller cancellation into bootstrap state lookup.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.InstanceOnboarding.Handlers.Queries;
using Explore.Application.Features.InstanceOnboarding.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Queries;

public sealed class GetSystemOnboardingStatusQueryHandlerTests
{
    [Test]
    public async Task Handle_WhenOnboardingIncomplete_ForwardsCancellationTokenToBootstrapLookup()
    {
        var bootstrapRepository = Substitute.For<IInstanceBootstrapStateRepository>();
        var deploymentModeProvider = Substitute.For<IDeploymentModeProvider>();
        bootstrapRepository.GetCurrent(Arg.Any<CancellationToken>()).Returns((InstanceBootstrapState?)null);
        deploymentModeProvider.GetConfiguredOnboardingModeAsync(Arg.Any<CancellationToken>()).Returns(DeploymentMode.SingleTenant);
        var handler = new GetSystemOnboardingStatusQueryHandler(bootstrapRepository, deploymentModeProvider);
        using var cancellationSource = new CancellationTokenSource();

        var result = await handler.Handle(new GetSystemOnboardingStatusQuery(), cancellationSource.Token);

        await Assert.That(result.RequiresOnboarding).IsTrue();
        await bootstrapRepository.Received(1).GetCurrent(cancellationSource.Token);
    }
}
