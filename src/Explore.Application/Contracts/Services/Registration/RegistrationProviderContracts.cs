// ABOUTME: Provider-neutral registration capability contracts and typed D3 request/result records.
// ABOUTME: Keeps provider operations segregated so downstream callbacks depend on capabilities, not provider names.

using Explore.Domain;

namespace Explore.Application.Contracts.Services.Registration;

public sealed record RegistrationProviderTuple(
    string ProviderCode,
    string DeploymentKind,
    string ApiVersion,
    string AdapterPolicyVersion,
    string ConformanceEvidenceRevision)
{
    public string Key => string.Join('|', ProviderCode, DeploymentKind, ApiVersion, AdapterPolicyVersion, ConformanceEvidenceRevision);
}

public sealed record RegistrationProviderCapabilitySet(
    bool Redirect,
    bool Embed,
    bool Manual,
    bool SchemaRead,
    bool FormProvision,
    bool SubmissionWrite,
    bool SubmissionRead,
    bool CallbackVerification,
    bool SubscriptionManagement,
    bool Reconciliation,
    bool SubmissionSink,
    bool AutoFinalize)
{
    public static RegistrationProviderCapabilitySet None { get; } = new(false, false, false, false, false, false, false, false, false, false, false, false);
    public static RegistrationProviderCapabilitySet Native { get; } = new(true, false, true, true, true, true, true, false, false, false, true, true);

    public RegistrationProviderCapabilitySet Intersect(RegistrationProviderCapabilitySet other) => new(
        Redirect && other.Redirect,
        Embed && other.Embed,
        Manual && other.Manual,
        SchemaRead && other.SchemaRead,
        FormProvision && other.FormProvision,
        SubmissionWrite && other.SubmissionWrite,
        SubmissionRead && other.SubmissionRead,
        CallbackVerification && other.CallbackVerification,
        SubscriptionManagement && other.SubscriptionManagement,
        Reconciliation && other.Reconciliation,
        SubmissionSink && other.SubmissionSink,
        AutoFinalize && other.AutoFinalize);

    public static RegistrationProviderCapabilitySet FromCodes(IEnumerable<string> codes)
    {
        HashSet<string> values = [.. codes.Select(code => code.Trim().ToUpperInvariant())];
        return new(
            values.Contains(RegistrationProviderCapabilityCodes.Redirect),
            values.Contains(RegistrationProviderCapabilityCodes.Embed),
            values.Contains(RegistrationProviderCapabilityCodes.Manual),
            values.Contains(RegistrationProviderCapabilityCodes.SchemaRead),
            values.Contains(RegistrationProviderCapabilityCodes.FormProvision),
            values.Contains(RegistrationProviderCapabilityCodes.SubmissionWrite),
            values.Contains(RegistrationProviderCapabilityCodes.SubmissionRead),
            values.Contains(RegistrationProviderCapabilityCodes.CallbackVerification),
            values.Contains(RegistrationProviderCapabilityCodes.SubscriptionManagement),
            values.Contains(RegistrationProviderCapabilityCodes.Reconciliation),
            values.Contains(RegistrationProviderCapabilityCodes.SubmissionSink),
            values.Contains(RegistrationProviderCapabilityCodes.AutoFinalize));
    }
}

public static class RegistrationProviderCapabilityCodes
{
    public const string Redirect = "REDIRECT";
    public const string Embed = "EMBED";
    public const string Manual = "MANUAL";
    public const string SchemaRead = "SCHEMA_READ";
    public const string FormProvision = "FORM_PROVISION";
    public const string SubmissionWrite = "SUBMISSION_WRITE";
    public const string SubmissionRead = "SUBMISSION_READ";
    public const string CallbackVerification = "CALLBACK_VERIFICATION";
    public const string SubscriptionManagement = "SUBSCRIPTION_MANAGEMENT";
    public const string Reconciliation = "RECONCILIATION";
    public const string SubmissionSink = "SUBMISSION_SINK";
    public const string AutoFinalize = "AUTO_FINALIZE";
}

public interface IRegistrationProviderDescriptor
{
    RegistrationProviderTuple Tuple { get; }
    RegistrationProviderCapabilitySet ProvenCapabilities { get; }
}

public interface IRegistrationProviderPresentation
{
    Task<RegistrationProviderPresentationResult> GetPresentationAsync(RegistrationProviderPresentationRequest request, CancellationToken cancellationToken);
}

public interface IRegistrationProviderSchemaReader
{
    Task<RegistrationProviderSchemaReadResult> ReadSchemaAsync(RegistrationProviderSchemaReadRequest request, CancellationToken cancellationToken);
}

public interface IRegistrationProviderFormProvisioner
{
    Task<RegistrationProviderFormProvisionResult> ProvisionFormAsync(RegistrationProviderFormProvisionRequest request, CancellationToken cancellationToken);
}

public interface IRegistrationProviderSubmissionWriter
{
    Task<RegistrationProviderSubmissionWriteResult> WriteSubmissionAsync(RegistrationProviderSubmissionWriteRequest request, CancellationToken cancellationToken);
}

public interface IRegistrationProviderSubmissionReader
{
    Task<RegistrationProviderSubmissionReadResult> ReadSubmissionAsync(RegistrationProviderSubmissionReadRequest request, CancellationToken cancellationToken);
}

public interface IRegistrationProviderCallbackVerifier
{
    Task<RegistrationProviderCallbackVerificationResult> VerifyCallbackAsync(RegistrationProviderCallbackVerificationRequest request, CancellationToken cancellationToken);
}

public interface IRegistrationProviderCallbackReceiptProtector
{
    string Protect(RegistrationProviderCallbackReceipt receipt);

    RegistrationProviderCallbackReceipt Unprotect(string protectedReceipt);
}

public interface IRegistrationProviderCallbackBindingResolver
{
    Task<RegistrationProviderBinding?> ResolveForCallbackAsync(
        string provider,
        Guid bindingId,
        CancellationToken cancellationToken);
}

public interface IRegistrationProviderSubscriptionManager
{
    Task<RegistrationProviderSubscriptionResult> EnsureSubscriptionAsync(RegistrationProviderSubscriptionRequest request, CancellationToken cancellationToken);
}

public interface IRegistrationProviderReconciliationProvider
{
    Task<RegistrationProviderReconciliationResult> ReconcileAsync(RegistrationProviderReconciliationRequest request, CancellationToken cancellationToken);
}

public interface IRegistrationProviderSubmissionSink
{
    Task<RegistrationProviderSubmissionSinkResult> AcceptAsync(RegistrationProviderSubmissionSinkRequest request, CancellationToken cancellationToken);
}

public interface IRegistrationProviderRegistry
{
    IRegistrationProviderDescriptor? TryResolve(RegistrationProviderTuple tuple);
}

public sealed record RegistrationProviderPresentationRequest(Guid TenantId, Guid BindingId);
public sealed record RegistrationProviderPresentationResult(bool RedirectAvailable, bool EmbedAvailable, bool ManualAvailable, Uri? RedirectUri = null, Uri? EmbedUri = null);
public sealed record RegistrationProviderSchemaReadRequest(Guid TenantId, Guid ConnectionId);
public sealed record RegistrationProviderSchemaReadResult(RegistrationProviderSchemaSnapshot Snapshot);
public sealed record RegistrationProviderFormProvisionRequest(Guid TenantId, Guid BindingId, Guid FormVersionId);
public sealed record RegistrationProviderFormProvisionResult(string ProviderFormId, string ProviderRevisionId);
public sealed record RegistrationProviderSubmissionWriteRequest(Guid TenantId, Guid AttemptId, IReadOnlyDictionary<string, string> Answers);
public sealed record RegistrationProviderSubmissionWriteResult(string ProviderSubmissionId, string ProviderRevisionId);
public sealed record RegistrationProviderSubmissionReadRequest(Guid TenantId, Guid BindingId, string ProviderSubmissionId);
public sealed record RegistrationProviderSubmissionReadResult(string ProviderSubmissionId, string ProviderRevisionId, IReadOnlyDictionary<string, string> Answers);
public sealed record RegistrationProviderCallbackVerificationRequest(Guid TenantId, Guid ConnectionId, ReadOnlyMemory<byte> Body, IReadOnlyDictionary<string, string> Headers);
public sealed record RegistrationProviderCallbackVerificationResult(bool IsVerified, string? FailureCode = null, string? Receipt = null);
public sealed record RegistrationProviderCallbackReceipt(
    Guid TenantId,
    Guid ConnectionId,
    Guid BindingId,
    string Provider,
    string TupleKey,
    string BodySha256,
    string ProviderSubmissionId,
    DateTimeOffset VerifiedAt,
    string Nonce);
public sealed record RegistrationProviderSubscriptionRequest(Guid TenantId, Guid ConnectionId, Uri CallbackUri);
public sealed record RegistrationProviderSubscriptionResult(bool IsActive, string? ProviderSubscriptionId);
public sealed record RegistrationProviderReconciliationRequest(Guid TenantId, Guid BindingId, DateTime SinceUtc);
public sealed record RegistrationProviderReconciliationResult(int ObservedSubmissionCount, bool HasMore);
public sealed record RegistrationProviderSubmissionSinkRequest(Guid TenantId, Guid AttemptId, RegistrationEvidenceHash EvidenceHash, string? ProviderSubmissionId);
public sealed record RegistrationProviderSubmissionSinkResult(bool Accepted, Guid SubmissionId, bool AutoFinalizable);

public sealed record RegistrationProviderSchemaSnapshot(IReadOnlyList<RegistrationProviderSchemaFieldSnapshot> Fields);

public sealed record RegistrationProviderSchemaFieldSnapshot(
    string Key,
    string Label,
    string Type,
    bool IsRequired,
    IReadOnlyList<RegistrationProviderSchemaOptionSnapshot> Options);

public sealed record RegistrationProviderSchemaOptionSnapshot(string Key, string Label);
