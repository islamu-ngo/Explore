using System.Security.Claims;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;

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
    protected ITenantNavigationService TenantNavigationService { get; set; } = null!;

    [Inject]
    protected IEventCreationEligibilityService EventCreationEligibilityService { get; set; } = null!;

    [Inject]
    protected SidebarState SidebarState { get; set; } = null!;

    [Parameter]
    public EventCallback OnToggleTheme { get; set; }

    private bool _dropdownOpen = false;
    private UserDto? _currentUser;
    private bool _userLoaded = false;
    private string _brandDisplayName = "ISLAMU Explore";
    private string _brandLogoUrl = string.Empty;
    public string SearchQuery { get; set; } = "";
    private ICollection<TenantNavigationLinkDto> _navigationLinks = new List<TenantNavigationLinkDto>();
    private EventCreationEligibility _eventCreationEligibility = EventCreationEligibility.NotEligible;

    protected override async Task OnInitializedAsync()
    {
        SidebarState.OnChange += StateHasChanged;
        await LoadPublicExperienceAsync();
        await LoadCurrentUserAsync();
        await LoadNavigationLinksAsync();
        await LoadEventCreationEligibilityAsync();
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
    }

    private void CloseDropdown()
    {
        _dropdownOpen = false;
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
        try
        {
            _navigationLinks = await TenantNavigationService.GetNavigationLinksAsync();
        }
        catch
        {
            // Silently fail - navigation links are optional
            _navigationLinks = new List<TenantNavigationLinkDto>();
        }
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

    public void Dispose()
    {
        SidebarState.OnChange -= StateHasChanged;
    }
}
