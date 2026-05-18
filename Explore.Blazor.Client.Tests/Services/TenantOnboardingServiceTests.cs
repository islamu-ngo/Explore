using System.Net;
using System.Text;
using System.Text.Json;
using Refit;

namespace Explore.Blazor.Client.Tests.Services;

public class TenantOnboardingServiceTests
{
    private Func<HttpRequestMessage, Task<HttpResponseMessage>> _bffHandler = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    private readonly ILogger<TenantOnboardingService> _logger;
    private readonly TenantOnboardingService _service;

    public TenantOnboardingServiceTests()
    {
        _logger = Substitute.For<ILogger<TenantOnboardingService>>();
        var client = new HttpClient(new MockHttpMessageHandler(request => _bffHandler(request)))
        {
            BaseAddress = new Uri("https://test.local")
        };
        var api = RestService.For<ITenantOnboardingApi>(client);
        _service = new TenantOnboardingService(api, _logger);
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
            IsCurrentUserPlatformAdministrator = false,
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
            PreferredHomePage = "Dashboard"
        };
        SetupBffClient(CreateJsonResponse(expected));

        // Act
        var result = await _service.GetSettingsAsync();

        // Assert
        await Assert.That(result.RequireEventApproval).IsTrue();
        await Assert.That(result.PreferredHomePage).IsEqualTo("Dashboard");
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
        await Assert.That(result.Message).IsEqualTo("Request failed.");
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
        _bffHandler = _ => Task.FromResult(response);
    }

    private void SetupBffClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _bffHandler = handler;
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
