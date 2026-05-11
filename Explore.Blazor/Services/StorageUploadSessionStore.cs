// ABOUTME: BFF-owned storage upload session store for binding browser upload proxy calls.
// ABOUTME: Prevents client-supplied upload URLs by resolving exact server-issued destinations from cache.

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Explore.Application.DTOs.StorageObject;
using Microsoft.Extensions.Caching.Distributed;

namespace Explore.Blazor.Services;

public sealed record StorageUploadSession(
    string SessionId,
    string OwnerUserId,
    string UploadUrl,
    string ObjectKey,
    string ViewUrl,
    string ContentType,
    DateTimeOffset ExpiresAtUtc);

public sealed record StorageUploadSessionIssueResult(
    bool Success,
    string? SessionId,
    string? ObjectKey,
    string? ViewUrl,
    int ExpiresInMinutes,
    string? FailureCode)
{
    public static StorageUploadSessionIssueResult Failed(string failureCode) =>
        new(false, null, null, null, 0, failureCode);

    public static StorageUploadSessionIssueResult Issued(
        StorageUploadSession session,
        int expiresInMinutes) =>
        new(true, session.SessionId, session.ObjectKey, session.ViewUrl, expiresInMinutes, null);
}

public sealed record StorageUploadSessionResolveResult(
    bool Success,
    StorageUploadSession? Session,
    string? FailureCode)
{
    public static StorageUploadSessionResolveResult Failed(string failureCode) =>
        new(false, null, failureCode);

    public static StorageUploadSessionResolveResult Resolved(StorageUploadSession session) =>
        new(true, session, null);
}

public interface IStorageUploadSessionStore
{
    Task<StorageUploadSessionIssueResult> IssueAsync(
        ClaimsPrincipal user,
        UploadUrlResponseDto response,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<StorageUploadSessionResolveResult> ResolveAsync(
        ClaimsPrincipal user,
        string sessionId,
        string contentType,
        CancellationToken cancellationToken = default);

    Task ConsumeAsync(string sessionId, CancellationToken cancellationToken = default);
}

public sealed class StorageUploadSessionStore(IDistributedCache cache) : IStorageUploadSessionStore
{
    private const string CacheKeyPrefix = "storage-upload-session:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<StorageUploadSessionIssueResult> IssueAsync(
        ClaimsPrincipal user,
        UploadUrlResponseDto response,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = GetRequiredUserId(user);
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            return StorageUploadSessionIssueResult.Failed("missing_user");
        }

        if (!IsTrustedPresignedUploadUrl(response.UploadUrl))
        {
            return StorageUploadSessionIssueResult.Failed("invalid_upload_url");
        }

        if (string.IsNullOrWhiteSpace(response.ObjectKey))
        {
            return StorageUploadSessionIssueResult.Failed("missing_object_key");
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return StorageUploadSessionIssueResult.Failed("missing_content_type");
        }

        var expiresInMinutes = response.ExpiresInMinutes <= 0
            ? 15
            : Math.Min(response.ExpiresInMinutes, 60);
        var session = new StorageUploadSession(
            SessionId: RandomNumberGenerator.GetHexString(32).ToLowerInvariant(),
            OwnerUserId: ownerUserId,
            UploadUrl: response.UploadUrl.Trim(),
            ObjectKey: response.ObjectKey.Trim(),
            ViewUrl: response.ViewUrl?.Trim() ?? string.Empty,
            ContentType: contentType.Trim(),
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(expiresInMinutes));

        var payload = JsonSerializer.Serialize(session, JsonOptions);
        await cache.SetStringAsync(
            BuildCacheKey(session.SessionId),
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = session.ExpiresAtUtc
            },
            cancellationToken);

        return StorageUploadSessionIssueResult.Issued(session, expiresInMinutes);
    }

    public async Task<StorageUploadSessionResolveResult> ResolveAsync(
        ClaimsPrincipal user,
        string sessionId,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = GetRequiredUserId(user);
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            return StorageUploadSessionResolveResult.Failed("missing_user");
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return StorageUploadSessionResolveResult.Failed("missing_session");
        }

        var payload = await cache.GetStringAsync(BuildCacheKey(sessionId.Trim()), cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return StorageUploadSessionResolveResult.Failed("session_not_found");
        }

        StorageUploadSession? session;
        try
        {
            session = JsonSerializer.Deserialize<StorageUploadSession>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            return StorageUploadSessionResolveResult.Failed("session_corrupt");
        }

        if (session is null)
        {
            return StorageUploadSessionResolveResult.Failed("session_corrupt");
        }

        if (!string.Equals(session.OwnerUserId, ownerUserId, StringComparison.Ordinal))
        {
            return StorageUploadSessionResolveResult.Failed("session_owner_mismatch");
        }

        if (session.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            await ConsumeAsync(session.SessionId, cancellationToken);
            return StorageUploadSessionResolveResult.Failed("session_expired");
        }

        if (!string.Equals(session.ContentType, contentType.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return StorageUploadSessionResolveResult.Failed("content_type_mismatch");
        }

        return StorageUploadSessionResolveResult.Resolved(session);
    }

    public Task ConsumeAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.CompletedTask;
        }

        return cache.RemoveAsync(BuildCacheKey(sessionId.Trim()), cancellationToken);
    }

    private static bool IsTrustedPresignedUploadUrl(string uploadUrl)
    {
        if (!Uri.TryCreate(uploadUrl, UriKind.Absolute, out var uploadUri) ||
            !string.Equals(uploadUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var query = uploadUri.Query;
        return query.Contains("X-Amz-Algorithm", StringComparison.OrdinalIgnoreCase) &&
            query.Contains("X-Amz-Signature", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetRequiredUserId(ClaimsPrincipal user) =>
        user.FindFirstValue("sub")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sid");

    private static string BuildCacheKey(string sessionId) => CacheKeyPrefix + sessionId;
}
