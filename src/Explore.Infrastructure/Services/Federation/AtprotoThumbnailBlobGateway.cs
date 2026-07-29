// ABOUTME: Acquires bounded ATProto thumbnail blobs from each DID's freshly resolved public PDS.
// ABOUTME: Validates content binding before staging exact bytes through provider-neutral file storage.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using CarpaNet;
using CarpaNet.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Models.Storage;
using Explore.Application.Services;
using Explore.Atproto.Transport;

namespace Explore.Infrastructure.Services.Federation;

public sealed class AtprotoThumbnailBlobGateway : IAtprotoThumbnailBlobGateway
{
    private const int MaximumIdentityResponseBytes = 1024 * 1024;
    private readonly Func<AtprotoOutboundPolicy, HttpMessageHandler> _primaryHandlerFactory;
    private readonly IFileStorageProvider _storage;
    private readonly int _maximumBytes;
    private readonly TimeSpan _requestTimeout;

    public AtprotoThumbnailBlobGateway(IFileStorageProvider storage)
        : this(
            policy => AtprotoHardenedHttpClient.CreatePrimaryHandler(policy, TimeSpan.FromSeconds(5)),
            storage,
            maximumBytes: AtprotoPdsSnapshotGateway.MaximumTargetRecordBytes,
            requestTimeout: AtprotoPdsSnapshotGateway.RequestTimeout)
    {
    }

    internal AtprotoThumbnailBlobGateway(
        Func<AtprotoOutboundPolicy, HttpMessageHandler> primaryHandlerFactory,
        IFileStorageProvider storage,
        int maximumBytes,
        TimeSpan requestTimeout)
    {
        ArgumentNullException.ThrowIfNull(primaryHandlerFactory);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumBytes);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(requestTimeout, TimeSpan.Zero);

        _primaryHandlerFactory = primaryHandlerFactory;
        _storage = storage;
        _maximumBytes = maximumBytes;
        _requestTimeout = requestTimeout;
    }

    public async Task<FileStorageWriteResult?> FetchAndStageAsync(
        AtprotoThumbnailBlobCandidate? candidate,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!TryValidateCandidate(candidate, tenantId, out ATCid cid, out string? mimeType))
        {
            return null;
        }

        var policy = new AtprotoOutboundPolicy(allowsDevelopmentLoopback: false);
        FileStorageWriteResult? staged = null;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_requestTimeout);
            Uri pdsOrigin = await ResolvePdsOriginAsync(
                candidate!.Did,
                policy,
                timeout.Token).ConfigureAwait(false);
            byte[] bytes = await FetchBlobAsync(
                pdsOrigin,
                candidate,
                cid,
                mimeType!,
                policy,
                timeout.Token).ConfigureAwait(false);

            using var content = new MemoryStream(bytes, writable: false);
            staged = await _storage.WriteAsync(
                new FileStorageWriteInput(
                    tenantId,
                    content,
                    mimeType!,
                    candidate.Cid,
                    Extension: null,
                    ExpectedSizeBytes: candidate.Size,
                    MaxSizeBytes: _maximumBytes),
                timeout.Token).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync(staged, CancellationToken.None).ConfigureAwait(false);
                }
                finally
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            return staged;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    public async Task CleanupAsync(
        FileStorageWriteResult staged,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(staged);
        await _storage.DeleteAsync(
            new FileStorageDeleteInput(staged.ObjectKey),
            cancellationToken).ConfigureAwait(false);
    }

    private bool TryValidateCandidate(
        AtprotoThumbnailBlobCandidate? candidate,
        Guid tenantId,
        out ATCid cid,
        [NotNullWhen(true)] out string? mimeType)
    {
        cid = default;
        mimeType = null;
        if (candidate is null
            || tenantId == Guid.Empty
            || candidate.Size <= 0
            || candidate.Size > _maximumBytes
            || string.IsNullOrWhiteSpace(candidate.Did)
            || string.IsNullOrWhiteSpace(candidate.Cid)
            || string.IsNullOrWhiteSpace(candidate.MimeType)
            || !IsSupportedDid(candidate.Did)
            || !SafeRasterContentPolicy.TryNormalizeMimeType(candidate.MimeType, out mimeType))
        {
            return false;
        }

        try
        {
            cid = new ATCid(candidate.Cid);
            return cid.Hash is { Length: ATCid.Sha256HashLength };
        }
        catch
        {
            return false;
        }
    }

    private async Task<Uri> ResolvePdsOriginAsync(
        string did,
        AtprotoOutboundPolicy policy,
        CancellationToken cancellationToken)
    {
        using var client = CreateBoundedIdentityClient(policy);
        using var resolver = new IdentityResolver(client);
        DidDocument document = await resolver.ResolveDidAsync(
            did,
            skipCache: true,
            cancellationToken).ConfigureAwait(false);
        if (!string.Equals(document.Id, did, StringComparison.Ordinal))
        {
            throw new InvalidDataException("ATProto identity binding mismatch.");
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
            throw new InvalidDataException("ATProto PDS endpoint is invalid.");
        }

        policy.ValidateUri(endpoint);
        return new Uri(endpoint.GetLeftPart(UriPartial.Authority) + "/", UriKind.Absolute);
    }

    private async Task<byte[]> FetchBlobAsync(
        Uri pdsOrigin,
        AtprotoThumbnailBlobCandidate candidate,
        ATCid cid,
        string expectedMimeType,
        AtprotoOutboundPolicy policy,
        CancellationToken cancellationToken)
    {
        using var client = CreateBlobClient(policy);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(
                pdsOrigin,
                $"xrpc/com.atproto.sync.getBlob?did={Uri.EscapeDataString(candidate.Did)}&cid={Uri.EscapeDataString(candidate.Cid)}"));
        using HttpResponseMessage response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        long? declaredSize = response.Content.Headers.ContentLength;
        if (!response.IsSuccessStatusCode
            || declaredSize is { } value
                && (value <= 0 || value != candidate.Size || value > _maximumBytes)
            || !SafeRasterContentPolicy.TryNormalizeMimeType(
                response.Content.Headers.ContentType?.ToString(),
                out string? responseMimeType)
            || !string.Equals(responseMimeType, expectedMimeType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("ATProto thumbnail response metadata is invalid.");
        }

        byte[] bytes = await AtprotoHttpContent.ReadBoundedAsync(
            response.Content,
            _maximumBytes,
            cancellationToken).ConfigureAwait(false);
        if (bytes.Length != candidate.Size
            || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes), cid.Hash!))
        {
            throw new InvalidDataException("ATProto thumbnail content binding is invalid.");
        }

        if (!SafeRasterContentPolicy.MatchesContainer(bytes, expectedMimeType))
        {
            throw new InvalidDataException("ATProto thumbnail container does not match the declared media type.");
        }

        return bytes;
    }

    private static bool IsSupportedDid(string did)
    {
        int suffixStart = did.StartsWith("did:plc:", StringComparison.Ordinal)
            ? "did:plc:".Length
            : did.StartsWith("did:web:", StringComparison.Ordinal)
                ? "did:web:".Length
                : -1;
        if (suffixStart < 0 || did.Length <= suffixStart || did.Length > 255)
        {
            return false;
        }

        foreach (char value in did.AsSpan(suffixStart))
        {
            if (value > 0x7f
                || char.IsControl(value)
                || char.IsWhiteSpace(value)
                || value is '/' or '?' or '#' or '\\')
            {
                return false;
            }
        }

        return true;
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The returned HttpClient owns and disposes the complete handler chain.")]
    private HttpClient CreateBoundedIdentityClient(AtprotoOutboundPolicy policy) => new(
        new AtprotoBoundedResponseHandler(
            MaximumIdentityResponseBytes,
            _primaryHandlerFactory(policy)),
        disposeHandler: true)
    {
        Timeout = _requestTimeout
    };

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The returned HttpClient owns and disposes the primary handler.")]
    private HttpClient CreateBlobClient(AtprotoOutboundPolicy policy) =>
        new(_primaryHandlerFactory(policy), disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
}
