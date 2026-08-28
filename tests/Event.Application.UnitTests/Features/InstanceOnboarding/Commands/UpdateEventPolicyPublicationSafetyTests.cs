// ABOUTME: RED specifications for transactional instance event-policy publication effects.
// ABOUTME: Requires boundary failures to be machine-coded and effects to occur only after commit.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Instance;
using Explore.Application.Features.InstanceOnboarding.Handlers.Commands;
using Explore.Application.Features.InstanceOnboarding.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Application.Settings;
using Explore.Domain;
using Explore.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.InstanceOnboarding.Commands;

public sealed class UpdateEventPolicyPublicationSafetyTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly IAdminContext _admin = Substitute.For<IAdminContext>();
    private readonly IHierarchicalSettingsResolver _resolver = Substitute.For<IHierarchicalSettingsResolver>();
    private readonly ISystemSettingRepository _settings = Substitute.For<ISystemSettingRepository>();
    private readonly IModuleCapabilityService _modules = Substitute.For<IModuleCapabilityService>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IPublicationPolicyMutationBoundary _boundary =
        Substitute.For<IPublicationPolicyMutationBoundary>();

    public UpdateEventPolicyPublicationSafetyTests()
    {
        _admin.IsInstanceAdminAsync(_actorId, Arg.Any<CancellationToken>()).Returns(true);
        _resolver.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns([]);
        _settings.UpsertAsync(Arg.Any<SystemSetting>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        _mediator.Publish(Arg.Any<SettingChangedNotification>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _boundary.ApplyInstanceAsync(
                Arg.Any<PublicationPolicyInstanceMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Succeeded());
    }

    [Test]
    public async Task GuardedAndCardClickPatch_WritesInsideCallerTransactionThenPublishesCanonicalNotifications()
    {
        var calls = new List<string>();
        var guarded = Notification(GovernanceSettingKeys.Events.UserSubmissionEnabled);
        _boundary.ApplyInstanceAsync(
                Arg.Any<PublicationPolicyInstanceMutationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("boundary");
                return Succeeded(guarded);
            });
        _settings.UpsertAsync(
                Arg.Is<SystemSetting>(setting => setting != null
                    && setting.SettingKey == GovernanceSettingKeys.Events.CardClickOpensDetailPage),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls.Add("card-click");
                return (string?)null;
            });
        _mediator.Publish(Arg.Any<SettingChangedNotification>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                calls.Add($"publish:{call.ArgAt<SettingChangedNotification>(0).Key}");
                return Task.CompletedTask;
            });
        var unitOfWork = new RecordingUnitOfWork(calls);
        var handler = CreateConcreteHandler(unitOfWork);

        var result = await handler.Handle(new UpdateEventPolicyCommand
        {
            UserId = _actorId,
            Patch = new PatchEventPolicyDto
            {
                AllowUserSubmittedEvents = OptionalUpdate<bool>.Set(false),
                EventCardClickOpensDetailPage = OptionalUpdate<bool>.Set(true)
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(calls.SequenceEqual([
            "transaction-start",
            "boundary",
            "card-click",
            "transaction-commit",
            $"publish:{GovernanceSettingKeys.Events.UserSubmissionEnabled}",
            $"publish:{GovernanceSettingKeys.Events.CardClickOpensDetailPage}"
        ])).IsTrue();
    }

    [Test]
    [Arguments("event_reporting_intake_unsafe_publication_policy")]
    [Arguments("event_reporting_intake_policy_invalid")]
    public async Task BoundaryRejection_MapsExactFailureCodeWithoutEffects(string failureCode)
    {
        var service = Substitute.For<IInstanceGovernanceSettingService>();
        service.ReadSettingsAsync().Returns(CreateReadSettings());
        service.ApplyEventPolicyPatchAsync(
                Arg.Any<PatchEventPolicyDto>(),
                Arg.Any<EventPolicyDto>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(Failed(failureCode));
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new UpdateEventPolicyCommandHandler(_admin, service, unitOfWork, _mediator);

        var result = await handler.Handle(new UpdateEventPolicyCommand
        {
            UserId = _actorId,
            Patch = new PatchEventPolicyDto
            {
                AllowUserSubmittedEvents = OptionalUpdate<bool>.Set(false),
                EventCardClickOpensDetailPage = OptionalUpdate<bool>.Set(true)
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(failureCode);
        await Assert.That(result.Errors!.SequenceEqual([failureCode])).IsTrue();
        await Assert.That(unitOfWork.CommitCount).IsEqualTo(1);
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task GuardedOrCardClickWriteException_RollsBackAndPublishesNothing(bool boundaryThrows)
    {
        var unitOfWork = new RecordingUnitOfWork();
        if (boundaryThrows)
        {
            _boundary.ApplyInstanceAsync(
                    Arg.Any<PublicationPolicyInstanceMutationRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromException<PublicationPolicyMutationResult>(
                    new InvalidOperationException("boundary failed")));
        }
        else
        {
            _settings.UpsertAsync(
                    Arg.Is<SystemSetting>(setting => setting != null
                        && setting.SettingKey == GovernanceSettingKeys.Events.CardClickOpensDetailPage),
                    Arg.Any<CancellationToken>())
                .Returns<Task<string?>>(_ => throw new InvalidOperationException("card-click failed"));
        }
        var handler = CreateConcreteHandler(unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new UpdateEventPolicyCommand
        {
            UserId = _actorId,
            Patch = new PatchEventPolicyDto
            {
                AllowUserSubmittedEvents = OptionalUpdate<bool>.Set(false),
                EventCardClickOpensDetailPage = OptionalUpdate<bool>.Set(true)
            }
        }, CancellationToken.None));

        await Assert.That(unitOfWork.CommitCount).IsEqualTo(0);
        await Assert.That(unitOfWork.RollbackCount).IsEqualTo(1);
        await _mediator.DidNotReceive().Publish(
            Arg.Any<SettingChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CardClickOnlyPatch_PreservesDeferredWriteWithoutInvokingBoundary()
    {
        var unitOfWork = new RecordingUnitOfWork();
        var handler = CreateConcreteHandler(unitOfWork);

        var result = await handler.Handle(new UpdateEventPolicyCommand
        {
            UserId = _actorId,
            Patch = new PatchEventPolicyDto
            {
                EventCardClickOpensDetailPage = OptionalUpdate<bool>.Set(true)
            }
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(unitOfWork.CommitCount).IsEqualTo(1);
        await _boundary.DidNotReceiveWithAnyArgs().ApplyInstanceAsync(default!, default);
        await _settings.Received(1).UpsertAsync(
            Arg.Is<SystemSetting>(setting => setting != null
                && setting.SettingKey == GovernanceSettingKeys.Events.CardClickOpensDetailPage),
            Arg.Any<CancellationToken>());
    }

    private static InstanceGovernanceSettings CreateReadSettings() => new()
    {
        DeploymentMode = new DeploymentModeDto(),
        Modules = new ModuleSettingsDto(),
        EventPolicy = new EventPolicyDto(),
        OrganizationPolicy = new OrganizationPolicyDto(),
        Branding = new BrandingSettingsDto(),
        Domains = new DomainSettingsDto(),
        TenantDelegation = new TenantDelegationSettingsDto(),
        AdminPortal = new AdminPortalSettingsDto(),
        AiAssistant = new AiAssistantGovernanceSettingsDto(),
        Mcp = new McpGovernanceSettingsDto(),
        RenderPolicy = new RenderPolicySettingsDto()
    };

    private UpdateEventPolicyCommandHandler CreateConcreteHandler(IUnitOfWork unitOfWork)
    {
        var upsert = new SettingUpsertService(
            _settings,
            _mediator,
            publicationPolicyMutationBoundary: _boundary);
        var service = new InstanceGovernanceSettingService(
            _resolver,
            upsert,
            _modules,
            Substitute.For<ILogger<InstanceGovernanceSettingService>>());
        return new UpdateEventPolicyCommandHandler(_admin, service, unitOfWork, _mediator);
    }

    private SettingChangedNotification Notification(string key) => new(
        key,
        "true",
        "false",
        SettingSource.SystemDefault,
        tenantId: null,
        _actorId,
        DateTime.UtcNow);

    private static Task<PublicationPolicyMutationResult> Succeeded(
        params SettingChangedNotification[] notifications) =>
        Task.FromResult(new PublicationPolicyMutationResult(true, null, string.Empty, [.. notifications]));

    private static Task<PublicationPolicyMutationResult> Failed(string failureCode) =>
        Task.FromResult(new PublicationPolicyMutationResult(false, failureCode, string.Empty, []));

    private sealed class RecordingUnitOfWork(List<string>? calls = null) : IUnitOfWork
    {
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }

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
            calls?.Add("transaction-start");
            try
            {
                T result = await operation(ct);
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
            CancellationToken ct = default) =>
            ExecuteInTransactionAsync(operation, ct);
    }
}
