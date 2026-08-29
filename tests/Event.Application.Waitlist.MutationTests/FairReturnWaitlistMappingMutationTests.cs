// ABOUTME: Kills mutations in private waitlist read authority, bounded state, and HAL capability decisions.
// ABOUTME: Uses valid Domain aggregates so Application mapping observes canonical commerce and lifecycle facts.

using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Waitlist;
using Explore.Application.DTOs.Waitlist;
using Explore.Application.Features.Waitlist.Handlers.Commands;
using Explore.Application.Features.Waitlist.Handlers.Queries;
using Explore.Application.Features.Waitlist.Requests.Commands;
using Explore.Application.Features.Waitlist.Requests.Queries;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using NSubstitute;

namespace Explore.Application.Waitlist.MutationTests;

public sealed class FairReturnWaitlistMappingMutationTests
{
    private static readonly DateTime UtcNow =
        new(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid TenantId =
        Guid.CreateVersion7();
    private static readonly Guid EventId =
        Guid.CreateVersion7();
    private static readonly Guid UserId =
        Guid.CreateVersion7();

    [Test]
    public async Task QueuedOwnerGetsBoundedPositionAndLeaveOnly()
    {
        EventWaitlistEntry entry = Entry();
        FairReturnWaitlistDto dto =
            (await QueryAsync(
                Access(
                    entry: entry,
                    position: 1_500)))!;

        await Assert.That(dto.StatusCode)
            .IsEqualTo("QUEUED");
        await Assert.That(dto.ReasonCode)
            .IsEqualTo("AWAITING_SUPPLY");
        await Assert.That(dto.Position)
            .IsEqualTo(
                FairReturnWaitlistDto
                    .MaximumPublishedPosition);
        await Assert.That(dto.Id)
            .IsEqualTo(entry.Id);
        await Assert.That(dto.CanJoin).IsFalse();
        await Assert.That(dto.CanLeave).IsTrue();
        await Assert.That(dto.CanAcceptOffer)
            .IsFalse();
        await Assert.That(dto.CanWithdrawSupply)
            .IsFalse();
        await Assert.That(dto.AllocationOpen).IsTrue();
        await Assert.That(dto.WithdrawalOpen).IsTrue();
    }

    [Test]
    public async Task OfferedOwnerRequiresSettlementToAccept()
    {
        Lifecycle lifecycle = LifecycleFacts();
        FairReturnWaitlistAccessContext access =
            Access(
                lifecycle.Entry,
                lifecycle.Offer,
                lifecycle.Supply,
                lifecycle.Binding,
                position: 3);

        FairReturnWaitlistDto pending =
            (await QueryAsync(
                access,
                replacementSettled: false))!;
        await Assert.That(pending.StatusCode)
            .IsEqualTo("OFFERED");
        await Assert.That(pending.ReasonCode)
            .IsEqualTo("PAYMENT_PENDING");
        await Assert.That(pending.CanLeave).IsFalse();
        await Assert.That(pending.CanAcceptOffer)
            .IsFalse();
        await Assert.That(pending.CanWithdrawSupply)
            .IsTrue();
        await Assert.That(pending.OfferExpiresAt)
            .IsEqualTo(lifecycle.Offer.ExpiresAt);

        FairReturnWaitlistDto settled =
            (await QueryAsync(
                access,
                replacementSettled: true))!;
        await Assert.That(settled.ReasonCode)
            .IsEqualTo("REPLACEMENT_SETTLED");
        await Assert.That(settled.CanAcceptOffer)
            .IsTrue();
    }

    [Test]
    public async Task ClosedControlsSuppressEveryAction()
    {
        FairReturnWaitlistDto dto =
            (await QueryAsync(
                Access(),
                controlsOpen: false))!;

        await Assert.That(dto.Position)
            .IsEqualTo(
                FairReturnWaitlistDto
                    .PositionUnavailable);
        await Assert.That(dto.StatusCode)
            .IsEqualTo("AVAILABLE");
        await Assert.That(dto.ReasonCode)
            .IsEqualTo("WAITLIST_AVAILABLE");
        await Assert.That(dto.CanJoin).IsFalse();
        await Assert.That(dto.CanLeave).IsFalse();
        await Assert.That(dto.CanAcceptOffer)
            .IsFalse();
        await Assert.That(dto.CanWithdrawSupply)
            .IsFalse();
        await Assert.That(dto.AllocationOpen).IsFalse();
        await Assert.That(dto.WithdrawalOpen).IsFalse();
    }

    [Test]
    public async Task AvailableSupplyPublishesOnlySellerAction()
    {
        FairReturnSupplyUnit supply = Supply();
        FairReturnWaitlistDto dto =
            (await QueryAsync(
                Access(supply: supply)))!;

        await Assert.That(dto.Id).IsEqualTo(supply.Id);
        await Assert.That(dto.StatusCode)
            .IsEqualTo("AVAILABLE");
        await Assert.That(dto.ReasonCode)
            .IsEqualTo("SUPPLY_AVAILABLE");
        await Assert.That(dto.CanJoin).IsFalse();
        await Assert.That(dto.CanWithdrawSupply)
            .IsTrue();
    }

    [Test]
    public async Task EmptyOpenStateAllowsJoinAndClampsNegativePosition()
    {
        FairReturnWaitlistAccessContext access =
            Access(position: -7);
        FairReturnWaitlistDto dto =
            (await QueryAsync(access))!;

        await Assert.That(dto.Id)
            .IsEqualTo(access.Line.Id);
        await Assert.That(dto.Position)
            .IsEqualTo(
                FairReturnWaitlistDto
                    .PositionUnavailable);
        await Assert.That(dto.CanJoin).IsTrue();
        await Assert.That(dto.CanLeave).IsFalse();
        await Assert.That(dto.CanAcceptOffer)
            .IsFalse();
        await Assert.That(dto.CanWithdrawSupply)
            .IsFalse();
    }

    [Test]
    public async Task GuestReadNeverReceivesOwnerActions()
    {
        Lifecycle lifecycle =
            LifecycleFacts(guest: true);
        FairReturnWaitlistAccessContext access =
            Access(
                lifecycle.Entry,
                lifecycle.Offer,
                lifecycle.Supply,
                lifecycle.Binding,
                position: 1,
                commerce: lifecycle.Commerce);

        FairReturnWaitlistDto dto =
            (await QueryAsync(
                access,
                replacementSettled: true,
                userId: null,
                useOwner: false,
                capabilityToken: "guest-token",
                capabilityMatches: true))!;

        await Assert.That(dto.CanJoin).IsFalse();
        await Assert.That(dto.CanLeave).IsFalse();
        await Assert.That(dto.CanAcceptOffer)
            .IsFalse();
        await Assert.That(dto.CanWithdrawSupply)
            .IsFalse();
    }

    [Test]
    public async Task BoundAndWithdrawnStatesUseExactCodes()
    {
        FairReturnSupplyUnit bound = Supply();
        bound.Bind(UtcNow);
        FairReturnWaitlistDto boundDto =
            (await QueryAsync(
                Access(supply: bound)))!;
        await Assert.That(boundDto.Id)
            .IsEqualTo(bound.Id);
        await Assert.That(boundDto.StatusCode)
            .IsEqualTo("BOUND");
        await Assert.That(boundDto.ReasonCode)
            .IsEqualTo("SELLER_CONFLICT");

        FairReturnSupplyUnit withdrawn = Supply();
        withdrawn.Withdraw(UtcNow);
        FairReturnWaitlistDto supplyDto =
            (await QueryAsync(
                Access(supply: withdrawn)))!;
        await Assert.That(supplyDto.StatusCode)
            .IsEqualTo("WITHDRAWN");
        await Assert.That(supplyDto.ReasonCode)
            .IsEqualTo("WITHDRAWN");

        EventWaitlistEntry entry = Entry();
        entry.Withdraw(UtcNow);
        FairReturnWaitlistDto entryDto =
            (await QueryAsync(
                Access(entry: entry)))!;
        await Assert.That(entryDto.Id)
            .IsEqualTo(entry.Id);
        await Assert.That(entryDto.StatusCode)
            .IsEqualTo("WITHDRAWN");
        await Assert.That(entryDto.ReasonCode)
            .IsEqualTo("WITHDRAWN");
    }

    [Test]
    public async Task LeaveRequiresEveryOwnedOpenCondition()
    {
        FairReturnWaitlistAccessContext valid =
            Access(entry: Entry());
        (FairReturnWaitlistAccessContext? Access,
            bool Authenticated,
            Guid? UserId,
            bool Open,
            bool Expected)[]
            cases =
        [
            (null, true, UserId, true, false),
            (valid, false, UserId, true, false),
            (valid, true, null, true, false),
            (valid, true, Guid.CreateVersion7(),
                true, false),
            (valid, true, UserId, false, false),
            (valid, true, UserId, true, true),
        ];

        foreach (var scenario in cases)
        {
            FairReturnWaitlistDto? result =
                await LeaveAsync(
                    scenario.Access,
                    scenario.Authenticated,
                    scenario.UserId,
                    scenario.Open);
            await Assert.That(result is not null)
                .IsEqualTo(scenario.Expected);
        }
    }

    [Test]
    public async Task NonOwnerCannotReadPrivateWaitlistState()
    {
        FairReturnWaitlistAccessContext access =
            Access(entry: Entry());
        IFairReturnWaitlistRepository repository =
            Substitute.For<
                IFairReturnWaitlistRepository>();
        repository.GetAccessAsync(
                TenantId,
                EventId,
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(access);
        ITenantContext tenant =
            Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(TenantId);
        ICurrentUserService current =
            Substitute.For<ICurrentUserService>();
        current.UserId.Returns(
            Guid.CreateVersion7());
        var activation =
            Substitute.For<
                IPaidCheckoutActivationService>();
        var handler =
            new GetFairReturnWaitlistQueryHandler(
                repository,
                tenant,
                current,
                Substitute.For<
                    IGuestCapabilityTokenService>(),
                activation);
        FairReturnWaitlistDto? result =
            await handler.Handle(
                new GetFairReturnWaitlistQuery(
                    EventId,
                    access.Order.Id,
                    access.Line.Id,
                    null),
                CancellationToken.None);

        await Assert.That(result).IsNull();
        await activation.DidNotReceive()
            .EvaluateSaleControlAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConvertedEntryPublishesClosedCompletedState()
    {
        EventWaitlistEntry entry = Entry();
        entry.MarkOffered(UtcNow);
        entry.Convert(UtcNow.AddMinutes(1));

        FairReturnWaitlistDto dto =
            (await QueryAsync(
                Access(entry: entry)))!;

        await Assert.That(dto.StatusCode)
            .IsEqualTo("CONVERTED");
        await Assert.That(dto.ReasonCode)
            .IsEqualTo("COMPLETED");
        await Assert.That(dto.CanLeave).IsFalse();
    }

    private static async Task<FairReturnWaitlistDto?>
        QueryAsync(
            FairReturnWaitlistAccessContext access,
            bool controlsOpen = true,
            bool replacementSettled = false,
            Guid? userId = null,
            bool useOwner = true,
            string? capabilityToken = null,
            bool capabilityMatches = false)
    {
        IFairReturnWaitlistRepository repository =
            Substitute.For<
                IFairReturnWaitlistRepository>();
        repository.GetAccessAsync(
                TenantId,
                EventId,
                access.Order.Id,
                access.Line.Id,
                Arg.Any<CancellationToken>())
            .Returns(access);
        repository.HasReplacementSettlementAsync(
                TenantId,
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(replacementSettled);
        ITenantContext tenant =
            Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(TenantId);
        ICurrentUserService current =
            Substitute.For<ICurrentUserService>();
        current.UserId.Returns(
            useOwner ? UserId : userId);
        IGuestCapabilityTokenService capabilities =
            Substitute.For<
                IGuestCapabilityTokenService>();
        if (access.Order.GuestAccessTokenHash is
            { } hash)
        {
            capabilities.Matches(
                    capabilityToken,
                    hash)
                .Returns(capabilityMatches);
        }
        var activation =
            Substitute.For<
                IPaidCheckoutActivationService>();
        activation.EvaluateSaleControlAsync(
                TenantId,
                EventId,
                Arg.Any<CancellationToken>())
            .Returns(
                new PaidCheckoutActivationResult(
                    controlsOpen,
                    controlsOpen ? null : "stopped",
                    string.Empty));
        var handler =
            new GetFairReturnWaitlistQueryHandler(
                repository,
                tenant,
                current,
                capabilities,
                activation);
        return await handler.Handle(
            new GetFairReturnWaitlistQuery(
                EventId,
                access.Order.Id,
                access.Line.Id,
                capabilityToken),
            CancellationToken.None);
    }

    private static async Task<FairReturnWaitlistDto?>
        LeaveAsync(
            FairReturnWaitlistAccessContext? access,
            bool authenticated,
            Guid? userId,
            bool controlsOpen)
    {
        IFairReturnWaitlistRepository repository =
            Substitute.For<
                IFairReturnWaitlistRepository>();
        repository.GetAccessAsync(
                TenantId,
                EventId,
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(access);
        if (access?.Entry is { } entry)
        {
            repository.LeaveAsync(
                    TenantId,
                    EventId,
                    access.Line.Id,
                    Arg.Any<DateTime>(),
                    Arg.Any<CancellationToken>())
                .Returns(entry);
        }
        ITenantContext tenant =
            Substitute.For<ITenantContext>();
        tenant.TenantId.Returns(TenantId);
        ICurrentUserService current =
            Substitute.For<ICurrentUserService>();
        current.IsAuthenticated.Returns(
            authenticated);
        current.UserId.Returns(userId);
        var activation =
            Substitute.For<
                IPaidCheckoutActivationService>();
        activation.EvaluateSaleControlAsync(
                TenantId,
                EventId,
                Arg.Any<CancellationToken>())
            .Returns(
                new PaidCheckoutActivationResult(
                    controlsOpen,
                    controlsOpen ? null : "stopped",
                    string.Empty));
        var handler =
            new LeaveFairReturnWaitlistCommandHandler(
                repository,
                tenant,
                current,
                activation,
                new FixedTimeProvider());
        Guid orderId =
            access?.Order.Id
            ?? Guid.CreateVersion7();
        Guid lineId =
            access?.Line.Id
            ?? Guid.CreateVersion7();
        return await handler.Handle(
            new LeaveFairReturnWaitlistCommand(
                EventId,
                orderId,
                lineId),
            CancellationToken.None);
    }

    private static FairReturnWaitlistAccessContext
        Access(
            EventWaitlistEntry? entry = null,
            EventWaitlistOffer? offer = null,
            FairReturnSupplyUnit? supply = null,
            FairReturnSourceBinding? binding = null,
            long position = 0,
            Commerce? commerce = null)
    {
        commerce ??= CommerceFacts();
        return new FairReturnWaitlistAccessContext(
            commerce.Order,
            commerce.Line,
            entry,
            offer,
            supply,
            binding,
            Policy(
                commerce.Catalog.Id,
                commerce.TicketType.Id),
            null,
            position);
    }

    private static Lifecycle LifecycleFacts(
        bool guest = false)
    {
        Commerce commerce = CommerceFacts(guest);
        Guid policySnapshotId =
            Guid.CreateVersion7();
        EventWaitlistEntry entry = Entry(
            commerce.Order.Id,
            commerce.Line.Id,
            commerce.Catalog.Id,
            commerce.TicketType.Id,
            policySnapshotId);
        FairReturnSupplyUnit supply = Supply(
            commerce.Catalog.Id,
            commerce.TicketType.Id,
            policySnapshotId);
        FairReturnSupplyPolicy policy = Policy(
            commerce.Catalog.Id,
            commerce.TicketType.Id);
        Guid bindingId = Guid.CreateVersion7();
        EventWaitlistOffer offer =
            EventWaitlistOffer.Create(
                Guid.CreateVersion7(),
                policy,
                entry,
                supply,
                bindingId,
                Guid.CreateVersion7(),
                UtcNow);
        FairReturnSourceBinding binding =
            FairReturnSourceBinding.Create(
                bindingId,
                supply,
                entry,
                UtcNow);
        return new Lifecycle(
            commerce,
            entry,
            supply,
            offer,
            binding);
    }

    private static Commerce CommerceFacts(
        bool guest = false)
    {
        EventTicketCatalogVersion catalog =
            EventTicketCatalogVersion.Create(
                TenantId,
                EventId,
                "USD",
                1);
        EventTicketType ticketType =
            EventTicketType.Create(
                Guid.CreateVersion7(),
                TenantId,
                catalog.Id,
                "Waitlist ticket",
                "USD",
                TicketPricingModeEnum.Free,
                null,
                null,
                null,
                ParticipantDataCollectionModeEnum.None,
                null,
                null,
                null,
                false,
                false,
                null,
                null,
                null,
                null);
        catalog.AddTicketType(ticketType, null);
        catalog.AddEntitlement(
            ticketType,
            TicketTypeEntitlement.CreateForEvent(
                ticketType.Id,
                TenantId,
                EventId,
                1));
        catalog.Publish();
        RegistrationOrder order =
            RegistrationOrder.Create(
                TenantId,
                EventId,
                guest ? null : UserId,
                null,
                BookingPartyTypeEnum.Individual,
                catalog.Id,
                RegistrationParticipationSnapshot
                    .Create(
                        Guid.CreateVersion7(),
                        1,
                        1,
                        1,
                        null),
                null,
                guest
                    ? CapabilityTokenHash.Create(
                        Convert.ToBase64String(
                            new byte[32]))
                    : null,
                "USD",
                UtcNow,
                null);
        RegistrationOrderLine line =
            RegistrationOrderLine.Create(
                catalog,
                ticketType,
                order.Id,
                1,
                null,
                null);
        order.AddLine(line);
        return new Commerce(
            catalog,
            ticketType,
            order,
            line);
    }

    private static EventWaitlistEntry Entry() =>
        Entry(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());

    private static EventWaitlistEntry Entry(
        Guid orderId,
        Guid lineId,
        Guid catalogId,
        Guid ticketTypeId,
        Guid policySnapshotId) =>
        EventWaitlistEntry.Enqueue(
            Guid.CreateVersion7(),
            TenantId,
            EventId,
            ticketTypeId,
            catalogId,
            policySnapshotId,
            orderId,
            lineId,
            Guid.CreateVersion7(),
            UserId,
            "USD",
            Digest("terms"),
            Digest("entitlement"),
            0,
            1,
            0,
            UtcNow);

    private static FairReturnSupplyUnit Supply(
        Guid? catalogId = null,
        Guid? ticketTypeId = null,
        Guid? policySnapshotId = null) =>
        FairReturnSupplyUnit.Create(
            Guid.CreateVersion7(),
            TenantId,
            EventId,
            ticketTypeId ?? Guid.CreateVersion7(),
            catalogId ?? Guid.CreateVersion7(),
            policySnapshotId
            ?? Guid.CreateVersion7(),
            "USD",
            Digest("terms"),
            Digest("entitlement"),
            0,
            1,
            Guid.CreateVersion7(),
            UtcNow);

    private static FairReturnSupplyPolicy Policy(
        Guid catalogId,
        Guid ticketTypeId) =>
        FairReturnSupplyPolicy.Create(
            Guid.CreateVersion7(),
            TenantId,
            EventId,
            catalogId,
            ticketTypeId,
            true,
            15,
            UtcNow);

    private static string Digest(string value) =>
        Convert.ToBase64String(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(value)));

    private sealed record Commerce(
        EventTicketCatalogVersion Catalog,
        EventTicketType TicketType,
        RegistrationOrder Order,
        RegistrationOrderLine Line);

    private sealed record Lifecycle(
        Commerce Commerce,
        EventWaitlistEntry Entry,
        FairReturnSupplyUnit Supply,
        EventWaitlistOffer Offer,
        FairReturnSourceBinding Binding);

    private sealed class FixedTimeProvider :
        TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(UtcNow);
    }
}
