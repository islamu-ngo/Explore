// ABOUTME: Specifies bounded, restart-safe refund-campaign reservation and dispatch scheduling.
// ABOUTME: Proves captured payments use accepted authority and provider I/O is deferred to outbox work.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.ValueObjects;
using NSubstitute;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class RefundCampaignProcessorTests
{
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly DateTime Now = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task ProcessBatchReservesAcceptedCapacityAndSchedulesDispatchWithoutProviderIo()
    {
        RefundCampaign campaign = RefundCampaign.CreateCancellation(
            Guid.CreateVersion7(), TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Cancelled.", Now);
        RefundCampaignClaim claim = campaign.Claim(Guid.CreateVersion7(), Now, TimeSpan.FromMinutes(5));
        PaymentAttempt payment = CapturedPayment();
        var campaigns = Substitute.For<IRefundCampaignRepository>();
        var refunds = Substitute.For<IRefundAttemptRepository>();
        campaigns.TryClaimAsync(TenantId, campaign.Id, Arg.Any<Guid>(), Now, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((campaign, claim));
        campaigns.GetCapturedPaymentPageAsync(campaign, RefundCampaignProcessor.BatchSize, Arg.Any<CancellationToken>())
            .Returns(new RefundCampaignPaymentPage([payment], HasMore: false));
        refunds.GetRefundableCapacityAsync(TenantId, payment.Id, Arg.Any<CancellationToken>()).Returns(payment.TotalMinor);
        refunds.ReserveAsync(Arg.Any<RefundAttempt>(), Arg.Any<CancellationToken>())
            .Returns(call => new RefundReservationResult(RefundReservationDisposition.Reserved, call.Arg<RefundAttempt>()));
        campaigns.CompleteBatchAsync(
                TenantId, campaign.Id, claim, payment.CampaignCursor,
                Arg.Any<RefundCampaignBatchOutcome>(), false,
                Arg.Any<IReadOnlyCollection<RegistrationMaterialChangeChoice>>(),
                Arg.Any<IReadOnlyCollection<OutboxMessage>>(),
                Now, Arg.Any<CancellationToken>())
            .Returns(true);
        campaigns.GetByIdAsync(TenantId, campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);

        RefundCampaign? result = await new RefundCampaignProcessor(
            campaigns, refunds, Substitute.For<IRegistrationMaterialChangeChoiceRepository>(),
            Substitute.For<IRegistrationPaymentAttemptRepository>(), new FixedTimeProvider(Now)).ProcessBatchAsync(
            TenantId, campaign.Id, Guid.CreateVersion7(), CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(campaign);
        await refunds.Received(1).ReserveAsync(
            Arg.Is<RefundAttempt>(attempt =>
                attempt.SourceCampaignId == campaign.Id &&
                attempt.PaidOrderAcceptanceSnapshotId == payment.PaidOrderAcceptanceSnapshotId &&
                attempt.Allocation.TotalMinor == payment.TotalMinor),
            Arg.Any<CancellationToken>());
        await campaigns.Received(1).CompleteBatchAsync(
            TenantId, campaign.Id, claim, payment.CampaignCursor,
            Arg.Is<RefundCampaignBatchOutcome>(value => value.Total == 1 && value.Generated == 1 && value.OperatorCases == 0),
            false,
            Arg.Is<IReadOnlyCollection<RegistrationMaterialChangeChoice>>(choices => choices.Count == 0),
            Arg.Is<IReadOnlyCollection<OutboxMessage>>(messages =>
                messages.Count == 1 && messages.Single().EventType == RefundOutboxMessageFactory.DispatchRequested),
            Now,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MaterialChangeCampaignProtectsAcceptedPaymentBeforeLateCapture()
    {
        RefundCampaign campaign = RefundCampaign.CreateMaterialChange(
            Guid.CreateVersion7(), TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Schedule changed.", Now);
        RefundCampaignClaim claim = campaign.Claim(Guid.CreateVersion7(), Now, TimeSpan.FromMinutes(5));
        PaymentAttempt payment = UncapturedPayment();
        var campaigns = Substitute.For<IRefundCampaignRepository>();
        var refunds = Substitute.For<IRefundAttemptRepository>();
        campaigns.TryClaimAsync(TenantId, campaign.Id, Arg.Any<Guid>(), Now, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((campaign, claim));
        campaigns.GetCapturedPaymentPageAsync(campaign, RefundCampaignProcessor.BatchSize, Arg.Any<CancellationToken>())
            .Returns(new RefundCampaignPaymentPage([payment], HasMore: false));
        campaigns.CompleteBatchAsync(
                TenantId, campaign.Id, claim, payment.CampaignCursor,
                Arg.Any<RefundCampaignBatchOutcome>(), false,
                Arg.Any<IReadOnlyCollection<RegistrationMaterialChangeChoice>>(),
                Arg.Any<IReadOnlyCollection<OutboxMessage>>(), Now, Arg.Any<CancellationToken>())
            .Returns(true);
        campaigns.GetByIdAsync(TenantId, campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);

        _ = await new RefundCampaignProcessor(
                campaigns, refunds, Substitute.For<IRegistrationMaterialChangeChoiceRepository>(),
                Substitute.For<IRegistrationPaymentAttemptRepository>(), new FixedTimeProvider(Now))
            .ProcessBatchAsync(TenantId, campaign.Id, Guid.CreateVersion7(), CancellationToken.None);

        await refunds.DidNotReceive().ReserveAsync(Arg.Any<RefundAttempt>(), Arg.Any<CancellationToken>());
        await campaigns.Received(1).CompleteBatchAsync(
            TenantId, campaign.Id, claim, payment.CampaignCursor,
            Arg.Is<RefundCampaignBatchOutcome>(value => value.Generated == 1), false,
            Arg.Is<IReadOnlyCollection<RegistrationMaterialChangeChoice>>(choices =>
                choices.Single().RegistrationOrderId == payment.RegistrationOrderId),
            Arg.Is<IReadOnlyCollection<OutboxMessage>>(messages => messages.Count == 0),
            Now, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancellationCampaignRoutesUncapturedHandoffToPaymentCancellation()
    {
        RefundCampaign campaign = RefundCampaign.CreateCancellation(
            Guid.CreateVersion7(), TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "Cancelled.", Now);
        RefundCampaignClaim claim = campaign.Claim(Guid.CreateVersion7(), Now, TimeSpan.FromMinutes(5));
        PaymentAttempt payment = UncapturedPayment();
        var campaigns = Substitute.For<IRefundCampaignRepository>();
        var refunds = Substitute.For<IRefundAttemptRepository>();
        var payments = Substitute.For<IRegistrationPaymentAttemptRepository>();
        campaigns.TryClaimAsync(TenantId, campaign.Id, Arg.Any<Guid>(), Now, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((campaign, claim));
        campaigns.GetCapturedPaymentPageAsync(campaign, RefundCampaignProcessor.BatchSize, Arg.Any<CancellationToken>())
            .Returns(new RefundCampaignPaymentPage([payment], false));
        payments.TryCancelBeforeProviderHandoffAsync(
                TenantId, payment.RegistrationOrderId, Now, Arg.Any<CancellationToken>())
            .Returns(PaymentCancellationDisposition.RequiresReconciliation);
        campaigns.CompleteBatchAsync(
                TenantId, campaign.Id, claim, payment.CampaignCursor,
                Arg.Any<RefundCampaignBatchOutcome>(), false,
                Arg.Any<IReadOnlyCollection<RegistrationMaterialChangeChoice>>(),
                Arg.Any<IReadOnlyCollection<OutboxMessage>>(), Now, Arg.Any<CancellationToken>())
            .Returns(true);
        campaigns.GetByIdAsync(TenantId, campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);

        _ = await new RefundCampaignProcessor(
                campaigns, refunds, Substitute.For<IRegistrationMaterialChangeChoiceRepository>(),
                payments, new FixedTimeProvider(Now))
            .ProcessBatchAsync(TenantId, campaign.Id, Guid.CreateVersion7(), CancellationToken.None);

        await refunds.DidNotReceive().ReserveAsync(Arg.Any<RefundAttempt>(), Arg.Any<CancellationToken>());
        await campaigns.Received(1).CompleteBatchAsync(
            TenantId, campaign.Id, claim, payment.CampaignCursor,
            Arg.Any<RefundCampaignBatchOutcome>(), false,
            Arg.Any<IReadOnlyCollection<RegistrationMaterialChangeChoice>>(),
            Arg.Is<IReadOnlyCollection<OutboxMessage>>(messages =>
                messages.Single().EventType == RefundOutboxMessageFactory.PaymentCancellationRequested),
            Now, Arg.Any<CancellationToken>());
    }

    private static PaymentAttempt CapturedPayment()
    {
        Guid orderId = Guid.CreateVersion7();
        OrganizerPaymentRecipientSnapshot recipient = OrganizerPaymentRecipientSnapshot.Create(
            TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "stripe", "platform-live-eu", "acct_original",
            "BE", "EUR", Guid.CreateVersion7(), null, Now.AddMinutes(-2));
        PaymentAttempt payment = PaymentAttempt.Create(
            Guid.CreateVersion7(), TenantId, orderId, recipient, "OrganizerDirect", "2026-08-20.acacia", "composition-1",
            Money.Create(1_000, recipient.CurrencyCode), Money.Create(75, recipient.CurrencyCode), Money.Create(0, recipient.CurrencyCode), "payment:campaign", Now.AddMinutes(-2), Now.AddMinutes(30));
        payment.AttachAcceptance(RefundTestAcceptance.Create(
            TenantId, orderId, 1_000, 75, 0, Now.AddMinutes(-3), recipient));
        payment.MarkSucceeded("pi_campaign", Now.AddMinutes(-1), "req_payment");
        return payment;
    }

    private static PaymentAttempt UncapturedPayment()
    {
        Guid orderId = Guid.CreateVersion7();
        OrganizerPaymentRecipientSnapshot recipient = OrganizerPaymentRecipientSnapshot.Create(
            TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "stripe", "platform-live-eu", "acct_original",
            "BE", "EUR", Guid.CreateVersion7(), null, Now.AddMinutes(-2));
        PaymentAttempt payment = PaymentAttempt.Create(
            Guid.CreateVersion7(), TenantId, orderId, recipient, "OrganizerDirect", "2026-08-20.acacia", "composition-1",
            Money.Create(1_000, recipient.CurrencyCode), Money.Create(75, recipient.CurrencyCode), Money.Create(0, recipient.CurrencyCode), "payment:campaign:uncaptured", Now.AddMinutes(-2), Now.AddMinutes(30));
        payment.AttachAcceptance(RefundTestAcceptance.Create(
            TenantId, orderId, 1_000, 75, 0, Now.AddMinutes(-3), recipient));
        payment.MarkRequiresAction("cs_cancel", Now.AddMinutes(-1), "req_checkout");
        return payment;
    }

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
