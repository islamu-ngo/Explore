using System.Security.Claims;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Explore.Blazor.Client.Layout;

public partial class NavMenu : IDisposable
{
    [Inject]
    protected NavigationManager Nav { get; set; } = null!;

    [Inject]
    protected AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    [Inject]
    protected IUserService UserService { get; set; } = null!;

    [Inject]
    protected IPublicExperienceService PublicExperienceService { get; set; } = null!;

    [Inject]
    protected IInstanceOnboardingService InstanceOnboardingService { get; set; } = null!;

    [Inject]
    protected ITenantNavigationService TenantNavigationService { get; set; } = null!;

    [Inject]
    protected TenantNavLinksState TenantNavLinksState { get; set; } = null!;

    [Inject]
    protected IEventCreationEligibilityService EventCreationEligibilityService { get; set; } = null!;

    [Inject]
    protected IOrganizationService OrganizationService { get; set; } = null!;

    [Inject]
    protected IGroupService GroupService { get; set; } = null!;

    [Inject]
    protected IDialogService DialogService { get; set; } = null!;

    [Inject]
    protected SidebarState SidebarState { get; set; } = null!;

    [Inject]
    private IAccessibilityFocusService AccessibilityFocusService { get; set; } = default!;

    [Parameter]
    public EventCallback OnToggleTheme { get; set; }

    private bool _dropdownOpen = false;
    private UserDto? _currentUser;
    private bool _userLoaded = false;
    private string _brandDisplayName = string.Empty;
    private string _brandLogoUrl = string.Empty;
    public string SearchQuery { get; set; } = "";
    private MudTextField<string> _searchField = null!;
    private IReadOnlyList<TenantNavigationLinkDto> _navigationLinks = [];
    private EventCreationEligibility _eventCreationEligibility = EventCreationEligibility.NotEligible;
    private bool _isSingleTenantMode = true;
    private bool _showAddEventForAnonymous;
    private ICollection<OrganizationListDto> _userOrganizations = new List<OrganizationListDto>();
    private ICollection<GroupPublisherListDto> _userGroups = new List<GroupPublisherListDto>();
    private bool _orgSubmenuOpen;
    private bool _groupSubmenuOpen;

    protected override async Task OnInitializedAsync()
    {
        SidebarState.OnChange += StateHasChanged;
        TenantNavLinksState.OnChange += StateHasChanged;
        await LoadPublicExperienceAsync();
        await LoadCurrentUserAsync();
        await LoadNavigationLinksAsync();
        await LoadEventCreationEligibilityAsync();
        await LoadDeploymentModeAsync();
        await LoadUserOrganizationsAsync();
        await LoadUserGroupsAsync();
    }

    private void HandleSearchKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                Nav.NavigateTo($"/events?q={Uri.EscapeDataString(SearchQuery)}");
            }
            else
            {
                Nav.NavigateTo("/events");
            }
        }
    }

    private async Task LoadPublicExperienceAsync()
    {
        var settings = await PublicExperienceService.GetSettingsAsync();
        if (settings == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.BrandDisplayName))
        {
            _brandDisplayName = settings.BrandDisplayName;
        }

        _brandLogoUrl = settings.BrandLogoUrl ?? string.Empty;

        // Show "Add Event" button to anonymous visitors when at least one
        // submission type is enabled, prompting them to log in on click.
        _showAddEventForAnonymous = settings.AllowUserSubmittedEvents
            || settings.AllowOrganizationSubmittedEvents
            || settings.AllowGroupSubmittedEvents;
    }

    private async Task LoadCurrentUserAsync()
    {
        if (_userLoaded) return;

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                _currentUser = await UserService.GetCurrentUserAsync();
                _userLoaded = true;
            }
        }
        catch
        {
            // Silently fail - will fall back to initials
        }
    }

    private void ToggleDropdown()
    {
        _dropdownOpen = !_dropdownOpen;
        if (!_dropdownOpen)
        {
            _orgSubmenuOpen = false;
            _groupSubmenuOpen = false;
        }
    }

    private void CloseDropdown()
    {
        _dropdownOpen = false;
        _orgSubmenuOpen = false;
        _groupSubmenuOpen = false;
    }

    private void ToggleOrgSubmenu()
    {
        _orgSubmenuOpen = !_orgSubmenuOpen;
        if (_orgSubmenuOpen) _groupSubmenuOpen = false;
    }

    private void ToggleGroupSubmenu()
    {
        _groupSubmenuOpen = !_groupSubmenuOpen;
        if (_groupSubmenuOpen) _orgSubmenuOpen = false;
    }

    private string GetInitials(string? name)
    {
        return DisplayHelper.GetInitials(name);
    }

    // DB-first authority: admin claims are added by AdminClaimsTransformation
    // and serialized to WASM via AddAuthenticationStateSerialization.
    // Claim types match Explore.Application.Authorization.AdminClaimTypes constants.

    private static bool HasAnyAdminAuthority(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return false;

        return user.HasClaim(c => c.Type == "explore:admin:instance")
               || user.HasClaim(c => c.Type == "explore:admin:tenant")
               || user.HasClaim(c => c.Type == "explore:admin:organization");
    }

    private static bool IsInstanceAdmin(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true
               && user.HasClaim(c => c.Type == "explore:admin:instance");
    }

    private static bool IsTenantAdmin(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true
               && user.HasClaim(c => c.Type == "explore:admin:tenant");
    }

    private static IEnumerable<string> GetAdminOrganizationIds(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return [];

        return user.FindAll("explore:admin:organization")
                   .Select(c => c.Value)
                   .Where(v => Guid.TryParse(v, out _));
    }

    private async Task LoadNavigationLinksAsync()
    {
        await TenantNavLinksState.EnsureLoadedAsync(TenantNavigationService);
        _navigationLinks = TenantNavLinksState.Links;
    }

    private async Task LoadEventCreationEligibilityAsync()
    {
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated != true)
            {
                return;
            }

            _eventCreationEligibility = await EventCreationEligibilityService.GetEligibilityAsync();
        }
        catch
        {
            // Silently fail - button simply won't appear
        }
    }

    private async Task LoadDeploymentModeAsync()
    {
        try
        {
            var status = await InstanceOnboardingService.GetStatusAsync();
            _isSingleTenantMode = status == null
                || string.IsNullOrWhiteSpace(status.SelectedDeploymentMode)
                || string.Equals(status.SelectedDeploymentMode, "SingleTenant", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            _isSingleTenantMode = true;
        }
    }

    private async Task LoadUserOrganizationsAsync()
    {
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated != true) return;
            _userOrganizations = await OrganizationService.GetMyOrganizationsAsync();
        }
        catch
        {
            _userOrganizations = new List<OrganizationListDto>();
        }
    }

    private async Task LoadUserGroupsAsync()
    {
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated != true) return;
            _userGroups = await GroupService.GetMyGroupsAsync();
        }
        catch
        {
            _userGroups = new List<GroupPublisherListDto>();
        }
    }

    private async Task OpenLoginPrompt(string? returnUrl)
    {
        returnUrl ??= new Uri(Nav.Uri).PathAndQuery;
        await AccessibilityFocusService.SaveFocusAsync();
        await LoginPromptDialog.ShowAsync(DialogService, returnUrl);
        await AccessibilityFocusService.RestoreFocusAsync();
    }

    private async Task FocusSearchAsync()
    {
        await _searchField.FocusAsync();
    }

    public void Dispose()
    {
        SidebarState.OnChange -= StateHasChanged;
        TenantNavLinksState.OnChange -= StateHasChanged;
        GC.SuppressFinalize(this);
    }
}
