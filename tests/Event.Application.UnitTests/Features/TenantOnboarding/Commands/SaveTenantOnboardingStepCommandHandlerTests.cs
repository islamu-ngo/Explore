// ABOUTME: Tests tenant onboarding analytics emission through the shared governance layer.
// ABOUTME: Verifies onboarding progress tracking becomes pseudonymous by default and remains non-blocking.

using Explore.Application.Analytics;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.TenantOnboarding.Handlers.Commands;
using Explore.Application.Features.TenantOnboarding.Requests.Commands;
using Explore.Application.Models;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.TenantOnboarding.Commands;

public class SaveTenantOnboardingStepCommandHandlerTests
{
    [Test]
    public async Task Handle_WithPseudonymousConsent_TracksSanitizedOnboardingEvent()
    {
        var tenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000201");
        var userId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000202");
        var onboardingStateId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000203");

        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);

        var repository = Substitute.For<ITenantOnboardingStateRepository>();
        repository.GetByTenantId(tenantId).Returns((TenantOnboardingState?)null);
        repository.Create(Arg.Any<TenantOnboardingState>()).Returns(ci =>
        {
            var entity = ci.Arg<TenantOnboardingState>();
            entity.Id = onboardingStateId;
            return entity;
        });

        var analyticsProvider = Substitute.For<IAnalyticsProvider>();
        var analyticsConfigResolver = Substitute.For<IAnalyticsConfigResolver>();
        analyticsConfigResolver.ResolveAsync(Arg.Any<CancellationToken>()).Returns(new AnalyticsConfiguration
        {
            Provider = AnalyticsProviderEnum.Posthog,
            IsEnabled = true,
            ConsentMode = AnalyticsConsentMode.Pseudonymous
        });

        var adminContext = Substitute.For<IAdminContext>();
        adminContext.IsTenantAdminAsync(tenantId, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new SaveTenantOnboardingStepCommandHandler(
            tenantContext,
            repository,
            analyticsProvider,
            analyticsConfigResolver,
            new AnalyticsGovernanceService(),
            adminContext);

        var command = new SaveTenantOnboardingStepCommand
        {
            UserId = userId,
            CurrentStep = 2,
            TotalSteps = 5,
            CompletedSteps = ["welcome", "branding"]
        };

        var response = await handler.Handle(command, CancellationToken.None);

        await Assert.That(response.IsSuccess).IsTrue();
        await analyticsProvider.Received(1).TrackAsync(
            Arg.Is<string>(x => x.StartsWith("pseudo-")),
            "onboarding.step_completed",
            Arg.Is<IDictionary<string, object>?>(props =>
                props != null
                && props.ContainsKey(AnalyticsEvents.Properties.TenantId)
                && props.ContainsKey(AnalyticsEvents.Properties.StepName)
                && !props.ContainsKey("email")),
            Arg.Any<CancellationToken>());
    }
}
