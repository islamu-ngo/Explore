// ABOUTME: RED specifications for coordinated instance event-policy writes.
// ABOUTME: Guards the five-key publication policy boundary while retaining card-click as an unguarded setting.

using System.Collections.Immutable;
using System.Reflection;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance;
using Explore.Application.Models.Common;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Settings;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class InstanceEventPolicyPublicationSafetyTests
{
    private readonly IHierarchicalSettingsResolver _resolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly ISystemSettingRepository _settings = Substitute.For<ISystemSettingRepository>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IModuleCapabilityService _modules = Substitute.For<IModuleCapabilityService>();
    private readonly IPublicationPolicyMutationBoundary _boundary =
        Substitute.For<IPublicationPolicyMutationBoundary>();
    private readonly SettingUpsertService _upsert;
    private readonly InstanceGovernanceSettingService _service;

    public InstanceEventPolicyPublicationSafetyTests()
    {
        _boundary.ApplyInstanceAsync(
                Arg.Any<PublicationPolicyInstanceMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Succeeded());
        _settings.UpsertAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        _mediator.Publish(Arg.Any<SettingChangedNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _upsert = new SettingUpsertService(
            _settings,
            _mediator,
            publicationPolicyMutationBoundary: _boundary);
        _service = new InstanceGovernanceSettingService(
            _resolver,
            _upsert,
            _modules,
            Substitute.For<ILogger<InstanceGovernanceSettingService>>());
    }

    [Test]
    public async Task ApplyEventPolicyAsync_SubmitsSubmissionValuesAsOneInstanceBoundaryBatch()
    {
        var writes = CaptureWrites();
        var actorId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        PublicationPolicyInstanceMutationRequest? request = null;
        CancellationToken observedCancellation = default;
        _boundary.ApplyInstanceAsync(
                Arg.Any<PublicationPolicyInstanceMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                request = call.ArgAt<PublicationPolicyInstanceMutationRequest>(0);
                observedCancellation = call.ArgAt<CancellationToken>(1);
                return Succeeded();
            });

        PublicationPolicyMutationResult result = await _service.ApplyEventPolicyAsync(
            new EventPolicyDto
            {
                AllowUserSubmittedEvents = false,
                AllowOrganizationSubmittedEvents = true,
                AllowGroupSubmittedEvents = false,
                EventCardClickOpensDetailPage = true
            },
            actorId,
            cancellation.Token);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(request).IsNotNull();
        await Assert.That(request!.ActorUserId).IsEqualTo(actorId);
        await Assert.That(request.OccurredAtUtc.Kind).IsEqualTo(DateTimeKind.Utc);
        await Assert.That(observedCancellation).IsEqualTo(cancellation.Token);
        await Assert.That(request.Mutations.Select(mutation => mutation.Key).SequenceEqual([
            GovernanceSettingKeys.Events.UserSubmissionEnabled,
            GovernanceSettingKeys.Events.OrganizationSubmissionEnabled,
            GovernanceSettingKeys.Events.GroupSubmissionEnabled
        ])).IsTrue();
        await Assert.That(request.Mutations.Select(mutation => mutation.JsonValue).SequenceEqual([
            "false", "true", "false"
        ])).IsTrue();
        await Assert.That(request.Mutations.All(mutation =>
            mutation.Kind == PublicationPolicyMutationKind.Set
            && mutation.TenantId is null
            && mutation.IsLocked == false)).IsTrue();
        await Assert.That(request.Mutations.All(mutation =>
            PublicationPolicySettingKeys.All.Contains(mutation.Key, StringComparer.Ordinal))).IsTrue();
        await Assert.That(writes.Select(setting => setting.SettingKey).SequenceEqual([
            GovernanceSettingKeys.Events.CardClickOpensDetailPage
        ])).IsTrue();
    }

    [Test]
    public async Task ApplyEventPolicyPatchAsync_BatchesOnlyTouchedSubmissionKeysInCanonicalOrder()
    {
        var writes = CaptureWrites();
        var actorId = Guid.NewGuid();
        var userChanged = Notification(GovernanceSettingKeys.Events.UserSubmissionEnabled, actorId);
        var groupChanged = Notification(GovernanceSettingKeys.Events.GroupSubmissionEnabled, actorId);
        PublicationPolicyInstanceMutationRequest? request = null;
        _boundary.ApplyInstanceAsync(
                Arg.Any<PublicationPolicyInstanceMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                request = call.ArgAt<PublicationPolicyInstanceMutationRequest>(0);
                return Succeeded(userChanged, groupChanged);
            });

        PublicationPolicyMutationResult result = await _service.ApplyEventPolicyPatchAsync(
            new PatchEventPolicyDto
            {
                AllowUserSubmittedEvents = OptionalUpdate<bool>.Set(false),
                AllowGroupSubmittedEvents = OptionalUpdate<bool>.Set(false),
                EventCardClickOpensDetailPage = OptionalUpdate<bool>.Set(true)
            },
            new EventPolicyDto
            {
                AllowUserSubmittedEvents = false,
                AllowOrganizationSubmittedEvents = true,
                AllowGroupSubmittedEvents = false,
                EventCardClickOpensDetailPage = true
            },
            actorId);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(request).IsNotNull();
        await Assert.That(request!.Mutations.Select(mutation => mutation.Key).SequenceEqual([
            GovernanceSettingKeys.Events.UserSubmissionEnabled,
            GovernanceSettingKeys.Events.GroupSubmissionEnabled
        ])).IsTrue();
        await Assert.That(request.Mutations.All(mutation =>
            mutation.TenantId is null && mutation.IsLocked == false)).IsTrue();
        await Assert.That(writes.Select(setting => setting.SettingKey).SequenceEqual([
            GovernanceSettingKeys.Events.CardClickOpensDetailPage
        ])).IsTrue();
        await Assert.That(result.DeferredNotifications.Select(notification => notification.Key).SequenceEqual([
            GovernanceSettingKeys.Events.UserSubmissionEnabled,
            GovernanceSettingKeys.Events.GroupSubmissionEnabled,
            GovernanceSettingKeys.Events.CardClickOpensDetailPage
        ])).IsTrue();
    }

    [Test]
    [Arguments("event_reporting_intake_unsafe_publication_policy")]
    [Arguments("event_reporting_intake_policy_invalid")]
    public async Task ApplyEventPolicyPatchAsync_WhenBoundaryRejects_ReturnsExactFailureBeforeCardClickWrite(
        string failureCode)
    {
        var writes = CaptureWrites();
        _boundary.ApplyInstanceAsync(
                Arg.Any<PublicationPolicyInstanceMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Failed(failureCode));

        PublicationPolicyMutationResult result = await _service.ApplyEventPolicyPatchAsync(
            new PatchEventPolicyDto
            {
                AllowUserSubmittedEvents = OptionalUpdate<bool>.Set(false),
                EventCardClickOpensDetailPage = OptionalUpdate<bool>.Set(true)
            },
            new EventPolicyDto
            {
                AllowUserSubmittedEvents = false,
                EventCardClickOpensDetailPage = true
            },
            Guid.NewGuid());

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(failureCode);
        await Assert.That(result.DeferredNotifications).IsEmpty();
        await Assert.That(writes).IsEmpty();
        _resolver.DidNotReceive().InvalidateCache(Arg.Any<SettingScope?>(), Arg.Any<Guid?>());
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApplyEventPolicyPatchAsync_WhenOnlyCardClickChanges_DoesNotInvokeBoundary()
    {
        var writes = CaptureWrites();
        var actorId = Guid.NewGuid();

        PublicationPolicyMutationResult result = await _service.ApplyEventPolicyPatchAsync(
            new PatchEventPolicyDto
            {
                EventCardClickOpensDetailPage = OptionalUpdate<bool>.Set(false)
            },
            new EventPolicyDto { EventCardClickOpensDetailPage = false },
            actorId);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(writes.Select(setting => setting.SettingKey).SequenceEqual([
            GovernanceSettingKeys.Events.CardClickOpensDetailPage
        ])).IsTrue();
        await Assert.That(result.DeferredNotifications.Select(notification => notification.Key).SequenceEqual([
            GovernanceSettingKeys.Events.CardClickOpensDetailPage
        ])).IsTrue();
        await _boundary.DidNotReceiveWithAnyArgs().ApplyInstanceAsync(default!, default);
    }

    [Test]
    public async Task OrdinaryUpserts_RejectEveryGuardedKeyBeforeRepositoryAccess()
    {
        foreach (string key in PublicationPolicySettingKeys.All)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _upsert.UpsertValueAsync(key, "false", Guid.NewGuid()));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                InvokeDeferredUpsertAsync(key, Guid.NewGuid()));
        }

        await _settings.DidNotReceive().UpsertAsync(
            Arg.Any<SystemSetting>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApprovedCoordinator_DelegatesOneInstanceBatchWithActorAndCancellation()
    {
        var actorId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow;
        using var cancellation = new CancellationTokenSource();
        var mutations = ImmutableArray.Create(new PublicationPolicySettingMutation(
            GovernanceSettingKeys.Events.UserSubmissionEnabled,
            PublicationPolicyMutationKind.Set,
            "false",
            TenantId: null,
            IsLocked: false));

        PublicationPolicyMutationResult result = await _upsert.ApplyInstancePublicationPolicyAsync(
            new PublicationPolicyInstanceMutationRequest(actorId, occurredAtUtc, mutations),
            cancellation.Token);

        await Assert.That(result.Success).IsTrue();
        await _boundary.Received(1).ApplyInstanceAsync(
            Arg.Is<PublicationPolicyInstanceMutationRequest>(request => request != null
                && request.ActorUserId == actorId
                && request.OccurredAtUtc == occurredAtUtc
                && request.Mutations == mutations),
            cancellation.Token);
    }

    [Test]
    public async Task ApplySettingsAsync_WhenPublicationPolicyIsRejected_ThrowsMachineFailureBeforeLaterPolicyWrites()
    {
        var writes = CaptureWrites();
        const string failureCode = "event_reporting_intake_unsafe_publication_policy";
        _boundary.ApplyInstanceAsync(
                Arg.Any<PublicationPolicyInstanceMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Failed(failureCode));

        FluentValidation.ValidationException exception = (await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.ApplySettingsAsync(
                defaultTenantId: null,
                settings: CreateSettings(),
                actorUserId: Guid.NewGuid())))!;

        await Assert.That(exception.Errors.Single().ErrorCode).IsEqualTo(failureCode);
        await Assert.That(writes.Any(setting => setting.SettingKey is
            GovernanceSettingKeys.Events.CardClickOpensDetailPage or
            GovernanceSettingKeys.Organizations.VerificationRequired or
            GovernanceSettingKeys.Organizations.SelfRegistrationEnabled or
            GovernanceSettingKeys.Groups.SelfRegistrationEnabled)).IsFalse();
    }

    private async Task InvokeDeferredUpsertAsync(string key, Guid actorId)
    {
        MethodInfo method = typeof(SettingUpsertService).GetMethod(
            "UpsertValueWithDeferredInvalidationAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Deferred upsert seam is missing.");
        Task operation;
        try
        {
            operation = (Task)method.Invoke(
                _upsert,
                [key, "false", actorId, false, CancellationToken.None])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }

        await operation;
    }

    private List<SystemSetting> CaptureWrites()
    {
        var writes = new List<SystemSetting>();
        _settings.UpsertAsync(
                Arg.Do<SystemSetting>(setting => writes.Add(setting)),
                Arg.Any<CancellationToken>())
            .Returns((string?)null);
        return writes;
    }

    private static SettingChangedNotification Notification(string key, Guid actorId) => new(
        key,
        "true",
        "false",
        SettingSource.SystemDefault,
        tenantId: null,
        actorId,
        DateTime.UtcNow);

    private static Task<PublicationPolicyMutationResult> Succeeded(
        params SettingChangedNotification[] notifications) =>
        Task.FromResult(new PublicationPolicyMutationResult(true, null, string.Empty, [.. notifications]));

    private static Task<PublicationPolicyMutationResult> Failed(string failureCode) =>
        Task.FromResult(new PublicationPolicyMutationResult(false, failureCode, string.Empty, []));

    private static InstanceGovernanceSettings CreateSettings() => new()
    {
        DeploymentMode = new DeploymentModeDto(),
        Modules = new ModuleSettingsDto(),
        EventPolicy = new EventPolicyDto(),
        OrganizationPolicy = new OrganizationPolicyDto(),
        Branding = new BrandingSettingsDto(),
        Domains = new DomainSettingsDto(),
        AiAssistant = new AiAssistantGovernanceSettingsDto(),
        Mcp = new McpGovernanceSettingsDto(),
        TenantDelegation = new TenantDelegationSettingsDto { DefaultPublicHomePage = "EventList" },
        AdminPortal = new AdminPortalSettingsDto(),
        LocationPrivacy = new LocationPrivacyGovernanceSettingsDto(),
        RenderPolicy = new RenderPolicySettingsDto
        {
            RenderPolicyVersion = 1,
            RenderPolicyPreset = "AllInteractiveServer",
            GlobalRenderMode = "InteractiveServer",
            PublicSeoRenderMode = "InteractiveServer",
            OperationalRenderMode = "InteractiveServer",
            AdminRenderMode = "InteractiveServer",
            OnboardingRenderMode = "InteractiveAuto"
        }
    };
}
