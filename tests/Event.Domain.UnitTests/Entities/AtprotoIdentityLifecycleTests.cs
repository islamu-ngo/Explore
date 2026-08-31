// ABOUTME: Proves global AT Protocol credential moderation changes current state and immutable evidence.
// ABOUTME: Covers suspend/reinstate idempotency, validation, deletion rejection, and activity preservation.

namespace Event.Domain.UnitTests.Entities;

using Explore.Domain.Enums;
using Explore.Domain.ValueObjects;

public sealed class AtprotoIdentityLifecycleTests
{
    private static readonly DateTime TransitionedAt = new(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Suspend_ActiveIdentity_UpdatesCurrentStateAndAppendsImmutableRecord()
    {
        var identity = CreateIdentity(isActive: true);
        var suspendedBy = Guid.CreateVersion7();
        var concurrencyStamp = identity.ConcurrencyStamp;

        identity.Suspend(" compromised-key ", TransitionedAt, suspendedBy);

        await Assert.That(identity.IsActive).IsTrue();
        await Assert.That(identity.IsSuspended).IsTrue();
        await Assert.That(identity.SuspendedAt).IsEqualTo(TransitionedAt);
        await Assert.That(identity.SuspendedBy).IsEqualTo(suspendedBy);
        await Assert.That(identity.ModerationReasonCode).IsEqualTo("compromised-key");
        await Assert.That(identity.UpdatedAt).IsEqualTo(TransitionedAt);
        await Assert.That(identity.UpdatedBy).IsEqualTo(suspendedBy);
        await Assert.That(identity.ConcurrencyStamp).IsNotEqualTo(concurrencyStamp);
        await Assert.That(identity.ModerationRecords.Count).IsEqualTo(1);
        await Assert.That(identity.ModerationRecords.Single().Action).IsEqualTo(GlobalModerationAction.Suspend);
        await Assert.That(identity.ModerationRecords.Single().ReasonCode).IsEqualTo("compromised-key");
        await Assert.That(identity.ModerationRecords.Single().CreatedAt).IsEqualTo(TransitionedAt);
        await Assert.That(identity.ModerationRecords.Single().CreatedBy).IsEqualTo(suspendedBy);
    }

    [Test]
    public async Task Reinstate_SuspendedIdentity_PreservesActivityAndAppendsImmutableRecord()
    {
        var identity = CreateIdentity(isActive: false);
        var suspendedBy = Guid.CreateVersion7();
        var reinstatedBy = Guid.CreateVersion7();
        identity.Suspend("compromised-key", TransitionedAt, suspendedBy);
        var suspension = identity.ModerationRecords.Single();
        var concurrencyStamp = identity.ConcurrencyStamp;
        var reinstatedAt = TransitionedAt.AddHours(1);

        identity.Reinstate(" key-rotated ", reinstatedAt, reinstatedBy);

        await Assert.That(identity.IsActive).IsFalse();
        await Assert.That(identity.IsSuspended).IsFalse();
        await Assert.That(identity.SuspendedAt).IsNull();
        await Assert.That(identity.SuspendedBy).IsNull();
        await Assert.That(identity.ModerationReasonCode).IsNull();
        await Assert.That(identity.UpdatedAt).IsEqualTo(reinstatedAt);
        await Assert.That(identity.UpdatedBy).IsEqualTo(reinstatedBy);
        await Assert.That(identity.ConcurrencyStamp).IsNotEqualTo(concurrencyStamp);
        await Assert.That(identity.ModerationRecords.Count).IsEqualTo(2);
        await Assert.That(suspension.Action).IsEqualTo(GlobalModerationAction.Suspend);
        await Assert.That(suspension.ReasonCode).IsEqualTo("compromised-key");
        await Assert.That(identity.ModerationRecords.Last().Action).IsEqualTo(GlobalModerationAction.Reinstate);
        await Assert.That(identity.ModerationRecords.Last().ReasonCode).IsEqualTo("key-rotated");
        await Assert.That(identity.ModerationRecords.Last().CreatedAt).IsEqualTo(reinstatedAt);
        await Assert.That(identity.ModerationRecords.Last().CreatedBy).IsEqualTo(reinstatedBy);
    }

    [Test]
    public async Task Suspend_AlreadySuspendedIdentity_IsSuccessfulNoOp()
    {
        var identity = CreateIdentity(isActive: true);
        var suspendedBy = Guid.CreateVersion7();
        identity.Suspend("compromised-key", TransitionedAt, suspendedBy);
        var concurrencyStamp = identity.ConcurrencyStamp;

        identity.Suspend("different-reason", TransitionedAt.AddHours(1), Guid.CreateVersion7());

        await Assert.That(identity.SuspendedAt).IsEqualTo(TransitionedAt);
        await Assert.That(identity.SuspendedBy).IsEqualTo(suspendedBy);
        await Assert.That(identity.ModerationReasonCode).IsEqualTo("compromised-key");
        await Assert.That(identity.UpdatedAt).IsEqualTo(TransitionedAt);
        await Assert.That(identity.ConcurrencyStamp).IsEqualTo(concurrencyStamp);
        await Assert.That(identity.ModerationRecords.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Reinstate_ActiveIdentity_IsSuccessfulNoOp()
    {
        var identity = CreateIdentity(isActive: false);
        var concurrencyStamp = identity.ConcurrencyStamp;

        identity.Reinstate("key-rotated", TransitionedAt, Guid.CreateVersion7());

        await Assert.That(identity.IsActive).IsFalse();
        await Assert.That(identity.IsSuspended).IsFalse();
        await Assert.That(identity.UpdatedAt).IsNull();
        await Assert.That(identity.UpdatedBy).IsNull();
        await Assert.That(identity.ConcurrencyStamp).IsEqualTo(concurrencyStamp);
        await Assert.That(identity.ModerationRecords.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Suspend_RejectsInvalidInputsAndDeletedIdentity()
    {
        var identity = CreateIdentity(isActive: true);
        var by = Guid.CreateVersion7();

        await Assert.That(() => identity.Suspend(" ", TransitionedAt, by)).Throws<ArgumentException>();
        await Assert.That(() => identity.Suspend(new string('x', 129), TransitionedAt, by)).Throws<ArgumentException>();
        await Assert.That(() => identity.Suspend("compromised-key", TransitionedAt, Guid.Empty)).Throws<ArgumentException>();
        await Assert.That(() => identity.Suspend(
                "compromised-key",
                DateTime.SpecifyKind(TransitionedAt, DateTimeKind.Local),
                by))
            .Throws<ArgumentException>();

        identity.IsDeleted = true;

        await Assert.That(() => identity.Suspend("compromised-key", TransitionedAt, by))
            .Throws<InvalidOperationException>();
        await Assert.That(() => identity.Reinstate("key-rotated", TransitionedAt, by))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RefreshVerifiedMetadata_WithMatchingDid_UpdatesState()
    {
        var identity = CreateIdentity(isActive: false);
        var did = AtprotoDid.Parse(identity.Did);
        var resolvedAt = TransitionedAt.AddHours(2);

        identity.RefreshVerifiedMetadata(did, " NEW-HANDLE.EXAMPLE.COM ", "https://new-pds.example", "key-123", resolvedAt);

        await Assert.That(identity.IsActive).IsTrue();
        await Assert.That(identity.Handle).IsEqualTo("new-handle.example.com");
        await Assert.That(identity.PdsHost).IsEqualTo("https://new-pds.example");
        await Assert.That(identity.SigningKey).IsEqualTo("key-123");
        await Assert.That(identity.LastResolvedAt).IsEqualTo(resolvedAt);
        await Assert.That(identity.LastSeenAt).IsEqualTo(resolvedAt);
    }

    [Test]
    public async Task RefreshVerifiedMetadata_WithMismatchedDid_ThrowsInvalidOperationException()
    {
        var identity = CreateIdentity(isActive: false);
        var differentDid = AtprotoDid.Parse("did:plc:different-owner-456");

        await Assert.That(() => identity.RefreshVerifiedMetadata(differentDid, "handle", "https://pds.example", null, TransitionedAt))
            .Throws<InvalidOperationException>();
    }

    private static AtprotoIdentity CreateIdentity(bool isActive)
    {
        var actorId = Guid.CreateVersion7();
        var actor = new Actor
        {
            Id = actorId,
            ActorTypeId = (int)ActorTypeEnum.User,
            ActorType = new ActorType
            {
                Id = (int)ActorTypeEnum.User,
                FullName = "User",
                MasterCode = "USER"
            },
            Pii = new ActorPii
            {
                ActorId = actorId,
                DisplayName = "Identity owner"
            }
        };

        return new AtprotoIdentity
        {
            Id = Guid.CreateVersion7(),
            Did = "did:plc:identity-owner",
            ActorId = actor.Id,
            Actor = actor,
            PdsHost = "https://pds.example",
            IsActive = isActive,
            ConcurrencyStamp = Guid.CreateVersion7()
        };
    }
}
