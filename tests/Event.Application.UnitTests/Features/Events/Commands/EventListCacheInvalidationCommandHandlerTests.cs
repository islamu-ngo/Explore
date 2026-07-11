// ABOUTME: Cache invalidation tests for event write handlers that mutate public event lists.
// ABOUTME: Verifies tenant-scoped list tag eviction instead of legacy fixed-key removal.

using Explore.Application.Caching;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Commands;

public class EventListCacheInvalidationCommandHandlerTests
{
    [Test]
    public async Task UpdateEvent_WhenEventIsUpdated_InvalidatesTenantEventListTag()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var @event = CreateEvent(eventId, tenantId);
        var concurrencyStamp = Guid.NewGuid();
        @event.ConcurrencyStamp = concurrencyStamp;
        eventRepository.GetScheduleGraphForUpdateAsync(eventId, Arg.Any<CancellationToken>()).Returns(@event);

        var handler = new UpdateEventCommandHandler(
            eventRepository,
            Substitute.For<IAudienceAgeRepository>(),
            Substitute.For<IAudienceGenderRepository>(),
            Substitute.For<IEventTypeRepository>(),
            Substitute.For<IVisibilityTypeRepository>(),
            Substitute.For<IEventFormatRepository>(),
            Substitute.For<IStorageObjectRepository>(),
            Substitute.For<IEventSeriesRepository>(),
            Substitute.For<IEventRegistrationPolicyRepository>(),
            new EventScheduleProjectionCalculator(),
            cache);

        var result = await handler.Handle(new UpdateEventCommand
        {
            EventId = eventId,
            ExpectedConcurrencyStamp = concurrencyStamp,
            UpdateEventDto = new UpdateEventDto
            {
                Title = new UpdateEventTitleDto { Value = "Updated title" }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteEvent_WhenEventIsDeleted_InvalidatesTenantEventListTag()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var eventRepository = Substitute.For<IEventRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var actorRepository = Substitute.For<IActorRepository>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var cache = Substitute.For<HybridCache>();

        var @event = CreateEvent(eventId, tenantId);
        @event.ActorId = actorId;
        eventRepository.GetById(eventId).Returns(@event);
        sessionRepository.GetSessionsByEvent(eventId).Returns([]);
        actorRepository.GetById(actorId).Returns(CreateActor(actorId, tenantId, userId));
        currentUserService.UserId.Returns(userId);

        var handler = new DeleteEventCommandHandler(
            eventRepository,
            sessionRepository,
            actorRepository,
            Substitute.For<IOrganizationMemberRepository>(),
            Substitute.For<ITenantUserRoleGrantRepository>(),
            Substitute.For<IRoleRepository>(),
            currentUserService,
            Substitute.For<ILogger<DeleteEventCommandHandler>>(),
            cache);

        var result = await handler.Handle(new DeleteEventCommand
        {
            Id = eventId,
            UserId = userId.ToString()
        }, CancellationToken.None);

        await Assert.That(result).IsTrue();
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
    }

    private static Explore.Domain.Event CreateEvent(Guid eventId, Guid tenantId) => new()
    {
        Id = eventId,
        Title = "Tenant Event",
        Actor = null!,
        TenantId = tenantId,
        Tenant = CreateTenant(tenantId),
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!
    };

    private static Actor CreateActor(Guid actorId, Guid tenantId, Guid userId) => new()
    {
        Id = actorId,
        TenantId = tenantId,
        Tenant = CreateTenant(tenantId),
        UserId = userId,
        ActorType = new ActorType { Id = 1, FullName = "User", MasterCode = "user" },
        Pii = new ActorPii { DisplayName = "Event Owner" }
    };

    private static Tenant CreateTenant(Guid tenantId) => new()
    {
        Id = tenantId,
        FullName = "Tenant",
        Slug = "tenant",
        TenantStatus = null!
    };
}
