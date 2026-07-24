// ABOUTME: Focused rendering tests for the API-local SkiaSharp event Open Graph image implementation.
// ABOUTME: Covers deterministic PNG output, embedded shaping font, fallback gradients, dates, and artwork cropping.

using System.Buffers.Binary;
using System.Security.Cryptography;
using Event.Api.IntegrationTests.Fixtures;
using Explore.API.Services.OpenGraph;
using Explore.Application.Features.Events.OpenGraph;
using Explore.Application.Features.Events.Requests.Queries;
using FluentAssertions;
using SkiaSharp;
using TUnit.Core;

namespace Event.Api.IntegrationTests.Features;

[Category(TestCategories.Fast)]
public sealed class SkiaEventOpenGraphImageRendererTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private const int CanvasWidth = 1200;
    private const int CanvasHeight = 630;
    private const int ArtworkLeft = 745;
    private const int ArtworkTop = 35;
    private const int ArtworkWidth = 420;
    private const int ArtworkHeight = 560;

    private readonly SkiaEventOpenGraphImageRenderer _renderer = new();

    [Test]
    public async Task RenderAsyncProducesStandardPngAndStrongSha256Etag()
    {
        var result = await RenderAsync(title: "Community Iftar");

        result.PngBytes.AsSpan(0, 8).SequenceEqual(PngSignature).Should().BeTrue();
        BinaryPrimitives.ReadInt32BigEndian(result.PngBytes.AsSpan(16, 4)).Should().Be(CanvasWidth);
        BinaryPrimitives.ReadInt32BigEndian(result.PngBytes.AsSpan(20, 4)).Should().Be(CanvasHeight);

        var expectedHash = Convert.ToHexStringLower(SHA256.HashData(result.PngBytes));
        result.ETag.Should().Be($"\"{expectedHash}\"");
        result.ETag.StartsWith("W/", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    }

    [Test]
    public async Task RenderAsyncIsDeterministicForIdenticalInput()
    {
        var first = await RenderAsync(title: "Deterministic Event");
        var second = await RenderAsync(title: "Deterministic Event");

        second.PngBytes.Should().Equal(first.PngBytes);
        second.ETag.Should().Be(first.ETag);
    }

    [Test]
    public async Task FallbackGradientDependsOnTrimmedTitleAndBlankUsesEventSeed()
    {
        var alpha = await RenderAsync(title: "  Alpha Gathering  ");
        var trimmedAlpha = await RenderAsync(title: "Alpha Gathering");
        var beta = await RenderAsync(title: "Beta Gathering");
        var blank = await RenderAsync(title: "   ");
        var eventSeed = await RenderAsync(title: "event");

        HashArtworkPanel(alpha.PngBytes).Should().Be(HashArtworkPanel(trimmedAlpha.PngBytes));
        HashArtworkPanel(alpha.PngBytes).Should().NotBe(HashArtworkPanel(beta.PngBytes));
        HashArtworkPanel(blank.PngBytes).Should().Be(HashArtworkPanel(eventSeed.PngBytes));
    }

    [Test]
    public async Task MissingInvalidUnsupportedAndOversizedArtworkUseSameFallback()
    {
        var fallback = await RenderAsync(title: "Fallback Event");
        using var invalid = new MemoryStream("not an image"u8.ToArray());
        var invalidResult = await RenderAsync("Fallback Event", invalid, "image/png");
        using var validPng = new MemoryStream(CreateSolidPng(40, 40, SKColors.Red));
        var unsupportedResult = await RenderAsync("Fallback Event", validPng, "image/svg+xml");
        using var oversized = new MemoryStream(new byte[(5 * 1024 * 1024) + 1]);
        var oversizedResult = await RenderAsync("Fallback Event", oversized, "image/png");

        var expectedPanel = HashArtworkPanel(fallback.PngBytes);
        HashArtworkPanel(invalidResult.PngBytes).Should().Be(expectedPanel);
        HashArtworkPanel(unsupportedResult.PngBytes).Should().Be(expectedPanel);
        HashArtworkPanel(oversizedResult.PngBytes).Should().Be(expectedPanel);
    }

    [Test]
    public async Task NonSeekableOversizedArtwork_UsesSameGradientFallbackWithoutDecoding()
    {
        const int maximumEncodedArtworkBytes = 5 * 1024 * 1024;
        var title = "Non-Seekable Oversized Event";
        var fallback = await RenderAsync(title);
        var validPng = CreateSolidPng(40, 40, SKColors.Red);
        var oversizedEncodedData = new byte[maximumEncodedArtworkBytes + 1];
        validPng.CopyTo(oversizedEncodedData, 0);

        using (var decodeStream = new NonSeekableReadStream(oversizedEncodedData))
        {
            using var decoded = await SkiaEventOpenGraphImageRenderer.TryDecodeArtworkAsync(
                decodeStream,
                "image/png",
                CancellationToken.None);

            decoded.Should().BeNull();
        }

        using var artwork = new NonSeekableReadStream(oversizedEncodedData);
        var result = await RenderAsync(title, artwork, "image/png");

        HashArtworkPanel(result.PngBytes).Should().Be(HashArtworkPanel(fallback.PngBytes));
        BinaryPrimitives.ReadInt32BigEndian(result.PngBytes.AsSpan(16, 4)).Should().Be(CanvasWidth);
        BinaryPrimitives.ReadInt32BigEndian(result.PngBytes.AsSpan(20, 4)).Should().Be(CanvasHeight);
    }

    [Test]
    public async Task TryDecodeArtworkAsync_LargeJpeg_UsesCodecScaleWithinArtworkPixelCeiling()
    {
        var jpegBytes = CreateStripedJpeg(
            width: 2000,
            height: 1500,
            vertical: true,
            firstBoundary: 500,
            secondBoundary: 1500);
        using var artwork = new MemoryStream(jpegBytes);

        using var decoded = await SkiaEventOpenGraphImageRenderer.TryDecodeArtworkAsync(
            artwork,
            "image/jpeg",
            CancellationToken.None);

        decoded.Should().NotBeNull();
        decoded!.Width.Should().BeGreaterThanOrEqualTo(ArtworkWidth);
        decoded.Height.Should().BeGreaterThanOrEqualTo(ArtworkHeight);
        ((long)decoded.Width * decoded.Height).Should().BeLessThanOrEqualTo(4L * ArtworkWidth * ArtworkHeight);

        using var renderArtwork = new MemoryStream(jpegBytes);
        var result = await RenderAsync("Large JPEG Event", renderArtwork, "image/jpeg");

        BinaryPrimitives.ReadInt32BigEndian(result.PngBytes.AsSpan(16, 4)).Should().Be(CanvasWidth);
        BinaryPrimitives.ReadInt32BigEndian(result.PngBytes.AsSpan(20, 4)).Should().Be(CanvasHeight);
    }

    [Test]
    public async Task TryDecodeArtworkAsync_LargePng_UsesGradientFallbackWhenCodecCannotScale()
    {
        var fallback = await RenderAsync(title: "Large PNG Event");
        using var artwork = new MemoryStream(CreateSolidPng(4000, 3000, SKColors.Red));

        using var decoded = await SkiaEventOpenGraphImageRenderer.TryDecodeArtworkAsync(
            artwork,
            "image/png",
            CancellationToken.None);

        decoded.Should().BeNull();

        using var renderArtwork = new MemoryStream(CreateSolidPng(4000, 3000, SKColors.Red));
        var result = await RenderAsync("Large PNG Event", renderArtwork, "image/png");

        BinaryPrimitives.ReadInt32BigEndian(result.PngBytes.AsSpan(16, 4)).Should().Be(CanvasWidth);
        BinaryPrimitives.ReadInt32BigEndian(result.PngBytes.AsSpan(20, 4)).Should().Be(CanvasHeight);
        HashArtworkPanel(result.PngBytes).Should().Be(HashArtworkPanel(fallback.PngBytes));
    }

    [Test]
    public async Task PortraitArtworkUsesCenteredCoverCrop()
    {
        using var artwork = new MemoryStream(CreateStripedPng(
            width: 300,
            height: 600,
            vertical: false,
            firstBoundary: 100,
            secondBoundary: 500));

        var result = await RenderAsync("Portrait Event", artwork, "image/png");
        using var bitmap = SKBitmap.Decode(result.PngBytes);

        AssertPanelCenterLineIsGreen(bitmap, vertical: true);
    }

    [Test]
    public async Task LandscapeArtworkUsesCenteredCoverCrop()
    {
        using var artwork = new MemoryStream(CreateStripedPng(
            width: 800,
            height: 400,
            vertical: true,
            firstBoundary: 250,
            secondBoundary: 550));

        var result = await RenderAsync("Landscape Event", artwork, "image/png");
        using var bitmap = SKBitmap.Decode(result.PngBytes);

        AssertPanelCenterLineIsGreen(bitmap, vertical: false);
    }

    [Test]
    public async Task FormatDateRangeUsesInvariantBoundedBadgeText()
    {
        SkiaEventOpenGraphImageRenderer.FormatDateRange(null, null)
            .Should().Be("DATE TO BE ANNOUNCED");
        SkiaEventOpenGraphImageRenderer.FormatDateRange(
                new DateOnly(2026, 1, 5),
                new DateOnly(2026, 1, 5))
            .Should().Be("JAN 5, 2026");
        SkiaEventOpenGraphImageRenderer.FormatDateRange(
                new DateOnly(2026, 1, 5),
                new DateOnly(2026, 1, 7))
            .Should().Be("JAN 5–7, 2026");
        SkiaEventOpenGraphImageRenderer.FormatDateRange(
                new DateOnly(2026, 1, 31),
                new DateOnly(2026, 2, 2))
            .Should().Be("JAN 31 – FEB 2, 2026");
        SkiaEventOpenGraphImageRenderer.FormatDateRange(
                new DateOnly(2026, 12, 31),
                new DateOnly(2027, 1, 1))
            .Should().Be("DEC 31, 2026 – JAN 1, 2027");

        await Task.CompletedTask;
    }

    [Test]
    public async Task EmbeddedFontLoadsWithoutHostFontDiscovery()
    {
        using var typeface = SkiaEventOpenGraphImageRenderer.LoadEmbeddedTypeface();

        typeface.FamilyName.Should().Contain("Noto Sans Arabic");
        typeface.GetGlyphs("فعاليات".AsSpan()).Should().OnlyContain(glyph => glyph != 0);
        await Task.CompletedTask;
    }

    [Test]
    public async Task ArabicBrandAndTitleRenderNonblankShapedText()
    {
        var arabic = await RenderAsync(
            title: "أمسية ثقافية للمجتمع",
            brandDisplayName: "فعاليات إسلامو");
        var blank = await RenderAsync(title: "", brandDisplayName: "");

        HashTextRegion(arabic.PngBytes).Should().NotBe(HashTextRegion(blank.PngBytes));
        CountDarkPixels(arabic.PngBytes).Should().BeGreaterThan(500);
    }

    private Task<EventOpenGraphImageRenderResult> RenderAsync(
        string title,
        Stream? artwork = null,
        string? contentType = null,
        string brandDisplayName = "ISLAMU Event")
    {
        return _renderer.RenderAsync(
            new EventOpenGraphImageRenderRequest(
                title,
                new DateOnly(2026, 1, 5),
                new DateOnly(2026, 1, 7),
                brandDisplayName,
                artwork,
                contentType),
            CancellationToken.None);
    }

    private static string HashArtworkPanel(byte[] pngBytes)
        => HashRegion(pngBytes, ArtworkLeft, ArtworkTop, ArtworkWidth, ArtworkHeight);

    private static string HashTextRegion(byte[] pngBytes)
        => HashRegion(pngBytes, 48, 48, 640, 534);

    private static string HashRegion(byte[] pngBytes, int left, int top, int width, int height)
    {
        using var bitmap = SKBitmap.Decode(pngBytes);
        var pixels = new byte[width * height * 4];
        var offset = 0;
        for (var y = top; y < top + height; y++)
        {
            for (var x = left; x < left + width; x++)
            {
                var color = bitmap.GetPixel(x, y);
                pixels[offset++] = color.Red;
                pixels[offset++] = color.Green;
                pixels[offset++] = color.Blue;
                pixels[offset++] = color.Alpha;
            }
        }

        return Convert.ToHexStringLower(SHA256.HashData(pixels));
    }

    private static int CountDarkPixels(byte[] pngBytes)
    {
        using var bitmap = SKBitmap.Decode(pngBytes);
        var count = 0;
        for (var y = 48; y < 582; y++)
        {
            for (var x = 48; x < 688; x++)
            {
                var color = bitmap.GetPixel(x, y);
                if (color.Red < 90 && color.Green < 90 && color.Blue < 90)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void AssertPanelCenterLineIsGreen(SKBitmap bitmap, bool vertical)
    {
        var points = vertical
            ? new[]
            {
                new SKPointI(ArtworkLeft + (ArtworkWidth / 2), ArtworkTop + 20),
                new SKPointI(ArtworkLeft + (ArtworkWidth / 2), ArtworkTop + (ArtworkHeight / 2)),
                new SKPointI(ArtworkLeft + (ArtworkWidth / 2), ArtworkTop + ArtworkHeight - 20)
            }
            : new[]
            {
                new SKPointI(ArtworkLeft + 20, ArtworkTop + (ArtworkHeight / 2)),
                new SKPointI(ArtworkLeft + (ArtworkWidth / 2), ArtworkTop + (ArtworkHeight / 2)),
                new SKPointI(ArtworkLeft + ArtworkWidth - 20, ArtworkTop + (ArtworkHeight / 2))
            };

        foreach (var point in points)
        {
            var color = bitmap.GetPixel(point.X, point.Y);
            color.Green.Should().BeGreaterThan(240);
            color.Red.Should().BeLessThan(15);
            color.Blue.Should().BeLessThan(15);
        }
    }

    private static byte[] CreateSolidPng(int width, int height, SKColor color)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(color);
        return EncodePng(bitmap);
    }

    private static byte[] CreateStripedPng(
        int width,
        int height,
        bool vertical,
        int firstBoundary,
        int secondBoundary)
        => CreateStripedImage(width, height, vertical, firstBoundary, secondBoundary, SKEncodedImageFormat.Png);

    private static byte[] CreateStripedJpeg(
        int width,
        int height,
        bool vertical,
        int firstBoundary,
        int secondBoundary)
        => CreateStripedImage(width, height, vertical, firstBoundary, secondBoundary, SKEncodedImageFormat.Jpeg);

    private static byte[] CreateStripedImage(
        int width,
        int height,
        bool vertical,
        int firstBoundary,
        int secondBoundary,
        SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var position = vertical ? x : y;
                bitmap.SetPixel(
                    x,
                    y,
                    position < firstBoundary
                        ? SKColors.Red
                        : position < secondBoundary
                            ? SKColors.Lime
                            : SKColors.Blue);
            }
        }

        return EncodeImage(bitmap, format);
    }

    private static byte[] EncodePng(SKBitmap bitmap)
        => EncodeImage(bitmap, SKEncodedImageFormat.Png);

    private static byte[] EncodeImage(SKBitmap bitmap, SKEncodedImageFormat format)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 100);
        return data.ToArray();
    }

    private sealed class NonSeekableReadStream : Stream
    {
        private readonly MemoryStream _inner;

        public NonSeekableReadStream(byte[] bytes)
            => _inner = new MemoryStream(bytes, writable: false);

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
            => _inner.Read(buffer, offset, count);

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
