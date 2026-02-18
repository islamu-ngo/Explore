// ABOUTME: Unit tests for TenantNavigationService HTTP behavior and failure handling.
// ABOUTME: Verifies endpoint contracts, success deserialization, and resilient fallback responses.

using System.Net;
using System.Text;
using System.Text.Json;

namespace Explore.Blazor.Client.Tests.Services;

public class TenantNavigationServiceTests
{
    private readonly ILogger<TenantNavigationService> _logger;

    public TenantNavigationServiceTests()
    {
        _logger = Substitute.For<ILogger<TenantNavigationService>>();
    }

    [Test]
    public async Task GetNavigationLinksAsync_ReturnsLinks_WhenApiSucceeds()
    {
        // Arrange
        var links = new List<TenantNavigationLinkDto> { new(), new() };
        var handler = new RecordingHttpMessageHandler(_ =>
            Task.FromResult(CreateJsonResponse(links)));

        var service = CreateService(handler);

        // Act
        var result = await service.GetNavigationLinksAsync();

        // Assert
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(handler.LastRequest).IsNotNull();
        await Assert.That(handler.LastRequest!.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(handler.LastRequest.RequestUri!.PathAndQuery).IsEqualTo("/api/v1/tenant/navigation");
    }

    [Test]
    public async Task GetNavigationLinksAsync_ReturnsEmpty_WhenApiReturnsFailureStatus()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var service = CreateService(handler);

        // Act
        var result = await service.GetNavigationLinksAsync();

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task CreateNavigationLinkAsync_ReturnsFailureResponse_WhenApiReturnsBadRequest()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "Bad Request"
            }));

        var service = CreateService(handler);

        // Act
        var result = await service.CreateNavigationLinkAsync(new CreateTenantNavigationLinkDto());

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
        await Assert.That(result.Message).Contains("API error");
        await Assert.That(handler.LastRequest).IsNotNull();
        await Assert.That(handler.LastRequest!.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(handler.LastRequest.RequestUri!.PathAndQuery).IsEqualTo("/api/v1/tenant/navigation");
    }

    [Test]
    public async Task UpdateNavigationLinkAsync_SendsPutToIdEndpoint_AndReturnsSuccessBody()
    {
        // Arrange
        var expected = new Explore.Blazor.Client.Models.Responses.BaseCommandResponse<bool>
        {
            Success = true,
            Message = "updated",
            Id = true
        };

        var handler = new RecordingHttpMessageHandler(_ =>
            Task.FromResult(CreateJsonResponse(expected)));

        var service = CreateService(handler);
        var linkId = Guid.NewGuid();

        // Act
        var result = await service.UpdateNavigationLinkAsync(linkId, new UpdateTenantNavigationLinkDto());

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(handler.LastRequest).IsNotNull();
        await Assert.That(handler.LastRequest!.Method).IsEqualTo(HttpMethod.Put);
        await Assert.That(handler.LastRequest.RequestUri!.PathAndQuery).IsEqualTo($"/api/v1/tenant/navigation/{linkId}");
    }

    [Test]
    public async Task DeleteNavigationLinkAsync_ReturnsFailureResponse_WhenHttpThrows()
    {
        // Arrange
        var handler = new RecordingHttpMessageHandler(_ => throw new HttpRequestException("network failed"));
        var service = CreateService(handler);
        var linkId = Guid.NewGuid();

        // Act
        var result = await service.DeleteNavigationLinkAsync(linkId);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
        await Assert.That(result.Message).Contains("Error:");
        await Assert.That(result.Errors).Contains("network failed");
    }

    [Test]
    public async Task ReorderNavigationLinksAsync_SendsPutToReorderEndpoint_AndReturnsSuccessBody()
    {
        // Arrange
        var expected = new Explore.Blazor.Client.Models.Responses.BaseCommandResponse<bool>
        {
            Success = true,
            Message = "reordered",
            Id = true
        };

        var handler = new RecordingHttpMessageHandler(_ =>
            Task.FromResult(CreateJsonResponse(expected)));

        var service = CreateService(handler);

        // Act
        var result = await service.ReorderNavigationLinksAsync([new UpdateTenantNavigationLinkOrderDto()]);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(handler.LastRequest).IsNotNull();
        await Assert.That(handler.LastRequest!.Method).IsEqualTo(HttpMethod.Put);
        await Assert.That(handler.LastRequest.RequestUri!.PathAndQuery).IsEqualTo("/api/v1/tenant/navigation/reorder");
    }

    private TenantNavigationService CreateService(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test.local")
        };

        return new TenantNavigationService(client, _logger);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T model, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(model);
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public HttpRequestMessage? LastRequest { get; private set; }

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return _handler(request);
        }
    }
}
