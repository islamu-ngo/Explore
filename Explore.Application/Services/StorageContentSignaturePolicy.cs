// ABOUTME: Content-signature validation policy for storage upload finalization.
// ABOUTME: Compares trusted session MIME/extension metadata with inspected upload bytes before provider writes.

using System.Buffers.Binary;

namespace Explore.Application.Services;

public static class StorageContentSignaturePolicy
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] OleCompoundDocumentHeader = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private static readonly IReadOnlyDictionary<string, ContentSignatureRule> Rules =
        new Dictionary<string, ContentSignatureRule>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = new(["jpg", "jpeg", "jpe"], 3, MatchesJpeg),
            ["image/png"] = new(["png"], 8, MatchesPng),
            ["image/gif"] = new(["gif"], 6, MatchesGif),
            ["image/webp"] = new(["webp"], 12, MatchesWebp),
            ["application/pdf"] = new(["pdf"], 5, MatchesPdf),
            ["application/rtf"] = new(["rtf"], 5, MatchesRtf),
            ["text/rtf"] = new(["rtf"], 5, MatchesRtf),
            ["application/msword"] = new(["doc"], 8, MatchesOleCompoundDocument),
            ["application/vnd.ms-excel"] = new(["xls"], 8, MatchesOleCompoundDocument),
            ["application/vnd.ms-powerpoint"] = new(["ppt"], 8, MatchesOleCompoundDocument),
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] =
                new(["docx"], 4, MatchesZipContainer),
            ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] =
                new(["xlsx"], 4, MatchesZipContainer),
            ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] =
                new(["pptx"], 4, MatchesZipContainer),
            ["application/vnd.oasis.opendocument.text"] =
                new(["odt"], 4, MatchesZipContainer),
            ["application/vnd.oasis.opendocument.spreadsheet"] =
                new(["ods"], 4, MatchesZipContainer)
        };

    public static async Task<StorageContentInspectionResult> InspectAsync(
        Stream content,
        string contentType,
        string? extension,
        long expectedSizeBytes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var normalizedContentType = NormalizeContentType(contentType);
        if (!Rules.TryGetValue(normalizedContentType, out var rule))
        {
            if (normalizedContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return StorageContentInspectionResult.Failed(
                    content,
                    ["Reserved image content type is not allowed for upload."]);
            }

            return StorageContentInspectionResult.Succeeded(content);
        }

        var errors = new List<string>();
        var normalizedExtension = NormalizeExtension(extension);
        if (string.IsNullOrWhiteSpace(normalizedExtension) || !rule.Extensions.Contains(normalizedExtension))
        {
            errors.Add("File extension did not match the reserved content type.");
        }

        var prefix = new byte[rule.RequiredBytes];
        var originalPosition = content.CanSeek ? content.Position : 0;
        var bytesRead = await ReadPrefixAsync(content, prefix, cancellationToken);
        Stream inspectedContent = content;

        if (content.CanSeek)
        {
            content.Seek(originalPosition, SeekOrigin.Begin);
        }
        else
        {
            inspectedContent = new ReplayPrefixReadStream(prefix[..bytesRead], content);
        }

        if (expectedSizeBytes < rule.RequiredBytes || !rule.Matches(prefix.AsSpan(0, bytesRead)))
        {
            errors.Add("Upload bytes did not match the reserved content type signature.");
        }

        return errors.Count == 0
            ? StorageContentInspectionResult.Succeeded(inspectedContent)
            : StorageContentInspectionResult.Failed(inspectedContent, errors);
    }

    private static async Task<int> ReadPrefixAsync(
        Stream content,
        byte[] prefix,
        CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < prefix.Length)
        {
            var bytesRead = await content.ReadAsync(prefix.AsMemory(totalRead), cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalRead += bytesRead;
        }

        return totalRead;
    }

    private static string NormalizeContentType(string contentType)
    {
        var candidate = contentType.Trim();
        var semicolonIndex = candidate.IndexOf(';', StringComparison.Ordinal);
        return (semicolonIndex >= 0 ? candidate[..semicolonIndex] : candidate).Trim().ToLowerInvariant();
    }

    private static string? NormalizeExtension(string? extension)
    {
        var candidate = extension?.Trim();
        return string.IsNullOrWhiteSpace(candidate)
            ? null
            : candidate.TrimStart('.').ToLowerInvariant();
    }

    private static bool MatchesJpeg(ReadOnlySpan<byte> header)
        => header.Length >= 3 &&
           header[0] == 0xFF &&
           header[1] == 0xD8 &&
           header[2] == 0xFF;

    private static bool MatchesPng(ReadOnlySpan<byte> header)
        => header.StartsWith(PngHeader);

    private static bool MatchesGif(ReadOnlySpan<byte> header)
        => header.StartsWith("GIF87a"u8) || header.StartsWith("GIF89a"u8);

    private static bool MatchesWebp(ReadOnlySpan<byte> header)
        => header.Length >= 12 &&
           header[..4].SequenceEqual("RIFF"u8) &&
           header[8..12].SequenceEqual("WEBP"u8);

    private static bool MatchesPdf(ReadOnlySpan<byte> header)
        => header.StartsWith("%PDF-"u8);

    private static bool MatchesRtf(ReadOnlySpan<byte> header)
        => header.StartsWith("{\\rtf"u8);

    private static bool MatchesOleCompoundDocument(ReadOnlySpan<byte> header)
        => header.StartsWith(OleCompoundDocumentHeader);

    private static bool MatchesZipContainer(ReadOnlySpan<byte> header)
        => header.Length >= 4 &&
           BinaryPrimitives.ReadUInt32LittleEndian(header[..4]) is 0x04034B50 or 0x06054B50 or 0x08074B50;

    private sealed record ContentSignatureRule(
        string[] Extensions,
        int RequiredBytes,
        ContentSignatureMatcher Matches);

    private delegate bool ContentSignatureMatcher(ReadOnlySpan<byte> header);

    private sealed class ReplayPrefixReadStream : Stream
    {
        private readonly byte[] _prefix;
        private readonly Stream _inner;
        private int _prefixOffset;

        public ReplayPrefixReadStream(byte[] prefix, Stream inner)
        {
            _prefix = prefix;
            _inner = inner;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
            => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            if (_prefixOffset < _prefix.Length)
            {
                var count = Math.Min(buffer.Length, _prefix.Length - _prefixOffset);
                _prefix.AsSpan(_prefixOffset, count).CopyTo(buffer);
                _prefixOffset += count;
                return count;
            }

            return _inner.Read(buffer);
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (_prefixOffset < _prefix.Length)
            {
                var count = Math.Min(buffer.Length, _prefix.Length - _prefixOffset);
                _prefix.AsMemory(_prefixOffset, count).CopyTo(buffer);
                _prefixOffset += count;
                return count;
            }

            return await _inner.ReadAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

public sealed record StorageContentInspectionResult(
    Stream Content,
    IReadOnlyList<string> Errors)
{
    public bool Success => Errors.Count == 0;

    public static StorageContentInspectionResult Succeeded(Stream content)
        => new(content, []);

    public static StorageContentInspectionResult Failed(Stream content, IReadOnlyList<string> errors)
        => new(content, errors);
}
