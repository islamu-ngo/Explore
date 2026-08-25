// ABOUTME: Security-boundary tests for direct Control Plane tenant setting mutation handlers.
// ABOUTME: Proves registry, scope, sensitivity, lock, value, and transition denials occur before writes.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Handlers.Commands;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.ControlPlane.Commands;

public sealed class ControlPlaneTenantSettingCommandHandlerTests
{
    private readonly ITenantSettingRepository _tenantSettings = Substitute.For<ITenantSettingRepository>();
    private readonly ISystemSettingRepository _systemSettings = Substitute.For<ISystemSettingRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ISettingMutationLock _mutationLock = new ImmediateSettingMutationLock();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    public ControlPlaneTenantSettingCommandHandlerTests()
    {
        _currentUserService.UserId.Returns(_actorUserId);
        _currentUserService.IsAuthenticated.Returns(true);
    }

    [Test]
    [Arguments("unknown.setting", "setting_not_found")]
    [Arguments(GovernanceSettingKeys.AdminPortal.Enabled, "setting_scope_not_supported")]
    [Arguments("email.smtp_password", "sensitive_setting_not_supported")]
    public async Task Set_DeniesInvalidTargetBeforeRepositoryAccess(string key, string failureCode)
    {
        var handler = new SetControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "value"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(failureCode);
        await _systemSettings.DidNotReceiveWithAnyArgs().IsLocked(default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().GetByTenantAndKey(default, default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().UpsertManyForTenantAsync(default, default!, default);
    }

    [Test]
    public async Task Set_DeniesSystemLockedSettingBeforeTenantRepositoryAccess()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new SetControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "smtp.example.test"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("setting_system_locked");
        await _tenantSettings.DidNotReceiveWithAnyArgs().GetByTenantAndKey(default, default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().UpsertManyForTenantAsync(default, default!, default);
    }

    [Test]
    [Arguments(GovernanceSettingKeys.Email.SmtpSkipCertValidation, "not-bool")]
    [Arguments(GovernanceSettingKeys.Email.SmtpPort, "12.5")]
    [Arguments(GovernanceSettingKeys.Email.SmtpSecurity, "plaintext")]
    [Arguments(GovernanceSettingKeys.PublicExperience.HomeBlocks, "{invalid-json")]
    public async Task Set_DeniesInvalidValueBeforeTenantRepositoryAccess(string key, string value)
    {
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new SetControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, value),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("setting_validation_failed");
        await _tenantSettings.DidNotReceiveWithAnyArgs().GetByTenantAndKey(default, default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().UpsertManyForTenantAsync(default, default!, default);
    }

    [Test]
    public async Task Set_ValidValueUsesValueOnlyMutation()
    {
        const string key = GovernanceSettingKeys.Email.SmtpSkipCertValidation;
        var cancellationToken = new CancellationTokenSource().Token;
        _systemSettings.IsLocked(key, cancellationToken).Returns(false);
        var handler = new SetControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "true"),
            cancellationToken);

        await Assert.That(result.IsSuccess).IsTrue();
        await _tenantSettings.Received(1).SetValueAsync(_tenantId, key, "true", cancellationToken, _actorUserId);
        await _mediator.Received(1).Publish(
            Arg.Is<Explore.Application.Notifications.SettingChangedNotification>(notification =>
                notification.ActorUserId == _actorUserId),
            cancellationToken);
    }

    [Test]
    public async Task Set_ValidStringUsesJsonStorageFormat()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new SetControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "smtp.example.test"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _tenantSettings.Received(1).SetValueAsync(
            _tenantId,
            key,
            "\"smtp.example.test\"",
            Arg.Any<CancellationToken>(),
            _actorUserId);
    }

    [Test]
    public async Task Set_PropagatesCancellationWithoutConvertingToSuccess()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _systemSettings.IsLocked(key, cancellation.Token).Returns(false);
        _tenantSettings.SetValueAsync(_tenantId, key, Arg.Any<string>(), cancellation.Token, _actorUserId)
            .Returns(Task.FromCanceled(cancellation.Token));
        var handler = new SetControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);

        await Assert.That(async () => await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "smtp.example.test"),
            cancellation.Token)).Throws<OperationCanceledException>();
    }

    [Test]
    [Arguments("unknown.setting", "setting_not_found")]
    [Arguments(GovernanceSettingKeys.AdminPortal.Enabled, "setting_scope_not_supported")]
    [Arguments("email.smtp_password", "sensitive_setting_not_supported")]
    public async Task LockAndUnlock_DenyInvalidTargetBeforeRepositoryAccess(string key, string failureCode)
    {
        var lockHandler = new LockControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);
        var unlockHandler = new UnlockControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);

        var lockResult = await lockHandler.Handle(
            new LockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);
        var unlockResult = await unlockHandler.Handle(
            new UnlockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);

        await Assert.That(lockResult.IsSuccess).IsFalse();
        await Assert.That(lockResult.FailureCode).IsEqualTo(failureCode);
        await Assert.That(unlockResult.IsSuccess).IsFalse();
        await Assert.That(unlockResult.FailureCode).IsEqualTo(failureCode);
        await _systemSettings.DidNotReceiveWithAnyArgs().IsLocked(default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().GetByTenantAndKey(default, default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().LockAsync(default, default!, default, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().UnlockAsync(default, default!, default);
    }

    [Test]
    public async Task LockAndUnlock_DenySystemLockedTargetBeforeTenantRepositoryAccess()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(true);
        var lockHandler = new LockControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);
        var unlockHandler = new UnlockControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);

        var lockResult = await lockHandler.Handle(
            new LockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);
        var unlockResult = await unlockHandler.Handle(
            new UnlockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);

        await Assert.That(lockResult.FailureCode).IsEqualTo("setting_system_locked");
        await Assert.That(unlockResult.FailureCode).IsEqualTo("setting_system_locked");
        await _tenantSettings.DidNotReceiveWithAnyArgs().GetByTenantAndKey(default, default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().LockAsync(default, default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().UnlockAsync(default, default!, default);
    }

    [Test]
    [Arguments(false, "setting_override_not_found")]
    [Arguments(true, "setting_state_conflict")]
    public async Task Lock_DeniesMissingOrAlreadyLockedOverride(bool alreadyLocked, string failureCode)
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(false);
        _tenantSettings.GetByTenantAndKey(_tenantId, key, Arg.Any<CancellationToken>())
            .Returns(alreadyLocked ? TenantSetting(key, isLocked: true) : null);
        var handler = new LockControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new LockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(failureCode);
        await _tenantSettings.DidNotReceiveWithAnyArgs().LockAsync(default, default!, default);
    }

    [Test]
    public async Task Lock_ValidUnlockedOverrideMutatesOnce()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(false);
        _tenantSettings.GetByTenantAndKey(_tenantId, key, Arg.Any<CancellationToken>())
            .Returns(TenantSetting(key, isLocked: false));
        _tenantSettings.LockAsync(_tenantId, key, _actorUserId, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new LockControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new LockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _tenantSettings.Received(1).LockAsync(
            _tenantId,
            key,
            _actorUserId,
            Arg.Any<CancellationToken>());
        _settingsResolver.Received(1).InvalidateCache(SettingScope.Tenant, _tenantId);
        await _mediator.Received(1).Publish(
            Arg.Is<Explore.Application.Notifications.SettingChangedNotification>(notification =>
                notification.ActorUserId == _actorUserId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(false, "setting_state_conflict")]
    [Arguments(true, "setting_override_not_found")]
    public async Task Unlock_DeniesUnlockedOrMissingOverride(bool missing, string failureCode)
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(false);
        _tenantSettings.GetByTenantAndKey(_tenantId, key, Arg.Any<CancellationToken>())
            .Returns(missing ? null : TenantSetting(key, isLocked: false));
        var handler = new UnlockControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new UnlockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(failureCode);
        await _tenantSettings.DidNotReceiveWithAnyArgs().UnlockAsync(default, default!, default, default);
    }

    [Test]
    public async Task Unlock_ValidLockedOverrideMutatesOnce()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(false);
        _tenantSettings.GetByTenantAndKey(_tenantId, key, Arg.Any<CancellationToken>())
            .Returns(TenantSetting(key, isLocked: true));
        _tenantSettings.UnlockAsync(_tenantId, key, _actorUserId, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UnlockControlPlaneTenantSettingCommandHandler(_tenantSettings, _systemSettings, _mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new UnlockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _tenantSettings.Received(1).UnlockAsync(
            _tenantId,
            key,
            _actorUserId,
            Arg.Any<CancellationToken>());
        _settingsResolver.Received(1).InvalidateCache(SettingScope.Tenant, _tenantId);
        await _mediator.Received(1).Publish(
            Arg.Is<Explore.Application.Notifications.SettingChangedNotification>(notification =>
                notification.ActorUserId == _actorUserId),
            Arg.Any<CancellationToken>());

    }

    [Test]
    public async Task Set_WhenTrustedOperatorIsMissing_FailsBeforeCoordinationOrPersistence()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        var mutationLock = Substitute.For<ISettingMutationLock>();
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.IsAuthenticated.Returns(false);
        var handler = new SetControlPlaneTenantSettingCommandHandler(
            _tenantSettings,
            _systemSettings,
            mutationLock,
            _currentUserService,
            _settingsResolver,
            _mediator);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "smtp.example.test"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("authenticated_operator_required");
        await mutationLock.DidNotReceiveWithAnyArgs().ExecuteAsync<object>(default!, default!, default);
        await _systemSettings.DidNotReceiveWithAnyArgs().IsLocked(default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs()
            .SetValueAsync(default, default!, default!, default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    public async Task LockAndUnlock_WhenTrustedOperatorIsMissing_FailBeforeCoordinationOrPersistence()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        var mutationLock = Substitute.For<ISettingMutationLock>();
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.IsAuthenticated.Returns(false);
        var lockHandler = new LockControlPlaneTenantSettingCommandHandler(
            _tenantSettings,
            _systemSettings,
            mutationLock,
            _currentUserService,
            _settingsResolver,
            _mediator);
        var unlockHandler = new UnlockControlPlaneTenantSettingCommandHandler(
            _tenantSettings,
            _systemSettings,
            mutationLock,
            _currentUserService,
            _settingsResolver,
            _mediator);

        var lockResult = await lockHandler.Handle(
            new LockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);
        var unlockResult = await unlockHandler.Handle(
            new UnlockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);

        await Assert.That(lockResult.FailureCode).IsEqualTo("authenticated_operator_required");
        await Assert.That(unlockResult.FailureCode).IsEqualTo("authenticated_operator_required");
        await mutationLock.DidNotReceiveWithAnyArgs().ExecuteAsync<object>(default!, default!, default);
        await _systemSettings.DidNotReceiveWithAnyArgs().IsLocked(default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().LockAsync(default, default!, default, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().UnlockAsync(default, default!, default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    private TenantSetting TenantSetting(string key, bool isLocked) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = _tenantId,
        Tenant = null!,
        SettingKey = key,
        Value = "value",
        IsLocked = isLocked
    };

    private sealed class ImmediateSettingMutationLock : ISettingMutationLock
    {
        public Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);

        public Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);
    }
}
