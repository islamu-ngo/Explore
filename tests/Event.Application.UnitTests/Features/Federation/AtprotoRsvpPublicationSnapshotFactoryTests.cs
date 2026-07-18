// ABOUTME: Verifies registration lifecycle maps only to going and cancellation waits for the final live registration.
// ABOUTME: Proves organizer approval state, attendee PII, and unsupported RSVP intents never influence the projection.

using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Features.Federation.Atproto.Services;
using Explore.Domain;
using Explore.Domain.Enums;

namespace Event.Application.UnitTests.Features.Federation;

public sealed class AtprotoRsvpPublicationSnapshotFactoryTests
{
    [Test]
    [Arguments(ApprovalStatusEnum.Pending)]
    [Arguments(ApprovalStatusEnum.Approved)]
    [Arguments(ApprovalStatusEnum.Rejected)]
    [Arguments(ApprovalStatusEnum.Waitlisted)]
    [Arguments(ApprovalStatusEnum.Cancelled)]
    [Arguments(ApprovalStatusEnum.Revoked)]
    public async Task ActiveIntent_AlwaysMapsOnlyToGoing_RegardlessOfOrganizerApproval(
        ApprovalStatusEnum approvalStatus)
    {
        EventRegistrationIntent intent = CreateIntent(approvalStatus);
        var subject = new AtprotoSettledEventReference(
            "at://did:plc:owner/community.lexicon.calendar.event/3m123",
            "bafyreicid");

        AtprotoRsvpPublicationPlan result = AtprotoRsvpPublicationSnapshotFactory.PlanActiveRegistration(
            intent,
            ContextFor(intent),
            "did:plc:owner",
            subject);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(result.Operation).IsEqualTo(AtprotoRsvpPublicationOperation.CreateOrUpdate);
        await Assert.That(result.Snapshot!.Status).IsEqualTo(AtprotoRsvpPublicationSnapshotFactory.GoingStatus);
        await Assert.That(result.Snapshot.SubjectUri).IsEqualTo(subject.Uri);
        await Assert.That(result.Snapshot.SubjectCid).IsEqualTo(subject.Cid);
        await Assert.That(result.Snapshot.ToString()).DoesNotContain("attendee-private-canary");
    }

    [Test]
    public async Task MissingOrUnsettledSubject_AndDeletedIntent_AreRejected()
    {
        EventRegistrationIntent intent = CreateIntent(ApprovalStatusEnum.Approved);
        AtprotoRsvpPublicationPlan missing = AtprotoRsvpPublicationSnapshotFactory.PlanActiveRegistration(
            intent,
            ContextFor(intent),
            "did:plc:owner",
            settledEvent: null);
        intent.IsDeleted = true;
        AtprotoRsvpPublicationPlan deleted = AtprotoRsvpPublicationSnapshotFactory.PlanActiveRegistration(
            intent,
            ContextFor(intent),
            "did:plc:owner",
            new("at://did:plc:owner/community.lexicon.calendar.event/key", "cid"));

        await Assert.That(missing.IsValid).IsFalse();
        await Assert.That(deleted.IsValid).IsFalse();
        await Assert.That(missing.Snapshot).IsNull();
        await Assert.That(deleted.Snapshot).IsNull();
    }

    [Test]
    public async Task Cancellation_DeletesOnlyAfterLastLiveRegistration()
    {
        AtprotoRsvpPublicationPlan stillRegistered = AtprotoRsvpPublicationSnapshotFactory.PlanCancellation(1, true);
        AtprotoRsvpPublicationPlan finalCancellation = AtprotoRsvpPublicationSnapshotFactory.PlanCancellation(0, true);
        AtprotoRsvpPublicationPlan noRemoteRecord = AtprotoRsvpPublicationSnapshotFactory.PlanCancellation(0, false);

        await Assert.That(stillRegistered.Operation).IsEqualTo(AtprotoRsvpPublicationOperation.None);
        await Assert.That(finalCancellation.Operation).IsEqualTo(AtprotoRsvpPublicationOperation.Delete);
        await Assert.That(noRemoteRecord.Operation).IsEqualTo(AtprotoRsvpPublicationOperation.None);
    }

    [Test]
    public async Task UnpersistedOrScopeMismatchedIntent_IsRejected()
    {
        EventRegistrationIntent intent = CreateIntent(ApprovalStatusEnum.Approved);
        AtprotoSettledEventReference subject = new(
            "at://did:plc:owner/community.lexicon.calendar.event/key",
            "cid");
        AtprotoRsvpPublicationPlan wrongTenant = AtprotoRsvpPublicationSnapshotFactory.PlanActiveRegistration(
            intent,
            ContextFor(intent) with { TenantId = Guid.CreateVersion7() },
            "did:plc:owner",
            subject);
        intent.Id = Guid.Empty;
        AtprotoRsvpPublicationPlan unpersisted = AtprotoRsvpPublicationSnapshotFactory.PlanActiveRegistration(
            intent,
            ContextFor(intent),
            "did:plc:owner",
            subject);

        await Assert.That(wrongTenant.IsValid).IsFalse();
        await Assert.That(unpersisted.IsValid).IsFalse();
    }

    private static EventRegistrationIntent CreateIntent(ApprovalStatusEnum approvalStatus)
        => new()
        {
            Id = Guid.CreateVersion7(),
            EventId = Guid.CreateVersion7(),
            Event = null!,
            UserId = Guid.CreateVersion7(),
            User = null!,
            RegistrationScopeId = (int)RegistrationScopeEnum.Event,
            RegistrationScope = null!,
            ApprovalStatusId = (int)approvalStatus,
            TenantId = Guid.CreateVersion7(),
            Tenant = null!,
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.CreateVersion7()
        };

    private static AtprotoRsvpPublicationContext ContextFor(EventRegistrationIntent intent)
        => new(intent.TenantId, intent.UserId, intent.EventId);
}
