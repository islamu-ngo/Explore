// ABOUTME: Unit tests for public event-session agenda item query privacy and cancellation propagation.
// ABOUTME: Proves handlers strip physical venue data and pass cancellation tokens into repository reads.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
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
    private readonly IEventLocationDisclosureService _disclosureService = Substitute.For<IEventLocationDisclosureService>();

    [Test]
    public async Task GetAgendaItemsBySession_ForwardsCancellationToken()
    {
        var agendaItems = new List<EventSessionAgendaItem>();
        var handler = new GetAgendaItemsBySessionRequestHandler(_repository, _mapper, _disclosureService);
        var request = new GetAgendaItemsBySessionRequest { EventSessionId = Guid.NewGuid() };
        using var cancellation = new CancellationTokenSource();

        _repository.GetPublicBySessionAsync(request.EventSessionId, cancellation.Token).Returns(agendaItems);
        _mapper.Map<List<EventSessionAgendaItemListDto>>(agendaItems).Returns([]);

        await handler.Handle(request, cancellation.Token);

        await _repository.Received(1).GetPublicBySessionAsync(request.EventSessionId, cancellation.Token);
    }

    [Test]
    public async Task GetEventSessionAgendaItemList_ForwardsCancellationToken()
    {
        var agendaItems = new List<EventSessionAgendaItem>();
        var handler = new GetEventSessionAgendaItemListRequestHandler(_repository, _mapper, _disclosureService);
        var request = new GetEventSessionAgendaItemListRequest { PageNumber = 2, PageSize = 10 };
        using var cancellation = new CancellationTokenSource();

        _repository.GetPublicAgendaItemsWithDetailsPagedAsync(2, 10, cancellation.Token).Returns((agendaItems, 0));
        _mapper.Map<List<EventSessionAgendaItemListDto>>(agendaItems).Returns([]);

        await handler.Handle(request, cancellation.Token);

        await _repository.Received(1).GetPublicAgendaItemsWithDetailsPagedAsync(2, 10, cancellation.Token);
    }

    [Test]
    public async Task GetEventSessionAgendaItemDetails_ForwardsCancellationToken()
    {
        var handler = new GetEventSessionAgendaItemDetailsRequestHandler(_repository, _mapper, _disclosureService);
        var request = new GetEventSessionAgendaItemDetailsRequest { Id = Guid.NewGuid() };
        using var cancellation = new CancellationTokenSource();

        _repository.GetPublicByIdWithDetailsAsync(request.Id, cancellation.Token).Returns((EventSessionAgendaItem?)null);

        await handler.Handle(request, cancellation.Token);

        await _repository.Received(1).GetPublicByIdWithDetailsAsync(request.Id, cancellation.Token);
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task GetAgendaItemsBySession_RedactsPhysicalLocation()
    {
        var entity = CreateAgendaItem();
        var agendaItems = new List<EventSessionAgendaItem> { entity };
        var mapped = new List<EventSessionAgendaItemListDto>
        {
            new() { Id = entity.Id, Title = "Public agenda", LocationFullName = "Private venue" }
        };
        var handler = new GetAgendaItemsBySessionRequestHandler(_repository, _mapper, _disclosureService);
        var request = new GetAgendaItemsBySessionRequest { EventSessionId = Guid.NewGuid() };
        _repository.GetPublicBySessionAsync(request.EventSessionId, Arg.Any<CancellationToken>()).Returns(agendaItems);
        _mapper.Map<List<EventSessionAgendaItemListDto>>(agendaItems).Returns(mapped);

        var result = await handler.Handle(request, CancellationToken.None);

        await Assert.That(result.Single().LocationFullName).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task GetEventSessionAgendaItemList_RedactsPhysicalLocation()
    {
        var entity = CreateAgendaItem();
        var agendaItems = new List<EventSessionAgendaItem> { entity };
        var mapped = new List<EventSessionAgendaItemListDto>
        {
            new() { Id = entity.Id, Title = "Public agenda", LocationFullName = "Private venue" }
        };
        var handler = new GetEventSessionAgendaItemListRequestHandler(_repository, _mapper, _disclosureService);
        var request = new GetEventSessionAgendaItemListRequest { PageNumber = 1, PageSize = 20 };
        _repository.GetPublicAgendaItemsWithDetailsPagedAsync(1, 20, Arg.Any<CancellationToken>()).Returns((agendaItems, 1));
        _mapper.Map<List<EventSessionAgendaItemListDto>>(agendaItems).Returns(mapped);

        var result = await handler.Handle(request, CancellationToken.None);

        await Assert.That(result.Items.Single().LocationFullName).IsNull();
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task GetEventSessionAgendaItemDetails_RedactsPhysicalLocation()
    {
        var entity = CreateAgendaItem();
        var mapped = new EventSessionAgendaItemDto
        {
            Title = "Public agenda",
            LocationId = Guid.NewGuid(),
            LocationFullName = "Private venue"
        };
        var handler = new GetEventSessionAgendaItemDetailsRequestHandler(_repository, _mapper, _disclosureService);
        var request = new GetEventSessionAgendaItemDetailsRequest { Id = Guid.NewGuid() };
        _repository.GetPublicByIdWithDetailsAsync(request.Id, Arg.Any<CancellationToken>()).Returns(entity);
        _mapper.Map<EventSessionAgendaItemDto>(entity).Returns(mapped);

        var result = await handler.Handle(request, CancellationToken.None);

        await Assert.That(result!.LocationId).IsNull();
        await Assert.That(result.LocationFullName).IsNull();
    }

    private static EventSessionAgendaItem CreateAgendaItem() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Public agenda",
        EventSession = new EventSession
        {
            EventId = Guid.NewGuid(),
            Event = null!,
            Tenant = null!
        },
        Tenant = null!
    };
}
