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
        var halResponse = CreateHalCollectionResponse(expectedEvents);

        _apiClient.GetEventsAsync().ReturnsForAnyArgs(halResponse);

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
        _apiClient.GetEventsAsync().ReturnsForAnyArgs((HalCollectionResourceOfEventListDto?)null);

        // Act
        var result = await _service.GetAllEventsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetAllEventsAsync_ReturnsEmptyList_WhenApiThrowsException()
    {
        // Arrange
        _apiClient.GetEventsAsync().ThrowsAsyncForAnyArgs(CreateApiException("API Error", 500));

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
        _apiClient.GetEventsAsync().ReturnsForAnyArgs(halResponse);

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
        _apiClient.GetEventsAsync().ReturnsForAnyArgs(halResponse);

        // Act
        await _service.GetAllEventsAsync();

        // Assert - Service should request page 1 with size 100
        await _apiClient.Received(1).GetEventsAsync(1, 100);
    }

    #endregion

    #region GetMyEventsAsync Tests

    [Test]
    public async Task GetMyEventsAsync_ReturnsUserEvents_WhenApiSucceeds()
    {
        // Arrange
        var expectedEvents = ComponentDataBuilder.EventListDto.Generate(2);
        var halResponse = CreateHalCollectionResponse(expectedEvents);

        _apiClient.GetMyEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
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
        _apiClient.GetMyEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Unauthorized", 401));

        // Act
        var result = await _service.GetMyEventsAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task GetMyEventsAsync_ReturnsEmptyList_WhenApiReturnsNull()
    {
        // Arrange
        _apiClient.GetMyEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
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
        _apiClient.GetMyEventsAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        await _service.GetMyEventsAsync();

        // Assert - Service should request page 1 with size 100
        await _apiClient.Received(1).GetMyEventsAsync(1, 100, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
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

        _apiClient.GetEventByIdAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
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
        _apiClient.GetEventByIdAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

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
        _apiClient.GetEventByIdAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

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

        _apiClient.GetEventByIdAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(halResponse);

        // Act
        await _service.GetEventByIdAsync(eventId);

        // Assert
        await _apiClient.Received(1).GetEventByIdAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetEventCreationContextAsync Tests

    [Test]
    public async Task GetEventCreationContextAsync_ReturnsContext_WhenApiSucceeds()
    {
        // Arrange
        var context = new EventCreationContextDto
        {
            CanCreate = true,
            DefaultPublisherMode = "personal",
            PublisherOptions =
            [
                new EventCreationPublisherOptionDto
                {
                    PublisherMode = "personal",
                    DisplayName = "Personal profile",
                    CanPublish = true
                }
            ]
        };

        _apiClient.GetEventCreationContextAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(context);

        // Act
        var result = await _service.GetEventCreationContextAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.CanCreate).IsTrue();
        await Assert.That(result.DefaultPublisherMode).IsEqualTo("personal");
        await Assert.That(result.PublisherOptions?.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetEventCreationContextAsync_ReturnsNull_WhenApiThrowsException()
    {
        // Arrange
        _apiClient.GetEventCreationContextAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Unauthorized", 401));

        // Act
        var result = await _service.GetEventCreationContextAsync();

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetEventCreationContextAsync_PassesCancellationToken()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var context = new EventCreationContextDto { CanCreate = false };

        _apiClient.GetEventCreationContextAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(context);

        // Act
        await _service.GetEventCreationContextAsync(cancellationTokenSource.Token);

        // Assert
        await _apiClient.Received(1).GetEventCreationContextAsync(
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            cancellationTokenSource.Token);
    }

    #endregion

    #region GetEventPublishReadinessAsync Tests

    [Test]
    public async Task GetEventPublishReadinessAsync_ReturnsReadiness_WhenApiSucceeds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var readiness = new EventPublishReadinessDto
        {
            EventId = eventId,
            IsReady = false,
            Errors =
            [
                new EventPublishReadinessErrorDto
                {
                    Code = "schedule_session_required",
                    FieldPath = "schedule.sessions",
                    Message = "At least one scheduled session is required before publishing.",
                    Severity = "error"
                }
            ]
        };

        _apiClient.GetEventPublishReadinessAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(readiness);

        // Act
        var result = await _service.GetEventPublishReadinessAsync(eventId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsReady).IsFalse();
        await Assert.That(result.EventId).IsEqualTo(eventId);
        await Assert.That(result.Errors?.Count).IsEqualTo(1);
        await Assert.That(result.Errors?.First().FieldPath).IsEqualTo("schedule.sessions");
    }

    [Test]
    public async Task GetEventPublishReadinessAsync_ReturnsNull_WhenApiThrowsException()
    {
        // Arrange
        _apiClient.GetEventPublishReadinessAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

        // Act
        var result = await _service.GetEventPublishReadinessAsync(Guid.NewGuid());

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetEventPublishReadinessAsync_PassesCancellationToken()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var eventId = Guid.NewGuid();
        var readiness = new EventPublishReadinessDto { EventId = eventId, IsReady = true };

        _apiClient.GetEventPublishReadinessAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(readiness);

        // Act
        await _service.GetEventPublishReadinessAsync(eventId, cancellationTokenSource.Token);

        // Assert
        await _apiClient.Received(1).GetEventPublishReadinessAsync(
            eventId,
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            cancellationTokenSource.Token);
    }

    #endregion

    #region CreateEventAsync Tests

    [Test]
    public async Task CreateEventAsync_ReturnsSuccess_WhenValid()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateEventRequest.Generate();
        var expectedId = Guid.NewGuid();
        var expectedResponse = ComponentDataBuilder.SuccessResponse(expectedId);

            _apiClient.CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
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
        var createDto = ComponentDataBuilder.CreateEventRequest.Generate();
            _apiClient.CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Bad Request", 400, "Validation failed"));

        // Act
        var result = await _service.CreateEventAsync(createDto);

        // Assert - Service propagates the exception, returns null
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task CreateEventAsync_CallsApiWithCorrectDto()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateEventRequest.Generate();
        var expectedResponse = ComponentDataBuilder.SuccessResponse();
            _apiClient.CreateEventAsync(Arg.Any<CreateEventDraftRequestDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _service.CreateEventAsync(createDto);

        // Assert
        await _apiClient.Received(1).CreateEventAsync(createDto, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region PublishEventAsync Tests

    [Test]
    public async Task PublishEventAsync_ReturnsSuccess_WhenApiSucceeds()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        var expectedResponse = ComponentDataBuilder.SuccessResponse(eventId);

        _apiClient.PublishEventAsync(
                Arg.Any<Guid>(),
                Arg.Any<PublishEventRequestDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _service.PublishEventAsync(eventId, concurrencyStamp);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(eventId);
    }

    [Test]
    public async Task PublishEventAsync_SendsExpectedConcurrencyStamp()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        var expectedResponse = ComponentDataBuilder.SuccessResponse(eventId);

        _apiClient.PublishEventAsync(
                Arg.Any<Guid>(),
                Arg.Any<PublishEventRequestDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _service.PublishEventAsync(eventId, concurrencyStamp);

        // Assert
        await _apiClient.Received(1).PublishEventAsync(
            eventId,
            Arg.Is<PublishEventRequestDto>(request => request.ExpectedConcurrencyStamp == concurrencyStamp),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task PublishEventAsync_ReturnsCommandResponse_WhenApiRejectsPublish()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        var rejectedResponse = new BaseCommandResponseOfGuid
        {
            Success = false,
            Id = eventId,
            FailureCode = "event_publish_concurrency_conflict",
            Errors = ["Refresh the event and try publishing again."]
        };

        _apiClient.PublishEventAsync(
                Arg.Any<Guid>(),
                Arg.Any<PublishEventRequestDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException<BaseCommandResponseOfGuid>(
                "Conflict",
                409,
                string.Empty,
                new Dictionary<string, IEnumerable<string>>(),
                rejectedResponse,
                null));

        // Act
        var result = await _service.PublishEventAsync(eventId, concurrencyStamp);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_publish_concurrency_conflict");
    }

    [Test]
    public async Task PublishEventAsync_ReturnsNull_WhenApiThrowsUnexpectedException()
    {
        // Arrange
        _apiClient.PublishEventAsync(
                Arg.Any<Guid>(),
                Arg.Any<PublishEventRequestDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

        // Act
        var result = await _service.PublishEventAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task PublishEventAsync_PassesCancellationToken()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var eventId = Guid.NewGuid();
        var expectedResponse = ComponentDataBuilder.SuccessResponse(eventId);

        _apiClient.PublishEventAsync(
                Arg.Any<Guid>(),
                Arg.Any<PublishEventRequestDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _service.PublishEventAsync(eventId, Guid.NewGuid(), cancellationTokenSource.Token);

        // Assert
        await _apiClient.Received(1).PublishEventAsync(
            eventId,
            Arg.Any<PublishEventRequestDto>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            cancellationTokenSource.Token);
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

        _apiClient.UpdateEventAsync(Arg.Any<Guid>(), Arg.Any<UpdateEventRequestDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
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
        _apiClient.UpdateEventAsync(Arg.Any<Guid>(), Arg.Any<UpdateEventRequestDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

        // Act
        var result = await _service.UpdateEventAsync(eventId, updateDto);

        // Assert - Service propagates the exception, returns null
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task UpdateEventAsync_CallsApiWithCorrectParameters()
    {
        // Arrange
        var eventId = new Guid("d415b43c-3f93-4b68-9a2d-59021d838e11");
        var updateDto = new UpdateEventDto { Id = eventId, Title = "Test Title" };
        var expectedResponse = ComponentDataBuilder.SuccessResponse(eventId);
        _apiClient.UpdateEventAsync(Arg.Any<Guid>(), Arg.Any<UpdateEventRequestDto>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _service.UpdateEventAsync(eventId, updateDto);

        // Assert: command wraps the DTO correctly
        await _apiClient.Received(1).UpdateEventAsync(
            eventId,
            Arg.Is<UpdateEventRequestDto>(c => c != null && c.EventDto == updateDto),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteEventAsync Tests

    [Test]
    public async Task DeleteEventAsync_ReturnsTrue_WhenSuccess()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.DeleteEventAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
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
        _apiClient.DeleteEventAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

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
        _apiClient.DeleteEventAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Unauthorized", 401));

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
        _apiClient.DeleteEventAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteEventAsync(eventId);

        // Assert
        await _apiClient.Received(1).DeleteEventAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
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

        _apiClient.GetEventSessionByIdAsync(sessionId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
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
        _apiClient.GetEventSessionByIdAsync(sessionId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));

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
        _apiClient.GetEventSessionByIdAsync(sessionId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

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
        _apiClient.GetEventSessionByIdAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedSession);

        // Act
        await _service.GetSessionByIdAsync(sessionId);

        // Assert
        await _apiClient.Received(1).GetEventSessionByIdAsync(sessionId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteEventAsync_ReturnsFalse_WhenServerError()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _apiClient.DeleteEventAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

        // Act
        var result = await _service.DeleteEventAsync(eventId);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task GetEventsPagedAsync_ReturnsEmptyPage_WhenApiThrows()
    {
        // Arrange
        _apiClient.GetEventsAsync(Arg.Any<int?>(), Arg.Any<int?>())
            .ThrowsAsync(CreateApiException("Server Error", 500));

        // Act
        var result = await _service.GetEventsPagedAsync(pageNumber: 3, pageSize: 25);

        // Assert
        await Assert.That(result.Items).IsEmpty();
        await Assert.That(result.PageNumber).IsEqualTo(3);
        await Assert.That(result.PageSize).IsEqualTo(25);
    }

    [Test]
    public async Task GetEventsPagedAsync_CallsApiWithCorrectPagination()
    {
        // Arrange
        var halResponse = CreateHalCollectionResponse(new List<EventListDto>());
        _apiClient.GetEventsAsync(Arg.Any<int?>(), Arg.Any<int?>())
            .Returns(halResponse);

        // Act
        await _service.GetEventsPagedAsync(pageNumber: 2, pageSize: 15);

        // Assert
        await _apiClient.Received(1).GetEventsAsync(2, 15);
    }

    [Test]
    public async Task GetPublicEventsByActorAsync_ForwardsActorIdToServerFilter()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var otherActorId = Guid.NewGuid();
        var events = new List<EventListDto>
        {
            new() { Id = Guid.NewGuid(), ActorId = actorId, Title = "Actor event" },
            new() { Id = Guid.NewGuid(), ActorId = otherActorId, Title = "Server decides visibility" }
        };
        _apiClient.GetEventsAsync().ReturnsForAnyArgs(CreateHalCollectionResponse(events));

        // Act
        var result = await _service.GetPublicEventsByActorAsync(actorId);

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await _apiClient.Received(1).GetEventsAsync(pageNumber: 1, pageSize: 100, actorId: actorId);
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

    private static ApiException CreateApiException(string message, int statusCode, string response = "")
    {
        return new ApiException(
            message,
            statusCode,
            response,
            new Dictionary<string, IEnumerable<string>>(),
            new InvalidOperationException(message));
    }

    #endregion
}
