// ABOUTME: Tests the ticket catalog management query handler's event authority and read mapping.
// ABOUTME: Proves empty management resources, draft preference, and published-only read state.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventTicketing.Handlers.Queries;
using Explore.Application.Features.EventTicketing.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;
using DomainEvent = Explore.Domain.Event;

namespace Event.Application.UnitTests.Features.EventTicketing;

[Category("Phase43Ticketing")]
public sealed class GetEventTicketCatalogManagementQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private readonly Guid _eventId = Guid.CreateVersion7();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventTicketCatalogRepository _catalogs = Substitute.For<IEventTicketCatalogRepository>();
    private readonly ITenantContext _tenant = Substitute.For<ITenantContext>();

    public GetEventTicketCatalogManagementQueryHandlerTests()
    {
        _tenant.TenantId.Returns(_tenantId);
    }

    [Test]
    public async Task Handle_WhenEventIsMissing_ReturnsNull()
    {
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>()).Returns((DomainEvent?)null);

        var result = await CreateHandler().Handle(new GetEventTicketCatalogManagementQuery(_eventId), CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Handle_WhenEventIsCrossTenant_ReturnsNull()
    {
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>())
            .Returns(CreatePlatformEvent(Guid.CreateVersion7(), _eventId));

        var result = await CreateHandler().Handle(new GetEventTicketCatalogManagementQuery(_eventId), CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Handle_WhenEventIsNotPlatformManaged_ReturnsNull()
    {
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>())
            .Returns(CreateEvent(_tenantId, _eventId, ParticipationHandlingModeEnum.ExternalManaged));

        var result = await CreateHandler().Handle(new GetEventTicketCatalogManagementQuery(_eventId), CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Handle_WhenPlatformManagedEventHasNoCatalog_ReturnsEmptyManagementDto()
    {
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>())
            .Returns(CreatePlatformEvent(_tenantId, _eventId));
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns((EventTicketCatalogVersion?)null);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns((EventTicketCatalogVersion?)null);

        var result = await CreateHandler().Handle(new GetEventTicketCatalogManagementQuery(_eventId), CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.EventId).IsEqualTo(_eventId);
        await Assert.That(result.CatalogId).IsNull();
        await Assert.That(result.VersionNumber).IsNull();
        await Assert.That(result.StatusId).IsNull();
        await Assert.That(result.TicketTypes.Count).IsEqualTo(0);
        await Assert.That(result.CapacityPools.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_WhenDraftExists_PrefersDraftAndMapsIt()
    {
        EventTicketCatalogVersion draft = EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 2);
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>())
            .Returns(CreatePlatformEvent(_tenantId, _eventId));
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(draft);

        var result = await CreateHandler().Handle(new GetEventTicketCatalogManagementQuery(_eventId), CancellationToken.None);

        await Assert.That(result!.CatalogId).IsEqualTo(draft.Id);
        await Assert.That(result.VersionNumber).IsEqualTo(2);
        await Assert.That(result.StatusId).IsEqualTo((int)TicketCatalogStatusEnum.Draft);
        await _catalogs.DidNotReceive().GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenOnlyPublishedCatalogExists_ReturnsPublishedReadState()
    {
        EventTicketCatalogVersion published = CreatePublishedCatalog();
        _events.GetAuthorizationTargetByIdAsync(_eventId, Arg.Any<CancellationToken>())
            .Returns(CreatePlatformEvent(_tenantId, _eventId));
        _catalogs.GetManagementCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>())
            .Returns((EventTicketCatalogVersion?)null);
        _catalogs.GetPublishedCatalogAsync(_eventId, _tenantId, Arg.Any<CancellationToken>()).Returns(published);

        var result = await CreateHandler().Handle(new GetEventTicketCatalogManagementQuery(_eventId), CancellationToken.None);

        await Assert.That(result!.CatalogId).IsEqualTo(published.Id);
        await Assert.That(result.StatusId).IsEqualTo((int)TicketCatalogStatusEnum.Published);
    }

    private GetEventTicketCatalogManagementQueryHandler CreateHandler() => new(_events, _catalogs, _tenant);

    private EventTicketCatalogVersion CreatePublishedCatalog()
    {
        EventTicketCatalogVersion catalog = EventTicketCatalogVersion.Create(_tenantId, _eventId, "USD", 1);
        EventTicketType ticket = EventTicketType.Create(
            _tenantId,
            catalog.Id,
            "General admission",
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
        catalog.AddTicketType(ticket, null);
        catalog.AddEntitlement(ticket, TicketTypeEntitlement.CreateForEvent(ticket.Id, _tenantId, _eventId, 1));
        catalog.Publish();
        return catalog;
    }

    private DomainEvent CreatePlatformEvent(Guid tenantId, Guid eventId) =>
        CreateEvent(tenantId, eventId, ParticipationHandlingModeEnum.PlatformManaged);

    private static DomainEvent CreateEvent(Guid tenantId, Guid eventId, ParticipationHandlingModeEnum mode) => new()
    {
        Id = eventId,
        TenantId = tenantId,
        Title = "Ticketing event",
        Actor = null!,
        Tenant = null!,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!,
        ParticipationConfiguration = EventParticipationConfiguration.Create(
            eventId,
            tenantId,
            (int)mode,
            (int)AdvanceRegistrationObligationEnum.Required,
            mode == ParticipationHandlingModeEnum.PlatformManaged
                ? (int)IdentityAccessModeEnum.AccountRequired
                : null,
            guestRecoveryPolicy: null,
            DateTime.UtcNow)
    };
}
