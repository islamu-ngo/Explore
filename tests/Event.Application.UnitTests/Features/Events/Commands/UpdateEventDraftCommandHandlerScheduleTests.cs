// ABOUTME: Tests draft event updates that change schedule timezone and reproject loaded child schedule rows.
// ABOUTME: Protects the UTC-source/local-cache invariant across event-level timezone edits.

using Explore.Application.Caching;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Commands;

public class UpdateEventDraftCommandHandlerScheduleTests
{
    [Test]
    public async Task Handle_WhenTimezoneChanges_ReprojectsScheduleGraph()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var visibilityTypeRepository = Substitute.For<IVisibilityTypeRepository>();
        var eventFormatRepository = Substitute.For<IEventFormatRepository>();
        var cache = Substitute.For<HybridCache>();
        var handler = CreateHandler(eventRepository, visibilityTypeRepository, eventFormatRepository, cache);
        var eventId = Guid.NewGuid();
        var tenant = CreateTenant();
        var concurrencyStamp = Guid.NewGuid();
        var day = new EventDay
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            Tenant = tenant,
            LocalDate = new DateOnly(2026, 6, 15)
        };
        var session = new EventSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            Tenant = tenant,
            EventSessionStatusId = (int)EventSessionStatusEnum.Published,
            StartTime = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)
        };
        var eventEntity = CreateEvent(eventId, tenant, concurrencyStamp);
        eventEntity.Days.Add(day);
        eventEntity.Sessions.Add(session);

        visibilityTypeRepository.Exists(1).Returns(true);
        eventFormatRepository.Exists(1).Returns(true);
        eventRepository.GetScheduleGraphForUpdateAsync(eventId, Arg.Any<CancellationToken>()).Returns(eventEntity);

        var result = await handler.Handle(new UpdateEventDraftCommand
        {
            Id = eventId,
            Draft = new UpdateEventDraftRequestDto
            {
                ExpectedConcurrencyStamp = concurrencyStamp,
                Title = "Updated",
                VisibilityTypeId = 1,
                EventFormatId = 1,
                EventTimeZoneId = "Europe/Brussels"
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(eventEntity.EventTimeZoneId).IsEqualTo("Europe/Brussels");
        await Assert.That(eventEntity.Timezone).IsEqualTo("Europe/Brussels");
        await Assert.That(session.LocalStartTime).IsEqualTo(new TimeOnly(12, 0));
        await Assert.That(session.LocalEndTime).IsEqualTo(new TimeOnly(14, 0));
        await Assert.That(session.EventDayId).IsEqualTo(day.Id);
        await Assert.That(eventEntity.FirstSessionStartUtc).IsEqualTo(session.StartTime);
        await Assert.That(eventEntity.FirstSessionDate).IsEqualTo(day.LocalDate);
        await eventRepository.Received(1).Update(eventEntity);
        await cache.Received(1).RemoveByTagAsync(CacheTags.EventListByTenant(tenant.Id), Arg.Any<CancellationToken>());
    }

    private static UpdateEventDraftCommandHandler CreateHandler(
        IEventRepository eventRepository,
        IVisibilityTypeRepository visibilityTypeRepository,
        IEventFormatRepository eventFormatRepository,
        HybridCache cache) => new(
            eventRepository,
            Substitute.For<IAudienceAgeRepository>(),
            Substitute.For<IAudienceGenderRepository>(),
            Substitute.For<IEventTypeRepository>(),
            visibilityTypeRepository,
            eventFormatRepository,
            Substitute.For<IStorageObjectRepository>(),
            Substitute.For<IEventSeriesRepository>(),
            Substitute.For<IEventRegistrationPolicyRepository>(),
            new EventScheduleProjectionCalculator(),
            cache);

    private static Explore.Domain.Event CreateEvent(Guid eventId, Tenant tenant, Guid concurrencyStamp) => new()
    {
        Id = eventId,
        Title = "Original",
        Actor = null!,
        TenantId = tenant.Id,
        Tenant = tenant,
        VisibilityType = null!,
        EventStatus = null!,
        EventFormat = null!,
        VisibilityTypeId = 1,
        EventStatusId = 1,
        EventFormatId = 1,
        ConcurrencyStamp = concurrencyStamp
    };

    private static Tenant CreateTenant() => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Tenant",
        Slug = "tenant",
        TenantStatus = null!
    };
}
