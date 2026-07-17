// ABOUTME: Unit tests for registration lifecycle notification channel materialization.
// ABOUTME: Proves final parent transitions select one coherent template and typed email eligibility outcome.

using Explore.Application.Contracts.Notifications;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Notifications;
using Explore.Application.Services;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Event.Application.UnitTests.Services;

public sealed class RegistrationNotificationDeliveryServiceTests
{
    private readonly RegistrationNotificationDeliveryService _service =
        new(new EventLifecycleEmailOutboxFactory());

    [Test]
    public async Task InitialPendingApprovedAndWaitlistedResultsUseOneReceiptPolicy()
    {
        foreach (int finalStatus in new[]
                 {
                     (int)ApprovalStatusEnum.Pending,
                     (int)ApprovalStatusEnum.Approved,
                     (int)ApprovalStatusEnum.Waitlisted
                 })
        {
            var fixture = CreateFixture(previousStatus: null, finalStatus, EventRegistrationTransitionReason.Created);

            RecipientNotificationMaterialization? result = _service.CreateLifecycleMaterialization(
                fixture.Intent,
                "Community Iftar",
                fixture.User,
                fixture.Transition,
                fixture.NotificationIntentId,
                fixture.EmailDispatchId);

            await Assert.That(result).IsNotNull();
            await Assert.That(result!.Intent.TemplateKey).IsEqualTo("registration.confirmation");
            await Assert.That(result.DeliveryPolicy).IsEqualTo(NotificationDeliveryPolicyEnum.RegistrationStatusOptional);
            await Assert.That(result.InApp!.IsRequired).IsTrue();
            await Assert.That(result.Email!.Kind).IsEqualTo(EmailDispatchKind.RegistrationConfirmation);
        }
    }

    [Test]
    public async Task FinalParentApprovalRejectionAndPromotionSelectDistinctTemplates()
    {
        var approved = CreateFixture(
            (int)ApprovalStatusEnum.Pending,
            (int)ApprovalStatusEnum.Approved,
            EventRegistrationTransitionReason.ApprovalStatusChanged);
        var rejected = CreateFixture(
            (int)ApprovalStatusEnum.Pending,
            (int)ApprovalStatusEnum.Rejected,
            EventRegistrationTransitionReason.ApprovalStatusChanged);
        var promoted = CreateFixture(
            (int)ApprovalStatusEnum.Waitlisted,
            (int)ApprovalStatusEnum.Approved,
            EventRegistrationTransitionReason.ApprovalStatusChanged);

        var approvedResult = Materialize(approved);
        var rejectedResult = Materialize(rejected);
        var promotedResult = Materialize(promoted);

        await Assert.That(approvedResult!.Intent.TemplateKey).IsEqualTo("registration.approved");
        await Assert.That(approvedResult.Email!.Kind).IsEqualTo(EmailDispatchKind.RegistrationApproved);
        await Assert.That(rejectedResult!.Intent.TemplateKey).IsEqualTo("registration.rejected");
        await Assert.That(rejectedResult.Email!.Kind).IsEqualTo(EmailDispatchKind.RegistrationRejected);
        await Assert.That(promotedResult!.Intent.TemplateKey).IsEqualTo("registration.waitlist-promoted");
        await Assert.That(promotedResult.Email!.Kind).IsEqualTo(EmailDispatchKind.WaitlistPromoted);
    }

    [Test]
    public async Task SelfCancellationAndOrganizerRevocationUsePersistedProvenance()
    {
        var selfCancelled = CreateFixture(
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Cancelled,
            EventRegistrationTransitionReason.SelfCancelled,
            EventRegistrationActorProvenance.Attendee);
        var revoked = CreateFixture(
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Revoked,
            EventRegistrationTransitionReason.Revoked,
            EventRegistrationActorProvenance.Organizer);

        var selfCancelledResult = Materialize(selfCancelled);
        var revokedResult = Materialize(revoked);

        await Assert.That(selfCancelledResult!.Intent.TemplateKey).IsEqualTo("registration.cancelled");
        await Assert.That(selfCancelledResult.Email!.Kind).IsEqualTo(EmailDispatchKind.RegistrationCancelled);
        await Assert.That(selfCancelledResult.InApp!.Body).Contains("as requested");
        await Assert.That(revokedResult!.Intent.TemplateKey).IsEqualTo("registration.revoked");
        await Assert.That(revokedResult.Email!.Kind).IsEqualTo(EmailDispatchKind.RegistrationRevoked);
        await Assert.That(revokedResult.InApp!.Body).DoesNotContain("organizer user");
    }

    [Test]
    public async Task MissingOrUnverifiedEmailKeepsRequiredInAppAndTypedSkippedEmail()
    {
        var missing = CreateFixture(
            (int)ApprovalStatusEnum.Pending,
            (int)ApprovalStatusEnum.Approved,
            EventRegistrationTransitionReason.ApprovalStatusChanged,
            email: string.Empty,
            emailVerified: true);
        var unverified = CreateFixture(
            (int)ApprovalStatusEnum.Pending,
            (int)ApprovalStatusEnum.Approved,
            EventRegistrationTransitionReason.ApprovalStatusChanged,
            emailVerified: false);

        var missingResult = Materialize(missing);
        var unverifiedResult = Materialize(unverified);

        await Assert.That(missingResult!.InApp!.IsRequired).IsTrue();
        await Assert.That(missingResult.Email).IsNull();
        await Assert.That(missingResult.IncludeEmailChannel).IsTrue();
        await Assert.That(missingResult.EmailSkipReason).IsEqualTo("recipient_email_missing");
        await Assert.That(unverifiedResult!.Email).IsNull();
        await Assert.That(unverifiedResult.EmailSkipReason).IsEqualTo("recipient_email_unverified");
    }

    [Test]
    public async Task NoOpChildOnlyAndNonPromotionWaitlistTransitionsCreateNothing()
    {
        var noOp = CreateFixture(
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Approved,
            EventRegistrationTransitionReason.NoChange);
        var childOnly = noOp with
        {
            Transition = noOp.Transition with
            {
                Changed = true,
                TransitionReason = EventRegistrationTransitionReason.Updated
            }
        };
        var becameWaitlisted = CreateFixture(
            (int)ApprovalStatusEnum.Approved,
            (int)ApprovalStatusEnum.Waitlisted,
            EventRegistrationTransitionReason.CapacityWaitlisted);

        await Assert.That(Materialize(noOp)).IsNull();
        await Assert.That(Materialize(childOnly)).IsNull();
        await Assert.That(Materialize(becameWaitlisted)).IsNull();
    }

    [Test]
    public async Task OccurrenceIdentityControlsDeduplicationAndEmailIdentity()
    {
        var fixture = CreateFixture(
            (int)ApprovalStatusEnum.Pending,
            (int)ApprovalStatusEnum.Approved,
            EventRegistrationTransitionReason.ApprovalStatusChanged);

        var result = Materialize(fixture);

        await Assert.That(result!.IntentId).IsEqualTo(fixture.NotificationIntentId);
        await Assert.That(result.Intent.DeduplicationKey).Contains(fixture.Transition.OccurrenceId.ToString("N"));
        await Assert.That(result.Email!.Id).IsEqualTo(fixture.EmailDispatchId);
        await Assert.That(result.Email.RecipientUserId).IsEqualTo(fixture.User.Id);
    }

    private RecipientNotificationMaterialization? Materialize(Fixture fixture)
    {
        return _service.CreateLifecycleMaterialization(
            fixture.Intent,
            "Community Iftar",
            fixture.User,
            fixture.Transition,
            fixture.NotificationIntentId,
            fixture.EmailDispatchId);
    }

    private static Fixture CreateFixture(
        int? previousStatus,
        int? finalStatus,
        EventRegistrationTransitionReason reason,
        EventRegistrationActorProvenance provenance = EventRegistrationActorProvenance.Attendee,
        string email = "attendee@example.test",
        bool? emailVerified = true)
    {
        Guid tenantId = Guid.CreateVersion7();
        Guid eventId = Guid.CreateVersion7();
        Guid userId = Guid.CreateVersion7();
        Guid registrationIntentId = Guid.CreateVersion7();
        var intent = new EventRegistrationIntent
        {
            Id = registrationIntentId,
            TenantId = tenantId,
            Tenant = null!,
            EventId = eventId,
            Event = null!,
            UserId = userId,
            User = null!,
            RegistrationScope = null!,
            ApprovalStatusId = finalStatus
        };
        var user = new User
        {
            Id = userId,
            EmailVerified = emailVerified,
            Pii = new UserPii
            {
                UserId = userId,
                Email = email,
                FirstName = "Test",
                LastName = "Attendee"
            }
        };
        user.Pii.User = user;

        return new Fixture(
            intent,
            user,
            new EventRegistrationTransitionResult(
                Changed: reason != EventRegistrationTransitionReason.NoChange,
                ParentIntentId: registrationIntentId,
                PreviousStatus: previousStatus,
                FinalStatus: finalStatus,
                TransitionReason: reason,
                OccurrenceId: Guid.CreateVersion7(),
                OccurredAt: DateTimeOffset.UtcNow,
                ActorProvenance: provenance,
                ActorUserId: provenance == EventRegistrationActorProvenance.System ? null : userId,
                ChildTransitions: []),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
    }

    private sealed record Fixture(
        EventRegistrationIntent Intent,
        User User,
        EventRegistrationTransitionResult Transition,
        Guid NotificationIntentId,
        Guid EmailDispatchId);
}
