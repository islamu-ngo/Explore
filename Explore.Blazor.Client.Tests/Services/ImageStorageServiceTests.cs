using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace Explore.Blazor.Client.Tests.Services;

public class ImageStorageServiceTests
{
    private readonly IEventApiClient _apiClient;
    private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImageStorageService> _logger;
    private readonly ImageStorageService _service;

    public ImageStorageServiceTests()
    {
        _apiClient = Substitute.For<IEventApiClient>();
        _httpClientFactory = Substitute.For<System.Net.Http.IHttpClientFactory>();
        _logger = Substitute.For<ILogger<ImageStorageService>>();
        _service = new ImageStorageService(_apiClient, _httpClientFactory, _logger);
    }

    #region ReadFileAsync

    [Test]
    public async Task ReadFileAsync_ReturnsFileData_WhenFileIsValid()
    {
        // Arrange
        var bytes = new byte[] { 1, 2, 3 };
        var file = Substitute.For<IBrowserFile>();
        file.Name.Returns("test.jpg");
        file.Size.Returns(3L);
        file.ContentType.Returns("image/jpeg");
        file.OpenReadStream(Arg.Any<long>()).Returns(_ => new MemoryStream(bytes));

        // Act
        var result = await _service.ReadFileAsync(file, 1024);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.FileName).IsEqualTo("test.jpg");
        await Assert.That(result.ContentType).IsEqualTo("image/jpeg");
        await Assert.That(result.Content.SequenceEqual(bytes)).IsTrue();
    }

    [Test]
    public async Task ReadFileAsync_ReturnsNull_WhenFileIsNull()
    {
        // Act
        var result = await _service.ReadFileAsync(null!, 1024);

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ReadFileAsync_ReturnsNull_WhenFileExceedsMaxSize()
    {
        // Arrange
        var file = Substitute.For<IBrowserFile>();
        file.Name.Returns("big.jpg");
        file.Size.Returns(2048L);
        file.ContentType.Returns("image/jpeg");

        // Act
        var result = await _service.ReadFileAsync(file, 1024);

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    #region GetUploadUrlAsync

    [Test]
    public async Task GetUploadUrlAsync_ReturnsResponse_WhenApiSucceeds()
    {
        // Arrange
        _apiClient.GenerateUploadUrlAsync(Arg.Any<UploadRequestDto>())
            .Returns(new UploadUrlResponseDto
            {
                UploadUrl = "https://upload.example.com/object",
                ObjectKey = "images/test.jpg",
                ViewUrl = "https://cdn.example.com/images/test.jpg",
                ExpiresInMinutes = 30
            });

        // Act
        var result = await _service.GetUploadUrlAsync("test.jpg", "image/jpeg");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.UploadUrl).IsEqualTo("https://upload.example.com/object");
        await Assert.That(result.ObjectKey).IsEqualTo("images/test.jpg");
        await Assert.That(result.ViewUrl).IsEqualTo("https://cdn.example.com/images/test.jpg");
        await Assert.That(result.ExpiresInMinutes).IsEqualTo(30);
    }

    [Test]
    public async Task GetUploadUrlAsync_ReturnsNull_WhenApiReturnsNull()
    {
        // Arrange
        _apiClient.GenerateUploadUrlAsync(Arg.Any<UploadRequestDto>())
            .Returns((UploadUrlResponseDto?)null);

        // Act
        var result = await _service.GetUploadUrlAsync("test.jpg", "image/jpeg");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetUploadUrlAsync_ReturnsNull_WhenApiThrows()
    {
        // Arrange
        _apiClient.GenerateUploadUrlAsync(Arg.Any<UploadRequestDto>())
            .ThrowsAsync(new ApiException("Error", 500, null, null, null));

        // Act
        var result = await _service.GetUploadUrlAsync("test.jpg", "image/jpeg");

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    #region UploadImageFromBytesAsync

    [Test]
    public async Task UploadImageFromBytesAsync_ReturnsTrue_WhenUploadSucceeds()
    {
        // Arrange
        _httpClientFactory.CreateClient("S3Upload")
            .Returns(CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK)));

        var fileData = new FileUploadData
        {
            Content = new byte[] { 1, 2, 3 },
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };

        // Act
        var result = await _service.UploadImageFromBytesAsync("https://upload.example.com/object", fileData);

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task UploadImageFromBytesAsync_ReturnsFalse_WhenUploadFails()
    {
        // Arrange
        _httpClientFactory.CreateClient("S3Upload")
            .Returns(CreateHttpClient(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var fileData = new FileUploadData
        {
            Content = new byte[] { 1, 2, 3 },
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };

        // Act
        var result = await _service.UploadImageFromBytesAsync("https://upload.example.com/object", fileData);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task UploadImageFromBytesAsync_ReturnsFalse_WhenUrlIsEmpty()
    {
        // Arrange
        var fileData = new FileUploadData
        {
            Content = new byte[] { 1, 2, 3 },
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };

        // Act
        var result = await _service.UploadImageFromBytesAsync(string.Empty, fileData);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task UploadImageFromBytesAsync_ReturnsFalse_WhenFileDataIsNull()
    {
        // Act
        var result = await _service.UploadImageFromBytesAsync("https://upload.example.com/object", null!);

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion

    #region UploadAndCreateRecordFromBytesAsync

    [Test]
    public async Task UploadAndCreateRecordFromBytesAsync_ReturnsSuccess_WhenFullFlowSucceeds()
    {
        // Arrange
        _apiClient.GenerateUploadUrlAsync(Arg.Any<UploadRequestDto>())
            .Returns(new UploadUrlResponseDto
            {
                UploadUrl = "https://upload.example.com/object",
                ObjectKey = "images/test.jpg",
                ViewUrl = "https://cdn.example.com/images/test.jpg",
                ExpiresInMinutes = 30
            });

        _httpClientFactory.CreateClient("S3Upload")
            .Returns(CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK)));

        var storageId = Guid.NewGuid();
        _apiClient.StorageobjectPOSTAsync(Arg.Any<CreateStorageObjectDto>())
            .Returns(new BaseCommandResponseOfGuid
            {
                Success = true,
                Id = storageId,
                Message = "OK"
            });

        var fileData = new FileUploadData
        {
            Content = new byte[] { 10, 20, 30 },
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };

        // Act
        var result = await _service.UploadAndCreateRecordFromBytesAsync(fileData);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.StorageObjectId).IsEqualTo(storageId);
        await Assert.That(result.ObjectKey).IsEqualTo("images/test.jpg");
    }

    [Test]
    public async Task UploadAndCreateRecordFromBytesAsync_ReturnsFailure_WhenUploadUrlFails()
    {
        // Arrange
        _apiClient.GenerateUploadUrlAsync(Arg.Any<UploadRequestDto>())
            .Returns((UploadUrlResponseDto?)null);

        var fileData = new FileUploadData
        {
            Content = new byte[] { 10, 20, 30 },
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };

        // Act
        var result = await _service.UploadAndCreateRecordFromBytesAsync(fileData);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
        await Assert.That(result.ErrorMessage).Contains("Failed to get pre-signed URL");
    }

    #endregion

    #region GetImageUrlAsync

    [Test]
    public async Task GetImageUrlAsync_ReturnsUrl_WhenApiSucceeds()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new PresignedDownloadUrlResponse
            {
                PresignedUrl = "https://cdn.example.com/presigned/test.jpg",
                ObjectKey = "images/test.jpg",
                ExpiresInMinutes = 60
            })
        };

        _httpClientFactory.CreateClient("BffClient").Returns(CreateHttpClient(response));

        // Act
        var result = await _service.GetImageUrlAsync("images/test.jpg");

        // Assert
        await Assert.That(result).IsEqualTo("https://cdn.example.com/presigned/test.jpg");
    }

    [Test]
    public async Task GetImageUrlAsync_ReturnsNull_WhenKeyIsEmpty()
    {
        // Act
        var result = await _service.GetImageUrlAsync(string.Empty);

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    #region GenerateLocalPreviewFromBytes

    [Test]
    public async Task GenerateLocalPreviewFromBytes_ReturnsDataUri_WhenValidData()
    {
        // Arrange
        var fileData = new FileUploadData
        {
            Content = new byte[] { 1, 2, 3 },
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };

        // Act
        var result = _service.GenerateLocalPreviewFromBytes(fileData);

        // Assert
        await Assert.That(result.StartsWith("data:image/jpeg;base64,")).IsTrue();
    }

    [Test]
    public async Task GenerateLocalPreviewFromBytes_ReturnsEmpty_WhenNullData()
    {
        // Act
        var result = _service.GenerateLocalPreviewFromBytes(null!);

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    #endregion

    #region DeleteImageAsync

    [Test]
    public async Task DeleteImageAsync_ReturnsFalse_Always()
    {
        // Act
        var result = await _service.DeleteImageAsync("images/test.jpg");

        // Assert
        await Assert.That(result).IsFalse();
    }

    #endregion

    private static HttpClient CreateHttpClient(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(_ => response);
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test.local")
        };
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
