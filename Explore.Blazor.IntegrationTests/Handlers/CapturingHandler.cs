// ABOUTME: Shared test HttpMessageHandler that captures outgoing requests from DelegatingHandler pipelines.
// ABOUTME: Returns deterministic HTTP 200 responses so tests can assert forwarded headers.

namespace Explore.Blazor.IntegrationTests.Handlers;

internal sealed class CapturingHandler : HttpMessageHandler
{
    public HttpRequestMessage? CapturedRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CapturedRequest = request;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
