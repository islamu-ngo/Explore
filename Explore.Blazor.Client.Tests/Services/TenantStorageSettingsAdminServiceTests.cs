// ABOUTME: Unit tests for tenant storage admin service HAL mapping and save behavior.
// ABOUTME: Ensures tenant storage overrides are gated by API-provided edit affordances.

using System.Net;
using System.Text;
using System.Text.Json;
using Refit;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class TenantStorageSettingsAdminServiceTests
{
    private Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler = _ =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

    [Test]
    public async Task GetAsync_MapsEditableHalResource()
    {
        // Arrange
        var service = CreateService(CreateJsonResponse(new Dictionary<string, object?>
        {
            ["tenantId"] = Guid.NewGuid(),
            ["provider"] = "local",
            ["maxUploadBytes"] = 10 * 1024 * 1024,
            ["tenantQuotaBytes"] = 1024L * 1024 * 1024,
            ["isReadOnly"] = false,
            ["tenantOverridesAllowed"] = true,
            ["tenantStorageLocked"] = false,
            ["effectivePolicy"] = new Dictionary<string, object?>
            {
                ["provider"] = "local",
                ["maxUploadBytes"] = 10 * 1024 * 1024,
                ["tenantQuotaBytes"] = 1024L * 1024 * 1024,
                ["instanceMaxUploadBytes"] = 100L * 1024 * 1024
            },
            ["usage"] = new Dictionary<string, object?>
            {
                ["provider"] = "local",
                ["usedBytes"] = 1024,
                ["availableBytes"] = 2048,
                ["objectCount"] = 2
            },
            ["_links"] = new Dictionary<string, object?>
            {
                ["edit"] = new { href = "/api/tenant/settings/storage", method = "PUT" }
            }
        }));

        // Act
        var result = await service.GetAsync();

        // Assert
        await Assert.That(result.IsEditable).IsTrue();
        await Assert.That(result.Usage.UsedBytes).IsEqualTo(1024);
        await Assert.That(result.EffectivePolicy.InstanceMaxUploadBytes).IsEqualTo(100L * 1024 * 1024);
    }

    [Test]
    public async Task SaveAsync_DoesNotCallApi_WhenEditAffordanceMissing()
    {
        // Arrange
        var called = false;
        var service = CreateService(request =>
        {
            called = true;
            return Task.FromResult(CreateJsonResponse(new InstanceCommandResponseModel { Success = true }));
        });

        // Act
        var result = await service.SaveAsync(new TenantStorageSettingsModel { CanUpdate = false });

        // Assert
        await Assert.That(result.Success).IsFalse();
        await Assert.That(called).IsFalse();
    }

    [Test]
    public async Task SaveAsync_SendsTenantStorageDto_WhenEditable()
    {
        // Arrange
        Uri? requestUri = null;
        HttpMethod? method = null;
        string? requestBody = null;
        var service = CreateService(async request =>
        {
            requestUri = request.RequestUri;
            method = request.Method;
            requestBody = await request.Content!.ReadAsStringAsync();
            return CreateJsonResponse(new InstanceCommandResponseModel { Success = true, Message = "Updated" });
        });

        // Act
        var result = await service.SaveAsync(new TenantStorageSettingsModel
        {
            TenantId = Guid.NewGuid(),
            CanUpdate = true,
            IsReadOnly = false,
            TenantOverridesAllowed = true,
            TenantStorageLocked = false,
            Provider = StorageProviderOptions.Local,
            MaxUploadBytes = 10 * 1024 * 1024,
            TenantQuotaBytes = 1024L * 1024 * 1024
        });

        // Assert
        await Assert.That(result.Success).IsTrue();
        await Assert.That(requestUri).IsNotNull();
        await Assert.That(requestUri!.AbsolutePath).IsEqualTo("/api/tenant/settings/storage");
        await Assert.That(method).IsEqualTo(HttpMethod.Put);
        await Assert.That(requestBody).Contains("\"provider\":\"local\"", StringComparison.OrdinalIgnoreCase);
    }

    private TenantStorageSettingsAdminService CreateService(HttpResponseMessage response) =>
        CreateService(_ => Task.FromResult(response));

    private TenantStorageSettingsAdminService CreateService(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
        var client = new HttpClient(new MockHttpMessageHandler(request => _handler(request)))
        {
            BaseAddress = new Uri("https://test.local")
        };
        var api = RestService.For<ITenantStorageSettingsApi>(client);
        return new TenantStorageSettingsAdminService(api, Substitute.For<ILogger<TenantStorageSettingsAdminService>>());
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T model, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request);
    }
}
