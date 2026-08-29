// ABOUTME: Defines prospective PostgreSQL contracts for optional event-bound add-on commerce.
// ABOUTME: Pins checked money, inventory, fulfillment, refund, tenancy, replay, and admission isolation.

using System.Collections;
using System.Reflection;
using Event.Persistence.IntegrationTests.Fixtures;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using Explore.Persistence;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Persistence.IntegrationTests;

[ClassDataSource<PostgreSqlContainerFixture>(Shared = SharedType.PerAssembly)]
[NotInParallel("PersistenceDb")]
public sealed class EventAddOnPersistenceTests(PostgreSqlContainerFixture fixture)
{
    private const string CatalogTypeName = "Explore.Domain.EventAddOnCatalogVersion";
    private const string ItemTypeName = "Explore.Domain.EventAddOnCatalogItem";
    private const string LineTypeName = "Explore.Domain.RegistrationOrderAddOnLine";
    private const string AllocationTypeName = "Explore.Domain.EventAddOnInventoryAllocation";
    private const string FulfillmentTypeName = "Explore.Domain.EventAddOnFulfillment";
    private const string RefundAllocationTypeName = "Explore.Domain.EventAddOnRefundAllocation";
    private const string RepositoryTypeName = "Explore.Persistence.Repositories.EventAddOnRepository";
    private static readonly DateTime UtcNow = new(2026, 8, 29, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task CatalogIsAMultiItemEventOwnedCollectionRatherThanASingleScalar()
    {
        Type? catalog = DomainType(CatalogTypeName);
        Type? item = DomainType(ItemTypeName);

        await Assert.That(catalog).IsNotNull()
            .Because("Phase 7 requires one event-owned add-on catalog");
        await Assert.That(item).IsNotNull()
            .Because("the organizer must be able to publish multiple independently governed add-ons");
        if (catalog is null || item is null)
        {
            return;
        }

        await Assert.That(HasProperties(
                catalog,
                "TenantId",
                "EventId",
                "CurrencyCode",
                "VersionNumber",
                "PublishedAt",
                "RetiredAt",
                "Items"))
            .IsTrue();
        await Assert.That(HasMethods(catalog, "AddItem", "Publish", "Retire")).IsTrue();
        await Assert.That(HasProperties(
                item,
                "TenantId",
                "EventAddOnCatalogVersionId",
                "Name",
                "Description",
                "UnitPriceMinor",
                "CurrencyCode",
                "InventoryCapacity",
                "FulfillmentDisclosure",
                "RefundDisclosure"))
            .IsTrue();
    }

    [Test]
    public async Task BuyerMaySelectZeroOneOrSeveralItemsWithLiteralIndependentTotals()
    {
        AddOnReflectionSurface? surface = await RequireSurfaceAsync();
        if (surface is null)
        {
            return;
        }

        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        RegistrationOrder emptyOrder = CreateOrder(tenantId, eventId);
        await Assert.That(surface.CollectionCount(emptyOrder, "AddOnLines")).IsEqualTo(0);
        await Assert.That(surface.Read<long>(emptyOrder, "AddOnTotalMinorSnapshot")).IsEqualTo(0L);

        object catalog = surface.CreateCatalog(tenantId, eventId, "EUR", 1);
        object lunch = surface.CreateItem(
            catalog,
            "Lunch package",
            "One prepared meal.",
            1_200,
            50,
            "Collect at the event service desk.",
            "Unfulfilled quantities may be refunded under the accepted policy.");
        object parking = surface.CreateItem(
            catalog,
            "Parking pass",
            "One event-day parking allocation.",
            500,
            100,
            "Present the fulfillment receipt at the parking desk.",
            "Unfulfilled quantities may be refunded under the accepted policy.");
        surface.AddItem(catalog, lunch);
        surface.AddItem(catalog, parking);
        surface.Publish(catalog, UtcNow);

        RegistrationOrder mixedOrder = CreateOrder(tenantId, eventId);
        surface.PinCatalog(mixedOrder, catalog);
        object lunchLine = surface.CreateLine(mixedOrder, catalog, lunch, quantity: 2);
        object parkingLine = surface.CreateLine(mixedOrder, catalog, parking, quantity: 1);
        surface.AddLine(mixedOrder, lunchLine);
        surface.AddLine(mixedOrder, parkingLine);

        await Assert.That(surface.CollectionCount(mixedOrder, "AddOnLines")).IsEqualTo(2);
        await Assert.That(surface.Read<long>(lunchLine, "LineTotalMinorSnapshot")).IsEqualTo(2_400L);
        await Assert.That(surface.Read<long>(parkingLine, "LineTotalMinorSnapshot")).IsEqualTo(500L);
        await Assert.That(surface.Read<long>(mixedOrder, "AddOnTotalMinorSnapshot")).IsEqualTo(2_900L);
        await Assert.That(surface.Read<string>(lunchLine, "NameSnapshot")).IsEqualTo("Lunch package");
        await Assert.That(surface.Read<string>(lunchLine, "CurrencyCodeSnapshot")).IsEqualTo("EUR");
    }

    [Test]
    public async Task OverflowFailsBeforeOrderInventoryFulfillmentOrRefundEffects()
    {
        AddOnReflectionSurface? surface = await RequireSurfaceAsync();
        if (surface is null)
        {
            return;
        }

        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        object catalog = surface.CreateCatalog(tenantId, eventId, "EUR", 1);
        object item = surface.CreateItem(
            catalog,
            "Overflow sentinel",
            null,
            long.MaxValue,
            2,
            "No fulfillment should be created.",
            "No refund should be created.");
        surface.AddItem(catalog, item);
        surface.Publish(catalog, UtcNow);
        RegistrationOrder order = CreateOrder(tenantId, eventId);

        Exception? failure = surface.CaptureLineCreationFailure(order, catalog, item, quantity: 2);

        await Assert.That(failure).IsTypeOf<OverflowException>();
        await Assert.That(surface.CollectionCount(order, "AddOnLines")).IsEqualTo(0);
        await Assert.That(surface.Read<long>(order, "AddOnTotalMinorSnapshot")).IsEqualTo(0L);
    }

    [Test]
    public async Task PersistenceModelEnforcesTenantLineageReplayAndAdmissionSeparation()
    {
        await using ExploreDbContext context = fixture.CreateDbContext();
        IModel model = context.GetService<IDesignTimeModel>().Model;
        (string Name, string[][] UniqueSlots)[] expectations =
        [
            (CatalogTypeName, [["TenantId", "EventId", "VersionNumber"]]),
            (ItemTypeName, [["TenantId", "EventAddOnCatalogVersionId", "Id"]]),
            (LineTypeName, [["TenantId", "RegistrationOrderId", "EventAddOnCatalogItemId"]]),
            (AllocationTypeName,
                [
                    ["TenantId", "OperationId"],
                    ["TenantId", "RegistrationOrderAddOnLineId", "ActiveUniquenessSlot"],
                ]),
            (FulfillmentTypeName,
                [
                    ["TenantId", "OperationId"],
                    ["TenantId", "RegistrationOrderAddOnLineId"],
                ]),
            (RefundAllocationTypeName, [["TenantId", "RefundOperationId"]]),
        ];

        foreach ((string name, string[][] uniqueSlots) in expectations)
        {
            IEntityType? entity = model.FindEntityType(name);
            await Assert.That(entity).IsNotNull()
                .Because($"{name} must be a first-class tenant-scoped persistence concept");
            if (entity is null)
            {
                continue;
            }

            await Assert.That(entity.FindDeclaredQueryFilter(QueryFilterNames.Tenant)).IsNotNull();
            foreach (string[] slot in uniqueSlots)
            {
                await Assert.That(entity.GetIndexes().Any(index =>
                        index.IsUnique &&
                        index.Properties.Select(property => property.Name).SequenceEqual(slot)))
                    .IsTrue()
                    .Because($"{name} must own the replay/winner slot {string.Join(',', slot)}");
            }

            await Assert.That(entity.GetForeignKeys().Any(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType.Name.Contains(
                        "Admission",
                        StringComparison.OrdinalIgnoreCase)))
                .IsFalse();
            await Assert.That(entity.GetProperties().Any(property =>
                    ForbiddenAdmissionFragments.Any(fragment =>
                        property.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase))))
                .IsFalse();
        }
    }

    [Test]
    public async Task RepositoryExposesAtomicInventoryFulfillmentRefundAndTenantReadPrimitives()
    {
        Type? repository = PersistenceType(RepositoryTypeName);
        await Assert.That(repository).IsNotNull();
        if (repository is null)
        {
            return;
        }

        await Assert.That(HasMethods(
                repository,
                "ReserveInventoryAsync",
                "FulfillAsync",
                "AllocateRefundAsync",
                "GetOrderWithAddOnsAsync"))
            .IsTrue();
        await Assert.That(repository.GetField(
                "CanonicalFenceOrder",
                BindingFlags.Public | BindingFlags.Static)?.GetRawConstantValue())
            .IsEqualTo("catalog-item>order>line>inventory>fulfillment>refund");

        await AssertEnumNamesAsync(
            "Explore.Domain.EventAddOnInventoryOutcome",
            "Reserved",
            "AlreadyReserved",
            "InsufficientInventory",
            "NotFound",
            "TenantMismatch");
        await AssertEnumNamesAsync(
            "Explore.Domain.EventAddOnFulfillmentOutcome",
            "Fulfilled",
            "AlreadyFulfilled",
            "NotReserved",
            "NotFound",
            "TenantMismatch");
        await AssertEnumNamesAsync(
            "Explore.Domain.EventAddOnRefundOutcome",
            "Allocated",
            "AlreadyAllocated",
            "ExceedsCapturedAmount",
            "NotFound",
            "TenantMismatch");
    }

    [Test]
    public async Task TwoBuyersRacingForTheLastUnitProduceExactlyOneWinner()
    {
        AddOnReflectionSurface? surface = await RequireSurfaceAsync();
        if (surface is null)
        {
            return;
        }

        AddOnSeed seed = await SeedAsync(surface, "inventory-race", capacity: 1, unitPriceMinor: 500, orderCount: 2);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var gate = new ExactRaceGate(2);

        async Task<object> ReserveAsync(Guid lineId)
        {
            await using ExploreDbContext context =
                fixture.CreateTenantFilteredDbContext(new TestTenantContext(seed.TenantId));
            object repository = surface.CreateRepository(context);
            await gate.ArriveAsync(timeout.Token);
            return await surface.ReserveInventoryAsync(
                repository,
                seed.TenantId,
                seed.EventId,
                lineId,
                Guid.CreateVersion7(),
                UtcNow,
                timeout.Token);
        }

        Task<object>[] contenders =
        [
            ReserveAsync(seed.LineIds[0]),
            ReserveAsync(seed.LineIds[1]),
        ];
        await gate.AllArrived.WaitAsync(timeout.Token);
        gate.Release();
        object[] results = await Task.WhenAll(contenders);

        await Assert.That(results.Count(result => surface.Outcome(result) == "Reserved")).IsEqualTo(1);
        await Assert.That(results.Count(result => surface.Outcome(result) == "InsufficientInventory")).IsEqualTo(1);
        await using ExploreDbContext verification =
            fixture.CreateTenantFilteredDbContext(new TestTenantContext(seed.TenantId));
        object[] allocations = surface.Rows(verification, surface.AllocationType);
        await Assert.That(allocations.Length).IsEqualTo(1);
        await Assert.That(allocations.Sum(allocation => surface.Read<int>(allocation, "Quantity"))).IsEqualTo(1);
    }

    [Test]
    public async Task PartialRefundConservesValueAndFulfillmentReplayCreatesOneOutcome()
    {
        AddOnReflectionSurface? surface = await RequireSurfaceAsync();
        if (surface is null)
        {
            return;
        }

        AddOnSeed seed = await SeedAsync(surface, "refund-fulfillment", capacity: 3, unitPriceMinor: 333, orderCount: 1, quantity: 3);
        Guid lineId = seed.LineIds.Single();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using ExploreDbContext context =
            fixture.CreateTenantFilteredDbContext(new TestTenantContext(seed.TenantId));
        object repository = surface.CreateRepository(context);

        object reservation = await surface.ReserveInventoryAsync(
            repository,
            seed.TenantId,
            seed.EventId,
            lineId,
            Guid.CreateVersion7(),
            UtcNow,
            timeout.Token);
        await Assert.That(surface.Outcome(reservation)).IsEqualTo("Reserved");

        Guid fulfillmentOperationId = Guid.CreateVersion7();
        object firstFulfillment = await surface.FulfillAsync(
            repository,
            seed.TenantId,
            seed.EventId,
            lineId,
            fulfillmentOperationId,
            UtcNow.AddMinutes(1),
            timeout.Token);
        object replayedFulfillment = await surface.FulfillAsync(
            repository,
            seed.TenantId,
            seed.EventId,
            lineId,
            fulfillmentOperationId,
            UtcNow.AddMinutes(1),
            timeout.Token);

        Guid refundOperationId = Guid.CreateVersion7();
        await SeedRefundAttemptAsync(
            context,
            seed,
            lineId,
            refundOperationId,
            purchasedQuantity: 3,
            unitPriceMinor: 333,
            amountMinor: 666,
            timeout.Token);
        object firstRefund = await surface.AllocateRefundAsync(
            repository,
            seed.TenantId,
            seed.EventId,
            lineId,
            refundOperationId,
            quantity: 2,
            UtcNow.AddMinutes(2),
            timeout.Token);
        object replayedRefund = await surface.AllocateRefundAsync(
            repository,
            seed.TenantId,
            seed.EventId,
            lineId,
            refundOperationId,
            quantity: 2,
            UtcNow.AddMinutes(2),
            timeout.Token);
        object excessRefund = await surface.AllocateRefundAsync(
            repository,
            seed.TenantId,
            seed.EventId,
            lineId,
            Guid.CreateVersion7(),
            quantity: 2,
            UtcNow.AddMinutes(3),
            timeout.Token);

        await Assert.That(surface.Outcome(firstFulfillment)).IsEqualTo("Fulfilled");
        await Assert.That(surface.Outcome(replayedFulfillment)).IsEqualTo("AlreadyFulfilled");
        await Assert.That(surface.Outcome(firstRefund)).IsEqualTo("Allocated");
        await Assert.That(surface.Outcome(replayedRefund)).IsEqualTo("AlreadyAllocated");
        await Assert.That(surface.Outcome(excessRefund)).IsEqualTo("ExceedsCapturedAmount");
        context.ChangeTracker.Clear();
        object[] fulfillments = surface.Rows(context, surface.FulfillmentType);
        object[] refunds = surface.Rows(context, surface.RefundAllocationType);
        object[] allocations = surface.Rows(context, surface.AllocationType);
        await Assert.That(fulfillments.Length).IsEqualTo(1);
        await Assert.That(refunds.Length).IsEqualTo(1);
        await Assert.That(allocations.Length).IsEqualTo(1);
        await Assert.That(surface.Read<int>(allocations[0], "ReleasedQuantity")).IsEqualTo(0);
        await Assert.That(surface.Read<Guid?>(allocations[0], "ActiveUniquenessSlot")).IsNotNull();
        await Assert.That(surface.Read<object>(refunds[0], "Status").ToString())
            .IsEqualTo("PendingProvider");
        await Assert.That(refunds.Sum(refund => surface.Read<long>(refund, "AmountMinor"))).IsEqualTo(666L);
        await Assert.That(refunds.Sum(refund => surface.Read<int>(refund, "Quantity"))).IsEqualTo(2);

        object? resolved = await surface.ResolveRefundAsync(
            repository,
            seed.TenantId,
            refundOperationId,
            providerSucceeded: true,
            UtcNow.AddMinutes(4),
            timeout.Token);
        await Assert.That(resolved).IsNotNull();
        context.ChangeTracker.Clear();
        allocations = surface.Rows(context, surface.AllocationType);
        refunds = surface.Rows(context, surface.RefundAllocationType);
        await Assert.That(surface.Read<int>(allocations[0], "ReleasedQuantity")).IsEqualTo(2);
        await Assert.That(surface.Read<object>(refunds[0], "Status").ToString())
            .IsEqualTo("Confirmed");
    }

    [Test]
    public async Task TenantReadsFailClosedAndAddOnOperationsNeverCreateAdmissionState()
    {
        AddOnReflectionSurface? surface = await RequireSurfaceAsync();
        if (surface is null)
        {
            return;
        }

        AddOnSeed tenantA = await SeedAsync(surface, "tenant-a", capacity: 2, unitPriceMinor: 700, orderCount: 1);
        AddOnSeed tenantB = await SeedAsync(
            surface,
            "tenant-b",
            capacity: 2,
            unitPriceMinor: 700,
            orderCount: 1,
            resetDatabase: false);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using ExploreDbContext context =
            fixture.CreateTenantFilteredDbContext(new TestTenantContext(tenantA.TenantId));
        object repository = surface.CreateRepository(context);

        object? foreignOrder = await surface.GetOrderWithAddOnsAsync(
            repository,
            tenantB.TenantId,
            tenantB.EventId,
            tenantB.OrderIds.Single(),
            timeout.Token);
        await Assert.That(foreignOrder).IsNull();
        await Assert.That(surface.Rows(context, surface.LineType)
                .All(line => surface.Read<Guid>(line, "TenantId") == tenantA.TenantId))
            .IsTrue();

        int ticketCountBefore = await context.AdmissionTickets.CountAsync(timeout.Token);
        int credentialCountBefore = await context.AdmissionTicketCredentials.CountAsync(timeout.Token);
        int checkInCountBefore = await context.AdmissionCheckInEvents.CountAsync(timeout.Token);
        Guid lineId = tenantA.LineIds.Single();
        _ = await surface.ReserveInventoryAsync(
            repository,
            tenantA.TenantId,
            tenantA.EventId,
            lineId,
            Guid.CreateVersion7(),
            UtcNow,
            timeout.Token);
        _ = await surface.FulfillAsync(
            repository,
            tenantA.TenantId,
            tenantA.EventId,
            lineId,
            Guid.CreateVersion7(),
            UtcNow.AddMinutes(1),
            timeout.Token);
        Guid refundOperationId = Guid.CreateVersion7();
        await SeedRefundAttemptAsync(
            context,
            tenantA,
            lineId,
            refundOperationId,
            purchasedQuantity: 1,
            unitPriceMinor: 700,
            amountMinor: 700,
            timeout.Token);
        _ = await surface.AllocateRefundAsync(
            repository,
            tenantA.TenantId,
            tenantA.EventId,
            lineId,
            refundOperationId,
            quantity: 1,
            UtcNow.AddMinutes(2),
            timeout.Token);

        await Assert.That(await context.AdmissionTickets.CountAsync(timeout.Token)).IsEqualTo(ticketCountBefore);
        await Assert.That(await context.AdmissionTicketCredentials.CountAsync(timeout.Token)).IsEqualTo(credentialCountBefore);
        await Assert.That(await context.AdmissionCheckInEvents.CountAsync(timeout.Token)).IsEqualTo(checkInCountBefore);
    }

    private static async Task SeedRefundAttemptAsync(
        ExploreDbContext context,
        AddOnSeed seed,
        Guid lineId,
        Guid refundAttemptId,
        int purchasedQuantity,
        long unitPriceMinor,
        long amountMinor,
        CancellationToken cancellationToken)
    {
        Guid paymentAttemptId = Guid.CreateVersion7();
        OrganizerPaymentRecipientSnapshot recipient =
            OrganizerPaymentRecipientSnapshot.Create(
                seed.TenantId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "stripe",
                "platform-live-eu",
                "acct_add_on",
                "BE",
                "EUR",
                Guid.CreateVersion7(),
                null,
                UtcNow);
        PaidOrderAcceptanceSnapshot acceptance =
            PaidOrderAcceptanceSnapshot.Create(
                paymentAttemptId,
                seed.TenantId,
                seed.TenantId,
                seed.OrderIds.Single(),
                seed.EventId,
                "event-add-on-test",
                "disclosure-1",
                PaidOrderAcceptanceSnapshot.CurrentAcceptanceTemplateIdentifier,
                PaidOrderAcceptanceSnapshot.CurrentAcceptanceTemplateText,
                recipient.OrganizerActorId,
                "Add-on Organizer",
                PaidCheckoutTenantDirectoryOperatorDisclosure.Create(
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    "Community Events",
                    "Community Events ASBL",
                    "registered_organization",
                    "BE",
                    null,
                    "directory@example.test",
                    "https://example.test/legal",
                    "https://example.test/terms",
                    "https://example.test/privacy"),
                PaidCheckoutOperatorDisclosure.Create(
                    Guid.CreateVersion7(),
                    "Example Operator",
                    false,
                    "https://events.example.test",
                    "BE",
                    "https://events.example.test",
                    "https://events.example.test/legal",
                    "https://events.example.test/terms",
                    "https://events.example.test/privacy",
                    "complaints@example.test",
                    "Trust and Safety",
                    "Payments Operations",
                    "Dispute Operations",
                    "Payment Reconciliation",
                    "approved"),
                PaidOrderDeliverySnapshot.Create(
                    new DateTimeOffset(UtcNow.AddDays(1)),
                    new DateTimeOffset(UtcNow.AddDays(1).AddHours(2)),
                    "Europe/Brussels"),
                "EUR",
                MinorUnitMath.Multiply(
                    unitPriceMinor,
                    purchasedQuantity),
                0,
                0,
                MinorUnitMath.Multiply(
                    unitPriceMinor,
                    purchasedQuantity),
                recipient.InstancePolicyVersionId,
                1,
                "Add-on refunds follow the accepted policy.",
                "en-GB",
                "support@example.test",
                PaidCheckoutProviderDisclosure.Create(
                    "stripe",
                    "OrganizerDirect",
                    "direct-charge",
                    "EXAMPLE EVENT",
                    "test",
                    "instance-operator"),
                [
                    PaidOrderAcceptanceLineFact.Create(
                        lineId,
                        "Add-on refund line",
                        purchasedQuantity,
                        unitPriceMinor,
                        0,
                        MinorUnitMath.Multiply(
                            unitPriceMinor,
                            purchasedQuantity)),
                ],
                UtcNow,
                recipient.TenantPolicyVersionId,
                recipient.OrganizerPaymentProviderConnectionId,
                recipient.ConnectPlatformId,
                recipient.ExternalAccountId,
                recipient.MerchantCountryCode);
        PaymentAttempt payment = PaymentAttempt.Create(
            paymentAttemptId,
            seed.TenantId,
            seed.OrderIds.Single(),
            recipient,
            "OrganizerDirect",
            "2026-08-20.acacia",
            "event-add-on-test",
            Money.Create(
                MinorUnitMath.Multiply(
                    unitPriceMinor,
                    purchasedQuantity),
                "EUR"),
            Money.Create(0, "EUR"),
            Money.Create(0, "EUR"),
            $"payment:{seed.TenantId:N}:{paymentAttemptId:N}",
            UtcNow,
            UtcNow.AddMinutes(30));
        payment.AttachAcceptance(acceptance);
        payment.MarkSucceeded(
            $"pi_{paymentAttemptId:N}",
            UtcNow.AddSeconds(1),
            "req_add_on_payment");
        RefundAttempt refund = RefundAttempt.Create(
            refundAttemptId,
            seed.TenantId,
            paymentAttemptId,
            acceptance,
            "acct_add_on",
            $"pi_{paymentAttemptId:N}",
            $"refund:{refundAttemptId:N}",
            amountMinor,
            UtcNow.AddMinutes(2));
        context.PaymentAttempts.Add(payment);
        context.RefundAttempts.Add(refund);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<AddOnReflectionSurface?> RequireSurfaceAsync()
    {
        Type? catalog = DomainType(CatalogTypeName);
        Type? item = DomainType(ItemTypeName);
        Type? line = DomainType(LineTypeName);
        Type? allocation = DomainType(AllocationTypeName);
        Type? fulfillment = DomainType(FulfillmentTypeName);
        Type? refund = DomainType(RefundAllocationTypeName);
        Type? repository = PersistenceType(RepositoryTypeName);
        (Type? Value, string Name)[] required =
        [
            (catalog, CatalogTypeName),
            (item, ItemTypeName),
            (line, LineTypeName),
            (allocation, AllocationTypeName),
            (fulfillment, FulfillmentTypeName),
            (refund, RefundAllocationTypeName),
            (repository, RepositoryTypeName),
        ];
        foreach ((Type? value, string name) in required)
        {
            await Assert.That(value).IsNotNull().Because($"Phase 7 product RED requires {name}");
        }

        return required.Any(value => value.Value is null)
            ? null
            : new AddOnReflectionSurface(
                catalog!,
                item!,
                line!,
                allocation!,
                fulfillment!,
                refund!,
                repository!);
    }

    private async Task<AddOnSeed> SeedAsync(
        AddOnReflectionSurface surface,
        string suffix,
        int capacity,
        long unitPriceMinor,
        int orderCount,
        int quantity = 1,
        bool resetDatabase = true)
    {
        if (resetDatabase)
        {
            await fixture.ResetAsync();
        }

        await using ExploreDbContext context = fixture.CreateDbContext();
        var tenant = new Tenant
        {
            FullName = $"Add-on {suffix}",
            Slug = $"add-on-{suffix}-{Guid.CreateVersion7():N}",
            TenantStatusId = (int)TenantStatusEnum.Active,
            TenantStatus = null!,
        };
        var user = new User
        {
            Pii = new UserPii
            {
                Email = $"add-on-{suffix}-{Guid.CreateVersion7():N}@example.test",
                FirstName = "Add-on",
                LastName = suffix,
            },
        };
        context.AddRange(tenant, user);
        await context.SaveChangesAsync();
        var actor = new Actor
        {
            Pii = new ActorPii { DisplayName = $"Add-on {suffix}" },
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = null!,
            UserId = user.Id,
        };
        context.Actors.Add(actor);
        await context.SaveChangesAsync();
        Guid eventId = Guid.CreateVersion7();
        var eventEntity = new DomainEvent(EventStatusEnum.Draft)
        {
            Id = eventId,
            Title = $"Add-on {suffix}",
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
            EventStatus = null!,
            EventFormatId = 1,
            EventFormat = null!,
            EventProvenanceTypeId = (int)EventProvenanceTypeEnum.OrganizerCreated,
            TotalViews = 0,
        };
        EventTicketCatalogVersion ticketCatalog =
            EventTicketCatalogVersion.Create(tenant.Id, eventId, "EUR", 1);
        EventTicketType ticketType = EventTicketType.Create(
            Guid.CreateVersion7(),
            tenant.Id,
            ticketCatalog.Id,
            "General admission",
            "EUR",
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
        ticketCatalog.AddTicketType(ticketType, null);
        ticketCatalog.AddEntitlement(
            ticketType,
            TicketTypeEntitlement.CreateForEvent(ticketType.Id, tenant.Id, eventId, 1));
        ticketCatalog.Publish();

        object addOnCatalog = surface.CreateCatalog(tenant.Id, eventId, "EUR", 1);
        object addOnItem = surface.CreateItem(
            addOnCatalog,
            $"Add-on {suffix}",
            null,
            unitPriceMinor,
            capacity,
            "Fulfilled at the event service desk.",
            "Unfulfilled quantities may be refunded under the accepted policy.");
        surface.AddItem(addOnCatalog, addOnItem);
        surface.Publish(addOnCatalog, UtcNow);

        var orderIds = new List<Guid>(orderCount);
        var lineIds = new List<Guid>(orderCount);
        for (int index = 0; index < orderCount; index++)
        {
            RegistrationOrder order = RegistrationOrder.Create(
                tenant.Id,
                eventId,
                user.Id,
                actor.Id,
                BookingPartyTypeEnum.Individual,
                ticketCatalog.Id,
                RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 1, 1, 1, null),
                null,
                null,
                "EUR",
                UtcNow,
                UtcNow.AddMinutes(30));
            surface.PinCatalog(order, addOnCatalog);
            order.AddLine(RegistrationOrderLine.Create(
                ticketCatalog,
                ticketType,
                order.Id,
                1,
                null,
                null));
            object addOnLine = surface.CreateLine(order, addOnCatalog, addOnItem, quantity);
            surface.AddLine(order, addOnLine);
            long addOnTotal = MinorUnitMath.Multiply(unitPriceMinor, quantity);
            order.ApplyTotals(RegistrationOrderTotalsSnapshot.Create(
                "EUR",
                addOnTotal,
                0,
                addOnTotal,
                0));
            context.RegistrationOrders.Add(order);
            orderIds.Add(order.Id);
            lineIds.Add(surface.Read<Guid>(addOnLine, "Id"));
        }

        context.Add(eventEntity);
        context.Add(ticketCatalog);
        context.Add(addOnCatalog);
        await context.SaveChangesAsync();
        return new AddOnSeed(tenant.Id, eventId, orderIds, lineIds);
    }

    private static RegistrationOrder CreateOrder(Guid tenantId, Guid eventId) =>
        RegistrationOrder.Create(
            tenantId,
            eventId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            BookingPartyTypeEnum.Individual,
            Guid.CreateVersion7(),
            RegistrationParticipationSnapshot.Create(Guid.CreateVersion7(), 1, 1, 1, null),
            null,
            null,
            "EUR",
            UtcNow,
            UtcNow.AddMinutes(30));

    private static Type? DomainType(string fullName) => typeof(RegistrationOrder).Assembly.GetType(fullName);

    private static Type? PersistenceType(string fullName) => typeof(ExploreDbContext).Assembly.GetType(fullName);

    private static bool HasProperties(Type type, params string[] names) =>
        names.All(name => type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) is not null);

    private static bool HasProperties(IReadOnlyList<IReadOnlyProperty> properties, params string[] names) =>
        properties.Select(property => property.Name).SequenceEqual(names);

    private static bool HasMethods(Type type, params string[] names) =>
        names.All(name => type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static) is not null);

    private static async Task AssertEnumNamesAsync(string typeName, params string[] requiredNames)
    {
        Type? type = DomainType(typeName);
        await Assert.That(type).IsNotNull();
        if (type is null)
        {
            return;
        }

        string[] actual = Enum.GetNames(type);
        foreach (string name in requiredNames)
        {
            await Assert.That(actual).Contains(name);
        }
    }

    private static readonly string[] ForbiddenAdmissionFragments =
    [
        "AdmissionTicket",
        "Credential",
        "CheckIn",
        "ParticipantReadiness",
        "TicketCapacity",
    ];

    private sealed record TestTenantContext(Guid TenantId) : ITenantContext;

    private sealed record AddOnSeed(
        Guid TenantId,
        Guid EventId,
        IReadOnlyList<Guid> OrderIds,
        IReadOnlyList<Guid> LineIds);

    private sealed class ExactRaceGate(int expected)
    {
        private readonly TaskCompletionSource allArrived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int arrivals;

        internal Task AllArrived => allArrived.Task;

        internal async Task ArriveAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref arrivals) == expected)
            {
                allArrived.TrySetResult();
            }

            await release.Task.WaitAsync(cancellationToken);
        }

        internal void Release() => release.TrySetResult();
    }
}

internal sealed class AddOnReflectionSurface(
    Type catalogType,
    Type itemType,
    Type lineType,
    Type allocationType,
    Type fulfillmentType,
    Type refundAllocationType,
    Type repositoryType)
{
    internal Type LineType => lineType;
    internal Type AllocationType => allocationType;
    internal Type FulfillmentType => fulfillmentType;
    internal Type RefundAllocationType => refundAllocationType;

    internal object CreateCatalog(Guid tenantId, Guid eventId, string currencyCode, int versionNumber) =>
        InvokeStatic(
            catalogType,
            "Create",
            [typeof(Guid), typeof(Guid), typeof(string), typeof(int)],
            [tenantId, eventId, currencyCode, versionNumber]);

    internal object CreateItem(
        object catalog,
        string name,
        string? description,
        long unitPriceMinor,
        int inventoryCapacity,
        string fulfillmentDisclosure,
        string refundDisclosure) =>
        InvokeStatic(
            itemType,
            "Create",
            [
                typeof(Guid),
                typeof(Guid),
                typeof(Guid),
                typeof(string),
                typeof(string),
                typeof(Money),
                typeof(int),
                typeof(string),
                typeof(string),
            ],
            [
                Guid.CreateVersion7(),
                Read<Guid>(catalog, "TenantId"),
                Read<Guid>(catalog, "Id"),
                name,
                description,
                Money.Create(unitPriceMinor, Read<string>(catalog, "CurrencyCode")),
                inventoryCapacity,
                fulfillmentDisclosure,
                refundDisclosure,
            ]);

    internal void AddItem(object catalog, object item) =>
        InvokeInstance(catalog, "AddItem", [itemType], [item]);

    internal void Publish(object catalog, DateTime publishedAtUtc) =>
        InvokeInstance(catalog, "Publish", [typeof(DateTime)], [publishedAtUtc]);

    internal object CreateLine(
        RegistrationOrder order,
        object catalog,
        object item,
        int quantity) =>
        InvokeStatic(
            lineType,
            "Create",
            [
                typeof(Guid),
                typeof(RegistrationOrder),
                catalogType,
                itemType,
                typeof(int),
            ],
            [Guid.CreateVersion7(), order, catalog, item, quantity]);

    internal Exception? CaptureLineCreationFailure(
        RegistrationOrder order,
        object catalog,
        object item,
        int quantity)
    {
        try
        {
            _ = CreateLine(order, catalog, item, quantity);
            return null;
        }
        catch (TargetInvocationException exception)
        {
            return exception.InnerException;
        }
    }

    internal void AddLine(RegistrationOrder order, object line) =>
        InvokeInstance(order, "AddAddOnLine", [lineType], [line]);

    internal void PinCatalog(RegistrationOrder order, object catalog) =>
        InvokeInstance(
            order,
            "PinAddOnCatalog",
            [catalogType],
            [catalog]);

    internal object CreateRepository(ExploreDbContext context)
    {
        ConstructorInfo constructor = repositoryType.GetConstructor([typeof(ExploreDbContext)])
            ?? throw new InvalidOperationException(
                $"Phase 7 product RED: missing {repositoryType.FullName}(ExploreDbContext).");
        return constructor.Invoke([context]);
    }

    internal Task<object> ReserveInventoryAsync(
        object repository,
        Guid tenantId,
        Guid eventId,
        Guid lineId,
        Guid operationId,
        DateTime reservedAtUtc,
        CancellationToken cancellationToken) =>
        InvokeTaskResultAsync(
            repository,
            "ReserveInventoryAsync",
            [tenantId, eventId, lineId, operationId, reservedAtUtc, cancellationToken]);

    internal Task<object> FulfillAsync(
        object repository,
        Guid tenantId,
        Guid eventId,
        Guid lineId,
        Guid operationId,
        DateTime fulfilledAtUtc,
        CancellationToken cancellationToken) =>
        InvokeTaskResultAsync(
            repository,
            "FulfillAsync",
            [tenantId, eventId, lineId, operationId, fulfilledAtUtc, cancellationToken]);

    internal Task<object> AllocateRefundAsync(
        object repository,
        Guid tenantId,
        Guid eventId,
        Guid lineId,
        Guid operationId,
        int quantity,
        DateTime allocatedAtUtc,
        CancellationToken cancellationToken) =>
        InvokeTaskResultAsync(
            repository,
            "AllocateRefundAsync",
            [tenantId, eventId, lineId, operationId, quantity, allocatedAtUtc, cancellationToken]);

    internal async Task<object?> ResolveRefundAsync(
        object repository,
        Guid tenantId,
        Guid operationId,
        bool providerSucceeded,
        DateTime resolvedAtUtc,
        CancellationToken cancellationToken) =>
        await InvokeTaskResultAsync(
            repository,
            "ResolveRefundAsync",
            [
                tenantId,
                operationId,
                providerSucceeded,
                resolvedAtUtc,
                cancellationToken,
            ]);

    internal async Task<object?> GetOrderWithAddOnsAsync(
        object repository,
        Guid tenantId,
        Guid eventId,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        object result = await InvokeTaskResultAsync(
            repository,
            "GetOrderWithAddOnsAsync",
            [tenantId, eventId, orderId, cancellationToken]);
        return result;
    }

    internal string Outcome(object result) => Read<object>(result, "Outcome").ToString()!;

    internal T Read<T>(object value, string propertyName)
    {
        PropertyInfo property = value.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                $"Phase 7 product RED: missing property {value.GetType().FullName}.{propertyName}.");
        return (T)property.GetValue(value)!;
    }

    internal int CollectionCount(object value, string propertyName) =>
        ((IEnumerable)Read<object>(value, propertyName)).Cast<object>().Count();

    internal object[] Rows(ExploreDbContext context, Type entityType)
    {
        IQueryable set = (IQueryable)typeof(DbContext)
            .GetMethods()
            .Single(method =>
                method.Name == nameof(DbContext.Set) &&
                method.IsGenericMethod &&
                method.GetParameters().Length == 0)
            .MakeGenericMethod(entityType)
            .Invoke(context, null)!;
        return ((IEnumerable)set).Cast<object>().ToArray();
    }

    private static object InvokeStatic(
        Type type,
        string methodName,
        Type[] parameterTypes,
        object?[] arguments)
    {
        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            parameterTypes,
            modifiers: null)
            ?? throw new InvalidOperationException(
                $"Phase 7 product RED: missing factory {type.FullName}.{methodName}.");
        return method.Invoke(null, arguments)
            ?? throw new InvalidOperationException(
                $"Phase 7 product RED: missing result from {type.FullName}.{methodName}.");
    }

    private static object? InvokeInstance(
        object target,
        string methodName,
        Type[] parameterTypes,
        object?[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            parameterTypes,
            modifiers: null)
            ?? throw new InvalidOperationException(
                $"Phase 7 product RED: missing method {target.GetType().FullName}.{methodName}.");
        return method.Invoke(target, arguments);
    }

    private static async Task<object> InvokeTaskResultAsync(
        object target,
        string methodName,
        object?[] arguments)
    {
        MethodInfo? method = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SingleOrDefault(candidate =>
                candidate.Name == methodName &&
                candidate.GetParameters().Length == arguments.Length);
        if (method is null)
        {
            throw new InvalidOperationException(
                $"Phase 7 product RED: missing method {target.GetType().FullName}.{methodName}.");
        }

        object taskObject = method.Invoke(target, arguments)
            ?? throw new InvalidOperationException(
                $"Phase 7 product RED: missing task from {target.GetType().FullName}.{methodName}.");
        await ((Task)taskObject);
        return taskObject.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(taskObject)!;
    }
}
