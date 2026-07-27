// ABOUTME: Unit tests for AppearanceResolutionService — verifies the 6-tier fallback chain, tenant isolation, and snapshot isolation.
// ABOUTME: Tests enterprise scenarios: tenant mutation isolation, deletion resilience, multi-profile management, system fallback.

namespace Explore.Application.UnitTests.Services;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Appearance;
using Explore.Application.Services;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
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
    public async Task ResolveForCurrentUser_WhenNoDefaultIsConfigured_DoesNotSelectAlphabeticalPreset()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);
        _tenantContext.TenantId.Returns(tenantId);
        _preferenceRepo.GetByUserAndTenantAsync(userId, Arg.Any<Guid?>()).Returns((UserAppearancePreference?)null);
        _settingsResolver.ResolveWithMetadataAsync(
                GovernanceSettingKeys.Appearance.DefaultPresetId,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResolvedSetting
            {
                Key = GovernanceSettingKeys.Appearance.DefaultPresetId,
                Value = "\"\"",
                Source = SettingSource.SystemDefault
            });

        var alphabeticalPreset = CreateSystemPreset();
        alphabeticalPreset.DisplayName = "Abyssal Dark";
        alphabeticalPreset.ThemeKey = "abyssal-dark";
        _presetRepo.GetDefaultPresetForTenantAsync(Arg.Any<Guid?>()).Returns(alphabeticalPreset);
        _presetRepo.GetByThemeKeyAsync(null, "enterprise-blue").Returns(CreateSystemPreset());

        var result = await CreateSut().ResolveForCurrentUserAsync();

        await Assert.That(result.ResolutionSource).IsEqualTo("SystemPresetFallback");
        await Assert.That(result.SourcePresetKey).IsEqualTo("enterprise-blue");
        await Assert.That(result.ThemeMode).IsEqualTo("system");
    }

    [Test]
    public async Task ResolveForCurrentUser_WhenTenantDefaultIsConfigured_UsesConfiguredPreset()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var configuredPreset = CreateSystemPreset();
        configuredPreset.Id = Guid.NewGuid();
        configuredPreset.TenantId = tenantId;
        configuredPreset.ThemeKey = "tenant-default";
        configuredPreset.DisplayName = "Tenant Default";

        _currentUserService.UserId.Returns((Guid?)userId);
        _tenantContext.TenantId.Returns(tenantId);
        _preferenceRepo.GetByUserAndTenantAsync(userId, Arg.Any<Guid?>()).Returns((UserAppearancePreference?)null);
        _settingsResolver.ResolveWithMetadataAsync(
                GovernanceSettingKeys.Appearance.DefaultPresetId,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResolvedSetting
            {
                Key = GovernanceSettingKeys.Appearance.DefaultPresetId,
                Value = $"\"{configuredPreset.Id}\"",
                Source = SettingSource.TenantOverride
            });
        _presetRepo.GetById(configuredPreset.Id).Returns(configuredPreset);

        var result = await CreateSut().ResolveForCurrentUserAsync();

        await Assert.That(result.ResolutionSource).IsEqualTo("TenantDefaultPreset");
        await Assert.That(result.SourcePresetKey).IsEqualTo("tenant-default");
        await Assert.That(result.ThemeMode).IsEqualTo("system");
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
    public async Task UpdateProfile_WithMetadataOnly_PreservesPalettesAndUpdatesOnce()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);
        var profile = CreateTestProfile(userId, null, "Original");
        var originalLight = profile.LightPaletteSnapshot;
        var originalDark = profile.DarkPaletteSnapshot;
        _profileRepo.GetById(profile.Id).Returns(profile);
        var sut = CreateSut();

        var result = await sut.UpdateProfileAsync(profile.Id, new UpdateAppearanceProfileRequestDto
        {
            Metadata = new UpdateAppearanceProfileMetadataDto { Name = " Updated " }
        });

        await Assert.That(result.Name).IsEqualTo("Updated");
        await Assert.That(profile.LightPaletteSnapshot).IsSameReferenceAs(originalLight);
        await Assert.That(profile.DarkPaletteSnapshot).IsSameReferenceAs(originalDark);
        await _profileRepo.Received(1).Update(profile);
    }

    [Test]
    public async Task UpdateProfile_WithEmptyWrapper_RejectsBeforeLoadingProfile()
    {
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)userId);
        var sut = CreateSut();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(async () =>
            await sut.UpdateProfileAsync(Guid.NewGuid(), new UpdateAppearanceProfileRequestDto()));

        await _profileRepo.DidNotReceive().GetById(Arg.Any<Guid>());
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
        Primary = "#18181B",
        PrimaryContrastText = "#FFFFFF",
        Secondary = "#52525B",
        SecondaryContrastText = "#FFFFFF",
        Background = "#F5F5F7",
        Surface = "#FFFFFF",
        AppbarBackground = "#FFFFFF",
        AppbarText = "#18181B",
        DrawerBackground = "#FFFFFF",
        DrawerText = "#18181B",
        DrawerIcon = "#52525B",
        TextPrimary = "#18181B",
        TextSecondary = "#404040",
        Info = "#52525B",
        Success = "#16A34A",
        Warning = "#D97706",
        Error = "#DC2626",
        LinesDefault = "#A1A1AA",
        Divider = "#E4E4E7"
    };

    private static UiThemePalette CreateBlackPalette() => new()
    {
        Primary = "#FAFAFA",
        PrimaryContrastText = "#1A1A1A",
        Secondary = "#A1A1AA",
        SecondaryContrastText = "#1A1A1A",
        Background = "#1A1A1A",
        Surface = "#242424",
        AppbarBackground = "rgba(18,18,18,0.92)",
        AppbarText = "#FAFAFA",
        DrawerBackground = "#1A1A1A",
        DrawerText = "#FAFAFA",
        DrawerIcon = "#A1A1AA",
        TextPrimary = "#FAFAFA",
        TextSecondary = "#A1A1AA",
        Info = "#A1A1AA",
        Success = "#34D399",
        Warning = "#FBBF24",
        Error = "#F87171",
        LinesDefault = "#3F3F46",
        Divider = "#2E2E2E"
    };
}
