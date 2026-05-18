// ABOUTME: Unit tests for the tenant branding typed settings admin BFF service.
// ABOUTME: Verifies HAL-gated editability, typed JSONB document replacement, and no scalar settings fallback calls.

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Refit;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class TenantBrandingSettingsAdminServiceTests
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantBrandingSettingsAdminService> _logger;
    private TenantBrandingSettingsAdminService _service;

    public TenantBrandingSettingsAdminServiceTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = Substitute.For<ILogger<TenantBrandingSettingsAdminService>>();
        // _service initialization is deferred to SetupBffClient where we have the mocked client
        _service = null!; 
    }

    [Test]
    public async Task GetAsync_WhenApiReturnsHalDocument_MapsPayloadAndReplaceAffordance()
    {
        Guid stamp = Guid.Parse("11111111-1111-1111-1111-111111111111");
        string json = $$"""
        {
          "documentKey": "tenant.branding",
          "schemaVersion": 1,
          "defaultsVersion": "2026-05-branding",
          "concurrencyStamp": "{{stamp}}",
          "payload": {
            "displayName": "Typed Tenant",
            "logoUrl": "https://cdn.example.test/logo.svg",
            "faviconUrl": "https://cdn.example.test/favicon.ico",
            "customCssUrl": "https://cdn.example.test/tenant.css"
          },
          "_links": {
            "self": { "href": "/api/tenant/settings/documents/branding", "method": "GET" },
            "self/replace-settings": { "href": "/api/tenant/settings/documents/branding", "method": "PUT" }
          }
        }
        """;
        SetupBffClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        TenantBrandingSettingsAdminModel result = await _service.GetAsync();

        await Assert.That(result.Exists).IsTrue();
        await Assert.That(result.CanReplace).IsTrue();
        await Assert.That(result.ConcurrencyStamp).IsEqualTo(stamp);
        await Assert.That(result.DisplayName).IsEqualTo("Typed Tenant");
        await Assert.That(result.LogoUrl).IsEqualTo("https://cdn.example.test/logo.svg");
        await Assert.That(result.FaviconUrl).IsEqualTo("https://cdn.example.test/favicon.ico");
        await Assert.That(result.CustomCssUrl).IsEqualTo("https://cdn.example.test/tenant.css");
    }

    [Test]
    public async Task GetAsync_WhenApiReturnsNotFound_ReportsSafeLoadFailure()
    {
        SetupBffClient(new HttpResponseMessage(HttpStatusCode.NotFound));

        TenantBrandingSettingsAdminModel result = await _service.GetAsync();

        await Assert.That(result.Exists).IsFalse();
        await Assert.That(result.CanReplace).IsFalse();
        await Assert.That(result.ErrorMessage).IsEqualTo("Unable to load tenant branding settings.");
    }

    [Test]
    public async Task SaveAsync_WhenHalActionIsMissing_DoesNotSendPut()
    {
        bool requestSent = false;
        SetupBffClient(request =>
        {
            requestSent = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        TenantBrandingSettingsAdminModel model = new()
        {
            Exists = true,
            CanReplace = false,
            ConcurrencyStamp = Guid.NewGuid(),
            DisplayName = "No Access"
        };

        TenantBrandingSettingsSaveResult result = await _service.SaveAsync(model);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("You do not have permission to replace tenant branding settings.");
        await Assert.That(requestSent).IsFalse();
    }

    [Test]
    public async Task SaveAsync_WhenApiSucceeds_SendsFullDocumentReplaceAndReturnsUpdatedModel()
    {
        Guid expectedStamp = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid updatedStamp = Guid.Parse("33333333-3333-3333-3333-333333333333");
        HttpRequestMessage? capturedRequest = null;
        SetupBffClient(async request =>
        {
            capturedRequest = request;
            string body = await request.Content!.ReadAsStringAsync();
            using JsonDocument requestJson = JsonDocument.Parse(body);
            await Assert.That(requestJson.RootElement.GetProperty("expectedConcurrencyStamp").GetGuid()).IsEqualTo(expectedStamp);
            await Assert.That(requestJson.RootElement.GetProperty("payload").GetProperty("displayName").GetString()).IsEqualTo("Updated Tenant");
            await Assert.That(requestJson.RootElement.GetProperty("payload").GetProperty("customCssUrl").GetString()).IsEqualTo("https://cdn.example.test/tenant.css");

            string json = $$"""
            {
              "documentKey": "tenant.branding",
              "schemaVersion": 1,
              "defaultsVersion": "2026-05-branding",
              "concurrencyStamp": "{{updatedStamp}}",
              "payload": { "displayName": "Updated Tenant", "customCssUrl": "https://cdn.example.test/updated.css" },
              "_links": {
                "self": { "href": "/api/tenant/settings/documents/branding", "method": "GET" },
                "self/replace-settings": { "href": "/api/tenant/settings/documents/branding", "method": "PUT" }
              }
            }
            """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        TenantBrandingSettingsAdminModel model = new()
        {
            Exists = true,
            CanReplace = true,
            ConcurrencyStamp = expectedStamp,
            DisplayName = " Updated Tenant ",
            CustomCssUrl = " https://cdn.example.test/tenant.css "
        };

        TenantBrandingSettingsSaveResult result = await _service.SaveAsync(model);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Model).IsNotNull();
        await Assert.That(result.Model!.ConcurrencyStamp).IsEqualTo(updatedStamp);
        await Assert.That(result.Model.DisplayName).IsEqualTo("Updated Tenant");
        await Assert.That(result.Model.CustomCssUrl).IsEqualTo("https://cdn.example.test/updated.css");
        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.Method).IsEqualTo(HttpMethod.Put);
        await Assert.That(capturedRequest.RequestUri!.ToString())
            .EndsWith("api/tenant/settings/documents/branding", StringComparison.Ordinal);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T model, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(model)
        };
    }

    private void SetupBffClient(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.local") };
        var api = RestService.For<ITenantBrandingSettingsApi>(client);
        _service = new TenantBrandingSettingsAdminService(api, _logger);
    }

    private void SetupBffClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        var httpHandler = new MockHttpMessageHandler(handler);
        var client = new HttpClient(httpHandler) { BaseAddress = new Uri("https://test.local") };
        var api = RestService.For<ITenantBrandingSettingsApi>(client);
        _service = new TenantBrandingSettingsAdminService(api, _logger);
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
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
