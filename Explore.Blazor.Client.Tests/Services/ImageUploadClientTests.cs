// ABOUTME: Focused tests for the image upload transport client seam.
// ABOUTME: Verifies BFF upload-session fallback and isolated raw upload HTTP behavior.

using System.Net;
using Explore.Blazor.Client.Services.Http;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class ImageUploadClientTests
{
    private readonly IEventApiClient _apiClient = Substitute.For<IEventApiClient>();
    private readonly System.Net.Http.IHttpClientFactory _httpClientFactory = Substitute.For<System.Net.Http.IHttpClientFactory>();
    private readonly ILogger<ImageUploadClient> _logger = Substitute.For<ILogger<ImageUploadClient>>();

    [Test]
    public async Task GetUploadUrlAsync_WhenBffSessionUnavailable_FallsBackToGeneratedApiClient()
    {
        _apiClient.GenerateStorageObjectUploadUrlAsync(Arg.Any<UploadRequestDto>())
            .Returns(new UploadUrlResponseDto
            {
                UploadUrl = "https://upload.example.com/object",
                ObjectKey = "images/test.jpg",
                ViewUrl = "https://cdn.example.com/images/test.jpg",
                ExpiresInMinutes = 30
            });

        var client = new ImageUploadClient(
            _apiClient,
            _httpClientFactory,
            _logger,
            apiClientExecutor: new FailingExecutor());

        var result = await client.GetUploadUrlAsync("test.jpg", "image/jpeg");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.UploadUrl).IsEqualTo("https://upload.example.com/object");
        await Assert.That(result.ObjectKey).IsEqualTo("images/test.jpg");
        await Assert.That(result.ViewUrl).IsEqualTo("https://cdn.example.com/images/test.jpg");
    }

    [Test]
    public async Task GetUploadUrlAsync_WhenBffSessionSucceedsOutsideBrowser_StillUsesDirectUploadUrl()
    {
        _apiClient.GenerateStorageObjectUploadUrlAsync(Arg.Any<UploadRequestDto>())
            .Returns(new UploadUrlResponseDto
            {
                UploadUrl = "https://upload.example.com/direct-object",
                ObjectKey = "images/direct.jpg",
                ViewUrl = "https://cdn.example.com/images/direct.jpg",
                ExpiresInMinutes = 45
            });

        var client = new ImageUploadClient(
            _apiClient,
            _httpClientFactory,
            _logger,
            apiClientExecutor: new SuccessfulSessionExecutor());

        var result = await client.GetUploadUrlAsync("direct.jpg", "image/jpeg");

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.UploadSessionId).IsEmpty();
        await Assert.That(result.UploadUrl).IsEqualTo("https://upload.example.com/direct-object");
        await Assert.That(result.ObjectKey).IsEqualTo("images/direct.jpg");
    }

    [Test]
    public async Task UploadImageFromBytesAsync_UsesS3UploadClientAndReturnsTrue_WhenUploadSucceeds()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        _httpClientFactory.CreateClient("S3Upload").Returns(new HttpClient(handler));
        var client = CreateClient();
        var fileData = new FileUploadData
        {
            Content = [1, 2, 3],
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };

        var result = await client.UploadImageFromBytesAsync("https://upload.example.com/object", fileData);

        await Assert.That(result).IsTrue();
        await Assert.That(handler.LastRequest?.Method).IsEqualTo(HttpMethod.Put);
        await Assert.That(handler.LastRequest?.RequestUri?.AbsoluteUri).IsEqualTo("https://upload.example.com/object");
    }

    [Test]
    public async Task UploadViaBffProxyAsync_WithBytes_PostsMultipartToUploadProxy()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        _httpClientFactory.CreateClient("BffClient").Returns(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://bff.test")
        });
        var client = CreateClient();
        var fileData = new FileUploadData
        {
            Content = [1, 2, 3],
            FileName = "test.jpg",
            ContentType = "image/jpeg"
        };

        var result = await client.UploadViaBffProxyAsync("session-1", fileData);

        await Assert.That(result).IsTrue();
        await Assert.That(handler.LastRequest?.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(handler.LastRequest?.RequestUri?.AbsolutePath).IsEqualTo("/bff/storage/upload-proxy");
    }

    private ImageUploadClient CreateClient()
    {
        return new ImageUploadClient(_apiClient, _httpClientFactory, _logger, apiClientExecutor: new FailingExecutor());
    }

    private sealed class FailingExecutor : IApiClientExecutor
    {
        public Task<ApiResult<T>> ReadJsonAsync<T>(
            Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
            string serviceName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ApiResult<T>.Failure(new InvalidOperationException("BFF unavailable")));
        }

        public Task<ApiResult> SendAsync(
            Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
            string serviceName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ApiResult.Failure(new InvalidOperationException("BFF unavailable")));
        }
    }

    private sealed class SuccessfulSessionExecutor : IApiClientExecutor
    {
        public Task<ApiResult<T>> ReadJsonAsync<T>(
            Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
            string serviceName,
            CancellationToken cancellationToken = default)
        {
            if (typeof(T) == typeof(BffStorageUploadSessionResponse))
            {
                var response = new BffStorageUploadSessionResponse
                {
                    UploadSessionId = "session-1",
                    ObjectKey = "images/session.jpg",
                    ViewUrl = "https://cdn.example.com/images/session.jpg",
                    ExpiresInMinutes = 60
                };

                return Task.FromResult(ApiResult<T>.Success((T)(object)response));
            }

            return Task.FromResult(ApiResult<T>.Failure(new InvalidOperationException("Unexpected executor request")));
        }

        public Task<ApiResult> SendAsync(
            Func<CancellationToken, Task<HttpResponseMessage>> sendAsync,
            string serviceName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ApiResult.Failure(new InvalidOperationException("Unexpected executor request")));
        }
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(responseFactory(request));
        }
    }
}
