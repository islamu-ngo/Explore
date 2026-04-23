// ABOUTME: Verifies BFF self-call cookie forwarding works across circuit activity boundaries.
// ABOUTME: Ensures the handler can read the current circuit cookie from the activity-scoped bridge.

using Explore.Blazor.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Explore.Blazor.IntegrationTests.Handlers;

public class BffCookieForwardingHandlerTests
{
    [Test]
    public async Task SendAsync_WithinCircuitActivityScope_ForwardsCookieHeader()
    {
        var circuitCookieStore = new BffAuthCookieStore();
        circuitCookieStore.SetCookieHeader("AuthCookie=test-value; XSRF-TOKEN=xsrf-value");

        var innerHandler = new CapturingHandler();

        using (circuitCookieStore.BeginActivityScope())
        {
            var handler = new BffCookieForwardingHandler(
                new BffAuthCookieStore(),
                NullLogger<BffCookieForwardingHandler>.Instance)
            {
                InnerHandler = innerHandler
            };

            using var invoker = new HttpMessageInvoker(handler);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://bff.example.com/bff/auth/refresh-session");
            _ = await invoker.SendAsync(request, CancellationToken.None);
        }

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.TryGetValues("Cookie", out var cookieValues)).IsTrue();
        await Assert.That(cookieValues).Contains("AuthCookie=test-value; XSRF-TOKEN=xsrf-value");
        await Assert.That(innerHandler.CapturedRequest.Headers.TryGetValues("X-CSRF-TOKEN", out var headerValues)).IsTrue();
        await Assert.That(headerValues).Contains("xsrf-value");
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
