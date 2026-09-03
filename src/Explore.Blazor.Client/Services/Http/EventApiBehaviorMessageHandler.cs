// ABOUTME: Applies generated Event API idempotency and capability-capture transport behavior.
// ABOUTME: Shares operation hooks across the monolithic and all per-tag NSwag clients.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services.Http;

public sealed class EventApiBehaviorMessageHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        EventApiTransportBehavior.PrepareRequest(request);
        var response = await base.SendAsync(request, cancellationToken);
        EventApiTransportBehavior.ProcessResponse(request, response);
        return response;
    }
}
