// ABOUTME: Verifies payment checkout targets require an exact current open unpaid provider session.
// ABOUTME: Rejects stale, completed, paid, expired, mismatched-money, and mismatched-session observations.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Payments;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.ValueObjects;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class RegistrationPaymentContractServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _orderId = Guid.CreateVersion7();
    private readonly IRegistrationPaymentAttemptRepository _attempts = Substitute.For<IRegistrationPaymentAttemptRepository>();
    private readonly IRegistrationFinalizationRepository _finalization = Substitute.For<IRegistrationFinalizationRepository>();
    private readonly IHostedCheckoutSessionRetriever _retriever = Substitute.For<IHostedCheckoutSessionRetriever>();
    private readonly IPaidOrderAcceptanceFreshnessService _freshness = Substitute.For<IPaidOrderAcceptanceFreshnessService>();
    private readonly IRefundAttemptRepository _refunds = Substitute.For<IRefundAttemptRepository>();

    [Test]
    public async Task ResolveCheckoutTargetAsync_RequiresExactOpenUnpaidUnexpiredMatchingSession()
    {
        _finalization.GetSucceededPaymentAsync(_tenantId, _orderId, Arg.Any<CancellationToken>())
            .Returns(SucceededPaymentLookupResult.Missing());
        RegistrationOrder order = CreateOrder();
        PaymentAttempt attempt = CreateRequiresActionAttempt();
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(attempt, UtcNow);
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>()).Returns((attempt, effect));
        RegistrationPaymentContractService service = CreateService();

        HostedCheckoutSession valid = new(
            "cs_exact",
            new Uri("https://checkout.stripe.com/c/pay/cs_exact"),
            HostedCheckoutSessionStatus.Open,
            HostedCheckoutPaymentStatus.Unpaid,
            null,
            UtcNow.AddMinutes(10),
            1_125,
            "EUR");
        _retriever.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(valid, null));

        var resolved = await service.ResolveCheckoutTargetAsync(order, CancellationToken.None);

        await Assert.That(resolved?.Url).IsEqualTo(valid.HostedUrl!.AbsoluteUri);

        HostedCheckoutSession[] rejected =
        [
            valid with { SessionId = "cs_other" },
            valid with { Status = HostedCheckoutSessionStatus.Complete },
            valid with { Status = HostedCheckoutSessionStatus.Expired },
            valid with { PaymentStatus = HostedCheckoutPaymentStatus.Paid },
            valid with { ExpiresAt = UtcNow },
            valid with { ExpiresAt = null },
            valid with { AmountTotalMinor = 1_201 },
            valid with { CurrencyCode = "USD" }
        ];

        foreach (HostedCheckoutSession session in rejected)
        {
            _retriever.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
                .Returns(HostedCheckoutRetrieveResult.Succeeded(session, null));
            await Assert.That(await service.ResolveCheckoutTargetAsync(order, CancellationToken.None)).IsNull();
        }

        RegistrationOrder expiredOrder = CreateOrder(UtcNow.AddMinutes(-1));
        _retriever.RetrieveAsync(Arg.Any<HostedCheckoutRetrieveRequest>(), Arg.Any<CancellationToken>())
            .Returns(HostedCheckoutRetrieveResult.Succeeded(valid, null));

        var expiredStart = await service.StartAsync(expiredOrder, null, CancellationToken.None);
        RegistrationPaymentDto? expiredRedirectStatus = await service.GetAsync(expiredOrder, CancellationToken.None);

        await Assert.That(expiredStart.IsSuccess).IsFalse();
        await Assert.That(expiredStart.FailureCode).IsEqualTo("not_payable");
        await Assert.That(expiredRedirectStatus!.HostedRedirectAvailable).IsFalse();
        await Assert.That(await service.ResolveCheckoutTargetAsync(expiredOrder, CancellationToken.None)).IsNull();

        PaymentAttempt retryAttempt = CreateRequiresActionAttempt();
        typeof(PaymentAttempt).GetProperty(nameof(PaymentAttempt.ProviderCheckoutSessionId))!.SetValue(retryAttempt, null);
        typeof(PaymentAttempt).GetProperty(nameof(PaymentAttempt.PaymentAttemptStatusId))!
            .SetValue(retryAttempt, (int)PaymentAttemptStatusEnum.DispatchPending);
        CheckoutDispatchEffect retryEffect = CheckoutDispatchEffect.Create(retryAttempt, UtcNow);
        typeof(CheckoutDispatchEffect).GetProperty(nameof(CheckoutDispatchEffect.Status))!
            .SetValue(retryEffect, OutboxMessageStatus.DeadLettered);
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>()).Returns((retryAttempt, retryEffect));

        RegistrationPaymentDto? expiredRetryStatus = await service.GetAsync(expiredOrder, CancellationToken.None);

        await Assert.That(expiredRetryStatus!.RetryAvailable).IsFalse();
    }

    [Test]
    public async Task GetAsync_WhenSucceededObservationsConflictSurfacesNeedsReconciliation()
    {
        RegistrationOrder order = CreateOrder();
        PaymentAttempt attempt = CreateRequiresActionAttempt();
        attempt.MarkSucceededFromCheckout("cs_exact", "pi_second", UtcNow.AddSeconds(3), null);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(attempt, UtcNow);
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>()).Returns((attempt, effect));
        _finalization.GetSucceededPaymentAsync(_tenantId, _orderId, Arg.Any<CancellationToken>())
            .Returns(SucceededPaymentLookupResult.Conflict());

        RegistrationPaymentDto? payment = await CreateService().GetAsync(order, CancellationToken.None);

        await Assert.That(payment!.StatusCode).IsEqualTo("NeedsReconciliation");
        await Assert.That(payment.FailureCode).IsEqualTo("payment_duplicate_succeeded_observations");
    }

    [Test]
    public async Task RetryAsync_ReplaysOnlyCreatedPendingReplacement()
    {
        RegistrationOrder order = CreateOrder();
        PaymentAttempt replacement = CreateRequiresActionAttempt();
        typeof(PaymentAttempt).GetProperty(nameof(PaymentAttempt.ProviderCheckoutSessionId))!.SetValue(replacement, null);
        typeof(PaymentAttempt).GetProperty(nameof(PaymentAttempt.PaymentAttemptStatusId))!
            .SetValue(replacement, (int)PaymentAttemptStatusEnum.Created);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(replacement, UtcNow);
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>()).Returns((replacement, effect));
        _freshness.IsCurrentAsync(replacement, Arg.Any<CancellationToken>()).Returns(true);

        RegistrationPaymentCommandResultDto replay = await CreateService().RetryAsync(order, CancellationToken.None);
        typeof(PaymentAttempt).GetProperty(nameof(PaymentAttempt.PaymentAttemptStatusId))!
            .SetValue(replacement, (int)PaymentAttemptStatusEnum.Unknown);
        RegistrationPaymentCommandResultDto unknown = await CreateService().RetryAsync(order, CancellationToken.None);

        await Assert.That(replay.IsSuccess).IsTrue();
        await Assert.That(replay.Payment!.Id).IsEqualTo(replacement.Id);
        await Assert.That(unknown.IsSuccess).IsFalse();
        await Assert.That(unknown.FailureCode).IsEqualTo("payment_retry_not_available");
    }

    [Test]
    public async Task RetryAsync_RejectsStaleAcceptanceBeforeAnyRetryMutation()
    {
        RegistrationOrder order = CreateOrder();
        PaymentAttempt attempt = CreateRequiresActionAttempt();
        typeof(PaymentAttempt).GetProperty(nameof(PaymentAttempt.ProviderCheckoutSessionId))!.SetValue(attempt, null);
        typeof(PaymentAttempt).GetProperty(nameof(PaymentAttempt.PaymentAttemptStatusId))!
            .SetValue(attempt, (int)PaymentAttemptStatusEnum.Created);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(attempt, UtcNow);
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>()).Returns((attempt, effect));
        _freshness.IsCurrentAsync(attempt, Arg.Any<CancellationToken>()).Returns(false);

        RegistrationPaymentCommandResultDto result = await CreateService().RetryAsync(order, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("payment_acceptance_stale");
        await _attempts.DidNotReceive().RetryParkedPreHandoffAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAsync_UsesRefundedLabelOnlyForProviderProvenSuccess()
    {
        RegistrationOrder order = CreateOrder();
        PaymentAttempt payment = CreateRequiresActionAttempt();
        payment.MarkSucceeded("pi_refund_status", UtcNow.AddSeconds(3), "req_paid");
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(payment, UtcNow);
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>()).Returns((payment, effect));
        _finalization.GetSucceededPaymentAsync(_tenantId, _orderId, Arg.Any<CancellationToken>())
            .Returns(SucceededPaymentLookupResult.Missing());
        RefundAttempt pending = RefundAttempt.Create(
            Guid.CreateVersion7(), _tenantId, payment.Id, payment.AcceptanceSnapshot!, "acct_123",
            "pi_refund_status", "refund:pending", 100, UtcNow.AddMinutes(1));
        pending.MarkDispatchPending(UtcNow.AddMinutes(1).AddSeconds(1), null);
        pending.MarkPending("re_pending", UtcNow.AddMinutes(1).AddSeconds(2), null);
        RefundAttempt succeeded = RefundAttempt.Create(
            Guid.CreateVersion7(), _tenantId, payment.Id, payment.AcceptanceSnapshot!, "acct_123",
            "pi_refund_status", "refund:succeeded", 200, UtcNow.AddMinutes(2));
        succeeded.MarkSucceeded("re_succeeded", UtcNow.AddMinutes(2).AddSeconds(1), null);
        RegistrationPaymentContractService service = CreateService();
        _refunds.GetByPaymentAsync(_tenantId, payment.Id, Arg.Any<CancellationToken>()).Returns([pending, succeeded]);
        _refunds.GetDisputesAsync(_tenantId, payment.Id, Arg.Any<CancellationToken>()).Returns([]);

        RegistrationPaymentDto? result = await service.GetAsync(order, CancellationToken.None);

        await Assert.That(result!.Refunds.Single(value => value.Id == pending.Id).StatusName).IsEqualTo("Pending");
        await Assert.That(result.Refunds.Single(value => value.Id == succeeded.Id).StatusName).IsEqualTo("Refunded");
        await Assert.That(result.RefundedAmountMinor).IsEqualTo(200);
        await Assert.That(result.RefundPendingAmountMinor).IsEqualTo(100);
    }

    [Test]
    public async Task GetAsync_DoesNotAdvertiseRefundOrCapturedMoneyBeforeProviderProof()
    {
        RegistrationOrder order = CreateOrder();
        PaymentAttempt attempt = CreateRequiresActionAttempt();
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(attempt, UtcNow);
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>()).Returns((attempt, effect));
        _finalization.GetSucceededPaymentAsync(_tenantId, _orderId, Arg.Any<CancellationToken>())
            .Returns(SucceededPaymentLookupResult.Missing());

        RegistrationPaymentDto? result = await CreateService().GetAsync(
            order, CancellationToken.None, buyerRefundAllowed: true, organizerRefundAllowed: true);

        await Assert.That(result!.CapturedAmountMinor).IsEqualTo(0);
        await Assert.That(result.BuyerRefundRequestAvailable).IsFalse();
        await Assert.That(result.OrganizerRefundAvailable).IsFalse();
    }

    [Test]
    public async Task PublicBaseUrl_NormalizesSubpathAndRejectsAuthorityContamination()
    {
        bool valid = HostedCheckoutReturnUrls.TryNormalizePublicBaseUrl("https://events.example.test/events", out Uri normalized);
        bool query = HostedCheckoutReturnUrls.TryNormalizePublicBaseUrl("https://events.example.test/events?x=1", out _);
        bool fragment = HostedCheckoutReturnUrls.TryNormalizePublicBaseUrl("https://events.example.test/events#x", out _);
        bool userInfo = HostedCheckoutReturnUrls.TryNormalizePublicBaseUrl("https://user@events.example.test/events", out _);

        await Assert.That(valid).IsTrue();
        await Assert.That(normalized.AbsoluteUri).IsEqualTo("https://events.example.test/events/");
        await Assert.That(query).IsFalse();
        await Assert.That(fragment).IsFalse();
        await Assert.That(userInfo).IsFalse();
    }

    private RegistrationPaymentContractService CreateService()
    {
        _refunds.GetByPaymentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        _refunds.GetDisputesAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        var materialChangeChoices = Substitute.For<IRegistrationMaterialChangeChoiceRepository>();
        materialChangeChoices.GetByPaymentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        var claimFreshness = Substitute.For<IPaidOrderAcceptanceFreshnessService>();
        claimFreshness.IsCurrentAsync(Arg.Any<PaymentAttempt>(), Arg.Any<CancellationToken>()).Returns(true);
        var claimService = new RegistrationPaymentAttemptClaimService(
            _attempts,
            Substitute.For<IRegistrationInventoryRepository>(),
            Substitute.For<IEventRepository>(),
            Substitute.For<IOrganizerPaymentProviderConnectionRepository>(),
            Substitute.For<IPaidEventPolicyRepository>(),
            Substitute.For<IOrganizerPaymentCommerceConfiguration>(),
            Substitute.For<IPaymentProviderDescriptor>(),
            Substitute.For<IPaidCheckoutActivationService>(),
            claimFreshness,
            new InlineUnitOfWork());
        var acceptanceService = new PaidOrderAcceptanceService(
            Substitute.For<IEventTicketCatalogRepository>(),
            Substitute.For<IEventRepository>(),
            Substitute.For<IPaidEventPolicyRepository>(),
            ReadyInstanceIdentity(),
            ReadyGovernance(),
            Substitute.For<ITenantDirectoryOperatorReadinessEvaluator>(),
            Substitute.For<IOrganizerPaymentProviderConnectionRepository>(),
            Substitute.For<IOrganizerPaymentCommerceConfiguration>(),
            Substitute.For<IPaidCheckoutActivationService>(),
            Substitute.For<IPaymentProviderDescriptor>(),
            new FixedTimeProvider(UtcNow));
        return new(
            _attempts,
            _finalization,
            claimService,
            acceptanceService,
            _freshness,
            _refunds,
            materialChangeChoices,
            _retriever,
            new FixedTimeProvider(UtcNow));
    }

    private RegistrationOrder CreateOrder(DateTime? expiresAt = null)
    {
        RegistrationOrder order = RegistrationOrder.Create(
            _orderId,
            _tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            BookingPartyTypeEnum.Individual,
            Guid.CreateVersion7(),
            RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 1, 1, 1, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            null,
            null,
            "EUR",
            UtcNow.AddHours(-2),
            expiresAt ?? UtcNow.AddMinutes(30));
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.RegistrationOrderStatusId))!
            .SetValue(order, (int)RegistrationOrderStatusEnum.AwaitingPayment);
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.TotalDueMinorSnapshot))!
            .SetValue(order, 1_125L);
        return order;
    }

    private static IPaidCheckoutGovernance ReadyGovernance()
    {
        var governance = Substitute.For<IPaidCheckoutGovernance>();
        governance.IsConfigured.Returns(true);
        governance.IsActivated.Returns(true);
        return governance;
    }

    private static IInstanceOperatorIdentity ReadyInstanceIdentity()
    {
        var identity = Substitute.For<IInstanceOperatorIdentity>();
        identity.OperatorId.Returns(Guid.CreateVersion7());
        identity.PublicName.Returns("Independent Operator");
        identity.IsOfficialInstance.Returns(false);
        identity.OfficialOrigin.Returns("https://events.example.test");
        identity.JurisdictionCountryCode.Returns("BE");
        identity.PublicContactEmail.Returns("contact@example.test");
        identity.WebsiteUrl.Returns("https://events.example.test");
        identity.LegalNoticeUrl.Returns("https://events.example.test/legal");
        identity.TermsUrl.Returns("https://events.example.test/terms");
        identity.PrivacyUrl.Returns("https://events.example.test/privacy");
        return identity;
    }

    private PaymentAttempt CreateRequiresActionAttempt()
    {
        OrganizerPaymentRecipientSnapshot recipient = OrganizerPaymentRecipientSnapshot.Create(
            _tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "stripe",
            "platform-live-eu",
            "acct_123",
            "BE",
            "EUR",
            Guid.CreateVersion7(),
            null,
            UtcNow);
        PaymentAttempt attempt = PaymentAttempt.Create(
            Guid.CreateVersion7(), _tenantId, _orderId, recipient, "OrganizerDirect", "2026-08-20.acacia",
            "composition-a", Money.Create(1_000, recipient.CurrencyCode), Money.Create(75, recipient.CurrencyCode), Money.Create(125, recipient.CurrencyCode), "checkout:key", UtcNow, UtcNow.AddMinutes(30));
        attempt.AttachAcceptance(PaidAcceptanceTestFacts.Create(
            _tenantId, _orderId, Guid.CreateVersion7(), "composition-a",
            recipient.InstancePolicyVersionId, recipient.TenantPolicyVersionId,
            1_000, 75, 125, UtcNow, recipient));
        attempt.MarkDispatchPending(UtcNow.AddSeconds(1), null);
        attempt.MarkRequiresAction("cs_exact", UtcNow.AddSeconds(2), null);
        return attempt;
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class InlineUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
    }
}
