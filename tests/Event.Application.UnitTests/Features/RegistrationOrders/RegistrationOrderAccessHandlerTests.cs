// ABOUTME: Tests guest-capability and account-scoped registration-order CQRS access wrappers.
// ABOUTME: Proves generic absence semantics, one-time token issuance, and lifecycle delegation.

using System.Text.Json;
using Explore.Application.Authorization;
using Explore.Application.Behaviors;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.RegistrationOrders;
using Explore.Application.Exceptions;
using Explore.Application.Features.RegistrationOrders.Handlers.Commands;
using Explore.Application.Features.RegistrationOrders.Handlers.Queries;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Features.RegistrationOrders.Requests.Queries;
using Explore.Application.Features.RegistrationSubmissions.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
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
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public RegistrationOrderAccessHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _unitOfWork.ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(0)(
                call.ArgAt<CancellationToken>(1)));
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

        GuestRegistrationOrderLifecycleResponseDto result = await CreateGuestContinueHandler().Handle(
            new ContinueGuestRegistrationOrderCommand(_eventId, _orderId, "guessed-token"),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_not_found");
        _ = _capabilities.DidNotReceive().Matches(Arg.Any<string?>(), Arg.Any<CapabilityTokenHash>());
        _ = _lifecycle.DidNotReceive().SubmitAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GuestParticipantMutationWithScopedCapabilityDispatchesExistingCommand()
    {
        RegistrationOrder order = CreateGuestOrder();
        var sender = Substitute.For<ISender>();
        var mutation = new AddRegistrationParticipantCommand(
            _orderId, (int)ParticipantTypeEnum.Adult, null, new ParticipantDetailsDto("Guest", null, null));
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _capabilities.Matches("guest-token", order.GuestAccessTokenHash!).Returns(true);
        sender.Send(mutation, Arg.Any<CancellationToken>()).Returns(new Explore.Application.Responses.BaseCommandResponse<Guid>
        {
            Id = Guid.CreateVersion7(),
            Success = true
        });

        var handler = new MutateGuestRegistrationParticipantsCommandHandler(
            _inventory, _capabilities, _tenant, new FixedTimeProvider(UtcNow), sender);
        Explore.Application.Responses.BaseCommandResponse<Guid> result = await handler.Handle(
            new MutateGuestRegistrationParticipantsCommand(_eventId, _orderId, "guest-token", mutation),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        _ = sender.Received(1).Send(mutation, CancellationToken.None);
    }

    [Test]
    public async Task GuestParticipantMutationWithCrossEventCapabilityReturnsGenericNotFound()
    {
        RegistrationOrder order = CreateGuestOrder(eventId: Guid.CreateVersion7());
        var sender = Substitute.For<ISender>();
        var mutation = new AddRegistrationParticipantCommand(
            _orderId, (int)ParticipantTypeEnum.Adult, null, new ParticipantDetailsDto(null, null, null));
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        var handler = new MutateGuestRegistrationParticipantsCommandHandler(
            _inventory, _capabilities, _tenant, new FixedTimeProvider(UtcNow), sender);

        Explore.Application.Responses.BaseCommandResponse<Guid> result = await handler.Handle(
            new MutateGuestRegistrationParticipantsCommand(_eventId, _orderId, "guest-token", mutation),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_not_found");
        _ = sender.DidNotReceive().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ContinueGuestRegistrationOrderForwardsContributionAfterCapabilityValidation()
    {
        RegistrationOrder order = CreateGuestOrder();
        RegistrationOrderLifecycleResponseDto expected = new() { Id = _orderId, Success = true };
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _capabilities.Matches("guest-token", order.GuestAccessTokenHash!).Returns(true);
        _lifecycle.SubmitAsync(_orderId, _tenantId, 500, Arg.Any<CancellationToken>()).Returns(expected);

        GuestRegistrationOrderLifecycleResponseDto result = await CreateGuestContinueHandler().Handle(
            new ContinueGuestRegistrationOrderCommand(_eventId, _orderId, "guest-token", 500),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        _ = _lifecycle.Received(1).SubmitAsync(_orderId, _tenantId, 500, CancellationToken.None);
    }

    [Test]
    public async Task GuestNativeAttemptLaunchDispatchesOnlyAfterExistingOrderCapabilityMatches()
    {
        RegistrationOrder order = CreateGuestOrder();
        var sender = Substitute.For<ISender>();
        Guid requirementId = Guid.CreateVersion7();
        Guid channelId = Guid.CreateVersion7();
        Guid formId = Guid.CreateVersion7();
        Guid formVersionId = Guid.CreateVersion7();
        var expected = new NativeRegistrationAttemptResult(
            true, Guid.CreateVersion7(), requirementId, channelId, formId, formVersionId,
            UtcNow.AddMinutes(5), null, [], null, false, "attempt-token");
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _capabilities.Matches("guest-token", order.GuestAccessTokenHash!).Returns(true);
        sender.Send(Arg.Any<LaunchNativeRegistrationAttemptCommand>(), Arg.Any<CancellationToken>()).Returns(expected);
        var handler = new LaunchGuestNativeRegistrationAttemptCommandHandler(
            _inventory, _capabilities, _tenant, new FixedTimeProvider(UtcNow), sender);

        NativeRegistrationAttemptResult missing = await handler.Handle(
            new LaunchGuestNativeRegistrationAttemptCommand(
                _eventId, _orderId, null, requirementId, channelId, formId, formVersionId),
            CancellationToken.None);
        NativeRegistrationAttemptResult valid = await handler.Handle(
            new LaunchGuestNativeRegistrationAttemptCommand(
                _eventId, _orderId, "guest-token", requirementId, channelId, formId, formVersionId),
            CancellationToken.None);

        await Assert.That(missing.Success).IsFalse();
        await Assert.That(missing.FailureCode).IsEqualTo("registration_order_not_found");
        await Assert.That(valid).IsEqualTo(expected);
        _ = sender.Received(1).Send(
            Arg.Is<LaunchNativeRegistrationAttemptCommand>(command =>
                command.TenantId == _tenantId && command.EventId == _eventId && command.OrderId == _orderId),
            CancellationToken.None);
    }

    [Test]
    public async Task GuestProviderAttemptLaunchDispatchesOnlyAfterExistingOrderCapabilityMatches()
    {
        RegistrationOrder order = CreateGuestOrder();
        var sender = Substitute.For<ISender>();
        Guid requirementId = Guid.CreateVersion7();
        Guid channelId = Guid.CreateVersion7();
        Guid bindingId = Guid.CreateVersion7();
        Guid formId = Guid.CreateVersion7();
        Guid formVersionId = Guid.CreateVersion7();
        var expected = new RegistrationProviderAttemptResult(true, Guid.CreateVersion7(), null);
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _capabilities.Matches("guest-token", order.GuestAccessTokenHash!).Returns(true);
        sender.Send(Arg.Any<LaunchRegistrationProviderAttemptCommand>(), Arg.Any<CancellationToken>()).Returns(expected);
        var handler = new LaunchGuestRegistrationProviderAttemptCommandHandler(
            _inventory, _capabilities, _tenant, new FixedTimeProvider(UtcNow), sender);

        RegistrationProviderAttemptResult missing = await handler.Handle(
            new LaunchGuestRegistrationProviderAttemptCommand(
                _eventId, _orderId, null, requirementId, channelId, bindingId, formId, formVersionId),
            CancellationToken.None);
        RegistrationProviderAttemptResult valid = await handler.Handle(
            new LaunchGuestRegistrationProviderAttemptCommand(
                _eventId, _orderId, "guest-token", requirementId, channelId, bindingId, formId, formVersionId),
            CancellationToken.None);

        await Assert.That(missing.Success).IsFalse();
        await Assert.That(missing.FailureCode).IsEqualTo("registration_order_not_found");
        await Assert.That(valid).IsEqualTo(expected);
        _ = sender.Received(1).Send(
            Arg.Is<LaunchRegistrationProviderAttemptCommand>(command =>
                command.TenantId == _tenantId && command.EventId == _eventId && command.OrderId == _orderId &&
                command.RequirementId == requirementId && command.ChannelId == channelId &&
                command.BindingId == bindingId && command.FormId == formId && command.FormVersionId == formVersionId),
            CancellationToken.None);
    }

    [Test]
    [Arguments(GuestLifecycleAction.Finalize)]
    [Arguments(GuestLifecycleAction.Cancel)]
    public async Task GuestLifecycleActionWhenCapabilityIsValidDelegatesToExistingLifecycleService(GuestLifecycleAction action)
    {
        RegistrationOrder order = CreateGuestOrder();
        RegistrationOrderLifecycleResponseDto expected = new() { Id = _orderId, Success = true };
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

        GuestRegistrationOrderLifecycleResponseDto result = action == GuestLifecycleAction.Finalize
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

        RegistrationOrderLifecycleResponseDto result = await new CancelAuthenticatedRegistrationOrderCommandHandler(
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

        RegistrationOrderLifecycleResponseDto result = await new ContinueAuthenticatedRegistrationOrderCommandHandler(
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

    [Test]
    public async Task ClaimGuestRegistrationOrderRequiresCapabilityAndDoesNotSilentlyLink()
    {
        Guid userId = Guid.CreateVersion7();
        RegistrationOrder order = CreateGuestOrderWithPii("buyer@example.test");
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
        _users.GetUserWithDetails(userId, Arg.Any<CancellationToken>()).Returns(CreateUser(userId, "buyer@example.test", verified: true));
        _inventory.GetOrderForUpdateWithPiiAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _capabilities.Matches("bad-token", order.GuestAccessTokenHash!).Returns(false);

        Explore.Application.Responses.BaseCommandResponse<Guid> result = await CreateClaimHandler().Handle(
            new ClaimGuestRegistrationOrderCommand(_eventId, _orderId, "bad-token"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_not_found");
        await Assert.That(order.AccountUserId).IsNull();
        await _inventory.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClaimGuestRegistrationOrderRequiresAuthenticatedCurrentAccount()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        Explore.Application.Responses.BaseCommandResponse<Guid> result = await CreateClaimHandler().Handle(
            new ClaimGuestRegistrationOrderCommand(_eventId, _orderId, "guest-token"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_authentication_required");
        _ = await _inventory.DidNotReceive().GetOrderForUpdateWithPiiAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClaimGuestRegistrationOrderRequiresVerifiedCurrentUserEmailAuthority()
    {
        Guid userId = Guid.CreateVersion7();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
        _users.GetUserWithDetails(userId, Arg.Any<CancellationToken>()).Returns(CreateUser(userId, "buyer@example.test", verified: false));

        Explore.Application.Responses.BaseCommandResponse<Guid> result = await CreateClaimHandler().Handle(
            new ClaimGuestRegistrationOrderCommand(_eventId, _orderId, "guest-token"), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("registration_order_verified_email_required");
        _ = await _inventory.DidNotReceive().GetOrderForUpdateWithPiiAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ClaimGuestRegistrationOrderLinksOnlyWhenNormalizedVerifiedEmailMatchesAndThenAllowsAccountAccess()
    {
        Guid userId = Guid.CreateVersion7();
        RegistrationOrder order = CreateGuestOrderWithPii("Buyer@Example.Test");
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
        _users.GetUserWithDetails(userId, Arg.Any<CancellationToken>()).Returns(CreateUser(userId, "buyer@example.test", verified: true));
        _inventory.GetOrderForUpdateWithPiiAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _inventory.GetOrderWithLinesAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(order);
        _capabilities.Matches("guest-token", order.GuestAccessTokenHash!).Returns(true);
        _lifecycle.GetAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(_ => RegistrationOrderDto.From(order));

        Explore.Application.Responses.BaseCommandResponse<Guid> claim = await CreateClaimHandler().Handle(
            new ClaimGuestRegistrationOrderCommand(_eventId, _orderId, "guest-token"), CancellationToken.None);
        RegistrationOrderDto? current = await new GetCurrentRegistrationOrderQueryHandler(
            _inventory, _lifecycle, _tenant, _currentUser).Handle(new GetCurrentRegistrationOrderQuery(_orderId), CancellationToken.None);

        await Assert.That(claim.Success).IsTrue();
        await Assert.That(order.AccountUserId).IsEqualTo(userId);
        await Assert.That(current).IsNotNull();
        await Assert.That(current!.AccountUserId).IsEqualTo(userId);
        await _inventory.Received(1).SaveChangesAsync(CancellationToken.None);
    }

    [Test]
    public async Task ClaimGuestRegistrationOrderRejectsCrossEventAndWrongEmailWithoutLinking()
    {
        Guid userId = Guid.CreateVersion7();
        RegistrationOrder crossEvent = CreateGuestOrderWithPii("buyer@example.test", eventId: Guid.CreateVersion7());
        RegistrationOrder wrongEmail = CreateGuestOrderWithPii("other@example.test");
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
        _users.GetUserWithDetails(userId, Arg.Any<CancellationToken>()).Returns(CreateUser(userId, "buyer@example.test", verified: true));
        _capabilities.Matches("guest-token", Arg.Any<CapabilityTokenHash>()).Returns(true);

        _inventory.GetOrderForUpdateWithPiiAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(crossEvent, wrongEmail);

        Explore.Application.Responses.BaseCommandResponse<Guid> eventResult = await CreateClaimHandler().Handle(
            new ClaimGuestRegistrationOrderCommand(_eventId, _orderId, "guest-token"), CancellationToken.None);
        Explore.Application.Responses.BaseCommandResponse<Guid> emailResult = await CreateClaimHandler().Handle(
            new ClaimGuestRegistrationOrderCommand(_eventId, _orderId, "guest-token"), CancellationToken.None);

        await Assert.That(eventResult.FailureCode).IsEqualTo("registration_order_not_found");
        await Assert.That(emailResult.FailureCode).IsEqualTo("registration_order_email_mismatch");
        await Assert.That(crossEvent.AccountUserId).IsNull();
        await Assert.That(wrongEmail.AccountUserId).IsNull();
    }

    [Test]
    public async Task ClaimGuestRegistrationOrderRejectsOtherAccountButIsIdempotentForSameAccount()
    {
        Guid userId = Guid.CreateVersion7();
        Guid otherUserId = Guid.CreateVersion7();
        RegistrationOrder otherLinked = CreateGuestOrderWithPii("buyer@example.test", accountUserId: otherUserId, guestHash: CapabilityTokenHash.Create(Convert.ToBase64String(new byte[32])));
        RegistrationOrder sameLinked = CreateGuestOrderWithPii("buyer@example.test", accountUserId: userId, guestHash: CapabilityTokenHash.Create(Convert.ToBase64String(new byte[32])));
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
        _users.GetUserWithDetails(userId, Arg.Any<CancellationToken>()).Returns(CreateUser(userId, "buyer@example.test", verified: true));
        _capabilities.Matches("guest-token", Arg.Any<CapabilityTokenHash>()).Returns(true);
        _inventory.GetOrderForUpdateWithPiiAsync(_orderId, _tenantId, Arg.Any<CancellationToken>()).Returns(otherLinked, sameLinked);

        Explore.Application.Responses.BaseCommandResponse<Guid> conflict = await CreateClaimHandler().Handle(
            new ClaimGuestRegistrationOrderCommand(_eventId, _orderId, "guest-token"), CancellationToken.None);
        Explore.Application.Responses.BaseCommandResponse<Guid> retry = await CreateClaimHandler().Handle(
            new ClaimGuestRegistrationOrderCommand(_eventId, _orderId, "guest-token"), CancellationToken.None);

        await Assert.That(conflict.Success).IsFalse();
        await Assert.That(conflict.FailureCode).IsEqualTo("registration_order_already_linked");
        await Assert.That(retry.Success).IsTrue();
        await Assert.That(retry.Message).IsEqualTo("Registration order already linked to the current account.");
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

    private ClaimGuestRegistrationOrderCommandHandler CreateClaimHandler() => new(
        _inventory,
        _capabilities,
        _tenant,
        _currentUser,
        _users,
        new FixedTimeProvider(UtcNow),
        _unitOfWork);

    private RegistrationOrder CreateGuestOrderWithPii(
        string email,
        Guid? eventId = null,
        Guid? accountUserId = null,
        CapabilityTokenHash? guestHash = null)
    {
        RegistrationOrder order = CreateGuestOrder(eventId, accountUserId, guestHash);
        order.SetPii(RegistrationOrderPii.Create(order.Id, order.TenantId, "Buyer", email, null, null));
        return order;
    }

    private static User CreateUser(Guid userId, string email, bool verified) => new()
    {
        Id = userId,
        EmailVerified = verified,
        Pii = new UserPii
        {
            UserId = userId,
            Email = email,
            FirstName = "Buyer",
            LastName = "User"
        }
    };

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
