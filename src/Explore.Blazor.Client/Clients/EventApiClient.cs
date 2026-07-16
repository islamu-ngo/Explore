// ABOUTME: Partial class extending NSwag-generated EventApiClient with request preparation hooks.
// ABOUTME: Tenant context is resolved server-side from the forwarded host header.

using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Explore.Blazor.Client.Clients;

/// <summary>
/// Partial class extending NSwag-generated EventApiClient.
/// Tenant context is resolved server-side from forwarded host or explicit X-Tenant-Id when provided.
/// </summary>
public partial class EventApiClient
{
    private static readonly AsyncLocal<string?> CreateEventIdempotencyKey = new();

    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.Converters.Add(new JsonStringEnumConverter());
    }

    public async Task<BaseCommandResponseOfGuid> CreateEventWithIdempotencyKeyAsync(
        CreateEventDraftRequestDto body,
        string idempotencyKey,
        string? apiVersion = null,
        string? xApiVersion = null,
        CancellationToken cancellationToken = default)
    {
        var previousKey = CreateEventIdempotencyKey.Value;
        CreateEventIdempotencyKey.Value = idempotencyKey;

        try
        {
            return await CreateEventAsync(body, apiVersion, xApiVersion, cancellationToken);
        }
        finally
        {
            CreateEventIdempotencyKey.Value = previousKey;
        }
    }

    /// <summary>
    /// Called before each request.
    /// </summary>
    partial void PrepareRequest(System.Net.Http.HttpClient client, System.Net.Http.HttpRequestMessage request, string url)
    {
        if (request.Method == HttpMethod.Post
            && IsCreateEventRequest(url)
            && !string.IsNullOrWhiteSpace(CreateEventIdempotencyKey.Value))
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", CreateEventIdempotencyKey.Value);
        }
    }

    private static bool IsCreateEventRequest(string url)
    {
        var path = url.Split('?', 2)[0].TrimStart('/');
        return string.Equals(path, "api/event", StringComparison.OrdinalIgnoreCase);
    }
}
