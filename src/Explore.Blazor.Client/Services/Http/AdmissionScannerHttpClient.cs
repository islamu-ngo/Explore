// ABOUTME: Isolates the scanner-capability HttpClient from authenticated staff transport handlers.
// ABOUTME: Provides only the exact online scanner check-in operation and retains no request material.

namespace Explore.Blazor.Client.Services.Http;

public sealed class AdmissionScannerHttpClient(IHttpClientFactory httpClientFactory)
{
    internal const string ClientName = "AdmissionScannerClient";

    public Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        httpClientFactory.CreateClient(ClientName).SendAsync(request, cancellationToken);
}
