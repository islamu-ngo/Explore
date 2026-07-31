// ABOUTME: Tests guest-capability and account-scoped registration-order CQRS access wrappers.
// ABOUTME: Proves generic absence semantics, one-time token issuance, and lifecycle delegation.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Authorization;
using Explore.Application.Behaviors;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Exceptions;
using Explore.Application.Features.RegistrationOrders.Handlers.Commands;
using Explore.Application.Features.RegistrationOrders.Handlers.Queries;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Features.RegistrationOrders.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using NSubstitute;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TUnit.Assertions;
using TUnit.Core;

namespace ApplicationUnitTests.Features.RegistrationOrders;

public sealed class RegistrationOrderAccessHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly Guid _orderId = Guid.CreateVersion7();
    private readonly IRegistrationInventoryRepository _inventory = Substitute.For<IRegistrationInventoryRepository>();
    private readonly IRegistrationOrderLifecycleService _lifecycle = Substitute.For<IRegistrationOrderLifecycleService>();
    private readonly IGuestCapabilityTokenService _capabilities = Substitute.For<IGuestCapabilityTokenService>();
    private readonly IRegistrationOrderStarter _starter = Substitute.For<IRegistrationOrderStarter>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    public RegistrationOrderAccessHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
    }

    [Test]
    public async Task GetGuestRegistrationOrderWhenScopeTokenAndExpiryAreValidReturnsSafeOrder()
    {
        RegistrationOrder order = CreateGuestOrder();
        RegistrationOrderDto dto = RegistrationOrderDto.From(order);
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _capabilities.Matches("guest-token", order.GuestAccessTokenHash!).Returns(true);
        _lifecycle.GetAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(dto);

        GuestRegistrationOrderDto? result = await CreateGuestQueryHandler().Handle(
            new GetGuestRegistrationOrderQuery(_eventId, _orderId, "guest-token"),
            CancellationToken.None);

        await Assert.That(result!.Id).IsEqualTo(dto.Id);
        _ = _capabilities.Received(1).Matches("guest-token", order.GuestAccessTokenHash!);
        _ = _lifecycle.Received(1).GetAsync(_orderId, _tenantId, CancellationToken.None);
    }

    [Test]
    public async Task GetGuestRegistrationOrderWhenTokenIsExpiredReturnsGenericAbsenceWithoutComparison()
    {
        RegistrationOrder order = CreateGuestOrder(expiresAt: UtcNow.AddSeconds(-1));
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);

        GuestRegistrationOrderDto? result = await CreateGuestQueryHandler().Handle(
            new GetGuestRegistrationOrderQuery(_eventId, _orderId, "expired-token"),
            CancellationToken.None);

        await Assert.That(result).IsNull();
        _ = _capabilities.DidNotReceive().Matches(Arg.Any<string?>(), Arg.Any<CapabilityTokenHash>());
        _ = _lifecycle.DidNotReceive().GetAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetGuestRegistrationOrderWhenCapabilityTokenIsMalformedReturnsGenericAbsenceWithoutComparison()
    {
        RegistrationOrder order = CreateGuestOrder();
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);

        GuestRegistrationOrderDto? result = await CreateGuestQueryHandler().Handle(
            new GetGuestRegistrationOrderQuery(_eventId, _orderId, string.Empty),
            CancellationToken.None);

        await Assert.That(result).IsNull();
        _ = _capabilities.DidNotReceive().Matches(Arg.Any<string?>(), Arg.Any<CapabilityTokenHash>());
    }

    [Test]
    public async Task ContinueGuestRegistrationOrderWhenOrderBelongsToAnotherEventReturnsGenericNotFound()
    {
        RegistrationOrder order = CreateGuestOrder(eventId: Guid.CreateVersion7());
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);

        GuestRegistrationOrderLifecycleResponse result = await CreateGuestContinueHandler().Handle(
            new ContinueGuestRegistrationOrderCommand(_eventId, _orderId, "guessed-token"),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_not_found");
        _ = _capabilities.DidNotReceive().Matches(Arg.Any<string?>(), Arg.Any<CapabilityTokenHash>());
        _ = _lifecycle.DidNotReceive().SubmitAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ContinueGuestRegistrationOrderForwardsContributionAfterCapabilityValidation()
    {
        RegistrationOrder order = CreateGuestOrder();
        RegistrationOrderLifecycleResponse expected = new() { Id = _orderId, Success = true };
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _capabilities.Matches("guest-token", order.GuestAccessTokenHash!).Returns(true);
        _lifecycle.SubmitAsync(_orderId, _tenantId, 500, Arg.Any<CancellationToken>()).Returns(expected);

        GuestRegistrationOrderLifecycleResponse result = await CreateGuestContinueHandler().Handle(
            new ContinueGuestRegistrationOrderCommand(_eventId, _orderId, "guest-token", 500),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        _ = _lifecycle.Received(1).SubmitAsync(_orderId, _tenantId, 500, CancellationToken.None);
    }

    [Test]
    [Arguments(GuestLifecycleAction.Finalize)]
    [Arguments(GuestLifecycleAction.Cancel)]
    public async Task GuestLifecycleActionWhenCapabilityIsValidDelegatesToExistingLifecycleService(GuestLifecycleAction action)
    {
        RegistrationOrder order = CreateGuestOrder();
        RegistrationOrderLifecycleResponse expected = new() { Id = _orderId, Success = true };
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _capabilities.Matches("guest-token", order.GuestAccessTokenHash!).Returns(true);
        if (action == GuestLifecycleAction.Finalize)
        {
            _lifecycle.FinalizeFreeAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(expected);
        }
        else
        {
            _lifecycle.CancelAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(expected);
        }

        GuestRegistrationOrderLifecycleResponse result = action == GuestLifecycleAction.Finalize
            ? await new FinalizeGuestRegistrationOrderCommandHandler(
                _inventory,
                _lifecycle,
                _capabilities,
                _tenant,
                new FixedTimeProvider(UtcNow)).Handle(
                new FinalizeGuestRegistrationOrderCommand(_eventId, _orderId, "guest-token"),
                CancellationToken.None)
            : await new CancelGuestRegistrationOrderCommandHandler(
                _inventory,
                _lifecycle,
                _capabilities,
                _tenant,
                new FixedTimeProvider(UtcNow)).Handle(
                new CancelGuestRegistrationOrderCommand(_eventId, _orderId, "guest-token"),
                CancellationToken.None);

        await Assert.That(result.Id).IsEqualTo(expected.Id);
        await Assert.That(result.Success).IsEqualTo(expected.Success);
        if (action == GuestLifecycleAction.Finalize)
        {
            _ = _lifecycle.Received(1).FinalizeFreeAsync(_orderId, _tenantId, CancellationToken.None);
        }
        else
        {
            _ = _lifecycle.Received(1).CancelAsync(_orderId, _tenantId, CancellationToken.None);
        }
    }

    [Test]
    public async Task StartGuestRegistrationOrderIssuesPlaintextOnceAndPersistsOnlyItsHash()
    {
        CapabilityTokenHash hash = CapabilityTokenHash.Create(Convert.ToBase64String(new byte[32]));
        CreateRegistrationOrderWithHoldCommand? started = null;
        _capabilities.Issue().Returns(new GuestCapabilityTokenIssue("guest-token", hash));
        _starter.StartAsync(Arg.Any<CreateRegistrationOrderWithHoldCommand>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                started = call.ArgAt<CreateRegistrationOrderWithHoldCommand>(0);
                return new Explore.Application.Responses.BaseCommandResponse<Guid>
                {
                    Id = _orderId,
                    Success = true,
                    Message = "Registration order created."
                };
            });

        GuestRegistrationOrderStartDto result = await new StartGuestRegistrationOrderCommandHandler(_starter, _capabilities).Handle(
            new StartGuestRegistrationOrderCommand(
                _eventId,
                Guid.CreateVersion7(),
                BookingPartyTypeEnum.Individual,
                [new RegistrationOrderLineSelection(Guid.CreateVersion7(), 1, null)],
                PlatformContributionBasisPoints: 500),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.GuestCapabilityToken).IsEqualTo("guest-token");
        await Assert.That(started).IsNotNull();
        await Assert.That(started!.AccountUserId).IsNull();
        await Assert.That(started.GuestAccessTokenHash).IsEqualTo(hash);
        await Assert.That(started.PlatformContributionBasisPoints).IsEqualTo(500);
        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await Assert.That(json).DoesNotContain("guestCapabilityToken");
        await Assert.That(json).DoesNotContain("guest-token");
    }

    [Test]
    public async Task GuestRegistrationOrderDtoSerializationOmitsAccountAndPurchaserActorIdentifiers()
    {
        Guid accountUserId = Guid.CreateVersion7();
        Guid purchaserActorId = Guid.CreateVersion7();
        RegistrationOrderDto internalOrder = new()
        {
            Id = _orderId,
            EventId = _eventId,
            AccountUserId = accountUserId,
            PurchaserActorId = purchaserActorId,
            StatusCode = "DRAFT",
            StatusName = "Draft"
        };

        string json = JsonSerializer.Serialize(GuestRegistrationOrderDto.From(internalOrder));

        await Assert.That(json.Contains(accountUserId.ToString(), StringComparison.OrdinalIgnoreCase)).IsFalse();
        await Assert.That(json.Contains(purchaserActorId.ToString(), StringComparison.OrdinalIgnoreCase)).IsFalse();
    }

    [Test]
    public async Task StartAuthenticatedRegistrationOrderWhenAnonymousRejectsWithoutStartingOrCreatingAnAccount()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        Explore.Application.Responses.BaseCommandResponse<Guid> result = await new StartAuthenticatedRegistrationOrderCommandHandler(
            _starter,
            _currentUser).Handle(
            new StartAuthenticatedRegistrationOrderCommand(
                _eventId,
                Guid.CreateVersion7(),
                BookingPartyTypeEnum.Individual,
                [new RegistrationOrderLineSelection(Guid.CreateVersion7(), 1, null)]),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_authentication_required");
        _ = await _starter.DidNotReceive().StartAsync(Arg.Any<CreateRegistrationOrderWithHoldCommand>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CancelAuthenticatedRegistrationOrderWhenAccountDoesNotOwnOrderReturnsGenericNotFound()
    {
        RegistrationOrder order = CreateGuestOrder(accountUserId: Guid.CreateVersion7(), guestHash: null);
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(Guid.CreateVersion7());
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);

        RegistrationOrderLifecycleResponse result = await new CancelAuthenticatedRegistrationOrderCommandHandler(
            _inventory,
            _lifecycle,
            _tenant,
            _currentUser).Handle(new CancelAuthenticatedRegistrationOrderCommand(_eventId, _orderId), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_not_found");
        _ = _lifecycle.DidNotReceive().CancelAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ContinueAuthenticatedRegistrationOrderWhenRouteEventDoesNotMatchRejectsBeforeMutation()
    {
        Guid userId = Guid.CreateVersion7();
        RegistrationOrder order = CreateGuestOrder(eventId: Guid.CreateVersion7(), accountUserId: userId, guestHash: null);
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);

        RegistrationOrderLifecycleResponse result = await new ContinueAuthenticatedRegistrationOrderCommandHandler(
            _inventory,
            _lifecycle,
            _tenant,
            _currentUser).Handle(
            new ContinueAuthenticatedRegistrationOrderCommand(_eventId, _orderId, 500),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_not_found");
        _ = _lifecycle.DidNotReceive().SubmitAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetEventRegistrationOrdersQueryWhenManageRegistrationsIsDeniedDoesNotInvokeHandler()
    {
        var authorization = Substitute.For<IAuthorizationProvider>();
        var behavior = new AuthorizationBehavior<GetEventRegistrationOrdersQuery, IReadOnlyList<RegistrationOrderDto>>(
            authorization,
            Substitute.For<ILogger<AuthorizationBehavior<GetEventRegistrationOrdersQuery, IReadOnlyList<RegistrationOrderDto>>>>());
        var query = new GetEventRegistrationOrdersQuery(_eventId);
        var handlerInvoked = false;

        authorization.IsAllowedAsync(
                ResourceKinds.Event,
                _eventId.ToString(),
                AuthorizationActions.Events.ManageRegistrations,
                Arg.Is<IDictionary<string, object>?>(attributes =>
                    attributes != null &&
                    attributes.ContainsKey("eventId") &&
                    Equals(attributes["eventId"], _eventId.ToString())),
                Arg.Any<CancellationToken>())
            .Returns(false);

        await Assert.ThrowsAsync<AuthorizationException>(async () =>
            await behavior.Handle(
                query,
                _ =>
                {
                    handlerInvoked = true;
                    return Task.FromResult<IReadOnlyList<RegistrationOrderDto>>([]);
                },
                CancellationToken.None));

        await Assert.That(handlerInvoked).IsFalse();
    }

    private GetGuestRegistrationOrderQueryHandler CreateGuestQueryHandler() => new(
        _inventory,
        _lifecycle,
        _capabilities,
        _tenant,
        new FixedTimeProvider(UtcNow));

    private ContinueGuestRegistrationOrderCommandHandler CreateGuestContinueHandler() => new(
        _inventory,
        _lifecycle,
        _capabilities,
        _tenant,
        new FixedTimeProvider(UtcNow));

    private RegistrationOrder CreateGuestOrder(
        Guid? eventId = null,
        Guid? accountUserId = null,
        CapabilityTokenHash? guestHash = null,
        DateTime? expiresAt = null)
    {
        CapabilityTokenHash? effectiveGuestHash = accountUserId.HasValue
            ? guestHash
            : guestHash ?? CapabilityTokenHash.Create(Convert.ToBase64String(new byte[32]));
        return RegistrationOrder.Create(
            _orderId,
            _tenantId,
            eventId ?? _eventId,
            accountUserId,
            purchaserActorId: null,
            BookingPartyTypeEnum.Individual,
            Guid.CreateVersion7(),
            RegistrationParticipationSnapshot.Create(
                Guid.CreateVersion7(),
                (int)ParticipationHandlingModeEnum.PlatformManaged,
                (int)AdvanceRegistrationObligationEnum.Required,
                (int)IdentityAccessModeEnum.CapabilityTokenAllowed,
                GuestRecoveryPolicyEnum.CapabilityLinkOnly),
            registrationWorkflowVersionId: null,
            effectiveGuestHash,
            "USD",
            UtcNow.AddMinutes(-1),
            expiresAt ?? UtcNow.AddMinutes(15));
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    public enum GuestLifecycleAction
    {
        Finalize,
        Cancel
    }
}
