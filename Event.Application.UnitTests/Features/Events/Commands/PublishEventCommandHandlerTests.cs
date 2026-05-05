using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Events.Commands;

public class PublishEventCommandHandlerTests
{
    private readonly IEventRepository _eventRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;
    private readonly PublishEventCommandHandler _handler;

    public PublishEventCommandHandlerTests()
    {
        _eventRepository = Substitute.For<IEventRepository>();
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _cache = Substitute.For<HybridCache>();

        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<Explore.Application.Responses.BaseCommandResponse<Guid>>>>();
                return operation(CancellationToken.None);
            });

        _outboxRepository.Create(Arg.Any<OutboxMessage>())
            .Returns(callInfo => callInfo.Arg<OutboxMessage>());

        _handler = new PublishEventCommandHandler(_eventRepository, _outboxRepository, _unitOfWork, _cache);
    }

    [Test]
    public async Task Handle_WhenDraftEventIsReady_PublishesAndCreatesOutboxMessage()
    {
        var concurrencyStamp = Guid.NewGuid();
        var @event = CreateReadyEvent(concurrencyStamp);
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(@event.EventStatusId).IsEqualTo((int)EventStatusEnum.Published);
        await _eventRepository.Received(1).Update(@event);
        await _outboxRepository.Received(1).Create(Arg.Is<OutboxMessage>(message =>
            message.AggregateType == "Event"
            && message.AggregateId == @event.Id
            && message.EventType == "EventPublished"
            && message.Status == OutboxMessageStatus.Pending
            && message.Payload != null));
    }

    [Test]
    public async Task Handle_WhenConcurrencyStampDoesNotMatch_ReturnsConflictFailure()
    {
        var @event = CreateReadyEvent(Guid.NewGuid());
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = Guid.NewGuid() }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_publish_concurrency_conflict");
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
        await _outboxRepository.DidNotReceive().Create(Arg.Any<OutboxMessage>());
    }

    [Test]
    public async Task Handle_WhenEventIsMissingSchedule_ReturnsReadinessFailure()
    {
        var concurrencyStamp = Guid.NewGuid();
        var @event = CreateReadyEvent(concurrencyStamp);
        @event.FirstSessionStartUtc = null;
        _eventRepository.GetById(@event.Id).Returns(@event);

        var result = await _handler.Handle(new PublishEventCommand
        {
            Id = @event.Id,
            Request = new() { ExpectedConcurrencyStamp = concurrencyStamp }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_publish_readiness_failed");
        await Assert.That(result.Errors).IsNotNull();
        await Assert.That(result.Errors![0]).Contains("scheduled session");
        await _eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
    }

    private static Explore.Domain.Event CreateReadyEvent(Guid concurrencyStamp) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Draft Event",
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
        LastSessionStartUtc = DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
        ConcurrencyStamp = concurrencyStamp
    };

    private static Tenant CreateTenant() => new()
    {
        FullName = "Test Tenant",
        Slug = "test",
        TenantStatus = null!
    };
}
