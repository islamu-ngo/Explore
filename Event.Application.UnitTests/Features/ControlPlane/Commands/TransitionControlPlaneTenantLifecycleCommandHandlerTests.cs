// ABOUTME: Unit tests for audited control-plane tenant lifecycle transitions.
// ABOUTME: Verifies reason enforcement, transition guards, and lifecycle log persistence.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.ControlPlane;
using Explore.Application.Features.ControlPlane.Handlers.Commands;
using Explore.Application.Features.ControlPlane.Requests.Commands;
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
        await tenantRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await lifecycleLogRepository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Test]
    public async Task Handle_WhenActiveTenantIsSuspended_UpdatesTenantAndWritesLifecycleLog()
    {
        var operatorId = Guid.NewGuid();
        var tenant = CreateTenant(TenantStatusEnum.Active);
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        TenantLifecycleLog? capturedLog = null;
        tenantRepository.GetById(tenant.Id).Returns(tenant);
        lifecycleLogRepository.Create(Arg.Do<TenantLifecycleLog>(log => capturedLog = log))
            .Returns(callInfo => Task.FromResult(callInfo.Arg<TenantLifecycleLog>()));
        var handler = CreateSut(tenantRepository, lifecycleLogRepository, operatorId);

        var result = await handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(tenant.Id, TenantStatusEnum.Suspended, "  policy breach  "),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id.OldStatusId).IsEqualTo((int)TenantStatusEnum.Active);
        await Assert.That(result.Id.NewStatusId).IsEqualTo((int)TenantStatusEnum.Suspended);
        await Assert.That(result.Id.Reason).IsEqualTo("policy breach");
        await Assert.That(tenant.TenantStatusId).IsEqualTo((int)TenantStatusEnum.Suspended);
        await Assert.That(tenant.UpdatedBy).IsEqualTo(operatorId);
        await tenantRepository.Received(1).Update(tenant);
        await lifecycleLogRepository.Received(1).Create(Arg.Any<TenantLifecycleLog>());
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
        tenantRepository.GetById(tenant.Id).Returns(tenant);
        var handler = CreateSut(tenantRepository, lifecycleLogRepository);

        var result = await handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(tenant.Id, TenantStatusEnum.Active, reason: null),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Cannot transition tenant from Purged to Active.");
        await tenantRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await lifecycleLogRepository.DidNotReceiveWithAnyArgs().Create(default!);
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
        await tenantRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await lifecycleLogRepository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Test]
    public async Task Handle_WhenSchedulingPurgeWithoutTenantSlugConfirmation_ReturnsFailureAndDoesNotPersist()
    {
        var tenant = CreateTenant(TenantStatusEnum.Archived);
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        tenantRepository.GetById(tenant.Id).Returns(tenant);
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
        await tenantRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await lifecycleLogRepository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Test]
    public async Task Handle_WhenArchivedTenantPurgeIsScheduled_UpdatesStatusAndWritesLifecycleLog()
    {
        var operatorId = Guid.NewGuid();
        var tenant = CreateTenant(TenantStatusEnum.Archived);
        var tenantRepository = Substitute.For<ITenantRepository>();
        var lifecycleLogRepository = Substitute.For<ITenantLifecycleLogRepository>();
        TenantLifecycleLog? capturedLog = null;
        tenantRepository.GetById(tenant.Id).Returns(tenant);
        lifecycleLogRepository.Create(Arg.Do<TenantLifecycleLog>(log => capturedLog = log))
            .Returns(callInfo => Task.FromResult(callInfo.Arg<TenantLifecycleLog>()));
        var handler = CreateSut(tenantRepository, lifecycleLogRepository, operatorId);

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
        await Assert.That(tenant.TenantStatusId).IsEqualTo((int)TenantStatusEnum.Purged);
        await tenantRepository.Received(1).Update(tenant);
        await lifecycleLogRepository.Received(1).Create(Arg.Any<TenantLifecycleLog>());
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
        tenantRepository.GetById(tenant.Id).Returns(tenant);
        var handler = CreateSut(tenantRepository, lifecycleLogRepository);

        var result = await handler.Handle(
            new TransitionControlPlaneTenantLifecycleCommand(tenant.Id, TenantStatusEnum.Purged, "confirmed", tenant.Slug),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("Cannot transition tenant from Active to Purged.");
        await tenantRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await lifecycleLogRepository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    private static TransitionControlPlaneTenantLifecycleCommandHandler CreateSut(
        ITenantRepository tenantRepository,
        ITenantLifecycleLogRepository lifecycleLogRepository,
        Guid? operatorId = null)
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
                return operation(CancellationToken.None);
            });

        return new TransitionControlPlaneTenantLifecycleCommandHandler(
            tenantRepository,
            lifecycleLogRepository,
            currentUserService,
            unitOfWork);
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
