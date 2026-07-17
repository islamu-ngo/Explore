// ABOUTME: Unit tests for event registration cancellation command handling.
// ABOUTME: Verifies cancellation delegates to the capacity-aware repository path.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventRegistrations.Handlers.Commands;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventRegistrations.Commands;

public sealed class DeleteEventRegistrationCommandHandlerTests
{
    [Test]
    public async Task HandleOwnsSerializableCancellationBoundary()
    {
        var registrationId = Guid.NewGuid();
        var expectedOwnerUserId = Guid.NewGuid();
        var occurrenceId = Guid.NewGuid();
        var repository = Substitute.For<IEventRegistrationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.UserId.Returns(expectedOwnerUserId);
        repository.CancelAndReleaseCapacityAsync(
                registrationId,
                expectedOwnerUserId,
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                EventRegistrationActorProvenance.Attendee,
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(new EventRegistrationTransitionResult(
                Changed: true,
                ParentIntentId: Guid.NewGuid(),
                PreviousStatus: 1,
                FinalStatus: 4,
                TransitionReason: EventRegistrationTransitionReason.SelfCancelled,
                OccurrenceId: occurrenceId,
                OccurredAt: DateTimeOffset.UtcNow,
                ActorProvenance: EventRegistrationActorProvenance.Attendee,
                ActorUserId: expectedOwnerUserId,
                ChildTransitions: []));
        unitOfWork.ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<EventRegistrationTransitionResult>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<EventRegistrationTransitionResult>>>()(
                call.ArgAt<CancellationToken>(1)));
        var handler = CreateHandler(repository, unitOfWork, currentUserService);

        var result = await handler.Handle(
            new DeleteEventRegistrationCommand
            {
                Id = registrationId,
                ExpectedOwnerUserId = expectedOwnerUserId
            },
            CancellationToken.None);

        await Assert.That(result).IsTrue();
        await repository.Received(1).CancelAndReleaseCapacityAsync(
            registrationId,
            expectedOwnerUserId,
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            EventRegistrationActorProvenance.Attendee,
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<EventRegistrationTransitionResult>>>(),
            Arg.Any<CancellationToken>());
        await repository.Received(1).GetById(registrationId);
    }

    [Test]
    public async Task HandleWithoutPersistedOwnerBindingFailsClosed()
    {
        var repository = Substitute.For<IEventRegistrationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var handler = CreateHandler(repository, unitOfWork, currentUserService);

        var result = await handler.Handle(
            new DeleteEventRegistrationCommand { Id = Guid.NewGuid() },
            CancellationToken.None);

        await Assert.That(result).IsFalse();
        await repository.DidNotReceive().CancelAndReleaseCapacityAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().ExecuteSerializableAsync(
            Arg.Any<Func<CancellationToken, Task<EventRegistrationTransitionResult>>>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleSelfCancellationMaterializesOneRequiredLifecycleGraph()
    {
        Guid registrationId = Guid.CreateVersion7();
        Guid registrationIntentId = Guid.CreateVersion7();
        Guid expectedOwnerUserId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        var registration = CreateRegistration(registrationId, registrationIntentId, expectedOwnerUserId, tenantId, eventId);
        var intent = CreateIntent(registrationIntentId, expectedOwnerUserId, tenantId, eventId);
        var repository = Substitute.For<IEventRegistrationRepository>();
        var unitOfWork = ImmediateUnitOfWork();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var intentRepository = Substitute.For<IEventRegistrationIntentRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var materializer = Materializer();
        currentUserService.UserId.Returns(expectedOwnerUserId);
        repository.GetById(registrationId).Returns(registration);
        intentRepository.GetById(registrationIntentId).Returns(intent);
        eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId));
        userRepository.GetById(expectedOwnerUserId).Returns(CreateUser(expectedOwnerUserId));
        repository.CancelAndReleaseCapacityAsync(
                registrationId,
                expectedOwnerUserId,
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                EventRegistrationActorProvenance.Attendee,
                expectedOwnerUserId,
                Arg.Any<CancellationToken>())
            .Returns(call => Transition(
                registrationIntentId,
                call.ArgAt<Guid>(2),
                call.ArgAt<DateTimeOffset>(3),
                EventRegistrationTransitionReason.SelfCancelled,
                EventRegistrationActorProvenance.Attendee,
                expectedOwnerUserId,
                ApprovalStatusEnum.Cancelled));
        var handler = CreateHandler(
            repository,
            unitOfWork,
            currentUserService,
            intentRepository,
            eventRepository,
            userRepository,
            materializer);

        bool result = await handler.Handle(
            new DeleteEventRegistrationCommand
            {
                Id = registrationId,
                ExpectedOwnerUserId = expectedOwnerUserId
            },
            CancellationToken.None);

        await Assert.That(result).IsTrue();
        await materializer.Received(1).MaterializeInCurrentTransactionAsync(
            Arg.Is<RecipientNotificationMaterialization>(request =>
                request.Intent.TemplateKey == "registration.cancelled"
                && request.InApp != null
                && request.InApp.IsRequired
                && request.Email != null
                && request.Email.Kind == EmailDispatchKind.RegistrationCancelled),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleOrganizerRevocationUsesRevokedCopyFromPersistedProvenance()
    {
        Guid registrationId = Guid.CreateVersion7();
        Guid registrationIntentId = Guid.CreateVersion7();
        Guid ownerUserId = Guid.CreateVersion7();
        Guid organizerUserId = Guid.CreateVersion7();
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        var repository = Substitute.For<IEventRegistrationRepository>();
        var unitOfWork = ImmediateUnitOfWork();
        var currentUserService = Substitute.For<ICurrentUserService>();
        var intentRepository = Substitute.For<IEventRegistrationIntentRepository>();
        var eventRepository = Substitute.For<IEventRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        var materializer = Materializer();
        currentUserService.UserId.Returns(organizerUserId);
        repository.GetById(registrationId)
            .Returns(CreateRegistration(registrationId, registrationIntentId, ownerUserId, tenantId, eventId));
        intentRepository.GetById(registrationIntentId)
            .Returns(CreateIntent(registrationIntentId, ownerUserId, tenantId, eventId));
        eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId));
        userRepository.GetById(ownerUserId).Returns(CreateUser(ownerUserId));
        repository.CancelAndReleaseCapacityAsync(
                registrationId,
                ownerUserId,
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                EventRegistrationActorProvenance.Organizer,
                organizerUserId,
                Arg.Any<CancellationToken>())
            .Returns(call => Transition(
                registrationIntentId,
                call.ArgAt<Guid>(2),
                call.ArgAt<DateTimeOffset>(3),
                EventRegistrationTransitionReason.Revoked,
                EventRegistrationActorProvenance.Organizer,
                organizerUserId,
                ApprovalStatusEnum.Revoked));
        var handler = CreateHandler(
            repository,
            unitOfWork,
            currentUserService,
            intentRepository,
            eventRepository,
            userRepository,
            materializer);

        await handler.Handle(
            new DeleteEventRegistrationCommand
            {
                Id = registrationId,
                ExpectedOwnerUserId = ownerUserId
            },
            CancellationToken.None);

        await materializer.Received(1).MaterializeInCurrentTransactionAsync(
            Arg.Is<RecipientNotificationMaterialization>(request =>
                request.Intent.TemplateKey == "registration.revoked"
                && request.Email != null
                && request.Email.Kind == EmailDispatchKind.RegistrationRevoked),
            Arg.Any<CancellationToken>());
    }

    private static DeleteEventRegistrationCommandHandler CreateHandler(
        IEventRegistrationRepository repository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEventRegistrationIntentRepository? intentRepository = null,
        IEventRepository? eventRepository = null,
        IUserRepository? userRepository = null,
        IRecipientNotificationMaterializer? materializer = null)
    {
        return new DeleteEventRegistrationCommandHandler(
            repository,
            unitOfWork,
            currentUserService,
            intentRepository ?? Substitute.For<IEventRegistrationIntentRepository>(),
            eventRepository ?? Substitute.For<IEventRepository>(),
            userRepository ?? Substitute.For<IUserRepository>(),
            new RegistrationNotificationDeliveryService(new EventLifecycleEmailOutboxFactory()),
            materializer ?? Substitute.For<IRecipientNotificationMaterializer>());
    }

    private static IUnitOfWork ImmediateUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<EventRegistrationTransitionResult>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task<EventRegistrationTransitionResult>>>(0)(
                call.ArgAt<CancellationToken>(1)));
        return unitOfWork;
    }

    private static IRecipientNotificationMaterializer Materializer()
    {
        var materializer = Substitute.For<IRecipientNotificationMaterializer>();
        materializer.MaterializeInCurrentTransactionAsync(
                Arg.Any<RecipientNotificationMaterialization>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                RecipientNotificationMaterialization request = call.ArgAt<RecipientNotificationMaterialization>(0);
                return new RecipientNotificationMaterializationResult(
                    new NotificationIntent
                    {
                        Id = request.IntentId,
                        TenantId = request.Intent.TenantId!.Value,
                        TemplateKey = request.Intent.TemplateKey!,
                        DeduplicationKey = request.Intent.DeduplicationKey!
                    },
                    [],
                    null,
                    request.Email);
            });
        return materializer;
    }

    private static EventRegistrationTransitionResult Transition(
        Guid registrationIntentId,
        Guid occurrenceId,
        DateTimeOffset occurredAt,
        EventRegistrationTransitionReason reason,
        EventRegistrationActorProvenance provenance,
        Guid? actorUserId,
        ApprovalStatusEnum finalStatus)
    {
        return new EventRegistrationTransitionResult(
            Changed: true,
            ParentIntentId: registrationIntentId,
            PreviousStatus: (int)ApprovalStatusEnum.Approved,
            FinalStatus: (int)finalStatus,
            TransitionReason: reason,
            OccurrenceId: occurrenceId,
            OccurredAt: occurredAt,
            ActorProvenance: provenance,
            ActorUserId: actorUserId,
            ChildTransitions: []);
    }

    private static EventRegistration CreateRegistration(
        Guid id,
        Guid intentId,
        Guid userId,
        Guid tenantId,
        Guid eventId)
    {
        return new EventRegistration
        {
            Id = id,
            EventRegistrationIntentId = intentId,
            EventId = eventId,
            Event = null!,
            EventSessionId = Guid.CreateVersion7(),
            EventSession = null!,
            UserId = userId,
            User = null!,
            ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
            TenantId = tenantId,
            Tenant = null!
        };
    }

    private static EventRegistrationIntent CreateIntent(Guid id, Guid userId, Guid tenantId, Guid eventId)
    {
        return new EventRegistrationIntent
        {
            Id = id,
            EventId = eventId,
            Event = null!,
            UserId = userId,
            User = null!,
            RegistrationScope = null!,
            ApprovalStatusId = (int)ApprovalStatusEnum.Approved,
            TenantId = tenantId,
            Tenant = null!
        };
    }

    private static Explore.Domain.Event CreateEvent(Guid id, Guid tenantId)
    {
        return new Explore.Domain.Event
        {
            Id = id,
            TenantId = tenantId,
            Tenant = null!,
            Actor = null!,
            Title = "Community Iftar",
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };
    }

    private static User CreateUser(Guid id)
    {
        var user = new User
        {
            Id = id,
            EmailVerified = true,
            Pii = new UserPii
            {
                UserId = id,
                Email = "attendee@example.test",
                FirstName = "Test",
                LastName = "Attendee"
            }
        };
        user.Pii.User = user;
        return user;
    }
}
