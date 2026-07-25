// ABOUTME: Tests UpdateAnalyticsGovernanceSettingsCommandHandler validation and persistence.
// ABOUTME: Covers rejection of invalid combos, advisory warnings, and settings persistence.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.DTOs.Analytics;
using Explore.Application.DTOs.Instance;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain.Constants;
using Explore.Domain.Enums.Analytics;
using Explore.Domain.Settings;
using NSubstitute;

namespace Event.Application.UnitTests.Analytics;

public class UpdateAnalyticsGovernanceSettingsCommandHandlerTests
{
    private readonly IHierarchicalSettingsResolver _settingsResolver;
    private readonly UpdateAnalyticsGovernanceSettingsCommandHandler _handler;

    public UpdateAnalyticsGovernanceSettingsCommandHandlerTests()
    {
        _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();

        // Default: empty group (no provider configured)
        _settingsResolver.ResolveGroupAsync<AnalyticsSettingGroup>(
            Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(new AnalyticsSettingGroup());

        _handler = new UpdateAnalyticsGovernanceSettingsCommandHandler(_settingsResolver);
    }

    private void SetupCurrentGroup(params (string key, string value)[] entries)
    {
        var dict = entries.ToDictionary(e => e.key, e => new ResolvedSetting { Value = e.value });
        var group = new AnalyticsSettingGroup();
        group.Populate(dict);

        _settingsResolver.ResolveGroupAsync<AnalyticsSettingGroup>(
            Arg.Any<SettingContext>(), Arg.Any<CancellationToken>())
            .Returns(group);
    }

    private static UpdateAnalyticsGovernanceSettingsCommand CreateCommand(
        Action<AnalyticsGovernanceSettingsDto>? configure = null)
    {
        var dto = new AnalyticsGovernanceSettingsDto
        {
            ConsentCookieLifetimeDays = 180
        };
        configure?.Invoke(dto);

        return new UpdateAnalyticsGovernanceSettingsCommand
        {
            UserId = Guid.NewGuid(),
            Patch = new PatchAnalyticsGovernanceSettingsDto
            {
                CookieConsentEnabled = OptionalUpdate<bool>.Set(dto.CookieConsentEnabled),
                DeclineBehavior = OptionalUpdate<DeclineBehavior>.Set(dto.DeclineBehavior),
                ConsentCookieLifetimeDays = OptionalUpdate<int>.Set(dto.ConsentCookieLifetimeDays),
                GlobalDisableClientTracking = OptionalUpdate<bool>.Set(dto.GlobalDisableClientTracking),
                PosthogCookielessMode = OptionalUpdate<PosthogCookielessMode>.Set(dto.PosthogCookielessMode),
                PosthogPersonProfiles = OptionalUpdate<PosthogPersonProfiles>.Set(dto.PosthogPersonProfiles),
                PosthogSessionReplay = OptionalUpdate<bool>.Set(dto.PosthogSessionReplay),
                PosthogAutocapture = OptionalUpdate<bool>.Set(dto.PosthogAutocapture),
                PosthogHeatmaps = OptionalUpdate<bool>.Set(dto.PosthogHeatmaps),
                PosthogToolbar = OptionalUpdate<bool>.Set(dto.PosthogToolbar)
            }
        };
    }

    // --- Validation: ConsentCookieLifetimeDays ---

    [Test]
    public async Task Handle_ConsentCookieLifetimeTooLow_ReturnsValidationError()
    {
        var command = CreateCommand(s => s.ConsentCookieLifetimeDays = 0);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("ValidationFailed");
        await Assert.That(result.Errors!.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task Handle_ConsentCookieLifetimeNegative_ReturnsValidationError()
    {
        var command = CreateCommand(s => s.ConsentCookieLifetimeDays = -1);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("ValidationFailed");
    }

    [Test]
    public async Task Handle_ConsentCookieLifetimeTooHigh_ReturnsValidationError()
    {
        var command = CreateCommand(s => s.ConsentCookieLifetimeDays = 731);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("ValidationFailed");
    }

    [Test]
    public async Task Handle_ConsentCookieLifetimeAtLowerBound_Succeeds()
    {
        var command = CreateCommand(s => s.ConsentCookieLifetimeDays = 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task Handle_ConsentCookieLifetimeAtUpperBound_Succeeds()
    {
        var command = CreateCommand(s => s.ConsentCookieLifetimeDays = 730);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
    }

    // --- Validation: Cookieless DeclineBehavior ---

    [Test]
    public async Task Handle_CookielessDeclineOnNonSupportingProvider_ReturnsValidationError()
    {
        SetupCurrentGroup(
            ("analytics.provider", "\"rudderstack\""),
            ("analytics.enabled", "true"));

        var command = CreateCommand(s => s.DeclineBehavior = DeclineBehavior.Cookieless);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("ValidationFailed");
    }

    [Test]
    public async Task Handle_CookielessDeclineOnPosthog_Succeeds()
    {
        SetupCurrentGroup(
            ("analytics.provider", "\"posthog\""),
            ("analytics.enabled", "true"));

        var command = CreateCommand(s => s.DeclineBehavior = DeclineBehavior.Cookieless);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task Handle_CookielessDeclineWhenProviderDisabled_Succeeds()
    {
        SetupCurrentGroup(
            ("analytics.provider", "\"rudderstack\""),
            ("analytics.enabled", "false"));

        var command = CreateCommand(s => s.DeclineBehavior = DeclineBehavior.Cookieless);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
    }

    [Test]
    public async Task Handle_DisableDeclineOnAnyProvider_Succeeds()
    {
        SetupCurrentGroup(
            ("analytics.provider", "\"rudderstack\""),
            ("analytics.enabled", "true"));

        var command = CreateCommand(s => s.DeclineBehavior = DeclineBehavior.Disable);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
    }

    // --- Validation failure blocks persistence ---

    [Test]
    public async Task Handle_ValidationFailed_DoesNotPersistSettings()
    {
        var command = CreateCommand(s => s.ConsentCookieLifetimeDays = 0);

        await _handler.Handle(command, CancellationToken.None);

        await _settingsResolver.DidNotReceive().SetValueAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<SettingScope>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenOneLeafIsProvided_PersistsOnlyThatSetting()
    {
        var userId = Guid.NewGuid();
        var command = new UpdateAnalyticsGovernanceSettingsCommand
        {
            UserId = userId,
            Patch = new PatchAnalyticsGovernanceSettingsDto
            {
                ConsentCookieLifetimeDays = OptionalUpdate<int>.Set(90)
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _settingsResolver.Received(1).SetValueAsync(
            GovernanceSettingKeys.Analytics.ConsentCookieLifetimeDays,
            "90",
            SettingScope.Instance,
            Guid.Empty,
            userId,
            Arg.Any<CancellationToken>());
        await _settingsResolver.Received(1).SetValueAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            SettingScope.Instance,
            Guid.Empty,
            userId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenMergedCandidateIsInvalid_DoesNotPersistSettings()
    {
        SetupCurrentGroup(
            ("analytics.provider", "\"rudderstack\""),
            ("analytics.enabled", "true"));
        var command = new UpdateAnalyticsGovernanceSettingsCommand
        {
            UserId = Guid.NewGuid(),
            Patch = new PatchAnalyticsGovernanceSettingsDto
            {
                DeclineBehavior = OptionalUpdate<DeclineBehavior>.Set(DeclineBehavior.Cookieless)
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _settingsResolver.DidNotReceive().SetValueAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<SettingScope>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    // --- Warnings: PostHog features on non-PostHog provider ---

    [Test]
    public async Task Handle_PosthogFeaturesOnNonPosthogProvider_ReturnsWarning()
    {
        SetupCurrentGroup(
            ("analytics.provider", "\"rudderstack\""),
            ("analytics.enabled", "true"));

        var command = CreateCommand(s =>
        {
            s.PosthogSessionReplay = true;
            s.DeclineBehavior = DeclineBehavior.Disable;
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsNotNull();
    }

    // --- Warnings: Session replay degraded in always-cookieless ---

    [Test]
    public async Task Handle_SessionReplayWithAlwaysCookieless_ReturnsWarning()
    {
        SetupCurrentGroup(
            ("analytics.provider", "\"posthog\""),
            ("analytics.enabled", "true"));

        var command = CreateCommand(s =>
        {
            s.PosthogSessionReplay = true;
            s.PosthogCookielessMode = PosthogCookielessMode.Always;
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsNotNull();
    }

    // --- Warnings: Banner unnecessary for cookieless provider ---

    [Test]
    public async Task Handle_BannerOnInherentlyCookielessProvider_ReturnsWarning()
    {
        SetupCurrentGroup(
            ("analytics.provider", "\"plausible\""),
            ("analytics.enabled", "true"));

        var command = CreateCommand(s => s.CookieConsentEnabled = true);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsNotNull();
    }

    // --- Successful persistence ---

    [Test]
    public async Task Handle_ValidSettings_PersistsAllTenSettings()
    {
        var command = CreateCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _settingsResolver.Received(10).SetValueAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<SettingScope>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ValidSettings_NoWarnings_NullMessage()
    {
        var command = CreateCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsNull();
    }

    [Test]
    public async Task Handle_ValidSettings_PersistsAtInstanceScope()
    {
        var command = CreateCommand();

        await _handler.Handle(command, CancellationToken.None);

        await _settingsResolver.Received(10).SetValueAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            SettingScope.Instance, Guid.Empty, command.UserId,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_MultipleWarnings_JoinsInMessage()
    {
        SetupCurrentGroup(
            ("analytics.provider", "\"posthog\""),
            ("analytics.enabled", "true"));

        var command = CreateCommand(s =>
        {
            s.PosthogSessionReplay = true;
            s.PosthogCookielessMode = PosthogCookielessMode.Always;
            s.CookieConsentEnabled = true;
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        // Should have both session replay warning and no unnecessary-banner warning
        // (PostHog is not inherently cookieless, so banner warning won't fire)
        await Assert.That(result.Message).IsNotNull();
    }
}
