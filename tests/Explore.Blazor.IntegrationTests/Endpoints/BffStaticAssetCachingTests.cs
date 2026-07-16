// ABOUTME: Regression tests for cache-safe static assets at the Blazor BFF boundary.
// ABOUTME: Proves XSRF token issuance does not force Home Discovery images into no-store responses.

using System.Net;
using Explore.Blazor.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Explore.Blazor.IntegrationTests.Endpoints;

public sealed class BffStaticAssetCachingTests : IAsyncDisposable
{
    private readonly BlazorBffWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public BffStaticAssetCachingTests()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
    }

    [Test]
    public async Task StaticAsset_Get_DoesNotIssueXsrfCookieOrDisableStorage()
    {
        using var response = await _client.GetAsync("/favicon.ico");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var setCookies).Should().BeFalse();
        (response.Headers.CacheControl?.NoStore ?? false).Should().BeFalse();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }
}
