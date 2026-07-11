// ABOUTME: Component tests for startup gate routing decisions after instance onboarding.
// ABOUTME: Ensures single-tenant users go to events and multi-tenant admins go to instance admin settings.

using Bunit.TestDoubles;
using Explore.Blazor.Client.Pages.Onboarding;
using Explore.Blazor.Client.Tests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Blazor.Client.Tests.Pages.Onboarding;

public class StartupGateTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IInstanceOnboardingService _instanceOnboardingService;
    private readonly BunitNavigationManager _nav;

    public StartupGateTests()
    {
        _ctx = new BlazorTestContext();
        _instanceOnboardingService = Substitute.For<IInstanceOnboardingService>();

        _ctx.Services.AddSingleton(_instanceOnboardingService);
        _ctx.Services.AddSingleton(Substitute.For<ILogger<StartupGate>>());

        _nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task StartupGate_WhenSingleTenantCompleted_RedirectsToEvents()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusDto
        {
            IsCompleted = true,
            IsAuthenticated = true,
            SelectedDeploymentMode = "SingleTenant"
        });

        // Act
        _nav.NavigateTo("/startup");
        var cut = _ctx.RenderMudComponent<StartupGate>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!UriEndsWith("/events"))
            {
                throw new InvalidOperationException($"Expected redirect to /events, got '{_nav.Uri}'.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task StartupGate_WhenMultiTenantCompleted_RedirectsToInstanceAdminSettings()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusDto
        {
            IsCompleted = true,
            IsAuthenticated = true,
            IsCurrentUserInstanceAdmin = true,
            SelectedDeploymentMode = "MultiTenant"
        });

        // Act
        _nav.NavigateTo("/startup");
        var cut = _ctx.RenderMudComponent<StartupGate>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!UriEndsWith("/admin/instance/settings"))
            {
                throw new InvalidOperationException($"Expected redirect to /admin/instance/settings, got '{_nav.Uri}'.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task StartupGate_WhenMultiTenantCompletedForNonAdmin_RedirectsToEvents()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusDto
        {
            IsCompleted = true,
            IsAuthenticated = true,
            IsCurrentUserInstanceAdmin = false,
            SelectedDeploymentMode = "MultiTenant"
        });

        // Act
        _nav.NavigateTo("/startup");
        var cut = _ctx.RenderMudComponent<StartupGate>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!UriEndsWith("/events"))
            {
                throw new InvalidOperationException($"Expected redirect to /events, got '{_nav.Uri}'.");
            }
        });

        await Task.CompletedTask;
    }

    private bool UriEndsWith(string suffix)
    {
        return _nav.Uri.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }
}
