// ABOUTME: Unit tests for shared storage presentation URL resolution.
// ABOUTME: Verifies projection helpers sign only safe object keys and leave URI-shaped values unsigned.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Event.Application.UnitTests.Services;

public sealed class StoragePresentationUrlResolverTests
{
    private readonly IObjectStorageService _objectStorageService = Substitute.For<IObjectStorageService>();

    public StoragePresentationUrlResolverTests()
    {
        _objectStorageService
            .GeneratePresignedDownloadUrl(Arg.Any<string>(), Arg.Any<int>())
            .Returns("https://storage.example.test/presigned");
    }

    [Test]
    public async Task ResolveImageUrlAsync_WithSafeObjectKey_GeneratesPresignedUrl()
    {
        var result = await Resolve("tenants/example/object.png");

        await Assert.That(result).IsEqualTo("https://storage.example.test/presigned");
        await _objectStorageService.Received(1).GeneratePresignedDownloadUrl(
            "tenants/example/object.png",
            60);
    }

    [Test]
    public async Task ResolveImageUrlAsync_WithExternalUrl_ReturnsUrlWithoutSigning()
    {
        var result = await Resolve("https://cdn.example.test/images/object.png");

        await Assert.That(result).IsEqualTo("https://cdn.example.test/images/object.png");
        await _objectStorageService.DidNotReceive().GeneratePresignedDownloadUrl(
            Arg.Any<string>(),
            Arg.Any<int>());
    }

    [Test]
    public async Task ResolveImageUrlAsync_WithLocalStorageApiPath_ReturnsPathWithoutSigning()
    {
        var path = $"/api/storageobject/{Guid.CreateVersion7()}/content";

        var result = await Resolve(path);

        await Assert.That(result).IsEqualTo(path);
        await _objectStorageService.DidNotReceive().GeneratePresignedDownloadUrl(
            Arg.Any<string>(),
            Arg.Any<int>());
    }

    [Test]
    public async Task ResolveImageUrlAsync_WithUnsafeRelativeReference_ReturnsNullWithoutSigning()
    {
        var result = await Resolve("../secret/object.png");

        await Assert.That(result).IsNull();
        await _objectStorageService.DidNotReceive().GeneratePresignedDownloadUrl(
            Arg.Any<string>(),
            Arg.Any<int>());
    }

    [Test]
    public async Task ResolveImageUrlAsync_WhenSigningFails_ReturnsNullWithoutLeakingReference()
    {
        _objectStorageService
            .GeneratePresignedDownloadUrl(Arg.Any<string>(), Arg.Any<int>())
            .Returns<Task<string>>(_ => throw new InvalidOperationException("provider failed"));

        var result = await Resolve("tenants/example/object.png");

        await Assert.That(result).IsNull();
    }

    private Task<string?> Resolve(string? value)
        => StoragePresentationUrlResolver.ResolveImageUrlAsync(
            value,
            _objectStorageService,
            NullLogger.Instance,
            "test image");
}
