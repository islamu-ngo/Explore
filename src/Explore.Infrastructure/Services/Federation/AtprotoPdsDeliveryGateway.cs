// ABOUTME: Restores a tenant/user-bound CarpaNet OAuth client and delivers one idempotent PDS record mutation.
// ABOUTME: Reconciles stable record keys, uses CID compare-and-swap, and returns only bounded failure codes.

using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarpaNet;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain.Federation;

namespace Explore.Infrastructure.Services.Federation;

public sealed class AtprotoPdsDeliveryGateway(
    IUserAuthenticationTokenRepository tokenRepository,
    AtprotoSessionEnvelopeProtector protector,
    AtprotoCoreClientFactory coreClientFactory) : IAtprotoPdsDeliveryGateway
{
    public async Task<AtprotoPdsDeliveryResult> DeliverAsync(
        AtprotoPdsDeliveryRequest command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var session = await tokenRepository.GetAtprotoSessionForReadAsync(
                command.TenantId,
                command.UserId,
                RepositoryBackedAtprotoSession.Provider,
                command.Did,
                cancellationToken).ConfigureAwait(false);
            if (session is null
                || string.IsNullOrWhiteSpace(session.PdsHost)
                || string.IsNullOrWhiteSpace(session.OAuthClientKeyId)
                || !Uri.TryCreate(session.PdsHost, UriKind.Absolute, out var storedPds)
                || !SamePds(storedPds, command.PdsHost))
            {
                return AtprotoPdsDeliveryResult.Failed("session_unavailable", retryable: false);
            }

            var context = new AtprotoOAuthSessionStoreContext(
                command.TenantId,
                command.UserId,
                command.Did,
                command.PdsHost,
                session.OAuthClientKeyId);
            var store = new RepositoryBackedOAuthSessionStore(tokenRepository, protector, context);
            using var lease = await coreClientFactory.CreateAsync(
                command.Did,
                session.OAuthClientKeyId,
                store,
                cancellationToken).ConfigureAwait(false);

            if (!string.Equals(lease.Client.AuthenticatedDid, command.Did, StringComparison.Ordinal)
                || !SamePds(lease.Client.BaseUrl, command.PdsHost))
            {
                return AtprotoPdsDeliveryResult.Failed("session_binding_mismatch", retryable: false);
            }

            return await AtprotoPdsRepositoryWriter.DeliverAsync(
                lease.Client,
                command,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RateLimitException)
        {
            return AtprotoPdsDeliveryResult.Failed("provider_rate_limited", retryable: true);
        }
        catch (AuthenticationException)
        {
            return AtprotoPdsDeliveryResult.Failed("reauth_required", retryable: false);
        }
        catch (ValidationException)
        {
            return AtprotoPdsDeliveryResult.Failed("provider_rejected", retryable: false);
        }
        catch (ATProtoException exception) when ((int)exception.StatusCode >= 500)
        {
            return AtprotoPdsDeliveryResult.Failed("provider_unavailable", retryable: true);
        }
        catch (ATProtoException)
        {
            return AtprotoPdsDeliveryResult.Failed("provider_rejected", retryable: false);
        }
        catch (HttpRequestException)
        {
            return AtprotoPdsDeliveryResult.Failed("provider_unavailable", retryable: true);
        }
        catch (OperationCanceledException)
        {
            return AtprotoPdsDeliveryResult.Failed("provider_timeout", retryable: true);
        }
        catch (TimeoutException)
        {
            return AtprotoPdsDeliveryResult.Failed("provider_timeout", retryable: true);
        }
        catch (AtprotoOAuthSessionUnavailableException)
        {
            return AtprotoPdsDeliveryResult.Failed("session_unavailable", retryable: false);
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or CryptographicException
            or JsonException
            or NotSupportedException)
        {
            return AtprotoPdsDeliveryResult.Failed("session_unavailable", retryable: false);
        }
    }

    private static bool SamePds(Uri left, Uri right)
    {
        var normalizedLeft = AtprotoOAuthSessionStoreContext.NormalizePdsUri(left.AbsoluteUri);
        var normalizedRight = AtprotoOAuthSessionStoreContext.NormalizePdsUri(right.AbsoluteUri);
        return normalizedLeft is not null
            && string.Equals(normalizedLeft, normalizedRight, StringComparison.Ordinal);
    }
}

internal static class AtprotoPdsRepositoryWriter
{
    internal const string GetRecordNsid = "com.atproto.repo.getRecord";
    internal const string PutRecordNsid = "com.atproto.repo.putRecord";
    internal const string DeleteRecordNsid = "com.atproto.repo.deleteRecord";

    internal static async Task<AtprotoPdsDeliveryResult> DeliverAsync(
        IATProtoClient client,
        AtprotoPdsDeliveryRequest command,
        CancellationToken cancellationToken)
    {
        bool isCidlessCompensation = command.ExpectedCid is null
            && command.Operation is PdsSyncOperation.Update or PdsSyncOperation.Delete;
        if (isCidlessCompensation
            && (!command.CompensationEvidenceComplete
                || command.CompensationBasePayloads is not { Count: > 0 }
                    && command.CompensationBaseCids is not { Count: > 0 }))
        {
            return AtprotoPdsDeliveryResult.Failed("record_conflict", retryable: false);
        }

        var existing = await GetExistingAsync(client, command, cancellationToken).ConfigureAwait(false);
        if (command.Operation == PdsSyncOperation.Delete)
        {
            return await DeleteAsync(client, command, existing, cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(command.Payload))
        {
            return AtprotoPdsDeliveryResult.Failed("payload_invalid", retryable: false);
        }

        JsonElement desired;
        try
        {
            desired = JsonSerializer.Deserialize<JsonElement>(command.Payload);
        }
        catch (JsonException)
        {
            return AtprotoPdsDeliveryResult.Failed("payload_invalid", retryable: false);
        }

        if (existing is not null && JsonElement.DeepEquals(existing.Value, desired))
        {
            return ValidResponse(existing.Uri, existing.Cid, existing.Cid);
        }

        if (command.Operation == PdsSyncOperation.Create && existing is not null)
        {
            return AtprotoPdsDeliveryResult.Failed("record_conflict", retryable: false);
        }

        if (command.Operation == PdsSyncOperation.Update
            && command.ExpectedCid is not null
            && existing is not null
            && !string.Equals(existing.Cid, command.ExpectedCid, StringComparison.Ordinal))
        {
            return AtprotoPdsDeliveryResult.Failed("record_conflict", retryable: false);
        }

        if (command.Operation == PdsSyncOperation.Update
            && existing is null
            && command.ExpectedCid is not null)
        {
            return AtprotoPdsDeliveryResult.Failed("remote_record_missing", retryable: false);
        }

        if (command.Operation == PdsSyncOperation.Update
            && command.ExpectedCid is null
            && existing is not null
            && !MatchesCompensationBase(existing, command))
        {
            return AtprotoPdsDeliveryResult.Failed("record_conflict", retryable: false);
        }

        var response = await client.PostAsync<AtprotoPutRecordInput, AtprotoPutRecordResponse>(
            PutRecordNsid,
            new AtprotoPutRecordInput
            {
                Repo = command.Did,
                Collection = command.Collection,
                RecordKey = command.RecordKey,
                Validate = true,
                Record = desired,
                SwapRecord = command.Operation == PdsSyncOperation.Update
                    ? command.ExpectedCid ?? existing?.Cid
                    : null
            },
            cancellationToken).ConfigureAwait(false);
        return ValidResponse(response.Uri, response.Cid, existing?.Cid);
    }

    private static async Task<AtprotoGetRecordResponse?> GetExistingAsync(
        IATProtoClient client,
        AtprotoPdsDeliveryRequest command,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetAsync<AtprotoGetRecordResponse>(
                GetRecordNsid,
                [
                    new("repo", command.Did),
                    new("collection", command.Collection),
                    new("rkey", command.RecordKey)
                ],
                cancellationToken).ConfigureAwait(false);
        }
        catch (ATProtoException exception) when (
            exception.StatusCode == HttpStatusCode.NotFound
            || string.Equals(exception.ErrorCode, "RecordNotFound", StringComparison.Ordinal))
        {
            return null;
        }
    }

    private static async Task<AtprotoPdsDeliveryResult> DeleteAsync(
        IATProtoClient client,
        AtprotoPdsDeliveryRequest command,
        AtprotoGetRecordResponse? existing,
        CancellationToken cancellationToken)
    {
        if (existing is null)
        {
            return command.ExpectedCid is { Length: > 0 } expectedCid
                ? AtprotoPdsDeliveryResult.Success(BuildAtUri(command), expectedCid)
                : AtprotoPdsDeliveryResult.SuccessAbsent(BuildAtUri(command));
        }

        if (command.ExpectedCid is not null
            && !string.Equals(existing.Cid, command.ExpectedCid, StringComparison.Ordinal))
        {
            return AtprotoPdsDeliveryResult.Failed("record_conflict", retryable: false);
        }

        if (command.ExpectedCid is null
            && !MatchesCompensationBase(existing, command))
        {
            return AtprotoPdsDeliveryResult.Failed("record_conflict", retryable: false);
        }

        var swapRecord = command.ExpectedCid ?? existing.Cid;
        await client.PostAsync<AtprotoDeleteRecordInput, AtprotoDeleteRecordResponse>(
            DeleteRecordNsid,
            new AtprotoDeleteRecordInput
            {
                Repo = command.Did,
                Collection = command.Collection,
                RecordKey = command.RecordKey,
                SwapRecord = swapRecord
            },
            cancellationToken).ConfigureAwait(false);
        return ValidResponse(existing.Uri, existing.Cid, existing.Cid);
    }

    private static AtprotoPdsDeliveryResult ValidResponse(
        string? uri,
        string? cid,
        string? observedBaseCid = null) =>
        string.IsNullOrWhiteSpace(uri) || string.IsNullOrWhiteSpace(cid)
            ? AtprotoPdsDeliveryResult.Failed("provider_response_invalid", retryable: true)
            : AtprotoPdsDeliveryResult.Success(uri, cid, observedBaseCid);

    private static string BuildAtUri(AtprotoPdsDeliveryRequest command) =>
        $"at://{command.Did}/{command.Collection}/{command.RecordKey}";

    private static bool MatchesCompensationBase(
        AtprotoGetRecordResponse existing,
        AtprotoPdsDeliveryRequest command)
    {
        if (command.CompensationBaseCids?.Contains(existing.Cid, StringComparer.Ordinal) == true)
        {
            return true;
        }

        if (command.CompensationBasePayloads is null)
        {
            return false;
        }

        foreach (string payload in command.CompensationBasePayloads)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(payload);
                if (JsonElement.DeepEquals(existing.Value, document.RootElement))
                {
                    return true;
                }
            }
            catch (JsonException)
            {
                // Stored invalid evidence is ignored so compensation fails closed.
            }
        }

        return false;
    }

}

internal sealed record AtprotoGetRecordResponse(
    string Uri,
    string Cid,
    JsonElement Value);

internal sealed class AtprotoPutRecordInput
{
    public required string Repo { get; init; }
    public required string Collection { get; init; }

    [JsonPropertyName("rkey")]
    public required string RecordKey { get; init; }

    public bool Validate { get; init; }
    public required JsonElement Record { get; init; }
    public string? SwapRecord { get; init; }
}

internal sealed record AtprotoPutRecordResponse(string Uri, string Cid);

internal sealed class AtprotoDeleteRecordInput
{
    public required string Repo { get; init; }
    public required string Collection { get; init; }

    [JsonPropertyName("rkey")]
    public required string RecordKey { get; init; }

    public string? SwapRecord { get; init; }
}

internal sealed class AtprotoDeleteRecordResponse;
