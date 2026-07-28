// ABOUTME: Covers immutable ticket catalog publication, cloning, capacity-pool scope, and entitlements.
// ABOUTME: Proves Domain invariants before persistence, checkout, or inventory-hold behavior is introduced.

using Explore.Domain.Enums;

namespace Event.Domain.UnitTests.Entities;

public sealed class EventTicketCatalogVersionTests
{
    [Test]
    public async Task Publish_ThenRetire_FreezesTheCatalogGraph()
    {
        EventTicketCatalogVersion catalog = CreateCatalog();
        EventTicketType ticketType = AddFreeTicketWithEventEntitlement(catalog);

        catalog.Publish();

        await Assert.That(catalog.TicketCatalogStatusId).IsEqualTo((int)TicketCatalogStatusEnum.Published);
        await Assert.That(() => catalog.UpdateTicketPricing(ticketType, TicketPricingModeEnum.Fixed, 1_000, null, null))
            .Throws<InvalidOperationException>();

        catalog.Retire();

        await Assert.That(catalog.TicketCatalogStatusId).IsEqualTo((int)TicketCatalogStatusEnum.Retired);
        await Assert.That(() => catalog.AddEntitlement(ticketType, CreateEventEntitlement(catalog, ticketType)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task CloneToDraft_CreatesIndependentTicketAndEntitlementGraph()
    {
        EventTicketCatalogVersion catalog = CreateCatalog();
        EventTicketType ticketType = AddFreeTicketWithEventEntitlement(catalog);
        catalog.Publish();

        EventTicketCatalogVersion clone = catalog.CloneToDraft();
        EventTicketType clonedTicketType = clone.TicketTypes.Single();

        clone.UpdateTicketPricing(clonedTicketType, TicketPricingModeEnum.Fixed, 1_235, null, null);

        await Assert.That(clone.Id).IsNotEqualTo(catalog.Id);
        await Assert.That(clone.VersionNumber).IsEqualTo(2);
        await Assert.That(clone.TicketCatalogStatusId).IsEqualTo((int)TicketCatalogStatusEnum.Draft);
        await Assert.That(clonedTicketType.Id).IsNotEqualTo(ticketType.Id);
        await Assert.That(clonedTicketType.Entitlements.Single().Id).IsNotEqualTo(ticketType.Entitlements.Single().Id);
        await Assert.That(clonedTicketType.FixedPriceMinor).IsEqualTo(1_235);
        await Assert.That(ticketType.TicketPricingModeId).IsEqualTo((int)TicketPricingModeEnum.Free);
    }

    [Test]
    public async Task UpdateAndDeleteTicketType_RequireDraftAndReplaceAuthoringFields()
    {
        EventTicketCatalogVersion catalog = CreateCatalog();
        EventTicketType ticketType = AddFreeTicketWithEventEntitlement(catalog);

        catalog.UpdateTicketType(ticketType, "Student", TicketPricingModeEnum.Fixed, 2_500, null, null,
            ParticipantDataCollectionModeEnum.PerTicketRequired, null, 12, 18, true, true, 2, 3, 4, 5, ticketType.Entitlements);

        await Assert.That(ticketType.Name).IsEqualTo("Student");
        await Assert.That(ticketType.FixedPriceMinor).IsEqualTo(2_500);
        await Assert.That(ticketType.MinimumAge).IsEqualTo(12);
        await Assert.That(ticketType.RequiresApproval).IsTrue();
        await Assert.That(ticketType.PerBookingPartyLimit).IsEqualTo(5);

        catalog.Publish();
        await Assert.That(() => catalog.DeleteTicketType(ticketType)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AddTicketType_WhenCurrencyDiffersFromCatalog_RejectsIt()
    {
        EventTicketCatalogVersion catalog = CreateCatalog();
        EventTicketType euroTicket = CreateTicket(catalog, "EUR", TicketPricingModeEnum.Free, null, null, null);

        await Assert.That(() => catalog.AddTicketType(euroTicket, null)).Throws<ArgumentException>();
    }

    [Test]
    public async Task XxxCurrency_AllowsOnlyFreeTicketCatalogs()
    {
        EventTicketCatalogVersion freeCatalog = CreateCatalog("XXX");
        EventTicketType freeTicket = CreateTicket(freeCatalog, "XXX", TicketPricingModeEnum.Free, null, null, null);
        freeCatalog.AddTicketType(freeTicket, null);
        freeCatalog.AddEntitlement(freeTicket, CreateEventEntitlement(freeCatalog, freeTicket));

        freeCatalog.Publish();

        EventTicketCatalogVersion pricedCatalog = CreateCatalog("XXX");
        await Assert.That(() => CreateTicket(pricedCatalog, "XXX", TicketPricingModeEnum.Donation, null, 0, null))
            .Throws<ArgumentException>();
        await Assert.That(freeCatalog.TicketCatalogStatusId).IsEqualTo((int)TicketCatalogStatusEnum.Published);
    }

    [Test]
    public async Task AddTicketType_AllowsSharedEventPoolButRejectsCrossEventPool()
    {
        EventTicketCatalogVersion catalog = CreateCatalog();
        EventCapacityPool sharedPool = EventCapacityPool.Create(
            catalog.TenantId,
            catalog.EventId,
            "Main hall",
            200,
            900,
            CapacityOversellPolicyEnum.Disallow,
            true);
        EventTicketType adultTicket = CreateTicket(catalog, "USD", TicketPricingModeEnum.Free, null, null, null);
        EventTicketType childTicket = CreateTicket(catalog, "USD", TicketPricingModeEnum.Free, null, null, null);

        catalog.AddTicketType(adultTicket, sharedPool);
        catalog.AddTicketType(childTicket, sharedPool);

        EventCapacityPool otherEventPool = EventCapacityPool.Create(
            catalog.TenantId,
            Guid.CreateVersion7(),
            "Other hall",
            100,
            900,
            CapacityOversellPolicyEnum.Disallow,
            true);
        EventTicketType otherTicket = CreateTicket(catalog, "USD", TicketPricingModeEnum.Free, null, null, null);

        await Assert.That(adultTicket.CapacityPoolId).IsEqualTo(sharedPool.Id);
        await Assert.That(childTicket.CapacityPoolId).IsEqualTo(sharedPool.Id);
        await Assert.That(() => catalog.AddTicketType(otherTicket, otherEventPool)).Throws<ArgumentException>();
    }

    [Test]
    public async Task AddEntitlement_RejectsCrossEventTargetsAndInvalidSelectionRules()
    {
        EventTicketCatalogVersion catalog = CreateCatalog();
        EventTicketType ticketType = CreateTicket(catalog, "USD", TicketPricingModeEnum.Free, null, null, null);
        catalog.AddTicketType(ticketType, null);

        TicketTypeEntitlement crossEventEntitlement = TicketTypeEntitlement.CreateForEvent(
            ticketType.Id,
            catalog.TenantId,
            Guid.CreateVersion7(),
            1);
        EventSession eventSession = CreateSession(catalog.EventId, catalog.TenantId);

        await Assert.That(() => catalog.AddEntitlement(ticketType, crossEventEntitlement)).Throws<ArgumentException>();
        await Assert.That(() => TicketTypeEntitlement.CreateForEventSession(ticketType.Id, eventSession, 2, EntitlementSelectionRuleEnum.ChooseOne))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task EntitlementFactories_KeepDayAndSessionTargetsInTheCatalogEvent()
    {
        EventTicketCatalogVersion catalog = CreateCatalog();
        EventTicketType ticketType = CreateTicket(catalog, "USD", TicketPricingModeEnum.Free, null, null, null);
        catalog.AddTicketType(ticketType, null);

        TicketTypeEntitlement eventDayEntitlement = TicketTypeEntitlement.CreateForEventDay(
            ticketType.Id,
            CreateDay(catalog.EventId, catalog.TenantId),
            1,
            EntitlementSelectionRuleEnum.FixedSelection);
        TicketTypeEntitlement eventSessionEntitlement = TicketTypeEntitlement.CreateForEventSession(
            ticketType.Id,
            CreateSession(catalog.EventId, catalog.TenantId),
            2,
            EntitlementSelectionRuleEnum.ChooseUpToN);
        TicketTypeEntitlement foreignSessionEntitlement = TicketTypeEntitlement.CreateForEventSession(
            ticketType.Id,
            CreateSession(Guid.CreateVersion7(), catalog.TenantId),
            1,
            EntitlementSelectionRuleEnum.FixedSelection);

        catalog.AddEntitlement(ticketType, eventDayEntitlement);
        catalog.AddEntitlement(ticketType, eventSessionEntitlement);

        await Assert.That(ticketType.Entitlements.Count).IsEqualTo(2);
        await Assert.That(() => catalog.AddEntitlement(ticketType, foreignSessionEntitlement)).Throws<ArgumentException>();
        await Assert.That(() => TicketTypeEntitlement.CreateForEvent(ticketType.Id, catalog.TenantId, catalog.EventId, 0))
            .Throws<ArgumentOutOfRangeException>();
    }

    private static EventTicketCatalogVersion CreateCatalog(string currencyCode = "USD") => EventTicketCatalogVersion.Create(
        Guid.CreateVersion7(),
        Guid.CreateVersion7(),
        currencyCode,
        1);

    private static EventTicketType AddFreeTicketWithEventEntitlement(EventTicketCatalogVersion catalog)
    {
        EventTicketType ticketType = CreateTicket(catalog, "USD", TicketPricingModeEnum.Free, null, null, null);
        catalog.AddTicketType(ticketType, null);
        catalog.AddEntitlement(ticketType, CreateEventEntitlement(catalog, ticketType));
        return ticketType;
    }

    private static EventTicketType CreateTicket(
        EventTicketCatalogVersion catalog,
        string currencyCode,
        TicketPricingModeEnum pricingMode,
        long? fixedPriceMinor,
        long? minimumPriceMinor,
        long? suggestedPriceMinor) => EventTicketType.Create(
        catalog.TenantId,
        catalog.Id,
        "General admission",
        currencyCode,
        pricingMode,
        fixedPriceMinor,
        minimumPriceMinor,
        suggestedPriceMinor,
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

    private static TicketTypeEntitlement CreateEventEntitlement(EventTicketCatalogVersion catalog, EventTicketType ticketType) => TicketTypeEntitlement.CreateForEvent(
        ticketType.Id,
        catalog.TenantId,
        catalog.EventId,
        1);

    private static EventSession CreateSession(Guid eventId, Guid tenantId) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        TenantId = tenantId,
        Event = null!,
        Tenant = null!
    };

    private static EventDay CreateDay(Guid eventId, Guid tenantId) => new()
    {
        Id = Guid.CreateVersion7(),
        EventId = eventId,
        TenantId = tenantId,
        Event = null!,
        Tenant = null!
    };
}
