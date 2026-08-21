// ABOUTME: Computes stable request identity metadata for Idempotency-Key replay validation.
// ABOUTME: Canonicalizes JSON request bodies so equivalent formatting does not change fingerprints.

using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.IO;
using Microsoft.Net.Http.Headers;

namespace Explore.API.Middleware;

internal sealed record IdempotencyRequestIdentity(
    string Method,
    string RequestTarget,
    string? ContentType,
    string BodyHash,
    string PrincipalFingerprint,
    string? UserId);

internal static class IdempotencyRequestIdentityFactory
{
    private static readonly string[] CapabilityHeaders =
    [
        "X-Registration-Order-Capability",
        "X-Registration-Attempt-Capability"
    ];

    public static async Task<IdempotencyRequestIdentity> CreateAsync(
        HttpContext context,
        RecyclableMemoryStreamManager streamManager,
        CancellationToken cancellationToken)
    {
        var method = context.Request.Method.ToUpperInvariant();
        var requestTarget = ResolveRequestTarget(context);
        var contentType = NormalizeContentType(context.Request.ContentType);
        var userId = ResolveUserId(context);
        var principalFingerprint = ComputeSha256Hex(
            $"{(userId is null ? "anonymous" : $"authenticated:{userId}")}|capabilities:{CapabilityScope(context.Request)}");
        var bodyHash = await ComputeBodyHashAsync(context.Request, streamManager, cancellationToken);

        return new IdempotencyRequestIdentity(
            method,
            requestTarget,
            contentType,
            bodyHash,
            principalFingerprint,
            userId);
    }

    private static async Task<string> ComputeBodyHashAsync(
        HttpRequest request,
        RecyclableMemoryStreamManager streamManager,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();

        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        using var bodyStream = streamManager.GetStream("idempotency-request-body");
        await request.Body.CopyToAsync(bodyStream, cancellationToken);

        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        if (bodyStream.Length == 0)
        {
            return ComputeSha256Hex(ReadOnlySpan<byte>.Empty);
        }

        bodyStream.Position = 0;
        if (IsJsonContentType(request.ContentType)
            && TryCanonicalizeJson(bodyStream, streamManager, out var canonicalJson))
        {
            using (canonicalJson)
            {
                return ComputeSha256Hex(canonicalJson.ToArray());
            }
        }

        bodyStream.Position = 0;
        return bodyStream.TryGetBuffer(out var buffer)
            ? ComputeSha256Hex(buffer.AsSpan(0, (int)bodyStream.Length))
            : ComputeSha256Hex(bodyStream.ToArray());
    }

    private static bool TryCanonicalizeJson(
        Stream jsonStream,
        RecyclableMemoryStreamManager streamManager,
        out MemoryStream canonicalJson)
    {
        canonicalJson = streamManager.GetStream("idempotency-canonical-json");

        try
        {
            using var document = JsonDocument.Parse(jsonStream);
            using var writer = new Utf8JsonWriter(canonicalJson);
            WriteCanonicalJson(document.RootElement, writer);
            writer.Flush();
            canonicalJson.Position = 0;
            return true;
        }
        catch (JsonException)
        {
            canonicalJson.Dispose();
            canonicalJson = new MemoryStream(0);
            return false;
        }
    }

    private static void WriteCanonicalJson(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonicalJson(property.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonicalJson(item, writer);
                }
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                element.WriteTo(writer);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                writer.WriteNullValue();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static string ResolveRequestTarget(HttpContext context)
    {
        var endpointPattern = context.GetEndpoint() is RouteEndpoint endpoint
            ? endpoint.RoutePattern.RawText
            : null;
        string actualPath = context.Request.PathBase.Add(context.Request.Path).ToUriComponent().ToLowerInvariant();
        string routeValues = string.Join(
            '&',
            context.Request.RouteValues
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{Uri.EscapeDataString(pair.Key.ToLowerInvariant())}={Uri.EscapeDataString(Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture)?.ToLowerInvariant() ?? string.Empty)}"));
        string target = $"{endpointPattern?.ToLowerInvariant() ?? actualPath}|path:{actualPath}|route:{routeValues}";
        return context.Request.QueryString.HasValue ? $"{target}{context.Request.QueryString.Value}" : target;
    }

    private static string CapabilityScope(HttpRequest request)
    {
        var canonical = new StringBuilder();
        foreach (string headerName in CapabilityHeaders)
        {
            string[] values = request.Headers[headerName]
                .Select(value => value ?? string.Empty)
                .Order(StringComparer.Ordinal)
                .ToArray();
            canonical.Append(headerName).Append(':').Append(values.Length).Append(':');
            foreach (string value in values)
            {
                canonical.Append(value.Length).Append(':').Append(value).Append('|');
            }
        }

        return ComputeSha256Hex(canonical.ToString());
    }

    private static string? NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        return MediaTypeHeaderValue.TryParse(contentType, out var parsed)
            ? parsed.ToString().ToLowerInvariant()
            : contentType.Trim().ToLowerInvariant();
    }

    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)
            || !MediaTypeHeaderValue.TryParse(contentType, out var parsed))
        {
            return false;
        }

        var mediaType = parsed.MediaType.Value ?? string.Empty;
        return mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
               || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveUserId(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return context.User.FindFirst("sub")?.Value
               ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
               ?? context.User.FindFirst("sid")?.Value;
    }

    private static string ComputeSha256Hex(string value)
        => ComputeSha256Hex(Encoding.UTF8.GetBytes(value));

    private static string ComputeSha256Hex(ReadOnlySpan<byte> bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string ComputeSha256Hex(ReadOnlySequence<byte> sequence)
        => sequence.IsSingleSegment
            ? ComputeSha256Hex(sequence.FirstSpan)
            : ComputeSha256Hex(sequence.ToArray());
}
