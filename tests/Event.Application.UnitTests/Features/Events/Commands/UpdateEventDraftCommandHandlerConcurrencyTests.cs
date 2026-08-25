// ABOUTME: Regression tests for event draft stale-write detection.
// ABOUTME: Ensures draft updates reject stale concurrency stamps before mutating events.

using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Exceptions;
using Explore.Application.Features.Events.Handlers.Commands;
using Explore.Application.Features.Events.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.Services.Scheduling;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;

namespace Event.Application.UnitTests.Features.Events.Commands;

public class UpdateEventDraftCommandHandlerConcurrencyTests
{
    [Test]
    public async Task Handle_WhenExpectedConcurrencyStampIsStale_ThrowsConcurrencyConflict()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var participationConfigurations = Substitute.For<IEventParticipationConfigurationRepository>();
        var visibilityTypeRepository = Substitute.For<IVisibilityTypeRepository>();
        var eventFormatRepository = Substitute.For<IEventFormatRepository>();
        var handler = new UpdateEventDraftCommandHandler(
            eventRepository,
            participationConfigurations,
            Substitute.For<IAudienceAgeRepository>(),
            Substitute.For<IAudienceGenderRepository>(),
            Substitute.For<IEventTypeRepository>(),
            visibilityTypeRepository,
            eventFormatRepository,
            Substitute.For<IStorageObjectRepository>(),
            Substitute.For<IEventSeriesRepository>(),
            Substitute.For<IEventRegistrationPolicyRepository>(),
            new EventScheduleProjectionCalculator(),
            Substitute.For<HybridCache>());

        var eventId = Guid.NewGuid();
        var existingStamp = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var staleStamp = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var existingEvent = CreateEvent(eventId, existingStamp);

        visibilityTypeRepository.Exists(1).Returns(true);
        eventFormatRepository.Exists(1).Returns(true);
        eventRepository.GetScheduleGraphForUpdateAsync(eventId, Arg.Any<CancellationToken>()).Returns(existingEvent);

        var command = new UpdateEventDraftCommand
        {
            Id = eventId,
            Draft = new UpdateEventDraftRequestDto
            {
                ExpectedConcurrencyStamp = staleStamp,
                ExpectedParticipationConfigurationConcurrencyStamp = Guid.NewGuid(),
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Title = "Updated title",
                VisibilityTypeId = 1,
                EventFormatId = 1
            }
        };

        var exception = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => handler.Handle(command, CancellationToken.None));

        await Assert.That(exception!.Code).IsEqualTo(ConcurrencyConflictException.ConcurrentUpdate);
        await Assert.That(exception.EntityType).IsEqualTo("event");
        await Assert.That(exception.EntityId).IsEqualTo(eventId.ToString());
        await eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WhenEventIsNotDraft_ReturnsFailureBeforeMutation()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var participationConfigurations = Substitute.For<IEventParticipationConfigurationRepository>();
        var visibilityTypeRepository = Substitute.For<IVisibilityTypeRepository>();
        var eventFormatRepository = Substitute.For<IEventFormatRepository>();
        var handler = new UpdateEventDraftCommandHandler(
            eventRepository,
            participationConfigurations,
            Substitute.For<IAudienceAgeRepository>(),
            Substitute.For<IAudienceGenderRepository>(),
            Substitute.For<IEventTypeRepository>(),
            visibilityTypeRepository,
            eventFormatRepository,
            Substitute.For<IStorageObjectRepository>(),
            Substitute.For<IEventSeriesRepository>(),
            Substitute.For<IEventRegistrationPolicyRepository>(),
            new EventScheduleProjectionCalculator(),
            Substitute.For<HybridCache>());

        var eventId = Guid.NewGuid();
        var concurrencyStamp = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var existingEvent = CreateEvent(eventId, concurrencyStamp, EventStatusEnum.Published);
        string originalTitle = existingEvent.Title;

        visibilityTypeRepository.Exists(1).Returns(true);
        eventFormatRepository.Exists(1).Returns(true);
        eventRepository.GetScheduleGraphForUpdateAsync(eventId, Arg.Any<CancellationToken>()).Returns(existingEvent);

        var command = new UpdateEventDraftCommand
        {
            Id = eventId,
            Draft = new UpdateEventDraftRequestDto
            {
                ExpectedConcurrencyStamp = concurrencyStamp,
                ExpectedParticipationConfigurationConcurrencyStamp = Guid.NewGuid(),
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Title = "Updated title",
                VisibilityTypeId = 1,
                EventFormatId = 1
            }
        };

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_draft_not_editable");
        await Assert.That(existingEvent.Title).IsEqualTo(originalTitle);
        await participationConfigurations.DidNotReceive().UpdateAsync(Arg.Any<EventParticipationConfiguration>(), Arg.Any<CancellationToken>());
        await eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
    }

    [Test]
    public async Task Handle_WhenStatusChangesAfterPreflight_DomainGuardRejectsBeforeMutation()
    {
        var eventRepository = Substitute.For<IEventRepository>();
        var participationConfigurations = Substitute.For<IEventParticipationConfigurationRepository>();
        var visibilityTypeRepository = Substitute.For<IVisibilityTypeRepository>();
        var eventFormatRepository = Substitute.For<IEventFormatRepository>();
        var handler = new UpdateEventDraftCommandHandler(
            eventRepository,
            participationConfigurations,
            Substitute.For<IAudienceAgeRepository>(),
            Substitute.For<IAudienceGenderRepository>(),
            Substitute.For<IEventTypeRepository>(),
            visibilityTypeRepository,
            eventFormatRepository,
            Substitute.For<IStorageObjectRepository>(),
            Substitute.For<IEventSeriesRepository>(),
            Substitute.For<IEventRegistrationPolicyRepository>(),
            new EventScheduleProjectionCalculator(),
            Substitute.For<HybridCache>());

        var eventId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        var existingEvent = CreateEvent(eventId, concurrencyStamp);
        var participationConfiguration = EventParticipationConfiguration.Create(
            eventId,
            existingEvent.TenantId,
            (int)ParticipationHandlingModeEnum.InformationOnly,
            (int)AdvanceRegistrationObligationEnum.NotApplicable,
            identityAccessModeId: null,
            guestRecoveryPolicy: null,
            DateTime.UtcNow);
        participationConfiguration.ConcurrencyStamp = Guid.NewGuid();
        Guid originalParticipationStamp = participationConfiguration.ConcurrencyStamp;
        string originalTitle = existingEvent.Title;

        visibilityTypeRepository.Exists(1).Returns(true);
        eventFormatRepository.Exists(1).Returns(true);
        eventRepository.GetScheduleGraphForUpdateAsync(eventId, Arg.Any<CancellationToken>()).Returns(existingEvent);
        participationConfigurations
            .GetByEventAndTenantAsync(eventId, existingEvent.TenantId, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                existingEvent.SynchronizeFederatedLifecycle(EventStatusEnum.Published, DateTime.UtcNow);
                return participationConfiguration;
            });
        var command = new UpdateEventDraftCommand
        {
            Id = eventId,
            Draft = new UpdateEventDraftRequestDto
            {
                ExpectedConcurrencyStamp = concurrencyStamp,
                ExpectedParticipationConfigurationConcurrencyStamp = participationConfiguration.ConcurrencyStamp,
                ParticipationConfiguration = CreateParticipationConfiguration(),
                Title = "Updated title",
                VisibilityTypeId = 1,
                EventFormatId = 1
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));

        await Assert.That(existingEvent.Title).IsEqualTo(originalTitle);
        await Assert.That(participationConfiguration.ConcurrencyStamp).IsEqualTo(originalParticipationStamp);
        await participationConfigurations.DidNotReceive().UpdateAsync(Arg.Any<EventParticipationConfiguration>(), Arg.Any<CancellationToken>());
        await eventRepository.DidNotReceive().Update(Arg.Any<Explore.Domain.Event>());
    }

    private static Explore.Domain.Event CreateEvent(
        Guid eventId,
        Guid concurrencyStamp,
        EventStatusEnum status = EventStatusEnum.Draft) => new(status)
        {
            Id = eventId,
            Title = "Original title",
            Actor = null!,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            TenantId = Guid.NewGuid(),
            VisibilityTypeId = 1,
            EventFormatId = 1,
            ConcurrencyStamp = concurrencyStamp
        };

    private static ConfigureEventParticipationDto CreateParticipationConfiguration() => new()
    {
        ParticipationHandlingModeId = (int)Explore.Domain.Enums.ParticipationHandlingModeEnum.InformationOnly,
        AdvanceRegistrationObligationId = (int)Explore.Domain.Enums.AdvanceRegistrationObligationEnum.NotApplicable
    };
}
