// ABOUTME: Unit tests for audited control-plane tenant lifecycle transitions.
// ABOUTME: Verifies reason enforcement, transition guards, and lifecycle log persistence.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.Exceptions;
using Explore.Application.Features.ControlPlane.Handlers.Commands;
using Explore.Application.Features.ControlPlane.Requests.Commands;
using Explore.Application.Features.Management;
using Explore.Application.Management;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.ControlPlane.Commands;

public sealed class TransitionControlPlaneTenantLifecycleCommandHandlerTests
{
    [Test]
    public async Task Handle_WhenSuspendingWithoutReason_ReturnsFailureAndDoesNotPersist()
    {
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        var handler = CreateSut(tenantRepository, lifecycleLogRepository);

        var result = await handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(Guid.NewGuid(), TenantStatusEnum.Suspended, reason: null),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors ?? []).Contains("Suspended requires a reason.");
        await tenantRepository.DidNotReceiveWithAnyArgs().TryTransitionStatusAsync(default, default, default, default, default, default);
        await lifecycleLogRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task Handle_WhenActiveTenantIsSuspended_UsesCompareAndSwapAndWritesLifecycleLog()
    {
        var operatorId = Guid.NewGuid();
        var tenant = CreateTenant(TenantStatusEnum.Active);
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        TenantLifecycleLog? capturedLog = null;
        tenantRepository.GetByIdAsNoTrackingAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        tenantRepository.TryTransitionStatusAsync(
                tenant.Id,
                (int)TenantStatusEnum.Active,
                (int)TenantStatusEnum.Suspended,
                Arg.Any<DateTime>(),
                operatorId,
                Arg.Any<CancellationToken>())
            .Returns(true);
        lifecycleLogRepository.CreateAsync(Arg.Do<TenantLifecycleLog>(log => capturedLog = log), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<TenantLifecycleLog>()));
        var handler = CreateSut(tenantRepository, lifecycleLogRepository, operatorId);

        var result = await handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(tenant.Id, TenantStatusEnum.Suspended, "  policy breach  "),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id.OldStatusId).IsEqualTo((int)TenantStatusEnum.Active);
        await Assert.That(result.Id.NewStatusId).IsEqualTo((int)TenantStatusEnum.Suspended);
        await Assert.That(result.Id.Reason).IsEqualTo("policy breach");
        await Assert.That(tenant.TenantStatusId).IsEqualTo((int)TenantStatusEnum.Active);
        await tenantRepository.Received(1).TryTransitionStatusAsync(
            tenant.Id,
            (int)TenantStatusEnum.Active,
            (int)TenantStatusEnum.Suspended,
            Arg.Any<DateTime>(),
            operatorId,
            Arg.Any<CancellationToken>());
        await tenantRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await lifecycleLogRepository.Received(1).CreateAsync(Arg.Any<TenantLifecycleLog>(), Arg.Any<CancellationToken>());
        await Assert.That(capturedLog).IsNotNull();
        await Assert.That(capturedLog!.TenantId).IsEqualTo(tenant.Id);
        await Assert.That(capturedLog.OldStatusId).IsEqualTo((int)TenantStatusEnum.Active);
        await Assert.That(capturedLog.NewStatusId).IsEqualTo((int)TenantStatusEnum.Suspended);
        await Assert.That(capturedLog.TransitionedByUserId).IsEqualTo(operatorId);
        await Assert.That(capturedLog.Reason).IsEqualTo("policy breach");
    }

    [Test]
    public async Task Handle_WhenPurgedTenantIsReactivated_ReturnsFailureAndDoesNotPersist()
    {
        var tenant = CreateTenant(TenantStatusEnum.Purged);
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        tenantRepository.GetByIdAsNoTrackingAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var handler = CreateSut(tenantRepository, lifecycleLogRepository);

        var result = await handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(tenant.Id, TenantStatusEnum.Active, reason: null),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Cannot transition tenant from Purged to Active.");
        await tenantRepository.DidNotReceiveWithAnyArgs().TryTransitionStatusAsync(default, default, default, default, default, default);
        await lifecycleLogRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task Handle_WhenSchedulingPurgeWithoutReason_ReturnsFailureAndDoesNotPersist()
    {
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        var handler = CreateSut(tenantRepository, lifecycleLogRepository);

        var result = await handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(Guid.NewGuid(), TenantStatusEnum.Purged, reason: null),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors ?? []).Contains("Purged requires a reason.");
        await tenantRepository.DidNotReceiveWithAnyArgs().TryTransitionStatusAsync(default, default, default, default, default, default);
        await lifecycleLogRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task Handle_WhenSchedulingPurgeWithoutTenantSlugConfirmation_ReturnsFailureAndDoesNotPersist()
    {
        var tenant = CreateTenant(TenantStatusEnum.Archived);
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        tenantRepository.GetByIdAsNoTrackingAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var handler = CreateSut(tenantRepository, lifecycleLogRepository);

        var result = await handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(
                tenant.Id,
                TenantStatusEnum.Purged,
                "operator confirmed backup",
                confirmationText: "wrong-slug"),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains($"Purged requires confirmation with tenant slug '{tenant.Slug}'.");
        await tenantRepository.DidNotReceiveWithAnyArgs().TryTransitionStatusAsync(default, default, default, default, default, default);
        await lifecycleLogRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task Handle_WhenArchivedTenantPurgeIsScheduled_UsesCompareAndSwapAndWritesLifecycleLog()
    {
        var operatorId = Guid.NewGuid();
        var tenant = CreateTenant(TenantStatusEnum.Archived);
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        var emailDispatchOutboxRepository = Substitute.For<IEmailDispatchOutboxRepository>();
        TenantLifecycleLog? capturedLog = null;
        tenantRepository.GetByIdAsNoTrackingAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        tenantRepository.TryTransitionStatusAsync(
                tenant.Id,
                (int)TenantStatusEnum.Archived,
                (int)TenantStatusEnum.Purged,
                Arg.Any<DateTime>(),
                operatorId,
                Arg.Any<CancellationToken>())
            .Returns(true);
        lifecycleLogRepository.CreateAsync(Arg.Do<TenantLifecycleLog>(log => capturedLog = log), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<TenantLifecycleLog>()));
        var handler = CreateSut(
            tenantRepository,
            lifecycleLogRepository,
            operatorId,
            emailDispatchOutboxRepository);

        var result = await handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(
                tenant.Id,
                TenantStatusEnum.Purged,
                "  operator confirmed backup  ",
                confirmationText: tenant.Slug),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Tenant purge scheduled.");
        await Assert.That(result.Id.OldStatusId).IsEqualTo((int)TenantStatusEnum.Archived);
        await Assert.That(result.Id.NewStatusId).IsEqualTo((int)TenantStatusEnum.Purged);
        await Assert.That(result.Id.Reason).IsEqualTo("operator confirmed backup");
        await Assert.That(tenant.TenantStatusId).IsEqualTo((int)TenantStatusEnum.Archived);
        await tenantRepository.Received(1).TryTransitionStatusAsync(
            tenant.Id,
            (int)TenantStatusEnum.Archived,
            (int)TenantStatusEnum.Purged,
            Arg.Any<DateTime>(),
            operatorId,
            Arg.Any<CancellationToken>());
        await tenantRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await lifecycleLogRepository.Received(1).CreateAsync(Arg.Any<TenantLifecycleLog>(), Arg.Any<CancellationToken>());
        await emailDispatchOutboxRepository.Received(1).SuppressAndRedactTenant(
            tenant.Id,
            operatorId,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await Assert.That(capturedLog).IsNotNull();
        await Assert.That(capturedLog!.OldStatusId).IsEqualTo((int)TenantStatusEnum.Archived);
        await Assert.That(capturedLog.NewStatusId).IsEqualTo((int)TenantStatusEnum.Purged);
        await Assert.That(capturedLog.Reason).IsEqualTo("operator confirmed backup");
    }

    [Test]
    public async Task Handle_WhenActiveTenantPurgeIsScheduled_ReturnsFailureAndDoesNotPersist()
    {
        var tenant = CreateTenant(TenantStatusEnum.Active);
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        tenantRepository.GetByIdAsNoTrackingAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var handler = CreateSut(tenantRepository, lifecycleLogRepository);

        var result = await handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(tenant.Id, TenantStatusEnum.Purged, "confirmed", tenant.Slug),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Cannot transition tenant from Active to Purged.");
        await tenantRepository.DidNotReceiveWithAnyArgs().TryTransitionStatusAsync(default, default, default, default, default, default);
        await lifecycleLogRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task Handle_WhenTenantAlreadyHasTargetStatus_DoesNotCompareAndSwapOrWriteLog()
    {
        var tenant = CreateTenant(TenantStatusEnum.Active);
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        tenantRepository.GetByIdAsNoTrackingAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var handler = CreateSut(tenantRepository, lifecycleLogRepository);

        var result = await handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(tenant.Id, TenantStatusEnum.Active, reason: null),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Tenant already has the requested lifecycle status.");
        await tenantRepository.DidNotReceiveWithAnyArgs().TryTransitionStatusAsync(default, default, default, default, default, default);
        await lifecycleLogRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task Handle_WhenCompareAndSwapLoses_ThrowsConflictAndDoesNotWriteLog()
    {
        var tenant = CreateTenant(TenantStatusEnum.Active);
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        tenantRepository.GetByIdAsNoTrackingAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        tenantRepository.TryTransitionStatusAsync(
                tenant.Id,
                (int)TenantStatusEnum.Active,
                (int)TenantStatusEnum.Suspended,
                Arg.Any<DateTime>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = CreateSut(tenantRepository, lifecycleLogRepository);

        var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(tenant.Id, TenantStatusEnum.Suspended, "policy breach"),
            CancellationToken.None));

        await Assert.That(exception!.Code).IsEqualTo(ConcurrencyConflictException.ConcurrentUpdate);
        await Assert.That(exception.Message).IsEqualTo("Tenant lifecycle status changed since it was loaded. Reload and retry the transition.");
        await Assert.That(exception.EntityType).IsEqualTo(nameof(Tenant));
        await Assert.That(exception.EntityId).IsEqualTo(tenant.Id.ToString());
        await lifecycleLogRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task Handle_ForwardsTransactionCancellationTokenToEveryRepositoryCall()
    {
        var operatorId = Guid.NewGuid();
        var tenant = CreateTenant(TenantStatusEnum.Active);
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        tenantRepository.GetByIdAsNoTrackingAsync(tenant.Id, cancellationToken).Returns(tenant);
        tenantRepository.TryTransitionStatusAsync(
                tenant.Id,
                (int)TenantStatusEnum.Active,
                (int)TenantStatusEnum.Suspended,
                Arg.Any<DateTime>(),
                operatorId,
                cancellationToken)
            .Returns(true);
        lifecycleLogRepository.CreateAsync(Arg.Any<TenantLifecycleLog>(), cancellationToken)
            .Returns(callInfo => Task.FromResult(callInfo.Arg<TenantLifecycleLog>()));
        var handler = CreateSut(tenantRepository, lifecycleLogRepository, operatorId);

        await handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(tenant.Id, TenantStatusEnum.Suspended, "policy breach"),
            cancellationToken);

        await tenantRepository.Received(1).GetByIdAsNoTrackingAsync(tenant.Id, cancellationToken);
        await tenantRepository.Received(1).TryTransitionStatusAsync(
            tenant.Id,
            (int)TenantStatusEnum.Active,
            (int)TenantStatusEnum.Suspended,
            Arg.Any<DateTime>(),
            operatorId,
            cancellationToken);
        await lifecycleLogRepository.Received(1).CreateAsync(Arg.Any<TenantLifecycleLog>(), cancellationToken);
    }

    private static TransitionControlPlaneTenantLifecycleCommandHandler CreateSut(
        ITenantRepository tenantRepository,
        ITenantLifecycleLogRepository lifecycleLogRepository,
        Guid? operatorId = null,
        IEmailDispatchOutboxRepository? emailDispatchOutboxRepository = null)
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(operatorId ?? Guid.NewGuid());
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<BaseCommandResponse<ControlPlaneTenantLifecycleTransitionDto>>>>();
                return operation(callInfo.Arg<CancellationToken>());
            });

        return new TransitionControlPlaneTenantLifecycleCommandHandler(
            tenantRepository,
            lifecycleLogRepository,
            emailDispatchOutboxRepository ?? Substitute.For<IEmailDispatchOutboxRepository>(),
            currentUserService,
            unitOfWork,
            Substitute.For<ISettingMutationLock>(),
            new TenantActivationCapacityPolicy(
                Substitute.For<IInstanceBootstrapStateRepository>(),
                tenantRepository,
                Substitute.For<IManagedTenantProvisioningOperationRepository>(),
                Microsoft.Extensions.Options.Options.Create(new ManagedControlPlaneOptions())));
    }

    private static Tenant CreateTenant(TenantStatusEnum status)
    {
        var tenantStatus = new TenantStatus
        {
            Id = (int)status,
            MasterCode = status.ToString().ToUpperInvariant(),
            FullName = status.ToString(),
            IsActiveState = status == TenantStatusEnum.Active
        };

        return new Tenant
        {
            Id = Guid.NewGuid(),
            FullName = "Demo Tenant",
            Slug = "demo",
            TenantStatusId = (int)status,
            TenantStatus = tenantStatus,
            CreatedAt = DateTime.UtcNow
        };
    }
}
