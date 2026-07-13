// ABOUTME: Infrastructure-local contracts for the Svix SDK wrapper used by webhook delivery.
// ABOUTME: Keeps provider tests deterministic while the production adapter owns Svix SDK calls.

namespace Explore.Infrastructure.Webhooks;

public interface ISvixWebhookClient
{
    Task<SvixApplicationBindingResult> GetApplicationAsync(
        string applicationId,
        CancellationToken cancellationToken);

    Task<SvixApplicationSyncResult> GetOrCreateApplicationAsync(
        SvixApplicationSyncRequest request,
        CancellationToken cancellationToken);

    Task<SvixMessageCreateResult> CreateMessageAsync(
        SvixMessageCreateRequest request,
        CancellationToken cancellationToken);

    Task<SvixAppPortalAccessResult> CreateAppPortalAccessAsync(
        SvixAppPortalAccessRequest request,
        CancellationToken cancellationToken);

    Task<SvixEventTypeSyncResult> UpsertEventTypeAsync(
        SvixEventTypeSyncRequest request,
        CancellationToken cancellationToken);
}

public sealed record SvixApplicationBindingResult(
    string AppId,
    string AppUid,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record SvixApplicationSyncRequest(
    Guid TenantId,
    string AppUid,
    string Name,
    IReadOnlyDictionary<string, string> Metadata,
    string IdempotencyKey);

public sealed record SvixApplicationSyncResult(
    string AppId,
    string AppUid);

public sealed record SvixMessageCreateRequest(
    Guid TenantId,
    string AppUid,
    string EventType,
    string EventId,
    byte[] PayloadBytes,
    int PayloadRetentionDays,
    string IdempotencyKey);

public sealed record SvixMessageCreateResult(
    string MessageId);

public sealed record SvixAppPortalAccessRequest(
    Guid TenantId,
    string AppId,
    string SessionId,
    bool ReadOnly,
    TimeSpan ExpiresIn,
    IReadOnlyCollection<string> FeatureFlags,
    string IdempotencyKey);

public sealed record SvixAppPortalAccessResult(
    string Url,
    string? Token);

public sealed record SvixEventTypeSyncRequest(
    string Name,
    string Description,
    string GroupName,
    string SchemaJson,
    string IdempotencyKey);

public sealed record SvixEventTypeSyncResult(
    string Name);
