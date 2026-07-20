// ABOUTME: Models the fenced local state and hashed short-lived receipt for one privacy-erasure intent.
// ABOUTME: Binds User-only execution to the intent policy without storing plaintext credentials or free text.

namespace Explore.Domain;

public sealed class PrivacyErasureSaga
{
    private PrivacyErasureSaga()
    {
    }

    public Guid IntentId { get; private set; }
    public PrivacyErasureSubjectKind SubjectKind { get; private set; }
    public Guid SubjectId { get; private set; }
    public int PolicyVersion { get; private set; }
    public long FenceToken { get; private set; }
    public byte[] ReceiptHash { get; private set; } = [];
    public DateTime ReceiptExpiresAtUtc { get; private set; }
    public DateTime FencedAtUtc { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public static PrivacyErasureSaga Start(
        PrivacyErasureIntent intent,
        long fenceToken,
        byte[] receiptHash,
        DateTime receiptExpiresAtUtc,
        DateTime fencedAtUtc,
        Guid? concurrencyToken = null)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(receiptHash);
        if (fenceToken <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fenceToken));
        }

        if (receiptHash.Length != 32)
        {
            throw new ArgumentException("Receipt hashes must be SHA-256 values.", nameof(receiptHash));
        }

        RequireUtc(fencedAtUtc, nameof(fencedAtUtc));
        RequireUtc(receiptExpiresAtUtc, nameof(receiptExpiresAtUtc));
        if (receiptExpiresAtUtc <= fencedAtUtc)
        {
            throw new ArgumentException("Receipt expiry must follow the fence timestamp.", nameof(receiptExpiresAtUtc));
        }

        Guid token = concurrencyToken ?? Guid.CreateVersion7();
        if (token == Guid.Empty || token.Version != 7 || token.Variant is < 8 or > 11)
        {
            throw new ArgumentException("Concurrency tokens must be RFC 4122 UUIDv7 values.", nameof(concurrencyToken));
        }

        return new PrivacyErasureSaga
        {
            IntentId = intent.IntentId,
            SubjectKind = intent.SubjectKind,
            SubjectId = intent.SubjectId,
            PolicyVersion = intent.PolicyVersion,
            FenceToken = fenceToken,
            ReceiptHash = [.. receiptHash],
            ReceiptExpiresAtUtc = receiptExpiresAtUtc,
            FencedAtUtc = fencedAtUtc,
            ConcurrencyToken = token
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
