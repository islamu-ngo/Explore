// ABOUTME: Defines provider-neutral admission recovery capability, persistence, and delivery contracts.
// ABOUTME: Keeps public receipts uniform and redacts every capability or digest-bearing diagnostic shape.

namespace Explore.Application.Contracts.Admissions;

public enum AdmissionRecoveryPurpose
{
    TicketRecovery,
    TransferAcceptance
}

public enum AdmissionRecoveryRequestOutcome
{
    Accepted
}

public enum AdmissionRecoveryConsumeOutcome
{
    Consumed,
    AlreadyConsumed,
    Expired,
    WrongPurpose,
    WrongTenant,
    Rotated,
    Invalid
}

public enum AdmissionRecoveryMutationOutcome
{
    Stored,
    Consumed,
    Rotated,
    Rejected
}

public enum AdmissionRecoveryDeliveryOutcome
{
    Accepted,
    Pending
}

public sealed record AdmissionRecoveryRequest(
    Guid TenantId,
    string NormalizedIdentity,
    AdmissionRecoveryPurpose Purpose);

public sealed record AdmissionRecoveryConsumeRequest(
    Guid TenantId,
    Guid RecoveryRequestId,
    string Capability,
    AdmissionRecoveryPurpose Purpose)
{
    public override string ToString() => "AdmissionRecoveryConsumeRequest(<redacted>)";
}

public sealed record AdmissionRecoveryResendRequest(
    Guid TenantId,
    Guid RecoveryRequestId,
    AdmissionRecoveryPurpose Purpose);

public sealed record AdmissionRecoveryRequestResult(AdmissionRecoveryRequestOutcome Outcome);

public sealed record AdmissionRecoveryConsumeResult(
    AdmissionRecoveryConsumeOutcome Outcome,
    Guid RecoveryRecordId = default,
    AdmissionRecoveryTicketDocument? Document = null);

public sealed record AdmissionRecoveryResendResult(AdmissionRecoveryRequestOutcome Outcome);

public sealed record AdmissionRecoveryIdentityResult
{
    public AdmissionRecoveryIdentityResult(
        Guid tenantId,
        Guid recoveryRequestId,
        bool identityPresent,
        IReadOnlyList<Guid> admissionTicketIds)
    {
        TenantId = tenantId;
        RecoveryRequestId = recoveryRequestId;
        IdentityPresent = identityPresent;
        AdmissionTicketIds = admissionTicketIds.ToArray();
    }

    public Guid TenantId { get; }
    public Guid RecoveryRequestId { get; }
    public bool IdentityPresent { get; }
    public IReadOnlyList<Guid> AdmissionTicketIds { get; }
}

public sealed record AdmissionRecoveryCapabilityIssueRequest(
    Guid TenantId,
    Guid RecoveryRequestId,
    Guid AdmissionTicketId,
    AdmissionRecoveryPurpose Purpose,
    int KeyVersion = 0);

public sealed record AdmissionRecoveryCapabilityDigestRequest(
    Guid TenantId,
    Guid RecoveryRequestId,
    Guid AdmissionTicketId,
    AdmissionRecoveryPurpose Purpose,
    string Capability,
    int KeyVersion = 0)
{
    public override string ToString() => "AdmissionRecoveryCapabilityDigestRequest(<redacted>)";
}

public sealed record AdmissionRecoveryCapabilityMaterial
{
    public AdmissionRecoveryCapabilityMaterial(
        string capability,
        string lookupDigest,
        int keyVersion,
        AdmissionRecoveryPurpose purpose,
        DateTimeOffset expiresAtUtc,
        string locatorDigest = "")
    {
        Capability = capability;
        LookupDigest = lookupDigest;
        KeyVersion = keyVersion;
        Purpose = purpose;
        ExpiresAtUtc = expiresAtUtc;
        LocatorDigest = locatorDigest;
    }

    public string Capability { get; }
    public string LookupDigest { get; }
    public int KeyVersion { get; }
    public AdmissionRecoveryPurpose Purpose { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public string LocatorDigest { get; }

    public override string ToString() =>
        $"AdmissionRecoveryCapabilityMaterial(keyVersion={KeyVersion}, <redacted>)";
}

public sealed record AdmissionRecoveryCapabilityDigest(string LookupDigest, int KeyVersion)
{
    public override string ToString() =>
        $"AdmissionRecoveryCapabilityDigest(keyVersion={KeyVersion}, <redacted>)";
}

public sealed record AdmissionRecoveryLocatorDigest(string LocatorDigest, int KeyVersion)
{
    public override string ToString() =>
        $"AdmissionRecoveryLocatorDigest(keyVersion={KeyVersion}, <redacted>)";
}

public sealed record AdmissionRecoveryCapabilityRecord(
    Guid TenantId,
    Guid RecoveryRequestId,
    Guid AdmissionTicketId,
    AdmissionRecoveryPurpose Purpose,
    string LookupDigest,
    int KeyVersion,
    DateTimeOffset ExpiresAtUtc,
    Guid CapabilityId = default,
    int CapabilityVersion = 1,
    DateTimeOffset CreatedAtUtc = default,
    string LocatorDigest = "")
{
    public override string ToString() =>
        $"AdmissionRecoveryCapabilityRecord(keyVersion={KeyVersion}, <redacted>)";
}

public sealed record AdmissionRecoveryCapabilityLookup(
    Guid TenantId,
    Guid RecoveryRequestId,
    Guid AdmissionTicketId,
    AdmissionRecoveryPurpose Purpose,
    string LookupDigest,
    int KeyVersion = 0)
{
    public override string ToString() => "AdmissionRecoveryCapabilityLookup(<redacted>)";
}

public sealed record AdmissionRecoveryCapabilityMutation(
    Guid TenantId,
    Guid RecoveryRequestId,
    Guid AdmissionTicketId,
    AdmissionRecoveryPurpose Purpose,
    string LookupDigest,
    DateTimeOffset ExpiresAtUtc,
    int KeyVersion = 0,
    Guid CapabilityId = default,
    Guid ExpectedConcurrencyStamp = default,
    DateTimeOffset OccurredAtUtc = default)
{
    public override string ToString() => "AdmissionRecoveryCapabilityMutation(<redacted>)";
}

public sealed record AdmissionRecoveryRotationRequest(
    Guid TenantId,
    Guid RecoveryRequestId,
    Guid AdmissionTicketId,
    AdmissionRecoveryPurpose Purpose,
    string OldLookupDigest,
    string ReplacementLookupDigest,
    int ReplacementKeyVersion,
    DateTimeOffset ReplacementExpiresAtUtc,
    int OldKeyVersion = 0,
    Guid OldCapabilityId = default,
    Guid ReplacementCapabilityId = default,
    int ReplacementCapabilityVersion = 0,
    Guid ExpectedConcurrencyStamp = default,
    DateTimeOffset RotatedAtUtc = default,
    string ReplacementLocatorDigest = "")
{
    public override string ToString() => "AdmissionRecoveryRotationRequest(<redacted>)";
}

public sealed record AdmissionRecoveryCapabilityState(
    bool Found,
    Guid TenantId,
    Guid RecoveryRequestId,
    Guid AdmissionTicketId,
    string LookupDigest,
    AdmissionRecoveryPurpose Purpose,
    DateTimeOffset ExpiresAtUtc,
    bool Consumed,
    bool Rotated,
    int KeyVersion = 0,
    Guid CapabilityId = default,
    int CapabilityVersion = 0,
    Guid ConcurrencyStamp = default)
{
    public override string ToString() =>
        $"AdmissionRecoveryCapabilityState(found={Found}, consumed={Consumed}, rotated={Rotated}, <redacted>)";
}

public sealed record AdmissionRecoveryMutationResult(AdmissionRecoveryMutationOutcome Outcome);

public sealed record AdmissionRecoveryDeliveryRequest(
    Guid TenantId,
    Guid RecoveryRequestId,
    Guid AdmissionTicketId,
    AdmissionRecoveryPurpose Purpose,
    string Capability,
    int CapabilityVersion = 1)
{
    public override string ToString() => "AdmissionRecoveryDeliveryRequest(<redacted>)";
}

public sealed record AdmissionRecoveryDeliveryResult(AdmissionRecoveryDeliveryOutcome Outcome);

public sealed record AdmissionRecoveryTicketDocument(
    Guid TicketId,
    Guid EventId,
    string StatusCode,
    string DisplayReference,
    string ManualCode,
    string ManualCodeClassificationCode,
    string QrRepresentation,
    string PrintModel)
{
    public override string ToString() =>
        $"AdmissionRecoveryTicketDocument(ticket={TicketId}, status={StatusCode}, <redacted>)";
}

public sealed record AdmissionRecoveryAuditFact(
    Guid TenantId,
    Guid RecoveryRequestId,
    string ActionCode,
    int CapabilityVersion,
    DateTimeOffset OccurredAtUtc);

public sealed record AdmissionRecoveryDeliveryEnvelope(
    string RecipientAddress,
    Guid RecoveryRequestId,
    string Capability)
{
    public override string ToString() => "AdmissionRecoveryDeliveryEnvelope(<redacted>)";
}

public sealed record AdmissionRecoveryProtectedDeliveryMaterial(string Ciphertext, int ProtectionVersion)
{
    public override string ToString() =>
        $"AdmissionRecoveryProtectedDeliveryMaterial(protectionVersion={ProtectionVersion}, <redacted>)";
}

public sealed class AdmissionRecoveryDeliveryIntent
{
    private AdmissionRecoveryDeliveryIntent()
    {
    }

    public AdmissionRecoveryDeliveryIntent(
        Guid id,
        Guid tenantId,
        Guid recoveryRequestId,
        Guid admissionTicketId,
        string purpose,
        int capabilityVersion,
        string protectedMaterial,
        int protectionVersion,
        DateTime createdAt)
    {
        if (id == Guid.Empty || tenantId == Guid.Empty || recoveryRequestId == Guid.Empty ||
            admissionTicketId == Guid.Empty || string.IsNullOrWhiteSpace(purpose) ||
            capabilityVersion < 1 || string.IsNullOrWhiteSpace(protectedMaterial) ||
            protectionVersion < 1 || createdAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Complete protected recovery delivery lineage is required.");
        }

        Id = id;
        TenantId = tenantId;
        RecoveryRequestId = recoveryRequestId;
        AdmissionTicketId = admissionTicketId;
        Purpose = purpose;
        CapabilityVersion = capabilityVersion;
        ProtectedMaterial = protectedMaterial;
        ProtectionVersion = protectionVersion;
        CreatedAt = createdAt;
        ConcurrencyStamp = Guid.CreateVersion7();
    }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid RecoveryRequestId { get; private set; }
    public Guid AdmissionTicketId { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public int CapabilityVersion { get; private set; }
    public string ProtectedMaterial { get; private set; } = string.Empty;
    public int ProtectionVersion { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RoutedAt { get; private set; }
    public DateTime? HandoffCompletedAt { get; private set; }
    public string? HandoffReceiptId { get; private set; }
    public Guid ConcurrencyStamp { get; private set; }

    public void MarkRouted(DateTime routedAtUtc)
    {
        if (routedAtUtc.Kind != DateTimeKind.Utc || routedAtUtc < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(routedAtUtc));
        }

        RoutedAt ??= routedAtUtc;
    }

    public void CompleteHandoff(string receiptId, DateTime completedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(receiptId) || completedAtUtc.Kind != DateTimeKind.Utc ||
            completedAtUtc < CreatedAt || RoutedAt is null)
        {
            throw new InvalidOperationException("Recovery delivery requires a routed receipt-bearing handoff.");
        }

        HandoffCompletedAt ??= completedAtUtc;
        HandoffReceiptId ??= receiptId.Trim();
        ProtectedMaterial = string.Empty;
    }

    public override string ToString() =>
        $"AdmissionRecoveryDeliveryIntent({Id}, request={RecoveryRequestId}, version={CapabilityVersion}, <redacted>)";
}

public static class AdmissionRecoveryDeliveryEvents
{
    public const string RecoveryDeliveryRequested = "AdmissionRecoveryDeliveryRequested";
}

public sealed record AdmissionRecoveryDeliveryPointer(
    Guid TenantId,
    Guid AdmissionTicketId,
    Guid DeliveryIntentId);

public enum AdmissionRecoveryDirectDeliveryOutcome
{
    Accepted,
    Ambiguous
}

public sealed record AdmissionRecoveryDirectDeliveryRequest(
    Guid TenantId,
    Guid DeliveryIntentId,
    Guid AdmissionTicketId,
    Guid RecoveryRequestId,
    string RecipientAddress,
    string Capability)
{
    public override string ToString() => "AdmissionRecoveryDirectDeliveryRequest(<redacted>)";
}

public sealed record AdmissionRecoveryDirectDeliveryResult(
    AdmissionRecoveryDirectDeliveryOutcome Outcome,
    string? ReceiptId = null);

public interface IAdmissionRecoveryCapabilityService
{
    Task<AdmissionRecoveryCapabilityMaterial> IssueAsync(
        AdmissionRecoveryCapabilityIssueRequest request,
        CancellationToken cancellationToken);

    Task<AdmissionRecoveryCapabilityDigest> DigestAsync(
        AdmissionRecoveryCapabilityDigestRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AdmissionRecoveryLocatorDigest>> DigestLocatorsAsync(
        string capability,
        CancellationToken cancellationToken);
}

public interface IAdmissionRecoveryDeliveryService
{
    Task<AdmissionRecoveryDeliveryResult> DeliverAsync(
        AdmissionRecoveryDeliveryRequest request,
        CancellationToken cancellationToken);
}

public interface IAdmissionRecoveryDeliveryStager
{
    Task<AdmissionRecoveryDeliveryResult> StageAsync(
        AdmissionRecoveryDeliveryRequest request,
        CancellationToken cancellationToken);
}

public interface IAdmissionRecoveryDeliveryEnvelopeProtector
{
    AdmissionRecoveryProtectedDeliveryMaterial Protect(AdmissionRecoveryDeliveryEnvelope envelope);
    AdmissionRecoveryDeliveryEnvelope Unprotect(string ciphertext, int protectionVersion);
}

public interface IAdmissionRecoveryDirectDeliveryChannel
{
    Task<AdmissionRecoveryDirectDeliveryResult> DeliverAsync(
        AdmissionRecoveryDirectDeliveryRequest request,
        CancellationToken cancellationToken);
}

public interface IAdmissionRecoveryDeliveryOutboxHandler
{
    Task HandleAsync(Explore.Domain.OutboxMessage message, CancellationToken cancellationToken);
}

public interface IAdmissionRecoveryTicketDocumentService
{
    Task<AdmissionRecoveryTicketDocument?> RotateAndCreateAsync(
        Guid tenantId,
        Guid admissionTicketId,
        CancellationToken cancellationToken);
}

public interface IAdmissionRecoveryAuditService
{
    Task AppendAsync(
        AdmissionRecoveryAuditFact fact,
        CancellationToken cancellationToken);
}

public interface IAdmissionTicketRecoveryRepository
{
    Task<Explore.Domain.AdmissionTicket?> GetForUpdateAsync(
        Guid tenantId,
        Guid admissionTicketId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IAdmissionRecoveryRepository
{
    Task<AdmissionRecoveryIdentityResult> FindIdentityAsync(
        AdmissionRecoveryRequest request,
        CancellationToken cancellationToken);

    Task<AdmissionRecoveryMutationResult> StoreAsync(
        AdmissionRecoveryCapabilityRecord request,
        CancellationToken cancellationToken);

    Task<AdmissionRecoveryCapabilityState> GetByDigestAsync(
        AdmissionRecoveryCapabilityLookup request,
        CancellationToken cancellationToken);

    Task<AdmissionRecoveryCapabilityState> GetByLocatorAsync(
        Guid tenantId,
        IReadOnlyList<AdmissionRecoveryLocatorDigest> locators,
        CancellationToken cancellationToken);

    Task<AdmissionRecoveryCapabilityState> GetCurrentByRequestIdAsync(
        Guid tenantId,
        Guid recoveryRequestId,
        AdmissionRecoveryPurpose purpose,
        CancellationToken cancellationToken);

    Task<AdmissionRecoveryMutationResult> ConsumeAsync(
        AdmissionRecoveryCapabilityMutation request,
        CancellationToken cancellationToken);

    Task<AdmissionRecoveryMutationResult> RotateAsync(
        AdmissionRecoveryRotationRequest request,
        CancellationToken cancellationToken);
}
