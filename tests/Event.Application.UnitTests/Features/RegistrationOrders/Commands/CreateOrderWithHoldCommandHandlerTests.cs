// ABOUTME: Tests transaction-safe registration-order creation and capacity-hold orchestration.
// ABOUTME: Proves stable retry identities, waitlisting, and invalid-request rejection without API exposure.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.RegistrationOrders.Handlers.Commands;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = global::Explore.Domain.Event;

namespace ApplicationUnitTests.Features.RegistrationOrders.Commands;

public sealed class CreateOrderWithHoldCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly IRegistrationInventoryRepository _inventory = Substitute.For<IRegistrationInventoryRepository>();
    private readonly IPlatformFeePolicyRepository _feePolicies = Substitute.For<IPlatformFeePolicyRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly Dictionary<Guid, RegistrationOrder> _orders = [];
    private readonly List<(RegistrationOrder Order, IReadOnlyCollection<RegistrationInventoryHold> Holds)> _saved = [];

    public CreateOrderWithHoldCommandHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns(CreatePlatformEvent());
        _feePolicies.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((PlatformFeePolicy?)null);
        _inventory.GetOrderByIdAsync(Arg.Any<Guid>(), _tenantId, Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(_orders.GetValueOrDefault(callInfo.ArgAt<Guid>(0))));
        _inventory.AddOrderWithHoldsAsync(
                Arg.Any<RegistrationOrder>(),
                Arg.Any<IReadOnlyCollection<RegistrationInventoryHold>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                RegistrationOrder order = callInfo.ArgAt<RegistrationOrder>(0);
                IReadOnlyCollection<RegistrationInventoryHold> holds = callInfo.ArgAt<IReadOnlyCollection<RegistrationInventoryHold>>(1);
                _orders[order.Id] = order;
                _saved.Add((order, holds));
                return Task.CompletedTask;
        });
        _inventory.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _inventory.GetTicketLimitUsageAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid?>(),
                Arg.Any<string?>(),
                Arg.Any<Guid?>(),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, RegistrationTicketLimitUsage>());
    }

    [Test]
    public async Task HandleWhenTimedHoldPolicyIsSelectedPersistsOneStableOrderAndHoldAcrossSerializableRetry()
    {
        (EventTicketCatalogVersion catalog, EventTicketType ticket, EventCapacityPool pool) = CreatePublishedCatalog(
            maximumQuantity: 2,
            holdPolicy: CapacityHoldPolicyEnum.TimedHoldOnSelection);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetPoolsForUpdateAsync(Arg.Any<IReadOnlyCollection<Guid>>(), _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns([pool]);
        _inventory.GetAllocatedQuantityAsync(pool.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(0);
        var unitOfWork = new RetryingUnitOfWork(() => _orders.Clear());

        var result = await CreateHandler(unitOfWork).Handle(CreateCommand(catalog.Id, ticket.Id, quantity: 2), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(unitOfWork.Attempts).IsEqualTo(2);
        await Assert.That(_saved).Count().IsEqualTo(2);
        RegistrationOrder firstAttemptOrder = _saved[0].Order;
        RegistrationInventoryHold firstAttemptHold = _saved[0].Holds.Single();
        RegistrationOrder order = _saved[1].Order;
        RegistrationInventoryHold hold = _saved[1].Holds.Single();
        await Assert.That(order.Id).IsEqualTo(firstAttemptOrder.Id);
        await Assert.That(order.Lines.Single().Id).IsEqualTo(firstAttemptOrder.Lines.Single().Id);
        await Assert.That(hold.Id).IsEqualTo(firstAttemptHold.Id);
        await Assert.That(order.Pii).IsNull();
        await Assert.That(order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingParticipantDetails);
        await Assert.That(order.Lines.Single().Quantity).IsEqualTo(2);
        await Assert.That(hold.RegistrationOrderId).IsEqualTo(order.Id);
        await Assert.That(hold.Quantity).IsEqualTo(2);
        await Assert.That(hold.ExpiresAt).IsEqualTo(UtcNow.AddSeconds(pool.HoldDurationSeconds));
    }

    [Test]
    public async Task HandleWhenCapacityIsFullPersistsWaitlistedOrderWithoutAnyHold()
    {
        (EventTicketCatalogVersion catalog, EventTicketType ticket, EventCapacityPool pool) = CreatePublishedCatalog(
            maximumQuantity: 1,
            holdPolicy: CapacityHoldPolicyEnum.WaitlistWhenFull);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetPoolsForUpdateAsync(Arg.Any<IReadOnlyCollection<Guid>>(), _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns([pool]);
        _inventory.GetAllocatedQuantityAsync(pool.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(CreateCommand(catalog.Id, ticket.Id, quantity: 1), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(_saved).HasSingleItem();
        await Assert.That(_saved.Single().Order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Waitlisted);
        await Assert.That(_saved.Single().Holds).IsEmpty();
    }

    [Test]
    [Arguments(CapacityHoldPolicyEnum.NoHoldUntilReady, RegistrationOrderStatusEnum.AwaitingParticipantDetails)]
    [Arguments(CapacityHoldPolicyEnum.ApprovalNoHold, RegistrationOrderStatusEnum.AwaitingApproval)]
    public async Task HandleWhenNonAllocatingPolicyIsSelectedSkipsReservation(
        CapacityHoldPolicyEnum holdPolicy,
        RegistrationOrderStatusEnum expectedStatus)
    {
        (EventTicketCatalogVersion catalog, EventTicketType ticket, EventCapacityPool pool) = CreatePublishedCatalog(
            maximumQuantity: 1,
            holdPolicy: holdPolicy);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetPoolsForUpdateAsync(Arg.Any<IReadOnlyCollection<Guid>>(), _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns([pool]);

        var result = await CreateHandler().Handle(CreateCommand(catalog.Id, ticket.Id, quantity: 1), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(_saved).HasSingleItem();
        await Assert.That(_saved.Single().Order.RegistrationOrderStatusId).IsEqualTo((int)expectedStatus);
        await Assert.That(_saved.Single().Order.ExpiresAt).IsNull();
        await Assert.That(_saved.Single().Holds).IsEmpty();
        await _inventory.Received(1).GetPoolsForUpdateAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            _eventId,
            _tenantId,
            Arg.Any<CancellationToken>());
        await _inventory.DidNotReceive().GetAllocatedQuantityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenWaitlistPolicyHasCapacityChecksAvailabilityWithoutCreatingAnExpiringHold()
    {
        (EventTicketCatalogVersion catalog, EventTicketType ticket, EventCapacityPool pool) = CreatePublishedCatalog(
            maximumQuantity: 1,
            holdPolicy: CapacityHoldPolicyEnum.WaitlistWhenFull);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetPoolsForUpdateAsync(Arg.Any<IReadOnlyCollection<Guid>>(), _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns([pool]);
        _inventory.GetAllocatedQuantityAsync(pool.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(0);

        var result = await CreateHandler().Handle(CreateCommand(catalog.Id, ticket.Id, quantity: 1), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(_saved).HasSingleItem();
        await Assert.That(_saved.Single().Order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.AwaitingParticipantDetails);
        await Assert.That(_saved.Single().Order.ExpiresAt).IsNull();
        await Assert.That(_saved.Single().Holds).IsEmpty();
        await _inventory.Received(1).GetAllocatedQuantityAsync(pool.Id, _tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenTimedPoolIsFullAndAnotherPoolCanWaitlistRejectsWithoutReservation()
    {
        var (catalog, timed, waitlist) = CreateTwoPoolPublishedCatalog(
            CapacityHoldPolicyEnum.TimedHoldOnSelection,
            CapacityHoldPolicyEnum.WaitlistWhenFull);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetPoolsForUpdateAsync(Arg.Any<IReadOnlyCollection<Guid>>(), _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns([timed.Pool, waitlist.Pool]);
        _inventory.GetAllocatedQuantityAsync(timed.Pool.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(1);
        _inventory.GetAllocatedQuantityAsync(waitlist.Pool.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(0);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            CreateMultiLineCommand(
                catalog.Id,
                new RegistrationOrderLineSelection(timed.Ticket.Id, 1, null),
                new RegistrationOrderLineSelection(waitlist.Ticket.Id, 1, null)),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_capacity_unavailable");
        await Assert.That(_saved).IsEmpty();
    }

    [Test]
    public async Task HandleWhenOnlyWaitlistPoolIsFullPersistsWaitlistedOrderWithoutTimedHold()
    {
        var (catalog, timed, waitlist) = CreateTwoPoolPublishedCatalog(
            CapacityHoldPolicyEnum.TimedHoldOnSelection,
            CapacityHoldPolicyEnum.WaitlistWhenFull);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetPoolsForUpdateAsync(Arg.Any<IReadOnlyCollection<Guid>>(), _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns([timed.Pool, waitlist.Pool]);
        _inventory.GetAllocatedQuantityAsync(timed.Pool.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(0);
        _inventory.GetAllocatedQuantityAsync(waitlist.Pool.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(1);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            CreateMultiLineCommand(
                catalog.Id,
                new RegistrationOrderLineSelection(timed.Ticket.Id, 1, null),
                new RegistrationOrderLineSelection(waitlist.Ticket.Id, 1, null)),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(_saved).HasSingleItem();
        await Assert.That(_saved.Single().Order.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.Waitlisted);
        await Assert.That(_saved.Single().Order.ExpiresAt).IsNull();
        await Assert.That(_saved.Single().Holds).IsEmpty();
        await _inventory.Received(1).GetAllocatedQuantityAsync(timed.Pool.Id, _tenantId, Arg.Any<CancellationToken>());
        await _inventory.Received(1).GetAllocatedQuantityAsync(waitlist.Pool.Id, _tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(CapacityHoldPolicyEnum.NoHoldUntilReady, RegistrationOrderStatusEnum.AwaitingParticipantDetails)]
    [Arguments(CapacityHoldPolicyEnum.ApprovalNoHold, RegistrationOrderStatusEnum.AwaitingApproval)]
    public async Task HandleWhenTimedAndNonAllocatingPoolsHaveCapacityCreatesOnlyTimedHold(
        CapacityHoldPolicyEnum nonAllocatingPolicy,
        RegistrationOrderStatusEnum expectedStatus)
    {
        var (catalog, timed, noHold) = CreateTwoPoolPublishedCatalog(
            CapacityHoldPolicyEnum.TimedHoldOnSelection,
            nonAllocatingPolicy);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetPoolsForUpdateAsync(Arg.Any<IReadOnlyCollection<Guid>>(), _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns([timed.Pool, noHold.Pool]);
        _inventory.GetAllocatedQuantityAsync(timed.Pool.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(0);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            CreateMultiLineCommand(
                catalog.Id,
                new RegistrationOrderLineSelection(timed.Ticket.Id, 1, null),
                new RegistrationOrderLineSelection(noHold.Ticket.Id, 1, null)),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(_saved).HasSingleItem();
        await Assert.That(_saved.Single().Order.RegistrationOrderStatusId).IsEqualTo((int)expectedStatus);
        await Assert.That(_saved.Single().Holds).HasSingleItem();
        await Assert.That(_saved.Single().Holds.Single().CapacityPoolId).IsEqualTo(timed.Pool.Id);
        await _inventory.Received(1).GetAllocatedQuantityAsync(timed.Pool.Id, _tenantId, Arg.Any<CancellationToken>());
        await _inventory.DidNotReceive().GetAllocatedQuantityAsync(noHold.Pool.Id, _tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(CapacityHoldPolicyEnum.NoHoldUntilReady)]
    [Arguments(CapacityHoldPolicyEnum.ApprovalNoHold)]
    public async Task HandleWhenTimedPoolIsFullAndAnotherPoolIsNonAllocatingRejectsWithoutReservation(
        CapacityHoldPolicyEnum nonAllocatingPolicy)
    {
        var (catalog, timed, nonAllocating) = CreateTwoPoolPublishedCatalog(
            CapacityHoldPolicyEnum.TimedHoldOnSelection,
            nonAllocatingPolicy);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetPoolsForUpdateAsync(Arg.Any<IReadOnlyCollection<Guid>>(), _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns([timed.Pool, nonAllocating.Pool]);
        _inventory.GetAllocatedQuantityAsync(timed.Pool.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(1);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            CreateMultiLineCommand(
                catalog.Id,
                new RegistrationOrderLineSelection(timed.Ticket.Id, 1, null),
                new RegistrationOrderLineSelection(nonAllocating.Ticket.Id, 1, null)),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_capacity_unavailable");
        await Assert.That(_saved).IsEmpty();
        await _inventory.DidNotReceive().GetAllocatedQuantityAsync(nonAllocating.Pool.Id, _tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenWaitlistPoolIsFullAndAnotherPoolRequiresApprovalRejectsIncompatibleOrderState()
    {
        var (catalog, waitlist, approval) = CreateTwoPoolPublishedCatalog(
            CapacityHoldPolicyEnum.WaitlistWhenFull,
            CapacityHoldPolicyEnum.ApprovalNoHold);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetPoolsForUpdateAsync(Arg.Any<IReadOnlyCollection<Guid>>(), _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns([waitlist.Pool, approval.Pool]);
        _inventory.GetAllocatedQuantityAsync(waitlist.Pool.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(1);

        BaseCommandResponse<Guid> result = await CreateHandler().Handle(
            CreateMultiLineCommand(
                catalog.Id,
                new RegistrationOrderLineSelection(waitlist.Ticket.Id, 1, null),
                new RegistrationOrderLineSelection(approval.Ticket.Id, 1, null)),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_policy_incompatible");
        await Assert.That(_saved).IsEmpty();
        await _inventory.DidNotReceive().GetAllocatedQuantityAsync(approval.Pool.Id, _tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenLineQuantityIsInvalidReturnsValidationFailureBeforeTransactionOrPersistence()
    {
        var unitOfWork = new RetryingUnitOfWork();

        var result = await CreateHandler(unitOfWork).Handle(CreateCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), quantity: 0), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_validation_failed");
        await Assert.That(unitOfWork.Attempts).IsEqualTo(0);
        await _inventory.DidNotReceive().AddOrderWithHoldsAsync(
            Arg.Any<RegistrationOrder>(),
            Arg.Any<IReadOnlyCollection<RegistrationInventoryHold>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenAccountContactOrBookingLimitWouldBeExceededReturnsLimitFailureWithoutReservation()
    {
        (EventTicketCatalogVersion catalog, EventTicketType ticket, EventCapacityPool _) = CreatePublishedCatalog(
            maximumQuantity: 2,
            perAccountLimit: 1);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetTicketLimitUsageAsync(
                _eventId,
                _tenantId,
                Arg.Any<Guid?>(),
                Arg.Any<string?>(),
                Arg.Any<Guid?>(),
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, RegistrationTicketLimitUsage>
            {
                [ticket.Id] = new(ticket.Id, AccountQuantity: 1, VerifiedContactQuantity: 0, BookingPartyQuantity: 0)
            });

        var result = await CreateHandler().Handle(CreateCommand(catalog.Id, ticket.Id, quantity: 1, accountUserId: Guid.CreateVersion7()), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_limit_exceeded");
        await _inventory.DidNotReceive().AddOrderWithHoldsAsync(
            Arg.Any<RegistrationOrder>(),
            Arg.Any<IReadOnlyCollection<RegistrationInventoryHold>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenPublishedCatalogDoesNotMatchPinnedVersionRejectsWithoutReservation()
    {
        (EventTicketCatalogVersion catalog, EventTicketType ticket, EventCapacityPool _) = CreatePublishedCatalog(maximumQuantity: 2);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);

        var result = await CreateHandler().Handle(
            CreateCommand(Guid.CreateVersion7(), ticket.Id, quantity: 1),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _inventory.DidNotReceive().GetPoolsForUpdateAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
        await _inventory.DidNotReceive().AddOrderWithHoldsAsync(
            Arg.Any<RegistrationOrder>(),
            Arg.Any<IReadOnlyCollection<RegistrationInventoryHold>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenTicketTypeIsNotInPinnedCatalogRejectsBeforePoolLock()
    {
        (EventTicketCatalogVersion catalog, EventTicketType _, EventCapacityPool _) = CreatePublishedCatalog(maximumQuantity: 2);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);

        var result = await CreateHandler().Handle(
            CreateCommand(catalog.Id, Guid.CreateVersion7(), quantity: 1),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _inventory.DidNotReceive().GetPoolsForUpdateAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenChosenPriceViolatesPinnedTicketRulesRejectsWithoutReservation()
    {
        (EventTicketCatalogVersion catalog, EventTicketType ticket, EventCapacityPool pool) = CreatePublishedCatalog(maximumQuantity: 2);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetPoolsForUpdateAsync(Arg.Any<IReadOnlyCollection<Guid>>(), _eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns([pool]);
        _inventory.GetAllocatedQuantityAsync(pool.Id, _tenantId, Arg.Any<CancellationToken>()).Returns(0);

        var result = await CreateHandler().Handle(
            CreateCommand(catalog.Id, ticket.Id, quantity: 1, chosenUnitPriceMinor: 1),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _inventory.DidNotReceive().AddOrderWithHoldsAsync(
            Arg.Any<RegistrationOrder>(),
            Arg.Any<IReadOnlyCollection<RegistrationInventoryHold>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenVerifiedContactLimitWouldBeExceededRejectsWithoutReservation()
    {
        (EventTicketCatalogVersion catalog, EventTicketType ticket, EventCapacityPool _) = CreatePublishedCatalog(
            maximumQuantity: 2,
            perVerifiedContactLimit: 1);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetTicketLimitUsageAsync(
                _eventId,
                _tenantId,
                null,
                "BUYER@EXAMPLE.TEST",
                null,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, RegistrationTicketLimitUsage>
            {
                [ticket.Id] = new(ticket.Id, AccountQuantity: 0, VerifiedContactQuantity: 1, BookingPartyQuantity: 0)
            });

        var result = await CreateHandler().Handle(
            CreateCommand(catalog.Id, ticket.Id, quantity: 1, verifiedContactNormalizedEmail: "buyer@example.test"),
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("registration_order_limit_exceeded");
        await _inventory.DidNotReceive().AddOrderWithHoldsAsync(
            Arg.Any<RegistrationOrder>(),
            Arg.Any<IReadOnlyCollection<RegistrationInventoryHold>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenBookingPartyLimitWouldBeExceededRejectsWithoutReservation()
    {
        Guid purchaserActorId = Guid.CreateVersion7();
        (EventTicketCatalogVersion catalog, EventTicketType ticket, EventCapacityPool _) = CreatePublishedCatalog(
            maximumQuantity: 2,
            perBookingPartyLimit: 1);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        _inventory.GetTicketLimitUsageAsync(
                _eventId,
                _tenantId,
                null,
                null,
                purchaserActorId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, RegistrationTicketLimitUsage>
            {
                [ticket.Id] = new(ticket.Id, AccountQuantity: 0, VerifiedContactQuantity: 0, BookingPartyQuantity: 1)
            });

        var result = await CreateHandler().Handle(
            CreateCommand(catalog.Id, ticket.Id, quantity: 1, purchaserActorId: purchaserActorId),
            CancellationToken.None);

        await Assert.That(result.FailureCode).IsEqualTo("registration_order_limit_exceeded");
        await _inventory.DidNotReceive().AddOrderWithHoldsAsync(
            Arg.Any<RegistrationOrder>(),
            Arg.Any<IReadOnlyCollection<RegistrationInventoryHold>>(),
            Arg.Any<CancellationToken>());
    }

    private CreateOrderWithHoldCommandHandler CreateHandler(IUnitOfWork? unitOfWork = null) => new(
        _events,
        _catalogs,
        _inventory,
        _feePolicies,
        _tenant,
        new OrganizerEarningsCalculator(),
        new FixedTimeProvider(UtcNow),
        unitOfWork ?? new RetryingUnitOfWork());

    private CreateRegistrationOrderWithHoldCommand CreateCommand(
        Guid ticketCatalogVersionId,
        Guid ticketTypeId,
        int quantity,
        Guid? accountUserId = null,
        string? verifiedContactNormalizedEmail = null,
        Guid? purchaserActorId = null,
        long? chosenUnitPriceMinor = null) => new()
    {
        EventId = _eventId,
        TicketCatalogVersionId = ticketCatalogVersionId,
        AccountUserId = accountUserId,
        PurchaserActorId = purchaserActorId,
        VerifiedContactNormalizedEmail = verifiedContactNormalizedEmail,
        BookingPartyType = BookingPartyTypeEnum.Individual,
        GuestAccessTokenHash = CapabilityTokenHash.Create(Convert.ToBase64String(new byte[32])),
        Lines = [new RegistrationOrderLineSelection(ticketTypeId, quantity, chosenUnitPriceMinor)]
    };

    private CreateRegistrationOrderWithHoldCommand CreateMultiLineCommand(
        Guid ticketCatalogVersionId,
        params RegistrationOrderLineSelection[] lines) => new()
    {
        EventId = _eventId,
        TicketCatalogVersionId = ticketCatalogVersionId,
        BookingPartyType = BookingPartyTypeEnum.Individual,
        GuestAccessTokenHash = CapabilityTokenHash.Create(Convert.ToBase64String(new byte[32])),
        Lines = lines
    };

    private DomainEvent CreatePlatformEvent() => new()
    {
        Id = _eventId,
        TenantId = _tenantId,
        Title = "Registration event",
        Actor = null!,
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!,
        ParticipationConfiguration = CreateParticipationConfiguration()
    };

    private EventParticipationConfiguration CreateParticipationConfiguration()
    {
        EventParticipationConfiguration configuration = EventParticipationConfiguration.Create(
            _eventId,
            _tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged,
            (int)AdvanceRegistrationObligationEnum.Required,
            (int)IdentityAccessModeEnum.CapabilityTokenAllowed,
            GuestRecoveryPolicyEnum.CapabilityLinkOnly,
            UtcNow);
        configuration.ConcurrencyStamp = Guid.CreateVersion7();
        return configuration;
    }

    private (EventTicketCatalogVersion Catalog, EventTicketType Ticket, EventCapacityPool Pool) CreatePublishedCatalog(
        int maximumQuantity,
        CapacityHoldPolicyEnum holdPolicy = CapacityHoldPolicyEnum.TimedHoldOnSelection,
        int? perAccountLimit = null,
        int? perVerifiedContactLimit = null,
        int? perBookingPartyLimit = null)
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1);
        EventCapacityPool pool = EventCapacityPool.Create(
            _tenantId,
            _eventId,
            "Hall",
            maximumQuantity,
            900,
            holdPolicy,
            CapacityOversellPolicyEnum.Disallow,
            true);
        EventTicketType ticket = EventTicketType.Create(
            Guid.CreateVersion7(),
            _tenantId,
            catalog.Id,
            "Admission",
            "USD",
            TicketPricingModeEnum.Free,
            null,
            null,
            null,
            ParticipantDataCollectionModeEnum.None,
            pool.Id,
            null,
            null,
            false,
            false,
            null,
            perAccountLimit,
            perVerifiedContactLimit,
            perBookingPartyLimit);
        catalog.AddTicketType(ticket, pool);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, _tenantId, _eventId, 1));
        catalog.Publish();
        return (catalog, ticket, pool);
    }

    private (
        EventTicketCatalogVersion Catalog,
        (EventTicketType Ticket, EventCapacityPool Pool) First,
        (EventTicketType Ticket, EventCapacityPool Pool) Second) CreateTwoPoolPublishedCatalog(
        CapacityHoldPolicyEnum firstPolicy,
        CapacityHoldPolicyEnum secondPolicy)
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1);
        (EventTicketType Ticket, EventCapacityPool Pool) first = AddTicketType(catalog, "First", firstPolicy);
        (EventTicketType Ticket, EventCapacityPool Pool) second = AddTicketType(catalog, "Second", secondPolicy);
        catalog.Publish();
        return (catalog, first, second);
    }

    private (EventTicketType Ticket, EventCapacityPool Pool) AddTicketType(
        EventTicketCatalogVersion catalog,
        string name,
        CapacityHoldPolicyEnum holdPolicy)
    {
        EventCapacityPool pool = EventCapacityPool.Create(
            _tenantId,
            _eventId,
            $"{name} pool",
            1,
            900,
            holdPolicy,
            CapacityOversellPolicyEnum.Disallow,
            true);
        EventTicketType ticket = EventTicketType.Create(
            Guid.CreateVersion7(),
            _tenantId,
            catalog.Id,
            $"{name} admission",
            "USD",
            TicketPricingModeEnum.Free,
            null,
            null,
            null,
            ParticipantDataCollectionModeEnum.None,
            pool.Id,
            null,
            null,
            false,
            false,
            null,
            null,
            null,
            null);
        catalog.AddTicketType(ticket, pool);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, _tenantId, _eventId, 1));
        return (ticket, pool);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class RetryingUnitOfWork(Action? betweenAttempts = null) : IUnitOfWork
    {
        public int Attempts { get; private set; }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default) =>
            ExecuteInTransactionAsync<object?>(async token =>
            {
                await operation(token);
                return null;
            }, ct);

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);

        public async Task<T> ExecuteSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        {
            Attempts++;
            await operation(ct);
            betweenAttempts?.Invoke();
            Attempts++;
            return await operation(ct);
        }
    }
}
