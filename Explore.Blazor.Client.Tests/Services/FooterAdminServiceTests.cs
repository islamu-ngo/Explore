// ABOUTME: Unit tests for FooterAdminService HTTP executor migration and fallback behavior.
// ABOUTME: Verifies footer admin endpoint routes, deserialization, and resilient command failures.

using System.Net;
using System.Text;
using System.Text.Json;
using Explore.Blazor.Client.Contracts.Services.Footer;
using Explore.Blazor.Client.Models.Responses;
using Refit;

namespace Explore.Blazor.Client.Tests.Services;

public sealed class FooterAdminServiceTests
{
    private readonly ILogger<FooterAdminService> _logger = Substitute.For<ILogger<FooterAdminService>>();

    [Test]
    public async Task GetLinkGroupsAsync_ReturnsGroups_WhenApiSucceeds()
    {
        var groups = new List<FooterLinkGroupListModel>
        {
            new() { Id = Guid.NewGuid(), Title = "Main", IsActive = true }
        };
        using var handler = new RecordingHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse(groups)));
        var service = CreateService(handler);

        var result = await service.GetLinkGroupsAsync();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Title).IsEqualTo("Main");
        await Assert.That(handler.LastRequest).IsNotNull();
        await Assert.That(handler.LastRequest!.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(handler.LastRequest.RequestUri!.PathAndQuery).IsEqualTo("/api/footer/link-groups");
    }

    [Test]
    public async Task GetFooterSettingsAsync_ReturnsSettings_FromConfigEnvelope()
    {
        var envelope = new
        {
            settings = new FooterSettingsResponseModel
            {
                Enabled = true,
                Template = "compact"
            }
        };
        using var handler = new RecordingHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse(envelope)));
        var service = CreateService(handler);

        var result = await service.GetFooterSettingsAsync();

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Enabled).IsTrue();
        await Assert.That(result.Template).IsEqualTo("compact");
        await Assert.That(handler.LastRequest).IsNotNull();
        await Assert.That(handler.LastRequest!.Method).IsEqualTo(HttpMethod.Get);
        await Assert.That(handler.LastRequest.RequestUri!.PathAndQuery).IsEqualTo("/api/footer/config");
    }

    [Test]
    public async Task CreateLinkGroupAsync_ReturnsFailureResponse_WhenApiReturnsBadRequest()
    {
        using var handler = new RecordingHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request"
        }));
        var service = CreateService(handler);

        var result = await service.CreateLinkGroupAsync(new CreateFooterLinkGroupModel { Title = "Main" });

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
        await Assert.That(result.Message).Contains("Error:");
        await Assert.That(result.Errors).IsNotEmpty();
        await Assert.That(handler.LastRequest).IsNotNull();
        await Assert.That(handler.LastRequest!.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(handler.LastRequest.RequestUri!.PathAndQuery).IsEqualTo("/api/footer/link-groups");
    }

    [Test]
    public async Task UpdateLinkAsync_SendsPutToLinkEndpoint_AndReturnsSuccessBody()
    {
        var expected = new BaseCommandResponse<Guid>
        {
            Success = true,
            Message = "updated",
            Id = Guid.NewGuid()
        };
        using var handler = new RecordingHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse(expected)));
        var service = CreateService(handler);
        var linkId = Guid.NewGuid();

        var result = await service.UpdateLinkAsync(linkId, new UpdateFooterLinkModel { Label = "Docs", Url = "/docs", IsActive = true });

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(expected.Id);
        await Assert.That(handler.LastRequest).IsNotNull();
        await Assert.That(handler.LastRequest!.Method).IsEqualTo(HttpMethod.Put);
        await Assert.That(handler.LastRequest.RequestUri!.PathAndQuery).IsEqualTo($"/api/footer/links/{linkId}");
    }

    [Test]
    public async Task DeleteLinkAsync_ReturnsFailureResponse_WhenHttpThrows()
    {
        using var handler = new RecordingHttpMessageHandler(_ => throw new HttpRequestException("network failed"));
        var service = CreateService(handler);

        var result = await service.DeleteLinkAsync(Guid.NewGuid());

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsFalse();
        await Assert.That(result.Message).Contains("Error:");
        await Assert.That(result.Errors).Contains("network failed");
    }

    [Test]
    public async Task UpdateTenantSettingsAsync_SendsPutToSettingsEndpoint_AndReturnsSuccessBody()
    {
        var expected = new BaseCommandResponse<Guid>
        {
            Success = true,
            Message = "updated",
            Id = Guid.NewGuid()
        };
        using var handler = new RecordingHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse(expected)));
        var service = CreateService(handler);

        var result = await service.UpdateTenantSettingsAsync(new UpdateTenantFooterSettingsModel { Enabled = true });

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Success).IsTrue();
        await Assert.That(handler.LastRequest).IsNotNull();
        await Assert.That(handler.LastRequest!.Method).IsEqualTo(HttpMethod.Put);
        await Assert.That(handler.LastRequest.RequestUri!.PathAndQuery).IsEqualTo("/api/footer/settings");
    }

    private FooterAdminService CreateService(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test.local")
        };

        return new FooterAdminService(RestService.For<IFooterAdminApi>(client), _logger);
    }

    private static HttpResponseMessage CreateJsonResponse<T>(T model, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class RecordingHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return handler(request);
        }
    }
}
