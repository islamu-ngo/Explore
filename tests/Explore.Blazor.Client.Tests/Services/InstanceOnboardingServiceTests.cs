// ABOUTME: Unit tests for InstanceOnboardingService BFF endpoint mapping and command behavior.
// ABOUTME: Covers onboarding/admin settings, auth provider flows, and storage HAL affordance mapping.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Explore.Blazor.Client.Helpers;
using Microsoft.AspNetCore.Components;
using Refit;

namespace Explore.Blazor.Client.Tests.Services;

public class InstanceOnboardingServiceTests
{
    private Func<HttpRequestMessage, Task<HttpResponseMessage>> _bffHandler = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    private Func<HttpRequestMessage, Task<HttpResponseMessage>> _authHandler = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    private readonly IBffAuthApi _bffAuthApi;
    private readonly ILogger<InstanceOnboardingService> _logger;
    private readonly NavigationManager _navigation;
    private readonly InstanceOnboardingService _service;

    public InstanceOnboardingServiceTests()
    {
        _logger = Substitute.For<ILogger<InstanceOnboardingService>>();
        _navigation = new TestNavigationManager("https://localhost/");
        var client = new HttpClient(new MockHttpMessageHandler(request => _bffHandler(request)))
        {
            BaseAddress = new Uri("https://test.local")
        };
        var authClient = new HttpClient(new MockHttpMessageHandler(request => _authHandler(request)))
        {
            BaseAddress = new Uri("https://test.local")
        };
        var api = new EventApiClient(client);
        _bffAuthApi = RestService.For<IBffAuthApi>(authClient);
        _service = new InstanceOnboardingService(api, _bffAuthApi, _logger, _navigation);
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
        var expected = new SystemOnboardingStatusDto
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
        var expected = new HalResourceOfInstanceOnboardingStatusDto
        {
            IsCompleted = true,
            IsAuthenticated = true,
            IsCurrentUserInstanceAdmin = true,
            SelectedDeploymentMode = "SingleTenant",
            _links = new Dictionary<string, HalLink>
            {
                ["manage-authentication"] = new HalLink { Href = "/api/instance/auth-provider" }
            }
        };
        SetupBffClient(CreateJsonResponse(expected));

        // Act
        var result = await _service.GetStatusAsync();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.IsCompleted).IsTrue();
        await Assert.That(result.SelectedDeploymentMode).IsEqualTo("SingleTenant");
        await Assert.That(result.HasHalLink("manage-authentication")).IsTrue();
    }

    [Test]
    public async Task GetStatusAsync_ReturnsSetupFields_WhenApiSucceeds()
    {
        // Arrange
        var startedAt = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc);
        var expected = new HalResourceOfInstanceOnboardingStatusDto
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
        var expected = new DeploymentModeDto { Mode = DeploymentMode.MultiTenant };
        SetupBffClient(CreateJsonResponse(expected));

        // Act
        var result = await _service.GetDeploymentModeAsync();

        // Assert
        await Assert.That(result.Mode).IsEqualTo(DeploymentMode.MultiTenant);
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
        await Assert.That(result.Mode).IsNull();
    }

    #endregion

    #region CompleteAsync

    [Test]
    public async Task CompleteAsync_ReturnsSuccess_WhenApiSucceeds()
    {
        // Arrange
        var commandResponse = new BaseCommandResponseOfGuid
        {
            Success = true,
            Message = "OK"
        };
        SetupBffClient(CreateJsonResponse(commandResponse));

        // Act
        var result = await _service.CompleteAsync(new CompleteInstanceOnboardingRequest
        {
            DeploymentMode = DeploymentMode.SingleTenant,
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
        var result = await _service.CompleteAsync(new CompleteInstanceOnboardingRequest());

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Request failed.");
        await Assert.That(result.Errors).DoesNotContain("network failed");
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
        var result = await _service.CompleteAsync(new CompleteInstanceOnboardingRequest());

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Failed with status 400.");
        await Assert.That(result.Errors).IsNotEmpty();
    }

    #endregion

    #region KeycloakBootstrapAsync

    [Test]
    public async Task BootstrapKeycloakRealmAsync_UsesSetupEndpointAndRefreshesAuthSchemes_WhenApiSucceeds()
    {
        // Arrange
        Uri? requestUri = null;
        HttpMethod? method = null;
        string? requestBody = null;
        var refreshCalled = false;
        var commandResponse = new BaseCommandResponseOfGuid { Success = true, Message = "Bootstrapped" };
        SetupBffClient(async request =>
        {
            requestUri = request.RequestUri;
            method = request.Method;
            requestBody = await request.Content!.ReadAsStringAsync();
            return CreateJsonResponse(commandResponse);
        });
        SetupBffSelfClient(request =>
        {
            refreshCalled = request.RequestUri?.AbsolutePath == "/bff/auth/refresh-schemes";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        // Act
        var result = await _service.BootstrapKeycloakRealmAsync(CreateKeycloakBootstrapRequest());

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(requestUri).IsNotNull();
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/api/instanceonboarding/auth-provider-configuration/keycloak-bootstrap");
        await Assert.That(method).IsEqualTo(HttpMethod.Post);
        await Assert.That(requestBody).Contains("\"blazorRedirectUris\":[\"https://localhost/*\"]");
        await Assert.That(requestBody).Contains("\"blazorWebOrigins\":[\"\\u002B\"]");
        await Assert.That(refreshCalled).IsTrue();
    }

    [Test]
    public async Task BootstrapKeycloakRealmAsync_DoesNotRefreshAuthSchemes_WhenApiFails()
    {
        // Arrange
        var refreshCalled = false;
        var commandResponse = new BaseCommandResponseOfGuid { Success = false, Message = "Bootstrap failed" };
        SetupBffClient(CreateJsonResponse(commandResponse, HttpStatusCode.BadRequest));
        SetupBffSelfClient(request =>
        {
            refreshCalled = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        // Act
        var result = await _service.BootstrapKeycloakRealmAsync(CreateKeycloakBootstrapRequest());

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(refreshCalled).IsFalse();
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

    #region AuthProviderConfigurationAsync

    [Test]
    public async Task GetAuthProviderConfigurationAsync_UsesPublicRedactedEndpoint()
    {
        // Arrange
        Uri? requestUri = null;
        var expected = new AuthProviderConfigurationDto
        {
            KeycloakEnabled = true,
            KeycloakAuthority = "https://keycloak.example.com/auth/realms/ISLAMU",
            KeycloakClientId = "islamu-event-blazor",
            KeycloakClientSecret = string.Empty,
            KeycloakDetectedFromEnvironment = true
        };
        SetupBffClient(request =>
        {
            requestUri = request.RequestUri;
            return Task.FromResult(CreateJsonResponse(expected));
        });

        // Act
        var result = await _service.GetAuthProviderConfigurationAsync();

        // Assert
        await Assert.That(requestUri).IsNotNull();
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/api/instanceonboarding/auth-provider-configuration");
        await Assert.That(result.KeycloakDetectedFromEnvironment).IsTrue();
        await Assert.That(result.KeycloakClientSecret).IsEmpty();
    }

    [Test]
    public async Task GetAuthProviderConfigurationAsAdminAsync_UsesInstanceAdminEndpoint()
    {
        // Arrange
        Uri? requestUri = null;
        var expected = new AuthProviderConfigurationDto
        {
            KeycloakEnabled = true,
            KeycloakAuthority = "https://keycloak.example.com/realms/ISLAMU",
            KeycloakClientId = "islamu-event-blazor"
        };
        SetupBffClient(request =>
        {
            requestUri = request.RequestUri;
            return Task.FromResult(CreateJsonResponse(expected));
        });

        // Act
        var result = await _service.GetAuthProviderConfigurationAsAdminAsync();

        // Assert
        await Assert.That(requestUri).IsNotNull();
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/api/instance/settings/auth-provider");
        await Assert.That(result.KeycloakClientId).IsEqualTo("islamu-event-blazor");
    }

    #endregion

    #region AuthorizationProviderAdminAsync

    [Test]
    public async Task GetAuthorizationProviderConfigurationAsAdminAsync_UsesAdminSettingsEndpoint()
    {
        // Arrange
        Uri? requestUri = null;
        var expected = new AuthorizationProviderConfigurationDto
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
        var commandResponse = new BaseCommandResponseOfGuid { Success = true, Message = "Updated" };
        SetupBffClient(async request =>
        {
            requestUri = request.RequestUri;
            method = request.Method;
            requestBody = await request.Content!.ReadAsStringAsync();
            return CreateJsonResponse(commandResponse);
        });

        // Act
        var result = await _service.UpdateAuthorizationProviderConfigurationAsAdminAsync(new AuthorizationProviderConfigurationDto
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
        var commandResponse = new BaseCommandResponseOfGuid { Success = true, Message = "Synced" };
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
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/api/instanceonboarding/authz-provider-configuration/sync");
        await Assert.That(method).IsEqualTo(HttpMethod.Post);
    }

    [Test]
    public async Task SyncAuthorizationPolicyPackageAsAdminAsync_UsesAdminEndpoint()
    {
        // Arrange
        Uri? requestUri = null;
        HttpMethod? method = null;
        var commandResponse = new BaseCommandResponseOfGuid { Success = true, Message = "Synced" };
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

    #endregion

    #region ValidateSecretAsync

    [Test]
    public async Task ValidateSecretAsync_ReturnsValid_WhenApiSucceeds()
    {
        // Arrange
        var expected = new SetupSecretValidationResultDto { Valid = true };
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
        var expected = new SetupSecretValidationResultDto { Valid = false };
        SetupBffClient(CreateJsonResponse(expected));

        // Act
        var result = await _service.ValidateSecretAsync("wrong-secret");

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Valid).IsFalse();
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
        var commandResponse = new BaseCommandResponseOfGuid
        {
            Success = true,
            Message = "Updated"
        };
        SetupBffClient(CreateJsonResponse(commandResponse));

        // Act
        var result = await _service.UpdateModuleSettingsAsync(new ModuleSettingsDto());

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
        var result = await _service.UpdateModuleSettingsAsync(new ModuleSettingsDto());

        // Assert
        await Assert.That(result.Success).IsFalse();
    }

    #endregion

    #region StorageSettingsAsync

    [Test]
    public async Task GetStorageSettingsAsync_MapsHalLinksAndUsage()
    {
        // Arrange
        var payload = new HalResourceOfInstanceStorageSettingsDto
        {
            Provider = StorageProviderOptions.Local,
            DefaultMaxUploadBytes = 10 * 1024 * 1024,
            DefaultTenantQuotaBytes = 1024L * 1024 * 1024,
            InstanceMaxUploadBytes = 100L * 1024 * 1024,
            LockTenantStorage = false,
            Usage = new Usage
            {
                UsedBytes = 4096,
                ReservedBytes = 2048,
                QuarantinedBytes = 0,
                ObjectCount = 3
            },
            ProviderStatus = new ProviderStatus
            {
                Provider = StorageProviderOptions.Local,
                IsAvailable = true,
                Message = "OK"
            },
            _links = new Dictionary<string, HalLink>
            {
                ["edit"] = new() { Href = "/api/instance/settings/storage", Method = "PUT" },
                ["provider-test"] = new() { Href = "/api/instance/settings/storage/test", Method = "POST" },
                ["recalculate-usage"] = new() { Href = "/api/instance/settings/storage/recalculate-usage", Method = "POST" }
            }
        };
        SetupBffClient(CreateJsonResponse(payload));

        // Act
        var result = await _service.GetStorageSettingsAsync();

        // Assert
        await Assert.That(result.Provider).IsEqualTo(StorageProviderOptions.Local);
        await Assert.That(result.HasLink("edit")).IsTrue();
        await Assert.That(result.HasLink("provider-test")).IsTrue();
        await Assert.That(result.HasLink("recalculate-usage")).IsTrue();
        await Assert.That(result.Usage!.UsedBytes).IsEqualTo(4096);
        await Assert.That(result.ProviderStatus!.IsAvailable).IsTrue();
    }

    [Test]
    public async Task UpdateStorageSettingsAsync_UsesStorageSettingsEndpoint_WhenEditAffordancePresent()
    {
        // Arrange
        Uri? requestUri = null;
        HttpMethod? method = null;
        string? requestBody = null;
        SetupBffClient(async request =>
        {
            requestUri = request.RequestUri;
            method = request.Method;
            requestBody = await request.Content!.ReadAsStringAsync();
            return CreateJsonResponse(new BaseCommandResponseOfGuid { Success = true, Message = "Updated" });
        });

        // Act
        var result = await _service.UpdateStorageSettingsAsync(new HalResourceOfInstanceStorageSettingsDto
        {
            Provider = StorageProviderOptions.Local,
            DefaultMaxUploadBytes = 10 * 1024 * 1024,
            DefaultTenantQuotaBytes = 1024L * 1024 * 1024,
            InstanceMaxUploadBytes = 100L * 1024 * 1024,
            _links = new Dictionary<string, HalLink>
            {
                ["edit"] = new() { Href = "/api/instance/settings/storage", Method = "PUT" }
            }
        });

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(requestUri).IsNotNull();
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/api/instance/settings/storage");
        await Assert.That(method).IsEqualTo(HttpMethod.Put);
        await Assert.That(requestBody).Contains("\"provider\":\"local\"", StringComparison.OrdinalIgnoreCase);
    }

    [Test]
    public async Task UpdateStorageSettingsAsync_DoesNotCallApi_WhenEditAffordanceMissing()
    {
        // Arrange
        var called = false;
        SetupBffClient(request =>
        {
            called = true;
            return Task.FromResult(CreateJsonResponse(new BaseCommandResponseOfGuid { Success = true }));
        });

        // Act
        var result = await _service.UpdateStorageSettingsAsync(new HalResourceOfInstanceStorageSettingsDto());

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(called).IsFalse();
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

    private static KeycloakBootstrapRequestDto CreateKeycloakBootstrapRequest() =>
        new()
        {
            KeycloakBaseUrl = "https://keycloak.example.com",
            Realm = "ISLAMU",
            BlazorClientId = "islamu-event-blazor",
            BlazorClientSecret = "runtime-blazor-secret",
            ApiClientId = "islamu-event-api",
            ApiClientSecret = "runtime-api-secret",
            Mode = 0,
            BootstrapAdminUsername = "keycloak-admin",
            BootstrapAdminPassword = "one-time-admin-password"
        };

    private void SetupBffClient(HttpResponseMessage response)
    {
        SetupBffClient(_ => Task.FromResult(response));
    }

    private void SetupBffClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _bffHandler = handler;
    }

    private void SetupBffSelfClient(HttpResponseMessage response)
    {
        SetupBffSelfClient(_ => Task.FromResult(response));
    }

    private void SetupBffSelfClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _authHandler = handler;
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
