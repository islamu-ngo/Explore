// ABOUTME: Specifies actor-audited refund reservation and atomic dispatch scheduling without provider I/O.
// ABOUTME: Proves caller idempotency input is hashed and captured accepted authority remains immutable.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.ValueObjects;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class RegistrationRefundServiceTests
{
    private static readonly Guid TenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");
    private static readonly DateTime Now = new(2026, 8, 25, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task InitiateAsyncReservesAndSchedulesAtomicallyWithoutPersistingCallerKey()
    {
        RegistrationOrder order = Order();
        PaymentAttempt payment = CapturedPayment(order.Id);
        var payments = Substitute.For<IRegistrationPaymentAttemptRepository>();
        var refunds = Substitute.For<IRefundAttemptRepository>();
        payments.GetLatestByOrderAsync(TenantId, order.Id, Arg.Any<CancellationToken>())
            .Returns((payment, CheckoutDispatchEffect.Create(payment, Now)));
        refunds.GetRefundableCapacityAsync(TenantId, payment.Id, Arg.Any<CancellationToken>()).Returns(1_000);
        refunds.ReserveAndScheduleAsync(
                Arg.Any<RefundAttempt>(), Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(call => new RefundReservationResult(RefundReservationDisposition.Reserved, call.Arg<RefundAttempt>()));

        var result = await new RegistrationRefundService(
            payments, refunds, new FixedTimeProvider(Now)).InitiateAsync(
            order, 400, "browser-key-must-not-be-stored", Guid.CreateVersion7(),
            "buyer", "event_cancelled", CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await refunds.Received(1).ReserveAndScheduleAsync(
            Arg.Is<RefundAttempt>(attempt =>
                attempt.Allocation.TotalMinor == 400 &&
                attempt.AuthorityCode == "buyer" &&
                attempt.ReasonCode == "event_cancelled" &&
                !attempt.ProviderIdempotencyKey.Contains("browser-key", StringComparison.Ordinal)),
            Arg.Is<OutboxMessage>(message => message.EventType == RefundOutboxMessageFactory.DispatchRequested),
            Arg.Any<CancellationToken>());
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
            Money.Create(1_000, recipient.CurrencyCode), Money.Create(75, recipient.CurrencyCode), Money.Create(0, recipient.CurrencyCode), "payment:refund", Now.AddMinutes(-2), Now.AddMinutes(30));
        payment.AttachAcceptance(RefundTestAcceptance.Create(
            TenantId, orderId, 1_000, 75, 0, Now.AddMinutes(-3)));
        payment.MarkSucceeded("pi_refund", Now.AddMinutes(-1), "req_payment");
        return payment;
    }

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
