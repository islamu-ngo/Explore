// ABOUTME: Unit tests for public event-level agenda item detail queries.
// ABOUTME: Verifies public eligibility repository routing and missing-item behavior.

using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.Features.EventAgendaItems.Handlers.Queries;
using Explore.Application.Features.EventAgendaItems.Requests.Queries;
using Explore.Domain;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventAgendaItems.Queries;

public class GetEventAgendaItemDetailRequestHandlerTests
{
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IMapper _mapper;
    private readonly GetEventAgendaItemDetailRequestHandler _handler;

    public GetEventAgendaItemDetailRequestHandlerTests()
    {
        _eventAgendaItemRepository = Substitute.For<IEventAgendaItemRepository>();
        _mapper = Substitute.For<IMapper>();

        _handler = new GetEventAgendaItemDetailRequestHandler(_eventAgendaItemRepository, _mapper);
    }

    [Test]
    public async Task Handle_WithExistingAgendaItem_ReturnsDto()
    {
        // Arrange
        var agendaItemId = Guid.NewGuid();
        var request = new GetEventAgendaItemDetailRequest { Id = agendaItemId };

        var agendaItem = DataBuilder.EventAgendaItem.Generate();
        agendaItem.Id = agendaItemId;
        agendaItem.Title = "Opening Ceremony";

        var expectedDto = new EventAgendaItemDto
        {
            Id = agendaItemId,
            Title = "Opening Ceremony"
        };

        _eventAgendaItemRepository.GetPublicByIdAsync(agendaItemId, Arg.Any<CancellationToken>()).Returns(agendaItem);
        _mapper.Map<EventAgendaItemDto>(agendaItem).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(agendaItemId);
        await Assert.That(result.Title).IsEqualTo("Opening Ceremony");
    }

    [Test]
    public async Task Handle_WithNonExistentAgendaItem_ReturnsNull()
    {
        // Arrange
        var agendaItemId = Guid.NewGuid();
        var request = new GetEventAgendaItemDetailRequest { Id = agendaItemId };

        _eventAgendaItemRepository.GetPublicByIdAsync(agendaItemId, Arg.Any<CancellationToken>())
            .Returns((EventAgendaItem?)null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        await Assert.That(result).IsNull();
    }
}
