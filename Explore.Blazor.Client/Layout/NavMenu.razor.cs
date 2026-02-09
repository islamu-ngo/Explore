using System.Security.Claims;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;

namespace Explore.Blazor.Client.Layout;

public partial class NavMenu
{
    [Inject]
    protected NavigationManager Nav { get; set; } = null!;

    [Inject]
    protected AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    [Inject]
    protected IUserService UserService { get; set; } = null!;

    [Inject]
    protected IPublicExperienceService PublicExperienceService { get; set; } = null!;

    [Parameter]
    public EventCallback OnToggleTheme { get; set; }

    private bool _dropdownOpen = false;
    private UserDto? _currentUser;
    private bool _userLoaded = false;
    private string _brandDisplayName = "ISLAMU Explore";
    private string _brandLogoUrl = string.Empty;
    public string SearchQuery { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        await LoadPublicExperienceAsync();
        await LoadCurrentUserAsync();
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

    private bool IsAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") ||
               user.HasClaim(c => c.Type == "role" && c.Value == "Admin") ||
               user.HasClaim(c => c.Type == "roles" && c.Value.Contains("Admin"));
    }
}
