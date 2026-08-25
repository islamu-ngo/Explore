// ABOUTME: Race-safety tests for linearized Control Plane tenant setting mutations.
// ABOUTME: Proves shared lock ordering, system-lock rechecks, value-only writes, and CAS conflicts.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ControlPlane.Handlers.Commands;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.ControlPlane.Commands;

public sealed class ControlPlaneTenantSettingMutationRaceTests
{
    private readonly ITenantSettingRepository _tenantSettings = Substitute.For<ITenantSettingRepository>();
    private readonly ISystemSettingRepository _systemSettings = Substitute.For<ISystemSettingRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IHierarchicalSettingsResolver _settingsResolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();

    public ControlPlaneTenantSettingMutationRaceTests()
    {
        _currentUserService.UserId.Returns(_actorUserId);
        _currentUserService.IsAuthenticated.Returns(true);
    }

    [Test]
    public async Task Set_AcquiresSharedLockBeforeSystemRecheckAndValueOnlyMutation()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        var calls = new List<string>();
        var mutationLock = new RecordingSettingMutationLock(calls);
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("system-recheck");
                return false;
            });
        _tenantSettings.SetValueAsync(_tenantId, key, Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(_ =>
            {
                calls.Add("value-only-mutation");
                return Task.CompletedTask;
            });
        var handler = new SetControlPlaneTenantSettingCommandHandler(
            _tenantSettings,
            _systemSettings,
            mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "smtp.example.test"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(calls.Count).IsEqualTo(3);
        await Assert.That(calls[0]).IsEqualTo("lock");
        await Assert.That(calls[1]).IsEqualTo("system-recheck");
        await Assert.That(calls[2]).IsEqualTo("value-only-mutation");
        await _tenantSettings.DidNotReceiveWithAnyArgs()
            .UpsertManyForTenantAsync(default, default!, default);
    }

    [Test]
    public async Task Set_WhenLockedSystemRecheckWins_DoesNotMutateTenantSetting()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        var calls = new List<string>();
        var mutationLock = new RecordingSettingMutationLock(calls);
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("system-recheck");
                return true;
            });
        var handler = new SetControlPlaneTenantSettingCommandHandler(
            _tenantSettings,
            _systemSettings,
            mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "smtp.example.test"),
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("setting_system_locked");
        await Assert.That(calls.Count).IsEqualTo(2);
        await Assert.That(calls[0]).IsEqualTo("lock");
        await Assert.That(calls[1]).IsEqualTo("system-recheck");
        await _tenantSettings.DidNotReceiveWithAnyArgs()
            .SetValueAsync(default, default!, default!, default);
        _settingsResolver.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    public async Task Set_PublishesAndInvalidatesOnlyAfterMutationTransactionCompletes()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        var calls = new List<string>();
        var mutationLock = new PostCommitRecordingSettingMutationLock(calls);
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(false);
        _tenantSettings.SetValueAsync(_tenantId, key, Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<Guid?>())
            .Returns(_ =>
            {
                calls.Add("mutation");
                return Task.CompletedTask;
            });
        _settingsResolver.When(resolver => resolver.InvalidateCache(SettingScope.Tenant, _tenantId))
            .Do(_ => calls.Add("invalidate"));
        _mediator.Publish(Arg.Any<Explore.Application.Notifications.SettingChangedNotification>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("publish");
                return Task.CompletedTask;
            });
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

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(calls.Count).IsEqualTo(5);
        await Assert.That(calls[0]).IsEqualTo("lock");
        await Assert.That(calls[1]).IsEqualTo("mutation");
        await Assert.That(calls[2]).IsEqualTo("transaction-complete");
        await Assert.That(calls[3]).IsEqualTo("invalidate");
        await Assert.That(calls[4]).IsEqualTo("publish");
    }

    [Test]
    public async Task Lock_WhenExpectedStateCasLoses_ReturnsConflict()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        var mutationLock = new RecordingSettingMutationLock([]);
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(false);
        _tenantSettings.GetByTenantAndKey(_tenantId, key, Arg.Any<CancellationToken>())
            .Returns(TenantSetting(key, isLocked: false));
        _tenantSettings.LockAsync(_tenantId, key, _actorUserId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new LockControlPlaneTenantSettingCommandHandler(
            _tenantSettings,
            _systemSettings,
            mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new LockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("setting_state_conflict");
    }

    [Test]
    public async Task Unlock_WhenExpectedStateCasLoses_ReturnsConflict()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        var mutationLock = new RecordingSettingMutationLock([]);
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(false);
        _tenantSettings.GetByTenantAndKey(_tenantId, key, Arg.Any<CancellationToken>())
            .Returns(TenantSetting(key, isLocked: true));
        _tenantSettings.UnlockAsync(_tenantId, key, _actorUserId, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new UnlockControlPlaneTenantSettingCommandHandler(
            _tenantSettings,
            _systemSettings,
            mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new UnlockControlPlaneTenantSettingCommand(_tenantId, key),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("setting_state_conflict");
    }

    [Test]
    public async Task Set_WhenTransactionDelegateRetries_ReappliesValueOnlyMutationWithoutLockPayload()
    {
        const string key = GovernanceSettingKeys.Email.SmtpHost;
        var mutationLock = new RecordingSettingMutationLock([], attempts: 2);
        _systemSettings.IsLocked(key, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new SetControlPlaneTenantSettingCommandHandler(
            _tenantSettings,
            _systemSettings,
            mutationLock, _currentUserService, _settingsResolver, _mediator);

        var result = await handler.Handle(
            new SetControlPlaneTenantSettingCommand(_tenantId, key, "smtp.example.test"),
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _tenantSettings.Received(2)
            .SetValueAsync(_tenantId, key, "\"smtp.example.test\"", Arg.Any<CancellationToken>(), Arg.Any<Guid?>());
        await _tenantSettings.DidNotReceiveWithAnyArgs()
            .UpsertManyForTenantAsync(default, default!, default);
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

    private sealed class RecordingSettingMutationLock(List<string> calls, int attempts = 1)
        : ISettingMutationLock
    {
        public async Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            calls.Add("lock");
            T result = default!;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                result = await operation(cancellationToken);
            }

            return result;
        }

        public Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    private sealed class PostCommitRecordingSettingMutationLock(List<string> calls) : ISettingMutationLock
    {
        public async Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            calls.Add("lock");
            T result = await operation(cancellationToken);
            calls.Add("transaction-complete");
            return result;
        }

        public Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);
    }
}
