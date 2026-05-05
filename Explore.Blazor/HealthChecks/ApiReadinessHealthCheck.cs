// ABOUTME: Blazor BFF readiness health check for the downstream Explore API dependency.
// ABOUTME: Uses a dedicated no-token HttpClient so health probing never forwards user credentials.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Explore.Blazor.HealthChecks;

public sealed class ApiReadinessHealthCheck(IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public const string HttpClientName = "ExploreApiHealth";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync("health", cancellationToken).ConfigureAwait(false);
            var data = new Dictionary<string, object>
            {
                ["statusCode"] = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Explore API readiness endpoint is reachable.", data);
            }

            return HealthCheckResult.Unhealthy("Explore API readiness endpoint returned a non-success status code.", data: data);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("Explore API readiness endpoint is unreachable.", ex);
        }
    }
}
