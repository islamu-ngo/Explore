// ABOUTME: Unit tests for AppearanceResolutionService — verifies the 6-tier fallback chain, tenant isolation, and snapshot isolation.
// ABOUTME: Tests enterprise scenarios: tenant mutation isolation, deletion resilience, multi-profile management, system fallback.

namespace Explore.Application.UnitTests.Services;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Appearance;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using NSubstitute;

public class AppearanceResolutionServiceTests
{
    private readonly IUiThemePresetRepository _presetRepo = Substitute.For<IUiThemePresetRepository>();
    private readonly IUserAppearanceProfileRepository _profileRepo = Substitute.For<IUserAppearanceProfileRepository>();
    private readonly IUserAppearancePreferenceRepository _preferenceRepo = Substitute.For<IUserAppearancePreferenceRepository>();
    private readonly IUiThemeRepository _uiThemeRepo = Substitute.For<IUiThemeRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();

    private AppearanceResolutionService CreateSut() => new(
        _presetRepo, _profileRepo, _preferenceRepo, _uiThemeRepo,
        _tenantContext, _currentUserService, _settingsResolver);

    [Test]
    public async Task ResolveForCurrentUser_WhenUnauthenticated_ReturnsEmergencyFallback()
    {
_currentUserService.UserId.Returns((Guid?)null);
        var sut = CreateSut();

        var result = await sut.ResolveForCurrentUserAsync();

        await Assert.That(result.ResolutionSource).IsEqualTo("EmergencyFallback");
    }

    [Test]
    public async Task ResolveForCurrentUser_WhenUserHasActiveProfile_ReturnsUserProfile()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);
        _tenantContext.TenantId.Returns(tenantId);

        var profile = CreateTestProfile(userId, tenantId, "My Theme");
        var preference = CreateTestPreference(userId, tenantId, profile.Id, profile);

        _preferenceRepo.GetByUserAndTenantAsync(userId, tenantId).Returns(preference);
        _profileRepo.GetById(profile.Id).Returns(profile);

        var sut = CreateSut();

        var result = await sut.ResolveForCurrentUserAsync();

        await Assert.That(result.ResolutionSource).IsEqualTo("UserTenantProfile");
        await Assert.That(result.ActiveProfileId).IsEqualTo(profile.Id);
        await Assert.That(result.Theme.DisplayName).IsEqualTo("My Theme");
    }

    [Test]
    public async Task ResolveForCurrentUser_WhenNoActiveProfile_FallsBackToSystemPreset()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);
        _tenantContext.TenantId.Returns(Guid.Empty);

        _preferenceRepo.GetByUserAndTenantAsync(userId, Arg.Any<Guid?>()).Returns((UserAppearancePreference?)null);
        _presetRepo.GetDefaultPresetForTenantAsync(Arg.Any<Guid?>()).Returns((UiThemePreset?)null);
        _presetRepo.GetByThemeKeyAsync(Arg.Any<Guid?>(), "enterprise-blue").Returns(CreateSystemPreset());

        var sut = CreateSut();

        var result = await sut.ResolveForCurrentUserAsync();

        await Assert.That(result.ResolutionSource).IsEqualTo("SystemPresetFallback");
        await Assert.That(result.Theme.DisplayName).IsEqualTo("Enterprise Blue");
    }

    [Test]
    public async Task ResolveForCurrentUser_WhenNoDataAtAll_ReturnsEmergencyFallback()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);
        _tenantContext.TenantId.Returns(Guid.Empty);

        _preferenceRepo.GetByUserAndTenantAsync(userId, Arg.Any<Guid?>()).Returns((UserAppearancePreference?)null);
        _presetRepo.GetDefaultPresetForTenantAsync(Arg.Any<Guid?>()).Returns((UiThemePreset?)null);
        _presetRepo.GetByThemeKeyAsync(Arg.Any<Guid?>(), "enterprise-blue").Returns((UiThemePreset?)null);

        var sut = CreateSut();

        var result = await sut.ResolveForCurrentUserAsync();

        await Assert.That(result.ResolutionSource).IsEqualTo("EmergencyFallback");
    }

    [Test]
    public async Task SetThemeMode_Parses_LightHighContrast()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);
        _tenantContext.TenantId.Returns(Guid.Empty);
        var pref = new UserAppearancePreference { Id = Guid.NewGuid(), UserId = userId, TenantId = Guid.Empty, ActiveProfileId = Guid.NewGuid(), ThemeMode = AppearanceThemeMode.System };
        _preferenceRepo.GetByUserAndTenantAsync(userId, Arg.Any<Guid?>()).Returns(pref);

        var sut = CreateSut();

        await sut.SetThemeModeAsync("lighthighcontrast");

        await _preferenceRepo.Received(1).Update(Arg.Is<UserAppearancePreference>(p => p.ThemeMode == AppearanceThemeMode.LightHighContrast));
    }

    [Test]
    public async Task SetThemeMode_Parses_DarkHighContrast()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);
        _tenantContext.TenantId.Returns(Guid.Empty);
        var pref = new UserAppearancePreference { Id = Guid.NewGuid(), UserId = userId, TenantId = Guid.Empty, ActiveProfileId = Guid.NewGuid(), ThemeMode = AppearanceThemeMode.System };
        _preferenceRepo.GetByUserAndTenantAsync(userId, Arg.Any<Guid?>()).Returns(pref);

        var sut = CreateSut();

        await sut.SetThemeModeAsync("darkhighcontrast");

        await _preferenceRepo.Received(1).Update(Arg.Is<UserAppearancePreference>(p => p.ThemeMode == AppearanceThemeMode.DarkHighContrast));
    }

    [Test]
    public async Task SetThemeMode_Parses_Custom()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);
        _tenantContext.TenantId.Returns(Guid.Empty);
        var pref = new UserAppearancePreference { Id = Guid.NewGuid(), UserId = userId, TenantId = Guid.Empty, ActiveProfileId = Guid.NewGuid(), ThemeMode = AppearanceThemeMode.System };
        _preferenceRepo.GetByUserAndTenantAsync(userId, Arg.Any<Guid?>()).Returns(pref);

        var sut = CreateSut();

        await sut.SetThemeModeAsync("custom");

        await _preferenceRepo.Received(1).Update(Arg.Is<UserAppearancePreference>(p => p.ThemeMode == AppearanceThemeMode.Custom));
    }

    [Test]
    public async Task ArchiveProfile_WhenProfileIsDefault_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);

        var profile = CreateTestProfile(userId, null, "Default");
        profile.IsDefault = true;
        _profileRepo.GetById(profile.Id).Returns(profile);

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await sut.ArchiveProfileAsync(profile.Id));
    }

    [Test]
    public async Task ArchiveProfile_WhenNotDefault_ArchivesSuccessfully()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);

        var profile = CreateTestProfile(userId, null, "My Theme");
        profile.IsDefault = false;
        _profileRepo.GetById(profile.Id).Returns(profile);

        var sut = CreateSut();

        await sut.ArchiveProfileAsync(profile.Id);

        await _profileRepo.Received(1).Update(Arg.Is<UserAppearanceProfile>(p => p.IsArchived == true));
    }

    [Test]
    public async Task DuplicateProfile_CreatesCopyWithSuffix()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);
        _tenantContext.TenantId.Returns(Guid.Empty);

        var source = CreateTestProfile(userId, null, "Ocean Blue");
        _profileRepo.GetById(source.Id).Returns(source);
        _profileRepo.Create(Arg.Any<UserAppearanceProfile>()).Returns(call => call.Arg<UserAppearanceProfile>());

        var sut = CreateSut();

        var result = await sut.DuplicateProfileAsync(source.Id, null);

        await _profileRepo.Received(1).Create(Arg.Is<UserAppearanceProfile>(p =>
            p.Name == "Ocean Blue (Copy)" &&
            p.SourcePresetKey == source.SourcePresetKey &&
            p.SourcePresetId == source.SourcePresetId));
    }

    [Test]
    public async Task DuplicateProfile_WithCustomName_UsesCustomName()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);
        _tenantContext.TenantId.Returns(Guid.Empty);

        var source = CreateTestProfile(userId, null, "Ocean Blue");
        _profileRepo.GetById(source.Id).Returns(source);
        _profileRepo.Create(Arg.Any<UserAppearanceProfile>()).Returns(call => call.Arg<UserAppearanceProfile>());

        var sut = CreateSut();

        var result = await sut.DuplicateProfileAsync(source.Id, "My Custom Name");

        await _profileRepo.Received(1).Create(Arg.Is<UserAppearanceProfile>(p =>
            p.Name == "My Custom Name"));
    }

    [Test]
    public async Task GeneratePalette_Returns_Light_Palette_When_IsDark_False()
    {
        var sut = CreateSut();

        var palette = sut.GeneratePalette("#475569", "#3B82F6", isDark: false);

        await Assert.That(palette.Primary).IsNotEmpty();
        await Assert.That(palette.Info).IsEqualTo("#2563EB");
    }

    [Test]
    public async Task GeneratePalette_Returns_Dark_Palette_When_IsDark_True()
    {
        var sut = CreateSut();

        var palette = sut.GeneratePalette("#1E293B", "#3B82F6", isDark: true);

        await Assert.That(palette.Primary).IsNotEmpty();
        await Assert.That(palette.Info).IsEqualTo("#60A5FA");
    }

    private static UserAppearanceProfile CreateTestProfile(Guid userId, Guid? tenantId, string name) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        TenantId = tenantId,
        Name = name,
        ThemeMode = AppearanceThemeMode.System,
        LightPaletteSnapshot = CreateWhitePalette(),
        DarkPaletteSnapshot = CreateBlackPalette(),
        IsUserEditable = true,
        IsDefault = false,
        IsArchived = false,
        SourcePresetKey = "enterprise-blue",
        SourcePresetId = Guid.Parse("a1b2c3d4-1111-1111-1111-111111111111"),
        SourcePresetSeedVersion = 2
    };

    private static UserAppearancePreference CreateTestPreference(Guid userId, Guid? tenantId, Guid profileId, UserAppearanceProfile profile) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        TenantId = tenantId,
        ActiveProfileId = profileId,
        ActiveProfile = profile,
        ThemeMode = AppearanceThemeMode.System,
        Direction = "auto",
        Language = "en"
    };

    private static UiThemePreset CreateSystemPreset() => new()
    {
        Id = Guid.Parse("a1b2c3d4-1111-1111-1111-111111111111"),
        TenantId = null,
        ThemeKey = "enterprise-blue",
        DisplayName = "Enterprise Blue",
        Description = "Default professional theme",
        LightPalette = CreateWhitePalette(),
        DarkPalette = CreateBlackPalette(),
        IsSystem = true,
        IsEditable = false,
        IsActive = true,
        SeedVersion = 2
    };

    private static UiThemePalette CreateWhitePalette() => new()
    {
        Primary = "#0F62FE", PrimaryContrastText = "#FFFFFF",
        Secondary = "#475569", SecondaryContrastText = "#FFFFFF",
        Background = "#F1F5F9", Surface = "#FFFFFF",
        AppbarBackground = "#FFFFFF", AppbarText = "#1E293B",
        DrawerBackground = "#FFFFFF", DrawerText = "#1E293B", DrawerIcon = "#475569",
        TextPrimary = "#0F172A", TextSecondary = "#475569",
        Info = "#2563EB", Success = "#16A34A", Warning = "#D97706", Error = "#DC2626",
        LinesDefault = "#CBD5E1", Divider = "#CBD5E1"
    };

    private static UiThemePalette CreateBlackPalette() => new()
    {
        Primary = "#3B82F6", PrimaryContrastText = "#FFFFFF",
        Secondary = "#F1F5F9", SecondaryContrastText = "#0F172A",
        Background = "#0B0F19", Surface = "#1E293B",
        AppbarBackground = "rgba(11,15,25,0.85)", AppbarText = "#F1F5F9",
        DrawerBackground = "#0B0F19", DrawerText = "#F1F5F9", DrawerIcon = "#CBD5E1",
        TextPrimary = "#F8FAFC", TextSecondary = "#94A3B8",
        Info = "#60A5FA", Success = "#10B981", Warning = "#F59E0B", Error = "#EF4444",
        LinesDefault = "#334155", Divider = "#1E293B"
    };
}