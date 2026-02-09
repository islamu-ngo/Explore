using System.Net;
using System.Text;
using System.Text.Json;

namespace Explore.Blazor.Client.Tests.Services;

public class TenantOnboardingServiceTests
{
    private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantOnboardingService> _logger;
    private readonly TenantOnboardingService _service;

    public TenantOnboardingServiceTests()
    {
        _httpClientFactory = Substitute.For<System.Net.Http.IHttpClientFactory>();
        _logger = Substitute.For<ILogger<TenantOnboardingService>>();
        _service = new TenantOnboardingService(_httpClientFactory, _logger);
    }

    #region GetStatusAsync

    [Test]
    public async Task GetStatusAsync_ReturnsStatus_WhenApiSucceeds()
    {
        // Arrange
        var expected = new TenantOnboardingStatusModel
        {
            IsCompleted = true,
            IsAuthenticated = true,
            IsCurrentUserTenantAdministrator = true,
            IsCurrentUserInstanceAdministrator = false,
            TenantId = Guid.NewGuid()
        };
        SetupBffClient(CreateJsonResponse(expected));

        // Act
        var result = await _service.GetStatusAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsCompleted).IsTrue();
        await Assert.That(result.TenantId).IsEqualTo(expected.TenantId);
    }

    [Test]
    public async Task GetStatusAsync_ReturnsNull_WhenApiThrows()
    {
        // Arrange
        SetupBffClient(_ => throw new HttpRequestException("boom"));

        // Act
        var result = await _service.GetStatusAsync();

        // Assert
        await Assert.That(result).IsNull();
    }

    #endregion

    #region GetSettingsAsync

    [Test]
    public async Task GetSettingsAsync_ReturnsSettings_WhenApiSucceeds()
    {
        // Arrange
        var expected = new TenantPolicySettingsModel
        {
            AllowUserSubmittedEvents = false,
            RequireEventApproval = true,
            PreferredHomePage = "Dashboard",
            BrandDisplayName = "Tenant Brand"
        };
        SetupBffClient(CreateJsonResponse(expected));

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        await Assert.That(result.RequireEventApproval).IsTrue();
        await Assert.That(result.PreferredHomePage).IsEqualTo("Dashboard");
        await Assert.That(result.BrandDisplayName).IsEqualTo("Tenant Brand");
    }

    [Test]
    public async Task GetSettingsAsync_ReturnsDefaultSettings_WhenApiThrows()
    {
        // Arrange
        SetupBffClient(_ => throw new HttpRequestException("boom"));

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.PreferredHomePage).IsEqualTo("EventList");
        await Assert.That(result.BrandDisplayName).IsEqualTo("ISLAMU Explore");
    }

    #endregion

    #region CompleteAsync

    [Test]
    public async Task CompleteAsync_ReturnsSuccess_WhenApiSucceeds()
    {
        // Arrange
        var commandResponse = new InstanceCommandResponseModel
        {
            Success = true,
            Message = "OK"
        };
        SetupBffClient(CreateJsonResponse(commandResponse));

        // Act
        var result = await _service.CompleteAsync(new TenantPolicySettingsModel());

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("OK");
    }

    [Test]
    public async Task CompleteAsync_ReturnsFailure_WhenApiThrows()
    {
        // Arrange
        SetupBffClient(_ => throw new HttpRequestException("network failed"));

        // Act
        var result = await _service.CompleteAsync(new TenantPolicySettingsModel());

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Request failed.");
        await Assert.That(result.Errors).Contains("network failed");
    }

    #endregion

    #region UpdateSettingsAsync

    [Test]
    public async Task UpdateSettingsAsync_ReturnsSuccess_WhenApiSucceeds()
    {
        // Arrange
        var commandResponse = new InstanceCommandResponseModel
        {
            Success = true,
            Message = "Updated"
        };
        SetupBffClient(CreateJsonResponse(commandResponse));

        // Act
        var result = await _service.UpdateSettingsAsync(new TenantPolicySettingsModel());

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Updated");
    }

    [Test]
    public async Task UpdateSettingsAsync_ReturnsFailure_WhenApiReturnsError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };
        SetupBffClient(response);

        // Act
        var result = await _service.UpdateSettingsAsync(new TenantPolicySettingsModel());

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Operation failed with status 400.");
    }

    #endregion

    private static HttpResponseMessage CreateJsonResponse<T>(T model, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(model);
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private void SetupBffClient(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };
        _httpClientFactory.CreateClient("BffClient").Returns(client);
    }

    private void SetupBffClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        var httpHandler = new MockHttpMessageHandler(handler);
        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("https://test.local") };
        _httpClientFactory.CreateClient("BffClient").Returns(client);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _handler = _ => Task.FromResult(response);
        }

        public MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request);
    }
}
