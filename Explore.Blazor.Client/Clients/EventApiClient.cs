using Explore.Blazor.Client.Constants;

namespace Explore.Blazor.Client.Clients;

/// <summary>
/// Partial class extending the NSwag-generated EventApiClient to add tenant header to all requests.
/// </summary>
public partial class EventApiClient
{
    /// <summary>
    /// Called before each request. Adds the X-Tenant-Id header with the default tenant ID.
    /// </summary>
    partial void PrepareRequest(System.Net.Http.HttpClient client, System.Net.Http.HttpRequestMessage request, string url)
    {
        // Add tenant ID header to every request
        request.Headers.Add(TenantConstants.TenantIdHeaderName, TenantConstants.DefaultTenantId.ToString());
    }
}
