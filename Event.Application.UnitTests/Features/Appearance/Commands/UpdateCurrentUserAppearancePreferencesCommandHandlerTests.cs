// ABOUTME: Unit tests for user appearance-preference upserts with sparse-override semantics.
// ABOUTME: Verifies DefaultThemeId visibility, parent-value collapse, and cache invalidation.

namespace Event.Application.UnitTests.Features.Appearance.Commands;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Appearance;
using Explore.Application.Features.Appearance.Handlers.Commands;
using Explore.Application.Features.Appearance.Requests.Commands;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Constants;
using NSubstitute;

public class UpdateCurrentUserAppearancePreferencesCommandHandlerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private static readonly Guid TestUserId = Guid.NewGuid();

    private readonly IUserPreferenceRepository _userPreferenceRepository;
    private readonly IHierarchicalSettingsResolver _hierarchicalSettingsResolver;
    private readonly IUiThemeRepository _uiThemeRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly UpdateCurrentUserAppearancePreferencesCommandHandler _handler;

    public UpdateCurrentUserAppearancePreferencesCommandHandlerTests()
    {
        _userPreferenceRepository = Substitute.For<IUserPreferenceRepository>();
        _hierarchicalSettingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _uiThemeRepository = Substitute.For<IUiThemeRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();

        _tenantContext.TenantId.Returns(TestTenantId);
        _currentUserService.UserId.Returns(TestUserId);
        _currentUserService.IsAuthenticated.Returns(true);

        _hierarchicalSettingsResolver
            .ResolveGroupAsync<AppearanceSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(new AppearanceSettingGroup());

        _userPreferenceRepository
            .GetByUserAndKey(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns((UserPreference?)null);

        _handler = new UpdateCurrentUserAppearancePreferencesCommandHandler(
            _userPreferenceRepository,
            _hierarchicalSettingsResolver,
            _uiThemeRepository,
            _tenantContext,
            _currentUserService);
    }

    [Test]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsFailure()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await _handler.Handle(
            new UpdateCurrentUserAppearancePreferencesCommand { Preferences = CreateDto() },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _userPreferenceRepository.DidNotReceive().Create(Arg.Any<UserPreference>());
        _hierarchicalSettingsResolver.DidNotReceive().InvalidateUserCache(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_WhenValuesMatchInheritedParent_RemovesOverridesInsteadOfCreating()
    {
        var dto = CreateDto();

        var result = await _handler.Handle(
            new UpdateCurrentUserAppearancePreferencesCommand { Preferences = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _userPreferenceRepository.Received().RemoveOverride(TestTenantId, TestUserId, GovernanceSettingKeys.Appearance.ThemeMode);
        await _userPreferenceRepository.Received().RemoveOverride(TestTenantId, TestUserId, GovernanceSettingKeys.Appearance.Direction);
        await _userPreferenceRepository.Received().RemoveOverride(TestTenantId, TestUserId, GovernanceSettingKeys.Appearance.Language);
        await _userPreferenceRepository.Received().RemoveOverride(TestTenantId, TestUserId, GovernanceSettingKeys.Appearance.LegacyDefaultThemeId);
        await _userPreferenceRepository.DidNotReceive().Create(Arg.Any<UserPreference>());
        _hierarchicalSettingsResolver.Received(1).InvalidateUserCache(TestTenantId, TestUserId);
    }

    [Test]
    public async Task Handle_WhenThemeModeDiffersFromParent_CreatesOverride()
    {
        var dto = CreateDto();
        dto.ThemeMode = "dark";

        var result = await _handler.Handle(
            new UpdateCurrentUserAppearancePreferencesCommand { Preferences = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _userPreferenceRepository.Received(1).Create(Arg.Is<UserPreference>(p =>
            p.UserId == TestUserId
            && p.TenantId == TestTenantId
            && p.SettingKey == GovernanceSettingKeys.Appearance.ThemeMode));
        await _userPreferenceRepository.DidNotReceive().RemoveOverride(TestTenantId, TestUserId, GovernanceSettingKeys.Appearance.ThemeMode);
    }

    [Test]
    public async Task Handle_WhenDefaultThemeIdReferencesInvisibleTheme_ReturnsFailureAndDoesNotPersist()
    {
        var invisibleThemeId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var dto = CreateDto();
        dto.DefaultThemeId = invisibleThemeId;

        _uiThemeRepository.GetById(invisibleThemeId).Returns(new UiTheme
        {
            Id = invisibleThemeId,
            TenantId = otherTenantId,
            ThemeKey = "other-tenant-theme",
            DisplayName = "Other Tenant Theme",
            IsActive = true,
            LightPalette = SamplePalette(),
            DarkPalette = SamplePalette(),
        });

        var result = await _handler.Handle(
            new UpdateCurrentUserAppearancePreferencesCommand { Preferences = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _userPreferenceRepository.DidNotReceive().Create(Arg.Any<UserPreference>());
        await _userPreferenceRepository.DidNotReceive().Update(Arg.Any<UserPreference>());
        _hierarchicalSettingsResolver.DidNotReceive().InvalidateUserCache(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_WhenDefaultThemeIdReferencesInactiveTheme_ReturnsFailure()
    {
        var themeId = Guid.NewGuid();
        var dto = CreateDto();
        dto.DefaultThemeId = themeId;

        _uiThemeRepository.GetById(themeId).Returns(new UiTheme
        {
            Id = themeId,
            TenantId = null,
            ThemeKey = "inactive-platform",
            DisplayName = "Inactive Platform",
            IsActive = false,
            LightPalette = SamplePalette(),
            DarkPalette = SamplePalette(),
        });

        var result = await _handler.Handle(
            new UpdateCurrentUserAppearancePreferencesCommand { Preferences = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
    }

    [Test]
    public async Task Handle_WhenDefaultThemeIdReferencesVisiblePlatformTheme_CreatesOverride()
    {
        var themeId = Guid.NewGuid();
        var dto = CreateDto();
        dto.DefaultThemeId = themeId;

        _uiThemeRepository.GetById(themeId).Returns(new UiTheme
        {
            Id = themeId,
            TenantId = null,
            ThemeKey = "platform-theme",
            DisplayName = "Platform Theme",
            IsActive = true,
            LightPalette = SamplePalette(),
            DarkPalette = SamplePalette(),
        });

        var result = await _handler.Handle(
            new UpdateCurrentUserAppearancePreferencesCommand { Preferences = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _userPreferenceRepository.Received(1).Create(Arg.Is<UserPreference>(p =>
            p.SettingKey == GovernanceSettingKeys.Appearance.LegacyDefaultThemeId
            && p.Value.Contains(themeId.ToString())));
        _hierarchicalSettingsResolver.Received(1).InvalidateUserCache(TestTenantId, TestUserId);
    }

    [Test]
    public async Task Handle_WhenValidationFails_ReturnsFailureWithErrors()
    {
        var dto = CreateDto();
        dto.ThemeMode = "neon";

        var result = await _handler.Handle(
            new UpdateCurrentUserAppearancePreferencesCommand { Preferences = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).IsNotNull();
        await _userPreferenceRepository.DidNotReceive().Create(Arg.Any<UserPreference>());
    }

    private static UpdateUserAppearancePreferencesDto CreateDto() => new()
    {
        ThemeMode = "system",
        Direction = "auto",
        Language = "en",
        DefaultThemeId = null,
    };

    private static Explore.Domain.ValueObjects.UiThemePalette SamplePalette() => new()
    {
        Primary = "#336699",
        PrimaryContrastText = "#FFFFFF",
        Secondary = "#112233",
        SecondaryContrastText = "#FFFFFF",
        Background = "#F8FAFC",
        Surface = "#FFFFFF",
        AppbarBackground = "rgba(51,102,153,0.85)",
        AppbarText = "#FFFFFF",
        DrawerBackground = "#0F172A",
        DrawerText = "#E2E8F0",
        DrawerIcon = "#CBD5E1",
        TextPrimary = "#0F172A",
        TextSecondary = "#64748B",
        Info = "#3B82F6",
        Success = "#10B981",
        Warning = "#F59E0B",
        Error = "#EF4444",
        LinesDefault = "#E2E8F0",
        Divider = "rgba(51,102,153,0.25)",
    };
}
