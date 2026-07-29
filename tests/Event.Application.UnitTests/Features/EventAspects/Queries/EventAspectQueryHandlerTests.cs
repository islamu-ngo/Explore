// ABOUTME: Unit tests for public Event aspect query handlers.
// ABOUTME: Verifies centrally ineligible parent Events fail closed before aspect reads.

using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.Features.EventAspects.Handlers.Queries;
using Explore.Application.Features.EventAspects.Requests.Queries;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventAspects.Queries;

public sealed class EventAspectQueryHandlerTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventIslamicAspectRepository _islamicAspectRepository = Substitute.For<IEventIslamicAspectRepository>();
    private readonly IEventTechAspectRepository _techAspectRepository = Substitute.For<IEventTechAspectRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    [Test]
    public async Task GetIslamicAspect_WhenParentEventIsNotCentrallyPubliclyEligible_ReturnsNullWithoutReadingAspect()
    {
        var eventId = Guid.NewGuid();
        var parentEvent = ConfigureParentEvent(eventId, eligible: false);

        var result = await new GetEventIslamicAspectRequestHandler(
                _eventRepository,
                _islamicAspectRepository,
                _mapper)
            .Handle(new GetEventIslamicAspectRequest { EventId = eventId }, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _eventRepository.Received(1).IsPubliclyEligibleAsync(parentEvent.TenantId, eventId, Arg.Any<CancellationToken>());
        _islamicAspectRepository.DidNotReceive().GetByEventIdWithDetails(Arg.Any<Guid>());
    }

    [Test]
    public async Task GetTechAspect_WhenParentEventIsNotCentrallyPubliclyEligible_ReturnsNullWithoutReadingAspect()
    {
        var eventId = Guid.NewGuid();
        var parentEvent = ConfigureParentEvent(eventId, eligible: false);

        var result = await new GetEventTechAspectRequestHandler(
                _eventRepository,
                _techAspectRepository,
                _mapper)
            .Handle(new GetEventTechAspectRequest { EventId = eventId }, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _eventRepository.Received(1).IsPubliclyEligibleAsync(parentEvent.TenantId, eventId, Arg.Any<CancellationToken>());
        _techAspectRepository.DidNotReceive().GetByEventId(Arg.Any<Guid>());
    }

    [Test]
    public async Task GetIslamicAspect_WhenParentEventIsEligible_MapsAspect()
    {
        var eventId = Guid.NewGuid();
        ConfigureParentEvent(eventId, eligible: true);
        var aspect = new EventIslamicAspect { Id = eventId };
        var dto = new EventIslamicAspectDto();
        _islamicAspectRepository.GetByEventIdWithDetails(eventId).Returns(aspect);
        _mapper.Map<EventIslamicAspectDto>(aspect).Returns(dto);

        var result = await new GetEventIslamicAspectRequestHandler(
                _eventRepository,
                _islamicAspectRepository,
                _mapper)
            .Handle(new GetEventIslamicAspectRequest { EventId = eventId }, CancellationToken.None);

        await Assert.That(result).IsEqualTo(dto);
    }

    [Test]
    public async Task GetTechAspect_WhenParentEventIsEligible_MapsAspect()
    {
        var eventId = Guid.NewGuid();
        ConfigureParentEvent(eventId, eligible: true);
        var aspect = new EventTechAspect { Id = eventId };
        var dto = new EventTechAspectDto();
        _techAspectRepository.GetByEventId(eventId).Returns(aspect);
        _mapper.Map<EventTechAspectDto>(aspect).Returns(dto);

        var result = await new GetEventTechAspectRequestHandler(
                _eventRepository,
                _techAspectRepository,
                _mapper)
            .Handle(new GetEventTechAspectRequest { EventId = eventId }, CancellationToken.None);

        await Assert.That(result).IsEqualTo(dto);
    }

    [Test]
    public async Task GetManagedIslamicAspect_ReturnsDraftAspectWithoutPublicEligibilityProbe()
    {
        var eventId = Guid.NewGuid();
        var aspect = new EventIslamicAspect { Id = eventId };
        var dto = new EventIslamicAspectDto();
        _islamicAspectRepository.GetByEventIdWithDetails(eventId).Returns(aspect);
        _mapper.Map<EventIslamicAspectDto>(aspect).Returns(dto);

        var result = await new GetManagedEventIslamicAspectRequestHandler(
                _islamicAspectRepository,
                _mapper)
            .Handle(new GetManagedEventIslamicAspectRequest { EventId = eventId }, CancellationToken.None);

        await Assert.That(result).IsEqualTo(dto);
        await _eventRepository.DidNotReceive().IsPubliclyEligibleAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetManagedTechAspect_ReturnsDraftAspectWithoutPublicEligibilityProbe()
    {
        var eventId = Guid.NewGuid();
        var aspect = new EventTechAspect { Id = eventId };
        var dto = new EventTechAspectDto();
        _techAspectRepository.GetByEventId(eventId).Returns(aspect);
        _mapper.Map<EventTechAspectDto>(aspect).Returns(dto);

        var result = await new GetManagedEventTechAspectRequestHandler(
                _techAspectRepository,
                _mapper)
            .Handle(new GetManagedEventTechAspectRequest { EventId = eventId }, CancellationToken.None);

        await Assert.That(result).IsEqualTo(dto);
        await _eventRepository.DidNotReceive().IsPubliclyEligibleAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    private Explore.Domain.Event ConfigureParentEvent(Guid eventId, bool eligible)
    {
        var parentEvent = DataBuilder.Event.Generate();
        parentEvent.Id = eventId;
        parentEvent.TenantId = Guid.NewGuid();
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _eventRepository.IsPubliclyEligibleAsync(parentEvent.TenantId, eventId, Arg.Any<CancellationToken>()).Returns(eligible);
        return parentEvent;
    }
}
