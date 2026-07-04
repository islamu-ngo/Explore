// ABOUTME: Unit tests for grouped event-session language PATCH command handling.
// ABOUTME: Covers validation, concurrency, duplicate checks, one-save updates, and cache invalidation.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionLanguage;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventSessionLanguages.Handlers.Commands;
using Explore.Application.Features.EventSessionLanguages.Requests.Commands;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventSessionLanguages.Commands;

public sealed class UpdateEventSessionLanguageCommandHandlerTests
{
    private readonly IEventSessionLanguageRepository _repository = Substitute.For<IEventSessionLanguageRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly ILanguageRepository _languageRepository = Substitute.For<ILanguageRepository>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateEventSessionLanguageCommandHandler _handler;

    public UpdateEventSessionLanguageCommandHandlerTests()
    {
        _handler = new UpdateEventSessionLanguageCommandHandler(
            _repository,
            _eventSessionRepository,
            _languageRepository,
            _cache);
    }

    [Test]
    public async Task Handle_WithEmptyWrapper_ReturnsFailedResponseWithoutSaving()
    {
        var command = new UpdateEventSessionLanguageCommand
        {
            EventSessionLanguageId = 4,
            EventSessionId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventSessionLanguageDto = new UpdateEventSessionLanguageDto()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("At least one event session language update group must be provided.");
        await _repository.DidNotReceive().Update(Arg.Any<EventSessionLanguage>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithStaleConcurrencyStamp_ThrowsConflictWithoutSaving()
    {
        var entity = CreateLanguageAssignment();
        entity.ConcurrencyStamp = Guid.NewGuid();
        _repository.GetById(entity.Id).Returns(entity);

        var command = CreateCommand(entity, new UpdateEventSessionLanguageDto
        {
            Language = new UpdateEventSessionLanguageLanguageDto { LanguageId = 2 }
        });
        command.ExpectedConcurrencyStamp = Guid.NewGuid();

        await Assert.That(async () => await _handler.Handle(command, CancellationToken.None))
            .Throws<ConcurrencyConflictException>();
        await _repository.DidNotReceive().Update(Arg.Any<EventSessionLanguage>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithLanguageChange_SavesOnceAndInvalidatesEventCaches()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var entity = CreateLanguageAssignment(tenantId: tenantId, eventSessionId: sessionId, languageId: 1);
        var session = CreateSession(sessionId, tenantId, eventId);
        _repository.GetById(entity.Id).Returns(entity);
        _eventSessionRepository.GetById(sessionId).Returns(session);
        _languageRepository.Exists(2).Returns(true);
        _repository.GetBySessionAndLanguage(sessionId, 2, entity.Id, cancellation.Token).Returns((EventSessionLanguage?)null);

        var result = await _handler.Handle(CreateCommand(entity, new UpdateEventSessionLanguageDto
        {
            Language = new UpdateEventSessionLanguageLanguageDto { LanguageId = 2 }
        }), cancellation.Token);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(entity.LanguageId).IsEqualTo(2);
        await _repository.Received(1).GetBySessionAndLanguage(sessionId, 2, entity.Id, cancellation.Token);
        await _repository.Received(1).Update(entity);
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithDuplicateSessionLanguage_ReturnsValidationFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var entity = CreateLanguageAssignment(tenantId: tenantId, eventSessionId: sessionId, languageId: 1);
        _repository.GetById(entity.Id).Returns(entity);
        _eventSessionRepository.GetById(sessionId).Returns(CreateSession(sessionId, tenantId, eventId));
        _languageRepository.Exists(2).Returns(true);
        _repository.GetBySessionAndLanguage(sessionId, 2, entity.Id).Returns(CreateLanguageAssignment(id: 99, tenantId: tenantId, eventSessionId: sessionId, languageId: 2));

        var result = await _handler.Handle(CreateCommand(entity, new UpdateEventSessionLanguageDto
        {
            Language = new UpdateEventSessionLanguageLanguageDto { LanguageId = 2 }
        }), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Language is already assigned to this event session.");
        await _repository.DidNotReceive().Update(Arg.Any<EventSessionLanguage>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static UpdateEventSessionLanguageCommand CreateCommand(EventSessionLanguage entity, UpdateEventSessionLanguageDto dto) =>
        new()
        {
            EventSessionLanguageId = entity.Id,
            EventSessionId = entity.EventSessionId,
            EventId = Guid.NewGuid(),
            TenantId = entity.TenantId,
            ExpectedConcurrencyStamp = entity.ConcurrencyStamp,
            EventSessionLanguageDto = dto
        };

    private static EventSessionLanguage CreateLanguageAssignment(
        int id = 7,
        Guid? tenantId = null,
        Guid? eventSessionId = null,
        int languageId = 1)
    {
        return new EventSessionLanguage
        {
            Id = id,
            ConcurrencyStamp = Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            Tenant = null!,
            EventSessionId = eventSessionId ?? Guid.NewGuid(),
            EventSession = null!,
            LanguageId = languageId,
            Language = null!
        };
    }

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
