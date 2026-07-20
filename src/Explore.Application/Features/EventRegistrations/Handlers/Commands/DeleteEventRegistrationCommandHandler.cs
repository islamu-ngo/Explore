// ABOUTME: Handler for cancelling an event registration with a server-bound persisted owner snapshot.
// ABOUTME: Delegates to the repository's atomic owner-predicate cancellation and capacity-release path.
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Domain;
using Explore.Domain.Federation;
using MediatR;

namespace Explore.Application.Features.EventRegistrations.Handlers.Commands;

public class DeleteEventRegistrationCommandHandler : IRequestHandler<DeleteEventRegistrationCommand, bool>
{
    private readonly IEventRegistrationRepository _eventRegistrationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEventRegistrationIntentRepository _intentRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRegistrationNotificationDeliveryService _notificationDeliveryService;
    private readonly IRecipientNotificationMaterializer _recipientNotificationMaterializer;
    private readonly IEventLifecycleScheduler _eventLifecycleScheduler;
    private readonly AtprotoEventPublicationPlanner _atprotoPublicationPlanner;

    public DeleteEventRegistrationCommandHandler(
        IEventRegistrationRepository eventRegistrationRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEventRegistrationIntentRepository intentRepository,
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IRegistrationNotificationDeliveryService notificationDeliveryService,
        IRecipientNotificationMaterializer recipientNotificationMaterializer,
        IEventLifecycleScheduler eventLifecycleScheduler,
        AtprotoEventPublicationPlanner atprotoPublicationPlanner)
    {
        _eventRegistrationRepository = eventRegistrationRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _intentRepository = intentRepository;
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _notificationDeliveryService = notificationDeliveryService;
        _recipientNotificationMaterializer = recipientNotificationMaterializer;
        _eventLifecycleScheduler = eventLifecycleScheduler;
        _atprotoPublicationPlanner = atprotoPublicationPlanner;
    }

    public async Task<bool> Handle(DeleteEventRegistrationCommand request, CancellationToken cancellationToken)
    {
        if (request.ExpectedOwnerUserId is not { } expectedOwnerUserId)
            return false;

        var occurrenceId = Guid.CreateVersion7();
        var occurredAt = DateTimeOffset.UtcNow;
        var notificationIntentId = Guid.CreateVersion7();
        var emailDispatchOutboxId = Guid.CreateVersion7();
        var atprotoOutboxId = Guid.CreateVersion7();
        var actorUserId = _currentUserService.UserId;
        var actorProvenance = actorUserId switch
        {
            null => EventRegistrationActorProvenance.System,
            var id when id == expectedOwnerUserId => EventRegistrationActorProvenance.Attendee,
            _ => EventRegistrationActorProvenance.Organizer
        };
        var transition = await _unitOfWork.ExecuteSerializableAsync(
            async ct =>
            {
                var registration = await _eventRegistrationRepository.GetById(request.Id);
                EventRegistrationIntent? registrationIntent = registration?.EventRegistrationIntentId is { } parentIntentId
                    ? await _intentRepository.GetById(parentIntentId)
                    : null;
                Explore.Domain.Event? parentEvent = registrationIntent is null
                    ? null
                    : await _eventRepository.GetById(registrationIntent.EventId);
                User? recipient = registrationIntent is null
                    ? null
                    : await _userRepository.GetById(registrationIntent.UserId);

                EventRegistrationTransitionResult result =
                    await _eventRegistrationRepository.CancelAndReleaseCapacityAsync(
                        request.Id,
                        expectedOwnerUserId,
                        occurrenceId,
                        occurredAt,
                        actorProvenance,
                        actorUserId,
                        ct);
                if (result.Changed
                    && registrationIntent is not null
                    && actorProvenance == EventRegistrationActorProvenance.Attendee
                    && result.ActorProvenance == EventRegistrationActorProvenance.Attendee
                    && result.TransitionReason == EventRegistrationTransitionReason.SelfCancelled)
                {
                    await _atprotoPublicationPlanner.PlanRsvpAsync(
                        new AtprotoRsvpPublicationInput(
                            registrationIntent.TenantId,
                            registrationIntent.UserId,
                            registrationIntent.EventId,
                            registrationIntent.Id,
                            registrationIntent.ConcurrencyStamp,
                            PdsSyncOperation.Delete,
                            atprotoOutboxId,
                            occurredAt.UtcDateTime),
                        ct);
                }
                if (registrationIntent is not null
                    && parentEvent is not null
                    && recipient is not null
                    && result.Changed
                    && result.PreviousStatus != result.FinalStatus)
                {
                    RecipientNotificationMaterialization? materialization =
                        _notificationDeliveryService.CreateLifecycleMaterialization(
                            registrationIntent,
                            parentEvent.Title,
                            recipient,
                            result,
                            notificationIntentId,
                            emailDispatchOutboxId);
                    if (materialization is not null)
                    {
                        await _recipientNotificationMaterializer.MaterializeInCurrentTransactionAsync(
                            materialization,
                            ct);
                    }
                }
                if (result.Changed && registrationIntent is not null && parentEvent is not null)
                {
                    await _eventLifecycleScheduler.ReprojectEventRemindersInCurrentTransactionAsync(
                        new EventReminderReprojectionInput(
                            registrationIntent.TenantId,
                            registrationIntent.EventId,
                            registrationIntent.Id,
                            SessionId: null,
                            parentEvent.Title,
                            occurredAt,
                            parentEvent.GetEffectiveScheduleTimeZoneId()),
                        ct);
                }

                return result;
            },
            cancellationToken);

        return transition.Changed;
    }
}
