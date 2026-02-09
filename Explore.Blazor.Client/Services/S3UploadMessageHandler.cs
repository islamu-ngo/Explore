using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Message handler for S3 cross-origin uploads from Blazor WASM.
/// Configures requests for CORS mode to allow PUT requests to external S3-compatible storage.
/// </summary>
public class S3UploadMessageHandler : DelegatingHandler
{
    private readonly ILogger<S3UploadMessageHandler>? _logger;

    public S3UploadMessageHandler()
    {
    }

    public S3UploadMessageHandler(ILogger<S3UploadMessageHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Set CORS mode for cross-origin S3 requests
        // This is required for browser to allow PUT requests to external origins (Hetzner Object Storage)
        request.SetBrowserRequestMode(BrowserRequestMode.Cors);

        // Do NOT include credentials for S3 pre-signed URL requests
        // Pre-signed URLs contain all authentication in the URL itself
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Omit);

        _logger?.LogDebug("[S3 Upload] {Method} {Host} - Mode: CORS, Credentials: Omit",
            request.Method, request.RequestUri?.Host);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("[S3 Upload] {Method} {Uri} - Failed with status {StatusCode}",
                    request.Method, request.RequestUri, (int)response.StatusCode);
            }
            else
            {
                _logger?.LogInformation("[S3 Upload] {Method} {Host} - Success ({StatusCode})",
                    request.Method, request.RequestUri?.Host, (int)response.StatusCode);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[S3 Upload] {Method} {Uri} - Request failed with exception",
                request.Method, request.RequestUri);
            throw;
        }
    }
}
