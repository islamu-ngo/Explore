namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for EventService.
/// Tests all event-related operations including CRUD and session management.
/// </summary>
/// <remarks>
/// These tests verify:
/// - Proper API client calls
/// - Error handling and fallback behavior
/// - Response transformation
/// - Edge cases (null responses, exceptions)
///
/// IMPORTANT: The API client has two overloads for each method:
/// - Without CancellationToken (used by the service)
/// - With CancellationToken
/// We must mock the correct overload (without CancellationToken) for tests to work.
/// </remarks>
public class EventServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<EventService> _logger;
    private readonly EventService _service;

    public EventServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _logger = Substitute.For<ILogger<EventService>>();
        _service = new EventService(_apiClient, _logger);
    }

    #region GetAllEventsAsync Tests

    [Test]
    public async Task GetAllEventsAsync_ReturnsEvents_WhenApiSucceeds()
    {
        // Arrange
        var expectedEvents = ComponentDataBuilder.EventListDto.Generate(3);
        var response = new PaginatedResultOfEventListDto
        {
            Items = expectedEvents,
            TotalCount = 3,
            PageNumber = 1,
            PageSize = 100
        };
        // Service calls EventGETAsync(pageNumber: 1, pageSize: 100) without CancellationToken
        _apiClient.EventGETAsync(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns(response);

        // Act
        var result = await _service.GetAllEventsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(result.First().Title).IsEqualTo(expectedEvents.First().Title);
        await Assert.That(result.First().Id).IsEqualTo(expectedEvents.First().Id);
    }

    [Test]
    public async Task GetAllEventsAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // Arrange
        _apiClient.EventGETAsync(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns((PaginatedResultOfEventListDto?)null);

        // Act
        var result = await _service.GetAllEventsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetAllEventsAsync_ReturnsEmptyList_WhenApiThrowsException()
    {
        // Arrange
        _apiClient.EventGETAsync(Arg.Any<int?>(), Arg.Any<int?>())
            .ThrowsAsync(new ApiException("API Error", 500, null, null, null));

        // Act
        var result = await _service.GetAllEventsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetAllEventsAsync_ReturnsEmptyList_WhenItemsIsNull()
    {
        // Arrange
        var response = new PaginatedResultOfEventListDto
        {
            Items = null,
            TotalCount = 0
        };
        _apiClient.EventGETAsync(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns(response);

        // Act
        var result = await _service.GetAllEventsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetAllEventsAsync_CallsApiWithCorrectPagination()
    {
        // Arrange
        var response = new PaginatedResultOfEventListDto
        {
            Items = new List<EventListDto>(),
            TotalCount = 0
        };
        _apiClient.EventGETAsync(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns(response);

        // Act
        await _service.GetAllEventsAsync();

        // Assert - Service should request page 1 with size 100
        await _apiClient.Received(1).EventGETAsync(1, 100);
    }

    #endregion

    #region GetMyEventsAsync Tests

    [Test]
    public async Task GetMyEventsAsync_ReturnsUserEvents_WhenApiSucceeds()
    {
        // Arrange
        var expectedEvents = ComponentDataBuilder.EventListDto.Generate(2);
        var response = new PaginatedResultOfEventListDto
        {
            Items = expectedEvents,
            TotalCount = 2
        };
        // Service calls MyAsync(pageNumber: 1, pageSize: 100) without CancellationToken
        _apiClient.MyAsync(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns(response);

        // Act
        var result = await _service.GetMyEventsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetMyEventsAsync_ReturnsEmptyList_WhenApiThrowsException()
    {
        // Arrange
        _apiClient.MyAsync(Arg.Any<int?>(), Arg.Any<int?>())
            .ThrowsAsync(new ApiException("Unauthorized", 401, null, null, null));

        // Act
        var result = await _service.GetMyEventsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetMyEventsAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // Arrange
        _apiClient.MyAsync(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns((PaginatedResultOfEventListDto?)null);

        // Act
        var result = await _service.GetMyEventsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetMyEventsAsync_CallsApiWithCorrectPagination()
    {
        // Arrange
        var response = new PaginatedResultOfEventListDto
        {
            Items = new List<EventListDto>(),
            TotalCount = 0
        };
        _apiClient.MyAsync(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns(response);

        // Act
        await _service.GetMyEventsAsync();

        // Assert - Service should request page 1 with size 100
        await _apiClient.Received(1).MyAsync(1, 100);
    }

    #endregion

    #region GetEventByIdAsync Tests

    [Test]
    public async Task GetEventByIdAsync_ReturnsEvent_WhenFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var expectedEvent = ComponentDataBuilder.EventDto.Generate();
        expectedEvent.Id = eventId;
        // Service calls EventGET2Async(eventId) without CancellationToken
        _apiClient.EventGET2Async(eventId)
            .Returns(expectedEvent);

        // Act
        var result = await _service.GetEventByIdAsync(eventId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(eventId);
        await Assert.That(result.Title).IsEqualTo(expectedEvent.Title);
    }

    [Test]
    public async Task GetEventByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.EventGET2Async(eventId)
            .ThrowsAsync(new ApiException("Not Found", 404, null, null, null));

        // Act
        var result = await _service.GetEventByIdAsync(eventId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetEventByIdAsync_ReturnsNull_WhenApiThrowsException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.EventGET2Async(eventId)
            .ThrowsAsync(new ApiException("Server Error", 500, null, null, null));

        // Act
        var result = await _service.GetEventByIdAsync(eventId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetEventByIdAsync_CallsApiWithCorrectId()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var expectedEvent = ComponentDataBuilder.EventDto.Generate();
        _apiClient.EventGET2Async(Arg.Any<Guid>())
            .Returns(expectedEvent);

        // Act
        await _service.GetEventByIdAsync(eventId);

        // Assert
        await _apiClient.Received(1).EventGET2Async(eventId);
    }

    #endregion

    #region CreateEventAsync Tests

    [Test]
    public async Task CreateEventAsync_ReturnsSuccess_WhenValid()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateEventDto.Generate();
        var expectedId = Guid.NewGuid();
        var expectedResponse = ComponentDataBuilder.SuccessResponse(expectedId);
        // Service calls EventPOSTAsync(createDto) without CancellationToken
        _apiClient.EventPOSTAsync(Arg.Any<CreateEventDto>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.CreateEventAsync(createDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(expectedId);
    }

    [Test]
    public async Task CreateEventAsync_ReturnsFailure_WhenApiThrowsException()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateEventDto.Generate();
        _apiClient.EventPOSTAsync(Arg.Any<CreateEventDto>())
            .ThrowsAsync(new ApiException("Bad Request", 400, "Validation failed", null, null));

        // Act
        // EventService catches exceptions and returns a failure response (doesn't throw)
        var result = await _service.CreateEventAsync(createDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
    }

    [Test]
    public async Task CreateEventAsync_CallsApiWithCorrectDto()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateEventDto.Generate();
        var expectedResponse = ComponentDataBuilder.SuccessResponse();
        _apiClient.EventPOSTAsync(Arg.Any<CreateEventDto>())
            .Returns(expectedResponse);

        // Act
        await _service.CreateEventAsync(createDto);

        // Assert
        await _apiClient.Received(1).EventPOSTAsync(createDto);
    }

    [Test]
    public async Task CreateEventAsync_ReturnsSuccessResponse_OnSuccessfulStatusCode()
    {
        // Arrange - Simulate NSwag throwing on 200/201 but with successful status
        var createDto = ComponentDataBuilder.CreateEventDto.Generate();
        _apiClient.EventPOSTAsync(Arg.Any<CreateEventDto>())
            .ThrowsAsync(new ApiException("Response parsing issue", 200, null, null, null));

        // Act
        // EventService has special handling for 200/201 status codes
        var result = await _service.CreateEventAsync(createDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
    }

    #endregion

    #region UpdateEventAsync Tests

    [Test]
    public async Task UpdateEventAsync_ReturnsSuccess_WhenValid()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var updateDto = new UpdateEventDto { Id = eventId, Title = "Updated Title" };
        var expectedResponse = ComponentDataBuilder.SuccessResponse(eventId);
        // Service calls EventPUTAsync(eventId, eventDto) without CancellationToken
        _apiClient.EventPUTAsync(Arg.Any<Guid>(), Arg.Any<UpdateEventDto>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.UpdateEventAsync(eventId, updateDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(eventId);
    }

    [Test]
    public async Task UpdateEventAsync_ReturnsFailure_WhenApiThrowsException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var updateDto = new UpdateEventDto { Id = eventId };
        _apiClient.EventPUTAsync(Arg.Any<Guid>(), Arg.Any<UpdateEventDto>())
            .ThrowsAsync(new ApiException("Not Found", 404, null, null, null));

        // Act
        // EventService catches exceptions and returns a failure response (doesn't throw)
        var result = await _service.UpdateEventAsync(eventId, updateDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
    }

    [Test]
    public async Task UpdateEventAsync_CallsApiWithCorrectParameters()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var updateDto = new UpdateEventDto { Id = eventId, Title = "Test Title" };
        var expectedResponse = ComponentDataBuilder.SuccessResponse(eventId);
        _apiClient.EventPUTAsync(Arg.Any<Guid>(), Arg.Any<UpdateEventDto>())
            .Returns(expectedResponse);

        // Act
        await _service.UpdateEventAsync(eventId, updateDto);

        // Assert
        await _apiClient.Received(1).EventPUTAsync(eventId, updateDto);
    }

    #endregion

    #region DeleteEventAsync Tests

    [Test]
    public async Task DeleteEventAsync_ReturnsTrue_WhenSuccess()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        // Service calls EventDELETEAsync(eventId) without CancellationToken
        _apiClient.EventDELETEAsync(Arg.Any<Guid>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteEventAsync(eventId);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task DeleteEventAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.EventDELETEAsync(Arg.Any<Guid>())
            .ThrowsAsync(new ApiException("Not Found", 404, null, null, null));

        // Act
        var result = await _service.DeleteEventAsync(eventId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task DeleteEventAsync_ReturnsFalse_WhenUnauthorized()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.EventDELETEAsync(Arg.Any<Guid>())
            .ThrowsAsync(new ApiException("Unauthorized", 401, null, null, null));

        // Act
        var result = await _service.DeleteEventAsync(eventId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task DeleteEventAsync_CallsApiWithCorrectId()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.EventDELETEAsync(Arg.Any<Guid>())
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteEventAsync(eventId);

        // Assert
        await _apiClient.Received(1).EventDELETEAsync(eventId);
    }

    #endregion

    #region GetSessionByIdAsync Tests

    [Test]
    public async Task GetSessionByIdAsync_ReturnsSession_WhenFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expectedSession = new EventSessionDto { Id = sessionId, Title = "Test Session" };
        // Service calls EventSessionGET2Async(sessionId) without CancellationToken
        _apiClient.EventSessionGET2Async(sessionId)
            .Returns(expectedSession);

        // Act
        var result = await _service.GetSessionByIdAsync(sessionId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(sessionId);
        await Assert.That(result.Title).IsEqualTo("Test Session");
    }

    [Test]
    public async Task GetSessionByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _apiClient.EventSessionGET2Async(sessionId)
            .ThrowsAsync(new ApiException("Not Found", 404, null, null, null));

        // Act
        var result = await _service.GetSessionByIdAsync(sessionId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetSessionByIdAsync_ReturnsNull_WhenApiThrowsException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _apiClient.EventSessionGET2Async(sessionId)
            .ThrowsAsync(new ApiException("Server Error", 500, null, null, null));

        // Act
        var result = await _service.GetSessionByIdAsync(sessionId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetSessionByIdAsync_CallsApiWithCorrectId()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expectedSession = new EventSessionDto { Id = sessionId };
        _apiClient.EventSessionGET2Async(Arg.Any<Guid>())
            .Returns(expectedSession);

        // Act
        await _service.GetSessionByIdAsync(sessionId);

        // Assert
        await _apiClient.Received(1).EventSessionGET2Async(sessionId);
    }

    #endregion
}
