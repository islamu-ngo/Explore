// ABOUTME: Defines RED contracts for server-owned ticket-purchase authority and stable CQRS failures.
// ABOUTME: Covers manual validation, cancellation, durable outcomes, and non-sentinel order identity.

using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.RegistrationOrders.Handlers.Commands;
using Explore.Application.Features.RegistrationOrders.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace ApplicationUnitTests;

public sealed class TicketPurchaseGovernanceHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly Guid _orderId = Guid.CreateVersion7();
    private readonly Guid _policyId = Guid.CreateVersion7();
    private readonly Guid _accountId = Guid.CreateVersion7();
    private readonly Guid _actorId = Guid.CreateVersion7();
    private readonly ITicketPurchaseAuthorityResolver _authorities =
        Substitute.For<ITicketPurchaseAuthorityResolver>();
    private readonly ITicketPurchaseOrderResolver _orders =
        Substitute.For<ITicketPurchaseOrderResolver>();
    private readonly ITicketPurchaseGovernanceRepository _governance =
        Substitute.For<ITicketPurchaseGovernanceRepository>();
    private readonly ITenantContext _tenant =
        Substitute.For<ITenantContext>();
    private readonly ICurrentUserService _currentUser =
        Substitute.For<ICurrentUserService>();
    private readonly IActorRepository _actors =
        Substitute.For<IActorRepository>();
    private readonly IGroupMemberRepository _groupMembers =
        Substitute.For<IGroupMemberRepository>();
    private readonly IOrganizationMemberRepository _organizationMembers =
        Substitute.For<IOrganizationMemberRepository>();
    private readonly IRegistrationInventoryRepository _inventory =
        Substitute.For<IRegistrationInventoryRepository>();

    public TicketPurchaseGovernanceHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_accountId);
        _orders.ResolveAsync(
                _tenantId,
                _eventId,
                _orderId,
                Arg.Any<CancellationToken>())
            .Returns(new TicketPurchaseOrderSnapshot(
                _orderId,
                2));
        _authorities.ResolveAsync(
                Arg.Any<TicketPurchaseAuthorityResolutionRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(TicketPurchaseAuthorityResolution.Success(
                TicketPurchaseAuthorityDimension.Authenticated(
                    _accountId,
                    _actorId)));
        _governance.GetPolicyVersionAsync(
                _tenantId,
                _eventId,
                _policyId,
                Arg.Any<CancellationToken>())
            .Returns(CreatePolicy());
        _governance.ReserveAsync(
                Arg.Any<TicketPurchasePolicyVersion>(),
                Arg.Any<TicketPurchaseReservationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new TicketPurchaseReservationResult(
                TicketPurchaseReservationDisposition.Reserved,
                _orderId,
                4,
                2));
    }

    [Test]
    public async Task AuthenticatedGroupPurchaseUsesServerResolvedAuthority()
    {
        TicketPurchaseReservationRequest? captured = null;
        _governance.ReserveAsync(
                Arg.Any<TicketPurchasePolicyVersion>(),
                Arg.Any<TicketPurchaseReservationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured =
                    call.ArgAt<TicketPurchaseReservationRequest>(1);
                return new TicketPurchaseReservationResult(
                    TicketPurchaseReservationDisposition.Reserved,
                    _orderId,
                    4,
                    2);
            });
        BaseCommandResponse<Guid> result =
            await CreateHandler().Handle(
                CreateCommand(),
                CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await _authorities.Received(1).ResolveAsync(
            Arg.Is<TicketPurchaseAuthorityResolutionRequest>(
                request =>
                    request.EventId == _eventId
                    && request.OrderId == _orderId
                    && request.AccessMode ==
                    TicketPurchaseAccessMode.AuthenticatedAccount
                    && request.RequestedPurchaserActorId == _actorId),
            CancellationToken.None);
        await _governance.Received(1).ReserveAsync(
            Arg.Any<TicketPurchasePolicyVersion>(),
            Arg.Is<TicketPurchaseReservationRequest>(
                reservation =>
                    reservation.Quantity == 2
                    && reservation.Authority.ActingAccountUserId ==
                    _accountId
                    && reservation.Authority.PurchaserActorId ==
                    _actorId),
            CancellationToken.None);
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Operation.KeyHash)
            .IsEqualTo(Hash("purchase-operation-1"));
        await Assert.That(captured.Operation.FingerprintHash)
            .IsEqualTo(Hash(string.Join(
                '|',
                "reserve-ticket-purchase-v1",
                _tenantId.ToString("N"),
                _eventId.ToString("N"),
                _orderId.ToString("N"),
                _policyId.ToString("N"),
                "2",
                "1",
                $"account:{_accountId:N}",
                _actorId.ToString("N"))));
    }

    [Test]
    public async Task InvalidRequestIsRejectedBeforeAuthorityResolution()
    {
        ReserveTicketPurchaseCommand invalid = CreateCommand() with
        {
            EventId = Guid.Empty,
            OperationKey = string.Empty,
        };

        BaseCommandResponse<Guid> result =
            await CreateHandler().Handle(
                invalid,
                CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(TicketPurchaseFailureCodes.InvalidRequest);
        await _authorities.DidNotReceiveWithAnyArgs()
            .ResolveAsync(default!, default);
    }

    [Test]
    public async Task CancellationIsObservedBeforeAnyDependencyCall()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateHandler().Handle(
                CreateCommand(),
                cancellation.Token));
        await _authorities.DidNotReceiveWithAnyArgs()
            .ResolveAsync(default!, default);
    }

    [Test]
    public async Task MissingOrderFailsBeforeAuthorityOrPolicyReads()
    {
        _orders.ResolveAsync(
                _tenantId,
                _eventId,
                _orderId,
                Arg.Any<CancellationToken>())
            .Returns((TicketPurchaseOrderSnapshot?)null);

        BaseCommandResponse<Guid> result =
            await CreateHandler().Handle(
                CreateCommand(),
                CancellationToken.None);

        await Assert.That(result.FailureCode)
            .IsEqualTo(
                TicketPurchaseFailureCodes.OrderUnavailable);
        await _authorities.DidNotReceiveWithAnyArgs()
            .ResolveAsync(default!, default);
        await _governance.DidNotReceiveWithAnyArgs()
            .GetPolicyVersionAsync(
                default,
                default,
                default,
                default);
    }

    [Test]
    public async Task AuthorityFailurePreservesStableResolverCode()
    {
        _authorities.ResolveAsync(
                Arg.Any<TicketPurchaseAuthorityResolutionRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(TicketPurchaseAuthorityResolution.Failure(
                TicketPurchaseFailureCodes.AuthorityUnavailable));

        BaseCommandResponse<Guid> result =
            await CreateHandler().Handle(
                CreateCommand(),
                CancellationToken.None);

        await Assert.That(result.FailureCode)
            .IsEqualTo(
                TicketPurchaseFailureCodes.AuthorityUnavailable);
        await _governance.DidNotReceiveWithAnyArgs()
            .GetPolicyVersionAsync(
                default,
                default,
                default,
                default);
    }

    [Test]
    public async Task MissingPolicyFailsBeforeAuthorityConsumption()
    {
        _governance.GetPolicyVersionAsync(
                _tenantId,
                _eventId,
                _policyId,
                Arg.Any<CancellationToken>())
            .Returns((TicketPurchasePolicyVersion?)null);

        BaseCommandResponse<Guid> result =
            await CreateHandler().Handle(
                CreateCommand(),
                CancellationToken.None);

        await Assert.That(result.FailureCode)
            .IsEqualTo(
                TicketPurchaseFailureCodes.PolicyUnavailable);
        await _governance.DidNotReceiveWithAnyArgs()
            .ReserveAsync(default!, default!, default);
    }

    [Test]
    public async Task ExactDurableReplayReturnsTheOrderSuccess()
    {
        _governance.ReserveAsync(
                Arg.Any<TicketPurchasePolicyVersion>(),
                Arg.Any<TicketPurchaseReservationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new TicketPurchaseReservationResult(
                TicketPurchaseReservationDisposition.Replay,
                Guid.CreateVersion7(),
                4,
                2));

        BaseCommandResponse<Guid> result =
            await CreateHandler().Handle(
                CreateCommand(),
                CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(_orderId);
    }

    [Test]
    public async Task NameOnlyFingerprintPinsOrderScopeWithoutActorSentinel()
    {
        TicketPurchaseAuthorityDimension authority =
            TicketPurchaseAuthorityDimension.NameOnly(_orderId);
        _authorities.ResolveAsync(
                Arg.Any<TicketPurchaseAuthorityResolutionRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(
                TicketPurchaseAuthorityResolution.Success(authority));
        TicketPurchaseReservationRequest? captured = null;
        _governance.ReserveAsync(
                Arg.Any<TicketPurchasePolicyVersion>(),
                Arg.Any<TicketPurchaseReservationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured =
                    call.ArgAt<TicketPurchaseReservationRequest>(1);
                return new TicketPurchaseReservationResult(
                    TicketPurchaseReservationDisposition.Reserved,
                    _orderId,
                    4,
                    2);
            });
        ReserveTicketPurchaseCommand command =
            CreateCommand() with
            {
                AccessMode = TicketPurchaseAccessMode.NameOnly,
                RequestedPurchaserActorId = null,
            };

        BaseCommandResponse<Guid> result =
            await CreateHandler().Handle(
                command,
                CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Operation.FingerprintHash)
            .IsEqualTo(Hash(string.Join(
                '|',
                "reserve-ticket-purchase-v1",
                _tenantId.ToString("N"),
                _eventId.ToString("N"),
                _orderId.ToString("N"),
                _policyId.ToString("N"),
                "2",
                "3",
                $"order:{_orderId:N}",
                "-")));
    }

    [Test]
    [Arguments(
        TicketPurchaseReservationDisposition.CeilingExceeded,
        TicketPurchaseFailureCodes.CeilingExceeded)]
    [Arguments(
        TicketPurchaseReservationDisposition.OperationConflict,
        TicketPurchaseFailureCodes.OperationConflict)]
    public async Task DurableOutcomesMapToStableFailureCodesWithoutZeroIds(
        TicketPurchaseReservationDisposition disposition,
        string expectedFailureCode)
    {
        _governance.ReserveAsync(
                Arg.Any<TicketPurchasePolicyVersion>(),
                Arg.Any<TicketPurchaseReservationRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new TicketPurchaseReservationResult(
                disposition,
                null,
                4,
                4));

        BaseCommandResponse<Guid> result =
            await CreateHandler().Handle(
                CreateCommand(),
                CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(expectedFailureCode);
        await Assert.That(result.Id).IsEqualTo(_orderId);
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task ResolverKeepsAccountAuthorityAcrossPersonalAndGroupContexts()
    {
        Guid groupId = Guid.CreateVersion7();
        Actor personal = CreateActor(
            _actorId,
            userId: _accountId);
        Actor group = CreateActor(
            Guid.CreateVersion7(),
            groupId: groupId);
        _actors.GetPublicActorProfileByTenantAsync(
                _tenantId,
                personal.Id,
                Arg.Any<CancellationToken>())
            .Returns(personal);
        _actors.GetPublicActorProfileByTenantAsync(
                _tenantId,
                group.Id,
                Arg.Any<CancellationToken>())
            .Returns(group);
        _groupMembers.Exists(groupId, _accountId)
            .Returns(true);
        TicketPurchaseAuthorityResolver resolver =
            CreateAuthorityResolver();

        TicketPurchaseAuthorityResolution personalResult =
            await resolver.ResolveAsync(
                new TicketPurchaseAuthorityResolutionRequest(
                    _eventId,
                    _orderId,
                    TicketPurchaseAccessMode.AuthenticatedAccount,
                    personal.Id),
                CancellationToken.None);
        TicketPurchaseAuthorityResolution groupResult =
            await resolver.ResolveAsync(
                new TicketPurchaseAuthorityResolutionRequest(
                    _eventId,
                    _orderId,
                    TicketPurchaseAccessMode.AuthenticatedAccount,
                    group.Id),
                CancellationToken.None);

        await Assert.That(personalResult.IsSuccess).IsTrue();
        await Assert.That(groupResult.IsSuccess).IsTrue();
        await Assert.That(personalResult.Authority!.EnforcementKey)
            .IsEqualTo(groupResult.Authority!.EnforcementKey);
        await Assert.That(groupResult.Authority.ActingAccountUserId)
            .IsEqualTo(_accountId);
        await Assert.That(groupResult.Authority.PurchaserActorId)
            .IsEqualTo(group.Id);
    }

    [Test]
    public async Task ResolverRejectsActorForUnrelatedGroupMember()
    {
        Guid groupId = Guid.CreateVersion7();
        Actor group = CreateActor(
            Guid.CreateVersion7(),
            groupId: groupId);
        _actors.GetPublicActorProfileByTenantAsync(
                _tenantId,
                group.Id,
                Arg.Any<CancellationToken>())
            .Returns(group);
        _groupMembers.Exists(groupId, _accountId)
            .Returns(false);

        TicketPurchaseAuthorityResolution result =
            await CreateAuthorityResolver().ResolveAsync(
                new TicketPurchaseAuthorityResolutionRequest(
                    _eventId,
                    _orderId,
                    TicketPurchaseAccessMode.AuthenticatedAccount,
                    group.Id),
                CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode)
            .IsEqualTo(
                TicketPurchaseFailureCodes.AuthorityUnavailable);
    }

    private ReserveTicketPurchaseCommandHandler CreateHandler() =>
        new(_authorities, _orders, _governance, _tenant);

    private TicketPurchaseAuthorityResolver
        CreateAuthorityResolver() => new(
            _currentUser,
            _tenant,
            _actors,
            _groupMembers,
            _organizationMembers,
            _inventory);

    private ReserveTicketPurchaseCommand CreateCommand() => new(
        _eventId,
        _orderId,
        _policyId,
        TicketPurchaseAccessMode.AuthenticatedAccount,
        _actorId,
        "purchase-operation-1");

    private TicketPurchasePolicyVersion CreatePolicy() =>
        TicketPurchasePolicyVersion.Create(
            _tenantId,
            _eventId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            5,
            4,
            6,
            new DateTime(
                2026,
                8,
                27,
                12,
                0,
                0,
                DateTimeKind.Utc));

    private static Actor CreateActor(
        Guid id,
        Guid? userId = null,
        Guid? groupId = null) => new()
    {
        Id = id,
        UserId = userId,
        GroupId = groupId,
        ActorType = new ActorType
        {
            Id = groupId.HasValue ? 3 : 1,
            FullName = groupId.HasValue ? "Group" : "User",
            MasterCode = groupId.HasValue ? "group" : "user",
        },
        Pii = new ActorPii
        {
            ActorId = id,
            DisplayName = "Authority test actor",
        },
    };

    private static string Hash(string value) =>
        Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
