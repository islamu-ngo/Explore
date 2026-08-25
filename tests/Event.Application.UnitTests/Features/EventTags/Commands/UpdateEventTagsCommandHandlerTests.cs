// ABOUTME: Unit tests for grouped event-tag link command handling.
// ABOUTME: Covers validation, concurrency, duplicate checks, one-save updates, and cache invalidation.

using Explore.Application.Authorization;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventTags;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventTags.Handlers.Commands;
using Explore.Application.Features.EventTags.Requests.Commands;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventTags.Commands;

public sealed class UpdateEventTagsCommandHandlerTests
{
    private readonly IEventTagsRepository _repository = Substitute.For<IEventTagsRepository>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly ITagRepository _tagRepository = Substitute.For<ITagRepository>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateEventTagsCommandHandler _handler;

    public UpdateEventTagsCommandHandlerTests()
    {
        _handler = new UpdateEventTagsCommandHandler(
            _repository,
            _eventRepository,
            _tagRepository,
            _cache);
    }

    [Test]
    public async Task Handle_WithEmptyWrapper_ReturnsFailedResponseWithoutSaving()
    {
        var command = new UpdateEventTagsCommand
        {
            EventTagId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventTagsDto = new UpdateEventTagsDto()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("At least one event tag update group must be provided.");
        await _repository.DidNotReceive().Update(Arg.Any<Explore.Domain.EventTags>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithForgedContext_UsesPersistedContextWithoutMutatingOriginalRequest()
    {
        var entity = CreateEventTag();
        _repository.GetById(entity.Id).Returns(entity);
        _eventRepository.GetById(entity.EventId).Returns(CreateEvent(entity.EventId, entity.TenantId));
        var newTagId = Guid.NewGuid();
        var forgedEventId = Guid.NewGuid();
        var forgedTenantId = Guid.NewGuid();
        _tagRepository.GetById(newTagId).Returns(CreateTag(newTagId, entity.TenantId));
        _repository.GetByEventAndTag(entity.EventId, newTagId, entity.Id).Returns((Explore.Domain.EventTags?)null);
        var command = CreateCommand(entity, new UpdateEventTagsDto
        {
            Tag = new UpdateEventTagsTagDto { TagId = newTagId }
        }) with
        {
            EventId = forgedEventId,
            TenantId = forgedTenantId,
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(command.EventId).IsEqualTo(forgedEventId);
        await Assert.That(command.TenantId).IsEqualTo(forgedTenantId);
        await _repository.Received(1).GetByEventAndTag(entity.EventId, newTagId, entity.Id);
        await _repository.Received(1).Update(entity);
        await _cache.Received(1).RemoveAsync($"event:detail:{entity.EventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(entity.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithStaleConcurrencyStamp_ThrowsConflictWithoutSaving()
    {
        var entity = CreateEventTag();
        entity.ConcurrencyStamp = Guid.NewGuid();
        _repository.GetById(entity.Id).Returns(entity);

        var command = CreateCommand(entity, new UpdateEventTagsDto
        {
            Tag = new UpdateEventTagsTagDto { TagId = Guid.NewGuid() }
        });
        command = command with { ExpectedConcurrencyStamp = Guid.NewGuid() };

        await Assert.That(async () => await _handler.Handle(command, CancellationToken.None))
            .Throws<ConcurrencyConflictException>();
        await _repository.DidNotReceive().Update(Arg.Any<Explore.Domain.EventTags>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithTagChange_SavesOnceAndInvalidatesEventCaches()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var newTagId = Guid.NewGuid();
        var entity = CreateEventTag(tenantId: tenantId, eventId: eventId, tagId: tagId);
        _repository.GetById(entity.Id).Returns(entity);
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId));
        _tagRepository.GetById(newTagId).Returns(CreateTag(newTagId, tenantId));
        _repository.GetByEventAndTag(eventId, newTagId, entity.Id).Returns((Explore.Domain.EventTags?)null);

        var result = await _handler.Handle(CreateCommand(entity, new UpdateEventTagsDto
        {
            Tag = new UpdateEventTagsTagDto { TagId = newTagId }
        }), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(entity.TagId).IsEqualTo(newTagId);
        await _repository.Received(1).Update(entity);
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithDuplicateEventTag_ReturnsValidationFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var newTagId = Guid.NewGuid();
        var entity = CreateEventTag(tenantId: tenantId, eventId: eventId);
        _repository.GetById(entity.Id).Returns(entity);
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId));
        _tagRepository.GetById(newTagId).Returns(CreateTag(newTagId, tenantId));
        _repository.GetByEventAndTag(eventId, newTagId, entity.Id).Returns(CreateEventTag(tenantId: tenantId, eventId: eventId, tagId: newTagId));

        var result = await _handler.Handle(CreateCommand(entity, new UpdateEventTagsDto
        {
            Tag = new UpdateEventTagsTagDto { TagId = newTagId }
        }), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Tag is already assigned to this event.");
        await _repository.DidNotReceive().Update(Arg.Any<Explore.Domain.EventTags>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static UpdateEventTagsCommand CreateCommand(Explore.Domain.EventTags entity, UpdateEventTagsDto dto) =>
        new()
        {
            EventTagId = entity.Id,
            EventId = entity.EventId,
            TenantId = entity.TenantId,
            ExpectedConcurrencyStamp = entity.ConcurrencyStamp,
            EventTagsDto = dto
        };

    private static Explore.Domain.EventTags CreateEventTag(
        Guid? id = null,
        Guid? tenantId = null,
        Guid? eventId = null,
        Guid? tagId = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            Tenant = null!,
            EventId = eventId ?? Guid.NewGuid(),
            Event = null!,
            TagId = tagId ?? Guid.NewGuid(),
            Tag = null!
        };

    private static Explore.Domain.Event CreateEvent(Guid id, Guid tenantId) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Tenant = null!,
            Title = "Event",
            Actor = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };

    private static Tag CreateTag(Guid id, Guid tenantId) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Tenant = null!,
            MasterCode = "tag",
            FullName = "Tag"
        };
}
