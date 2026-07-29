// ABOUTME: Component tests for startup gate routing decisions after instance onboarding.
// ABOUTME: Covers mode-specific handoff plus fail-closed incomplete, invalid, null, and error states.

using Bunit.TestDoubles;
using Explore.Blazor.Client.Pages.Onboarding;
using Explore.Blazor.Client.Routing.ControlPlane;
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
            IsCurrentUserInstanceAdmin = true,
            SelectedDeploymentMode = "SingleTenant"
        });

        // Act
        _nav.NavigateTo("/startup");
        var cut = _ctx.RenderMudComponent<StartupGate>();

        // Assert
        cut.WaitForAssertion(() => AssertUriEndsWith("/events"));

        await Task.CompletedTask;
    }

    [Test]
    public async Task StartupGate_WhenMultiTenantAdminCompletedWithZeroTenants_RedirectsToControlPlaneOverview()
    {
        // Arrange
        // No tenant-count dependency is registered: an empty control plane must still receive the admin handoff.
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
        cut.WaitForAssertion(() => AssertUriEndsWith(ControlPlaneRoutes.Overview));

        await _instanceOnboardingService.Received(1).GetStatusAsync();
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
        cut.WaitForAssertion(() => AssertUriEndsWith("/events"));

        await Task.CompletedTask;
    }

    [Test]
    public async Task StartupGate_WhenOnboardingIsIncomplete_RedirectsToSetup()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusDto
        {
            IsCompleted = false,
            IsAuthenticated = true,
            SelectedDeploymentMode = "MultiTenant"
        });

        // Act
        _nav.NavigateTo("/startup");
        var cut = _ctx.RenderMudComponent<StartupGate>();

        // Assert
        cut.WaitForAssertion(() => AssertUriEndsWith("/setup"));

        await Task.CompletedTask;
    }

    [Test]
    public async Task StartupGate_WhenAuthenticatedProviderIsConfigured_ResumesAuthorizationFromAuthoritativeState()
    {
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusDto
        {
            IsCompleted = false,
            IsAuthenticated = true
        });
        _instanceOnboardingService.GetAuthProviderConfiguredStateAsync().Returns(true);
        _instanceOnboardingService.ShouldSkipAuthorizationProviderStepAsync().Returns(false);

        _nav.NavigateTo("/startup");
        var cut = _ctx.RenderMudComponent<StartupGate>();

        cut.WaitForAssertion(() => AssertUriEndsWith("/onboarding/authz-provider"));

        await _instanceOnboardingService.Received(1).GetAuthProviderConfiguredStateAsync();
        await _instanceOnboardingService.Received(1).ShouldSkipAuthorizationProviderStepAsync();
    }

    [Test]
    public async Task StartupGate_WhenCompletedDeploymentModeIsBlank_RemainsFailClosedAndShowsError()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusDto
        {
            IsCompleted = true,
            IsAuthenticated = true,
            IsCurrentUserInstanceAdmin = true,
            SelectedDeploymentMode = " "
        });

        // Act
        _nav.NavigateTo("/startup");
        var cut = _ctx.RenderMudComponent<StartupGate>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            AssertUriEndsWith("/startup");
            AssertAlertContains(cut, "Could not determine deployment mode. Try refreshing.");
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task StartupGate_WhenCompletedDeploymentModeIsUnknown_RemainsFailClosedAndShowsError()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusDto
        {
            IsCompleted = true,
            IsAuthenticated = true,
            IsCurrentUserInstanceAdmin = true,
            SelectedDeploymentMode = "HybridTenant"
        });

        // Act
        _nav.NavigateTo("/startup");
        var cut = _ctx.RenderMudComponent<StartupGate>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            AssertUriEndsWith("/startup");
            AssertAlertContains(cut, "Could not determine deployment mode. Try refreshing.");
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task StartupGate_WhenStatusIsNull_RemainsFailClosedAndShowsError()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync().Returns((InstanceOnboardingStatusDto?)null);

        // Act
        _nav.NavigateTo("/startup");
        var cut = _ctx.RenderMudComponent<StartupGate>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            AssertUriEndsWith("/startup");
            AssertAlertContains(cut, "Could not determine onboarding status. Try refreshing.");
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task StartupGate_WhenStatusLookupFails_RemainsFailClosedAndShowsError()
    {
        // Arrange
        _instanceOnboardingService.GetStatusAsync()
            .ThrowsAsync(new HttpRequestException("Status unavailable."));

        // Act
        _nav.NavigateTo("/startup");
        var cut = _ctx.RenderMudComponent<StartupGate>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            AssertUriEndsWith("/startup");
            AssertAlertContains(cut, "Startup routing failed. Try refreshing.");
        });

        await Task.CompletedTask;
    }

    private static void AssertAlertContains(IRenderedComponent<StartupGate> cut, string expectedText)
    {
        var alert = cut.Find("[role='alert']");
        if (!alert.TextContent.Contains(expectedText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected alert to contain '{expectedText}', got '{alert.TextContent}'.");
        }
    }

    private void AssertUriEndsWith(string expectedPath)
    {
        if (!UriEndsWith(expectedPath))
        {
            throw new InvalidOperationException($"Expected redirect to {expectedPath}, got '{_nav.Uri}'.");
        }
    }

    private bool UriEndsWith(string suffix)
    {
        return _nav.Uri.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }
}
