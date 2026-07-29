// ABOUTME: Unit tests for event-session language query cancellation propagation.
// ABOUTME: Proves handlers pass MediatR cancellation tokens into repository reads.

using Event.Application.UnitTests.Common;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Features.EventSessionLanguages.Handlers.Queries;
using Explore.Application.Features.EventSessionLanguages.Requests.Queries;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionLanguages.Queries;

public sealed class EventSessionLanguageQueryHandlerCancellationTests
{
    private readonly IEventSessionLanguageRepository _repository = Substitute.For<IEventSessionLanguageRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    [Test]
    public async Task GetLanguagesBySession_ForwardsCancellationToken()
    {
        var languages = new List<EventSessionLanguage>();
        var handler = new GetLanguagesBySessionRequestHandler(_repository, _eventSessionRepository, _mapper);
        var request = new GetLanguagesBySessionRequest { EventSessionId = Guid.NewGuid() };
        using var cancellation = new CancellationTokenSource();
        var eventSession = DataBuilder.EventSession.Generate();
        eventSession.Id = request.EventSessionId;

        _eventSessionRepository.GetPublicSessionWithDetailsAsync(request.EventSessionId, cancellation.Token).Returns(eventSession);
        _repository.GetBySession(request.EventSessionId, cancellation.Token).Returns(languages);
        _mapper.Map<List<EventSessionLanguageListDto>>(languages).Returns([]);

        await handler.Handle(request, cancellation.Token);

        await _eventSessionRepository.Received(1).GetPublicSessionWithDetailsAsync(request.EventSessionId, cancellation.Token);
        await _repository.Received(1).GetBySession(request.EventSessionId, cancellation.Token);
    }

    [Test]
    public async Task GetLanguagesBySession_WhenPublicSessionIsUnavailable_ReturnsEmptyWithoutReadingLanguages()
    {
        var handler = new GetLanguagesBySessionRequestHandler(_repository, _eventSessionRepository, _mapper);
        var request = new GetLanguagesBySessionRequest { EventSessionId = Guid.NewGuid() };
        using var cancellation = new CancellationTokenSource();

        _eventSessionRepository.GetPublicSessionWithDetailsAsync(request.EventSessionId, cancellation.Token)
            .Returns((EventSession?)null);

        var result = await handler.Handle(request, cancellation.Token);

        await TUnit.Assertions.Assert.That(result).IsEmpty();
        _repository.DidNotReceive().GetBySession(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _mapper.DidNotReceive().Map<List<EventSessionLanguageListDto>>(Arg.Any<List<EventSessionLanguage>>());
    }

    [Test]
    public async Task GetManagedLanguagesBySession_ReturnsDraftAssignmentsForMatchingEvent()
    {
        Guid eventId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        var eventSession = DataBuilder.EventSession.Generate();
        eventSession.Id = sessionId;
        eventSession.EventId = eventId;
        var languages = new List<EventSessionLanguage>();
        var expected = new List<EventSessionLanguageListDto>();
        _eventSessionRepository.GetSessionWithDetails(sessionId).Returns(eventSession);
        _repository.GetBySession(sessionId, Arg.Any<CancellationToken>()).Returns(languages);
        _mapper.Map<List<EventSessionLanguageListDto>>(languages).Returns(expected);

        var result = await new GetManagedLanguagesBySessionRequestHandler(
                _repository,
                _eventSessionRepository,
                _mapper)
            .Handle(new GetManagedLanguagesBySessionRequest
            {
                EventId = eventId,
                EventSessionId = sessionId
            }, CancellationToken.None);

        await Assert.That(result).IsEqualTo(expected);
        await _repository.Received(1).GetBySession(sessionId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetManagedLanguagesBySession_RejectsSessionFromAnotherEvent()
    {
        Guid sessionId = Guid.NewGuid();
        var eventSession = DataBuilder.EventSession.Generate();
        eventSession.Id = sessionId;
        eventSession.EventId = Guid.NewGuid();
        _eventSessionRepository.GetSessionWithDetails(sessionId).Returns(eventSession);

        var result = await new GetManagedLanguagesBySessionRequestHandler(
                _repository,
                _eventSessionRepository,
                _mapper)
            .Handle(new GetManagedLanguagesBySessionRequest
            {
                EventId = Guid.NewGuid(),
                EventSessionId = sessionId
            }, CancellationToken.None);

        await Assert.That(result).IsEmpty();
        _repository.DidNotReceive().GetBySession(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetEventSessionLanguageList_ForwardsCancellationToken()
    {
        var languages = new List<EventSessionLanguage>();
        var handler = new GetEventSessionLanguageListRequestHandler(_repository, _mapper);
        var request = new GetEventSessionLanguageListRequest { PageNumber = 2, PageSize = 10 };
        using var cancellation = new CancellationTokenSource();

        _repository.GetLanguagesWithDetailsPaged(2, 10, cancellation.Token).Returns((languages, 0));
        _mapper.Map<List<EventSessionLanguageListDto>>(languages).Returns([]);

        await handler.Handle(request, cancellation.Token);

        await _repository.Received(1).GetLanguagesWithDetailsPaged(2, 10, cancellation.Token);
    }
}
