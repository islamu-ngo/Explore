// ABOUTME: Shared intake implementation for incoming integration webhooks.
// ABOUTME: Reads raw request bodies, delegates verification, and writes idempotency ledger rows before dispatch.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Domain;
using Microsoft.Extensions.Primitives;

namespace Explore.API.Services;

public sealed class IncomingWebhookIntakeService(
    IIncomingWebhookVerifierRegistry verifierRegistry,
    IIncomingWebhookMessageRepository incomingWebhookMessageRepository,
    ILogger<IncomingWebhookIntakeService> logger) : IIncomingWebhookIntakeService
{
    private const int BufferThresholdBytes = 30 * 1024;
    private const int MaxProviderLength = 100;
    private const int MaxProviderMessageIdLength = 500;
    private const int MaxIdempotencyKeyLength = 500;
    private const int MaxEventTypeLength = 200;
    private const int MaxSafeHeaderValueLength = 256;

    private static readonly string[] SensitiveHeaderFragments =
    [
        "authorization",
        "cookie",
        "signature",
        "secret",
        "token",
        "api-key",
        "apikey",
        "key"
    ];

    public async Task<IncomingWebhookReadResult> ReadAndVerifyAsync(
        HttpRequest request,
        string provider,
        long maxBodyBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedProvider = NormalizeRequired(provider, MaxProviderLength);
        var headers = CaptureHeaders(request.Headers);

        byte[] bodyBytes;
        try
        {
            bodyBytes = await ReadBodyBytesAsync(request, maxBodyBytes, cancellationToken);
        }
        catch (IOException)
        {
            return IncomingWebhookReadResult.Failure(
                normalizedProvider,
                StatusCodes.Status413PayloadTooLarge,
                "Incoming webhook body is too large",
                "The incoming webhook body exceeds the configured size limit.",
                $"{normalizedProvider}_webhook_body_too_large");
        }

        if (bodyBytes.Length == 0)
        {
            return IncomingWebhookReadResult.Failure(
                normalizedProvider,
                StatusCodes.Status400BadRequest,
                "Empty incoming webhook body",
                "The incoming webhook body is required.",
                $"{normalizedProvider}_webhook_body_empty");
        }

        var rawPayload = Encoding.UTF8.GetString(bodyBytes);
        var payloadHash = ComputePayloadHash(bodyBytes);
        var verifier = verifierRegistry.GetRequired(normalizedProvider);
        var verification = await verifier.VerifyAsync(
            new IncomingWebhookContext(normalizedProvider, rawPayload, headers, DateTimeOffset.UtcNow),
            cancellationToken);

        if (!verification.IsVerified)
        {
            var failureCategory = NormalizeFailureCategory(verification.FailureCategory, normalizedProvider);
            var statusCode = MapVerificationStatusCode(failureCategory);
            return IncomingWebhookReadResult.Failure(
                normalizedProvider,
                statusCode,
                "Incoming webhook verification failed",
                verification.SafeDetail ?? "The incoming webhook could not be verified.",
                failureCategory);
        }

        return IncomingWebhookReadResult.Success(normalizedProvider, rawPayload, payloadHash, headers, verification);
    }

    public async Task<IncomingWebhookCaptureResult> CaptureAsync(
        IncomingWebhookReadResult readResult,
        Guid tenantId,
        string? providerMessageId,
        string? eventType,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(readResult);

        if (!readResult.Succeeded || readResult.Verification is null || string.IsNullOrWhiteSpace(readResult.PayloadHash))
        {
            return IncomingWebhookCaptureResult.Failure(
                StatusCodes.Status400BadRequest,
                "Incoming webhook was not verified",
                "Only verified incoming webhooks can be captured.",
                $"{readResult.Provider}_webhook_not_verified");
        }

        if (tenantId == Guid.Empty)
        {
            return IncomingWebhookCaptureResult.Failure(
                StatusCodes.Status400BadRequest,
                "Incoming webhook tenant is required",
                "The incoming webhook could not be associated with a tenant.",
                $"{readResult.Provider}_webhook_tenant_required");
        }

        var normalizedProvider = NormalizeRequired(readResult.Provider, MaxProviderLength);
        var normalizedProviderMessageId = NormalizeOptional(providerMessageId, MaxProviderMessageIdLength)
            ?? NormalizeOptional(readResult.Verification.ProviderMessageId, MaxProviderMessageIdLength)
            ?? NormalizeRequired(readResult.PayloadHash, MaxProviderMessageIdLength);
        var normalizedIdempotencyKey = NormalizeOptional(idempotencyKey, MaxIdempotencyKeyLength)
            ?? NormalizeOptional(readResult.Verification.IdempotencyKey, MaxIdempotencyKeyLength)
            ?? normalizedProviderMessageId;

        var now = DateTime.UtcNow;
        var message = new IncomingWebhookMessage
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Provider = normalizedProvider,
            ProviderMessageId = normalizedProviderMessageId,
            IdempotencyKey = normalizedIdempotencyKey,
            EventType = NormalizeOptional(eventType, MaxEventTypeLength)
                ?? NormalizeOptional(readResult.Verification.EventType, MaxEventTypeLength),
            HeadersJson = SerializeSafeHeaders(readResult.Headers),
            PayloadJson = null,
            PayloadHash = readResult.PayloadHash,
            Status = IncomingWebhookMessageStatus.Verified,
            ReceivedAt = now,
            VerifiedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await incomingWebhookMessageRepository.TryCreateAsync(message, cancellationToken);
        if (created)
        {
            return IncomingWebhookCaptureResult.Captured(message.Id, normalizedProviderMessageId, normalizedIdempotencyKey);
        }

        var existing = await incomingWebhookMessageRepository.GetByProviderMessageIdAsync(
            tenantId,
            normalizedProvider,
            normalizedProviderMessageId,
            cancellationToken);
        logger.LogInformation(
            "Incoming webhook duplicate captured for provider {Provider}",
            normalizedProvider);

        return IncomingWebhookCaptureResult.Duplicate(
            existing?.Id ?? Guid.Empty,
            normalizedProviderMessageId,
            normalizedIdempotencyKey);
    }

    public async Task MarkProcessedAsync(
        Guid tenantId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || messageId == Guid.Empty)
        {
            return;
        }

        await incomingWebhookMessageRepository.MarkProcessedAsync(tenantId, messageId, DateTime.UtcNow, cancellationToken);
    }

    public async Task MarkRejectedAsync(
        Guid tenantId,
        Guid messageId,
        string failureCategory,
        string? safeDetail,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || messageId == Guid.Empty)
        {
            return;
        }

        await incomingWebhookMessageRepository.MarkRejectedAsync(
            tenantId,
            messageId,
            NormalizeRequired(failureCategory, 100),
            safeDetail,
            DateTime.UtcNow,
            cancellationToken);
    }

    private static async Task<byte[]> ReadBodyBytesAsync(
        HttpRequest request,
        long maxBodyBytes,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength > maxBodyBytes)
        {
            throw new IOException("Incoming webhook request body exceeded the configured size limit.");
        }

        request.EnableBuffering(BufferThresholdBytes, maxBodyBytes);
        await using var memory = new MemoryStream();
        var buffer = new byte[81920];
        long totalBytes = 0;
        while (true)
        {
            var bytesRead = await request.Body.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;
            if (totalBytes > maxBodyBytes)
            {
                throw new IOException("Incoming webhook request body exceeded the configured size limit.");
            }

            await memory.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        request.Body.Position = 0;
        return memory.ToArray();
    }

    private static IReadOnlyDictionary<string, string> CaptureHeaders(IHeaderDictionary headers)
    {
        Dictionary<string, string> captured = new(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            captured[header.Key] = JoinHeaderValues(header.Value);
        }

        return captured;
    }

    private static string JoinHeaderValues(StringValues values) =>
        string.Join(",", values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));

    private static string ComputePayloadHash(byte[] bodyBytes)
    {
        var hash = SHA256.HashData(bodyBytes);
        return "sha256:" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string SerializeSafeHeaders(IReadOnlyDictionary<string, string> headers)
    {
        SortedDictionary<string, string> safe = new(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            if (IsSensitiveHeader(header.Key))
            {
                continue;
            }

            safe[header.Key] = Truncate(header.Value, MaxSafeHeaderValueLength);
        }

        return JsonSerializer.Serialize(safe);
    }

    private static bool IsSensitiveHeader(string headerName) =>
        SensitiveHeaderFragments.Any(fragment => headerName.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static int MapVerificationStatusCode(string failureCategory)
    {
        if (failureCategory.Contains("not_configured", StringComparison.OrdinalIgnoreCase) ||
            failureCategory.Contains("secret_missing", StringComparison.OrdinalIgnoreCase) ||
            failureCategory.Contains("secret_unresolved", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status503ServiceUnavailable;
        }

        if (failureCategory.Contains("body_too_large", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status413PayloadTooLarge;
        }

        if (failureCategory.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
            failureCategory.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
            failureCategory.Contains("timestamp", StringComparison.OrdinalIgnoreCase) ||
            failureCategory.Contains("signature", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status401Unauthorized;
        }

        return StatusCodes.Status400BadRequest;
    }

    private static string NormalizeFailureCategory(string? failureCategory, string provider)
    {
        if (string.IsNullOrWhiteSpace(failureCategory))
        {
            return $"{provider}_webhook_verification_failed";
        }

        return Truncate(failureCategory.Trim(), 100);
    }

    private static string NormalizeRequired(string value, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Truncate(value.Trim().ToLowerInvariant(), maxLength);
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Truncate(value.Trim(), maxLength);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
