// ABOUTME: Proves external Actor promotion and merged-source retirement preserve identity and evidence.
// ABOUTME: Covers owner XOR, audit/concurrency mutation, subject retirement, and rejected transitions.

namespace Event.Domain.UnitTests.Entities;

using Explore.Domain.Enums;

public sealed class ActorLifecycleTests
{
    private static readonly DateTime TransitionedAt = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task ExternalUnclassified_HasStableLookupId()
    {
        await Assert.That((int)ActorTypeEnum.ExternalUnclassified).IsEqualTo(6);
    }

    [Test]
    public async Task PromoteToOrganization_PreservesIdentityAndPii_AndReplacesOnlyExternalOwner()
    {
        var (actor, externalSubject, pii, identity) = CreateExternalActor();
        var actorId = actor.Id;
        var identityId = identity.Id;
        var actorConcurrencyStamp = actor.ConcurrencyStamp;
        var externalConcurrencyStamp = externalSubject.ConcurrencyStamp;
        var promotedBy = Guid.CreateVersion7();
        var organization = CreateOrganization();
        var organizationActorType = CreateActorType(ActorTypeEnum.Organization);

        actor.PromoteToOrganization(organization, organizationActorType, TransitionedAt, promotedBy);

        await Assert.That(actor.Id).IsEqualTo(actorId);
        await Assert.That(identity.Id).IsEqualTo(identityId);
        await Assert.That(identity.ActorId).IsEqualTo(actorId);
        await Assert.That(ReferenceEquals(actor.Pii, pii)).IsTrue();
        await Assert.That(actor.DisplayName).IsEqualTo("External organizer");
        await Assert.That(actor.ProfilePictureUri).IsEqualTo("https://cdn.example/avatar.png");
        await Assert.That(actor.ExternalActorSubjectId).IsNull();
        await Assert.That(actor.ExternalActorSubject).IsNull();
        await Assert.That(actor.OrganizationId).IsEqualTo(organization.Id);
        await Assert.That(ReferenceEquals(actor.Organization, organization)).IsTrue();
        await Assert.That(ReferenceEquals(organization.Actor, actor)).IsTrue();
        await Assert.That(actor.ActorTypeId).IsEqualTo((int)ActorTypeEnum.Organization);
        await Assert.That(ReferenceEquals(actor.ActorType, organizationActorType)).IsTrue();
        await Assert.That(OwnerCount(actor)).IsEqualTo(1);
        await Assert.That(actor.UpdatedAt).IsEqualTo(TransitionedAt);
        await Assert.That(actor.UpdatedBy).IsEqualTo(promotedBy);
        await Assert.That(actor.ConcurrencyStamp).IsNotEqualTo(actorConcurrencyStamp);
        await Assert.That(externalSubject.IsDeleted).IsTrue();
        await Assert.That(externalSubject.DeletedAt).IsEqualTo(TransitionedAt);
        await Assert.That(externalSubject.DeletedBy).IsEqualTo(promotedBy);
        await Assert.That(externalSubject.UpdatedAt).IsEqualTo(TransitionedAt);
        await Assert.That(externalSubject.UpdatedBy).IsEqualTo(promotedBy);
        await Assert.That(externalSubject.ConcurrencyStamp).IsNotEqualTo(externalConcurrencyStamp);
        await Assert.That(externalSubject.FirstObservedAt).IsEqualTo(TransitionedAt.AddDays(-2));
        await Assert.That(externalSubject.LastObservedAt).IsEqualTo(TransitionedAt.AddDays(-1));
    }

    [Test]
    public async Task PromoteToGroup_PreservesIdentityAndPii_AndReplacesOnlyExternalOwner()
    {
        var (actor, externalSubject, pii, identity) = CreateExternalActor();
        var actorId = actor.Id;
        var identityId = identity.Id;
        var promotedBy = Guid.CreateVersion7();
        var group = new Group { Id = Guid.CreateVersion7(), FullName = "Promoted group" };
        var groupActorType = CreateActorType(ActorTypeEnum.Group);

        actor.PromoteToGroup(group, groupActorType, TransitionedAt, promotedBy);

        await Assert.That(actor.Id).IsEqualTo(actorId);
        await Assert.That(identity.Id).IsEqualTo(identityId);
        await Assert.That(identity.ActorId).IsEqualTo(actorId);
        await Assert.That(ReferenceEquals(actor.Pii, pii)).IsTrue();
        await Assert.That(actor.ExternalActorSubjectId).IsNull();
        await Assert.That(actor.GroupId).IsEqualTo(group.Id);
        await Assert.That(ReferenceEquals(actor.Group, group)).IsTrue();
        await Assert.That(ReferenceEquals(group.Actor, actor)).IsTrue();
        await Assert.That(actor.ActorTypeId).IsEqualTo((int)ActorTypeEnum.Group);
        await Assert.That(ReferenceEquals(actor.ActorType, groupActorType)).IsTrue();
        await Assert.That(OwnerCount(actor)).IsEqualTo(1);
        await Assert.That(externalSubject.IsDeleted).IsTrue();
        await Assert.That(externalSubject.Actor).IsNull();
    }

    [Test]
    public async Task RetireAsMergedSource_PreservesExternalOwnerPiiIdentityAndMergeEvidence()
    {
        var (actor, externalSubject, pii, identity) = CreateExternalActor();
        var actorId = actor.Id;
        var externalSubjectId = externalSubject.Id;
        var identityId = identity.Id;
        var sourceConcurrencyStamp = actor.ConcurrencyStamp;
        var retiredBy = Guid.CreateVersion7();
        var merge = ActorMerge.Create(
            actor.Id,
            Guid.CreateVersion7(),
            ActorMergeProofKind.VerifiedDid,
            "did-proof:sha256:abc123",
            TransitionedAt,
            retiredBy);
        actor.MergesFrom.Add(merge);

        actor.RetireAsMergedSource(TransitionedAt, retiredBy);

        await Assert.That(actor.Id).IsEqualTo(actorId);
        await Assert.That(actor.ExternalActorSubjectId).IsEqualTo(externalSubjectId);
        await Assert.That(ReferenceEquals(actor.ExternalActorSubject, externalSubject)).IsTrue();
        await Assert.That(externalSubject.IsDeleted).IsFalse();
        await Assert.That(ReferenceEquals(actor.Pii, pii)).IsTrue();
        await Assert.That(actor.DisplayName).IsEqualTo("External organizer");
        await Assert.That(identity.Id).IsEqualTo(identityId);
        await Assert.That(identity.ActorId).IsEqualTo(actorId);
        await Assert.That(actor.MergesFrom.Single()).IsEqualTo(merge);
        await Assert.That(merge.EvidenceReference).IsEqualTo("did-proof:sha256:abc123");
        await Assert.That(actor.IsDeleted).IsTrue();
        await Assert.That(actor.DeletedAt).IsEqualTo(TransitionedAt);
        await Assert.That(actor.DeletedBy).IsEqualTo(retiredBy);
        await Assert.That(actor.UpdatedAt).IsEqualTo(TransitionedAt);
        await Assert.That(actor.UpdatedBy).IsEqualTo(retiredBy);
        await Assert.That(actor.ConcurrencyStamp).IsNotEqualTo(sourceConcurrencyStamp);
        await Assert.That(OwnerCount(actor)).IsEqualTo(1);
    }

    [Test]
    public async Task ExternalActorSubject_Retire_UpdatesSoftDeleteAuditAndConcurrency()
    {
        var (_, externalSubject, _, _) = CreateExternalActor();
        var concurrencyStamp = externalSubject.ConcurrencyStamp;
        var retiredBy = Guid.CreateVersion7();

        externalSubject.Retire(TransitionedAt, retiredBy);

        await Assert.That(externalSubject.IsDeleted).IsTrue();
        await Assert.That(externalSubject.DeletedAt).IsEqualTo(TransitionedAt);
        await Assert.That(externalSubject.DeletedBy).IsEqualTo(retiredBy);
        await Assert.That(externalSubject.UpdatedAt).IsEqualTo(TransitionedAt);
        await Assert.That(externalSubject.UpdatedBy).IsEqualTo(retiredBy);
        await Assert.That(externalSubject.ConcurrencyStamp).IsNotEqualTo(concurrencyStamp);
    }

    [Test]
    public async Task PromoteToOrganization_FromUserActor_IsRejected()
    {
        var actor = CreateActor(ActorTypeEnum.User);
        actor.UserId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() =>
            actor.PromoteToOrganization(
                CreateOrganization(),
                CreateActorType(ActorTypeEnum.Organization),
                TransitionedAt,
                Guid.CreateVersion7())));
    }

    [Test]
    public async Task PromoteToOrganization_WithoutExternalOwner_IsRejected()
    {
        var actor = CreateActor(ActorTypeEnum.ExternalUnclassified);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() =>
            actor.PromoteToOrganization(
                CreateOrganization(),
                CreateActorType(ActorTypeEnum.Organization),
                TransitionedAt,
                Guid.CreateVersion7())));
    }

    [Test]
    public async Task PromoteToOrganization_WithAdditionalOwner_IsRejectedWithoutRetiringExternalSubject()
    {
        var (actor, externalSubject, _, _) = CreateExternalActor();
        actor.UserId = Guid.CreateVersion7();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() =>
            actor.PromoteToOrganization(
                CreateOrganization(),
                CreateActorType(ActorTypeEnum.Organization),
                TransitionedAt,
                Guid.CreateVersion7())));
        await Assert.That(externalSubject.IsDeleted).IsFalse();
        await Assert.That(actor.ExternalActorSubjectId).IsEqualTo(externalSubject.Id);
    }

    [Test]
    public async Task PromoteToOrganization_WithMismatchedActorType_IsRejectedWithoutMutation()
    {
        var (actor, externalSubject, _, _) = CreateExternalActor();

        await Assert.ThrowsAsync<ArgumentException>(() => Task.Run(() =>
            actor.PromoteToOrganization(
                CreateOrganization(),
                CreateActorType(ActorTypeEnum.Group),
                TransitionedAt,
                Guid.CreateVersion7())));
        await Assert.That(externalSubject.IsDeleted).IsFalse();
        await Assert.That(actor.ActorTypeId).IsEqualTo((int)ActorTypeEnum.ExternalUnclassified);
        await Assert.That(actor.OrganizationId).IsNull();
    }

    [Test]
    public async Task PromoteToOrganization_RepeatedTransition_IsRejected()
    {
        var (actor, _, _, _) = CreateExternalActor();
        var organization = CreateOrganization();
        var organizationActorType = CreateActorType(ActorTypeEnum.Organization);
        var promotedBy = Guid.CreateVersion7();
        actor.PromoteToOrganization(organization, organizationActorType, TransitionedAt, promotedBy);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() =>
            actor.PromoteToOrganization(organization, organizationActorType, TransitionedAt.AddMinutes(1), promotedBy)));
    }

    [Test]
    public async Task Transition_FromSuspendedOrDeletedExternalActor_IsRejected()
    {
        var (suspendedActor, suspendedExternalSubject, _, _) = CreateExternalActor();
        suspendedActor.IsSuspended = true;
        var (deletedActor, deletedExternalSubject, _, _) = CreateExternalActor();
        deletedActor.IsDeleted = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() =>
            suspendedActor.RetireAsMergedSource(TransitionedAt, Guid.CreateVersion7())));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() =>
            deletedActor.RetireAsMergedSource(TransitionedAt, Guid.CreateVersion7())));
        await Assert.That(suspendedExternalSubject.IsDeleted).IsFalse();
        await Assert.That(deletedExternalSubject.IsDeleted).IsFalse();
    }

    [Test]
    public async Task Transition_WithRetiredExternalSubject_IsRejected()
    {
        var (actor, externalSubject, _, _) = CreateExternalActor();
        externalSubject.Retire(TransitionedAt, Guid.CreateVersion7());

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() =>
            actor.RetireAsMergedSource(TransitionedAt.AddMinutes(1), Guid.CreateVersion7())));
        await Assert.That(actor.IsDeleted).IsFalse();
    }

    private static (Actor Actor, ExternalActorSubject ExternalSubject, ActorPii Pii, AtprotoIdentity Identity)
        CreateExternalActor()
    {
        var actor = CreateActor(ActorTypeEnum.ExternalUnclassified);
        var externalSubject = new ExternalActorSubject
        {
            Id = Guid.CreateVersion7(),
            Actor = actor,
            FirstObservedAt = TransitionedAt.AddDays(-2),
            LastObservedAt = TransitionedAt.AddDays(-1),
            ConcurrencyStamp = Guid.CreateVersion7()
        };
        actor.ExternalActorSubjectId = externalSubject.Id;
        actor.ExternalActorSubject = externalSubject;
        var identity = new AtprotoIdentity
        {
            Id = Guid.CreateVersion7(),
            Did = "did:plc:external-organizer",
            ActorId = actor.Id,
            Actor = actor,
            PdsHost = "https://pds.example",
            IsActive = true
        };
        actor.AtprotoIdentities.Add(identity);
        return (actor, externalSubject, actor.Pii, identity);
    }

    private static Actor CreateActor(ActorTypeEnum actorType)
    {
        var actorId = Guid.CreateVersion7();
        return new Actor
        {
            Id = actorId,
            ActorTypeId = (int)actorType,
            ActorType = CreateActorType(actorType),
            Pii = new ActorPii
            {
                ActorId = actorId,
                DisplayName = "External organizer",
                ProfilePictureUri = "https://cdn.example/avatar.png"
            },
            ConcurrencyStamp = Guid.CreateVersion7()
        };
    }

    private static ActorType CreateActorType(ActorTypeEnum actorType) => new()
    {
        Id = (int)actorType,
        FullName = actorType.ToString(),
        MasterCode = actorType.ToString().ToUpperInvariant()
    };

    private static Organization CreateOrganization()
    {
        var organizationId = Guid.CreateVersion7();
        return new Organization
        {
            Id = organizationId,
            Pii = new OrganizationPii
            {
                OrganizationId = organizationId,
                FullName = "Promoted organization"
            }
        };
    }

    private static int OwnerCount(Actor actor) => new Guid?[]
    {
        actor.UserId,
        actor.OrganizationId,
        actor.GroupId,
        actor.ExternalActorSubjectId,
        actor.ServicePrincipalId
    }.Count(id => id.HasValue);
}
