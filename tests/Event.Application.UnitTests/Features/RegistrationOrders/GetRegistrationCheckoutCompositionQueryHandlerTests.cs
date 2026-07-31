// ABOUTME: Tests public registration checkout composition and server-authored sliding-scale amounts.
// ABOUTME: Verifies public eligibility and organizer earnings remain Application-owned.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.RegistrationOrders.Handlers.Queries;
using Explore.Application.Features.RegistrationOrders.Requests.Queries;
using Explore.Application.Services.Registration;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.RegistrationOrders;

public sealed class GetRegistrationCheckoutCompositionQueryHandlerTests
{
    [Test]
    public async Task Handle_SlidingScaleCatalog_ReturnsServerComputedLinkedOptions()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var events = Substitute.For<IEventRepository>();
        var catalogs = Substitute.For<IEventTicketCatalogRepository>();
        var feePolicies = Substitute.For<IPlatformFeePolicyRepository>();
        var eventTarget = new DomainEvent
        {
            Id = eventId,
            TenantId = tenantId,
            EventStatusId = (int)EventStatusEnum.Published,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            Title = "Community event",
            Actor = null!,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            ParticipationConfiguration = EventParticipationConfiguration.Create(
                eventId,
                tenantId,
                (int)ParticipationHandlingModeEnum.PlatformManaged,
                (int)AdvanceRegistrationObligationEnum.Required,
                (int)IdentityAccessModeEnum.CapabilityTokenAllowed,
                GuestRecoveryPolicyEnum.CapabilityLinkOnly,
                DateTime.UtcNow)
        };
        var catalog = EventTicketCatalogVersion.Create(tenantId, eventId, "EUR", 1);
        var ticket = EventTicketType.Create(
            Guid.CreateVersion7(), tenantId, catalog.Id, "Community rate", "EUR",
            TicketPricingModeEnum.SlidingScale, null, 500, 1000,
            ParticipantDataCollectionModeEnum.None, null, null, null, false, false,
            5, null, null, null);
        catalog.AddTicketType(ticket, null);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, tenantId, eventId, 1));
        catalog.Publish();
        events.GetById(eventId).Returns(eventTarget);
        events.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>()).Returns(true);
        catalogs.GetPublishedCatalogAsync(eventId, tenantId, Arg.Any<CancellationToken>()).Returns(catalog);
        feePolicies.GetActiveAsync(Arg.Any<CancellationToken>()).Returns((PlatformFeePolicy?)null);
        var handler = new GetRegistrationCheckoutCompositionQueryHandler(
            events,
            catalogs,
            feePolicies,
            new OrganizerEarningsCalculator());

        var result = await handler.Handle(new GetRegistrationCheckoutCompositionQuery(eventId), CancellationToken.None);
        var options = result!.TicketTypes.Single().SlidingScaleOptions;

        await Assert.That(options.Count).IsEqualTo(5);
        await Assert.That(options.First().BuyerPriceMinor).IsEqualTo(500);
        await Assert.That(options.Last().BuyerPriceMinor).IsEqualTo(1000);
        await Assert.That(options.All(option => option.OrganizerEarningsMinor == option.BuyerPriceMinor)).IsTrue();
    }
}
