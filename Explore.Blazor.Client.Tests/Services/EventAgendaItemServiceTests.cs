// ABOUTME: Unit tests for EventAgendaItemService covering CRUD operations.
// ABOUTME: Tests GetAgendaItemsByEvent, GetAgendaItemById, Create, Update, Delete with success and error paths.

namespace Explore.Blazor.Client.Tests.Services;

public class EventAgendaItemServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<EventAgendaItemService> _logger;
    private readonly EventAgendaItemService _service;

    public EventAgendaItemServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _logger = Substitute.For<ILogger<EventAgendaItemService>>();
        _service = new EventAgendaItemService(_apiClient, _logger);
    }

    // ========== GetAgendaItemsByEventAsync ==========

    [Test]
    public async Task GetAgendaItemsByEventAsync_ReturnsItems_WhenApiSucceeds()
    {
        var eventId = Guid.NewGuid();
        var halResponse = CreateHalCollectionResponse(new List<EventAgendaItemListDto>
        {
            new() { Id = Guid.NewGuid(), Title = "Keynote", SortOrder = 1 },
            new() { Id = Guid.NewGuid(), Title = "Workshop", SortOrder = 2 }
        });

        _apiClient.GetEventAgendaItemsByEventAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        var result = await _service.GetAgendaItemsByEventAsync(eventId);

        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetAgendaItemsByEventAsync_ReturnsEmptyList_WhenApiThrows()
    {
        _apiClient.GetEventAgendaItemsByEventAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Network error"));

        var result = await _service.GetAgendaItemsByEventAsync(Guid.NewGuid());

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetAgendaItemsByEventAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        _apiClient.GetEventAgendaItemsByEventAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((HalCollectionResourceOfEventAgendaItemListDto?)null);

        var result = await _service.GetAgendaItemsByEventAsync(Guid.NewGuid());

        await Assert.That(result).IsEmpty();
    }

    // ========== GetAgendaItemByIdAsync ==========

    [Test]
    public async Task GetAgendaItemByIdAsync_ReturnsItem_WhenApiSucceeds()
    {
        var itemId = Guid.NewGuid();
        var dto = new EventAgendaItemDto
        {
            Id = itemId,
            Title = "Keynote Speech"
        };
        var halResponse = CreateHalResourceResponse(dto);

        _apiClient.GetEventAgendaItemByIdAsync(itemId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        var result = await _service.GetAgendaItemByIdAsync(itemId);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Title).IsEqualTo("Keynote Speech");
    }

    [Test]
    public async Task GetAgendaItemByIdAsync_ReturnsNull_WhenNotFound()
    {
        _apiClient.GetEventAgendaItemByIdAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not found", 404));

        var result = await _service.GetAgendaItemByIdAsync(Guid.NewGuid());

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetAgendaItemByIdAsync_ReturnsNull_WhenApiThrows()
    {
        _apiClient.GetEventAgendaItemByIdAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Server error"));

        var result = await _service.GetAgendaItemByIdAsync(Guid.NewGuid());

        await Assert.That(result).IsNull();
    }

    // ========== CreateAgendaItemAsync ==========

    [Test]
    public async Task CreateAgendaItemAsync_ReturnsResponse_WhenApiSucceeds()
    {
        var newId = Guid.NewGuid();
        var dto = new CreateEventAgendaItemDto { EventId = Guid.NewGuid(), Title = "Workshop" };
        var response = new BaseCommandResponseOfGuid { Success = true, Id = newId };

        _apiClient.CreateEventAgendaItemAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _service.CreateAgendaItemAsync(dto);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(newId);
    }

    [Test]
    public async Task CreateAgendaItemAsync_ReturnsFailureResponse_WhenApiThrows()
    {
        var dto = new CreateEventAgendaItemDto { EventId = Guid.NewGuid(), Title = "Workshop" };

        _apiClient.CreateEventAgendaItemAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Validation failed", 400));

        var result = await _service.CreateAgendaItemAsync(dto);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
    }

    // ========== UpdateAgendaItemAsync ==========

    [Test]
    public async Task UpdateAgendaItemAsync_ReturnsResponse_WhenApiSucceeds()
    {
        var itemId = Guid.NewGuid();
        var dto = new UpdateEventAgendaItemDto { Title = "Updated Workshop" };
        var response = new BaseCommandResponseOfGuid { Success = true, Id = itemId };

        _apiClient.UpdateEventAgendaItemAsync(itemId, dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _service.UpdateAgendaItemAsync(itemId, dto);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
    }

    [Test]
    public async Task UpdateAgendaItemAsync_ReturnsFailureResponse_WhenApiThrows()
    {
        var itemId = Guid.NewGuid();
        var dto = new UpdateEventAgendaItemDto { Title = "Updated Workshop" };

        _apiClient.UpdateEventAgendaItemAsync(itemId, dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Conflict", 409));

        var result = await _service.UpdateAgendaItemAsync(itemId, dto);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
    }

    // ========== DeleteAgendaItemAsync ==========

    [Test]
    public async Task DeleteAgendaItemAsync_ReturnsTrue_WhenApiSucceeds()
    {
        var itemId = Guid.NewGuid();

        _apiClient.DeleteEventAgendaItemAsync(itemId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAgendaItemAsync(itemId);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task DeleteAgendaItemAsync_ReturnsFalse_WhenApiThrows()
    {
        _apiClient.DeleteEventAgendaItemAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Delete failed"));

        var result = await _service.DeleteAgendaItemAsync(Guid.NewGuid());

        await Assert.That(result).IsFalse();
    }

    // ========== Helpers ==========

    private static HalCollectionResourceOfEventAgendaItemListDto CreateHalCollectionResponse(
        IList<EventAgendaItemListDto> items)
    {
        return new HalCollectionResourceOfEventAgendaItemListDto
        {
            _embedded = new HalCollectionEmbeddedOfEventAgendaItemListDto
            {
                Items = items.Cast<object>().ToList()
            }
        };
    }

    private static HalResourceOfEventAgendaItemDto CreateHalResourceResponse(EventAgendaItemDto dto)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(dto);
        return System.Text.Json.JsonSerializer.Deserialize<HalResourceOfEventAgendaItemDto>(json)
               ?? new HalResourceOfEventAgendaItemDto();
    }

    private static ApiException CreateApiException(string message, int statusCode, string response = "")
    {
        return new ApiException(
            message,
            statusCode,
            response,
            new Dictionary<string, IEnumerable<string>>(),
            new InvalidOperationException(message));
    }
}
