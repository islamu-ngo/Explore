// ABOUTME: Unit-style DelegatingHandler tests for tenant and forwarded-host propagation behavior.
// ABOUTME: Verifies request header forwarding based on ITenantRouteContextAccessor and HttpContext host values.

using Explore.Application.Constants;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Http;

namespace Explore.Blazor.IntegrationTests.Handlers;

public class TenantHeaderForwardingHandlerTests
{
    [Test]
    public async Task SendAsync_WithTenantSlug_AddsXTenantSlugHeader()
    {
        var tenantAccessor = Substitute.For<ITenantRouteContextAccessor>();
        tenantAccessor.TenantSlug.Returns("acme");

        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var innerHandler = new CapturingHandler();
        var handler = new TenantHeaderForwardingHandler(httpContextAccessor, tenantAccessor)
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/events");
        using var response = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains(TenantHeaderNames.TenantSlug)).IsTrue();
        await Assert.That(innerHandler.CapturedRequest.Headers.GetValues(TenantHeaderNames.TenantSlug).Single()).IsEqualTo("acme");
    }

    [Test]
    public async Task SendAsync_WithoutTenantSlug_DoesNotAddXTenantSlugHeader()
    {
        var tenantAccessor = Substitute.For<ITenantRouteContextAccessor>();
        tenantAccessor.TenantSlug.Returns((string?)null);

        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var innerHandler = new CapturingHandler();
        var handler = new TenantHeaderForwardingHandler(httpContextAccessor, tenantAccessor)
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/events");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains(TenantHeaderNames.TenantSlug)).IsFalse();
    }

    [Test]
    public async Task SendAsync_WithHttpContextHost_AddsXForwardedHostHeader()
    {
        var tenantAccessor = Substitute.For<ITenantRouteContextAccessor>();
        tenantAccessor.TenantSlug.Returns((string?)null);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("bff.example.com");

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var innerHandler = new CapturingHandler();
        var handler = new TenantHeaderForwardingHandler(httpContextAccessor, tenantAccessor)
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/events");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains("X-Forwarded-Host")).IsTrue();
        await Assert.That(innerHandler.CapturedRequest.Headers.GetValues("X-Forwarded-Host").Single()).IsEqualTo("bff.example.com");
    }

    [Test]
    public async Task SendAsync_WithExistingHeaders_DoesNotOverwrite()
    {
        var tenantAccessor = Substitute.For<ITenantRouteContextAccessor>();
        tenantAccessor.TenantSlug.Returns("acme");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("bff.example.com");

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var innerHandler = new CapturingHandler();
        var handler = new TenantHeaderForwardingHandler(httpContextAccessor, tenantAccessor)
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/events");
        request.Headers.Add(TenantHeaderNames.TenantSlug, "preset-tenant");
        request.Headers.Add("X-Forwarded-Host", "preset-host");

        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.GetValues(TenantHeaderNames.TenantSlug).Single()).IsEqualTo("preset-tenant");
        await Assert.That(innerHandler.CapturedRequest.Headers.GetValues("X-Forwarded-Host").Single()).IsEqualTo("preset-host");
    }
}
