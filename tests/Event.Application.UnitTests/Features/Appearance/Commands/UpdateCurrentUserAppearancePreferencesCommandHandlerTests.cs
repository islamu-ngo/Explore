// ABOUTME: Unit tests for privacy-fenced appearance-preference sparse overrides.
// ABOUTME: Verifies atomic writes, theme visibility, inherited-value collapse, and cache invalidation.

namespace Event.Application.UnitTests.Features.Appearance.Commands;

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Appearance;
using Explore.Application.Features.Appearance.Handlers.Commands;
using Explore.Application.Features.Appearance.Requests.Commands;
using Explore.Application.Responses;
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
    private readonly IPrivacyErasureStateRepository _privacyErasureStateRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateCurrentUserAppearancePreferencesCommandHandler _handler;

    public UpdateCurrentUserAppearancePreferencesCommandHandlerTests()
    {
        _userPreferenceRepository = Substitute.For<IUserPreferenceRepository>();
        _hierarchicalSettingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        _uiThemeRepository = Substitute.For<IUiThemeRepository>();
        _privacyErasureStateRepository = Substitute.For<IPrivacyErasureStateRepository>();
        _tenantContext = Substitute.For<ITenantContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _tenantContext.TenantId.Returns(TestTenantId);
        _currentUserService.UserId.Returns(TestUserId);
        _currentUserService.IsAuthenticated.Returns(true);

        _hierarchicalSettingsResolver
            .ResolveGroupAsync<AppearanceSettingGroup>(Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(new AppearanceSettingGroup());

        _userPreferenceRepository
            .GetByUserAndKey(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns((UserPreference?)null);
        _unitOfWork
            .ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>()(CancellationToken.None));

        _handler = new UpdateCurrentUserAppearancePreferencesCommandHandler(
            _userPreferenceRepository,
            _hierarchicalSettingsResolver,
            _uiThemeRepository,
            _privacyErasureStateRepository,
            _tenantContext,
            _currentUserService,
            _unitOfWork);
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
    public async Task Handle_WhenFenceAppearsBeforeTransactionDoesNotPersistOverrides()
    {
        _privacyErasureStateRepository
            .GetBySubjectAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null, CreatePrivacyErasureSaga());
        var dto = CreateDto();
        dto.ThemeMode = "dark";

        BaseCommandResponse<Guid> result = await _handler.Handle(
            new UpdateCurrentUserAppearancePreferencesCommand { Preferences = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Appearance preference update failed.");
        await Assert.That(result.Errors).IsNull();
        await _privacyErasureStateRepository.Received(2)
            .GetBySubjectAsync(TestUserId, Arg.Any<CancellationToken>());
        await _userPreferenceRepository.DidNotReceive().Create(Arg.Any<UserPreference>());
        await _userPreferenceRepository.DidNotReceive().Update(Arg.Any<UserPreference>());
        await _userPreferenceRepository.DidNotReceive().RemoveOverride(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>());
        _hierarchicalSettingsResolver.DidNotReceive().InvalidateUserCache(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_WhenFenceAppearsDuringValidationMasksDetailedErrors()
    {
        _privacyErasureStateRepository
            .GetBySubjectAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns((PrivacyErasureSaga?)null, CreatePrivacyErasureSaga());
        var dto = CreateDto();
        dto.ThemeMode = "neon";

        BaseCommandResponse<Guid> result = await _handler.Handle(
            new UpdateCurrentUserAppearancePreferencesCommand { Preferences = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Appearance preference update failed.");
        await Assert.That(result.Errors).IsNull();
        await _unitOfWork.DidNotReceive().ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        await _userPreferenceRepository.DidNotReceive().Create(Arg.Any<UserPreference>());
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

        _uiThemeRepository.GetByIdAsync(invisibleThemeId, Arg.Any<CancellationToken>()).Returns(new UiTheme
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

        _uiThemeRepository.GetByIdAsync(themeId, Arg.Any<CancellationToken>()).Returns(new UiTheme
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
        using var cancellation = new CancellationTokenSource();

        _uiThemeRepository.GetByIdAsync(themeId, Arg.Any<CancellationToken>()).Returns(new UiTheme
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
            cancellation.Token);

        await Assert.That(result.Success).IsTrue();
        await _userPreferenceRepository.Received(1).Create(Arg.Is<UserPreference>(p =>
            p.SettingKey == GovernanceSettingKeys.Appearance.LegacyDefaultThemeId
            && p.Value.Contains(themeId.ToString())));
        await _uiThemeRepository.Received(1).GetByIdAsync(themeId, cancellation.Token);
        _hierarchicalSettingsResolver.Received(1).InvalidateUserCache(TestTenantId, TestUserId);
    }

    [Test]
    public async Task Handle_WhenAllOverridesDifferUsesOneRetryStableTimestamp()
    {
        var themeId = Guid.CreateVersion7();
        var dto = CreateDto();
        dto.ThemeMode = "dark";
        dto.Direction = "rtl";
        dto.Language = "ar";
        dto.DefaultThemeId = themeId;
        _uiThemeRepository.GetByIdAsync(themeId, Arg.Any<CancellationToken>())
            .Returns(CreateActivePlatformTheme(themeId));
        var created = new List<UserPreference>();
        _userPreferenceRepository.Create(Arg.Do<UserPreference>(created.Add))
            .Returns(call => call.Arg<UserPreference>());

        var result = await _handler.Handle(
            new UpdateCurrentUserAppearancePreferencesCommand { Preferences = dto },
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(created.Count).IsEqualTo(4);
        await Assert.That(created.Select(preference => preference.CreatedAt).Distinct().Count()).IsEqualTo(1);
        await _unitOfWork.Received(1).ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenSecondOverrideWriteFailsDoesNotInvalidateCacheOutsideTransaction()
    {
        var themeId = Guid.CreateVersion7();
        var dto = CreateDto();
        dto.ThemeMode = "dark";
        dto.Direction = "rtl";
        dto.Language = "ar";
        dto.DefaultThemeId = themeId;
        _uiThemeRepository.GetByIdAsync(themeId, Arg.Any<CancellationToken>())
            .Returns(CreateActivePlatformTheme(themeId));
        var createCount = 0;
        _userPreferenceRepository.Create(Arg.Any<UserPreference>()).Returns(call =>
        {
            if (++createCount == 2)
            {
                throw new InvalidOperationException("simulated transaction conflict");
            }

            return call.Arg<UserPreference>();
        });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(
                new UpdateCurrentUserAppearancePreferencesCommand { Preferences = dto },
                CancellationToken.None));

        await _unitOfWork.Received(1).ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
        _hierarchicalSettingsResolver.DidNotReceive().InvalidateUserCache(Arg.Any<Guid>(), Arg.Any<Guid>());
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

    private static PrivacyErasureSaga CreatePrivacyErasureSaga()
    {
        DateTime nowUtc = DateTime.UtcNow;
        PrivacyErasureIntent intent = PrivacyErasureIntent.Record(
            Guid.CreateVersion7(),
            1,
            PrivacyErasureSubjectKind.User,
            TestUserId,
            PrivacyErasureReasonCode.AccountDeletion,
            1,
            nowUtc,
            nowUtc);
        return PrivacyErasureSaga.Start(intent, 1, new byte[32], nowUtc.AddMinutes(5), nowUtc);
    }

    private static UiTheme CreateActivePlatformTheme(Guid themeId) => new()
    {
        Id = themeId,
        TenantId = null,
        ThemeKey = "platform-theme",
        DisplayName = "Platform Theme",
        IsActive = true,
        LightPalette = SamplePalette(),
        DarkPalette = SamplePalette()
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
