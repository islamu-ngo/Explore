// ABOUTME: Verifies one-pass durable Checkout dispatch orchestration and fenced outcome settlement.
// ABOUTME: Ensures provider calls stay outside claims and ambiguous creates park for reconciliation.

using Explore.Application.Contracts.Payments;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Services.Registration;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class RegistrationPaymentCheckoutDispatchServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid OrderId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000002");
    private static readonly Guid AttemptId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000003");

    [Test]
    public async Task DispatchDueAsync_CreatesOnceFromImmutableAttemptAndCompletesCurrentFence()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out PaymentAttempt attempt);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Succeeded(Session(), "req_create"));
        repository.CompleteCheckoutDispatchAsync(claim, "cs_123", "req_create", UtcNow.AddTicks(1), Arg.Any<CancellationToken>())
            .Returns(true);
        var service = Service(repository, creator);

        RegistrationPaymentCheckoutDispatchResult result = await service.DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Completed).IsEqualTo(1);
        await creator.Received(1).CreateAsync(
            Arg.Is<HostedCheckoutCreateRequest>(request =>
                request.PaymentAttemptId == AttemptId &&
                request.RegistrationOrderId == OrderId &&
                request.ExternalAccountId == "acct_123" &&
                request.ProviderIdempotencyKey == "checkout:stable" &&
                request.CurrencyCode == "EUR" &&
                request.TotalMinor == 12_50 &&
                request.ApplicationFeeMinor == 4_50 &&
                request.ExpiresAt == attempt.ExpiresAt),
            Arg.Any<CancellationToken>());
        await repository.Received(1).CompleteCheckoutDispatchAsync(
            claim,
            "cs_123",
            "req_create",
            UtcNow.AddTicks(1),
            Arg.Any<CancellationToken>());
        await repository.Received(1).GetClaimedAttemptAsync(claim, UtcNow, Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            _ = repository.PrepareCheckoutDispatchAsync(claim, UtcNow, UtcNow.AddMinutes(31), Arg.Any<CancellationToken>());
            _ = creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>());
            _ = repository.CompleteCheckoutDispatchAsync(claim, "cs_123", "req_create", UtcNow.AddTicks(1), Arg.Any<CancellationToken>());
        });
        _ = attempt;
    }

    [Test]
    public async Task DispatchDueAsync_RechecksStoppedSaleBeforeProviderButDoesNotBlockAlreadyHandedOffSettlement()
    {
        var stoppedRepository = RepositoryWithClaim(out CheckoutDispatchClaim stoppedClaim, out _);
        var stoppedCreator = Substitute.For<IHostedCheckoutSessionCreator>();
        var stoppedActivation = Substitute.For<IPaidCheckoutActivationService>();
        stoppedActivation.EvaluateAsync(Arg.Any<PaidCheckoutActivationRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaidCheckoutActivationResult.Failure("paid_sale_stopped", "stopped"));
        stoppedRepository.DeferCheckoutDispatchForConfigurationAsync(
                stoppedClaim, "paid_sale_stopped", Arg.Any<DateTime>(), UtcNow, Arg.Any<CancellationToken>())
            .Returns(CheckoutDispatchConfigurationDisposition.Deferred);

        RegistrationPaymentCheckoutDispatchResult stopped = await Service(
            stoppedRepository, stoppedCreator, activation: stoppedActivation).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(stopped.Retried).IsEqualTo(1);
        await stoppedCreator.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);

        var handedOffRepository = RepositoryWithClaim(out CheckoutDispatchClaim handedOffClaim, out PaymentAttempt handedOffAttempt);
        handedOffAttempt.MarkDispatchPending(UtcNow.AddSeconds(-1), null);
        handedOffAttempt.MarkRequiresAction("cs_existing", UtcNow, null);
        handedOffRepository.CompleteCheckoutDispatchAsync(
            handedOffClaim, "cs_existing", Arg.Any<string?>(), UtcNow, Arg.Any<CancellationToken>()).Returns(true);
        RegistrationPaymentCheckoutDispatchResult handedOff = await Service(
            handedOffRepository, stoppedCreator, activation: stoppedActivation).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(handedOff.Completed).IsEqualTo(1);
        await stoppedActivation.Received(1).EvaluateAsync(Arg.Any<PaidCheckoutActivationRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchDueAsync_StaleAcceptanceDefersBeforePreparingOrCallingProvider()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        var freshness = Substitute.For<IPaidOrderAcceptanceFreshnessService>();
        freshness.IsCurrentAsync(Arg.Any<PaymentAttempt>(), Arg.Any<CancellationToken>()).Returns(false);
        repository.DeferCheckoutDispatchForConfigurationAsync(
                claim, "payment_acceptance_stale", Arg.Any<DateTime>(), UtcNow, Arg.Any<CancellationToken>())
            .Returns(CheckoutDispatchConfigurationDisposition.Deferred);

        RegistrationPaymentCheckoutDispatchResult result = await Service(
            repository, creator, freshness: freshness).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Retried).IsEqualTo(1);
        await repository.DidNotReceiveWithAnyArgs().PrepareCheckoutDispatchAsync(default!, default, default, default);
        await creator.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task DispatchDueAsync_DelayedQueuePersistsSharedMarginCutoffImmediatelyBeforeHandoff()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out PaymentAttempt attempt);
        DateTime renewedCutoff = UtcNow.AddMinutes(31);
        typeof(PaymentAttempt).GetProperty(nameof(PaymentAttempt.ExpiresAt))!.SetValue(attempt, renewedCutoff);
        repository.PrepareCheckoutDispatchAsync(claim, UtcNow, renewedCutoff, Arg.Any<CancellationToken>()).Returns(attempt);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Succeeded(Session() with { ExpiresAt = renewedCutoff }, "req_delayed"));
        repository.CompleteCheckoutDispatchAsync(claim, "cs_123", "req_delayed", UtcNow.AddTicks(1), CancellationToken.None).Returns(true);

        _ = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await repository.Received(1).PrepareCheckoutDispatchAsync(claim, UtcNow, renewedCutoff, Arg.Any<CancellationToken>());
        await creator.Received(1).CreateAsync(
            Arg.Is<HostedCheckoutCreateRequest>(request => request.ExpiresAt == renewedCutoff),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchDueAsync_FiftyDueEffectsUseFiftyFreshOneItemClaims()
    {
        var repository = Substitute.For<IRegistrationPaymentAttemptRepository>();
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        var claims = Enumerable.Range(0, 50).Select(index => new CheckoutDispatchClaim(
            Guid.CreateVersion7(), TenantId, OrderId, Guid.CreateVersion7(), Guid.CreateVersion7(), index + 1, AttemptCount: 1)).ToArray();
        int cursor = 0;
        repository.ClaimDueDispatchEffectsAsync("checkout-test", 1, Arg.Any<DateTime>(), TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns(_ => cursor < claims.Length ? new[] { claims[cursor++] } : []);
        foreach (CheckoutDispatchClaim claim in claims)
        {
            PaymentAttempt attempt = Attempt(Guid.CreateVersion7());
            repository.GetClaimedAttemptAsync(claim, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(attempt);
            repository.PrepareCheckoutDispatchAsync(claim, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(attempt);
            repository.CompleteCheckoutDispatchAsync(claim, Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<DateTime>(), CancellationToken.None).Returns(true);
        }
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => HostedCheckoutCreateResult.Succeeded(Session() with { SessionId = $"cs_{call.Arg<HostedCheckoutCreateRequest>().PaymentAttemptId:N}" }, "req_50"));

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request() with { BatchSize = 50 }, CancellationToken.None);

        await Assert.That(result.Claimed).IsEqualTo(50);
        await creator.Received(50).CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>());
        await repository.Received(50).ClaimDueDispatchEffectsAsync("checkout-test", 1, Arg.Any<DateTime>(), TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeferDueForConfigurationAsync_ExpiredAttemptRoutesToApplicationLifecycle()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        repository.DeferCheckoutDispatchForConfigurationAsync(
                claim,
                "checkout_provider_secret_unavailable",
                UtcNow.AddMinutes(5),
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(CheckoutDispatchConfigurationDisposition.RequiresLifecycleCancellation);
        var lifecycle = Substitute.For<IRegistrationOrderLifecycleService>();
        lifecycle.CancelExpiredConfigurationBlockedPaymentAsync(claim, UtcNow, Arg.Any<CancellationToken>())
            .Returns(CheckoutDispatchConfigurationDisposition.CancelledExpired);

        RegistrationPaymentCheckoutDispatchResult result = await Service(
            repository,
            Substitute.For<IHostedCheckoutSessionCreator>(),
            orderLifecycle: lifecycle).DeferDueForConfigurationAsync(
            "checkout-test",
            1,
            TimeSpan.FromMinutes(2),
            "checkout_provider_secret_unavailable",
            CancellationToken.None);

        await Assert.That(result.Retried).IsEqualTo(1);
        await lifecycle.Received(1).CancelExpiredConfigurationBlockedPaymentAsync(
            claim,
            UtcNow,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchDueAsync_AmbiguousCreateMarksUnknownAndDoesNotRetryOrParkAsFailed()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Unknown(new HostedCheckoutFailure(
                "checkout_provider_network_ambiguous",
                HostedCheckoutFailureKind.Network,
                ProviderRequestId: "req_unknown")));
        repository.MarkCheckoutDispatchUnknownAsync(claim, "req_unknown", UtcNow.AddTicks(1), Arg.Any<CancellationToken>())
            .Returns(true);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator)
            .DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Unknown).IsEqualTo(1);
        await creator.Received(1).CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>());
        await repository.DidNotReceiveWithAnyArgs().RetryDispatchAsync(default!, default, default, default);
        await repository.DidNotReceiveWithAnyArgs().FailCheckoutDispatchAsync(default!, default!, default, default, default);
    }

    [Test]
    public async Task DispatchDueAsync_DeterministicRejectionParksBoundedFailure()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Failed(new HostedCheckoutFailure(
                "checkout_provider_rejected",
                HostedCheckoutFailureKind.ProviderRejected,
                "invalid_request_error",
                "currency_not_supported",
                400,
                "req_bad")));
        repository.FailCheckoutDispatchAsync(claim, "checkout_provider_rejected", "req_bad", UtcNow.AddTicks(1), Arg.Any<CancellationToken>())
            .Returns(true);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Parked).IsEqualTo(1);
        await repository.Received(1).FailCheckoutDispatchAsync(
            claim,
            "checkout_provider_rejected",
            "req_bad",
            UtcNow.AddTicks(1),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchDueAsync_CancelledAttemptParksBeforeProviderHandoff()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out PaymentAttempt attempt);
        attempt.MarkCancelled(UtcNow.AddSeconds(-1), null);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        repository.FailCheckoutDispatchAsync(claim, "checkout_attempt_cancelled", null, UtcNow, Arg.Any<CancellationToken>())
            .Returns(true);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Parked).IsEqualTo(1);
        await creator.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task DispatchDueAsync_AlreadyBoundSessionCompletesWithoutDuplicateCreate()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out PaymentAttempt attempt);
        attempt.MarkRequiresAction("cs_existing", UtcNow.AddSeconds(-1), "req_existing");
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        repository.CompleteCheckoutDispatchAsync(claim, "cs_existing", "req_existing", UtcNow, Arg.Any<CancellationToken>())
            .Returns(true);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Completed).IsEqualTo(1);
        await creator.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Test]
    public async Task DispatchDueAsync_RecoveredDispatchPendingAttemptParksUnknownWithoutSecondCreate()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out PaymentAttempt attempt);
        attempt.MarkDispatchPending(UtcNow.AddSeconds(-1), null);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        repository.MarkCheckoutDispatchUnknownAsync(claim, null, UtcNow, CancellationToken.None).Returns(true);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Unknown).IsEqualTo(1);
        await creator.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await repository.Received(1).MarkCheckoutDispatchUnknownAsync(claim, null, UtcNow, CancellationToken.None);
    }

    [Test]
    public async Task DispatchDueAsync_StaleFenceDoesNotReportSettled()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Succeeded(Session(), "req_create"));
        repository.CompleteCheckoutDispatchAsync(claim, "cs_123", "req_create", UtcNow.AddTicks(1), Arg.Any<CancellationToken>())
            .Returns(false);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Stale).IsEqualTo(1);
        await Assert.That(result.Completed).IsEqualTo(0);
    }

    [Test]
    public async Task DispatchDueAsync_ProviderCompleteStatusStillSettlesCreationWithoutPaymentFinalization()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        HostedCheckoutSession misleading = Session() with
        {
            Status = HostedCheckoutSessionStatus.Complete,
            PaymentStatus = HostedCheckoutPaymentStatus.Paid,
            PaymentId = "pi_unverified"
        };
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Succeeded(misleading, "req_create"));
        repository.CompleteCheckoutDispatchAsync(claim, "cs_123", "req_create", UtcNow.AddTicks(1), CancellationToken.None)
            .Returns(true);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Completed).IsEqualTo(1);
        await repository.Received(1).CompleteCheckoutDispatchAsync(claim, "cs_123", "req_create", UtcNow.AddTicks(1), CancellationToken.None);
        await repository.DidNotReceiveWithAnyArgs().FailCheckoutDispatchAsync(default!, default!, default, default, default);
    }

    [Test]
    public async Task DispatchDueAsync_PreCancelledTokenPropagatesBeforeClaim()
    {
        var repository = Substitute.For<IRegistrationPaymentAttemptRepository>();
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.That(async () => await Service(repository, creator).DispatchDueAsync(Request(), source.Token))
            .Throws<OperationCanceledException>();
        await repository.DidNotReceiveWithAnyArgs().ClaimDueDispatchEffectsAsync(default!, default, default, default, default);
    }

    [Test]
    public async Task DispatchDueAsync_CancellationDuringHandoffStillDurablyMarksUnknown()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        using var source = new CancellationTokenSource();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), source.Token)
            .Returns(_ =>
            {
                source.Cancel();
                return HostedCheckoutCreateResult.Unknown(new HostedCheckoutFailure(
                    "checkout_provider_network_ambiguous",
                    HostedCheckoutFailureKind.Network));
            });
        repository.MarkCheckoutDispatchUnknownAsync(claim, null, UtcNow.AddTicks(1), CancellationToken.None).Returns(true);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), source.Token);

        await Assert.That(result.Unknown).IsEqualTo(1);
        await repository.Received(1).MarkCheckoutDispatchUnknownAsync(claim, null, UtcNow.AddTicks(1), CancellationToken.None);
    }

    [Test]
    public async Task DispatchDueAsync_ExplicitUnknownRedriveWithoutSessionReplaysSameAttemptAndIdempotencyKey()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim original, out PaymentAttempt attempt);
        attempt.MarkUnknown(UtcNow.AddSeconds(-1), "req_unknown");
        CheckoutDispatchClaim claim = original;
        int claimCount = 0;
        repository.ClaimDueDispatchEffectsAsync("checkout-test", 1, UtcNow, TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns(_ => claimCount++ == 0 ? [claim] : []);
        repository.GetClaimedAttemptAsync(claim, UtcNow, Arg.Any<CancellationToken>()).Returns(attempt);
        repository.PrepareCheckoutDispatchAsync(claim, UtcNow, UtcNow.AddMinutes(31), Arg.Any<CancellationToken>()).Returns(attempt);
        repository.CompleteCheckoutDispatchAsync(claim, "cs_redrive", "req_redrive", UtcNow.AddTicks(1), CancellationToken.None).Returns(true);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Succeeded(Session() with { SessionId = "cs_redrive" }, "req_redrive"));

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Completed).IsEqualTo(1);
        await creator.Received(1).CreateAsync(
            Arg.Is<HostedCheckoutCreateRequest>(request => request.PaymentAttemptId == AttemptId && request.ProviderIdempotencyKey == "checkout:stable"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchDueAsync_ExplicitUnknownRedriveWithSessionRetrievesBeforeProgress()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim original, out PaymentAttempt attempt);
        attempt.MarkRequiresAction("cs_existing", UtcNow.AddSeconds(-2), "req_create");
        attempt.MarkUnknown(UtcNow.AddSeconds(-1), "req_unknown");
        CheckoutDispatchClaim claim = original;
        int claimCount = 0;
        repository.ClaimDueDispatchEffectsAsync("checkout-test", 1, UtcNow, TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns(_ => claimCount++ == 0 ? [claim] : []);
        repository.GetClaimedAttemptAsync(claim, UtcNow, Arg.Any<CancellationToken>()).Returns(attempt);
        repository.CompleteCheckoutDispatchAsync(claim, "cs_existing", "req_pi", UtcNow.AddTicks(1), CancellationToken.None).Returns(true);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        var retriever = Substitute.For<IHostedCheckoutSessionRetriever>();
        retriever.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(Session() with { SessionId = "cs_existing", PaymentId = "pi_existing" }, "req_retrieve"));
        var paymentIntents = Substitute.For<IPaymentIntentRetriever>();
        paymentIntents.RetrievePaymentIntentAsync(Arg.Any<PaymentIntentRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentIntentRetrieveResult.Succeeded(new(
                "pi_existing", 12_50, "EUR", 4_50, PaymentIntentStatus.Succeeded), "req_pi"));

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator, retriever, paymentIntents).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Completed).IsEqualTo(1);
        await retriever.Received(1).RetrieveAsync(
            Arg.Is<HostedCheckoutRetrieveRequest>(request => request.ProviderCheckoutSessionId == "cs_existing" && request.ExternalAccountId == "acct_123"),
            Arg.Any<CancellationToken>());
        await creator.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await paymentIntents.Received(1).RetrievePaymentIntentAsync(
            Arg.Is<PaymentIntentRetrieveRequest>(request => request.PaymentIntentId == "pi_existing"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchDueAsync_WhenProviderReadsOutliveLeaseUsesPostIoTimeAndRejectsStaleSettlement()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out PaymentAttempt attempt);
        attempt.MarkRequiresAction("cs_existing", UtcNow.AddSeconds(-2), "req_create");
        attempt.MarkUnknown(UtcNow.AddSeconds(-1), "req_unknown");
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        var retriever = Substitute.For<IHostedCheckoutSessionRetriever>();
        retriever.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(
                Session() with { SessionId = "cs_existing", PaymentId = "pi_existing" },
                "req_retrieve"));
        var clock = new MutableTimeProvider(UtcNow);
        DateTime afterLease = UtcNow.AddMinutes(3);
        var paymentIntents = Substitute.For<IPaymentIntentRetriever>();
        paymentIntents.RetrievePaymentIntentAsync(Arg.Any<PaymentIntentRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                clock.Set(afterLease);
                return PaymentIntentRetrieveResult.Succeeded(
                    new("pi_existing", 12_50, "EUR", 4_50, PaymentIntentStatus.Succeeded),
                    "req_pi");
            });
        repository.CompleteCheckoutDispatchAsync(
            claim,
            "cs_existing",
            "req_pi",
            afterLease,
            CancellationToken.None).Returns(false);

        RegistrationPaymentCheckoutDispatchResult result = await Service(
            repository,
            creator,
            retriever,
            paymentIntents,
            timeProvider: clock).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Completed).IsEqualTo(0);
        await Assert.That(result.Stale).IsEqualTo(1);
        await repository.Received(1).CompleteCheckoutDispatchAsync(
            claim,
            "cs_existing",
            "req_pi",
            afterLease,
            CancellationToken.None);
    }

    [Test]
    public async Task DispatchDueAsync_PreHandoffConfigurationFailureRetriesExistingEffectWithoutTerminalRelease()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Failed(new HostedCheckoutFailure(
                "checkout_provider_secret_unavailable",
                HostedCheckoutFailureKind.Configuration,
                ProviderHandoffStarted: false,
                PreHandoffDisposition: HostedCheckoutPreHandoffDisposition.Transient)));
        repository.DeferCheckoutDispatchForConfigurationAsync(
            claim,
            "checkout_provider_secret_unavailable",
            UtcNow.AddTicks(1).AddSeconds(5),
            UtcNow.AddTicks(1),
            CancellationToken.None).Returns(CheckoutDispatchConfigurationDisposition.Deferred);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Retried).IsEqualTo(1);
        await repository.Received(1).DeferCheckoutDispatchForConfigurationAsync(
            claim,
            "checkout_provider_secret_unavailable",
            UtcNow.AddTicks(1).AddSeconds(5),
            UtcNow.AddTicks(1),
            CancellationToken.None);
        await repository.DidNotReceiveWithAnyArgs().FailCheckoutDispatchAsync(default!, default!, default, default, default);
    }

    [Test]
    public async Task DispatchDueAsync_PermanentPreHandoffFailureParksExistingEffectWithoutTerminalAttemptFailure()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Failed(new HostedCheckoutFailure(
                "checkout_provider_unsupported",
                HostedCheckoutFailureKind.Configuration,
                ProviderHandoffStarted: false,
                PreHandoffDisposition: HostedCheckoutPreHandoffDisposition.Permanent)));
        repository.DeferCheckoutDispatchForConfigurationAsync(
            claim, "checkout_provider_unsupported", UtcNow.AddTicks(1).AddMinutes(15), UtcNow.AddTicks(1), CancellationToken.None)
            .Returns(CheckoutDispatchConfigurationDisposition.Deferred);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Retried).IsEqualTo(1);
        await repository.Received(1).DeferCheckoutDispatchForConfigurationAsync(
            claim, "checkout_provider_unsupported", UtcNow.AddTicks(1).AddMinutes(15), UtcNow.AddTicks(1), CancellationToken.None);
        await repository.DidNotReceiveWithAnyArgs().FailCheckoutDispatchAsync(default!, default!, default, default, default);
    }

    [Test]
    public async Task DispatchDueAsync_TransientPreHandoffFailureAtAttemptCeilingParksWithoutFurtherRetry()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim original, out PaymentAttempt attempt);
        CheckoutDispatchClaim claim = original with { AttemptCount = RegistrationPaymentCheckoutDispatchService.MaxPreHandoffAttempts };
        int claimCount = 0;
        repository.ClaimDueDispatchEffectsAsync("checkout-test", 1, UtcNow, TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns(_ => claimCount++ == 0 ? [claim] : []);
        repository.GetClaimedAttemptAsync(claim, UtcNow, Arg.Any<CancellationToken>()).Returns(attempt);
        repository.PrepareCheckoutDispatchAsync(claim, UtcNow, UtcNow.AddMinutes(31), Arg.Any<CancellationToken>()).Returns(attempt);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Failed(new HostedCheckoutFailure(
                "checkout_provider_secret_unavailable",
                HostedCheckoutFailureKind.Configuration,
                ProviderHandoffStarted: false,
                PreHandoffDisposition: HostedCheckoutPreHandoffDisposition.Transient)));
        repository.DeferCheckoutDispatchForConfigurationAsync(
            claim, "checkout_provider_secret_unavailable", UtcNow.AddTicks(1).AddMinutes(15), UtcNow.AddTicks(1), CancellationToken.None)
            .Returns(CheckoutDispatchConfigurationDisposition.Deferred);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Retried).IsEqualTo(1);
    }

    [Test]
    public async Task DeferDueForConfigurationAsync_StopsExactlyAtBatchSize()
    {
        var repository = Substitute.For<IRegistrationPaymentAttemptRepository>();
        CheckoutDispatchClaim[] claims = Enumerable.Range(0, 3).Select(index => new CheckoutDispatchClaim(
            Guid.CreateVersion7(), TenantId, OrderId, AttemptId, Guid.CreateVersion7(), index + 1, AttemptCount: 1)).ToArray();
        int cursor = 0;
        repository.ClaimDueDispatchEffectsAsync("checkout-test", 1, UtcNow, TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns(_ => cursor < claims.Length ? [claims[cursor++]] : []);
        repository.DeferCheckoutDispatchForConfigurationAsync(
                Arg.Any<CheckoutDispatchClaim>(), "configuration_unavailable", UtcNow.AddMinutes(5), UtcNow, Arg.Any<CancellationToken>())
            .Returns(CheckoutDispatchConfigurationDisposition.Deferred);

        RegistrationPaymentCheckoutDispatchResult result = await Service(
            repository, Substitute.For<IHostedCheckoutSessionCreator>()).DeferDueForConfigurationAsync(
            "checkout-test", 2, TimeSpan.FromMinutes(2), "configuration_unavailable", CancellationToken.None);

        await Assert.That(result.Claimed).IsEqualTo(2);
        await Assert.That(result.Retried).IsEqualTo(2);
        await repository.Received(2).ClaimDueDispatchEffectsAsync(
            "checkout-test", 1, UtcNow, TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>());
        await repository.Received(2).DeferCheckoutDispatchForConfigurationAsync(
            Arg.Any<CheckoutDispatchClaim>(), "configuration_unavailable", UtcNow.AddMinutes(5), UtcNow, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeferDueForConfigurationAsync_StopsWhenNoClaimIsAvailable()
    {
        var repository = Substitute.For<IRegistrationPaymentAttemptRepository>();
        repository.ClaimDueDispatchEffectsAsync(
                "checkout-test", 1, UtcNow, TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns([]);

        RegistrationPaymentCheckoutDispatchResult result = await Service(
            repository, Substitute.For<IHostedCheckoutSessionCreator>()).DeferDueForConfigurationAsync(
            "checkout-test", 1, TimeSpan.FromMinutes(2), "configuration_unavailable", CancellationToken.None);

        await Assert.That(result.Claimed).IsEqualTo(0);
        await repository.DidNotReceiveWithAnyArgs().DeferCheckoutDispatchForConfigurationAsync(default!, default!, default, default, default);
    }

    [Test]
    public async Task DispatchDueAsync_RejectsZeroBatchSizeBeforeRepositoryAccess()
    {
        var repository = Substitute.For<IRegistrationPaymentAttemptRepository>();

        await Assert.That(async () => await Service(
                repository, Substitute.For<IHostedCheckoutSessionCreator>()).DispatchDueAsync(
                Request() with { BatchSize = 0 }, CancellationToken.None))
            .Throws<ArgumentException>();
        await repository.DidNotReceiveWithAnyArgs().ClaimDueDispatchEffectsAsync(default!, default, default, default, default);
    }

    [Test]
    public async Task DispatchDueAsync_AcceptsExactRequestBoundaries()
    {
        var repository = Substitute.For<IRegistrationPaymentAttemptRepository>();
        string leaseOwner = new('x', CheckoutDispatchEffect.MaxLeaseOwnerLength);
        repository.ClaimDueDispatchEffectsAsync(
                leaseOwner, 1, UtcNow, TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns([]);

        RegistrationPaymentCheckoutDispatchResult result = await Service(
            repository, Substitute.For<IHostedCheckoutSessionCreator>()).DispatchDueAsync(
            Request() with { LeaseOwner = leaseOwner, BatchSize = 1000 }, CancellationToken.None);

        await Assert.That(result.Claimed).IsEqualTo(0);
        await repository.Received(1).ClaimDueDispatchEffectsAsync(
            leaseOwner, 1, UtcNow, TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DispatchDueAsync_ReplacesEmptyProviderFailureCode()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Failed(new HostedCheckoutFailure(
                string.Empty, HostedCheckoutFailureKind.ProviderRejected, ProviderRequestId: "req_bad")));
        repository.FailCheckoutDispatchAsync(
            claim, "checkout_provider_rejected", "req_bad", UtcNow.AddTicks(1), CancellationToken.None).Returns(true);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Parked).IsEqualTo(1);
        await repository.Received(1).FailCheckoutDispatchAsync(
            claim, "checkout_provider_rejected", "req_bad", UtcNow.AddTicks(1), CancellationToken.None);
    }

    [Test]
    public async Task DispatchDueAsync_PreservesMaximumLengthProviderFailureCode()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        string failureCode = new('x', CheckoutDispatchEffect.MaxFailureCodeLength);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Failed(new HostedCheckoutFailure(
                failureCode, HostedCheckoutFailureKind.ProviderRejected, ProviderRequestId: "req_bad")));
        repository.FailCheckoutDispatchAsync(
            claim, failureCode, "req_bad", UtcNow.AddTicks(1), CancellationToken.None).Returns(true);

        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator).DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Parked).IsEqualTo(1);
        await repository.Received(1).FailCheckoutDispatchAsync(
            claim, failureCode, "req_bad", UtcNow.AddTicks(1), CancellationToken.None);
    }

    [Test]
    public async Task DispatchDueAsync_UnknownSessionAmountMismatchDoesNotRetrievePaymentIntent()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out PaymentAttempt attempt);
        attempt.MarkRequiresAction("cs_existing", UtcNow.AddSeconds(-2), "req_create");
        attempt.MarkUnknown(UtcNow.AddSeconds(-1), "req_unknown");
        var retriever = Substitute.For<IHostedCheckoutSessionRetriever>();
        retriever.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(
                Session() with { SessionId = "cs_existing", PaymentId = "pi_existing", AmountTotalMinor = 12_51 },
                "req_retrieve"));
        var paymentIntents = Substitute.For<IPaymentIntentRetriever>();
        paymentIntents.RetrievePaymentIntentAsync(Arg.Any<PaymentIntentRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentIntentRetrieveResult.Succeeded(
                new("pi_existing", 12_50, "EUR", 4_50, PaymentIntentStatus.Succeeded), "req_pi"));
        repository.MarkCheckoutDispatchUnknownAsync(
            claim, "req_retrieve", UtcNow.AddTicks(1), CancellationToken.None).Returns(true);

        RegistrationPaymentCheckoutDispatchResult result = await Service(
            repository, Substitute.For<IHostedCheckoutSessionCreator>(), retriever, paymentIntents)
            .DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Unknown).IsEqualTo(1);
        await paymentIntents.DidNotReceiveWithAnyArgs().RetrievePaymentIntentAsync(default!, default);
        await repository.DidNotReceiveWithAnyArgs().CompleteCheckoutDispatchAsync(default!, default!, default, default, default);
    }

    [Test]
    public async Task DispatchDueAsync_UnknownPaymentIntentAmountMismatchStaysUnknown()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out PaymentAttempt attempt);
        attempt.MarkRequiresAction("cs_existing", UtcNow.AddSeconds(-2), "req_create");
        attempt.MarkUnknown(UtcNow.AddSeconds(-1), "req_unknown");
        var retriever = Substitute.For<IHostedCheckoutSessionRetriever>();
        retriever.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(
                Session() with { SessionId = "cs_existing", PaymentId = "pi_existing" }, "req_retrieve"));
        var paymentIntents = Substitute.For<IPaymentIntentRetriever>();
        paymentIntents.RetrievePaymentIntentAsync(Arg.Any<PaymentIntentRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentIntentRetrieveResult.Succeeded(
                new("pi_existing", 12_51, "EUR", 4_50, PaymentIntentStatus.Succeeded), "req_pi"));
        repository.MarkCheckoutDispatchUnknownAsync(
            claim, "req_retrieve", UtcNow.AddTicks(1), CancellationToken.None).Returns(true);

        RegistrationPaymentCheckoutDispatchResult result = await Service(
            repository, Substitute.For<IHostedCheckoutSessionCreator>(), retriever, paymentIntents)
            .DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Unknown).IsEqualTo(1);
        await repository.DidNotReceiveWithAnyArgs().CompleteCheckoutDispatchAsync(default!, default!, default, default, default);
    }

    [Test]
    public async Task DispatchDueAsync_SecondPreHandoffFailureUsesTenSecondBackoff()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim original, out PaymentAttempt attempt);
        CheckoutDispatchClaim claim = original with { AttemptCount = 2 };
        repository.ClaimDueDispatchEffectsAsync(
                "checkout-test", 1, UtcNow, TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns([claim], []);
        repository.GetClaimedAttemptAsync(claim, UtcNow, Arg.Any<CancellationToken>()).Returns(attempt);
        repository.PrepareCheckoutDispatchAsync(claim, UtcNow, UtcNow.AddMinutes(31), Arg.Any<CancellationToken>()).Returns(attempt);
        var creator = Substitute.For<IHostedCheckoutSessionCreator>();
        creator.CreateAsync(Arg.Any<HostedCheckoutCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutCreateResult.Failed(new HostedCheckoutFailure(
                "configuration_unavailable", HostedCheckoutFailureKind.Configuration,
                ProviderHandoffStarted: false,
                PreHandoffDisposition: HostedCheckoutPreHandoffDisposition.Transient)));
        repository.DeferCheckoutDispatchForConfigurationAsync(
                claim, "configuration_unavailable", UtcNow.AddTicks(1).AddSeconds(10), UtcNow.AddTicks(1), CancellationToken.None)
            .Returns(CheckoutDispatchConfigurationDisposition.Deferred);

        RegistrationPaymentCheckoutDispatchRequest request = Request() with { BatchSize = 1 };
        RegistrationPaymentCheckoutDispatchResult result = await Service(repository, creator)
            .DispatchDueAsync(request, CancellationToken.None);

        await Assert.That(result.Retried).IsEqualTo(1);
        await repository.Received(1).DeferCheckoutDispatchForConfigurationAsync(
            claim, "configuration_unavailable", UtcNow.AddTicks(1).AddSeconds(10), UtcNow.AddTicks(1), CancellationToken.None);
    }

    [Test]
    public async Task DispatchDueAsync_CancelledExpiredActivationDispositionCountsParked()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        var activation = Substitute.For<IPaidCheckoutActivationService>();
        activation.EvaluateAsync(Arg.Any<PaidCheckoutActivationRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaidCheckoutActivationResult.Failure("paid_sale_stopped", "stopped"));
        repository.DeferCheckoutDispatchForConfigurationAsync(
                claim, "paid_sale_stopped", UtcNow.AddMinutes(5), UtcNow, Arg.Any<CancellationToken>())
            .Returns(CheckoutDispatchConfigurationDisposition.CancelledExpired);

        RegistrationPaymentCheckoutDispatchResult result = await Service(
            repository, Substitute.For<IHostedCheckoutSessionCreator>(), activation: activation)
            .DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Parked).IsEqualTo(1);
        await Assert.That(result.Retried).IsEqualTo(0);
        await Assert.That(result.Stale).IsEqualTo(0);
    }

    [Test]
    public async Task DispatchDueAsync_StaleActivationDispositionCountsStale()
    {
        var repository = RepositoryWithClaim(out CheckoutDispatchClaim claim, out _);
        var activation = Substitute.For<IPaidCheckoutActivationService>();
        activation.EvaluateAsync(Arg.Any<PaidCheckoutActivationRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaidCheckoutActivationResult.Failure("paid_sale_stopped", "stopped"));
        repository.DeferCheckoutDispatchForConfigurationAsync(
                claim, "paid_sale_stopped", UtcNow.AddMinutes(5), UtcNow, Arg.Any<CancellationToken>())
            .Returns(CheckoutDispatchConfigurationDisposition.Stale);

        RegistrationPaymentCheckoutDispatchResult result = await Service(
            repository, Substitute.For<IHostedCheckoutSessionCreator>(), activation: activation)
            .DispatchDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Stale).IsEqualTo(1);
        await Assert.That(result.Retried).IsEqualTo(0);
        await Assert.That(result.Parked).IsEqualTo(0);
    }

    private static IRegistrationPaymentAttemptRepository RepositoryWithClaim(out CheckoutDispatchClaim claim, out PaymentAttempt attempt)
    {
        claim = new(
            Guid.CreateVersion7(),
            TenantId,
            OrderId,
            AttemptId,
            Guid.CreateVersion7(),
            4,
            CheckoutDispatchReplayKind.None,
            1);
        attempt = Attempt();
        var repository = Substitute.For<IRegistrationPaymentAttemptRepository>();
        CheckoutDispatchClaim claimed = claim;
        int claimCount = 0;
        repository.ClaimDueDispatchEffectsAsync("checkout-test", 1, UtcNow, TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns(_ => claimCount++ == 0 ? [claimed] : []);
        repository.GetClaimedAttemptAsync(claim, UtcNow, Arg.Any<CancellationToken>()).Returns(attempt);
        repository.PrepareCheckoutDispatchAsync(claim, UtcNow, UtcNow.AddMinutes(31), Arg.Any<CancellationToken>()).Returns(attempt);
        return repository;
    }

    private static RegistrationPaymentCheckoutDispatchService Service(
        IRegistrationPaymentAttemptRepository repository,
        IHostedCheckoutSessionCreator creator,
        IHostedCheckoutSessionRetriever? retriever = null,
        IPaymentIntentRetriever? paymentIntents = null,
        IRegistrationOrderLifecycleService? orderLifecycle = null,
        TimeProvider? timeProvider = null,
        IPaidCheckoutActivationService? activation = null,
        IPaidOrderAcceptanceFreshnessService? freshness = null) =>
        new(
            repository,
            creator,
            retriever ?? Substitute.For<IHostedCheckoutSessionRetriever>(),
            paymentIntents ?? Substitute.For<IPaymentIntentRetriever>(),
            orderLifecycle ?? Substitute.For<IRegistrationOrderLifecycleService>(),
            timeProvider ?? new FixedTimeProvider(UtcNow),
            activation ?? ReadyActivation(),
            freshness ?? CurrentAcceptance());

    private static IPaidOrderAcceptanceFreshnessService CurrentAcceptance()
    {
        var freshness = Substitute.For<IPaidOrderAcceptanceFreshnessService>();
        freshness.IsCurrentAsync(Arg.Any<PaymentAttempt>(), Arg.Any<CancellationToken>()).Returns(true);
        return freshness;
    }

    private static IPaidCheckoutActivationService ReadyActivation()
    {
        var activation = Substitute.For<IPaidCheckoutActivationService>();
        activation.EvaluateAsync(Arg.Any<PaidCheckoutActivationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaidCheckoutActivationResult(true, null, "active"));
        return activation;
    }

    private static RegistrationPaymentCheckoutDispatchRequest Request() => new(
        "checkout-test",
        10,
        TimeSpan.FromMinutes(2),
        new Uri("https://events.example.test"),
        new Uri("https://events.example.test/checkout/success"),
        new Uri("https://events.example.test/checkout/cancel"));

    private static HostedCheckoutSession Session() => new(
        "cs_123",
        new Uri("https://checkout.stripe.example.test/c/pay/cs_123"),
        HostedCheckoutSessionStatus.Open,
        HostedCheckoutPaymentStatus.Unpaid,
        null,
        UtcNow.AddMinutes(30),
        12_50,
        "EUR");

    private static PaymentAttempt Attempt(Guid? attemptId = null)
    {
        OrganizerPaymentRecipientSnapshot recipient = OrganizerPaymentRecipientSnapshot.Create(
            TenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "stripe",
            "platform-eu",
            "acct_123",
            "BE",
            "EUR",
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            UtcNow.AddMinutes(-2));
        PaymentAttempt attempt = PaymentAttempt.Create(
            attemptId ?? AttemptId,
            TenantId,
            OrderId,
            recipient,
            "OrganizerDirect",
            "2026-08-20.acacia",
            "composition-1",
            10_00,
            2_00,
            2_50,
            "checkout:stable",
            UtcNow.AddMinutes(-2),
            UtcNow.AddMinutes(30));
        attempt.AttachAcceptance(PaidAcceptanceTestFacts.Create(
            TenantId, OrderId, Guid.CreateVersion7(), "composition-1",
            recipient.InstancePolicyVersionId, recipient.TenantPolicyVersionId,
            10_00, 2_00, 2_50, UtcNow.AddMinutes(-1)));
        return attempt;
    }

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class MutableTimeProvider(DateTime now) : TimeProvider
    {
        private DateTime _now = now;

        public void Set(DateTime value) => _now = value;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
