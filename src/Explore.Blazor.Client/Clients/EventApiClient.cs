// ABOUTME: Partial class extending NSwag-generated EventApiClient with request preparation hooks.
// ABOUTME: Tenant context is resolved server-side from the forwarded host header.

using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Explore.Blazor.Client.Clients;

public sealed class GuestRegistrationOrderStartResult
{
    internal GuestRegistrationOrderStartResult(GuestRegistrationOrderStartDto response, string capability)
    {
        Response = response;
        Capability = capability;
    }

    public GuestRegistrationOrderStartDto Response { get; }
    public bool HasCapability => !string.IsNullOrWhiteSpace(Capability);
    internal string Capability { get; }
}

public partial interface IEventApiClient
{
    Task<GuestRegistrationOrderStartResult> StartGuestRegistrationOrderWithCapabilityAsync(
        Guid eventId,
        StartRegistrationOrderRequest body,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Partial class extending NSwag-generated EventApiClient.
/// Tenant context is resolved server-side from forwarded host or explicit X-Tenant-Id when provided.
/// </summary>
public partial class EventApiClient
{
    private static readonly AsyncLocal<string?> CreateEventIdempotencyKey = new();
    private static readonly AsyncLocal<string?> GuestRegistrationOrderIdempotencyKey = new();
    private static readonly AsyncLocal<GuestRegistrationOrderCapabilityCapture?> guestRegistrationOrderCapabilityCapture = new();

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

    public async Task<GuestRegistrationOrderStartResult> StartGuestRegistrationOrderWithCapabilityAsync(
        Guid eventId,
        StartRegistrationOrderRequest body,
        CancellationToken cancellationToken = default)
    {
        var previousCapture = guestRegistrationOrderCapabilityCapture.Value;
        var previousKey = GuestRegistrationOrderIdempotencyKey.Value;
        var capture = new GuestRegistrationOrderCapabilityCapture();
        guestRegistrationOrderCapabilityCapture.Value = capture;
        GuestRegistrationOrderIdempotencyKey.Value = Guid.CreateVersion7().ToString("N");

        try
        {
            var response = await StartGuestRegistrationOrderAsync(
                eventId,
                GuestRegistrationOrderIdempotencyKey.Value!,
                body: body,
                cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(capture.Value))
            {
                throw new InvalidOperationException("Guest registration capability was not returned.");
            }

            return new GuestRegistrationOrderStartResult(response, capture.Value);
        }
        finally
        {
            guestRegistrationOrderCapabilityCapture.Value = previousCapture;
            GuestRegistrationOrderIdempotencyKey.Value = previousKey;
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

        if (request.Method != HttpMethod.Get
            && IsGuestRegistrationOrderRequest(url)
            && !request.Headers.Contains("Idempotency-Key"))
        {
            request.Headers.TryAddWithoutValidation(
                "Idempotency-Key",
                GuestRegistrationOrderIdempotencyKey.Value ?? Guid.CreateVersion7().ToString("N"));
        }

        if (request.Method == HttpMethod.Post && IsRegistrationProviderAttemptRequest(url))
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.CreateVersion7().ToString("N"));
        }
    }

    partial void ProcessResponse(HttpClient client, HttpResponseMessage response)
    {
        var capture = guestRegistrationOrderCapabilityCapture.Value;
        if (capture is null
            || response.RequestMessage?.Method != HttpMethod.Post
            || response.RequestMessage.RequestUri?.AbsolutePath.EndsWith("/guest", StringComparison.OrdinalIgnoreCase) != true
            || !response.Headers.TryGetValues("X-Registration-Order-Capability", out var values))
        {
            return;
        }

        capture.Value = values.FirstOrDefault();
    }

    private static bool IsCreateEventRequest(string url)
    {
        var path = url.Split('?', 2)[0].TrimStart('/');
        return string.Equals(path, "api/event", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGuestRegistrationOrderRequest(string url) =>
        url.Split('?', 2)[0].Contains("/registration-orders/guest", StringComparison.OrdinalIgnoreCase);

    private static bool IsRegistrationProviderAttemptRequest(string url) =>
        url.Split('?', 2)[0].EndsWith("/provider-attempts", StringComparison.OrdinalIgnoreCase);

    private sealed class GuestRegistrationOrderCapabilityCapture
    {
        public string? Value { get; set; }
    }
}
