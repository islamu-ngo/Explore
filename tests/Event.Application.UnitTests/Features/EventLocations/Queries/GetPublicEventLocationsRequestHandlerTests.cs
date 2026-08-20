// ABOUTME: Unit tests for the public event location disclosure gate.
// ABOUTME: Verifies central event eligibility blocks location loading and disclosure for ineligible public events.

using Explore.Application.Contracts.LocationPrivacy;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Location;
using Explore.Application.Features.EventLocations.Handlers.Queries;
using Explore.Application.Features.EventLocations.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventLocations.Queries;

public sealed class GetPublicEventLocationsRequestHandlerTests
{
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEventLocationRepository _eventLocations = Substitute.For<IEventLocationRepository>();
    private readonly RecordingDisclosureService _disclosureService = new();
    private readonly GetPublicEventLocationsRequestHandler _handler;

    public GetPublicEventLocationsRequestHandlerTests()
    {
        _handler = new GetPublicEventLocationsRequestHandler(_events, _eventLocations, _disclosureService);
    }

    [Test]
    public async Task Handle_WithEligiblePublicEvent_ReturnsDisclosedLocations()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var request = new GetPublicEventLocationsRequest(eventId);
        Explore.Domain.Event @event = CreatePublicEvent(tenantId, eventId);
        EventLocation placement = EventLocation.CreatePhysical(
            tenantId,
            eventId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);

        _events.GetById(eventId).Returns(@event);
        _events.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>()).Returns(true);
        _eventLocations.GetByEventIdAsync(eventId, Arg.Any<CancellationToken>()).Returns([placement]);

        IReadOnlyList<EventLocationPublicDto>? result = await _handler.Handle(request, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!).Count().IsEqualTo(1);
        await _events.Received(1).IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>());
        await _eventLocations.Received(1).GetByEventIdAsync(eventId, Arg.Any<CancellationToken>());
        await Assert.That(_disclosureService.Calls).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Handle_WithIneligiblePublicEvent_ReturnsNullWithoutLoadingLocations()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var request = new GetPublicEventLocationsRequest(eventId);
        Explore.Domain.Event @event = CreatePublicEvent(tenantId, eventId);

        _events.GetById(eventId).Returns(@event);
        _events.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>()).Returns(false);

        IReadOnlyList<EventLocationPublicDto>? result = await _handler.Handle(request, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _events.Received(1).IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>());
        await _eventLocations.DidNotReceive().GetByEventIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await Assert.That(_disclosureService.Calls).IsEmpty();
    }

    private static Explore.Domain.Event CreatePublicEvent(Guid tenantId, Guid eventId)
    {
        return new Explore.Domain.Event(EventStatusEnum.Published)
        {
            Id = eventId,
            TenantId = tenantId,
            Tenant = new Tenant
            {
                Id = tenantId,
                FullName = "Tenant",
                Slug = "tenant",
                TenantStatus = new TenantStatus
                {
                    Id = 1,
                    MasterCode = "ACTIVE",
                    FullName = "Active"
                }
            },
            Title = "Public event",
            Actor = new Actor
            {
                Id = Guid.NewGuid(),
                ActorType = new ActorType
                {
                    Id = 1,
                    FullName = "Organizer",
                    MasterCode = "ORGANIZER"
                },
                Pii = new ActorPii
                {
                    DisplayName = "Organizer"
                }
            },
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = new VisibilityType
            {
                Id = (int)VisibilityTypeEnum.Public,
                FullName = "Public",
                MasterCode = "PUBLIC"
            },
            EventStatus = new EventStatus
            {
                Id = (int)EventStatusEnum.Published,
                FullName = "Published",
                MasterCode = "PUBLISHED"
            },
            EventFormat = new EventFormat
            {
                Id = 1,
                FullName = "In person",
                MasterCode = "IN_PERSON"
            }
        };
    }

    private sealed class RecordingDisclosureService : IEventLocationDisclosureService
    {
        public List<IReadOnlyCollection<EventLocationDisclosureRequest>> Calls { get; } = [];

        public Task<IReadOnlyDictionary<Guid, EventLocationDisclosureResult>> ResolveManyAsync(
            IReadOnlyCollection<EventLocationDisclosureRequest> requests,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add(requests);

            IReadOnlyDictionary<Guid, EventLocationDisclosureResult> result = requests.ToDictionary(
                request => request.EventLocationId,
                request => EventLocationDisclosureResult.Public(
                    request.EventLocationId,
                    EventLocationDisclosureState.Available,
                    new EventLocationDisclosureValues(City: "Brussels", VenueName: "Venue")));

            return Task.FromResult(result);
        }
    }
}
