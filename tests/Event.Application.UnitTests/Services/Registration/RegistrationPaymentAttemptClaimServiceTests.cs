// ABOUTME: Tests durable checkout attempt claim orchestration before provider dispatch exists.
// ABOUTME: Proves duplicate starts reuse the same attempt/effect and do not expose any provider I/O seam.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Payments;
using Explore.Application.Contracts.Services;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.ValueObjects;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class RegistrationPaymentAttemptClaimServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _orderId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IRegistrationPaymentAttemptRepository _attempts = Substitute.For<IRegistrationPaymentAttemptRepository>();
    private readonly IRegistrationInventoryRepository _orders = Substitute.For<IRegistrationInventoryRepository>();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IOrganizerPaymentProviderConnectionRepository _connections = Substitute.For<IOrganizerPaymentProviderConnectionRepository>();
    private readonly IPaidEventPolicyRepository _policies = Substitute.For<IPaidEventPolicyRepository>();
    private readonly IOrganizerPaymentCommerceConfiguration _commerce = Substitute.For<IOrganizerPaymentCommerceConfiguration>();
    private readonly IPaymentProviderDescriptor _descriptor = Substitute.For<IPaymentProviderDescriptor>();
    private readonly IPaidCheckoutActivationService _activation = Substitute.For<IPaidCheckoutActivationService>();
    private readonly IPaidOrderAcceptanceFreshnessService _freshness =
        Substitute.For<IPaidOrderAcceptanceFreshnessService>();

    [Test]
    public async Task ClaimAsyncWhenActiveAttemptExistsReturnsExistingAttemptAndEffectWithoutCreatingAnother()
    {
        PaymentAttempt attempt = CreateAttempt(PaymentAttemptStatusEnum.DispatchPending);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(attempt, UtcNow);
        ConfigureCurrentReadiness(attempt);
        _attempts.GetActiveByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>())
            .Returns((attempt, effect));
        RegistrationOrder order = CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, totalDueMinor: 1_125);
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, AcceptanceSnapshot: attempt.AcceptanceSnapshot), CancellationToken.None);

        await Assert.That(result.Created).IsFalse();
        await Assert.That(result.Attempt!.Id).IsEqualTo(attempt.Id);
        await Assert.That(result.DispatchEffect!.Id).IsEqualTo(effect.Id);
        await _attempts.DidNotReceive().ClaimAsync(Arg.Any<RegistrationPaymentAttemptClaim>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClaimAsyncNeverBackfillsHistoricalAttemptWithoutAcceptance()
    {
        PaymentAttempt historical = CreateAttempt(PaymentAttemptStatusEnum.Created, withAcceptance: false);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(historical, UtcNow);
        PaymentAttempt acceptanceSource = CreateAttempt(PaymentAttemptStatusEnum.Created);
        ConfigureCurrentReadiness(acceptanceSource);
        _attempts.GetActiveByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>()).Returns((historical, effect));
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, 1_125));

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(
                _tenantId,
                _orderId,
                UtcNow,
                AcceptanceSnapshot: acceptanceSource.AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("payment_acceptance_required");
        await Assert.That(historical.PaidOrderAcceptanceSnapshotId).IsNull();
        await _attempts.DidNotReceive().ClaimAsync(Arg.Any<RegistrationPaymentAttemptClaim>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClaimAsyncRejectsSnapshotThatFailsCanonicalFreshness()
    {
        PaymentAttempt acceptanceSource = CreateAttempt(PaymentAttemptStatusEnum.Created);
        ConfigureCurrentReadiness(acceptanceSource);
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, 1_125));

        RegistrationPaymentAttemptClaimResult result = await CreateService(acceptanceCurrent: false).ClaimAsync(
            new(
                _tenantId,
                _orderId,
                UtcNow,
                AcceptanceSnapshot: acceptanceSource.AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("payment_acceptance_stale");
        await _attempts.DidNotReceive().ClaimAsync(
            Arg.Any<RegistrationPaymentAttemptClaim>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(nameof(PaidOrderAcceptanceSnapshot.OrganizerPaymentProviderConnectionId))]
    [Arguments(nameof(PaidOrderAcceptanceSnapshot.ConnectPlatformId))]
    [Arguments(nameof(PaidOrderAcceptanceSnapshot.ExternalAccountId))]
    [Arguments(nameof(PaidOrderAcceptanceSnapshot.MerchantCountryCode))]
    public async Task ClaimAsyncFencesEveryAcceptedRecipientFactBeforePersistence(string propertyName)
    {
        PaymentAttempt acceptanceSource = CreateAttempt(PaymentAttemptStatusEnum.Created);
        ConfigureCurrentReadiness(acceptanceSource);
        PaidOrderAcceptanceSnapshot acceptance = acceptanceSource.AcceptanceSnapshot!;
        object changed = propertyName == nameof(PaidOrderAcceptanceSnapshot.OrganizerPaymentProviderConnectionId)
            ? Guid.CreateVersion7()
            : propertyName == nameof(PaidOrderAcceptanceSnapshot.MerchantCountryCode) ? "FR" : "changed";
        typeof(PaidOrderAcceptanceSnapshot).GetProperty(propertyName)!.SetValue(acceptance, changed);
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, 1_125));

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, AcceptanceSnapshot: acceptance), CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("payment_acceptance_stale");
        await Assert.That(result.Attempt).IsNull();
        await Assert.That(result.DispatchEffect).IsNull();
        await _attempts.DidNotReceive().ClaimAsync(
            Arg.Any<RegistrationPaymentAttemptClaim>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClaimAsyncRejectsNonPayableOrZeroTotalOrders()
    {
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateOrder(RegistrationOrderStatusEnum.ReadyForCheckout, totalDueMinor: 1_125));

        RegistrationPaymentAttemptClaimResult wrongStatus = await CreateService().ClaimAsync(new(_tenantId, _orderId, UtcNow), CancellationToken.None);
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, totalDueMinor: 0));
        RegistrationPaymentAttemptClaimResult zeroTotal = await CreateService().ClaimAsync(new(_tenantId, _orderId, UtcNow), CancellationToken.None);

        await Assert.That(wrongStatus.Success).IsFalse();
        await Assert.That(zeroTotal.Success).IsFalse();
        await _attempts.DidNotReceive().ClaimAsync(Arg.Any<RegistrationPaymentAttemptClaim>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClaimAsyncRejectsExpiredAwaitingPaymentOrder()
    {
        RegistrationOrder order = CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, totalDueMinor: 1_125);
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.ExpiresAt))!
            .SetValue(order, UtcNow.AddSeconds(-1));
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("not_payable");
        await _attempts.DidNotReceive().ClaimAsync(Arg.Any<RegistrationPaymentAttemptClaim>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClaimAsyncDurableStopSaleBlocksBeforeRecipientOrAttemptCreation()
    {
        RegistrationOrder order = CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, 1_125);
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _activation.EvaluateAsync(Arg.Any<PaidCheckoutActivationRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaidCheckoutActivationResult.Failure("paid_sale_stopped", "stopped"));
        RegistrationPaymentAttemptClaimService service = CreateService();
        _activation.EvaluateAsync(Arg.Any<PaidCheckoutActivationRequest>(), Arg.Any<CancellationToken>())
            .Returns(PaidCheckoutActivationResult.Failure("paid_sale_stopped", "stopped"));

        RegistrationPaymentAttemptClaimResult result = await service.ClaimAsync(
            new(_tenantId, _orderId, UtcNow, AcceptanceSnapshot: CreateAttempt(PaymentAttemptStatusEnum.Created).AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("paid_sale_stopped");
        await _attempts.DidNotReceiveWithAnyArgs().ClaimAsync(default!, default);
        await _connections.DidNotReceiveWithAnyArgs().GetActiveByScopeAsync(default, default, default!, default!, default);
    }

    [Test]
    public async Task ClaimAsyncWhenHistoricalSameCompositionExistsReturnsItWithoutUniqueIndexRetry()
    {
        PaymentAttempt attempt = CreateAttempt(PaymentAttemptStatusEnum.Created);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(attempt, UtcNow);
        ConfigureCurrentReadiness(attempt);
        _attempts.GetByOrderCompositionAsync(_tenantId, _orderId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((attempt, effect));
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, totalDueMinor: 1_125));

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, AcceptanceSnapshot: attempt.AcceptanceSnapshot), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Created).IsFalse();
        await Assert.That(result.Attempt!.Id).IsEqualTo(attempt.Id);
        await _attempts.DidNotReceive().ClaimAsync(Arg.Any<RegistrationPaymentAttemptClaim>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClaimAsyncReturnsTypedUnavailableForMissingActorPolicyAndPlatform()
    {
        RegistrationOrder order = CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, 1_125);
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _events.GetEventWithDetailsAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(EventTarget(organizerActorId: null));
        PaidOrderAcceptanceSnapshot acceptance =
            CreateAttempt(PaymentAttemptStatusEnum.Created).AcceptanceSnapshot!;

        RegistrationPaymentAttemptClaimResult missingActor = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, AcceptanceSnapshot: acceptance), CancellationToken.None);

        _events.GetEventWithDetailsAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(EventTarget(Guid.CreateVersion7()));
        RegistrationPaymentAttemptClaimResult missingPolicy = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, AcceptanceSnapshot: acceptance), CancellationToken.None);

        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(EnabledPolicy());
        RegistrationPaymentAttemptClaimResult missingPlatform = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, AcceptanceSnapshot: acceptance), CancellationToken.None);

        await Assert.That(missingActor.FailureCode).IsEqualTo("payment_organizer_unavailable");
        await Assert.That(missingPolicy.FailureCode).IsEqualTo("payment_configuration_unavailable");
        await Assert.That(missingPlatform.FailureCode).IsEqualTo("payment_configuration_unavailable");
    }

    [Test]
    public async Task ClaimAsyncPropagatesTypedConnectionStateCurrencyAndStalenessFailures()
    {
        Guid organizerActorId = Guid.CreateVersion7();
        _events.GetEventWithDetailsAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(EventTarget(organizerActorId));
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(EnabledPolicy());
        _commerce.ProviderCode.Returns("stripe");
        _commerce.ConnectPlatformId.Returns("platform-live-eu");
        OrganizerPaymentProviderConnection pending = Connection(organizerActorId);
        OrganizerPaymentProviderConnection restricted = Connection(organizerActorId);
        restricted.ApplyReadiness(OrganizerPaymentProviderReadinessObservation.Create(
            "BE", ChargeCapabilityState.Pending, ProviderRequirementsState.CurrentlyDue, ["EUR"], UtcNow.AddMinutes(-1), "restricted"));
        OrganizerPaymentProviderConnection disabled = ReadyConnection(organizerActorId, ["EUR"], UtcNow.AddMinutes(-1));
        disabled.Disable("operator_disabled", UtcNow);
        OrganizerPaymentProviderConnection replaced = ReadyConnection(organizerActorId, ["EUR"], UtcNow.AddMinutes(-1));
        _ = replaced.ReplaceWith(Guid.CreateVersion7(), "acct_new", UtcNow);
        OrganizerPaymentProviderConnection unsupportedCurrency = ReadyConnection(organizerActorId, ["USD"], UtcNow.AddMinutes(-1));
        OrganizerPaymentProviderConnection stale = ReadyConnection(organizerActorId, ["EUR"], UtcNow.AddMinutes(-10));
        OrganizerPaymentProviderConnection[] connections = [pending, restricted, disabled, replaced, unsupportedCurrency, stale];
        string[] expected =
        [
            "payment_connection_pending",
            "payment_connection_restricted",
            "payment_connection_disabled",
            "payment_connection_replaced",
            "payment_currency_unsupported",
            "payment_readiness_stale"
        ];

        var actual = new List<string?>();
        PaidOrderAcceptanceSnapshot acceptance =
            CreateAttempt(PaymentAttemptStatusEnum.Created).AcceptanceSnapshot!;
        foreach (OrganizerPaymentProviderConnection connection in connections)
        {
            _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>())
                .Returns(CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, 1_125));
            _connections.GetActiveByScopeAsync(
                    _tenantId, organizerActorId, "stripe", "platform-live-eu", Arg.Any<CancellationToken>())
                .Returns(connection);
            RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
                new(_tenantId, _orderId, UtcNow, AcceptanceSnapshot: acceptance), CancellationToken.None);
            actual.Add(result.FailureCode);
        }

        await Assert.That(actual).IsEquivalentTo(expected);
    }

    [Test]
    public async Task ClaimAsyncRejectsMissingIdentityBeforeRepositoryAccess()
    {
        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(Guid.Empty, _orderId, UtcNow), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("validation_failed");
        await _orders.DidNotReceiveWithAnyArgs().GetOrderForUpdateWithLinesAsync(default, default, default);
    }

    [Test]
    public async Task ClaimAsyncRejectsNonUtcTimestampBeforeRepositoryAccess()
    {
        DateTime localTime = DateTime.SpecifyKind(UtcNow, DateTimeKind.Local);

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, localTime), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("validation_failed");
        await _orders.DidNotReceiveWithAnyArgs().GetOrderForUpdateWithLinesAsync(default, default, default);
    }

    [Test]
    public async Task ClaimAsyncRejectsAcceptanceFromAnotherTenantBeforeReadinessLookup()
    {
        PaymentAttempt acceptanceSource = CreateAttempt(PaymentAttemptStatusEnum.Created);
        typeof(PaidOrderAcceptanceSnapshot).GetProperty(nameof(PaidOrderAcceptanceSnapshot.TenantId))!
            .SetValue(acceptanceSource.AcceptanceSnapshot, Guid.CreateVersion7());
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, 1_125));

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, AcceptanceSnapshot: acceptanceSource.AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("payment_acceptance_stale");
        await _events.DidNotReceiveWithAnyArgs().GetEventWithDetailsAsync(default, default, default);
    }

    [Test]
    public async Task ClaimAsyncRejectsAcceptanceForAnotherEventBeforeReadinessLookup()
    {
        PaymentAttempt acceptanceSource = CreateAttempt(PaymentAttemptStatusEnum.Created);
        typeof(PaidOrderAcceptanceSnapshot).GetProperty(nameof(PaidOrderAcceptanceSnapshot.EventId))!
            .SetValue(acceptanceSource.AcceptanceSnapshot, Guid.CreateVersion7());
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, 1_125));

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, AcceptanceSnapshot: acceptanceSource.AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("payment_acceptance_stale");
        await _events.DidNotReceiveWithAnyArgs().GetEventWithDetailsAsync(default, default, default);
    }

    [Test]
    public async Task ClaimAsyncReturnsSafelyQueuedCreatedPendingReplacement()
    {
        PaymentAttempt replacement = CreateAttempt(PaymentAttemptStatusEnum.Created);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(replacement, UtcNow);
        ConfigureCurrentReadiness(replacement);
        ConfigurePayableOrder();
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>())
            .Returns((replacement, effect));

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, Guid.CreateVersion7(), replacement.AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Created).IsFalse();
        await Assert.That(result.Attempt).IsSameReferenceAs(replacement);
        await Assert.That(result.DispatchEffect).IsSameReferenceAs(effect);
        await _attempts.DidNotReceiveWithAnyArgs().ReleaseActiveSlotAsync(default!, default, default);
    }

    [Test]
    public async Task ClaimAsyncRejectsCreatedReplacementWhoseEffectIsFailed()
    {
        PaymentAttempt replacement = CreateAttempt(PaymentAttemptStatusEnum.Created);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(replacement, UtcNow);
        typeof(CheckoutDispatchEffect).GetProperty(nameof(CheckoutDispatchEffect.Status))!
            .SetValue(effect, OutboxMessageStatus.Failed);
        ConfigureCurrentReadiness(replacement);
        ConfigurePayableOrder();
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>())
            .Returns((replacement, effect));

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, Guid.CreateVersion7(), replacement.AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("payment_retry_not_available");
        await _attempts.DidNotReceiveWithAnyArgs().ReleaseActiveSlotAsync(default!, default, default);
    }

    [Test]
    public async Task ClaimAsyncRejectsDispatchPendingReplacementWhoseEffectIsPending()
    {
        PaymentAttempt replacement = CreateAttempt(PaymentAttemptStatusEnum.DispatchPending);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(replacement, UtcNow);
        ConfigureCurrentReadiness(replacement);
        ConfigurePayableOrder();
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>())
            .Returns((replacement, effect));

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, Guid.CreateVersion7(), replacement.AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("payment_retry_not_available");
    }

    [Test]
    public async Task ClaimAsyncRejectsRetryWhenLatestAttemptIsMissing()
    {
        PaymentAttempt acceptanceSource = CreateAttempt(PaymentAttemptStatusEnum.Created);
        ConfigureCurrentReadiness(acceptanceSource);
        ConfigurePayableOrder();

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, Guid.CreateVersion7(), acceptanceSource.AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("payment_retry_not_available");
        await _attempts.DidNotReceiveWithAnyArgs().ReleaseActiveSlotAsync(default!, default, default);
    }

    [Test]
    public async Task ClaimAsyncRejectsMatchingNonTerminalRetryWithoutReleasingSlot()
    {
        PaymentAttempt latest = CreateAttempt(PaymentAttemptStatusEnum.Created);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(latest, UtcNow);
        ConfigureCurrentReadiness(latest);
        ConfigurePayableOrder();
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>())
            .Returns((latest, effect));

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, latest.Id, latest.AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("payment_retry_not_available");
        await _attempts.DidNotReceiveWithAnyArgs().ReleaseActiveSlotAsync(default!, default, default);
    }

    [Test]
    public async Task ClaimAsyncDoesNotProceedWhenTerminalSlotReleaseFails()
    {
        PaymentAttempt latest = CreateTerminalAttempt(PaymentAttemptStatusEnum.Failed);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(latest, UtcNow);
        ConfigureCurrentReadiness(latest);
        ConfigurePayableOrder();
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>())
            .Returns((latest, effect));
        _attempts.ReleaseActiveSlotAsync(latest, UtcNow, Arg.Any<CancellationToken>()).Returns(false);

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, latest.Id, latest.AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("payment_retry_not_available");
        await _attempts.DidNotReceiveWithAnyArgs().ClaimAsync(default!, default);
    }

    [Test]
    public async Task ClaimAsyncCreatesReplacementWhenTerminalSlotWasAlreadyReleased()
    {
        DateTime requestedAt = UtcNow.AddSeconds(3);
        PaymentAttempt latest = CreateTerminalAttempt(
            PaymentAttemptStatusEnum.Failed);
        _ = latest.TryReleaseActiveSlot(UtcNow.AddSeconds(2));
        CheckoutDispatchEffect effect =
            CheckoutDispatchEffect.Create(latest, UtcNow);
        ConfigureCurrentReadiness(latest);
        ConfigurePayableOrder();
        _attempts.GetLatestByOrderAsync(
                _tenantId,
                _orderId,
                Arg.Any<CancellationToken>())
            .Returns((latest, effect));
        _attempts.ClaimAsync(
                Arg.Any<RegistrationPaymentAttemptClaim>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                RegistrationPaymentAttemptClaim claim =
                    call.Arg<RegistrationPaymentAttemptClaim>();
                return new RegistrationPaymentAttemptClaimOutcome(
                    claim.Attempt,
                    claim.DispatchEffect,
                    true);
            });

        RegistrationPaymentAttemptClaimResult result =
            await CreateService().ClaimAsync(
                new(
                    _tenantId,
                    _orderId,
                    requestedAt,
                    latest.Id,
                    latest.AcceptanceSnapshot),
                CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Created).IsTrue();
        await Assert.That(result.Attempt!.AcceptanceSnapshot)
            .IsSameReferenceAs(latest.AcceptanceSnapshot);
        await _attempts.DidNotReceiveWithAnyArgs()
            .ReleaseActiveSlotAsync(default!, default, default);
        await _attempts.Received(1).ClaimAsync(
            Arg.Any<RegistrationPaymentAttemptClaim>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClaimAsyncCreatesFreshAcceptedAttemptAfterTerminalSlotRelease()
    {
        PaymentAttempt latest = CreateTerminalAttempt(PaymentAttemptStatusEnum.Failed);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(latest, UtcNow);
        ConfigureCurrentReadiness(latest);
        ConfigurePayableOrder();
        _attempts.GetLatestByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>())
            .Returns((latest, effect));
        _attempts.ReleaseActiveSlotAsync(latest, UtcNow, Arg.Any<CancellationToken>()).Returns(true);
        _attempts.ClaimAsync(Arg.Any<RegistrationPaymentAttemptClaim>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                RegistrationPaymentAttemptClaim claim = call.Arg<RegistrationPaymentAttemptClaim>();
                return new RegistrationPaymentAttemptClaimOutcome(claim.Attempt, claim.DispatchEffect, true);
            });

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow, latest.Id, latest.AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Created).IsTrue();
        await Assert.That(result.Attempt!.Id).IsNotEqualTo(latest.Id);
        await Assert.That(result.Attempt.HasImmutableAcceptance).IsTrue();
        await Assert.That(result.Attempt.AcceptanceSnapshot)
            .IsSameReferenceAs(latest.AcceptanceSnapshot);
        await Assert.That(result.Attempt.ProviderIdempotencyKey).IsEqualTo($"checkout:{result.Attempt.Id:N}");
        await _attempts.Received(1).ReleaseActiveSlotAsync(latest, UtcNow, Arg.Any<CancellationToken>());
        await _attempts.Received(1).ClaimAsync(Arg.Any<RegistrationPaymentAttemptClaim>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClaimAsyncRejectsStaleExistingActiveAttempt()
    {
        PaymentAttempt active = CreateAttempt(PaymentAttemptStatusEnum.Created);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(active, UtcNow);
        ConfigureCurrentReadiness(active);
        ConfigurePayableOrder();
        _attempts.GetActiveByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>()).Returns((active, effect));

        RegistrationPaymentAttemptClaimResult result = await CreateService(acceptanceCurrent: false).ClaimAsync(
            new(_tenantId, _orderId, UtcNow, AcceptanceSnapshot: active.AcceptanceSnapshot),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("payment_acceptance_stale");
        await _attempts.DidNotReceiveWithAnyArgs().ClaimAsync(default!, default);
    }

    private static PaidEventPolicyVersion EnabledPolicy()
    {
        PaidEventPolicyVersion disabled = PaidEventPolicyVersion.CreateDefaultInstance();
        return disabled.CreateRevision(
            true, disabled.AllowedOrganizerKinds, false, disabled.AllowedCurrencyCodes, "EUR",
            disabled.RefundProtections, [], false, null);
    }

    private RegistrationPaymentAttemptClaimService CreateService(bool acceptanceCurrent = true)
    {
        _descriptor.Describe().Returns(new PaymentProviderDescriptor(
            "stripe", "OrganizerDirect", "2026-07-29.dahlia", "test", "instance-operator"));
        _activation.EvaluateAsync(Arg.Any<PaidCheckoutActivationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaidCheckoutActivationResult(true, null, "active"));
        _freshness.IsCurrentAsync(Arg.Any<PaymentAttempt>(), Arg.Any<CancellationToken>())
            .Returns(acceptanceCurrent);
        return new(
            _attempts,
            _orders,
            _events,
            _connections,
            _policies,
            _commerce,
            _descriptor,
            _activation,
            _freshness,
            new InlineUnitOfWork());
    }

    private RegistrationOrder CreateOrder(RegistrationOrderStatusEnum status, long totalDueMinor)
    {
        RegistrationOrder order = RegistrationOrder.Create(
            _orderId,
            _tenantId,
            _eventId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            BookingPartyTypeEnum.Individual,
            Guid.CreateVersion7(),
            RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 1, 1, 1, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            null,
            null,
            "EUR",
            UtcNow,
            UtcNow.AddMinutes(15));
        SetStatus(order, status);
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.OrganizerDirectedTotalMinorSnapshot))!
            .SetValue(order, totalDueMinor == 0 ? 0L : 1_000L);
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.PlatformFeeTotalMinorSnapshot))!
            .SetValue(order, totalDueMinor == 0 ? 0L : 75L);
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.PlatformContributionTotalMinorSnapshot))!
            .SetValue(order, totalDueMinor == 0 ? 0L : 125L);
        SetTotal(order, totalDueMinor);
        typeof(RegistrationOrder).GetProperty(nameof(RegistrationOrder.ConcurrencyStamp))!
            .SetValue(order, Guid.Empty);
        return order;
    }

    private void ConfigurePayableOrder() =>
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, 1_125));

    private PaymentAttempt CreateTerminalAttempt(PaymentAttemptStatusEnum status)
    {
        PaymentAttempt attempt = CreateAttempt(PaymentAttemptStatusEnum.Created);
        if (status == PaymentAttemptStatusEnum.Failed)
        {
            _ = attempt.MarkDispatchFailed(UtcNow.AddSeconds(1), null);
        }
        else if (status == PaymentAttemptStatusEnum.Cancelled)
        {
            _ = attempt.MarkCancelled(UtcNow.AddSeconds(1), null);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return attempt;
    }

    private PaymentAttempt CreateAttempt(PaymentAttemptStatusEnum status, bool withAcceptance = true)
    {
        OrganizerPaymentRecipientSnapshot recipient = RecipientSnapshot();
        PaymentAttempt attempt = PaymentAttempt.Create(
            Guid.CreateVersion7(),
            _tenantId,
            _orderId,
            recipient,
            "OrganizerDirect",
            "2026-08-20.acacia",
            Guid.Empty.ToString("N"),
            Money.Create(1_000, recipient.CurrencyCode),
            Money.Create(75, recipient.CurrencyCode),
            Money.Create(125, recipient.CurrencyCode),
            "checkout:" + _tenantId.ToString("N") + ":" + _orderId.ToString("N") + ":abc",
            UtcNow,
            UtcNow.AddMinutes(30));
        if (withAcceptance)
        {
            attempt.AttachAcceptance(PaidAcceptanceTestFacts.Create(
                _tenantId, _orderId, _eventId, Guid.Empty.ToString("N"),
                attempt.RecipientSnapshot.InstancePolicyVersionId, attempt.RecipientSnapshot.TenantPolicyVersionId,
                1_000, 75, 125, UtcNow, recipient));
        }
        if (status == PaymentAttemptStatusEnum.DispatchPending)
        {
            attempt.MarkDispatchPending(UtcNow.AddSeconds(1), null);
        }

        return attempt;
    }

    private void ConfigureCurrentReadiness(PaymentAttempt attempt)
    {
        OrganizerPaymentRecipientSnapshot recipient = attempt.RecipientSnapshot;
        Guid organizerActorId = recipient.OrganizerActorId;
        _events.GetEventWithDetailsAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(EventTarget(organizerActorId));
        PaidEventPolicyVersion policy = EnabledPolicy();
        typeof(PaidEventPolicyVersion).GetProperty(nameof(PaidEventPolicyVersion.Id))!
            .SetValue(policy, attempt.RecipientSnapshot.InstancePolicyVersionId);
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(policy);
        _commerce.ProviderCode.Returns("stripe");
        _commerce.ConnectPlatformId.Returns("platform-live-eu");
        OrganizerPaymentProviderConnection connection = OrganizerPaymentProviderConnection.Create(
            recipient.OrganizerPaymentProviderConnectionId, _tenantId, organizerActorId,
            recipient.ProviderCode, recipient.ConnectPlatformId, recipient.ExternalAccountId,
            UtcNow.AddMinutes(-20));
        connection.ApplyReadiness(OrganizerPaymentProviderReadinessObservation.Create(
            recipient.MerchantCountryCode, ChargeCapabilityState.Active,
            ProviderRequirementsState.Satisfied, [recipient.CurrencyCode], UtcNow.AddMinutes(-1),
            $"rev-{Guid.CreateVersion7():N}"));
        _connections.GetActiveByScopeAsync(
            _tenantId, organizerActorId, "stripe", "platform-live-eu", Arg.Any<CancellationToken>()).Returns(connection);
    }

    private OrganizerPaymentRecipientSnapshot RecipientSnapshot() => OrganizerPaymentRecipientSnapshot.Create(
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

    private Explore.Domain.Event EventTarget(Guid? organizerActorId) => new(EventStatusEnum.Draft)
    {
        Id = _eventId,
        Title = "Payment readiness event",
        TenantId = _tenantId,
        OrganizerActorId = organizerActorId,
        Actor = null!,
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!
    };

    private OrganizerPaymentProviderConnection Connection(Guid organizerActorId) => OrganizerPaymentProviderConnection.Create(
        Guid.CreateVersion7(), _tenantId, organizerActorId, "stripe", "platform-live-eu", $"acct_{Guid.CreateVersion7():N}", UtcNow.AddMinutes(-20));

    private OrganizerPaymentProviderConnection ReadyConnection(
        Guid organizerActorId,
        string[] currencies,
        DateTime observedAt)
    {
        OrganizerPaymentProviderConnection connection = Connection(organizerActorId);
        connection.ApplyReadiness(OrganizerPaymentProviderReadinessObservation.Create(
            "BE", ChargeCapabilityState.Active, ProviderRequirementsState.Satisfied, currencies, observedAt, $"rev-{Guid.CreateVersion7():N}"));
        return connection;
    }

    private static void SetStatus(RegistrationOrder order, RegistrationOrderStatusEnum status) => typeof(RegistrationOrder)
        .GetProperty(nameof(RegistrationOrder.RegistrationOrderStatusId))!
        .SetValue(order, (int)status);

    private static void SetTotal(RegistrationOrder order, long totalDueMinor) => typeof(RegistrationOrder)
        .GetProperty(nameof(RegistrationOrder.TotalDueMinorSnapshot))!
        .SetValue(order, totalDueMinor);

    private sealed class InlineUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
    }
}
