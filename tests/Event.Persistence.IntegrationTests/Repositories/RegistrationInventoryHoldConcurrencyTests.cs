// ABOUTME: Proves PostgreSQL capacity locking prevents two ticket types from taking one shared last seat.
// ABOUTME: Uses independent DbContexts and serializable transactions against the real registration-hold repository.

using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using Explore.Persistence;
using Explore.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Persistence.IntegrationTests.Repositories;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class RegistrationInventoryHoldConcurrencyTests(PostgreSqlContainerFixture fixture)
{
    private static readonly DateTime UtcNow = new(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task PostgreSqlCatalog_ContainsCapacityHoldPolicySeedsAndCapacityPoolForeignKey()
    {
        await fixture.ResetAsync();

        await Assert.That(await ScalarAsync(
            "SELECT COUNT(*) FROM capacity_hold_policies WHERE (id, master_code) IN ((1, 'NO_HOLD_UNTIL_READY'), (2, 'TIMED_HOLD_ON_SELECTION'), (3, 'APPROVAL_NO_HOLD'), (4, 'WAITLIST_WHEN_FULL'))"))
            .IsEqualTo(4L);
        await Assert.That(await ScalarAsync(
            "SELECT COUNT(*) FROM pg_constraint WHERE conname = 'fk_event_capacity_pools_capacity_hold_policies_capacity_hold_p' AND contype = 'f'"))
            .IsEqualTo(1L);
        await Assert.That(await ScalarAsync(
            "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE migration_id = '20260730200905_AddCapacityHoldPolicyLookup'"))
            .IsEqualTo(1L);
    }

    [Test]
    public async Task SharedPoolLastSeat_TwoDifferentTicketTypesCreateAtMostOneActiveHold()
    {
        (Guid tenantId, Guid eventId, Guid poolId, Guid firstTicketTypeId, Guid secondTicketTypeId) = await SeedAsync();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));

        Task<bool> firstReservation = ReserveAsync(tenantId, eventId, poolId, firstTicketTypeId, timeout.Token);
        Task<bool> secondReservation = ReserveAsync(tenantId, eventId, poolId, secondTicketTypeId, timeout.Token);
        bool[] reservations = await Task.WhenAll(firstReservation, secondReservation);

        await Assert.That(reservations.Count(result => result)).IsEqualTo(1);
        await using ExploreDbContext verificationContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        var verificationRepository = new RegistrationInventoryRepository(verificationContext);
        await Assert.That(await verificationRepository.GetAllocatedQuantityAsync(poolId, tenantId, timeout.Token)).IsEqualTo(1);
        await Assert.That(await verificationContext.RegistrationInventoryHolds
            .Where(hold => hold.CapacityPoolId == poolId)
            .Select(hold => hold.TicketTypeId)
            .Distinct()
            .CountAsync(timeout.Token)).IsEqualTo(1);
    }

    [Test]
    public async Task NoHoldUntilReadyLastSeat_TwoDifferentTicketTypesReserveAtMostOneFinalizationHold()
    {
        (Guid tenantId, Guid eventId, Guid poolId, Guid firstTicketTypeId, Guid secondTicketTypeId) = await SeedAsync(
            holdPolicy: CapacityHoldPolicyEnum.NoHoldUntilReady);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));

        Task<bool> firstReservation = ReserveNonTimedAtReadyAsync(tenantId, eventId, poolId, firstTicketTypeId, timeout.Token);
        Task<bool> secondReservation = ReserveNonTimedAtReadyAsync(tenantId, eventId, poolId, secondTicketTypeId, timeout.Token);
        bool[] reservations = await Task.WhenAll(firstReservation, secondReservation);

        await Assert.That(reservations.Count(result => result)).IsEqualTo(1);
        await using ExploreDbContext verificationContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        var verificationRepository = new RegistrationInventoryRepository(verificationContext);
        await Assert.That(await verificationRepository.GetAllocatedQuantityAsync(poolId, tenantId, timeout.Token)).IsEqualTo(1);
        await Assert.That(await verificationContext.RegistrationInventoryHolds
            .Where(hold => hold.CapacityPoolId == poolId && hold.RegistrationInventoryHoldStatusId == (int)RegistrationInventoryHoldStatusEnum.Active)
            .CountAsync(timeout.Token)).IsEqualTo(1);
    }

    [Test]
    public async Task ExpiredHold_AtomicallyReleasesCapacityAndMovesOrderToReconciliation()
    {
        (Guid tenantId, Guid eventId, Guid poolId, Guid ticketTypeId, Guid _) = await SeedAsync();
        DateTime createdAt = UtcNow.AddMinutes(-20);
        DateTime expiresAt = UtcNow.AddMinutes(-5);
        Guid orderId = Guid.CreateVersion7();
        Guid holdId = Guid.CreateVersion7();
        await using (ExploreDbContext setupContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId)))
        {
            var catalogs = new EventTicketCatalogRepository(setupContext);
            var inventory = new RegistrationInventoryRepository(setupContext);
            EventTicketCatalogVersion catalog = (await catalogs.GetPublishedCatalogAsync(eventId, tenantId, CancellationToken.None))!;
            EventTicketType ticketType = catalog.TicketTypes.Single(ticket => ticket.Id == ticketTypeId);
            RegistrationOrder order = RegistrationOrder.Create(
                orderId,
                tenantId,
                eventId,
                accountUserId: Guid.CreateVersion7(),
                purchaserActorId: null,
                bookingPartyType: BookingPartyTypeEnum.Individual,
                ticketCatalogVersionId: catalog.Id,
                participationSnapshot: RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
                registrationWorkflowVersionId: null,
                guestAccessTokenHash: null,
                currencyCode: catalog.CurrencyCode,
                createdAt,
                expiresAt);
            order.AddLine(RegistrationOrderLine.Create(
                Guid.CreateVersion7(),
                catalog,
                ticketType,
                order.Id,
                quantity: 1,
                chosenUnitPriceAmount: null,
                platformFeePolicy: null));
            order.TransitionTo(RegistrationOrderStatusEnum.AwaitingParticipantDetails, createdAt);
            RegistrationInventoryHold hold = RegistrationInventoryHold.Create(
                holdId,
                order.Id,
                poolId,
                ticketType.Id,
                tenantId,
                quantity: 1,
                createdAt,
                expiresAt);
            await inventory.AddOrderWithHoldsAsync(order, [hold], CancellationToken.None);
            await inventory.SaveChangesAsync(CancellationToken.None);
        }

        await using (ExploreDbContext expiryContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId)))
        {
            var inventory = new RegistrationInventoryRepository(expiryContext);
            await Assert.That(await inventory.TryExpireDueHoldAsync(holdId, UtcNow, CancellationToken.None)).IsTrue();
        }

        await using ExploreDbContext verificationContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        var verificationRepository = new RegistrationInventoryRepository(verificationContext);
        RegistrationOrder persistedOrder = await verificationContext.RegistrationOrders.SingleAsync(order => order.Id == orderId);
        await Assert.That(persistedOrder.RegistrationOrderStatusId).IsEqualTo((int)RegistrationOrderStatusEnum.NeedsReconciliation);
        await Assert.That(await verificationRepository.GetAllocatedQuantityAsync(poolId, tenantId, CancellationToken.None)).IsEqualTo(0);
    }

    [Test]
    public async Task RecoveredHoldConsumption_RetainsExpiredAuditRowAndConsumesOnlyTheReplacementHold()
    {
        (Guid tenantId, Guid eventId, Guid poolId, Guid ticketTypeId, Guid _) = await SeedAsync();
        Guid orderId = Guid.CreateVersion7();
        Guid expiredHoldId = Guid.CreateVersion7();
        Guid replacementHoldId = Guid.CreateVersion7();
        await using (ExploreDbContext setupContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId)))
        {
            var catalogs = new EventTicketCatalogRepository(setupContext);
            var inventory = new RegistrationInventoryRepository(setupContext);
            EventTicketCatalogVersion catalog = (await catalogs.GetPublishedCatalogAsync(eventId, tenantId, CancellationToken.None))!;
            EventTicketType ticketType = catalog.TicketTypes.Single(ticket => ticket.Id == ticketTypeId);
            RegistrationOrder order = RegistrationOrder.Create(
                orderId,
                tenantId,
                eventId,
                accountUserId: null,
                purchaserActorId: null,
                bookingPartyType: BookingPartyTypeEnum.Individual,
                ticketCatalogVersionId: catalog.Id,
                participationSnapshot: RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
                registrationWorkflowVersionId: null,
                guestAccessTokenHash: CreateGuestCapabilityHash(orderId),
                currencyCode: catalog.CurrencyCode,
                createdAt: UtcNow.AddMinutes(-15),
                expiresAt: UtcNow.AddMinutes(15));
            order.AddLine(RegistrationOrderLine.Create(Guid.CreateVersion7(), catalog, ticketType, order.Id, 1, null, null));
            RegistrationInventoryHold expiredHold = RegistrationInventoryHold.Create(
                expiredHoldId,
                order.Id,
                poolId,
                ticketType.Id,
                tenantId,
                1,
                UtcNow.AddMinutes(-15),
                UtcNow.AddMinutes(-1));
            expiredHold.TryExpire(UtcNow);
            RegistrationInventoryHold replacementHold = RegistrationInventoryHold.Create(
                replacementHoldId,
                order.Id,
                poolId,
                ticketType.Id,
                tenantId,
                1,
                UtcNow,
                UtcNow.AddMinutes(15));
            await inventory.AddOrderWithHoldsAsync(order, [expiredHold, replacementHold], CancellationToken.None);
            await inventory.SaveChangesAsync(CancellationToken.None);

            await Assert.That(await inventory.TryConsumeActiveHoldsForOrderAsync(order.Id, tenantId, UtcNow, CancellationToken.None)).IsEqualTo(1);
        }

        await using ExploreDbContext verificationContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        RegistrationInventoryHold[] holds = await verificationContext.RegistrationInventoryHolds
            .Where(hold => hold.RegistrationOrderId == orderId)
            .ToArrayAsync();
        RegistrationInventoryHold expiredHoldAudit = holds.Single(hold => hold.Id == expiredHoldId);
        RegistrationInventoryHold consumedReplacementHold = holds.Single(hold => hold.Id == replacementHoldId);
        var verificationRepository = new RegistrationInventoryRepository(verificationContext);

        await Assert.That(expiredHoldAudit.RegistrationInventoryHoldStatusId).IsEqualTo((int)RegistrationInventoryHoldStatusEnum.Expired);
        await Assert.That(expiredHoldAudit.ConsumedAt).IsNull();
        await Assert.That(consumedReplacementHold.RegistrationInventoryHoldStatusId).IsEqualTo((int)RegistrationInventoryHoldStatusEnum.Consumed);
        await Assert.That(consumedReplacementHold.ConsumedAt).IsEqualTo(UtcNow);
        await Assert.That(await verificationRepository.GetAllocatedQuantityAsync(poolId, tenantId, CancellationToken.None)).IsEqualTo(1);
    }

    [Test]
    public async Task CancelledOrderWithoutParticipants_ReleasesEveryActiveLineHold()
    {
        (Guid tenantId, Guid eventId, Guid poolId, Guid firstTicketTypeId, Guid secondTicketTypeId) =
            await SeedAsync(maximumQuantity: 2);
        Guid orderId = Guid.CreateVersion7();

        await using (ExploreDbContext setupContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId)))
        {
            var catalogs = new EventTicketCatalogRepository(setupContext);
            var inventory = new RegistrationInventoryRepository(setupContext);
            EventTicketCatalogVersion catalog = (await catalogs.GetPublishedCatalogAsync(eventId, tenantId, CancellationToken.None))!;
            EventTicketType firstTicket = catalog.TicketTypes.Single(ticket => ticket.Id == firstTicketTypeId);
            EventTicketType secondTicket = catalog.TicketTypes.Single(ticket => ticket.Id == secondTicketTypeId);
            RegistrationOrder order = RegistrationOrder.Create(
                orderId,
                tenantId,
                eventId,
                accountUserId: null,
                purchaserActorId: null,
                bookingPartyType: BookingPartyTypeEnum.Individual,
                ticketCatalogVersionId: catalog.Id,
                participationSnapshot: RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
                registrationWorkflowVersionId: null,
                guestAccessTokenHash: CreateGuestCapabilityHash(orderId),
                currencyCode: catalog.CurrencyCode,
                createdAt: UtcNow,
                expiresAt: UtcNow.AddMinutes(15));
            order.AddLine(RegistrationOrderLine.Create(Guid.CreateVersion7(), catalog, firstTicket, order.Id, 1, null, null));
            order.AddLine(RegistrationOrderLine.Create(Guid.CreateVersion7(), catalog, secondTicket, order.Id, 1, null, null));
            order.TransitionTo(RegistrationOrderStatusEnum.AwaitingParticipantDetails, UtcNow);

            RegistrationInventoryHold[] holds =
            [
                RegistrationInventoryHold.Create(Guid.CreateVersion7(), order.Id, poolId, firstTicket.Id, tenantId, 1, UtcNow, UtcNow.AddMinutes(15)),
                RegistrationInventoryHold.Create(Guid.CreateVersion7(), order.Id, poolId, secondTicket.Id, tenantId, 1, UtcNow, UtcNow.AddMinutes(15))
            ];
            await inventory.AddOrderWithHoldsAsync(order, holds, CancellationToken.None);
            await inventory.SaveChangesAsync(CancellationToken.None);

            int released = await inventory.TryReleaseActiveHoldsForOrderAsync(
                order.Id,
                tenantId,
                RegistrationInventoryHoldStatusEnum.Cancelled,
                UtcNow,
                CancellationToken.None);

            await Assert.That(released).IsEqualTo(2);
        }

        await using ExploreDbContext verificationContext = fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantId));
        var verificationRepository = new RegistrationInventoryRepository(verificationContext);
        int[] holdStatusIds = await verificationContext.RegistrationInventoryHolds
            .Where(hold => hold.RegistrationOrderId == orderId)
            .OrderBy(hold => hold.Id)
            .Select(hold => hold.RegistrationInventoryHoldStatusId)
            .ToArrayAsync();
        await Assert.That(holdStatusIds).IsEquivalentTo(
            [(int)RegistrationInventoryHoldStatusEnum.Cancelled, (int)RegistrationInventoryHoldStatusEnum.Cancelled]);
        await Assert.That(await verificationContext.EventRegistrations.CountAsync(registration => registration.RegistrationOrderId == orderId)).IsEqualTo(0);
        await Assert.That(await verificationRepository.GetAllocatedQuantityAsync(poolId, tenantId, CancellationToken.None)).IsEqualTo(0);
    }

    private async Task<bool> ReserveAsync(
        Guid tenantId,
        Guid eventId,
        Guid poolId,
        Guid ticketTypeId,
        CancellationToken cancellationToken)
    {
        await using ExploreDbContext context = CreateRetryingTenantContext(tenantId);
        var inventory = new RegistrationInventoryRepository(context);
        var catalogs = new EventTicketCatalogRepository(context);
        var unitOfWork = new EfCoreUnitOfWork(context);
        Guid orderId = Guid.CreateVersion7();
        Guid accountUserId = Guid.CreateVersion7();
        Guid lineId = Guid.CreateVersion7();
        Guid holdId = Guid.CreateVersion7();

        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            EventTicketCatalogVersion catalog = (await catalogs.GetPublishedCatalogAsync(eventId, tenantId, token))!;
            EventTicketType ticketType = catalog.TicketTypes.Single(ticket => ticket.Id == ticketTypeId);
            EventCapacityPool pool = (await inventory.GetPoolsForUpdateAsync([poolId], eventId, tenantId, token)).Single();
            int allocated = await inventory.GetAllocatedQuantityAsync(pool.Id, tenantId, token);
            if (pool.MaximumQuantity is int maximumQuantity && allocated >= maximumQuantity)
            {
                return false;
            }

            RegistrationOrder order = RegistrationOrder.Create(
                orderId,
                tenantId,
                eventId,
                accountUserId,
                purchaserActorId: null,
                bookingPartyType: BookingPartyTypeEnum.Individual,
                ticketCatalogVersionId: catalog.Id,
                participationSnapshot: RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
                registrationWorkflowVersionId: null,
                guestAccessTokenHash: null,
                currencyCode: catalog.CurrencyCode,
                createdAt: UtcNow,
                expiresAt: UtcNow.AddSeconds(pool.HoldDurationSeconds));
            order.AddLine(RegistrationOrderLine.Create(
                lineId,
                catalog,
                ticketType,
                order.Id,
                quantity: 1,
                chosenUnitPriceAmount: null,
                platformFeePolicy: null));
            RegistrationInventoryHold hold = RegistrationInventoryHold.Create(
                holdId,
                order.Id,
                pool.Id,
                ticketType.Id,
                tenantId,
                quantity: 1,
                UtcNow,
                UtcNow.AddSeconds(pool.HoldDurationSeconds));

            await inventory.AddOrderWithHoldsAsync(order, [hold], token);
            await inventory.SaveChangesAsync(token);
            return true;
        }, cancellationToken);
    }

    private async Task<bool> ReserveNonTimedAtReadyAsync(
        Guid tenantId,
        Guid eventId,
        Guid poolId,
        Guid ticketTypeId,
        CancellationToken cancellationToken)
    {
        await using ExploreDbContext context = CreateRetryingTenantContext(tenantId);
        var inventory = new RegistrationInventoryRepository(context);
        var catalogs = new EventTicketCatalogRepository(context);
        var unitOfWork = new EfCoreUnitOfWork(context);
        Guid orderId = Guid.CreateVersion7();
        Guid lineId = Guid.CreateVersion7();
        Guid holdId = Guid.CreateVersion7();

        return await unitOfWork.ExecuteSerializableAsync(async token =>
        {
            EventTicketCatalogVersion catalog = (await catalogs.GetPublishedCatalogAsync(eventId, tenantId, token))!;
            EventTicketType ticketType = catalog.TicketTypes.Single(ticket => ticket.Id == ticketTypeId);
            RegistrationOrder order = RegistrationOrder.Create(
                orderId,
                tenantId,
                eventId,
                accountUserId: null,
                purchaserActorId: null,
                bookingPartyType: BookingPartyTypeEnum.Individual,
                ticketCatalogVersionId: catalog.Id,
                participationSnapshot: RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 4, 3, 2, GuestRecoveryPolicyEnum.VerifiedEmailRequired),
                registrationWorkflowVersionId: null,
                guestAccessTokenHash: CreateGuestCapabilityHash(orderId),
                currencyCode: catalog.CurrencyCode,
                createdAt: UtcNow,
                expiresAt: null);
            order.AddLine(RegistrationOrderLine.Create(lineId, catalog, ticketType, order.Id, 1, null, null));
            order.TransitionTo(RegistrationOrderStatusEnum.AwaitingParticipantDetails, UtcNow);
            order.TransitionTo(RegistrationOrderStatusEnum.AwaitingRequirements, UtcNow);
            await inventory.AddOrderWithHoldsAsync(order, [], token);
            await inventory.SaveChangesAsync(token);

            RegistrationInventoryReservationResult reservation = await inventory.ReserveNonTimedHoldsAsync(
                eventId,
                tenantId,
                [new RegistrationInventoryReservation(holdId, order.Id, poolId, ticketType.Id, 1)],
                approvalGranted: false,
                UtcNow,
                token);
            return reservation.Reserved;
        }, cancellationToken);
    }

    private ExploreDbContext CreateRetryingTenantContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<ExploreDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql => npgsql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null))
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(warnings => warnings.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new ExploreDbContext(options)
        {
            TenantContext = new TestTenantContext(tenantId)
        };
    }

    private static CapabilityTokenHash CreateGuestCapabilityHash(Guid orderId) =>
        CapabilityTokenHash.Create(Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(orderId.ToByteArray())));

    private async Task<(Guid TenantId, Guid EventId, Guid PoolId, Guid FirstTicketTypeId, Guid SecondTicketTypeId)> SeedAsync(
        int maximumQuantity = 1,
        CapacityHoldPolicyEnum holdPolicy = CapacityHoldPolicyEnum.TimedHoldOnSelection)
    {
        await fixture.ResetAsync();
        await using ExploreDbContext context = fixture.CreateDbContext();
        TenantStatus activeStatus = await context.TenantStatuses.SingleAsync(status => status.Id == (int)TenantStatusEnum.Active);
        var tenant = new Tenant { FullName = "Registration hold race tenant", Slug = $"registration-hold-race-{Guid.NewGuid():N}", TenantStatusId = activeStatus.Id, TenantStatus = activeStatus };
        context.Tenants.Add(tenant);
        var user = new User { Pii = new UserPii { Email = "registration-hold-race@example.test", FirstName = "Registration", LastName = "Race" } };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var actor = new Actor { Pii = new ActorPii { DisplayName = "Registration Hold Race Actor" }, ActorTypeId = 1, ActorType = null!, UserId = user.Id };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();

        Guid eventId = Guid.CreateVersion7();
        var eventTarget = new DomainEvent
        {
            Id = eventId,
            Title = "Registration hold race event",
            Subtitle = string.Empty,
            Description = string.Empty,
            FirstSessionDate = DateOnly.FromDateTime(UtcNow.AddDays(1)),
            LastSessionDate = DateOnly.FromDateTime(UtcNow.AddDays(1)),
            EventTypeId = 1,
            AudienceGenderId = 1,
            AudienceAgeId = 1,
            ActorId = actor.Id,
            Actor = null!,
            OrganizerActorId = actor.Id,
            TenantId = tenant.Id,
            Tenant = tenant,
            VisibilityTypeId = 1,
            VisibilityType = null!,
            EventStatusId = 1,
            EventStatus = null!,
            EventFormatId = 1,
            EventFormat = null!,
            EventProvenanceTypeId = (int)EventProvenanceTypeEnum.OrganizerCreated,
            TotalViews = 0
        };
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(tenant.Id, eventId, "USD", 1);
        EventCapacityPool pool = EventCapacityPool.Create(tenant.Id, eventId, "Shared last seat", maximumQuantity, 900, holdPolicy, CapacityOversellPolicyEnum.Disallow, true);
        EventTicketType firstTicket = CreateTicketType(catalog, "General", pool.Id);
        EventTicketType secondTicket = CreateTicketType(catalog, "Student", pool.Id);
        catalog.AddTicketType(firstTicket, pool);
        catalog.AddTicketType(secondTicket, pool);
        catalog.AddEntitlement(firstTicket, TicketTypeEntitlement.CreateForEvent(firstTicket.Id, tenant.Id, eventId, 1));
        catalog.AddEntitlement(secondTicket, TicketTypeEntitlement.CreateForEvent(secondTicket.Id, tenant.Id, eventId, 1));
        catalog.Publish();
        context.AddRange(eventTarget, catalog, pool);
        await context.SaveChangesAsync();

        return (tenant.Id, eventId, pool.Id, firstTicket.Id, secondTicket.Id);
    }

    private static EventTicketType CreateTicketType(EventTicketCatalogVersion catalog, string name, Guid poolId) => EventTicketType.Create(
        Guid.CreateVersion7(),
        catalog.TenantId,
        catalog.Id,
        name,
        catalog.CurrencyCode,
        TicketPricingModeEnum.Free,
        fixedPriceMinor: null,
        minimumPriceMinor: null,
        suggestedPriceMinor: null,
        ParticipantDataCollectionModeEnum.None,
        poolId,
        minimumAge: null,
        maximumAge: null,
        requiresGuardian: false,
        requiresApproval: false,
        perOrderLimit: null,
        perAccountLimit: null,
        perVerifiedContactLimit: null,
        perBookingPartyLimit: null);

    private async Task<long> ScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;
}
