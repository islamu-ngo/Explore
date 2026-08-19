// ABOUTME: Unit tests for event-session speaker assignment deletion.
// ABOUTME: Proves EventSession-scoped authorization metadata, ownership checks, and cache invalidation.

using Explore.Application.Authorization;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventSessionSpeakers.Handlers.Commands;
using Explore.Application.Features.EventSessionSpeakers.Requests.Commands;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionSpeakers.Commands;

public sealed class DeleteEventSessionSpeakerCommandHandlerTests
{
    private readonly IEventSessionSpeakerRepository _repository = Substitute.For<IEventSessionSpeakerRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly DeleteEventSessionSpeakerCommandHandler _handler;

    public DeleteEventSessionSpeakerCommandHandlerTests()
    {
        _handler = new DeleteEventSessionSpeakerCommandHandler(_repository, _eventSessionRepository, _cache);
    }

    [Test]
    public async Task Command_UsesEventSessionAsAuthorizationResource()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = new DeleteEventSessionSpeakerCommand
        {
            Id = Guid.NewGuid(),
            EventSessionId = sessionId,
            TenantId = tenantId,
            EventId = eventId
        };

        var secureRequest = (ISecureRequest)command;

        await Assert.That(secureRequest.ResourceId).IsEqualTo(sessionId.ToString());
        await Assert.That(secureRequest.AuthorizationFacts)
            .IsEqualTo(new EventScopedAuthorizationFacts(tenantId, eventId));
    }

    [Test]
    public async Task Handle_WithMatchingAssignment_DeletesAndInvalidatesEventCaches()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var speaker = CreateSpeakerAssignment(tenantId: tenantId, eventSessionId: sessionId);
        var command = CreateCommand(speaker.Id, sessionId, tenantId, eventId);
        using var cancellation = new CancellationTokenSource();

        _repository.GetById(speaker.Id).Returns(speaker);
        _eventSessionRepository.GetById(sessionId).Returns(CreateSession(sessionId, tenantId, eventId));

        var result = await _handler.Handle(command, cancellation.Token);

        await Assert.That(result).IsTrue();
        await _repository.Received(1).Delete(speaker);
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", cancellation.Token);
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), cancellation.Token);
    }

    [Test]
    public async Task Handle_WithMismatchedEventSession_DoesNotDelete()
    {
        var tenantId = Guid.NewGuid();
        var speaker = CreateSpeakerAssignment(tenantId: tenantId);

        _repository.GetById(speaker.Id).Returns(speaker);

        var result = await _handler.Handle(
            CreateCommand(speaker.Id, Guid.NewGuid(), tenantId, Guid.NewGuid()),
            CancellationToken.None);

        await Assert.That(result).IsFalse();
        await _repository.DidNotReceive().Delete(Arg.Any<EventSessionSpeaker>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithMismatchedTenant_DoesNotDelete()
    {
        var tenantId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var speaker = CreateSpeakerAssignment(tenantId: tenantId, eventSessionId: sessionId);

        _repository.GetById(speaker.Id).Returns(speaker);
        _eventSessionRepository.GetById(sessionId).Returns(CreateSession(sessionId, tenantId, Guid.NewGuid()));

        var result = await _handler.Handle(
            CreateCommand(speaker.Id, sessionId, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await Assert.That(result).IsFalse();
        await _repository.DidNotReceive().Delete(Arg.Any<EventSessionSpeaker>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static DeleteEventSessionSpeakerCommand CreateCommand(Guid id, Guid sessionId, Guid tenantId, Guid eventId) =>
        new()
        {
            Id = id,
            EventSessionId = sessionId,
            TenantId = tenantId,
            EventId = eventId
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
}
