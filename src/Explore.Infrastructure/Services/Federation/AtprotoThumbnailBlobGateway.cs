// ABOUTME: Acquires bounded ATProto thumbnail blobs from each DID's freshly resolved public PDS.
// ABOUTME: Validates content binding before staging exact bytes through provider-neutral file storage.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using CarpaNet;
using CarpaNet.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Features.Federation.Atproto.Models;
using Explore.Application.Models.Storage;
using Explore.Atproto.Transport;

namespace Explore.Infrastructure.Services.Federation;

public sealed class AtprotoThumbnailBlobGateway : IAtprotoThumbnailBlobGateway
{
    private const int MaximumIdentityResponseBytes = 1024 * 1024;
    private static readonly byte[] JpegSignature = [0xff, 0xd8, 0xff];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
    private static readonly byte[] Vp8KeyFrameSignature = [0x9d, 0x01, 0x2a];
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
            || !TryReadImageMime(candidate.MimeType, out mimeType))
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
            || !TryReadImageMime(response.Content.Headers.ContentType, out string? responseMimeType)
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

        if (!MatchesRasterContainer(bytes, expectedMimeType))
        {
            throw new InvalidDataException("ATProto thumbnail container does not match the declared media type.");
        }

        return bytes;
    }

    private static bool TryReadImageMime(
        string value,
        [NotNullWhen(true)] out string? mimeType)
    {
        mimeType = null;
        return MediaTypeHeaderValue.TryParse(value, out MediaTypeHeaderValue? parsed)
            && TryReadImageMime(parsed, out mimeType);
    }

    private static bool TryReadImageMime(
        MediaTypeHeaderValue? value,
        [NotNullWhen(true)] out string? mimeType)
    {
        mimeType = null;
        if (value?.MediaType is not { } parsedMimeType
            || value.Parameters.Count != 0)
        {
            return false;
        }

        string normalizedMimeType = parsedMimeType.ToLowerInvariant();
        if (normalizedMimeType is not (
            "image/jpeg" or
            "image/png" or
            "image/gif" or
            "image/webp" or
            "image/avif"))
        {
            return false;
        }

        mimeType = normalizedMimeType;
        return true;
    }

    private static bool MatchesRasterContainer(ReadOnlySpan<byte> bytes, string mimeType) =>
        mimeType switch
        {
            "image/jpeg" => IsJpegContainer(bytes),
            "image/png" => IsPngContainer(bytes),
            "image/gif" => IsGifContainer(bytes),
            "image/webp" => IsWebpContainer(bytes),
            "image/avif" => IsAvifContainer(bytes),
            _ => false
        };

    private static bool IsPngContainer(ReadOnlySpan<byte> bytes)
    {
        if (!bytes.StartsWith(PngSignature))
        {
            return false;
        }

        int offset = PngSignature.Length;
        bool seenHeader = false;
        bool seenImageData = false;
        bool imageDataEnded = false;
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < 12)
            {
                return false;
            }

            uint payloadLength = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(offset, 4));
            if (payloadLength > int.MaxValue
                || payloadLength > bytes.Length - offset - 12)
            {
                return false;
            }

            ReadOnlySpan<byte> chunkType = bytes.Slice(offset + 4, 4);
            ReadOnlySpan<byte> payload = bytes.Slice(offset + 8, (int)payloadLength);
            offset += checked(12 + (int)payloadLength);
            if (!seenHeader)
            {
                if (!chunkType.SequenceEqual("IHDR"u8)
                    || !IsPngHeader(payload))
                {
                    return false;
                }

                seenHeader = true;
                continue;
            }

            if (chunkType.SequenceEqual("IHDR"u8))
            {
                return false;
            }

            if (chunkType.SequenceEqual("IDAT"u8))
            {
                if (imageDataEnded || payload.IsEmpty)
                {
                    return false;
                }

                seenImageData = true;
                continue;
            }

            imageDataEnded |= seenImageData;
            if (chunkType.SequenceEqual("IEND"u8))
            {
                return payload.IsEmpty
                    && seenImageData
                    && offset == bytes.Length;
            }

            if (IsPngCriticalChunk(chunkType)
                && !chunkType.SequenceEqual("PLTE"u8))
            {
                return false;
            }
        }

        return false;
    }

    private static bool IsPngHeader(ReadOnlySpan<byte> payload)
    {
        if (payload.Length != 13
            || BinaryPrimitives.ReadUInt32BigEndian(payload[..4]) == 0
            || BinaryPrimitives.ReadUInt32BigEndian(payload.Slice(4, 4)) == 0
            || payload[10] != 0
            || payload[11] != 0
            || payload[12] > 1)
        {
            return false;
        }

        byte bitDepth = payload[8];
        return payload[9] switch
        {
            0 => bitDepth is 1 or 2 or 4 or 8 or 16,
            2 => bitDepth is 8 or 16,
            3 => bitDepth is 1 or 2 or 4 or 8,
            4 or 6 => bitDepth is 8 or 16,
            _ => false
        };
    }

    private static bool IsPngCriticalChunk(ReadOnlySpan<byte> chunkType) =>
        chunkType.Length == 4
        && chunkType[0] is >= (byte)'A' and <= (byte)'Z';

    private static bool IsJpegContainer(ReadOnlySpan<byte> bytes)
    {
        if (!bytes.StartsWith(JpegSignature))
        {
            return false;
        }

        int offset = 2;
        bool seenFrame = false;
        bool seenScan = false;
        while (offset < bytes.Length)
        {
            if (bytes[offset++] != 0xff)
            {
                return false;
            }

            while (offset < bytes.Length && bytes[offset] == 0xff)
            {
                offset++;
            }

            if (offset >= bytes.Length)
            {
                return false;
            }

            byte marker = bytes[offset++];
            if (marker == 0xd9)
            {
                return seenFrame && seenScan && offset == bytes.Length;
            }

            if (marker is 0x00 or 0x01 or 0xd8
                || marker is >= 0xd0 and <= 0xd7
                || bytes.Length - offset < 2)
            {
                return false;
            }

            int segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset, 2));
            if (segmentLength < 2 || segmentLength > bytes.Length - offset)
            {
                return false;
            }

            ReadOnlySpan<byte> payload = bytes.Slice(offset + 2, segmentLength - 2);
            offset += segmentLength;
            if (IsJpegFrameMarker(marker))
            {
                if (seenFrame || !IsJpegFrameHeader(payload))
                {
                    return false;
                }

                seenFrame = true;
                continue;
            }

            if (marker != 0xda)
            {
                continue;
            }

            if (!seenFrame || !IsJpegScanHeader(payload))
            {
                return false;
            }

            seenScan = true;
            if (!TrySkipJpegEntropy(bytes, ref offset))
            {
                return false;
            }
        }

        return false;
    }

    private static bool IsJpegFrameMarker(byte marker) =>
        marker is >= 0xc0 and <= 0xc3
        or >= 0xc5 and <= 0xc7
        or >= 0xc9 and <= 0xcb
        or >= 0xcd and <= 0xcf;

    private static bool IsJpegFrameHeader(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 9
            || payload[0] == 0
            || BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(1, 2)) == 0
            || BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(3, 2)) == 0)
        {
            return false;
        }

        int componentCount = payload[5];
        return componentCount > 0
            && payload.Length == 6 + (3 * componentCount);
    }

    private static bool IsJpegScanHeader(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 6)
        {
            return false;
        }

        int componentCount = payload[0];
        return componentCount > 0
            && payload.Length == 1 + (2 * componentCount) + 3;
    }

    private static bool TrySkipJpegEntropy(ReadOnlySpan<byte> bytes, ref int offset)
    {
        while (offset < bytes.Length)
        {
            if (bytes[offset] != 0xff)
            {
                offset++;
                continue;
            }

            int markerStart = offset++;
            while (offset < bytes.Length && bytes[offset] == 0xff)
            {
                offset++;
            }

            if (offset >= bytes.Length)
            {
                return false;
            }

            byte marker = bytes[offset];
            if (marker == 0x00 || marker is >= 0xd0 and <= 0xd7)
            {
                offset++;
                continue;
            }

            offset = markerStart;
            return true;
        }

        return false;
    }

    private static bool IsGifContainer(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 13
            || !(bytes.StartsWith("GIF87a"u8) || bytes.StartsWith("GIF89a"u8)))
        {
            return false;
        }

        ReadOnlySpan<byte> logicalScreen = bytes.Slice(6, 7);
        if (BinaryPrimitives.ReadUInt16LittleEndian(logicalScreen[..2]) == 0
            || BinaryPrimitives.ReadUInt16LittleEndian(logicalScreen.Slice(2, 2)) == 0)
        {
            return false;
        }

        int offset = 13;
        if (!TrySkipGifColorTable(bytes, ref offset, logicalScreen[4]))
        {
            return false;
        }

        bool seenImage = false;
        while (offset < bytes.Length)
        {
            byte blockType = bytes[offset++];
            if (blockType == 0x3b)
            {
                return seenImage && offset == bytes.Length;
            }

            if (blockType == 0x21)
            {
                if (offset >= bytes.Length)
                {
                    return false;
                }

                offset++;
                if (!TrySkipGifSubBlocks(bytes, ref offset, requireData: false))
                {
                    return false;
                }

                continue;
            }

            if (blockType != 0x2c || bytes.Length - offset < 9)
            {
                return false;
            }

            ReadOnlySpan<byte> descriptor = bytes.Slice(offset, 9);
            offset += 9;
            if (BinaryPrimitives.ReadUInt16LittleEndian(descriptor.Slice(4, 2)) == 0
                || BinaryPrimitives.ReadUInt16LittleEndian(descriptor.Slice(6, 2)) == 0
                || !TrySkipGifColorTable(bytes, ref offset, descriptor[8])
                || offset >= bytes.Length
                || bytes[offset++] is < 2 or > 8
                || !TrySkipGifSubBlocks(bytes, ref offset, requireData: true))
            {
                return false;
            }

            seenImage = true;
        }

        return false;
    }

    private static bool TrySkipGifColorTable(
        ReadOnlySpan<byte> bytes,
        ref int offset,
        byte packedFields)
    {
        if ((packedFields & 0x80) == 0)
        {
            return true;
        }

        int tableLength = 3 * (1 << ((packedFields & 0x07) + 1));
        if (tableLength > bytes.Length - offset)
        {
            return false;
        }

        offset += tableLength;
        return true;
    }

    private static bool TrySkipGifSubBlocks(
        ReadOnlySpan<byte> bytes,
        ref int offset,
        bool requireData)
    {
        bool seenData = false;
        while (offset < bytes.Length)
        {
            int blockLength = bytes[offset++];
            if (blockLength == 0)
            {
                return seenData || !requireData;
            }

            if (blockLength > bytes.Length - offset)
            {
                return false;
            }

            seenData = true;
            offset += blockLength;
        }

        return false;
    }

    private static bool IsWebpContainer(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 12
            || !bytes[..4].SequenceEqual("RIFF"u8)
            || BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4, 4)) != bytes.Length - 8
            || !bytes.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return false;
        }

        int offset = 12;
        bool seenImage = false;
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < 8)
            {
                return false;
            }

            ReadOnlySpan<byte> chunkType = bytes.Slice(offset, 4);
            uint payloadLength = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset + 4, 4));
            offset += 8;
            if (payloadLength > int.MaxValue
                || payloadLength > bytes.Length - offset)
            {
                return false;
            }

            ReadOnlySpan<byte> payload = bytes.Slice(offset, (int)payloadLength);
            offset += (int)payloadLength;
            if ((payloadLength & 1) != 0)
            {
                if (offset >= bytes.Length)
                {
                    return false;
                }

                offset++;
            }

            if (chunkType.SequenceEqual("VP8 "u8))
            {
                if (seenImage || !IsVp8Payload(payload))
                {
                    return false;
                }

                seenImage = true;
            }
            else if (chunkType.SequenceEqual("VP8L"u8))
            {
                if (seenImage || !IsVp8LosslessPayload(payload))
                {
                    return false;
                }

                seenImage = true;
            }
            else if (chunkType.SequenceEqual("VP8X"u8))
            {
                if (payload.Length != 10 || (payload[0] & 0x02) != 0)
                {
                    return false;
                }
            }
            else if (!(chunkType.SequenceEqual("ALPH"u8)
                || chunkType.SequenceEqual("ICCP"u8)
                || chunkType.SequenceEqual("EXIF"u8)
                || chunkType.SequenceEqual("XMP "u8)))
            {
                return false;
            }
        }

        return seenImage && offset == bytes.Length;
    }

    private static bool IsVp8Payload(ReadOnlySpan<byte> payload) =>
        payload.Length > 10
        && (payload[0] & 0x01) == 0
        && payload.Slice(3, 3).SequenceEqual(Vp8KeyFrameSignature)
        && (BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(6, 2)) & 0x3fff) > 0
        && (BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(8, 2)) & 0x3fff) > 0;

    private static bool IsVp8LosslessPayload(ReadOnlySpan<byte> payload)
    {
        if (payload.Length <= 5 || payload[0] != 0x2f)
        {
            return false;
        }

        uint dimensions = BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(1, 4));
        return (dimensions >> 29) == 0;
    }

    private static bool IsAvifContainer(ReadOnlySpan<byte> bytes)
    {
        int offset = 0;
        bool seenFileType = false;
        bool seenMetadata = false;
        bool seenMediaData = false;
        while (offset < bytes.Length)
        {
            if (!TryReadIsoBox(bytes, ref offset, out ReadOnlySpan<byte> boxType, out ReadOnlySpan<byte> payload))
            {
                return false;
            }

            if (!seenFileType)
            {
                if (!boxType.SequenceEqual("ftyp"u8) || !IsAvifFileType(payload))
                {
                    return false;
                }

                seenFileType = true;
                continue;
            }

            if (boxType.SequenceEqual("meta"u8))
            {
                if (seenMetadata || !IsAvifMetadata(payload))
                {
                    return false;
                }

                seenMetadata = true;
            }
            else if (boxType.SequenceEqual("mdat"u8))
            {
                if (seenMediaData || payload.IsEmpty)
                {
                    return false;
                }

                seenMediaData = true;
            }
        }

        return seenFileType && seenMetadata && seenMediaData;
    }

    private static bool IsAvifFileType(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 8 || payload.Length % 4 != 0)
        {
            return false;
        }

        if (IsAvifBrand(payload[..4]))
        {
            return true;
        }

        for (int offset = 8; offset < payload.Length; offset += 4)
        {
            if (IsAvifBrand(payload.Slice(offset, 4)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAvifMetadata(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 4 || payload[0] != 0)
        {
            return false;
        }

        int offset = 4;
        bool seenHandler = false;
        bool seenPrimaryItem = false;
        bool seenLocation = false;
        bool seenItemInfo = false;
        bool seenProperties = false;
        while (offset < payload.Length)
        {
            if (!TryReadIsoBox(
                    payload,
                    ref offset,
                    out ReadOnlySpan<byte> boxType,
                    out ReadOnlySpan<byte> childPayload))
            {
                return false;
            }

            seenHandler |= boxType.SequenceEqual("hdlr"u8) && !childPayload.IsEmpty;
            seenPrimaryItem |= boxType.SequenceEqual("pitm"u8) && !childPayload.IsEmpty;
            seenLocation |= boxType.SequenceEqual("iloc"u8) && !childPayload.IsEmpty;
            seenItemInfo |= boxType.SequenceEqual("iinf"u8) && !childPayload.IsEmpty;
            seenProperties |= boxType.SequenceEqual("iprp"u8) && !childPayload.IsEmpty;
        }

        return seenHandler
            && seenPrimaryItem
            && seenLocation
            && seenItemInfo
            && seenProperties;
    }

    private static bool TryReadIsoBox(
        ReadOnlySpan<byte> bytes,
        ref int offset,
        out ReadOnlySpan<byte> boxType,
        out ReadOnlySpan<byte> payload)
    {
        boxType = default;
        payload = default;
        if (bytes.Length - offset < 8)
        {
            return false;
        }

        int boxStart = offset;
        uint compactSize = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(offset, 4));
        boxType = bytes.Slice(offset + 4, 4);
        int headerLength = 8;
        ulong boxSize = compactSize;
        if (compactSize == 1)
        {
            if (bytes.Length - offset < 16)
            {
                return false;
            }

            headerLength = 16;
            boxSize = BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(offset + 8, 8));
        }

        if (boxSize < (ulong)headerLength
            || boxSize > int.MaxValue
            || boxSize > (ulong)(bytes.Length - boxStart))
        {
            return false;
        }

        payload = bytes.Slice(boxStart + headerLength, (int)boxSize - headerLength);
        offset = boxStart + (int)boxSize;
        return true;
    }

    private static bool IsAvifBrand(ReadOnlySpan<byte> brand) =>
        brand.SequenceEqual("avif"u8) || brand.SequenceEqual("avis"u8);

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
