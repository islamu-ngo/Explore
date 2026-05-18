// ABOUTME: Focused tests for image storage record creation client behavior.
// ABOUTME: Verifies DTO construction, success mapping, and ProblemDetails error mapping.

namespace Explore.Blazor.Client.Tests.Services;

public sealed class ImageStorageRecordClientTests
{
    private readonly IEventApiClient _apiClient = Substitute.For<IEventApiClient>();
    private readonly ILogger<ImageStorageRecordClient> _logger = Substitute.For<ILogger<ImageStorageRecordClient>>();

    [Test]
    public async Task CreateRecordFromBytesAsync_BuildsDtoAndMapsSuccessResult()
    {
        CreateStorageObjectDto? capturedDto = null;
        var storageId = Guid.NewGuid();
        _apiClient.CreateStorageObjectAsync(Arg.Do<CreateStorageObjectDto>(dto => capturedDto = dto))
            .Returns(new BaseCommandResponseOfGuid
            {
                Success = true,
                Id = storageId,
                Message = "OK"
            });
        var client = CreateClient();
        var uploadResponse = new ImageUploadResponse
        {
            ObjectKey = "images/test",
            ViewUrl = "https://cdn.example.com/images/test",
        };
        var fileData = new FileUploadData
        {
            Content = [1, 2, 3],
            FileName = "test",
            ContentType = "image/png"
        };

        var result = await client.CreateRecordFromBytesAsync(uploadResponse, fileData);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.StorageObjectId).IsEqualTo(storageId);
        await Assert.That(capturedDto).IsNotNull();
        await Assert.That(capturedDto!.Extension).IsEqualTo(".png");
        await Assert.That(capturedDto.FileTypeId).IsEqualTo(1);
        await Assert.That(capturedDto.Uri).IsEqualTo("https://cdn.example.com/images/test");
    }

    [Test]
    public async Task CreateRecordFromBytesAsync_WhenStorageApiReturnsFailure_MapsMessage()
    {
        _apiClient.CreateStorageObjectAsync(Arg.Any<CreateStorageObjectDto>())
            .Returns(new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "record rejected"
            });
        var client = CreateClient();

        var result = await client.CreateRecordFromBytesAsync(CreateUploadResponse(), CreateFileData());

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
        await Assert.That(result.ErrorMessage).IsEqualTo("record rejected");
    }

    [Test]
    public async Task CreateRecordFromBytesAsync_WithProblemDetails_ReturnsProblemMessage()
    {
        var problemDetails = new ProblemDetails
        {
            Title = "Invalid storage object",
            Detail = "File type is not allowed"
        };
        _apiClient.CreateStorageObjectAsync(Arg.Any<CreateStorageObjectDto>())
            .ThrowsAsync(new ApiException<ProblemDetails>(
                "Bad Request",
                400,
                string.Empty,
                new Dictionary<string, IEnumerable<string>>(),
                problemDetails,
                null));
        var client = CreateClient();

        var result = await client.CreateRecordFromBytesAsync(CreateUploadResponse(), CreateFileData());

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
        await Assert.That(result.ErrorMessage).Contains("API call failed (400)");
        await Assert.That(result.ErrorMessage).Contains("Invalid storage object");
        await Assert.That(result.ErrorMessage).Contains("File type is not allowed");
    }

    [Test]
    public async Task CreateRecordFromBytesAsync_WithMissingUri_ReturnsMetadataFailure()
    {
        var uploadResponse = new ImageUploadResponse();

        var result = await CreateClient().CreateRecordFromBytesAsync(uploadResponse, CreateFileData());

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
        await Assert.That(result.ErrorMessage).IsEqualTo("Failed to build storage metadata for uploaded image.");
        await _apiClient.DidNotReceive().CreateStorageObjectAsync(Arg.Any<CreateStorageObjectDto>());
    }

    private ImageStorageRecordClient CreateClient()
    {
        return new ImageStorageRecordClient(_apiClient, new ImageContentClassifier(), _logger);
    }

    private static ImageUploadResponse CreateUploadResponse()
    {
        return new ImageUploadResponse
        {
            ObjectKey = "images/test.jpg",
            ViewUrl = "https://cdn.example.com/images/test.jpg"
        };
    }

    private static FileUploadData CreateFileData()
    {
        return new FileUploadData
        {
            Content = [1, 2, 3],
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };
    }
}
