// ABOUTME: Main layout code-behind handling theme initialization, user sync, and accessibility.
// ABOUTME: Uses the new IAppearanceThemeService with AppearanceState for reactive theme management.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Contracts.Services.Organizations;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Appearance;
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

        _theme = AppearanceThemeService.CreateTheme(GetAppbarHeight());
        AppearanceThemeService.Changed += OnAppearanceChanged;
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
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
                    await AppearanceThemeService.InitializeAsync(_mudThemeProvider);
                    _theme = AppearanceThemeService.CreateTheme(GetAppbarHeight());
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not initialize appearance theme service.");
            }

            if (!InitialTheme.HasValue)
            {
                try
                {
                    _isDarkMode = await AppearanceThemeService.ResolveEffectiveDarkModeAsync(_mudThemeProvider);
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
        var mode = _isDarkMode ? "dark" : "light";
        await AppearanceThemeService.SetThemeModeAsync(mode);
    }

    private void OnAppearanceChanged(object? sender, AppearanceStateChangedEventArgs e)
    {
        _theme = AppearanceThemeService.CreateTheme(GetAppbarHeight());
        var mode = e.State.ThemeMode.ToLowerInvariant();
        if (mode is "dark" or "darkhighcontrast") _isDarkMode = true;
        else if (mode is "light" or "lighthighcontrast") _isDarkMode = false;
        InvokeAsync(StateHasChanged);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        UpdateChromeVisibility();
        _ = InvokeAsync(async () =>
        {
            StateHasChanged();
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

    private void OnAnnouncementVisibilityChanged(bool isVisible)
    {
        _announcementVisible = isVisible;
        if (_theme is not null)
        {
            _theme = AppearanceThemeService.CreateTheme(GetAppbarHeight());
            StateHasChanged();
        }
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
        AppearanceThemeService.Changed -= OnAppearanceChanged;
        GC.SuppressFinalize(this);
    }
}