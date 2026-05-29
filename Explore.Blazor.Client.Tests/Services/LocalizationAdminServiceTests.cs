// ABOUTME: Unit tests for LocalizationAdminService's Refit-backed endpoint wrapper.
// ABOUTME: Covers successful responses, unauthorized/error responses, network failures, and request shapes.

using System.Net;
using System.Text;
using System.Text.Json;
using Explore.Blazor.Client.Models.Admin;
using Microsoft.Extensions.Logging.Abstractions;
using Refit;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class LocalizationAdminServiceTests
{
    [Test]
    public async Task GetConfigurationAsync_WhenApiSucceeds_ReturnsConfiguration()
    {
        using var handler = new RecordingHandler(_ => CreateJsonResponse(new LocalizationConfigDto
        {
            DefaultLanguage = "en",
            TmsProvider = "tolgee",
            EnabledLanguages = ["en", "fr"],
            FallbackLanguage = "en"
        }));
        var service = CreateService(handler);

        var result = await service.GetConfigurationAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TmsProvider).IsEqualTo("tolgee");
        await Assert.That(handler.Requests.Single().Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(handler.Requests.Single().RequestUri!.PathAndQuery)
            .IsEqualTo("/api/admin/localization/configuration");
    }

    [Test]
    public async Task GetConfigurationAsync_WhenUnauthorized_ReturnsNull()
    {
        using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var service = CreateService(handler);

        var result = await service.GetConfigurationAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetConfigurationAsync_WhenNetworkFails_ReturnsNull()
    {
        using var handler = new RecordingHandler(_ => throw new HttpRequestException("network failed"));
        var service = CreateService(handler);

        var result = await service.GetConfigurationAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task TestConnectionAsync_WhenApiSucceeds_ReturnsServerMessage()
    {
        using var handler = new RecordingHandler(_ => CreateJsonResponse(new LocalizationAdminCommandResponse
        {
            Success = true,
            Message = "Connected."
        }));
        var service = CreateService(handler);

        var result = await service.TestConnectionAsync();

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Connected.");
        await Assert.That(handler.Requests.Single().Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(handler.Requests.Single().RequestUri!.PathAndQuery)
            .IsEqualTo("/api/admin/localization/test-connection");
    }

    [Test]
    public async Task TestConnectionAsync_WhenApiReturnsForbidden_ReturnsFailureFallback()
    {
        using var handler = new RecordingHandler(_ => CreateJsonResponse(new LocalizationAdminCommandResponse
        {
            Success = false,
            Message = "Forbidden"
        }, HttpStatusCode.Forbidden));
        var service = CreateService(handler);

        var result = await service.TestConnectionAsync();

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("TMS connection failed.");
    }

    [Test]
    public async Task TestConnectionAsync_WhenNetworkFails_ReturnsFailureWithReason()
    {
        using var handler = new RecordingHandler(_ => throw new HttpRequestException("network failed"));
        var service = CreateService(handler);

        var result = await service.TestConnectionAsync();

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("network failed");
    }

    [Test]
    public async Task ExportFromTmsAsync_WhenApiSucceeds_SendsLanguageQueryAndMapsResult()
    {
        using var handler = new RecordingHandler(_ => CreateJsonResponse(new LocalizationAdminCommandResponse
        {
            Success = true,
            Message = "Exported 12 keys."
        }));
        var service = CreateService(handler);

        var result = await service.ExportFromTmsAsync("fr");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Exported 12 keys.");
        await Assert.That(handler.Requests.Single().Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(handler.Requests.Single().RequestUri!.PathAndQuery)
            .IsEqualTo("/api/admin/localization/export-from-tms?languageCode=fr");
    }

    [Test]
    public async Task ExportFromTmsAsync_WhenApiReturnsBadRequest_ReturnsFailureFallback()
    {
        using var handler = new RecordingHandler(_ => CreateJsonResponse(new LocalizationAdminCommandResponse
        {
            Success = false,
            Message = "Invalid language"
        }, HttpStatusCode.BadRequest));
        var service = CreateService(handler);

        var result = await service.ExportFromTmsAsync("fr");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Export failed for 'fr'.");
    }

    [Test]
    public async Task UpdateGovernanceAsync_WhenApiSucceeds_SendsPutBodyAndMapsResult()
    {
        using var handler = new RecordingHandler(_ => CreateJsonResponse(new LocalizationAdminCommandResponse
        {
            Success = true,
            Message = "Saved."
        }));
        var service = CreateService(handler);
        var payload = new LocalizationGovernancePayload
        {
            DefaultLanguage = "fr",
            TmsProvider = "weblate",
            TmsApiUrl = "https://weblate.test",
            TmsProjectId = "event",
            TmsComponent = "ui",
            EnabledLanguages = ["en", "fr"],
            FallbackLanguage = "en",
            ClientPickerEnabled = false,
            ForceOfflineMode = true
        };

        var result = await service.UpdateGovernanceAsync(payload);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Saved.");
        var request = handler.Requests.Single();
        await Assert.That(request.Method).IsEqualTo(HttpMethod.Put);
        await Assert.That(request.RequestUri!.PathAndQuery).IsEqualTo("/api/admin/localization/governance");
        var body = handler.RequestBodies.Single();
        await Assert.That(body).Contains("\"defaultLanguage\":\"fr\"");
        await Assert.That(body).Contains("\"forceOfflineMode\":true");
    }

    [Test]
    public async Task UpdateGovernanceAsync_WhenNetworkFails_ReturnsFailure()
    {
        using var handler = new RecordingHandler(_ => throw new HttpRequestException("network failed"));
        var service = CreateService(handler);

        var result = await service.UpdateGovernanceAsync(new LocalizationGovernancePayload());

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).Contains("network failed");
    }

    [Test]
    public async Task GetBundlePathHealthAsync_WhenApiSucceeds_ReturnsHealth()
    {
        using var handler = new RecordingHandler(_ => CreateJsonResponse(new BundlePathHealthResult(
            Exists: true,
            Writable: false,
            Reason: "Permission denied",
            TargetPath: "/app/App_Data/Localization/Bundles")));
        var service = CreateService(handler);

        var result = await service.GetBundlePathHealthAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Exists).IsTrue();
        await Assert.That(result.Writable).IsFalse();
        await Assert.That(result.Reason).IsEqualTo("Permission denied");
        await Assert.That(handler.Requests.Single().RequestUri!.PathAndQuery)
            .IsEqualTo("/api/admin/localization/bundle-health");
    }

    [Test]
    public async Task GetBundlePathHealthAsync_WhenApiReturnsForbidden_ReturnsNull()
    {
        using var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var service = CreateService(handler);

        var result = await service.GetBundlePathHealthAsync();

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GetBundlePathHealthAsync_WhenNetworkFails_ReturnsNull()
    {
        using var handler = new RecordingHandler(_ => throw new HttpRequestException("network failed"));
        var service = CreateService(handler);

        var result = await service.GetBundlePathHealthAsync();

        await Assert.That(result).IsNull();
    }

    private static LocalizationAdminService CreateService(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://client.test")
        };
        var api = RestService.For<ILocalizationAdminApi>(client);
        return new LocalizationAdminService(api, NullLogger<LocalizationAdminService>.Instance);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T model, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> RequestBodies { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.Content is not null)
            {
                RequestBodies.Add(request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult());
            }

            var response = responder(request);
            response.RequestMessage ??= request;
            return Task.FromResult(response);
        }
    }
}
