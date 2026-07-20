// ABOUTME: Unit tests for transaction-bound approved-registration reminder preparation.
// ABOUTME: Proves recipient graph authority precedes pointer-only post-commit scheduling.

using Explore.Application.Configuration;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;

namespace Event.Application.UnitTests.Services;

public sealed class EventLifecycleSchedulerTests
{
    [Test]
    public async Task PrepareApprovedReminderMaterializesOptionalChannelsWithoutTriggeringScheduler()
    {
        DateTimeOffset referenceAt = new(2026, 7, 20, 8, 0, 0, TimeSpan.Zero);
        EventRegistrationIntent intent = CreateIntent();
        EventSession earliestApprovedSession = CreateSession(intent, referenceAt.AddDays(2));
        EventSession promotedSession = CreateSession(intent, referenceAt.AddDays(3));
        User recipient = CreateRecipient(intent.UserId, "attendee@example.test");
        EventReminderGraphIds graphIds = EventReminderGraphIds.Create();
        IEventRegistrationIntentRepository repository = Substitute.For<IEventRegistrationIntentRepository>();
        IRecipientNotificationMaterializer materializer = Substitute.For<IRecipientNotificationMaterializer>();
        IScheduledEmailDispatchTrigger trigger = Substitute.For<IScheduledEmailDispatchTrigger>();
        INotificationPreferenceResolver preferenceResolver = CreatePreferenceResolver(inAppEnabled: true, emailEnabled: true);
        RecipientNotificationMaterialization? captured = null;
        repository.GetEarliestApprovedReminderSessionAsync(
                intent.TenantId,
                intent.Id,
                referenceAt,
                Arg.Any<CancellationToken>())
            .Returns(earliestApprovedSession);
        materializer.MaterializeInCurrentTransactionAsync(
                Arg.Any<RecipientNotificationMaterialization>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.ArgAt<RecipientNotificationMaterialization>(0);
                return CreateMaterializationResult(captured);
            });
        var scheduler = CreateScheduler(repository, materializer, preferenceResolver, trigger, leadTimeHours: 24);

        EventReminderPreparedSchedule? prepared = await scheduler.PrepareEventReminderInCurrentTransactionAsync(
            new EventReminderPreparationInput(
                intent,
                CreateApprovedTransition(intent, promotedSession.Id, referenceAt),
                recipient,
                "Community Dinner",
                referenceAt,
                graphIds),
            CancellationToken.None);

        await Assert.That(prepared).IsNotNull();
        await Assert.That(prepared!.SessionId).IsEqualTo(earliestApprovedSession.Id);
        await Assert.That(prepared.DispatchAt).IsEqualTo(earliestApprovedSession.StartTime!.Value.AddHours(-24));
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Intent.DeduplicationKey)
            .IsEqualTo($"event-registration-intent:{intent.Id:N}:session:{earliestApprovedSession.Id:N}:event-reminder");
        await Assert.That(captured.DeliveryPolicy).IsEqualTo(NotificationDeliveryPolicyEnum.ReminderOptional);
        await Assert.That(captured.IncludeInAppChannel).IsTrue();
        await Assert.That(captured.InAppPreferenceEnabled).IsTrue();
        await Assert.That(captured.InApp!.IsRequired).IsFalse();
        await Assert.That(captured.IncludeEmailChannel).IsTrue();
        await Assert.That(captured.EmailRequired).IsFalse();
        await Assert.That(captured.EmailPreferenceEnabled).IsTrue();
        await Assert.That(captured.Email!.Id).IsEqualTo(graphIds.EmailDispatchOutboxId);
        await Assert.That(captured.Email.PublishEventId).IsEqualTo(graphIds.PublishEventId);
        await Assert.That(captured.Email.NextAttemptAt).IsEqualTo(prepared.DispatchAt.UtcDateTime);
        await trigger.DidNotReceiveWithAnyArgs().ScheduleAsync(default!, default, default);
    }

    [Test]
    public async Task PreparePastDueFutureSessionClampsToTransactionReferenceAndPersistsSkippedInAppDecision()
    {
        DateTimeOffset referenceAt = new(2026, 7, 20, 8, 0, 0, TimeSpan.Zero);
        EventRegistrationIntent intent = CreateIntent();
        EventSession session = CreateSession(intent, referenceAt.AddHours(2));
        IEventRegistrationIntentRepository repository = Substitute.For<IEventRegistrationIntentRepository>();
        IRecipientNotificationMaterializer materializer = Substitute.For<IRecipientNotificationMaterializer>();
        RecipientNotificationMaterialization? captured = null;
        repository.GetEarliestApprovedReminderSessionAsync(
                intent.TenantId,
                intent.Id,
                referenceAt,
                Arg.Any<CancellationToken>())
            .Returns(session);
        materializer.MaterializeInCurrentTransactionAsync(
                Arg.Any<RecipientNotificationMaterialization>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.ArgAt<RecipientNotificationMaterialization>(0);
                return CreateMaterializationResult(captured);
            });
        var scheduler = CreateScheduler(
            repository,
            materializer,
            CreatePreferenceResolver(inAppEnabled: false, emailEnabled: true),
            Substitute.For<IScheduledEmailDispatchTrigger>(),
            leadTimeHours: 24);

        EventReminderPreparedSchedule? prepared = await scheduler.PrepareEventReminderInCurrentTransactionAsync(
            new EventReminderPreparationInput(
                intent,
                CreateApprovedTransition(intent, session.Id, referenceAt),
                CreateRecipient(intent.UserId, "attendee@example.test"),
                "Community Dinner",
                referenceAt,
                EventReminderGraphIds.Create()),
            CancellationToken.None);

        await Assert.That(prepared).IsNotNull();
        await Assert.That(prepared!.DispatchAt).IsEqualTo(referenceAt);
        await Assert.That(captured!.IncludeInAppChannel).IsTrue();
        await Assert.That(captured.InApp).IsNull();
        await Assert.That(captured.InAppPreferenceEnabled).IsFalse();
        await Assert.That(captured.InAppSkipReason).IsEqualTo("in_app_preference_disabled");
    }

    [Test]
    public async Task PrepareEmailPreferenceDisabledPersistsTypedSkipWithoutOutboxOrPointer()
    {
        DateTimeOffset referenceAt = new(2026, 7, 20, 8, 0, 0, TimeSpan.Zero);
        EventRegistrationIntent intent = CreateIntent();
        EventSession session = CreateSession(intent, referenceAt.AddDays(2));
        IEventRegistrationIntentRepository repository = Substitute.For<IEventRegistrationIntentRepository>();
        IRecipientNotificationMaterializer materializer = Substitute.For<IRecipientNotificationMaterializer>();
        IScheduledEmailDispatchTrigger trigger = Substitute.For<IScheduledEmailDispatchTrigger>();
        RecipientNotificationMaterialization? captured = null;
        repository.GetEarliestApprovedReminderSessionAsync(
                intent.TenantId,
                intent.Id,
                referenceAt,
                Arg.Any<CancellationToken>())
            .Returns(session);
        materializer.MaterializeInCurrentTransactionAsync(
                Arg.Any<RecipientNotificationMaterialization>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.ArgAt<RecipientNotificationMaterialization>(0);
                return CreateMaterializationResult(captured);
            });
        var scheduler = CreateScheduler(
            repository,
            materializer,
            CreatePreferenceResolver(inAppEnabled: true, emailEnabled: false),
            trigger,
            leadTimeHours: 24);

        EventReminderPreparedSchedule? prepared = await scheduler.PrepareEventReminderInCurrentTransactionAsync(
            new EventReminderPreparationInput(
                intent,
                CreateApprovedTransition(intent, session.Id, referenceAt),
                CreateRecipient(intent.UserId, "sensitive-address@example.test"),
                "Sensitive title snapshot",
                referenceAt,
                EventReminderGraphIds.Create()),
            CancellationToken.None);
        if (prepared is not null)
        {
            await scheduler.TriggerPreparedEventReminderAsync(prepared, CancellationToken.None);
        }

        await Assert.That(prepared).IsNull();
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.Email).IsNull();
        await Assert.That(captured.EmailPreferenceEnabled).IsFalse();
        await Assert.That(captured.EmailSkipReason).IsEqualTo("email_preference_disabled");
        await trigger.DidNotReceiveWithAnyArgs().ScheduleAsync(default!, default, default);
    }

    [Test]
    public async Task TriggerPreparedReminderSchedulesOnlyThePersistedPointer()
    {
        IScheduledEmailDispatchTrigger trigger = Substitute.For<IScheduledEmailDispatchTrigger>();
        Guid schedulerJobId = Guid.CreateVersion7();
        trigger.ScheduleAsync(
                Arg.Any<ScheduledEmailDispatchPointer>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(ScheduledEmailDispatchTriggerResult.Success(schedulerJobId));
        var scheduler = CreateScheduler(
            Substitute.For<IEventRegistrationIntentRepository>(),
            Substitute.For<IRecipientNotificationMaterializer>(),
            CreatePreferenceResolver(true, true),
            trigger,
            leadTimeHours: 24);
        var prepared = new EventReminderPreparedSchedule(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow.AddDays(2),
            DateTimeOffset.UtcNow.AddDays(1));

        EventLifecycleScheduleResult result = await scheduler.TriggerPreparedEventReminderAsync(
            prepared,
            CancellationToken.None);

        await trigger.Received(1).ScheduleAsync(
            Arg.Is<ScheduledEmailDispatchPointer>(pointer =>
                pointer.TenantId == prepared.TenantId
                && pointer.PublishEventId == prepared.PublishEventId
                && pointer.UseCase == EventLifecycleAutomationUseCases.EventReminder
                && pointer.EventId == prepared.EventId
                && pointer.RegistrationIntentId == prepared.RegistrationIntentId),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
        await Assert.That(result.EmailDispatchOutboxId).IsEqualTo(prepared.EmailDispatchOutboxId);
        await Assert.That(result.SchedulerJobId).IsEqualTo(schedulerJobId);
    }

    [Test]
    public async Task PrepareAlreadyStartedOrNotNewlyApprovedCreatesNothing()
    {
        DateTimeOffset referenceAt = new(2026, 7, 20, 8, 0, 0, TimeSpan.Zero);
        EventRegistrationIntent intent = CreateIntent();
        IEventRegistrationIntentRepository repository = Substitute.For<IEventRegistrationIntentRepository>();
        IRecipientNotificationMaterializer materializer = Substitute.For<IRecipientNotificationMaterializer>();
        repository.GetEarliestApprovedReminderSessionAsync(
                intent.TenantId,
                intent.Id,
                referenceAt,
                Arg.Any<CancellationToken>())
            .Returns(CreateSession(intent, referenceAt.AddMinutes(-1)));
        var scheduler = CreateScheduler(
            repository,
            materializer,
            CreatePreferenceResolver(true, true),
            Substitute.For<IScheduledEmailDispatchTrigger>(),
            leadTimeHours: 24);

        EventReminderPreparedSchedule? started = await scheduler.PrepareEventReminderInCurrentTransactionAsync(
            new EventReminderPreparationInput(
                intent,
                CreateApprovedTransition(intent, Guid.CreateVersion7(), referenceAt),
                CreateRecipient(intent.UserId, "attendee@example.test"),
                "Community Dinner",
                referenceAt,
                EventReminderGraphIds.Create()),
            CancellationToken.None);
        EventRegistrationTransitionResult noTargetApproval = CreateApprovedTransition(
            intent,
            Guid.CreateVersion7(),
            referenceAt) with
        {
            PreviousStatus = (int)ApprovalStatusEnum.Waitlisted,
            ChildTransitions =
            [
                new EventRegistrationChildTransition(
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    (int)ApprovalStatusEnum.Approved,
                    (int)ApprovalStatusEnum.Cancelled)
            ]
        };
        EventReminderPreparedSchedule? replay = await scheduler.PrepareEventReminderInCurrentTransactionAsync(
            new EventReminderPreparationInput(
                intent,
                noTargetApproval,
                CreateRecipient(intent.UserId, "attendee@example.test"),
                "Community Dinner",
                referenceAt,
                EventReminderGraphIds.Create()),
            CancellationToken.None);

        await Assert.That(started).IsNull();
        await Assert.That(replay).IsNull();
        await materializer.DidNotReceiveWithAnyArgs()
            .MaterializeInCurrentTransactionAsync(default!, default);
    }

    [Test]
    public async Task PrepareRejectsLeadTimeOutsideInclusiveRangeDefensively()
    {
        DateTimeOffset referenceAt = DateTimeOffset.UtcNow;
        EventRegistrationIntent intent = CreateIntent();
        IEventRegistrationIntentRepository repository = Substitute.For<IEventRegistrationIntentRepository>();
        EventSession targetSession = CreateSession(intent, referenceAt.AddDays(2));
        repository.GetEarliestApprovedReminderSessionAsync(
                intent.TenantId,
                intent.Id,
                referenceAt,
                Arg.Any<CancellationToken>())
            .Returns(targetSession);
        var scheduler = CreateScheduler(
            repository,
            Substitute.For<IRecipientNotificationMaterializer>(),
            CreatePreferenceResolver(true, true),
            Substitute.For<IScheduledEmailDispatchTrigger>(),
            leadTimeHours: 169);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scheduler.PrepareEventReminderInCurrentTransactionAsync(
                new EventReminderPreparationInput(
                    intent,
                    CreateApprovedTransition(intent, targetSession.Id, referenceAt),
                    CreateRecipient(intent.UserId, "attendee@example.test"),
                    "Community Dinner",
                    referenceAt,
                    EventReminderGraphIds.Create()),
                CancellationToken.None));
    }

    [Test]
    public async Task ReminderAuthorityReferenceAcceptsOnlyItsCanonicalRoundTrip()
    {
        Guid sessionId = Guid.Parse("018e4e5c-7f00-7000-8000-abcdef123456");
        DateTimeOffset sessionStartUtc = new(2026, 10, 25, 1, 30, 0, TimeSpan.Zero);
        string canonical = EventReminderAuthorityReference.Format(
            sessionId,
            sessionStartUtc,
            "Europe/Brussels");

        bool parsed = EventReminderAuthorityReference.TryParse(
            canonical,
            out Guid parsedSessionId,
            out DateTimeOffset parsedStartUtc,
            out string parsedTimeZoneId);

        await Assert.That(parsed).IsTrue();
        await Assert.That(parsedSessionId).IsEqualTo(sessionId);
        await Assert.That(parsedStartUtc).IsEqualTo(sessionStartUtc);
        await Assert.That(parsedTimeZoneId).IsEqualTo("Europe/Brussels");
    }

    [Test]
    public async Task ReminderAuthorityReferenceRejectsUppercaseUuidAndZeroPaddedTicks()
    {
        Guid sessionId = Guid.Parse("018e4e5c-7f00-7000-8000-abcdef123456");
        DateTimeOffset sessionStartUtc = new(2026, 10, 25, 1, 30, 0, TimeSpan.Zero);
        string canonical = EventReminderAuthorityReference.Format(
            sessionId,
            sessionStartUtc,
            "Europe/Brussels");
        string uppercaseUuid = canonical.Replace(
            sessionId.ToString("N"),
            sessionId.ToString("N").ToUpperInvariant(),
            StringComparison.Ordinal);
        string zeroPaddedTicks = canonical.Replace(
            $":{sessionStartUtc.UtcDateTime.Ticks}:",
            $":0{sessionStartUtc.UtcDateTime.Ticks}:",
            StringComparison.Ordinal);

        bool uppercaseParsed = EventReminderAuthorityReference.TryParse(
            uppercaseUuid,
            out _,
            out _,
            out _);
        bool zeroPaddedParsed = EventReminderAuthorityReference.TryParse(
            zeroPaddedTicks,
            out _,
            out _,
            out _);

        await Assert.That(uppercaseParsed).IsFalse();
        await Assert.That(zeroPaddedParsed).IsFalse();
    }

    private static EventLifecycleScheduler CreateScheduler(
        IEventRegistrationIntentRepository repository,
        IRecipientNotificationMaterializer materializer,
        INotificationPreferenceResolver preferenceResolver,
        IScheduledEmailDispatchTrigger trigger,
        int leadTimeHours) => new(
            new EventLifecycleEmailOutboxFactory(),
            materializer,
            repository,
            Substitute.For<INotificationFanoutOccurrenceRepository>(),
            Substitute.For<IEmailDispatchOutboxRepository>(),
            preferenceResolver,
            trigger,
            Options.Create(new EventReminderOptions
            {
                EventReminderLeadTimeHours = leadTimeHours
            }));

    private static INotificationPreferenceResolver CreatePreferenceResolver(
        bool inAppEnabled,
        bool emailEnabled)
    {
        INotificationPreferenceResolver resolver = Substitute.For<INotificationPreferenceResolver>();
        resolver.ResolveBatchAsync(
                Arg.Any<IReadOnlyCollection<NotificationPreferenceResolveRequest>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<IReadOnlyCollection<NotificationPreferenceResolveRequest>>(0)
                .Select(request => new NotificationPreferenceDecision(
                    request.CategoryCode,
                    request.ChannelCode,
                    request.ChannelCode == NotificationPreferenceChannelCodes.InApp
                        ? inAppEnabled
                        : emailEnabled,
                    IsRequired: false,
                    IsLocked: false,
                    IsMuted: false,
                    EffectiveSourceScope: "Default",
                    LockReason: null))
                .ToArray());
        return resolver;
    }

    private static EventRegistrationIntent CreateIntent() => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = Guid.CreateVersion7(),
        Tenant = null!,
        EventId = Guid.CreateVersion7(),
        Event = null!,
        UserId = Guid.CreateVersion7(),
        User = null!,
        RegistrationScopeId = 1,
        RegistrationScope = null!,
        ApprovalStatusId = (int)ApprovalStatusEnum.Approved
    };

    private static EventSession CreateSession(EventRegistrationIntent intent, DateTimeOffset startTime) => new()
    {
        Id = Guid.CreateVersion7(),
        TenantId = intent.TenantId,
        Tenant = null!,
        EventId = intent.EventId,
        Event = null!,
        StartTime = startTime,
        EventSessionStatusId = (int)EventSessionStatusEnum.Published
    };

    private static User CreateRecipient(Guid userId, string email) => new()
    {
        Id = userId,
        EmailVerified = true,
        Pii = new UserPii
        {
            UserId = userId,
            Email = email,
            FirstName = "Amina",
            LastName = "Tester"
        }
    };

    private static EventRegistrationTransitionResult CreateApprovedTransition(
        EventRegistrationIntent intent,
        Guid sessionId,
        DateTimeOffset occurredAt) => new(
            Changed: true,
            ParentIntentId: intent.Id,
            PreviousStatus: (int)ApprovalStatusEnum.Waitlisted,
            FinalStatus: (int)ApprovalStatusEnum.Approved,
            TransitionReason: EventRegistrationTransitionReason.ApprovalStatusChanged,
            OccurrenceId: Guid.CreateVersion7(),
            OccurredAt: occurredAt,
            ActorProvenance: EventRegistrationActorProvenance.System,
            ActorUserId: null,
            ChildTransitions:
            [
                new EventRegistrationChildTransition(
                    Guid.CreateVersion7(),
                    sessionId,
                    (int)ApprovalStatusEnum.Waitlisted,
                    (int)ApprovalStatusEnum.Approved)
            ]);

    private static RecipientNotificationMaterializationResult CreateMaterializationResult(
        RecipientNotificationMaterialization request)
    {
        var intent = new NotificationIntent
        {
            Id = request.IntentId,
            TenantId = request.Intent.TenantId!.Value,
            CategoryId = (int)NotificationCategoryEnum.RegistrationLifecycle,
            OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
            RecipientKindId = (int)NotificationRecipientKindEnum.User,
            StatusId = request.Email is null
                ? (int)NotificationIntentStatusEnum.Resolved
                : (int)NotificationIntentStatusEnum.DispatchQueued,
            TemplateKey = request.Intent.TemplateKey!,
            DeduplicationKey = request.Intent.DeduplicationKey!,
            RecipientUserId = request.Intent.UserId!.Value
        };
        return new RecipientNotificationMaterializationResult(
            intent,
            [],
            null,
            request.Email);
    }
}
