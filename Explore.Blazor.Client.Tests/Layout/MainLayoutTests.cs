// ABOUTME: Tests for MainLayout covering chrome visibility, user sync, accessibility landmarks, and settings-driven UI.
// ABOUTME: Validates WCAG 2.4.1 skip link, ARIA live regions, sidebar brand name, and community guidelines conditional.

using Explore.Blazor.Client.Layout;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Layout;

/// <summary>
/// Behavioral tests for MainLayout covering:
/// - WCAG 2.4.1 accessibility landmarks (skip link, ARIA live regions, main content landmark)
/// - Chrome visibility toggling on setup/onboarding/startup routes
/// - User sync on first authenticated render
/// - Settings-driven sidebar content (brand name, community guidelines)
/// - Theme initialization
/// </summary>
public class MainLayoutTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly IUserService _userService;
    private readonly IPublicExperienceService _publicExperienceService;
    private readonly IAppearanceThemeService _appearanceThemeService;

    public MainLayoutTests()
    {
        _ctx = new BlazorTestContext();

        // Explicit state registration for assertion control (not via AddShellStateMocks)
        _ctx.Services.AddScoped<AiAssistantState>();
        _ctx.Services.AddScoped<TenantNavLinksState>();

        // Bulk NavMenu deps (IUserService, IPublicExperienceService, SidebarState, etc.)
        NavMenuTestServices.Register(_ctx);

        // Override IUserService for SyncUserAsync assertions (last AddSingleton wins)
        _userService = Substitute.For<IUserService>();
        _ctx.Services.AddSingleton(_userService);

        // Override IPublicExperienceService for settings assertions
        _publicExperienceService = Substitute.For<IPublicExperienceService>();
        _publicExperienceService.ResolveHomeRoute(Arg.Any<PublicExperienceSettingsModel?>()).Returns("/events");
        _ctx.Services.AddSingleton(_publicExperienceService);

        // Theme service — CreateTheme returns a valid MudTheme to avoid NRE
        _appearanceThemeService = Substitute.For<IAppearanceThemeService>();
        _appearanceThemeService.CreateTheme(Arg.Any<string>()).Returns(new MudTheme());
        _ctx.Services.AddSingleton(_appearanceThemeService);

        // AnalyticsInitializer child deps (GetSettingsAsync returns null → early-return, no JS calls)
        _ctx.Services.AddSingleton(Substitute.For<IAnalyticsInterop>());
        _ctx.Services.AddSingleton(Substitute.For<ICookieConsentInterop>());
        _ctx.Services.AddSingleton(new CookieConsentStateService());
    }

    private IRenderedComponent<MainLayout> RenderLayout()
    {
        return _ctx.Render<MainLayout>(p =>
            p.Add(l => l.Body, (RenderFragment)(b => b.AddContent(0, "Test body content"))));
    }

    public void Dispose() => _ctx.Dispose();

    #region Accessibility

    [Test]
    public async Task Render_HasSkipToContentLink_ForKeyboardNavigation()
    {
        var cut = RenderLayout();

        var skipLink = cut.Find("a.skip-link");

        await Assert.That(skipLink.GetAttribute("href")).IsEqualTo("#main-content");
        await Assert.That(skipLink.TextContent).Contains("Skip to main content");
    }

    [Test]
    public async Task Render_HasMainContentLandmark_WithNegativeTabIndex()
    {
        var cut = RenderLayout();

        var main = cut.Find("main#main-content");

        await Assert.That(main.GetAttribute("tabindex")).IsEqualTo("-1");
    }

    [Test]
    public async Task Render_HasAriaLiveRegions_ForDynamicAnnouncements()
    {
        var cut = RenderLayout();

        var polite = cut.Find("#aria-live-polite");
        await Assert.That(polite.GetAttribute("aria-live")).IsEqualTo("polite");
        await Assert.That(polite.GetAttribute("aria-atomic")).IsEqualTo("true");

        var assertive = cut.Find("#aria-live-assertive");
        await Assert.That(assertive.GetAttribute("aria-live")).IsEqualTo("assertive");
        await Assert.That(assertive.GetAttribute("aria-atomic")).IsEqualTo("true");
    }

    #endregion

    #region Chrome Visibility

    [Test]
    public async Task Render_OnDefaultRoute_ShowsHeaderAndSidebar()
    {
        var cut = RenderLayout();

        var root = cut.Find(".main-layout-root");
        await Assert.That(root.ClassList.Contains("main-layout-root--hide-chrome")).IsFalse();

        // Header present on default route
        var headers = cut.FindAll("header.main-layout__header");
        await Assert.That(headers.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task NavigateToSetup_HidesChrome()
    {
        var cut = RenderLayout();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        nav.NavigateTo("/setup");

        cut.WaitForAssertion(() =>
        {
            var root = cut.Find(".main-layout-root");
            if (!root.ClassList.Contains("main-layout-root--hide-chrome"))
                throw new InvalidOperationException("Expected hide-chrome class on /setup route");
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task NavigateToOnboarding_HidesChrome()
    {
        var cut = RenderLayout();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        nav.NavigateTo("/onboarding/step-1");

        cut.WaitForAssertion(() =>
        {
            var root = cut.Find(".main-layout-root");
            if (!root.ClassList.Contains("main-layout-root--hide-chrome"))
                throw new InvalidOperationException("Expected hide-chrome class on /onboarding route");
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task NavigateToStartup_HidesChrome()
    {
        var cut = RenderLayout();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        nav.NavigateTo("/startup");

        cut.WaitForAssertion(() =>
        {
            var root = cut.Find(".main-layout-root");
            if (!root.ClassList.Contains("main-layout-root--hide-chrome"))
                throw new InvalidOperationException("Expected hide-chrome class on /startup route");
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task NavigateFromHiddenRoute_ToNormalRoute_RestoresChrome()
    {
        var cut = RenderLayout();
        var nav = _ctx.Services.GetRequiredService<NavigationManager>();

        // Navigate to setup — chrome hides
        nav.NavigateTo("/setup");
        cut.WaitForAssertion(() =>
        {
            var root = cut.Find(".main-layout-root");
            if (!root.ClassList.Contains("main-layout-root--hide-chrome"))
                throw new InvalidOperationException("Expected hide-chrome class on /setup");
        });

        // Navigate back to normal route — chrome restores
        nav.NavigateTo("/events");
        cut.WaitForAssertion(() =>
        {
            var root = cut.Find(".main-layout-root");
            if (root.ClassList.Contains("main-layout-root--hide-chrome"))
                throw new InvalidOperationException("Expected chrome restored on /events");
        });

        await Task.CompletedTask;
    }

    #endregion

    #region User Sync

    [Test]
    public async Task OnFirstRender_AuthenticatedUser_CallsSyncUser()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = RenderLayout();

        cut.WaitForAssertion(() =>
            _userService.Received(1).SyncUserAsync());

        await Task.CompletedTask;
    }

    [Test]
    public async Task OnFirstRender_AnonymousUser_DoesNotCallSyncUser()
    {
        _ctx.SetAnonymousUser();

        var cut = RenderLayout();

        // Wait for async lifecycle to complete (settings load proves OnAfterRenderAsync ran)
        cut.WaitForAssertion(() =>
            _publicExperienceService.Received().GetCachedSettingsAsync());

        // Anonymous user should not trigger user sync
        _userService.DidNotReceive().SyncUserAsync();

        await Task.CompletedTask;
    }

    #endregion

    #region Settings-Driven UI

    [Test]
    public async Task OnFirstRender_WithBrandName_DisplaysBrandInSidebar()
    {
        PublicExperienceSettingsModel settings = new PublicExperienceSettingsBuilder()
            .WithBranding("My Test Brand");
        _publicExperienceService.GetCachedSettingsAsync().Returns(settings);

        var cut = RenderLayout();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("My Test Brand"))
                throw new InvalidOperationException("Expected brand name 'My Test Brand' in sidebar");
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task OnFirstRender_NoSubmissionPolicies_HidesCommunityGuidelinesLink()
    {
        // Model defaults AllowUser/Org/GroupSubmittedEvents to true — must explicitly disable
        PublicExperienceSettingsModel settings = new PublicExperienceSettingsBuilder()
            .WithUserSubmittedEvents(false)
            .WithOrganizationSubmittedEvents(false)
            .WithGroupSubmittedEvents(false);
        _publicExperienceService.GetCachedSettingsAsync().Returns(settings);

        var cut = RenderLayout();

        // Wait for settings to load, re-render, and community guidelines link to disappear
        cut.WaitForAssertion(() =>
        {
            var links = cut.FindAll("a[href='/community-guidelines']");
            if (links.Count > 0)
                throw new InvalidOperationException("Expected community guidelines link to be hidden");
        });

        await Task.CompletedTask;
    }

    #endregion

    #region Theme

    [Test]
    public async Task Render_CallsCreateTheme_DuringInitialization()
    {
        var cut = RenderLayout();

        _appearanceThemeService.Received(1).CreateTheme(Arg.Any<string>());

        await Task.CompletedTask;
    }

    #endregion

    #region Content

    [Test]
    public async Task Render_DisplaysProvidedBodyContent()
    {
        var cut = RenderLayout();

        await Assert.That(cut.Markup).Contains("Test body content");
    }

    #endregion
}
