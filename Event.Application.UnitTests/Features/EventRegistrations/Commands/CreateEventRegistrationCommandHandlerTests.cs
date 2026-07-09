// ABOUTME: Unit tests for event registration creation capacity and waitlist behavior.
// ABOUTME: Verifies handlers call the capacity-aware repository contract and surface waitlist outcomes.

using System.Diagnostics.Metrics;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Handlers.Commands;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventRegistrations.Commands;

using AppNotificationCategory = Explore.Application.Notifications.NotificationCategory;

public sealed class CreateEventRegistrationCommandHandlerTests
{
    private readonly IEventRegistrationIntentRepository _intentRepository = Substitute.For<IEventRegistrationIntentRepository>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEventDayRepository _eventDayRepository = Substitute.For<IEventDayRepository>();
    private readonly IEventSessionRepository _eventSessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly IApprovalStatusRepository _approvalStatusRepository = Substitute.For<IApprovalStatusRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IContactShareConsentService _consentService = Substitute.For<IContactShareConsentService>();
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly INotificationOrchestrator _notificationOrchestrator = Substitute.For<INotificationOrchestrator>();
    private readonly IListmonkRegistrationSyncOutboxFactory _listmonkFactory = Substitute.For<IListmonkRegistrationSyncOutboxFactory>();
    private readonly CreateEventRegistrationCommandHandler _handler;

    public CreateEventRegistrationCommandHandlerTests()
    {
        _notificationOrchestrator.EnqueueAsync(
                Arg.Any<NotificationIntentDraft>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var draft = callInfo.ArgAt<NotificationIntentDraft>(0);
                return new NotificationOrchestrationResult(
                    new NotificationIntent
                    {
                        TenantId = draft.TenantId ?? Guid.CreateVersion7(),
                        Tenant = null!,
                        CategoryId = (int)NotificationCategoryEnum.RegistrationLifecycle,
                        Category = null!,
                        OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
                        OwnershipType = null!,
                        RecipientKindId = (int)NotificationRecipientKindEnum.User,
                        RecipientKind = null!,
                        StatusId = (int)NotificationIntentStatusEnum.Pending,
                        Status = null!,
                        TemplateKey = draft.TemplateKey ?? string.Empty,
                        DeduplicationKey = draft.DeduplicationKey ?? string.Empty
                    },
                    new NotificationOwnershipDecision(
                        draft.Category,
                        NotificationOwnership.IslamuEvent));
            });
        _listmonkFactory.CreateForRegistrationAsync(
                Arg.Any<Explore.Domain.Event>(),
                Arg.Any<User>(),
                Arg.Any<CreateEventRegistrationDto>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns((IntegrationSyncOutbox?)null);

        _handler = new CreateEventRegistrationCommandHandler(
            _intentRepository,
            _eventRepository,
            _userRepository,
            _eventDayRepository,
            _eventSessionRepository,
            _approvalStatusRepository,
            _tenantContext,
            CreateBusinessMetrics(),
            _consentService,
            new EventLifecycleEmailOutboxFactory(_notificationOrchestrator),
            _listmonkFactory,
            new RegistrationNotificationDeliveryService(
                _notificationRepository,
                CreateNotificationPreferenceResolver()),
            Substitute.For<ILogger<CreateEventRegistrationCommandHandler>>());
    }

    private static INotificationPreferenceResolver CreateNotificationPreferenceResolver()
    {
        var resolver = Substitute.For<INotificationPreferenceResolver>();
        resolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new NotificationPreferenceDecision(
                call.Arg<NotificationPreferenceResolveRequest>().CategoryCode,
                call.Arg<NotificationPreferenceResolveRequest>().ChannelCode,
                true,
                false,
                false,
                false,
                "Default",
                null));
        return resolver;
    }

    [Test]
    public async Task HandleWhenCapacityIsAvailableReturnsConfirmedRegistration()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = CreateSessionRegistrationCommand(eventId, userId, sessionId);
        SetupValidRegistration(tenantId, eventId, userId, sessionId);

        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<CancellationToken>(),
                Arg.Any<EmailDispatchOutbox?>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo =>
            {
                var intent = callInfo.ArgAt<EventRegistrationIntent>(0);
                return new EventRegistrationIntentCreationResult(intent, []);
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event Registration created successfully.");
        await _notificationOrchestrator.Received(1).EnqueueAsync(
            Arg.Is<NotificationIntentDraft>(draft =>
                draft.Category == AppNotificationCategory.RegistrationLifecycle
                && draft.TenantId == tenantId
                && draft.RecipientKind == "User"
                && draft.TemplateKey == "registration.confirmation"
                && draft.SafePayloadReference == $"event-registration-intent:{result.Id}"
                && draft.DeduplicationKey == $"event-registration-intent:{result.Id}:registration-confirmation"
                && draft.CorrelationId == result.Id.ToString()
                && draft.UserId == userId
                && draft.EventId == eventId
                && draft.IsUserFacing
                && draft.IsIslamuInitiated),
            Arg.Any<CancellationToken>());
        await _intentRepository.Received(1).CreateWithChildrenAndCapacityAsync(
            Arg.Is<EventRegistrationIntent>(intent =>
                intent != null
                && intent.ApprovalStatusId == (int)ApprovalStatusEnum.Approved
                && intent.TenantId == tenantId),
            Arg.Is<IReadOnlyList<EventRegistration>>(children =>
                children != null
                && children.Count == 1
                && children[0].EventSessionId == sessionId
                && children[0].ApprovalStatusId == (int)ApprovalStatusEnum.Approved),
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            Arg.Any<CancellationToken>(),
            Arg.Is<EmailDispatchOutbox>(outbox =>
                outbox != null
                && outbox.TenantId == tenantId
                && outbox.Kind == EmailDispatchKind.RegistrationConfirmation
                && outbox.SourceType == "event_registration_intent"
                && outbox.EventId == eventId
                && outbox.UserId == userId
                && outbox.RecipientEmail == "registrant@example.test"
                && outbox.Status == EmailDispatchStatus.Pending),
            Arg.Is<IntegrationSyncOutbox?>(outbox => outbox == null));
        await _notificationRepository.DidNotReceive().Create(Arg.Any<Notification>());
    }

    [Test]
    public async Task HandleWhenListmonkSyncOutboxFactoryReturnsRowPersistsItWithRegistration()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = CreateSessionRegistrationCommand(eventId, userId, sessionId);
        command.EventRegistrationDto.ShareEmailWithOrganizer = true;
        command.EventRegistrationDto.ConsentTextAcknowledged = "Share my email with the organizer.";
        command.EventRegistrationDto.ConsentUiVersion = "v1";
        SetupValidRegistration(tenantId, eventId, userId, sessionId);

        IntegrationSyncOutbox? capturedOutbox = null;
        _listmonkFactory.CreateForRegistrationAsync(
                Arg.Any<Explore.Domain.Event>(),
                Arg.Any<User>(),
                Arg.Any<CreateEventRegistrationDto>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => new IntegrationSyncOutbox
            {
                TenantId = tenantId,
                Tenant = null!,
                Kind = IntegrationKind.Listmonk,
                SourceType = "event_registration_intent",
                SourceId = callInfo.ArgAt<Guid>(3),
                EventId = eventId,
                UserId = userId,
                SubscriberEmail = "registrant@example.test",
                SubscriberPayloadJson = "{}",
                ListmonkListId = 42
            });
        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<CancellationToken>(),
                Arg.Any<EmailDispatchOutbox?>(),
                Arg.Do<IntegrationSyncOutbox?>(outbox => capturedOutbox = outbox))
            .Returns(callInfo => new EventRegistrationIntentCreationResult(callInfo.ArgAt<EventRegistrationIntent>(0), []));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedOutbox).IsNotNull();
        await Assert.That(capturedOutbox!.TenantId).IsEqualTo(tenantId);
        await Assert.That(capturedOutbox.Kind).IsEqualTo(IntegrationKind.Listmonk);
        await Assert.That(capturedOutbox.SourceType).IsEqualTo("event_registration_intent");
        await Assert.That(capturedOutbox.SourceId).IsEqualTo(result.Id);
        await Assert.That(capturedOutbox.EventId).IsEqualTo(eventId);
        await Assert.That(capturedOutbox.UserId).IsEqualTo(userId);
        await Assert.That(capturedOutbox.SubscriberEmail).IsEqualTo("registrant@example.test");
        await Assert.That(capturedOutbox.ListmonkListId).IsEqualTo(42);
        await Assert.That(capturedOutbox.Status).IsEqualTo(IntegrationSyncStatus.Pending);
    }

    [Test]
    public async Task HandleWhenEmailIsUnverifiedCreatesInAppFallbackWithoutEmailOutbox()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = CreateSessionRegistrationCommand(eventId, userId, sessionId);
        SetupValidRegistration(tenantId, eventId, userId, sessionId, CreateUser(userId, emailVerified: false));
        _notificationRepository.ExistsByDeduplicationKeyAsync(
                tenantId,
                userId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _notificationRepository.Create(Arg.Any<Notification>())
            .Returns(callInfo => callInfo.ArgAt<Notification>(0));

        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<CancellationToken>(),
                Arg.Any<EmailDispatchOutbox?>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo => new EventRegistrationIntentCreationResult(callInfo.ArgAt<EventRegistrationIntent>(0), []));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _intentRepository.Received(1).CreateWithChildrenAndCapacityAsync(
            Arg.Any<EventRegistrationIntent>(),
            Arg.Any<IReadOnlyList<EventRegistration>>(),
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            Arg.Any<CancellationToken>(),
            Arg.Is<EmailDispatchOutbox?>(outbox => outbox == null),
            Arg.Is<IntegrationSyncOutbox?>(outbox => outbox == null));
        await _notificationOrchestrator.DidNotReceive().EnqueueAsync(
            Arg.Any<NotificationIntentDraft>(),
            Arg.Any<CancellationToken>());
        await _notificationRepository.Received(1).Create(Arg.Is<Notification>(notification =>
            notification.TenantId == tenantId
            && notification.UserId == userId
            && notification.NotificationTypeId == (int)NotificationTypeEnum.RegistrationConfirmed
            && notification.NotificationEntityTypeId == (int)NotificationEntityTypeEnum.EventRegistration
            && notification.EntityId == result.Id.ToString()
            && notification.NotificationReasonId == (int)NotificationReasonEnum.System
            && notification.DeduplicationKey == $"event-registration-intent:{result.Id:N}:registration-confirmation:fallback"));
    }

    [Test]
    public async Task HandleWhenEmailIsMissingCreatesInAppFallbackWithoutFailingRegistration()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = CreateSessionRegistrationCommand(eventId, userId, sessionId);
        SetupValidRegistration(tenantId, eventId, userId, sessionId, CreateUser(userId, email: string.Empty, emailVerified: null));
        _notificationRepository.ExistsByDeduplicationKeyAsync(
                tenantId,
                userId,
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(false);
        _notificationRepository.Create(Arg.Any<Notification>())
            .Returns(callInfo => callInfo.ArgAt<Notification>(0));

        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<CancellationToken>(),
                Arg.Any<EmailDispatchOutbox?>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo => new EventRegistrationIntentCreationResult(callInfo.ArgAt<EventRegistrationIntent>(0), []));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _intentRepository.Received(1).CreateWithChildrenAndCapacityAsync(
            Arg.Any<EventRegistrationIntent>(),
            Arg.Any<IReadOnlyList<EventRegistration>>(),
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            Arg.Any<CancellationToken>(),
            Arg.Is<EmailDispatchOutbox?>(outbox => outbox == null),
            Arg.Is<IntegrationSyncOutbox?>(outbox => outbox == null));
        await _notificationRepository.Received(1).Create(Arg.Any<Notification>());
    }

    [Test]
    public async Task HandleWhenSessionIsFullReturnsWaitlistMessage()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = CreateSessionRegistrationCommand(eventId, userId, sessionId);
        SetupValidRegistration(tenantId, eventId, userId, sessionId);

        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<CancellationToken>(),
                Arg.Any<EmailDispatchOutbox?>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo =>
            {
                var intent = callInfo.ArgAt<EventRegistrationIntent>(0);
                intent.ApprovalStatusId = (int)ApprovalStatusEnum.Waitlisted;
                return new EventRegistrationIntentCreationResult(intent, [sessionId]);
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event Registration added to the waitlist.");
    }

    [Test]
    public async Task HandleWhenRepositoryReturnsExistingRaceWinnerReturnsAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var existingIntentId = Guid.NewGuid();
        var command = CreateSessionRegistrationCommand(eventId, userId, sessionId);
        command.EventRegistrationDto.ShareEmailWithOrganizer = true;
        command.EventRegistrationDto.ConsentTextAcknowledged = "Share my email with the organizer.";
        command.EventRegistrationDto.ConsentUiVersion = "v1";
        SetupValidRegistration(tenantId, eventId, userId, sessionId);

        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<CancellationToken>(),
                Arg.Any<EmailDispatchOutbox?>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(new EventRegistrationIntentCreationResult(
                new EventRegistrationIntent
                {
                    Id = existingIntentId,
                    EventId = eventId,
                    Event = null!,
                    UserId = userId,
                    User = null!,
                    RegistrationScopeId = (int)RegistrationScopeEnum.SessionSelection,
                    RegistrationScope = null!,
                    TenantId = tenantId,
                    Tenant = null!
                },
                [],
                WasExisting: true));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(existingIntentId);
        await Assert.That(result.Message).IsEqualTo("Event Registration already exists.");
        await _notificationOrchestrator.DidNotReceive().EnqueueAsync(
            Arg.Any<NotificationIntentDraft>(),
            Arg.Any<CancellationToken>());
        await _notificationRepository.DidNotReceive().Create(Arg.Any<Notification>());
        await _consentService.DidNotReceive().ProcessRegistrationConsent(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<string?>());
    }

    [Test]
    public async Task HandleWhenEventScopeOmitsSelectedSessionsCreatesRowsForEveryEventSession()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var firstSessionId = Guid.NewGuid();
        var secondSessionId = Guid.NewGuid();
        var command = new CreateEventRegistrationCommand
        {
            EventRegistrationDto = new CreateEventRegistrationDto
            {
                EventId = eventId,
                UserId = userId,
                RegistrationScopeId = (int)RegistrationScopeEnum.Event,
                SelectedSessionIds = null
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _eventRepository.Exists(eventId).Returns(true);
        _userRepository.Exists(userId).Returns(true);
        _userRepository.GetById(userId).Returns(CreateUser(userId));
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId, EventRegistrationPolicyEnum.WholeEventOnly));
        _eventSessionRepository.GetSessionsByEvent(eventId).Returns([
            CreateEventSession(eventId, tenantId, firstSessionId),
            CreateEventSession(eventId, tenantId, secondSessionId)
        ]);
        _intentRepository.FindExistingAsync(
                eventId,
                userId,
                (int)RegistrationScopeEnum.Event,
                null,
                Arg.Any<CancellationToken>())
            .Returns((EventRegistrationIntent?)null);
        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<CancellationToken>(),
                Arg.Any<EmailDispatchOutbox?>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo =>
            {
                var intent = callInfo.ArgAt<EventRegistrationIntent>(0);
                return new EventRegistrationIntentCreationResult(intent, []);
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _intentRepository.Received(1).CreateWithChildrenAndCapacityAsync(
            Arg.Any<EventRegistrationIntent>(),
            Arg.Is<IReadOnlyList<EventRegistration>>(children =>
                children != null
                && children.Count == 2
                && children.Select(child => child.EventSessionId).ToHashSet().SetEquals(new[] { firstSessionId, secondSessionId })),
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            Arg.Any<CancellationToken>(),
            Arg.Any<EmailDispatchOutbox?>(),
            Arg.Any<IntegrationSyncOutbox?>());
    }

    private void SetupValidRegistration(Guid tenantId, Guid eventId, Guid userId, Guid sessionId, User? user = null)
    {
        _tenantContext.TenantId.Returns(tenantId);
        _eventRepository.Exists(eventId).Returns(true);
        _userRepository.Exists(userId).Returns(true);
        _userRepository.GetById(userId).Returns(user ?? CreateUser(userId));
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId));
        _eventSessionRepository.GetSessionsByEvent(eventId).Returns([CreateEventSession(eventId, tenantId, sessionId)]);
        _intentRepository.FindExistingAsync(
                eventId,
                userId,
                (int)RegistrationScopeEnum.SessionSelection,
                null,
                Arg.Any<CancellationToken>())
            .Returns((EventRegistrationIntent?)null);
    }

    private static CreateEventRegistrationCommand CreateSessionRegistrationCommand(Guid eventId, Guid userId, Guid sessionId)
    {
        return new CreateEventRegistrationCommand
        {
            EventRegistrationDto = new CreateEventRegistrationDto
            {
                EventId = eventId,
                UserId = userId,
                RegistrationScopeId = (int)RegistrationScopeEnum.SessionSelection,
                SelectedSessionIds = [sessionId]
            }
        };
    }

    private static Explore.Domain.Event CreateEvent(
        Guid eventId,
        Guid tenantId,
        EventRegistrationPolicyEnum policy = EventRegistrationPolicyEnum.SessionSelectionOnly)
    {
        return new Explore.Domain.Event
        {
            Id = eventId,
            Title = "Capacity Test Event",
            Actor = null!,
            TenantId = tenantId,
            Tenant = null!,
            VisibilityTypeId = (int)VisibilityTypeEnum.Public,
            VisibilityType = null!,
            EventStatusId = (int)EventStatusEnum.Published,
            EventStatus = null!,
            EventFormatId = (int)EventFormatEnum.Local,
            EventFormat = null!,
            RegistrationPolicyId = (int)policy
        };
    }

    private static EventSession CreateEventSession(Guid eventId, Guid tenantId, Guid sessionId)
    {
        return new EventSession
        {
            Id = sessionId,
            EventId = eventId,
            Event = null!,
            TenantId = tenantId,
            Tenant = null!,
            StartTime = DateTimeOffset.UtcNow.AddDays(7),
            EndTime = DateTimeOffset.UtcNow.AddDays(7).AddHours(2)
        };
    }

    private static User CreateUser(Guid userId, string email = "registrant@example.test", bool? emailVerified = true)
    {
        var user = new User
        {
            Id = userId,
            EmailVerified = emailVerified,
            Pii = new UserPii
            {
                UserId = userId,
                Email = email,
                FirstName = "Test",
                LastName = "Registrant"
            }
        };

        user.Pii.User = user;
        return user;
    }

    private static BusinessMetrics CreateBusinessMetrics()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        var provider = services.BuildServiceProvider();
        return new BusinessMetrics(provider.GetRequiredService<IMeterFactory>());
    }
}
