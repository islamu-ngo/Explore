using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Events.Handlers.Queries;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Queries;

public class GetEventPublishReadinessRequestHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly GetEventPublishReadinessRequestHandler _handler;

    public GetEventPublishReadinessRequestHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _handler = new GetEventPublishReadinessRequestHandler(_eventRepository);
    }

    [Test]
    public async Task Handle_WhenEventIsReady_ReturnsReadyResult()
    {
        var @event = CreateReadyEvent();
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new GetEventPublishReadinessRequest { Id = @event.Id }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.EventId).IsEqualTo(@event.Id);
        await Assert.That(result.IsReady).IsTrue();
        await Assert.That(result.Errors.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_WhenEventIsMissingSchedule_ReturnsMachineReadableError()
    {
        var @event = CreateReadyEvent();
        @event.FirstSessionStartUtc = null;
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new GetEventPublishReadinessRequest { Id = @event.Id }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsReady).IsFalse();
        await Assert.That(result.Errors.Count).IsEqualTo(1);
        await Assert.That(result.Errors[0].Code).IsEqualTo("schedule_session_required");
        await Assert.That(result.Errors[0].FieldPath).IsEqualTo("schedule.sessions");
        await Assert.That(result.Errors[0].Severity).IsEqualTo("error");
    }

    [Test]
    public async Task Handle_WhenEventDoesNotExist_ReturnsNull()
    {
        var eventId = Guid.NewGuid();
        _eventRepository.GetById(eventId).Returns((Explore.Domain.Event?)null);

        var result = await _handler.Handle(new GetEventPublishReadinessRequest { Id = eventId }, CancellationToken.None);

        await Assert.That(result).IsNull();
    }

    private static Explore.Domain.Event CreateReadyEvent() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Ready Event",
        Actor = new Actor
        {
            ActorType = new ActorType { Id = 1, FullName = "User", MasterCode = "user" },
            Tenant = CreateTenant(),
            Pii = new ActorPii { DisplayName = "Publisher" }
        },
        TenantId = Guid.NewGuid(),
        Tenant = CreateTenant(),
        VisibilityTypeId = 1,
        VisibilityType = new VisibilityType { Id = 1, FullName = "Public", MasterCode = "public" },
        EventStatusId = (int)EventStatusEnum.Draft,
        EventStatus = new EventStatus { Id = (int)EventStatusEnum.Draft, FullName = "Draft", MasterCode = "draft" },
        EventFormatId = 1,
        EventFormat = new EventFormat { Id = 1, FullName = "In person", MasterCode = "in_person" },
        FirstSessionStartUtc = DateTimeOffset.UtcNow.AddDays(1),
        LastSessionStartUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(2)
    };

    private static Tenant CreateTenant() => new()
    {
        FullName = "Test Tenant",
        Slug = "test",
        TenantStatus = null!
    };
}
