// ABOUTME: Unit tests for tenant-scoped AT Protocol delivery state on the current user's event list.
// ABOUTME: Verifies stable failure-code mapping, published fallback, and one bounded outbox batch read.

using AutoMapper;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Domain.Federation;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Queries;

public sealed class GetMyEventsRequestHandlerTests
{
    [Test]
    public async Task Handle_MapsCurrentDeliveryStateAndPublishedFallbackFromOneTenantBatch()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid failedEventId = Guid.CreateVersion7();
        Guid publishedEventId = Guid.CreateVersion7();
        var failedEvent = EventDto(failedEventId);
        var publishedEvent = EventDto(publishedEventId);
        publishedEvent = publishedEvent with { AtprotoRecordId = Guid.CreateVersion7() };
        var events = new List<Explore.Domain.Event>();
        var delivery = Delivery(failedEventId, PdsSyncStatus.DeadLettered, "session_unavailable");
        IEventRepository eventRepository = Substitute.For<IEventRepository>();
        IMapper mapper = Substitute.For<IMapper>();
        IObjectStorageService objectStorage = Substitute.For<IObjectStorageService>();
        ILogger<GetMyEventsRequestHandler> logger = Substitute.For<ILogger<GetMyEventsRequestHandler>>();
        IPdsSyncOutboxRepository outboxRepository = Substitute.For<IPdsSyncOutboxRepository>();
        ITenantContext tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        eventRepository.GetMyEventsWithDetailsPaged("user-42", 2, 20).Returns((events, 2));
        mapper.Map<List<EventListDto>>(events).Returns([failedEvent, publishedEvent]);
        outboxRepository.GetCurrentEventDeliveryStatesAsync(
                tenantId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns([delivery]);
        var handler = new GetMyEventsRequestHandler(
            eventRepository,
            mapper,
            objectStorage,
            logger,
            outboxRepository,
            tenantContext);

        var result = await handler.Handle(
            new GetMyEventsRequest { UserId = "user-42", PageNumber = 2, PageSize = 20 },
            CancellationToken.None);

        await Assert.That(result.Items[0].AtprotoDeliveryStatus).IsEqualTo("failed");
        await Assert.That(result.Items[0].AtprotoDeliveryFailureCode).IsEqualTo("session_unavailable");
        await Assert.That(result.Items[1].AtprotoDeliveryStatus).IsEqualTo("published");
        await Assert.That(result.Items[1].AtprotoDeliveryFailureCode).IsNull();
        await outboxRepository.Received(1).GetCurrentEventDeliveryStatesAsync(
            tenantId,
            Arg.Is<IReadOnlyCollection<Guid>>(ids =>
                ids.Count == 2 && ids.Contains(failedEventId) && ids.Contains(publishedEventId)),
            CancellationToken.None);
    }

    private static EventListDto EventDto(Guid eventId) => new()
    {
        Id = eventId,
        Title = "AT Protocol event",
        EventTypeFullName = "Conference",
        AudienceGenderFullName = "Everyone",
        AudienceAgeFullName = "All ages",
        ActorDisplayName = "Owner",
        ActorTypeFullName = "User",
        EventStatusFullName = "Published",
        VisibilityTypeFullName = "Public",
        EventFormatFullName = "Digital"
    };

    private static PdsSyncOutbox Delivery(Guid eventId, PdsSyncStatus status, string failureCode) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        UserId = Guid.CreateVersion7(),
        Did = "did:plc:delivery-owner",
        Collection = "community.lexicon.calendar.event",
        RecordKey = "3m7delivery",
        PayloadHash = new string('a', 64),
        IdempotencyKey = $"event:{eventId:N}:create",
        PdsHost = "https://pds.example",
        SourceEntityType = "Event",
        SourceEntityId = eventId,
        SourceVersion = Guid.CreateVersion7(),
        Status = status,
        LastError = failureCode,
        CreatedAt = DateTime.UtcNow,
        MaxRetries = 3
    };
}
