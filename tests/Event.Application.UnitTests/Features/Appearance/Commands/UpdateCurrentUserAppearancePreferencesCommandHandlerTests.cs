// ABOUTME: Unit tests for privacy-fenced grouped appearance localization updates.
// ABOUTME: Verifies omitted localization leaves remain untouched and cache invalidation follows commit.

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

    private readonly IUserPreferenceRepository _userPreferenceRepository = Substitute.For<IUserPreferenceRepository>();
    private readonly IHierarchicalSettingsResolver _hierarchicalSettingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly IPrivacyErasureStateRepository _privacyErasureStateRepository = Substitute.For<IPrivacyErasureStateRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateCurrentUserAppearancePreferencesCommandHandler _handler;

    public UpdateCurrentUserAppearancePreferencesCommandHandlerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(TestTenantId);
        _currentUserService.UserId.Returns(TestUserId);

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
            _privacyErasureStateRepository,
            tenantContext,
            _currentUserService,
            _unitOfWork);
    }

    [Test]
    public async Task Handle_WhenWrapperIsEmpty_RejectsWithoutTransaction()
    {
        var result = await HandleAsync(new UpdateUserAppearancePreferencesDto());

        await Assert.That(result.Success).IsFalse();
        await _unitOfWork.DidNotReceive().ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenLocalizationIsEmpty_RejectsWithoutMutation()
    {
        var result = await HandleAsync(new UpdateUserAppearancePreferencesDto
        {
            Localization = new UpdateAppearanceLocalizationDto()
        });

        await Assert.That(result.Success).IsFalse();
        await _userPreferenceRepository.DidNotReceive().Create(Arg.Any<UserPreference>());
    }

    [Test]
    public async Task Handle_WhenOnlyDirectionIsSupplied_DoesNotWriteLanguage()
    {
        var result = await HandleAsync(new UpdateUserAppearancePreferencesDto
        {
            Localization = new UpdateAppearanceLocalizationDto { Direction = "rtl" }
        });

        await Assert.That(result.Success).IsTrue();
        await _userPreferenceRepository.Received(1).Create(Arg.Is<UserPreference>(preference =>
            preference.SettingKey == GovernanceSettingKeys.Appearance.Direction));
        await _userPreferenceRepository.DidNotReceive().Create(Arg.Is<UserPreference>(preference =>
            preference.SettingKey == GovernanceSettingKeys.Appearance.Language));
        _hierarchicalSettingsResolver.Received(1).InvalidateUserCache(TestTenantId, TestUserId);
    }

    [Test]
    public async Task Handle_WhenOnlyLanguageMatchesParent_RemovesOnlyLanguageOverride()
    {
        var result = await HandleAsync(new UpdateUserAppearancePreferencesDto
        {
            Localization = new UpdateAppearanceLocalizationDto { Language = "en" }
        });

        await Assert.That(result.Success).IsTrue();
        await _userPreferenceRepository.Received(1).RemoveOverride(
            TestTenantId,
            TestUserId,
            GovernanceSettingKeys.Appearance.Language);
        await _userPreferenceRepository.DidNotReceive().RemoveOverride(
            TestTenantId,
            TestUserId,
            GovernanceSettingKeys.Appearance.Direction);
    }

    [Test]
    public async Task Handle_WhenUserIsFenced_DoesNotPersist()
    {
        _privacyErasureStateRepository
            .GetBySubjectAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(CreatePrivacyErasureSaga());

        var result = await HandleAsync(new UpdateUserAppearancePreferencesDto
        {
            Localization = new UpdateAppearanceLocalizationDto { Direction = "ltr" }
        });

        await Assert.That(result.Success).IsFalse();
        await _userPreferenceRepository.DidNotReceive().Create(Arg.Any<UserPreference>());
        _hierarchicalSettingsResolver.DidNotReceive().InvalidateUserCache(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    private Task<BaseCommandResponse<Guid>> HandleAsync(UpdateUserAppearancePreferencesDto preferences) =>
        _handler.Handle(
            new UpdateCurrentUserAppearancePreferencesCommand { Preferences = preferences },
            CancellationToken.None);

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
}
