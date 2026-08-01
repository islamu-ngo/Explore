// ABOUTME: Verifies event public-action updates enforce participation legality before mutating tracked state.
// ABOUTME: Covers allowed review reset and fail-closed missing, incompatible, and unknown participation modes.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventPublicAction;
using Explore.Application.Features.EventPublicActions.Handlers.Commands;
using Explore.Application.Features.EventPublicActions.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventPublicActions.Commands;

public sealed class UpdateEventPublicActionCommandHandlerTests
{
    [Test]
    public async Task Handle_CompatibleParticipationMode_UpdatesPendingReviewAction()
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        EventPublicAction action = CreateAction(tenantId, eventId);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(CreateEvent(tenantId, eventId, (int)ParticipationHandlingModeEnum.ExternalManaged));
        var actionRepository = Substitute.For<IEventPublicActionRepository>();
        actionRepository.GetForUpdateAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.CreateVersion7());
        var handler = new UpdateEventPublicActionCommandHandler(
            eventRepository,
            actionRepository,
            new EventPublicActionTestUnitOfWork(),
            tenantContext,
            currentUser);

        var result = await handler.Handle(CreateCommand(action, EventPublicActionKindEnum.ExternalRegistration), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(action.EventPublicActionKindId)
            .IsEqualTo((int)EventPublicActionKindEnum.ExternalRegistration);
        await Assert.That(action.HealthStateId)
            .IsEqualTo((int)EventPublicActionHealthStateEnum.PendingReview);
        await Assert.That(action.DestinationDomain).IsEqualTo("tickets.example.org");
        await actionRepository.Received(1).Update(action);
    }

    [Test]
    [Arguments(null)]
    [Arguments((int)ParticipationHandlingModeEnum.PlatformManaged)]
    [Arguments(999)]
    public async Task Handle_IncompatibleParticipationMode_RejectsWithoutMutationOrUpdate(
        int? participationHandlingModeId)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        EventPublicAction action = CreateAction(tenantId, eventId);
        var eventRepository = Substitute.For<IEventRepository>();
        eventRepository.GetAuthorizationTargetByIdAsync(eventId, Arg.Any<CancellationToken>())
            .Returns(CreateEvent(tenantId, eventId, participationHandlingModeId));
        var actionRepository = Substitute.For<IEventPublicActionRepository>();
        actionRepository.GetForUpdateAsync(action.Id, Arg.Any<CancellationToken>()).Returns(action);
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(tenantId);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.CreateVersion7());
        var handler = new UpdateEventPublicActionCommandHandler(
            eventRepository,
            actionRepository,
            new EventPublicActionTestUnitOfWork(),
            tenantContext,
            currentUser);

        var result = await handler.Handle(CreateCommand(action, EventPublicActionKindEnum.ExternalRegistration), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains(
            "Public action kind is not available for this event's participation mode.");
        await Assert.That(action.EventPublicActionKindId)
            .IsEqualTo((int)EventPublicActionKindEnum.ExternalEventPage);
        await Assert.That(action.HealthStateId)
            .IsEqualTo((int)EventPublicActionHealthStateEnum.Active);
        await Assert.That(action.DestinationDomain).IsEqualTo("events.example.org");
        await actionRepository.DidNotReceiveWithAnyArgs()
            .HasOtherPrimaryAsync(default, default, default);
        await actionRepository.DidNotReceive().Update(Arg.Any<EventPublicAction>());
    }

    private static UpdateEventPublicActionCommand CreateCommand(
        EventPublicAction action,
        EventPublicActionKindEnum kind) => new()
        {
            EventId = action.EventId,
            ActionId = action.Id,
            Action = new ManageEventPublicActionDto
            {
                KindId = (int)kind,
                Url = "https://tickets.example.org/register",
                IsPrimary = true,
                ExpectedConcurrencyStamp = action.ConcurrencyStamp
            }
        };

    private static EventPublicAction CreateAction(Guid tenantId, Guid eventId)
    {
        var action = new EventPublicAction
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            EventId = eventId,
            EventPublicActionKindId = (int)EventPublicActionKindEnum.ExternalEventPage,
            HealthStateId = (int)EventPublicActionHealthStateEnum.Active,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        action.SetDestination(ExternalActionUrl.Create("https://events.example.org/details"));
        return action;
    }

    private static Explore.Domain.Event CreateEvent(
        Guid tenantId,
        Guid eventId,
        int? participationHandlingModeId) => new()
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
        ParticipationHandlingModeEnum validMode = participationHandlingModeId == (int)ParticipationHandlingModeEnum.ExternalManaged
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
