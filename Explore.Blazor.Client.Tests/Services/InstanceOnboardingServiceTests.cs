using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Refit;

namespace Explore.Blazor.Client.Tests.Services;

public class InstanceOnboardingServiceTests
{
    private readonly System.Net.Http.IHttpClientFactory _httpClientFactory;
    private Func<HttpRequestMessage, Task<HttpResponseMessage>> _bffHandler = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<InstanceOnboardingService> _logger;
    private readonly NavigationManager _navigation;
    private readonly InstanceOnboardingService _service;

    public InstanceOnboardingServiceTests()
    {
        _httpClientFactory = Substitute.For<System.Net.Http.IHttpClientFactory>();
        _jsRuntime = new NullJsRuntime();
        _logger = Substitute.For<ILogger<InstanceOnboardingService>>();
        _navigation = new TestNavigationManager("https://localhost/");
        var client = new HttpClient(new MockHttpMessageHandler(request => _bffHandler(request)))
        {
            BaseAddress = new Uri("https://test.local")
        };
        var api = RestService.For<IInstanceOnboardingApi>(client);
        _service = new InstanceOnboardingService(api, _httpClientFactory, _jsRuntime, _logger, _navigation);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUri) => Initialize(baseUri, baseUri);
    }

    #region GetStatusAsync

    [Test]
    public async Task GetSystemOnboardingStatusAsync_ReturnsStatus_WhenApiSucceeds()
    {
        // Arrange
        var expected = new SystemOnboardingStatusModel
        {
            RequiresOnboarding = true,
            DeploymentMode = "MultiTenant"
        };
        SetupBffClient(CreateJsonResponse(expected));

        // Act
        var result = await _service.GetSystemOnboardingStatusAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.RequiresOnboarding).IsTrue();
        await Assert.That(result.DeploymentMode).IsEqualTo("MultiTenant");
    }

    [Test]
    public async Task GetStatusAsync_ReturnsStatus_WhenApiSucceeds()
    {
        // Arrange
        var expected = new InstanceOnboardingStatusModel
        {
            IsCompleted = true,
            IsAuthenticated = true,
            IsCurrentUserInstanceAdmin = true,
            SelectedDeploymentMode = "SingleTenant"
        };
        SetupBffClient(CreateJsonResponse(expected));

        // Act
        var result = await _service.GetStatusAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsCompleted).IsTrue();
        await Assert.That(result.SelectedDeploymentMode).IsEqualTo("SingleTenant");
    }

    [Test]
    public async Task GetStatusAsync_ReturnsSetupFields_WhenApiSucceeds()
    {
        // Arrange
        var startedAt = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc);
        var expected = new InstanceOnboardingStatusModel
        {
            IsCompleted = false,
            IsSetupModeActive = true,
            SetupSecretFromEnvironment = true,
            SetupTimedOut = false,
            InstanceStartedAt = startedAt
        };
        SetupBffClient(CreateJsonResponse(expected));

        // Act
        var result = await _service.GetStatusAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsSetupModeActive).IsTrue();
        await Assert.That(result.SetupSecretFromEnvironment).IsTrue();
        await Assert.That(result.SetupTimedOut).IsFalse();
        await Assert.That(result.InstanceStartedAt).IsEqualTo(startedAt);
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

    #region GetDeploymentModeAsync

    [Test]
    public async Task GetDeploymentModeAsync_ReturnsMode_WhenApiSucceeds()
    {
        // Arrange
        var expected = new DeploymentModeModel { Mode = "MultiTenant" };
        SetupBffClient(CreateJsonResponse(expected));

        // Act
        var result = await _service.GetDeploymentModeAsync();

        // Assert
        await Assert.That(result.Mode).IsEqualTo("MultiTenant");
    }

    [Test]
    public async Task GetDeploymentModeAsync_ReturnsDefault_WhenApiThrows()
    {
        // Arrange
        SetupBffClient(_ => throw new HttpRequestException("boom"));

        // Act
        var result = await _service.GetDeploymentModeAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Mode).IsEqualTo("SingleTenant");
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
        var result = await _service.CompleteAsync(new OnboardingCompletionModel
        {
            DeploymentMode = "SingleTenant",
            InstanceName = "Test Instance"
        });

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
        var result = await _service.CompleteAsync(new OnboardingCompletionModel());

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Request failed.");
        await Assert.That(result.Errors).Contains("network failed");
    }

    [Test]
    public async Task CompleteAsync_ReturnsSafeFailure_WhenApiReturnsBadRequest()
    {
        // Arrange
        var problemDetails = new
        {
            title = "Validation failed.",
            status = 400,
            errors = new Dictionary<string, string[]>
            {
                ["DeploymentMode"] = new[] { "Invalid deployment mode." }
            }
        };
        SetupBffClient(CreateJsonResponse(problemDetails, HttpStatusCode.BadRequest));

        // Act
        var result = await _service.CompleteAsync(new OnboardingCompletionModel());

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Request failed.");
        await Assert.That(result.Errors).IsNotEmpty();
    }

    #endregion

    #region RefreshAuthSessionAsync

    [Test]
    public async Task RefreshAuthSessionAsync_ReturnsTrue_WhenEndpointSucceeds()
    {
        // Arrange
        SetupBffSelfClient(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await _service.RefreshAuthSessionAsync();

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task RefreshAuthSessionAsync_ReturnsFalse_WhenEndpointFails()
    {
        // Arrange
        SetupBffSelfClient(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        // Act
        var result = await _service.RefreshAuthSessionAsync();

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task RefreshAuthSessionAsync_ReturnsFalse_WhenEndpointThrows()
    {
        // Arrange
        SetupBffSelfClient(_ => throw new HttpRequestException("refresh failed"));

        // Act
        var result = await _service.RefreshAuthSessionAsync();

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task RefreshAuthSessionAsync_UsesInternalEndpoint_ForServerSideSelfCall()
    {
        // Arrange
        Uri? requestUri = null;
        SetupBffSelfClient(request =>
        {
            requestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        // Act
        var result = await _service.RefreshAuthSessionAsync();

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(requestUri).IsNotNull();
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/bff/auth/refresh-session/internal");
    }

    #endregion

    #region AuthorizationProviderAdminAsync

    [Test]
    public async Task GetAuthorizationProviderConfigurationAsAdminAsync_UsesAdminSettingsEndpoint()
    {
        // Arrange
        Uri? requestUri = null;
        var expected = new AuthorizationProviderConfigurationModel
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "cerbosgrpc.local:3593",
            CerbosEndpointVerified = true
        };
        SetupBffClient(request =>
        {
            requestUri = request.RequestUri;
            return Task.FromResult(CreateJsonResponse(expected));
        });

        // Act
        var result = await _service.GetAuthorizationProviderConfigurationAsAdminAsync();

        // Assert
        await Assert.That(requestUri).IsNotNull();
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/api/instance/settings/authz-provider");
        await Assert.That(result.Provider).IsEqualTo("cerbos");
        await Assert.That(result.CerbosEndpointVerified).IsTrue();
    }

    [Test]
    public async Task UpdateAuthorizationProviderConfigurationAsAdminAsync_UsesAdminSettingsEndpoint()
    {
        // Arrange
        Uri? requestUri = null;
        HttpMethod? method = null;
        string? requestBody = null;
        var commandResponse = new InstanceCommandResponseModel { Success = true, Message = "Updated" };
        SetupBffClient(async request =>
        {
            requestUri = request.RequestUri;
            method = request.Method;
            requestBody = await request.Content!.ReadAsStringAsync();
            return CreateJsonResponse(commandResponse);
        });

        // Act
        var result = await _service.UpdateAuthorizationProviderConfigurationAsAdminAsync(new AuthorizationProviderConfigurationModel
        {
            Provider = "cerbos",
            CerbosGrpcEndpoint = "cerbosgrpc.local:3593"
        });

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(requestUri).IsNotNull();
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/api/instance/settings/authz-provider");
        await Assert.That(method).IsEqualTo(HttpMethod.Put);
        await Assert.That(requestBody).Contains("cerbos", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task SyncAuthorizationPolicyPackageAsync_UsesSetupEndpoint()
    {
        // Arrange
        Uri? requestUri = null;
        HttpMethod? method = null;
        var commandResponse = new InstanceCommandResponseModel { Success = true, Message = "Synced" };
        SetupBffClient(request =>
        {
            requestUri = request.RequestUri;
            method = request.Method;
            return Task.FromResult(CreateJsonResponse(commandResponse));
        });

        // Act
        var result = await _service.SyncAuthorizationPolicyPackageAsync();

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(requestUri).IsNotNull();
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/api/InstanceOnboarding/authz-provider-configuration/sync");
        await Assert.That(method).IsEqualTo(HttpMethod.Post);
    }

    [Test]
    public async Task SyncAuthorizationPolicyPackageAsAdminAsync_UsesAdminEndpoint()
    {
        // Arrange
        Uri? requestUri = null;
        HttpMethod? method = null;
        var commandResponse = new InstanceCommandResponseModel { Success = true, Message = "Synced" };
        SetupBffClient(request =>
        {
            requestUri = request.RequestUri;
            method = request.Method;
            return Task.FromResult(CreateJsonResponse(commandResponse));
        });

        // Act
        var result = await _service.SyncAuthorizationPolicyPackageAsAdminAsync();

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(requestUri).IsNotNull();
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/api/instance/settings/authz-provider/sync");
        await Assert.That(method).IsEqualTo(HttpMethod.Post);
    }

    [Test]
    public async Task DownloadAuthorizationPolicyPackageAsync_UsesSetupEndpointAndMapsFile()
    {
        // Arrange
        Uri? requestUri = null;
        HttpMethod? method = null;
        SetupBffClient(request =>
        {
            requestUri = request.RequestUri;
            method = request.Method;
            return Task.FromResult(CreateZipResponse([1, 2, 3], "setup-policy-package.zip"));
        });

        // Act
        var result = await _service.DownloadAuthorizationPolicyPackageAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.FileBytes).IsEquivalentTo(new byte[] { 1, 2, 3 });
        await Assert.That(result.FileName).IsEqualTo("setup-policy-package.zip");
        await Assert.That(result.ContentType).IsEqualTo("application/zip");
        await Assert.That(requestUri).IsNotNull();
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/api/InstanceOnboarding/authz-provider-configuration/package");
        await Assert.That(method).IsEqualTo(HttpMethod.Get);
    }

    [Test]
    public async Task DownloadAuthorizationPolicyPackageAsAdminAsync_UsesAdminEndpointAndMapsFile()
    {
        // Arrange
        Uri? requestUri = null;
        HttpMethod? method = null;
        SetupBffClient(request =>
        {
            requestUri = request.RequestUri;
            method = request.Method;
            return Task.FromResult(CreateZipResponse([4, 5, 6], "admin-policy-package.zip"));
        });

        // Act
        var result = await _service.DownloadAuthorizationPolicyPackageAsAdminAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.FileBytes).IsEquivalentTo(new byte[] { 4, 5, 6 });
        await Assert.That(result.FileName).IsEqualTo("admin-policy-package.zip");
        await Assert.That(result.ContentType).IsEqualTo("application/zip");
        await Assert.That(requestUri).IsNotNull();
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/api/instance/settings/authz-provider/package");
        await Assert.That(method).IsEqualTo(HttpMethod.Get);
    }

    #endregion

    #region ValidateSecretAsync

    [Test]
    public async Task ValidateSecretAsync_ReturnsValid_WhenApiSucceeds()
    {
        // Arrange
        var expected = new SetupSecretValidationResult { Valid = true };
        SetupBffClient(CreateJsonResponse(expected));

        // Act
        var result = await _service.ValidateSecretAsync("test-secret");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Valid).IsTrue();
    }

    [Test]
    public async Task ValidateSecretAsync_ReturnsInvalid_WhenApiReturnsInvalid()
    {
        // Arrange
        var expected = new SetupSecretValidationResult { Valid = false, Error = "Invalid setup secret." };
        SetupBffClient(CreateJsonResponse(expected));

        // Act
        var result = await _service.ValidateSecretAsync("wrong-secret");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Valid).IsFalse();
        await Assert.That(result.Error).IsEqualTo("Invalid setup secret.");
    }

    [Test]
    public async Task ValidateSecretAsync_ReturnsInvalid_WhenApiThrows()
    {
        // Arrange
        SetupBffClient(_ => throw new HttpRequestException("connection refused"));

        // Act
        var result = await _service.ValidateSecretAsync("any-secret");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Valid).IsFalse();
    }

    #endregion

    #region UpdateModuleSettingsAsync

    [Test]
    public async Task UpdateModuleSettingsAsync_ReturnsSuccess_WhenApiSucceeds()
    {
        // Arrange
        var commandResponse = new InstanceCommandResponseModel
        {
            Success = true,
            Message = "Updated"
        };
        SetupBffClient(CreateJsonResponse(commandResponse));

        // Act
        var result = await _service.UpdateModuleSettingsAsync(new ModuleSettingsModel());

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Updated");
    }

    [Test]
    public async Task UpdateModuleSettingsAsync_ReturnsFailure_WhenApiReturnsError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };
        SetupBffClient(response);

        // Act
        var result = await _service.UpdateModuleSettingsAsync(new ModuleSettingsModel());

        // Assert
        await Assert.That(result.Success).IsFalse();
    }

    #endregion

    private static HttpResponseMessage CreateJsonResponse<T>(T model, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(model, options);
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage CreateZipResponse(byte[] content, string fileName)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = $"\"{fileName}\""
        };
        return response;
    }

    private void SetupBffClient(HttpResponseMessage response)
    {
        SetupBffClient(_ => Task.FromResult(response));
    }

    private void SetupBffClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _bffHandler = handler;
        var httpHandler = new MockHttpMessageHandler(handler);
        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("https://test.local") };
        _httpClientFactory.CreateClient("BffClient").Returns(client);
    }

    private void SetupBffSelfClient(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };
        _httpClientFactory.CreateClient("BffSelfClient").Returns(client);
    }

    private void SetupBffSelfClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        var httpHandler = new MockHttpMessageHandler(handler);
        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("https://test.local") };
        _httpClientFactory.CreateClient("BffSelfClient").Returns(client);
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

    private sealed class NullJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }
    }
}
