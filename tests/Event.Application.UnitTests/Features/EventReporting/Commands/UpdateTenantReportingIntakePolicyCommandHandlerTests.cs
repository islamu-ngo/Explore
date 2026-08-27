// ABOUTME: RED specifications for current-tenant reporting-intake policy mutation handling.
// ABOUTME: Requires manual validation, tenant binding, guarded transactions, exact failures, and post-commit effects.

using System.Collections.Immutable;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventReporting;
using Explore.Application.Features.EventReporting.Handlers.Commands;
using Explore.Application.Features.EventReporting.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Settings;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using FluentValidation;
using MediatR;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventReporting.Commands;

public sealed class UpdateTenantReportingIntakePolicyCommandHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _actorUserId = Guid.CreateVersion7();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IPublicationPolicyMutationBoundary _boundary =
        Substitute.For<IPublicationPolicyMutationBoundary>();
    private readonly IHierarchicalSettingsResolver _settings = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public UpdateTenantReportingIntakePolicyCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _boundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Succeeded());
        _mediator.Publish(Arg.Any<SettingChangedNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    [Test]
    public async Task RequestMetadata_UsesCanonicalTenantSettingUpdateCapability()
    {
        var request = Command(enabled: false);
        var secure = (ISecureRequest)request;
        AuthorizeResourceAttribute authorization = typeof(UpdateTenantReportingIntakePolicyCommand)
            .GetCustomAttributes(typeof(AuthorizeResourceAttribute), inherit: false)
            .Cast<AuthorizeResourceAttribute>()
            .Single();

        await Assert.That(authorization.Resource).IsEqualTo(ResourceKinds.TenantSetting);
        await Assert.That(authorization.Action).IsEqualTo(AuthorizationActions.TenantSettings.Update);
        await Assert.That(secure.ResourceId).IsEqualTo(GovernanceSettingKeys.EventReporting.IntakeEnabled);
        await Assert.That(secure.AuthorizationFacts).IsEqualTo(new TenantSettingAuthorizationFacts(
            _tenantId,
            GovernanceSettingKeys.EventReporting.IntakeEnabled));
    }

    [Test]
    public async Task Validator_RejectsMissingServerOwnedIdentityAndAcceptsCompleteRequest()
    {
        IValidator<UpdateTenantReportingIntakePolicyCommand> validator =
            new UpdateTenantReportingIntakePolicyCommandValidator();

        var missingActor = await validator.ValidateAsync(new UpdateTenantReportingIntakePolicyCommand(
            _tenantId,
            Guid.Empty,
            new UpdateTenantReportingIntakePolicyDto { Enabled = false }));
        var missingTenant = await validator.ValidateAsync(new UpdateTenantReportingIntakePolicyCommand(
            Guid.Empty,
            _actorUserId,
            new UpdateTenantReportingIntakePolicyDto { Enabled = false }));
        var valid = await validator.ValidateAsync(Command(enabled: true));

        await Assert.That(missingActor.IsValid).IsFalse();
        await Assert.That(missingTenant.IsValid).IsFalse();
        await Assert.That(valid.IsValid).IsTrue();
    }

    [Test]
    public async Task Handle_ManuallyValidatesBeforeOpeningTransactionOrCallingBoundary()
    {
        var unitOfWork = new RecordingUnitOfWork();
        var invalid = new UpdateTenantReportingIntakePolicyCommand(
            _tenantId,
            Guid.Empty,
            new UpdateTenantReportingIntakePolicyDto { Enabled = false });

        var result = await CreateHandler(unitOfWork).Handle(invalid, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_reporting_intake_policy_invalid");
        await Assert.That(unitOfWork.TransactionCount).IsEqualTo(0);
        await _boundary.DidNotReceiveWithAnyArgs().ApplyTenantAsync(default!, default);
        _settings.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    public async Task Handle_WhenRequestTenantDiffersFromAmbientTenant_FailsBeforeTransactionOrBoundary()
    {
        var unitOfWork = new RecordingUnitOfWork();
        var request = new UpdateTenantReportingIntakePolicyCommand(
            Guid.CreateVersion7(),
            _actorUserId,
            new UpdateTenantReportingIntakePolicyDto { Enabled = false });

        var result = await CreateHandler(unitOfWork).Handle(request, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("tenant_context_mismatch");
        await Assert.That(unitOfWork.TransactionCount).IsEqualTo(0);
        await _boundary.DidNotReceiveWithAnyArgs().ApplyTenantAsync(default!, default);
        _settings.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    [Arguments(true, "true")]
    [Arguments(false, "false")]
    public async Task Handle_EnabledAndDisabledValuesUseOneCanonicalRejectLockedBoundaryCall(
        bool enabled,
        string expectedJson)
    {
        using var cancellation = new CancellationTokenSource();
        PublicationPolicyTenantMutationRequest? observed = null;
        _boundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                cancellation.Token)
            .Returns(call =>
            {
                observed = call.Arg<PublicationPolicyTenantMutationRequest>();
                return Succeeded();
            });
        var unitOfWork = new RecordingUnitOfWork();

        var result = await CreateHandler(unitOfWork).Handle(Command(enabled), cancellation.Token);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(observed).IsNotNull();
        await Assert.That(observed!.TenantId).IsEqualTo(_tenantId);
        await Assert.That(observed.ActorUserId).IsEqualTo(_actorUserId);
        await Assert.That(observed.OccurredAtUtc.Kind).IsEqualTo(DateTimeKind.Utc);
        await Assert.That(observed.LockedSystemBehavior)
            .IsEqualTo(PublicationPolicyLockedSystemBehavior.Reject);
        await Assert.That(observed.Mutations).HasSingleItem();
        PublicationPolicySettingMutation mutation = observed.Mutations[0];
        await Assert.That(mutation.Key).IsEqualTo(GovernanceSettingKeys.EventReporting.IntakeEnabled);
        await Assert.That(mutation.Kind).IsEqualTo(PublicationPolicyMutationKind.Set);
        await Assert.That(mutation.JsonValue).IsEqualTo(expectedJson);
        await Assert.That(mutation.TenantId).IsEqualTo(_tenantId);
        await Assert.That(mutation.IsLocked).IsNull();
        await Assert.That(unitOfWork.TransactionCount).IsEqualTo(1);
        await Assert.That(unitOfWork.CommitCount).IsEqualTo(1);
    }

    [Test]
    [Arguments("event_reporting_policy_locked")]
    [Arguments("event_reporting_intake_unsafe_publication_policy")]
    public async Task Handle_LockOrUnsafeBoundaryFailurePreservesExactCodeWithoutEffects(string failureCode)
    {
        _boundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Failed(failureCode));
        var unitOfWork = new RecordingUnitOfWork();

        var result = await CreateHandler(unitOfWork).Handle(Command(enabled: false), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(failureCode);
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors!).Contains(failureCode);
        await Assert.That(unitOfWork.TransactionCount).IsEqualTo(1);
        await Assert.That(unitOfWork.CommitCount).IsEqualTo(1);
        _settings.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    [Test]
    public async Task Handle_SuccessCommitsOuterTransactionBeforeCacheInvalidationAndDeferredNotifications()
    {
        var calls = new List<string>();
        var first = Notification(GovernanceSettingKeys.Events.RequireApproval);
        var second = Notification(GovernanceSettingKeys.EventReporting.IntakeEnabled);
        _boundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("boundary");
                return Succeeded(first, second);
            });
        _settings.When(settings => settings.InvalidateCache(SettingScope.Tenant, _tenantId))
            .Do(_ => calls.Add("invalidate"));
        _mediator.Publish(Arg.Any<SettingChangedNotification>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                calls.Add($"publish:{call.Arg<SettingChangedNotification>().Key}");
                return Task.CompletedTask;
            });
        var unitOfWork = new RecordingUnitOfWork(calls);

        var result = await CreateHandler(unitOfWork).Handle(Command(enabled: false), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(calls.SequenceEqual([
            "transaction-start",
            "boundary",
            "transaction-commit",
            "invalidate",
            $"publish:{GovernanceSettingKeys.Events.RequireApproval}",
            $"publish:{GovernanceSettingKeys.EventReporting.IntakeEnabled}"
        ])).IsTrue();
        await _mediator.Received(1).Publish(first, CancellationToken.None);
        await _mediator.Received(1).Publish(second, CancellationToken.None);
    }

    [Test]
    public async Task Handle_WhenOuterTransactionFails_PublishesNoDeferredEffects()
    {
        var notification = Notification(GovernanceSettingKeys.EventReporting.IntakeEnabled);
        _boundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Succeeded(notification));
        var unitOfWork = new RecordingUnitOfWork
        {
            CommitFailure = new InvalidOperationException("commit failed")
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateHandler(unitOfWork).Handle(Command(enabled: false), CancellationToken.None));

        await Assert.That(unitOfWork.CommitCount).IsEqualTo(0);
        await Assert.That(unitOfWork.RollbackCount).IsEqualTo(1);
        _settings.DidNotReceiveWithAnyArgs().InvalidateCache(default, default);
        await _mediator.DidNotReceiveWithAnyArgs().Publish(default!, default);
    }

    private UpdateTenantReportingIntakePolicyCommandHandler CreateHandler(IUnitOfWork unitOfWork) => new(
        _tenantContext,
        _boundary,
        unitOfWork,
        _settings,
        _mediator);

    private UpdateTenantReportingIntakePolicyCommand Command(bool enabled) => new(
        _tenantId,
        _actorUserId,
        new UpdateTenantReportingIntakePolicyDto { Enabled = enabled });

    private SettingChangedNotification Notification(string key) => new(
        key,
        "true",
        "false",
        SettingSource.TenantOverride,
        _tenantId,
        _actorUserId,
        DateTime.UtcNow);

    private static Task<PublicationPolicyMutationResult> Succeeded(
        params SettingChangedNotification[] notifications) =>
        Task.FromResult(new PublicationPolicyMutationResult(
            Success: true,
            FailureCode: null,
            Message: "Reporting intake policy updated.",
            DeferredNotifications: [.. notifications]));

    private static Task<PublicationPolicyMutationResult> Failed(string failureCode) =>
        Task.FromResult(new PublicationPolicyMutationResult(
            Success: false,
            FailureCode: failureCode,
            Message: "Reporting intake policy rejected.",
            DeferredNotifications: ImmutableArray<SettingChangedNotification>.Empty));

    private sealed class RecordingUnitOfWork(List<string>? calls = null) : IUnitOfWork
    {
        public int TransactionCount { get; private set; }
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }
        public Exception? CommitFailure { get; init; }

        public Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken ct = default) =>
            ExecuteInTransactionAsync<object?>(async token =>
            {
                await operation(token);
                return null;
            }, ct);

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default)
        {
            TransactionCount++;
            calls?.Add("transaction-start");
            try
            {
                T result = await operation(ct);
                if (CommitFailure is not null)
                    throw CommitFailure;
                CommitCount++;
                calls?.Add("transaction-commit");
                return result;
            }
            catch
            {
                RollbackCount++;
                calls?.Add("transaction-rollback");
                throw;
            }
        }

        public Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken ct = default) => ExecuteInTransactionAsync(operation, ct);
    }
}
