// ABOUTME: Security-boundary tests for direct Control Plane tenant setting mutation handlers.
// ABOUTME: Proves registry, scope, sensitivity, lock, value, and transition denials occur before writes.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Handlers.Commands;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Settings;
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
        var handler = CreateSetHandler();

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
        var handler = CreateSetHandler();

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
        var handler = CreateSetHandler();

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
        var handler = CreateSetHandler();

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
        var handler = CreateSetHandler();

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
        var handler = CreateSetHandler();

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
    public async Task Lock_GuardedPublicationPolicyKey_FailsBeforeCoordinationOrSideEffects()
    {
        string key = PublicationPolicySettingKeys.All[0];
        var mutationLock = Substitute.For<ISettingMutationLock>();
        var handler = new LockControlPlaneTenantSettingCommandHandler(
            _tenantSettings,
            _systemSettings,
            mutationLock,
            _currentUserService,
            _settingsResolver,
            _mediator);

        var result = await handler.Handle(
            new LockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("setting_not_lockable");
        await Assert.That(mutationLock.ReceivedCalls().Count()).IsEqualTo(0);
        await Assert.That(_systemSettings.ReceivedCalls().Count()).IsEqualTo(0);
        await Assert.That(_tenantSettings.ReceivedCalls().Count()).IsEqualTo(0);
        await Assert.That(_settingsResolver.ReceivedCalls().Count()).IsEqualTo(0);
        await Assert.That(_mediator.ReceivedCalls().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Unlock_GuardedPublicationPolicyKey_FailsBeforeCoordinationOrSideEffects()
    {
        string key = PublicationPolicySettingKeys.All[0];
        var mutationLock = Substitute.For<ISettingMutationLock>();
        var handler = new UnlockControlPlaneTenantSettingCommandHandler(
            _tenantSettings,
            _systemSettings,
            mutationLock,
            _currentUserService,
            _settingsResolver,
            _mediator);

        var result = await handler.Handle(
            new UnlockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("setting_not_lockable");
        await Assert.That(mutationLock.ReceivedCalls().Count()).IsEqualTo(0);
        await Assert.That(_systemSettings.ReceivedCalls().Count()).IsEqualTo(0);
        await Assert.That(_tenantSettings.ReceivedCalls().Count()).IsEqualTo(0);
        await Assert.That(_settingsResolver.ReceivedCalls().Count()).IsEqualTo(0);
        await Assert.That(_mediator.ReceivedCalls().Count()).IsEqualTo(0);
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
        var handler = CreateSetHandler(mutationLock);

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

    [Test]
    public async Task Set_GuardedPublicationPolicyKey_DelegatesOneRejectingMutationToBoundary()
    {
        const string key = GovernanceSettingKeys.EventReporting.IntakeEnabled;
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        var mutationLock = Substitute.For<ISettingMutationLock>();
        var unitOfWork = new ImmediateUnitOfWork();
        var cancellationToken = new CancellationTokenSource().Token;
        PublicationPolicyTenantMutationRequest? observedRequest = null;
        CancellationToken observedCancellationToken = default;
        boundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                observedRequest = call.Arg<PublicationPolicyTenantMutationRequest>();
                observedCancellationToken = call.Arg<CancellationToken>();
                return Task.FromResult(BoundaryResult(accepted: true));
            });
        var handler = CreateGuardedSetHandler(boundary, unitOfWork, mutationLock);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "true"),
            cancellationToken);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(observedRequest).IsNotNull();
        await Assert.That(observedRequest!.TenantId).IsEqualTo(_tenantId);
        await Assert.That(observedRequest.ActorUserId).IsEqualTo(_actorUserId);
        await Assert.That(observedRequest.OccurredAtUtc.Kind).IsEqualTo(DateTimeKind.Utc);
        await Assert.That(observedRequest.LockedSystemBehavior)
            .IsEqualTo(PublicationPolicyLockedSystemBehavior.Reject);
        await Assert.That(observedRequest.Mutations.Length).IsEqualTo(1);
        await Assert.That(observedRequest.Mutations[0].Kind).IsEqualTo(PublicationPolicyMutationKind.Set);
        await Assert.That(observedRequest.Mutations[0].Key).IsEqualTo(key);
        await Assert.That(observedRequest.Mutations[0].JsonValue).IsEqualTo("true");
        await Assert.That(observedRequest.Mutations[0].TenantId).IsEqualTo(_tenantId);
        await Assert.That(observedRequest.Mutations[0].IsLocked).IsNull();
        await Assert.That(observedCancellationToken).IsEqualTo(cancellationToken);
        await boundary.Received(1).ApplyTenantAsync(
            Arg.Any<PublicationPolicyTenantMutationRequest>(),
            cancellationToken);
        await _systemSettings.DidNotReceiveWithAnyArgs().IsLocked(default!, default);
        await mutationLock.DidNotReceiveWithAnyArgs().ExecuteAsync<object>(default!, default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().GetByTenantAndKey(default, default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs()
            .SetValueAsync(default, default!, default!, default, default);
    }

    [Test]
    [Arguments("event_reporting_intake_unsafe_publication_policy")]
    [Arguments("event_reporting_intake_policy_invalid")]
    [Arguments("event_reporting_policy_locked")]
    public async Task Set_GuardedPublicationPolicyBoundaryFailure_MapsExactFailureWithoutSideEffects(
        string failureCode)
    {
        const string key = GovernanceSettingKeys.EventReporting.IntakeEnabled;
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        var unitOfWork = new ImmediateUnitOfWork();
        boundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(BoundaryResult(accepted: false, failureCode)));
        var handler = CreateGuardedSetHandler(boundary, unitOfWork);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "false"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(string.IsNullOrWhiteSpace(result.FailureCode)).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(failureCode);
        await _tenantSettings.DidNotReceiveWithAnyArgs()
            .SetValueAsync(default, default!, default!, default, default);
        _settingsResolver.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    public async Task Set_AcceptedGuardedPublicationPolicyWrite_CommitsBeforeDeferredCanonicalEffects()
    {
        const string key = GovernanceSettingKeys.EventReporting.IntakeEnabled;
        var calls = new List<string>();
        var unitOfWork = new RecordingUnitOfWork(calls);
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        SettingChangedNotification first = Notification("events.require_approval");
        SettingChangedNotification second = Notification(key);
        boundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("boundary");
                return Task.FromResult(BoundaryResult(accepted: true, notifications: [first, second]));
            });
        _settingsResolver.When(resolver => resolver.InvalidateCache(SettingScope.Tenant, _tenantId))
            .Do(_ => calls.Add("invalidate"));
        _mediator.Publish(Arg.Any<SettingChangedNotification>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                SettingChangedNotification notification = call.Arg<SettingChangedNotification>()!;
                calls.Add($"publish:{notification.Key}");
                return Task.CompletedTask;
            });
        var handler = CreateGuardedSetHandler(boundary, unitOfWork);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "true"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(calls.SequenceEqual([
            "transaction-start",
            "boundary",
            "transaction-commit",
            "invalidate",
            "publish:events.require_approval",
            $"publish:{key}"
        ])).IsTrue();
    }

    [Test]
    public async Task Set_AcceptedGuardedPublicationPolicyWriteWithNoDeferredEffects_DoesNotInvalidateOrPublish()
    {
        const string key = GovernanceSettingKeys.EventReporting.IntakeEnabled;
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        boundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(BoundaryResult(accepted: true)));
        var handler = CreateGuardedSetHandler(boundary, new ImmediateUnitOfWork());

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "true"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        _settingsResolver.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    public async Task Set_UnguardedSetting_PreservesDirectRepositoryCacheAndNotificationBehavior()
    {
        const string key = GovernanceSettingKeys.Email.SmtpSkipCertValidation;
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        var handler = CreateGuardedSetHandler(boundary, new ImmediateUnitOfWork());
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(false);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "true"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await boundary.DidNotReceiveWithAnyArgs()
            .ApplyTenantAsync(default!, default);
        await _tenantSettings.Received(1)
            .SetValueAsync(_tenantId, key, "true", Arg.Any<CancellationToken>(), _actorUserId);
        _settingsResolver.Received(1).InvalidateCache(SettingScope.Tenant, _tenantId);
        await _mediator.Received(1).Publish(
            Arg.Is<Explore.Application.Notifications.SettingChangedNotification>(notification =>
                notification != null
                && notification.Key == key
                && notification.ActorUserId == _actorUserId),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Set_GuardedPublicationPolicyKey_WhenOperatorIdentityIsMissingOrEmpty_FailsBeforeBoundary(
        bool missingIdentity)
    {
        const string key = GovernanceSettingKeys.EventReporting.IntakeEnabled;
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        _currentUserService.UserId.Returns(missingIdentity ? null : Guid.Empty);
        var handler = CreateGuardedSetHandler(boundary, new ImmediateUnitOfWork());

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "true"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("authenticated_operator_required");
        await boundary.DidNotReceiveWithAnyArgs()
            .ApplyTenantAsync(default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs()
            .SetValueAsync(default, default!, default!, default, default);
        _settingsResolver.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    public async Task Set_GuardedPublicationPolicyKey_PropagatesCancellationFromBoundary()
    {
        const string key = GovernanceSettingKeys.Events.RequireApproval;
        using var cancellation = new CancellationTokenSource();
        var boundary = Substitute.For<IPublicationPolicyMutationBoundary>();
        var boundaryCalled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var boundaryResult = new TaskCompletionSource<PublicationPolicyMutationResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        boundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                cancellation.Token)
            .Returns(_ =>
            {
                boundaryCalled.TrySetResult();
                return boundaryResult.Task;
            });
        var handler = CreateGuardedSetHandler(boundary, new ImmediateUnitOfWork());

        Task act = handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "true"),
            cancellation.Token);
        await boundaryCalled.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await cancellation.CancelAsync();
        boundaryResult.SetCanceled(cancellation.Token);

        await Assert.That(async () => await act).Throws<OperationCanceledException>();
        await boundary.Received(1).ApplyTenantAsync(Arg.Any<PublicationPolicyTenantMutationRequest>(), cancellation.Token);
        await _tenantSettings.DidNotReceiveWithAnyArgs()
            .SetValueAsync(default, default!, default!, default, default);
    }

    private SetControlPlaneTenantSettingCommandHandler CreateSetHandler(
        ISettingMutationLock? mutationLock = null) => CreateGuardedSetHandler(
        Substitute.For<IPublicationPolicyMutationBoundary>(),
        new ImmediateUnitOfWork(),
        mutationLock);

    private SetControlPlaneTenantSettingCommandHandler CreateGuardedSetHandler(
        IPublicationPolicyMutationBoundary publicationPolicyMutationBoundary,
        IUnitOfWork unitOfWork,
        ISettingMutationLock? mutationLock = null) => new(
        _tenantSettings,
        _systemSettings,
        mutationLock ?? _mutationLock,
        _currentUserService,
        _settingsResolver,
        _mediator,
        publicationPolicyMutationBoundary,
        unitOfWork);

    private SettingChangedNotification Notification(string key) => new(
        key,
        null,
        "true",
        SettingSource.TenantOverride,
        _tenantId,
        _actorUserId,
        DateTime.UtcNow);

    private static PublicationPolicyMutationResult BoundaryResult(
        bool accepted,
        string? failureCode = null,
        params SettingChangedNotification[] notifications) => new(
        Success: accepted,
        FailureCode: failureCode,
        Message: accepted ? "Publication policy updated." : "Publication policy rejected.",
        DeferredNotifications: [.. notifications]);

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) => await operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => operation(ct);

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => operation(ct);
    }

    private sealed class RecordingUnitOfWork(List<string> calls) : IUnitOfWork
    {
        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default)
        {
            calls.Add("transaction-start");
            await operation(ct);
            calls.Add("transaction-commit");
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            calls.Add("transaction-start");
            T result = await operation(ct);
            calls.Add("transaction-commit");
            return result;
        }

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => operation(ct);
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
