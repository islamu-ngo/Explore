// ABOUTME: Tests registration-order lifecycle orchestration at its transaction-bound persistence edges.
// ABOUTME: Verifies free admission materialization, hold release, and approval routing without API exposure.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Services.Registration;
using Explore.Domain;
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
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly IPlatformContributionSettingRepository _contributionSettings = Substitute.For<IPlatformContributionSettingRepository>();
    private readonly IEventSessionRepository _sessions = Substitute.For<IEventSessionRepository>();
    private readonly IOutboxRepository _outbox = Substitute.For<IOutboxRepository>();

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

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingRequirements);
        await Assert.That(order.PlatformContribution!.AmountMinor).IsEqualTo(100);
        await Assert.That(order.OrganizerDirectedTotalMinorSnapshot).IsEqualTo(1_000);
        await Assert.That(order.OrganizerEarningsTotalMinorSnapshot).IsEqualTo(1_000);
        await Assert.That(order.TotalDueMinorSnapshot).IsEqualTo(1_100);
        _ = _inventory.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        await Assert.That(result.Success).IsTrue();
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
        _inventory.GetHoldsByOrderAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns([]);
        _inventory.TryTransitionOrderAsync(
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

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.ReadyForCheckout);
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

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.NeedsReconciliation);
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

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.ReadyForCheckout);
        await Assert.That(reservations).HasSingleItem();
        await Assert.That(reservations.Single().TicketTypeId).IsEqualTo(ticket.Id);
        await Assert.That(reservations.Single().CapacityPoolId).IsEqualTo(ticket.CapacityPoolId!.Value);
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

        await Assert.That(result.Success).IsTrue();
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

        await Assert.That(result.Success).IsTrue();
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

        await Assert.That(result.Success).IsTrue();
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

        await Assert.That(result.Success).IsTrue();
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

        await Assert.That(result.Success).IsTrue();
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

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingRequirements);
        await Assert.That(timedHold.IsCapacityAllocated).IsTrue();
        await Assert.That(unitOfWork.RollbackCount).IsEqualTo(1);
        await _inventory.DidNotReceive().TryTransitionOrderAsync(
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

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingRequirements);
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

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.NeedsReconciliation);
        await _inventory.DidNotReceive().TryTransitionOrderAsync(
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
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _sessions.GetSessionsByEvent(_eventId).Returns([CreateOpenSession()]);
        _inventory.TryTransitionOrderAsync(
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

        await Assert.That(result.Success).IsTrue();
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

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Cancelled);
        await _inventory.Received(1).TryReleaseActiveHoldsForOrderAsync(
            order.Id,
            _tenantId,
            RegistrationInventoryHoldStatusEnum.Cancelled,
            UtcNow,
            Arg.Any<CancellationToken>());
        await _outbox.Received(1).Create(Arg.Is<OutboxMessage>(entry => entry.EventType == "RegistrationOrderCancelled"));
    }

    [Test]
    public async Task ApproveAsyncWhenPaidOrderWasAwaitingApprovalRoutesItToPayment()
    {
        (RegistrationOrder order, EventTicketCatalogVersion catalog, _) = CreateOrder(unitPriceMinor: 1_000);
        MoveTo(order, RegistrationOrderStatusEnum.AwaitingApproval);
        ConfigureOrder(order, []);
        _catalogs.GetOrderCatalogAsync(catalog.Id, _eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);

        var result = await CreateService().ApproveAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingPayment);
        await _inventory.Received(1).TryTransitionOrderAsync(
            order.Id,
            _tenantId,
            RegistrationOrderStatusEnum.AwaitingApproval,
            RegistrationOrderStatusEnum.ReadyForCheckout,
            UtcNow,
            Arg.Any<CancellationToken>());
        await _inventory.Received(1).TryTransitionOrderAsync(
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
        _inventory.TryTransitionOrderAsync(
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

        await Assert.That(result.Success).IsTrue();
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

        await Assert.That(result.Success).IsTrue();
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

        await Assert.That(result.Success).IsTrue();
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

        await Assert.That(repeated.Success).IsTrue();
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

        await Assert.That(recovery.Success).IsTrue();
        await Assert.That(finalization.Success).IsTrue();
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

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Order!.StatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Waitlisted);
        await _inventory.DidNotReceive().AddEventRegistrationsAsync(
            Arg.Any<IReadOnlyCollection<EventRegistration>>(),
            Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task RecoverExpiredHoldAsyncWhenAConcurrentAttemptAlreadyResolvedReturnsCurrentStateWithoutASecondReservation()
    {
        (RegistrationOrder order, _, _) = CreateOrder(capacityBacked: true);
        MoveTo(order, RegistrationOrderStatusEnum.ReadyForCheckout);
        ConfigureOrder(order, []);

        var result = await CreateService().RecoverExpiredHoldAsync(order.Id, _tenantId, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
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
        _inventory.TryTransitionOrderAsync(
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

        await Assert.That(result.Success).IsTrue();
        await Assert.That(messages).Count().IsEqualTo(2);
        await Assert.That(messages.Select(message => message.Id).Distinct()).HasSingleItem();
    }

    private RegistrationOrderLifecycleService CreateService(IUnitOfWork? unitOfWork = null) => new(
        _inventory,
        _catalogs,
        _contributionSettings,
        _sessions,
        _outbox,
        unitOfWork ?? new InlineUnitOfWork(),
        new FixedTimeProvider(UtcNow));

    private void ConfigureOrder(RegistrationOrder order, IReadOnlyList<RegistrationInventoryHold> holds)
    {
        _inventory.GetOrderWithLinesAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _inventory.GetHoldsByOrderAsync(order.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(holds);
        _inventory.TryTransitionOrderAsync(
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

    private (RegistrationOrder Order, EventTicketCatalogVersion Catalog, EventTicketType Ticket) CreateOrder(
        bool addLine = true,
        long unitPriceMinor = 0,
        bool capacityBacked = false,
        CapacityHoldPolicyEnum holdPolicy = CapacityHoldPolicyEnum.NoHoldUntilReady)
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
            unitPriceMinor == 0 ? null : unitPriceMinor, null, null,
            ParticipantDataCollectionModeEnum.None, pool?.Id, null, null, false, false, null, null, null, null);
        catalog.AddTicketType(ticket, pool);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, _tenantId, _eventId, 1));
        catalog.Publish();
        RegistrationOrder order = RegistrationOrder.Create(
            _tenantId, _eventId, Guid.CreateVersion7(), null, BookingPartyTypeEnum.Individual, catalog.Id,
            RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
            null, null, "USD", UtcNow, UtcNow.AddMinutes(15));
        if (addLine)
        {
            order.AddLine(RegistrationOrderLine.Create(catalog, ticket, order.Id, 1, null, null));
            order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create("USD", unitPriceMinor, 0, unitPriceMinor, 0));
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
        fixedPriceMinor: null,
        minimumPriceMinor: null,
        suggestedPriceMinor: null,
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
