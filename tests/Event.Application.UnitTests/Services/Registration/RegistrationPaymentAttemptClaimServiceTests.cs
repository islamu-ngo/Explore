// ABOUTME: Tests durable checkout attempt claim orchestration before provider dispatch exists.
// ABOUTME: Proves duplicate starts reuse the same attempt/effect and do not expose any provider I/O seam.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Payments;
using Explore.Application.Contracts.Services;
using Explore.Application.Services.Registration;
using Explore.Domain;
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

    [Test]
    public async Task ClaimAsyncWhenActiveAttemptExistsReturnsExistingAttemptAndEffectWithoutCreatingAnother()
    {
        PaymentAttempt attempt = CreateAttempt(PaymentAttemptStatusEnum.DispatchPending);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(attempt, UtcNow);
        _attempts.GetActiveByOrderAsync(_tenantId, _orderId, Arg.Any<CancellationToken>())
            .Returns((attempt, effect));
        RegistrationOrder order = CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, totalDueMinor: 1_125);
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow), CancellationToken.None);

        await Assert.That(result.Created).IsFalse();
        await Assert.That(result.Attempt!.Id).IsEqualTo(attempt.Id);
        await Assert.That(result.DispatchEffect!.Id).IsEqualTo(effect.Id);
        await _attempts.DidNotReceive().ClaimAsync(Arg.Any<RegistrationPaymentAttemptClaim>(), Arg.Any<CancellationToken>());
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
    public async Task ClaimAsyncWhenHistoricalSameCompositionExistsReturnsItWithoutUniqueIndexRetry()
    {
        PaymentAttempt attempt = CreateAttempt(PaymentAttemptStatusEnum.Created);
        CheckoutDispatchEffect effect = CheckoutDispatchEffect.Create(attempt, UtcNow);
        _attempts.GetByOrderCompositionAsync(_tenantId, _orderId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((attempt, effect));
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, totalDueMinor: 1_125));

        RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow), CancellationToken.None);

        await Assert.That(result.Created).IsFalse();
        await Assert.That(result.Attempt!.Id).IsEqualTo(attempt.Id);
        await _attempts.DidNotReceive().ClaimAsync(Arg.Any<RegistrationPaymentAttemptClaim>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClaimAsyncReturnsTypedUnavailableForMissingActorPolicyAndPlatform()
    {
        RegistrationOrder order = CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, 1_125);
        _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _events.GetEventWithDetails(_eventId).Returns(EventTarget(organizerActorId: null));

        RegistrationPaymentAttemptClaimResult missingActor = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow), CancellationToken.None);

        _events.GetEventWithDetails(_eventId).Returns(EventTarget(Guid.CreateVersion7()));
        RegistrationPaymentAttemptClaimResult missingPolicy = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow), CancellationToken.None);

        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(PaidEventPolicyVersion.CreateDefaultInstance());
        RegistrationPaymentAttemptClaimResult missingPlatform = await CreateService().ClaimAsync(
            new(_tenantId, _orderId, UtcNow), CancellationToken.None);

        await Assert.That(missingActor.FailureCode).IsEqualTo("payment_organizer_unavailable");
        await Assert.That(missingPolicy.FailureCode).IsEqualTo("payment_configuration_unavailable");
        await Assert.That(missingPlatform.FailureCode).IsEqualTo("payment_configuration_unavailable");
    }

    [Test]
    public async Task ClaimAsyncPropagatesTypedConnectionStateCurrencyAndStalenessFailures()
    {
        Guid organizerActorId = Guid.CreateVersion7();
        _events.GetEventWithDetails(_eventId).Returns(EventTarget(organizerActorId));
        _policies.GetActiveInstanceAsync(Arg.Any<CancellationToken>()).Returns(PaidEventPolicyVersion.CreateDefaultInstance());
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
        foreach (OrganizerPaymentProviderConnection connection in connections)
        {
            _orders.GetOrderForUpdateWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>())
                .Returns(CreateOrder(RegistrationOrderStatusEnum.AwaitingPayment, 1_125));
            _connections.GetActiveByScopeAsync(
                    _tenantId, organizerActorId, "stripe", "platform-live-eu", Arg.Any<CancellationToken>())
                .Returns(connection);
            RegistrationPaymentAttemptClaimResult result = await CreateService().ClaimAsync(
                new(_tenantId, _orderId, UtcNow), CancellationToken.None);
            actual.Add(result.FailureCode);
        }

        await Assert.That(actual).IsEquivalentTo(expected);
    }

    private RegistrationPaymentAttemptClaimService CreateService()
    {
        _descriptor.Describe().Returns(new PaymentProviderDescriptor("stripe", "OrganizerDirect", "2026-07-29.dahlia"));
        return new(
            _attempts,
            _orders,
            _events,
            _connections,
            _policies,
            _commerce,
            _descriptor,
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
        SetTotal(order, totalDueMinor);
        return order;
    }

    private PaymentAttempt CreateAttempt(PaymentAttemptStatusEnum status)
    {
        PaymentAttempt attempt = PaymentAttempt.Create(
            Guid.CreateVersion7(),
            _tenantId,
            _orderId,
            RecipientSnapshot(),
            "OrganizerDirect",
            "2026-08-20.acacia",
            "composition-a",
            1_000,
            75,
            125,
            "checkout:" + _tenantId.ToString("N") + ":" + _orderId.ToString("N") + ":abc",
            UtcNow,
            UtcNow.AddMinutes(30));
        if (status == PaymentAttemptStatusEnum.DispatchPending)
        {
            attempt.MarkDispatchPending(UtcNow.AddSeconds(1), null);
        }

        return attempt;
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
