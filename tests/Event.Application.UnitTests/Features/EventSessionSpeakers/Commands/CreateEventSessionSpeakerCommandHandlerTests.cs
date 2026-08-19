// ABOUTME: Unit tests for event-session speaker assignment creation.
// ABOUTME: Proves tenant ownership, duplicate detection, auth metadata, and cache invalidation.

using AutoMapper;
using Explore.Application.Authorization;
using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionSpeaker;
using Explore.Application.Features.EventSessionSpeakers.Handlers.Commands;
using Explore.Application.Features.EventSessionSpeakers.Requests.Commands;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionSpeakers.Commands;

public sealed class CreateEventSessionSpeakerCommandHandlerTests
{
    private readonly IEventSessionSpeakerRepository _repository = Substitute.For<IEventSessionSpeakerRepository>();
    private readonly IActorRepository _actorRepository = Substitute.For<IActorRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly CreateEventSessionSpeakerCommandHandler _handler;

    public CreateEventSessionSpeakerCommandHandlerTests()
    {
        _handler = new CreateEventSessionSpeakerCommandHandler(
            _repository,
            _actorRepository,
            _eventSessionRepository,
            _tenantContext,
            _mapper,
            _cache);
    }

    [Test]
    public async Task Command_UsesEventSessionAsAuthorizationResource()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new CreateEventSessionSpeakerCommand
        {
            TenantId = tenantId,
            EventId = eventId,
            SpeakerDto = new CreateEventSessionSpeakerDto
            {
                EventSessionId = sessionId,
                ActorId = Guid.NewGuid()
            }
        };

        var secureRequest = (ISecureRequest)command;

        await Assert.That(secureRequest.ResourceId).IsEqualTo(sessionId.ToString());
        await Assert.That(secureRequest.AuthorizationFacts)
            .IsEqualTo(new EventScopedAuthorizationFacts(tenantId, eventId));
    }

    [Test]
    public async Task Handle_WithValidAssignment_CreatesWithSessionTenantAndInvalidatesEventCaches()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var command = CreateCommand(sessionId, actorId, tenantId, eventId);
        var session = CreateSession(sessionId, tenantId, eventId);
        var actor = CreateActor(actorId, tenantId);
        var mapped = CreateSpeakerAssignment(tenantId: Guid.NewGuid(), eventSessionId: sessionId, actorId: actorId);
        var created = CreateSpeakerAssignment(id: Guid.NewGuid(), tenantId: tenantId, eventSessionId: sessionId, actorId: actorId);
        using var cancellation = new CancellationTokenSource();

        _tenantContext.TenantId.Returns(tenantId);
        _actorRepository.Exists(actorId).Returns(true);
        _eventSessionRepository.Exists(sessionId).Returns(true);
        _eventSessionRepository.GetById(sessionId).Returns(session);
        _actorRepository.GetById(actorId).Returns(actor);
        _repository.GetBySessionAndActor(sessionId, actorId, cancellationToken: cancellation.Token).Returns((EventSessionSpeaker?)null);
        _mapper.Map<EventSessionSpeaker>(command.SpeakerDto).Returns(mapped);
        _repository.Create(mapped).Returns(created);

        var result = await _handler.Handle(command, cancellation.Token);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(created.Id);
        await Assert.That(mapped.TenantId).IsEqualTo(tenantId);
        await _repository.Received(1).Create(mapped);
        await _repository.Received(1).GetBySessionAndActor(sessionId, actorId, cancellationToken: cancellation.Token);
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", cancellation.Token);
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), cancellation.Token);
    }

    [Test]
    public async Task Handle_WithGlobalActor_CreatesAssignmentInSessionTenant()
    {
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var command = CreateCommand(sessionId, actorId, tenantId, eventId);
        var mapped = CreateSpeakerAssignment(tenantId: Guid.NewGuid(), eventSessionId: sessionId, actorId: actorId);
        var created = CreateSpeakerAssignment(tenantId: tenantId, eventSessionId: sessionId, actorId: actorId);

        _tenantContext.TenantId.Returns(tenantId);
        _actorRepository.Exists(actorId).Returns(true);
        _eventSessionRepository.Exists(sessionId).Returns(true);
        _eventSessionRepository.GetById(sessionId).Returns(CreateSession(sessionId, tenantId, eventId));
        _actorRepository.GetById(actorId).Returns(CreateActor(actorId, Guid.NewGuid()));
        _repository.GetBySessionAndActor(sessionId, actorId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns((EventSessionSpeaker?)null);
        _mapper.Map<EventSessionSpeaker>(command.SpeakerDto).Returns(mapped);
        _repository.Create(mapped).Returns(created);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(mapped.TenantId).IsEqualTo(tenantId);
        await _repository.Received(1).Create(mapped);
    }

    [Test]
    public async Task Handle_WithDuplicateAssignment_ReturnsFailureWithoutCreating()
    {
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        _tenantContext.TenantId.Returns(tenantId);
        _actorRepository.Exists(actorId).Returns(true);
        _eventSessionRepository.Exists(sessionId).Returns(true);
        _eventSessionRepository.GetById(sessionId).Returns(CreateSession(sessionId, tenantId, Guid.NewGuid()));
        _actorRepository.GetById(actorId).Returns(CreateActor(actorId, tenantId));
        _repository.GetBySessionAndActor(sessionId, actorId, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(CreateSpeakerAssignment(tenantId: tenantId, eventSessionId: sessionId, actorId: actorId));

        var result = await _handler.Handle(CreateCommand(sessionId, actorId, tenantId, Guid.NewGuid()), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Actor is already assigned as a speaker for this event session.");
        await _repository.DidNotReceive().Create(Arg.Any<EventSessionSpeaker>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static CreateEventSessionSpeakerCommand CreateCommand(Guid sessionId, Guid actorId, Guid tenantId, Guid eventId) =>
        new()
        {
            TenantId = tenantId,
            EventId = eventId,
            SpeakerDto = new CreateEventSessionSpeakerDto
            {
                EventSessionId = sessionId,
                ActorId = actorId
            }
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
