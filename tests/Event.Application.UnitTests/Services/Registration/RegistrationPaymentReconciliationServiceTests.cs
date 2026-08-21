// ABOUTME: Verifies authoritative Checkout and PaymentIntent reconciliation over durable payment claims.
// ABOUTME: Covers account-scoped retrieval, exact money checks, monotonic outcomes, retry, and parking.

using Explore.Application.Contracts.Payments;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class RegistrationPaymentReconciliationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly Guid AttemptId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000003");

    [Test]
    public async Task ReconcileDueAsync_FiftyDueEffectsUseFiftyFreshOneItemClaims()
    {
        var repository = Substitute.For<IRegistrationPaymentAttemptRepository>();
        var checkout = Substitute.For<IHostedCheckoutSessionRetriever>();
        var payments = Substitute.For<IPaymentIntentRetriever>();
        PaymentReconciliationClaim[] claims = Enumerable.Range(0, 50)
            .Select(index => new PaymentReconciliationClaim(TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), index + 1, 1))
            .ToArray();
        int cursor = 0;
        repository.ClaimDueReconciliationsAsync("payment-reconciliation", 1, Arg.Any<DateTime>(), TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns(_ => cursor < claims.Length ? new[] { claims[cursor++] } : []);
        foreach (PaymentReconciliationClaim claim in claims)
        {
            repository.GetReconciliationAttemptAsync(claim, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns((PaymentAttempt?)null);
            repository.SettleReconciliationAsync(claim, Arg.Any<PaymentReconciliationDecision>(), Arg.Any<CancellationToken>()).Returns(true);
        }

        RegistrationPaymentReconciliationResult result = await new RegistrationPaymentReconciliationService(
            repository, checkout, payments, new FixedTimeProvider(UtcNow)).ReconcileDueAsync(
            new("payment-reconciliation", 50, TimeSpan.FromMinutes(2)), CancellationToken.None);

        await Assert.That(result.Claimed).IsEqualTo(50);
        await repository.Received(50).ClaimDueReconciliationsAsync(
            "payment-reconciliation", 1, Arg.Any<DateTime>(), TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReconcileDueAsync_AuthoritativePaidSessionAndSucceededIntent_CompletesWithRequestEvidence()
    {
        var setup = Setup();
        setup.Checkout.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(Session(), "req_session"));
        setup.Payment.RetrievePaymentIntentAsync(Arg.Any<PaymentIntentRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentIntentRetrieveResult.Succeeded(Intent(), "req_intent"));

        RegistrationPaymentReconciliationResult result = await setup.Service.ReconcileDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Succeeded).IsEqualTo(1);
        await setup.Checkout.Received(1).RetrieveAsync(
            Arg.Is<HostedCheckoutRetrieveRequest>(request => request.ExternalAccountId == "acct_123" && request.ProviderCheckoutSessionId == "cs_123"),
            Arg.Any<CancellationToken>());
        await setup.Payment.Received(1).RetrievePaymentIntentAsync(
            Arg.Is<PaymentIntentRetrieveRequest>(request => request.ExternalAccountId == "acct_123" && request.PaymentIntentId == "pi_123"),
            Arg.Any<CancellationToken>());
        await setup.Repository.Received(1).SettleReconciliationAsync(
            setup.Claim,
            Arg.Is<PaymentReconciliationDecision>(decision =>
                decision.Disposition == PaymentReconciliationDisposition.Complete &&
                decision.Status == PaymentAttemptStatusEnum.Succeeded &&
                decision.ProviderRequestId == "req_intent"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReconcileDueAsync_MoneyOrApplicationFeeMismatch_ParksWithoutSuccess()
    {
        var setup = Setup();
        setup.Checkout.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(Session(), "req_session"));
        setup.Payment.RetrievePaymentIntentAsync(Arg.Any<PaymentIntentRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentIntentRetrieveResult.Succeeded(Intent() with { ApplicationFeeMinor = 449 }, "req_wrong"));

        RegistrationPaymentReconciliationResult result = await setup.Service.ReconcileDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Parked).IsEqualTo(1);
        await Assert.That(result.Succeeded).IsEqualTo(0);
        await setup.Repository.Received(1).SettleReconciliationAsync(
            setup.Claim,
            Arg.Is<PaymentReconciliationDecision>(decision =>
                decision.Disposition == PaymentReconciliationDisposition.Park &&
                decision.FailureCode == "payment_reconciliation_money_mismatch"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReconcileDueAsync_ProviderTimeout_RecordsUnknownAndRetries()
    {
        var setup = Setup();
        setup.Checkout.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Unknown(new HostedCheckoutFailure(
                "checkout_provider_network_ambiguous", HostedCheckoutFailureKind.Network, ProviderRequestId: "req_timeout")));

        RegistrationPaymentReconciliationResult result = await setup.Service.ReconcileDueAsync(Request(), CancellationToken.None);

        await Assert.That(result.Unknown).IsEqualTo(1);
        await setup.Repository.Received(1).SettleReconciliationAsync(
            setup.Claim,
            Arg.Is<PaymentReconciliationDecision>(decision =>
                decision.Disposition == PaymentReconciliationDisposition.Retry &&
                decision.Status == PaymentAttemptStatusEnum.Unknown &&
                decision.ProviderRequestId == "req_timeout"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(PaymentIntentStatus.RequiresAction, PaymentAttemptStatusEnum.RequiresAction)]
    [Arguments(PaymentIntentStatus.Processing, PaymentAttemptStatusEnum.Processing)]
    [Arguments(PaymentIntentStatus.Canceled, PaymentAttemptStatusEnum.Failed)]
    public async Task ReconcileDueAsync_MapsBuyerActionProcessingAndTerminalFailure(
        PaymentIntentStatus providerStatus,
        PaymentAttemptStatusEnum expectedStatus)
    {
        var setup = Setup();
        setup.Checkout.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(Session(), "req_session"));
        setup.Payment.RetrievePaymentIntentAsync(Arg.Any<PaymentIntentRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentIntentRetrieveResult.Succeeded(Intent() with { Status = providerStatus }, "req_status"));

        _ = await setup.Service.ReconcileDueAsync(Request(), CancellationToken.None);

        await setup.Repository.Received(1).SettleReconciliationAsync(
            setup.Claim,
            Arg.Is<PaymentReconciliationDecision>(decision => decision.Status == expectedStatus),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReconcileDueAsync_ExpiredSessionWithPaymentId_RetrievesAuthoritativePaymentBeforeCancellation()
    {
        var setup = Setup();
        setup.Checkout.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(
                Session() with { Status = HostedCheckoutSessionStatus.Expired, PaymentStatus = HostedCheckoutPaymentStatus.Paid },
                "req_expired"));
        setup.Payment.RetrievePaymentIntentAsync(Arg.Any<PaymentIntentRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentIntentRetrieveResult.Succeeded(
                Intent() with { Status = PaymentIntentStatus.Succeeded },
                "req_payment"));

        _ = await setup.Service.ReconcileDueAsync(Request(), CancellationToken.None);

        await setup.Payment.Received(1).RetrievePaymentIntentAsync(Arg.Any<PaymentIntentRetrieveRequest>(), Arg.Any<CancellationToken>());
        await setup.Repository.Received(1).SettleReconciliationAsync(
            setup.Claim,
            Arg.Is<PaymentReconciliationDecision>(decision =>
                decision.Disposition == PaymentReconciliationDisposition.Complete &&
                decision.Status == PaymentAttemptStatusEnum.Succeeded),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReconcileDueAsync_CompletePaidSessionWithoutPaymentIntent_IsUnknownNotBuyerAction()
    {
        var setup = Setup();
        setup.Checkout.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(Session() with { PaymentId = null }, "req_missing_pi"));

        _ = await setup.Service.ReconcileDueAsync(Request(), CancellationToken.None);

        await setup.Repository.Received(1).SettleReconciliationAsync(
            setup.Claim,
            Arg.Is<PaymentReconciliationDecision>(decision =>
                decision.Disposition == PaymentReconciliationDisposition.Retry &&
                decision.Status == PaymentAttemptStatusEnum.Unknown),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReconcileDueAsync_CompleteUnpaidRequiresPaymentMethod_IsAuthoritativeFailure()
    {
        var setup = Setup();
        setup.Checkout.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(
                Session() with { PaymentStatus = HostedCheckoutPaymentStatus.Unpaid },
                "req_failed"));
        setup.Payment.RetrievePaymentIntentAsync(Arg.Any<PaymentIntentRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentIntentRetrieveResult.Succeeded(Intent() with { Status = PaymentIntentStatus.RequiresPaymentMethod }, "req_failed_pi"));

        _ = await setup.Service.ReconcileDueAsync(Request(), CancellationToken.None);

        await setup.Repository.Received(1).SettleReconciliationAsync(
            setup.Claim,
            Arg.Is<PaymentReconciliationDecision>(decision =>
                decision.Disposition == PaymentReconciliationDisposition.Complete &&
                decision.Status == PaymentAttemptStatusEnum.Failed),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReconcileDueAsync_OpenUnpaidRequiresPaymentMethod_RemainsBuyerAction()
    {
        var setup = Setup();
        setup.Checkout.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(
                Session() with { Status = HostedCheckoutSessionStatus.Open, PaymentStatus = HostedCheckoutPaymentStatus.Unpaid },
                "req_action"));
        setup.Payment.RetrievePaymentIntentAsync(Arg.Any<PaymentIntentRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaymentIntentRetrieveResult.Succeeded(Intent() with { Status = PaymentIntentStatus.RequiresPaymentMethod }, "req_action_pi"));

        _ = await setup.Service.ReconcileDueAsync(Request(), CancellationToken.None);

        await setup.Repository.Received(1).SettleReconciliationAsync(
            setup.Claim,
            Arg.Is<PaymentReconciliationDecision>(decision =>
                decision.Disposition == PaymentReconciliationDisposition.Retry &&
                decision.Status == PaymentAttemptStatusEnum.RequiresAction),
            Arg.Any<CancellationToken>());
    }

    private static TestSetup Setup()
    {
        PaymentAttempt attempt = Attempt();
        attempt.MarkRequiresAction("cs_123", UtcNow.AddMinutes(-1), "req_create");
        var claim = new PaymentReconciliationClaim(TenantId, Guid.CreateVersion7(), AttemptId, Guid.CreateVersion7(), 1, 1);
        var repository = Substitute.For<IRegistrationPaymentAttemptRepository>();
        int claimCount = 0;
        repository.ClaimDueReconciliationsAsync("payment-reconciliation", 1, UtcNow, TimeSpan.FromMinutes(2), Arg.Any<CancellationToken>())
            .Returns(_ => claimCount++ == 0 ? [claim] : []);
        repository.GetReconciliationAttemptAsync(claim, UtcNow, Arg.Any<CancellationToken>()).Returns(attempt);
        repository.SettleReconciliationAsync(claim, Arg.Any<PaymentReconciliationDecision>(), Arg.Any<CancellationToken>()).Returns(true);
        var checkout = Substitute.For<IHostedCheckoutSessionRetriever>();
        var payment = Substitute.For<IPaymentIntentRetriever>();
        return new(repository, checkout, payment, claim, new(repository, checkout, payment, new FixedTimeProvider(UtcNow)));
    }

    private static RegistrationPaymentReconciliationRequest Request() => new("payment-reconciliation");

    private static HostedCheckoutSession Session() => new(
        "cs_123", null, HostedCheckoutSessionStatus.Complete, HostedCheckoutPaymentStatus.Paid,
        "pi_123", UtcNow.AddMinutes(30), 1_250, "EUR");

    private static PaymentIntentObservation Intent() => new(
        "pi_123", 1_250, "EUR", 450, PaymentIntentStatus.Succeeded);

    private static PaymentAttempt Attempt()
    {
        OrganizerPaymentRecipientSnapshot recipient = OrganizerPaymentRecipientSnapshot.Create(
            TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "stripe", "platform-eu", "acct_123", "BE", "EUR",
            Guid.CreateVersion7(), Guid.CreateVersion7(), UtcNow.AddMinutes(-2));
        return PaymentAttempt.Create(
            AttemptId, TenantId, Guid.CreateVersion7(), recipient, "OrganizerDirect", "2026-08-20.acacia",
            "composition-1", 1_000, 200, 250, "checkout:stable", UtcNow.AddMinutes(-2), UtcNow.AddMinutes(30));
    }

    private sealed record TestSetup(
        IRegistrationPaymentAttemptRepository Repository,
        IHostedCheckoutSessionRetriever Checkout,
        IPaymentIntentRetriever Payment,
        PaymentReconciliationClaim Claim,
        RegistrationPaymentReconciliationService Service);

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
