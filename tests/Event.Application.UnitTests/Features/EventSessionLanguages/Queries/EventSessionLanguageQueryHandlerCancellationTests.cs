// ABOUTME: Unit tests for event-session language query cancellation propagation.
// ABOUTME: Proves handlers pass MediatR cancellation tokens into repository reads.

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

        _repository.GetBySession(request.EventSessionId, cancellation.Token).Returns(languages);
        _mapper.Map<List<EventSessionLanguageListDto>>(languages).Returns([]);

        await handler.Handle(request, cancellation.Token);

        await _repository.Received(1).GetBySession(request.EventSessionId, cancellation.Token);
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
