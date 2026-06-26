// ABOUTME: Unit tests for the event-scoped registration list query handler.
// ABOUTME: Verifies pagination normalization and mapping stay in the Application layer.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Handlers.Queries;
using Explore.Application.Features.EventRegistrations.Requests.Queries;
using Explore.Domain;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventRegistrations.Queries;

public class GetEventRegistrationsByEventRequestHandlerTests
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository;
    private readonly IMapper _mapper;
    private readonly GetEventRegistrationsByEventRequestHandler _handler;

    public GetEventRegistrationsByEventRequestHandlerTests()
    {
        _eventRegistrationRepository = Substitute.For<IEventRegistrationRepository>();
        _mapper = Substitute.For<IMapper>();
        _handler = new GetEventRegistrationsByEventRequestHandler(_eventRegistrationRepository, _mapper);
    }

    [Test]
    public async Task Handle_ForwardsEventIdAndReturnsMappedPage()
    {
        var eventId = Guid.NewGuid();
        var registrations = new List<EventRegistration>
        {
            CreateRegistration(eventId),
            CreateRegistration(eventId)
        };
        var dtos = registrations
            .Select(registration => new EventRegistrationListDto { Id = registration.Id, EventId = eventId })
            .ToList();

        _eventRegistrationRepository.GetRegistrationsByEventWithDetailsPaged(
                eventId,
                2,
                7,
                Arg.Any<CancellationToken>())
            .Returns((registrations, 13));
        _mapper.Map<List<EventRegistrationListDto>>(registrations).Returns(dtos);

        var result = await _handler.Handle(
            new GetEventRegistrationsByEventRequest
            {
                EventId = eventId,
                PageNumber = 2,
                PageSize = 7
            },
            CancellationToken.None);

        await Assert.That(result.Items.Count).IsEqualTo(2);
        await Assert.That(result.TotalCount).IsEqualTo(13);
        await Assert.That(result.PageNumber).IsEqualTo(2);
        await Assert.That(result.PageSize).IsEqualTo(7);
        await _eventRegistrationRepository.Received(1).GetRegistrationsByEventWithDetailsPaged(
            eventId,
            2,
            7,
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_NormalizesInvalidPaginationBeforeRepositoryCall()
    {
        var eventId = Guid.NewGuid();
        _eventRegistrationRepository.GetRegistrationsByEventWithDetailsPaged(
                eventId,
                1,
                1,
                Arg.Any<CancellationToken>())
            .Returns((new List<EventRegistration>(), 0));
        _mapper.Map<List<EventRegistrationListDto>>(Arg.Any<List<EventRegistration>>())
            .Returns(new List<EventRegistrationListDto>());

        var result = await _handler.Handle(
            new GetEventRegistrationsByEventRequest
            {
                EventId = eventId,
                PageNumber = -10,
                PageSize = 0
            },
            CancellationToken.None);

        await Assert.That(result.PageNumber).IsEqualTo(1);
        await Assert.That(result.PageSize).IsEqualTo(1);
        await _eventRegistrationRepository.Received(1).GetRegistrationsByEventWithDetailsPaged(
            eventId,
            1,
            1,
            Arg.Any<CancellationToken>());
    }

    private static EventRegistration CreateRegistration(Guid eventId)
        => new()
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            User = null!,
            EventSession = null!,
            Tenant = null!
        };
}
