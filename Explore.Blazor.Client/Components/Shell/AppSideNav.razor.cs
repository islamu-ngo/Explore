using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;

namespace Explore.Blazor.Client.Components.Shell;

public partial class AppSideNav : ComponentBase, IDisposable
{
    [Inject] protected IPublicExperienceService PublicExperienceService { get; set; } = null!;
    [Inject] protected TenantNavLinksState TenantNavLinksState { get; set; } = null!;

    [Parameter] public string AriaLabel { get; set; } = "Sidebar navigation";
    [Parameter] public string DataTestId { get; set; } = "app-side-nav";
    [Parameter]
    public string? BrandDisplayName
    {
        get => _parameterBrandDisplayName;
        set => _parameterBrandDisplayName = value;
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
    private bool _showCommunityGuidelinesLink = true;
    private string? _parameterBrandDisplayName;
    private bool? _parameterShowCommunityGuidelinesLink;
    private IReadOnlyList<TenantNavigationLinkDto>? _parameterTenantLinks;

    private string? ResolvedBrandDisplayName => _parameterBrandDisplayName ?? _brandDisplayName;
    private bool ResolvedShowCommunityGuidelinesLink => _parameterShowCommunityGuidelinesLink ?? _showCommunityGuidelinesLink;
    private IReadOnlyList<TenantNavigationLinkDto> ResolvedTenantLinks => _parameterTenantLinks ?? TenantNavLinksState.Links;

    protected override async Task OnInitializedAsync()
    {
        TenantNavLinksState.OnChange += StateHasChanged;

        try
        {
            var settings = await PublicExperienceService.GetCachedSettingsAsync();
            if (settings is null)
            {
                return;
            }

            _showCommunityGuidelinesLink = settings.AllowUserSubmittedEvents
                || settings.AllowOrganizationSubmittedEvents
                || settings.AllowGroupSubmittedEvents;
            _brandDisplayName = settings.BrandDisplayName;
        }
        catch
        {
            // Fallback to defaults
        }
    }

    public void Dispose()
    {
        TenantNavLinksState.OnChange -= StateHasChanged;
    }
}
