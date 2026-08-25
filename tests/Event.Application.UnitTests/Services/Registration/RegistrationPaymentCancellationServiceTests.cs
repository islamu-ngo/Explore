// ABOUTME: Specifies provider cancellation routing for handed-off uncaptured event payments.
// ABOUTME: Proves the original account and stable campaign key are used outside persistence mutation.

using Explore.Application.Contracts.Payments;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.ValueObjects;
using NSubstitute;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class RegistrationPaymentCancellationServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task HandedOffUncapturedPaymentCancelsOnOriginalAccountAndAuditsCampaignActor()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid actorId = Guid.CreateVersion7();
        PaymentAttempt payment = UncapturedPayment(tenantId);
        RefundCampaign campaign = RefundCampaign.CreateCancellation(
            Guid.CreateVersion7(), tenantId, Guid.CreateVersion7(), actorId, "Cancelled.", Now);
        var payments = Substitute.For<IRegistrationPaymentAttemptRepository>();
        var refunds = Substitute.For<IRefundAttemptRepository>();
        var campaigns = Substitute.For<IRefundCampaignRepository>();
        var provider = Substitute.For<IPaymentCancellationProvider>();
        payments.GetByIdForCancellationAsync(tenantId, payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        campaigns.GetByIdAsync(tenantId, campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        provider.CancelAsync(Arg.Any<PaymentCancellationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentCancellationProviderResult(PaymentCancellationProviderOutcome.Cancelled, "req_cancel"));
        payments.MarkCancelledAfterProviderAsync(
                tenantId, payment.Id, actorId, Now, "req_cancel", Arg.Any<CancellationToken>())
            .Returns(true);

        bool completed = await new RegistrationPaymentCancellationService(
            payments, refunds, campaigns, provider, new FixedTimeProvider(Now))
            .CancelAsync(tenantId, campaign.Id, payment.Id, CancellationToken.None);

        await Assert.That(completed).IsTrue();
        await provider.Received(1).CancelAsync(
            Arg.Is<PaymentCancellationRequest>(request =>
                request.ExternalAccountId == "acct_original" &&
                request.ProviderCheckoutSessionId == "cs_cancel" &&
                request.ProviderIdempotencyKey == $"cancel:{campaign.Id:N}:{payment.Id:N}"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DefinitiveProviderCancellationFailureRequiresOperatorAndAcknowledgesOutbox()
    {
        Guid tenantId = Guid.CreateVersion7();
        PaymentAttempt payment = UncapturedPayment(tenantId);
        RefundCampaign campaign = RefundCampaign.CreateCancellation(
            Guid.CreateVersion7(), tenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Cancelled.", Now);
        var payments = Substitute.For<IRegistrationPaymentAttemptRepository>();
        var refunds = Substitute.For<IRefundAttemptRepository>();
        var campaigns = Substitute.For<IRefundCampaignRepository>();
        var provider = Substitute.For<IPaymentCancellationProvider>();
        payments.GetByIdForCancellationAsync(tenantId, payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        campaigns.GetByIdAsync(tenantId, campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        provider.CancelAsync(Arg.Any<PaymentCancellationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentCancellationProviderResult(PaymentCancellationProviderOutcome.Failed, "req_rejected"));

        bool acknowledged = await new RegistrationPaymentCancellationService(
            payments, refunds, campaigns, provider, new FixedTimeProvider(Now))
            .CancelAsync(tenantId, campaign.Id, payment.Id, CancellationToken.None);

        await Assert.That(acknowledged).IsTrue();
        await provider.Received(1).CancelAsync(Arg.Any<PaymentCancellationRequest>(), Arg.Any<CancellationToken>());
        await campaigns.Received(1).RequireOperatorAsync(tenantId, campaign.Id, Now, Arg.Any<CancellationToken>());
    }

    private static PaymentAttempt UncapturedPayment(Guid tenantId)
    {
        Guid orderId = Guid.CreateVersion7();
        OrganizerPaymentRecipientSnapshot recipient = OrganizerPaymentRecipientSnapshot.Create(
            tenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "stripe", "platform-live-eu", "acct_original",
            "BE", "EUR", Guid.CreateVersion7(), null, Now.AddMinutes(-2));
        PaymentAttempt payment = PaymentAttempt.Create(
            Guid.CreateVersion7(), tenantId, orderId, recipient, "OrganizerDirect", "2026-08-20.acacia", "composition-1",
            Money.Create(1_000, recipient.CurrencyCode), Money.Create(75, recipient.CurrencyCode), Money.Create(0, recipient.CurrencyCode), "payment:cancel", Now.AddMinutes(-2), Now.AddMinutes(30));
        payment.MarkRequiresAction("cs_cancel", Now.AddMinutes(-1), "req_checkout");
        return payment;
    }

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
