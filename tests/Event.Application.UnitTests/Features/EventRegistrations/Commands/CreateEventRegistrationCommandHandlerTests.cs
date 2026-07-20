// ABOUTME: Unit tests for event registration creation capacity and waitlist behavior.
// ABOUTME: Verifies handlers call the capacity-aware repository contract and surface waitlist outcomes.

using System.Diagnostics.Metrics;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Contracts.Webhooks;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Features.EventRegistrations.Handlers.Commands;
using Explore.Application.Features.EventRegistrations.Requests.Commands;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Application.Services.Federation;
using Explore.Application.Settings;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Explore.Domain.Federation;
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
    private readonly IRecipientNotificationMaterializer _recipientNotificationMaterializer = Substitute.For<IRecipientNotificationMaterializer>();
    private readonly IEventLifecycleScheduler _eventLifecycleScheduler = Substitute.For<IEventLifecycleScheduler>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IListmonkRegistrationSyncOutboxFactory _listmonkFactory = Substitute.For<IListmonkRegistrationSyncOutboxFactory>();
    private readonly IWebhookEventPublisher _webhookPublisher = Substitute.For<IWebhookEventPublisher>();
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
        _webhookPublisher.PublishAsync(
                Arg.Any<WebhookEventBuildContext>(),
                Arg.Any<CancellationToken>())
            .Returns(WebhookEventPublishResult.SkippedResult("webhooks_disabled"));
        _unitOfWork.ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<EventRegistrationIntentCreationResult>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<CancellationToken, Task<EventRegistrationIntentCreationResult>>>(0)(
                call.ArgAt<CancellationToken>(1)));
        _recipientNotificationMaterializer.MaterializeInCurrentTransactionAsync(
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

        _handler = CreateHandler(AtprotoPublicationPlannerTestFactory.Disabled());
    }

    private CreateEventRegistrationCommandHandler CreateHandler(AtprotoEventPublicationPlanner planner) =>
        new(
            _intentRepository,
            _eventRepository,
            _userRepository,
            _eventDayRepository,
            _eventSessionRepository,
            _approvalStatusRepository,
            _tenantContext,
            CreateBusinessMetrics(),
            _consentService,
            _listmonkFactory,
            new RegistrationNotificationDeliveryService(new EventLifecycleEmailOutboxFactory()),
            _recipientNotificationMaterializer,
            _eventLifecycleScheduler,
            _unitOfWork,
            _webhookPublisher,
            Substitute.For<ILogger<CreateEventRegistrationCommandHandler>>(),
            planner);

    [Test]
    public async Task HandleWithEnabledAtprotoStagesRsvpInsideLocalTransactionWithoutPdsCall()
    {
        var tenantId = Guid.CreateVersion7();
        var eventId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        SetupValidRegistration(tenantId, eventId, userId, sessionId);
        EventRegistrationIntent? committedIntent = null;
        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(call =>
            {
                committedIntent = call.ArgAt<EventRegistrationIntent>(0);
                committedIntent.ConcurrencyStamp = Guid.CreateVersion7();
                committedIntent.CreatedAt = DateTime.UtcNow;
                return CreationResult(committedIntent, []);
            });
        _intentRepository.GetAtprotoLifecycleStateAsync(
                tenantId,
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => committedIntent);
        _intentRepository.CountActiveForEventUserAsync(
                tenantId,
                eventId,
                userId,
                Arg.Any<CancellationToken>())
            .Returns(1);

        var settings = Substitute.For<IHierarchicalSettingsResolver>();
        settings.ResolveBatchAsync(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SettingContext>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IEnumerable<string>>().Select(key => new ResolvedSetting
            {
                Key = key,
                Value = key == GovernanceSettingKeys.Federation.AtprotoEventValidationProfile
                    ? "\"platform\""
                    : "true",
                Source = SettingSource.UserPreference
            }).ToArray());
        var records = Substitute.For<IAtprotoRecordRepository>();
        var settledEvent = new AtprotoRecord
        {
            Id = Guid.CreateVersion7(),
            Did = "did:plc:organizer",
            Collection = AtprotoEventPublicationPlanner.EventCollection,
            RecordKey = "event-key",
            Uri = "at://did:plc:organizer/community.lexicon.calendar.event/event-key",
            Cid = "bafy-event",
            UpdatedAt = DateTime.UtcNow
        };
        records.GetOwnedRecordForSourceAsync(
                tenantId,
                AtprotoEventPublicationPlanner.EventSourceType,
                eventId,
                Arg.Any<CancellationToken>())
            .Returns(new AtprotoOutboundRecordOwnership
            {
                AtprotoRecordId = settledEvent.Id,
                TenantId = tenantId,
                UserId = Guid.CreateVersion7(),
                SourceEntityType = AtprotoEventPublicationPlanner.EventSourceType,
                SourceEntityId = eventId,
                SourceVersion = Guid.CreateVersion7(),
                AtprotoRecord = settledEvent
            });
        var sessions = Substitute.For<IUserAuthenticationTokenRepository>();
        sessions.GetAtprotoSessionsForReadAsync(
                tenantId,
                userId,
                RepositoryBackedAtprotoSession.Provider,
                Arg.Any<CancellationToken>())
            .Returns([
                new UserAuthenticationToken
                {
                    Id = Guid.CreateVersion7(),
                    TenantId = tenantId,
                    Tenant = null!,
                    UserId = userId,
                    User = null!,
                    Provider = RepositoryBackedAtprotoSession.Provider,
                    SubjectDid = "did:plc:attendee",
                    SessionCiphertext = [1],
                    EncryptionKeyId = "enc",
                    OAuthClientKeyId = "oauth",
                    PdsHost = "https://pds.example/"
                }
            ]);
        var logins = Substitute.For<IUserExternalLoginRepository>();
        logins.GetByProviderAndKey(RepositoryBackedAtprotoSession.Provider, "did:plc:attendee")
            .Returns(new UserExternalLogin
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                Tenant = null!,
                UserId = userId,
                User = null!,
                Provider = RepositoryBackedAtprotoSession.Provider,
                ProviderKey = "did:plc:attendee"
            });
        var payloads = Substitute.For<IAtprotoPublicationPayloadBuilder>();
        payloads.BuildRsvp(Arg.Any<Explore.Application.Features.Federation.Atproto.Models.AtprotoRsvpPublicationSnapshot>())
            .Returns(AtprotoPublicationPayloadBuildResult.Valid(new("{}", "hash")));
        var outbox = Substitute.For<IPdsSyncOutboxRepository>();
        var pdsGateway = Substitute.For<IAtprotoPdsDeliveryGateway>();
        var insideTransaction = false;
        var addedInsideTransaction = false;
        _unitOfWork.ExecuteSerializableAsync(
                Arg.Any<Func<CancellationToken, Task<EventRegistrationIntentCreationResult>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                insideTransaction = true;
                try
                {
                    return await call.ArgAt<Func<CancellationToken, Task<EventRegistrationIntentCreationResult>>>(0)(
                        call.ArgAt<CancellationToken>(1));
                }
                finally
                {
                    insideTransaction = false;
                }
            });
        outbox.AddAsync(Arg.Any<PdsSyncOutbox>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                addedInsideTransaction = insideTransaction;
                return Task.CompletedTask;
            });
        var planner = new AtprotoEventPublicationPlanner(
            new AtprotoEventGovernanceResolver(settings),
            _eventRepository,
            _intentRepository,
            records,
            sessions,
            logins,
            payloads,
            outbox,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AtprotoEventPublicationPlanner>.Instance);

        var result = await CreateHandler(planner).Handle(
            CreateSessionRegistrationCommand(eventId, userId, sessionId),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(addedInsideTransaction).IsTrue();
        await outbox.Received(1).AddAsync(
            Arg.Is<PdsSyncOutbox>(row =>
                row.Operation == PdsSyncOperation.Create
                && row.Collection == AtprotoEventPublicationPlanner.RsvpCollection
                && row.DependsOnAtprotoRecordId == settledEvent.Id),
            Arg.Any<CancellationToken>());
        await pdsGateway.DidNotReceiveWithAnyArgs().DeliverAsync(default!, default);
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
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo =>
            {
                var intent = callInfo.ArgAt<EventRegistrationIntent>(0);
                return CreationResult(intent, []);
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event Registration created successfully.");
        await _recipientNotificationMaterializer.Received(1).MaterializeInCurrentTransactionAsync(
            Arg.Is<RecipientNotificationMaterialization>(request =>
                request.Intent.Category == AppNotificationCategory.RegistrationLifecycle
                && request.Intent.TenantId == tenantId
                && request.Intent.TemplateKey == "registration.confirmation"
                && request.Intent.UserId == userId
                && request.Intent.EventId == eventId
                && request.InApp != null
                && request.Email != null
                && request.Email.RecipientEmail == "registrant@example.test"
                && request.Email.RecipientAddressSource == RecipientAddressSource.TenantUserVerifiedEmail),
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
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>(),
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
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Do<IntegrationSyncOutbox?>(outbox => capturedOutbox = outbox))
            .Returns(callInfo => CreationResult(callInfo.ArgAt<EventRegistrationIntent>(0), []));

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
    public async Task HandleWhenContactShareConsentIsFalsePublishesWebhookWithoutAttendeePii()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = CreateSessionRegistrationCommand(eventId, userId, sessionId);
        SetupValidRegistration(tenantId, eventId, userId, sessionId);
        WebhookEventBuildContext? capturedContext = null;

        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo => CreationResult(callInfo.ArgAt<EventRegistrationIntent>(0), []));
        _webhookPublisher.PublishAsync(
                Arg.Do<WebhookEventBuildContext>(context => capturedContext = context),
                Arg.Any<CancellationToken>())
            .Returns(WebhookEventPublishResult.Success(Guid.CreateVersion7()));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedContext).IsNotNull();
        await Assert.That(capturedContext!.EventType).IsEqualTo(WebhookEventNames.RegistrationCreated);
        await Assert.That(capturedContext.TenantId).IsEqualTo(tenantId);
        await Assert.That(capturedContext.Data["registrationId"]).IsEqualTo(result.Id.ToString());
        await Assert.That(capturedContext.Data["eventId"]).IsEqualTo(eventId.ToString());
        await Assert.That(capturedContext.Data["status"]).IsEqualTo("Approved");
        await Assert.That(capturedContext.Data["consentToEmailShare"]).IsEqualTo(false);
        await Assert.That(capturedContext.Data.ContainsKey("attendeeEmail")).IsFalse();
        await Assert.That(capturedContext.Data.ContainsKey("attendeeFirstName")).IsFalse();
        await Assert.That(capturedContext.Data.ContainsKey("attendeeLastName")).IsFalse();
    }

    [Test]
    public async Task HandleWhenContactShareConsentIsTruePublishesWebhookWithAttendeePii()
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
        WebhookEventBuildContext? capturedContext = null;

        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                (int)ApprovalStatusEnum.Approved,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo => CreationResult(callInfo.ArgAt<EventRegistrationIntent>(0), []));
        _webhookPublisher.PublishAsync(
                Arg.Do<WebhookEventBuildContext>(context => capturedContext = context),
                Arg.Any<CancellationToken>())
            .Returns(WebhookEventPublishResult.Success(Guid.CreateVersion7()));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedContext).IsNotNull();
        await Assert.That(capturedContext!.Data["consentToEmailShare"]).IsEqualTo(true);
        await Assert.That(capturedContext.Data["attendeeEmail"]).IsEqualTo("registrant@example.test");
        await Assert.That(capturedContext.Data["attendeeFirstName"]).IsEqualTo("Test");
        await Assert.That(capturedContext.Data["attendeeLastName"]).IsEqualTo("Registrant");
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
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo => CreationResult(callInfo.ArgAt<EventRegistrationIntent>(0), []));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _intentRepository.Received(1).CreateWithChildrenAndCapacityAsync(
            Arg.Any<EventRegistrationIntent>(),
            Arg.Any<IReadOnlyList<EventRegistration>>(),
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>(),
            Arg.Is<IntegrationSyncOutbox?>(outbox => outbox == null));
        await _recipientNotificationMaterializer.Received(1).MaterializeInCurrentTransactionAsync(
            Arg.Is<RecipientNotificationMaterialization>(request =>
                request.InApp != null
                && request.Email == null
                && request.EmailSkipReason == "recipient_email_unverified"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleWhenEmailIsMissingCreatesInAppFallbackWithoutFailingRegistration()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = CreateSessionRegistrationCommand(eventId, userId, sessionId);
        SetupValidRegistration(tenantId, eventId, userId, sessionId, CreateUser(userId, email: string.Empty, emailVerified: true));
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
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo => CreationResult(callInfo.ArgAt<EventRegistrationIntent>(0), []));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _intentRepository.Received(1).CreateWithChildrenAndCapacityAsync(
            Arg.Any<EventRegistrationIntent>(),
            Arg.Any<IReadOnlyList<EventRegistration>>(),
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>(),
            Arg.Is<IntegrationSyncOutbox?>(outbox => outbox == null));
        await _recipientNotificationMaterializer.Received(1).MaterializeInCurrentTransactionAsync(
            Arg.Is<RecipientNotificationMaterialization>(request =>
                request.InApp != null
                && request.Email == null
                && request.EmailSkipReason == "recipient_email_missing"),
            Arg.Any<CancellationToken>());
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
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo =>
            {
                var intent = callInfo.ArgAt<EventRegistrationIntent>(0);
                intent.ApprovalStatusId = (int)ApprovalStatusEnum.Waitlisted;
                return CreationResult(intent, [sessionId]);
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event Registration added to the waitlist.");
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments((int)RegistrationModeEnum.Open, true, (int)ApprovalStatusEnum.Approved)]
    [Arguments((int)RegistrationModeEnum.ApprovalRequired, true, (int)ApprovalStatusEnum.Pending)]
    [Arguments((int)RegistrationModeEnum.InviteOnly, false, 0)]
    [Arguments((int)RegistrationModeEnum.Closed, false, 0)]
    public async Task HandleWithNullApprovalDerivesFailClosedStateFromRegistrationMode(
        int registrationModeId,
        bool shouldCreate,
        int expectedApprovalStatusId)
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var command = CreateSessionRegistrationCommand(eventId, userId, sessionId);
        SetupValidRegistration(tenantId, eventId, userId, sessionId);
        var session = CreateEventSession(eventId, tenantId, sessionId);
        session.RegistrationModeId = registrationModeId;
        session.RegistrationMode = new RegistrationMode
        {
            Id = registrationModeId,
            MasterCode = ((RegistrationModeEnum)registrationModeId).ToString().ToUpperInvariant(),
            FullName = ((RegistrationModeEnum)registrationModeId).ToString()
        };
        _eventSessionRepository.GetSessionsByEvent(eventId).Returns([session]);

        EventRegistrationIntent? capturedIntent = null;
        IReadOnlyList<EventRegistration>? capturedChildren = null;
        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Do<EventRegistrationIntent>(intent => capturedIntent = intent),
                Arg.Do<IReadOnlyList<EventRegistration>>(children => capturedChildren = children),
                Arg.Any<int>(),
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo => CreationResult(
                callInfo.ArgAt<EventRegistrationIntent>(0),
                []));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsEqualTo(shouldCreate);
        if (shouldCreate)
        {
            await Assert.That(capturedIntent!.ApprovalStatusId).IsEqualTo(expectedApprovalStatusId);
            await Assert.That(capturedChildren).IsNotNull();
            await Assert.That(capturedChildren!.Count).IsEqualTo(1);
            await Assert.That(capturedChildren[0].ApprovalStatusId).IsEqualTo(expectedApprovalStatusId);
            await _intentRepository.Received(1).CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                expectedApprovalStatusId,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>());
        }
        else
        {
            await _intentRepository.DidNotReceive().CreateWithChildrenAndCapacityAsync(
                Arg.Any<EventRegistrationIntent>(),
                Arg.Any<IReadOnlyList<EventRegistration>>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>());
        }
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task HandleAcrossOpenAndApprovalRequiredSessionsUsesMostRestrictiveInitialState()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var openSession = CreateEventSession(eventId, tenantId, Guid.NewGuid(), RegistrationModeEnum.Open);
        var approvalSession = CreateEventSession(eventId, tenantId, Guid.NewGuid(), RegistrationModeEnum.ApprovalRequired);
        var command = new CreateEventRegistrationCommand
        {
            EventRegistrationDto = new CreateEventRegistrationDto
            {
                EventId = eventId,
                UserId = userId,
                RegistrationScopeId = (int)RegistrationScopeEnum.Event
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _eventRepository.Exists(eventId).Returns(true);
        _userRepository.Exists(userId).Returns(true);
        _userRepository.GetById(userId).Returns(CreateUser(userId));
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId, EventRegistrationPolicyEnum.WholeEventOnly));
        _eventSessionRepository.GetSessionsByEvent(eventId).Returns([openSession, approvalSession]);
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
                (int)ApprovalStatusEnum.Pending,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo => CreationResult(
                callInfo.ArgAt<EventRegistrationIntent>(0),
                []));
        WebhookEventBuildContext? webhookContext = null;
        _webhookPublisher.PublishAsync(
                Arg.Do<WebhookEventBuildContext>(context => webhookContext = context),
                Arg.Any<CancellationToken>())
            .Returns(WebhookEventPublishResult.Success(Guid.CreateVersion7()));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Event Registration submitted for approval.");
        await Assert.That(webhookContext!.Data["status"]).IsEqualTo("Pending");
        await _intentRepository.Received(1).CreateWithChildrenAndCapacityAsync(
            Arg.Is<EventRegistrationIntent>(intent => intent.ApprovalStatusId == (int)ApprovalStatusEnum.Pending),
            Arg.Is<IReadOnlyList<EventRegistration>>(children =>
                children.Count == 2
                && children.All(child => child.ApprovalStatusId == (int)ApprovalStatusEnum.Pending)),
            (int)ApprovalStatusEnum.Pending,
            (int)ApprovalStatusEnum.Waitlisted,
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<IntegrationSyncOutbox?>());
    }

    [Test]
    [Category("EventLocationPrivacy")]
    public async Task HandleWhenAnyCoveredSessionHasNoRegistrationModeDeniesWithoutMutation()
    {
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var openSession = CreateEventSession(eventId, tenantId, Guid.NewGuid(), RegistrationModeEnum.Open);
        var unknownSession = CreateEventSession(eventId, tenantId, Guid.NewGuid());
        unknownSession.RegistrationModeId = null;
        unknownSession.RegistrationMode = null;
        var command = new CreateEventRegistrationCommand
        {
            EventRegistrationDto = new CreateEventRegistrationDto
            {
                EventId = eventId,
                UserId = userId,
                RegistrationScopeId = (int)RegistrationScopeEnum.Event
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _eventRepository.Exists(eventId).Returns(true);
        _userRepository.Exists(userId).Returns(true);
        _userRepository.GetById(userId).Returns(CreateUser(userId));
        _eventRepository.GetById(eventId).Returns(CreateEvent(eventId, tenantId, EventRegistrationPolicyEnum.WholeEventOnly));
        _eventSessionRepository.GetSessionsByEvent(eventId).Returns([openSession, unknownSession]);
        _intentRepository.FindExistingAsync(
                eventId,
                userId,
                (int)RegistrationScopeEnum.Event,
                null,
                Arg.Any<CancellationToken>())
            .Returns((EventRegistrationIntent?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _intentRepository.DidNotReceive().CreateWithChildrenAndCapacityAsync(
            Arg.Any<EventRegistrationIntent>(),
            Arg.Any<IReadOnlyList<EventRegistration>>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<IntegrationSyncOutbox?>());
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
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(CreationResult(
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
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo =>
            {
                var intent = callInfo.ArgAt<EventRegistrationIntent>(0);
                return CreationResult(intent, []);
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
            Arg.Any<Guid>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<EventRegistrationActorProvenance>(),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>(),
            Arg.Any<IntegrationSyncOutbox?>());
    }

    [Test]
    [Category("EventLocationPrivacy")]
    [Arguments((int)RegistrationScopeEnum.Event)]
    [Arguments((int)RegistrationScopeEnum.Day)]
    [Arguments((int)RegistrationScopeEnum.SessionSelection)]
    public async Task HandleWithNullApprovalDerivesPendingCapacityRowsForEveryScope(int scopeId)
    {
        var scope = (RegistrationScopeEnum)scopeId;
        var tenantId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var selectedDayId = Guid.NewGuid();
        var otherDayId = Guid.NewGuid();
        var selectedSession = CreateEventSession(
            eventId,
            tenantId,
            Guid.NewGuid(),
            RegistrationModeEnum.ApprovalRequired);
        selectedSession.EventDayId = selectedDayId;
        var otherSession = CreateEventSession(
            eventId,
            tenantId,
            Guid.NewGuid(),
            RegistrationModeEnum.ApprovalRequired);
        otherSession.EventDayId = otherDayId;
        var dto = new CreateEventRegistrationDto
        {
            EventId = eventId,
            UserId = userId,
            RegistrationScopeId = scopeId,
            ApprovalStatusId = null,
            SelectedEventDayId = scope == RegistrationScopeEnum.Day ? selectedDayId : null,
            SelectedSessionIds = scope == RegistrationScopeEnum.SessionSelection
                ? [selectedSession.Id]
                : null
        };
        var command = new CreateEventRegistrationCommand { EventRegistrationDto = dto };

        _tenantContext.TenantId.Returns(tenantId);
        _eventRepository.Exists(eventId).Returns(true);
        _userRepository.Exists(userId).Returns(true);
        _userRepository.GetById(userId).Returns(CreateUser(userId));
        _eventRepository.GetById(eventId).Returns(CreateEvent(
            eventId,
            tenantId,
            EventRegistrationPolicyEnum.Flexible));
        _eventSessionRepository.GetSessionsByEvent(eventId).Returns([selectedSession, otherSession]);
        if (scope == RegistrationScopeEnum.Day)
        {
            _eventDayRepository.BelongsToEventAsync(
                    selectedDayId,
                    eventId,
                    Arg.Any<CancellationToken>())
                .Returns(true);
        }

        _intentRepository.FindExistingAsync(
                eventId,
                userId,
                scopeId,
                dto.SelectedEventDayId,
                Arg.Any<CancellationToken>())
            .Returns((EventRegistrationIntent?)null);
        EventRegistrationIntent? capturedIntent = null;
        IReadOnlyList<EventRegistration>? capturedChildren = null;
        _intentRepository.CreateWithChildrenAndCapacityAsync(
                Arg.Do<EventRegistrationIntent>(intent => capturedIntent = intent),
                Arg.Do<IReadOnlyList<EventRegistration>>(children => capturedChildren = children),
                (int)ApprovalStatusEnum.Pending,
                (int)ApprovalStatusEnum.Waitlisted,
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<EventRegistrationActorProvenance>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>(),
                Arg.Any<IntegrationSyncOutbox?>())
            .Returns(callInfo => CreationResult(
                callInfo.ArgAt<EventRegistrationIntent>(0),
                []));

        var result = await _handler.Handle(command, CancellationToken.None);

        var expectedSessionIds = scope == RegistrationScopeEnum.Event
            ? new[] { selectedSession.Id, otherSession.Id }.ToHashSet()
            : new[] { selectedSession.Id }.ToHashSet();
        await Assert.That(result.Success).IsTrue();
        await Assert.That(capturedIntent).IsNotNull();
        await Assert.That(capturedIntent!.ApprovalStatusId).IsEqualTo((int)ApprovalStatusEnum.Pending);
        await Assert.That(capturedIntent.RegistrationScopeId).IsEqualTo(scopeId);
        await Assert.That(capturedIntent.SelectedEventDayId).IsEqualTo(dto.SelectedEventDayId);
        await Assert.That(capturedChildren).IsNotNull();
        await Assert.That(capturedChildren!.Select(child => child.EventSessionId).ToHashSet()
            .SetEquals(expectedSessionIds)).IsTrue();
        await Assert.That(capturedChildren.All(child =>
            child.ApprovalStatusId == (int)ApprovalStatusEnum.Pending)).IsTrue();
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

    private static EventSession CreateEventSession(
        Guid eventId,
        Guid tenantId,
        Guid sessionId,
        RegistrationModeEnum registrationMode = RegistrationModeEnum.Open)
    {
        return new EventSession
        {
            Id = sessionId,
            EventId = eventId,
            Event = null!,
            TenantId = tenantId,
            Tenant = null!,
            StartTime = DateTimeOffset.UtcNow.AddDays(7),
            EndTime = DateTimeOffset.UtcNow.AddDays(7).AddHours(2),
            RegistrationModeId = (int)registrationMode,
            RegistrationMode = new RegistrationMode
            {
                Id = (int)registrationMode,
                MasterCode = registrationMode.ToString().ToUpperInvariant(),
                FullName = registrationMode.ToString()
            }
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

    private static EventRegistrationIntentCreationResult CreationResult(
        EventRegistrationIntent intent,
        IReadOnlyList<Guid> waitlistedSessionIds,
        bool WasExisting = false)
    {
        if (intent.Id == Guid.Empty)
        {
            intent.Id = Guid.CreateVersion7();
        }

        if (intent.ConcurrencyStamp == Guid.Empty)
        {
            intent.ConcurrencyStamp = Guid.CreateVersion7();
        }

        if (intent.CreatedAt == default)
        {
            intent.CreatedAt = DateTime.UtcNow;
        }

        return new EventRegistrationIntentCreationResult(
            intent,
            waitlistedSessionIds,
            new EventRegistrationTransitionResult(
                Changed: true,
                ParentIntentId: intent.Id,
                PreviousStatus: null,
                FinalStatus: intent.ApprovalStatusId,
                TransitionReason: waitlistedSessionIds.Count == 0
                    ? EventRegistrationTransitionReason.Created
                    : EventRegistrationTransitionReason.CapacityWaitlisted,
                OccurrenceId: Guid.CreateVersion7(),
                OccurredAt: DateTimeOffset.UtcNow,
                ActorProvenance: EventRegistrationActorProvenance.Attendee,
                ActorUserId: intent.UserId,
                ChildTransitions: []),
            WasExisting);
    }

    private static BusinessMetrics CreateBusinessMetrics()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        var provider = services.BuildServiceProvider();
        return new BusinessMetrics(provider.GetRequiredService<IMeterFactory>());
    }
}
