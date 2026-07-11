// ABOUTME: Component tests for tenant onboarding completion handoff choices.
// ABOUTME: Verifies tenant admins choose administration or events before leaving onboarding.

using Explore.Blazor.Client.Models.Responses;
using Explore.Blazor.Client.Pages.Onboarding;

namespace Explore.Blazor.Client.Tests.Pages.Onboarding;

public class TenantOnboardingTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly ITenantOnboardingService _tenantOnboardingService;
    private readonly IUserService _userService;
    private readonly BunitNavigationManager _nav;

    public TenantOnboardingTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Tenant Admin");

        _tenantOnboardingService = Substitute.For<ITenantOnboardingService>();
        _userService = Substitute.For<IUserService>();

        _ctx.Services.AddSingleton(_tenantOnboardingService);
        _ctx.Services.AddSingleton(_userService);

        _nav = _ctx.Services.GetRequiredService<BunitNavigationManager>();

        _userService.SyncUserAsync().Returns(new BaseCommandResponseOfGuid { Success = true });
        _tenantOnboardingService.GetSettingsAsync().Returns(CreateSettingsModel());
        _tenantOnboardingService.CompleteAsync(Arg.Any<TenantPolicySettingsDto>())
            .Returns(new BaseCommandResponseOfGuid { Success = true, Message = "ok" });
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task AlreadyCompletedTenantOnboarding_ShowsPostCompletionChoices()
    {
        _tenantOnboardingService.GetStatusAsync().Returns(new TenantOnboardingStatusDto
        {
            IsCompleted = true,
            IsAuthenticated = true,
            IsCurrentUserTenantAdministrator = true,
            TenantId = Guid.NewGuid()
        });

        var cut = _ctx.RenderMudComponent<TenantOnboarding>();

        cut.WaitForAssertion(() => AssertCompletionChoices(cut.Markup));
        await Assert.That(_nav.Uri).EndsWith("/");
    }

    [Test]
    public async Task CompleteTenantOnboarding_ShowsPostCompletionChoicesInsteadOfRedirecting()
    {
        _tenantOnboardingService.GetStatusAsync().Returns(new TenantOnboardingStatusDto
        {
            IsCompleted = false,
            IsAuthenticated = true,
            IsCurrentUserTenantAdministrator = true,
            TenantId = Guid.NewGuid()
        });

        var cut = _ctx.RenderMudComponent<TenantOnboarding>();
        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Complete Tenant Onboarding", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Tenant onboarding form did not render.");
            }
        });

        ClickButton(cut, "Complete Tenant Onboarding");

        cut.WaitForAssertion(() => AssertCompletionChoices(cut.Markup));
        await Assert.That(_nav.Uri).EndsWith("/");
    }

    [Test]
    public async Task AlreadyCompletedTenantOnboarding_ForPlatformAdminOnly_LinksToInstanceAdministration()
    {
        _tenantOnboardingService.GetStatusAsync().Returns(new TenantOnboardingStatusDto
        {
            IsCompleted = true,
            IsAuthenticated = true,
            IsCurrentUserTenantAdministrator = false,
            IsCurrentUserPlatformAdministrator = true,
            TenantId = Guid.NewGuid()
        });

        var cut = _ctx.RenderMudComponent<TenantOnboarding>();

        cut.WaitForAssertion(() =>
        {
            AssertCompletionChoices(cut.Markup, "/admin/instance/settings");
            if (cut.Markup.Contains("/admin/tenant/settings", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Platform-admin-only handoff should not link to tenant-admin settings.");
            }
        });

        await Assert.That(_nav.Uri).EndsWith("/");
    }

    private static TenantPolicySettingsDto CreateSettingsModel() => new()
    {
        PreferredHomePage = "EventList",
        CanOverrideHomePagePreference = true,
        CanOverrideEventCardClickBehavior = true,
        CanOverrideSubdomain = true,
        CanOverrideCustomDomain = true,
        CanTenantOmitVerification = true
    };

    private static void AssertCompletionChoices(string markup, string administrationHref = "/admin/tenant/settings")
    {
        if (!markup.Contains("Tenant onboarding is complete", StringComparison.OrdinalIgnoreCase)
            || !markup.Contains("Go to administration settings", StringComparison.OrdinalIgnoreCase)
            || !markup.Contains("Go to events", StringComparison.OrdinalIgnoreCase)
            || !markup.Contains(administrationHref, StringComparison.OrdinalIgnoreCase)
            || !markup.Contains("/events", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Expected post-onboarding handoff choices were not rendered.");
        }
    }

    private static void ClickButton(IRenderedComponent<TenantOnboarding> cut, string text)
    {
        var button = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains(text, StringComparison.OrdinalIgnoreCase));

        if (button is null)
        {
            throw new InvalidOperationException($"Button containing '{text}' was not found.");
        }

        button.Click();
    }
}
