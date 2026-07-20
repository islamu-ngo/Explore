// ABOUTME: Code-behind for the top navigation shell and profile dropdown state.
// ABOUTME: Loads BFF/API-backed public experience and admin authority status for menu affordances.

using System.Security.Claims;
using System.Text.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Components.Shell;
using Explore.Blazor.Client.Contracts.Services;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Contracts.Services.Events;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Docking;
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
    protected IUserSettingsService UserSettingsService { get; set; } = null!;

    [Inject]
    protected IInstanceOnboardingService InstanceOnboardingService { get; set; } = null!;

    [Inject]
    protected ITenantOnboardingService TenantOnboardingService { get; set; } = null!;

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
    protected CurrentUserState CurrentUserState { get; set; } = null!;

    [Inject]
    private IAccessibilityFocusService AccessibilityFocusService { get; set; } = default!;

    [Inject]
    protected AiAssistantState AiAssistantState { get; set; } = null!;

    [Inject]
    protected DockLayoutState DockLayoutState { get; set; } = null!;

    private bool _dropdownOpen = false;
    private UserDto? _currentUser;
    private bool _userLoaded = false;
    private string _brandDisplayName = string.Empty;
    private string _brandLogoUrl = string.Empty;
    private string _eventCatalogLabel = "events";
    public string SearchQuery { get; set; } = "";
    private MudTextField<string> _searchField = null!;
    private IReadOnlyList<TenantNavigationLinkDto> _navigationLinks = [];
    private EventCreationEligibility _eventCreationEligibility = EventCreationEligibility.NotEligible;
    private bool _isSingleTenantMode = true;
    private bool _isCurrentUserInstanceAdmin;
    private bool _isCurrentUserTenantAdmin;
    private bool _showAddEventForAnonymous;
    private bool _languagePickerEnabled = true;
    private ICollection<OrganizationListDto> _userOrganizations = new List<OrganizationListDto>();
    private ICollection<GroupListDto> _userGroups = new List<GroupListDto>();
    private bool _orgSubmenuOpen;
    private bool _groupSubmenuOpen;
    private const string AiAssistantPreferencesCategory = "AiAssistantPreferences";
    private const string ShowAiAssistantNavbarButtonKey = "ai_assistant_preferences.show_navbar_button";

    protected override async Task OnInitializedAsync()
    {
        SidebarState.OnChange += StateHasChanged;
        AiAssistantState.OnChange += StateHasChanged;
        TenantNavLinksState.OnChange += StateHasChanged;
        DockLayoutState.Changed += OnDockLayoutChanged;
        CurrentUserState.OnChanged += OnCurrentUserChanged;
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
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var isAuthenticated = authState.User.Identity?.IsAuthenticated == true;
        var shellTask = PublicExperienceService.GetCachedShellAsync();
        var shell = shellTask is null ? null : await shellTask;
        if (shell is not null)
        {
            if (!string.IsNullOrWhiteSpace(shell.Home?.BrandDisplayName))
            {
                _brandDisplayName = shell.Home.BrandDisplayName;
            }

            _brandLogoUrl = shell.Home?.BrandLogoUrl ?? string.Empty;
            _eventCatalogLabel = string.IsNullOrWhiteSpace(shell.EventCatalog?.Label)
                ? _eventCatalogLabel
                : shell.EventCatalog.Label.ToLowerInvariant();
        }

        var settingsTask = PublicExperienceService.GetSettingsAsync();
        var settings = settingsTask is null ? null : await settingsTask;
        if (settings == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_brandDisplayName) && !string.IsNullOrWhiteSpace(settings.BrandDisplayName))
        {
            _brandDisplayName = settings.BrandDisplayName;
        }

        _brandLogoUrl = string.IsNullOrWhiteSpace(_brandLogoUrl)
            ? settings.BrandLogoUrl ?? string.Empty
            : _brandLogoUrl;
        AiAssistantState.SetPolicy(
            settings.IsAiAssistantEnabled == true,
            settings.IsAiAssistantAvailable == true,
            settings.AiAssistantAllowAnonymousAccess == true,
            isAuthenticated);
        _languagePickerEnabled = settings.ClientPickerEnabled ?? true;

        if (isAuthenticated)
        {
            await LoadAiAssistantPreferenceAsync();
        }

        // Show "Add Event" button to anonymous visitors when at least one
        // submission type is enabled, prompting them to log in on click.
        _showAddEventForAnonymous = settings.AllowUserSubmittedEvents == true
            || settings.AllowOrganizationSubmittedEvents == true
            || settings.AllowGroupSubmittedEvents == true;
    }

    private async Task LoadAiAssistantPreferenceAsync()
    {
        try
        {
            var response = await UserSettingsService.GetSettingsAsync(AiAssistantPreferencesCategory);
            var setting = response?.Settings?.FirstOrDefault(s => s.Key == ShowAiAssistantNavbarButtonKey);
            AiAssistantState.SetUserNavbarPreference(ParseBooleanSetting(setting?.Value, defaultValue: true));
        }
        catch
        {
            AiAssistantState.SetUserNavbarPreference(true);
        }
    }

    private static bool ParseBooleanSetting(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        try
        {
            return JsonSerializer.Deserialize<bool>(value);
        }
        catch (JsonException)
        {
            return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
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

    private async Task ToggleDropdown()
    {
        if (!_dropdownOpen)
        {
            await LoadDeploymentModeAsync();
        }

        _dropdownOpen = !_dropdownOpen;
        if (!_dropdownOpen)
        {
            _orgSubmenuOpen = false;
            _groupSubmenuOpen = false;
        }
    }

    private void ToggleSidebarPanel()
    {
        DockLayoutState.Toggle(ShellDockPanels.LeftNavId);
    }

    private void ToggleAiAssistantPanel()
    {
        AiAssistantState.Toggle();
        MirrorShellDockPanel(ShellDockPanels.AiAssistantId, AiAssistantState.IsAvailable && AiAssistantState.IsOpen);
    }

    private void MirrorShellDockPanel(DockPanelId id, bool shouldBeOpen)
    {
        var panel = DockLayoutState.GetPanel(id);
        if (panel is null || panel.State.IsOpen == shouldBeOpen)
        {
            return;
        }

        if (shouldBeOpen)
        {
            DockLayoutState.Open(id);
            return;
        }

        DockLayoutState.Close(id);
    }

    private void CloseDropdown()
    {
        _dropdownOpen = false;
        _orgSubmenuOpen = false;
        _groupSubmenuOpen = false;
    }

    private bool IsSidebarDockOpen => DockLayoutState.GetPanel(ShellDockPanels.LeftNavId)?.State.IsOpen == true;

    private void OnDockLayoutChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnCurrentUserChanged()
    {
        _userLoaded = false;
        _ = InvokeAsync(LoadCurrentUserAsync);
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

    private bool HasAnyAdminAuthority(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return false;

        return _isCurrentUserInstanceAdmin
               || _isCurrentUserTenantAdmin;
    }

    private bool IsInstanceAdmin(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true
               && _isCurrentUserInstanceAdmin;
    }

    private bool IsTenantAdmin(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true
               && _isCurrentUserTenantAdmin;
    }

    private static IEnumerable<string> GetAdminOrganizationIds(ClaimsPrincipal user)
    {
        return [];
    }

    private async Task LoadNavigationLinksAsync()
    {
        var shellTask = PublicExperienceService.GetCachedShellAsync();
        var shell = shellTask is null ? null : await shellTask;
        if (shell?.Navigation?.Links?.Count > 0)
        {
            _navigationLinks = shell.Navigation.Links
                .Where(link => !string.IsNullOrWhiteSpace(link.Label) && !string.IsNullOrWhiteSpace(link.Url))
                .OrderBy(link => link.SortOrder)
                .ThenBy(link => link.Label, StringComparer.OrdinalIgnoreCase)
                .Select(link => new TenantNavigationLinkDto
                {
                    Id = Guid.NewGuid(),
                    Label = link.Label!,
                    Url = link.Url!,
                    Order = link.SortOrder ?? 0,
                    OpenInNewTab = false
                })
                .ToList();
            return;
        }

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
            _isCurrentUserInstanceAdmin = status?.IsAuthenticated == true && status.IsCurrentUserInstanceAdmin == true;

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                var authority = await UserService.GetAdminAuthorityAsync();
                if (authority is not null)
                {
                    _isCurrentUserInstanceAdmin = authority.IsInstanceAdmin == true || _isCurrentUserInstanceAdmin;
                    _isCurrentUserTenantAdmin = authority.AdminTenantIds?.Any() == true;
                }

                var tenantStatus = await TenantOnboardingService.GetStatusAsync();
                _isCurrentUserTenantAdmin = _isCurrentUserTenantAdmin
                    || (tenantStatus?.IsAuthenticated == true && tenantStatus.IsCurrentUserTenantAdministrator == true);
            }
        }
        catch
        {
            _isSingleTenantMode = true;
            _isCurrentUserInstanceAdmin = false;
            _isCurrentUserTenantAdmin = false;
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
            _userGroups = new List<GroupListDto>();
        }
    }

    private void StartLogin()
    {
        var returnUrl = new Uri(Nav.Uri).PathAndQuery;
        Nav.NavigateTo($"/auth/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
    }

    private async Task OpenLoginPrompt(string? returnUrl, string? message = null)
    {
        returnUrl ??= new Uri(Nav.Uri).PathAndQuery;
        await AccessibilityFocusService.SaveFocusAsync();
        await LoginPromptDialog.ShowAsync(DialogService, returnUrl, message);
        await AccessibilityFocusService.RestoreFocusAsync();
    }

    private async Task FocusSearchAsync()
    {
        await _searchField.FocusAsync();
    }

    public void Dispose()
    {
        SidebarState.OnChange -= StateHasChanged;
        AiAssistantState.OnChange -= StateHasChanged;
        TenantNavLinksState.OnChange -= StateHasChanged;
        DockLayoutState.Changed -= OnDockLayoutChanged;
        CurrentUserState.OnChanged -= OnCurrentUserChanged;
        GC.SuppressFinalize(this);
    }
}
