// ABOUTME: Verifies public event-action collection and detail queries enforce participation legality.
// ABOUTME: Preserves published, public, and active gates while omitting incompatible or unknown action state.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.EventPublicActions.Handlers.Queries;
using Explore.Application.Features.EventPublicActions.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventPublicActions.Queries;

public sealed class EventPublicActionQueryHandlerTests
{
    [Test]
    public async Task List_PlatformManagedEvent_ReturnsOnlyCompatibleActiveActions()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        EventPublicAction compatibleActive = CreateAction(
            eventId,
            tenantId,
            EventPublicActionKindEnum.ExternalEventPage,
            EventPublicActionHealthStateEnum.Active);
        EventPublicAction incompatibleActive = CreateAction(
            eventId,
            tenantId,
            EventPublicActionKindEnum.ExternalRegistration,
            EventPublicActionHealthStateEnum.Active);
        EventPublicAction compatiblePending = CreateAction(
            eventId,
            tenantId,
            EventPublicActionKindEnum.OptionalQuestionnaire,
            EventPublicActionHealthStateEnum.PendingReview);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetById(eventId).Returns(CreateEvent(
            eventId,
            tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged));
        eventRepository.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>())
            .Returns(true);
        var actionRepository = Substitute.For<IEventPublicActionRepository>();
        actionRepository.ListByEventAsync(eventId, false, Arg.Any<CancellationToken>())
            .Returns([compatibleActive, incompatibleActive, compatiblePending]);
        IMapper mapper = CreateMapper();
        var handler = new GetEventPublicActionsRequestHandler(eventRepository, actionRepository, mapper);

        IReadOnlyList<EventPublicActionDto> result = await handler.Handle(
            new GetEventPublicActionsRequest(eventId),
            CancellationToken.None);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo(compatibleActive.Id);
        await eventRepository.Received(1)
            .IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>());
    }

    [Test]
    [Arguments(null)]
    [Arguments(999)]
    public async Task List_MissingOrUnknownParticipationConfiguration_ReturnsEmpty(
        int? participationHandlingModeId)
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetById(eventId).Returns(CreateEvent(
            eventId,
            tenantId,
            participationHandlingModeId));
        var actionRepository = Substitute.For<IEventPublicActionRepository>();
        actionRepository.ListByEventAsync(eventId, false, Arg.Any<CancellationToken>())
            .Returns([
                CreateAction(
                    eventId,
                    tenantId,
                    EventPublicActionKindEnum.ExternalEventPage,
                    EventPublicActionHealthStateEnum.Active)
            ]);
        var handler = new GetEventPublicActionsRequestHandler(
            eventRepository,
            actionRepository,
            CreateMapper());

        IReadOnlyList<EventPublicActionDto> result = await handler.Handle(
            new GetEventPublicActionsRequest(eventId),
            CancellationToken.None);

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task List_PublicEligibilityDenied_ReturnsEmptyBeforeActionRead()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetById(eventId).Returns(CreateEvent(
            eventId,
            tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged));
        eventRepository.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>())
            .Returns(false);
        var actionRepository = Substitute.For<IEventPublicActionRepository>();
        var handler = new GetEventPublicActionsRequestHandler(
            eventRepository,
            actionRepository,
            CreateMapper());

        IReadOnlyList<EventPublicActionDto> result = await handler.Handle(
            new GetEventPublicActionsRequest(eventId),
            CancellationToken.None);

        await Assert.That(result).IsEmpty();
        await eventRepository.Received(1)
            .IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>());
        await actionRepository.DidNotReceiveWithAnyArgs()
            .ListByEventAsync(default, default, default);
    }

    [Test]
    [Arguments((int)EventStatusEnum.Draft, (int)VisibilityTypeEnum.Public)]
    [Arguments((int)EventStatusEnum.Published, (int)VisibilityTypeEnum.Private)]
    public async Task List_EventIsNotPublishedAndPublic_ReturnsEmptyBeforeActionRead(
        int eventStatusId,
        int visibilityTypeId)
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        Explore.Domain.Event @event = CreateEvent(
            eventId,
            tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged,
            (EventStatusEnum)eventStatusId);
        @event.VisibilityTypeId = visibilityTypeId;
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetById(eventId).Returns(@event);
        var actionRepository = Substitute.For<IEventPublicActionRepository>();
        var handler = new GetEventPublicActionsRequestHandler(
            eventRepository,
            actionRepository,
            CreateMapper());

        IReadOnlyList<EventPublicActionDto> result = await handler.Handle(
            new GetEventPublicActionsRequest(eventId),
            CancellationToken.None);

        await Assert.That(result).IsEmpty();
        await actionRepository.DidNotReceiveWithAnyArgs()
            .ListByEventAsync(default, default, default);
    }

    [Test]
    [Arguments((int)EventPublicActionKindEnum.ExternalEventPage, (int)EventPublicActionHealthStateEnum.Active, true)]
    [Arguments((int)EventPublicActionKindEnum.ExternalRegistration, (int)EventPublicActionHealthStateEnum.Active, false)]
    [Arguments((int)EventPublicActionKindEnum.ExternalEventPage, (int)EventPublicActionHealthStateEnum.PendingReview, false)]
    public async Task Detail_PlatformManagedEvent_RequiresCompatibleActiveAction(
        int actionKindId,
        int healthStateId,
        bool expected)
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        EventPublicAction action = CreateAction(
            eventId,
            tenantId,
            (EventPublicActionKindEnum)actionKindId,
            (EventPublicActionHealthStateEnum)healthStateId);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetById(eventId).Returns(CreateEvent(
            eventId,
            tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged));
        eventRepository.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>())
            .Returns(true);
        var actionRepository = Substitute.For<IEventPublicActionRepository>();
        actionRepository.GetDetailsAsync(action.Id, false, Arg.Any<CancellationToken>()).Returns(action);
        var handler = new GetEventPublicActionRequestHandler(
            eventRepository,
            actionRepository,
            CreateMapper());

        EventPublicActionDto? result = await handler.Handle(
            new GetEventPublicActionRequest(eventId, action.Id),
            CancellationToken.None);

        await Assert.That(result is not null).IsEqualTo(expected);
        await eventRepository.Received(1)
            .IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Detail_PublicEligibilityDenied_ReturnsNullBeforeActionRead()
    {
        Guid eventId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        EventPublicAction action = CreateAction(
            eventId,
            tenantId,
            EventPublicActionKindEnum.ExternalEventPage,
            EventPublicActionHealthStateEnum.Active);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetById(eventId).Returns(CreateEvent(
            eventId,
            tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged));
        eventRepository.IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>())
            .Returns(false);
        var actionRepository = Substitute.For<IEventPublicActionRepository>();
        actionRepository.GetDetailsAsync(action.Id, false, Arg.Any<CancellationToken>()).Returns(action);
        var handler = new GetEventPublicActionRequestHandler(
            eventRepository,
            actionRepository,
            CreateMapper());

        EventPublicActionDto? result = await handler.Handle(
            new GetEventPublicActionRequest(eventId, action.Id),
            CancellationToken.None);

        await Assert.That(result).IsNull();
        await eventRepository.Received(1)
            .IsPubliclyEligibleAsync(tenantId, eventId, Arg.Any<CancellationToken>());
        await actionRepository.DidNotReceiveWithAnyArgs()
            .GetDetailsAsync(default, default, default);
    }

    private static IMapper CreateMapper()
    {
        IMapper mapper = Substitute.For<IMapper>();
        mapper.Map<List<EventPublicActionDto>>(Arg.Any<object>())
            .Returns(call => ((IEnumerable<EventPublicAction>)call.Arg<object>())
                .Select(ToDto)
                .ToList());
        mapper.Map<EventPublicActionDto>(Arg.Any<object>())
            .Returns(call => ToDto((EventPublicAction)call.Arg<object>()));
        return mapper;
    }

    private static EventPublicActionDto ToDto(EventPublicAction action) => new()
    {
        Id = action.Id,
        EventId = action.EventId,
        KindId = action.EventPublicActionKindId,
        HealthStateId = action.HealthStateId,
        Url = action.Url,
        DestinationDomain = action.DestinationDomain
    };

    private static EventPublicAction CreateAction(
        Guid eventId,
        Guid tenantId,
        EventPublicActionKindEnum kind,
        EventPublicActionHealthStateEnum healthState)
    {
        var action = new EventPublicAction
        {
            Id = Guid.CreateVersion7(),
            EventId = eventId,
            TenantId = tenantId,
            EventPublicActionKindId = (int)kind,
            HealthStateId = (int)healthState
        };
        action.SetDestination(ExternalActionUrl.Create("https://events.example.org/action"));
        return action;
    }

    private static Explore.Domain.Event CreateEvent(
        Guid eventId,
        Guid tenantId,
        int? participationHandlingModeId,
        EventStatusEnum status = EventStatusEnum.Published) => new(status)
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Event",
            EventProvenanceTypeId = (int)EventProvenanceTypeEnum.OrganizerCreated,
            Actor = null!,
            Tenant = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!,
            ParticipationConfiguration = participationHandlingModeId.HasValue
            ? CreateParticipationConfiguration(eventId, tenantId, participationHandlingModeId.Value)
            : null
        };

    private static EventParticipationConfiguration CreateParticipationConfiguration(
        Guid eventId,
        Guid tenantId,
        int participationHandlingModeId)
    {
        EventParticipationConfiguration configuration = EventParticipationConfiguration.Create(
            eventId,
            tenantId,
            (int)ParticipationHandlingModeEnum.PlatformManaged,
            (int)AdvanceRegistrationObligationEnum.Required,
            (int)IdentityAccessModeEnum.AccountRequired,
            guestRecoveryPolicy: null,
            DateTime.UtcNow);
        typeof(EventParticipationConfiguration)
            .GetProperty(nameof(EventParticipationConfiguration.ParticipationHandlingModeId))!
            .SetValue(configuration, participationHandlingModeId);
        return configuration;
    }
}
