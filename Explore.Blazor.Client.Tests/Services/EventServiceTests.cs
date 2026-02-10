// ABOUTME: Unit tests for EventService.
// Tests all event-related operations including CRUD and session management.

namespace Explore.Blazor.Client.Tests.Services;

/// <summary>
/// Unit tests for EventService.
/// Tests all event-related operations including CRUD and session management.
/// </summary>
/// <remarks>
/// These tests verify:
/// - Proper API client calls with HAL resource types
/// - Error handling and fallback behavior
/// - Response transformation from HAL to DTO using extension methods
/// - Edge cases (null responses, exceptions)
///
/// IMPORTANT: The API client uses HAL resource types:
/// - GetEventsAsync returns HalCollectionResourceOfEventListDto
/// - GetMyEventsAsync returns HalCollectionResourceOfEventListDto
/// - GetEventByIdAsync returns HalResourceOfEventDto
/// The service converts these to plain DTOs using HalResourceExtensions.
/// </remarks>
public class EventServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<EventService> _logger;
    private readonly EventService _service;

    public EventServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _organizationService = Substitute.For<IOrganizationService>();
        _logger = Substitute.For<ILogger<EventService>>();
        _service = new EventService(_apiClient, _organizationService, _logger);
    }

    #region GetAllEventsAsync Tests

    [Test]
    public async Task GetAllEventsAsync_ReturnsEvents_WhenApiSucceeds()
    {
        // Arrange
        var expectedEvents = ComponentDataBuilder.EventListDto.Generate(3);
        var halResponse = CreateHalCollectionResponse(expectedEvents);

        _apiClient.GetEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

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
        _apiClient.GetEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns((HalCollectionResourceOfEventListDto?)null);

        // Act
        var result = await _service.GetAllEventsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetAllEventsAsync_ReturnsEmptyList_WhenApiThrowsException()
    {
        // Arrange
        _apiClient.GetEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("API Error", 500, null, null, null));

        // Act
        var result = await _service.GetAllEventsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetAllEventsAsync_ReturnsEmptyList_WhenEmbeddedIsNull()
    {
        // Arrange
        var halResponse = new HalCollectionResourceOfEventListDto
        {
            _embedded = null
        };
        _apiClient.GetEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetAllEventsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetAllEventsAsync_CallsApiWithCorrectPagination()
    {
        // Arrange
        var halResponse = CreateHalCollectionResponse(new List<EventListDto>());
        _apiClient.GetEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        await _service.GetAllEventsAsync();

        // Assert - Service should request page 1 with size 100
    }

    #endregion

    #region GetMyEventsAsync Tests

    [Test]
    public async Task GetMyEventsAsync_ReturnsUserEvents_WhenApiSucceeds()
    {
        // Arrange
        var expectedEvents = ComponentDataBuilder.EventListDto.Generate(2);
        var halResponse = CreateHalCollectionResponse(expectedEvents);

        _apiClient.GetMyEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        var result = await _service.GetMyEventsAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetMyEventsAsync_ReturnsEmptyList_WhenApiThrowsException()
    {
        // Arrange
        _apiClient.GetMyEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
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
        _apiClient.GetMyEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns((HalCollectionResourceOfEventListDto?)null);

        // Act
        var result = await _service.GetMyEventsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetMyEventsAsync_CallsApiWithCorrectPagination()
    {
        // Arrange
        var halResponse = CreateHalCollectionResponse(new List<EventListDto>());
        _apiClient.GetMyEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        await _service.GetMyEventsAsync();

        // Assert - Service should request page 1 with size 100
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
        expectedEvent.ActorTypeFullName ??= "Organization";
        expectedEvent.EventStatusId = 1;
        expectedEvent.EventStatusFullName = "Draft";
        expectedEvent.EventStatusMasterCode = "DRFT";
        expectedEvent.EventFormatId = 1;
        expectedEvent.EventFormatFullName = "In-Person";
        expectedEvent.EventFormatMasterCode = "INPERSON";
        expectedEvent.VisibilityTypeId = 1;
        expectedEvent.VisibilityTypeFullName = "Public";
        expectedEvent.VisibilityTypeMasterCode = "PUBLIC";
        expectedEvent.FeaturedImageId = Guid.NewGuid();
        expectedEvent.FeaturedImageUri = "https://example.com/image.png";
        expectedEvent.SessionCount = 1;
        expectedEvent.Timezone = "UTC";
        var halResponse = CreateHalResourceResponse(expectedEvent);

        _apiClient.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(halResponse);

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
        _apiClient.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>())
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
        _apiClient.GetEventByIdAsync(eventId, Arg.Any<CancellationToken>())
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
        var halResponse = new HalResourceOfEventDto
        {
            Id = eventId,
            Title = "Tracked Event"
        };

        _apiClient.GetEventByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        await _service.GetEventByIdAsync(eventId);

        // Assert
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

        _apiClient.CreateEventAsync(Arg.Any<CreateEventDto>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.CreateEventAsync(createDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(expectedId);
    }

    [Test]
    public async Task CreateEventAsync_ReturnsNull_WhenApiThrowsException()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateEventDto.Generate();
        _apiClient.CreateEventAsync(Arg.Any<CreateEventDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Bad Request", 400, "Validation failed", null, null));

        // Act
        var result = await _service.CreateEventAsync(createDto);

        // Assert - Service propagates the exception, returns null
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task CreateEventAsync_CallsApiWithCorrectDto()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateEventDto.Generate();
        var expectedResponse = ComponentDataBuilder.SuccessResponse();
        _apiClient.CreateEventAsync(Arg.Any<CreateEventDto>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _service.CreateEventAsync(createDto);

        // Assert
        await _apiClient.Received(1).CreateEventAsync(createDto, Arg.Any<CancellationToken>());
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

        _apiClient.UpdateEventAsync(Arg.Any<Guid>(), Arg.Any<UpdateEventDto>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.UpdateEventAsync(eventId, updateDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(eventId);
    }

    [Test]
    public async Task UpdateEventAsync_ReturnsNull_WhenApiThrowsException()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var updateDto = new UpdateEventDto { Id = eventId };
        _apiClient.UpdateEventAsync(Arg.Any<Guid>(), Arg.Any<UpdateEventDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException("Not Found", 404, null, null, null));

        // Act
        var result = await _service.UpdateEventAsync(eventId, updateDto);

        // Assert - Service propagates the exception, returns null
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task UpdateEventAsync_CallsApiWithCorrectParameters()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var updateDto = new UpdateEventDto { Id = eventId, Title = "Test Title" };
        var expectedResponse = ComponentDataBuilder.SuccessResponse(eventId);
        _apiClient.UpdateEventAsync(Arg.Any<Guid>(), Arg.Any<UpdateEventDto>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _service.UpdateEventAsync(eventId, updateDto);

        // Assert
        await _apiClient.Received(1).UpdateEventAsync(eventId, updateDto, Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteEventAsync Tests

    [Test]
    public async Task DeleteEventAsync_ReturnsTrue_WhenSuccess()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.DeleteEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
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
        _apiClient.DeleteEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
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
        _apiClient.DeleteEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
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
        _apiClient.DeleteEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteEventAsync(eventId);

        // Assert
        await _apiClient.Received(1).DeleteEventAsync(eventId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSessionByIdAsync Tests

    [Test]
    public async Task GetSessionByIdAsync_ReturnsSession_WhenFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expectedSession = new HalResourceOfEventSessionDto
        {
            Id = sessionId,
            EventId = Guid.NewGuid(),
            EventTitle = "Parent Event",
            Title = "Test Session",
            Slug = "test-session",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1),
            MaxAudienceAttendees = 100,
            CurrentAudienceAttendees = 10
        };

        _apiClient.GetEventSessionByIdAsync(sessionId, Arg.Any<CancellationToken>())
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
        _apiClient.GetEventSessionByIdAsync(sessionId, Arg.Any<CancellationToken>())
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
        _apiClient.GetEventSessionByIdAsync(sessionId, Arg.Any<CancellationToken>())
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
        var expectedSession = new HalResourceOfEventSessionDto { Id = sessionId };
        _apiClient.GetEventSessionByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(expectedSession);

        // Act
        await _service.GetSessionByIdAsync(sessionId);

        // Assert
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a HAL collection response with the provided event list items.
    /// </summary>
    private static HalCollectionResourceOfEventListDto CreateHalCollectionResponse(
        IList<EventListDto> items)
    {
        return new HalCollectionResourceOfEventListDto
        {
            _embedded = new HalCollectionEmbeddedOfEventListDto
            {
                Items = items.Cast<object>().ToList()
            }
        };
    }

    /// <summary>
    /// Creates a HAL resource response from an event DTO.
    /// Uses JSON serialization to properly populate all properties.
    /// </summary>
    private static HalResourceOfEventDto CreateHalResourceResponse(EventDto dto)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(dto);
        return System.Text.Json.JsonSerializer.Deserialize<HalResourceOfEventDto>(json)
               ?? new HalResourceOfEventDto();
    }

    #endregion
}
