// ABOUTME: Verifies event cancellation drains active admission orders in bounded replayable batches.
// ABOUTME: Fails closed when any per-order revocation cannot converge.

using Explore.Application.Contracts.Admissions;
using Explore.Application.Services.Registration;
using NSubstitute;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class AdmissionEventCancellationServiceTests
{
    [Test]
    public async Task ReconcileProcessesOneBoundedBatchAndPersistsContinuation()
    {
        Guid sourceMessageId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid[] orderIds = Enumerable.Range(0, 100)
            .Select(_ => Guid.CreateVersion7())
            .ToArray();
        IAdmissionEventCancellationRepository repository =
            Substitute.For<IAdmissionEventCancellationRepository>();
        repository.ListRevocableOrderIdsAsync(
                tenantId, eventId, 100, Arg.Any<CancellationToken>())
            .Returns(orderIds);
        IAdmissionRevocationService revocation =
            Substitute.For<IAdmissionRevocationService>();
        revocation.ReconcileAsync(
                Arg.Any<AdmissionRevocationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call => new AdmissionRevocationResult(
                AdmissionRevocationOutcome.Applied,
                [call.Arg<AdmissionRevocationRequest>().RegistrationOrderId],
                []));
        var service = new AdmissionEventCancellationService(
            repository, revocation, TimeProvider.System);

        int reconciled = await service.ReconcileAsync(
            sourceMessageId, tenantId, eventId, CancellationToken.None);

        await Assert.That(reconciled).IsEqualTo(100);
        await repository.Received(1).ListRevocableOrderIdsAsync(
            tenantId, eventId, 100, Arg.Any<CancellationToken>());
        await revocation.Received(100).ReconcileAsync(
            Arg.Is<AdmissionRevocationRequest>(request =>
                request.TenantId == tenantId &&
                request.Reason == AdmissionRevocationService.OrderCancellationReason &&
                request.RefundAllocations.Count == 0),
            Arg.Any<CancellationToken>());
        await repository.Received(1).ScheduleContinuationAsync(
            sourceMessageId,
            tenantId,
            eventId,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReconcileFailsClosedWhenAnOrderCannotConverge()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        IAdmissionEventCancellationRepository repository =
            Substitute.For<IAdmissionEventCancellationRepository>();
        repository.ListRevocableOrderIdsAsync(
                tenantId, eventId, 100, Arg.Any<CancellationToken>())
            .Returns([Guid.CreateVersion7()]);
        IAdmissionRevocationService revocation =
            Substitute.For<IAdmissionRevocationService>();
        revocation.ReconcileAsync(
                Arg.Any<AdmissionRevocationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new AdmissionRevocationResult(
                AdmissionRevocationOutcome.NotFound, [], []));
        var service = new AdmissionEventCancellationService(
            repository, revocation, TimeProvider.System);

        await Assert.That(async () => await service.ReconcileAsync(
                Guid.CreateVersion7(), tenantId, eventId, CancellationToken.None))
            .Throws<InvalidOperationException>();
    }
}
