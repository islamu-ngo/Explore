// ABOUTME: Component tests for InstanceOnboarding wizard completion flow and redirect outcomes.
// ABOUTME: Verifies single-tenant host choice behavior and multi-tenant admin redirect behavior.

using Bunit.TestDoubles;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Pages.Onboarding;
using Explore.Blazor.Client.Tests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Explore.Blazor.Client.Tests.Pages.Onboarding;

public class InstanceOnboardingTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IInstanceOnboardingService _instanceOnboardingService;
    private readonly IUserService _userService;
    private readonly IGroupService _groupService;
    private readonly FakeNavigationManager _nav;

    public InstanceOnboardingTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Setup Admin");

        _instanceOnboardingService = Substitute.For<IInstanceOnboardingService>();
        _userService = Substitute.For<IUserService>();
        _groupService = Substitute.For<IGroupService>();

        _ctx.Services.AddSingleton(_instanceOnboardingService);
        _ctx.Services.AddSingleton(_userService);
        _ctx.Services.AddSingleton(_groupService);
        _ctx.Services.AddSingleton(Substitute.For<ILogger<InstanceOnboarding>>());

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(new OkHttpHandler())
        {
            BaseAddress = new Uri("https://localhost/")
        });
        _ctx.Services.AddSingleton(httpClientFactory);

        _nav = _ctx.Services.GetRequiredService<FakeNavigationManager>();

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

        _instanceOnboardingService.CompleteAsync(Arg.Any<InstanceGovernanceSettingsModel>())
            .Returns(new InstanceCommandResponseModel
            {
                Success = true,
                Message = "ok"
            });

        _groupService.CreateGroupAsync(Arg.Any<string>(), Arg.Any<string?>()).Returns(true);
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task DeploymentStep_SingleTenant_HidesTenantSelfServiceRegistrationOption()
    {
        // Arrange
        var cut = RenderForDeploymentMode("SingleTenant");

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (cut.Markup.Contains("Allow tenant self-service registration", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Tenant self-service option should be hidden in single-tenant mode.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task DeploymentStep_MultiTenant_ShowsTenantSelfServiceRegistrationOption()
    {
        // Arrange
        var cut = RenderForDeploymentMode("MultiTenant");

        // Act — navigate to Instance Settings step where tenant governance now lives
        ClickButton(cut, "Next");

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Allow tenant self-service registration", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Tenant self-service option should be visible in multi-tenant mode.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompleteOnboarding_SingleTenantPersonal_RedirectsToEvents()
    {
        // Arrange
        var cut = RenderForDeploymentMode("SingleTenant");

        // Act
        GoToSingleTenantHostStep(cut);
        ClickButton(cut, "Publish Under My Account");
        ClickButton(cut, "Next");
        ClickButton(cut, "Complete Instance Onboarding");

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
    public async Task CompleteOnboarding_SingleTenantOrganization_RedirectsToOrganizationCreate()
    {
        // Arrange
        var cut = RenderForDeploymentMode("SingleTenant");

        // Act
        GoToSingleTenantHostStep(cut);
        ClickButton(cut, "Organization (Formal Setup)");
        ClickButton(cut, "Next");
        ClickButton(cut, "Complete Instance Onboarding");

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!UriEndsWith("/organization/create"))
            {
                throw new InvalidOperationException($"Expected redirect to /organization/create, got '{_nav.Uri}'.");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task CompleteOnboarding_SingleTenantGroup_CreatesStarterGroup_AndRedirectsToEvents()
    {
        // Arrange
        var cut = RenderForDeploymentMode("SingleTenant");

        // Act
        GoToSingleTenantHostStep(cut);
        ClickButton(cut, "Group (Informal Quick Setup)");
        ClickButton(cut, "Next");
        ClickButton(cut, "Complete Instance Onboarding");

        // Assert
        cut.WaitForAssertion(() =>
        {
            if (!UriEndsWith("/events"))
            {
                throw new InvalidOperationException($"Expected redirect to /events, got '{_nav.Uri}'.");
            }
        });

        await _groupService.Received(1).CreateGroupAsync(
            Arg.Is<string>(name => !string.IsNullOrWhiteSpace(name)),
            Arg.Any<string?>());
    }

    [Test]
    public async Task CompleteOnboarding_MultiTenant_RedirectsToInstanceAdminSettings()
    {
        // Arrange
        var cut = RenderForDeploymentMode("MultiTenant");

        // Act
        ClickButton(cut, "Next");
        ClickButton(cut, "Next");
        ClickButton(cut, "Complete Instance Onboarding");

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

    private IRenderedComponent<InstanceOnboarding> RenderForDeploymentMode(string deploymentMode)
    {
        _instanceOnboardingService.GetSettingsAsync().Returns(new InstanceGovernanceSettingsModel
        {
            DeploymentMode = deploymentMode,
            DefaultPublicHomePage = "EventList",
            DefaultBrandDisplayName = "ISLAMU Explore"
        });

        var cut = _ctx.RenderMudComponent<InstanceOnboarding>();
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Choose Your Tenant Mode", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Onboarding form did not finish loading.");
            }
        });

        return cut;
    }

    private static void GoToSingleTenantHostStep(IRenderedComponent<InstanceOnboarding> cut)
    {
        ClickButton(cut, "Next");
        ClickButton(cut, "Next");
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Set Up Your First Host", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Expected first host setup step for single tenant mode.");
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

    private bool UriEndsWith(string suffix)
    {
        return _nav.Uri.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class OkHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
