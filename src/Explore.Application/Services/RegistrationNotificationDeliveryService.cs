// ABOUTME: Builds one registration lifecycle notification graph from the finalized parent transition.
// ABOUTME: Keeps required in-app delivery while representing unavailable email as a typed channel skip.

using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Notifications;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Explore.Application.Services;

public sealed class RegistrationNotificationDeliveryService(
    IEventLifecycleEmailOutboxFactory emailOutboxFactory)
    : IRegistrationNotificationDeliveryService
{
    public RegistrationNotificationEmailResolution ResolveRegistrationConfirmationEmail(User user)
    {
        RecipientEmailAddressResolution resolution = RecipientEmailAddressResolver.Resolve(user, user.Id);
        return resolution.SkipReason == RecipientEmailAddressResolver.RecipientEmailUnverified
            ? new RegistrationNotificationEmailResolution(
                RegistrationNotificationEmailStatus.UnverifiedIdentityEmail,
                null)
            : resolution.HasVerifiedEmail
                ? new RegistrationNotificationEmailResolution(
                    RegistrationNotificationEmailStatus.VerifiedEmail,
                    resolution.Email)
                : new RegistrationNotificationEmailResolution(
                    RegistrationNotificationEmailStatus.MissingEmail,
                    null);
    }

    public RecipientNotificationMaterialization? CreateLifecycleMaterialization(
        EventRegistrationIntent registrationIntent,
        string eventTitle,
        User user,
        EventRegistrationTransitionResult transition,
        Guid notificationIntentId,
        Guid emailDispatchOutboxId)
    {
        if (!transition.Changed
            || transition.ParentIntentId != registrationIntent.Id
            || transition.PreviousStatus == transition.FinalStatus
            || registrationIntent.UserId != user.Id)
        {
            return null;
        }

        LifecycleTemplate? template = ResolveTemplate(transition);
        if (template is null)
        {
            return null;
        }

        RegistrationNotificationEmailResolution emailResolution = ResolveRegistrationConfirmationEmail(user);
        EmailDispatchOutbox? email = emailResolution.HasVerifiedEmail
            ? CreateEmail(template.Kind, registrationIntent, user.Id, emailResolution.Email!, eventTitle)
            : null;
        if (email is not null)
        {
            email.Id = emailDispatchOutboxId;
        }

        string deduplicationKey =
            $"event-registration-intent:{registrationIntent.Id:N}:lifecycle:{transition.OccurrenceId:N}";
        return new RecipientNotificationMaterialization(
            notificationIntentId,
            new NotificationIntentDraft(
                Explore.Application.Notifications.NotificationCategory.RegistrationLifecycle,
                TenantId: registrationIntent.TenantId,
                RecipientKind: "User",
                TemplateKey: template.TemplateKey,
                SafePayloadReference: $"event-registration-intent:{registrationIntent.Id}",
                DeduplicationKey: deduplicationKey,
                CorrelationId: transition.OccurrenceId.ToString("D"),
                UserId: registrationIntent.UserId,
                EventId: registrationIntent.EventId),
            NotificationDeliveryPolicyEnum.RegistrationStatusOptional,
            "registration_status",
            new RecipientInAppNotificationDraft(
                (int)template.NotificationType,
                template.Title,
                template.Body(eventTitle),
                (int)ActorTypeEnum.User,
                (int)NotificationReasonEnum.System,
                (int)NotificationEntityTypeEnum.EventRegistration,
                registrationIntent.Id.ToString("D")),
            email,
            IncludeEmailChannel: true,
            EmailRequired: false,
            EmailSkipReason: emailResolution.Status switch
            {
                RegistrationNotificationEmailStatus.MissingEmail => "recipient_email_missing",
                RegistrationNotificationEmailStatus.UnverifiedIdentityEmail => "recipient_email_unverified",
                _ => null
            },
            PreferenceCategoryCode: NotificationPreferenceCategoryCodes.RegistrationStatus,
            LinkAllowed: false);
    }

    private EmailDispatchOutbox CreateEmail(
        EmailDispatchKind kind,
        EventRegistrationIntent registrationIntent,
        Guid userId,
        string recipientEmail,
        string eventTitle)
    {
        return kind switch
        {
            EmailDispatchKind.RegistrationConfirmation => emailOutboxFactory.CreateRegistrationConfirmation(
                registrationIntent.TenantId, userId, registrationIntent.EventId, registrationIntent.Id, recipientEmail, eventTitle),
            EmailDispatchKind.RegistrationApproved => emailOutboxFactory.CreateRegistrationApproved(
                registrationIntent.TenantId, userId, registrationIntent.EventId, registrationIntent.Id, recipientEmail, eventTitle),
            EmailDispatchKind.RegistrationRejected => emailOutboxFactory.CreateRegistrationRejected(
                registrationIntent.TenantId, userId, registrationIntent.EventId, registrationIntent.Id, recipientEmail, eventTitle),
            EmailDispatchKind.WaitlistPromoted => emailOutboxFactory.CreateWaitlistPromoted(
                registrationIntent.TenantId, userId, registrationIntent.EventId, registrationIntent.Id, recipientEmail, eventTitle),
            EmailDispatchKind.RegistrationCancelled => emailOutboxFactory.CreateRegistrationCancelled(
                registrationIntent.TenantId, userId, registrationIntent.EventId, registrationIntent.Id, recipientEmail, eventTitle),
            EmailDispatchKind.RegistrationRevoked => emailOutboxFactory.CreateRegistrationRevoked(
                registrationIntent.TenantId, userId, registrationIntent.EventId, registrationIntent.Id, recipientEmail, eventTitle),
            _ => throw new InvalidOperationException($"Unsupported registration lifecycle email kind {kind}.")
        };
    }

    private static LifecycleTemplate? ResolveTemplate(EventRegistrationTransitionResult transition)
    {
        if (transition.PreviousStatus is null
            && transition.FinalStatus is (int)ApprovalStatusEnum.Pending
                or (int)ApprovalStatusEnum.Approved
                or (int)ApprovalStatusEnum.Waitlisted)
        {
            return new LifecycleTemplate(
                "registration.confirmation",
                EmailDispatchKind.RegistrationConfirmation,
                NotificationTypeEnum.RegistrationConfirmed,
                "Registration received",
                title => $"Your registration for {NormalizeTitle(title)} was received.");
        }

        if (transition.TransitionReason == EventRegistrationTransitionReason.SelfCancelled
            && transition.ActorProvenance == EventRegistrationActorProvenance.Attendee)
        {
            return new LifecycleTemplate(
                "registration.cancelled",
                EmailDispatchKind.RegistrationCancelled,
                NotificationTypeEnum.General,
                "Registration cancelled",
                title => $"Your registration for {NormalizeTitle(title)} was cancelled as requested.");
        }

        if (transition.TransitionReason == EventRegistrationTransitionReason.Revoked
            && transition.ActorProvenance is EventRegistrationActorProvenance.Organizer
                or EventRegistrationActorProvenance.System)
        {
            return new LifecycleTemplate(
                "registration.revoked",
                EmailDispatchKind.RegistrationRevoked,
                NotificationTypeEnum.General,
                "Registration no longer active",
                title => $"Your registration for {NormalizeTitle(title)} is no longer active. Contact the event organizer if you need more information.");
        }

        if (transition.PreviousStatus == (int)ApprovalStatusEnum.Waitlisted
            && transition.FinalStatus == (int)ApprovalStatusEnum.Approved)
        {
            return new LifecycleTemplate(
                "registration.waitlist-promoted",
                EmailDispatchKind.WaitlistPromoted,
                NotificationTypeEnum.WaitlistPromoted,
                "Registration confirmed",
                title => $"A place opened for {NormalizeTitle(title)}; your waitlisted registration is now approved.");
        }

        return transition.FinalStatus switch
        {
            (int)ApprovalStatusEnum.Approved => new LifecycleTemplate(
                "registration.approved",
                EmailDispatchKind.RegistrationApproved,
                NotificationTypeEnum.ApprovalGranted,
                "Registration approved",
                title => $"Your registration for {NormalizeTitle(title)} was approved."),
            (int)ApprovalStatusEnum.Rejected => new LifecycleTemplate(
                "registration.rejected",
                EmailDispatchKind.RegistrationRejected,
                NotificationTypeEnum.ApprovalRejected,
                "Registration not approved",
                title => $"Your registration for {NormalizeTitle(title)} was not approved."),
            _ => null
        };
    }

    private static string NormalizeTitle(string eventTitle)
    {
        return string.IsNullOrWhiteSpace(eventTitle) ? "the event" : eventTitle.Trim();
    }

    private sealed record LifecycleTemplate(
        string TemplateKey,
        EmailDispatchKind Kind,
        NotificationTypeEnum NotificationType,
        string Title,
        Func<string, string> Body);
}
