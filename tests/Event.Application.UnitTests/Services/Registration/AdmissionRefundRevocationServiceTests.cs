// ABOUTME: Verifies cumulative buyer-refund success becomes exact provider-neutral admission facts.
// ABOUTME: Proves non-success observations cannot trigger ticket revocation.

using Explore.Application.Contracts.Admissions;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services.Registration;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class AdmissionRefundRevocationServiceTests
{
    private static readonly DateTime UtcNow =
        new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task CumulativeBuyerRefundSuccessBuildsOneFullAcceptedLineFact()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid orderId = Guid.CreateVersion7();
        Guid paymentAttemptId = Guid.CreateVersion7();
        PaidOrderAcceptanceSnapshot acceptance = RefundTestAcceptance.Create(
            tenantId, orderId, 1_000, 0, 0, UtcNow.AddHours(-1));
        RefundAttempt first = Attempt(
            tenantId, paymentAttemptId, acceptance, 500, UtcNow.AddMinutes(-10));
        RefundAttempt second = Attempt(
            tenantId, paymentAttemptId, acceptance, 500, UtcNow.AddMinutes(-5));
        first.MarkBuyerRefundSucceeded("re_first", UtcNow.AddMinutes(-4), null);
        second.MarkBuyerRefundSucceeded("re_second", UtcNow.AddMinutes(-3), null);

        IRefundAttemptRepository refunds = Substitute.For<IRefundAttemptRepository>();
        refunds.GetByIdAsync(tenantId, second.Id, Arg.Any<CancellationToken>())
            .Returns(second);
        refunds.GetAcceptanceAsync(tenantId, acceptance.Id, Arg.Any<CancellationToken>())
            .Returns(acceptance);
        refunds.GetByPaymentAsync(tenantId, paymentAttemptId, Arg.Any<CancellationToken>())
            .Returns([first, second]);
        IAdmissionRevocationService revocation = Substitute.For<IAdmissionRevocationService>();
        revocation.ReconcileAsync(
                Arg.Any<AdmissionRevocationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new AdmissionRevocationResult(
                AdmissionRevocationOutcome.Applied, [], []));
        var service = new AdmissionRefundRevocationService(refunds, revocation);

        AdmissionRevocationResult? result = await service.ReconcileSucceededAsync(
            tenantId, second.Id, CancellationToken.None);

        await Assert.That(result?.Outcome).IsEqualTo(AdmissionRevocationOutcome.Applied);
        await revocation.Received(1).ReconcileAsync(
            Arg.Is<AdmissionRevocationRequest>(request =>
                request.TenantId == tenantId &&
                request.RegistrationOrderId == orderId &&
                request.Reason == AdmissionRevocationService.RefundReconciledReason &&
                request.RefundAllocations.Count == 1 &&
                request.RefundAllocations[0].OrderLineId ==
                    acceptance.Lines.Single().OrderLineId &&
                request.RefundAllocations[0].RefundedMinor == 1_000 &&
                request.RefundAllocations[0].RelevantLineTotalMinor == 1_000),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AttemptWithoutBuyerSuccessDoesNotRequestAdmissionRevocation()
    {
        Guid tenantId = Guid.CreateVersion7();
        PaidOrderAcceptanceSnapshot acceptance = RefundTestAcceptance.Create(
            tenantId, Guid.CreateVersion7(), 1_000, 0, 0, UtcNow.AddHours(-1));
        RefundAttempt attempt = Attempt(
            tenantId, Guid.CreateVersion7(), acceptance, 500, UtcNow.AddMinutes(-5));
        IRefundAttemptRepository refunds = Substitute.For<IRefundAttemptRepository>();
        refunds.GetByIdAsync(tenantId, attempt.Id, Arg.Any<CancellationToken>())
            .Returns(attempt);
        IAdmissionRevocationService revocation = Substitute.For<IAdmissionRevocationService>();
        var service = new AdmissionRefundRevocationService(refunds, revocation);

        AdmissionRevocationResult? result = await service.ReconcileSucceededAsync(
            tenantId, attempt.Id, CancellationToken.None);

        await Assert.That(result).IsNull();
        await revocation.DidNotReceive().ReconcileAsync(
            Arg.Any<AdmissionRevocationRequest>(),
            Arg.Any<CancellationToken>());
    }

    private static RefundAttempt Attempt(
        Guid tenantId,
        Guid paymentAttemptId,
        PaidOrderAcceptanceSnapshot acceptance,
        long requestedMinor,
        DateTime createdAt) =>
        RefundAttempt.Create(
            Guid.CreateVersion7(),
            tenantId,
            paymentAttemptId,
            acceptance,
            "acct_example",
            "pi_example",
            $"refund:{Guid.CreateVersion7():N}",
            requestedMinor,
            createdAt);
}
