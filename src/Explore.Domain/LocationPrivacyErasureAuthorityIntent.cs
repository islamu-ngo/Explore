// ABOUTME: Immutable PII-free fact returned after the separate erasure authority appends an intent.
// ABOUTME: Carries a UUIDv7 idempotency key, monotonic sequence, opaque IDs, reason, and UTC metadata only.

using Explore.Domain.Enums;

namespace Explore.Domain;

public sealed class LocationPrivacyErasureAuthorityIntent
{
    private LocationPrivacyErasureAuthorityIntent()
    {
    }

    public Guid IntentId { get; private set; }
    public long AuthoritySequence { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public IReadOnlyList<Guid> LocationIds { get; private set; } = Array.Empty<Guid>();
    public LocationPrivacyErasureReasonEnum Reason { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }

    public static LocationPrivacyErasureAuthorityIntent Record(
        Guid intentId,
        long authoritySequence,
        Guid ownerUserId,
        IEnumerable<Guid> locationIds,
        LocationPrivacyErasureReasonEnum reason,
        DateTime requestedAtUtc,
        DateTime recordedAtUtc)
    {
        if (intentId == Guid.Empty ||
            intentId.Version != 7 ||
            intentId.Variant is < 8 or > 11)
        {
            throw new ArgumentException(
                "Erasure intent idempotency keys must be non-empty RFC 4122 UUIDv7 values.",
                nameof(intentId));
        }

        if (authoritySequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(authoritySequence), "Authority sequence must be positive.");
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("Owner user id is required.", nameof(ownerUserId));
        }

        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        ArgumentNullException.ThrowIfNull(locationIds);
        Guid[] normalizedLocationIds = locationIds.Distinct().Order().ToArray();
        if (normalizedLocationIds.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException("Location ids cannot contain an empty value.", nameof(locationIds));
        }

        RequireUtc(requestedAtUtc, nameof(requestedAtUtc));
        RequireUtc(recordedAtUtc, nameof(recordedAtUtc));
        if (recordedAtUtc < requestedAtUtc)
        {
            throw new ArgumentException("Authority recording cannot precede the request.", nameof(recordedAtUtc));
        }

        return new LocationPrivacyErasureAuthorityIntent
        {
            IntentId = intentId,
            AuthoritySequence = authoritySequence,
            OwnerUserId = ownerUserId,
            LocationIds = Array.AsReadOnly(normalizedLocationIds),
            Reason = reason,
            RequestedAtUtc = requestedAtUtc,
            RecordedAtUtc = recordedAtUtc
        };
    }

    private static void RequireUtc(DateTime value, string parameterName)
    {
        if (value == default || value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be a non-default UTC value.", parameterName);
        }
    }
}
