using System.Net;
using System.Net.Http;
using Refit;

namespace Explore.Blazor.Client.Tests.Services;

public class MapsServiceTests
{
    private readonly ILogger<MapsService> _logger;

    public MapsServiceTests()
    {
        _logger = Substitute.For<ILogger<MapsService>>();
    }

    #region GetEmbedUrlAsync

    [Test]
    public async Task GetEmbedUrlAsync_ReturnsUrl_WhenApiSucceeds()
    {
        // Arrange
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("\"https://maps.example.com/embed\"")
        });

        // Act
        var result = await service.GetEmbedUrlAsync("london mosque");

        // Assert
        await Assert.That(result).IsEqualTo("https://maps.example.com/embed");
    }

    [Test]
    public async Task GetEmbedUrlAsync_ReturnsEmpty_WhenQueryIsEmpty()
    {
        // Arrange
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("\"https://maps.example.com/embed\"")
        });

        // Act
        var result = await service.GetEmbedUrlAsync(string.Empty);

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task GetEmbedUrlAsync_ReturnsEmpty_WhenApiReturnsError()
    {
        // Arrange
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var result = await service.GetEmbedUrlAsync("istanbul");

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task GetEmbedUrlAsync_ReturnsEmpty_WhenHttpThrows()
    {
        // Arrange
        var service = CreateService(_ => throw new HttpRequestException("network failure"));

        // Act
        var result = await service.GetEmbedUrlAsync("makkah");

        // Assert
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    #endregion

    private MapsService CreateService(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var handler = new MockHttpMessageHandler(responseFactory);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test.local")
        };

        var api = RestService.For<IMapsApi>(httpClient);
        return new MapsService(api, _logger);
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
