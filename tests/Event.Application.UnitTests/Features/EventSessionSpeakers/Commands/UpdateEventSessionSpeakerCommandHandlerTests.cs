// ABOUTME: Unit tests for grouped event-session speaker link command handling.
// ABOUTME: Covers validation, concurrency, duplicate checks, one-save updates, and cache invalidation.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventSessionSpeakers.Handlers.Commands;
using Explore.Application.Features.EventSessionSpeakers.Requests.Commands;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionSpeakers.Commands;

public sealed class UpdateEventSessionSpeakerCommandHandlerTests
{
    private readonly IEventSessionSpeakerRepository _repository = Substitute.For<IEventSessionSpeakerRepository>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateEventSessionSpeakerCommandHandler _handler;

    public UpdateEventSessionSpeakerCommandHandlerTests()
    {
        _handler = new UpdateEventSessionSpeakerCommandHandler(
            _repository,
            _actorRepository,
            _eventSessionRepository,
            _cache);
    }

    [Test]
    public async Task Handle_WithEmptyWrapper_ReturnsFailedResponseWithoutSaving()
    {
        var command = new UpdateEventSessionSpeakerCommand
        {
            EventSessionSpeakerId = Guid.NewGuid(),
            EventSessionId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            SpeakerDto = new UpdateEventSessionSpeakerDto()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("At least one event session speaker update group must be provided.");
        await _repository.DidNotReceive().Update(Arg.Any<EventSessionSpeaker>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithStaleConcurrencyStamp_ThrowsConflictWithoutSaving()
    {
        var entity = CreateSpeakerAssignment();
        entity.ConcurrencyStamp = Guid.NewGuid();
        _repository.GetById(entity.Id).Returns(entity);

        var command = CreateCommand(entity, new UpdateEventSessionSpeakerDto
        {
            Actor = new UpdateEventSessionSpeakerActorDto { ActorId = Guid.NewGuid() }
        });
        command.ExpectedConcurrencyStamp = Guid.NewGuid();

        await Assert.That(async () => await _handler.Handle(command, CancellationToken.None))
            .Throws<ConcurrencyConflictException>();
        await _repository.DidNotReceive().Update(Arg.Any<EventSessionSpeaker>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithActorChange_SavesOnceAndInvalidatesEventCaches()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var newActorId = Guid.NewGuid();
        var entity = CreateSpeakerAssignment(tenantId: tenantId, eventSessionId: sessionId, actorId: actorId);
        _repository.GetById(entity.Id).Returns(entity);
        _eventSessionRepository.GetById(sessionId).Returns(CreateSession(sessionId, tenantId, eventId));
        _actorRepository.GetById(newActorId).Returns(CreateActor(newActorId, tenantId));
        _repository.GetBySessionAndActor(sessionId, newActorId, entity.Id).Returns((EventSessionSpeaker?)null);

        var result = await _handler.Handle(CreateCommand(entity, new UpdateEventSessionSpeakerDto
        {
            Actor = new UpdateEventSessionSpeakerActorDto { ActorId = newActorId }
        }, eventId), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(entity.ActorId).IsEqualTo(newActorId);
        await _repository.Received(1).Update(entity);
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithDuplicateSessionSpeaker_ReturnsValidationFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var newActorId = Guid.NewGuid();
        var entity = CreateSpeakerAssignment(tenantId: tenantId, eventSessionId: sessionId);
        _repository.GetById(entity.Id).Returns(entity);
        _eventSessionRepository.GetById(sessionId).Returns(CreateSession(sessionId, tenantId, eventId));
        _actorRepository.GetById(newActorId).Returns(CreateActor(newActorId, tenantId));
        _repository.GetBySessionAndActor(sessionId, newActorId, entity.Id).Returns(CreateSpeakerAssignment(tenantId: tenantId, eventSessionId: sessionId, actorId: newActorId));

        var result = await _handler.Handle(CreateCommand(entity, new UpdateEventSessionSpeakerDto
        {
            Actor = new UpdateEventSessionSpeakerActorDto { ActorId = newActorId }
        }, eventId), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Actor is already assigned as a speaker for this event session.");
        await _repository.DidNotReceive().Update(Arg.Any<EventSessionSpeaker>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithRouteSessionMismatch_UsesPersistedSessionContext()
    {
        var entity = CreateSpeakerAssignment();
        var eventId = Guid.NewGuid();
        var command = CreateCommand(entity, new UpdateEventSessionSpeakerDto
        {
            Actor = new UpdateEventSessionSpeakerActorDto { ActorId = Guid.NewGuid() }
        });
        command.EventSessionId = Guid.NewGuid();
        _repository.GetById(entity.Id).Returns(entity);
        _eventSessionRepository.GetById(entity.EventSessionId)
            .Returns(CreateSession(entity.EventSessionId, entity.TenantId, eventId));
        _actorRepository.GetById(command.SpeakerDto.Actor.ActorId).Returns(CreateActor(command.SpeakerDto.Actor.ActorId, entity.TenantId));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(command.EventSessionId).IsEqualTo(entity.EventSessionId);
        await Assert.That(command.EventId).IsEqualTo(eventId);
        await Assert.That(command.TenantId).IsEqualTo(entity.TenantId);
        await _repository.Received(1).Update(entity);
    }

    [Test]
    public async Task Handle_WithTargetSessionFromAnotherEvent_ReturnsValidationFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();
        var targetSessionId = Guid.NewGuid();
        var entity = CreateSpeakerAssignment(tenantId: tenantId, eventSessionId: currentSessionId);
        _repository.GetById(entity.Id).Returns(entity);
        _eventSessionRepository.GetById(currentSessionId)
            .Returns(CreateSession(currentSessionId, tenantId, eventId));
        _eventSessionRepository.GetById(targetSessionId)
            .Returns(CreateSession(targetSessionId, tenantId, Guid.NewGuid()));

        var result = await _handler.Handle(CreateCommand(entity, new UpdateEventSessionSpeakerDto
        {
            Session = new UpdateEventSessionSpeakerSessionDto { EventSessionId = targetSessionId }
        }, eventId), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors)
            .Contains("Event session must belong to the same event as the speaker assignment.");
        await _repository.DidNotReceive().Update(Arg.Any<EventSessionSpeaker>());
        await _repository.DidNotReceiveWithAnyArgs().GetBySessionAndActor(default, default, default, default);
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithActorChange_ForwardsCancellationTokenToDuplicateCheck()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var newActorId = Guid.NewGuid();
        var entity = CreateSpeakerAssignment(tenantId: tenantId, eventSessionId: sessionId);
        using var cancellation = new CancellationTokenSource();
        _repository.GetById(entity.Id).Returns(entity);
        _eventSessionRepository.GetById(sessionId).Returns(CreateSession(sessionId, tenantId, eventId));
        _actorRepository.GetById(newActorId).Returns(CreateActor(newActorId, tenantId));
        _repository.GetBySessionAndActor(sessionId, newActorId, entity.Id, cancellation.Token)
            .Returns((EventSessionSpeaker?)null);

        await _handler.Handle(CreateCommand(entity, new UpdateEventSessionSpeakerDto
        {
            Actor = new UpdateEventSessionSpeakerActorDto { ActorId = newActorId }
        }, eventId), cancellation.Token);

        await _repository.Received(1)
            .GetBySessionAndActor(sessionId, newActorId, entity.Id, cancellation.Token);
    }

    private static UpdateEventSessionSpeakerCommand CreateCommand(
        EventSessionSpeaker entity,
        UpdateEventSessionSpeakerDto dto,
        Guid? eventId = null) =>
        new()
        {
            EventSessionSpeakerId = entity.Id,
            EventSessionId = entity.EventSessionId,
            EventId = eventId ?? Guid.NewGuid(),
            TenantId = entity.TenantId,
            ExpectedConcurrencyStamp = entity.ConcurrencyStamp,
            SpeakerDto = dto
        };

    private static EventSessionSpeaker CreateSpeakerAssignment(
        Guid? id = null,
        Guid? tenantId = null,
        Guid? eventSessionId = null,
        Guid? actorId = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            Tenant = null!,
            EventSessionId = eventSessionId ?? Guid.NewGuid(),
            EventSession = null!,
            ActorId = actorId ?? Guid.NewGuid(),
            Actor = null!
        };

    private static EventSession CreateSession(Guid sessionId, Guid tenantId, Guid eventId) =>
        new()
        {
            Id = sessionId,
            TenantId = tenantId,
            Tenant = null!,
            EventId = eventId,
            Event = null!
        };

    private static Actor CreateActor(Guid id, Guid tenantId) =>
        new()
        {
            Id = id,
            ActorType = null!,
            Pii = null!
        };
}
