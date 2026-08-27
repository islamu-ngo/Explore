// ABOUTME: Unit tests for tenant policy effective setting resolution.
// ABOUTME: Protects MCP runtime defaults and tenant-lock behavior in the tenant admin read model.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.TenantPolicy;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class TenantPolicySettingServiceTests
{
    private readonly ISystemSettingRepository _systemSettings = Substitute.For<ISystemSettingRepository>();
    private readonly ITenantSettingRepository _tenantSettings = Substitute.For<ITenantSettingRepository>();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly RecordingSettingMutationLock _mutationLock = new();
    private readonly IPublicationPolicyMutationBoundary _publicationPolicyBoundary =
        Substitute.For<IPublicationPolicyMutationBoundary>();
    private readonly TenantPolicySettingService _service;

    public TenantPolicySettingServiceTests()
    {
        _systemSettings.GetByKey(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((SystemSetting?)null);
        _systemSettings.GetAllSettings(
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns([]);
        _tenantSettings.GetByTenantAndKey(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()).Returns((TenantSetting?)null);
        _tenantSettings.GetAllForTenant(Arg.Any<Guid>()).Returns([]);
        _tenants.GetByIdAsNoTrackingAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>()).Returns((Tenant?)null);
        _publicationPolicyBoundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(SucceededBoundaryResult());

        _service = new TenantPolicySettingService(
            _systemSettings,
            _tenantSettings,
            _tenants,
            _mutationLock,
            _publicationPolicyBoundary);
    }

    [Test]
    public async Task ReadEffectiveTenantSettingsAsync_WhenMcpSettingsMissing_DefaultsRuntimeMcpEnabled()
    {
        var result = await _service.ReadEffectiveTenantSettingsAsync(Guid.NewGuid());

        await Assert.That(result.McpEnabled).IsTrue();
        await Assert.That(result.McpEnableLegacySse).IsFalse();
    }

    [Test]
    public async Task ReadEffectiveTenantSettingsAsync_WhenMcpLocked_IgnoresTenantOverride()
    {
        var tenantId = Guid.NewGuid();
        UseSystemSettings(
            CreateSystemSetting(GovernanceSettingKeys.Deployment.Mode, "\"MultiTenant\""),
            CreateSystemSetting(GovernanceSettingKeys.TenantDelegation.LockMcp, "true"),
            CreateSystemSetting(GovernanceSettingKeys.Mcp.Enabled, "true"));
        UseTenantSettings(tenantId, CreateTenantSetting(tenantId, GovernanceSettingKeys.Mcp.Enabled, "false"));

        var result = await _service.ReadEffectiveTenantSettingsAsync(tenantId);

        await Assert.That(result.CanOverrideMcp).IsFalse();
        await Assert.That(result.McpEnabled).IsTrue();
    }

    [Test]
    public async Task ReadEffectiveTenantSettingsAsync_WhenMcpUnlocked_AppliesTenantOverride()
    {
        var tenantId = Guid.NewGuid();
        UseSystemSettings(
            CreateSystemSetting(GovernanceSettingKeys.Deployment.Mode, "\"MultiTenant\""),
            CreateSystemSetting(GovernanceSettingKeys.TenantDelegation.LockMcp, "false"),
            CreateSystemSetting(GovernanceSettingKeys.Mcp.Enabled, "true"));
        UseTenantSettings(tenantId, CreateTenantSetting(tenantId, GovernanceSettingKeys.Mcp.Enabled, "false"));

        var result = await _service.ReadEffectiveTenantSettingsAsync(tenantId);

        await Assert.That(result.CanOverrideMcp).IsTrue();
        await Assert.That(result.McpEnabled).IsFalse();
    }

    [Test]
    public async Task ReadEffectiveTenantSettingsAsync_UsesBatchedSettingReads()
    {
        var tenantId = Guid.NewGuid();

        await _service.ReadEffectiveTenantSettingsAsync(tenantId);

        await _systemSettings.Received(1).GetAllSettings(Arg.Any<string?>());
        await _tenantSettings.Received(1).GetAllForTenant(tenantId);
        await _systemSettings.DidNotReceive().GetByKey(Arg.Any<string>());
        await _tenantSettings.DidNotReceive().GetByTenantAndKey(Arg.Any<Guid>(), Arg.Any<string>());
    }

    [Test]
    public async Task ApplyTenantSettingsAsync_ExecutesAllReadsAndWritesInsideOneDeterministicLockBatch()
    {
        bool allSystemReadsInsideLock = true;
        bool allTenantWritesInsideLock = true;
        _systemSettings.GetAllSettings(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                allSystemReadsInsideLock &= _mutationLock.IsInsideLock;
                return new List<SystemSetting>();
            });
        _tenantSettings.SetValueAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<Guid?>())
            .Returns(callInfo =>
            {
                allTenantWritesInsideLock &= _mutationLock.IsInsideLock;
                return Task.CompletedTask;
            });
        _tenantSettings.RemoveOverrideAsync(
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                allTenantWritesInsideLock &= _mutationLock.IsInsideLock;
                return false;
            });
        IReadOnlyList<SettingChangedNotification> notifications = await _service.ApplyTenantSettingsAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new UpdateTenantPolicyRequest());

        await Assert.That(_mutationLock.Keys.Count).IsEqualTo(
            _mutationLock.Keys.Distinct(StringComparer.Ordinal).Count());
        await Assert.That(_mutationLock.Keys.Where(PublicationPolicySettingKeys.All.Contains).SequenceEqual(
            PublicationPolicySettingKeys.All)).IsTrue();
        await Assert.That(_mutationLock.Keys).Contains(GovernanceSettingKeys.EventReporting.IntakeEnabled);
        await Assert.That(_mutationLock.Keys).Contains(GovernanceSettingKeys.Deployment.Mode);
        await Assert.That(_mutationLock.Keys).Contains(GovernanceSettingKeys.TenantDelegation.LockAiAssistant);
        await Assert.That(allSystemReadsInsideLock).IsTrue();
        await Assert.That(allTenantWritesInsideLock).IsTrue();
        await Assert.That(notifications).IsNotEmpty();
    }

    [Test]
    public async Task ApplyTenantSettingsAsync_WhenCancelled_ForwardsTokenAndPerformsNoReadsOrWrites()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _service.ApplyTenantSettingsAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new UpdateTenantPolicyRequest(),
                cancellation.Token));

        await Assert.That(_mutationLock.LastCancellationToken).IsEqualTo(cancellation.Token);
        await _systemSettings.DidNotReceiveWithAnyArgs().GetAllSettings(default, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs()
            .SetValueAsync(default, default!, default!, default, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().RemoveOverrideAsync(default, default!, default);
    }

    [Test]
    public async Task ApplyTenantSettingsAsync_WhenInstanceLocksKey_RemovesInsteadOfUpdatingTenantOverride()
    {
        var tenantId = Guid.NewGuid();
        string key = GovernanceSettingKeys.PublicExperience.AnnouncementBarEnabled;
        _systemSettings.GetAllSettings(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(
        [
            new SystemSetting
            {
                SettingKey = key,
                Value = "false",
                IsLocked = true
            }
        ]);
        _tenantSettings.GetByTenantAndKey(tenantId, key, Arg.Any<CancellationToken>()).Returns(
            CreateTenantSetting(tenantId, key, "true"));
        _tenantSettings.RemoveOverrideAsync(tenantId, key, Arg.Any<CancellationToken>()).Returns(true);

        IReadOnlyList<SettingChangedNotification> notifications = await _service.ApplyTenantSettingsAsync(
            tenantId,
            Guid.NewGuid(),
            new UpdateTenantPolicyRequest { AnnouncementBarEnabled = true });

        await _tenantSettings.Received(1).RemoveOverrideAsync(
            tenantId,
            key,
            Arg.Any<CancellationToken>());
        await _tenantSettings.DidNotReceive().SetValueAsync(
            tenantId,
            key,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<Guid?>());
        SettingChangedNotification removal = notifications.Single(notification => notification.Key == key);
        await Assert.That(removal.OldValue).IsEqualTo("true");
        await Assert.That(removal.NewValue).IsNull();
    }

    [Test]
    public async Task ApplyTenantSettingsAsync_WhenIntakeIsDisabledAndProposedSubmissionIsUnsafe_ThrowsMachineFailureBeforeOtherWrites()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        PublicationPolicyTenantMutationRequest? boundaryRequest = null;
        _publicationPolicyBoundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                boundaryRequest = callInfo.ArgAt<PublicationPolicyTenantMutationRequest>(0);
                return FailedBoundaryResult("event_reporting_intake_unsafe_publication_policy");
            });

        FluentValidation.ValidationException exception = (await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.ApplyTenantSettingsAsync(tenantId, actorUserId, new UpdateTenantPolicyRequest
            {
                AllowUserSubmittedEvents = true,
                AllowOrganizationSubmittedEvents = false,
                AllowGroupSubmittedEvents = false,
                RequireEventApproval = false
            })))!;

        await Assert.That(exception.Errors.Count()).IsEqualTo(1);
        await Assert.That(exception.Errors.Single().ErrorCode)
            .IsEqualTo("event_reporting_intake_unsafe_publication_policy");
        await Assert.That(boundaryRequest).IsNotNull();
        await Assert.That(boundaryRequest!.Mutations.Any(mutation =>
            mutation.Key == GovernanceSettingKeys.Events.UserSubmissionEnabled
            && mutation.Kind == PublicationPolicyMutationKind.Set
            && mutation.JsonValue == "true")).IsTrue();
        await _tenantSettings.DidNotReceiveWithAnyArgs()
            .SetValueAsync(default, default!, default!, default, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().RemoveOverrideAsync(default, default!, default);
    }

    [Test]
    [Arguments("event_reporting_intake_policy_invalid")]
    [Arguments("event_reporting_policy_locked")]
    public async Task ApplyTenantSettingsAsync_WhenBoundaryRejects_MapsExactMachineFailureWithoutWritesOrNotifications(
        string failureCode)
    {
        _publicationPolicyBoundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(FailedBoundaryResult(failureCode));

        FluentValidation.ValidationException exception = (await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.ApplyTenantSettingsAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateTenantPolicyRequest())))!;

        await Assert.That(exception.Errors.Count()).IsEqualTo(1);
        await Assert.That(exception.Errors.Single().ErrorCode).IsEqualTo(failureCode);
        await _tenantSettings.DidNotReceiveWithAnyArgs()
            .SetValueAsync(default, default!, default!, default, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().RemoveOverrideAsync(default, default!, default);
    }

    [Test]
    [Arguments(true, true, true, true)]
    [Arguments(false, false, false, false)]
    public async Task ApplyTenantSettingsAsync_WhenProposedPublicationPolicyIsSafe_SubmitsOneCompleteBoundaryBatch(
        bool requireApproval,
        bool allowUserSubmission,
        bool allowOrganizationSubmission,
        bool allowGroupSubmission)
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        PublicationPolicyTenantMutationRequest? boundaryRequest = null;
        CancellationToken boundaryCancellationToken = default;
        _publicationPolicyBoundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                boundaryRequest = callInfo.ArgAt<PublicationPolicyTenantMutationRequest>(0);
                boundaryCancellationToken = callInfo.ArgAt<CancellationToken>(1);
                return SucceededBoundaryResult();
            });

        await _service.ApplyTenantSettingsAsync(tenantId, actorUserId, new UpdateTenantPolicyRequest
        {
            RequireEventApproval = requireApproval,
            AllowUserSubmittedEvents = allowUserSubmission,
            AllowOrganizationSubmittedEvents = allowOrganizationSubmission,
            AllowGroupSubmittedEvents = allowGroupSubmission
        }, cancellation.Token);

        await Assert.That(boundaryRequest).IsNotNull();
        await Assert.That(boundaryRequest!.TenantId).IsEqualTo(tenantId);
        await Assert.That(boundaryRequest.ActorUserId).IsEqualTo(actorUserId);
        await Assert.That(boundaryRequest.OccurredAtUtc.Kind).IsEqualTo(DateTimeKind.Utc);
        await Assert.That(boundaryRequest.OccurredAtUtc).IsNotEqualTo(default(DateTime));
        await Assert.That(boundaryRequest.LockedSystemBehavior)
            .IsEqualTo(PublicationPolicyLockedSystemBehavior.RemoveOverride);
        await Assert.That(boundaryRequest.Mutations.Select(mutation => mutation.Key).SequenceEqual(
        [
            GovernanceSettingKeys.Events.RequireApproval,
            GovernanceSettingKeys.Events.UserSubmissionEnabled,
            GovernanceSettingKeys.Events.OrganizationSubmissionEnabled,
            GovernanceSettingKeys.Events.GroupSubmissionEnabled
        ])).IsTrue();
        await Assert.That(boundaryRequest.Mutations.All(mutation =>
            mutation.Kind == PublicationPolicyMutationKind.Set
            && mutation.TenantId == tenantId
            && mutation.IsLocked is null)).IsTrue();
        await Assert.That(boundaryRequest.Mutations.Select(mutation => mutation.JsonValue).SequenceEqual(
        [
            requireApproval ? "true" : "false",
            allowUserSubmission ? "true" : "false",
            allowOrganizationSubmission ? "true" : "false",
            allowGroupSubmission ? "true" : "false"
        ])).IsTrue();
        await Assert.That(boundaryRequest.Mutations.Any(mutation =>
            mutation.Key == GovernanceSettingKeys.EventReporting.IntakeEnabled)).IsFalse();
        await Assert.That(boundaryCancellationToken).IsEqualTo(cancellation.Token);
        await Assert.That(_mutationLock.LastCancellationToken).IsEqualTo(cancellation.Token);
        await _publicationPolicyBoundary.Received(1).ApplyTenantAsync(
            Arg.Any<PublicationPolicyTenantMutationRequest>(),
            cancellation.Token);
    }

    [Test]
    public async Task ApplyTenantSettingsAsync_MergesDeferredBoundaryNotificationsBeforeUnguardedNotifications()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var boundaryNotification = new SettingChangedNotification(
            GovernanceSettingKeys.Events.RequireApproval,
            "false",
            "true",
            SettingSource.TenantOverride,
            tenantId,
            actorUserId,
            DateTime.UtcNow);
        _publicationPolicyBoundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(SucceededBoundaryResult(boundaryNotification));

        IReadOnlyList<SettingChangedNotification> notifications = await _service.ApplyTenantSettingsAsync(
            tenantId,
            actorUserId,
            new UpdateTenantPolicyRequest { AnnouncementBarEnabled = true });

        await Assert.That(notifications[0]).IsEqualTo(boundaryNotification);
        await Assert.That(notifications.Skip(1).Any(notification =>
            notification.Key == GovernanceSettingKeys.PublicExperience.AnnouncementBarEnabled)).IsTrue();
    }

    [Test]
    public async Task ApplyTenantSettingsAsync_WhenEventPolicyIsLocked_DelegatesOverrideCleanupToBoundary()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var removal = new SettingChangedNotification(
            GovernanceSettingKeys.Events.UserSubmissionEnabled,
            "true",
            null,
            SettingSource.TenantOverride,
            tenantId,
            actorUserId,
            DateTime.UtcNow);
        PublicationPolicyTenantMutationRequest? boundaryRequest = null;
        _publicationPolicyBoundary.ApplyTenantAsync(
                Arg.Any<PublicationPolicyTenantMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                boundaryRequest = callInfo.ArgAt<PublicationPolicyTenantMutationRequest>(0);
                return SucceededBoundaryResult(removal);
            });

        IReadOnlyList<SettingChangedNotification> notifications = await _service.ApplyTenantSettingsAsync(
            tenantId,
            actorUserId,
            new UpdateTenantPolicyRequest { AllowUserSubmittedEvents = false });

        await Assert.That(boundaryRequest).IsNotNull();
        await Assert.That(boundaryRequest!.LockedSystemBehavior)
            .IsEqualTo(PublicationPolicyLockedSystemBehavior.RemoveOverride);
        await Assert.That(notifications[0]).IsEqualTo(removal);
        await _tenantSettings.DidNotReceive().SetValueAsync(
            tenantId,
            GovernanceSettingKeys.Events.UserSubmissionEnabled,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<Guid?>());
    }

    [Test]
    public async Task ApplyTenantSettingsAsync_WhenAiProviderIsInvalid_DoesNotInvokeBoundaryOrWrite()
    {
        await Assert.ThrowsAsync<Explore.Application.Exceptions.ValidationException>(() =>
            _service.ApplyTenantSettingsAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateTenantPolicyRequest
            {
                AiAssistantEnabled = true,
                AiAssistantProvider = "unsupported"
            }));

        await _publicationPolicyBoundary.DidNotReceiveWithAnyArgs().ApplyTenantAsync(default!, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs()
            .SetValueAsync(default, default!, default!, default, default);
        await _tenantSettings.DidNotReceiveWithAnyArgs().RemoveOverrideAsync(default, default!, default);
    }

    private static Task<PublicationPolicyMutationResult> SucceededBoundaryResult(
        params SettingChangedNotification[] notifications) =>
        Task.FromResult(new PublicationPolicyMutationResult(
            Success: true,
            FailureCode: null,
            Message: string.Empty,
            DeferredNotifications: [.. notifications]));

    private static Task<PublicationPolicyMutationResult> FailedBoundaryResult(string failureCode) =>
        Task.FromResult(new PublicationPolicyMutationResult(
            Success: false,
            FailureCode: failureCode,
            Message: string.Empty,
            DeferredNotifications: []));

    private void UseSystemSettings(params SystemSetting[] settings)
    {
        _systemSettings.GetAllSettings(
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>()).Returns(settings.ToList());
    }

    private void UseTenantSettings(Guid tenantId, params TenantSetting[] settings)
    {
        _tenantSettings.GetAllForTenant(tenantId).Returns(settings.ToList());
    }

    private static SystemSetting CreateSystemSetting(string key, string value) => new()
    {
        SettingKey = key,
        Value = value
    };

    private static TenantSetting CreateTenantSetting(Guid tenantId, string key, string value) => new()
    {
        TenantId = tenantId,
        Tenant = null!,
        SettingKey = key,
        Value = value
    };

    private sealed class RecordingSettingMutationLock : ISettingMutationLock
    {
        public IReadOnlyList<string> Keys { get; private set; } = [];
        public bool IsInsideLock { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task<T> ExecuteAsync<T>(
            string canonicalSettingKey,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default) => operation(cancellationToken);

        public async Task<T> ExecuteManyAsync<T>(
            IEnumerable<string> canonicalSettingKeys,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            Keys = canonicalSettingKeys.ToArray();
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            IsInsideLock = true;
            try
            {
                return await operation(cancellationToken);
            }
            finally
            {
                IsInsideLock = false;
            }
        }
    }
}
