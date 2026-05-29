// ABOUTME: Integration tests for BFF storage upload proxy destination binding.
// ABOUTME: Proves browser-supplied upload destinations are rejected unless bound to a server-issued session.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Explore.Blazor.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class BffStorageUploadProxyTests : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly StorageApiHandler _apiHandler = new();
    private readonly string _authHeader = TestAuthHandler.CreateAuthHeaderValue(
        Guid.NewGuid(),
        "Storage Tester",
        (ClaimTypes.Name, "Storage Tester"));
    private readonly string _otherAuthHeader = TestAuthHandler.CreateAuthHeaderValue(
        Guid.NewGuid(),
        "Other Storage Tester",
        (ClaimTypes.Name, "Other Storage Tester"));

    public BffStorageUploadProxyTests()
    {
        var antiforgery = Substitute.For<IAntiforgery>();
        antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);

        _factory = new BlazorBffWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAntiforgery>();
                services.AddSingleton(antiforgery);
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StorageHttpClientFactory(_apiHandler));
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Test]
    public async Task UploadProxy_WithArbitraryPresignedLookingHttpsUrl_ReturnsBadRequest()
    {
        using var request = CreateUploadRequest();

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Raw upload destinations are not accepted");
        _apiHandler.FinalizeCallCount.Should().Be(0);
    }

    [Test]
    public async Task UploadSession_WithPathSegmentFileName_ReturnsBadRequestWithoutCallingApi()
    {
        using var request = CreateUploadSessionRequest("../probe.png", "image/png");

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("simple file name");
        _apiHandler.CallCount.Should().Be(0);
    }

    [Test]
    public async Task UploadSession_WithInvalidContentType_ReturnsBadRequestWithoutCallingApi()
    {
        using var request = CreateUploadSessionRequest("probe.png", "not-a-mime-type");

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("valid MIME type");
        _apiHandler.CallCount.Should().Be(0);
    }

    [Test]
    public async Task UploadProxy_WithContentTypeMismatch_ReturnsBadRequestWithoutUploading()
    {
        var uploadSessionId = await IssueUploadSessionAsync();
        using var request = CreateUploadProxyRequest(uploadSessionId, "image/png", "image/jpeg");

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("content type must match");
        _apiHandler.FinalizeCallCount.Should().Be(0);
    }

    [Test]
    public async Task UploadProxy_WithDifferentUserSession_ReturnsBadRequestWithoutUploading()
    {
        var uploadSessionId = await IssueUploadSessionAsync();
        using var request = CreateUploadProxyRequest(uploadSessionId, "image/png", "image/png", _otherAuthHeader);

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("server-issued upload session");
        _apiHandler.FinalizeCallCount.Should().Be(0);
    }

    [Test]
    public async Task UploadProxy_WithConsumedSession_CannotReuseSession()
    {
        var uploadSessionId = await IssueUploadSessionAsync();

        using var firstRequest = CreateUploadProxyRequest(uploadSessionId, "image/png", "image/png");
        using var firstResponse = await _client.SendAsync(firstRequest);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _apiHandler.FinalizeCallCount.Should().Be(1);
        var firstBody = await firstResponse.Content.ReadAsStringAsync();
        firstBody.Should().Contain("storageObjectId");

        using var secondRequest = CreateUploadProxyRequest(uploadSessionId, "image/png", "image/png");
        using var secondResponse = await _client.SendAsync(secondRequest);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await secondResponse.Content.ReadAsStringAsync();
        body.Should().Contain("server-issued upload session");
        _apiHandler.FinalizeCallCount.Should().Be(1);
    }

    [Test]
    public async Task UploadSession_WithExpectedSize_CallsProviderNeutralApiSessionEndpoint()
    {
        using var request = CreateUploadSessionRequest("probe.png", "image/png");

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _apiHandler.ReserveCallCount.Should().Be(1);
        _apiHandler.CapturedReserveRequestBody.Should().Contain("\"expectedSizeBytes\":4");
        _apiHandler.CapturedReserveRequestBody.Should().NotBeNull();
        _apiHandler.CapturedReserveRequestBody!.ToLowerInvariant().Should().NotContain("uploadurl");
        _apiHandler.CapturedReserveRequestBody.ToLowerInvariant().Should().NotContain("objectkey");
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private HttpRequestMessage CreateUploadRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/bff/storage/upload-proxy");
        AddBffHeaders(request);

        var uploadUrl = "https://127.0.0.1:1/bucket/key?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Signature=fake";
        var form = new MultipartFormDataContent();
        var uploadUrlContent = new StringContent(uploadUrl);
        form.Add(uploadUrlContent, "uploadUrl");
        var contentTypeContent = new StringContent("image/png");
        form.Add(contentTypeContent, "contentType");

        var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "probe.png");
        request.Content = form;

        return request;
    }

    private async Task<string> IssueUploadSessionAsync()
    {
        using var request = CreateUploadSessionRequest("probe.png", "image/png");
        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("uploadSessionId").GetString()!;
    }

    private HttpRequestMessage CreateUploadSessionRequest(string fileName, string contentType)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/bff/storage/upload-session");
        AddBffHeaders(request);
        request.Content = JsonContent.Create(new
        {
            fileName,
            contentType,
            expectedSizeBytes = 4L
        });

        return request;
    }

    private HttpRequestMessage CreateUploadProxyRequest(
        string uploadSessionId,
        string declaredContentType,
        string fileContentType,
        string? authHeader = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/bff/storage/upload-proxy");
        AddBffHeaders(request, authHeader);

        var form = new MultipartFormDataContent();
        form.Add(new StringContent(uploadSessionId), "uploadSessionId");
        form.Add(new StringContent(declaredContentType), "contentType");

        var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(fileContentType);
        form.Add(fileContent, "file", "probe.png");
        request.Content = form;

        return request;
    }

    private void AddBffHeaders(HttpRequestMessage request, string? authHeader = null)
    {
        request.Headers.Add("X-CSRF-TOKEN", "test-token");
        request.Headers.Add(TestAuthHandler.AuthHeaderName, authHeader ?? _authHeader);
    }

    private sealed class StorageHttpClientFactory(
        StorageApiHandler apiHandler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(apiHandler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://api.example.test/")
            };
        }
    }

    private sealed class StorageApiHandler : HttpMessageHandler
    {
        private readonly Guid _uploadSessionId = Guid.CreateVersion7();
        private readonly Guid _storageObjectId = Guid.CreateVersion7();
        public int CallCount { get; private set; }
        public int ReserveCallCount { get; private set; }
        public int FinalizeCallCount { get; private set; }
        public string? CapturedReserveRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;

            if (request.Method == HttpMethod.Post &&
                string.Equals(request.RequestUri?.AbsolutePath, "/api/storageobject/upload-sessions", StringComparison.Ordinal))
            {
                ReserveCallCount++;
                CapturedReserveRequestBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);

                return JsonResponse(CreateSessionResponse(_uploadSessionId, null, "reserved"));
            }

            if (request.Method == HttpMethod.Put &&
                string.Equals(request.RequestUri?.AbsolutePath, $"/api/storageobject/upload-sessions/{_uploadSessionId}/content", StringComparison.Ordinal))
            {
                FinalizeCallCount++;
                return JsonResponse(CreateSessionResponse(_uploadSessionId, _storageObjectId, "finalized"));
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        private static HttpResponseMessage JsonResponse(string responseJson) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };

        private static string CreateSessionResponse(Guid uploadSessionId, Guid? storageObjectId, string status)
        {
            var storageObjectProperty = storageObjectId.HasValue
                ? $"\"storageObjectId\": \"{storageObjectId.Value}\","
                : string.Empty;

            return $$"""
                {
                  "id": {
                    "id": "{{uploadSessionId}}",
                    "tenantId": "{{Guid.CreateVersion7()}}",
                    "provider": "local",
                    "expectedSizeBytes": 4,
                    "reservedBytes": 4,
                    "contentType": "image/png",
                    "safeDisplayName": "probe.png",
                    "purpose": "legacy_image",
                    "visibility": "public_image",
                    "status": "{{status}}",
                    {{storageObjectProperty}}
                    "expiresAt": "{{DateTime.UtcNow.AddMinutes(15):O}}",
                    "maxUploadBytes": 10485760,
                    "tenantQuotaBytes": 1073741824,
                    "usedBytes": 0,
                    "totalReservedBytes": 4
                  },
                  "success": true,
                  "message": "ok"
                }
                """;
        }
    }
}
