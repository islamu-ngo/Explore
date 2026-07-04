// ABOUTME: Unit tests for event-session speaker query cancellation propagation.
// ABOUTME: Proves handlers pass MediatR cancellation tokens into repository reads.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.Features.EventSessionSpeakers.Handlers.Queries;
using Explore.Application.Features.EventSessionSpeakers.Requests.Queries;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionSpeakers.Queries;

public sealed class EventSessionSpeakerQueryHandlerCancellationTests
{
    private readonly IEventSessionSpeakerRepository _repository = Substitute.For<IEventSessionSpeakerRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    [Test]
    public async Task GetSpeakersBySession_ForwardsCancellationToken()
    {
        var speakers = new List<EventSessionSpeaker>();
        var handler = new GetSpeakersBySessionRequestHandler(_repository, _mapper);
        var request = new GetSpeakersBySessionRequest { EventSessionId = Guid.NewGuid() };
        using var cancellation = new CancellationTokenSource();

        _repository.GetBySession(request.EventSessionId, cancellation.Token).Returns(speakers);
        _mapper.Map<List<EventSessionSpeakerListDto>>(speakers).Returns([]);

        await handler.Handle(request, cancellation.Token);

        await _repository.Received(1).GetBySession(request.EventSessionId, cancellation.Token);
    }

    [Test]
    public async Task GetSessionsByActor_ForwardsCancellationToken()
    {
        var speakers = new List<EventSessionSpeaker>();
        var handler = new GetSessionsByActorRequestHandler(_repository, _mapper);
        var request = new GetSessionsByActorRequest { ActorId = Guid.NewGuid() };
        using var cancellation = new CancellationTokenSource();

        _repository.GetByActor(request.ActorId, cancellation.Token).Returns(speakers);
        _mapper.Map<List<EventSessionSpeakerListDto>>(speakers).Returns([]);

        await handler.Handle(request, cancellation.Token);

        await _repository.Received(1).GetByActor(request.ActorId, cancellation.Token);
    }

    [Test]
    public async Task GetEventSessionSpeakerList_ForwardsCancellationToken()
    {
        var speakers = new List<EventSessionSpeaker>();
        var handler = new GetEventSessionSpeakerListRequestHandler(_repository, _mapper);
        var request = new GetEventSessionSpeakerListRequest { PageNumber = 2, PageSize = 10 };
        using var cancellation = new CancellationTokenSource();

        _repository.GetSpeakersWithDetailsPaged(2, 10, cancellation.Token).Returns((speakers, 0));
        _mapper.Map<List<EventSessionSpeakerListDto>>(speakers).Returns([]);

        await handler.Handle(request, cancellation.Token);

        await _repository.Received(1).GetSpeakersWithDetailsPaged(2, 10, cancellation.Token);
    }
}
