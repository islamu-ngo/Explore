// ABOUTME: Main layout code-behind handling theme initialization, user sync, and accessibility.
// ABOUTME: Uses MudBlazor theme switching with cookie persistence, and manages focus-on-navigate for screen readers.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Contracts.Services.Organizations;

using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    private const int NavbarHeightPx = 64;
    private const int AnnouncementBarHeightPx = 48;

    private bool _isDarkMode = false;
    private bool _isInitialized = false;
    private bool _announcementVisible = true;
    private MudTheme? _theme;
    private MudThemeProvider _mudThemeProvider = null!;
    private bool _hideChrome;
    private bool _showCommunityGuidelinesLink = true;
    private string _brandDisplayName = string.Empty;
    private AvailableThemeDto? _activeTheme;

    [Inject]
    protected IUserService UserService { get; set; } = null!;

    [Inject]
    protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    [Inject]
    protected ILogger<MainLayout> Logger { get; set; } = null!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    protected SidebarState SidebarState { get; set; } = null!;

    [Inject]
    protected AiAssistantState AiAssistantState { get; set; } = null!;

    [Inject]
    protected TenantNavLinksState TenantNavLinksState { get; set; } = null!;

    [Inject]
    protected ITenantNavigationService TenantNavigationService { get; set; } = null!;

    [Inject]
    protected IPublicExperienceService PublicExperienceService { get; set; } = null!;

    [Inject]
    protected IAppearanceThemeService AppearanceThemeService { get; set; } = null!;

    [Inject]
    protected IAccessibilityFocusService AccessibilityFocusService { get; set; } = null!;

    [CascadingParameter(Name = "InitialTheme")]
    public bool? InitialTheme { get; set; }

    [CascadingParameter(Name = "Language")]
    public Models.LanguageContext? LanguageContext { get; set; }

    private bool _isRtl => LanguageContext?.EffectiveIsRtl ?? false;

    public string DarkLightModeButtonIcon => _isDarkMode switch
    {
        true => Icons.Material.Rounded.AutoMode,
        false => Icons.Material.Outlined.DarkMode,
    };

    protected override void OnInitialized()
    {
        base.OnInitialized();

        NavigationManager.LocationChanged += OnLocationChanged;
        SidebarState.OnChange += StateHasChanged;
        AiAssistantState.OnChange += StateHasChanged;
        TenantNavLinksState.OnChange += StateHasChanged;
        UpdateChromeVisibility();

        if (InitialTheme.HasValue)
        {
            _isDarkMode = InitialTheme.Value;
        }

        _theme = AppearanceThemeService.CreateTheme(GetAppbarHeight(), _activeTheme);
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Sync user in background after first render to improve perceived performance
            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated == true)
                {
                    await UserService.SyncUserAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error syncing user");
            }

            // Load public experience settings to determine sidebar visibility
            try
            {
                var settings = await PublicExperienceService.GetCachedSettingsAsync();
                _showCommunityGuidelinesLink = settings.AllowUserSubmittedEvents
                    || settings.AllowOrganizationSubmittedEvents
                    || settings.AllowGroupSubmittedEvents;
                _brandDisplayName = settings.BrandDisplayName;
                AiAssistantState.SetAvailable(settings.IsAiAssistantAvailable);
                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error loading public experience settings for sidebar");
            }

            // Load tenant navigation links for the sidebar drawer
            try
            {
                await TenantNavLinksState.EnsureLoadedAsync(TenantNavigationService);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error loading tenant navigation links for sidebar");
            }

            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated == true)
                {
                    await LoadActiveThemeAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not load active UI theme for the current user.");
            }

            if (!InitialTheme.HasValue)
            {
                try
                {
                    _isDarkMode = await AppearanceThemeService.ResolveInitialDarkModeAsync(null, _mudThemeProvider);
                    await InvokeAsync(StateHasChanged);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error initializing theme");
                }
            }

            _isInitialized = true;
        }
    }

    private async Task DarkModeToggle()
    {
        _isDarkMode = !_isDarkMode;
        await AppearanceThemeService.PersistThemeModeAsync(_isDarkMode);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        UpdateChromeVisibility();
        _ = InvokeAsync(async () =>
        {
            StateHasChanged();
            // Move focus to h1 after navigation for screen readers (replaces FocusOnNavigate for Blazouter)
            await AccessibilityFocusService.FocusOnNavigateAsync();
        });
    }

    private void UpdateChromeVisibility()
    {
        var relative = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        var path = relative.Split('?', '#')[0];

        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        if (path.Length > 1)
        {
            path = path.TrimEnd('/');
        }

        _hideChrome = path.Equals("/setup", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/onboarding/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/startup", StringComparison.OrdinalIgnoreCase);

        SidebarState.SetHasSidebar(!_hideChrome);
    }

    private void OnDrawerOpenChanged(bool open) => SidebarState.SetOpen(open);

    /// <summary>
    /// Called when the announcement bar is shown or dismissed.
    /// Recreates the theme with an updated AppbarHeight so
    /// --mud-appbar-height on :root reflects the true header height.
    /// MudBlazor's ClipMode.Always drawer CSS and sticky components
    /// automatically use the updated value.
    /// </summary>
    private void OnAnnouncementVisibilityChanged(bool isVisible)
    {
        _announcementVisible = isVisible;
        if (_theme is not null)
        {
            _theme = AppearanceThemeService.CreateTheme(GetAppbarHeight(), _activeTheme);
            StateHasChanged();
        }
    }

    private async Task LoadActiveThemeAsync()
    {
        var active = await AppearanceThemeService.ResolveActiveThemeAsync();
        if (active is null)
        {
            return;
        }

        _activeTheme = active;
        _theme = AppearanceThemeService.CreateTheme(GetAppbarHeight(), _activeTheme);
        await InvokeAsync(StateHasChanged);
    }

    private string GetAppbarHeight()
    {
        var height = NavbarHeightPx + (_announcementVisible ? AnnouncementBarHeightPx : 0);
        return $"{height}px";
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        SidebarState.OnChange -= StateHasChanged;
        AiAssistantState.OnChange -= StateHasChanged;
        TenantNavLinksState.OnChange -= StateHasChanged;
        GC.SuppressFinalize(this);
    }
}
