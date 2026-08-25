// ABOUTME: Cache invalidation tests for event write handlers that mutate public event lists.
// ABOUTME: Verifies tenant-scoped list tag eviction instead of legacy fixed-key removal.

using Event.Application.UnitTests.Common;
using Explore.Application.Caching;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Notifications;
using Explore.Application.Responses;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Services.Scheduling;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Commands;

public class EventListCacheInvalidationCommandHandlerTests
{
    [Test]
    public async Task PublishedTimezoneChangeCapturesDstGapDisplayTimesInOneEventOccurrence()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var expectedStamp = Guid.CreateVersion7();
        var at = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
        var eventRepository = Substitute.For<IEventRepository>();
        var occurrenceRepository = Substitute.For<INotificationFanoutOccurrenceRepository>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        var cache = Substitute.For<HybridCache>();
        var calculator = new EventScheduleProjectionCalculator();
        Explore.Domain.Event @event = CreateEvent(eventId, tenantId, Explore.Domain.Enums.EventStatusEnum.Published);
        @event.ConcurrencyStamp = expectedStamp;
        @event.EventTimeZoneId = "UTC";
        @event.Timezone = "UTC";
        var session = new EventSession(Explore.Domain.Enums.EventSessionStatusEnum.Published)
        {
            Id = sessionId,
            EventId = eventId,
            Event = @event,
            TenantId = tenantId,
            Tenant = @event.Tenant,
            Title = "DST boundary session",
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        session.Reschedule(
            new DateTimeOffset(2026, 3, 29, 0, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.Zero),
            "UTC",
            calculator);
        @event.Sessions.Add(session);
        eventRepository.GetScheduleGraphForUpdateAsync(eventId, Arg.Any<CancellationToken>()).Returns(@event);
        occurrenceRepository.GetPendingForEventCoordinationAsync(
                tenantId,
                eventId,
                Arg.Any<CancellationToken>())
            .Returns(Array.Empty<NotificationFanoutOccurrence>());
        NotificationFanoutOccurrence? persistedOccurrence = null;
        occurrenceRepository.Create(Arg.Do<NotificationFanoutOccurrence>(value => persistedOccurrence = value))
            .Returns(call => call.Arg<NotificationFanoutOccurrence>());
        outboxRepository.Create(Arg.Any<OutboxMessage>()).Returns(call => call.Arg<OutboxMessage>());
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(Guid.CreateVersion7());
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
            calculator,
            cache,
            ImmediateUnitOfWork(),
            userContext,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new NotificationFanoutOccurrenceCoordinator(
                occurrenceRepository,
                Substitute.For<INotificationFanoutEmailSuppressionRepository>(),
                outboxRepository,
                new NotificationFanoutRecipientTemplateFactory()),
            Substitute.For<IEventLifecycleScheduler>(),
            Substitute.For<IRefundCampaignRepository>(),
            new FixedTimeProvider(at));

        BaseCommandResponse<Guid> result = await handler.Handle(new UpdateEventCommand
        {
            EventId = eventId,
            ExpectedConcurrencyStamp = expectedStamp,
            UpdateEventDto = new UpdateEventDto
            {
                EventTimeZone = new UpdateEventEventTimeZoneDto
                {
                    Value = OptionalUpdate<string?>.Set("Europe/Brussels")
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(persistedOccurrence).IsNotNull();
        NotificationFanoutRecipientTemplate template = new NotificationFanoutRecipientTemplateFactory()
            .Parse(persistedOccurrence!);
        await Assert.That(persistedOccurrence!.SessionId).IsNull();
        await Assert.That(persistedOccurrence.AudienceCutoffAt).IsEqualTo(at.UtcDateTime);
        await Assert.That(persistedOccurrence.AggregateVersion).IsEqualTo(expectedStamp);
        await Assert.That(template.Before.SessionDisplayTimes!).Count().IsEqualTo(1);
        await Assert.That(template.After.SessionDisplayTimes!).Count().IsEqualTo(1);
        await Assert.That(template.Before.SessionDisplayTimes![0].StartsAt.Offset).IsEqualTo(TimeSpan.Zero);
        await Assert.That(template.After.SessionDisplayTimes![0].StartsAt)
            .IsEqualTo(new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.FromHours(1)));
        await Assert.That(template.After.SessionDisplayTimes[0].EndsAt)
            .IsEqualTo(new DateTimeOffset(2026, 3, 29, 3, 30, 0, TimeSpan.FromHours(2)));
    }

    [Test]
    public async Task UpdateEvent_WhenEventIsUpdated_InvalidatesTenantEventListTag()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var eventRepository = Substitute.For<IEventRepository>();
        var cache = Substitute.For<HybridCache>();
        var @event = CreateEvent(eventId, tenantId);
        var concurrencyStamp = Guid.CreateVersion7();
        @event.ConcurrencyStamp = concurrencyStamp;
        eventRepository.GetScheduleGraphForUpdateAsync(eventId, Arg.Any<CancellationToken>()).Returns(@event);
        var unitOfWork = ImmediateUnitOfWork();
        var userContext = Substitute.For<IUserContext>();
        userContext.GetRequiredUserId().Returns(Guid.CreateVersion7());

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
            cache,
            unitOfWork,
            userContext,
            AtprotoPublicationPlannerTestFactory.Disabled(),
            new NotificationFanoutOccurrenceCoordinator(
                Substitute.For<INotificationFanoutOccurrenceRepository>(),
                Substitute.For<INotificationFanoutEmailSuppressionRepository>(),
                Substitute.For<IOutboxRepository>(),
                new NotificationFanoutRecipientTemplateFactory()),
            Substitute.For<IEventLifecycleScheduler>(),
            Substitute.For<IRefundCampaignRepository>(),
            TimeProvider.System);

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
    [Arguments(false)]
    [Arguments(true)]
    public async Task DeleteEvent_PaidEvidenceControlsDeletionAndCacheInvalidation(bool hasPaidEvidence)
    {
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var actorId = Guid.CreateVersion7();
        var eventRepository = Substitute.For<IEventRepository>();
        var sessionRepository = Substitute.For<IEventSessionRepository>();
        var inventoryRepository = Substitute.For<IRegistrationInventoryRepository>();
        var actorRepository = Substitute.For<IActorRepository>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var cache = Substitute.For<HybridCache>();

        var @event = CreateEvent(eventId, tenantId);
        @event.ActorId = actorId;
        eventRepository.GetById(eventId).Returns(@event);
        sessionRepository.GetSessionsByEvent(eventId).Returns([]);
        actorRepository.GetById(actorId).Returns(CreateActor(actorId, tenantId, userId));
        currentUserService.UserId.Returns(userId);
        inventoryRepository.HasPaidEvidenceAsync(eventId, tenantId, Arg.Any<CancellationToken>())
            .Returns(hasPaidEvidence);
        var unitOfWork = ImmediateUnitOfWork();

        var handler = new DeleteEventCommandHandler(
            eventRepository,
            sessionRepository,
            inventoryRepository,
            actorRepository,
            Substitute.For<IOrganizationMemberRepository>(),
            Substitute.For<ITenantUserRoleGrantRepository>(),
            Substitute.For<IRoleRepository>(),
            currentUserService,
            Substitute.For<ILogger<DeleteEventCommandHandler>>(),
            cache,
            unitOfWork,
            AtprotoPublicationPlannerTestFactory.Disabled());

        var result = await handler.Handle(new DeleteEventCommand
        {
            Id = eventId,
            UserId = userId.ToString()
        }, CancellationToken.None);

        await Assert.That(result).IsEqualTo(!hasPaidEvidence);
        if (hasPaidEvidence)
        {
            await eventRepository.DidNotReceive().Delete(Arg.Any<Explore.Domain.Event>());
            await cache.DidNotReceive().RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
        }
        else
        {
            await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenantId), Arg.Any<CancellationToken>());
        }
    }

    private static Explore.Domain.Event CreateEvent(
        Guid eventId,
        Guid tenantId,
        Explore.Domain.Enums.EventStatusEnum status = Explore.Domain.Enums.EventStatusEnum.Draft) => new(status)
        {
            Id = eventId,
            Title = "Tenant Event",
            Actor = null!,
            TenantId = tenantId,
            Tenant = CreateTenant(tenantId),
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            ConcurrencyStamp = Guid.CreateVersion7()
        };

    private static Actor CreateActor(Guid actorId, Guid tenantId, Guid userId) => new()
    {
        Id = actorId,
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

    private static IUnitOfWork ImmediateUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        unitOfWork.ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>()(CancellationToken.None));
        return unitOfWork;
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
