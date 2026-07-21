// ABOUTME: bUnit coverage for the AppWorkspaceRail permanent shell chrome.
// ABOUTME: Verifies workspace link rendering, auth filtering, active state, and Settings pinning.

using Explore.Blazor.Client.Components.Shell;
using Explore.Blazor.Client.Services.Shell;

namespace Explore.Blazor.Client.Tests.Components.Shell;

public sealed class AppWorkspaceRailTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();

    public AppWorkspaceRailTests()
    {
        _ctx.Services.AddScoped<IWorkspaceRegistry, WorkspaceRegistry>();
        _ctx.Services.AddScoped<WorkspaceRouteClassifier>();
        _ctx.Services.AddScoped<UiShellState>();
    }

    public void Dispose() => _ctx.Dispose();

    [Test]
    public async Task RenderHasApplicationWorkspacesNavLandmark()
    {
        _ctx.SetAnonymousUser();

        var cut = _ctx.Render<AppWorkspaceRail>();

        await Assert.That(cut.Find(".app-workspace-rail").GetAttribute("aria-label"))
            .IsEqualTo("Application workspaces");
    }

    [Test]
    public async Task RenderAnonymousUserShowsOnlyAnonymousWorkspaces()
    {
        _ctx.SetAnonymousUser();

        var cut = _ctx.Render<AppWorkspaceRail>();

        var links = cut.FindAll(".app-workspace-rail__link");
        await Assert.That(links.Count).IsEqualTo(1);
        await Assert.That(links[0].GetAttribute("href")).IsEqualTo("/");
    }

    [Test]
    public async Task RenderAuthenticatedUserShowsAllWorkspaces()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.Render<AppWorkspaceRail>();

        var links = cut.FindAll(".app-workspace-rail__link");
        await Assert.That(links.Count).IsEqualTo(2);
    }

    [Test]
    public async Task RenderSettingsIsPinnedToBlockEnd()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.Render<AppWorkspaceRail>();

        var links = cut.FindAll(".app-workspace-rail__link");
        await Assert.That(links[^1].GetAttribute("href")).IsEqualTo("/settings");
    }

    [Test]
    public async Task RenderActiveWorkspaceGetsAriaCurrentPage()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/settings");

        var cut = _ctx.Render<AppWorkspaceRail>();

        var activeLink = cut.Find(".app-workspace-rail__link--active");
        await Assert.That(activeLink.GetAttribute("aria-current")).IsEqualTo("page");
        await Assert.That(activeLink.GetAttribute("href")).IsEqualTo("/settings");
    }

    [Test]
    public async Task RenderEventsRouteEventsLinkIsActive()
    {
        _ctx.SetAnonymousUser();
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/events");

        var cut = _ctx.Render<AppWorkspaceRail>();

        var activeLink = cut.Find(".app-workspace-rail__link--active");
        await Assert.That(activeLink.GetAttribute("href")).IsEqualTo("/events");
        await Assert.That(activeLink.GetAttribute("aria-current")).IsEqualTo("page");
    }

    [Test]
    public async Task RenderLinksRestoreLastWorkspaceRoutesWithQueries()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");
        var navigation = _ctx.Services.GetRequiredService<NavigationManager>();
        _ = _ctx.Services.GetRequiredService<UiShellState>();
        navigation.NavigateTo("/events?q=iftar&format=online");
        navigation.NavigateTo("/settings?section=appearance");

        var cut = _ctx.Render<AppWorkspaceRail>();
        var links = cut.FindAll(".app-workspace-rail__link");

        await Assert.That(links[0].GetAttribute("href")).IsEqualTo("/events?q=iftar&format=online");
        await Assert.That(links[1].GetAttribute("href")).IsEqualTo("/settings?section=appearance");
    }

    [Test]
    public async Task RenderEachLinkHasVisibleLabelAndAriaLabel()
    {
        _ctx.SetAuthenticatedUser(Guid.NewGuid(), "Test User");

        var cut = _ctx.Render<AppWorkspaceRail>();

        foreach (var link in cut.FindAll(".app-workspace-rail__link"))
        {
            await Assert.That(link.GetAttribute("aria-label")).IsNotNull();
            var label = link.QuerySelector(".app-workspace-rail__label");
            await Assert.That(label).IsNotNull();
            await Assert.That(label!.TextContent).IsNotNull().And.IsNotEmpty();
        }
    }
}
