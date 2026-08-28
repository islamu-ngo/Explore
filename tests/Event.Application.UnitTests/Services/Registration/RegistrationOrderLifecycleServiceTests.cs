// ABOUTME: Tests registration-order lifecycle orchestration at its transaction-bound persistence edges.
// ABOUTME: Verifies free and reconciled-paid admission materialization, hold recovery, and approval routing.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Scheduling;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.ValueObjects;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = global::Explore.Domain.Event;

namespace Event.Application.UnitTests.Services.Registration;

public sealed class RegistrationOrderLifecycleServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IRegistrationInventoryRepository _inventory = Substitute.For<IRegistrationInventoryRepository>();
    private readonly IPromotionRedemptionRepository _promotions = Substitute.For<IPromotionRedemptionRepository>();
    private readonly IRegistrationParticipantRepository _participants = Substitute.For<IRegistrationParticipantRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly IPlatformContributionSettingRepository _contributionSettings = Substitute.For<IPlatformContributionSettingRepository>();
    private readonly IEventSessionRepository _sessions = Substitute.For<IEventSessionRepository>();
    private readonly IOutboxRepository _outbox = Substitute.For<IOutboxRepository>();
    private readonly IRegistrationFinalizationRepository _finalization = Substitute.For<IRegistrationFinalizationRepository>();
    private readonly IRegistrationPaymentAttemptRepository _paymentAttempts = Substitute.For<IRegistrationPaymentAttemptRepository>();
    private readonly IScheduledDeadlineDispatcher _deadlines = Substitute.For<IScheduledDeadlineDispatcher>();
    private readonly IRegistrationOrderTransitionCoordinator _transitions =
        Substitute.For<IRegistrationOrderTransitionCoordinator>();

    public RegistrationOrderLifecycleServiceTests()
    {
        _finalization.GetSucceededPaymentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(SucceededPaymentLookupResult.Missing());
    }

    [Test]
    public async Task GetAsyncWhenOrderIsMissingReturnsNullWithoutLoadingContributionSettings()
    {
        Guid orderId = Guid.CreateVersion7();
        using var cancellation = new CancellationTokenSource();

        RegistrationOrderDto? result = await CreateService()
            .GetAsync(orderId, _tenantId, cancellation.Token);

        await Assert.That(result).IsNull();
        await _inventory.Received(1).GetOrderWithLinesAsync(
            orderId,
            _tenantId,
            cancellation.Token);
        await _contributionSettings.DidNotReceive()
            .GetActiveAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetByEventAsyncMapsEveryTenantQualifiedOrder()
    {
        (RegistrationOrder order, _, _) = CreateOrder();
        using var cancellation = new CancellationTokenSource();
        _inventory.GetOrdersByEventAsync(
                _eventId,
                _tenantId,
                cancellation.Token)
            .Returns([order]);

        IReadOnlyList<RegistrationOrderDto> result = await CreateService()
            .GetByEventAsync(_eventId, _tenantId, cancellation.Token);

        await Assert.That(result).HasSingleItem();
        await Assert.That(result[0].Id).IsEqualTo(order.Id);
        await _inventory.Received(1).GetOrdersByEventAsync(
            _eventId,
            _tenantId,
            cancellation.Token);
    }

    [Test]
    public async Task GetAsyncWhenPaidCheckoutAcceptanceIsAvailableReturnsActivationAffordance()
    {
        (RegistrationOrder order, _, _) = CreateOrder(unitPriceMinor: 100);
        MoveToAwaitingPayment(order);
        ConfigureOrder(order, []);
        var paidAcceptance = Substitute.For<IPaidOrderAcceptanceService>();
        PaidOrderAcceptanceSnapshot snapshot = PaidAcceptanceTestFacts.Create(
            _tenantId,
            order.Id,
            _eventId,
            "checkout:current",
            Guid.CreateVersion7(),
            tenantPolicyVersionId: null,
            organizerAmountMinor: 100,
            platformFeeMinor: 0,
            platformContributionMinor: 0,
            UtcNow);
        paidAcceptance.DescribeAsync(order, Arg.Any<CancellationToken>()).Returns(
            new PaidOrderAcceptanceResult(PaidAcceptanceTestFacts.ToDisclosure(snapshot), snapshot, null, null));

        RegistrationOrderDto? result = await CreateService(paidAcceptance: paidAcceptance)
            .GetAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result!.PaidCheckoutActivationAvailable).IsTrue();
        await paidAcceptance.Received(1).DescribeAsync(order, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FinalizePaidAsyncWhenSucceededAndEligibleConfirmsOnceWithAdmissionIssuanceIntent()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(unitPriceMinor: 100, capacityBacked: true);
        MoveToAwaitingPayment(order);
        RegistrationInventoryHold hold = RegistrationInventoryHold.Create(
            order.Id, ticket.CapacityPoolId!.Value, ticket.Id, _tenantId, 1, UtcNow, UtcNow.AddMinutes(15));
        ConfigureOrder(order, [hold]);
        ConfigurePaidEvidence(order);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.TryConsumeActiveHoldsForOrderAsync(order.Id, _tenantId, UtcNow, Arg.Any<CancellationToken>()).Returns(1);
        OutboxMessage? message = null;
        _outbox.Create(Arg.Any<OutboxMessage>()).Returns(call =>
        {
            message = call.ArgAt<OutboxMessage>(0);
            return Task.FromResult(message);
        });

        RegistrationOrderLifecycleResponseDto first = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);
        RegistrationOrderLifecycleResponseDto duplicate = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(first.IsSuccess).IsTrue();
        await Assert.That(first.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Confirmed);
        await Assert.That(duplicate.IsSuccess).IsTrue();
        await _outbox.Received(1).Create(Arg.Any<OutboxMessage>());
        await Assert.That(message!.Payload).Contains("\"AdmissionIssuanceRequested\":true", StringComparison.Ordinal);
    }

    [Test]
    public async Task ReconciledPaymentAloneDoesNotBypassRequirementsApprovalOrCapacityAuthority()
    {
        (RegistrationOrder requirementsOrder, _, _) = CreateOrder(
            unitPriceMinor: 100,
            registrationWorkflowId: Guid.CreateVersion7());
        MoveToAwaitingPayment(requirementsOrder);
        ConfigureOrder(requirementsOrder, []);
        ConfigurePaidEvidence(requirementsOrder);
        _finalization.AreMandatoryRequirementsFulfilledAsync(
                _tenantId, requirementsOrder.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        (RegistrationOrder approvalOrder, _, _) = CreateOrder(unitPriceMinor: 100);
        MoveTo(approvalOrder, RegistrationOrderStatusEnum.AwaitingApproval);
        ConfigureOrder(approvalOrder, []);
        ConfigurePaidEvidence(approvalOrder);

        (RegistrationOrder capacityOrder, EventTicketCatalogVersion capacityCatalog, EventTicketType capacityTicket) =
            CreateOrder(unitPriceMinor: 100, capacityBacked: true);
        MoveToAwaitingPayment(capacityOrder);
        RegistrationInventoryHold expired = RegistrationInventoryHold.Create(
            capacityOrder.Id,
            capacityTicket.CapacityPoolId!.Value,
            capacityTicket.Id,
            _tenantId,
            1,
            UtcNow.AddMinutes(-15),
            UtcNow.AddMinutes(-1));
        expired.TryExpire(UtcNow);
        ConfigureOrder(capacityOrder, [expired]);
        ConfigurePaidEvidence(capacityOrder);
        _catalogs.GetOrderCatalogAsync(
                capacityCatalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns(capacityCatalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.ReserveRecoveredHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(false, false, false));

        RegistrationOrderLifecycleResponseDto requirements = await CreateService().FinalizePaidAsync(
            requirementsOrder.Id, _tenantId, CancellationToken.None);
        RegistrationOrderLifecycleResponseDto approval = await CreateService().FinalizePaidAsync(
            approvalOrder.Id, _tenantId, CancellationToken.None);
        RegistrationOrderLifecycleResponseDto capacity = await CreateService().FinalizePaidAsync(
            capacityOrder.Id, _tenantId, CancellationToken.None);

        await Assert.That(requirementsOrder.RegistrationOrderStatusId)
            .IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingPayment);
        await Assert.That(approvalOrder.RegistrationOrderStatusId)
            .IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingApproval);
        await Assert.That(capacity.Order!.StatusId)
            .IsEqualTo((int)RegistrationOrderStatusEnum.NeedsReconciliation);
        await _inventory.DidNotReceive().AddEventRegistrationsAsync(
            Arg.Any<IReadOnlyCollection<EventRegistration>>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task FinalizePaidAsyncWithMissingRequirementsDoesNotConfirm()
    {
        (RegistrationOrder order, _, _) = CreateOrder(unitPriceMinor: 100, registrationWorkflowId: Guid.CreateVersion7());
        MoveToAwaitingPayment(order);
        ConfigureOrder(order, []);
        ConfigurePaidEvidence(order);
        _finalization.AreMandatoryRequirementsFulfilledAsync(_tenantId, order.Id, Arg.Any<CancellationToken>()).Returns(false);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingPayment);
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task FinalizePaidAsyncAfterRequirementsCompleteResumesTheSamePaidOrder()
    {
        Guid workflowId = Guid.CreateVersion7();
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(unitPriceMinor: 100, registrationWorkflowId: workflowId);
        MoveToAwaitingPayment(order);
        ConfigureOrder(order, []);
        ConfigurePaidEvidence(order);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _finalization.AreMandatoryRequirementsFulfilledAsync(_tenantId, order.Id, Arg.Any<CancellationToken>()).Returns(false, true, true);

        RegistrationOrderLifecycleResponseDto blocked = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);
        RegistrationOrderLifecycleResponseDto resumed = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(blocked.IsSuccess).IsFalse();
        await Assert.That(resumed.IsSuccess).IsTrue();
        await Assert.That(resumed.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Confirmed);
    }

    [Test]
    public async Task FinalizePaidAsyncWhileApprovalIsPendingDoesNotConfirm()
    {
        (RegistrationOrder order, _, _) = CreateOrder(unitPriceMinor: 100);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingApproval);
        ConfigureOrder(order, []);
        ConfigurePaidEvidence(order);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingApproval);
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task FinalizePaidAsyncAfterApprovalResumesWithoutAnotherCheckout()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(unitPriceMinor: 100);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingApproval);
        ConfigureOrder(order, []);
        ConfigurePaidEvidence(order);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);

        RegistrationOrderLifecycleResponseDto blocked = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);
        order.TransitionTo(RegistrationOrderStatusEnum.ReadyForCheckout, UtcNow);
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingPayment, UtcNow);
        RegistrationOrderLifecycleResponseDto resumed = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(blocked.IsSuccess).IsFalse();
        await Assert.That(resumed.IsSuccess).IsTrue();
        await Assert.That(resumed.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Confirmed);
    }

    [Test]
    public async Task FinalizePaidAsyncAfterHoldExpiryReacquiresCapacityBeforeConfirming()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(unitPriceMinor: 100, capacityBacked: true);
        MoveToAwaitingPayment(order);
        RegistrationInventoryHold expired = RegistrationInventoryHold.Create(
            order.Id, ticket.CapacityPoolId!.Value, ticket.Id, _tenantId, 1, UtcNow.AddMinutes(-15), UtcNow.AddMinutes(-1));
        expired.TryExpire(UtcNow);
        RegistrationInventoryHold recovered = RegistrationInventoryHold.Create(
            order.Id, ticket.CapacityPoolId.Value, ticket.Id, _tenantId, 1, UtcNow, UtcNow.AddMinutes(15));
        ConfigureOrder(order, [expired]);
        _inventory.GetHoldsByOrderAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns([expired], [recovered]);
        ConfigurePaidEvidence(order);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.ReserveRecoveredHoldsAsync(_eventId, _tenantId, Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(), UtcNow, Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(true, false, false));
        _inventory.TryConsumeActiveHoldsForOrderAsync(order.Id, _tenantId, UtcNow, Arg.Any<CancellationToken>()).Returns(1);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Confirmed);
        await _inventory.Received(1).ReserveRecoveredHoldsAsync(
            _eventId, _tenantId, Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(), UtcNow, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FinalizePaidAsyncWhenExpiredCapacityCannotBeRecoveredParksWithoutReleasingPaidEvidence()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(unitPriceMinor: 100, capacityBacked: true);
        MoveToAwaitingPayment(order);
        RegistrationInventoryHold expired = RegistrationInventoryHold.Create(
            order.Id, ticket.CapacityPoolId!.Value, ticket.Id, _tenantId, 1, UtcNow.AddMinutes(-15), UtcNow.AddMinutes(-1));
        expired.TryExpire(UtcNow);
        ConfigureOrder(order, [expired]);
        ConfigurePaidEvidence(order);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.ReserveRecoveredHoldsAsync(_eventId, _tenantId, Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(), UtcNow, Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(false, false, false));

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.NeedsReconciliation);
        await _inventory.DidNotReceive().TryReleaseActiveHoldsForOrderAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<RegistrationInventoryHoldStatusEnum>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task FinalizePaidAsyncWhenCapacityLaterBecomesAvailableResumesFromNeedsReconciliation()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(unitPriceMinor: 100, capacityBacked: true);
        MoveToAwaitingPayment(order);
        RegistrationInventoryHold expired = RegistrationInventoryHold.Create(
            order.Id, ticket.CapacityPoolId!.Value, ticket.Id, _tenantId, 1, UtcNow.AddMinutes(-15), UtcNow.AddMinutes(-1));
        expired.TryExpire(UtcNow);
        RegistrationInventoryHold recovered = RegistrationInventoryHold.Create(
            order.Id, ticket.CapacityPoolId.Value, ticket.Id, _tenantId, 1, UtcNow, UtcNow.AddMinutes(15));
        ConfigureOrder(order, [expired]);
        ConfigurePaidEvidence(order);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.ReserveRecoveredHoldsAsync(_eventId, _tenantId, Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(), UtcNow, Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(false, false, false), new RegistrationInventoryReservationResult(true, false, false));
        _inventory.GetHoldsByOrderAsync(order.Id, _tenantId, Arg.Any<CancellationToken>())
            .Returns([expired], [expired], [recovered]);
        _inventory.TryConsumeActiveHoldsForOrderAsync(order.Id, _tenantId, UtcNow, Arg.Any<CancellationToken>()).Returns(1);

        RegistrationOrderLifecycleResponseDto parked = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);
        RegistrationOrderLifecycleResponseDto resumed = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(parked.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.NeedsReconciliation);
        await Assert.That(resumed.IsSuccess).IsTrue();
        await Assert.That(resumed.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Confirmed);
    }

    [Test]
    [Arguments("currency")]
    [Arguments("organizer")]
    [Arguments("fee")]
    [Arguments("contribution")]
    public async Task FinalizePaidAsyncWhenCommercialCompositionDiffersParksWithoutAdmissionsOrOutbox(string mismatch)
    {
        (RegistrationOrder order, _, _) = CreateOrder(unitPriceMinor: 100);
        MoveToAwaitingPayment(order);
        ConfigureOrder(order, []);
        ConfigurePaidEvidence(
            order,
            currencyCode: mismatch == "currency" ? "EUR" : null,
            organizerAmountMinor: mismatch == "organizer" ? 99 : null,
            platformFeeMinor: mismatch == "fee" ? 1 : null,
            platformContributionMinor: mismatch == "contribution" ? 1 : null);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.NeedsReconciliation);
        await _inventory.DidNotReceive().AddEventRegistrationsAsync(
            Arg.Any<IReadOnlyCollection<EventRegistration>>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task FinalizePaidAsyncLocksPromotionBeforeRecoveredCapacity()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(unitPriceMinor: 100, capacityBacked: true);
        MoveToAwaitingPayment(order);
        RegistrationInventoryHold expired = RegistrationInventoryHold.Create(
            order.Id, ticket.CapacityPoolId!.Value, ticket.Id, _tenantId, 1, UtcNow.AddMinutes(-15), UtcNow.AddMinutes(-1));
        expired.TryExpire(UtcNow);
        ConfigureOrder(order, [expired]);
        ConfigurePaidEvidence(order);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.ReserveRecoveredHoldsAsync(_eventId, _tenantId, Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(), UtcNow, Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(false, false, false));

        _ = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);

        Received.InOrder(() =>
        {
            _promotions.GetActiveReservationForUpdateAsync(_tenantId, order.Id, Arg.Any<CancellationToken>());
            _inventory.ReserveRecoveredHoldsAsync(
                _eventId, _tenantId, Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(), UtcNow, Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task RecoverExpiredHoldAsyncForSucceededPaidOrderKeepsNeedsReconciliationAndRequeuesPaidFinalization()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(unitPriceMinor: 100, capacityBacked: true);
        MoveToAwaitingPayment(order);
        order.TransitionTo(RegistrationOrderStatusEnum.NeedsReconciliation, UtcNow);
        ConfigureOrder(order, []);
        ConfigurePaidEvidence(order);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.ReserveRecoveredHoldsAsync(_eventId, _tenantId, Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(), UtcNow, Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(true, false, false));

        RegistrationOrderLifecycleResponseDto result = await CreateService().RecoverExpiredHoldAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.NeedsReconciliation);
        await _finalization.Received(1).RequestAsync(order, UtcNow, Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(RegistrationOrderStatusEnum.Rejected)]
    [Arguments(RegistrationOrderStatusEnum.Cancelled)]
    [Arguments(RegistrationOrderStatusEnum.Expired)]
    public async Task FinalizePaidAsyncCannotResurrectTerminalOrders(RegistrationOrderStatusEnum terminalStatus)
    {
        (RegistrationOrder order, _, _) = CreateOrder(unitPriceMinor: 100);
        MoveTo(order, terminalStatus == RegistrationOrderStatusEnum.Rejected
            ? RegistrationOrderStatusEnum.AwaitingApproval
            : RegistrationOrderStatusEnum.ReadyForCheckout);
        order.TransitionTo(terminalStatus, UtcNow);
        ConfigureOrder(order, []);
        ConfigurePaidEvidence(order);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)terminalStatus);
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task FinalizePaidAsyncWithoutExactSucceededObservationDoesNotMutateOrderOrHolds()
    {
        (RegistrationOrder order, _, _) = CreateOrder(unitPriceMinor: 100);
        MoveToAwaitingPayment(order);
        ConfigureOrder(order, []);
        _finalization.GetSucceededPaymentAsync(_tenantId, order.Id, Arg.Any<CancellationToken>())
            .Returns(SucceededPaymentLookupResult.Missing());

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizePaidAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingPayment);
        await _inventory.DidNotReceive().TryConsumeActiveHoldsForOrderAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FinalizePaidAsyncWithMultipleSucceededObservationsParksWithBoundedDuplicateCode()
    {
        (RegistrationOrder order, _, _) = CreateOrder(unitPriceMinor: 100);
        MoveToAwaitingPayment(order);
        ConfigureOrder(order, []);
        _finalization.GetSucceededPaymentAsync(_tenantId, order.Id, Arg.Any<CancellationToken>())
            .Returns(SucceededPaymentLookupResult.Conflict());

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizePaidAsync(
            order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.NeedsReconciliation);
        await Assert.That(result.Message).IsEqualTo("payment_duplicate_succeeded_observations");
        await _inventory.DidNotReceive().AddEventRegistrationsAsync(
            Arg.Any<IReadOnlyCollection<EventRegistration>>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task FinalizePaidAsyncWhenConfirmedAndLateSecondSuccessExistsSurfacesConflictWithoutUndoingAdmission()
    {
        (RegistrationOrder order, _, _) = CreateOrder(unitPriceMinor: 100);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        order.TransitionTo(RegistrationOrderStatusEnum.NeedsReconciliation, UtcNow);
        order.TransitionTo(RegistrationOrderStatusEnum.Confirmed, UtcNow);
        ConfigureOrder(order, []);
        _finalization.GetSucceededPaymentAsync(_tenantId, order.Id, Arg.Any<CancellationToken>())
            .Returns(SucceededPaymentLookupResult.Conflict());

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizePaidAsync(
            order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Confirmed);
        await Assert.That(result.Message).IsEqualTo("payment_duplicate_succeeded_observations");
        await _finalization.Received(1).GetSucceededPaymentAsync(
            _tenantId, order.Id, Arg.Any<CancellationToken>());
        await _inventory.DidNotReceive().AddEventRegistrationsAsync(
            Arg.Any<IReadOnlyCollection<EventRegistration>>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task FinalizeFreeAsyncWhenPromotionReservationIsActiveConsumesItBeforeConfirming()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(capacityBacked: true);
        RegistrationInventoryHold hold = RegistrationInventoryHold.Create(
            order.Id, ticket.CapacityPoolId!.Value, ticket.Id, _tenantId, 1, UtcNow, UtcNow.AddMinutes(15));
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        PromotionReservation reservation = CreateActivePromotionReservation(order, catalog);
        ConfigureFinalization(order, catalog, [], [], []);
        ConfigureOrder(order, [hold]);
        _promotions.GetActiveReservationForUpdateAsync(_tenantId, order.Id, Arg.Any<CancellationToken>()).Returns(reservation);
        _inventory.TryConsumeActiveHoldsForOrderAsync(order.Id, _tenantId, UtcNow, Arg.Any<CancellationToken>()).Returns(1);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(reservation.PromotionReservationStatusId).IsEqualTo((int)PromotionReservationStatusEnum.Consumed);
        Received.InOrder(() =>
        {
            _promotions.GetActiveReservationForUpdateAsync(_tenantId, order.Id, Arg.Any<CancellationToken>());
            _inventory.GetPoolsForUpdateAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { ticket.CapacityPoolId!.Value })),
                order.EventId,
                _tenantId,
                Arg.Any<CancellationToken>());
            _inventory.TryConsumeActiveHoldsForOrderAsync(order.Id, _tenantId, UtcNow, Arg.Any<CancellationToken>());
        });
    }

    /// <summary>
    /// A cancelled order can never need its hold-expiry wake-up again, so the deadline is withdrawn as the
    /// transition lands. Leaving it would accumulate one dead trigger per finished order in the scheduler
    /// tables — the reason cancellation is centralized rather than left to each transition to remember.
    /// </summary>
    [Test]
    public async Task CancelAsyncWithdrawsTheOrdersHoldExpiryDeadline()
    {
        (RegistrationOrder order, _, _) = CreateOrder(addLine: false);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        ConfigureOrder(order, []);

        RegistrationOrderLifecycleResponseDto result = await CreateService()
            .CancelAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _deadlines.Received(1).CancelAsync(
            ScheduledJobNames.InventoryHoldExpiry,
            InventoryHoldDeadline.KeyFor(order.Id),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// A transition that leaves the order mid-flight must keep the deadline: the holds are still live and
    /// still need releasing at their expiry.
    /// </summary>
    [Test]
    public async Task ANonTerminalTransitionLeavesTheHoldExpiryDeadlineInPlace()
    {
        (RegistrationOrder order, _, _) = CreateOrder(addLine: false);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingParticipantDetails);
        ConfigureOrder(order, []);

        await CreateService().SubmitAsync(order.Id, _tenantId, CancellationToken.None);

        await _deadlines.DidNotReceive().CancelAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelAsyncWhenPromotionReservationIsActiveReleasesItBeforeHoldRelease()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(capacityBacked: true);
        RegistrationInventoryHold hold = RegistrationInventoryHold.Create(
            order.Id, ticket.CapacityPoolId!.Value, ticket.Id, _tenantId, 1, UtcNow, UtcNow.AddMinutes(15));
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        PromotionReservation reservation = CreateActivePromotionReservation(order, catalog);
        ConfigureOrder(order, [hold]);
        _promotions.GetActiveReservationForUpdateAsync(_tenantId, order.Id, Arg.Any<CancellationToken>()).Returns(reservation);
        _inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                _tenantId,
                RegistrationInventoryHoldStatusEnum.Cancelled,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(1);

        RegistrationOrderLifecycleResponseDto result = await CreateService().CancelAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(reservation.PromotionReservationStatusId).IsEqualTo((int)PromotionReservationStatusEnum.Released);
        await _promotions.Received(1).GetActiveReservationForUpdateAsync(_tenantId, order.Id, Arg.Any<CancellationToken>());
        Received.InOrder(() =>
        {
            _promotions.GetActiveReservationForUpdateAsync(_tenantId, order.Id, Arg.Any<CancellationToken>());
            _inventory.GetPoolsForUpdateAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { ticket.CapacityPoolId!.Value })),
                order.EventId,
                _tenantId,
                Arg.Any<CancellationToken>());
            _inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                _tenantId,
                RegistrationInventoryHoldStatusEnum.Cancelled,
                UtcNow,
                Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task CancelAsyncUsesOneSerializableTransactionAndNoOrdinaryTransaction()
    {
        (RegistrationOrder order, _, _) = CreateOrder(addLine: false);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        ConfigureOrder(order, []);
        _inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                _tenantId,
                RegistrationInventoryHoldStatusEnum.Cancelled,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(1);
        _outbox.Create(Arg.Any<OutboxMessage>()).Returns(call => Task.FromResult(call.ArgAt<OutboxMessage>(0)));
        var unitOfWork = new CountingUnitOfWork();
        using var cancellation = new CancellationTokenSource();

        RegistrationOrderLifecycleResponseDto result = await CreateService(unitOfWork)
            .CancelAsync(order.Id, _tenantId, cancellation.Token);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(unitOfWork.SerializableCount).IsEqualTo(1);
        await Assert.That(unitOfWork.TransactionCount).IsEqualTo(0);
        await Assert.That(unitOfWork.LastSerializableToken).IsEqualTo(cancellation.Token);
    }

    [Test]
    public async Task RejectAsyncUsesOneSerializableTransactionAndNoOrdinaryTransaction()
    {
        (RegistrationOrder order, _, _) = CreateOrder(addLine: false);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingApproval);
        ConfigureOrder(order, []);
        _inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                _tenantId,
                RegistrationInventoryHoldStatusEnum.Released,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(1);
        _outbox.Create(Arg.Any<OutboxMessage>()).Returns(call => Task.FromResult(call.ArgAt<OutboxMessage>(0)));
        var unitOfWork = new CountingUnitOfWork();

        RegistrationOrderLifecycleResponseDto result = await CreateService(unitOfWork)
            .RejectAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Rejected);
        await Assert.That(unitOfWork.SerializableCount).IsEqualTo(1);
        await Assert.That(unitOfWork.TransactionCount).IsEqualTo(0);
    }

    [Test]
    public async Task CancelAsyncWhenAlreadyCancelledDoesNotDuplicateLifecycleEffects()
    {
        (RegistrationOrder order, _, _) = CreateOrder(addLine: false);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        order.TransitionTo(RegistrationOrderStatusEnum.Cancelled, UtcNow);
        ConfigureOrder(order, []);
        var unitOfWork = new CountingUnitOfWork();

        RegistrationOrderLifecycleResponseDto result = await CreateService(unitOfWork)
            .CancelAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(unitOfWork.SerializableCount).IsEqualTo(1);
        await Assert.That(unitOfWork.TransactionCount).IsEqualTo(0);
        await _inventory.DidNotReceive().TryReleaseActiveHoldsForOrderAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<RegistrationInventoryHoldStatusEnum>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task ReadyForCheckoutRequiresEveryMandatoryRequirementFulfillment()
    {
        Guid workflowId = Guid.CreateVersion7();
        (RegistrationOrder order, _, _) = CreateOrder(registrationWorkflowId: workflowId);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        ConfigureOrder(order, []);
        _finalization.AreMandatoryRequirementsFulfilledAsync(_tenantId, order.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        RegistrationOrderLifecycleResponseDto result = await CreateService()
            .ReadyForCheckoutAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingRequirements);
        await _transitions.DidNotReceive().PersistAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<RegistrationOrderStatusEnum>(),
            Arg.Any<RegistrationOrderStatusEnum>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SubmitAsyncWithContributionPersistsServerComputedSnapshotBeforeAdvancing()
    {
        (RegistrationOrder order, _, _) = CreateOrder(unitPriceMinor: 1_000);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingParticipantDetails);
        PlatformContributionSetting setting = PlatformContributionSetting.CreateInitial(
            true,
            "Support the platform",
            "Optional contribution",
            [PlatformContributionOption.Create(0, 0, true), PlatformContributionOption.Create(1_000, 1, false)]);
        ConfigureOrder(order, []);
        _inventory.GetOrderForUpdateWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _contributionSettings.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(setting);
        _inventory.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        RegistrationOrderLifecycleResponseDto result = await CreateService().SubmitAsync(
            order.Id,
            _tenantId,
            1_000,
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingRequirements);
        await Assert.That(order.PlatformContribution!.AmountMinor).IsEqualTo(100);
        await Assert.That(order.OrganizerDirectedTotalMinorSnapshot).IsEqualTo(1_000);
        await Assert.That(order.OrganizerEarningsTotalMinorSnapshot).IsEqualTo(1_000);
        await Assert.That(order.TotalDueMinorSnapshot).IsEqualTo(1_100);
        _ = _inventory.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FinalizeFreeAsyncNoneModeCreatesOneParticipantBackedAdmissionPerTicketUnit()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(quantity: 2);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        var admissions = new List<EventRegistration>();
        var placeholders = new List<RegistrationParticipant>();
        ConfigureOrder(order, []);
        _participants.GetAssignmentsWithParticipantsByOrderAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns([]);
        _participants.AddParticipantsAsync(Arg.Any<IReadOnlyCollection<RegistrationParticipant>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                placeholders.AddRange(call.ArgAt<IReadOnlyCollection<RegistrationParticipant>>(0));
                return Task.CompletedTask;
            });
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.AddEventRegistrationsAsync(Arg.Any<IReadOnlyCollection<EventRegistration>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                admissions.AddRange(call.ArgAt<IReadOnlyCollection<EventRegistration>>(0));
                return Task.CompletedTask;
            });
        _outbox.Create(Arg.Any<OutboxMessage>()).Returns(call => Task.FromResult(call.ArgAt<OutboxMessage>(0)));

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizeFreeAsync(
            order.Id,
            _tenantId,
            CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(placeholders).Count().IsEqualTo(2);
        await Assert.That(placeholders.All(participant =>
            participant.ParticipantTypeId == (int)ParticipantTypeEnum.Unnamed &&
            participant.LinkedUserId is null &&
            participant.Pii is null)).IsTrue();
        await Assert.That(admissions).Count().IsEqualTo(2);
        await Assert.That(admissions.All(admission => admission.RegistrationParticipantId != Guid.Empty)).IsTrue();
        await Assert.That(admissions.Select(admission => admission.RegistrationParticipantId).Distinct()).Count().IsEqualTo(2);
        await Assert.That(admissions.All(admission => admission.LinkedUserId is null)).IsTrue();
    }

    [Test]
    public async Task FinalizeFreeAsyncBookingPartyOverageStopsBeforeHoldsAdmissionsAndOutbox()
    {
        Guid purchaserActorId = Guid.CreateVersion7();
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(
            purchaserActorId: purchaserActorId,
            perBookingPartyLimit: 1);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _participants.GetAssignmentsWithParticipantsByOrderAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns([]);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.GetTicketLimitUsageAsync(
                _eventId, _tenantId, null, null, purchaserActorId,
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, RegistrationTicketLimitUsage>
            {
                [ticket.Id] = new(ticket.Id, 0, 0, 2)
            });

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizeFreeAsync(
            order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).Contains("Registration order exceeds its booking-party ticket limit.");
        _ = _inventory.DidNotReceive().GetHoldsByOrderAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _ = _inventory.DidNotReceive().AddEventRegistrationsAsync(Arg.Any<IReadOnlyCollection<EventRegistration>>(), Arg.Any<CancellationToken>());
        _ = _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task FinalizeFreeAsyncAbsentBookingPartyIdentityDoesNotInventCrossOrderLimit()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(perBookingPartyLimit: 1);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        var placeholders = new List<RegistrationParticipant>();
        var admissions = new List<EventRegistration>();
        ConfigureFinalization(order, catalog, [], placeholders, admissions);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizeFreeAsync(
            order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        _ = _inventory.DidNotReceive().GetTicketLimitUsageAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<Guid?>(),
            Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(ParticipantDataCollectionModeEnum.None)]
    [Arguments(ParticipantDataCollectionModeEnum.LeadBookerOnly)]
    public async Task FinalizeFreeAsyncNonAssignedModesCreatePiiFreeUnitPlaceholders(
        ParticipantDataCollectionModeEnum mode)
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(quantity: 2, participantMode: mode);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        var placeholders = new List<RegistrationParticipant>();
        var admissions = new List<EventRegistration>();
        ConfigureFinalization(order, catalog, [], placeholders, admissions);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(placeholders).Count().IsEqualTo(2);
        await Assert.That(placeholders.All(participant => participant.ParticipantTypeId == (int)ParticipantTypeEnum.Unnamed &&
            participant.LinkedUserId is null && participant.Pii is null)).IsTrue();
        await Assert.That(admissions.Select(admission => admission.RegistrationParticipantId).SequenceEqual(
            placeholders.Select(participant => participant.Id))).IsTrue();
        await Assert.That(admissions.All(admission => admission.LinkedUserId is null)).IsTrue();
    }

    [Test]
    public async Task FinalizeFreeAsyncOptionalModeUsesAssignedParticipantAndPlaceholderForMissingUnit()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            quantity: 2,
            participantMode: ParticipantDataCollectionModeEnum.PerTicketOptional);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        Guid linkedUserId = Guid.CreateVersion7();
        RegistrationParticipant participant = RegistrationParticipant.Create(
            _tenantId, order.Id, linkedUserId, ParticipantTypeEnum.Adult, null);
        RegistrationTicketAssignment assignment = RegistrationTicketAssignment.CreateAssigned(
            Guid.CreateVersion7(), order.Lines.Single().Id, 1, participant, UtcNow);
        var placeholders = new List<RegistrationParticipant>();
        var admissions = new List<EventRegistration>();
        ConfigureFinalization(order, catalog, [assignment], placeholders, admissions);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(placeholders).HasSingleItem();
        await Assert.That(admissions).Count().IsEqualTo(2);
        await Assert.That(admissions.Single(admission => admission.RegistrationParticipantId == participant.Id).LinkedUserId)
            .IsEqualTo(linkedUserId);
        await Assert.That(admissions.Single(admission => admission.RegistrationParticipantId == placeholders.Single().Id).LinkedUserId)
            .IsNull();
    }

    [Test]
    public async Task FinalizeFreeAsyncRequiredModeMissingAssignmentFailsBeforeTransactionEffects()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            quantity: 2,
            participantMode: ParticipantDataCollectionModeEnum.PerTicketRequired);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _participants.GetAssignmentsWithParticipantsByOrderAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns([]);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await _inventory.DidNotReceive().TryConsumeActiveHoldsForOrderAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _participants.DidNotReceive().AddParticipantsAsync(
            Arg.Any<IReadOnlyCollection<RegistrationParticipant>>(), Arg.Any<CancellationToken>());
        await _inventory.DidNotReceive().AddEventRegistrationsAsync(
            Arg.Any<IReadOnlyCollection<EventRegistration>>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task FinalizeFreeAsyncRequiredModeMapsParticipantAndLinkedUser()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            participantMode: ParticipantDataCollectionModeEnum.PerTicketRequired);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        Guid linkedUserId = Guid.CreateVersion7();
        RegistrationParticipant participant = RegistrationParticipant.Create(
            _tenantId, order.Id, linkedUserId, ParticipantTypeEnum.Adult, null);
        RegistrationTicketAssignment assignment = RegistrationTicketAssignment.CreateAssigned(
            Guid.CreateVersion7(), order.Lines.Single().Id, 1, participant, UtcNow);
        var placeholders = new List<RegistrationParticipant>();
        var admissions = new List<EventRegistration>();
        ConfigureFinalization(order, catalog, [assignment], placeholders, admissions);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(placeholders).IsEmpty();
        await Assert.That(admissions).HasSingleItem();
        await Assert.That(admissions.Single().RegistrationParticipantId).IsEqualTo(participant.Id);
        await Assert.That(admissions.Single().LinkedUserId).IsEqualTo(linkedUserId);
    }

    [Test]
    public async Task FinalizeFreeAsyncDeferredModeMaterializesOnlyAssignedUnits()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            quantity: 2,
            participantMode: ParticipantDataCollectionModeEnum.DeferredAssignment);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        RegistrationParticipant participant = RegistrationParticipant.Create(
            _tenantId, order.Id, null, ParticipantTypeEnum.Adult, null);
        DateTime deadline = UtcNow.AddDays(1);
        RegistrationTicketAssignment[] assignments =
        [
            RegistrationTicketAssignment.CreateAssigned(
                Guid.CreateVersion7(), order.Lines.Single().Id, 1, participant, UtcNow),
            RegistrationTicketAssignment.Create(
                _tenantId, order.Id, order.Lines.Single().Id, 2, null,
                AssignmentStatusEnum.Deferred, deadline, UtcNow)
        ];
        var placeholders = new List<RegistrationParticipant>();
        var admissions = new List<EventRegistration>();
        ConfigureFinalization(order, catalog, assignments, placeholders, admissions);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(placeholders).IsEmpty();
        await Assert.That(admissions).HasSingleItem();
        await Assert.That(admissions.Single().RegistrationParticipantId).IsEqualTo(participant.Id);
    }

    [Test]
    public async Task FinalizeFreeAsyncDeferredModeAllUnassignedConfirmsWithoutAdmissions()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            quantity: 2,
            participantMode: ParticipantDataCollectionModeEnum.DeferredAssignment);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        DateTime deadline = UtcNow.AddDays(1);
        RegistrationTicketAssignment[] assignments = new[] { 1, 2 }
            .Select(ordinal => RegistrationTicketAssignment.Create(
                _tenantId, order.Id, order.Lines.Single().Id, ordinal, null,
                AssignmentStatusEnum.Deferred, deadline, UtcNow))
            .ToArray();
        var placeholders = new List<RegistrationParticipant>();
        var admissions = new List<EventRegistration>();
        ConfigureFinalization(order, catalog, assignments, placeholders, admissions);

        RegistrationOrderLifecycleResponseDto result = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(placeholders).IsEmpty();
        await Assert.That(admissions).IsEmpty();
        await _outbox.Received(1).Create(Arg.Is<OutboxMessage>(message => message.EventType == "RegistrationOrderConfirmed"));
    }

    [Test]
    public async Task FinalizeFreeAsyncRetryReusesParticipantAdmissionAndOutboxIds()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(quantity: 2);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _participants.GetAssignmentsWithParticipantsByOrderAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns([]);
        _transitions.PersistAsync(
            order.Id, _tenantId, Arg.Any<RegistrationOrderStatusEnum>(), Arg.Any<RegistrationOrderStatusEnum>(),
            UtcNow, Arg.Any<CancellationToken>()).Returns(true);
        var participantAttempts = new List<Guid[]>();
        var admissionAttempts = new List<Guid[]>();
        var outboxIds = new List<Guid>();
        _participants.AddParticipantsAsync(Arg.Any<IReadOnlyCollection<RegistrationParticipant>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                participantAttempts.Add(call.ArgAt<IReadOnlyCollection<RegistrationParticipant>>(0).Select(item => item.Id).ToArray());
                return Task.CompletedTask;
            });
        _inventory.AddEventRegistrationsAsync(Arg.Any<IReadOnlyCollection<EventRegistration>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                admissionAttempts.Add(call.ArgAt<IReadOnlyCollection<EventRegistration>>(0).Select(item => item.Id).ToArray());
                return Task.CompletedTask;
            });
        _outbox.Create(Arg.Any<OutboxMessage>()).Returns(call =>
        {
            OutboxMessage message = call.ArgAt<OutboxMessage>(0);
            outboxIds.Add(message.Id);
            return Task.FromResult(message);
        });

        RegistrationOrderLifecycleResponseDto result = await CreateService(new RetryingUnitOfWork())
            .FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(participantAttempts).Count().IsEqualTo(2);
        await Assert.That(admissionAttempts).Count().IsEqualTo(2);
        await Assert.That(participantAttempts[0].SequenceEqual(participantAttempts[1])).IsTrue();
        await Assert.That(admissionAttempts[0].SequenceEqual(admissionAttempts[1])).IsTrue();
        await Assert.That(participantAttempts.SelectMany(ids => ids).Distinct()).Count().IsEqualTo(2);
        await Assert.That(admissionAttempts.SelectMany(ids => ids).Distinct()).Count().IsEqualTo(2);
        await Assert.That(outboxIds.Distinct()).HasSingleItem();
    }

    [Test]
    public async Task FinalizeFreeAsyncWhenReadyConsumesHoldsCreatesAdmissionsAndWritesConfirmationOutbox()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(capacityBacked: true);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        RegistrationInventoryHold hold = RegistrationInventoryHold.Create(order.Id, ticket.CapacityPoolId!.Value, ticket.Id, _tenantId, 1, UtcNow, UtcNow.AddMinutes(15));
        var admissions = new List<EventRegistration>();
        OutboxMessage? message = null;
        ConfigureOrder(order, [hold]);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.TryConsumeActiveHoldsForOrderAsync(order.Id, _tenantId, UtcNow, Arg.Any<CancellationToken>()).Returns(1);
        _inventory.AddEventRegistrationsAsync(Arg.Any<IReadOnlyCollection<EventRegistration>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                admissions.AddRange(call.ArgAt<IReadOnlyCollection<EventRegistration>>(0));
                return Task.CompletedTask;
            });
        _outbox.Create(Arg.Any<OutboxMessage>()).Returns(call =>
        {
            message = call.ArgAt<OutboxMessage>(0);
            return Task.FromResult(message);
        });

        var result = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Confirmed);
        await Assert.That(admissions).HasSingleItem();
        await Assert.That(admissions.Single().RegistrationOrderId).IsEqualTo(order.Id);
        await Assert.That(admissions.Single().RegistrationOrderLineId).IsEqualTo(order.Lines.Single().Id);
        await Assert.That(message!.EventType).IsEqualTo("RegistrationOrderConfirmed");
        await Assert.That(message.Payload).DoesNotContain("@", StringComparison.Ordinal);
    }

    [Test]
    public async Task FinalizeFreeAsyncWhenCapacityBackedLineHasNoActiveHoldDoesNotConfirm()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(capacityBacked: true);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        _inventory.GetOrderWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _inventory.GetOrderForUpdateWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _inventory.GetHoldsByOrderAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns([]);
        _transitions.PersistAsync(
                order.Id,
                _tenantId,
                RegistrationOrderStatusEnum.ReadyForCheckout,
                RegistrationOrderStatusEnum.NeedsReconciliation,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(true);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);

        var result = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.ReadyForCheckout);
        await _inventory.DidNotReceive().AddEventRegistrationsAsync(
            Arg.Any<IReadOnlyCollection<EventRegistration>>(),
            Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task FinalizeFreeAsyncWhenCapacityBackedLineHasOnlyExpiredHoldDoesNotConfirm()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(capacityBacked: true);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        RegistrationInventoryHold expiredHold = RegistrationInventoryHold.Create(
            order.Id,
            ticket.CapacityPoolId!.Value,
            ticket.Id,
            _tenantId,
            1,
            UtcNow.AddMinutes(-15),
            UtcNow.AddMinutes(-1));
        expiredHold.TryExpire(UtcNow);
        ConfigureOrder(order, [expiredHold]);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);

        var result = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.NeedsReconciliation);
        await _inventory.DidNotReceive().TryConsumeActiveHoldsForOrderAsync(
            order.Id,
            _tenantId,
            UtcNow,
            Arg.Any<CancellationToken>());
        await _inventory.DidNotReceive().AddEventRegistrationsAsync(
            Arg.Any<IReadOnlyCollection<EventRegistration>>(),
            Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task ReadyForCheckoutAsyncWhenNoHoldUntilReadyReservesTheCapacityBackedLine()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(
            capacityBacked: true,
            holdPolicy: CapacityHoldPolicyEnum.NoHoldUntilReady);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        var reservations = new List<RegistrationInventoryReservation>();
        _inventory.ReserveNonTimedHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                approvalGranted: false,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                reservations.AddRange(call.ArgAt<IReadOnlyCollection<RegistrationInventoryReservation>>(2));
                return Task.FromResult(new RegistrationInventoryReservationResult(Reserved: true, RequiresApproval: false, ShouldWaitlist: false));
            });

        var result = await CreateService().ReadyForCheckoutAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.ReadyForCheckout);
        await Assert.That(reservations).HasSingleItem();
        await Assert.That(reservations.Single().TicketTypeId).IsEqualTo(ticket.Id);
        await Assert.That(reservations.Single().CapacityPoolId).IsEqualTo(ticket.CapacityPoolId!.Value);
    }

    [Test]
    public async Task ReadyForCheckoutAsyncWhenPromotionIsActiveLocksPromotionBeforeCapacityReservation()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            capacityBacked: true,
            holdPolicy: CapacityHoldPolicyEnum.NoHoldUntilReady);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        PromotionReservation reservation = CreateActivePromotionReservation(order, catalog);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _promotions.GetActiveReservationForUpdateAsync(_tenantId, order.Id, Arg.Any<CancellationToken>()).Returns(reservation);
        _inventory.ReserveNonTimedHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                approvalGranted: false,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(Reserved: true, RequiresApproval: false, ShouldWaitlist: false));

        RegistrationOrderLifecycleResponseDto result = await CreateService().ReadyForCheckoutAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        Received.InOrder(() =>
        {
            _inventory.GetOrderForUpdateWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>());
            _promotions.GetActiveReservationForUpdateAsync(_tenantId, order.Id, Arg.Any<CancellationToken>());
            _inventory.ReserveNonTimedHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                approvalGranted: false,
                UtcNow,
                Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task ReadyForCheckoutAsyncWhenApprovalNoHoldIsConfiguredRequiresApprovalBeforeReservation()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            capacityBacked: true,
            holdPolicy: CapacityHoldPolicyEnum.ApprovalNoHold);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.ReserveNonTimedHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                approvalGranted: false,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(Reserved: false, RequiresApproval: true, ShouldWaitlist: false));

        var result = await CreateService().ReadyForCheckoutAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingApproval);
        await _inventory.Received(1).ReserveNonTimedHoldsAsync(
            _eventId,
            _tenantId,
            Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
            approvalGranted: false,
            UtcNow,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApproveAsyncWhenApprovalNoHoldIsConfiguredReservesOnlyAfterApproval()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            capacityBacked: true,
            holdPolicy: CapacityHoldPolicyEnum.ApprovalNoHold);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingApproval);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.ReserveNonTimedHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                approvalGranted: true,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(Reserved: true, RequiresApproval: false, ShouldWaitlist: false));

        var result = await CreateService().ApproveAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.ReadyForCheckout);
        await _inventory.Received(1).ReserveNonTimedHoldsAsync(
            _eventId,
            _tenantId,
            Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
            approvalGranted: true,
            UtcNow,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadyForCheckoutAsyncWhenWaitlistCapacityChangesRoutesToWaitlistedWithoutReservation()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            capacityBacked: true,
            holdPolicy: CapacityHoldPolicyEnum.WaitlistWhenFull);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.ReserveNonTimedHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                approvalGranted: false,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(Reserved: false, RequiresApproval: false, ShouldWaitlist: true));

        var result = await CreateService().ReadyForCheckoutAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Waitlisted);
        await _inventory.DidNotReceive().TryConsumeActiveHoldsForOrderAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadyForCheckoutAsyncWhenMixedTimedAndWaitlistPoliciesAreFullReleasesTimedHoldBeforeWaitlisting()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, RegistrationInventoryHold timedHold) = CreateMixedPolicyOrder();
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        ConfigureOrder(order, [timedHold]);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.ReserveNonTimedHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                approvalGranted: false,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(Reserved: false, RequiresApproval: false, ShouldWaitlist: true));
        _inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                _tenantId,
                RegistrationInventoryHoldStatusEnum.Released,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(timedHold.TryRelease(UtcNow) ? 1 : 0));

        var result = await CreateService().ReadyForCheckoutAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Waitlisted);
        await Assert.That(timedHold.IsCapacityAllocated).IsFalse();
        await _inventory.Received(1).TryReleaseActiveHoldsForOrderAsync(
            order.Id,
            _tenantId,
            RegistrationInventoryHoldStatusEnum.Released,
            UtcNow,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadyForCheckoutAsyncWhenMixedTimedAndWaitlistPoliciesRetryReleasesTimedHoldOnlyOnce()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, RegistrationInventoryHold timedHold) = CreateMixedPolicyOrder();
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        ConfigureOrder(order, [timedHold]);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.ReserveNonTimedHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                approvalGranted: false,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(Reserved: false, RequiresApproval: false, ShouldWaitlist: true));
        _inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                _tenantId,
                RegistrationInventoryHoldStatusEnum.Released,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(timedHold.TryRelease(UtcNow) ? 1 : 0));

        var result = await CreateService(new RetryingUnitOfWork()).ReadyForCheckoutAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Waitlisted);
        await _inventory.Received(1).TryReleaseActiveHoldsForOrderAsync(
            order.Id,
            _tenantId,
            RegistrationInventoryHoldStatusEnum.Released,
            UtcNow,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadyForCheckoutAsyncWhenTimedHoldReleaseFailsRollsBackWithoutWaitlisting()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, RegistrationInventoryHold timedHold) = CreateMixedPolicyOrder();
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        ConfigureOrder(order, [timedHold]);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.ReserveNonTimedHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                approvalGranted: false,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(Reserved: false, RequiresApproval: false, ShouldWaitlist: true));
        _inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                _tenantId,
                RegistrationInventoryHoldStatusEnum.Released,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(0);
        var unitOfWork = new RollbackTrackingUnitOfWork();

        var result = await CreateService(unitOfWork).ReadyForCheckoutAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingRequirements);
        await Assert.That(timedHold.IsCapacityAllocated).IsTrue();
        await Assert.That(unitOfWork.RollbackCount).IsEqualTo(1);
        await _transitions.DidNotReceive().PersistAsync(
            order.Id,
            _tenantId,
            Arg.Any<RegistrationOrderStatusEnum>(),
            RegistrationOrderStatusEnum.Waitlisted,
            UtcNow,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadyForCheckoutAsyncWhenNonWaitlistCapacityIsUnavailableRollsBackWithoutTransition()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            capacityBacked: true,
            holdPolicy: CapacityHoldPolicyEnum.TimedHoldOnSelection);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.ReserveNonTimedHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                approvalGranted: false,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(Reserved: false, RequiresApproval: false, ShouldWaitlist: false));

        var result = await CreateService().ReadyForCheckoutAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingRequirements);
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task RecoverExpiredHoldAsyncWhenNonWaitlistCapacityIsFullLeavesTheOrderForReconciliation()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            capacityBacked: true,
            holdPolicy: CapacityHoldPolicyEnum.TimedHoldOnSelection);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        order.TransitionTo(RegistrationOrderStatusEnum.NeedsReconciliation, UtcNow);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.ReserveRecoveredHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(Reserved: false, RequiresApproval: false, ShouldWaitlist: false));

        var result = await CreateService().RecoverExpiredHoldAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.NeedsReconciliation);
        await _transitions.DidNotReceive().PersistAsync(
            order.Id,
            _tenantId,
            RegistrationOrderStatusEnum.NeedsReconciliation,
            Arg.Any<RegistrationOrderStatusEnum>(),
            UtcNow,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReadyForCheckoutAsyncWhenOrderHasMultipleCapacityLinesStagesEveryLineWithRetryStableIds()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog) = CreateTwoLineCapacityOrder();
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        _inventory.GetOrderWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _inventory.GetOrderForUpdateWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _transitions.PersistAsync(
                order.Id,
                _tenantId,
                RegistrationOrderStatusEnum.AwaitingRequirements,
                RegistrationOrderStatusEnum.ReadyForCheckout,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(true);
        var attempts = new List<IReadOnlyCollection<RegistrationInventoryReservation>>();
        _inventory.ReserveNonTimedHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                approvalGranted: false,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                attempts.Add(call.ArgAt<IReadOnlyCollection<RegistrationInventoryReservation>>(2));
                return Task.FromResult(new RegistrationInventoryReservationResult(Reserved: true, RequiresApproval: false, ShouldWaitlist: false));
            });

        var result = await CreateService(new RetryingUnitOfWork()).ReadyForCheckoutAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(attempts).Count().IsEqualTo(2);
        await Assert.That(attempts.SelectMany(attempt => attempt).Select(reservation => reservation.TicketTypeId).Distinct()).Count().IsEqualTo(2);
        await Assert.That(attempts.SelectMany(attempt => attempt).Select(reservation => reservation.HoldId).Distinct()).Count().IsEqualTo(2);
    }

    [Test]
    public async Task CancelAsyncWhenOrderHasNoParticipantLinesReleasesActiveHoldsAndWritesCancellationOutbox()
    {
        (RegistrationOrder order, _, _) = CreateOrder(addLine: false);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        ConfigureOrder(order, []);
        _inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                _tenantId,
                RegistrationInventoryHoldStatusEnum.Cancelled,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(1);
        _outbox.Create(Arg.Any<OutboxMessage>()).Returns(call => Task.FromResult(call.ArgAt<OutboxMessage>(0)));

        var result = await CreateService().CancelAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Cancelled);
        await _inventory.Received(1).TryReleaseActiveHoldsForOrderAsync(
            order.Id,
            _tenantId,
            RegistrationInventoryHoldStatusEnum.Cancelled,
            UtcNow,
            Arg.Any<CancellationToken>());
        await _outbox.Received(1).Create(Arg.Is<OutboxMessage>(entry => entry.EventType == "RegistrationOrderCancelled"));
        await _inventory.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ApproveAsyncWhenPaidOrderWasAwaitingApprovalRoutesItToPayment()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(unitPriceMinor: 1_000);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingApproval);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);

        var result = await CreateService().ApproveAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingPayment);
        await _transitions.Received(1).PersistAsync(
            order.Id,
            _tenantId,
            RegistrationOrderStatusEnum.AwaitingApproval,
            RegistrationOrderStatusEnum.ReadyForCheckout,
            UtcNow,
            Arg.Any<CancellationToken>());
        await _transitions.Received(1).PersistAsync(
            order.Id,
            _tenantId,
            RegistrationOrderStatusEnum.ReadyForCheckout,
            RegistrationOrderStatusEnum.AwaitingPayment,
            UtcNow,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task FinalizeFreeAsyncWhenConcurrentWinnerAlreadyConfirmedReturnsOriginalWithoutExtraEffects()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(capacityBacked: true);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        RegistrationInventoryHold hold = RegistrationInventoryHold.Create(order.Id, ticket.CapacityPoolId!.Value, ticket.Id, _tenantId, 1, UtcNow, UtcNow.AddMinutes(15));
        ConfigureOrder(order, [hold]);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _transitions.PersistAsync(
                order.Id,
                _tenantId,
                RegistrationOrderStatusEnum.ReadyForCheckout,
                RegistrationOrderStatusEnum.NeedsReconciliation,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                order.TransitionTo(RegistrationOrderStatusEnum.Confirmed, UtcNow);
                return false;
            });

        var result = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Confirmed);
        await _inventory.DidNotReceive().AddEventRegistrationsAsync(
            Arg.Any<IReadOnlyCollection<EventRegistration>>(),
            Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task FinalizeFreeAsyncWhenAlreadyConfirmedReturnsOriginalWithoutExtraEffects()
    {
        (RegistrationOrder order, _, _) = CreateOrder();
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        order.TransitionTo(RegistrationOrderStatusEnum.Confirmed, UtcNow);
        ConfigureOrder(order, []);

        var result = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Confirmed);
        await _inventory.DidNotReceive().TryConsumeActiveHoldsForOrderAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _inventory.DidNotReceive().AddEventRegistrationsAsync(
            Arg.Any<IReadOnlyCollection<EventRegistration>>(),
            Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task RecoverExpiredHoldAsyncWhenCapacityIsAvailableReReservesAndReturnsTheOrderToCheckout()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            capacityBacked: true,
            holdPolicy: CapacityHoldPolicyEnum.NoHoldUntilReady);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        order.TransitionTo(RegistrationOrderStatusEnum.NeedsReconciliation, UtcNow);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.ReserveRecoveredHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(Reserved: true, RequiresApproval: false, ShouldWaitlist: false));

        var result = await CreateService().RecoverExpiredHoldAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.ReadyForCheckout);
        await _inventory.Received(1).ReserveRecoveredHoldsAsync(
            _eventId,
            _tenantId,
            Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
            UtcNow,
            CancellationToken.None);
        await _inventory.DidNotReceive().AddEventRegistrationsAsync(
            Arg.Any<IReadOnlyCollection<EventRegistration>>(),
            Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());

        var repeated = await CreateService().RecoverExpiredHoldAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(repeated.IsSuccess).IsTrue();
        await Assert.That(repeated.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.ReadyForCheckout);
        await _inventory.Received(1).ReserveRecoveredHoldsAsync(
            _eventId,
            _tenantId,
            Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
            UtcNow,
            CancellationToken.None);
    }

    [Test]
    public async Task RecoverExpiredHoldAsyncThenFinalizeFreeAsyncConsumesOnlyTheReplacementActiveHold()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(capacityBacked: true);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        order.TransitionTo(RegistrationOrderStatusEnum.NeedsReconciliation, UtcNow);
        RegistrationInventoryHold expiredHold = RegistrationInventoryHold.Create(
            order.Id,
            ticket.CapacityPoolId!.Value,
            ticket.Id,
            _tenantId,
            1,
            UtcNow.AddMinutes(-15),
            UtcNow.AddMinutes(-1));
        expiredHold.TryExpire(UtcNow);
        var holds = new List<RegistrationInventoryHold> { expiredHold };
        var admissions = new List<EventRegistration>();
        RegistrationInventoryHold? replacementHold = null;
        ConfigureOrder(order, holds);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.ReserveRecoveredHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                replacementHold = RegistrationInventoryHold.Create(
                    order.Id,
                    ticket.CapacityPoolId!.Value,
                    ticket.Id,
                    _tenantId,
                    1,
                    UtcNow,
                    UtcNow.AddMinutes(15));
                holds.Add(replacementHold);
                return new RegistrationInventoryReservationResult(Reserved: true, RequiresApproval: false, ShouldWaitlist: false);
            });
        _inventory.TryConsumeActiveHoldsForOrderAsync(order.Id, _tenantId, UtcNow, Arg.Any<CancellationToken>())
            .Returns(_ => holds.Count(hold => hold.TryConsume(UtcNow)));
        _inventory.AddEventRegistrationsAsync(Arg.Any<IReadOnlyCollection<EventRegistration>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                admissions.AddRange(call.ArgAt<IReadOnlyCollection<EventRegistration>>(0));
                return Task.CompletedTask;
            });
        _outbox.Create(Arg.Any<OutboxMessage>()).Returns(call => Task.FromResult(call.ArgAt<OutboxMessage>(0)));

        var recovery = await CreateService().RecoverExpiredHoldAsync(order.Id, _tenantId, CancellationToken.None);
        var finalization = await CreateService().FinalizeFreeAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(recovery.IsSuccess).IsTrue();
        await Assert.That(finalization.IsSuccess).IsTrue();
        await Assert.That(finalization.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Confirmed);
        await Assert.That(expiredHold.RegistrationInventoryHoldStatusId).IsEqualTo((int)RegistrationInventoryHoldStatusEnum.Expired);
        await Assert.That(replacementHold!.RegistrationInventoryHoldStatusId).IsEqualTo((int)RegistrationInventoryHoldStatusEnum.Consumed);
        await Assert.That(admissions).HasSingleItem();
        await _inventory.Received(1).TryConsumeActiveHoldsForOrderAsync(order.Id, _tenantId, UtcNow, Arg.Any<CancellationToken>());
        await _inventory.Received(1).AddEventRegistrationsAsync(Arg.Any<IReadOnlyCollection<EventRegistration>>(), Arg.Any<CancellationToken>());
        await _outbox.Received(1).Create(Arg.Is<OutboxMessage>(message => message.EventType == "RegistrationOrderConfirmed"));
    }

    [Test]
    public async Task RecoverExpiredHoldAsyncWhenWaitlistPolicyIsFullTransitionsToWaitlistedWithoutOversell()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(
            capacityBacked: true,
            holdPolicy: CapacityHoldPolicyEnum.WaitlistWhenFull);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        order.TransitionTo(RegistrationOrderStatusEnum.NeedsReconciliation, UtcNow);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.ReserveRecoveredHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(Reserved: false, RequiresApproval: false, ShouldWaitlist: true));

        var result = await CreateService().RecoverExpiredHoldAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Waitlisted);
        await _inventory.DidNotReceive().AddEventRegistrationsAsync(
            Arg.Any<IReadOnlyCollection<EventRegistration>>(),
            Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task RecoverExpiredHoldAsyncWhenWaitlistedWithPromotionReleasesPromotionAndHoldsOnce()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(
            capacityBacked: true,
            holdPolicy: CapacityHoldPolicyEnum.WaitlistWhenFull);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        order.TransitionTo(RegistrationOrderStatusEnum.NeedsReconciliation, UtcNow);
        RegistrationInventoryHold hold = RegistrationInventoryHold.Create(order.Id, ticket.CapacityPoolId!.Value, ticket.Id, _tenantId, 1, UtcNow, UtcNow.AddMinutes(15));
        PromotionReservation reservation = CreateActivePromotionReservation(order, catalog);
        ConfigureOrder(order, [hold]);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _promotions.GetActiveReservationForUpdateAsync(_tenantId, order.Id, Arg.Any<CancellationToken>()).Returns(reservation);
        _inventory.ReserveRecoveredHoldsAsync(
                _eventId,
                _tenantId,
                Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(new RegistrationInventoryReservationResult(Reserved: false, RequiresApproval: false, ShouldWaitlist: true));
        _inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                _tenantId,
                RegistrationInventoryHoldStatusEnum.Released,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(1);

        RegistrationOrderLifecycleResponseDto result = await CreateService().RecoverExpiredHoldAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Waitlisted);
        await Assert.That(reservation.PromotionReservationStatusId).IsEqualTo((int)PromotionReservationStatusEnum.Released);
        Received.InOrder(() =>
        {
            _promotions.GetActiveReservationForUpdateAsync(_tenantId, order.Id, Arg.Any<CancellationToken>());
            _inventory.GetPoolsForUpdateAsync(
                Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(new[] { ticket.CapacityPoolId!.Value })),
                order.EventId,
                _tenantId,
                Arg.Any<CancellationToken>());
            _inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                _tenantId,
                RegistrationInventoryHoldStatusEnum.Released,
                UtcNow,
                Arg.Any<CancellationToken>());
        });
        await _inventory.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RecoverExpiredHoldAsyncWhenAConcurrentAttemptAlreadyResolvedReturnsCurrentStateWithoutASecondReservation()
    {
        (RegistrationOrder order, _, _) = CreateOrder(capacityBacked: true);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        ConfigureOrder(order, []);

        var result = await CreateService().RecoverExpiredHoldAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.ReadyForCheckout);
        await _inventory.DidNotReceive().ReserveRecoveredHoldsAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<RegistrationInventoryReservation>>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelAsyncWhenTransactionRetriesReusesTheSameOutboxIdentity()
    {
        (RegistrationOrder order, _, _) = CreateOrder(addLine: false);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingRequirements);
        var messages = new List<OutboxMessage>();
        _inventory.GetOrderWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _inventory.GetOrderForUpdateWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _transitions.PersistAsync(
                order.Id,
                _tenantId,
                RegistrationOrderStatusEnum.AwaitingRequirements,
                RegistrationOrderStatusEnum.Cancelled,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(true);
        _inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                _tenantId,
                RegistrationInventoryHoldStatusEnum.Cancelled,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(1);
        _outbox.Create(Arg.Any<OutboxMessage>()).Returns(call =>
        {
            OutboxMessage message = call.ArgAt<OutboxMessage>(0);
            messages.Add(message);
            return Task.FromResult(message);
        });

        var result = await CreateService(new RetryingUnitOfWork()).CancelAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(messages).Count().IsEqualTo(2);
        await Assert.That(messages.Select(message => message.Id).Distinct()).HasSingleItem();
    }

    [Test]
    public async Task ConfigurationExpiryCancellationReleasesPromotionHoldsOutboxAndDeadlineOnceOnDuplicate()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, EventTicketType ticket) = CreateOrder(
            unitPriceMinor: 100,
            capacityBacked: true);
        MoveToAwaitingPayment(order);
        RegistrationInventoryHold hold = RegistrationInventoryHold.Create(
            order.Id,
            ticket.CapacityPoolId!.Value,
            ticket.Id,
            _tenantId,
            1,
            UtcNow.AddMinutes(-15),
            UtcNow.AddMinutes(-1));
        ConfigureOrder(order, [hold]);
        PromotionReservation reservation = CreateActivePromotionReservation(order, catalog);
        _promotions.GetActiveReservationForUpdateAsync(_tenantId, order.Id, Arg.Any<CancellationToken>())
            .Returns(reservation, (PromotionReservation?)null);
        _inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                _tenantId,
                RegistrationInventoryHoldStatusEnum.Cancelled,
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(1);
        var claim = new CheckoutDispatchClaim(
            Guid.CreateVersion7(),
            _tenantId,
            order.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            1,
            AttemptCount: 1);
        _paymentAttempts.CancelExpiredConfigurationBlockedAsync(claim, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _outbox.Create(Arg.Any<OutboxMessage>()).Returns(call => call.Arg<OutboxMessage>());

        CheckoutDispatchConfigurationDisposition first = await CreateService().CancelExpiredConfigurationBlockedPaymentAsync(
            claim,
            UtcNow,
            CancellationToken.None);
        CheckoutDispatchConfigurationDisposition duplicate = await CreateService().CancelExpiredConfigurationBlockedPaymentAsync(
            claim,
            UtcNow.AddSeconds(1),
            CancellationToken.None);

        await Assert.That(first).IsEqualTo(CheckoutDispatchConfigurationDisposition.CancelledExpired);
        await Assert.That(duplicate).IsEqualTo(CheckoutDispatchConfigurationDisposition.CancelledExpired);
        await Assert.That(reservation.PromotionReservationStatusId).IsEqualTo((int)PromotionReservationStatusEnum.Released);
        await _paymentAttempts.Received(2).CancelExpiredConfigurationBlockedAsync(
            claim,
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _inventory.Received(1).TryReleaseActiveHoldsForOrderAsync(
            order.Id,
            _tenantId,
            RegistrationInventoryHoldStatusEnum.Cancelled,
            UtcNow,
            Arg.Any<CancellationToken>());
        await _outbox.Received(1).Create(Arg.Is<OutboxMessage>(message =>
            message.AggregateId == order.Id && message.EventType == RegistrationOrderOutboxMessageFactory.CancelledEventType));
        await _deadlines.Received(1).CancelAsync(
            ScheduledJobNames.InventoryHoldExpiry,
            InventoryHoldDeadline.KeyFor(order.Id),
            Arg.Any<CancellationToken>());
    }

    private RegistrationOrderLifecycleService CreateService(
        IUnitOfWork? unitOfWork = null,
        IPaidOrderAcceptanceService? paidAcceptance = null) => new(
        _inventory,
        _promotions,
        _participants,
        _catalogs,
        _contributionSettings,
        _sessions,
        _outbox,
        unitOfWork ?? new InlineUnitOfWork(),
        _finalization,
        _paymentAttempts,
        _deadlines,
        new FixedTimeProvider(UtcNow),
        paidAcceptance ?? Substitute.For<IPaidOrderAcceptanceService>(),
        _transitions);

    private static PromotionReservation CreateActivePromotionReservation(
        RegistrationOrder order,
        EventTicketCatalogVersion catalog)
    {
        PromotionScopeMetadata scope = PromotionScopeMetadata.Create(
            order.TenantId,
            order.EventId,
            order.TicketCatalogVersionId,
            catalog.VersionNumber,
            order.CurrencyCode);
        PromotionDefinition definition = PromotionDefinition.CreateDraft(
            scope,
            "Checkout discount",
            PromotionEligibility.AllTickets(),
            PromotionDiscountRule.FixedMinor(order.CurrencyCode, 1, maximumDiscountMinor: null),
            UtcNow.AddDays(-1),
            UtcNow.AddDays(1),
            totalRedemptionLimit: 10,
            perVerifiedPurchaserLimit: 1);
        definition.Publish(UtcNow.AddMinutes(-1));
        PromotionCode code = PromotionCode.Create(definition, "T1", scope);
        return PromotionReservation.Reserve(Guid.CreateVersion7(), order, definition, code, UtcNow);
    }

    private void ConfigureOrder(RegistrationOrder order, IReadOnlyList<RegistrationInventoryHold> holds)
    {
        _inventory.GetOrderWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _inventory.GetOrderForUpdateWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _inventory.GetHoldsByOrderAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(holds);
        _transitions.PersistAsync(
                order.Id,
                _tenantId,
                Arg.Any<RegistrationOrderStatusEnum>(),
                Arg.Any<RegistrationOrderStatusEnum>(),
                UtcNow,
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                RegistrationOrderStatusEnum expected = call.ArgAt<RegistrationOrderStatusEnum>(2);
                RegistrationOrderStatusEnum desired = call.ArgAt<RegistrationOrderStatusEnum>(3);
                if (order.RegistrationOrderStatusId != (int)expected)
                {
                    return false;
                }

                order.TransitionTo(desired, UtcNow);
                return true;
            });
    }

    private void ConfigurePaidEvidence(
        RegistrationOrder order,
        string? currencyCode = null,
        long? organizerAmountMinor = null,
        long? platformFeeMinor = null,
        long? platformContributionMinor = null)
    {
        OrganizerPaymentRecipientSnapshot recipient = OrganizerPaymentRecipientSnapshot.Create(
            _tenantId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "stripe",
            "platform-eu",
            "acct_paid",
            "BE",
            currencyCode ?? order.CurrencyCode,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            UtcNow.AddMinutes(-2));
        PaymentAttempt attempt = PaymentAttempt.Create(
            Guid.CreateVersion7(),
            _tenantId,
            order.Id,
            recipient,
            "OrganizerDirect",
            "2026-08-20.acacia",
            "paid-composition",
            Money.Create(organizerAmountMinor ?? order.OrganizerDirectedTotalMinorSnapshot, recipient.CurrencyCode),
            Money.Create(platformFeeMinor ?? order.PlatformFeeTotalMinorSnapshot, recipient.CurrencyCode),
            Money.Create(platformContributionMinor ?? order.PlatformContributionTotalMinorSnapshot, recipient.CurrencyCode),
            "checkout:paid",
            UtcNow.AddMinutes(-2),
            UtcNow.AddMinutes(30));
        attempt.MarkSucceededFromCheckout("cs_paid", "pi_paid", UtcNow.AddMinutes(-1), "req_paid");
        PaymentSucceededObservation observation = PaymentSucceededObservation.Create(
            attempt, null, "cs_paid", "pi_paid", "req_paid", UtcNow.AddMinutes(-1));
        _finalization.GetSucceededPaymentAsync(_tenantId, order.Id, Arg.Any<CancellationToken>())
            .Returns(SucceededPaymentLookupResult.Found(attempt, observation));
    }

    private void ConfigureFinalization(
        RegistrationOrder order,
        EventTicketCatalogVersion catalog,
        IReadOnlyList<RegistrationTicketAssignment> assignments,
        List<RegistrationParticipant> placeholders,
        List<EventRegistration> admissions)
    {
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _participants.GetAssignmentsWithParticipantsByOrderAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(assignments);
        _participants.AddParticipantsAsync(Arg.Any<IReadOnlyCollection<RegistrationParticipant>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                placeholders.AddRange(call.ArgAt<IReadOnlyCollection<RegistrationParticipant>>(0));
                return Task.CompletedTask;
            });
        _inventory.AddEventRegistrationsAsync(Arg.Any<IReadOnlyCollection<EventRegistration>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                admissions.AddRange(call.ArgAt<IReadOnlyCollection<EventRegistration>>(0));
                return Task.CompletedTask;
            });
        _outbox.Create(Arg.Any<OutboxMessage>()).Returns(call => Task.FromResult(call.ArgAt<OutboxMessage>(0)));
    }

    private (RegistrationOrder Order, EventTicketCatalogVersion Catalog, EventTicketType Ticket) CreateOrder(
        bool addLine = true,
        long unitPriceMinor = 0,
        bool capacityBacked = false,
        CapacityHoldPolicyEnum holdPolicy = CapacityHoldPolicyEnum.NoHoldUntilReady,
        int quantity = 1,
        ParticipantDataCollectionModeEnum participantMode = ParticipantDataCollectionModeEnum.None,
        Guid? purchaserActorId = null,
        int? perBookingPartyLimit = null,
        Guid? registrationWorkflowId = null)
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1);
        EventCapacityPool? pool = capacityBacked
            ? EventCapacityPool.Create(
                _tenantId,
                _eventId,
                "Registration capacity",
                maximumQuantity: 1,
                holdDurationSeconds: 900,
                holdPolicy,
                CapacityOversellPolicyEnum.Disallow,
                isActive: true)
            : null;
        EventTicketType ticket = EventTicketType.Create(
            Guid.CreateVersion7(), _tenantId, catalog.Id, "Admission", "USD",
            unitPriceMinor == 0 ? TicketPricingModeEnum.Free : TicketPricingModeEnum.Fixed,
            unitPriceMinor == 0 ? null : Money.Create(unitPriceMinor, "USD"), null, null,
            participantMode, pool?.Id, null, null, false, false, null, null, null, perBookingPartyLimit);
        catalog.AddTicketType(ticket, pool);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, _tenantId, _eventId, 1));
        catalog.UpdateCommercialDisclosures("Merchant", "Refund", "Support");
        catalog.Publish();
        RegistrationOrder order = RegistrationOrder.Create(
            _tenantId, _eventId, Guid.CreateVersion7(), purchaserActorId, BookingPartyTypeEnum.Individual, catalog.Id,
            RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            registrationWorkflowId, null, "USD", UtcNow, UtcNow.AddMinutes(15));
        if (addLine)
        {
            order.AddLine(RegistrationOrderLine.Create(catalog, ticket, order.Id, quantity, null, null));
            long lineTotal = checked(unitPriceMinor * quantity);
            order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create("USD", lineTotal, 0, lineTotal, 0));
        }

        return (order, catalog, ticket);
    }

    private (RegistrationOrder Order, EventTicketCatalogVersion Catalog) CreateTwoLineCapacityOrder()
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1);
        EventCapacityPool pool = EventCapacityPool.Create(
            _tenantId,
            _eventId,
            "Registration capacity",
            maximumQuantity: 2,
            holdDurationSeconds: 900,
            holdPolicy: CapacityHoldPolicyEnum.NoHoldUntilReady,
            oversellPolicy: CapacityOversellPolicyEnum.Disallow,
            isActive: true);
        EventTicketType firstTicket = CreateCapacityTicket(catalog, pool, "First admission");
        EventTicketType secondTicket = CreateCapacityTicket(catalog, pool, "Second admission");
        catalog.AddTicketType(firstTicket, pool);
        catalog.AddTicketType(secondTicket, pool);
        catalog.AddEntitlement(firstTicket, TicketTypeEntitlement.CreateForEvent(firstTicket.Id, _tenantId, _eventId, 1));
        catalog.AddEntitlement(secondTicket, TicketTypeEntitlement.CreateForEvent(secondTicket.Id, _tenantId, _eventId, 1));
        catalog.UpdateCommercialDisclosures("Merchant", "Refund", "Support");
        catalog.Publish();
        RegistrationOrder order = RegistrationOrder.Create(
            _tenantId, _eventId, Guid.CreateVersion7(), null, BookingPartyTypeEnum.Individual, catalog.Id,
            RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            null, null, "USD", UtcNow, UtcNow.AddMinutes(15));
        order.AddLine(RegistrationOrderLine.Create(catalog, firstTicket, order.Id, 1, null, null));
        order.AddLine(RegistrationOrderLine.Create(catalog, secondTicket, order.Id, 1, null, null));
        order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create("USD", 0, 0, 0, 0));
        return (order, catalog);
    }

    private (RegistrationOrder Order, EventTicketCatalogVersion Catalog, RegistrationInventoryHold TimedHold) CreateMixedPolicyOrder()
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1);
        EventCapacityPool timedPool = EventCapacityPool.Create(
            _tenantId,
            _eventId,
            "Timed capacity",
            maximumQuantity: 1,
            holdDurationSeconds: 900,
            holdPolicy: CapacityHoldPolicyEnum.TimedHoldOnSelection,
            oversellPolicy: CapacityOversellPolicyEnum.Disallow,
            isActive: true);
        EventCapacityPool waitlistPool = EventCapacityPool.Create(
            _tenantId,
            _eventId,
            "Waitlist capacity",
            maximumQuantity: 1,
            holdDurationSeconds: 900,
            holdPolicy: CapacityHoldPolicyEnum.WaitlistWhenFull,
            oversellPolicy: CapacityOversellPolicyEnum.Disallow,
            isActive: true);
        EventTicketType timedTicket = CreateCapacityTicket(catalog, timedPool, "Timed admission");
        EventTicketType waitlistTicket = CreateCapacityTicket(catalog, waitlistPool, "Waitlist admission");
        catalog.AddTicketType(timedTicket, timedPool);
        catalog.AddTicketType(waitlistTicket, waitlistPool);
        catalog.AddEntitlement(timedTicket, TicketTypeEntitlement.CreateForEvent(timedTicket.Id, _tenantId, _eventId, 1));
        catalog.AddEntitlement(waitlistTicket, TicketTypeEntitlement.CreateForEvent(waitlistTicket.Id, _tenantId, _eventId, 1));
        catalog.UpdateCommercialDisclosures("Merchant", "Refund", "Support");
        catalog.Publish();

        RegistrationOrder order = RegistrationOrder.Create(
            _tenantId, _eventId, Guid.CreateVersion7(), null, BookingPartyTypeEnum.Individual, catalog.Id,
            RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            null, null, "USD", UtcNow, UtcNow.AddMinutes(15));
        order.AddLine(RegistrationOrderLine.Create(catalog, timedTicket, order.Id, 1, null, null));
        order.AddLine(RegistrationOrderLine.Create(catalog, waitlistTicket, order.Id, 1, null, null));
        order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create("USD", 0, 0, 0, 0));
        return (
            order,
            catalog,
            RegistrationInventoryHold.Create(order.Id, timedPool.Id, timedTicket.Id, _tenantId, 1, UtcNow, UtcNow.AddMinutes(15)));
    }

    private EventTicketType CreateCapacityTicket(EventTicketCatalogVersion catalog, EventCapacityPool pool, string name) => EventTicketType.Create(
        Guid.CreateVersion7(),
        _tenantId,
        catalog.Id,
        name,
        "USD",
        TicketPricingModeEnum.Free,
        fixedPrice: null,
        minimumPrice: null,
        suggestedPrice: null,
        ParticipantDataCollectionModeEnum.None,
        pool.Id,
        minimumAge: null,
        maximumAge: null,
        requiresGuardian: false,
        requiresApproval: false,
        perOrderLimit: null,
        perAccountLimit: null,
        perVerifiedContactLimit: null,
        perBookingPartyLimit: null);

    private EventSession CreateOpenSession() => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = _eventId,
        Event = null!,
        TenantId = _tenantId,
        Tenant = null!,
        RegistrationModeId = (int)RegistrationModeEnum.Open
    };

    private static void MoveTo(RegistrationOrder order, RegistrationOrderStatusEnum target)
    {
        foreach (RegistrationOrderStatusEnum status in new[]
                 {
                     RegistrationOrderStatusEnum.AwaitingParticipantDetails,
                     RegistrationOrderStatusEnum.AwaitingRequirements,
                     RegistrationOrderStatusEnum.AwaitingApproval,
                     RegistrationOrderStatusEnum.ReadyForCheckout
                 })
        {
            if (order.RegistrationOrderStatusId == (int)target)
            {
                return;
            }

            order.TransitionTo(status, UtcNow);
            if (status == target)
            {
                return;
            }
        }
    }

    private static void MoveToAwaitingPayment(RegistrationOrder order)
    {
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        order.TransitionTo(RegistrationOrderStatusEnum.AwaitingPayment, UtcNow);
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

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public int TransactionCount { get; private set; }
        public int SerializableCount { get; private set; }
        public CancellationToken LastSerializableToken { get; private set; }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
        {
            TransactionCount++;
            return operation(ct);
        }

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            TransactionCount++;
            return operation(ct);
        }

        public Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            SerializableCount++;
            LastSerializableToken = ct;
            return operation(ct);
        }
    }

    private sealed class RetryingUnitOfWork : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) =>
            ExecuteInTransactionAsync<object?>(async token =>
            {
                await operation(token);
                return null;
            }, ct);

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            await operation(ct);
            return await operation(ct);
        }

        public async Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            await operation(ct);
            return await operation(ct);
        }
    }

    private sealed class RollbackTrackingUnitOfWork : IUnitOfWork
    {
        public int RollbackCount { get; private set; }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) => operation(ct);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);

        public async Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            try
            {
                return await operation(ct);
            }
            catch
            {
                RollbackCount++;
                throw;
            }
        }
    }
}
