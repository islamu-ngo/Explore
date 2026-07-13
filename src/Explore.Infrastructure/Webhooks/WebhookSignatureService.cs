// ABOUTME: Svix-compatible webhook signature implementation for outgoing and incoming webhook payloads.
// ABOUTME: Uses raw-body HMAC verification, timestamp tolerance, and fixed-time comparisons.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Explore.Application.Contracts.Webhooks;

namespace Explore.Infrastructure.Webhooks;

public sealed class WebhookSignatureService : IWebhookSignatureService
{
    internal static readonly TimeSpan TimestampTolerance = TimeSpan.FromMinutes(5);
    private readonly TimeProvider _timeProvider;

    public WebhookSignatureService()
        : this(TimeProvider.System)
    {
    }

    internal WebhookSignatureService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public WebhookSignatureHeaders Sign(
        string messageId,
        DateTimeOffset timestamp,
        string rawPayload,
        WebhookSecretMaterial secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentNullException.ThrowIfNull(rawPayload);

        var unixTimestamp = timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var signatures = GetActiveSecrets(secret, _timeProvider.GetUtcNow())
            .Select(activeSecret =>
                $"v1,{Convert.ToBase64String(ComputeSignature(messageId, unixTimestamp, rawPayload, DecodeSecret(activeSecret)))}");

        return new WebhookSignatureHeaders(messageId, unixTimestamp, string.Join(' ', signatures));
    }

    public WebhookVerificationResult Verify(
        string rawPayload,
        IReadOnlyDictionary<string, string> headers,
        WebhookSecretMaterial secret)
    {
        ArgumentNullException.ThrowIfNull(rawPayload);
        ArgumentNullException.ThrowIfNull(headers);

        if (!TryGetHeader(headers, "svix-id", out var messageId)
            || !TryGetHeader(headers, "svix-timestamp", out var timestampHeader)
            || !TryGetHeader(headers, "svix-signature", out var signatureHeader))
        {
            return WebhookVerificationResult.Failure("missing_header");
        }

        if (!long.TryParse(timestampHeader, out var unixTimestamp))
        {
            return WebhookVerificationResult.Failure("invalid_timestamp");
        }

        DateTimeOffset timestamp;
        try
        {
            timestamp = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        }
        catch (ArgumentOutOfRangeException)
        {
            return WebhookVerificationResult.Failure("invalid_timestamp");
        }

        var now = _timeProvider.GetUtcNow();
        if (timestamp < now.Subtract(TimestampTolerance) || timestamp > now.Add(TimestampTolerance))
        {
            return WebhookVerificationResult.Failure("timestamp_outside_tolerance");
        }

        if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return WebhookVerificationResult.Failure("missing_header");
        }

        var secrets = GetActiveSecrets(secret, now);
        if (secrets.Count == 0)
        {
            return WebhookVerificationResult.Failure("invalid_secret");
        }

        foreach (var activeSecret in secrets)
        {
            byte[] secretBytes;
            try
            {
                secretBytes = DecodeSecret(activeSecret);
            }
            catch (FormatException)
            {
                return WebhookVerificationResult.Failure("invalid_secret");
            }

            var expected = ComputeSignature(messageId, timestampHeader, rawPayload, secretBytes);
            if (ContainsMatchingSignature(signatureHeader, expected))
            {
                return WebhookVerificationResult.Success(timestamp);
            }
        }

        return WebhookVerificationResult.Failure("signature_mismatch");
    }

    private static List<string> GetActiveSecrets(WebhookSecretMaterial secret, DateTimeOffset now)
    {
        List<string> secrets = [secret.CurrentSecret];

        if (!string.IsNullOrWhiteSpace(secret.PreviousSecret)
            && secret.PreviousSecretValidUntil is { } validUntil
            && validUntil >= now)
        {
            secrets.Add(secret.PreviousSecret);
        }

        return secrets;
    }

    private static byte[] ComputeSignature(
        string messageId,
        string timestamp,
        string rawPayload,
        byte[] secret)
    {
        var signedContent = $"{messageId}.{timestamp}.{rawPayload}";
        return HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(signedContent));
    }

    private static byte[] DecodeSecret(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        var normalized = secret.StartsWith("whsec_", StringComparison.Ordinal)
            ? secret["whsec_".Length..]
            : secret;

        return Convert.FromBase64String(normalized);
    }

    private static bool ContainsMatchingSignature(string signatureHeader, byte[] expected)
    {
        foreach (var signaturePart in signatureHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var segments = signaturePart.Split(',', 2, StringSplitOptions.TrimEntries);
            if (segments.Length != 2 || !segments[0].Equals("v1", StringComparison.Ordinal))
            {
                continue;
            }

            byte[] supplied;
            try
            {
                supplied = Convert.FromBase64String(segments[1]);
            }
            catch (FormatException)
            {
                continue;
            }

            if (supplied.Length == expected.Length && CryptographicOperations.FixedTimeEquals(supplied, expected))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetHeader(
        IReadOnlyDictionary<string, string> headers,
        string name,
        out string value)
    {
        foreach (var header in headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = header.Value;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }
}
