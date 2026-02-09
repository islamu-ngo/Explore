namespace Explore.Blazor.Client.Clients;

/// <summary>
/// Partial class extending NSwag-generated EventApiClient.
/// Tenant context is resolved server-side from forwarded host or explicit X-Tenant-Id when provided.
/// </summary>
public partial class EventApiClient
{
    /// <summary>
    /// Called before each request.
    /// </summary>
    partial void PrepareRequest(System.Net.Http.HttpClient client, System.Net.Http.HttpRequestMessage request, string url)
    {
        // Intentionally left empty.
        // Host/subdomain/custom-domain resolution is handled by the API tenant context.
    }
}
