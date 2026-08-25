// ABOUTME: Tests effective ATProto event governance, fail-closed profile selection, and self-consent isolation.
// ABOUTME: Covers the tenant capability/profile matrix and generic-setting authorization against forged consent writes.

namespace Event.Application.UnitTests.Settings;

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Settings.Handlers.Commands;
using Explore.Application.Features.Settings.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Services.Federation;
using Explore.Application.Settings;
using Explore.Application.Settings.Groups;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using Explore.Domain.Settings.Definitions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

public sealed class AtprotoFederationGovernanceTests
{
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid CurrentUserId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000099");

    [Test]
    public async Task BackfillSettings_AreRegisteredAndExposedByTheTypedGroup()
    {
        const string enabledKey = "federation.atproto_events_backfill_enabled";
        const string modeKey = "federation.atproto_events_backfill_mode";

        var enabled = SettingRegistry.Get(enabledKey);
        var mode = SettingRegistry.Get(modeKey);
        var settingKeys = AtprotoFederationSettingGroup.SettingKeys.ToArray();

        await Assert.That(enabled).IsNotNull();
        await Assert.That(enabled!.ValueType).IsEqualTo(SettingValueType.Boolean);
        await Assert.That(enabled.DefaultValue).IsEqualTo("false");
        await Assert.That(enabled.MaxScope).IsEqualTo(SettingScope.Tenant);
        await Assert.That(enabled.IsLockable).IsTrue();
        await Assert.That(mode).IsNotNull();
        await Assert.That(mode!.ValueType).IsEqualTo(SettingValueType.String);
        await Assert.That(mode.DefaultValue).IsEqualTo("\"downtime_only\"");
        await Assert.That(mode.AllowedValues).IsEquivalentTo(["downtime_only", "full"]);
        await Assert.That(mode.MaxScope).IsEqualTo(SettingScope.Tenant);
        await Assert.That(mode.IsLockable).IsTrue();
        await Assert.That(settingKeys).Contains(enabledKey);
        await Assert.That(settingKeys).Contains(modeKey);
        await Assert.That(AtprotoFederationSettingDefinitions.AdministratorKeys).Contains(enabledKey);
        await Assert.That(AtprotoFederationSettingDefinitions.AdministratorKeys).Contains(modeKey);
        await Assert.That(typeof(AtprotoFederationSettingGroup).GetProperty("EventsBackfillEnabled")).IsNotNull();
        await Assert.That(typeof(AtprotoFederationSettingGroup).GetProperty("EventsBackfillMode")).IsNotNull();
    }

    [Test]
    [Arguments("federation.atproto_events_backfill_enabled", "yes")]
    [Arguments("federation.atproto_events_backfill_mode", "continuous")]
    public async Task BackfillWrite_MalformedValueIsRejectedBeforePersistence(string key, string value)
    {
        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        var tenantContext = Substitute.For<ITenantContext>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var admin = Substitute.For<IAdminContext>();
        tenantContext.TenantId.Returns(TenantId);
        currentUser.UserId.Returns(CurrentUserId);
        currentUser.IsAuthenticated.Returns(true);
        admin.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateSettingCommandHandler(
            settingsResolver,
            Substitute.For<IUserPreferenceRepository>(),
            tenantContext,
            currentUser,
            admin,
            Substitute.For<IMediator>(),
            Substitute.For<ILogger<UpdateSettingCommandHandler>>());

        var result = await handler.Handle(new UpdateSettingCommand
        {
            Key = key,
            Value = value,
            Scope = SettingScope.Instance
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await settingsResolver.DidNotReceiveWithAnyArgs().SetValueAsync(default!, default!, default, default, default);
    }

    [Test]
    [Arguments(false, "platform", false, "platform")]
    [Arguments(false, "community_lexicon", true, "platform")]
    [Arguments(true, "platform", true, "platform")]
    [Arguments(true, "community_lexicon", false, "community_lexicon")]
    [Arguments(true, "COMMUNITY_LEXICON", true, "community_lexicon")]
    [Arguments(true, "Community_Lexicon", false, "community_lexicon")]
    [Arguments(true, "unknown", true, "platform")]
    [Arguments(true, "", true, "platform")]
    public async Task ResolveAsync_UsesOneCapabilityAndFailsClosedForUnknownOrDisabledProfiles(
        bool enabled,
        string storedProfile,
        bool consent,
        string expectedProfile)
    {
        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        settingsResolver.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(call => ResolveBatch(call.ArgAt<IEnumerable<string>>(0), call.ArgAt<SettingContext>(1), enabled, storedProfile, consent));
        var resolver = new AtprotoEventGovernanceResolver(settingsResolver);

        AtprotoEventGovernance result = await resolver.ResolveAsync(TenantId, CurrentUserId, CancellationToken.None);

        await Assert.That(result.EventsEnabled).IsEqualTo(enabled);
        await Assert.That(result.ValidationProfile).IsEqualTo(expectedProfile);
        await Assert.That(result.PublishMyEvents).IsEqualTo(consent);
    }

    [Test]
    [Arguments(SettingScope.Instance)]
    [Arguments(SettingScope.Tenant)]
    public async Task ConsentWrite_ForgedAdministratorScopeIsDeniedWithoutPersistence(SettingScope forgedScope)
    {
        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        var userPreferences = Substitute.For<IUserPreferenceRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var admin = Substitute.For<IAdminContext>();
        tenantContext.TenantId.Returns(TenantId);
        currentUser.UserId.Returns(CurrentUserId);
        currentUser.IsAuthenticated.Returns(true);
        admin.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        admin.IsTenantAdminAsync(TenantId, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateSettingCommandHandler(
            settingsResolver,
            userPreferences,
            tenantContext,
            currentUser,
            admin,
            Substitute.For<IMediator>(),
            Substitute.For<ILogger<UpdateSettingCommandHandler>>());

        var result = await handler.Handle(new UpdateSettingCommand
        {
            Key = GovernanceSettingKeys.Federation.AtprotoPublishMyEvents,
            Value = "true",
            Scope = forgedScope
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await userPreferences.DidNotReceiveWithAnyArgs().Create(default!);
        await settingsResolver.DidNotReceiveWithAnyArgs().SetValueAsync(default!, default!, default, default, default);
    }

    [Test]
    [Arguments(GovernanceSettingKeys.Federation.AtprotoEventsEnabled, "true")]
    [Arguments(GovernanceSettingKeys.Federation.AtprotoEventValidationProfile, "\"community_lexicon\"")]
    public async Task AdministratorPolicyWrite_UserScopeIsDeniedWithoutPreferencePersistence(string key, string value)
    {
        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        var userPreferences = Substitute.For<IUserPreferenceRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var admin = Substitute.For<IAdminContext>();
        tenantContext.TenantId.Returns(TenantId);
        currentUser.UserId.Returns(CurrentUserId);
        currentUser.IsAuthenticated.Returns(true);
        admin.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateSettingCommandHandler(
            settingsResolver,
            userPreferences,
            tenantContext,
            currentUser,
            admin,
            Substitute.For<IMediator>(),
            Substitute.For<ILogger<UpdateSettingCommandHandler>>());

        var result = await handler.Handle(new UpdateSettingCommand
        {
            Key = key,
            Value = value,
            Scope = SettingScope.User
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await userPreferences.DidNotReceiveWithAnyArgs().Create(default!);
        await settingsResolver.DidNotReceiveWithAnyArgs().SetValueAsync(default!, default!, default, default, default);
    }

    [Test]
    public async Task ConsentWrite_CurrentUserPersistsOnlyOwnPreferenceAndInvalidatesOwnCache()
    {
        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        var userPreferences = Substitute.For<IUserPreferenceRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var admin = Substitute.For<IAdminContext>();
        tenantContext.TenantId.Returns(TenantId);
        currentUser.UserId.Returns(CurrentUserId);
        currentUser.IsAuthenticated.Returns(true);
        settingsResolver.ResolveWithMetadataAsync(
                GovernanceSettingKeys.Federation.AtprotoPublishMyEvents,
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns((ResolvedSetting?)null);
        var handler = new UpdateSettingCommandHandler(
            settingsResolver,
            userPreferences,
            tenantContext,
            currentUser,
            admin,
            Substitute.For<IMediator>(),
            Substitute.For<ILogger<UpdateSettingCommandHandler>>());

        var result = await handler.Handle(new UpdateSettingCommand
        {
            Key = GovernanceSettingKeys.Federation.AtprotoPublishMyEvents,
            Value = "true",
            Scope = SettingScope.User
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await userPreferences.Received(1).Create(Arg.Is<UserPreference>(preference =>
            preference.TenantId == TenantId
            && preference.UserId == CurrentUserId
            && preference.SettingKey == GovernanceSettingKeys.Federation.AtprotoPublishMyEvents
            && preference.Value == "true"));
        settingsResolver.Received(1).InvalidateUserCache(TenantId, CurrentUserId);
    }

    [Test]
    public async Task CapabilityUnlock_InstanceAdminUsesGenericLockPipelineAndInvalidatesInstanceCache()
    {
        var settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
        var tenantContext = Substitute.For<ITenantContext>();
        var currentUser = Substitute.For<ICurrentUserService>();
        var admin = Substitute.For<IAdminContext>();
        tenantContext.TenantId.Returns(TenantId);
        currentUser.UserId.Returns(CurrentUserId);
        currentUser.IsAuthenticated.Returns(true);
        admin.IsInstanceAdminAsync(Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UnlockSettingCommandHandler(
            settingsResolver,
            tenantContext,
            currentUser,
            admin,
            Substitute.For<IMediator>(),
            Substitute.For<ILogger<UnlockSettingCommandHandler>>());

        var result = await handler.Handle(new UnlockSettingCommand
        {
            Key = GovernanceSettingKeys.Federation.AtprotoEventsEnabled,
            Scope = SettingScope.Instance
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await settingsResolver.Received(1).UnlockAsync(
            GovernanceSettingKeys.Federation.AtprotoEventsEnabled,
            SettingScope.Instance,
            Guid.Empty,
            CurrentUserId,
            Arg.Any<CancellationToken>());
        settingsResolver.Received(1).InvalidateCache(SettingScope.Instance, Guid.Empty);
    }

    private static IReadOnlyList<ResolvedSetting> ResolveBatch(
        IEnumerable<string> keys,
        SettingContext context,
        bool enabled,
        string profile,
        bool consent)
    {
        return keys.Select(key => key switch
        {
            var value when value == GovernanceSettingKeys.Federation.AtprotoEventsEnabled => Setting(key, enabled ? "true" : "false", SettingValueType.Boolean),
            var value when value == GovernanceSettingKeys.Federation.AtprotoEventValidationProfile => Setting(key, $"\"{profile}\"", SettingValueType.String),
            var value when value == GovernanceSettingKeys.Federation.AtprotoPublishMyEvents && context.UserId == CurrentUserId => Setting(key, consent ? "true" : "false", SettingValueType.Boolean),
            _ => Setting(key, "false", SettingValueType.Boolean)
        }).ToArray();
    }

    private static ResolvedSetting Setting(string key, string value, SettingValueType valueType) => new()
    {
        Key = key,
        Value = value,
        ValueType = valueType,
        Source = SettingSource.SystemDefault
    };
}
