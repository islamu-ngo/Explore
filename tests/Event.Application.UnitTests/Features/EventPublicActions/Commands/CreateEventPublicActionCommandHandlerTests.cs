// ABOUTME: Verifies event public-action creation enforces participation legality, review state, and primary uniqueness.
// ABOUTME: Covers fail-closed modes, normalized destination disclosure, and authenticated tenant ownership checks.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventPublicAction;
using Explore.Application.Features.EventPublicActions.Handlers.Commands;
using Explore.Application.Features.EventPublicActions.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventPublicActions.Commands;

public sealed class CreateEventPublicActionCommandHandlerTests
{
    [Test]
    public async Task Handle_ValidAction_CreatesPendingReviewDestination()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(CreateEvent(tenantId, eventId));
        var actionRepository = Substitute.For<IEventPublicActionRepository>();
        actionRepository.Create(Arg.Any<EventPublicAction>())
            .Returns(call => call.Arg<EventPublicAction>());
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.CreateVersion7());
        var handler = new CreateEventPublicActionCommandHandler(
            eventRepository,
            actionRepository,
            tenantContext,
            currentUser);

        var result = await handler.Handle(new CreateEventPublicActionCommand
        {
            EventId = eventId,
            Action = new ManageEventPublicActionDto
            {
                KindId = (int)EventPublicActionKindEnum.ExternalRegistration,
                Url = "https://tickets.example.org/register",
                IsPrimary = true
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await actionRepository.Received(1).Create(Arg.Is<EventPublicAction>(action =>
            action.HealthStateId == (int)EventPublicActionHealthStateEnum.PendingReview
            && action.DestinationDomain == "tickets.example.org"
            && action.IsPrimary));
    }

    [Test]
    public async Task Handle_SecondPrimaryAction_FailsClosed()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(CreateEvent(tenantId, eventId));
        var actionRepository = Substitute.For<IEventPublicActionRepository>();
        actionRepository.HasOtherPrimaryAsync(eventId, null, Arg.Any<CancellationToken>()).Returns(true);
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.CreateVersion7());
        var handler = new CreateEventPublicActionCommandHandler(
            eventRepository,
            actionRepository,
            tenantContext,
            currentUser);

        var result = await handler.Handle(new CreateEventPublicActionCommand
        {
            EventId = eventId,
            Action = new ManageEventPublicActionDto
            {
                KindId = (int)EventPublicActionKindEnum.ExternalEventPage,
                Url = "https://events.example.org/details",
                IsPrimary = true
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await actionRepository.DidNotReceive().Create(Arg.Any<EventPublicAction>());
    }

    [Test]
    [Arguments(null)]
    [Arguments((int)ParticipationHandlingModeEnum.PlatformManaged)]
    [Arguments(999)]
    public async Task Handle_IncompatibleParticipationMode_FailsBeforePrimaryLookupOrCreate(
        int? participationHandlingModeId)
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(CreateEvent(tenantId, eventId, participationHandlingModeId));
        var actionRepository = Substitute.For<IEventPublicActionRepository>();
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.CreateVersion7());
        var handler = new CreateEventPublicActionCommandHandler(
            eventRepository,
            actionRepository,
            tenantContext,
            currentUser);

        var result = await handler.Handle(new CreateEventPublicActionCommand
        {
            EventId = eventId,
            Action = new ManageEventPublicActionDto
            {
                KindId = (int)EventPublicActionKindEnum.ExternalRegistration,
                Url = "https://tickets.example.org/register",
                IsPrimary = true
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains(
            "Public action kind is not available for this event's participation mode.");
        await actionRepository.DidNotReceiveWithAnyArgs()
            .HasOtherPrimaryAsync(default, default, default);
        await actionRepository.DidNotReceive().Create(Arg.Any<EventPublicAction>());
    }

    private static Explore.Domain.Event CreateEvent(
        Guid tenantId,
        Guid eventId,
        int? participationHandlingModeId = (int)ParticipationHandlingModeEnum.ExternalManaged) => new()
        {
            Id = eventId,
            TenantId = tenantId,
            Title = "Event",
            EventProvenanceTypeId = (int)EventProvenanceTypeEnum.OrganizerCreated,
            Actor = null!,
            Tenant = null!,
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
        ParticipationHandlingModeEnum validMode =
            participationHandlingModeId == (int)ParticipationHandlingModeEnum.ExternalManaged
                ? ParticipationHandlingModeEnum.ExternalManaged
                : ParticipationHandlingModeEnum.PlatformManaged;
        EventParticipationConfiguration configuration = EventParticipationConfiguration.Create(
            eventId,
            tenantId,
            (int)validMode,
            (int)AdvanceRegistrationObligationEnum.Required,
            validMode == ParticipationHandlingModeEnum.PlatformManaged
                ? (int)IdentityAccessModeEnum.AccountRequired
                : null,
            guestRecoveryPolicy: null,
            DateTime.UtcNow);
        typeof(EventParticipationConfiguration)
            .GetProperty(nameof(EventParticipationConfiguration.ParticipationHandlingModeId))!
            .SetValue(configuration, participationHandlingModeId);
        return configuration;
    }
}
