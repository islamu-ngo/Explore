// ABOUTME: Unit tests for event-session agenda item query cancellation propagation.
// ABOUTME: Proves handlers pass MediatR cancellation tokens into repository reads.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.Features.EventSessionAgendaItems.Handlers.Queries;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Queries;
using Explore.Domain;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionAgendaItems.Queries;

public sealed class EventSessionAgendaItemQueryHandlerCancellationTests
{
    private readonly IEventSessionAgendaItemRepository _repository = Substitute.For<IEventSessionAgendaItemRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();

    [Test]
    public async Task GetAgendaItemsBySession_ForwardsCancellationToken()
    {
        var agendaItems = new List<EventSessionAgendaItem>();
        var handler = new GetAgendaItemsBySessionRequestHandler(_repository, _mapper);
        var request = new GetAgendaItemsBySessionRequest { EventSessionId = Guid.NewGuid() };
        using var cancellation = new CancellationTokenSource();

        _repository.GetBySession(request.EventSessionId, cancellation.Token).Returns(agendaItems);
        _mapper.Map<List<EventSessionAgendaItemListDto>>(agendaItems).Returns([]);

        await handler.Handle(request, cancellation.Token);

        await _repository.Received(1).GetBySession(request.EventSessionId, cancellation.Token);
    }

    [Test]
    public async Task GetEventSessionAgendaItemList_ForwardsCancellationToken()
    {
        var agendaItems = new List<EventSessionAgendaItem>();
        var handler = new GetEventSessionAgendaItemListRequestHandler(_repository, _mapper);
        var request = new GetEventSessionAgendaItemListRequest { PageNumber = 2, PageSize = 10 };
        using var cancellation = new CancellationTokenSource();

        _repository.GetAgendaItemsWithDetailsPaged(2, 10, cancellation.Token).Returns((agendaItems, 0));
        _mapper.Map<List<EventSessionAgendaItemListDto>>(agendaItems).Returns([]);

        await handler.Handle(request, cancellation.Token);

        await _repository.Received(1).GetAgendaItemsWithDetailsPaged(2, 10, cancellation.Token);
    }

    [Test]
    public async Task GetEventSessionAgendaItemDetails_ForwardsCancellationToken()
    {
        var handler = new GetEventSessionAgendaItemDetailsRequestHandler(_repository, _mapper);
        var request = new GetEventSessionAgendaItemDetailsRequest { Id = Guid.NewGuid() };
        using var cancellation = new CancellationTokenSource();

        _repository.GetByIdWithDetails(request.Id, cancellation.Token).Returns((EventSessionAgendaItem?)null);

        await handler.Handle(request, cancellation.Token);

        await _repository.Received(1).GetByIdWithDetails(request.Id, cancellation.Token);
    }
}
