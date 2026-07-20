// ABOUTME: Verifies current persisted recipient identity controls fanout email materialization.
// ABOUTME: Keeps required in-app delivery while missing or unverified email becomes a typed skip.

using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Notifications;

public sealed class NotificationFanoutRecipientMaterializationServiceTests
{
    [Test]
    [Arguments(true, false, true, "current@example.test", true)]
    [Arguments(true, false, false, "current@example.test", false)]
    [Arguments(true, false, true, "", false)]
    [Arguments(true, true, true, "current@example.test", false)]
    [Arguments(false, false, true, "current@example.test", false)]
    public async Task CurrentVerifiedAddressControlsOptionalEmail(
        bool userExists,
        bool userDeleted,
        bool verified,
        string email,
        bool expectsEmail)
    {
        NotificationFanoutOccurrence occurrence = CreateOccurrence();
        Guid recipientUserId = Guid.CreateVersion7();
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        User? currentUser = userExists
            ? new User
            {
                Id = recipientUserId,
                Pii = new UserPii { Email = email, FirstName = "Current", LastName = "Recipient" },
                EmailVerified = verified,
                IsDeleted = userDeleted
            }
            : null;
        userRepository.GetUserWithDetails(recipientUserId, Arg.Any<CancellationToken>())
            .Returns(currentUser);
        INotificationPreferenceResolver preferenceResolver = Substitute.For<INotificationPreferenceResolver>();
        preferenceResolver.ResolveAsync(Arg.Any<NotificationPreferenceResolveRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<NotificationPreferenceResolveRequest>();
                return new NotificationPreferenceDecision(
                    request.CategoryCode,
                    request.ChannelCode,
                    true,
                    false,
                    false,
                    false,
                    "Default",
                    null);
            });
        var locationAuthorization = Substitute.For<IFanoutAttendeeLocationAuthorizationService>();
        var materializer = Substitute.For<IRecipientNotificationMaterializer>();
        RecipientNotificationMaterialization? captured = null;
        materializer.MaterializeAsync(Arg.Any<RecipientNotificationMaterialization>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.Arg<RecipientNotificationMaterialization>();
                var intent = new NotificationIntent
                {
                    Id = captured.IntentId,
                    TenantId = occurrence.TenantId,
                    CategoryId = (int)NotificationCategoryEnum.EventLifecycle,
                    OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
                    RecipientKindId = (int)NotificationRecipientKindEnum.User,
                    StatusId = (int)NotificationIntentStatusEnum.Pending,
                    TemplateKey = occurrence.TemplateKey,
                    DeduplicationKey = captured.Intent.DeduplicationKey!,
                    RecipientUserId = recipientUserId,
                    FanoutOccurrenceId = occurrence.Id
                };
                return new RecipientNotificationMaterializationResult(intent, [], null, captured.Email);
            });
        var service = new NotificationFanoutRecipientMaterializationService(
            userRepository,
            preferenceResolver,
            locationAuthorization,
            new NotificationFanoutRecipientTemplateFactory(),
            materializer);

        await service.MaterializeAsync(occurrence, recipientUserId);

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.InApp).IsNotNull();
        await Assert.That(captured.Email is not null).IsEqualTo(expectsEmail);
        if (expectsEmail)
        {
            await Assert.That(captured.Email!.RecipientEmail).IsEqualTo(email);
        }
        else
        {
            await Assert.That(captured.EmailSkipReason).StartsWith("recipient_");
        }
    }

    [Test]
    public async Task HeavyModerationUsesCurrentVerifiedAddressAndBypassesOptionalPreference()
    {
        NotificationFanoutOccurrence occurrence = CreateHeavyModerationOccurrence();
        Guid recipientUserId = Guid.CreateVersion7();
        IUserRepository userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserWithDetails(recipientUserId, Arg.Any<CancellationToken>())
            .Returns(new User
            {
                Id = recipientUserId,
                Pii = new UserPii
                {
                    Email = " current-heavy@example.test ",
                    FirstName = "Current",
                    LastName = "Recipient"
                },
                EmailVerified = true
            });
        INotificationPreferenceResolver preferenceResolver = Substitute.For<INotificationPreferenceResolver>();
        var locationAuthorization = Substitute.For<IFanoutAttendeeLocationAuthorizationService>();
        var materializer = Substitute.For<IRecipientNotificationMaterializer>();
        RecipientNotificationMaterialization? captured = null;
        materializer.MaterializeAsync(Arg.Any<RecipientNotificationMaterialization>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                captured = call.Arg<RecipientNotificationMaterialization>();
                var intent = new NotificationIntent
                {
                    Id = captured.IntentId,
                    TenantId = occurrence.TenantId,
                    CategoryId = (int)NotificationCategoryEnum.TrustSafetyModeration,
                    OwnershipTypeId = (int)NotificationOwnershipTypeEnum.IslamuEvent,
                    RecipientKindId = (int)NotificationRecipientKindEnum.User,
                    StatusId = (int)NotificationIntentStatusEnum.DispatchQueued,
                    TemplateKey = occurrence.TemplateKey,
                    DeduplicationKey = captured.Intent.DeduplicationKey!,
                    RecipientUserId = recipientUserId,
                    FanoutOccurrenceId = occurrence.Id
                };
                return new RecipientNotificationMaterializationResult(intent, [], null, captured.Email);
            });
        var service = new NotificationFanoutRecipientMaterializationService(
            userRepository,
            preferenceResolver,
            locationAuthorization,
            new NotificationFanoutRecipientTemplateFactory(),
            materializer);

        await service.MaterializeAsync(occurrence, recipientUserId);

        await preferenceResolver.DidNotReceiveWithAnyArgs().ResolveAsync(default!, default);
        await locationAuthorization.DidNotReceiveWithAnyArgs().AuthorizeAsync(default!, default);
        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.DeliveryPolicy).IsEqualTo(NotificationDeliveryPolicyEnum.ModerationAvailabilityRequired);
        await Assert.That(captured.InApp!.IsRequired).IsTrue();
        await Assert.That(captured.EmailRequired).IsTrue();
        await Assert.That(captured.Email!.RecipientEmail).IsEqualTo("current-heavy@example.test");
    }

    private static NotificationFanoutOccurrence CreateOccurrence()
    {
        DateTime now = DateTime.UtcNow;
        string snapshot = NotificationFanoutTemplateJson.Serialize(new NotificationFanoutSnapshotV1(
            "Immutable event",
            null,
            new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.FromHours(2)),
            "Europe/Brussels",
            null));
        return NotificationFanoutOccurrence.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), null,
            now, now, Guid.CreateVersion7(),
            NotificationFanoutTemplateJson.Serialize(new NotificationFanoutChangeSetV1(
                [NotificationFanoutChangeField.StartTime])),
            snapshot,
            snapshot,
            NotificationFanoutRecipientTemplateFactory.EventUpdatedTemplateKey,
            1,
            (int)NotificationDeliveryPolicyEnum.CriticalEventUpdateOptional,
            1,
            30,
            now,
            "event",
            Guid.CreateVersion7(),
            $"event:{Guid.NewGuid():N}",
            null);
    }

    private static NotificationFanoutOccurrence CreateHeavyModerationOccurrence()
    {
        DateTime now = DateTime.UtcNow;
        Guid eventId = Guid.CreateVersion7();
        return NotificationFanoutOccurrence.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            eventId,
            null,
            now,
            now,
            Guid.CreateVersion7(),
            "{}",
            "{}",
            "{}",
            NotificationFanoutOccurrenceCoordinationPolicy.HeavyModerationUnavailableTemplateKey,
            NotificationFanoutRecipientTemplateFactory.CurrentTemplateVersion,
            (int)NotificationDeliveryPolicyEnum.ModerationAvailabilityRequired,
            NotificationFanoutRecipientTemplateFactory.CurrentPolicyVersion,
            NotificationFanoutOccurrenceCoordinationPolicy.HeavyModerationUnavailablePriority,
            now,
            "event_moderation_record",
            Guid.CreateVersion7(),
            $"event:{eventId:N}",
            null);
    }
}
