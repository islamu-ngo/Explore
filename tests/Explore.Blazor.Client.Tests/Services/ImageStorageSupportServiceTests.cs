// ABOUTME: Focused tests for ImageStorageService support seams extracted during Phase 3.
// ABOUTME: Covers file reading, preview generation, content-type classification, and URL resolution behavior.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Explore.Blazor.Client.Tests.Services;

public class ImageStorageSupportServiceTests
{
    [Test]
    public async Task ImageFileReaderService_ReadFileAsync_ReturnsFileData_WhenFileIsValid()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var file = Substitute.For<IBrowserFile>();
        file.Name.Returns("test.jpg");
        file.Size.Returns(3L);
        file.ContentType.Returns("image/jpeg");
        file.OpenReadStream(Arg.Any<long>()).Returns(_ => new MemoryStream(bytes));

        var service = new ImageFileReaderService(NullLogger<ImageFileReaderService>.Instance);

        var result = await service.ReadFileAsync(file, 1024);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.FileName).IsEqualTo("test.jpg");
        await Assert.That(result.ContentType).IsEqualTo("image/jpeg");
        await Assert.That(result.Content.SequenceEqual(bytes)).IsTrue();
    }

    [Test]
    public async Task ImageFileReaderService_ReadFileAsync_SanitizesDangerousBrowserFileNameAndDoesNotLogIt()
    {
        var dangerousFileName = @"..\..\secret<script>.png";
        var bytes = new byte[] { 1, 2, 3 };
        var file = Substitute.For<IBrowserFile>();
        file.Name.Returns(dangerousFileName);
        file.Size.Returns(3L);
        file.ContentType.Returns("image/png");
        file.OpenReadStream(Arg.Any<long>()).Returns(_ => new MemoryStream(bytes));
        var logger = Substitute.For<ILogger<ImageFileReaderService>>();
        var service = new ImageFileReaderService(logger);

        var result = await service.ReadFileAsync(file, 1024);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.FileName).IsEqualTo("secret-script.png");
        logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Is<object>(state => LogStateContains(state, dangerousFileName)),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task ImageUploadClientPolicy_BuildSafeFileName_DerivesAllowedExtensionFromContentType()
    {
        var result = ImageUploadClientPolicy.BuildSafeFileName("../avatar.svg", "image/webp");

        await Assert.That(result).IsEqualTo("avatar.webp");
    }

    [Test]
    public async Task ImageFileReaderService_ReadFileAsync_ReturnsNull_WhenFileExceedsMaxSize()
    {
        var file = Substitute.For<IBrowserFile>();
        file.Name.Returns("big.jpg");
        file.Size.Returns(2048L);
        file.ContentType.Returns("image/jpeg");

        var service = new ImageFileReaderService(NullLogger<ImageFileReaderService>.Instance);

        var result = await service.ReadFileAsync(file, 1024);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ImagePreviewService_GenerateLocalPreviewFromBytes_ReturnsDataUri()
    {
        var service = new ImagePreviewService(NullLogger<ImagePreviewService>.Instance);
        var fileData = new FileUploadData
        {
            Content = new byte[] { 1, 2, 3 },
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };

        var result = service.GenerateLocalPreviewFromBytes(fileData);

        await Assert.That(result).IsEqualTo("data:image/jpeg;base64,AQID");
    }

    [Test]
    public async Task ImagePreviewService_GenerateLocalPreviewFromBytes_ReturnsEmpty_WhenFileDataIsNull()
    {
        var service = new ImagePreviewService(NullLogger<ImagePreviewService>.Instance);

        var result = service.GenerateLocalPreviewFromBytes(null!);

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ImageContentClassifier_GetDefaultExtension_MapsKnownImageTypes()
    {
        var classifier = new ImageContentClassifier();

        await Assert.That(classifier.GetDefaultExtension("image/jpeg")).IsEqualTo(".jpg");
        await Assert.That(classifier.GetDefaultExtension("image/png")).IsEqualTo(".png");
        await Assert.That(classifier.GetDefaultExtension("application/octet-stream")).IsEqualTo(".bin");
    }

    [Test]
    public async Task ImageContentClassifier_GetFileTypeId_ClassifiesContentFamily()
    {
        var classifier = new ImageContentClassifier();

        await Assert.That(classifier.GetFileTypeId("image/png")).IsEqualTo(1);
        await Assert.That(classifier.GetFileTypeId("application/pdf")).IsEqualTo(2);
        await Assert.That(classifier.GetFileTypeId("video/mp4")).IsEqualTo(3);
        await Assert.That(classifier.GetFileTypeId("audio/mpeg")).IsEqualTo(4);
        await Assert.That(classifier.GetFileTypeId("application/octet-stream")).IsEqualTo(5);
    }

    [Test]
    public async Task StorageObjectUrlResolver_ResolvePublicImageUrl_ReturnsStableMetadataUrl_WhenGuidProvided()
    {
        var storageObjectId = Guid.NewGuid();
        var resolver = new StorageObjectUrlResolver();

        var result = resolver.ResolvePublicImageUrl(storageObjectId.ToString());

        await Assert.That(result).IsEqualTo($"/api/storageobject/{storageObjectId}/public");
    }

    [Test]
    public async Task StorageObjectUrlResolver_ResolvePublicImageUrl_PreservesExistingStorageApiPath()
    {
        var resolver = new StorageObjectUrlResolver();

        var result = resolver.ResolvePublicImageUrl("/api/storageobject/00000000-0000-0000-0000-000000000001/public");

        await Assert.That(result).IsEqualTo("/api/storageobject/00000000-0000-0000-0000-000000000001/public");
    }

    [Test]
    public async Task StorageObjectUrlResolver_ResolvePublicImageUrl_RejectsProviderObjectKeys()
    {
        var resolver = new StorageObjectUrlResolver();

        var result = resolver.ResolvePublicImageUrl("tenant/files/raw-object-key.jpg");

        await Assert.That(result).IsNull();
    }

    private static bool LogStateContains(object? state, string value)
    {
        return state.ToString()?.Contains(value, StringComparison.Ordinal) == true;
    }
}
