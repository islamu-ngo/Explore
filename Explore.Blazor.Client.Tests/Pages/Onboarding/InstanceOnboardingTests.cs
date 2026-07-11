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

        _instanceOnboardingService.GetStatusAsync().Returns(new InstanceOnboardingStatusDto
        {
            IsCompleted = false,
            IsAuthenticated = true
        });

        _instanceOnboardingService.CompleteAsync(Arg.Any<CompleteInstanceOnboardingRequest>())
            .Returns(new BaseCommandResponseOfGuid
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
    public async Task MultiTenantAdministrationAccessChoice_IsRendered()
    {
        var cut = RenderForDeploymentMode("MultiTenant");

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Platform administration access", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Embedded admin area", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected multi-tenant onboarding to render the administration access choice.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task SingleTenantAdministrationAccessChoice_IsNotRendered()
    {
        var cut = RenderForDeploymentMode("SingleTenant");

        cut.WaitForAssertion(() =>
        {
            if (cut.Markup.Contains("Platform administration access", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("Embedded admin area", StringComparison.OrdinalIgnoreCase)
                || cut.Markup.Contains("Dedicated admin hostname", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Single-tenant onboarding must not expose platform administration access choices.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompleteOnboarding_SingleTenant_ShowsLaunchHandoff()
    {
        var cut = RenderForDeploymentMode("SingleTenant");

        GoToReviewAndLaunch(cut);
        ClickMudButton(cut, "Launch Instance");

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
            Arg.Is<CompleteInstanceOnboardingRequest>(model => model != null
                && model.DeploymentMode == DeploymentMode.SingleTenant
                && model.SiteProfile!.SiteName == "ISLAMU Explore"
                && model.SiteProfile.Locale == "en"
                && model.SiteProfile.TimeZone == "UTC"));
    }

    [Test]
    public async Task CompleteOnboarding_MultiTenant_ShowsLaunchHandoff()
    {
        var cut = RenderForDeploymentMode("MultiTenant");

        GoToReviewAndLaunch(cut);
        ClickMudButton(cut, "Launch Instance");

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
        var cut = RenderForDeploymentMode("MultiTenant");

        GoToReviewAndLaunch(cut);
        ClickMudButton(cut, "Launch Instance");

        // Assert
        await _instanceOnboardingService.Received(1).CompleteAsync(
            Arg.Is<CompleteInstanceOnboardingRequest>(model => model != null
                && model.DeploymentMode == DeploymentMode.MultiTenant
                && model.SiteProfile!.SiteName == "ISLAMU Explore"
                && model.AdministrationAccessMode == "Embedded"
                && string.IsNullOrWhiteSpace(model.AdminHost)));
    }

    [Test]
    public async Task CompleteOnboarding_RefreshesAuthSession_BeforeRedirect()
    {
        var cut = RenderForDeploymentMode("MultiTenant");

        GoToReviewAndLaunch(cut);
        ClickMudButton(cut, "Launch Instance");

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

    private IRenderedComponent<InstanceOnboarding> RenderForDeploymentMode(
        string deploymentMode,
        OnboardingPreflightDto? preflight = null)
    {
        _instanceOnboardingService.GetSystemOnboardingStatusAsync().Returns(new SystemOnboardingStatusDto
        {
            RequiresOnboarding = true,
            DeploymentMode = deploymentMode
        });

        _instanceOnboardingService.GetBrandingSettingsAsync().Returns(new BrandingSettingsDto
        {
            DefaultBrandDisplayName = "ISLAMU Explore"
        });

        _instanceOnboardingService.GetOnboardingPreflightAsync().Returns(preflight ?? new OnboardingPreflightDto
        {
            DeploymentMode = deploymentMode,
            IsReadyToLaunch = true,
            BlockingChecks =
            [
                new OnboardingPreflightCheckDto
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
                new OnboardingPreflightCheckDto
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

    private void GoToReviewAndLaunch(IRenderedComponent<InstanceOnboarding> cut)
    {
        cut.Instance._preflight = _instanceOnboardingService.GetOnboardingPreflightAsync().GetAwaiter().GetResult();
        cut.Instance._activeStep = 1;
        cut.Render();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Critical Launch Requirements", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Review & Launch step did not render after setting _activeStep.");
            }
        }, TimeSpan.FromSeconds(5));
    }

    private static void ClickMudButton(IRenderedComponent<InstanceOnboarding> cut, string text)
    {
        var htmlButton = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains(text, StringComparison.OrdinalIgnoreCase));

        if (htmlButton is null)
        {
            throw new InvalidOperationException($"Button containing '{text}' was not found.");
        }

        htmlButton.Click();
    }

    [Test]
    public async Task StepIndicator_ShowsCorrectStepNumbers()
    {
        var cut = RenderForDeploymentMode("SingleTenant");

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Step 1 of 2", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected Step 1 of 2 indicator on Site Profile.");
            }
        });

        GoToReviewAndLaunch(cut);

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Step 2 of 2", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected Step 2 of 2 indicator on Review & Launch.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task StepRail_ShowsAllSteps()
    {
        var cut = RenderForDeploymentMode("SingleTenant");

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Step 1", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Site Profile", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Step 2", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Review & Launch", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected step rail to show both steps.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task LaunchRecapPanel_ShowsSummary()
    {
        var cut = RenderForDeploymentMode("SingleTenant");

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Launch Recap", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Site:", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Mode:", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Auth:", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Destination:", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected Launch Recap panel with summary fields.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task ReviewAndLaunch_ShowsBlockingChecks()
    {
        var cut = RenderForDeploymentMode("SingleTenant");
        GoToReviewAndLaunch(cut);

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Critical Launch Requirements", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("Critical launch requirements passed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected Critical Launch Requirements section with passed checks summary.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task ReviewAndLaunch_MultiTenant_ShowsDnsChecklistWarnings()
    {
        var cut = RenderForDeploymentMode("MultiTenant", new OnboardingPreflightDto
        {
            DeploymentMode = "MultiTenant",
            IsReadyToLaunch = true,
            BlockingChecks = new List<OnboardingPreflightCheckDto>
            {
                new OnboardingPreflightCheckDto
                {
                    Code = "setup_secret",
                    Name = "Setup Secret",
                    Severity = "Blocking",
                    Status = "Pass",
                    Message = "Setup secret is active."
                }
            },
            WarningChecks = new List<OnboardingPreflightCheckDto>
            {
                new OnboardingPreflightCheckDto
                {
                    Code = "dns_public_platform",
                    Name = "Public platform DNS",
                    Severity = "Warning",
                    Status = "Warning",
                    Message = "Point the public platform host events.example.org at the Blazor/BFF entry point before launch.",
                    Detail = "Create an A/AAAA or CNAME record at your edge provider."
                }
            }
        });
        GoToReviewAndLaunch(cut);

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Public platform DNS", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("events.example.org", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected DNS checklist warning to render in Review & Launch.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task ReviewAndLaunch_WithFailedBlockingCheck_PreventsLaunch()
    {
        var cut = RenderForDeploymentMode("SingleTenant", new OnboardingPreflightDto
        {
            DeploymentMode = "SingleTenant",
            IsReadyToLaunch = false,
            BlockingChecks = new List<OnboardingPreflightCheckDto>
            {
                new OnboardingPreflightCheckDto
                {
                    Code = "setup_secret",
                    Name = "Setup Secret",
                    Severity = "Blocking",
                    Status = "Fail",
                    Message = "Setup secret is missing or invalid."
                }
            },
            WarningChecks = new List<OnboardingPreflightCheckDto>()
        });
        GoToReviewAndLaunch(cut);

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Setup secret is missing or invalid.", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected failed blocking check message to be visible.");
            }
        });

        ClickMudButton(cut, "Launch Instance");
        await _instanceOnboardingService.DidNotReceive().CompleteAsync(Arg.Any<CompleteInstanceOnboardingRequest>());
    }

    [Test]
    public async Task ReviewAndLaunch_Acknowledgements_RequiredForSeriousWarnings()
    {
        var cut = RenderForDeploymentMode("SingleTenant", new OnboardingPreflightDto
        {
            DeploymentMode = "SingleTenant",
            IsReadyToLaunch = true,
            BlockingChecks = new List<OnboardingPreflightCheckDto>
            {
                new OnboardingPreflightCheckDto
                {
                    Code = "setup_secret",
                    Name = "Setup Secret",
                    Severity = "Blocking",
                    Status = "Pass",
                    Message = "Setup secret is active."
                }
            },
            WarningChecks = new List<OnboardingPreflightCheckDto>
            {
                new OnboardingPreflightCheckDto
                {
                    Code = "public_exposure",
                    Name = "Public Exposure",
                    Severity = "Critical",
                    Status = "Warning",
                    Message = "Site will be publicly accessible."
                }
            }
        });
        GoToReviewAndLaunch(cut);

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Acknowledgements", StringComparison.OrdinalIgnoreCase)
                || !cut.Markup.Contains("publicly accessible", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected acknowledgement section for serious warning.");
            }
        });

        ClickMudButton(cut, "Launch Instance");
        await _instanceOnboardingService.DidNotReceive().CompleteAsync(Arg.Any<CompleteInstanceOnboardingRequest>());
    }

    private sealed class OkHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
