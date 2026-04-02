// ABOUTME: Unit-style DelegatingHandler tests for correlation ID propagation on outgoing HTTP requests.
// ABOUTME: Verifies X-Correlation-ID header is added when missing and preserved when already present.

using Explore.Infrastructure.Services;

namespace Explore.Blazor.IntegrationTests.Handlers;

public class CorrelationIdDelegatingHandlerTests
{
    [Test]
    public async Task SendAsync_AlwaysAdds_XCorrelationIdHeader()
    {
        var innerHandler = new CapturingHandler();
        var handler = new CorrelationIdDelegatingHandler
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/events");
        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.Contains("X-Correlation-ID")).IsTrue();
        var value = innerHandler.CapturedRequest.Headers.GetValues("X-Correlation-ID").Single();
        await Assert.That(string.IsNullOrWhiteSpace(value)).IsFalse();
    }

    [Test]
    public async Task SendAsync_WithExistingCorrelationId_DoesNotOverwrite()
    {
        var innerHandler = new CapturingHandler();
        var handler = new CorrelationIdDelegatingHandler
        {
            InnerHandler = innerHandler
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/events");
        request.Headers.Add("X-Correlation-ID", "preset-correlation-id");

        _ = await invoker.SendAsync(request, CancellationToken.None);

        await Assert.That(innerHandler.CapturedRequest).IsNotNull();
        await Assert.That(innerHandler.CapturedRequest!.Headers.GetValues("X-Correlation-ID").Single()).IsEqualTo("preset-correlation-id");
    }
}
