using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using System.Security.Claims;

namespace Explore.Blazor.Client.Layout;

public partial class NavMenu
{
    [Inject]
    protected NavigationManager Nav { get; set; } = null!;

    [Inject]
    protected AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

    [Inject]
    protected IUserService UserService { get; set; } = null!;

    [Parameter]
    public EventCallback OnToggleTheme { get; set; }

    private bool _dropdownOpen = false;
    private UserDto? _currentUser;
    private bool _userLoaded = false;
    public string SearchQuery { get; set; } = "";

    protected override async Task OnInitializedAsync()
    {
        await LoadCurrentUserAsync();
    }

    private void HandleSearchKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                Nav.NavigateTo($"/?q={Uri.EscapeDataString(SearchQuery)}");
            }
            else
            {
                Nav.NavigateTo("/");
            }
        }
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
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[1][0]}".ToUpper();
        return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }

    private bool IsAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") ||
               user.HasClaim(c => c.Type == "role" && c.Value == "Admin") ||
               user.HasClaim(c => c.Type == "roles" && c.Value.Contains("Admin"));
    }
}
