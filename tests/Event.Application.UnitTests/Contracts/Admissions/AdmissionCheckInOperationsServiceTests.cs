// ABOUTME: Verifies authorized admission stop, restore, reconcile, and exact-target health orchestration.
// ABOUTME: Proves every operation is transactional, audited, bounded, and denied before persistence.

using Explore.Application.Authorization;
using Explore.Application.Contracts.Admissions;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace ApplicationUnitTests.Contracts.Admissions;

public sealed class AdmissionCheckInOperationsServiceTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid EventId = Guid.CreateVersion7();
    private static readonly Guid TargetId = Guid.CreateVersion7();
    private static readonly Guid ActorId = Guid.CreateVersion7();
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task StopRestoreAndReconcileAreTransactionalAuditedAndHealthVisible()
    {
        AdmissionTarget target = AdmissionTarget.Create(
            TargetId,
            TenantId,
            EventId,
            AdmissionTargetTypeEnum.Event,
            null,
            null);
        IAdmissionTargetOperationsRepository targets =
            Substitute.For<IAdmissionTargetOperationsRepository>();
        targets.GetAsync(TenantId, EventId, TargetId, Arg.Any<CancellationToken>())
            .Returns(target);
        targets.UpdateAsync(target, Arg.Any<CancellationToken>()).Returns(target);
        IAdmissionCheckInHealthProbe probe = Substitute.For<IAdmissionCheckInHealthProbe>();
        probe.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
        IAuditLogRepository audit = Substitute.For<IAuditLogRepository>();
        var unitOfWork = new AdmissionOperationsUnitOfWork();
        var service = new AdmissionCheckInOperationsService(
            AllowedAuthorization(),
            targets,
            probe,
            audit,
            unitOfWork,
            new AdmissionOperationsTimeProvider(UtcNow));

        AdmissionCheckInOperationalResult? stopped = await Execute(
            service,
            AdmissionCheckInOperationalAction.Stop,
            AdmissionCheckInOperationalReasonCode.DeviceLoss);
        AdmissionCheckInHealthResult? stoppedHealth = await Health(service);
        AdmissionCheckInOperationalResult? restored = await Execute(
            service,
            AdmissionCheckInOperationalAction.Restore,
            AdmissionCheckInOperationalReasonCode.OperatorCorrection);
        AdmissionCheckInOperationalResult? reconciled = await Execute(
            service,
            AdmissionCheckInOperationalAction.Reconcile,
            AdmissionCheckInOperationalReasonCode.PostIncidentReconciliation);
        AdmissionCheckInHealthResult? restoredHealth = await Health(service);

        await Assert.That(stopped?.Status).IsEqualTo(AdmissionCheckInOperationalStatus.Stopped);
        await Assert.That(stoppedHealth?.Status).IsEqualTo(AdmissionCheckInOperationalStatus.Stopped);
        await Assert.That(restored?.Status).IsEqualTo(AdmissionCheckInOperationalStatus.Active);
        await Assert.That(reconciled?.Status).IsEqualTo(AdmissionCheckInOperationalStatus.Active);
        await Assert.That(restoredHealth?.InfrastructureStatus)
            .IsEqualTo(AdmissionCheckInDependencyStatus.Available);
        await Assert.That(unitOfWork.TransactionCount).IsEqualTo(3);
        await audit.Received(3).Create(Arg.Is<AuditLog>(entry =>
            entry.TenantId == TenantId &&
            entry.EntityId == TargetId.ToString("D") &&
            entry.ActorId == ActorId &&
            !entry.NewValues.Contains("DeviceLabel", StringComparison.Ordinal)));
    }

    [Test]
    public async Task DeniedOperatorCannotReadHealthOrMutateTarget()
    {
        IAuthorizationProvider denied = Substitute.For<IAuthorizationProvider>();
        denied.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Deny(AuthorizationProviderMetadata.Local));
        IAdmissionTargetOperationsRepository targets =
            Substitute.For<IAdmissionTargetOperationsRepository>();
        var service = new AdmissionCheckInOperationsService(
            denied,
            targets,
            Substitute.For<IAdmissionCheckInHealthProbe>(),
            Substitute.For<IAuditLogRepository>(),
            new AdmissionOperationsUnitOfWork(),
            new AdmissionOperationsTimeProvider(UtcNow));

        AdmissionCheckInHealthResult? health = await Health(service);
        AdmissionCheckInOperationalResult? result = await Execute(
            service,
            AdmissionCheckInOperationalAction.Stop,
            AdmissionCheckInOperationalReasonCode.DeviceLoss);

        await Assert.That(health).IsNull();
        await Assert.That(result).IsNull();
        await targets.DidNotReceiveWithAnyArgs().GetAsync(default, default, default, default);
    }

    [Test]
    public async Task UnavailableDependencyDoesNotFabricateStoppedTargetState()
    {
        IAdmissionTargetOperationsRepository targets =
            Substitute.For<IAdmissionTargetOperationsRepository>();
        IAdmissionCheckInHealthProbe probe = Substitute.For<IAdmissionCheckInHealthProbe>();
        probe.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);
        var service = new AdmissionCheckInOperationsService(
            AllowedAuthorization(),
            targets,
            probe,
            Substitute.For<IAuditLogRepository>(),
            new AdmissionOperationsUnitOfWork(),
            new AdmissionOperationsTimeProvider(UtcNow));

        AdmissionCheckInHealthResult? result = await Health(service);

        await Assert.That(result?.Status)
            .IsEqualTo(AdmissionCheckInOperationalStatus.Unavailable);
        await Assert.That(result?.InfrastructureStatus)
            .IsEqualTo(AdmissionCheckInDependencyStatus.Unavailable);
        await targets.DidNotReceiveWithAnyArgs().GetAsync(default, default, default, default);
    }

    private static Task<AdmissionCheckInOperationalResult?> Execute(
        AdmissionCheckInOperationsService service,
        AdmissionCheckInOperationalAction action,
        AdmissionCheckInOperationalReasonCode reasonCode) =>
        service.ExecuteAsync(
            new AdmissionCheckInOperationalRequest(
                TenantId,
                EventId,
                TargetId,
                ActorId,
                action,
                reasonCode),
            CancellationToken.None);

    private static Task<AdmissionCheckInHealthResult?> Health(
        AdmissionCheckInOperationsService service) =>
        service.GetHealthAsync(
            new AdmissionCheckInHealthRequest(TenantId, EventId, TargetId, ActorId),
            CancellationToken.None);

    private static IAuthorizationProvider AllowedAuthorization()
    {
        IAuthorizationProvider authorization = Substitute.For<IAuthorizationProvider>();
        authorization.AuthorizeAsync(Arg.Any<AuthorizationRequest>(), Arg.Any<CancellationToken>())
            .Returns(AuthorizationDecision.Allow(AuthorizationProviderMetadata.Local));
        return authorization;
    }
}

internal sealed class AdmissionOperationsUnitOfWork : IUnitOfWork
{
    internal int TransactionCount { get; private set; }

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken ct = default)
    {
        TransactionCount++;
        return operation(ct);
    }

    public Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        TransactionCount++;
        return operation(ct);
    }

    public Task<T> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        TransactionCount++;
        return operation(ct);
    }
}

internal sealed class AdmissionOperationsTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
