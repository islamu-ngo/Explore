// ABOUTME: Integration tests for BFF storage upload proxy destination binding.
// ABOUTME: Proves browser-supplied upload destinations are rejected unless bound to a server-issued session.

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Explore.Blazor.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class BffStorageUploadProxyTests : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _authHeader = TestAuthHandler.CreateAuthHeaderValue(
        Guid.NewGuid(),
        "Storage Tester",
        (ClaimTypes.Name, "Storage Tester"));

    public BffStorageUploadProxyTests()
    {
        var antiforgery = Substitute.For<IAntiforgery>();
        antiforgery.ValidateRequestAsync(Arg.Any<HttpContext>()).Returns(Task.CompletedTask);

        _factory = new BlazorBffWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAntiforgery>();
                services.AddSingleton(antiforgery);
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Test]
    public async Task UploadProxy_WithArbitraryPresignedLookingHttpsUrl_ReturnsBadRequest()
    {
        using var request = CreateUploadRequest();

        using var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("server-issued upload session");
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private HttpRequestMessage CreateUploadRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/bff/storage/upload-proxy");
        request.Headers.Add("X-CSRF-TOKEN", "test-token");
        request.Headers.Add(TestAuthHandler.AuthHeaderName, _authHeader);

        var uploadUrl = "https://127.0.0.1:1/bucket/key?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Signature=fake";
        var form = new MultipartFormDataContent();
        var uploadUrlContent = new StringContent(uploadUrl);
        form.Add(uploadUrlContent, "uploadUrl");
        var contentTypeContent = new StringContent("image/png");
        form.Add(contentTypeContent, "contentType");

        var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "probe.png");
        request.Content = form;

        return request;
    }
}
