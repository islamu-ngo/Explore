// ABOUTME: bUnit coverage for the extracted AppSideNav shell navigation content.
// ABOUTME: Protects legacy MainLayout drawer links before dock host migration.

using Explore.Blazor.Client.Components.Shell;
using Explore.Blazor.Client.Services.Docking;
using MudBlazor;

namespace Explore.Blazor.Client.Tests.Components.Shell;

public sealed class AppSideNavTests : IDisposable
{
    private readonly BlazorTestContext _ctx = new();
    private readonly IPublicExperienceService _publicExperienceService;
    private readonly TenantNavLinksState _tenantNavLinksState;

    public AppSideNavTests()
    {
        _publicExperienceService = Substitute.For<IPublicExperienceService>();
        _publicExperienceService.GetCachedSettingsAsync().Returns(new PublicExperienceSettingsModel());
        _ctx.Services.AddSingleton(_publicExperienceService);

        _tenantNavLinksState = new TenantNavLinksState();
        _ctx.Services.AddSingleton(_tenantNavLinksState);
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task Render_ShowsCoreNavigationLinks()
    {
        var cut = _ctx.RenderMudComponent<AppSideNav>();

        await Assert.That(cut.Markup).Contains("Advanced Search");
        await Assert.That(cut.Markup).Contains("Recently Added");
        await Assert.That(cut.Markup).Contains("Random");
        await Assert.That(cut.Markup).Contains("About Us");
        await Assert.That(cut.Markup).Contains("Contact");
        await Assert.That(cut.Find("[data-testid='app-side-nav']").GetAttribute("aria-label")).IsEqualTo("Sidebar navigation");
    }

    [Test]
    public async Task Render_WithBrandName_ShowsBrandLabel()
    {
        _publicExperienceService.GetCachedSettingsAsync().Returns(new PublicExperienceSettingsModel
        {
            BrandDisplayName = "Community Hub"
        });

        var cut = _ctx.RenderMudComponent<AppSideNav>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Community Hub"))
                throw new InvalidOperationException("Expected 'Community Hub' label");
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_WhenCommunityGuidelinesDisabled_HidesCommunityGuidelinesLink()
    {
        _publicExperienceService.GetCachedSettingsAsync().Returns(new PublicExperienceSettingsModel
        {
            AllowUserSubmittedEvents = false,
            AllowOrganizationSubmittedEvents = false,
            AllowGroupSubmittedEvents = false
        });

        var cut = _ctx.RenderMudComponent<AppSideNav>();

        cut.WaitForAssertion(() =>
        {
            if (cut.Markup.Contains("Community Guidelines"))
                throw new InvalidOperationException("Expected 'Community Guidelines' to be hidden");
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_WithTenantLinks_OrdersLinksAndPreservesExternalAttributes()
    {
        var tenantLinks = new List<TenantNavigationLinkDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Label = "Second Link",
                Url = "/second",
                Order = 20
            },
            new()
            {
                Id = Guid.NewGuid(),
                Label = "First Link",
                Url = "https://example.test/first",
                Order = 10,
                OpenInNewTab = true
            }
        };

        var cut = _ctx.RenderMudComponent<DynamicComponent>(parameters => parameters
            .Add(component => component.Type, typeof(AppSideNav))
            .Add(component => component.Parameters, new Dictionary<string, object>
            {
                ["TenantLinks"] = tenantLinks
            }));
        
        var firstIndex = cut.Markup.IndexOf("First Link", StringComparison.Ordinal);
        var secondIndex = cut.Markup.IndexOf("Second Link", StringComparison.Ordinal);

        await Assert.That(cut.Markup).Contains("Quick Links");
        await Assert.That(firstIndex).IsLessThan(secondIndex);
        await Assert.That(cut.Markup).Contains("target=\"_blank\"");
        await Assert.That(cut.Markup).Contains("rel=\"noopener noreferrer\"");
    }

    [Test]
    public async Task Render_WithOrganizationCentricShell_UsesCatalogLabelAndHidesDiscoveryShortcuts()
    {
        _publicExperienceService.GetCachedShellAsync().Returns(new PublicExperienceShellModel
        {
            Mode = "OrganizationCentric",
            EventCatalog = new PublicExperienceEventCatalogModel
            {
                Label = "Programs",
                Url = "/events?ActorId=11111111-1111-1111-1111-111111111111"
            },
            PrimaryOrganization = new PublicExperiencePrimaryOrganizationModel
            {
                State = "Available"
            }
        });

        var cut = _ctx.RenderMudComponent<AppSideNav>();

        cut.WaitForAssertion(() =>
        {
            if (!cut.Markup.Contains("Programs", StringComparison.Ordinal))
                throw new InvalidOperationException("Expected catalog label");
            if (cut.Markup.Contains("Advanced Search", StringComparison.Ordinal)
                || cut.Markup.Contains("Recently Added", StringComparison.Ordinal)
                || cut.Markup.Contains("Random", StringComparison.Ordinal))
                throw new InvalidOperationException("Expected discovery shortcuts to be hidden");
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task Render_WithCloseCallbackInDockedMode_HidesCloseButton()
    {
        var cut = _ctx.RenderMudComponent<CascadingValue<DockPanelEntry>>(parameters => parameters
            .Add(component => component.Value, CreateDockPanelEntry(DockMode.Docked))
            .AddChildContent<AppSideNav>(childParameters => childParameters
                .Add(component => component.OnCloseRequested, EventCallback.Factory.Create(this, () => { }))));

        await Assert.That(cut.FindAll("[aria-label='Close sidebar navigation']")).IsEmpty();
    }

    [Test]
    public async Task Render_WithCloseCallbackInOverlayMode_ShowsAccessibleCloseButtonAndInvokesCallback()
    {
        var closed = false;
        var cut = _ctx.RenderMudComponent<CascadingValue<DockPanelEntry>>(parameters => parameters
            .Add(component => component.Value, CreateDockPanelEntry(DockMode.Temporary))
            .AddChildContent<AppSideNav>(childParameters => childParameters
                .Add(component => component.BrandDisplayName, "Community Hub")
                .Add(component => component.BrandLogoUrl, "/brand.svg")
                .Add(component => component.OnCloseRequested, EventCallback.Factory.Create(this, () => closed = true))));

        var closeButton = cut.Find("[aria-label='Close sidebar navigation']");
        await closeButton.ClickAsync(new MouseEventArgs());

        await Assert.That(cut.Markup).Contains("Community Hub");
        await Assert.That(cut.Markup).Contains("/brand.svg");
        await Assert.That(closed).IsTrue();
    }

    private static DockPanelEntry CreateDockPanelEntry(DockMode mode)
    {
        var id = new DockPanelId("test-sidebar");
        return new DockPanelEntry(
            new DockPanelDescriptor(
                id,
                DockScope.Shell,
                DockSide.Start,
                DockMode.Docked,
                "Navigation",
                "Sidebar navigation",
                280,
                240,
                360,
                0,
                false,
                true,
                true),
            _ => { },
            new DockPanelState(id, true, mode, 280, 0, true));
    }
}
