// ABOUTME: Main layout code-behind handling theme initialization, user sync, and accessibility.
// ABOUTME: Uses the new IAppearanceThemeService with AppearanceState for reactive theme management.

using Explore.Blazor.Client.Components.Shell;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Services;
using Explore.Blazor.Client.Services.Docking;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Explore.Blazor.Client.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    private const string ShellDockLayoutKey = "shell";
    private static readonly TimeSpan DockLayoutAutosaveDelay = TimeSpan.FromMilliseconds(500);
    private const int NavbarHeightPx = 64;
    private const int AnnouncementBarHeightPx = 48;
    private const int SupportAccessBannerHeightPx = 56;

    private bool _isDarkMode = false;
    private bool _isInitialized = false;
    private bool _announcementVisible;
    private bool _supportAccessBannerVisible;
    private MudTheme? _theme;
    private MudThemeProvider _mudThemeProvider = null!;
    private bool _hideChrome;
    private bool _languagePickerEnabled = true;
    private bool _showCommunityGuidelinesLink = true;
    private string _brandDisplayName = string.Empty;
    private string _brandLogoUrl = string.Empty;
    private bool _syncingShellLegacyState;
    private bool _shellDockLayoutHydrated;
    private bool _suppressShellDockLayoutAutosave;
    private DockLayoutSnapshot? _lastPersistedShellDockLayoutSnapshot;
    private CancellationTokenSource? _shellDockLayoutAutosaveCts;

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
    protected MainContentAppearanceState MainContentAppearanceState { get; set; } = null!;

    [Inject]
    protected AiAssistantState AiAssistantState { get; set; } = null!;

    [Inject]
    protected DockLayoutState DockLayoutState { get; set; } = null!;

    [Inject]
    protected IDockLayoutPersistence DockLayoutPersistence { get; set; } = null!;

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
    public global::Explore.Blazor.Client.Models.LanguageContext? LanguageContext { get; set; }

    private bool _isRtl => LanguageContext?.EffectiveIsRtl ?? false;

    private string MainContentClass => MainContentAppearanceState.HasAppearance
        ? "main-layout__content-wrapper main-layout__content-wrapper--themed"
        : "main-layout__content-wrapper";

    private string MainContentStyle => MainContentAppearanceState.Style;

    public string DarkLightModeButtonIcon => _isDarkMode switch
    {
        true => Icons.Material.Rounded.AutoMode,
        false => Icons.Material.Outlined.DarkMode,
    };

    protected override void OnInitialized()
    {
        base.OnInitialized();

        RegisterShellDockPanels();

        NavigationManager.LocationChanged += OnLocationChanged;
        SidebarState.OnChange += OnLegacySidebarStateChanged;
        MainContentAppearanceState.Changed += OnMainContentAppearanceChanged;
        AiAssistantState.OnChange += OnLegacyAiAssistantStateChanged;
        TenantNavLinksState.OnChange += OnTenantNavLinksChanged;
        DockLayoutState.Changed += OnDockLayoutChanged;
        UpdateChromeVisibility();
        SyncShellDockState();

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
            await HydrateShellDockLayoutAsync();

            var isAuthenticated = false;

            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                isAuthenticated = authState.User.Identity?.IsAuthenticated == true;
                if (isAuthenticated)
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
                if (settings is not null)
                {
                    _showCommunityGuidelinesLink = settings.AllowUserSubmittedEvents
                        || settings.AllowOrganizationSubmittedEvents
                        || settings.AllowGroupSubmittedEvents;
                    _brandDisplayName = settings.BrandDisplayName;
                    _brandLogoUrl = settings.BrandLogoUrl ?? string.Empty;
                    _languagePickerEnabled = settings.ClientPickerEnabled;
                    AiAssistantState.SetPolicy(
                        settings.IsAiAssistantEnabled,
                        settings.IsAiAssistantAvailable,
                        settings.AiAssistantAllowAnonymousAccess,
                        isAuthenticated);
                    DockLayoutState.Refresh();
                    await InvokeAsync(StateHasChanged);
                }
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

    private void OnMainContentAppearanceChanged()
    {
        _ = InvokeAsync(StateHasChanged);
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


    private void RegisterShellDockPanels()
    {
        DockLayoutState.Register(ShellDockPanels.LeftNav, RenderShellLeftNav);
        DockLayoutState.Register(ShellDockPanels.AiAssistant, RenderShellAiAssistant);
    }

    private void RenderShellLeftNav(RenderTreeBuilder builder)
    {
        builder.OpenComponent<AppSideNav>(0);
        builder.AddAttribute(1, "AriaLabel", "Sidebar navigation");
        builder.AddAttribute(2, "BrandDisplayName", _brandDisplayName);
        builder.AddAttribute(3, "BrandLogoUrl", _brandLogoUrl);
        builder.AddAttribute(4, "ShowCommunityGuidelinesLink", _showCommunityGuidelinesLink);
        builder.AddAttribute(5, "TenantLinks", TenantNavLinksState.Links);
        builder.AddAttribute(6, "OnCloseRequested", EventCallback.Factory.Create(this, CloseShellLeftNav));
        builder.CloseComponent();
    }

    private static void RenderShellAiAssistant(RenderTreeBuilder builder)
    {
        builder.OpenComponent<AiAssistantRail>(0);
        builder.AddAttribute(1, "HostedInDock", true);
        builder.CloseComponent();
    }

    private void OnLegacySidebarStateChanged()
    {
        if (_syncingShellLegacyState)
        {
            return;
        }

        SyncShellDockState();
        StateHasChanged();
    }

    private void OnLegacyAiAssistantStateChanged()
    {
        if (_syncingShellLegacyState)
        {
            return;
        }

        SyncShellDockState();
        StateHasChanged();
    }

    private void OnDockLayoutChanged()
    {
        var leftNavOpen = DockLayoutState.GetPanel(ShellDockPanels.LeftNavId)?.State.IsOpen == true;
        var aiOpen = DockLayoutState.GetPanel(ShellDockPanels.AiAssistantId)?.State.IsOpen == true;

        _syncingShellLegacyState = true;
        try
        {
            SidebarState.SetOpen(leftNavOpen);

            if (aiOpen && !AiAssistantState.IsOpen)
            {
                AiAssistantState.Open();
            }
            else if (!aiOpen && AiAssistantState.IsOpen)
            {
                AiAssistantState.Close();
            }
        }
        finally
        {
            _syncingShellLegacyState = false;
        }

        if (ShouldAutosaveShellDockLayout())
        {
            ScheduleShellDockLayoutAutosave();
        }
        _ = InvokeAsync(StateHasChanged);
    }

    private bool ShouldAutosaveShellDockLayout()
    {
        return DockLayoutState.LastChangeReason is DockLayoutChangeReason.UserAction or DockLayoutChangeReason.Reset;
    }

    private async Task HydrateShellDockLayoutAsync()
    {
        _suppressShellDockLayoutAutosave = true;

        try
        {
            var snapshot = await DockLayoutPersistence.LoadAsync(ShellDockLayoutKey);
            if (snapshot is not null)
            {
                DockLayoutState.RestoreSnapshot(snapshot, ShellDockLayoutKey, DockScope.Shell);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to hydrate shell dock layout.");
        }
        finally
        {
            _lastPersistedShellDockLayoutSnapshot = CreateShellDockLayoutSnapshot();
            _shellDockLayoutHydrated = true;
            _suppressShellDockLayoutAutosave = false;
        }
    }

    private void ScheduleShellDockLayoutAutosave()
    {
        if (!_shellDockLayoutHydrated || _suppressShellDockLayoutAutosave || !HasShellDockLayoutChanged())
        {
            return;
        }

        _shellDockLayoutAutosaveCts?.Cancel();
        _shellDockLayoutAutosaveCts?.Dispose();

        var autosaveCts = new CancellationTokenSource();
        _shellDockLayoutAutosaveCts = autosaveCts;
        _ = PersistShellDockLayoutAfterDelayAsync(autosaveCts);
    }

    private async Task PersistShellDockLayoutAfterDelayAsync(CancellationTokenSource autosaveCts)
    {
        try
        {
            await Task.Delay(DockLayoutAutosaveDelay, autosaveCts.Token);
            var snapshot = CreateShellDockLayoutSnapshot();
            if (SnapshotPanelsEqual(_lastPersistedShellDockLayoutSnapshot, snapshot))
            {
                return;
            }

            await DockLayoutPersistence.SaveAsync(snapshot, autosaveCts.Token);
            _lastPersistedShellDockLayoutSnapshot = snapshot;
        }
        catch (OperationCanceledException) when (autosaveCts.IsCancellationRequested)
        {
            // A newer dock layout change superseded this pending autosave.
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to save shell dock layout.");
        }
    }

    private async Task ResetShellDockLayoutAsync()
    {
        _suppressShellDockLayoutAutosave = true;

        try
        {
            ResetShellPanelToDefaults(ShellDockPanels.LeftNav);
            ResetShellPanelToDefaults(ShellDockPanels.AiAssistant);
            await DockLayoutPersistence.DeleteAsync(ShellDockLayoutKey);
            _lastPersistedShellDockLayoutSnapshot = CreateShellDockLayoutSnapshot();
            SyncShellDockState();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to reset shell dock layout.");
        }
        finally
        {
            _suppressShellDockLayoutAutosave = false;
            _shellDockLayoutHydrated = true;
        }
    }

    private void ResetShellPanelToDefaults(DockPanelDescriptor descriptor)
    {
        var entry = DockLayoutState.GetPanel(descriptor.Id);
        if (entry is null)
        {
            return;
        }

        if (entry.State.IsOpen && descriptor.CanClose)
        {
            DockLayoutState.Close(descriptor.Id);
        }

        DockLayoutState.SetMode(descriptor.Id, descriptor.DefaultMode);

        if (descriptor.IsResizable)
        {
            DockLayoutState.Resize(descriptor.Id, descriptor.DefaultWidth);
        }
    }

    private DockLayoutSnapshot CreateShellDockLayoutSnapshot()
    {
        return DockLayoutState.CreateSnapshot(ShellDockLayoutKey, DockScope.Shell);
    }

    private bool HasShellDockLayoutChanged()
    {
        return !SnapshotPanelsEqual(_lastPersistedShellDockLayoutSnapshot, CreateShellDockLayoutSnapshot());
    }

    private static bool SnapshotPanelsEqual(DockLayoutSnapshot? previous, DockLayoutSnapshot current)
    {
        return previous is not null && previous.Panels.SequenceEqual(current.Panels);
    }

    private void CloseShellLeftNav()
    {
        SyncPanelState(ShellDockPanels.LeftNavId, shouldBeOpen: false);
    }

    private void OnTenantNavLinksChanged()
    {
        DockLayoutState.Refresh();
        StateHasChanged();
    }

    private void SyncShellDockState()
    {
        SyncPanelState(ShellDockPanels.LeftNavId, !_hideChrome && SidebarState.HasSidebar && SidebarState.IsOpen);
        SyncPanelState(ShellDockPanels.AiAssistantId, !_hideChrome && AiAssistantState.IsAvailable && AiAssistantState.IsOpen);
    }

    private void SyncPanelState(DockPanelId id, bool shouldBeOpen)
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

    private void OnAnnouncementVisibilityChanged(bool isVisible)
    {
        _announcementVisible = isVisible;
        if (_theme is not null)
        {
            _theme = AppearanceThemeService.CreateTheme(GetAppbarHeight());
            StateHasChanged();
        }
    }

    private void OnSupportAccessBannerVisibilityChanged(bool isVisible)
    {
        _supportAccessBannerVisible = isVisible;
        if (_theme is not null)
        {
            _theme = AppearanceThemeService.CreateTheme(GetAppbarHeight());
            StateHasChanged();
        }
    }

    private string GetAppbarHeight()
    {
        var height = NavbarHeightPx
            + (_announcementVisible ? AnnouncementBarHeightPx : 0)
            + (_supportAccessBannerVisible ? SupportAccessBannerHeightPx : 0);
        return $"{height}px";
    }

    public void Dispose()
    {
        _shellDockLayoutAutosaveCts?.Cancel();
        _shellDockLayoutAutosaveCts?.Dispose();
        NavigationManager.LocationChanged -= OnLocationChanged;
        SidebarState.OnChange -= OnLegacySidebarStateChanged;
        MainContentAppearanceState.Changed -= OnMainContentAppearanceChanged;
        AiAssistantState.OnChange -= OnLegacyAiAssistantStateChanged;
        TenantNavLinksState.OnChange -= OnTenantNavLinksChanged;
        DockLayoutState.Changed -= OnDockLayoutChanged;
        AppearanceThemeService.Changed -= OnAppearanceChanged;
        DockLayoutState.Unregister(ShellDockPanels.LeftNavId);
        DockLayoutState.Unregister(ShellDockPanels.AiAssistantId);
        GC.SuppressFinalize(this);
    }
}
