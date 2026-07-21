// ABOUTME: Fetches complete bounded ATProto repository snapshots over hardened public egress.
// ABOUTME: Verifies DID/PDS binding and CAR integrity before reusing canonical record materialization.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CarpaNet;
using CarpaNet.Cbor;
using CarpaNet.Cbor.Converters;
using CarpaNet.Identity;
using CarpaNet.Jetstream;
using CarpaNet.Repo;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Atproto.Transport;
using Explore.Domain;

namespace Explore.Infrastructure.Services.Federation;

public sealed class AtprotoPdsSnapshotGateway : IAtprotoPdsSnapshotGateway
{
    internal const int MaximumCarBytes = 64 * 1024 * 1024;
    internal const int MaximumBlocks = 50_000;
    internal const int MaximumTargetRecords = 10_000;
    internal static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);
    private const int MaximumIdentityResponseBytes = 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly Func<AtprotoOutboundPolicy, HttpMessageHandler> _primaryHandlerFactory;
    private readonly MemoryIdentityCache _identityCache = new();

    public AtprotoPdsSnapshotGateway()
        : this(policy => AtprotoHardenedHttpClient.CreatePrimaryHandler(policy, TimeSpan.FromSeconds(5)))
    {
    }

    internal AtprotoPdsSnapshotGateway(
        Func<AtprotoOutboundPolicy, HttpMessageHandler> primaryHandlerFactory)
    {
        _primaryHandlerFactory = primaryHandlerFactory;
    }

    public async Task<AtprotoPdsSnapshotFetchResult> FetchAsync(
        string did,
        long snapshotVersion,
        CancellationToken cancellationToken)
    {
        if (!IsSupportedDid(did) || !TryFromUnixMicroseconds(snapshotVersion, out DateTime observedAt))
        {
            return AtprotoPdsSnapshotFetchResult.Failed("snapshot_request_invalid");
        }

        var policy = new AtprotoOutboundPolicy(allowsDevelopmentLoopback: false);
        Uri pdsOrigin;
        try
        {
            pdsOrigin = await ResolvePdsOriginAsync(did, policy, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SnapshotValidationException exception)
        {
            return AtprotoPdsSnapshotFetchResult.Failed(exception.FailureCode);
        }
        catch
        {
            return AtprotoPdsSnapshotFetchResult.Failed("identity_resolution_failed");
        }

        byte[] car;
        try
        {
            using var client = CreateClient(policy, MaximumCarBytes);
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildGetRepoUri(pdsOrigin, did));
            using HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return AtprotoPdsSnapshotFetchResult.Failed("repository_fetch_failed");
            }

            car = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            if (car.Length == 0)
            {
                return AtprotoPdsSnapshotFetchResult.Failed("repository_invalid");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AtprotoOAuthSecurityException exception)
            when (exception.FailureCode == "response_too_large")
        {
            return AtprotoPdsSnapshotFetchResult.Failed("repository_too_large");
        }
        catch
        {
            return AtprotoPdsSnapshotFetchResult.Failed("repository_fetch_failed");
        }

        try
        {
            return ParseSnapshot(did, snapshotVersion, observedAt, car);
        }
        catch (SnapshotValidationException exception)
        {
            return AtprotoPdsSnapshotFetchResult.Failed(exception.FailureCode);
        }
        catch (Exception exception) when (exception is InvalidDataException
            or InvalidOperationException
            or ArgumentException
            or OverflowException
            or DecoderFallbackException
            or JsonException)
        {
            return AtprotoPdsSnapshotFetchResult.Failed("repository_invalid");
        }
    }

    private async Task<Uri> ResolvePdsOriginAsync(
        string did,
        AtprotoOutboundPolicy policy,
        CancellationToken cancellationToken)
    {
        using var client = CreateClient(policy, MaximumIdentityResponseBytes);
        using var resolver = IdentityResolver.CreateWithCache(_identityCache, client);
        DidDocument document = await resolver.ResolveDidAsync(did, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(document.Id, did, StringComparison.Ordinal))
        {
            throw new SnapshotValidationException("identity_binding_mismatch");
        }

        DidService[] services = document.Service
            .Where(service => string.Equals(service.Type, "AtprotoPersonalDataServer", StringComparison.Ordinal)
                && (string.Equals(service.Id, "#atproto_pds", StringComparison.Ordinal)
                    || string.Equals(service.Id, $"{did}#atproto_pds", StringComparison.Ordinal)))
            .ToArray();
        if (services.Length != 1
            || !Uri.TryCreate(services[0].ServiceEndpoint, UriKind.Absolute, out Uri? endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment)
            || endpoint.AbsolutePath is not ("" or "/"))
        {
            throw new SnapshotValidationException("pds_endpoint_invalid");
        }

        try
        {
            policy.ValidateUri(endpoint);
        }
        catch (AtprotoOAuthSecurityException)
        {
            throw new SnapshotValidationException("pds_endpoint_invalid");
        }

        return new Uri(endpoint.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute);
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The returned HttpClient owns and disposes the complete handler chain.")]
    private HttpClient CreateClient(AtprotoOutboundPolicy policy, int maximumResponseBytes) => new(
        new AtprotoBoundedResponseHandler(maximumResponseBytes, _primaryHandlerFactory(policy)),
        disposeHandler: true)
    {
        Timeout = RequestTimeout
    };

    private static Uri BuildGetRepoUri(Uri pdsOrigin, string did) => new(
        pdsOrigin,
        $"xrpc/com.atproto.sync.getRepo?did={Uri.EscapeDataString(did)}");

    private static AtprotoPdsSnapshotFetchResult ParseSnapshot(
        string did,
        long snapshotVersion,
        DateTime observedAt,
        byte[] car)
    {
        ValidateCarFraming(car);
        ValidateCarBlocks(car);
        Repository repository = Repository.Load(car);
        if (repository.Roots.Count != 1
            || !repository.RootCid.IsAtProtoBlessedFormat
            || repository.GetBlock(repository.RootCid) is null
            || !string.Equals(repository.Did, did, StringComparison.Ordinal))
        {
            throw new SnapshotValidationException("repository_identity_mismatch");
        }

        if (repository.Version != 3
            || !repository.Commit.Data.IsAtProtoBlessedFormat
            || string.IsNullOrWhiteSpace(repository.Rev)
            || repository.Rev.Length > 32
            || repository.Commit.Sig.Length == 0)
        {
            throw new SnapshotValidationException("repository_structure_invalid");
        }

        var present = new List<AtprotoPdsSnapshotIdentity>();
        var items = new List<AtprotoPdsSnapshotItem>();
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var visitedNodes = new HashSet<string>(StringComparer.Ordinal);
        var nodes = new Queue<ATCid>();
        nodes.Enqueue(repository.Commit.Data);
        while (nodes.TryDequeue(out ATCid nodeCid))
        {
            if (!visitedNodes.Add(nodeCid.Value) || visitedNodes.Count > MaximumBlocks)
            {
                throw new SnapshotValidationException("repository_structure_invalid");
            }

            byte[] nodeData = repository.GetBlock(nodeCid)
                ?? throw new SnapshotValidationException("repository_incomplete");
            MstNode node = ReadMstNode(nodeData);
            Enqueue(node.Left, nodes);
            foreach (MstEntry entry in node.Entries)
            {
                Enqueue(entry.Tree, nodes);
                if (!entry.Value.IsAtProtoBlessedFormat)
                {
                    throw new SnapshotValidationException("repository_integrity_invalid");
                }

                byte[] recordData = repository.GetBlock(entry.Value)
                    ?? throw new SnapshotValidationException("repository_incomplete");
                string path = StrictUtf8.GetString(entry.KeyBytes);
                if (!paths.Add(path) || !TrySplitPath(path, out string collection, out string recordKey))
                {
                    throw new SnapshotValidationException("repository_structure_invalid");
                }

                if (!AtprotoJetstreamConstants.Collections.Contains(collection, StringComparer.Ordinal))
                {
                    continue;
                }

                if (present.Count >= MaximumTargetRecords)
                {
                    throw new SnapshotValidationException("repository_target_limit_exceeded");
                }

                present.Add(new(collection, recordKey));
                AtprotoPdsSnapshotItem? item = TryMaterialize(
                    did,
                    collection,
                    recordKey,
                    entry.Value,
                    recordData,
                    snapshotVersion,
                    observedAt);
                if (item is not null)
                {
                    items.Add(item);
                }
            }
        }

        AtprotoPdsSnapshotIdentity[] orderedPresent = present
            .OrderBy(identity => identity.Collection, StringComparer.Ordinal)
            .ThenBy(identity => identity.RecordKey, StringComparer.Ordinal)
            .ToArray();
        AtprotoPdsSnapshotItem[] orderedItems = items
            .OrderBy(item => item.Record.Collection, StringComparer.Ordinal)
            .ThenBy(item => item.Record.RecordKey, StringComparer.Ordinal)
            .ToArray();
        return AtprotoPdsSnapshotFetchResult.Complete(new(did, orderedPresent, orderedItems));
    }

    private static void ValidateCarFraming(ReadOnlySpan<byte> car)
    {
        if (car.Length is 0 or > MaximumCarBytes)
        {
            throw new SnapshotValidationException("repository_too_large");
        }

        int offset = 0;
        int headerLength = ReadSectionLength(car, ref offset);
        offset += headerLength;
        while (offset < car.Length)
        {
            int blockLength = ReadSectionLength(car, ref offset);
            offset += blockLength;
        }
    }

    private static int ReadSectionLength(ReadOnlySpan<byte> car, ref int offset)
    {
        ulong value = 0;
        int shift = 0;
        while (shift <= 63)
        {
            if (offset >= car.Length)
            {
                throw new SnapshotValidationException("repository_framing_invalid");
            }

            byte current = car[offset++];
            if (shift == 63 && (current & 0xfe) != 0)
            {
                throw new SnapshotValidationException("repository_framing_invalid");
            }

            value |= (ulong)(current & 0x7f) << shift;
            if ((current & 0x80) == 0)
            {
                if (value is 0 or > int.MaxValue || value > (ulong)(car.Length - offset))
                {
                    throw new SnapshotValidationException("repository_framing_invalid");
                }

                return (int)value;
            }

            shift += 7;
        }

        throw new SnapshotValidationException("repository_framing_invalid");
    }

    private static void ValidateCarBlocks(byte[] car)
    {
        using var reader = new CarReader(car);
        if (reader.Header.Roots.Count != 1 || !reader.Header.Roots[0].IsAtProtoBlessedFormat)
        {
            throw new SnapshotValidationException("repository_structure_invalid");
        }

        var blocks = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        int count = 0;
        foreach (CarBlock block in reader.ReadBlocks())
        {
            if (++count > MaximumBlocks)
            {
                throw new SnapshotValidationException("repository_block_limit_exceeded");
            }

            if (!VerifyBlock(block.Cid, block.Data))
            {
                throw new SnapshotValidationException("repository_integrity_invalid");
            }

            if (blocks.TryGetValue(block.Cid.Value, out byte[]? existing))
            {
                if (!existing.AsSpan().SequenceEqual(block.Data))
                {
                    throw new SnapshotValidationException("repository_integrity_invalid");
                }
            }
            else
            {
                blocks.Add(block.Cid.Value, block.Data);
            }
        }

        if (!blocks.ContainsKey(reader.Header.Roots[0].Value))
        {
            throw new SnapshotValidationException("repository_incomplete");
        }
    }

    private static bool VerifyBlock(ATCid cid, byte[] data) => cid.IsAtProtoBlessedFormat
        && cid.Hash is { Length: ATCid.Sha256HashLength } hash
        && CryptographicOperations.FixedTimeEquals(SHA256.HashData(data), hash);

    private static MstNode ReadMstNode(byte[] data)
    {
        var reader = new DagCborReader(data);
        MstNode node = MstNode.FromCbor(ref reader);
        if (reader.BytesRemaining != 0)
        {
            throw new SnapshotValidationException("repository_structure_invalid");
        }

        return node;
    }

    private static void Enqueue(ATCid? cid, Queue<ATCid> nodes)
    {
        if (cid is not { } value)
        {
            return;
        }

        if (!value.IsAtProtoBlessedFormat)
        {
            throw new SnapshotValidationException("repository_integrity_invalid");
        }

        nodes.Enqueue(value);
    }

    private static AtprotoPdsSnapshotItem? TryMaterialize(
        string did,
        string collection,
        string recordKey,
        ATCid cid,
        byte[] recordData,
        long snapshotVersion,
        DateTime observedAt)
    {
        JsonElement record;
        try
        {
            var reader = new DagCborReader(recordData);
            record = new JsonElementCborConverter().ReadTyped(ref reader);
            if (reader.BytesRemaining != 0 || record.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException
            or InvalidDataException
            or ArgumentException
            or JsonException)
        {
            return null;
        }

        var envelope = new JetstreamEvent
        {
            Did = did,
            TimeUs = snapshotVersion,
            Kind = "commit",
            Commit = new JetstreamCommit
            {
                Operation = "update",
                Collection = collection,
                Rkey = recordKey,
                Cid = cid.Value,
                Record = record
            }
        };
        AtprotoJetstreamParsedEnvelope parsed = AtprotoJetstreamEnvelopeParser.Parse(
            envelope,
            currentCursor: 0,
            [did],
            observedAt);
        if (parsed.Record is null)
        {
            return null;
        }

        parsed.Record.SourceCursor = null;
        return new(parsed.Record, parsed.EventProjection);
    }

    private static bool TrySplitPath(string path, out string collection, out string recordKey)
    {
        int separator = path.IndexOf('/');
        if (path.Length > 600
            || separator is <= 0
            || separator == path.Length - 1
            || path.IndexOf('/', separator + 1) >= 0)
        {
            collection = string.Empty;
            recordKey = string.Empty;
            return false;
        }

        collection = path[..separator];
        recordKey = path[(separator + 1)..];
        return collection.Length <= 320 && recordKey.Length <= 255;
    }

    private static bool IsSupportedDid(string did)
    {
        try
        {
            return did.Length <= 255
                && ATDid.IsValid(did)
                && (did.StartsWith("did:plc:", StringComparison.Ordinal)
                    || did.StartsWith("did:web:", StringComparison.Ordinal));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool TryFromUnixMicroseconds(long value, out DateTime result)
    {
        try
        {
            result = DateTime.UnixEpoch.AddTicks(checked(value * 10));
            return value > 0;
        }
        catch (Exception exception) when (exception is ArgumentOutOfRangeException or OverflowException)
        {
            result = default;
            return false;
        }
    }

    private sealed class SnapshotValidationException(string failureCode) : Exception
    {
        public string FailureCode { get; } = failureCode;
    }
}
