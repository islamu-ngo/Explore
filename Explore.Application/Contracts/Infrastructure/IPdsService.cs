// ABOUTME: Contract for PDS (Personal Data Server) communication service.
// ABOUTME: Handles hosting records on Islamu PDS and proxying to external PDS providers.

using Explore.Domain.Federation;

namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Service for communicating with AT Protocol PDS (Personal Data Server).
/// Supports both Islamu-hosted PDS and proxying to external PDS providers.
/// </summary>
public interface IPdsService
{
    /// <summary>
    /// Creates a new record in the PDS repository.
    /// </summary>
    /// <param name="did">The DID of the repository owner.</param>
    /// <param name="collection">The AT Protocol collection NSID (e.g., "app.islamu.event").</param>
    /// <param name="recordKey">The record key (TID format).</param>
    /// <param name="payload">JSON-serialized record content.</param>
    /// <param name="pdsHost">Target PDS host, or null for Islamu-hosted PDS.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the AT URI and CID on success.</returns>
    Task<PdsOperationResult> CreateRecordAsync(
        string did,
        string collection,
        string recordKey,
        string payload,
        string? pdsHost = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing record in the PDS repository.
    /// </summary>
    /// <param name="did">The DID of the repository owner.</param>
    /// <param name="collection">The AT Protocol collection NSID.</param>
    /// <param name="recordKey">The record key to update.</param>
    /// <param name="payload">JSON-serialized updated record content.</param>
    /// <param name="pdsHost">Target PDS host, or null for Islamu-hosted PDS.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the new CID on success.</returns>
    Task<PdsOperationResult> UpdateRecordAsync(
        string did,
        string collection,
        string recordKey,
        string payload,
        string? pdsHost = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a record from the PDS repository.
    /// </summary>
    /// <param name="did">The DID of the repository owner.</param>
    /// <param name="collection">The AT Protocol collection NSID.</param>
    /// <param name="recordKey">The record key to delete.</param>
    /// <param name="pdsHost">Target PDS host, or null for Islamu-hosted PDS.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<PdsOperationResult> DeleteRecordAsync(
        string did,
        string collection,
        string recordKey,
        string? pdsHost = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves which PDS host should be used for an actor.
    /// </summary>
    /// <param name="did">The actor's DID.</param>
    /// <param name="actorPdsHost">The actor's configured PDS host (from Actor.PdsHost).</param>
    /// <returns>The PDS host URL to use, or null for Islamu-hosted PDS.</returns>
    string? ResolvePdsHost(string did, string? actorPdsHost);

    /// <summary>
    /// Processes a single outbox entry, performing the PDS operation.
    /// </summary>
    /// <param name="outboxEntry">The outbox entry to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    Task<PdsOperationResult> ProcessOutboxEntryAsync(
        PdsSyncOutbox outboxEntry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the PDS service is available and configured.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the service is ready to process operations.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a PDS operation.
/// </summary>
public class PdsOperationResult
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The AT URI of the created/updated record (e.g., "at://did:plc:xxx/collection/rkey").
    /// </summary>
    public string? Uri { get; init; }

    /// <summary>
    /// The CID (Content Identifier) of the record.
    /// </summary>
    public string? Cid { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Whether this error is retryable (e.g., network timeout vs validation error).
    /// </summary>
    public bool IsRetryable { get; init; }

    /// <summary>
    /// HTTP status code if applicable.
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static PdsOperationResult Succeeded(string? uri = null, string? cid = null)
        => new() { Success = true, Uri = uri, Cid = cid };

    /// <summary>
    /// Creates a failed result with retryable error.
    /// </summary>
    public static PdsOperationResult RetryableError(string error, int? statusCode = null)
        => new() { Success = false, Error = error, IsRetryable = true, StatusCode = statusCode };

    /// <summary>
    /// Creates a failed result with permanent error.
    /// </summary>
    public static PdsOperationResult PermanentError(string error, int? statusCode = null)
        => new() { Success = false, Error = error, IsRetryable = false, StatusCode = statusCode };
}
