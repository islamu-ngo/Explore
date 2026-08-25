// ABOUTME: Unit tests for grouped event-category link command handling.
// ABOUTME: Covers validation, concurrency, duplicate checks, one-save updates, and cache invalidation.

using Explore.Application.Authorization;
using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventCategories;
using Explore.Application.Exceptions;
using Explore.Application.Features.EventCategories.Handlers.Commands;
using Explore.Application.Features.EventCategories.Requests.Commands;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventCategories.Commands;

public sealed class UpdateEventCategoriesCommandHandlerTests
{
    private readonly IEventCategoriesRepository _repository = Substitute.For<IEventCategoriesRepository>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly ICategoryRepository _categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();
    private readonly UpdateEventCategoriesCommandHandler _handler;

    public UpdateEventCategoriesCommandHandlerTests()
    {
        _handler = new UpdateEventCategoriesCommandHandler(
            _repository,
            _eventRepository,
            _categoryRepository,
            _cache);
    }

    [Test]
    public async Task Handle_WithEmptyWrapper_ReturnsFailedResponseWithoutSaving()
    {
        var command = new UpdateEventCategoriesCommand
        {
            EventCategoryId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            EventCategoriesDto = new UpdateEventCategoriesDto()
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).Contains("At least one event category update group must be provided.");
        await _repository.DidNotReceive().Update(Arg.Any<Explore.Domain.EventCategories>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithForgedContext_UsesPersistedContextWithoutMutatingOriginalRequest()
    {
        var entity = CreateEventCategory();
        _repository.GetById(entity.Id).Returns(entity);
        _eventRepository.GetById(entity.EventId).Returns(CreateEvent(entity.EventId, entity.TenantId));
        var newCategoryId = Guid.NewGuid();
        var forgedEventId = Guid.NewGuid();
        var forgedTenantId = Guid.NewGuid();
        _categoryRepository.GetById(newCategoryId).Returns(CreateCategory(newCategoryId, entity.TenantId));
        _repository.GetByEventAndCategory(entity.EventId, newCategoryId, entity.Id).Returns((Explore.Domain.EventCategories?)null);
        var command = CreateCommand(entity, new UpdateEventCategoriesDto
        {
            Category = new UpdateEventCategoriesCategoryDto { CategoryId = newCategoryId }
        }) with
        {
            EventId = forgedEventId,
            TenantId = forgedTenantId,
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(command.EventId).IsEqualTo(forgedEventId);
        await Assert.That(command.TenantId).IsEqualTo(forgedTenantId);
        await _repository.Received(1).GetByEventAndCategory(entity.EventId, newCategoryId, entity.Id);
        await _repository.Received(1).Update(entity);
        await _cache.Received(1).RemoveAsync($"event:detail:{entity.EventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(entity.TenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithStaleConcurrencyStamp_ThrowsConflictWithoutSaving()
    {
        var entity = CreateEventCategory();
        entity.ConcurrencyStamp = Guid.NewGuid();
        _repository.GetById(entity.Id).Returns(entity);

        var command = CreateCommand(entity, new UpdateEventCategoriesDto
        {
            Category = new UpdateEventCategoriesCategoryDto { CategoryId = Guid.NewGuid() }
        });
        command = command with { ExpectedConcurrencyStamp = Guid.NewGuid() };

        await Assert.That(async () => await _handler.Handle(command, CancellationToken.None))
            .Throws<ConcurrencyConflictException>();
        await _repository.DidNotReceive().Update(Arg.Any<Explore.Domain.EventCategories>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithCategoryChange_SavesOnceAndInvalidatesEventCaches()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var entity = CreateEventCategory(tenantId: tenantId, eventId: eventId, categoryId: categoryId);
        _repository.GetById(entity.Id).Returns(entity);
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId));
        _categoryRepository.GetById(newCategoryId).Returns(CreateCategory(newCategoryId, tenantId));
        _repository.GetByEventAndCategory(eventId, newCategoryId, entity.Id).Returns((Explore.Domain.EventCategories?)null);

        var result = await _handler.Handle(CreateCommand(entity, new UpdateEventCategoriesDto
        {
            Category = new UpdateEventCategoriesCategoryDto { CategoryId = newCategoryId }
        }), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(entity.CategoryId).IsEqualTo(newCategoryId);
        await _repository.Received(1).Update(entity);
        await _cache.Received(1).RemoveAsync($"event:detail:{eventId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithDuplicateEventCategory_ReturnsValidationFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var entity = CreateEventCategory(tenantId: tenantId, eventId: eventId);
        _repository.GetById(entity.Id).Returns(entity);
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId));
        _categoryRepository.GetById(newCategoryId).Returns(CreateCategory(newCategoryId, tenantId));
        _repository.GetByEventAndCategory(eventId, newCategoryId, entity.Id).Returns(CreateEventCategory(tenantId: tenantId, eventId: eventId, categoryId: newCategoryId));

        var result = await _handler.Handle(CreateCommand(entity, new UpdateEventCategoriesDto
        {
            Category = new UpdateEventCategoriesCategoryDto { CategoryId = newCategoryId }
        }), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Errors).Contains("Category is already assigned to this event.");
        await _repository.DidNotReceive().Update(Arg.Any<Explore.Domain.EventCategories>());
        await _cache.DidNotReceive().RemoveByTagAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static UpdateEventCategoriesCommand CreateCommand(Explore.Domain.EventCategories entity, UpdateEventCategoriesDto dto) =>
        new()
        {
            EventCategoryId = entity.Id,
            EventId = entity.EventId,
            TenantId = entity.TenantId,
            ExpectedConcurrencyStamp = entity.ConcurrencyStamp,
            EventCategoriesDto = dto
        };

    private static Explore.Domain.EventCategories CreateEventCategory(
        Guid? id = null,
        Guid? tenantId = null,
        Guid? eventId = null,
        Guid? categoryId = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            Tenant = null!,
            EventId = eventId ?? Guid.NewGuid(),
            Event = null!,
            CategoryId = categoryId ?? Guid.NewGuid(),
            Category = null!
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

    private static Category CreateCategory(Guid id, Guid tenantId) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Tenant = null!,
            MasterCode = "category",
            FullName = "Category"
        };
}
