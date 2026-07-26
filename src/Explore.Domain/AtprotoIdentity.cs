// ABOUTME: Global exact-DID credential identity associated with one represented Actor.
// ABOUTME: Owns mutable handle, PDS, signing-key, cache, and credential moderation state.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class AtprotoIdentity : IAuditableEntity, ISoftDeletable, IConcurrencyAware
{
    public Guid Id { get; set; }
    public required string Did { get; set; }
    public Guid ActorId { get; set; }
    public required Actor Actor { get; set; }
    public string? Handle { get; set; }
    public required string PdsHost { get; set; }
    public string? SigningKey { get; set; }
    public bool IsActive { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public Guid? SuspendedBy { get; set; }
    public string? ModerationReasonCode { get; set; }
    public DateTime LastResolvedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public ICollection<AtprotoIdentityModerationRecord> ModerationRecords { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    public void RefreshVerifiedMetadata(string did, string? handle, string pdsHost, string? signingKey, DateTime resolvedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(did);
        ArgumentException.ThrowIfNullOrWhiteSpace(pdsHost);
        if (!string.Equals(Did, did, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Verified metadata cannot change the identity DID.");
        }

        Handle = string.IsNullOrWhiteSpace(handle) ? null : handle.Trim().ToLowerInvariant();
        PdsHost = pdsHost.Trim();
        SigningKey = string.IsNullOrWhiteSpace(signingKey) ? null : signingKey.Trim();
        LastResolvedAt = resolvedAt;
        LastSeenAt = resolvedAt;
        IsActive = true;
    }
}
