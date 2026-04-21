// ABOUTME: Unit tests for LocationRoomService covering CRUD operations.
// ABOUTME: Tests GetRoomsByLocation, GetRoomById, CreateRoom, UpdateRoom, DeleteRoom with success and error paths.

namespace Explore.Blazor.Client.Tests.Services;

public class LocationRoomServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<LocationRoomService> _logger;
    private readonly LocationRoomService _service;

    public LocationRoomServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _logger = Substitute.For<ILogger<LocationRoomService>>();
        _service = new LocationRoomService(_apiClient, _logger);
    }

    // ========== GetRoomsByLocationAsync ==========

    [Test]
    public async Task GetRoomsByLocationAsync_ReturnsRooms_WhenApiSucceeds()
    {
        var locationId = Guid.NewGuid();
        var halResponse = CreateHalCollectionResponse(new List<LocationRoomListDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Main Hall", Capacity = 500 },
            new() { Id = Guid.NewGuid(), Name = "Room A", Capacity = 50 }
        });

        _apiClient.GetLocationRoomsByLocationAsync(locationId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        var result = await _service.GetRoomsByLocationAsync(locationId);

        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetRoomsByLocationAsync_ReturnsEmptyList_WhenApiThrows()
    {
        _apiClient.GetLocationRoomsByLocationAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Network error"));

        var result = await _service.GetRoomsByLocationAsync(Guid.NewGuid());

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetRoomsByLocationAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        _apiClient.GetLocationRoomsByLocationAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns((HalCollectionResourceOfLocationRoomListDto?)null);

        var result = await _service.GetRoomsByLocationAsync(Guid.NewGuid());

        await Assert.That(result).IsEmpty();
    }

    // ========== GetRoomByIdAsync ==========

    [Test]
    public async Task GetRoomByIdAsync_ReturnsRoom_WhenApiSucceeds()
    {
        var roomId = Guid.NewGuid();
        var dto = new LocationRoomDto
        {
            Id = roomId,
            Name = "Main Hall",
            Capacity = 500
        };
        var halResponse = CreateHalResourceResponse(dto);

        _apiClient.GetLocationRoomByIdAsync(roomId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        var result = await _service.GetRoomByIdAsync(roomId);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo("Main Hall");
    }

    [Test]
    public async Task GetRoomByIdAsync_ReturnsNull_WhenNotFound()
    {
        _apiClient.GetLocationRoomByIdAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not found", 404));

        var result = await _service.GetRoomByIdAsync(Guid.NewGuid());

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetRoomByIdAsync_ReturnsNull_WhenApiThrows()
    {
        _apiClient.GetLocationRoomByIdAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Server error"));

        var result = await _service.GetRoomByIdAsync(Guid.NewGuid());

        await Assert.That(result).IsNull();
    }

    // ========== CreateRoomAsync ==========

    [Test]
    public async Task CreateRoomAsync_ReturnsResponse_WhenApiSucceeds()
    {
        var newId = Guid.NewGuid();
        var dto = new CreateLocationRoomDto { LocationId = Guid.NewGuid(), Name = "Room B" };
        var response = new BaseCommandResponseOfGuid { Success = true, Id = newId };

        _apiClient.CreateLocationRoomAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _service.CreateRoomAsync(dto);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(newId);
    }

    [Test]
    public async Task CreateRoomAsync_ReturnsFailureResponse_WhenApiThrows()
    {
        var dto = new CreateLocationRoomDto { LocationId = Guid.NewGuid(), Name = "Room B" };

        _apiClient.CreateLocationRoomAsync(dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Validation failed", 400));

        var result = await _service.CreateRoomAsync(dto);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
    }

    // ========== UpdateRoomAsync ==========

    [Test]
    public async Task UpdateRoomAsync_ReturnsResponse_WhenApiSucceeds()
    {
        var roomId = Guid.NewGuid();
        var dto = new UpdateLocationRoomDto { Name = "Updated Hall" };
        var response = new BaseCommandResponseOfGuid { Success = true, Id = roomId };

        _apiClient.UpdateLocationRoomAsync(roomId, dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var result = await _service.UpdateRoomAsync(roomId, dto);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
    }

    [Test]
    public async Task UpdateRoomAsync_ReturnsFailureResponse_WhenApiThrows()
    {
        var roomId = Guid.NewGuid();
        var dto = new UpdateLocationRoomDto { Name = "Updated Hall" };

        _apiClient.UpdateLocationRoomAsync(roomId, dto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Conflict", 409));

        var result = await _service.UpdateRoomAsync(roomId, dto);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
    }

    // ========== DeleteRoomAsync ==========

    [Test]
    public async Task DeleteRoomAsync_ReturnsTrue_WhenApiSucceeds()
    {
        var roomId = Guid.NewGuid();

        _apiClient.DeleteLocationRoomAsync(roomId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteRoomAsync(roomId);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task DeleteRoomAsync_ReturnsFalse_WhenApiThrows()
    {
        _apiClient.DeleteLocationRoomAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Delete failed"));

        var result = await _service.DeleteRoomAsync(Guid.NewGuid());

        await Assert.That(result).IsFalse();
    }

    // ========== Helpers ==========

    private static HalCollectionResourceOfLocationRoomListDto CreateHalCollectionResponse(
        IList<LocationRoomListDto> items)
    {
        return new HalCollectionResourceOfLocationRoomListDto
        {
            _embedded = new HalCollectionEmbeddedOfLocationRoomListDto
            {
                Items = items.Cast<object>().ToList()
            }
        };
    }

    private static HalResourceOfLocationRoomDto CreateHalResourceResponse(LocationRoomDto dto)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(dto);
        return System.Text.Json.JsonSerializer.Deserialize<HalResourceOfLocationRoomDto>(json)
               ?? new HalResourceOfLocationRoomDto();
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
