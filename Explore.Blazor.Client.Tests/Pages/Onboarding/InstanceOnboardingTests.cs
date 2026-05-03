// ABOUTME: Component tests for InstanceOnboarding wizard completion flow and redirect outcomes.
// ABOUTME: Verifies convention-first single-tenant completion and multi-tenant admin redirect behavior.

using Explore.Blazor.Client.Models.Responses;
using Explore.Blazor.Client.Pages.Onboarding;

namespace Explore.Blazor.Client.Tests.Pages.Onboarding;

public class InstanceOnboardingTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IInstanceOnboardingService _instanceOnboardingService;
    private readonly IUserService _userService;
    private readonly BunitNavigationManager _nav;

    public InstanceOnboardingTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Setup Admin");

        _instanceOnboardingService = Substitute.For<IInstanceOnboardingService>();
        _userService = Substitute.For<IUserService>();

        _ctx.Services.AddSingleton(_instanceOnboardingService);
        _ctx.Services.AddSingleton(_userService);
        _ctx.Services.AddSingleton(Substitute.For<ILogger<InstanceOnboarding>>());

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(new OkHttpHandler())
        {
            BaseAddress = new Uri("https://localhost/")
        });
        _ctx.Services.AddSingleton(httpClientFactory);

        _nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();

        _userService.SyncUserAsync().Returns(new BaseCommandResponseOfGuid { Success = true });
        _userService.GetCurrentUserAsync().Returns(new UserDto
        {
            Email = "setup-admin@example.com",
            FirstName = "Setup",
            LastName = "Admin"
        });

        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsAuthenticated = true
        });

        _instanceOnboardingService.CompleteAsync(Arg.Any<OnboardingCompletionModel>())
            .Returns(new InstanceCommandResponseModel
            {
                Success = true,
                Message = "ok"
            });
        _instanceOnboardingService.RefreshAuthSessionAsync().Returns(true);

        SetupBffJsModule();
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task DeploymentModeChoice_IsNotRendered()
    {
        // Arrange
        var cut = RenderForDeploymentMode("SingleTenant");

        // Assert — deployment mode is fixed by API system config, not chosen by the user.
        cut.WaitForAssertion(() =>
        {
            if (cut.Markup.Contains("Choose Your Tenant Mode", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("Help me choose", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("Single Tenant (Recommended)", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("Multi Tenant (Advanced)", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Tenant mode chooser should not be visible during onboarding.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task SingleTenantHostChoice_IsNotRendered()
    {
        // Arrange
        var cut = RenderForDeploymentMode("SingleTenant");

        // Assert — single-tenant onboarding is convention-first and does not ask for first publisher scope.
        cut.WaitForAssertion(() =>
        {
            if (cut.Markup.Contains("Set Up Your First Host", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("Personal Account", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("Quick Group", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("Formal Organization", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("I Will Do This Later", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Single-tenant first host choice UI should not be visible during onboarding.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompleteOnboarding_SingleTenant_ShowsLaunchHandoff()
    {
        // Arrange
        var cut = RenderForDeploymentMode("SingleTenant");

        // Act
        GoToPreflight(cut);
        ClickButton(cut, "Complete Instance Onboarding");

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Your Site Is Ready", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Browse Events", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Open Instance Settings", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected launch handoff after completing SingleTenant onboarding.");
            }
        });

        await _instanceOnboardingService.Received(1).CompleteAsync(
            Arg.Is<OnboardingCompletionModel>(model => model != null
                && model.DeploymentMode == "SingleTenant"
                && model.SiteProfile.SiteName == "ISLAMU Explore"
                && model.SiteProfile.Locale == "en"
                && model.SiteProfile.TimeZone == "UTC"));
    }

    [Test]
    public async Task CompleteOnboarding_MultiTenant_ShowsLaunchHandoff()
    {
        // Arrange
        var cut = RenderForDeploymentMode("MultiTenant");

        // Act
        GoToPreflight(cut);
        ClickButton(cut, "Complete Instance Onboarding");

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Your Site Is Ready", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Open Instance Settings", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected launch handoff after completing MultiTenant onboarding.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompleteOnboarding_MultiTenant_SendsConfiguredDeploymentMode()
    {
        // Arrange
        var cut = RenderForDeploymentMode("MultiTenant");

        // Act
        GoToPreflight(cut);
        ClickButton(cut, "Complete Instance Onboarding");

        // Assert
        await _instanceOnboardingService.Received(1).CompleteAsync(
            Arg.Is<OnboardingCompletionModel>(model => model != null
                && model.DeploymentMode == "MultiTenant"
                && model.SiteProfile.SiteName == "ISLAMU Explore"));
    }

    [Test]
    public async Task CompleteOnboarding_RefreshesAuthSession_BeforeRedirect()
    {
        // Arrange
        var cut = RenderForDeploymentMode("MultiTenant");

        // Act
        GoToPreflight(cut);
        ClickButton(cut, "Complete Instance Onboarding");

        // Assert
        // RefreshAuthSessionAsync is now called twice during the Complete flow:
        // (1) proactively before the POST to guarantee a fresh access token survives the
        //     long form-fill window (Keycloak access tokens are short-lived), and
        // (2) after successful Complete to pick up the newly assigned instance-admin claims.
        // Both calls are load-bearing; see InstanceOnboarding.razor CompleteOnboardingAsync.
        await _instanceOnboardingService.Received(2).RefreshAuthSessionAsync();
        await Task.CompletedTask;
    }

    private void SetupBffJsModule(bool syncOk = true)
    {
        var module = _ctx.JSInterop.SetupModule("/js/bff.js");

        module.Setup<BffMutationResult>("syncSetupSecret", _ => true)
            .SetResult(new BffMutationResult
            {
                Ok = syncOk,
                Status = syncOk ? 200 : 400,
                Error = syncOk ? null : "Sync failed."
            });
    }

    private IRenderedComponent<InstanceOnboarding> RenderForDeploymentMode(string deploymentMode)
    {
        _instanceOnboardingService.GetSystemOnboardingStatusAsync().Returns(new SystemOnboardingStatusModel
        {
            RequiresOnboarding = true,
            DeploymentMode = deploymentMode
        });

        _instanceOnboardingService.GetBrandingSettingsAsync().Returns(new BrandingSettingsModel
        {
            DefaultBrandDisplayName = "ISLAMU Explore"
        });

        _instanceOnboardingService.GetOnboardingPreflightAsync().Returns(new OnboardingPreflightModel
        {
            DeploymentMode = deploymentMode,
            IsReadyToLaunch = true,
            BlockingChecks =
            [
                new OnboardingPreflightCheckModel
                {
                    Code = "setup_secret",
                    Name = "Setup Secret",
                    Severity = "Blocking",
                    Status = "Pass",
                    Message = "Setup secret is active."
                }
            ],
            WarningChecks =
            [
                new OnboardingPreflightCheckModel
                {
                    Code = "smtp",
                    Name = "SMTP",
                    Severity = "Warning",
                    Status = "Warning",
                    Message = "SMTP can be configured after launch."
                }
            ]
        });

        var cut = _ctx.RenderMudComponent<InstanceOnboarding>();
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Name Your Site", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Onboarding form did not finish loading.");
            }
        });

        return cut;
    }

    private static void GoToPreflight(IRenderedComponent<InstanceOnboarding> cut)
    {
        ClickButton(cut, "Next");
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Launch Readiness", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Preflight step did not render.");
            }
        });
    }

    private static void ClickButton(IRenderedComponent<InstanceOnboarding> cut, string text)
    {
        var button = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains(text, StringComparison.OrdinalIgnoreCase));

        if (button is null)
        {
            throw new InvalidOperationException($"Button containing '{text}' was not found.");
        }

        button.Click();
    }

    private sealed class OkHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
