// ABOUTME: Specifies buyer material-change refund choice against immutable campaign/payment evidence.
// ABOUTME: Proves choice mutation, refund reservation, and dispatch scheduling share one persistence call.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.ValueObjects;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class RegistrationMaterialChangeChoiceServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly DateTime Now = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task RefundChoiceUsesStableChoiceIdentityAndAtomicPersistenceBoundary()
    {
        RegistrationOrder order = Order();
        PaymentAttempt payment = CapturedPayment(order.Id);
        RefundCampaign campaign = RefundCampaign.CreateMaterialChange(
            Guid.CreateVersion7(), TenantId, order.EventId, Guid.CreateVersion7(), "Schedule changed.", Now);
        RegistrationMaterialChangeChoice choice = RegistrationMaterialChangeChoice.Create(
            Guid.CreateVersion7(), campaign, payment, Now);
        var choices = Substitute.For<IRegistrationMaterialChangeChoiceRepository>();
        var campaigns = Substitute.For<IRefundCampaignRepository>();
        var payments = Substitute.For<IRegistrationPaymentAttemptRepository>();
        var refunds = Substitute.For<IRefundAttemptRepository>();
        choices.GetAsync(TenantId, campaign.Id, order.Id, Arg.Any<CancellationToken>()).Returns(choice);
        campaigns.GetByIdAsync(TenantId, campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        payments.GetLatestByOrderAsync(TenantId, order.Id, Arg.Any<CancellationToken>())
            .Returns((payment, CheckoutDispatchEffect.Create(payment, Now)));
        refunds.GetRefundableCapacityAsync(TenantId, payment.Id, Arg.Any<CancellationToken>()).Returns(1_000);
        refunds.ReserveAndRecordMaterialChangeRefundAsync(
                Arg.Any<RefundAttempt>(), choice.Id, Arg.Any<Guid>(), Now,
                Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(call => new RefundReservationResult(
                RefundReservationDisposition.Reserved, call.Arg<RefundAttempt>()));

        var result = await new RegistrationMaterialChangeChoiceService(
            choices, campaigns, payments, refunds, new FixedTimeProvider(Now)).RespondAsync(
            order, campaign.Id, "request_refund", Guid.CreateVersion7(), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await refunds.Received(1).ReserveAndRecordMaterialChangeRefundAsync(
            Arg.Is<RefundAttempt>(attempt => attempt.SourceCampaignId == campaign.Id &&
                attempt.ProviderIdempotencyKey == $"refund-material-change:{choice.Id:N}" &&
                attempt.ReasonCode == "material_change"),
            choice.Id,
            Arg.Any<Guid>(),
            Now,
            Arg.Is<OutboxMessage>(message => message.EventType == RefundOutboxMessageFactory.DispatchRequested),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AcceptAfterRefundChoiceReturnsBoundedConflictWithoutNewRefund()
    {
        RegistrationOrder order = Order();
        PaymentAttempt payment = CapturedPayment(order.Id);
        RefundCampaign campaign = RefundCampaign.CreateMaterialChange(
            Guid.CreateVersion7(), TenantId, order.EventId, Guid.CreateVersion7(), "Schedule changed.", Now);
        RegistrationMaterialChangeChoice choice = RegistrationMaterialChangeChoice.Create(
            Guid.CreateVersion7(), campaign, payment, Now);
        choice.RequestRefund(Guid.CreateVersion7(), Now);
        var choices = Substitute.For<IRegistrationMaterialChangeChoiceRepository>();
        var campaigns = Substitute.For<IRefundCampaignRepository>();
        var refunds = Substitute.For<IRefundAttemptRepository>();
        choices.GetAsync(TenantId, campaign.Id, order.Id, Arg.Any<CancellationToken>()).Returns(choice);
        campaigns.GetByIdAsync(TenantId, campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);

        var result = await new RegistrationMaterialChangeChoiceService(
            choices, campaigns, Substitute.For<IRegistrationPaymentAttemptRepository>(), refunds,
            new FixedTimeProvider(Now)).RespondAsync(
            order, campaign.Id, "accept_new_terms", Guid.CreateVersion7(), CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("material_change_choice_invalid");
        await refunds.DidNotReceiveWithAnyArgs().ReserveAndRecordMaterialChangeRefundAsync(
            default!, default, default, default, default!, default);
    }

    [Test]
    public async Task RefundAfterAcceptedChoiceReturnsTypedConflictWithoutOutboxMutation()
    {
        RegistrationOrder order = Order();
        PaymentAttempt payment = CapturedPayment(order.Id);
        RefundCampaign campaign = RefundCampaign.CreateMaterialChange(
            Guid.CreateVersion7(), TenantId, order.EventId, Guid.CreateVersion7(), "Schedule changed.", Now);
        RegistrationMaterialChangeChoice choice = RegistrationMaterialChangeChoice.Create(
            Guid.CreateVersion7(), campaign, payment, Now);
        choice.AcceptNewTerms(Guid.CreateVersion7(), Now);
        var choices = Substitute.For<IRegistrationMaterialChangeChoiceRepository>();
        var campaigns = Substitute.For<IRefundCampaignRepository>();
        var payments = Substitute.For<IRegistrationPaymentAttemptRepository>();
        var refunds = Substitute.For<IRefundAttemptRepository>();
        choices.GetAsync(TenantId, campaign.Id, order.Id, Arg.Any<CancellationToken>()).Returns(choice);
        campaigns.GetByIdAsync(TenantId, campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        payments.GetLatestByOrderAsync(TenantId, order.Id, Arg.Any<CancellationToken>())
            .Returns((payment, CheckoutDispatchEffect.Create(payment, Now)));
        refunds.GetRefundableCapacityAsync(TenantId, payment.Id, Arg.Any<CancellationToken>()).Returns(1_000);
        refunds.ReserveAndRecordMaterialChangeRefundAsync(
                Arg.Any<RefundAttempt>(), choice.Id, Arg.Any<Guid>(), Now,
                Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(new RefundReservationResult(RefundReservationDisposition.MaterialChangeChoiceConflict, null));

        var result = await new RegistrationMaterialChangeChoiceService(
            choices, campaigns, payments, refunds, new FixedTimeProvider(Now)).RespondAsync(
            order, campaign.Id, "request_refund", Guid.CreateVersion7(), CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("material_change_choice_invalid");
        await campaigns.DidNotReceive().RefreshOutcomeCountersAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    private static RegistrationOrder Order() => RegistrationOrder.Create(
        Guid.CreateVersion7(), TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
        BookingPartyTypeEnum.Individual, Guid.CreateVersion7(),
        RegistrationParticipationSnapshot.Create(
            Guid.CreateVersion7(), 1, 1, 1, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
        null, null, "EUR", Now.AddHours(-1), Now.AddMinutes(30));

    private static PaymentAttempt CapturedPayment(Guid orderId)
    {
        OrganizerPaymentRecipientSnapshot recipient = OrganizerPaymentRecipientSnapshot.Create(
            TenantId, Guid.CreateVersion7(), Guid.CreateVersion7(), "stripe", "platform-live-eu", "acct_original",
            "BE", "EUR", Guid.CreateVersion7(), null, Now.AddMinutes(-2));
        PaymentAttempt payment = PaymentAttempt.Create(
            Guid.CreateVersion7(), TenantId, orderId, recipient, "OrganizerDirect", "2026-08-20.acacia", "composition-1",
            Money.Create(1_000, recipient.CurrencyCode), Money.Create(75, recipient.CurrencyCode), Money.Create(0, recipient.CurrencyCode), "payment:material-change", Now.AddMinutes(-2), Now.AddMinutes(30));
        payment.AttachAcceptance(RefundTestAcceptance.Create(
            TenantId, orderId, 1_000, 75, 0, Now.AddMinutes(-3)));
        payment.MarkSucceeded("pi_material", Now.AddMinutes(-1), "req_payment");
        return payment;
    }

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
