// ABOUTME: Immutable local fact proving one contiguous erasure-authority intent was applied.
// ABOUTME: Stores only authority sequence, intent identity, prior checkpoint identity, and UTC application time.

namespace Explore.Domain;

public sealed class LocationPrivacyErasureReplayCheckpoint
{
    private LocationPrivacyErasureReplayCheckpoint()
    {
    }

    public Guid Id { get; private set; }
    public long AuthoritySequence { get; private set; }
    public Guid IntentId { get; private set; }
    public Guid? PreviousCheckpointId { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }

    public static LocationPrivacyErasureReplayCheckpoint Start(
        LocationPrivacyErasureAuthorityIntent intent,
        DateTime appliedAtUtc,
        Guid? checkpointId = null)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (intent.AuthoritySequence != 1)
        {
            throw new InvalidOperationException("A fresh local checkpoint must start at authority sequence one.");
        }

        return Create(intent, null, appliedAtUtc, checkpointId ?? Guid.CreateVersion7());
    }

    public static LocationPrivacyErasureReplayCheckpoint Advance(
        LocationPrivacyErasureReplayCheckpoint previous,
        LocationPrivacyErasureAuthorityIntent intent,
        DateTime appliedAtUtc,
        Guid? checkpointId = null)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(intent);
        if (intent.AuthoritySequence != previous.AuthoritySequence + 1)
        {
            throw new InvalidOperationException("Local replay checkpoints must advance by one without duplicates or gaps.");
        }

        if (intent.IntentId == previous.IntentId)
        {
            throw new InvalidOperationException("One erasure intent cannot occupy multiple authority sequences.");
        }

        return Create(intent, previous.Id, appliedAtUtc, checkpointId ?? Guid.CreateVersion7());
    }

    public bool Matches(LocationPrivacyErasureAuthorityIntent intent) =>
        intent is not null
        && AuthoritySequence == intent.AuthoritySequence
        && IntentId == intent.IntentId;

    private static LocationPrivacyErasureReplayCheckpoint Create(
        LocationPrivacyErasureAuthorityIntent intent,
        Guid? previousCheckpointId,
        DateTime appliedAtUtc,
        Guid checkpointId)
    {
        if (appliedAtUtc == default || appliedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Application timestamp must be a non-default UTC value.", nameof(appliedAtUtc));
        }

        if (appliedAtUtc < intent.RecordedAtUtc)
        {
            throw new ArgumentException("An intent cannot be applied before the authority records it.", nameof(appliedAtUtc));
        }

        if (checkpointId == Guid.Empty || checkpointId.Version != 7 || checkpointId.Variant is < 8 or > 11)
        {
            throw new ArgumentException("Checkpoint ids must be RFC 4122 UUIDv7 values.", nameof(checkpointId));
        }

        return new LocationPrivacyErasureReplayCheckpoint
        {
            Id = checkpointId,
            AuthoritySequence = intent.AuthoritySequence,
            IntentId = intent.IntentId,
            PreviousCheckpointId = previousCheckpointId,
            AppliedAtUtc = appliedAtUtc
        };
    }
}
