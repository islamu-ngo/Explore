// ABOUTME: Unit tests for EventService.
// Tests all event-related operations including CRUD and session management.

using Explore.Blazor.Client.Helpers;
using Explore.Blazor.Client.Models.Events;

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
        _apiClient.GetEventManagementDetailsAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Unauthorized", 401));

        // Act
        var result = await _service.GetEventByIdAsync(eventId);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetEventByIdAsync_WhenPublicDetailHiddenAndManagementDetailAllowed_ReturnsManagementEvent()
    {
        var eventId = Guid.NewGuid();
        var expectedEvent = ComponentDataBuilder.EventDto.Generate();
        expectedEvent.Id = eventId;
        expectedEvent.Title = "Moderated management event";
        expectedEvent.EventStatusId = 6;
        expectedEvent.EventStatusFullName = "Moderated";
        expectedEvent.EventStatusMasterCode = "MODERATED";
        var managementResponse = CreateHalResourceResponse(expectedEvent);

        _apiClient.GetEventByIdAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));
        _apiClient.GetEventManagementDetailsAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(managementResponse);

        var result = await _service.GetEventByIdAsync(eventId);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(eventId);
        await Assert.That(result.Title).IsEqualTo("Moderated management event");
    }

    [Test]
    public async Task GetEventByIdAsync_WhenPublicDetailHiddenAndManagementDetailForbidden_ReturnsNull()
    {
        var eventId = Guid.NewGuid();

        _apiClient.GetEventByIdAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));
        _apiClient.GetEventManagementDetailsAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Forbidden", 403));

        var result = await _service.GetEventByIdAsync(eventId);

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

    [Test]
    public async Task CreateEventAsync_WithConcreteGeneratedClient_SendsIdempotencyKey()
    {
        // Arrange
        var createDto = ComponentDataBuilder.CreateEventRequest.Generate();
        var idempotencyKey = Guid.NewGuid().ToString("N");
        using var handler = new CapturingHandler("""
            {"id":"11111111-1111-1111-1111-111111111111","success":true,"message":"Event created successfully."}
            """);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        var service = new EventService(new EventApiClient(httpClient), _logger);

        // Act
        var result = await service.CreateEventAsync(createDto, idempotencyKey);

        // Assert
        await Assert.That(result?.Success).IsTrue();
        IEnumerable<string>? values = null;
        var hasIdempotencyHeader = handler.LastRequest?.Headers.TryGetValues("Idempotency-Key", out values) == true;
        await Assert.That(hasIdempotencyHeader).IsTrue();
        await Assert.That(values!.Single()).IsEqualTo(idempotencyKey);
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

    #region Moderation Tests

    [Test]
    public async Task ModerateEventLightAsync_SendsReasonMetadata()
    {
        var eventId = Guid.NewGuid();
        var expectedResponse = ComponentDataBuilder.SuccessResponse(eventId);

        _apiClient.ModerateEventLightAsync(
                Arg.Any<Guid>(),
                Arg.Any<EventModerationRequestDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var result = await _service.ModerateEventLightAsync(eventId, reasonCode: "policy_review", correlationId: "case-1");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await _apiClient.Received(1).ModerateEventLightAsync(
            eventId,
            Arg.Is<EventModerationRequestDto>(request =>
                request.ReasonCode == "policy_review" && request.CorrelationId == "case-1"),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ModerateEventHeavyAsync_SendsReasonMetadata()
    {
        var eventId = Guid.NewGuid();
        var expectedResponse = ComponentDataBuilder.SuccessResponse(eventId);

        _apiClient.ModerateEventHeavyAsync(
                Arg.Any<Guid>(),
                Arg.Any<EventModerationRequestDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var result = await _service.ModerateEventHeavyAsync(eventId, reasonCode: "illegal_image", correlationId: "case-heavy-1");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await _apiClient.Received(1).ModerateEventHeavyAsync(
            eventId,
            Arg.Is<EventModerationRequestDto>(request =>
                request.ReasonCode == "illegal_image" && request.CorrelationId == "case-heavy-1"),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UnmoderateEventAsync_SendsReasonMetadata()
    {
        var eventId = Guid.NewGuid();
        var expectedResponse = ComponentDataBuilder.SuccessResponse(eventId);

        _apiClient.UnmoderateEventAsync(
                Arg.Any<Guid>(),
                Arg.Any<EventModerationRequestDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var result = await _service.UnmoderateEventAsync(eventId, reasonCode: "appeal_approved", correlationId: "case-restore-1");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await _apiClient.Received(1).UnmoderateEventAsync(
            eventId,
            Arg.Is<EventModerationRequestDto>(request =>
                request.ReasonCode == "appeal_approved" && request.CorrelationId == "case-restore-1"),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSessionsByEventAsync Tests

    [Test]
    public async Task GetSessionsByEventAsync_WhenManagedSessionsNotRequested_UsesPublicSessionsOnly()
    {
        var eventId = Guid.NewGuid();
        var publicSession = new EventSessionListDto
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Title = "Public session",
            EventSessionStatusFullName = "Published"
        };

        _apiClient.GetEventSessionsAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateHalSessionCollectionResponse([publicSession]));

        var result = await _service.GetSessionsByEventAsync(eventId);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result.Single().Title).IsEqualTo("Public session");
        await _apiClient.Received(1).GetEventSessionsAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _apiClient.DidNotReceive().GetManagedEventSessionsByEventAsync(Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetSessionsByEventAsync_WhenManagedSessionsRequestedAndPublicRouteHidden_ReturnsManagedDraftSessions()
    {
        var eventId = Guid.NewGuid();
        var draftSession = new EventSessionListDto
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Title = "Internal draft session",
            EventSessionStatusFullName = "Draft",
            EventSessionStatusMasterCode = "DRAFT"
        };

        _apiClient.GetEventSessionsAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));
        _apiClient.GetManagedEventSessionsByEventAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateHalSessionCollectionResponse([draftSession]));

        var result = await _service.GetSessionsByEventAsync(eventId, includeManagedSessions: true);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result.Single().Title).IsEqualTo("Internal draft session");
        await Assert.That(result.Single().EventSessionStatusMasterCode).IsEqualTo("DRAFT");
    }

    [Test]
    public async Task GetSessionsByEventAsync_WhenManagedDuplicateExists_ReplacesPublicSession()
    {
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var publicSession = new EventSessionListDto
        {
            Id = sessionId,
            EventId = eventId,
            Title = "Public projection",
            EventSessionStatusFullName = "Published"
        };
        var managedSession = new EventSessionListDto
        {
            Id = sessionId,
            EventId = eventId,
            Title = "Managed projection",
            EventSessionStatusFullName = "Draft",
            EventSessionStatusMasterCode = "DRAFT"
        };
        var internalSession = new EventSessionListDto
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Title = "Internal only",
            EventSessionStatusFullName = "Draft"
        };

        _apiClient.GetEventSessionsAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateHalSessionCollectionResponse([publicSession]));
        _apiClient.GetManagedEventSessionsByEventAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateHalSessionCollectionResponse([managedSession, internalSession]));

        var result = await _service.GetSessionsByEventAsync(eventId, includeManagedSessions: true);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.Select(session => session.Title)).Contains("Managed projection");
        await Assert.That(result.Select(session => session.Title)).Contains("Internal only");
        await Assert.That(result.Select(session => session.Title)).DoesNotContain("Public projection");
    }

    [Test]
    public async Task GetSessionsByEventAsync_WhenManagedReadUnauthorized_ReturnsPublicSessions()
    {
        var eventId = Guid.NewGuid();
        var publicSession = new EventSessionListDto
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Title = "Public session"
        };

        _apiClient.GetEventSessionsAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateHalSessionCollectionResponse([publicSession]));
        _apiClient.GetManagedEventSessionsByEventAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Forbidden", 403));

        var result = await _service.GetSessionsByEventAsync(eventId, includeManagedSessions: true);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result.Single().Title).IsEqualTo("Public session");
    }

    #endregion

    #region UpdateEventAsync Tests

    [Test]
    public async Task UpdateEventAsync_ReturnsSuccess_WhenValid()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var updateDto = new EventDraftEditModel
        {
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            Title = "Updated Title"
        };
        var expectedResponse = ComponentDataBuilder.SuccessResponse(eventId);

        _apiClient.UpdateEventAsync(
                Arg.Any<Guid>(),
                Arg.Any<UpdateEventDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
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
        var updateDto = new EventDraftEditModel
        {
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            Title = "Updated Title"
        };
        _apiClient.UpdateEventAsync(
                Arg.Any<Guid>(),
                Arg.Any<UpdateEventDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
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
        var concurrencyStamp = Guid.NewGuid();
        var updateDto = new EventDraftEditModel
        {
            ExpectedConcurrencyStamp = concurrencyStamp,
            Title = "Test Title"
        };
        var expectedResponse = ComponentDataBuilder.SuccessResponse(eventId);
        UpdateEventDto? capturedUpdate = null;
        string? capturedIfMatch = null;
        _apiClient.UpdateEventAsync(
                Arg.Any<Guid>(),
                Arg.Do<UpdateEventDto>(dto => capturedUpdate = dto),
                Arg.Do<string?>(value => capturedIfMatch = value),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        await _service.UpdateEventAsync(eventId, updateDto);

        await _apiClient.Received(1).UpdateEventAsync(
            eventId,
            Arg.Any<UpdateEventDto>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
        await Assert.That(capturedUpdate).IsNotNull();
        await Assert.That(capturedUpdate!.Title?.Value).IsEqualTo("Test Title");
        await Assert.That(capturedUpdate.Subtitle?.Value.HasValue).IsTrue();
        await Assert.That(capturedUpdate.FeaturedImage?.Value.HasValue).IsTrue();
        await Assert.That(capturedIfMatch).IsEqualTo($"\"{concurrencyStamp:D}\"");
    }

    [Test]
    public async Task UpdateEventAsync_WhenApiReturnsConflictProblem_ReturnsStaleDraftFailure()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var updateDto = new EventDraftEditModel
        {
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            Title = "Updated Title"
        };
        var problemDetails = new ProblemDetails
        {
            Status = 409,
            Title = "Concurrency conflict",
            Detail = "The event draft changed since it was loaded. Refresh the event and try again."
        };

        _apiClient.UpdateEventAsync(
                Arg.Any<Guid>(),
                Arg.Any<UpdateEventDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiException<ProblemDetails>(
                "Conflict",
                409,
                string.Empty,
                new Dictionary<string, IEnumerable<string>>(),
                problemDetails,
                null));

        // Act
        var result = await _service.UpdateEventAsync(eventId, updateDto);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_concurrency_conflict");
        await Assert.That(result.Message).Contains("event draft changed");
    }

    #endregion

    #region Event Session Composer Request Tests

    [Test]
    public async Task CreateSessionAsync_MapsComposerRequestToGeneratedDto()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var request = new Explore.Blazor.Client.Clients.CreateEventSessionDto
        {
            EventId = eventId,
            TenantId = tenantId,
            Title = "Opening talk",
            Description = "Welcome session",
            Slug = "opening-talk",
            LocationId = locationId,
            RoomId = roomId,
            StartTime = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            MaxAudienceAttendees = 120,
            RegistrationModeId = 2,
            EventSessionKindId = 1,
            IslamicAspect = new EventSessionIslamicAspectDto
            {
                StartTimeType = (SessionStartTimeType)1,
                ReferencePrayer = (PrayerTime)2,
                OffsetMinutes = 10,
                RequiresWudu = true,
                RitualRequirementsJson = "{\"note\":\"Create\"}"
            }
        };
        _apiClient.CreateEventSessionAsync(
                Arg.Any<CreateEventSessionDto>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true, Id = sessionId });

        var result = await _service.CreateSessionAsync(request);

        await Assert.That(result.Id).IsEqualTo(sessionId);
        await _apiClient.Received(1).CreateEventSessionAsync(
            Arg.Is<CreateEventSessionDto>(dto =>
                dto.EventId == eventId
                && dto.TenantId == tenantId
                && dto.Title == "Opening talk"
                && dto.Description == "Welcome session"
                && dto.Slug == "opening-talk"
                && dto.LocationId == locationId
                && dto.RoomId == roomId
                && dto.StartTime == request.StartTime
                && dto.EndTime == request.EndTime
                && dto.MaxAudienceAttendees == 120
                && dto.RegistrationModeId == 2
                && dto.EventSessionKindId == 1
                && dto.IslamicAspect != null
                && dto.IslamicAspect.ReferencePrayer == (PrayerTime)2
                && dto.IslamicAspect.RitualRequirementsJson == "{\"note\":\"Create\"}"),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateSessionAsync_ForwardsGeneratedDtoWithConcurrencyHeader()
    {
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        var startTime = new DateTimeOffset(2026, 6, 1, 11, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var request = new UpdateEventSessionDto
        {
            Event = new UpdateEventSessionEventDto { EventId = eventId },
            Title = new UpdateEventSessionTitleDto
            {
                Value = new OptionalUpdateOfstring { HasValue = true, Value = "Updated workshop" }
            },
            Description = new UpdateEventSessionDescriptionDto
            {
                Value = new OptionalUpdateOfstring { HasValue = true, Value = "Updated description" }
            },
            Slug = new UpdateEventSessionSlugDto
            {
                Value = new OptionalUpdateOfstring { HasValue = true, Value = "updated-workshop" }
            },
            Location = new UpdateEventSessionLocationDto
            {
                Value = new OptionalUpdateOfGuid { HasValue = true, Value = locationId }
            },
            Room = new UpdateEventSessionRoomDto
            {
                Value = new OptionalUpdateOfGuid { HasValue = true, Value = roomId }
            },
            Schedule = new UpdateEventSessionScheduleDto
            {
                StartTime = new OptionalUpdateOfDateTimeOffset { HasValue = true, Value = startTime },
                EndTime = new OptionalUpdateOfDateTimeOffset { HasValue = true, Value = endTime }
            },
            MaxAudienceAttendees = new UpdateEventSessionMaxAudienceAttendeesDto
            {
                Value = new OptionalUpdateOfint { HasValue = true, Value = 80 }
            },
            RegistrationMode = new UpdateEventSessionRegistrationModeDto
            {
                Value = new OptionalUpdateOfint { HasValue = true, Value = 3 }
            },
            Kind = new UpdateEventSessionKindDto
            {
                Value = new OptionalUpdateOfint { HasValue = true, Value = 2 }
            },
            IslamicAspect = new UpdateEventSessionIslamicAspectUpdateDto
            {
                Value = new OptionalUpdateOfEventSessionIslamicAspectDto
                {
                    HasValue = true,
                    Value = new EventSessionIslamicAspectDto
                    {
                        StartTimeType = (SessionStartTimeType)2,
                        ReferencePrayer = (PrayerTime)3,
                        OffsetMinutes = 20,
                        RequiresWudu = false,
                        RitualRequirementsJson = "{\"note\":\"Update\"}"
                    }
                }
            }
        };
        UpdateEventSessionDto? capturedDto = null;
        string? capturedIfMatch = null;
        _apiClient.UpdateEventSessionAsync(
                Arg.Any<Guid>(),
                Arg.Do<UpdateEventSessionDto>(dto => capturedDto = dto),
                Arg.Do<string?>(value => capturedIfMatch = value),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new BaseCommandResponseOfGuid { Success = true, Id = sessionId });

        var result = await _service.UpdateSessionAsync(sessionId, concurrencyStamp, request);

        await Assert.That(result.Id).IsEqualTo(sessionId);
        await _apiClient.Received(1).UpdateEventSessionAsync(
            sessionId,
            Arg.Any<UpdateEventSessionDto>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
        await Assert.That(capturedIfMatch).IsEqualTo($"\"{concurrencyStamp:D}\"");
        await Assert.That(capturedDto).IsNotNull();
        await Assert.That(capturedDto!.Event?.EventId).IsEqualTo(eventId);
        await Assert.That(capturedDto.Title?.Value?.Value).IsEqualTo("Updated workshop");
        await Assert.That(capturedDto.Description?.Value?.Value).IsEqualTo("Updated description");
        await Assert.That(capturedDto.Slug?.Value?.Value).IsEqualTo("updated-workshop");
        await Assert.That(capturedDto.Location?.Value?.Value).IsEqualTo(locationId);
        await Assert.That(capturedDto.Room?.Value?.Value).IsEqualTo(roomId);
        await Assert.That(capturedDto.Schedule?.StartTime?.Value).IsEqualTo(startTime);
        await Assert.That(capturedDto.Schedule?.EndTime?.Value).IsEqualTo(endTime);
        await Assert.That(capturedDto.MaxAudienceAttendees?.Value?.Value).IsEqualTo(80);
        await Assert.That(capturedDto.RegistrationMode?.Value?.Value).IsEqualTo(3);
        await Assert.That(capturedDto.Kind?.Value?.Value).IsEqualTo(2);
        await Assert.That(capturedDto.IslamicAspect?.Value?.Value?.ReferencePrayer).IsEqualTo((PrayerTime)3);
        await Assert.That(capturedDto.IslamicAspect?.Value?.Value?.RitualRequirementsJson).IsEqualTo("{\"note\":\"Update\"}");
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
    public async Task GetManagedSessionByIdAsync_WhenPublicDetailHidden_ReturnsManagedSession()
    {
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var draftSession = new EventSessionListDto
        {
            Id = sessionId,
            EventId = eventId,
            EventTitle = "Managed Event",
            Title = "Internal draft session",
            EventSessionStatusFullName = "Draft",
            EventSessionStatusMasterCode = "DRAFT",
            ConcurrencyStamp = Guid.NewGuid(),
            AdditionalProperties = CreateHalLinks("publish", "archive")
        };

        _apiClient.GetEventSessionByIdAsync(sessionId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(CreateApiException("Not Found", 404));
        _apiClient.GetManagedEventSessionsByEventAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateHalSessionCollectionResponse([draftSession]));

        var result = await _service.GetManagedSessionByIdAsync(eventId, sessionId);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(sessionId);
        await Assert.That(result.EventId).IsEqualTo(eventId);
        await Assert.That(result.Title).IsEqualTo("Internal draft session");
        await Assert.That(result.EventSessionStatusMasterCode).IsEqualTo("DRAFT");
        await Assert.That(result.HasHalLink("publish")).IsTrue();
        await Assert.That(result.HasHalLink("archive")).IsTrue();
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
        await _apiClient.Received(1).GetEventsAsync(pageNumber: 1, pageSize: 100, actorId: actorId, view: "All");
    }

    [Test]
    public async Task GetProfileEventsByActorAsync_MergesManagedEventsAndUsesManagedDuplicate()
    {
        var actorId = Guid.NewGuid();
        var sharedEventId = Guid.NewGuid();
        var publicEvent = new EventListDto { Id = sharedEventId, ActorId = actorId, Title = "Public version" };
        var managedDuplicate = new EventListDto { Id = sharedEventId, ActorId = actorId, Title = "Managed version" };
        var moderatedEvent = new EventListDto { Id = Guid.NewGuid(), ActorId = actorId, Title = "Moderated", EventStatusId = 6 };

        _apiClient.GetEventsAsync().ReturnsForAnyArgs(CreateHalCollectionResponse(new List<EventListDto> { publicEvent }));
        _apiClient.GetManagedEventsByActorAsync(
                actorId: actorId,
                pageNumber: 1,
                pageSize: 100)
            .Returns(CreateHalCollectionResponse(new List<EventListDto> { managedDuplicate, moderatedEvent }));

        var result = await _service.GetProfileEventsByActorAsync(actorId);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.Select(evt => evt.Title)).Contains("Managed version");
        await Assert.That(result.Select(evt => evt.Title)).Contains("Moderated");
        await Assert.That(result.Select(evt => evt.Title)).DoesNotContain("Public version");
        await _apiClient.Received(1).GetEventsAsync(pageNumber: 1, pageSize: 100, actorId: actorId, view: "All");
        await _apiClient.Received(1).GetManagedEventsByActorAsync(
            actorId: actorId,
            pageNumber: 1,
            pageSize: 100);
    }

    [Test]
    public async Task GetRegistrationEventsByUserAsync_GroupsRegistrationsAndEnrichesEventCards()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var firstSessionId = Guid.NewGuid();
        var secondSessionId = Guid.NewGuid();
        var firstSessionDate = DateTimeOffset.UtcNow.AddDays(7);

        _apiClient.GetRegistrationsByUserAsync(userId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
            [
                new EventRegistrationListDto
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    EventSessionId = firstSessionId,
                    EventTitle = "Fallback event title",
                    EventStartTime = firstSessionDate
                },
                new EventRegistrationListDto
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    EventSessionId = secondSessionId,
                    EventTitle = "Fallback event title",
                    EventStartTime = firstSessionDate.AddHours(1)
                }
            ]);

        _apiClient.GetEventByIdAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(CreateHalResourceResponse(new EventDto
            {
                Id = eventId,
                Title = "Annual Conference",
                EventTypeId = 1,
                EventTypeFullName = "Conference",
                FeaturedImageUri = "https://example.test/event.png",
                FirstSessionDate = firstSessionDate,
                LastSessionDate = firstSessionDate.AddHours(2)
            }));

        var result = await _service.GetRegistrationEventsByUserAsync(userId);

        await Assert.That(result.Count).IsEqualTo(1);
        var registrationEvent = result.Single();
        await Assert.That(registrationEvent.Id).IsEqualTo(eventId);
        await Assert.That(registrationEvent.Title).IsEqualTo("Annual Conference");
        await Assert.That(registrationEvent.FeaturedImageUri).IsEqualTo("https://example.test/event.png");
        await Assert.That(registrationEvent.IsPast).IsFalse();
        await _apiClient.Received(1).GetEventByIdAsync(eventId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetRegistrationEventsByActorAsync_UsesUserActorRegistrations()
    {
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _apiClient.GetActorByIdAsync(actorId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new HalResourceOfActorDto
            {
                Id = actorId,
                UserId = userId
            });
        _apiClient.GetRegistrationsByUserAsync(userId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _service.GetRegistrationEventsByActorAsync(actorId);

        await Assert.That(result).IsEmpty();
        await _apiClient.Received(1).GetActorByIdAsync(actorId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _apiClient.Received(1).GetRegistrationsByUserAsync(userId, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
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
                Items = items.Select(ToHalResource).ToList()
            }
        };
    }

    private static HalResourceOfEventListDto ToHalResource(EventListDto item)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(item);
        return System.Text.Json.JsonSerializer.Deserialize<HalResourceOfEventListDto>(json)
               ?? new HalResourceOfEventListDto();
    }

    private static HalCollectionResourceOfEventSessionListDto CreateHalSessionCollectionResponse(
        IList<EventSessionListDto> items)
    {
        return new HalCollectionResourceOfEventSessionListDto
        {
            _embedded = new HalCollectionEmbeddedOfEventSessionListDto
            {
                Items = items.Select(ToHalSessionResource).ToList()
            }
        };
    }

    private static HalResourceOfEventSessionListDto ToHalSessionResource(EventSessionListDto item)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(item);
        return System.Text.Json.JsonSerializer.Deserialize<HalResourceOfEventSessionListDto>(json)
               ?? new HalResourceOfEventSessionListDto();
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

    private static Dictionary<string, object> CreateHalLinks(params string[] linkRels)
    {
        var links = string.Join(
            ',',
            linkRels.Select(rel => $"\"{rel}\":{{\"href\":\"/api/eventsession/{rel}\",\"method\":\"POST\"}}"));
        using var doc = System.Text.Json.JsonDocument.Parse($"{{\"_links\":{{{links}}}}}");

        return new Dictionary<string, object>
        {
            ["_links"] = doc.RootElement.GetProperty("_links").Clone()
        };
    }

    private sealed class CapturingHandler(string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.Created)
            {
                Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    #endregion
}
