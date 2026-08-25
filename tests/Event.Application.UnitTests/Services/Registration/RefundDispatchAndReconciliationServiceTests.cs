// ABOUTME: Specifies provider-neutral refund dispatch and reconciliation authority over durable attempts.
// ABOUTME: Proves pre-I/O persistence, pinned routing, stable idempotency, ambiguity, and truthful status mapping.

using Explore.Application.Contracts.Payments;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class RefundDispatchAndReconciliationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid AttemptId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000002");

    [Test]
    public async Task DispatchAsync_PersistsDispatchPendingBeforeProviderIoAndUsesPinnedAttemptAuthority()
    {
        RefundAttempt attempt = Attempt();
        var repository = Repository(attempt);
        var creator = Substitute.For<IRefundCreator>();
        RefundCreateRequest? captured = null;
        RefundAttemptStatusEnum statusAtProviderCall = default;
        creator.CreateAsync(Arg.Any<RefundCreateRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            captured = call.Arg<RefundCreateRequest>();
            statusAtProviderCall = attempt.Status;
            return RefundProviderResult.Observed(Observation(RefundProviderStatus.Pending), "req_create");
        });

        RefundAttempt? result = await new RefundDispatchService(
            repository, creator, new FixedTimeProvider(UtcNow)).DispatchAsync(TenantId, AttemptId, CancellationToken.None);

        await Assert.That(statusAtProviderCall).IsEqualTo(RefundAttemptStatusEnum.DispatchPending);
        await Assert.That(captured!.ExternalAccountId).IsEqualTo("acct_original");
        await Assert.That(captured.ProviderIdempotencyKey).IsEqualTo("refund:stable");
        await Assert.That(captured.ProviderPaymentId).IsEqualTo("pi_original");
        await Assert.That(captured.ApplicationFeeRefundAmountMinor).IsEqualTo(38);
        await Assert.That(result!.Status).IsEqualTo(RefundAttemptStatusEnum.Pending);
        Received.InOrder(() =>
        {
            repository.SaveChangesAsync(Arg.Any<CancellationToken>());
            creator.CreateAsync(Arg.Any<RefundCreateRequest>(), Arg.Any<CancellationToken>());
            repository.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task DispatchAsync_DefinitiveProviderBlockRequiresActionWithoutReleasingCapacity()
    {
        RefundAttempt attempt = Attempt();
        var repository = Repository(attempt);
        var creator = Substitute.For<IRefundCreator>();
        creator.CreateAsync(Arg.Any<RefundCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(RefundProviderResult.Failed(new RefundProviderFailure(
                "refund_provider_rejected",
                RefundProviderFailureKind.Configuration,
                "req_blocked",
                ProviderHandoffStarted: true)));

        RefundAttempt? result = await new RefundDispatchService(
            repository, creator, new FixedTimeProvider(UtcNow))
            .DispatchAsync(TenantId, AttemptId, CancellationToken.None);

        await Assert.That(result!.Status).IsEqualTo(RefundAttemptStatusEnum.RequiresAction);
        await Assert.That(result.FailureCode).IsEqualTo("refund_provider_rejected");
        await Assert.That(result.ReservesCapacity).IsTrue();
    }

    [Test]
    public async Task DispatchAsync_TimeoutBeforeHandoffRemainsRetryableWithTheSameIdempotencyKey()
    {
        RefundAttempt attempt = Attempt();
        var repository = Repository(attempt);
        var creator = Substitute.For<IRefundCreator>();
        var requests = new List<RefundCreateRequest>();
        creator.CreateAsync(Arg.Any<RefundCreateRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            requests.Add(call.Arg<RefundCreateRequest>());
            return RefundProviderResult.Unknown(new(
                "refund_provider_network_ambiguous", RefundProviderFailureKind.Network,
                ProviderHandoffStarted: false));
        });
        var service = new RefundDispatchService(repository, creator, new FixedTimeProvider(UtcNow));

        _ = await service.DispatchAsync(TenantId, AttemptId, CancellationToken.None);
        _ = await service.DispatchAsync(TenantId, AttemptId, CancellationToken.None);

        await Assert.That(attempt.Status).IsEqualTo(RefundAttemptStatusEnum.DispatchPending);
        await Assert.That(requests).Count().IsEqualTo(2);
        await Assert.That(requests.Select(request => request.ProviderIdempotencyKey).Distinct()).IsEquivalentTo(["refund:stable"]);
    }

    [Test]
    public async Task ReconcileAsync_TimeoutAfterHandoffRepeatsOnlyStableCreateAndAcceptsLateSuccess()
    {
        RefundAttempt attempt = Attempt();
        var repository = Repository(attempt);
        var creator = Substitute.For<IRefundCreator>();
        var requests = new List<RefundCreateRequest>();
        creator.CreateAsync(Arg.Any<RefundCreateRequest>(), Arg.Any<CancellationToken>()).Returns(
            call =>
            {
                requests.Add(call.Arg<RefundCreateRequest>());
                return RefundProviderResult.Unknown(new(
                    "refund_provider_network_ambiguous", RefundProviderFailureKind.Network,
                    ProviderHandoffStarted: true));
            },
            call =>
            {
                requests.Add(call.Arg<RefundCreateRequest>());
                return RefundProviderResult.Observed(Observation(RefundProviderStatus.Succeeded), "req_late");
            });
        var retriever = Substitute.For<IRefundRetriever>();

        _ = await new RefundDispatchService(repository, creator, new FixedTimeProvider(UtcNow))
            .DispatchAsync(TenantId, AttemptId, CancellationToken.None);
        RefundAttempt? result = await new RefundReconciliationService(
            repository, creator, retriever, new FixedTimeProvider(UtcNow.AddMinutes(1)))
            .ReconcileAsync(TenantId, AttemptId, CancellationToken.None);

        await Assert.That(requests).Count().IsEqualTo(2);
        await Assert.That(requests.Select(request => request.ProviderIdempotencyKey).Distinct()).IsEquivalentTo(["refund:stable"]);
        await Assert.That(result!.Status).IsEqualTo(RefundAttemptStatusEnum.Succeeded);
        await retriever.DidNotReceiveWithAnyArgs().RetrieveAsync(default!, default);
    }

    [Test]
    [Arguments(RefundProviderStatus.Pending, RefundAttemptStatusEnum.Pending)]
    [Arguments(RefundProviderStatus.RequiresAction, RefundAttemptStatusEnum.RequiresAction)]
    [Arguments(RefundProviderStatus.Failed, RefundAttemptStatusEnum.Failed)]
    [Arguments(RefundProviderStatus.Cancelled, RefundAttemptStatusEnum.Cancelled)]
    public async Task DispatchAsync_MapsProviderEvidenceWithoutManufacturingSuccess(
        RefundProviderStatus providerStatus,
        RefundAttemptStatusEnum expectedStatus)
    {
        RefundAttempt attempt = Attempt();
        var repository = Repository(attempt);
        var creator = Substitute.For<IRefundCreator>();
        creator.CreateAsync(Arg.Any<RefundCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(RefundProviderResult.Observed(Observation(providerStatus), "req_status"));

        RefundAttempt? result = await new RefundDispatchService(
            repository, creator, new FixedTimeProvider(UtcNow)).DispatchAsync(TenantId, AttemptId, CancellationToken.None);

        await Assert.That(result!.Status).IsEqualTo(expectedStatus);
        await Assert.That(result.Status == RefundAttemptStatusEnum.Succeeded).IsFalse();
    }

    [Test]
    public async Task ReconcileAsync_KnownProviderRefundRetrievesOnOriginalAccountWithoutMutationIdempotency()
    {
        RefundAttempt attempt = Attempt();
        attempt.MarkPending("re_123", UtcNow.AddMinutes(-1), "req_create");
        var repository = Repository(attempt);
        var creator = Substitute.For<IRefundCreator>();
        var retriever = Substitute.For<IRefundRetriever>();
        retriever.RetrieveAsync(Arg.Any<RefundRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(RefundProviderResult.Observed(Observation(RefundProviderStatus.Succeeded), "req_get"));

        RefundAttempt? result = await new RefundReconciliationService(
            repository, creator, retriever, new FixedTimeProvider(UtcNow))
            .ReconcileAsync(TenantId, AttemptId, CancellationToken.None);

        await retriever.Received(1).RetrieveAsync(
            Arg.Is<RefundRetrieveRequest>(request =>
                request.ExternalAccountId == "acct_original" && request.ProviderRefundId == "re_123"),
            Arg.Any<CancellationToken>());
        await creator.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await Assert.That(result!.Status).IsEqualTo(RefundAttemptStatusEnum.Succeeded);
    }

    [Test]
    public async Task DispatchAsync_SuccessWithoutProviderRefundIdentityRemainsUnknown()
    {
        RefundAttempt attempt = Attempt();
        var repository = Repository(attempt);
        var creator = Substitute.For<IRefundCreator>();
        creator.CreateAsync(Arg.Any<RefundCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(RefundProviderResult.Observed(
                new RefundProviderObservation(string.Empty, "pi_original", RefundProviderStatus.Succeeded, 500, "EUR", 38),
                "req_incomplete"));

        RefundAttempt? result = await new RefundDispatchService(
            repository, creator, new FixedTimeProvider(UtcNow)).DispatchAsync(TenantId, AttemptId, CancellationToken.None);

        await Assert.That(result!.Status).IsEqualTo(RefundAttemptStatusEnum.Unknown);
        await Assert.That(result.SucceededAt).IsNull();
    }

    [Test]
    public async Task DispatchAsync_BuyerSuccessWithDefinitiveFeeFailurePreservesBuyerTruthAndRequiresOperator()
    {
        RefundAttempt attempt = Attempt();
        var repository = Repository(attempt);
        var creator = Substitute.For<IRefundCreator>();
        creator.CreateAsync(Arg.Any<RefundCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(RefundProviderResult.Observed(
                new RefundProviderObservation(
                    "re_123", "pi_original", RefundProviderStatus.Succeeded, 500, "EUR", null,
                    "refund_provider_fee_rejected"),
                "req_fee"));

        RefundAttempt? result = await new RefundDispatchService(
            repository, creator, new FixedTimeProvider(UtcNow)).DispatchAsync(
            TenantId, AttemptId, CancellationToken.None);

        await Assert.That(result!.BuyerRefundSucceededAt).IsEqualTo(UtcNow);
        await Assert.That(result.Status).IsEqualTo(RefundAttemptStatusEnum.RequiresAction);
        await Assert.That(result.FailureCode).IsEqualTo("refund_provider_fee_rejected");
        await Assert.That(result.ApplicationFeeRefundedAmountMinor).IsEqualTo(0);
        await Assert.That(result.ReservesCapacity).IsTrue();
    }

    [Test]
    public async Task ReconcileAsync_ProviderBlockedAttemptDoesNotMutateProviderWithoutExplicitRetry()
    {
        RefundAttempt attempt = Attempt();
        attempt.MarkDispatchPending(UtcNow.AddMinutes(-1), null);
        attempt.MarkProviderBlocked(UtcNow.AddSeconds(-1), "req_blocked", "refund_provider_rejected");
        var repository = Repository(attempt);
        var creator = Substitute.For<IRefundCreator>();
        var retriever = Substitute.For<IRefundRetriever>();

        RefundAttempt? result = await new RefundReconciliationService(
            repository, creator, retriever, new FixedTimeProvider(UtcNow))
            .ReconcileAsync(TenantId, AttemptId, CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(attempt);
        await creator.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await retriever.DidNotReceiveWithAnyArgs().RetrieveAsync(default!, default);
    }

    private static IRefundAttemptRepository Repository(RefundAttempt attempt)
    {
        var repository = Substitute.For<IRefundAttemptRepository>();
        repository.GetByIdAsync(TenantId, AttemptId, Arg.Any<CancellationToken>()).Returns(attempt);
        return repository;
    }

    private static RefundProviderObservation Observation(RefundProviderStatus status) =>
        new("re_123", "pi_original", status, 500, "EUR", status == RefundProviderStatus.Succeeded ? 38 : null);

    private static RefundAttempt Attempt() => RefundAttempt.Create(
        AttemptId, TenantId, Guid.CreateVersion7(), Acceptance(), "acct_original",
        "pi_original", "refund:stable", 500, UtcNow.AddMinutes(-2));

    private static PaidOrderAcceptanceSnapshot Acceptance() => RefundTestAcceptance.Create(
        TenantId, Guid.CreateVersion7(), organizerAmountMinor: 1_000, platformFeeMinor: 75,
        platformContributionMinor: 0, acceptedAt: UtcNow.AddMinutes(-3));

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
