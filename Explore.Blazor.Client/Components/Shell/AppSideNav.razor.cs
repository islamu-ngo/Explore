using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Docking;
using Microsoft.AspNetCore.Components;

namespace Explore.Blazor.Client.Components.Shell;

public partial class AppSideNav : ComponentBase, IDisposable
{
    [Inject] protected IPublicExperienceService PublicExperienceService { get; set; } = null!;
    [Inject] protected TenantNavLinksState TenantNavLinksState { get; set; } = null!;

    [Parameter] public string AriaLabel { get; set; } = "Sidebar navigation";
    [Parameter] public string DataTestId { get; set; } = "app-side-nav";
    [Parameter] public EventCallback OnCloseRequested { get; set; }
    [CascadingParameter] public DockPanelEntry? DockPanelEntry { get; set; }

    [Parameter]
    public string? BrandDisplayName
    {
        get => _parameterBrandDisplayName;
        set => _parameterBrandDisplayName = value;
    }

    [Parameter]
    public string? BrandLogoUrl
    {
        get => _parameterBrandLogoUrl;
        set => _parameterBrandLogoUrl = value;
    }

    [Parameter]
    public bool? ShowCommunityGuidelinesLink
    {
        get => _parameterShowCommunityGuidelinesLink;
        set => _parameterShowCommunityGuidelinesLink = value;
    }

    [Parameter]
    public IReadOnlyList<TenantNavigationLinkDto>? TenantLinks
    {
        get => _parameterTenantLinks;
        set => _parameterTenantLinks = value;
    }

    private string? _brandDisplayName;
    private string? _brandLogoUrl;
    private bool _showCommunityGuidelinesLink = true;
    private PublicExperienceShellModel? _shell;
    private string? _parameterBrandDisplayName;
    private string? _parameterBrandLogoUrl;
    private bool? _parameterShowCommunityGuidelinesLink;
    private IReadOnlyList<TenantNavigationLinkDto>? _parameterTenantLinks;

    private string? ResolvedBrandDisplayName => _parameterBrandDisplayName ?? _brandDisplayName;
    private string? ResolvedBrandLogoUrl => _parameterBrandLogoUrl ?? _brandLogoUrl;
    private bool ResolvedShowCommunityGuidelinesLink => _parameterShowCommunityGuidelinesLink ?? _showCommunityGuidelinesLink;
    private IReadOnlyList<TenantNavigationLinkDto> ResolvedTenantLinks => _parameterTenantLinks
        ?? BuildTenantLinks(_shell?.Navigation.Links)
        ?? TenantNavLinksState.Links;
    private string ResolvedOverlayBrandAriaLabel => string.IsNullOrWhiteSpace(ResolvedBrandDisplayName)
        ? "Home"
        : ResolvedBrandDisplayName;
    private string ResolvedEventCatalogLabel => string.IsNullOrWhiteSpace(_shell?.EventCatalog.Label)
        ? "Events"
        : _shell.EventCatalog.Label;
    private string ResolvedEventCatalogUrl => string.IsNullOrWhiteSpace(_shell?.EventCatalog.Url)
        ? "/events"
        : _shell.EventCatalog.Url;
    private bool IsOrganizationCentric => _shell?.Mode.Equals("OrganizationCentric", StringComparison.OrdinalIgnoreCase) == true;
    private bool HasCloseAction => OnCloseRequested.HasDelegate;
    private bool ShouldRenderOverlayHeader => HasCloseAction
        && DockPanelEntry?.State.Mode is DockMode.Overlay or DockMode.Temporary or DockMode.Inspector;

    protected override async Task OnInitializedAsync()
    {
        TenantNavLinksState.OnChange += StateHasChanged;

        try
        {
            var shellTask = PublicExperienceService.GetCachedShellAsync();
            _shell = shellTask is null ? null : await shellTask;
            if (!string.IsNullOrWhiteSpace(_shell?.Home.BrandDisplayName))
            {
                _brandDisplayName = _shell.Home.BrandDisplayName;
            }

            if (!string.IsNullOrWhiteSpace(_shell?.Home.BrandLogoUrl))
            {
                _brandLogoUrl = _shell.Home.BrandLogoUrl;
            }

            var settingsTask = PublicExperienceService.GetCachedSettingsAsync();
            var settings = settingsTask is null ? null : await settingsTask;
            if (settings is null)
            {
                return;
            }

            _showCommunityGuidelinesLink = settings.AllowUserSubmittedEvents
                || settings.AllowOrganizationSubmittedEvents
                || settings.AllowGroupSubmittedEvents;
            _brandDisplayName = string.IsNullOrWhiteSpace(_brandDisplayName)
                ? settings.BrandDisplayName
                : _brandDisplayName;
            _brandLogoUrl = string.IsNullOrWhiteSpace(_brandLogoUrl)
                ? settings.BrandLogoUrl
                : _brandLogoUrl;
        }
        catch
        {
            // Fallback to defaults
        }
    }

    private static IReadOnlyList<TenantNavigationLinkDto>? BuildTenantLinks(IReadOnlyList<PublicExperienceNavigationLinkModel>? links)
    {
        if (links is null || links.Count == 0)
        {
            return null;
        }

        return links
            .Where(link => !string.IsNullOrWhiteSpace(link.Label) && !string.IsNullOrWhiteSpace(link.Url))
            .OrderBy(link => link.SortOrder)
            .ThenBy(link => link.Label, StringComparer.OrdinalIgnoreCase)
            .Select(link => new TenantNavigationLinkDto
            {
                Id = Guid.NewGuid(),
                Label = link.Label,
                Url = link.Url,
                Order = link.SortOrder,
                OpenInNewTab = false
            })
            .ToList();
    }

    public void Dispose()
    {
        TenantNavLinksState.OnChange -= StateHasChanged;
    }
}
