// ABOUTME: Implementation of PDS (Personal Data Server) communication service for AT Protocol.
// ABOUTME: Handles record creation, updates, and deletes with support for both hosted and external PDS.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Explore.Application.Contracts.Infrastructure;
using Explore.Domain.Federation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Services.Federation;

/// <summary>
/// Service for communicating with AT Protocol PDS (Personal Data Server).
/// </summary>
public class PdsService : IPdsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PdsSyncSettings _settings;
    private readonly ILogger<PdsService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public PdsService(
        IHttpClientFactory httpClientFactory,
        IOptions<PdsSyncSettings> settings,
        ILogger<PdsService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates a configured HttpClient for PDS operations.
    /// </summary>
    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient("PdsService");
        client.Timeout = TimeSpan.FromSeconds(_settings.ApiTimeoutSeconds);
        return client;
    }

    public async Task<PdsOperationResult> CreateRecordAsync(
        string did,
        string collection,
        string recordKey,
        string payload,
        string? pdsHost = null,
        CancellationToken cancellationToken = default)
    {
        var host = pdsHost ?? _settings.IslamuPdsHost;
        if (string.IsNullOrEmpty(host))
        {
            return PdsOperationResult.PermanentError("PDS host not configured");
        }

        var endpoint = $"{host.TrimEnd('/')}/xrpc/com.atproto.repo.putRecord";

        try
        {
            var request = new PutRecordRequest
            {
                Repo = did,
                Collection = collection,
                Rkey = recordKey,
                Record = JsonSerializer.Deserialize<JsonElement>(payload)
            };

            if (_settings.VerboseLogging)
            {
                _logger.LogDebug("Creating record: {Did}/{Collection}/{RecordKey} on {Host}",
                    did, collection, recordKey, host);
            }

            using var httpClient = CreateHttpClient();
            var response = await httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

            return await HandleResponseAsync(response, cancellationToken);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("PDS create record timed out: {Did}/{Collection}/{RecordKey}",
                did, collection, recordKey);
            return PdsOperationResult.RetryableError("Request timed out", 408);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "PDS create record failed: {Did}/{Collection}/{RecordKey}",
                did, collection, recordKey);
            return PdsOperationResult.RetryableError($"HTTP error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating record: {Did}/{Collection}/{RecordKey}",
                did, collection, recordKey);
            return PdsOperationResult.PermanentError($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<PdsOperationResult> UpdateRecordAsync(
        string did,
        string collection,
        string recordKey,
        string payload,
        string? pdsHost = null,
        CancellationToken cancellationToken = default)
    {
        // putRecord handles both create and update in AT Protocol
        return await CreateRecordAsync(did, collection, recordKey, payload, pdsHost, cancellationToken);
    }

    public async Task<PdsOperationResult> DeleteRecordAsync(
        string did,
        string collection,
        string recordKey,
        string? pdsHost = null,
        CancellationToken cancellationToken = default)
    {
        var host = pdsHost ?? _settings.IslamuPdsHost;
        if (string.IsNullOrEmpty(host))
        {
            return PdsOperationResult.PermanentError("PDS host not configured");
        }

        var endpoint = $"{host.TrimEnd('/')}/xrpc/com.atproto.repo.deleteRecord";

        try
        {
            var request = new DeleteRecordRequest
            {
                Repo = did,
                Collection = collection,
                Rkey = recordKey
            };

            if (_settings.VerboseLogging)
            {
                _logger.LogDebug("Deleting record: {Did}/{Collection}/{RecordKey} on {Host}",
                    did, collection, recordKey, host);
            }

            using var httpClient = CreateHttpClient();
            var response = await httpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);

            // Delete returns 200 with empty body on success
            if (response.IsSuccessStatusCode)
            {
                return PdsOperationResult.Succeeded();
            }

            return await HandleErrorResponseAsync(response, cancellationToken);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("PDS delete record timed out: {Did}/{Collection}/{RecordKey}",
                did, collection, recordKey);
            return PdsOperationResult.RetryableError("Request timed out", 408);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "PDS delete record failed: {Did}/{Collection}/{RecordKey}",
                did, collection, recordKey);
            return PdsOperationResult.RetryableError($"HTTP error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting record: {Did}/{Collection}/{RecordKey}",
                did, collection, recordKey);
            return PdsOperationResult.PermanentError($"Unexpected error: {ex.Message}");
        }
    }

    public string? ResolvePdsHost(string did, string? actorPdsHost)
    {
        // If actor has a configured PDS host and it's not Bluesky's default, use it
        if (!string.IsNullOrEmpty(actorPdsHost) &&
            !actorPdsHost.Contains("bsky.social", StringComparison.OrdinalIgnoreCase))
        {
            return actorPdsHost;
        }

        // Otherwise use Islamu-hosted PDS (null indicates default)
        return null;
    }

    public async Task<PdsOperationResult> ProcessOutboxEntryAsync(
        PdsSyncOutbox outboxEntry,
        CancellationToken cancellationToken = default)
    {
        if (outboxEntry == null)
        {
            return PdsOperationResult.PermanentError("Outbox entry is null");
        }

        return outboxEntry.Operation switch
        {
            PdsSyncOperation.Create => await CreateRecordAsync(
                outboxEntry.Did,
                outboxEntry.Collection,
                outboxEntry.RecordKey,
                outboxEntry.Payload ?? "{}",
                outboxEntry.PdsHost,
                cancellationToken),

            PdsSyncOperation.Update => await UpdateRecordAsync(
                outboxEntry.Did,
                outboxEntry.Collection,
                outboxEntry.RecordKey,
                outboxEntry.Payload ?? "{}",
                outboxEntry.PdsHost,
                cancellationToken),

            PdsSyncOperation.Delete => await DeleteRecordAsync(
                outboxEntry.Did,
                outboxEntry.Collection,
                outboxEntry.RecordKey,
                outboxEntry.PdsHost,
                cancellationToken),

            _ => PdsOperationResult.PermanentError($"Unknown operation: {outboxEntry.Operation}")
        };
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        // Service is available if enabled and PDS host is configured
        var isAvailable = _settings.Enabled && !string.IsNullOrEmpty(_settings.IslamuPdsHost);
        return Task.FromResult(isAvailable);
    }

    private async Task<PdsOperationResult> HandleResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            try
            {
                var result = await response.Content.ReadFromJsonAsync<PutRecordResponse>(
                    JsonOptions, cancellationToken);

                return PdsOperationResult.Succeeded(result?.Uri, result?.Cid);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse PDS success response");
                return PdsOperationResult.Succeeded(); // Still successful, just couldn't parse response
            }
        }

        return await HandleErrorResponseAsync(response, cancellationToken);
    }

    private async Task<PdsOperationResult> HandleErrorResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var statusCode = (int)response.StatusCode;
        string errorMessage;

        try
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var errorResponse = JsonSerializer.Deserialize<AtProtoError>(errorBody, JsonOptions);
            errorMessage = errorResponse?.Message ?? errorResponse?.Error ?? errorBody;
        }
        catch
        {
            errorMessage = $"HTTP {statusCode}: {response.ReasonPhrase}";
        }

        // Determine if error is retryable based on status code
        var isRetryable = response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => true,     // 429 - Rate limited
            HttpStatusCode.ServiceUnavailable => true,  // 503 - Temporary unavailable
            HttpStatusCode.GatewayTimeout => true,      // 504 - Gateway timeout
            HttpStatusCode.BadGateway => true,          // 502 - Bad gateway
            HttpStatusCode.RequestTimeout => true,      // 408 - Request timeout
            HttpStatusCode.InternalServerError => true, // 500 - Server error (might be temporary)
            _ => false // All other errors are permanent (400, 401, 403, 404, etc.)
        };

        _logger.LogWarning("PDS error response: {StatusCode} - {Message}", statusCode, errorMessage);

        return isRetryable
            ? PdsOperationResult.RetryableError(errorMessage, statusCode)
            : PdsOperationResult.PermanentError(errorMessage, statusCode);
    }

    #region AT Protocol Request/Response Models

    private class PutRecordRequest
    {
        public string Repo { get; init; } = string.Empty;
        public string Collection { get; init; } = string.Empty;
        public string Rkey { get; init; } = string.Empty;
        public JsonElement Record { get; init; }
    }

    private class PutRecordResponse
    {
        public string? Uri { get; init; }
        public string? Cid { get; init; }
    }

    private class DeleteRecordRequest
    {
        public string Repo { get; init; } = string.Empty;
        public string Collection { get; init; } = string.Empty;
        public string Rkey { get; init; } = string.Empty;
    }

    private class AtProtoError
    {
        public string? Error { get; init; }
        public string? Message { get; init; }
    }

    #endregion
}
